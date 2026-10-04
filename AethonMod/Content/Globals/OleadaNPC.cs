using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.DataStructures;
using AethonMod.Content.VFX;
using AethonMod.Content.Systems;
using AethonMod.Content.Items.Esencias;

namespace AethonMod.Content.Globals
{
    /// <summary>
    /// OleadaNPC — EL SELLO DE LAS OLEADAS DEL GRIMORIO. Todo monstruo o
    /// jefe que el libro hambriento CONVOCA lleva esta marca:
    ///
    /// - v6.50.29 — EL MOTOR NATURAL DE VANILLA (la orden del usuario:
    ///   «investiga cómo funcionan las oleadas reales en terraria y usa
    ///   su mismo sistema»): las lunas de calabaza/escamarcha NO
    ///   engendran a nadie — multiplican el MOTOR NATURAL (spawnRate ×
    ///   0.2, maxSpawns × 2) y le REEMPLAZAN el pool. ESTA clase hace
    ///   EXACTAMENTE eso mientras la furia vive (EditSpawnRate +
    ///   EditSpawnPool): el motor de vanilla elige el tile en el anillo
    ///   0.52–0.7× pantalla del jugador (JUSTO FUERA DEL CUADRO —
    ///   «deberían estar más cerca»), valida suelo/aire/agua/lava (jamás
    ///   nacen en pared — la ruina de la cuna manual) y respeta los
    ///   límites del mundo. OnSpawn sella a los que el motor trae.
    /// - STATS ENFURECIDAS (v6.48, LA LETRA DEL USUARIO): la oleada k
    ///   multiplica la vida Y el ataque por ×(k+1) — la 1 golpea ×2 y la
    ///   10 ×11, monstruos Y jefes; LA OLEADA ESPECIAL (11) los viste a
    ///   ×15. La defensa sigue escalando aparte (chusma +2k, jefes +6k).
    /// - LA PROGRESIÓN ES DE MUERTES (v6.50.29 — como vanilla): cada
    ///   chusma sellada que cae suma su punto en OnKill →
    ///   GrimorioFuriaSistema.PuntoDeMuerte → el indicador y el avance
    ///   de oleada (el waveKills de las lunas de vanilla).
    /// - AGRESIÓN REAL (v6.48, CRECIENTE): la chusma re-objetiva y
    ///   empuja hacia la presa MÁS FUERTE con cada oleada (empuje
    ///   0.16+0.02k, techo 10+0.5k); los JEFES ya no son sagrados:
    ///   re-objetivo cada 20 ticks, los que surcan tiles HOMING hacia la
    ///   presa y los embistes (el lunge de la furia), y TODOS disparan
    ///   sus DIENTES (AtaqueOleadaProjectile — los ataques nuevos de
    ///   librería) con cadencia que crece con la oleada.
    /// - EL AURA (AuraLib, la octava librería): capa trasera ANTES del
    ///   cuerpo (PreDraw) y el VELO frontal al 6% DESPUÉS (PostDraw) —
    ///   gris-blanca, en la 10 gris-negra con bordes rojo oscuro y en la
    ///   ESPECIAL el JUICIO (negra, rojo intenso, chispas carmesí).
    ///   Las partículas corren por Actualizar (AI, no render).
    /// - LA XP DE LA OLEADA: GlobalNPCXP multiplica el cobro por
    ///   MultiplicadorXP() — oleada k paga ×(k+1), la ESPECIAL ×15.
    /// - EL PAGO EN METALES (v6.48): cada monstruo muerto suelta k
    ///   MONEDAS DE ORO y cada jefe de oleada k MONEDAS DE PLATINO (la
    ///   especial: 15) — la furia del libro paga lo que come. Los jefes
    ///   además dejan caer SU ESENCIA (EsenciaDeJefeItem — un nivel
    ///   completo para el libro).
    ///
    /// PROPAGACIÓN: los segmentos que los jefes-gusano y sus sirvientes
    /// engendran a su lado HEREDAN el sello al nacer (OnSpawn) — el
    /// Devorador entero y los Creepers del Cerebro visten el aura y la
    /// furia de su convocador. (Consecuencia querida y documentada: la
    /// marca también salta a los spawns NATURALES que caigan cerca de
    /// una oleada — el hambre del libro es contagiosa, y paga más XP Y
    /// más peligro: el sello es el sello.)
    ///
    /// LOTES (el contrato de la casa en PreDraw/PostDraw): AuraLib vuelca
    /// su lote aditivo cerrando el activo; aquí se REABRE el lote del
    /// sprite de vanilla justo después (Deferred/AlphaBlend/LinearClamp
    /// con la matriz del mundo — el estado del dibujado de NPCs).
    ///
    /// v6.50.48 — LA QUINTA RONDA (la petición literal): «todos esas
    /// formas de ataques extras de los jefes, o sea los proyectiles
    /// brillantes y los tajos que tienen no combinan nada con el jefe»:
    /// LOS DIENTES DE LIBRERÍA MUEREN para los seis guardianes y nacen
    /// LAS COREOGRAFÍAS TEMÁTICAS — cada jefe invoca a LOS SUYOS (los
    /// monstruos y proyectiles ORIGINALES de Terraria): la Abeja Reina
    /// suelta ENJAMBRES de abejas vanilla, el Ojo convoca a sus
    /// Sirvientes, el Rey Gelatina escupe BOLAS DE GEL (el ítem Gel
    /// dibujado como proyectil) al saltar y aterrizar, el Cerebro
    /// ENGENDRA más Creepers + los monstruos del Carmesí, el Devorador
    /// se hace 3 VECES MÁS LARGO + los monstruos de la Corrupción, y
    /// Skeletron invoca esqueletos y lanza HUESOS (el proyectil vanilla
    /// del ítem Hueso). Y LOS TRES FIXES de la oleada: el préstamo de
    /// zona (el Devorador y el Cerebro ya NO SE VAN), el día ya no
    /// encierra a Skeletron en 9999 de defensa, y la BARRA DE VIDA de
    /// los jefes multi-pieza vuelve a caber en su marco.
    /// </summary>
    public class OleadaNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        /// <summary>¿Este NPC fue convocado por la furia del grimorio?</summary>
        public bool EsDeOleada = false;
        /// <summary>El número de oleada que lo convocó (1..11).</summary>
        public int Oleada = 0;
        /// <summary>
        /// v6.50.59 — EL NIVEL DE FURIA del festín que lo convocó (1..10):
        /// el castigo del portador — TODAS las criaturas del festín escalan
        /// vida/daño/defensa/agresión con ÉL, además de la oleada interna.
        /// </summary>
        public int Nivel = 1;
        /// <summary>¿Es la VERSIÓN ESPECIAL del jefe de la oleada?</summary>
        public bool EsJefeDeOleada = false;
        /// <summary>
        /// v6.48 — ¿Es de LA OLEADA ESPECIAL (11 — El Juicio)? Todos los
        /// jefes juntos a ×15: stats, XP y aura del JUICIO.
        /// </summary>
        public bool EsEspecial = false;
        /// <summary>El perfil del aura que viste (null = sin aura).</summary>
        public AuraPerfil Aura = null;

        private int _tickAggro = 0;
        private int _tickAtaque = 0;   // el tempo de los dientes
        private int _tickLunge = 0;    // el tempo de los embites
        private int _tickEscupe = 0;   // v6.50.59 — el tempo del escupitajo del Devorador

        // v6.50.10 — LAS BASES DEL SELLO (idempotencia de Marcar): las
        // stats capturadas en la PRIMERA marca; toda re-marca recalcula
        // desde aquí — nunca encadena multiplicadores (ver Marcar).
        private int _vidaBase = -1;
        private int _danoBase = 0;
        private int _defensaBase = 0;
        private float _kbBase = 1f;

        // v6.50.48 — EL PRE-ESCALADO DEL HOOK DE LA BARRA: si este NPC
        // nació (SetDefaults) mientras la barra de su familia estaba
        // viva, su lifeMax llegó YA multiplicado — Marcar lo DESHACE para
        // no encadenar el multiplicador (la pieza real queda a SU talla;
        // la referencia de la barra, que nunca pasa por Marcar, conserva
        // la escala y el denominador de la barra casa con el numerador).
        private float _preEscala = 1f;

        // v6.50.48 — EL PRÉSTAMO DE ZONA (el fix del jefe que SE VA):
        // el estado del flag original del jugador mientras dura el AI
        // de esta pieza (PreAI lo presta, PostAI lo devuelve).
        private bool _zonaPrestada = false;
        private bool _zonaCrimsonOriginal = false;
        private bool _zonaCorruptOriginal = false;

        // v6.50.48 — EL RITMO DEL REY (la física del salto leída de la
        // velocidad, no de su ai[]): la caída y el aterrizaje del Rey
        // Gelatina escupen las BOLAS DE GEL.
        private float _prevVelY = 0f;
        private bool _prevEnSuelo = false;
        // v6.50.56 — EL TELETRANSPORTE DEL REY (ai[1] de su aiStyle 15:
        // 5 = desvaneciendo, 6 = materializando, 0-3 = ciclo de saltos):
        // leer el ESTADO además de la velocidad — el teletransporte ES su
        // «salto» más firma y la velocidad lo atraviesa sin verlo.
        private float _prevAi1Rey = 0f;

        // ==================================================================
        //  EL SELLADO
        // ==================================================================

