using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using AethonMod.Content.Globals;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// GrimorioFuriaSistema — LA FURIA DEL GRIMORIO: el evento de las
    /// OLEADAS DE HAMBRE.
    ///
    /// EL CUENTO ENTERO:
    /// 1. LA VOZ DEL HAMBRE (ShardPlayer): con el libro a nivel alto
    ///    (25+), cada 75 segundos sin matar es un MOMENTO DE HAMBRE —
    ///    EcoLib susurra ("El grimorio tiene hambre…") y la barra dorada
    ///    palidece un poco más. Pure flavor, cero mecánica… hasta que no.
    /// 2. LA FURIA: al cuarto momento (~5 minutos sin comer), el libro
    ///    pierde la paciencia: "El grimorio está furioso…" y luego
    ///    "El grimorio llama a su comida…".
    /// 3. EL EVENTO (5 minutos de oleadas): UNA OLEADA POR CADA MOMENTO
    ///    DE HAMBRE acumulado, hasta 10 (hambriento durante una pelea de
    ///    jefes = más hambres acumuladas = más oleadas). Cada oleada:
    ///    - La CHUSMA: monstruos del bioma Y de la hora (noche = ojos y
    ///      zombis, día = babosas…) con stats enfurecidas (vida, daño,
    ///      defensa, sin knockback) y empuje hacia la presa (OleadaNPC).
    ///    - AL FINAL DE CADA OLEADA: UN JEFE PRE-HARDMODE del bioma y la
    ///      hora — superficie día: Rey Gelatina · noche: Ojo de Cthulhu ·
    ///      nieve: Deerclops · jungla: Abeja Reina · corrupción:
    ///      Devorador de Mundos · carmesí: Cerebro · mazmorra: Skeletron
    ///      (noche) · infierno: el Ojo los caza. Versión ESPECIAL: vida,
    ///      daño y defensa potenciados por la oleada (OleadaNPC.Marcar
    ///      con jefe=true).
    ///    - LA XP: TODO lo que muera en la oleada k paga ×(k+1) — la
    ///      oleada 1 paga ×2, la 10 paga ×11 (GlobalNPCXP).
    ///    - EL AURA: cada criatura y jefe viste el humo gris-blanco de
    ///      AuraLib (capa trasera + velo frontal al 94%); en la OLEADA 10
    ///      el aura se pudre: gris-negra con bordes rojo oscuro.
    /// 4. EL SITIO: el reloj de las oleadas dura 5 MINUTOS repartidos
    ///    entre las N oleadas (18000/N ticks cada fase de chusma); los
    ///    jefes paran el reloj (la oleada k+1 empieza cuando cae el
    ///    jefe k). Al final: "El grimorio está saciado… por ahora." y la
    ///    hambre del portador se perdona.
    ///
    /// SP-FIRST: la máquina corre en el servidor del mundo (en SP es el
    /// mismo proceso — las voces de EcoLib y los chat funcionan); en MP
    /// dedicado las voces son TODO pendiente como el resto del sync de
    /// la casa. La Carnada del Grimorio (ítem de prueba) dispara esto
    /// SIEMPRE, aunque la config tenga el hambre automática apagada.
    /// </summary>
    public class GrimorioFuriaSistema : ModSystem
    {
        // === LAS FASES DEL EVENTO ===
        private enum Fase { Inactivo, Llamada, Monstruos, Jefe, Interludio, Fin }

        // === EL ESTADO (servidor del mundo; SP = el mismo proceso) ===
        private static Fase _fase = Fase.Inactivo;
        private static int _ticksFase = 0;
        private static int _oleadasTotales = 0;    // N (1..10)
        private static int _oleadaActual = 0;      // k (1..N)
        private static int _ticksOleada = 0;       // reloj de la fase de chusma
        private static int _pulsoSpawn = 0;        // tempo entre escupitajos
        private static int _bossIdx = -1;          // whoAmI del jefe de la oleada
        private static int _jugador = -1;          // whoAmI del hambriento

        // === LAS CONSTANTES DE LA CASA ===
        /// <summary>El evento completo dura 5 MINUTOS de oleadas (300 s).</summary>
        public const int TicksEvento = 18000;
        /// <summary>El jefe de la oleada tiene 90 s para caer antes de hundirse.</summary>
        public const int TicksJefeMax = 5400;
        /// <summary>La llamada dramática inicial (las dos voces).</summary>
        public const int TicksLlamada = 200;
        /// <summary>El respiro entre oleadas.</summary>
        private const int TicksInterludio = 90;
        /// <summary>Tope de chusma viva por oleada: 8 + 2k.</summary>
        private const int VivosBase = 8;

        /// <summary>¿El evento de las oleadas está corriendo ahora?</summary>
        public static bool Activo => _fase != Fase.Inactivo && _fase != Fase.Fin;
        /// <summary>La oleada actual (0 si no hay evento).</summary>
        public static int OleadaActual => Activo ? _oleadaActual : 0;

        // ==================================================================
        //  EL DISPARO
        // ==================================================================

        /// <summary>
        /// ¿Está el mundo LIBRE para el festín? (un jefe vivo o una
        /// invasión en marcha BLOQUEAN la furia — y el libro sigue
        /// acumulando hambres hasta 10 mientras espera su turno: así se
        /// alcanzan las 10 oleadas naturales).
        /// </summary>
        public static bool MundoLibre()
        {
            if (Main.invasionType != 0) return false;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n != null && n.active && n.boss) return false;
            }
            return true;
        }

        /// <summary>
        /// LA FURIA: arranca el evento con N oleadas (el número de
        /// momentos de hambre, 1..10). La llama ShardPlayer cuando el
        /// libro cruza el umbral — y la Carnada del Grimorio para las
        /// pruebas (salta la config: el cebo es la herramienta de test).
        /// Corre en el servidor del mundo (SP = el mismo proceso).
        /// </summary>
        public static void Provocar(Player jugador, int oleadas)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return; // el servidor manda
            if (Activo) return;                                      // un festín a la vez
            if (jugador == null || !jugador.active) return;

            _oleadasTotales = (int)MathHelper.Clamp(oleadas, 1, 10);
            _oleadaActual = 0;
            _jugador = jugador.whoAmI;
            _fase = Fase.Llamada;
            _ticksFase = 0;

            // LAS DOS VOCES: primero la ira, después la llamada. Solo en
            // el proceso que TIENE pantalla (SP: aquí mismo).
            if (Main.netMode != NetmodeID.Server && jugador.whoAmI == Main.myPlayer)
            {
                EcoLib.Hablar(Language.GetTextValue("Mods.AethonMod.Eco.Furia.Ira"),
                    new Color(168, 96, 60), rugido: true);
                EcoLib.Hablar(Language.GetTextValue("Mods.AethonMod.Eco.Furia.Llamada"),
                    new Color(226, 64, 64), rugido: true);
            }
        }

        // ==================================================================
        //  LA MÁQUINA (PostUpdateWorld: el reloj del mundo)
        // ==================================================================

        public override void PostUpdateWorld()
        {
            if (_fase == Fase.Inactivo) return;
            try { Paso(); }
            catch { Terminar(); }
        }

        private static void Paso()
        {
            _ticksFase++;
            Player hambriento = (_jugador >= 0 && _jugador < Main.maxPlayers) ? Main.player[_jugador] : null;
            if (hambriento == null || !hambriento.active)
            {
                // el portador se fue del mundo: el banquete se cancela
                Terminar();
                return;
            }

            switch (_fase)
            {
                case Fase.Llamada:
                    if (_ticksFase >= TicksLlamada) SiguienteOleada(hambriento);
                    break;

                case Fase.Monstruos:
                    FaseMonstruos(hambriento);
                    break;

                case Fase.Jefe:
                    FaseJefe(hambriento);
                    break;

                case Fase.Interludio:
                    if (_ticksFase >= TicksInterludio) SiguienteOleada(hambriento);
                    break;

                case Fase.Fin:
                    if (_ticksFase >= 90) Terminar();
                    break;
            }
        }

                // ==============================================================
                //  LAS OLEADAS
                // ==============================================================

        private static void SiguienteOleada(Player hambriento)
        {
            _oleadaActual++;
            if (_oleadaActual > _oleadasTotales)
            {
                // EL FINAL: saciedad y perdón
                _fase = Fase.Fin;
                _ticksFase = 0;
                if (Main.netMode != NetmodeID.Server && hambriento.whoAmI == Main.myPlayer)
                    EcoLib.Hablar(Language.GetTextValue("Mods.AethonMod.Eco.Furia.Saciado"),
                        new Color(245, 196, 81), rugido: false);
                // la hambre del portador se perdona: el festín contó
                var sp = hambriento.GetModPlayer<Players.ShardPlayer>();
                sp?.PerdonarHambre();
                return;
            }

            _fase = Fase.Monstruos;
            _ticksFase = 0;
            _ticksOleada = 0;
            _pulsoSpawn = 0;
            _bossIdx = -1;

            // EL ANUNCIO: la oleada k de N (la final se anuncia en negro y rojo)
            if (Main.netMode != NetmodeID.Server)
            {
                if (_oleadaActual == _oleadasTotales && _oleadasTotales >= 5)
                    Main.NewText(Language.GetTextValue("Mods.AethonMod.Furia.OleadaFinal", _oleadaActual, _oleadasTotales),
                        new Color(178, 26, 38));
                else
                    Main.NewText(Language.GetTextValue("Mods.AethonMod.Furia.Oleada", _oleadaActual, _oleadasTotales,
                        _oleadaActual + 1),
                        new Color(198, 200, 206));
            }
        }

        private static void FaseMonstruos(Player hambriento)
        {
            int duracion = Math.Max(600, TicksEvento / _oleadasTotales); // ≥ 10 s por oleada
            _ticksOleada++;

            // === LOS ESCUPITAJOS DE CHUSMA ===
            _pulsoSpawn++;
            if (_pulsoSpawn >= 90) // cada 1.5 s
            {
                _pulsoSpawn = 0;
                int vivos = ContarChusma();
                int tope = VivosBase + 2 * _oleadaActual;
                if (vivos < tope)
                {
                    int porPulso = 2 + (_oleadaActual + 2) / 3; // 3..6
                    int[] pool = PoolMonstruos(hambriento);
                    for (int s = 0; s < porPulso; s++)
                        SpawnMonstruo(hambriento, pool);
                }
            }

            // === ¿SE ACABÓ LA OLEADA? ===
            // Por reloj (su parte de los 5 minutos) o porque la chusma se
            // limpió habiendo pasado al menos media fase.
            bool porReloj = _ticksOleada >= duracion;
            bool limpia = ContarChusma() == 0 && _ticksOleada >= duracion / 2;
            if (porReloj || limpia)
            {
                _fase = Fase.Jefe;
                _ticksFase = 0;
                _bossIdx = -1;
                SpawnJefeOleada(hambriento);
            }
        }

        private static void FaseJefe(Player hambriento)
        {
            NPC jefe = (_bossIdx >= 0 && _bossIdx < Main.maxNPCs) ? Main.npc[_bossIdx] : null;

            // EL JEFE CALLÓ → la oleada se completa
            if (jefe == null || !jefe.active || jefe.life <= 0)
            {
                _fase = Fase.Interludio;
                _ticksFase = 0;
                return;
            }

            // EL JEFE SE CANSÓ: se hunde insatisfecho (90 s) y la furia
            // sigue su curso — 10 jefes vivos a la vez no es un evento,
            // es un dilema de render.
            if (_ticksFase >= TicksJefeMax)
            {
                if (Main.netMode != NetmodeID.Server)
                    Main.NewText(Language.GetTextValue("Mods.AethonMod.Furia.JefeHuido", jefe.FullName),
                        new Color(150, 140, 148));
                jefe.active = false; // despawn limpio (patrón HollowTitan)
                _fase = Fase.Interludio;
                _ticksFase = 0;
            }
        }

        /// <summary>Cuánta chusma de ESTA oleada sigue viva.</summary>
        private static int ContarChusma()
        {
            int vivos = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active) continue;
                var sello = n.GetGlobalNPC<OleadaNPC>();
                if (sello != null && sello.EsDeOleada && !n.boss) vivos++;
            }
            return vivos;
        }

        // ==================================================================
        //  LOS POOLS — el bioma y la hora deciden la comida
        // ==================================================================

        /// <summary>
        /// Los monstruos que el bioma y la hora del portador ofrecen. IDs
        /// verificados contra el Terraria real (sondeo de reflexión).
        /// </summary>
        private static int[] PoolMonstruos(Player p)
        {
            bool noche = !Main.dayTime;

            // === INFIERNO ===
            if (p.ZoneUnderworldHeight)
                return noche
                    ? new int[] { NPCID.Demon, NPCID.FireImp, NPCID.LavaSlime }
                    : new int[] { NPCID.FireImp, NPCID.LavaSlime, NPCID.Demon };

            // === MAZMORRA ===
            if (p.ZoneDungeon)
                return new int[] { NPCID.AngryBones, NPCID.DungeonSlime, NPCID.BlazingWheel };

            // === CAVERNAS DE GRANITO/MÁRMOL ===
            if (p.ZoneGranite)
                return new int[] { NPCID.GraniteGolem, NPCID.GraniteFlyer };
            if (p.ZoneMarble)
                return new int[] { NPCID.GreekSkeleton, NPCID.Medusa, NPCID.GreekSkeleton };

            // === NIEVE ===
            if (p.ZoneSnow)
                return new int[] { NPCID.IceSlime, NPCID.SnowFlinx, NPCID.ZombieEskimo };

            // === JUNGLA ===
            if (p.ZoneJungle)
                return new int[] { NPCID.JungleSlime, NPCID.JungleBat, NPCID.Hornet };

            // === CORRUPCIÓN / CARMESÍ ===
            if (p.ZoneCorrupt)
                return new int[] { NPCID.EaterofSouls, NPCID.EaterofSouls, NPCID.CorruptSlime };
            if (p.ZoneCrimson)
                return new int[] { NPCID.Crimera, NPCID.FaceMonster, NPCID.BloodCrawler };

            // === DESIERTO ===
            if (p.ZoneDesert)
                return noche
                    ? new int[] { NPCID.SandSlime, NPCID.Antlion, NPCID.Zombie, NPCID.DemonEye }
                    : new int[] { NPCID.SandSlime, NPCID.Antlion, NPCID.Antlion };

            // === PLAYA ===
            if (p.ZoneBeach)
                return new int[] { NPCID.Crab, NPCID.BlueSlime, NPCID.Crab };

            // === CIELO ===
            if (p.ZoneSkyHeight)
                return new int[] { NPCID.Harpy, NPCID.Harpy, NPCID.BlueSlime };

            // === SUBSUELO (roca o tierra, sin bioma especial) ===
            if (p.ZoneRockLayerHeight || p.ZoneDirtLayerHeight)
                return new int[] { NPCID.CaveBat, NPCID.Skeleton, NPCID.BlueSlime };

            // === SUPERFICIE: el día escupe babosas, la noche ojos y zombis ===
            if (noche)
                return new int[] { NPCID.Zombie, NPCID.DemonEye, NPCID.BaldZombie, NPCID.CataractEye };
            return new int[] { NPCID.GreenSlime, NPCID.BlueSlime, NPCID.Zombie, NPCID.PurpleSlime };
        }

        /// <summary>
        /// El jefe PRE-HARDMODE que cierra la oleada según el bioma y la
        /// hora. La superficie de día es del Rey Gelatina y la noche del
        /// Ojo de Cthulhu (lo que pide la letra); el resto de biomas
        /// aportan SU guardián. El Muro de Carne NO está en la lista a
        /// propósito: es la puerta del hardmode y una furia involuntaria
        /// no puede abrir mundos.
        /// </summary>
        private static int JefeDelLugar(Player p)
        {
            bool noche = !Main.dayTime;

            if (p.ZoneUnderworldHeight) return NPCID.EyeofCthulhu; // el ojo los caza en el infierno
            if (p.ZoneDungeon) return noche ? NPCID.SkeletronHead : NPCID.EyeofCthulhu;
            if (p.ZoneSnow) return NPCID.Deerclops;
            if (p.ZoneJungle) return NPCID.QueenBee;
            if (p.ZoneCorrupt) return NPCID.EaterofWorldsHead;
            if (p.ZoneCrimson) return NPCID.BrainofCthulhu;

            // SUBSUELO sin bioma: el mal del mundo (o el Rey, en mundos limpios)
            if (p.ZoneRockLayerHeight || p.ZoneDirtLayerHeight)
                return WorldGen.crimson ? NPCID.BrainofCthulhu : NPCID.EaterofWorldsHead;

            // SUPERFICIE (y desierto, playa, cielo, cualquier techo al aire)
            return noche ? NPCID.EyeofCthulhu : NPCID.KingSlime;
        }

        // ==================================================================
        //  LOS SPAWNS
        // ==================================================================

        /// <summary>Escupe UN monstruo de la oleada alrededor del portador.</summary>
        private static void SpawnMonstruo(Player hambriento, int[] pool)
        {
            try
            {
                int tipo = pool[(int)(Main.rand.NextFloat() * pool.Length) % pool.Length];

                // posición: anillo alrededor del portador (fuera de pantalla a ser posible)
                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                float dist = 380f + Main.rand.NextFloat(320f);
                Vector2 pos = hambriento.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * dist;

                NPC npc = Main.npc[NPC.NewNPC(hambriento.GetSource_FromAI(),
                    (int)pos.X, (int)pos.Y, tipo)];
                if (npc == null || !npc.active) return;

                // los que chocan con tiles necesitan SUELO: buscar hacia abajo
                if (!npc.noTileCollide)
                {
                    Vector2 good = BuscarSuelo(pos);
                    if (good == Vector2.Zero) { npc.active = false; return; } // sin hueco: este no nace
                    npc.position = good - new Vector2(npc.width / 2f, npc.height);
                }
                npc.netUpdate = true;

                // EL SELLO (marca + stats + aura)
                npc.GetGlobalNPC<OleadaNPC>().Marcar(npc, _oleadaActual, jefe: false);
            }
            catch { }
        }

        /// <summary>Convoca al JEFE (versión especial de la oleada) del bioma/hora.</summary>
        private static void SpawnJefeOleada(Player hambriento)
        {
            try
            {
                int tipo = JefeDelLugar(hambriento);
                Vector2 pos = hambriento.Center + new Vector2(
                    hambriento.direction * -560f, -240f); // frente al portador, arriba

                int idx = NPC.NewNPC(hambriento.GetSource_FromAI(), (int)pos.X, (int)pos.Y, tipo);
                NPC jefe = (idx >= 0 && idx < Main.maxNPCs) ? Main.npc[idx] : null;
                if (jefe == null || !jefe.active) return;
                _bossIdx = idx;

                jefe.GetGlobalNPC<OleadaNPC>().Marcar(jefe, _oleadaActual, jefe: true);
                jefe.netUpdate = true;

                if (Main.netMode != NetmodeID.Server)
                    Main.NewText(Language.GetTextValue("Mods.AethonMod.Furia.Jefe", jefe.FullName,
                        _oleadaActual + 1),
                        new Color(226, 64, 64));
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, jefe.Center);
            }
            catch { _bossIdx = -1; }
        }

        /// <summary>
        /// Busca el primer tile SÓLIDO bajo la posición (hasta 50 tiles
        /// abajo); devuelve el punto DE ENCIMA o Vector2.Zero si no hay.
        /// Bordes del mundo respetados (índices clampeados).
        /// </summary>
        private static Vector2 BuscarSuelo(Vector2 pos)
        {
            int x = (int)(pos.X / 16f);
            int y = (int)(pos.Y / 16f);
            if (x < 5 || x >= Main.maxTilesX - 5 || y < 5 || y >= Main.maxTilesY - 5)
                return Vector2.Zero; // fuera del mundo: este no nace
            for (int i = 0; i < 50; i++)
            {
                if (y + i >= Main.maxTilesY - 5) break;
                Tile tile = Main.tile[x, y + i];
                if (tile != null && tile.HasTile && Main.tileSolid[tile.TileType])
                    return new Vector2(pos.X, (y + i) * 16f);
            }
            return Vector2.Zero;
        }

        // ==================================================================
        //  EL FINAL
        // ==================================================================

        private static void Terminar()
        {
            _fase = Fase.Inactivo;
            _oleadasTotales = 0;
            _oleadaActual = 0;
            _bossIdx = -1;
            _jugador = -1;
        }

        public override void OnWorldUnload()
        {
            Terminar();
            AuraLib.Reiniciar(); // los emisores de partículas mueren con el mundo
        }
        public override void Unload()
        {
            Terminar();
            AuraLib.Reiniciar(); // las texturas de ruido también
        }
    }
}
