using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;
using Corrupto = global::AethonMod.Content.Globals.CorruptoGlobalItem;
using Submundo = global::AethonMod.Content.Systems.SubmundoGrimorio;
using TrioArma = global::AethonMod.Content.Weapons.Investigaciones.TrioDelEterno;

namespace AethonMod.Content.UI
{
    // ======================================================================
    //  ALTAROFRENDASUI — EL MOTOR DEL ALTAR DE LA CONSAGRACIÓN.
    //
    //  Desde la v6.163 el altar no dibuja nada: su ventana es la
    //  PESTAÑA del libro (AltarDelLibro) y ESTA clase es el motor —
    //  la ley de la distancia, el hueco de la ofrenda, las tres
    //  ranuras del trío, la consagración y la puerta al mundo interior.
    //
    //  ──────────────────────────────────────────────────────────────
    //  v6.174 — LA OFRENDA PRESTADA (la letra: «en el caso de consagrar
    //  hay duplicación de arma […] cuando se toca consagrar el juego
    //  devuelva la misma arma ya con el modificador consagrada» + LA
    //  NOTA: «el arma en el inventario solo se borrará en el momento en
    //  el que se toque el botón consagrar y se cree la versión
    //  consagrada»).
    //
    //  LA AUTOPSIA de las versiones viejas: al ELEGIR una ofrenda en la
    //  vitrina, el motor sacaba el arma del inventario, la clonaba y
    //  trabajaba con la COPIA — al consagrar podía volver la copia Y el
    //  original (dos armas), y al cerrar el libro la ofrenda sin
    //  consagrar saltaba al cursor. Y la persistencia guardaba la
    //  ofrenda en el archivo del mundo Y además la devolvía al jugador
    //  al descargar el mundo: otra vía de duplicación.
    //
    //  LA LEY NUEVA — DOS CLASES DE OFRENDA:
    //   · LA PRESTADA (la elegida en la vitrina): el hueco recuerda LA
    //     REFERENCIA VIVA de la pila del inventario. NADA sale de ahí.
    //     Al pulsar CONSAGRAR: si la pila tiene un solo ejemplar, ESA
    //     MISMA arma se sella EN SU SITIO (la misma arma, ya
    //     consagrada); si tiene varios, la pila paga uno y la unidad
    //     sellada vuelve al inventario. Cerrar el libro o retirar la
    //     ofrenda = olvidar el préstamo: el arma nunca se movió.
    //     Si la pila desaparece del inventario (se vendió, se tiró), el
    //     préstamo se da por perdido y el hueco queda libre (se avisa).
    //   · LA FÍSICA (la que el jugador DEJA con el cursor): vive en el
    //     hueco, como en un cofre — persiste con el mundo (y SOLO con el
    //     mundo: nada se devuelve al descargar, nada se duplica) y se
    //     recupera con un clic (al cursor) o con el clic derecho (al
    //     inventario).
    //
    //  EL TRÍO: el arma entra en LA RANURA CLICADA (si estaba ocupada,
    //  la vieja vuelve al inventario) — la copia nace primero y el
    //  original muere después. El clic derecho devuelve sin perder nada:
    //  si el inventario está lleno, el arma SE QUEDA en su ranura y el
    //  libro lo dice.
    //  ──────────────────────────────────────────────────────────────
    public class AltarOfrendasUI : ModSystem
    {
        private static bool _abierta;
        private static int _tileX, _tileY;

        /// <summary>LA OFRENDA FÍSICA: la que llegó con el cursor y vive
        /// FUERA del inventario (persiste con el mundo).</summary>
        private static Item _arma = new Item();

        /// <summary>LA OFRENDA PRESTADA: la referencia VIVA a la pila del
        /// inventario elegida en la vitrina. Nunca se clona ni se mueve.</summary>
        private static Item _armaOrigen;

        // EL CHAT (fondo oscuro): las tintas LUMINOSAS de la familia del libro
        private static readonly Color ChatVioleta = new(196, 110, 228);
        private static readonly Color ChatOro = new(214, 152, 70);
        private static readonly Color ChatApagado = new(168, 140, 104);
        private static readonly Color ChatRojo = new(226, 96, 88);

        private static int TipoEsencias => ModContent.ItemType
            <global::AethonMod.Content.Items.Altar.ItemCorrupto>();

        private static int TipoLibro => ModContent.ItemType
            <global::AethonMod.Content.Items.Altar.LibroCorrupto>();

        private static void Decir(string clave, Color tinta, params object[] args)
            => Main.NewText(Language.GetTextValue("Mods.AethonMod.Altar." + clave, args), tinta);

        private static void DecirItem(string clave, Color tinta, params object[] args)
            => Main.NewText(Language.GetTextValue("Mods.AethonMod.Items." + clave, args), tinta);

        // ================================================================
        //  LO QUE LA PÁGINA DEL LIBRO LEE
        // ================================================================
        public static bool Abierta => _abierta;

