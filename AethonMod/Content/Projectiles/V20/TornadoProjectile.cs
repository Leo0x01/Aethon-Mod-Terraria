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
    /// TornadoProjectile — un tornado swirling con dust en espiral y multi-capas
    /// de Vortex.png formando un funnel (ancho arriba, estrecho abajo).
    /// </summary>
    public class TornadoProjectile : ModProjectile
    {
        private float SpinAngle { get => Projectile.ai[0]; set => Projectile.ai[0] = value; }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 120;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 180;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 25;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            try
            {
                SpinAngle += 0.18f;

                // Slow forward drift
                Projectile.velocity *= 0.985f;

                float funnelHeight = 110f; // total funnel height in pixels

                if (Main.netMode != NetmodeID.Server)
                {
                    // === SPIRAL DUST (BlueTorch) ===
                    // Multiple particles per frame in a spiral pattern rising up the funnel.
                    for (int i = 0; i < 4; i++)
                    {
                        // Each particle gets a phase-offset angle so they fill the spiral evenly.
                        float angle = SpinAngle + i * (MathHelper.Pi / 2f);
                        // Height factor 0..1 (0 = bottom, 1 = top). Particles rise over time.
                        float heightFactor = (float)(Main.GameUpdateCount % 40) / 40f + i * 0.07f;
                        heightFactor = heightFactor % 1f;
                        // Radius shrinks as particles rise (tornado is narrow at the bottom).
                        float radius = MathHelper.Lerp(45f, 8f, heightFactor);
                        Vector2 spawnOffset = new Vector2(
                            (float)Math.Cos(angle) * radius,
                            -heightFactor * funnelHeight * 0.5f);
                        Vector2 spawnPos = Projectile.Center + spawnOffset;

                        // Tangential + slight upward velocity (the spinning + rising motion).
                        Vector2 vel = new Vector2(
                            -(float)Math.Sin(angle) * 2.2f,
                            -0.6f - heightFactor * 0.4f);

                        Dust d = Dust.NewDustPerfect(spawnPos, DustID.BlueTorch,
                            vel, 180, new Color(120, 180, 255), 1.1f);
                        d.noGravity = true;
                        d.fadeIn = 0f;
                    }

                    // Ground-level debris dust (kicked up at the base of the funnel)
                    if (Main.rand.NextBool(3))
                    {
                        float a = Main.rand.NextFloat(0, MathHelper.TwoPi);
                        Vector2 pos = Projectile.Center + new Vector2(
                            (float)Math.Cos(a) * 50f, 50f);
                        Vector2 vel = new Vector2(
                            (float)Math.Cos(a) * 4f,
                            -Main.rand.NextFloat(2f, 5f));
                        Dust d = Dust.NewDustPerfect(pos, DustID.BlueTorch,
                            vel, 120, new Color(80, 140, 220), 1.0f);
                        d.noGravity = true;
                        d.fadeIn = 0f;
                    }
                }

                // Suction: pull nearby NPCs slightly toward the tornado center (lateral drag).
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.CanBeChasedBy()) continue;
                    Vector2 toCenter = Projectile.Center - npc.Center;
                    float dist = toCenter.Length();
                    if (dist > 120f || dist < 4f) continue;
                    if (toCenter.Length() > 0.1f)
                    {
                        toCenter.Normalize();
                        // Lateral pull only - lighter than a real black hole.
                        npc.velocity += toCenter * 0.4f;
                    }
                }

                // Cool blue lighting.
                Lighting.AddLight(Projectile.Center, new Vector3(0.2f, 0.45f, 0.9f));
            }
            catch { }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                Texture2D vortex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Vortex").Value;
                Texture2D softGlow = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;

                Vector2 basePos = Projectile.Center - Main.screenPosition + new Vector2(0f, 50f);
                Vector2 origin = new Vector2(vortex.Width / 2f, vortex.Height / 2f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                // === FUNNEL: 7 stacked Vortex layers ===
                // Bottom layer is smallest (narrow base), top is widest (funnel mouth).
                int layers = 7;
                float funnelHeight = 110f;
                for (int i = 0; i < layers; i++)
                {
                    float t = i / (float)(layers - 1); // 0 at bottom, 1 at top
                    float scale = MathHelper.Lerp(0.35f, 1.4f, t);
                    float yOffset = -t * funnelHeight;
                    // Counter-rotating alternating layers produce the twisting-funnel look.
                    float rot = SpinAngle * (i % 2 == 0 ? 1f : -1.2f) * 0.6f;
                    // Vertical squash so each disk looks like a horizontal slice of the funnel.
                    Vector2 scaleVec = new Vector2(scale, scale * 0.45f);
                    int alpha = (int)(180 - t * 60);
                    Color col = new Color(110, 170, 255, alpha);

                    Main.spriteBatch.Draw(vortex,
                        basePos + new Vector2(0f, yOffset), null,
                        col, rot, origin, scaleVec, SpriteEffects.None, 0f);
                }

                // === Ground dust cloud (small base glow) ===
                Vector2 glowOrigin = new Vector2(softGlow.Width / 2f, softGlow.Height / 2f);
                Main.spriteBatch.Draw(softGlow, basePos + new Vector2(0f, 6f), null,
                    new Color(70, 120, 200, 140),
                    0f, glowOrigin, 1.4f, SpriteEffects.None, 0f);

                // === Top cap glow (funnel mouth) ===
                Main.spriteBatch.Draw(softGlow, basePos + new Vector2(0f, -funnelHeight), null,
                    new Color(150, 200, 255, 120),
                    0f, glowOrigin, 1.8f, SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode == NetmodeID.Server) return;
            // Burst of blue sparkles on hit
            for (int i = 0; i < 14; i++)
            {
                float angle = (MathHelper.TwoPi / 14) * i;
                Vector2 dir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * Main.rand.NextFloat(3f, 6f);
                Dust d = Dust.NewDustPerfect(target.Center, DustID.BlueTorch,
                    dir, 200, new Color(160, 210, 255), 1.2f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }
    }
}
