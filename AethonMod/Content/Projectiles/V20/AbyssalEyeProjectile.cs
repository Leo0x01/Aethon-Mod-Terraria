using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.V20
{
    /// <summary>
    /// AbyssalEyeProjectile — un ojo colosal que persigue con la mirada al
    /// enemigo más cercano y parpadea cada 40 frames infligiendo daño en radio 200.
    ///
    /// Visuales:
    ///   - Outer ring (Ring.png púrpura oscuro)
    ///   - Iris (Vortex.png rotando magenta)
    ///   - Pupil (SoftGlow negro)
    ///   - El ojo "tracks" al enemigo más cercano (rotation = ángulo al target)
    ///   - Cada 40 frames parpadea: flash blanco + daño en radio 200
    ///   - Polvo púrpura cae como lágrimas
    ///
    /// timeLeft = 200, penetrate = -1, tileCollide = false.
    /// </summary>
    public class AbyssalEyeProjectile : ModProjectile
    {
        private float Age { get => Projectile.ai[0]; set => Projectile.ai[0] = value; }
        private float BlinkTimer { get => Projectile.ai[1]; set => Projectile.ai[1] = value; }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 100;
            Projectile.height = 100;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 200;
            Projectile.light = 0f;
            Projectile.alpha = 0;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 40;
            Projectile.ignoreWater = true;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            try
            {
                Age += 1f;
                BlinkTimer += 1f;

                // Drift down very slowly
                Projectile.velocity *= 0.95f;
                Projectile.velocity.Y += 0.005f;

                // Track nearest enemy → store angle in rotation
                NPC target = null;
                float minDist = 600f;
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.CanBeChasedBy()) continue;
                    float d = (npc.Center - Projectile.Center).Length();
                    if (d < minDist) { minDist = d; target = npc; }
                }
                if (target != null)
                {
                    Vector2 toTarget = target.Center - Projectile.Center;
                    if (toTarget.Length() > 0.1f)
                    {
                        float targetAngle = (float)Math.Atan2(toTarget.Y, toTarget.X);
                        // Smooth rotation toward target
                        float currentAngle = Projectile.rotation;
                        float diff = ((targetAngle - currentAngle + MathHelper.Pi * 3) % MathHelper.TwoPi) - MathHelper.Pi;
                        Projectile.rotation = currentAngle + diff * 0.15f;
                    }
                }

                // Tears: purple dust falling from below eye
                if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(4))
                {
                    Vector2 tearPos = Projectile.Center + new Vector2(
                        Main.rand.NextFloat(-20f, 20f), 20f);
                    Vector2 vel = new Vector2(
                        Main.rand.NextFloat(-0.5f, 0.5f),
                        Main.rand.NextFloat(1f, 3f));
                    Dust d = Dust.NewDustPerfect(tearPos, DustID.PurpleTorch,
                        vel, 150, new Color(150, 60, 200), 0.9f);
                    d.noGravity = false;
                    d.fadeIn = 0f;
                }

                // === Blink every 40 frames ===
                if (BlinkTimer >= 40f)
                {
                    BlinkTimer = 0f;
                    DoBlinkDamage();
                }

                // Light: pulsing purple
                float pulse = 0.7f + 0.3f * (float)Math.Sin(Age * 0.1f);
                Lighting.AddLight(Projectile.Center,
                    new Vector3(0.5f * pulse, 0.1f * pulse, 0.7f * pulse));
            }
            catch { }
        }

        private void DoBlinkDamage()
        {
            try
            {
                // AoE damage in 200px radius
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.CanBeChasedBy()) continue;
                    float dist = (npc.Center - Projectile.Center).Length();
                    if (dist < 200f)
                    {
                        npc.SimpleStrikeNPC(Projectile.damage, npc.direction,
                            true, Projectile.knockBack, DamageClass.Magic);
                    }
                }

                // Visual: flash dust
                if (Main.netMode != NetmodeID.Server)
                {
                    for (int i = 0; i < 30; i++)
                    {
                        float angle = (MathHelper.TwoPi / 30f) * i;
                        Vector2 vel = new Vector2(
                            (float)Math.Cos(angle) * 8f,
                            (float)Math.Sin(angle) * 8f);
                        Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch,
                            vel, 240, Color.White, 1.4f);
                        d.noGravity = true;
                        d.fadeIn = 0f;
                    }
                    // White flash sparks
                    for (int i = 0; i < 10; i++)
                    {
                        Vector2 v = new Vector2(
                            Main.rand.NextFloat(-6f, 6f),
                            Main.rand.NextFloat(-6f, 6f));
                        Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Gold,
                            v, 255, Color.White, 0.8f);
                        d.noGravity = true;
                        d.fadeIn = 0f;
                    }
                }

                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, Projectile.Center);
            }
            catch { }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            try
            {
                target.AddBuff(BuffID.ShadowFlame, 180);
            }
            catch { }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                Texture2D ring = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Ring").Value;
                Texture2D vortex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Vortex").Value;
                Texture2D softGlow = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                Texture2D glowCircleWhite = ModContent.Request<Texture2D>("AethonMod/Content/Effects/GlowCircleWhite").Value;
                if (ring == null || vortex == null || softGlow == null || glowCircleWhite == null) return false;

                Vector2 drawPos = Projectile.Center - Main.screenPosition;
                Vector2 ringOrigin = new Vector2(ring.Width / 2f, ring.Height / 2f);
                Vector2 vortexOrigin = new Vector2(vortex.Width / 2f, vortex.Height / 2f);
                Vector2 glowOrigin = new Vector2(softGlow.Width / 2f, softGlow.Height / 2f);
                Vector2 whiteOrigin = new Vector2(glowCircleWhite.Width / 2f, glowCircleWhite.Height / 2f);

                // Blink overlay: fades quickly when blink fires
                float blinkFlash = 0f;
                if (BlinkTimer < 6f) blinkFlash = 1f - BlinkTimer / 6f;

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                // === Outer ring (dark purple, static) ===
                Main.spriteBatch.Draw(ring, drawPos, null,
                    new Color(80, 30, 110, 230),
                    0f, ringOrigin, 2.0f, SpriteEffects.None, 0f);

                // Outer halo (subtle purple)
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(90, 30, 130, 80),
                    0f, glowOrigin, 3.0f, SpriteEffects.None, 0f);

                // === Iris (Vortex rotating, magenta) — rotated by Projectile.rotation ===
                Main.spriteBatch.Draw(vortex, drawPos, null,
                    new Color(220, 80, 220, 220),
                    Age * 0.08f, vortexOrigin, 1.3f, SpriteEffects.None, 0f);
                // Counter-rotating layer for depth
                Main.spriteBatch.Draw(vortex, drawPos, null,
                    new Color(255, 150, 255, 150),
                    -Age * 0.12f + Projectile.rotation, vortexOrigin, 0.8f, SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

                // === Pupil (Solid black SoftGlow) — drawn in AlphaBlend to mask ===
                // Offset slightly in the direction the eye is looking
                Vector2 pupilOffset = new Vector2(
                    (float)Math.Cos(Projectile.rotation) * 12f,
                    (float)Math.Sin(Projectile.rotation) * 12f);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                Main.spriteBatch.Draw(softGlow, drawPos + pupilOffset, null,
                    Color.Black,
                    0f, glowOrigin, 0.55f, SpriteEffects.None, 0f);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

                // === Blink flash ===
                if (blinkFlash > 0f)
                {
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
                    Main.spriteBatch.Draw(glowCircleWhite, drawPos, null,
                        new Color(255, 255, 255, (byte)(255 * blinkFlash)),
                        0f, whiteOrigin, 3.5f * blinkFlash + 0.5f, SpriteEffects.None, 0f);
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                }
            }
            catch { }
            return false;
        }
    }
}
