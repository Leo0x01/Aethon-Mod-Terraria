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

        // EL ICONO: la geometría vanilla del botón de logros (el
        // decompile: 20 + 560·inventoryScale + num, 20 + num2)
        internal static Rectangle GeoIcono;

        public override void Load()
        {
            _iface = new UserInterface();
        }

        public static void Abrir()
        {
            if (_estado == null) _estado = new LibroEstado();
            _estado.Visible = true;
            _iface.SetState(_estado);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuOpen);
        }

        public static void Cerrar()
        {
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

            // EL LIBRO ABIERTO
            layers.Insert(idx + 1, new LegacyGameInterfaceLayer("AethonMod: LibroDelEterno",
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
                catch { }

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
            catch { }
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
        private static readonly Color Tinta = new(72, 44, 24);
        private static readonly Color TintaTitulo = new(128, 52, 158);
        private static readonly Color TintaCap = new(140, 84, 34);
        private static readonly Color Papel = new(232, 218, 186);
        private static readonly Color PapelSombra = new(206, 188, 150);
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
            bool cursorEnLibro = GeoPagIzq.Contains(Main.mouseX, Main.mouseY) ||
                                 GeoPagDer.Contains(Main.mouseX, Main.mouseY);
            if (cursorEnLibro)
                Terraria.GameInput.PlayerInput.LockVanillaMouseScroll("AethonMod/LibroEterno");

            // v6.144 — EL SCROLL INDEPENDIENTE: la rueda SOLO mueve LA
            // PÁGINA bajo el cursor (fuera de ambas: nada).
            int delta = Terraria.GameInput.PlayerInput.ScrollWheelDelta;
            if (delta != 0)
            {
                Rectangle mitIzq = new Rectangle(0, 0, Main.screenWidth / 2, Main.screenHeight);
                if (GeoPagIzq.Contains(Main.mouseX, Main.mouseY))
                    _scrollIzq = MathHelper.Clamp(_scrollIzq - delta * 0.45f, 0f, Math.Max(0f, _maxIzq));
                else if (GeoPagDer.Contains(Main.mouseX, Main.mouseY))
                    _scrollDer = MathHelper.Clamp(_scrollDer - delta * 0.45f, 0f, Math.Max(0f, _maxDer));
                Terraria.GameInput.PlayerInput.ScrollWheelDelta = 0;   // la rueda es del libro
            }

            if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape))
                LibroDelEternoUI.Cerrar();

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

                // LOS RÓTULOS — arriba de cada página (Info · Lore),
                // grabados en la franja superior del papel
                try
                {
                    string rotI = Language.GetTextValue("Mods.AethonMod.LibroEterno.RotuloIzq");
                    string rotD = Language.GetTextValue("Mods.AethonMod.LibroEterno.RotuloDer");
                    if (!string.IsNullOrEmpty(rotI) && !rotI.StartsWith("Mods."))
                        Utils.DrawBorderString(sb, rotI, new Vector2(GeoPagIzq.X + 8f, GeoPagIzq.Y + 2f),
                            TintaCap * 0.95f, 0.78f);
                    if (!string.IsNullOrEmpty(rotD) && !rotD.StartsWith("Mods."))
                        Utils.DrawBorderString(sb, rotD, new Vector2(GeoPagDer.X + 8f, GeoPagDer.Y + 2f),
                            TintaCap * 0.95f, 0.78f);
                }
                catch { }

                // el cuerpo arranca DEBAJO del rótulo de la página —
                // nunca debajo del titular (que ya vive en el cuero)
                float cy = GeoPagIzq.Y + 26f;

                // ============ EL CUERPO: DOS COLUMNAS CON RECORTE ============
                int cuerpoY = (int)cy;
                int cuerpoH = h - (int)cy - y - 30;
                Rectangle recIzq = new Rectangle(pagIzq.X + 6, cuerpoY, pagIzq.Width - 12, cuerpoH);
                Rectangle recDer = new Rectangle(pagDer.X + 6, cuerpoY, pagDer.Width - 12, cuerpoH);

                // LAS SECCIONES: IZQ = LAS MECÁNICAS COMPLETAS (v6.147 —
                // las 4 de la derecha se mudan aquí, la página es una y
                // scrollea) · DER = EL COMPENDIO DEL ETERNO (el lore)
                string[][][] columnas =
                {
                    new string[][]
                    {
                        new[] { "SecAltar", "Altar1", "Altar2", "Altar3", "Altar4", "Altar5" },
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
                        // v6.147 — LA PÁGINA DERECHA ES EL COMPENDIO DEL
                        // ETERNO (la letra: «el lore es el documento
                        // Aethon_El_Compendio_del_Eterno…, ya lo puedes
                        // integrar al juego»): las CUATRO PARTES del
                        // documento canónico viven aquí — el prólogo con
                        // su pregunta, el Devorador de Finales, el
                        // Sistema del Pacto, el Ciclo del Portador y La
                        // Respuesta. Las mecánicas (despertar·ataques·
                        // forma·auras) se mudan a la página IZQUIERDA
                        // con sus hermanas de siempre.
                        new[] { "SecLore", "LoreEpigrafe0", "LoreIntro" },
                        new[] { "LoreSecI", "LoreEpigrafe1", "LoreI_Loquedevora",
                                "LoreI_LaCuarentenadeTer", "LoreI_ElSellado" },
                        new[] { "LoreSecII", "LoreEpigrafe2", "LoreII_ElNivel",
                                "LoreII_ElDespertar", "LoreII_Doshambresdistinta" },
                        new[] { "LoreSecIII", "LoreEpigrafe3", "LoreIII_LaLlegada",
                                "LoreIII_ElGrimoriodelEter", "LoreIII_ElAltar" },
                        new[] { "LoreSecIV", "LoreIV_Laimposibilidaddel",
                                "LoreIV_Laentidadinmortal", "LoreIV_Elaburrimientoyla" },
                        // v6.149 — LORE V: EL CANON ASSENTADO (la letra:
                        // «con el nuevo lore que te di, integralo al mod …
                        // e informar de ello en el libro de informacion»):
                        // la Ira, el Pilar que cuenta, el Sellado y sus
                        // funerales, los compañeros, las sombras y la
                        // respuesta final — las piezas nuevas del Compendio.
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
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None,
                    RasterizerState.CullNone, null, Main.UIScaleMatrix);

                _maxIzq = DibujarColumna(sb, columnas[0], pagIzq, recIzq, ref _scrollIzq, true);
                _maxDer = DibujarColumna(sb, columnas[1], pagDer, recDer, ref _scrollDer, false);

                sb.End();
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    Main.DefaultSamplerState, DepthStencilState.None,
                    Main.Rasterizer, null, Main.Transform);

                // ============ v6.144 — EL OJO DE ABAJO-DERECHA MURIÓ
                // (la letra: «el ojo de abajo a la derecha lo puedes
                // eliminar») — la página derecha es del LORE: el ojo
                // ya no vive aquí. ============

                // ============ LA X ============
                var rectX = new Rectangle(x + w - 42, y + 8, 28, 28);
                bool hoverX = rectX.Contains(Main.mouseX, Main.mouseY);
                sb.Draw(_pixel, rectX, (hoverX ? new Color(220, 90, 90) : new Color(96, 74, 52)) * 0.4f);
                Utils.DrawBorderString(sb, "X", new Vector2(rectX.Center.X, rectX.Center.Y),
                    hoverX ? new Color(220, 90, 90) : Tinta, 1f, 0.5f, 0.5f);
                if (hoverX && Main.mouseLeft && Main.mouseLeftRelease)
                    LibroDelEternoUI.Cerrar();
            }
            catch { }
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

            foreach (var sec in secciones)
            {
                cy += 12f;
                string titulo = T(sec[0]);
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

                for (int i = 1; i < sec.Length; i++)
                    cy = LineaLibro(sb, T(sec[i]), xizq, cy, maxAncho,
                        Tinta, recorte, pagina);
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
                float ancho = fuente.MeasureString(linea.ToString()).X * 0.92f;
                float xc = pagina.X + (pagina.Width - ancho) * 0.5f;
                Utils.DrawBorderString(sb, linea.ToString(), new Vector2(xc, y), c, 0.92f);
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
            catch { }
        }
    }
}
