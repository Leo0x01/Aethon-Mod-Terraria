using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;

using Terraria.ModLoader;
using AethonMod.Content.Particles;
using AethonMod.Content.Projectiles.Cosmic;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Effects
{
    /// <summary>
    /// BlackHoleLensSystem — lente gravitacional de pantalla completa.
    ///
    /// v6.34 — GRAVLENS, LA PUERTA ABIERTA: cualquier efecto del mod
    /// (ondas de choque, portales, impactos...) ya puede curvar el fondo
    /// con GravLens.Registrar(...) — la librería unificada de
    /// Content/VFX. DECISIÓN DE INTEGRACIÓN: los agujeros negros NO se
    /// recogen vía Registrar (su recolección está trenzada con los
    /// índices de dibujado "encima de la lente" y con los
    /// LensRadiusMult por tipo); las lentes externas se FUNDEN en las
    /// mismas arrays del pase A justo antes de renderizar (punto 1b de
    /// RenderLens) — mismo hook, mismo shader, cero duplicación, y el
    /// camino de los agujeros queda INTACTO (pixel-perfect con v6.33).
    /// El reloj de vida de esas lentes (GravLens.Actualizar) late en el
    /// hook: este sistema no tiene punto de update propio.
    ///
    /// v5.97 — LA MEDUSA NEBULAR NADA EN EL ESPACIOTIEMPO: el minion
    /// invocador (NebulaJellyfishMinion, petición del usuario: "crea una
    /// nueva arma que sea un invocador para un minion… el proyectil será la
    /// invocación") se recoge SIEMPRE para dibujarlo ENCIMA de la lente (su
    /// campana translúcida jamás es deformada — solo curva lo que hay
    /// DETRÁS) y actúa como la fuente MÁS SUTIL del pase B: fuerza
    /// 0.06→0.14 RESPIRANDO con el pulso de nado (es un trozo de nebulosa
    /// con masa — donde nada, el fondo se dobla apenas). También entra la
    /// ONDA NOVA DE LENTE (StyleNova — la explosión única del sol) como
    /// fuente del pase A.
    ///
    /// v5.96 — EL ANILLO DE EINSTEIN (v6.00 — y el OJO DEL VACÍO fue
    /// ELIMINADO por petición del usuario: "borra el ojo, se ve feo"): (1)
    /// la onda StyleEinstein (el lente gravitacional anular que ES la
    /// explosión del agujero negro, petición del usuario) se registra como
    /// fuente del pase A con radio que ABRAZA al anillo (0.85× el frente) y
    /// fuerza que decae más lento — curva el fondo mientras se expande.
    /// (v6.01 — LA GALAXIA VIVIENTE también fue ELIMINADA por petición del
    /// usuario: "se ve horrible y es muy simple"; su bloque de recolección
    /// y dibujado fue extirpado — el protocolo del pase B vive en medusas,
    /// cometas y púlsares.)
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
    /// re-bindearlo. La semántica de FNA es
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
    /// Arquitectura nueva (v5.89), pipeline completo de pantalla:
    ///   1. El mundo se renderiza en Main.screenTarget SIN el núcleo del agujero
    ///      (los agujeros rúnicos saltan su dibujado cuando la lente
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

        // ================================================================
        //  v6.51.3 — LA CURA DEL FILTRO ASESINADO (la letra del playtest:
        //  «al parecer no solo es la explosión final de los soles rúnicos,
        //  los agujeros negros runicos por completo o sea durante toda su
        //  vida cambia por completo el bioma creado para las oleadas, es
        //  como si quitaran el filtro usado para el bioma»).
        //
        //  LA CAUSA RAÍZ (verificada al IL de Main.Draw 2026.08.3.0): el
        //  mundo se renderiza en Main.screenTarget SOLO cuando hay
        //  filtros activos (CanCapture) y entonces FilterManager.EndCapture
        //  compone screenTarget + LA CADENA DE FILTROS DE PANTALLA (el
        //  Graveyard del festín entre ellos) sobre el BACKBUFFER. La lente
        //  vieja (punto 36, DESPUÉS de EndCapture) copiaba Main.screenTarget
        //  — el mundo SIN FILTRAR — y lo volcaba A PANTALLA COMPLETA: el
        //  filtro del bioma quedaba BORRADO mientras cualquier fuente viviera
        //  (el agujero entero · la nova del sol — la onda StyleNova también
        //  es fuente).
        //
        //  LA CURA: la distorsión se compone AHORA — ANTES de que la cadena
        //  de filtros corra: el hook envuelve FilterManager.EndCapture, hace
        //  los pases de distorsión de screenTarget → _lensTarget y llama al
        //  orig CON LA COPIA DISTORSIONADA como textura de entrada: los
        //  filtros de vanilla se aplican ENCIMA del mundo ya lensado — ni la
        //  nova de un sol ni un agujero vivo pueden volver a apagar el bioma.
        // ================================================================

        /// <summary>¿La composición bajo filtros ya corrió este frame? (el
        /// punto 36 lo lee para NO repetir el volcado de pantalla).</summary>
        private static bool _compuestoEnEndCapture;

        /// <summary>El frame (GameUpdateCount) en el que corrió EndCapture —
        /// si NO coincide, Main.screenTarget está RANCIO (el mundo se dibujó
        /// directo al backbuffer) y JAMÁS se vuelca.</summary>
        private static ulong _frameEndCapture;

        /// <summary>v6.51.3 — EL CORAZÓN DE LA CURA: envuelve
        /// FilterManager.EndCapture. SOLO corre cuando Main decidió capturar
        /// (flag2 = CanCapture): en ese frame Main.screenTarget está FRESCO.
        /// Con fuentes de lente: se distorsiona ANTES y el orig recibe la
        /// copia lensada — la cadena de filtros se aplica ENCIMA. Sin fuentes:
        /// el orig tal cual (cero cambios). Defensivo al 100%: si algo falla,
        /// el orig SIEMPRE corre una vez con los argumentos originales.</summary>
        private void ComponerLenteBajoFiltros(
            Terraria.Graphics.Effects.On_FilterManager.orig_EndCapture orig,
            Terraria.Graphics.Effects.FilterManager self,
            RenderTarget2D finalTexture, RenderTarget2D screenTarget1,
            RenderTarget2D screenTarget2, Color clearColor)
        {
            _frameEndCapture = Main.GameUpdateCount;   // este frame SÍ capturó
            _compuestoEnEndCapture = false;

            bool intercambiado = false;
            try
            {
                if (!Main.gameMenu && CanRender() && screenTarget1 != null &&
                    !screenTarget1.IsDisposed)
                {
                    // LA RECOLECCIÓN DE FUENTES (la misma de siempre — vive
                    // aquí ahora: EndCapture y el punto 36 son el MISMO frame).
                    Vector2 screenSize = new Vector2(Main.screenWidth, Main.screenHeight);
                    bool fuentes = RecolectarFuentes(screenSize);

                    if (fuentes)
                    {
                        // Los pases A/B de distorsión (sin el volcado final):
                        // screenTarget fresco → mundo lensado.
                        RenderTarget2D lensado = RenderPasesDeDistorsion(screenTarget1, screenSize);
                        if (lensado != null)
                        {
                            // LA ENTREGA: la cadena de filtros de vanilla se
                            // aplica SOBRE el mundo ya distorsionado.
                            orig(self, finalTexture, lensado, screenTarget2, clearColor);
                            intercambiado = true;
                            _compuestoEnEndCapture = true;
                        }
                    }
                }
            }
            catch
            {
                // La lente jamás puede romper la composición de vanilla.
                LensActive = false;
            }

            if (!intercambiado)
                orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);
        }

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

        /// <summary>v5.97 — medusas nebulares dibujadas encima de la lente.</summary>
        private readonly int[] _jellyIndices = new int[MaxSources];

        /// <summary>Número de medusas recogidas este frame.</summary>
        private int _jellyCount;

        /// <summary>v5.99 — COMETAS ESTELARES dibujados encima de la lente.</summary>
        private readonly int[] _cometIndices = new int[MaxSources];

        /// <summary>Número de cometas recogidos este frame.</summary>
        private int _cometCount;

        /// <summary>v5.99 — PÚLSARES VIVOS dibujados encima de la lente.</summary>
        private readonly int[] _pulsarIndices = new int[MaxSources];

        /// <summary>Número de púlsares recogidos este frame.</summary>
        private int _pulsarCount;

        /// <summary>v6.51.3 — ¿Hay fuentes del pase A (agujeros + ondas +
        /// GravLens) en la última recolección?</summary>
        private bool _hasA;

        /// <summary>v6.51.3 — Cuántas fuentes llenó la recolección en el
        /// array del pase A (agujeros + ondas + GravLens — para rellenar de
        /// nulos el resto de slots del shader).</summary>
        private int _conteoA;

        /// <summary>v6.51.3 — ¿Hay fuentes del pase B (gigante roja +
        /// medusas + cometas + púlsares)?</summary>
        private bool _hasB;

        public override void Load()
        {
            // Hook MonoMod: punto 36 = tras EndCapture del mundo, antes de la UI.
            Terraria.On_TimeLogger.DetailedDrawTime += ApplyGravitationalLens;

            // v6.51.3 — EL HOOK DE LA COMPOSICIÓN BAJO FILTROS: la lente se
            // distorsiona ANTES de que FilterManager.EndCapture aplique la
            // cadena de filtros de pantalla (Graveyard, eventos, mods) —
            // ver ComponerLenteBajoFiltros para la causa raíz completa.
            Terraria.Graphics.Effects.On_FilterManager.EndCapture += ComponerLenteBajoFiltros;
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
            try
            {
                Terraria.Graphics.Effects.On_FilterManager.EndCapture -= ComponerLenteBajoFiltros;
            }
            catch { }

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
                // v6.34 — GRAVLENS: el reloj de vida de las lentes
                // registradas late AQUÍ (este sistema no tiene punto de
                // update propio: su único tick es el hook de render;
                // Actualizar es idempotente por Main.GameUpdateCount,
                // el mismo dedup de VFXCore.Presupuesto).
                GravLens.Actualizar();

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
            // cuando muere la última fuente.
            bool wasActive = LensActive;

            // ================================================================
            //  v6.51.3 — LA BIFURCACIÓN DE LA CURA:
            //  · SI la composición bajo filtros ya corrió (el hook de
            //    EndCapture distorsionó y la cadena de filtros de vanilla se
            //    aplicó ENCIMA): aquí SOLO queda dibujar lo que vive sobre la
            //    lente (los núcleos, los soles, las ondas) — el fondo ya está
            //    en pantalla, distorsionado Y filtrado.
            //  · SI NO corrió: se recolectan las fuentes AHORA y solo se
            //    vuelca el mundo lensado cuando Main.screenTarget está FRESCO
            //    (este frame capturó). Si el frame NO capturó, el mundo se
            //    dibujó DIRECTO al backbuffer y screenTarget está RANCIO —
            //    JAMÁS se vuelca (el viejo volcado incondicional era la
            //    pantalla congelada esperando a pasar).
            // ================================================================
            if (_compuestoEnEndCapture)
            {
                _compuestoEnEndCapture = false;
                DibujarEncimaDeLaLente();
                LensActive = _hasA || _hasB;
                return;
            }

            bool captureCorrio = _frameEndCapture == Main.GameUpdateCount;

            Vector2 screenSize = new Vector2(Main.screenWidth, Main.screenHeight);
            if (screenSize.X <= 0f || screenSize.Y <= 0f)
            {
                LensActive = false;
                return;
            }

            // === 1. Recopilar fuentes: agujeros + ondas cromáticas/de lente/
            //     anillo de Einstein + soles + MEDUSAS NEBULARES + COMETAS/
            //     PÚLSARES (la MISMA recolección de siempre) ===
            bool fuentes = RecolectarFuentes(screenSize);
            bool anyDrawables = _blackHoleCount > 0 || _waveCount > 0 || _sunCount > 0 ||
                                _jellyCount > 0 || _cometCount > 0 || _pulsarCount > 0;

            if (!fuentes && !(wasActive && anyDrawables))
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

            // === v6.51.3 — EL VOLCADO SOLO CON FUENTE FRESCA ===
            // Si este frame capturó (screenTarget fresco) y el hook de
            // EndCapture no pudo componer (su red de seguridad), el camino
            // VIEJO completo sigue vivo: pases de distorsión + volcado.
            // Si el frame NO capturó, NO se toca NINGÚN render target: el
            // mundo ya está completo en el backbuffer y solo quedan por
            // dibujar los proyectiles que se saltaron el pase del mundo.
            if (fuentes && captureCorrio && CanRender())
            {
                try
                {
                    RenderTargetBinding[] previousBindings = gd.GetRenderTargets();
                    RenderTarget2D finalImage = RenderPasesDeDistorsion(Main.screenTarget, screenSize);
                    if (finalImage != null)
                    {
                        // === Restaurar el render target original y VOLCAR a
                        //     pantalla === (v5.89: al volver al backbuffer FNA
                        //     lo LIMPIA — este blit redibuja la pantalla).
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
                }
                catch
                {
                    LensActive = false;
                }
            }

            // === LO QUE VIVE ENCIMA DE LA LENTE (los soles, las medusas, los
            //     efectos del agujero, su núcleo y los anillos de las ondas) ===
            DibujarEncimaDeLaLente();

            // v5.95 — La lente permanece activa SOLO si hubo distorsión real
            // (fuentes): en modo identidad los proyectiles vuelven al pase del
            // mundo el frame siguiente — transición SIN parpadeos.
            LensActive = fuentes;
        }

        /// <summary>v6.51.3 — LA RECOLECCIÓN DE FUENTES (el paso 1 de
        /// siempre, intacto): agujeros negros + ondas (cromáticas/de lente/
        /// Einstein/nova) + soles (y su gigante roja) + medusas + cometas +
        /// púlsares + LAS LENTES EXTERNAS de GravLens. Llena los campos de
        /// índices/conteos y _hasA/_hasB. Devuelve true si hay FUENTE de
        /// distorsión (pase A o pase B).</summary>
        private bool RecolectarFuentes(Vector2 screenSize)
        {
            int olvidoHoleType = ModContent.ProjectileType<OlvidoBlackHoleProjectile>();
            int cosmicHoleType = ModContent.ProjectileType<CosmicBlackHoleProjectile>();
            int umbralHoleType = ModContent.ProjectileType<UmbralBlackHoleProjectile>();
            int brumaHoleType = ModContent.ProjectileType<BrumaBlackHoleProjectile>();
            int supremoHoleType = ModContent.ProjectileType<SupremoBlackHoleProjectile>();
            int supremoAuroraType = ModContent.ProjectileType<SupremoAuroraBlackHoleProjectile>();
            int eclipseType = ModContent.ProjectileType<EclipsePrimordialProjectile>();
            int umbralAscType = ModContent.ProjectileType<UmbralAscendidoBlackHoleProjectile>();
            int brumaAscType = ModContent.ProjectileType<BrumaAscendidoBlackHoleProjectile>();
            int cosmicAscType = ModContent.ProjectileType<CosmicAscendidoBlackHoleProjectile>();
            int olvidoAscType = ModContent.ProjectileType<OlvidoAscendidoBlackHoleProjectile>();
            int waveType = ModContent.ProjectileType<CosmicShockwaveProjectile>();
            int sunType = ModContent.ProjectileType<SunProjectile>();
            int jellyType = ModContent.ProjectileType<NebulaJellyfishMinion>();
            int cometType = ModContent.ProjectileType<StellarCometMinion>();
            int pulsarType = ModContent.ProjectileType<LivingPulsarMinion>();

            int count = 0;           // pase A: agujeros + ondas
            _blackHoleCount = 0;
            _waveCount = 0;
            _sunCount = 0;
            _sunSourceCount = 0;
            _jellyCount = 0;
            _cometCount = 0;
            _pulsarCount = 0;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p == null || !p.active) continue;

                if (p.type == olvidoHoleType || p.type == cosmicHoleType ||
                    p.type == umbralHoleType || p.type == brumaHoleType ||
                    p.type == supremoHoleType || p.type == supremoAuroraType ||
                    p.type == eclipseType ||
                    p.type == umbralAscType ||
                    p.type == brumaAscType || p.type == cosmicAscType ||
                    p.type == olvidoAscType)
                {
                    if (count >= MaxSources) continue;
                    Vector2 screenPos = p.Center - Main.screenPosition;
                    Vector2 uv = screenPos / screenSize;
                    if (uv.X < -0.25f || uv.X > 1.25f || uv.Y < -0.25f || uv.Y > 1.25f)
                        continue;

                    float radiusMult = 1.4f;
                    if (p.type == olvidoHoleType)
                        radiusMult = OlvidoBlackHoleProjectile.LensRadiusMult;
                    else if (p.type == cosmicHoleType)
                        radiusMult = CosmicBlackHoleProjectile.LensRadiusMult;
                    else if (p.type == umbralHoleType)
                        radiusMult = UmbralBlackHoleProjectile.LensRadiusMult;
                    else if (p.type == brumaHoleType)
                        radiusMult = BrumaBlackHoleProjectile.LensRadiusMult;
                    else if (p.type == supremoHoleType)
                        radiusMult = SupremoBlackHoleProjectile.LensRadiusMult;
                    else if (p.type == supremoAuroraType)
                        radiusMult = SupremoAuroraBlackHoleProjectile.LensRadiusMult;
                    else if (p.type == eclipseType)
                        radiusMult = EclipsePrimordialProjectile.LensRadiusMult;
                    else if (p.type == umbralAscType)
                        radiusMult = UmbralAscendidoBlackHoleProjectile.LensRadiusMult;
                    else if (p.type == brumaAscType)
                        radiusMult = BrumaAscendidoBlackHoleProjectile.LensRadiusMult;
                    else if (p.type == cosmicAscType)
                        radiusMult = CosmicAscendidoBlackHoleProjectile.LensRadiusMult;
                    else if (p.type == olvidoAscType)
                        radiusMult = OlvidoAscendidoBlackHoleProjectile.LensRadiusMult;
                    float radius = p.width * p.scale / screenSize.X * radiusMult;

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
                    float style = p.ai[1];
                    if (style != CosmicShockwaveProjectile.StyleChromatic &&
                        style != CosmicShockwaveProjectile.StyleChromaticInverse &&
                        style != CosmicShockwaveProjectile.StyleLens &&
                        style != CosmicShockwaveProjectile.StyleEinstein &&
                        style != CosmicShockwaveProjectile.StyleNova)
                        continue;

                    float front = CosmicShockwaveProjectile.GetFrontRadius(p);
                    if (front <= 1f) continue;

                    Vector2 screenPos = p.Center - Main.screenPosition;
                    Vector2 uv = screenPos / screenSize;
                    if (uv.X < -0.35f || uv.X > 1.35f || uv.Y < -0.35f || uv.Y > 1.35f)
                        continue;

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
                    if (_sunCount >= MaxSources) continue;
                    Vector2 screenPos = p.Center - Main.screenPosition;
                    Vector2 uv = screenPos / screenSize;
                    if (uv.X < -0.6f || uv.X > 1.6f || uv.Y < -0.6f || uv.Y > 1.6f)
                        continue;
                    _sunIndices[_sunCount++] = i;

                    float rg = SunProjectile.GetRedGiantProgress(p);
                    if (rg > 0.05f && _sunSourceCount < MaxSources)
                    {
                        float radius = SunProjectile.GetStarVisualRadius(p) / screenSize.X * 1.4f;
                        _sunPositions[_sunSourceCount] = uv;
                        _sunRadii[_sunSourceCount] = Math.Max(radius, 0.0001f);
                        _sunStrengths[_sunSourceCount] = MathHelper.Clamp(rg * 0.4f, 0f, 0.4f);
                        _sunSourceCount++;
                    }
                }
                else if (p.type == jellyType)
                {
                    if (_jellyCount >= MaxSources) continue;
                    Vector2 screenPos = p.Center - Main.screenPosition;
                    Vector2 uv = screenPos / screenSize;
                    if (uv.X < -0.35f || uv.X > 1.35f || uv.Y < -0.35f || uv.Y > 1.35f)
                        continue;
                    _jellyIndices[_jellyCount++] = i;

                    if (_sunSourceCount < MaxSources)
                    {
                        float contract = NebulaJellyfishMinion.GetPulseContract(p);
                        float radius = NebulaJellyfishMinion.GetBellVisualRadius(p) / screenSize.X * 1.3f;
                        _sunPositions[_sunSourceCount] = uv;
                        _sunRadii[_sunSourceCount] = Math.Max(radius, 0.0001f);
                        _sunStrengths[_sunSourceCount] = 0.06f + 0.08f * contract;
                        _sunSourceCount++;
                    }
                }
                else if (p.type == cometType)
                {
                    if (_cometCount >= MaxSources) continue;
                    Vector2 screenPos = p.Center - Main.screenPosition;
                    Vector2 uv = screenPos / screenSize;
                    if (uv.X < -0.35f || uv.X > 1.35f || uv.Y < -0.35f || uv.Y > 1.35f)
                        continue;
                    _cometIndices[_cometCount++] = i;

                    if (_sunSourceCount < MaxSources)
                    {
                        float radius = StellarCometMinion.GetCometVisualRadius(p) / screenSize.X * 2.6f;
                        _sunPositions[_sunSourceCount] = uv;
                        _sunRadii[_sunSourceCount] = Math.Max(radius, 0.0001f);
                        _sunStrengths[_sunSourceCount] = StellarCometMinion.GetCometLensStrength(p);
                        _sunSourceCount++;
                    }
                }
                else if (p.type == pulsarType)
                {
                    if (_pulsarCount >= MaxSources) continue;
                    Vector2 screenPos = p.Center - Main.screenPosition;
                    Vector2 uv = screenPos / screenSize;
                    if (uv.X < -0.35f || uv.X > 1.35f || uv.Y < -0.35f || uv.Y > 1.35f)
                        continue;
                    _pulsarIndices[_pulsarCount++] = i;

                    if (_sunSourceCount < MaxSources)
                    {
                        float radius = LivingPulsarMinion.GetPulsarVisualRadius(p) / screenSize.X * 2.2f;
                        _sunPositions[_sunSourceCount] = uv;
                        _sunRadii[_sunSourceCount] = Math.Max(radius, 0.0001f);
                        _sunStrengths[_sunSourceCount] = LivingPulsarMinion.GetPulsarLensStrength(p);
                        _sunSourceCount++;
                    }
                }
            }

            // === 1b. v6.34 — GRAVLENS: LAS LENTES EXTERNAS SE FUNDEN AQUÍ ===
            count += GravLens.PoblarParaRender(_sourcePositions, _sourceRadii,
                _strengths, count, MaxSources);

            _conteoA = count;
            _hasA = count > 0;
            _hasB = _sunSourceCount > 0;
            return _hasA || _hasB;
        }

        /// <summary>v6.51.3 — LOS PASES DE DISTORSIÓN (los pasos 2-4 de
        /// siempre, SIN el volcado final): crea/valida los targets a
        /// resolución nativa, corre el pase A (agujeros + ondas — fuerte) y
        /// el pase B (gigante roja + medusas + cometas + púlsares — sutil)
        /// y devuelve la textura final (el mundo lensado). El CALLER decide
        /// qué hacer con ella: el hook de EndCapture la ENTREGA a la cadena
        /// de filtros; el camino de respaldo la vuelca a pantalla.</summary>
        private RenderTarget2D RenderPasesDeDistorsion(Texture2D entrada, Vector2 screenSize)
        {
            if (entrada == null || entrada.IsDisposed) return null;

            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            if (gd == null) return null;

            int lensW = Math.Max(2, Main.screenWidth);
            int lensH = Math.Max(2, Main.screenHeight);
            if (_lensTarget == null || _lensTarget.IsDisposed ||
                _lensTarget.Width != lensW || _lensTarget.Height != lensH)
            {
                _lensTarget?.Dispose();
                _lensTarget = new RenderTarget2D(gd, lensW, lensH,
                    false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
            }
            if (_hasB && (_lensTargetB == null || _lensTargetB.IsDisposed ||
                _lensTargetB.Width != lensW || _lensTargetB.Height != lensH))
            {
                _lensTargetB?.Dispose();
                _lensTargetB = new RenderTarget2D(gd, lensW, lensH,
                    false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
            }

            Effect shader = _distortionShader;
            // La imagen que fluye por los pases: arranca en la entrada (si
            // solo hay pase B — p. ej. una medusa sola — el pase sutil
            // distorsiona DIRECTO el mundo fresco).
            Texture2D imagen = entrada;
            RenderTarget2D finalImage = null;

            // === PASE A — distorsión fuerte (agujeros + ondas) ===
            if (_hasA)
            {
                // Rellenar el resto de slots con fuentes nulas (el shader
                // itera los 5) — desde el conteo de la recolección.
                for (int i = _conteoA; i < MaxSources; i++)
                {
                    _sourcePositions[i] = Vector2.One * -9999f;
                    _sourceRadii[i] = 0.0001f;
                    _strengths[i] = 0f;
                }

                float maxStrength = 0f;
                for (int i = 0; i < MaxSources; i++)
                    if (_strengths[i] > maxStrength) maxStrength = _strengths[i];

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
                Main.spriteBatch.Draw(imagen,
                    new Rectangle(0, 0, _lensTarget.Width, _lensTarget.Height), Color.White);
                Main.spriteBatch.End();

                imagen = _lensTarget;
                finalImage = _lensTarget;
            }

            // === PASE B — LENTE SUTIL DEL SOL GIGANTE ROJA + medusas/cometas/
            //     púlsares (v5.95) ===
            if (_hasB)
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

                shader.Parameters["distortionStrength"].SetValue(
                    0.55f * MathHelper.Clamp(maxSunStrength, 0f, 0.4f));
                shader.Parameters["maxLensingAngle"].SetValue(1.5f);
                shader.Parameters["sourceRadii"].SetValue(_sunRadii);
                shader.Parameters["sourcePositions"].SetValue(_sunPositions);
                shader.Parameters["aspectRatioCorrectionFactor"].SetValue(
                    new Vector2(screenSize.X / screenSize.Y, 1f));
                shader.Parameters["zoom"].SetValue(Main.GameViewMatrix.Zoom);

                gd.SetRenderTarget(_lensTargetB);
                gd.Clear(Color.Transparent);
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Matrix.Identity);
                shader.CurrentTechnique.Passes[0].Apply();
                Main.spriteBatch.Draw(imagen,
                    new Rectangle(0, 0, _lensTargetB.Width, _lensTargetB.Height), Color.White);
                Main.spriteBatch.End();

                finalImage = _lensTargetB;
            }

            // Si SOLO corrió el pase B (sin A), la entrada era un
            // RenderTarget2D (Main.screenTarget) y el resultado es B; si no
            // corrió nada, null (el caller ya lo trata).
            if (finalImage == null && _hasB)
                finalImage = _lensTargetB;
            return finalImage;
        }

        /// <summary>v6.51.3 — LO QUE VIVE ENCIMA DE LA LENTE (los pasos 6-9
        /// de siempre, intactos): los SOLES y las MEDUSAS/COMETAS/PÚLSARES
        /// que se saltaron el pase del mundo, la capa AboveLens de
        /// partículas, los NÚCLEOS de los agujeros y los ANILLOS de las
        /// ondas.</summary>
        private void DibujarEncimaDeLaLente()
        {
            // === 6. LOS SOLES (v5.95) Y LAS MEDUSAS (v5.97) ===
            for (int i = 0; i < _sunCount; i++)
            {
                Projectile sun = Main.projectile[_sunIndices[i]];
                if (sun != null && sun.active)
                    SunProjectile.DrawStarVisuals(sun, false);
            }
            for (int i = 0; i < _jellyCount; i++)
            {
                Projectile jelly = Main.projectile[_jellyIndices[i]];
                if (jelly != null && jelly.active)
                    NebulaJellyfishMinion.DrawJellyfishVisuals(jelly, false);
            }
            for (int i = 0; i < _cometCount; i++)
            {
                Projectile comet = Main.projectile[_cometIndices[i]];
                if (comet != null && comet.active)
                    StellarCometMinion.DrawCometVisuals(comet, false);
            }
            for (int i = 0; i < _pulsarCount; i++)
            {
                Projectile pulsar = Main.projectile[_pulsarIndices[i]];
                if (pulsar != null && pulsar.active)
                    LivingPulsarMinion.DrawPulsarVisuals(pulsar, false);
            }

            // === 7. EFECTOS DEL AGUJERO (capa AboveLens) ===
            ParticleManager.RenderAboveLensLayer();

            // === 8. EL NÚCLEO DEL AGUJERO NEGRO ===
            int olvidoHoleType = ModContent.ProjectileType<OlvidoBlackHoleProjectile>();
            int cosmicHoleType = ModContent.ProjectileType<CosmicBlackHoleProjectile>();
            int umbralHoleType = ModContent.ProjectileType<UmbralBlackHoleProjectile>();
            int brumaHoleType = ModContent.ProjectileType<BrumaBlackHoleProjectile>();
            int supremoHoleType = ModContent.ProjectileType<SupremoBlackHoleProjectile>();
            int supremoAuroraType = ModContent.ProjectileType<SupremoAuroraBlackHoleProjectile>();
            int eclipseType = ModContent.ProjectileType<EclipsePrimordialProjectile>();
            int umbralAscType = ModContent.ProjectileType<UmbralAscendidoBlackHoleProjectile>();
            int brumaAscType = ModContent.ProjectileType<BrumaAscendidoBlackHoleProjectile>();
            int cosmicAscType = ModContent.ProjectileType<CosmicAscendidoBlackHoleProjectile>();
            int olvidoAscType = ModContent.ProjectileType<OlvidoAscendidoBlackHoleProjectile>();
            for (int i = 0; i < _blackHoleCount; i++)
            {
                Projectile bh = Main.projectile[_blackHoleIndices[i]];
                if (bh != null && bh.active)
                {
                    if (bh.type == olvidoHoleType)
                        OlvidoBlackHoleProjectile.DrawCoreVisuals(bh);
                    else if (bh.type == cosmicHoleType)
                        CosmicBlackHoleProjectile.DrawCoreVisuals(bh);
                    else if (bh.type == umbralHoleType)
                        UmbralBlackHoleProjectile.DrawCoreVisuals(bh);
                    else if (bh.type == brumaHoleType)
                        BrumaBlackHoleProjectile.DrawCoreVisuals(bh);
                    else if (bh.type == supremoHoleType)
                        SupremoBlackHoleProjectile.DrawCoreVisuals(bh);
                    else if (bh.type == supremoAuroraType)
                        SupremoAuroraBlackHoleProjectile.DrawCoreVisuals(bh);
                    else if (bh.type == eclipseType)
                        EclipsePrimordialProjectile.DrawCoreVisuals(bh);
                    else if (bh.type == umbralAscType)
                        UmbralAscendidoBlackHoleProjectile.DrawCoreVisuals(bh);
                    else if (bh.type == brumaAscType)
                        BrumaAscendidoBlackHoleProjectile.DrawCoreVisuals(bh);
                    else if (bh.type == cosmicAscType)
                        CosmicAscendidoBlackHoleProjectile.DrawCoreVisuals(bh);
                    else if (bh.type == olvidoAscType)
                        OlvidoAscendidoBlackHoleProjectile.DrawCoreVisuals(bh);
                }
            }

            // === 9. ANILLOS DE LAS ONDAS CROMÁTICAS/DE LENTE ===
            for (int i = 0; i < _waveCount; i++)
            {
                Projectile wave = Main.projectile[_waveIndices[i]];
                if (wave != null && wave.active)
                    CosmicShockwaveProjectile.DrawWaveVisual(wave, false);
            }
        }
    }
}
