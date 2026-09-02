using Terraria.ID;
using System;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// Sistema central de niveles del fragmento. Funciones estáticas para
    /// otorgar XP, consultar tablas, y manejar eventos por hito de nivel.
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
        /// Otorga XP al arma sostenida del jugador (no al jugador directamente).
        /// Aplica el multiplicador de XP de la configuración.
        /// Llamado por GlobalNPCXP.OnKill.
        /// </summary>
        public static int ApplyXPMultiplier(int amount)
        {
            var config = ModContent.GetInstance<Content.AethonConfig>();
            if (config != null)
                return (int)(amount * config.XPMultiplier);
            return amount;
        }

        /// <summary>
        /// Tabla de XP por tipo de NPC. Devuelve la XP que da al morir.
        /// </summary>
        public static int XPForNPC(NPC npc)
        {
            // Jefes dan mucha más XP.
            if (npc.boss)
            {
                // Endgame (Moon Lord) = 100000, Hardmode = 25000, Pre-Hardmode = 5000.
                if (npc.type == NPCID.MoonLordCore ||
                    npc.type == NPCID.MoonLordHand ||
                    npc.type == NPCID.MoonLordHead)
                    return 100000;
                // Heurística simple: si el jefe tiene > 20000 HP, es hardmode.
                if (npc.lifeMax > 20000)
                    return 25000;
                return 5000;
            }
            // Mobs comunes: basado en lifeMax.
            if (npc.lifeMax > 500) return 15;
            if (npc.lifeMax > 100) return 5;
            return 1 + (int)(npc.lifeMax / 50f);
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
