using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.V20
{
    /// <summary>
    /// InfernoTornadoProjectile — tornado de fuego que avanza lentamente.
    ///
    /// Visuales:
    ///   - Spawn de DustID.Torch en espiral ascendente.
    ///   - Dibuja Vortex.png con tinte naranja-rojo en múltiples capas rotando
    ///     a distintas velocidades.
    ///   - GlowCircleGold en la base.
    ///
    /// Físicas:
    ///   - Se mueve hacia adelante lentamente.
    ///   - Aplica OnFire a los enemigos que toca.
    ///   - penetrate = -1, timeLeft = 180.
    /// </summary>
    public class InfernoTornadoProjectile : ModProjectile
    {
        private float Age { get => Projectile.ai[0]; set => Projectile.ai[0] = value; }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 120;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 180;
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
                Age += 1f;
                // Slow forward motion (slightly)
                Projectile.velocity *= 0.985f;
                if (Projectile.velocity.Length() < 1.5f && Projectile.velocity.Length() > 0.1f)
                    Projectile.velocity = Vector2.Normalize(Projectile.velocity) * 1.5f;

                // === Spawn Torch dust in rising spiral ===
                if (Main.netMode != NetmodeID.Server)
                {
                    int dustCount = 3;
                    for (int i = 0; i < dustCount; i++)
                    {
                        float angle = Age * 0.3f + i * MathHelper.TwoPi / 3f;
                        float radius = 25f - (Age % 30f) * 0.3f; // narrows then resets
                        if (radius < 8f) radius = 8f;
                        float yOff = -((Age % 60f) * 1.5f); // rises
                        Vector2 spawnPos = Projectile.Center + new Vector2(
                            (float)Math.Cos(angle) * radius,
                            40f + yOff); // starts at bottom (y+40), rises
                        if (spawnPos.Y < Projectile.Center.Y - 60f) continue;
                        Vector2 vel = new Vector2(
                            -(float)Math.Sin(angle) * 1.5f,
                            -1.5f);
                        Color color = Main.rand.NextBool(2)
                            ? new Color(255, 180, 60)
                            : new Color(255, 100, 30);
                        Dust d = Dust.NewDustPerfect(spawnPos, DustID.Torch,
                            vel, 180, color, 1.3f);
                        d.noGravity = true;
                        d.fadeIn = 0f;
                    }
                }

                // === Warm flickering light ===
                float flicker = 0.8f + 0.2f * (float)Math.Sin(Age * 0.3f);
                Lighting.AddLight(Projectile.Center,
                    new Vector3(1f * flicker, 0.5f * flicker, 0.1f * flicker));
            }
            catch { }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            try
            {
                target.AddBuff(BuffID.OnFire, 240);
                target.AddBuff(BuffID.OnFire3, 120); // hellfire
                // Burst of fire sparks
                for (int i = 0; i < 8; i++)
                {
                    float a = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                    float s = Main.rand.NextFloat(3f, 6f);
                    Vector2 v = new Vector2((float)Math.Cos(a) * s, (float)Math.Sin(a) * s);
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch,
                        v, 200, new Color(255, 180, 60), 1.2f);
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
                Texture2D vortex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Vortex").Value;
                Texture2D glowCircleGold = ModContent.Request<Texture2D>("AethonMod/Content/Effects/GlowCircleGold").Value;
                Texture2D softGlow = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                if (vortex == null || glowCircleGold == null || softGlow == null) return false;

                Vector2 basePos = Projectile.Center - Main.screenPosition + new Vector2(0f, 50f);
                Vector2 drawPos = Projectile.Center - Main.screenPosition;
                Vector2 vortexOrigin = new Vector2(vortex.Width / 2f, vortex.Height / 2f);
                Vector2 goldOrigin = new Vector2(glowCircleGold.Width / 2f, glowCircleGold.Height / 2f);
                Vector2 glowOrigin = new Vector2(softGlow.Width / 2f, softGlow.Height / 2f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                // === GlowCircleGold at the base ===
                Main.spriteBatch.Draw(glowCircleGold, basePos, null,
                    new Color(255, 180, 60, 200),
                    0f, goldOrigin, 1.2f, SpriteEffects.None, 0f);
                // Larger softer halo at base
                Main.spriteBatch.Draw(softGlow, basePos, null,
                    new Color(255, 150, 50, 100),
                    0f, glowOrigin, 1.5f, SpriteEffects.None, 0f);

                // === Tornado layers (Vortex.png, orange-red tint) ===
                // Multiple layers rotating at different speeds, scaled and offset vertically
                float breath = 0.95f + 0.05f * (float)Math.Sin(Age * 0.15f);
                int layerCount = 5;
                for (int i = 0; i < layerCount; i++)
                {
                    float layerT = i / (float)(layerCount - 1); // 0 at base, 1 at top
                    float yOff = -layerT * 90f;
                    float scale = (1.4f - layerT * 0.9f) * breath; // narrows at top
                    float rot = Age * (0.15f + i * 0.04f);
                    // Color shifts from red (base) to bright orange-yellow (top)
                    int r = 255;
                    int g = (int)(80 + layerT * 150);
                    int b = (int)(20 + layerT * 80);
                    Color color = new Color(r, g, b, 200);
                    Vector2 layerPos = drawPos + new Vector2(0f, yOff);
                    Main.spriteBatch.Draw(vortex, layerPos, null,
                        color, rot, vortexOrigin, scale, SpriteEffects.None, 0f);
                    // Counter-rotating layer for richness
                    Main.spriteBatch.Draw(vortex, layerPos, null,
                        new Color(255, 100, 30, 100),
                        -rot * 1.3f, vortexOrigin, scale * 0.8f, SpriteEffects.None, 0f);
                }

                // === Bright top apex ===
                Main.spriteBatch.Draw(softGlow, drawPos - new Vector2(0f, 80f), null,
                    new Color(255, 230, 150, 200),
                    0f, glowOrigin, 0.8f * breath, SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
            return false;
        }
    }
}
