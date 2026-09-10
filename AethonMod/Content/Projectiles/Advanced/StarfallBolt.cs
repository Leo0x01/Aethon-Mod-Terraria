using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Particles;

namespace AethonMod.Content.Projectiles.Advanced
{
    /// <summary>
    /// StarfallBolt — estrella fugaz con estela dorada.
    /// Combina:
    /// - Star.png texturizado rotando
    /// - Trail continuo con oldPos[] (degradado dorado)
    /// - Partículas Star del ParticleManager como chispas
    /// - Trail.png estirado entre posiciones anteriores
    /// - Additive blending
    /// - Homing hacia enemigos
    /// </summary>
    public class StarfallBolt : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            // v5.74: configurar TrailCacheLength para que oldPos tenga 20 elementos
            ProjectileID.Sets.TrailCacheLength[Type] = 20;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 200;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            // Rotación de la estrella
            Projectile.rotation += 0.2f;

            // === PARTÍCULAS CHISPA CON PARTICLE MANAGER ===
            if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(2))
            {
                Vector2 offset = new Vector2(
                    Main.rand.NextFloat(-15f, 15f),
                    Main.rand.NextFloat(-15f, 15f));
                var data = new ParticleData
                {
                    Position = Projectile.Center + offset,
                    Velocity = -Projectile.velocity * 0.05f,
                    Scale = new Vector2(0.5f),
                    PackedColor = ParticleManager.PackColor(new Color(255, 220, 100)),
                    Rotation = Main.rand.NextFloat(0, MathHelper.TwoPi),
                    RotationSpeed = 0.3f,
                    TimeLeft = 20,
                    Duration = 20,
                    TextureId = 2, // Star
                    BlendMode = 1,
                    LayerPriority = LayerPriorities.BeforeProjectiles,
                };
                data.EnableComponent(ComponentFlag.FadeOut);
                data.EnableComponent(ComponentFlag.ScaleDown);
                ParticleManager.Spawn(data);
            }

            // Luz dorada
            Lighting.AddLight(Projectile.Center, new Vector3(1f, 0.85f, 0.3f));

            // === HOMING HACIA ENEMIGOS ===
            NPC target = FindClosestNPC(400f);
            if (target != null)
            {
                Vector2 dir = target.Center - Projectile.Center;
                // v5.74: usar LengthSquared > 0.0001f en vez de != Vector2.Zero
                if (dir.LengthSquared() > 0.0001f)
                {
                    dir.Normalize();
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, dir * 14f, 0.05f);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                float t = Main.GameUpdateCount;
                float pulse = 0.8f + 0.2f * (float)Math.Sin(t * 0.2f);

                // === 1. TRAIL CONTINUO CON oldPos[] ===
                Texture2D trailTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Trail").Value;
                if (trailTex != null)
                {
                    int trailLength = Math.Min(Projectile.oldPos.Length, 20);
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
                    for (int i = trailLength - 1; i > 0; i--)
                    {
                        if (Projectile.oldPos[i] == Vector2.Zero) continue;
                        Vector2 pos = Projectile.oldPos[i] + new Vector2(Projectile.width / 2f, Projectile.height / 2f);
                        Vector2 prevPos = Projectile.oldPos[i - 1] + new Vector2(Projectile.width / 2f, Projectile.height / 2f);
                        if (Projectile.oldPos[i - 1] == Vector2.Zero) prevPos = pos;

                        Vector2 dir = prevPos - pos;
                        float dist = dir.Length();
                        if (dist < 0.1f) continue;
                        dir.Normalize();

                        float angle = (float)Math.Atan2(dir.Y, dir.X);
                        float progress = i / (float)trailLength;
                        float alpha = (1f - progress) * 0.7f;
                        float scale = (1f - progress * 0.5f) * 0.5f;

                        Main.spriteBatch.Draw(trailTex,
                            pos - Main.screenPosition, null,
                            new Color(255, 220, 100, (byte)(255 * alpha)),
                            angle,
                            new Vector2(0, trailTex.Height / 2f),
                            new Vector2(dist / trailTex.Width, scale),
                            SpriteEffects.None, 0f);
                    }
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                }

                // === 2. ESTRELLA TEXTURIZADA ===
                Texture2D starTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Star").Value;
                if (starTex != null)
                {
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                    // Estrella dorada rotando
                    Main.spriteBatch.Draw(starTex,
                        Projectile.Center - Main.screenPosition, null,
                        new Color(255, 220, 100, 220),
                        Projectile.rotation,
                        new Vector2(starTex.Width / 2f, starTex.Height / 2f),
                        1.2f * pulse, SpriteEffects.None, 0f);

                    // Estrella blanca más pequeña (núcleo)
                    Main.spriteBatch.Draw(starTex,
                        Projectile.Center - Main.screenPosition, null,
                        new Color(255, 255, 255, 200),
                        -Projectile.rotation * 0.5f,
                        new Vector2(starTex.Width / 2f, starTex.Height / 2f),
                        0.7f * pulse, SpriteEffects.None, 0f);

                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                }

                // === 3. GLOW RADIAL ===
                Texture2D glowTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                if (glowTex != null)
                {
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
                    Main.spriteBatch.Draw(glowTex,
                        Projectile.Center - Main.screenPosition, null,
                        new Color(255, 220, 100, 120),
                        0f,
                        new Vector2(glowTex.Width / 2f, glowTex.Height / 2f),
                        0.8f * pulse, SpriteEffects.None, 0f);
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                }
            }
            catch { }
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode == NetmodeID.Server) return;

            // Explosión de estrellas doradas
            for (int i = 0; i < 15; i++)
            {
                float angle = (MathHelper.TwoPi / 15) * i;
                Vector2 dir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 5f;
                var data = new ParticleData
                {
                    Position = target.Center,
                    Velocity = dir,
                    Scale = new Vector2(0.8f),
                    PackedColor = ParticleManager.PackColor(new Color(255, 220, 100)),
                    Rotation = angle,
                    RotationSpeed = 0.4f,
                    TimeLeft = 30,
                    Duration = 30,
                    TextureId = 2, // Star
                    BlendMode = 1,
                    LayerPriority = LayerPriorities.AfterProjectiles,
                };
                data.EnableComponent(ComponentFlag.FadeOut);
                data.EnableComponent(ComponentFlag.ScaleDown);
                ParticleManager.Spawn(data);
            }
        }

        private NPC FindClosestNPC(float maxDist)
        {
            NPC closest = null;
            float minDist = maxDist;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy()) continue;
                float dist = Vector2.Distance(Projectile.Center, npc.Center);
                if (dist < minDist) { minDist = dist; closest = npc; }
            }
            return closest;
        }
    }
}