        /// <summary>
        /// Marca este NPC como criatura de la oleada k (especial=true →
        /// la 11, EL JUICIO) y aplica TODAS las consecuencias: stats
        /// enfurecidos, aura y knockback resistido. Llamado por
        /// GrimorioFuriaSistema JUSTO DESPUÉS de NPC.NewNPC (OnSpawn
        /// corre DENTRO de NewNPC — todavía no existía la marca).
        /// v6.50.59 — EL NIVEL: el festín de nivel N escala TODO con N
        /// (la oleada interna k sigue escalando también — el castigo se
        /// compone: la oleada 5 de un festín de nivel 5 pega ×6·×1.8).
        /// </summary>
        public void Marcar(NPC npc, int oleada, bool jefe, bool especial = false, int nivel = 1)
        {
            try
            {
                // v6.50.48 — EL DESHACE DEL PRE-ESCALADO: si el hook de la
                // barra ya multiplicó este lifeMax en su nacimiento (la
                // referencia fresca de la familia), las BASES se capturan
                // SIN la escala — la pieza real queda a SU talla exacta y
                // la referencia de la barra conserva la suya.
                float esc = _preEscala > 1f ? _preEscala : 1f;
                _preEscala = 1f;

                // v6.50.10 — IDEMPOTENCIA (EL FIX DE LA CORRUPCIÓN DEL
                // FESTÍN): Marcar podía llegar DOS VECES al mismo NPC.
                // La herencia de OnSpawn corre DENTRO de NPC.NewNPC y
                // sella PRIMERO (el anillo de la chusma nace a 380-700px
                // del portador — casi siempre a <800px de un marcado:
                // hereda el sello); y el convocador (SpawnMonstruo /
                // SpawnJefeOleada / el Juicio) sella DESPUÉS al volver de
                // NewNPC. El cuerpo viejo re-escalaba lifeMax/damage desde
                // los valores YA escalados y re-multiplicaba
                // knockBackResist → ×(k+1)² de vida y daño (×121 en la
                // oleada 10, ×225 en El Juicio) con el knockback resistido
                // al 12%: monstruos inmortales de 4-5 cifras que no se
                // interrumpen — "se corrompe el mundo" durante el festín.
                // AHORA la PRIMERA marca captura las stats BASE y toda
                // re-marca recalcula DESDE ELLAS con el sello FINAL (el
                // convocador manda: su oleada/jefe/especial pisan a la
                // herencia — la cabeza del jefe queda con stats de JEFE,
                // no de chusma contagiada).
                if (!EsDeOleada || _vidaBase < 0)
                {
                    _vidaBase = esc > 1f ? Math.Max(1, (int)(npc.lifeMax / esc)) : npc.lifeMax;
                    _danoBase = npc.damage;
                    _defensaBase = npc.defense;
                    _kbBase = npc.knockBackResist;
                }

                EsDeOleada = true;
                EsEspecial = especial;
                Oleada = especial ? 11 : (oleada < 1 ? 1 : (oleada > 10 ? 10 : oleada));
                EsJefeDeOleada = jefe;
                // v6.50.59 — EL NIVEL DEL FESTÍN (1..10): la marca manda
                // (la herencia y el convocador viajan con él).
                Nivel = Math.Max(1, Math.Min(10, nivel));

                // === STATS: LA LETRA DEL USUARIO (v6.48 + v6.50.59) ===
                // La oleada k: vida Y daño ×(k+1) — la 1 ×2, la 10 ×11,
                // para chusma Y jefes. LA ESPECIAL: ×15.
                // v6.50.59 — EL NIVEL ENCIMA (la letra: «la fuerza, poder,
                // vida y defensa de los monstruos aumenta en consecuencia
                // del nivel de las oleadas»): ×(1 + 0.20·(nivel−1)) — un
                // festín de nivel 5 golpea ×1.8 MÁS, de nivel 10 ×2.8.
                // (v6.50.10: siempre DESDE LAS BASES — jamás encadenado.)
                float mult = MultiplicadorStats;

                int nuevaVida = (int)(_vidaBase * mult);
                if (nuevaVida < 1) nuevaVida = 1;
                npc.lifeMax = nuevaVida;
                npc.life = nuevaVida;
                if (_danoBase > 0)
                    npc.damage = (int)(_danoBase * mult);
                // v6.50.59 — LA DEFENSA ESCALA CON LA OLEADA Y EL NIVEL (la
                // letra: «todos los monstruos o jefes de las oleadas deben
                // ver su defensa aumentada en consecuencia del numero de
                // oleadas»): chusma +2k+3(n−1) · jefes +6k+8(n−1).
                npc.defense = _defensaBase + DefensaExtra(jefe);
                npc.knockBackResist = _kbBase * 0.35f; // la furia no se interrumpe

                // === EL AURA ===
                Aura = AuraPerfil.OleadaGrimorio(Oleada);
                Aura.Radio = RadioSegun(npc, jefe);

                npc.netUpdate = true; // MP: mejor esfuerzo de la casa (SP-first)

                // v6.50.48 — EL CENSO DE LAS PIEZAS (la maquinaria del
                // fix de la barra): cada pieza de multi-jefe (segmentos
                // del Devorador, Creepers del Cerebro) que nace sellada
                // se cuenta — el denominador de la barra de vanilla
                // (vida de una pieza FRESCA × número ASUMIDO de piezas)
                // se recalibra con el multiplicador y el largo REAL.
                if (npc.type == NPCID.EaterofWorldsHead || npc.type == NPCID.EaterofWorldsBody ||
                    npc.type == NPCID.EaterofWorldsTail)
                    GrimorioFuriaSistema.PiezaDevoradorNacio(MultiplicadorStats);
                else if (npc.type == NPCID.Creeper)
                    GrimorioFuriaSistema.PiezaCerebroNacio(MultiplicadorStats);
            }
            catch { EsDeOleada = false; }
        }

        // ==================================================================
        //  v6.50.48 - EL HOOK DE LA BARRA (EL FIX DEL DESBORDE)
        // ==================================================================

        /// <summary>
        /// LA REFERENCIA DE LA BARRA, ESCALADA COMO SUS PIEZAS.
        ///
        /// LA CAUSA (decompile de EaterOfWorldsProgressBar y
        /// BrainOfCthuluBigProgressBar): la barra de vida de vanilla para
        /// los jefes multi-pieza divide la suma de las vidas ACTUALES
        /// (que la oleada multiplica x(k+1)) entre la vida de una pieza
        /// FRESCA (SetDefaults(14/267) cada frame, vida VAINILLA) x un
        /// numero ASUMIDO de piezas. Con la oleada 2 (x3) el relleno ya
        /// se salia del marco; en la 10 (x11) la barra media ONCE
        /// marcos. EL FIX: este hook ve nacer ESA referencia (tML pasa
        /// por los GlobalNPC.SetDefaults de TODO NPC, incluida la
        /// efimera de la barra) y la multiplica por EL MISMO
        /// multiplicador de la oleada y por el FACTOR DE LARGO REAL
        /// (piezas nacidas / piezas asumidas): el denominador vuelve a
        /// casa con el numerador y el relleno NUNCA supera el marco.
        /// Las piezas REALES que nacen mientras la barra vive tambien
        /// pasan por aqui: llegan pre-escaladas y Marcar las deshace
        /// (nunca se encadena el multiplicador).
        /// </summary>
        public override void SetDefaults(NPC npc)
        {
            try
            {
                // EL DEVORADOR: la barra referencia un CUERPO fresco (14)
                // y asume GetEaterOfWorldsSegmentsCount()+2 piezas.
                if (npc.type == NPCID.EaterofWorldsBody)
                {
                    float escala = GrimorioFuriaSistema.EscalaBarraDevorador();
                    if (escala > 1f)
                    {
                        npc.lifeMax = Math.Max(1, (int)(npc.lifeMax * escala));
                        npc.life = npc.lifeMax;
                        _preEscala = escala;
                    }
                }
                // EL CEREBRO: la barra referencia un CREEPER fresco (267)
                // y asume GetBrainOfCthuluCreepersCount() creepers.
                else if (npc.type == NPCID.Creeper)
                {
                    float escala = GrimorioFuriaSistema.EscalaBarraCerebro();
                    if (escala > 1f)
                    {
                        npc.lifeMax = Math.Max(1, (int)(npc.lifeMax * escala));
                        npc.life = npc.lifeMax;
                        _preEscala = escala;
                    }
                }
            }
            catch { _preEscala = 1f; }
        }

        /// <summary>
        /// EL MULTIPLICADOR DE STATS: ×(oleada+1) — la 1 ×2 … la 10 ×11;
        /// la OLEADA ESPECIAL ×15 (la letra del usuario). v6.50.59 — EL
        /// NIVEL ENCIMA: ×(1 + 0.20·(nivel−1)) — la oleada 5 del festín de
        /// nivel 5 pega ×6·×1.8 = ×10.8 (la especial se queda ×15 PLANO:
        /// el Juicio es el Juicio).
        /// </summary>
        public float MultiplicadorStats
        {
            get
            {
                if (EsEspecial) return 15f;
                float porOleada = Oleada + 1f;
                float porNivel = 1f + 0.20f * (Nivel - 1);
                return porOleada * porNivel;
            }
        }

        /// <summary>
        /// v6.50.59 — LA DEFENSA EXTRA DEL SELLO: crece con la OLEADA y con
        /// EL NIVEL (la letra: «todos deben ver su defensa aumentada en
        /// consecuencia del numero de oleadas… y la fuerza, poder, vida y
        /// defensa aumenta en consecuencia del nivel»).
        /// </summary>
        public int DefensaExtra(bool jefe)
        {
            if (EsEspecial) return 6 * 11;              // el Juicio como siempre
            int n = Math.Max(1, Nivel);
            return jefe ? 6 * Oleada + 8 * (n - 1) : 2 * Oleada + 3 * (n - 1);
        }

        /// <summary>
        /// EL MULTIPLICADOR DE XP (lo lee GlobalNPCXP): ×(oleada+nivel) — la
        /// 1 de nivel 1 paga ×2, la 5 de nivel 5 ×10; la ESPECIAL ×15.
        /// </summary>
        public int MultiplicadorXP => EsEspecial ? 15 : Oleada + Math.Max(1, Nivel);

        /// <summary>El radio del aura según el tamaño del bicho (los jefes visten más grande).</summary>
        private static float RadioSegun(NPC npc, bool jefe)
        {
            float baseR = (npc.width + npc.height) * 0.45f + 26f;
            if (jefe) baseR *= 1.35f;
            return baseR;
        }

