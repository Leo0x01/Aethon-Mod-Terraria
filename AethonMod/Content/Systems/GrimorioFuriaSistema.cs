using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    /// v6.50.29 — EL MOTOR DE VANILLA (la orden del usuario: «deberías
    /// investigar cómo funcionan las oleadas reales en terraria, y usar
    /// su mismo sistema, solo es cambiar los valores para aumentarla a
    /// medida que el nivel de oleada del grimorio aumente y además poner
    /// un indicador de oleada igual que los que se usan en terraria
    /// original»). LA INVESTIGACIÓN (decompile del Terraria real
    /// 2026.07.3.0 — NPC.SpawnNPC, CheckProgressFrostMoon, Main.
    /// DrawInvasionProgress):
    ///
    /// · CÓMO SPAWNEAN LAS LUNAS DE CALABAZA/ESCAMARCHA: NUNCA llaman a
    ///   NewNPC ellas mismas — multiplican el MOTOR NATURAL de spawn
    ///   (spawnRate × 0.2, maxSpawns × 2) y REEMPLAZAN el pool de la
    ///   zona. El motor de vanilla elige el tile (dentro del anillo
    ///   0.52–0.7× pantalla alrededor del jugador — JUSTO FUERA DEL
    ///   CUADRO, «deberían estar más cerca»), valida el suelo, el aire,
    ///   el agua y la lava — imposible nacer en pared. tModLoader
    ///   expone EXACTAMENTE esos mandos: GlobalNPC.EditSpawnRate +
    ///   GlobalNPC.EditSpawnPool (viven en OleadaNPC).
    /// · CÓMO AVANZAN: POR MUERTES. Cada NPC del evento vale puntos (la
    ///   chusma 1–5, los mini-jefes 10–50, Pumpking/Reina 100–150 ×
    ///   expert/master); waveKills acumula y al cruzar la tabla
    ///   MoonEventRequiredPointsPerWaveLookup[waveNumber] (25, 40, 50,
    ///   80, 100, 160…) → waveKills=0, waveNumber++ y anuncio en el
    ///   chat. NOSOTROS: chusma = 1 punto, requeridos = 12+6k (la 1 se
    ///   paga con 18 muertes, la 10 con 72) — «solo es cambiar los
    ///   valores».
    /// · CÓMO SE VE: Main.ReportInvasionProgress → DrawInvasionProgress
    ///   pinta ABAJO A LA DERECHA la caja «Oleada {0}: {1}%» + la barra
    ///   amarilla/naranja/negra + la cajita del título con el icono del
    ///   evento. Clonado EXACTO en PostDrawInterface (los pinceles son
    ///   los de vanilla: Utils.DrawInvBG, ColorBar, MagicPixel,
    ///   DrawBorderString) con NUESTRO icono (el grimorio) y
    ///   NUESTRO título («Furia del grimorio»).
    ///
    /// EL CUENTO ENTERO:
    /// 1. LA VOZ DEL HAMBRE (ShardPlayer): con el libro a nivel alto
    ///    (25+), cada 75 segundos sin matar es un MOMENTO DE HAMBRE —
    ///    EcoLib susurra (con EL SAJOR DEL BIOMA en la primera línea)
    ///    y la barra dorada palidece un poco más. Pure flavor… hasta
    ///    que no.
    /// 2. LA FURIA: al cuarto momento (~5 minutos sin comer), el libro
    ///    pierde la paciencia: "El grimorio está furioso…" y luego
    ///    "El grimorio llama a su comida…".
    /// 3. EL EVENTO (5 minutos de oleadas): UNA OLEADA POR CADA MOMENTO
    ///    DE HAMBRE acumulado, hasta 10. Cada oleada: LA CHUSMA del
    ///    bioma NACE DEL MOTOR NATURAL DE VANILLA (EditSpawnRate ×0.2 y
    ///    menguando por oleada — la 10 escupe a ×0.08; tope de vivos
    ///    10+4k) con stats ×(k+1) (la 1 ×2, la 10 ×11), y AL FINAL UN
    ///    JEFE PRE-HARDMODE del bioma — SIN importar la hora. La
    ///    oleada AVANZA por MUERTES (los puntos) con el reloj solo de
    ///    red de seguridad. El pago: k monedas de oro por monstruo, k
    ///    de platino + SU ESENCIA por jefe, y TODO paga XP ×(k+1).
    /// 4. LA OLEADA ESPECIAL — EL JUICIO (tras la 10 o directa con la
    ///    Carnada en 11): TODOS los guardianes a la vez, ×15 en TODO y
    ///    sin pausa en los dientes. Empiezan DOS y el resto se va sumando
    ///    cada 15 s — el festín final. Cuando cae el último: "El grimorio
    ///    está saciado… por ahora." y el perdón de la hambre.
    /// 5. LA MUERTE DEL PORTADOR (v6.48): si el jugador cae durante el
    ///    festín, EL EVENTO TERMINA y el libro TOMA VENGANZA — varias
    ///    líneas (la voz del libro SIEMPRE con prioridad: se cuela al
    ///    frente de la cola de EcoLib y las demás voces esperan detrás).
    ///
    /// SP-FIRST: la máquina corre en el servidor del mundo (en SP es el
    /// mismo proceso — las voces de EcoLib y los chat funcionan); en MP
    /// dedicado las voces son TODO pendiente como el resto del sync de
    /// la casa. La Carnada del Grimorio (ítem de prueba) dispara esto
    /// SIEMPRE, aunque la config tenga el hambre automática apagada (y
    /// cicla 1..10 y LA ESPECIAL).
    /// </summary>
    public class GrimorioFuriaSistema : ModSystem
    {
        // === LAS FASES DEL EVENTO ===
        private enum Fase { Inactivo, Llamada, Monstruos, Jefe, Interludio, Especial, Fin }

        // === EL ESTADO (servidor del mundo; SP = el mismo proceso) ===
        private static Fase _fase = Fase.Inactivo;
        private static int _ticksFase = 0;
        private static int _oleadasTotales = 0;    // N (1..10, 11 = solo especial)
        private static int _oleadaActual = 0;      // k (1..N)
        private static int _ticksOleada = 0;       // reloj de la red de seguridad
        // ==================================================================
        //  v6.50.59 — EL SISTEMA DE NIVELES DE FURIA (la letra del usuario:
        //  «cuando inicia la primera oleada natural siempre debe empezar
        //  en nivel 1, luego si el jugador logra vencer el nivel 1 de esa
        //  oleada, la siguiente sera de nivel 2, o sea 2 oleadas, si el
        //  jugador logra vencer esa oleada, la siguiente sera de nivel 3,
        //  o sea 3 oleadas, y la fuerza, poder, vida y defensa de los
        //  monstruos aumenta en consecuencia del nivel de las oleadas»):
        //  el festín de nivel N trae N OLEADAS CONSECUTIVAS y TODAS sus
        //  criaturas escalan vida/daño/defensa/agresión con N (además de
        //  la oleada interna k). El nivel VIVE en el portador
        //  (ShardPlayer.FuriaNivel, persistente): nace en 1 y SOLO SUBE AL
        //  VENCER el festín completo (morir lo congela: la próxima repite
        //  el nivel — el libro no olvida, pero tampoco regala).
        // ==================================================================
        private static int _nivelFuria = 1;        // N (1..10) — el nivel del festín en marcha
        // v6.50.59 — LA MEZCLA DE BIOMAS DEL NIVEL (la letra: «a mayor
        // nivel de oleada mas monstruos de biomas diferentes aparecen,
        // siempre usando los biomas disponibles en el momento»): el festín
        // de nivel N mezcla el bioma ACTUAL del portador (peso doble, lo
        // sigue en TIEMPO REAL) + (N−1) biomas acompañantes elegidos al
        // arrancar cada oleada de la MESA DE BIOMAS DISPONIBLES del mundo
        // (el Santuario SOLO tras el Muro de Carne — «cuando se mate el
        // muro de carne los nuevos biomas son añadidos a las nuevas
        // oleadas a partir de ese momento, pero antes no»).
        private static List<int[]> _acompanantes = null; // los biomas extra de ESTA oleada
        private static string _firmaBioma = "";         // el bioma principal del portador AHORA
        // v6.50.29 — LOS PUNTOS DE LA OLEADA (el waveKills de vanilla): cada
        // muerte de chusma sellada suma 1; al cruzar PuntosRequeridos(k) la
        // oleada avanza — LA PROGRESIÓN ES DE MUERTES, como las lunas de
        // vanilla (CheckProgressFrostMoon: waveKills += puntos; si cruza la
        // tabla → waveNumber++). El reloj de abajo es SOLO la red de
        // seguridad (que el festín nunca se cuelgue si el portador no mata).
        private static int _puntosOleada = 0;
        // v6.50.29 — EL POOL DE LA OLEADA (caché): los tipos que el motor
        // natural puede escupir durante ESTA oleada — v6.50.59: ahora es un
        // DICCIONARIO PONDERADO (el bioma del portador PESA EL DOBLE que
        // los acompañantes del nivel) y VIVE EN TIEMPO REAL: cuando el
        // portador CAMBIA DE BIOMA, el principal se reconstruye al vuelo
        // (lo lee OleadaNPC.EditSpawnPool).
        private static Dictionary<int, float> _poolActual = null;
        private static int _bossIdx = -1;          // whoAmI del jefe de la oleada
        private static int _jugador = -1;          // whoAmI del hambriento
        // v6.50.2 — FIX (carrera del slot reciclado en FaseJefe): el TYPE del
        // jefe de la oleada, fijado al spawnear. El sello de OleadaNPC
        // valida el SLOT; el tipo valida la ESPECIE: un town NPC que recicle
        // el slot del jefe muerto en el MISMO tick (UpdateTime corre ANTES
        // de PostUpdateWorld) ya no puede colarse en la comparación del
        // sello — el crédito de la oleada y el timeout miran SOLO al jefe.
        private static int _tipoJefeOleada = -1;

        // ==================================================================
        //  v6.50.48 - EL CENSO DE LA BARRA + LA EXTENSION DEL DEVORADOR
        // ==================================================================

        // EL CENSO (el fix del desborde): la barra de vanilla de los
        // jefes multi-pieza divide las vidas ACTUALES (multiplicadas por
        // la oleada) entre la vida de una pieza FRESCA (vanilla, sin
        // multiplicar) por un numero ASUMIDO de piezas. La oleada 2
        // hacia desbordar UN marco; la 10, ONCE. El hook de
        // OleadaNPC.SetDefaults escala la referencia con EL MISMO
        // multiplicador y el FACTOR DE LARGO REAL (nacidas/asumidas).
        private static float _multDevorador = 1f;
        private static int _inicialDevorador = 0;   // piezas nacidas (cabeza+cuerpos+cola)
        private static float _multCerebro = 1f;
        private static int _inicialCerebro = 0;     // creepers nacidos

        // EL PENDIENTE DEL DEVORADOR: la cabeza acaba de nacer; la
        // cadena de vanilla se construye en los primeros AI. Cuando
        // este lista, la EXTENDemos (la letra: «el devorador debe ser
        // mas largo, al menos 3 veces mas largo»).
        private static int _pendCabeza = -1;        // whoAmI de la cabeza
        private static int _pendTicks = 0;         // la espera de la cadena
        private static int _pendIntentos = 0;      // reintentos antes de rendirse

        /// <summary>Pieza del Devorador nacida (la llama Marcar): el censo se refresca.</summary>
        public static void PiezaDevoradorNacio(float mult)
        {
            if (mult > _multDevorador) _multDevorador = mult;
            _inicialDevorador = ContarPiezas(NPCID.EaterofWorldsHead, NPCID.EaterofWorldsTail);
        }

        /// <summary>Creeper nacido (la llama Marcar): el censo se refresca.</summary>
        public static void PiezaCerebroNacio(float mult)
        {
            if (mult > _multCerebro) _multCerebro = mult;
            _inicialCerebro = ContarPiezas(NPCID.Creeper, NPCID.Creeper);
        }

        /// <summary>Las piezas VIVAS de la familia (min..max), selladas por la furia.</summary>
        private static int ContarPiezas(int min, int max)
        {
            int n = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC c = Main.npc[i];
                if (c == null || !c.active || c.type < min || c.type > max) continue;
                var sello = c.GetGlobalNPC<OleadaNPC>();
                if (sello != null && sello.EsDeOleada) n++;
            }
            return n;
        }

        /// <summary>
        /// LA ESCALA DE LA REFERENCIA DEL DEVORADOR para la barra:
        /// multiplicador de la oleada x factor de largo real
        /// (nacidas / asumidas). La llama OleadaNPC.SetDefaults cuando
        /// la barra reconstruye su cuerpo de referencia (cada frame).
        /// </summary>
        public static float EscalaBarraDevorador()
        {
            if (_inicialDevorador < 1 || _multDevorador <= 1f) return 1f;
            float asumidas = NPC.GetEaterOfWorldsSegmentsCount() + 2;
            return _multDevorador * (_inicialDevorador / asumidas);
        }

        /// <summary>LA ESCALA DE LA REFERENCIA DEL CEREBRO (los creepers).</summary>
        public static float EscalaBarraCerebro()
        {
            if (_inicialCerebro < 1 || _multCerebro <= 1f) return 1f;
            float asumidos = NPC.GetBrainOfCthuluCreepersCount();
            return _multCerebro * (_inicialCerebro / asumidos);
        }

        /// <summary>
        /// LA EXTENSION DEL DEVORADOR (la letra del usuario: «el devorador
        /// debe ser mas largo, al menos 3 veces mas largo»): cuando la
        /// cadena de vanilla esta completa, se INSERTAN cuerpos nuevos
        /// entre el ultimo cuerpo y la cola, con la convencion de enlaze
        /// de AI_006 (ai[1] = el de adelante, ai[0] = el de atras) — el
        /// gusano crece sin romper la fisica. Tope 192 piezas: el motor
        /// solo tiene 200 NPC (en expert la cadena base ya es 72: la
        /// extension la lleva hasta el tope; en normal son 197 = 3.0x).
        /// Cada pieza nueva nace como HIJA del ultimo cuerpo: hereda el
        /// sello (aura, stats y XP de la oleada) — y el censo de la
        /// barra crece con la cadena.
        /// </summary>
        private static void ProcesarExtensionDevorador()
        {
            if (_pendCabeza < 0) return;
            try
            {
                NPC cabeza = (_pendCabeza < Main.maxNPCs) ? Main.npc[_pendCabeza] : null;
                if (cabeza == null || !cabeza.active || cabeza.type != NPCID.EaterofWorldsHead)
                { _pendCabeza = -1; return; }

                if (_pendTicks > 0) { _pendTicks--; return; }

                // ¿La cadena esta completa? Caminamos desde la cabeza
                // (ai[0] apunta al de atras) hasta la pieza sin atras:
                // ESA es la cola.
                NPC cola = null;
                int cur = _pendCabeza;
                for (int paso = 0; paso < 250; paso++)
                {
                    if (cur < 0 || cur >= Main.maxNPCs) { _pendCabeza = -1; return; }
                    NPC pieza = Main.npc[cur];
                    if (pieza == null || !pieza.active) { _pendCabeza = -1; return; }
                    int atras = (int)pieza.ai[0];
                    if (atras <= 0 || atras >= Main.maxNPCs || atras == cur) { cola = pieza; break; }
                    cur = atras;
                }
                if (cola == null || cola.type != NPCID.EaterofWorldsTail)
                {
                    // aun no: la cadena de vanilla sigue construyendose
                    if (++_pendIntentos >= 6) { _pendCabeza = -1; return; }
                    _pendTicks = 15;
                    return;
                }

                int vivos = ContarPiezas(NPCID.EaterofWorldsHead, NPCID.EaterofWorldsTail);
                int objetivo = Math.Min(NPC.GetEaterOfWorldsSegmentsCount() * 3 + 2, 192);
                if (vivos >= objetivo) { _pendCabeza = -1; return; }

                NPC ultimoCuerpo = (cola.ai[1] > 0 && cola.ai[1] < Main.maxNPCs)
                    ? Main.npc[(int)cola.ai[1]] : null;
                if (ultimoCuerpo == null || !ultimoCuerpo.active ||
                    ultimoCuerpo.type != NPCID.EaterofWorldsBody)
                { _pendCabeza = -1; return; }

                int hechos = 0;
                for (int i = 0; i < objetivo - vivos; i++)
                {
                    int idx = NPC.NewNPC(ultimoCuerpo.GetSource_FromAI(),
                        (int)ultimoCuerpo.Center.X, (int)ultimoCuerpo.Center.Y,
                        NPCID.EaterofWorldsBody, 0);
                    if (idx < 0 || idx >= Main.maxNPCs) break; // el mundo lleno: se queda como este
                    NPC nuevo = Main.npc[idx];
                    if (nuevo == null || !nuevo.active) break;

                    // EL ENLACE (la convencion de AI_006):
                    nuevo.ai[1] = ultimoCuerpo.whoAmI;  // el de adelante
                    nuevo.ai[0] = cola.whoAmI;          // mi atras es la cola
                    nuevo.ai[2] = 0f;                  // sin presupuesto: enlazado a mano
                    nuevo.target = cabeza.target;
                    ultimoCuerpo.ai[0] = nuevo.whoAmI; // el de atras del ultimo soy yo
                    cola.ai[1] = nuevo.whoAmI;         // el de adelante de la cola soy yo

                    nuevo.netUpdate = true;
                    ultimoCuerpo.netUpdate = true;
                    cola.netUpdate = true;
                    ultimoCuerpo = nuevo;
                    hechos++;
                }

                // el censo crece con la cadena (Marcar ya conto cada pieza
                // al nacer; el refresco final clava el numero).
                _inicialDevorador = Math.Max(_inicialDevorador, vivos + hechos);
                _pendCabeza = -1;
            }
            catch { _pendCabeza = -1; }
        }

        // === EL ESTADO DE LA OLEADA ESPECIAL ===
        private static int _spawneados = 0;         // cuántos guardián ya nacieron (la cuenta la valida el escaneo de sellos)
        private static int _ticksSuma = 0;          // tempo de las sumas

        // === LAS CONSTANTES DE LA CASA ===
        /// <summary>El evento completo dura 5 MINUTOS de oleadas (300 s).</summary>
        public const int TicksEvento = 18000;
        /// <summary>El jefe de la oleada tiene 90 s para caer antes de hundirse.</summary>
        public const int TicksJefeMax = 5400;
        /// <summary>La llamada dramática inicial (las dos voces).</summary>
        public const int TicksLlamada = 200;
        /// <summary>El respiro entre oleadas.</summary>
        private const int TicksInterludio = 90;
        /// <summary>Cada cuánto se suma un guardián más en LA ESPECIAL.</summary>
        private const int TicksSumaEspecial = 900; // 15 s

        /// <summary>¿El evento de las oleadas está corriendo ahora?</summary>
        public static bool Activo => _fase != Fase.Inactivo && _fase != Fase.Fin;
        /// <summary>La oleada actual (0 si no hay evento; 11 = la ESPECIAL).</summary>
        public static int OleadaActual => Activo ? _oleadaActual : 0;
        /// <summary>¿Está corriendo LA OLEADA ESPECIAL (El Juicio)?</summary>
        public static bool EspecialActiva => Activo && _fase == Fase.Especial;

        /// <summary>
        /// v6.50.59 — EL NIVEL DE FURIA DEL FESTÍN EN MARCHA (lo leen el
        /// sello OleadaNPC para escalar vida/daño/defensa/agresión y la
        /// mesa de biomas para la mezcla).
        /// </summary>
        public static int NivelFuriaPublico => _nivelFuria;

        // ==================================================================
        //  v6.50.29 — LOS MANDOS DEL MOTOR NATURAL (lo que OleadaNPC lee)
        //  ==================================================================

        /// <summary>
        /// ¿La CHUSMA está en marcha? (fase Monstruos — la ventana en la que
        /// el MOTOR NATURAL de vanilla escupe la horda: OleadaNPC.
        /// EditSpawnRate/EditSpawnPool aceleran y reemplazan el pool SOLO
        /// aquí, exactamente como las lunas de vanilla multiplican el motor
        /// mientras el evento vive).
        /// </summary>
        public static bool ChusmaEnMarcha => Activo && _fase == Fase.Monstruos && _poolActual != null;

        /// <summary>
        /// EL POOL PONDERADO de la oleada en marcha (los tipos que el motor
        /// natural puede escupir — el bioma del portador con PESO DOBLE +
        /// los acompañantes del nivel). null fuera de la fase de chusma.
        /// </summary>
        public static IReadOnlyDictionary<int, float> PoolDeOleada => ChusmaEnMarcha ? _poolActual : null;

        /// <summary>EL PORTADOR del festín (a quien la horda converge).</summary>
        public static Player Portador => (_jugador >= 0 && _jugador < Main.maxPlayers)
            ? Main.player[_jugador] : null;

        /// <summary>
        /// v6.50.59 — LOS PUNTOS REQUERIDOS por oleada — ×5 (la letra: «se
        /// necesita que la aparicion de mosntruo aumente al menos 5 veces
        /// mas por oleada al igual que el limite y la duracion» — TODO el
        /// sistema ×5): la 1 se paga con 90 muertes, la 10 con 360 — con el
        /// motor ×5 más denso la carnicería tarda lo SUYO (las oleadas son
        /// un castigo: hacerlas faciles no sirve de nada).
        /// </summary>
        public static int PuntosRequeridos(int k) => 60 + 30 * Math.Max(1, Math.Min(10, k));

        /// <summary>
        /// v6.50.59 — LA DENSIDAD DEL MOTOR ×5: spawnRate × (0.04 −
        /// 0.0012k) — la 1 ×0.039 (≈26× el motor natural ≈ CINCO VECES la
        /// furia de la .58), la 10 ×0.028 (≈36× vanilla): LA CALLE SE LLENA
        /// — la oleada se SIENTE como un castigo.
        /// </summary>
        public static float MultiplicadorSpawnRate(int k)
            => MathF.Max(0.028f, 0.040f - 0.0012f * Math.Max(1, Math.Min(10, k)));

        /// <summary>
        /// v6.50.59 — EL TOPE DE VIVOS ×5: 50+20k con el TECHO DEL MOTOR
        /// (170 — Main.maxNPCs es 200 y el mundo también tiene que vivir):
        /// la 1 sostiene 70, la 5 150, la 8+ el CIELO ENTERO.
        /// </summary>
        public static int TopeVivos(int k) => Math.Min(170, 50 + 20 * Math.Max(1, Math.Min(10, k)));

        /// <summary>
        /// v6.50.29 — EL PUNTO DE MUERTE (el corazón de la progresión de
        /// vanilla): cada chusma sellada que cae suma SU punto. Lo llama
        /// OleadaNPC.OnKill. Cruza el umbral → la oleada AVANZA (el Jefe
        /// nace); el clamp evita que el indicador pase del 100% (vanilla
        /// resetea waveKills al cruzar — el reset lo hace el cambio de fase).
        /// </summary>
        public static void PuntoDeMuerte(int puntos = 1)
        {
            if (!ChusmaEnMarcha || puntos <= 0) return;
            _puntosOleada = Math.Min(_puntosOleada + puntos, PuntosRequeridos(_oleadaActual));
        }

        // ==================================================================
        //  v6.50.2 — EL FESTÍN VISTO DESDE LOS CLIENTES (diagnóstico)
        // ==================================================================
        //
        // La máquina de fases corre SOLO en el server (PostUpdateWorld):
        // _fase/_oleadaActual/_oleadasTotales son CEROS eternos en un cliente
        // remoto — el panel F8 mostraba "silencio" durante todo el festín.
        // EcoRed.MsgHambre lleva (fase, oleada, total) al portador y llena
        // estos estáticos en la recepción (red de seguridad 600t + cada
        // cambio de fase del ciclo).

        /// <summary>La fase del festín vista por el CLIENTE (0 = Inactivo … 6 = Fin).</summary>
        public static int FaseCliente;
        /// <summary>La oleada actual vista por el CLIENTE (0 si no hay festín).</summary>
        public static int OleadaCliente;
        /// <summary>Las oleadas totales vistas por el CLIENTE.</summary>
        public static int TotalesCliente;
        // v6.50.29 — EL INDICADOR EN MP: los puntos y el umbral de la oleada
        /// <summary>Los PUNTOS de la oleada vista por el CLIENTE (para el indicador).</summary>
        public static int PuntosCliente;
        /// <summary>Los puntos REQUERIDOS de la oleada vista por el CLIENTE.</summary>
        public static int RequeridosCliente;

        /// <summary>La fase del festín en el SERVER (lo serializa EcoRed.SincronizarHambre).</summary>
        public static int FaseServidor => (int)_fase;
        /// <summary>La oleada actual en el SERVER (lo serializa EcoRed).</summary>
        public static int OleadaServidor => _oleadaActual;
        /// <summary>Las oleadas totales en el SERVER (lo serializa EcoRed).</summary>
        public static int TotalesServidor => _oleadasTotales;
        /// <summary>Los PUNTOS de la oleada en el SERVER (los serializa EcoRed para el indicador).</summary>
        public static int PuntosServidor => _puntosOleada;
        /// <summary>Los puntos REQUERIDOS de la oleada actual en el SERVER.</summary>
        public static int RequeridosServidor => PuntosRequeridos(_oleadaActual);

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
        /// momentos de hambre, 1..10; 11 = LA OLEADA ESPECIAL directa —
        /// la vía de prueba de la Carnada). La llama ShardPlayer cuando
        /// el libro cruza el umbral — y la Carnada del Grimorio para las
        /// pruebas (salta la config: el cebo es la herramienta de test).
        /// Corre en el servidor del mundo (SP = el mismo proceso).
        /// v6.50.59 — EL NIVEL: N es ahora EL NIVEL DE FURIA (la furia
        /// natural usa el nivel persistente del portador — ShardPlayer.
        /// FuriaNivel: la PRIMERA es SIEMPRE 1, vencer sube, morir
        /// congela): todas las criaturas del festín escalan con él.
        /// </summary>
        public static void Provocar(Player jugador, int oleadas)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return; // el servidor manda
            if (Activo) return;                                      // un festín a la vez
            if (jugador == null || !jugador.active) return;
            // v6.50.10 — FIX: la furia no se provoca sobre un CAÍDO —
            // Paso la cancelaría al primer tick con VenganzaPorMuerte
            // (el festín naciendo-muriendo mientras el portador
            // respawnea). La hambre ya no crece muerto (ShardPlayer);
            // esto cierra la puerta a la Carnada/otros llamadores.
            if (jugador.dead) return;

            _oleadasTotales = (int)MathHelper.Clamp(oleadas, 1, 11);
            _oleadaActual = 0;
            _jugador = jugador.whoAmI;
            // v6.50.59 — EL NIVEL DEL FESTÍN: las oleadas que vienen SON el
            // nivel (la especial 11 corre a nivel 10, el tope).
            _nivelFuria = _oleadasTotales >= 11 ? 10 : Math.Max(1, Math.Min(10, _oleadasTotales));
            _acompanantes = null;
            _firmaBioma = "";
            _fase = Fase.Llamada;
            _ticksFase = 0;
            // v6.50.2 — EL FESTÍN CAMINA (cada cambio de fase): el estado
            // viaja al portador dentro del MsgHambre para SU diagnóstico.
            EcoRed.SincronizarHambre(jugador); // no-op fuera del servidor

            // LAS DOS VOCES: primero la ira, después la llamada — LA VOZ
            // DEL LIBRO VA PRIMERO (prioridad: se cuela al frente).
            // v6.49: EcoRed las lleva SOLO al portador (en SP habla
            // directo; en MP viajan al cliente del portador).
            EcoRed.HablarAlPortador(jugador, "Mods.AethonMod.Eco.Furia.Ira",
                new Color(168, 96, 60), rugido: true, prioridad: true);
            EcoRed.HablarAlPortador(jugador, "Mods.AethonMod.Eco.Furia.Llamada",
                new Color(226, 64, 64), rugido: true, prioridad: true);

            // v6.50.59 — EL AVISO DEL NIVEL: cuando la furia es de nivel 2+
            // el MUNDO entero sabe lo que viene (N oleadas de escalada).
            if (_oleadasTotales <= 10 && _nivelFuria > 1)
                EcoRed.AnunciarMundo("Mods.AethonMod.Furia.NivelArranque",
                    new Color(178, 26, 38), _nivelFuria, _oleadasTotales);
        }

        // ==================================================================
        //  LA MÁQUINA (PostUpdateWorld: el reloj del mundo)
        // ==================================================================

        public override void PostUpdateWorld()
        {
            // v6.50.48 - LA EXTENSION DEL DEVORADOR corre siempre que haya
            // un pendiente (la fase puede cambiar debajo).
            try { ProcesarExtensionDevorador(); } catch { _pendCabeza = -1; }
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

            // v6.48 — LA MUERTE DEL PORTADOR: el festín se CANCELA y el
            // libro TOMA VENGANZA (varias líneas — la voz del libro
            // siempre con prioridad). Castigo puro de voz: el evento
            // muere, la venganza es la despedida.
            if (hambriento.dead)
            {
                VenganzaPorMuerte(hambriento);
                Terminar();
                return;
            }

            switch (_fase)
            {
                case Fase.Llamada:
                    if (_ticksFase >= TicksLlamada)
                    {
                        if (_oleadasTotales >= 11) ArrancarEspecial(hambriento);
                        else SiguienteOleada(hambriento);
                    }
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

                case Fase.Especial:
                    FaseEspecial(hambriento);
                    break;

                case Fase.Fin:
                    if (_ticksFase >= 90) Terminar();
                    break;
            }
        }

        // ==================================================================
        //  LAS OLEADAS
        // ==================================================================

        private static void SiguienteOleada(Player hambriento)
        {
            _oleadaActual++;
            if (_oleadaActual > _oleadasTotales)
            {
                // ¿LA OLEADA ESPECIAL? Solo tras un 10x10 COMPLETO (10
                // oleadas pedidas y las 10 servidas).
                if (_oleadasTotales == 10 && _oleadaActual == 11)
                {
                    ArrancarEspecial(hambriento);
                    return;
                }

                // EL FINAL: saciedad y perdón
                _fase = Fase.Fin;
                _ticksFase = 0;
                EcoRed.HablarAlPortador(hambriento, "Mods.AethonMod.Eco.Furia.Saciado",
                    new Color(245, 196, 81), rugido: false, prioridad: true);
                // la hambre del portador se perdona: el festín contó
                var sp = hambriento.GetModPlayer<Players.ShardPlayer>();
                sp?.PerdonarHambre(); // v6.50.2: PerdonarHambre ya viaja con el festín (MsgHambre)

                // v6.50.59 — LA FURIA CRECE (la letra del sistema de
                // niveles: «luego si el jugador logra vencer el nivel 1 de
                // esa oleada, la siguiente sera de nivel 2, o sea 2
                // oleadas…»): el portador VENCIÓ el festín COMPLETO de
                // nivel N — el grimorio RECONOCE su fuerza: su próxima
                // furia será de nivel N+1 (más oleadas, más fuerza, más
                // biomas en la mesa). El nivel vive en SU ShardPlayer
                // (persistente). Morir no sube nada: la próxima repite.
                try
                {
                    var spVencedor = hambriento.GetModPlayer<Players.ShardPlayer>();
                    if (spVencedor != null && _oleadasTotales <= 10 && _nivelFuria >= 1)
                    {
                        int proximoNivel = Math.Min(10, Math.Max(spVencedor.FuriaNivel, _nivelFuria + 1));
                        if (proximoNivel > spVencedor.FuriaNivel)
                        {
                            spVencedor.FuriaNivel = proximoNivel;
                            EcoRed.AnunciarMundo("Mods.AethonMod.Furia.NivelVencido",
                                new Color(245, 196, 81), hambriento.name, _nivelFuria, proximoNivel);
                        }
                    }
                }
                catch { }
                return;
            }

            _fase = Fase.Monstruos;
            _ticksFase = 0;
            _ticksOleada = 0;
            _puntosOleada = 0;      // v6.50.29 — el waveKills de la oleada nueva
            _bossIdx = -1;
            _tipoJefeOleada = -1;
            // v6.50.59 — EL POOL DE LA OLEADA, PERO DE VERDAD VIVO:
            // (a) LOS ACOMPAÑANTES del nivel se reparten AL ARRANCAR la
            // oleada (la mezcla varía de oleada en oleada — siempre de la
            // MESA DE BIOMAS DISPONIBLES del mundo en ESTE momento: el
            // Santuario solo si el Muro de Carne ya cayó), (b) el bioma
            // PRINCIPAL se fotografia (su firma) y se relee CADA MEDIO
            // SEGUNDO: si el portador SE MUDA de bioma, el pool se
            // reconstruye al vuelo (la oleada TE SIGVE — la letra: «deberia
            // adaptarse al momento en el que el jugador cambia de bioma»).
            _acompanantes = ElegirAcompanantes(_nivelFuria);
            _firmaBioma = FirmaBioma(hambriento);
            _poolActual = ConstruirPool(hambriento);

            // v6.50.2 — EL FESTÍN CAMINA: fase nueva al portador.
            EcoRed.SincronizarHambre(hambriento); // no-op fuera del servidor

            // EL ANUNCIO: la oleada k de N (la final se anuncia en negro y rojo)
            // v6.50.59 — CON EL NIVEL: la furia de nivel 2+ anuncia SU
            // nivel en cada oleada (el castigo con nombre).
            // v6.49 — EL FESTÍN ES DEL MUNDO: el anuncio viaja a TODOS
            // (ChatHelper en MP; el usuario lo pidió así — "el evento de
            // uno es el evento del mundo", la VOZ sigue siendo del
            // portador, el AVISO es del festín).
            int xpOleada = _oleadaActual + Math.Min(10, _nivelFuria);
            if (_oleadaActual == _oleadasTotales && _oleadasTotales >= 5)
                EcoRed.AnunciarMundo("Mods.AethonMod.Furia.OleadaFinal", new Color(178, 26, 38),
                    _oleadaActual, _oleadasTotales);
            else if (_nivelFuria > 1)
                EcoRed.AnunciarMundo("Mods.AethonMod.Furia.OleadaNivel",
                    new Color(198, 200, 206), _oleadaActual, _oleadasTotales, _nivelFuria, xpOleada);
            else
                EcoRed.AnunciarMundo("Mods.AethonMod.Furia.Oleada",
                    new Color(198, 200, 206), _oleadaActual, _oleadasTotales, xpOleada);

            // v6.50.29 — NADA MÁS QUE ESPERAR: la horda la trae EL MOTOR
            // NATURAL DE VANILLA (OleadaNPC.EditSpawnRate/EditSpawnPool
            // activados por ChusmaEnMarcha). El motor de vanilla es el que
            // mantiene el mundo lleno de enemigos desde 2011 — la furia
            // solo lo ACELERA (×0.2 menguante) y le SUSTITUYE el pool (el
            // bioma del portador). Los monstruos nacen en el anillo
            // 0.52–0.7× pantalla — JUSTO FUERA DEL CUADRO (más cerca que
            // nunca) — sobre suelo validado por el propio motor (jamás en
            // pared) y vienen CORRIENDO por el empuje del sello.
        }

        private static void FaseMonstruos(Player hambriento)
        {
            _ticksOleada++;

            // === v6.50.29 — LA PROGRESIÓN ES DE MUERTES (como las lunas de
            // vanilla): cada chusma sellada que cae suma su punto en
            // PuntoDeMuerte (OleadaNPC.OnKill). Al cruzar el umbral la
            // oleada SE PAGA y nace el guardián. El reloj de abajo es SOLO
            // la red de seguridad — el festín nunca se cuelga si el
            // portador no mata, pero el que MANDA es el machete.
            int requeridos = PuntosRequeridos(_oleadaActual);
            bool porMuertes = _puntosOleada >= requeridos;

            // LA RED DE SEGURIDAD — v6.50.59 ×5 (la letra: «al igual que el
            // limite y la duracion»): el TECHO del festín entero pasa de 5
            // min a 25 (TicksEvento×5) y el MÍNIMO por oleada de 10 s a 50
            // — la oleada DURA lo suyo (con 90-360 muertes que pagar, el
            // reloj casi nunca manda: manda el machete).
            int duracion = Math.Max(3000, TicksEvento * 5 / Math.Max(1, _oleadasTotales));
            bool porReloj = _ticksOleada >= duracion;

            // v6.50.59 — LA OLEADA TE SIGVE AL BIOMA (la letra: «la oleada
            // solo se adapta cuando inicia en ese bioma, pero deberia
            // adaptarse al momento en el que el jugador cambia de bioma»):
            // cada MEDIO SEGUNDO la FIRMA del bioma del portador se relee —
            // si CAMBIÓ (estabas en el bosque y te fuiste al corrupto), el
            // pool SE RECONSTRUYE al vuelo: el bioma PRINCIPAL pasa a ser
            // el NUEVO (peso doble) y los acompañantes del nivel siguen en
            // la mesa. Los que ya nacieron siguen siendo los que son — los
            // NUEVOS nacen del bioma donde ESTÁS (también atardecer/amanecer
            // y el hardmode cuentan: la firma los lleva).
            if ((_ticksOleada % 30) == 0)
            {
                string firmaAhora = FirmaBioma(hambriento);
                if (firmaAhora != _firmaBioma)
                {
                    _firmaBioma = firmaAhora;
                    _poolActual = ConstruirPool(hambriento);
                }
            }

            // EL LATIDO MP: cada 60 t el estado camina al portador (el
            // indicador de oleada del cliente remoto lee los PUNTOS — sin
            // esto la barra de MP quedaba congelada hasta el cambio de fase).
            if ((_ticksOleada % 60) == 0)
                EcoRed.SincronizarHambre(hambriento); // no-op fuera del servidor

            // ==============================================================
            //  v6.50.61 — EL VIGÍA DEL FESTÍN (la letra del usuario:
            //  «a veces cuando la activo en el spawn original está como
            //  que no trae a monstruo hasta que no salga del spawn»).
            //
            //  LA CAUSA (decompile de NPC.SpawnNPC contra tModLoader
            //  2026.08.3.0, verificado al IL): con UN solo townNPC cerca
            //  del jugador (Player.townNPCs >= 1) el motor natural pone
            //  su flag de bloqueo SIN CONDICIÓN — y con DOS o más ni
            //  siquiera tira el dado: CERO spawns naturales mientras
            //  el portador viva rodeado de gente. EL SPAWN ORIGINAL DEL
            //  MUNDO ES EL PUEBLO (la casa del Guía y los suyos): la
            //  furia aceleraba el motor todo lo que quisiera… y el
            //  motor ni arrancaba. Al salir del pueblo la zona se
            //  limpiaba y la comida volvía — el síntoma EXACTO de la
            //  letra.
            //
            //  LA CURA: el VIGÍA. Cada 15 t cuenta la chusma sellada
            //  VIVA; si tras la gracia de arranque (150 t) el festín
            //  está AYUNANDO (menos de 3 vivos), el vigía SIRVE la
            //  comida él mismo: nacimientos directos del pool de la
            //  oleada, en el ANILLO de siempre (fuera del cuadro), en
            //  la CUNA LIMPIA de siempre (PosicionLimpia — ni muros ni
            //  subsuelo) y sellados por OnSpawn como toda la chusma
            //  (stats del festín, aura, re-objetivado, XP y puntos).
            //  El motor natural SIGUE mandando donde puede trabajar;
            //  el vigía solo tapa el hambre que el pueblo apaga.
            // ==============================================================
            if ((_ticksOleada % 15) == 0 && _ticksOleada >= 150)
            {
                int vivos = 0;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC c = Main.npc[i];
                    if (c == null || !c.active || c.townNPC) continue;
                    var sello = c.GetGlobalNPC<Globals.OleadaNPC>();
                    if (sello != null && sello.EsDeOleada && !sello.EsJefeDeOleada)
                        vivos++;
                }
                if (vivos < 3)
                    NacerChusmaVigia(hambriento, Math.Min(3, 3 - vivos));
            }

            if (porMuertes || porReloj)
            {
                _poolActual = null;  // el motor natural vuelve a lo suyo YA
                _fase = Fase.Jefe;
                _ticksFase = 0;
                _bossIdx = -1;
                SpawnJefeOleada(hambriento);
                // v6.50.2 — EL FESTÍN CAMINA: fase nueva al portador.
                EcoRed.SincronizarHambre(hambriento); // no-op fuera del servidor
            }
        }

        /// <summary>
        /// v6.50.61 — LA COMIDA DEL VIGÍA: nace `cuantos` monstruos del
        /// pool de la oleada en el ANILLO alrededor del portador (el
        /// mismo espíritu del motor natural: fuera del cuadro), cada
        /// uno en su CUNA LIMPIA (OleadaNPC.PosicionLimpia: aire 3×3,
        /// sin muros en superficie, jamás bajo el suelo del portador).
        /// El sello lo pone OnSpawn al nacer (ChusmaEnMarcha vive):
        /// stats ×(k+1)·nivel, aura del festín, re-objetivado al
        /// portador, XP y puntos al caer — exactamente la chusma de
        /// siempre. Solo la autoridad (server/SP).
        /// </summary>
        private static void NacerChusmaVigia(Player hambriento, int cuantos)
        {
            try
            {
                if (Main.netMode == NetmodeID.MultiplayerClient) return; // el servidor manda
                var pool = _poolActual;
                if (pool == null || pool.Count == 0 || hambriento == null) return;

                // EL PLATILLO PONDERADO del menú de la oleada (el mismo
                // reparto que EditSpawnPool sirve al motor natural).
                float total = 0f;
                foreach (var par in pool) total += par.Value;
                if (total <= 0f) return;

                for (int n = 0; n < cuantos; n++)
                {
                    // EL TIPO: la ruleta ponderada del festín.
                    float dado = Main.rand.NextFloat(total);
                    int tipo = 0;
                    foreach (var par in pool)
                    {
                        dado -= par.Value;
                        if (dado <= 0f) { tipo = par.Key; break; }
                    }
                    if (tipo <= 0) continue;

                    // LA CUNA: el anillo de siempre (fuera del cuadro, el
                    // mismo 0.52–0.7× pantalla que el motor natural usa —
                    // aquí fijo en 640–960 px del portador) y la LIMPIEZA
                    // de la casa encima (la red final de PosicionLimpia es
                    // el aire de la presa: la comida SIEMPRE nace).
                    float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                    float dist = Main.rand.NextFloat(640f, 960f);
                    Vector2 cruda = hambriento.Center + new Vector2(
                        MathF.Cos(ang), MathF.Sin(ang) * 0.55f) * dist;
                    cruda.Y = Math.Clamp(cruda.Y, Main.topWorld + 320f, Main.bottomWorld - 320f);
                    Vector2 pos = Globals.OleadaNPC.PosicionLimpia(null, hambriento, cruda);

                    // NACE: OnSpawn lo sella como chusma del festín al
                    // volver de NewNPC (la fuente del jugador NO es padre
                    // NPC: la herencia no aplica, el sello de la furia sí).
                    int idx = NPC.NewNPC(hambriento.GetSource_FromAI(),
                        (int)pos.X, (int)pos.Y, tipo);
                    if (idx >= 0 && idx < Main.maxNPCs && Main.npc[idx].active)
                        Main.npc[idx].netUpdate = true; // MP: la comida camina
                }
            }
            catch { }
        }

        private static void FaseJefe(Player hambriento)
        {
            NPC jefe = (_bossIdx >= 0 && _bossIdx < Main.maxNPCs) ? Main.npc[_bossIdx] : null;

            // v6.50.1 — FIX (EL SLOT HUÉRFANO + LA HUIDA QUE ERA VICTORIA):
            // (a) _bossIdx es un whoAmI reciclable — si el jefe moría y el
            // slot se re-llenaba con otro NPC, la furia miraba al EXTRAÑO
            // hasta el timeout (o lo "mataba" por él): el sello de
            // OleadaNPC valida que el slot siga siendo del festín.
            // (b) un jefe que se DESPAWNEA solo (amanecer, distancia) no
            // debe entregar el DerrotaOleada10: solo la MUERTE (life ≤ 0,
            // confirmada por el sello) paga. La instancia muerta conserva
            // el sello (solo NewNPC lo resetea) y el reciclado lo pierde.
            var sello = jefe != null ? jefe.GetGlobalNPC<Globals.OleadaNPC>() : null;
            // v6.50.2 — FIX (carrera del slot reciclado en FaseJefe): además
            // del sello, la ESPECIE — el type del jefe se fija al spawnear
            // (_tipoJefeOleada). Un town NPC que recicle el slot del jefe
            // muerto en el MISMO tick (UpdateTime corre ANTES de
            // PostUpdateWorld) queda fuera del festín por AMBAS puertas: no
            // roba el crédito de DerrotaOleada10 ni sufre el timeout como
            // si fuera el guardián.
            bool esDelFestin = sello != null && sello.EsJefeDeOleada &&
                _tipoJefeOleada > 0 && jefe.type == _tipoJefeOleada;

            // EL JEFE CALLÓ (vida ≤ 0 con sello) → la oleada se completa
            if (esDelFestin && jefe.life <= 0)
            {
                // v6.48 — LA OLEADA 10 VENCIDA: el portador gana el
                // DERECHO A LAS ESENCIAS (la marca persiste en SU
                // ShardPlayer — dato de la crónica del festín). v6.50.61 —
                // LA PURGA: la TIENDA murió con el Testigo (el alma de
                // los guardianes cae de sus cadáveres, como manda el
                // festín); la marca queda como constancia silenciosa
                // (viaja con EcoRed.MsgCronica, los guardados viejos la
                // conservan).
                if (_oleadaActual >= 10 && _oleadasTotales >= 10)
                {
                    var sp = hambriento.GetModPlayer<Players.ShardPlayer>();
                    if (sp != null && !sp.DerrotaOleada10)
                    {
                        sp.DerrotaOleada10 = true;
                        EcoRed.SincronizarCronica(hambriento);
                    }
                }
                _fase = Fase.Interludio;
                _ticksFase = 0;
                // v6.50.2 — EL FESTÍN CAMINA: fase nueva al portador.
                EcoRed.SincronizarHambre(hambriento); // no-op fuera del servidor
                return;
            }

            // EL JEFE YA NO ESTÁ (slot reciclado, nunca nació o se fue
            // solo) → la furia sigue SIN crédito de victoria.
            if (!esDelFestin || !jefe.active)
            {
                _fase = Fase.Interludio;
                _ticksFase = 0;
                // v6.50.2 — EL FESTÍN CAMINA: fase nueva al portador.
                EcoRed.SincronizarHambre(hambriento); // no-op fuera del servidor
                return;
            }

            // EL JEFE SE CANSÓ: se hunde insatisfecho (90 s) y la furia
            // sigue su curso — 10 jefes vivos a la vez no es un evento,
            // es un dilema de render.
            if (_ticksFase >= TicksJefeMax)
            {
                EcoRed.AnunciarMundo("Mods.AethonMod.Furia.JefeHuido",
                    new Color(150, 140, 148), jefe.FullName);
                jefe.active = false; // despawn limpio (apagado directo de la autoridad)
                // v6.50.1 — FIX (JEFE FANTASMA): este despawn corre en
                // PostUpdateWorld (FUERA de la AI del NPC — vanilla difunde
                // el 23 desde UpdateNetworkCode solo si el apagado pasa
                // DENTRO de su update). Sin el 23 explícito, los clientes
                // conservaban un jefe congelado/invulnerable hasta que el
                // slot se reciclaba.
                if (Main.netMode == NetmodeID.Server)
                    Terraria.NetMessage.SendData(23, -1, -1, null, jefe.whoAmI);
                _fase = Fase.Interludio;
                _ticksFase = 0;
                // v6.50.2 — EL FESTÍN CAMINA: fase nueva al portador.
                EcoRed.SincronizarHambre(hambriento); // no-op fuera del servidor
            }
        }

        // ==================================================================
        //  LA OLEADA ESPECIAL — EL JUICIO (todos los guardianes a ×15)
        // ==================================================================

        /// <summary>
        /// LOS SEIS GUARDIANES de las oleadas (el reparto de zonas sin el
        /// invierno caminante) — TODOS juntos en la ESPECIAL, cada uno
        /// vestido de ×15 y con el aura del JUICIO.
        /// v6.50.30 — DEERCLOPS FUERA (la letra del usuario: «quita al
        /// Derrclops como jefe probable»): el Juicio son SEIS. Su esencia
        /// sigue siendo de los guardianes (la rareza la hace CARA) y su
        /// único camino es EL DADO del 1%.
        /// </summary>
        private static int[] LosSeis()
        {
            return new int[]
            {
                NPCID.KingSlime,
                NPCID.EyeofCthulhu,
                NPCID.QueenBee,
                NPCID.EaterofWorldsHead,
                NPCID.BrainofCthulhu,
                NPCID.SkeletronHead,
            };
        }

        /// <summary>
        /// v6.50.30 — EL NÚMERO DE GUARDIANES DEL JUICIO (los que nacen
        /// en la ESPECIAL y los que cuenta el indicador).
        /// </summary>
        private const int GuardianesJuicio = 6;

        /// <summary>
        /// ARRANCA EL JUICIO: la voz del libro anuncia el festín final y
        /// los guardianes empiezan a nacer — DOS EN PANTALLA desde el
        /// primer segundo (la letra del usuario) y el resto se suma cada
        /// 15 s hasta los seis (v6.50.30: el Juicio sin Deerclops).
        /// </summary>
        private static void ArrancarEspecial(Player hambriento)
        {
            _fase = Fase.Especial;
            _ticksFase = 0;
            _oleadaActual = 11;
            _ticksSuma = 0;
            _spawneados = 0;
            // v6.50.2 — EL FESTÍN CAMINA: el Juicio empieza — fase nueva al
            // portador (la oleada 11 también viaja).
            EcoRed.SincronizarHambre(hambriento); // no-op fuera del servidor

            // LA VOZ DEL JUICIO (prioridad: el libro manda — solo al
            // portador: es SU grimorio quien juzga).
            EcoRed.HablarAlPortador(hambriento, "Mods.AethonMod.Eco.Furia.Juicio",
                new Color(178, 26, 38), rugido: true, prioridad: true);
            EcoRed.AnunciarMundo("Mods.AethonMod.Furia.OleadaEspecial",
                new Color(178, 26, 38));

            // v6.50.18 — FUERA DE PANTALLA TAMBIÉN (antes "LOS DOS PRIMEROS:
            // en pantalla YA" — el reporte del usuario no distingue: NINGÚN
            // monstruo del festín nace a la vista; los guardianes llegan
            // desde fuera del cuadro como el resto del Juicio).
            for (int i = 0; i < 2; i++) NacerGuardian(hambriento);
        }

        /// <summary>Nace UN guardián de la ESPECIAL en el BORDE del cuadro del portador.</summary>
        private static void NacerGuardian(Player hambriento)
        {
            try
            {
                if (_spawneados >= GuardianesJuicio) return;
                int tipo = LosSeis()[_spawneados];

                // v6.50.29 — LA CUNA DEL GUARDIÁN: el BORDE DEL CUADRO (el
                // patrón de las invasiones de vanilla) — alternando lados y
                // SIN validación de caja: son JEFES (Rey Gelatina se
                // teletransporta, el Ojo y la Reina vuelan, Deerclops cae
                // y camina…) — la validación de tiles de la cuna vieja era
                // la que podía TRAGARSE al guardián (sin hueco libre en 8
                // intentos, el Juicio se quedaba SIN guardianes: "activo
                // la oleada y no veo NINGUNO"). NPC.sWidth es el tamaño
                // de pantalla SERVER-SEGURO de vanilla (1920 por defecto
                // en dedicados — el mismo que usa el motor de spawn).
                Vector2 pos = PosicionBordeJefe(hambriento, _spawneados % 2 == 0 ? -1 : 1);

                int idx = NPC.NewNPC(hambriento.GetSource_FromAI(), (int)pos.X, (int)pos.Y, tipo);
                NPC jefe = (idx >= 0 && idx < Main.maxNPCs) ? Main.npc[idx] : null;
                if (jefe == null || !jefe.active) return;

                // EL SELLO DEL JUICIO: ×15 en TODO, aura del JUICIO.
                jefe.GetGlobalNPC<OleadaNPC>().Marcar(jefe, 11, jefe: true, especial: true);
                jefe.netUpdate = true;

                _spawneados++;

                EcoRed.AnunciarMundo("Mods.AethonMod.Furia.Jefe",
                    new Color(226, 64, 64), jefe.FullName, 15);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, jefe.Center);
            }
            catch { }
        }

        /// <summary>
        /// EL JUICIO EN MARCHA: cada 15 s nace OTRO guardián hasta los
        /// SEIS (siempre ≥ 2 en pantalla mientras vivan). El festín
        /// termina cuando cae el ÚLTIMO — entonces la saciedad especial
        /// y el perdón de la hambre.
        /// </summary>
        private static void FaseEspecial(Player hambriento)
        {
            // LA SUMA: otro guardián cada 15 s (hasta 6).
            if (_spawneados < GuardianesJuicio)
            {
                _ticksSuma++;
                if (_ticksSuma >= TicksSumaEspecial)
                {
                    _ticksSuma = 0;
                    NacerGuardian(hambriento);
                }
            }

            // ¿VIVE ALGUNO? (los índices pueden reciclarse: valida tipo+marca)
            // v6.50.2 — FIX (el Juicio se cerraba con el Devorador vivo):
            // EoW (tipos 13/14/15) NO pone npc.boss=true en vanilla (lo
            // trackean por tipo) → quedaba fuera del conteo y el Juicio
            // terminaba "saciado" con el guardián ×15 aún en el mundo. El
            // sello (EsDeOleada && EsEspecial && EsJefeDeOleada) ya valida
            // de sobra — el filtro boss sobraba y mentía.
            int vivos = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active) continue;
                var sello = n.GetGlobalNPC<OleadaNPC>();
                if (sello != null && sello.EsDeOleada && sello.EsEspecial && sello.EsJefeDeOleada)
                    vivos++;
            }

            if (_spawneados >= GuardianesJuicio && vivos == 0)
            {
                // EL FINAL DEL JUICIO: la saciedad especial (la variante la
                // reparte la autoridad y viaja por EcoRed).
                _fase = Fase.Fin;
                _ticksFase = 0;
                EcoRed.HablarVarianteAlPortador(hambriento,
                    "Mods.AethonMod.Eco.Furia.JuicioFin", 3,
                    new Color(245, 196, 81), rugido: false, prioridad: true);
                var sp = hambriento.GetModPlayer<Players.ShardPlayer>();
                sp?.PerdonarHambre();
            }
        }

        // ==================================================================
        //  LA VENGANZA POR MUERTE (el castigo de voz del libro)
        // ==================================================================

        /// <summary>
        /// v6.48 — EL PORTADOR MURIÓ durante el festín: el evento SE
        /// TERMINA y el libro toma venganza — no de daño: de PALABRA
        /// (4 muestras para no repetir). La voz del libro con PRIORIDAD:
        /// las voces de jefes que esperaban en la cola salen DESPUÉS.
        /// </summary>
        private static void VenganzaPorMuerte(Player hambriento)
        {
            try
            {
                // LA VENGANZA ES DEL LIBRO — la oye SOLO el portador muerto
                // (prioridad: las voces de jefes esperan detrás).
                EcoRed.HablarVarianteAlPortador(hambriento,
                    "Mods.AethonMod.Eco.Furia.Venganza", 4,
                    new Color(178, 26, 38), rugido: true, escala: 0.62f, prioridad: true);
                EcoRed.AnunciarMundo("Mods.AethonMod.Furia.MuertePortador",
                    new Color(150, 140, 148));
            }
            catch { }
        }

        // ==================================================================
        //  LOS POOLS — el bioma decide la comida (la hora ya no manda)
        //  v6.50.59 — LA MESA COMPLETA: bioma principal (peso doble, EN
        //  TIEMPO REAL) + los acompañantes del nivel (la mezcla)
        // ==================================================================

        /// <summary>
        /// LA FIRMA DEL BIOMA del portador AHORA (todas las zonas + la hora
        /// + la era del mundo): se fotografía al arrancar la oleada y se
        /// relee cada medio segundo — si CAMBIÓ, el pool se reconstruye
        /// (la oleada sigue al portador a su bioma nuevo, y la superficie
        /// cambia de turno día/noche en vivo).
        /// </summary>
        private static string FirmaBioma(Player p)
        {
            return
                (p.ZoneUnderworldHeight ? "I" : "") + (p.ZoneDungeon ? "D" : "") +
                (p.ZoneGranite ? "G" : "") + (p.ZoneMarble ? "M" : "") +
                (p.ZoneSnow ? "N" : "") + (p.ZoneJungle ? "J" : "") +
                (p.ZoneCorrupt ? "C" : "") + (p.ZoneCrimson ? "K" : "") +
                (p.ZoneHallow ? "H" : "") + (p.ZoneDesert ? "S" : "") +
                (p.ZoneBeach ? "P" : "") + (p.ZoneSkyHeight ? "Y" : "") +
                (p.ZoneMeteor ? "T" : "") +
                (p.ZoneRockLayerHeight || p.ZoneDirtLayerHeight ? "U" : "") +
                (Main.hardMode ? "!" : "") + (Main.dayTime ? "@" : "");
        }

        /// <summary>
        /// LA MESA DE BIOMAS DISPONIBLES del mundo — los acompañantes que
        /// el nivel puede invitar a la mezcla. v6.50.59 (la letra: «a mayor
        /// nivel de oleada mas monstruos de biomas diferentes aparecen,
        /// siempre usando los biomas disponibles en el momento, por ejemplo
        /// cuando se mata al muro de carne los nuevos biomas son añadidos
        /// a las nuevas oleadas a partir de ese momento, pero antes no»):
        /// · PRE-HARDMODE: los biomas de siempre (nieve, jungla, desierto,
        ///   playa, cielo, subsuelo, granito, mármol y EL MAL DEL MUNDO —
        ///   solo el que EXISTE: corrupción O carmesí).
        /// · TRAS EL MURO DE CARNE: EL SANTUARIO SE UNE (el Hallow NACE
        ///   con el hardmode — antes NO EXISTE en la mesa: pixies y
        ///   unicornios solo desde que la carne cayó).
        /// (El infierno, la mazmorra y el meteorito son biomas PRINCIPALES
        /// — donde ESTÁS tú manda; como acompañantes sus criaturas no
        /// cruzan bien el cielo abierto.)
        /// </summary>
        private static List<int[]> MesaDeBiomas()
        {
            var mesa = new List<int[]>();
            mesa.Add(new int[] { NPCID.IceSlime, NPCID.SnowFlinx, NPCID.ZombieEskimo });     // la nieve
            mesa.Add(new int[] { NPCID.JungleSlime, NPCID.JungleBat, NPCID.Hornet });        // la jungla
            mesa.Add(new int[] { NPCID.SandSlime, NPCID.Antlion, NPCID.Antlion });          // el desierto
            mesa.Add(new int[] { NPCID.Crab, NPCID.BlueSlime, NPCID.Crab });                 // la playa
            mesa.Add(new int[] { NPCID.Harpy, NPCID.Harpy, NPCID.BlueSlime });               // el cielo
            mesa.Add(new int[] { NPCID.CaveBat, NPCID.Skeleton, NPCID.BlueSlime });         // el subsuelo
            mesa.Add(new int[] { NPCID.GraniteGolem, NPCID.GraniteFlyer });                  // el granito
            mesa.Add(new int[] { NPCID.GreekSkeleton, NPCID.Medusa, NPCID.GreekSkeleton });// el mármol
            // EL MAL DEL MUNDO — SOLO el bioma que EXISTE (el otro no está
            // disponible: «siempre usando los biomas disponibles en el
            // momento»).
            mesa.Add(WorldGen.crimson
                ? new int[] { NPCID.Crimera, NPCID.FaceMonster, NPCID.BloodCrawler }
                : new int[] { NPCID.EaterofSouls, NPCID.EaterofSouls, NPCID.CorruptSlime });
            // v6.50.59 — TRAS EL MURO DE CARNE: EL SANTUARIO EN LA MESA (el
            // Hallow nace con el hardmode — ANTES no existe para el libro).
            if (Main.hardMode)
                mesa.Add(new int[] { NPCID.Pixie, NPCID.Pixie, NPCID.Unicorn });
            return mesa;
        }

        /// <summary>
        /// LOS ACOMPAÑANTES DE LA OLEADA: (nivel−1) biomas extra SIN
        /// repetir, elegidos al azar de la MESA DISPONIBLE — nivel 1 = SOLO
        /// tu bioma (la furia de aprendizaje), nivel 5 = TU bioma + 4 más,
        /// nivel 10 = LA CIUDAD ENTERA a la vez.
        /// </summary>
        private static List<int[]> ElegirAcompanantes(int nivel)
        {
            var elegidos = new List<int[]>();
            if (nivel <= 1) return elegidos;
            var mesa = MesaDeBiomas();
            int cuantos = Math.Min(nivel - 1, mesa.Count);
            for (int i = 0; i < cuantos; i++)
            {
                int idx = Main.rand.Next(mesa.Count);
                elegidos.Add(mesa[idx]);
                mesa.RemoveAt(idx);
            }
            return elegidos;
        }

        /// <summary>
        /// EL POOL PONDERADO DE LA OLEADA: el bioma PRINCIPAL del portador
        /// (peso 2.0 — la comida local manda) + los ACOMPAÑANTES del nivel
        /// (peso 1.0 — la mezcla que crece con el nivel).
        /// </summary>
        private static Dictionary<int, float> ConstruirPool(Player p)
        {
            var dic = new Dictionary<int, float>();
            foreach (int id in BiomaPrincipal(p))
                if (id > 0) dic[id] = 2.0f;
            if (_acompanantes != null)
                foreach (int[] grupo in _acompanantes)
                    foreach (int id in grupo)
                        if (id > 0 && !dic.ContainsKey(id))
                            dic[id] = 1.0f;
            return dic;
        }

        /// <summary>
        /// Los monstruos que el bioma del portador ofrece (EL PRINCIPAL —
        /// donde ESTÁS tú manda). IDs verificados contra el Terraria real
        /// (sondeo de reflexión). v6.50.59: + EL SANTUARIO (ZoneHallow) y
        /// + EL METEORITO (ZoneMeteor) como principales propios.
        /// </summary>
        private static int[] BiomaPrincipal(Player p)
        {
            // === INFIERNO ===
            if (p.ZoneUnderworldHeight)
                return new int[] { NPCID.Demon, NPCID.FireImp, NPCID.LavaSlime };

            // === MAZMORRA ===
            if (p.ZoneDungeon)
                return new int[] { NPCID.AngryBones, NPCID.DungeonSlime, NPCID.BlazingWheel };

            // === CAVERNAS DE GRANITO/MÁRMOL ===
            if (p.ZoneGranite)
                return new int[] { NPCID.GraniteGolem, NPCID.GraniteFlyer };
            if (p.ZoneMarble)
                return new int[] { NPCID.GreekSkeleton, NPCID.Medusa, NPCID.GreekSkeleton };

            // === METEORITO (v6.50.59 — el cielo caído también alimenta) ===
            if (p.ZoneMeteor)
                return new int[] { NPCID.MeteorHead, NPCID.MeteorHead, NPCID.BlueSlime };

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

            // === SANTUARIO (v6.50.59 — el Hallow: pixies y unicornios) ===
            if (p.ZoneHallow)
                return new int[] { NPCID.Pixie, NPCID.Unicorn, NPCID.Pixie };

            // === DESIERTO ===
            if (p.ZoneDesert)
                return new int[] { NPCID.SandSlime, NPCID.Antlion, NPCID.Antlion };

            // === PLAYA ===
            if (p.ZoneBeach)
                return new int[] { NPCID.Crab, NPCID.BlueSlime, NPCID.Crab };

            // === CIELO ===
            if (p.ZoneSkyHeight)
                return new int[] { NPCID.Harpy, NPCID.Harpy, NPCID.BlueSlime };

            // === SUBSUELO (roca o tierra, sin bioma especial) ===
            if (p.ZoneRockLayerHeight || p.ZoneDirtLayerHeight)
                return new int[] { NPCID.CaveBat, NPCID.Skeleton, NPCID.BlueSlime };

            // === SUPERFICIE (v6.50.25 — CONCIENTE DE LA HORA Y DEL MUNDO,
            //     el reporte: «las oleadas del grimorio no se activan por
            //     el dia, recuerda que las oleadas deben tomar los
            //     monstruos de la zona y usarlos»: el OJO DEMONÍCO HUYE del
            //     sol recién nacido (su IA de vanilla lo despega del suelo
            //     y lo manda a des-spawnear) — la mitad de la chusma de
            //     superficie EVAPORABA al nacer de día y la oleada leía
            //     como «no se activa». DE DÍA la zona manda LIMOS (los que
            //     de verdad viven en la superficie al sol); de noche, el
            //     repertorio clásico; en hardmode, los de la zona
            //     hardmode) ===
            if (Main.hardMode)
            {
                if (Main.dayTime)
                    return new int[] { NPCID.BlueSlime, NPCID.GreenSlime, NPCID.Derpling, NPCID.BlueSlime };
                return new int[] { NPCID.Werewolf, NPCID.WanderingEye, NPCID.Zombie };
            }
            if (Main.dayTime)
                return new int[] { NPCID.GreenSlime, NPCID.BlueSlime, NPCID.PurpleSlime };
            return new int[] { NPCID.Zombie, NPCID.DemonEye, NPCID.GreenSlime, NPCID.BlueSlime };
        }

        /// <summary>
        /// El JEFE PRE-HARDMODE que cierra la oleada según el bioma.
        ///
        /// v6.48 — LA REGLA NUEVA DEL USUARIO: los jefes de oleada NO se
        /// ven afectados por la HORA — cada ZONA tiene su guardián fijo
        /// (aparecen en su zona predeterminada), y las zonas que no
        /// tenían guardián YA TIENEN UNO:
        ///   · superficie → alterna Rey Gelatina / Ojo por PARIDAD de
        ///     oleada (variedad sin hora: la 1 Rey, la 2 Ojo, la 3 Rey…)
        ///   · desierto → REY GELATINA (la corona de la arena)
        ///   · playa y cielo → OJO DE CTHULHU (el vigía que vuela)
        ///   · granito/mármol/subsuelo → el MAL DEL MUNDO (Devorador o
        ///     Cerebro según el mundo)
        ///   · nieve → Skeletron (el hueso congelado) · jungla → Abeja
        ///     Reina · corrupción → Devorador · carmesí → Cerebro ·
        ///     mazmorra → Skeletron · infierno → el Ojo los caza.
        /// v6.50.30 — DEERCLOPS YA NO ES GUARDIÁN PROBABLE (la letra del
        ///     usuario: «quita al Derrclops como jefe probable, has que
        ///     sea un jefe que salga con una probalidad de 1% en
        ///     oleadas»): salió del reparto de zonas y del Juicio — el
        ///     invierno caminante AHORA solo llega por EL DADO: 1% por
        ///     oleada (NacerJefeRaro, anunciado aparte).
        /// El Muro de Carne sigue EXCLUIDO a propósito (una furia
        /// involuntaria no abre el hardmode).
        /// </summary>
        private static int JefeDelLugar(Player p)
        {
            // v6.50.25 — LOS JEFES NO VEN LA HORA (el reporte del usuario:
            // «en los jefes estos no se pueden ver afectados por el dia»).
            // EL OJO DE CTHULHU HUYE DEL SOL (su IA de vanilla lo manda a
            // despegar en cuanto amanece y se des-spawnea): DE DÍA el
            // guardián es uno que NO duerme — donde el ojo no puede, la
            // CORONA manda. v6.50.30 — DEERCLOPS FUERA DEL REPARTO (la
            // letra del usuario): la nieve la guarda ahora el HUESO
            // CONGELADO (Skeletron: no duerme, no huye del sol) y la
            // paridad de superficie de día pasa a REY/SKELETRON. El
            // invierno caminante SOLO llega por EL DADO: 1% por oleada
            // (NacerJefeRaro).
            bool deDia = Main.dayTime;

            if (p.ZoneUnderworldHeight)
                return deDia ? NPCID.KingSlime : NPCID.EyeofCthulhu; // el ojo los caza en el infierno; de día la corona reina en el fuego
            if (p.ZoneDungeon) return NPCID.SkeletronHead;          // v6.48: SIN hora — el guardián no duerme
            if (p.ZoneSnow) return NPCID.SkeletronHead;             // v6.50.30: el hueso congelado — la nieve no duerme
            if (p.ZoneJungle) return NPCID.QueenBee;                 // la colmena no duerme
            if (p.ZoneCorrupt) return NPCID.EaterofWorldsHead;
            if (p.ZoneCrimson) return NPCID.BrainofCthulhu;

            // DESIERTO (v6.48 — zona SIN guardián, ahora con el Rey).
            if (p.ZoneDesert) return NPCID.KingSlime;

            // PLAYA y CIELO (v6.48 — zonas sin guardián, ahora con el Ojo
            // — de día, el Rey: el vigía no puede volar bajo el sol).
            if (p.ZoneBeach) return deDia ? NPCID.KingSlime : NPCID.EyeofCthulhu;
            if (p.ZoneSkyHeight) return deDia ? NPCID.KingSlime : NPCID.EyeofCthulhu;

            // SUBSUELO sin bioma: el mal del mundo (o el Rey, en mundos limpios)
            if (p.ZoneRockLayerHeight || p.ZoneDirtLayerHeight)
                return WorldGen.crimson ? NPCID.BrainofCthulhu : NPCID.EaterofWorldsHead;

            // SUPERFICIE: por PARIDAD de oleada (sin hora — la 1 Rey, la 2 Ojo…
            // de día la paridad sigue VIVA: Rey/Skeletron, los que no huyen).
            if (deDia)
                return (_oleadaActual % 2 == 1) ? NPCID.KingSlime : NPCID.SkeletronHead;
            return (_oleadaActual % 2 == 1) ? NPCID.KingSlime : NPCID.EyeofCthulhu;
        }

        // ==================================================================
        //  LOS SPAWNS DE LOS GUARDIANES (los jefes NO salen del motor
        //  natural — vanilla tampoco: los jefes de evento se convocan)
        //  ==================================================================

        /// <summary>
        /// v6.50.29 — EL BORDE DEL CUADRO para un guardián: x = medio
        /// NPC.sWidth + margen (la medida SERVER-SEGURA de pantalla que el
        /// propio motor de spawn de vanilla usa — Main.screenPosition NO
        /// existe en un servidor dedicado), y = algo sobre la línea de la
        /// presa (los voladores aprovechan, los caminantes caen). Sin
        /// validación de tiles — los JEFES resuelven el terreno solos (el
        /// Rey se teletransporta, el Ojo vuela, Deerclops cae): la caja
        /// libre de la cuna vieja podía TRAGARSE al guardián entero.
        /// Bordes del mundo respetados.
        /// </summary>
        private static Vector2 PosicionBordeJefe(Player p, int lado)
        {
            float dx = NPC.sWidth * 0.5f + 140f;
            float x = p.Center.X + (lado < 0 ? -dx : dx);
            float y = p.Center.Y - 240f + Main.rand.NextFloat(-80f, 80f);
            x = Math.Clamp(x, Main.leftWorld + 400f, Main.rightWorld - 400f);
            y = Math.Clamp(y, Main.topWorld + 400f, Main.bottomWorld - 400f);
            return new Vector2(x, y);
        }

        /// <summary>Convoca al JEFE (versión especial de la oleada) del bioma.</summary>
        private static void SpawnJefeOleada(Player hambriento)
        {
            try
            {
                int tipo = JefeDelLugar(hambriento);
                // v6.50.29 — EL BORDE DEL CUADRO (lado trasero preferido):
                // el jefe llega CAMINANDO/VOLANDO al cuadro como las
                // invasiones de vanilla — visible en segundos, sin la
                // caja-libre que podía tragárselo. Sin hueco no hay
                // problema: los jefes resuelven el terreno.
                int lado = (Main.rand.Next(4) == 0) ? 1 : (hambriento.direction >= 0 ? -1 : 1);
                Vector2 pos = PosicionBordeJefe(hambriento, lado);

                int idx = NPC.NewNPC(hambriento.GetSource_FromAI(), (int)pos.X, (int)pos.Y, tipo);
                NPC jefe = (idx >= 0 && idx < Main.maxNPCs) ? Main.npc[idx] : null;
                if (jefe == null || !jefe.active) return;
                _bossIdx = idx;
                // v6.50.2 — FIX (carrera del slot reciclado): la ESPECIE del
                // jefe queda registrada al nacer — FaseJefe la valida junto
                // al sello (un slot reciclado por un town NPC del mismo tick
                // ya no pasa por jefe del festín).
                _tipoJefeOleada = tipo;

                jefe.GetGlobalNPC<OleadaNPC>().Marcar(jefe, _oleadaActual, jefe: true, nivel: _nivelFuria);
                jefe.netUpdate = true;

                // v6.50.48 - LA EXTENSION DEL DEVORADOR: la cabeza acaba
                // de nacer; cuando su cadena de vanilla este completa
                // (unos ticks), la alargamos 3x.
                if (tipo == NPCID.EaterofWorldsHead)
                {
                    _pendCabeza = idx;
                    _pendTicks = 45;
                    _pendIntentos = 0;
                }

                EcoRed.AnunciarMundo("Mods.AethonMod.Furia.Jefe",
                    new Color(226, 64, 64), jefe.FullName, _oleadaActual + 1);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, jefe.Center);

                // v6.50.30 — EL DADO DEL INVIERNO (la letra del usuario:
                // «has que sea un jefe que salga con una probalidad de 1%
                // en oleadas»): Deerclops YA NO es guardián de zona ni del
                // Juicio — cada vez que nace un jefe de oleada se tira UN
                // dado de 100: al 1, EL INVIERNO CAMINANTE se sienta a la
                // mesa como INVITADO EXTRA (borde opuesto, sello de la
                // MISMA oleada, su esencia y sus dientes de escarcha). No
                // cuenta para la fase: la oleada se paga con el guardián
                // oficial — el raro es el POSTRE.
                if (Main.rand.Next(100) == 0)
                    NacerJefeRaro(hambriento, lado);
            }
            catch { _bossIdx = -1; _tipoJefeOleada = -1; }
        }

        /// <summary>
        /// v6.50.30 — EL JEFE RARO DE LAS OLEADAS: DEERCLOPS, el invierno
        /// caminante. 1% por oleada (el dado de SpawnJefeOleada). Nace en
        /// el BORDE OPUESTO del guardián oficial, vestido con el sello de
        /// la MISMA oleada (stats ×(k+1), XP ×(k+1), aura y dientes de
        /// escarcha) y deja caer SU ESENCIA. Es un INVITADO: la oleada se
        /// completa con el guardián oficial — si el raro sobrevive al
        /// festín, el mundo se encarga de él al terminar (CheckActive
        /// vuelve a la normalidad cuando la furia muere).
        /// </summary>
        private static void NacerJefeRaro(Player hambriento, int ladoGuardian)
        {
            try
            {
                // el lado OPUESTO del guardián: el festín rodea.
                Vector2 pos = PosicionBordeJefe(hambriento, ladoGuardian < 0 ? 1 : -1);

                int idx = NPC.NewNPC(hambriento.GetSource_FromAI(), (int)pos.X, (int)pos.Y,
                    NPCID.Deerclops);
                NPC raro = (idx >= 0 && idx < Main.maxNPCs) ? Main.npc[idx] : null;
                if (raro == null || !raro.active) return;

                raro.GetGlobalNPC<OleadaNPC>().Marcar(raro, _oleadaActual, jefe: true, nivel: _nivelFuria);
                raro.netUpdate = true;

                // EL ANUNCIO DE LA RAREZA (color propio: la escarcha).
                EcoRed.AnunciarMundo("Mods.AethonMod.Furia.JefeRaro",
                    new Color(137, 178, 212), raro.FullName, _oleadaActual);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, raro.Center);
            }
            catch { }
        }

        // ==================================================================
        //  v6.50.29 — NOTA DE MIGRACIÓN: LA CUNA ENTERA MURIÓ.
        //  BuscarCuna/PosicionBorde/CajaLibre/BuscarSuelo/
        //  DistanciaFueraDePantalla/SpawnMonstruo/ContarChusma — toda la
        //  maquinaria de spawn manual fue SUSTITUIDA por el MOTOR NATURAL
        //  de vanilla (EditSpawnRate/EditSpawnPool en OleadaNPC, el mismo
        //  que las lunas de calabaza/escamarcha usan): él elige el tile
        //  (anillo 0.52–0.7× pantalla — más cerca que nunca), valida el
        //  suelo/el aire/el agua/la lava (jamás en pared) y respeta los
        //  límites del mundo. La vieja cuna podía fallar en cadena: si
        //  CajaLibre no hallaba hueco, el monstruo "no nacía" — con el
        //  Juicio de por medio, la furia entera podía pasar SIN UN SOLO
        //  enemigo visible. El motor de vanilla no falla: es el mismo que
        //  llena el mundo de enemigos desde 2011.
        // ==================================================================
        //  EL FINAL
        // ==================================================================

        private static void Terminar()
        {
            // v6.50.2 — EL FESTÍN CAMINA: el estado final (Inactivo) también
            // viaja — sin esto el F8 del portador mostraba el último festín
            // "en marcha" para siempre. ANTES de resetear _jugador (lo
            // necesita la réplica); seguro en unload (try/catch dentro de
            // SincronizarHambre y la réplica ya puede no existir).
            try
            {
                Player ultimo = (_jugador >= 0 && _jugador < Main.maxPlayers)
                    ? Main.player[_jugador] : null;
                if (ultimo != null && ultimo.active)
                    EcoRed.SincronizarHambre(ultimo); // no-op fuera del servidor
            }
            catch { }

            _fase = Fase.Inactivo;
            _oleadasTotales = 0;
            _oleadaActual = 0;
            _puntosOleada = 0;
            _poolActual = null;      // el motor natural vuelve a lo suyo
            _bossIdx = -1;
            _tipoJefeOleada = -1;
            _jugador = -1;
            _spawneados = 0;
            _ticksSuma = 0;
            // v6.50.59 — LA NUEVA MESA SE RECOGE: el nivel del festín y su
            // mezcla de biomas mueren con el festín (el NIVEL del portador
            // NO: vive en su ShardPlayer).
            _nivelFuria = 1;
            _acompanantes = null;
            _firmaBioma = "";

            // v6.50.10 — FIX: también las RÉPLICAS de cliente (el F8 de
            // un remoto mostraba el último festín "en marcha" para
            // siempre tras cambiar de mundo — fantasmas estáticos que
            // nadie reseteaba; Terminar corre vía OnWorldUnload).
            FaseCliente = 0;
            OleadaCliente = 0;
            TotalesCliente = 0;
            PuntosCliente = 0;
            RequeridosCliente = 0;
        }

        /// <summary>
        /// v6.49 — EL DIAGNÓSTICO DEL FESTÍN (lo pinta el overlay F8): la
        /// oleada y la fase, ya localizadas — o el silencio.
        /// </summary>
        public static string Diagnostico()
        {
            try
            {
                // v6.50.2 — FIX (diagnóstico de furia siempre "inactivo" en
                // clientes MP): la máquina vive SOLO en el server
                // (PostUpdateWorld) — un remoto leía _fase == Inactivo
                // eterno. En clientes MP se leen los estáticos que
                // EcoRed.MsgHambre llena en la recepción (red de seguridad
                // 600t + cada cambio de fase).
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    if (FaseCliente == 0)
                        return Language.GetTextValue("Mods.AethonMod.Diag.FuriaInactivo");
                    return Language.GetTextValue("Mods.AethonMod.Diag.FuriaActiva",
                        OleadaCliente, TotalesCliente);
                }
                if (_fase == Fase.Inactivo)
                    return Language.GetTextValue("Mods.AethonMod.Diag.FuriaInactivo");
                return Language.GetTextValue("Mods.AethonMod.Diag.FuriaActiva",
                    _oleadaActual, _oleadasTotales);
            }
            catch { return ""; }
        }

        // ==================================================================
        //  v6.50.29 — EL INDICADOR DE OLEADA (el clon de vanilla)
        // ==================================================================

        /// <summary>El alfa del indicador (el fade de vanilla: ±0.05/tick).</summary>
        private static float _alfaIndicador = 0f;
        /// <summary>El icono del título (el grimorio — cacheado).</summary>
        private static Microsoft.Xna.Framework.Graphics.Texture2D _iconoTitulo = null;

        /// <summary>
        /// v6.50.29 — EL INDICADOR DE OLEADA IGUAL QUE TERRARIA ORIGINAL
        /// (la orden del usuario): el clon EXACTO de Main.
        /// DrawInvasionProgress — la caja de abajo a la derecha con
        /// «Oleada {0}: {1}%» + la barra amarilla/naranja/negra sobre
        /// ColorBar, y la cajita del título con el icono y el nombre del
        /// evento (como la cajita de la luna de calabaza, pero con NUESTRO
        /// grimorio y «Furia del grimorio»). Los pinceles son LOS DE
        /// VANILLA: Utils.DrawInvBG, TextureAssets.ColorBar,
        /// TextureAssets.MagicPixel, Utils.DrawBorderString — píxel a
        /// píxel el mismo look. PERSISTENTE mientras el festín vive (como
        /// la barra del Ejército del Antiguo) y se desvanece al terminar
        /// (el ±0.05 de vanilla).
        /// </summary>
        public override void PostDrawInterface(SpriteBatch spriteBatch)
        {
            try { DibujarIndicador(); }
            catch { _alfaIndicador = 0f; }
        }

        private static void DibujarIndicador()
        {
            if (Main.gameMenu) return;

            // === ¿VIVO? — desde la primera oleada hasta el Juicio; la
            // llamada y el final lo desvanecen (el fade de vanilla).
            bool enCliente = Main.netMode == NetmodeID.MultiplayerClient;
            int faseVista = enCliente ? FaseCliente : (int)_fase;
            bool vivo = faseVista == (int)Fase.Monstruos || faseVista == (int)Fase.Jefe ||
                        faseVista == (int)Fase.Interludio || faseVista == (int)Fase.Especial;
            _alfaIndicador = MathHelper.Clamp(_alfaIndicador + (vivo ? 0.05f : -0.05f), 0f, 1f);
            if (_alfaIndicador <= 0.001f) return;

            int oleada = enCliente ? OleadaCliente : _oleadaActual;
            if (oleada <= 0) return;

            // === LOS NÚMEROS ===
            int puntos, requeridos;
            if (faseVista == (int)Fase.Especial)
            {
                // EL JUICIO: la barra cuenta los guardianes CAÍDOS de los
                // SEIS (los sellos viajan por SendExtraAI — el conteo
                // funciona también en el cliente remoto).
                int vivos = 0;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n == null || !n.active) continue;
                    var sello = n.GetGlobalNPC<OleadaNPC>();
                    if (sello != null && sello.EsDeOleada && sello.EsEspecial &&
                        sello.EsJefeDeOleada) vivos++;
                }
                puntos = Math.Max(0, GuardianesJuicio - vivos);
                requeridos = GuardianesJuicio;
            }
            else if (enCliente)
            {
                puntos = PuntosCliente;
                requeridos = RequeridosCliente > 0 ? RequeridosCliente
                    : PuntosRequeridos(Math.Min(oleada, 10));
            }
            else
            {
                puntos = _puntosOleada;
                requeridos = PuntosRequeridos(_oleadaActual);
            }
            // La fase del guardián: la oleada YA ESTÁ PAGADA — barra llena
            // (el jugador lee «oleada cobrada, jefe pendiente»).
            if (faseVista == (int)Fase.Jefe || faseVista == (int)Fase.Interludio)
                puntos = requeridos;

            float num = 0.5f + _alfaIndicador * 0.5f;

            // === LA CAJA DE LA OLEADA (clon píxel a píxel) ===
            int w = (int)(200f * num), h = (int)(45f * num);
            Vector2 centro = new Vector2(Main.screenWidth - 120, Main.screenHeight - 40);
            var sb = Main.spriteBatch;
            Utils.DrawInvBG(sb, new Rectangle((int)centro.X - w / 2, (int)centro.Y - h / 2, w, h),
                new Color(63, 65, 151, 255) * 0.785f);

            string texto = Language.GetTextValue("Mods.AethonMod.Furia.Indicador",
                oleada,
                requeridos != 0 ? ((int)(puntos * 100f / requeridos)).ToString() + "%" : puntos.ToString());

            Texture2D barra = Terraria.GameContent.TextureAssets.ColorBar.Value;
            float frac = requeridos != 0 ? MathHelper.Clamp((float)puntos / requeridos, 0f, 1f) : 1f;
            float largo = 169f * num, alto = 8f * num;
            Vector2 pos = centro + Vector2.UnitY * alto + Vector2.UnitX * 1f;
            Utils.DrawBorderString(sb, texto, pos, Color.White * _alfaIndicador, num, 0.5f, 1f);
            sb.Draw(barra, centro, null, Color.White * _alfaIndicador, 0f,
                new Vector2(barra.Width / 2, 0f), num, SpriteEffects.None, 0f);
            pos += Vector2.UnitX * (frac - 0.5f) * largo;
            sb.Draw(Terraria.GameContent.TextureAssets.MagicPixel.Value, pos,
                new Rectangle(0, 0, 1, 1), new Color(255, 241, 51) * _alfaIndicador, 0f,
                new Vector2(1f, 0.5f), new Vector2(largo * frac, alto), SpriteEffects.None, 0f);
            sb.Draw(Terraria.GameContent.TextureAssets.MagicPixel.Value, pos,
                new Rectangle(0, 0, 1, 1), new Color(255, 165, 0, 127) * _alfaIndicador, 0f,
                new Vector2(1f, 0.5f), new Vector2(2f, alto), SpriteEffects.None, 0f);
            sb.Draw(Terraria.GameContent.TextureAssets.MagicPixel.Value, pos,
                new Rectangle(0, 0, 1, 1), Color.Black * _alfaIndicador, 0f,
                new Vector2(0f, 0.5f), new Vector2(largo * (1f - frac), alto), SpriteEffects.None, 0f);

            // === LA CAJITA DEL TÍTULO (el grimorio + «Furia del grimorio») ===
            if (_iconoTitulo == null || _iconoTitulo.IsDisposed)
                _iconoTitulo = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Weapons/GrimoireEternal").Value;
            Texture2D icono = _iconoTitulo;
            // v6.50.59 — EL TÍTULO CON NIVEL: la furia de nivel 2+ se
            // anuncia en SU cajita (el nivel es el nombre del castigo). En
            // el cliente MP el nivel lo trae TotalesCliente (el TOTAL de
            // oleadas ES el nivel — sin tocar el MsgHambre).
            int nivelVisto = enCliente ? TotalesCliente : _oleadasTotales;
            string titulo = nivelVisto > 1 && nivelVisto <= 10
                ? Language.GetTextValue("Mods.AethonMod.Furia.IndicadorTituloNivel", nivelVisto)
                : Language.GetTextValue("Mods.AethonMod.Furia.IndicadorTitulo");
            Vector2 medTitulo = Terraria.GameContent.FontAssets.MouseText.Value.MeasureString(titulo);
            float offX = 120f;
            if (medTitulo.X > 200f) offX += medTitulo.X - 200f;
            Rectangle r2 = Utils.CenteredRectangle(
                new Vector2(Main.screenWidth - offX, Main.screenHeight - 80),
                (medTitulo + new Vector2(icono.Width + 12, 6f)) * num);
            Utils.DrawInvBG(sb, r2, new Color(122, 62, 66) * 0.5f);
            sb.Draw(icono, r2.Left() + Vector2.UnitX * num * 8f, null,
                Color.White * _alfaIndicador, 0f, new Vector2(0f, icono.Height / 2),
                num * 0.8f, SpriteEffects.None, 0f);
            Utils.DrawBorderString(sb, titulo, r2.Right() + Vector2.UnitX * num * -22f,
                Color.White * _alfaIndicador, num * 0.9f, 1f, 0.4f);
        }

        public override void OnWorldUnload()
        {
            // v6.50.15 — ARMADURA (auditoría R55-c): Terminar() y el
            // Reiniciar del aura ya son nulo-seguros, pero OnWorldUnload
            // corre en la cadena de descarga — el mismo blindaje que los
            // vecinos de abajo.
            try { Terminar(); } catch { }
            try { AuraLib.Reiniciar(); } catch { } // los emisores de partículas mueren con el mundo
            // v6.49 — EL BARRENDERO COMPLETO (auditoría AUD-C): el núcleo
            // de VFX y el anti-coros de audio también mueren con el mundo.
            try { VFXCore.Reiniciar(); } catch { }
            try { AudioLib.Reiniciar(); } catch { }
            // v6.50.1 — FIX: el contador de la carnada era un ESTÁTICO que
            // sobrevivía al mundo (dato de mundo en campo de clase — la
            // deuda nº recurrente de las auditorías). Vuelve al default.
            try { Items.CarnadaDelGrimorio.OleadasPreparadas = 3; } catch { }
        }
        public override void Unload()
        {
            Terminar();
            _iconoTitulo = null;   // el asset muere con el mod
            _alfaIndicador = 0f;
            AuraLib.Reiniciar(); // las texturas de ruido también
        }
    }
}
