using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Systems
{
    // ======================================================================
    //  EL DESPERTAR DEL GRIMORIO (v6.51.0) — LA NUEVA PROGRESIÓN DEL
    //  ETERNO, DISEÑADA PARA NO ARRUINAR LA DE TERRARIA.
    //
    // LA LETRA DEL USUARIO: «esta nueva mecánica de corrupción funciona de
    // la siguiente forma… esta mecánica desbloquea los poderes del
    // grimorio» · «en vez de ser una mecánica llamada niveles de
    // corrupción… es mejor llamarlo niveles de despertar, así es esta es
    // la mecánica del despertar del grimorio» (el cambio de nombre a
    // último minuto: LORE — el grimorio DESPIERTA, no se corrompe).
    //
    // LA ESTRUCTURA (0..100 — v6.86):
    //  · SIEMPRE EMPIEZA EN NIVEL 0.
    //  · Para subir al nivel N se necesitan TRES ofrendas DEVORADAS
    //    (basta SOLTAR el arma corrupta con el Eterno SELECCIONADO):
    //      1. N Esencias Corruptas (más por nivel — el nivel 1 pide 1).
    //      2. DINERO creciente (10 de cobre en el nivel 1… hasta
    //         1 DE PLATINO en los niveles altos — la letra).
    //      3. UN ARMA CORRUPTA del peldaño de la progresión de Terraria
    //         de ese nivel — o su MINERAL ALTERNATIVO (el nivel 1:
    //         «1 espada corta de cobre corrupta o una espada larga de
    //         cobre» — minerales alternativos, los jefes, las armas
    //         que «a veces aparecen en un mundo sí y otro no»).
    //
    // LOS PODERES (los hitos de la letra — v6.86, redistribuidos en las
    //  CIEN puertas · histórico v6.51 entre paréntesis):
    //   10 (5) → MEJORA 1 de los ojos. 15 (7) → EL SOL RÚNICO COMBINADO
    //       (1 anillo, 2/min). 20 → EL GRIMORIO ESCUPE AL MINIGRIMORIO
    //       (la mascota, con cine — y v6.86: LA PUERTA DEL LIBRO
    //       CORRUPTO: el peldaño 20 exige además devorar 1 Libro
    //       Corrupto — el item raro de la mazmorra).
    //   30 (10) → LOS AGUJEROS NEGROS COMBINADOS (1/min).
    //   40 (15) → MEJORA 2. 50 (17) → el sol con 2 ANILLOS y 3/min.
    //   60 (25) → los agujeros ASCENDIDOS. 70 (30) → MEJORA 3.
    //   75 (35) → el sol de 3 ANILLOS y 4/min. 80 (40) → MEJORA 4.
    //   85 (45) → los agujeros 2/min. 90 (45) → EL SUPREMO/AURORA.
    //   100 (50) → LA IRA DE UN SER EXTERIOR (EL HAZ) — y con la letra
    //       nueva de la cima: Moon Lord vencido ≥ 5 veces y Aethon ≥ 1.
    //  Además CADA NIVEL: +1 % de daño a TODOS los tipos de daño del
    //  grimorio (mágico y de invocación — la letra: «consumir la esencia
    //  corrupta sigue aumentando un poquito el daño en %»).
    // ======================================================================
    public static class DespertarGrimorio
    {
        /// <summary>v6.86 — EL TOPE NUEVO: 100 (la letra: «creo que de
        /// ser necesario aumenta el nivel de Despertar a nivel 100 para
        /// un mejor progreso y reordena los requisitos necesarios para
        /// subir cada nivel de Despertar teniendo en cuenta la progresion
        /// natural de terraria con los items que se puedan obtener
        /// teniendo en cuenta el jefe en cuestion»: la escalera entera
        /// renació alineada — cada arma y cada lingote OBTENIBLE en la
        /// era de su jefe (nada de adamantita con el Rey Gelatina vivo).
        /// Historic: 50 (v6.51.0-v6.85).</summary>
        public const int NivelMax = 100;

        /// <summary>v6.51.6 — EL DESPERTAR ES DEL LIBRO (la letra: «el
        /// despertar se comparte entre todos los grimorios, eso debe ser
        /// algo individual de cada grimorio del eterno»): el nivel vive
        /// EN EL ITEM (AlmaEternoGlobalItem) — esta lectura resuelve el
        /// GRIMORIO ACTIVO del portador (en la MANO primero; si no, el
        /// primero de su inventario).</summary>
        public static int NivelDe(Player p)
        {
            if (p == null || !p.active) return 0;
            return NivelDe(Globals.AlmaEternoGlobalItem.Activo(p));
        }

        /// <summary>v6.51.6 — El nivel de despertar de UN grimorio
        /// concreto (el item manda — cada libro es su propia escalera).</summary>
        public static int NivelDe(Item grimorio)
        {
            return Globals.AlmaEternoGlobalItem.NivelDe(grimorio);
        }

        /// <summary>El nivel SIGUIENTE (el de la ofrenda pendiente): si ya
        /// está en el tope, devuelve el tope (nadie ofrece al vacío).</summary>
        public static int NivelDespertarSiguiente(Player p)
        {
            int nivel = NivelDe(p);
            return nivel >= NivelMax ? NivelMax : nivel + 1;
        }

        /// <summary>v6.51.6 — El nivel siguiente de UN grimorio concreto.</summary>
        public static int NivelDespertarSiguiente(Item grimorio)
        {
            int nivel = NivelDe(grimorio);
            return nivel >= NivelMax ? NivelMax : nivel + 1;
        }

        /// <summary>El daño extra por nivel: +1 % por nivel de despertar a
        /// TODOS los tipos de daño del grimorio (mágico y de invocación).</summary>
        public static float BonusDano(int nivel) => MathF.Max(0, Math.Min(NivelMax, nivel)) * 0.01f;

        // ================================================================
        //  LAS OFRENDAS DEL NIVEL N (esencias · dinero · arma del peldaño)
        // ================================================================

        /// <summary>Las ESENCIAS CORRUPTAS que exige el nivel N (la letra:
        /// «para subir a nivel 1 necesitará haber devorado 1 esencia
        /// corrupta… y así por cada nivel necesita devorar más esencia
        /// corrupta»): el nivel N pide N esencias — 1.275 en toda la
        /// escalera, el banco de las oleadas de hambre).</summary>
        public static int EsenciasPara(int nivel) => Math.Max(1, Math.Min(NivelMax, nivel));

        /// <summary>EL DINERO que exige el nivel N (la letra: «cierta
        /// cantidad de dinero que aumenta por nivel hasta un máximo de 1
        /// de platino en niveles altos» — y el nivel 1 explícito: «10
        /// monedas de cobre»): 10 cobres que CRECEN ×1,33 por nivel y se
        /// TRUNCAN en 1 de platino (100 de oro) desde el nivel ~43.</summary>
        public static long MonedasPara(int nivel)
        {
            if (nivel <= 1) return 10L;
            if (nivel > NivelMax) nivel = NivelMax;
            long monedas = (long)(10.0 * Math.Pow(1.33, nivel - 1));
            return Math.Min(1_000_000L, monedas);   // 1 de platino, jamás más
        }

        /// <summary>LA ESCALERA DE ARMAS: el peldaño de cada nivel sigue
        /// SIEMPRE la progresión de armas y minerales de Terraria.
        /// v6.86 — LA ESCALERA DE LAS CIEN PUERTAS: renació REALINEADA
        /// con la letra nueva: cada arma y cada lingote OBTENIBLE en la
        /// era de su jefe («no puedes decirle al jugador que consiga
        /// adamantium si el jefe necesario para la progresion es el rey
        /// slime»). LAS REGLAS DE LA CASA que sobreviven: armas
        /// corruptas al devorarlas · minerales alternativos en TODOS
        /// los peldaños · las vacías (=) son PUERTAS DE OFRENDA MINERAL
        /// (EsNivelMineral las lee del array vacío). v6.51.8 MURIÓ la
        /// pintura de Israpony, EL ZENIT y las armas no obtenibles del
        /// mod — NADA vuelve.</summary>
        public static readonly int[][] ArmasDelNivel =
        {
            // (índice 0 vacío — el nivel 1 vive en [1])
            new int[] { 0 },
            new int[] { ItemID.CopperShortsword, ItemID.CopperBroadsword, ItemID.TinShortsword, ItemID.TinBroadsword },  // 1  el cobre/estaño de la letra
            new int[] { ItemID.IronBroadsword, ItemID.LeadBroadsword },                                                // 2  el hierro/plomo
            new int[] { ItemID.SilverBroadsword, ItemID.TungstenBroadsword },                                           // 3  PUERTA Rey Gelatina · la plata/tungsteno
            new int[] { ItemID.GoldBroadsword, ItemID.PlatinumBroadsword },                                             // 4  el oro/platino
            new int[] { ItemID.Musket, ItemID.TheUndertaker, ItemID.BallOHurt, ItemID.TheMeatball, ItemID.TheRottedFork },// 5  PUERTA Ojo de Cthulhu · las esferas/corazones (mundo sí, mundo no)
            new int[] { ItemID.BladeofGrass },                                                                          // 6  la jungla
            new int[] { ItemID.Starfury, ItemID.IceBlade, ItemID.FalconBlade },                                         // 7  las islas del cielo y las cajas
            new int[] { ItemID.LightsBane, ItemID.BloodButcherer },                                                     // 8  PUERTA el Mal del Mundo · la demonita/carmesí (su jefe la suelta)
            new int[] { ItemID.FieryGreatsword },                                                                      // 9  el infierno (pico de demonita en mano — tras el Mal del Mundo)
            new int[] { ItemID.BeesKnees, ItemID.BeeGun },                                                             // 10 PUERTA Reina Abeja · la colmena (sus drops)
            new int[] { ItemID.Boomstick },                                                                            // 11 los santuarios de jungla
            new int[] { ItemID.BeeKeeper },                                                                            // 12 la Reina Abeja (la espada que ella suelta)
            new int[] { ItemID.Muramasa },                                                                             // 13 PUERTA Skeletron · la mazmorra se abre
            new int[] { ItemID.NightsEdge },                                                                           // 14 el Filo de la Noche (la suma de la era)
            new int[] { ItemID.Handgun },                                                                              // 15 la mazmorra profunda
            new int[] { ItemID.DarkLance },                                                                            // 16 la lanza oscura
            new int[] { ItemID.Sunfury },                                                                              // 17 los cofres de sombra del infierno
            new int[] { ItemID.Katana },                                                                               // 18 el mercader viajero
            new int[] { },                                                                                             // 19 LA OFRENDA MINERAL (la gran ofrenda pre-difícil)
            new int[] { },                                                                                             // 20 PUERTA la Muralla · LA OFRENDA MINERAL (la puerta del modo difícil)
            new int[] { ItemID.CobaltSword, ItemID.PalladiumSword },                                                   // 21 el cobalto/paladio (MODO DIFÍCIL)
            new int[] { ItemID.CobaltNaginata, ItemID.PalladiumSword },                                                 // 22 la naginata cobalto (la alabada de la casa)
            new int[] { },                                                                                             // 23 PUERTA Reina Gelatina DIFÍCIL · LA OFRENDA MINERAL
            new int[] { ItemID.CrystalSerpent, ItemID.Flymeal },                                                          // 24 la Reina Gelatina DIFÍCIL (la serpiente y la espada de gel)
            new int[] { ItemID.MythrilSword, ItemID.OrichalcumSword },                                                 // 25 PUERTA los Gemelos · el mitril/oricalco
            new int[] { ItemID.MythrilHalberd, ItemID.OrichalcumSword },                                                // 26 la alabarda mitril
            new int[] { ItemID.AdamantiteSword, ItemID.TitaniumSword },                                                 // 27 la adamantita/titanio
            new int[] { ItemID.AdamantiteGlaive, ItemID.TitaniumSword },                                                // 28 PUERTA el Devorador · la guja adamantita
            new int[] { },                                                                                             // 29 LA OFRENDA MINERAL
            new int[] { ItemID.Excalibur },                                                                            // 30 PUERTA Skeletron Prime · la sagrada (lingotes de las mecánicas)
            new int[] { ItemID.ChlorophyteClaymore, ItemID.ChlorophyteSaber },                                         // 31 la clorofita (nace tras la 1.ª mecánica caída)
            new int[] { },                                                                                             // 32 PUERTA Plantera · LA OFRENDA MINERAL
            new int[] { ItemID.DeathSickle },                                                                          // 33 el eclipse
            new int[] { ItemID.TerraBlade },                                                                           // 34 LA TERRA BLADE (las verdaderas + el héroe roto — tras Plantera)
            new int[] { ItemID.TheAxe, ItemID.GrenadeLauncher, ItemID.VenusMagnum },                                   // 35 PUERTA el Gólem · Plantera (sus drops)
            new int[] { ItemID.PossessedHatchet, ItemID.GolemFist, ItemID.StaffofEarth, ItemID.HeatRay },              // 36 el Gólem (sus drops)
            new int[] { ItemID.Tsunami, ItemID.Flairon, ItemID.RazorbladeTyphoon, ItemID.BubbleGun },                  // 37 el Duque Fishron (SUS armas; el jefe ya NO es puerta — la letra)
            new int[] { ItemID.RainbowWhip, ItemID.EmpressBlade },                                                     // 38 PUERTA la Emperatriz de la Luz · SUS armas (la puerta y el botín, el mismo latido)
            new int[] { ItemID.TheHorsemansBlade, ItemID.BatScepter, ItemID.RavenStaff },                              // 39 la Luna de Calabaza
            new int[] { ItemID.BlizzardStaff, ItemID.ElfMelter, ItemID.SnowmanCannon, ItemID.ChristmasTreeSword },     // 40 la Luna de Escarcha
            new int[] { ItemID.InfluxWaver, ItemID.XenoStaff, ItemID.LaserMachinegun },                                // 41 la Locura Marciana
            new int[] { ItemID.DD2BetsyBow, ItemID.DD2SquireBetsySword },                                              // 42 Betsy (el Arco Aéreo y el Dragón Volador)
            new int[] { ItemID.ChargedBlasterCannon },                                                                 // 43 el cañón marciano RARO
            new int[] { },                                                                                             // 44 LA OFRENDA MINERAL
            new int[] { },                                                                                             // 45 PUERTA el Cultista · LA OFRENDA MINERAL (la víspera de las torres)
            new int[] { ItemID.SolarEruption, ItemID.DayBreak },                                                       // 46 la Torre Solar
            new int[] { ItemID.VortexBeater, ItemID.Phantasm },                                                         // 47 la Torre del Vórtice
            new int[] { ItemID.NebulaBlaze, ItemID.NebulaArcanum },                                                     // 48 la Torre de la Nebulosa
            new int[] { ItemID.StardustDragonStaff, ItemID.StardustCellStaff },                                         // 49 la Torre del Polvo Estelar
            new int[] { },                                                                                             // 50 PUERTA el Señor de la Luna · LA OFRENDA MINERAL (LA CIMA de vanilla)
            new int[] { ItemID.Meowmere },                                                                             // 51 el Señor de la Luna: la espada gato
            new int[] { ItemID.StarWrath },                                                                            // 52 la ira de las estrellas
            new int[] { ItemID.LastPrism },                                                                            // 53 EL PRISMA FINAL
            new int[] { ItemID.SDMG },                                                                                 // 54 la SDMG
            new int[] { ItemID.Terrarian },                                                                            // 55 el yoyó definitivo
            new int[] { ItemID.MoonlordTurretStaff },                                                                  // 56 el Celebration Mk2
            new int[] { ItemID.RainbowCrystalStaff },                                                                  // 57 el cristal arcoíris
            new int[] { ItemID.LunarFlareBook },                                                                       // 58 el libro del destello lunar
            new int[] { },                                                                                             // 59 PUERTA Aethon (el recuerdo lejano) · LA OFRENDA MINERAL
            // 60-100 — LA ERA DE LAS GRANDES OFRENDAS (mineral puro:
            // la escalera de la cima — ver MineralDe: luminita 60-79 es
            // hongonita, 80-99 es espectro, 100 es LA GRAN OFRENDA FINAL)
            new int[] { }, new int[] { }, new int[] { }, new int[] { }, new int[] { },   // 60-64
            new int[] { }, new int[] { }, new int[] { }, new int[] { }, new int[] { },   // 65-69
            new int[] { }, new int[] { }, new int[] { }, new int[] { }, new int[] { },   // 70-74
            new int[] { }, new int[] { }, new int[] { }, new int[] { }, new int[] { },   // 75-79
            new int[] { }, new int[] { }, new int[] { }, new int[] { }, new int[] { },   // 80-84
            new int[] { }, new int[] { }, new int[] { }, new int[] { }, new int[] { },   // 85-89
            new int[] { }, new int[] { }, new int[] { }, new int[] { }, new int[] { },   // 90-94
            new int[] { }, new int[] { }, new int[] { }, new int[] { }, new int[] { },   // 95-99
            new int[] { },                                                                                             // 100 LA GRAN OFRENDA FINAL (donde nace LA IRA)
        };

        // ================================================================
        //  v6.51.8 — LA OFRENDA MINERAL (la letra: «en caso de no haber
        //  mas arma usar cierta cantidad de lingotes de algun mineral
        //  correspondiente con el nivel… al ser minerales no necesitan
        //  estar corruptos ya que no se corrompen como las armas»).
        //  v6.51.9 — LA OFRENDA MINERAL EN TODA LA ESCALERA (la letra:
        //  «en caso de que la gente no pueda conseguir las armas,
        //  tambien dale una opcion de lingotes segun el nivel claro y
        //  siempre deben ser mas de 50 lingotes para cada despertar no
        //  menos, ademas el numero de lingotes siempre debe ir
        //  aumentando por cada nivel de despertar»): el metal que
        //  corresponde a CADA nivel — en los peldaños de armas (1-44) es
        //  la ALTERNATIVA cuando el arma no llega; en la cima (45-50,
        //  agotadas las armas de verdad) es LA OFRENDA. Se devuelven
        //  SOLTANDO LA PILA con el Eterno en la mano (el mismo gesto del
        //  arma — sin corrupción: los minerales no se corrompen).
        // ================================================================

        /// <summary>¿EL NIVEL N ES DE MINERALES? (true = la ofrenda es
        /// SOLO la pila de lingotes — no hay arma del peldaño; los
        /// peldaños de armas ofrecen el mineral como ALTERNATIVA).
        /// v6.86 — DATA-DRIVEN: la decide el ARRAY VACÍO de ArmasDelNivel
        /// (nadie vuelve a mantener dos listas a mano).</summary>
        public static bool EsNivelMineral(int nivel)
        {
            if (nivel < 1 || nivel > NivelMax) return false;
            var peldano = ArmasDelNivel[nivel];
            return peldano == null || peldano.Length == 0;
        }

        /// <summary>EL MINERAL DEL PELDAÑO N (el lingote que pide — la
        /// escalera de metales QUE CORRESPONDE al nivel: el cobre del
        /// primer peldaño… la luminita del Señor de la Luna en la cima
        /// 37-48… la hongonita y el espectro en el final del mundo).
        /// v6.51.9: definido EN TODA LA ESCALERA 1-50 — la alternativa
        /// mineral vive en cada peldaño de armas.</summary>
        public static int MineralDe(int nivel)
        {
            if (nivel < 1 || nivel > NivelMax) return -1;   // no es peldaño
            if (nivel == 100) return ItemID.SpectreBar;     // LA GRAN OFRENDA FINAL (donde nace LA IRA)
            if (nivel >= 80) return ItemID.SpectreBar;      // 80-99: la era del espectro (la cima mineral)
            if (nivel >= 60) return ItemID.ShroomiteBar;    // 60-79: la era de la hongonita
            if (nivel >= 45) return ItemID.LunarBar;        // 45-59: la luminita (las torres → el Señor de la Luna → Aethon)
            if (nivel >= 35) return ItemID.ShroomiteBar;    // 35-44: la era post-Plantera
            if (nivel >= 31) return ItemID.ChlorophyteBar;  // 31-34: la clorofita (nace tras la 1.ª mecánica)
            if (nivel >= 30) return ItemID.HallowedBar;     // 30: la sagrada (los lingotes que sueltan las mecánicas)
            if (nivel >= 27) return ItemID.AdamantiteBar;   // 27-29: la adamantita/titanio
            if (nivel >= 25) return ItemID.MythrilBar;      // 25-26: el mitril/oricalco
            if (nivel >= 21) return ItemID.CobaltBar;       // 21-24: el cobalto/paladio (MODO DIFÍCIL)
            if (nivel >= 9) return ItemID.HellstoneBar;     // 9-20: la era del infierno (tras el Mal del Mundo: el pico de demonita)
            if (nivel == 8) return ItemID.DemoniteBar;      // 8: la demonita/carmesí (la suelta SU jefe caído)
            if (nivel >= 5) return ItemID.MeteoriteBar;     // 5-7: el meteorito (cae tras el Ojo)
            if (nivel == 4) return ItemID.GoldBar;          // 4: el oro/platino
            if (nivel == 3) return ItemID.SilverBar;        // 3: la plata/tungsteno
            if (nivel == 2) return ItemID.IronBar;          // 2: el hierro/plomo
            return ItemID.CopperBar;                        // 1: el cobre/estaño de la letra
        }

        /// <summary>CUÁNTOS LINGOTES exige el peldaño N (la letra de la
        /// .51.9: «siempre deben ser mas de 50 lingotes para cada
        /// despertar no menos, ademas el numero de lingotes siempre debe
        /// ir aumentando por cada nivel de despertar»): 51 en el nivel 1
        /// y +2 POR NIVEL — siempre más de 50, siempre creciente (149
        /// la gran ofrenda final del 50).</summary>
        public static int CantidadMineralDe(int nivel)
        {
            if (nivel < 1 || nivel > NivelMax) return 0;
            return 51 + 2 * (nivel - 1);
        }

        // ================================================================
        //  v6.51.11 — LOS CONTADORES DE LA MESA: la comida se sirve EN
        //  EL SUELO y el libro come SOLO lo que le falta del peldaño —
        //  los contadores parciales viven en el ALMA DEL LIBRO
        //  (AlmaEternoGlobalItem.Ofrenda*) y NADIE vuelve a mirar el
        //  inventario (la mochila es sagrada — el bug de la cadena
        //  silenciosa de la .51.10 murió con ella).
        // ================================================================
        //  LOS PODERES (los umbrales de la letra)
        // ================================================================

        /// <summary>Las MEJORAS de los ojos desbloqueadas. v6.86 —
        /// redistribuidas a lo largo de las CIEN puertas (la letra de
        /// los 100 niveles: «así tenemos más margen»): 10 → la 1 ·
        /// 40 → la 2 · 70 → la 3 · 80 → la 4 (histórico v6.51: 5/15/
        /// 30/40).</summary>
        public static int MejorasDesbloqueadas(int despertar)
        {
            if (despertar >= 80) return 4;
            if (despertar >= 70) return 3;
            if (despertar >= 40) return 2;
            if (despertar >= 10) return 1;
            return 0;
        }

        /// <summary>¿El SOL RÚNICO COMBINADO ya vive? v6.86 — nace en
        /// el 15 (histórico: 7): «2 minions con la primera mejora
        /// combinan su ataque y crean un sol runico de 1 anillo».</summary>
        public static bool SolDisponible(int despertar) => despertar >= 15;

        /// <summary>Los ANILLOS del sol combinado. v6.86 — 1 → 15 ·
        /// 2 → 50 («el sol runico aumenta su ataque y sus anillos,
        /// antes 1 anillo ahora 2») · 3 → 75 (histórico: 7/17/35).</summary>
        public static int AnillosDelSol(int despertar)
        {
            if (despertar >= 75) return 3;
            if (despertar >= 50) return 2;
            return despertar >= 15 ? 1 : 0;
        }

        /// <summary>Los SOLES por minuto (2 base · 3 en el 50 · 4 en el
        /// 75 — «este ataque se puede hacer 2 veces por minuto»).</summary>
        public static int SolesPorMinuto(int despertar)
        {
            if (despertar >= 75) return 4;
            if (despertar >= 50) return 3;
            return despertar >= 15 ? 2 : 0;
        }

        /// <summary>¿Los AGUJEROS NEGROS COMBINADOS ya viven? v6.86 —
        /// nacen en el 30 (histórico: 10): «al tener 5 o más minion
        /// con la primera mejora, estos pueden combinar su ataque y
        /// lanzar un agujero negro aleatorio de 4 listados».</summary>
        public static bool AgujerosDisponibles(int despertar) => despertar >= 30;

        /// <summary>Los AGUJEROS por minuto (1 base · 2 en el 85:
        /// «antes solo podían lanzar 1 cada minuto, ahora se pueden
        /// lanzar 2 cada minuto» — histórico: 45).</summary>
        public static int AgujerosPorMinuto(int despertar)
            => despertar >= 85 ? 2 : (despertar >= 30 ? 1 : 0);

        /// <summary>¿La versión ASCENDIDA de los agujeros? v6.86 — en el
        /// 60 (histórico: 25): «se mejoran los ataques de los agujeros
        /// negros a su versión de Ascendido haciendo aún más daño».</summary>
        public static bool AgujerosAscendidos(int despertar) => despertar >= 60;

        /// <summary>¿El SUPREMO/AURORA ya vive? v6.86 — en el 90
        /// (histórico: 45): «se agrega un nuevo tipo de agujero negro
        /// runico independiente… Agujero Negro Supremo y Agujero
        /// Supremo Aurora… el ataque combinado de 8 minions o más en
        /// su mejora número 4».</summary>
        public static bool SupremoDisponible(int despertar) => despertar >= 90;

        /// <summary>v6.59 — ¿LA IRA DE UN SER EXTERIOR ya vive? (DESPERTAR
        /// 100 — el final del camino): LO que consigue el grimorio al
        /// completar su despertar — el ataque definitivo del escuadrón:
        /// el círculo que SE CIERRA girando sobre el jugador (elevándose)
        /// o alrededor del libro, los rayos de perlin de TODO el
        /// escuadrón clavados al centro, la bruma blanca y morada, los
        /// círculos rúnicos… y EL HAZ — el rayo concentrado definitivo —
        /// disparado al enemigo que cazan (jefes SIEMPRE primero). Daño:
        /// base 1000 + la cadena mágica del grimorio + el 50 % del daño
        /// total de invocación. v6.60 — el REQUISITO y el COMPÁS los
        /// pone AtaquesCombinadosSistema: ONCE ojos TODOS en su mejora
        /// máxima (la prueba: 5 haces/min; el final: 1 cada 30 min).
        /// v6.86 — LA LETRA NUEVA DE LA CIMA: «la Ira o sea el Haz,
        /// sera obtenible en el nivel 100 y venciendo a moon lord al
        /// menos 5 veces, y venciendo al fragmento de un recuerdo
        /// lejano Aethon 1 vez» — el despertar 100 YA NO BASTA: los
        /// CONTADORES DE CAZA tienen la última palabra (los lleva
        /// CazaJefesPlayer y los pagan los OnKill de
        /// CazaJefesGlobalNPC).</summary>
        public static bool IraDisponible(int despertar) => despertar >= NivelMax;

        /// <summary>v6.86 — LA IRA COMPLETA (la letra de la cima): el
        /// despertar 100 Y el Señor de la Luna vencido ≥ 5 veces Y
        /// Aethon (el fragmento de un recuerdo lejano) vencido ≥ 1 vez.
        /// El legacy: el que ya lo tenía muerto en su mundo al cargar
        /// esta versión cobra 1 vida de las 5 (una cacería es del que
        /// la ve — el resto hay que ganárselo).</summary>
        public static bool IraDisponible(Player p)
        {
            if (p == null || !p.active) return false;
            if (NivelDe(p) < NivelMax) return false;
            var cazador = p.GetModPlayer<Players.CazaJefesPlayer>();
            if (cazador == null) return false;
            return cazador.VecesMoonLord >= 5 && cazador.VecesAethon >= 1;
        }

        /// <summary>Los CONTADORES DE LA IRA en texto (para tooltips y
        /// la ventana del Cielo): «Moon Lord 3/5 · Aethon 0/1».</summary>
        public static string IraContadoresTexto(Player p)
        {
            var cazador = p?.GetModPlayer<Players.CazaJefesPlayer>();
            int ml = cazador?.VecesMoonLord ?? 0;
            int ae = cazador?.VecesAethon ?? 0;
            return $"Moon Lord {Math.Min(ml, 5)}/5 · Aethon {Math.Min(ae, 1)}/1";
        }

        // ================================================================
        //  v6.86 — EL ALTAR DE OFRENDAS (la letra: «investiga el altar
        //  del mod Heroic Weapons... este es el tipo de altar que
        //  necesitamos para que el grimorio pueda consumir cosas que no
        //  sean las que necesita y no esten corruptas»). La ECONOMÍA
        //  (el informe R5 §5): el altar devora RELLENO (ni requisito ni
        //  corrupto) y lo vuelve PUNTOS en el alma del libro — 1 punto
        //  por cada 10 de plata de valor. Los puntos canjean SOLO
        //  esencias del peldaño, hasta el 30 % (ver el canje en
        //  DespertarGrimorioSistema.ServirLaMesa).
        // ================================================================

        /// <summary>El precio de UNA esencia en puntos del altar (la
        /// referencia de R5 §5.2: «una esencia equivale a 25 puntos»).</summary>
        public const int PuntosPorEsencia = 25;

        /// <summary>El umbral de valor mínimo del relleno (500 cobre =
        /// 5 de plata: la basura blanca no alimenta a nadie — R5 §5.4).</summary>
        public const long ValorMinimoRelleno = 500L;

        /// <summary>¿ACEPTA EL ALTAR este item como RELLENO? (la tabla de
        /// R5 §5.4 — NI requisito NI corrupto NI sagrado):
        ///   · NO la Esencia Corrupta (es requisito), NO las MONEDAS, NO
        ///     el mineral del peldaño, NO las armas del peldaño.
        ///   · NO lo corrupto (requisito/ingrediente sagrado).
        ///   · NO los items vivos del mod (grimorios, bolsas, el libro
        ///     corrupto, quest) ni los favoritos.
        ///   · NO lo que vale menos de 5 de plata.
        ///   · SÍ todo lo demás (materiales, armas limpias, drops).</summary>
        public static bool AceptaRelleno(Item it, Player portador, Item grimorio)
        {
            try
            {
                if (it == null || it.IsAir) return false;

                // LOS SAGRADOS: la esencia, las monedas, el libro corrupto.
                if (it.type == ItemID.CopperCoin || it.type == ItemID.SilverCoin ||
                    it.type == ItemID.GoldCoin || it.type == ItemID.PlatinumCoin) return false;
                if (it.type == ModContent.ItemType
                    <global::AethonMod.Content.Items.Altar.ItemCorrupto>()) return false;
                if (it.type == ModContent.ItemType
                    <global::AethonMod.Content.Items.Altar.LibroCorrupto>()) return false;

                // LO CORRUPTO (requisito/ingrediente sagrado).
                var sello = it.GetGlobalItem
                    <global::AethonMod.Content.Globals.CorruptoGlobalItem>();
                if (sello != null && sello.Corrompido) return false;

                // LOS VIVOS DEL MOD: grimorios, bolsas, quests (el patrón
                // de la casa: nada devorable que sea una llave de juego).
                if (it.ModItem != null) return false;
                if (it.favorited) return false;

                // LOS REQUISITOS DEL PELDAÑO VIVO (nada de alimentarse de
                // la propia ofrenda): el mineral y las armas de AHORA.
                if (grimorio != null && !grimorio.IsAir)
                {
                    int siguiente = NivelDespertarSiguiente(grimorio);
                    if (siguiente >= 1 && siguiente <= NivelMax)
                    {
                        int tipoMin = MineralDe(siguiente);
                        if (tipoMin > 0 && it.type == tipoMin) return false;
                        var armas = ArmasDelNivel[siguiente];
                        if (armas != null)
                            for (int k = 0; k < armas.Length; k++)
                                if (armas[k] == it.type) return false;
                    }
                }

                // EL UMBRAL (la basura blanca no alimenta).
                long valor = (long)it.value * (long)it.stack;
                return valor >= ValorMinimoRelleno;
            }
            catch { return false; }
        }

        /// <summary>CUÁNTOS PUNTOS vale la ofrenda (1 punto por cada 10
        /// de plata del valor TOTAL de la pila — R5 §5.2).</summary>
        public static int ValorEnPuntos(Item it)
        {
            if (it == null || it.IsAir) return 0;
            long valor = (long)it.value * (long)it.stack;
            int puntos = (int)(valor / 1000L);   // 10 de plata = 1000 cobre
            return Math.Max(0, puntos);
        }

        /// <summary>EL REGISTRO DE LA OFRENDA: el altar devora el relleno
        /// (la pila entera de una vez — stack--, la UI manda) y sus puntos
        /// viajan al ALMA DEL LIBRO (ProgresoOfrendas — persiste y viaja
        /// por el cable con el item). Devuelve los puntos ganados.</summary>
        public static int RegistrarOfrenda(Item relleno, Item grimorio)
        {
            try
            {
                if (relleno == null || relleno.IsAir) return 0;
                int puntos = ValorEnPuntos(relleno);
                if (puntos <= 0) return 0;

                var alma = grimorio?.GetGlobalItem
                    <global::AethonMod.Content.Globals.AlmaEternoGlobalItem>();
                if (alma == null) return 0;

                alma.ProgresoOfrendas += puntos;
                return puntos;
            }
            catch { return 0; }
        }

        /// <summary>La CLAVE del anuncio del hito de este nivel (o null si
        /// el nivel no desbloquea nada — la lista pequeña de la letra).</summary>
        public static string ClaveHito(int nivel)
        {
            switch (nivel)
            {
                case 10: return "Mods.AethonMod.Despertar.HitoMejora1";
                case 15: return "Mods.AethonMod.Despertar.HitoSol";
                case 20: return "Mods.AethonMod.Despertar.HitoMascota";
                case 30: return "Mods.AethonMod.Despertar.HitoAgujeros";
                case 40: return "Mods.AethonMod.Despertar.HitoMejora2";
                case 50: return "Mods.AethonMod.Despertar.HitoSol2";
                case 60: return "Mods.AethonMod.Despertar.HitoAscendidos";
                case 70: return "Mods.AethonMod.Despertar.HitoMejora3";
                case 75: return "Mods.AethonMod.Despertar.HitoSol3";
                case 80: return "Mods.AethonMod.Despertar.HitoMejora4";
                case 85: return "Mods.AethonMod.Despertar.HitoAgujeros2";
                case 90: return "Mods.AethonMod.Despertar.HitoSupremo";
                case 100: return "Mods.AethonMod.Despertar.HitoMaximo";
                default: return null;
            }
        }

        // ================================================================
        //  v6.67 — LA CAZA DEL DESPERTAR: LOS JEFES DE TERRARIA EN LA
        //  ESCALERA (la letra del usuario: «quiero que para ciertos
        //  niveles se tomen en cuenta los bosses principales de terraria
        //  para la subida de nivel del despertar, algo importante debe
        //  haber un checklist de los jefes en caso de no tener el nivel
        //  necesario que lo pida y mates el jefe, este debe contar, por
        //  ejemplo, estamos en nivel 1 y matamos a plantera, es obvio que
        //  plantera no es un jefe de incio, seria un jefe que se pediria
        //  en niveles altos de despertar, pero como ya la matamos entonces
        //  cuando lleguemos al nivel de despertar que la pide entonces
        //  este ya estaria marcado»).
        //
        //  LA ESCALERA DE LA CAZA (17 puertas, siguiendo SIEMPRE la
        //  progresión de Terraria — el mismo camino que las armas del
        //  peldaño): cada puerta exige que SU jefe principal YA HAYA
        //  CAÍDO — y el crédito es RETROACTIVO DE VERDAD: mátalo hoy a
        //  nivel 1 de despertar (o lo mataste ANTES de que el mod
        //  existiera, en este mundo) y cuando llegues a su nivel YA
        //  ESTÁ MARCADO.
        //
        //  LAS DOS FUENTES DEL CRÉDITO (cualquiera de las dos basta):
        //   · LA BANDERA DEL MUNDO (NPC.downed* de vanilla — la lápida
        //     que Terraria planta al morir un jefe: SIEMPRE escrita al
        //     matarlo, incluso por otros jugadores o antes del mod).
        //   · EL REGISTRO DEL CAZADOR (CazaJefesPlayer: los jefes que
        //     ESTE portador tiró con el mod vivo — viaja con el
        //     personaje a otros mundos).
        // ================================================================

        /// <summary>UNA PUERTA DE LA CAZA: el nivel que la exige, la clave
        /// hjson de su nombre y el NPC de la miniatura (la ventana del
        /// Cielo pinta su retrato).</summary>
        public readonly struct JefeDelCamino
        {
            public readonly int Nivel;
            public readonly string Clave;
            public readonly int Miniatura;
            public JefeDelCamino(int nivel, string clave, int miniatura)
            {
                Nivel = nivel;
                Clave = clave;
                Miniatura = miniatura;
            }
        }

        /// <summary>v6.86 — LA CAZA DE LAS CIEN PUERTAS: misma escalera
        /// de siempre REALINEADA — la Emperatriz SUBE a SU propia puerta
        /// (38) con SUS armas en el MISMO peldaño (la letra: «no está la
        /// emperatriz de la luz como requisito para despertar pero sí
        /// sus armas» — ahora es puerta Y botín, el mismo latido);
        /// Deerclops y el Duque Fishron MUEREN como puerta (la letra:
        /// «el jefe Deerclop no lo incluyas en la progresion del
        /// despertar del grimorio» + «el jefe duque fishron no
        /// necesitas incluirlo» — las armas del Duque siguen siendo
        /// ofrenda en el 37, pero su jefe ya NO manda); y LA CIMA NUEVA:
        /// Aethon custodia la puerta 59 — el fragmento de un recuerdo
        /// lejano, el dios del propio mod, justo antes de la ERA DE LAS
        /// GRANDES OFRENDAS. La cima del despertar de vanilla sigue
        /// siendo el Señor de la Luna en el 50 (la letra v6.75).
        /// v6.75 histórico: verificada la escalera contra la progresión
        /// CANÓNICA de la wiki (los 11 principales — TODOS tienen su
        /// puerta, en orden).</summary>
        public static readonly JefeDelCamino[] LosJefesDelDespertar =
        {
            new JefeDelCamino(3,  "Mods.AethonMod.Despertar.JefeReyGelatina",     NPCID.KingSlime),
            new JefeDelCamino(5,  "Mods.AethonMod.Despertar.JefeOjoDeCthulhu",    NPCID.EyeofCthulhu),
            new JefeDelCamino(8,  "Mods.AethonMod.Despertar.JefeElMalDelMundo",   NPCID.EaterofWorldsHead),
            new JefeDelCamino(10, "Mods.AethonMod.Despertar.JefeReinaAbeja",      NPCID.QueenBee),
            new JefeDelCamino(13, "Mods.AethonMod.Despertar.JefeSkeletron",       NPCID.SkeletronHead),
            new JefeDelCamino(20, "Mods.AethonMod.Despertar.JefeLaMuralla",       NPCID.WallofFlesh),
            new JefeDelCamino(23, "Mods.AethonMod.Despertar.JefeReinaGelatina",   NPCID.QueenSlimeBoss),
            new JefeDelCamino(25, "Mods.AethonMod.Despertar.JefeLosGemelos",      NPCID.Retinazer),
            new JefeDelCamino(28, "Mods.AethonMod.Despertar.JefeElDevorador",     NPCID.TheDestroyer),
            new JefeDelCamino(30, "Mods.AethonMod.Despertar.JefeSkeletronPrime",  NPCID.SkeletronPrime),
            new JefeDelCamino(32, "Mods.AethonMod.Despertar.JefePlantera",        NPCID.Plantera),
            new JefeDelCamino(35, "Mods.AethonMod.Despertar.JefeGolem",           NPCID.Golem),
            new JefeDelCamino(38, "Mods.AethonMod.Despertar.JefeEmperatrizDeLuz", NPCID.HallowBoss),
            new JefeDelCamino(45, "Mods.AethonMod.Despertar.JefeCultistaLunatico",NPCID.CultistBoss),
            new JefeDelCamino(50, "Mods.AethonMod.Despertar.JefeSenorDeLaLuna",   NPCID.MoonLordCore),
            new JefeDelCamino(59, "Mods.AethonMod.Despertar.JefeAethon",          MiniAethon()),
        };

        /// <summary>v6.86 — LA MINIATURA DE AETHON (la ventana del Cielo
        /// pinta su retrato): ModContent NO está listo durante la
        /// inicialización estática de un array — el helper try/catchea
        /// y cae al Rey Gelatina (la cara es cosmética; la PUERTA la
        /// pone el registro del cazador, no la miniatura).</summary>
        private static int MiniAethon()
        {
            try { return ModContent.NPCType<global::AethonMod.Content.NPCs.AethonBoss>(); }
            catch { return NPCID.KingSlime; }
        }

        /// <summary>¿ESTE nivel exige un jefe? (la puerta de la caza — o
        /// null si el nivel no tiene puerta).</summary>
        public static JefeDelCamino? JefeDeNivel(int nivel)
        {
            for (int i = 0; i < LosJefesDelDespertar.Length; i++)
                if (LosJefesDelDespertar[i].Nivel == nivel)
                    return LosJefesDelDespertar[i];
            return null;
        }

        /// <summary>LA BANDERA DEL MUNDO de una puerta (la lápida de
        /// vanilla: la escribió Terraria al morir el jefe, en este
        /// mundo, por quien fuera — la fuente del crédito retroactivo
        /// de SIEMPRE). La Muralla no tiene bandera propia: su lápida
        /// ES el modo difícil (la única puerta al hardmode).
        /// v6.86 — la Emperatriz baja a SU puerta (38) y Aethon (59)
        /// NO tiene lápida de mundo (es del mod): la paga el REGISTRO
        /// DEL CAZADOR (VecesAethon ≥ 1 — lo escribe
        /// CazaJefesGlobalNPC al morir el dios).</summary>
        public static bool BanderaDelMundo(int nivel)
        {
            return nivel switch
            {
                3  => NPC.downedSlimeKing,
                5  => NPC.downedBoss1,
                8  => NPC.downedBoss2,          // el Devorador O el Cerebro (el mal del mundo)
                10 => NPC.downedQueenBee,
                13 => NPC.downedBoss3,
                20 => Main.hardMode,            // la Muralla de Carne
                23 => NPC.downedQueenSlime,
                25 => NPC.downedMechBoss1,      // Los Gemelos
                28 => NPC.downedMechBoss2,      // El Devorador
                30 => NPC.downedMechBoss3,      // Skeletron Prime
                32 => NPC.downedPlantBoss,      // Plantera
                35 => NPC.downedGolemBoss,
                38 => NPC.downedEmpressOfLight, // la Emperatriz de la Luz
                45 => NPC.downedAncientCultist, // la antesala de la cima
                50 => NPC.downedMoonlord,       // LA CIMA de vanilla: el Señor de la Luna
                // 59 — Aethon: sin bandera de mundo (la paga el cazador)
                _  => false,
            };
        }

        /// <summary>¿EL JEFE DE ESTA PUERTA YA CAÍDO? (el crédito
        /// retroactivo de la letra: la bandera del mundo O el registro
        /// del cazador — mátalo cuando quieras, ANTES o DESPUÉS de
        /// llegar al nivel: cuenta igual). Sin puerta: siempre true.
        /// v6.86 — Aethon (59) además cuenta por SUS VECES (el contador
        /// de la Ira sirve de bandera: una victoria = puerta paga).</summary>
        public static bool JefeYaCaido(Player p, int nivel)
        {
            JefeDelCamino? j = JefeDeNivel(nivel);
            if (j == null) return true;
            if (BanderaDelMundo(nivel)) return true;
            if (p != null && p.active)
            {
                var cazador = p.GetModPlayer<Players.CazaJefesPlayer>();
                if (cazador != null)
                {
                    if (nivel == 59 && cazador.VecesAethon >= 1) return true;
                    if (cazador.Caido(nivel)) return true;
                }
            }
            return false;
        }

        // ================================================================
        //  v6.68 — LA VUELTA A CASA (la letra del usuario: «volvamos a
        //  esto donde todos los buff aumentaran solo con el nivel del
        //  libro, dejemos al despertar para habilidades y poderes como
        //  soles agujeros negros y el haz»): LA SEPARACIÓN de la v6.67
        //  MURIÓ — todo el poder del escuadrón (daño del rayo y del
        //  contacto, velocidad, rango, cadena, cadencia) vuelve al
        //  NIVEL DEL LIBRO (WeaponScaling: la casa de siempre) y el
        //  DESPERTAR queda para LO QUE ABRE: las mejoras de los ojos,
        //  el sol rúnico, los agujeros negros, los ascendidos, el
        //  supremo… y EL HAZ de la Ira. (Las funciones de la v6.67 —
        //  DanoEscuadronMult · ContactoEscuadronMult ·
        //  VelocidadEscuadronMult · RangoEscuadron · CadenaEscuadron —
        //  y LOS MICROPODERES murieron de raíz.)
        // ================================================================

        /// <summary>¿ES ESTE nivel un HITO (abre un poder de la letra)?</summary>
        public static bool EsNivelHito(int nivel) => ClaveHito(nivel) != null;

        /// <summary>¿ES ESTE nivel una PUERTA DE CAZA (exige un jefe)?</summary>
        public static bool EsNivelDeCaza(int nivel) => JefeDeNivel(nivel) != null;

        // ================================================================
        //  v6.51.8 — EL CAMINO DEL DESPERTAR (la letra: «en cada nivel del
        //  despertar solo dice % de aumento de daño, pero no dicen nada
        //  mas, deberian decir cuando es que aparecen las mejoras en sus
        //  respectivos niveles y no solo aumento de daño»): la escalera
        //  COMPLETA de los hitos — cada MEJORA con SU nivel. La lee la
        //  ventana de info del Eterno (los QUE FALTAN, con su número).
        // ================================================================

        /// <summary>UN PASO DEL CAMINO: el nivel que desbloquea y la
        /// clave hjson de su descripción corta.</summary>
        public readonly struct PasoDelCamino
        {
            public readonly int Nivel;
            public readonly string Clave;
            public PasoDelCamino(int nivel, string clave)
            {
                Nivel = nivel;
                Clave = clave;
            }
        }

        /// <summary>LA ESCALERA DE HITOS COMPLETA (10→100) — qué mejora
        /// aparece y EN QUÉ NIVEL (la letra: «cuando es que aparecen las
        /// mejoras en sus respectivos niveles»). v6.86 — redistribuida
        /// a lo largo de las CIEN puertas (histórico v6.51: 5→50).</summary>
        public static readonly PasoDelCamino[] ElCaminoDelDespertar =
        {
            new PasoDelCamino(10, "Mods.AethonMod.Despertar.CaminoMejora1"),
            new PasoDelCamino(15, "Mods.AethonMod.Despertar.CaminoSol"),
            new PasoDelCamino(20, "Mods.AethonMod.Despertar.CaminoMascota"),
            new PasoDelCamino(30, "Mods.AethonMod.Despertar.CaminoAgujeros"),
            new PasoDelCamino(40, "Mods.AethonMod.Despertar.CaminoMejora2"),
            new PasoDelCamino(50, "Mods.AethonMod.Despertar.CaminoSol2"),
            new PasoDelCamino(60, "Mods.AethonMod.Despertar.CaminoAscendidos"),
            new PasoDelCamino(70, "Mods.AethonMod.Despertar.CaminoMejora3"),
            new PasoDelCamino(75, "Mods.AethonMod.Despertar.CaminoSol3"),
            new PasoDelCamino(80, "Mods.AethonMod.Despertar.CaminoMejora4"),
            new PasoDelCamino(85, "Mods.AethonMod.Despertar.CaminoAgujeros2"),
            new PasoDelCamino(90, "Mods.AethonMod.Despertar.CaminoSupremo"),
            new PasoDelCamino(100,"Mods.AethonMod.Despertar.CaminoMaximo"),
        };
    }
}
