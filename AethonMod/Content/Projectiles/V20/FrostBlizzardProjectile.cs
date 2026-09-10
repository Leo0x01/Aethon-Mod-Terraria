using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.V20
{
    /// <summary>
    /// FrostBlizzardProjectile — cristal de hielo pequeño disparado en ráfaga.
    ///
    /// Visuales:
    ///   - Star.png pequeña (cyan-white) con rotación lenta.
    ///   - SoftGlow cyan aditivo como núcleo.
    ///   - TrailCacheLength = 8 (estela corta helada).
    ///   - Estela de IceTorch dust (cyan-blanco).
    ///
    /// Físicas:
    ///   - penetrate = 2 (atraviesa 2 enemigos).
    ///   - timeLeft = 90 (1.5 segundos).
    ///   - extraUpdates = 1.
    ///   - tileCollide = true (rebota en paredes... no, simplemente muere).
    ///   - Aplica Frostburn (BuffID.Frostburn) al impactar.
    /// </summary>
    public class FrostBlizzardProjectile : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.tileCollide = true;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 2;            // atraviesa 2 enemigos
            Projectile.timeLeft = 90;
            Projectile.light = 0f;
            Projectile.alpha = 0;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            try
            {
                Projectile.ai[0] += 1f;
                // Rotación lenta para que la estrella gire
                Projectile.rotation += 0.12f;
                // Leve gravedad para que caiga como cristales
                Projectile.velocity.Y += 0.03f;
                // Clamp de velocidad horizontal para que no se descontrole
                if (Projectile.velocity.Length() > 18f)
                    Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.Zero) * 18f;

                // Estela de IceTorch dust
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.IceTorch,
                        -Projectile.velocity * 0.1f + new Vector2(
                            Main.rand.NextFloat(-0.8f, 0.8f),
                            Main.rand.NextFloat(-0.8f, 0.8f)),
                        150, new Color(150, 230, 255), 0.9f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }

                // Iluminación cyan-blanca
                Lighting.AddLight(Projectile.Center, new Vector3(0.55f, 0.9f, 1f));
            }
            catch { }
        }

        // ================================================================
        //  OnHitNPC — aplica Frostburn
        // ================================================================
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            try
            {
                target.AddBuff(BuffID.Frostburn, 240); // 4 segundos de Frostburn
            }
            catch { }
        }

        // ================================================================
        //  Kill — al impactar tile, pequeña explosión de cristales helados
        // ================================================================
        public override void Kill(int timeLeft)
        {
            try
            {
                for (int i = 0; i < 8; i++)
                {
                    float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                    float speed = Main.rand.NextFloat(2f, 5f);
                    Vector2 vel = new Vector2((float)Math.Cos(angle) * speed,
                                               (float)Math.Sin(angle) * speed);
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.IceTorch,
                        vel, 180, new Color(180, 240, 255), 1.1f);
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
                float rotation = Projectile.rotation;

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                // === Estela corta con trail cache ===
                int trailLen = ProjectileID.Sets.TrailCacheLength[Type];
                for (int i = 0; i < trailLen - 1; i++)
                {
                    Vector2 p = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                    float alpha = (1f - (float)i / trailLen) * 0.4f;
                    float scale = (1f - (float)i / trailLen) * 0.7f;
                    Main.spriteBatch.Draw(star, p, null,
                        new Color(150, 230, 255, (byte)(alpha * 255f)),
                        rotation * 0.5f,
                        new Vector2(star.Width / 2f, star.Height / 2f),
                        scale, SpriteEffects.None, 0f);
                }

                // === Glow cyan aditivo ===
                Vector2 glowOrigin = new Vector2(softGlow.Width / 2f, softGlow.Height / 2f);
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(80, 200, 255, 180),
                    0f, glowOrigin, 0.7f, SpriteEffects.None, 0f);

                // === Star.png principal cyan-white ===
                Vector2 starOrigin = new Vector2(star.Width / 2f, star.Height / 2f);
                // Halo blanco
                Main.spriteBatch.Draw(star, drawPos, null,
                    new Color(255, 255, 255, 230),
                    rotation, starOrigin, 1.1f, SpriteEffects.None, 0f);
                // Núcleo cyan
                Main.spriteBatch.Draw(star, drawPos, null,
                    new Color(120, 220, 255, 200),
                    rotation, starOrigin, 0.7f, SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
            return false;
        }
    }
}
