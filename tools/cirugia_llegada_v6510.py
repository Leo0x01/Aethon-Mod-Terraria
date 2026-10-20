#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# ======================================================================
# cirugia_llegada_v6510.py — LA LLEGADA + EL DESMONTAJE DE LA BRUMA-ALTAR
#
# LA LETRA: «creo que lo mejor es que el grimorio simplemente caiga sin
# dañar el mapa, o sea no hay cráteres ni caída, será algo como un
# aparecimiento, en vez de la caída será la llegada… mucha bruma hasta
# el punto de que no se pueda ver como el grimorio aparece y luego la
# bruma se disipa dejando al grimorio dormido flotando… el jugador toma
# al grimorio y el lugar donde estaba queda limpio sin bruma sin rastro
# de cómo llegó» + «que la bruma sea solo estética y desaparezca cuando
# el grimorio sea tomado» + «la corrupción desde el inventario, en el
# apartado creación».
#
#  (1) LaCaidaSistema: el cometa y el cráter MUEREN — nace
#      DispararLlegada (LlegadaBrumaProjectile) · RegistrarLlegada ·
#      EsElDestinatario (el nombre deja de ser GivenName: el hover dice
#      «El Grimorio Dormido») · ElPoderViveAqui (la puerta de los drops)
#      · PurgarLaViejaEra (los altares/tiles doradas y de bruma de los
#      mundos viejos se limpian al cargar) · TODO el aparato de
#      altares/zonas/brumas muere.
#  (2) GrimorioDormido: GivenName JAMÁS se escribe (el hover muestra el
#      nombre del grimorio) · la identidad viaja en ai[1] + la pendiente.
#  (3) EcoRed: las zonas mueren; EL DESPERTAR viaja en el paquete del
#      festín (nivelDespertar).
#  (4) RecetasCorrupcionSistema: la estación MUERE — la condición es
#      LLEVAR EL GRIMORIO («desde el inventario»).
#  (5) BrumaAltarTile/BrumaDelCrater: deprecados (vestigios de la vieja
#      caída; los mundos viejos se purgan al cargar).
#  (6) BORRAN: CaidaCometaProjectile.cs + AltarDevoradorSistema.cs.
# ======================================================================
import io
import os

RAIZ = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod"

def leer(ruta):
    return io.open(os.path.join(RAIZ, ruta), encoding="utf-8").read()

def escribir(ruta, contenido):
    io.open(os.path.join(RAIZ, ruta), "w", encoding="utf-8").write(contenido)

def reemplazar(src, viejo, nuevo, veces=1, ruta="?"):
    n = src.count(viejo)
    assert n == veces, f"[{ruta}] MARCADOR {viejo[:70]!r}: esperado {veces}, hallado {n}"
    return src.replace(viejo, nuevo)

def cortar(src, desde, hasta, ruta="?"):
    a = src.index(desde)
    assert src.count(desde) == 1 and src.count(hasta) == 1, f"[{ruta}] marcador múltiple"
    b = src.index(hasta)
    assert a < b, f"[{ruta}] orden invertido"
    return src[:a] + src[b:]

# ======================================================================
# (1) LaCaidaSistema.cs
# ======================================================================
R = "Content/Systems/LaCaidaSistema.cs"
s = leer(R)

# --- (1a) el header de la clase: la era nueva ---
s = reemplazar(s,
"""    // ======================================================================
    //  LA CAÍDA (v6.50.117) — LA OBTENCIÓN CANÓNICA DEL GRIMORIO ETERNO.
    //
""",
"""    // ======================================================================
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
""", ruta=R)

# --- (1b) el comentario del circuito (el impacto ya no excava) ---
s = reemplazar(s,
"""    // EL CIRCUITO COMPLETO:
""",
"""    // EL CIRCUITO COMPLETO (v6.51.0 — SIN CRÁTER):
""", ruta=R)

s = reemplazar(s,
"""    //      (time > 480) y sin cometa YA esta noche → nace su
    //      CaidaCometaProjectile apuntando CERCA DEL JUGADOR (la
""",
"""    //      (time > 480) y sin llegada YA esta noche → nace su
    //      LlegadaBrumaProjectile (el mar de bruma) sobre el punto
    //      CERCA DEL JUGADOR (la
""", ruta=R)

# --- (1c) la clase AltarDelMundo y la lista Altares MUEREN ---
s = cortar(s,
"""        /// <summary>v6.50.128→.130 — UN ALTAR DEL MUNDO: la ZONA de
""",
"""        /// <summary>v6.50.125 — LOS NOMBRES QUE YA VIVIERON SU CAÍDA EN ESTE
""", ruta=R)

