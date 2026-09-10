using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.V20
{
    /// <summary>
    /// LaserBeamProjectile — rayo láser continuo de energía cian.
    ///
    /// Visuales:
    ///   - Trail.png estirado a lo largo de la dirección de movimiento
    ///   - TrailCacheLength = 30 (estela larga)
    ///   - BeamCyan dibujado como núcleo
    ///   - Glow aditivo cian brillante
    ///
    /// Físicas:
    ///   - Slow-moving (velocidad reducida)
    ///   - Penetrate = -1 (atraviesa todo)
    ///   - timeLeft = 60 (1 segundo)
    ///   - Dust trail cian
    /// </summary>
    public class LaserBeamProjectile : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.TrailCacheLength[Type] = 30;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
            Projectile.light = 0f;
            Projectile.alpha = 0;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            try
            {
                Projectile.ai[0] += 1f;
                Projectile.rotation = Projectile.velocity.ToRotation();

                // Slow-moving: factor de amortiguación para reducir velocidad
                Projectile.velocity *= 0.985f;

                // Dust trail cian brillante
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch,
                        -Projectile.velocity * 0.1f + new Vector2(
                            Main.rand.NextFloat(-0.5f, 0.5f),
                            Main.rand.NextFloat(-0.5f, 0.5f)),
                        150, new Color(80, 220, 255), 0.9f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }

                // Iluminación cian brillante
                Lighting.AddLight(Projectile.Center, new Vector3(0.2f, 0.85f, 1f));
            }
            catch { }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                Texture2D trail = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Trail").Value;
                Texture2D beamCyan = ModContent.Request<Texture2D>("AethonMod/Content/Effects/BeamCyan").Value;
                Texture2D softGlow = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;

                if (trail == null || beamCyan == null || softGlow == null)
                    return false;

                Vector2 drawPos = Projectile.Center - Main.screenPosition;
                float rotation = Projectile.velocity.ToRotation();

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                // === Estela larga dibujada con Trail.png estirada ===
                // Trail.png es 64x8, dibujamos cada segmento entre oldPos consecutivos
                int trailLen = ProjectileID.Sets.TrailCacheLength[Type];
                for (int i = 0; i < trailLen - 1; i++)
                {
                    Vector2 p1 = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                    Vector2 p2 = Projectile.oldPos[i + 1] + Projectile.Size / 2f - Main.screenPosition;
                    if (p1 == p2) continue;

                    float segLen = Vector2.Distance(p1, p2);
                    if (segLen < 0.1f) continue;
                    Vector2 segDir = (p2 - p1).SafeNormalize(Vector2.Zero);
                    float segRot = segDir.ToRotation();
                    float alpha = (1f - (float)i / trailLen) * 0.85f;
                    float scale = (1f - (float)i / trailLen) * 1.2f;

                    // Trail.png estirada a lo largo de la dirección del segmento
                    Main.spriteBatch.Draw(trail, p1, null,
                        new Color(80, 220, 255, (byte)(alpha * 255f)),
                        segRot,
                        new Vector2(0f, trail.Height / 2f),
                        new Vector2(segLen / (float)trail.Width, scale),
                        SpriteEffects.None, 0f);
                }

                // === Núcleo: BeamCyan rotado a lo largo de la velocidad ===
                // BeamCyan es textura horizontal, la estiramos en X para simular un rayo
                Vector2 beamOrigin = new Vector2(0f, beamCyan.Height / 2f);
                Main.spriteBatch.Draw(beamCyan, drawPos, null,
                    new Color(180, 240, 255, 230),
                    rotation, beamOrigin,
                    new Vector2(2.5f, 0.7f), SpriteEffects.None, 0f);

                // === Glow central aditivo ===
                Vector2 glowOrigin = new Vector2(softGlow.Width / 2f, softGlow.Height / 2f);
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(100, 230, 255, 180),
                    0f, glowOrigin, 0.8f, SpriteEffects.None, 0f);
                // Núcleo blanco más pequeño
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(220, 250, 255, 220),
                    0f, glowOrigin, 0.4f, SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
            return false;
        }
    }
}
