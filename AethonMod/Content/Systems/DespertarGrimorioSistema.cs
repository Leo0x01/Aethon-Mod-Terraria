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
    //     (esencias + dinero + arma O materiales), el libro DESPIERTA
    //     (+1) y los contadores RENACEN limpios para el peldaño nuevo.
    //
    //  v6.90 — LA LETRA NUEVA (dos leyes):
    //   · EL ARMA **O** LOS MATERIALES — BASTA UNO DE LOS DOS (la
    //     letra: «hay que dejar claro que para el despertar se puede
    //     usar el arma, y en caso de no poder usar el arma, se usan
    //     los materiales… con el arma o los materiales basta, aun asi
    //     debes dejar claro que solo necesitan uno de los dos»): si el
    //     grimorio ya devoró EL ARMA, los materiales del suelo quedan
    //     para el jugador; si ya completó EL MATERIAL, el arma queda
    //     en el suelo. El peldaño paga con el PRIMERO que complete.
    //   · TODO LO QUE EL GRIMORIO COME DEBE ESTAR CONSAGRADO (la letra:
    //     «asegurate de que cada item que el grimorio se coma, debe
    //     estar corrompido menos las esencias mismas ya que estas son
    //     la misma esencia del eterno» — el nombre nuevo de la casa es
    //     CONSAGRADO, no corrupto): el arma del peldaño y los
    //     materiales solo se los traga si pasaron por EL ALTAR. Las
    //     esencias y las monedas quedan exentas (las esencias SON la
    //     esencia del Eterno; el dinero es dinero).
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
                int tipoLibro = ModContent.ItemType
                    <global::AethonMod.Content.Items.Altar.LibroCorrupto>();

                // v6.86 — LA RULETA DE LOS COFRES (la segunda fuente del
                // Libro Corrupto: 1% abriendo cofres en la mazmorra).
                LaRuletaDeLosCofres();

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

                    // v6.89 — LA ENTREGA DEL LIBRO CORRUPTO (la letra:
                    //     «veo que el despertar pide el libro corrupto como
                    //     debe ser, tienes que darle el libro corrupto al
                    //     jugador»): al llegar a la VÍSPERA de la puerta
                    //     del 20, el Eterno escupe su libro a la bolsa del
                    //     portador — la puerta nace con su llave puesta.
                    RevisarEntregaDelLibro(p, alma);

                    bool desperto = false;
                    if (alma.NivelDespertar < DespertarGrimorio.NivelMax)
                    {
                        int siguiente = System.Math.Min(
                            DespertarGrimorio.NivelDespertarSiguiente(mano),
                            DespertarGrimorio.NivelMax);
                        desperto = ServirLaMesa(p, mano, alma, siguiente, tipoEsencia, tipoLibro);
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
                        if (drop.type == tipoLibro) continue;        // v6.86: el libro corrupto, su propia mesa
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
        //  v6.86 — LA SEGUNDA FUENTE DEL LIBRO (la letra del peldaño 20):
        //  «o con una probabilidad de 1% abriendo cofres» — en la MAZMORRA
        //  (el contexto de la letra: los cofres de la mazmorra). Cada cofre
        //  tira UNA vez por cazador por sesión — y solo si aún no levantó
        //  su libro (el sello del UNO POR CAZADOR).
        // ================================================================

        /// <summary>LOS COFRES YA RULETADOS por cazador (índices de
        /// Main.chest — viven solo la sesión; al recargar el mundo los
        /// índices renacen y cada cofre vuelve a tirar: da igual, el
        /// sello de 1-por-cazador es el guardián de verdad).</summary>
        private static readonly Dictionary<int, HashSet<int>> _cofresRuletados = new();

        /// <summary>v6.90 — LA RULETA DE LOS COFRES (la letra nueva:
        /// «se puede obtener con un 1% de probabilidad… al abrir
        /// cualquier cofre, con 2% de abrir cofres en la mazmorra»):
        /// CUALQUIER cofre tira 1% (2% en la mazmorra) — una tirada por
        /// cofre por cazador por sesión, solo si aún no levantó su
        /// libro (el sello del UNO POR CAZADOR).</summary>
        private static void LaRuletaDeLosCofres()
        {
            try
            {
                if (Main.gameMenu || Main.netMode == NetmodeID.MultiplayerClient) return;
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player p = Main.player[i];
                    if (p == null || !p.active || p.dead) continue;
                    if (p.chest < 0) continue;   // un cofre ABIERTO (cualquiera)

                    var cazador = p.GetModPlayer<Players.CazaJefesPlayer>();
                    if (cazador == null) continue;
                    if (cazador.YaTuvoLibro) continue;   // el sello: ya no le toca

                    if (!_cofresRuletados.TryGetValue(i, out var hechos))
                    { hechos = new HashSet<int>(); _cofresRuletados[i] = hechos; }
                    if (!hechos.Add(p.chest)) continue;   // este cofre ya tiró

                    // LA TIRADA: 2% en la mazmorra · 1% en el resto del mundo.
                    int caras = p.ZoneDungeon ? 50 : 100;
                    if (!Main.rand.NextBool(caras)) continue;

                    int idx = Item.NewItem(p.GetSource_Loot(), p.Hitbox,
                        ModContent.ItemType<global::AethonMod.Content.Items.Altar.LibroCorrupto>(), 1);
                    if (idx >= 0 && Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.SyncItem, -1, -1, null, idx);
                }
            }
            catch { }
        }

        // ================================================================
        //  v6.90 — LA TERCERA FUENTE DEL LIBRO: LAS CRIATURAS (la letra:
        //  «se puede obtener con un 1% de probabilidad al matar cualquier
        //  criatura… con 2% al matar criaturas [en la mazmorra]»): TODO lo
        //  que muere tira 1% (2% si su cazador está en la mazmorra) — con
        //  el sello del UNO POR CAZADOR de siempre (si el que mató ya
        //  tuvo su libro, no cae para él).
        // ================================================================
        public class LibroDeLasCriaturasNPC : GlobalNPC
        {
            public override void OnKill(NPC npc)
            {
                try
                {
                    if (Main.gameMenu || Main.netMode == NetmodeID.MultiplayerClient) return;
                    if (npc == null || !npc.active) return;
                    // El cazador: quien le dio el ÚLTIMO golpe
                    // (npc.lastInteraction — el campo de la casa).
                    Player p = npc.lastInteraction >= 0 && npc.lastInteraction < Main.maxPlayers
                        ? Main.player[npc.lastInteraction] : null;
                    if (p == null || !p.active) return;

                    var cazador = p.GetModPlayer<Players.CazaJefesPlayer>();
                    if (cazador == null || cazador.YaTuvoLibro) return;   // el sello

                    // LA TIRADA: 2% en la mazmorra · 1% en el resto.
                    int caras = p.ZoneDungeon ? 50 : 100;
                    if (!Main.rand.NextBool(caras)) return;

                    int idx = Item.NewItem(npc.GetSource_Loot(), npc.Hitbox,
                        ModContent.ItemType<global::AethonMod.Content.Items.Altar.LibroCorrupto>(), 1);
                    if (idx >= 0 && Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.SyncItem, -1, -1, null, idx);
                }
                catch { }
            }
        }

        // ================================================================
        //  v6.51.11 — LA MESA DEL SUELO: el libro come de los items
        //  CAÍDOS delante de él, SOLO lo que le falta del peldaño, y
        //  despierta cuando la mesa está completa. Devuelve true si
        //  DESPERTÓ (el pulso termina: una subida por vez).
        // ================================================================
        private static bool ServirLaMesa(Player p, Item libro,
            global::AethonMod.Content.Globals.AlmaEternoGlobalItem alma,
            int siguiente, int tipoEsencia, int tipoLibro)
        {
            try
            {
                int esenciasNecesarias = DespertarGrimorio.EsenciasPara(siguiente);
                long monedasNecesarias = DespertarGrimorio.MonedasPara(siguiente);
                // v6.89 — LAS ARMAS DEL PELDAÑO (magia/invocador, sin
                //     látigos) y SU MATERIAL (una opción completa —
                //     GruposMaterialesDe/CantidadMateriales).
                int[] armasDelPeldano = DespertarGrimorio.ArmasDelNivel[siguiente];
                bool mordisco = false;

                // ============================================================
                //  v6.88 — EL CANJE DEL ALTAR MURIÓ (la letra: «la funcion
                //  del altar que implementaste no debe estar ligada al
                //  grimorio, asi que quita ese requisito de ofrendas en el
                //  despertar»): el crédito del 30 % y los puntos de
                //  ofrenda se extinguieron con la v6.86 — el altar hoy
                //  corrompe armas (AltarOfrendasUI) y ya no paga NINGUNA
                //  parte de esta mesa. Las esencias se comen solas.
                // ============================================================

                for (int d = 0; d < Main.maxItems; d++)
                {
                    Item drop = Main.item[d];
                    if (drop == null || drop.IsAir) continue;
                    if (drop.Center.DistanceSQ(p.Center) > RADIO_DEVORAR * RADIO_DEVORAR)
                        continue;

                    // === 0) EL LIBRO CORRUPTO (v6.86 — la puerta del 20,
                    //     la letra del peldaño): si ESTE libro está por
                    //     debajo del 20 y aún no devoró el suyo, se lo
                    //     come del suelo — y la puerta queda marcada PARA
                    //     SIEMPRE («sin importar en que nivel esté su
                    //     despertar actual»). Ya comido o ya en el 20: el
                    //     libro se queda en el suelo (no es asunto de
                    //     ESTA mesa).
                    if (drop.type == tipoLibro)
                    {
                        if (alma.NivelDespertar >= 20 || alma.LibroCorruptoConsumido)
                            continue;

                        AnimarElBocado(p, drop, 4f);
                        alma.LibroCorruptoConsumido = true;
                        drop.TurnToAir();
                        SincronizarDrop(d);

                        PolvosDelBocado(p, 6);
                        EcoRed.HablarAlPortador(p, "Mods.AethonMod.Despertar.LibroDevorado",
                            new Color(196, 138, 255));
                        mordisco = true;
                    }
                    // === 1) LAS ESENCIAS — solo LAS QUE FALTAN (si ya
                    //     comió las suyas, la pila se queda en el suelo:
                    //     la letra: «si ya comio las que necesitaba ya
                    //     no debe de comer»). v6.86 — el CRÉDITO DEL
                    //     ALTAR paga su parte (los puntos cubren hasta
                    //     el 30 % del peldaño). ===
                    else if (drop.type == tipoEsencia)
                    {
                        int falta = esenciasNecesarias - alma.OfrendaEsencias;
                        if (falta <= 0) continue;

                        int come = System.Math.Min(falta, drop.stack);
                        AnimarElBocado(p, drop, 0f);   // v6.51.12: EL FESTÍN CHICO (antes del trago: el plato aún vive)
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
                        AnimarElBocado(p, drop, 1f);   // v6.51.12: EL FESTÍN CHICO (antes del trago)
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
                    // === 3) EL MATERIAL DEL PELDAÑO (v6.89 — la letra:
                    //     «tambien seria obligatorio dar al menos uno de
                    //     los materiales o das 20 lingotes de oro o das
                    //     20 rubies») · v6.90 — DOS LEYES NUEVAS:
                    //     (a) el arma O el material BASTAN — si el arma
                    //         YA se devoró, los materiales se quedan en
                    //         el suelo (el peldaño ya tiene su pago);
                    //     (b) SOLO COME LO CONSAGRADO (la letra: «cada
                    //         item que el grimorio se coma, debe estar
                    //         corrompido menos las esencias mismas» — el
                    //         sello CONSAGRADO del altar: sin él, el
                    //         material queda en el suelo y el aviso con
                    //         calma lo explica).
                    else if (!MaterialPagado(alma, siguiente) && !alma.OfrendaArma)
                    {
                        int[][] gruposMat = DespertarGrimorio.GruposMaterialesDe(siguiente);
                        // ¿este drop pertenece a ALGÚN grupo del peldaño?
                        int grupoDelDrop = -1;
                        for (int g = 0; g < gruposMat.Length; g++)
                        {
                            var grp = gruposMat[g];
                            if (grp != null && System.Array.IndexOf(grp, drop.type) >= 0)
                            { grupoDelDrop = g; break; }
                        }
                        if (grupoDelDrop >= 0)
                        {
                            // v6.90 — LA LEY DE LA CONSAGRACIÓN: sin el
                            //     sello del altar, el grimorio NI LO PRUEBA
                            //     (el aviso con calma, una vez cada 5 s).
                            var selloMat = drop.GetGlobalItem
                                <global::AethonMod.Content.Globals.CorruptoGlobalItem>();
                            if (selloMat == null || !selloMat.Corrompido)
                            {
                                AvisoConCalma(p, "Mods.AethonMod.Despertar.NoConsagrado");
                            }
                            else
                            {
                            // EL GRIMORIO SE QUEDA CON EL PRIMERO (la ley del
                            // bloqueo — la mesa es el SUELO, el que SE
                            // ALIMENTA es EL GRIMORIO).
                            if (alma.OfrendaMineralGrupo < 0)
                            {
                                alma.OfrendaMineralGrupo = grupoDelDrop;
                                alma.OfrendaMineral = 0;
                            }
                            if (alma.OfrendaMineralGrupo == grupoDelDrop)
                            {
                                int cuota = DespertarGrimorio.CantidadMateriales(siguiente, grupoDelDrop);
                                int faltaMat = cuota - alma.OfrendaMineral;
                                if (faltaMat > 0)
                                {
                                    int comeMat = System.Math.Min(faltaMat, drop.stack);
                                    AnimarElBocado(p, drop, 2f);   // v6.51.12: EL FESTÍN CHICO
                                    alma.OfrendaMineral += comeMat;
                                    drop.stack -= comeMat;
                                    if (drop.stack <= 0) drop.TurnToAir();
                                    SincronizarDrop(d);

                                    PolvosDelBocado(p, comeMat);
                                    EcoRed.AnunciarAlPortador(p, "Mods.AethonMod.Despertar.BocadoMineral",
                                        new Color(196, 138, 255), comeMat, alma.OfrendaMineral, cuota);
                                    mordisco = true;
                                }
                            }
                            }
                        }
                    }
                    // === 4) EL ARMA DEL PELDAÑO (v6.89: magia/invocador;
                    //     «aunque pida varias armas, el grimorio solo
                    //     consumirá 1 de ellas») · v6.90 — DOS LEYES
                    //     NUEVAS: (a) si EL MATERIAL ya pagó el peldaño,
                    //     el arma se queda en el suelo (basta UNO de los
                    //     dos); (b) SOLO COME LO CONSAGRADO — el sello
                    //     del altar es la marca del Eterno (sin él, ni la
                    //     prueba: el aviso con calma).
                    else if (!alma.OfrendaArma && !MaterialPagado(alma, siguiente)
                        && EsArma(drop) && EsDelPeldano(drop, armasDelPeldano))
                    {
                        var selloArmaPeldano = drop.GetGlobalItem
                            <global::AethonMod.Content.Globals.CorruptoGlobalItem>();
                        if (selloArmaPeldano == null || !selloArmaPeldano.Corrompido)
                        {
                            AvisoConCalma(p, "Mods.AethonMod.Despertar.NoConsagrado");
                        }
                        else
                        {
                            AnimarElBocado(p, drop, 3f);   // v6.51.12: EL FESTÍN CHICO
                            alma.OfrendaArma = true;
                            drop.TurnToAir();
                            SincronizarDrop(d);

                            PolvosDelBocado(p, 6);
                            EcoRed.AnunciarAlPortador(p, "Mods.AethonMod.Despertar.BocadoArma",
                                new Color(255, 215, 120));
                            mordisco = true;
                        }
                    }

                    // === v6.67 — ¿LA MESA ESTÁ COMPLETA? EL DESPERTAR (una
                    //     sola vez por pulso — con calma, que es un libro)...
                    //     PERO LA PUERTA DE LA CAZA PRIMERO (la letra: los
                    //     jefes principales de Terraria también se pagan
                    //     para subir): si el peldaño exige un jefe y su
                    //     puerta sigue cerrada, el libro NO despierta —
                    //     exige su presa y sigue esperando (con el aviso
                    //     con calma: cada 5 s, no cada pulso). ===
                    if (LaMesaEstaCompleta(alma, siguiente))
                    {
                        if (!PuedePasarLaPuerta(p, alma, siguiente)) return true;   // la puerta manda: ni un bocado más
                        SubirNivel(p, libro, null);   // SubirNivel RENACE los contadores
                        return true;
                    }
                }

                // ¿LA MESA YA ESTABA COMPLETA al terminar el paseo? (el
                // seguro del pulso: si el último bocado murió a mitad de
                // camino —excepción, cable— el despertar NO se pierde:
                // el próximo pulso con el libro en la mano lo cobra).
                // v6.67 — Y TAMBIÉN LA PUERTA DE LA CAZA (el jefe del
                // peldaño, si lo hay, ya caído — o el aviso con calma).
                if (LaMesaEstaCompleta(alma, siguiente))
                {
                    if (!PuedePasarLaPuerta(p, alma, siguiente)) return true;
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

        /// <summary>¿LAS CUATRO OFRENDAS del peldaño están PAGAS? (las
        /// esencias + el dinero + **EL ARMA O EL MATERIAL** — v6.90, la
        /// letra: «con el arma o los materiales basta… deja claro que
        /// solo necesitan uno de los dos»). v6.67 — SOLO las ofrendas:
        /// la PUERTA DE LA CAZA (el jefe del peldaño) es un candado
        /// aparte y lo revisa PuedePasarLaPuerta.</summary>
        private static bool LaMesaEstaCompleta(
            global::AethonMod.Content.Globals.AlmaEternoGlobalItem alma, int siguiente)
        {
            if (alma == null) return false;
            if (alma.OfrendaEsencias <
                DespertarGrimorio.EsenciasPara(siguiente)) return false;
            if (alma.OfrendaMonedas < DespertarGrimorio.MonedasPara(siguiente)) return false;
            // v6.90 — EL ARMA **O** EL MATERIAL: BASTA UNO DE LOS DOS.
            return alma.OfrendaArma || MaterialPagado(alma, siguiente);
        }

        /// <summary>v6.89 — ¿EL MATERIAL del peldaño ya está pago? (el
        /// grupo bloqueado completó SU cuota — 20 de oro O 20 rubíes:
        /// el que la mesa tenga bloqueado).</summary>
        private static bool MaterialPagado(
            global::AethonMod.Content.Globals.AlmaEternoGlobalItem alma, int siguiente)
        {
            if (alma == null) return false;
            int g = alma.OfrendaMineralGrupo;
            if (g < 0) return false;
            return alma.OfrendaMineral >=
                DespertarGrimorio.CantidadMateriales(siguiente, g);
        }

        // ================================================================
        //  v6.89 — LA ENTREGA DEL LIBRO CORRUPTO (la letra: «veo que el
        //  despertar pide el libro corrupto como debe ser, tienes que
        //  darle el libro corrupto al jugador»): el 20 exige devorar el
        //  libro — y el Eterno SE LO DA a su portador en la VÍSPERA (al
        //  llegar al 19), directo a la bolsa (las fuentes de siempre —
        //  1% mazmorra, 50% la Muralla — SIGEN vivas: esta es la llave
        //  GARANTIZADA de la puerta). Una sola vez por cazador.
        // ================================================================

        /// <summary>¿Este portador ya lleva su Libro Corrupto encima?</summary>
        private static bool LlevaElLibro(Player p)
        {
            if (p == null) return false;
            int tipoLibro = ModContent.ItemType
                <global::AethonMod.Content.Items.Altar.LibroCorrupto>();
            for (int k = 0; k < 58; k++)
            {
                Item it = p.inventory[k];
                if (it != null && !it.IsAir && it.type == tipoLibro) return true;
            }
            return false;
        }

        /// <summary>LA VÍSPERA DE LA PUERTA DEL 20: si ESTE grimorio está
        /// a un paso del 19 (o ya lo cruzó) y la puerta del libro sigue
        /// por pagar, el Eterno escupe SU libro a la bolsa del portador.</summary>
        private static void RevisarEntregaDelLibro(Player p,
            global::AethonMod.Content.Globals.AlmaEternoGlobalItem alma)
        {
            try
            {
                if (p == null || !p.active) return;
                if (alma == null) return;
                if (alma.NivelDespertar != 19) return;          // la víspera exacta
                if (alma.LibroCorruptoConsumido) return;         // su puerta ya nació paga
                if (LlevaElLibro(p)) return;                     // ya lo lleva encima

                var cazador = p.GetModPlayer<Players.CazaJefesPlayer>();
                if (cazador == null || cazador.YaTuvoLibro) return;   // ya tuvo el suyo

                // LA ENTREGA: directo a la bolsa (o al suelo si va llena).
                int tipoLibro = ModContent.ItemType
                    <global::AethonMod.Content.Items.Altar.LibroCorrupto>();
                bool puesto = false;
                for (int k = 0; k < 50; k++)
                {
                    Item slot = p.inventory[k];
                    if (slot != null && !slot.IsAir) continue;
                    p.inventory[k].SetDefaults(tipoLibro);
                    puesto = true;
                    break;
                }
                if (!puesto)
                    p.QuickSpawnItem(p.GetSource_Misc("LibroDelEterno"), tipoLibro, 1);

                cazador.YaTuvoLibro = true;   // el sello: su libro ya le llegó
                if (Main.netMode == NetmodeID.Server)
                    EcoRed.HablarAlPortador(p, "Mods.AethonMod.Despertar.LibroEntregado",
                        new Color(196, 138, 255));
                else if (p == Main.LocalPlayer)
                    Main.NewText(Language.GetTextValue(
                        "Mods.AethonMod.Despertar.LibroEntregado"), new Color(196, 138, 255));
            }
            catch { }
        }

        // ================================================================
        //  v6.67 — LA PUERTA DE LA CAZA (la letra: «para ciertos niveles
        //  se tomen en cuenta los bosses principales de terraria para
        //  la subida de nivel del despertar»): los 17 jefes del camino
        //  (DespertarGrimorio.LosJefesDelDespertar) son candados de sus
        //  peldaños. EL CRÉDITO ES RETROACTIVO (la bandera del mundo de
        //  vanilla O el registro del cazador): mátalo cuando quieras —
        //  ANTES o DESPUÉS de llegar al nivel — que la puerta ya está
        //  paga cuando llegues.
        // ================================================================

        /// <summary>EL ÚLTIMO AVISO de la puerta por portador (GameUpdateCount
        /// — el aviso con CALMA: cada 5 s, no cada pulso de 15 t).</summary>
        private static readonly Dictionary<int, ulong> _ultimoAvisoPuerta = new Dictionary<int, ulong>();

        /// <summary>v6.90 — EL ÚLTIMO AVISO genérico por portador (su
        /// propio diccionario: no pelea con el de las puertas).</summary>
        private static readonly Dictionary<int, ulong> _ultimoAvisoCalma = new Dictionary<int, ulong>();

        /// <summary>v6.90 — EL AVISO CON CALMA genérico (la ley de la
        /// consagración lo usa: sin el sello del altar, el grimorio no
        /// prueba la ofrenda — y el portador se entera SIN spam: el
        /// mismo compás de 5 s de las puertas de caza).</summary>
        private static void AvisoConCalma(Player p, string clave)
        {
            try
            {
                if (p == null || !p.active) return;
                ulong ahora = Main.GameUpdateCount;
                if (!_ultimoAvisoCalma.TryGetValue(p.whoAmI, out ulong ultimo) ||
                    ahora - ultimo >= 300ul)
                {
                    _ultimoAvisoCalma[p.whoAmI] = ahora;
                    EcoRed.HablarAlPortador(p, clave, new Color(255, 170, 120));
                }
            }
            catch { }
        }

        /// <summary>¿PUEDE PASAR LA PUERTA de este peldaño? Si el nivel no
        /// exige jefe: siempre true. Si lo exige y ya cayó: true. Si lo
        /// exige y VIVE: el aviso con calma (qué presa quiere el libro) y
        /// false — el despertar espera su caza.
        /// v6.86 — LA PUERTA DEL LIBRO (el peldaño 20): además de su jefe
        /// (la Muralla), el 20 exige devorar el LIBRO CORRUPTO (la letra:
        /// «para desbloquear el nivel de despertar 20, a parte de lo que
        /// ya necesita, debemos añadir que tambien necesita consumir 1
        /// libro corrupto») — y el sello ya no se borra: comido en el 3 o
        /// en el 19, la puerta NACE paga.</summary>
        private static bool PuedePasarLaPuerta(Player p,
            global::AethonMod.Content.Globals.AlmaEternoGlobalItem alma, int siguiente)
        {
            DespertarGrimorio.JefeDelCamino? jefe = DespertarGrimorio.JefeDeNivel(siguiente);
            if (jefe != null && !DespertarGrimorio.JefeYaCaido(p, siguiente))
            {
                // EL AVISO CON CALMA (la presa que el libro exige — el hambre
                // de caza no grita cada cuarto de segundo).
                ulong ahora = Main.GameUpdateCount;
                if (!_ultimoAvisoPuerta.TryGetValue(p.whoAmI, out ulong ultimo) ||
                    ahora - ultimo >= 300ul)
                {
                    _ultimoAvisoPuerta[p.whoAmI] = ahora;
                    EcoRed.AnunciarAlPortador(p, "Mods.AethonMod.Despertar.PuertaDeCaza",
                        new Color(255, 120, 120), siguiente,
                        Terraria.Localization.Language.GetTextValue(jefe.Value.Clave));
                }
                return false;
            }

            // LA PUERTA DEL LIBRO CORRUPTO (solo el 20 la tiene).
            if (siguiente == 20 && alma != null && !alma.LibroCorruptoConsumido)
            {
                ulong ahora2 = Main.GameUpdateCount;
                if (!_ultimoAvisoPuerta.TryGetValue(p.whoAmI, out ulong ultimo2) ||
                    ahora2 - ultimo2 >= 300ul)
                {
                    _ultimoAvisoPuerta[p.whoAmI] = ahora2;
                    EcoRed.HablarAlPortador(p, "Mods.AethonMod.Despertar.PuertaLibro",
                        new Color(255, 120, 120));
                }
                return false;
            }
            return true;
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
        //  v6.51.12 — LA ANIMACIÓN DE DEVORAR (la letra: «el grimorio
        //  devora los items correctamaente, pero necesita hacer la
        //  animacion de devorar, es la misma animacion que usa con los
        //  jefes»): cada bocado de LA MESA DEL SUELO dispara EL FESTÍN
        //  CHICO — el tentáculo del FauceDevorador a escala de plato
        //  (BocadoDelSueloProjectile: sale del portador, envuelve al
        //  plato, se lo lleva por dentro y disipa). PURA PANTALLA: el
        //  consumo ya ocurrió — esto es el teatro.
        // ================================================================
        private static void AnimarElBocado(Player p, Item drop, float estilo)
        {
            try
            {
                if (Main.netMode == NetmodeID.MultiplayerClient) return;
                if (p == null || drop == null || drop.IsAir) return;
                int idx = Array.IndexOf(Main.item, drop);
                if (idx < 0) return;

                int pr = Projectile.NewProjectile(p.GetSource_Misc("MesaDelEterno"),
                    drop.Center, Vector2.Zero,
                    ModContent.ProjectileType
                        <global::AethonMod.Content.Projectiles.Sombras.BocadoDelSueloProjectile>(),
                    0, 0f, p.whoAmI, idx, estilo);
                if (pr >= 0)
                {
                    var mp = Main.projectile[pr];
                    mp.localAI[1] = drop.Center.X;
                    mp.localAI[2] = drop.Center.Y;
                    mp.localAI[0] = drop.type;   // v6.52.2 — float[3]: índice 3 NO existe (la cura del festín invisible)
                    mp.netUpdate = true;   // que el ExtraAI camine YA
                }
            }
            catch { }
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
                alma.OfrendaMineralGrupo = -1;   // v6.89 — la mesa nueva SIN material bloqueado
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

                // v6.89 — LA VÍSPERA DEL LIBRO (la letra: «tienes que
                //     darle el libro corrupto al jugador»): al SUBIR al
                //     19, la puerta del 20 ya tiene su llave en la bolsa.
                if (nivel == 19)
                    RevisarEntregaDelLibro(p, alma);

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
                AnimarElBocado(p, drop, 4f);   // v6.51.12: EL FESTÍN CHICO
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
