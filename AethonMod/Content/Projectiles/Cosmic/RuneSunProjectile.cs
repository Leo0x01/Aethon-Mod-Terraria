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
    /// RuneSunProjectile — v6.19 — LA FAMILIA DE LOS SOLES RÚNICOS.
    ///
    /// UN solo proyectil parametrizado: `ai[0]` = copia (1..20). La copia
    /// N lleva N ANILLOS RÚNICOS en planos orbitales distintos con GIROS
    /// ALTERNOS, y cada nivel añade una mejora más (ver RuneSunRenderer).
    ///
    /// EL SOL ORIGINAL NO SE TOCA: SunProjectile queda 100% intacto — esta
    /// familia es una construcción nueva que hereda la TÉCNICA visual del
    /// sol (disco SunShader + glow coronal creciente) por re-implementación,
    /// no por dependencia.
    ///
    /// FÍSICA (el patrón probado del sol, calibrado por copia):
    ///   · Vida: 10 s + 0.2 s por copia (la 10 dura 12 s).
    ///   · Pop elástico de aparición + deriva lenta con freno.
    ///   · Persigue suavemente a la presa más cercana (tope 3 px/t).
    ///   · Aura de daño cada 10 ticks (45% del daño, radio creciente).
    ///   · GIGANTE FINAL (últimos 1.5 s): se hincha ×1.5, enrojece y su
    ///     daño sube ×1.5 — la versión compacta del ciclo del sol.
    ///   · ONKill = UNA SOLA NOVA RÚNICA (la lección v5.97): onda nova de
    ///     lente + AoE del núcleo + explosión de runas de eco — TODO en
    ///     una explosión, escalado por copia.
    /// </summary>
    public class RuneSunProjectile : ModProjectile
    {
        /// <summary>Vida base en ticks (10 s) + 12 por copia.</summary>
        private const int BaseLifetime = 600;

        /// <summary>El daño base registrado al nacer (para el ramp de la gigante).</summary>
        private float BaseDamage
        {
            get => Projectile.ai[2];
            set => Projectile.ai[2] = value;
        }

        /// <summary>Edad visual en ticks (solo cliente — animaciones).</summary>
        private float _age;

        /// <summary>Vida total de ESTA copia (ticks) — se fija al nacer.</summary>
        private float _lifetime = BaseLifetime;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 104;
            Projectile.height = 104;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = BaseLifetime;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            int tier = Tier;
            // v6.26 — LA MEJORA MAYOR DE LA VEINTE: +90 ticks de vida (15,3 s
            // totales) para que el GRAN SELLADO tenga tiempo de sonar entero.
            _lifetime = BaseLifetime + 12 * (tier - 1) + (tier >= 20 ? 90 : 0);
            Projectile.timeLeft = (int)_lifetime;
            // ai[1] = semilla determinista del disparo (visual).
            if (Projectile.ai[1] <= 0f)
                Projectile.ai[1] = (Projectile.identity % 9973 + 1) * 1f;
            BaseDamage = Projectile.damage;
        }

        /// <summary>La copia (1..20), clampeada.</summary>
        private int Tier => System.Math.Clamp((int)Projectile.ai[0], 1, RuneSunRenderer.MaxTier);

        /// <summary>Semilla determinista del disparo.</summary>
        private int Seed => Math.Max(1, (int)Projectile.ai[1]) + Projectile.identity;

        /// <summary>0..1 del ciclo de vida (para el glow coronal).</summary>
        private float LifeT => MathHelper.Clamp(_age / Math.Max(_lifetime, 1f), 0f, 1f);

        /// <summary>0..1 de la GIGANTE FINAL (últimos 90 ticks).</summary>
        private float RedGiant
        {
            get
            {
                float t = 1f - Projectile.timeLeft / (float)RuneSunRenderer.RedGiantTicks;
                return MathHelper.Clamp(t, 0f, 1f);
            }
        }

        public override void AI()
        {
            _age += 1f;

            // === POP ELÁSTICO DE APARICIÓN ===
            // (v6.26 — LA VEINTE nace GRANDE: sistema ×1.30 — el GRAN
            // SELLADO merece un cielo entero para él solo.)
            Projectile.scale = ElasticOut(Utils.GetLerpValue(0f, 90f, _age, true)) *
                               (float)Math.Sqrt(Utils.GetLerpValue(0f, 45f, _age, true)) *
                               (Tier >= 20 ? 1.30f : 1f);

            // === LA GIGANTE FINAL: se hincha ×1.5 y el daño sube ×1.5
            // (v6.22: la copia 20 — EL GRAN SELLADO — se hincha ×1.75) ===
            float rg = RedGiant;
            if (rg > 0f)
            {
                float ease = rg * rg * (3f - 2f * rg);
                float swell = Tier >= 20 ? 0.75f : 0.50f;
                Projectile.scale *= 1f + swell * ease;

                float baseDmg = BaseDamage > 0f ? BaseDamage : Projectile.damage;
                BaseDamage = baseDmg;
                int newDmg = Math.Max(1, (int)(baseDmg * (1f + swell * rg)));
                if (newDmg != Projectile.damage)
                    Projectile.damage = newDmg;

                int sz = Math.Max(16, (int)(104f * Projectile.scale));   // v6.31: 92→104 (BodyPx 46→52)
                if (sz != Projectile.width)
                    Projectile.Resize(sz, sz);
            }
            else if (BaseDamage <= 0f)
            {
                BaseDamage = Projectile.damage;
            }

            // === EL AURA DE DAÑO (cada 10 ticks — 45% del daño;
            //     LA VEINTE ARDE MÁS: 55% — la Mejora Mayor) — v6.50 —
            //     GolpeMotor: el cauce del motor, resuelto en el cliente
            //     dueño; la quemadura sigue siendo autoridad. ===
            if (_age > 30f && _age % 10f == 0f && Projectile.scale > 0.25f)
            {
                float auraRadius = RuneSunRenderer.BodyPx * Projectile.scale *
                                   (1.75f + 0.55f * LifeT);
                // Las copias altas tienen anillos más anchos: el aura crece
                // con la copia (la 10 abraza su 3er anillo).
                auraRadius *= 1f + 0.06f * (Tier - 1);
                int auraDamage = Math.Max(1, (int)(Projectile.damage * (Tier >= 20 ? 0.55f : 0.45f)));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!VFXCore.EsObjetivo(npc)) continue;
                    float dist = (npc.Center - Projectile.Center).Length();
                    if (dist > auraRadius) continue;
                    Content.Systems.GolpeMotor.Golpear(Projectile, npc, auraDamage, 2f, true);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        npc.AddBuff(BuffID.OnFire, rg > 0f ? 600 : 300);
                }
            }

            // === MOVIMIENTO: deriva lenta + persecución suave ===
            Projectile.velocity *= 0.97f;

            NPC prey = FindNearestEnemy(560f);
            if (prey != null)
            {
                Vector2 toPrey = prey.Center - Projectile.Center;
                float len = toPrey.Length();
                if (len > 60f && len > 0.01f)
                    Projectile.velocity += toPrey / len * 0.9f;
                if (Projectile.velocity.Length() > 3f)
                    Projectile.velocity = Vector2.Normalize(Projectile.velocity) * 3f;
            }

            // === LUZ DEL SOL (cálida, crece con la gigante) ===
            float lightR = (0.85f + 0.55f * rg) * Projectile.scale;
            Lighting.AddLight(Projectile.Center, 0.9f * lightR, 0.62f * lightR, 0.24f * lightR);
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

        /// <summary>v6.30 — TODO SOL QUEMA (petición del usuario): el golpe de
        /// CONTACTO del sol también prende — OnFire 10 s en el cuerpo del sol.</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // MP-seguro: AddBuff corre en quien tenga autoridad sobre el NPC.
            try { target.AddBuff(BuffID.OnFire, 600); } catch { }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // ============================================================
            //  CONTRATO DE BATCH A PRUEBA DE BALAS (v6.10 — la lección de
            //  los agujeros): durante PreDraw el batch de tML está ABIERTO;
            //  hay que CERRARLO antes de que el renderer llame a Begin()
            //  con sus propios estados. Sin esto: InvalidOperationException
            //  "Begin has been called before calling End" → el sol NUNCA
            //  se pinta (proyectil invisible) — bug v6.19 corregido.
            // ============================================================
            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try
            {
                RuneSunRenderer.Draw(Projectile, Tier, LifeT, RedGiant, Seed);
            }
            catch { try { Main.spriteBatch.End(); } catch { } }

            if (wasActive)
                RestoreSpriteBatch();
            return false;
        }

        /// <summary>Restaura el SpriteBatch con los parámetros EXACTOS del
        /// pase de proyectiles de vanilla (Main.DrawProjectiles) — el mismo
        /// contrato validado de la familia de agujeros negros.</summary>
        private static void RestoreSpriteBatch()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.Transform);
        }

        public override void OnKill(int timeLeft)
        {
            int tier = Tier;

            // ============================================================
            //  LA NOVA RÚNICA — UNA SOLA EXPLOSIÓN (la lección v5.97):
            //  una onda nova (StyleNova, la de la familia del sol) + AoE
            //  del núcleo + la RÁFAGA DE RUNAS DE ECO — escalado por copia.
            //  v6.50 — GolpeMotor: el AoE del núcleo va por el cauce del
            //  motor (cliente dueño); la onda y la quemadura siguen
            //  siendo autoridad.
            // ============================================================
            // v6.26 — LA NOVA DEL SELLADO: la VEINTE revienta ×1.6 con
            // el radio más grande del arma alguna (766 px).
            bool supremo = tier >= 20;
            int novaDamage = Math.Max(1, (int)(Projectile.damage * (supremo ? 1.6f : 1.25f)));
            float novaRadius = 380f + 14f * (tier - 1) + (supremo ? 120f : 0f);

            // v6.50.2 — FIX (nova fantasma en MP): el gate netMode dejaba
            // el spawn en el SERVER con owner=índice de jugador ≠ myPlayer(255)
            // → NewProjectile JAMÁS lo difundía (solo difunde owner==myPlayer,
            // verificado en el IL) y el cliente dueño tampoco lo engendraba:
            // visual Y daño de la nova perdidos para cualquier jugador no-host.
            // El patrón de los agujeros (BlackHoleProjectile): lo engendra la
            // máquina DUEÑA y el propio NewProjectile sincroniza.
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center.X, Projectile.Center.Y, 0f, 0f,
                    ModContent.ProjectileType<CosmicShockwaveProjectile>(),
                    novaDamage, 0f, Projectile.owner,
                    0f,                                  // sin retardo — TODO sucede YA
                    CosmicShockwaveProjectile.StyleNova,
                    novaRadius);                         // radio escalado por copia
            }

            // AoE del núcleo (el epicentro de la MISMA explosión).
            float coreR = 260f + 12f * (tier - 1) + (supremo ? 90f : 0f);
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;
                float dist = (npc.Center - Projectile.Center).Length();
                if (dist < coreR)
                {
                    Content.Systems.GolpeMotor.Golpear(Projectile, npc, novaDamage,
                        Projectile.knockBack, true);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        npc.AddBuff(BuffID.OnFire, 600);
                }
            }

            if (Main.netMode == NetmodeID.Server) return;

            // === LA EXPLOSIÓN DE LA LIBRERÍA DE PARTÍCULAS ===
            float scale = Math.Max(Projectile.scale, 0.4f);
            ParticlePresets.Explosion(Projectile.Center, 170f * scale, 40,
                new Color(255, 245, 200), new Color(255, 90, 20), 50);
            ParticlePresets.RingPulse(Projectile.Center, 200f * scale,
                new Color(255, 210, 100, 210), 34);
            ParticlePresets.RingPulse(Projectile.Center, 270f * scale,
                new Color(255, 80, 30, 150), 46);

            // === LAS RUNAS DE ECO: la nova escupe glifos de luz ===
            int echoRunes = 12 + 2 * tier;
            for (int i = 0; i < echoRunes; i++)
            {
                float angle = (MathHelper.TwoPi / echoRunes) * i +
                              Main.rand.NextFloat(-0.08f, 0.08f);
                Vector2 outward = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Gold,
                    outward * Main.rand.NextFloat(5f, 11f) * scale,
                    255, new Color(255, 235, 170), 1.5f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // === EL FUEGO DE LA NOVA (dusts del patrón del sol) ===
            for (int i = 0; i < 60; i++)
            {
                float angle = (MathHelper.TwoPi / 60) * i;
                Vector2 dir = new Vector2(
                    (float)Math.Cos(angle) * Main.rand.NextFloat(6f, 14f),
                    (float)Math.Sin(angle) * Main.rand.NextFloat(6f, 14f)) * scale;
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                    dir, 240, new Color(255, 200, 100), 1.7f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
            for (int i = 0; i < 35; i++)
            {
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch,
                    new Vector2((float)Math.Cos(angle) * Main.rand.NextFloat(4f, 9f),
                                (float)Math.Sin(angle) * Main.rand.NextFloat(4f, 9f)) * scale,
                    200, new Color(255, 150, 50), 1.4f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // === Screenshake coordinado (LA VEINTE sacude más: 12) ===
            try
            {
                Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(
                    Projectile.Center, new Vector2(1f, 0f), tier >= 20 ? 12f : 8f, 12, 18, 0.45f,
                    "AethonRuneSunNova"));
            }
            catch { }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item45, Projectile.Center);
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
