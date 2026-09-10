using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.Custom
{
    /// <summary>
    /// BlackHoleBolt — proyectil de energía concentrada con:
    /// - Aberración cromática (3 sprites desplazados en rojo/cyan/magenta)
    /// - Ondas expansivas que salen del proyectil (shockwaves)
    /// - Apariencia de agujero negro (vórtice con halo)
    /// - Atracción gravitacional de partículas
    /// </summary>
    public class BlackHoleBolt : ModProjectile
    {
        private float SpinAngle { get => Projectile.ai[0]; set => Projectile.ai[0] = value; }
        private float WaveTimer { get => Projectile.ai[1]; set => Projectile.ai[1] = value; }

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
            Projectile.penetrate = 3;
            Projectile.timeLeft = 180;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            // Spin continuo
            SpinAngle += 0.08f;

            // Wave timer — generar ondas cada 15 frames
            WaveTimer += 1f;
            if (WaveTimer >= 15f)
            {
                WaveTimer = 0f;
                // Spawn de dusts en anillo expansivo (onda)
                for (int i = 0; i < 24; i++)
                {
                    float angle = (MathHelper.TwoPi / 24) * i;
                    Vector2 dir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch,
                        dir * 3f, 150, new Color(180, 80, 255), 0.8f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }

            // Partículas orbitando (accretion disk)
            for (int i = 0; i < 3; i++)
            {
                float angle = SpinAngle * (1f + i * 0.3f) + i * MathHelper.TwoPi / 3f;
                float radius = 18f + i * 6f;
                Vector2 offset = new Vector2((float)Math.Cos(angle) * radius, (float)Math.Sin(angle) * radius);
                Dust d = Dust.NewDustPerfect(Projectile.Center + offset, DustID.PurpleTorch,
                    -offset * 0.05f, 180, new Color(255, 100, 200), 0.7f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Partículas siendo absorbidas desde lejos
            if (Main.rand.NextBool(3))
            {
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(50f, 80f);
                Vector2 spawnPos = Projectile.Center + new Vector2((float)Math.Cos(angle) * dist, (float)Math.Sin(angle) * dist);
                Vector2 vel = (Projectile.Center - spawnPos) * 0.04f;
                Dust d = Dust.NewDustPerfect(spawnPos, DustID.PurpleTorch,
                    vel, 150, new Color(200, 150, 255), 0.6f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Luz púrpura pulsante
            float pulse = 0.8f + 0.2f * (float)Math.Sin(Main.GameUpdateCount * 0.15f);
            Lighting.AddLight(Projectile.Center, new Vector3(0.6f * pulse, 0.2f * pulse, 0.9f * pulse));

            // Homing leve
            NPC target = FindClosestNPC(350f);
            if (target != null)
            {
                Vector2 dir = target.Center - Projectile.Center;
                if (dir != Vector2.Zero)
                {
                    dir.Normalize();
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, dir * 10f, 0.04f);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                float t = Main.GameUpdateCount;
                float pulse = 0.8f + 0.2f * (float)Math.Sin(t * 0.15f);

                // === 1. ABERRACIÓN CROMÁTICA ===
                // 3 sprites del BlackHole desplazados en rojo/cyan/magenta
                Vector2[] offsets = {
                    new Vector2(-2f, 0f),  // rojo (desplazado izquierda)
                    new Vector2(2f, 0f),   // cyan (desplazado derecha)
                    new Vector2(0f, -2f),  // magenta (desplazado arriba)
                };
                Color[] chromaColors = {
                    new Color(255, 0, 0, 100),     // rojo
                    new Color(0, 255, 255, 100),   // cyan
                    new Color(255, 0, 255, 100),   // magenta
                };
                for (int i = 0; i < 3; i++)
                {
                    DrawAdditive("AethonMod/Content/Effects/Custom/BlackHole",
                        Projectile.Center + offsets[i], 0.7f * pulse, chromaColors[i], SpinAngle);
                }

                // === 2. ONDAS EXPANSIVAS (shockwaves) ===
                // 2 anillos concéntricos escalando con el tiempo
                float wave1Scale = 0.3f + ((t * 0.05f) % 1f) * 1.5f;
                float wave1Alpha = (1f - ((t * 0.05f) % 1f)) * 150f;
                DrawAdditive("AethonMod/Content/Effects/Custom/Shockwave",
                    Projectile.Center, wave1Scale, new Color(200, 100, 255, (int)wave1Alpha), 0f);

                float wave2Scale = 0.3f + ((t * 0.05f + 0.5f) % 1f) * 1.5f;
                float wave2Alpha = (1f - ((t * 0.05f + 0.5f) % 1f)) * 120f;
                DrawAdditive("AethonMod/Content/Effects/Custom/Shockwave",
                    Projectile.Center, wave2Scale, new Color(255, 150, 200, (int)wave2Alpha), 0f);

                // === 3. NÚCLEO DEL AGUJERO NEGRO ===
                // GlowOrb púrpura oscuro en el centro
                DrawAdditive("AethonMod/Content/Effects/GlowOrbPurple",
                    Projectile.Center, 0.5f * pulse, new Color(80, 0, 120, 220), 0f);

                // ChromaticAberration rotando
                DrawAdditive("AethonMod/Content/Effects/Custom/ChromaticAberration",
                    Projectile.Center, 0.6f * pulse, new Color(255, 100, 255, 150), SpinAngle);

                // === 4. HALO EXTERNO ===
                DrawAdditive("AethonMod/Content/Effects/GlowCirclePurple",
                    Projectile.Center, 1.0f * pulse, new Color(150, 50, 200, 100), 0f);
            }
            catch { }
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Implosión masiva con aberración cromática
            for (int i = 0; i < 30; i++)
            {
                float angle = (MathHelper.TwoPi / 30) * i;
                float dist = 50f + Main.rand.NextFloat(0, 20f);
                Vector2 spawnPos = target.Center + new Vector2((float)Math.Cos(angle) * dist, (float)Math.Sin(angle) * dist);
                Vector2 vel = (target.Center - spawnPos) * 0.15f;
                Dust d = Dust.NewDustPerfect(spawnPos, DustID.PurpleTorch,
                    vel, 200, new Color(200, 100, 255), 1.0f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Onda expansiva de impacto
            for (int i = 0; i < 24; i++)
            {
                float angle = (MathHelper.TwoPi / 24) * i;
                Vector2 dir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                Dust d = Dust.NewDustPerfect(target.Center, DustID.RainbowTorch,
                    dir * 6f, 200, new Color(255, 100, 255), 1.2f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        private NPC FindClosestNPC(float maxDist)
        {
            NPC closest = null;
            float minDist = maxDist;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy()) continue;
                float dist = Vector2.Distance(Projectile.Center, npc.Center);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = npc;
                }
            }
            return closest;
        }

        private void DrawAdditive(string path, Vector2 pos, float scale, Color color, float rotation)
        {
            try
            {
                Texture2D tex = ModContent.Request<Texture2D>(path).Value;
                if (tex == null) return;
                Vector2 origin = new Vector2(tex.Width / 2f, tex.Height / 2f);
                Vector2 drawPos = pos - Main.screenPosition;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
                Main.spriteBatch.Draw(tex, drawPos, null, color, rotation, origin, scale, SpriteEffects.None, 0f);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
        }
    }
}
