using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.UI;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Cosmetic
{
    /// <summary>
    /// ParejaCerebro — v6.50.122 — EL CEREBRO DEL GRIMORIO MENOR (la
    /// mascota minigrimorio — ideas 15/21 de la letra original). NACIÓ
    /// en la .117 montado en la cría de luz… hasta que el usuario
    /// desenredó los conceptos: la mascota de luz es la miniatura del
    /// JEFE (solo alumbra, estalla y se fusiona) y ESTA máquina (los
    /// mini minions, la sombrilla, el pueblo, el AFK) es el repertorio
    /// del LIBRO en miniatura. El huésped es el GrimorioMenorPet: lo
    /// llama desde su AI (Tick + ElegirMeta) y desde su PreDraw
    /// (DibujarExtrasAlfa / DibujarExtrasAditivos).
    ///
    /// LA MÁQUINA (la letra, punto por punto):
    /// · EL AFK: si el dueño no se mueve (≥4 px contra su ANCLA) durante
    ///   3600 ticks (~1 min), el mini grimorio se aburre Y SALE A
    ///   MOLESTAR al vecino CON CASA más cercano (townNPC + homeTileX ≥ 0
    ///   + !homeless) a ≤1200 px del JUGADOR. Cualquier movimiento real
    ///   del dueño resetea el reloj — y si estaba de misión, ABORTA TODO
    ///   y vuelve inmediatamente a su lado.
    /// · LAS DOS FORMAS (la letra: «tendrá 2 formas, usando sus pequeños
    ///   mini minions o usando su pequeña sombra de la página»), al
    ///   llegar a ~90 px del vecino, ALTERNANDO golpe a golpe:
    ///   FORMA 1 — LA MINI SOMBRA: una sombrilla de página (elipse
    ///   oscura semitransparente) BAJO el NPC; cada 90 ticks «muerde».
    ///   FORMA 2 — LOS 3 MINI OJOS EMBISTEN: salen de su órbita hacia el
    ///   vecino (ida ~40 t + vuelta ~40 t) y golpean al contacto.
    /// · EL DAÑO: 1, JAMÁS MÁS, y SIN acreditación al jugador
    ///   (noPlayerInteraction) — ni ira de guardias, ni felicidad, ni
    ///   XP: «esto no afecta en nada al jugador». Tope 10 golpes por
    ///   vecino (los NPCs de pueblo tienen 250 HP: 10 golpes = nada).
    /// · EL ABURRIMIENTO: a los 10 golpes se aburre (emote final) — al
    ///   50% busca OTRO vecino cercano (contador nuevo) o vuelve al
    ///   lado del dueño, y se queda orbitándolo normal.
    ///
    /// LA SINCRONÍA (el contrato de la casa, aprendido del
    /// MiniEstallidoPet): el proyectil es netImportant — el servidor Y
    /// todos los clientes corren esta IA. Por eso TODO el cerebro es
    /// DETERMINISTA: lee posiciones sincronizadas, usa el reloj del
    /// mundo y tira la «moneda» con VFXCore.Hash01 (NUNCA Main.rand) —
    /// así todas las máquinas evolucionan IGUAL sin gastar ancho de red.
    /// Los EFECTOS sí se reparten: el StrikeNPC corre en SP/servidor
    /// (donde el daño es REAL); los emotes y el polvo corren en las
    /// pantallas (donde se VE). En MP puro, si la copia del cliente
    /// llegara a desviarse un tick, la red la corrige sola — lo peor
    /// que puede pasar es que un emote salga 1 tick tarde (teatro).
    /// </summary>
    public static class ParejaCerebro
    {
        // === LOS MODOS DE LA PAREJA ===
        public const float MODO_NORMAL = 0f;    // el hombro del dueño (la vida serena)
        public const float MODO_VIAJAR = 1f;    // vuela hacia el vecino elegido
        public const float MODO_MOLESTAR = 2f;  // ya llegado: las DOS FORMAS
        public const float MODO_VOLVER = 3f;    // de vuelta al lado del dueño
        /// <summary>v6.50.126→.131 — nació como LA GUERRA DE LOS CONEJOS
        /// BLANCOS (Re:Zero); la letra de la .130 la bautizó EL CORO DE
        /// LAS HADAS (hadas de COLORES, la misma silueta de siempre,
        /// cada una con su color); la letra de la .131 lo selló: «deja el
        /// evento de las hadas como evento PREDERMINADO y que suceda DE
        /// VEZ EN CUANDO» — el evento de los conejos reales murió y el
        /// coro reina solo, sin muro de carne de por medio. Hadas de
        /// colores que acosan al libro quieto. Puro teatro.</summary>
        public const float MODO_PLAGA = 4f;     // EL CORO DE LAS HADAS (predeterminado)
        /// <summary>v6.50.129 — LA CURIOSIDAD DE LA LETRA: «el grimorio
        /// menor despues de varios segundos sin que el jugador se mueva
        /// debe ser curioso y volar por ahi, en ese momento es cuando
        /// vuela y molesta a los npc y es cuando se puede activar el
        /// evento de conejos blancos». Es EL MODO CENTRAL del repertorio:
        /// deambula alrededor del dueño (Lissajous perezoso), y DESDE AHÍ
        /// salen las misiones al vecindario y la GUERRA DE LOS CONEJOS.
        /// Es un evento que NO AFECTA EN NADA a nadie — puro teatro.</summary>
        public const float MODO_CURIOSO = 5f;

        /// <summary>v6.50.129 — «varios segundos sin que el jugador se
        /// mueva» (la .122 exigía UN MINUTO — ahora: 8 segundos y el libro
        /// ya está curioseando por ahí).</summary>
        private const int TICKS_CURIOSIDAD = 480;

        /// <summary>El tope de la letra: 10 golpes de 1 por vecino.</summary>
        private const int GOLPES_TOPE = 10;

        /// <summary>Tick de la embestida en que el PRIMER ojo toca al vecino.</summary>
        private const int EMBESTIDA_CONTACTO = 40;

        /// <summary>Fin de la embestida: ida (40) + desfase de hermanos (10) + vuelta (40).</summary>
        private const int EMBESTIDA_FIN = 90;

        /// <summary>«town NPC CON CASA a ≤1200px del JUGADOR».</summary>
        private const float RADIO_VECINO = 1200f;

        /// <summary>El compás de la mordida de la sombrilla (FORMA 1).</summary>
        private const int TICKS_MORDIDA = 90;

        // ==================================================================
        //  v6.50.126 — LA PLAGA DEL CONEJO BLANCO (el easteregg Re:Zero:
        //  «sería interesante poner eso de los conejos blancos como un
        //  easteregg algo así como minievento donde sea la minimascota con
        //  sus miniminions quien luche con ellos cuando se está en afk»).
        //
        //  LA INVESTIGACIÓN (la pidió la letra): el GRAN CONEJO (大兎,
        //  Ōusagi) de Re:Zero — una de las TRES bestias mágicas mayores,
        //  creada por Daphne, la Bruja de la Gula: UNA SOLA conciencia
        //  repartida en una HORDA de conejos blancos del tamaño de un
        //  puño, HAMBRIENTOS SIN FIN (devoraron a Subaru en el Santuario
        //  — una de sus muertes más brutales), atraídos por el MANA
        //  (la nevada de Roswaal los convocó) y que se MULTIPLICAN al
        //  ser atacados (cada conejo se divide infinitamente: matarlos a
        //  golpes solo los hace MÁS).
        //
        //  LA TRADUCCIÓN AL MOD: el dueño quieto (AFK ~1 min) acumula
        //  maná quieto — y la HORDA LO HUELE: una bandada de conejos
        //  blancos de ojos rojos salta desde los bordes hacia él… y el
        //  GRIMORIO MENOR con sus TRES OJOS les SALE AL PASO (la
        //  minimascota y sus miniminions LUCHAN con ellos — la letra):
        //  los ojos EMBISTEN en oleadas, cada conejo golpeado SE DIVIDE
        //  (el gimmick de Re:Zero… la plaga crece) y el hambre del libro
        //  TERMINA DEVORÁNDOLOS a todos (el festín de la casa). Teatro
        //  100% determinista (la misma máquina de siempre: sin Main.rand
        //  en el cerebro, sin daño real, sin NPCs reales — el mundo ni se
        //  entera); si el dueño SE MUEVE, los conejos huyen y aborta.
        // ==================================================================

        /// <summary>La duración de una RONDA de combate (ticks) — el
        /// compás de las oleadas de embestida.</summary>
        private const int PLAGA_RONDA = 90;

        /// <summary>El tope de la horda (Re:Zero: se multiplican — pero
        /// el cielo solo presta doce cuerpos a una sola conciencia).</summary>
        private const int PLAGA_MAX_CONEJOS = 12;

        /// <summary>El tope de rondas (~1 minuto de guerra: el hambre del
        /// libro es más grande que la gula de Daphne).</summary>
        private const int PLAGA_MAX_RONDAS = 40;

        /// <summary>v6.51.1 — La moneda del easteregg: 22% plaga por
        /// tirada (la letra nueva: «que demore más en aparecer» — las
        /// hadas visitan, no se mudan a vivir).</summary>
        private const float PLAGA_PROBABILIDAD = 0.22f;

        /// <summary>Episodios de aburrimiento que deben pasar entre plaga
        /// y plaga (la horda no espanta dos veces seguidas).</summary>
        private const int PLAGA_ENFRIAMIENTO = 2;

        /// <summary>El radio del ANILLO DE AMENAZA: a dónde quiere llegar
        /// la horda (pegada al dueño — lo rodean, lo babean… sin tocarlo:
        /// el libro nunca los deja).</summary>
        private const float PLAGA_ANILLO = 110f;

        /// <summary>De dónde salen: el borde lejano de la pantalla.</summary>
        private const float PLAGA_NACIMIENTO = 520f;

        // === v6.50.125 — LOS MINIONS DE VERDAD (la letra: «los
        //     miniminions del grimorio mascota deben ser los mismos minion
        //     que el grimorio normal, o sea ojos, pero mas pequeños»): los
        //     3 hermanos visten el MISMO sprite del Ojo Eterno del ARMA
        //     (OjoEterno_Abierto 56×54, la familia del ojo) a ESCALA 0,27
        //     (~15 px en pantalla — el arma los usa a 0,375: claramente
        //     menores), con SU iris real (el mismo descanso y el mismo
        //     viaje por la almendra) y SUS párpados de textura (Medio /
        //     Cerrado — la convención de fases del arma, en miniatura). ===

        /// <summary>La escala del mini ojo: 0,27 (el arma lo usa a 0,375 —
        /// los hermanos son los mismos ojos, MENORES).</summary>
        public const float MiniOjoEscala = 0.27f;

        /// <summary>El descanso del iris dentro de la almendra (el del arma).</summary>
        private static readonly Vector2 MiniIrisRest = new(0.7f, 2.3f);

        /// <summary>La escala del iris (la del arma: caja 13 px → disco ~11).</summary>
        private const float MiniIrisEscala = 0.41f;

        /// <summary>El viaje del iris: media almendra (el ±7,5 del arma).</summary>
        private const float MiniMiraX = 7.5f;

        /// <summary>El viaje del iris: media altura (el ±2,5 del arma).</summary>
        private const float MiniMiraY = 2.5f;

        /// <summary>Las fases del párpado (la convención del arma).</summary>
        private const int OJO_ABIERTO = 0, OJO_MEDIO = 1, OJO_CERRADO = 2;

        /// <summary>EL REGISTRO DEL PUEBLO (v6.50.117 — nació en
        /// AethonMenorPet; v6.50.122 se mudó AQUÍ, con la máquina que lo
        /// escribe): npcIndex → el tick (Main.GameUpdateCount) de la
        /// ÚLTIMA molestia. Lo escriben los golpes del mini grimorio —
        /// los diálogos de reclamo de los NPCs (PuebloChismesNPC) lo leen
        /// para protestar («¿otra vez tú?»).</summary>
        public static readonly Dictionary<int, int> TicksMolestado = new();

        /// <summary>
        /// EL ESTADO DEL MINI GRIMORIO — una instancia por GrimorioMenorPet
        /// (un campo privado de la mascota). TODO determinista: sin Main.rand.
        /// </summary>
        public sealed class Estado
        {
            /// <summary>El ancla de la quietud del dueño (el punto contra
            /// el que se mide «se mueve &lt; 4 px»).</summary>
            public Vector2 AnclaDueño = Vector2.Zero;

            /// <summary>Los ticks seguidos sin un movimiento real.</summary>
            public int TicksQuieto;

            /// <summary>El modo actual (MODO_*).</summary>
            public float Modo = MODO_NORMAL;

            /// <summary>El reloj del modo actual (resetea al cambiar).</summary>
            public int TicksEnModo;

            /// <summary>El índice del NPC víctima (−1: ninguno).</summary>
            public int NpcObjetivo = -1;

            /// <summary>Los golpes al vecino ACTUAL (tope 10).</summary>
            public int Golpes;

            /// <summary>El total histórico de golpes (semilla de la moneda).</summary>
            public int TotalGolpes;

            /// <summary>Las misiones completadas (semilla de la moneda).</summary>
            public int Viajes;

            /// <summary>TRUE = FORMA 1 (la sombrilla), FALSE = FORMA 2 (los ojos).</summary>
            public bool FormaSombra = true;

            /// <summary>El compás de la mordida de la sombrilla.</summary>
            public int TicksMordisco;

            /// <summary>La embestida de los ojos: −1 quieta; 0..89 ida/vuelta.</summary>
            public int Embestida = -1;

            // === v6.50.126 — LA PLAGA DEL CONEJO BLANCO (Re:Zero) ===

            /// <summary>El reloj de la ronda actual (0..PLAGA_RONDA−1).</summary>
            public int PlagaRonda;

            /// <summary>Las rondas completadas (el reloj de la guerra).</summary>
            public int PlagaRondas;

            /// <summary>La semilla de ESTA plaga (nace de whoAmI+golpes+viajes
            /// — determinista: todas las máquinas simulan LA MISMA horda).</summary>
            public int PlagaSemilla;

            /// <summary>Los episodios de aburrimiento que faltan para que la
            /// horda pueda volver a oler el maná del dueño.</summary>
            public int PlagaEnfriamiento;

            /// <summary>v6.50.129 — LAS TIRADAS FALLIDAS de la moneda de
            /// la horda en ESTA sesión de curiosidad: a la TERCERA la
            /// horda viene GARANTIZADA («no veo que se active el evento» —
            /// ahora el usuario la ve, SIEMPRE, en el peor caso ~1 min).</summary>
            public int PlagaTiradasFallidas;

            /// <summary>LA HORDA (12 cuerpos, una sola conciencia — Re:Zero).
            /// v6.50.130 — EL CORO DE LAS HADAS: la misma bandada, cada
            /// cuerpo con SU color (ColorDeHada).</summary>
            public ConejoPlaga[] Conejos = new ConejoPlaga[PLAGA_MAX_CONEJOS];

        }

        /// <summary>UN CUERPO DE LA HORDA (v6.50.126 — Re:Zero: conejos
        /// blancos del tamaño de un puño que se multiplican al ser
        /// golpeados). TODO determinista: la posición evoluciona en Tick
        /// desde las mismas entradas en todas las máquinas. v6.50.130:
        /// el mismo cuerpo viste el CORO DE LAS HADAS (su color nace de
        /// la semilla — ColorDeHada).</summary>
        public struct ConejoPlaga
        {
            /// <summary>¿Vive este cuerpo?</summary>
            public bool Vivo;

            /// <summary>Su posición REAL (coordenadas del mundo — Tick la mueve).</summary>
            public Vector2 Pos;

            /// <summary>El ángulo de SU anillo de amenaza (a dónde salta).</summary>
            public float Angulo;

            /// <summary>La generación (0 la originals, 1-2 las divisiones —
            /// cada división nace más pequeña).</summary>
            public int Gen;

            /// <summary>La fase de su salto (semilla determinista).</summary>
            public float Fase;
        }

        // === v6.50.130 — LA PALETA DEL CORO (hadas de COLORES: la letra
        //     pidió que el evento fuera de hadas de colores — la MISMA
        //     silueta de siempre, cada una con SU color). ===

        /// <summary>Los SEIS colores del coro: rosa, cian, lima, ámbar,
        /// violeta y celeste (uno por hada — nace de la semilla).</summary>
        private static readonly Color[] ColoresDelCoro =
        {
            new Color(255, 126, 200),   // rosa
            new Color(111, 227, 255),   // cian
            new Color(168, 255, 111),   // lima
            new Color(255, 196, 107),   // ámbar
            new Color(192, 139, 255),   // violeta
            new Color(126, 168, 255),   // celeste
        };

        /// <summary>EL COLOR DE UN HADA (determinista — la semilla del
        /// evento y su índice: todas las pantallas ven el MISMO coro).
        /// v6.50.130: antes la horda era blanca con ojos rojos; la letra
        /// la bautizó — HADAS DE COLORES.</summary>
        private static Color ColorDeHada(int semilla, int idx)
        {
            return ColoresDelCoro[(semilla + idx * 3) % ColoresDelCoro.Length];
        }

        // ==================================================================
        //  EL CICLO DEL MUNDO — LA FUSIÓN/SEPARACIÓN (idea 21)
        // ==================================================================

        /// <summary>
        /// EL EQUINOCCIO (la letra de la idea 21): el día de Terraria
        /// dura 54000 ticks (Main.time corre 0→54000 mientras
        /// Main.dayTime). El amanecer (time &lt; 1500) y el crepúsculo
        /// (time &gt; 52500) son los «equinoccios» del mundo: los
        /// minutos en que el día y la noche pesan CASI igual — ahí los
        /// gemelos se FUSIONAN (el oscuro se coloca EXACTO sobre el
        /// claro: uno solo bicolor); el resto del tiempo SEPARADOS (el
        /// oscuro orbita al claro a ~34 px).
        /// </summary>
        public static bool Equinoccio() => Main.dayTime && (Main.time < 1500f || Main.time > 52500f);

        // ==================================================================
        //  EL TICK — el corazón de la máquina (lo llama la AI del claro)
        // ==================================================================

        public static void Tick(Projectile p, Player dueño, Estado e)
        {
            e.TicksEnModo++;

            // === EL RELOJ DEL AFK: la quietud del dueño. La comparación
            //     es contra el ANCLA (no contra el tick anterior): hasta
            //     el caminar MÁS LENTO se aleja del ancla y cuenta como
            //     movimiento real — solo la quietud de verdad acumula. ===
            if (e.AnclaDueño == Vector2.Zero ||
                Vector2.DistanceSquared(dueño.Center, e.AnclaDueño) >= 4f * 4f)
            {
                e.AnclaDueño = dueño.Center;
                e.TicksQuieto = 0;
                // EL DUEÑO SE MOVIÓ y el mini grimorio estaba de misión:
                // ABORTA TODO y vuelve INMEDIATAMENTE a su lado (la letra:
                // la mascota jamás se aleja de un dueño despierto).
                // v6.50.126 — también de la GUERRA: el maná quieto se
                // disipó y la horda HUYE (los conejos de Re:Zero
                // devoran al quieto… pero el libro jamás deja que el
                // espectáculo toque a su dueño despierto).
                // v6.50.129 — también de LA CURIOSIDAD (la letra nueva:
                // la curiosidad solo vive mientras el dueño está quieto).
                // v6.50.130 — también del CORO DE LAS HADAS y de los
                // CONEJOS REALES (la letra: «como el de las hadas que te
                // dije que dejara también se desaparece el evento si el
                // jugador se mueve» — TODO el teatro vive del dueño quieto).
                if (e.Modo == MODO_VIAJAR || e.Modo == MODO_MOLESTAR ||
                    e.Modo == MODO_PLAGA || e.Modo == MODO_CURIOSO)
                {
                    if (e.Modo == MODO_PLAGA) DispersarPlaga(e);
                    e.Modo = MODO_VOLVER;
                    e.NpcObjetivo = -1;
                    e.Embestida = -1;
                    e.TicksEnModo = 0;
                    e.PlagaTiradasFallidas = 0;   // sesión de curiosidad nueva
                }
            }
            else
            {
                e.TicksQuieto++;
            }

            switch (e.Modo)
            {
                case MODO_NORMAL:
                    // v6.50.129 — LA CURIOSIDAD DE LA LETRA: «después de
                    // varios segundos sin que el jugador se mueva debe ser
                    // curioso y volar por ahí». OCHO SEGUNDOS (antes era
                    // UN MINUTO entero) y el libro ya sale a curiosear.
                    if (e.TicksQuieto >= TICKS_CURIOSIDAD)
                    {
                        e.Modo = MODO_CURIOSO;
                        e.TicksEnModo = 0;
                        e.PlagaTiradasFallidas = 0;
                    }
                    break;

                case MODO_CURIOSO:
                    // ====================================================
                    //  v6.50.129 — EL VUELO CURIOSO: el libro deambula por
                    //  ahí alrededor del dueño (ElegirMeta dibuja la
                    //  figura)… y DESDE AQUÍ salen las dos travesuras de
                    //  la letra: molestar a los npc Y la guerra de los
                    //  conejos blancos («es cuando se puede activar el
                    //  evento» — un evento que no afecta a nadie: puro
                    //  teatro, como una cinemática curiosa del grimorio).
                    // ====================================================
                    {
                        // v6.51.1 — LA MONEDA PACIENTE (la letra: «el
                        // evento de las hadas es muy seguido, que demore
                        // más en aparecer»): la PRIMERA tirada espera 25 s
                        // de curiosidad, luego tira cada 45 s — 22% y
                        // GARANTIZADA a la quinta tirada fallida (peor
                        // caso ~3,4 min: las hadas son una visita RARA,
                        // no una inquilina).
                        if (e.TicksEnModo == 1500 ||
                            (e.TicksEnModo > 1500 && (e.TicksEnModo - 1500) % 2700 == 0))
                        {
                            if (e.PlagaEnfriamiento > 0)
                                e.PlagaEnfriamiento--;
                            else if (e.PlagaTiradasFallidas >= 4 ||
                                     VFXCore.Hash01(p.whoAmI,
                                         e.TotalGolpes * 13 + 29,
                                         e.Viajes * 19 + 7 + e.TicksQuieto * 131)
                                     < PLAGA_PROBABILIDAD)
                            {
                                e.PlagaTiradasFallidas = 0;
                                // v6.50.131 — EL CORO DE LAS HADAS ES EL
                                // EVENTO PREDERMINADO (la letra: «deja el
                                // evento de las hadas como evento
                                // predeterminado y que suceda de vez en
                                // cuando»): el evento de los conejos reales
                                // MURIÓ — la misma moneda de siempre (35 %
                                // y garantizada a la tercera), sin muro de
                                // carne de por medio.
                                ArrancarPlaga(p, e);
                                break;
                            }
                            else
                                e.PlagaTiradasFallidas++;
                        }

                        // MOLESTAR AL VECINDARIO (la letra: «en ese
                        // momento es cuando vuela y molesta a los npc»):
                        // cada 10 s de curiosidad, moneda del 50% — si hay
                        // vecino CON CASA cerca, la misión de siempre.
                        if (e.TicksEnModo > 0 && e.TicksEnModo % 600 == 0)
                        {
                            NPC vecino = BuscarVecino(dueño.Center, RADIO_VECINO, -1);
                            if (vecino != null && VFXCore.Hash01(p.whoAmI,
                                  e.TotalGolpes * 23 + 5,
                                  e.Viajes * 7 + 11 + e.TicksEnModo / 600) < 0.5f)
                            {
                                e.Modo = MODO_VIAJAR;
                                e.NpcObjetivo = vecino.whoAmI;
                                e.Golpes = 0;
                                e.TicksEnModo = 0;
                                e.TicksMordisco = 0;
                                e.FormaSombra = true;
                                e.Embestida = -1;
                                // v6.50.129 — EL RELOJ DE LA QUIETUD NO SE
                                // TOCA: la misión ES parte de la curiosidad
                                // (el dueño sigue quieto — al volver, el
                                // libro SIGUE curioseando, no espera otro
                                // minuto entero como la .128).
                            }
                        }
                    }
                    break;

                case MODO_VIAJAR:
                    {
                        NPC objetivo = NpcValido(e.NpcObjetivo);
                        if (objetivo == null)
                        {
                            // El vecino se murió por otra vía: decide como
                            // al aburrirse (sin la risa final — no fue obra suya).
                            Aburrirse(p, e, emoteFinal: false);
                            break;
                        }
                        // A ~90 px del vecino: EMPIEZA EL TEATRO.
                        if (Vector2.DistanceSquared(p.Center, objetivo.Center) < 90f * 90f)
                        {
                            e.Modo = MODO_MOLESTAR;
                            e.TicksEnModo = 0;
                            e.TicksMordisco = 0;
                            e.Embestida = -1;
                        }
                    }
                    break;

                case MODO_MOLESTAR:
                    {
                        NPC n = NpcValido(e.NpcObjetivo);
                        if (n == null)
                        {
                            Aburrirse(p, e, emoteFinal: false);
                            break;
                        }
                        if (e.FormaSombra)
                        {
                            // === FORMA 1 — LA MINI SOMBRILLA DE LA PÁGINA:
                            //     cada 90 t la sombrilla «muerde» ===
                            e.TicksMordisco++;
                            if (e.TicksMordisco >= TICKS_MORDIDA)
                            {
                                e.TicksMordisco = 0;
                                Golpear(p, e, n);
                            }
                        }
                        else
                        {
                            // === FORMA 2 — LOS 3 MINI OJOS EMBISTEN ===
                            if (e.Embestida < 0 && e.TicksEnModo > 45)
                                e.Embestida = 0;     // un respiro y se arrojan
                            if (e.Embestida >= 0)
                            {
                                e.Embestida++;
                                if (e.Embestida == EMBESTIDA_CONTACTO)
                                    Golpear(p, e, n);         // el primer ojo TOCA
                                if (e.Embestida >= EMBESTIDA_FIN)
                                    e.Embestida = -1;         // de vuelta a la órbita
                            }
                        }
                    }
                    break;

                case MODO_VOLVER:
                    // «Al volver, se queda orbitando al dueño normal.»
                    // v6.50.129 — ¿el dueño SIGUE quieto? La curiosidad
                    // CONTINÚA (misiones y guerra incluidas — la letra:
                    // todo vive DENTRO del estado curioso); si se movió,
                    // la vida serena del hombro.
                    if (Vector2.DistanceSquared(p.Center, dueño.Center) < 90f * 90f)
                    {
                        if (e.TicksQuieto >= TICKS_CURIOSIDAD)
                        {
                            e.Modo = MODO_CURIOSO;
                            e.TicksEnModo = 0;
                            e.PlagaTiradasFallidas = 0;
                        }
                        else
                        {
                            e.Modo = MODO_NORMAL;
                            e.TicksEnModo = 0;
                        }
                    }
                    break;

                case MODO_PLAGA:
                    // ====================================================
                    //  v6.50.126 — LA GUERRA DE LOS CONEJOS BLANCOS:
                    //  la horda salta hacia el anillo de amenaza y los
                    //  TRES OJOS responden en oleadas (una ronda = 90 t:
                    //  ~75 t de acecho + 15 t de embestida — y al caer la
                    //  ronda, los golpes se cobran: divididos o devorados).
                    // ====================================================
                    {
                        e.PlagaRonda++;

                        // LA HORDA AVANZA: cada cuerpo salta hacia SU punto
                        // del anillo (lerp perezoso — la convergencia lenta
                        // de una horda hambrienta que rodea su maná).
                        float anillo = PLAGA_ANILLO + MathF.Sin(Main.GlobalTimeWrappedHourly * 0.6f) * 8f;
                        for (int c = 0; c < e.Conejos.Length; c++)
                        {
                            if (!e.Conejos[c].Vivo) continue;
                            ConejoPlaga k = e.Conejos[c];
                            // el anillo de cada conejo gira lentamente (la
                            // rueda del asedio — determinista: el reloj).
                            float aRueda = k.Angulo + Main.GlobalTimeWrappedHourly * 0.22f;
                            Vector2 meta = dueño.Center + new Vector2(
                                MathF.Cos(aRueda) * anillo,
                                MathF.Sin(aRueda) * anillo * 0.72f - 14f);
                            k.Pos = Vector2.Lerp(k.Pos, meta, 0.010f);
                            e.Conejos[c] = k;
                        }

                        // LA RONDA CAE: los ojos cobran los golpes.
                        if (e.PlagaRonda >= PLAGA_RONDA)
                        {
                            e.PlagaRonda = 0;
                            e.PlagaRondas++;
                            RondaDePlaga(p, dueño, e);
                        }

                        // EL FIN DE LA GUERRA: la horda aplastada (o el
                        // reloj del hambre agotado — 40 rondas y el libro
                        // se cansa de ganar). El festín termina: risa y a
                        // casa (la sensación del deber cumplido).
                        bool quedaAlguno = false;
                        for (int c = 0; c < e.Conejos.Length; c++)
                            if (e.Conejos[c].Vivo) { quedaAlguno = true; break; }
                        if ((!quedaAlguno && e.PlagaRondas >= 6) || e.PlagaRondas >= PLAGA_MAX_RONDAS)
                        {
                            DispersarPlaga(e);
                            e.Modo = MODO_VOLVER;
                            e.TicksEnModo = 0;
                            // v6.50.129 — el reloj de la quietud ya NO se
                            // resetea: si el dueño sigue quieto, la
                            // curiosidad (y con el enfriamiento de por
                            // medio, otra guerra algún día) continúa sola.
                            if (Main.netMode != NetmodeID.Server)
                            {
                                try
                                {
                                    EmoteBubble.NewBubble(EmoteID.EmoteLaugh,
                                        new WorldUIAnchor(p), 100);
                                }
                                catch { }
                            }
                        }
                    }
                    break;

            }
        }

        // ==================================================================
        //  LA META — a dónde vuela el claro (lo llama la AI antes del muelle)
        // ==================================================================

        public static void ElegirMeta(Projectile p, Player dueño, Estado e, ref Vector2 meta)
        {
            switch (e.Modo)
            {
                case MODO_VIAJAR:
                    {
                        NPC n = NpcValido(e.NpcObjetivo);
                        if (n != null)
                            meta = n.Center + new Vector2(0f, -34f);   // llega POR ENCIMA del vecino
                    }
                    break;

                case MODO_MOLESTAR:
                    {
                        NPC n = NpcValido(e.NpcObjetivo);
                        if (n != null)
                        {
                            // EL MINI GRIMORIO RODEA a su víctima: elipse a
                            // ~110 px (los mini ojos orbitan a ÉL — el
                            // centro de la cacería es el libro).
                            float a = e.TicksEnModo * 0.045f;
                            meta = n.Center + new Vector2(MathF.Cos(a) * 110f,
                                MathF.Sin(a) * 62f - 26f);
                        }
                    }
                    break;

                case MODO_VOLVER:
                    meta = dueño.Center;   // directo al lado del dueño
                    break;

                case MODO_CURIOSO:
                    {
                        // v6.50.129 — EL VUELO CURIOSO DE LA LETRA («debe
                        // ser curioso y volar por ahí»): una figura de
                        // LISSAJOUS perezosa alrededor del dueño — dos
                        // senos inconmensurables (el rumbo JAMÁS se
                        // repite) con el radio respirando (57..153 px) y
                        // la altura siempre por encima de su cabeza (un
                        // libro curiosea FLOTANDO). TODO determinista
                        // (el reloj del modo): todas las máquinas ven el
                        // mismo paseo.
                        float sem = p.whoAmI * 0.618f;
                        float t = e.TicksEnModo / 60f;
                        float radio = 105f + MathF.Sin(t * 0.23f + sem) * 48f;
                        float a = t * 0.9f + sem;
                        meta = dueño.Center + new Vector2(
                            MathF.Sin(a) * radio,
                            MathF.Sin(a * 1.31f + 1.7f) * radio * 0.55f - 46f);
                    }
                    break;

                case MODO_PLAGA:
                    {
                        // v6.50.126 — EL LIBRO GUARDA LA FRONTERA: entre su
                        // dueño y el corazón de la horda, paseando la línea
                        // (el pastor de la guerra — los ojos hacen el
                        // trabajo sucio, el libro vigila y muerde el aire).
                        Vector2 centroHorda = CentroDeLaHorda(e, dueño.Center);
                        Vector2 frente = centroHorda - dueño.Center;
                        float fl = frente.Length();
                        frente = fl > 0.001f ? frente / fl : Vector2.UnitX;
                        float a = Main.GlobalTimeWrappedHourly * 0.5f;
                        meta = dueño.Center + frente * 62f
                            + new Vector2(MathF.Cos(a) * 26f, MathF.Sin(a) * 18f - 22f);
                    }
                    break;

            }
        }

        // ==================================================================
        //  EL GOLPE — 1 de daño SIN acreditación + el teatro de emotes
        // ==================================================================

        private static void Golpear(Projectile p, Estado e, NPC n)
        {
            e.Golpes++;
            e.TotalGolpes++;

            // EL REGISTRO DEL PUEBLO (lo leerán los diálogos de reclamo
            // de los NPCs): vecino → tick (GameUpdateCount) de la última
            // molestia.
            TicksMolestado[n.whoAmI] = (int)Main.GameUpdateCount;

            // === 1 DE DAÑO, JAMÁS MÁS — Y SIN ACREDITACIÓN AL JUGADOR
            //     (la letra: «esto no afecta en nada al jugador»). LA
            //     FIRMA 2026 del golpe (verificada contra el tML
            //     2026.07.3.0 real):
            //     npc.StrikeNPC(NPC.HitInfo hit, bool fromNet, bool noPlayerInteraction)
            //     — con noPlayerInteraction:TRUE el daño NO pasa por el
            //     pipeline del jugador: ni ira de guardias, ni felicidad,
            //     ni XP, ni drops a su nombre. (La otra firma del mod,
            //     SimpleStrikeNPC, también existe — pero StrikeNPC con
            //     HitInfo es el cauce limpio del motor.)
            //     NOTA MP: esto corre en SP y en el SERVIDOR (donde el
            //     daño es real); el StrikeNPC de vanilla se encarga de la
            //     difusión. El resto (emotes/polvo) corre en las
            //     pantallas — en MP puro, si una copia client del cerebro
            //     desviara un tick, lo que se ve es teatro correcto y el
            //     servidor puso el daño de verdad. ===
            // LA VALLA DE LA CASA (decisión propia, documentada): el golpe
            // se cobra SIEMPRE que el vecino siga holgado de vida — pero si
            // una maratón de AFK (horas de «molesta → descansa → molesta»)
            // fuera dejando al vecino por los suelos, el daño SE DETIENE y
            // queda el teatro puro (emotes, sombrilla, embestidas). MOLESTAR
            // no es MATAR: el vecino jamás muere por culpa de la mascota.
            if (n.life > 20f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                try
                {
                    n.StrikeNPC(new NPC.HitInfo
                    {
                        Damage = 1,
                        SourceDamage = 1,
                        DamageType = DamageClass.Default,
                        Crit = false,
                        HitDirection = p.Center.X < n.Center.X ? 1 : -1,
                        Knockback = 0f,       // SIN empuje: no hay que MOVER al vecino
                        HideCombatText = false,
                    }, false, true);
                }
                catch { }
            }

            // === EL TEATRO (solo pantallas): el vecino ASUSTADO y la
            //     mascota RIÉNDOSE. OJO: en el tML 2026.07 EmoteID y ya
            //     NO viven en Terraria.ID — están en
            //     Terraria.GameContent.UI (nombres exactos verificados:
            //     EmoteFear, EmoteLaugh, EmoteSilly). ===
            if (Main.netMode != NetmodeID.Server)
            {
                try
                {
                    EmoteBubble.NewBubble(EmoteID.EmoteFear, new WorldUIAnchor(n), 90);
                    EmoteBubble.NewBubble(EmoteID.EmoteLaugh, new WorldUIAnchor(p), 90);
                }
                catch { }

                // LAS PARTÍCULAS OSCURAS de la mordida (o del zarpazo de
                // los ojos — el mismo polvillo del grimorio).
                for (int i = 0; i < 9; i++)
                {
                    Dust d = Dust.NewDustPerfect(
                        n.Center + new Vector2(Main.rand.NextFloat(-10f, 10f),
                            Main.rand.NextFloat(-14f, 4f)),
                        DustID.Shadowflame,
                        new Vector2(Main.rand.NextFloat(-0.8f, 0.8f),
                            Main.rand.NextFloat(-1.4f, -0.3f)),
                        128, new Color(110, 40, 170), 0.7f);
                    d.noGravity = true;
                }
            }

            // EL TOPE DE LA LETRA: «solo les puede hacer 1 de daño máximo
            // 10 de daño… hasta aburrirse» — 10 golpes y SE ABURRE.
            if (e.Golpes >= GOLPES_TOPE)
                Aburrirse(p, e, emoteFinal: true);
            else
                e.FormaSombra = !e.FormaSombra;   // «alternará las 2 formas» golpe a golpe
        }

        /// <summary>EL ABURRIMIENTO: la risa final y la moneda del 50%
        /// (determinista — Hash01, JAMÁS Main.rand: todas las máquinas
        /// tienen que decidir LO MISMO).</summary>
        private static void Aburrirse(Projectile p, Estado e, bool emoteFinal)
        {
            // LA MUECA DEL ABURRIMIENTO + la risa final (solo pantallas).
            if (emoteFinal && Main.netMode != NetmodeID.Server)
            {
                try
                {
                    EmoteBubble.NewBubble(EmoteID.EmoteSilly, new WorldUIAnchor(p), 90);
                    EmoteBubble.NewBubble(EmoteID.EmoteLaugh, new WorldUIAnchor(p), 90);
                }
                catch { }
            }

            // LA MONEDA DEL 50%: OTRO vecino cercano (contador NUEVO) o
            // de vuelta al lado del dueño.
            e.Viajes++;
            bool buscaOtro = VFXCore.Hash01(p.whoAmI, e.TotalGolpes * 7 + 3, e.Viajes * 13 + 5) < 0.5f;
            NPC otro = buscaOtro ? BuscarVecino(p.Center, RADIO_VECINO, e.NpcObjetivo) : null;
            if (otro != null)
            {
                e.Modo = MODO_VIAJAR;
                e.NpcObjetivo = otro.whoAmI;
                e.Golpes = 0;             // «repite el ciclo con contador nuevo»
            }
            else
            {
                e.Modo = MODO_VOLVER;     // «o vuelve al lado del dueño»
                e.NpcObjetivo = -1;
            }
            e.Embestida = -1;
            e.TicksMordisco = 0;
            e.TicksEnModo = 0;
            // v6.50.129 — EL RELOJ DE LA QUIETUD YA NO SE RESETEA (la
            // letra nueva: las misiones viven DENTRO de la curiosidad —
            // si el dueño sigue quieto, al volver el libro SIGUE
            // curioseando; el propio ancla detectará cualquier
            // movimiento REAL y cortará todo, como siempre).
        }

        // ==================================================================
        //  EL DIBUJADO — LOTE ALFA (lo NEGRO: la sombrilla y los ojos)
        // ==================================================================

        /// <summary>LOS EXTRAS DEL MINI GRIMORIO, LOTE ALFA: la sombrilla
        /// de página bajo el vecino (FORMA 1) y los discos NEGROS de los 3
        /// mini ojos — lo negro NO vive en el aditivo (negro = suma 0 =
        /// invisible). Se vuelca SOLO (Begin→Quads→FlushAlpha) ANTES del
        /// lote del cuerpo del mini grimorio: al salir, el búfer queda
        /// limpio.</summary>
        public static void DibujarExtrasAlfa(Projectile p, Estado e)
        {
            if (Main.netMode == NetmodeID.Server) return;

            NPC victima = e.Modo == MODO_MOLESTAR || e.Modo == MODO_VIAJAR
                ? NpcValido(e.NpcObjetivo) : null;

            VFXCore.Begin();

            // === LA SOMBRILLA DE PÁGINA (FORMA 1): la «pequeña sombra de
            //     la página» del grimorio en miniatura — una elipse oscura
            //     semitransparente BAJO el NPC que se HINCHE justo antes
            //     de morder (su compás es de 90 t, el del cerebro). ===
            if (e.Modo == MODO_MOLESTAR && e.FormaSombra && victima != null)
            {
                Vector2 pies = victima.Bottom + new Vector2(0f, 5f);
                float ancho = MathHelper.Clamp(victima.width * 1.25f, 26f, 60f);
                float fase = e.TicksMordisco / (float)TICKS_MORDIDA;          // 0..1 del compás
                float cerca = fase > 0.82f ? (fase - 0.82f) / 0.18f : 0f;      // la hinchazón final
                float hinchazon = 1f + 0.28f * cerca;
                VFXCore.Quad(pies, SombrasLib.Alfa(SombrasLib.HumoNegro, 0.52f),
                    new Vector2(ancho * hinchazon, ancho * 0.30f * hinchazon));
                VFXCore.Quad(pies, SombrasLib.Alfa(SombrasLib.Negro, 0.34f + 0.18f * cerca),
                    new Vector2(ancho * 0.72f, ancho * 0.20f));
            }

            // (v6.50.125 — LOS 3 MINI OJOS YA NO VIVEN AQUÍ: los hermanos
            // visten el SPRITE REAL del Ojo Eterno en miniatura y se
            // dibujan con el libro, en el lote vanilla — DibujarLosTresOjos;
            // aquí solo queda su RIM, en el lote aditivo.)

            // v6.50.126 — LA HORDA (Re:Zero): los conejos blancos saltan
            // BAJO el libro y sus ojos (la guerra se ve antes de que los
            // ojos embistan: son parte del piso de la pelea).
            // v6.50.130 — LA HORDA es ahora EL CORO DE LAS HADAS (la misma
            // silueta, colores propios — la letra de la .130).
            if (e.Modo == MODO_PLAGA) DibujarLaHorda(e);


            VFXCore.FlushAlpha();
        }

        // ==================================================================
        //  EL DIBUJADO — LOTE ADITIVO (el anillo rúnico, el rim y el aliento)
        // ==================================================================

        /// <summary>LOS EXTRAS DEL MINI GRIMORIO, LOTE ADITIVO: EL ANILLO
        /// RÚNICO OSCURO (violeta, orbitando al libro) y v6.50.125 — EL
        /// RIM VIOLETA + EL ALIENTO ÁMBAR de los 3 ojos (el halo que los
        /// lee de noche — los sprites reales viajan en el lote vanilla con
        /// el libro: DibujarLosTresOjos). NO vuelca nada: se suma al lote
        /// del cuerpo del mini grimorio (un solo volcado — la llaman justo
        /// antes de su FlushAdditive).</summary>
        public static void DibujarExtrasAditivos(Projectile p, Estado e)
        {
            float t = Main.GlobalTimeWrappedHourly;

            // === EL ANILLO RÚNICO OSCURO (idea 15, re-vestida en la .122:
            //     nació como mini sol DORADO de la cría de luz — la máquina
            //     cambió de dueño y de paleta): un disco VIOLETA con su
            //     anillo de 6 marcas rúnicas (pequeñas rayitas), orbitando
            //     al mini grimorio — las runas del libro en miniatura. ===
            {
                float ang = t * 0.6f + 0.7f;
                Vector2 runa = p.Center + new Vector2(MathF.Cos(ang) * 26f, MathF.Sin(ang) * 26f * 0.8f);
                float tam = 7f * (0.9f + 0.1f * MathF.Sin(t * 2.2f));
                Color violeta = new Color(150, 90, 225, 255);
                VFXCore.Quad(runa, violeta * 0.34f, new Vector2(tam * 3.2f, tam * 3.2f));
                VFXCore.Quad(runa, violeta * 0.60f, new Vector2(tam * 1.7f, tam * 1.7f));
                VFXCore.Quad(runa, new Color(236, 210, 255, 255) * 0.80f, new Vector2(tam * 0.8f, tam * 0.8f));
                // EL ANILLO RÚNICO: 6 rayitas girando con la runa.
                float giro = t * 0.5f;
                for (int m = 0; m < 6; m++)
                {
                    float a = giro + m * MathHelper.TwoPi / 6f;
                    Vector2 dir = new Vector2(MathF.Cos(a), MathF.Sin(a));
                    VFXCore.Line(runa + dir * (tam * 1.15f), runa + dir * (tam * 1.9f), violeta * 0.5f, 1.5f);
                }
            }

            if (Main.netMode == NetmodeID.Server) return;

            // === v6.50.124 — EL DESPERTAR DE LOS MINIONS: al nacer el libro,
            //     sus tres ojos BROTAN de las páginas (radio 0→44 en 36 t)
            //     con un ANILLO violeta de nacimiento que se expande y muere
            //     — el momento «los minions LLEGAN» que la .122 no tenía. ===
            float brotar = MathHelper.Clamp(p.localAI[0] / 36f, 0f, 1f);
            if (brotar < 1f)
            {
                float r = 22f + 48f * brotar;
                VFXCore.Quad(p.Center,
                    new Color(150, 90, 225, 255) * (0.45f * (1f - brotar)),
                    new Vector2(r, r), 0f, VFXCore.Ring);
            }

            // === v6.50.125 — EL RIM VIOLETA de los 3 ojos (la lección .66:
            //     lo oscuro sin rim es invisible de noche): la aureola que
            //     lee a los hermanos en CUALQUIER fondo — ahora DETRÁS del
            //     sprite real (este lote vuelca ANTES que el cuerpo: es el
            //     halo) + EL ALIENTO ÁMBAR del iris abierto (el fulgor
            //     cálido que asoma por dentro — el iris real viaja en el
            //     lote vanilla con la luz del mundo; este aliento lo
            //     mantiene VIVO de noche y el piso de luz hace el resto). ===
            NPC victima = e.Modo == MODO_MOLESTAR || e.Modo == MODO_VIAJAR
                ? NpcValido(e.NpcObjetivo) : null;
            PosicionOjos(p, e, victima, out Vector2[] ojos, out int[] estados, out Vector2[] miradas);
            for (int i = 0; i < 3; i++)
            {
                // EL RIM (en TODOS los estados: hasta el párpado cerrado
                // lleva su aureola — los hermanos nunca desaparecen).
                VFXCore.Quad(ojos[i], new Color(118, 74, 190, 255) * 0.30f, new Vector2(17f, 17f));
                if (estados[i] == OJO_ABIERTO)
                    VFXCore.Quad(ojos[i] + miradas[i] * MiniOjoEscala,
                        new Color(196, 140, 92, 255) * 0.22f, new Vector2(9f, 9f));
            }

            // === v6.50.126→.130 — LOS OJOS DEL CORO: dos puntos encendidos
            //     por hada, del COLOR DE CADA UNA con núcleo blanco,
            //     mirando al maná del dueño (antes eran ROJOS — la firma
            //     Re:Zero; la letra los bautizó de hadas: ahora brillan
            //     con SU propio color, la luz de dentro de cada una). ===
            if (e.Modo == MODO_PLAGA && Main.netMode != NetmodeID.Server)
            {
                Vector2 mira = Main.player[p.owner].MountedCenter;
                for (int c = 0; c < e.Conejos.Length; c++)
                {
                    ConejoPlaga k = e.Conejos[c];
                    if (!k.Vivo) continue;
                    float ancho = 13f - k.Gen * 1.6f;
                    Vector2 dir = mira - k.Pos;
                    float dl = dir.Length();
                    dir = dl > 0.001f ? dir / dl : Vector2.UnitX;
                    Vector2 perp = new(-dir.Y, dir.X);
                    Vector2 cabeza = PosicionDibujadaConejo(k, ancho);
                    Color hada = ColorDeHada(e.PlagaSemilla, c);
                    for (int o = -1; o <= 1; o += 2)
                    {
                        Vector2 punto = cabeza + dir * ancho * 0.22f + perp * (o * ancho * 0.22f);
                        VFXCore.Quad(punto, hada * 0.75f, new Vector2(4.6f, 4.6f));
                        VFXCore.Quad(punto, new Color(255, 252, 245, 255) * 0.85f,
                            new Vector2(2.6f, 2.6f));
                    }
                }
            }
        }

        // ==================================================================
        //  LA POSICIÓN DE LOS 3 MINI OJOS (compartida por ambos lotes)
        // ==================================================================

        /// <summary>Dónde está cada ojo, en qué FASE de párpado va
        /// (ABIERTO/MEDIO/CERRADO — v6.50.125: la convención de fases del
        /// ARMA, con sus texturas) y hacia dónde VIAJA su iris — TODO
        /// determinista (el reloj del mundo): todas las pantallas ven los
        /// MISMOS ojos. En FORMA 2 los ojos DEJAN la órbita y embisten al
        /// vecino (ida ~40 t + vuelta ~40 t, cada hermano sale 5 t después
        /// que el anterior — el primero toca en el tick 40 exacto, donde el
        /// cerebro cobra el golpe). v6.50.124: la órbita BAJA a ~44 px con
        /// el radio respirando (±3) y el vaivén propio de cada hermano —
        /// y al NACER el libro los ojos BROTAN de las páginas (36 t, la
        /// edad local del proyectil: el florecimiento es un teatro de
        /// pantalla, cada máquina ve el suyo): nacen CERRADOS y ABRREN los
        /// ojos al llegar (el MEDIO de la floración es la apertura).</summary>
        private static void PosicionOjos(Projectile p, Estado e, NPC victima,
            out Vector2[] pos, out int[] estados, out Vector2[] miradas)
        {
            pos = new Vector2[3];
            estados = new int[3];
            miradas = new Vector2[3];
            float t = Main.GlobalTimeWrappedHourly;

            // v6.50.124 — EL DESPERTAR: los mini minions BROTAN del libro
            // (radio 0 → completo en 36 t — p.localAI[0] la cuenta la IA
            // del GrimorioMenorPet).
            float brotar = MathHelper.Clamp(p.localAI[0] / 36f, 0f, 1f);

            // v6.50.122 — EL CENTRO ES EL MINI GRIMORIO MISMO (antes era
            // el punto medio de la pareja de luz: la máquina cambió de
            // dueño — los mini ojos orbitan AL LIBRO).
            Vector2 centro = p.Center;

            // LA MIRADA DE LOS HERMANOS: su dueño… salvo de misión —
            // entonces CLAVADA en la víctima (la vigilancia en miniatura).
            Vector2 objetivo = Main.player[p.owner].MountedCenter;
            if (victima != null) objetivo = victima.Center;

            // v6.50.126 — EN PLAGA: la mirada va a LA PRESA — el conejo
            // vivo más cercano (los ojos de la guerra no parpadean por
            // gusto: acechan).
            if (e.Modo == MODO_PLAGA)
            {
                float mejorD = float.MaxValue;
                for (int c = 0; c < e.Conejos.Length; c++)
                {
                    if (!e.Conejos[c].Vivo) continue;
                    float d = Vector2.DistanceSquared(e.Conejos[c].Pos, centro);
                    if (d < mejorD) { mejorD = d; objetivo = e.Conejos[c].Pos; }
                }
            }


            for (int i = 0; i < 3; i++)
            {
                float ang = t * 0.75f + i * MathHelper.TwoPi / 3f;   // 120° exactos
                // v6.50.126 — LA ÓRBITA SE ENCOGE (la letra: «sus pequeños
                // minion están muy separados deben estar más juntos»):
                // 44 → 27 px — una CORONA PEGADA al tomo (los hermanos
                // casi rozan las tapas: manada, no constelación),
                // respirando ±2 y el vaivén propio de cada hermano.
                float radio = (27f + MathF.Sin(t * 0.9f) * 2f) * brotar;
                float bob = MathF.Sin(t * 1.6f + i * 2.4f) * 2f;
                pos[i] = centro + new Vector2(MathF.Cos(ang) * radio,
                    MathF.Sin(ang) * radio * 0.82f + bob);

                // v6.50.125 — EL PARPADEO DE TRES FASES (la convención del
                // ARMA: ABIERTO → MEDIO → CERRADO → MEDIO, con las mismas
                // texturas): nacen CERRADOS (brotar) y ABRREN al llegar —
                // el MEDIO de la floración ES la apertura del ojo.
                float parp = (t * 1.15f + i * 2.61f) % 6.5f;
                if (brotar < 0.55f) estados[i] = OJO_CERRADO;
                else if (brotar < 0.75f) estados[i] = OJO_MEDIO;
                else if (parp <= 0.26f) estados[i] = OJO_CERRADO;
                else if (parp <= 0.42f || parp > 6.5f - 0.16f) estados[i] = OJO_MEDIO;
                else estados[i] = OJO_ABIERTO;

                // v6.50.125 — LA MIRADA EN LA ALMENDRA (el formato del
                // ARMA): el iris viaja ±7,5 × ±2,5 DENTRO del ojo hacia su
                // objetivo (art-píxeles, escalados al dibujar).
                Vector2 d = objetivo - pos[i];
                float len = d.Length();
                miradas[i] = len > 4f
                    ? new Vector2(d.X / len * MiniMiraX, d.Y / len * MiniMiraY)
                    : Vector2.Zero;
            }

            // FORMA 2 — LA EMBESTIDA: los ojos salen de la órbita hacia
            // el vecino y vuelven (suave ida con smoothstep, golpe seco).
            if (victima != null && e.Embestida >= 0)
            {
                for (int i = 0; i < 3; i++)
                {
                    int salida = i * 5;    // el desfase entre hermanos
                    float prog;
                    if (e.Embestida <= EMBESTIDA_CONTACTO + salida)
                        prog = MathHelper.Clamp((e.Embestida - salida) / 40f, 0f, 1f);          // la ida
                    else
                        prog = 1f - MathHelper.Clamp((e.Embestida - (EMBESTIDA_CONTACTO + salida)) / 40f, 0f, 1f); // la vuelta
                    float suave = prog * prog * (3f - 2f * prog);
                    Vector2 blanco = victima.Center + new Vector2((i - 1) * 5f, (i - 1) * -3f);
                    pos[i] = Vector2.Lerp(pos[i], blanco, suave);
                    if (prog > 0.05f)
                    {
                        // v6.50.130 — LA ANTICIPACIÓN: los primeros 8 t del
                        // arranque los ojos ENTRECIERRAN (el wind-up del
                        // golpe — se PREPARAN antes de lanzarse y abren de
                        // golpe al despegar: animación con intención, la
                        // letra de «mejora las animaciones»).
                        estados[i] = e.Embestida <= 8 + salida ? OJO_MEDIO : OJO_ABIERTO;
                    }
                }
            }

            // v6.50.126 — LA EMBESTIDA DE LA PLAGA: en los ÚLTIMOS 15 t de
            // cada ronda, los ojos SE LANZAN sobre los conejos que ESTA
            // ronda golpeará (la misma moneda que cobrará RondaDePlaga —
            // el golpe se VE llegar antes de caer: teatralidad).
            if (e.Modo == MODO_PLAGA && e.PlagaRonda >= PLAGA_RONDA - 15)
            {
                float prog = (e.PlagaRonda - (PLAGA_RONDA - 15)) / 15f;
                int golpe = 0;
                for (int c = 0; c < e.Conejos.Length && golpe < 3; c++)
                {
                    if (!e.Conejos[c].Vivo || !ConejoGolpeado(e, c, e.PlagaRondas)) continue;
                    int ojo = golpe++;
                    float suave = prog * prog * (3f - 2f * prog);
                    pos[ojo] = Vector2.Lerp(pos[ojo], e.Conejos[c].Pos, suave);
                    estados[ojo] = OJO_ABIERTO;   // la carga: ojos CLAVADOS
                }
            }

        }

        // ==================================================================
        //  v6.50.125 — LOS TRES OJOS DE VERDAD (los sprites reales del arma)
        // ==================================================================

        /// <summary>LOS 3 MINI MINIONS DE VERDAD (la letra de la .125: «los
        /// mismos minion que el grimorio normal, o sea ojos, pero mas
        /// pequeños»): el MISMO sprite del Ojo Eterno del ARMA a ESCALA
        /// 0,27 (el arma lo usa a 0,375), con SU iris (el descanso + el
        /// viaje de la almendra) y SUS párpados de textura (Medio/Cerrado).
        /// Se dibuja en el LOTE VANILLA junto al libro (lo llama
        /// GrimorioMenorPet.PreDraw justo después de DibujarElLibro) con un
        /// PISO DE LUZ: los ojos del libro NUNCA se apagan del todo — son
        /// mascota, la legibilidad manda (la lección .66).</summary>
        public static void DibujarLosTresOjos(Projectile p, Estado e)
        {
            if (Main.netMode == NetmodeID.Server) return;
            try
            {
                NPC victima = e.Modo == MODO_MOLESTAR || e.Modo == MODO_VIAJAR
                    ? NpcValido(e.NpcObjetivo) : null;
                PosicionOjos(p, e, victima, out Vector2[] ojos, out int[] estados, out Vector2[] miradas);

                Texture2D abierto = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Projectiles/OjoEterno_Abierto").Value;
                Texture2D medio = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Projectiles/OjoEterno_Medio").Value;
                Texture2D cerrado = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Projectiles/OjoEterno_Cerrado").Value;
                Texture2D iris = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Weapons/GrimorioHambriento_Iris").Value;
                if (abierto == null || iris == null) return;

                for (int i = 0; i < 3; i++)
                {
                    Texture2D cuerpo = estados[i] == OJO_CERRADO ? cerrado
                        : estados[i] == OJO_MEDIO ? medio : abierto;
                    if (cuerpo == null) cuerpo = abierto;
                    Vector2 posScr = ojos[i] - Main.screenPosition;
                    Vector2 origen = cuerpo.Size() * 0.5f;

                    // EL PISO DE LUZ (mascota: jamás negra total — el ojo
                    // ámbar del Eterno brilla hasta en la cueva profunda;
                    // el rim violeta del lote aditivo hace el resto).
                    Color luz = Lighting.GetColor(ojos[i].ToTileCoordinates());
                    luz = new Color(Math.Max((int)luz.R, 130), Math.Max((int)luz.G, 104), Math.Max((int)luz.B, 96));

                    Main.spriteBatch.Draw(cuerpo, posScr, null, luz, 0f, origen,
                        MiniOjoEscala, SpriteEffects.None, 0f);

                    if (estados[i] == OJO_ABIERTO)
                    {
                        // EL IRIS REAL (el descanso + el viaje del ARMA,
                        // a escala mini) — mira al dueño… o a la víctima.
                        Main.spriteBatch.Draw(iris,
                            posScr + (MiniIrisRest + miradas[i]) * MiniOjoEscala, null, luz, 0f,
                            iris.Size() * 0.5f, MiniIrisEscala * MiniOjoEscala,
                            SpriteEffects.None, 0f);
                    }
                }
            }
            catch { }
        }

        // ==================================================================
        //  v6.50.126 — LA PLAGA DEL CONEJO BLANCO (el motor Re:Zero)
        // ==================================================================

        /// <summary>LA PRIMERA OLA: la horda huele el maná quieto del
        /// dueño y ARRANCA la guerra (5 cuerpos desde los bordes de la
        /// pantalla, la semilla determinista, el susto del libro).</summary>
        private static void ArrancarPlaga(Projectile p, Estado e)
        {
            e.Modo = MODO_PLAGA;
            e.NpcObjetivo = -1;
            e.Embestida = -1;
            e.TicksEnModo = 0;
            // v6.50.129 — el reloj de la quietud ya NO se toca (la
            // curiosidad continúa tras la guerra si el dueño sigue
            // quieto — la tirada fallida también vuelve a cero: la
            // garantía de la tercera cuenta DESDE la guerra).
            e.PlagaTiradasFallidas = 0;
            e.PlagaRonda = 0;
            e.PlagaRondas = 0;
            e.PlagaEnfriamiento = PLAGA_ENFRIAMIENTO;   // la horda no espanta dos veces seguidas
            // LA SEMILLA (determinista — la misma en todas las máquinas:
            // nace del estado compartido, jamás del reloj ni de Main.rand).
            e.PlagaSemilla = unchecked((int)((p.whoAmI * 2654435761L
                + e.TotalGolpes * 941L + e.Viajes * 131L) & 0x7FFFFFFFL));

            Player dueño = Main.player[p.owner];
            for (int c = 0; c < 5; c++)
                NacerConejoEnElBorde(e, dueño.Center);

            // EL LIBRO LO VE PRIMERO (solo pantallas): el susto del
            // pastor antes de la guerra.
            if (Main.netMode != NetmodeID.Server)
            {
                try
                {
                    EmoteBubble.NewBubble(EmoteID.EmoteFear, new WorldUIAnchor(p), 90);
                }
                catch { }
            }
        }

        /// <summary>LA RONDA CAE: los ojos cobran los golpes de la ronda
        /// que terminó — cada conejo golpeado SE DIVIDE (Re:Zero: dos
        /// hijos más pequeños… la plaga crece) o ES DEVORADO (la tinta
        /// del libro se lo traga). Y mientras la horda sea poca, la
        /// conciencia manda REFUERZOS desde los bordes.</summary>
        private static void RondaDePlaga(Projectile p, Player dueño, Estado e)
        {
            int vivos = ContarVivos(e);
            // LA RONDA QUE ACABA DE CAER (PlagaRondas YA se incrementó —
            // la dash de los ojos la leyó como PlagaRondas−1).
            int ronda = e.PlagaRondas - 1;

            for (int c = 0; c < e.Conejos.Length; c++)
            {
                if (!e.Conejos[c].Vivo) continue;
                if (!ConejoGolpeado(e, c, ronda)) continue;

                ConejoPlaga k = e.Conejos[c];
                bool seDivide = k.Gen < 2 && vivos + 1 <= PLAGA_MAX_CONEJOS &&
                    VFXCore.Hash01(e.PlagaSemilla, c * 29 + 5, ronda * 3 + 1) < 0.45f;

                e.Conejos[c].Vivo = false;      // el cuerpo golpeado muere…
                vivos--;
                if (seDivide)
                {
                    // …PERO LA GULA SE REPARE: dos hijos nacen de él (el
                    // gimmick de Re:Zero — golpearlos solo los hace MÁS).
                    NacerConejo(e, k.Pos + new Vector2(-7f, -2f), k.Angulo - 0.28f, k.Gen + 1);
                    NacerConejo(e, k.Pos + new Vector2(7f, -2f), k.Angulo + 0.28f, k.Gen + 1);
                    vivos += 2;
                    PolvoPlaga(k.Pos, dividido: true);
                }
                else
                {
                    // DEVORADO: el festín de la casa — tinta violeta.
                    PolvoPlaga(k.Pos, dividido: false);
                }
            }

            // LOS REFUERZOS: la conciencia no se rinde fácil (mientras
            // queden rondas, los cuerpos siguen llegando).
            if (vivos < 4 && e.PlagaRondas < PLAGA_MAX_RONDAS - 6)
            {
                NacerConejoEnElBorde(e, dueño.Center);
                if (vivos < 2) NacerConejoEnElBorde(e, dueño.Center);
            }
        }

        /// <summary>Un conejo vivo menos (y sus hermanos huyen): el fin de
        /// la guerra — victoria (la horda aplastada), reloj agotado o el
        /// dueño DESPIERTO (la horda huye del maná que se movió).</summary>
        private static void DispersarPlaga(Estado e)
        {
            for (int c = 0; c < e.Conejos.Length; c++)
                e.Conejos[c].Vivo = false;
        }

        /// <summary>NACE UN CUERPO DE LA HORDA en la primera ranura libre
        /// (determinista: la fase de su salto nace de la semilla).</summary>
        private static void NacerConejo(Estado e, Vector2 pos, float angulo, int gen)
        {
            for (int c = 0; c < e.Conejos.Length; c++)
            {
                if (e.Conejos[c].Vivo) continue;
                e.Conejos[c] = new ConejoPlaga
                {
                    Vivo = true,
                    Pos = pos,
                    Angulo = angulo,
                    Gen = gen,
                    Fase = VFXCore.Hash01(e.PlagaSemilla, c * 7 + 13, gen * 31 + 5) * MathHelper.TwoPi
                };
                return;
            }
        }

        /// <summary>NACE UN CUERPO EN EL BORDE: el ángulo (determinista) y
        /// el salto desde lejos (la horda llega de FUERA de la pantalla).</summary>
        private static void NacerConejoEnElBorde(Estado e, Vector2 centroDueño)
        {
            int nacidos = ContarVivos(e);
            float a = VFXCore.Hash01(e.PlagaSemilla, nacidos * 17 + 3, 97) * MathHelper.TwoPi;
            NacerConejo(e, centroDueño + new Vector2(MathF.Cos(a), MathF.Sin(a) * 0.7f)
                * PLAGA_NACIMIENTO, a, 0);
        }

        /// <summary>LA MONEDA DEL GOLPE: ¿ESTE conejo recibe el zarpazo de
        /// la ronda N? (determinista — la misma lectura en Tick, en la
        /// dash de los ojos y en todas las máquinas).</summary>
        private static bool ConejoGolpeado(Estado e, int idxConejo, int ronda)
            => VFXCore.Hash01(e.PlagaSemilla, ronda * 31 + idxConejo * 7 + 11,
                idxConejo * 13 + 3) < 0.55f;

        /// <summary>¿Cuántos cuerpos vive la horda?</summary>
        private static int ContarVivos(Estado e)
        {
            int vivos = 0;
            for (int c = 0; c < e.Conejos.Length; c++)
                if (e.Conejos[c].Vivo) vivos++;
            return vivos;
        }

        /// <summary>EL CORAZÓN DE LA HORDA (el promedio de sus cuerpos —
        /// o el propio dueño si no queda nadie: el frente de batalla).</summary>
        private static Vector2 CentroDeLaHorda(Estado e, Vector2 respaldo)
        {
            Vector2 suma = Vector2.Zero;
            int vivos = 0;
            for (int c = 0; c < e.Conejos.Length; c++)
            {
                if (!e.Conejos[c].Vivo) continue;
                suma += e.Conejos[c].Pos;
                vivos++;
            }
            return vivos > 0 ? suma / vivos : respaldo;
        }

        /// <summary>El polvo del golpe (solo pantallas): el DESTELLO
        /// BLANCO de la división (golpeaste uno… ahora son dos) o la TINTA
        /// VIOLETA del devorado (el festín de la casa).</summary>
        private static void PolvoPlaga(Vector2 pos, bool dividido)
        {
            if (Main.netMode == NetmodeID.Server || Main.dedServ) return;
            try
            {
                int n = dividido ? 7 : 9;
                for (int i = 0; i < n; i++)
                {
                    float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                    float rap = Main.rand.NextFloat(0.6f, 2.4f);
                    Dust d = Dust.NewDustPerfect(pos,
                        dividido ? DustID.PurpleTorch : DustID.Shadowflame,
                        new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.8f) * rap, 150,
                        dividido ? new Color(240, 236, 255) : new Color(110, 40, 170),
                        Main.rand.NextFloat(0.7f, 1.2f));
                    d.noGravity = !dividido;
                }
            }
            catch { }
        }

        /// <summary>La posición DIBUJADA de un conejo (su Pos + el salto
        /// vivo del reloj — el rebote de la esperanza hambrienta).</summary>
        private static Vector2 PosicionDibujadaConejo(ConejoPlaga k, float ancho)
        {
            float salto = MathF.Abs(MathF.Sin(Main.GlobalTimeWrappedHourly * 5.2f + k.Fase));
            return k.Pos + new Vector2(0f, -salto * (5f + ancho * 0.25f));
        }

        /// <summary>LA HORDA EN PANTALLA (lote alfa): bolas blancas de
        /// orejas largas y tamaño de puño (Re:Zero) con su SOMBRA violeta
        /// bajo el cuerpo (la lección .66: lo blanco puro sobre cielo
        /// claro necesita piso) y su contorno oscuro (legibles en CUALQUIER
        /// fondo). El salto es del reloj del mundo — todas las pantallas
        /// ven la MISMA horda botando.
        /// v6.50.130 — EL CORO DE LAS HADAS: la MISMA silueta que la letra
        /// pidió conservar («dejémoslo de esa forma se ve bien como está»)
        /// pero cada hada con SU COLOR propio (ColorDeHada: rosa, cian,
        /// lima, ámbar, violeta, celeste) — contorno oscuro del propio
        /// color, cuerpo coloreado con núcleo CLARO, orejas vivas y un
        /// brillo que se enciende y apaga (la chispa de las hadas).</summary>
        private static void DibujarLaHorda(Estado e)
        {
            if (Main.netMode == NetmodeID.Server) return;
            float t = Main.GlobalTimeWrappedHourly;

            for (int c = 0; c < e.Conejos.Length; c++)
            {
                ConejoPlaga k = e.Conejos[c];
                if (!k.Vivo) continue;

                float ancho = 13f - k.Gen * 1.6f;       // 13 / 11,4 / 9,8 px
                Vector2 cuerpo = PosicionDibujadaConejo(k, ancho);

                // EL COLOR DE ESTA HADA (determinista — la semilla).
                Color hada = ColorDeHada(e.PlagaSemilla, c);
                Color núcleo = new Color(
                    (hada.R + 255) / 2, (hada.G + 255) / 2, (hada.B + 255) / 2, 255);

                // EL PISO: la sombra oscura bajo el cuerpo (clara, plana).
                VFXCore.Quad(k.Pos + new Vector2(0f, 5f),
                    SombrasLib.Alfa(SombrasLib.HumoNegro, 0.32f),
                    new Vector2(ancho * 1.5f, ancho * 0.42f));

                // EL CONTORNO OSCURO del PROPIO color (la legibilidad en
                // cualquier cielo).
                Color borde = new Color(hada.R * 38 / 100, hada.G * 38 / 100, hada.B * 52 / 100, 255);
                VFXCore.Quad(cuerpo, borde * 0.55f,
                    new Vector2(ancho * 2.35f, ancho * 2.35f), 0f, VFXCore.SoftGlow);

                // EL CUERPO COLOREADO (fuzzy — la hada): halo de SU color
                // + núcleo CLARO (casi blanco — la luz de dentro).
                VFXCore.Quad(cuerpo, hada * 0.66f,
                    new Vector2(ancho * 1.9f, ancho * 1.9f), 0f, VFXCore.SoftGlow);
                VFXCore.Quad(cuerpo, núcleo * 0.95f,
                    new Vector2(ancho * 1.05f, ancho * 0.95f), 0f, VFXCore.SoftGlow);

                // LA CHISPA DEL HADA: un brillo que se enciende y se apaga
                // a su compás (el polvo de hadas — cada una al suyo).
                float chispa = 0.5f + 0.5f * MathF.Sin(t * 3.3f + k.Fase * 4f);
                VFXCore.Quad(cuerpo + new Vector2(ancho * 0.5f, -ancho * 0.7f),
                    núcleo * (0.55f * chispa), new Vector2(ancho * 0.5f, ancho * 0.5f));

                // LAS OREJAS LARGAS (dos líneas vivas que se mecen con el
                // salto — la firma de la silueta de siempre).
                float mecer = MathF.Sin(t * 5.2f + k.Fase) * 0.22f;
                for (int o = -1; o <= 1; o += 2)
                {
                    float aOreja = -MathHelper.PiOver2 + o * 0.38f + mecer * o;
                    Vector2 punta = cuerpo + new Vector2(MathF.Cos(aOreja), MathF.Sin(aOreja))
                        * ancho * 1.25f;
                    VFXCore.Line(cuerpo + new Vector2(o * ancho * 0.18f, -ancho * 0.35f),
                        punta, núcleo * 0.88f, 2.6f);
                }
            }
        }

        // ==================================================================
        //  LAS BÚSQUEDAS (vecinos y hermanos)
        // ==================================================================

        /// <summary>LA VÍCTIMA CANDIDATA: el town NPC CON CASA
        /// (townNPC + homeTileX ≥ 0 + !homeless — «los NPC que viven en
        /// las casas») más cercano a «desde», a ≤ radio, sin contar
        /// «excluir».</summary>
        private static NPC BuscarVecino(Vector2 desde, float radio, int excluir)
        {
            NPC mejor = null;
            float mejorD = radio * radio;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || !n.townNPC) continue;
                if (n.homeTileX < 0 || n.homeless) continue;   // SIN casa NO cuenta
                if (i == excluir) continue;
                float d = Vector2.DistanceSquared(n.Center, desde);
                if (d < mejorD) { mejorD = d; mejor = n; }
            }
            return mejor;
        }

        /// <summary>¿La víctima sigue viva y siendo un vecino?</summary>
        private static NPC NpcValido(int idx)
        {
            if (idx < 0 || idx >= Main.maxNPCs) return null;
            NPC n = Main.npc[idx];
            return n != null && n.active && n.townNPC ? n : null;
        }

        /// <summary>EL HERMANO CLARO de este dueño (el líder de la pareja
        /// de luz — lo usa AethonMenorOscuroPet para su órbita/fusión; el
        /// mini grimorio NO lo usa: él es su propio centro).</summary>
        public static Projectile BuscarClaro(int dueño)
        {
            int tipo = ModContent.ProjectileType<AethonMenorPet>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile pr = Main.projectile[i];
                if (pr != null && pr.active && pr.type == tipo && pr.owner == dueño)
                    return pr;
            }
            return null;
        }
    }
}
