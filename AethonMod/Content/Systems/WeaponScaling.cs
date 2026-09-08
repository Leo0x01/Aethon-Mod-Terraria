using System.Collections.Generic;
using Terraria;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// Sistema de escalado del Grimorio del Eterno por nivel (INFINITO).
    /// Todas las mejoras tienen máximo salvo donde se indique "Infinito".
    /// </summary>
    public static class WeaponScaling
    {
        // === STATS BASE POR NIVEL ===
        public const float MagicDamagePerLevel = 0.022f;
        public const float SummonDamagePerLevel = 0.01f;
        public const float CritPerLevel = 0.002f;
        public const float UseSpeedPerLevel = 0.003f;

        // === MANA ===
        public const int ManaBase = 3;
        public const int ManaPer20Levels = 3;
        public const int ManaMax = 30;

        // ================================================================
        //  STATS BASE
        // ================================================================

        /// <summary>Multiplicador de daño mágico (Infinito).</summary>
        public static float MagicDamageMult(int level) => 1f + level * MagicDamagePerLevel;

        /// <summary>Bonus de daño de invocación (Infinito).</summary>
        public static float SummonDamageBonus(int level) => level * SummonDamagePerLevel;

        /// <summary>Bonus de crítico (Máximo 100%).</summary>
        public static float CritBonus(int level)
        {
            float crit = level * CritPerLevel * 100f;
            if (crit > 100f) crit = 100f;
            return crit;
        }

        /// <summary>Bonus de armor penetration cada 5 niveles (Máximo 50%).</summary>
        public static float ArmorPenBonus(int level)
        {
            float pen = (level / 5) * 2f;
            if (pen > 50f) pen = 50f;
            return pen;
        }

        /// <summary>
        /// Multiplicador de use time (Máximo -25%).
        /// v5.25: Redondea el useTime efectivo a ENTERO para evitar doble Shoot.
        /// Causa raíz: si useTime × multiplier no es entero, tModLoader llama
        /// Shoot 2 veces por ciclo de animación. Redondear a entero lo arregla.
        /// El multiplier se calcula para que useTimeBase × multiplier sea entero.
        /// </summary>
        public static float UseSpeedMult(int level, int useTimeBase = 22)
        {
            float reduction = level * UseSpeedPerLevel;
            if (reduction > 0.25f) reduction = 0.25f;
            // Calcular useTime efectivo y redondear a entero
            int effectiveUseTime = (int)(useTimeBase * (1f - reduction));
            if (effectiveUseTime < 1) effectiveUseTime = 1;
            // Retornar el multiplier que produce ese useTime entero
            return (float)effectiveUseTime / useTimeBase;
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

        /// <summary>+0.5% daño por 1% mana faltante (tope +50%).</summary>
        public static float LowManaDamageMult(int currentMana, int maxMana)
        {
            if (maxMana <= 0) return 1f;
            float missing = (float)(maxMana - currentMana) / maxMana;
            if (missing < 0f) missing = 0f;
            if (missing > 1f) missing = 1f;
            return 1f + missing * 0.5f;
        }

        // ================================================================
        //  STATS DEL JUGADOR (persisten al cambiar de arma)
        // ================================================================

        /// <summary>+1 mana max cada 4 niveles (Infinito).</summary>
        public static int BonusMana(int level) => level / 4;

        /// <summary>+2 vida max cada 20 niveles (Infinito).</summary>
        public static int BonusLife(int level) => (level / 20) * 2;

        /// <summary>+1 slot de minion cada 5 niveles (Infinito).</summary>
        public static int BonusMinionSlots(int level)
        {
            if (level < 5) return 0;
            return level / 5;
        }

        // ================================================================
        //  KNOCKBACK (cada 10 niveles, máximo +100%)
        // ================================================================

        /// <summary>+10% knockback cada 10 niveles (tope +100%).</summary>
        public static float KnockbackMult(int level)
        {
            float bonus = (level / 10) * 0.1f;
            if (bonus > 1f) bonus = 1f;
            return 1f + bonus;
        }

        // ================================================================
        //  REGENERACIÓN (cada 20 niveles)
        // ================================================================

        /// <summary>+1 mana/seg cada 20 niveles (tope 10/seg).</summary>
        public static int ManaRegen(int level)
        {
            int regen = level / 20;
            if (regen > 10) regen = 10;
            return regen;
        }

        /// <summary>+0.5 vida/seg cada 20 niveles (tope 5/seg).</summary>
        public static float LifeRegen(int level)
        {
            float regen = (level / 20) * 0.5f;
            if (regen > 5f) regen = 5f;
            return regen;
        }

        // ================================================================
        //  REDUCCIÓN DE DAÑO (cada 100 niveles, máximo 10%)
        // ================================================================

        /// <summary>-1% daño recibido cada 100 niveles (tope 10%).</summary>
        public static float DamageReduction(int level)
        {
            float reduction = (level / 100) * 0.01f;
            if (reduction > 0.10f) reduction = 0.10f;
            return reduction;
        }

        // ================================================================
        //  LIFESTEAL (cada 7 niveles, empieza nivel 7)
        // ================================================================

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
        //  PROYECTILES EXTRA (cada 3 niveles)
        // ================================================================

        public static int ExtraProjectiles(int level)
        {
            if (level < 3) return 0;
            return level / 3;
        }

        // ================================================================
        //  PROBABILIDAD DE DISPARO DOBLE (cada 5 niveles, tope 50%)
        // ================================================================

        /// <summary>+5% prob de disparo doble cada 5 niveles (tope 50%).</summary>
        public static float DoubleShotChance(int level)
        {
            float chance = (level / 5) * 0.05f;
            if (chance > 0.5f) chance = 0.5f;
            return chance;
        }

        // ================================================================
        //  DAÑO EN ÁREA DEL BOLT (+1px por nivel, tope 100px)
        // ================================================================

        /// <summary>+1px radio de daño en área por nivel (tope 100px).</summary>
        public static int BoltAreaDamage(int level)
        {
            int area = level;
            if (area > 100) area = 100;
            return area;
        }

        // ================================================================
        //  STATS DEL MINION
        // ================================================================

        /// <summary>Hit cooldown: 15 - 1 cada 10 niveles (min 1).</summary>
        public static int MinionHitCooldown(int level) => System.Math.Max(1, 15 - level / 10);

        /// <summary>+50% daño de contacto cada 15 niveles (tope +500%).</summary>
        public static float MinionContactDamageMult(int level)
        {
            float bonus = (level / 15) * 0.5f;
            if (bonus > 5f) bonus = 5f;
            return 1f + bonus;
        }

        /// <summary>+50% velocidad del minion cada 30 niveles (tope +200%).</summary>
        public static float MinionSpeedMult(int level)
        {
            float bonus = (level / 30) * 0.5f;
            if (bonus > 2f) bonus = 2f;
            return 1f + bonus;
        }

        /// <summary>+100px rango de detección cada 10 niveles (tope 1500px, base 500).</summary>
        public static float MinionDetectionRange(int level)
        {
            float range = 500f + (level / 10) * 100f;
            if (range > 1500f) range = 1500f;
            return range;
        }

        // ================================================================
        //  HITOS (cada 5 niveles, con múltiples mejoras por hito)
        // ================================================================

        public static List<string> MilestonesForLevel(int level)
        {
            var list = new List<string>();
            int milestoneNum = level / 5;
            for (int m = 1; m <= milestoneNum; m++)
            {
                var rewards = MilestoneRewards(m);
                foreach (var r in rewards)
                    list.Add($"Nivel {m * 5}: {r}");
            }
            return list;
        }

        /// <summary>
        /// Devuelve TODAS las mejoras que se otorgan en un hito específico.
        /// Un hito puede tener múltiples mejoras simultáneas.
        /// </summary>
        public static List<string> MilestoneRewards(int milestone)
        {
            var rewards = new List<string>();
            int level = milestone * 5;

            // +1 slot de minion cada 5 niveles
            rewards.Add("+1 slot de minion");

            // +1 bolt extra cada 3 niveles (en hitos múltiplos de 3)
            if (milestone % 3 == 0) // nivel 15, 30, 45...
                rewards.Add("+1 bolt extra");

            // Probabilidad de disparo doble +5% cada 5 niveles
            rewards.Add("+5% prob disparo doble");

            // Daño en área +1px cada 5 niveles
            rewards.Add("+1px daño en área");

            // Armor penetration +2% cada 5 niveles (cada hito)
            float pen = ArmorPenBonus(level);
            if (pen < 50f)
                rewards.Add("+2% armor penetration");

            // Knockback +10% cada 10 niveles
            if (milestone % 2 == 0) // nivel 10, 20, 30...
            {
                float kb = KnockbackMult(level);
                if (kb < 2f)
                    rewards.Add("+10% knockback");
            }

            // Regeneración mana +1/seg cada 20 niveles
            if (milestone % 4 == 0) // nivel 20, 40, 60...
            {
                int regen = ManaRegen(level);
                if (regen < 10)
                    rewards.Add("+1 mana/seg regen");
            }

            // Regeneración vida +0.5/seg cada 20 niveles
            if (milestone % 4 == 0)
            {
                float regen = LifeRegen(level);
                if (regen < 5f)
                    rewards.Add("+0.5 vida/seg regen");
            }

            // Vida max +2 cada 20 niveles
            if (milestone % 4 == 0)
                rewards.Add("+2 vida max");

            // Mana max +1 cada 4 niveles (en hitos múltiplos de 4)
            // Como los hitos son cada 5, agregamos mana en niveles que son múltiplos de 4
            // Esto se maneja en PostUpdateEquips, no en hitos.

            // Daño de contacto minion +50% cada 15 niveles
            if (milestone % 3 == 0) // nivel 15, 30, 45...
            {
                float dmg = MinionContactDamageMult(level);
                if (dmg < 6f)
                    rewards.Add("+50% daño contacto minion");
            }

            // Velocidad minion +50% cada 30 niveles
            if (milestone % 6 == 0) // nivel 30, 60, 90...
            {
                float spd = MinionSpeedMult(level);
                if (spd < 3f)
                    rewards.Add("+50% velocidad minion");
            }

            // Rango detección minion +100px cada 10 niveles
            if (milestone % 2 == 0)
            {
                float range = MinionDetectionRange(level);
                if (range < 1500f)
                    rewards.Add("+100px rango detección");
            }

            // Lifesteal +0.1% cada 7 niveles (en hitos cercanos)
            // Nivel 7, 14, 21, 28, 35... = hitos 1.4, 2.8, 4.2, 5.6, 7...
            // Redondeamos: hito 1 (nivel 5) no, hito 2 (nivel 10) cerca de 7, hito 3 (15) cerca de 14
            int lsTier = level / 7;
            int prevLsTier = (level - 5) / 7;
            if (lsTier > prevLsTier && level >= 7)
                rewards.Add("+0.1% lifesteal");

            // Reducción de daño -1% cada 100 niveles
            if (milestone % 20 == 0) // nivel 100, 200...
            {
                float dr = DamageReduction(level);
                if (dr < 0.10f)
                    rewards.Add("-1% daño recibido");
            }

            // Crítico mágico (cada hito da algo de crit, se muestra solo si no ha llegado al tope)
            float crit = CritBonus(level);
            if (crit < 100f)
                rewards.Add("+1% crítico mágico");

            return rewards;
        }

        /// <summary>
        /// Descripción corta del próximo hito (para el tooltip).
        /// </summary>
        public static string NextMilestoneSummary(int currentLevel)
        {
            int nextMilestoneNum = (currentLevel / 5) + 1;
            int nextLevel = nextMilestoneNum * 5;
            var rewards = MilestoneRewards(nextMilestoneNum);
            if (rewards.Count == 0) return "Sin recompensa";
            if (rewards.Count == 1) return rewards[0];
            return $"{rewards.Count} mejoras";
        }
    }
}
