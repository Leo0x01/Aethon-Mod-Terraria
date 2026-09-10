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
    /// PrismBeamProjectile — un rayo que se "separa" en 7 colores del arcoíris.
    /// Dibuja 7 BeamCyan/Gold a lo largo de la velocity, cada una tinted de un
    /// color del arcoíris y desfasadas perpendicularmente. Spawn Star sparkles.
    /// </summary>
    public class PrismBeamProjectile : ModProjectile
    {
        private static readonly Color[] Rainbow = new Color[]
        {
            new Color(255, 60, 60),    // red
            new Color(255, 150, 50),  // orange
            new Color(255, 240, 60),   // yellow
            new Color(80, 230, 90),    // green
            new Color(60, 220, 255),   // cyan
            new Color(80, 120, 255),   // blue
            new Color(190, 90, 255),   // purple
        };

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 30;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.extraUpdates = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            try
            {
                Projectile.ai[0] += 1f;

                if (Main.netMode == NetmodeID.Server) return;

                // === Star sparkles along the path ===
                if (Main.rand.NextBool(2))
                {
                    int colorIdx = Main.rand.Next(7);
                    Color c = Rainbow[colorIdx];
                    Vector2 perp = new Vector2(-Projectile.velocity.Y, Projectile.velocity.X);
                    if (perp.Length() > 0.1f)
                    {
                        perp.Normalize();
                        float offset = Main.rand.NextFloat(-8f, 8f);
                        Vector2 pos = Projectile.Center + perp * offset;
                        Dust d = Dust.NewDustPerfect(pos, DustID.Enchanted_Gold,
                            -Projectile.velocity * 0.1f, 200, c, 0.9f);
                        d.noGravity = true;
                        d.fadeIn = 0f;
                    }
                }

                // Rainbow glow (cycles color rapidly for visual variety)
                Color lc = Rainbow[(int)(Main.GameUpdateCount * 0.3f) % 7];
                Lighting.AddLight(Projectile.Center,
                    new Vector3(lc.R / 255f, lc.G / 255f, lc.B / 255f));
            }
            catch { }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                Texture2D beam = ModContent.Request<Texture2D>("AethonMod/Content/Effects/BeamCyan").Value;
                Texture2D star = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Star").Value;

                Vector2 drawPos = Projectile.Center - Main.screenPosition;
                Vector2 vel = Projectile.velocity;
                if (vel.Length() < 0.1f) vel = new Vector2(1f, 0f);
                Vector2 dir = Vector2.Normalize(vel);
                Vector2 perp = new Vector2(-dir.Y, dir.X);

                Vector2 beamOrigin = new Vector2(0f, beam.Height / 2f);
                Vector2 starOrigin = new Vector2(star.Width / 2f, star.Height / 2f);
                float rotation = (float)Math.Atan2(dir.Y, dir.X);

                float beamLength = 40f;

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                // === 7 rainbow beams offset perpendicular ===
                for (int i = 0; i < 7; i++)
                {
                    // Spread 7 beams across a perpendicular range; center beam at 0.
                    float perpOffset = (i - 3) * 4f;
                    Color c = Rainbow[i];
                    Vector2 start = drawPos + perp * perpOffset - dir * beamLength;
                    Main.spriteBatch.Draw(beam, start, null, c * 0.85f,
                        rotation, beamOrigin, new Vector2(beamLength / (float)beam.Width, 1.0f),
                        SpriteEffects.None, 0f);
                }

                // === Central white-hot core (full BeamGold, brighter) ===
                Texture2D beamGold = ModContent.Request<Texture2D>("AethonMod/Content/Effects/BeamGold").Value;
                Vector2 goldStart = drawPos - dir * beamLength;
                Vector2 goldOrigin = new Vector2(0f, beamGold.Height / 2f);
                Main.spriteBatch.Draw(beamGold, goldStart, null,
                    Color.White * 0.7f, rotation, goldOrigin,
                    new Vector2(beamLength / (float)beamGold.Width, 0.5f),
                    SpriteEffects.None, 0f);

                // === Star sparkle at the head ===
                Main.spriteBatch.Draw(star, drawPos, null,
                    Color.White * 0.9f, Main.GameUpdateCount * 0.3f, starOrigin,
                    1.0f, SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode == NetmodeID.Server) return;
            for (int i = 0; i < 14; i++)
            {
                Color c = Rainbow[i % 7];
                float a = (MathHelper.TwoPi / 14) * i;
                Vector2 dir = new Vector2(
                    (float)Math.Cos(a) * Main.rand.NextFloat(2f, 5f),
                    (float)Math.Sin(a) * Main.rand.NextFloat(2f, 5f));
                Dust d = Dust.NewDustPerfect(target.Center, DustID.Enchanted_Gold,
                    dir, 200, c, 1.0f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }
    }
}
