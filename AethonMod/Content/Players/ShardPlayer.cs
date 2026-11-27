using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using AethonMod.Content.Systems;
using AethonMod.Content.Globals;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Players
{
    public class ShardPlayer : ModPlayer
    {
        public int ResonanceShards = 0;
        public bool FirstLevelUpTriggered = false;

        // ================================================================
        //  v6.47 — LA VOZ DEL HAMBRE
        // ================================================================
        //
        // Con el libro a nivel alto, cada rato sin matar es un MOMENTO
        // DE HAMBRE: EcoLib susurra y la barra dorada paladece. Al cuarto
        // momento (~5 minutos), el libro pierde la paciencia y GRIMORIO
        // FURIOSO llama a su comida (GrimorioFuriaSistema — las oleadas).
        //
        // La hambre solo cuenta con el libro VISIBLE (barra rápida):
        // guardado, el libro DUERME (la hambre se congela, no se olvida).
        // Una kill del portador lo alimenta: hambre a cero.

        /// <summary>Ticks desde la última kill del portador (con libro visible).</summary>
        public int TicksSinMatar = 0;
        /// <summary>Momentos de hambre acumulados (0..10).</summary>
        public int MomentosHambre = 0;

        // ================================================================
        //  v6.48 — EL LIBRO CELOSO · LA CRÓNICA · EL JUICIO
        // ================================================================

        /// <summary>
        /// EL LIBRO CELOSO: el tipo del último ítem sostenido — si el
        /// portador EMPIÑA OTRA ARMA mientras el libro tiene hambre, el
        /// susurro celoso (“¿Eso también mata?”) según la CLASE del arma
        /// (melé, magia, invocación, arrojadiza, arco o bala). Pura voz:
        /// cero código de gameplay.
        /// </summary>
        private int _ultimaArmaSostenida = -1;
        /// <summary>El frío entre susurros de celos (no repite a cada swap).</summary>
        private int _ticksCelos = 0;

        /// <summary>
        /// LA CRÓNICA DEL FESTÍN: los jefes que EL LIBRO ha devorado con
        /// ESTE portador (los mata GlobalNPCXP al cobrar la kill).
        /// v6.50.61 — LA PURGA: el Testigo murió — la crónica queda como
        /// REGISTRO silencioso del festín (viaja con EcoRed.MsgCronica y
        /// persiste en el .plr: los guardados viejos la conservan).
        /// </summary>
        public System.Collections.Generic.List<int> CronicaJefes = new System.Collections.Generic.List<int>();

        /// <summary>
        /// v6.50.110 — LA PRIMERA CENA: los tipos de jefe que el grimorio
        /// YA devoró con ESTE portador. La letra del usuario: «que esto
        /// suceda la primera vez que se mate un jefe y solo la primera
        /// vez, ya despues de la primera vez el jefe muere de forma
        /// normal» — ESTA lista hace «solo la primera vez»: un tipo de
        /// jefe, UNA cena. Persiste en el .plr (un guardado viejo trae
        /// la lista vacía: todo jefe estrena su primera cena).
        /// </summary>
        public System.Collections.Generic.List<int> JefesDevorados = new System.Collections.Generic.List<int>();

        /// <summary>v6.170 — LAS HISTORIAS DEL CRONISTA (la letra: «es
        /// quien desbloquea finalmente esa parte del lore en el libro
        /// de informacion»): bits 0..4 = las cinco secciones del
        /// archivo viejo (El devorador de finales · El sistema del
        /// pacto · El ciclo del portador · La respuesta · El canon
        /// asentado) que el Cronista YA contó a ESTE portador. El
        /// despertar ALCANZADO no basta: hay que ESCUCHARLO — él
        /// narra, el libro abre.</summary>
        public int LoreDesbloqueado;

        /// <summary>v6.137 — LA CUENTA DE LA ARENA: cuántas veces mató
        /// el portador a CADA jefe DENTRO del mundo interior (pares
        /// tipo→kills — el poder del jefe interior crece +25% por cada
        /// una; los kills de afuera no cuentan: la arena es la que
        /// entrena).</summary>
        public System.Collections.Generic.List<int> MuertesInteriorTipos = new System.Collections.Generic.List<int>();
        public System.Collections.Generic.List<int> MuertesInteriorCuenta = new System.Collections.Generic.List<int>();

        /// <summary>
        /// v6.50.115 — ¿Ya ocurrió EL FESTÍN DEL GUARDIÁN? El easter egg de
        /// la Mazmorra (la letra: «el guardia intenta atacar al jugador
        /// pero antes de que toque al jugador el grimorio lo detiene y
        /// hace la animación de devorarlo, esto solo lo hace una vez y
        /// el grimorio lo dice… esta acción con el guardián le da 1
        /// nivel completo»): UNA VEZ por portador — la marca persiste en
        /// el .plr. PaginaEnBlanco también la borra (el probador de
        /// primeras veces).</summary>
        public bool GuardianMazmorraDevorado = false;

        /// <summary>
        /// v6.50.116 — LA GRACIA DEL GUARDIÁN (la letra: «debes bloquear
        /// la aparición de guardianes… después de ser devorado por al
        /// menos 2 minutos… luego hay 2 minutos de gracia antes de que
        /// vuelvan a aparecer»): el reloj de la tregua — desde el
        /// mordisco del easteregg, este contador de ticks (7200 = 2 min)
        /// corre hacia atrás y mientras viva, la mazmorra NO engendra
        /// guardianes (FaucesGlobalNPC.EditSpawnPool). NO viaja al .plr:
        /// la gracia es de LA sesión (el pacto en sí es el que persiste).
        /// </summary>
        public int GuardianGracia = 0;

        /// <summary>
        /// v6.50.117 — LA CAÍDA (la letra: «un grimorio por jugador, cada
        /// jugador solo puede tener 1, si ya tiene uno no puede tomar
        /// otro»): ¿ESTE portador ya recibió su Grimorio del Eterno de
        /// LA CAÍDA? La marca persiste en el .plr (un guardado viejo trae
        /// false: la primera noche avanzada le espera su cometa). La lee
        /// LaCaidaSistema para decidir si el cielo aún le debe un libro.
        /// </summary>
        public bool CaidaRecibida = false;

        /// <summary>v6.51.0 — EL DESPERTAR DEL GRIMORIO… v6.51.6 — ESTE
        /// campo es LEGADO: el despertar VIVE EN EL ITEM (AlmaEternoGlobal
        /// Item — la letra: «eso debe ser algo individual de cada
        /// grimorio del eterno»). Solo sirve para la MIGRACIÓN de los
        /// guardados de la .51.0–.51.5: al entrar al mundo, esto camina
        /// AL PRIMER GRIMORIO del inventario (ver AlmaLegacyPendiente).</summary>
        public int NivelDespertar;

        /// <summary>v6.51.6 — LA MIGRACIÓN PENDIENTE: el guardado trae el
        /// despertar/festín del JUGADOR (la era .51.0–.51.5) — el primer
        /// tick con un Eterno en el inventario lo hereda TODO y el jugador
        /// queda limpio (una sola vez, para siempre).</summary>
        public bool AlmaLegacyPendiente = false;

        /// <summary>v6.51.0 — LA MASCOTA DEL ESCUPIDIZO… v6.51.6 — LEGADO
        /// (vive en el item — ver AlmaEternoGlobalItem.PetDelDespertar).</summary>
        public bool PetDelDespertar;

        /// <summary>v6.50.120 — LA BOLSA A LA MANO (la letra: «además no
        /// tengo la bolsa, no olvides que debes dárselo al jugador»):
        /// ¿ESTE portador ya recibió LA BOLSA DE LA CRÍA directamente en
        /// su inventario? La primera victoria de la familia de jefes (un
        /// gemelo final o EL RECUERDO COMPLETO) se la ENTREGA a la mano
        /// — una sola vez por jugador, para siempre: la pareja de mascotas
        /// (la cría de luz y su gemela oscura) es suya aunque la pierda.
        /// La lee AethonBoss.DropBotin para decidir si la entrega corre.</summary>
        public bool BolsaCriaEntregada = false;

        /// <summary>
        /// v6.50.128 — EL PODER DEVORADO (la letra: «la mecanica es la de
        /// comerse los item y mejorar el grimorio del eterno»): cada
        /// esencia corrupta que el Eterno traga en EL ALTAR DE LA CAÍDA
        /// suma UNO aquí — para SIEMPRE (persiste en el .plr). Cada punto
        /// es +0,25 % de daño al Grimorio del Eterno (tope +75 %) y cada
        /// 75 puntos despiertan UN OJO más en la corona (tope +3).
        /// </summary>
        public int DevoracionPoder = 0;

        /// <summary>EL LÍMITE DEL PODER: 300 esencias = el +75 % entero.</summary>
        public const int DevoracionTope = 300;

        // ================================================================
        //  v6.51.11 — EL FESTÍN DEL PORTADOR (la letra del usuario: «sobre
        //  las esencia corrupta estas si las consume el jugador aumentan
        //  su ataque siguen aumentando hasta un maximo de 100 % en
        //  esencia consumidas por el jugador independientemente de lo
        //  que consuma el grimorio, las esencias consumidas por el
        //  grimorio no cuentan para el aumento general de daño, solo
        //  cuentan para su despertar, cuando el jugador cumpla con el
        //  limite de esencias consumida y llegue al tope que es 100% de
        //  daño o sea que el jugador consuma 1000 esencias corruptas,
        //  entonces las esencias se convierten en monedas de oro, 1
        //  esencia 1 de oro»): cada esencia corrupta que EL JUGADOR se
        //  come (ItemCorrupto.UseItem) es +0,1 % de daño en TODO lo que
        //  haga — SIN importar el arma. Lo que el GRIMORIO come no
        //  cuenta aquí (solo despierta). Al tope (1000 = +100 %), cada
        //  esencia consumida se vuelve 1 MONEDA DE ORO.
        // ================================================================
        public int EsenciasConsumidas = 0;

        /// <summary>v6.51.12 — EL RELOJ LARGO DEL CORO (la letra: «la
        /// pequeña animación de las hadas del minigrimorio que suceda
        /// al menos 1 vez cada 2 horas de juego si es que el jugador
        /// pasa mas de 30 segundos sin moverse, en cualquier caso si no
        /// aparece esa posibildad simplemente no sucede»): los ticks de
        /// juego desde el ÚLTIMO coro de hadas de la mascota — al llegar
        /// a CoroTicks (2 h) y con el dueño quieto ≥30 s, el coro es
        /// GARANTIZADO (la moneda de la .51.1 murió). Persiste en el
        /// .plr: las 2 h son DE JUEGO, no de sesión.</summary>
        public int TicksSinCoro = 0;

        /// <summary>EL TOPE DEL FESTÍN DEL PORTADOR: 1000 esencias = el +100 % entero.</summary>
        public const int EsenciasTope = 1000;

        /// <summary>EL BONUS DE DAÑO del portador por las esencias que ÉL
        /// comió (+0,1 % por esencia, tope +100 % — TODO su daño).</summary>
        public static float BonusDanoEsencias(Player p)
        {
            if (p == null) return 0f;
            var sp = p.GetModPlayer<ShardPlayer>();
            return sp == null ? 0f
                : System.Math.Min(sp.EsenciasConsumidas, EsenciasTope) * 0.001f;
        }

        /// <summary>v6.51.11 — LA COMIDA DEL PORTADOR: el daño de TODO lo
        /// que usa sube +0,1 % por esencia corrupta consumida (tope
        /// +100 % con 1000 — la letra: «aumentan su ataque… hasta un
        /// maximo de 100 %»). v6.51.12 — LIGADO AL GRIMORIO (la letra
        /// nueva: «aunque sea el jugador quien se coma 1000 esencias
        /// corruptas solo con el grimorio se activa ese poder»): el
        /// festín SOLO vive mientras el libro esté SELECCIONADO (100 %)
        /// o EN EL INVENTARIO (25 %) — guardado en cofre o botado a la
        /// basura, el poder APAGA (FactorDelLibro).</summary>
        public override void ModifyWeaponDamage(Item item, ref StatModifier damage)
        {
            if (EsenciasConsumidas <= 0) return;
            float factor = global::AethonMod.Content.Globals.AlmaEternoGlobalItem
                .FactorDelLibro(Player);
            if (factor <= 0f) return;   // sin libro encima: el festín duerme
            damage *= 1f + System.Math.Min(EsenciasConsumidas, EsenciasTope)
                * 0.001f * factor;
        }

        /// <summary>EL BONUS DE DAÑO del Eterno por lo devorado. v6.51.11 —
        /// MUERTO (la letra: lo que el grimorio come SOLO despierta — el
        /// daño por esencias vive ahora en el PORTADOR que las come,
        /// BonusDanoEsencias): la forma queda por compatibilidad y
        /// devuelve 0.</summary>
        public static float BonusDanoDevorado(Player p)
        {
            return 0f;
        }

        /// <summary>LOS OJOS EXTRA de la corona por lo devorado. v6.51.11 —
        /// MUERTOS con el poder devorado (la letra: lo que el libro come
        /// SOLO despierta): la forma queda por compatibilidad y devuelve 0.</summary>
        public static int OjosExtraDevorados(Player p)
        {
            return 0;
        }

        // ================================================================
        //  v6.50.129 — EL FESTÍN DE LAS ARMAS (la letra: «cuualquier arma
        //  corrompida se vuelve exp para el grimorio a mayor la rareza
        //  del arma mayor los exp. ademas cada arma debe subir algun
        //  stats del grimorio ya sea un stats unico o no, en esto caso
        //  puedes crear stats y efectos para las armas en caso de que
        //  estas tengan efectos o sean especiales»). TODO persiste en
        //  el .plr y viaja por EcoRed.MsgDevoracion (el mismo cauce del
        //  poder — el daño del Eterno se calcula en la máquina del
        //  portador: los números tienen que llegar).
        // ================================================================

        /// <summary>EL EXP DEVORADO (la rareza de cada arma comida —
        /// el contador del festín, +0,02 % de daño por punto tope +40 %).</summary>
        public long DevoracionExp = 0;

        /// <summary>LA FUERZA VIOLENTA: cada arma CUERPO A CUERPO comida
        /// suma 1 (+0,35 % daño del Eterno por punto, tope conjunto +45 %).</summary>
        public int DevoracionFuerza = 0;

        /// <summary>LA PRISA DEL VUELO: cada arma A DISTANCIA comida suma
        /// 1 (+0,25 % velocidad de uso por punto, tope conjunto +35 %).</summary>
        public int DevoracionPrisa = 0;

        /// <summary>LA MIRADA PUNTERA: cada arma MÁGICA comida suma 1
        /// (+0,30 % prob. de crítico por punto, tope conjunto +30 %).</summary>
        public int DevoracionMirada = 0;

        /// <summary>LA CORONA DE OJOS: cada arma DE INVOCACIÓN comida suma
        /// 1 (cada 5 puntos despiertan UN OJO más en la corona, tope +3).</summary>
        public int DevoracionCorona = 0;

        /// <summary>LA MORDIDA: cada arma OTRA (lanzables y raridades)
        /// comida suma 1 (+0,6 % empuje por punto, tope conjunto +75 %).</summary>
        public int DevoracionMordida = 0;

        // === LAS ALMAS ÚNICAS (las armas ESPECIALES de la letra: «puedes
        //     crear stats y efectos para las armas en caso de que estas
        //     tengan efectos o sean especiales») — cada copia comida
        //     despierta SU alma en el libro, con SU efecto. ===

        /// <summary>ALMA ÍGNEA (las armas de fuego: Fiery Greatsword,
        /// Sunfury, Flamelash, Inferno Fork, Flamethrower, Volcano):
        /// +6 % de prender por copia (tope 30 %).</summary>
        public int AlmaIgnea = 0;

        /// <summary>ALMA SALVAJE (las armas de veneno: Blade of Grass,
        /// Thorn Chakram, Bee Gun, Beenade, Poison Staff): +6 % de
        /// envenenar por copia (tope 30 %).</summary>
        public int AlmaSalvaje = 0;

        /// <summary>ALMA VAMPIRA (Vampire Knives): +2 % de robo de vida
        /// por copia (tope 8 %).</summary>
        public int AlmaVampira = 0;

        /// <summary>CORAZÓN ESTELAR (Starfury, Star Wrath, Star Cannon):
        /// +1,5 % crítico por copia (tope +7,5 % — suma con la Mirada).</summary>
        public int CorazonEstelar = 0;

        /// <summary>ALMA VELOZ (Muramasa, Katana, Fetid Baghnakhs): +2 %
        /// velocidad de uso por copia (tope +10 % — suma con la Prisa).</summary>
        public int AlmaVeloz = 0;

        /// <summary>PUÑO TITÁN (Paladin's Hammer, Golem Fist): +12 %
        /// empuje por copia (tope +60 % — suma con la Mordida).</summary>
        public int PunoTitan = 0;

        /// <summary>EL DAÑO DEL FESTÍN DE ARMAS: fuerza (+0,35 %/pto) + el
        /// EXP (+0,02 %/pto, tope +40 %) — tope conjunto +45 %.</summary>
        /// <summary>v6.51.6 — LOS NÚMEROS SON DEL LIBRO ACTIVO (la letra:
        /// las estadísticas son exclusivas de cada grimorio): esta lectura
        /// resuelve el Eterno en la MANO (o el primero del inventario) y
        /// devuelve SUS números — el cálculo real vive en
        /// AlmaEternoGlobalItem (el patrón de siempre, ahora por item).</summary>
        public static float BonusDanoArmasDevoradas(Player p)
        {
            return global::AethonMod.Content.Globals.AlmaEternoGlobalItem
                .BonusDanoArmasDevoradas(global::AethonMod.Content.Globals.AlmaEternoGlobalItem.Activo(p));
        }

        /// <summary>LA VELOCIDAD DEL FESTÍN: prisa (+0,25 %/pto) + alma
        /// veloz (+2 %/copa) — tope conjunto +35 %.</summary>
        /// <summary>v6.51.6 — LOS NÚMEROS SON DEL LIBRO ACTIVO (la letra:
        /// las estadísticas son exclusivas de cada grimorio): esta lectura
        /// resuelve el Eterno en la MANO (o el primero del inventario) y
        /// devuelve SUS números — el cálculo real vive en
        /// AlmaEternoGlobalItem (el patrón de siempre, ahora por item).</summary>
        public static float BonusVelocidadArmasDevoradas(Player p)
        {
            return global::AethonMod.Content.Globals.AlmaEternoGlobalItem
                .BonusVelocidadArmasDevoradas(global::AethonMod.Content.Globals.AlmaEternoGlobalItem.Activo(p));
        }

        /// <summary>EL CRÍTICO DEL FESTÍN: mirada (+0,30 %/pto) + corazón
        /// estelar (+1,5 %/copia) — tope conjunto +30 %.</summary>
        /// <summary>v6.51.6 — LOS NÚMEROS SON DEL LIBRO ACTIVO (la letra:
        /// las estadísticas son exclusivas de cada grimorio): esta lectura
        /// resuelve el Eterno en la MANO (o el primero del inventario) y
        /// devuelve SUS números — el cálculo real vive en
        /// AlmaEternoGlobalItem (el patrón de siempre, ahora por item).</summary>
        public static float BonusCritArmasDevoradas(Player p)
        {
            return global::AethonMod.Content.Globals.AlmaEternoGlobalItem
                .BonusCritArmasDevoradas(global::AethonMod.Content.Globals.AlmaEternoGlobalItem.Activo(p));
        }

        /// <summary>EL EMPUJE DEL FESTÍN: mordida (+0,6 %/pto) + puño
        /// titán (+12 %/copia) — tope conjunto +75 %.</summary>
        /// <summary>v6.51.6 — LOS NÚMEROS SON DEL LIBRO ACTIVO (la letra:
        /// las estadísticas son exclusivas de cada grimorio): esta lectura
        /// resuelve el Eterno en la MANO (o el primero del inventario) y
        /// devuelve SUS números — el cálculo real vive en
        /// AlmaEternoGlobalItem (el patrón de siempre, ahora por item).</summary>
        public static float BonusEmpujeArmasDevoradas(Player p)
        {
            return global::AethonMod.Content.Globals.AlmaEternoGlobalItem
                .BonusEmpujeArmasDevoradas(global::AethonMod.Content.Globals.AlmaEternoGlobalItem.Activo(p));
        }

        /// <summary>LOS OJOS DE LA CORONA DEL FESTÍN: cada 5 armas de
        /// invocación comidas, UN ojo más (tope +3 — suma con los del
        /// poder devorado).</summary>
        /// <summary>v6.51.6 — LOS NÚMEROS SON DEL LIBRO ACTIVO (la letra:
        /// las estadísticas son exclusivas de cada grimorio): esta lectura
        /// resuelve el Eterno en la MANO (o el primero del inventario) y
        /// devuelve SUS números — el cálculo real vive en
        /// AlmaEternoGlobalItem (el patrón de siempre, ahora por item).</summary>
        public static int OjosCoronaDevorados(Player p)
        {
            return global::AethonMod.Content.Globals.AlmaEternoGlobalItem
                .OjosCoronaDevorados(global::AethonMod.Content.Globals.AlmaEternoGlobalItem.Activo(p));
        }

        /// <summary>EL ROBO DE VIDA DEL ALMA VAMPIRA (+2 %/copia, tope 8 %).</summary>
        /// <summary>v6.51.6 — LOS NÚMEROS SON DEL LIBRO ACTIVO (la letra:
        /// las estadísticas son exclusivas de cada grimorio): esta lectura
        /// resuelve el Eterno en la MANO (o el primero del inventario) y
        /// devuelve SUS números — el cálculo real vive en
        /// AlmaEternoGlobalItem (el patrón de siempre, ahora por item).</summary>
        public static float RoboVidaAlmas(Player p)
        {
            return global::AethonMod.Content.Globals.AlmaEternoGlobalItem
                .RoboVidaAlmas(global::AethonMod.Content.Globals.AlmaEternoGlobalItem.Activo(p));
        }

        /// <summary>LA PROBABILIDAD DE PRENDER del alma ígnea (+6 %/copia, tope 30 %).</summary>
        /// <summary>v6.51.6 — LOS NÚMEROS SON DEL LIBRO ACTIVO (la letra:
        /// las estadísticas son exclusivas de cada grimorio): esta lectura
        /// resuelve el Eterno en la MANO (o el primero del inventario) y
        /// devuelve SUS números — el cálculo real vive en
        /// AlmaEternoGlobalItem (el patrón de siempre, ahora por item).</summary>
        public static float ProbIgnea(Player p)
        {
            return global::AethonMod.Content.Globals.AlmaEternoGlobalItem
                .ProbIgnea(global::AethonMod.Content.Globals.AlmaEternoGlobalItem.Activo(p));
        }

        /// <summary>LA PROBABILIDAD DE ENVENENAR del alma salvaje (+6 %/copia, tope 30 %).</summary>
        /// <summary>v6.51.6 — LOS NÚMEROS SON DEL LIBRO ACTIVO (la letra:
        /// las estadísticas son exclusivas de cada grimorio): esta lectura
        /// resuelve el Eterno en la MANO (o el primero del inventario) y
        /// devuelve SUS números — el cálculo real vive en
        /// AlmaEternoGlobalItem (el patrón de siempre, ahora por item).</summary>
        public static float ProbSalvaje(Player p)
        {
            return global::AethonMod.Content.Globals.AlmaEternoGlobalItem
                .ProbSalvaje(global::AethonMod.Content.Globals.AlmaEternoGlobalItem.Activo(p));
        }

        /// <summary>v6.50.128 — EL SYNC MANUAL: el trago del altar tocó el
        /// poder de este portador (solo AUTORIDAD lo hace) — esto lo lleva
        /// por el cable AHORA (EcoRed.MsgDevoracion: el daño del Eterno se
        /// calcula en la máquina del portador, el número tiene que llegar;
        /// el host-local se entrega por el loopback de EnviarPaquete).</summary>
        public void SyncAhoraDevoracion()
        {
            try
            {
                EcoRed.SincronizarDevoracion(Player);
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("ShardPlayer", exRegistro); }
        }

        // ================================================================
        // v6.50.114 — EL TECHO DE LOS OJOS (cacheado por tick, por jugador)
        // ================================================================

        /// <summary>El techo de invocación de los ojos para cada jugador
        /// (cacheado en PostUpdateEquips: el ESCUADRÓN 10 de la .115 —
        /// PLANO, ya sin equipo ni nivel). Lo lee la compuerta del clic
        /// derecho del Eterno — NO maxMinions, que el techo vivo de la
        /// promesa .111 infla (el runaway de los ojos sin límite).</summary>
        private static readonly int[] _techoOjos = new int[Main.maxPlayers];
        /// <summary>El tick del cacheo (frío = aún no corrió el hook).</summary>
        private static readonly uint[] _techoTick = new uint[Main.maxPlayers];

        /// <summary>EL TECHO DE INVOCACIÓN de los ojos del Eterno para un
        /// jugador (caliente del cache si el hook ya corrió este tick;
        /// frío: el escuadrón 10 — el primer tick de todos).</summary>
        public static int TechoOjosDe(Player p, int nivel)
        {
            if (p == null) return WeaponScaling.TechoOjosEterno(0, nivel);
            int i = p.whoAmI;
            if (i >= 0 && i < _techoTick.Length && _techoTick[i] == Main.GameUpdateCount)
                return _techoOjos[i];
            return WeaponScaling.TechoOjosEterno(0, nivel);
        }

        /// <summary>
        /// v6.48 — EL CURSOR DE LA CRÓNICA: cuántas derrotas ya están
        /// registradas (dato histórico del Testigo — sin narrador hoy,
        /// el contador sigue siendo el índice de la próxima página).
        /// </summary>
        public int CronicaNarrada = 0;

        /// <summary>
        /// ¿El portador ya derrotó LA OLEADA 10 de la furia? La marca del
        /// festín completo — v6.50.61: la tienda murió con el Testigo,
        /// la marca queda como CONSTANCIA (persiste en el .plr).
        /// </summary>
        public bool DerrotaOleada10 = false;

        /// <summary>
        /// v6.50.59 — EL NIVEL DE FURIA del portador (1..10, persistente):
        /// «cuando inicia la primera oleada natural siempre debe empezar en
        /// nivel 1, luego si el jugador logra vencer el nivel 1 de esa
        /// oleada, la siguiente sera de nivel 2, o sea 2 oleadas…». El
        /// festín de nivel N trae N oleadas y TODO escala con N — vencer
        /// sube, morir congela.
        /// </summary>
        public int FuriaNivel = 1;

        /// <summary>v6.151 — EL % DE HAMBRE DEL LIBRO ACTIVO (0-100,
        /// continuo): lee EL RELOJ DE VERDAD (Estado.Hambre del Grimorio
        /// del Eterno — la misma máquina del ojo rojo y la barra del
        /// tooltip). Sin libro visible: la cuenta de momentos de siempre
        /// (MomentosHambre × 10 % — el respaldo que nunca miente).</summary>
        public int PorcientoHambreDelLibro()
        {
            try
            {
                var eterno = global::AethonMod.Content.Weapons.GrimoireEternal.EstadoEterno;
                if (eterno != null && eterno.Hambre > 0f)
                    return (int)Math.Min(100f, MathF.Floor(eterno.Hambre * 100f));
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("ShardPlayer", exRegistro); }
            return Math.Min(100, MomentosHambre * 10);
        }

        /// <summary>Segundos por momento de hambre ("si pasas minutos sin matar").</summary>
        // v6.142 — LA HORA DEL HAMBRE (la letra: «el indicador real es
        // el que se usa en el Grimorio Hambriento Nervioso… la
        // diferencia es que en el grimorio hambriento nervioso la
        // mecanica esta acelerada por motivos de prueba»): el Eterno
        // corre SU reloj normal — UNA HORA de 0 a 100% (la tasa del
        // arma: 1/216000). El reloj de los momentos CALZA con él:
        // 360 s por momento → el 90% (momento 9) a los 54 min y el
        // 100% a la hora exacta. Cuando LA OLEADA estalla, el ojo del
        // libro ya llegó ROJO del todo (la rampa del Nervioso: 20%→
        // 80% de hambre) — UNA sola verdad, la del arma.
        public const int SegundosPorMomento = 360;
        /// <summary>v6.140 — LA FURIA AL 90% (la letra: «el grimorio
        /// lanza el texto de tener hambre al 4%… las oleadas de hambre
        /// solo deben aparecer a partir de 90% de hambre y al llegar al
        /// 100% aparecer si o si con el llamado del grimorio»): la furia
        /// YA NO estalla al 40% (los viejos 4 momentos de 10) — el
        /// UMBRAL DE OLEADA es el 90% (momento 9) y el 100% (momento
        /// 10, el tope) es SIEMPRE. Los momentos 1-8 son EL SUSURRO
        /// (la voz del libro, informativa: «tengo hambre…» — sin
        /// oleada, sin aura). EL AURA TAMBIÉN ESPERA: no hay «aura
        /// rara» sin peligro real (la aura vive con MomentosHambre ≥ 9:
        /// cuando el aura prende, la oleada YA está prometida).
        /// v6.151 — LA LEY NUEVA (la letra: «asegurate de que el % de
        /// hambre sea el % de probabilidad de que se active una oleada
        /// de hambre, pero no sera tal cual lo presenta, si el libro
        /// tiene 89% de hambre o menos la probabilidad de que las
        /// oleadas de hambre se activen es de solo 1%, pero al llegar a
        /// 90% de hambre la probabilidad pasa al 5% y se incrementa 5%
        /// por cada punto extra, en 91% es 10%, y asi hasta llegar a
        /// 100% que directamente activa la oleada del nivel
        /// correspondiente»): LA TABLA — 0-89 %: 1 % · 90 %: 5 % ·
        /// 91 %: 10 % · … · 99 %: 50 % · 100 %: DIRECTA. Y el nivel es
        /// EL DE SIEMPRE: victoria sube, derrota mantiene (FuriaNivel).</summary>
        public const int MomentosParaFuria = 9;

        /// <summary>v6.151 — LA PROBABILIDAD DE OLEADA según el % de
        /// hambre del libro (0-100): la tabla de la letra — 1 % con
        /// 89 o menos, 5 % al 90, +5 % por punto extra, DIRECTA al 100.
        /// (La llama el chequeo de furia cada 5 s; el dado es de 100.)</summary>
        public static int ProbabilidadOleada(int porcentajeHambre)
        {
            if (porcentajeHambre >= 100) return 100;      // LA DIRECTA
            if (porcentajeHambre < 90) return 1;          // el ruido de fondo
            return 5 + (porcentajeHambre - 90) * 5;       // 90→5 · 91→10 · 99→50
        }
        /// <summary>El libro hambriento habla "a nivel alto" (25 = el segundo peldaño del festín).</summary>
        public const int NivelMinimoHambre = 25;
        /// <summary>Tope de momentos de hambre (= tope de oleadas).</summary>
        public const int MomentosMax = 10;

        /// <summary>
        /// v6.50.2 — LA SELECCIÓN DE LA CARNADA DEL REMOTO, vista por la
        /// AUTORIDAD: OleadasPreparadas (CarnadaDelGrimorio) es un estático
        /// POR MÁQUINA — en MP el clic derecho del remoto ciclaba SU
        /// contador local y el server ejecutaba el clic izquierdo (uso
        /// sincronizado) con SU propio default. EcoRed.MsgPrepararOleadas
        /// deposita aquí la selección para que el UseItem del server lea
        /// el número que el portador preparó.
        /// CICLO DE VIDA: vive la SESIÓN completa — NO se resetea en
        /// ResetEffects (se perdería a cada tick) ni al morir; NO se
        /// persiste en el .plr (la selección es un estado de la sesión,
        /// como el propio estático en SP). El default 0 = "nunca preparó"
        /// → el UseItem cae al estático local del server (SP/host).
        /// </summary>
        public int OleadasPreparadasRemoto;

        // ================================================================
        //  v6.46 — LOS TRES ESTADOS DEL LIBRO
        // ================================================================
        //
        // 1. SOSTENIDO: TODO el poder. El combate exige blandirlo.
        // 2. EN LA BARRA RÁPIDA (slots 0–9, sin sostener): el libro sigue
        //    COMIENDO XP (GlobalNPCXP), conserva los SLOTS DE MINION y la
        //    vida/maná extra — limitados a +100 cada uno.
        // 3. GUARDADO (inventario profundo, hucha/caja de seguridad/
        //    vaulta, cofre, suelo): NADA. Los minions YA invocados
        //    permanecen hasta que el jugador los desinvoque o mueran (el
        //    buff del orbe vive en el JUGADOR, no en el libro — vanilla no
        //    despawnea minions al bajar maxMinions), pero el libro no
        //    aporta nada hasta volver a la barra rápida.
        //
        // Las stats salen de la PRIMERA copia (sin stacking); la XP la
        // cobran TODAS las copias visibles. La detección se centraliza en
        // NivelLibro() para que todos los hooks cuenten la misma historia.

        /// <summary>
        /// v6.50.37 — EL NIVEL DEL GRIMORIO, EXPUESTO PARA EL MUNDO.
        /// Lo lee LA OSCURIDAD PRIMORDIAL: cuando Aethon concentra toda la
        /// luz del mundo, SOLO un Grimorio de nivel 50 o superior (en la
        /// mano o en la barra rápida — acompañándote) sostiene un círculo
        /// de luz alrededor del portador.
        /// </summary>
        public int NivelGrimorioPublico => NivelLibro(false);

        /// <summary>
        /// El nivel del libro que manda para el jugador.
        /// soloSostenido = true → SOLO el sostenido (poder de combate).
        /// soloSostenido = false → sostenido, o la primera copia de la
        /// barra rápida (capacidades que no exigen blandir: slots, vida,
        /// maná topeado, y la propia XP que GlobalNPCXP cobra en 0–9).
        /// Devuelve 0 si el libro no está en ningún sitio que cuente.
        /// v6.51.12 — LA PRESENCIA NUEVA (la letra: «si no esta
        /// seleccionado o esta guardado en el inventario el jugador
        /// solo obtiene 25 % del poder del grimorio»): el libro cuenta
        /// desde CUALQUIER slot del inventario (0–57 — antes solo la
        /// barra rápida 0–9). El 25 %/100 % lo aplica el FACTOR
        /// (FactorDelLibro) en cada stat — el nivel aquí solo dice
        /// «hay libro».
        /// </summary>
        private int NivelLibro(bool soloSostenido)
        {
            Item held = Player.HeldItem;
            if (held != null && held.type == ModContent.ItemType<Weapons.GrimoireEternal>())
            {
                try
                {
                    var s = held.GetGlobalItem<ShardLevelItem>();
                    if (s != null) return s.Level;
                }
                catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("ShardPlayer", exRegistro); }
            }
            if (soloSostenido) return 0;

            for (int i = 0; i < 58; i++)
            {
                Item inv = Player.inventory[i];
                if (inv == null || inv.type != ModContent.ItemType<Weapons.GrimoireEternal>())
                    continue;
                try
                {
                    var s = inv.GetGlobalItem<ShardLevelItem>();
                    if (s != null) return s.Level;
                }
                catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("ShardPlayer", exRegistro); }
                break; // la PRIMERA copia del inventario manda
            }
            return 0;
        }

        /// <summary>
        /// PostUpdateEquips: los bonuses que da el libro al portador —
        /// v6.51.12 — LA PRESENCIA NUEVA (la letra: «si esta seleccionado
        /// el jugador tiene todo el poder del grimorio, si no esta
        /// seleccionado o esta guardado en el inventario el jugador solo
        /// obtiene 25 % del poder del grimorio, si el grimorio esta
        /// guardado en cualquier tipo de cofre o este fue botado a la
        /// basura entonces el jugador no obtiene nada»): TODAS las stats
        /// multiplican por FactorDelLibro (1,0 / 0,25 / 0) — el techo de
        /// ojos, la vida, el maná, el crítico, la penetración. La
        /// separación sostenido/bolsa de la v6.46 MUERE: ahora es una
        /// sola regla de presencia.
        /// </summary>
        public override void PostUpdateEquips()
        {
            int nivel = NivelLibro(false);   // sostenido O cualquier slot del inventario

            // v6.51.12 — EL FACTOR DE PRESENCIA (la única regla nueva):
            // 1,0 EN LA MANO · 0,25 EN LA BOLSA · 0,0 NADA (cofre/basura/suelo).
            float factor = global::AethonMod.Content.Globals.AlmaEternoGlobalItem
                .FactorDelLibro(Player);

            // ============================================================
            // v6.50.114 — EL EQUIPO LIMPIO (se captura ANTES de tocar
            // maxMinions): lo que la armadura y los accesorios de
            // INVOCADOR suman MÁS ALLÁ de la base 1 de vanilla. Es la
            // materia prima del TECHO DE LOS OJOS (la letra: «un límite
            // de 20 con el libro y más con items y armadura de
            // invocador»). El cacheo por tick alimenta la compuerta de
            // invocación del Eterno (TechoOjosDe) — que lee ESTE número
            // y no maxMinions (el techo vivo de abajo lo infla, y era
            // EL BUG de los ojos sin límite: cada ojo vivo subía el
            // techo del siguiente).
            // ============================================================
            int equipoInvocador = Player.maxMinions - 1;
            if (equipoInvocador < 0) equipoInvocador = 0;
            // v6.51.0 — EL TECHO ESTRICTO DE LA LETRA: «la cantidad de
            // minions están sujetos a la subida normal de nivel del
            // grimorio, cada 10 niveles 1 minion nuevo, en nivel 5 puede
            // invocar su primer minion, en nivel 10 su segundo minion y a
            // partir de aquí cada 10 niveles 1 nuevo» — NI el equipo NI
            // lo devorado inflan el escuadrón (los ojos de banquete y de
            // corona de la .128-.129 MURIERON con el despertar).
            // v6.51.12 — EL TECHO TAMBIÉN VISTE EL FACTOR: en la mano el
            // escuadrón entero · en la bolsa el 25 % · guardado, cero.
            int techoOjos = (int)(WeaponScaling.TechoOjosEterno(equipoInvocador, nivel) * factor);
            {
                int i = Player.whoAmI;
                if (i >= 0 && i < _techoOjos.Length)
                {
                    _techoOjos[i] = techoOjos;
                    _techoTick[i] = Main.GameUpdateCount;
                }
            }

            // ============================================================
            // v6.50.111 — LA PROMESA CUMPLIDA (el fix de «a veces los
            // minion se desactivan por si solos").
            //
            // EL DESCUBRIMIENTO: vanilla CULLA minions CADA TICK dentro
            // de Projectile.Update — «si slotsMinions + minionSlots >
            // maxMinions → Kill()» (el comentario de abajo, «vanilla no
            // los despawnea al bajar maxMinions», era FALSO). El bonus
            // de slots del libro solo vive mientras está SOSTENIDO o en
            // la BARRA RÁPIDA: el tick en que el libro salía de ahí —
            // AL CURSOR al reorganizar el inventario, a un cofre, a la
            // hucha, al inventario profundo — el bonus se esfumaba y el
            // escuadrón entero moría EN UN SOLO TICK, «sin razón".
            //
            // LA LEY NUEVA: el techo PUEDE bajar (guardado el libro, ya
            // no se invoca más de lo que el cuerpo aguanta) — pero
            // JAMÁS por debajo de lo que YA ESTÁ VIVO: mientras el
            // libro siga contando (barra rápida) o quede algún OJO del
            // Eterno en el aire, los slots en uso son LEGALES.
            //
            // v6.51.3 — LA PROMESA COMPLETA (la letra del playtest: «es
            // raro, a veces los minions del grimorio se desactivan solo,
            // revisa eso, los minions deben persistir incluso si el
            // grimorio deja el inventario, solo se deben desactivar si
            // el jugador los quita usando su buff de minion»): la .111
            // solo protegía al OJO ETERNO — el resto del escuadrón
            // (Fragmento de Supernova, Living Pulsar, Nebula Jellyfish,
            // Stellar Comet y la sierpe) moría igual en cuanto el libro
            // salía de la mano/barra. AHORA la ley cubre a TODO el
            // escuadrón: mientras viva CUALQUIERA de los SEIS, lo vivo
            // es legal — el libro puede irse AL COFRE que los suyos
            // siguen en el aire (la única despedida legítima es
            // QUITAR EL BUFF, como manda la letra).
            // ============================================================
            {
                // EL ESCUADRÓN ENTERO (los 6 minions de la casa + LAS DAGAS
                // v6.124 — la promesa cubre también a LAS CINCO DAGAS DEL
                // ETERNO: mientras viva CUALQUIERA de las suyas, lo vivo es
                // legal — el libro puede irse AL COFRE que sus dagas siguen
                // en el aire, como sus ojos).
                int tipoOjo = ModContent.ProjectileType
                    <global::AethonMod.Content.Projectiles.OjoEternoMinion>();
                int tipoDaga = ModContent.ProjectileType
                    <global::AethonMod.Content.Projectiles.DagaEternaMinion>();
                int tipoSupernova = ModContent.ProjectileType
                    <global::AethonMod.Content.Projectiles.Cosmic.FragmentoSupernovaMinion>();
                int tipoPulsar = ModContent.ProjectileType
                    <global::AethonMod.Content.Projectiles.Cosmic.LivingPulsarMinion>();
                int tipoMedusa = ModContent.ProjectileType
                    <global::AethonMod.Content.Projectiles.Cosmic.NebulaJellyfishMinion>();
                int tipoCometa = ModContent.ProjectileType
                    <global::AethonMod.Content.Projectiles.Cosmic.StellarCometMinion>();
                int tipoSierpe = ModContent.ProjectileType
                    <global::AethonMod.Content.Projectiles.Cosmic.SierpeEstelarProjectile>();
                float slotsEnUso = 0f;
                bool hayEscuadron = false;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p == null || !p.active || !p.minion) continue;
                    if (p.owner != Player.whoAmI) continue;
                    slotsEnUso += p.minionSlots;      // lo que vanilla va a contar
                    if (p.type == tipoOjo || p.type == tipoDaga || p.type == tipoSupernova || p.type == tipoPulsar ||
                        p.type == tipoMedusa || p.type == tipoCometa || p.type == tipoSierpe)
                        hayEscuadron = true;
                }
                // el techo vivo: libro a mano (nivel) O escuadrón viviendo
                if (nivel > 0 || hayEscuadron)
                {
                    int techoVivo = (int)Math.Ceiling(slotsEnUso);
                    if (techoVivo > Player.maxMinions)
                        Player.maxMinions = techoVivo;
                }
            }

            if (nivel <= 0 || factor <= 0f) return;

            // === CAPACIDAD (el techo del escuadrón, vestido por el factor) ===

            // v6.50.114 — EL TECHO DE LOS OJOS: el ESCUADRÓN del libro
            // (20 + equipo de invocador + la escalera por nivel) es un
            // PISO para maxMinions — se aplica SOLO si está MÁS ALTO
            // (el techo vivo de la promesa .111 puede estar más alto: lo
            // vivo no se culla, pero la compuerta de invocación ya no
            // lee maxMinions — lee el techo cacheado de arriba: JAMÁS
            // otro runaway). Guardado el libro, la capacidad baja; los
            // ojos YA invocados permanecen por la promesa.
            if (Player.maxMinions < techoOjos)
                Player.maxMinions = techoOjos;

            // === RECURSOS: TODO vestido por el factor (la vida y el maná
            //     del libro — 100 % en la mano, 25 % en la bolsa) ===
            int bonusVida = (int)(WeaponScaling.BonusLife(nivel) * factor);
            int bonusMana = (int)(WeaponScaling.BonusMana(nivel) * factor);
            Player.statLifeMax2 += bonusVida;
            Player.statManaMax2 += bonusMana;

            // === PODER DE COMBATE: también vestido por el factor ===
            try
            {
                // v6.51.0 — EL STACK DE INVOCACIÓN MURIÓ (la letra: «los
                // minions del grimorio son INDEPENDIENTES de los minions
                // del juego… sin importar la armadura o accesorios que
                // lleve el jugador, los minions del grimorio estarán
                // sujetos a las características del grimorios»): los ojos
                // leen SOLO las stats del grimorio (DanoOjosDelEterno al
                // invocarlos) — el crítico y la penetración mágicos son
                // del propio libro (su clic izquierdo).
                // +0.2% crítico mágico por nivel (Máximo 100%)
                Player.GetCritChance(DamageClass.Magic) += WeaponScaling.CritBonus(nivel) * factor;
                // Armor penetration +2% cada 5 niveles (Máximo 50%)
                Player.GetArmorPenetration(DamageClass.Magic) += WeaponScaling.ArmorPenBonus(nivel) * factor;
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("ShardPlayer", exRegistro); }
        }

        /// <summary>
        /// PostUpdateBuffs: regeneración de mana y vida.
        /// v6.44 (auditoría R44): EL HOOK CORRECTO. En el binario real,
        /// Player.Update consume lifeRegen/manaRegen en UpdateLifeRegen y
        /// UpdateManaRegen, que corren ANTES de PostUpdate — las escritas
        /// del hook viejo eran letra muerta (ResetEffects las borra al
        /// tick siguiente sin que nadie las lea). PostUpdateBuffs corre
        /// entre ResetEffects y el consumo: aquí sí cuentan.
        /// v6.51.12 — la regeneración VISTE EL FACTOR DE PRESENCIA (100 %
        /// en la mano · 25 % en la bolsa · 0 guardado — la letra nueva:
        /// TODAS las stats del libro siguen la presencia).
        /// </summary>
        public override void PostUpdateBuffs()
        {
            int level = NivelLibro(false);
            if (level <= 0) return;
            float factor = global::AethonMod.Content.Globals.AlmaEternoGlobalItem
                .FactorDelLibro(Player);
            if (factor <= 0f) return;

            // Regeneración de mana (por segundo)
            // v6.44 (auditoría R44): Player.manaRegen lo REESCRIBE
            // UpdateManaRegen desde cero cada tick — el campo que
            // ACUMULA aportes externos es manaRegenBonus. Unidades del
            // motor: 120 cuentas = 1 maná → para R maná/seg hay que
            // aportar 2·R cuentas por tick.
            int manaRegen = (int)(WeaponScaling.ManaRegen(level) * factor);
            if (manaRegen > 0 && Player.statMana < Player.statManaMax2)
            {
                Player.manaRegenBonus += manaRegen * 2;
            }

            // Regeneración de vida (por segundo)
            float lifeRegen = WeaponScaling.LifeRegen(level) * factor;
            if (lifeRegen > 0 && Player.statLife < Player.statLifeMax2)
            {
                Player.lifeRegen += (int)(lifeRegen * 2); // lifeRegen es en 1/2 vida/seg
            }
        }

        /// <summary>
        /// Modifica el daño recibido (reducción por nivel del Grimorio).
        /// v6.51.12 — la reducción VISTE EL FACTOR DE PRESENCIA (100 % en
        /// la mano · 25 % en la bolsa · 0 guardado — TODAS las stats).
        /// </summary>
        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            int level = NivelLibro(false);
            if (level <= 0) return;
            float factor = global::AethonMod.Content.Globals.AlmaEternoGlobalItem
                .FactorDelLibro(Player);
            if (factor <= 0f) return;

            float reduction = WeaponScaling.DamageReduction(level) * factor;
            if (reduction > 0f)
            {
                modifiers.FinalDamage *= 1f - reduction;
            }
        }

        public override void OnHurt(Player.HurtInfo info) { }

        /// <summary>
        /// v6.117 — LA PRESA TRAGADA NO SE DIBUJA: en el instante del
        /// TRAGO (DevoracionEterna.Tragado) el portador ya está DENTRO
        /// del libro — hideEntirePlayer (el interruptor del
        /// PlayerDrawSet que vuelve Transparente TODA la paleta del
        /// jugador) lo hace desaparecer dentro del estallido de
        /// páginas. La v6.115 lo dejaba flotando ENCIMA del libro
        /// cerrado: el trago no se leía. Se libera al llegar al interior
        /// (y en la red de seguridad del Kill del proyectil).
        /// </summary>
        public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
        {
            if (global::AethonMod.Content.Projectiles.Definitivas.DevoracionEterna.Tragado &&
                Player.whoAmI == Main.myPlayer)
            {
                drawInfo.hideEntirePlayer = true;
            }
        }

        /// <summary>
        /// v6.47: EL CORAZÓN DE LA VOZ DEL HAMBRE. Corre cada tick: cuenta
        /// el tiempo sin matar (solo con libro visible y nivel alto),
        /// susurra cada momento de hambre y cruza el umbral de la furia.
        ///
        /// v6.49 — EL RÉGIMEN AUTORITATIVO: la AUTORIDAD del hambre es el
        /// servidor (en SP, el propio proceso). El cliente MP NO cuenta
        /// sus propios momentos — recibe el estado por EcoRed
        /// (SincronizarHambre) para la barra palidecida, la ceniza del aura
        /// y los celos del libro. Así "la voz y el hambre son del
        /// portador": las siente ÉL, en SU pantalla, contadas UNA vez.
        /// </summary>
        // v6.146 — LOS PASOS DE LOS SELECTORES (la letra del Eco y
        // el Verdugo): click derecho alterna, click izquierdo aplica.
        public bool EcoPasoLargo;       // el Eco: 1 ↔ 10
        public bool VerdugoPasoCien;    // el Verdugo: 10 ↔ 100

        // v6.146 — EL BUFF CONTADOR DESDE EL PRIMER MOMENTO (la
        // letra: «el buff contador no aparece deberia a aparecer desde
        // el primer momento en el que el grimorio desbloquea ese
        // ataque en el despertar»): en cuanto el despertar llega al
        // 100 (la cima pagada), el reloj de la Ira NACE con su buff —
        // disponible o recargando, el portador SIEMPRE lo ve.
        private void MantenerBuffDeLaIra()
        {
            try
            {
                if (Player != Main.LocalPlayer || Main.gameMenu) return;
                bool desbloqueada = Systems.DespertarGrimorio.IraDisponible(Player);
                int buffRecarga = ModContent.BuffType
                    <global::AethonMod.Content.Buffs.IraSerExteriorRecargaBuff>();
                int idx = Player.FindBuffIndex(buffRecarga);
                if (desbloqueada)
                {
                    // el buff VIVE siempre (la descripción es el reloj
                    // vivo: recargando o lista — el jugador la ve venir)
                    if (idx < 0)
                        Player.AddBuff(buffRecarga, 2);
                }
                else if (idx >= 0)
                    Player.DelBuff(idx);
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("ShardPlayer", exRegistro); }
        }

        // ================================================================
        //  v6.152 — EL GESTO NUEVO DE LA IRA (la letra: «en todo caso
        //  debemos cambiar los botones para la ira, ahora que sean
        //  dejando precionado el click derecho durante 2 segundos, y
        //  solo eso, creo que seria lo mejor»): MANTÉN EL CLICK DERECHO
        //  2 SEGUNDOS — sin nada más. La autopsia del gesto viejo: el
        //  izq+der hacía que CADA click derecho del gesto disparara la
        //  rama de invocación del Shoot (el libro se re-usaba en plena
        //  ceremonia) — el origen del «los minions desaparecen».
        //  AHORA: el hold de 120 t carga la Ira; MIENTRAS carga, el
        //  grimorio NO SE USA (CanUseItem callado — cero invocación,
        //  cero re-conteo, el escuadrón INTACTO); al cumplir los 2 s
        //  LA CEREMONIA nace. Soltar antes = nada (y el hold se anula
        //  si abre el inventario o una UI).
        // ================================================================
        private int _iraHoldDerecho;          // ticks de DER sostenido
        private bool _iraCargandoHold;        // el hold llegó a la ira (sigue callando un rato)
        private const int IRA_HOLD_TICKS = 120;   // 2 segundos exactos

        // v6.151 — EL SILENCIO DE USO DEL LIBRO mientras nace la Ira
        // (lo mantiene TODO el hold + la eclosión: el grimorio no
        // dispara su Shoot — los minions no se tocan).
        private int _iraSilenciarItem;

        /// <summary>v6.151 — True mientras el gesto de la Ira acalla el
        /// uso del grimorio (lo lee GrimoireEternal.CanUseItem).</summary>
        public bool IraSilenciaElLibro => _iraSilenciarItem > 0 || _iraHoldDerecho > 0 || _iraCargandoHold;

        /// <summary>v6.152 — EL PROGRESO DEL HOLD (0..1): lo dibuja el
        /// buff de la Ira (el anillo del icono se CIERRRE mientras
        /// cargas el gesto — sabes cuándo falta). 0 si no hay hold.</summary>
        public float IraHoldProgreso =>
            _iraHoldDerecho <= 0 ? 0f : Math.Min(1f, _iraHoldDerecho / (float)IRA_HOLD_TICKS);

        public override void PostUpdate()
        {
            // EL GESTO DE LA IRA — mantener el CLICK DERECHO 2 segundos
            try
            {
                if (Player == Main.LocalPlayer && !Main.gameMenu)
                {
                    bool der = Main.mouseRight && !Main.playerInventory &&
                        !Player.mouseInterface && !Main.ingameOptionsWindow;
                    if (der)
                    {
                        // EL HOLD VIVE: cuenta ticks y al llegar a 2 s
                        // dispara LA IRA (una sola vez por hold — la
                        // marca _iraCargandoHold impide el ametrallado
                        // mientras sigas sosteniendo).
                        _iraHoldDerecho++;
                        if (_iraHoldDerecho == IRA_HOLD_TICKS && !_iraCargandoHold)
                        {
                            _iraCargandoHold = true;
                            _iraSilenciarItem = 15;   // la eclosión calla el libro un instante más
                            Systems.AtaquesCombinadosSistema.ShiftClickDeLaIra(Player);
                        }
                    }
                    else
                    {
                        // SOLTÓ (o abrió UI): el hold se reinicia limpio
                        _iraHoldDerecho = 0;
                        _iraCargandoHold = false;
                    }
                    if (_iraSilenciarItem > 0) _iraSilenciarItem--;
                }
                else { _iraHoldDerecho = 0; _iraCargandoHold = false; }
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("ShardPlayer", exRegistro); }

            // EL BUFF DEL RELOJ — desde el nivel 100, siempre visible
            MantenerBuffDeLaIra();

            // ============================================================
            // v6.51.6 — LA MIGRACIÓN DEL ALMA (una sola vez): el guardado
            // de la .51.0–.51.5 trae el despertar y el festín en el
            // JUGADOR; la letra nueva manda: TODO eso es del GRIMORIO.
            // El primer Eterno del inventario lo HEREDA todo y el jugador
            // queda limpio — cada libro nuevo empieza su propia escalera.
            // ============================================================
            try
            {
                if (AlmaLegacyPendiente && !Main.gameMenu && Player.active)
                {
                    int eternoM = ModContent.ItemType
                        <global::AethonMod.Content.Weapons.GrimoireEternal>();
                    Item heredero = null;
                    for (int k = 0; k < 58 && heredero == null; k++)
                    {
                        Item it = Player.inventory[k];
                        if (it != null && !it.IsAir && it.type == eternoM) heredero = it;
                    }
                    if (heredero != null)
                    {
                        var alma = heredero.GetGlobalItem
                            <global::AethonMod.Content.Globals.AlmaEternoGlobalItem>();
                        if (alma != null)
                        {
                            // SOLO si el heredero está VIRGEN (nunca PISAR
                            // el progreso de un libro que ya empezó).
                            if (alma.NivelDespertar == 0 && alma.DevoracionPoder == 0 &&
                                alma.DevoracionExp == 0)
                            {
                                alma.NivelDespertar = NivelDespertar;
                                alma.PetDelDespertar = PetDelDespertar;
                                alma.DevoracionPoder = DevoracionPoder;
                                alma.DevoracionExp = DevoracionExp;
                                alma.DevoracionFuerza = DevoracionFuerza;
                                alma.DevoracionPrisa = DevoracionPrisa;
                                alma.DevoracionMirada = DevoracionMirada;
                                alma.DevoracionCorona = DevoracionCorona;
                                alma.DevoracionMordida = DevoracionMordida;
                                alma.AlmaIgnea = AlmaIgnea;
                                alma.AlmaSalvaje = AlmaSalvaje;
                                alma.AlmaVampira = AlmaVampira;
                                alma.CorazonEstelar = CorazonEstelar;
                                alma.AlmaVeloz = AlmaVeloz;
                                alma.PunoTitan = PunoTitan;
                                heredero.NetStateChanged();

                                if (Player.whoAmI == Main.myPlayer && !Main.dedServ)
                                    Main.NewText(Terraria.Localization.Language.GetTextValue(
                                        "Mods.AethonMod.Despertar.Migracion"),
                                        new Color(196, 138, 255));
                            }
                        }
                        // EL JUGADOR QUEDA LIMPIO (la era del despertar por
                        // jugador murió — el libro lo lleva todo ahora).
                        NivelDespertar = 0;
                        PetDelDespertar = false;
                        DevoracionPoder = 0;
                        DevoracionExp = 0;
                        DevoracionFuerza = 0;
                        DevoracionPrisa = 0;
                        DevoracionMirada = 0;
                        DevoracionCorona = 0;
                        DevoracionMordida = 0;
                        AlmaIgnea = 0;
                        AlmaSalvaje = 0;
                        AlmaVampira = 0;
                        CorazonEstelar = 0;
                        AlmaVeloz = 0;
                        PunoTitan = 0;
                        AlmaLegacyPendiente = false;
                    }
                    // sin libro aún: la marca espera al primer Eterno.
                }
            }
            catch { AlmaLegacyPendiente = false; }

            // v6.51.12 — EL RELOJ LARGO DEL CORO: un tick de juego más
            // sin hadas (la garantía de las 2 h lo lee
            // AethonMenorPareja — y ArrancarPlaga lo devuelve a cero).
            try { if (TicksSinCoro < int.MaxValue - 1) TicksSinCoro++; } catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("ShardPlayer", exRegistro); }

            // v6.51.0 — LOS ATAQUES COMBINADOS DEL DESPERTAR (el sol
            // rúnico y los agujeros negros de los ojos): el coordinador
            // corre en la PANTALLA del dueño (cada jugador con SUS ojos).
            try
            {
                Content.Systems.AtaquesCombinadosSistema.Tick(Player);
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("ShardPlayer", exRegistro); }

            try
            {
                // v6.50.116 — EL RELOJ DE LA GRACIA: la tregua del guardián
                //     corre hacia atrás en la AUTORIDAD (el pool de spawns
                //     vive allí; en SP es la misma máquina).
                if (GuardianGracia > 0 &&
                    Main.netMode != Terraria.ID.NetmodeID.MultiplayerClient)
                    GuardianGracia--;

                // v6.50.1 — FIX (EL HOST SORDO): en un listen server el host
                // es netMode Server PERO tiene pantalla y es Main.myPlayer —
                // el guard viejo lo excluía del Libro Celoso y del aura de
                // ceniza. Main.dedServ separa al host del dedicado.
                bool local = Player.whoAmI == Main.myPlayer &&
                    (Main.netMode != Terraria.ID.NetmodeID.Server || !Main.dedServ);
                bool autoridad = Main.netMode != Terraria.ID.NetmodeID.MultiplayerClient; // SP o server
                int nivel = NivelLibro(false);

                // v6.50 — LA RED DE SEGURIDAD: cada 10 s el server re-envía
                // libros y crónica al portador (cubre al que entra a mitad
                // de sesión, los movimientos de slot y cualquier deriva
                // que un paquete perdido hubiera dejado — la foto de
                // entrada se pide en OnEnterWorld; esto la repone).
                // v6.50.1: también el HAMBRE (si un MsgHambre se pierde, la
                // barra/aura quedaban desfasadas hasta el próximo cambio).
                // v6.50.2: MsgHambre SIEMPRE (lleva además el FESTÍN —
                // fase/oleada/total para el diagnóstico de furia del
                // cliente; el estado de un portador sin hambre es 0/0 y
                // no molesta).
                if (Main.netMode == Terraria.ID.NetmodeID.Server && Player.active &&
                    ((Main.GameUpdateCount + (ulong)Player.whoAmI * 37ul) % 600u) == 0ul)
                {
                    EcoRed.SincronizarLibros(Player);
                    EcoRed.SincronizarCronica(Player);
                    EcoRed.SincronizarHambre(Player);
                }

                if (autoridad)
                {
                    // v6.50.10 — FIX: UN MUERTO NO ALIMENTA EL LIBRO.
                    // La hambre crecía con el portador caído (5 min de
                    // respawn bastaban para los 10 momentos) y la furia
                    // PROVOCABA con él muerto — el festín nacía y moría al
                    // primer tick (VenganzaPorMuerte) con el churn de
                    // voces de ira/venganza encima del respawn. Ahora el
                    // hambre se CONGELA mientras yace (como guardado el
                    // libro: DUERME) y reanuda al levantarse.
                    if (!Player.dead && nivel >= NivelMinimoHambre)
                    {
                        TicksSinMatar++;
                        int momentos = Math.Min(TicksSinMatar / (60 * SegundosPorMomento), MomentosMax);

                        if (momentos > MomentosHambre)
                        {
                            MomentosHambre = momentos;

                            // v6.142 — EL ESPEJO MURIÓ (la letra: el
                            // indicador real es EL DEL ARMA — la
                            // máquina del Grimorio Hambriento Nervioso
                            // a velocidad NORMAL): el Eterno corre SU
                            // propio reloj (TasaHambre 1/216000, la
                            // hora) con su ojo rojo, su inquietud, SU
                            // caza y SU festín. El medidor de momentos
                            // solo decide LA OLEADA (90%/100% a la
                            // hora — calzadas al mismo reloj).

                            // v6.49 — el estado nuevo viaja al portador
                            // (su barra, su aura, sus celos — en SU cliente).
                            EcoRed.SincronizarHambre(Player);

                            // EL SUSURRO: la voz del libro en modo hambre — sin
                            // rugido, escala menuda, gris pálido. v6.49: la
                            // clave la reparte la AUTORIDAD y EcoRed la lleva
                            // SOLO al portador ("¿eso también mata?"… pero
                            // de hambre).
                            // v6.48 — EL SAJOR DEL BIOMA: la primer línea de cada
                            // momento cambia con el bioma ("carne de jungla",
                            // "sal del infierno"…): el libro sabe DÓNDE está
                            // hambriento. El resto del escalado sigue siendo el
                            // de la casa (1→4).
                            if (MomentosHambre == 1)
                            {
                                string claveSabor = EcoSistema.ClaveSusurroDelBioma(Player);
                                if (!string.IsNullOrEmpty(claveSabor))
                                    EcoRed.SusurrarAlPortador(Player, claveSabor,
                                        new Microsoft.Xna.Framework.Color(176, 172, 168));
                            }
                            else
                            {
                                EcoRed.SusurrarAlPortador(Player,
                                    "Mods.AethonMod.Eco.Hambre.Susurro" +
                                    Math.Min(MomentosHambre, 4),
                                    new Microsoft.Xna.Framework.Color(176, 172, 168));
                            }

                            // LA FURIA: al cuarto momento, el evento (si la config
                            // lo permite y el mundo está libre — un jefe o una
                            // invasión lo BLOQUEA y la hambre sigue subiendo hasta
                            // 10: por eso existen las 10 oleadas naturales).
                            if (MomentosHambre >= MomentosParaFuria)
                            {
                                // v6.50.2 — FIX (config ClientSide leída por la
                                // AUTORIDAD): EventoHambreGrimorio es una
                                // decisión del SERVER (el evento lo corre él) —
                                // vive en AethonConfigServidor (ServerSide):
                                // en dedicado manda la config del server, no
                                // la ilusión del cliente.
                                // v6.50.59 — LA FURIA NATURAL ES DE NIVEL: ya
                                // NO son los momentos de hambre acumulados —
                                // es EL NIVEL DEL PORTADOR (la primera vez 1:
                                // una oleada; vencer la sube a 2: DOS oleadas;
                                // y así hasta 10). Los momentos siguen siendo
                                // EL RELOJ que decide CUÁNDO estalla.
                                var config = ModContent.GetInstance<Content.AethonConfigServidor>();
                                bool evento = config == null || config.EventoHambreGrimorio;
                                // v6.151 — EL DADO DE LA LETRA: la probabilidad
                                // la pone EL % DE HAMBRE DEL LIBRO (1 % ≤ 89 ·
                                // 5 % al 90 · +5 % por punto · 100 % DIRECTA).
                                // Este sitio (el instante del momento nuevo) y
                                // el reloj de abajo tiran EL MISMO dado.
                                int pctHambre = PorcientoHambreDelLibro();
                                if (pctHambre >= 100)
                                {
                                    if (evento && !GrimorioFuriaSistema.Activo &&
                                        GrimorioFuriaSistema.MundoLibre())
                                        GrimorioFuriaSistema.Provocar(Player,
                                            Math.Max(1, Math.Min(10, FuriaNivel)), natural: true);
                                }
                                else if (evento && !GrimorioFuriaSistema.Activo &&
                                    GrimorioFuriaSistema.MundoLibre() &&
                                    Main.rand.Next(100) < ProbabilidadOleada(pctHambre))
                                    GrimorioFuriaSistema.Provocar(Player,
                                        Math.Max(1, Math.Min(10, FuriaNivel)), natural: true);
                            }
                        }
                    }
                    // guardado el libro: DUERME — la hambre se congela.

                    // ============================================================
                    // v6.50.60 — EL RELOJ DE LA FURIA (la letra: «analisa
                    // bien el codigo de la oleada para que se pueda activar
                    // SIEMPRE que el libro tenga hambre»): el intento viejo
                    // vivía SOLO en el instante de cada momento nuevo (una
                    // ventana de 1 tick cada 75 s — y NINGUNA tras el
                    // momento 10): si en ese instante había un jefe vivo o
                    // una invasión, la furia esperaba otro momento
                    // completo… o moría de hambre para siempre. AHORA: libro
                    // FURIOSO + mundo libre = festín EN CADA CHEQUEO (cada
                    // 5 s, día o noche, siempre) — la oleada sale SIEMPRE
                    // que el libro tenga hambre y el mundo pueda recibirla.
                    // ============================================================
                    if (MomentosHambre >= MomentosParaFuria && !Player.dead &&
                        nivel >= NivelMinimoHambre &&
                        ((Main.GameUpdateCount + (ulong)Player.whoAmI * 61ul) % 300u) == 0u)
                    {
                        var configR = ModContent.GetInstance<Content.AethonConfigServidor>();
                        bool eventoR = configR == null || configR.EventoHambreGrimorio;
                        // v6.151 — EL MISMO DADO DE LA TABLA (la letra: el %
                        // de hambre ES la probabilidad): el reloj de cada 5 s
                        // consulta la tabla con el % CONTINUO del libro —
                        // 100 % SIEMPRE dispara, 90 % falla 19 de 20 veces,
                        // 1 % el ruido de fondo de la espera.
                        int pctHambreR = PorcientoHambreDelLibro();
                        if (eventoR && !GrimorioFuriaSistema.Activo &&
                            GrimorioFuriaSistema.MundoLibre() &&
                            (pctHambreR >= 100 ||
                             Main.rand.Next(100) < ProbabilidadOleada(pctHambreR)))
                            GrimorioFuriaSistema.Provocar(Player,
                                Math.Max(1, Math.Min(10, FuriaNivel)), natural: true);
                    }
                }

                // EL LIBRO CELOSO y EL AURA corren en el CLIENTE del
                // portador (percepción local: lo que ÉL sostiene y viste).
                if (local) ElLibroCeloso(nivel);

                // EL AURA DEL HAMBRE: la ceniza del libro sobre su portador
                // — v6.48 por el PORTADOR (AuraPortadorHalo, el camino
                // aditivo del halo-proyectil: el neón de verdad detrás del
                // cuerpo). Las partículas corren en la AI del portador.
                // v6.141 — EL AURA DEL PORTADOR MURIÓ (la letra: «el
                // aura rara por algun motivo sigue apareciendo y ademas
                // no es necesaria, por lo tanto la puedes quitar del
                // jugador, recuerda que el indicador de que el grimorio
                // del eterno tiene hambre es cuando se vuelve inquieto
                // y su ojo se vuelve rojo y ese es su unica forma que
                // debe tener para saber si tiene hambre»): NADA de halo
                // de hambre sobre el jugador — el ojo rojo e inquieto
                // del libro ES el único indicador. (El modo 0 del halo
                // queda muerto: ya nadie lo engendra; SigueViva()
                // devuelve false por su propia guarda de MomentosHambre
                // — el que quede vivo de partidas viejas se despide
                // solo en el primer tick.)
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("ShardPlayer", exRegistro); }
        }

        /// <summary>
        /// v6.48 — EL LIBRO CELOSO: si el portador cambia a OTRA ARMA
        /// mientras el libro está hambriento, el libro susurra su celos —
        /// una muestra DISTINTA según la CLASE del arma sostenida (melé,
        /// magia, invocación, arrojadiza, y la distancia repartida entre
        /// ARCO y BALA, como pidió el usuario). PURA VOZ: cero mecánica,
        /// cero números — el libro solo opina. Frío de 8 s para no cantar
        /// a cada swap de hotbar.
        /// </summary>
        private void ElLibroCeloso(int nivelLibro)
        {
            if (_ticksCelos > 0) _ticksCelos--;

            Item sostenido = Player.HeldItem;
            int tipo = sostenido != null ? sostenido.type : -1;
            if (tipo == _ultimaArmaSostenida) return; // mismo ítem: nada nuevo
            _ultimaArmaSostenida = tipo;

            // solo con hambre de verdad (la primer boca cuenta) y solo
            // con ARMAS (daño > 0, no el propio libro, no herramientas).
            if (MomentosHambre <= 0 || nivelLibro <= 0) return;
            if (sostenido == null || sostenido.IsAir || sostenido.damage <= 0) return;
            if (tipo == ModContent.ItemType<Weapons.GrimoireEternal>()) return;
            if (_ticksCelos > 0) return;

            // LA CLASE DEL ARMA CELADA (con arco y bala separados).
            string pool;
            var dt = sostenido.DamageType;
            if (dt == DamageClass.Melee || dt == DamageClass.MeleeNoSpeed) pool = "Melee";
            else if (dt == DamageClass.Magic || dt == DamageClass.MagicSummonHybrid) pool = "Magia";
            else if (dt == DamageClass.Summon || dt == DamageClass.SummonMeleeSpeed) pool = "Invocacion";
            else if (dt == DamageClass.Throwing) pool = "Arrojadiza";
            else if (dt == DamageClass.Ranged)
            {
                if (sostenido.useAmmo == Terraria.ID.AmmoID.Arrow) pool = "Arco";
                else if (sostenido.useAmmo == Terraria.ID.AmmoID.Bullet) pool = "Bala";
                else pool = "Arco"; // cerbatanas/dardos: el arco cubre
            }
            else return; // sin clase clara: el libro calla

            _ticksCelos = 480; // 8 s de frío
            string texto = EcoLib.ElegirVariante("Mods.AethonMod.Eco.Celos." + pool, 3);
            if (!string.IsNullOrEmpty(texto))
                EcoLib.Hablar(texto,
                    new Microsoft.Xna.Framework.Color(196, 116, 108),
                    rugido: false, escala: 0.52f);
        }

        /// <summary>¿Ya vive mi portador de aura con este modo?</summary>
        private bool EspiarPortador(int modo)
        {
            int tipo = ModContent.ProjectileType<Projectiles.Cosmetic.AuraPortadorHalo>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == Player.whoAmI && p.type == tipo &&
                    (int)p.ai[0] == modo)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// v6.48 — LA CRÓNICA: apunta que EL LIBRO devoró a este jefe con
        /// este portador (lo llama GlobalNPCXP al cobrar la kill — antes
        /// de la voz). El registro vive en el .plr (v6.50.61: sin
        /// narrador, la crónica queda como constancia del festín).
        /// v6.50.3 — devuelve TRUE solo si la página es NUEVA (el llamador
        /// sincroniza por EcoRed únicamente entonces: antes el Devorador
        /// disparaba ~80 MsgCronica idénticos por una sola derrota).
        /// </summary>
        /// <summary>v6.137 — ¿CUÁNTOS KILLS INTERIORES lleva el
        /// portador contra ESTE jefe? (el nivel de la arena).</summary>
        public int KillsInteriorDe(int npcType)
        {
            int idx = MuertesInteriorTipos.IndexOf(npcType);
            return idx >= 0 ? MuertesInteriorCuenta[idx] : 0;
        }

        // ================================================================
        // ================================================================
        //  v6.180 — EL PEDACITO DEL DEVORAMUNDOS (la letra: «que ocurra
        //  la primera vez que aparece el Devoramundos y que al igual que
        //  los demas jefes su festin se reinicie con la pagina en
        //  blanco»): la marca vive con el portador — la PRIMERA
        //  aparición sirve el pedacito (UNA sección, la cola) y la
        //  Página en Blanco la devuelve a la mesa. El contador de la
        //  2ª invocación y su llamada pendiente MURIERON con la
        //  aspiradora (la animación que nunca ocurría).
        //  v6.182 — LA MARCA DE LA LLAMADA (la autopsia del festín que
        //  no iniciaba): la v6.180 armaba el pedacito con CUALQUIER
        //  cabeza de Devorador nacida cerca — y LOS GUARDIANES DE LAS
        //  OLEADAS también son Devoradores (LosSeis): el primer
        //  guardián de oleada se comía la marca sin que el portador
        //  viera NADA. AHORA el pedacito solo se sirve con la cabeza
        //  nacida de UNA INVOCACIÓN REAL del portador: el pilar y la
        //  comida de gusano (dentro y fuera del interior) la MARCAN
        //  aquí, y OnSpawn la consume al nacer la cabeza.
        // ================================================================
        public bool DevoramundosPedacito;

        /// <summary>v6.182 — LA LLAMADA PENDIENTE: el portador acaba de
        /// INVOCAR al Devoramundos (pilar · comida de gusano). Vive
        /// segundos (caduca sola) — la consume la cabeza que nace.</summary>
        private bool _llamadaDevoramundos;
        private ulong _llamadaDevoramundosTick;

        /// <summary>v6.182 — EL PORTADOR LLAMA: marca la invocación real
        /// (10 s de vida — si vanilla rechaza el uso, la marca caduca
        /// sola sin quemar nada).</summary>
        public void MarcarLlamadaDevoramundos()
        {
            _llamadaDevoramundos = true;
            _llamadaDevoramundosTick = Main.GameUpdateCount;
        }

        /// <summary>v6.182 — ¿NACE UNA CABEZA DE LA LLAMADA? La consume
        /// (una invocación, una cabeza).</summary>
        public bool ConsumirLlamadaDevoramundos()
        {
            if (!_llamadaDevoramundos) return false;
            if (Main.GameUpdateCount - _llamadaDevoramundosTick > 600ul)
            {
                _llamadaDevoramundos = false;   // caducó: la llamada se enfrió
                return false;
            }
            _llamadaDevoramundos = false;       // consumida por ESTA cabeza
            return true;
        }

        /// <summary>v6.180 — LA PRIMERA VEZ CON EL CRONISTA (la letra:
        /// «hay que dejar claro que el lore lo escribió el mismo
        /// cronista y eso se hace la primera vez que el jugador habla
        /// con el cronista»): la primera conversación abre con LA
        /// AUTORÍA — una sola vez por portador.</summary>
        public bool PrimeraVezCronista;

        /// <summary>v6.137 — UN KILL MÁS en la arena (devuelve la cuenta
        /// nueva; -1 si algo falla).</summary>
        public int MarcarKillInterior(int npcType)
        {
            try
            {
                int idx = MuertesInteriorTipos.IndexOf(npcType);
                if (idx < 0)
                {
                    MuertesInteriorTipos.Add(npcType);
                    MuertesInteriorCuenta.Add(1);
                    return 1;
                }
                MuertesInteriorCuenta[idx]++;
                return MuertesInteriorCuenta[idx];
            }
            catch { return -1; }
        }

        public bool CronicaMarcar(int npcType)
        {
            if (npcType <= 0) return false;
            if (CronicaJefes.Contains(npcType)) return false;
            CronicaJefes.Add(npcType);
            return true;
        }

        /// <summary>
        /// v6.50.60 — ¿Es CHUSMA NOCTURNA NATURAL? (la de vanilla, la de
        /// siempre: zombies, ojos demoníacos, licántropos, los de luna de
        /// sangre). LA LETRA: «por la noche la oleada parece no activarse
        /// […] que se pueda activar siempre que el libro tenga hambre» —
        /// LA CAUSA RAÍZ: de noche los monstruos NATURALES te obligan a
        /// defenderte y CADA kill borraba el hambre de raíz (el reloj
        /// volvía a cero una y otra vez: el 4º momento JAMÁS llegaba).
        /// La defensa de medianoche NO es una cena: el libro no cuenta
        /// esa chusma de siempre como comida (la oleada sí cuenta — es
        /// SU comida, la que ÉL trajo a la mesa).
        /// </summary>
        private static bool EsChusmaNocturnaNatural(int tipo)
        {
            if (tipo <= 0) return false;
            try
            {
                return NPCID.Sets.Zombies[tipo] ||        // TODA la familia zombie (la noche de siempre)
                    tipo == NPCID.DemonEye ||               // el ojo que abre la noche
                    tipo == NPCID.WanderingEye ||           // su primo del hardmode
                    tipo == NPCID.Werewolf ||               // la luna llena
                    tipo == NPCID.Drippler ||               // la luna de sangre
                    tipo == NPCID.BloodZombie;              // la luna de sangre
            }
            catch { return false; }
        }

        /// <summary>
        /// El portador mató algo (lo llama GlobalNPCXP cuando el libro de
        /// su barra rápida ha comido): la hambre se perdona — la barra
        /// dorada recupera su color y los susurros callan. v6.49: el
        /// estado nuevo viaja al portador (EcoRed) — en MP la autoridad
        /// la perdona y el cliente del portador la VE perdonada.
        /// v6.50.2 — FIX (bandwidth): SOLO cuando el hambre CAMBIA.
        /// v6.50.60 — LAS CENAS DE VERDAD (la letra: «que la oleada se
        /// pueda activar SIEMPRE que el libro tenga hambre» — la cura del
        /// «por la noche no se activa»): (a) la chusma nocturna NATURAL
        /// no alimenta NADA (defenderse de los zombies de siempre no es
        /// cenar — el reloj del hambre sigue corriendo);
        /// (b) el libro FURIOSO (MomentosHambre >= MomentosParaFuria)
        /// ya no se calma con bocados: la kill solo le resta 45 s al
        /// reloj y NUNCA por debajo del umbral — la furia está
        /// DECLARADA, la oleada VA a salir; (c) la comida de la PROPIA
        /// oleada no toca el reloj (es SU festín, no una cena); (d) lo
        /// demás (la chusma del día, los monstruos de mazmorra/cuevas)
        /// alimenta como siempre: hambre a cero — el sabor de siempre.
        /// </summary>
        public void RegistrarKill(int npcType = -1, bool deOleada = false, bool esJefe = false)
        {
            // (c) LA COMIDA DE LA OLEADA: el festín ya está en marcha —
            // matar a LOS SUYOS no cambia el reloj (la saciedad llega
            // con el final del festín, PerdonarHambre).
            if (deOleada) return;

            // (a) LA DEFENSA DE MEDIANOCHE NO ES CENA: el zombie natural
            // que te saltó a la garganta no alimenta el libro — el reloj
            // sigue su marcha y la furia llega de noche IGUAL que de día.
            if (!esJefe && EsChusmaNocturnaNatural(npcType)) return;

            // (b) EL LIBRO FURIOSO NO SE CALMA CON BOCADOS: una vez
            // declarada la furia, la kill solo muerde 45 s del reloj —
            // y jamás baja del umbral (la oleada YA está prometida).
            if (MomentosHambre >= MomentosParaFuria)
            {
                int suelo = MomentosParaFuria * 60 * SegundosPorMomento;
                TicksSinMatar = Math.Max(suelo, TicksSinMatar - 45 * 60);
                return;
            }

            // (d) LA CENA DE VERDAD: hambre a cero (el sabor de siempre).
            TicksSinMatar = 0;
            bool cambio = MomentosHambre != 0;
            MomentosHambre = 0;
            if (cambio)
                EcoRed.SincronizarHambre(Player); // no-op fuera del servidor
        }

        // === EL PERFIL DEL AURA DE HAMBRE (cacheado: cero GC por frame) ===
        // v6.50.3 — FIX (cache ESTÁTICO en un ModPlayer): los campos static
        // se comparten entre TODAS las instancias — en SP (un jugador) la
        // caché acertaba por casualidad, pero en el SERVER de MP el hook
        // corre por cada portador: dos hambres distintas = MISS por frame
        // por jugador (la promesa "cero GC" moría justo donde hay más
        // jugadores). Campos de INSTANCIA: una caché POR JUGADOR, como
        // siempre debió ser.
        private AuraPerfil _auraHambre;
        private int _auraHambreIntensidad = -1;

        /// <summary>El perfil del hambre a la intensidad actual (creado SOLO cuando cambia).</summary>
        private AuraPerfil AuraHambre()
        {
            if (_auraHambre == null || _auraHambreIntensidad != MomentosHambre)
            {
                _auraHambre = AuraPerfil.HambreDelGrimorio(MomentosHambre);
                _auraHambreIntensidad = MomentosHambre;
            }
            return _auraHambre;
        }

        /// <summary>El perfil cacheado del aura de hambre (las capas de jugador lo dibujan).</summary>
        public AuraPerfil AuraHambrePublica() => AuraHambre();

        /// <summary>
        /// El festín terminó y el libro está saciado: la hambre del
        /// portador se perdona por completo (GrimorioFuriaSistema.Fin).
        /// v6.49: el estado viaja por EcoRed al portador.
        /// </summary>
        public void PerdonarHambre()
        {
            TicksSinMatar = 0;
            MomentosHambre = 0;
            EcoRed.SincronizarHambre(Player); // no-op fuera del servidor
        }

        public override void OnEnterWorld()
        {
            // v6.50 — LA FOTO DE ENTRADA: el cliente MP recién llegado pide
            // SUS libros (nivel/XP), SU crónica y SU hambre — vanilla
            // sincroniza el inventario, pero no los datos de GlobalItem ni
            // del ModPlayer. En SP no hay nada que pedir.
            EcoRed.PedirMisLibros();

            // v6.50.50 — EL MODO PRUEBA (la petición literal: «esos item
            // nuevos no pongas recetas, daselos directamente al jugador,
            // recuerda que todo esto es una prueba»): los ítems NUEVOS del
            // ciclo (la Forma Ascendida 2, la Forma Ascendida 3 y el Aethon
            // Menor) ya no tienen receta — se ENTREGAN al entrar al mundo,
            // una sola copia por ítem (si ya la tiene — inventario, hueco
            // funcional, vanidad o misceláneo — no se repite).
            if (Player.whoAmI == Main.myPlayer && !Main.dedServ)
                EntregarRegaloDePruebas();
        }

        // ================================================================
        //  v6.50.50 — EL REGALO DE PRUEBAS (los ítems nuevos, directo)
        // ================================================================

        /// <summary>
        /// Entrega al jugador LOCAL los ítems nuevos del ciclo de pruebas
        /// si no los tiene ya (inventario, accesorios, vanidad o misc).
        /// Los que no caben caen al suelo junto a él (QuickSpawnItem).
        /// </summary>
        private void EntregarRegaloDePruebas()
        {
            try
            {
                int entregados = 0;
                // ==============================================================
                //  v6.137 — LOS 999 MUÑECOS DE PRUEBAS (la letra: «dale
                //  al jugador 999 muñecos de prueba negros»): el banco
                //  de pruebas del grimorio A GRANEL — un stack de 999
                //  (se rellena SOLO si el jugador no lleva ya uno
                //  completo: la entrega es una vez por guardado).
                // ==============================================================
                try
                {
                    int tipoDummy = ModContent.ItemType
                        <global::AethonMod.Content.Items.Llamados.DummyNegroItem>();
                    bool yaLlena = false;
                    for (int k = 0; k < 50; k++)
                    {
                        Item inv = Player.inventory[k];
                        if (inv != null && !inv.IsAir && inv.type == tipoDummy &&
                            inv.stack >= 999) { yaLlena = true; break; }
                    }
                    if (!yaLlena)
                    {
                        // QuickSpawnItem: el patrón de la casa (devuelve el
                        // índice y SINCRONIZA solo — el gift del server)
                        int idx99 = Player.QuickSpawnItem(
                            Player.GetSource_GiftOrReward(), tipoDummy, 999);
                        if (idx99 >= 0) entregados++;
                    }
                }
                catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("ShardPlayer", exRegistro); }
                entregados += EntregarSiNoTiene(ModContent.ItemType
                    <global::AethonMod.Content.Items.Cosmetics.FormaAscendidaDosItem>());
                entregados += EntregarSiNoTiene(ModContent.ItemType
                    <global::AethonMod.Content.Items.Cosmetics.FormaAscendidaTresItem>());
                // v6.50.53 — LA FORMA ASCENDIDA 4 (EL SERAFÍN — la cuarta luz).
                entregados += EntregarSiNoTiene(ModContent.ItemType
                    <global::AethonMod.Content.Items.Cosmetics.FormaAscendidaCuatroItem>());
                // v6.112 — LAS LUCES 5, 6, 7, 9 Y 10 MURIERON por la letra
                // del usuario («las otras formas 5, 6, 7, 9, 10 pueden ser
                // borradas») — su entrega directa también (la 8 VIVE abajo).
                entregados += EntregarSiNoTiene(ModContent.ItemType
                    <global::AethonMod.Content.Items.Llamados.AethonMenorItem>());
                // v6.50.54 — EL HALO ARCOÍRIS (el arcoíris de la Forma 3
                // hecho adorno: pequeño, alrededor de la cabeza).
                entregados += EntregarSiNoTiene(ModContent.ItemType
                    <global::AethonMod.Content.Items.Cosmetics.HaloArcoirisItem>());
                // v6.51.3 — EL BRILLO DE LOS SOLES RÚNICOS (los destellos
                // de 4 puntas de los soles de 3 anillos+, hechos adorno).
                entregados += EntregarSiNoTiene(ModContent.ItemType
                    <global::AethonMod.Content.Items.Cosmetics.BrilloSolarItem>());
                // v6.153 — EL TOMO II MURIÓ con la purga de las sombras extra.
                // v6.153 — EL TOMO IV MURIÓ con la purga de las sombras extra.
                // v6.153 — EL TOMO V MURIÓ con la purga de las sombras extra.
                // v6.99 — EL ESTANDARTE DEL JUICIO MURIÓ en la v6.109
                // (la letra: «ya puedes borrar los otros 2 estandarte»)
                // — su entrega directa también.
                // v6.100 — LOS FAVORITOS DE LA PURGA: el Taller y las
                // Tres Casas murieron con sus armas, pero sus DOS
                // favoritos (el canon de SPRITES-FAVORITOS) y la mascota
                // del taller siguen VIVOS — y llegan SOLOS (la regla
                // permanente: nada nuevo queda sin llegar al jugador).
                entregados += EntregarSiNoTiene(ModContent.ItemType
                    <global::AethonMod.Content.Weapons.TresCasas.CoroDeLosProfundos>());
                entregados += EntregarSiNoTiene(ModContent.ItemType
                    <global::AethonMod.Content.Weapons.TresCasas.PlumaDelEscribaSanto>());
                entregados += EntregarSiNoTiene(ModContent.ItemType
                    <global::AethonMod.Content.Items.Llamados.VastagoDelEternoItem>());
                // v6.100 — LAS ARMAS NUEVAS DE LA SESIÓN (las auras y
                // la definitiva — todas hechas con LAS HERRAMIENTAS de
                // la casa): también en la Bolsa de las Investigaciones,
                // pero la regla es la regla — DIRECTO.
                // v6.170 — EL CLICKER DEL UMBRAL MURIÓ (la letra:
                // «el arma que creaste el clicker umbral esta horrible,
                // lo puedes borrar») — sin clickers otra vez: la casa
                // no insiste con lo que el dueño no quiere.
                // v6.168 — EL ARMA NUEVA (la letra: «crea una nueva
                // arma, su ataque seran lineas en el aire a gran
                // velocidad»): La Regla del Cielo — también DIRECTO
                // (la regla de la casa: nada nuevo sin entrega).
                entregados += EntregarSiNoTiene(ModContent.ItemType
                    <global::AethonMod.Content.Weapons.Regla.LaReglaDelCielo>());
                entregados += EntregarSiNoTiene(ModContent.ItemType
                    <global::AethonMod.Content.Weapons.Auras.CoronaDePlumas>());
                entregados += EntregarSiNoTiene(ModContent.ItemType
                    <global::AethonMod.Content.Weapons.Auras.DerrameDeTinta>());
                entregados += EntregarSiNoTiene(ModContent.ItemType
                    <global::AethonMod.Content.Weapons.Definitivas.LaUltimaPalabra>());
                // v6.104 — EL AURA DE JUICIO MURIÓ en la v6.109 (la
                // letra: «ya puedes borrar los otros 2 estandarte» — el
                // puerto del warbanner y el estandarte original cayeron
                // JUNTOS; sobrevive LA ÚNICA: el Aura del Vacío abajo).
                entregados += EntregarSiNoTiene(ModContent.ItemType
                    <global::AethonMod.Content.Items.Bolsas.BolsaAuras>());
                // v6.105 → v6.109 — EL AURA DEL VACÍO (nacida «Aura de
                // Juicio 2»: la copia silenciosa — el efecto del viejo
                // estandarte, NADA sobre el jugador, el calentamiento
                // aerodinámico en los impíos — la v6.109 la RENOMBRA y
                // la deja como LA ÚNICA del oficio) — también en la
                // Bolsa de las Auras (la regla de la casa: DIRECTO).
                entregados += EntregarSiNoTiene(ModContent.ItemType
                    <global::AethonMod.Content.Items.Accessories.AuraDeJuicio2Item>());
                // v6.110 — LA FORMA ASCENDIDA 8 (la letra: «y ademas crear
                // la forma ascendida 8, 9 y 10» — la v6.111 la re-creó como
                // LAS OFANIM y la v6.112 la CONSERVA: la única de la
                // segunda década que vive). La regla permanente: todo lo
                // nuevo llega DIRECTO.
                entregados += EntregarSiNoTiene(ModContent.ItemType
                    <global::AethonMod.Content.Items.Cosmetics.FormaAscendidaOchoItem>());
                // v6.114 — LA BOLSA DE LA FORMA ETERNA (la letra: «los
                // accesorios mencionados se deben guardar en una nueva
                // bolsa junto con el accesorio definitivo»): el kit del
                // cosmético definitivo — LOS OCHO DEL PANTEÓN + la Forma
                // Eterna dentro. La regla de la casa: DIRECTO.
                entregados += EntregarSiNoTiene(ModContent.ItemType
                    <global::AethonMod.Content.Items.Bolsas.BolsaFormaEterna>());
                // v6.141 — LAS ALAS RÚNICAS DIRECTO (la letra: «no veo
                // las alas, lo mas probables es que no se lo diste al
                // jugador»): la copia exacta de la Valquiria Lisa con el
                // ciclo dorado→morado llega al inventario desde ya — la
                // regla de la casa: lo nuevo llega.
                entregados += EntregarSiNoTiene(ModContent.ItemType
                    <global::AethonMod.Content.Items.Alas.AlasRunicas>());
                // v6.141 — EL OJO QUE MIRA (la letra: «para todo esto
                // crea un arma nueva, no olvides darsela al jugador al
                // terminarla»): LA NUEVA ARMA DEL OJO — el ojo aparece
                // cerca del enemigo, su mirada marca, la marca enciende
                // la quemadura del vacío (PvP incluido). DIRECTO.
                entregados += EntregarSiNoTiene(ModContent.ItemType
                    <global::AethonMod.Content.Weapons.Ojos.ElOjoQueMira>());
                // v6.153 — LA SOMBRA DE TINTA MURIÓ (la letra: «el arma,
                // sombra de tinta borrala») — su entrega también.
                // v6.115 — EL MUNDO INTERIOR DEL ETERNO (la letra: «para
                // que el altar nos lleve a otro mundo crea un nuevo item
                // que se fabrica con esencia del eterno en el altar»):
                // la puerta del trago — el arte de las mil esencias
                // vive en el altar, pero el kit de pruebas la trae
                // DIRECTO (la regla de la casa) para que el interior
                // se pueda probar de una vez.
                entregados += EntregarSiNoTiene(ModContent.ItemType
                    <global::AethonMod.Content.Items.Llamados.MundoInteriorEternoItem>());
                // v6.123 — LA BOLSA DE LA SUPERFORJA (la letra: «se supone
                // que creaste nuevas armas, donde estan, recuerda que debes
                // darselas al jugador en una bolsa»): las TRES ARMAS
                // DEFINITIVAS de la superforja (El Juicio de los Dos Soles,
                // El Tajo de Oscuridad y Las Agujas del Vacío) llegan
                // DIRECTO en su bolsa — también viven en el Cofre del
                // Arsenal y tras sus recetas de 5 maderas.
                entregados += EntregarSiNoTiene(ModContent.ItemType
                    <global::AethonMod.Content.Items.Bolsas.BolsaSuperforja>());

                // v6.160 — EL TRÍO DEL ETERNO (la regla de la casa, la
                // letra: «como regla todo item nuevo debe ser dado
                // directamente al jugador»): el arma de las tres armas
                // llega DIRECTO al inventario al entrar al mundo — la
                // receta de 5 maderas queda como vía alternativa.
                entregados += EntregarSiNoTiene(ModContent.ItemType
                    <global::AethonMod.Content.Weapons.Investigaciones.TrioDelEterno>());

                if (entregados > 0)
                {
                    Main.NewText(Terraria.Localization.Language.GetTextValue(
                        "Mods.AethonMod.Mensajes.RegaloPruebas", entregados),
                        new Microsoft.Xna.Framework.Color(255, 240, 180));
                }
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("ShardPlayer", exRegistro); }
        }

        /// <summary>
        /// True si el jugador ya lleva un ítem de este tipo puesto o en el
        /// inventario (revisa inventario, armadura y misc — v6.50.54 — EL FIX
        /// DEL CRASH DEL client.log: los bucles iban a CIFRAS FIJAS (59/20/10)
        /// y en tML 2026.08 el array de inventario MIDO MENOS en algunos
        /// contextos → IndexOutOfRangeException en OnEnterWorld: si el jugador
        /// NO llevaba encima un ítem del regalo, la entrega MORÍA en el índice
        /// 58 y los ítems NUNCA llegaban (la mascota del log del usuario). LA
        /// CURA: los bucles leen .Length — el tamaño REAL, sea el que sea).
        /// </summary>
        private static bool YaLoTiene(Player p, int tipo)
        {
            for (int i = 0; i < p.inventory.Length; i++)
                if (p.inventory[i] != null && !p.inventory[i].IsAir && p.inventory[i].type == tipo)
                    return true;
            for (int i = 0; i < p.armor.Length; i++)
                if (p.armor[i] != null && !p.armor[i].IsAir && p.armor[i].type == tipo)
                    return true;
            for (int i = 0; i < p.miscEquips.Length; i++)
                if (p.miscEquips[i] != null && !p.miscEquips[i].IsAir && p.miscEquips[i].type == tipo)
                    return true;
            return false;
        }

        /// <summary>Entrega UNA copia si no la tiene; devuelve 1 si entregó.</summary>
        private int EntregarSiNoTiene(int tipo)
        {
            if (YaLoTiene(Player, tipo)) return 0;

            // Al inventario (primer hueco libre de las 50 primeras ranuras).
            int hueco = -1;
            for (int i = 0; i < 50; i++)
                if (Player.inventory[i] == null || Player.inventory[i].IsAir) { hueco = i; break; }

            Item nuevo = new Item();
            nuevo.SetDefaults(tipo);
            if (hueco >= 0)
                Player.inventory[hueco] = nuevo;
            else
                Player.QuickSpawnItem(Player.GetSource_Misc("RegaloDePruebas"), tipo, 1);
            return 1;
        }

        public override void SaveData(TagCompound tag)
        {
            // v6.50.15 — ARMADURA (auditoría R55-c): tML captura las
            // excepciones del SaveData y escribe un tag "error", pero el
            // cuerpo es gratis de blindar — el patrón de la casa.
            try
            {
                tag["resonanceShards"] = ResonanceShards;
                tag["firstLevelUpTriggered"] = FirstLevelUpTriggered;
                // v6.48 — la crónica del festín y el derecho a las esencias.
                tag["cronicaJefes"] = CronicaJefes;
                tag["cronicaNarrada"] = CronicaNarrada;
                tag["derrotaOleada10"] = DerrotaOleada10;
                // v6.50.59 — EL NIVEL DE FURIA (la progresión de las oleadas
                // naturales: vencer sube, morir congela).
                // v6.50.74 — EL MARCADOR furiaV2: distingue los guardados
                // de la era en la que TODA victoria (incluidas las de la
                // CARNADA de pruebas) subía el nivel natural.
                tag["furiaNivel"] = FuriaNivel;
                tag["furiaV2"] = true;
                // v6.50.110 — LA PRIMERA CENA: los jefes ya devorados
                tag["jefesDevorados"] = JefesDevorados;
                // v6.137 — LA CUENTA DE LA ARENA (los kills interiores
                // por jefe — el poder +25% por muerte persiste).
                tag["muertesInteriorTipos"] = MuertesInteriorTipos;
                tag["muertesInteriorCuenta"] = MuertesInteriorCuenta;
                // v6.175 — LAS INVOCACIONES DEL PILAR (la segunda llamada
                // del Devorador desata la ASPIRADORA del grimorio)
                tag["devoramundosPedacito"] = DevoramundosPedacito;
                // v6.182 — LA MIGRACIÓN DEL FÓSIL DEL FESTÍN: los
                // guardados de la era v6.180-v6.181 pudieron quemar la
                // marca con un GUARDIÁN DE OLEADA (la autopsia del festín
                // que no iniciaba) — la mecánica cambió: se revive UNA vez
                tag["pedacitoV2"] = true;
                tag["primeraVezCronista"] = PrimeraVezCronista;
                // v6.50.115 — EL FESTÍN DEL GUARDIÁN (el easter egg de la
                // Mazmorra: una vez por portador)
                tag["guardianDevorado"] = GuardianMazmorraDevorado;
                // v6.50.117 — LA CAÍDA: la marca del libro entregado por
                // el cometa (un grimorio por jugador, para siempre).
                tag["caidaRecibida"] = CaidaRecibida;
                // v6.50.120 — LA BOLSA A LA MANO: la marca de la cría
                // entregada (la pareja es suya, una vez por portador).
                tag["bolsaCriaEntregada"] = BolsaCriaEntregada;
                // v6.50.128 — EL PODER DEVORADO (lo comido en el altar es
                // para SIEMPRE — mejora permanente del Grimorio del Eterno).
                tag["devoracionPoder"] = DevoracionPoder;
                tag["devoracionV2"] = true;
                // v6.51.11 — EL FESTÍN DEL PORTADOR (las esencias que EL
                // JUGADOR comió: +0,1 % de daño en todo, tope +100 %).
                tag["esenciasConsumidas"] = EsenciasConsumidas;
                // v6.51.12 — EL RELOJ LARGO DEL CORO (las 2 h de juego
                // entre coros de hadas — persiste: son horas DE JUEGO).
                tag["ticksSinCoro"] = TicksSinCoro;
                // v6.50.129 — EL FESTÍN DE LAS ARMAS (EXP por rareza + los
                // stats por clase + las almas únicas — todo permanente).
                tag["devoracionExp"] = DevoracionExp;
                tag["devoracionFuerza"] = DevoracionFuerza;
                tag["devoracionPrisa"] = DevoracionPrisa;
                tag["devoracionMirada"] = DevoracionMirada;
                tag["devoracionCorona"] = DevoracionCorona;
                tag["devoracionMordida"] = DevoracionMordida;
                tag["almaIgnea"] = AlmaIgnea;
                tag["almaSalvaje"] = AlmaSalvaje;
                tag["almaVampira"] = AlmaVampira;
                tag["corazonEstelar"] = CorazonEstelar;
                tag["almaVeloz"] = AlmaVeloz;
                tag["punoTitan"] = PunoTitan;
                // v6.51.0 — EL DESPERTAR (el nivel y la mascota del 20).
                tag["nivelDespertar"] = NivelDespertar;
                tag["petDespertar"] = PetDelDespertar;
                // v6.170 — LAS HISTORIAS DEL CRONISTA (lo que ya narró)
                tag["loreDesbloqueado"] = LoreDesbloqueado;
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("ShardPlayer", exRegistro); }
        }

        public override void LoadData(TagCompound tag)
        {
            try
            {
                ResonanceShards = tag.GetInt("resonanceShards");
                FirstLevelUpTriggered = tag.GetBool("firstLevelUpTriggered");
                CronicaJefes = new System.Collections.Generic.List<int>(tag.GetList<int>("cronicaJefes"));
                CronicaNarrada = tag.GetInt("cronicaNarrada");
                DerrotaOleada10 = tag.GetBool("derrotaOleada10");
                // v6.50.59 — EL NIVEL DE FURIA (clamp 1..10 — un guardado
                // corrupto jamás rompe el sistema).
                // v6.50.74 — LA MIGRACIÓN DE UNA SOLA VEZ: los guardados
                // sin el marcador furiaV2 traen un nivel PLAGADO por las
                // victorias de la carnada (la letra: «la oleada natural
                // siempre empieza por el nivel 3, se salta el 1 y el 2») —
                // se resetea a 1 y el marcador sella la era nueva (solo
                // las victorias NATURALES suben desde ahora).
                if (!tag.ContainsKey("furiaV2"))
                    FuriaNivel = 1;
                else
                    FuriaNivel = Math.Max(1, Math.Min(10, tag.GetInt("furiaNivel")));
                // v6.50.110 — LA PRIMERA CENA: los jefes ya devorados
                JefesDevorados = new System.Collections.Generic.List<int>(tag.GetList<int>("jefesDevorados"));
                // v6.170 — LAS HISTORIAS DEL CRONISTA (ausente = 0: el
                // guardado viejo escucha al Cronista desde cero)
                LoreDesbloqueado = tag.GetInt("loreDesbloqueado");
                MuertesInteriorTipos = new System.Collections.Generic.List<int>(tag.GetList<int>("muertesInteriorTipos"));
                MuertesInteriorCuenta = new System.Collections.Generic.List<int>(tag.GetList<int>("muertesInteriorCuenta"));
                // v6.175 — LAS INVOCACIONES DEL PILAR (ausente = lista
                // vacía: los guardados viejos estrenan su Devorador)
                DevoramundosPedacito = tag.GetBool("devoramundosPedacito");
                // v6.182 — LA MIGRACIÓN DEL FÓSIL: los guardados de la era
                // v6.180-v6.181 traen la marca quemada por el agujero de
                // las oleadas — la mecánica NUEVA la revive una única vez
                if (!tag.ContainsKey("pedacitoV2"))
                    DevoramundosPedacito = false;
                PrimeraVezCronista = tag.GetBool("primeraVezCronista");
                // v6.50.115 — EL FESTÍN DEL GUARDIÁN (ausente = false: aún
                // no ocurrió — el guardado viejo estrena el easter egg)
                GuardianMazmorraDevorado = tag.GetBool("guardianDevorado");
                // v6.50.117 — LA CAÍDA (ausente = false: el guardado viejo
                // todavía puede recibir su cometa de tinta)
                CaidaRecibida = tag.GetBool("caidaRecibida");
                // v6.50.120 — LA BOLSA A LA MANO (ausente = false: el
                // guardado viejo aún puede recibir a la cría en su primera
                // victoria de la familia de jefes)
                BolsaCriaEntregada = tag.GetBool("bolsaCriaEntregada");
                // v6.50.128 — EL PODER DEVORADO (ausente = 0: los guardados
                // viejos estrenan el altar con el poder intacto; clamp
                // anticornografía de siempre).
                DevoracionPoder = Math.Max(0, Math.Min(999999, tag.GetInt("devoracionPoder")));
                // v6.51.11 — EL FESTÍN DEL PORTADOR (ausente = 0: el
                // guardado viejo estrena el festín limpio).
                EsenciasConsumidas = Math.Max(0, Math.Min(999999, tag.GetInt("esenciasConsumidas")));
                // v6.51.12 — EL RELOJ LARGO DEL CORO (persistente).
                TicksSinCoro = Math.Max(0, Math.Min(int.MaxValue - 1, tag.GetInt("ticksSinCoro")));
                // v6.50.129 — EL FESTÍN DE LAS ARMAS (ausente = 0 en todos:
                // el guardado viejo estrena el festín limpio; clamps).
                DevoracionExp = Math.Max(0L, Math.Min(999999999L, tag.GetLong("devoracionExp")));
                DevoracionFuerza = Math.Max(0, Math.Min(9999, tag.GetInt("devoracionFuerza")));
                DevoracionPrisa = Math.Max(0, Math.Min(9999, tag.GetInt("devoracionPrisa")));
                DevoracionMirada = Math.Max(0, Math.Min(9999, tag.GetInt("devoracionMirada")));
                DevoracionCorona = Math.Max(0, Math.Min(9999, tag.GetInt("devoracionCorona")));
                DevoracionMordida = Math.Max(0, Math.Min(9999, tag.GetInt("devoracionMordida")));
                AlmaIgnea = Math.Max(0, Math.Min(99, tag.GetInt("almaIgnea")));
                AlmaSalvaje = Math.Max(0, Math.Min(99, tag.GetInt("almaSalvaje")));
                AlmaVampira = Math.Max(0, Math.Min(99, tag.GetInt("almaVampira")));
                CorazonEstelar = Math.Max(0, Math.Min(99, tag.GetInt("corazonEstelar")));
                AlmaVeloz = Math.Max(0, Math.Min(99, tag.GetInt("almaVeloz")));
                PunoTitan = Math.Max(0, Math.Min(99, tag.GetInt("punoTitan")));
                // v6.51.0 — EL DESPERTAR (ausente = 0: los guardados
                // viejos estrenan la mecánica desde el nivel 0 — la
                // letra: «siempre empieza desde nivel 0»).
                // v6.51.6 — LEGADO: estos valores ya no mandan — al
                // entrar al mundo, la MIGRACIÓN los lleva AL PRIMER
                // GRIMORIO del inventario (AlmaLegacyPendiente) y el
                // jugador queda en cero.
                NivelDespertar = Math.Max(0, Math.Min(
                    Content.Systems.DespertarGrimorio.NivelMax,
                    tag.GetInt("nivelDespertar")));
                PetDelDespertar = tag.GetBool("petDespertar");
                // v6.51.6 — LA MARCA DE LA MIGRACIÓN: si el guardado trae
                // ALGO del viejo despertar/festín del jugador, caminará al
                // primer Eterno del inventario (una sola vez).
                if (NivelDespertar > 0 || PetDelDespertar || DevoracionPoder > 0 ||
                    DevoracionExp > 0 || DevoracionFuerza > 0 || DevoracionPrisa > 0 ||
                    DevoracionMirada > 0 || DevoracionCorona > 0 || DevoracionMordida > 0 ||
                    AlmaIgnea > 0 || AlmaSalvaje > 0 || AlmaVampira > 0 ||
                    CorazonEstelar > 0 || AlmaVeloz > 0 || PunoTitan > 0)
                    AlmaLegacyPendiente = true;
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("ShardPlayer", exRegistro); }
        }
    }
}
