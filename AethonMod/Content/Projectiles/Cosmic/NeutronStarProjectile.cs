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
    /// NeutronStarProjectile — v6.26 — LA ESTRELLA DE NEUTRONES.
    ///
    /// El cadáver ultradenso: daño ENORME en radio MINÚSCULO.
    ///
    /// FÍSICA:
    ///   · Vida CORTA: 4 s (240 ticks) — la estrella de neutrones no
    ///     dura: su furia se gasta.
    ///   · GRAVITA a los enemigos: atracción FUERTE (la masa de un sol
    ///     en 12 px curva el espacio) — tope 4.5 px/t.
    ///   · AURA: cada 10 ticks, radio MINÚSCULO (60 px) con daño ×3 el
    ///     aura estándar de la casa (45% → 135% del daño).
    ///   · STARQUAKE: cada ~2 s la corteza se ROMPE (18 ticks de
    ///     temblor): sacudida OndaLib.Kick + anillo de onda en el
    ///     renderer + pulso de daño extra en el epicentro.
    ///
    /// Daño MP-seguro: SimpleStrikeNPC con Main.netMode != MultiplayerClient.
    /// Visual solo cliente (Main.netMode == Server → return).
    /// </summary>
    public class NeutronStarProjectile : ModProjectile
    {
        /// <summary>Vida total en ticks (4 s — corta).</summary>
        private const int Lifetime = 240;

        /// <summary>Radio del aura de daño (MINÚSCULO).</summary>
        private const float AuraRadius = 60f;

        /// <summary>Duración del starquake (ticks de temblor).</summary>
        private const int QuakeTicks = 18;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 34;
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

        /// <summary>Edad en ticks (ai[0] — se sincroniza: server y clientes calculan igual).</summary>
        private float Age => Projectile.ai[0];

        /// <summary>Semilla determinista del disparo.</summary>
        private int Seed => Math.Max(1, (int)Projectile.ai[1]) + Projectile.identity;

        /// <summary>0..1 del ciclo de vida.</summary>
        private float LifeT => MathHelper.Clamp(Age / Lifetime, 0f, 1f);

        /// <summary>0..1 del starquake activo (0 = ninguno) — DETERMINISTA:
        /// la fase se deriva de Age (ai[0] sincronizado) y la semilla, así
        /// server y clientes ven EL MISMO temblor sin estado extra.</summary>
        private int QuakeCycle => QuakeTicks + 118 + (Seed % 40);

        /// <summary>Fase actual dentro del ciclo sísmico (0..ciclo).</summary>
        private float QuakePhase => Age % QuakeCycle;

        /// <summary>0..1 del progreso del starquake activo.</summary>
        private float QuakeT
        {
            get
            {
                float ph = QuakePhase;
                return ph < QuakeTicks ? ph / QuakeTicks : 0f;
            }
        }

        public override void AI()
        {
            Projectile.ai[0] += 1f;

            // === EL POP ELÁSTICO DE APARICIÓN (nace y se planta) ===
            Projectile.scale = ElasticOut(Utils.GetLerpValue(0f, 45f, Age, true));

            // === LA GRAVEDAD DE UN SOL EN 12 PX: atracción FUERTE ===
            Projectile.velocity *= 0.97f;
            NPC prey = FindNearestEnemy(700f);
            if (prey != null)
            {
                Vector2 toPrey = prey.Center - Projectile.Center;
                float len = toPrey.Length();
                if (len > 14f && len > 0.01f)
                    Projectile.velocity += toPrey / len * 1.5f;
                if (Projectile.velocity.Length() > 4.5f)
                    Projectile.velocity = Vector2.Normalize(Projectile.velocity) * 4.5f;
            }

            // === EL AURA ULTRADENSA: daño ×3 el estándar en 60 px ===
            if (Main.netMode != NetmodeID.MultiplayerClient &&
                Age > 20f && Age % 10f == 0f && Projectile.scale > 0.25f)
            {
                int auraDamage = Math.Max(1, (int)(Projectile.damage * 1.35f));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!VFXCore.EsObjetivo(npc)) continue;
                    if ((npc.Center - Projectile.Center).Length() > AuraRadius) continue;
                    npc.SimpleStrikeNPC(auraDamage, npc.direction, false, 1f, DamageClass.Magic);
                    try { npc.AddBuff(BuffID.OnFire, 240); } catch { }   // v6.30: TODO SOL QUEMA — el plasma ultracaliente
                    try { npc.AddBuff(BuffID.CursedInferno, 90); } catch { }
                }
            }

            // === EL STARQUAKE: la corteza se rompe cada ~2 s (DETERMINISTA:
            //     la fase vive en Age, no en estado local) ===
            if (Main.netMode != NetmodeID.MultiplayerClient && Age > 60f)
            {
                float ph = QuakePhase;
                if (ph < 1f)
                {
                    // EL MOMENTO EXACTO de la rotura: pulso de daño en el
                    // epicentro + sacudida + trueno de corteza.
                    int quakeDamage = Math.Max(1, (int)(Projectile.damage * 0.8f));
                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        if (!VFXCore.EsObjetivo(npc)) continue;
                        if ((npc.Center - Projectile.Center).Length() > 100f) continue;
                        npc.SimpleStrikeNPC(quakeDamage, npc.direction, false, 2f, DamageClass.Magic);
                    try { npc.AddBuff(BuffID.OnFire, 240); } catch { }   // v6.30: TODO SOL QUEMA — el plasma ultracaliente
                    }
                    if (Main.netMode != NetmodeID.Server)
                    {
                        OndaLib.Kick(4f, 8);
                        Terraria.Audio.SoundEngine.PlaySound(
                            SoundID.Item70.WithPitchOffset(0.45f), Projectile.Center);
                    }
                }
            }

            // === LA LUZ: blanco-azul cegador ===
            float lightR = 1.05f * Projectile.scale;
            Lighting.AddLight(Projectile.Center, 0.95f * lightR, 0.98f * lightR, 1.10f * lightR);

            // === VISUAL SOLO CLIENTE: motas de la superficie ===
            if (Main.netMode == NetmodeID.Server) return;
            if (Age % 7f == 0f && Projectile.scale > 0.3f)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch,
                    new Vector2(Main.rand.NextFloat(-1.6f, 1.6f), Main.rand.NextFloat(-1.6f, 1.6f)),
                    180, new Color(190, 220, 255), 0.7f);
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
                NeutronStarRenderer.Draw(Projectile, LifeT, QuakeT, Seed);
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
            // === EL DAÑO FINAL (MP-seguro) ===
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int burst = Math.Max(1, (int)(Projectile.damage * 1.0f));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!VFXCore.EsObjetivo(npc)) continue;
                    if ((npc.Center - Projectile.Center).Length() > 120f) continue;
                    npc.SimpleStrikeNPC(burst, npc.direction, false, 2f, DamageClass.Magic);
                    try { npc.AddBuff(BuffID.OnFire, 240); } catch { }   // v6.30: TODO SOL QUEMA — el plasma ultracaliente
                }
            }

            if (Main.netMode == NetmodeID.Server) return;

            // === EL ESTALLIDO FINAL: la estrella se apaga con un latido ===
            float scale = Math.Max(Projectile.scale, 0.4f);
            ParticlePresets.RingPulse(Projectile.Center, 90f * scale,
                new Color(190, 220, 255, 220), 22);
            ParticlePresets.Explosion(Projectile.Center, 70f * scale, 22,
                new Color(240, 246, 255), new Color(120, 170, 255), 34);

            for (int i = 0; i < 26; i++)
            {
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch,
                    new Vector2(MathF.Cos(angle), MathF.Sin(angle)) *
                    Main.rand.NextFloat(4f, 10f) * scale,
                    230, new Color(200, 225, 255), 1.1f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            try
            {
                Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(
                    Projectile.Center, new Vector2(1f, 0f), 5f, 10, 14, 0.4f,
                    "AethonNeutronStarDeath"));
            }
            catch { }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item70, Projectile.Center);
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
