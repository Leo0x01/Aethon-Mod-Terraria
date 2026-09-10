using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.V20
{
    /// <summary>
    /// SolarFlareProjectile — ráfaga radial de luz solar.
    ///
    /// Visual:
    ///   - 6 rayos BeamGold a 60° rotando lentamente
    ///   - GlowCircleGold central pulsante (3 capas additive)
    ///   - Dust GoldFlame hacia afuera
    ///
    /// Físicas:
    ///   - velocity = 0 (no se mueve)
    ///   - timeLeft = 60, penetrate = -1
    /// </summary>
    public class SolarFlareProjectile : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 48;
            Projectile.height = 48;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
            Projectile.light = 0f;
            Projectile.alpha = 0;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
            Projectile.ignoreWater = true;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            try
            {
                Projectile.velocity = Vector2.Zero;
                Projectile.ai[0] += 1f;

                // === Dust GoldFlame hacia afuera ===
                if (Main.rand.NextBool(2))
                {
                    float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                    float speed = Main.rand.NextFloat(3f, 7f);
                    Vector2 vel = new Vector2((float)Math.Cos(angle) * speed,
                                              (float)Math.Sin(angle) * speed);
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, vel, 200,
                        new Color(255, 180, 60), 1.3f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }

                // === Lighting ===
                Lighting.AddLight(Projectile.Center, new Vector3(1.0f, 0.6f, 0.1f));
            }
            catch { }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                Texture2D beam = ModContent.Request<Texture2D>("AethonMod/Content/Effects/BeamGold").Value;
                Texture2D glowCircle = ModContent.Request<Texture2D>("AethonMod/Content/Effects/GlowCircleGold").Value;
                if (beam == null || glowCircle == null) return false;

                Vector2 drawPos = Projectile.Center - Main.screenPosition;
                // Beam origin en el borde izquierdo-centro para que crezca desde el centro hacia afuera
                Vector2 beamOrigin = new Vector2(0f, beam.Height / 2f);
                Vector2 glowOrigin = new Vector2(glowCircle.Width / 2f, glowCircle.Height / 2f);

                float t = Projectile.ai[0];
                float baseRot = t * 0.04f; // rotación lenta
                float pulse = 0.7f + 0.3f * (float)Math.Sin(t * 0.2f);
                float alpha = MathHelper.Clamp(Projectile.timeLeft / 60f, 0f, 1f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                // === 6 rayos a 60° ===
                for (int i = 0; i < 6; i++)
                {
                    float angle = baseRot + (MathHelper.TwoPi / 6f) * i;
                    Main.spriteBatch.Draw(beam, drawPos, null,
                        new Color(255, 200, 80, 220) * alpha * pulse,
                        angle, beamOrigin, new Vector2(3f, 1f), SpriteEffects.None, 0f);
                }

                // === GlowCircle central pulsante ===
                Main.spriteBatch.Draw(glowCircle, drawPos, null,
                    new Color(255, 220, 120, 255) * alpha * pulse,
                    0f, glowOrigin, 1.5f + pulse * 0.5f, SpriteEffects.None, 0f);

                // Núcleo brillante interior
                Main.spriteBatch.Draw(glowCircle, drawPos, null,
                    new Color(255, 255, 220, 200) * alpha * pulse,
                    0f, glowOrigin, 0.8f, SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
            return false;
        }
    }
}
