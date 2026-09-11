using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Particles;
using AethonMod.Content.Projectiles.Cosmic;

namespace AethonMod.Content.Effects
{
    /// <summary>
    /// BlackHoleLensSystem — lente gravitacional de pantalla completa.
    ///
    /// v5.87 — FIX del ThreadStateException al desactivar el mod.
    ///
    /// El error de la v5.85 era que la lente distorsionaba la pantalla YA
    /// RENDERIZADA, y el núcleo del agujero negro (dibujado en el pase del
    /// mundo) quedaba DENTRO de esa pantalla → la lente deformaba al propio
    /// agujero negro. Arquitectura nueva en el punto 36 del pipeline:
    ///
    ///   1. El mundo se renderiza en Main.screenTarget SIN el núcleo del
    ///      agujero negro (BlackHoleProjectile.PreDraw se salta su dibujado
    ///      cuando la lente está activa — ver LensActive).
    ///   2. Se recopilan hasta 5 fuentes de distorsión: agujeros negros Y
    ///      ondas cromáticas (CosmicShockwaveProjectile, estilos 0/1) —
    ///      cada frente de onda curva el fondo a su paso.
    ///   3. screenTarget se copia a través de BlackHoleDistortionShader hacia
    ///      un render target a media resolución (lensTarget).
    ///   4. COMPOSICIÓN POR REGIONES: solo la zona alrededor de cada fuente
    ///      se re-dibuja distorsionada (el resto del mundo conserva su
    ///      resolución nativa — la v5.85 volcaba la pantalla completa a media
    ///      resolución, emborronando todo el juego).
    ///   5. ENCIMA de la lente, en orden:
    ///        a) partículas de la capa AboveLens (efectos del agujero negro:
    ///           disco de acreción, anillo de fotones, espiral de succión...),
    ///        b) el NÚCLEO del agujero negro (halo + RealBlackHoleShader +
    ///           refuerzo del horizonte de sucesos),
    ///        c) los anillos de las ondas cromáticas.
    ///   6. La UI se dibuja después, intacta.
    ///
    /// LensActive: bandera estática que indica "la lente se renderizó en el
    /// frame anterior". Los proyectiles la consultan en PreDraw (que corre
    /// ANTES del punto 36) para decidir si se saltan su dibujado del mundo.
    /// Si la lente falla o no hay fuentes, la bandera cae a false y todo se
    /// dibuja por el camino normal (fallback automático, el agujero jamás
    /// desaparece).
    ///
    /// v5.87 — Unload() con programación defensiva: tModLoader descarga los
    /// mods en un hilo de carga secundario, pero FNA3D exige que Dispose()
    /// de recursos gráficos corra en el hilo principal. El render target se
    /// destruye vía Main.QueueMainThreadAction (cola ConcurrentQueue drenada
    /// al final de Main.Update() cada frame — también durante la pantalla de
    /// carga del reload), verificado contra tModLoader v2026.07.3.0 real.
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

        /// <summary>
        /// ¿La lente se renderizó en el frame anterior? Los PreDraw de los
        /// proyectiles cósmicos la consultan para saltarse el pase del mundo.
        /// </summary>
        public static bool LensActive { get; private set; }

        // Datos de las fuentes (como el shader los espera: arrays de 5)
        private readonly float[] _sourceRadii = new float[MaxSources];
        private readonly Vector2[] _sourcePositions = new Vector2[MaxSources];
        private readonly float[] _strengths = new float[MaxSources];

        // Regiones de composición (pantalla, píxeles) por fuente
        private readonly Rectangle[] _sourceRegions = new Rectangle[MaxSources];

        // Índices de proyectiles a dibujar encima de la lente
        private readonly int[] _blackHoleIndices = new int[MaxSources];
        private int _blackHoleCount;
        private readonly int[] _waveIndices = new int[MaxSources];
        private int _waveCount;

        public override void Load()
        {
            // Hook MonoMod: punto 36 = tras EndCapture del mundo, antes de la UI.
            Terraria.On_TimeLogger.DetailedDrawTime += ApplyGravitationalLens;
        }

