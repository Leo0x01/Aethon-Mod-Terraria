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
    /// VortexProjectile — proyectil avanzado que combina:
    /// - Trail continuo con oldPos[] (técnica del libro sección 8.3)
    /// - Sistema de partículas data-oriented (ParticleManager)
    /// - Vórtice texturizado con rotación (Vortex.png procedural)
    /// - Anillos concéntricos rotando (Ring.png procedural)
    /// - Glow radial (SoftGlow.png procedural)
    /// - Additive blending en todos los draws
    /// </summary>
    public class VortexProjectile : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            // v5.74: configurar TrailCacheLength para que oldPos tenga 15 elementos
            ProjectileID.Sets.TrailCacheLength[Type] = 15;
        }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 180;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            // Rotación del vórtice
            Projectile.rotation += 0.12f;

            // === TRAIL CON PARTICLE MANAGER ===
            // Spawn partícula del ParticleManager cada frame (trail delgado)
            if (Main.netMode != NetmodeID.Server)
            {
                var trailData = new ParticleData
                {
                    Position = Projectile.Center,
                    Velocity = -Projectile.velocity * 0.1f,
                    Scale = new Vector2(0.6f),
                    PackedColor = ParticleManager.PackColor(new Color(180, 80, 255)),
                    Rotation = 0f,
                    RotationSpeed = 0f,
                    TimeLeft = 25,
                    Duration = 25,
                    TextureId = 0, // SoftGlow
                    BlendMode = 1, // Additive
                    LayerPriority = LayerPriorities.BeforeProjectiles,
                };
                trailData.EnableComponent(ComponentFlag.FadeOut);
                trailData.EnableComponent(ComponentFlag.ScaleDown);
                ParticleManager.Spawn(trailData);
            }

            // === PARTÍCULAS ORBITANDO (accretion disk) ===
            // Usar ParticleManager para partículas que orbitan
            if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(2))
            {
                float angle = Main.GameUpdateCount * 0.15f + Main.rand.NextFloat(0, MathHelper.TwoPi);
                float radius = 20f + Main.rand.NextFloat(0, 15f);
                Vector2 offset = new Vector2((float)Math.Cos(angle) * radius, (float)Math.Sin(angle) * radius);
                var orbitData = new ParticleData
                {
                    Position = Projectile.Center + offset,
                    Velocity = -offset * 0.03f, // atraer hacia el centro
                    Scale = new Vector2(0.4f),
                    PackedColor = ParticleManager.PackColor(new Color(200, 100, 255)),
                    Rotation = angle,
                    RotationSpeed = 0.1f,
                    TimeLeft = 20,
                    Duration = 20,
                    TextureId = 2, // Star
                    BlendMode = 1,
                    LayerPriority = LayerPriorities.BeforeProjectiles,
                };
                orbitData.EnableComponent(ComponentFlag.FadeOut);
                orbitData.EnableComponent(ComponentFlag.ScaleDown);
                ParticleManager.Spawn(orbitData);
            }

            // === PARTÍCULAS ABSORBIDAS DESDE LEJOS ===
            if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(4))
            {
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(50f, 80f);
                Vector2 spawnPos = Projectile.Center + new Vector2((float)Math.Cos(angle) * dist, (float)Math.Sin(angle) * dist);
                Vector2 vel = (Projectile.Center - spawnPos) * 0.04f;
                var absorbData = new ParticleData
                {
                    Position = spawnPos,
                    Velocity = vel,
                    Scale = new Vector2(0.5f),
                    PackedColor = ParticleManager.PackColor(new Color(150, 50, 200)),
                    Rotation = 0f,
                    RotationSpeed = 0f,
                    TimeLeft = 30,
                    Duration = 30,
                    TextureId = 0, // SoftGlow
                    BlendMode = 1,
                    LayerPriority = LayerPriorities.BeforeProjectiles,
                };
                absorbData.EnableComponent(ComponentFlag.FadeOut);
                ParticleManager.Spawn(absorbData);
            }

            // Luz púrpura pulsante
            float pulse = 0.8f + 0.2f * (float)Math.Sin(Main.GameUpdateCount * 0.15f);
            Lighting.AddLight(Projectile.Center, new Vector3(0.6f * pulse, 0.2f * pulse, 0.9f * pulse));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                float t = Main.GameUpdateCount;
                float pulse = 0.8f + 0.2f * (float)Math.Sin(t * 0.15f);

                // === 1. TRAIL CONTINUO CON oldPos[] ===
                // v5.74: hoist End/Begin fuera del loop (1 Begin/Additive + 1 End en vez de N)
                Texture2D trailTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Trail").Value;
                if (trailTex != null)
                {
                    int trailLength = Math.Min(Projectile.oldPos.Length, 15);
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
                        float alpha = (1f - progress) * 0.6f;
                        float scale = (1f - progress * 0.5f) * 0.4f;

                        Main.spriteBatch.Draw(trailTex,
                            pos - Main.screenPosition, null,
                            new Color(180, 80, 255, (byte)(255 * alpha)),
                            angle,
                            new Vector2(0, trailTex.Height / 2f),
                            new Vector2(dist / trailTex.Width, scale),
                            SpriteEffects.None, 0f);
                    }
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                }

                // === 2. VÓRTICE TEXTURIZADO ===
                Texture2D vortexTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Vortex").Value;
                if (vortexTex != null)
                {
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
                    // Vórtice principal rotando
                    Main.spriteBatch.Draw(vortexTex,
                        Projectile.Center - Main.screenPosition, null,
                        new Color(200, 100, 255, 180),
                        Projectile.rotation,
                        new Vector2(vortexTex.Width / 2f, vortexTex.Height / 2f),
                        0.8f * pulse, SpriteEffects.None, 0f);
                    // Vórtice secundario más pequeño, cyan, rotación inversa
                    Main.spriteBatch.Draw(vortexTex,
                        Projectile.Center - Main.screenPosition, null,
                        new Color(100, 200, 255, 120),
                        -Projectile.rotation * 0.7f,
                        new Vector2(vortexTex.Width / 2f, vortexTex.Height / 2f),
                        0.5f * pulse, SpriteEffects.None, 0f);
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                }

                // === 3. ANILLOS CONCÉNTRICOS ===
                Texture2D ringTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Ring").Value;
                if (ringTex != null)
                {
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
                    // Anillo 1: expandiendo
                    float ring1Scale = 0.3f + ((t * 0.04f) % 1f) * 1.2f;
                    float ring1Alpha = (1f - ((t * 0.04f) % 1f)) * 150f;
                    Main.spriteBatch.Draw(ringTex,
                        Projectile.Center - Main.screenPosition, null,
                        new Color(200, 100, 255, (int)ring1Alpha),
                        0f,
                        new Vector2(ringTex.Width / 2f, ringTex.Height / 2f),
                        ring1Scale, SpriteEffects.None, 0f);
                    // Anillo 2: offset de fase
                    float ring2Scale = 0.3f + ((t * 0.04f + 0.5f) % 1f) * 1.2f;
                    float ring2Alpha = (1f - ((t * 0.04f + 0.5f) % 1f)) * 120f;
                    Main.spriteBatch.Draw(ringTex,
                        Projectile.Center - Main.screenPosition, null,
                        new Color(255, 150, 200, (int)ring2Alpha),
                        0f,
                        new Vector2(ringTex.Width / 2f, ringTex.Height / 2f),
                        ring2Scale, SpriteEffects.None, 0f);
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                }

                // === 4. GLOW RADIAL DE FONDO ===
                Texture2D glowTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                if (glowTex != null)
                {
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
                    Main.spriteBatch.Draw(glowTex,
                        Projectile.Center - Main.screenPosition, null,
                        new Color(150, 50, 200, 100),
                        0f,
                        new Vector2(glowTex.Width / 2f, glowTex.Height / 2f),
                        1.2f * pulse, SpriteEffects.None, 0f);
                    // Núcleo blanco
                    Main.spriteBatch.Draw(glowTex,
                        Projectile.Center - Main.screenPosition, null,
                        new Color(255, 255, 255, 200),
                        0f,
                        new Vector2(glowTex.Width / 2f, glowTex.Height / 2f),
                        0.3f * pulse, SpriteEffects.None, 0f);
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                }
            }
            catch { }
            return false; // no dibujar sprite vanilla
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Implosión con ParticleManager
            if (Main.netMode == NetmodeID.Server) return;

            for (int i = 0; i < 25; i++)
            {
                float angle = (MathHelper.TwoPi / 25) * i;
                float dist = 50f + Main.rand.NextFloat(0, 20f);
                Vector2 spawnPos = target.Center + new Vector2((float)Math.Cos(angle) * dist, (float)Math.Sin(angle) * dist);
                Vector2 vel = (target.Center - spawnPos) * 0.12f;
                var data = new ParticleData
                {
                    Position = spawnPos,
                    Velocity = vel,
                    Scale = new Vector2(0.8f),
                    PackedColor = ParticleManager.PackColor(new Color(200, 100, 255)),
                    Rotation = angle,
                    RotationSpeed = 0.2f,
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
    }
}
