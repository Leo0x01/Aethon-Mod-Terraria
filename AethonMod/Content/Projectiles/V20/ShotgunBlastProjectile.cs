using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.V20
{
    /// <summary>
    /// ShotgunBlastProjectile — proyectil individual de la descarga en abanico.
    ///
    /// Visuales:
    ///   - Star.png como textura (pequeño)
    ///   - Glow aditivo naranja
    ///
    /// Físicas:
    ///   - Rápido (shootSpeed alto)
    ///   - timeLeft = 30 (corta vida)
    ///   - On hit: genera 8 partículas spark
    /// </summary>
    public class ShotgunBlastProjectile : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.tileCollide = true;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 30;
            Projectile.light = 0f;
            Projectile.alpha = 0;
            Projectile.aiStyle = -1;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 0;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            try
            {
                Projectile.ai[0] += 1f;
                Projectile.rotation += 0.25f;

                // Pequeño trail de dust
                if (Main.rand.NextBool(3))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch,
                        -Projectile.velocity * 0.05f + new Vector2(
                            Main.rand.NextFloat(-0.8f, 0.8f),
                            Main.rand.NextFloat(-0.8f, 0.8f)),
                        150, new Color(255, 200, 80), 0.7f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }

                Lighting.AddLight(Projectile.Center, new Vector3(1f, 0.7f, 0.3f));
            }
            catch { }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SpawnSparks(Projectile.Center);
        }

        public override void Kill(int timeLeft)
        {
            SpawnSparks(Projectile.Center);
        }

        private void SpawnSparks(Vector2 center)
        {
            try
            {
                // 8 partículas spark en direcciones aleatorias
                for (int i = 0; i < 8; i++)
                {
                    float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                    float speed = Main.rand.NextFloat(2f, 6f);
                    Vector2 vel = new Vector2((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed);
                    int dustType = Main.rand.NextBool(2) ? DustID.Torch : DustID.GoldFlame;
                    Dust d = Dust.NewDustPerfect(center, dustType, vel, 200,
                        new Color(255, 180, 60), 1.2f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }
            catch { }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                Texture2D star = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Star").Value;
                Texture2D softGlow = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;

                if (star == null || softGlow == null)
                    return false;

                Vector2 drawPos = Projectile.Center - Main.screenPosition;

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                // Glow base naranja
                Vector2 glowOrigin = new Vector2(softGlow.Width / 2f, softGlow.Height / 2f);
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(255, 160, 60, 140),
                    0f, glowOrigin, 0.5f, SpriteEffects.None, 0f);

                // Star rotando, color naranja-blanco
                Vector2 starOrigin = new Vector2(star.Width / 2f, star.Height / 2f);
                Main.spriteBatch.Draw(star, drawPos, null,
                    new Color(255, 220, 140, 230),
                    Projectile.rotation, starOrigin, 0.7f, SpriteEffects.None, 0f);
                // Segunda capa star más blanca
                Main.spriteBatch.Draw(star, drawPos, null,
                    new Color(255, 255, 220, 180),
                    -Projectile.rotation * 0.7f, starOrigin, 0.45f, SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
            return false;
        }
    }
}