        /// <summary>LA OFRENDA del hueco: la prestada si vive, si no la
        /// física, si no un ítem vacío.</summary>
        public static Item Ofrenda
        {
            get
            {
                if (_armaOrigen != null && !_armaOrigen.IsAir) return _armaOrigen;
                if (_arma != null && !_arma.IsAir) return _arma;
                return new Item();
            }
        }

        /// <summary>¿La ofrenda del hueco es un PRÉSTAMO (sigue en el
        /// inventario de su dueño)?</summary>
        public static bool OfrendaPrestada => _armaOrigen != null && !_armaOrigen.IsAir;

        public static bool EsOfrendaConsagriblePublica(Item it) => EsOfrendaConsagrible(it);

        // ================================================================
        //  EL FILTRO DEL CONSAGRADOR (v6.90): armas limpias Y materiales
        //  («si se corrompe otro item como un lingote de oro, entonces
        //  solo sale la etiqueta»). Nada de herramientas ni accesorios,
        //  ni el grimorio, ni el libro, ni lo ya consagrado. Las monedas
        //  no se consagran (v6.95). LAS ESENCIAS SÍ entran (v6.121): el
        //  Eterno las devora — son la llave al mundo interior.
        // ================================================================
        private static bool EsOfrendaConsagrible(Item it)
        {
            try
            {
                if (it == null || it.IsAir) return false;
                if (it.type == TipoEsencias) return true;
                if (it.type == TipoLibro) return false;
                if (it.ModItem is global::AethonMod.Content.Weapons.GrimoireEternal) return false;
                if (it.type == ItemID.CopperCoin || it.type == ItemID.SilverCoin ||
                    it.type == ItemID.GoldCoin || it.type == ItemID.PlatinumCoin) return false;
                if (it.accessory || it.vanity) return false;
                if (it.pick > 0 || it.axe > 0 || it.hammer > 0) return false;
                if (Consagrado(it)) return false;
                return true;
            }
            catch { return false; }
        }

        private static bool EsArmaDeVerdad(Item it)
            => it != null && !it.IsAir && it.damage > 0 && it.ammo == 0;

        /// <summary>¿ESTE item es un CANDIDATO? En las ranuras del trío: arma
        /// mágica Y CONSAGRADA. En el hueco: todo lo que el altar acepta
        /// (la MISMA regla de EsOfrendaConsagrible — antes la vitrina
        /// enseñaba cosas que el hueco luego rechazaba).</summary>
        internal static bool EsCandidato(Item it, bool paraTrío)
        {
            if (it == null || it.IsAir) return false;
            if (!paraTrío) return EsOfrendaConsagrible(it);
            return Consagrado(it) && TrioArma.EsArmaValida(it);
        }

        /// <summary>LA COSECHA del inventario: las REFERENCIAS VIVAS de los
        /// candidatos (un icono por tipo), en orden de ranura.</summary>
        internal static List<Item> CosecharCandidatos(bool paraTrío)
        {
            var lista = new List<Item>();
            var vistos = new HashSet<int>();
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

        // ================================================================
        //  EL ALTAR SE ABRE / SE CIERRA
        // ================================================================
        public static void Alternar(int i, int j)
        {
            if (_abierta)
            {
                // YA ABIERTO: se cierra — y el libro con él (si está en SU pestaña)
                ForzarCierre();
                if (LibroDelEternoUI.Abierto && LibroDelEternoUI.Pestana == 1)
                    LibroDelEternoUI.Cerrar();
                return;
            }
            _abierta = true;
            _tileX = i;
            _tileY = j;
            _armaOrigen = null;     // el préstamo de la vez anterior no resucita
        }

        /// <summary>EL CIERRE: el préstamo se olvida (el arma sigue en el
        /// inventario de su dueño); la ofrenda FÍSICA se queda en el hueco
        /// del altar — es un cofre, vuelve a verse al reabrir.</summary>
        public static void ForzarCierre()
        {
            _abierta = false;
            _armaOrigen = null;
        }

        public static void ClicEnPuerta()
        {
            try
            {
                Player yo = Main.LocalPlayer;
                if (yo == null || !yo.active) return;
                if (Submundo.PuedeEntrar(yo, out string razonPuerta))
                {
                    ForzarCierre();
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
                        Main.NewText(razonPuerta, ChatVioleta);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                }
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("AltarOfrendasUI", exRegistro); }
        }

        /// <summary>v6.182 — EL VIAJE SIN EL ALTAR ABIERTO (la letra: el
        /// botón de la ventana muere y LA PUERTA PASA A SER EL BUFF
        /// «Mundo Interior» — clic derecho en su icono entre los buffs): el
        /// camino de siempre, disponible desde cualquier parte del mundo.
        /// Lo llama el buff (MundoInteriorBuff).</summary>
        public static void ViajarAlInterior()
        {
            try
            {
                Player yo = Main.LocalPlayer;
                if (yo == null || !yo.active) return;
                if (!Submundo.PuedeEntrar(yo, out string razonPuerta))
                {
                    if (razonPuerta != null)
                        Main.NewText(razonPuerta, ChatVioleta);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                    return;
                }
                if (Abierta) ForzarCierre();
                if (LibroDelEternoUI.Abierto) LibroDelEternoUI.Cerrar();
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74);
                Projectile.NewProjectile(yo.GetSource_Misc("BuffMundoInterior"),
                    yo.Center, Vector2.Zero,
                    ModContent.ProjectileType
                        <global::AethonMod.Content.Projectiles.Definitivas.DevoracionEterna>(),
                    0, 0f, yo.whoAmI);
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("AltarOfrendasUI", exRegistro); }
        }

