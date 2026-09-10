using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.V20
{
    /// <summary>
    /// PhoenixNovaProjectile — nova/explosión centrada en el jugador.
    ///
    /// Visuales (60 frames total):
    ///   - Se dibuja múltiple Ring.png a escalas crecientes con colores
    ///     naranja-rojo (de naranja brillante a rojo profundo).
    ///   - En frame 30: flash blanco con GlowCircleWhite.
    ///   - Spawn continuo de DustID.Torch en todas las direcciones.
    ///
    /// Físicas:
    ///   - Velocidad cero (estático).
    ///   - penetrate = -1 (atraviesa todo en el área).
    ///   - timeLeft = 60 (1 segundo).
    ///   - tileCollide = false.
    ///   - Aplica OnFire a los NPCs golpeados.
    /// </summary>
    public class PhoenixNovaProjectile : ModProjectile
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
                float age = Projectile.ai[0];
                Projectile.ai[0] += 1f;

                // v5.78: spawn dust solo en cliente (no en server)
                if (Main.netMode != NetmodeID.Server)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        SpawnTorchDust(age);
                    }
                }

                // Iluminación cálida intensa (más intensa en el pico frame 30)
                float intensity = age < 30f ? (age / 30f) : (1f - (age - 30f) / 30f);
                intensity = MathHelper.Clamp(intensity, 0f, 1f);
                Lighting.AddLight(Projectile.Center,
                    new Vector3(1f * intensity, 0.55f * intensity, 0.15f * intensity));
            }
            catch { }
        }

        // ================================================================
        //  Spawn DustID.Torch en direcciones radiales
        // ================================================================
        private void SpawnTorchDust(float age)
        {
            float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
            // Velocidad creciente con la edad (la nova se expande)
            float speed = 2f + age * 0.18f + Main.rand.NextFloat(-1f, 1f);
            Vector2 vel = new Vector2((float)Math.Cos(angle) * speed,
                                       (float)Math.Sin(angle) * speed);
            Color color = Main.rand.NextBool(2)
                ? new Color(255, 180, 60)
                : new Color(255, 100, 30);
            Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch,
                vel, 180, color, 1.3f);
            d.noGravity = true;
            d.fadeIn = 0f;
        }

        // ================================================================
        //  OnHitNPC — aplica OnFire debuff
        // ================================================================
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            try
            {
                target.AddBuff(BuffID.OnFire, 300); // 5 segundos de OnFire
            }
            catch { }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                Texture2D ring = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Ring").Value;
                Texture2D glowCircleWhite = ModContent.Request<Texture2D>("AethonMod/Content/Effects/GlowCircleWhite").Value;
                Texture2D softGlow = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;

                if (ring == null || glowCircleWhite == null || softGlow == null)
                    return false;

                Vector2 drawPos = Projectile.Center - Main.screenPosition;
                float age = Projectile.ai[0];
                float progress = age / 60f; // 0..1

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                // === Múltiples Ring.png a escalas crecientes ===
                // Cada anillo más grande que el anterior, colores de naranja → rojo profundo
                Vector2 ringOrigin = new Vector2(ring.Width / 2f, ring.Height / 2f);
                for (int i = 0; i < 5; i++)
                {
                    // Cada anillo se expande con la edad, desfasado
                    float ringProgress = MathHelper.Clamp(progress - i * 0.05f, 0f, 1f);
                    float scale = 0.4f + ringProgress * (1.8f + i * 0.6f);
                    float alpha = (1f - ringProgress) * 0.85f;
                    if (alpha <= 0f) continue;

                    // Color: del naranja brillante (i=0) al rojo profundo (i=4)
                    int r = 255;
                    int g = (int)(180 - i * 32);
                    int b = (int)(60 - i * 12);
                    g = Math.Max(g, 30);
                    b = Math.Max(b, 10);

                    Main.spriteBatch.Draw(ring, drawPos, null,
                        new Color(r, g, b, (byte)(alpha * 255f)),
                        0f, ringOrigin, scale, SpriteEffects.None, 0f);
                }

                // === Halo central SoftGlow naranja (pulsa con la edad) ===
                float pulse = 0.9f + (float)Math.Sin(age * 0.3f) * 0.15f;
                Vector2 glowOrigin = new Vector2(softGlow.Width / 2f, softGlow.Height / 2f);
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(255, 160, 60, 200),
                    0f, glowOrigin, 1.5f * pulse, SpriteEffects.None, 0f);
                // Núcleo blanco-amarillo central
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(255, 240, 180, 230),
                    0f, glowOrigin, 0.8f * pulse, SpriteEffects.None, 0f);

                // === FLASH BLANCO en frame 30 (±5 frames) ===
                if (age >= 25f && age <= 35f)
                {
                    // Intensidad máxima en frame 30, decrece simétricamente
                    float flashStrength = 1f - Math.Abs(age - 30f) / 5f;
                    flashStrength = MathHelper.Clamp(flashStrength, 0f, 1f);
                    Vector2 whiteOrigin = new Vector2(glowCircleWhite.Width / 2f, glowCircleWhite.Height / 2f);
                    // GlowCircleWhite pulsante
                    float flashScale = 2.5f * flashStrength + 0.5f;
                    Main.spriteBatch.Draw(glowCircleWhite, drawPos, null,
                        new Color(255, 255, 255, (byte)(flashStrength * 255f)),
                        0f, whiteOrigin, flashScale, SpriteEffects.None, 0f);
                }

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
            return false;
        }
    }
}
