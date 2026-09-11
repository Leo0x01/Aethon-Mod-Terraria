using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.V20
{
    /// <summary>
    /// GravityPulseProjectile — pulso de gravedad que se expande desde el
    /// centro, REPELIENDO enemigos hacia afuera (lo opuesto a un pozo
    /// gravitacional). Escala de 0 a 5 en 60 frames. Flash blanco en frame 30.
    /// </summary>
    public class GravityPulseProjectile : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 80;
            Projectile.height = 80;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            try
            {
                Projectile.ai[0] += 1f;
                float age = Projectile.ai[0];

                Projectile.velocity = Vector2.Zero;

                float currentScale = MathHelper.Lerp(0f, 5f, age / 60f);
                float currentRadius = currentScale * 40f; // pixels

                if (Main.netMode != NetmodeID.Server)
                {
                    // === Outward flying dust ===
                    if (age % 2f < 1f)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            float a = (MathHelper.TwoPi / 4) * i + age * 0.05f;
                            Vector2 pos = Projectile.Center + new Vector2(
                                (float)Math.Cos(a) * currentRadius * 0.9f,
                                (float)Math.Sin(a) * currentRadius * 0.9f);
                            Vector2 vel = new Vector2(
                                (float)Math.Cos(a) * 4f,
                                (float)Math.Sin(a) * 4f);
                            Dust d = Dust.NewDustPerfect(pos, DustID.PurpleTorch,
                                vel, 180, new Color(200, 130, 255), 1.0f);
                            d.noGravity = true;
                            d.fadeIn = 0f;
                        }
                    }

                    // Ring sparkle
                    if (Main.rand.NextBool(3))
                    {
                        float a = Main.rand.NextFloat(0, MathHelper.TwoPi);
                        Vector2 pos = Projectile.Center + new Vector2(
                            (float)Math.Cos(a) * currentRadius,
                            (float)Math.Sin(a) * currentRadius);
                        Dust d = Dust.NewDustPerfect(pos, DustID.AncientLight,
                            new Vector2((float)Math.Cos(a) * 1.5f, (float)Math.Sin(a) * 1.5f),
                            200, new Color(220, 200, 255), 0.8f);
                        d.noGravity = true;
                        d.fadeIn = 0f;
                    }
                }

                // === PUSH ALL enemies outward from the center (explosive repulsion) ===
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.CanBeChasedBy()) continue;
                    Vector2 dir = npc.Center - Projectile.Center;
                    float dist = dir.Length();
                    if (dist > currentRadius + 60f) continue; // only push outward up to the shockwave radius
                    if (dist < 1f) continue;
                    dir.Normalize();
                    float strength = (1f - Math.Abs(dist - currentRadius) / 80f) * 2.5f;
                    if (strength < 0f) strength = 0f;
                    npc.velocity += dir * strength;
                    // v5.78: removed NPC velocity cap (was capping existing velocity)
                }

                // Lighting pulse
                float lightPulse = 0.6f + 0.4f * (float)Math.Sin(age * 0.3f);
                Lighting.AddLight(Projectile.Center,
                    new Vector3(0.5f * lightPulse, 0.3f * lightPulse, 0.9f * lightPulse));
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

                Vector2 drawPos = Projectile.Center - Main.screenPosition;
                float age = Projectile.ai[0];
                float scale = MathHelper.Lerp(0f, 5f, age / 60f);
                float pulse = 0.6f + 0.4f * (float)Math.Sin(age * 0.4f);

                Vector2 ringOrigin = new Vector2(ring.Width / 2f, ring.Height / 2f);
                Vector2 glowOrigin = new Vector2(softGlow.Width / 2f, softGlow.Height / 2f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                // === Multiple pulsing Rings at the current scale ===
                for (int i = 0; i < 4; i++)
                {
                    float s = scale * (1f - i * 0.1f);
                    float alpha = (0.7f - i * 0.12f) * pulse;
                    if (alpha <= 0f) continue;
                    Color col;
                    if (i == 0) col = new Color(180, 200, 255, (byte)(alpha * 255));
                    else if (i == 1) col = new Color(150, 130, 255, (byte)(alpha * 255));
                    else if (i == 2) col = new Color(200, 150, 255, (byte)(alpha * 255));
                    else col = new Color(120, 200, 255, (byte)(alpha * 255));
                    Main.spriteBatch.Draw(ring, drawPos, null, col,
                        i * 0.1f, ringOrigin, s, SpriteEffects.None, 0f);
                }

                // === Central glow (purple-blue) ===
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(120, 140, 255, 120) * pulse,
                    0f, glowOrigin, 1.2f * pulse, SpriteEffects.None, 0f);

                // === FLASH BLANCO en frame 30 ===
                if (age >= 25f && age <= 35f)
                {
                    float flashStrength = 1f - Math.Abs(age - 30f) / 5f;
                    flashStrength = MathHelper.Clamp(flashStrength, 0f, 1f);
                    Vector2 whiteOrigin = new Vector2(glowCircleWhite.Width / 2f, glowCircleWhite.Height / 2f);
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

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Push outward on hit
            Vector2 dir = target.Center - Projectile.Center;
            if (dir.Length() > 0.1f)
            {
                dir.Normalize();
                target.velocity += dir * 8f;
            }
        }
    }
}
