using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using ReLogic.Graphics;

namespace AethonMod.Content.UI
{
    /// <summary>
    /// LibroDelEternoUI — v6.141 — EL LIBRO DEL ETERNO (la letra: «por
    /// cierto no veo el pergamino en ninguna parte, al tocar P no hace
    /// nada, por lo tanto hagamos algo, crea un pequeño icono el cual
    /// sera el mismo grimorio que estara ubicado justo al lado del
    /// icono de logro en el inventario cuando este se habre, ahi estara
    /// el pergamino con toda la informacion, de hecho que no sea un
    /// pergamino, que sea un libro creado por la sforja en fullhd, si
    /// no sabes como mira libros abiertos en internet para tener una
    /// idea, ahora el ojo que mira al jugador es el mismo ojo de los
    /// minions usa ese ya tenemos todo lo que se necesita para hacerlo
    /// funcionar»).
    ///
    /// EL DISEÑO:
    ///  · EL ICONO: un botón GRIMORIO (la textura del arma del libro)
    ///    dibujado JUSTO AL LADO DEL BOTÓN DE LOGROS del inventario
    ///    (la geometría vanilla verificada en el decompile: el ícono de
    ///    logros vive en 20 + 560·escala + num — el nuestro a su
    ///    IZQUIERDA). Un clic: EL LIBRO SE ABRE.
    ///  · EL LIBRO: LA PORTADA DE LA SUPERFORJA (el estilo Sforza de
    ///    la casa): cuero viejo, el lomo cosido, el broche dorado — y
    ///    al abrir, LAS DOS PÁGINAS (la izquierda con la info del
    ///    mundo, la derecha con la del grimorio), cosidas al centro,
    ///    con su margen y sus capítulos. TODO PROCEDURAL (el mismo
    ///    motor de la casa: MagicPixel + SoftGlow).
    ///  · EL OJO: EL MISMO OJO DE LOS MINIONS (OjoEterno_Abierto — la
    ///    letra) en la portada del libro derecho: LA PUPILA SIGUE AL
    ///    CURSOR en todo momento (el mecanismo vivo de los ojos del
    ///    escuadrón: el iris desplazado hacia el ratón) y parpadea solo.
    ///  · EL CONTENIDO: las 8 secciones de la v6.140 (altar · mundo
    ///    interior · oleadas · grimorio · despertar · ataques · forma
    ///    eterna · auras) — TODO en hjson, los 3 idiomas.
    ///  · LA TECLA P sigue viva (atajo extra — la letra solo pedía el
    ///    icono, la tecla no molesta).
    /// </summary>
    public class LibroDelEternoUI : ModSystem
    {
        private static UserInterface _iface;
        private static LibroEstado _estado;
        internal static bool Abierto => _estado != null && _estado.Visible;

        // ============================================================
        //  v6.163 — LAS PESTAÑAS DEL LIBRO (la letra: «puedes usar la
        //  misma ventana que usas para el libro de informacion en el
        //  altar, para que todo encaje mejor con el tema de que todo
        //  se hace en el mismo grimorio, y lo mismo para el pilar en
        //  el mundo interior»): EL GRIMORIO ES EL CENTRO — el altar y
        //  el pilar YA NO son paneles flotantes: son PESTAÑAS del
        //  mismo libro. El índice de siempre (Mecánica/Lore) vive en
        //  la pestaña 0; la pestaña ALTAR trae la ofrenda + el trío;
        //  la pestaña PILAR trae el mundo interior. AbrirEn() las
        //  cambia (los tiles llaman).
        // ============================================================

        /// <summary>La pestaña viva: 0 = Índice · 1 = Altar · 2 = Pilar.</summary>
        internal static int Pestana;

        /// <summary>ABRE EL LIBRO en una pestaña (0 Índice · 1 Altar · 2 Pilar).</summary>
        public static void AbrirEn(int pestana)
        {
            // v6.165 — LA CURA DE LAS VENTANAS PISADAS (la letra: «al
            // tocar el altar solo sale el libro de informacion, no
            // sale la ventana del altar»): ESTA ruta NO pasa por
            // Abrir() — Abrir() fuerza Pestana = 0 (el icono y la P
            // abren SOLO el índice) y PISABA la pestaña del
            // altar/pilar que el tile acababa de poner. El libro se
            // abre AQUÍ, en la pestaña de su disparador.
            Pestana = pestana;
            if (_estado == null) _estado = new LibroEstado();
            _estado.Visible = true;
            SelectorFlotante.Cerrar();      // v6.174: nada de la sesión anterior flota sobre el libro
            _estado.ReiniciarScroll();
            _estado.ReiniciarFlancos();    // v6.174: el clic (o la tecla) que abrió el libro no dispara nada
            _iface.SetState(_estado);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuOpen);
        }

        // EL ICONO: la geometría vanilla del botón de logros (el
        // decompile: 20 + 560·inventoryScale + num, 20 + num2)
        internal static Rectangle GeoIcono;

        public override void Load()
        {
            _iface = new UserInterface();
        }

        public override void Unload()
        {
            // v6.174 — el selector flotante no sobrevive a la descarga
            // (ni su lista de ítems de un jugador que ya no existe).
            SelectorFlotante.Descargar();
            _iface = null;
            _estado = null;
        }

