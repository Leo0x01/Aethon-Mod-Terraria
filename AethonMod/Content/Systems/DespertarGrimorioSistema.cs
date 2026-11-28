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

                    // v6.93 — LA MESA UNIFICADA (la letra: «cualquier arma
                    //     se puede consagrar para que el grimorio las coma
                    //     aunque no seran un requisito valido para
                    //     despertar» + «cualquier material o cantidad de
                    //     materiales se pueden consagrar para aumentar la
                    //     exp del grimorio»): EL BOCADO DE SIEMPRE murió
                    //     como bucle aparte y vive DENTRO de ServirLaMesa
                    //     — TODO lo consagrado que caiga al suelo se lo
                    //     come (arma o material), con su EXP y su stats, y
                    //     TODO cae al BANCO por su tipo. El Eterno come
                    //     TAMBIÉN en la cima: el nivel 100 no apaga el
                    //     hambre (la subida, solo mientras quede escalera).
                    int siguiente = System.Math.Min(
                        DespertarGrimorio.NivelDespertarSiguiente(mano),
                        DespertarGrimorio.NivelMax);
                    bool desperto = ServirLaMesa(p, mano, alma, siguiente, tipoEsencia, tipoLibro);
                    if (desperto) continue;   // un despertar por pulso (con calma)
                }
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("DespertarGrimorioSistema", exRegistro); }
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
                    // v6.91 — LA COMPUTA DE LOS DROPS (la letra: «solo
                    //     cuando el jugador lo usa y el grimorio tiene un
                    //     nivel de despertar de 20 o superior es que dejan
                    //     de salir»): mientras no haya usado el suyo, la
                    //     ruleta SIGUE viva para él.
                    // v6.185 — LA REGLA DEL INVENTARIO (la letra: «si ya esta
                    //     en el inventario del jugador este no sale"): las
                    //     fuentes del mundo (cofres, criaturas, jefes) miran
                    //     el INVENTARIO y el consumo — no el sello de la
                    //     víspera (que sigue siendo UNA por cazador).
                    if (!ElLibroPuedeCaerle(p, cazador)) continue;

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
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("DespertarGrimorioSistema", exRegistro); }
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
                    if (cazador == null) return;
                    // v6.185 — la misma regla del inventario para la carta
                    //     de las criaturas.
                    if (!ElLibroPuedeCaerle(p, cazador)) return;

                    // LA TIRADA: 2% en la mazmorra · 1% en el resto.
                    int caras = p.ZoneDungeon ? 50 : 100;
                    if (!Main.rand.NextBool(caras)) return;

                    int idx = Item.NewItem(npc.GetSource_Loot(), npc.Hitbox,
                        ModContent.ItemType<global::AethonMod.Content.Items.Altar.LibroCorrupto>(), 1);
                    if (idx >= 0 && Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.SyncItem, -1, -1, null, idx);
                }
                catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("DespertarGrimorioSistema", exRegistro); }
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

                    // v6.93 — LA MARCA DEL ARROJO (la letra: «el grimorio
                    //     solo comerá lo que el jugador arroje al suelo,
                    //     cualquier cosa que se caiga de un enemigo no será
                    //     consumida por el grimorio»): sin la marca, NI LA
                    //     MIRA — ni las esencias recién caídas de una
                    //     criatura, ni el botín, ni el cambio del Eterno.
                    if (!global::AethonMod.Content.Globals.ArrojadoGlobalItem.EsArrojado(drop))
                        continue;

                    // === 0) EL LIBRO CORRUPTO (v6.86 — el Despertar 20,
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
                        // v6.91 — EL CONTADOR INDIVIDUAL DEL JUGADOR (la
                        //     letra: «esto es algo individual por jugador»):
                        //     el USO se marca en SU CazaJefesPlayer — con
                        //     esto (y su grimorio al 20) los libros dejan
                        //     de salir para él.
                        var cazadorDelBocado = p.GetModPlayer<Players.CazaJefesPlayer>();
                        if (cazadorDelBocado != null) cazadorDelBocado.LibroUsado = true;
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
                    // === 2) EL DINERO — SIN SELLO, SOLO LO QUE FALTA
                    //     (v6.97 — la letra nueva: «si pide 30 lingotes
                    //     de oro, y yo arrojo al suelo 50 lingotes, el
                    //     grimorio comera sus 30 lingotes y dejara el
                    //     resto, lo mismo debe hacer con cualquier
                    //     material que pida por cantidad, como las monedas
                    //     o cualquier otro item que necesite como
                    //     requisito»): el Eterno ya NO se traga la pila
                    //     entera — come LAS MONEDAS que faltan del peldaño
                    //     VIVO y el resto se queda en el suelo. El dinero
                    //     sigue SIN consagrar (no pasa por el altar) y lo
                    //     comido cae al banco del despertar (el peldaño lo
                    //     cobra de ahí). Si el billete es más grande que
                    //     la deuda (1 de platino para una deuda de 3 de
                    //     oro), la pila queda intacta: que pague la
                    //     calderilla. ===
                    else if (drop.type == ItemID.CopperCoin || drop.type == ItemID.SilverCoin ||
                        drop.type == ItemID.GoldCoin || drop.type == ItemID.PlatinumCoin)
                    {
                        long faltaMon = DespertarGrimorio.MonedasPara(siguiente)
                            - global::AethonMod.Content.Globals.AlmaEternoGlobalItem
                                .BancoMonedasDisponibles(alma);
                        if (faltaMon > 0)
                        {
                            long deno = ValorDeMoneda(drop.type);
                            int comeMon = (int)System.Math.Min((long)drop.stack, faltaMon / deno);
                            if (comeMon > 0)
                            {
                                long cobre = deno * comeMon;
                                AnimarElBocado(p, drop, 1f);   // v6.51.12: EL FESTÍN CHICO (antes del trago)
                                global::AethonMod.Content.Globals.AlmaEternoGlobalItem
                                    .BancoGuardarMonedas(libro, cobre);
                                drop.stack -= comeMon;
                                if (drop.stack <= 0) drop.TurnToAir();
                                SincronizarDrop(d);

                                PolvosDelBocado(p, 3);
                                EcoRed.AnunciarAlPortador(p, "Mods.AethonMod.Despertar.BocadoMonedas",
                                    new Color(196, 138, 255),
                                    ItemToText(cobre));
                                mordisco = true;
                            }
                        }
                    }
                    // === 2b) LAS ESTRELLAS FUGACES CONSAGRADAS (v6.185 — la letra
                    //     del ataque básico: «los requisitos de ese nivel de
                    //     despertar deben incluir cierta cantidad de Estrellas
                    //     Fugaces… el grimorio solo comera las estrellas
                    //     consagradas»): SOLO si el peldaño vivo las exige y
                    //     SOLO LAS QUE FALTAN (la regla de los 30 de 50). Si
                    //     el peldaño no las pide, la estrella queda en el suelo
                    //     (no es «fuera de la lista»: es de OTRO peldaño).
                    else if (drop.type == global::AethonMod.Content.Systems
                        .DespertarGrimorio.TipoEstrellaFugaz())
                    {
                        int faltaEstrellas = DespertarGrimorio.EstrellasPara(siguiente) - alma.OfrendaEstrellas;
                        if (faltaEstrellas <= 0) continue;
                        int comeEst = System.Math.Min(faltaEstrellas, drop.stack);
                        AnimarElBocado(p, drop, 1f);   // el festín chico — el oro (1) de las monedas: la estrella es de oro
                        alma.OfrendaEstrellas += comeEst;
                        drop.stack -= comeEst;
                        if (drop.stack <= 0) drop.TurnToAir();
                        SincronizarDrop(d);
                        PolvosDelBocado(p, comeEst);
                        EcoRed.AnunciarAlPortador(p, "Mods.AethonMod.Despertar.BocadoEstrellas",
                            new Color(255, 208, 96), comeEst, alma.OfrendaEstrellas,
                            DespertarGrimorio.EstrellasPara(siguiente));
                        mordisco = true;
                    }
                    // === 3) SOLO LO LISTADO (v6.97 — la letra nueva: «asegurate
                    //     de que el grimorio coma solo lo que este listado en
                    //     ese momento como requisito para su siguiente nivel
                    //     de despertar, a excepción del libro consagrado»):
                    //     EL FESTÍN DE SIEMPRE MURIÓ — ya NO se come
                    //     cualquier cosa consagrada. Solo prueba el plato si
                    //     está EN EL CHECKLIST del peldaño vivo: UN ARMA de
                    //     las del peldaño (1 unidad, con su EXP y su stat —
                    //     el festín de la .129 le sale gratis al plato
                    //     legítimo) o UN MATERIAL de un GRUPO del peldaño
                    //     (SOLO LAS UNIDADES QUE FALTAN de su cuota: si
                    //     pide 30 lingotes y caen 50, come 30 y el resto se
                    //     queda en el suelo). Lo consagrado que NO está en
                    //     la lista NI se prueba (el aviso con calma). ===
                    else
                    {
                        var selloFestin = drop.GetGlobalItem
                            <global::AethonMod.Content.Globals.CorruptoGlobalItem>();
                        if (selloFestin == null || !selloFestin.Corrompido)
                        {
                            AvisoConCalma(p, "Mods.AethonMod.Despertar.NoConsagrado");
                        }
                        else
                        {
                            int nivelMesa = System.Math.Max(1, System.Math.Min(
                                DespertarGrimorio.NivelMax, siguiente));

                            // ¿ES UN ARMA DEL PELDAÑO? (la opción de su tipo).
                            int[] armasMesa = DespertarGrimorio.ArmasDe(nivelMesa);
                            bool esArmaPedida = false;
                            if (armasMesa != null)
                            {
                                for (int am = 0; am < armasMesa.Length; am++)
                                    if (armasMesa[am] == drop.type) { esArmaPedida = true; break; }
                            }

                            if (esArmaPedida)
                            {
                                // ¿Ya está paga su opción? (1 unidad en el
                                // banco basta — la pila extra queda al
                                // jugador).
                                bool yaPagada = global::AethonMod.Content.Globals
                                    .AlmaEternoGlobalItem.BancoItemDisponible(alma, drop.type) >= 1;
                                if (!yaPagada && EsArma(drop))
                                {
                                    DevorarComoBocado(p, libro, drop);
                                    mordisco = true;
                                }
                            }
                            else
                            {
                                // ¿ES UN MATERIAL DE UN GRUPO DEL PELDAÑO?
                                int[][] gruposMesa = DespertarGrimorio.GruposMaterialesDe(nivelMesa);
                                int grupoIdx = -1;
                                if (gruposMesa != null)
                                {
                                    for (int gm = 0; gm < gruposMesa.Length && grupoIdx < 0; gm++)
                                    {
                                        int[] grupo = gruposMesa[gm];
                                        if (grupo == null) continue;
                                        for (int gi = 0; gi < grupo.Length; gi++)
                                            if (grupo[gi] == drop.type) { grupoIdx = gm; break; }
                                    }
                                }

                                if (grupoIdx >= 0)
                                {
                                    int cuota = DespertarGrimorio.CantidadMateriales(nivelMesa, grupoIdx);
                                    long dispGrupo = global::AethonMod.Content.Globals
                                        .AlmaEternoGlobalItem.BancoGrupoDisponible(
                                            alma, gruposMesa[grupoIdx]);
                                    int faltaMat = cuota - (int)System.Math.Min(int.MaxValue, dispGrupo);
                                    if (faltaMat > 0)
                                    {
                                        // SOLO LAS UNIDADES QUE FALTAN (la
                                        // letra de los 30 de 50).
                                        DevorarMaterialParcial(p, libro, drop,
                                            System.Math.Min(faltaMat, drop.stack));
                                        mordisco = true;
                                    }
                                    // ya paga: la pila se queda en el suelo.
                                }
                                else
                                {
                                    // Consagrado pero FUERA DE LA LISTA: el
                                    // Eterno ni lo prueba (la letra nueva).
                                    AvisoConCalma(p, "Mods.AethonMod.Despertar.FueraDeLaLista");
                                }
                            }
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
                    //     v6.93 — LA GUARDA NUEVA: la mesa unificada corre
                    //     SIEMPRE (el festín no se apaga en la cima) — la
                    //     SUBIDA solo mientras quede escalera. ===
                    if (alma.NivelDespertar < DespertarGrimorio.NivelMax &&
                        LaMesaEstaCompleta(alma, siguiente))
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
                // v6.93 — con la GUARDA de la cima (la mesa corre siempre;
                // la subida, mientras haya escalera).
                if (alma.NivelDespertar < DespertarGrimorio.NivelMax &&
                    LaMesaEstaCompleta(alma, siguiente))
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

        // ================================================================
        //  v6.93 — EL ESCUPIDERO DE CAMBIO murió con la mesa vieja (las
        //  monedas ya no se comen «solo lo que falta»: la pila entera al
        //  BANCO). Enterrado aquí para la historia; nadie lo llama.
        // ================================================================
        /// <summary>¿LAS OFRENDAS del peldaño están PAGAS? (v6.93 — LA
        /// LISTA NUEVA de la letra: «Esencia del Eterno» como siempre +
        /// EL DINERO DEL BANCO (retroactivo) + AL MENOS 2 DE LAS OPCIONES
        /// de Devorar — «el grimorio debe consumir al menos 2 de esas
        /// opciones para desbloquear el siguiente nivel de Despertar, las
        /// 2 opciones pueden ser las 2 armas, los 2 materiales, 1 arma y
        /// un material, en fin lo importante es que devore 2»).
        /// v6.67 — SOLO las ofrendas: la PUERTA DE LA CAZA (el jefe del
        /// peldaño) es un candado aparte y lo revisa PuedePasarLaPuerta.</summary>
        private static bool LaMesaEstaCompleta(
            global::AethonMod.Content.Globals.AlmaEternoGlobalItem alma, int siguiente)
        {
            if (alma == null) return false;
            if (alma.OfrendaEsencias <
                DespertarGrimorio.EsenciasPara(siguiente)) return false;
            // v6.93 — EL DINERO DEL BANCO (la letra del retroactivo: lo
            //     devorado en OTRO peldaño paga ESTE si pide dinero).
            if (global::AethonMod.Content.Globals.AlmaEternoGlobalItem
                .BancoMonedasDisponibles(alma) <
                DespertarGrimorio.MonedasPara(siguiente)) return false;
            // v6.185 — LAS ESTRELLAS FUGACES del peldaño que viste a la
            //     estrella: sin ellas la mesa no está completa.
            if (alma.OfrendaEstrellas < DespertarGrimorio.EstrellasPara(siguiente)) return false;
            // v6.144 — EL DEVORAR VIVE ARRIBA DEL CIEN (la letra:
            // «despues del nivel 100 dice que devore armas o materiales
            // para despertar, pero no hay nada para devorar, asi que pon
            // materiales o armas para devorar como requisito y ademas
            // agrega jefes tambien»): esencias + monedas COMO SIEMPRE —
            // y ADEMÁS las 2 opciones de Devorar de la Forma (armas de la
            // casa + materiales del finalgame).
            return ContarOpcionesPagadas(alma, siguiente) >= 2;
        }

        /// <summary>v6.93 — ¿CUÁNTAS opciones de Devorar ya están pagas?
        /// Las ARMAS del peldaño valen 1 unidad cada una; cada GRUPO de
        /// materiales vale su cuota (la suma de sus alternativas cuenta
        /// junta — devorar 10 de oro y 10 de platino es 20 para «20 de
        /// oro o de platino»). La leen la mesa, SubirNivel (para cobrar)
        /// y la LISTA del tooltip (para pintar los colores).</summary>
        public static int ContarOpcionesPagadas(
            global::AethonMod.Content.Globals.AlmaEternoGlobalItem alma, int nivel)
        {
            if (alma == null) return 0;
            int pagadas = 0;
            int[] armas = DespertarGrimorio.ArmasDe(nivel);
            if (armas != null)
            {
                for (int i = 0; i < armas.Length; i++)
                    if (global::AethonMod.Content.Globals.AlmaEternoGlobalItem
                        .BancoItemDisponible(alma, armas[i]) >= 1) pagadas++;
            }
            int[][] grupos = DespertarGrimorio.GruposMaterialesDe(nivel);
            for (int g = 0; g < grupos.Length; g++)
            {
                int cuota = DespertarGrimorio.CantidadMateriales(nivel, g);
                if (global::AethonMod.Content.Globals.AlmaEternoGlobalItem
                    .BancoGrupoDisponible(alma, grupos[g]) >= cuota) pagadas++;
            }
            return pagadas;
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

        // ================================================================
        //  v6.91 — LA COMPUTA DE LOS DROPS DEL LIBRO (la letra: «solo se
        //  puede tener 1 por jugador, PERO si el jugador no lo ha usado
        //  todavia le pueden aparecer mas, solo cuando el jugador lo usa
        //  y el grimorio tiene un nivel de despertar de 20 o superior es
        //  que dejan de salir» — el contador ES DEL JUGADOR:
        //  CazaJefesPlayer.LibroUsado, individual en multijugador).
        // ================================================================

        /// <summary>¿LOS LIBROS ya dejaron de salir PARA ESTE jugador?
        /// v6.97 — LA LETRA NUEVA: «el libro consagrado solo debe caer 1
        /// vez cuando el despertar esta por debajo de nivel 20 o cuando
        /// el jugador todavia no a consumido 1, una vez que el jugador
        /// consume el libro consagrado este deja de aparecer en el mundo
        /// para siempre y solo aparece para el jugador que no lo ha
        /// consumido en multijugador y el resto que lo consumio no lo
        /// ven» — UNA CAÍDA POR CAZADOR: ya no sale nada para quien ya
        /// RECIBIÓ el suyo (lo haya usado o lo tenga guardado) ni para
        /// quien ya lo consumió. El viejo par USO+nivel-20 de la v6.91
        /// murió.</summary>
        private static bool ElLibroYaNoLeToca(Player p, Players.CazaJefesPlayer cazador)
        {
            try
            {
                if (p == null || !p.active) return true;
                if (cazador == null) return true;
                // UNA vez: ya recibió el suyo → no le cae otro jamás.
                if (cazador.LibroRecibido) return true;
                // Consumido → desaparece del mundo para siempre.
                if (cazador.LibroUsado) return true;
                return false;   // sin recibir: el suyo PUEDE caer (la única vez).
            }
            catch { return true; }
        }

        /// <summary>v6.185 — ¿El libro PUEDE caerle a este cazador desde las
        /// FUENTES DEL MUNDO (jefes al 50 %, criaturas, cofres, libros de
        /// la mazmorra)? LA LETRA NUEVA: «si ya esta en el inventario del
        /// jugador este no sale» — la regla es el INVENTARIO: quien lleva
        /// el libro encima no recibe otro; quien ya lo CONSUMIÓ tampoco
        /// (su puerta del 20 nació paga: el libro ya no le sirve). Quien lo
        /// recibió y lo perdió (o lo dejó caer para el grimorio) VUELVE a
        /// tener derecho: los jefes son la fuente principal ahora. El
        /// sello de la víspera (LibroRecibido, una entrega garantizada por
        /// cazador) NO se mira aquí — vive en ElLibroYaNoLeToca.</summary>
        public static bool ElLibroPuedeCaerle(Player p, Players.CazaJefesPlayer cazador)
        {
            try
            {
                if (p == null || !p.active) return false;
                if (cazador == null) return false;
                if (LlevaElLibro(p)) return false;       // ya lo lleva encima
                if (cazador.LibroUsado) return false;    // ya consumió el suyo
                return true;
            }
            catch { return false; }
        }

        /// <summary>El ALMA (el despertar) del grimorio que ESTE jugador
        /// porta — el nivel de la computa nueva.</summary>
        private static global::AethonMod.Content.Globals.AlmaEternoGlobalItem ElAlmaDelPortador(Player p)
        {
            try
            {
                if (p == null) return null;
                int tipoGrimorio = ModContent.ItemType<Weapons.GrimoireEternal>();
                for (int k = 0; k < 58; k++)
                {
                    Item it = p.inventory[k];
                    if (it == null || it.IsAir || it.type != tipoGrimorio) continue;
                    var alma = it.GetGlobalItem
                        <global::AethonMod.Content.Globals.AlmaEternoGlobalItem>();
                    if (alma != null) return alma;
                }
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("DespertarGrimorioSistema", exRegistro); }
            return null;
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
                // v6.183 — LA VÍSPERA QUE SE ESPERA (la letra: «es un
                // item importante para llegar a nivel de despertar 20»):
                // era un DISPARO EXACTO al 19 — si el libro caía al
                // suelo y se perdía (o el alijo se llenó y pasó algo),
                // la puerta del 20 se quedaba con la llave perdida
                // hasta un 50 % de la Muralla. Ahora la víspera vive
                // MIENTRAS la llave falte: al 19 o a CUALQUIERA que
                // venga después — el Eterno no se queda con la llave.
                if (alma.NivelDespertar < 19) return;               // la víspera (y lo que siga)
                if (alma.LibroCorruptoConsumido) return;         // su puerta ya nació paga
                if (LlevaElLibro(p)) return;                     // ya lo lleva encima

                var cazador = p.GetModPlayer<Players.CazaJefesPlayer>();
                // v6.97 — la computa nueva (UNA caída por cazador): si
                //     aún no recibió el suyo, la víspera SIGUE
                //     garantizando la llave — pero ya no se entrega dos
                //     veces.
                if (ElLibroYaNoLeToca(p, cazador)) return;

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

                // v6.97 — LA ÚNICA CAÍDA: el libro entregado por la
                //     víspera ES su caída — el sello del cazador se pone
                //     YA (las fuentes del mundo ya no le sueltan otro y
                //     el resto de la letra vive en el OnPickup).
                if (cazador != null) cazador.LibroRecibido = true;

                // v6.91 — EL SELLO VIEJO (YaTuvoLibro) SE RETIRA: la
                //     llave garantizada de la víspera puede repetirse si
                //     el jugador aún no usó su libro (la letra nueva) —
                //     la recogida limita a 1 EN EL INVENTARIO.
                if (Main.netMode == NetmodeID.Server)
                    EcoRed.HablarAlPortador(p, "Mods.AethonMod.Despertar.LibroEntregado",
                        new Color(196, 138, 255));
                else if (p == Main.LocalPlayer)
                    Main.NewText(Language.GetTextValue(
                        "Mods.AethonMod.Despertar.LibroEntregado"), new Color(196, 138, 255));
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("DespertarGrimorioSistema", exRegistro); }
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
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("DespertarGrimorioSistema", exRegistro); }
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

        // ================================================================
        //  v6.93 — EsDelPeldano murió con la mesa vieja (el «arma del
        // peldaño» ya no es un caso aparte: TODO va al festín y al
        // banco). Enterrado aquí para la historia; nadie lo llama.
        // ================================================================
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
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("DespertarGrimorioSistema", exRegistro); }
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
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("DespertarGrimorioSistema", exRegistro); }
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
                // pagado mueren; los del nuevo nacen en 0). v6.93 — la
                // mesa vieja del MATERIAL/ARMA/MONEDAS-por-peldaño murió
                // (el banco la entierra): los contadores se limpian por
                // compatibilidad de los saves, ya nadie los lee.
                alma.OfrendaEsencias = 0;
                alma.OfrendaEstrellas = 0;   // v6.185 — las estrellas del peldaño pagado renacen
                alma.OfrendaMonedas = 0L;
                alma.OfrendaMineral = 0;
                alma.OfrendaMineralGrupo = -1;   // v6.89 — la mesa nueva SIN material bloqueado
                alma.OfrendaArma = false;

                // ============================================================
                //  v6.93 — EL COBRO DEL BANCO (la letra del RETROACTIVO:
                //  «como ya se habian consumido 50, entoces la cheklist se
                //  queda con 150 lingotes de oro completo, faltanto 50
                //  lingotes mas para completarla»): el peldaño pagado
                //  descuenta del banco lo que usó — el DINERO entero y
                //  las 2 PRIMERAS opciones de Devorar que estén pagas
                //  (armas primero, en el orden de la lista; el material
                //  del grupo se cobra de sus tipos, en orden). Lo cobrado
                //  muere aquí: el disponible baja para los peldaños que
                //  vengan.
                // ============================================================
                try
                {
                    global::AethonMod.Content.Globals.AlmaEternoGlobalItem.BancoCobrarMonedas(
                        grimorio, DespertarGrimorio.MonedasPara(nivel));
                    // v6.144 — el Devorar de la Forma TAMBIÉN se cobra
                    // (las 2 primeras pagas, la regla de siempre).
                    int porCobrar = 2;
                    int[] armasCobro = DespertarGrimorio.ArmasDe(nivel);
                    if (armasCobro != null)
                    {
                        for (int i = 0; i < armasCobro.Length && porCobrar > 0; i++)
                        {
                            if (global::AethonMod.Content.Globals.AlmaEternoGlobalItem
                                .BancoItemDisponible(grimorio, armasCobro[i]) >= 1)
                            {
                                global::AethonMod.Content.Globals.AlmaEternoGlobalItem
                                    .BancoCobrarTipo(grimorio, armasCobro[i], 1);
                                porCobrar--;
                            }
                        }
                    }
                    int[][] gruposCobro = DespertarGrimorio.GruposMaterialesDe(nivel);
                    for (int g = 0; g < gruposCobro.Length && porCobrar > 0; g++)
                    {
                        int cuotaG = DespertarGrimorio.CantidadMateriales(nivel, g);
                        if (global::AethonMod.Content.Globals.AlmaEternoGlobalItem
                            .BancoGrupoDisponible(grimorio, gruposCobro[g]) >= cuotaG)
                        {
                            global::AethonMod.Content.Globals.AlmaEternoGlobalItem
                                .BancoCobrarGrupo(grimorio, gruposCobro[g], cuotaG);
                            porCobrar--;
                        }
                    }
                }
                catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("DespertarGrimorioSistema", exRegistro); }

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
                // v6.124 — DENTRO DEL MUNDO INTERIOR solo habla EL SET
                // EXCLUSIVO DEL INTERIOR (la letra: «el grimorio tendra un
                // nuevo set de dialogos solo dentro del mundo interior,
                // esto hara que los dialogos anteriores no se activen y
                // solo se activen los nuevos dialogos exclusivos del
                // mundo interior»): la voz del despertar NO se activa
                // dentro — habla el interior.
                if (SubmundoGrimorio.VozInteriorActiva)
                    SubmundoGrimorio.HablarInterior(p);
                else
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

                // ============================================================
                //  v6.142 — LA CONSTRUCCIÓN DE LA FORMA ETERNA (la letra:
                //  «cuando el despertar llegue a nivel 100, entonces
                //  comienza a desbloquear la forma del eterno… seria
                //  hasta nivel 112… una parte por cada nivel»): cada
                //  peldaño 101-112 construye UNA PARTE (puesta en el
                //  portador mientras usa el libro). Al 112 (la 12ª):
                //  el ítem cosmético NACE, LA MANIFESTACIÓN estalla y
                //  LOS VISUALES DESAPARECEN (la forma se CONVIERTE en
                //  el ítem — la letra: «en el momento de la
                //  manifestacion la forma eterna que es parte del
                //  grimorio desaparece ya que es convertida en el item
                //  cosmetico»).
                // ============================================================
                // v6.144 — LA ESCALERA NUEVA DE LAS PARTES (la letra:
                // «que el hito del nivel de despertar 120 sean las Alas
                // Runicas y el Aura del Vacio… donde todos los requisitos
                // se unen creando asi La Forma Eterna»): las partes 1-11
                // se construyen en los peldaños 101-111 (una por nivel) y
                // LA PARTES 12ª — LAS ALAS RÚNICAS + EL AURA — SOLO al
                // llegar al 120, con LA MANIFESTACIÓN (la explosión de
                // luz MÁS GRANDE — la del 112 queda ANULADA).
                if (nivel >= DespertarGrimorio.NivelFormaInicio &&
                    nivel <= DespertarGrimorio.NivelManifestacion && p != null && p.active)
                {
                    try
                    {
                        var fe = p.GetModPlayer
                            <global::AethonMod.Content.Players.FormaEternaPlayer>();
                        // ============================================================
                        //  v6.151 — LA ESCALERA EXACTA DE LA LETRA (la
                        //  autopsia: «explota en nivel 112 de despertar, el
                        //  113 y 119 no hacen nada» — el peldaño 112 llamaba
                        //  ConstruirParte(112-100)=12: construía LA 12ª EN
                        //  EL 112, disparaba la conversión+manifestación y
                        //  FormaEscupida apagaba TODO el vestido — por eso
                        //  113-119 «no hacían nada»). LA CURA:
                        //   · 101-111 → partes 1-11 (una por nivel, como
                        //     siempre)
                        //   · 112-119 → NADA se construye: son los peldaños
                        //     de LOS ANILLOS DE SOLES RÚNICOS (el vestido
                        //     los enciende uno a uno — AnillosSolRunico en
                        //     FormaEternaPlayer, ya vivo)
                        //   · 120 → LA 12ª PARTE + LA CONVERSIÓN (el ítem
                        //     nace de la explosión de luz — SOLO al 120)
                        // ============================================================
                        if (nivel < DespertarGrimorio.NivelManifestacion)
                        {
                            if (nivel <= 111)
                                fe?.ConstruirParte(nivel - DespertarGrimorio.NivelCaza);
                            // 112-119: los ANILLOS viven en el vestido (el
                            // aviso de cada anillo lo da el propio vestido)
                        }
                        else
                        {
                            // EL 120: la 12ª parte + LA CONVERSIÓN (el ítem
                            // nace de la explosión — nada se construye dos veces)
                            fe?.ConstruirParte(12);
                        }
                    }
                    catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("DespertarGrimorioSistema", exRegistro); }
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
                //     19, el Despertar 20 ya tiene su llave en la bolsa.
                if (nivel >= 19)   // v6.183: la víspera se repite mientras la llave falte
                    RevisarEntregaDelLibro(p, alma);

                // EL NUEVO NIVEL CAMINA (el daño del portador lo lee su
                // máquina — el paquete del festín lleva el despertar).
                EcoRed.SincronizarDevoracion(p);
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("DespertarGrimorioSistema", exRegistro); }
        }

        /// <summary>v6.141 — LA MANIFESTACIÓN DE LA FORMA ETERNA (la
        /// letra: «la manifestacion sera una explocion de luz sobre el
        /// jugador que eliminara a cualquier enemigo en la pantalla de
        /// un solo golpe, jefes incluidos y con su loot ya que es una
        /// muerte valida»): el estallido de luz del cuerpo completo.
        /// TODO lo que viva en la PANTALLA muere con StrikeNPC (la
        /// muerte VÁLIDA: loot, lápidas, bestiario, contadores de la
        /// casa) — jefes incluidos (el festín nunca interrumpe: se
        /// respeta DevorarEstado). Es el NACIMIENTO del dios.</summary>
        public static void ManifestacionDeLaForma(Player p)
        {
            try
            {
                if (Main.netMode == NetmodeID.MultiplayerClient) return;

                // EL TEATRO DEL ESTALLIDO (la máquina con pantalla)
                if (!Main.dedServ)
                {
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item122.WithPitchOffset(-0.2f), p.Center);
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item89.WithPitchOffset(0.3f), p.Center);
                }

                // EL BARRIDO DE LA PANTALLA: todo lo VIVO en el
                // rectángulo de la cámara muere con UN GOLPE VÁLIDO.
                int x0 = (int)(Main.screenPosition.X / 16f) - 4;
                int y0 = (int)(Main.screenPosition.Y / 16f) - 4;
                int x1 = (int)((Main.screenPosition.X + Main.screenWidth) / 16f) + 4;
                int y1 = (int)((Main.screenPosition.Y + Main.screenHeight) / 16f) + 4;
                Rectangle zona = new Rectangle(
                    x0, y0, x1 - x0, y1 - y0);

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n == null || !n.active) continue;
                    if (!n.Center.Between(zona.TopLeft() * 16f, zonasBottomRight(zona))) continue;
                    // LO SAGRADO NO SE TOCA: el festín en curso sigue su
                    // cine (DevorarEstado), los pueblos y los tuyos viven.
                    if (n.townNPC || n.friendly) continue;
                    if (n.TryGetGlobalNPC<Globals.FaucesGlobalNPC>(out var fauces) &&
                        fauces.DevorarEstado != 0) continue;

                    // LA MUERTE VÁLIDA (la API de esta era del tML,
                    // verificada en el decompile): CalculateHitInfo →
                    // StrikeNPC(HitInfo) dispara el golpe mortal
                    // completo — loot, lápidas, bestiario, los
                    // contadores de la casa (Fauces, CazaJefes…).
                    var golpe = n.CalculateHitInfo(int.MaxValue, 0);
                    golpe.InstantKill = true;
                    n.StrikeNPC(golpe);
                    if (Main.netMode == NetmodeID.Server)
                        Terraria.NetMessage.SendStrikeNPC(n, in golpe);
                    n.netUpdate = true;
                }

                // ============================================================
                //  v6.151 — LA EXPLOSIÓN DE LUZ PURA (la letra: «la
                //  explosion es horrible… la explocion esta llena de
                //  rayas, y lo que debe ser es una explosion de luz, sin
                //  texturas ya que es puro codigo, analiza como es que la
                //  mascota de luz hace su explosion, esa es la explosion
                //  que quiero para cuando termina la construccion de la
                //  forma eterna»). LA AUTOPSIA: el velo viejo eran 220
                //  POLVOS + el AtaqueJefeProjectile (sus brumas y
                //  destellos con TEXTURAS — las «rayas»). LA CURA: EL
                //  ESTALLIDO DE LA MASCOTA DE LUZ (MiniEstallidoPet) EN
                //  TAMAÑO DIOS — LA MISMA RECETA 100 % CÓDIGO: los 22
                //  RAYOS deterministas (línea núcleo blanca + halo
                //  oro→ámbar→brasa), LA CRUZ ANAMÓRFICA, el NÚCLEO que
                //  inunda (Bloom), LA ESTRELLA de 8 rayos (DestelloFinal
                //  — el flare de la casa), LA ONDA expansiva y LAS BOKEH
                //  — TODO por Quads y librerías de la casa (VFXCore/
                //  LumenLib/OndaLib), CERO brumas con texturas, cero
                //  polvos, cero rayas. La coronación violeta vive en la
                //  ONDA doble y las bokeh alternadas.
                // ============================================================
                if (!Main.dedServ)
                {
                    // LA ONDA DE CHOQUE (el triple latido de la casa —
                    // el mismo Kick de la muerte del dios, TRIPLEDADO)
                    global::AethonMod.Content.VFX.OndaLib.Kick(20f, 40);
                    global::AethonMod.Content.VFX.OndaLib.Kick(12f, 26);
                    // EL ESTALLIDO DE LA MASCOTA DE LUZ (receta propia,
                    // escala dios — el mismo proyectil de la mascota con
                    // SU semilla y SU dueño: nace, dura y muere SOLO)
                    var estallido = Projectile.NewProjectileDirect(
                        p.GetSource_FromThis(), p.Center, Vector2.Zero,
                        ModContent.ProjectileType
                            <global::AethonMod.Content.Projectiles.Cosmetic.MiniEstallidoPet>(),
                        0, 0f, p.whoAmI);
                    // LA ESCALA DIOS DE VERDAD v6.155 (la letra: «la
                    // explocion de luz final debe ser al menos 500% mas
                    // grande y debe siempre perseguir al jugador»): el
                    // estallido de la manifestación nace en MODO DIOS —
                    // ai[1] = -2: TODO el dibujo crece ×6 (el 500%
                    // pedido) y PERSIGUE AL PORTADOR (el destello le
                    // pega el centro al jugador VIVO mientras dure: el
                    // nacimiento del dios LO SIGUE, no se queda en el
                    // punto donde nació).
                    estallido.ai[0] = Main.rand.Next(10000);
                    estallido.ai[1] = -2;
                    estallido.netUpdate = true;

                    Main.NewText(Language.GetTextValue(
                        "Mods.AethonMod.Despertar.ManifestacionTexto"),
                        new Color(255, 236, 190));
                }
                if (Main.netMode == NetmodeID.Server)
                    EcoRed.AnunciarAlPortador(p,
                        "Mods.AethonMod.Despertar.ManifestacionTexto",
                        new Color(255, 236, 190));
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("DespertarGrimorioSistema", exRegistro); }
        }

        /// <summary>La esquina inferior-derecha de la zona en píxeles (el
        /// between de la manifestación pide esquinas del MISMO espacio).</summary>
        private static Vector2 zonasBottomRight(Rectangle zona)
            => new Vector2((zona.X + zona.Width) * 16f, (zona.Y + zona.Height) * 16f);

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

                // v6.93 — EL EXP NUEVO (la letra: «dependiendo de la rareza,
                //     dificultad de obtener y cantidad de materiales con
                //     las que se tiene que hacer, asi como la calidad de los
                //     materiales usados, puede aumentar el nivel del
                //     grimorio y dar experiencia extra al grimorio, la
                //     cantidad minima de nivel que puede subir un arma es
                //     de 1 y la cantidad de exp minima que pueda dar un arma
                //     es de 500 siendo estas las armas mas basicas del
                //     juego»): MÍNIMO 500 (la escala de la .129 nacía en 5
                //     — la basura alimentaba a nada) y el arma devorada
                //     sube el NIVEL DEL LIBRO (GrantXP) con un mínimo de
                //     UN NIVEL por arma (si la exp no alcanza el umbral,
                //     SubirNivelDirecto lo fuerza).
                long exp = System.Math.Max(500L, ExpPorRareza(drop.rare));
                alma.DevoracionExp += exp;

                // EL STAT por clase (el festín de la .129 — intacto).
                string stat = SubirStatPorClase(alma, drop);

                // EL ALMA ÚNICA (si era especial) — intacto.
                string almaUnica = DespertarAlma(alma, drop);

                // v6.93 — LA SUBIDA DEL LIBRO (la letra: «puede aumentar el
                //     nivel del grimorio»): la exp va AL LIBRO (ShardLevelItem)
                //     — y si no alcanzó para subir, el mínimo de UN NIVEL.
                var slBocado = grimorio.GetGlobalItem
                    <global::AethonMod.Content.Globals.ShardLevelItem>();
                if (slBocado != null)
                {
                    int nivelAntes = slBocado.Level;
                    slBocado.GrantXP(grimorio, (int)System.Math.Min(int.MaxValue, exp));
                    if (slBocado.Level <= nivelAntes)
                        slBocado.SubirNivelDirecto(grimorio, 1);
                }

                // v6.93 — AL BANCO por su tipo (el pago RETROACTIVO del
                //     peldaño que pida ESTA arma, hoy o mañana: 1 unidad).
                global::AethonMod.Content.Globals.AlmaEternoGlobalItem
                    .BancoGuardarItem(grimorio, drop.type, 1);

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
                    new Color(196, 138, 255), nombre, exp,
                    Language.GetTextValue($"Mods.AethonMod.Caida.{stat}"));
                if (almaUnica != null)
                    EcoRed.AnunciarAlPortador(p, "Mods.AethonMod.Caida.AlmaDespertada",
                        new Color(255, 215, 120), nombre,
                        Language.GetTextValue($"Mods.AethonMod.Caida.{almaUnica}"));

                // EL FESTÍN CAMINA (stats nuevos a la máquina del portador).
                EcoRed.SincronizarDevoracion(p);
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("DespertarGrimorioSistema", exRegistro); }
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

        // ================================================================
        //  v6.93 — EL FESTÍN DE LOS MATERIALES (la letra: «Ademas cualquier
        //  material o cantidad de materiales se pueden consagrar para
        //  aumentar la exp del grimorio, la cantidad exp minima que pueda
        //  dar 1 material es de 10, la cantidad de exp que puede dar un
        //  material se debe calcular dependiendo la rareza del material,
        //  como se obtuvo y el momento del juego en el que se obtiene»).
        //  El proxy de la casa para «cómo se obtuvo y el momento»: el
        //  VALOR DE VENTA de la unidad (lo que vale un lingote de
        //  luminita no vale uno de cobre) sobre la escala de rareza.
        // ================================================================

        /// <summary>EL FESTÍN DE UN MATERIAL — v6.97 PARCIAL (la letra
        /// nueva: «si pide 30 lingotes y arrojo 50, come 30 y deja el
        /// resto»): come SOLO las unidades que faltan de la cuota, la
        /// EXP de las comidas al libro y lo comido al BANCO por su tipo
        /// — el resto de la pila vive en el suelo.</summary>
        private static void DevorarMaterialParcial(Player p, Item grimorio, Item drop, int come)
        {
            try
            {
                if (p == null || grimorio == null || drop == null || drop.IsAir) return;
                come = System.Math.Max(1, System.Math.Min(come, drop.stack));

                var alma = grimorio.GetGlobalItem
                    <global::AethonMod.Content.Globals.AlmaEternoGlobalItem>();
                if (alma == null) return;

                long expUnidad = ExpPorMaterial(drop);
                long exp = expUnidad * come;
                alma.DevoracionExp += exp;

                // LA EXP AL LIBRO (materiales: sin nivel forzado — la letra
                //     del mínimo de 1 nivel era para las ARMAS).
                var slMat = grimorio.GetGlobalItem
                    <global::AethonMod.Content.Globals.ShardLevelItem>();
                if (slMat != null)
                    slMat.GrantXP(grimorio, (int)System.Math.Min(int.MaxValue, exp));

                // AL BANCO por su tipo (las comidas — retroactivo).
                global::AethonMod.Content.Globals.AlmaEternoGlobalItem
                    .BancoGuardarItem(grimorio, drop.type, come);

                // LA MUERTE DEL PLATO (en todas las pantallas — la pila
                //     que sobra VIVE en el suelo).
                string nombre = drop.Name;
                int cuantas = come;
                AnimarElBocado(p, drop, 2f);   // EL FESTÍN CHICO del material
                drop.stack -= come;
                if (drop.stack <= 0) drop.TurnToAir();
                int idx = Array.IndexOf(Main.item, drop);
                if (Main.netMode == NetmodeID.Server && idx >= 0)
                    NetMessage.SendData(MessageID.SyncItem, -1, -1, null, idx);

                // LOS POLVOS Y LA VOZ.
                if (!Main.dedServ)
                {
                    for (int k = 0; k < System.Math.Min(20, 4 + cuantas); k++)
                    {
                        Dust d = Dust.NewDustPerfect(p.Center, DustID.Shadowflame,
                            new Vector2(Main.rand.NextFloat(-2.2f, 2.2f),
                                Main.rand.NextFloat(-3f, 0.4f)), 150,
                            new Color(110, 40, 170), 0.9f);
                        d.noGravity = true;
                    }
                }
                EcoRed.AnunciarAlPortador(p, "Mods.AethonMod.Caida.MaterialDevorado",
                    new Color(196, 138, 255), nombre, cuantas, exp);

                // EL FESTÍN CAMINA.
                EcoRed.SincronizarDevoracion(p);
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("DespertarGrimorioSistema", exRegistro); }
        }

        /// <summary>LA EXP DE UNA UNIDAD de material (la letra: mínimo 10;
        ///     crece con la rareza y el valor de venta — el proxy de la
        ///     casa para «cómo se obtuvo y el momento del juego»).</summary>
        private static long ExpPorMaterial(Item it)
        {
            if (it == null || it.IsAir) return 10L;
            long porRareza = 10L * (1L + System.Math.Max(0, it.rare));
            long porValor = System.Math.Max(0L, it.value) / 10_000L;   // 1 exp por cada 10 de oro que vale la unidad
            return System.Math.Max(10L, porRareza + porValor);
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
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("DespertarGrimorioSistema", exRegistro); }
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
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("DespertarGrimorioSistema", exRegistro); }
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
