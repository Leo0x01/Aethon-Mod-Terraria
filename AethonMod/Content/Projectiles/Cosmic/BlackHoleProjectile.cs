using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// BlackHoleProjectile — adaptado del código de Wrath of the Gods.
    /// Usa el shader RealBlackHoleShader.fx (75-step lightmarch con lensing gravitacional real).
    /// También spawnea partículas en espiral (CircularSuctionParticle pattern de WoTG).
    /// El proyectil se mueve lentamente (velocity *= 0.97f).
    /// </summary>
    public class BlackHoleProjectile : ModProjectile
    {
        private float SpinAngle { get => Projectile.ai[0]; set => Projectile.ai[0] = value; }
        private Ref<Effect> _blackHoleShader;
        private Ref<Effect> _distortionShader;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 76;
            Projectile.height = 76;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
        }

        public override void AI()
        {
            SpinAngle += 0.05f;

            // Movimiento lento (el agujero negro flota)
            Projectile.velocity *= 0.97f;

            // === PARTÍCULAS INFALLING EN ESPIRAL (patrón CircularSuction de WoTG) ===
            // Spawn en círculo rotando (patrón pinwheel)
            if (Main.netMode != NetmodeID.Server)
            {
                for (int i = 0; i < 3; i++)
                {
                    float angle = SpinAngle + i * (MathHelper.TwoPi / 3f);
                    float dist = Main.rand.NextFloat(90f, 140f);
                    Vector2 spawnPos = Projectile.Center + new Vector2(
                        (float)Math.Cos(angle) * dist,
                        (float)Math.Sin(angle) * dist);

                    // Velocidad inicial hacia el centro
                    Vector2 toCenter = Projectile.Center - spawnPos;
                    float speed = Main.rand.NextFloat(4f, 8f);
                    if (toCenter.LengthSquared() > 0.01f)
                    {
                        toCenter.Normalize();
                        Vector2 velocity = toCenter * speed;

                        // RotateTowards: rota velocity hacia línea central (crea espiral)
                        // Esto es LA técnica que convierte infalling radial en espiral
                        float angleToCenter = (float)Math.Atan2(toCenter.Y, toCenter.X);
                        velocity = velocity.RotateTowards(angleToCenter + MathHelper.PiOver2 * 0.3f, 0.5f);

                        // Color: caliente (naranja-blanco) cerca del centro, púrpura lejos
                        Color color;
                        if (dist < 60f)
                            color = new Color(255, 240, 180); // blanco-amarillo (hot)
                        else if (dist < 100f)
                            color = new Color(255, 160, 60);  // naranja
                        else
                            color = new Color(160, 80, 255);  // púrpura (frío)

                        Dust d = Dust.NewDustPerfect(spawnPos, DustID.PurpleTorch,
                            velocity, 150, color, 1.3f);
                        d.noGravity = true;
                        d.fadeIn = 0f;
                        d.scale = Main.rand.NextFloat(0.8f, 1.6f);
                    }
                }

                // === DISCO DE ACRECIÓN (partículas orbitando) ===
                // Spawn muy cerca del centro en plano aplanado (disco visto en ángulo)
                if (Main.rand.NextBool(2))
                {
                    float diskAngle = SpinAngle * 4f; // rota más rápido que las infalling
                    float diskRadius = Main.rand.NextFloat(10f, 40f);
                    Vector2 diskPos = Projectile.Center + new Vector2(
                        (float)Math.Cos(diskAngle) * diskRadius,
                        (float)Math.Sin(diskAngle) * diskRadius * 0.25f); // aplanado (disco en ángulo)

                    // Velocidad tangencial (orbitando, no cayendo)
                    Vector2 tangent = new Vector2(
                        -(float)Math.Sin(diskAngle),
                        (float)Math.Cos(diskAngle) * 0.25f) * 3f;

                    Dust d = Dust.NewDustPerfect(diskPos, DustID.GoldFlame,
                        tangent, 220, new Color(255, 210, 100), 1.2f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }

                // === POLVO Y HUMO (efectos de entorno) ===
                if (Main.rand.NextBool(5))
                {
                    float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                    float dist = Main.rand.NextFloat(120f, 180f);
                    Vector2 spawnPos = Projectile.Center + new Vector2(
                        (float)Math.Cos(angle) * dist,
                        (float)Math.Sin(angle) * dist);
                    Vector2 vel = (Projectile.Center - spawnPos) * 0.025f;
                    Dust d = Dust.NewDustPerfect(spawnPos, DustID.Smoke,
                        vel, 80, new Color(80, 40, 100), 0.8f);
                    d.noGravity = false;
                    d.fadeIn = 0f;
                }
            }

            // === ATRACCIÓN GRAVITACIONAL DE ENEMIGOS (como WoTG) ===
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy()) continue;
                Vector2 toCenter = Projectile.Center - npc.Center;
                float dist = toCenter.Length();
                if (dist > 350f || dist < 5f) continue;
                // Fuerza 1/r (más débil que 1/r² pero más visible)
                float strength = (1f - dist / 350f) * 2f;
                if (toCenter.LengthSquared() > 0.01f)
                {
                    toCenter.Normalize();
                    npc.velocity += toCenter * strength;
                }
            }

            // === ILUMINACIÓN ===
            float pulse = 0.8f + 0.2f * (float)Math.Sin(Main.GameUpdateCount * 0.08f);
            // Luz cálida del disco de acreción
            Lighting.AddLight(Projectile.Center, new Vector3(0.95f * pulse, 0.45f * pulse, 0.1f * pulse));
            // Halo púrpura
            Lighting.AddLight(Projectile.Center, new Vector3(0.2f * pulse, 0.1f * pulse, 0.4f * pulse));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Cargar shaders si no están cargados
            if (_blackHoleShader == null)
            {
                try { _blackHoleShader = new Ref<Effect>(ModContent.Request<Effect>("AethonMod/Content/Effects/Shaders/RealBlackHoleShader").Value); }
                catch { }
            }
            if (_distortionShader == null)
            {
                try { _distortionShader = new Ref<Effect>(ModContent.Request<Effect>("AethonMod/Content/Effects/Shaders/BlackHoleDistortionShader").Value); }
                catch { }
            }

            try
            {
                float t = Main.GameUpdateCount;
                float pulse = 0.9f + 0.1f * (float)Math.Sin(t * 0.08f);
                Vector2 drawPos = Projectile.Center - Main.screenPosition;

                // === 1. HALO EXTERNO PÚRPURA (glow difuso) ===
                Texture2D glowTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
                Main.spriteBatch.Draw(glowTex, drawPos, null,
                    new Color(60, 20, 90, 50),
                    0f, new Vector2(glowTex.Width / 2f, glowTex.Height / 2f),
                    3.0f * pulse, SpriteEffects.None, 0f);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

                // === 2. SHADER DE AGUJERO NEGRO (RealBlackHoleShader) ===
                // Este shader hace un lightmarch de 75 pasos con lensing gravitacional real
                if (_blackHoleShader != null && _blackHoleShader.Value != null)
                {
                    Effect shader = _blackHoleShader.Value;
                    Vector2 screenSize = new Vector2(Main.screenWidth, Main.screenHeight);
                    Vector2 bhUV = new Vector2(drawPos.X / screenSize.X, drawPos.Y / screenSize.Y);

                    shader.Parameters["blackHoleRadius"].SetValue(0.3f);
                    shader.Parameters["blackHoleCenter"].SetValue(Vector3.Zero);
                    shader.Parameters["aspectRatioCorrectionFactor"].SetValue(1f);
                    shader.Parameters["accretionDiskColor"].SetValue(new Color(245, 105, 61).ToVector3());
                    shader.Parameters["cameraAngle"].SetValue(0.32f);
                    shader.Parameters["cameraRotationAxis"].SetValue(new Vector3(1f, 0f, Projectile.rotation));
                    shader.Parameters["accretionDiskScale"].SetValue(new Vector3(1f, 0.33f, 1f));
                    shader.Parameters["zoom"].SetValue(Vector2.One * 0.12f * pulse);
                    shader.Parameters["accretionDiskRadius"].SetValue(Projectile.scale * 0.4f);
                    shader.Parameters["globalTime"].SetValue((float)Main.GameUpdateCount * 0.0167f);

                    // Usar Noise.png como textura de ruido para el disco
                    Texture2D noiseTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Noise").Value;
                    Main.graphics.GraphicsDevice.Textures[1] = noiseTex;
                    Main.graphics.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;

                    shader.CurrentTechnique.Passes[0].Apply();

                    // Dibujar un pixel invisible que el shader usa como canvas
                    Texture2D pixel = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                    Main.spriteBatch.Draw(pixel, drawPos, null, Color.White, 0f,
                        new Vector2(pixel.Width / 2f, pixel.Height / 2f),
                        5f, SpriteEffects.None, 0f);
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                }
                else
                {
                    // === FALLBACK: si el shader no carga, dibujar manualmente ===
                    DrawFallback(drawPos, pulse, t);
                }
            }
            catch { }
            return false;
        }

        private void DrawFallback(Vector2 drawPos, float pulse, float t)
        {
            Texture2D glowTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
            Texture2D vortexTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Vortex").Value;
            Texture2D ringTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Ring").Value;

            // Glow externo
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
            Main.spriteBatch.Draw(glowTex, drawPos, null,
                new Color(80, 30, 120, 60), 0f,
                new Vector2(glowTex.Width / 2f, glowTex.Height / 2f),
                2.5f * pulse, SpriteEffects.None, 0f);

            // Disco de acreción frontal (elíptico, naranja)
            Main.spriteBatch.Draw(vortexTex, drawPos, null,
                new Color(255, 180, 80, 200),
                SpinAngle * 2f,
                new Vector2(vortexTex.Width / 2f, vortexTex.Height / 2f),
                new Vector2(1.5f, 0.5f), SpriteEffects.None, 0f);

            // Disco trasero (Einstein ring)
            Main.spriteBatch.Draw(vortexTex, drawPos - new Vector2(0, 4f), null,
                new Color(200, 50, 0, 100),
                -SpinAngle * 2f,
                new Vector2(vortexTex.Width / 2f, vortexTex.Height / 2f),
                new Vector2(1.5f, 0.5f), SpriteEffects.FlipVertically, 0f);

            // Beaming relativístico (lado brillante)
            Main.spriteBatch.Draw(vortexTex, drawPos - new Vector2(3f, 0f), null,
                new Color(255, 230, 150, 130),
                SpinAngle * 2f,
                new Vector2(vortexTex.Width / 2f, vortexTex.Height / 2f),
                new Vector2(1.5f, 0.5f), SpriteEffects.None, 0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            // Event horizon (disco negro)
            Main.spriteBatch.Draw(glowTex, drawPos, null,
                Color.Black, 0f,
                new Vector2(glowTex.Width / 2f, glowTex.Height / 2f),
                0.8f, SpriteEffects.None, 0f);

            // Photon ring + aberración cromática
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
            // Rojo
            Main.spriteBatch.Draw(ringTex, drawPos - new Vector2(2f, 0f), null,
                new Color(255, 0, 0, 80), 0f,
                new Vector2(ringTex.Width / 2f, ringTex.Height / 2f),
                0.6f * pulse, SpriteEffects.None, 0f);
            // Verde
            Main.spriteBatch.Draw(ringTex, drawPos, null,
                new Color(0, 255, 0, 80), 0f,
                new Vector2(ringTex.Width / 2f, ringTex.Height / 2f),
                0.6f * pulse, SpriteEffects.None, 0f);
            // Azul
            Main.spriteBatch.Draw(ringTex, drawPos + new Vector2(2f, 0f), null,
                new Color(0, 100, 255, 80), 0f,
                new Vector2(ringTex.Width / 2f, ringTex.Height / 2f),
                0.6f * pulse, SpriteEffects.None, 0f);
            // Blanco
            Main.spriteBatch.Draw(ringTex, drawPos, null,
                new Color(255, 240, 200, 220), 0f,
                new Vector2(ringTex.Width / 2f, ringTex.Height / 2f),
                0.6f * pulse, SpriteEffects.None, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode == NetmodeID.Server) return;

            // Implosión + explosión (como WoTG)
            for (int i = 0; i < 50; i++)
            {
                float angle = (MathHelper.TwoPi / 50) * i + Main.rand.NextFloat(-0.2f, 0.2f);
                float dist = Main.rand.NextFloat(80f, 140f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist,
                    (float)Math.Sin(angle) * dist);
                Vector2 toCenter = Projectile.Center - spawnPos;
                if (toCenter.LengthSquared() > 0.01f)
                {
                    toCenter.Normalize();
                    Vector2 tangent = new Vector2(-toCenter.Y, toCenter.X) * 0.5f;
                    Vector2 vel = (toCenter * 7f + tangent * 4f);
                    Dust d = Dust.NewDustPerfect(spawnPos, DustID.PurpleTorch,
                        vel, 200, new Color(200, 100, 255), 1.3f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }

            // Explosión post-implosión
            for (int i = 0; i < 40; i++)
            {
                float angle = (MathHelper.TwoPi / 40) * i;
                Vector2 dir = new Vector2(
                    (float)Math.Cos(angle) * Main.rand.NextFloat(5f, 11f),
                    (float)Math.Sin(angle) * Main.rand.NextFloat(5f, 11f));
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                    dir, 220, new Color(255, 200, 100), 1.5f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Flash blanco
            for (int i = 0; i < 15; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Gold,
                    new Vector2(Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-4f, 4f)),
                    255, Color.White, 1.0f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
        }
    }

    public static class Vector2Extensions
    {
        public static Vector2 RotateTowards(this Vector2 current, float targetAngle, float maxStep)
        {
            float currentAngle = (float)Math.Atan2(current.Y, current.X);
            float diff = ((targetAngle - currentAngle + MathHelper.Pi * 3) % MathHelper.TwoPi) - MathHelper.Pi;
            if (Math.Abs(diff) <= maxStep)
                return new Vector2((float)Math.Cos(targetAngle), (float)Math.Sin(targetAngle)) * current.Length();
            float newAngle = currentAngle + Math.Sign(diff) * maxStep;
            return new Vector2((float)Math.Cos(newAngle), (float)Math.Sin(newAngle)) * current.Length();
        }
    }
}
