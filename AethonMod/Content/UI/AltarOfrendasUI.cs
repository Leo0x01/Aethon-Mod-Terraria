using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.ModLoader.IO;

namespace AethonMod.Content.UI
{
    // ======================================================================
    //  AltarOfrendasUI — v6.88 — EL ALTAR DE LA CONSAGRACIÓN (v6.90 — el
    //  renombrado: el grimorio es una entidad CÓSMICA, no el bioma de la
    //  Corrupción — la letra: «llamarlos corrompidos no se si sea
    //  correcto, seria mejor decir algo como corrompido por el eterno o
    //  de otra manera»; el sello de la casa ahora es CONSAGRADO: lo que
    //  pasa por el altar queda CONSAGRADO POR EL ETERNO).
    //
    //  v6.90 — LA LETRA NUEVA: «si lo que corrompes es un arma salen
    //  estadisticas, pero si se corrompe otro item como un lingote de
    //  oro, entonces solo sale la etiqueta» — EL ALTAR ACEPTA ARMAS **Y
    //  MATERIALES**: el ARMA consagrada gana sus estadísticas (el +5 %
    //  de siempre) y su etiqueta; cualquier OTRO item (lingotes, gemas,
    //  cera…) gana SOLO LA ETIQUETA junto al nombre — el sello que el
    //  grimorio exige para devorar la ofrenda del despertar.
    //
    //  LA VENTANA MEJORADA (la letra: «debes mejorar la ventana que usa
    //  para devorar»): EL PRECIO A LA VISTA (cuántas esencias cuesta
    //  ANTES de pulsar · cuántas llevas), el RESULTADO (arma → stats +
    //  etiqueta · material → solo la etiqueta) — todo legible de un
    //  vistazo.
    //
    //  CERO VÍNCULO CON EL GRIMORIO: el panel ya NO lee el alma del
    //  libro (ni puntos, ni crédito, ni SinLibro) — el altar consagra
    //  para CUALQUIERA. El patrón de UI es el de siempre (la casa: El
    //  Cielo del Despertar — LegacyGameInterfaceLayer antes de "Vanilla:
    //  Mouse Text", estado estático de cliente, la ley de la distancia a
    //  280 px y la persistencia del slot con el MUNDO).
    // ======================================================================
    public class AltarOfrendasUI : ModSystem
    {
        // === EL ESTADO (cliente puro — el patrón El Cielo) ===
        private static bool _abierta;
        private static int _tileX, _tileY;
        private static Item _arma = new Item();
        private static bool _ultimoClicIzq;
        private static bool _ultimoEsc;

        /// <summary>¿El panel está abierto?</summary>
        public static bool Abierta => _abierta;

        /// <summary>EL ALTAR SE ABRE (o se cierra): lo llama el RightClick
        /// del tile — con la posición del altar (para la ley de la
        /// distancia).</summary>
        public static void Alternar(int i, int j)
        {
            _abierta = !_abierta;
            _tileX = i; _tileY = j;
            if (!_abierta) DevolverElArma();
        }

        public override void OnWorldUnload() { CerrarDeVerdad(); }
        public override void Unload() { CerrarDeVerdad(); _arma = new Item(); }

        /// <summary>EL CIERRE LIMPIO: nada queda en el slot (al cursor si
        /// está libre, al suelo si no).</summary>
        private static void CerrarDeVerdad()
        {
            if (!_abierta) { _arma = new Item(); return; }
            _abierta = false;
            DevolverElArma();
        }

        private static void DevolverElArma()
        {
            try
            {
                if (_arma == null || _arma.IsAir) { _arma = new Item(); return; }
                Player yo = Main.LocalPlayer;
                if (yo == null || !yo.active) return;
                if (Main.mouseItem != null && Main.mouseItem.IsAir)
                    Main.mouseItem = (Item)_arma.Clone();
                else
                    yo.QuickSpawnItem(yo.GetSource_Misc("AltarCorrupcion"), _arma, _arma.stack);
                _arma = new Item();
            }
            catch { _arma = new Item(); }
        }

        // ================================================================
        //  LA PERSISTENCIA DEL SLOT (con el MUNDO — la clave de siempre
        //  «altarRelleno» se lee por compatibilidad con mundos v6.86).
        // ================================================================
        public override void SaveWorldData(TagCompound tag)
        {
            try
            {
                if (_arma != null && !_arma.IsAir)
                    tag["altarArma"] = ItemIO.Save(_arma);
            }
            catch { }
        }

