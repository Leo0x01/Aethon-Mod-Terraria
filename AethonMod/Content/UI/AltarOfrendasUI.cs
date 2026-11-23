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
        private static bool _ultimoP;

        /// <summary>¿El panel está abierto?</summary>
        public static bool Abierta => _abierta;

        /// <summary>v6.164 — EL CIERRE POR ORDEN DEL LIBRO (ESC/X en
        /// la pestaña altar): suelta el arma del slot como siempre.</summary>
        public static void ForzarCierre() => CerrarDeVerdad();

        // ================================================================
        //  v6.163 — EL ALTAR VIVE EN EL LIBRO (la letra: «puedes usar
        //  la misma ventana que usas para el libro de informacion en
        //  el altar»): el panel FLOTANTE murió — el contenido se
        //  dibuja como PESTAÑA del libro (AltarDelLibro). Este
        //  sistema sigue vivo como EL MOTOR: la ley de distancia, el
        //  slot de la ofrenda (_arma), el sello y TODA la lógica de
        //  clic. El libro llama; el altar hace.
        // ================================================================

        /// <summary>La ofrenda viva del altar (la que dibuja el libro).</summary>
        public static Item Ofrenda => _arma;

        /// <summary>¿Es consagrible? (la puerta pública para el libro).</summary>
        public static bool EsOfrendaConsagriblePublica(Item it) => EsOfrendaConsagrible(it);

        /// <summary>v6.164 — LOS CLICS GRANULARES DEL ALTAR (la cura de los