        /// <summary>
        /// v6.50.29 — EL SELLO EN EL NACIMIENTO (el motor natural trae a la
        /// horda — el sello la viste):
        ///
        /// · MIENTRAS LA CHUSMA ESTÁ EN MARCHA (fase Monstruos): TODO hostil
        ///   no-town no-jefe que nace — el natural del motor reemplazado,
        ///   el de una estatua, el colado — queda sellado como chusma de la
        ///   oleada actual: el festín es del mundo, el mundo entero es
        ///   comida. (Los spawns del motor ya son SOLO del pool de la furia
        ///   para el portador — esto sella también a los que nazcan de otra
        ///   ventana.)
        /// · HERENCIA POR PADRE (cualquier fase): los segmentos que los
        ///   jefes engendran (el Devorador, los Creepers del Cerebro, los
        ///   Sirvientes del Ojo…) heredan el sello DEL PADRE — el festín no
        ///   regala XP ni monedas por las piezas en cascada.
        /// </summary>
        public override void OnSpawn(NPC npc, IEntitySource source)
        {
            try
            {
                if (EsDeOleada) return; // ya sellado por su convocador

                // LA HERENCIA POR PADRE (segmentos de gusano, Creepers,
                // Sirvientes): el sello del que los escupe — v6.50.59:
                // TAMBIÉN EL NIVEL (el castigo se hereda completo).
                if (source is EntitySource_Parent padre && padre.Entity is NPC papi &&
                    papi.active)
                {
                    var selloPapi = papi.GetGlobalNPC<OleadaNPC>();
                    if (selloPapi != null && selloPapi.EsDeOleada)
                    {
                        Marcar(npc, selloPapi.Oleada, selloPapi.EsJefeDeOleada, selloPapi.EsEspecial,
                            selloPapi.Nivel);
                        return;
                    }
                }

                // EL SELLO DE LA FURIA (fase de chusma): todo hostil nuevo
                // es comida del libro (v6.50.59 — al NIVEL del festín).
                if (GrimorioFuriaSistema.ChusmaEnMarcha &&
                    !npc.friendly && !npc.townNPC && !npc.boss && !npc.SpawnedFromStatue)
                {
                    Marcar(npc, GrimorioFuriaSistema.OleadaActual, jefe: false,
                        nivel: GrimorioFuriaSistema.NivelFuriaPublico);
                }
            }
            catch { }
        }

        // ==================================================================
        //  EL VIAJE DEL SELLO POR LA RED (v6.50.1 — flags con el NPC)
        // ==================================================================

