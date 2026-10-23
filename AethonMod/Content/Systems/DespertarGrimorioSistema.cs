using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace AethonMod.Content.Systems
{
    // ======================================================================
    //  EL SISTEMA DEL DESPERTAR (v6.51.0) — EL COMENSAL Y SU ESCALERA.
    //
    // LA DEVORACIÓN (la letra: «cuando el arma es soltada por el jugador
    // y el grimorio está seleccionado, este puede devorar el arma»): cada
    // 15 t la AUTORIDAD mira los items CAÍDOS alrededor de quien blande
    // el Eterno — un ARMA CORRUPTA soltada ante él es un PLATO:
    //   · Si es LA OFRENDA del siguiente nivel y el portador trae las
    //     ESENCIAS y el DINERO del peldaño: NIVEL DE DESPERTAR +1 (las
    //     tres ofrendas se consumen — el arma, las esencias y las
    //     monedas) y los PODERES desbloqueados se anuncian.
    //   · Si es la ofrenda pero FALTA algo: el grimorio REHÚSA el plato
    //     y dice qué falta (el arma queda en el suelo, recogerla es
    //     gratis).
    //   · Si es CUALQUIER OTRA arma corrupta: EL BOCADO DE SIEMPRE (el
    //     festín de la .129 vive): EXP por rareza + UN STAT por clase +
    //     EL ALMA ÚNICA si era especial.
    //
    // LA ESENCIA CORRUPTA (la letra: «en el momento en el que el grimorio
    // hace su aparición en el mundo, su poder se filtra entre las
    // realidades… se pega a ellos»): desde LA LLEGADA, toda criatura
    // puede soltar 1 (0,1 %) y los jefes 1..10 (90 % → 1 · 5 % → 3 ·
    // 1 % → 10 · el resto repartido) — la chusma de las OLEADAS DE
    // HAMBRE la suelta al 2 % (el propio grimorio los llama) y sus
    // jefes pagan 10 (EsenciasJefesDrops).
    // ======================================================================
    public class DespertarGrimorioSistema : ModSystem
    {
        /// <summary>El pulso del comensal (cada 15 t — 4 veces por
        /// segundo: un plato no se enfría en el suelo).</summary>
        private const int PULSO_DEVORAR = 15;

        /// <summary>El radio del tenedor: items caídos a menos de 200 px
        /// del portador con el Eterno en la MANO.</summary>
        private const float RADIO_DEVORAR = 200f;

        public override void PreUpdateTime()
        {
            try
            {
                if (Main.gameMenu) return;
                if (Main.netMode == NetmodeID.MultiplayerClient) return;
                if ((Main.GameUpdateCount % (ulong)PULSO_DEVORAR) != 0ul) return;

                int eterno = ModContent.ItemType<global::AethonMod.Content.Weapons.GrimoireEternal>();

                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player p = Main.player[i];
                    if (p == null || !p.active || p.dead) continue;

                    // ====================================================
                    //  v6.51.10 — LA DEVORACIÓN DE LA MOCHILA, PRIMERO (la
                    //  letra: «pese a que cumpli con los requisitos del
                    //  despertar el grimorio no aumenta su nivel de
                    //  despertar […] ahora cuando los requisistos se
                    //  cumpla el grimorio los debe consumir automticamente
                    //  del inventario y subir su nivel de despertar
                    //  comiendo directamente del inventario […] los items
                    //  necesarios para subir el nivel del despertar deben
                    //  estar en cualquier parte del inventario del
                    //  jugador»): el Eterno ACTIVO (en la MANO o en la
                    //  BOLSA — cualquier rincón) revisa su mesa CADA PULSO;
                    //  con las esencias + el dinero + LA OFRENDA (el arma
                    //  corrupta del peldaño O los lingotes) completas, SE
                    //  SIRVE SOLO y DESPIERTA (+1). Nada se tira al suelo.
                    // ====================================================
                    AutoDevorarOfrenda(p);

                    // EL GRIMORIO SELECCIONADO (los platos CAÍDOS siguen
                    // pidiendo el libro en la MANO — el gesto de siempre).
                    Item mano = p.HeldItem;
                    if (mano == null || mano.IsAir || mano.type != eterno) continue;

                    // v6.51.1 — LAS ESENCIAS SOLTADAS (la letra: «se debe
                    //     poder seguir devorando esencia corrupta»): con el
                    //     Eterno EN LA MANO, la Esencia Corrupta caída a
                    //     ≤200 px es un bocado INMEDIATO… PERO v6.51.6 —
                    //     EL REQUISITO PRIMERO (la letra: «las escencia
                    //     corrupta no se contabilizan correctamente como el
                    //     requisito para el despertar» — el bocado viejo se
                    //     comía TODO lo caído y el requisito quedaba en 0/2
                    //     PARA SIEMPRE): mientras la ofrenda del próximo
                    //     despertar de ESTE LIBRO no esté completa en el
                    //     INVENTARIO, lo caído NO SE TOCA (el portador lo
                    //     recoge y cuenta); SOLO el EXCESO se devora como
                    //     +0,1 % de daño PERMANENTE — y ese poder es DEL
                    //     LIBRO (v6.51.6: las estadísticas son exclusivas
                    //     de cada grimorio).
                    int tipoEsencia = ModContent.ItemType
                        <global::AethonMod.Content.Items.Altar.ItemCorrupto>();
                    var almaMano = mano.GetGlobalItem
                        <global::AethonMod.Content.Globals.AlmaEternoGlobalItem>();
                    for (int d = 0; d < Main.maxItems; d++)
                    {
                        Item drop = Main.item[d];
                        if (drop == null || drop.IsAir) continue;
                        if (drop.type != tipoEsencia) continue;
                        if (drop.Center.DistanceSQ(p.Center) > RADIO_DEVORAR * RADIO_DEVORAR)
                            continue;

                        int comidas = drop.stack;
                        if (comidas <= 0 || almaMano == null) continue;

                        // ¿LA OFRENDA YA ESTÁ COMPLETA en el inventario?
                        // (al tope del despertar ya no hay ofrenda: todo es
                        // exceso — el hambre del libro no termina.)
                        int proximoNivel = System.Math.Min(
                            DespertarGrimorio.NivelDespertarSiguiente(mano),
                            DespertarGrimorio.NivelMax);
                        bool ofrendaIncompleta = almaMano.NivelDespertar < DespertarGrimorio.NivelMax &&
                            DespertarGrimorio.EsenciasEnManos(p) <
                            DespertarGrimorio.EsenciasPara(proximoNivel);

                        if (ofrendaIncompleta)
                        {
                            // SE QUEDA EN EL SUELO (la recogida natural la
                            // cuenta para el requisito): silencio — la
                            // lista del despertar ya cuenta lo que falta.
                            continue;
                        }

                        // EL EXCESO: EL BOCADO DE PODER DE SIEMPRE (+0,1 %
                        // PERMANENTE por esencia — del LIBRO en la mano).
                        almaMano.DevoracionPoder += comidas;
                        string nombre = drop.Name;
                        drop.TurnToAir();
                        int idd = Array.IndexOf(Main.item, drop);
                        if (Main.netMode == NetmodeID.Server && idd >= 0)
                            NetMessage.SendData(MessageID.SyncItem, -1, -1, null, idd);

                        if (!Main.dedServ)
                        {
                            for (int k = 0; k < Math.Min(20, 4 + comidas); k++)
                            {
                                Dust dd = Dust.NewDustPerfect(p.Center, DustID.Shadowflame,
                                    new Vector2(Main.rand.NextFloat(-2.4f, 2.4f),
                                        Main.rand.NextFloat(-3.2f, 0.4f)), 150,
                                    new Color(120, 40, 180), 0.95f);
                                dd.noGravity = true;
                            }
                            Terraria.Audio.SoundEngine.PlaySound(
                                SoundID.Item113.WithPitchOffset(-0.45f), p.Center);
                        }

                        EcoRed.AnunciarAlPortador(p, "Mods.AethonMod.Despertar.EsenciaDevorada",
                            new Color(196, 138, 255), nombre, comidas,
                            (global::AethonMod.Content.Globals.AlmaEternoGlobalItem
                                .BonusDanoDevorado(mano) * 100f).ToString("0.##"));
                        EcoRed.SincronizarDevoracion(p);
                    }

                    // === v6.51.10 — LOS PLATOS DE ARMAS CAÍDAS (el festín
                    //     de la .129 VIVE — el gesto de siempre): con el
                    //     Eterno EN LA MANO, toda ARMA CORRUPTA soltada a
                    //     su lado sigue siendo EL BOCADO DE SIEMPRE (EXP +
                    //     stats + almas). Las OFRENDAS del despertar ya NO
                    //     se tiran al suelo: la mesa se sirve sola del
                    //     INVENTARIO (AutoDevorarOfrenda arriba) — la
                    //     letra: «quitar esos item consumidos». ===
                    for (int d = 0; d < Main.maxItems; d++)
                    {
                        Item drop = Main.item[d];
                        if (drop == null || drop.IsAir) continue;   // SOLO items caídos vivos
                        if (drop.type == tipoEsencia) continue;      // las esencias: su propia mesa
                        if (drop.Center.DistanceSQ(p.Center) > RADIO_DEVORAR * RADIO_DEVORAR)
                            continue;

                        var sello = drop.GetGlobalItem
                            <global::AethonMod.Content.Globals.CorruptoGlobalItem>();
                        if (sello == null || !sello.Corrompido) continue;
                        if (!EsArma(drop)) continue;

                        // EL BOCADO DE SIEMPRE: el festín de la .129
                        // (del LIBRO en la mano — su EXP, sus stats).
                        DevorarComoBocado(p, mano, drop);
                        break;   // un plato por pulso (con calma, que es un libro)
                    }
                }
            }
            catch { }
        }

        // ================================================================
        //  v6.51.10 — LA DEVORACIÓN DE LA MOCHILA (la letra del usuario:
        //  «pese a que cumpli con los requisitos del despertar el
        //  grimorio no aumenta su nivel de despertar […] ahora cuando
        //  los requisistos se cumpla el grimorio los debe consumir
        //  automticamente del inventario y subir su nivel de despertar
        //  comiendo directamente del inventario por lo tanto quitar esos
        //  item consumidos […] es o las armas o el lingote, con que se
        //  cumpla uno de ellos es suficiente»): cada pulso, el ETERNO
        //  ACTIVO del portador (en la mano o en la bolsa — «cualquier
        //  parte del inventario») revisa su mesa: con las ESENCIAS, el
        //  DINERO y LA OFRENDA (EL ARMA CORRUPTA del peldaño O LA PILA
        //  DE LINGOTES — basta UNA de las dos), el libro SE LA COME
        //  directamente del inventario y DESPIERTA (+1). Si el dueño ve
        //  su inventario ABIERTO, LA SOMBRA DE SU PÁGINA se estira hasta
        //  los platos y se los lleva a la boca (DevoracionInventarioFX —
        //  la sorpresa de la letra); si está cerrado, sube en silencio.
        // ================================================================
        private static void AutoDevorarOfrenda(Player p)
        {
            try
            {
                if (p == null || !p.active || p.dead) return;

                // EL LIBRO ACTIVO: en la MANO primero… si no, el primero
                // de la bolsa (AlmaEternoGlobalItem.Activo) — «cualquier
                // parte del inventario del jugador».
                Item libro = global::AethonMod.Content.Globals.AlmaEternoGlobalItem.Activo(p);
                var alma = global::AethonMod.Content.Globals.AlmaEternoGlobalItem.De(libro);
                if (libro == null || alma == null) return;
                if (alma.NivelDespertar >= DespertarGrimorio.NivelMax) return;

                int siguiente = System.Math.Min(
                    DespertarGrimorio.NivelDespertarSiguiente(libro),
                    DespertarGrimorio.NivelMax);

                // 1) LAS ESENCIAS — en cualquier parte del inventario
                //    (consumidas «como cualquier consumible»: la letra).
                int esencias = DespertarGrimorio.EsenciasPara(siguiente);
                if (DespertarGrimorio.EsenciasEnManos(p) < esencias) return;

                // 2) EL DINERO (BuyItem valida y cobra al final — si no
                //    alcanza, no se toca NADA).
                long monedas = DespertarGrimorio.MonedasPara(siguiente);
                if (ContarMonedas(p) < monedas) return;

                // 3) LA OFRENDA — el ARMA CORRUPTA del peldaño… O la PILA
                //    de lingotes del metal del nivel (SIEMPRE >50 y
                //    creciente): con que se cumpla UNA basta — la letra.
                //    (El arma tiene preferencia: es el plato canónico.)
                int slotArma = SlotArmaDelPeldano(p, siguiente);   // -1 si no la lleva
                int tipoMin = DespertarGrimorio.MineralDe(siguiente);
                int cantMin = DespertarGrimorio.CantidadMineralDe(siguiente);
                bool porMineral = slotArma < 0 &&
                    DespertarGrimorio.MineralesEnManos(p, siguiente) >= cantMin;
                if (slotArma < 0 && !porMineral) return;

                // — LA MESA ESTÁ COMPLETA: el Eterno SE SIRVE SOLO —
                var victimas = new System.Collections.Generic.List<
                    global::AethonMod.Content.Globals.DevoracionInventarioFX.Victima>();

                if (!p.BuyItem(monedas, -1)) return;   // las monedas (de donde sean — vanilla)

                ConsumirEsenciasConVictimas(p, esencias, victimas);
                if (slotArma >= 0)
                {
                    Item plato = p.inventory[slotArma];
                    victimas.Add(new global::AethonMod.Content.Globals.
                        DevoracionInventarioFX.Victima(slotArma, plato.type, 1));
                    plato.stack--;
                    if (plato.stack <= 0) plato.TurnToAir();
                }
                else
                {
                    ConsumirMineralesConVictimas(p, tipoMin, cantMin, victimas);
                }

                // LA SOMBRA DE LA PÁGINA (solo en la pantalla del dueño —
                // SP y anfitrión «Host & Play»; con el inventario ABIERTO:
                // la letra: «seria interesante una animacion donde el
                // grimorio del inventario extiende su sombra de la pagina
                // hasta los items correctos y estos son automaticamente
                // devorados en caso de que el inventario este abierto, si
                // el inventario no esta abierto pues no pasa nada y solo
                // sube su nivel de despertar»).
                if (!Main.dedServ && p.whoAmI == Main.myPlayer && Main.playerInventory &&
                    victimas.Count > 0)
                {
                    global::AethonMod.Content.Globals.DevoracionInventarioFX
                        .Disparar(p, victimas, libro);
                }

                // EL AVISO DEL SERVICIO (el portador se entera igual con
                // el inventario cerrado — y con él abierto, la sorpresa).
                EcoRed.AnunciarAlPortador(p, "Mods.AethonMod.Despertar.DevoraInventario",
                    new Color(196, 138, 255), siguiente);

                SubirNivel(p, libro, null);   // la ofrenda ya está comida
            }
            catch { }
        }

        /// <summary>¿Dónde lleva el arma CORRUPTA del peldaño? (el
        /// PRIMER slot del inventario que la traiga — cualquier rincón;
        /// -1 si no la lleva).</summary>
        private static int SlotArmaDelPeldano(Player p, int nivel)
        {
            if (p == null || nivel < 1 || nivel > DespertarGrimorio.NivelMax) return -1;
            int[] peldano = DespertarGrimorio.ArmasDelNivel[nivel];
            for (int k = 0; k < 58; k++)
            {
                Item it = p.inventory[k];
                if (it == null || it.IsAir) continue;
                for (int a = 0; a < peldano.Length; a++)
                {
                    if (peldano[a] != it.type) continue;
                    if (it.GetGlobalItem
                        <global::AethonMod.Content.Globals.CorruptoGlobalItem>().Corrompido)
                        return k;
                }
            }
            return -1;
        }

        /// <summary>COME N esencias del inventario ANOTANDO los slots
        /// (para que la sombra sepa a quién mordió — el FX).</summary>
        private static void ConsumirEsenciasConVictimas(Player p, int cantidad,
            System.Collections.Generic.List<global::AethonMod.Content.Globals.
                DevoracionInventarioFX.Victima> victimas)
        {
            int tipo = ModContent.ItemType
                <global::AethonMod.Content.Items.Altar.ItemCorrupto>();
            int falta = cantidad;
            for (int k = 0; k < 58 && falta > 0; k++)
            {
                Item it = p.inventory[k];
                if (it == null || it.IsAir || it.type != tipo) continue;
                int quita = Math.Min(falta, it.stack);
                victimas.Add(new global::AethonMod.Content.Globals.
                    DevoracionInventarioFX.Victima(k, tipo, quita));
                it.stack -= quita;
                falta -= quita;
                if (it.stack <= 0) p.inventory[k].TurnToAir();
            }
        }

        /// <summary>COME los lingotes de la ofrenda del inventario
        /// ANOTANDO los slots (exactamente LA CANTIDAD del peldaño — el
        /// sobrante queda en la bolsa: recogerlo ya fue gratis).</summary>
        private static void ConsumirMineralesConVictimas(Player p, int tipo, int cantidad,
            System.Collections.Generic.List<global::AethonMod.Content.Globals.
                DevoracionInventarioFX.Victima> victimas)
        {
            int falta = cantidad;
            for (int k = 0; k < 58 && falta > 0; k++)
            {
                Item it = p.inventory[k];
                if (it == null || it.IsAir || it.type != tipo) continue;
                int quita = Math.Min(falta, it.stack);
                victimas.Add(new global::AethonMod.Content.Globals.
                    DevoracionInventarioFX.Victima(k, tipo, quita));
                it.stack -= quita;
                falta -= quita;
                if (it.stack <= 0) p.inventory[k].TurnToAir();
            }
        }

        // ================================================================
        //  LA SUBIDA DE NIVEL (las tres ofrendas ya están pagas)
        // ================================================================

        /// <summary>SUBE EL DESPERTAR (+1) y celebra: la voz del libro,
        /// el aviso dorado, los polvos, el hito si el nivel desbloquea
        /// algo… y en el 20, LA PELÍCULA DE LA MASCOTA. v6.51.6 — el
        /// despertar sube EN EL GRIMORIO que devoró la ofrenda (cada
        /// libro es su propia escalera).</summary>
        public static void SubirNivel(Player p, Item grimorio, Item ofrenda)
        {
            try
            {
                var alma = grimorio?.GetGlobalItem
                    <global::AethonMod.Content.Globals.AlmaEternoGlobalItem>();
                if (alma == null) return;
                if (alma.NivelDespertar >= DespertarGrimorio.NivelMax) return;

                alma.NivelDespertar++;
                int nivel = alma.NivelDespertar;

                // LA OFRENDA SE LA COME (el item muere en todas las pantallas).
                if (ofrenda != null && !ofrenda.IsAir)
                {
                    ofrenda.TurnToAir();
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.SyncItem, -1, -1, null,
                            Array.IndexOf(Main.item, ofrenda));
                }

                // LOS POLVOS DEL TRAGO (la máquina con pantalla).
                if (!Main.dedServ && p != null)
                {
                    for (int k = 0; k < 26; k++)
                    {
                        Dust d = Dust.NewDustPerfect(p.Center, DustID.Shadowflame,
                            new Vector2(Main.rand.NextFloat(-3f, 3f),
                                Main.rand.NextFloat(-4f, 0.5f)), 160,
                            new Color(140, 60, 210), 1.1f);
                        d.noGravity = true;
                    }
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item113.WithPitchOffset(-0.3f), p.Center);
                }

                // EL AVISO DORADO (el patrón de las subidas de nivel).
                if (Main.netMode != NetmodeID.Server && p == Main.LocalPlayer)
                {
                    Main.NewText(Language.GetTextValue(
                        "Mods.AethonMod.Despertar.Subida", nivel),
                        new Color(196, 138, 255));
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4);
                }
                else if (Main.netMode == NetmodeID.Server)
                {
                    EcoRed.AnunciarAlPortador(p, "Mods.AethonMod.Despertar.Subida",
                        new Color(196, 138, 255), nivel);
                }

                // LA VOZ DEL LIBRO (los pensamientos del portador).
                EcoRed.HablarVarianteAlPortador(p, "Mods.AethonMod.Despertar.Voz",
                    4, new Color(178, 110, 236), rugido: true, escala: 0.62f);

                // EL HITO (si el nivel desbloquea un poder).
                string hito = DespertarGrimorio.ClaveHito(nivel);
                if (hito != null)
                {
                    if (Main.netMode != NetmodeID.Server && p == Main.LocalPlayer)
                        Main.NewText(Language.GetTextValue(hito), new Color(255, 215, 120));
                    else if (Main.netMode == NetmodeID.Server)
                        EcoRed.AnunciarAlPortador(p, hito, new Color(255, 215, 120));
                }

                // NIVEL 20 — LA PELÍCULA DE LA MASCOTA (una sola vez POR
                // LIBRO — cada Eterno escupe al suyo).
                if (nivel == 20 && !alma.PetDelDespertar)
                {
                    alma.PetDelDespertar = true;
                    EscupirMascota(p);
                }

                // EL NUEVO NIVEL CAMINA (el daño del portador lo lee su
                // máquina — el paquete del festín lleva el despertar).
                EcoRed.SincronizarDevoracion(p);
            }
            catch { }
        }

        /// <summary>CUÁNTO DINERO lleva el jugador (en cobre — las 4
        /// monedas del inventario, contadas a mano: la casa no usa la
        /// banca para las ofrendas del despertar).</summary>
        public static long ContarMonedas(Player p)
        {
            if (p == null) return 0L;
            long total = 0L;
            for (int k = 0; k < 58; k++)
            {
                Item it = p.inventory[k];
                if (it == null || it.IsAir) continue;
                if (it.type == ItemID.CopperCoin) total += it.stack;
                else if (it.type == ItemID.SilverCoin) total += (long)it.stack * 100;
                else if (it.type == ItemID.GoldCoin) total += (long)it.stack * 10000;
                else if (it.type == ItemID.PlatinumCoin) total += (long)it.stack * 1000000;
            }
            return total;
        }

        /// <summary>Un precio de cobre vuelto TEXTO de monedas (el formato
        /// de la casa: 1 de platino · 3 de oro · 50 de plata · 10 de cobre).</summary>
        public static string ItemToText(long cobre)
        {
            if (cobre <= 0L) return "0";
            long platino = cobre / 1_000_000L;
            long oro = (cobre % 1_000_000L) / 10_000L;
            long plata = (cobre % 10_000L) / 100L;
            long resto = cobre % 100L;
            var partes = new List<string>();
            if (platino > 0) partes.Add($"{platino} de platino");
            if (oro > 0) partes.Add($"{oro} de oro");
            if (plata > 0) partes.Add($"{plata} de plata");
            if (resto > 0) partes.Add($"{resto} de cobre");
            return string.Join(" · ", partes);
        }

        // ================================================================
        //  EL BOCADO DE SIEMPRE (el festín de la .129 — portado del altar)
        // ================================================================

        /// <summary>EL BOCADO: un arma corrupta que NO era la ofrenda se
        /// la come igual — EXP por rareza, UN stat por clase y EL ALMA
        /// ÚNICA si era especial (el festín del cráter de la .129, ahora
        /// servido EN EL LIBRO QUE COME — v6.51.6: su EXP, sus stats, sus
        /// almas: cada grimorio su propio festín).</summary>
        private static void DevorarComoBocado(Player p, Item grimorio, Item drop)
        {
            try
            {
                var alma = grimorio?.GetGlobalItem
                    <global::AethonMod.Content.Globals.AlmaEternoGlobalItem>();
                if (alma == null) return;

                // EL EXP por rareza (la tabla de la .129).
                alma.DevoracionExp += ExpPorRareza(drop.rare);

                // EL STAT por clase.
                string stat = SubirStatPorClase(alma, drop);

                // EL ALMA ÚNICA (si era especial).
                string almaUnica = DespertarAlma(alma, drop);

                // LA MUERTE DEL PLATO (en todas las pantallas).
                string nombre = drop.Name;
                drop.TurnToAir();
                int idx = Array.IndexOf(Main.item, drop);
                if (Main.netMode == NetmodeID.Server && idx >= 0)
                    NetMessage.SendData(MessageID.SyncItem, -1, -1, null, idx);

                // LOS POLVOS Y LA VOZ.
                if (!Main.dedServ)
                {
                    for (int k = 0; k < 16; k++)
                    {
                        Dust d = Dust.NewDustPerfect(p.Center, DustID.Shadowflame,
                            new Vector2(Main.rand.NextFloat(-2.2f, 2.2f),
                                Main.rand.NextFloat(-3f, 0.4f)), 150,
                            new Color(110, 40, 170), 0.9f);
                        d.noGravity = true;
                    }
                }
                EcoRed.HablarVarianteAlPortador(p, "Mods.AethonMod.Caida.DevoradoSabor",
                    4, new Color(178, 110, 236), rugido: true, escala: 0.58f);

                // EL AVISO DESGLOSADO (el plato contado).
                EcoRed.AnunciarAlPortador(p, "Mods.AethonMod.Caida.ArmaDevorada",
                    new Color(196, 138, 255), nombre, ExpPorRareza(drop.rare),
                    Language.GetTextValue($"Mods.AethonMod.Caida.{stat}"));
                if (almaUnica != null)
                    EcoRed.AnunciarAlPortador(p, "Mods.AethonMod.Caida.AlmaDespertada",
                        new Color(255, 215, 120), nombre,
                        Language.GetTextValue($"Mods.AethonMod.Caida.{almaUnica}"));

                // EL FESTÍN CAMINA (stats nuevos a la máquina del portador).
                EcoRed.SincronizarDevoracion(p);
            }
            catch { }
        }

        /// <summary>¿Es un ARMA? («un arma de cualquier tipo»): la única
        /// verdad de la corrupción — la leían el altar de la .129 y las
        /// recetas de la .130; ahora la leen las recetas y el comensal.</summary>
        public static bool EsArma(Item it)
        {
            return it != null && !it.IsAir && it.damage > 0 && !it.accessory &&
                !it.vanity && it.pick == 0 && it.axe == 0 && it.hammer == 0 &&
                it.ammo == 0 && it.type !=
                ModContent.ItemType<global::AethonMod.Content.Weapons.GrimoireEternal>();
        }

        /// <summary>EL EXP DE UN ARMA por su rareza (la escala de la .129:
        /// la basura alimenta poco, la mítica es un banquete).</summary>
        private static long ExpPorRareza(int rareza)
        {
            switch (rareza)
            {
                case -1: return 5;
                case 0: return 10;
                case 1: return 20;
                case 2: return 45;
                case 3: return 100;
                case 4: return 250;
                case 5: return 600;
                case 6: return 1500;
                case 7: return 4000;
                case 8: return 8000;
                case 9: return 8000;
                default: return rareza >= 10 ? 15000 : 10;
            }
        }

        /// <summary>EL STAT QUE ESTA ARMA SUBE (por clase — la .129 — en
        /// el alma del LIBRO que come: v6.51.6).</summary>
        private static string SubirStatPorClase(
            global::AethonMod.Content.Globals.AlmaEternoGlobalItem alma, Item arma)
        {
            if (arma.DamageType == DamageClass.Melee) { alma.DevoracionFuerza++; return "StatFuerza"; }
            if (arma.DamageType == DamageClass.Ranged) { alma.DevoracionPrisa++; return "StatPrisa"; }
            if (arma.DamageType == DamageClass.Magic) { alma.DevoracionMirada++; return "StatMirada"; }
            if (arma.DamageType == DamageClass.Summon) { alma.DevoracionCorona++; return "StatCorona"; }
            alma.DevoracionMordida++;
            return "StatMordida";
        }

        /// <summary>EL ALMA ÚNICA (las armas especiales de la .129 — el
        /// fuego, el veneno, el vampiro, las estrellas, la velocidad, el
        /// peso: VIVEN en los disparos del Eterno que las comió).</summary>
        private static string DespertarAlma(
            global::AethonMod.Content.Globals.AlmaEternoGlobalItem alma, Item arma)
        {
            int t = arma.type;
            if (t == ItemID.FieryGreatsword || t == ItemID.Sunfury ||
                t == ItemID.Flamelash || t == ItemID.InfernoFork ||
                t == ItemID.Flamethrower || t == ItemID.VolcanoSmall ||
                t == ItemID.VolcanoLarge)
            { alma.AlmaIgnea++; return "AlmaIgnea"; }
            if (t == ItemID.BladeofGrass || t == ItemID.ThornChakram ||
                t == ItemID.BeeGun || t == ItemID.Beenade || t == ItemID.PoisonStaff)
            { alma.AlmaSalvaje++; return "AlmaSalvaje"; }
            if (t == ItemID.Muramasa || t == ItemID.Katana || t == ItemID.FetidBaghnakhs)
            { alma.AlmaVeloz++; return "AlmaVeloz"; }
            if (t == ItemID.Starfury || t == ItemID.StarWrath || t == ItemID.StarCannon)
            { alma.CorazonEstelar++; return "CorazonEstelar"; }
            if (t == ItemID.VampireKnives)
            { alma.AlmaVampira++; return "AlmaVampira"; }
            if (t == ItemID.PaladinsHammer || t == ItemID.GolemFist)
            { alma.PunoTitan++; return "PunoTitan"; }
            return null;
        }

        // ================================================================
        //  NIVEL 20 — LA PELÍCULA DE LA MASCOTA (el escupidizo)
        // ================================================================

        /// <summary>EL GRIMORIO ESCUPE AL MINIGRIMORIO (la letra: «a nivel
        /// 20 el grimorio escupe al minigrimorio con una animación donde
        /// el grimorio sale del jugador, se pone encima de él y en un mar
        /// de brumas escupe al minigrimorio como mascota… esto también
        /// debe revelar algún texto o algo que el grimorio le diga al
        /// jugador»): nace el CINE (EscupidizoProjectile — el mar de
        /// brumas encima del portador) y la mascota queda ENTREGADA (el
        /// item si no lo tenía, el buff si ya lo tenía).</summary>
        public static void EscupirMascota(Player p)
        {
            try
            {
                if (p == null || !p.active) return;

                // LA PELÍCULA (viaja sola a todas las pantallas).
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int pr = Projectile.NewProjectile(p.GetSource_Misc("Despertar"),
                        p.Center, Vector2.Zero,
                        ModContent.ProjectileType
                            <global::AethonMod.Content.Projectiles.Cosmetic.EscupidizoProjectile>(),
                        0, 0f, p.whoAmI);
                    if (pr >= 0)
                        Main.projectile[pr].timeLeft = 300;
                }

                // LA PALABRA DEL GRIMORIO (el texto de la letra).
                EcoRed.HablarAlPortador(p, "Mods.AethonMod.Despertar.Escupidizo",
                    new Color(178, 110, 236), rugido: true, escala: 0.72f, prioridad: true);
            }
            catch { }
        }

        /// <summary>LA MASCOTA YA ESTÁ FUERA: la llama el cine al llegar
        /// su momento (le da el item si no lo tenía; si ya lo tenía, la
        /// invoca AHORA mismo — el escupido es una CEREMONIA).</summary>
        public static void EntregarMascota(Player p)
        {
            try
            {
                if (p == null || !p.active) return;
                int itemMascota = ModContent.ItemType
                    <global::AethonMod.Content.Items.Llamados.GrimorioMenorItem>();

                bool loTenia = false;
                for (int k = 0; k < 58; k++)
                {
                    Item it = p.inventory[k];
                    if (it != null && !it.IsAir && it.type == itemMascota)
                    { loTenia = true; break; }
                }
                if (!loTenia)
                    p.QuickSpawnItem(p.GetSource_Misc("Despertar"), itemMascota, 1);

                // Y LA INVOCACIÓN YA (el buff + el engendro canónico).
                int buff = ModContent.BuffType<global::AethonMod.Content.Buffs.GrimorioMenorBuff>();
                p.AddBuff(buff, 18000);
            }
            catch { }
        }

        // ================================================================
        //  EL FILTRO ENTRE LAS REALIDADES (la puerta de los drops de
        //  Esencia Corrupta — la letra: «en el momento en el que el
        //  grimorio hace su aparición en el mundo»)
        // ================================================================

        /// <summary>¿El poder del grimorio YA SE FILTRA en este mundo?
        /// (LA LLEGADA ocurrió aquí: alguien vio su bruma, alguien espera
        /// su libro, o alguien YA carga el Eterno).</summary>
        public static bool ElPoderSeFiltra()
        {
            try
            {
                var caida = ModContent.GetInstance<LaCaidaSistema>();
                if (caida != null && caida.ElPoderViveAqui()) return true;

                int eterno = ModContent.ItemType<global::AethonMod.Content.Weapons.GrimoireEternal>();
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player p = Main.player[i];
                    if (p == null || !p.active) continue;
                    for (int k = 0; k < 59; k++)
                    {
                        Item it = p.inventory[k];
                        if (it != null && !it.IsAir && it.type == eterno) return true;
                    }
                }
                return false;
            }
            catch { return false; }
        }
    }
}
