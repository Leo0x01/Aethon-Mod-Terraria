using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// BlackHoleProjectile — reescrito siguiendo EXACTAMENTE el código de WoTG.
    /// Usa RealBlackHoleShader.fx (75-step lightmarch con lensing gravitacional real)
    /// + FireNoiseB.png como textura de ruido del disco de acreción.
    /// También spawnea partículas en espiral (CircularSuctionPattern de WoTG).
    /// </summary>
    public class BlackHoleProjectile : ModProjectile
    {
        private Ref<Effect> _shader;

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
            // Movimiento lento (el agujero negro flota)
            Projectile.velocity *= 0.97f;
            Projectile.rotation += 0.05f;

            // === PARTÍCULAS INFALLING EN ESPIRAL (patrón CircularSuction de WoTG) ===
            if (Main.netMode != NetmodeID.Server)
            {
                for (int i = 0; i < 3; i++)
                {
                    float angle = Projectile.rotation + i * (MathHelper.TwoPi / 3f);
                    float dist = Main.rand.NextFloat(90f, 140f);
                    Vector2 spawnPos = Projectile.Center + new Vector2(
                        (float)Math.Cos(angle) * dist,
                        (float)Math.Sin(angle) * dist);

                    Vector2 toCenter = Projectile.Center - spawnPos;
                    float speed = Main.rand.NextFloat(4f, 8f);
                    if (toCenter.LengthSquared() > 0.01f)
                    {
                        toCenter.Normalize();
                        Vector2 velocity = toCenter * speed;

                        // RotateTowards: LA técnica que convierte infalling radial en espiral
                        float angleToCenter = (float)Math.Atan2(toCenter.Y, toCenter.X);
                        velocity = velocity.RotateTowards(angleToCenter + MathHelper.PiOver2 * 0.3f, 0.5f);

                        // Color: caliente cerca del centro, púrpura lejos
                        Color color = dist < 60f ? new Color(255, 240, 180)
                                   : dist < 100f ? new Color(255, 160, 60)
                                   : new Color(160, 80, 255);

                        Dust d = Dust.NewDustPerfect(spawnPos, DustID.PurpleTorch,
                            velocity, 150, color, 1.3f);
                        d.noGravity = true;
                        d.fadeIn = 0f;
                        d.scale = Main.rand.NextFloat(0.8f, 1.6f);
                    }
                }

                // === DISCO DE ACRECIÓN (partículas GoldFlame orbitando) ===
                if (Main.rand.NextBool(2))
                {
                    float diskAngle = Projectile.rotation * 4f;
                    float diskRadius = Main.rand.NextFloat(10f, 40f);
                    Vector2 diskPos = Projectile.Center + new Vector2(
                        (float)Math.Cos(diskAngle) * diskRadius,
                        (float)Math.Sin(diskAngle) * diskRadius * 0.25f);
                    Vector2 tangent = new Vector2(
                        -(float)Math.Sin(diskAngle),
                        (float)Math.Cos(diskAngle) * 0.25f) * 3f;
                    Dust d = Dust.NewDustPerfect(diskPos, DustID.GoldFlame,
                        tangent, 220, new Color(255, 210, 100), 1.2f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }

                // === POLVO Y HUMO ===
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

            // === ATRACCIÓN GRAVITACIONAL DE ENEMIGOS ===
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy()) continue;
                Vector2 toCenter = Projectile.Center - npc.Center;
                float dist = toCenter.Length();
                if (dist > 350f || dist < 5f) continue;
                float strength = (1f - dist / 350f) * 2f;
                if (toCenter.LengthSquared() > 0.01f)
                {
                    toCenter.Normalize();
                    npc.velocity += toCenter * strength;
                }
            }

            // === ILUMINACIÓN ===
            float pulse = 0.8f + 0.2f * (float)Math.Sin(Main.GameUpdateCount * 0.08f);
            Lighting.AddLight(Projectile.Center, new Vector3(0.95f * pulse, 0.45f * pulse, 0.1f * pulse));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (_shader == null)
            {
                try { _shader = new Ref<Effect>(ModContent.Request<Effect>("AethonMod/Content/Effects/Shaders/RealBlackHoleShader").Value); }
                catch { }
            }

            try
            {
                Vector2 drawPos = Projectile.Center - Main.screenPosition;

                // === 1. HALO EXTERNO PÚRPURA ===
                Texture2D glowTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
                Main.spriteBatch.Draw(glowTex, drawPos, null,
                    new Color(60, 20, 90, 50), 0f,
                    new Vector2(glowTex.Width / 2f, glowTex.Height / 2f),
                    3.0f, SpriteEffects.None, 0f);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

                // === 2. SHADER REALBLACKHOLE (exactamente como WoTG) ===
                if (_shader != null && _shader.Value != null)
                {
                    Effect shader = _shader.Value;

                    // Parámetros EXACTOS de WoTG BlackHole.DrawBlackHole()
                    shader.Parameters["blackHoleRadius"].SetValue(0.3f);
                    shader.Parameters["blackHoleCenter"].SetValue(Vector3.Zero);
                    shader.Parameters["aspectRatioCorrectionFactor"].SetValue(1f);
                    shader.Parameters["accretionDiskColor"].SetValue(new Color(245, 105, 61).ToVector3());
                    shader.Parameters["cameraAngle"].SetValue(0.32f);
                    shader.Parameters["cameraRotationAxis"].SetValue(new Vector3(1f, 0f, Projectile.rotation));
                    shader.Parameters["accretionDiskScale"].SetValue(new Vector3(1f, 0.33f, 1f));
                    shader.Parameters["zoom"].SetValue(Vector2.One * 0.12f);
                    shader.Parameters["accretionDiskRadius"].SetValue(0.33f);
                    shader.Parameters["globalTime"].SetValue((float)Main.GameUpdateCount * 0.0167f);

                    // FireNoiseB como textura de ruido del disco (exactamente como WoTG)
                    Texture2D fireNoise = ModContent.Request<Texture2D>("AethonMod/Content/Effects/WoTG/FireNoiseB").Value;
                    Main.graphics.GraphicsDevice.Textures[1] = fireNoise;
                    Main.graphics.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;

                    // InvisiblePixel como canvas (exactamente como WoTG)
                    Texture2D invisiblePixel = ModContent.Request<Texture2D>("AethonMod/Content/Effects/WoTG/InvisiblePixel").Value;

                    // Orden correcto: Begin(Immediate) → Apply → Draw → End → Begin(Deferred)
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                    shader.CurrentTechnique.Passes[0].Apply();
                    Main.spriteBatch.Draw(invisiblePixel, drawPos, null, Color.Transparent, 0f,
                        new Vector2(invisiblePixel.Width / 2f, invisiblePixel.Height / 2f),
                        400f, SpriteEffects.None, 0f);
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                }
                else
                {
                    // === FALLBACK: si el shader no carga ===
                    DrawFallback(drawPos);
                }
            }
            catch { }
            return false;
        }

        private void DrawFallback(Vector2 drawPos)
        {
            float pulse = 0.9f + 0.1f * (float)Math.Sin(Main.GameUpdateCount * 0.08f);
            Texture2D glowTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
            Texture2D vortexTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Vortex").Value;
            Texture2D ringTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Ring").Value;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

            // Disco de acreción frontal (elíptico, naranja)
            Main.spriteBatch.Draw(vortexTex, drawPos, null,
                new Color(255, 180, 80, 200), Projectile.rotation * 2f,
                new Vector2(vortexTex.Width / 2f, vortexTex.Height / 2f),
                new Vector2(1.5f, 0.5f), SpriteEffects.None, 0f);

            // Disco trasero (Einstein ring)
            Main.spriteBatch.Draw(vortexTex, drawPos - new Vector2(0, 4f), null,
                new Color(200, 50, 0, 100), -Projectile.rotation * 2f,
                new Vector2(vortexTex.Width / 2f, vortexTex.Height / 2f),
                new Vector2(1.5f, 0.5f), SpriteEffects.FlipVertically, 0f);

            // Beaming relativístico
            Main.spriteBatch.Draw(vortexTex, drawPos - new Vector2(3f, 0f), null,
                new Color(255, 230, 150, 130), Projectile.rotation * 2f,
                new Vector2(vortexTex.Width / 2f, vortexTex.Height / 2f),
                new Vector2(1.5f, 0.5f), SpriteEffects.None, 0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            // Event horizon
            Main.spriteBatch.Draw(glowTex, drawPos, null,
                Color.Black, 0f, new Vector2(glowTex.Width / 2f, glowTex.Height / 2f),
                0.8f, SpriteEffects.None, 0f);

            // Photon ring + aberración cromática
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
            Main.spriteBatch.Draw(ringTex, drawPos - new Vector2(2f, 0f), null,
                new Color(255, 0, 0, 80), 0f, new Vector2(ringTex.Width / 2f, ringTex.Height / 2f),
                0.6f * pulse, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(ringTex, drawPos, null,
                new Color(0, 255, 0, 80), 0f, new Vector2(ringTex.Width / 2f, ringTex.Height / 2f),
                0.6f * pulse, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(ringTex, drawPos + new Vector2(2f, 0f), null,
                new Color(0, 100, 255, 80), 0f, new Vector2(ringTex.Width / 2f, ringTex.Height / 2f),
                0.6f * pulse, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(ringTex, drawPos, null,
                new Color(255, 240, 200, 220), 0f, new Vector2(ringTex.Width / 2f, ringTex.Height / 2f),
                0.6f * pulse, SpriteEffects.None, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode == NetmodeID.Server) return;

            // Implosión + explosión
            for (int i = 0; i < 50; i++)
            {
                float angle = (MathHelper.TwoPi / 50) * i + Main.rand.NextFloat(-0.2f, 0.2f);
                float dist = Main.rand.NextFloat(80f, 140f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist, (float)Math.Sin(angle) * dist);
                Vector2 toCenter = Projectile.Center - spawnPos;
                if (toCenter.LengthSquared() > 0.01f)
                {
                    toCenter.Normalize();
                    Vector2 tangent = new Vector2(-toCenter.Y, toCenter.X) * 0.5f;
                    Vector2 vel = (toCenter * 7f + tangent * 4f);
                    Dust d = Dust.NewDustPerfect(spawnPos, DustID.PurpleTorch,
                        vel, 200, new Color(200, 100, 255), 1.3f);
                    d.noGravity = true; d.fadeIn = 0f;
                }
            }

            for (int i = 0; i < 40; i++)
            {
                float angle = (MathHelper.TwoPi / 40) * i;
                Vector2 dir = new Vector2(
                    (float)Math.Cos(angle) * Main.rand.NextFloat(5f, 11f),
                    (float)Math.Sin(angle) * Main.rand.NextFloat(5f, 11f));
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                    dir, 220, new Color(255, 200, 100), 1.5f);
                d.noGravity = true; d.fadeIn = 0f;
            }

            for (int i = 0; i < 15; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Gold,
                    new Vector2(Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-4f, 4f)),
                    255, Color.White, 1.0f);
                d.noGravity = true; d.fadeIn = 0f;
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
