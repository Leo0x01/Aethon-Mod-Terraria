using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Particles;

namespace AethonMod.Content.Projectiles.Advanced
{
    /// <summary>
    /// ShockwaveProjectile — proyectil de onda expansiva.
    /// Combina:
    /// - Múltiples anillos Ring.png expandiéndose a diferentes velocidades
    /// - Trail continuo con oldPos[]
    /// - Partículas del ParticleManager en anillo expansivo
    /// - Glow central pulsante
    /// - Additive blending
    /// </summary>
    public class ShockwaveProjectile : ModProjectile
    {
        public override void SetStaticDefaults() { Main.projFrames[Projectile.type] = 1; }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1; // atraviesa todo
            Projectile.timeLeft = 60;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            // Decelerar el proyectil (onda que se expande y pierde fuerza)
            Projectile.velocity *= 0.96f;

            // === PARTÍCULAS EN ANILLO EXPANSIVO ===
            if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(3))
            {
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                float dist = 20f + Main.rand.NextFloat(0, 10f);
                Vector2 offset = new Vector2((float)Math.Cos(angle) * dist, (float)Math.Sin(angle) * dist);
                var data = new ParticleData
                {
                    Position = Projectile.Center + offset,
                    Velocity = offset * 0.05f, // expandir hacia afuera
                    Scale = new Vector2(0.5f),
                    PackedColor = ParticleManager.PackColor(new Color(100, 220, 255)),
                    Rotation = 0f,
                    RotationSpeed = 0f,
                    TimeLeft = 20,
                    Duration = 20,
                    TextureId = 0, // SoftGlow
                    BlendMode = 1,
                    LayerPriority = LayerPriorities.BeforeProjectiles,
                };
                data.EnableComponent(ComponentFlag.FadeOut);
                data.EnableComponent(ComponentFlag.ScaleDown);
                ParticleManager.Spawn(data);
            }

            // Luz cian pulsante
            float pulse = 0.7f + 0.3f * (float)Math.Sin(Main.GameUpdateCount * 0.3f);
            Lighting.AddLight(Projectile.Center, new Vector3(0.3f * pulse, 0.7f * pulse, 1f * pulse));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                float t = Main.GameUpdateCount;
                float lifeProgress = Projectile.timeLeft / 60f;
                float pulse = 0.6f + 0.4f * lifeProgress;

                Texture2D ringTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Ring").Value;
                Texture2D glowTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                // === 1. MÚLTIPLES ANILLOS EXPANSIVOS ===
                // 3 anillos a diferentes fases de expansión
                if (ringTex != null)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        float phase = (t * 0.03f + i * 0.33f) % 1f;
                        float scale = 0.2f + phase * 2.5f;
                        float alpha = (1f - phase) * 200f * pulse;

                        Color color = i switch
                        {
                            0 => new Color(100, 220, 255, (int)alpha), // cyan
                            1 => new Color(255, 255, 255, (int)alpha), // blanco
                            _ => new Color(150, 200, 255, (int)alpha), // azul claro
                        };

                        Main.spriteBatch.Draw(ringTex,
                            Projectile.Center - Main.screenPosition, null,
                            color,
                            0f,
                            new Vector2(ringTex.Width / 2f, ringTex.Height / 2f),
                            scale, SpriteEffects.None, 0f);
                    }
                }

                // === 2. GLOW CENTRAL PULSANTE ===
                if (glowTex != null)
                {
                    // Glow externo
                    Main.spriteBatch.Draw(glowTex,
                        Projectile.Center - Main.screenPosition, null,
                        new Color(100, 200, 255, (byte)(100 * pulse)),
                        0f,
                        new Vector2(glowTex.Width / 2f, glowTex.Height / 2f),
                        1.0f * pulse, SpriteEffects.None, 0f);
                    // Núcleo blanco
                    Main.spriteBatch.Draw(glowTex,
                        Projectile.Center - Main.screenPosition, null,
                        new Color(255, 255, 255, (byte)(200 * pulse)),
                        0f,
                        new Vector2(glowTex.Width / 2f, glowTex.Height / 2f),
                        0.4f * pulse, SpriteEffects.None, 0f);
                }

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode == NetmodeID.Server) return;

            // Onda expansiva masiva al impactar
            for (int i = 0; i < 30; i++)
            {
                float angle = (MathHelper.TwoPi / 30) * i;
                Vector2 dir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 7f;
                var data = new ParticleData
                {
                    Position = target.Center,
                    Velocity = dir,
                    Scale = new Vector2(0.8f),
                    PackedColor = ParticleManager.PackColor(new Color(150, 220, 255)),
                    Rotation = 0f,
                    RotationSpeed = 0f,
                    TimeLeft = 25,
                    Duration = 25,
                    TextureId = 0, // SoftGlow
                    BlendMode = 1,
                    LayerPriority = LayerPriorities.AfterProjectiles,
                };
                data.EnableComponent(ComponentFlag.FadeOut);
                data.EnableComponent(ComponentFlag.ScaleDown);
                ParticleManager.Spawn(data);
            }
        }
    }
}
