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
    // ======================================================================
    //  PrismaInteriorUI — v6.115 → v6.137 — LA VENTANA DEL PILAR PRISMA:
    //  EL CONTROL DEL CUERPO ENTERO DEL DIOS.
    //
    //  LA LETRA NUEVA: «mejorar la distribucion de textos en el pilar de
    //  prisma ya que estos se desbordan, ademas demosle utilidad al
    //  mismo y al propio mundo interior, el pilar de prisma sera capas
    //  de activar las oleada de hambre dependiendo del ultimo nivel en
    //  el que quedo el jugador, tambien permitira elejir el nivel de la
    //  misma pero siempre sera hasta el ultimo nivel desbloqueado por
    //  el jugador, y para desbloquear un nivel el jugador debe completar
    //  ese nivel de la oleada de hambre fuera del mundo interior» +
    //  «el pilar de prisma tambien permitira invocar a todos los jefes
    //  que el jugador haya vencido y devorado, cada jefe devorado se
    //  podra invocar en una version aun mas poderosa dentro del mundo
    //  interior» + «asegurate de que el pilar de prisma tenga todas las
    //  opciones en su ventana, hazlo lo mejor que pueda».
    //
    //  LA VENTANA NUEVA (el patrón medido del altar v6.92 — el panel
    //  NACE con la altura de SU contenido: nada se desborda jamás):
    //   · VOLVER AL MUNDO REAL (el oro de siempre).
    //   · INVOCAR A AETHON (el violeta de siempre).
    //   · LA OLEADA DE HAMBRE (nuevo): el selector de nivel 1..el
    //     último DESBLOQUEADO (FuriaNivel del portador — desbloquear
    //     un nivel = COMPLETARLO FUERA del interior, la progresión de
    //     siempre) y el botón LANZAR. El festín del interior NO sube
    //     el FuriaNivel (probar no es progresar — la ley v6.50.74):
    //     se lanza con natural:false.
    //   · LOS JEFES DEVORADOS (nuevo): la lista de JefesDevorados del
    //     portador, cada uno con SU nivel de poder (1 + kills
    //     interiores ×25%) y su botón. El elegido nace delante del
    //     pilar sellado por JefeInteriorGlobalNPC (aura de oleada +
    //     stats + IA por niveles).
    //  Con scroll propio cuando la lista crece (la rueda del ratón —
    //  el patrón de la casa).
    // ======================================================================
    public class PrismaInteriorUI : ModSystem
    {
        // === EL ESTADO (cliente puro — el patrón El Cielo) ===
        private static bool _abierta;
        private static int _tileX, _tileY;
        private static bool _ultimoClicIzq;
        private static int _nivelElegido = 1;      // el selector de la oleada
        private static float _scrollJefes = 0f;    // el scroll de la lista
        private static int _ultimaRueda = int.MinValue;   // la rueda (el patrón del árbol)
        private static int _hoverJefe = -1;        // el jefe bajo el cursor

        private static readonly Color TintaFondo = new Color(16, 8, 28);
        private static readonly Color TintaVioleta = new Color(185, 110, 234);
        private static readonly Color TintaOro = new Color(255, 214, 130);
        private static readonly Color TintaApagado = new Color(150, 130, 170);
        private static readonly Color TintaTeal = new Color(120, 235, 190);

        /// <summary>¿La ventana está abierta?</summary>
        public static bool Abierta => _abierta;

        /// <summary>v6.164 — EL CIERRE POR ORDEN DEL LIBRO (ESC/X en
        /// la pestaña pilar).</summary>
        public static void ForzarCierre() => _abierta = false;

        // ================================================================
        //  v6.163 — EL PILAR VIVE EN EL LIBRO (la letra: «y lo mismo
        //  para el pilar en el mundo interior»): el panel FLOTANTE
        //  murió — el contenido se dibuja como PESTAÑA del libro
        //  (PilarDelLibro). Este sistema sigue vivo como EL MOTOR: la
        //  ley de distancia y TODA la lógica de clic (invocar, la
        //  oleada, los jefes). El libro llama; el pilar hace.
        // ================================================================

        /// <summary>v6.164 — LOS CLICS GRANULARES DEL PILAR (la cura de los
/// botones muertos: la página del libro llama a ESTOS métodos con el
/// flanco de la casa — cada botón, su función).</summary>
        public static void ClicEnVolver()
        {
            try
            {
                Player yo = Main.LocalPlayer;
                if (yo == null || !yo.active || !_abierta) return;
                _abierta = false;
                if (LibroDelEternoUI.Abierto) LibroDelEternoUI.Cerrar();
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74);
                Systems.SubmundoGrimorio.Salir(yo);
            }
            catch { }
        }

        public static void ClicEnInvocar()
        {
            try
            {
                Player yo = Main.LocalPlayer;
                if (yo == null || !yo.active || !_abierta) return;
                InvocarLaLlamada(yo);
            }
            catch { }
        }

        public static void ClicEnSellado()
        {
            try
            {
                Player yo = Main.LocalPlayer;
                if (yo == null || !yo.active || !_abierta) return;
                if (!NPCs.AethonSellado.Invocar(yo))
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
            }
            catch { }
        }

        public static void ClicEnLanzar(int nivel)
        {
            try
            {
                Player yo = Main.LocalPlayer;
                if (yo == null || !yo.active || !_abierta) return;
                nivel = Math.Max(1, Math.Min(10, nivel));
                LanzarLaOleada(yo, nivel);
            }
            catch { }
        }

        public static void ClicEnJefe(int tipoJefe)
        {
            try
            {
                Player yo = Main.LocalPlayer;
                if (yo == null || !yo.active || !_abierta) return;
                InvocarJefeInterior(yo, tipoJefe);
            }
            catch { }
        }

        /// <summary>EL PRISMA SE ABRE (o se cierra).</summary>
        public static void Alternar(int i, int j)
        {
            _abierta = !_abierta;
            _tileX = i;
            _tileY = j;
            _scrollJefes = 0f;
            // el selector arranca en el ÚLTIMO nivel desbloqueado
            try
            {
                var sp = Main.LocalPlayer?.GetModPlayer<Players.ShardPlayer>();
                if (sp != null)
                {
                    _nivelElegido = Math.Max(1, Math.Min(10, sp.FuriaNivel));
                    PilarDelLibro.NivelElegido = _nivelElegido;
                }
            }
            catch { }
        }

        public override void OnWorldUnload() { _abierta = false; }
        public override void Unload() { _abierta = false; }

        public override void ModifyInterfaceLayers(System.Collections.Generic.List<GameInterfaceLayer> layers)
        {
            // v6.163 — EL PANEL FLOTANTE MURIÓ (la letra: «y lo mismo
            // para el pilar en el mundo interior»): el contenido vive
            // como PESTAÑA del libro (PilarDelLibro). Solo queda EL
            // MOTOR: la ley de distancia del pilar físico — cada
            // frame, sin pilar (roto o lejos) o sin mundo interior,
            // se cierra; y si el libro está en la pestaña pilar, se
            // cierra TAMBIÉN.
            if (_abierta)
            {
                try
                {
                    Player yo = Main.LocalPlayer;
                    if (yo == null || !yo.active || Main.gameMenu || Main.dedServ ||
                        !Systems.SubmundoGrimorio.Activo)
                    {
                        _abierta = false;
                        if (LibroDelEternoUI.Abierto && LibroDelEternoUI.Pestana == 2)
                            LibroDelEternoUI.Cerrar();
                        return;
                    }
                    float dist = Vector2.Distance(yo.Center,
                        new Vector2(_tileX * 16f + 24f, _tileY * 16f + 8f));
                    if (dist > 280f)
                    {
                        _abierta = false;
                        if (LibroDelEternoUI.Abierto && LibroDelEternoUI.Pestana == 2)
                            LibroDelEternoUI.Cerrar();
                    }
                }
                catch { }
            }
        }

        // === LOS RECTS VIVOS (el clic los lee — el patrón del panel) ===
        private static Rectangle _geoVolver, _geoInvocar, _geoCerrar;

        // v6.154 — EL FLANCO DEL ESC (la letra: «que la ventana del
        // pilar… se cierren tambien con esc»): el mismo borde de
        // teclado del altar — dispara SOLO al PRESIONAR, no mientras
        // está sostenida.
        private static bool _ultimoEsc;
        private static Rectangle _geoMenos, _geoMas, _geoLanzar;
        private static Rectangle _geoListaJefes;
        private static readonly List<Rectangle> _geoJefes = new List<Rectangle>();

        private static bool DibujarPanel()
        {
            try
            {
                if (!_abierta || Main.gameMenu || Main.dedServ) return true;
                if (!Systems.SubmundoGrimorio.Activo) { _abierta = false; return true; }

                Player yo = Main.LocalPlayer;
                if (yo == null || !yo.active) { _abierta = false; return true; }

                // LA LEY DE LA DISTANCIA (la casa: 280 px).
                float dist = Vector2.Distance(yo.Center,
                    new Vector2(_tileX * 16f + 24f, _tileY * 16f + 8f));
                if (dist > 280f) { _abierta = false; return true; }

                SpriteBatch sb = Main.spriteBatch;
                Texture2D pixel = TextureAssets.MagicPixel.Value;
                var fuente = FontAssets.MouseText.Value;

                // === LOS DATOS VIVOS ===
                var sp = yo.GetModPlayer<Players.ShardPlayer>();
                int maxNivel = sp != null ? Math.Max(1, Math.Min(10, sp.FuriaNivel)) : 1;
                var devorados = sp != null ? sp.JefesDevorados : null;

                // ==============================================================
                //  PASADA 1 — LA MEDIDA (la cura del altar v6.92: el panel
                //  NACE con la altura de su contenido — NADA se desborda)
                // ==============================================================
                float altoContenido = 66f;   // titular + subtítulo
                altoContenido += 44f + 10f;  // VOLVER
                altoContenido += 44f + 10f;  // INVOCAR AETHON
                altoContenido += 26f + 30f;  // título OLEADA + selector
                altoContenido += 44f + 14f;  // LANZAR
                altoContenido += 26f;        // título JEFES DEVORADOS
                int nJefes = devorados != null ? devorados.Count : 0;
                float altoLista = 0f;
                if (nJefes > 0)
                {
                    altoLista = Math.Min(nJefes, 5) * 32f;   // hasta 5 visibles
                    altoContenido += altoLista + 8f;
                }
                else
                {
                    altoContenido += 24f;    // la línea del vacío
                }
                altoContenido += 30f;        // la leyenda del pie

                int w = 460;
                int h = (int)Math.Ceiling(altoContenido);
                if (h < 300) h = 300;
                if (h > Main.screenHeight - 16) h = Main.screenHeight - 16;

                int x = Main.screenWidth / 2 - w / 2;
                int y = Main.screenHeight / 2 - h / 2;
                if (x < 10) x = 10;
                if (y < 8) y = 8;
                if (y + h > Main.screenHeight - 8)
                    y = Math.Max(8, Main.screenHeight - 8 - h);

                // EL FONDO — el cielo estrellado de la casa (el del altar).
                Texture2D cielo = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/Investigacion/Espacio_1").Value;
                sb.Draw(pixel, new Rectangle(x - 3, y - 3, w + 6, h + 6), TintaFondo * 0.94f);
                sb.Draw(cielo, new Rectangle(x, y, w, h), Color.White * 0.92f);
                sb.Draw(pixel, new Rectangle(x, y, w, h), TintaFondo * 0.60f);
                Marco(sb, pixel, new Rectangle(x - 3, y - 3, w + 6, h + 6), TintaVioleta * 0.80f);

                // EL TITULAR.
                string titTxt = Language.GetTextValue("Mods.AethonMod.PrismaInterior.Titulo");
                string subTxt = Language.GetTextValue("Mods.AethonMod.PrismaInterior.Subtitulo");
                Utils.DrawBorderString(sb, titTxt, new Vector2(x + w / 2f, y + 24f),
                    TintaVioleta, 1.05f, 0.5f, 0.4f);
                Utils.DrawBorderString(sb, subTxt, new Vector2(x + w / 2f, y + 44f),
                    TintaApagado, 0.75f, 0.5f, 0.4f);

                // === LA INTERACCIÓN ===
                bool clicAhora = Main.mouseLeft && !Terraria.GameInput.PlayerInput.IgnoreMouseInterface;
                bool clicNuevo = clicAhora && !_ultimoClicIzq;
                _ultimoClicIzq = clicAhora;

                float cy = y + 62f;

                // === EL BOTÓN 1 — VOLVER AL MUNDO REAL (el oro) ===
                _geoVolver = new Rectangle(x + 40, (int)cy, w - 80, 44);
                bool hoverVolver = _geoVolver.Contains(Main.mouseX, Main.mouseY);
                sb.Draw(pixel, _geoVolver,
                    (hoverVolver ? TintaOro : new Color(212, 168, 84)) * (hoverVolver ? 0.34f : 0.20f));
                Marco(sb, pixel, _geoVolver, TintaOro * (hoverVolver ? 0.95f : 0.55f));
                Utils.DrawBorderString(sb, Language.GetTextValue("Mods.AethonMod.PrismaInterior.BotonVolver"),
                    new Vector2(_geoVolver.Center.X, _geoVolver.Center.Y),
                    hoverVolver ? TintaOro : new Color(232, 200, 140), 0.95f, 0.5f, 0.5f);
                cy += 54f;

                // === EL BOTÓN 2 — INVOCAR A AETHON (el violeta) ===
                bool yaVive = YaViveLaFamilia();
                _geoInvocar = new Rectangle(x + 40, (int)cy, w - 80, 44);
                bool hoverInvocar = _geoInvocar.Contains(Main.mouseX, Main.mouseY) && !yaVive;
                sb.Draw(pixel, _geoInvocar,
                    (hoverInvocar ? TintaVioleta : new Color(120, 74, 178)) * (hoverInvocar ? 0.36f : 0.20f));
                Marco(sb, pixel, _geoInvocar,
                    (yaVive ? TintaApagado : TintaVioleta) * (hoverInvocar ? 0.95f : 0.55f));
                string txtInvocar = yaVive
                    ? Language.GetTextValue("Mods.AethonMod.PrismaInterior.YaVive")
                    : Language.GetTextValue("Mods.AethonMod.PrismaInterior.BotonInvocar");
                Utils.DrawBorderString(sb, txtInvocar,
                    new Vector2(_geoInvocar.Center.X, _geoInvocar.Center.Y),
                    yaVive ? TintaApagado : (hoverInvocar ? TintaVioleta : new Color(196, 160, 240)),
                    0.95f, 0.5f, 0.5f);
                cy += 58f;

                // ==============================================================
                //  LA SECCIÓN NUEVA — LA OLEADA DE HAMBRE
                // ==============================================================
                string tituloOleada = Language.GetTextValue("Mods.AethonMod.PrismaInterior.OleadaTitulo");
                Utils.DrawBorderString(sb, tituloOleada, new Vector2(x + w / 2f, cy),
                    TintaTeal, 0.85f, 0.5f, 0.4f);
                cy += 22f;

                // EL SELECTOR: [-] NIVEL N [+] (1..maxNivel)
                _geoMenos = new Rectangle(x + 90, (int)cy, 34, 30);
                _geoMas = new Rectangle(x + w - 124, (int)cy, 34, 30);
                Rectangle geoNivel = new Rectangle(x + 130, (int)cy, w - 260, 30);
                bool bloqueado = maxNivel <= 1;
                bool hoverMenos = _geoMenos.Contains(Main.mouseX, Main.mouseY) && !bloqueado;
                bool hoverMas = _geoMas.Contains(Main.mouseX, Main.mouseY) && !bloqueado && _nivelElegido < maxNivel;
                sb.Draw(pixel, _geoMenos, (hoverMenos ? TintaTeal : TintaApagado) * 0.25f);
                sb.Draw(pixel, _geoMas, (hoverMas ? TintaTeal : TintaApagado) * 0.25f);
                sb.Draw(pixel, geoNivel, TintaFondo * 0.6f);
                Marco(sb, pixel, geoNivel, TintaTeal * 0.4f);
                Utils.DrawBorderString(sb, "−", new Vector2(_geoMenos.Center.X, _geoMenos.Center.Y),
                    hoverMenos ? TintaTeal : TintaApagado, 1.1f, 0.5f, 0.5f);
                Utils.DrawBorderString(sb, "+", new Vector2(_geoMas.Center.X, _geoMas.Center.Y),
                    hoverMas ? TintaTeal : TintaApagado, 1.1f, 0.5f, 0.5f);
                string nivelTxt = Language.GetTextValue("Mods.AethonMod.PrismaInterior.OleadaNivel",
                    _nivelElegido, maxNivel);
                Utils.DrawBorderString(sb, nivelTxt, new Vector2(geoNivel.Center.X, geoNivel.Center.Y),
                    bloqueado ? TintaApagado : TintaTeal, 0.92f, 0.5f, 0.5f);
                cy += 36f;

                // LA PISTA de la progresión (el desbloqueo vive AFUERA)
                Utils.DrawBorderString(sb, Language.GetTextValue(
                        "Mods.AethonMod.PrismaInterior.OleadaPista", maxNivel),
                    new Vector2(x + w / 2f, cy), TintaApagado * 0.9f, 0.68f, 0.5f, 0.4f);
                cy += 18f;

                // EL BOTÓN LANZAR (la oleada del festín — el rojo del hambre)
                bool festinVivo = false;
                try { festinVivo = Systems.GrimorioFuriaSistema.Activo; } catch { }
                _geoLanzar = new Rectangle(x + 90, (int)cy, w - 180, 40);
                bool hoverLanzar = _geoLanzar.Contains(Main.mouseX, Main.mouseY) && !festinVivo;
                Color rojoHambre = new Color(226, 64, 64);
                sb.Draw(pixel, _geoLanzar,
                    (hoverLanzar ? rojoHambre : new Color(150, 60, 60)) * (hoverLanzar ? 0.36f : 0.20f));
                Marco(sb, pixel, _geoLanzar,
                    (festinVivo ? TintaApagado : rojoHambre) * (hoverLanzar ? 0.95f : 0.55f));
                string txtLanzar = festinVivo
                    ? Language.GetTextValue("Mods.AethonMod.PrismaInterior.OleadaCorre")
                    : Language.GetTextValue("Mods.AethonMod.PrismaInterior.OleadaLanzar", _nivelElegido);
                Utils.DrawBorderString(sb, txtLanzar,
                    new Vector2(_geoLanzar.Center.X, _geoLanzar.Center.Y),
                    festinVivo ? TintaApagado : (hoverLanzar ? new Color(255, 150, 140) : new Color(240, 170, 160)),
                    0.92f, 0.5f, 0.5f);
                cy += 50f;

                // ==============================================================
                //  LA SECCIÓN NUEVA — LOS JEFES DEVORADOS (la arena)
                // ==============================================================
                string tituloJefes = Language.GetTextValue("Mods.AethonMod.PrismaInterior.JefesTitulo");
                Utils.DrawBorderString(sb, tituloJefes, new Vector2(x + w / 2f, cy),
                    TintaVioleta, 0.85f, 0.5f, 0.4f);
                cy += 20f;

                _geoJefes.Clear();
                _hoverJefe = -1;
                if (nJefes == 0)
                {
                    // LA LÍNEA DEL VACÍO (los jefes que aún no han caído)
                    string vacio = Language.GetTextValue("Mods.AethonMod.PrismaInterior.JefesVacio");
                    Utils.DrawBorderString(sb, vacio, new Vector2(x + w / 2f, cy + 10f),
                        TintaApagado * 0.85f, 0.72f, 0.5f, 0.5f);
                    cy += 26f;
                }
                else
                {
                    // LA LISTA (hasta 5 filas visibles + scroll con la rueda)
                    _geoListaJefes = new Rectangle(x + 40, (int)cy, w - 80, (int)altoLista);
                    sb.Draw(pixel, _geoListaJefes, TintaFondo * 0.55f);
                    Marco(sb, pixel, _geoListaJefes, TintaVioleta * 0.35f);

                    float maxScroll = Math.Max(0f, nJefes * 32f - altoLista);
                    _scrollJefes = MathHelper.Clamp(_scrollJefes, 0f, maxScroll);
                    if (_geoListaJefes.Contains(Main.mouseX, Main.mouseY))
                    {
                        // LA RUEDA (el patrón del árbol de habilidades:
                        // Mouse.GetState().ScrollWheelValue con delta)
                        int rueda = Microsoft.Xna.Framework.Input.Mouse.GetState().ScrollWheelValue;
                        if (_ultimaRueda != int.MinValue)
                            _scrollJefes -= (rueda - _ultimaRueda) / 120f * 24f;
                        _ultimaRueda = rueda;
                        _scrollJefes = MathHelper.Clamp(_scrollJefes, 0f, maxScroll);
                    }
                    else
                    {
                        _ultimaRueda = Microsoft.Xna.Framework.Input.Mouse.GetState().ScrollWheelValue;
                    }

                    int visibles = Math.Min(nJefes, 5 + (int)(_scrollJefes / 32f));
                    for (int k = 0; k < nJefes; k++)
                    {
                        float filaY = cy + 4f + k * 32f - _scrollJefes;
                        if (filaY < cy - 30f || filaY > cy + altoLista) continue;
                        int tipoJefe = devorados[k];
                        int kills = sp.KillsInteriorDe(tipoJefe);
                        int nivelPoder = Math.Min(10, 1 + kills);
                        float mult = Globals.JefeInteriorGlobalNPC.Multiplicador(kills);

                        Rectangle fila = new Rectangle(x + 44, (int)filaY, w - 88, 28);
                        bool hoverFila = fila.Contains(Main.mouseX, Main.mouseY);
                        if (hoverFila) _hoverJefe = k;
                        sb.Draw(pixel, fila,
                            (hoverFila ? TintaVioleta : new Color(90, 60, 140)) * (hoverFila ? 0.34f : 0.20f));
                        Marco(sb, pixel, fila, (hoverFila ? TintaVioleta : TintaApagado) * 0.5f);

                        // EL NOMBRE DEL JEFE + SU PODER
                        string nombreJefe = NombreDe(tipoJefe);
                        string poder = Language.GetTextValue(
                            "Mods.AethonMod.PrismaInterior.JefePoder", nivelPoder, (int)(mult * 100f));
                        Utils.DrawBorderString(sb, nombreJefe,
                            new Vector2(fila.X + 10f, fila.Center.Y),
                            hoverFila ? TintaVioleta : new Color(210, 190, 240), 0.82f, 0f, 0.5f);
                        Utils.DrawBorderString(sb, poder,
                            new Vector2(fila.Right - 10f, fila.Center.Y),
                            nivelPoder >= 8 ? new Color(255, 150, 140) : TintaOro, 0.72f, 1f, 0.5f);
                        _geoJefes.Add(fila);
                    }
                    cy += altoLista + 8f;
                }

                // === LA LEYENDA DEL PIE — v6.153: LA VERDAD Y EL ANCHO
                //     (la letra: «ese texto se desborda […] en realidad
                //     no se cumple nada de lo que dice ahi»): la
                //     leyenda vieja prometía una ceremonia y unos
                //     fragmentos que NUNCA pasaron — el pilar es EL
                //     OBELISCO DEL UMBRAL (v6.152): la ventana que
                //     manda en el interior. El texto NUEVO dice LO QUE
                //     HAY, y la escala SE CALCULA para caber (la cura
                //     del altar v6.92: mide y achica — jamás desborda).
                string leyenda = Language.GetTextValue("Mods.AethonMod.PrismaInterior.Leyenda");
                float escLey = 0.68f;
                var medLey = fuente.MeasureString(leyenda) * escLey;
                if (medLey.X > w - 48f && medLey.X > 0f)
                    escLey *= (w - 48f) / medLey.X;
                Utils.DrawBorderString(sb, leyenda, new Vector2(x + w / 2f, y + h - 16f),
                    TintaApagado, escLey, 0.5f, 0.5f);

                // === EL BOTÓN CERRAR (la esquina de siempre) ===
                _geoCerrar = new Rectangle(x + w - 34, y + 8, 26, 26);
                bool hoverCerrar = _geoCerrar.Contains(Main.mouseX, Main.mouseY);
                sb.Draw(pixel, _geoCerrar, (hoverCerrar ? new Color(220, 90, 90) : TintaApagado) * 0.25f);
                Utils.DrawBorderString(sb, "X", new Vector2(_geoCerrar.Center.X, _geoCerrar.Center.Y),
                    hoverCerrar ? new Color(220, 90, 90) : TintaApagado, 1.0f, 0.5f, 0.5f);

                // === EL MOTOR DEL CLIC (el patrón de la casa) ===
                bool dentroPanel = new Rectangle(x, y, w, h).Contains(Main.mouseX, Main.mouseY);
                if (dentroPanel) Main.LocalPlayer.mouseInterface = true;

                // v6.154 — ESC TAMBIÉN CIERRA (la letra: «que la
                // ventana del pilar, del altar y del libro de
                // informacion se cierren tambien con esc, no solo con
                // tocar la X»): el flanco de la tecla — mismo patrón
                // del altar.
                bool escAhora = Microsoft.Xna.Framework.Input.Keyboard
                    .GetState().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape);
                if (escAhora && !_ultimoEsc)
                {
                    _abierta = false;
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuClose);
                }
                _ultimoEsc = escAhora;

                if (clicNuevo)
                {
                    if (hoverCerrar)
                    {
                        _abierta = false;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuClose);
                    }
                    else if (hoverVolver)
                    {
                        _abierta = false;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74);
                        Systems.SubmundoGrimorio.Salir(Main.LocalPlayer);
                    }
                    else if (hoverInvocar)
                    {
                        _abierta = false;
                        // v6.145 — EL PRISMA TAMBIÉN DESPIERTA AL
                        // SELLADO (la letra: «el prisma del eterno debe
                        // invocar a AETHON (SELLADO) como mismo invoca
                        // al otro aethon»): SIN Shift, la LLAMADA de
                        // siempre (los dos fragmentos); CON Shift, EL
                        // DIOS SELLADO — el mismo gesto del pilar.
                        bool sellado = Main.keyState.IsKeyDown(
                            Microsoft.Xna.Framework.Input.Keys.LeftShift) ||
                            Main.keyState.IsKeyDown(
                            Microsoft.Xna.Framework.Input.Keys.RightShift);
                        if (sellado)
                        {
                            if (!NPCs.AethonSellado.Invocar(yo))
                                _abierta = true;   // ya vive: la ventana queda
                        }
                        else
                            InvocarLaLlamada(yo);
                    }
                    else if (hoverLanzar)
                    {
                        LanzarLaOleada(yo, _nivelElegido);
                    }
                    else if (hoverMenos && !bloqueado)
                    {
                        _nivelElegido = Math.Max(1, _nivelElegido - 1);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                    }
                    else if (hoverMas && !bloqueado && _nivelElegido < maxNivel)
                    {
                        _nivelElegido = Math.Min(maxNivel, _nivelElegido + 1);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                    }
                    else if (_hoverJefe >= 0 && _hoverJefe < nJefes)
                    {
                        InvocarJefeInterior(yo, devorados[_hoverJefe]);
                    }
                }
            }
            catch { _abierta = false; }
            return true;
        }

        /// <summary>EL NOMBRE de un tipo de NPC (el de la lang del juego
        /// — Lang.GetNPCNameValue, la vía de la casa).</summary>
        private static string NombreDe(int tipo)
        {
            try
            {
                string n = Terraria.Lang.GetNPCNameValue(tipo);
                if (!string.IsNullOrEmpty(n)) return n;
            }
            catch { }
            return "???";
        }

        /// <summary>LA OLEADA DEL PILAR (v6.137): el festín nace DENTRO
        /// del cuerpo — nivel elegido en el selector (1..el último
        /// desbloqueado). natural:false para JAMÁS subir el FuriaNivel
        /// (probar no es progresar — la ley v6.50.74: desbloquear es
        /// COMPLETAR FUERA).</summary>
        private static void LanzarLaOleada(Player yo, int nivel)
        {
            try
            {
                if (Systems.GrimorioFuriaSistema.Activo) return;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74);
                Systems.GrimorioFuriaSistema.Provocar(yo,
                    Math.Max(1, Math.Min(10, nivel)), natural: false);
            }
            catch { }
        }

        /// <summary>EL JEFE INTERIOR (v6.137): el devorado elegido nace
        /// delante del pilar, sellado por la arena (aura de oleada +
        /// stats ×(1+0,25·kills) + IA por niveles). Solo AUTORIDAD.</summary>
        private static void InvocarJefeInterior(Player yo, int tipoJefe)
        {
            try
            {
                if (Main.netMode == NetmodeID.MultiplayerClient) return;
                // ¿YA VIVE uno de la arena? (la arena es de UN duelista)
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n == null || !n.active) continue;
                    if (n.TryGetGlobalNPC<Globals.JefeInteriorGlobalNPC>(out var ji) && ji.EsInterior)
                        return;
                }

                var sp = yo.GetModPlayer<Players.ShardPlayer>();
                int kills = sp != null ? sp.KillsInteriorDe(tipoJefe) : 0;
                int nivelIA = Math.Min(10, 1 + kills);

                // v6.141 — EL ANCLA LEJOS (la letra: «en el mundo
                // interior los jefes deben aparecer alejados del
                // jugador»): 100 tiles de distancia (1600 px) del lado
                // del jugador — el jefe NACE lejos y CAMINA/VAJA hacia
                // la pelea (como toda invocación de vanilla: la emoción
                // de verlo venir).
                int dirX = yo.Center.X < _tileX * 16f ? -1 : 1;
                int tileX = _tileX + dirX * 100;
                int tileY = _tileY - 2;
                int suelo = -1;
                for (int y = tileY; y < tileY + 60 && y < Main.maxTilesY - 10; y++)
                {
                    if (WorldGen.SolidTile(tileX, y)) { suelo = y; break; }
                }
                Vector2 ancla = suelo >= 0
                    ? new Vector2(tileX * 16f + 8f, suelo * 16f - 40f)
                    : new Vector2(tileX * 16f + 8f, (_tileY + 2) * 16f);

                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74);
                int idx = NPC.NewNPC(yo.GetSource_FromThis(), (int)ancla.X, (int)ancla.Y,
                    tipoJefe, 0, 0f, yo.whoAmI);
                if (idx >= 0 && idx < Main.maxNPCs && Main.npc[idx].active)
                {
                    Globals.JefeInteriorGlobalNPC.Sellar(Main.npc[idx], nivelIA);
                    Main.npc[idx].netUpdate = true;
                }
            }
            catch { }
        }

        /// <summary>¿El cursor vive DENTRO del rectángulo?</summary>
        private static bool PanelContiene(Rectangle r)
            => r.Contains(Main.mouseX, Main.mouseY);

        /// <summary>EL MARCO DOBLE de la casa.</summary>
        private static void Marco(SpriteBatch sb, Texture2D pixel, Rectangle r, Color c)
        {
            sb.Draw(pixel, new Rectangle(r.X, r.Y, r.Width, 2), c);
            sb.Draw(pixel, new Rectangle(r.X, r.Y + r.Height - 2, r.Width, 2), c);
            sb.Draw(pixel, new Rectangle(r.X, r.Y, 2, r.Height), c);
            sb.Draw(pixel, new Rectangle(r.X + r.Width - 2, r.Y, 2, r.Height), c);
        }

        /// <summary>¿Ya camina ALGUNO de los fragmentos del recuerdo por
        /// el interior? (no se dobla el llamado — la ley de la casa).</summary>
        private static bool YaViveLaFamilia()
        {
            int[] tipos =
            {
                ModContent.NPCType<NPCs.AethonBoss>(),
                ModContent.NPCType<NPCs.AethonBossOscuro>(),
                ModContent.NPCType<NPCs.AethonBossFusionado>(),
                ModContent.NPCType<NPCs.AethonSellado>(),
            };
            for (int t = 0; t < tipos.Length; t++)
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n != null && n.active && n.type == tipos[t]) return true;
                }
            return false;
        }

        /// <summary>
        /// LA LLAMADA NACE DEL PRISMA (v6.123 — LA CEREMONIA MURIÓ): los
        /// DOS fragmentos nacen AL INSTANTE delante del prisma con SUS
        /// estallidos de luz de nacimiento.
        /// </summary>
        private static void InvocarLaLlamada(Player yo)
        {
            // EL ANCLA: delante del prisma, del lado del jugador.
            int dirX = yo.Center.X < _tileX * 16f ? -1 : 1;
            int tileX = _tileX + dirX * 9;
            int tileY = _tileY - 2;
            int suelo = -1;
            for (int y = tileY; y < tileY + 60 && y < Main.maxTilesY - 10; y++)
            {
                if (WorldGen.SolidTile(tileX, y)) { suelo = y; break; }
            }
            Vector2 ancla = suelo >= 0
                ? new Vector2(tileX * 16f + 8f, suelo * 16f)
                : new Vector2(tileX * 16f + 8f, (_tileY + 2) * 16f);

            Vector2 posLuz = ancla + new Vector2(-600f, -600f) +
                Main.rand.NextVector2Circular(100f, 100f);
            NPC.SpawnBoss((int)posLuz.X, (int)posLuz.Y,
                ModContent.NPCType<NPCs.AethonBoss>(), yo.whoAmI);

            Vector2 posOscuro = ancla + new Vector2(600f, -600f) +
                Main.rand.NextVector2Circular(100f, 100f);
            NPC.SpawnBoss((int)posOscuro.X, (int)posOscuro.Y,
                ModContent.NPCType<NPCs.AethonBossOscuro>(), yo.whoAmI);
        }
    }
}