        /// <summary>
        /// v6.50.1 — FIX: los flags de oleada viajan con el NPC (el aura del
        /// JUICIO se dibuja en los clientes de MP — antes solo el server los
        /// tenía: los GlobalNPC de instancia NO viajan solos). Corre cuando
        /// el NPC se sincroniza (MessageID.SyncNPC: netUpdate, creación y
        /// jugadores que entran a media oleada). ESCRITURA SIMÉTRICA
        /// EXACTA con ReceiveExtraAI (mismo orden y tipos).
        /// v6.50.2 — LOS STATS ESCALADOS TAMBIÉN CAMINAN: el msg 23 de
        /// vanilla NO lleva damage/defense/lifeMax (solo life, con el bit
        /// life==lifeMax fijando life=lifeMax VAINILLA) — el daño
        /// NPC→jugador se evalúa en el CLIENTE con SU copia → los remotos
        /// recibían daño ×1 (la promesa "la 10 golpea ×11" no existía en
        /// MP). Con el 4º bit se envían los 5 stats EXACTOS del server y
        /// el cliente los ASIGNA (idempotente por construcción: cada sync
        /// re-asigna los mismos valores — nunca se re-multiplican).
        /// </summary>
        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter writer)
        {
            // v6.50.15 — ARMADURA (auditoría R55-c): tML NO envuelve el
            // SendExtraAI (el receive sí lo captura) — una excepción aquí
            // rompería la sincronización del NPC en toda la sesión MP.
            try
            {
                bitWriter.WriteBit(EsDeOleada);
                bitWriter.WriteBit(EsEspecial);
                bitWriter.WriteBit(EsJefeDeOleada);
                bitWriter.WriteBit(EsDeOleada); // lleva stats: solo las bestias del festín
                writer.Write((byte)Oleada);
                writer.Write((byte)Math.Max(1, Nivel)); // v6.50.59 — el nivel del festín
                if (EsDeOleada)
                {
                    // Los stats ESCALADOS de la autoridad (tal cual los tiene).
                    writer.Write(npc.lifeMax);
                    writer.Write(npc.life);
                    writer.Write(npc.damage);
                    writer.Write(npc.defense);
                    writer.Write(npc.knockBackResist);
                }
            }
            catch { }
        }

        /// <summary>
        /// v6.50.1 — FIX (el simétrico de SendExtraAI): el cliente asigna los
        /// flags de instancia con los datos leídos Y reconstruye el aura —
        /// PreDraw/PostDraw leen Aura en el cliente y sin reconstruirla el
        /// sello llegaba pero el JUICIO seguía invisible.
        /// v6.50.2 — ASIGNA los stats escalados (exactos del server): el
        /// handler del msg 23 corre ANTES de este hook (life incluida — el
        /// bit life==lifeMax del vanilla fija una lifeMax SIN escalar: esta
        /// asignación la corrige con el valor real). Asignación, nunca
        /// multiplicación: cada sync deja la copia idéntica a la autoridad.
        /// </summary>
        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader reader)
        {
            // v6.50.15 — ARMADURA (auditoría R55-c): si el paquete llega
            // truncado/corrupto (versión mixta, desconexión a mitad de
            // sync), degrada a "NPC sin festín" en vez de envenenar el
            // estado de la sesión.
            try
            {
                EsDeOleada = bitReader.ReadBit();
                EsEspecial = bitReader.ReadBit();
                EsJefeDeOleada = bitReader.ReadBit();
                bool llevaStats = bitReader.ReadBit();
                Oleada = reader.ReadByte();
                Nivel = Math.Max(1, (int)reader.ReadByte()); // v6.50.59 — el nivel del festín

                if (llevaStats)
                {
                    npc.lifeMax = reader.ReadInt32();
                    npc.life = reader.ReadInt32();
                    npc.damage = reader.ReadInt32();
                    npc.defense = reader.ReadInt32();
                    npc.knockBackResist = reader.ReadSingle();
                }

                if (EsDeOleada && Aura == null)
                {
                    Aura = AuraPerfil.OleadaGrimorio(Oleada);
                    Aura.Radio = RadioSegun(npc, EsJefeDeOleada);
                }
            }
            catch
            {
                EsDeOleada = false;
                EsEspecial = false;
                EsJefeDeOleada = false;
                Oleada = 0;
                Nivel = 1;
                Aura = null;
            }
        }

        // ==================================================================
        //  LA AGRESIÓN (chusma Y jefes — la furia crece con la oleada)
        // ==================================================================

        // ==================================================================
        //  v6.50.27 — LA INMUNIDAD DE DESPAWN (LA CAUSA RAÍZ del reporte
        //  «las oleadas sigue sin funcionar, la activo y no veo enemigos»)
        //  ==================================================================

        // ==================================================================
        //  v6.50.29 — LOS MANDOS DEL MOTOR NATURAL DE VANILLA
        //  (el mismo asiento que usan las lunas de calabaza/escamarcha:
        //  NPC.SpawnNPC consulta estos hooks ANTES de elegir el tile y
        //  la criatura — la furia solo APRIETA el acelerador y cambia
        //  el menú; el MOTOR de vanilla hace TODO el trabajo de siempre:
        //  el anillo 0.52-0.7 pantalla, el suelo, el aire, el agua)
        //  ==================================================================

        /// <summary>
        /// EL ACELERADOR: mientras la chusma está en marcha, el motor
        /// natural del portador se MULTIPLICA como el de las lunas de
        /// vanilla (spawnRate × 0.2 — la furia aprieta MÁS con cada
        /// oleada: la 10 corre a ×0.08, un monstruo cada ~1.2 s por
        /// intento de tick) y el tope de vivos crece con la oleada
        /// (10+4k: la 1 sostiene 14, la 10 sostiene 50 — «cada oleada
        /// salen MÁS enemigos»). Solo el ciclo del PORTADOR (la comida
        /// es suya); los demás jugadores ven el mundo normal.
        /// </summary>
        public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
        {
            try
            {
                if (!GrimorioFuriaSistema.ChusmaEnMarcha) return;
                Player portador = GrimorioFuriaSistema.Portador;
                if (portador == null || player == null || player.whoAmI != portador.whoAmI) return;

                int k = GrimorioFuriaSistema.OleadaActual;
                float mult = GrimorioFuriaSistema.MultiplicadorSpawnRate(k);
                if (spawnRate > 1)
                    spawnRate = Math.Max(1, (int)(spawnRate * mult));
                maxSpawns = Math.Max(maxSpawns, GrimorioFuriaSistema.TopeVivos(k));
            }
            catch { }
        }

        /// <summary>
        /// EL MENÚ: mientras la chusma está en marcha, el pool de spawns
        /// del portador es REEMPLAZADO por el menú del bioma (el mismo
        /// reemplazo que hace la luna de calabaza con sus rutinas — aquí
        /// con el pool ponderado de tModLoader). El motor de vanilla
        /// sigue eligiendo DÓNDE (anillo 0.52-0.7× pantalla: justo fuera
        /// del cuadro, MÁS CERCA que la cuna manual de 1300-1600 px) y
        /// VALIDANDO el tile (suelo/aire/agua/lava — jamás en pared).
        /// </summary>
        public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
        {
            try
            {
                if (!GrimorioFuriaSistema.ChusmaEnMarcha) return;
                Player portador = GrimorioFuriaSistema.Portador;
                if (portador == null || spawnInfo.Player == null ||
                    spawnInfo.Player.whoAmI != portador.whoAmI) return;

                // ==============================================================
                //  v6.50.56 — LA CASA DE LA CHUSMA: ni entre muros ni bajo
                //  tierra (la letra: «algunos enemigos de las oleadas se
                //  siguen generando entre los muros y bajo tierra entre la
                //  tierra»). El motor natural de vanilla puede elegir
                //  bolsas de aire SUBTERRÁNEAS del anillo de spawn (la
                //  cueva sellada bajo tus pies, el hueco entre los muros
                //  de una estructura): mientras el portador esté en la
                //  SUPERFICIE, todo intento de nacimiento en un tile con
                //  MURO de fondo (muro natural = dentro de la tierra;
                //  muro colocado = dentro de una casa) o claramente POR
                //  DEBAJO de sus pies VACÍA el menú — este intento no nace
                //  nada y el motor reintenta en otro tile al próximo tick
                //  (como toda invasión: la chusma llega POR EL AIRE
                //  LIBRE). En el subsuelo (el portador abajo) la cueva es
                //  su casa y el motor sigue como siempre.
                // ==============================================================
                Player pl = spawnInfo.Player;
                bool portadorEnSuperficie = !pl.ZoneDirtLayerHeight && !pl.ZoneRockLayerHeight &&
                                             !pl.ZoneUnderworldHeight && !pl.ZoneDungeon;
                // v6.50.60 — LA CAPA DE TIERRA YA NO ES TIERRA DE NADIE (la
                // letra: «en algunos niveles de la oleada los monstruos
                // siguen apareciendo entre los muros y bajo tierra»): el
                // chequeo de la .56 SOLO corría con el portador en la
                // SUPERFICIE — en cuanto pisaba la capa de tierra
                // (ZoneDirtLayerHeight: la entrada de la cueva, la casa
                // cavada en la loma) la protección moría ENTERA y el motor
                // sembraba bolsas de aire en la roca PROFUNDA. AHORA la
                // capa de tierra tiene SU REGLA: los muros son su casa
                // (cueva es cueva) PERO nada nace MÁS ABAJO de su propio
                // suelo (margen 10 tiles — la chusma nace en TU nivel de
                // cueva, no en bolsas bajo tus pies).
                bool portadorEnTierra = pl.ZoneDirtLayerHeight && !pl.ZoneRockLayerHeight &&
                                         !pl.ZoneUnderworldHeight && !pl.ZoneDungeon;
                if (portadorEnSuperficie || portadorEnTierra)
                {
                    int sx = spawnInfo.SpawnTileX, sy = spawnInfo.SpawnTileY;
                    bool bajoTierra = sy > spawnInfo.PlayerFloorY +
                        (portadorEnSuperficie ? 6 : 10);
                    bool entreMuros = false;
                    if (sx > 5 && sx < Main.maxTilesX - 5 && sy > 5 && sy < Main.maxTilesY - 5)
                    {
                        Tile tl = Main.tile[sx, sy];
                        entreMuros = tl != null && tl.WallType != 0;  // muro natural o colocado = DENTRO de algo
                    }
                    // Superficie: ni muros ni sótano. Capa de tierra: SOLO
                    // se veta lo que nace bajo tu propio suelo (la cueva al
                    // nivel del portador es su casa).
                    if (bajoTierra || (portadorEnSuperficie && entreMuros))
                    {
                        pool.Clear();   // este intento no nace: el motor prueba otro tile
                        return;
                    }
                }

                var menu = GrimorioFuriaSistema.PoolDeOleada;
                if (menu == null || menu.Count == 0) return;

                // EL MENÚ DEL FESTÍN (limpio y PONDERADO — v6.50.59: el
                // bioma del portador pesa EL DOBLE que los acompañantes del
                // nivel): el motor solo escupe la comida del libro mientras
                // la oleada viva — y el menú CAMBIA EN VIVO cuando el
                // portador se muda de bioma (GrimorioFuriaSistema reconstruye
                // el pool cada medio segundo).
                pool.Clear();
                foreach (var par in menu)
                    pool[par.Key] = par.Value;
            }
            catch { }
        }

        /// <summary>
        /// EL SELLO NO SE APAGA SOLO: NPC.CheckActive desactiva a cualquier
        /// hostil que pase 750 ticks (12,5 s) fuera del rectángulo
        /// pantalla+tamaño de TODOS los jugadores (verificado en el
        /// decompile: timeLeft-- hasta 0 → active=false). La cuna del
        /// festín nace FUERA de ese rectángulo por diseño («fuera de
        /// pantalla»): la chusma despertaba condenada — los limos saltaban
        /// tras la colina durante 12,5 segundos y DESAPARECÍAN sin que el
        /// jugador viera UNO. Mientras el festín vive, los convocados por
        /// el libro son del EVENTO: viven hasta que los maten o la furia
        /// termine. (Terminado el festín, este hook devuelve true y los
        /// sobrevivientes se apagan por el camino de vanilla.)
        /// </summary>
        public override bool CheckActive(NPC npc)
        {
            try
            {
                if (EsDeOleada && GrimorioFuriaSistema.Activo) return false;
            }
            catch { }
            return true;
        }

        /// <summary>
        /// PreAI v6.50.48 - EL PRESTAMO DE ZONA (el fix de los dos jefes
        /// que SE VAN) + el ritmo del Rey.
        ///
        /// LA CAUSA (decompile): la IA vanilla del Cerebro (aiStyle 26)
        /// sube al jefe hacia el cielo y lo hace FANTASMA (alpha 10)
        /// cuando su presa NO esta en el Carmesi, y la IA del Devorador
        /// (aiStyle 6) MATA toda la cadena si NINGUN jugador vive en la
        /// Corrupcion. La oleada los convoca donde ESTE el portador (la
        /// letra del sistema: el guardian del bioma del festin) - y a los
        /// dos ticks de nacer, el jefe SE IBA («son invocados pero se
        /// van»). EL FIX: durante el AI de esta pieza, la presa lleva
        /// PRESTADA la zona de su guardián (Carmesí para el Cerebro,
        /// Corrupción para el Devorador): la IA de vanilla lee SU bioma,
        /// el jefe se queda. PostAI devuelve el flag original al
        /// jugador (el prestamo dura lo que dura UN AI de esta pieza).
        /// </summary>
        public override bool PreAI(NPC npc)
        {
            try
            {
                if (EsDeOleada && EsJefeDeOleada && _zonaPrestada == false &&
                    (npc.type == NPCID.BrainofCthulhu || npc.type == NPCID.EaterofWorldsHead) &&
                    npc.target >= 0 && npc.target < Main.maxPlayers)
                {
                    Player presa = Main.player[npc.target];
                    if (presa != null && presa.active && !presa.dead)
                    {
                        _zonaCrimsonOriginal = presa.ZoneCrimson;
                        _zonaCorruptOriginal = presa.ZoneCorrupt;
                        if (npc.type == NPCID.BrainofCthulhu) presa.ZoneCrimson = true;
                        else presa.ZoneCorrupt = true;
                        _zonaPrestada = true;
                    }
                }
            }
            catch { }

            if (EsDeOleada && !npc.boss)
            {
                _tickAggro++;
                // v6.50.59 — MÁS AGRESIVA (la letra: «todos deben ser mas
                // agresivos»): la chusma re-objetiva cada 20 t (antes 30 —
                // se distraían con cualquier cosa).
                if (_tickAggro >= 20)
                {
                    _tickAggro = 0;
                    npc.TargetClosest(false);
                }
            }
            return true;
        }

        /// <summary>
        /// PostAI (después de la AI de vanilla): LA AGRESIÓN v6.48 —
        ///
        /// · La CHUSMA: empuje hacia la presa más fuerte con cada oleada
        ///   (0.16+0.02k, techo 10+0.5k). Gusanos (aiStyle 6) exentos
        ///   (la física de segmentos se rompe).
        /// · Los JEFES (ya no sagrados): re-objetivo cada 20 ticks; los
        ///   que surcan tiles HOMING suave (la furia los pega a la
        ///   presa) + EL EMBITE (lunge periódico que crece con la
        ///   oleada); y TODOS escupen SUS DIENTES — los ataques nuevos
        ///   de librería (AtaqueOleadaProjectile) con cadencia
        ///   300−18k (tope 60; especial 50).
        ///
        /// Las PARTÍCULAS del aura corren aquí para todos (chusma Y
        /// jefes — en AI, no en render).
        /// </summary>
        public override void PostAI(NPC npc)
        {
            try
            {
                // v6.50.48 - LA DEVOLUCION DEL PRESTAMO (lo primero de
                // todo: el AI de esta pieza ya corrio; el jugador recupera
                // SU zona. El prestamo dura exactamente un AI).
                if (_zonaPrestada)
                {
                    _zonaPrestada = false;
                    if (npc.target >= 0 && npc.target < Main.maxPlayers)
                    {
                        Player pl = Main.player[npc.target];
                        if (pl != null && pl.active)
                        {
                            pl.ZoneCrimson = _zonaCrimsonOriginal;
                            pl.ZoneCorrupt = _zonaCorruptOriginal;
                        }
                    }
                }

                // v6.50.48 - EL DIA YA NO ENCOLERA A SKELETRON (el fix de
                // «esqueletron tiene una defensa muy alta aunque eso solo
                // pasa a veces, solo le hago 1 de daño»). LA CAUSA
                // (decompile de su aiStyle 11): cuando Main.IsItDay(), su
                // IA lo viste de damage 1000 / defense 9999 (el modo
                // guardian) - y la oleada lo convoca de DIA a proposito
                // (la paridad Rey/Skeletron de JefeDelLugar). De noche el
                // daño era normal y de dia UNO: «solo pasa a veces». EL
                // FIX: despues de su AI, la oleada lo devuelve a SU talla
                // (las bases de su sello) - el giro visual queda (le queda
                // bien), los numeros no.
                if (EsDeOleada && EsJefeDeOleada && npc.type == NPCID.SkeletronHead && Main.dayTime)
                {
                    npc.defense = _defensaBase + DefensaExtra(true);
                    if (_danoBase > 0)
                        npc.damage = (int)(_danoBase * MultiplicadorStats);
                }

                // v6.50.48 - EL RITMO DEL REY (la coreografia del salto):
                // el Rey Gelatina de la oleada escupe BOLAS DE GEL al
                // despegar y AL ATERRIZAR (la fisica leida de la velocidad,
                // no de su ai[]): la caida veloz que se frena de golpe es
                // el instante del impacto.
                // v6.50.56 — CON CADA SALTO, DE VERDAD (la letra: «no esta
                // usando el item Gel como proyectil tipo salpicadura con
                // cada salto»): (a) las bolas YA ERAN INVISIBLES (el crash
                // del dibujado del gel — AtaqueOleadaProjectile, curado
                // esta versión: dibujaban SIN lote abierto); (b) EL
                // TELETRANSPORTE TAMBIÉN SALPICA (ai[1] 5→6→0 — el "salto"
                // más firma del Rey); (c) solo la AUTORIDAD dispara (SP y
                // server — antes un cliente MP disparaba bolas fantasma).
                if (EsDeOleada && EsJefeDeOleada && npc.type == NPCID.KingSlime)
                {
                    // LA POLARIDAD DE TERRARIA: caer es velocity.Y POSITIVA
                    // (abajo = +Y). EL ATERRIZAJE: venía cayendo fuerte y la
                    // velocidad COLAPSÓ (el suelo la frenó). EL DESPEGUE: del
                    // reposo al salto violento hacia arriba (-Y).
                    bool veniaCayendo = _prevVelY >= 7f;
                    bool aterrizo = veniaCayendo && npc.velocity.Y < _prevVelY * 0.3f;
                    bool despego = npc.velocity.Y < -5f && _prevVelY > -5f;
                    // EL TELETRANSPORTE: entra en el estado 6 (materializa
                    // en el NUEVO sitio) — el salpicón de la llegada.
                    bool aparece = npc.ai[1] == 6f && _prevAi1Rey != 6f;
                    // …y el 5 (desvanece) se despide del viejo con 4 bolas.
                    bool seVa = npc.ai[1] == 5f && _prevAi1Rey != 5f;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        if (aterrizo && !_prevEnSuelo) ReyAterriza(npc);
                        if (despego) ReyDespega(npc);
                        if (aparece) ReyAparece(npc);
                        if (seVa) ReyDespega(npc);
                    }
                    _prevVelY = npc.velocity.Y;
                    _prevEnSuelo = aterrizo;
                    _prevAi1Rey = npc.ai[1];
                }

                // las partículas del aura: SIEMPRE (también los jefes de
                // la oleada visten chispas)
                if (Aura != null)
                    AuraLib.Actualizar(npc, Aura);

                if (!EsDeOleada) return;

                Player presa = Main.player[npc.target];
                bool presaValida = presa != null && presa.active && !presa.dead;

                // === LA CHUSMA: el empuje creciente ===
                if (!npc.boss)
                {
                    if (npc.aiStyle == 6) return; // gusanos: física sagrada
                    if (!presaValida) return;
                    Vector2 dir = presa.Center - npc.Center;
                    float d = dir.Length();

                    // v6.50.29 — LA RE-CUNA MURIÓ: el motor natural de
                    // vanilla ya nace a la chusma EN el anillo 0.52-0.7×
                    // pantalla (justo fuera del cuadro) — no hay "perdidos
                    // a 2300 px" que re-llamar: nacen CERCA y el empuje de
                    // abajo los trae.
                    // v6.50.26 — LA CARGA DE LA FURIA NO SE APAGA A 1500 PX:
                    // los monstruos nacen FUERA DE PANTALLA (la semidiagonal
                    // real + 200 px ≈ 1300-1550 px) y el viejo radio los
                    // dejaba PARADOS en su cuna — con la IA pasiva de los
                    // limos de día, la oleada entera se quedaba saltando
                    // tras la colina y el festín leía como «no salen
                    // enemigos». El hambre los trae CORRIENDO desde donde
                    // nazcan (el libro los llama: vienen).
                    // v6.50.59 — MÁS AGRESIVA AÚN (la letra: «todos deben
                    // ser mas agresivos»): el empuje base sube (0.20→0.26,
                    // +0.026 por oleada y +0.02 por NIVEL) y el TECHO de
                    // velocidad también (11→13, +0.6k, +0.4(n−1)) — la
                    // chusma de nivel 10 CORRE a 19 px/t: la calle es suya.
                    if (d > 4600f || d < 1f) return;
                    int nCh = Math.Max(1, Nivel);
                    npc.velocity += dir / d * (0.26f + 0.026f * Oleada + 0.02f * (nCh - 1));
                    float techo = 13f + 0.6f * Oleada + 0.4f * (nCh - 1);
                    float vel = npc.velocity.Length();
                    if (vel > techo)
                        npc.velocity = npc.velocity * (techo / vel);
                    return;
                }

                // === LOS JEFES: la furia de verdad ===
                if (!EsJefeDeOleada) return; // solo los convocados por el libro

                // EL RE-OBJETIVO (v6.50.59 — cada 12 t: nunca se distraen,
                // la letra: «haz que la IA de los jefes de oleada en la
                // oleada sea mas agresiva»).
                _tickAggro++;
                if (_tickAggro >= 12)
                {
                    _tickAggro = 0;
                    npc.TargetClosest(false);
                    presa = Main.player[npc.target];
                    presaValida = presa != null && presa.active && !presa.dead;
                }

                // EL HOMING + EL EMBITE: solo los que surcan tiles (los
                // demás ya persiguen por su cuenta — su AI usa el suelo).
                // v6.50.59 — MÁS HAMBRIENTOS: homing 0.09+0.012k+0.01(n−1)
                // (antes 0.05+0.008k — se dejaban llevar), el EMBITE más
                // frecuente (260−26k−12(n−1), tope 70) y más FUERTE
                // (2.6+0.42k+0.2(n−1)).
                if (presaValida && npc.noTileCollide)
                {
                    Vector2 dir = presa.Center - npc.Center;
                    float d = dir.Length();
                    int nJ = Math.Max(1, Nivel);
                    if (d > 60f && d < 2200f)
                        npc.velocity += dir / d * (0.09f + 0.012f * Oleada + 0.01f * (nJ - 1));

                    _tickLunge++;
                    int cadenciaLunge = EsEspecial ? 70
                        : Math.Max(70, 260 - 26 * Oleada - 12 * (nJ - 1));
                    if (_tickLunge >= cadenciaLunge)
                    {
                        _tickLunge = 0;
                        Vector2 embite = (presa.Center - npc.Center).SafeNormalize(Vector2.Zero);
                        npc.velocity += embite * (2.6f + 0.42f * Oleada + 0.2f * (nJ - 1));
                    }
                }

                // v6.50.59 — EL DEVORADOR ESCUPE (la letra: «el jefe
                // devorador de mundo en la oleada debe lanzar sus propios
                // proyectiles estos son los mismos mosntruos de su bioma,
                // el jefe los escupira cada vez que este delante del
                // jugador»): cada vez que la CABEZA pasa DELANTE de la
                // presa (cerca y volando hacia ella), escupe 2-3 monstruos
                // de la Corrupción DESDE SU BOCA — proyectiles vivos que
                // salen disparados hacia el jugador. Cadencia corta (80 t
                // + la ración de la coreografía): el gusano-barra
                // AMETRALLA comida.
                if (presaValida && npc.type == NPCID.EaterofWorldsHead)
                {
                    _tickEscupe++;
                    Vector2 rumboPresa = (presa.Center - npc.Center);
                    float dPresa = rumboPresa.Length();
                    Vector2 velNorm = npc.velocity.LengthSquared() > 0.01f
                        ? Vector2.Normalize(npc.velocity) : Vector2.UnitX;
                    bool delante = dPresa < 520f && dPresa > 40f &&
                        Vector2.Dot(velNorm, rumboPresa / dPresa) > 0.35f;
                    if (_tickEscupe >= 80 && delante && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        _tickEscupe = 0;
                        EscupirDevorador(npc, presa, 2 + (Oleada >= 5 ? 1 : 0));
                    }
                }

                // v6.50.48 - LA COREOGRAFIA DEL GUARDIAN: los ataques
                // de libreria (proyectiles brillantes y tajos) MURIERON
                // («no combinan nada con el jefe»); ahora cada guardián
                // convoca a LOS SUYOS con la misma cadencia de siempre
                // (la 10 y la especial, sin pausa).
                // v6.50.59 — LA CADENCIA SE APRIETA (la letra: «mas
                // agresivos»): 240−24k−10(n−1) con tope 40 (antes
                // 300−18k con tope 60 — casi el DOBLE de frecuente).
                if (presaValida)
                {
                    _tickAtaque++;
                    int nCo = Math.Max(1, Nivel);
                    int cadencia = EsEspecial ? 36
                        : Math.Max(40, 240 - 24 * Oleada - 10 * (nCo - 1));
                    if (_tickAtaque >= cadencia)
                    {
                        _tickAtaque = 0;
                        Coreografia(npc, presa);
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// v6.50.60 — LA CUNA LIMPIA (la letra: «en algunos niveles de la
        /// oleada los monstruos siguen apareciendo entre los muros y bajo
        /// tierra»). LA CAUSA: las convocaciones MANUALES (el escupitajo
        /// del Devorador, los limos del Rey, los anillos del Ojo/Cerebro/
        /// Skeletron) nacían en la posición CRUDA — y el Devorador VIVE
        /// DENTRO del terreno: sus escupidos nacían ENTRE MUROS y BAJO
        /// TIERRA (por eso era «en algunos niveles»: solo en las oleadas
        /// cuyo guardián es el gusano). LA CURA: TODO monstruo convocado
        /// por la furia nace en AIRE DE VERDAD — la cuna se valida
        /// (hueco 3×3 sin sólidos) y, si el portador está en la
        /// superficie, SIN MURO y SOBRE su suelo; si la posición cruda no
        /// sirve, se busca la más cercana caminando HACIA la presa (y
        /// luego hacia arriba — la boca del gusano escupe AL AIRE que
        /// respira su comida); la red final es el aire de la propia
        /// presa. En el subsuelo los muros son la casa (se permiten) —
        /// pero el hueco de aire es ley SIEMPRE.
        /// v6.50.61 — INTERNAL: el VIGÍA del festín (GrimorioFuriaSistema)
        /// reutiliza ESTA MISMA cuna para sus nacimientos forzosos — una
        /// sola definición de "posición limpia" en toda la casa (el
        /// parámetro jefe jamás se leyó: el vigía lo pasa null).
        /// </summary>
        internal static Vector2 PosicionLimpia(NPC jefe, Player presa, Vector2 cruda)
        {
            try
            {
                // ¿El portador está AL AIRE LIBRE? (encima de la capa de
                // tierra): los convocados nacen SIN MURO y SOBRE su suelo.
                bool portadorArriba = presa == null ||
                    (!presa.ZoneDirtLayerHeight && !presa.ZoneRockLayerHeight &&
                     !presa.ZoneUnderworldHeight && !presa.ZoneDungeon);
                int sueloPortador = presa != null
                    ? (int)(presa.Bottom.Y + 8f) / 16 : int.MaxValue;

                // EL RUMBO DE LA BÚSQUEDA: hacia la presa (el aire que
                // respira la comida) — y si nada sirve, hacia arriba.
                Vector2 haciaPresa = presa != null
                    ? (presa.Center - cruda).SafeNormalize(Vector2.UnitX)
                    : -Vector2.UnitY;

                // 12 CANDIDATOS: la cruda · acercándose a la presa (6
                // pasos de 28 px) · subiendo desde el rumbo (6 pasos de
                // 32 px — la superficie está arriba).
                for (int i = 0; i < 12; i++)
                {
                    Vector2 cand = i < 6
                        ? cruda + haciaPresa * (28f * i)
                        : cruda + haciaPresa * 140f + new Vector2(0f, -32f * (i - 5));
                    int cx = (int)(cand.X / 16f), cy = (int)(cand.Y / 16f);
                    if (cx < 5 || cx >= Main.maxTilesX - 5 ||
                        cy < 5 || cy >= Main.maxTilesY - 5) continue;

                    // EL HUECO: 3×3 de aire (la criatura NECESITA cuerpo).
                    bool hueco = true;
                    for (int tx = cx - 1; tx <= cx + 1 && hueco; tx++)
                        for (int ty = cy - 1; ty <= cy + 1; ty++)
                        {
                            Tile t = Main.tile[tx, ty];
                            if (t != null && t.HasTile && Main.tileSolid[t.TileType])
                            { hueco = false; break; }
                        }
                    if (!hueco) continue;

                    if (portadorArriba)
                    {
                        Tile tl = Main.tile[cx, cy];
                        if (tl != null && tl.WallType != 0) continue;   // ENTRE MUROS: no
                        if (cy > sueloPortador + 5) continue;            // BAJO TIERRA: no
                    }
                    return cand;   // LIMPIA: aquí nace
                }

                // LA RED: el aire de la propia presa (donde está la comida
                // SIEMPRE hay aire de verdad).
                if (presa != null && presa.active)
                    return presa.Center + new Vector2(Main.rand.NextFloat(-70f, 70f), -50f);
                return cruda;
            }
            catch { return cruda; }
        }

        /// <summary>
        /// v6.50.59 — EL ESCUPITajo DEL DEVORADOR: los monstruos de SU
        /// bioma (la Corrupción) salen DISPARADOS de su boca hacia la
        /// presa — son sus proyectiles vivos (nacen de la boca con
        /// velocidad heredada, como todo escupitajo que se respete).
        /// v6.50.60 — LA BOCA SE LIMPIA: el gusano escupe desde DENTRO de
        /// la tierra — la cuna pasa por PosicionLimpia (el escupitajo
        /// sale AL AIRE, camino de su comida, jamás dentro del muro).
        /// </summary>
        private void EscupirDevorador(NPC npc, Player presa, int cuantos)
        {
            try
            {
                bool subsuelo = npc.Center.Y > Main.worldSurface * 16f + 320f;
                Vector2 rumbo = (presa.Center - npc.Center).SafeNormalize(Vector2.UnitX);
                Vector2 boca = PosicionLimpia(npc, presa, npc.Center + rumbo * 30f);
                for (int i = 0; i < cuantos; i++)
                {
                    int tipo = subsuelo
                        ? (Main.rand.NextBool() ? NPCID.DevourerHead : NPCID.CorruptSlime)
                        : (Main.rand.NextBool() ? NPCID.EaterofSouls : NPCID.CorruptSlime);
                    Vector2 vel = rumbo.RotatedBy(Main.rand.NextFloat(-0.45f, 0.45f)) *
                        Main.rand.NextFloat(7f, 11f);
                    Convocar(npc, tipo, boca + new Vector2(Main.rand.NextFloat(-18f, 18f), 0f), vel.X, vel.Y);
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, npc.Center); // el escupitajo
            }
            catch { }
        }

        // ==================================================================
        //  v6.50.48 - LAS COREOGRAFIAS: EL ATAQUE DE CADA GUARDIAN ES
        //  CONVOCAR A LOS SUYOS
        //
        //  La letra del usuario: «todos esas formas de ataques extras
        //  de los jefes, o sea los proyectiles brillantes y los tajos
        //  que tienen no combinan nada con el jefe». Cada guardián
        //  invoca a los MONSTRUOS y PROYECTILES ORIGINALES de su mundo
        //  (la guía de IDs verificada contra el decompile):
        //
        //  · REY GELATINA: invoca MUCHOS limos (BlueSlime) y con cada
        //    salto escupe BOLAS DE GEL (el ítem Gel dibujado como
        //    proyectil, gravedad y rebote) al despegar y AL ATERRIZAR
        //    una lluvia de bolas hacia TODAS las direcciones al azar.
        //  · OJO DE CTHULHU: invoca SIRVIENTES DEL OJO (los monstruos
        //    originales, NPCID 5) en anillo alrededor de la presa -
        //    ellos hacen el resto (su IA vanilla ya embiste).
        //  · ABEJA REINA: el ENJAMBRE (abejas gigantes del arma de
        //    abejas vanilla, proyectil 566) volando alrededor de la
        //    reina + andanadas de abejas (566/181) lanzadas desde un
        //    anillo alrededor de la presa + su AGUIJON original (719).
        //  · CEREBRO: mas CREEPERS (los que vuelan a su alrededor, 267)
        //    + los monstruos de su bioma (Carmesí: cara monstruo /
        //    trepador de sangre / crimera).
        //  · DEVORADOR: los monstruos de su bioma (Corrupción: devorador
        //    de almas / gusano devorador / limo corrupto) y la cadena
        //    3 veces mas larga la arma GrimorioFuriaSistema.
        //  · SKELETRON: invoca ESQUELETOS (el monstruo original, 21) y
        //    lanza HUESOS (el proyectil vanilla del ítem Hueso, 21)
        //    en tres figuras: abanico, anillo convergente y lluvia.
        //  · DEERCLOPS conserva sus látigos de escarcha (le quedan).
        // ==================================================================

        /// <summary>El daño que llevan los proyectiles hostiles de la coreografía (la talla de la oleada).</summary>
        private int DanoRacion => Math.Max(1, (int)(16f * MultiplicadorStats));

        /// <summary>
        /// SIRVE LA RACION del guardián: LOS SUYOS. Solo la autoridad
        /// (server / SP) - la casa MP de siempre.
        /// </summary>
        private void Coreografia(NPC npc, Player presa)
        {
            try
            {
                if (Main.netMode == NetmodeID.MultiplayerClient) return;
                switch (npc.type)
                {
                    case NPCID.KingSlime: CoroRey(npc, presa); break;
                    case NPCID.EyeofCthulhu: CoroOjo(npc, presa); break;
                    case NPCID.QueenBee: CoroAbeja(npc, presa); break;
                    case NPCID.BrainofCthulhu: CoroCerebro(npc, presa); break;
                    case NPCID.EaterofWorldsHead:
                    case NPCID.EaterofWorldsBody:
                    case NPCID.EaterofWorldsTail: CoroDevorador(npc, presa); break;
                    case NPCID.SkeletronHead: CoroSkeletron(npc, presa); break;
                    case NPCID.Deerclops:
                        // El invierno caminante conserva sus latigos: le
                        // quedan (la escarcha SI es su tema).
                        LatigosDeerclops(npc, presa);
                        break;
                }
            }
            catch { }
        }

        /// <summary>
        /// CONVOCAR UN MONSTRUO de la coreografia: nace como hijo del
        /// guardián (hereda el sello y sus stats) pero pasa a cobrar
        /// como CHUSMA (oro y puntos, no platino): son los INVITADOS
        /// del festín, no la cabeza de mesa.
        /// </summary>
        private static NPC Convocar(NPC jefe, int tipo, Vector2 pos, float vx = 0f, float vy = 0f)
        {
            int idx = NPC.NewNPC(jefe.GetSource_FromAI(), (int)pos.X, (int)pos.Y, tipo, 0, vx, vy, 0f, 0f);
            if (idx < 0 || idx >= Main.maxNPCs) return null;
            NPC n = Main.npc[idx];
            if (n == null || !n.active) return null;
            var sello = n.GetGlobalNPC<OleadaNPC>();
            if (sello != null && sello.EsDeOleada) sello.EsJefeDeOleada = false; // chusma: oro, no platino
            n.netUpdate = true;
            return n;
        }

        /// <summary>
        /// LANZA UN PROYECTIL VANILLA HOSTIL (la letra: los proyectiles
        /// ORIGINALES de Terraria - abejas, aguijones, huesos). Los de
        /// la casa vienen friendly de fábrica: se voltean aquí.
        /// </summary>
        private static Projectile LanzarHostil(NPC jefe, int tipo, Vector2 pos, Vector2 vel, int danio, float kb = 2f)
        {
            int idx = Projectile.NewProjectile(jefe.GetSource_FromAI(), pos, vel, tipo,
                danio, kb, Main.myPlayer);
            if (idx < 0 || idx >= Main.maxProjectiles) return null;
            Projectile p = Main.projectile[idx];
            if (p == null || !p.active) return null;
            if (p.friendly) { p.friendly = false; p.hostile = true; }
            if (p.timeLeft < 540) p.timeLeft = 540;
            p.netUpdate = true;
            return p;
        }

        // ==================================================================
        //  EL REY GELATINA - MUCHOS LIMOS + LAS BOLAS DE GEL DEL SALTO
        // ==================================================================

        /// <summary>
        /// LA RACION DEL REY: muchos, PERO MUCHOS limos — y v6.50.56 nacen
        /// TODOS DESDE EL CUERPO DEL REY (la letra: «los slime debe
        /// aparecer desde el rey slime», como las bolas SlimeSpawn del Rey
        /// vanilla): escupidos de su masa en abanico hacia la presa, con
        /// su arco al aire — NUNCA MÁS el anillo en el cielo alrededor del
        /// jugador.
        /// </summary>
        private void CoroRey(NPC npc, Player presa)
        {
            int cantidad = 5 + Oleada / 2;                 // 5..10 (especial: 12)
            if (EsEspecial) cantidad = 12;
            float rumbo = (presa.Center - npc.Center).SafeNormalize(Vector2.UnitY).ToRotation();
            float paso = cantidad > 1 ? 1f / (cantidad - 1) : 0.5f;
            for (int i = 0; i < cantidad; i++)
            {
                // EL ABANICO: ±60° alrededor del rumbo a la presa.
                float ang = rumbo + ((i * paso) - 0.5f) * (MathHelper.Pi * 2f / 3f);
                Vector2 vel = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) *
                    Main.rand.NextFloat(4f, 8f);
                vel.Y -= 3f;   // el escupitajo se alza (nace del cuerpo y vuela)
                // v6.50.60 — LA CUNA LIMPIA: el Rey puede saltar sobre un
                // tejado o una colina — el limo nace en AIRE de verdad.
                Convocar(npc, NPCID.BlueSlime,
                    PosicionLimpia(npc, presa, npc.Center + new Vector2(0f, -12f)), vel.X, vel.Y);
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, npc.Center);
        }

        /// <summary>EL DESPEGUE DEL REY: cuatro bolas de gel hacia atras y abajo.</summary>
        private void ReyDespega(NPC npc)
        {
            for (int i = 0; i < 4; i++)
            {
                float ang = MathHelper.PiOver2 + Main.rand.NextFloat(-0.9f, 0.9f); // abajo, abierto
                if (npc.direction < 0) ang = MathHelper.Pi - ang;
                Vector2 vel = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * Main.rand.NextFloat(5f, 9f);
                LanzarBolaGel(npc, npc.Center + new Vector2(0f, 20f), vel);
            }
        }

        /// <summary>
        /// EL ATERRIZAJE DEL REY (la letra: «al caer muchas de esas bolas
        /// salpican del jefe hacia todas las direcciones de forma
        /// aleatoria»): DOCE bolas de gel al azar + TRES limos mas de
        /// rebote (naciendo DE su cuerpo, v6.50.56).
        /// </summary>
        private void ReyAterriza(NPC npc)
        {
            // v6.50.60 — LA CUNA LIMPIA también aquí: la presa del Rey
            // valida la cuna de sus limos de rebote.
            Player presa = (npc.target >= 0 && npc.target < Main.maxPlayers)
                ? Main.player[npc.target] : null;
            for (int i = 0; i < 12; i++)
            {
                float ang = Main.rand.NextFloat(MathHelper.TwoPi);       // TODAS las direcciones
                float rap = Main.rand.NextFloat(4f, 10f);                 // al azar
                Vector2 vel = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * rap;
                vel.Y -= 2.5f;                                            // el salpicón se alza
                LanzarBolaGel(npc, npc.Center + new Vector2(0f, 30f), vel);
            }
            for (int i = 0; i < 3; i++)
                Convocar(npc, NPCID.BlueSlime,
                    PosicionLimpia(npc, presa, npc.Center + new Vector2(Main.rand.NextFloat(-120f, 120f), -12f)),
                    Main.rand.NextFloat(-3f, 3f), -4f);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, npc.Center);
        }

        /// <summary>
        /// v6.50.56 — LA LLEGADA DEL TELETRANSPORTE (ai[1] 5→6): el cuerpo
        /// recién materializado SALPICA su gel — el «salto» más firma del
        /// Rey Gelatina también paga en bolas (la letra: gel «con cada
        /// salto»).
        /// </summary>
        private void ReyAparece(NPC npc)
        {
            for (int i = 0; i < 6; i++)
            {
                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 vel = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) *
                    Main.rand.NextFloat(3f, 7f);
                vel.Y -= 2f;
                LanzarBolaGel(npc, npc.Center, vel);
            }
        }

        /// <summary>LA BOLA DE GEL (el ítem Gel de Terraria dibujado como proyectil con gravedad y rebote).</summary>
        private void LanzarBolaGel(NPC npc, Vector2 pos, Vector2 vel)
        {
            int idx = Projectile.NewProjectile(npc.GetSource_FromAI(), pos, vel,
                ModContent.ProjectileType<Projectiles.Oleadas.AtaqueOleadaProjectile>(),
                DanoRacion, 2f, Main.myPlayer,
                Projectiles.Oleadas.AtaqueOleadaProjectile.EstiloBolaGel, 0f, Main.rand.Next(9973));
            if (idx < 0 || idx >= Main.maxProjectiles) return;
            Projectile p = Main.projectile[idx];
            if (p == null) return;
            p.tileCollide = true;   // la bola es física: cae, rebota y salpica
            p.netUpdate = true;
        }

        // ==================================================================
        //  EL OJO - LOS SIRVIENTES EN ANILLO (los ojos pequeños que ya
        //  existen como monstruos, la letra del usuario)
        // ==================================================================

        private void CoroOjo(NPC npc, Player presa)
        {
            int cantidad = 4 + Math.Min(4, 1 + Oleada / 3);      // 5..8 (especial 9)
            if (EsEspecial) cantidad = 9;
            for (int i = 0; i < cantidad; i++)
            {
                float ang = i * MathHelper.TwoPi / cantidad;
                Vector2 pos = presa.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 300f;
                pos.Y -= 60f;
                // v6.50.60 — LA CUNA LIMPIA: el anillo vive en el aire de
                // la presa — pero un flanco puede caer en una colina
                // maciza: se valida IGUAL (el sirviente nace en aire).
                Convocar(npc, NPCID.ServantofCthulhu, PosicionLimpia(npc, presa, pos), MathF.Cos(ang) * 4f, -2f);
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Zombie104, npc.Center); // el nido abriendo
        }

        // ==================================================================
        //  LA ABEJA REINA - EL ENJAMBRE + LAS ANDANADAS + EL AGUIJON
        // ==================================================================

        private void CoroAbeja(NPC npc, Player presa)
        {
            // (1) EL ENJAMBRE: abejas gigantes alrededor de la REINA -
            // vuelan a su alrededor (la IA vanilla de las abejas las
            // pegara a su colmena viviente).
            int enjambre = 6 + Math.Min(4, 1 + Oleada / 3);
            if (EsEspecial) enjambre = 12;
            for (int i = 0; i < enjambre; i++)
            {
                float ang = i * MathHelper.TwoPi / enjambre;
                Vector2 pos = npc.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.6f) * 90f;
                // tangencial: nacen girando alrededor de ella
                Vector2 vel = new Vector2(-MathF.Sin(ang), MathF.Cos(ang)) * 6f;
                LanzarHostil(npc, i % 3 == 0 ? ProjectileID.GiantBee : ProjectileID.Bee, pos, vel, DanoRacion);
            }

            // (2) LA ANDANADA: abejas desde un anillo alrededor de la
            // PRESA, rumbo a la reina - la nube barre la posición del
            // jugador de camino a su colmena.
            int andanada = 6 + Math.Min(4, 1 + Oleada / 3);
            if (EsEspecial) andanada = 10;
            Vector2 rumbo = (npc.Center - presa.Center).SafeNormalize(Vector2.UnitY);
            for (int i = 0; i < andanada; i++)
            {
                float ang = i * MathHelper.TwoPi / andanada;
                Vector2 pos = presa.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 240f;
                Vector2 vel = rumbo.RotatedBy((i % 2 == 0 ? 1f : -1f) * 0.35f) * Main.rand.NextFloat(8f, 11f);
                LanzarHostil(npc, i % 2 == 0 ? ProjectileID.GiantBee : ProjectileID.Bee, pos, vel, DanoRacion);
            }

            // (3) EL AGUIJON ORIGINAL: su arma de fábrica, en abanico.
            Vector2 dir = (presa.Center - npc.Center).SafeNormalize(Vector2.UnitY);
            for (int i = -1; i <= 1; i++)
                LanzarHostil(npc, ProjectileID.QueenBeeStinger, npc.Center, dir.RotatedBy(i * 0.14f) * 10f, DanoRacion);

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Zombie104, npc.Center);
        }

        // ==================================================================
        //  EL CEREBRO - MAS CREEPERS + LOS MONSTRUOS DEL CARMESI
        // ==================================================================

        private void CoroCerebro(NPC npc, Player presa)
        {
            // (1) MAS DE ESAS COSAS QUE VUELAN A SU ALREDEDOR: creepers
            // extra alrededor del cerebro (la letra del usuario).
            int extra = 6 + Math.Min(4, 1 + Oleada / 3);
            if (EsEspecial) extra = 12;
            for (int i = 0; i < extra; i++)
            {
                float ang = i * MathHelper.TwoPi / extra;
                Vector2 pos = npc.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.5f) * 130f;
                Convocar(npc, NPCID.Creeper, PosicionLimpia(npc, presa, pos), MathF.Cos(ang) * 3f, -1f);
            }

            // (2) LOS MONSTRUOS DE SU BIOMA: el Carmesí según la depth.
            bool subsuelo = npc.Center.Y > Main.worldSurface * 16f + 320f;
            int cantidad = 2 + Math.Min(3, 1 + Oleada / 4);
            for (int i = 0; i < cantidad; i++)
            {
                int tipo = subsuelo
                    ? (Main.rand.NextBool() ? NPCID.FaceMonster : NPCID.BloodCrawler)
                    : NPCID.Crimera;
                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 pos = npc.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 220f;
                pos.Y -= 40f;
                Convocar(npc, tipo, PosicionLimpia(npc, presa, pos), Main.rand.NextFloat(-2f, 2f), -2f);
            }
        }

        // ==================================================================
        //  EL DEVORADOR - LOS MONSTRUOS DE SU BIOMA, ESCUPIDOS DE LA BOCA
        //  (v6.50.59 — la letra: «el jefe devorador de mundo en la oleada
        //  debe lanzar sus propios proyectiles estos son los mismos
        //  mosntruos de su bioma, el jefe los escupira cada vez que este
        //  delante del jugador» — ya no un anillo lejano: la ración sale
        //  DISPARADA de su boca hacia la presa; la cadena 3 veces más
        //  larga la arma GrimorioFuriaSistema)
        // ==================================================================

        private void CoroDevorador(NPC npc, Player presa)
        {
            // LA RACIÓN DE LA BOCA: los monstruos de la Corrupción salen
            // escupidos hacia la presa (sus proyectiles vivos) — el abanico
            // del escupitajo se abre rumbo al jugador.
            // v6.50.60 — LA BOCA SE LIMPIA (el gusano escupe desde dentro
            // de la tierra: la cuna pasa por PosicionLimpia — el abanico
            // sale AL AIRE, jamás entre los muros).
            bool subsuelo = npc.Center.Y > Main.worldSurface * 16f + 320f;
            int cantidad = 3 + Math.Min(3, 1 + Oleada / 4);
            Vector2 rumbo = (presa.Center - npc.Center).SafeNormalize(Vector2.UnitX);
            Vector2 boca = PosicionLimpia(npc, presa, npc.Center + rumbo * 30f);
            for (int i = 0; i < cantidad; i++)
            {
                int tipo = subsuelo
                    ? (Main.rand.NextBool() ? NPCID.DevourerHead : NPCID.CorruptSlime)
                    : (Main.rand.NextBool() ? NPCID.EaterofSouls : NPCID.CorruptSlime);
                Vector2 vel = rumbo.RotatedBy((i - (cantidad - 1) * 0.5f) * 0.16f) *
                    Main.rand.NextFloat(7f, 11f);
                Convocar(npc, tipo, boca + new Vector2(Main.rand.NextFloat(-18f, 18f), 0f), vel.X, vel.Y);
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, npc.Center);
        }

        // ==================================================================
        //  SKELETRON - LOS ESQUELETOS + LAS CALAVERAS DEL LIBRO POTENCIADAS
        //  (v6.50.57 — el proyectil del Libro de las Calaveras, gigante,
        //   veloz, brillante y con estela — el hueso simple MURIÓ)
        // ==================================================================

        private void CoroSkeletron(NPC npc, Player presa)
        {
            // (1) LOS ESQUELETOS: los monstruos originales, en anillo.
            int cantidad = 3 + Math.Min(3, 1 + Oleada / 3);
            for (int i = 0; i < cantidad; i++)
            {
                float ang = i * MathHelper.TwoPi / cantidad;
                Vector2 pos = presa.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 280f;
                // v6.50.60 — LA CUNA LIMPIA: el anillo de la presa, validado
                // (un flanco en la colina ya no se traga al esqueleto).
                Convocar(npc, NPCID.Skeleton, PosicionLimpia(npc, presa, pos), MathF.Cos(ang) * 3f, -2f);
            }

            // (2) v6.50.57 — LAS CALAVERAS DEL LIBRO, POTENCIADAS (la
            //     letra: «el jefe esqueleto en la oleada debe lanzar los
            //     proyectiles del libro de las calavera, pero esos
            //     proyectiles deben estar potenciados de alguna forma»):
            //     EL PROYECTIL 837 (BookOfSkullsSkull — EL del LIBRO DE
            //     LAS CALAVERAS, aiStyle 1: frena y VUELVE a acelerar)
            //     GIGANTE (escala ×2.1+), VELOZ (extraUpdates), BRILLANTE
            //     (luz 0.9) y con ESTELA DE FUEGO DORADO-VIOLETA (el
            //     GlobalProjectile CalaveraPotenciadaFX la pinta) — en
            //     las TRES figuras de siempre que rotan por ración.
            int danoCala = Math.Max(1, (int)(DanoRacion * 1.35f));
            float escalaCala = Math.Min(2.8f, 2.1f + Oleada * 0.06f);
            int figura = (int)((Main.GameUpdateCount / 60u) % 3u);
            if (figura == 0)
            {
                // EL ABANICO: cinco calaveras en arco hacia la presa.
                Vector2 dir = (presa.Center - npc.Center).SafeNormalize(Vector2.UnitY);
                for (int i = -2; i <= 2; i++)
                {
                    Vector2 vel = dir.RotatedBy(i * 0.18f) * Main.rand.NextFloat(8f, 11f);
                    vel.Y -= 3f;                                   // el arco de las calaveras
                    CalaveraPotenciada(npc, npc.Center, vel, danoCala, escalaCala);
                }
            }
            else if (figura == 1)
            {
                // EL ANILLO CONVERGENTE: ocho calaveras naciendo alrededor
                // de la presa y volando hacia ella.
                for (int i = 0; i < 8; i++)
                {
                    float ang = i * MathHelper.TwoPi / 8f;
                    Vector2 pos = presa.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.7f) * 300f;
                    Vector2 vel = (presa.Center - pos).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(9f, 12f);
                    vel.Y -= 3.5f;
                    CalaveraPotenciada(npc, pos, vel, danoCala, escalaCala);
                }
            }
            else
            {
                // LA LLUVIA: calaveras del cielo sobre la presa.
                for (int i = 0; i < 6; i++)
                {
                    Vector2 pos = presa.Center + new Vector2(Main.rand.NextFloat(-260f, 260f), -430f);
                    Vector2 vel = new Vector2(Main.rand.NextFloat(-2.5f, 2.5f), Main.rand.NextFloat(7f, 10f));
                    CalaveraPotenciada(npc, pos, vel, danoCala, escalaCala);
                }
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, npc.Center);
        }

        /// <summary>
        /// v6.50.57 — LA CALAVERA DEL LIBRO, POTENCIADA (la letra: «esos
        /// proyectiles deben estar potenciados de alguna forma»): el
        /// proyectil vanilla del LIBRO DE LAS CALAVERAS (BookOfSkullsSkull,
        /// 837 — verificado en el decompile: aiStyle 1, 26×26, fade-in de 5
        /// t, 3 frames de animación) GIGANTE (×2.1+ con la oleada, hitbox
        /// honesta de 50×50), VELOZ (extraUpdates 1 — el doble de rápida),
        /// BRILLANTE (luz 0.9) y con estela de fuego DORADO-VIOLETA (la
        /// pinta el GlobalProjectile CalaveraPotenciadaFX — solo las
        /// hostiles: las del jugador quedan como siempre).
        /// </summary>
        private static void CalaveraPotenciada(NPC jefe, Vector2 pos, Vector2 vel,
            int danio, float escala)
        {
            Projectile p = LanzarHostil(jefe, ProjectileID.BookOfSkullsSkull, pos, vel, danio);
            if (p == null) return;
            p.scale = escala;
            p.extraUpdates = 1;        // vanilla 0 — la potenciada VUELA al doble
            p.light = 0.9f;            // arde
            p.Resize(50, 50);          // la talla honesta del gigante dibujado
            p.netUpdate = true;
        }

        // ==================================================================
        //  DEERCLOPS (invitado de la .30) - SUS LATIGOS DE ESCARCHA
        // ==================================================================

        private void LatigosDeerclops(NPC npc, Player presa)
        {
            for (int i = 0; i < 3; i++)
            {
                Vector2 pos = presa.Center + new Vector2(
                    (i - 1) * 130f + Main.rand.NextFloat(-40f, 40f), -420f);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(npc.GetSource_FromAI(), pos, new Vector2(0f, 4f),
                        ModContent.ProjectileType<Projectiles.Oleadas.AtaqueOleadaProjectile>(),
                        DanoRacion, 2f, Main.myPlayer,
                        Projectiles.Oleadas.AtaqueOleadaProjectile.EstiloDeerclops, 0f,
                        Oleada * 11 + i);
                }
            }
        }
        // ==================================================================
        //  LA MUERTE PAGA — monedas y esencias
        // ==================================================================

        /// <summary>
        /// v6.48 — EL PAGO EN METALES Y ESENCIAS: cada monstruo de la
        /// oleada k suelta k MONEDAS DE ORO; cada JEFE de la oleada k
        /// MONEDAS DE PLATINO + SU ESENCIA (la especial: 15). El festín
        /// del libro paga en los dos metales.
        /// </summary>
        public override void OnKill(NPC npc)
        {
            if (!EsDeOleada) return;
            // v6.50.29 — EL PUNTO DE MUERTE (el corazón de la progresión
            // de vanilla): cada chusma sellada que cae suma SU punto — el
            // waveKills de las lunas de calabaza/escamarcha. Al cruzar el
            // umbral de la oleada, el guardián nace (GrimorioFuriaSistema.
            // FaseMonstruos). Las piezas en cascada y los jefes NO pagan
            // puntos: los puntos son de la CHUSMA (el jefe cierra la
            // oleada — su muerte la cierra, no la empuja).
            if (!EsJefeDeOleada && !Content.Systems.ShardLevelSystem.EsParteDeJefe(npc))
                GrimorioFuriaSistema.PuntoDeMuerte(1);
            // v6.50.10 — FIX: LAS PARTES EN CASCADA NO COBRAN. Los
            // segmentos de gusano heredan el sello por propagación
            // (diseño: "segmentos de gusano, Creepers…") y caían aquí:
            // ~80 cuerpos del EoW soltando k monedas CADA UNO (800 de
            // oro por derrota en la oleada 10) — una derrota, UN cobro
            // (la cabeza es quien paga, como en la XP v6.46).
            if (Content.Systems.ShardLevelSystem.EsParteDeJefe(npc)) return;
            try
            {
                // v6.50.59 — EL PAGO DEL NIVEL: el festín de nivel N paga
                // k+(N−1) monedas (la furia que crece paga lo que cuesta).
                int monedas = EsEspecial ? 15 : Oleada + Math.Max(0, Nivel - 1);
                var src = npc.GetSource_Loot();

                if (EsJefeDeOleada)
                {
                    // EL PLATINO DEL GUARDIÁN (k monedas — la 10: 10).
                    if (monedas > 0)
                        Item.NewItem(src, npc.Center, ItemID.PlatinumCoin, monedas);

                    // SU ESENCIA (un alma por guardián — el ítem que sube
                    // un nivel COMPLETO al libro).
                    int esencia = EsenciaDeJefeItem.DeNPC(npc.type);
                    if (esencia > 0)
                        Item.NewItem(src, npc.Center, esencia, 1);
                }
                else
                {
                    // EL ORO DE LA CHUSMA (k monedas — la 10: 10).
                    if (monedas > 0)
                        Item.NewItem(src, npc.Center, ItemID.GoldCoin, monedas);
                }
            }
            catch { }
        }

        // ==================================================================
        //  EL RENDER DEL AURA (PreDraw → trasera · PostDraw → velo)
        // ==================================================================

        /// <summary>
        /// La CAPA TRASERA del aura: se dibuja ANTES del sprite del NPC —
        /// el cuerpo TAPA el humo (la profundidad del look). AuraLib vuelca
        /// su lote aditivo cerrando el activo; se REABRE el lote del sprite
        /// de vanilla SOLO si se cerró de verdad (el bool del contrato).
        /// </summary>
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Aura == null) return true;
            try
            {
                if (AuraLib.DibujarNPC(npc, Aura, frontal: false))
                    AuraLib.ReabrirLoteVanilla();
            }
            catch
            {
                try { AuraLib.ReabrirLoteVanilla(); } catch { }
            }
            return true;
        }

        /// <summary>
        /// El VELO FRONTAL (la transparencia del 94%): la MISMA geometría
        /// pisando al cuerpo DESPUÉS del sprite — la criatura emite desde
        /// dentro. PostDraw es el último paso del dibujado de ESTE NPC: el
        /// lote se reabre para que el siguiente NPC dibuje normal.
        /// </summary>
        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Aura == null) return;
            try
            {
                if (AuraLib.DibujarNPC(npc, Aura, frontal: true))
                    AuraLib.ReabrirLoteVanilla();
            }
            catch
            {
                try { AuraLib.ReabrirLoteVanilla(); } catch { }
            }
        }
    }
}
