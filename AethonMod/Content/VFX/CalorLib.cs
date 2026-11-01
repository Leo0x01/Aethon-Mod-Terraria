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
    //   · REFRACTIÓN EN ESPACIO DE PANTALLA (Wolfire 2006, Linden Reid,
    //     Codrops): la distorsión re-muestrea la pantalla capturada en UVs
    //     desplazadas por ruido — EL postulado del heat haze 2D.
    //   · EL RUIDO QUE HIERVE EN SU SITIO (Starlight River,
    //     MoonstoneDistortion): dos octavas en DIAGONALES OPUESTAS — el
    //     aire hierve sin «fluir» — + la convección vertical de Rastertek
    //     (el sesgo ascendente que el ojo lee como «calor»).
    //   · EL CAMPO DE CALOR A ¼ DE RESOLUCIÓN (el patrón del
    //     WaterShaderData de vanilla, DISTORTION_BUFFER_SCALE 0.25): las
    //     fuentes se pintan ADITIVAS en un RT diminuto (1/16 de los
    //     píxeles) — donde dos fuentes se solapan, el calor SE SUMA.
    //   · LA ABERRACIÓN CROMÁTICA DEL ESTALLIDO (Unity PPv2, el 90 % de
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
    //  advertencia de Starlight River); el frame se deduplica por
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
        private const float AnchoPx = 5.5f;    // la amplitud: px de flexión con calor pleno
        private const float RadioCA = 0.36f;   // el estallido: ⅓ de pantalla alrededor del golpe
        private const float Conveccion = 0.5f; // el sesgo ascendente del aire caliente

        /// <summary>¿Vive el filtro? (para la telemetría del F8).</summary>
        public static bool Activo => _filtroActivo && !_shaderFallo;

        // ====================== EL PASE DEL FRAME =========================
        // Lo llama BlackHoleLensSystem.ComponerLenteBajoFiltros — UN solo
        // hook de EndCapture por proceso (la advertencia de Starlight
        // River: dos hooks que se pelean el finalTexture se rompen; aquí
        // la lente y el calor viven en EL MISMO sitio).
        // ================================================================
        internal static void PrepararPaseDeCalor()
        {
            if (_shaderFallo || _filtro == null) return;
            try
            {
                ulong frame = Main.GameUpdateCount;
                if (frame == _framePreparado) return;   // dedup del frame
                _framePreparado = frame;

                // LA PUERTA DE CALIDAD (la familia WarpPantalla — «lentes,
                // calor»: el enum la esperaba desde v6.31) y el guard del
                // menú (la lección de PantallaLib).
                if (Main.gameMenu || !VFXCore.CalidadPermitida(VFXCore.CalidadFX.WarpPantalla))
                {
                    Desactivar();
                    return;
                }

                // ¿HAY FUENTES? Los haces/puntos deben ser FRESCOS (un frame
                // de vida: el proyectil los re-registra CADA tick — el que
                // calló, murió). Los estallidos viven por EDAD.
                bool fuentesFrescas = (_nHaz > 0 || _nPunto > 0) && frame - _frameReg <= 1;
                bool hayEstallidos = false;
                for (int i = 0; i < MaxEstallido; i++)
                {
                    if (_estNacido[i] == 0) continue;
                    float edad = (frame - _estNacido[i]) / 60f;
                    if (edad < VidaEstallido) { hayEstallidos = true; break; }
                }

                if (!fuentesFrescas && !hayEstallidos)
                {
                    if (_filtroActivo)
                    {
                        Desactivar();
                        // LA MUERTE SUAVE: mientras el filtro se desvanece
                        // (±1 s — IsVisible sigue true hasta Opacity = 0),
                        // la cadena seguiría leyendo el CAMPO CONGELADO del
                        // último frame — el calor hay que ENTREGARLO al
                        // fade: el campo se VACÍA en la transición y lo que
                        // muere es el aire, no el frame.
                        VaciarCampo();
                    }
                    return;
                }

                Activar();

                // === LA FUERZA ADAPTATIVA (VFXCore.FactorCalidad): las
                //     octavas del hervidero y la CA respiran con los FPS —
                //     la calidad que se adapta sola (v6.34).
                float fc = VFXCore.FactorCalidad;
                float calidad = fc >= 0.9f ? 3f : fc >= 0.72f ? 2f : 1f;

                // === EL ESTALLIDO VIVO MÁS FUERTE: el centro de la CA ===
                Vector2 centroCA = Vector2.Zero;
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
                float caFuerza = 0f;
                for (int i = 0; i < MaxEstallido; i++)
                {
                    if (_estNacido[i] == 0) continue;
                    float edad = (frame - _estNacido[i]) / 60f;
                    float env = Utils.GetLerpValue(0f, 0.1f, edad, true)
                        * MathF.Exp(-edad * 5.5f) * MathF.Min(1f, fc + 0.25f);
                    caFuerza += _estFuerza[i] * env;
                }
                if (mejorEnv <= 0f && fuentesFrescas && _nHaz > 0)
                    centroCA = _hazDesde[0];   // sin estallido: la boca manda

                // === 1. EL CAMPO DE CALOR (¼ de resolución, aditivo) ===
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (gd == null) { Desactivar(); return; }

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
                if (glow == null || ring == null) { Desactivar(); return; }

                RenderTargetBinding[] previos = gd.GetRenderTargets();
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

                    // ——— LOS HACES: la cápsula cónica — el paso ADAPTATIVO
                    //      (denso donde el haz es gordo, ralo en la aguja:
                    //      el hervidero vive en la boca) ———
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
                                Vector2 pos = (a + dir * avance - origen) * k;
                                float esc = radio * k / medioGlow.X;
                                Main.spriteBatch.Draw(glow, pos, null,
                                    new Color(new Vector3(peso * 0.30f, 0f, 0f)),
                                    0f, medioGlow, esc, SpriteEffects.None, 0f);
                            }
                            avance += MathF.Max(52f, radio * 1.1f);
                            pasos++;
                        }
                    }

                    // ——— LOS FOCOS: el gradiente radial ———
                    for (int p = 0; p < _nPunto; p++)
                    {
                        Vector2 pos = (_puntoPos[p] - origen) * k;
                        float esc = _puntoRadio[p] * k / medioGlow.X;
                        Main.spriteBatch.Draw(glow, pos, null,
                            new Color(new Vector3(_puntoIntensidad[p] * 0.32f, 0f, 0f)),
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
                        Vector2 pos = (_estPos[e] - origen) * k;
                        float radio = _estRadio[e] * frente;
                        if (radio > 3f)
                        {
                            float esc = radio * k / medioRing.X;
                            Main.spriteBatch.Draw(ring, pos, null,
                                new Color(new Vector3(env * 0.55f, 0f, 0f)),
                                0f, medioRing, esc, SpriteEffects.None, 0f);
                        }
                        // EL CORAZÓN: el charco caliente del golpe (muere
                        // rápido — el anillo es lo que viaja).
                        if (tVida < 0.4f)
                        {
                            float corazon = (1f - tVida / 0.4f) * env;
                            float escC = _estRadio[e] * 0.35f * k / medioGlow.X;
                            Main.spriteBatch.Draw(glow, pos, null,
                                new Color(new Vector3(corazon * 0.5f, 0f, 0f)),
                                0f, medioGlow, escC, SpriteEffects.None, 0f);
                        }
                    }
                }
                finally
                {
                    Main.spriteBatch.End();
                    gd.SetRenderTargets(previos);
                }

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

        private static void Activar()
        {
            if (_filtroActivo || _filtro == null) return;
            try
            {
                Filters.Scene.Activate(NombreFiltro, Vector2.Zero);
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

        /// <summary>LA MUERTE SUAVE DEL CAMPO: el RT se limpia y la CA
        /// calla — el filtro que se desvanece ya no tiene nada que
        /// distorsionar (sin esto, el calor del último frame quedaría
        /// CONGELADO un segundo entero).</summary>
        private static void VaciarCampo()
        {
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
                    gd.SetRenderTarget(_campo);
                    gd.Clear(Color.Transparent);
                    gd.SetRenderTargets(previos);
                }
            }
            catch { }
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
    }
}
