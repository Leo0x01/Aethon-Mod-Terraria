using System;
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
    //  SelectorFlotante — v6.168 — LA VENTANA FLOTANTE DE SELECCIÓN (la
    //  letra: «creo que seria mejor que para seleccionar las ofrendas y
    //  las armas en el slot de armas, se haga con una ventana flotante,
    //  una pequeña ventana que muestre los items disponibles para slot
    //  de ofrenda y para slot de fusion respectivamente»).
    //
    //  LA AUTOPSIA DE LA v6.167: la bandeja estaba INCROSTRADA en la
    //  página del libro (una sola fila de 8 iconos) — con más de 8
    //  ofrendas el resto ni se veía («+N» apagado), y al abrirse
    //  EMPUJABA el contenido de la página hacia abajo. LA CURA: la
    //  selección vive en SU PROPIA VENTANA FLOTANTE — un panel pequeño
    //  que nace ANCLADO al slot que lo abrió, con LA LISTA COMPLETA en
    //  una cuadrícula de 6 columnas y SU PROPIA RUEDA (el scroll es de
    //  la ventana: el libro no se mueve). El mismo motor para las DOS
    //  bandejas: la ofrenda del altar (ranura -2) y las ranuras del
    //  trío (0-2). Clic en un icono = elegir; clic fuera o ESC = la
    //  ventana se cierra y NADA cambia.
    //
    //  LA PALETA: la del LIBRO DE INFORMACIÓN (la letra de la v6.168:
    //  «usa el color de la tipografia del libro de informacion») —
    //  papel 216,200,166 · tinta 28,20,16 · violeta 148,72,178 ·
    //  capítulo 168,108,44. Sin transparencias raras: papel sólido.
    // ======================================================================
    internal static class SelectorFlotante
    {
        // LA PALETA DEL LIBRO (exacta — la de LibroDelEternoUI)
        private static readonly Color Papel = new(216, 200, 166);
        private static readonly Color PapelSombra = new(190, 172, 134);
        private static readonly Color Tinta = new(28, 20, 16);
        private static readonly Color TintaVioleta = new(148, 72, 178);
        private static readonly Color TintaOro = new(168, 108, 44);
        private static readonly Color TintaApagado = new(118, 96, 70);

        // LA CUADRÍCULA: 6 columnas de 44 px, iconos de 40.
        private const int Columnas = 6;
        private const int Celda = 44;
        private const int FilasVisibles = 5;         // 30 candidatos a la vista
        private const int AltoTitulo = 34;

        // EL ESTADO (cliente puro): el ancla (el slot que abrió), la
        // lista viva y SU scroll propio.
        private static Rectangle _ancla = Rectangle.Empty;
        private static float _scroll;
        private static readonly System.Collections.Generic.List<Item> _lista = new();
        private static bool _paraTrio;

        /// <summary>¿Está abierta la ventanita?</summary>
        internal static bool Abierta => _ancla.Width > 0;

        /// <summary>ABRIR: anclada al slot, con los candidatos VIVOS.</summary>
        internal static void Abrir(Rectangle ancla, bool paraTrio, string titulo)
        {
            _ancla = ancla;
            _paraTrio = paraTrio;
            _titulo = titulo;
            _scroll = 0f;
            // v6.170 — EL FRAME DE GRACIA (la letra: «esas ventanas
            // flotantes no aparecen, bueno aparecen rapido y se
            // quitan, no duran ni medio segundo»): LA AUTOPSIA — la
            // ventana nace DENTRO del mismo clic que la abrió (el clic
            // en el slot), y el «clic fuera = cerrar» del final del
            // Dibujar la mataba EL MISMO FRAME en que nacía (nació
            // con el botón aún apretado: al soltar, el flanco del
            // clic YA consumido contaba como «clic fuera»). LA CURA:
            // la ventana ignora el clic VIVO de su nacimiento — el
            // «clic fuera» solo vale desde el frame siguiente.
            _tickNacimiento = Main.GameUpdateCount;
            Refrescar();
        }

        private static string _titulo = "";

        /// <summary>v6.170 — EL TICK DE NACIMIENTO (el frame de gracia:
        /// el clic que ABRE la ventana jamás la cierra).</summary>
        private static ulong _tickNacimiento;

        /// <summary>REFRESCAR la lista (el inventario vive: cada frame
        /// la ventana muestra lo que HAY, no una foto vieja).</summary>
        internal static void Refrescar()
        {
            _lista.Clear();
            _lista.AddRange(AltarOfrendasUI.CosecharCandidatos(_paraTrio));
        }

        /// <summary>CERRAR (elegir, clic fuera o ESC).</summary>
        internal static void Cerrar() => _ancla = Rectangle.Empty;

        /// <summary>El rectángulo VIVO de la ventana (para que el libro
        /// sepa que la rueda es NUESTRA cuando el cursor vive aquí).</summary>
        internal static Rectangle RectanguloVivo()
        {
            if (!Abierta) return Rectangle.Empty;
            return CalcularRect(_ancla);
        }

        private static Rectangle CalcularRect(Rectangle ancla)
        {
            // EL TAMAÑO: 6 columnas + márgenes; el alto: título + filas.
            int w = Columnas * Celda + 20;
            int h = AltoTitulo + FilasVisibles * Celda + 12;
            // EL ANCLAJE: la ventana nace BAJO el slot; si se sale de la
            // pantalla, se voltea ARRIBA; si se sale por los lados, se
            // desliza hacia dentro.
            int x = ancla.Center.X - w / 2;
            int y = ancla.Bottom + 6;
            if (y + h > Main.screenHeight - 8) y = ancla.Y - h - 6;
            if (y < 8) y = 8;
            x = Math.Clamp(x, 8, Main.screenWidth - w - 8);
            return new Rectangle(x, y, w, h);
        }

        // ================================================================
        //  EL DIBUJO + LA RUEDA + EL CLIC — todo en un pase (el patrón
        //  de la casa: la geometría y el flanco viven juntos).
        // ================================================================
        public static void Dibujar(SpriteBatch sb, bool clicNuevo, bool ruedaNueva)
        {
            if (!Abierta) return;
            Refrescar();   // la lista vive

            Rectangle r = CalcularRect(_ancla);
            var px = TextureAssets.MagicPixel.Value;
            var fuente = FontAssets.MouseText.Value;

            // EL PAPEL SÓLIDO (nada de transparencias: la ventana lee
            // encima de la página sin fundirse con ella).
            sb.Draw(px, r, Papel);
            // LA SOMBRA del borde (el doblez del papel flotante)
            sb.Draw(px, new Rectangle(r.X + 3, r.Y + 3, r.Width, r.Height), PapelSombra * 0.6f);
            // EL MARCO COSIDO (el del libro)
            LibroMarco.Marco(sb, r, TintaOro * 0.8f);

            // EL TÍTULO (la letra de cada bandeja)
            string tit = _titulo;
            float escT = 0.8f;
            var mT = fuente.MeasureString(tit) * escT;
            if (mT.X > r.Width - 24f && mT.X > 0f) escT *= (r.Width - 24f) / mT.X;
            Utils.DrawBorderString(sb, tit, new Vector2(r.Center.X, r.Y + AltoTitulo * 0.5f),
                TintaVioleta, escT, 0.5f, 0.5f);
            // el filete del título (el estilo del libro)
            sb.Draw(px, new Rectangle(r.X + 10, r.Y + AltoTitulo - 6, r.Width - 20, 2),
                TintaOro * 0.55f);

            // ============================================================
            //  LA RUEDA ES DE LA VENTANA: el máximo scroll nace de la
            //  lista VIVA; la rueda SOLO se come si el cursor está
            //  DENTRO de la ventanita (la página del libro no se mueve).
            // ============================================================
            int totalCeldas = _lista.Count;
            int filasTotales = (totalCeldas + Columnas - 1) / Columnas;
            float maxScroll = Math.Max(0f, (filasTotales - FilasVisibles) * Celda);
            bool cursorDentro = r.Contains(Main.mouseX, Main.mouseY) &&
                !Terraria.GameInput.PlayerInput.IgnoreMouseInterface;
            if (cursorDentro && ruedaNueva)
            {
                _scroll = MathHelper.Clamp(_scroll - Terraria.GameInput.PlayerInput
                    .ScrollWheelDelta * 0.4f, 0f, maxScroll);
                Terraria.GameInput.PlayerInput.ScrollWheelDelta = 0;   // la rueda es NUESTRA
            }
            // LA BARRA DE PROGRESO (la línea del scroll: oro apagado)
            if (maxScroll > 0f)
            {
                float proporcion = (FilasVisibles * Celda) / (float)(filasTotales * Celda);
                int altoBarra = Math.Max(18, (int)((AltoTitulo + FilasVisibles * Celda) * proporcion));
                int yBarra = r.Y + AltoTitulo + (int)((_scroll / maxScroll) *
                    (AltoTitulo + FilasVisibles * Celda - altoBarra));
                sb.Draw(px, new Rectangle(r.Right - 6, r.Y + AltoTitulo, 4,
                    FilasVisibles * Celda), PapelSombra * 0.8f);
                sb.Draw(px, new Rectangle(r.Right - 6, yBarra, 4, altoBarra), TintaOro * 0.7f);
            }

            // ============================================================
            //  LA CUADRÍCULA CON RECORTE: lo que no cabe por ARRIBA no
            //  se dibuja (la ventana es una vitrina, no una página).
            // ============================================================
            int dibujadas = 0;
            for (int i = 0; i < totalCeldas; i++)
            {
                int col = i % Columnas;
                int fila = i / Columnas;
                float yFila = r.Y + AltoTitulo + 2 + fila * Celda - _scroll;
                // EL RECORTE: la fila que no vive en la vitrina, fuera
                if (yFila + Celda < r.Y + AltoTitulo || yFila > r.Bottom - 10) continue;
                Rectangle celda = new Rectangle(r.X + 10 + col * Celda, (int)yFila, Celda - 4, Celda - 4);
                Item it = _lista[i];
                bool hov = celda.Contains(Main.mouseX, Main.mouseY) &&
                    !Terraria.GameInput.PlayerInput.IgnoreMouseInterface;
                if (hov) Main.LocalPlayer.mouseInterface = true;
                sb.Draw(px, celda, hov ? PapelSombra * 1.15f : PapelSombra * 0.7f);
                LibroMarco.Marco(sb, celda, hov ? TintaVioleta : TintaOro * 0.35f);
                Texture2D tex = TextureAssets.Item[it.type].Value;
                float esc = Math.Min(32f / tex.Width, 32f / tex.Height);
                sb.Draw(tex, celda.Center.ToVector2(), null, Color.White, 0f,
                    tex.Size() * 0.5f, esc, SpriteEffects.None, 0f);
                if (it.stack > 1)
                    Utils.DrawBorderString(sb, it.stack.ToString(),
                        new Vector2(celda.Right - 3f, celda.Bottom - 7f),
                        Tinta, 0.68f, 1f, 0.85f);
                // EL TOOLTIP DE LA CASA (el nombre al pasar el cursor)
                if (hov)
                {
                    string nombre = it.Name;
                    float escN = 0.8f;
                    var mN = fuente.MeasureString(nombre) * escN;
                    if (mN.X > 260f && mN.X > 0f) escN *= 260f / mN.X;
                    Utils.DrawBorderString(sb, nombre,
                        new Vector2(celda.Center.X, celda.Y - 8f),
                        Tinta, escN, 0.5f, 1f);
                }
                if (hov && clicNuevo)
                {
                    // LA ELECCIÓN: el índice de la lista VIVA al motor
                    // del altar (el mismo camino de la bandeja vieja).
                    AltarOfrendasUI.ClicEnCandidato(i);
                    Cerrar();
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Grab);
                }
                dibujadas++;
            }

            // LA LISTA VACÍA (su aviso, centrado en la vitrina)
            if (totalCeldas == 0)
            {
                string vacio = Language.GetTextValue(_paraTrio
                    ? "Mods.AethonMod.Items.TrioSinConsagradas"
                    : "Mods.AethonMod.Items.BandejaVacia");
                Utils.DrawBorderString(sb, vacio, new Vector2(r.Center.X,
                    r.Y + AltoTitulo + FilasVisibles * Celda * 0.5f),
                    TintaApagado, 0.78f, 0.5f, 0.5f);
            }

            // CLIC FUERA = cerrar (sin elegir nada — la ventana
            // respeta) — PERO NUNCA EL CLIC DE SU NACIMIENTO (el
            // frame de gracia de la v6.170: el clic que abrió la
            // ventana vive en _tickNacimiento; un clic «fuera» del
            // MISMO frame de nacimiento no cuenta).
            if (clicNuevo && !r.Contains(Main.mouseX, Main.mouseY) &&
                Main.GameUpdateCount > _tickNacimiento)
                Cerrar();
        }
    }
}