/// botones muertos: las páginas del libro llaman a ESTOS métodos con
/// el flanco de la casa — cada acción, su función).</summary>
        public static void ClicEnSlot()
        {
            try
            {
                Player yo = Main.LocalPlayer;
                if (yo == null || !yo.active || !_abierta) return;

                if (!Main.mouseItem.IsAir && Main.mouseItem.type == ModContent.ItemType
                    <global::AethonMod.Content.Items.Altar.ItemCorrupto>())
                {
                    // EL DEPÓSITO DE LAS MIL (el devoro con tope).
                    int topePuerta = global::AethonMod.Content.Systems.SubmundoGrimorio.EsenciasPuerta;
                    int yaConsagradas = global::AethonMod.Content.Systems.SubmundoGrimorio.EsenciasConsagradas;
                    int falta = topePuerta - yaConsagradas;
                    if (falta <= 0)
                    {
                        Main.NewText(Language.GetTextValue(Clave("PuertaYaAbierta")), TintaTeal);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                    }
                    else
                    {
                        int toma = Math.Min(falta, Main.mouseItem.stack);
                        bool yaAbierta = global::AethonMod.Content.Systems.SubmundoGrimorio.PuertaAbierta;
                        global::AethonMod.Content.Systems.SubmundoGrimorio.EsenciasConsagradas += toma;
                        Main.NewText(Language.GetTextValue(Clave("PuertaBebe"), toma,
                            global::AethonMod.Content.Systems.SubmundoGrimorio.EsenciasConsagradas,
                            topePuerta), TintaVioleta);
                        if (!yaAbierta && global::AethonMod.Content.Systems.SubmundoGrimorio.PuertaAbierta)
                        {
                            Main.NewText(Terraria.Localization.Language.GetTextValue(
                                "Mods.AethonMod.Submundo.PuertaAbiertaAnuncio"),
                                new Color(220, 170, 255));
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item113.WithPitchOffset(-0.7f));
                        }
                        else Terraria.Audio.SoundEngine.PlaySound(SoundID.Item113.WithPitchOffset(-0.25f));
                        Main.mouseItem.stack -= toma;
                        if (Main.mouseItem.stack <= 0) Main.mouseItem.TurnToAir();
                        if (!Main.dedServ)
                            for (int k = 0; k < Math.Min(30, 8 + toma / 40); k++)
                            {
                                Dust d = Dust.NewDustPerfect(
                                    new Vector2(_tileX * 16f + 16f, _tileY * 16f + 4f) +
                                        new Vector2(Main.rand.NextFloat(-14f, 14f), Main.rand.NextFloat(-30f, 4f)),
                                    DustID.Shadowflame, Main.rand.NextVector2Circular(2.2f, 2.2f), 140,
                                    new Color(140, 60, 210), 1f);
                                d.noGravity = true;
                            }
                    }
                }
                else if (!Main.mouseItem.IsAir)
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
                else
                {
                    // v6.167 — LA BANDEJA DE LA OFRENDA (la cura del
                    // «slot de ofrendas no funciona»): clic en el slot
                    // VACÍO con cursor VACÍO → los candidatos (con LAS
                    // ESENCIAS de la llave) se despliegan debajo.
                    AbrirSelector(-2);
                }
            }
            catch { }
        }

        /// <summary>EL CLIC EN UNA RANURA DEL TRÍO: inserta / extrae /
        /// ELIGE (v6.163: la elección vive AQUÍ — el arma ya no elige).</summary>
        // ================================================================
        //  v6.166 — EL SELECTOR DE INVENTARIO (la letra: «cuando toco un
        //  slot para colocar un arma, quiero que debajo se muestren los
        //  iconos de armas compatibles que yo tenga en el inventario […]
        //  y en el slot de consagrar […] debes mostrar justo debajo todo
        //  lo que se puede consagrar que el jugador tenga en el
        //  inventario»): al TOCAR una ranura vacía (o el slot de la
        //  ofrenda), la bandeja se abre DEBAJO con los candidatos del
        //  inventario — un clic en un icono lo coloca. En las ranuras
        //  del trío SOLO entran ARMAS CONSAGRADAS (la letra: «las
        //  únicas armas que se pueden colocar en esos slot son armas
        //  consagradas, si el arma no esta consagrada no se colocara y
        //  ademas debes dejar en claro que las armas deben consagrarse
        //  para ser colocadas ahi»).
        // ================================================================
        /// <summary>La bandeja abierta: null = cerrada. Item1 = para qué
        /// es (-1 ranura trío 0-2 · -2 slot de consagrar).</summary>
        internal static (int ranura, float expira)? SelectorAbierto;

        /// <summary>¿ESTE item es un CANDIDATO para la bandeja? En las
        /// ranuras del trío: arma VÁLIDA (mágica, con daño — la regla de
        /// EsArmaValida) Y CONSAGRADA (Corrompido). En el slot de la
        /// ofrenda: cualquier item consagrabLE (todo lo que no esté ya
        /// consagrado).</summary>
        internal static bool EsCandidato(Item it, bool paraTrío)
        {
            if (it == null || it.IsAir) return false;
            bool sello = false;
            try
            {
                var g = it.GetGlobalItem<global::AethonMod.Content.Globals.CorruptoGlobalItem>();
                sello = g != null && g.Corrompido;
            }
            catch { }
            if (paraTrío)
                return sello && global::AethonMod.Content.Weapons.Investigaciones
                    .TrioDelEterno.EsArmaValida(it);
            // EL SLOT DE CONSAGRAR: todo lo que el altar acepta (armas Y
            // materiales — la letra de la v6.90) que aún NO esté sellado.
            // v6.167 — LAS ESENCIAS DEL ETERNO ENTRE LAS CANDIDATAS (la
            // letra: «una ofrenda que debe incluir entre las que se
            // muestra son las esencias del eterno, que no se consagran,
            // solo son la llave al mundo interior»): entran SIEMPRE —
            // el altar las DEVORA directo (SU flujo de las mil, de
            // poco a poco o las 1000 de golpe).
            if (it.type == ModContent.ItemType
                <global::AethonMod.Content.Items.Altar.ItemCorrupto>())
                return true;
            return !sello && (it.damage > 0 || it.ammo > 0 || it.createTile >= 0 ||
                it.createWall >= 0 || it.material || it.consumable ||
                it.healLife > 0 || it.healMana > 0 || it.buffType > 0 ||
                it.pick > 0 || it.axe > 0 || it.hammer > 0);
        }

        /// <summary>LA COSECHA del inventario: los candidatos VIVOS (sin
        /// duplicar tipo) en orden de ranura.</summary>
        internal static System.Collections.Generic.List<Item> CosecharCandidatos(bool paraTrío)
        {
            var lista = new System.Collections.Generic.List<Item>();
            var vistos = new System.Collections.Generic.HashSet<int>();
            Player yo = Main.LocalPlayer;
            if (yo == null) return lista;
            for (int k = 0; k < 58; k++)
            {
                Item it = yo.inventory[k];
                if (!EsCandidato(it, paraTrío)) continue;
                if (!vistos.Add(it.type)) continue;   // un icono por tipo
                lista.Add(it);
            }
            return lista;
        }

        /// <summary>LA BANDEJA SE ABRE (clic en ranura vacía / en el slot
        /// de ofrenda sin item al cursor).</summary>
        public static void AbrirSelector(int ranura)
        {
            SelectorAbierto = (ranura, Main.GlobalTimeWrappedHourly + 20f);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
        }

        /// <summary>EL CLIC EN UN ICONO de la bandeja: lo coloca (o lo
        /// consagra primero si es el slot de ofrenda — no: el slot
        /// consagra con SU botón; la bandeja SOLO coloca la ofrenda).</summary>
        public static void ClicEnCandidato(int indiceCandidato)
        {
            try
            {
                if (SelectorAbierto == null) return;
                int ranura = SelectorAbierto.Value.ranura;
                bool paraTrío = ranura >= 0;
                var candidatos = CosecharCandidatos(paraTrío);
                if (indiceCandidato < 0 || indiceCandidato >= candidatos.Count) return;
                Item elegido = candidatos[indiceCandidato];

                if (paraTrío)
                {
                    var trio = global::AethonMod.Content.Weapons.Investigaciones
                        .TrioDelEterno.Del(Main.LocalPlayer);
                    if (trio == null) return;
                    // LA DOBLE PUERTA de la letra: válida Y CONSAGRADA
                    if (!global::AethonMod.Content.Weapons.Investigaciones
                        .TrioDelEterno.EsArmaValida(elegido))
                    {
                        Main.NewText(Language.GetTextValue("Mods.AethonMod.Items.TrioNoArma"),
                            new Color(255, 170, 130));
                        return;
                    }
                    bool sellada = false;
                    try
                    {
                        var g = elegido.GetGlobalItem
                            <global::AethonMod.Content.Globals.CorruptoGlobalItem>();
                        sellada = g != null && g.Corrompido;
                    }
                    catch { }
                    if (!sellada)
                    {
                        // LA LEY DE LA LETRA: «debes dejar en claro que
                        // las armas deben consagrarse para ser colocadas
                        // ahi»
                        Main.NewText(Language.GetTextValue(
                            "Mods.AethonMod.Items.TrioRequiereConsagrada"),
                            new Color(255, 170, 130));
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                        return;
                    }
                    // v6.167 — SIN DUPLICACIÓN (la letra: «cuando el
                    // arma se equipa en el slot esta debe dejar el
                    // inventario del jugador ya que ahora esta en el
                    // slot del altar»): la copia va a la ranura y EL
                    // ORIGINAL MUERE en el inventario (stack incluido:
                    // si llevaba 2, la ranura sostiene 1 y el otro
                    // vuelve al inventario).
                    if (elegido.stack > 1)
                    {
                        elegido.stack--;   // el sobreviviente queda en SU ranura
                    }
                    else
                    {
                        // el único ejemplar viaja: SU ranura muere
                        for (int k = 0; k < 58; k++)
                        {
                            if (Main.LocalPlayer.inventory[k] == elegido)
                            {
                                Main.LocalPlayer.inventory[k].TurnToAir();
                                break;
                            }
                        }
                    }
                    Item paraRanura = (Item)elegido.Clone();
                    paraRanura.stack = 1;
                    trio.Ranuras[ranura] = paraRanura;
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item85);
                    Main.NewText(Language.GetTextValue(
                        "Mods.AethonMod.Items.TrioInsertada", ranura + 1),
                        new Color(160, 230, 255));
                }
                else
                {
                    // EL SLOT DE LA OFRENDA: el item viaja al slot (la
                    // consagración la paga SU botón). v6.167 — SIN
                    // DUPLICACIÓN (la misma ley del trío): si había una
                    // ofrenda anterior, VUELVE al inventario primero.
                    // v6.168 — LA VUELTA ES DIRECTA (AdoptarItem — la
                    // cura de las desaparecidas: INSERCIÓN, no spawn).
                    if (_arma != null && !_arma.IsAir)
                        AdoptarItem(_arma);
                    _arma = (Item)elegido.Clone();
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Grab);
                }
                SelectorAbierto = null;   // la bandeja se cierra al elegir
            }
            catch { }
        }

        public static void ClicEnRanura(int slotT)
        {
            try
            {
                var trio = global::AethonMod.Content.Weapons.Investigaciones
                    .TrioDelEterno.Del(Main.LocalPlayer);
                if (trio == null || slotT < 0 || slotT > 2) return;

                Item armaSlot = trio.Ranuras != null && slotT < trio.Ranuras.Length
                    ? trio.Ranuras[slotT] : null;
                if (!Main.mouseItem.IsAir)
                {
                    if (global::AethonMod.Content.Weapons.Investigaciones
                        .TrioDelEterno.EsArmaValida(Main.mouseItem))
                    {
                        int destino = (armaSlot == null || armaSlot.IsAir) ? slotT : -1;
                        if (destino < 0)
                            for (int k = 0; k < 3; k++)
                            {
                                Item rk = trio.Ranuras[k];
                                if (rk == null || rk.IsAir) { destino = k; break; }
                            }
                        if (destino >= 0)
                        {
                            trio.Ranuras[destino] = (Item)Main.mouseItem.Clone();
                            Main.mouseItem.TurnToAir();
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item85);
                            Main.NewText(Language.GetTextValue(
                                "Mods.AethonMod.Items.TrioInsertada", destino + 1),
                                new Color(160, 230, 255));
                        }
                        else Main.NewText(Language.GetTextValue(
                            "Mods.AethonMod.Items.TrioLleno"), new Color(255, 170, 130));
                    }
                    else
                    {
                        Main.NewText(Language.GetTextValue(
                            "Mods.AethonMod.Items.TrioNoArma"), new Color(255, 170, 130));
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                    }
                }
                else if (armaSlot != null && !armaSlot.IsAir)
                {
                    // CLIC IZQUIERDO en ranura llena con cursor vacío: LA
                    // ELIGE. CLIC DERECHO: la extrae al cursor.
                    if (Main.mouseRight)
                    {
                        // v6.167 — LA VUELTA AL INVENTARIO (la letra: «si
                        // el jugador desequipa el arma con click derecho,
                        // esta entonces vuelve al inventario»): el arma
                        // regresa AL INVENTARIO. v6.168 — LA CURA DE LAS
                        // DESAPARECIDAS: QuickSpawnItem (spawn al suelo)
                        // muere — la vuelta es UNA INSERCIÓN DIRECTA
                        // (AdoptarItem: apila, entra en el hueco, y solo
                        // de último recurso cae — con aviso).
                        Item devolver = (Item)armaSlot.Clone();
                        devolver.stack = 1;
                        trio.Ranuras[slotT] = new Item();
                        AdoptarItem(devolver);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Grab);
                    }
                    else
                    {
                        trio.ModoDisparo = slotT;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                        Main.NewText(Language.GetTextValue(
                            "Mods.AethonMod.Items.TrioModoUna", slotT + 1),
                            new Color(160, 230, 255));
                    }
                }
                else
                {
                    // v6.166 — LA BANDEJA (la letra: «cuando toco un slot
                    // para colocar un arma, quiero que debajo se muestren
                    // los iconos de armas compatibles que yo tenga en el
                    // inventario»): clic en la ranura con cursor vacío →
                    // los candidatos (armas CONSAGRADAS) se despliegan
                    // DEBAJO. La ranura también ELIGE el modo (lo de
                    // siempre) — la bandeja es el extra.
                    AbrirSelector(slotT);
                    trio.ModoDisparo = slotT;
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                    Main.NewText(Language.GetTextValue(
                        "Mods.AethonMod.Items.TrioModoUna", slotT + 1),
                        new Color(160, 230, 255));
                }
            }
            catch { }
        }

        /// <summary>v6.167 — EL CLIC EN «DESEQUIPAR TODO» (la letra:
        /// «que haya un boton que permita desequiparlo todo»): las tres
        /// ranuras DEVUELVEN sus armas al inventario — una por una, sin
        /// perder NINGUNA (el camino de la extracción de siempre).</summary>
        /// <summary>v6.168 — LA ENTREGA DIRECTA AL INVENTARIO (la cura de
        /// las armas desaparecidas: la letra: «al equipar y desequipar
        /// las armas en la mesa de fusion, estas desaparecieron por
        /// completo»). LA AUTOPSIA: los caminos del trío devolvían el
        /// arma con QuickSpawnItem — un SPAWN AL SUELO con vida propia
        /// (busca hueco, y si el inventario está lleno o el mouse
        /// interface lo interfiere, el item VIVE EN EL SUELO, donde un
        /// jugador que no mira abajo lo pierde de vista). LA CURA: la
        /// devolución es UNA INSERCIÓN DIRECTA — apila en una pila
        /// igual, entra en el primer hueco libre, y SOLO si no cabe
        /// en NINGUNA parte cae al suelo (QuickSpawnItem como ÚLTIMO
        /// recurso). El arma vuelve AL INVENTARIO, visible y clara.</summary>
        private static void AdoptarItem(Item it)
        {
            try
            {
                Player yo = Main.LocalPlayer;
                if (yo == null || !yo.active || it == null || it.IsAir) return;
                // 1) APILAR en una pila del mismo tipo (por si el arma
                //    era apilable — las consagradas no, pero la ley
                //    vale para toda devolución).
                for (int k = 0; k < 58; k++)
                {
                    Item inv = yo.inventory[k];
                    if (inv == null || inv.IsAir || inv.type != it.type) continue;
                    if (inv.stack + it.stack <= inv.maxStack)
                    {
                        inv.stack += it.stack;
                        return;
                    }
                }
                // 2) EL PRIMER HUECO LIBRE del inventario real.
                for (int k = 0; k < 58; k++)
                {
                    if (yo.inventory[k] == null || yo.inventory[k].IsAir)
                    {
                        yo.inventory[k] = (Item)it.Clone();
                        return;
                    }
                }
                // 3) ÚLTIMO RECURSO: al suelo (QuickSpawnItem) — pero
                //    con su AVISO (nada de pérdidas silenciosas).
                yo.QuickSpawnItem(yo.GetSource_FromThis(), (Item)it.Clone(), it.stack);
                Main.NewText(Language.GetTextValue("Mods.AethonMod.Altar.DevueltaAlSuelo"),
                    new Color(178, 52, 44));
            }
            catch { }
        }

        public static void ClicEnDesequiparTodo()
        {
            try
            {
                var trio = global::AethonMod.Content.Weapons.Investigaciones
                    .TrioDelEterno.Del(Main.LocalPlayer);
                if (trio == null || trio.Ranuras == null) return;
                int devueltas = 0;
                for (int k = 0; k < 3; k++)
                {
                    Item arma = trio.Ranuras[k];
                    if (arma == null || arma.IsAir) continue;
                    Item devolver = (Item)arma.Clone();
                    devolver.stack = 1;
                    trio.Ranuras[k] = new Item();
                    AdoptarItem(devolver);   // v6.168 — inserción directa, no spawn
                    devueltas++;
                }
                if (devueltas > 0)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Grab);
                    Main.NewText(Language.GetTextValue(
                        "Mods.AethonMod.Items.TrioTodoDevuelto", devueltas),
                        new Color(160, 230, 255));
                }
            }
            catch { }
        }

        /// <summary>EL CLIC EN LA MARCA «3»: alterna las-tres / ranura 1.</summary>
        public static void ClicEnMarcaTres()
        {
            try
            {
                var trio = global::AethonMod.Content.Weapons.Investigaciones
                    .TrioDelEterno.Del(Main.LocalPlayer);
                if (trio == null) return;
                trio.ModoDisparo = trio.ModoDisparo >= 3 ? 0 : 3;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                Main.NewText(Language.GetTextValue(
                    "Mods.AethonMod.Items." +
                    (trio.ModoDisparo >= 3 ? "TrioModoTres" : "TrioModoUna"), 1),
                    new Color(160, 230, 255));
            }
            catch { }
        }

        /// <summary>EL CLIC EN CONSAGRAR (el sello de la casa).</summary>
        public static void ClicEnConsagrar()
        {
            try
            {
                Player yo = Main.LocalPlayer;
                if (yo == null || !yo.active || !_abierta || _arma.IsAir) return;
                ConsagrarDesdeElLibro(yo);
            }
            catch { }
        }

        /// <summary>EL CLIC EN LA PUERTA al mundo interior.</summary>
        public static void ClicEnPuerta()
        {
            try
            {
                Player yo = Main.LocalPlayer;
                if (yo == null || !yo.active || !_abierta) return;
                string razonPuerta;
                if (global::AethonMod.Content.Systems.SubmundoGrimorio
                    .PuedeEntrar(yo, out razonPuerta))
                {
                    _abierta = false;
            SelectorAbierto = null;   // v6.166 — la bandeja muere con la ventana
                    if (LibroDelEternoUI.Abierto) LibroDelEternoUI.Cerrar();
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74);
                    Projectile.NewProjectile(yo.GetSource_Misc("AltarMundoInterior"),
                        yo.Center, Vector2.Zero,
                        ModContent.ProjectileType
                            <global::AethonMod.Content.Projectiles.Definitivas.DevoracionEterna>(),
                        0, 0f, yo.whoAmI);
                }
                else
                {
                    if (razonPuerta != null)
                        Main.NewText(razonPuerta, new Color(190, 160, 220));
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                }
            }
            catch { }
        }

        /// <summary>LA CONSAGRACIÓN (el mismo bloque del botón del
        /// panel viejo — el devoro de las esencias o el sello del
        /// arma/material, con su teatro de bruma).</summary>
        private static void ConsagrarDesdeElLibro(Player yo)
        {
            bool lista = EsOfrendaConsagrible(_arma);
            bool esEsencias = lista && _arma.type == ModContent.ItemType
                <global::AethonMod.Content.Items.Altar.ItemCorrupto>();
            Vector2 altarCentro = new Vector2(_tileX * 16f + 16f, _tileY * 16f + 8f);

            if (esEsencias)
            {
                int topePuerta = global::AethonMod.Content.Systems.SubmundoGrimorio.EsenciasPuerta;
                int yaConsagradas = global::AethonMod.Content.Systems.SubmundoGrimorio.EsenciasConsagradas;
                int falta = topePuerta - yaConsagradas;
                int cuantas = _arma.stack;
                if (falta <= 0)
                {
                    Main.NewText(Language.GetTextValue(Clave("PuertaYaAbierta")), TintaTeal);
                    if (cuantas > 0 && Main.netMode != NetmodeID.MultiplayerClient)
                        yo.QuickSpawnItem(yo.GetSource_FromThis(), _arma.type, cuantas);
                }
                else
                {
                    int toma = Math.Min(falta, cuantas);
                    bool yaAbiertaEsencias =
                        global::AethonMod.Content.Systems.SubmundoGrimorio.PuertaAbierta;
                    global::AethonMod.Content.Systems.SubmundoGrimorio.EsenciasConsagradas += toma;
                    if (cuantas - toma > 0 && Main.netMode != NetmodeID.MultiplayerClient)
                        yo.QuickSpawnItem(yo.GetSource_FromThis(), _arma.type, cuantas - toma);
                    if (!Main.dedServ)
                        for (int k = 0; k < 22; k++)
                        {
                            Dust d = Dust.NewDustPerfect(
                                altarCentro + new Vector2(Main.rand.NextFloat(-14f, 14f),
                                    Main.rand.NextFloat(-30f, 6f)),
                                DustID.Shadowflame, Main.rand.NextVector2Circular(2.5f, 2.5f), 140,
                                new Color(140, 60, 210), 1f);
                            d.noGravity = true;
                        }
                    Main.NewText(Language.GetTextValue(Clave("DevoradasHecha"), toma,
                        global::AethonMod.Content.Systems.SubmundoGrimorio.EsenciasConsagradas,
                        topePuerta), TintaVioleta);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item113.WithPitchOffset(-0.45f));
                    if (!yaAbiertaEsencias &&
                        global::AethonMod.Content.Systems.SubmundoGrimorio.PuertaAbierta)
                    {
                        Main.NewText(Terraria.Localization.Language.GetTextValue(
                            "Mods.AethonMod.Submundo.PuertaAbiertaAnuncio"),
                            new Color(220, 170, 255));
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item113.WithPitchOffset(-0.7f));
                    }
                }
                _arma = new Item();
                return;
            }

            var sello = _arma.GetGlobalItem
                <global::AethonMod.Content.Globals.CorruptoGlobalItem>();
            if (sello == null) return;
            int coste = global::AethonMod.Content.Globals.CorruptoGlobalItem.CosteCorrupcion(_arma);
            int llevaba = global::AethonMod.Content.Globals.CorruptoGlobalItem
                .EsenciasEnLaBolsa(yo);
            if (llevaba < coste)
            {
                Main.NewText(Language.GetTextValue(Clave("FaltanEsencias")), TintaRojo);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                return;
            }
            global::AethonMod.Content.Globals.CorruptoGlobalItem.PagarCorrupcion(yo, coste);
            bool yaAbiertaS = global::AethonMod.Content.Systems.SubmundoGrimorio.PuertaAbierta;
            global::AethonMod.Content.Systems.SubmundoGrimorio.EsenciasConsagradas += coste;
            if (!yaAbiertaS && global::AethonMod.Content.Systems.SubmundoGrimorio.PuertaAbierta)
            {
                Main.NewText(Terraria.Localization.Language.GetTextValue(
                    "Mods.AethonMod.Submundo.PuertaAbiertaAnuncio"),
                    new Color(220, 170, 255));
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item113.WithPitchOffset(-0.7f));
            }
            global::AethonMod.Content.Globals.CorruptoGlobalItem.Consagrar(_arma);
            _arma.ResetPrefix();
            global::AethonMod.Content.Globals.CorruptoGlobalItem.VestirLaRareza(_arma);
            Main.NewText(Language.GetTextValue(
                EsArmaDeVerdad(_arma) ? Clave("ConsagradaHechaArma") : Clave("ConsagradaHechaMaterial"),
                _arma.Name), TintaVioleta);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item113.WithPitchOffset(-0.45f));
            if (!Main.dedServ)
                for (int k = 0; k < 22; k++)
                {
                    Dust d = Dust.NewDustPerfect(
                        altarCentro + new Vector2(Main.rand.NextFloat(-14f, 14f),
                            Main.rand.NextFloat(-30f, 6f)),
                        DustID.Shadowflame, Main.rand.NextVector2Circular(2.5f, 2.5f), 140,
                        new Color(140, 60, 210), 1f);
                    d.noGravity = true;
                }
            DevolverElArma();
        }

        /// <summary>v6.140 — EL PERGAMINO DEL ALTAR (la letra: «dame
        /// una idea sobre que se podria hacer con todo el texto del
        /// altar… creando un libro de informacion»): el panel queda
        /// MÍNIMO — toda la info vive en EL PERGAMINO (PergaminoUI, la
        /// hoja con el ojo que mira al cursor). Se abre con la tecla P
        /// mientras el panel vive (el atajo de la casa).</summary>
        public static void AlternarPergamino()
        {
            if (PergaminoUI.Abierto) PergaminoUI.Cerrar();
            else PergaminoUI.Abrir();
        }

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
            // v6.163 — EL PANEL FLOTANTE MURIÓ (la letra: «puedes usar la
            // misma ventana que usas para el libro de informacion en el
            // altar»): el contenido vive como PESTAÑA del libro
            // (AltarDelLibro) — este sistema solo sigue siendo EL MOTOR
            // (la ley de distancia, el slot, los clics). NADA que
            // insertar: el libro dibuja.
            // LA LEY DEL ALTAR SIGUE VIVA: cada frame, sin altar físico
            // (roto o lejos), el altar se cierra — y si el libro está en
            // la pestaña altar, se cierra TAMBIÉN (nada de clics fantasma).
            if (_abierta)
            {
                try
                {
                    Player yo = Main.LocalPlayer;
                    Tile t = Framing.GetTileSafely(_tileX, _tileY);
                    bool altarVivo = t.HasTile && t.TileType ==
                        (ushort)ModContent.TileType<global::AethonMod.Content.Tiles.AltarOfrendasTile>();
                    Vector2 altarCentro = new Vector2(_tileX * 16f + 16f, _tileY * 16f + 8f);
                    if (yo == null || !yo.active || !altarVivo ||
                        yo.Center.DistanceSQ(altarCentro) > 280f * 280f)
                    {
                        CerrarDeVerdad();
                        if (LibroDelEternoUI.Abierto && LibroDelEternoUI.Pestana == 1)
                            LibroDelEternoUI.Cerrar();
                    }
                }
                catch { }
            }
        }

        // ================================================================
        //  LA PALETA (la de la casa)
        // ================================================================
        // v6.167 — LA VENTANA DEL ALTAR VISTE LA PALETA DEL LIBRO (la
        // letra: «en la ventana del altar debes poner el mismo color
        // de letras que tiene la ventana de informacion» — y la del
        // libro quedó BIEN): el panel oscuro teal MUERE — el altar es
        // PAPEL de manuscrito (el mismo 216,200,166 del libro) con
        // TINTA cálida (28,20,16), títulos violeta (148,72,178) y
        // acentos ámbar (168,108,44). El borde de DrawBorderString ya
        // no se funde con NADA: papel claro + tinta oscura.
        private static readonly Color TintaFondo = new(216, 200, 166);   // el papel del libro
        private static readonly Color TintaPanel = new(190, 172, 134);   // la sombra del papel
        private static readonly Color TintaTeal = new(60, 118, 102);     // acento profundo (sobre papel)
        private static readonly Color TintaVioleta = new(148, 72, 178);  // el violeta del libro
        private static readonly Color TintaTexto = new(28, 20, 16);      // la tinta del libro
        private static readonly Color TintaApagado = new(104, 84, 60);   // tinta suave (se lee en papel)
        private static readonly Color TintaRojo = new(178, 52, 44);      // rojo tinta (errores)
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
                    // ==========================================================
                    //  v6.122 — EL DEPÓSITO DE LAS MIL (la letra: «en el
                    //  altar debes poner como limite solo 1000 esencias
                    //  ya que al poner mas de 1000 el resto se pierden,
                    //  asi que el altar tiene un tope de 1000 si pones
                    //  mas de 1000 el altar tomara 1000 y el resto
                    //  quedaran seleccionadas para que sean devueltas al
                    //  inventario»). EL ALTAR COME ESENCIAS DIRECTO:
                    //  clic en la boca con una pila de Esencias del
                    //  Eterno y LA PUERTA se alimenta (EsenciasPuerta =
                    //  1000 — el tope de la casa): toma SOLO lo que
                    //  falta para las mil y EL RESTO QUEDA EN EL CURSOR
                    //  («seleccionadas»): NI UNA se pierde — se devuelven
                    //  al inventario con el mismo clic de siempre.
                    // ==========================================================
                    if (!Main.mouseItem.IsAir && Main.mouseItem.type == ModContent.ItemType
                        <global::AethonMod.Content.Items.Altar.ItemCorrupto>())
                    {
                        int topePuerta = global::AethonMod.Content.Systems.SubmundoGrimorio.EsenciasPuerta;
                        int yaConsagradas = global::AethonMod.Content.Systems.SubmundoGrimorio.EsenciasConsagradas;
                        int falta = topePuerta - yaConsagradas;
                        if (falta <= 0)
                        {
                            // LA PUERTA YA ESTÁ ABIERTA: el altar no bebe
                            // más — las mil ya viven en sus huesos.
                            Main.NewText(Language.GetTextValue(Clave("PuertaYaAbierta")),
                                TintaTeal);
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                        }
                        else
                        {
                            int toma = Math.Min(falta, Main.mouseItem.stack);
                            bool yaAbierta = global::AethonMod.Content.Systems.SubmundoGrimorio.PuertaAbierta;
                            global::AethonMod.Content.Systems.SubmundoGrimorio.EsenciasConsagradas += toma;
                            Main.NewText(Language.GetTextValue(Clave("PuertaBebe"),
                                toma,
                                global::AethonMod.Content.Systems.SubmundoGrimorio.EsenciasConsagradas,
                                topePuerta), TintaVioleta);
                            // el anuncio de la puerta abriéndose (el mismo
                            // del camino del coste — la puerta es UNA).
                            if (!yaAbierta && global::AethonMod.Content.Systems.SubmundoGrimorio.PuertaAbierta)
                            {
                                Main.NewText(Terraria.Localization.Language.GetTextValue(
                                    "Mods.AethonMod.Submundo.PuertaAbiertaAnuncio"),
                                    new Color(220, 170, 255));
                                Terraria.Audio.SoundEngine.PlaySound(
                                    SoundID.Item113.WithPitchOffset(-0.7f));
                            }
                            else
                            {
                                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item113
                                    .WithPitchOffset(-0.25f));
                            }
                            // EL SOBREVIVIENTE VIVE EN EL CURSOR: lo que
                            // sobra NO se pierde — queda SELECCIONADO para
                            // volver al inventario (la letra, exacta).
                            Main.mouseItem.stack -= toma;
                            if (Main.mouseItem.stack <= 0) Main.mouseItem.TurnToAir();
                            // EL TEATRO DEL TRAGO: la bruma violeta bebe.
                            if (!Main.dedServ)
                            {
                                for (int k = 0; k < Math.Min(30, 8 + toma / 40); k++)
                                {
                                    Dust d = Dust.NewDustPerfect(
                                        new Vector2(_tileX * 16f + 16f, _tileY * 16f + 4f) +
                                            new Vector2(Main.rand.NextFloat(-14f, 14f),
                                                Main.rand.NextFloat(-30f, 4f)),
                                        DustID.Shadowflame,
                                        Main.rand.NextVector2Circular(2.2f, 2.2f), 140,
                                        new Color(140, 60, 210), 1f);
                                    d.noGravity = true;
                                }
                            }
                        }
                    }
                    else if (!Main.mouseItem.IsAir)
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
                    else
                    {
                        // v6.166 — LA BANDEJA DE CONSAGRAR (la letra: «en
                        // el slot de consagrar […] debes mostrar justo
                        // debajo todo lo que se puede consagrar que el
                        // jugador tenga en el inventario»): clic en el
                        // slot VACÍO con cursor VACÍO → los candidatos
                        // se despliegan DEBAJO del slot.
                        AbrirSelector(-2);
                    }
                }

                // v6.161 — LOS SLOTS DEL TRÍO (la mesa de fusión): clic
                // con un ARMA en el cursor → entra a la primera ranura
                // libre; clic en una ranura llena → el arma vuelve al
                // cursor. El MISMO contrato del atril (TrioDelEterno.
                // EsArmaValida), pero SIN llevar el Trío en la mano —
                // el altar es la mesa de fusionar de verdad.
                var trioClic = global::AethonMod.Content.Weapons.Investigaciones
                    .TrioDelEterno.Del(Main.LocalPlayer);
                if (trioClic != null)
                {
                    for (int slotT = 0; slotT < 3; slotT++)
                    {
                        Rectangle geoT = _geoTrio[slotT];
                        if (geoT.Width <= 0) continue;   // la sección no vive este frame
                        bool hoverTrio = geoT.Contains(Main.mouseX, Main.mouseY) &&
                            !Terraria.GameInput.PlayerInput.IgnoreMouseInterface;
                        if (hoverTrio) Main.LocalPlayer.mouseInterface = true;
                        if (!hoverTrio || !clicNuevo) continue;

                        Item armaSlot = trioClic.Ranuras != null && slotT < trioClic.Ranuras.Length
                            ? trioClic.Ranuras[slotT] : null;
                        if (!Main.mouseItem.IsAir)
                        {
                            // INSERCIÓN: solo armas (la regla del atril).
                            if (global::AethonMod.Content.Weapons.Investigaciones
                                .TrioDelEterno.EsArmaValida(Main.mouseItem))
                            {
                                // ¿el slot está libre? si no, busco el
                                // primero libre (el atril también lo
                                // hace así — el cursor entra donde pueda).
                                int destino = (armaSlot == null || armaSlot.IsAir)
                                    ? slotT : -1;
                                if (destino < 0)
                                {
                                    for (int k = 0; k < 3; k++)
                                    {
                                        Item rk = trioClic.Ranuras[k];
                                        if (rk == null || rk.IsAir) { destino = k; break; }
                                    }
                                }
                                if (destino >= 0)
                                {
                                    trioClic.Ranuras[destino] = (Item)Main.mouseItem.Clone();
                                    Main.mouseItem.TurnToAir();
                                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item85);
                                    Main.NewText(Language.GetTextValue(
                                        "Mods.AethonMod.Items.TrioInsertada", destino + 1),
                                        new Color(160, 230, 255));
                                }
                                else
                                {
                                    Main.NewText(Language.GetTextValue(
                                        "Mods.AethonMod.Items.TrioLleno"),
                                        new Color(255, 170, 130));
                                }
                            }
                            else
                            {
                                Main.NewText(Language.GetTextValue(
                                    "Mods.AethonMod.Items.TrioNoArma"),
                                    new Color(255, 170, 130));
                                Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                            }
                        }
                        else if (armaSlot != null && !armaSlot.IsAir)
                        {
                            // EXTRACCIÓN: el arma del slot vuelve al cursor.
                            Main.mouseItem = (Item)armaSlot.Clone();
                            trioClic.Ranuras[slotT] = new Item();
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Grab);
                        }
                        break;   // un slot por clic
                    }

                    // v6.162 — LA MARCA DE LAS TRES (la letra: «para
                    // elegir disparar las 3 al mismo tiempo tengas
                    // marcar en el altar»): la casilla «3» alterna EL
                    // MODO TRES (marcada = las tres a la vez — como
                    // nace el Trío; desmarcada = la última ranura
                    // elegida con el clic derecho).
                    if (_geoTrioMarca.Width > 0 &&
                        _geoTrioMarca.Contains(Main.mouseX, Main.mouseY) &&
                        !Terraria.GameInput.PlayerInput.IgnoreMouseInterface)
                    {
                        Main.LocalPlayer.mouseInterface = true;
                        if (clicNuevo)
                        {
                            trioClic.ModoDisparo = trioClic.ModoDisparo >= 3 ? 0 : 3;
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                            Main.NewText(Language.GetTextValue(
                                "Mods.AethonMod.Items." +
                                (trioClic.ModoDisparo >= 3 ? "TrioModoTres" : "TrioModoUna"),
                                trioClic.ModoDisparo + 1),
                                new Color(160, 230, 255));
                        }
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
                        // v6.122 — EL DEVORO CON TOPE DE MIL (la letra:
                        // «el altar tiene un tope de 1000 si pones mas de
                        // 1000 el altar tomara 1000 y el resto quedaran
                        // seleccionadas para que sean devueltas al
                        // inventario»): el devoro v6.121 tragaba la pila
                        // ENTERA y el exceso sobre las mil se PERDÍA para
                        // siempre (contado a la puerta que ya estaba
                        // llena). AHORA: toma SOLO lo que falta para las
                        // mil y EL SOBRANTE VUELVE al inventario del
                        // portador — NI UNA se pierde, en ningún camino.
                        int topePuerta = global::AethonMod.Content.Systems.SubmundoGrimorio.EsenciasPuerta;
                        int yaConsagradas = global::AethonMod.Content.Systems.SubmundoGrimorio.EsenciasConsagradas;
                        int falta = topePuerta - yaConsagradas;
                        int cuantas = _arma.stack;
                        if (falta <= 0)
                        {
                            // LA PUERTA YA ESTÁ ABIERTA: el altar no bebe
                            // más — TODO vuelve al inventario.
                            Main.NewText(Language.GetTextValue(Clave("PuertaYaAbierta")),
                                TintaTeal);
                            if (cuantas > 0 && Main.netMode != NetmodeID.MultiplayerClient)
                                Main.LocalPlayer.QuickSpawnItem(
                                    Main.LocalPlayer.GetSource_FromThis(),
                                    _arma.type, cuantas);
                        }
                        else
                        {
                            int toma = Math.Min(falta, cuantas);
                            bool yaAbiertaEsencias =
                                global::AethonMod.Content.Systems.SubmundoGrimorio.PuertaAbierta;
                            global::AethonMod.Content.Systems.SubmundoGrimorio.EsenciasConsagradas += toma;
                            // EL SOBRANTE VUELVE (la letra: «devueltas al
                            // inventario») — jamás se pierde.
                            if (cuantas - toma > 0 && Main.netMode != NetmodeID.MultiplayerClient)
                                Main.LocalPlayer.QuickSpawnItem(
                                    Main.LocalPlayer.GetSource_FromThis(),
                                    _arma.type, cuantas - toma);
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
                                toma,
                                global::AethonMod.Content.Systems.SubmundoGrimorio.EsenciasConsagradas,
                                topePuerta), TintaVioleta);
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
                        }
                        _arma = new Item();   // el hueco queda vacío (lo suyo ya vive en la puerta)
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
                            // v6.158 — LA CONSAGRACIÓN CENTRAL (la
                            // poción consagrada DUPLICA lo que hace).
                            global::AethonMod.Content.Globals.CorruptoGlobalItem
                                .Consagrar(_arma);
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
                {
                    if (PergaminoUI.Abierto) PergaminoUI.Cerrar();
                    else CerrarDeVerdad();
                }
                _ultimoEsc = escAhora;

                // v6.140 — LA TECLA P: EL PERGAMINO (toda la info del
                // altar vive en la hoja del libro — el ojo te mira).
                bool pAhora = Microsoft.Xna.Framework.Input.Keyboard
                    .GetState().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.P);
                if (pAhora && !_ultimoP && !PergaminoUI.Abierto)
                {
                    PergaminoUI.Abrir();
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuOpen);
                }
                _ultimoP = pAhora;

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
        /// <summary>v6.161 — LOS TRES SLOTS DEL TRÍO (la mesa de
        /// fusión del altar).</summary>
        private static readonly Rectangle[] _geoTrio = new Rectangle[3];
        /// <summary>v6.162 — LA MARCA DE LAS TRES (la casilla «3» del
        /// altar: disparar las 3 a la vez — por defecto ON).</summary>
        private static Rectangle _geoTrioMarca;
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

            // v6.166 — LA BANDEJA DE CONSAGRABLES (la letra: «en el
            // slot de consagrar debes mostrar justo debajo todo lo que
            // se puede consagrar que el jugador tenga en el
            // inventario»): abierta por el clic en el slot vacío, la
            // fila de iconos vive AQUÍ (justo debajo del slot y su
            // texto). El alto de la bandeja se RESERVA siempre que está
            // abierta (el panel no salta).
            float altoBandeja = 0f;
            if (SelectorAbierto != null && SelectorAbierto.Value.ranura == -2)
            {
                var cands = CosecharCandidatos(false);
                if (cands.Count == 0)
                    altoBandeja = AltoMedido(Language.GetTextValue(Clave("BandejaVacia")), w - 48f, 0.7f) + 4f;
                else
                    altoBandeja = 46f + (cands.Count > 8 ? 14f : 0f);
            }

            // Las secciones nacen bajo el slot Y bajo el texto, lo que pida más.
            cy = Math.Max(cy, y + 138f) + 10f;

            // EL DIBUJO DE LA BANDEJA (la fila de iconos consagrables)
            if (dibujar && SelectorAbierto != null && SelectorAbierto.Value.ranura == -2)
            {
                var cands = CosecharCandidatos(false);
                if (cands.Count == 0)
                {
                    LineaMedida(sb, Language.GetTextValue(Clave("BandejaVacia")),
                        x + 24f, cy, w - 48f, 0.7f, TintaApagado);
                }
                else
                {
                    int visibles = Math.Min(8, cands.Count);
                    float anchoFila = visibles * 46f;
                    float bx = x + 24f;
                    for (int ic = 0; ic < visibles; ic++)
                    {
                        Rectangle icRect = new Rectangle((int)(bx + ic * 46f), (int)cy, 42, 42);
                        Item cand = cands[ic];
                        bool hovIc = PanelContiene(icRect) &&
                            !Terraria.GameInput.PlayerInput.IgnoreMouseInterface;
                        if (hovIc) Main.LocalPlayer.mouseInterface = true;
                        sb.Draw(pixel, icRect, TintaPanel * (hovIc ? 1.6f : 1f));
                        Marco(sb, pixel, icRect, hovIc ? TintaTeal : TintaTeal * 0.4f);
                        Texture2D texI = TextureAssets.Item[cand.type].Value;
                        float escI = Math.Min(34f / texI.Width, 34f / texI.Height);
                        sb.Draw(texI, icRect.Center.ToVector2(), null, Color.White, 0f,
                            texI.Size() * 0.5f, escI, SpriteEffects.None, 0f);
                        // EL NOMBRE al pasar el cursor (la etiqueta de la casa)
                        if (hovIc)
                        {
                            string nombreC = cand.Name;
                            float escNom = 0.8f;
                            var mNom = FontAssets.MouseText.Value.MeasureString(nombreC) * escNom;
                            Vector2 posNom = new Vector2(
                                Math.Min(icRect.Center.X, Main.screenWidth - mNom.X - 8f),
                                icRect.Top - mNom.Y - 4f);
                            Utils.DrawBorderString(sb, nombreC, posNom,
                                TintaTexto, escNom);
                        }
                        if (hovIc && Main.mouseLeft && !_ultimoClicIzq)
                            ClicEnCandidato(ic);
                    }
                    if (cands.Count > visibles)
                        Utils.DrawBorderString(sb, "+" + (cands.Count - visibles),
                            new Vector2(bx + anchoFila + 8f, cy + 21f), TintaApagado, 0.7f, 0f, 0.5f);
                }
                cy += altoBandeja;
            }

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

            // ── v6.159 — LA SECCIÓN «CÓMO CONSEGUIR ESENCIAS» MUERE DEL
            //    PANEL (la letra: «ya tenemos al libro de informacion,
            //    podemos poner toda la informacion que tiene la ventana
            //    del atlar en el libro y de esa forma liberar espacio
            //    para lo importante»): TODO el contenido informativo
            //    (la mesa de las esencias: criaturas 0,1% · jefes 1-10 ·
            //    las oleadas · LAS ESTATUAS · la nota) vive AHORA en EL
            //    LIBRO DEL ETERNO (página izquierda, SecAltar/Altar5b).
            //    El panel queda para LO VIVO: el estado del slot, el
            //    sello y la puerta del mundo interior. ──
            cy += 6f;

            // ── v6.161 — LA SECCIÓN 3 — EL TRÍO DEL ETERNO (la letra:
            //    «que esta ventana tenga un apartado donde se pueda
            //    usar para fusionar y desfusionar el arma el trio
            //    eterno»): LA MESA DE FUSIÓN. TRES slots (los mismos
            //    Ranuras[] del atril — la MISMA arma, el MISMO
            //    inventario): clic con un arma en el cursor → se
            //    inserta; clic en un slot lleno → vuelve al cursor.
            //    Si el portador NO lleva el Trío, la sección lo dice
            //    (trae el atril al altar). ──
            var trio = global::AethonMod.Content.Weapons.Investigaciones
                .TrioDelEterno.Del(Main.LocalPlayer);
            cy = TituloSeccion(sb, Language.GetTextValue(Clave("TrioTitulo")),
                x, cy, w, new Color(150, 220, 255));
            if (dibujar)
            {
                if (trio == null)
                {
                    // SIN TRÍO: la invitación (la mesa está puesta).
                    LineaMedida(sb, Language.GetTextValue(Clave("TrioSinTrio")),
                        x + 24f, cy, w - 48f, 0.75f, TintaApagado);
                }
                else
                {
                    LineaMedida(sb, Language.GetTextValue(Clave("TrioPista")),
                        x + 24f, cy, w - 48f, 0.72f, TintaApagado * 0.9f);
                }
            }
            float altoTrio = Math.Max(
                AltoMedido(Language.GetTextValue(Clave("TrioSinTrio")), w - 48f, 0.75f),
                AltoMedido(Language.GetTextValue(Clave("TrioPista")), w - 48f, 0.72f));
            cy += altoTrio + 6f;
            if (trio != null)
            {
                // LOS TRES SLOTS DEL ATRIL (en fila, 52 px — el mismo
                // tamaño del slot de ofrenda: el lenguaje del panel).
                for (int slotT = 0; slotT < 3; slotT++)
                {
                    Rectangle s = new Rectangle(x + 24 + slotT * 62, (int)cy, 52, 52);
                    if (dibujar)
                    {
                        _geoTrio[slotT] = s;
                        bool hoverS = PanelContiene(s) &&
                            !Terraria.GameInput.PlayerInput.IgnoreMouseInterface;
                        sb.Draw(pixel, s, TintaFondo * 0.9f);
                        // v6.162 — LA RANURA ELEGIDA BRILLA (la letra: el
                        // clic derecho elige el arma 1/2/3 — el altar lo
                        // MUESTRA con el marco pleno de la elegida).
                        bool elegida = trio.ModoDisparo == slotT;
                        Marco(sb, pixel, s, elegida ? new Color(200, 170, 255)
                            : hoverS ? new Color(150, 220, 255)
                            : new Color(150, 220, 255) * 0.4f);
                        Item armaS = trio.Ranuras != null && slotT < trio.Ranuras.Length
                            ? trio.Ranuras[slotT] : null;
                        if (armaS != null && !armaS.IsAir)
                        {
                            Texture2D texS = TextureAssets.Item[armaS.type].Value;
                            float esc = Math.Min(44f / texS.Width, 44f / texS.Height);
                            sb.Draw(texS, s.Center.ToVector2(), null, Color.White, 0f,
                                texS.Size() * 0.5f, esc, SpriteEffects.None, 0f);
                        }
                        else
                        {
                            Utils.DrawBorderString(sb, (slotT + 1).ToString(),
                                new Vector2(s.Center.X, s.Center.Y),
                                TintaApagado * 0.8f, 0.8f, 0.5f, 0.5f);
                        }
                    }
                }
                // v6.162 — LA MARCA DE LAS TRES (la letra: «para elegir
                // disparar las 3 al mismo tiempo tengas marcar en el
                // altar usar las 3 al mismo tiempo, que sea algo ya
                // seleccionado por defecto»): la casilla «3» junto a
                // los slots — marcada DE FÁBRICA (ModoDisparo = 3 al
                // nacer el Trío). Clic: alterna el modo TRES ↔ la
                // última ranura elegida.
                Rectangle marca = new Rectangle(x + 24 + 3 * 62 + 6, (int)cy + 8, 36, 36);
                if (dibujar)
                {
                    _geoTrioMarca = marca;
                    bool marcada = trio.ModoDisparo >= 3;
                    bool hoverM = PanelContiene(marca) &&
                        !Terraria.GameInput.PlayerInput.IgnoreMouseInterface;
                    sb.Draw(pixel, marca, (hoverM ? new Color(150, 220, 255) : TintaVioleta) * 0.25f);
                    Marco(sb, pixel, marca, marcada ? new Color(200, 170, 255)
                        : new Color(150, 220, 255) * 0.45f);
                    if (marcada)
                    {
                        sb.Draw(pixel, new Rectangle(marca.X + 8, marca.Center.Y - 1, 20, 2),
                            new Color(200, 170, 255));
                        sb.Draw(pixel, new Rectangle(marca.Center.X - 1, marca.Y + 8, 2, 20),
                            new Color(200, 170, 255));
                    }
                    Utils.DrawBorderString(sb, "3", new Vector2(marca.Center.X, marca.Center.Y),
                        marcada ? new Color(230, 210, 255) : TintaApagado, 0.9f, 0.5f, 0.5f);
                }
                cy += 52f + 8f;
                // la leyenda de la marca (qué dispara el Trío AHORA —
                // siempre medida con su variante más larga).
                float altoMarca = Math.Max(
                    AltoMedido(Language.GetTextValue(Clave("TrioMarca"), 1), w - 48f, 0.70f),
                    AltoMedido(Language.GetTextValue(Clave("TrioMarca"), 0), w - 48f, 0.70f));
                if (dibujar)
                    LineaMedida(sb, Language.GetTextValue(Clave("TrioMarca"),
                            trio.ModoDisparo >= 3 ? 1 : 0),
                        x + 24f, cy, w - 48f, 0.70f, TintaApagado * 0.9f);
                cy += altoMarca + 4f;
            }

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
        // v6.137 — EL AIRE: los títulos de sección respiran — un
        // margen de 8 px ANTES de cada título (antes las secciones
        // venían pegadas una a la otra y el panel se leía apretado).
        private static float TituloSeccion(SpriteBatch sb, string texto, int x, float cy, int w, Color c)
        {
            cy += 8f;   // el respiro de la casa
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