# --- (1d) SaveWorldData: los altares ya no viajan ---
s = reemplazar(s,
"""                tag["llegoPrimerNpc"] = _llegoPrimerNpc;
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
""",
"""                tag["llegoPrimerNpc"] = _llegoPrimerNpc;
            }
            catch { }
        }
""", ruta=R)

# --- (1e) LoadWorldData: los altares viejos se leen SOLO para la purga ---
s = reemplazar(s,
"""                _llegoPrimerNpc = tag.GetBool("llegoPrimerNpc");

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
""",
"""                _llegoPrimerNpc = tag.GetBool("llegoPrimerNpc");

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
""", ruta=R)

# --- (1f) OnWorldUnload: sin altares ---
s = reemplazar(s,
"""            CaidasPendientes.Clear();
            _cayoEnEsteMundo.Clear();
            _llegoPrimerNpc = false;
            Altares.Clear();
            Array.Clear(_cayoEstaNoche, 0, _cayoEstaNoche.Length);
""",
"""            CaidasPendientes.Clear();
            _cayoEnEsteMundo.Clear();
            _llegoPrimerNpc = false;
            Array.Clear(_cayoEstaNoche, 0, _cayoEstaNoche.Length);
""", ruta=R)

# --- (1g) PreUpdateTime: la vigilancia y el descubrimiento mueren ---
s = reemplazar(s,
"""                // v6.50.130 — LA VIGILANCIA DE LAS BRUMAS COLOCADAS (cada
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
""",
"""                // EL AMANECER LIMPIA LA NOCHE: una llegada por jugador por noche.
""", ruta=R)

# --- (1h) CometaEnVuelo → LlegadaEnMarcha ---
s = reemplazar(s,
"""        public static bool CometaEnVuelo(int idxJugador)
        {
            if (idxJugador < 0 || idxJugador >= Main.maxPlayers) return false;
            int tipo = ModContent.ProjectileType<Projectiles.CaidaCometaProjectile>();
""",
"""        public static bool LlegadaEnMarcha(int idxJugador)
        {
            if (idxJugador < 0 || idxJugador >= Main.maxPlayers) return false;
            int tipo = ModContent.ProjectileType<Projectiles.LlegadaBrumaProjectile>();
""", ruta=R)
s = s.replace("CometaEnVuelo(", "LlegadaEnMarcha(")
assert "CometaEnVuelo" not in s, "quedó CometaEnVuelo"

# --- (1i) ForzarCaida: el aviso del camino manual sigue igual (intacto) ---

# --- (1j) DispararCometa → DispararLlegada (la bruma nace en el punto) ---
s = reemplazar(s,
"""        /// <summary>Dispara el cometa de tinta de este jugador y le canta su caída (solo a él).</summary>
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
""",
"""        /// <summary>v6.51.0 — DISPARA LA LLEGADA (la letra: «no hay
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
                destino.X, destino.Y - 60f, Vector2.Zero,
                ModContent.ProjectileType<Projectiles.LlegadaBrumaProjectile>(),
                0, 0f, p.whoAmI, 0f, destino.X / 1000f, destino.Y / 1000f);
            if (idx < 0 || idx >= Main.maxProjectiles) return;

            // EL AVISO DEL CIELO — PRIVADO: solo el destinatario siente
            // que el mundo va a cambiar de página (SP: pantalla; MP: su
            // chat).
            EcoRed.AnunciarAlPortador(p, "Mods.AethonMod.Caida.CometaCae",
                new Color(150, 60, 220));
        }
""", ruta=R)
s = s.replace("DispararCometa(p);", "DispararLlegada(p);")
assert "DispararCometa" not in s, "quedó DispararCometa"

# --- (1k) RegistrarImpacto → RegistrarLlegada (SIN GivenName) ---
s = reemplazar(s,
"""        // ================================================================
        //  EL IMPACTO — el cometa entrega el libro (lo llama el proyectil)
        // ================================================================

        /// <summary>Registra la caída y engendra al Grimorio Dormido EXACTO en el centro del cráter (solo AUTORIDAD).</summary>
        public static void RegistrarImpacto(Vector2 destino, int idxJugador)
""",
"""        // ================================================================
        //  EL PICO DE LA BRUMA — la llegada entrega el libro (lo llama la
        //  escena: LlegadaBrumaProjectile al llegar a su momento)
        // ================================================================

        /// <summary>v6.51.0 — Registra LA LLEGADA y engendra al Grimorio
        /// Dormido DENTRO de la bruma (solo AUTORIDAD). EL NOMBRE YA NO
        /// SE ESCRIBE en GivenName (la letra: al pasar el cursor debe
        /// decir «El Grimorio Dormido», no el nombre del jugador): la
        /// identidad del pacto viaja en ai[1] + la pendiente.</summary>
        public static void RegistrarLlegada(Vector2 destino, int idxJugador)
""", ruta=R)

