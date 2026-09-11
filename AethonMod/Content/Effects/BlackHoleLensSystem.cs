using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Cosmic;

namespace AethonMod.Content.Effects
{
    /// <summary>
    /// BlackHoleLensSystem — lente gravitacional de pantalla completa.
    ///
    /// Distorsiona el FONDO REAL del juego alrededor de cada agujero negro activo,
    /// siguiendo la matemática del lensing gravitatorio relativista (formalismo de
    /// lentes con decaimiento exponencial por distancia). El shader
    /// BlackHoleDistortionShader recibe hasta 5 fuentes (posiciones UV en pantalla
    /// y radios) y rota las coordenadas de muestreo de la textura de pantalla,
    /// curvando la luz que "pasa" cerca del horizonte de sucesos.
    ///
    /// Pipeline (verificado contra el binario real de tModLoader):
    ///   1. El mundo se renderiza en Main.screenTarget (RenderTargets activos).
    ///   2. Terraria.Graphics.Effects.Filters.Scene.EndCapture(...) vuelca el mundo.
    ///   3. TimeLogger.DetailedDrawTime(36) — punto EXACTO entre el fin del mundo
    ///      y el inicio de la UI: aquí intervenimos con un hook de MonoMod.
    ///   4. Copiamos screenTarget a través del shader de distorsión hacia un
    ///      render target a media resolución (rendimiento) y lo volvemos a
    ///      dibujar cubriendo la pantalla completa → el fondo queda distorsionado.
    ///   5. La UI se dibuja después, intacta, encima del efecto.
    ///
    /// La intensidad es "pequeña" y elegante: se desvanece con la escala del
    /// agujero (nacimiento/colapso) y se apaga sola cuando no hay agujeros activos.
    /// </summary>
    [Autoload(Side = ModSide.Client)]
    public class BlackHoleLensSystem : ModSystem
    {
        private const int MaxSources = 5;

        /// <summary>Target de la pantalla distorsionada (media resolución).</summary>
        private static RenderTarget2D _lensTarget;

        /// <summary>Shader de lensing (mismo pipeline .fxc del resto de efectos).</summary>
        private static Effect _distortionShader;
        private static bool _shaderFailed;

        // Datos de las fuentes (como el shader los espera: arrays de 5)
        private readonly float[] _sourceRadii = new float[MaxSources];
        private readonly Vector2[] _sourcePositions = new Vector2[MaxSources];
        private readonly float[] _strengths = new float[MaxSources];

        public override void Load()
        {
            // Hook MonoMod: punto 36 = tras EndCapture del mundo, antes de la UI.
            Terraria.On_TimeLogger.DetailedDrawTime += ApplyGravitationalLens;
        }

        public override void Unload()
        {
            Terraria.On_TimeLogger.DetailedDrawTime -= ApplyGravitationalLens;
            _lensTarget?.Dispose();
            _lensTarget = null;
            _distortionShader = null;
            _shaderFailed = false;
        }

        // ================================================================
        //  HOOK PRINCIPAL — DetailedDrawTime(36)
        // ================================================================
        private void ApplyGravitationalLens(Terraria.On_TimeLogger.orig_DetailedDrawTime orig, int detailedDrawType)
        {
            try
            {
                if (detailedDrawType == 36 && CanRender())
                    RenderLens();
            }
            catch { /* la lente jamás puede romper el render del juego */ }

            orig(detailedDrawType);
        }

        private bool CanRender()
        {
            // Sin mundo, menú o sin render targets de pantalla → nada que distorsionar.
            if (Main.gameMenu || Main.screenTarget == null || Main.screenTarget.IsDisposed)
                return false;

            if (!_shaderFailed && _distortionShader == null)
            {
                try
                {
                    _distortionShader = ModContent.Request<Effect>(
                        "AethonMod/Content/Effects/Shaders/BlackHoleDistortionShader",
                        AssetRequestMode.ImmediateLoad).Value;
                }
                catch
                {
                    _shaderFailed = true;
                }
            }

            return _distortionShader != null && !_distortionShader.IsDisposed;
        }

