using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// SunProjectile — réplica fiel del StarPet de Wrath of the Gods.
    ///
    /// Render (EXACTAMENTE como StarPet.DrawSelf de WoTG, en orden):
    ///   1. Backglow con BloomCircleSmall: amarillo * 0.7 (escala 0.95) + rojo * 0.45 (escala 1.61)
    ///   2. RadialShineShader sobre WavyBlotchNoise: color (252, 212, 112) * 0.24,
    ///      escala = width * scale * 2.72 / tamaño de la textura
    ///   3. SunShader sobre DendriticNoiseZoomedOut (canvas):
    ///      coronaIntensityFactor = 0.05, mainColor = blanco, darkerColor = (204, 92, 25),
    ///      subtractiveAccentFactor = (181, 0, 0), sphereSpinTime = GlobalTimeWrappedHourly * 0.9,
    ///      s1 = WavyBlotchNoise, s2 = PsychedelicWingTextureOffsetMap,
    ///      escala = width * scale * 1.5 / tamaño de la textura
    ///
    /// Mejoras propias: pop elástico de aparición, hinchazón previa a la nova final,
    /// chispas de fuego orbitando, llamaradas periódicas, prominencias solares,
    /// humo cálido, destellos encantados y nova de fuego al morir.
    /// </summary>
    public class SunProjectile : ModProjectile
    {
        private Ref<Effect> _sunShader;
        private Ref<Effect> _shineShader;
        private bool _sunShaderFailed;
        private bool _shineShaderFailed;

        /// <summary>Tiempo visual de vida — usada para el pop elástico de aparición.</summary>
        public ref float VisualsTime => ref Projectile.ai[0];

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
            Projectile.timeLeft = 600;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
        }

        public override void AI()
        {
            // === POP ELÁSTICO DE APARICIÓN (como el black hole) ===
            Projectile.scale = ElasticOut(Utils.GetLerpValue(0f, 90f, VisualsTime, true)) *
                               (float)Math.Sqrt(Utils.GetLerpValue(0f, 45f, VisualsTime, true));
            VisualsTime += 1f;

            // === NOVA FINAL: los últimos 30 ticks se hincha antes de explotar ===
            if (Projectile.timeLeft < 30f)
                Projectile.scale *= 1.025f;

            // === MOVIMIENTO: deriva lenta y frenado ===
            Projectile.velocity *= 0.97f;
            Projectile.rotation += 0.01f;

            // === PARTÍCULAS (solo cliente) ===
            if (Main.netMode != NetmodeID.Server)
            {
                SpawnOrbitingSparks();
                SpawnFlames();
                SpawnSmoke();
                SpawnSolarFlare();
                SpawnTwinkles();
            }

            // === ILUMINACIÓN INTENSA (como StarPet: Vector3.One * 3.2f, con pulso sutil) ===
            float pulse = 0.92f + 0.08f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4f);
            Lighting.AddLight(Projectile.Center, new Vector3(1f, 0.9f, 0.5f) * 3.2f * pulse);
        }

        // ------------------------------------------------------------------
        //  PARTÍCULAS
        // ------------------------------------------------------------------

        /// <summary>Chispas de fuego (Torch) orbitando y cayendo hacia la superficie.</summary>
        private void SpawnOrbitingSparks()
        {
            if (Main.rand.NextBool(2))
            {
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(40f, 60f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist,
                    (float)Math.Sin(angle) * dist);
                Vector2 vel = Projectile.Center - spawnPos;
                if (vel.LengthSquared() > 0.01f)
                {
                    vel.Normalize();
                    vel *= Main.rand.NextFloat(1f, 3f);
                    vel += new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-1f, 1f));
                    Dust d = Dust.NewDustPerfect(spawnPos, DustID.Torch,
                        vel, 150, new Color(255, 150, 50), 1.0f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }
        }

        /// <summary>Llamas de GoldFlame escapando de la fotosfera.</summary>
        private void SpawnFlames()
        {
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
        }

        /// <summary>Humo cálido ascendiendo desde la corona.</summary>
        private void SpawnSmoke()
        {
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

        /// <summary>Llamarada solar periódica: explosión radial de fuego desde el borde.</summary>
        private void SpawnSolarFlare()
        {
            // Cada ~45 ticks (0.75 s), una llamarada prominente
            if (VisualsTime % 45f == 0f && VisualsTime > 30f)
            {
                float baseAngle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                int count = 12;
                for (int i = 0; i < count; i++)
                {
                    float angle = baseAngle + (MathHelper.TwoPi / count) * i * 0.35f;
                    float dist = Projectile.width * 0.55f * Projectile.scale;
                    Vector2 spawnPos = Projectile.Center + new Vector2(
                        (float)Math.Cos(angle) * dist,
                        (float)Math.Sin(angle) * dist);
                    Vector2 vel = new Vector2(
                        (float)Math.Cos(angle) * Main.rand.NextFloat(3f, 6f),
                        (float)Math.Sin(angle) * Main.rand.NextFloat(3f, 6f));
                    Dust d = Dust.NewDustPerfect(spawnPos, DustID.GoldFlame,
                        vel, 220, new Color(255, 180, 80), 1.4f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }
        }

        /// <summary>Destellos encantados parpadeando alrededor de la estrella.</summary>
        private void SpawnTwinkles()
        {
            if (Main.rand.NextBool(20))
            {
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(60f, 110f) * Projectile.scale;
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist,
                    (float)Math.Sin(angle) * dist);
                Dust d = Dust.NewDustPerfect(spawnPos, DustID.Enchanted_Gold,
                    Vector2.Zero, 255, new Color(255, 240, 180), 0.7f);
                d.noGravity = true;
                d.fadeIn = 0.3f;
            }
        }

        // ------------------------------------------------------------------
        //  RENDER — réplica exacta de StarPet.DrawSelf de WoTG
        // ------------------------------------------------------------------

        public override bool PreDraw(ref Color lightColor)
        {
            if (!_sunShaderFailed && _sunShader == null)
            {
                try
                {
                    _sunShader = new Ref<Effect>(ModContent.Request<Effect>(
                        "AethonMod/Content/Effects/Shaders/SunShader",
                        AssetRequestMode.ImmediateLoad).Value);
                }
                catch
                {
                    _sunShaderFailed = true;
                }
            }
            if (!_shineShaderFailed && _shineShader == null)
            {
                try
                {
                    _shineShader = new Ref<Effect>(ModContent.Request<Effect>(
                        "AethonMod/Content/Effects/Shaders/RadialShineShader",
                        AssetRequestMode.ImmediateLoad).Value);
                }
                catch
                {
                    _shineShaderFailed = true;
                }
            }

            try
            {
                Vector2 drawPos = Projectile.Center - Main.screenPosition;
                float scale = Projectile.scale;

                // === 1. BACKGLOW (EXACTAMENTE como StarPet.DrawSelf de WoTG) ===
                Texture2D bloomCircle = ModContent.Request<Texture2D>("AethonMod/Content/Effects/WoTG/BloomCircleSmall").Value;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                    SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                // Amarillo (pequeño e intenso)
                Main.spriteBatch.Draw(bloomCircle, drawPos, null,
                    (new Color(255, 230, 100) { A = 0 }) * 0.7f, 0f,
                    bloomCircle.Size() * 0.5f, scale * 0.95f, SpriteEffects.None, 0f);
                // Rojo (grande y tenue)
                Main.spriteBatch.Draw(bloomCircle, drawPos, null,
                    (new Color(255, 50, 0) { A = 0 }) * 0.45f, 0f,
                    bloomCircle.Size() * 0.5f, scale * 1.61f, SpriteEffects.None, 0f);
                Main.spriteBatch.End();

                // === 2. RADIAL SHINE (aura con ruido animado) ===
                Texture2D wavyBlotch = ModContent.Request<Texture2D>("AethonMod/Content/Effects/WoTG/WavyBlotchNoise").Value;
                if (_shineShader != null && _shineShader.Value != null)
                {
                    Effect shineShader = _shineShader.Value;
                    shineShader.Parameters["globalTime"].SetValue(Main.GlobalTimeWrappedHourly);
                    Vector2 shineScale = Vector2.One * Projectile.width * scale * 2.72f / wavyBlotch.Size();

                    // El shader samplea s0 (la textura dibujada); LinearWrap en el Begin
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                        SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone,
                        null, Main.GameViewMatrix.TransformationMatrix);
                    _shineShader.Value.CurrentTechnique.Passes[0].Apply();
                    Main.spriteBatch.Draw(wavyBlotch, drawPos, null,
                        new Color(252, 212, 112) * 0.24f, Projectile.rotation,
                        wavyBlotch.Size() * 0.5f, shineScale, SpriteEffects.None, 0f);
                    Main.spriteBatch.End();
                }

                // === 3. SUNSHADER (la estrella — EXACTAMENTE como WoTG) ===
                if (_sunShader != null && _sunShader.Value != null)
                {
                    Effect shader = _sunShader.Value;
                    Texture2D psychedelicWing = ModContent.Request<Texture2D>("AethonMod/Content/Effects/WoTG/PsychedelicWingTextureOffsetMap").Value;
                    // ¡El canvas de WoTG es DendriticNoiseZoomedOut, no WavyBlotchNoise!
                    Texture2D dendritic = ModContent.Request<Texture2D>("AethonMod/Content/Effects/WoTG/DendriticNoiseZoomedOut").Value;

                    // Parámetros EXACTOS de StarPet.DrawSelf()
                    shader.Parameters["coronaIntensityFactor"].SetValue(0.05f);
                    shader.Parameters["mainColor"].SetValue(new Color(255, 255, 255).ToVector3());
                    shader.Parameters["darkerColor"].SetValue(new Color(204, 92, 25).ToVector3());
                    shader.Parameters["subtractiveAccentFactor"].SetValue(new Color(181, 0, 0).ToVector3());
                    shader.Parameters["sphereSpinTime"].SetValue(Main.GlobalTimeWrappedHourly * 0.9f);
                    shader.Parameters["globalTime"].SetValue(Main.GlobalTimeWrappedHourly);

                    // s1 = accentNoise (WavyBlotchNoise), s2 = uvOffsetNoise (PsychedelicWingTextureOffsetMap)
                    Main.graphics.GraphicsDevice.Textures[1] = wavyBlotch;
                    Main.graphics.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
                    Main.graphics.GraphicsDevice.Textures[2] = psychedelicWing;
                    Main.graphics.GraphicsDevice.SamplerStates[2] = SamplerState.LinearWrap;

                    // Canvas: DendriticNoiseZoomedOut (512x512) con la escala exacta de WoTG
                    Vector2 drawScale = Vector2.One * Projectile.width * scale * 1.5f / dendritic.Size();

                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                        SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone,
                        null, Main.GameViewMatrix.TransformationMatrix);
                    shader.CurrentTechnique.Passes[0].Apply();
                    Main.spriteBatch.Draw(dendritic, drawPos, null, Color.White, Projectile.rotation,
                        dendritic.Size() * 0.5f, drawScale, SpriteEffects.None, 0f);
                    Main.spriteBatch.End();
                }
                else
                {
                    // === FALLBACK: dibujado manual si el shader no carga ===
                    DrawFallback(drawPos, scale);
                }
            }
            catch { }

            RestoreSpriteBatch();
            return false;
        }

        /// <summary>Restaura el SpriteBatch al estado que tML espera tras PreDraw.</summary>
        private static void RestoreSpriteBatch()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise,
                null, Main.Transform);
        }

        /// <summary>Dibujado manual de respaldo (glow multicapa naranja).</summary>
        private void DrawFallback(Vector2 drawPos, float scale)
        {
            Texture2D glowTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
            float pulse = 0.9f + 0.1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4f);

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(glowTex, drawPos, null,
                new Color(255, 250, 200, 220), 0f,
                glowTex.Size() * 0.5f, 1.5f * scale * pulse, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(glowTex, drawPos, null,
                new Color(255, 180, 60, 180), 0f,
                glowTex.Size() * 0.5f, 2.0f * scale * pulse, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(glowTex, drawPos, null,
                new Color(200, 50, 0, 100), 0f,
                glowTex.Size() * 0.5f, 2.8f * scale * pulse, SpriteEffects.None, 0f);
            Main.spriteBatch.End();
        }

        // ------------------------------------------------------------------
        //  IMPACTO Y MUERTE
        // ------------------------------------------------------------------

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode == NetmodeID.Server) return;

            // Explosión radial de fuego sobre el objetivo
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

            // Ráfaga de chispas Torch
            for (int i = 0; i < 12; i++)
            {
                Vector2 dir = new Vector2(Main.rand.NextFloat(-5f, 5f), Main.rand.NextFloat(-5f, 5f));
                Dust d = Dust.NewDustPerfect(target.Center, DustID.Torch,
                    dir, 180, new Color(255, 150, 50), 1.1f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            target.AddBuff(BuffID.OnFire, 300);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, target.Center);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.Server) return;

            // === NOVA FINAL: explosión masiva de fuego ===
            // Onda expansiva de GoldFlame
            for (int i = 0; i < 60; i++)
            {
                float angle = (MathHelper.TwoPi / 60) * i;
                Vector2 dir = new Vector2(
                    (float)Math.Cos(angle) * Main.rand.NextFloat(6f, 14f),
                    (float)Math.Sin(angle) * Main.rand.NextFloat(6f, 14f));
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                    dir, 240, new Color(255, 200, 100), 1.7f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Chispas Torch en todas direcciones
            for (int i = 0; i < 35; i++)
            {
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch,
                    new Vector2((float)Math.Cos(angle) * Main.rand.NextFloat(4f, 9f),
                                (float)Math.Sin(angle) * Main.rand.NextFloat(4f, 9f)),
                    200, new Color(255, 150, 50), 1.4f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Núcleo de la nova: destellos encantados
            for (int i = 0; i < 20; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Gold,
                    new Vector2(Main.rand.NextFloat(-6f, 6f), Main.rand.NextFloat(-6f, 6f)),
                    255, new Color(255, 240, 180), 1.2f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Humo ascendente tras la explosión
            for (int i = 0; i < 15; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Smoke,
                    new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-4f, -1f)),
                    100, new Color(120, 70, 40), 1.0f);
                d.noGravity = false;
                d.fadeIn = 0f;
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item45, Projectile.Center);
        }

        // ------------------------------------------------------------------
        //  HELPERS
        // ------------------------------------------------------------------

        /// <summary>Elastic ease-out (réplica de EasingCurves.Elastic.Evaluate(EasingType.Out) de WoTG).</summary>
        private static float ElasticOut(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            float c = (2f * (float)Math.PI) / 3f;
            return (float)(Math.Pow(2, -10 * t) * Math.Sin((t * 10 - 0.75) * c) + 1);
        }
    }
}
