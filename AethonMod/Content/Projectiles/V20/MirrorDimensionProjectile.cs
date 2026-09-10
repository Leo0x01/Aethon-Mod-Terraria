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
    /// MirrorDimensionProjectile — crea un duplicado en la posición reflejada
    /// del jugador a través del cursor. Ambos dibujan Crescent.png rotando
    /// con tint cyan. Al impactar, el partner se teleporta al target. Sparkles
    /// conectan las dos posiciones formando una línea.
    /// </summary>
    public class MirrorDimensionProjectile : ModProjectile
    {
        // ai[0] = spin angle
        // ai[1] = partner whoAmI + 1 (0 = unset)
        // localAI[0] = 0 original, 1 mirror

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
            Projectile.penetrate = 3;
            Projectile.timeLeft = 120;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            try
            {
                Projectile.ai[0] += 1f;

                // Slight drag so the projectile eventually settles
                Projectile.velocity *= 0.98f;

                // Find partner
                Projectile partner = null;
                int partnerIdx = (int)Projectile.ai[1] - 1;
                if (partnerIdx >= 0 && partnerIdx < Main.projectile.Length)
                {
                    Projectile p = Main.projectile[partnerIdx];
                    if (p.active && p.type == Projectile.type) partner = p;
                }

                if (Main.netMode != NetmodeID.Server)
                {
                    // === Sparkle line between self and partner ===
                    if (partner != null)
                    {
                        Vector2 a = Projectile.Center;
                        Vector2 b = partner.Center;
                        float len = (b - a).Length();
                        if (len > 1f)
                        {
                            // Spawn 1 sparkle at a random t along the line.
                            float t = Main.rand.NextFloat(0f, 1f);
                            Vector2 pos = a + (b - a) * t;
                            Dust d = Dust.NewDustPerfect(pos, DustID.Enchanted_Gold,
                                Vector2.Zero, 200, new Color(120, 230, 255), 0.7f);
                            d.noGravity = true;
                            d.fadeIn = 0f;
                        }
                    }

                    // Cyan sparkle dust trail
                    if (Main.rand.NextBool(4))
                    {
                        Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.AncientLight,
                            Projectile.velocity * 0.2f, 150, new Color(100, 220, 255), 0.8f);
                        d.noGravity = true;
                        d.fadeIn = 0f;
                    }
                }

                Lighting.AddLight(Projectile.Center, new Vector3(0.2f, 0.6f, 0.9f));
            }
            catch { }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            try
            {
                // Teleport the partner to the target's position so it can also strike.
                int partnerIdx = (int)Projectile.ai[1] - 1;
                if (partnerIdx >= 0 && partnerIdx < Main.projectile.Length)
                {
                    Projectile p = Main.projectile[partnerIdx];
                    if (p.active && p.type == Projectile.type)
                    {
                        p.Center = target.Center + new Vector2(
                            Main.rand.NextFloat(-20f, 20f),
                            Main.rand.NextFloat(-20f, 20f));
                        p.velocity = Vector2.Zero;
                    }
                }

                // Burst of cyan sparkles
                if (Main.netMode == NetmodeID.Server) return;
                for (int i = 0; i < 14; i++)
                {
                    float angle = (MathHelper.TwoPi / 14) * i;
                    Vector2 dir = new Vector2(
                        (float)Math.Cos(angle) * Main.rand.NextFloat(2f, 5f),
                        (float)Math.Sin(angle) * Main.rand.NextFloat(2f, 5f));
                    Dust d = Dust.NewDustPerfect(target.Center, DustID.Enchanted_Gold,
                        dir, 200, new Color(120, 230, 255), 1.0f);
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
                Texture2D crescent = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Crescent").Value;
                Texture2D softGlow = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;

                Vector2 drawPos = Projectile.Center - Main.screenPosition;
                Vector2 cOrigin = new Vector2(crescent.Width / 2f, crescent.Height / 2f);
                Vector2 gOrigin = new Vector2(softGlow.Width / 2f, softGlow.Height / 2f);

                // Mirror gets a slightly different hue so it's identifiable
                bool isMirror = Projectile.localAI[0] > 0.5f;
                Color tint = isMirror
                    ? new Color(180, 230, 255, 220)
                    : new Color(100, 200, 255, 220);

                float spin = Projectile.ai[0] * 0.12f;
                float pulse = 0.9f + 0.1f * (float)Math.Sin(Projectile.ai[0] * 0.2f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                // Outer cyan glow
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(80, 180, 255, 100) * pulse,
                    0f, gOrigin, 1.2f * pulse, SpriteEffects.None, 0f);

                // Triple rotated crescents create a kaleidoscopic mirror glyph.
                for (int i = 0; i < 3; i++)
                {
                    float rot = spin + i * MathHelper.TwoPi / 3f;
                    Main.spriteBatch.Draw(crescent, drawPos, null,
                        tint * 0.9f,
                        rot, cOrigin, 0.9f * pulse, SpriteEffects.None, 0f);
                }

                // White core
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(220, 240, 255, 200) * pulse,
                    0f, gOrigin, 0.5f * pulse, SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
            return false;
        }
    }
}
