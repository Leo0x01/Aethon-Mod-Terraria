using Terraria;
using Terraria.ModLoader;
using AethonMod.Content.Players;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// Sistema de escalado de armas por nivel del Fragmento Genesis.
    ///
    /// REGLAS:
    /// - Cada nivel subido mejora UNA estadistica base del arma (crecimiento lineal).
    /// - Cada 5 niveles (5, 10, 15, 20, ...) se AGREGA una estadistica extra/bonus.
    ///   Estos bonus son hitos: no se repiten, cada uno anade algo nuevo.
    ///
    /// El escalado es POR RAMA (cada rama tiene sus propias stats/bonus).
    /// </summary>
    public static class WeaponScaling
    {
        // ================================================================
        //  STATS BASE — suben cada nivel (crecimiento lineal)
        // ================================================================

        /// <summary>+2.5% daño por nivel (Melee)</summary>
        public const float MeleeDamagePerLevel = 0.025f;
        /// <summary>+2% daño por nivel (Distancia)</summary>
        public const float RangedDamagePerLevel = 0.02f;
        /// <summary>+2.2% daño magico por nivel (Magia)</summary>
        public const float MagicDamagePerLevel = 0.022f;
        /// <summary>+1% daño de invocacion por nivel (Magia hibrida)</summary>
        public const float SummonDamagePerLevel = 0.01f;

        /// <summary>Velocidad de uso: -0.3% por nivel (mas rapido), minimo 25% total.</summary>
        public const float UseSpeedPerLevel = 0.003f;
        /// <summary>+0.2% critico por nivel.</summary>
        public const float CritPerLevel = 0.002f;
        /// <summary>+0.5% knockback por nivel.</summary>
        public const float KnockbackPerLevel = 0.005f;
        /// <summary>+0.4% armadura penetracion por nivel.</summary>
        public const float ArmorPenPerLevel = 0.004f;

        /// <summary>Mana: +1 por cada 5 niveles (Grimorio). Tope: 20.</summary>
        public const int ManaPerFiveLevels = 1;
        public const int ManaMax = 20;

        // ================================================================
        //  BONUS DE HITO — cada 5 niveles se agrega una stat nueva
        //  (acumulativa: en nivel 25 tienes los bonus de 5, 10, 15, 20, 25)
        // ================================================================

        /// <summary>
        /// Devuelve el multiplicador de daño base segun la rama y el nivel.
        /// </summary>
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

        /// <summary>
        /// Multiplicador de velocidad de uso (menor = mas rapido).
        /// -0.3% por nivel, tope -25% (mult minimo 0.75).
        /// </summary>
        public static float UseSpeedMult(int level)
        {
            float reduction = level * UseSpeedPerLevel;
            if (reduction > 0.25f) reduction = 0.25f;
            return 1f - reduction;
        }

        /// <summary>
        /// Bonus de crit chance por nivel (+0.2% cada nivel).
        /// </summary>
        public static float CritBonus(int level) => level * CritPerLevel * 100f;

        /// <summary>
        /// Bonus de knockback por nivel.
        /// </summary>
        public static float KnockbackMult(int level) => 1f + level * KnockbackPerLevel;

        /// <summary>
        /// Bonus de armor penetration por nivel.
        /// </summary>
        public static float ArmorPenBonus(int level) => level * ArmorPenPerLevel * 100f;

        /// <summary>
        /// Cantidad de slots de minion extra otorgados por el Grimorio.
        /// +1 slot cada 5 niveles (5, 10, 15...). Tope: +10 (nivel 50+).
        /// </summary>
        public static int BonusMinionSlots(int level)
        {
            if (level < 5) return 0;
            int slots = level / 5;
            if (slots > 10) slots = 10;
            return slots;
        }

        /// <summary>
        /// Costo de mana del Grimorio segun el nivel.
        /// +1 mana cada 5 niveles, tope 20.
        /// </summary>
        public static int ManaCost(int level)
        {
            int cost = (level / 5) * ManaPerFiveLevels;
            if (cost > ManaMax) cost = ManaMax;
            return cost;
        }

        /// <summary>
        /// Cantidad de proyectiles extra que dispara el arma (bonus de hito).
        /// Melee: +1 proyectil (DawnSlash) cada 10 niveles.
        /// Distancia: +1 flecha extra cada 5 niveles.
        /// Magia: +1 bolt extra cada 5 niveles.
        /// </summary>
        public static int ExtraProjectiles(BranchType branch, int level)
        {
            return branch switch
            {
                BranchType.Melee => level / 10,    // +1 cada 10 niveles
                BranchType.Distance => level / 5,  // +1 cada 5 niveles
                BranchType.Magic => level / 5,      // +1 cada 5 niveles
                _ => 0,
            };
        }

        /// <summary>
        /// Devuelve la lista de hitos alcanzados (cada 5 niveles) para mostrar en el tooltip.
        /// Cada hito agrega una stat nueva.
        /// </summary>
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
        /// Descripcion del bonus de hito numero N (1=nivel5, 2=nivel10, etc.) por rama.
        /// </summary>
        public static string? MilestoneDescription(BranchType branch, int milestone)
        {
            // Bonus acumulativos: cada hito anade algo nuevo
            return branch switch
            {
                BranchType.Melee => milestone switch
                {
                    1 => "+5% velocidad de ataque",
                    2 => "+10% critico",
                    3 => "+1 proyectil Corte del Alba",
                    4 => "+15% knockback",
                    5 => "+10% armadura penetracion",
                    6 => "+5% velocidad de ataque",
                    7 => "+10% critico",
                    8 => "+1 proyectil Corte del Alba",
                    9 => "+15% knockback",
                    10 => "+10% armadura penetracion",
                    _ => "+2% daño",
                },
                BranchType.Distance => milestone switch
                {
                    1 => "+1 flecha extra por disparo",
                    2 => "+10% critico",
                    3 => "+5% velocidad de ataque",
                    4 => "+1 flecha extra por disparo",
                    5 => "+10% armadura penetracion",
                    6 => "+1 flecha extra por disparo",
                    7 => "+10% critico",
                    8 => "+5% velocidad de ataque",
                    9 => "+1 flecha extra por disparo",
                    10 => "+10% armadura penetracion",
                    _ => "+2% daño",
                },
                BranchType.Magic => milestone switch
                {
                    1 => "+1 slot de minion",
                    2 => "+1 bolt extra por lanzamiento",
                    3 => "+5% velocidad de lanzamiento",
                    4 => "+1 slot de minion",
                    5 => "+10% critico magico",
                    6 => "+1 bolt extra por lanzamiento",
                    7 => "+1 slot de minion",
                    8 => "+5% velocidad de lanzamiento",
                    9 => "+1 bolt extra por lanzamiento",
                    10 => "+10% critico magico",
                    _ => "+2% daño",
                },
                _ => null,
            };
        }
    }
}