        public override void LoadWorldData(TagCompound tag)
        {
            try
            {
                _arma = new Item();
                if (tag.ContainsKey("altarArma"))
                    _arma = ItemIO.Load(tag.Get<TagCompound>("altarArma"));
                else if (tag.ContainsKey("altarRelleno"))   // mundos de la v6.86
                    _arma = ItemIO.Load(tag.Get<TagCompound>("altarRelleno"));
                if (_arma == null) _arma = new Item();
            }
            catch { _arma = new Item(); }
        }

        // ================================================================
        //  LA CAPA (la de la casa: antes de "Vanilla: Mouse Text")
        // ================================================================
        public override void ModifyInterfaceLayers(System.Collections.Generic.List<GameInterfaceLayer> layers)
        {
            int idx = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
            if (idx != -1)
            {
                layers.Insert(idx, new LegacyGameInterfaceLayer(
                    "AethonMod: El Altar de la Corrupción",
                    DibujarElPanel, InterfaceScaleType.UI));
            }
        }

        // ================================================================
        //  LA PALETA (la de la casa)
        // ================================================================
        private static readonly Color TintaFondo = new(10, 16, 18);
        private static readonly Color TintaPanel = new(16, 28, 30);
        private static readonly Color TintaTeal = new(90, 220, 180);
        private static readonly Color TintaVioleta = new(186, 96, 236);
        private static readonly Color TintaTexto = new(220, 250, 245);
        private static readonly Color TintaApagado = new(110, 150, 145);
        private static readonly Color TintaRojo = new(255, 120, 120);
        private static string Clave(string c) => "Mods.AethonMod.Altar." + c;

        // ================================================================
        //  v6.90 — EL FILTRO DEL CONSAGRADOR: ARMAS limpias **Y
        //  MATERIALES** (la letra: «si se corrompe otro item como un
        //  lingote de oro, entonces solo sale la etiqueta»): cualquier
        //  item de ofrenda vale — el arma gana stats + etiqueta, el
        //  material SOLO la etiqueta. Nada de herramientas ni
        //  accesorios, ni el grimorio, ni las esencias (ellas SON la
        //  esencia del Eterno), ni el libro, ni monedas, ni lo ya
        //  consagrado.
        // ================================================================
        private static bool EsOfrendaConsagrible(Item it)
        {
            try
            {
                if (it == null || it.IsAir) return false;
                // LAS ESENCIAS NO (la letra: «menos las esencias mismas
                // ya que estas son la misma esencia del eterno»).
                if (it.type == ModContent.ItemType
                    <global::AethonMod.Content.Items.Altar.ItemCorrupto>()) return false;
                if (it.type == ModContent.ItemType
                    <global::AethonMod.Content.Items.Altar.LibroCorrupto>()) return false;
                // EL DINERO NO (las monedas se funden: el sello no vive en ellas).
                if (it.type == ItemID.CopperCoin || it.type == ItemID.SilverCoin ||
                    it.type == ItemID.GoldCoin || it.type == ItemID.PlatinumCoin) return false;
                // EL GRIMORIO NO (él es el comensal, no el plato).
                if (it.ModItem is global::AethonMod.Content.Weapons.GrimoireEternal) return false;
                // LOS ACCESORIOS Y LA VANIDAD NO SON OFRENDA NI ARMA.
                if (it.accessory || it.vanity) return false;
                // LAS HERRAMIENTAS TAMPOCO (picos, hachas, martillos).
                if (it.pick > 0 || it.axe > 0 || it.hammer > 0) return false;
                // YA CONSAGRADO: NO (una vez sellado, para siempre).
                var sello = it.GetGlobalItem
                    <global::AethonMod.Content.Globals.CorruptoGlobalItem>();
                if (sello != null && sello.Corrompido) return false;
                // TODO LO DEMÁS ES OFRENDA: armas (daño > 0) y materiales
                // (lingotes, gemas, cera, fragmentos, almas…).
                return true;
            }
            catch { return false; }
        }

        /// <summary>v6.90 — ¿ESTE item es un ARMA? (para el texto del
        /// resultado: arma → stats + etiqueta · material → solo
        /// etiqueta).</summary>
        private static bool EsArmaDeVerdad(Item it)
            => it != null && !it.IsAir && it.damage > 0 && it.ammo == 0;

