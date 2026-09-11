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
    /// v5.89 — FIX CRÍTICO de la "pantalla negra": FNA limpia el backbuffer al
    /// re-bindearlo. La semántica de FNA (verificada decompilando FNA.dll) es
    /// que SetRenderTarget(null)/SetRenderTargets(...) ejecuta
    /// `Clear(Target|Depth|Stencil)` sobre el target recién bindeado cuando su
    /// RenderTargetUsage es DiscardContents — y el PresentationParameters del
    /// juego usa DiscardContents por defecto. Por eso el propio Terraria hace
    /// Clear + redraw completo en su FilterManager.EndCapture.
    ///
    /// La v5.86-v5.88 componía por REGIONES (solo el cuadrado alrededor de cada
    /// fuente) intentando conservar el backbuffer intacto fuera de ellas... pero
    /// el restore del binding YA HABÍA BORRADO el backbuffer: el mundo dibujado
    /// por EndCapture se destruía y solo quedaban las regiones → pantalla negra
    /// con un cuadrado brillante (exactamente lo que reportó el usuario).
    ///
    /// Arquitectura nueva (v5.89), el mismo pipeline del renderer de WoTG que
    /// inspiró el sistema:
    ///   1. El mundo se renderiza en Main.screenTarget SIN el núcleo del agujero
    ///      (BlackHoleProjectile.PreDraw se salta su dibujado cuando la lente
    ///      está activa — ver LensActive).
    ///   2. Se recopilan hasta 5 fuentes de distorsión: agujeros negros Y ondas
    ///      cromáticas (CosmicShockwaveProjectile, estilos 0/1).
    ///   3. screenTarget se copia COMPLETO a través de BlackHoleDistortionShader
    ///      hacia _lensTarget — ahora a RESOLUCIÓN NATIVA (el shader es barato:
    ///      una sola lectura de textura por píxel, no necesita media resolución).
    ///   4. Se restaura el binding original (FNA borra el backbuffer — esperado)
    ///      y se dibuja _lensTarget A PANTALLA COMPLETA: el mundo vuelve a estar
    ///      en pantalla a resolución nativa, distorsionado solo cerca de las
    ///      fuentes. Sin regiones, sin bordes duros, sin media resolución.
    ///   5. ENCIMA de la lente, en orden:
    ///        a) partículas de la capa AboveLens (efectos del agujero negro),
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
    /// v5.90 — LENTE DELGADA: la v5.89 usaba maxLensingAngle=24 rad (×0.62 de
    /// fuerza → ángulo pico de ~14.9 rad cerca del horizonte): como el shader
    /// rota las coords de muestreo ALREDEDOR DEL CENTRO DE PANTALLA, cualquier
    /// ángulo grande desplazaba píxeles lejanos proporcional a su distancia al
    /// centro → "movía toda la pantalla". Ahora el ángulo pico es ~0.8 rad
    /// (1.5 × 0.55) con radio 1.1× el tamaño visual: la distorsión vive en un
    /// anillo estrecho alrededor del agujero y muere a ~3 radios — el resto
    /// de la pantalla queda INTACTA.
    ///
    /// v5.91 — LENTE LIGERAMENTE MÁS GRANDE: radio 1.1× → 1.4× el tamaño
    /// visual del agujero (petición del usuario: "la lente gravitacional debe
    /// ser ligeramente más grande"). El ángulo pico SE MANTIENE en ~0.8 rad
    /// (sigue siendo delgada): solo el ANILLO donde vive la deformación se
    /// ensancha — abraza el disco de acreción completo y muere a ~3 radios
    /// (≈ 4.2× el tamaño visual), el resto de la pantalla sigue intacta.
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

        /// <summary>Target de la pantalla distorsionada (resolución nativa).</summary>
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

                    // Radio de influencia en UV: v5.91 — 1.4× el tamaño visual
                    // (antes 1.1×, petición del usuario: "ligeramente más grande").
                    // Con el ángulo pico reducido a ~0.8 rad el anillo de
                    // distorsión sigue siendo DELGADO (no mueve toda la pantalla):
                    // solo abraza el disco de acreción completo y muere a ~3 radios.
                    float radius = p.width * p.scale / screenSize.X * 1.4f;

                    // La lente es "pequeña": intensidad ligada a la escala del agujero
                    // (nace con el pop elástico, crece con la expansión final del
                    // v5.86, muere con la evaporación).
                    float strength = MathHelper.Clamp(p.scale * 1.1f, 0f, 1f);

                    _sourcePositions[count] = uv;
                    _sourceRadii[count] = Math.Max(radius, 0.0001f);
                    _strengths[count] = strength;
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

            // === 2. Preparar el target de la lente (RESOLUCIÓN NATIVA) ===
            // v5.89: a resolución completa — el shader de distorsión es barato
            // (una lectura de textura por píxel) y así el mundo lenteado no
            // pierde nitidez ni muestra píxeles gordos al ampliarse.
            int lensW = Math.Max(2, Main.screenWidth);
            int lensH = Math.Max(2, Main.screenHeight);
            if (_lensTarget == null || _lensTarget.IsDisposed ||
                _lensTarget.Width != lensW || _lensTarget.Height != lensH)
            {
                _lensTarget?.Dispose();
                _lensTarget = new RenderTarget2D(gd, lensW, lensH,
                    false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
            }

            // Guardar el estado de render targets ACTIVO (backbuffer o screenTarget).
            RenderTargetBinding[] previousBindings = gd.GetRenderTargets();

            // === 3. Copiar la pantalla COMPLETA a través del shader de lensing ===
            gd.SetRenderTarget(_lensTarget);
            gd.Clear(Color.Transparent);

            Effect shader = _distortionShader;
            float maxStrength = 0f;
            for (int i = 0; i < MaxSources; i++)
                if (_strengths[i] > maxStrength) maxStrength = _strengths[i];

            // "Pequeña lente": distorsión contenida (no la fuerza máxima del shader).
            // v5.90 — LENTE DELGADA: 0.55 × maxLensingAngle 1.5 rad → ángulo pico
            // ~0.8 rad (antes 0.62 × 24 = 14.9 rad: movía TODA la pantalla porque
            // el shader rota alrededor del centro de pantalla y los píxeles lejanos
            // se desplazan proporcional a su distancia al centro). Con ~0.8 rad la
            // deformación queda confinada a un anillo estrecho alrededor de las
            // fuentes y el resto de la pantalla queda pixel-perfect.
            float distortionStrength = 0.55f * MathHelper.Clamp(maxStrength, 0f, 1f);
            float maxLensingAngle = 1.5f;

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
            // Quad completo 1:1 — la lente está a resolución nativa.
            Main.spriteBatch.Draw(Main.screenTarget,
                new Rectangle(0, 0, _lensTarget.Width, _lensTarget.Height), Color.White);
            Main.spriteBatch.End();

            // === 4. Restaurar el render target original ===
            // NOTA (v5.89): al volver al backbuffer FNA lo LIMPIA (semántica
            // DiscardContents de PresentationParameters, verificada contra
            // FNA.dll). Es lo mismo que hace el EndCapture de Terraria — y por
            // eso el paso 5 redibuja la pantalla COMPLETA.
            if (previousBindings != null && previousBindings.Length > 0)
                gd.SetRenderTargets(previousBindings);
            else
                gd.SetRenderTarget(null);

            // === 5. VOLCAR LA LENTE A PANTALLA COMPLETA ===
            // El backbuffer acaba de ser borrado por el restore del binding:
            // este blit devuelve el mundo entero a pantalla (resolución nativa,
            // distorsionado solo cerca de las fuentes). Es exactamente el mismo
            // blit que hace el EndCapture de Terraria con screenTarget — el
            // pipeline continúa como si la lente nunca hubiera existido, salvo
            // por la distorsión alrededor de las fuentes.
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Matrix.Identity);
            Main.spriteBatch.Draw(_lensTarget,
                new Rectangle(0, 0, gd.Viewport.Width, gd.Viewport.Height), Color.White);
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
    }
}
