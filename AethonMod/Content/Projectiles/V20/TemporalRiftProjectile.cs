using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.V20
{
    /// <summary>
    /// TemporalRiftProjectile — un desgarro vertical en el espacio-tiempo.
    ///
    /// Visuales:
    ///   - Dibuja Noise.png estirado verticalmente (scaleX=0.5, scaleY=3.0)
    ///     con blending aditivo y color cambiante en el tiempo (hue shifting).
    ///   - El rift "respira": scale pulsa.
    ///   - Genera dust que es SUCKED hacia el centro (velocidad acelerante).
    ///
    /// Físicas:
    ///   - Proyectil estático.
    ///   - Dilatación temporal: NPCs cercanos (200px) tienen velocity *= 0.5.
    ///   - penetrate = -1, timeLeft = 150, tileCollide = false.
    /// </summary>
    public class TemporalRiftProjectile : ModProjectile
    {
        private float Age { get => Projectile.ai[0]; set => Projectile.ai[0] = value; }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 200;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 150;
            Projectile.light = 0f;
            Projectile.alpha = 0;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
            Projectile.ignoreWater = true;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            try
            {
                Age += 1f;
                Projectile.velocity *= 0.95f;

                // v5.78: Time dilation via Slow buff (no velocity mutation)
                // ANTES: npc.velocity *= 0.5f — congelaba enemigos permanentemente
                // DESPUES: aplicar BuffID.Slow que es la forma correcta de ralentizar
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.CanBeChasedBy()) continue;
                    float dist = (npc.Center - Projectile.Center).Length();
                    if (dist < 200f)
                    {
                        npc.AddBuff(BuffID.Slow, 10); // 10 frames = se refresca cada frame
                    }
                }

                // === Spawn dust that gets sucked toward the rift ===
                if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(2))
                {
                    // Spawn dust around the rift (vertically distributed)
                    float yOff = Main.rand.NextFloat(-100f, 100f);
                    float xOff = Main.rand.NextFloat(-80f, 80f);
                    if (Math.Abs(xOff) < 15f) xOff = Math.Sign(xOff == 0 ? 1 : xOff) * 20f;
                    Vector2 spawnPos = Projectile.Center + new Vector2(xOff, yOff);
                    Vector2 toCenter = Projectile.Center - spawnPos;
                    if (toCenter.Length() > 0.1f)
                    {
                        toCenter.Normalize();
                        // Accelerate toward center: store magnitude via ai in dust? Use velocity * speed scale
                        float speed = 2f + Age * 0.02f;
                        Vector2 vel = toCenter * speed;
                        // Pick a hue-shifted color based on Age
                        Color c = HueColor((Age * 0.01f) % 1f);
                        Dust d = Dust.NewDustPerfect(spawnPos, DustID.RainbowRod,
                            vel, 200, c, 0.9f);
                        d.noGravity = true;
                        d.fadeIn = 0f;
                    }
                }

                // Light with hue shifting
                Color lc = HueColor((Age * 0.01f) % 1f);
                Lighting.AddLight(Projectile.Center,
                    new Vector3(lc.R / 255f * 0.7f, lc.G / 255f * 0.7f, lc.B / 255f * 0.7f));
            }
            catch { }
        }

        private Color HueColor(float hue)
        {
            return Main.hslToRgb(hue, 1f, 0.6f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            try
            {
                // Slow debuff
                target.AddBuff(BuffID.Slow, 180);
            }
            catch { }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                Texture2D noise = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Noise").Value;
                Texture2D softGlow = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                if (noise == null || softGlow == null) return false;

                Vector2 drawPos = Projectile.Center - Main.screenPosition;
                Vector2 noiseOrigin = new Vector2(noise.Width / 2f, noise.Height / 2f);
                Vector2 glowOrigin = new Vector2(softGlow.Width / 2f, softGlow.Height / 2f);

                // "Breathing" — scale pulses
                float breath = 0.9f + 0.15f * (float)Math.Sin(Age * 0.15f);
                // Hue cycles over time
                float hue = (Age * 0.01f) % 1f;
                Color baseColor = HueColor(hue);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                // === Glow halo behind rift ===
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(baseColor.R, baseColor.G, baseColor.B, 60),
                    0f, glowOrigin, new Vector2(1.2f, 3.0f) * breath, SpriteEffects.None, 0f);

                // === Rift: Noise stretched vertically (scaleX=0.5, scaleY=3.0) ===
                // Multiple layers for depth, with hue shift between them
                for (int i = 0; i < 3; i++)
                {
                    float layerHue = (hue + i * 0.08f) % 1f;
                    Color layerColor = HueColor(layerHue);
                    byte layerAlpha = (byte)(200 - i * 50);
                    float rotOffset = i * 0.02f * (float)Math.Sin(Age * 0.05f);
                    Main.spriteBatch.Draw(noise, drawPos, null,
                        new Color(layerColor.R, layerColor.G, layerColor.B, layerAlpha),
                        rotOffset, noiseOrigin,
                        new Vector2(0.5f * breath, 3.0f * breath), SpriteEffects.None, 0f);
                }

                // === Bright center line (white-hot core of the rift) ===
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(255, 255, 255, 180),
                    0f, glowOrigin, new Vector2(0.15f, 2.8f) * breath, SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
            return false;
        }
    }
}