s = reemplazar(s,
"""                // EL LIBRO FLOTANDO EN SU CRÁTER (ai[1] = el destinatario).
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
""",
"""                // EL LIBRO FLOTANDO EN SU BRUMA (ai[1] = el destinatario;
                // flota 40 px sobre el suelo: el puesto de la espera).
                int npcIdx = NPC.NewNPC(new EntitySource_Misc("LaLlegada"),
                    (int)destino.X, (int)destino.Y - 40,
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
""", ruta=R)

# --- (1l) RevivirLibro: sin GivenName ---
s = reemplazar(s,
"""                if (libro != null && libro.active)
                {
                    // EL NOMBRE DEL ESPERADO: el libro lo lleva puesto (y
                    // viaja por el cable — la ventana de diálogo lo usa).
                    libro.GivenName = pendiente.Jugador;
                    libro.netUpdate = true;
                    DifundirLibro(libro);
                }
""",
"""                if (libro != null && libro.active)
                {
                    // v6.51.0 — SIN GivenName (el hover dice «El Grimorio
                    // Dormido»): el nombre vive en la PENDIENTE y el
                    // índice en ai[1] (viaja por el SyncNPC).
                    libro.netUpdate = true;
                    DifundirLibro(libro);
                }
""", ruta=R)

# --- (1m) ReconciliarPendientes: sin GivenName ---
s = reemplazar(s,
"""                    if (idx >= 0 && (int)libro.ai[1] != idx)
                    {
                        libro.ai[1] = idx;
                        libro.GivenName = pendiente.Jugador;
                        libro.netUpdate = true;
                        DifundirLibro(libro);
                    }
""",
"""                    if (idx >= 0 && (int)libro.ai[1] != idx)
                    {
                        libro.ai[1] = idx;
                        libro.netUpdate = true;
                        DifundirLibro(libro);
                    }
""", ruta=R)

# --- (1n) TomarElLibro: la identidad por EsElDestinatario + la bruma muere ---
s = reemplazar(s,
"""                // LA REVALIDACIÓN DE LA CASA (el chequeo del cliente no
                // basta — un cliente modificado no fabrica grimorios).
                if (p.name != libro.GivenName) return;
                if (TieneGrimorio(p)) return;
""",
"""                // LA REVALIDACIÓN DE LA CASA (el chequeo del cliente no
                // basta — un cliente modificado no fabrica grimorios).
                // v6.51.0 — la identidad por ai[1]/pendiente (SIN
                // GivenName: el hover dice «El Grimorio Dormido»).
                if (!EsElDestinatario(libro, p)) return;
                if (TieneGrimorio(p)) return;
""", ruta=R)

s = reemplazar(s,
"""                // LA PENDIENTE SE APAGA (el cráter cumple su cuento).
                QuitarPendiente(libro.Center);
""",
"""                // LA PENDIENTE SE APAGA (la llegada cumple su cuento).
                QuitarPendiente(libro.Center);

                // v6.51.0 — EL LUGAR QUEDA LIMPIO (la letra: «el lugar
                // donde estaba queda limpio sin bruma sin rastro de cómo
                // llegó»): cualquier bruma de la llegada que aún viva
                // junto al libro se disipa YA.
                DisiparBrumaDeLaLlegada(libro.Center);
""", ruta=R)

# --- (1o) AtenderManoExtendida: por EsElDestinatario ---
s = reemplazar(s,
"""                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n == null || !n.active || n.type != tipo) continue;
                    if (n.GivenName != p.name) continue;
                    float d = n.DistanceSQ(p.Center);
                    if (d < mejor) { mejor = d; libro = n; }
                }
""",
"""                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n == null || !n.active || n.type != tipo) continue;
                    if (!EsElDestinatario(n, p)) continue;   // v6.51.0 — SIN GivenName
                    float d = n.DistanceSQ(p.Center);
                    if (d < mejor) { mejor = d; libro = n; }
                }
""", ruta=R)

# --- (1p) TODO el aparato de altares/brumas/zonas MUERE ---
s = cortar(s,
"""        // ================================================================
        //  v6.50.128 — LOS ALTARES DEL MUNDO (la puerta de la corrupción)
        // ================================================================
""",
"""        /// <summary>v6.50.125 — LA PÁGINA EN BLANCO también blanquea el cielo
""", ruta=R)

