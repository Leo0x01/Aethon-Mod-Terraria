using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Graphics.CameraModifiers; // PunchCameraModifier

namespace AethonMod.Content.Projectiles.V20
{
    /// <summary>
    /// EarthquakeProjectile — ondas sísmicas en el suelo. velocity = 0 (estático).
    /// Cada 10 frames crea un Ring.png expansivo. Cada 15 frames sacude la cámara.
    /// Draw Slash.png estirado horizontalmente para simular grietas.
    /// </summary>
    public class EarthquakeProjectile : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 120;
            Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 120;
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

                // Velocity = 0 (stays at spawn position)
                Projectile.velocity = Vector2.Zero;

                if (Main.netMode != NetmodeID.Server)
                {
                    // === Ground-level debris every frame ===
                    if (Main.rand.NextBool(2))
                    {
                        float a = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                        float outward = Main.rand.NextFloat(3f, 6f);
                        Vector2 pos = Projectile.Center + new Vector2(
                            (float)Math.Cos(a) * 20f, 10f);
                        Vector2 vel = new Vector2(
                            (float)Math.Cos(a) * outward,
                            -Main.rand.NextFloat(3f, 6f));
                        Dust d = Dust.NewDustPerfect(pos, DustID.Stone,
                            vel, 100, new Color(180, 130, 80), 1.2f);
                        d.noGravity = false;
                        d.fadeIn = 0f;
                    }

                    // Brown dust clouds outward on the ground
                    if (age % 5f < 1f)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            int dir = (i < 2) ? -1 : 1;
                            Vector2 pos = Projectile.Center + new Vector2(dir * (40f + i * 10f), 10f);
                            Vector2 vel = new Vector2(dir * Main.rand.NextFloat(3f, 5f),
                                -Main.rand.NextFloat(1f, 3f));
                            Dust d = Dust.NewDustPerfect(pos, DustID.Dirt,
                                vel, 120, new Color(150, 100, 60), 1.1f);
                            d.noGravity = false;
                            d.fadeIn = 0f;
                        }
                    }
                }

                // === Expanding Ring every 10 frames (creates new shockwave) ===
                // We just track time; rings are drawn procedurally in PreDraw based on age.

                // === Screen shake every 15 frames ===
                if (age > 0 && age % 15f < 1f)
                {
                    PunchCameraModifier shake = new PunchCameraModifier(
                        Projectile.Center,
                        Main.rand.NextBool() ? new Vector2(1f, 0f) : new Vector2(0f, 1f),
                        8f, 8f, 20, 1000f, FullName);
                    Main.instance.CameraModifiers.Add(shake);
                }

                Lighting.AddLight(Projectile.Center, new Vector3(0.6f, 0.35f, 0.1f));
            }
            catch { }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                Texture2D ring = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Ring").Value;
                Texture2D slash = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Slash").Value;
                Texture2D softGlow = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;

                Vector2 drawPos = Projectile.Center - Main.screenPosition + new Vector2(0f, 10f);
                float age = Projectile.ai[0];
                Vector2 ringOrigin = new Vector2(ring.Width / 2f, ring.Height / 2f);
                Vector2 slashOrigin = new Vector2(slash.Width / 2f, slash.Height / 2f);
                Vector2 glowOrigin = new Vector2(softGlow.Width / 2f, softGlow.Height / 2f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                // === Expanding rings (one shockwave every 10 frames, life 30 frames) ===
                // Wavefront spacing: every 10 frames spawns a new ring; it expands.
                for (int i = 0; i < 6; i++)
                {
                    float spawnTime = i * 10f;
                    float ringAge = age - spawnTime;
                    if (ringAge < 0f) continue;
                    if (ringAge > 30f) continue;
                    float progress = ringAge / 30f;
                    float scale = 0.3f + progress * 2.4f;
                    float alpha = (1f - progress) * 0.7f;
                    if (alpha <= 0f) continue;
                    Color col = new Color(255, 180, 80, (byte)(alpha * 255));
                    Main.spriteBatch.Draw(ring, drawPos, null,
                        col, 0f, ringOrigin,
                        new Vector2(scale, scale * 0.35f), // elliptical (ground-perspective)
                        SpriteEffects.None, 0f);
                }

                // === Cracks: Slash.png stretched horizontally at ground level ===
                // 5 cracks with random rotation/fade
                for (int i = 0; i < 5; i++)
                {
                    float phase = i * 1.7f;
                    float lifeProgress = ((age + phase * 5f) % 40f) / 40f;
                    if (lifeProgress > 0.7f) continue;
                    float scale = 1.5f + lifeProgress * 1.5f;
                    float alpha = (1f - lifeProgress / 0.7f) * 0.6f;
                    float rot = (i - 2) * 0.12f; // small variation
                    Main.spriteBatch.Draw(slash, drawPos + new Vector2(i * 18f - 36f, 0f), null,
                        new Color(200, 110, 40, (byte)(alpha * 255)),
                        rot, slashOrigin,
                        new Vector2(scale, 0.35f),
                        i % 2 == 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0f);
                }

                // === Ground dust glow ===
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(180, 100, 40, 150),
                    0f, glowOrigin, new Vector2(2.5f, 0.6f), SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Apply brief slowness-style knockback (heavy upward + outward)
            if (target.knockBackResist > 0f)
            {
                Vector2 dir = target.Center - Projectile.Center;
                if (dir.Length() > 0.1f)
                {
                    dir.Normalize();
                    target.velocity += dir * 6f - new Vector2(0f, 4f);
                }
            }
        }
    }
}