        public override void Unload()
        {
            try
            {
                Terraria.On_TimeLogger.DetailedDrawTime -= ApplyGravitationalLens;
            }
            catch
            {
                // Programación defensiva: el detach del hook jamás puede
                // impedir que la desactivación del mod continúe.
            }

            // v5.87 — FIX del ThreadStateException:
            // "most FNA3D audio/graphics functions must be called on the main
            // thread". Unload() corre en el hilo de carga secundario de tML;
            // RenderTarget2D.Dispose() ahí lanza y rompía toda la desactivación
            // del mod. La destrucción se encola al hilo principal: la cola
            // _mainThreadActions se drena en Main.Update() cada frame, incluso
            // mientras la pantalla de carga del reload sigue dibujándose.
            // El closure captura una variable local (no el ModSystem ni estado
            // estático), así que la acción es autosuficiente.
            RenderTarget2D target = _lensTarget;
            if (target != null)
            {
                try
                {
                    Main.QueueMainThreadAction(() =>
                    {
                        try { target.Dispose(); }
                        catch
                        {
                            // Defensivo: una excepción aquí subiría hasta
                            // Main.Update() y rompería el bucle del juego.
                        }
                    });
                }
                catch
                {
                    // Encolado imposible (p. ej. apagado total del proceso):
                    // se abandona la referencia — el driver libera los
                    // recursos del proceso al terminar de todos modos.
                }
            }

            _lensTarget = null;
            _distortionShader = null;
            _shaderFailed = false;
            LensActive = false;
        }

        // ================================================================
        //  HOOK PRINCIPAL — DetailedDrawTime(36)
        // ================================================================
        private void ApplyGravitationalLens(Terraria.On_TimeLogger.orig_DetailedDrawTime orig, int detailedDrawType)
        {
            try
            {
                if (detailedDrawType == 36)
                {
                    if (CanRender())
                        RenderLens();
                    else
                        LensActive = false;
                }
            }
            catch
            {
                // La lente jamás puede romper el render del juego: si algo falla,
                // el frame siguiente todos vuelven al dibujado normal del mundo.
                LensActive = false;
            }

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
            // === 1. Recopilar fuentes: agujeros negros + ondas cromáticas ===
            int blackHoleType = ModContent.ProjectileType<BlackHoleProjectile>();
            int waveType = ModContent.ProjectileType<CosmicShockwaveProjectile>();
            Vector2 screenSize = new Vector2(Main.screenWidth, Main.screenHeight);
            if (screenSize.X <= 0f || screenSize.Y <= 0f)
            {
                LensActive = false;
                return;
            }

            int count = 0;
            _blackHoleCount = 0;
            _waveCount = 0;

            for (int i = 0; i < Main.maxProjectiles && count < MaxSources; i++)
            {
                Projectile p = Main.projectile[i];
                if (p == null || !p.active) continue;

                if (p.type == blackHoleType)
                {
                    Vector2 screenPos = p.Center - Main.screenPosition;
                    Vector2 uv = screenPos / screenSize;
                    if (uv.X < -0.25f || uv.X > 1.25f || uv.Y < -0.25f || uv.Y > 1.25f)
                        continue;

                    // Radio de influencia en UV: el 75% del tamaño visual (métrica del shader).
                    float radius = p.width * p.scale / screenSize.X * 0.75f;

                    // La lente es "pequeña": intensidad ligada a la escala del agujero
                    // (nace con el pop elástico, crece con la expansión final del
                    // v5.86, muere con la evaporación).
                    float strength = MathHelper.Clamp(p.scale * 1.1f, 0f, 1f);

                    _sourcePositions[count] = uv;
                    _sourceRadii[count] = Math.Max(radius, 0.0001f);
                    _strengths[count] = strength;
                    _sourceRegions[count] = BuildRegion(screenPos, radius * screenSize.X);
                    _blackHoleIndices[_blackHoleCount++] = i;
                    count++;
                }
                else if (p.type == waveType)
                {
                    // Solo las ondas cromáticas (0/1) distorsionan el fondo.
                    float style = p.ai[1];
                    if (style != CosmicShockwaveProjectile.StyleChromatic &&
                        style != CosmicShockwaveProjectile.StyleChromaticInverse)
                        continue;

                    float front = CosmicShockwaveProjectile.GetFrontRadius(p);
                    if (front <= 1f) continue; // retrasada o disipada

                    Vector2 screenPos = p.Center - Main.screenPosition;
                    Vector2 uv = screenPos / screenSize;
                    if (uv.X < -0.35f || uv.X > 1.35f || uv.Y < -0.35f || uv.Y > 1.35f)
                        continue;

                    // El frente de la onda curva el espacio que atraviesa.
                    float radius = front / screenSize.X;
                    float progress = CosmicShockwaveProjectile.GetProgress(p);
                    float strength = MathHelper.Clamp(1f - progress * 0.75f, 0f, 1f);

                    _sourcePositions[count] = uv;
                    _sourceRadii[count] = Math.Max(radius, 0.0001f);
                    _strengths[count] = strength;
                    _sourceRegions[count] = BuildRegion(screenPos, front);
                    _waveIndices[_waveCount++] = i;
                    count++;
                }
            }

            if (count <= 0)
            {
                // Sin agujeros ni ondas → no hay lente: todo se dibuja normal.
                LensActive = false;
                return;
            }

            // Rellenar el resto de slots con fuentes nulas (el shader itera los 5).
            for (int i = count; i < MaxSources; i++)
            {
                _sourcePositions[i] = Vector2.One * -9999f;
                _sourceRadii[i] = 0.0001f;
                _strengths[i] = 0f;
            }

            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            if (gd == null)
            {
                LensActive = false;
                return;
            }

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

            // === 4. Restaurar el render target original ===
            if (previousBindings != null && previousBindings.Length > 0)
                gd.SetRenderTargets(previousBindings);
            else
                gd.SetRenderTarget(null);

            // === 5. COMPOSICIÓN POR REGIONES ===
            // Solo la zona alrededor de cada fuente se sustituye por su versión
            // distorsionada: el resto del mundo conserva la resolución nativa.
            Viewport viewport = gd.Viewport;
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Matrix.Identity);
            for (int i = 0; i < count; i++)
            {
                Rectangle region = _sourceRegions[i];
                if (region.Width <= 0 || region.Height <= 0) continue;
                region = ClampRegion(region, viewport);
                if (region.Width <= 0 || region.Height <= 0) continue;

                // lensTarget está a media resolución: la fuente es la mitad del rect.
                var srcRect = new Rectangle(
                    region.X / 2, region.Y / 2,
                    region.Width / 2, region.Height / 2);
                Main.spriteBatch.Draw(_lensTarget, region, srcRect, Color.White);
            }
            Main.spriteBatch.End();

