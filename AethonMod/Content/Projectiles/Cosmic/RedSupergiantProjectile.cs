using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Particles;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// RedSupergiantProjectile — v6.26 — LA SUPERGIGANTE ROJA.
    ///
    /// EL COLOSO: enorme (~90 px), roja y fría — y muere como mueren las
    /// masivas: COLAPSO → NOVA.
    ///
    /// FÍSICA:
    ///   · EL PULSO DE VIDA LENTO: late a 0.2 Hz hinchándose 5% (el
    ///     cuerpo entero respira en segundos).
    ///   · AURA: cada 10 ticks, radio del cuerpo (~75% de BodyPx) al 45%
    ///     del daño + OnFire (la gigante abrasa por TAMAÑO, no por furia).
    ///   · DERIVA lenta con freno (una masa tan grande no se apura).
    ///   · EL GUION DE LA MUERTE: los últimos 20 ticks el cuerpo SE
    ///     COLAPSA (ai[2] = 0..1 de la implosión: el renderer encoge
    ///     TODO con el anillo de distorsión) — y la NOVA sale DEL
    ///     COLAPSO en OnKill: ×1.8 de daño, OndaLib.Kick grande, la
    ///     onda StyleNova de la casa y el fuego SolarFire de PyraLib.
    ///   · Vida 7 s.
    ///
    /// Daño MP-seguro: SimpleStrikeNPC con Main.netMode != MultiplayerClient.
    /// Visual solo cliente (Main.netMode == Server → return).
    /// </summary>
    public class RedSupergiantProjectile : ModProjectile
    {
        /// <summary>Vida total en ticks (7 s — las masivas viven poco).</summary>
        private const int Lifetime = 420;

        /// <summary>Duración del COLAPSO previo a la nova (ticks).</summary>
        private const int CollapseTicks = 20;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 116;
            Projectile.height = 116;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            // ai[1] = semilla determinista del disparo (visual).
            if (Projectile.ai[1] <= 0f)
                Projectile.ai[1] = (Projectile.identity % 9973 + 1) * 1f;
        }

        /// <summary>Edad en ticks (ai[0] — determinista, sincronizado).</summary>
        private float Age => Projectile.ai[0];

        /// <summary>0..1 del COLAPSO (ai[2]: los últimos 20 ticks).</summary>
        private float Collapse
        {
            get => Projectile.ai[2];
            set => Projectile.ai[2] = value;
        }

        /// <summary>Semilla determinista del disparo.</summary>
        private int Seed => Math.Max(1, (int)Projectile.ai[1]) + Projectile.identity;

        /// <summary>0..1 del ciclo de vida.</summary>
        private float LifeT => MathHelper.Clamp(Age / Lifetime, 0f, 1f);

        public override void AI()
        {
            Projectile.ai[0] += 1f;

            // === EL DISPARADOR DEL COLAPSO: cuando quedan ≤20 ticks, el
            //     guion de la muerte arranca (UNA sola vez, en ai[2]) ===
            if (Collapse <= 0f && Projectile.timeLeft <= CollapseTicks)
                Collapse = 1f / CollapseTicks;

            // === EL POP ELÁSTICO (nace GRANDE — coloso) + EL PULSO DE VIDA
            //     LENTO: 0.2 Hz hinchándose 5% (el renderer también respira
            //     su geometría con el MISMO seno: un solo corazón) ===
            float pop = ElasticOut(Utils.GetLerpValue(0f, 70f, Age, true));
            float breathe = 1f + 0.05f * MathF.Sin(
                Main.GlobalTimeWrappedHourly * RedSupergiantRenderer.PulseHz * MathHelper.TwoPi + Seed);
            Projectile.scale = pop * breathe;

            if (Collapse > 0f)
            {
                // === EL COLAPSO: los últimos 20 ticks — el cuerpo se APRIETA
                //     (la escala la aplica el renderer como (1−c)²; aquí solo
                //     contamos el progreso, congelamos la deriva y sonamos) ===
                Collapse = MathHelper.Clamp(Collapse + 1f / CollapseTicks, 0f, 1f);
                Projectile.velocity *= 0.80f;
                Projectile.scale = pop;   // sin respiración: el corazón PARÓ

                // EL INICIO DEL COLAPSO (una sola vez): implosión + temblor.
                if (Collapse <= 1f / CollapseTicks + 0.001f)
                {
                    if (Main.netMode != NetmodeID.Server)
                    {
                        ParticlePresets.Implosion(Projectile.Center, 170f * pop,
                            26, new Color(210, 230, 255), 26);
                        OndaLib.Kick(6f, 14);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item70.WithPitchOffset(-0.55f), Projectile.Center);
                }
            }
            else
            {
                // === LA DERIVA LENTA DEL COLOSO ===
                Projectile.velocity *= 0.965f;
                NPC prey = FindNearestEnemy(500f);
                if (prey != null)
                {
                    Vector2 toPrey = prey.Center - Projectile.Center;
                    float len = toPrey.Length();
                    if (len > 110f && len > 0.01f)
                        Projectile.velocity += toPrey / len * 0.5f;
                    if (Projectile.velocity.Length() > 1.8f)
                        Projectile.velocity = Vector2.Normalize(Projectile.velocity) * 1.8f;
                }

                // === EL AURA: cada 10 ticks, el CUERPO entero (75% de
                //     BodyPx ~ 67 px de radio) al 45% + OnFire ===
                if (Main.netMode != NetmodeID.MultiplayerClient &&
                    Age > 30f && Age % 10f == 0f && Projectile.scale > 0.25f)
                {
                    float auraRadius = RedSupergiantRenderer.BodyPx * Projectile.scale * 0.75f;
                    int auraDamage = Math.Max(1, (int)(Projectile.damage * 0.45f));
                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        if (!VFXCore.EsObjetivo(npc)) continue;
                        if ((npc.Center - Projectile.Center).Length() > auraRadius) continue;
                        npc.SimpleStrikeNPC(auraDamage, npc.direction, false, 2f, DamageClass.Magic);
                        npc.AddBuff(BuffID.OnFire, 300);
                    }
                }
            }

            // === LA LUZ: rojo-naranja amplio y no cegador (una gigante es
            //     un horno ABIERTO, no una bomba) ===
            float lightR = 0.55f * Projectile.scale * (1f - Collapse * 0.5f);
            Lighting.AddLight(Projectile.Center, 0.85f * lightR, 0.30f * lightR, 0.10f * lightR);
            // Durante el colapso: el núcleo se enciende BLANCO-AZUL.
            if (Collapse > 0f)
                Lighting.AddLight(Projectile.Center, 0.9f * Collapse, 0.95f * Collapse, 1.1f * Collapse);

            // === VISUAL SOLO CLIENTE: las brasas del horno ===
            if (Main.netMode == NetmodeID.Server) return;
            if (Collapse <= 0f && Age % 8f == 0f && Projectile.scale > 0.3f)
            {
                float ang = Main.rand.NextFloat(0, MathHelper.TwoPi);
                Vector2 rim = Projectile.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) *
                    (RedSupergiantRenderer.BodyPx * Projectile.scale * 0.6f);
                Dust d = Dust.NewDustPerfect(rim, DustID.Torch,
                    new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * Main.rand.NextFloat(0.4f, 1.2f),
                    150, new Color(255, 120, 50), 0.9f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        private NPC FindNearestEnemy(float maxRange)
        {
            NPC best = null;
            float bestDist = maxRange;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;
                float dist = (npc.Center - Projectile.Center).Length();
                if (dist < bestDist) { bestDist = dist; best = npc; }
            }
            return best;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // ============================================================
            //  CONTRATO DE BATCH A PRUEBA DE BALAS (v6.10): durante PreDraw
            //  el batch de tML está ABIERTO; cerrarlo antes del pase propio.
            // ============================================================
            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try
            {
                RedSupergiantRenderer.Draw(Projectile, LifeT, Collapse, Seed);
            }
            catch { try { Main.spriteBatch.End(); } catch { } }

            if (wasActive)
                RestoreSpriteBatch();
            return false;
        }

        /// <summary>Restaura el SpriteBatch con los parámetros EXACTOS del
        /// pase de proyectiles de vanilla (Main.DrawProjectiles).</summary>
        private static void RestoreSpriteBatch()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.Transform);
        }

        public override void OnKill(int timeLeft)
        {
            // ============================================================
            //  LA NOVA SALE DEL COLAPSO: el guión completo — 20 ticks de
            //  implosión (AI/renderer) y AQUÍ el estallido ×1.8 con el
            //  fuego SolarFire de PyraLib (paleta muestreada para el polvo)
            //  y la onda StyleNova de la casa.
            // ============================================================
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int novaDamage = Math.Max(1, (int)(Projectile.damage * 1.8f));
                float novaRadius = 560f;

                // LA ONDA NOVA (el frente de espaciotiempo que LLEVA la nova).
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center.X, Projectile.Center.Y, 0f, 0f,
                    ModContent.ProjectileType<CosmicShockwaveProjectile>(),
                    novaDamage, 0f, Projectile.owner,
                    0f,                                  // sin retardo
                    CosmicShockwaveProjectile.StyleNova,
                    novaRadius);                         // el radio del coloso

                // EL AoE DEL EPICENTRO (el núcleo recién encendido).
                float coreR = 320f;
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!VFXCore.EsObjetivo(npc)) continue;
                    if ((npc.Center - Projectile.Center).Length() > coreR) continue;
                    npc.SimpleStrikeNPC(novaDamage, npc.direction, false,
                        5f, DamageClass.Magic);
                    npc.AddBuff(BuffID.OnFire, 600);
                }
            }

            if (Main.netMode == NetmodeID.Server) return;

            // === EL FUEGO DE LA NOVA (dusts con la paleta SolarFire de
            //     PyraLib — muestreada por temperatura decreciente) ===
            float scale = Math.Max(Projectile.scale, 0.4f);
            ParticlePresets.Explosion(Projectile.Center, 240f * scale, 48,
                new Color(255, 240, 200), new Color(255, 90, 20), 52);
            ParticlePresets.RingPulse(Projectile.Center, 220f * scale,
                new Color(255, 170, 90, 220), 30);
            ParticlePresets.RingPulse(Projectile.Center, 330f * scale,
                new Color(255, 80, 30, 160), 44);

            for (int i = 0; i < 70; i++)
            {
                float angle = (MathHelper.TwoPi / 70) * i + Main.rand.NextFloat(-0.05f, 0.05f);
                Vector2 dir = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                // La temperatura NACE alta y muere (la paleta cuenta el cuento).
                float temp = Main.rand.NextFloat(0.55f, 1f);
                Color fire = PyraPalettes.Sample(PyraPalettes.SolarFire, temp);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch,
                    dir * Main.rand.NextFloat(6f, 15f) * scale,
                    240, fire, 1.7f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
            for (int i = 0; i < 40; i++)
            {
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                Color fire = PyraPalettes.Sample(PyraPalettes.SolarFire,
                    Main.rand.NextFloat(0.35f, 0.8f));
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                    new Vector2(MathF.Cos(angle), MathF.Sin(angle)) *
                    Main.rand.NextFloat(4f, 10f) * scale,
                    230, fire, 1.4f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // === LA SACUDIDA DE LA SUPERNOVA (la mayor de la familia) ===
            try
            {
                Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(
                    Projectile.Center, new Vector2(1f, 0f), 11f, 12, 18, 0.45f,
                    "AethonRedSupergiantNova"));
            }
            catch { }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item45.WithPitchOffset(-0.25f), Projectile.Center);
        }

        /// <summary>Elastic ease-out (curva elástica de aparición).</summary>
        private static float ElasticOut(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            float c = (2f * (float)Math.PI) / 3f;
            return (float)(Math.Pow(2, -10 * t) * Math.Sin((t * 10 - 0.75) * c) + 1);
        }
    }
}
