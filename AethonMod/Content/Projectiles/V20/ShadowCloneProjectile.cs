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
    /// ShadowCloneProjectile — sombra que orbita al jugador como un minion.
    /// Dibuja Star.png con Color.Black semi-transparente (additive = sombra).
    /// Cuando un enemigo está a 200px, dash hacia él (velocidad 20), vuelve.
    /// Estela púrpura. penetrate=5, timeLeft=600.
    /// </summary>
    public class ShadowCloneProjectile : ModProjectile
    {
        // ai[0] = state (0 orbit, 1 dash, 2 returning)
        // ai[1] = state timer

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 5;
            Projectile.timeLeft = 600;
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
                Player player = Main.player[Projectile.owner];
                if (!player.active || player.dead) { Projectile.Kill(); return; }

                Projectile.ai[1] += 1f;
                int state = (int)Projectile.ai[0];
                float timer = Projectile.ai[1];

                // === ORBIT ===
                if (state == 0)
                {
                    float orbitRadius = 60f;
                    float angle = timer * 0.08f;
                    Vector2 targetPos = player.Center + new Vector2(
                        (float)Math.Cos(angle) * orbitRadius,
                        (float)Math.Sin(angle) * orbitRadius);
                    // Smooth toward target
                    Projectile.Center = targetPos;
                    Projectile.velocity = Vector2.Zero;

                    // Detect nearby enemy within 200px
                    NPC nearest = null;
                    float nearestDist = 200f;
                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        if (!npc.CanBeChasedBy()) continue;
                        float d = (npc.Center - Projectile.Center).Length();
                        if (d < nearestDist) { nearestDist = d; nearest = npc; }
                    }
                    if (nearest != null)
                    {
                        Vector2 dir = nearest.Center - Projectile.Center;
                        if (dir.Length() > 0.1f)
                        {
                            dir.Normalize();
                            Projectile.velocity = dir * 20f;
                            Projectile.ai[0] = 1f; // dash
                            Projectile.ai[1] = 0f;
                        }
                    }
                }
                // === DASH ===
                else if (state == 1)
                {
                    // After 25 frames of dashing, return to orbit
                    if (timer > 25f)
                    {
                        Projectile.ai[0] = 2f;
                        Projectile.ai[1] = 0f;
                    }
                    // Continue current velocity (slight slowdown)
                    Projectile.velocity *= 0.96f;
                }
                // === RETURNING ===
                else if (state == 2)
                {
                    Vector2 toPlayer = player.Center - Projectile.Center;
                    float dist = toPlayer.Length();
                    if (dist < 20f)
                    {
                        // Resume orbit
                        Projectile.ai[0] = 0f;
                        Projectile.ai[1] = 0f;
                    }
                    else if (dist > 0.1f)
                    {
                        toPlayer.Normalize();
                        Projectile.velocity = toPlayer * 14f;
                    }
                }

                if (Main.netMode != NetmodeID.Server)
                {
                    // === Purple shadow dust trail ===
                    if (Main.rand.NextBool(2))
                    {
                        Vector2 vel = -Projectile.velocity * 0.2f + new Vector2(
                            Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-1f, 1f));
                        Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch,
                            vel, 150, new Color(120, 60, 180), 0.9f);
                        d.noGravity = true;
                        d.fadeIn = 0f;
                    }
                }

                Lighting.AddLight(Projectile.Center, new Vector3(0.15f, 0.05f, 0.25f));
            }
            catch { }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                Texture2D star = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Star").Value;
                Texture2D softGlow = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;

                Vector2 drawPos = Projectile.Center - Main.screenPosition;
                Vector2 starOrigin = new Vector2(star.Width / 2f, star.Height / 2f);
                Vector2 glowOrigin = new Vector2(softGlow.Width / 2f, softGlow.Height / 2f);

                float rot = Main.GameUpdateCount * 0.05f;
                float pulse = 0.8f + 0.2f * (float)Math.Sin(Main.GameUpdateCount * 0.15f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                // Dark purple halo (additive on a dark color = shadow-like aura)
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(50, 10, 90, 200) * pulse,
                    0f, glowOrigin, 1.2f * pulse, SpriteEffects.None, 0f);

                // Triple Star.png rotating with Color.Black alpha 150 (looks like a void star)
                for (int i = 0; i < 3; i++)
                {
                    float r = rot + i * MathHelper.TwoPi / 3f;
                    Color shadow = new Color(0, 0, 0, 150);
                    Main.spriteBatch.Draw(star, drawPos, null, shadow,
                        r, starOrigin, 0.9f * pulse, SpriteEffects.None, 0f);
                }

                // Faint violet rim on the shadow
                Main.spriteBatch.Draw(star, drawPos, null,
                    new Color(80, 30, 130, 80),
                    -rot, starOrigin, 1.0f * pulse, SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode == NetmodeID.Server) return;
            for (int i = 0; i < 10; i++)
            {
                float a = (MathHelper.TwoPi / 10) * i;
                Vector2 dir = new Vector2(
                    (float)Math.Cos(a) * Main.rand.NextFloat(2f, 4f),
                    (float)Math.Sin(a) * Main.rand.NextFloat(2f, 4f));
                Dust d = Dust.NewDustPerfect(target.Center, DustID.PurpleTorch,
                    dir, 200, new Color(150, 80, 220), 1.0f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }
    }
}
