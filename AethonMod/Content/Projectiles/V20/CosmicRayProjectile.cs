using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.V20
{
    /// <summary>
    /// CosmicRayProjectile — rayo cósmico perforante (long + thin).
    ///
    /// Visuales:
    ///   - BeamGold estirado MUY largo (scaleX=5, scaleY=0.3) en la dirección
    ///     del movimiento → rayo fino dorado-blanco brillante.
    ///   - SoftGlow central como núcleo blanco de mayor intensidad.
    ///   - Spawn de Star.png sparkles a lo largo de la trayectoria del rayo.
    ///   - TrailCacheLength = 20 (estela corta pero densa).
    ///
    /// Físicas:
    ///   - penetrate = -1 (atraviesa todo)
    ///   - timeLeft = 30
    ///   - extraUpdates = 3 (muy rápido)
    ///   - tileCollide = false (atraviesa paredes)
    ///   - Velocidad constante (no damping).
    /// </summary>
    public class CosmicRayProjectile : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.TrailCacheLength[Type] = 20;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;          // atraviesa todo
            Projectile.timeLeft = 30;
            Projectile.light = 0f;
            Projectile.alpha = 0;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 3;        // muy rápido
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            try
            {
                Projectile.ai[0] += 1f;
                Projectile.rotation = Projectile.velocity.ToRotation();

                // Spawn de sparkles Star.png a lo largo del rayo
                if (Main.rand.NextBool(2))
                {
                    SpawnStarSparkle();
                }

                // Iluminación dorada brillante
                Lighting.AddLight(Projectile.Center, new Vector3(1f, 0.92f, 0.55f));
            }
            catch { }
        }

        // ================================================================
        //  Spawn Star.png sparkles a lo largo de la trayectoria del rayo
        // ================================================================
        private void SpawnStarSparkle()
        {
            // Posición aleatoria a lo largo del rayo: desde -120px detrás hasta +20px delante
            float dist = Main.rand.NextFloat(-120f, 20f);
            Vector2 offset = new Vector2((float)Math.Cos(Projectile.rotation) * dist,
                                         (float)Math.Sin(Projectile.rotation) * dist);
            Vector2 spawnPos = Projectile.Center + offset;

            // Pequeña velocidad perpendicular para dispersión
            Vector2 perp = new Vector2((float)Math.Cos(Projectile.rotation + MathHelper.PiOver2),
                                       (float)Math.Sin(Projectile.rotation + MathHelper.PiOver2))
                           * Main.rand.NextFloat(-1.5f, 1.5f);

            Dust d = Dust.NewDustPerfect(spawnPos, DustID.Enchanted_Gold,
                perp, 200, new Color(255, 240, 180), 0.9f);
            d.noGravity = true;
            d.fadeIn = 0f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                Texture2D beamGold = ModContent.Request<Texture2D>("AethonMod/Content/Effects/BeamGold").Value;
                Texture2D softGlow = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                Texture2D star = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Star").Value;

                if (beamGold == null || softGlow == null || star == null)
                    return false;

                Vector2 drawPos = Projectile.Center - Main.screenPosition;
                float rotation = Projectile.velocity.ToRotation();

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                // === Estela del trail cacheada ===
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
                    float alpha = (1f - (float)i / trailLen) * 0.7f;

                    // Trail del rayo: BeamGold estirado en cada segmento
                    Main.spriteBatch.Draw(beamGold, p1, null,
                        new Color(255, 230, 150, (byte)(alpha * 255f)),
                        segRot,
                        new Vector2(0f, beamGold.Height / 2f),
                        new Vector2(segLen / (float)beamGold.Width, 0.3f),
                        SpriteEffects.None, 0f);
                }

                // === NÚCLEO: BeamGold estirado muy largo (scaleX=5, scaleY=0.3) ===
                // BeamGold es 16x128, dibujamos horizontal con origen en (0, midY)
                Vector2 beamOrigin = new Vector2(0f, beamGold.Height / 2f);
                Main.spriteBatch.Draw(beamGold, drawPos, null,
                    new Color(255, 245, 200, 240),
                    rotation, beamOrigin,
                    new Vector2(5f, 0.3f), SpriteEffects.None, 0f);

                // Halo dorado más amplio
                Main.spriteBatch.Draw(beamGold, drawPos, null,
                    new Color(255, 200, 100, 120),
                    rotation, beamOrigin,
                    new Vector2(5f, 0.5f), SpriteEffects.None, 0f);

                // === Glow central blanco ===
                Vector2 glowOrigin = new Vector2(softGlow.Width / 2f, softGlow.Height / 2f);
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(255, 250, 220, 220),
                    0f, glowOrigin, 0.9f, SpriteEffects.None, 0f);
                // Núcleo blanco pequeño
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(255, 255, 255, 240),
                    0f, glowOrigin, 0.45f, SpriteEffects.None, 0f);

                // === Star.png sparkle central (cabecera del rayo) ===
                Vector2 starOrigin = new Vector2(star.Width / 2f, star.Height / 2f);
                Main.spriteBatch.Draw(star, drawPos, null,
                    new Color(255, 255, 220, 230),
                    Main.GameUpdateCount * 0.08f, starOrigin, 1.2f, SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
            return false;
        }
    }
}