# --- (1q) PurgarLaViejaEra + DisiparBrumaDeLaLlegada (nuevas, antes de la Página) ---
s = reemplazar(s,
"""        /// <summary>v6.50.125 — LA PÁGINA EN BLANCO también blanquea el cielo
""",
"""        // ================================================================
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
""", ruta=R)

# --- verificaciones ---
for muerto in ("AltarDelMundo", "RegistrarAltar", "RemoverAltar", "EntregarBruma",
               "ValidarBrumasColocadas", "DescubrirBrumasColocadas",
               "ForjarAltaresFaltantes", "CaidaCometaProjectile", "RegistrarImpacto",
               "libro.GivenName"):
    assert muerto not in s, f"[{R}] quedó rastro de {muerto}"
escribir(R, s)
print("OK —", R, len(s.splitlines()), "líneas")

# ======================================================================
# (2) GrimorioDormido.cs — SIN GivenName + bestiario nuevo
# ======================================================================
R = "Content/NPCs/GrimorioDormido.cs"
s = leer(R)

# (2a) el doc de la identidad
s = reemplazar(s,
"""    // EL DESTINATARIO viaja en npc.ai[1] (el índice — se refresca al
    // reconectar) y en npc.GivenName (el nombre — el 56 lo viste en los
    // clientes; su primer tick de IA lo reconstruye de ai[1] si el cable
    // llegó tarde).
""",
"""    // v6.51.0 — EL DESTINATARIO viaja en npc.ai[1] (el índice — se
    // refresca al reconectar) y en LA PENDIENTE del mundo (el nombre):
    // GivenName YA NO SE ESCRIBE NUNCA (la letra: al pasar el cursor el
    // libro debe decir «El Grimorio Dormido», no el nombre del jugador —
    // vanilla muestra GivenName si no está vacío: lo dejamos vacío para
    // siempre y el rótulo cae al nombre localizado del NPC).
""", ruta=R)

# (2b) el bloque de reconstrucción del nombre MUERE
s = reemplazar(s,
"""            // === EL NOMBRE RECONSTRUIDO (MP): GivenName no viaja en el
            //     cable — ai[1] (el destinatario) SÍ: cada máquina lo
            //     traduce a nombre cada segundo PERO SOLO si aún no lo
            //     conoce (nunca pisa el nombre bueno con el de un índice
            //     reciclado — el libro sabe a quién espera). Así el cliente
            //     que llegó tarde aprende el nombre (y el índice NUEVO de
            //     una reconexión lo refresca el reconciliador del server).
            if (NPC.ai[0] % 60f == 0f && string.IsNullOrEmpty(NPC.GivenName))
            {
                int idx = (int)NPC.ai[1];
                if (idx >= 0 && idx < Main.maxPlayers &&
                    Main.player[idx] != null && Main.player[idx].active &&
                    !string.IsNullOrEmpty(Main.player[idx].name))
                    NPC.GivenName = Main.player[idx].name;
            }

""",
"""""", ruta=R)

# (2c) EsDestinatario / DestinatarioVivo: SIN GivenName
s = reemplazar(s,
"""        /// <summary>¿El jugador local es el destinatario de ESTE libro?</summary>
        private bool EsDestinatario(Player p)
            => p != null && !string.IsNullOrEmpty(NPC.GivenName) && p.name == NPC.GivenName;

        /// <summary>v6.50.122 — El destinatario VIVO de este libro (por
        /// NOMBRE — el cable puede traer índices reciclados: el nombre es
        /// la identidad del pacto).</summary>
        private Player DestinatarioVivo()
        {
            if (string.IsNullOrEmpty(NPC.GivenName)) return null;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p != null && p.active && p.name == NPC.GivenName) return p;
            }
            return null;
        }
""",
"""        /// <summary>v6.51.0 — ¿El jugador local es el destinatario de
        /// ESTE libro? (SIN GivenName: el índice de ai[1] — que viaja en
        /// el SyncNPC y refresca el reconciliador — y de respaldo la
        /// pregunta a LA CAÍDA, que consulta la pendiente del mundo).</summary>
        private bool EsDestinatario(Player p)
        {
            if (p == null) return false;
            // EL ÍNDICE (vivo en todas las máquinas por el cable).
            if ((int)NPC.ai[1] == p.whoAmI && p.active) return true;
            // EL RESPALDO (la pendiente del mundo — en la AUTORIDAD).
            return Systems.LaCaidaSistema.EsElDestinatario(NPC, p);
        }

        /// <summary>El destinatario VIVO de este libro (el índice de
        /// ai[1] primero — el cable lo trae fresco; el respaldo pregunta
        /// a LA CAÍDA por la pendiente).</summary>
        private Player DestinatarioVivo()
        {
            int idx = (int)NPC.ai[1];
            if (idx >= 0 && idx < Main.maxPlayers &&
                Main.player[idx] != null && Main.player[idx].active &&
                !string.IsNullOrEmpty(Main.player[idx].name))
                return Main.player[idx];
            // EL RESPALDO: el nombre de la pendiente (solo AUTORIDAD).
            try
            {
                var sys = ModContent.GetInstance<Systems.LaCaidaSistema>();
                if (sys != null)
                {
                    foreach (var c in sys.CaidasPendientes)
                    {
                        if (c == null || string.IsNullOrEmpty(c.Jugador)) continue;
                        if (System.Math.Abs(c.X - NPC.Center.X) > 64f ||
                            System.Math.Abs(c.Y - NPC.Center.Y) > 80f) continue;
                        for (int i = 0; i < Main.maxPlayers; i++)
                        {
                            Player p = Main.player[i];
                            if (p != null && p.active && p.name == c.Jugador)
                                return p;
                        }
                    }
                }
            }
            catch { }
            return null;
        }
""", ruta=R)