        public static void AlternarPergamino()
        {
            if (PergaminoUI.Abierto) PergaminoUI.Cerrar();
            else PergaminoUI.Abrir();
        }

        // ================================================================
        //  EL HUECO DE LA OFRENDA
        // ================================================================

        /// <summary>CLIC IZQUIERDO en el hueco (lo llama la página cuando el
        /// cursor trae algo o el hueco tiene ofrenda): esencias al cursor =
        /// el depósito de las mil · ofrenda al cursor = el trueque · cursor
        /// vacío = retirar.</summary>
        public static void ClicEnSlot()
        {
            try
            {
                Player yo = Main.LocalPlayer;
                if (yo == null || !yo.active || !_abierta) return;
                Item enCursor = Main.mouseItem;

                if (enCursor != null && !enCursor.IsAir && enCursor.type == TipoEsencias)
                {
                    DepositarLasEsencias(enCursor);
                    return;
                }

                if (enCursor != null && !enCursor.IsAir)
                {
                    if (!EsOfrendaConsagrible(enCursor))
                    {
                        Decir(Consagrado(enCursor) ? "YaConsagrada" : "NoArma", ChatRojo);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                        return;
                    }
                    // EL TRUEQUE: la pila del cursor entra ENTERA al hueco; lo
                    // que había en el hueco sale al cursor. Nada se clona.
                    Item sale = (_arma != null && !_arma.IsAir) ? _arma : new Item();
                    _armaOrigen = null;                 // el préstamo se olvida (sigue en el inventario)
                    _arma = enCursor;
                    Main.mouseItem = sale;
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Grab);
                    return;
                }

                // CURSOR VACÍO: retirar
                if (OfrendaPrestada || (_arma != null && !_arma.IsAir))
                {
                    SoltarLaOfrenda(true);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Grab);
                }
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("AltarOfrendasUI", exRegistro); }
        }

        /// <summary>CLIC DERECHO en el hueco (v6.174, la letra: «el click
        /// derecho sobre un slot debe desequipar el arma»): la ofrenda
        /// vuelve al inventario sin pasar por el cursor.</summary>
        public static void ClicDerechoSlot()
        {
            try
            {
                Player yo = Main.LocalPlayer;
                if (yo == null || !yo.active || !_abierta) return;
                bool habia = OfrendaPrestada || (_arma != null && !_arma.IsAir);
                SoltarLaOfrenda(true);
                Terraria.Audio.SoundEngine.PlaySound(habia ? SoundID.Grab : SoundID.MenuTick);
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("AltarOfrendasUI", exRegistro); }
        }

        /// <summary>EL DEPÓSITO DE LAS MIL: las esencias del cursor suben a
        /// la puerta (con tope). La pila paga lo que la puerta necesita.</summary>
        private static void DepositarLasEsencias(Item pila)
        {
            int topePuerta = Submundo.EsenciasPuerta;
            // v6.179 — LA PUERTA YA VIVA no vuelve a cobrar (la letra:
            // «una vez activo debe estar activo siempre»): con el trago
            // de la primera vez, la cuenta vuelve a 0 y el cerrojo
            // PuertaViva manda — las esencias se quedan en la mano.
            if (Submundo.PuertaViva || topePuerta - Submundo.EsenciasConsagradas <= 0)
            {
                Decir("PuertaYaAbierta", ChatOro);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                return;
            }
            int falta = topePuerta - Submundo.EsenciasConsagradas;
            int toma = Math.Min(falta, pila.stack);
            bool yaAbierta = Submundo.PuertaAbierta;
            Submundo.EsenciasConsagradas += toma;
            Decir("PuertaBebe", ChatVioleta, toma, Submundo.EsenciasConsagradas, topePuerta);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item113.WithPitchOffset(-0.25f));
            if (!yaAbierta && Submundo.PuertaAbierta)
            {
                AnunciarLaPuerta();
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item113.WithPitchOffset(-0.7f));
            }
            // v6.179 — EL TRAGO DE LA PRIMERA VEZ: las mil se COMEN y la
            // puerta queda VIVA PARA SIEMPRE (PuertaViva — nada la cierra).
            Submundo.TragarLaPuerta();
            pila.stack -= toma;
            if (pila.stack <= 0) pila.TurnToAir();
            LluviaDeBruma(Math.Min(30, 8 + toma / 40), 4f);
        }

        private static void AnunciarLaPuerta()
            => Main.NewText(Language.GetTextValue("Mods.AethonMod.Submundo.PuertaAbiertaAnuncio"), ChatVioleta);

        private static void LluviaDeBruma(int cuantos, float alto)
        {
            if (Main.dedServ) return;
            for (int k = 0; k < cuantos; k++)
            {
                Dust d = Dust.NewDustPerfect(
                    new Vector2(_tileX * 16f + 16f, _tileY * 16f + alto) +
                        new Vector2(Main.rand.NextFloat(-14f, 14f), Main.rand.NextFloat(-30f, 6f)),
                    DustID.Shadowflame, Main.rand.NextVector2Circular(2.5f, 2.5f), 140,
                    new Color(140, 60, 210), 1f);
                d.noGravity = true;
            }
        }

        /// <summary>SOLTAR la ofrenda: la prestada solo se olvida; la física
        /// vuelve al inventario (a la pila gemela o a un hueco) y, si no
        /// cabe, al cursor — y si el cursor está ocupado, a los pies del
        /// jugador con aviso (jamás se pierde).</summary>
        private static void SoltarLaOfrenda(bool avisar)
        {
            Player yo = Main.LocalPlayer;
            if (_armaOrigen != null)
            {
                string nombre = _armaOrigen.IsAir ? "" : _armaOrigen.Name;
                _armaOrigen = null;
                if (avisar && !string.IsNullOrEmpty(nombre))
                    Decir("OfrendaSoltada", ChatApagado, nombre);
            }
            if (_arma == null || _arma.IsAir) return;
            Item fisica = _arma;
            _arma = new Item();
            if (yo == null || !yo.active) return;
            Entregar(yo, fisica);
        }

        // ================================================================
        //  LA CONSAGRACIÓN (el botón CONSAGRAR)
        // ================================================================
        public static void ClicEnConsagrar()
        {
            try
            {
                Player yo = Main.LocalPlayer;
                if (yo == null || !yo.active || !_abierta) return;
                ConsagrarDesdeElLibro(yo);
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("AltarOfrendasUI", exRegistro); }
        }

        private static void Sellar(Item arma)
        {
            Corrupto.Consagrar(arma);
            arma.ResetPrefix();
            Corrupto.VestirLaRareza(arma);
        }

        private static void ConsagrarDesdeElLibro(Player yo)
        {
            Item arma = Ofrenda;
            if (arma == null || arma.IsAir) return;
            if (!EsOfrendaConsagrible(arma))
            {
                Decir("YaConsagrada", ChatRojo);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                return;
            }
            bool prestada = OfrendaPrestada;
            bool esEsencias = arma.type == TipoEsencias;

            // ── LAS ESENCIAS: EL ETERNO LAS DEVORA ──
            if (esEsencias)
            {
                DepositarUnMonton(arma, prestada);
                return;
            }

            // ── EL ARMA O EL MATERIAL: EL SELLO ──
            int coste = Corrupto.CosteCorrupcion(arma);
            if (Corrupto.EsenciasEnLaBolsa(yo) < coste)
            {
                Decir("FaltanEsencias", ChatRojo);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                return;
            }
            string nombre = arma.Name;
            bool esArma = EsArmaDeVerdad(arma);

            if (prestada)
            {
                Item origen = _armaOrigen;
                _armaOrigen = null;
                if (origen.stack > 1)
                {
                    // LA PILA PAGA UNO: la unidad sellada vuelve al inventario
                    Item consagrada = (Item)origen.Clone();    // la copia nace primero…
                    consagrada.stack = 1;
                    origen.stack--;                            // …el original cede uno después
                    Sellar(consagrada);
                    Entregar(yo, consagrada);
                }
                else
                {
                    // LA MISMA ARMA, EN SU SITIO: el único ejemplar se sella
                    // donde vive (la letra: «el juego devuelva la misma arma ya
                    // con el modificador consagrada»)
                    Sellar(origen);
                    SincronizarRanura(yo, origen);
                }
            }
            else
            {
                // LA FÍSICA: se sella y vuelve al dueño
                Item consagrada = _arma;
                if (consagrada.stack > 1)
                {
                    consagrada = (Item)_arma.Clone();
                    consagrada.stack = 1;
                    _arma.stack--;                           // el resto de la pila sigue en el hueco
                }
                else _arma = new Item();
                Sellar(consagrada);
                Entregar(yo, consagrada);
            }

            // EL PAGO (después del sello: lo consagrado ya está a salvo)
            Corrupto.PagarCorrupcion(yo, coste);
            Submundo.EsenciasConsagradas += coste;
            if (Submundo.PuertaAbierta)
            {
                AnunciarLaPuerta();
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item113.WithPitchOffset(-0.7f));
            }
            // v6.179 — EL TRAGO DE LA PRIMERA VEZ: las mil se COMEN y la
            // puerta queda VIVA PARA SIEMPRE (PuertaViva — nada la cierra).
            Submundo.TragarLaPuerta();
            Decir(esArma ? "ConsagradaHechaArma" : "ConsagradaHechaMaterial", ChatVioleta, nombre);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item113.WithPitchOffset(-0.45f));
            LluviaDeBruma(22, 8f);
        }

        /// <summary>Las esencias de UNA pila (el cursor o la ofrenda) suben
        /// a la puerta; la pila paga lo que la puerta necesita.</summary>
        private static void DepositarUnMonton(Item pila, bool prestada)
        {
            int topePuerta = Submundo.EsenciasPuerta;
            // v6.179 — LA PUERTA YA VIVA no vuelve a cobrar (la letra:
            // «una vez activo debe estar activo siempre»).
            if (Submundo.PuertaViva || topePuerta - Submundo.EsenciasConsagradas <= 0)
            {
                Decir("PuertaYaAbierta", ChatOro);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                return;                         // la pila queda donde está
            }
            int falta = topePuerta - Submundo.EsenciasConsagradas;
            int toma = Math.Min(falta, pila.stack);
            bool yaAbierta = Submundo.PuertaAbierta;
            Submundo.EsenciasConsagradas += toma;
            pila.stack -= toma;                 // LA PILA PAGA (en el inventario o en el hueco)
            if (pila.stack <= 0) pila.TurnToAir();
            Decir("DevoradasHecha", ChatVioleta, toma, Submundo.EsenciasConsagradas, topePuerta);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item113.WithPitchOffset(-0.45f));
            if (!yaAbierta && Submundo.PuertaAbierta)
            {
                AnunciarLaPuerta();
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item113.WithPitchOffset(-0.7f));
            }
            // v6.179 — EL TRAGO DE LA PRIMERA VEZ: las mil se COMEN y la
            // puerta queda VIVA PARA SIEMPRE (PuertaViva — nada la cierra).
            Submundo.TragarLaPuerta();
            LluviaDeBruma(22, 8f);
            if (prestada) _armaOrigen = null;
            else if (_arma == null || _arma.IsAir) _arma = new Item();   // v6.179: nulo-seguro (el barrendero puede anular el estático)
        }

        /// <summary>ENTREGAR al inventario lo ya sellado (o la ofrenda que
        /// vuelve): a la pila gemela (mismo sello), a un hueco, al cursor
        /// libre — y si nada: a los pies del jugador, con aviso.</summary>
        private static void Entregar(Player yo, Item it)
        {
            if (AdoptarItem(yo, it)) return;
            if (Main.mouseItem.IsAir)
            {
                Main.mouseItem = it;
                return;
            }
            yo.QuickSpawnItem(yo.GetSource_Misc("AltarCorrupcion"), it, it.stack);
            Decir("DevueltaAlSuelo", ChatRojo);
        }

        private static void SincronizarRanura(Player yo, Item it)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient) return;
            for (int k = 0; k < 58; k++)
                if (ReferenceEquals(yo.inventory[k], it))
                {
                    NetMessage.SendData(MessageID.InstancedItem, number: yo.whoAmI, number2: k,
                        number3: it.prefix);
                    return;
                }
        }

        // ================================================================
        //  EL INVENTARIO: lo consagrado NUNCA se funde con lo limpio
        // ================================================================
        private static bool Consagrado(Item it)
        {
            try { return it.GetGlobalItem<Corrupto>().Corrompido; }
            catch { return false; }
        }

        /// <summary>¿Cabe en el inventario? (pila gemela del mismo tipo,
        /// prefijo Y sello, o un hueco libre).</summary>
        private static bool CabeEnLaBolsa(Player yo, Item it)
        {
            for (int k = 0; k < 58; k++)
            {
                Item inv = yo.inventory[k];
                if (inv == null || inv.IsAir) continue;
                if (inv.type != it.type || inv.prefix != it.prefix) continue;
                if (Consagrado(inv) != Consagrado(it)) continue;
                if (inv.stack + it.stack <= inv.maxStack) return true;
            }
            for (int k = 0; k < 50; k++)
                if (yo.inventory[k] == null || yo.inventory[k].IsAir) return true;
            return false;
        }

        /// <summary>METER al inventario SIN PASAR POR EL SUELO: primero la
        /// pila gemela (mismo tipo, prefijo y sello), luego el primer
        /// hueco. Devuelve false si no cupo.</summary>
        private static bool AdoptarItem(Player yo, Item it)
        {
            if (yo == null || !yo.active || it == null || it.IsAir) return false;
            for (int k = 0; k < 58; k++)
            {
                Item inv = yo.inventory[k];
                if (inv == null || inv.IsAir) continue;
                if (inv.type != it.type || inv.prefix != it.prefix) continue;
                if (Consagrado(inv) != Consagrado(it)) continue;
                if (inv.stack + it.stack <= inv.maxStack)
                {
                    inv.stack += it.stack;
                    return true;
                }
            }
            for (int k = 0; k < 50; k++)
            {
                if (yo.inventory[k] == null || yo.inventory[k].IsAir)
                {
                    yo.inventory[k] = it;
                    return true;
                }
            }
            return false;
        }


        // ================================================================
        //  LA VITRINA: elegir un candidato
        // ================================================================
        public static void ClicEnCandidato(int indice, int ranuraDestino)
        {
            try
            {
                Player yo = Main.LocalPlayer;
                if (yo == null || !yo.active || !_abierta || ranuraDestino == int.MinValue) return;
                bool paraTrio = ranuraDestino >= 0;
                List<Item> candidatos = CosecharCandidatos(paraTrio);
                if (indice < 0 || indice >= candidatos.Count) return;
                Item elegido = candidatos[indice];
                if (paraTrio)
                {
                    TrioArma trio = TrioArma.Del(yo);
                    if (trio == null)
                    {
                        Decir("TrioSinTrio", ChatApagado);
                        return;
                    }
                    if (!TrioArma.PuedeEntrar(elegido, out string razon))
                    {
                        Main.NewText(razon, ChatRojo);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                        return;
                    }
                    MeterEnLaRanura(yo, trio, ranuraDestino, elegido, false);
                }
                else PrestarLaOfrenda(elegido);
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("AltarOfrendasUI", exRegistro); }
        }

        /// <summary>EL PRÉSTAMO: el hueco recuerda la referencia viva de la
        /// pila. NADA sale del inventario.</summary>
        private static void PrestarLaOfrenda(Item elegido)
        {
            if (!EsOfrendaConsagrible(elegido))
            {
                Decir(Consagrado(elegido) ? "YaConsagrada" : "NoArma", ChatRojo);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                return;
            }
            if (_arma != null && !_arma.IsAir) SoltarLaOfrenda(false);   // la física anterior vuelve
            _armaOrigen = elegido;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Grab);
            Decir("OfrendaElegida", ChatVioleta, elegido.Name);
        }

        // ================================================================
        //  EL TRÍO: las tres ranuras
        // ================================================================

        /// <summary>METER un arma en LA RANURA CLICADA. La copia nace
        /// PRIMERO y el original muere DESPUÉS; si la ranura estaba
        /// ocupada, la vieja vuelve al inventario (jamás se pierde).</summary>
        private static void MeterEnLaRanura(Player yo, TrioArma trio, int ranura, Item arma, bool delCursor)
        {
            Item vieja = (trio.Ranuras != null && ranura >= 0 && ranura < trio.Ranuras.Length)
                ? trio.Ranuras[ranura] : null;
            bool habiaVieja = vieja != null && !vieja.IsAir;
            if (habiaVieja && !CabeEnLaBolsa(yo, vieja) && (delCursor || !Main.mouseItem.IsAir))
            {
                // el arma vieja no tiene adónde ir: SE QUEDA en su ranura
                Decir("InventarioLleno", ChatRojo, vieja.Name);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                return;
            }

            Item paraRanura = (Item)arma.Clone();     // la copia nace primero…
            paraRanura.stack = 1;
            if (trio.Ranuras == null || trio.Ranuras.Length < 3) trio.Ranuras = new Item[3];
            if (trio.Inteligencia == null || trio.Inteligencia.Length < 3) trio.Inteligencia = new int[3];
            trio.Ranuras[ranura] = paraRanura;
            trio.Inteligencia[ranura] = 0;            // el albedrío es del arma: la nueva nace Original
            trio.PonerSeleccion(ranura, true);        // v6.182 — nace ENCIENDA: dispara desde el primer clic

            // …y el original muere después: UNA sola arma en total
            arma.stack--;
            if (arma.stack <= 0) arma.TurnToAir();

            if (habiaVieja && !AdoptarItem(yo, vieja))
                Main.mouseItem = vieja;               // solo cabe así viniendo de la vitrina

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Grab);
            DecirItem("TrioInsertada", ChatVioleta, ranura + 1);
            if (paraRanura.shoot <= 0)
                DecirItem("TrioSinProyectil", ChatApagado, paraRanura.Name);
        }

        /// <summary>CLIC IZQUIERDO en una ranura con un arma en el cursor:
        /// la deja en ESA ranura.</summary>
        public static void ClicEnRanura(int slotT)
        {
            try
            {
                Player yo = Main.LocalPlayer;
                if (yo == null || !yo.active || !_abierta || slotT < 0 || slotT > 2) return;
                TrioArma trio = TrioArma.Del(yo);
                if (trio == null)
                {
                    Decir("TrioSinTrio", ChatApagado);
                    return;
                }
                Item enCursor = Main.mouseItem;
                if (enCursor == null || enCursor.IsAir) return;
                if (!TrioArma.PuedeEntrar(enCursor, out string razon))
                {
                    Main.NewText(razon, ChatRojo);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                    return;
                }
                MeterEnLaRanura(yo, trio, slotT, enCursor, true);
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("AltarOfrendasUI", exRegistro); }
        }

        /// <summary>SACAR el arma de una ranura: al inventario (o al cursor
        /// si está libre). Devuelve false — sin tocar nada — si no tiene
        /// adónde ir.</summary>
        private static bool SacarDeLaRanura(Player yo, TrioArma trio, int i)
        {
            if (trio.Ranuras == null || i < 0 || i >= trio.Ranuras.Length) return false;
            Item arma = trio.Ranuras[i];
            if (arma == null || arma.IsAir) return false;
            bool alInventario = AdoptarItem(yo, arma);
            if (!alInventario && !Main.mouseItem.IsAir) return false;
            trio.Ranuras[i] = new Item();
            if (trio.Inteligencia != null && i < trio.Inteligencia.Length) trio.Inteligencia[i] = 0;
            trio.PonerSeleccion(i, false);   // v6.182 — sin arma no hay marca
            if (!alInventario) Main.mouseItem = arma;
            return true;
        }

        /// <summary>CLIC DERECHO en una ranura: el arma vuelve al
        /// inventario. Si no cabe, SE QUEDA en la ranura (nada se pierde).</summary>
        public static void DesequiparRanura(int slotT)
        {
            try
            {
                Player yo = Main.LocalPlayer;
                if (yo == null || !yo.active || !_abierta || slotT < 0 || slotT > 2) return;
                TrioArma trio = TrioArma.Del(yo);
                if (trio == null || trio.Ranuras == null || slotT >= trio.Ranuras.Length) return;
                Item arma = trio.Ranuras[slotT];
                if (arma == null || arma.IsAir) return;
                if (!SacarDeLaRanura(yo, trio, slotT))
                {
                    Decir("InventarioLleno", ChatRojo, arma.Name);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                    return;
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Grab);
                DecirItem("TrioExtraida", ChatVioleta, slotT + 1);
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("AltarOfrendasUI", exRegistro); }
        }

        public static void ClicEnDesequiparTodo()
        {
            try
            {
                Player yo = Main.LocalPlayer;
                if (yo == null || !yo.active || !_abierta) return;
                TrioArma trio = TrioArma.Del(yo);
                if (trio == null || trio.Ranuras == null) return;
                int devueltas = 0;
                string primera = "";
                for (int i = 0; i < 3; i++)
                {
                    if (SacarDeLaRanura(yo, trio, i)) devueltas++;
                    else if (primera.Length == 0 && i < trio.Ranuras.Length &&
                        trio.Ranuras[i] != null && !trio.Ranuras[i].IsAir)
                        primera = trio.Ranuras[i].Name;
                }
                if (devueltas > 0)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Grab);
                    DecirItem("TrioTodoDevuelto", ChatVioleta, devueltas);
                }
                if (primera.Length > 0) Decir("InventarioLleno", ChatRojo, primera);
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("AltarOfrendasUI", exRegistro); }
        }

        /// <summary>v6.182 — LA SELECCIÓN DEL DISPARO (la letra: «solo hay
        /// que marcar el arma con click izquierdo para que el slot se
        /// active y se dispare con esa arma, si se selecciona 2 slot
        /// disparan esas 2»): el clic izquierdo en una ranura LLENA la
        /// ENCIENDE o la apaga — disparan SOLO las marcadas.</summary>
        public static void ClicEnSeleccionRanura(int slotT)
        {
            try
            {
                Player yo = Main.LocalPlayer;
                if (yo == null || !yo.active || !_abierta) return;
                TrioArma trio = TrioArma.Del(yo);
                if (trio == null || trio.Ranuras == null || slotT < 0 || slotT > 2) return;
                Item arma = slotT < trio.Ranuras.Length ? trio.Ranuras[slotT] : null;
                if (arma == null || arma.IsAir) return;   // la vacía no se marca
                bool ahora = !trio.Seleccionada(slotT);
                trio.PonerSeleccion(slotT, ahora);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                // v6.183 — EL GUARDIÁN DE LAS LLAVES (la letra: la
                // captura del chat PINTANDO «Mods.AethonMod.Items.
                // TrioMarcaPuesta» crudo): las claves de la selección
                // viven en la categoría ALTAR (Altar.TrioMarcaPuesta)
                // y esta mano usaba el helper de Items — cada clic en
                // una ranura imprimía la clave cruda. Decir() es el
                // helper del ALTAR: el que apunta a donde viven.
                Decir(ahora ? "TrioMarcaPuesta" : "TrioMarcaQuitada",
                    ahora ? ChatVioleta : ChatApagado, slotT + 1);
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("AltarOfrendasUI", exRegistro); }
        }

        /// <summary>EL ALBEDRÍO (v6.174, la letra: «crear un selector tipo
        /// checklist para dar inteligencia a los proyectiles»): por ranura,
        /// Original / Presa / Cursor. Tocar la que ya está viva la devuelve
        /// a Original.</summary>
        public static void ClicEnAlbedrio(int ranura, int modo)
        {
            try
            {
                Player yo = Main.LocalPlayer;
                if (yo == null || !yo.active || !_abierta || ranura < 0 || ranura > 2 ||
                    modo < 0 || modo > 2) return;
                TrioArma trio = TrioArma.Del(yo);
                if (trio == null) return;
                if (trio.Inteligencia == null || trio.Inteligencia.Length < 3) trio.Inteligencia = new int[3];
                if (trio.Ranuras == null || ranura >= trio.Ranuras.Length ||
                    trio.Ranuras[ranura] == null || trio.Ranuras[ranura].IsAir)
                    return;                                   // sin arma no hay albedrío que elegir
                int actual = trio.Inteligencia[ranura];
                int nuevo = (modo != 0 && actual == modo) ? 0 : modo;
                trio.Inteligencia[ranura] = nuevo;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                DecirItem("TrioAlbedrioCambiada", ChatVioleta, ranura + 1,
                    Language.GetTextValue("Mods.AethonMod.Items.TrioAlbedrio" + nuevo));
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("AltarOfrendasUI", exRegistro); }
        }

        // ================================================================
        //  LA LEY DEL ALTAR Y LA VIDA DEL PRÉSTAMO (cada frame)
        // ================================================================
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            // v6.163 — EL PANEL FLOTANTE MURIÓ: el contenido vive como
            // PESTAÑA del libro (AltarDelLibro) — este sistema solo sigue
            // siendo EL MOTOR. NADA que insertar: el libro dibuja.
            // LA LEY DEL ALTAR SIGUE VIVA: cada frame, sin altar físico
            // (roto o lejos), el altar se cierra — y si el libro está en
            // la pestaña altar, se cierra TAMBIÉN (nada de clics fantasma).
            if (!_abierta) return;
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
                    ForzarCierre();
                    if (LibroDelEternoUI.Abierto && LibroDelEternoUI.Pestana == 1)
                        LibroDelEternoUI.Cerrar();
                    return;
                }
                // v6.174 — EL PRÉSTAMO SE VIGILA: si la pila prestada ya no
                // vive en el inventario (se vendió, se tiró, se gastó), el
                // préstamo muere y el hueco queda libre — se avisa.
                if (_armaOrigen != null && !PrestadaViva(yo, _armaOrigen))
                {
                    _armaOrigen = null;
                    Decir("OfrendaPerdida", ChatRojo);
                }
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("AltarOfrendasUI", exRegistro); }
        }

        /// <summary>¿LA PILA prestada sigue en el inventario (o en el
        /// cursor) de su dueño? La MISMA referencia — sin adivinar.</summary>
        private static bool PrestadaViva(Player yo, Item pila)
        {
            if (pila == null || pila.IsAir || yo == null) return false;
            for (int k = 0; k < 58; k++)
                if (ReferenceEquals(yo.inventory[k], pila)) return true;
            return ReferenceEquals(Main.mouseItem, pila);
        }

        // ================================================================
        //  LA PERSISTENCIA — SIN DUPLICACIONES NI PÉRDIDAS
        //  La ofrenda FÍSICA vive en el archivo del mundo (es un cofre).
        //  La PRESTADA nunca se guarda: vive con el jugador, en su
        //  inventario. Al descargar el mundo NADA se devuelve: devolver
        //  aquí lo que el archivo ya guarda era la duplicación silenciosa.
        // ================================================================
        public override void OnWorldUnload()
        {
            _abierta = false;
            _armaOrigen = null;
            // v6.179 — LA OFRENDA FÍSICA muere con el mundo (vive en SU
            // archivo: LoadWorldData la devuelve al volver) — el campo
            // estático con un ítem del mod dentro era UN ANCLA de memoria
            // (el warn del client.log «still using memory»).
            _arma = null;
        }

        public override void Unload()
        {
            _abierta = false;
            _armaOrigen = null;
        }

        public override void SaveWorldData(TagCompound tag)
        {
            try
            {
                if (_arma != null && !_arma.IsAir)
                    tag["altarArma"] = ItemIO.Save(_arma);
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("AltarOfrendasUI", exRegistro); }
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
                _armaOrigen = null;
                _abierta = false;
            }
            catch { _arma = new Item(); }
        }
    }
}
