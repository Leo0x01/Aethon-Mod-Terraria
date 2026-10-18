using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AethonMod.Content.Systems
{
    // ======================================================================
    //  LA CAÍDA (v6.50.117) — LA OBTENCIÓN CANÓNICA DEL GRIMORIO ETERNO.
    //
    // LA LETRA DEL USUARIO: «por cada jugador cae un cometa de tinta (en
    // MP uno por jugador)», «reusar la estética del meteorito de fondo de
    // Terraria pero en NEGRO y MORADO», «un grimorio por jugador, cada
    // jugador solo puede tener 1, si ya tiene uno no puede tomar otro»,
    // v6.50.118 — «el cometa de tinta caiga CERCA DEL JUGADOR» (la cita
    // vieja «lejos del spawn (~30 pantallas)» MURIÓ: 32–64 tiles a un
    // lado del destinatario). LA PURGA DEL GÉNESIS mató el altar y su
    // fragmento: ESTE es el camino del libro ahora — el día 1, AL CAER
    // LA PRIMERA NOCHE (v6.50.119: time > 480 — la .117 esperaba
    // pasada la medianoche y el cometa llegaba cuando el jugador ya
    // se había ido a dormir).
    //
    // EL CIRCUITO COMPLETO:
    //   1. PreUpdateTime (solo AUTORIDAD): cada jugador activo cuya caída
    //      TODAVÍA NO sucedió EN ESTE MUNDO (v6.50.125: la lista
    //      _cayoEnEsteMundo — la marca del .plr CaidaRecibida YA NO calla
    //      al cielo: la ponían la toma de la .117 y la devoración de la
    //      .122 y silenciaba el cometa PARA SIEMPRE y EN TODOS LOS MUNDOS
    //      — el personaje de pruebas jamás volvía a ver el evento, la
    //      letra: «la caída no parece activarse cuando pasa la primera
    //      noche»), sin caída pendiente a su nombre, en noche recién caída
    //      (time > 480) y sin cometa YA esta noche → nace su
    //      CaidaCometaProjectile apuntando CERCA DEL JUGADOR (la
    //      .118: 32–64 tiles a un lado — antes ~30 pantallas del spawn).
    //      v6.50.121 — EL COMETA ES DE TODOS: la .117-.120 exigían NO
    //      llevar el Grimorio del Eterno encima para dispararlo… pero el
    //      KIT DE PRUEBAS entrega la Bolsa Fundacional CON el grimorio
    //      dentro: el jugador de pruebas SIEMPRE lo llevaba y EL COMETA
    //      JAMÁS CAÍA (la letra: «La Caída nunca se activa al usar el
    //      ítem de hacer de noche y luego de día»). Ahora el cielo cae
    //      SIEMPRE en la primera noche de cada jugador (el ESPECTÁCULO
    //      no se le niega a nadie) y la regla «un grimorio por alma»
    //      vive donde siempre vivió: en LA TOMA. v6.50.122 — Y SI YA
    //      TIENES UNO, TE LO COMEN: TomarElLibro ya no RECHAZA al
    //      portador con una charla — el Eterno del inventario hace lo
    //      mismo que con los jefes en la primera muerte: SELLOS + LA
    //      SOMBRA DE LA PÁGINA devoran al doble caído (GrimorioDormido
    //      gatilla al acercarse, CaidaDevoradaProjectile actúa,
    //      DevorarLibroCaido cierra) y el libro dice su línea por los
    //      PENSAMIENTOS (EcoRed).
    //   2. El cometa cruza el cielo, pica, excava su cráter y deja al
    //      GRIMORIO DORMIDO flotando en el centro (NPC town con ventana
    //      de diálogo vanilla) + registra aquí su CaidaPendiente.
    //   3. El destinatario «extiende la mano» → el libro habla por
    //      primera vez y se entrega (LA DÁDIVA nace de la AUTORIDAD —
    //      la lección del drop fantasma del Altar, auditoría nº5).
    //
    // PERSISTENCIA (patrón de EsenciasModSistema): la lista de caídas
    // vive en el SaveWorldData — el libro dormido SOBREVIVE sesiones
    // (OnWorldLoad lo re-engendra en su cráter para su destinatario).
    // ======================================================================
    public class LaCaidaSistema : ModSystem
    {
        /// <summary>Una caída aún no reclamada: el libro duerme en (X, Y)… y espera a Jugador.</summary>
        public class CaidaPendiente
        {
            public float X;
            public float Y;
            public string Jugador;
        }

        /// <summary>Los libros caídos aún no reclamados (el estado por mundo).</summary>
        public readonly List<CaidaPendiente> CaidasPendientes = new List<CaidaPendiente>();

        /// <summary>¿Ya cayó un cometa para este jugador ESTA noche? (índice → bool; se limpia al amanecer).</summary>
        private readonly bool[] _cayoEstaNoche = new bool[Main.maxPlayers];

        /// <summary>v6.50.128→.130 — UN ALTAR DEL MUNDO: la ZONA de
        /// corrupción del minibioma (vive para SIEMPRE — es la puerta de
        /// LA CORRUPCIÓN DE ITEMS del AltarDevoradorSistema). v6.50.130 —
        /// DOS clases de altar: el del CRÁTER (nació con la caída, no
        /// tiene tile — la puerta es la bruma) y EL DE LA BRUMA COLOCADA
        /// (la letra: «un item invisible que es la bruma en si… los
        /// jugadores pueden tomarla y colocarla donde quieran, este item
        /// de bruma sera usado como altar principal» — el jugador puso SU
        /// bruma: la tile manda y si la quitan, la zona se va con ella).
        /// v6.50.130 — BrumaEntregada: el cráter YA soltó SU item de
        /// bruma (una sola vez por cráter — el respaldo de la letra).</summary>
        public class AltarDelMundo
        {
            /// <summary>La columna de la tile del altar.</summary>
            public int X;
            /// <summary>La fila de la tile del altar.</summary>
            public int Y;
            /// <summary>v6.50.130 — TRUE si es una BRUMA COLOCADA por el
            /// jugador (la zona vive y muere con SU tile).</summary>
            public bool Colocada;
            /// <summary>v6.50.130 — TRUE si el cráter ya entregó SU item
            /// de bruma (los cráteres viejos también — nadie se queda sin
            /// la suya, pero NADIE recibe dos).</summary>
            public bool BrumaEntregada;

            /// <summary>El centro del altar en píxeles del mundo.</summary>
            public Vector2 Posicion() => new(X * 16f + 8f, Y * 16f + 8f);
        }

        /// <summary>v6.50.128 — LOS ALTARES del mundo (nacen con cada
        /// caída y JAMÁS mueren: aunque el libro dormido se marche, el
        /// altar sigue corrompiendo items para el Eterno — la mecánica
        /// que «dará más vida al mod», la letra).</summary>
        public readonly List<AltarDelMundo> Altares = new List<AltarDelMundo>();

        /// <summary>v6.50.125 — LOS NOMBRES QUE YA VIVIERON SU CAÍDA EN ESTE
        /// MUNDO (el silenciador VERDADERO del cielo). La letra: «la caída
        /// no parece activarse cuando pasa la primera noche» — la marca del
        /// .plr (CaidaRecibida, la puso la toma de la .117 o la devoración
        /// del doble de la .122) callaba al cometa PARA SIEMPRE y EN TODOS
        /// LOS MUNDOS: el personaje de pruebas, que ya lo tomó o devoró
        /// todo, jamás volvía a ver el evento por muchos mundos nuevos que
        /// abriera. AHORA el cielo calla SOLO por ESTE mundo: la primera
        /// noche de cada jugador EN CADA MUNDO tiene su cometa (el espíritu
        /// .121: «el espectáculo no se le niega a nadie») y la regla «un
        /// grimorio por alma» sigue viva donde siempre vivió: en LA TOMA.
        /// La Página en Blanco también borra ESTA lista (el probador puede
        /// re-estrenar la caída en el mismo mundo).</summary>
        private readonly HashSet<string> _cayoEnEsteMundo = new HashSet<string>();

        /// <summary>v6.50.126 — EL FAILSAFE DEL PRIMER VECINO (la letra:
        /// «este deberá activarse en caso de que otros métodos no lo hayan
        /// activado en cuanto llegue el primer NPC diferente del guía»):
        /// ¿ya llegó al mundo SU primer habitante que no es el Guía? El
        /// momento queda grabado EN EL MUNDO — es la red de seguridad que
        /// garantiza que LA CAÍDA nunca se pierde (primera noche → primer
        /// vecino → Tinta del Cometa: TRES caminos al cielo).</summary>
        private bool _llegoPrimerNpc;

        /// <summary>El estado del cielo del tick anterior (para sentir el amanecer).</summary>
        private bool _eraNoche;

        // ================================================================
        //  LA PERSISTENCIA (el libro sobrevive sesiones)
        // ================================================================

        public override void SaveWorldData(TagCompound tag)
        {
            // El patrón de la casa: blindado, aunque tML ya capture errores.
            try
            {
                var lista = new List<TagCompound>();
                foreach (var c in CaidasPendientes)
                {
                    if (c == null || string.IsNullOrEmpty(c.Jugador)) continue;
                    lista.Add(new TagCompound
                    {
                        ["x"] = c.X,
                        ["y"] = c.Y,
                        ["jugador"] = c.Jugador
                    });
                }
                tag["caidasPendientes"] = lista;
                // v6.50.125 — el silenciador por-mundo viaja con el guardado
                // (los nombres que ya vivieron su caída AQUÍ).
                tag["cayosDelMundo"] = new List<string>(_cayoEnEsteMundo);
                // v6.50.126 — el failsafe del primer vecino también viaja
                // (el momento quedó grabado en ESTE mundo).
                tag["llegoPrimerNpc"] = _llegoPrimerNpc;
                // v6.50.128 — los altares viajan con el mundo (la puerta de
                // la corrupción es ETERNA — cada cráter guarda el suyo).
                // v6.50.130 — Colocada/BrumaEntregada también viajan (la
                // bruma colocada por el jugador ES su altar; el cráter que
                // ya soltó su bruma no suelta otra).
                var altares = new List<TagCompound>();
                foreach (var a in Altares)
                {
                    if (a == null) continue;
                    var t = new TagCompound { ["x"] = a.X, ["y"] = a.Y };
                    if (a.Colocada) t["col"] = true;
                    if (a.BrumaEntregada) t["bruma"] = true;
                    altares.Add(t);
                }
                tag["altares"] = altares;
            }
            catch { }
        }

        public override void LoadWorldData(TagCompound tag)
        {
            CaidasPendientes.Clear();
            try
            {
                var lista = tag.GetList<TagCompound>("caidasPendientes");
                foreach (var entrada in lista)
                {
                    string jugador = entrada.GetString("jugador");
                    if (string.IsNullOrEmpty(jugador)) continue;
                    CaidasPendientes.Add(new CaidaPendiente
                    {
                        X = entrada.GetFloat("x"),
                        Y = entrada.GetFloat("y"),
                        Jugador = jugador
                    });
                }
                // v6.50.125 — el silenciador por-mundo vuelve con el mundo
                // (vacío en los mundos viejos: su primera noche bajo la
                // .125 vuelve a cruzar el cielo — la cura de la letra).
                foreach (string nombre in tag.GetList<string>("cayosDelMundo"))
                {
                    if (!string.IsNullOrEmpty(nombre)) _cayoEnEsteMundo.Add(nombre);
                }
                // v6.50.126 — el failsafe del primer vecino vuelve con el
                // mundo (false en los mundos viejos: su «momento» se
                // re-vive al cargar — el espectáculo no se le niega a nadie
                // y el evento NUNCA se pierde).
                _llegoPrimerNpc = tag.GetBool("llegoPrimerNpc");

                // v6.50.128 — los altares vuelven con el mundo (y los de
                // las caídas VIEJAS sin altar — mundos de la .127 con su
                // libro aún esperando — se FORJAN al cargar: nadie se
                // queda sin su mecánica).
                Altares.Clear();
                foreach (var entrada in tag.GetList<TagCompound>("altares"))
                {
                    Altares.Add(new AltarDelMundo
                    {
                        X = entrada.GetInt("x"),
                        Y = entrada.GetInt("y"),
                        Colocada = entrada.GetBool("col"),
                        BrumaEntregada = entrada.GetBool("bruma")
                    });
                }
                ForjarAltaresFaltantes();
                // v6.50.130 — LA BRUMA EN LAS MANOS: cada cráter que aún
                // no soltó SU item de bruma lo suelta ahora (los cráteres
                // de la .129 reciben la suya al cargar el mundo).
                EntregarBrumasPendientes();
            }
            catch { }
        }

        public override void OnWorldUnload()
        {
            CaidasPendientes.Clear();
            _cayoEnEsteMundo.Clear();
            _llegoPrimerNpc = false;
            Altares.Clear();
            Array.Clear(_cayoEstaNoche, 0, _cayoEstaNoche.Length);
            _eraNoche = false;
        }

        // ================================================================
        //  EL RE-ENGENDRO — los libros dormidos sobreviven sesiones
        // ================================================================

        public override void OnWorldLoad()
        {
            try
            {
                // La lista del mundo vive con la AUTORIDAD (el guardado del
                // cliente MP no la trae): en clientes los libros llegan por
                // el cable (netAlways), no por aquí.
                if (Main.netMode == NetmodeID.MultiplayerClient) return;

                foreach (var pendiente in CaidasPendientes)
                {
                    if (pendiente == null || string.IsNullOrEmpty(pendiente.Jugador)) continue;
                    RevivirLibro(pendiente);
                }
            }
            catch { }
        }

        /// <summary>Engendra el Grimorio Dormido de una pendiente (si aún no vive) para SU destinatario.</summary>
        private static void RevivirLibro(CaidaPendiente pendiente)
        {
            try
            {
                // ¿Ya vive el libro de ESTA pendiente? (otro engendro del
                // mismo tick, o un cliente que llegó antes — no duplicar).
                if (BuscarLibroCerca(pendiente.X, pendiente.Y) != null) return;

                int idx = IndiceDeJugador(pendiente.Jugador);
                int npcIdx = NPC.NewNPC(new EntitySource_Misc("LaCaida"),
                    (int)pendiente.X, (int)pendiente.Y,
                    ModContent.NPCType<NPCs.GrimorioDormido>(), 0, 0f, idx);
                NPC libro = (npcIdx >= 0 && npcIdx < Main.maxNPCs) ? Main.npc[npcIdx] : null;
                if (libro != null && libro.active)
                {
                    // EL NOMBRE DEL ESPERADO: el libro lo lleva puesto (y
                    // viaja por el cable — la ventana de diálogo lo usa).
                    libro.GivenName = pendiente.Jugador;
                    libro.netUpdate = true;
                    DifundirLibro(libro);
                }
            }
            catch { }
        }

        /// <summary>El índice del jugador vivo con este nombre (-1 si no está conectado).</summary>
        private static int IndiceDeJugador(string nombre)
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p != null && p.active && p.name == nombre) return i;
            }
            return -1;
        }

        /// <summary>El Grimorio Dormido vivo a menos de 64 px de un punto (null si nadie).</summary>
        private static NPC BuscarLibroCerca(float x, float y)
        {
            int tipo = ModContent.NPCType<NPCs.GrimorioDormido>();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || n.type != tipo) continue;
                if (Math.Abs(n.Center.X - x) <= 64f && Math.Abs(n.Center.Y - y) <= 64f)
                    return n;
            }
            return null;
        }

        /// <summary>Sincroniza un libro recién nacido (la posición + su ai[1] viajan en el SyncNPC).</summary>
        private static void DifundirLibro(NPC libro)
        {
            if (Main.netMode != NetmodeID.Server) return;
            try
            {
                // El SyncNPC lleva la posición y el ai[1] (el destinatario);
                // GivenName no viaja — cada cliente lo reconstruye de su
                // ai[1] en su propia IA (GrimorioDormido, cada 60 ticks).
                Terraria.NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, libro.whoAmI);
            }
            catch { }
        }

        // ================================================================
        //  EL CIELO DE LA NOCHE RECÉN CAÍDA (solo AUTORIDAD)
        // ================================================================

        public override void PreUpdateTime()
        {
            try
            {
                if (Main.gameMenu) return;
                if (Main.netMode == NetmodeID.MultiplayerClient) return;

                // v6.50.130 — LA VIGILANCIA DE LAS BRUMAS COLOCADAS (cada
                // 10 s): la bruma cuya tile murió se despide — su zona se
                // va con ella (la letra: el altar es la bruma COLOCADA).
                if (Main.GameUpdateCount % 600ul == 0ul)
                    ValidarBrumasColocadas();

                // v6.50.130 — EL DESCUBRIMIENTO (cada 30 t): ESTE tML
                // 2026.08 no trae hook de colocación — la AUTORIDAD mira
                // alrededor de cada jugador y registra las brumas NUEVAS
                // que encuentre (la colocación SIEMPRE pasa cerca de un
                // jugador: la zona nace sola, en SP y en MP por igual).
                if (Main.GameUpdateCount % 30ul == 0ul)
                    DescubrirBrumasColocadas();

                // EL AMANECER LIMPIA LA NOCHE: un cometa por jugador por noche.
                bool esNoche = !Main.dayTime;
                if (_eraNoche && !esNoche)
                    Array.Clear(_cayoEstaNoche, 0, _cayoEstaNoche.Length);
                _eraNoche = esNoche;

                // v6.50.119 — AL CAER LA PRIMERA NOCHE (la letra: «el
                // evento de caída no parece activarse, debe activarse al
                // caer la primera noche»): time > 480 — apenas 8 s tras
                // el ocaso, el cielo ya es de noche y el cometa cruza
                // AHORA (la .117 esperaba la medianoche: con la ventana
                // a time>12000, el jugador esperaba TODA la primera
                // mitad de la noche sin ver nada y se iba a dormir).
                if (esNoche && Main.time > 480.0)
                {
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        Player p = Main.player[i];
                        if (p == null || !p.active || p.dead) continue;
                        if (_cayoEstaNoche[i]) continue;

                        // El pulso de la casa: cada jugador se revisa cada
                        // 30 ticks (desfase propio — sin avalancha).
                        if ((Main.GameUpdateCount + (ulong)i * 7ul) % 30ul != 0ul) continue;

                        var sp = p.GetModPlayer<Players.ShardPlayer>();
                        if (sp == null) continue;

                        // v6.50.125 — LA MARCA DEL .PLR YA NO CALLA AL
                        // CIELO (la letra: «la caída no parece activarse
                        // cuando pasa la primera noche»): el silenciador
                        // es ESTE mundo — la primera noche de cada jugador
                        // EN CADA MUNDO tiene su cometa (el espíritu .121:
                        // el espectáculo no se le niega a nadie) y la regla
                        // «un grimorio por alma» vive donde siempre vivió:
                        // en LA TOMA (el gate del grimorio murió en la
                        // .121 — TomarElLibro rechaza al portador).
                        if (_cayoEnEsteMundo.Contains(p.name)) continue;
                        if (YaEsperaLibro(p.name)) continue;
                        // v6.50.126 — ni cometa YA en vuelo: los TRES
                        // caminos al cielo (primera noche · primer vecino ·
                        // Tinta del Cometa) jamás se pisan entre sí.
                        if (CometaEnVuelo(i)) continue;

                        DispararCometa(p);
                        _cayoEstaNoche[i] = true;
                        // v6.50.125 — este mundo ya le dio su caída (aunque
                        // el cometa muera en pleno vuelo, no repite).
                        _cayoEnEsteMundo.Add(p.name);
                    }
                }

                // ============================================================
                //  v6.50.126 — EL FAILSAFE DEL PRIMER VECINO (la letra:
                //  «este deberá activarse en caso de que otros métodos no
                //  lo hayan activado en cuanto llegue el primer NPC
                //  diferente del guía»). LA CAÍDA NUNCA SE PIERDE: si la
                //  primera noche pasó de largo (jugador muerto en su
                //  momento, desconectado a media noche, un mundo cargado a
                //  media noche ya vivida…), el PRIMER habitante del mundo
                //  que NO es el Guía trae el cometa consigo — de día o de
                //  noche. El momento queda grabado EN EL MUNDO (una sola
                //  vez); los mundos VIEJOS (con vecinos desde hace lunas)
                //  lo re-viven al cargar: solo dispara para quien TODAVÍA
                //  no vivió su caída aquí (las guardas de arriba protegen
                //  a los demás).
                // ============================================================
                if (!_llegoPrimerNpc && HayVecinoQueNoEsElGuia())
                {
                    _llegoPrimerNpc = true;
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        Player p = Main.player[i];
                        if (p == null || !p.active || p.dead) continue;
                        if (_cayoEnEsteMundo.Contains(p.name)) continue;
                        if (YaEsperaLibro(p.name)) continue;
                        if (CometaEnVuelo(i)) continue;

                        DispararCometa(p);
                        _cayoEstaNoche[i] = true;
                        _cayoEnEsteMundo.Add(p.name);
                    }
                }

                // EL RECONCILIADOR (cada 120 ticks): las pendientes cuyos
                // destinatarios ENTRARON TARDE (el libro los espera igual).
                if ((Main.GameUpdateCount % 120ul) == 0ul)
                    ReconciliarPendientes();
            }
            catch { }
        }

        /// <summary>¿Este jugador ya tiene un Grimorio del Eterno en los 59 slots del inventario?</summary>
        public static bool TieneGrimorio(Player p)
        {
            if (p == null) return false;
            int eterno = ModContent.ItemType<Content.Weapons.GrimoireEternal>();
            for (int k = 0; k < 59; k++)
            {
                Item inv = p.inventory[k];
                if (inv != null && !inv.IsAir && inv.type == eterno) return true;
            }
            return false;
        }

        /// <summary>¿Ya cae/duerme un libro para este nombre? (un grimorio por jugador — sin cráteres en serie).</summary>
        private static bool YaEsperaLibro(string nombre)
        {
            var sys = ModContent.GetInstance<LaCaidaSistema>();
            if (sys == null) return false;
            return sys.CaidasPendientes.Exists(c => c != null && c.Jugador == nombre);
        }

        /// <summary>¿Tiene este jugador un cometa de LA CAÍDA en pleno
        /// vuelo? (v6.50.126 — el guarda-choques de los TRES caminos: la
        /// primera noche, el primer vecino y la Tinta del Cometa jamás
        /// disparan dos cometas al mismo alma).</summary>
        public static bool CometaEnVuelo(int idxJugador)
        {
            if (idxJugador < 0 || idxJugador >= Main.maxPlayers) return false;
            int tipo = ModContent.ProjectileType<Projectiles.CaidaCometaProjectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile pr = Main.projectile[i];
                if (pr == null || !pr.active || pr.type != tipo) continue;
                if (pr.owner == idxJugador) return true;
            }
            return false;
        }

        /// <summary>v6.50.126 — ¿ya vive en el mundo un vecino que NO es
        /// el Guía? (el detector del failsafe: townNPC activo y de otro
        /// tipo — el Mercader que llega con 50 monedas, la Enfermera que
        /// sigue a la primera vida… el PRIMER HABITANTE de verdad).</summary>
        private static bool HayVecinoQueNoEsElGuia()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || !n.townNPC) continue;
                if (n.type == NPCID.Guide) continue;
                return true;
            }
            return false;
        }

        /// <summary>v6.50.126 — LA TINTA DEL COMETA (la letra: «crea un
        /// item para activar el evento, este será un item de prueba,
        /// además como activador obligatorio para evitar que el evento
        /// del grimorio se pierda»). El camino MANUAL al cielo: rompe
        /// TODAS las reglas del horario (de día, de noche, con la caída
        /// de este mundo ya vivida — es un ítem DE PRUEBAS) PERO jamás
        /// duplica: si un libro ya duerme para este nombre o un cometa
        /// vuela hacia él, el cielo no insiste. Devuelve 0 = disparado ·
        /// 1 = ya hay libro esperando · 2 = cometa en vuelo (para el
        /// aviso de la tinta).</summary>
        public static int ForzarCaida(Player p)
        {
            try
            {
                if (Main.netMode == NetmodeID.MultiplayerClient) return -1;
                if (p == null || !p.active || p.dead || string.IsNullOrEmpty(p.name)) return -1;

                if (YaEsperaLibro(p.name)) return 1;
                if (CometaEnVuelo(p.whoAmI)) return 2;

                DispararCometa(p);
                var sys = ModContent.GetInstance<LaCaidaSistema>();
                if (sys != null && p.whoAmI >= 0 && p.whoAmI < Main.maxPlayers)
                    sys._cayoEstaNoche[p.whoAmI] = true;
                // ESTE mundo queda servido para este nombre (los caminos
                // automáticos no volverán a insistir tras la prueba).
                RegistrarCaidaDelMundo(p.name);
                return 0;
            }
            catch { return -1; }
        }

        /// <summary>v6.50.118 — LA LETRA: «el cometa de tinta caiga cerca
        /// del jugador»: el punto de impacto vive a 32–64 tiles a un lado
        /// DEL JUGADOR (lo ve cruzar el cielo y cae a la vista — un
        /// momento caminando y ya está sobre su cráter). La .117 lo
        /// medía desde el SPAWN DEL MUNDO (~30 pantallas): un jugador
        /// con base lejos del spawn se perdía la caída y el cráter caía a
        /// un océano de distancia. La dirección sigue siendo NO vertical
        /// (la estética del picado), con los límites del mundo de
        /// siempre y la superficie de esa columna.</summary>
        private static Vector2 PuntoDeImpacto(Player p)
        {
            // LA DIRECCIÓN: un lado al azar del JUGADOR y una distancia
            // corta (32–64 tiles = 512–1024 px: en pantalla o a un paso
            // de ella — «cerca del jugador», la letra).
            float lado = Main.rand.NextBool() ? 1f : -1f;
            float distTiles = Main.rand.NextFloat(32f, 64f);
            Vector2 destino = new Vector2(p.Center.X + lado * distTiles * 16f, p.Center.Y);

            // LOS LÍMITES DEL MUNDO (margen de 200 tiles del borde).
            destino.X = MathHelper.Clamp(destino.X, 200f * 16f, (Main.maxTilesX - 200) * 16f);

            // LA SUPERFICIE: la primera tile SÓLIDA de esa columna, del
            // cielo hacia abajo (la estrella besa la tierra, no la
            // roca); si la columna es aire puro, la altura DEL JUGADOR
            // la salva (antes el spawn — el jugador manda ahora).
            destino.Y = p.Center.Y;
            int tx = (int)(destino.X / 16f);
            int limite = Main.maxTilesY - 200;
            for (int y = 20; y < limite; y++)
            {
                Tile tile = Main.tile[tx, y];
                if (tile == null || !tile.HasTile) continue;
                if (Main.tileSolid[tile.TileType]) { destino.Y = y * 16f; break; }
            }
            return destino;
        }

        /// <summary>Dispara el cometa de tinta de este jugador y le canta su caída (solo a él).</summary>
        private static void DispararCometa(Player p)
        {
            // v6.50.118 — el cráter nace CERCA DEL JUGADOR (la letra): la
            // estrella cae a la vista de su destinatario.
            Vector2 destino = PuntoDeImpacto(p);

            // NACE por encima de la cámara del destinatario, del lado del
            // impacto, viajando en DIAGONAL (la fase 0 del cometa).
            float lado = destino.X > p.Center.X ? 1f : -1f;
            Vector2 nace = p.Center + new Vector2(-lado * 1100f, -1400f);
            Vector2 vel = new Vector2(lado * 13.5f, 1.5f);

            int idx = Projectile.NewProjectile(new EntitySource_Misc("LaCaida"), nace, vel,
                ModContent.ProjectileType<Projectiles.CaidaCometaProjectile>(),
                0, 0f, p.whoAmI, 0f, destino.X / 1000f, destino.Y / 1000f);
            if (idx < 0 || idx >= Main.maxProjectiles) return;

            // EL AVISO DEL CIELO — PRIVADO: solo el destinatario ve la
            // estrella negra desgarrarse (SP: pantalla; MP: solo su chat).
            EcoRed.AnunciarAlPortador(p, "Mods.AethonMod.Caida.CometaCae",
                new Color(150, 60, 220));
        }

        // ================================================================
        //  EL IMPACTO — el cometa entrega el libro (lo llama el proyectil)
        // ================================================================

        /// <summary>Registra la caída y engendra al Grimorio Dormido EXACTO en el centro del cráter (solo AUTORIDAD).</summary>
        public static void RegistrarImpacto(Vector2 destino, int idxJugador)
        {
            try
            {
                if (Main.netMode == NetmodeID.MultiplayerClient) return;
                if (idxJugador < 0 || idxJugador >= Main.maxPlayers) return;
                Player p = Main.player[idxJugador];
                if (p == null || !p.active || string.IsNullOrEmpty(p.name)) return;

                var sys = ModContent.GetInstance<LaCaidaSistema>();
                if (sys == null) return;

                // SIN DUPLICADOS: un grimorio por jugador (la marca de la
                // noche ya frenó el segundo cometa — esto frena lo exótico).
                if (sys.CaidasPendientes.Exists(c => c != null && c.Jugador == p.name)) return;

                sys.CaidasPendientes.Add(new CaidaPendiente
                {
                    X = destino.X,
                    Y = destino.Y,
                    Jugador = p.name
                });

                // EL LIBRO FLOTANDO EN SU CRÁTER (ai[1] = el destinatario).
                int npcIdx = NPC.NewNPC(new EntitySource_Misc("LaCaida"),
                    (int)destino.X, (int)destino.Y,
                    ModContent.NPCType<NPCs.GrimorioDormido>(), 0, 0f, idxJugador);
                NPC libro = (npcIdx >= 0 && npcIdx < Main.maxNPCs) ? Main.npc[npcIdx] : null;
                if (libro != null && libro.active)
                {
                    libro.GivenName = p.name;
                    libro.netUpdate = true;
                    DifundirLibro(libro);
                }
            }
            catch { }
        }

        /// <summary>El reconciliador: pendientes sin libro vivo + destinatarios que entraron tarde reciben su libro igual.</summary>
        private static void ReconciliarPendientes()
        {
            try
            {
                var sys = ModContent.GetInstance<LaCaidaSistema>();
                if (sys == null) return;
                for (int i = sys.CaidasPendientes.Count - 1; i >= 0; i--)
                {
                    var pendiente = sys.CaidasPendientes[i];
                    if (pendiente == null || string.IsNullOrEmpty(pendiente.Jugador)) { sys.CaidasPendientes.RemoveAt(i); continue; }

                    NPC libro = BuscarLibroCerca(pendiente.X, pendiente.Y);
                    int idx = IndiceDeJugador(pendiente.Jugador);
                    if (libro == null)
                    {
                        // El destinatario está conectado y su libro no vive:
                        // el cráter no puede quedar huérfano (desconexión a
                        // media caída, mundo recargado… el libro REGRESA).
                        if (idx >= 0) RevivirLibro(pendiente);
                        continue;
                    }

                    // El índice vivo cambió (reconexión): el libro aprende
                    // el nuevo (la ventana de diálogo compara por NOMBRE —
                    // esto mantiene el respaldo ai[1] fresco).
                    if (idx >= 0 && (int)libro.ai[1] != idx)
                    {
                        libro.ai[1] = idx;
                        libro.GivenName = pendiente.Jugador;
                        libro.netUpdate = true;
                        DifundirLibro(libro);
                    }
                }
            }
            catch { }
        }

        // ================================================================
        //  LA TOMA — «extender la mano» (el clic del destinatario)
        // ================================================================

        /// <summary>
        /// EL LIBRO SE ENTREGA (solo AUTORIDAD — SP, host y server): valida,
        /// da el Grimorio del Eterno, marca al portador, apaga la pendiente,
        /// dice su primera palabra y despide al libro dormido. Llamado por
        /// GrimorioDormido.OnChatButtonClicked en SP/host y por el pedido
        /// EcoRed.MsgTomarCaida en MP dedicado.
        /// </summary>
        public static void TomarElLibro(NPC libro, Player p)
        {
            try
            {
                if (Main.netMode == NetmodeID.MultiplayerClient) return;
                if (libro == null || !libro.active || p == null || !p.active) return;

                // LA REVALIDACIÓN DE LA CASA (el chequeo del cliente no
                // basta — un cliente modificado no fabrica grimorios).
                if (p.name != libro.GivenName) return;
                if (TieneGrimorio(p)) return;

                // EL LIBRO NACE DE LA AUTORIDAD (sin drops fantasma): si el
                // inventario no tiene hueco, QuickSpawnItem lo posa en el
                // suelo a sus pies — el pacto nunca pierde el libro.
                p.QuickSpawnItem(p.GetSource_Misc("LaCaida"),
                    ModContent.ItemType<Content.Weapons.GrimoireEternal>(), 1);

                // LA MARCA (la copia de la AUTORIDAD; la del .plr la trae la
                // dádiva EcoRed.MsgTomarCaida en el cliente).
                p.GetModPlayer<Players.ShardPlayer>().CaidaRecibida = true;
                // v6.50.125 — y ESTE mundo queda servido para este nombre
                // (cierra también las pendientes nacidas antes de la lista:
                // mundos viejos con libros tomados hoy no repiten mañana).
                RegistrarCaidaDelMundo(p.name);

                // LA PENDIENTE SE APAGA (el cráter cumple su cuento).
                QuitarPendiente(libro.Center);

                // LA PRIMERA PALABRA — directa a los PENSAMIENTOS (EcoLib,
                // morada): «Por fin.»
                EcoRed.HablarAlPortador(p, "Mods.AethonMod.Caida.PrimerPensamiento",
                    new Color(178, 110, 236), rugido: true, escala: 0.72f, prioridad: true);

                // EL LIBRO SE MARCHA (su cita terminó).
                libro.active = false;
                if (Main.netMode == NetmodeID.Server)
                {
                    Terraria.NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, libro.whoAmI);
                    // LA DÁDIVA viaja al cliente del portador (marca su
                    // .plr + la sensación local del pacto).
                    EcoRed.OtorgarCaida(p);
                    // EL HOST tiene pantalla propia (la dádiva por cable no
                    // vuelve a él: su copia del portador ya quedó marcada
                    // arriba — mismo proceso, mismo Player).
                    if (!Main.dedServ) SensacionDelPacto(p);
                }
                else
                {
                    // SP puro: la sensación es toda local.
                    SensacionDelPacto(p);
                }
            }
            catch { }
        }

        /// <summary>
        /// v6.50.122 — LA DEVORACIÓN DEL DOBLE (la letra: «si tienes uno
        /// en el inventario, el del inventario hace lo mismo que le hace
        /// a los jefes en la primera muerte, se lo come y lo devora como
        /// si fuera un jefe»). La cierra GrimorioDormido a los 200 t del
        /// festín (solo AUTORIDAD): marca el alma (el cielo no vuelve a
        /// insistir), apaga la pendiente, suelta la VOZ del Eterno por
        /// los PENSAMIENTOS (EcoRed) y despide al doble.
        /// </summary>
        public static void DevorarLibroCaido(NPC libro, Player p)
        {
            try
            {
                if (Main.netMode == NetmodeID.MultiplayerClient) return;
                if (libro == null || !libro.active) return;

                // LA PENDIENTE SE APAGA (el doble ya cumplió su cuento).
                QuitarPendiente(libro.Center);

                if (p != null && p.active)
                {
                    // LA MARCA DEL ALMA: el cielo no vuelve a insistir con
                    // quien ya carga su grimorio — la regla 1-por-alma vive
                    // en la posesión Y en la toma… y el devorado TAMBIÉN
                    // cuenta: esta fue SU caída.
                    p.GetModPlayer<Players.ShardPlayer>().CaidaRecibida = true;
                    // v6.50.125 — y ESTE mundo queda servido para este
                    // nombre (el silenciador por-mundo de la letra .125).
                    RegistrarCaidaDelMundo(p.name);

                    // LA VOZ DEL ETERNO — directa a los PENSAMIENTOS
                    // (EcoLib, morada): el libro comenta SU festín.
                    EcoRed.HablarAlPortador(p, "Mods.AethonMod.Caida.Devorado",
                        new Color(178, 110, 236), rugido: true, escala: 0.72f, prioridad: true);
                }

                // LA SENSACIÓN DEL TRAGO (la máquina con pantalla).
                if (!Main.dedServ)
                {
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item113.WithPitchOffset(-0.45f), libro.Center);
                    for (int i = 0; i < 22; i++)
                    {
                        Dust d = Dust.NewDustPerfect(libro.Center, DustID.Shadowflame,
                            new Vector2(Main.rand.NextFloat(-2.6f, 2.6f),
                                Main.rand.NextFloat(-3.4f, 0.4f)), 160,
                            new Color(110, 40, 170), 0.9f);
                        d.noGravity = true;
                    }
                }

                // EL DOBLE SE MARCHA (su cita terminó en la boca del Eterno).
                libro.active = false;
                if (Main.netMode == NetmodeID.Server)
                {
                    Terraria.NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, libro.whoAmI);
                    // LA MARCA del .plr del cliente (la dádiva por cable —
                    // el trago también se siente allá).
                    if (p != null && p.active)
                        EcoRed.OtorgarCaida(p);
                }
            }
            catch { }
        }

        /// <summary>
        /// v6.50.117 — LA MANO EXTENDIDA (MP dedicado): el cliente del
        /// destinatario clicó «Extender la mano» — este es el brazo server
        /// de EcoRed.MsgTomarCaida. Busca SU libro (por nombre, cerca de
        /// él) y corre la toma completa de la AUTORIDAD.
        /// </summary>
        public static void AtenderManoExtendida(Player p)
        {
            try
            {
                if (Main.netMode == NetmodeID.MultiplayerClient) return;
                if (p == null || !p.active || string.IsNullOrEmpty(p.name)) return;

                // SU libro: el Grimorio Dormido que porta su nombre, el más
                // cercano (estaba hablando con él — a un paso del clic).
                int tipo = ModContent.NPCType<NPCs.GrimorioDormido>();
                NPC libro = null;
                float mejor = float.MaxValue;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n == null || !n.active || n.type != tipo) continue;
                    if (n.GivenName != p.name) continue;
                    float d = n.DistanceSQ(p.Center);
                    if (d < mejor) { mejor = d; libro = n; }
                }
                if (libro == null || mejor > 2600f * 2600f) return;

                TomarElLibro(libro, p);
            }
            catch { }
        }

        /// <summary>Apaga la pendiente cuyo libro vive cerca de este punto (la toma consumó la caída).</summary>
        public static void QuitarPendiente(Vector2 punto)
        {
            try
            {
                var sys = ModContent.GetInstance<LaCaidaSistema>();
                if (sys == null) return;
                for (int i = sys.CaidasPendientes.Count - 1; i >= 0; i--)
                {
                    var c = sys.CaidasPendientes[i];
                    if (c == null) { sys.CaidasPendientes.RemoveAt(i); continue; }
                    if (Math.Abs(c.X - punto.X) <= 64f && Math.Abs(c.Y - punto.Y) <= 64f)
                        sys.CaidasPendientes.RemoveAt(i);
                }
            }
            catch { }
        }

        /// <summary>v6.50.125 — Registra que ESTE mundo ya le dio su caída a
        /// este nombre (lo llaman el disparo, la toma y la devoración — la
        /// última cierra también los mundos viejos cuyas pendientes nacieron
        /// antes de esta lista).</summary>
        public static void RegistrarCaidaDelMundo(string nombre)
        {
            try
            {
                if (string.IsNullOrEmpty(nombre)) return;
                var sys = ModContent.GetInstance<LaCaidaSistema>();
                if (sys != null) sys._cayoEnEsteMundo.Add(nombre);
            }
            catch { }
        }

        // ================================================================
        //  v6.50.128 — LOS ALTARES DEL MUNDO (la puerta de la corrupción)
        // ================================================================

        /// <summary>Registra el altar nacido en un cráter (lo llama el
        /// cometa al forjarlo — solo AUTORIDAD; la lista viaja con el
        /// guardado del mundo). Sin duplicados: si YA hay un altar a menos
        /// de 3 tiles, el cielo no insiste.
        /// v6.50.130 — <paramref name="colocada"/>: TRUE cuando es una
        /// BRUMA COLOCADA por el jugador (su zona vive y muere con la
        /// tile — la letra: «colocarla donde quieran… altar principal»).
        /// Devuelve el altar registrado (o el que ya vivía ahí).</summary>
        public static AltarDelMundo RegistrarAltar(int x, int y, bool colocada = false)
        {
            try
            {
                var sys = ModContent.GetInstance<LaCaidaSistema>();
                if (sys == null) return null;
                if (x < 10 || y < 10 || x >= Main.maxTilesX - 10 || y >= Main.maxTilesY - 10)
                    return null;
                var existente = sys.Altares.Find(a => a != null &&
                    Math.Abs(a.X - x) <= 3 && Math.Abs(a.Y - y) <= 3);
                if (existente != null) return existente;
                var nuevo = new AltarDelMundo { X = x, Y = y, Colocada = colocada, BrumaEntregada = colocada };
                sys.Altares.Add(nuevo);
                // v6.50.129 — LA BRUMA CAMINA: los clientes MP descubrían
                // las zonas por la TILE DORADA (murió con el altar) — la
                // lista nueva viaja al aire AHORA (y la foto de entrada
                // la llevará a quien entre después).
                if (Main.netMode == NetmodeID.Server)
                    EcoRed.DifundirZonas();
                return nuevo;
            }
            catch { return null; }
        }

        /// <summary>v6.50.130 — LA BRUMA SE RECOGE: el jugador quitó SU
        /// bruma colocada (picó la tile) — la zona se va con ella y la
        /// lista nueva viaja al aire (solo AUTORIDAD difunde; en cliente
        /// la lista local es teatro hasta la próxima foto del server).</summary>
        public static void RemoverAltar(int x, int y)
        {
            try
            {
                var sys = ModContent.GetInstance<LaCaidaSistema>();
                if (sys == null) return;
                int antes = sys.Altares.Count;
                sys.Altares.RemoveAll(a => a != null && a.Colocada &&
                    Math.Abs(a.X - x) <= 2 && Math.Abs(a.Y - y) <= 2);
                if (sys.Altares.Count != antes && Main.netMode == NetmodeID.Server)
                    EcoRed.DifundirZonas();
            }
            catch { }
        }

        /// <summary>v6.50.130 — EL CRÁTER SUELTA SU BRUMA (la letra: «en el
        /// crater debe estar un item invisible que es la bruma en si de
        /// esa forma los jugadores pueden tomarla»): el ITEM DE BRUMA
        /// nace en el corazón del minibioma — una sola vez por cráter
        /// (BrumaEntregada). Solo AUTORIDAD.</summary>
        public static void EntregarBruma(int x, int y)
        {
            try
            {
                if (Main.netMode == NetmodeID.MultiplayerClient) return;
                var sys = ModContent.GetInstance<LaCaidaSistema>();
                if (sys == null) return;

                var altar = sys.Altares.Find(a => a != null &&
                    Math.Abs(a.X - x) <= 3 && Math.Abs(a.Y - y) <= 3);
                if (altar == null || altar.Colocada || altar.BrumaEntregada) return;

                // ¿Ya hay UNA bruma tirada en este cráter? (la red de
                // seguridad del respawn — jamás dos).
                int tipoBruma = ModContent.ItemType<global::AethonMod.Content.Items.Altar.BrumaDelCrater>();
                for (int i = 0; i < Main.maxItems; i++)
                {
                    Item d = Main.item[i];
                    if (d == null || !d.active || d.type != tipoBruma) continue;
                    if (Math.Abs(d.Center.X / 16f - altar.X) <= 10 &&
                        Math.Abs(d.Center.Y / 16f - altar.Y) <= 12)
                    {
                        altar.BrumaEntregada = true;
                        return;
                    }
                }

                altar.BrumaEntregada = true;
                Vector2 pos = altar.Posicion() + new Vector2(0f, -18f);
                int idx = Item.NewItem(new Terraria.DataStructures.EntitySource_WorldGen(),
                    (int)pos.X, (int)pos.Y, 12, 12, tipoBruma, 1, true, -1);
                if (idx >= 0 && Main.netMode == NetmodeID.Server)
                    Terraria.NetMessage.SendData(MessageID.SyncItem, -1, -1, null, idx);
            }
            catch { }
        }

        /// <summary>v6.50.130 — LA BARRIDA DE BRUMAS (al cargar el mundo):
        /// cada cráter que nunca soltó su bruma la suelta AHORA (los
        /// cráteres de la .129 la reciben en su primera carga bajo la
        /// .130 — nadie se queda sin su altar portátil).</summary>
        private static void EntregarBrumasPendientes()
        {
            try
            {
                if (Main.netMode == NetmodeID.MultiplayerClient) return;
                var sys = ModContent.GetInstance<LaCaidaSistema>();
                if (sys == null) return;
                foreach (var a in sys.Altares.ToArray())
                {
                    if (a == null || a.Colocada || a.BrumaEntregada) continue;
                    EntregarBruma(a.X, a.Y);
                }
            }
            catch { }
        }

        /// <summary>v6.50.130 — LA VIGILANCIA DE LAS BRUMAS COLOCADAS (el
        /// pulso de cada 10 s, solo AUTORIDAD): una bruma colocada cuya
        /// tile YA NO EXISTE (explosión, mundo raro, el picoteo que SP
        /// no vio) entrega su zona al olvido — la lista camina limpia.
        /// (Los altares de CRÁTER no se tocan: su puerta es la bruma del
        /// minibioma, no una tile.)</summary>
        public static void ValidarBrumasColocadas()
        {
            try
            {
                if (Main.netMode == NetmodeID.MultiplayerClient) return;
                var sys = ModContent.GetInstance<LaCaidaSistema>();
                if (sys == null || sys.Altares.Count == 0) return;

                int tipoBruma = ModContent.TileType<global::AethonMod.Content.Tiles.BrumaAltarTile>();
                bool toco = false;
                foreach (var a in sys.Altares.ToArray())
                {
                    if (a == null || !a.Colocada) continue;
                    if (a.X < 10 || a.Y < 10 || a.X >= Main.maxTilesX - 10 || a.Y >= Main.maxTilesY - 10)
                    { sys.Altares.Remove(a); toco = true; continue; }
                    Tile t = Main.tile[a.X, a.Y];
                    if (t == null || !t.HasTile || t.TileType != tipoBruma)
                    {
                        sys.Altares.Remove(a);
                        toco = true;
                    }
                }
                if (toco && Main.netMode == NetmodeID.Server)
                    EcoRed.DifundirZonas();
            }
            catch { }
        }

        /// <summary>v6.50.130 — EL DESCUBRIMIENTO DE LAS BRUMAS COLOCADAS
        /// (la colocación sin hook): cada 30 t la AUTORIDAD escanea la
        /// ventana alrededor de cada jugador vivo (±40×±25 tiles — la
        /// colocación SIEMPRE pasa cerca de alguien) y registra las
        /// tiles de bruma que aún no viven en la lista (RegistrarAltar
        /// difunde solo — la zona nueva CAMINA a todas las máquinas).
        /// Cubre SP, listen-server y dedicado con EL MISMO código: sin
        /// paquetes, sin hooks, sin casos.</summary>
        public static void DescubrirBrumasColocadas()
        {
            try
            {
                if (Main.netMode == NetmodeID.MultiplayerClient) return;
                var sys = ModContent.GetInstance<LaCaidaSistema>();
                if (sys == null) return;

                int tipoBruma = ModContent.TileType<global::AethonMod.Content.Tiles.BrumaAltarTile>();
                bool toco = false;

                for (int pj = 0; pj < Main.maxPlayers; pj++)
                {
                    Player p = Main.player[pj];
                    if (p == null || !p.active || p.dead) continue;

                    int cx = (int)(p.Center.X / 16f);
                    int cy = (int)(p.Center.Y / 16f);
                    int x0 = Math.Max(10, cx - 40), x1 = Math.Min(Main.maxTilesX - 11, cx + 40);
                    int y0 = Math.Max(10, cy - 25), y1 = Math.Min(Main.maxTilesY - 11, cy + 25);

                    for (int x = x0; x <= x1; x++)
                    {
                        for (int y = y0; y <= y1; y++)
                        {
                            Tile t = Main.tile[x, y];
                            if (t == null || !t.HasTile || t.TileType != tipoBruma) continue;

                            // ¿Ya vive en la lista? (cráter o colocada —
                            // cualquiera de las dos cuenta: la tile ya
                            // tiene SU zona).
                            if (sys.Altares.Exists(a => a != null &&
                                Math.Abs(a.X - x) <= 1 && Math.Abs(a.Y - y) <= 1)) continue;

                            var nueva = RegistrarAltar(x, y, colocada: true);
                            if (nueva != null) toco = true;
                        }
                    }
                }

                // (RegistrarAltar ya difunde en el server — aquí nada más.)
                if (toco && Main.netMode == NetmodeID.Server)
                    EcoRed.DifundirZonas();
            }
            catch { }
        }

        /// <summary>v6.50.129 — LA LIMPIEZA DEL ALTAR DORADO + EL
        /// BACKFILL (la letra: «quita eso amarillo que esta dentro del
        /// crater» + «el centro debe estar vacio solo obsidiana y ademas
        /// bruma»): (1) los mundos de la .128 tienen la TILE DORADA, su
        /// pedestal y los COLMILLOS DE LA GARGANTA en el fondo de cada
        /// cráter registrado — el minibioma LOS BARRE al cargar (la tile
        /// dorada muere; el pedestal se queda: es obsidiana, el piso;
        /// los colmillos del centro caen: «el centro debe estar VACÍO»).
        /// La ZONA sigue VIVA — la puerta ahora es la BRUMA, no un
        /// bloque. (2) cada caída pendiente de un mundo VIEJO sin zona
        /// (cráteres .127) la recibe — sin tile, solo el registro (la
        /// mecánica no se le niega a nadie). Solo AUTORIDAD.</summary>
        private static void ForjarAltaresFaltantes()
        {
            try
            {
                if (Main.netMode == NetmodeID.MultiplayerClient) return;
                var sys = ModContent.GetInstance<LaCaidaSistema>();
                if (sys == null) return;

                // === (1) LA LIMPIEZA: nada dorado, centro vacío ===
                int tipoAltar = ModContent.TileType<global::AethonMod.Content.Tiles.AltarDeLaCaida>();
                foreach (var altar in sys.Altares.ToArray())
                {
                    if (altar == null) continue;
                    bool toco = false;
                    for (int dx = -3; dx <= 3; dx++)
                    {
                        for (int dy = -9; dy <= 3; dy++)
                        {
                            int x = altar.X + dx, y = altar.Y + dy;
                            if (x < 10 || y < 10 || x >= Main.maxTilesX - 10 ||
                                y >= Main.maxTilesY - 10) continue;
                            Tile t = Main.tile[x, y];
                            if (t == null || !t.HasTile) continue;

                            // LA TILE DORADA: muere (nada amarillo en el cráter).
                            if (t.TileType == tipoAltar)
                            {
                                WorldGen.KillTile(x, y, noItem: true);
                                toco = true;
                                continue;
                            }

                            // LOS COLMILLOS DE LA GARGANTA (.128: columnas
                            // de obsidiana ±2/±3 SOBRE el fondo): caen — el
                            // centro queda vacío para la bruma. El PEDERAL
                            // (dy ≥ 0: el piso de obsidiana) se queda.
                            if (dy <= -2 && t.TileType == TileID.Obsidian)
                            {
                                WorldGen.KillTile(x, y, noItem: true);
                                toco = true;
                            }
                        }
                    }
                    if (toco && Main.netMode == NetmodeID.Server)
                        NetMessage.SendTileSquare(-1, altar.X - 4, altar.Y - 10, 9, 15);
                }

                // === (2) EL BACKFILL: cráteres .127 sin zona la reciben ===
                foreach (var pendiente in sys.CaidasPendientes)
                {
                    if (pendiente == null || string.IsNullOrEmpty(pendiente.Jugador)) continue;
                    int cx = (int)(pendiente.X / 16f);
                    int cy = (int)(pendiente.Y / 16f);
                    if (cx < 12 || cy < 12 || cx >= Main.maxTilesX - 12 || cy >= Main.maxTilesY - 12) continue;

                    // ¿El cráter ya tiene SU zona? (la lista manda).
                    if (sys.Altares.Exists(a => a != null && Math.Abs(a.X - cx) <= 6 && Math.Abs(a.Y - cy) <= 12))
                        continue;

                    // EL FONDO DEL CRÁTER: la primera roca viva bajo la caída.
                    int piso = -1;
                    for (int y = cy + 3; y < Math.Min(cy + 14, Main.maxTilesY - 10); y++)
                    {
                        Tile t = Main.tile[cx, y];
                        if (t != null && t.HasTile && Main.tileSolid[t.TileType]) { piso = y - 1; break; }
                    }
                    if (piso < 10) continue;

                    // SOLO EL REGISTRO (v6.50.129: sin pedestal, sin tile —
                    // la bruma es la puerta del minibioma).
                    sys.Altares.Add(new AltarDelMundo { X = cx, Y = piso });
                }
            }
            catch { }
        }

        /// <summary>v6.50.125 — LA PÁGINA EN BLANCO también blanquea el cielo
        /// de ESTE mundo: ¿estaba este nombre entre los que ya vivieron su
        /// caída aquí? (true si había algo que borrar — para el aviso).</summary>
        public static bool OlvidarCaidaDelMundo(string nombre)
        {
            try
            {
                if (string.IsNullOrEmpty(nombre)) return false;
                var sys = ModContent.GetInstance<LaCaidaSistema>();
                return sys != null && sys._cayoEnEsteMundo.Remove(nombre);
            }
            catch { return false; }
        }

        /// <summary>La sensación del pacto (la máquina del portador): el sonido del libro + su bruma violeta.</summary>
        private static void SensacionDelPacto(Player p)
        {
            if (Main.dedServ || p == null) return;
            try
            {
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item113.WithPitchOffset(-0.3f), p.Center);
                for (int i = 0; i < 18; i++)
                {
                    Dust d = Dust.NewDustPerfect(p.Center, DustID.PurpleTorch,
                        new Vector2(Main.rand.NextFloat(-2.2f, 2.2f),
                            Main.rand.NextFloat(-3f, 0.6f)), 180,
                        new Color(150, 70, 220), 0.85f);
                    d.noGravity = true;
                }
            }
            catch { }
        }

        /// <summary>La dádiva llegó al cliente (EcoRed.MsgTomarCaida): marca local del .plr + sensación.</summary>
        public static void RecibirDadiva()
        {
            try
            {
                var sp = Main.LocalPlayer?.GetModPlayer<Players.ShardPlayer>();
                if (sp != null) sp.CaidaRecibida = true;
                SensacionDelPacto(Main.LocalPlayer);
            }
            catch { }
        }
    }
}
