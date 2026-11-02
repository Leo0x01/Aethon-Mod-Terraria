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

        /// <summary>v6.88 — EL DINERO que exige el nivel N (la letra
        /// nueva: «el requisito de monedas maximo que se pueden pedir son
        /// 100 monedas de platino para el nivel 100 y el minimo es 100
        /// monedas de cobre para el nivel 1 asi que entre el nivel 1 al
        /// 100 distribuye bien la cantidad de monedas» — la v6.86 TRUNCABA
        /// en 1 platino desde el ~43 y el requisito se ESTANCABA): escala
        /// exponencial SUAVE de 100 cobre → 100 platino. La base: el
        /// crecimiento exacto g = 10^(6/99) ≈ 1,1437 — así el nivel 100
        /// cae CLAVADO en 100.000.000 cobre (100 de platino, JAMÁS más) y
        /// cada nivel pide visiblemente más que el anterior (ningún
        /// tramo plano). Redondeo a múltiplos de 100 cobre (1 de plata):
        /// las cantidades se leen limpias en la mesa. El nivel 1: 100
        /// cobre (la letra); ~35: 5 de oro; ~70: 82 de oro; 100: 100 P.</summary>
        public static long MonedasPara(int nivel)
        {
            if (nivel <= 1) return 100L;                       // 100 monedas de cobre (la letra)
            if (nivel > NivelMax) nivel = NivelMax;
            double g = Math.Pow(10.0, 6.0 / 99.0);            // 1,1437 — clava el 100 en 100 P
            long monedas = (long)(100.0 * Math.Pow(g, nivel - 1));
            monedas = (monedas / 100L) * 100L;                 // múltiplos de 1 plata
            if (monedas < 100L) monedas = 100L;
            return Math.Min(100_000_000L, monedas);            // 100 de platino, JAMÁS más
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

        // ================================================================
        //  v6.88 — LA OFRENDA MINERAL VARIADA (la letra nueva: «despues
        //  del nivel 50 la ofrenda de minerales se concentra en 1 mineral
        //  y pide mucho, debe ser variada, pedir menos de ese mineral,
        //  pero pedir varios al mismo tiempo» — la v6.86 pedía UN solo
        //  mineral con 51..249 lingotes). LA NUEVA LEY: cada peldaño pide
        //  UNA MEZCLA de 2-3 minerales DE SU ERA (se entregan en ORDEN:
        //  el primero incompleto de la lista se acredita primero) con
        //  cantidades MODERADAS por cabeza (18 + nivel/5) — el TOTAL del
        //  peldaño sigue creciendo (2-3 grupos × más nivel) pero NINGUNA
        //  pila solitaria es una pared. Cada GRUPO lleva sus ALTERNATIVOS
        //  (cobre/estaño, hierro/plomo…): la mezcla existe en CUALQUIER
        //  mundo, con el metal que la suerte dejó.
        // ================================================================

        /// <summary>LOS GRUPOS DE MINERALES DEL PELDAÑO N (la mezcla de
        /// su era): cada grupo es un int[] de tipos ALTERNATIVOS (el
        /// primero existe en este mundo o no — basta CUALQUIERA del
        /// grupo para pagar el grupo). Se entregan EN ORDEN (grupo a
        /// grupo — «varios al mismo tiempo»: la mesa LISTA la mezcla
        /// completa desde el primer vistazo).</summary>
        public static int[][] GruposMineralDe(int nivel)
        {
            if (nivel < 1 || nivel > NivelMax)
                return new int[][] { new int[] { ItemID.CopperBar } };
            if (nivel <= 4)    // 1-4: la era del cobre — cobre/estaño + hierro/plomo
                return new int[][]
                {
                    new int[] { ItemID.CopperBar, ItemID.TinBar },
                    new int[] { ItemID.IronBar, ItemID.LeadBar },
                };
            if (nivel <= 8)    // 5-8: post-Ojo — plata/tungsteno + oro/platino + el meteorito
                return new int[][]
                {
                    new int[] { ItemID.SilverBar, ItemID.TungstenBar },
                    new int[] { ItemID.GoldBar, ItemID.PlatinumBar },
                    new int[] { ItemID.MeteoriteBar },
                };
            if (nivel <= 20)   // 9-20: la era del infierno — demonita/carmesí + piedra infernal
                return new int[][]
                {
                    new int[] { ItemID.DemoniteBar, ItemID.CrimtaneBar },
                    new int[] { ItemID.HellstoneBar },
                };
            if (nivel <= 24)   // 21-24: el amanecer difícil — cobalto/paladio
                return new int[][]
                {
                    new int[] { ItemID.CobaltBar, ItemID.PalladiumBar },
                };
            if (nivel <= 26)   // 25-26: el mitril/oricalco
                return new int[][]
                {
                    new int[] { ItemID.MythrilBar, ItemID.OrichalcumBar },
                };
            if (nivel <= 29)   // 27-29: la adamantita/titanio
                return new int[][]
                {
                    new int[] { ItemID.AdamantiteBar, ItemID.TitaniumBar },
                };
            if (nivel <= 30)   // 30: la sagrada (los lingotes que sueltan las mecánicas)
                return new int[][]
                {
                    new int[] { ItemID.HallowedBar },
                };
            if (nivel <= 44)   // 31-44: la era de la clorofita — clorofita + hongonita
                return new int[][]
                {
                    new int[] { ItemID.ChlorophyteBar },
                    new int[] { ItemID.ShroomiteBar },
                };
            if (nivel <= 59)   // 45-59: la luminita de las torres + hongonita + la víspera de espectro
                return new int[][]
                {
                    new int[] { ItemID.LunarBar },
                    new int[] { ItemID.ShroomiteBar },
                    new int[] { ItemID.SpectreBar },
                };
            if (nivel <= 79)   // 60-79: la era de la hongonita — MEZCLA RICA (hongonita + espectro + clorofita)
                return new int[][]
                {
                    new int[] { ItemID.ShroomiteBar },
                    new int[] { ItemID.SpectreBar },
                    new int[] { ItemID.ChlorophyteBar },
                };
            // 80-100: la era del espectro — MEZCLA FINAL (espectro + luminita + hongonita)
            return new int[][]
            {
                new int[] { ItemID.SpectreBar },
                new int[] { ItemID.LunarBar },
                new int[] { ItemID.ShroomiteBar },
            };
        }

        /// <summary>CUÁNTOS LINGOTES exige CADA GRUPO de la mezcla del
        /// peldaño N (la letra nueva: «pedir menos de ese mineral, pero
        /// pedir varios al mismo tiempo»): 18 + nivel/5 por cabeza —
        /// MODERADO (la v6.86 pedía 51..249 de UN solo tipo; el muro
        /// murió). El TOTAL del peldaño (grupos × cantidad) sigue
        /// creciendo con el nivel.</summary>
        public static int CantidadPorGrupo(int nivel)
        {
            if (nivel < 1) return 18;
            if (nivel > NivelMax) nivel = NivelMax;
            return 18 + nivel / 5;   // 18 en el 1 · 28 en el 50 · 38 en el 100
        }

        /// <summary>LEGADO (v6.51.9→v6.88): el mineral REPRESENTATIVO del
        /// peldaño (el PRIMER grupo de la mezcla, primer tipo) — lo siguen
        /// leyendo los textos de la ALTERNATIVA mineral de los peldaños
        /// de armas. La ofrenda de los peldaños minerales pasa por
        /// GruposMineralDe (la mezcla completa).</summary>
        public static int MineralDe(int nivel)
        {
            var grupos = GruposMineralDe(nivel);
            return (grupos != null && grupos.Length > 0 && grupos[0] != null
                && grupos[0].Length > 0) ? grupos[0][0] : -1;
        }

        /// <summary>LEGADO (v6.51.9→v6.88): la cantidad del PRIMER grupo
        /// (para los textos de la alternativa mineral).</summary>
        public static int CantidadMineralDe(int nivel) => CantidadPorGrupo(nivel);

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

        /// <summary>v6.88 — LA IRA VIVE CON EL DESPERTAR 100 (la letra
        /// nueva de la cima: «Aethon debería estar como requisito para el
        /// nivel 100, junto con 10 señores de la luna y 5 emperatris de la
        /// luz, ya que en el nivel 100 es donde se consigue la habilidad
        /// definitiva del grimorio, o sea su ataque definitivo, obtiene
        /// Ira de un Ser Exterior»): la PUERTA 100 (JefeYaCaido) exige
        /// Aethon ≥ 1 Y Moon Lord ≥ 10 Y Emperatriz ≥ 5 ANTES DE SUBIR —
        /// llegar al 100 ya ES haber pagado la caza completa. La cura del
        /// bug «al llegar al nivel máximo del despertar el haz no se
        /// lanza»: la v6.86 volvía a pedir los contadores DESPUÉS de
        /// subir (si no los tenía, el haz NUNCA nacía); la v6.88 los
        /// cobra en la puerta y la Ira nace con el despertar 100. El
        /// escuadrón de 11 TODOS al máximo sigue siendo requisito del
        /// DISPARO (la letra v6.60, la pone
        /// AtaquesCombinadosSistema.Tick).</summary>
        public static bool IraDisponible(Player p)
        {
            if (p == null || !p.active) return false;
            return NivelDe(p) >= NivelMax;
        }

        /// <summary>Los CONTADORES DE LA CIMA en texto (para tooltips y
        /// la ventana del Cielo) — v6.88: «Moon Lord n/10 · Emperatriz
        /// n/5 · Aethon n/1» (la letra nueva de la cima).</summary>
        public static string IraContadoresTexto(Player p)
        {
            var cazador = p?.GetModPlayer<Players.CazaJefesPlayer>();
            int ml = cazador?.VecesMoonLord ?? 0;
            int eo = cazador?.VecesEmperatriz ?? 0;
            int ae = cazador?.VecesAethon ?? 0;
            return $"Moon Lord {Math.Min(ml, 10)}/10 · Emperatriz {Math.Min(eo, 5)}/5 · Aethon {Math.Min(ae, 1)}/1";
        }

        // ================================================================
        //  v6.88 — LOS HITOS DEL ESCUADRÓN (la letra: «hay una parte en
        //  las ventana de info que dice Despertar 50 todo el ataque del
        //  escuadron 1.25 y mas rapido, creo que deberia decirlo en
        //  porcentaje y deberia de haber un aumento cada 20 niveles
        //  ademas de que debe decirlo como hito»): EL HITO DEL ESCUADRÓN
        //  nace CADA 20 NIVELES, en PORCENTAJE, como HITO de la escalera
        //  (ClaveHito + ElCaminoDelDespertar + el tooltip del arma):
        ///    20 → TODO el ataque del escuadrón +10 %
        ///    40 → +15 %   · 60 → +20 %   · 80 → +25 %   · 100 → +30 %
        //  La v6.51.9 lo cobraba UNA sola vez (×1,25 con el despertar 50,
        //  horneado al nacer); la v6.88 lo cobra VIVO por hitos — el
        //  escuadrón entero se fortalece con el despertar de su dueño.
        // ================================================================

        /// <summary>EL MULTIPLICADOR DE ATAQUE DEL ESCUADRÓN por los
        /// hitos del despertar (cada 20 niveles, en porcentaje): lo leen
        /// BonusHitoVivo (el daño del rayo de cada ojo — el reemplazo
        /// del ×1,25 del 50) y los conjuros combinados (sol, agujeros,
        /// supremo — AtaquesCombinadosSistema).</summary>
        public static float MultEscuadron(int despertar)
        {
            if (despertar >= 100) return 1.30f;
            if (despertar >= 80) return 1.25f;
            if (despertar >= 60) return 1.20f;
            if (despertar >= 40) return 1.15f;
            if (despertar >= 20) return 1.10f;
            return 1.00f;
        }

        /// <summary>El PORCENTAJE del hito en texto (para tooltips:
        /// «+25 %»).</summary>
        public static int PctEscuadron(int despertar)
            => (int)Math.Round((MultEscuadron(despertar) - 1f) * 100f);


        // ================================================================
        //  v6.88 — EL ALTAR SE DESLIGA DEL DESPERTAR (la letra: «la
        //  funcion del altar que implementaste no debe estar ligada al
        //  grimorio, asi que quita ese requisito de ofrendas en el
        //  despertar, el altar sirve para corromper las armas»): LA
        //  ECONOMÍA DE PUNTOS (PuntosPorEsencia, ValorMinimoRelleno,
        //  AceptaRelleno, ValorEnPuntos, RegistrarOfrenda) MUERE de
        //  raíz — el canje del 30 % de esencias (ServirLaMesa) muere con
        //  ella y los campos del alma (ProgresoOfrendas /
        //  CreditoPuntosEsencias) quedan huérfanos y se extinguen. El
        //  altar hoy es EL CORRUPTOR DE ARMAS (AltarOfrendasUI) y nada
        //  más. La vía de recetas de corrupción (RecetasCorrupcionSistema)
        //  SIGUE como alternativa de siempre.
        // ================================================================


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

        /// <summary>UNA PUERTA DE LA CAZA: el nivel que la exige, la
        /// clave hjson de su nombre, el NPC de la miniatura (la ventana
        /// del Cielo pinta su retrato) — v6.88: LAS VECES que la puerta
        /// exige (1 = la de siempre; 2+ = el jefe caído DOS o más veces:
        /// «se puede pedir matar a un jefe varias veces seguidas») y EL
        /// CONTADOR que la paga: 0 = la lápida del mundo + el registro
        /// del cazador (la de siempre) · 1 = Moon Lord (VecesMoonLord)
        /// · 2 = la Emperatriz (VecesEmperatriz) · 3 = Aethon y SU cima
        /// compuesta (la puerta 100).</summary>
        public readonly struct JefeDelCamino
        {
            public readonly int Nivel;
            public readonly string Clave;
            public readonly int Miniatura;
            public readonly int Veces;
            public readonly int Contador;
            public JefeDelCamino(int nivel, string clave, int miniatura,
                int veces = 1, int contador = 0)
            {
                Nivel = nivel;
                Clave = clave;
                Miniatura = miniatura;
                Veces = Math.Max(1, veces);
                Contador = contador;
            }
        }

        /// <summary>v6.88 — LA CAZA REPARTIDA DE LA UNO A LA CIEN (la
        /// letra: «el orden de los jefes como requisito del Despertar
        /// esta mal, usa la wiki para ver el orden correcto… recuerda
        /// que ahora son 100 niveles, y ubicaste a todos en los primeros
        /// 50 niveles» + «ya que como requisitos pides items del Duque
        /// Fishron entonces incluye a ese jefe entre los requisitos» +
        /// «el jefe Aethon no deberia estar como requisito para el nivel
        /// 59, Aethon deberia estar como requisito para el nivel 100,
        /// junto con 10 señores de la luna y 5 emperatris de la luz»):
        /// la escalera 1-50 queda como la v6.86 (VERIFICADA contra la
        /// progresión CANÓNICA de la wiki — cada puerta en la era de
        /// sus armas), el DUQUE FISHRON entra como puerta en el 37 (sus
        /// armas ya vivían ahí — ahora es puerta Y botín, el mismo
        /// latido), Aethon ABANDONA el 59 y custodia LA CIEN — y las
        /// REPETICIONES (Moon Lord y Emperatriz, la letra: «recuerda
        /// que se puede pedir matar a un jefe varias veces seguidas, asi
        /// que distribuye bien todos los requisistos de 1 a 100»)
        /// reparten la caza por la segunda mitad de la escalera: la
        /// cima de vanilla (Señor de la Luna, 50) sigue en su sitio con
        /// las armas de su drop detrás (51-58, OBTENIBLES en su era — la
        /// ley de la casa), y de la 63 a la 99 el cazador vuelve a
        /// tirar de los dos titanes hasta la cuenta completa: la puerta
        /// 100 — LA CIMA COMPUESTA: Aethon ×1 Y Moon Lord ×10 Y
        /// Emperatriz ×5 — y ahí nace LA IRA (la letra: «en el nivel 100
        /// es donde se consigue la habilidad definitiva del grimorio»).</summary>
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
            new JefeDelCamino(37, "Mods.AethonMod.Despertar.JefeDuqueFishron",    NPCID.DukeFishron),
            new JefeDelCamino(38, "Mods.AethonMod.Despertar.JefeEmperatrizDeLuz", NPCID.HallowBoss),
            new JefeDelCamino(45, "Mods.AethonMod.Despertar.JefeCultistaLunatico",NPCID.CultistBoss),
            new JefeDelCamino(50, "Mods.AethonMod.Despertar.JefeSenorDeLaLuna",   NPCID.MoonLordCore),
            // v6.88 — LAS REPETICIONES (la caza repartida por la cima):
            new JefeDelCamino(63, "Mods.AethonMod.Despertar.JefeSenorDeLaLuna",   NPCID.MoonLordCore, 2, 1),
            new JefeDelCamino(67, "Mods.AethonMod.Despertar.JefeEmperatrizDeLuz", NPCID.HallowBoss,   2, 2),
            new JefeDelCamino(71, "Mods.AethonMod.Despertar.JefeSenorDeLaLuna",   NPCID.MoonLordCore, 3, 1),
            new JefeDelCamino(75, "Mods.AethonMod.Despertar.JefeEmperatrizDeLuz", NPCID.HallowBoss,   3, 2),
            new JefeDelCamino(79, "Mods.AethonMod.Despertar.JefeSenorDeLaLuna",   NPCID.MoonLordCore, 4, 1),
            new JefeDelCamino(83, "Mods.AethonMod.Despertar.JefeEmperatrizDeLuz", NPCID.HallowBoss,   4, 2),
            new JefeDelCamino(87, "Mods.AethonMod.Despertar.JefeSenorDeLaLuna",   NPCID.MoonLordCore, 5, 1),
            new JefeDelCamino(91, "Mods.AethonMod.Despertar.JefeEmperatrizDeLuz", NPCID.HallowBoss,   5, 2),
            new JefeDelCamino(95, "Mods.AethonMod.Despertar.JefeSenorDeLaLuna",   NPCID.MoonLordCore, 7, 1),
            new JefeDelCamino(99, "Mods.AethonMod.Despertar.JefeSenorDeLaLuna",   NPCID.MoonLordCore, 9, 1),
            // LA CIMA COMPUESTA (la letra): Aethon ×1 + Moon Lord ×10 + Emperatriz ×5
            new JefeDelCamino(100,"Mods.AethonMod.Despertar.JefeAethon",          MiniAethon(),       1, 3),
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

        /// <summary>LA BANDERA DEL MUNDO de una puerta (la lápide de
        /// vanilla: la escribió Terraria al morir el jefe, en este
        /// mundo, por quien fuera — la fuente del crédito retroactivo
        /// de SIEMPRE). La Muralla no tiene bandera propia: su lápida
        /// ES el modo difícil (la única puerta al hardmode).
        /// v6.88 — el DUQUE FISHRON gana SU puerta (37) con su lápida
        /// de mundo; Aethon (100) sigue SIN lápida (es del mod): la paga
        /// el REGISTRO DEL CAZADOR — y las REPETICIONES (63-99) no leen
        /// la lápida (una lápida es UNA sola vez): leen los CONTADORES.</summary>
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
                37 => NPC.downedFishron,        // v6.88 — EL DUQUE (la puerta nueva)
                38 => NPC.downedEmpressOfLight, // la Emperatriz de la Luz
                45 => NPC.downedAncientCultist, // la antesala de la cima
                50 => NPC.downedMoonlord,       // LA CIMA de vanilla: el Señor de la Luna
                // 63-99 — las REPETICIONES: sin lápida (la pagan los contadores)
                // 100 — Aethon: sin bandera de mundo (la paga el cazador)
                _  => false,
            };
        }

        /// <summary>¿EL JEFE DE ESTA PUERTA YA CAÍDO? (el crédito
        /// retroactivo de la letra: la bandera del mundo O el registro
        /// del cazador — mátalo cuando quieras, ANTES o DESPUÉS de
        /// llegar al nivel: cuenta igual). Sin puerta: siempre true.
        /// v6.88 — LAS REPETICIONES: las puertas de Moon Lord (63+) y
        /// la Emperatriz (67+) leen SUS CONTADORES (una lápide es UNA
        /// vez; el contador es para siempre); y LA CIMA COMPUESTA (100):
        /// Aethon ≥ 1 Y Moon Lord ≥ 10 Y Emperatriz ≥ 5 — la caza
        /// completa ANTES de subir a la cima donde nace LA IRA.</summary>
        public static bool JefeYaCaido(Player p, int nivel)
        {
            JefeDelCamino? j = JefeDeNivel(nivel);
            if (j == null) return true;
            if (BanderaDelMundo(nivel) && j.Value.Veces <= 1) return true;
            if (p != null && p.active)
            {
                var cazador = p.GetModPlayer<Players.CazaJefesPlayer>();
                if (cazador != null)
                {
                    switch (j.Value.Contador)
                    {
                        case 1:  // Moon Lord (las repetidas de la 63 a la 99)
                            return cazador.VecesMoonLord >= j.Value.Veces;
                        case 2:  // la Emperatriz (las repetidas de la 67 a la 91)
                            return cazador.VecesEmperatriz >= j.Value.Veces;
                        case 3:  // LA CIMA COMPUESTA (100): Aethon + ML×10 + EoL×5
                            return cazador.VecesAethon >= 1
                                && cazador.VecesMoonLord >= 10
                                && cazador.VecesEmperatriz >= 5;
                        default:
                            if (nivel == 100 && cazador.VecesAethon >= 1) return true;
                            if (cazador.Caido(nivel)) return true;
                            return false;
                    }
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
