using System;
using Microsoft.Xna.Framework;
using Terraria;
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

        /// <summary>v6.51.0 — EL DESPERTAR DEL GRIMORIO (la nueva
        /// progresión, la letra: «empieza a nivel 0… máximo 50 por
        /// ahora»): el nivel que desbloquea los poderes del grimorio
        /// (mejoras de los ojos, el sol rúnico, los agujeros negros,
        /// la mascota del nivel 20) y suma +1 % de daño por nivel a
        /// TODOS los tipos de daño del grimorio.</summary>
        public int NivelDespertar;

        /// <summary>v6.51.0 — LA MASCOTA DEL ESCUPIDIZO (nivel 20 del
        /// despertar: el grimorio la escupió UNA VEZ — la ceremonia no
        /// se repite).</summary>
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

        /// <summary>EL BONUS DE DAÑO del Eterno por lo devorado (0 → +0,75).
        public static float BonusDanoDevorado(Player p)
        {
            if (p == null) return 0f;
            var sp = p.GetModPlayer<ShardPlayer>();
            if (sp == null) return 0f;
            // v6.51.1 — LA LETRA: «incluso con el despertar al máximo, se
            // debe poder seguir devorando esencia corrupta y esta debe
            // aumentar el daño del grimorio del eterno en 0.1% por cada
            // esencia corrupta» — 0,1% por esencia, SIN techo práctico
            // (el tope del .128 de 300 murió con el altar).
            return Math.Min(sp.DevoracionPoder, 5000) * 0.001f;
        }

        /// <summary>LOS OJOS EXTRA de la corona por lo devorado (+1 cada 75,
        /// tope +3 — el escuadrón del banquete).</summary>
        public static int OjosExtraDevorados(Player p)
        {
            if (p == null) return 0;
            var sp = p.GetModPlayer<ShardPlayer>();
            if (sp == null) return 0;
            return Math.Min(sp.DevoracionPoder / 75, 3);
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
        public static float BonusDanoArmasDevoradas(Player p)
        {
            if (p == null) return 0f;
            var sp = p.GetModPlayer<ShardPlayer>();
            if (sp == null) return 0f;
            return Math.Min(sp.DevoracionFuerza * 0.0035f +
                Math.Min(sp.DevoracionExp * 0.0002f, 0.40f), 0.45f);
        }

        /// <summary>LA VELOCIDAD DEL FESTÍN: prisa (+0,25 %/pto) + alma
        /// veloz (+2 %/copa) — tope conjunto +35 %.</summary>
        public static float BonusVelocidadArmasDevoradas(Player p)
        {
            if (p == null) return 0f;
            var sp = p.GetModPlayer<ShardPlayer>();
            if (sp == null) return 0f;
            return Math.Min(sp.DevoracionPrisa * 0.0025f + sp.AlmaVeloz * 0.02f, 0.35f);
        }

        /// <summary>EL CRÍTICO DEL FESTÍN: mirada (+0,30 %/pto) + corazón
        /// estelar (+1,5 %/copia) — tope conjunto +30 %.</summary>
        public static float BonusCritArmasDevoradas(Player p)
        {
            if (p == null) return 0f;
            var sp = p.GetModPlayer<ShardPlayer>();
            if (sp == null) return 0f;
            return Math.Min(sp.DevoracionMirada * 0.0030f + sp.CorazonEstelar * 0.015f, 0.30f);
        }

        /// <summary>EL EMPUJE DEL FESTÍN: mordida (+0,6 %/pto) + puño
        /// titán (+12 %/copia) — tope conjunto +75 %.</summary>
        public static float BonusEmpujeArmasDevoradas(Player p)
        {
            if (p == null) return 0f;
            var sp = p.GetModPlayer<ShardPlayer>();
            if (sp == null) return 0f;
            return Math.Min(sp.DevoracionMordida * 0.006f + sp.PunoTitan * 0.12f, 0.75f);
        }

        /// <summary>LOS OJOS DE LA CORONA DEL FESTÍN: cada 5 armas de
        /// invocación comidas, UN ojo más (tope +3 — suma con los del
        /// poder devorado).</summary>
        public static int OjosCoronaDevorados(Player p)
        {
            if (p == null) return 0;
            var sp = p.GetModPlayer<ShardPlayer>();
            if (sp == null) return 0;
            return Math.Min(sp.DevoracionCorona / 5, 3);
        }

        /// <summary>EL ROBO DE VIDA DEL ALMA VAMPIRA (+2 %/copia, tope 8 %).</summary>
        public static float RoboVidaAlmas(Player p)
        {
            if (p == null) return 0f;
            var sp = p.GetModPlayer<ShardPlayer>();
            if (sp == null) return 0f;
            return Math.Min(sp.AlmaVampira * 0.02f, 0.08f);
        }

        /// <summary>LA PROBABILIDAD DE PRENDER del alma ígnea (+6 %/copia, tope 30 %).</summary>
        public static float ProbIgnea(Player p)
        {
            if (p == null) return 0f;
            var sp = p.GetModPlayer<ShardPlayer>();
            if (sp == null) return 0f;
            return Math.Min(sp.AlmaIgnea * 0.06f, 0.30f);
        }

        /// <summary>LA PROBABILIDAD DE ENVENENAR del alma salvaje (+6 %/copia, tope 30 %).</summary>
        public static float ProbSalvaje(Player p)
        {
            if (p == null) return 0f;
            var sp = p.GetModPlayer<ShardPlayer>();
            if (sp == null) return 0f;
            return Math.Min(sp.AlmaSalvaje * 0.06f, 0.30f);
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
            catch { }
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

        /// <summary>Segundos por momento de hambre ("si pasas minutos sin matar").</summary>
        public const int SegundosPorMomento = 75;
        /// <summary>Momentos que tarden la furia (~5 minutos sin comer).</summary>
        public const int MomentosParaFuria = 4;
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
                catch { }
            }
            if (soloSostenido) return 0;

            for (int i = 0; i < 10; i++)
            {
                Item inv = Player.inventory[i];
                if (inv == null || inv.type != ModContent.ItemType<Weapons.GrimoireEternal>())
                    continue;
                try
                {
                    var s = inv.GetGlobalItem<ShardLevelItem>();
                    if (s != null) return s.Level;
                }
                catch { }
                break; // la PRIMERA copia de la barra rápida manda
            }
            return 0;
        }

        /// <summary>
        /// PostUpdateEquips: los bonuses que persisten mientras el libro
        /// esté SOSTENIDO o en la BARRA RÁPIDA. Corre tras ResetEffects:
        /// las stats aquí escritas cuentan de verdad.
        ///
        /// v6.45: el bonus de invocación, el crítico mágico y la
        /// penetración movidos desde GrimoireEternal.ModifyWeaponDamage
        /// (el sitio EQUIVOCADO: ese hook solo corre al calcular el daño
        /// del propio grimorio, así que los minions atacando en otros
        /// ticks no recibían nada — letra muerta de la clase R44).
        ///
        /// v6.46: la separación de estados — el COMBATE (daño de
        /// invocación, crítico, penetración) exige SOSTENER el libro; la
        /// capacidad (slots de minion) y los recursos (vida/maná, tope
        /// +100) sobreviven en la barra rápida.
        /// </summary>
        public override void PostUpdateEquips()
        {
            int nivel = NivelLibro(false);   // sostenido O barra rápida

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
            int techoOjos = WeaponScaling.TechoOjosEterno(equipoInvocador, nivel);
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
                // EL ESCUADRÓN ENTERO (los 6 minions de la casa).
                int tipoOjo = ModContent.ProjectileType
                    <global::AethonMod.Content.Projectiles.OjoEternoMinion>();
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
                    if (p.type == tipoOjo || p.type == tipoSupernova || p.type == tipoPulsar ||
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

            if (nivel <= 0) return;
            bool sostenido = NivelLibro(true) > 0;

            // === CAPACIDAD (sostener O barra rápida) ===

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

            // === RECURSOS: completos al sostener, tope +100 en la barra ===
            int bonusVida = WeaponScaling.BonusLife(nivel);
            int bonusMana = WeaponScaling.BonusMana(nivel);
            if (!sostenido)
            {
                // v6.46: sin blandir el libro, el cuerpo solo tolera un
                // préstamo de +100 vida y +100 maná — el resto exige
                // sostenerlo.
                if (bonusVida > 100) bonusVida = 100;
                if (bonusMana > 100) bonusMana = 100;
            }
            Player.statLifeMax2 += bonusVida;
            Player.statManaMax2 += bonusMana;

            // === PODER DE COMBATE: exige SOSTENER el libro ===
            if (!sostenido) return;

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
                Player.GetCritChance(DamageClass.Magic) += WeaponScaling.CritBonus(nivel);
                // Armor penetration +2% cada 5 niveles (Máximo 50%)
                Player.GetArmorPenetration(DamageClass.Magic) += WeaponScaling.ArmorPenBonus(nivel);
            }
            catch { }
        }

        /// <summary>
        /// PostUpdateBuffs: regeneración de mana y vida.
        /// v6.44 (auditoría R44): EL HOOK CORRECTO. En el binario real,
        /// Player.Update consume lifeRegen/manaRegen en UpdateLifeRegen y
        /// UpdateManaRegen, que corren ANTES de PostUpdate — las escritas
        /// del hook viejo eran letra muerta (ResetEffects las borra al
        /// tick siguiente sin que nadie las lea). PostUpdateBuffs corre
        /// entre ResetEffects y el consumo: aquí sí cuentan.
        /// v6.46: la regeneración es poder del libro — SOLO sostenido.
        /// </summary>
        public override void PostUpdateBuffs()
        {
            int level = NivelLibro(true);
            if (level <= 0) return;

            // Regeneración de mana (por segundo)
            // v6.44 (auditoría R44): Player.manaRegen lo REESCRIBE
            // UpdateManaRegen desde cero cada tick — el campo que
            // ACUMULA aportes externos es manaRegenBonus. Unidades del
            // motor: 120 cuentas = 1 maná → para R maná/seg hay que
            // aportar 2·R cuentas por tick.
            int manaRegen = WeaponScaling.ManaRegen(level);
            if (manaRegen > 0 && Player.statMana < Player.statManaMax2)
            {
                Player.manaRegenBonus += manaRegen * 2;
            }

            // Regeneración de vida (por segundo)
            float lifeRegen = WeaponScaling.LifeRegen(level);
            if (lifeRegen > 0 && Player.statLife < Player.statLifeMax2)
            {
                Player.lifeRegen += (int)(lifeRegen * 2); // lifeRegen es en 1/2 vida/seg
            }
        }

        /// <summary>
        /// Modifica el daño recibido (reducción por nivel del Grimorio).
        /// v6.46: reducción de daño = poder del libro — SOLO sostenido.
        /// </summary>
        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            int level = NivelLibro(true);
            if (level <= 0) return;

            float reduction = WeaponScaling.DamageReduction(level);
            if (reduction > 0f)
            {
                modifiers.FinalDamage *= 1f - reduction;
            }
        }

        public override void OnHurt(Player.HurtInfo info) { }

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
        public override void PostUpdate()
        {
            // v6.51.0 — LOS ATAQUES COMBINADOS DEL DESPERTAR (el sol
            // rúnico y los agujeros negros de los ojos): el coordinador
            // corre en la PANTALLA del dueño (cada jugador con SUS ojos).
            try
            {
                Content.Systems.AtaquesCombinadosSistema.Tick(Player);
            }
            catch { }

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
                                if (evento && !GrimorioFuriaSistema.Activo && GrimorioFuriaSistema.MundoLibre())
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
                        if (eventoR && !GrimorioFuriaSistema.Activo &&
                            GrimorioFuriaSistema.MundoLibre())
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
                if (local && MomentosHambre > 0 && !Player.dead)
                {
                    if (!EspiarPortador(0))
                    {
                        Projectile.NewProjectile(Player.GetSource_Misc("AuraHambre"),
                            Player.Center, Vector2.Zero,
                            ModContent.ProjectileType<Projectiles.Cosmetic.AuraPortadorHalo>(),
                            0, 0f, Player.whoAmI, 0f);
                    }
                }
            }
            catch { }
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
                entregados += EntregarSiNoTiene(ModContent.ItemType
                    <global::AethonMod.Content.Items.Cosmetics.FormaAscendidaDosItem>());
                entregados += EntregarSiNoTiene(ModContent.ItemType
                    <global::AethonMod.Content.Items.Cosmetics.FormaAscendidaTresItem>());
                // v6.50.53 — LA FORMA ASCENDIDA 4 (EL SERAFÍN — la cuarta luz).
                entregados += EntregarSiNoTiene(ModContent.ItemType
                    <global::AethonMod.Content.Items.Cosmetics.FormaAscendidaCuatroItem>());
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

                if (entregados > 0)
                {
                    Main.NewText(Terraria.Localization.Language.GetTextValue(
                        "Mods.AethonMod.Mensajes.RegaloPruebas", entregados),
                        new Microsoft.Xna.Framework.Color(255, 240, 180));
                }
            }
            catch { }
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
            }
            catch { }
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
                NivelDespertar = Math.Max(0, Math.Min(
                    Content.Systems.DespertarGrimorio.NivelMax,
                    tag.GetInt("nivelDespertar")));
                PetDelDespertar = tag.GetBool("petDespertar");
            }
            catch { }
        }
    }
}
