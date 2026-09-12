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
    /// v5.96 — EL ANILLO DE EINSTEIN Y EL OJO DEL VACÍO: (1) la onda
    /// StyleEinstein (el lente gravitacional anular que nace al FINAL de la
    /// explosión del agujero negro, petición del usuario) se registra como
    /// fuente del pase A con radio que ABRAZA al anillo (0.85× el frente) y
    /// fuerza que decae más lento — curva el fondo mientras se expande.
    /// (2) EL OJO DEL VACÍO (VoidEyeProjectile, arma nueva de terror cósmico)
    /// se dibuja ENCIMA de la lente (igual que el sol) y actúa como fuente
    /// del pase B: su distorsión crece con la DILATACIÓN de la pupila.
    ///
    /// v5.95 — DOS PASES DE DISTORSIÓN + EL SOL COMO FUENTE SUTIL: el shader
    /// solo acepta UNA fuerza global (distortionStrength), así que la LENTE
    /// DEL SOL EN GIGANTE ROJA va en su PROPIO pase débil (pase B: fuerza =
    /// progreso×0.4, "un poco de lente" — petición del usuario) después del
    /// pase fuerte (pase A: agujeros + ondas cromáticas/de lente), sin que
    /// herede la fuerza del agujero negro. El sol (y sus hijos: llamarada +
    /// carga de la nova) se dibuja ENCIMA de la lente (DrawStarVisuals) igual
    /// que el núcleo del agujero negro. MODO IDENTIDAD: si la lente estuvo
    /// activa en el frame anterior pero las fuentes desaparecieron (p. ej.
    /// el agujero explotó mientras un sol seguía en pantalla), los
    /// proyectiles que se saltaron el pase del mundo se dibujan encima SIN
    /// distorsión ese último frame — sin él habría un frame de INVISIBILIDAD
    /// (parpadeo de 1 frame) justo cuando muere la última fuente.
    ///
    /// v5.95 — ONDAS DE LENTE (StyleLens): la onda gravitacional de la
    /// explosión del sol también se registra como fuente (curva el fondo a
    /// su paso) y se dibuja encima de la lente junto a las cromáticas.
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

        /// <summary>v5.95 — Target del pase B (lente sutil del sol gigante roja).</summary>
        private static RenderTarget2D _lensTargetB;

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

        // v5.95 — Pase B: fuentes del SOL en fase de gigante roja (lente sutil)
        private readonly float[] _sunRadii = new float[MaxSources];
        private readonly Vector2[] _sunPositions = new Vector2[MaxSources];
        private readonly float[] _sunStrengths = new float[MaxSources];

        // Índices de proyectiles a dibujar encima de la lente
        private readonly int[] _blackHoleIndices = new int[MaxSources];
        private int _blackHoleCount;
        private readonly int[] _waveIndices = new int[MaxSources];
        private int _waveCount;
        // v5.95 — soles a dibujar encima de la lente (SIEMPRE que la lente esté
        // activa: su PreDraw se salta el pase del mundo — igual que el agujero)
        private readonly int[] _sunIndices = new int[MaxSources];
        private int _sunCount;
        /// <summary>Número de soles que actúan como FUENTE (gigante roja).</summary>
        private int _sunSourceCount;
        // v5.96 — OJOS DEL VACÍO a dibujar encima de la lente (su PreDraw se
        // salta el pase del mundo igual que el sol y el agujero)
        private readonly int[] _eyeIndices = new int[MaxSources];
        private int _eyeCount;

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

            // v5.95 — el target del pase B se destruye igual (hilo principal).
            RenderTarget2D targetB = _lensTargetB;
            if (targetB != null)
            {
                try
                {
                    Main.QueueMainThreadAction(() =>
                    {
                        try { targetB.Dispose(); }
                        catch { }
                    });
                }
                catch { }
            }

            _lensTarget = null;
            _lensTargetB = null;
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
            // v5.95 — ¿Estuvo activa en el frame anterior? Modo identidad: si
            // las fuentes desaparecieron pero hay proyectiles que se saltaron
            // el pase del mundo, se dibujan encima SIN distorsión este último
            // frame — sin él habría UN frame de invisibilidad (parpadeo) justo
            // cuando muere la última fuente (p. ej. el agujero explotó mientras
            // un sol seguía en pantalla).
            bool wasActive = LensActive;

            // === 1. Recopilar fuentes: agujeros + ondas cromáticas/de lente/
            //     anillo de Einstein + soles + OJOS DEL VACÍO (v5.96) ===
            int blackHoleType = ModContent.ProjectileType<BlackHoleProjectile>();
            int waveType = ModContent.ProjectileType<CosmicShockwaveProjectile>();
            int sunType = ModContent.ProjectileType<SunProjectile>();
            int eyeType = ModContent.ProjectileType<VoidEyeProjectile>();
            Vector2 screenSize = new Vector2(Main.screenWidth, Main.screenHeight);
            if (screenSize.X <= 0f || screenSize.Y <= 0f)
            {
                LensActive = false;
                return;
            }

            int count = 0;           // pase A: agujeros + ondas
            _blackHoleCount = 0;
            _waveCount = 0;
            _sunCount = 0;           // v5.95 — soles a dibujar encima de la lente
            _sunSourceCount = 0;     // v5.95 — soles como FUENTE (gigante roja)
            _eyeCount = 0;           // v5.96 — ojos del vacío encima de la lente

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p == null || !p.active) continue;

                if (p.type == blackHoleType)
                {
                    if (count >= MaxSources) continue;
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
                    if (count >= MaxSources) continue;
                    // v5.95 — también las ONDAS DE LENTE (estilo 3, la onda
                    // gravitacional de la explosión del sol) curvan el fondo.
                    // v5.96 — y el ANILLO DE EINSTEIN (estilo 4, el lente
                    // gravitacional anular del final de la explosión del
                    // agujero negro) también es fuente.
                    float style = p.ai[1];
                    if (style != CosmicShockwaveProjectile.StyleChromatic &&
                        style != CosmicShockwaveProjectile.StyleChromaticInverse &&
                        style != CosmicShockwaveProjectile.StyleLens &&
                        style != CosmicShockwaveProjectile.StyleEinstein)
                        continue;

                    float front = CosmicShockwaveProjectile.GetFrontRadius(p);
                    if (front <= 1f) continue; // retrasada o disipada

                    Vector2 screenPos = p.Center - Main.screenPosition;
                    Vector2 uv = screenPos / screenSize;
                    if (uv.X < -0.35f || uv.X > 1.35f || uv.Y < -0.35f || uv.Y > 1.35f)
                        continue;

                    // El frente de la onda curva el espacio que atraviesa.
                    // v5.96 — el ANILLO DE EINSTEIN arrastra su pozo COMPLETO:
                    // el radio de distorsión abraza al anillo (0.85× el frente)
                    // y decae MÁS LENTO (la lente es lo que ES, no un subproducto).
                    bool isEinstein = style == CosmicShockwaveProjectile.StyleEinstein;
                    float radius = front / screenSize.X * (isEinstein ? 0.85f : 1f);
                    float progress = CosmicShockwaveProjectile.GetProgress(p);
                    float strength = isEinstein
                        ? MathHelper.Clamp(0.9f - progress * 0.5f, 0f, 1f)
                        : MathHelper.Clamp(1f - progress * 0.75f, 0f, 1f);

                    _sourcePositions[count] = uv;
                    _sourceRadii[count] = Math.Max(radius, 0.0001f);
                    _strengths[count] = strength;
                    _waveIndices[_waveCount++] = i;
                    count++;
                }
                else if (p.type == sunType)
                {
                    // v5.95 — EL SOL: SIEMPRE se recoge para dibujarlo ENCIMA de
                    // la lente (su PreDraw se salta el pase del mundo cuando
                    // LensActive — igual que el agujero); como FUENTE de
                    // distorsión solo en la fase de GIGANTE ROJA (petición del
                    // usuario: "dale al sol un poco de lente gravitacional a
                    // medida que vaya creciendo como gigante roja").
                    if (_sunCount >= MaxSources) continue;
                    Vector2 screenPos = p.Center - Main.screenPosition;
                    Vector2 uv = screenPos / screenSize;
                    // Margen generoso: el sol es grande y CRECE con la gigante.
                    if (uv.X < -0.6f || uv.X > 1.6f || uv.Y < -0.6f || uv.Y > 1.6f)
                        continue;
                    _sunIndices[_sunCount++] = i;

                    float rg = SunProjectile.GetRedGiantProgress(p);
                    if (rg > 0.05f && _sunSourceCount < MaxSources)
                    {
                        // Radio de influencia en UV: 1.4× el radio visual real
                        // de la estrella (crece con la gigante → el anillo de
                        // lensing la abraza mientras se hincha).
                        float radius = SunProjectile.GetStarVisualRadius(p) / screenSize.X * 1.4f;
                        _sunPositions[_sunSourceCount] = uv;
                        _sunRadii[_sunSourceCount] = Math.Max(radius, 0.0001f);
                        // "Un POCO de lente": fuerza máx 0.4 (el agujero llega a 1).
                        _sunStrengths[_sunSourceCount] = MathHelper.Clamp(rg * 0.4f, 0f, 0.4f);
                        _sunSourceCount++;
                    }
                }
                else if (p.type == eyeType)
                {
                    // v5.96 — EL OJO DEL VACÍO: como el sol, SIEMPRE se recoge
                    // para dibujarlo ENCIMA de la lente (su PreDraw se salta el
                    // pase del mundo); como FUENTE del pase B, su distorsión
                    // CRECE con la DILATACIÓN de la pupila (el terror curva el
                    // espacio alrededor del ojo) y se dispara en la fase final.
                    if (_eyeCount >= MaxSources) continue;
                    Vector2 screenPos = p.Center - Main.screenPosition;
                    Vector2 uv = screenPos / screenSize;
                    if (uv.X < -0.6f || uv.X > 1.6f || uv.Y < -0.6f || uv.Y > 1.6f)
                        continue;
                    _eyeIndices[_eyeCount++] = i;

                    float dil = VoidEyeProjectile.GetDilation(p);
                    if (dil > 0.6f && _sunSourceCount < MaxSources)
                    {
                        float radius = VoidEyeProjectile.GetEyeVisualRadius(p) / screenSize.X * 1.35f;
                        _sunPositions[_sunSourceCount] = uv;
                        _sunRadii[_sunSourceCount] = Math.Max(radius, 0.0001f);
                        // Fuerza sutil que crece con la dilatación (tope 0.45,
                        // apenas por encima del sol: el ojo es MÁS perturbador).
                        _sunStrengths[_sunSourceCount] = MathHelper.Clamp((dil - 0.6f) * 0.45f, 0f, 0.45f);
                        _sunSourceCount++;
                    }
                }
            }

            bool hasA = count > 0;             // pase A: agujeros + ondas
            bool hasB = _sunSourceCount > 0;   // pase B: soles en gigante roja + ojos
            bool anyDrawables = _blackHoleCount > 0 || _waveCount > 0 || _sunCount > 0 || _eyeCount > 0;

            if (!hasA && !hasB && !(wasActive && anyDrawables))
            {
                // Sin fuentes (y sin proyectiles que pintar encima) → no hay
                // lente: todo se dibuja por el camino normal, fallback total.
                LensActive = false;
                return;
            }

            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            if (gd == null)
            {
                LensActive = false;
                return;
            }

            // === 2. Pases de distorsión (solo si hay fuentes) ===
            // v5.95: si no hay fuentes pero la lente estuvo activa (modo
            // identidad), NO se toca NINGÚN render target — el mundo ya está
            // en el backbuffer y solo quedan por dibujar los proyectiles que
            // se saltaron el pase del mundo (pasos 6-9).
            Texture2D finalImage = Main.screenTarget;
            if (hasA || hasB)
            {
                // v5.89: targets a RESOLUCIÓN NATIVA — el shader de distorsión
                // es barato (una lectura de textura por píxel) y así el mundo
                // lenteado no pierde nitidez ni muestra píxeles gordos.
                int lensW = Math.Max(2, Main.screenWidth);
                int lensH = Math.Max(2, Main.screenHeight);
                if (_lensTarget == null || _lensTarget.IsDisposed ||
                    _lensTarget.Width != lensW || _lensTarget.Height != lensH)
                {
                    _lensTarget?.Dispose();
                    _lensTarget = new RenderTarget2D(gd, lensW, lensH,
                        false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
                }
                if (hasB && (_lensTargetB == null || _lensTargetB.IsDisposed ||
                    _lensTargetB.Width != lensW || _lensTargetB.Height != lensH))
                {
                    _lensTargetB?.Dispose();
                    _lensTargetB = new RenderTarget2D(gd, lensW, lensH,
                        false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
                }

                // Guardar el estado de render targets ACTIVO (backbuffer o screenTarget).
                RenderTargetBinding[] previousBindings = gd.GetRenderTargets();
                Effect shader = _distortionShader;

                // === 3. PASE A — distorsión fuerte (agujeros + ondas) ===
                if (hasA)
                {
                    // Rellenar el resto de slots con fuentes nulas (el shader itera los 5).
                    for (int i = count; i < MaxSources; i++)
                    {
                        _sourcePositions[i] = Vector2.One * -9999f;
                        _sourceRadii[i] = 0.0001f;
                        _strengths[i] = 0f;
                    }

                    float maxStrength = 0f;
                    for (int i = 0; i < MaxSources; i++)
                        if (_strengths[i] > maxStrength) maxStrength = _strengths[i];

                    // "Pequeña lente": distorsión contenida.
                    // v5.90 — LENTE DELGADA: 0.55 × maxLensingAngle 1.5 rad → ángulo
                    // pico ~0.8 rad (antes 0.62 × 24 = 14.9 rad: movía TODA la
                    // pantalla porque el shader rota alrededor del centro de pantalla
                    // y los píxeles lejanos se desplazan proporcional a su distancia
                    // al centro). Con ~0.8 rad la deformación queda confinada a un
                    // anillo estrecho alrededor de las fuentes y el resto de la
                    // pantalla queda pixel-perfect.
                    shader.Parameters["distortionStrength"].SetValue(
                        0.55f * MathHelper.Clamp(maxStrength, 0f, 1f));
                    shader.Parameters["maxLensingAngle"].SetValue(1.5f);
                    shader.Parameters["sourceRadii"].SetValue(_sourceRadii);
                    shader.Parameters["sourcePositions"].SetValue(_sourcePositions);
                    shader.Parameters["aspectRatioCorrectionFactor"].SetValue(
                        new Vector2(screenSize.X / screenSize.Y, 1f));
                    shader.Parameters["zoom"].SetValue(Main.GameViewMatrix.Zoom);

                    gd.SetRenderTarget(_lensTarget);
                    gd.Clear(Color.Transparent);
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                        SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                        null, Matrix.Identity);
                    shader.CurrentTechnique.Passes[0].Apply();
                    // Quad completo 1:1 — la lente está a resolución nativa.
                    Main.spriteBatch.Draw(Main.screenTarget,
                        new Rectangle(0, 0, _lensTarget.Width, _lensTarget.Height), Color.White);
                    Main.spriteBatch.End();

                    finalImage = _lensTarget;
                }

                // === 4. PASE B — LENTE SUTIL DEL SOL GIGANTE ROJA (v5.95) ===
                // El shader solo acepta UNA fuerza global: el sol va en su
                // PROPIO pase débil para no heredar la fuerza del agujero.
                if (hasB)
                {
                    for (int i = _sunSourceCount; i < MaxSources; i++)
                    {
                        _sunPositions[i] = Vector2.One * -9999f;
                        _sunRadii[i] = 0.0001f;
                        _sunStrengths[i] = 0f;
                    }

                    float maxSunStrength = 0f;
                    for (int i = 0; i < MaxSources; i++)
                        if (_sunStrengths[i] > maxSunStrength) maxSunStrength = _sunStrengths[i];

                    // "Un POCO de lente": 0.55 × (rg×0.4) → ángulo pico ≤ 0.22
                    // (≈ un cuarto del agujero): el anillo de deformación que
                    // abraza a la gigante es SUTIL y muere a ~3 radios.
                    shader.Parameters["distortionStrength"].SetValue(
                        0.55f * MathHelper.Clamp(maxSunStrength, 0f, 0.4f));
                    shader.Parameters["maxLensingAngle"].SetValue(1.5f);
                    shader.Parameters["sourceRadii"].SetValue(_sunRadii);
                    shader.Parameters["sourcePositions"].SetValue(_sunPositions);
                    shader.Parameters["aspectRatioCorrectionFactor"].SetValue(
                        new Vector2(screenSize.X / screenSize.Y, 1f));
                    shader.Parameters["zoom"].SetValue(Main.GameViewMatrix.Zoom);

                    // Entrada: screenTarget (solo B) o el resultado del pase A.
                    // Salida: _lensTargetB (nunca se lee y escribe el MISMO target).
                    gd.SetRenderTarget(_lensTargetB);
                    gd.Clear(Color.Transparent);
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                        SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                        null, Matrix.Identity);
                    shader.CurrentTechnique.Passes[0].Apply();
                    Main.spriteBatch.Draw(finalImage,
                        new Rectangle(0, 0, _lensTargetB.Width, _lensTargetB.Height), Color.White);
                    Main.spriteBatch.End();

                    finalImage = _lensTargetB;
                }

                // === 5. Restaurar el render target original y VOLCAR a pantalla ===
                // NOTA (v5.89): al volver al backbuffer FNA lo LIMPIA (semántica
                // DiscardContents de PresentationParameters, verificada contra
                // FNA.dll). Es lo mismo que hace el EndCapture de Terraria — y por
                // eso este blit redibuja la pantalla COMPLETA (el pipeline continúa
                // como si la lente nunca hubiera existido, salvo por la distorsión).
                if (previousBindings != null && previousBindings.Length > 0)
                    gd.SetRenderTargets(previousBindings);
                else
                    gd.SetRenderTarget(null);

                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Matrix.Identity);
                Main.spriteBatch.Draw(finalImage,
                    new Rectangle(0, 0, gd.Viewport.Width, gd.Viewport.Height), Color.White);
                Main.spriteBatch.End();
            }

            // === 6. ENCIMA DE LA LENTE: los SOLES (v5.95) y los OJOS (v5.96) ===
            // La estrella se saltó el pase del mundo (LensActive): el sistema
            // la pinta encima de la distorsión — con su glow coronal y creciendo
            // como gigante roja. El ojo del vacío igual: la estrella muerta y
            // su ojo vivo jamás son deformados por la lente.
            for (int i = 0; i < _sunCount; i++)
            {
                Projectile sun = Main.projectile[_sunIndices[i]];
                if (sun != null && sun.active)
                    SunProjectile.DrawStarVisuals(sun, false);
            }
            for (int i = 0; i < _eyeCount; i++)
            {
                Projectile eye = Main.projectile[_eyeIndices[i]];
                if (eye != null && eye.active)
                    VoidEyeProjectile.DrawEyeVisuals(eye, false);
            }

            // === 7. ENCIMA DE LA LENTE: efectos del agujero (capa AboveLens) ===
            // Disco de acreción, anillo de fotones, espiral de succión...
            // La lente queda DETRÁS de los efectos del agujero negro.
            ParticleManager.RenderAboveLensLayer();

            // === 8. ENCIMA DE LA LENTE: el núcleo del agujero negro ===
            // El shader del agujero nunca es deformado por su propia lente.
            for (int i = 0; i < _blackHoleCount; i++)
            {
                Projectile bh = Main.projectile[_blackHoleIndices[i]];
                if (bh != null && bh.active)
                    BlackHoleProjectile.DrawCoreVisuals(bh, false);
            }

            // === 9. ENCIMA DE LA LENTE: anillos de las ondas cromáticas/de lente ===
            for (int i = 0; i < _waveCount; i++)
            {
                Projectile wave = Main.projectile[_waveIndices[i]];
                if (wave != null && wave.active)
                    CosmicShockwaveProjectile.DrawWaveVisual(wave, false);
            }

            // v5.95 — La lente permanece activa SOLO si hubo distorsión real
            // (fuentes): en modo identidad los proyectiles vuelven al pase del
            // mundo el frame siguiente — transición SIN parpadeos y sin frames
            // de invisibilidad.
            LensActive = hasA || hasB;

            // El pipeline de Terraria continúa con su propio Begin para la UI:
            // dejamos el SpriteBatch CERRADO y los targets tal como estaban.
        }
    }
}
