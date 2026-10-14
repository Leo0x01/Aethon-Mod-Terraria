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
    // «el cometa aterriza lejos del spawn (~30 pantallas) con cráter y
    // bruma». LA PURGA DEL GÉNESIS mató el altar y su fragmento: ESTE es
    // el camino del libro ahora — el día 1, la primera noche avanzada.
    //
    // EL CIRCUITO COMPLETO:
    //   1. PreUpdateTime (solo AUTORIDAD): cada jugador activo SIN
    //      Grimorio del Eterno en los 59 slots, SIN la marca
    //      ShardPlayer.CaidaRecibida y SIN caída pendiente, en NOCHE
    //      avanzada (time > 12000) y sin cometa YA esta noche → nace su
    //      CaidaCometaProjectile apuntando a ~30 pantallas del spawn.
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
            }
            catch { }
        }

        public override void OnWorldUnload()
        {
            CaidasPendientes.Clear();
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
        //  EL CIELO DE LA NOCHE AVANZADA (solo AUTORIDAD)
        // ================================================================

        public override void PreUpdateTime()
        {
            try
            {
                if (Main.gameMenu) return;
                if (Main.netMode == NetmodeID.MultiplayerClient) return;

                // EL AMANECER LIMPIA LA NOCHE: un cometa por jugador por noche.
                bool esNoche = !Main.dayTime;
                if (_eraNoche && !esNoche)
                    Array.Clear(_cayoEstaNoche, 0, _cayoEstaNoche.Length);
                _eraNoche = esNoche;

                // LA NOCHE AVANZADA (time > 12000: pasada la primera mitad
                // de la noche — el cielo ya está negro de verdad).
                if (esNoche && Main.time > 12000.0)
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
                        if (sp == null || sp.CaidaRecibida) continue;
                        if (TieneGrimorio(p)) continue;
                        if (YaEsperaLibro(p.name)) continue;

                        DispararCometa(p);
                        _cayoEstaNoche[i] = true;
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

        /// <summary>El punto de impacto: a ~30 pantallas del spawn, dirección NO vertical, clavado a los límites del mundo y a la superficie.</summary>
        private static Vector2 PuntoDeImpacto()
        {
            Vector2 spawn = new Vector2(Main.spawnTileX * 16f, Main.spawnTileY * 16f);

            // LA DIRECCIÓN: un ángulo NO vertical (±60° del horizonte) y un
            // lado al azar — el cometa puede caer a oriente o a occidente.
            float ang = Main.rand.NextFloat(-1f, 1f) * MathHelper.Pi / 3f;
            float lado = Main.rand.NextBool() ? 1f : -1f;
            Vector2 dir = new Vector2(lado * MathF.Cos(ang), MathF.Sin(ang));
            Vector2 destino = spawn + dir * (30f * 1920f);

            // LOS LÍMITES DEL MUNDO (margen de 200 tiles del borde).
            destino.X = MathHelper.Clamp(destino.X, 200f * 16f, (Main.maxTilesX - 200) * 16f);

            // LA SUPERFICIE: la primera tile SÓLIDA de esa columna, del
            // cielo hacia abajo (la estrella besa la tierra, no la roca);
            // si la columna es aire puro, la altura del spawn la salva.
            destino.Y = spawn.Y;
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
            Vector2 destino = PuntoDeImpacto();

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
