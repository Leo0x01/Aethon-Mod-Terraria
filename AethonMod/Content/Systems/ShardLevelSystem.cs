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
    ///   entrada (verificado contra el IL del binario real:
    ///   BestiaryEntry.Enemy/TownNPC/Critter la pasan a
    ///   NPCPortraitInfoElement).
    /// - Jefes: la mayor XP de todas (tabla propia 5.000–100.000).
    /// - Hardmode (Muro de Carne caído): la ganancia de XP MEJORA AL DOBLE.
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
        /// calculada (base + hardmode). v6.45: redondeo con piso de 1 para
        /// que un multiplicador bajo no aniquile las kills débiles.
        /// Llamado por GlobalNPCXP.OnKill.
        /// </summary>
        public static int ApplyXPMultiplier(int amount)
        {
            if (amount <= 0) return 0;
            var config = ModContent.GetInstance<Content.AethonConfig>();
            if (config == null) return amount;
            float mult = config.XPMultiplier;
            if (mult <= 0f) return 0;
            int result = (int)(amount * mult + 0.5f);
            if (result < 1) result = 1;
            return result;
        }

        /// <summary>
        /// Tabla de XP por kill (v6.45: XP REAL, no "1 por kill").
        /// Devuelve la XP que da al morir.
        /// </summary>
        public static int XPForNPC(NPC npc)
        {
            if (npc == null || !npc.active) return 0;
            if (npc.friendly || npc.townNPC) return 0;

            // === JEFES: la mayor XP de todas (la fuente gorda del arma) ===
            if (npc.boss)
            {
                // Endgame (Moon Lord) = 100000, Hardmode = 25000, Pre-Hardmode = 5000.
                if (npc.type == NPCID.MoonLordCore ||
                    npc.type == NPCID.MoonLordHand ||
                    npc.type == NPCID.MoonLordHead)
                    return ConHardmode(100000);
                // Heurística simple: si el jefe tiene > 20000 HP, es hardmode.
                if (npc.lifeMax > 20000)
                    return ConHardmode(25000);
                return ConHardmode(5000);
            }

            // === MOBS: las ESTRELLAS del bestiario (0–5) valen la XP ===
            int estrellas = EstrellasBestiario(npc);
            int xp = XPPorEstrellas(estrellas);
            return ConHardmode(xp);
        }

        /// <summary>
        /// v6.45: al entrar en HARDMODE (matar al Muro de Carne) la ganancia
        /// de XP mejora AL DOBLE — mobs y jefes por igual.
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
        /// </summary>
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
        /// nivel 2 cuesta 20 kills de lo más común — o una sola criatura 5★.
        /// En hardmode (×2) todo el catálogo paga el doble.
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