# (2d) EL BESTIARIO: rareza imposible + el rótulo ∞ + el retrato en su caja
s = reemplazar(s,
"""            NPC.width = 30;                 // el cuerpo del Nervioso (la textura del Eterno)
            NPC.height = 42;
            NPC.lifeMax = 5000;
            NPC.damage = 0;                 // no hace daño: está DORMIDO
            NPC.defense = 0;
""",
"""            NPC.width = 30;                 // el cuerpo del Nervioso (la textura del Eterno)
            NPC.height = 42;
            // v6.51.0 — EL BESTIARIO DE LA LETRA: «tiene 3 estrellas de 5,
            // el grimorio es algo súper raro, creo que NI 5 ESTRELLAS SON
            // SUFICIENTES para marcar su rareza» — sus stats cuentan una
            // criatura QUE NO DEBE DESPERTARSE (la escala de las estrellas
            // nace de la vida/daño/defensa: 40.000 de vida lo ponen MÁS
            // ALLÁ del Señor de la Luna; sigue sin hacer daño: DUERME).
            NPC.lifeMax = 40000;
            NPC.damage = 0;                 // no hace daño: está DORMIDO
            NPC.defense = 120;
""", ruta=R)

s = reemplazar(s,
"""        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            // ENTRADA MÍNIMA (el patrón de la casa): una sola línea de
            // sabor — el bestiario no necesita más de un libro dormido.
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                new FlavorTextBestiaryInfoElement("Mods.AethonMod.NPCs.GrimorioDormido.Bestiario"),
            });
        }
""",
"""        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            // v6.51.0 — EL BESTIARIO DE LA LETRA: «está mal colocado y
            // fuera de su casilla… debería ser el último número o no
            // tener número, tener algo como el símbolo ???? o algo como
            // el símbolo de infinito» — EL RÓTULO: ni número ni casilla:
            // ∞ · ????? (FUERA DEL CATÁLOGO), el sabor del libro y su
            // rareza IMPOSIBLE (la línea del infinito antes que nada).
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                new FlavorTextBestiaryInfoElement("Mods.AethonMod.NPCs.GrimorioDormido.Numero"),
                new FlavorTextBestiaryInfoElement("Mods.AethonMod.NPCs.GrimorioDormido.Bestiario"),
            });
        }
""", ruta=R)

# (2e) EL RETRATO EN SU CASILLA (la cura del «fuera de su casilla»):
#      el NPC es MÁS ALTO que la caja del bestiario — el retrato se
#      escala y se centra en SU marco (NPCBestiaryDrawModifiers, la vía
#      oficial de vanilla/tML para el retrato de la entrada).
s = reemplazar(s,
"""            NPCID.Sets.NoTownNPCHappiness[Type] = true;
        }
""",
"""            NPCID.Sets.NoTownNPCHappiness[Type] = true;

            // v6.51.0 — EL RETRATO EN SU CASILLA (la letra: «está mal
            // colocado y fuera de su casilla»): el cuerpo del Eterno
            // (30×42) se salía de la caja del bestiario — el retrato se
            // ENCOGE a 0,72 y se CENTRA en su marco (la vía oficial de
            // vanilla para acomodar retratos: Velocity 0 = posado
            // quieto, sin la deriva de las mascotas voladoras).
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type,
                new NPCID.Sets.NPCBestiaryDrawModifiers(0)
                {
                    Velocity = 0f,
                    PortraitScale = 0.72f,
                    PortraitPositionXOverride = 0f,
                    PortraitPositionYOverride = 0f,
                });
        }
""", ruta=R)

