using System;
using System.Collections.Generic;
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
    /// MagnetarProjectile — v6.26 — EL MAGNETAR.
    ///
    /// La estrella de neutrones EXTREMA: el arma más potente de la familia
    /// de las estrellas reales (daño 320).
    ///
    /// FÍSICA:
    ///   · AURA MAGNÉTICA: cada 10 ticks, 140 px al 45% (el campo aplasta).
    ///   · CADENAS DE RAYO AUTOMÁTICAS: cada 20 ticks, hasta 3 enemigos
    ///     cercanos (≤240 px) reciben un rayo de cadenas (50% del daño +
    ///     Electrified) — el mismo criterio que dibuja el renderer.
    ///   · PERSECUCIÓN firme (el magnetar ACECHA — no deriva).
    ///   · Vida 5 s (extrema y corta).
    ///
    /// Daño MP-seguro: SimpleStrikeNPC con Main.netMode != MultiplayerClient.
    /// Visual solo cliente (Main.netMode == Server → return).
    /// </summary>
    public class MagnetarProjectile : ModProjectile
    {
        /// <summary>Vida total en ticks (5 s — la extrema se gasta rápido).</summary>
        private const int Lifetime = 300;

        /// <summary>Radio del aura magnética de daño.</summary>
        private const float AuraRadius = 140f;

        /// <summary>Enemigos máximos por andanada de cadenas.</summary>
        private const int ChainTargets = 3;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
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

        /// <summary>Semilla determinista del disparo.</summary>
        private int Seed => Math.Max(1, (int)Projectile.ai[1]) + Projectile.identity;

        /// <summary>0..1 del ciclo de vida.</summary>
        private float LifeT => MathHelper.Clamp(Age / Lifetime, 0f, 1f);

        public override void AI()
        {
            Projectile.ai[0] += 1f;

            // === EL POP ELÁSTICO DE APARICIÓN ===
            Projectile.scale = ElasticOut(Utils.GetLerpValue(0f, 45f, Age, true));

            // === EL MAGNETAR ACECHA: persecución firme ===
            Projectile.velocity *= 0.96f;
            NPC prey = FindNearestEnemy(600f);
            if (prey != null)
            {
                Vector2 toPrey = prey.Center - Projectile.Center;
                float len = toPrey.Length();
                if (len > 30f && len > 0.01f)
                    Projectile.velocity += toPrey / len * 0.8f;
                if (Projectile.velocity.Length() > 3f)
                    Projectile.velocity = Vector2.Normalize(Projectile.velocity) * 3f;
            }

            // === EL AURA MAGNÉTICA (140 px, cada 10 ticks) ===
            if (Main.netMode != NetmodeID.MultiplayerClient &&
                Age > 20f && Age % 10f == 0f && Projectile.scale > 0.25f)
            {
                int auraDamage = Math.Max(1, (int)(Projectile.damage * 0.45f));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!VFXCore.EsObjetivo(npc)) continue;
                    if ((npc.Center - Projectile.Center).Length() > AuraRadius) continue;
                    npc.SimpleStrikeNPC(auraDamage, npc.direction, false, 1f, DamageClass.Magic);
                    try { npc.AddBuff(BuffID.Electrified, 150); } catch { }   // v6.30: el campo magnético ELECTRIFICA
                }
            }

            // === LAS CADENAS DE RAYO AUTOMÁTICAS (cada 20 ticks): hasta 3
            //     enemigos en 240 px — 50% del daño + Electrified ===
            if (Main.netMode != NetmodeID.MultiplayerClient &&
                Age > 20f && Age % MagnetarRenderer.ChainPeriod == 0f && Projectile.scale > 0.25f)
            {
                int chainDamage = Math.Max(1, (int)(Projectile.damage * 0.5f));
                List<NPC> targets = FindNearestEnemies(MagnetarRenderer.ChainRange, ChainTargets);
                foreach (NPC npc in targets)
                {
                    npc.SimpleStrikeNPC(chainDamage, npc.direction, false, 2f, DamageClass.Magic);
                    try { npc.AddBuff(BuffID.Electrified, 150); } catch { }
                }

                // El TRUENO de la andanada (solo donde se oye).
                if (Main.netMode != NetmodeID.Server && targets.Count > 0)
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item93.WithPitchOffset(0.25f), Projectile.Center);
            }

            // === LA LUZ: violeta con ARRTIMIA (el latido irregular) ===
            float arrhythmia = MathF.Sin(Main.GlobalTimeWrappedHourly * 6.3f +
                MathF.Sin(Main.GlobalTimeWrappedHourly * 2.17f + Seed * 0.7f) * 2.4f + Seed);
            float pulse = 0.55f + 0.45f * arrhythmia;
            float lightR = (0.45f + 0.55f * pulse) * Projectile.scale;
            Lighting.AddLight(Projectile.Center, 0.72f * lightR, 0.45f * lightR, 1.15f * lightR);

            // === VISUAL SOLO CLIENTE: chispas del campo ===
            if (Main.netMode == NetmodeID.Server) return;
            if (Age % 5f == 0f && Projectile.scale > 0.3f)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch,
                    new Vector2(Main.rand.NextFloat(-2.2f, 2.2f), Main.rand.NextFloat(-2.2f, 2.2f)),
                    160, new Color(200, 150, 255), 0.7f);
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

        /// <summary>Los N enemigos más cercanos en rango (para las cadenas).</summary>
        private List<NPC> FindNearestEnemies(float maxRange, int count)
        {
            var list = new List<NPC>();
            var dists = new List<float>();
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;
                float dist = (npc.Center - Projectile.Center).Length();
                if (dist > maxRange) continue;
                // inserción ordenada (N pequeño: burbuja fina, cero GC).
                int i = list.Count;
                while (i > 0 && dists[i - 1] > dist) i--;
                list.Insert(i, npc);
                dists.Insert(i, dist);
                if (list.Count > count) { list.RemoveAt(count); dists.RemoveAt(count); }
            }
            return list;
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
                MagnetarRenderer.Draw(Projectile, LifeT, Seed);
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
            // === EL DAÑO FINAL (MP-seguro): el CAMPO se suelta entero ===
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int burst = Math.Max(1, (int)(Projectile.damage * 1.0f));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!VFXCore.EsObjetivo(npc)) continue;
                    if ((npc.Center - Projectile.Center).Length() > 180f) continue;
                    npc.SimpleStrikeNPC(burst, npc.direction, false, 3f, DamageClass.Magic);
                    try { npc.AddBuff(BuffID.Electrified, 180); } catch { }
                }
            }

            if (Main.netMode == NetmodeID.Server) return;

            // === LA DESCARGA FINAL: el campo colapsa en violeta ===
            float scale = Math.Max(Projectile.scale, 0.4f);
            ParticlePresets.RingPulse(Projectile.Center, 150f * scale,
                new Color(200, 150, 255, 230), 26);
            ParticlePresets.RingPulse(Projectile.Center, 220f * scale,
                new Color(140, 90, 220, 170), 40);
            ParticlePresets.Explosion(Projectile.Center, 110f * scale, 30,
                new Color(255, 250, 255), new Color(140, 90, 220), 40);

            for (int i = 0; i < 34; i++)
            {
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch,
                    new Vector2(MathF.Cos(angle), MathF.Sin(angle)) *
                    Main.rand.NextFloat(4f, 11f) * scale,
                    220, new Color(210, 160, 255), 1.2f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            try
            {
                Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(
                    Projectile.Center, new Vector2(1f, 0f), 7f, 10, 16, 0.4f,
                    "AethonMagnetarDeath"));
            }
            catch { }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item93, Projectile.Center);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122.WithPitchOffset(-0.3f), Projectile.Center);
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
