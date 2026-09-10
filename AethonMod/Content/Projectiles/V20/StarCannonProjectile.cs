using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.V20
{
    /// <summary>
    /// StarCannonProjectile — estrella cayendo del cielo.
    ///
    /// Visual:
    ///   - Star.png rotando rápido
    ///   - Trail con oldPos (TrailCacheLength = 10) usando Trail.png
    ///
    /// Físicas:
    ///   - Tiene gravedad (velocity.Y += 0.3f cada frame)
    ///   - tileCollide = true
    ///   - On hit: genera 10 star sparks (DustID.Enchanted_Gold)
    /// </summary>
    public class StarCannonProjectile : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.tileCollide = true;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 240;
            Projectile.light = 0.5f;
            Projectile.alpha = 0;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.ignoreWater = true;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            try
            {
                Projectile.ai[0] += 1f;

                // === Gravedad ===
                Projectile.velocity.Y += 0.3f;
                if (Projectile.velocity.Y > 16f) Projectile.velocity.Y = 16f;

                // === Rotación rápida ===
                Projectile.rotation += 0.4f;

                // === Lighting ===
                Lighting.AddLight(Projectile.Center, new Vector3(1.0f, 0.9f, 0.5f));
            }
            catch { }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SpawnStarSparks(Projectile.Center);
        }

        public override void Kill(int timeLeft)
        {
            SpawnStarSparks(Projectile.Center);
        }

        private void SpawnStarSparks(Vector2 center)
        {
            try
            {
                for (int i = 0; i < 10; i++)
                {
                    float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                    float speed = Main.rand.NextFloat(3f, 7f);
                    Vector2 vel = new Vector2((float)Math.Cos(angle) * speed,
                                              (float)Math.Sin(angle) * speed);
                    Dust d = Dust.NewDustPerfect(center, DustID.Enchanted_Gold, vel, 200,
                        new Color(255, 230, 120), 1.2f);
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
                Texture2D trail = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Trail").Value;
                if (star == null || trail == null) return false;

                Vector2 drawPos = Projectile.Center - Main.screenPosition;
                Vector2 starOrigin = new Vector2(star.Width / 2f, star.Height / 2f);
                Vector2 trailOrigin = new Vector2(trail.Width / 2f, trail.Height / 2f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                // === Trail con oldPos ===
                for (int i = 0; i < Projectile.oldPos.Length; i++)
                {
                    if (Projectile.oldPos[i] == Vector2.Zero) continue;
                    Vector2 oldDraw = Projectile.oldPos[i] - Main.screenPosition + Projectile.Size / 2f;
                    float t = i / (float)Projectile.oldPos.Length;
                    float alpha = (1f - t) * 0.6f;
                    float scale = (1f - t) * 1.2f;
                    float trailRot = Projectile.velocity.LengthSquared() > 0.01f
                        ? Projectile.velocity.ToRotation()
                        : 0f;
                    Main.spriteBatch.Draw(trail, oldDraw, null,
                        new Color(255, 230, 120, 200) * alpha,
                        trailRot, trailOrigin,
                        new Vector2(scale, scale * 2f), SpriteEffects.None, 0f);
                }

                // === Estrella principal ===
                Main.spriteBatch.Draw(star, drawPos, null,
                    new Color(255, 240, 180, 255),
                    Projectile.rotation, starOrigin, 1.5f, SpriteEffects.None, 0f);

                // Glow interior
                Main.spriteBatch.Draw(star, drawPos, null,
                    new Color(255, 255, 220, 200),
                    -Projectile.rotation * 0.5f, starOrigin, 0.8f, SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
            return false;
        }
    }
}
