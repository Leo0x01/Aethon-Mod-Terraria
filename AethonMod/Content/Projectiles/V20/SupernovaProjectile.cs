using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.V20
{
    /// <summary>
    /// SupernovaProjectile — estrella que colapsa y luego explota.
    ///
    /// Fases:
    ///   - Phase 1 (frames 0-60): contrae, atrae enemigos hacia el centro,
    ///     dibuja SoftGlow cada vez más pequeño y más blanco (white-hot),
    ///     genera GoldFlame dust en espiral hacia dentro.
    ///   - Phase 2 (frame 60): EXPLOSIÓN MASIVA: Ring.png expansivo, 50 dust
    ///     outward, flash con GlowCircleWhite.
    ///   - Phase 3 (frames 61-90): glow desvaneciente.
    ///
    /// timeLeft = 90, penetrate = -1, tileCollide = false.
    /// </summary>
    public class SupernovaProjectile : ModProjectile
    {
        private float Age { get => Projectile.ai[0]; set => Projectile.ai[0] = value; }
        private bool Exploded { get => Projectile.ai[1] > 0f; set => Projectile.ai[1] = value ? 1f : 0f; }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 80;
            Projectile.height = 80;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 90;
            Projectile.light = 0f;
            Projectile.alpha = 0;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
            Projectile.ignoreWater = true;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            try
            {
                Age += 1f;
                Projectile.velocity *= 0.92f;

                // === Phase 1: collapse (0-60) ===
                if (Age < 60f)
                {
                    // Pull enemies toward center
                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        if (!npc.CanBeChasedBy()) continue;
                        Vector2 toCenter = Projectile.Center - npc.Center;
                        float dist = toCenter.Length();
                        if (dist > 280f || dist < 5f) continue;
                        float strength = (1f - dist / 280f) * 1.2f;
                        if (toCenter.Length() > 0.1f)
                        {
                            toCenter.Normalize();
                            npc.velocity += toCenter * strength;
                            // v5.78: removed NPC velocity cap (was capping existing velocity)
                        }
                    }

                    // GoldFlame dust spiraling inward (2 per frame)
                    if (Main.netMode != NetmodeID.Server)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            float angle = Age * 0.18f + i * MathHelper.Pi;
                            float dist = 70f + (60f - Age) * 0.6f;
                            Vector2 spawnPos = Projectile.Center + new Vector2(
                                (float)Math.Cos(angle) * dist,
                                (float)Math.Sin(angle) * dist);
                            Vector2 toCenter = Projectile.Center - spawnPos;
                            if (toCenter.Length() > 0.1f)
                            {
                                toCenter.Normalize();
                                Vector2 tangent = new Vector2(-toCenter.Y, toCenter.X) * 0.8f;
                                Vector2 vel = toCenter * 4f + tangent;
                                Dust d = Dust.NewDustPerfect(spawnPos, DustID.GoldFlame,
                                    vel, 200, new Color(255, 220, 150), 1.1f);
                                d.noGravity = true;
                                d.fadeIn = 0f;
                            }
                        }
                    }

                    // Light grows brighter as the star collapses
                    float collapse = Age / 60f;
                    Lighting.AddLight(Projectile.Center,
                        new Vector3(1f, 0.9f - 0.2f * collapse, 0.6f - 0.4f * collapse));
                }
                // === Phase 2: explosion (frame 60) ===
                else if (!Exploded)
                {
                    Exploded = true;
                    DoExplosion();
                }
                // === Phase 3: fading glow (61-90) ===
                else
                {
                    float fadeProgress = (Age - 60f) / 30f;
                    float fade = 1f - fadeProgress;
                    Lighting.AddLight(Projectile.Center,
                        new Vector3(1f * fade, 0.6f * fade, 0.2f * fade));
                }
            }
            catch { }
        }

        private void DoExplosion()
        {
            try
            {
                // 50 dust outward
                if (Main.netMode != NetmodeID.Server)
                {
                    for (int i = 0; i < 50; i++)
                    {
                        float angle = (MathHelper.TwoPi / 50f) * i + Main.rand.NextFloat(-0.1f, 0.1f);
                        float speed = Main.rand.NextFloat(6f, 12f);
                        Vector2 vel = new Vector2(
                            (float)Math.Cos(angle) * speed,
                            (float)Math.Sin(angle) * speed);
                        Color color = Main.rand.NextBool(2)
                            ? new Color(255, 230, 150)
                            : new Color(255, 160, 60);
                        Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                            vel, 240, color, 1.6f);
                        d.noGravity = true;
                        d.fadeIn = 0f;
                    }
                    // 15 white sparks
                    for (int i = 0; i < 15; i++)
                    {
                        Vector2 v = new Vector2(
                            Main.rand.NextFloat(-8f, 8f),
                            Main.rand.NextFloat(-8f, 8f));
                        Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Gold,
                            v, 255, Color.White, 1.0f);
                        d.noGravity = true;
                        d.fadeIn = 0f;
                    }
                }

                // AoE damage in 200px radius (visual-ish: deal damage via direct hits)
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.CanBeChasedBy()) continue;
                    float dist = (npc.Center - Projectile.Center).Length();
                    if (dist < 220f)
                    {
                        npc.SimpleStrikeNPC(Projectile.damage, npc.direction,
                            false, Projectile.knockBack, DamageClass.Magic);
                    }
                }

                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            }
            catch { }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                Texture2D softGlow = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                Texture2D ring = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Ring").Value;
                Texture2D glowCircleWhite = ModContent.Request<Texture2D>("AethonMod/Content/Effects/GlowCircleWhite").Value;
                if (softGlow == null || ring == null || glowCircleWhite == null) return false;

                Vector2 drawPos = Projectile.Center - Main.screenPosition;
                Vector2 glowOrigin = new Vector2(softGlow.Width / 2f, softGlow.Height / 2f);
                Vector2 ringOrigin = new Vector2(ring.Width / 2f, ring.Height / 2f);
                Vector2 whiteOrigin = new Vector2(glowCircleWhite.Width / 2f, glowCircleWhite.Height / 2f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                if (Age < 60f)
                {
                    // === Phase 1: shrinking + brightening ===
                    float collapse = Age / 60f;
                    float scale = 2.0f - collapse * 1.4f; // 2.0 → 0.6
                    // Outer halo: shifts from gold to white as collapse→1
                    int r = 255;
                    int g = (int)(180 + 75 * collapse);
                    int b = (int)(80 + 175 * collapse);
                    Color halo = new Color(r, g, b, 220);
                    Main.spriteBatch.Draw(softGlow, drawPos, null, halo, 0f, glowOrigin, scale, SpriteEffects.None, 0f);
                    // Inner white-hot core
                    float coreScale = 0.4f + collapse * 0.2f;
                    Color core = new Color(255, 255, 255, 240);
                    Main.spriteBatch.Draw(softGlow, drawPos, null, core, 0f, glowOrigin, coreScale, SpriteEffects.None, 0f);
                }
                else if (Age < 62f)
                {
                    // === Phase 2: explosion ring ===
                    float flashStrength = 1f - (Age - 60f) * 0.5f;
                    flashStrength = MathHelper.Clamp(flashStrength, 0f, 1f);
                    // Massive expanding Ring
                    float ringScale = 1.0f + (Age - 60f) * 2.5f;
                    Main.spriteBatch.Draw(ring, drawPos, null,
                        new Color(255, 230, 180, (byte)(220 * flashStrength)),
                        0f, ringOrigin, ringScale, SpriteEffects.None, 0f);
                    // GlowCircleWhite flash
                    float flashScale = 3.5f * flashStrength + 0.5f;
                    Main.spriteBatch.Draw(glowCircleWhite, drawPos, null,
                        new Color(255, 255, 255, (byte)(255 * flashStrength)),
                        0f, whiteOrigin, flashScale, SpriteEffects.None, 0f);
                }
                else
                {
                    // === Phase 3: fading glow ===
                    float fadeProgress = MathHelper.Clamp((Age - 60f) / 30f, 0f, 1f);
                    float fade = 1f - fadeProgress;
                    // Fading outer ring (still expanding slowly)
                    float ringScale = 6.0f + (Age - 60f) * 0.8f;
                    Main.spriteBatch.Draw(ring, drawPos, null,
                        new Color(255, 180, 80, (byte)(160 * fade)),
                        0f, ringOrigin, ringScale, SpriteEffects.None, 0f);
                    // Soft glow core fading
                    Main.spriteBatch.Draw(softGlow, drawPos, null,
                        new Color(255, 220, 140, (byte)(200 * fade)),
                        0f, glowOrigin, 1.5f * fade + 0.3f, SpriteEffects.None, 0f);
                }

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
            return false;
        }
    }
}
