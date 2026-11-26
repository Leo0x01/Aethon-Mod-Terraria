using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace AethonMod.Content.UI
{
    // ======================================================================
    //  SelectorFlotante — v6.168 — LA VENTANA FLOTANTE DE SELECCIÓN: la
    //  lista completa de candidatos (las ofrendas del altar · las armas
    //  consagradas del trío) en una cuadrícula de 6 columnas, anclada
    //  DEBAJO del control que la abrió, con SU rueda. El mismo motor para
    //  las dos bandejas: la ofrenda del altar (ranura −2) y las ranuras
    //  del trío (0..2). Clic en un icono = elegir; clic fuera o ESC = se
    //  cierra y NADA cambia.
    //
    //  v6.174 — REESCRITA (la letra: «en el altar tampoco funcionan los
    //  botones, funcionaron la primera vez pero ahora no» + «a veces
    //  pone 2 y al poner la tercera las dos iniciales se desequipan y
    //  solo queda la tercera; revisa bien todo el código de esos
    //  botones»). TRES leyes nuevas:
    //   · SU PROPIO RELOJ: la ventana lleva `_edad` (los frames que
    //     lleva VIVA). El clic de su nacimiento no cuenta y el «clic
    //     fuera» solo vale desde el segundo frame. NADA de
    //     Main.GameUpdateCount (que vuelve a 0 en cada carga de mundo y
    //     dejaba todos los botones muertos).
    //   · EL DUEÑO ÚNICO DEL CLIC: mientras la ventana vive, las páginas
    //     del libro no procesan NINGÚN clic (lo decide el libro). El
    //     fantasma que vaciaba las ranuras al elegir ya no tiene dónde
    //     caer.
    //   · SIN IgnoreMouseInterface: ese filtro transitorio de vanilla
    //     se encendía con el libro abierto y mataba el clic limpio.
    //  LA TIPOGRAFÍA: toda cadena pasa por LibroLineas.Texto — contorno
    //  BLANCO cálido y los colores del libro de información.
    // ======================================================================
    internal static class SelectorFlotante
    {
        // LA PALETA DEL LIBRO (exacta — la de LibroEstado)
        private static readonly Color Papel = new(216, 200, 166);
        private static readonly Color PapelSombra = new(190, 172, 134);
        private static readonly Color Tinta = new(28, 20, 16);
        private static readonly Color TintaVioleta = new(148, 72, 178);
        private static readonly Color TintaOro = new(168, 108, 44);
        private static readonly Color TintaApagado = new(118, 96, 70);

        // LA CUADRÍCULA: 6 columnas de 44 px, iconos de 32.
        private const int Columnas = 6;
        private const int Celda = 44;
        private const int FilasVisibles = 5;         // por defecto: 30 candidatos a la vista
        private const int AltoTitulo = 44;   // v6.177: el título GRANDE (la letra: «un texto muy pequeño y no se puede leer»)
        private const int AltoPie = 22;               // la pista de la rueda (solo si hay scroll)

        // EL ESTADO (cliente puro): el ancla (el control que la abrió),
        // la lista viva y SU scroll propio (en FILAS enteras).
        private static Rectangle _ancla = Rectangle.Empty;
        private static int _filas;
        private static int _filasVis = FilasVisibles;
        private static int _scroll;
        private static readonly List<Item> _lista = new();
        private static bool _paraTrio;
        private static string _titulo = "";

        /// <summary>LA RANURA DESTINO de la elección (−2 = el hueco de la
        /// ofrenda · 0..2 = las ranuras del trío · int.MinValue = sin
        /// destino). Vive AQUÍ desde el nacimiento de la ventana.</summary>
        private static int _ranuraDestino = int.MinValue;

        /// <summary>LOS FRAMES DE VIDA PROPIOS (jamás Main.GameUpdateCount):
        /// 0 = el frame en que nació — el clic que la abrió no cuenta.</summary>
        private static int _edad;

        /// <summary>El flanco de la tecla ESC: mientras siga apretada tras
        /// cerrar ESTA ventana, no baja al libro (se suelta y se vuelve a
        /// pulsar — un ESC no se come las dos ventanas).</summary>
        private static bool _escConsumido;

        /// <summary>¿Está abierta la ventanita?</summary>
        internal static bool Abierta => _ancla.Width > 0;

        /// <summary>El rectángulo VIVO de la ventana (siempre calculado
        /// del ancla y de la lista — el libro lo consulta en SU Update para
        /// no comerse la rueda cuando el cursor vive aquí).</summary>
        internal static Rectangle UltimoRect => Abierta ? CalcularRect(_ancla) : Rectangle.Empty;

        /// <summary>¿La tecla ESC ya gastó su pulsación en esta ventana?</summary>
        internal static bool EscConsumido => _escConsumido;

        /// <summary>ABRIR: anclada bajo el control, con los candidatos
        /// VIVOS. `filasVisibles` lo elige quien abre (el trío la abre
        /// corta para que quepa bajo su albedrío).</summary>
        internal static void Abrir(Rectangle ancla, bool paraTrio, string titulo,
            int ranuraDestino = int.MinValue, int filasVisibles = FilasVisibles)
        {
            _ancla = ancla;
            _paraTrio = paraTrio;
            _titulo = titulo ?? "";
            _scroll = 0;
            _ranuraDestino = ranuraDestino;
            _filasVis = Math.Max(1, Math.Min(FilasVisibles, filasVisibles));
            _edad = 0;
            Refrescar();
            Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
        }

        /// <summary>REFRESCAR la lista (el inventario vive: cada frame la
        /// ventana muestra lo que HAY, no una foto vieja).</summary>
        internal static void Refrescar()
        {
            _lista.Clear();
            _lista.AddRange(AltarOfrendasUI.CosecharCandidatos(_paraTrio));
            _filas = (_lista.Count + Columnas - 1) / Columnas;
            _scroll = Math.Clamp(_scroll, 0, Math.Max(0, _filas - _filasVis));
        }

        /// <summary>CERRAR (elegir o clic fuera). Nada queda abierto.</summary>
        internal static void Cerrar()
        {
            _ancla = Rectangle.Empty;
            _edad = 0;
            _scroll = 0;
            _filasVis = FilasVisibles;
            _lista.Clear();
        }

        /// <summary>CERRAR POR ESC: marca la pulsación como gastada (el
        /// libro no se cierra con la misma tecla aún apretada).</summary>
        internal static void CerrarPorEsc()
        {
            Cerrar();
            _escConsumido = true;
        }

        /// <summary>El flanco de la tecla ESC (lo llama el Update del libro
        /// cada frame): al soltar la tecla, la pulsación vuelve a valer.</summary>
        internal static void FilarEsc(bool escVivo)
        {
            if (!escVivo) _escConsumido = false;
        }

        /// <summary>EL CICLO DE VIDA: al salir del mundo y al descargar el
        /// mod nada queda abierto (la ventanita zombi) ni referencias a
        /// ítems de un jugador que ya no existe.</summary>
        internal static void Descargar()
        {
            Cerrar();
            _titulo = "";
            _ranuraDestino = int.MinValue;
            _escConsumido = false;
        }

        /// <summary>LA GEOMETRÍA: nace DEBAJO del ancla; si no cabe se
        /// voltea ARRIBA; si se sale por los lados, se desliza hacia
        /// dentro. El alto nace de las filas que hay (ventana corta para
        /// listas cortas — nunca un hueco vacío de cinco filas).</summary>
        private static Rectangle CalcularRect(Rectangle ancla)
        {
            int w = Columnas * Celda + 20;
            int filasVis = Math.Min(Math.Max(_filas, 1), _filasVis);
            int h = AltoTitulo + filasVis * Celda + 12 + (_filas > _filasVis ? AltoPie : 0);
            int x = ancla.Center.X - w / 2;
            int y = ancla.Bottom + 6;
            if (y + h > Main.screenHeight - 8) y = ancla.Y - h - 6;
            if (y < 8) y = 8;
            x = Math.Clamp(x, 8, Math.Max(8, Main.screenWidth - w - 8));
            return new Rectangle(x, y, w, h);
        }

        // ================================================================
        //  EL DIBUJO + LA RUEDA + EL CLIC — todo en un pase (el patrón de
        //  la casa: la geometría y el flanco viven juntos).
        // ================================================================
        public static void Dibujar(SpriteBatch sb, bool clicNuevo, bool clicDerNuevo, bool ruedaNueva)
        {
            if (!Abierta) return;
            bool recienNacida = _edad == 0;
            try
            {
                Refrescar();   // la lista vive

                Rectangle r = UltimoRect;
                var px = TextureAssets.MagicPixel.Value;
                var fuente = FontAssets.MouseText.Value;
                bool dentro = r.Contains(Main.mouseX, Main.mouseY);

                // EL PAPEL SÓLIDO (nada de transparencias: la ventana lee
                // encima de la página sin fundirse con ella).
                sb.Draw(px, new Rectangle(r.X + 3, r.Y + 3, r.Width, r.Height), PapelSombra * 0.6f);
                sb.Draw(px, r, Papel);
                LibroMarco.Marco(sb, r, TintaOro * 0.8f);

                // EL TÍTULO (la letra de cada bandeja — medido para caber)
                // v6.177 — EL TÍTULO SE LEE (la letra: «esta tiene un
                // texto muy pequeño y no se puede leer»): escala base 1.0
                float escT = LibroLineas.EscalaQueCabe(_titulo, 1.0f, r.Width - 24f);
                LibroLineas.Texto(sb, _titulo, new Vector2(r.Center.X, r.Y + AltoTitulo * 0.5f),
                    TintaVioleta, escT, 0.5f, 0.5f);
                // el filete del título (el estilo del libro)
                sb.Draw(px, new Rectangle(r.X + 10, r.Y + AltoTitulo - 6, r.Width - 20, 2),
                    TintaOro * 0.55f);

                // ============================================================
                //  LA RUEDA ES DE LA VENTANA: el máximo scroll nace de la
                //  lista VIVA; una muesca = una fila; SOLO se come si el
                //  cursor está DENTRO (la página del libro no se mueve).
                // ============================================================
                int filasVis = Math.Min(Math.Max(_filas, 1), _filasVis);
                int maxScroll = Math.Max(0, _filas - _filasVis);
                if (ruedaNueva && dentro)
                {
                    int delta = Terraria.GameInput.PlayerInput.ScrollWheelDelta;
                    if (delta != 0)
                    {
                        _scroll = Math.Clamp(_scroll + (delta > 0 ? -1 : 1), 0, maxScroll);
                        Terraria.GameInput.PlayerInput.ScrollWheelDelta = 0;   // la rueda es NUESTRA
                    }
                }
                if (dentro) Main.LocalPlayer.mouseInterface = true;

                // LA VITRINA (el papel donde viven los iconos)
                int topVitrina = r.Y + AltoTitulo;
                int altoVitrina = filasVis * Celda;
                Rectangle vitrina = new Rectangle(r.X + 8, topVitrina, r.Width - 16, altoVitrina);
                sb.Draw(px, vitrina, PapelSombra * 0.45f);
                LibroMarco.Marco(sb, vitrina, TintaOro * 0.3f);

                // LA BARRA DE PROGRESO (la línea del scroll: oro apagado)
                if (maxScroll > 0)
                {
                    float proporcion = altoVitrina / (float)(_filas * Celda);
                    int altoBarra = Math.Max(18, (int)(altoVitrina * proporcion));
                    int yBarra = vitrina.Y + 2 + (int)(_scroll / (float)maxScroll *
                        (altoVitrina - altoBarra - 4));
                    sb.Draw(px, new Rectangle(vitrina.Right - 6, vitrina.Y + 2, 4, altoVitrina - 4),
                        PapelSombra * 0.9f);
                    sb.Draw(px, new Rectangle(vitrina.Right - 6, yBarra, 4, altoBarra), TintaOro * 0.7f);
                }

                // ============================================================
                //  LA CUADRÍCULA CON RECORTE: la fila que no vive en la
                //  vitrina no se dibuja (la ventana es una vitrina, no una
                //  página). El icono bajo el cursor se elige con UN clic.
                // ============================================================
                int hover = -1;
                int total = _lista.Count;
                for (int i = 0; i < total; i++)
                {
                    int col = i % Columnas;
                    int fila = i / Columnas;
                    int yFila = vitrina.Y + 2 + fila * Celda - _scroll * Celda;
                    if (yFila + Celda < vitrina.Y || yFila > vitrina.Bottom) continue;
                    Rectangle celda = new Rectangle(vitrina.X + 3 + col * Celda, yFila, Celda - 4, Celda - 4);
                    bool hov = celda.Contains(Main.mouseX, Main.mouseY) && dentro;
                    if (hov) hover = i;
                    Item it = _lista[i];
                    if (it == null || it.IsAir) continue;
                    sb.Draw(px, celda, hov ? PapelSombra * 1.15f : PapelSombra * 0.7f);
                    LibroMarco.Marco(sb, celda, hov ? TintaVioleta : TintaOro * 0.35f);
                    Texture2D tex = TextureAssets.Item[it.type].Value;
                    float esc = Math.Min(32f / tex.Width, 32f / tex.Height);
                    sb.Draw(tex, celda.Center.ToVector2(), null, Color.White, 0f,
                        tex.Size() * 0.5f, esc, SpriteEffects.None, 0f);
                    if (it.stack > 1)
                        LibroLineas.Texto(sb, it.stack.ToString(),
                            new Vector2(celda.Right - 3f, celda.Bottom - 5f), Tinta, 0.78f, 1f, 0.85f);
                }

                // EL NOMBRE DEL ÍTEM bajo el cursor — CON SU PLACA DE PAPEL
                // (la tinta del libro SIEMPRE viaja sobre su papel: se lee
                // sobre el mundo, sobre el libro, sobre cualquier fondo).
                if (hover >= 0)
                {
                    string nombre = _lista[hover].Name;
                    float escN = LibroLineas.EscalaQueCabe(nombre, 0.92f, 260f);
                    var mN = fuente.MeasureString(nombre) * escN;
                    Rectangle placa = new Rectangle(
                        (int)(r.Center.X - mN.X * 0.5f - 10f),
                        (int)(vitrina.Y - 10f - mN.Y - 7f),
                        (int)(mN.X + 20f), (int)(mN.Y + 12f));
                    placa.X = Math.Clamp(placa.X, 6, Math.Max(6, Main.screenWidth - placa.Width - 6));
                    if (placa.Y < 6) placa.Y = vitrina.Bottom + 8;
                    sb.Draw(px, placa, Papel);
                    sb.Draw(px, new Rectangle(placa.X + 2, placa.Y + 2, placa.Width - 4, placa.Height - 4),
                        PapelSombra * 0.35f);
                    LibroMarco.Marco(sb, placa, TintaOro * 0.8f);
                    LibroLineas.Texto(sb, nombre, new Vector2(placa.Center.X, placa.Y + 6f),
                        Tinta, escN, 0.5f, 0f);
                }

                // LA LISTA VACÍA (su aviso, centrado en la vitrina)
                if (total == 0)
                {
                    string vacio = Language.GetTextValue(_paraTrio
                        ? "Mods.AethonMod.Items.TrioSinConsagradas"
                        : "Mods.AethonMod.Items.BandejaVacia");
                    float escV = LibroLineas.EscalaQueCabe(vacio, 0.9f, r.Width - 30f);
                    LibroLineas.Texto(sb, vacio, new Vector2(r.Center.X, vitrina.Center.Y),
                        TintaApagado, escV, 0.5f, 0.5f);
                }

                // EL PIE: la pista de la rueda (solo cuando hay algo que recorrer)
                if (_filas > _filasVis)
                {
                    string pista = Language.GetTextValue("Mods.AethonMod.Altar.SelectorRueda");
                    float escP = LibroLineas.EscalaQueCabe(pista, 0.78f, r.Width - 24f);
                    LibroLineas.Texto(sb, pista, new Vector2(r.Center.X, r.Bottom - 4f),
                        TintaApagado, escP, 0.5f, 1f);
                }

                // ============================================================
                //  LOS CLICS — NUNCA los del frame de nacimiento: la ventana
                //  nace DENTRO del clic que la abrió; si contara, el mismo
                //  botón apretado la cerraría o elegiría lo que quedó
                //  debajo. Con el flanco limpio del libro, el frame
                //  siguiente no trae NINGÚN clic nuevo (no hace falta
                //  ningún «frame de gracia» con reloj ajeno).
                // ============================================================
                if (recienNacida) return;

                if (clicNuevo && hover >= 0)
                {
                    // LA ELECCIÓN: el índice de la lista VIVA + LA RANURA
                    // DESTINO. La ventana MUERE primero: lo que quede
                    // debajo no puede recibir este clic.
                    int destino = _ranuraDestino;
                    int indice = hover;
                    Cerrar();
                    AltarOfrendasUI.ClicEnCandidato(indice, destino);
                    return;
                }
                if ((clicNuevo || clicDerNuevo) && !dentro) Cerrar();
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("SelectorFlotante", exRegistro); }
            finally
            {
                // LA EDAD AVANZA AL FINAL DEL PASE: en el frame de
                // nacimiento valía 0 mientras se dibujó.
                if (Abierta) _edad++;
            }
        }
    }
}
