using System.Collections.Generic;
using Terraria.ID;

using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// Sistema central de niveles del fragmento. Funciones estáticas para
    /// otorgar XP, consultar tablas, y manejar eventos por hito de nivel.
    ///
    /// v6.45 — LA XP ES REAL:
    /// - Mobs: la rareza del BESTIARIO de Terraria (sus estrellas 0–5)
    ///   mide cuánta XP vale cada criatura. Fuente:
    ///   ContentSamples.NpcBestiaryRarityStars, la MISMA tabla oficial que
    ///   el bestiario usa para dibujar el rango de estrellas de cada
    ///   entrada.
    /// - Jefes: la mayor XP de todas (tabla propia 5.000–100.000).
    ///
    /// v6.46 — LA DIETA DEL GRIMORIO, AFINADA:
    /// - MOBS: 5×estrellas² · ×3 la PRIMERA kill de cada especie (leída
    ///   del tracker de kills del PROPIO bestiario vanilla:
    ///   NPCLoot registra la kill ANTES de llamar a NPCLoader.OnKill —
    ///   verificado contra el IL del binario real — así que cuando OnKill
    ///   corre, la primera kill de una especie ya cuenta 1) · ×2 en
    ///   hardmode. Premia explorar el mundo en vez de farmear la misma
    ///   babosa: el bestiario ES su dieta.
    /// - JEFES: XP FIJA (inmune al ×2 del hardmode y al ×3 de primera
    ///   kill — son la fuente gorda estable, no un evento de
    ///   exploración), con la fórmula pedida:
    ///     (base + 10% de la vida total del jefe + estrellas del
    ///     bestiario) + 1.1 × (nivel del grimorio × 111)
    ///   El término del nivel mantiene a los jefes relevantes cuando el
    ///   libro ya está alto.
    /// - PARTES: una derrota, UN cobro. Los gusanos solo pagan por la
    ///   cabeza (Devorador de Mundos / El Devorador), el Muro de Carne
    ///   por la boca, el Golem por el cuerpo y el Señor de la Luna por
    ///   el núcleo — los segmentos y órganos que mueren en cascada o
    ///   como fase NO pagan (v6.45 pagaba por cada segmento del gusano:
    ///   una fuente de XP por error).
    /// </summary>
    public class ShardLevelSystem : ModSystem
    {
        public static ShardLevelSystem Instance =>
            ModContent.GetInstance<ShardLevelSystem>();

        public override void PostUpdateWorld()
        {
            // Aquí se podrían verificar eventos cósmicos por nivel de cada jugador
            // (Lluvia de luz estelar, extensión del Sagrario, Rifts, etc.)
            // Por ahora, el esqueleto; se implementará en Fase 10.
        }

        /// <summary>
        /// Aplica el multiplicador de XP de la configuración sobre la XP ya
        /// calculada (base + hardmode + primera kill). v6.45: redondeo con
        /// piso de 1 para que un multiplicador bajo no aniquile las kills
        /// débiles. Llamado por GlobalNPCXP.OnKill.
        /// </summary>
        public static int ApplyXPMultiplier(int amount)
        {
            if (amount <= 0) return 0;
            // v6.50.2 — FIX (config ClientSide leída por la AUTORIDAD): el
            // multiplicador lo aplica el SERVER en el cobro de XP — la
            // decisión vive en AethonConfigServidor (ServerSide). Antes, en
            // un dedicado, la lectura caía en el default del server y el
            // ajuste del cliente era una ilusión.
            var config = ModContent.GetInstance<Content.AethonConfigServidor>();
            if (config == null) return amount;
            float mult = config.XPMultiplier;
            if (mult <= 0f) return 0;
            int result = (int)(amount * mult + 0.5f);
            if (result < 1) result = 1;
            return result;
        }

        /// <summary>
        /// Tabla de XP por kill. nivelGrimorio = el de la PRIMERA copia
        /// del libro en la barra rápida del jugador (la misma que manda
        /// en las stats) — solo lo usa la fórmula de los jefes.
        /// </summary>
        public static int XPForNPC(NPC npc, int nivelGrimorio)
        {
            if (npc == null || !npc.active) return 0;
            if (npc.friendly || npc.townNPC) return 0;

            // v6.50.10 — FIX (las partes en cascada valen cero en TODAS
            // las ramas): cuerpo/cola de los gusanos, ojo del Muro,
            // manos/cabeza del Lord y cabeza del Golem son boss=false y
            // caían a la rama MOB — ~80 OnKill del Devorador pagando XP
            // de estrellas + hardmode (+primera kill) CADA UNO: el
            // grimorio subía a saltos absurdos tras cada gusano ("se
            // corrompe el jugador"). XPDeJefe ya las anulaba para la
            // rama de jefes (v6.46); ahora valen cero SIEMPRE — una
            // derrota, un cobro (la cabeza/fase final paga la fórmula).
            if (EsParteDeJefe(npc)) return 0;

            // === JEFES: XP FIJA, la fuente gorda del libro ===
            if (npc.boss)
                return XPDeJefe(npc, nivelGrimorio);

            // === MOBS: las ESTRELLAS del bestiario (0–5) valen la XP ===
            int estrellas = EstrellasBestiario(npc);
            int xp = XPPorEstrellas(estrellas);
            xp = ConPrimeraKillDeEspecie(npc, xp);
            return ConHardmode(xp);
        }

        /// <summary>
        /// v6.46: LA FÓRMULA DE LOS JEFES (fija — ni hardmode ni primera
        /// kill la tocan):
        ///   (base + 10% de la vida total del jefe + estrellas del
        ///   bestiario) + 1.1 × (nivel del grimorio × 111)
        /// Base: Moon Lord (núcleo) 100.000 · jefe de vida alta (>20.000)
        /// 25.000 · resto 5.000 — la tabla v6.45 intacta.
        /// </summary>
        private static int XPDeJefe(NPC npc, int nivelGrimorio)
        {
            // Las partes que mueren en cascada o como fase no pagan:
            // una derrota, un cobro (ver EsParteDeJefe).
            if (EsParteDeJefe(npc)) return 0;

            int baseXP;
            if (npc.type == NPCID.MoonLordCore)
                baseXP = 100000;
            else if (npc.lifeMax > 20000)
                baseXP = 25000;
            else
                baseXP = 5000;

            if (nivelGrimorio < 1) nivelGrimorio = 1;
            int terminoNivel = (int)(1.1f * nivelGrimorio * 111);
            return baseXP + npc.lifeMax / 10 + EstrellasBestiario(npc) + terminoNivel;
        }

        /// <summary>
        /// v6.46: ¿es una PARTE de jefe que no paga? El pago cae en la
        /// parte que realmente mata al jefe:
        /// - Devorador de Mundos: solo la cabeza (13) — cuerpo (14) y
        ///   cola (15) mueren en cascada al caer la cabeza (v6.45 les
        ///   pagaba a TODOS: una fuente de XP por derrota).
        /// - El Devorador: solo la cabeza (134) — cuerpo (135) y cola
        ///   (136) mueren con ella.
        /// - Muro de Carne: solo la boca (113) — el ojo (114) muere con
        ///   el muro.
        /// - Señor de la Luna: solo el núcleo (398) — cabeza (396) y
        ///   manos (397) son fases de la pelea.
        /// - Golem: solo el cuerpo (245) — la cabeza fijada (246) y la
        ///   cabeza libre (249) mueren con él.
        /// Los Gemelos NO están aquí: son DOS cuerpos con vida propia y
        /// cada uno cobra su fórmula (la voz del grimorio sí espera a que
        /// caiga el último — ver EcoSistema).
        /// Público porque EcoSistema lo usa para no "hablar" dos veces
        /// por la misma derrota.
        /// </summary>
        public static bool EsParteDeJefe(NPC npc)
        {
            if (npc.type == NPCID.EaterofWorldsBody || npc.type == NPCID.EaterofWorldsTail)
                return true;
            if (npc.type == NPCID.TheDestroyerBody || npc.type == NPCID.TheDestroyerTail)
                return true;
            if (npc.type == NPCID.WallofFleshEye)
                return true;
            if (npc.type == NPCID.MoonLordHead || npc.type == NPCID.MoonLordHand)
                return true;
            if (npc.type == NPCID.GolemHead || npc.type == NPCID.GolemHeadFree)
                return true;
            return false;
        }

        /// <summary>
        /// v6.46: LA DIETA DEL BESTIARIO — la primera vez que matas cada
        /// ESPECIE paga ×3 (los jefes quedan fuera: su XP es fija). Se
        /// lee el tracker de kills del propio bestiario vanilla
        /// (Main.BestiaryTracker.Kills), que usa el MISMO crédito por
        /// especie que el bestiario dibuja (GetBestiaryCreditId — las
        /// variantes visuales comparten entrada). RegisterKill corre
        /// ANTES de OnKill dentro de NPCLoot (IL verificado): cuando
        /// llegamos aquí, la primera kill de la especie ya cuenta 1.
        /// </summary>
        private static int ConPrimeraKillDeEspecie(NPC npc, int xp)
        {
            try
            {
                var kills = Main.BestiaryTracker?.Kills;
                if (kills != null && kills.GetKillCount(npc) <= 1)
                    return xp * 3;
            }
            catch { }
            return xp;
        }

        /// <summary>
        /// v6.45: al entrar en HARDMODE (matar al Muro de Carne) la
        /// ganancia de XP de las CRIATURAS mejora AL DOBLE.
        /// v6.46: los JEFES quedan fuera del doble — su XP es FIJA (la
        /// fórmula ya no pasa por aquí).
        /// </summary>
        private static int ConHardmode(int xp)
        {
            if (Main.hardMode) return xp * 2;
            return xp;
        }

        /// <summary>
        /// Las estrellas de rareza que el BESTIARIO muestra para el tipo de
        /// este NPC (0–5). Primero la tabla oficial
        /// (ContentSamples.NpcBestiaryRarityStars — la misma que el
        /// bestiario dibuja); si no existe todavía (carga temprana /
        /// servidor dedicado sin bestiario), la MISMA fórmula vanilla
        /// recalculada sobre el NPC vivo. Las variantes visuales (netID
        /// negativo) comparten las estrellas de su tipo base.
        /// v6.47: público como EstrellasDe — la PRIMERA 5★ del libro
        /// (GlobalNPCXP) consulta lo mismo que la dieta.
        /// </summary>
        public static int EstrellasDe(NPC npc) => EstrellasBestiario(npc);

        private static int EstrellasBestiario(NPC npc)
        {
            try
            {
                if (ContentSamples.NpcBestiaryRarityStars != null &&
                    ContentSamples.NpcBestiaryRarityStars.TryGetValue(npc.type, out int estrellas))
                    return estrellas;
            }
            catch { }
            return FormulaEstrellasVanilla(npc);
        }

        /// <summary>
        /// La fórmula EXACTA de vanilla (ContentSamples.
        /// GetNPCBestiaryRarityStarsCount, leída del IL del binario real):
        /// 1 + rareza del Lifeform Analyzer (+ su bonus creciente)
        /// + 0.5 si es jefe + poder estadístico
        /// (daño + defensa + vida/4, por tramos), tope 5.
        /// </summary>
        private static int FormulaEstrellasVanilla(NPC npc)
        {
            float n = 1f;
            n += npc.rarity;
            if (npc.rarity == 1) n += 1f;
            else if (npc.rarity == 2) n += 1.5f;
            else if (npc.rarity == 3) n += 2f;
            else if (npc.rarity == 4) n += 2.5f;
            else if (npc.rarity == 5) n += 3f;
            else if (npc.rarity > 0) n += 3.5f;
            if (npc.boss) n += 0.5f;
            int poder = npc.damage + npc.defense + npc.lifeMax / 4;
            if (poder > 10000) n += 3.5f;
            else if (poder > 5000) n += 3f;
            else if (poder > 1000) n += 2.5f;
            else if (poder > 500) n += 2f;
            else if (poder > 150) n += 1.5f;
            else if (poder > 50) n += 1f;
            if (n > 5f) n = 5f;
            return (int)n;
        }

        /// <summary>
        /// Estrellas del bestiario → XP base: 5 × estrellas².
        /// 1★=5 · 2★=20 · 3★=45 · 4★=80 · 5★=125.
        /// La rareza pesa al CUADRADO: una criatura 5★ (lo más raro de ver)
        /// vale 25 kills de 1★. Con el coste inicial de 100 XP, subir al
        /// nivel 2 cuesta 20 kills de lo más común — o una sola criatura
        /// 5★. En hardmode (×2) todo el catálogo paga el doble, y la
        /// PRIMERA kill de cada especie (×3) hace que explorar valga la
        /// pena: el bestiario es su dieta.
        /// </summary>
        private static int XPPorEstrellas(int estrellas)
        {
            if (estrellas < 1) estrellas = 1;
            if (estrellas > 5) estrellas = 5;
            return 5 * estrellas * estrellas;
        }

        /// <summary>
        /// Hitos cósmicos por nivel del fragmento.
        /// </summary>
        public static bool IsMilestone(int level)
        {
            return level == 10 || level == 25 || level == 50 ||
                   level == 75 || level == 100 || level == 150 || level == 200;
        }
    }
}
