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
                    // v6.91 — LA COMPUTA DE LOS DROPS (la letra: «solo
                    //     cuando el jugador lo usa y el grimorio tiene un
                    //     nivel de despertar de 20 o superior es que dejan
                    //     de salir»): mientras no haya usado el suyo, la
                    //     ruleta SIGUE viva para él.
                    if (ElLibroYaNoLeToca(p, cazador)) continue;

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
                    if (cazador == null) return;
                    // v6.91 — la misma computa nueva: sin usar (o con el
                    //     grimorio bajo el 20), las criaturas SIGUEN
                    //     tirando la carta del libro.
                    if (ElLibroYaNoLeToca(p, cazador)) return;

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
                    // === 2) EL DINERO — SIN SELLO, ENTERO, al BANCO (la
                    //     letra de la sesión del banco: «al consumir 100
                    //     monedas de platino estas completarian todos los
                    //     checklist que pidan dinero hasta llegar a
                    //     consumir las 100» + la letra nueva: «el dinero
                    //     no se consagra, simplemente se deja caer,
                    //     recuerda que el grimorio solo devora lo que el
                    //     jugador deja caer»): el Eterno se traga la pila
                    //     entera — SIN CONSAGRAR, como las esencias — y la
                    //     suma al banco del despertar; los peldaños cobran
                    //     de ahí, hoy o MAÑANA (retroactivo). El dinero no
                    //     pasa por el altar: cae delante del portador y ya.
                    //     El ESCUPIDERO de cambio de la v6.51 murió con la
                    //     mesa vieja: ya no se come «solo lo que falta». ===
                    else if (drop.type == ItemID.CopperCoin || drop.type == ItemID.SilverCoin ||
                        drop.type == ItemID.GoldCoin || drop.type == ItemID.PlatinumCoin)
                    {
                        long cobre = ValorDeMoneda(drop.type) * drop.stack;
                        AnimarElBocado(p, drop, 1f);   // v6.51.12: EL FESTÍN CHICO (antes del trago)
                        global::AethonMod.Content.Globals.AlmaEternoGlobalItem
                            .BancoGuardarMonedas(libro, cobre);
                        drop.TurnToAir();
                        SincronizarDrop(d);

                        PolvosDelBocado(p, 3);
                        EcoRed.AnunciarAlPortador(p, "Mods.AethonMod.Despertar.BocadoMonedas",
                            new Color(196, 138, 255),
                            ItemToText(cobre));
                        mordisco = true;
                    }
                    // === 3) EL FESTÍN DE SIEMPRE, UNIFICADO (v6.93 — la
                    //     muerte de los casos «EL MATERIAL DEL PELDAÑO» y
                    //     «EL ARMA DEL PELDAÑO»): TODO lo consagrado que
                    //     cae es PLATO — la letra: «cualquier arma se puede
                    //     consagrar para que el grimorio las coma aunque no
                    //     seran un requisito valido para despertar ya que
                    //     para despertar solo son validas las armas de mago
                    //     e invocador» + «cualquier material o cantidad de
                    //     materiales se pueden consagrar para aumentar la
                    //     exp del grimorio». El arma trae su EXP (mínimo
                    //     500), su stat por clase y su alma única; el
                    //     material su EXP (mínimo 10 por unidad); y AMBOS
                    //     caen al BANCO por su tipo — el pago del peldaño
                    //     es RETROACTIVO: las 2 opciones de Devorar que
                    //     pida (armas y/o materiales), hoy o MAÑANA, salen
                    //     de ahí. Sin sello: el aviso con calma. ===
                    else
                    {
                        var selloFestin = drop.GetGlobalItem
                            <global::AethonMod.Content.Globals.CorruptoGlobalItem>();
                        if (selloFestin == null || !selloFestin.Corrompido)
                        {
                            AvisoConCalma(p, "Mods.AethonMod.Despertar.NoConsagrado");
                        }
                        else if (EsArma(drop))
                        {
                            DevorarComoBocado(p, libro, drop);
                            mordisco = true;
                        }
                        else
                        {
                            DevorarMaterialBocado(p, libro, drop);
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
            // v6.93 — DEVORAR: AL MENOS 2 DE LAS OPCIONES.
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
            int[] armas = DespertarGrimorio.ArmasDelNivel[
                System.Math.Max(1, System.Math.Min(DespertarGrimorio.NivelMax, nivel))];
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
        /// Dejan de caer SOLO con el USO **Y** el nivel 20 del grimorio
        /// que porta, JUNTOS: si no usó el suyo, SIGUEN cayendo (aunque
        /// lleve uno sin usar — la recogida limita a 1 EN EL INVENTARIO,
        /// pero el mundo puede escupirle otro).</summary>
        private static bool ElLibroYaNoLeToca(Player p, Players.CazaJefesPlayer cazador)
        {
            try
            {
                if (p == null || !p.active) return true;
                if (cazador == null) return true;
                if (!cazador.LibroUsado) return false;   // sin usar: SIGUEN saliendo
                var alma = ElAlmaDelPortador(p);
                return alma != null && alma.NivelDespertar >= 20;
            }
            catch { return true; }
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
            catch { }
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
                if (alma.NivelDespertar != 19) return;          // la víspera exacta
                if (alma.LibroCorruptoConsumido) return;         // su puerta ya nació paga
                if (LlevaElLibro(p)) return;                     // ya lo lleva encima

                var cazador = p.GetModPlayer<Players.CazaJefesPlayer>();
                // v6.91 — la computa nueva: si aún le tocan libros
                //     (no usó el suyo o su grimorio va bajo el 20), la
                //     víspera SIGUE garantizando la llave.
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
                // pagado mueren; los del nuevo nacen en 0). v6.93 — la
                // mesa vieja del MATERIAL/ARMA/MONEDAS-por-peldaño murió
                // (el banco la entierra): los contadores se limpian por
                // compatibilidad de los saves, ya nadie los lee.
                alma.OfrendaEsencias = 0;
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
                    int porCobrar = 2;
                    int[] armasCobro = DespertarGrimorio.ArmasDelNivel[
                        System.Math.Max(1, System.Math.Min(DespertarGrimorio.NivelMax, nivel))];
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
                catch { }

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

        /// <summary>EL FESTÍN DE UN MATERIAL: la pila entera al EXP del
        /// libro y al BANCO por su tipo (la otra cara del festín de
        /// siempre — v6.93).</summary>
        private static void DevorarMaterialBocado(Player p, Item grimorio, Item drop)
        {
            try
            {
                var alma = grimorio?.GetGlobalItem
                    <global::AethonMod.Content.Globals.AlmaEternoGlobalItem>();
                if (alma == null) return;

                long expUnidad = ExpPorMaterial(drop);
                long exp = expUnidad * drop.stack;
                alma.DevoracionExp += exp;

                // LA EXP AL LIBRO (materiales: sin nivel forzado — la letra
                //     del mínimo de 1 nivel era para las ARMAS).
                var slMat = grimorio.GetGlobalItem
                    <global::AethonMod.Content.Globals.ShardLevelItem>();
                if (slMat != null)
                    slMat.GrantXP(grimorio, (int)System.Math.Min(int.MaxValue, exp));

                // AL BANCO por su tipo (la pila entera — retroactivo).
                global::AethonMod.Content.Globals.AlmaEternoGlobalItem
                    .BancoGuardarItem(grimorio, drop.type, drop.stack);

                // LA MUERTE DEL PLATO (en todas las pantallas).
                string nombre = drop.Name;
                int cuantas = drop.stack;
                AnimarElBocado(p, drop, 2f);   // EL FESTÍN CHICO del material
                drop.TurnToAir();
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
                EcoRed.HablarVarianteAlPortador(p, "Mods.AethonMod.Caida.DevoradoSabor",
                    4, new Color(178, 110, 236), rugido: true, escala: 0.58f);
                EcoRed.AnunciarAlPortador(p, "Mods.AethonMod.Caida.MaterialDevorado",
                    new Color(196, 138, 255), nombre, cuantas, exp);

                // EL FESTÍN CAMINA.
                EcoRed.SincronizarDevoracion(p);
            }
            catch { }
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
