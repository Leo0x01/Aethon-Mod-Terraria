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
    /// BlackHoleMiniProjectile — un agujero negro en miniatura que orbita al
    /// jugador. Pequeña escala, tirón gravitacional débil, dust púrpura en
    /// espiral y disco de acreción naranja elíptico.
    /// </summary>
    public class BlackHoleMiniProjectile : ModProjectile
    {
        private float SpinAngle { get => Projectile.ai[0]; set => Projectile.ai[0] = value; }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
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
                Player player = Main.player[Projectile.owner];
                if (!player.active || player.dead) { Projectile.Kill(); return; }

                SpinAngle += 0.07f;

                // === ORBITA alrededor del jugador ===
                float orbitRadius = 100f;
                float angle = Main.GameUpdateCount * 0.05f;
                Vector2 offset = new Vector2(
                    (float)Math.Cos(angle) * orbitRadius,
                    (float)Math.Sin(angle) * orbitRadius);
                Projectile.Center = player.Center + offset;
                Projectile.velocity = Vector2.Zero;

                if (Main.netMode != NetmodeID.Server)
                {
                    // === SPIRAL PurpleTorch dust ===
                    for (int i = 0; i < 2; i++)
                    {
                        float a = SpinAngle + i * MathHelper.Pi;
                        float dist = Main.rand.NextFloat(40f, 60f);
                        Vector2 spawnPos = Projectile.Center + new Vector2(
                            (float)Math.Cos(a) * dist,
                            (float)Math.Sin(a) * dist);

                        Vector2 toCenter = Projectile.Center - spawnPos;
                        if (toCenter.Length() > 0.1f)
                        {
                            toCenter.Normalize();
                            Vector2 tangent = new Vector2(-toCenter.Y, toCenter.X) * 0.6f;
                            Vector2 vel = toCenter * 2.5f + tangent;
                            Dust d = Dust.NewDustPerfect(spawnPos, DustID.PurpleTorch,
                                vel, 150, new Color(170, 90, 255), 1.0f);
                            d.noGravity = true;
                            d.fadeIn = 0f;
                        }
                    }

                    // === Hot accretion disk dust ===
                    if (Main.rand.NextBool(2))
                    {
                        float diskA = SpinAngle * 3f;
                        float diskR = Main.rand.NextFloat(8f, 18f);
                        Vector2 diskPos = Projectile.Center + new Vector2(
                            (float)Math.Cos(diskA) * diskR,
                            (float)Math.Sin(diskA) * diskR * 0.3f);
                        Vector2 tangent = new Vector2(
                            -(float)Math.Sin(diskA),
                            (float)Math.Cos(diskA) * 0.3f) * 1.2f;
                        Dust d = Dust.NewDustPerfect(diskPos, DustID.GoldFlame,
                            tangent, 200, new Color(255, 200, 100), 0.8f);
                        d.noGravity = true;
                        d.fadeIn = 0f;
                    }
                }

                // === PULL enemies (weaker than full black hole) ===
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.CanBeChasedBy()) continue;
                    Vector2 toCenter = Projectile.Center - npc.Center;
                    float dist = toCenter.Length();
                    if (dist > 150f || dist < 4f) continue;
                    if (toCenter.Length() > 0.1f)
                    {
                        toCenter.Normalize();
                        float strength = (1f - dist / 150f) * 0.6f;
                        npc.velocity += toCenter * strength;
                        // v5.78: removed NPC velocity cap (was capping existing velocity)
                    }
                }

                // === Lighting ===
                float pulse = 0.8f + 0.2f * (float)Math.Sin(Main.GameUpdateCount * 0.12f);
                Lighting.AddLight(Projectile.Center,
                    new Vector3(0.7f * pulse, 0.35f * pulse, 0.1f * pulse));
            }
            catch { }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                Texture2D glow = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                Texture2D vortex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Vortex").Value;
                Texture2D ring = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Ring").Value;

                Vector2 drawPos = Projectile.Center - Main.screenPosition;
                Vector2 glowOrigin = new Vector2(glow.Width / 2f, glow.Height / 2f);
                Vector2 vortexOrigin = new Vector2(vortex.Width / 2f, vortex.Height / 2f);
                Vector2 ringOrigin = new Vector2(ring.Width / 2f, ring.Height / 2f);
                float pulse = 0.8f + 0.2f * (float)Math.Sin(Main.GameUpdateCount * 0.12f);

                // === Outer purple halo ===
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
                Main.spriteBatch.Draw(glow, drawPos, null,
                    new Color(70, 25, 110, 50),
                    0f, glowOrigin, 1.5f * pulse, SpriteEffects.None, 0f);

                // === Accretion disk (Vortex, elliptical orange) ===
                Main.spriteBatch.Draw(vortex, drawPos, null,
                    new Color(255, 170, 70, 200),
                    SpinAngle * 2f, vortexOrigin,
                    new Vector2(0.9f, 0.3f), SpriteEffects.None, 0f);
                // Back side (Einstein ring half) - dimmer red
                Main.spriteBatch.Draw(vortex, drawPos - new Vector2(0f, 2f), null,
                    new Color(180, 40, 0, 80),
                    -SpinAngle * 2f, vortexOrigin,
                    new Vector2(0.9f, 0.3f), SpriteEffects.FlipVertically, 0f);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

                // === Event horizon (black SoftGlow, smaller scale than full BH) ===
                Main.spriteBatch.Draw(glow, drawPos, null,
                    Color.Black, 0f, glowOrigin, 0.45f, SpriteEffects.None, 0f);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

                // === Photon ring (Ring white, additive) ===
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
                // v5.94 - fix 16x: Ring.png paso de 64px a 1024px (v5.93).
                Main.spriteBatch.Draw(ring, drawPos, null,
                    new Color(255, 240, 200, 220),
                    0f, ringOrigin, (0.35f / 16f) * pulse, SpriteEffects.None, 0f);
                // Bright core
                Main.spriteBatch.Draw(glow, drawPos, null,
                    new Color(255, 200, 100, 150 * pulse),
                    0f, glowOrigin, 0.18f * pulse, SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode == NetmodeID.Server) return;
            for (int i = 0; i < 16; i++)
            {
                float angle = (MathHelper.TwoPi / 16) * i;
                Vector2 dir = new Vector2(
                    (float)Math.Cos(angle) * Main.rand.NextFloat(3f, 6f),
                    (float)Math.Sin(angle) * Main.rand.NextFloat(3f, 6f));
                Dust d = Dust.NewDustPerfect(target.Center, DustID.PurpleTorch,
                    dir, 200, new Color(200, 130, 255), 1.1f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }
    }
}
