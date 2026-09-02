using Terraria;
using Terraria.ModLoader;
using AethonMod.Content.Players;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// Sistema de escalado de armas por nivel del Fragmento Genesis (INFINITO).
    ///
    /// REGLAS:
    /// - Cada nivel subido mejora UNA estadistica base del arma (crecimiento lineal, SIN tope).
    /// - Cada 5 niveles se AGREGA una estadistica extra/bonus (hito acumulativo, infinito).
    /// - Melee: cada 10 niveles, +1 proyectil "Solbrand Blade" autoguiado por ataque.
    /// - Grimorio: +1 slot de minion cada 5 niveles (stackea con armadura de invocador).
    /// </summary>
    public static class WeaponScaling
    {
        // ================================================================
        //  STATS BASE — suben cada nivel (crecimiento lineal INFINITO)
        // ================================================================

        public const float MeleeDamagePerLevel = 0.025f;
        public const float RangedDamagePerLevel = 0.02f;
        public const float MagicDamagePerLevel = 0.022f;
        public const float SummonDamagePerLevel = 0.01f;

        /// <summary>Velocidad de uso: -0.3% por nivel, tope -25%.</summary>
        public const float UseSpeedPerLevel = 0.003f;
        public const float CritPerLevel = 0.002f;
        public const float KnockbackPerLevel = 0.005f;
        public const float ArmorPenPerLevel = 0.004f;

        /// <summary>Mana: +3 por cada 20 niveles (Grimorio). Tope: 30.</summary>
        public const int ManaPer20Levels = 3;
        public const int ManaMax = 30;
        /// <summary>Mana base del Grimorio (siempre cuesta al menos esto).</summary>
        public const int ManaBase = 3;

        // ================================================================
        //  METODOS DE STATS BASE
        // ================================================================

        public static float DamageMult(BranchType branch, int level)
        {
            return branch switch
            {
                BranchType.Melee => 1f + level * MeleeDamagePerLevel,
                BranchType.Distance => 1f + level * RangedDamagePerLevel,
                BranchType.Magic => 1f + level * MagicDamagePerLevel,
                _ => 1f,
            };
        }

        public static float UseSpeedMult(int level)
        {
            float reduction = level * UseSpeedPerLevel;
            if (reduction > 0.25f) reduction = 0.25f;
            return 1f - reduction;
        }

        public static float CritBonus(int level) => level * CritPerLevel * 100f;
        public static float KnockbackMult(int level) => 1f + level * KnockbackPerLevel;
        public static float ArmorPenBonus(int level) => level * ArmorPenPerLevel * 100f;

        // ================================================================
        //  GRIMORIO — slots de minion (stackea con armadura de invocador)
        // ================================================================

        /// <summary>
        /// Cantidad de slots de minion extra otorgados por el Grimorio.
        /// +1 slot cada 5 niveles. SIN tope (infinito).
        /// Estos slots se SUMAN a player.maxMinions, que ya incluye los bonuses
        /// de armadura de invocador, accesorios y buffs.
        /// </summary>
        public static int BonusMinionSlots(int level)
        {
            if (level < 5) return 0;
            return level / 5;
        }

        /// <summary>
        /// Costo de mana del Grimorio segun el nivel.
        /// Base 3, +3 cada 20 niveles, tope 30.
        /// Nivel 1-19: 3 mana. Nivel 20-39: 6. Nivel 40-59: 9... Nivel 200: 30.
        /// </summary>
        public static int ManaCost(int level)
        {
            int cost = ManaBase + (level / 20) * ManaPer20Levels;
            if (cost > ManaMax) cost = ManaMax;
            return cost;
        }

        /// <summary>
        /// Multiplicador de daño por % de mana FALTANTE.
        /// A 0% mana faltante (full mana): x1.0 (sin bonus).
        /// A 50% mana faltante: x1.25 (+25% daño).
        /// A 90% mana faltante: x1.45 (+45% daño).
        /// Formula: 1 + (missingFraction * 0.5)
        /// Tope soft: +50% daño a mana casi vacio.
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
        //  PROYECTILES EXTRA
        // ================================================================

        /// <summary>
        /// Cantidad de proyectiles extra que dispara el arma.
        /// - Melee: +1 Solbrand Blade cada 10 niveles (proyectil autoguiado que copia el arma)
        /// - Distancia: +1 flecha extra cada 5 niveles
        /// - Magia: +1 ArcaneBolt extra cada 5 niveles
        /// INFINITO.
        /// </summary>
        public static int ExtraProjectiles(BranchType branch, int level)
        {
            return branch switch
            {
                BranchType.Melee => level / 10,
                BranchType.Distance => level / 5,
                BranchType.Magic => level / 5,
                _ => 0,
            };
        }

        // ================================================================
        //  HITOS — cada 5 niveles (INFINITO, cicla el patron)
        // ================================================================

        public static System.Collections.Generic.List<string> MilestonesReached(BranchType branch, int level)
        {
            var list = new System.Collections.Generic.List<string>();
            int maxMilestone = level / 5;

            for (int m = 1; m <= maxMilestone; m++)
            {
                int milestoneLevel = m * 5;
                string? desc = MilestoneDescription(branch, m);
                if (desc != null)
                    list.Add($"[c/78FF96:Nivel {milestoneLevel}] {desc}");
            }
            return list;
        }

        /// <summary>
        /// Descripcion del bonus de hito numero N por rama.
        /// El patron se cicla cada 5 hitos (25 niveles) para que sea infinito.
        /// </summary>
        public static string? MilestoneDescription(BranchType branch, int milestone)
        {
            // Ciclar el patron cada 5 hitos (m1=m6=m11, m2=m7=m12, ...)
            int cycle = ((milestone - 1) % 5) + 1;

            return branch switch
            {
                BranchType.Melee => cycle switch
                {
                    1 => "+5% velocidad de ataque",
                    2 => "+10% critico",
                    3 => "+1 espada autoguiada por ataque",
                    4 => "+15% knockback",
                    5 => "+10% armadura penetracion",
                    _ => "+2% daño",
                },
                BranchType.Distance => cycle switch
                {
                    1 => "+1 flecha extra por disparo",
                    2 => "+10% critico",
                    3 => "+5% velocidad de ataque",
                    4 => "+5% armadura penetracion",
                    5 => "+1 flecha extra por disparo",
                    _ => "+2% daño",
                },
                BranchType.Magic => cycle switch
                {
                    1 => "+1 slot de minion",
                    2 => "+1 bolt extra por lanzamiento",
                    3 => "+5% velocidad de lanzamiento",
                    4 => "+10% critico magico",
                    5 => "+1 slot de minion",
                    _ => "+2% daño",
                },
                _ => null,
            };
        }
    }
}
