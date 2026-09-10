using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.V20
{
    /// <summary>
    /// NebulaCloudProjectile — nube de nebulosa lenta y pulsante.
    ///
    /// Visuales:
    ///   - Noise.png con escala grande y colores púrpura-magenta, rotando
    ///     lentamente (segunda capa de Noise con rotación opuesta para más detalle).
    ///   - GlowOrbPurple/Magenta como núcleo pulsante.
    ///   - Spawn de PurpleTorch dust en patrón de nube (posiciones aleatorias
    ///     dentro de un radio de 40px del centro).
    ///
    /// Físicas:
    ///   - Slow-moving: velocity *= 0.92 cada frame.
    ///   - penetrate = -1.
    ///   - timeLeft = 240 (4 segundos).
    ///   - tileCollide = false.
    ///   - extraUpdates = 0.
    /// </summary>
    public class NebulaCloudProjectile : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 80;
            Projectile.height = 80;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 240;
            Projectile.light = 0f;
            Projectile.alpha = 0;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 25;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 0;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            try
            {
                Projectile.ai[0] += 1f;
                Projectile.rotation += 0.01f; // rotación lenta

                // === Slow-moving: damping de velocidad ===
                Projectile.velocity *= 0.92f;
                if (Projectile.velocity.Length() < 0.05f) Projectile.velocity = Vector2.Zero;

                // === Spawn PurpleTorch dust en patrón de nube (radio 40px) ===
                if (Main.rand.NextBool(2))
                {
                    SpawnCloudDust();
                }

                // === Iluminación púrpura pulsante ===
                float pulse = 0.7f + (float)Math.Sin(Projectile.ai[0] * 0.08f) * 0.3f;
                Lighting.AddLight(Projectile.Center,
                    new Vector3(0.45f * pulse, 0.15f * pulse, 0.6f * pulse));
            }
            catch { }
        }

        // ================================================================
        //  Spawn PurpleTorch dust en patrón de nube (radio 40px)
        // ================================================================
        private void SpawnCloudDust()
        {
            // Posición aleatoria dentro de un radio de 40px (nube dispersa)
            float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
            float dist = Main.rand.NextFloat(0f, 40f);
            Vector2 offset = new Vector2((float)Math.Cos(angle) * dist,
                                          (float)Math.Sin(angle) * dist);
            Vector2 spawnPos = Projectile.Center + offset;

            // Velocidad muy lenta, ligeramente hacia afuera para expandir la nube
            Vector2 outDir = offset.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(0.2f, 0.8f);
            // Pequeña componente aleatoria
            outDir += new Vector2(Main.rand.NextFloat(-0.5f, 0.5f),
                                   Main.rand.NextFloat(-0.5f, 0.5f));

            // Color alterno: púrpura o magenta
            bool magenta = Main.rand.NextBool(2);
            Color color = magenta ? new Color(220, 80, 255) : new Color(160, 60, 220);
            Dust d = Dust.NewDustPerfect(spawnPos, DustID.PurpleTorch,
                outDir, 150, color, 1.1f);
            d.noGravity = true;
            d.fadeIn = 0f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                Texture2D noise = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Noise").Value;
                Texture2D glowOrbPurple = ModContent.Request<Texture2D>("AethonMod/Content/Effects/GlowOrbPurple").Value;
                Texture2D glowOrbMagenta = ModContent.Request<Texture2D>("AethonMod/Content/Effects/GlowOrbMagenta").Value;

                if (noise == null || glowOrbPurple == null || glowOrbMagenta == null)
                    return false;

                Vector2 drawPos = Projectile.Center - Main.screenPosition;
                float t = Main.GameUpdateCount;
                // Pulso para glow (0.85..1.15)
                float pulse = 1f + (float)Math.Sin(Projectile.ai[0] * 0.08f) * 0.15f;

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                // === NOISE LAYER 1 — púrpura-magenta, escala grande, rotación lenta ===
                Vector2 noiseOrigin = new Vector2(noise.Width / 2f, noise.Height / 2f);
                Main.spriteBatch.Draw(noise, drawPos, null,
                    new Color(180, 60, 200, 90),
                    t * 0.006f + Projectile.rotation,
                    noiseOrigin, 2.2f * pulse, SpriteEffects.None, 0f);

                // === NOISE LAYER 2 — magenta más brillante, rotación opuesta ===
                Main.spriteBatch.Draw(noise, drawPos, null,
                    new Color(220, 100, 230, 70),
                    -t * 0.004f + Projectile.rotation * 0.5f,
                    noiseOrigin, 1.6f * pulse, SpriteEffects.None, 0f);

                // === NOISE LAYER 3 — núcleo blanco-magenta (más pequeño, más brillante) ===
                Main.spriteBatch.Draw(noise, drawPos, null,
                    new Color(255, 200, 255, 100),
                    t * 0.008f,
                    noiseOrigin, 1.0f * pulse, SpriteEffects.None, 0f);

                // === GlowOrbPurple (núcleo pulsante púrpura) ===
                Vector2 glowOrigin = new Vector2(glowOrbPurple.Width / 2f, glowOrbPurple.Height / 2f);
                Main.spriteBatch.Draw(glowOrbPurple, drawPos, null,
                    new Color(200, 100, 255, 180),
                    0f, glowOrigin, 1.2f * pulse, SpriteEffects.None, 0f);

                // === GlowOrbMagenta (núcleo pulsante magenta, más pequeño) ===
                Main.spriteBatch.Draw(glowOrbMagenta, drawPos, null,
                    new Color(255, 120, 255, 200),
                    0f, glowOrigin, 0.7f * pulse, SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
            return false;
        }
    }
}
