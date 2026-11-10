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
    //  v6.92 — LA CURA DEL DESBORDE REAL (la captura del playtest de la
    //  .91): el filtro junto al slot se CORTABA en el borde derecho
    //  (una línea sin medir) y las leyendas del sello quedaban COMIDAS
    //  por el borde inferior al chocar con CÓMO CONSEGUIR ESENCIAS (la
    //  altura fija 576 ya no daba abasto). DOS PASADAS: se MIDE todo
    //  con la fuente real y el panel NACE con la altura exacta; el
    //  estado y EL SELLO se reservan con su variante más alta (el
    //  panel no salta). Y LA CLAVE PERDIDA: el código pedía
    //  Altar.YaConsagrada pero el hjson decía YaCorrupta — el estado
    //  «ya consagrada» pintaba la CLAVE cruda (renombrada en los TRES
    //  idiomas).
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
        //  accesorios, ni el grimorio, ni el libro, ni lo ya consagrado.
        //  v6.95 — EL DINERO SALE DEL ALTAR (la letra nueva: «el dinero
        //  no se consagra, simplemente se deja caer»): las monedas ya
        //  NO son ofrenda — el Eterno las devora SIN sello, tal cual
        //  caen delante del portador (DespertarGrimorioSistema
        //  .ServirLaMesa); el altar consagra armas y materiales,
        //  nada más.
        // ================================================================
        private static bool EsOfrendaConsagrible(Item it)
        {
            try
            {
                if (it == null || it.IsAir) return false;
                // v6.121 — LAS ESENCIAS SÍ (la letra nueva: «intento
                // colocar 1000 esencias del eterno en el altar y dice
                // no apto»): la letra vieja las vedaba del hueco; ahora
                // SON LA OFRENDA DE LA PUERTA — entran como cualquier
                // ofrenda y al consagrarlas EL ETERNO LAS DEVORA ENTERAS
                // (la cuenta X/1000 sube con TODAS — ver el botón).
                if (it.type == ModContent.ItemType
                    <global::AethonMod.Content.Items.Altar.ItemCorrupto>()) return true;
                if (it.type == ModContent.ItemType
                    <global::AethonMod.Content.Items.Altar.LibroCorrupto>()) return false;
                // EL GRIMORIO NO (él es el comensal, no el plato).
                if (it.ModItem is global::AethonMod.Content.Weapons.GrimoireEternal) return false;
                // EL DINERO TAMPOCO (v6.95: «el dinero no se consagra,
                // simplemente se deja caer» — el Eterno lo devora sin
                // sello, directamente del suelo).
                if (it.type == ItemID.CopperCoin || it.type == ItemID.SilverCoin ||
                    it.type == ItemID.GoldCoin || it.type == ItemID.PlatinumCoin) return false;
                // LOS ACCESORIOS Y LA VANIDAD NO SON OFRENDA NI ARMA.
                if (it.accessory || it.vanity) return false;
                // LAS HERRAMIENTAS TAMPOCO (picos, hachas, martillos).
                if (it.pick > 0 || it.axe > 0 || it.hammer > 0) return false;
                // YA CONSAGRADO: NO (una vez sellado, para siempre).
                var sello = it.GetGlobalItem
                    <global::AethonMod.Content.Globals.CorruptoGlobalItem>();
                if (sello != null && sello.Corrompido) return false;
                // TODO LO DEMÁS ES OFRENDA: armas (daño > 0) y materiales
                // (lingotes, gemas, cera, fragmentos, almas…). Las monedas
                // se fueron con la v6.95: el dinero NO se consagra.
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

                // v6.119 — LA PUERTA SE APAGA CADA FRAME: si el botón al
                // interior no se dibuja (puerta cerrada o panel de otro
                // altar), su rect muere aquí — jamás un clic fantasma.
                _geoPuerta = Rectangle.Empty;

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

                // === LA GEOMETRÍA (v6.92 — LA CURA DEL DESBORDE REAL:
                //     la captura del playtest mostró DOS cortes — el
                //     texto del filtro junto al slot PARTIDO por el
                //     borde derecho (era UNA línea sin medir) y las
                //     leyendas del sello COMIDAS por el borde inferior
                //     al chocar con la última línea de CÓMO CONSEGUIR
                //     ESENCIAS: la altura FIJA (576) ya no daba abasto
                //     para las tres secciones envueltas. LA CURA
                //     ESTRUCTURAL — DOS PASADAS: primero se MIDE todo
                //     el contenido con la fuente real (sin dibujar) y
                //     el panel NACE con la altura exacta; después se
                //     dibuja con la MISMA función (imposible que
                //     diverjan). Altura ESTABLE por diseño: el estado
                //     y la sección EL SELLO se reservan con su
                //     variante MÁS ALTA — el panel no salta al meter
                //     la ofrenda). ===
                int w = 400;
                bool lista = EsOfrendaConsagrible(_arma);
                bool esArma = EsArmaDeVerdad(_arma);
                // v6.121 — LA OFRENDA QUE ES EL DINERO MISMO (la letra:
                // «intento colocar 1000 esencias del eterno en el altar
                // y dice no apto»): las ESENCIAS DEL ETERNO entran al
                // hueco como cualquier ofrenda — pero no se sellan: EL
                // ETERNO LAS DEVORA ENTERAS y la cuenta de la puerta
                // (X/1000) sube con TODAS. Su sello no cuesta nada:
                // ELLAS SON EL PAGO.
                bool esEsencias = lista && _arma != null && !_arma.IsAir && _arma.type ==
                    ModContent.ItemType<global::AethonMod.Content.Items.Altar.ItemCorrupto>();
                int costeVivo = lista && !esEsencias
                    ? global::AethonMod.Content.Globals.CorruptoGlobalItem
                        .CosteCorrupcion(_arma) : 0;
                int llevadas = global::AethonMod.Content.Globals.CorruptoGlobalItem
                    .EsenciasEnLaBolsa(Main.LocalPlayer);

                // PASADA 1 — LA MEDIDA: el fondo del contenido con la
                //     fuente REAL. La altura del panel ya no se
                //     adivina: se CALCULA.
                float altoContenido = FluirElContenido(null, 0, 0, w,
                    lista, esArma, costeVivo, llevadas);
                int h = (int)Math.Ceiling(altoContenido);
                if (h < 460) h = 460;                       // el suelo de la casa
                if (h > Main.screenHeight - 16) h = Main.screenHeight - 16;   // la pantalla manda

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

                // EL TITULAR (v6.88 — el corruptor · v6.92 — con la
                //     llave de la escala: si un titular nace más ancho
                //     que el panel, su escala baja hasta que quepa).
                string titTxt = Language.GetTextValue(Clave("Titulo"));
                string subTxt = Language.GetTextValue(Clave("Subtitulo"));
                Utils.DrawBorderString(sb, titTxt, new Vector2(x + w / 2f, y + 22f),
                    TintaVioleta, EscalaQueCabe(titTxt, 1.1f, w - 60f), 0.5f, 0.4f);
                Utils.DrawBorderString(sb, subTxt, new Vector2(x + w / 2f, y + 42f),
                    TintaApagado, EscalaQueCabe(subTxt, 0.75f, w - 40f), 0.5f, 0.4f);

                // === v6.92 — PASADA 2 — EL DIBUJO: TODO el contenido
                //     del panel (el slot con su texto MEDIDO — antes el
                //     filtro era UNA línea sola y la captura del
                //     playtest la mostró PARTIDA por el borde derecho —
                //     el estado, las TRES SECCIONES de la v6.91 y las
                //     LEYENDAS que ya NO viven en posiciones fijas: la
                //     captura las mostraba COMIDAS por el borde
                //     inferior al chocar con la última línea de CÓMO
                //     CONSEGUIR ESENCIAS) fluye por UNA SOLA función
                //     que dibuja (sb) o mide (null) — la misma
                //     geometría en las dos pasadas, imposible que
                //     diverjan. La cuenta del sello y las ganancias
                //     siguen DEBAJO de sus palabras (la letra de la
                //     v6.91). ===
                FluirElContenido(sb, x, y, w, lista, esArma, costeVivo, llevadas);

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
                // limpios — el resto rebota con su porqué). v6.92 — el
                // rectángulo vivo lo dejo grabado la pasada de dibujo
                // (_geoSlot): la posición fluye con el contenido.
                bool hoverSlotAhora = _geoSlot.Contains(Main.mouseX, Main.mouseY) &&
                    !Terraria.GameInput.PlayerInput.IgnoreMouseInterface;
                if (hoverSlotAhora && clicNuevo)
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
                bool hoverBtnAhora = _geoBtn.Contains(Main.mouseX, Main.mouseY);
                if (hoverBtnAhora && clicNuevo && lista && !_arma.IsAir)
                {
                    // ============================================================
                    // v6.121 — EL DEVORO DE LAS ESENCIAS (la letra: «intento
                    // colocar 1000 esencias del eterno en el altar y dice
                    // no apto»): LAS ESENCIAS DEL ETERNO ya entran al hueco
                    // — y al CONSAGRARLAS el Eterno no las sella: LAS
                    // DEVORA ENTERAS. TODAS suben a la cuenta de la
                    // puerta del mundo interior (EsenciasConsagradas):
                    // las mil en el hueco y LA PUERTA NACE. No vuelven al
                    // cursor: lo devorado es del Eterno.
                    // ============================================================
                    if (esEsencias)
                    {
                        int cuantas = _arma.stack;
                        bool yaAbiertaEsencias =
                            global::AethonMod.Content.Systems.SubmundoGrimorio.PuertaAbierta;
                        global::AethonMod.Content.Systems.SubmundoGrimorio.EsenciasConsagradas += cuantas;
                        // EL TEATRO DEL DEVORO: la bruma violeta se las traga.
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
                        Main.NewText(Language.GetTextValue(Clave("DevoradasHecha"),
                            cuantas,
                            global::AethonMod.Content.Systems.SubmundoGrimorio.EsenciasConsagradas,
                            global::AethonMod.Content.Systems.SubmundoGrimorio.EsenciasPuerta),
                            TintaVioleta);
                        Terraria.Audio.SoundEngine.PlaySound(
                            SoundID.Item113.WithPitchOffset(-0.45f));
                        if (!yaAbiertaEsencias &&
                            global::AethonMod.Content.Systems.SubmundoGrimorio.PuertaAbierta)
                        {
                            Main.NewText(Terraria.Localization.Language.GetTextValue(
                                "Mods.AethonMod.Submundo.PuertaAbiertaAnuncio"),
                                new Color(220, 170, 255));
                            Terraria.Audio.SoundEngine.PlaySound(
                                SoundID.Item113.WithPitchOffset(-0.7f));
                        }
                        _arma = new Item();   // DEVORADAS: el hueco queda vacío
                    }
                    else
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
                            // v6.119 — LA CUENTA DE LA PUERTA (la letra:
                            // «al consagrar 1000 esencias el altar se
                            // pueda ir al mundo interior»): cada esencia
                            // que el Eterno COBRA por un sello queda
                            // anotada en el altar — y el día que la
                            // cuenta cruza LAS MIL, EL ALTAR SE VUELVE
                            // PUERTA (el botón del interior nace en la
                            // ventana y el mundo entero se entera).
                            bool yaAbierta = global::AethonMod.Content.Systems.SubmundoGrimorio.PuertaAbierta;
                            global::AethonMod.Content.Systems.SubmundoGrimorio.EsenciasConsagradas += coste;
                            if (!yaAbierta && global::AethonMod.Content.Systems.SubmundoGrimorio.PuertaAbierta)
                            {
                                Main.NewText(Terraria.Localization.Language.GetTextValue(
                                    "Mods.AethonMod.Submundo.PuertaAbiertaAnuncio"),
                                    new Color(220, 170, 255));
                                Terraria.Audio.SoundEngine.PlaySound(
                                    SoundID.Item113.WithPitchOffset(-0.7f));
                            }
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
                    }   // v6.121 — fin del else del devoro (las esencias ya tuvieron su rama)
                }

                // ============================================================
                //  v6.119 — EL BOTÓN DE LA PUERTA AL MUNDO INTERIOR (la
                //  letra: «al consagrar 1000 esencias el altar se pueda
                //  ir al mundo interior»): EL ALTAR TRAGA igual que el
                //  ítem — LA MISMA DEVORACIÓN (DevoracionEterna: el
                //  libro baja, se abre, traga al portador SIN daño, la
                //  bruma inunda y el interior lo recibe con su
                //  revelación). El ítem «Mundo Interior del Eterno»
                //  SIGUE siendo la puerta de la bolsa: el altar es la
                //  puerta de la casa (y no se gasta NINGUNA de las dos).
                // ============================================================
                bool hoverPuertaAhora = _geoPuerta.Contains(Main.mouseX, Main.mouseY) &&
                    !Terraria.GameInput.PlayerInput.IgnoreMouseInterface;
                if (hoverPuertaAhora) Main.LocalPlayer.mouseInterface = true;
                if (hoverPuertaAhora && clicNuevo)
                {
                    Player portador = Main.LocalPlayer;
                    string razonPuerta;
                    if (!global::AethonMod.Content.Systems.SubmundoGrimorio
                        .PuedeEntrar(portador, out razonPuerta))
                    {
                        if (razonPuerta != null)
                            Main.NewText(razonPuerta, new Color(190, 160, 220));
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                    }
                    else
                    {
                        CerrarDeVerdad();   // el altar suelta la ventana: el libro toma el relevo
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74);
                        Projectile.NewProjectile(portador.GetSource_Misc("AltarMundoInterior"),
                            portador.Center, Vector2.Zero,
                            ModContent.ProjectileType
                                <global::AethonMod.Content.Projectiles.Definitivas.DevoracionEterna>(),
                            0, 0f, portador.whoAmI);
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

        // ================================================================
        //  v6.92 — EL FLUJO DEL PANEL (la cura del desborde): UNA SOLA
        //  función dibuja (sb != null) o MIDE (sb == null) todo el
        //  contenido del altar — del slot a las leyendas. La MISMA
        //  geometría en las dos pasadas: imposible que diverjan.
        //  Devuelve el fondo del contenido (la Y de la última línea +
        //  el respiro del borde inferior).
        //
        //  ALTURA ESTABLE POR DISEÑO: el ESTADO y la sección EL SELLO
        //  se reservan con su variante MÁS ALTA (la de con-ofrenda) —
        //  el panel NO salta al meter o sacar la ofrenda.
        // ================================================================
        /// <summary>Los rects vivos de la pasada de dibujo (el clic los lee).</summary>
        private static Rectangle _geoSlot;
        private static Rectangle _geoBtn;
        /// <summary>v6.119 — EL RECT de la PUERTA al mundo interior (solo
        /// vive cuando la puerta está abierta: Rectangle.Empty la apaga —
        /// un clic en un rect vacío jamás entra).</summary>
        private static Rectangle _geoPuerta;

        private static float FluirElContenido(SpriteBatch sb, int x, int y, int w,
            bool lista, bool esArma, int costeVivo, int llevadas)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            bool dibujar = sb != null;
            // v6.121 — ¿LA OFRENDA SON LAS ESENCIAS? (el devoro de la
            // puerta: el hueco las acepta y el Eterno las traga enteras).
            bool esEsencias = lista && _arma != null && !_arma.IsAir && _arma.type ==
                ModContent.ItemType<global::AethonMod.Content.Items.Altar.ItemCorrupto>();

            // ── EL SLOT DE LA OFRENDA (la boca del consagrador) ──
            Rectangle slot = new Rectangle(x + 24, y + 86, 52, 52);
            if (dibujar)
            {
                _geoSlot = slot;
                bool hoverSlot = PanelContiene(slot) &&
                    !Terraria.GameInput.PlayerInput.IgnoreMouseInterface;
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
            }

            // v6.92 — EL TEXTO DE LA OFRENDA, MEDIDO: antes la etiqueta y
            //     el filtro eran DOS líneas fijas sin medir y la captura
            //     del playtest mostró el filtro PARTIDO por el borde
            //     derecho. Ahora se parten por palabras al ancho REAL
            //     que deja el slot a la derecha.
            float anchoTexto = w - 92 - 16;
            float cy = LineaMedida(sb, Language.GetTextValue(Clave("EtiquetaArma")),
                x + 92f, y + 88f, anchoTexto, 0.88f, TintaTexto);
            cy = LineaMedida(sb, Language.GetTextValue(Clave("FiltroArma")),
                x + 92f, cy + 1f, anchoTexto, 0.66f, TintaApagado * 0.95f);

            // Las secciones nacen bajo el slot Y bajo el texto, lo que pida más.
            cy = Math.Max(cy, y + 138f) + 10f;

            // ── EL ESTADO (v6.92 — reservado SIEMPRE con su variante más
            //    alta: el panel no salta al meter la ofrenda). La cura del
            //    «ListaArma» (v6.91) sigue viva: TODAS las claves pasan
            //    por Clave(). ──
            float altoEstado = Math.Max(
                AltoMedido(Language.GetTextValue(Clave("ListaArma")), w - 48f, 0.85f),
                AltoMedido(Language.GetTextValue(Clave("YaConsagrada")), w - 48f, 0.85f));
            altoEstado = Math.Max(altoEstado,
                AltoMedido(Language.GetTextValue(Clave("ListaMaterial")), w - 48f, 0.85f));
            altoEstado = Math.Max(altoEstado,
                AltoMedido(Language.GetTextValue(Clave("NoArma")), w - 48f, 0.85f));
            altoEstado = Math.Max(altoEstado,
                AltoMedido(Language.GetTextValue(Clave("ListaEsencias")), w - 48f, 0.85f));
            if (dibujar && _arma != null && !_arma.IsAir)
            {
                var selloArma = _arma.GetGlobalItem
                    <global::AethonMod.Content.Globals.CorruptoGlobalItem>();
                bool yaEra = selloArma != null && selloArma.Corrompido;
                string estado = yaEra ? Language.GetTextValue(Clave("YaConsagrada"))
                    : lista ? Language.GetTextValue(Clave(
                          esEsencias ? "ListaEsencias"
                          : esArma ? "ListaArma" : "ListaMaterial"))
                    : Language.GetTextValue(Clave("NoArma"));
                Color colEstado = yaEra ? TintaVioleta
                    : lista ? (esEsencias ? TintaVioleta : TintaTeal) : TintaRojo;
                LineaMedida(sb, estado, x + 24f, cy, w - 48f, 0.85f, colEstado);
            }
            cy += altoEstado + 4f;

            // ── LA SECCIÓN 1 — GANA (lo que gana, DEBAJO de la palabra —
            //    la letra: «lo que gana debe estar debajo de la palabra
            //    Gana»). ──
            cy = TituloSeccion(sb, Language.GetTextValue(Clave("GanaTitulo")),
                x, cy, w, TintaTeal);
            cy = LineaMedida(sb, Language.GetTextValue(Clave("GanaArma")),
                x + 24f, cy, w - 48f, 0.78f, TintaTexto);
            cy = LineaMedida(sb, Language.GetTextValue(Clave("GanaMaterial")),
                x + 24f, cy, w - 48f, 0.78f, TintaApagado);
            // v6.121 — LA TERCERA GANANCIA: LAS ESENCIAS (la ofrenda que
            //     es el pago — el Eterno las devora por la puerta).
            cy = LineaMedida(sb, Language.GetTextValue(Clave("GanaEsencias"),
                    global::AethonMod.Content.Systems.SubmundoGrimorio.EsenciasPuerta),
                x + 24f, cy, w - 48f, 0.78f,
                esEsencias ? TintaVioleta : TintaApagado);
            cy += 8f;

            // ── LA SECCIÓN 2 — EL SELLO (el precio A LA VISTA y LA CUENTA
            //    DEL ALTAR: cuánto cuesta, cuántas llevas y CUÁNTO FALTA —
            //    la letra: «el altar es quien dice cuanto necesitas y lo
            //    que falta» · v6.92 — reservada con su variante MÁS ALTA
            //    (la de con-ofrenda): el panel no salta). ──
            cy = TituloSeccion(sb, Language.GetTextValue(Clave("SelloTitulo")),
                x, cy, w, TintaVioleta);
            bool alcanza = llevadas >= costeVivo;
            float altoConOfrenda = Math.Max(Math.Max(
                AltoMedido(Language.GetTextValue(Clave("SelloCuesta"), costeVivo), w - 48f, 0.80f),
                AltoMedido(Language.GetTextValue(Clave("SelloLlevas"), llevadas), w - 48f, 0.80f)),
                AltoMedido(Language.GetTextValue(
                    alcanza ? Clave("SelloAlcanza") : Clave("SelloFalta"),
                    Math.Max(costeVivo - llevadas, 0)), w - 48f, 0.80f));
            // v6.121 — EL DEVORO también se mide (la variante más ancha:
            // 9999 esencias — la pila del hueco puede ser enorme).
            altoConOfrenda = Math.Max(altoConOfrenda,
                AltoMedido(Language.GetTextValue(Clave("DevoraLinea"),
                    9999,
                    global::AethonMod.Content.Systems.SubmundoGrimorio.EsenciasConsagradas,
                    global::AethonMod.Content.Systems.SubmundoGrimorio.EsenciasPuerta),
                    w - 48f, 0.80f));
            float altoSinOfrenda = AltoMedido(Language.GetTextValue(Clave("SelloSinOfrenda")),
                w - 48f, 0.78f);
            if (dibujar)
            {
                if (lista && !_arma.IsAir)
                {
                    if (esEsencias)
                    {
                        // v6.121 — EL DEVORO A LA VISTA: lo que el hueco
                        //     trae y a dónde va — la cuenta de la puerta.
                        LineaMedida(sb, Language.GetTextValue(Clave("DevoraLinea"),
                            _arma.stack,
                            global::AethonMod.Content.Systems.SubmundoGrimorio.EsenciasConsagradas,
                            global::AethonMod.Content.Systems.SubmundoGrimorio.EsenciasPuerta),
                            x + 24f, cy, w - 48f, 0.80f, TintaVioleta);
                    }
                    else
                    {
                    LineaMedida(sb, Language.GetTextValue(Clave("SelloCuesta"), costeVivo),
                        x + 24f, cy, w - 48f, 0.80f, TintaTexto);
                    LineaMedida(sb, Language.GetTextValue(Clave("SelloLlevas"), llevadas),
                        x + 24f, cy, w - 48f, 0.80f, TintaApagado);
                    LineaMedida(sb, Language.GetTextValue(
                            alcanza ? Clave("SelloAlcanza") : Clave("SelloFalta"),
                            Math.Max(costeVivo - llevadas, 0)),
                        x + 24f, cy, w - 48f, 0.80f,
                        alcanza ? TintaTeal : TintaRojo);
                    }
                }
                else
                {
                    LineaMedida(sb, Language.GetTextValue(Clave("SelloSinOfrenda")),
                        x + 24f, cy, w - 48f, 0.78f, TintaApagado * 0.9f);
                }
            }
            cy += Math.Max(altoConOfrenda, altoSinOfrenda) + 4f;

            // EL BOTÓN CONSAGRAR (el sello) — debajo de SU sección, con el
            // hueco contado (v6.92: el rect vive en _geoBtn para el clic).
            Rectangle btnCorromper = new Rectangle(x + 24, (int)cy, w - 48, 42);
            if (dibujar)
            {
                _geoBtn = btnCorromper;
                bool hoverBtn = PanelContiene(btnCorromper);
                sb.Draw(pixel, btnCorromper,
                    (hoverBtn && lista ? TintaVioleta : TintaVioleta * 0.35f) * 0.30f);
                Marco(sb, pixel, btnCorromper, lista ? TintaVioleta : TintaVioleta * 0.35f);
                Utils.DrawBorderString(sb, Language.GetTextValue(
                        Clave(esEsencias ? "Devorar" : "Consagrar")),
                    new Vector2(btnCorromper.Center.X, btnCorromper.Center.Y),
                    lista ? TintaTexto : TintaApagado, 1.0f, 0.5f, 0.5f);
            }
            cy += 42f + 10f;

            // ── v6.119 — LA SECCIÓN DE LA PUERTA — EL MUNDO INTERIOR (la
            //    letra: «al consagrar 1000 esencias el altar se pueda ir
            //    al mundo interior»): la cuenta VIVA de lo consagrado en
            //    el altar (X/1000, persistida con el mundo) y, al llegar
            //    a las mil, EL BOTÓN QUE TRAGA — la misma DEVORACIÓN del
            //    ítem, nacida del altar. La geometría fluye: el botón
            //    solo existe cuando la puerta está abierta (y la pasada
            //    de medida reserva EXACTAMENTE lo mismo que dibuja). ──
            cy = TituloSeccion(sb, Language.GetTextValue(Clave("MundoTitulo")),
                x, cy, w, new Color(210, 150, 255));
            bool puertaAbierta = global::AethonMod.Content.Systems.SubmundoGrimorio.PuertaAbierta;
            if (puertaAbierta)
            {
                cy = LineaMedida(sb, Language.GetTextValue(Clave("MundoAbierta")),
                    x + 24f, cy, w - 48f, 0.78f, TintaVioleta);
                Rectangle btnPuerta = new Rectangle(x + 24, (int)cy, w - 48, 42);
                if (dibujar)
                {
                    _geoPuerta = btnPuerta;
                    bool hoverPuerta = PanelContiene(btnPuerta);
                    sb.Draw(pixel, btnPuerta,
                        (hoverPuerta ? new Color(210, 150, 255) : TintaVioleta * 0.6f) * 0.35f);
                    Marco(sb, pixel, btnPuerta,
                        hoverPuerta ? new Color(230, 190, 255) : TintaVioleta * 0.7f);
                    Utils.DrawBorderString(sb, Language.GetTextValue(Clave("MundoPuerta")),
                        new Vector2(btnPuerta.Center.X, btnPuerta.Center.Y),
                        TintaTexto, 1.0f, 0.5f, 0.5f);
                }
                cy += 42f + 10f;
            }
            else
            {
                cy = LineaMedida(sb, Language.GetTextValue(Clave("MundoProgreso"),
                        global::AethonMod.Content.Systems.SubmundoGrimorio.EsenciasConsagradas,
                        global::AethonMod.Content.Systems.SubmundoGrimorio.EsenciasPuerta),
                    x + 24f, cy, w - 48f, 0.78f, TintaApagado);
                cy = LineaMedida(sb, Language.GetTextValue(Clave("MundoPista")),
                    x + 24f, cy, w - 48f, 0.72f, TintaApagado * 0.9f);
            }

            // ── LA SECCIÓN 3 — CÓMO CONSEGUIR ESENCIAS (la mesa REAL del
            //    juego: criaturas 0,1% · jefes 1-10 · ¡las OLEADAS DE
            //    HAMBRE suben la probabilidad! — tal como caen de
            //    verdad, ver EsenciaCorruptaDrops.cs y OleadaNPC). ──
            cy = TituloSeccion(sb, Language.GetTextValue(Clave("ComoTitulo")),
                x, cy, w, TintaApagado * 1.25f);
            cy = LineaMedida(sb, Language.GetTextValue(Clave("ComoCriaturas")),
                x + 24f, cy, w - 48f, 0.75f, TintaTexto);
            cy = LineaMedida(sb, Language.GetTextValue(Clave("ComoJefes")),
                x + 24f, cy, w - 48f, 0.75f, TintaTexto);
            cy = LineaMedida(sb, Language.GetTextValue(Clave("ComoOleadas")),
                x + 24f, cy, w - 48f, 0.75f, TintaVioleta);
            // v6.93 — LA LEY DE LAS ESTATUAS (la letra: «también en el
            //    altar tienes que decir que si el grimorio mata criatura
            //    que salen de una estatua su hambre aumenta»): la línea
            //    de la mesa de las estatuas, junto a las demás verdades.
            cy = LineaMedida(sb, Language.GetTextValue(Clave("ComoEstatuas")),
                x + 24f, cy, w - 48f, 0.75f, new Color(235, 130, 120));
            cy = LineaMedida(sb, Language.GetTextValue(Clave("ComoNota")),
                x + 24f, cy, w - 48f, 0.72f, TintaApagado * 0.9f);
            cy += 6f;

            // ── LA LEYENDA DEL SELLO (v6.90 — para qué sirve: el grimorio
            //    SOLO devora lo consagrado · v6.92 — AHORA FLUYE: la
            //    captura del playtest la mostraba PARTIDA por el borde
            //    inferior al chocar con la última línea de CÓMO
            //    CONSEGUIR ESENCIAS — las leyendas ya NO viven en
            //    posiciones fijas, siguen al contenido y se parten
            //    centradas si hiciera falta). ──
            cy = LineaMedida(sb, Language.GetTextValue(Clave("LeyendaSello")),
                x + w / 2f, cy, w - 40f, 0.72f, TintaApagado, true);
            cy = LineaMedida(sb, Language.GetTextValue(Clave("LeyendaSello2")),
                x + w / 2f, cy, w - 40f, 0.72f, TintaApagado * 0.85f, true);

            return cy + 8f;   // el respiro del borde inferior
        }

        /// <summary>v6.91 — EL TITULAR DE SECCIÓN de la ventana del altar:
        /// centrado, con sus filetes a los lados. v6.92 — con sb == null
        /// SOLO mide (la pasada 1 del panel). Devuelve la Y siguiente.</summary>
        private static float TituloSeccion(SpriteBatch sb, string texto, int x, float cy, int w, Color c)
        {
            if (sb != null)
            {
                var fuente = FontAssets.MouseText.Value;
                float ancho = fuente.MeasureString(texto).X * 0.85f;
                float cx = x + w / 2f;
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle((int)(cx - ancho / 2f - 26f), (int)cy + 7, 18, 2), c * 0.45f);
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle((int)(cx + ancho / 2f + 8f), (int)cy + 7, 18, 2), c * 0.45f);
                Utils.DrawBorderString(sb, texto, new Vector2(cx, cy), c, 0.85f, 0.5f, 0.4f);
            }
            return cy + 24f;
        }

        /// <summary>v6.91 — LA LÍNEA MEDIDA (la letra: «los texto se
        /// desbordan»): cada línea del altar se mide con la fuente REAL y
        /// si no cabe en maxAncho se PARTE POR PALABRAS — es matemáticamente
        /// imposible que un texto se salga del panel. v6.92 — DOS MODOS:
        /// con sb == null SOLO mide (la pasada 1); con centrado = true
        /// cada línea parte se centra alrededor de x (las leyendas).
        /// Devuelve la Y siguiente.</summary>
        private static float LineaMedida(SpriteBatch sb, string texto, float x, float cy,
            float maxAncho, float escala, Color c, bool centrado = false)
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
                    DibujarLaLinea(sb, linea.ToString(), x, cy, escala, c, centrado);
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
                DibujarLaLinea(sb, linea.ToString(), x, cy, escala, c, centrado);
                cy += altoLinea;
            }
            return cy + 3f;
        }

        /// <summary>El pincel de LineaMedida (sb == null = pasada de
        /// medida: no se dibuja NADA).</summary>
        private static void DibujarLaLinea(SpriteBatch sb, string texto, float x, float cy,
            float escala, Color c, bool centrado)
        {
            if (sb == null) return;
            float dx = x;
            if (centrado)
                dx = x - FontAssets.MouseText.Value.MeasureString(texto).X * escala * 0.5f;
            Utils.DrawBorderString(sb, texto, new Vector2(dx, cy), c, escala);
        }

        /// <summary>El ALTO medido de un texto partido por palabras (la
        /// medida pura de la pasada 1 — sin dibujar).</summary>
        private static float AltoMedido(string texto, float maxAncho, float escala)
            => LineaMedida(null, texto, 0f, 0f, maxAncho, escala, Color.White);

        /// <summary>v6.92 — LA ESCALA QUE CABE: si un titular nace más
        /// ancho que el panel, su escala baja hasta que quepa — jamás se
        /// corta por los lados.</summary>
        private static float EscalaQueCabe(string texto, float escala, float maxAncho)
        {
            var fuente = FontAssets.MouseText.Value;
            float e = escala;
            while (e > 0.55f && fuente.MeasureString(texto).X * e > maxAncho)
                e -= 0.05f;
            return e;
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
