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
    //  v6.51.11 — LA COMIDA SE SIRVE EN EL SUELO (la letra del usuario:
    //  «hay un bug grave, el grimorio ahora se come toda la esencia
    //  corrupta que llega al inventario, pero no solo se la come toda,
    //  tambien la esencia se queda en el inventario por lo que sigue
    //  comiendo, lo que debe hacer es solo comer lo que necesita para
    //  subir al siguiente nivel de despertar, si necesita 1, se come 1,
    //  si necesita 24 se come 24, pero si ya comio las que necesitaba
    //  ya no debe de comer […] creo que mejor es que el jugador suelte
    //  los items delante de el con el grimorio seleccionado, pero que
    //  se vaya comiendo los item segun los necesite para sus
    //  contadores, por ejemplo necesita 70 lingotes de oro, y suelto 30
    //  lingotes de oro delante del grimorio, entonces el grimorio los
    //  toma de igual forma que se come a los jefes y se come los 30
    //  lingotes de oro, entonces el contador dice que necesita otros 40
    //  lingotes de oro, antes 70, menos los 30 que se comio, ahora
    //  necesita 40»).
    //
    //  LA REGLA NUEVA (una sola, para TODO el sistema):
    //   · LA MOCHILA ES SAGRADA — el Eterno JAMÁS vuelve a servirse del
    //     inventario (la DEVORACIÓN DE LA MOCHILA de la .51.10 MURIÓ:
    //     era el bug de la cadena silenciosa que se comía todo lo que
    //     llegaba a la bolsa).
    //   · LA MESA ES EL SUELO — con el Eterno EN LA MANO, lo que el
    //     portador SUELTA delante del libro es UN PLATO: esencias ·
    //     monedas · lingotes · el arma corrupta del peldaño.
    //   · SOLO COME LO QUE LE FALTA (contadores PARCIALES que viven en
    //     el alma del libro — persisten y viajan por el cable): si al
    //     peldaño le faltan 40 de 70 lingotes y sueltas 30, come LOS
    //     30 y ESPERA por los otros 40. Si sueltas 100, come 40 y las
    //     otras 60 se quedan en el suelo (recogerlas es gratis).
    //   · UN DESPERTAR POR PULSO — con las TRES ofrendas completas
    //     (esencias + dinero + arma O lingotes), el libro DESPIERTA
    //     (+1) y los contadores RENACEN limpios para el peldaño nuevo.
    //   · EL BOCADO DE SIEMPRE VIVE — cualquier OTRA arma corrupta
    //     soltada ante él sigue siendo el festín de la .129 (EXP por
    //     rareza + UN stat por clase + EL ALMA ÚNICA si era especial).
    //
    //  EL FESTÍN DEL PORTADOR (la otra letra de la .51.11): las
    //  esencias que EL JUGADOR se come (ItemCorrupto.UseItem) suben SU
    //  propio ataque (+0,1 % por esencia, tope +100 % con 1000 — al
    //  tope, cada esencia se vuelve 1 MONEDA DE ORO). Lo que el LIBRO
    //  come NO cuenta para ese daño: SOLO despierta (MigrarViejoPoder
    //  pasa el viejo poder devorado de los libros viejos al portador,
    //  una sola vez).
    // ======================================================================
    public class DespertarGrimorioSistema : ModSystem
    {
        /// <summary>El pulso del comensal: cada 15 t la AUTORIDAD mira
        /// la mesa (un plato no se enfría en el suelo).</summary>
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
                int tipoEsencia = ModContent.ItemType
                    <global::AethonMod.Content.Items.Altar.ItemCorrupto>();

                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player p = Main.player[i];
                    if (p == null || !p.active || p.dead) continue;

                    // v6.51.11 — EL VIEJO PODER CAMINA AL PORTADOR (una
                    // vez por libro): las esencias que este libro devoró
                    // en las versiones viejas ya no dan daño AL LIBRO
                    // (la letra: lo que el grimorio come SOLO despierta)
                    // — pasan a SU portador como esencias consumidas.
                    MigrarViejoPoder(p);

                    // LA MESA ES EL SUELO: solo con el Eterno EN LA MANO
                    // (el gesto de siempre — «con el grimorio
                    // seleccionado»).
                    Item mano = p.HeldItem;
                    if (mano == null || mano.IsAir || mano.type != eterno) continue;

                    var alma = mano.GetGlobalItem
                        <global::AethonMod.Content.Globals.AlmaEternoGlobalItem>();
                    if (alma == null) continue;

                    bool desperto = false;
                    if (alma.NivelDespertar < DespertarGrimorio.NivelMax)
                    {
                        int siguiente = System.Math.Min(
                            DespertarGrimorio.NivelDespertarSiguiente(mano),
                            DespertarGrimorio.NivelMax);
                        desperto = ServirLaMesa(p, mano, alma, siguiente, tipoEsencia);
                    }
                    if (desperto) continue;   // un despertar por pulso (con calma)

                    // === EL BOCADO DE SIEMPRE (el festín de la .129
                    //     VIVE): con el Eterno EN LA MANO, toda ARMA
                    //     CORRUPTA soltada a su lado que NO era la
                    //     ofrenda del peldaño sigue siendo EXP + stats +
                    //     almas — un plato por pulso (con calma). ===
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

                        DevorarComoBocado(p, mano, drop);
                        break;
                    }
                }
            }
            catch { }
        }

        // ================================================================
        //  v6.51.11 — LA MESA DEL SUELO: el libro come de los items
        //  CAÍDOS delante de él, SOLO lo que le falta del peldaño, y
        //  despierta cuando la mesa está completa. Devuelve true si
        //  DESPERTÓ (el pulso termina: una subida por vez).
        // ================================================================
        private static bool ServirLaMesa(Player p, Item libro,
            global::AethonMod.Content.Globals.AlmaEternoGlobalItem alma,
            int siguiente, int tipoEsencia)
        {
            try
            {
                int esenciasNecesarias = DespertarGrimorio.EsenciasPara(siguiente);
                long monedasNecesarias = DespertarGrimorio.MonedasPara(siguiente);
                int mineralNecesario = DespertarGrimorio.CantidadMineralDe(siguiente);
                int tipoMineral = DespertarGrimorio.MineralDe(siguiente);
                int[] armasDelPeldano = DespertarGrimorio.ArmasDelNivel[siguiente];
                bool mordisco = false;

                for (int d = 0; d < Main.maxItems; d++)
                {
                    Item drop = Main.item[d];
                    if (drop == null || drop.IsAir) continue;
                    if (drop.Center.DistanceSQ(p.Center) > RADIO_DEVORAR * RADIO_DEVORAR)
                        continue;

                    // === 1) LAS ESENCIAS — solo LAS QUE FALTAN (si ya
                    //     comió las suyas, la pila se queda en el suelo:
                    //     la letra: «si ya comio las que necesitaba ya
                    //     no debe de comer»). ===
                    if (drop.type == tipoEsencia)
                    {
                        int falta = esenciasNecesarias - alma.OfrendaEsencias;
                        if (falta <= 0) continue;

                        int come = System.Math.Min(falta, drop.stack);
                        alma.OfrendaEsencias += come;
                        drop.stack -= come;
                        if (drop.stack <= 0) drop.TurnToAir();
                        SincronizarDrop(d);

                        PolvosDelBocado(p, come);
                        EcoRed.AnunciarAlPortador(p, "Mods.AethonMod.Despertar.BocadoEsencias",
                            new Color(196, 138, 255), come, alma.OfrendaEsencias,
                            esenciasNecesarias);
                        mordisco = true;
                    }
                    // === 2) EL DINERO — solo LO QUE FALTA (y si la
                    //     moneda no cabe en la deuda, se la traga y
                    //     ESCUPE el cambio: el libro es glotón, no
                    //     ladrón). ===
                    else if (drop.type == ItemID.CopperCoin || drop.type == ItemID.SilverCoin ||
                        drop.type == ItemID.GoldCoin || drop.type == ItemID.PlatinumCoin)
                    {
                        long falta = monedasNecesarias - alma.OfrendaMonedas;
                        if (falta <= 0) continue;

                        long valor = ValorDeMoneda(drop.type);
                        long enteras = System.Math.Min(drop.stack, falta / valor);
                        if (enteras > 0)
                        {
                            alma.OfrendaMonedas += enteras * valor;
                            drop.stack -= (int)enteras;
                            falta -= enteras * valor;
                            mordisco = true;
                        }
                        // LA MONEDA QUE NO CABE: la última se la traga
                        // entera y devuelve el cambio en cobre.
                        if (falta > 0 && drop.stack > 0)
                        {
                            drop.stack--;
                            alma.OfrendaMonedas += falta;
                            long cambio = valor - falta;
                            if (cambio > 0) EscupirCambio(p, cambio);
                            falta = 0;
                            mordisco = true;
                        }
                        if (drop.stack <= 0) drop.TurnToAir();
                        SincronizarDrop(d);

                        if (mordisco)
                        {
                            PolvosDelBocado(p, 3);
                            EcoRed.AnunciarAlPortador(p, "Mods.AethonMod.Despertar.BocadoMonedas",
                                new Color(196, 138, 255),
                                ItemToText(System.Math.Max(0L,
                                    monedasNecesarias - alma.OfrendaMonedas)));
                        }
                    }
                    // === 3) LOS LINGOTES DEL PELDAÑO — la ofrenda
                    //     mineral (la ALTERNATIVA de los peldaños de
                    //     armas y LA ofrenda de la cima 45-50): SOLO si
                    //     el arma del peldaño no ha sido devorada ya, y
                    //     SOLO los que faltan (el ejemplo de la letra:
                    //     70 que necesita, 30 que sueltas, 40 que
                    //     faltan). ===
                    else if (tipoMineral > 0 && drop.type == tipoMineral)
                    {
                        if (alma.OfrendaArma) continue;
                        int faltaMin = mineralNecesario - alma.OfrendaMineral;
                        if (faltaMin <= 0) continue;

                        int comeMin = System.Math.Min(faltaMin, drop.stack);
                        alma.OfrendaMineral += comeMin;
                        drop.stack -= comeMin;
                        if (drop.stack <= 0) drop.TurnToAir();
                        SincronizarDrop(d);

                        PolvosDelBocado(p, comeMin);
                        EcoRed.AnunciarAlPortador(p, "Mods.AethonMod.Despertar.BocadoMineral",
                            new Color(196, 138, 255), comeMin, alma.OfrendaMineral,
                            mineralNecesario);
                        mordisco = true;
                    }
                    // === 4) LAS ARMAS — ¿es LA OFRENDA del peldaño? Se
                    //     la devora ENTERA y la ofrenda queda PAGA (con
                    //     el arma O los lingotes basta: la regla de
                    //     siempre). Si no: no es asunto de ESTA mesa
                    //     (el BOCADO DE SIEMPRE la atiende después). ===
                    else if (EsArma(drop) && !alma.OfrendaArma && EsDelPeldano(drop, armasDelPeldano))
                    {
                        var sello = drop.GetGlobalItem
                            <global::AethonMod.Content.Globals.CorruptoGlobalItem>();
                        if (sello == null || !sello.Corrompido) continue;

                        alma.OfrendaArma = true;
                        drop.TurnToAir();
                        SincronizarDrop(d);

                        PolvosDelBocado(p, 6);
                        EcoRed.AnunciarAlPortador(p, "Mods.AethonMod.Despertar.BocadoArma",
                            new Color(255, 215, 120));
                        mordisco = true;
                    }

                    // === ¿LA MESA ESTÁ COMPLETA? EL DESPERTAR (una
                    //     sola vez por pulso — con calma, que es un
                    //     libro). ===
                    if (LaMesaEstaCompleta(alma, siguiente))
                    {
                        SubirNivel(p, libro, null);   // SubirNivel RENACE los contadores
                        return true;
                    }
                }

                // ¿LA MESA YA ESTABA COMPLETA al terminar el paseo? (el
                // seguro del pulso: si el último bocado murió a mitad de
                // camino —excepción, cable— el despertar NO se pierde:
                // el próximo pulso con el libro en la mano lo cobra).
                if (LaMesaEstaCompleta(alma, siguiente))
                {
                    SubirNivel(p, libro, null);
                    return true;
                }

                // el bocado movió contadores: que caminen por el cable
                if (mordisco) EcoRed.SincronizarDevoracion(p);
                return false;
            }
            catch { return false; }
        }

        /// <summary>EL CAMBIO DEL ETERNO: el cobre que sobra del último
        /// trago, ESCUPIDO EN MONEDAS DE VERDAD (platino → oro → plata →
        /// cobre, pilas de ≤999 — QuickSpawnItem CLAMPA la pila al
        /// maxStack del item: sin el desglose, un platino de cambio con
        /// falta de 50 cobre DEVORABA 999.950 cobre del jugador).</summary>
        private static void EscupirCambio(Player p, long cobre)
        {
            if (p == null || cobre <= 0) return;
            var fuente = p.GetSource_Misc("CambioDelEterno");
            int[] tipos = { ItemID.PlatinumCoin, ItemID.GoldCoin, ItemID.SilverCoin, ItemID.CopperCoin };
            long[] valores = { 1000000L, 10000L, 100L, 1L };
            for (int k = 0; k < tipos.Length && cobre > 0; k++)
            {
                long cuantas = cobre / valores[k];
                while (cuantas > 0)
                {
                    int pila = (int)System.Math.Min(cuantas, 999);
                    p.QuickSpawnItem(fuente, tipos[k], pila);
                    cuantas -= pila;
                    cobre -= (long)pila * valores[k];
                }
            }
        }

        /// <summary>¿LAS TRES OFRENDAS del peldaño están PAGAS? (las
        /// esencias + el dinero + el arma O los lingotes).</summary>
        private static bool LaMesaEstaCompleta(
            global::AethonMod.Content.Globals.AlmaEternoGlobalItem alma, int siguiente)
        {
            if (alma == null) return false;
            if (alma.OfrendaEsencias < DespertarGrimorio.EsenciasPara(siguiente)) return false;
            if (alma.OfrendaMonedas < DespertarGrimorio.MonedasPara(siguiente)) return false;
            if (alma.OfrendaArma) return true;
            return alma.OfrendaMineral >= DespertarGrimorio.CantidadMineralDe(siguiente);
        }

        /// <summary>¿Es ESTA arma una de las del peldaño?</summary>
        private static bool EsDelPeldano(Item drop, int[] peldano)
        {
            if (peldano == null) return false;
            for (int k = 0; k < peldano.Length; k++)
                if (peldano[k] == drop.type) return true;
            return false;
        }

        /// <summary>El valor de una moneda, en cobre.</summary>
        private static long ValorDeMoneda(int tipo)
        {
            if (tipo == ItemID.SilverCoin) return 100L;
            if (tipo == ItemID.GoldCoin) return 10000L;
            if (tipo == ItemID.PlatinumCoin) return 1000000L;
            return 1L;
        }

        /// <summary>La muerte del plato en TODAS las pantallas (el sync
        /// del item caído que acaba de encogerse o morir).</summary>
        private static void SincronizarDrop(int indice)
        {
            if (Main.netMode == NetmodeID.Server && indice >= 0)
                NetMessage.SendData(MessageID.SyncItem, -1, -1, null, indice);
        }

        /// <summary>LOS POLVOS DEL TRAGO (la máquina con pantalla — el
        /// tamaño del bocado manda).</summary>
        private static void PolvosDelBocado(Player p, int bocado)
        {
            if (Main.dedServ || p == null) return;
            for (int k = 0; k < Math.Min(20, 4 + bocado); k++)
            {
                Dust d = Dust.NewDustPerfect(p.Center, DustID.Shadowflame,
                    new Vector2(Main.rand.NextFloat(-2.4f, 2.4f),
                        Main.rand.NextFloat(-3.2f, 0.4f)), 150,
                    new Color(120, 40, 180), 0.95f);
                d.noGravity = true;
            }
            Terraria.Audio.SoundEngine.PlaySound(
                SoundID.Item113.WithPitchOffset(-0.45f), p.Center);
        }

        // ================================================================
        //  v6.51.11 — EL VIEJO PODER CAMINA AL PORTADOR (la letra: «las
        //  esencias consumidas por el grimorio no cuentan para el
        //  aumento general de daño, solo cuentan para su despertar»):
        //  los libros de las versiones viejas traen DevoracionPoder (el
        //  +0,1 % de daño POR LIBRO de la .51.6-.51.10) — se pasa al
        //  portador como ESENCIAS CONSUMIDAS (tope 1000, igual que si
        //  se las hubiera comido él) y el contador del libro MUERE. La
        //  migración es UNA VEZ por libro (el contador queda en 0 y ya
        //  nunca vuelve a crecer: el libro SOLO despierta).
        // ================================================================
        private static void MigrarViejoPoder(Player p)
        {
            try
            {
                if (p == null || !p.active) return;
                var sp = p.GetModPlayer<global::AethonMod.Content.Players.ShardPlayer>();
                if (sp == null) return;

                int eterno = ModContent.ItemType<global::AethonMod.Content.Weapons.GrimoireEternal>();
                for (int k = 0; k < 58; k++)
                {
                    Item it = p.inventory[k];
                    if (it == null || it.IsAir || it.type != eterno) continue;

                    var alma = it.GetGlobalItem
                        <global::AethonMod.Content.Globals.AlmaEternoGlobalItem>();
                    if (alma == null || alma.DevoracionPoder <= 0) continue;

                    int viejo = alma.DevoracionPoder;
                    int cabe = System.Math.Max(0,
                        global::AethonMod.Content.Players.ShardPlayer.EsenciasTope -
                        sp.EsenciasConsumidas);
                    int pasa = System.Math.Min(viejo, cabe);
                    sp.EsenciasConsumidas += pasa;
                    alma.DevoracionPoder = 0;

                    EcoRed.AnunciarAlPortador(p, "Mods.AethonMod.Despertar.MigracionPoder",
                        new Color(196, 138, 255), viejo,
                        (pasa * 0.1f).ToString("0.#", System.Globalization.CultureInfo
                            .InvariantCulture));
                    sp.SyncAhoraDevoracion();
                }
            }
            catch { }
        }

        // ================================================================
        //  LA SUBIDA DE NIVEL (las tres ofrendas ya están pagas)
        // ================================================================

        /// <summary>SUBE EL DESPERTAR (+1) y celebra: la voz del libro,
        /// el aviso dorado, los polvos, el hito si el nivel desbloquea
        /// algo… y en el 20, LA PELÍCULA DE LA MASCOTA. v6.51.6 — el
        /// despertar sube EN EL GRIMORIO que devoró la ofrenda (cada
        /// libro es su propia escalera). v6.51.11 — la MESA RENACE: los
        /// contadores parciales de la ofrenda vuelven a 0 para el
        /// peldaño nuevo (la escalera sigue su hambre).</summary>
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

                // v6.51.11 — LA MESA RENACE (los contadores del peldaño
                // pagado mueren; los del nuevo nacen en 0).
                alma.OfrendaEsencias = 0;
                alma.OfrendaMonedas = 0L;
                alma.OfrendaMineral = 0;
                alma.OfrendaArma = false;

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