        public static void Abrir()
        {
            // v6.164 — LAS VENTANAS SEPARADAS: el icono y la tecla P
            // abren SOLO EL ÍNDICE (la letra: «cuando toco el libro de
            // informacion solo debe salir libro de informacion»). El
            // altar y el pilar se abren con sus tiles (AbrirEn).
            Pestana = 0;
            if (_estado == null) _estado = new LibroEstado();
            _estado.Visible = true;
            SelectorFlotante.Cerrar();      // v6.174: nada de la sesión anterior flota sobre el libro
            _estado.ReiniciarScroll();
            _estado.ReiniciarFlancos();    // v6.174: el clic (o la tecla) que abrió el libro no dispara nada
            _iface.SetState(_estado);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuOpen);
        }

        public static void Cerrar()
        {
            // v6.174 — EL SELECTOR MUERE CON EL LIBRO (antes quedaba vivo
            // huérfano: su ventana flotaba sobre el mundo ya cerrado).
            SelectorFlotante.Cerrar();
            // v6.164 — EL CIERRE SINCRONIZADO: si el libro muere (ESC, X)
            // mientras muestra el ALTAR o el PILAR, los motores sueltan
            // SU estado también (v6.174: la ofrenda PRESTADA se olvida —
            // nunca salió del inventario; la física se queda en el altar).
            try
            {
                if (Pestana == 1 && AltarOfrendasUI.Abierta) AltarOfrendasUI.ForzarCierre();
                else if (Pestana == 2 && PrismaInteriorUI.Abierta) PrismaInteriorUI.ForzarCierre();
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("LibroDelEternoUI", exRegistro); }
            if (_estado != null) _estado.Visible = false;
            _iface.SetState(null);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuClose);
        }

        public override void UpdateUI(GameTime gameTime)
        {
            if (_estado != null && _estado.Visible)
                _iface.Update(gameTime);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            // EL ICONO junto al de logros: DESPUÉS del inventario (la
            // capa vanilla dibuja el botón de logros dentro de la suya)
            int idx = layers.FindIndex(l => l.Name.Equals("Vanilla: Inventory"));
            if (idx < 0) idx = layers.Count - 1;

            // EL ICONO del libro (antes del inventario: dibuja encima)
            layers.Insert(idx, new LegacyGameInterfaceLayer("AethonMod: IconoLibroEterno",
                () =>
                {
                    DibujarIcono(Main.spriteBatch);
                    return true;
                }, InterfaceScaleType.UI));

            // ============================================================
            //  v6.153 — EL CURSOR GANA DE VERDAD (la letra: «el libro de
            //  informacion sigue estando por encima del cursor y el
            //  cursor siempre queda oculto»): LA AUTOPSIA — insertar la
            //  capa del libro antes de 'Vanilla: Mouse Over' NO basta:
            //  el cursor de Terraria NO vive en esa capa cuando el
            //  inventario está abierto (el libro FUERZA playerInventory
            //  = false en su Draw, pero vanilla ya decidió SU pase de
            //  cursor con el inventario abierto de ANTES — el cursor
            //  queda pintado y el libro encima lo TAPA). LA CURA
            //  DOBLE:
            //   (a) la capa del libro vive ANTES de 'Vanilla: Mouse
            //       Text' (la 33 — TODAS las capas de cursor de vanilla
            //       van después) — ya no puede tapar NADA de vanilla;
            //   (b) el libro dibuja SU PROPIO CURSOR al final de su
            //       Draw (el cursor de vanilla clonado en la posición
            //       real del ratón) — el puntero es LO ÚLTIMO que se
            //       ve, SIEMPRE.
            // ============================================================
            int idxCursor = layers.FindIndex(l => l.Name.Equals("Vanilla: Mouse Text"));
            if (idxCursor < 0) idxCursor = layers.Count - 1;
            layers.Insert(idxCursor, new LegacyGameInterfaceLayer("AethonMod: LibroDelEterno",
                () =>
                {
                    if (_estado != null && _estado.Visible)
                        _iface.Draw(Main.spriteBatch, new GameTime());
                    return true;
                }, InterfaceScaleType.UI));
        }

        /// <summary>EL ICONO DEL GRIMORIO junto al botón de logros (la
        /// letra: «justo al lado del icono de logro en el inventario
        /// cuando este se habre») — SOLO con el inventario ABIERTO.</summary>
        private static void DibujarIcono(SpriteBatch sb)
        {
            try
            {
                if (Main.playerInventory != true || Main.player[Main.myPlayer] == null) return;
                if (_estado != null && _estado.Visible) return;   // el libro abierto: no icono

                // LA GEOMETRÍA VANILLA (verificada en el decompile): el
                // botón de logros vive en (20 + 560·escala + num, 20 +
                // num2) — el num/num2 es el corrimiento responsive del
                // inventario (0 en pantallas anchas). Nuestro icono: a su
                // IZQUIERDA (48 px de margen).
                float esc = Main.inventoryScale;
                int num = (int)(Main.screenWidth / 2f - 560f * esc / 2f - 20f); // el origen X responsive
                if (num < 0) num = 0;
                // v6.144 — JUSTO AL LADO DE LOS LOGROS (la letra:
                // «debe estar justo al lado de donde van los logros»):
                // el botón de logros ocupa (20 + 560·esc + num, 20) —
                // nuestro icono vive PEGADO a su DERECHA (+48 px).
                int x = (int)(20f + 560f * esc) + num + 48;    // a la DERECHA del de logros
                int y = 20;
                GeoIcono = new Rectangle(x, y, 40, 40);
                if (GeoIcono.X < 8) GeoIcono.X = 8;

                // LA TEXTURA DEL GRIMORIO (la del arma — «el mismo grimorio»)
                Texture2D tex = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Weapons/GrimorioHambrientoNervioso").Value;
                bool hover = GeoIcono.Contains(Main.mouseX, Main.mouseY);

                // el marco del botón (el estilo de vanilla: el recuadro)
                Texture2D pixel = TextureAssets.MagicPixel.Value;
                sb.Draw(pixel, new Rectangle(GeoIcono.X - 4, GeoIcono.Y - 4, 48, 48),
                    new Color(63, 65, 151) * (hover ? 0.85f : 0.55f));
                sb.Draw(pixel, new Rectangle(GeoIcono.X - 4, GeoIcono.Y - 4, 48, 2),
                    new Color(130, 132, 216) * 0.8f);
                sb.Draw(pixel, new Rectangle(GeoIcono.X - 4, GeoIcono.Y + 42, 48, 2),
                    new Color(130, 132, 216) * 0.8f);
                sb.Draw(pixel, new Rectangle(GeoIcono.X - 4, GeoIcono.Y - 4, 2, 48),
                    new Color(130, 132, 216) * 0.8f);
                sb.Draw(pixel, new Rectangle(GeoIcono.X + 42, GeoIcono.Y - 4, 2, 48),
                    new Color(130, 132, 216) * 0.8f);

                // EL LIBRO (escala al slot)
                var origen = tex.Size() * 0.5f;
                Vector2 centroIcono = new Vector2(GeoIcono.X + 20, GeoIcono.Y + 22);
                float rotIcono = hover ? -0.12f : -0.05f;
                sb.Draw(tex, centroIcono, null,
                    hover ? Color.White : new Color(210, 200, 235),
                    rotIcono, origen, 0.62f, SpriteEffects.None, 0f);

                // v6.146 — LA ANIMACIÓN DEL GRIMORIO DEL ETERNO (la
                // letra: «el icono de Información no esta animado como
                // el grimorio del eterno en el inventario, debes animar
                // ese icono igual que al grimorio del eterno»): LAS
                // MISMAS CAPAS del GrimorioHambriento en el inventario
                // — EL OJO QUE PARPADEA: el ciclo de la casa (abierto
                // la mayoría del tiempo, párpado MEDIO un instante,
                // CERRADO un flash) + EL IRIS DORADO que respira y
                // SIGUE al cursor mientras está abierto.
                try
                {
                    float tiempo = Main.GlobalTimeWrappedHourly;
                    // EL CICLO DEL PARPADEO (la firma del eterno: 6 s
                    // abierto → 8 ticks medio → 4 cerrado → 6 medio)
                    float fase = tiempo % 7.5f;
                    string capa = null;
                    if (fase > 6.9f && fase <= 7.1f) capa = "AethonMod/Content/Weapons/GrimorioHambriento_Medio";
                    else if (fase > 7.1f && fase <= 7.3f) capa = "AethonMod/Content/Weapons/GrimorioHambriento_Cerrado";
                    else if (fase > 7.3f && fase <= 7.5f) capa = "AethonMod/Content/Weapons/GrimorioHambriento_Medio";
                    if (capa != null)
                    {
                        var parpado = ModContent.Request<Texture2D>(capa).Value;
                        sb.Draw(parpado, centroIcono, null,
                            hover ? Color.White : new Color(210, 200, 235),
                            rotIcono, parpado.Size() * 0.5f, 0.62f, SpriteEffects.None, 0f);
                    }
                    else
                    {
                        // EL IRIS VIVO — mira al cursor y RESPIRA (la
                        // escala late con el tiempo).
                        // v6.147 — EL IRIS PROPORCIONADO (la letra del
                        // usuario: «el iris esta desproporcionadamente
                        // grande, se sale por completo del ojo»): la
                        // v6.146 lo dibujaba a LA MISMA ESCALA que el
                        // libro (0.62 → 20×20 px, casi todo el icono de
                        // 22×30). El grimorio REAL usa IRIS_ESC 0.2773
                        // SOBRE la escala del libro (GrimorioHambriento:
                        // internal const IRIS_ESC = 0.2773f) — ESA es la
                        // proporción del ojo. Y nace en EL OJO del libro
                        // (OJO 19.09, 22.45 sobre 36×49), no en el
                        // centro del icono.
                        var iris = ModContent.Request<Texture2D>(
                            "AethonMod/Content/Weapons/GrimorioHambriento_Iris").Value;
                        Vector2 alOjo = (new Vector2(19.09f, 22.45f) -
                            new Vector2(18f, 24.5f)) * 0.62f;   // el ojo, relativo al centro
                        Vector2 posOjo = centroIcono + alOjo.RotatedBy(rotIcono);
                        Vector2 alCursor = new Vector2(Main.mouseX, Main.mouseY) - posOjo;
                        Vector2 mirada = alCursor.LengthSquared() > 4f
                            ? Vector2.Normalize(alCursor) * new Vector2(2.4f, 1.9f) * 0.62f
                            : Vector2.Zero;
                        float respira = 0.62f * 0.2773f *
                            (1f + 0.10f * (float)Math.Sin(tiempo * 2.2f));
                        sb.Draw(iris, posOjo + mirada, null, Color.White,
                            rotIcono, iris.Size() * 0.5f, respira,
                            SpriteEffects.None, 0f);
                    }
                }
                catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("LibroDelEternoUI", exRegistro); }

                // LA PULSACIÓN DEL OJO (la vida del icono: el destello
                // violeta que respira)
                Texture2D glow = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                float pulso = 0.16f + 0.10f *
                    (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2.2f);
                sb.Draw(glow, new Vector2(GeoIcono.X + 20, GeoIcono.Y + 22), null,
                    new Color(150, 84, 235) * pulso, 0f, glow.Size() * 0.5f,
                    0.9f, SpriteEffects.None, 0f);

                // EL HOVER: el nombre de la casa
                if (hover)
                {
                    string nombre = Language.GetTextValue("Mods.AethonMod.LibroEterno.NombreIcono");
                    Utils.DrawBorderString(sb, nombre,
                        new Vector2(GeoIcono.X + 20, GeoIcono.Y + 54),
                        new Color(200, 170, 255), 0.85f, 0.5f, 0f);

                    // EL CLIC ABRE EL LIBRO
                    if (Main.mouseLeft && Main.mouseLeftRelease)
                    {
                        Main.mouseLeftRelease = false;
                        Abrir();
                    }
                    Main.LocalPlayer.mouseInterface = true;
                }
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("LibroDelEternoUI", exRegistro); }
        }
    }

    /// <summary>El estado del libro abierto (las dos páginas vivas).</summary>
    internal class LibroEstado : UIState
    {
        public bool Visible;
        private float _scrollIzq, _scrollDer;     // cada página desplaza sola
        private float _maxIzq, _maxDer;
        private int _parpadeo, _parpadeoEn;
        // v6.144 — LAS GEOMETRÍAS DE LAS PÁGINAS (para el scroll
        // independiente: la rueda decide por la página bajo el cursor)
        private Rectangle GeoPagIzq, GeoPagDer;
        // v6.164 — EL FLANCO DEL CLIC de la casa (la cura de los
        // botones muertos: el patrón del árbol/altar/prisma viejos).
        private bool _ultimoClicIzq;

        /// <summary>v6.173 — EL FLANCO DEL CLIC DERECHO (la letra:
        /// «el click derecho sobre un slot debe desequipar el arma»):
        /// un clic derecho puro JAMÁS entraba por el flanco izquierdo
        /// — la extracción vivía muerta dentro de un if mouseRight
        /// que solo corría dentro del clic IZQUIERDO. Ahora tiene SU
        /// propio flanco.</summary>
        private bool _ultimoClicDer;

        /// <summary>v6.163 — al cambiar de pestaña el scroll vuelve
        /// arriba (cada pestaña es un mundo nuevo).</summary>
        public void ReiniciarScroll() { _scrollIzq = 0f; _scrollDer = 0f; }

        /// <summary>v6.174 — LOS FLANCOS SE SIEMBRAN AL ABRIR: el clic (o
        /// la tecla) que abrió el libro sigue apretado en ese mismo
        /// frame — sin esto, el primer dibujo veía un clic «nuevo».
        /// Se llama cuando el libro se abre y cuando cambia de pestaña.</summary>
        public void ReiniciarFlancos()
        {
            _ultimoClicIzq = Main.mouseLeft;
            _ultimoClicDer = Main.mouseRight;
        }

        // v6.149 — LA LEGIBILIDAD DE VERDAD (la letra: «todos los textos
        // estan desentrados y el color de los textos los hace
        // ilegibles»). DOS CURAS:
        //   · LOS COLORES: el marrón-tinta apagado (46,30,22) sobre papel
        //     era CENIZA — apenas distinguible de la sombra del papel.
        //     La tinta nueva es la CLÁSICA de manuscrito vivo: un
        //     sepia-oscuro con CONTRASTE REAL contra el papel (el truco
        //     del WriteBorderString: el texto lleva SU PROPIO borde
        //     negro y el cuerpo sube de luminosidad). Títulos: violeta
        //     SATURADO; capítulos: ámbar quemado con más cuerpo.
        //   · EL CENTRADO (abajo, en DibujarColumna): títulos de sección
        //     CENTRADOS en la página (no pegados a la izquierda) y el
        //     cuerpo JUSTIFICADO con centrado óptico por línea.
        // v6.151 — LA TINTA DE LA LETRA («nunca arreglaste el color de
        // los textos, ese color no se ve y es horrible, que sean letras
        // con bordes blanco centro negro»): el CUERPO pasa por
        // DrawBorderString con centro NEGRO PURO (el contraste máximo
        // contra el papel — la línea que SIEMPRE se lee) y el borde que
        // DibujaCuerpo pinta BLANCO (el halo que la separa de las
        // sombras del papel). LOS TÍTULOS QUEDAN COMO ESTÁN (la letra:
        // «la letra de los titulos y esas cosas dejalas como estan»).
        // v6.166 — LA CURA DE LA LETRA (la letra: «todas las letras se
        // ven o demasiado oscuras o demasiado claras y sus colores son
        // raros, tal vez si oscureces un poco la textura usada para
        // esas ventanas»): el contorno BLANCO PURO alrededor de tinta
        // casi-negra lavaba la letra (halo blanco sobre papel claro =
        // colores raros). EL PAPEL OSCURECE UN PUNTO (el amarillo
        // claro 232,218,186 era demasiado brillante), EL CONTORNO
        // PASA A SEPIA OSCURO (el tono del papel en sombra — se funde
        // con la página y separa la tinta) y LA TINTA RESPIRA (10,8,12
        // → 28,20,16: negro con matiz cálido, el clásico de
        // manuscrito). Los títulos suben de luminosidad para leerse
        // SIEMPRE.
        private static readonly Color Tinta = new(28, 20, 16);
        private static readonly Color TintaTitulo = new(148, 72, 178);
        private static readonly Color TintaCap = new(168, 108, 44);
        private static readonly Color Papel = new(216, 200, 166);
        private static readonly Color PapelSombra = new(190, 172, 134);
        private static readonly Color Cuero = new(84, 54, 34);
        private static readonly Color CueroClaro = new(112, 76, 46);

        private static Texture2D _pixel;
        private static Texture2D _glow;

        public override void OnInitialize()
        {
            _pixel = TextureAssets.MagicPixel.Value;
            _glow = ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
            _parpadeoEn = 240 + Main.rand.Next(300);
        }

        private static string T(string c) => Language.GetTextValue("Mods.AethonMod.Pergamino." + c);

        public override void Update(GameTime gameTime)
        {
            // ============================================================
            //  v6.146 — EL CERROJO OFICIAL DE LA RUEDA (la letra: «esto
            //  tiene algo que cuando uso la rueda del mouse para hacer
            //  scroll tambien se hace scroll en el inventario»): el
            //  patrón del PROPIO UIScrollbar de tML — cuando el cursor
            //  vive sobre LAS PÁGINAS del libro, LockVanillaMouseScroll
            //  clava la lista que vanilla consulta ANTES de mover SU
            //  inventario (Main.cs: 'MouseInModdedUI.Count == 0'): la
            //  rueda es DEL LIBRO y NADIE MÁS. (El cerrojo viejo de
            //  'ScrollWheelDelta = 0' no bastaba: vanilla lee su
            //  ScrollWheelValue directo — SOLO esta lista lo frena.)
            // ============================================================
            // v6.174 — LA RUEDA DE LA VITRINA DEL PILAR Y DEL SELECTOR: los
            // dos leen SU rueda en el DIBUJO (que corre después de este
            // Update) — si aquí se la comíamos, nunca les llegaba. Sobre
            // ellos el libro no se mueve, pero el cerrojo de vanilla sigue.
            bool ruedaDelSelector = SelectorFlotante.Abierta &&
                                     SelectorFlotante.UltimoRect.Contains(Main.mouseX, Main.mouseY);
            bool ruedaDelPilar = LibroDelEternoUI.Pestana == 2 && PilarDelLibro.CursorEnVitrina();
            bool cursorEnLibro = GeoPagIzq.Contains(Main.mouseX, Main.mouseY) ||
                                 GeoPagDer.Contains(Main.mouseX, Main.mouseY);
            if (cursorEnLibro)
                Terraria.GameInput.PlayerInput.LockVanillaMouseScroll("AethonMod/LibroEterno");

            // v6.144 — EL SCROLL INDEPENDIENTE: la rueda SOLO mueve LA
            // PÁGINA bajo el cursor (fuera de ambas: nada).
            int delta = Terraria.GameInput.PlayerInput.ScrollWheelDelta;
            if (delta != 0 && (ruedaDelSelector || ruedaDelPilar))
                delta = 0;                      // v6.174: la rueda es de la vitrina — el libro no se mueve
            if (delta != 0)
            {
                Rectangle mitIzq = new Rectangle(0, 0, Main.screenWidth / 2, Main.screenHeight);
                if (GeoPagIzq.Contains(Main.mouseX, Main.mouseY))
                    _scrollIzq = MathHelper.Clamp(_scrollIzq - delta * 0.45f, 0f, Math.Max(0f, _maxIzq));
                else if (GeoPagDer.Contains(Main.mouseX, Main.mouseY))
                    _scrollDer = MathHelper.Clamp(_scrollDer - delta * 0.45f, 0f, Math.Max(0f, _maxDer));
                Terraria.GameInput.PlayerInput.ScrollWheelDelta = 0;   // la rueda es del libro
            }

            // v6.168 — ESC CIERRA PRIMERO AL SELECTOR FLOTANTE (la
            // ventanita de elegir es lo más joven de la pila).
            // v6.174 — LA PULSACIÓN SE GASTA en la ventana que cerró:
            // mientras la tecla siga apretada no baja al libro (un ESC
            // no se come las dos ventanas); al soltarla vuelve a valer.
            bool escVivo = Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape);
            if (escVivo)
            {
                if (SelectorFlotante.Abierta) SelectorFlotante.CerrarPorEsc();
                else if (!SelectorFlotante.EscConsumido) LibroDelEternoUI.Cerrar();
            }
            SelectorFlotante.FilarEsc(escVivo);

            // (v6.164 — el motor de clics SE MUDÓ al dibujo de las
            //  páginas: el flanco de la casa vive donde viven las
            //  geometrías. El Update ya no dispara clics.)

            // EL PARPADEO DEL OJO
            if (_parpadeo > 0) _parpadeo--;
            else if (--_parpadeoEn <= 0)
            {
                _parpadeo = 8;
                _parpadeoEn = 280 + Main.rand.Next(360);
            }
        }

        public override void Draw(SpriteBatch sb)
        {
            // v6.174 — EL ESTADO DEL CLIC SE LEE UNA VEZ, ANTES DEL try, y
            // los flancos se completan en el finally. Antes el flanco se
            // completaba al final del try: si una página lanzaba (y el
            // catch se lo tragaba), el clic quedaba «nuevo» CADA frame
            // mientras el botón estaba apretado — el mismo botón se
            // disparaba en cadena.
            bool clicAhora = Main.mouseLeft;
            bool clicDerAhora = Main.mouseRight;
            try
            {
                Main.LocalPlayer.mouseInterface = true;
                Main.playerInventory = false;

                // ============ EL LIBRO ABIERTO (el estilo Sforza) ============
                int w = Math.Min(1060, Main.screenWidth - 40);
                int h = Math.Min(720, Main.screenHeight - 40);
                int x = (Main.screenWidth - w) / 2;
                int y = (Main.screenHeight - h) / 2;

                // LA TAPA: el cuero viejo (más grande que las páginas)
                sb.Draw(_pixel, new Rectangle(x - 26, y - 26, w + 52, h + 52), Cuero);
                sb.Draw(_pixel, new Rectangle(x - 26, y - 26, w + 52, 6), CueroClaro);
                sb.Draw(_pixel, new Rectangle(x - 26, y + h + 20, w + 52, 6), CueroClaro);
                sb.Draw(_pixel, new Rectangle(x - 26, y - 26, 6, h + 52), CueroClaro);
                sb.Draw(_pixel, new Rectangle(x + w + 20, y - 26, 6, h + 52), CueroClaro);

                // LAS ESQUINAS DE METAL (las chapas del Sforza)
                for (int k = 0; k < 4; k++)
                {
                    int ex = k % 2 == 0 ? x - 26 : x + w + 8;
                    int ey = k < 2 ? y - 26 : y + h - 10;
                    sb.Draw(_pixel, new Rectangle(ex, ey, 18, 18), new Color(150, 108, 34));
                    sb.Draw(_pixel, new Rectangle(ex + 2, ey + 2, 14, 14), new Color(196, 152, 58));
                }

                // EL CENTRO: el lomo (la costura del libro)
                int cx = x + w / 2;
                sb.Draw(_pixel, new Rectangle(cx - 14, y - 10, 28, h + 20),
                    new Color(64, 42, 26));
                sb.Draw(_pixel, new Rectangle(cx - 2, y - 10, 4, h + 20),
                    new Color(40, 26, 16));
                // LAS PUNTADAS del lomo (la costura viva)
                for (int yy = y + 8; yy < y + h - 8; yy += 26)
                {
                    sb.Draw(_pixel, new Rectangle(cx - 10, yy, 20, 3), new Color(150, 108, 34));
                    sb.Draw(_pixel, new Rectangle(cx - 10, yy + 13, 20, 3), new Color(150, 108, 34));
                }

                // LAS DOS PÁGINAS (la izquierda y la derecha)
                int pagW = w / 2 - 20;
                Rectangle pagIzq = new Rectangle(x + 10, y + 8, pagW, h - 16);
                Rectangle pagDer = new Rectangle(cx + 14, y + 8, pagW, h - 16);
                GeoPagIzq = pagIzq; GeoPagDer = pagDer;   // v6.144 — para el scroll por página
                sb.Draw(_pixel, pagIzq, Papel);
                sb.Draw(_pixel, pagDer, Papel);
                // LA SOMBRA DEL LOMO sobre cada página (el hueco real)
                sb.Draw(_pixel, new Rectangle(pagIzq.Right - 22, pagIzq.Y, 22, pagIzq.Height),
                    PapelSombra * 0.45f);
                sb.Draw(_pixel, new Rectangle(pagDer.X, pagDer.Y, 22, pagDer.Height),
                    PapelSombra * 0.45f);
                // EL BORDE de las páginas (las hojas apiladas)
                for (int k = 0; k < 3; k++)
                {
                    sb.Draw(_pixel, new Rectangle(pagIzq.X - 3 - k, pagIzq.Y + 4 + k, 3, pagIzq.Height - 8),
                        PapelSombra);
                    sb.Draw(_pixel, new Rectangle(pagDer.Right + k, pagDer.Y + 4 + k, 3, pagDer.Height - 8),
                        PapelSombra);
                }

                // ============ EL TITULAR (la portada interior) ============
                // v6.147 — EL TITULAR YA NO PISA LAS PÁGINAS (la letra
                // del usuario: «el texto de la izquierda se solapa con
                // el texto del titulo de la izquierda»): la v6.145 lo
                // ponía a (x+22, y+26) — ENCIMA de la página izquierda,
                // pisando su rótulo y el arranque de su columna. Ahora
                // vive en LA FRANJA DE CUERO sobre las páginas (entre
                // la tapa y el papel, y−22): un rótulo de archivo
                // grabado en el cuero, con la tinta clara del lomo.
                var fuente = FontAssets.MouseText.Value;
                string tit = T("Titulo");
                float escTit = EscalaQueCabe(tit, 0.95f, w - 60f);
                Vector2 medT = fuente.MeasureString(tit) * escTit;
                Utils.DrawBorderString(sb, tit, new Vector2(x + 22f, y - 22f),
                    new Color(232, 214, 170), escTit);
                string sub = T("Subtitulo");
                float escSub = EscalaQueCabe(sub, 0.65f, w - 60f - medT.X);
                if (!string.IsNullOrEmpty(sub) && !sub.StartsWith("Mods."))
                    Utils.DrawBorderString(sb, sub, new Vector2(x + 26f + medT.X, y - 20f),
                        new Color(206, 184, 148), escSub);

                // ============================================================
                //  v6.164 — LOS TÍTULOS DE LAS PÁGINAS (la letra:
                //  «lo que dice, la izquierda: la pagina informativa -
                //  la derecha: la pagina de lore, eso debes quitarlo, y
                //  en la pagina izquierda solo debes escribir al inicio
                //  de la pagina y centrado Informacion, luego en la
                //  pagina derecha al inicio de todo escribir Lore»):
                //  los rótulos largos MURIERON — solo la palabra,
                //  CENTRADA, al inicio de cada página (y solo en la
                //  pestaña ÍNDICE: el altar y el pilar tienen sus
                //  propios títulos de sección). LAS PESTAÑAS TAMBIÉN
                //  MURIERON (la letra: «que hayan 3 pestañas siempre
                //  en la parte superior derecho esta mal»): cada
                //  ventana se abre SOLO con su disparador — el libro
                //  (icono/P) abre el índice, el altar abre el altar,
                //  el pilar abre el pilar. Nadie cambia de una a otra
                //  dentro del libro.
                // ============================================================
                if (LibroDelEternoUI.Pestana == 0)
                {
                    string titI = Language.GetTextValue("Mods.AethonMod.LibroEterno.TituloPaginaInfo");
                    string titD = Language.GetTextValue("Mods.AethonMod.LibroEterno.TituloPaginaLore");
                    if (!string.IsNullOrEmpty(titI) && !titI.StartsWith("Mods."))
                        Utils.DrawBorderString(sb, titI,
                            new Vector2(GeoPagIzq.X + GeoPagIzq.Width / 2f, GeoPagIzq.Y + 10f),
                            TintaTitulo, 0.92f, 0.5f, 0.4f);
                    if (!string.IsNullOrEmpty(titD) && !titD.StartsWith("Mods."))
                        Utils.DrawBorderString(sb, titD,
                            new Vector2(GeoPagDer.X + GeoPagDer.Width / 2f, GeoPagDer.Y + 10f),
                            TintaTitulo, 0.92f, 0.5f, 0.4f);
                }

                // EL MOTOR DE CLIC DE LA CASA (v6.164): el FLANCO de
                // siempre (mouseLeft && !_ultimoClicIzq — el patrón del
                // árbol y de los paneles viejos que FUNCIONABAN),
                // procesado AQUÍ en el dibujo — donde viven las geometrías.
                // v6.174 — LA AUTOPSIA DE LOS BOTONES MUERTOS (la letra:
                // «en el mundo interior, al usar el pilar los botones no
                // hacen nada» · «en el altar tampoco funcionan, funcionaron
                // la primera vez pero ahora no»): la v6.173 coló DOS
                // filtros encima del flanco — IgnoreMouseInterface (la
                // bandera TRANSITORIA de vanilla, que se enciende con el
                // inventario abierto y con mil cosas más) y un «frame de
                // gracia» medido con el reloj de vanilla, un reloj que
                // VUELVE A CERO al cargar el mundo o al entrar al mundo
                // interior (la marca de gracia se quedaba por delante para
                // siempre: ningún clic volvía a pasar). Los dos mueren.
                bool clicNuevo = clicAhora && !_ultimoClicIzq;
                bool clicDerNuevo = clicDerAhora && !_ultimoClicDer;

                // v6.174 — EL SELECTOR ES EL DUEÑO ÚNICO DEL CLIC:
                // mientras su ventana vive, las páginas de abajo NO oyen
                // (el fantasma que vaciaba las ranuras al elegir ya no
                // tiene dónde caer).
                bool selectorVivo = SelectorFlotante.Abierta;
                bool clicPagina = clicNuevo && !selectorVivo;
                bool clicDerPagina = clicDerNuevo && !selectorVivo;

                // el cuerpo arranca DEBAJO del rótulo de la página —
                // nunca debajo del titular (que ya vive en el cuero)
                float cy = GeoPagIzq.Y + 26f;

                // ============ EL CUERPO: DOS COLUMNAS CON RECORTE ============
                // v6.151 — EL MARGEN DEL DESAPARECER (la letra: «al hacer
                // scroll los textos desaparecen muy pegados al borde
                // superio, intenta dejar un pequeño margen arriba»): el
                // RECORTE de las columnas nace 30 px MÁS ABAJO de la
                // página (el aire del papel) — la línea que se va
                // desvaneció dentro de la página, no contra su borde.
                // v6.163 — EL MARGEN INFERIOR ENORME MURIÓ (la letra:
                // «el libro de informacion tiene un margen inferior muy
                // grande, demasiado»): la fórmula vieja era
                // h − cy − y − 60 — le RESTABA 'y' (una COORDENADA de
                // pantalla) a 'h' (un TAMAÑO): el recorte acababa 58 px
                // antes del borde del papel, y con la pantalla grande
                // el hueco se veía GIGANTE. LA CURA: el cuerpo mide
                // contra EL BORDE REAL de la página (y + h − margen) —
                // la columna muere a 20 px del borde, como arriba.
                int cuerpoY = (int)cy + 30;
                int cuerpoH = (y + h - 20) - cuerpoY - 12;
                if (cuerpoH < 120) cuerpoH = 120;
                Rectangle recIzq = new Rectangle(pagIzq.X + 6, cuerpoY, pagIzq.Width - 12, cuerpoH);
                Rectangle recDer = new Rectangle(pagDer.X + 6, cuerpoY, pagDer.Width - 12, cuerpoH);

                // LAS SECCIONES: IZQ = LAS MECÁNICAS COMPLETAS (v6.147 —
                // las 4 de la derecha se mudan aquí, la página es una y
                // scrollea) · DER = EL COMPENDIO DEL ETERNO (el lore)
                string[][][] columnas =
                {
                    new string[][]
                    {
                        new[] { "SecAltar", "Altar1", "Altar2", "Altar3", "Altar4", "Altar5", "Altar5b" },
                        new[] { "SecInterior", "Interior1", "Interior2", "Interior3", "Interior4" },
                        new[] { "SecOleadas", "Oleadas1", "Oleadas2", "Oleadas3", "Oleadas4" },
                        new[] { "SecGrimorio", "Grimorio1", "Grimorio2", "Grimorio3", "Grimorio4", "Grimorio5" },
                        new[] { "SecDespertar", "Despertar1", "Despertar2", "Despertar3", "Despertar4" },
                        new[] { "SecAtaques", "Ataques1", "Ataques2", "Ataques3", "Ataques4", "Ataques5", "Ataques6" },
                        new[] { "SecForma", "Forma1", "Forma2", "Forma3", "Forma4" },
                        new[] { "SecAuras", "Auras1", "Auras2", "Auras3", "Auras4", "Auras5", "Auras6" },
                    },
                    new string[][]
                    {
                        // v6.160 — LA PÁGINA DERECHA ES EL CÓDEX DEL
                        // ETERNO COMPLETO (la letra: «te di todo el lore
                        // para integrarlo de la mejor manera posible»):
                        // el documento canónico del usuario — el prólogo,
                        // los ONCE capítulos y el epílogo, TODOS SUS
                        // PÁRRAFOS (la v6.159 dejaba 5 de los 13 del
                        // capítulo VII sin incluir; la cura del texto en
                        // crudo: el bloque vivía en el namespace
                        // EQUIVOCADO del hjson y tML devolvía las keys
                        // sin traducir), cada capítulo una sección que se
                        // abre con el Despertar (el canon SE GANA:
                        // 0/5/12/20/30/38/45/55/65/75/85/95/110). El
                        // Compendio viejo queda debajo como EL ARCHIVO DE
                        // LA CASA.
                        new[] { "SecLore", "LoreEpigrafe0", "LoreIntro" },
                        new[] { "CodexPrologoTit", "CodexPrologoP0", "CodexPrologoP1", "CodexPrologoP2", "CodexPrologoP3" },
                        new[] { "CodexITit", "CodexIP0", "CodexIP1", "CodexIP2", "CodexIP3" },
                        new[] { "CodexIITit", "CodexIIP0", "CodexIIP1", "CodexIIP2", "CodexIIP3" },
                        new[] { "CodexIIITit", "CodexIIIP0", "CodexIIIP1", "CodexIIIP2", "CodexIIIP3" },
                        new[] { "CodexIVTit", "CodexIVP0", "CodexIVP1", "CodexIVP2", "CodexIVP3", "CodexIVP4" },
                        new[] { "CodexVTit", "CodexVP0", "CodexVP1", "CodexVP2", "CodexVP3" },
                        new[] { "CodexVITit", "CodexVIP0", "CodexVIP1", "CodexVIP2", "CodexVIP3" },
                        new[] { "CodexVIITit", "CodexVIIP0", "CodexVIIP1", "CodexVIIP2", "CodexVIIP3", "CodexVIIP4", "CodexVIIP5", "CodexVIIP6", "CodexVIIP7", "CodexVIIP8", "CodexVIIP9", "CodexVIIP10", "CodexVIIP11", "CodexVIIP12" },
                        new[] { "CodexVIIITit", "CodexVIIIP0", "CodexVIIIP1", "CodexVIIIP2", "CodexVIIIP3" },
                        new[] { "CodexIXTit", "CodexIXP0", "CodexIXP1", "CodexIXP2", "CodexIXP3" },
                        new[] { "CodexXTit", "CodexXP0", "CodexXP1", "CodexXP2", "CodexXP3", "CodexXP4", "CodexXP5" },
                        new[] { "CodexXITit", "CodexXIP0", "CodexXIP1", "CodexXIP2", "CodexXIP3" },
                        new[] { "CodexEpilogoTit", "CodexEpilogoP0", "CodexEpilogoP1", "CodexEpilogoP2", "CodexEpilogoP3" },
                        // EL ARCHIVO VIEJO DE LA CASA (el Compendio de
                        // siempre, bajo el Codex nuevo).
                        new[] { "LoreSecI", "LoreEpigrafe1", "LoreI_Loquedevora",
                                "LoreI_LaCuarentenadeTer", "LoreI_ElSellado" },
                        new[] { "LoreSecII", "LoreEpigrafe2", "LoreII_ElNivel",
                                "LoreII_ElDespertar", "LoreII_Doshambresdistinta" },
                        new[] { "LoreSecIII", "LoreEpigrafe3", "LoreIII_LaLlegada",
                                "LoreIII_ElGrimoriodelEter", "LoreIII_ElAltar" },
                        new[] { "LoreSecIV", "LoreIV_Laimposibilidaddel",
                                "LoreIV_Laentidadinmortal", "LoreIV_Elaburrimientoyla" },
                        new[] { "LoreSecV", "LoreV_LaIra",
                                "LoreV_ElPilar", "LoreV_ElSellado",
                                "LoreV_Loscompaneros", "LoreV_Lassombras",
                                "LoreV_Larespuestafinal" },
                    },
                };

                sb.End();
                // v6.150 — LA MATRIZ RECUPERADA (la autopsia: «todos los
                // textos estan corridos hacia la izquierda y un poco
                // hacia arriba»): este Begin intermedio NO pasaba la
                // matriz — el marco del libro se dibujaba con la
                // UIScaleMatrix del pase de interfaz (UI escalada), pero
                // TODO el cuerpo de texto caía en coordenadas CRUDAS de
                // pantalla → con la escala de UI ≠ 100 % los textos
                // quedaban DESPLAZADOS hacia arriba-izquierda respecto a
                // las páginas. LA CURA: reabrir con Main.UIScaleMatrix
                // (la MISMA matriz del pase — el decompile lo prueba: el
                // bucle de layers corre bajo _uiScaleMatrix) + recorte
                // con RasterizerState.ScissorTest para las columnas.
                // v6.176 — EL PASE CON TIJERAS: el cuerpo del libro corre
                // recortado a las DOS PÁGINAS — lo que no cabe en el
                // papel NO SE DIBUJA (la letra: «los textos… se
                // desbordan, especialmente en los botones»). El recorte
                // se mide en píxeles de pantalla (la matriz de UI
                // aplicada a las esquinas de las páginas).
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None,
                    LibroMarco.Tijeras, null, Main.UIScaleMatrix);
                try
                {
                    Vector2 cTL = Vector2.Transform(new Vector2(
                        Math.Min(pagIzq.X, pagDer.X), Math.Min(pagIzq.Y, pagDer.Y)), Main.UIScaleMatrix);
                    Vector2 cBR = Vector2.Transform(new Vector2(
                        Math.Max(pagIzq.Right, pagDer.Right), Math.Max(pagIzq.Bottom, pagDer.Bottom)), Main.UIScaleMatrix);
                    var recortePag = new Rectangle((int)cTL.X, (int)cTL.Y,
                        (int)(cBR.X - cTL.X) + 2, (int)(cBR.Y - cTL.Y) + 2);
                    var vpPag = sb.GraphicsDevice.Viewport.Bounds;
                    if (recortePag.X < vpPag.X) { recortePag.Width -= vpPag.X - recortePag.X; recortePag.X = vpPag.X; }
                    if (recortePag.Y < vpPag.Y) { recortePag.Height -= vpPag.Y - recortePag.Y; recortePag.Y = vpPag.Y; }
                    if (recortePag.Right > vpPag.Right) recortePag.Width = vpPag.Right - recortePag.X;
                    if (recortePag.Bottom > vpPag.Bottom) recortePag.Height = vpPag.Bottom - recortePag.Y;
                    if (recortePag.Width < 0) recortePag.Width = 0;
                    if (recortePag.Height < 0) recortePag.Height = 0;
                    if (recortePag.Width > 0 && recortePag.Height > 0)
                        sb.GraphicsDevice.ScissorRectangle = recortePag;
                }
                catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("LibroDelEternoUI", exRegistro); }

                // ============================================================
                //  v6.163 — EL CUERPO SEGÚN LA PESTAÑA: 0 = EL ÍNDICE DE
                //  SIEMPRE (mecánica izquierda · lore derecha) ·
                //  1 = EL ALTAR (la ofrenda y el sello en la izquierda;
                //  EL TRÍO con sus tres ranuras en grande en la
                //  derecha) · 2 = EL PILAR (el mundo interior: la
                //  puerta y la invocación en la izquierda; la oleada y
                //  los jefes devorados en la derecha). Las páginas de
                //  las pestañas 1-2 se dibujan con EL MISMO recorte y
                //  LA MISMA rueda (el patrón del índice).
                // ============================================================
                // v6.164 — LAS VENTANAS SEPARADAS (la letra: «cuando
                // toco el libro de informacion solo debe salir libro de
                // informacion, cuando toco altar, solo altar, y cuando
                // toco el pilar solo debe abrirse el pilar»): la
                // pestaña la decide QUIÉN ABRIÓ (Pestana) y NO HAY
                // botones para cambiarla. EL SCROLL: SOLO el índice lo
                // necesita (la letra: «el scroll en la ventana pilar es
                // innecesario […] nada de scroll» en el altar): el
                // altar y el pilar dibujan su página FIJA — el contenido
                // cabe porque la ventana del libro es GRANDE y el
                // diseño de cada página se mide para caber.
                if (LibroDelEternoUI.Pestana == 1)
                {
                    // EL ALTAR DEL LIBRO: página fija, sin rueda.
                    float sinScroll = 0f;
                    _maxIzq = 0f; _maxDer = 0f;
                    AltarDelLibro.Izquierda(sb, pagIzq, recIzq, ref sinScroll, clicPagina, clicDerPagina);
                    AltarDelLibro.Derecha(sb, pagDer, recDer, ref sinScroll, clicPagina, clicDerPagina);
                }
                else if (LibroDelEternoUI.Pestana == 2)
                {
                    // EL PILAR DEL LIBRO: página fija, sin rueda.
                    float sinScroll = 0f;
                    _maxIzq = 0f; _maxDer = 0f;
                    PilarDelLibro.Izquierda(sb, pagIzq, recIzq, ref sinScroll, clicPagina);
                    PilarDelLibro.Derecha(sb, pagDer, recDer, ref sinScroll, clicPagina);
                }
                else
                {
                    _maxIzq = DibujarColumna(sb, columnas[0], pagIzq, recIzq, ref _scrollIzq, true);
                    _maxDer = DibujarColumna(sb, columnas[1], pagDer, recDer, ref _scrollDer, false);
                }

                // ============================================================
                //  v6.154 — LA X VIVE CON LAS PÁGINAS (la cura de «la X
                //  sigue desplazada a la izquierda lejos de su hitbox»):
                //  el bloque v6.153 dibujaba la X en el pase Main.Transform
                //  (coordenadas CRUDAS) mientras el libro entero vive en
                //  el pase UIScaleMatrix (coordenadas ESCALADAS) — con la
                //  escala de UI ≠ 100 % las páginas corrían y la X se
                //  quedaba atrás: corrida a la izquierda, lejos de donde
                //  el dedo caía. AHORA el dibujo Y la lectura del click
                //  corren en EL MISMO sistema de coordenadas que las
                //  páginas: la X cae EXACTA a cualquier escala.
                // ============================================================
                // ============ LA X — EN SU SITIO DE VERDAD ============
                // v6.151 — EL BOTÓN Y SU DIBUJO SON LO MISMO (la letra:
                // «esa X esta mal ubicada, esta muy a la izquierda
                // cuando el boton real para tocarla esta mas a la
                // derecha»): la v6.150 movió el DIBUJO pero el rectángulo
                // seguía midiendo 28 px dentro del marco — el dedo caía
                // DONDE NO SE VE. AHORA: la X vive pegada a la ESQUINA
                // SUPERIOR DERECHA del LIBRO (la esquina de la TAPA, el
                // sitio universal del cierre) con su hitbox de 36 px
                // dibujada EXACTA — donde tocas es donde cae.
                // ============================================================
                //  v6.153 — LA X EN SU ESQUINA DE VERDAD (la letra: «la X
                //  no fue movida de lugar y no cuadra con su hitbox»):
                //  el rectángulo viejo SOBRESALÍA de la tapa (6 px a la
                //  derecha, 8 arriba — el botón FLOTABA fuera de la
                //  esquina y el usuario lo veía despegado). AHORA vive
                //  PEGADO POR DENTRO de la esquina superior derecha de
                //  la TAPA (el sitio universal del cierre): DIBUJO y
                //  HITBOX son EL MISMO rectángulo de 30×30.
                // ============================================================
                // v6.176 — LAS TIJERAS SE GUARDAN: la X de la tapa y el
                // selector flotante viven FUERA de las páginas (la tapa
                // y el aire) — el recorte vuelve a la pantalla entera.
                try { sb.GraphicsDevice.ScissorRectangle = sb.GraphicsDevice.Viewport.Bounds; }
                catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("LibroDelEternoUI", exRegistro); }

                var rectX = new Rectangle(x + w - 38, y + 6, 30, 30);
                bool hoverX = rectX.Contains(Main.mouseX, Main.mouseY);
                // v6.152 — LA X DIBUJADA ES SU HITBOX (la letra: «el
                //     hitbox esta bien, pero la X no, la X esta corrida
                //     hacia la izquierda»): el marco se dibuja con el
                //     MISMO rectángulo del hitbox y la letra muere
                //     EXACTAMENTE en su centro medido de verdad (el
                //     centro vertical del glifo 'X' de la fuente, no
                //     el del rectángulo — el ajuste de -2 px que la
                //     fuente MouseText pide para centrar óptico).
                sb.Draw(_pixel, rectX, (hoverX ? new Color(220, 90, 90) : new Color(96, 74, 52)) * 0.45f);
                sb.Draw(_pixel, new Rectangle(rectX.X + 2, rectX.Y + 2, rectX.Width - 4, rectX.Height - 4),
                    (hoverX ? new Color(150, 40, 40) : new Color(58, 40, 30)) * 0.55f);
                var fuenteX = FontAssets.MouseText.Value;
                Vector2 midX = fuenteX.MeasureString("X") * 0.5f;
                sb.DrawString(fuenteX, "X",
                    new Vector2(rectX.Center.X, rectX.Center.Y + 1f) - midX,
                    hoverX ? new Color(255, 210, 210) : new Color(235, 225, 205), 0f,
                    Vector2.Zero, 1.1f, SpriteEffects.None, 0f);
                if (hoverX && clicPagina)
                    LibroDelEternoUI.Cerrar();

                // ============================================================
                //  v6.168 — EL SELECTOR FLOTANTE (la ventana de elegir
                //  ofrendas y armas — la letra: «se haga con una ventana
                //  flotante, una pequeña ventana que muestre los items
                //  disponibles»): se dibuja AL FINAL, ENCIMA de las
                //  páginas (la vitrina flota sobre el libro). Su rueda
                //  se come sola (el libro no se mueve) y ESC la cierra
                //  primero — la ventanita es lo más joven de la pila.
                // ============================================================
                bool ruedaSelector = Terraria.GameInput.PlayerInput.ScrollWheelDelta != 0;
                SelectorFlotante.Dibujar(sb, clicNuevo, clicDerNuevo, ruedaSelector);

                sb.End();
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    Main.DefaultSamplerState, DepthStencilState.None,
                    Main.Rasterizer, null, Main.Transform);

                // ============ v6.144 — EL OJO DE ABAJO-DERECHA MURIÓ
                // (la letra: «el ojo de abajo a la derecha lo puedes
                // eliminar») — la página derecha es del LORE: el ojo
                // ya no vive aquí. ============
                // ============================================================
                //  v6.154 — EL SEGUNDO CURSOR MUERE (la letra: «en el
                //  libro de informacion aparece un segundo cursor
                //  desplazado a la izquierda»): el CLON v6.153 se
                //  dibujaba con el sb bajo Main.Transform (la matriz de
                //  JUEGO) dentro del pase de interfaz — con la escala de
                //  UI ≠ 100 % caía corrido: el fantasma de la izquierda.
                //  La cura es RESTAR: el clon NO existe — el problema
                //  original (el libro tapaba al cursor) ya lo mata la
                //  CAPA (vive antes de 'Vanilla: Mouse Text': vanilla
                //  dibuja SU cursor ENCIMA del libro, SIEMPRE). Un solo
                //  cursor, el de verdad, en su sitio.
                // ============================================================
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("LibroDelEternoUI", exRegistro); }
            finally
            {
                // v6.174 — EL FLANCO SE COMPLETA SIEMPRE (con o sin
                // excepción): un clic vale UN frame, el que lo leyó.
                _ultimoClicIzq = clicAhora;
                _ultimoClicDer = clicDerAhora;
            }
        }

        /// <summary>Una columna de secciones (medir y dibujar con recorte).
        /// v6.147 — EL CUERPO CENTRADO Y LEGIBLE (la letra del usuario:
        /// «los textos estan desentrados y siguen un poco ilegibles por
        /// el color»): la sangría v6.146 (X+26 con margen derecho de 26)
        /// empujaba el bloque contra el lomo — la izquierda nacía pegada
        /// a la costura y la derecha terminaba corta. Ahora el bloque SE
        /// CENTRA en su página (márgenes iguales a ambos lados) y la
        /// escala del cuerpo sube de 0.85 a 0.92 con la TINTA OSCURA
        /// nueva (ver Tinta v6.147).</summary>
        /// <summary>v6.152 — EL UMBRAL DE DESPERTAR de cada sección del
        /// Compendio (la letra: «debes hacer que las piezas de lore se
        /// vayan desbloqueando a medida que el nivel de despertar
        /// aumenta»). La sección [0] (el título del compendio) y [1]
        /// (el epígrafe) SIEMPRE se ven; el CUERPO se abre al umbral.
        /// Los umbrales por índice de sección del lore (II+): el canon
        /// SE GANA con el despertar.</summary>
        private static readonly int[] UmbralLore =
        {
            // v6.161 — LAS 19 ENTRADAS EXACTAS (la autopsia: la v6.160
            // dejó 20 entradas para 19 secciones y el 110 huérfano caía
            // sobre LoreI — EL ARCHIVO VIEJO DE LA CASA, que debe estar
            // SIEMPRE abierto, quedaba sellado al despertar 110). La
            // escalera NUEVA, pedacito a pedacito como pide la letra:
            // [0] SecLore: SIEMPRE · [1] Prólogo: SIEMPRE · capítulos
            // I..XI: 2/12/22/32/42/52/62/72/82/92/102 (una puerta de
            // despertar por capítulo, del 2 al 102) · [13] Epílogo:
            // 118 (al borde del NivelMax — el borrador infinito se lee
            // al final del camino) · [14..18] el archivo viejo:
            // v6.170 — TAMBIÉN SE GANA (la letra: «todo el lore del
            // mod debe estar sellado hasta llegar a cierto nivel de
            // despertar»): 4/24/44/64/84 — el Compendio de la casa
            // abre sus cinco historias a lo largo del camino.
            0, 0, 2, 12, 22, 32, 42, 52, 62, 72, 82, 92, 102, 118,
            4, 24, 44, 64, 84,
        };

        /// <summary>El Despertar del portador local (para el lore).</summary>
        private static int DespertarLocal()
        {
            try
            {
                return Systems.DespertarGrimorio.NivelDe(Main.LocalPlayer);
            }
            catch { return 0; }
        }

        private float DibujarColumna(SpriteBatch sb, string[][] secciones,
            Rectangle pagina, Rectangle recorte, ref float scroll, bool esIzq)
        {
            var fuente = FontAssets.MouseText.Value;
            float cy = recorte.Y - scroll;
            // v6.147 — EL BLOQUE CENTRADO: el texto ocupa el ANCHO ÚTIL
            // de la página y la sangría se reparte a los DOS lados
            float margen = 18f;
            float xizq = pagina.X + margen;
            float maxAncho = pagina.Width - margen * 2f;

            // v6.152 — EL LORE PROGRESIVO (la letra: «debes hacer
            //     que las piezas de lore se vayan desbloqueando a
            //     medida que el nivel de despertar aumenta»): en la
            //     página del COMPENDIO cada sección nace bajo su
            //     umbral de despertar — el título y el epígrafe se ven
            //     SIEMPRE, el cuerpo SOLO al llegar. La mecánica
            //     (página izquierda) está SIEMPRE completa.
            int despertarAhora = esIzq ? int.MaxValue : DespertarLocal();
            // v6.170 — EL SELLO DEL CRONISTA (la letra: «es quien
            //     desbloquea finalmente esa parte del lore en el libro
            //     de informacion»): las CINCO ARCAS del archivo viejo
            //     (índices 14..18) piden el despertar Y HABERLO
            //     ESCUCHADO — el bit de ShardPlayer que el Cronista
            //     marca al narrarlas. Sin su charla, el umbral
            //     alcanzado NO abre: hay que ir a OÍRLO.
            int selloCronista = 0;
            if (!esIzq)
            {
                try
                {
                    var sp = Main.LocalPlayer.GetModPlayer
                        <global::AethonMod.Content.Players.ShardPlayer>();
                    if (sp != null) selloCronista = sp.LoreDesbloqueado;
                }
                catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("LibroDelEternoUI", exRegistro); }
            }
            foreach (var sec in secciones)
            {
                cy += 12f;
                string titulo = T(sec[0]);
                int idxSec = System.Array.IndexOf(secciones, sec);
                bool seccionAbierta = true;
                if (!esIzq && idxSec >= 2)
                {
                    int umbral = idxSec < UmbralLore.Length ? UmbralLore[idxSec] : 0;
                    seccionAbierta = despertarAhora >= umbral;
                    // v6.171 — LAS HISTORIAS (14..18) además piden SU bit
                    if (seccionAbierta && idxSec >= 14 && idxSec <= 18)
                    {
                        int bitHistoria = 1 << (idxSec - 14);
                        seccionAbierta = (selloCronista & bitHistoria) != 0;
                    }
                }
                var medT = fuente.MeasureString(titulo);
                if (VisibleEn(cy, medT.Y, recorte))
                {
                    // v6.149 — EL TÍTULO CENTRADO (la letra: «todos los
                    // textos estan desentrados»): el título de sección vive
                    // en el CENTRO de la página, con su subrayado SIMÉTRICO
                    // a ambos lados (el rótulo de archivo clásico).
                    float xTit = pagina.X + (pagina.Width - medT.X * 0.95f) * 0.5f;
                    Utils.DrawBorderString(sb, titulo, new Vector2(xTit, cy), TintaTitulo, 0.95f);
                    float yLinea = cy + medT.Y * 0.95f + 2f;
                    sb.Draw(_pixel, new Rectangle((int)(pagina.X + margen), (int)yLinea,
                        (int)Math.Max(4f, xTit - 20f - (pagina.X + margen)), 2), TintaCap * 0.55f);
                    sb.Draw(_pixel, new Rectangle((int)(xTit + medT.X * 0.95f + 20f), (int)yLinea,
                        (int)Math.Max(4f, pagina.Right - margen - (xTit + medT.X * 0.95f + 20f)), 2),
                        TintaCap * 0.55f);
                }
                cy += medT.Y * 0.95f + 9f;

                if (seccionAbierta)
                {
                    for (int i = 1; i < sec.Length; i++)
                        cy = LineaLibro(sb, T(sec[i]), xizq, cy, maxAncho,
                            Tinta, recorte, pagina);
                }
                else
                {
                    // LA SECCIÓN SELLADA: su epígrafe (sec[1] — la
                    // promesa) y EL SELLO — la línea de puntos con el
                    // candado y el nivel que la abre
                    if (sec.Length > 1)
                        cy = LineaLibro(sb, T(sec[1]), xizq, cy, maxAncho,
                            Tinta, recorte, pagina);
                    int umbralSello = idxSec < UmbralLore.Length ? UmbralLore[idxSec] : 0;
                    // v6.171 — EL SELLO DE LAS HISTORIAS DICE LA
                    // VERDAD (la letra: «yo lo llamaria historias o
                    // escrituras o algo asi»): el Cronista narra y el
                    // libro abre — el resto solo cuenta la puerta de
                    // despertar de siempre.
                    bool esHistoria = idxSec >= 14 && idxSec <= 18;
                    bool despertarYa = despertarAhora >= umbralSello;
                    string sello = esHistoria
                        ? (despertarYa
                            ? "· · · ✦ el Cronista aún no contó esta historia — búscalo ✦ · · ·"
                            : "· · · ✦ sellada hasta el Despertar " + umbralSello + " — y las palabras del Cronista ✦ · · ·")
                        : "· · · ✦ sellado · se revela al Despertar " + umbralSello + " ✦ · · ·";
                    cy = LineaLibro(sb, sello, xizq, cy, maxAncho,
                        new Color(120, 104, 82), recorte, pagina);
                }
                cy += 5f;
            }
            return Math.Max(0f, cy + scroll - recorte.Y + 20f);
        }

        private float LineaLibro(SpriteBatch sb, string texto, float x, float cy,
            float maxAncho, Color c, Rectangle recorte, Rectangle pagina)
        {
            var fuente = FontAssets.MouseText.Value;
            float alto = fuente.MeasureString("Ag").Y * 0.92f + 3f;
            var palabras = texto.Split(' ');
            var linea = new System.Text.StringBuilder();
            void Volcar(float y)
            {
                if (!VisibleEn(y, alto, recorte)) return;
                // v6.149 — EL CENTRADO ÓPTICO (la letra: «todos los textos
                // estan desentrados»): cada línea del cuerpo se CENTRA en
                // el ancho útil de la página — el bloque de texto respira
                // simétrico, como un manuscrito de verdad (la última línea
                // corta ya no cuelga sola a la izquierda).
                // v6.151 — EL BORDE BLANCO + EL CENTRO NEGRO (la letra):
                // DOS pases — el PASO 1 dibuja el texto BLANCO en los 4
                // offsets cardinales (el contorno que SIEMPRE se lee,
                // incluso sobre la sombra del lomo), el PASO 2 el texto
                // NEGRO encima (el cuerpo de la tinta). El contraste
                // máximo de la letra, sin tocar los títulos.
                float ancho = fuente.MeasureString(linea.ToString()).X * 0.92f;
                float xc = pagina.X + (pagina.Width - ancho) * 0.5f;
                var posL = new Vector2(xc, y);
                const float o = 1.1f;   // el grosor del contorno
                // v6.166 — EL CONTORNO SEPIA (la cura del halo blanco):
                // el borde toma el color de la SOMBRA del papel — se
                // funde con la página y la tinta queda limpia.
                Color borde = new Color(178, 158, 120);
                sb.DrawString(fuente, linea.ToString(), posL + new Vector2(o, 0f),
                    borde, 0f, Vector2.Zero, 0.92f, SpriteEffects.None, 0f);
                sb.DrawString(fuente, linea.ToString(), posL + new Vector2(-o, 0f),
                    borde, 0f, Vector2.Zero, 0.92f, SpriteEffects.None, 0f);
                sb.DrawString(fuente, linea.ToString(), posL + new Vector2(0f, o),
                    borde, 0f, Vector2.Zero, 0.92f, SpriteEffects.None, 0f);
                sb.DrawString(fuente, linea.ToString(), posL + new Vector2(0f, -o),
                    borde, 0f, Vector2.Zero, 0.92f, SpriteEffects.None, 0f);
                sb.DrawString(fuente, linea.ToString(), posL,
                    c, 0f, Vector2.Zero, 0.92f, SpriteEffects.None, 0f);
            }
            foreach (string pal in palabras)
            {
                string prueba = linea.Length == 0 ? pal : linea + " " + pal;
                if (fuente.MeasureString(prueba).X * 0.92f > maxAncho && linea.Length > 0)
                {
                    Volcar(cy);
                    cy += alto;
                    linea.Clear().Append(pal);
                }
                else linea.Clear().Append(prueba);
            }
            if (linea.Length > 0)
            {
                Volcar(cy);
                cy += alto;
            }
            return cy + 3f;
        }

        private static bool VisibleEn(float cy, float alto, Rectangle r)
            => cy < r.Y + r.Height && cy + alto > r.Y;

        private static float EscalaQueCabe(string t, float esc, float maxAncho)
        {
            var f = FontAssets.MouseText.Value;
            float e = esc;
            while (e > 0.55f && f.MeasureString(t).X * e > maxAncho) e -= 0.05f;
            return e;
        }

        // ================================================================
        //  EL OJO DE LOS MINIONS (la letra: «el ojo que mira al jugador
        //  es el mismo ojo de los minions usa ese»): LA TEXTURA DEL
        //  ESCUADRÓN (OjoEterno_Abierto), con la PUPILA viva que SIGUE
        //  AL CURSOR (el mecanismo de los ojos del escuadrón: el iris
        //  se desplaza hacia el objetivo) y su parpadeo propio.
        // ================================================================
        private void DibujarElOjo(SpriteBatch sb, float cx, float cy, float radio)
        {
            try
            {
                Texture2D tex = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Projectiles/OjoEterno_Abierto").Value;

                // EL RESPLANDOR VIVO (el aura del ojo de la casa)
                float pulso = 0.30f + 0.12f *
                    (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2.6f);
                sb.Draw(_glow, new Rectangle((int)(cx - radio * 2.0f), (int)(cy - radio * 2.0f),
                    (int)(radio * 4.0f), (int)(radio * 4.0f)),
                    new Color(150, 84, 235) * pulso);

                // EL OJO: la textura del escuadrón, la escala del botón
                float escala = (radio * 2f) / Math.Max(tex.Width, tex.Height);
                var pos = new Vector2(cx, cy);

                // LA MIRADA: la textura SE INCLINA hacia el cursor (el
                // ojo entero gira a mirar — la pupila del sprite ya mira
                // al frente: el corrimiento hace el efecto)
                Vector2 hacia = new Vector2(Main.mouseX, Main.mouseY) +
                    Main.screenPosition - pos;   // mundo→pantalla coherente
                float rot = 0f;
                if (hacia.LengthSquared() > 1f)
                    rot = MathHelper.Clamp(
                        (float)Math.Atan2(hacia.Y, hacia.X - Math.Abs(hacia.X) * 0.86f) * 0.25f,
                        -0.3f, 0.3f);

                // EL PARPADEO: el ojo SE ESCONDE un instante (8 ticks)
                float alpha = 1f;
                if (_parpadeo > 0)
                    alpha = Math.Abs(_parpadeo - 4) / 4f;

                sb.Draw(tex, pos - Main.screenPosition + new Vector2(Main.mouseX, Main.mouseY) * 0f,
                    null, Microsoft.Xna.Framework.Color.White * alpha,
                    rot, tex.Size() * 0.5f, escala, SpriteEffects.None, 0f);

                // EL IRIS VIVO — EL DIBUJO EXTRA: el núcleo dorado que se
                // DESPLAZA hacia el cursor (el mecanismo del escuadrón)
                Vector2 miradaPantalla = new Vector2(Main.mouseX, Main.mouseY) -
                    new Vector2(cx, cy);
                if (miradaPantalla.LengthSquared() > 1f) miradaPantalla.Normalize();
                Vector2 iris = miradaPantalla * (radio * 0.18f);

                sb.Draw(_glow, new Rectangle(
                    (int)(cx + iris.X - radio * 0.28f), (int)(cy + iris.Y - radio * 0.28f),
                    (int)(radio * 0.56f), (int)(radio * 0.56f)),
                    new Color(255, 200, 90) * (0.85f * alpha));
                // EL BRILLO (la chispa de vida)
                sb.Draw(_glow, new Rectangle(
                    (int)(cx + iris.X - radio * 0.34f - 4), (int)(cy + iris.Y - radio * 0.34f - 4),
                    8, 8), new Color(255, 246, 220) * (0.8f * alpha));
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("LibroDelEternoUI", exRegistro); }
        }
    }
    // ====================================================================
    //  v6.163 — EL ALTAR DEL LIBRO. IZQUIERDA = LA OFRENDA (el hueco, el
    //  precio A LA VISTA, el botón CONSAGRAR, cómo ganar esencias y la
    //  puerta al interior). DERECHA = EL TRÍO con sus tres ranuras, la
    //  casilla «las tres a la vez», «Desequipar todo» y EL ALBEDRÍO.
    //  v6.174 — REESCRITA (la letra: «al poner la tercera las dos
    //  iniciales se desequipan y solo queda la tercera», «un selector a
    //  modo de checklist», «hay letras demasiado oscuras»):
    //   · LOS CLICS: la página recibe el flanco SOLO cuando el selector
    //     flotante no vive (el libro decide — un solo dueño por clic).
    //   · EL SELECTOR NACE DEBAJO DE TODO: anclado bajo el botón
    //     CONSAGRAR (ofrenda) o bajo la última fila del albedrío (trío)
    //     — su ventana JAMÁS cubre un control que el clic pueda tocar.
    //   · EL ALBEDRÍO: tres casillas por ranura (Original · A enemigos ·
    //     Al cursor) con SU palomita.
    //   · LA TINTA: LibroLineas.Texto (contorno blanco + los colores del
    //     índice).
    // ====================================================================
    internal static class AltarDelLibro
    {
        // LA PALETA DEL LIBRO DE INFORMACIÓN, EXACTA (la de LibroEstado)
        private static readonly Color Tinta = new(28, 20, 16);
        private static readonly Color TintaVioleta = new(148, 72, 178);
        private static readonly Color TintaOro = new(168, 108, 44);
        private static readonly Color TintaApagado = new(82, 60, 40);   // v6.176: oscurecido (leía claro sobre el papel)
        private static readonly Color TintaRojo = new(140, 40, 40);
        private static readonly Color PapelSombra = new(190, 172, 134);

        // LA GEOMETRÍA VIVA
        internal static Rectangle GeoSlot, GeoConsagrar, GeoPuerta;
        internal static readonly Rectangle[] GeoTrio = new Rectangle[3];
        internal static Rectangle GeoMarcaTres;
        internal static Rectangle GeoDesequiparTodo;
        internal static readonly Rectangle[,] GeoAlbedrio = new Rectangle[3, 3];

        private const string A = "Mods.AethonMod.Altar.";
        private static string At(string c) => Language.GetTextValue(A + c);

        /// <summary>¿El bloque nace DENTRO del cuerpo de la página?</summary>
        private static bool Cabe(float cy, float alto, Rectangle rec) => cy + alto <= rec.Bottom;

        /// <summary>El título de sección cosido (centrado, con sus filetes).</summary>
        private static void Titulo(SpriteBatch sb, string t, Rectangle pag, float cy, out float cyOut, float esc = 0.95f)
        {
            var fuente = FontAssets.MouseText.Value;
            t = t ?? "";
            esc = LibroLineas.EscalaQueCabe(t, esc, pag.Width - 80f);
            var m = fuente.MeasureString(t) * esc;
            float xT = pag.X + (pag.Width - m.X) * 0.5f;
            LibroLineas.Texto(sb, t, new Vector2(xT, cy), TintaVioleta, esc);
            float yL = cy + m.Y + 3f;
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle((int)(pag.X + 16), (int)yL, (int)Math.Max(4f, xT - 18f - pag.X - 16), 2), TintaOro * 0.55f);
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle((int)(xT + m.X + 18f), (int)yL,
                    (int)Math.Max(4f, pag.Right - 16 - (xT + m.X + 18f)), 2), TintaOro * 0.55f);
            cyOut = cy + m.Y + 12f;
        }

        /// <summary>El botón cosido del libro (texto MEDIDO para caber). Un
        /// botón que no cabe en la página queda SIN hitbox (geo vacío).</summary>
        private static float Boton(SpriteBatch sb, Rectangle pag, Rectangle rec, float cy, string t,
            Color cBase, out Rectangle geo, bool clicNuevo, System.Action alClic,
            bool apagado = false)
        {
            var fuente = FontAssets.MouseText.Value;
            // v6.177 — EL BOTÓN GRANDE (la letra: «todos los textos del
            // altar son muy pequeños» + «los textos de los botones se
            // desbordan»): escala base 1.05 (antes 0.88) y el texto se
            // mide CON margen de halo (el contorno vive FUERA del área
            // medida) — nada desborda jamás y el botón se lee.
            float esc = LibroLineas.EscalaQueCabe(t ?? "", 1.05f, 202f);
            Rectangle b = new Rectangle((int)(pag.X + pag.Width / 2f - 113f), (int)cy, 226, 46);
            bool vivo = Cabe(cy, 46f, rec);
            geo = vivo ? b : Rectangle.Empty;
            bool hov = vivo && !apagado && b.Contains(Main.mouseX, Main.mouseY);
            sb.Draw(TextureAssets.MagicPixel.Value, b,
                (hov ? cBase : new Color(cBase.R * 2 / 3, cBase.G * 2 / 3, cBase.B * 2 / 3)) * (hov ? 0.30f : 0.15f));
            LibroMarco.Marco(sb, b, apagado ? TintaApagado * 0.5f : (hov ? cBase : cBase * 0.55f));
            // v6.176 — EL BOTÓN JAMÁS REBOSA (re-medida al momento del
            // trazo — la ventana de la fuente dinámica se cierra aquí)
            esc = LibroLineas.EscalaQueCabe(t ?? "", esc, 202f);
            LibroLineas.Texto(sb, t ?? "", new Vector2(b.Center.X, b.Center.Y),
                apagado ? TintaApagado : Tinta, esc, 0.5f, 0.5f);
            if (hov && clicNuevo) alClic?.Invoke();
            return cy + 54f;
        }

        /// <summary>LA PÁGINA IZQUIERDA: la ofrenda y su mundo.</summary>
        public static float Izquierda(SpriteBatch sb, Rectangle pag, Rectangle rec,
            ref float scroll, bool clicNuevo, bool clicDerNuevo)
        {
            float cy = rec.Y + 6f;                       // página FIJA: sin scroll
            float maxAncho = pag.Width - 36f;
            float Linea(string t, float y, float esc, Color c)
                => LibroLineas.Linea(sb, t ?? "", pag.X + 18f, y, maxAncho, esc, c, rec, pag);
            GeoSlot = Rectangle.Empty; GeoConsagrar = Rectangle.Empty; GeoPuerta = Rectangle.Empty;
            if (!AltarOfrendasUI.Abierta) return 0f;

            Item arma = AltarOfrendasUI.Ofrenda;
            bool hayArma = arma != null && !arma.IsAir;
            bool lista = AltarOfrendasUI.EsOfrendaConsagriblePublica(arma);
            bool esEsencias = lista && hayArma && arma.type ==
                ModContent.ItemType<global::AethonMod.Content.Items.Altar.ItemCorrupto>();
            bool abrirSelector = false;
            Rectangle ancla = new Rectangle(pag.X + pag.Width / 2 - 141, (int)cy, 282, 1);

            // ── LA OFRENDA (el slot + su guía) ──
            Titulo(sb, At("Titulo"), pag, cy, out cy);
            Rectangle slot = new Rectangle((int)(pag.X + pag.Width / 2f - 26f), (int)cy, 52, 52);
            GeoSlot = slot;
            bool hoverSlot = slot.Contains(Main.mouseX, Main.mouseY);
            sb.Draw(TextureAssets.MagicPixel.Value, slot, PapelSombra * 0.9f);
            LibroMarco.Marco(sb, slot, hoverSlot ? TintaVioleta : TintaVioleta * 0.45f);
            if (hayArma)
            {
                Texture2D tex = TextureAssets.Item[arma.type].Value;
                float esc = Math.Min(44f / tex.Width, 44f / tex.Height);
                sb.Draw(tex, slot.Center.ToVector2(), null, Color.White, 0f,
                    tex.Size() * 0.5f, esc, SpriteEffects.None, 0f);
                if (arma.stack > 1)
                    LibroLineas.Texto(sb, arma.stack.ToString(),
                        new Vector2(slot.Right - 6f, slot.Bottom - 8f), Tinta, 0.85f, 1f, 0.85f);
            }
            // v6.177 — EL SLOT LIMPIO (la letra: «en el slot de ofrendas
            // deberia estar el slot de ofrenda sin textos… ese texto es
            // innecesario»): el hueco vacío queda VACÍO de verdad.

            if (hoverSlot && clicDerNuevo) AltarOfrendasUI.ClicDerechoSlot();
            else if (hoverSlot && clicNuevo)
            {
                if (Main.mouseItem.IsAir && !hayArma) abrirSelector = true;   // el hueco vacío: elegir
                else AltarOfrendasUI.ClicEnSlot();                           // dejar / retirar / devorar
            }
            cy += 58f;
            cy = Linea(At("EtiquetaArma"), cy, 0.92f, Tinta);
            cy = Linea(At("FiltroArma"), cy, 0.84f, TintaApagado);
            if (AltarOfrendasUI.OfrendaPrestada && Cabe(cy, 44f, rec))
                cy = Linea(At("OfrendaPrestada"), cy, 0.84f, TintaOro);

            // ── EL SELLO (el precio A LA VISTA + consagrar) ──
            cy += 6f;
            Titulo(sb, At("SelloTitulo"), pag, cy, out cy);
            if (lista && hayArma)
            {
                if (esEsencias)
                {
                    cy = Linea(Language.GetTextValue(A + "DevoraLinea", arma.stack,
                        global::AethonMod.Content.Systems.SubmundoGrimorio.EsenciasConsagradas,
                        global::AethonMod.Content.Systems.SubmundoGrimorio.EsenciasPuerta), cy, 0.88f, Tinta);
                }
                else
                {
                    int coste = global::AethonMod.Content.Globals.CorruptoGlobalItem.CosteCorrupcion(arma);
                    int llevadas = global::AethonMod.Content.Globals.CorruptoGlobalItem
                        .EsenciasEnLaBolsa(Main.LocalPlayer);
                    cy = Linea(Language.GetTextValue(A + "SelloCuesta", coste), cy, 0.9f, Tinta);
                    cy = Linea(Language.GetTextValue(A + "SelloLlevas", llevadas), cy, 0.88f, TintaApagado);
                    if (llevadas < coste)
                        cy = Linea(Language.GetTextValue(A + "SelloFalta", coste - llevadas), cy, 0.88f, TintaRojo);
                }
            }
            else
                cy = Linea(At("SelloSinOfrenda"), cy, 0.88f, TintaApagado);
            float cyBoton = cy + 4f;
            cy = Boton(sb, pag, rec, cyBoton, At("BotonConsagrar"), TintaVioleta, out GeoConsagrar, clicNuevo,
                () => AltarOfrendasUI.ClicEnConsagrar(), apagado: !hayArma);
            if (GeoConsagrar.Width > 0)
                // v6.175 — LA VENTANA PEGADA AL SLOT (la letra: «la ventana
                // flotante en el slot consagrar… están muy separadas de los
                // slots, las ventanas flotantes deben estar pegadas al slot
                // justo debajo de él»): el ancla es EL SLOT DE LA OFRENDA,
                // no el botón de abajo — la ventana nace justo bajo el hueco
                ancla = new Rectangle(GeoSlot.X, GeoSlot.Bottom + 2, GeoSlot.Width, 1);

            // ── GANA + CÓMO CONSEGUIR ESENCIAS ──
            cy += 4f;
            if (Cabe(cy, 120f, rec))
            {
                Titulo(sb, At("GanaTitulo"), pag, cy, out cy);
                cy = Linea(Language.GetTextValue(A + "GanaEsencias",
                    global::AethonMod.Content.Systems.SubmundoGrimorio.EsenciasPuerta), cy, 0.88f, Tinta);
                cy = Linea(At("GanaConsagrada"), cy, 0.88f, Tinta);
                cy += 4f;
            }
            if (Cabe(cy, 120f, rec))
            {
                Titulo(sb, At("ComoTitulo"), pag, cy, out cy);
                cy = Linea(At("ComoCriaturas"), cy, 0.86f, TintaApagado);
                cy = Linea(At("ComoJefes"), cy, 0.86f, TintaApagado);
                cy = Linea(At("ComoOleadas"), cy, 0.86f, TintaApagado);
            }

            // ── EL MUNDO INTERIOR (la puerta cuando las mil están) ──
            cy += 6f;
            if (Cabe(cy, 60f, rec))
            {
                Titulo(sb, At("MundoTitulo"), pag, cy, out cy);
                if (global::AethonMod.Content.Systems.SubmundoGrimorio.PuertaAbierta)
                    cy = Boton(sb, pag, rec, cy, At("MundoPuerta"), TintaOro, out GeoPuerta, clicNuevo,
                        () => AltarOfrendasUI.ClicEnPuerta());
                else
                    cy = Linea(Language.GetTextValue(A + "MundoProgreso",
                        global::AethonMod.Content.Systems.SubmundoGrimorio.EsenciasConsagradas,
                        global::AethonMod.Content.Systems.SubmundoGrimorio.EsenciasPuerta), cy, 0.88f, Tinta);
            }

            // ── EL SELECTOR DE LA OFRENDA: nace DEBAJO del botón CONSAGRAR ──
            if (abrirSelector)
                SelectorFlotante.Abrir(ancla, false, At("SelectorTitulo"), -2);
            return 0f;
        }

        /// <summary>LA PÁGINA DERECHA: EL TRÍO DEL ETERNO en grande.</summary>
        public static float Derecha(SpriteBatch sb, Rectangle pag, Rectangle rec,
            ref float scroll, bool clicNuevo, bool clicDerNuevo = false)
        {
            var fuente = FontAssets.MouseText.Value;
            float cy = rec.Y + 6f;
            float maxAncho = pag.Width - 36f;
            float Linea(string t, float y, float esc, Color c)
                => LibroLineas.Linea(sb, t ?? "", pag.X + 18f, y, maxAncho, esc, c, rec, pag);
            for (int i = 0; i < 3; i++) { GeoTrio[i] = Rectangle.Empty; for (int j = 0; j < 3; j++) GeoAlbedrio[i, j] = Rectangle.Empty; }
            GeoMarcaTres = Rectangle.Empty; GeoDesequiparTodo = Rectangle.Empty;
            if (!AltarOfrendasUI.Abierta) return 0f;

            Titulo(sb, At("TrioTitulo"), pag, cy, out cy);
            var trio = global::AethonMod.Content.Weapons.Investigaciones.TrioDelEterno.Del(Main.LocalPlayer);
            if (trio == null)
            {
                Linea(At("TrioSinTrio"), cy, 0.92f, TintaApagado);
                return 0f;
            }
            bool selectorVivo = SelectorFlotante.Abierta;    // el libro ya no nos da clics: cinturón y tirantes
            cy = Linea(At("TrioPista"), cy, 0.88f, Tinta);
            cy += 12f;

            // ── LAS TRES RANURAS COSIDAS AL PAPEL ──
            int abrirPara = int.MinValue;
            for (int i = 0; i < 3; i++)
            {
                Rectangle s = new Rectangle(pag.X + pag.Width / 2 - 99 + i * 66, (int)cy, 62, 62);
                GeoTrio[i] = s;
                bool hov = s.Contains(Main.mouseX, Main.mouseY);
                Item ra = trio.Ranuras != null && i < trio.Ranuras.Length ? trio.Ranuras[i] : null;
                bool llena = ra != null && !ra.IsAir;
                sb.Draw(TextureAssets.MagicPixel.Value, s, llena ? PapelSombra * 1.25f : PapelSombra * 0.9f);
                LibroMarco.Marco(sb, s, hov ? TintaVioleta : llena ? TintaOro : TintaOro * 0.4f);
                if (llena)
                {
                    Texture2D tex = TextureAssets.Item[ra.type].Value;
                    float esc = Math.Min(50f / tex.Width, 50f / tex.Height);
                    sb.Draw(tex, s.Center.ToVector2(), null, Color.White, 0f,
                        tex.Size() * 0.5f, esc, SpriteEffects.None, 0f);
                }
                else
                    LibroLineas.Texto(sb, (i + 1).ToString(), s.Center.ToVector2(), TintaApagado, 1.0f, 0.5f, 0.5f);

                // EL GESTO: izquierdo con el cursor vacío = ELEGIR · con un arma al cursor = DEJARLA · derecho = DEVOLVER
                if (hov && !selectorVivo)
                {
                    if (clicDerNuevo) AltarOfrendasUI.DesequiparRanura(i);
                    else if (clicNuevo)
                    {
                        if (Main.mouseItem.IsAir) abrirPara = i;
                        else AltarOfrendasUI.ClicEnRanura(i);
                    }
                }
            }
            cy += 72f;

            // ── LA CASILLA «LAS TRES A LA VEZ» ──
            Rectangle marca = new Rectangle(pag.X + pag.Width / 2 - 150, (int)cy, 22, 22);
            GeoMarcaTres = marca;
            bool marcada = trio.ModoDisparo >= 3;
            bool hovM = marca.Contains(Main.mouseX, Main.mouseY);
            Rectangle txtMarca = new Rectangle(marca.Right + 8, marca.Y - 3, 282, 28);
            bool hovTxt = txtMarca.Contains(Main.mouseX, Main.mouseY);
            // v6.176 — EL CHECKLIST UNIFORME (la letra: «el marcador del
            // checklis que dice que se usan las 3 armas esta mal,
            // deberia ser igual a los otros checklis donde el cuadrito
            // se pinta de morado»): la palomita MUERE — la casilla vive
            // EXACTAMENTE como las del albedrío: el cuadradito se
            // RELLENA de morado, sin símbolo encima.
            sb.Draw(TextureAssets.MagicPixel.Value, marca, marcada ? TintaVioleta * 0.85f : PapelSombra * 0.9f);
            LibroMarco.Marco(sb, marca, marcada || hovM ? TintaVioleta : TintaApagado * 0.6f);
            LibroLineas.Texto(sb, At("TrioMarcaTexto"), new Vector2(marca.Right + 8, marca.Center.Y),
                marcada ? TintaVioleta : hovTxt ? Tinta : TintaApagado, 0.92f, 0f, 0.5f);
            if ((hovM || hovTxt) && (clicNuevo || clicDerNuevo) && !selectorVivo)
                AltarOfrendasUI.ClicEnMarcaTres();
            cy += 34f;

            // ── EL BOTÓN «DESEQUIPAR TODO» ──
            Rectangle btnTodo = new Rectangle(pag.X + pag.Width / 2 - 78, (int)cy, 156, 28);
            GeoDesequiparTodo = btnTodo;
            bool hovTodo = btnTodo.Contains(Main.mouseX, Main.mouseY);
            sb.Draw(TextureAssets.MagicPixel.Value, btnTodo, hovTodo ? TintaOro * 0.30f : PapelSombra * 0.9f);
            LibroMarco.Marco(sb, btnTodo, hovTodo ? TintaOro : TintaApagado * 0.6f);
            LibroLineas.Texto(sb, At("TrioDesequiparTodo"), btnTodo.Center.ToVector2(),
                hovTodo ? Tinta : TintaApagado, 0.92f, 0.5f, 0.5f);
            if (hovTodo && clicNuevo && !selectorVivo) AltarOfrendasUI.ClicEnDesequiparTodo();
            cy += 40f;

            // ================================================================
            //  v6.174 — EL ALBEDRÍO DE CADA ARMA (la letra: «un selector a
            //  modo de checklist para darte cierta inteligencia a los
            //  proyectiles: perseguir al enemigo, al cursor, o dejarlos
            //  como son originalmente»): por cada ranura, TRES casillas —
            //  Original (el proyectil tal cual nació) · A enemigos (gira
            //  hacia la presa más cercana) · Al cursor (persigue el ratón).
            //  La viva lleva SU palomita; tocarla la devuelve a Original.
            // ================================================================
            cy += 4f;
            Titulo(sb, At("TrioAlbedrioTitulo"), pag, cy, out cy, 0.8f);
            cy = Linea(At("TrioAlbedrioPista"), cy, 0.84f, TintaApagado);

            float anchoEtiqueta = 78f;
            for (int i = 0; i < 3 && Cabe(cy, 28f, rec); i++)
            {
                Item ra = trio.Ranuras != null && i < trio.Ranuras.Length ? trio.Ranuras[i] : null;
                bool llena = ra != null && !ra.IsAir;
                float x0 = pag.X + 18f;
                string et = Language.GetTextValue(A + "TrioRanuraEtiqueta", i + 1);
                LibroLineas.Texto(sb, et, new Vector2(x0, cy + 13f), llena ? Tinta : TintaApagado, 0.86f, 0f, 0.5f);
                for (int j = 0; j < 3; j++)
                {
                    // ========================================================
                    //  v6.175 — EL CHECKLIST SIMPLE (la letra: «el checklist
                    //  de las armas del trio eterno es demasiado grando, o
                    //  sea es una linea morada, hazlo simple, que el
                    //  cuadradito solo cambie de color y se rellene de
                    //  morado»): los CHIPS anchos MUEREN (la caja grande
                    //  con marco leía como «una línea morada») — queda SOLO
                    //  el cuadradito cosido: apagado vacío · viva = RELLENO
                    //  DE MORADO + su marco violeta. El texto al lado.
                    // ========================================================
                    Rectangle casilla = new Rectangle((int)(x0 + anchoEtiqueta + j * 118f), (int)cy + 4, 18, 18);
                    GeoAlbedrio[i, j] = casilla;
                    bool viva = llena && trio.Inteligencia != null && i < trio.Inteligencia.Length &&
                        trio.Inteligencia[i] == j;
                    bool hovC = llena && casilla.Contains(Main.mouseX, Main.mouseY);
                    // EL CUADRADITO: vacío (papel) o RELLENO de morado
                    sb.Draw(TextureAssets.MagicPixel.Value, casilla,
                        viva ? TintaVioleta * 0.85f : PapelSombra * 0.7f);
                    LibroMarco.Marco(sb, casilla,
                        !llena ? TintaApagado * 0.35f : viva || hovC ? TintaVioleta : TintaApagado * 0.7f);
                    string txt = Language.GetTextValue("Mods.AethonMod.Items.TrioAlbedrio" + j);
                    float escT = LibroLineas.EscalaQueCabe(txt, 0.86f, 88f);
                    LibroLineas.Texto(sb, txt, new Vector2(casilla.Right + 6f, casilla.Center.Y),
                        !llena ? TintaApagado : viva ? TintaVioleta : Tinta, escT, 0f, 0.5f);
                    if (hovC && (clicNuevo || clicDerNuevo) && !selectorVivo)
                        AltarOfrendasUI.ClicEnAlbedrio(i, j);
                }
                cy += 32f;
            }

            // ── EL SELECTOR DEL TRÍO: PEGADO A LA RANURA CLICADA ──
            // v6.175 (la letra: «el slot de las armas del trio eterno…
            // deben estar pegadas al slot justo debajo de él»): nace
            // DEBAJO DE LA RANURA que la abrió, no al final de la página
            if (abrirPara != int.MinValue && GeoTrio[abrirPara].Width > 0)
            {
                Rectangle s = GeoTrio[abrirPara];
                SelectorFlotante.Abrir(new Rectangle(s.X, s.Bottom + 2, s.Width, 1), true,
                    At("SelectorTrioTitulo"), abrirPara, 3);
            }
            return 0f;
        }
    }

    internal static class LibroLineas
    {
        // ================================================================
        //  v6.174 — LA TIPOGRAFÍA DEL LIBRO (la letra: «cambia el color de
        //  la tipografia de la ventana del pilar y la ventana del altar,
        //  ambos deben tener los colores de la ventana del libro de
        //  informacion» + «cuando pongas letras oscuras deben tener bordes
        //  blancos para poder verlas»). LA AUTOPSIA: el pilar y el altar
        //  pintaban SUS cadenas con Utils.DrawBorderString — que dibuja
        //  el texto CUATRO veces en NEGRO puro alrededor y una vez en la
        //  tinta: a las escalas pequeñas de los controles (0.62–0.78) el
        //  borde negro SE COME el glifo y toda la letra lee NEGRA sobre el
        //  papel — «hay letras demasiado oscuras y no se ven nada».
        //  LA CURA: UNA sola función para TODO el libro — el contorno es
        //  BLANCO cálido (el halo que separa la tinta del papel) y la
        //  tinta encima es LA DEL ÍNDICE (cuerpo 28,20,16 · título
        //  148,72,178 · capítulo 168,108,44).
        // ================================================================

        /// <summary>La línea con recorte y centrado óptico (el pase doble del índice).</summary>
        public static float Linea(SpriteBatch sb, string texto, float x, float cy,
            float maxAncho, float esc, Color c, Rectangle recorte, Rectangle pagina)
        {
            var fuente = FontAssets.MouseText.Value;
            float alto = fuente.MeasureString("Ag").Y * esc + 3f;
            var palabras = texto.Split(' ');
            var linea = new System.Text.StringBuilder();
            void Volcar(float y)
            {
                if (!(y < recorte.Y + recorte.Height && y + alto > recorte.Y)) return;
                float ancho = fuente.MeasureString(linea.ToString()).X * esc;
                // v6.175 — EL DESBORDE DE LA PALABRA LARGA (la letra:
                // «algunos textos se desbordan»): una línea sin espacios
                // que excede el ancho SE ENCOGE para caber (jamás sale del
                // papel) — la escala vive solo en el dibujado, el reflow
                // de siempre no cambia
                float escV = esc;
                if (ancho > maxAncho && ancho > 0f)
                {
                    escV = esc * (maxAncho / ancho);
                    ancho = maxAncho;
                }
                float xc = pagina.X + (pagina.Width - ancho) * 0.5f;
                Texto(sb, linea.ToString(), new Vector2(xc, y), c, escV);
            }
            foreach (string pal in palabras)
            {
                string prueba = linea.Length == 0 ? pal : linea + " " + pal;
                if (fuente.MeasureString(prueba).X * esc > maxAncho && linea.Length > 0)
                {
                    Volcar(cy);
                    cy += alto;
                    linea.Clear().Append(pal);
                }
                else linea.Clear().Append(prueba);
            }
            if (linea.Length > 0)
            {
                Volcar(cy);
                cy += alto;
            }
            return cy + 3f;
        }

        /// <summary>EL TEXTO DEL LIBRO: contorno BLANCO cálido en cuatro
        /// direcciones y la tinta encima. Misma firma que el DrawBorderString
        /// de vanilla (escala + anclaje) para que los botones no cambien.</summary>
        public static Vector2 Texto(SpriteBatch sb, string texto, Vector2 pos, Color tinta,
            float esc = 1f, float ax = 0f, float ay = 0f)
        {
            var fuente = FontAssets.MouseText.Value;
            if (string.IsNullOrEmpty(texto)) return Vector2.Zero;
            Vector2 origen = fuente.MeasureString(texto) * new Vector2(ax, ay);
            Vector2 p = pos - origen;
            // v6.176 — EL HALO MANSO (la letra: «el color de los textos
            // es demasiado claro en la ventana del pilar y el altar»):
            // el contorno blanco era GRUESO constante — a las escalas
            // menudas de los controles (0.45-0.88) el borde COMÍA el
            // glifo y la tinta leía clara. Ahora el grosor ESCALA con
            // el texto y el blanco pierde un punto de alfa: separa, no
            // lava.
            Color halo = new Color(255, 250, 236) * 0.75f;   // v6.177: un punto más manso (la letra: «demasiado claros»)
            float o = Math.Clamp(esc * 1.15f, 0.55f, 1.1f);
            sb.DrawString(fuente, texto, p + new Vector2(o, 0f), halo, 0f, Vector2.Zero, esc, SpriteEffects.None, 0f);
            sb.DrawString(fuente, texto, p + new Vector2(-o, 0f), halo, 0f, Vector2.Zero, esc, SpriteEffects.None, 0f);
            sb.DrawString(fuente, texto, p + new Vector2(0f, o), halo, 0f, Vector2.Zero, esc, SpriteEffects.None, 0f);
            sb.DrawString(fuente, texto, p + new Vector2(0f, -o), halo, 0f, Vector2.Zero, esc, SpriteEffects.None, 0f);
            sb.DrawString(fuente, texto, p, tinta, 0f, Vector2.Zero, esc, SpriteEffects.None, 0f);
            return fuente.MeasureString(texto) * esc;
        }

        /// <summary>La escala más grande (≤ esc) con la que el texto cabe en maxAncho.</summary>
        public static float EscalaQueCabe(string texto, float esc, float maxAncho)
        {
            if (maxAncho <= 0f) return esc;
            float ancho = FontAssets.MouseText.Value.MeasureString(texto).X * esc;
            if (ancho > maxAncho && ancho > 0f) esc *= maxAncho / ancho;
            return esc;
        }
    }

    /// <summary>El marco cosido del libro (doble línea — el borde de
    /// las páginas vivas de las pestañas nuevas) y la palomita de la casa.</summary>
    internal static class LibroMarco
    {
        /// <summary>v6.176 — LAS TIJERAS del libro: el rasterizador con
        /// recorte para el pase del cuerpo — el papel manda (nada
        /// desborda la página; la letra: «los textos en la ventana del
        /// pilar y el altar se desbordan, especialmente en los
        /// botones»).</summary>
        internal static readonly Microsoft.Xna.Framework.Graphics.RasterizerState Tijeras =
            new Microsoft.Xna.Framework.Graphics.RasterizerState
            { CullMode = Microsoft.Xna.Framework.Graphics.CullMode.None, ScissorTestEnable = true };

        public static void Marco(SpriteBatch sb, Rectangle r, Color c)
        {
            var px = TextureAssets.MagicPixel.Value;
            sb.Draw(px, new Rectangle(r.X, r.Y, r.Width, 2), c);
            sb.Draw(px, new Rectangle(r.X, r.Y + r.Height - 2, r.Width, 2), c);
            sb.Draw(px, new Rectangle(r.X, r.Y, 2, r.Height), c);
            sb.Draw(px, new Rectangle(r.X + r.Width - 2, r.Y, 2, r.Height), c);
        }

        /// <summary>v6.174 — EL SÍMBOLO DEL CHECKLIST (la letra: «en el
        /// checklist debe ponerse el símbolo ese que siempre va en los
        /// checklist si es que está seleccionado»): UNA palomita de
        /// verdad — dos trazos diagonales gruesos, no tres barritas.</summary>
        public static void Palomita(SpriteBatch sb, Rectangle caja, Color tinta)
        {
            var px = TextureAssets.MagicPixel.Value;
            Vector2 c = new Vector2(caja.X + caja.Width / 2f, caja.Y + caja.Height / 2f);
            Vector2 a = c + new Vector2(-0.36f, 0.04f) * caja.Width;
            Vector2 b = c + new Vector2(-0.10f, 0.32f) * caja.Width;
            Vector2 d = c + new Vector2(0.40f, -0.34f) * caja.Width;
            float g = Math.Max(2f, caja.Width * 0.17f);
            Trazo(sb, px, a, b, g, tinta);
            Trazo(sb, px, b, d, g, tinta);
            sb.Draw(px, new Rectangle((int)(b.X - g / 2f), (int)(b.Y - g / 2f), (int)Math.Round(g), (int)Math.Round(g)), tinta);
        }

        private static void Trazo(SpriteBatch sb, Texture2D px, Vector2 a, Vector2 b, float grosor, Color c)
        {
            Vector2 d = b - a;
            float len = d.Length();
            if (len < 0.5f) return;
            sb.Draw(px, a, null, c, d.ToRotation(), new Vector2(0f, 0.5f),
                new Vector2(len, grosor), SpriteEffects.None, 0f);
        }
    }

    // ====================================================================
    //  v6.164 — EL PILAR DEL LIBRO, LA PÁGINA FIJA: la salida y las
    //  invocaciones a la izquierda; la oleada de hambre y la vitrina de
    //  los jefes devorados a la derecha. SIN scroll de página: solo la
    //  vitrina tiene SU rueda.
    //  v6.174 — LA TIPOGRAFÍA Y EL CLIC (la letra: «en el mundo interior,
    //  al usar el pilar los botones no hacen nada […] hay letras demasiado
    //  oscuras y no se ven nada»): toda cadena pasa por LibroLineas.Texto
    //  (contorno BLANCO + los colores del índice); el filtro
    //  IgnoreMouseInterface muere; y LA RUEDA DE LA VITRINA por fin
    //  llega (el Update del libro se la comía antes de que la vitrina
    //  pudiera leerla).
    // ====================================================================
    internal static class PilarDelLibro
    {
        // LA PALETA DEL LIBRO DE INFORMACIÓN, EXACTA
        private static readonly Color Tinta = new(28, 20, 16);
        private static readonly Color TintaVioleta = new(148, 72, 178);
        private static readonly Color TintaOro = new(168, 108, 44);
        private static readonly Color TintaApagado = new(82, 60, 40);   // v6.176: oscurecido (leía claro sobre el papel)
        private static readonly Color TintaRojo = new(140, 40, 40);
        private static readonly Color PapelSombra = new(216, 200, 166);

        internal static Rectangle GeoVolver, GeoInvocar, GeoSellado;
        internal static Rectangle GeoMenos, GeoMas, GeoLanzar;
        internal static readonly List<Rectangle> GeoJefes = new List<Rectangle>();
        internal static int HoverJefe = -1;
        internal static int NivelElegido = 1;

        // EL SCROLL DE LA VITRINA DE DEVORADOS: el desplazamiento INTERNO
        // de la lista — SOLO esa sección se mueve; el resto sigue FIJO.
        private static float _scrollJefes;

        /// <summary>La vitrina dibujada el último frame (el Update del libro
        /// la consulta para NO comerse su rueda).</summary>
        internal static Rectangle GeoVitrina;

        internal static bool CursorEnVitrina()
            => GeoVitrina.Width > 0 && GeoVitrina.Contains(Main.mouseX, Main.mouseY);

        private static void Titulo(SpriteBatch sb, string t, Rectangle pag, float cy, out float cyOut)
        {
            var fuente = FontAssets.MouseText.Value;
            t = t ?? "";
            float esc = LibroLineas.EscalaQueCabe(t, 0.95f, pag.Width - 80f);
            var m = fuente.MeasureString(t) * esc;
            float xT = pag.X + (pag.Width - m.X) * 0.5f;
            LibroLineas.Texto(sb, t, new Vector2(xT, cy), TintaVioleta, esc);
            float yL = cy + m.Y + 3f;
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle((int)(pag.X + 16), (int)yL, (int)Math.Max(4f, xT - 18f - pag.X - 16), 2), TintaOro * 0.55f);
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle((int)(xT + m.X + 18f), (int)yL,
                    (int)Math.Max(4f, pag.Right - 16 - (xT + m.X + 18f)), 2), TintaOro * 0.55f);
            cyOut = cy + m.Y + 12f;
        }

        /// <summary>El botón cosido (texto medido — jamás desborda). Lo que
        /// no cabe en la página queda SIN hitbox.</summary>
        private static float Boton(SpriteBatch sb, Rectangle pag, Rectangle rec, float cy, string t,
            Color cBase, out Rectangle geo, bool clicNuevo, System.Action alClic,
            bool apagado = false)
        {
            string txt = t ?? "";
            // v6.177 — EL BOTÓN GRANDE (el pilar con la misma cura del
            // altar: escala 1.05 · medido con margen de halo · 46 de alto)
            float esc = LibroLineas.EscalaQueCabe(txt, 1.05f, 202f);
            Rectangle b = new Rectangle((int)(pag.X + pag.Width / 2f - 113f), (int)cy, 226, 46);
            bool vivo = cy + 46f <= rec.Bottom;
            geo = vivo ? b : Rectangle.Empty;
            bool hov = vivo && !apagado && b.Contains(Main.mouseX, Main.mouseY);
            sb.Draw(TextureAssets.MagicPixel.Value, b,
                (hov ? cBase : new Color(cBase.R * 2 / 3, cBase.G * 2 / 3, cBase.B * 2 / 3)) * (hov ? 0.30f : 0.15f));
            LibroMarco.Marco(sb, b, apagado ? TintaApagado * 0.5f : (hov ? cBase : cBase * 0.55f));
            // v6.176 — EL BOTÓN JAMÁS REBOSA: re-medida AL MOMENTO DEL
            // TRAZO (la fuente MouseText de vanilla es dinámica — puede
            // crecer entre la medida y el dibujo; medir pegado al trazo
            // cierra la ventana y las tijeras del pase lo blindan).
            esc = LibroLineas.EscalaQueCabe(txt, esc, 202f);
            LibroLineas.Texto(sb, txt, new Vector2(b.Center.X, b.Center.Y),
                apagado ? TintaApagado : Tinta, esc, 0.5f, 0.5f);
            if (hov && clicNuevo) alClic?.Invoke();
            return cy + 54f;
        }

        /// <summary>LA PÁGINA IZQUIERDA: la salida y las invocaciones.</summary>
        public static float Izquierda(SpriteBatch sb, Rectangle pag, Rectangle rec,
            ref float scroll, bool clicNuevo)
        {
            float cy = rec.Y + 6f;                       // página FIJA: sin scroll
            float maxAncho = pag.Width - 36f;
            float Linea(string t, float y, float esc, Color c)
                => LibroLineas.Linea(sb, t ?? "", pag.X + 18f, y, maxAncho, esc, c, rec, pag);

            GeoVolver = Rectangle.Empty; GeoInvocar = Rectangle.Empty; GeoSellado = Rectangle.Empty;
            if (!PrismaInteriorUI.Abierta) return 0f;

            Titulo(sb, Language.GetTextValue("Mods.AethonMod.PrismaInterior.Titulo"), pag, cy, out cy);

            // EL BOTÓN VOLVER (el oro de la salida)
            cy = Boton(sb, pag, rec, cy, Language.GetTextValue(
                "Mods.AethonMod.PrismaInterior.BotonVolver"), TintaOro, out GeoVolver,
                clicNuevo, () => PrismaInteriorUI.ClicEnVolver());
            cy += 8f;

            // LA INVOCACIÓN: Aethon (la llamada de los dos fragmentos)
            bool yaVive = false;
            try
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n != null && n.active && (n.type ==
                        ModContent.NPCType<global::AethonMod.Content.NPCs.AethonSellado>() ||
                        n.type == ModContent.NPCType
                            <global::AethonMod.Content.NPCs.AethonBoss>()))
                    { yaVive = true; break; }
                }
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("LibroDelEternoUI", exRegistro); }
            cy = Boton(sb, pag, rec, cy, yaVive
                ? Language.GetTextValue("Mods.AethonMod.PrismaInterior.YaVive")
                : Language.GetTextValue("Mods.AethonMod.PrismaInterior.BotonInvocar"),
                TintaVioleta, out GeoInvocar, clicNuevo,
                () => { if (!yaVive) PrismaInteriorUI.ClicEnInvocar(); }, yaVive);
            cy = Linea(Language.GetTextValue("Mods.AethonMod.PrismaInterior.InvocarPista"), cy, 0.88f, TintaApagado);

            // EL SELLADO (la vía del dios completo)
            cy += 8f;
            Titulo(sb, Language.GetTextValue("Mods.AethonMod.PrismaInterior.SelladoTitulo"), pag, cy, out cy);
            cy = Boton(sb, pag, rec, cy, Language.GetTextValue("Mods.AethonMod.PrismaInterior.BotonSellado"),
                TintaRojo, out GeoSellado, clicNuevo, () => PrismaInteriorUI.ClicEnSellado());
            cy = Linea(Language.GetTextValue("Mods.AethonMod.PrismaInterior.SelladoPista"), cy, 0.88f, TintaApagado);
            return 0f;
        }

        /// <summary>LA PÁGINA DERECHA: la oleada y los devorados.</summary>
        public static float Derecha(SpriteBatch sb, Rectangle pag, Rectangle rec,
            ref float scroll, bool clicNuevo)
        {
            var fuente = FontAssets.MouseText.Value;
            float cy = rec.Y + 6f;
            float xizq = pag.X + 18f;
            float maxAncho = pag.Width - 36f;
            float Linea(string t, float y, float esc, Color c)
                => LibroLineas.Linea(sb, t ?? "", xizq, y, maxAncho, esc, c, rec, pag);

            GeoMenos = Rectangle.Empty; GeoMas = Rectangle.Empty; GeoLanzar = Rectangle.Empty;
            GeoVitrina = Rectangle.Empty;
            GeoJefes.Clear();
            HoverJefe = -1;
            if (!PrismaInteriorUI.Abierta) return 0f;

            var sp = Main.LocalPlayer.GetModPlayer<Players.ShardPlayer>();
            int maxNivel = sp != null ? Math.Max(1, Math.Min(10, sp.FuriaNivel)) : 1;
            if (NivelElegido > maxNivel) NivelElegido = maxNivel;
            if (NivelElegido < 1) NivelElegido = 1;

            // ── LA OLEADA DE HAMBRE ──
            Titulo(sb, Language.GetTextValue("Mods.AethonMod.PrismaInterior.OleadaTitulo"), pag, cy, out cy);

            // EL SELECTOR [-] NIVEL N [+] (texto medido dentro)
            Rectangle sel = new Rectangle((int)(pag.X + pag.Width / 2f - 130f), (int)cy, 260, 32);
            GeoMenos = new Rectangle(sel.X, sel.Y, 36, 32);
            GeoMas = new Rectangle(sel.Right - 36, sel.Y, 36, 32);
            bool bloqueado = maxNivel <= 1;
            bool hovMenos = !bloqueado && GeoMenos.Contains(Main.mouseX, Main.mouseY);
            bool hovMas = !bloqueado && NivelElegido < maxNivel && GeoMas.Contains(Main.mouseX, Main.mouseY);
            sb.Draw(TextureAssets.MagicPixel.Value, sel, PapelSombra * 0.5f);
            LibroMarco.Marco(sb, sel, TintaOro * 0.4f);
            LibroLineas.Texto(sb, "-", new Vector2(GeoMenos.Center.X, GeoMenos.Center.Y),
                hovMenos ? TintaOro : TintaApagado, 1.15f, 0.5f, 0.5f);
            LibroLineas.Texto(sb, "+", new Vector2(GeoMas.Center.X, GeoMas.Center.Y),
                hovMas ? TintaOro : TintaApagado, 1.15f, 0.5f, 0.5f);
            string nivelTxt = Language.GetTextValue(
                "Mods.AethonMod.PrismaInterior.OleadaNivel", NivelElegido, maxNivel);
            float escN = LibroLineas.EscalaQueCabe(nivelTxt, 1.02f, 172f);
            LibroLineas.Texto(sb, nivelTxt, new Vector2(sel.Center.X, sel.Center.Y),
                bloqueado ? TintaApagado : Tinta, escN, 0.5f, 0.5f);
            if (hovMenos && clicNuevo) { NivelElegido--; Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick); }
            if (hovMas && clicNuevo) { NivelElegido++; Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick); }
            cy += 40f;
            cy = Linea(Language.GetTextValue("Mods.AethonMod.PrismaInterior.OleadaPista", maxNivel),
                cy, 0.88f, TintaApagado);

            // EL BOTÓN LANZAR (la oleada del festín)
            bool festinVivo = false;
            try { festinVivo = Systems.GrimorioFuriaSistema.Activo; } catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("LibroDelEternoUI", exRegistro); }
            cy = Boton(sb, pag, rec, cy + 2f, festinVivo
                ? Language.GetTextValue("Mods.AethonMod.PrismaInterior.OleadaCorre")
                : Language.GetTextValue("Mods.AethonMod.PrismaInterior.OleadaLanzar", NivelElegido),
                TintaRojo, out GeoLanzar, clicNuevo,
                () => { if (!festinVivo) PrismaInteriorUI.ClicEnLanzar(NivelElegido); }, festinVivo);
            cy += 8f;

            // ── LA VITRINA DE LOS JEFES DEVORADOS (la arena) ──
            Titulo(sb, Language.GetTextValue("Mods.AethonMod.PrismaInterior.JefesTitulo"), pag, cy, out cy);
            var devorados = sp != null ? sp.JefesDevorados : null;
            int nJefes = devorados != null ? devorados.Count : 0;
            // LA CUENTA TOTAL (encima de la vitrina: antes nacía BAJO la página)
            if (nJefes > 0)
                cy = Linea(Language.GetTextValue("Mods.AethonMod.PrismaInterior.JefesCuenta", nJefes), cy, 0.88f, TintaApagado);
            if (nJefes == 0)
            {
                cy = Linea(Language.GetTextValue("Mods.AethonMod.PrismaInterior.JefesVacio"), cy, 0.8f, TintaApagado);
            }
            else
            {
                // LA VITRINA: desde cy hasta el borde útil de la página.
                // v6.175 — EL DESBORDE VERTICAL (la letra: «algunos textos
                // se desbordan»): la vitrina NUNCA pasa del borde inferior
                // de la página — se ENCOGE a lo que quede (antes forzaba
                // 60 px y rebosaba el papel)
                int topVitrina = (int)cy;
                int altoVitrina = (pag.Y + pag.Height - 14) - topVitrina;
                if (altoVitrina < 60)
                {
                    // no cabe: se dibuja la fila inicial a nivel de texto y
                    // el recorte de filas hace el resto (nada rebosa)
                    altoVitrina = Math.Max(0, altoVitrina);
                }
                Rectangle vitrina = new Rectangle((int)xizq - 4, topVitrina, (int)maxAncho + 8, altoVitrina);
                GeoVitrina = vitrina;

                // LA RUEDA INTERNA: el Update del libro YA no consume la
                // rueda sobre la vitrina — la lee AQUÍ.
                float altoFila = 34f;
                float altoTotal = nJefes * altoFila;
                float maxScroll = Math.Max(0f, altoTotal - altoVitrina);
                int deltaRueda = Terraria.GameInput.PlayerInput.ScrollWheelDelta;
                if (vitrina.Contains(Main.mouseX, Main.mouseY) && deltaRueda != 0)
                {
                    _scrollJefes = MathHelper.Clamp(_scrollJefes - deltaRueda * 0.45f, 0f, maxScroll);
                    Terraria.GameInput.PlayerInput.ScrollWheelDelta = 0;   // la rueda es de la vitrina
                }
                if (_scrollJefes > maxScroll) _scrollJefes = maxScroll;

                // EL PAPEL DE LA VITRINA
                sb.Draw(TextureAssets.MagicPixel.Value, vitrina, PapelSombra * 0.45f);
                LibroMarco.Marco(sb, vitrina, TintaOro * 0.45f);

                // LAS FILAS (solo las que viven en la vitrina)
                for (int k = 0; k < nJefes; k++)
                {
                    float yFila = topVitrina + 4 + k * altoFila - _scrollJefes;
                    if (yFila + altoFila < topVitrina || yFila > vitrina.Bottom - 4) continue;
                    int tipoJefe = devorados[k];
                    Rectangle fila = new Rectangle((int)xizq, (int)yFila, (int)maxAncho, 30);
                    bool hovF = fila.Contains(Main.mouseX, Main.mouseY) &&
                        vitrina.Contains(Main.mouseX, Main.mouseY);
                    if (hovF) HoverJefe = k;
                    sb.Draw(TextureAssets.MagicPixel.Value, fila,
                        (hovF ? TintaVioleta : PapelSombra) * (hovF ? 0.30f : 0.55f));
                    LibroMarco.Marco(sb, fila, hovF ? TintaVioleta : TintaOro * 0.35f);
                    int kills = sp.KillsInteriorDe(tipoJefe);
                    // v6.177 — SIN TOPE (la letra: «que este nivel sea
                    // infitino»): la estatura del jefe es la REAL — 1+kills
                    int nivelPoder = 1 + kills;
                    float mult = Globals.JefeInteriorGlobalNPC.Multiplicador(kills);
                    string nombreJefe = "???";
                    try { nombreJefe = Terraria.Lang.GetNPCNameValue(tipoJefe); } catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("LibroDelEternoUI", exRegistro); }
                    if (string.IsNullOrEmpty(nombreJefe)) nombreJefe = "???";
                    // EL NOMBRE: medido para su mitad; EL PODER: para la suya.
                    string poder = Language.GetTextValue(
                        "Mods.AethonMod.PrismaInterior.JefePoder", nivelPoder, (int)(mult * 100f));
                    float escP = LibroLineas.EscalaQueCabe(poder, 0.86f, 96f);
                    float anchoNom = maxAncho - 104f;
                    float escNom = LibroLineas.EscalaQueCabe(nombreJefe, 0.96f, anchoNom);
                    LibroLineas.Texto(sb, nombreJefe, new Vector2(fila.X + 10f, fila.Center.Y),
                        hovF ? TintaVioleta : Tinta, escNom, 0f, 0.5f);
                    LibroLineas.Texto(sb, poder, new Vector2(fila.Right - 10f, fila.Center.Y),
                        nivelPoder >= 8 ? TintaRojo : TintaOro, escP, 1f, 0.5f);
                    if (hovF && clicNuevo) PrismaInteriorUI.ClicEnJefe(tipoJefe);
                    GeoJefes.Add(fila);
                }

                // LA BARRA DE LA VITRINA (la línea del scroll)
                if (maxScroll > 0f)
                {
                    float proporcion = altoVitrina / altoTotal;
                    int altoBarra = Math.Max(20, (int)(altoVitrina * proporcion));
                    int yBarra = vitrina.Y + 2 + (int)((_scrollJefes / maxScroll) *
                        (altoVitrina - altoBarra - 4));
                    sb.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle(vitrina.Right - 8, vitrina.Y + 2, 5, altoVitrina - 4),
                        PapelSombra * 0.9f);
                    sb.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle(vitrina.Right - 8, yBarra, 5, altoBarra), TintaOro * 0.7f);
                }
            }
            return 0f;
        }
    }
}
