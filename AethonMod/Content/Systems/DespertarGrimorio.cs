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
    // LA ESTRUCTURA (0..50):
    //  · SIEMPRE EMPIEZA EN NIVEL 0.
    //  · Para subir al nivel N se necesitan TRES ofrendas DEVORADAS
    //    (basta SOLTAR el arma corrupta con el Eterno SELECCIONADO):
    //      1. N Esencias Corruptas (más por nivel — el nivel 1 pide 1).
    //      2. DINERO creciente (10 de cobre en el nivel 1… hasta
    //         1 DE PLATINO en los niveles altos — la letra).
    //      3. UN ARMA CORRUPTA del peldaño de la progresión de Terraria
    //         de ese nivel (el nivel 1: «1 espada corta de cobre corrupta
    //         o una espada larga de cobre» — minerales alternativos, los
    //         jefes, las armas que «a veces aparecen en un mundo sí y
    //         otro no», hasta las armas del PROPIO MOD en la cima).
    //
    // LOS PODERES (los hitos de la letra):
    //   5 → MEJORA 1 de los ojos. 7 → EL SOL RÚNICO COMBINADO (1 anillo,
    //       2/min). 10 → LOS AGUJEROS NEGROS COMBINADOS (los 4 rúnicos,
    //       1/min). 15 → MEJORA 2. 17 → el sol con 2 ANILLOS y 3/min.
    //   20 → EL GRIMORIO ESCUPE AL MINIGRIMORIO (la mascota, con cine).
    //   25 → los agujeros ASCENDIDOS. 30 → MEJORA 3. 35 → el sol de
    //   3 ANILLOS y 4/min. 40 → MEJORA 4 (la última). 45 → agujeros
    //   2/min + EL SUPREMO/AURORA (8+ ojos con mejora 4, 1/min).
    //   50 → MÁXIMO POR AHORA: TODO el ataque de los ojos y sus
    //   proyectiles más fuerte y más rápido.
    //  Además CADA NIVEL: +1 % de daño a TODOS los tipos de daño del
    //  grimorio (mágico y de invocación — la letra: «consumir la esencia
    //  corrupta sigue aumentando un poquito el daño en %»).
    // ======================================================================
    public static class DespertarGrimorio
    {
        /// <summary>El tope por ahora (la letra: «nivel 50 como máximo
        /// nivel de la mecánica de corrupción por ahora»).</summary>
        public const int NivelMax = 50;

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

        /// <summary>La ESCALERA DE ARMAS: el peldaño de cada nivel sigue
        /// SIEMPRE la progresión de armas y minerales de Terraria (con sus
        /// minerales ALTERNATIVOS — «armas que a veces aparecen en un
        /// mundo sí y otro no» —, las de los jefes y las de la cima del
        /// propio mod). El arma debe estar CORRUPTA al devorarla.
        ///
        /// v6.51.8 — LA CURA DE LOS PELDAÑOS IMPOSIBLES (la letra: «para
        /// el despertar 42 piden el sol de israpony, eso es una pintura,
        /// no es un arma no deberia pedir eso, siempre debe seguir
        /// pidiendo un arma o en su defecto lingotes de algun mineral» +
        /// «no creo que sea bueno que pida la cenit en ningun momento» +
        /// «tambien pide armas propias del mod, estas armas no son
        /// obtenibles»): MUEREN la pintura de Israpony (43), EL ZENIT
        /// (44) y las SEIS armas del mod (45-50 — nunca obtenibles).
        /// En su lugar: los CUATRO picos restantes del Señor de la Luna
        /// (43: Terrariano y Celebration Mk2 · 44: Cetro de Cristal
        /// Arcoíris y Destello Lunar — armas de VERDAD, obtenibles)… y
        /// desde el 45, LA OFRENDA MINERAL (ver MineralDe): los
        /// lingotes del fin del mundo, en cantidades que crecen con el
        /// nivel — que NO necesitan corrupción («al ser minerales no
        /// necesitan estar corruptos ya que no se corrompen como las
        /// armas»).</summary>
        public static readonly int[][] ArmasDelNivel =
        {
            // (índice 0 vacío — el nivel 1 vive en [1])
            new int[] { 0 },
            new int[] { ItemID.CopperShortsword, ItemID.CopperBroadsword, ItemID.TinShortsword, ItemID.TinBroadsword },                    // 1  el cobre/estaño de la letra
            new int[] { ItemID.IronBroadsword, ItemID.LeadBroadsword },                                                                   // 2  el hierro/plomo
            new int[] { ItemID.SilverBroadsword, ItemID.TungstenBroadsword },                                                                // 3  la plata/tungsteno
            new int[] { ItemID.GoldBroadsword, ItemID.PlatinumBroadsword },                                                                  // 4  el oro/platino
            new int[] { ItemID.LightsBane, ItemID.BloodButcherer },                                                                          // 5  la demonita/carmesí
            new int[] { ItemID.BladeofGrass },                                                                                               // 6  la jungla
            new int[] { ItemID.BeeKeeper },                                                                                                  // 7  la Reina Abeja
            new int[] { ItemID.Muramasa },                                                                                                   // 8  la mazmorra
            new int[] { ItemID.FieryGreatsword },                                                                                            // 9  el infierno
            new int[] { ItemID.NightsEdge },                                                                                                 // 10 el Filo de la Noche
            new int[] { ItemID.Starfury },                                                                                                   // 11 las islas del cielo
            new int[] { ItemID.BluePhaseblade },                                                                                                 // 12 el meteorito
            new int[] { ItemID.Musket, ItemID.TheUndertaker, ItemID.BallOHurt, ItemID.TheMeatball, ItemID.TheRottedFork },                      // 13 los orbes/corazones (mundo sí, mundo no)
            new int[] { ItemID.Boomstick },                                                                                                  // 14 los santuarios de jungla
            new int[] { ItemID.IceBlade, ItemID.FalconBlade },                                                                               // 15 la nieve y las cajas flotantes
            new int[] { ItemID.BeesKnees, ItemID.BeeGun },                                                                                   // 16 la colmena (Reina Abeja)
            new int[] { ItemID.DarkLance },                                                                                                  // 17 la lanza oscura
            new int[] { ItemID.Sunfury },                                                                                                    // 18 los cofres de sombra del infierno
            new int[] { ItemID.Handgun },                                                                                                    // 19 la mazmorra profunda
            new int[] { ItemID.Katana },                                                                                                     // 20 el mercader viajero
            new int[] { ItemID.CobaltSword, ItemID.PalladiumSword },                                                                         // 21 el cobalto/paladio (MODO DIFÍCIL)
            new int[] { ItemID.MythrilSword, ItemID.OrichalcumSword },                                                                       // 22 el mitril/oricalco
            new int[] { ItemID.AdamantiteSword, ItemID.TitaniumSword },                                                                      // 23 la adamantita/titanio
            new int[] { ItemID.Excalibur },                                                                                                  // 24 la sagrada
            new int[] { ItemID.ChlorophyteClaymore, ItemID.ChlorophyteSaber },                                                               // 25 la clorofita
            new int[] { ItemID.TrueExcalibur, ItemID.TrueNightsEdge },                                                                       // 26 las verdaderas
            new int[] { ItemID.DeathSickle },                                                                                                // 27 el eclipse
            new int[] { ItemID.TheAxe, ItemID.GrenadeLauncher, ItemID.VenusMagnum },                                                         // 28 Plantera
            new int[] { ItemID.PossessedHatchet, ItemID.GolemFist, ItemID.StaffofEarth, ItemID.HeatRay },                                    // 29 el Gólem
            new int[] { ItemID.Tsunami, ItemID.Flairon, ItemID.RazorbladeTyphoon, ItemID.BubbleGun },                                        // 30 el Duque Fishron
            new int[] { ItemID.RainbowWhip, ItemID.EmpressBlade },                                                                             // 31 la Emperatriz de Luz (el látigo arcoíris y su hoja)
            new int[] { ItemID.TheHorsemansBlade, ItemID.BatScepter, ItemID.RavenStaff },                                                    // 32 la Luna de Calabaza
            new int[] { ItemID.BlizzardStaff, ItemID.ElfMelter, ItemID.SnowmanCannon, ItemID.ChristmasTreeSword },                           // 33 la Luna de Escarcha
            new int[] { ItemID.InfluxWaver, ItemID.XenoStaff, ItemID.LaserMachinegun },                                                      // 34 la Locura Marciana
            new int[] { ItemID.DD2BetsyBow, ItemID.DD2SquireBetsySword },                                                                            // 35 Betsy (el Arco Aéreo y el Dragón Volador)
            new int[] { ItemID.ChargedBlasterCannon },                                                                                       // 36 el cañón marciano RARO
            new int[] { ItemID.SolarEruption, ItemID.DayBreak },                                                                             // 37 la Torre Solar
            new int[] { ItemID.VortexBeater, ItemID.Phantasm },                                                                              // 38 la Torre del Vórtice
            new int[] { ItemID.NebulaBlaze, ItemID.NebulaArcanum },                                                                          // 39 la Torre de la Nebulosa
            new int[] { ItemID.StardustDragonStaff, ItemID.StardustCellStaff },                                                             // 40 la Torre del Polvo Estelar
            new int[] { ItemID.Meowmere, ItemID.StarWrath },                                                                                 // 41 el Señor de la Luna
            new int[] { ItemID.LastPrism, ItemID.SDMG },                                                                                     // 42 el Señor de la Luna (los dos picos)
            new int[] { ItemID.Terrarian, ItemID.MoonlordTurretStaff },                                                                    // 43 el Señor de la Luna (el yoyó y el Celebration Mk2 — v6.51.8: la PINTURA de Israpony MURIÓ; el nombre interno del Mk2 es MoonlordTurretStaff)
            new int[] { ItemID.RainbowCrystalStaff, ItemID.LunarFlareBook },                                                                // 44 el Señor de la Luna (cristal y destello — v6.51.8: EL ZENIT MUERE: «no creo que sea bueno que pida la cenit»)
            new int[] { },                                                                                                                   // 45 LA OFRENDA MINERAL (v6.51.8 — ver MineralDe)
            new int[] { },                                                                                                                   // 46 LA OFRENDA MINERAL
            new int[] { },                                                                                                                   // 47 LA OFRENDA MINERAL
            new int[] { },                                                                                                                   // 48 LA OFRENDA MINERAL
            new int[] { },                                                                                                                   // 49 LA OFRENDA MINERAL
            new int[] { },                                                                                                                   // 50 LA OFRENDA MINERAL (la gran ofrenda final)
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
        /// peldaños de armas 1-44 ofrecen el mineral como ALTERNATIVA).</summary>
        public static bool EsNivelMineral(int nivel)
            => nivel >= 45 && nivel <= NivelMax;

        /// <summary>EL MINERAL DEL PELDAÑO N (el lingote que pide — la
        /// escalera de metales QUE CORRESPONDE al nivel: el cobre del
        /// primer peldaño… la luminita del Señor de la Luna en la cima
        /// 37-48… la hongonita y el espectro en el final del mundo).
        /// v6.51.9: definido EN TODA LA ESCALERA 1-50 — la alternativa
        /// mineral vive en cada peldaño de armas.</summary>
        public static int MineralDe(int nivel)
        {
            if (nivel < 1 || nivel > NivelMax) return -1;   // no es peldaño
            if (nivel == 50) return ItemID.SpectreBar;      // LA GRAN OFRENDA FINAL
            if (nivel == 49) return ItemID.ShroomiteBar;    // la cima mineral del mundo vivo
            if (nivel >= 45) return ItemID.LunarBar;        // 45-48: la luminita del Señor de la Luna
            if (nivel >= 37) return ItemID.LunarBar;        // 37-44: las torres y el Señor de la Luna
            if (nivel >= 30) return ItemID.ShroomiteBar;    // 30-36: la era post-Plantera
            if (nivel >= 25) return ItemID.ChlorophyteBar;  // 25-29: la clorofita y sus verdaderas
            if (nivel >= 24) return ItemID.HallowedBar;     // 24: la sagrada (Excalibur)
            if (nivel >= 23) return ItemID.AdamantiteBar;   // 23: la adamantita/titanio
            if (nivel >= 22) return ItemID.MythrilBar;      // 22: el mitril/oricalco
            if (nivel >= 21) return ItemID.CobaltBar;       // 21: el cobalto/paladio (MODO DIFÍCIL)
            if (nivel >= 7) return ItemID.HellstoneBar;     // 7-20: la era del infierno (la pre-difícil larga)
            if (nivel >= 6) return ItemID.MeteoriteBar;     // 6: el meteorito
            if (nivel >= 5) return ItemID.DemoniteBar;      // 5: la demonita/carmesí
            if (nivel >= 4) return ItemID.GoldBar;          // 4: el oro/platino
            if (nivel >= 3) return ItemID.SilverBar;        // 3: la plata/tungsteno
            if (nivel >= 2) return ItemID.IronBar;          // 2: el hierro/plomo
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

        /// <summary>¿CUÁNTOS LINGOTES del peldaño lleva ESTE jugador en
        /// el inventario? (el conteo VIVO del requisito — para la lista
        /// del despertar y su ✓).</summary>
        public static int MineralesEnManos(Player p, int nivel)
        {
            int tipo = MineralDe(nivel);
            if (p == null || tipo < 0) return 0;
            int total = 0;
            for (int k = 0; k < 58; k++)
            {
                Item it = p.inventory[k];
                if (it != null && !it.IsAir && it.type == tipo) total += it.stack;
            }
            return total;
        }

        /// <summary>¿LLEVA YA LA OFRENDA MINERAL COMPLETA? (el ✓ de la
        /// lista — listo para soltar la pila ante el libro). v6.51.9:
        /// vale en TODA la escalera — en los peldaños de armas es la
        /// ALTERNATIVA («O la ofrenda mineral»), en la cima es la
        /// ofrenda misma.</summary>
        public static bool LlevaMineralCompleto(Player p, int nivel)
            => nivel >= 1 && nivel <= NivelMax &&
               MineralesEnManos(p, nivel) >= CantidadMineralDe(nivel);

        /// <summary>¿Es ESTA arma la del peldaño del nivel N?</summary>
        public static bool EsArmaDelNivel(int nivel, Item arma)
        {
            if (arma == null || arma.IsAir) return false;
            if (nivel < 1 || nivel > NivelMax) return false;
            int[] peldaño = ArmasDelNivel[nivel];
            for (int i = 0; i < peldaño.Length; i++)
                if (peldaño[i] == arma.type) return true;
            return false;
        }

        /// <summary>El NOMBRE de la ofrenda del nivel N (para la lista de
        /// requisitos de la ventana de información — la primera arma del
        /// peldaño; en los peldaños MINERALES, el lingote de la ofrenda).</summary>
        public static string NombreOfrenda(int nivel)
        {
            if (nivel < 1 || nivel > NivelMax) return "—";
            if (EsNivelMineral(nivel))
            {
                try
                {
                    var proto = ContentSamples.ItemsByType[MineralDe(nivel)];
                    if (proto != null) return proto.Name;
                }
                catch { }
                return "—";
            }
            int[] peldano = ArmasDelNivel[nivel];
            if (peldano.Length == 0) return "—";
            int tipo = peldano[0];
            Item proto2 = null;
            try { proto2 = ContentSamples.ItemsByType[tipo]; } catch { }
            return proto2 != null ? proto2.Name : "—";
        }

        /// <summary>¿Cuántas esencias de ESTE jugador hay en su inventario?</summary>
        public static int EsenciasEnManos(Player p)
        {
            if (p == null) return 0;
            int tipo = ModContent.ItemType<global::AethonMod.Content.Items.Altar.ItemCorrupto>();
            int total = 0;
            for (int k = 0; k < 58; k++)
            {
                Item it = p.inventory[k];
                if (it != null && !it.IsAir && it.type == tipo) total += it.stack;
            }
            return total;
        }

        /// <summary>v6.51.5 — ¿Este jugador lleva UNA ARMA CORRUPTA de este
        /// tipo en el inventario? (para la lista de requisitos VIVA: la
        /// fila de sprites del peldaño arde con halo dorado en las que ya
        /// lleva corruptas — la letra: «deberia mostrar cuando cumples uno
        /// de ellos, ademas deberia mostrar el sprite del arma muy pequeño
        /// para saber que arma se necesita»).</summary>
        public static bool LlevaCorrupta(Player p, int tipoArma)
        {
            if (p == null) return false;
            for (int k = 0; k < 58; k++)
            {
                Item it = p.inventory[k];
                if (it == null || it.IsAir || it.type != tipoArma) continue;
                if (it.GetGlobalItem<global::AethonMod.Content.Globals.CorruptoGlobalItem>().Corrompido)
                    return true;
            }
            return false;
        }

        /// <summary>Consume N esencias corruptas del inventario (devuelve
        /// false si no alcanzaban — nadie toca nada entonces).</summary>
        public static bool ConsumirEsencias(Player p, int cantidad)
        {
            if (EsenciasEnManos(p) < cantidad) return false;
            int tipo = ModContent.ItemType<global::AethonMod.Content.Items.Altar.ItemCorrupto>();
            int falta = cantidad;
            for (int k = 0; k < 58 && falta > 0; k++)
            {
                Item it = p.inventory[k];
                if (it == null || it.IsAir || it.type != tipo) continue;
                int quita = Math.Min(falta, it.stack);
                it.stack -= quita;
                falta -= quita;
                if (it.stack <= 0) p.inventory[k].TurnToAir();
            }
            return falta <= 0;
        }

        // ================================================================
        //  LOS PODERES (los umbrales de la letra)
        // ================================================================

        /// <summary>Las MEJORAS de los ojos desbloqueadas (la letra:
        /// «a nivel 5 de corrupción el grimorio desbloquea la primera
        /// mejora de los minions… a nivel 15 la mejora 2… a nivel 30 la
        /// mejora 3… a nivel 40 la última mejora, la 4»).</summary>
        public static int MejorasDesbloqueadas(int despertar)
        {
            if (despertar >= 40) return 4;
            if (despertar >= 30) return 3;
            if (despertar >= 15) return 2;
            if (despertar >= 5) return 1;
            return 0;
        }

        /// <summary>¿El SOL RÚNICO COMBINADO ya vive? (nivel 7: «a nivel 7
        /// se desbloquea un nuevo ataque para los minions… 2 minions con
        /// la primera mejora combinan su ataque y crean un sol runico de
        /// 1 anillo»).</summary>
        public static bool SolDisponible(int despertar) => despertar >= 7;

        /// <summary>Los ANILLOS del sol combinado (1 → nivel 7 · 2 →
        /// nivel 17 («el sol runico aumenta su ataque y sus anillos,
        /// antes 1 anillo ahora 2») · 3 → nivel 35).</summary>
        public static int AnillosDelSol(int despertar)
        {
            if (despertar >= 35) return 3;
            if (despertar >= 17) return 2;
            return despertar >= 7 ? 1 : 0;
        }

        /// <summary>Los SOLES por minuto (2 base · 3 en el 17 · 4 en el
        /// 35 — «este ataque se puede hacer 2 veces por minuto»).</summary>
        public static int SolesPorMinuto(int despertar)
        {
            if (despertar >= 35) return 4;
            if (despertar >= 17) return 3;
            return despertar >= 7 ? 2 : 0;
        }

        /// <summary>¿Los AGUJEROS NEGROS COMBINADOS ya viven? (nivel 10:
        /// «al tener 5 o más minion con la primera mejora, estos pueden
        /// combinar su ataque y lanzar un agujero negro aleatorio de 4
        /// listados»).</summary>
        public static bool AgujerosDisponibles(int despertar) => despertar >= 10;

        /// <summary>Los AGUJEROS por minuto (1 base · 2 en el 45: «antes
        /// solo podían lanzar 1 cada minuto, ahora se pueden lanzar 2
        /// cada minuto»).</summary>
        public static int AgujerosPorMinuto(int despertar)
            => despertar >= 45 ? 2 : (despertar >= 10 ? 1 : 0);

        /// <summary>¿La versión ASCENDIDA de los agujeros? (nivel 25: «se
        /// mejoran los ataques de los agujeros negros a su versión de
        /// Ascendido haciendo aún más daño»).</summary>
        public static bool AgujerosAscendidos(int despertar) => despertar >= 25;

        /// <summary>¿El SUPREMO/AURORA ya vive? (nivel 45: «se agrega un
        /// nuevo tipo de agujero negro runico independiente… Agujero
        /// Negro Supremo y Agujero Supremo Aurora… el ataque combinado de
        /// 8 minions o más en su mejora número 4»).</summary>
        public static bool SupremoDisponible(int despertar) => despertar >= 45;

        /// <summary>La CLAVE del anuncio del hito de este nivel (o null si
        /// el nivel no desbloquea nada — la lista pequeña de la letra).</summary>
        public static string ClaveHito(int nivel)
        {
            switch (nivel)
            {
                case 5: return "Mods.AethonMod.Despertar.HitoMejora1";
                case 7: return "Mods.AethonMod.Despertar.HitoSol";
                case 10: return "Mods.AethonMod.Despertar.HitoAgujeros";
                case 15: return "Mods.AethonMod.Despertar.HitoMejora2";
                case 17: return "Mods.AethonMod.Despertar.HitoSol2";
                case 20: return "Mods.AethonMod.Despertar.HitoMascota";
                case 25: return "Mods.AethonMod.Despertar.HitoAscendidos";
                case 30: return "Mods.AethonMod.Despertar.HitoMejora3";
                case 35: return "Mods.AethonMod.Despertar.HitoSol3";
                case 40: return "Mods.AethonMod.Despertar.HitoMejora4";
                case 45: return "Mods.AethonMod.Despertar.HitoSupremo";
                case 50: return "Mods.AethonMod.Despertar.HitoMaximo";
                default: return null;
            }
        }

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

        /// <summary>LA ESCALERA DE HITOS COMPLETA (5→50) — qué mejora
        /// aparece y EN QUÉ NIVEL (la letra: «cuando es que aparecen las
        /// mejoras en sus respectivos niveles»).</summary>
        public static readonly PasoDelCamino[] ElCaminoDelDespertar =
        {
            new PasoDelCamino(5,  "Mods.AethonMod.Despertar.CaminoMejora1"),
            new PasoDelCamino(7,  "Mods.AethonMod.Despertar.CaminoSol"),
            new PasoDelCamino(10, "Mods.AethonMod.Despertar.CaminoAgujeros"),
            new PasoDelCamino(15, "Mods.AethonMod.Despertar.CaminoMejora2"),
            new PasoDelCamino(17, "Mods.AethonMod.Despertar.CaminoSol2"),
            new PasoDelCamino(20, "Mods.AethonMod.Despertar.CaminoMascota"),
            new PasoDelCamino(25, "Mods.AethonMod.Despertar.CaminoAscendidos"),
            new PasoDelCamino(30, "Mods.AethonMod.Despertar.CaminoMejora3"),
            new PasoDelCamino(35, "Mods.AethonMod.Despertar.CaminoSol3"),
            new PasoDelCamino(40, "Mods.AethonMod.Despertar.CaminoMejora4"),
            new PasoDelCamino(45, "Mods.AethonMod.Despertar.CaminoSupremo"),
            new PasoDelCamino(50, "Mods.AethonMod.Despertar.CaminoMaximo"),
        };
    }
}
