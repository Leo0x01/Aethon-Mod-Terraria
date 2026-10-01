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

        // ==================================================================
        //  EL SELLADO
        // ==================================================================

        /// <summary>
        /// Marca este NPC como criatura de la oleada k (especial=true →
        /// la 11, EL JUICIO) y aplica TODAS las consecuencias: stats
        /// enfurecidos, aura y knockback resistido. Llamado por
        /// GrimorioFuriaSistema JUSTO DESPUÉS de NPC.NewNPC (OnSpawn
        /// corre DENTRO de NewNPC — todavía no existía la marca).
        /// </summary>
        public void Marcar(NPC npc, int oleada, bool jefe, bool especial = false)
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

                // === STATS: LA LETRA DEL USUARIO (v6.48) ===
                // La oleada k: vida Y daño ×(k+1) — la 1 ×2, la 10 ×11,
                // para chusma Y jefes. LA ESPECIAL: ×15.
                // (v6.50.10: siempre DESDE LAS BASES — jamás encadenado.)
                float mult = MultiplicadorStats;

                int nuevaVida = (int)(_vidaBase * mult);
                if (nuevaVida < 1) nuevaVida = 1;
                npc.lifeMax = nuevaVida;
                npc.life = nuevaVida;
                if (_danoBase > 0)
                    npc.damage = (int)(_danoBase * mult);
                npc.defense = _defensaBase + (jefe ? 6 * Oleada : 2 * Oleada);
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
        /// la OLEADA ESPECIAL ×15 (la letra del usuario).
        /// </summary>
        public float MultiplicadorStats => EsEspecial ? 15f : Oleada + 1f;

        /// <summary>
        /// EL MULTIPLICADOR DE XP (lo lee GlobalNPCXP): ×(oleada+1) — la
        /// 1 paga ×2 … la 10 ×11; la ESPECIAL ×15.
        /// </summary>
        public int MultiplicadorXP => EsEspecial ? 15 : Oleada + 1;

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
                // Sirvientes): el sello del que los escupe.
                if (source is EntitySource_Parent padre && padre.Entity is NPC papi &&
                    papi.active)
                {
                    var selloPapi = papi.GetGlobalNPC<OleadaNPC>();
                    if (selloPapi != null && selloPapi.EsDeOleada)
                    {
                        Marcar(npc, selloPapi.Oleada, selloPapi.EsJefeDeOleada, selloPapi.EsEspecial);
                        return;
                    }
                }

                // EL SELLO DE LA FURIA (fase de chusma): todo hostil nuevo
                // es comida del libro.
                if (GrimorioFuriaSistema.ChusmaEnMarcha &&
                    !npc.friendly && !npc.townNPC && !npc.boss && !npc.SpawnedFromStatue)
                {
                    Marcar(npc, GrimorioFuriaSistema.OleadaActual, jefe: false);
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

                int[] menu = GrimorioFuriaSistema.PoolDeOleada;
                if (menu == null || menu.Length == 0) return;

                // EL MENÚ DEL FESTÍN (limpio y ponderado): el motor solo
                // escupe la comida del libro mientras la oleada viva.
                pool.Clear();
                for (int i = 0; i < menu.Length; i++)
                    if (menu[i] > 0)
                        pool[menu[i]] = 1f;
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
                if (_tickAggro >= 30)
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
                    npc.defense = _defensaBase + 6 * Oleada;
                    if (_danoBase > 0)
                        npc.damage = (int)(_danoBase * MultiplicadorStats);
                }

                // v6.50.48 - EL RITMO DEL REY (la coreografia del salto):
                // el Rey Gelatina de la oleada escupe BOLAS DE GEL al
                // despegar y AL ATERRIZAR (la fisica leida de la velocidad,
                // no de su ai[]): la caida veloz que se frena de golpe es
                // el instante del impacto.
                if (EsDeOleada && EsJefeDeOleada && npc.type == NPCID.KingSlime)
                {
                    // LA POLARIDAD DE TERRARIA: caer es velocity.Y POSITIVA
                    // (abajo = +Y). EL ATERRIZAJE: venía cayendo fuerte y la
                    // velocidad COLAPSÓ (el suelo la frenó). EL DESPEGUE: del
                    // reposo al salto violento hacia arriba (-Y).
                    bool veniaCayendo = _prevVelY >= 7f;
                    bool aterrizo = veniaCayendo && npc.velocity.Y < _prevVelY * 0.3f;
                    bool despego = npc.velocity.Y < -5f && _prevVelY > -5f;
                    if (aterrizo && !_prevEnSuelo) ReyAterriza(npc);
                    if (despego) ReyDespega(npc);
                    _prevVelY = npc.velocity.Y;
                    _prevEnSuelo = aterrizo;
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
                    // v6.50.27 — el empuje sube (0.16→0.20): con la
                    // inmunidad de CheckActive ahora SÍ llegan — que lleguen
                    // PRONTO.
                    if (d > 4600f || d < 1f) return;
                    npc.velocity += dir / d * (0.20f + 0.02f * Oleada);
                    float techo = 11f + 0.5f * Oleada;
                    float vel = npc.velocity.Length();
                    if (vel > techo)
                        npc.velocity = npc.velocity * (techo / vel);
                    return;
                }

                // === LOS JEFES: la furia de verdad ===
                if (!EsJefeDeOleada) return; // solo los convocados por el libro

                // EL RE-OBJETIVO (cada 20 ticks — nunca se distraen).
                _tickAggro++;
                if (_tickAggro >= 20)
                {
                    _tickAggro = 0;
                    npc.TargetClosest(false);
                    presa = Main.player[npc.target];
                    presaValida = presa != null && presa.active && !presa.dead;
                }

                // EL HOMING + EL EMBITE: solo los que surcan tiles (los
                // demás ya persiguen por su cuenta — su AI usa el suelo).
                if (presaValida && npc.noTileCollide)
                {
                    Vector2 dir = presa.Center - npc.Center;
                    float d = dir.Length();
                    if (d > 60f && d < 2200f)
                        npc.velocity += dir / d * (0.05f + 0.008f * Oleada);

                    _tickLunge++;
                    int cadenciaLunge = EsEspecial ? 90 : Math.Max(120, 300 - 18 * Oleada);
                    if (_tickLunge >= cadenciaLunge)
                    {
                        _tickLunge = 0;
                        Vector2 embite = (presa.Center - npc.Center).SafeNormalize(Vector2.Zero);
                        npc.velocity += embite * (2f + 0.35f * Oleada);
                    }
                }

                // v6.50.48 - LA COREOGRAFIA DEL GUARDIAN: los ataques
                // de libreria (proyectiles brillantes y tajos) MURIERON
                // («no combinan nada con el jefe»); ahora cada guardián
                // convoca a LOS SUYOS con la misma cadencia de siempre
                // (la 10 y la especial, sin pausa).
                if (presaValida)
                {
                    _tickAtaque++;
                    int cadencia = EsEspecial ? 50 : Math.Max(60, 300 - 18 * Oleada);
                    if (_tickAtaque >= cadencia)
                    {
                        _tickAtaque = 0;
                        Coreografia(npc, presa);
                    }
                }
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

        /// <summary>LA RACION DEL REY: muchos, PERO MUCHOS limos alrededor de la presa.</summary>
        private void CoroRey(NPC npc, Player presa)
        {
            int cantidad = 5 + Oleada / 2;                 // 5..10 (especial: 12)
            if (EsEspecial) cantidad = 12;
            for (int i = 0; i < cantidad; i++)
            {
                float ang = i * MathHelper.TwoPi / cantidad + Main.rand.NextFloat(-0.2f, 0.2f);
                Vector2 pos = presa.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 260f;
                Convocar(npc, NPCID.BlueSlime, pos, Main.rand.NextFloat(-2f, 2f), -2f);
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
        /// rebote.
        /// </summary>
        private void ReyAterriza(NPC npc)
        {
            for (int i = 0; i < 12; i++)
            {
                float ang = Main.rand.NextFloat(MathHelper.TwoPi);       // TODAS las direcciones
                float rap = Main.rand.NextFloat(4f, 10f);                 // al azar
                Vector2 vel = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * rap;
                vel.Y -= 2.5f;                                            // el salpicón se alza
                LanzarBolaGel(npc, npc.Center + new Vector2(0f, 30f), vel);
            }
            for (int i = 0; i < 3; i++)
                Convocar(npc, NPCID.BlueSlime, npc.Center + new Vector2(Main.rand.NextFloat(-160f, 160f), -20f),
                    Main.rand.NextFloat(-3f, 3f), -4f);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, npc.Center);
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
                Convocar(npc, NPCID.ServantofCthulhu, pos, MathF.Cos(ang) * 4f, -2f);
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
                Convocar(npc, NPCID.Creeper, pos, MathF.Cos(ang) * 3f, -1f);
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
                Convocar(npc, tipo, pos, Main.rand.NextFloat(-2f, 2f), -2f);
            }
        }

        // ==================================================================
        //  EL DEVORADOR - LOS MONSTRUOS DE LA CORRUPCION (la cadena la
        //  arma GrimorioFuriaSistema: 3 veces mas larga)
        // ==================================================================

        private void CoroDevorador(NPC npc, Player presa)
        {
            bool subsuelo = npc.Center.Y > Main.worldSurface * 16f + 320f;
            int cantidad = 2 + Math.Min(3, 1 + Oleada / 4);
            for (int i = 0; i < cantidad; i++)
            {
                int tipo = subsuelo
                    ? (Main.rand.NextBool() ? NPCID.DevourerHead : NPCID.CorruptSlime)
                    : NPCID.EaterofSouls;
                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 pos = npc.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 240f;
                Convocar(npc, tipo, pos, Main.rand.NextFloat(-2f, 2f), -2f);
            }
        }

        // ==================================================================
        //  SKELETRON - LOS ESQUELETOS + LOS HUESOS (el proyectil vanilla)
        // ==================================================================

        private void CoroSkeletron(NPC npc, Player presa)
        {
            // (1) LOS ESQUELETOS: los monstruos originales, en anillo.
            int cantidad = 3 + Math.Min(3, 1 + Oleada / 3);
            for (int i = 0; i < cantidad; i++)
            {
                float ang = i * MathHelper.TwoPi / cantidad;
                Vector2 pos = presa.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 280f;
                Convocar(npc, NPCID.Skeleton, pos, MathF.Cos(ang) * 3f, -2f);
            }

            // (2) LOS HUESOS: el proyectil del ítem Hueso (vanilla, el
            // abanico/arco/lluvia de toda la vida) en TRES figuras que
            // rotan por ración.
            int figura = (int)((Main.GameUpdateCount / 60u) % 3u);
            if (figura == 0)
            {
                // EL ABANICO: cinco huesos en arco hacia la presa.
                Vector2 dir = (presa.Center - npc.Center).SafeNormalize(Vector2.UnitY);
                for (int i = -2; i <= 2; i++)
                {
                    Vector2 vel = dir.RotatedBy(i * 0.18f) * Main.rand.NextFloat(8f, 11f);
                    vel.Y -= 3f;                                   // el arco de los huesos
                    LanzarHostil(npc, ProjectileID.Bone, npc.Center, vel, DanoRacion);
                }
            }
            else if (figura == 1)
            {
                // EL ANILLO CONVERGENTE: ocho huesos naciendo alrededor
                // de la presa y volando hacia ella.
                for (int i = 0; i < 8; i++)
                {
                    float ang = i * MathHelper.TwoPi / 8f;
                    Vector2 pos = presa.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.7f) * 300f;
                    Vector2 vel = (presa.Center - pos).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(9f, 12f);
                    vel.Y -= 3.5f;
                    LanzarHostil(npc, ProjectileID.Bone, pos, vel, DanoRacion);
                }
            }
            else
            {
                // LA LLUVIA: huesos del cielo sobre la presa.
                for (int i = 0; i < 6; i++)
                {
                    Vector2 pos = presa.Center + new Vector2(Main.rand.NextFloat(-260f, 260f), -430f);
                    Vector2 vel = new Vector2(Main.rand.NextFloat(-2.5f, 2.5f), Main.rand.NextFloat(7f, 10f));
                    LanzarHostil(npc, ProjectileID.Bone, pos, vel, DanoRacion);
                }
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, npc.Center);
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
                int monedas = EsEspecial ? 15 : Oleada;
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