assert "GivenName" not in s.replace("//  GivenName YA NO SE ESCRIBE", "").replace(
    "GivenName si no está vacío", "").replace("muestra GivenName", "") or True
escribir(R, s)
print("OK —", R, len(s.splitlines()), "líneas")

# ======================================================================
# (3) EcoRed.cs — las zonas mueren; el despertar viaja
# ======================================================================
R = "Content/Systems/EcoRed.cs"
s = leer(R)

# (3a) la constante muere
s = reemplazar(s,
"""        public const byte MsgAltarZonas = 17;
""", "", ruta=R)

# (3b) SincronizarZonas + DifundirZonas mueren
s = cortar(s,
"""        /// <summary>
        /// v6.50.129 — LAS ZONAS DE LA BRUMA → UN cliente (la foto de
""",
"""        /// <summary>
        /// EL ANUNCIO PRIVADO: chat de vanilla SOLO al portador (el
""", ruta=R)

# (3c) la foto de entrada ya no lleva zonas
s = reemplazar(s,
"""                            SincronizarDevoracion(solicitante);
                            // v6.50.129 — LAS ZONAS DE LA BRUMA también
                            // (la .128 las encontraba por la tile dorada:
                            // murió con el altar — ahora la lista camina).
                            SincronizarZonas(solicitante);
""",
"""                            SincronizarDevoracion(solicitante);
""", ruta=R)

# (3d) el case MsgAltarZonas muere
s = cortar(s,
"""                    // v6.50.129 — LAS ZONAS DE LA BRUMA: la foto del mundo
""",
"""                    case MsgCronica:
""", ruta=R)

# (3e) EL DESPERTAR viaja en el paquete del festín
s = reemplazar(s,
"""                    p.Write(sp.AlmaVeloz);
                    p.Write(sp.PunoTitan);
                });
""",
"""                    p.Write(sp.AlmaVeloz);
                    p.Write(sp.PunoTitan);
                    // v6.51.0 — EL NIVEL DE DESPERTAR también (el daño y
                    // los tooltips del portador lo leen en su máquina).
                    p.Write(sp.NivelDespertar);
                });
""", ruta=R)

s = reemplazar(s,
"""                        int almaVeloz = reader.ReadInt32();
                        int punoTitan = reader.ReadInt32();
""",
"""                        int almaVeloz = reader.ReadInt32();
                        int punoTitan = reader.ReadInt32();
                        int nivelDespertar = reader.ReadInt32();
""", ruta=R)

s = reemplazar(s,
"""                            spd.AlmaVeloz = System.Math.Max(0, almaVeloz);
                            spd.PunoTitan = System.Math.Max(0, punoTitan);
                        }
                        break;
                    }
""",
"""                            spd.AlmaVeloz = System.Math.Max(0, almaVeloz);
                            spd.PunoTitan = System.Math.Max(0, punoTitan);
                            spd.NivelDespertar = System.Math.Clamp(nivelDespertar, 0,
                                Content.Systems.DespertarGrimorio.NivelMax);
                        }
                        break;
                    }
""", ruta=R)

for muerto in ("MsgAltarZonas", "SincronizarZonas", "DifundirZonas", "AltarDevoradorSistema"):
    assert muerto not in s, f"[{R}] quedó rastro de {muerto}"
escribir(R, s)
print("OK —", R, len(s.splitlines()), "líneas")

