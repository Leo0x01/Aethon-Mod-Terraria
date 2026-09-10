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
    /// CrystalShatterProjectile — cristal que se rompe en shards al morir.
    /// - Main (ai[1] = 0): dibuja Crescent cyan-white, penetrate=3. Al morir
    ///   spawnea 12 shards (mismo tipo, menor daño, ángulos aleatorios).
    /// - Shard (ai[1] = 1): dibuja Star rotando con trail cyan, penetrate=1.
    ///   Shards shatter después de 2 bounces (trackeados en ai[0]).
    /// </summary>
    public class CrystalShatterProjectile : ModProjectile
    {
        // ai[0] = bounces (for shards) or spin angle (for main)
        // ai[1] = 0 main / 1 shard

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 240;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
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
                bool isShard = Projectile.ai[1] > 0.5f;

                if (Main.netMode != NetmodeID.Server)
                {
                    if (isShard)
                    {
                        // Cyan trail dust
                        if (Main.rand.NextBool(2))
                        {
                            Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.AncientLight,
                                -Projectile.velocity * 0.15f, 150, new Color(100, 230, 255), 0.7f);
                            d.noGravity = true;
                            d.fadeIn = 0f;
                        }
                    }
                    else
                    {
                        // Cyan sparkle around main crystal
                        if (Main.rand.NextBool(3))
                        {
                            float a = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                            Vector2 pos = Projectile.Center + new Vector2(
                                (float)Math.Cos(a) * 14f, (float)Math.Sin(a) * 14f);
                            Dust d = Dust.NewDustPerfect(pos, DustID.AncientLight,
                                Vector2.Zero, 200, new Color(180, 240, 255), 0.6f);
                            d.noGravity = true;
                            d.fadeIn = 0f;
                        }
                    }
                }

                Lighting.AddLight(Projectile.Center, new Vector3(0.3f, 0.7f, 0.9f));
            }
            catch { }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            try
            {
                bool isShard = Projectile.ai[1] > 0.5f;
                if (!isShard)
                {
                    // Main bounces too (just for visual variety)
                    if (Math.Abs(Projectile.velocity.X - oldVelocity.X) > float.Epsilon)
                        Projectile.velocity.X = -oldVelocity.X;
                    if (Math.Abs(Projectile.velocity.Y - oldVelocity.Y) > float.Epsilon)
                        Projectile.velocity.Y = -oldVelocity.Y;
                    return false;
                }

                // Shard: count bounces, shatter after 2
                int bounces = (int)Projectile.localAI[0] + 1;
                Projectile.localAI[0] = bounces;

                if (bounces >= 2)
                {
                    // Shatter: spawn small cyan sparks and die
                    if (Main.netMode != NetmodeID.Server)
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            float a = (MathHelper.TwoPi / 8) * i;
                            Vector2 dir = new Vector2(
                                (float)Math.Cos(a) * 3f,
                                (float)Math.Sin(a) * 3f);
                            Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.AncientLight,
                                dir, 200, new Color(180, 240, 255), 0.9f);
                            d.noGravity = true;
                            d.fadeIn = 0f;
                        }
                    }
                    return true; // die
                }

                // Bounce off the wall
                if (Math.Abs(Projectile.velocity.X - oldVelocity.X) > float.Epsilon)
                    Projectile.velocity.X = -oldVelocity.X;
                if (Math.Abs(Projectile.velocity.Y - oldVelocity.Y) > float.Epsilon)
                    Projectile.velocity.Y = -oldVelocity.Y;
                return false;
            }
            catch { return false; }
        }

        public override void OnKill(int timeLeft)
        {
            try
            {
                bool isShard = Projectile.ai[1] > 0.5f;
                if (isShard)
                {
                    // Shard dies with sparkle burst (no recursion)
                    if (Main.netMode == NetmodeID.Server) return;
                    for (int i = 0; i < 10; i++)
                    {
                        float a = (MathHelper.TwoPi / 10) * i;
                        Vector2 dir = new Vector2(
                            (float)Math.Cos(a) * Main.rand.NextFloat(2f, 5f),
                            (float)Math.Sin(a) * Main.rand.NextFloat(2f, 5f));
                        Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.AncientLight,
                            dir, 200, new Color(200, 240, 255), 0.9f);
                        d.noGravity = true;
                        d.fadeIn = 0f;
                    }
                    return;
                }

                // === MAIN SHATTER: spawn 12 shards at random angles ===
                int shardDamage = Math.Max(1, Projectile.damage / 3);
                for (int i = 0; i < 12; i++)
                {
                    float a = (MathHelper.TwoPi / 12) * i + Main.rand.NextFloat(-0.15f, 0.15f);
                    Vector2 vel = new Vector2(
                        (float)Math.Cos(a) * Main.rand.NextFloat(5f, 8f),
                        (float)Math.Sin(a) * Main.rand.NextFloat(5f, 8f));
                    int p = Projectile.NewProjectile(
                        Projectile.GetSource_Death(),
                        Projectile.Center, vel,
                        Projectile.type, shardDamage, 2f,
                        Projectile.owner,
                        0f, 1f); // ai[0]=0 (bounces=0), ai[1]=1 (shard)
                    if (p >= 0 && p < Main.projectile.Length)
                    {
                        Projectile shard = Main.projectile[p];
                        shard.penetrate = 1;
                        shard.timeLeft = 90;
                        shard.scale = 0.55f;
                    }
                }

                if (Main.netMode == NetmodeID.Server) return;
                // Burst of cyan dust on shatter
                for (int i = 0; i < 20; i++)
                {
                    float a = (MathHelper.TwoPi / 20) * i;
                    Vector2 dir = new Vector2(
                        (float)Math.Cos(a) * Main.rand.NextFloat(3f, 6f),
                        (float)Math.Sin(a) * Main.rand.NextFloat(3f, 6f));
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.AncientLight,
                        dir, 220, new Color(220, 250, 255), 1.0f);
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
                Vector2 drawPos = Projectile.Center - Main.screenPosition;
                bool isShard = Projectile.ai[1] > 0.5f;
                float pulse = 0.85f + 0.15f * (float)Math.Sin(Main.GameUpdateCount * 0.2f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                if (isShard)
                {
                    // === Shard: rotating Star.png with cyan trail ===
                    Texture2D star = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Star").Value;
                    Texture2D softGlow = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                    Vector2 sOrigin = new Vector2(star.Width / 2f, star.Height / 2f);
                    Vector2 gOrigin = new Vector2(softGlow.Width / 2f, softGlow.Height / 2f);
                    float fastRot = Main.GameUpdateCount * 0.4f;

                    // Draw trail cache as fading cyan stars
                    for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[Type]; i++)
                    {
                        float t = i / (float)ProjectileID.Sets.TrailCacheLength[Type];
                        Vector2 oldPos = Projectile.oldPos[i] + new Vector2(Projectile.width / 2f, Projectile.height / 2f) - Main.screenPosition;
                        if (oldPos == Vector2.Zero) continue;
                        Color trailCol = new Color(100, 220, 255, (byte)(120 * (1f - t)));
                        Main.spriteBatch.Draw(star, oldPos, null, trailCol,
                            fastRot + i * 0.3f, sOrigin, (1f - t) * 0.7f * Projectile.scale, SpriteEffects.None, 0f);
                    }

                    // Halo
                    Main.spriteBatch.Draw(softGlow, drawPos, null,
                        new Color(80, 200, 255, 180) * pulse,
                        0f, gOrigin, 0.8f * pulse * Projectile.scale, SpriteEffects.None, 0f);

                    // Bright core star
                    Main.spriteBatch.Draw(star, drawPos, null,
                        new Color(220, 250, 255, 230) * pulse,
                        fastRot, sOrigin, 0.9f * Projectile.scale, SpriteEffects.None, 0f);
                }
                else
                {
                    // === Main: Crescent cyan-white crystal ===
                    Texture2D crescent = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Crescent").Value;
                    Texture2D softGlow = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                    Vector2 cOrigin = new Vector2(crescent.Width / 2f, crescent.Height / 2f);
                    Vector2 gOrigin = new Vector2(softGlow.Width / 2f, softGlow.Height / 2f);
                    float rot = Projectile.ai[0] * 0.05f;

                    // Outer cyan glow
                    Main.spriteBatch.Draw(softGlow, drawPos, null,
                        new Color(80, 200, 255, 160) * pulse,
                        0f, gOrigin, 1.2f * pulse, SpriteEffects.None, 0f);

                    // Triple crescents (crystal facets)
                    for (int i = 0; i < 3; i++)
                    {
                        float r = rot + i * MathHelper.TwoPi / 3f;
                        Color tint = i == 0
                            ? new Color(220, 250, 255, 220)
                            : new Color(140, 220, 255, 180);
                        Main.spriteBatch.Draw(crescent, drawPos, null, tint * pulse,
                            r, cOrigin, 1.0f, SpriteEffects.None, 0f);
                    }

                    // Bright white core
                    Main.spriteBatch.Draw(softGlow, drawPos, null,
                        new Color(255, 255, 255, 200) * pulse,
                        0f, gOrigin, 0.45f * pulse, SpriteEffects.None, 0f);
                }

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode == NetmodeID.Server) return;
            // Cyan sparkles on hit
            for (int i = 0; i < 8; i++)
            {
                float a = (MathHelper.TwoPi / 8) * i;
                Vector2 dir = new Vector2(
                    (float)Math.Cos(a) * 3f,
                    (float)Math.Sin(a) * 3f);
                Dust d = Dust.NewDustPerfect(target.Center, DustID.AncientLight,
                    dir, 200, new Color(200, 240, 255), 0.8f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }
    }
}