        // ================================================================
        //  RENDER DE LA LENTE
        // ================================================================
        private void RenderLens()
        {
            // === 1. Recopilar agujeros negros activos (máx. 5, como el shader) ===
            int blackHoleType = ModContent.ProjectileType<BlackHoleProjectile>();
            Vector2 screenSize = new Vector2(Main.screenWidth, Main.screenHeight);
            if (screenSize.X <= 0f || screenSize.Y <= 0f)
                return;

            int count = 0;
            for (int i = 0; i < Main.maxProjectiles && count < MaxSources; i++)
            {
                Projectile p = Main.projectile[i];
                if (p == null || !p.active || p.type != blackHoleType)
                    continue;

                // Posición en UV de pantalla (0..1) — la misma métrica del shader.
                Vector2 screenPos = p.Center - Main.screenPosition;
                Vector2 uv = screenPos / screenSize;

                // Fuera de pantalla (con margen) → fuente nula.
                if (uv.X < -0.25f || uv.X > 1.25f || uv.Y < -0.25f || uv.Y > 1.25f)
                    continue;

                // Radio de influencia en UV: el 75% del tamaño visual (métrica del shader).
                float radius = p.width * p.scale / screenSize.X * 0.75f;

                // La lente es "pequeña": intensidad ligada a la escala del agujero
                // (nace con el pop elástico, muere con el colapso final).
                float strength = MathHelper.Clamp(p.scale * 1.1f, 0f, 1f);

                _sourcePositions[count] = uv;
                _sourceRadii[count] = Math.Max(radius, 0.0001f);
                _strengths[count] = strength;
                count++;
            }

            if (count <= 0)
                return;

            // Rellenar el resto de slots con fuentes nulas (el shader itera los 5).
            for (int i = count; i < MaxSources; i++)
            {
                _sourcePositions[i] = Vector2.One * -9999f;
                _sourceRadii[i] = 0.0001f;
                _strengths[i] = 0f;
            }

            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            if (gd == null)
                return;

            // === 2. Preparar el target de la lente (media resolución) ===
            int lensW = Math.Max(2, Main.screenWidth / 2);
            int lensH = Math.Max(2, Main.screenHeight / 2);
            if (_lensTarget == null || _lensTarget.IsDisposed ||
                _lensTarget.Width != lensW || _lensTarget.Height != lensH)
            {
                _lensTarget?.Dispose();
                _lensTarget = new RenderTarget2D(gd, lensW, lensH,
                    false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
            }

            // Guardar el estado de render targets ACTIVO (backbuffer o screenTarget).
            RenderTargetBinding[] previousBindings = gd.GetRenderTargets();

            // === 3. Copiar la pantalla a través del shader de lensing ===
            gd.SetRenderTarget(_lensTarget);
            gd.Clear(Color.Transparent);

            Effect shader = _distortionShader;
            float maxStrength = 0f;
            for (int i = 0; i < MaxSources; i++)
                if (_strengths[i] > maxStrength) maxStrength = _strengths[i];

            // "Pequeña lente": distorsión contenida (no la fuerza máxima del shader).
            float distortionStrength = 0.62f * MathHelper.Clamp(maxStrength, 0f, 1f);
            float maxLensingAngle = 24f;

            shader.Parameters["distortionStrength"].SetValue(distortionStrength);
            shader.Parameters["maxLensingAngle"].SetValue(maxLensingAngle);
            shader.Parameters["sourceRadii"].SetValue(_sourceRadii);
            shader.Parameters["sourcePositions"].SetValue(_sourcePositions);
            shader.Parameters["aspectRatioCorrectionFactor"].SetValue(
                new Vector2(screenSize.X / screenSize.Y, 1f));
            shader.Parameters["zoom"].SetValue(Main.GameViewMatrix.Zoom);

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Matrix.Identity);
            shader.CurrentTechnique.Passes[0].Apply();
            // Quad completo: TEXCOORD0 = 0..1 = UV de pantalla (rect destino = tamaño de la lente).
            Main.spriteBatch.Draw(Main.screenTarget,
                new Rectangle(0, 0, _lensTarget.Width, _lensTarget.Height), Color.White);
            Main.spriteBatch.End();

            // === 4. Restaurar el render target original y volcar la lente ===
            if (previousBindings != null && previousBindings.Length > 0)
                gd.SetRenderTargets(previousBindings);
            else
                gd.SetRenderTarget(null);

            Viewport viewport = gd.Viewport;
            var destRect = new Rectangle(0, 0, viewport.Width, viewport.Height);

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Matrix.Identity);
            Main.spriteBatch.Draw(_lensTarget, destRect, Color.White);
            Main.spriteBatch.End();

            // El pipeline de Terraria continúa con su propio Begin para la UI:
            // dejamos el SpriteBatch CERRADO y los targets tal como estaban.
        }
    }
}
