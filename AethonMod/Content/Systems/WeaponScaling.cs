using System.Collections.Generic;
using Terraria;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// Sistema de escalado del Grimorio del Eterno por nivel (INFINITO).
    ///
    /// REGLAS:
    /// - Cada nivel sube estadisticas base (crecimiento lineal).
    /// - Cada 5 niveles se AGREGA una estadistica extra (hito acumulativo).
    /// - Sin dependencias de BranchType ni ShardPlayer.
    /// </summary>
    public static class WeaponScaling
    {
        // === STATS BASE POR NIVEL ===
        public const float MagicDamagePerLevel = 0.022f;   // +2.2%
        public const float SummonDamagePerLevel = 0.01f;   // +1%
        public const float CritPerLevel = 0.002f;           // +0.2%
        public const float UseSpeedPerLevel = 0.003f;       // -0.3% (tope -25%)
        public const float ArmorPenPerLevel = 0.004f;        // +0.4%

        // === MANA ===
        public const int ManaBase = 3;
        public const int ManaPer20Levels = 3;
        public const int ManaMax = 30;

        // ================================================================
        //  STATS BASE
        // ================================================================

        /// <summary>Multiplicador de daño mágico: 1 + nivel * 0.022</summary>
        public static float MagicDamageMult(int level) => 1f + level * MagicDamagePerLevel;

        /// <summary>Bonus de daño de invocación: nivel * 0.01 (aditivo)</summary>
        public static float SummonDamageBonus(int level) => level * SummonDamagePerLevel;

        /// <summary>Bonus de critico: nivel * 0.2%</summary>
        public static float CritBonus(int level) => level * CritPerLevel * 100f;

        /// <summary>Bonus de armor penetration: nivel * 0.4%</summary>
        public static float ArmorPenBonus(int level) => level * ArmorPenPerLevel * 100f;

        /// <summary>Multiplicador de use time (menor = mas rapido). Tope -25%.</summary>
        public static float UseSpeedMult(int level)
        {
            float reduction = level * UseSpeedPerLevel;
            if (reduction > 0.25f) reduction = 0.25f;
            return 1f - reduction;
        }

        // ================================================================
        //  MANA
        // ================================================================

        /// <summary>Costo de mana del bolt: 3 + nivel/20 * 3, tope 30.</summary>
        public static int ManaCost(int level)
        {
            int cost = ManaBase + (level / 20) * ManaPer20Levels;
            if (cost > ManaMax) cost = ManaMax;
            return cost;
        }

        /// <summary>Costo de mana del minion: 15 + nivel, tope 100.</summary>
        public static int MinionManaCost(int level)
        {
            int cost = 15 + level;
            if (cost > 100) cost = 100;
            return cost;
        }

        // ================================================================
        //  BONUS POR MANA FALTANTE
        // ================================================================

        /// <summary>
        /// +0.5% daño por 1% mana faltante (tope +50%).
        /// Formula: 1 + (missingFraction * 0.5)
        /// </summary>
        public static float LowManaDamageMult(int currentMana, int maxMana)
        {
            if (maxMana <= 0) return 1f;
            float missing = (float)(maxMana - currentMana) / maxMana;
            if (missing < 0f) missing = 0f;
            if (missing > 1f) missing = 1f;
            return 1f + missing * 0.5f;
        }

        // ================================================================
        //  SLOTS DE MINION
        // ================================================================

        /// <summary>+1 slot de minion cada 5 niveles (infinito).</summary>
        public static int BonusMinionSlots(int level)
        {
            if (level < 5) return 0;
            return level / 5;
        }

        // ================================================================
        //  LIFESTEAL
        // ================================================================

        /// <summary>Lifesteal: 0% hasta nivel 7, luego +0.1% cada 7 niveles.</summary>
        public static float LifestealPercent(int level)
        {
            if (level < 7) return 0f;
            return (level / 7) * 0.001f;
        }

        public static bool HasLifesteal(int level) => level >= 7;

        public static void ApplyLifesteal(Player player, int damageDone, int level)
        {
            float pct = LifestealPercent(level);
            if (pct <= 0f || damageDone <= 0) return;
            int heal = (int)System.Math.Max(1, damageDone * pct);
            player.HealEffect(heal);
            player.statLife += heal;
            if (player.statLife > player.statLifeMax2)
                player.statLife = player.statLifeMax2;
        }

        // ================================================================
        //  PROYECTILES EXTRA
        // ================================================================

        /// <summary>+1 ArcaneBolt extra cada 5 niveles.</summary>
        public static int ExtraProjectiles(int level) => level / 5;

        // ================================================================
        //  HITOS (cada 5 niveles, cicla patron)
        // ================================================================

        public static List<string> MilestonesReached(int level)
        {
            var list = new List<string>();
            int maxMilestone = level / 5;
            for (int m = 1; m <= maxMilestone; m++)
            {
                string? desc = MilestoneDescription(m);
                if (desc != null)
                    list.Add($"Nivel {m * 5}: {desc}");
            }
            return list;
        }

        public static string? MilestoneDescription(int milestone)
        {
            int cycle = ((milestone - 1) % 5) + 1;
            return cycle switch
            {
                1 => "+1 slot de minion",
                2 => "+1 bolt extra por lanzamiento",
                3 => "+5% velocidad de lanzamiento",
                4 => "+10% critico magico",
                5 => "+1 slot de minion",
                _ => "+2% daño",
            };
        }
    }
}
