using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
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

        /// <summary>El reloj del AFK: 3600 t quieto (~1 min) = «se aburrió».</summary>
        private const int TICKS_AFK = 3600;

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
                if (e.Modo == MODO_VIAJAR || e.Modo == MODO_MOLESTAR)
                {
                    e.Modo = MODO_VOLVER;
                    e.NpcObjetivo = -1;
                    e.Embestida = -1;
                    e.TicksEnModo = 0;
                }
            }
            else
            {
                e.TicksQuieto++;
            }

            switch (e.Modo)
            {
                case MODO_NORMAL:
                    // EL DUEÑO LLEVA ~1 MIN QUIETO: busca vecino CON CASA
                    // (escaneo barato cada 60 t — no hace falta más).
                    if (e.TicksQuieto >= TICKS_AFK && e.TicksQuieto % 60 == 0)
                    {
                        NPC vecino = BuscarVecino(dueño.Center, RADIO_VECINO, -1);
                        if (vecino != null)
                        {
                            e.Modo = MODO_VIAJAR;
                            e.NpcObjetivo = vecino.whoAmI;
                            e.Golpes = 0;
                            e.TicksEnModo = 0;
                            e.TicksQuieto = 0;    // al volver a casa esperará OTRO minuto entero
                            e.TicksMordisco = 0;
                            e.FormaSombra = true; // llega con la FORMA 1 (la sombrilla)
                            e.Embestida = -1;
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
                    if (Vector2.DistanceSquared(p.Center, dueño.Center) < 90f * 90f)
                    {
                        e.Modo = MODO_NORMAL;
                        e.TicksEnModo = 0;
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
            // El reloj del AFK vuelve a cero: entre misión y misión, el
            // mini grimorio espera SIEMPRE un minuto entero de quietud —
            // así el bucle «molesta → vuelve → descansa → molesta» respira. (El
            // ANCLA NO se toca: sigue midiéndose contra la quietud REAL
            // del dueño — si se movió durante la misión, el próximo tick
            // lo detecta y aborta como manda la letra.)
            e.TicksQuieto = 0;
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

            PosicionOjos(p, e, victima, out Vector2[] ojos, out bool[] cerrados);

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

            // === LOS 3 MINI OJOS (idea 15): los minions del grimorio en
            //     miniatura — discos con iris violeta y pupila. Orbitan a
            //     la PAREJA completa (radio ~58 px, 120° exactos entre
            //     hermanos) y cada uno PARPADEA a su compás. (El iris
            //     brillante va al lote aditivo del cuerpo.) ===
            for (int i = 0; i < 3; i++)
            {
                if (cerrados[i])
                {
                    // EL PÁRPADO CERRADO: una rayita oscura en vez del ojo.
                    VFXCore.Line(ojos[i] - new Vector2(4f, 0f), ojos[i] + new Vector2(4f, 0f),
                        SombrasLib.Alfa(SombrasLib.HumoNegro, 0.65f), 1.6f);
                    continue;
                }
                // LA ESCLERÓTICA oscura (el disco) + LA PUPILA negra.
                VFXCore.Quad(ojos[i], SombrasLib.Alfa(new Color(26, 12, 40), 0.85f), new Vector2(9f, 9f));
                VFXCore.Quad(ojos[i], SombrasLib.Alfa(new Color(8, 3, 12), 0.95f), new Vector2(3.6f, 3.6f));
            }

            VFXCore.FlushAlpha();
        }

        // ==================================================================
        //  EL DIBUJADO — LOTE ADITIVO (el anillo rúnico oscuro y los iris)
        // ==================================================================

        /// <summary>LOS EXTRAS DEL MINI GRIMORIO, LOTE ADITIVO: EL ANILLO
        /// RÚNICO OSCURO (violeta, orbitando al libro) y EL IRIS VIOLETA
        /// de los 3 mini ojos. NO vuelca nada: se suma al lote del cuerpo
        /// del mini grimorio (un solo volcado — la llaman justo antes de
        /// su FlushAdditive).</summary>
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

            // === EL IRIS VIOLETA de los 3 mini ojos (el brillo — el
            //     disco negro vive en el lote alfa) ===
            NPC victima = e.Modo == MODO_MOLESTAR || e.Modo == MODO_VIAJAR
                ? NpcValido(e.NpcObjetivo) : null;
            PosicionOjos(p, e, victima, out Vector2[] ojos, out bool[] cerrados);
            for (int i = 0; i < 3; i++)
            {
                if (cerrados[i]) continue;
                VFXCore.Quad(ojos[i], new Color(118, 74, 190, 255) * 0.55f, new Vector2(5.2f, 5.2f));
                VFXCore.Quad(ojos[i] + new Vector2(-1.2f, -1.2f),
                    new Color(230, 210, 255, 255) * 0.45f, new Vector2(1.6f, 1.6f));
            }
        }

        // ==================================================================
        //  LA POSICIÓN DE LOS 3 MINI OJOS (compartida por ambos lotes)
        // ==================================================================

        /// <summary>Dónde está cada ojo y si está parpadeando — TODO
        /// determinista (el reloj del mundo): todas las pantallas ven los
        /// MISMOS ojos. En FORMA 2 los ojos DEJAN la órbita y embisten al
        /// vecino (ida ~40 t + vuelta ~40 t, cada hermano sale 5 t después
        /// que el anterior — el primero toca en el tick 40 exacto, donde
        /// el cerebro cobra el golpe).</summary>
        private static void PosicionOjos(Projectile p, Estado e, NPC victima,
            out Vector2[] pos, out bool[] cerrados)
        {
            pos = new Vector2[3];
            cerrados = new bool[3];
            float t = Main.GlobalTimeWrappedHourly;

            // v6.50.122 — EL CENTRO ES EL MINI GRIMORIO MISMO (antes era
            // el punto medio de la pareja de luz: la máquina cambió de
            // dueño — los mini ojos orbitan AL LIBRO).
            Vector2 centro = p.Center;

            for (int i = 0; i < 3; i++)
            {
                float ang = t * 0.75f + i * MathHelper.TwoPi / 3f;   // 120° exactos
                pos[i] = centro + new Vector2(MathF.Cos(ang) * 58f, MathF.Sin(ang) * 58f * 0.82f);

                // EL PARPADEO PROPIO de cada hermano.
                float parp = (t * 1.15f + i * 2.61f) % 6.5f;
                cerrados[i] = parp < 0.30f;
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
                        cerrados[i] = false;   // embistiendo NO parpadea: mirada fija
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
