using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.V20
{
    /// <summary>
    /// VortexChainProjectile — proyectil principal que avanza dejando un rastro
    /// de mini-vórtices (VortexMineProjectile). Cada 8 frames genera una mina.
    ///
    /// Visuales:
    ///   - SoftGlow con Star.png superpuesto (cometa de vacío).
    ///   - TrailCacheLength = 10 (rastro del proyectil principal).
    ///
    /// Físicas:
    ///   - penetrate = 5, timeLeft = 120.
    ///   - Cada 8 frames, spawnea VortexMineProjectile (estacionaria, 30 frames).
    /// </summary>
    public class VortexChainProjectile : ModProjectile
    {
        private float Timer { get => Projectile.ai[0]; set => Projectile.ai[0] = value; }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.tileCollide = true;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 5;
            Projectile.timeLeft = 120;
            Projectile.light = 0.6f;
            Projectile.alpha = 0;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.ignoreWater = true;
        }

        public override bool? CanCutTiles() => true;

        public override void AI()
        {
            try
            {
                Timer += 1f;
                Projectile.rotation += 0.2f;

                // Spawn a vortex mine every 8 frames
                if (Timer % 8f == 0f && Main.netMode != NetmodeID.Server)
                {
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center,
                        Vector2.Zero,
                        ModContent.ProjectileType<VortexMineProjectile>(),
                        (int)(Projectile.damage * 0.6f),
                        1f,
                        Projectile.owner);
                }

                // Trail dust
                if (Main.rand.NextBool(3))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch,
                        -Projectile.velocity * 0.1f, 150, new Color(180, 120, 255), 0.9f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }

                // Slight gravity-style homing toward enemies
                NPC target = null;
                float minDist = 400f;
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.CanBeChasedBy()) continue;
                    float d = (npc.Center - Projectile.Center).Length();
                    if (d < minDist) { minDist = d; target = npc; }
                }
                if (target != null && minDist > 0.1f)
                {
                    Vector2 toTarget = (target.Center - Projectile.Center);
                    if (toTarget.Length() > 0.1f)
                    {
                        toTarget.Normalize();
                        Projectile.velocity += toTarget * 0.15f;
                        if (Projectile.velocity.Length() > 14f)
                            Projectile.velocity = Vector2.Normalize(Projectile.velocity) * 14f;
                    }
                }

                // Light
                Lighting.AddLight(Projectile.Center, new Vector3(0.4f, 0.2f, 0.9f));
            }
            catch { }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            try
            {
                for (int i = 0; i < 8; i++)
                {
                    float a = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                    float s = Main.rand.NextFloat(2f, 5f);
                    Vector2 v = new Vector2((float)Math.Cos(a) * s, (float)Math.Sin(a) * s);
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch,
                        v, 200, new Color(180, 120, 255), 1.1f);
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
                Texture2D softGlow = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                Texture2D star = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Star").Value;
                if (softGlow == null || star == null) return false;

                Vector2 origin = new Vector2(softGlow.Width / 2f, softGlow.Height / 2f);
                Vector2 starOrigin = new Vector2(star.Width / 2f, star.Height / 2f);
                float t = Main.GameUpdateCount;
                float pulse = 0.85f + 0.15f * (float)Math.Sin(t * 0.2f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                // === Trail (oldPos) ===
                int trailLen = ProjectileID.Sets.TrailCacheLength[Type];
                for (int i = 0; i < trailLen; i++)
                {
                    if (Projectile.oldPos[i] == Vector2.Zero) continue;
                    float progress = (float)i / (float)trailLen;
                    float alpha = (1f - progress) * 0.45f;
                    float scale = (1f - progress * 0.5f) * 0.5f;
                    Vector2 trailPos = Projectile.oldPos[i] + new Vector2(Projectile.width / 2f, Projectile.height / 2f) - Main.screenPosition;
                    Main.spriteBatch.Draw(softGlow, trailPos, null,
                        new Color(140, 80, 220) * alpha, 0f, origin, scale, SpriteEffects.None, 0f);
                }

                Vector2 drawPos = Projectile.Center - Main.screenPosition;

                // Outer purple halo
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(160, 100, 240, 180) * pulse,
                    0f, origin, 1.2f * pulse, SpriteEffects.None, 0f);
                // Star overlay (rotating)
                Main.spriteBatch.Draw(star, drawPos, null,
                    new Color(220, 200, 255, 200),
                    Projectile.rotation, starOrigin, 0.8f, SpriteEffects.None, 0f);
                // White-hot core
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(255, 250, 255, 230),
                    0f, origin, 0.4f * pulse, SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
            return false;
        }
    }

    /// <summary>
    /// VortexMineProjectile — mini-vórtice estacionario generado por
    /// VortexChainProjectile. Dura 30 frames, atrae enemigos en radio 100px.
    /// </summary>
    public class VortexMineProjectile : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 30;
            Projectile.light = 0f;
            Projectile.alpha = 0;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
            Projectile.ignoreWater = true;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            try
            {
                Projectile.rotation += 0.25f;

                // Pull enemies in 100px range
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.CanBeChasedBy()) continue;
                    Vector2 toCenter = Projectile.Center - npc.Center;
                    float dist = toCenter.Length();
                    if (dist > 100f || dist < 5f) continue;
                    if (toCenter.Length() > 0.1f)
                    {
                        toCenter.Normalize();
                        float strength = (1f - dist / 100f) * 0.8f;
                        npc.velocity += toCenter * strength;
                        if (npc.velocity.Length() > 10f)
                            npc.velocity = Vector2.Normalize(npc.velocity) * 10f;
                    }
                }

                // Dust spiral inward
                if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(2))
                {
                    float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                    float dist = 40f;
                    Vector2 spawnPos = Projectile.Center + new Vector2(
                        (float)Math.Cos(angle) * dist,
                        (float)Math.Sin(angle) * dist);
                    Vector2 toCenter = Projectile.Center - spawnPos;
                    if (toCenter.Length() > 0.1f)
                    {
                        toCenter.Normalize();
                        Vector2 tangent = new Vector2(-toCenter.Y, toCenter.X) * 0.5f;
                        Dust d = Dust.NewDustPerfect(spawnPos, DustID.PurpleTorch,
                            toCenter * 1.5f + tangent, 150, new Color(150, 100, 220), 0.8f);
                        d.noGravity = true;
                        d.fadeIn = 0f;
                    }
                }

                Lighting.AddLight(Projectile.Center, new Vector3(0.3f, 0.15f, 0.7f));
            }
            catch { }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                Texture2D vortex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Vortex").Value;
                Texture2D softGlow = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                if (vortex == null || softGlow == null) return false;

                Vector2 drawPos = Projectile.Center - Main.screenPosition;
                Vector2 origin = new Vector2(vortex.Width / 2f, vortex.Height / 2f);
                Vector2 glowOrigin = new Vector2(softGlow.Width / 2f, softGlow.Height / 2f);
                float fade = (float)Projectile.timeLeft / 30f;
                fade = MathHelper.Clamp(fade, 0f, 1f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                // Outer halo
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(120, 80, 200, 180) * fade,
                    0f, glowOrigin, 1.0f * fade, SpriteEffects.None, 0f);

                // Vortex (rotating)
                Main.spriteBatch.Draw(vortex, drawPos, null,
                    new Color(180, 120, 255, 200) * fade,
                    Projectile.rotation, origin, 0.7f * fade, SpriteEffects.None, 0f);

                // Counter-rotating inner
                Main.spriteBatch.Draw(vortex, drawPos, null,
                    new Color(220, 200, 255, 150) * fade,
                    -Projectile.rotation * 1.5f, origin, 0.35f * fade, SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
            return false;
        }
    }
}