        private static bool DibujarElPanel()
        {
            try
            {
                if (!_abierta || Main.gameMenu || Main.netMode == NetmodeID.Server)
                    return true;

                Player yo = Main.LocalPlayer;
                if (yo == null || !yo.active) { CerrarDeVerdad(); return true; }

                SpriteBatch sb = Main.spriteBatch;
                Texture2D pixel = TextureAssets.MagicPixel.Value;

                // LA LEY DE LA DISTANCIA: el altar manda — sin altar (roto
                // o lejos), el panel se cierra solo.
                Tile t = Framing.GetTileSafely(_tileX, _tileY);
                bool altarVivo = t.HasTile && t.TileType ==
                    (ushort)ModContent.TileType<global::AethonMod.Content.Tiles.AltarOfrendasTile>();
                Vector2 altarCentro = new Vector2(_tileX * 16f + 16f, _tileY * 16f + 8f);
                if (!altarVivo || yo.Center.DistanceSQ(altarCentro) > 280f * 280f)
                { CerrarDeVerdad(); return true; }

                // === LA GEOMETRÍA (v6.91 — LA VENTANA DEL USUARIO:
                //     «la ventana del altar debes arreglar y optimizarla
                //     ya que los texto se desbordan… puedes usar la
                //     textura Espacio_1.png para mejorar el aspecto de la
                //     ventana»): más alta para SUS TRES SECCIONES (GANA ·
                //     EL SELLO · CÓMO CONSEGUIR ESENCIAS), el fondo de
                //     cielo estrellado y CADA LÍNEA MEDIDA — es
                //     matemáticamente imposible que un texto desborde
                //     (LineaMedida parte por palabras al ancho real). ===
                int w = 400, h = 576;
                int x = Math.Min(Main.screenWidth - w - 10,
                    470 + Math.Max(0, (Main.screenWidth - 1100) / 3));
                int y = Main.screenHeight / 2 - h / 2;
                if (x < 10) x = 10;
                if (y < 8) y = 8;
                if (y + h > Main.screenHeight - 8)
                    y = Math.Max(8, Main.screenHeight - 8 - h);

                // EL FONDO — EL CIELO ESTRELLADO Espacio_1.png (la letra:
                //     «puedes usar la textura Espacio_1.png para mejorar
                //     el aspecto de la ventana»): la textura cubre el
                //     panel y un velo oscuro encima deja respirar al
                //     texto. El marco doble de la casa, siempre.
                Texture2D cielo = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/Investigacion/Espacio_1").Value;
                sb.Draw(pixel, new Rectangle(x - 3, y - 3, w + 6, h + 6), TintaFondo * 0.94f);
                sb.Draw(cielo, new Rectangle(x, y, w, h), Color.White * 0.92f);
                sb.Draw(pixel, new Rectangle(x, y, w, h), TintaFondo * 0.60f);
                Marco(sb, pixel, new Rectangle(x - 3, y - 3, w + 6, h + 6), TintaVioleta * 0.80f);

                // EL TITULAR (v6.88 — el corruptor).
                Utils.DrawBorderString(sb, Language.GetTextValue(Clave("Titulo")),
                    new Vector2(x + w / 2f, y + 22f), TintaVioleta, 1.1f, 0.5f, 0.4f);
                Utils.DrawBorderString(sb, Language.GetTextValue(Clave("Subtitulo")),
                    new Vector2(x + w / 2f, y + 42f), TintaApagado, 0.75f, 0.5f, 0.4f);

                // === EL SLOT DE LA OFRENDA (la boca del consagrador) ===
                Rectangle slot = new Rectangle(x + 24, y + 86, 52, 52);
                bool hoverSlot = PanelContiene(slot) && !Terraria.GameInput.PlayerInput.IgnoreMouseInterface;
                sb.Draw(pixel, slot, TintaFondo * 0.9f);
                Marco(sb, pixel, slot, hoverSlot ? TintaVioleta : TintaVioleta * 0.45f);
                if (_arma != null && !_arma.IsAir)
                {
                    Texture2D tex = TextureAssets.Item[_arma.type].Value;
                    float escala = Math.Min(44f / tex.Width, 44f / tex.Height);
                    sb.Draw(tex, slot.Center.ToVector2(), null, Color.White, 0f,
                        tex.Size() * 0.5f, escala, SpriteEffects.None, 0f);
                    if (_arma.stack > 1)
                        Utils.DrawBorderString(sb, _arma.stack.ToString(),
                            new Vector2(slot.Right - 6f, slot.Bottom - 8f), TintaTexto, 0.85f, 1f, 0.85f);
                }
                else
                {
                    Utils.DrawBorderString(sb, Language.GetTextValue(Clave("SlotVacio")),
                        new Vector2(slot.Center.X, slot.Center.Y), TintaApagado * 0.8f, 0.7f, 0.5f, 0.5f);
                }
                Utils.DrawBorderString(sb, Language.GetTextValue(Clave("EtiquetaArma")),
                    new Vector2(x + 92f, y + 96f), TintaTexto, 0.9f);
                string filtroTxt = Language.GetTextValue(Clave("FiltroArma"));
                Utils.DrawBorderString(sb, filtroTxt, new Vector2(x + 92f, y + 118f), TintaApagado, 0.72f);

                // === v6.91 — EL ESTADO + LAS TRES SECCIONES DE LA
                //     VENTANA NUEVA (la letra: «lo que gana debe estar
                //     debajo de la palabra Gana… el altar es quien dice
                //     cuanto necesitas y lo que falta, ademas debe decir
                //     como conseguiras esencia»): GANA (las ganancias
                //     DEBAJO de la palabra) · EL SELLO (precio + llevas
                //     + LO QUE FALTA — la cuenta vive AQUÍ, no en el chat)
                //     · CÓMO CONSEGUIR ESENCIAS (la mecánica REAL:
                //     criaturas 0,1% · jefes 1-10 · las oleadas de hambre
                //     pagan MÁS — tal como caen de verdad, ver
                //     EsenciaCorruptaDrops.cs y OleadaNPC). ===
                bool lista = EsOfrendaConsagrible(_arma);
                bool esArma = EsArmaDeVerdad(_arma);
                int costeVivo = lista ? global::AethonMod.Content.Globals.CorruptoGlobalItem
                    .CosteCorrupcion(_arma) : 0;
                int llevadas = global::AethonMod.Content.Globals.CorruptoGlobalItem
                    .EsenciasEnLaBolsa(Main.LocalPlayer);
                if (_arma != null && !_arma.IsAir)
                {
                    var selloArma = _arma.GetGlobalItem
                        <global::AethonMod.Content.Globals.CorruptoGlobalItem>();
                    bool yaEra = selloArma != null && selloArma.Corrompido;
                    // v6.91 — LA CURA DEL «ListaArma»: la llamada perdía el
                    //     prefijo Mods.AethonMod.Altar. y el juego pintaba la
                    //     CLAVE cruda. Ahora TODAS pasan por Clave().
                    string estado = yaEra ? Language.GetTextValue(Clave("YaConsagrada"))
                        : lista ? Language.GetTextValue(Clave(esArma ? "ListaArma" : "ListaMaterial"))
                        : Language.GetTextValue(Clave("NoArma"));
                    Color colEstado = yaEra ? TintaVioleta
                        : lista ? TintaTeal : TintaRojo;
                    Utils.DrawBorderString(sb, estado,
                        new Vector2(x + 24f, y + 158f), colEstado, 0.85f);
                }

                float cy = y + 186f;

                // ── LA SECCIÓN 1 — GANA (lo que gana, DEBAJO de la
                //    palabra — la letra: «lo que gana debe estar debajo
                //    de la palabra Gana»). ──
                cy = TituloSeccion(sb, Language.GetTextValue(Clave("GanaTitulo")),
                    x, cy, w, TintaTeal);
                cy = LineaMedida(sb, Language.GetTextValue(Clave("GanaArma")),
                    x + 24f, cy, w - 48f, 0.78f, TintaTexto);
                cy = LineaMedida(sb, Language.GetTextValue(Clave("GanaMaterial")),
                    x + 24f, cy, w - 48f, 0.78f, TintaApagado);
                cy += 8f;

                // ── LA SECCIÓN 2 — EL SELLO (el precio A LA VISTA y LA
                //    CUENTA DEL ALTAR: cuánto cuesta, cuántas llevas y
                //    CUÁNTO FALTA — la letra: «el altar es quien dice
                //    cuanto necesitas y lo que falta»). ──
                cy = TituloSeccion(sb, Language.GetTextValue(Clave("SelloTitulo")),
                    x, cy, w, TintaVioleta);
                if (lista && !_arma.IsAir)
                {
                    bool alcanza = llevadas >= costeVivo;
                    cy = LineaMedida(sb, Language.GetTextValue(Clave("SelloCuesta"), costeVivo),
                        x + 24f, cy, w - 48f, 0.80f, TintaTexto);
                    cy = LineaMedida(sb, Language.GetTextValue(Clave("SelloLlevas"), llevadas),
                        x + 24f, cy, w - 48f, 0.80f, TintaApagado);
                    cy = LineaMedida(sb, Language.GetTextValue(
                            alcanza ? Clave("SelloAlcanza") : Clave("SelloFalta"),
                            Math.Max(costeVivo - llevadas, 0)),
                        x + 24f, cy, w - 48f, 0.80f,
                        alcanza ? TintaTeal : TintaRojo);
                }
                else
                {
                    cy = LineaMedida(sb, Language.GetTextValue(Clave("SelloSinOfrenda")),
                        x + 24f, cy, w - 48f, 0.78f, TintaApagado * 0.9f);
                }
                cy += 4f;

                // EL BOTÓN CONSAGRAR (el sello) — ahora debajo de SU
                // sección, con el hueco contado.
                Rectangle btnCorromper = new Rectangle(x + 24, (int)cy, w - 48, 42);
                cy += 42f + 10f;
                bool hoverBtn = PanelContiene(btnCorromper);
                sb.Draw(pixel, btnCorromper,
                    (hoverBtn && lista ? TintaVioleta : TintaVioleta * 0.35f) * 0.30f);
                Marco(sb, pixel, btnCorromper, lista ? TintaVioleta : TintaVioleta * 0.35f);
                Utils.DrawBorderString(sb, Language.GetTextValue(Clave("Consagrar")),
                    new Vector2(btnCorromper.Center.X, btnCorromper.Center.Y),
                    lista ? TintaTexto : TintaApagado, 1.0f, 0.5f, 0.5f);

                // ── LA SECCIÓN 3 — CÓMO CONSEGUIR ESENCIAS (la letra:
                //    «debe decir como conseguiras esencia, y las esencias
                //    se consiguen matando cualquier criatura de terraria
                //    cierta probabilidad al igual que matando jefes,
                //    ademas la probabilidad aumenta en las oleadas de
                //    hambre» — la MESA REAL del juego, sin adornos). ──
                cy = TituloSeccion(sb, Language.GetTextValue(Clave("ComoTitulo")),
                    x, cy, w, TintaApagado * 1.25f);
                cy = LineaMedida(sb, Language.GetTextValue(Clave("ComoCriaturas")),
                    x + 24f, cy, w - 48f, 0.75f, TintaTexto);
                cy = LineaMedida(sb, Language.GetTextValue(Clave("ComoJefes")),
                    x + 24f, cy, w - 48f, 0.75f, TintaTexto);
                cy = LineaMedida(sb, Language.GetTextValue(Clave("ComoOleadas")),
                    x + 24f, cy, w - 48f, 0.75f, TintaVioleta);
                cy = LineaMedida(sb, Language.GetTextValue(Clave("ComoNota")),
                    x + 24f, cy, w - 48f, 0.72f, TintaApagado * 0.9f);

                // LA LEYENDA DEL SELLO (v6.90 — para qué sirve: el
                // grimorio SOLO devora lo consagrado — la ofrenda del
                // despertar pasa por AQUÍ).
                Utils.DrawBorderString(sb, Language.GetTextValue(Clave("LeyendaSello")),
                    new Vector2(x + w / 2f, y + h - 34f), TintaApagado, 0.72f, 0.5f, 0.4f);
                Utils.DrawBorderString(sb, Language.GetTextValue(Clave("LeyendaSello2")),
                    new Vector2(x + w / 2f, y + h - 16f), TintaApagado * 0.85f, 0.72f, 0.5f, 0.4f);

                // === EL BOTÓN CERRAR (la esquina de siempre) ===
                Rectangle btnCerrar = new Rectangle(x + w - 34, y + 8, 26, 26);
                bool hoverCerrar = PanelContiene(btnCerrar);
                sb.Draw(pixel, btnCerrar, (hoverCerrar ? TintaRojo : TintaApagado) * 0.25f);
                Utils.DrawBorderString(sb, "X",
                    new Vector2(btnCerrar.Center.X, btnCerrar.Center.Y),
                    hoverCerrar ? TintaRojo : TintaApagado, 1.0f, 0.5f, 0.5f);

                // ============================================================
                //  LA INTERACCIÓN (el borde del clic — TODO con
                //  mouseInterface: el juego NO usa el item al clicar).
                // ============================================================
                bool clicAhora = Main.mouseLeft && !Terraria.GameInput.PlayerInput.IgnoreMouseInterface;
                bool clicNuevo = clicAhora && !_ultimoClicIzq;
                _ultimoClicIzq = clicAhora;

                // EL SLOT: meter/sacar con el cursor (armas Y materiales
                // limpios — el resto rebota con su porqué).
                if (hoverSlot && clicNuevo)
                {
                    if (!Main.mouseItem.IsAir)
                    {
                        if (EsOfrendaConsagrible(Main.mouseItem))
                        {
                            Item sale = _arma.IsAir ? new Item() : (Item)_arma.Clone();
                            _arma = (Item)Main.mouseItem.Clone();
                            Main.mouseItem = sale;
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Grab);
                        }
                        else
                        {
                            var selloMouse = Main.mouseItem.GetGlobalItem
                                <global::AethonMod.Content.Globals.CorruptoGlobalItem>();
                            bool yaEraMouse = selloMouse != null && selloMouse.Corrompido;
                            Main.NewText(Language.GetTextValue(
                                yaEraMouse ? Clave("YaConsagrada") : Clave("NoArma")), TintaRojo);
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                        }
                    }
                    else if (!_arma.IsAir)
                    {
                        Main.mouseItem = (Item)_arma.Clone();
                        _arma = new Item();
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Grab);
                    }
                }