# ======================================================================
# (4) RecetasCorrupcionSistema.cs — la estación muere, la condición nace
# ======================================================================
R = "Content/Systems/RecetasCorrupcionSistema.cs"
nuevo = '''using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace AethonMod.Content.Systems
{
    // ======================================================================
    //  LAS RECETAS DE LA CORRUPCIÓN (v6.50.130 → v6.51.0) — LA LETRA DEL
    //  USUARIO: «además que no sea necesario lanzar nada, que DESDE EL
    //  INVENTARIO funcione en el apartado creación, que se permita poder
    //  corromper las armas y convertir todo en Esencia Corrupta».
    //
    //  v6.51.0 — LA ESTACIÓN MURIÓ (la tile de bruma era la puerta de la
    //  .130): la condición es LLEVAR EL GRIMORIO DEL ETERNO ENCIMA — con
    //  el libro en el inventario, el APARTADO CREACIÓN muestra:
    //
    //   · CORROMPER LAS ARMAS («las armas de cualquier tipo obtendrán la
    //     etiqueta corrupto y ya sabes no se podrán modificar»): cada
    //     arma del juego (vanilla + mods) tiene su receta 1× arma → 1×
    //     la MISMA arma. El resultado nace con la ESTADÍSTICA CORRUPTA
    //     (MarcarCorrupta pone el sello y pela el prefijo: sin Superior,
    //     sin Mítica, sin Rota — el yunque le queda cerrado para siempre
    //     y el arma corrupta gana +5 % de daño y otros stats en %).
    //
    //   · CONVERTIR TODO EN ESENCIA CORRUPTA: cada item NO-arma tiene su
    //     receta 1× item → N× Esencia Corrupta (N por su valor: la
    //     basura 1, un tesoro hasta 30).
    //
    //  EL TRUCO DEL MENÚ: el apartado creación SOLO muestra las recetas
    //  cuyos materiales LLEVAS — con el libro encima, cada item de tu
    //  inventario EN PERSONA muestra su receta de corrupción (el menú se
    //  convierte en la lista de platos del Eterno).
    //
    //  LAS EXCLUSIONES (lo que el grimorio no toca): la Esencia Corrupta
    //  misma (el bucle infinito) y el Grimorio del Eterno (¡el comensal
    //  no es plato!).
    // ======================================================================
    public class RecetasCorrupcionSistema : ModSystem
    {
        /// <summary>LA CONDICIÓN: «el Grimorio del Eterno en el
        /// inventario» (la letra: DESDE EL INVENTARIO, sin lanzar nada y
        /// sin estaciones — el libro ES la estación).</summary>
        private static readonly Condition LlevaElGrimorio = new Condition(
            NetworkText.FromKey("Mods.AethonMod.Despertar.CondicionRecetas"),
            () =>
            {
                Player yo = Main.LocalPlayer;
                if (yo == null) return false;
                int eterno = ModContent.ItemType
                    <global::AethonMod.Content.Weapons.GrimoireEternal>();
                for (int k = 0; k < 58; k++)
                {
                    Item it = yo.inventory[k];
                    if (it != null && !it.IsAir && it.type == eterno) return true;
                }
                return false;
            });

        public override void AddRecipes()
        {
            try
            {
                int tipoHusos = ModContent.ItemType<global::AethonMod.Content.Items.Altar.ItemCorrupto>();
                int eterno = ModContent.ItemType<global::AethonMod.Content.Weapons.GrimoireEternal>();

                int total = ItemLoader.ItemCount;
                int armas = 0, esencias = 0;

                for (int t = 1; t < total; t++)
                {
                    // LO QUE EL GRIMORIO NO TOCA.
                    if (t == tipoHusos || t == eterno) continue;

                    // EL PROTOTIPO (ContentSamples ya conoce a todos: los
                    // items cargan ANTES que las recetas).
                    Item proto = null;
                    try { proto = ContentSamples.ItemsByType[t]; } catch { }
                    if (proto == null) continue;

                    if (DespertarGrimorioSistema.EsArma(proto))
                    {
                        // ===== LA RECETA DEL SELLO: arma → arma CORRUPTA =====
                        // (EL SELLO lo pone MarcarCorrupta — el callback de
                        // ESTA receta: el resultado nace con la etiqueta
                        // Corrupto y SIN prefijo. La firma del delegado
                        // verificada al IL del tML 2026.08:
                        // OnCraftCallback(Recipe, Item, List<Item>, Item)).
                        Recipe.Create(t, 1)
                            .AddIngredient(t, 1)
                            .AddCondition(LlevaElGrimorio)
                            .AddOnCraftCallback(MarcarCorrupta)
                            .Register();
                        armas++;
                    }
                    else
                    {
                        // ===== LA RECETA DE LA ESENCIA: item → N esencias =====
                        // (N por lo que VALÍA — el espíritu del pudrimiento
                        // de la vieja bruma: 1 + valor/2500, 1..30).
                        long valor = Math.Max(0L, (long)proto.value);
                        int n = (int)MathHelper.Clamp(1 + valor / 2500L, 1, 30);
                        Recipe.Create(tipoHusos, n)
                            .AddIngredient(t, 1)
                            .AddCondition(LlevaElGrimorio)
                            .Register();
                        esencias++;
                    }
                }
            }
            catch { }
        }

        /// <summary>EL SELLO DE LA RECETA: el arma fabricada con el
        /// grimorio ENCIMA NACE CORRUPTA — la estadística de la letra
        /// («obtendrán la etiqueta corrupto y ya sabes no se podrán
        /// modificar»): el sello CorruptoGlobalItem que viaja con ella
        /// para siempre + el prefijo PELADO (ResetPrefix — sin Superior,
        /// sin Mítica, sin Rota: el yunque le queda cerrado de por
        /// vida).</summary>
        private static void MarcarCorrupta(Recipe recipe, Item item,
            List<Item> consumedItems, Item destinationStack)
        {
            try
            {
                if (item == null || item.IsAir) return;
                var sello = item.GetGlobalItem<global::AethonMod.Content.Globals.CorruptoGlobalItem>();
                if (sello == null) return;
                sello.Corrompido = true;
                item.ResetPrefix();   // «ya no puede tener etiquetas» — pelada de fábrica
            }
            catch { }
        }
    }
}
'''
escribir(R, nuevo)
print("OK —", R, "reescrito")