            // === 6. ENCIMA DE LA LENTE: efectos del agujero (capa AboveLens) ===
            // Disco de acreción, anillo de fotones, espiral de succión...
            // La lente queda DETRÁS de los efectos del agujero negro.
            ParticleManager.RenderAboveLensLayer();

            // === 7. ENCIMA DE LA LENTE: el núcleo del agujero negro ===
            // El shader del agujero nunca es deformado por su propia lente.
            for (int i = 0; i < _blackHoleCount; i++)
            {
                Projectile bh = Main.projectile[_blackHoleIndices[i]];
                if (bh != null && bh.active)
                    BlackHoleProjectile.DrawCoreVisuals(bh, false);
            }

            // === 8. ENCIMA DE LA LENTE: anillos de las ondas cromáticas ===
            for (int i = 0; i < _waveCount; i++)
            {
                Projectile wave = Main.projectile[_waveIndices[i]];
                if (wave != null && wave.active)
                    CosmicShockwaveProjectile.DrawWaveVisual(wave, false);
            }

            // Todo renderizado con éxito: el frame siguiente los proyectiles se
            // saltan el pase del mundo y esta lente se encarga de pintarlos.
            LensActive = true;

            // El pipeline de Terraria continúa con su propio Begin para la UI:
            // dejamos el SpriteBatch CERRADO y los targets tal como estaban.
        }

        /// <summary>Región de influencia de una fuente: centro ± radio*2.2 (margen del decaimiento exponencial).</summary>
        private static Rectangle BuildRegion(Vector2 centerPx, float radiusPx)
        {
            float r = radiusPx * 2.2f;
            return new Rectangle(
                (int)(centerPx.X - r), (int)(centerPx.Y - r),
                (int)(r * 2f), (int)(r * 2f));
        }

        /// <summary>Recorta la región al viewport (coordenadas de pantalla).</summary>
        private static Rectangle ClampRegion(Rectangle region, Viewport viewport)
        {
            int x0 = Math.Max(0, region.X);
            int y0 = Math.Max(0, region.Y);
            int x1 = Math.Min(viewport.Width, region.X + region.Width);
            int y1 = Math.Min(viewport.Height, region.Y + region.Height);
            return new Rectangle(x0, y0, Math.Max(0, x1 - x0), Math.Max(0, y1 - y0));
        }
    }
}
