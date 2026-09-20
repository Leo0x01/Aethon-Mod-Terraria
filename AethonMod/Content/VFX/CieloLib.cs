using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    // ======================================================================
    //  v6.43 — CIELOLIB: LA LIBRERÍA DEL FONDO
    //
    //  Petición del usuario: "una librería que sea capaz de interactuar con
    //  el fondo de Terraria, o sea con los paisajes que muestra sus capas,
    //  parallax y demás cosas del fondo del juego".
    //
    //  TRES PIEZAS:
    //   1. CAPAS LIBRES (parallax propio SIEMPRE visible): una EscenaDeCielo
    //      es una lista de Capas (siluetas, nebulosas, estrellas...) que se
    //      dibujan ENCIMA del fondo del juego y DEBAJO de los tiles, con su
    //      propio parallax, deriva horizontal, escala y tinte. El motor vive
    //      en el hook del dibujado del fondo vanilla (se dibuja después de
    //      su paisaje, antes del terreno).
    //   2. EFECTOS DE ESCENA: cada escena puede teñir el cielo (el color del
    //      cielo del juego), teñir la luz ambiental del fondo y mover el
    //      brillo de la iluminación — con fundidos de entrada y salida.
    //   3. FONDOS DE BIOMA: el estilo de superficie y de subsuelo del
    //      Sagrario Hueco (SanctumBackgroundStyle, en su propio archivo)
    //      enchufan el paisaje del bioma al sistema de fondos del juego.
    //
    //  Convenciones de la casa: cero Main.rand en render (las capas son
    //  deterministas), cero GC por frame (capas pre-ordenadas, texturas
    //  cacheadas como Asset), lotes con try/finally y el estado del
    //  SpriteBatch SIEMPRE restaurado (la lección del volcado blindado).
    // ======================================================================

    /// <summary>
    /// UNA CAPA del paisaje: una textura que se repite horizontalmente al
    /// infinito y viaja con su propio parallax.
    /// <para>
    /// · <see cref="Parallax"/>: 0 = fija como el cielo lejano, 1 = se mueve
    ///   1:1 con el terreno. Los valores de un paisaje natural van de 0.05
    ///   (estrellas) a 0.6 (siluetas cercanas).
    /// · <see cref="ScrollX"/>: deriva automática en píxeles/segundo del
    ///   mundo (el viento de las nubes, la corriente de las nebulosas).
    /// · <see cref="Escala"/>: qué fracción de la ALTURA DE PANTALLA ocupa
    ///   la capa (0.55 = un poco más de media pantalla). La anchura conserva
    ///   el aspecto de la textura.
    /// · <see cref="OffsetY"/>: píxeles de pantalla que SUBEN la capa desde
    ///   su anclaje (que ya depende de la profundidad).
    /// · <see cref="Profundidad"/>: 0 = horizonte lejano, 1 = primer plano.
    ///   Ordena el dibujado (lejos → cerca) y eleva a las capas lejanas.
    /// · <see cref="Brillante"/>: true = se dibuja en lote ADITIVO (la capa
    ///   SUMA luz: estrellas, nebulosas luminosas). false = lote alfa normal
    ///   (siluetas que tapan).
    /// </para>
    /// <para>La ruta es RELATIVA AL MOD (sin el nombre del mod delante):
    /// "Content/Effects/Cielo/MiCapa".</para>
    /// </summary>
    public struct Capa
    {
        /// <summary>Ruta de la textura, relativa al mod (sin su nombre).</summary>
        public string RutaTextura;

        /// <summary>0 = fija como el cielo · 1 = se mueve 1:1 como el terreno.</summary>
        public float Parallax;

        /// <summary>Píxeles de pantalla que SUBEN la capa desde su anclaje.</summary>
        public float OffsetY;

        /// <summary>Fracción de la altura de pantalla que ocupa la capa.</summary>
        public float Escala;

        /// <summary>Alfa de la capa (0..1), antes del fundido de la escena.</summary>
        public float Alpha;

        /// <summary>Tinte multiplicativo sobre la textura (blanco = tal cual).</summary>
        public Color Tinte;

        /// <summary>Deriva automática en px/segundo (hacia la izquierda si es negativa).</summary>
        public float ScrollX;

        /// <summary>0 = horizonte lejano · 1 = primer plano (ordena y eleva).</summary>
        public float Profundidad;

        /// <summary>True = lote aditivo (suma luz); false = lote alfa (tapar).</summary>
        public bool Brillante;
    }

    /// <summary>
    /// UNA ESCENA del cielo: capas ordenadas por profundidad + los efectos
    /// globales del paisaje (tintes y brillo). Se construye una vez, se
    /// activa con <see cref="CieloLib.Activar"/> y el motor hace el resto.
    /// </summary>
    public class EscenaDeCielo
    {
        /// <summary>El nombre de la escena (para <see cref="CieloLib.Desactivar"/> y los mensajes).</summary>
        public readonly string Nombre;

        /// <summary>Las capas del paisaje (se ordenan por profundidad al
        /// activarse la escena — el motor lo garantiza, idempotente).</summary>
        public readonly List<Capa> Capas;

        private bool _capasOrdenadas;

        /// <summary>Tinte del cielo: el color del cielo del juego se funde
        /// hacia este color mientras la escena está activa.</summary>
        public Color? TinteDelCielo;

        /// <summary>Tinte de la luz de fondo (el color ambiental que el
        /// motor de iluminación usa detrás de todo).</summary>
        public Color? TinteDelFondo;

        /// <summary>Brillo de la iluminación (1 = neutro, 0.8 = penumbra,
        /// 1.1 = amanecer). null = no tocar.</summary>
        public float? Brillo;

        /// <summary>Segundos del fundido de entrada.</summary>
        public float FadeInSeg;

        /// <summary>Segundos del fundido de salida.</summary>
        public float FadeOutSeg;

        /// <summary>Construye la escena con sus capas (añadidas aquí o por
        /// inicializador de colección — el motor las ordena al activar).</summary>
        public EscenaDeCielo(string nombre, params Capa[] capas)
        {
            Nombre = nombre ?? "";
            Capas = new List<Capa>(capas);
            FadeInSeg = 1.5f;
            FadeOutSeg = 1.0f;
        }

        /// <summary>
        /// Ordena las capas por profundidad (lejos → cerca: la MENOR se
        /// dibuja primero y queda detrás). Lo llama el motor al activar la
        /// escena — idempotente, cero trabajo por frame.
        /// </summary>
        internal void AsegurarOrdenDeCapas()
        {
            if (_capasOrdenadas) return;
            Capas.Sort((a, b) => a.Profundidad.CompareTo(b.Profundidad));
            _capasOrdenadas = true;
        }
    }

    /// <summary>
    /// EL GESTOR DEL CIELO — la API pública de la librería. Una escena se
    /// activa, se desactiva o se cambia con fundido cruzado (la saliente se
    /// desvanece mientras la entrante sube su alfa). Todo el estado es
    /// función pura del frame del juego: dos llamadas en el mismo frame
    /// ven exactamente lo mismo.
    /// </summary>
    public static class CieloLib
    {
        // --- EL ESTADO (una sola escena activa + la saliente del fundido) ---
        private static EscenaDeCielo _activa;
        private static EscenaDeCielo _saliente;
        private static uint _frameCambio;
        private static uint _frameDelUltimoDibujo;
        private static bool _falloDeRender;

        /// <summary>El nombre de la escena ACTIVA (null si el cielo está limpio).</summary>
        public static string NombreEscenaActiva => _activa?.Nombre;

        // --- EL CACHE DE TEXTURAS (Asset, resolución diferida — la casa) ---
        private static readonly Dictionary<string, Asset<Texture2D>> _texturas =
            new Dictionary<string, Asset<Texture2D>>(16);

        // ==================================================================
        //  LA API PÚBLICA
        // ==================================================================

        /// <summary>
        /// ACTIVA una escena (o la cambia con fundido cruzado). Idempotente:
        /// activar la escena que ya está activa no reinicia su fundido.
        /// </summary>
        public static void Activar(EscenaDeCielo escena)
        {
            if (escena == null) { DesactivarTodas(); return; }
            if (_activa == escena) return;

            escena.AsegurarOrdenDeCapas();          // lejos → cerca, una vez
            _saliente = _activa;                    // la actual empieza a desvanecerse
            _activa = escena;
            _frameCambio = Main.GameUpdateCount;
            _falloDeRender = false;                 // un fallo anterior se perdona
        }

        /// <summary>
        /// DESACTIVA la escena con ese nombre (si es la activa): fundido de
        /// salida y cielo limpio.
        /// </summary>
        public static void Desactivar(string nombre)
        {
            if (_activa == null || _activa.Nombre != nombre) return;
            _saliente = _activa;
            _activa = null;
            _frameCambio = Main.GameUpdateCount;
        }

        /// <summary>DESACTIVA todo lo que haya (con su fundido de salida).</summary>
        public static void DesactivarTodas()
        {
            if (_activa == null) return;
            _saliente = _activa;
            _activa = null;
            _frameCambio = Main.GameUpdateCount;
        }

        /// <summary>El estado interno completo se limpia (descarga del mod).</summary>
        internal static void Reiniciar()
        {
            _activa = null;
            _saliente = null;
            _texturas.Clear();
            _estiloSanctum = null;
            _estiloSanctuSubsuelo = null;
            _frameCambio = 0;
            _falloDeRender = false;
        }

        // ==================================================================
        //  LOS FUNDIDOS (función pura del frame — sin estado mutable)
        // ==================================================================

        /// <summary>El fundido de ENTRADA de la escena activa (0..1).</summary>
        private static float FadeDeEntrada(uint frame)
        {
            if (_activa == null) return 0f;
            if (_activa.FadeInSeg <= 0f) return 1f;
            return MathHelper.Clamp((frame - _frameCambio) / (_activa.FadeInSeg * 60f), 0f, 1f);
        }

        /// <summary>El fundido de SALIDA de la escena saliente (1..0).</summary>
        private static float FadeDeSalida(uint frame)
        {
            if (_saliente == null) return 0f;
            if (_saliente.FadeOutSeg <= 0f) return 0f;
            return 1f - MathHelper.Clamp((frame - _frameCambio) / (_saliente.FadeOutSeg * 60f), 0f, 1f);
        }

        /// <summary>¿Hay algo que fundir ahora mismo? (tinte, brillo o capas).</summary>
        private static bool HayEscena => _activa != null || _saliente != null;

        // ==================================================================
        //  LOS EFECTOS DE ESCENA (tintes y brillo — para CieloSistema)
        // ==================================================================

        /// <summary>
        /// El TINTE DEL CIELO efectivo (mezcla del saliente y la activa
        /// durante el fundido cruzado) y su fuerza. False si no hay tinte.
        /// </summary>
        internal static bool TinteDeCieloEfectivo(out Color tinte, out float fuerza)
            => MezclarTinte(_saliente?.TinteDelCielo, _saliente, _activa?.TinteDelCielo, _activa, out tinte, out fuerza);

        /// <summary>El TINTE DEL FONDO efectivo y su fuerza (para la luz ambiental).</summary>
        internal static bool TinteDeFondoEfectivo(out Color tinte, out float fuerza)
            => MezclarTinte(_saliente?.TinteDelFondo, _saliente, _activa?.TinteDelFondo, _activa, out tinte, out fuerza);

        /// <summary>El BRILLO efectivo de la iluminación (1 = neutro).</summary>
        internal static float BrilloEfectivo
        {
            get
            {
                uint frame = Main.GameUpdateCount;
                return MezclarBrillo(_saliente, FadeDeSalida(frame), _activa, FadeDeEntrada(frame));
            }
        }

        /// <summary>
        /// La media ponderada de un tinte entre saliente y activa: el color
        /// mezclado y la FUERZA total (clampeada a 1 — durante el cruce de
        /// dos fundidos simétricos la suma es exactamente 1).
        /// </summary>
        private static bool MezclarTinte(Color? tinteSal, EscenaDeCielo sal, Color? tinteAct, EscenaDeCielo act,
            out Color tinte, out float fuerza)
        {
            tinte = default;
            fuerza = 0f;
            uint frame = Main.GameUpdateCount;
            float wSal = tinteSal.HasValue && sal != null ? FadeDeSalida(frame) : 0f;
            float wAct = tinteAct.HasValue && act != null ? FadeDeEntrada(frame) : 0f;
            float total = wSal + wAct;
            if (total <= 0.0001f) return false;

            if (tinteSal.HasValue && tinteAct.HasValue)
                tinte = Color.Lerp(tinteSal.Value, tinteAct.Value, wAct / total);
            else
                tinte = tinteSal.HasValue ? tinteSal.Value : tinteAct.Value;

            fuerza = Math.Min(total, 1f);
            return true;
        }

        /// <summary>
        /// El brillo efectivo: media ponderada de los brillos, donde el peso
        /// que falta hasta 1 cuenta como NEUTRO (brillo 1).
        /// </summary>
        private static float MezclarBrillo(EscenaDeCielo sal, float wSal, EscenaDeCielo act, float wAct)
        {
            if (sal != null && !sal.Brillo.HasValue) wSal = 0f;
            if (act != null && !act.Brillo.HasValue) wAct = 0f;
            float total = wSal + wAct;
            if (total <= 0.0001f) return 1f;

            float vSal = sal?.Brillo ?? 1f;
            float vAct = act?.Brillo ?? 1f;
            float neutro = Math.Max(0f, 1f - total);
            return (vSal * wSal + vAct * wAct + neutro) / (wSal + wAct + neutro);
        }

        // ==================================================================
        //  EL RENDER — LAS CAPAS LIBRES (lo llama CieloSistema tras el
        //  fondo vanilla y antes de los tiles)
        // ==================================================================

        /// <summary>
        /// Dibuja las capas de las escenas activas. Defensivo por completo:
        /// si algo falla, el render del juego queda intacto y la librería
        /// se apaga hasta la próxima activación.
        /// </summary>
        internal static void DibujarCapas()
        {
            if (_falloDeRender) return;
            if (Main.netMode == NetmodeID.Server) return;
            if (Main.gameMenu) return;

            uint frame = Main.GameUpdateCount;
            // El fondo puede dibujarse más de una vez por frame (capturas,
            // repeticiones internas): solo la primera pasada pinta las capas.
            if (frame == _frameDelUltimoDibujo) return;
            if (!HayEscena) return;
            _frameDelUltimoDibujo = frame;

            // Higiene: la saliente terminada se despide (cero referencias colgadas).
            if (_saliente != null && FadeDeSalida(frame) <= 0f) _saliente = null;
            if (_activa == null && _saliente == null) return;

            SpriteBatch batch = Main.spriteBatch;

            // === EL FIN DEL LOTE DEL FONDO (la defensa de estado) ===
            // Si el fondo dejó un lote ABIERTO, este End lo cierra (y hay que
            // reabrirlo al terminar, como lo tenía). Si ya estaba cerrado, el
            // End no tiene lote que cerrar y lo avisa con una excepción — la
            // detectamos y NO reabrimos: el estado queda exactamente como estaba.
            bool loteDelFondoAbierto = false;
            try { batch.End(); loteDelFondoAbierto = true; }
            catch { loteDelFondoAbierto = false; }

            try
            {
                float fIn = FadeDeEntrada(frame);
                float fOut = FadeDeSalida(frame);
                float anchoPantalla = Main.screenWidth;
                float altoPantalla = Main.screenHeight;

                // PASO 1 — LAS CAPAS OPACAS (lote alfa: siluetas que tapan).
                if (HayCapas(false))
                {
                    batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                        SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone,
                        null, Matrix.Identity);
                    DibujarEscena(_saliente, fOut, anchoPantalla, altoPantalla, false);
                    DibujarEscena(_activa, fIn, anchoPantalla, altoPantalla, false);
                    batch.End();
                }

                // PASO 2 — LAS CAPAS BRILLANTES (lote aditivo: suman luz).
                if (HayCapas(true))
                {
                    batch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                        SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone,
                        null, Matrix.Identity);
                    DibujarEscena(_saliente, fOut, anchoPantalla, altoPantalla, true);
                    DibujarEscena(_activa, fIn, anchoPantalla, altoPantalla, true);
                    batch.End();
                }
            }
            finally
            {
                // El lote propio queda SIEMPRE cerrado (aunque un Draw tire a
                // mitad de pase — la lección del volcado blindado de v6.41).
                try { batch.End(); } catch { }

                // === LA REAPERTURA: el lote del fondo como lo tenía ===
                // Se reabre SOLO si nosotros lo cerramos: mismos parámetros
                // canónicos de la fase de fondo (diferido, alfa, LinearClamp
                // y la matriz de vista del FONDO — el mismo espacio en el que
                // el juego dibuja su paisaje).
                if (loteDelFondoAbierto)
                {
                    try
                    {
                        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                            null, Main.BackgroundViewMatrix.TransformationMatrix);
                    }
                    catch { }
                }
            }
        }

        /// <summary>¿La escena activa o la saliente tienen capas de este tipo?</summary>
        private static bool HayCapas(bool brillantes)
        {
            return TieneTipo(_activa, brillantes) || TieneTipo(_saliente, brillantes);
        }

        private static bool TieneTipo(EscenaDeCielo escena, bool brillantes)
        {
            if (escena == null) return false;
            for (int i = 0; i < escena.Capas.Count; i++)
                if (escena.Capas[i].Brillante == brillantes && escena.Capas[i].Alpha > 0f)
                    return true;
            return false;
        }

        /// <summary>
        /// Dibuja TODAS las capas de una escena (ya ordenadas lejos → cerca)
        /// con el fundido dado. Coordenadas de pantalla puras (Matrix.Identity
        /// — el fondo del juego NO comparte el zoom del mundo): la capa se
        /// escala a mano a su fracción de pantalla y su posición horizontal
        /// sigue a la cámara al ritmo de su parallax.
        /// </summary>
        private static void DibujarEscena(EscenaDeCielo escena, float fundido,
            float anchoPantalla, float altoPantalla, bool brillantes)
        {
            if (escena == null || fundido <= 0f) return;

            SpriteBatch batch = Main.spriteBatch;
            float segundos = Main.GameUpdateCount / 60f;   // el reloj determinista de la casa

            for (int i = 0; i < escena.Capas.Count; i++)
            {
                Capa capa = escena.Capas[i];
                if (capa.Brillante != brillantes || capa.Alpha <= 0f) continue;

                try
                {
                    Texture2D tex = TexturaDe(capa.RutaTextura);
                    if (tex == null) continue;

                    // === LA ESCALA: la capa ocupa Escala×la altura de pantalla ===
                    float altoPx = altoPantalla * capa.Escala;
                    float anchoPx = altoPx * (tex.Width / (float)tex.Height);
                    if (altoPx < 8f || anchoPx < 16f) continue;   // degenerada: fuera

                    // === EL PARALLAX: la deriva + el viaje de la cámara ===
                    // La capa "viaja" más despacio que el mundo: su desfase
                    // horizontal crece con la posición de cámara al ritmo del
                    // parallax, más su propia deriva (px/s).
                    float desfase = Main.screenPosition.X * capa.Parallax + capa.ScrollX * segundos;
                    float x0 = -Mod(desfase, anchoPx);             // (-anchoPx, 0]

                    // === EL ANCLAJE VERTICAL ===
                    // El pie de la capa vive en el borde inferior; la LEJANÍA
                    // la alza (las capas del horizonte cuelgan más arriba) y
                    // OffsetY la afina a mano.
                    float prof = MathHelper.Clamp(capa.Profundidad, 0f, 1f);
                    float y = altoPantalla * (1f - capa.Escala)
                            - capa.OffsetY
                            - altoPantalla * 0.20f * (1f - prof);

                    // === EL COLOR: tinte × alfa × fundido de la escena ===
                    float alfaFinal = MathHelper.Clamp(capa.Alpha * fundido, 0f, 1f);
                    Color color = new Color(capa.Tinte.R, capa.Tinte.G, capa.Tinte.B,
                        (byte)(255f * alfaFinal));

                    // === EL TILING: repetición manual con módulo (robusta —
                    // sin depender de coordenadas UV gigantes) ===
                    Vector2 escala = new Vector2(anchoPx / tex.Width, altoPx / tex.Height);
                    int repeticiones = (int)MathF.Ceiling((anchoPantalla - x0) / anchoPx) + 1;
                    if (repeticiones > 8) repeticiones = 8;       // techo de seguridad

                    for (int r = 0; r < repeticiones; r++)
                        batch.Draw(tex, new Vector2(x0 + r * anchoPx, y), null, color,
                            0f, Vector2.Zero, escala, SpriteEffects.None, 0f);
                }
                catch
                {
                    // Una capa enferma (textura descargada en caliente) se
                    // salta este frame — el resto del paisaje sigue vivo.
                }
            }
        }

        /// <summary>El módulo siempre positivo: resultado en [0, b).</summary>
        private static float Mod(float a, float b)
            => a - b * MathF.Floor(a / b);

        /// <summary>
        /// La textura de una capa, cacheada como Asset (resolución diferida,
        /// la convención de VFXCore). Ruta RELATIVA AL MOD — aquí se le
        /// pone el nombre del mod delante.
        /// </summary>
        private static Texture2D TexturaDe(string rutaModRelativa)
        {
            if (string.IsNullOrEmpty(rutaModRelativa)) return null;
            // v6.49 — EL FALLO TAMBIÉN SE CACHEA (hallazgo AUD-C): una ruta
            // muerta reintentaba ModContent.Request + EXCEPCIÓN cada frame
            // por capa rota (coste de excepción + GC por frame en pleno
            // render). El diccionario admite null: la primera vez se
            // aprende, las demás se saltan gratis.
            if (_texturas.TryGetValue(rutaModRelativa, out Asset<Texture2D> asset))
                return asset?.Value;
            try
            {
                asset = ModContent.Request<Texture2D>(
                    "AethonMod/" + rutaModRelativa, AssetRequestMode.AsyncLoad);
                _texturas[rutaModRelativa] = asset;
            }
            catch
            {
                _texturas[rutaModRelativa] = null; // la ruta no existe: se salta en silencio, PARA SIEMPRE
                return null;
            }
            return asset.Value;
        }

        /// <summary>Un fallo de render apaga la librería hasta la próxima activación.</summary>
        internal static void MarcarFalloDeRender() => _falloDeRender = true;

        // ==================================================================
        //  LOS ESTILOS DEL SAGRARIO (carga diferida — el bioma los pide)
        // ==================================================================

        private static SanctumBackgroundStyle _estiloSanctum;
        private static SanctumUndergroundBackgroundStyle _estiloSanctuSubsuelo;

        /// <summary>El estilo de fondo de SUPERFICIE del Sagrario Hueco.</summary>
        public static SanctumBackgroundStyle EstiloSanctum
            => _estiloSanctum ??= ModContent.GetInstance<SanctumBackgroundStyle>();

        /// <summary>El estilo de fondo de SUBSUELO del Sagrario Hueco.</summary>
        public static SanctumUndergroundBackgroundStyle EstiloSanctuSubsuelo
            => _estiloSanctuSubsuelo ??= ModContent.GetInstance<SanctumUndergroundBackgroundStyle>();
    }

    /// <summary>
    /// EL MOTOR DEL CIELO — el ModSystem que enchufa la librería al juego:
    /// subscribe el hook del dibujado del fondo (capas + tinte del cielo),
    /// aplica el tinte de la luz ambiental y el brillo, y limpia el estado
    /// al descargar el mundo o el mod.
    /// </summary>
    public class CieloSistema : ModSystem
    {
        public override void Load()
        {
            // El hook del fondo: MonoMod sobre el método que dibuja TODO el
            // paisaje del juego — nuestras capas van después del orig() y
            // antes de que el bucle siga con los tiles.
            Terraria.On_Main.DrawBG += DibujarFondoDelCielo;
        }

        public override void Unload()
        {
            try
            {
                Terraria.On_Main.DrawBG -= DibujarFondoDelCielo;
            }
            catch
            {
                // Programación defensiva: el detach del hook jamás puede
                // impedir que la desactivación del mod continúe (la casa).
            }
            CieloLib.Reiniciar();
        }

        /// <summary>Al dejar el mundo el cielo vuelve a su dueño.</summary>
        public override void OnWorldUnload()
        {
            CieloLib.DesactivarTodas();
        }

        /// <summary>
        /// EL TINTE DE LA LUZ DE FONDO: el color ambiental que el motor de
        /// iluminación usa detrás de todo se funde hacia el tinte de la
        /// escena (con la fuerza del fundido).
        /// </summary>
        public override void ModifySunLightColor(ref Color tileColor, ref Color backgroundColor)
        {
            if (CieloLib.TinteDeFondoEfectivo(out Color tinte, out float fuerza))
                backgroundColor = Color.Lerp(backgroundColor, tinte, fuerza);
        }

        /// <summary>EL BRILLO: la iluminación del mundo respira con la escena.</summary>
        public override void ModifyLightingBrightness(ref float scale)
        {
            float brillo = CieloLib.BrilloEfectivo;
            if (brillo != 1f && brillo > 0f)
                scale *= brillo;
        }

        // ================================================================
        //  EL HOOK DEL FONDO
        // ================================================================

        /// <summary>
        /// El corazón del render: primero tiñe EL CIELO (antes de que el
        /// juego dibuje su gradiente — con restauración garantizada en el
        /// finally para no envenenar el color persistente del día), luego
        /// deja que el juego pinte su paisaje, y después dibuja las capas
        /// libres de la escena activa.
        /// </summary>
        private void DibujarFondoDelCielo(Terraria.On_Main.orig_DrawBG orig, Terraria.Main self)
        {
            try
            {
                // 1) EL TINTE DEL CIELO — antes del orig (el gradiente ya
                //    dibujado no se puede teñir: hay que llegar PRIMERO).
                //    El color del cielo del juego es estado persistente del
                //    ciclo del día: se presta, se tiñe y SE DEVUELVE — siempre.
                Color cieloOriginal = Main.ColorOfTheSkies;
                if (CieloLib.TinteDeCieloEfectivo(out Color tinte, out float fuerza))
                    Main.ColorOfTheSkies = Color.Lerp(cieloOriginal, tinte, fuerza);

                try { orig(self); }
                finally { Main.ColorOfTheSkies = cieloOriginal; }

                // 2) LAS CAPAS LIBRES — sobre el paisaje, bajo los tiles.
                CieloLib.DibujarCapas();
            }
            catch
            {
                // El cielo jamás puede romper el render del juego: si algo
                // falla, la librería se apaga hasta la próxima activación.
                CieloLib.MarcarFalloDeRender();
            }
        }
    }
}
