using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// SunProjectile — basado en el StarPet de Wrath of the Gods.
    /// Usa el SunShader.fx para renderizar una estrella con:
    /// - Textura de fuego con esfericidad (spherePinchFactor)
    /// - Corona con brillo radial (coronaIntensityFactor)
    /// - Manchas oscuras (subtractiveAccentFactor)
    /// - Flujo de lava (uvOffset)
    /// Se mueve lentamente (velocity *= 0.97f).
    /// </summary>
    public class SunProjectile : ModProjectile
    {
        private Ref<Effect> _sunShader;
        private Ref<Effect> _shineShader;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 92;
            Projectile.height = 92;
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
            // Movimiento lento
            Projectile.velocity *= 0.97f;

            // Rotación lenta del sol
            Projectile.rotation += 0.01f;

            // === PARTÍCULAS DE FUEGO (chorona + llamas saliendo) ===
            if (Main.netMode != NetmodeID.Server)
            {
                // Chispas de fuego (DustID.Torch) orbitando
                if (Main.rand.NextBool(2))
                {
                    float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                    float dist = Main.rand.NextFloat(40f, 60f);
                    Vector2 spawnPos = Projectile.Center + new Vector2(
                        (float)Math.Cos(angle) * dist,
                        (float)Math.Sin(angle) * dist);
                    Vector2 vel = -spawnPos + Projectile.Center;
                    vel.Normalize();
                    vel *= Main.rand.NextFloat(1f, 3f);
                    vel += new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-1f, 1f));

                    Dust d = Dust.NewDustPerfect(spawnPos, DustID.Torch,
                        vel, 150, new Color(255, 150, 50), 1.0f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }

                // Llamas saliendo del sol
                if (Main.rand.NextBool(3))
                {
                    float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                    float dist = Main.rand.NextFloat(30f, 45f);
                    Vector2 spawnPos = Projectile.Center + new Vector2(
                        (float)Math.Cos(angle) * dist,
                        (float)Math.Sin(angle) * dist);
                    Vector2 vel = new Vector2(
                        (float)Math.Cos(angle) * 2f,
                        (float)Math.Sin(angle) * 2f);

                    Dust d = Dust.NewDustPerfect(spawnPos, DustID.GoldFlame,
                        vel, 200, new Color(255, 200, 100), 1.2f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }

                // Humo sutil
                if (Main.rand.NextBool(8))
                {
                    float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                    float dist = Main.rand.NextFloat(50f, 70f);
                    Vector2 spawnPos = Projectile.Center + new Vector2(
                        (float)Math.Cos(angle) * dist,
                        (float)Math.Sin(angle) * dist);
                    Vector2 vel = new Vector2(
                        (float)Math.Cos(angle) * 0.5f,
                        (float)Math.Sin(angle) * 0.5f - 1f);

                    Dust d = Dust.NewDustPerfect(spawnPos, DustID.Smoke,
                        vel, 60, new Color(100, 60, 30), 0.6f);
                    d.noGravity = false;
                    d.fadeIn = 0f;
                }
            }

            // === ILUMINACIÓN (muy intensa como el StarPet de WoTG) ===
            // WoTG usa Vector3.One * 3.2f para luz
            float pulse = 0.9f + 0.1f * (float)Math.Sin(Main.GameUpdateCount * 0.05f);
            Lighting.AddLight(Projectile.Center, new Vector3(1f, 0.9f, 0.5f) * 3.2f * pulse);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Cargar shaders
            if (_sunShader == null)
            {
                try { _sunShader = new Ref<Effect>(ModContent.Request<Effect>("AethonMod/Content/Effects/Shaders/SunShader").Value); }
                catch { }
            }
            if (_shineShader == null)
            {
                try { _shineShader = new Ref<Effect>(ModContent.Request<Effect>("AethonMod/Content/Effects/Shaders/RadialShineShader").Value); }
                catch { }
            }

            try
            {
                Vector2 drawPos = Projectile.Center - Main.screenPosition;
                float t = (float)Main.GameUpdateCount;
                float scale = Projectile.scale;

                // === 1. BACKGLOW (bloom detrás del sol) ===
                Texture2D glowTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
                // Amarillo
                Main.spriteBatch.Draw(glowTex, drawPos, null,
                    new Color(255, 230, 100, 80),
                    0f, new Vector2(glowTex.Width / 2f, glowTex.Height / 2f),
                    2.5f * scale, SpriteEffects.None, 0f);
                // Rojo
                Main.spriteBatch.Draw(glowTex, drawPos, null,
                    new Color(200, 50, 0, 50),
                    0f, new Vector2(glowTex.Width / 2f, glowTex.Height / 2f),
                    3.5f * scale, SpriteEffects.None, 0f);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

                // === 2. SHADER DEL SOL (SunShader.fx) ===
                if (_sunShader != null && _sunShader.Value != null)
                {
                    Effect shader = _sunShader.Value;
                    Texture2D noiseTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Noise").Value;

                    shader.Parameters["globalTime"].SetValue((float)Main.GameUpdateCount * 0.0167f);
                    shader.Parameters["coronaIntensityFactor"].SetValue(0.05f);
                    shader.Parameters["mainColor"].SetValue(new Color(255, 255, 255).ToVector3());
                    shader.Parameters["darkerColor"].SetValue(new Color(204, 92, 25).ToVector3());
                    shader.Parameters["subtractiveAccentFactor"].SetValue(new Color(181, 0, 0).ToVector3());
                    shader.Parameters["sphereSpinTime"].SetValue((float)Main.GameUpdateCount * 0.0167f * 0.9f);

                    // Configurar texturas de ruido
                    Main.graphics.GraphicsDevice.Textures[1] = noiseTex;
                    Main.graphics.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
                    Main.graphics.GraphicsDevice.Textures[2] = noiseTex;
                    Main.graphics.GraphicsDevice.SamplerStates[2] = SamplerState.LinearWrap;

                    shader.CurrentTechnique.Passes[0].Apply();

                    // Dibujar el sol
                    Texture2D pixel = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                    Main.spriteBatch.Draw(pixel, drawPos, null, Color.White, 0f,
                        new Vector2(pixel.Width / 2f, pixel.Height / 2f),
                        3f * scale, SpriteEffects.None, 0f);
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                }
                else
                {
                    // === FALLBACK: dibujar sol manualmente si el shader no carga ===
                    DrawFallback(drawPos, scale, t);
                }

                // === 3. RADIAL SHINE (brillo radial sobre el sol) ===
                if (_shineShader != null && _shineShader.Value != null)
                {
                    Effect shineShader = _shineShader.Value;
                    Texture2D noiseTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Noise").Value;
                    shineShader.Parameters["globalTime"].SetValue((float)Main.GameUpdateCount * 0.0167f);
                    Main.graphics.GraphicsDevice.Textures[1] = noiseTex;
                    Main.graphics.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
                    shineShader.CurrentTechnique.Passes[0].Apply();

                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
                    Texture2D pixel = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                    Main.spriteBatch.Draw(pixel, drawPos, null,
                        new Color(252, 212, 112) * 0.3f, Projectile.rotation,
                        new Vector2(pixel.Width / 2f, pixel.Height / 2f),
                        2.5f * scale, SpriteEffects.None, 0f);
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                }
            }
            catch { }
            return false;
        }

        private void DrawFallback(Vector2 drawPos, float scale, float t)
        {
            Texture2D glowTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
            float pulse = 0.9f + 0.1f * (float)Math.Sin(t * 0.05f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

            // Núcleo blanco-amarillo
            Main.spriteBatch.Draw(glowTex, drawPos, null,
                new Color(255, 250, 200, 220), 0f,
                new Vector2(glowTex.Width / 2f, glowTex.Height / 2f),
                1.5f * scale * pulse, SpriteEffects.None, 0f);

            // Capa naranja
            Main.spriteBatch.Draw(glowTex, drawPos, null,
                new Color(255, 180, 60, 180), 0f,
                new Vector2(glowTex.Width / 2f, glowTex.Height / 2f),
                2.0f * scale * pulse, SpriteEffects.None, 0f);

            // Capa roja
            Main.spriteBatch.Draw(glowTex, drawPos, null,
                new Color(200, 50, 0, 100), 0f,
                new Vector2(glowTex.Width / 2f, glowTex.Height / 2f),
                2.8f * scale * pulse, SpriteEffects.None, 0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode == NetmodeID.Server) return;

            // Explosión solar
            for (int i = 0; i < 30; i++)
            {
                float angle = (MathHelper.TwoPi / 30) * i;
                Vector2 dir = new Vector2(
                    (float)Math.Cos(angle) * Main.rand.NextFloat(5f, 10f),
                    (float)Math.Sin(angle) * Main.rand.NextFloat(5f, 10f));
                Dust d = Dust.NewDustPerfect(target.Center, DustID.GoldFlame,
                    dir, 220, new Color(255, 200, 100), 1.3f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Aplicar OnFire
            target.AddBuff(BuffID.OnFire, 300);

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, target.Center);
        }
    }
}