                // EL BOTÓN CONSAGRAR: el sello de la casa (el MISMO de la
                // receta — Corrompido = true + ResetPrefix: el «Superior»
                // jamás vuelve) + el teatro.
                // v6.89 — EL PRECIO (la letra: la fórmula de la casa
                //     según calidad, tipo, daño y estadísticas — la de
                //     cobre pide menos que la de adamantita): el altar
                //     cobra CosteCorrupcion en Esencias del Eterno.
                // v6.90 — ARMA O MATERIAL: el arma gana stats + etiqueta;
                //     el material SOLO la etiqueta (el mensaje lo dice).
                if (hoverBtn && clicNuevo && lista && !_arma.IsAir)
                {
                    var sello = _arma.GetGlobalItem
                        <global::AethonMod.Content.Globals.CorruptoGlobalItem>();
                    if (sello != null)
                    {
                        int coste = global::AethonMod.Content.Globals.CorruptoGlobalItem
                            .CosteCorrupcion(_arma);
                        int llevaba = global::AethonMod.Content.Globals.CorruptoGlobalItem
                            .EsenciasEnLaBolsa(Main.LocalPlayer);
                        if (llevaba < coste)
                        {
                            // v6.91 — LA LETRA: «no es necesario llevar la
                            //     cuenta en el chat, para eso dejarlo en el
                            //     altar» — el chat SOLO AVISA; la cuenta
                            //     (cuánto cuesta, cuánto falta y cómo
                            //     conseguirlas) vive en LA VENTANA del
                            //     altar. (Y el bug viejo: «te faltan 0»
                            //     pintaba LO QUE LLEVABAS, no lo que
                            //     faltaba — la cuenta se fue del chat para
                            //     siempre.)
                            Main.NewText(Language.GetTextValue(Clave("FaltanEsencias")),
                                TintaRojo);
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                        }
                        else
                        {
                            global::AethonMod.Content.Globals.CorruptoGlobalItem
                                .PagarCorrupcion(Main.LocalPlayer, coste);
                            sello.Corrompido = true;
                            _arma.ResetPrefix();
                            global::AethonMod.Content.Globals.CorruptoGlobalItem
                                .VestirLaRareza(_arma);
                            // v6.90 — EL MENSAJE SEGUN LO QUE SEA: el arma
                            //     con SUS estadísticas; el material con SU
                            //     etiqueta (la letra: «si es un arma salen
                            //     estadisticas… si es otro item entonces
                            //     solo sale la etiqueta»).
                            Main.NewText(Language.GetTextValue(
                                EsArmaDeVerdad(_arma) ? Clave("ConsagradaHechaArma")
                                                      : Clave("ConsagradaHechaMaterial"),
                                _arma.Name), TintaVioleta);
                            Terraria.Audio.SoundEngine.PlaySound(
                                SoundID.Item113.WithPitchOffset(-0.45f));
                            // EL TEATRO DEL SELLO: la bruma violeta del altar.
                            if (!Main.dedServ)
                            {
                                for (int k = 0; k < 22; k++)
                                {
                                    Dust d = Dust.NewDustPerfect(
                                        altarCentro + new Vector2(Main.rand.NextFloat(-14f, 14f),
                                            Main.rand.NextFloat(-30f, 6f)),
                                        DustID.Shadowflame,
                                        Main.rand.NextVector2Circular(2.5f, 2.5f), 140,
                                        new Color(140, 60, 210), 1f);
                                    d.noGravity = true;
                                }
                            }
                            DevolverElArma();   // la ofrenda consagrada vuelve al cursor
                        }
                    }
                }