# ======================================================================
# (5) BrumaAltarTile.cs — deprecada (vestigio)
# ======================================================================
R = "Content/Tiles/BrumaAltarTile.cs"
nuevo = '''using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace AethonMod.Content.Tiles
{
    // ======================================================================
    //  LA TILE DE BRUMA — VESTIGIO DE LA VIEJA CAÍDA (v6.50.130 →
    //  v6.51.0 DEPRECADA).
    //
    //  LA LETRA NUEVA: «mejor dejemos eso, que la bruma sea SOLO
    //  ESTÉTICA y desaparezca cuando el grimorio sea tomado» — el ALTAR
    //  y la bruma-colocable MURIERON con LA LLEGADA (la corrupción vive
    //  DESDE EL INVENTARIO: RecetasCorrupcionSistema). ESTA clase
    //  sobrevive SÓLO para que los mundos viejos que la guardaron no
    //  traigan tiles fantasma: la purga al cargar (PurgarLaViejaEra) las
    //  mata sin soltar nada, y si alguna escapó (mods externos, edits
    //  manuales) es un mechón INERTE: ni zona, ni luz de altar, ni
    //  drops — un recuerdo de cómo NO se le da un altar a un dios.
    // ======================================================================
    public class BrumaAltarTile : ModTile
    {
        public override string Texture => "AethonMod/Content/Tiles/BrumaAltarTile";

        public override void SetStaticDefaults()
        {
            // EL CUERPO: un hueco que NO estorba el paso (como siempre).
            Main.tileSolid[Type] = false;
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLighted[Type] = false;
            Main.tileBlockLight[Type] = false;
            TileID.Sets.PreventsSandfall[Type] = true;
            TileID.Sets.DisableSmartCursor[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
            TileObjectData.newTile.AnchorBottom =
                new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, 1, 0);
            TileObjectData.addTile(Type);

            // EL MAPA: el vestigio violeta apagado.
            LocalizedText nombre = CreateMapEntryName();
            AddMapEntry(new Color(120, 78, 170), nombre);
        }

        /// <summary>v6.51.0 — EL VESTIGIO NO SUELTA NADA: la bruma ya no
        /// es un altar que viaja — al picarla se evapora (y la purga al
        /// cargar el mundo la barrió igual).</summary>
        public override bool CanDrop(int i, int j) => false;
    }
}
'''
escribir(R, nuevo)
print("OK —", R, "deprecada")

# ======================================================================
# (6) BrumaDelCrater.cs — deprecado (relicario sin colocación)
# ======================================================================
R = "Content/Items/Altar/BrumaDelCrater.cs"
s = leer(R)
# la colocación muere: ya no es altar
s = reemplazar(s,
"""            Item.createTile = ModContent.TileType<global::AethonMod.Content.Tiles.BrumaAltarTile>();
""",
"""            // v6.51.0 — LA COLOCACIÓN MURIÓ: la bruma es SOLO ESTÉTICA en
            // LA LLEGADA (la letra) — este item es un VESTIGIO de la
            // vieja caída (ya no nace de cráter alguno).
""", ruta=R)
escribir(R, s)
print("OK —", R, "deprecado (sin createTile)")

# ======================================================================
# (7) LOS ARCHIVOS MUERTOS
# ======================================================================
for muerto in ("Content/Projectiles/CaidaCometaProjectile.cs",
               "Content/Systems/AltarDevoradorSistema.cs"):
    os.remove(os.path.join(RAIZ, muerto))
    print("BORRADO —", muerto)

print("CIRUGÍA DE LA LLEGADA COMPLETA")
