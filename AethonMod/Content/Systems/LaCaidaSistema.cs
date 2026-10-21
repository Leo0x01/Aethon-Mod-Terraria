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
    //  LA LLEGADA (v6.50.117 «LA CAÍDA» → v6.51.0 «LA LLEGADA») — LA
    //  OBTENCIÓN CANÓNICA DEL GRIMORIO ETERNO.
    //
    //  v6.51.0 — LA LETRA NUEVA: «que el grimorio simplemente caiga sin
    //  dañar el mapa, o sea NO HAY CRÁTERES NI CAÍDA, será algo como un
    //  APARECIMIENTO, en vez de la caída será LA LLEGADA: mucha bruma
    //  hasta el punto de que no se pueda ver cómo el grimorio aparece y
    //  luego la bruma se disipa dejando al grimorio dormido flotando a
    //  la espera del jugador. El jugador toma al grimorio y el lugar
    //  donde estaba queda limpio, sin bruma, sin rastro de cómo llegó».
    //  EL COMETA, EL CRÁTER, EL ALTAR Y LA TILE DE BRUMA MURIERON: la
    //  escena la dirige LlegadaBrumaProjectile (el mar de niebla), el
    //  mapa queda INTACTO y la corrupción vive DESDE EL INVENTARIO (las
    //  recetas de RecetasCorrupcionSistema piden LLEVAR el grimorio).
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
    // EL CIRCUITO COMPLETO (v6.51.0 — SIN CRÁTER):
    //   1. PreUpdateTime (solo AUTORIDAD): cada jugador activo cuya caída
    //      TODAVÍA NO sucedió EN ESTE MUNDO (v6.50.125: la lista
    //      _cayoEnEsteMundo — la marca del .plr CaidaRecibida YA NO calla
    //      al cielo: la ponían la toma de la .117 y la devoración de la
    //      .122 y silenciaba el cometa PARA SIEMPRE y EN TODOS LOS MUNDOS
    //      — el personaje de pruebas jamás volvía a ver el evento, la
    //      letra: «la caída no parece activarse cuando pasa la primera
    //      noche»), sin caída pendiente a su nombre, en noche recién caída
    //      (time > 480) y sin llegada YA esta noche → nace su
    //      LlegadaBrumaProjectile (el mar de bruma) sobre el punto
    //      CERCA DEL JUGADOR (la
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

                // v6.51.0 — LA PURGA DE LA VIEJA ERA: los mundos de la
                // .128-.130 traen altares, tiles doradas, tiles de bruma
                // y colmillos en sus cráteres — TODO se barre al cargar
                // (LA LLEGADA no deja rastro: ni altares, ni brumas
                // colocadas, ni lo amarillo). Las coordenadas viejas se
                // leen SOLO para limpiarlas.
                var viejos = new List<System.ValueTuple<int, int>>();
                foreach (var entrada in tag.GetList<TagCompound>("altares"))
                    viejos.Add(System.ValueTuple.Create(
                        entrada.GetInt("x"), entrada.GetInt("y")));
                PurgarLaViejaEra(viejos);
            }
            catch { }
        }

        public override void OnWorldUnload()
        {
            CaidasPendientes.Clear();
            _cayoEnEsteMundo.Clear();
            _llegoPrimerNpc = false;
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
                    // v6.51.0 — SIN GivenName (el hover dice «El Grimorio
                    // Dormido»): el nombre vive en la PENDIENTE y el
                    // índice en ai[1] (viaja por el SyncNPC).
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

        /// <summary>El Grimorio Dormido vivo cerca de un punto (null si
        /// nadie). v6.51.1 — EL RADAR ANCHO (la cura de los DOS GRIMORIOS:
        /// con la tolerancia vieja de ±64 y el libro flotando a −40−21±6
        /// del punto, el bobbing lo sacaba del radar en algunos ticks — el
        /// reconciliador no lo hallaba y ENGENDRABA OTRO justo al lado:
        /// «en la llegada aparecen 2 grimorios en vez de 1». Ahora el
        /// radar lee ±96/±140: TODO el rango del bobbing, siempre).</summary>
        private static NPC BuscarLibroCerca(float x, float y)
        {
            int tipo = ModContent.NPCType<NPCs.GrimorioDormido>();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || n.type != tipo) continue;
                if (Math.Abs(n.Center.X - x) <= 96f && Math.Abs(n.Center.Y - y) <= 140f)
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

                // EL AMANECER LIMPIA LA NOCHE: una llegada por jugador por noche.
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
                        if (LlegadaEnMarcha(i)) continue;

                        DispararLlegada(p);
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
                        if (LlegadaEnMarcha(i)) continue;

                        DispararLlegada(p);
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
        public static bool LlegadaEnMarcha(int idxJugador)
        {
            if (idxJugador < 0 || idxJugador >= Main.maxPlayers) return false;
            int tipo = ModContent.ProjectileType<Projectiles.LlegadaBrumaProjectile>();
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
                if (LlegadaEnMarcha(p.whoAmI)) return 2;

                DispararLlegada(p);
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

        /// <summary>v6.51.0 — DISPARA LA LLEGADA (la letra: «no hay
        /// cráteres ni caída… mucha bruma hasta el punto de que no se
        /// pueda ver cómo el grimorio aparece»): el MAR DE BRUMA nace
        /// DIRECTO sobre el punto elegido (32–64 tiles a un lado del
        /// destinatario, la superficie de esa columna) — crece 3 s
        /// (nadie ve NADA), al pico engendra al Grimorio Dormido DENTRO
        /// (RegistrarLlegada) y luego se disipa solo. NI UNA TILE se
        /// toca: el mapa queda INTACTO.</summary>
        private static void DispararLlegada(Player p)
        {
            // v6.50.118 — el punto sigue CERCA DEL JUGADOR (la letra): la
            /// bruma abre a la vista de su destinatario.
            Vector2 destino = PuntoDeImpacto(p);

            // EL MAR DE BRUMA nace sobre el punto (la escena entera la
            // dirige LlegadaBrumaProjectile — sin cometa, sin vuelo).
            int idx = Projectile.NewProjectile(new EntitySource_Misc("LaLlegada"),
                destino.X, destino.Y - 60f, 0f, 0f,
                ModContent.ProjectileType<Projectiles.LlegadaBrumaProjectile>(),
                0, 0f, p.whoAmI, 0f, destino.X / 1000f, destino.Y / 1000f);
            if (idx < 0 || idx >= Main.maxProjectiles) return;

            // EL AVISO DEL CIELO — PRIVADO: solo el destinatario siente
            // que el mundo va a cambiar de página (SP: pantalla; MP: su
            // chat).
            EcoRed.AnunciarAlPortador(p, "Mods.AethonMod.Caida.CometaCae",
                new Color(150, 60, 220));
        }

        // ================================================================
        //  EL PICO DE LA BRUMA — la llegada entrega el libro (lo llama la
        //  escena: LlegadaBrumaProjectile al llegar a su momento)
        // ================================================================

        /// <summary>v6.51.0 — Registra LA LLEGADA y engendra al Grimorio
        /// Dormido DENTRO de la bruma (solo AUTORIDAD). EL NOMBRE YA NO
        /// SE ESCRIBE en GivenName (la letra: al pasar el cursor debe
        /// decir «El Grimorio Dormido», no el nombre del jugador): la
        /// identidad del pacto viaja en ai[1] + la pendiente.</summary>
        public static void RegistrarLlegada(Vector2 destino, int idxJugador)
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

                // EL LIBRO FLOTANDO EN SU BRUMA — EXACTO EN EL PUNTO de la
                // pendiente (v6.51.1: la cura de los DOS GRIMORIOS — el
                // libro nace donde el reconciliador lo BUSCA; flota solo:
                // noGravity + noTileCollide + su bobbing propio)… y JAMÁS
                // dos: si YA vive uno cerca, el cielo no insiste.
                if (BuscarLibroCerca(destino.X, destino.Y) != null) return;
                int npcIdx = NPC.NewNPC(new EntitySource_Misc("LaLlegada"),
                    (int)destino.X, (int)destino.Y,
                    ModContent.NPCType<NPCs.GrimorioDormido>(), 0, 0f, idxJugador);
                NPC libro = (npcIdx >= 0 && npcIdx < Main.maxNPCs) ? Main.npc[npcIdx] : null;
                if (libro != null && libro.active)
                {
                    // v6.51.0 — SIN GivenName: el hover dice «El Grimorio
                    // Dormido» (la identidad la lleva ai[1] + la pendiente).
                    libro.netUpdate = true;
                    DifundirLibro(libro);
                }
            }
            catch { }
        }

        /// <summary>v6.51.0 — ¿ES EL DESTINATARIO de este libro? (la
        /// identidad del pacto SIN GivenName: el índice vivo de ai[1]…
        /// o el nombre de la PENDIENTE que espera junto al libro — la
        /// revalidación de la AUTORIDAD para la toma y la devoración).</summary>
        public static bool EsElDestinatario(NPC libro, Player p)
        {
            try
            {
                if (libro == null || !libro.active || p == null || !p.active) return false;
                if (string.IsNullOrEmpty(p.name)) return false;

                // EL ÍNDICE VIVO (ai[1] viaja en el SyncNPC — el
                // reconciliador lo refresca en cada reconexión).
                int idx = (int)libro.ai[1];
                if (idx == p.whoAmI) return true;

                // EL NOMBRE DE LA PENDIENTE (la palabra escrita en el
                // mundo: el libro espera JUNTO a ella).
                var sys = ModContent.GetInstance<LaCaidaSistema>();
                if (sys == null) return false;
                return sys.CaidasPendientes.Exists(c => c != null && c.Jugador == p.name &&
                    Math.Abs(c.X - libro.Center.X) <= 64f &&
                    Math.Abs(c.Y - libro.Center.Y) <= 80f);
            }
            catch { return false; }
        }

        /// <summary>v6.51.0 — ¿EL PODER DEL GRIMORIO YA VIVE EN ESTE
        /// MUNDO? (la puerta de los drops de Esencia Corrupta: «en el
        /// momento en el que el grimorio hace su aparición en el mundo,
        /// su poder se filtra entre las realidades»).</summary>
        public bool ElPoderViveAqui()
        {
            try
            {
                // LA LLEGADA ocurrió aquí (alguien ya la vivió o la está
                // esperando).
                if (_cayoEnEsteMundo.Count > 0) return true;
                foreach (var c in CaidasPendientes)
                    if (c != null && !string.IsNullOrEmpty(c.Jugador)) return true;
                // …o un Grimorio Dormido espera en este mundo.
                int tipo = ModContent.NPCType<NPCs.GrimorioDormido>();
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n != null && n.active && n.type == tipo) return true;
                }
                return false;
            }
            catch { return false; }
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
                // v6.51.0 — la identidad por ai[1]/pendiente (SIN
                // GivenName: el hover dice «El Grimorio Dormido»).
                if (!EsElDestinatario(libro, p)) return;
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

                // LA PENDIENTE SE APAGA (la llegada cumple su cuento).
                QuitarPendiente(libro.Center);

                // v6.51.0 — EL LUGAR QUEDA LIMPIO (la letra: «el lugar
                // donde estaba queda limpio sin bruma sin rastro de cómo
                // llegó»): cualquier bruma de la llegada que aún viva
                // junto al libro se disipa YA.
                DisiparBrumaDeLaLlegada(libro.Center);

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
                    if (!EsElDestinatario(n, p)) continue;   // v6.51.0 — SIN GivenName
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
        //  v6.51.0 — LA PURGA DE LA VIEJA ERA + LA LIMPIEZA DE LA LLEGADA
        // ================================================================

        /// <summary>v6.51.0 — LA PURGA (al cargar mundos de la .128-.130):
        /// en las coordenadas de los VIEJOS ALTARES mueren la TILE DORADA,
        /// LA TILE DE BRUMA y los COLMILLOS del centro (el fondo de
        /// obsidiana se queda: es el suelo ya). Los cráteres abiertos
        /// TAMBIÉN se quedan (la tierra ya movida es historia: LA LLEGADA
        /// no daña NADA NUEVO, pero no rellena el pasado). Solo
        /// AUTORIDAD.</summary>
        private static void PurgarLaViejaEra(List<System.ValueTuple<int, int>> viejos)
        {
            try
            {
                if (Main.netMode == NetmodeID.MultiplayerClient) return;
                if (viejos == null || viejos.Count == 0) return;

                int tipoDorada = ModContent.TileType
                    <global::AethonMod.Content.Tiles.AltarDeLaCaida>();
                int tipoBruma = ModContent.TileType
                    <global::AethonMod.Content.Tiles.BrumaAltarTile>();

                foreach (var (x, y) in viejos.ToArray())
                {
                    if (x < 10 || y < 10 || x >= Main.maxTilesX - 10 ||
                        y >= Main.maxTilesY - 10) continue;
                    bool toco = false;
                    for (int dx = -3; dx <= 3; dx++)
                    {
                        for (int dy = -9; dy <= 3; dy++)
                        {
                            int tx = x + dx, ty = y + dy;
                            if (tx < 10 || ty < 10 || tx >= Main.maxTilesX - 10 ||
                                ty >= Main.maxTilesY - 10) continue;
                            Tile t = Main.tile[tx, ty];
                            if (t == null || !t.HasTile) continue;

                            // LA TILE DORADA Y LA TILE DE BRUMA: mueren SIN
                            // soltar nada (la bruma colocada era un
                            // vestigio de la vieja caída).
                            if (t.TileType == tipoDorada || t.TileType == tipoBruma)
                            {
                                WorldGen.KillTile(tx, ty, noItem: true);
                                toco = true;
                                continue;
                            }

                            // LOS COLMILLOS DE LA GARGANTA (.128: columnas
                            // de obsidiana SOBRE el fondo): caen — el
                            // centro queda vacío. El PEDERAL (dy ≥ 0: el
                            // piso de obsidiana) se queda.
                            if (dy <= -2 && t.TileType == TileID.Obsidian)
                            {
                                WorldGen.KillTile(tx, ty, noItem: true);
                                toco = true;
                            }
                        }
                    }
                    if (toco && Main.netMode == NetmodeID.Server)
                        NetMessage.SendTileSquare(-1, x - 4, y - 10, 9, 15);
                }
            }
            catch { }
        }

        /// <summary>v6.51.0 — LA BRUMA DE LA LLEGADA SE DISIPA: la toma del
        /// libro limpia su sitio («sin bruma sin rastro»).</summary>
        private static void DisiparBrumaDeLaLlegada(Vector2 punto)
        {
            try
            {
                int tipo = ModContent.ProjectileType<Projectiles.LlegadaBrumaProjectile>();
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile pr = Main.projectile[i];
                    if (pr == null || !pr.active || pr.type != tipo) continue;
                    if (pr.Center.DistanceSQ(punto) > 400f * 400f) continue;
                    pr.Kill();
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
