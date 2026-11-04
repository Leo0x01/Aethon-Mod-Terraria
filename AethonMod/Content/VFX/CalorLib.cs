using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    // =====================================================================
    //  CalorLib — LA DISTORSIÓN DE CALOR DE PANTALLA (v6.78)
    //  EL AIRE QUE HIERVE — la librería de refracción de calor del mod.
    //
    //  LA LETRA: «quiero que le agregues al rayo de prueba C ondas de calor
    //  alrededor del haz, como una distorsión de calor, y aberración
    //  cromática en el estallido» — con la misión de investigar en internet
    //  y otros mods para crear LA MEJOR LIBRERÍA POSIBLE.
    //
    //  LA INVESTIGACIÓN (la síntesis de tres escuelas + el código real de
    //  vanilla descompilado):
    //   · REFRACTIÓN EN ESPACIO DE PANTALLA: la distorsión re-muestrea la
    //     pantalla capturada en UVs desplazadas por ruido — EL postulado
    //     del heat haze 2D.
    //   · EL RUIDO QUE HIERVE EN SU SITIO (el truco del ruido
    //     hirviendo): dos octavas en DIAGONALES OPUESTAS — el
    //     aire hierve sin «fluir» — + la convección vertical
    //     (el sesgo ascendente que el ojo lee como «calor»).
    //   · EL CAMPO DE CALOR A ¼ DE RESOLUCIÓN (el patrón del
    //     WaterShaderData de vanilla, DISTORTION_BUFFER_SCALE 0.25): las
    //     fuentes se pintan ADITIVAS en un RT diminuto (1/16 de los
    //     píxeles) — donde dos fuentes se solapan, el calor SE SUMA.
    //   · LA ABERRACIÓN CROMÁTICA DEL ESTALLIDO (el estándar del 90 % de
    //     los juegos): 3 taps radiales R/G/B alrededor del punto del golpe,
    //     con la matemática zoom-correcta de vanilla
    //     ((uTargetPosition − uScreenPosition)/uScreenResolution).
    //
    //  EL LUGAR DEL FRAME — EL FILTRO DE CADENA: el efecto vive como
    //  filtro propio en Filters.Scene ("AethonCalor"): la cadena de
    //  FilterManager.EndCapture lo aplica ENCIMA del mundo capturado y
    //  DEBAJO de la UI, componiendo con los demás filtros (el Graveyard
    //     tiñe el mundo ya distorsionado — la lección v6.51.3 no se rompe).
    //  Y la clave que lo hace POSIBLE: la ACTIVACIÓN del filtro fuerza la
    //  captura de pantalla (FilterManager.CanCapture = HasActiveFilter —
    //  vanilla no captura NADA sin filtros activos; el hook de la lente
    //  heredaba esa limitación). Con fuentes: Activate (el fade ±1/s
    //  calienta durante la carga del haz — pleno para la ignición); sin
    //  fuentes: Deactivate (la captura se apaga sola, cero costo).
    //
    //  LA LEY DE LA GPU-UNIVERSAL: si el .fxc no cargara (bandera
    //  _shaderFallo), el filtro JAMÁS se activa — el haz conserva su pila
    //  de SIETE capas de siempre. El guard de vanilla se hereda gratis:
    //  iluminación Retro / drawToScreen / mapa a pantalla = sin captura =
    //  sin calor (el fallback del propio juego).
    //
    //  EL CONTRATO DEL FRAME: las fuentes se registran en la IA (fase de
    //  update) y se consumen en el pase (fase de dibujado, dentro del hook
    //  EndCapture de BlackHoleLensSystem — UN solo hook por proceso, la
    //  advertencia de la investigación); el frame se deduplica por
    //  GameUpdateCount (el mismo patrón de VFXCore.Presupuesto). Los
    //  estallidos son POR EDAD (nacido = GameUpdateCount): cero reloj que
    //  mantener, cero sincronía que romper.
    // =====================================================================
    public static class CalorLib
    {
        // ============================ LA API =============================

        /// <summary>LA CÁPSULA DEL CONDUCTO: el aire a lo largo de un haz.
        /// Ancho cónico (boca → punta) con el peso menguando hacia la
        /// punta — el hervidero vive donde el haz es GORDO.</summary>
        public static void Haz(Vector2 desde, Vector2 hasta, float anchoBoca,
            float anchoPunta, float intensidad)
        {
            if (_shaderFallo || desde == hasta || intensidad <= 0.01f) return;
            Tocar();
            if (_nHaz >= MaxHaz) return;
            _hazDesde[_nHaz] = desde;
            _hazHasta[_nHaz] = hasta;
            _hazAnchoBoca[_nHaz] = MathF.Max(6f, anchoBoca);
            _hazAnchoPunta[_nHaz] = MathF.Max(4f, anchoPunta);
            _hazIntensidad[_nHaz] = MathHelper.Clamp(intensidad, 0f, 1.5f);
            _nHaz++;
        }

        /// <summary>UN FOCO DE CALOR suelto (una hoguera, un impacto
        /// sostenido): gradiente radial a su alrededor.</summary>
        public static void Punto(Vector2 pos, float radioPx, float intensidad)
        {
            if (_shaderFallo || radioPx <= 2f || intensidad <= 0.01f) return;
            Tocar();
            if (_nPunto >= MaxPunto) return;
            _puntoPos[_nPunto] = pos;
            _puntoRadio[_nPunto] = radioPx;
            _puntoIntensidad[_nPunto] = MathHelper.Clamp(intensidad, 0f, 1.5f);
            _nPunto++;
        }

        /// <summary>EL ESTALLIDO — el golpe de calor: un ANILLO que se
        /// expande (la onda del aire caliente) + LA ABERRACIÓN CROMÁTICA
        /// alrededor del punto (el split R/B de pico «fuerzaPx» px, radio
        /// «radioPx»). Muere solo en ~1 s (la edad manda).</summary>
        public static void Estallido(Vector2 pos, float fuerzaPx, float radioPx)
        {
            if (_shaderFallo || fuerzaPx <= 0.5f) return;
            Tocar();
            int slot = _estCabeza;
            _estCabeza = (_estCabeza + 1) % MaxEstallido;
            _estPos[slot] = pos;
            _estFuerza[slot] = fuerzaPx;
            _estRadio[slot] = MathF.Max(60f, radioPx);
            _estNacido[slot] = Main.GameUpdateCount;
            _nEst++;   // (solo para saber si ALGUNO ha vivido alguna vez)
        }

        // ========================= LOS POZOS ==============================
        // (cero-GC: arrays estáticas que se sobrescriben cada frame —
        // la convención de los pozos del RC2)

        private const int MaxHaz = 4;
        private const int MaxPunto = 16;
        private const int MaxEstallido = 8;

        private static readonly Vector2[] _hazDesde = new Vector2[MaxHaz];
        private static readonly Vector2[] _hazHasta = new Vector2[MaxHaz];
        private static readonly float[] _hazAnchoBoca = new float[MaxHaz];
        private static readonly float[] _hazAnchoPunta = new float[MaxHaz];
        private static readonly float[] _hazIntensidad = new float[MaxHaz];
        private static int _nHaz;

        private static readonly Vector2[] _puntoPos = new Vector2[MaxPunto];
        private static readonly float[] _puntoRadio = new float[MaxPunto];
        private static readonly float[] _puntoIntensidad = new float[MaxPunto];
        private static int _nPunto;

        private static readonly Vector2[] _estPos = new Vector2[MaxEstallido];
        private static readonly float[] _estFuerza = new float[MaxEstallido];
        private static readonly float[] _estRadio = new float[MaxEstallido];
        private static readonly ulong[] _estNacido = new ulong[MaxEstallido];
        private static int _estCabeza;
        private static int _nEst;

        /// <summary>El frame de la ÚLTIMA registración — el sello de
        /// frescura (fuentes de un frame muerto = fuentes que ya no
        /// existen: el haz murió a media carga y su dueño calló).</summary>
        private static ulong _frameReg;

        /// <summary>El pase corre UNA vez por frame (máx) — el dedup de la
        /// casa (la captura multi-chunk de 1.4.5 puede llamar EndCapture
        /// más de una vez: el campo se pinta una sola).</summary>
        private static ulong _framePreparado;

        /// <summary>La vida del estallido en segundos (el ataque instantáneo
        /// + la muerte exponencial — el «punch» del impacto).</summary>
        private const float VidaEstallido = 1.05f;

        // ========================= EL FILTRO ==============================

        internal const string NombreFiltro = "AethonCalor";

        private static Filter _filtro;
        private static bool _filtroActivo;
        private static bool _shaderFallo;
        private static Asset<Texture2D> _texRuido;

        /// <summary>EL CAMPO DE CALOR — ¼ de resolución (el patrón del
        /// WaterShaderData: 1/16 de los píxeles de pantalla, el fetch
        /// LINEAR suaviza el upscale gratis).</summary>
        private static RenderTarget2D _campo;
        private const float EscalaCampo = 0.25f;

        // LOS VALORES DEL LOOK (la investigación, tuned para el conducto):
        // v6.85 — LA AMPLIFICACIÓN DE LA VISIBILIDAD: con el bug de unidades
        // curado (v6.82), el presupuesto de px era el CORRECTO pero el CAMPO
        // apenas llegaba: cuentas de glow de 6,4 px de campo cada 52 px de
        // avance (solape NULO — el 93 % del conducto veía H≈0,03 → 0,3 px de
        // horno) y UNA sola octava a 30 FPS. La letra del playtest v6.84:
        // «no veo el efecto sobre el arma de rayo de prueba c, NINGUN efecto»
        // con el F8 diciendo «ON · 1 fuentes» — el pase componía, el campo
        // pintaba… y el aire NO HERVÍA porque casi no había campo ni ruido.
        // LA CURA (cuatro frentes a la vez):
        //   · uAnchoPx 9 → 16 (px de flexión plena);
        //   · el PINTOR del haz: cada glow cubre ahora ±1,3·radio (el AIRE
        //     QUE RESPIRA ALREDEDOR del conducto, no solo el eje) y el avance
        //     es radio·1,15 (solape ×4-5 → H SATURA a 1,0 en el núcleo);
        //   · la CALIDAD con suelo en 2 (a 30 FPS también hierven las DOS
        //     octavas — la contraria es la que hace HERVER EN SU SITIO);
        //   · el ruido más vivo y más fino (ver el .fx: las 3 octavas suben
        //     de frecuencia y de velocidad — a 30 FPS el horno se NOTA).
        // RESULTADO medido: boca ±16 px · medioconducto ±13 · aguja ±8 — el
        // espejismo SE VE sin derretir la pantalla (la referencia: el heat
        // haze 2D de los juegos va de 6 a 20 px a 1080p).
        private const float AnchoPx = 16.0f;   // la amplitud: px de flexión con calor pleno
        private const float RadioCA = 0.36f;  // el estallido: ⅓ de pantalla alrededor del golpe
        private const float Conveccion = 0.58f; // el sesgo ascendente del aire caliente

        /// <summary>¿Vive el filtro? (para la telemetría del F8).</summary>
        public static bool Activo => _filtroActivo && !_shaderFallo;

        /// <summary>v6.83 — ¿Hay calor vivo ESTE frame? (lo lee la
        /// CAPTURA FORZADA del lens system — el bind del RT del modo sin
        /// captura se decide en ModifyTransformMatrix, antes del draw).</summary>
        internal static bool HayCalorVivo => !_shaderFallo && !_retroFallo &&
            _filtro != null && FuentesVivas(Main.GameUpdateCount);

        /// <summary>v6.85 — El frame del ÚLTIMO PINTADO REAL del campo —
        /// la cura del F8 mentiroso: hasta v6.84 la línea decía «ON · N
        /// fuentes» con solo que el HOOK hubiera corrido (el sello vivía en
        /// la PRIMERA línea del pase, antes de TODAS las puertas) — podía
        /// decir ON mientras PintarCampo devolvía false y el aire no se veía
        /// NUNCA. Hoy la línea solo promete lo que el campo pintó.</summary>
        private static ulong _framePintado;

        /// <summary>v6.83 — El frame del ÚLTIMO pase retro (telemetría F8).
        /// </summary>
        private static ulong _frameRetro;

        /// <summary>v6.83 — El pase retro murió para siempre (la defensa
        /// de la casa: cero excepciones por frame).</summary>
        private static bool _retroFallo;

        /// <summary>v6.81 — ¿El filtro está vivo SOLO como FORZADOR de
        /// captura (la lente manda, el campo va vacío)? (F8.)</summary>
        internal static bool PorLente => _porLente;

        /// <summary>v6.81 — ¿El campo a ¼ lleva calor del frame? (la muerte
        /// suave solo vacía lo que está sucio — F8 y el pase lo leen).</summary>
        private static bool _campoSucio;

        /// <summary>v6.81 — El filtro sostenido por la LENTE (captura
        /// forzada sin calor — el early-out del shader copia limpio).</summary>
        private static bool _porLente;

        /// <summary>Las fuentes vivas del frame (para la telemetría del F8).</summary>
        public static int Fuentes => _nHaz + _nPunto;

        // ====================== LA DECISIÓN (v6.81) =======================
        // EL CATCH-22 DESMONTADO — la autopsia contra el FilterManager y el
        // Main.DoDraw REALES de tML 2026.08 (descompilados):
        //
        //   Main.DoDraw:  flag2 = … && Filters.Scene.CanCapture();   ← ANTES
        //                 de dibujar el mundo — CanCapture() exige un
        //                 filtro YA ACTIVO en _activeFilters;
        //                 if (flag2) BeginCapture(screenTarget); … mundo …
        //                 if (flag2) EndCapture(…)  ← el ÚNICO camino del
        //                 juego normal por donde corre NUESTRO hook.
        //
        // v6.78-6.80 activaba el filtro DENTRO del hook… que solo corre si
        // EndCapture fue llamado… que solo se llama si ya había captura…
        // que solo la hay si un filtro ya estaba activo: EL AIRE QUE HIERVE
        // JAMÁS PODÍA ENCENDERSE SOLO (solo vivía por accidente cuando un
        // filtro de vanilla coincidía — cementerio, tormenta — o durante
        // las capturas ajenas del minimapa/foto). «Nunca vi que esto
        // funcionara»: exactamente esto, con CERO errores en el log.
        //
        // LA CURA: la DECISIÓN de activar se muda al lado del UPDATE —
        // CalorSistema.PostUpdate (tras la IA de los proyectiles — las
        // fuentes ya están registradas — y ANTES del draw: el flag2 de ESTE
        // MISMO frame ya lo ve). El filtro se convierte además en EL
        // FORZADOR DE CAPTURA DEL MOD: cuando la LENTE tiene fuentes pero
        // no hay calor, el filtro se activa igual con el CAMPO VACÍO — el
        // shader hace early-out y copia la pantalla pixel-perfect (la lente
        // por fin compone bajo filtros SIEMPRE que hay un agujero, no solo
        // en el cementerio).
        // ================================================================
        internal static void EvaluarActivacion()
        {
            if (Main.dedServ || _shaderFallo || _filtro == null) return;
            try
            {
                // LA PUERTA DE CALIDAD (WarpPantalla — «lentes, calor») y el
                // guard del menú: el PostUpdate manda, el pase obedece.
                if (Main.gameMenu || !VFXCore.CalidadPermitida(VFXCore.CalidadFX.WarpPantalla))
                {
                    _porLente = false;
                    Desactivar();
                    return;
                }

                bool calor = FuentesVivas(Main.GameUpdateCount);
                _porLente = !calor &&
                    global::AethonMod.Content.Effects.BlackHoleLensSystem.NecesitaCaptura();
                if (calor || _porLente)
                    Activar();
                else
                    Desactivar();
            }
            catch
            {
                Desactivar();
                _shaderFallo = true;
            }
        }

        /// <summary>¿Hay ALGO que hierve? — haces/puntos frescos (un frame
        /// de vida: el proyectil los re-registra CADA tick) o estallidos
        /// vivos por edad. La leen la decisión (PostUpdate) y el pase
        /// (EndCapture) — LA MISMA ley en los dos lados del frame.</summary>
        private static bool FuentesVivas(ulong frame)
        {
            if ((_nHaz > 0 || _nPunto > 0) && frame - _frameReg <= 1)
                return true;
            for (int i = 0; i < MaxEstallido; i++)
            {
                if (_estNacido[i] == 0) continue;
                float edad = (frame - _estNacido[i]) / 60f;
                if (edad < VidaEstallido) return true;
            }
            return false;
        }

        // ====================== EL PASE DEL FRAME =========================
        // Lo llama BlackHoleLensSystem.ComponerLenteBajoFiltros — UN solo
        // hook de EndCapture por proceso (la advertencia de Starlight
        // River: dos hooks que se pelean el finalTexture se rompen; aquí
        // la lente y el calor viven en EL MISMO sitio).
        // ================================================================
        // v6.79 — LA CURA DE LA PANTALLA NEGRA: el pase recibe el buffer de
        // trabajo de vanilla (screenTarget2 — el EndCapture del hook se lo
        // pasa). Tras pintar el campo, el binding NO vuelve a la fuente:
        // queda en ese buffer, que la cadena de vanilla limpia y rellena
        // ella misma (SetRenderTarget(t2)+Clear en su primera línea útil).
        //
        // v6.81 — EL PASE SOLO PINTA, YA NO DECIDE: la activación vive en
        // EvaluarActivacion (PostUpdate — la cura del catch-22, ver arriba)
        // y sin fuentes el pase NO desactiva (la lente puede estar
        // sosteniendo el filtro con el campo vacío). Nueva firma con LA
        // PUERTA DE LAS CAPTURAS AJENAS: solo la captura del MUNDO
        // (pantallaFuente == Main.screenTarget) es nuestra — el minimapa y
        // la cámara de captura llaman EndCapture con SUS propios buffers y
        // ahí no se pinta NI se vacía nada (y el dedup del frame queda
        // intacto para la captura buena).
        internal static void PrepararPaseDeCalor(RenderTarget2D pantallaFuente,
            RenderTarget2D pantallaAlternativa)
        {
            if (_shaderFallo || _filtro == null) return;
            try
            {
                // v6.81 — CAPTURAS AJENAS FUERA: el EndCapture del minimapa/
                // foto pasa SUS render targets (CaptureCamera.DrawTick →
                // EndCapture(_frameBuffer, _filterFrameBuffer1, …)). Si la
                // fuente no es Main.screenTarget, no es el mundo: ni calor
                // (evita hervir el minimapa con UVs de pantalla) ni lente.
                if (pantallaFuente == null || pantallaFuente != Main.screenTarget)
                    return;

                ulong frame = Main.GameUpdateCount;
                if (frame == _framePreparado) return;   // dedup del frame
                _framePreparado = frame;

                // LA PUERTA DE CALIDAD (defensa — el PostUpdate ya decidió;
                // aquí solo se apaga el campo si la calidad cayó a media).
                if (Main.gameMenu || !VFXCore.CalidadPermitida(VFXCore.CalidadFX.WarpPantalla))
                {
                    VaciarSiSucio(pantallaAlternativa);
                    return;
                }

                // ¿HAY FUENTES? (la MISMA ley de la decisión — ver arriba).
                bool fuentesFrescas = FuentesVivas(frame);

                if (!fuentesFrescas)
                {
                    // v6.81 — SIN DESACTIVAR: el filtro puede seguir vivo
                    // POR LA LENTE (captura forzada con el campo vacío — el
                    // early-out del shader copia la pantalla limpia). Solo
                    // corre LA MUERTE SUAVE del aire: mientras el filtro se
                    // desvanece (±1 s), la cadena seguiría leyendo el CAMPO
                    // CONGELADO del último frame — el campo se VACÍA en la
                    // transición y lo que muere es el aire, no el frame.
                    // (v6.79: VaciarCampo aprendió la cura del binding —
                    // jamás re-bindea la fuente.)
                    if (_filtroActivo)
                        VaciarSiSucio(pantallaAlternativa);
                    return;
                }

                // === 1. EL CAMPO DE CALOR (¼) + los sobres del estallido —
                //     v6.83: la pintura vive en PintarCampo, COMPARTIDA con
                //     el pase retro (PaseRetro): UN solo corazón, DOS
                //     caminos de composición. False = este frame no capturó
                //     (previos vacíos) o los recursos fallaron (PintarCampo
                //     ya desactivó): nada que vestir.
                if (!PintarCampo(frame, pantallaAlternativa,
                        out Vector2 centroCA, out float caFuerza, out float calidad))
                    return;

                // === 2. LOS PARÁMETROS DEL FILTRO (antes de que la cadena
                //     corra: orig los aplica ENCIMA de este pase) ===
                ScreenShaderData sh = _filtro.GetShader();
                sh.UseTargetPosition(centroCA);
                // (los nombres existen: el .fxc los declara — verificado
                // contra el MojoShader real por fx20_validate)
                sh.Shader.Parameters["uTexturaCampo"].SetValue(_campo);
                if (_texRuido != null)
                    sh.Shader.Parameters["uTexturaRuido"].SetValue(_texRuido.Value);
                sh.Shader.Parameters["uAnchoPx"].SetValue(AnchoPx);
                sh.Shader.Parameters["uCAFuerza"].SetValue(caFuerza);
                sh.Shader.Parameters["uCARadio"].SetValue(RadioCA);
                sh.Shader.Parameters["uConveccion"].SetValue(Conveccion);
                sh.Shader.Parameters["uCalidad"].SetValue(calidad);
            }
            catch
            {
                // EL CALOR JAMÁS ROMPE EL FRAME: cualquier cosa que salga
                // mal apaga el filtro y el juego sigue intacto.
                Desactivar();
                _shaderFallo = true;
            }
        }

        // ================================================================
        //  v6.83 — LA PINTURA DEL CAMPO (el corazón compartido)
        // ================================================================
        // Lo que v6.78-6.82 pintaba inline en PrepararPaseDeCalor: los
        // sobres del estallido (el centro de la aberración + su fuerza)
        // y el campo a ¼ de resolución con TODAS las fuentes. HOY lo
        // comparten los DOS caminos de composición:
        //   · EL PASE DE FILTROS (PrepararPaseDeCalor): el filtro de
        //     cadena «AethonCalor» lo aplica la cadena de vanilla sobre
        //     el mundo capturado (con captura — iluminación Color/Blanca).
        //   · EL PASE RETRO (PaseRetro): el pase manual sobre la COPIA
        //     del backbuffer (sin captura — Retro/Trippy o «Lighting
        //     Update Every Frame»: ver PaseRetro).
        // «salida»: el RT al que sale el binding tras pintar — el camino
        // de filtros pasa el buffer de trabajo de vanilla (la cura
        // v6.79: la fuente JAMÁS se re-bindea); el retro pasa null (el
        // binding vuelve a los previos — el backbuffer).
        // ================================================================
        private static bool PintarCampo(ulong frame, RenderTarget2D salida,
            out Vector2 centroCA, out float caFuerza, out float calidad)
        {
            // (los outs por defecto — si algo sale mal, cero aberración)
            centroCA = Vector2.Zero;
            caFuerza = 0f;
            calidad = 1f;

            // === LA FUERZA ADAPTATIVA (VFXCore.FactorCalidad): las
            //     octavas del hervidero y la CA respiran con los FPS —
            //     la calidad que se adapta sola (v6.34).
            // v6.85 — SUELO EN 2: la calidad 1 (una sola octava) mataba el
            //     hervor a 30 FPS (el F8 del playtest: «Factor de calidad:
            //     0,5») — SIN la octava contraria el aire no HIERVE EN SU
            //     SITIO (la base se va de viaje: el ojo lo lee como scroll,
            //     no como calor). Dos octavas SIEMPRE; la tercera (el shimmer
            //     fino) sigue siendo el premio de los 54+ FPS.
            float fc = VFXCore.FactorCalidad;
            calidad = fc >= 0.9f ? 3f : 2f;

            // === EL ESTALLIDO VIVO MÁS FUERTE: el centro de la CA ===
            float mejorEnv = 0f;
                for (int i = 0; i < MaxEstallido; i++)
                {
                    if (_estNacido[i] == 0) continue;
                    float edad = (frame - _estNacido[i]) / 60f;
                    if (edad >= VidaEstallido) { _estNacido[i] = 0; continue; }
                    // EL SOBRE DEL GOLPE: ataque instantáneo (6 ticks — el
                    // punch) + muerte exponencial (la lente de aire se apaga).
                    float env = Utils.GetLerpValue(0f, 0.1f, edad, true)
                        * MathF.Exp(-edad * 5.5f);
                    if (env > mejorEnv)
                    {
                        mejorEnv = env;
                        centroCA = _estPos[i];
                    }
                }
                caFuerza = 0f;
                for (int i = 0; i < MaxEstallido; i++)
                {
                    if (_estNacido[i] == 0) continue;
                    float edad = (frame - _estNacido[i]) / 60f;
                    float env = Utils.GetLerpValue(0f, 0.1f, edad, true)
                        * MathF.Exp(-edad * 5.5f) * MathF.Min(1f, fc + 0.25f);
                    caFuerza += _estFuerza[i] * env;
                }
                if (mejorEnv <= 0f && _nHaz > 0)   // (sin estallido: la boca manda — los DOS callers ya verificaron FuentesVivas)
                    centroCA = _hazDesde[0];   // sin estallido: la boca manda

                // === 1. EL CAMPO DE CALOR (¼ de resolución, aditivo) ===
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (gd == null) { Desactivar(); return false; }

                int cw = Math.Max(2, (int)(Main.screenWidth * EscalaCampo));
                int ch = Math.Max(2, (int)(Main.screenHeight * EscalaCampo));
                if (_campo == null || _campo.IsDisposed || _campo.Width != cw || _campo.Height != ch)
                {
                    if (_campo != null && !_campo.IsDisposed)
                    {
                        RenderTarget2D viejo = _campo;
                        _campo = null;
                        try
                        {
                            Main.QueueMainThreadAction(() => { try { viejo.Dispose(); } catch { } });
                        }
                        catch { }
                    }
                    _campo = new RenderTarget2D(gd, cw, ch, false,
                        SurfaceFormat.Color, DepthFormat.None, 0,
                        RenderTargetUsage.DiscardContents);
                }

                Texture2D glow = VFXCore.SoftGlow;
                Texture2D ring = VFXCore.Ring;
                if (glow == null || ring == null) { Desactivar(); return false; }

                // ============================================================
                //  v6.79 — LA CURA DE LA PANTALLA NEGRA (la letra del
                //  playtest: «toda la pantalla se vuelve negra al lanzar el
                //  rayo de prueba C, y a veces sin previo aviso»).
                //
                //  LA CAUSA (cazada contra el FilterManager.EndCapture REAL
                //  descompilado + la lección v5.89 del mismo síntoma): el
                //  restore «correcto» SetRenderTargets(previos) re-bindeaba
                //  el binding de entrada — que al entrar al hook es
                //  Main.screenTarget CON EL MUNDO CAPTURADO DENTRO. En FNA,
                //  re-bindear un target lo INVALIDA (Clear oculto): el
                //  mundo se borraba justo antes de que la cadena de filtros
                //  lo compusiera — y la cadena componía SOBRE NEGRO. En
                //  frames sin captura, previos = [] (el backbuffer con el
                //  mundo directo): el restore invalidaba el BACKBUFFER —
                //  la pantalla negra «sin previo aviso».
                //
                //  LA CURA (dos leyes):
                //   1. SIN CAPTURA (previos vacío) NO se toca NADA: no hay
                //      composición que vestir y restaurar el backbuffer lo
                //      borraría. El filtro ya quedó activo: vanilla captura
                //      el frame SIGUIENTE (CanCapture) y el calor lo viste.
                //   2. Tras pintar, el binding NO vuelve a la fuente: queda
                //      en pantallaAlternativa (screenTarget2, el buffer de
                //      trabajo que vanilla limpia y rellena él mismo). La
                //      fuente (el mundo) jamás se re-bindea.
                // ============================================================
                RenderTargetBinding[] previos = gd.GetRenderTargets();
                if (previos.Length == 0)
                    return false;   // mundo directo al backbuffer: el CAMINO DE
                                    // FILTROS no puede — (v6.83: el PASE RETRO
                                    // toma desde aquí — ver PaseRetro)

                bool pintado = false;
                try
                {
                    gd.SetRenderTarget(_campo);
                    gd.Clear(Color.Transparent);
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                        SamplerState.LinearClamp, DepthStencilState.None,
                        RasterizerState.CullNone);
                    try
                    {
                        Vector2 origen = Main.screenPosition;
                        float k = EscalaCampo;
                        Vector2 medioGlow = glow.Size() * 0.5f;

                        // v6.81 — LA CORRECCIÓN DE ZOOM: el campo se pinta
                        // SIN la matrix del spriteBatch (coords de pantalla
                        // directas), pero el mundo se ve a través de
                        // GameViewMatrix.Zoom — con zoom ≠ 1 cada px de mundo
                        // ocupa zoom px de pantalla. El lote del mundo resta
                        // screenPosition y deja que la matrix escale; aquí
                        // el zoom se aplica A MANO (con zoom 1 el
                        // comportamiento es el de siempre, byte a byte).
                        Vector2 zoom = Main.GameViewMatrix.Zoom;
                        if (zoom.X <= 0f || zoom.Y <= 0f || !float.IsFinite(zoom.X) || !float.IsFinite(zoom.Y))
                            zoom = Vector2.One;

                        // v6.83 — EL ANCLA DEL ZOOM (la matemática de
                        // ScreenShaderData.Apply descompilada): la cámara
                        // escala ALREDEDOR DEL CENTRO de la pantalla —
                        // px = (mundo − screenPosition − mediaPantalla·(1−1/zoom))·zoom.
                        // v6.81 multiplicaba por zoom SIN el término del
                        // centro (ancla de ESQUINA): con zoom ≠ 1 (el zoom
                        // forzado de 4K/ultrawide, el slider de zoom) el
                        // campo quedaba desplazado hasta MEDIA PANTALLA
                        // del haz — el aire hervía donde NO estaba el
                        // conducto. Con zoom 1: byte a byte, el de siempre.
                        Vector2 ancla = origen + new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f
                            * (Vector2.One - Vector2.One / zoom);

                        // ——— LOS HACES: la cápsula cónica — el paso ADAPTATIVO
                        //      (denso donde el haz es gordo, ralo en la aguja:
                        //      el hervidero vive en la boca) ———
                        //      v6.85 — LA LEY DEL SOLAPE ×4: el glow de cada
                        //      paso cubre ahora ±2,6·radio (EL AIRE ALREDEDOR
                        //      del conducto — el calor no vive solo en el eje)
                        //      y el avance baja a radio·1,15: cada punto del
                        //      eje cae bajo 4-5 glows superpuestos → H SATURA
                        //      (antes: cuentas aisladas de 6,4 px de campo
                        //      cada 52 px — el 93 % del conducto veía H≈0).
                        for (int h = 0; h < _nHaz; h++)
                        {
                            Vector2 a = _hazDesde[h], b = _hazHasta[h];
                            Vector2 delta = b - a;
                            float largo = delta.Length();
                            if (largo < 8f) continue;
                            Vector2 dir = delta / largo;
                            float inten = _hazIntensidad[h];

                            float avance = 0f;
                            int pasos = 0;
                            while (avance <= largo && pasos < 64)
                            {
                                float t = largo > 0f ? avance / largo : 0f;
                                float ancho = MathHelper.Lerp(_hazAnchoBoca[h],
                                    _hazAnchoPunta[h], MathF.Pow(t, 0.8f));
                                float radio = ancho * 0.5f;
                                // EL PESO: pleno en la boca, menguando hacia la
                                // aguja (el aire caliente sigue al VOLUMEN del haz).
                                float peso = inten * (1f - 0.62f * t);
                                if (peso > 0.02f)
                                {
                                    Vector2 pos = (a + dir * avance - ancla) * zoom * k;
                                    // v6.88 — EL GLOW DEL GRUPO: ×1,25·radio (el
                                    //     aire PEGADO al conducto — la cura del
                                    //     «se ve en toda la pantalla pero el arma
                                    //     se queda fuera»: la v6.85 lo pintaba a
                                    //     ±2,6·radio y con las bocas de 3.000 px
                                    //     de la .85 el campo inundaba la vista
                                    //     entera; ahora la cápsula la dicta el
                                    //     ARMA (boca ×1,35 en el registro) y el
                                    //     pintor la respeta — el hervor vive en
                                    //     la franja del haz, y solo ahí).
                                    float esc = radio * 1.25f * zoom.X * k / medioGlow.X;
                                    Main.spriteBatch.Draw(glow, pos, null,
                                        new Color(new Vector3(peso * 0.42f, 0f, 0f)),
                                        0f, medioGlow, esc, SpriteEffects.None, 0f);
                                }
                                avance += MathF.Max(24f, radio * 1.15f);
                                pasos++;
                            }
                        }

                        // ——— LOS FOCOS: el gradiente radial ———
                        for (int p = 0; p < _nPunto; p++)
                        {
                            Vector2 pos = (_puntoPos[p] - ancla) * zoom * k;
                            float esc = _puntoRadio[p] * zoom.X * k / medioGlow.X;
                            Main.spriteBatch.Draw(glow, pos, null,
                                new Color(new Vector3(_puntoIntensidad[p] * 0.45f, 0f, 0f)),
                                0f, medioGlow, esc, SpriteEffects.None, 0f);
                        }

                        // ——— LOS ESTALLIDOS: EL ANILLO QUE SE EXPANDE (la onda
                        //      del aire caliente — ease-out: rápida al abrir,
                        //      lenta al morir) + el corazón temprano ———
                        Vector2 medioRing = ring.Size() * 0.5f;
                        for (int e = 0; e < MaxEstallido; e++)
                        {
                            if (_estNacido[e] == 0) continue;
                            float edad = (frame - _estNacido[e]) / 60f;
                            float tVida = edad / VidaEstallido;
                            if (tVida >= 1f) continue;
                            float env = Utils.GetLerpValue(0f, 0.1f, edad, true)
                                * MathF.Exp(-edad * 5.5f);
                            // EL FRENTE DE ONDA: ease-out cuadrático.
                            float frente = 1f - MathF.Pow(1f - tVida, 2f);
                            Vector2 pos = (_estPos[e] - ancla) * zoom * k;
                            float radio = _estRadio[e] * frente;
                            if (radio > 3f)
                            {
                                float esc = radio * zoom.X * k / medioRing.X;
                                Main.spriteBatch.Draw(ring, pos, null,
                                    new Color(new Vector3(env * 0.65f, 0f, 0f)),
                                    0f, medioRing, esc, SpriteEffects.None, 0f);
                            }
                            // EL CORAZÓN: el charco caliente del golpe (muere
                            // rápido — el anillo es lo que viaja).
                            if (tVida < 0.4f)
                            {
                                float corazon = (1f - tVida / 0.4f) * env;
                                float escC = _estRadio[e] * 0.35f * zoom.X * k / medioGlow.X;
                                Main.spriteBatch.Draw(glow, pos, null,
                                    new Color(new Vector3(corazon * 0.62f, 0f, 0f)),
                                    0f, medioGlow, escC, SpriteEffects.None, 0f);
                            }
                        }
                    }
                    finally
                    {
                        Main.spriteBatch.End();
                    }
                    pintado = true;
                    _campoSucio = true;   // v6.81 — la muerte suave lo sabrá
                    _framePintado = frame; // v6.85 — el sello de la VERDAD (F8)
                }
                finally
                {
                    // LA CURA v6.79 (2/2): JAMÁS re-bindear la fuente. El
                    // binding queda en el buffer de trabajo de vanilla; si
                    // algo faltara (filtro muerto a mitad, buffer nulo), la
                    // última defensa restaura el binding viejo.
                    if (pintado && _filtroActivo && salida != null
                        && !salida.IsDisposed)
                        gd.SetRenderTarget(salida);
                    else
                        gd.SetRenderTargets(previos);
                }

                // (la pintura terminó: el binding quedó en «salida» — o en
                // los previos — y el campo a ¼ lleva el calor de ESTE frame)
                return true;
            // OJO: sin catch — las excepciones suben al caller (el pase de
            // filtros apaga el filtro con _shaderFallo; el retro muere con
            // _retroFallo: cada camino con SU defensa, como siempre).
        }

        // ================================================================
        //  v6.83 — EL PASE RETRO (el aire que hierve SIN captura)
        // ================================================================
        // LA VERDAD DEL flag2 (Main.DoDraw descompilado):
        //     flag2 = !(drawToScreen || netMode==2 || flag) && !mapFullscreen
        //             && Lighting.NotRetro && Filters.Scene.CanCapture()
        // DOS AJUSTES DE VÍDEO del juego matan la captura SIN aviso y
        // SIN un error en el log (el síntoma perfecto de «no veo nada»):
        //   · Iluminación RETRO/TRIPPY (Lighting.NotRetro = false)
        //   · «Lighting Update Every Frame» (drawToScreen = true)
        // En ese modo el mundo se dibuja DIRECTO AL BACKBUFFER y la
        // cadena de filtros JAMÁS corre: ni calor, ni lente, ni siquiera
        // el tinte del cementerio de vanilla.
        //
        // LA CURA — LA CAPTURA FORZADA (la que vanilla no nos da): el LENS
        // SYSTEM BINDEA SU PROPIO RT en ModifyTransformMatrix (tras el
        // Clear, ANTES del primer trazo del mundo — la ventana limpia del
        // frame) y TODO el mundo cae ahí; en el punto 36 (mundo completo,
        // UI sin empezar, spriteBatch cerrado) ESTE pase corre el shader
        // del calor MANUALMENTE sobre ese mundo (el patrón probado del
        // HazConcentrado: parámetros + Passes[0].Apply + draw) y lo pinta
        // DIRECTO al backbuffer — UN draw, cero copias intermedias.
        // (GetBackBufferData NO existe en GPU en el FNA de tML — solo el
        // readback a CPU, inviable por frame — la captura forzada es LA
        // única vía.)
        // LA LEY DEL DRAW DEL 100 %: el volcado cubre TODO el viewport —
        // aunque algo raro pasara con los bindings a mitad de camino, la
        // pantalla termina siendo mundo+calor (JAMÁS negro).
        // ================================================================
        internal static bool PaseRetro(GraphicsDevice gd, Texture2D mundo)
        {
            if (_shaderFallo || _retroFallo || _filtro == null || gd == null) return false;
            if (mundo == null || mundo.IsDisposed) return false;
            try
            {
                if (Main.dedServ || Main.gameMenu || Main.mapFullscreen) return false;
                if (!VFXCore.CalidadPermitida(VFXCore.CalidadFX.WarpPantalla)) return false;
                ulong frame = Main.GameUpdateCount;
                if (frame == _framePreparado) return false;   // el camino de filtros YA compuso este frame
                if (!FuentesVivas(frame)) return false;        // sin fuentes: nada que hervir

                // LA PANTALLA ACTUAL debe ser el BACKBUFFER (el draw final
                // de este pase pinta AHÍ — el caller la dejó lista).
                RenderTargetBinding[] previos = gd.GetRenderTargets();
                if (previos.Length != 0) return false;

                // EL CAMPO (¼) — pintado fresco para ESTE frame (salida
                // null: el binding vuelve a los previos — el backbuffer).
                if (!PintarCampo(frame, null, out Vector2 centroCA,
                        out float caFuerza, out float calidad))
                    return false;

                // LOS PARÁMETROS — LA MATEMÁTICA EXACTA de
                // ScreenShaderData.Apply (descompilada): sin Apply no hay
                // vanilla que los ponga (el mismo zoom-correcto del camino
                // de filtros — verificado contra el fuente real arriba).
                Vector2 zoom = Main.GameViewMatrix.Zoom;
                if (zoom.X <= 0f || zoom.Y <= 0f || !float.IsFinite(zoom.X) || !float.IsFinite(zoom.Y))
                    zoom = Vector2.One;
                Vector2 borde = new Vector2(Main.offScreenRange, Main.offScreenRange);
                Vector2 tam = new Vector2(Main.screenWidth, Main.screenHeight);
                Effect fx = _filtro.GetShader().Shader;
                EffectParameterCollection pars = fx.Parameters;
                pars["uOpacity"].SetValue(1f);
                pars["uIntensity"].SetValue(1f);
                pars["uTime"].SetValue(Main.GlobalTimeWrappedHourly);
                pars["uScreenResolution"].SetValue(tam / zoom);
                pars["uScreenPosition"].SetValue(
                    Main.screenPosition + tam * 0.5f * (Vector2.One - Vector2.One / zoom) - borde);
                pars["uTargetPosition"].SetValue(centroCA - borde);
                pars["uTexturaCampo"].SetValue(_campo);
                if (_texRuido != null)
                    pars["uTexturaRuido"].SetValue(_texRuido.Value);
                pars["uAnchoPx"].SetValue(AnchoPx);
                pars["uCAFuerza"].SetValue(caFuerza);
                pars["uCARadio"].SetValue(RadioCA);
                pars["uConveccion"].SetValue(Conveccion);
                pars["uCalidad"].SetValue(calidad);

                // EL PASE FINAL: «mundo» (el crudo o el lensado) pasa por el
                // shader del calor DIRECTO al backbuffer — la textura
                // dibujada cae en s0 (texPantalla) y el volcado cubre el
                // 100 % del viewport (la ley del draw — jamás negro).
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Matrix.Identity);
                try
                {
                    fx.CurrentTechnique.Passes[0].Apply();
                    Main.spriteBatch.Draw(mundo,
                        new Rectangle(0, 0, gd.Viewport.Width, gd.Viewport.Height), Color.White);
                }
                finally
                {
                    Main.spriteBatch.End();
                }
                _frameRetro = frame;
                return true;
            }
            catch
            {
                // LA LEY DE LA CASA: el calor jamás rompe el frame — el
                // pase retro se apaga para siempre y el juego sigue.
                _retroFallo = true;
                return false;
            }
        }

        private static void Activar()
        {
            if (_filtroActivo || _filtro == null) return;
            try
            {
                Filters.Scene.Activate(NombreFiltro, Vector2.Zero);
                // v6.81 — SIN EL FADE-IN DE VANILLA (OPACITY_RATE = 1/s): el
                // ESTALLIDO de la ignición vive 1.05 s — con el fade de 1 s
                // del filtro la aberración cromática NUNCA habría llegado a
                // verse (0.016 → 1.0 mientras el golpe ya murió). El haz
                // trae SU PROPIA rampa (kCalor: la carga del conducto) — el
                // pop no existe; y si el filtro re-enciende a mitad de un
                // fade (calor nuevo tras la muerte), la opacidad se respeta
                // (solo se fuerza cuando estaba a cero).
                if (_filtro.Opacity <= 0f)
                    _filtro.Opacity = 1f;
                // v6.82 — LA CURA DEL FRAME CIEGO: FilterManager.OnActivate
                // PONE Opacity=0 y Filter.Activate empuja ese 0 al SHADER
                // (UseGlobalOpacity(0)) — y IsVisible() lee COMBINEDOPACITY
                // DEL SHADER, no del filtro: el frame N de la ignición el
                // filtro estaba ACTIVO pero INVISIBLE (la cadena lo saltaba;
                // recién el FilterManager.Update del frame N+1 empujaba el 1).
                // Se empuja el 1 AL SHADER aquí mismo — cero frames ciegos.
                _filtro.GetShader().UseGlobalOpacity(1f);
                _filtroActivo = true;
            }
            catch
            {
                _filtroActivo = false;
            }
        }

        private static void Desactivar()
        {
            if (!_filtroActivo) return;
            _filtroActivo = false;
            try
            {
                // EL GUARD DEL LOG LIMPIO: el getter del indexer devuelve null
                // si no está registrado — Deactivate sobre un ausente lanza
                // MissingEffectException (atrapada… pero tML loggea TODAS las
                // first-chance: el client.log del jugador quedaría sucio).
                if (Filters.Scene[NombreFiltro] != null)
                    Filters.Scene.Deactivate(NombreFiltro);
            }
            catch { }
        }

        /// <summary>LA MUERTE SUAVE DEL CAMPO (v6.81: con bandera — solo se
        /// vacía lo que está sucio, y si el vaciado no pudo completarse el
        /// sello queda para reintentar): el RT se limpia y la CA calla — el
        /// filtro que se desvanece ya no tiene nada que distorsionar (sin
        /// esto, el calor del último frame quedaría CONGELADO un segundo
        /// entero).</summary>
        // v6.79 — VaciarCampo aprendió la cura del binding: sin captura
        // (previos vacío) no se toca NADA (restaurar el backbuffer lo
        // borraría — la pantalla negra «sin previo aviso» del fade), y con
        // captura el binding sale hacia el buffer de trabajo de vanilla,
        // jamás de vuelta a la fuente (el mundo capturado).
        private static bool VaciarCampo(RenderTarget2D pantallaAlternativa)
        {
            bool limpio = false;
            try
            {
                if (_filtro != null)
                    _filtro.GetShader().Shader.Parameters["uCAFuerza"].SetValue(0f);
            }
            catch { }
            try
            {
                GraphicsDevice gd = Main.graphics?.GraphicsDevice;
                if (gd != null && _campo != null && !_campo.IsDisposed)
                {
                    RenderTargetBinding[] previos = gd.GetRenderTargets();
                    if (previos.Length == 0)
                        return false;  // sin captura: el backbuffer queda intacto (y el sello sigue sucio)
                    gd.SetRenderTarget(_campo);
                    gd.Clear(Color.Transparent);
                    if (pantallaAlternativa != null && !pantallaAlternativa.IsDisposed)
                        gd.SetRenderTarget(pantallaAlternativa);
                    else
                        gd.SetRenderTargets(previos);
                    limpio = true;
                }
            }
            catch { }
            return limpio;
        }

        /// <summary>v6.81 — EL GUARDIA DEL SELLO: vacía el campo solo si
        /// lleva calor, y solo baja el sello cuando el vaciado COMPLETÓ
        /// (un device-lost a mitad no puede dejar el campo sucio engañando
        /// a la muerte suave).</summary>
        private static void VaciarSiSucio(RenderTarget2D pantallaAlternativa)
        {
            if (!_campoSucio) return;
            if (VaciarCampo(pantallaAlternativa))
                _campoSucio = false;
        }

        /// <summary>v6.81 — LA TELEMETRÍA DEL F8 (la línea que habría
        /// cazado el catch-22 en un segundo: «filtro OFF · fuentes 2» era
        /// la firma del encierro). Compacta y técnica, como la de Furia.</summary>
        public static string Diagnostico()
        {
            if (_shaderFallo) return "shader FALLÓ";
            if (_retroFallo) return _filtroActivo ? "ON · ¡RETRO FALLÓ!" : "off";
            ulong f = Main.GameUpdateCount;
            // v6.83 — LA COMPOSICIÓN REAL, NO LA INTENCIÓN: hasta v6.82 la
            // línea podía decir «ON · 1 fuentes» mientras la pantalla JAMÁS
            // se capturaba (Retro/Trippy o «Lighting Update Every Frame»)
            // — el diagnóstico mentía mientras el aire no se veía. Hoy:
            // «RETRO» = el pase del backbuffer ESTÁ hirviendo el aire;
            // «SIN CAPTURA» = el filtro vive pero NADA compone (el modo
            // de vídeo mata la cadena — se ve EN UN VISTAZO).
            if (_frameRetro == f)
                return $"ON · {Fuentes} fuentes · RETRO";
            if (!_filtroActivo) return "off";
            if (_framePreparado == f)
            {
                if (_porLente)
                    return "ON · por lente (campo vacío)";
                // v6.85 — LA LÍNEA QUE NO MIENTE: «campo ✓» solo si el campo
                // se pintó DE VERDAD este frame; «CAMPO SIN PINTAR» es la
                // firma del pase que corrió pero no pudo vestir al filtro.
                return _framePintado == f
                    ? $"ON · {Fuentes} fuentes · campo ✓"
                    : $"ON · {Fuentes} fuentes · CAMPO SIN PINTAR";
            }
            return _porLente
                ? "ON · por lente · SIN CAPTURA (¿iluminación Retro?)"
                : $"ON · {Fuentes} fuentes · SIN CAPTURA (¿iluminación Retro?)";
        }

        /// <summary>El sello del frame de registración (la frescura de las
        /// fuentes: lo llaman Haz/Punto/Estallido al registrar).</summary>
        private static void Tocar()
        {
            ulong f = Main.GameUpdateCount;
            if (f != _frameReg)
            {
                _frameReg = f;
                _nHaz = 0;   // el frame nuevo trae fuentes nuevas (el viejo
                _nPunto = 0; //  murió con su proyectil)
            }
        }

        // ==================== LA CARGA / DESCARGA ========================
        // (la llama CalorSistema — el ModSystem del archivo)

        internal static bool Cargar(Mod mod)
        {
            try
            {
                Asset<Effect> fx = ModContent.Request<Effect>(
                    "AethonMod/Content/Effects/Shaders/CalorDistorsionShader",
                    AssetRequestMode.ImmediateLoad);
                Effect efecto = fx.Value;
                if (efecto == null) { _shaderFallo = true; return false; }

                _texRuido = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/RuidoPlasmaCalor",
                    AssetRequestMode.ImmediateLoad);

                // EL FILTRO DE CADENA: ScreenShaderData envuelve el efecto y
                // el pase ("PaseCalor" — el nombre del pass del .fx); el
                // VeryHigh lo pone POR ENCIMA del umbral del jugador (el
                // tutorial del shockwave comunitario: los filtros Low mueren
                // con el FilterPriorityThreshold por defecto... el nuestro
                // no).
                _filtro = new Filter(new ScreenShaderData(fx, "PaseCalor"),
                    Terraria.Graphics.Effects.EffectPriority.VeryHigh);
                Filters.Scene[NombreFiltro] = _filtro;
                _filtroActivo = false;
                _shaderFallo = false;
                return true;
            }
            catch
            {
                _shaderFallo = true;
                _filtro = null;
                return false;
            }
        }

        internal static void Descargar()
        {
            try
            {
                // LA DEFENSA DEL RELOAD: IsHidden quita al filtro de la
                // cadena AL INSTANTE (IsVisible cae a false) y Deactivate
                // lo desvanece del registro — un filtro activo con el
                // efecto muerto sería un crash en el próximo frame de
                // cualquier otra captura. (El guard del null: en el arranque
                // en frío el filtro aún no existe — Deactivate sobre un
                // ausente ensucia el log con first-chance.)
                if (_filtro != null)
                    _filtro.IsHidden = true;
                if (Filters.Scene[NombreFiltro] != null)
                    Filters.Scene.Deactivate(NombreFiltro);
            }
            catch { }
            _filtro = null;
            _filtroActivo = false;
            _texRuido = null;
            _retroFallo = false;
            _frameRetro = 0;
            _framePintado = 0;

            // LA LECCIÓN v5.87: RenderTarget2D.Dispose() en el hilo de
            // carga lanza ThreadStateException — la destrucción se encola
            // al hilo principal (Main.Update drena la cola).
            if (_campo != null)
            {
                RenderTarget2D target = _campo;
                _campo = null;
                try
                {
                    Main.QueueMainThreadAction(() =>
                    {
                        try { target.Dispose(); }
                        catch { }
                    });
                }
                catch { }
            }
        }
    }

    // =====================================================================
    //  CalorSistema — EL ModSystem DE LA LIBRERÍA: registra el filtro en
    //  Load (la asignación al indexer de Filters.Scene ES el registro — el
    //  patrón del tutorial comunitario del shockwave) y lo retira limpio
    //  en Unload. El pase del frame lo disparan los demás (el hook de la
    //  lente llama a CalorLib.PrepararPaseDeCalor): este sistema no tiene
    //  punto de dibujado propio.
    //
    //  v6.81 — LA DECISIÓN VIVE AQUÍ: PostUpdateEverything corre al final
    //  de Main.Update (el MISMO latido de CalidadFpsSystem — v6.49), con la
    //  IA de los proyectiles ya corrida (las fuentes del calor están
    //  registradas frescas) y ANTES del draw (el flag2 de captura de
    //  Main.DoDraw — Filters.Scene.CanCapture() — se evalúa DESPUÉS: el
    //  filtro activado en ESTE tick ya fuerza la captura de ESTE frame).
    //  La cura del catch-22: antes la activación vivía dentro del hook de
    //  EndCapture, que solo corre cuando ya había captura — el aire que
    //  hierve JAMÁS podía encenderse solo (ver CalorLib.EvaluarActivacion).
    // =====================================================================
    public class CalorSistema : ModSystem
    {
        public override void Load()
        {
            CalorLib.Descargar();          // recarga caliente: estado limpio
            CalorLib.Cargar(Mod);
        }

        public override void Unload()
        {
            CalorLib.Descargar();
        }

        /// <summary>v6.81 — EL LATIDO DE LA DECISIÓN: activa o desactiva el
        /// filtro de cadena según haya calor… o lente que necesite captura
        /// (el forzador). Update-side, cada tick, al final de TODO el update
        /// (el latido de CalidadFpsSystem), en SP y en clientes MP
        /// (Main.dedServ corta dentro).</summary>
        public override void PostUpdateEverything()
        {
            CalorLib.EvaluarActivacion();
        }
    }
}