                // EL BOTÓN CERRAR / ESC (el borde del teclado).
                bool escAhora = Microsoft.Xna.Framework.Input.Keyboard
                    .GetState().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape);
                if ((hoverCerrar && clicNuevo) || (escAhora && !_ultimoEsc))
                    CerrarDeVerdad();
                _ultimoEsc = escAhora;

                return true;
            }
            catch { return true; }
        }

        /// <summary>v6.91 — EL TITULAR DE SECCIÓN de la ventana del altar:
        /// centrado, con sus filetes a los lados. Devuelve la Y siguiente.</summary>
        private static float TituloSeccion(SpriteBatch sb, string texto, int x, float cy, int w, Color c)
        {
            var fuente = FontAssets.MouseText.Value;
            float ancho = fuente.MeasureString(texto).X * 0.85f;
            float cx = x + w / 2f;
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle((int)(cx - ancho / 2f - 26f), (int)cy + 7, 18, 2), c * 0.45f);
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle((int)(cx + ancho / 2f + 8f), (int)cy + 7, 18, 2), c * 0.45f);
            Utils.DrawBorderString(sb, texto, new Vector2(cx, cy), c, 0.85f, 0.5f, 0.4f);
            return cy + 24f;
        }

        /// <summary>v6.91 — LA LÍNEA MEDIDA (la letra: «los texto se
        /// desbordan»): cada línea del altar se mide con la fuente REAL y
        /// si no cabe en maxAncho se PARTE POR PALABRAS — es matemáticamente
        /// imposible que un texto se salga del panel. Devuelve la Y
        /// siguiente.</summary>
        private static float LineaMedida(SpriteBatch sb, string texto, float x, float cy,
            float maxAncho, float escala, Color c)
        {
            var fuente = FontAssets.MouseText.Value;
            float altoLinea = fuente.MeasureString("Ag").Y * escala + 2f;
            var palabras = texto.Split(' ');
            var linea = new System.Text.StringBuilder();
            foreach (string pal in palabras)
            {
                string prueba = linea.Length == 0 ? pal : linea.ToString() + " " + pal;
                if (fuente.MeasureString(prueba).X * escala > maxAncho && linea.Length > 0)
                {
                    Utils.DrawBorderString(sb, linea.ToString(), new Vector2(x, cy), c, escala);
                    cy += altoLinea;
                    linea.Clear().Append(pal);
                }
                else
                {
                    linea.Clear().Append(prueba);
                }
            }
            if (linea.Length > 0)
            {
                Utils.DrawBorderString(sb, linea.ToString(), new Vector2(x, cy), c, escala);
                cy += altoLinea;
            }
            return cy + 3f;
        }

        /// <summary>¿El ratón está encima de este rect DEL PANEL? (y come
        /// el clic: mouseInterface — el juego no usa el item).</summary>
        private static bool PanelContiene(Rectangle r)
        {
            bool dentro = r.Contains(Main.mouseX, Main.mouseY);
            if (dentro)
            {
                Main.LocalPlayer.mouseInterface = true;
                return true;
            }
            return false;
        }

        /// <summary>EL MARCO de la casa (el doble filete de El Cielo).</summary>
        private static void Marco(SpriteBatch sb, Texture2D pixel, Rectangle r, Color c)
        {
            sb.Draw(pixel, new Rectangle(r.X, r.Y, r.Width, 2), c);
            sb.Draw(pixel, new Rectangle(r.X, r.Y + r.Height - 2, r.Width, 2), c);
            sb.Draw(pixel, new Rectangle(r.X, r.Y, 2, r.Height), c);
            sb.Draw(pixel, new Rectangle(r.X + r.Width - 2, r.Y, 2, r.Height), c);
        }
    }
}
