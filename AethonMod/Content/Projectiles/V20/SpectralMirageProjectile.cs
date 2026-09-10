using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.V20
{
    /// <summary>
    /// SpectralMirageProjectile — proyectil fantasma que deja afterimages con
    /// corrimiento de color cian → púrpura. No genera dust (es un fantasma).
    ///
    /// Visuales:
    ///   - PreDraw dibuja el proyectil en 5 posiciones anteriores (oldPos) con
    ///     alpha y scale decrecientes (0.4, 0.3, 0.2, 0.1, 0.05).
    ///   - Cada afterimage tiene un leve color shift (cyan → purple).
    ///   - El proyectil principal dibuja Crescent.png con alpha baja (ghostly).
    ///
    /// penetrate = 3, timeLeft = 120, TrailCacheLength = 10, tileCollide = false.
    /// </summary>
    public class SpectralMirageProjectile : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 120;
            Projectile.light = 0.4f;
            Projectile.alpha = 80;
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
                Projectile.rotation += 0.12f;

                // Ghostly hover — small sine wave drift
                float wave = (float)Math.Sin(Projectile.ai[0] * 0.1f) * 0.05f;
                Projectile.velocity = Projectile.velocity.RotatedBy(wave);

                // Slight friction
                Projectile.velocity *= 0.99f;

                // Subtle ghost glow (no dust per spec)
                Lighting.AddLight(Projectile.Center, new Vector3(0.2f, 0.6f, 0.9f));
            }
            catch { }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            try
            {
                // Brief ghostly tint flash — no dust (per spec)
                target.AddBuff(BuffID.ShadowFlame, 120);
            }
            catch { }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                Texture2D crescent = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Crescent").Value;
                Texture2D softGlow = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                if (crescent == null || softGlow == null) return false;

                Vector2 drawPos = Projectile.Center - Main.screenPosition;
                Vector2 crescentOrigin = new Vector2(crescent.Width / 2f, crescent.Height / 2f);
                Vector2 glowOrigin = new Vector2(softGlow.Width / 2f, softGlow.Height / 2f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                // === 5 afterimages ===
                // alpha values: 0.4, 0.3, 0.2, 0.1, 0.05
                // color shift cyan → purple as we go back in time
                float[] alphas = { 0.4f, 0.3f, 0.2f, 0.1f, 0.05f };
                int trailLen = ProjectileID.Sets.TrailCacheLength[Type];
                int afterCount = 5;

                for (int i = 0; i < afterCount; i++)
                {
                    if (i >= trailLen) break;
                    if (Projectile.oldPos[i] == Vector2.Zero) continue;
                    float a = alphas[i];
                    // Color shift: i=0 cyan, i=4 purple
                    float t = i / (float)(afterCount - 1);
                    int r = (int)(80 + 140 * t);   // 80 → 220
                    int g = (int)(220 - 140 * t);  // 220 → 80
                    int b = (int)(220 + 35 * t);   // 220 → 255
                    Color color = new Color(r, g, b, (byte)(a * 255f));

                    float scale = 1.0f - i * 0.15f;
                    Vector2 afterPos = Projectile.oldPos[i] + new Vector2(Projectile.width / 2f, Projectile.height / 2f) - Main.screenPosition;
                    // Soft glow halo around each afterimage
                    Main.spriteBatch.Draw(softGlow, afterPos, null,
                        color * 0.6f, 0f, glowOrigin, scale * 0.7f, SpriteEffects.None, 0f);
                    // Crescent itself
                    Main.spriteBatch.Draw(crescent, afterPos, null,
                        color, Projectile.rotation, crescentOrigin, scale, SpriteEffects.None, 0f);
                }

                // === Main projectile: ghostly Crescent with low alpha ===
                // Glow halo (low alpha)
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(140, 200, 255, 80),
                    0f, glowOrigin, 0.8f, SpriteEffects.None, 0f);
                // Crescent (ghostly)
                Main.spriteBatch.Draw(crescent, drawPos, null,
                    new Color(180, 230, 255, 110),
                    Projectile.rotation, crescentOrigin, 1.0f, SpriteEffects.None, 0f);
                // Counter-rotating layer
                Main.spriteBatch.Draw(crescent, drawPos, null,
                    new Color(200, 160, 255, 80),
                    -Projectile.rotation * 0.7f, crescentOrigin, 0.7f, SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
            return false;
        }
    }
}
