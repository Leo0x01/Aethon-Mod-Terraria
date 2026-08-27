using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// Aplica los efectos de los nodos asignados del arbol de habilidades.
    ///
    /// IMPORTANTE: Los IDs de nodos deben coincidir EXACTAMENTE con los que genera
    /// PoETreeCatalog.BuildCluster:
    ///   - "{clusterId}-entry"        (Small, Cost 1)
    ///   - "{clusterId}-small-0"      (Small, Cost 1)
    ///   - "{clusterId}-small-1"      (Small, Cost 1)
    ///   - "{clusterId}-small-2"      (Small, Cost 1)
    ///   - "{clusterId}-notable"      (Notable, Cost 2)
    ///   - "{clusterId}-keystone"     (Keystone, Cost 3, solo si hasKeystone=true)
    ///   - "ascend-regen-keystone"    (Keystone de lifesteal, Ascendancy Lv 100+)
    ///
    /// Los clusters disponibles por rama:
    ///   Distance: proj-power, quiver, hunter, celestial, phantom, velocity,
    ///             piercing, elemental, range, ammo, survival, absorption
    ///   Melee:    blade, combo, solar, aegis, weight, berserk, vampire,
    ///             whirlwind, thrust, ground, survival, absorption
    ///   Magic:    mana, element, proj, convert, cosmic, summon, cast-speed,
    ///             area, debuff, barrier, survival, vampire, critical, absorption
    /// </summary>
    public static class NodeEffectSystem
    {
        /// <summary>True si el jugador tiene un nodo asignado (por ID).</summary>
        public static bool HasNode(Player player, string nodeId)
        {
            var sp = player.GetModPlayer<Players.ShardPlayer>();
            return sp != null && sp.AllocatedNodes.Contains(nodeId);
        }

        // ====================================================================
        // DISTANCIA — Lumina, la Arcoestelar
        // ====================================================================

        /// <summary>Modifica el daño del arco Lumina segun nodos de Distancia.</summary>
        public static void ApplyDistanceEffects(Player player, ref StatModifier damage, ref float critChance)
        {
            // Poder de Proyectiles: cada small node da +5/7/9% daño
            if (HasNode(player, "proj-power-small-0")) damage *= 1.05f;
            if (HasNode(player, "proj-power-small-1")) damage *= 1.07f;
            if (HasNode(player, "proj-power-small-2")) damage *= 1.09f;
            // Poder Letal de Proyectiles (Notable): +25% daño
            if (HasNode(player, "proj-power-notable")) damage *= 1.25f;
            // DESTRUCCION TOTAL (Keystone): +50% daño total
            if (HasNode(player, "proj-power-keystone")) damage *= 1.50f;

            // Cazador: cada small da +4/6/8% crit
            if (HasNode(player, "hunter-small-0")) critChance += 4f;
            if (HasNode(player, "hunter-small-1")) critChance += 6f;
            if (HasNode(player, "hunter-small-2")) critChance += 8f;
            // Ojo de Halcon (Notable): +20% crit
            if (HasNode(player, "hunter-notable")) critChance += 20f;
            // MARCA LETAL (Keystone): +40% crit en marcados (simplificado: +15%)
            if (HasNode(player, "hunter-keystone")) critChance += 15f;

            // Disparos Celestiales: small da +5/8/11% daño solar
            if (HasNode(player, "celestial-small-0")) damage *= 1.05f;
            if (HasNode(player, "celestial-small-1")) damage *= 1.08f;
            if (HasNode(player, "celestial-small-2")) damage *= 1.11f;
            // Lluvia de Meteoros (Notable): +50% daño solar
            if (HasNode(player, "celestial-notable")) damage *= 1.50f;
            // SUPERNOVA (Keystone): +40% daño total
            if (HasNode(player, "celestial-keystone")) damage *= 1.40f;

            // Carcaj Fantasma: small da +3/4/5% daño fase
            if (HasNode(player, "phantom-small-0")) damage *= 1.03f;
            if (HasNode(player, "phantom-small-1")) damage *= 1.04f;
            if (HasNode(player, "phantom-small-2")) damage *= 1.05f;
            // Forma Eterea (Notable): +25% daño
            if (HasNode(player, "phantom-notable")) damage *= 1.25f;
            // EXISTENCIA ETEREA (Keystone): +30% daño
            if (HasNode(player, "phantom-keystone")) damage *= 1.30f;

            // Perforacion: small da +1 perforacion (no afecta daño directo)
            // Elemental: small da +5/8/11% daño elemental
            if (HasNode(player, "elemental-small-0")) damage *= 1.05f;
            if (HasNode(player, "elemental-small-1")) damage *= 1.08f;
            if (HasNode(player, "elemental-small-2")) damage *= 1.11f;
            // Maestria Elemental (Notable): +30% daño elemental
            if (HasNode(player, "elemental-notable")) damage *= 1.30f;
            // APOCALIPSIS ELEMENTAL (Keystone): +40% daño total
            if (HasNode(player, "elemental-keystone")) damage *= 1.40f;
        }

        /// <summary>Velocidad de uso del arco segun nodos (multiplicador de useTime).</summary>
        public static float GetUseSpeedMultiplier(Player player)
        {
            float mult = 1f;
            // Carcaj: small-0 da +3% velocidad de disparo
            if (HasNode(player, "quiver-small-0")) mult *= 0.97f; // -3% use time
            if (HasNode(player, "quiver-small-1")) mult *= 0.95f;
            if (HasNode(player, "quiver-small-2")) mult *= 0.93f;
            // Velocidad: small da +5/7/9% velocidad de movimiento (y disparo)
            if (HasNode(player, "velocity-small-0")) mult *= 0.95f;
            if (HasNode(player, "velocity-small-1")) mult *= 0.93f;
            if (HasNode(player, "velocity-small-2")) mult *= 0.91f;
            // Velocidad Arcana (notable)
            if (HasNode(player, "velocity-notable")) mult *= 0.85f;
            // (velocity-keystone no existe — el cluster velocity no tiene keystone en PoETreeCatalog)
            // Ascendancy: +100% velocidad de disparo (ascend-5)
            if (HasNode(player, "ascend-5")) mult *= 0.50f;
            return mult;
        }

        /// <summary>Numero de proyectiles extra por disparo.</summary>
        public static int GetExtraProjectiles(Player player)
        {
            int extra = 0;
            // Carcaj: notable da +1 proyectil (Cuerda doble)
            if (HasNode(player, "quiver-notable")) extra += 1;
            // Carcaj Omnisciente (Keystone): +5 proyectiles
            if (HasNode(player, "quiver-keystone")) extra += 4;
            // Ascendancy: +3 proyectiles (ascend-2)
            if (HasNode(player, "ascend-2")) extra += 3;
            return extra;
        }

        /// <summary>True si el arco no consume municion (Carcaj infinito).</summary>
        public static bool HasInfiniteQuiver(Player player) =>
            HasNode(player, "quiver-keystone") || HasNode(player, "ammo-keystone");

        // ====================================================================
        // CUERPO A CUERPO — Solbrand
        // ====================================================================

        public static void ApplyMeleeEffects(Player player, ref StatModifier damage, ref float critChance)
        {
            // Génesis de Hoja: small da +5/7/9% daño de corte
            if (HasNode(player, "blade-small-0")) damage *= 1.05f;
            if (HasNode(player, "blade-small-1")) damage *= 1.07f;
            if (HasNode(player, "blade-small-2")) damage *= 1.09f;
            // Corte de Realidad (Notable): +30% daño de corte
            if (HasNode(player, "blade-notable")) damage *= 1.30f;
            // CORTE DE REALIDAD (Keystone): +100% daño
            if (HasNode(player, "blade-keystone")) damage *= 2.00f;

            // Combo: small da +3/4/5% velocidad de combo (damage)
            if (HasNode(player, "combo-small-0")) damage *= 1.03f;
            if (HasNode(player, "combo-small-1")) damage *= 1.04f;
            if (HasNode(player, "combo-small-2")) damage *= 1.05f;
            // Filo Infinito (Notable): +50% daño (remate)
            if (HasNode(player, "combo-notable")) damage *= 1.50f;
            // COMBO INFINITO (Keystone): +100% daño
            if (HasNode(player, "combo-keystone")) damage *= 2.00f;

            // Ira Solar: small da +5/8/11% daño solar
            if (HasNode(player, "solar-small-0")) damage *= 1.05f;
            if (HasNode(player, "solar-small-1")) damage *= 1.08f;
            if (HasNode(player, "solar-small-2")) damage *= 1.11f;
            // Corona Solar (Notable): +50% daño solar
            if (HasNode(player, "solar-notable")) damage *= 1.50f;
            // EXPLOSION SOLAR (Keystone): +50% daño total
            if (HasNode(player, "solar-keystone")) damage *= 1.50f;

            // Peso de Estrellas: small da +10/15/20% daño (peso)
            if (HasNode(player, "weight-small-0")) damage *= 1.10f;
            if (HasNode(player, "weight-small-1")) damage *= 1.15f;
            if (HasNode(player, "weight-small-2")) damage *= 1.20f;
            // Pozo de Gravedad (Notable): +50% knockback (no afecta daño directo)
            // POZO GRAVITACIONAL (Keystone): +50% daño
            if (HasNode(player, "weight-keystone")) damage *= 1.50f;

            // Furia: small da +5/7/9% velocidad de ataque (damage)
            if (HasNode(player, "berserk-small-0")) damage *= 1.05f;
            if (HasNode(player, "berserk-small-1")) damage *= 1.07f;
            if (HasNode(player, "berserk-small-2")) damage *= 1.09f;
            // Furia Sangrienta (Notable): +30% daño cuando HP < 50% (simplificado)
            if (HasNode(player, "berserk-notable") && player.statLife < player.statLifeMax2 * 0.5f) damage *= 1.30f;
            // FURIA DE SANGRE (Keystone): +100% daño cuando HP < 30%
            if (HasNode(player, "berserk-keystone") && player.statLife < player.statLifeMax2 * 0.3f) damage *= 2.00f;
        }

        /// <summary>Knockback bonus del Melee.</summary>
        public static float GetMeleeKnockbackMult(Player player)
        {
            float mult = 1f;
            if (HasNode(player, "weight-small-0")) mult *= 1.10f;
            if (HasNode(player, "weight-small-1")) mult *= 1.15f;
            if (HasNode(player, "weight-small-2")) mult *= 1.20f;
            if (HasNode(player, "weight-notable")) mult *= 1.50f;
            if (HasNode(player, "weight-keystone")) mult *= 2.00f;
            return mult;
        }

        /// <summary>Defensa bonus del Melee (Baluarte).</summary>
        public static int GetMeleeDefenseBonus(Player player)
        {
            int def = 0;
            // Egida del Alba: small da +3/4/5% parry (defensa)
            if (HasNode(player, "aegis-small-0")) def += 3;
            if (HasNode(player, "aegis-small-1")) def += 4;
            if (HasNode(player, "aegis-small-2")) def += 5;
            // Guardia Eterna (Notable): +20 defensa
            if (HasNode(player, "aegis-notable")) def += 20;
            // BULWARK ETERNO (Keystone): +50 defensa
            if (HasNode(player, "aegis-keystone")) def += 50;
            // Supervivencia: small da +10/15/20 vida (defensa bonus)
            if (HasNode(player, "survival-small-0")) def += 2;
            if (HasNode(player, "survival-small-1")) def += 3;
            if (HasNode(player, "survival-small-2")) def += 4;
            if (HasNode(player, "survival-notable")) def += 10;
            // INMORTALIDAD DEL ARQUERO (Keystone): +50 vida (defensa)
            if (HasNode(player, "survival-keystone")) def += 30;
            return def;
        }

        // ====================================================================
        // ARTES MAGICAS — Grimorio
        // ====================================================================

        public static void ApplyMagicEffects(Player player, ref StatModifier damage, ref float critChance)
        {
            // Flujo de Mana: small da +5/8/11% daño (flujo)
            if (HasNode(player, "mana-small-0")) damage *= 1.05f;
            if (HasNode(player, "mana-small-1")) damage *= 1.08f;
            if (HasNode(player, "mana-small-2")) damage *= 1.11f;
            // Reserva Inagotable (Notable): +20% daño
            if (HasNode(player, "mana-notable")) damage *= 1.20f;
            // MANA INFINITO (Keystone): +30% daño
            if (HasNode(player, "mana-keystone")) damage *= 1.30f;

            // Génesis Elemental: small da +5/8/11% daño elemental
            if (HasNode(player, "element-small-0")) damage *= 1.05f;
            if (HasNode(player, "element-small-1")) damage *= 1.08f;
            if (HasNode(player, "element-small-2")) damage *= 1.11f;
            // Convergencia Elemental (Notable): +30% daño elemental
            if (HasNode(player, "element-notable")) damage *= 1.30f;
            // CONVERGENCIA TOTAL (Keystone): +50% daño elemental
            if (HasNode(player, "element-keystone")) damage *= 1.50f;

            // Evolucion de Proyectiles: small da +3/4/5% velocidad (damage)
            if (HasNode(player, "proj-small-0")) damage *= 1.03f;
            if (HasNode(player, "proj-small-1")) damage *= 1.04f;
            if (HasNode(player, "proj-small-2")) damage *= 1.05f;
            // Multilanzamiento (Notable): +20% daño
            if (HasNode(player, "proj-notable")) damage *= 1.20f;
            // TORMENTA DE BOLTS (Keystone): +50% daño
            if (HasNode(player, "proj-keystone")) damage *= 1.50f;

            // Hechizos Cosmicos: small da +5/8/11% daño cosmico
            if (HasNode(player, "cosmic-small-0")) damage *= 1.05f;
            if (HasNode(player, "cosmic-small-1")) damage *= 1.08f;
            if (HasNode(player, "cosmic-small-2")) damage *= 1.11f;
            // Desgarro de Realidad (Notable): +40% daño cosmico
            if (HasNode(player, "cosmic-notable")) damage *= 1.40f;
            // DESGARRO DE REALIDAD (Keystone): +60% daño
            if (HasNode(player, "cosmic-keystone")) damage *= 1.60f;

            // Conversion Arcana: small da +2/3/4% eficiencia (damage)
            if (HasNode(player, "convert-small-0")) damage *= 1.02f;
            if (HasNode(player, "convert-small-1")) damage *= 1.03f;
            if (HasNode(player, "convert-small-2")) damage *= 1.04f;
            // Ciclo Eterno (Notable): daño escala con mana faltante
            if (HasNode(player, "convert-notable"))
            {
                float missingManaPct = 1f - (float)player.statMana / Math.Max(1, player.statManaMax2);
                damage += missingManaPct * 0.3f;
            }
            // CICLO ETERNO (Keystone): daño escala mas con mana faltante
            if (HasNode(player, "convert-keystone"))
            {
                float missingManaPct = 1f - (float)player.statMana / Math.Max(1, player.statManaMax2);
                damage += missingManaPct * 0.6f;
            }

            // Critico Magico: small da +4/6/8% crit
            if (HasNode(player, "critical-small-0")) critChance += 4f;
            if (HasNode(player, "critical-small-1")) critChance += 6f;
            if (HasNode(player, "critical-small-2")) critChance += 8f;
            // Toque Critico (Notable): +15% crit
            if (HasNode(player, "critical-notable")) critChance += 15f;
            // TOQUE DE LA MUERTE (Keystone): +30% crit
            if (HasNode(player, "critical-keystone")) critChance += 30f;
        }

        /// <summary>Mana maximo bonus segun nodos.</summary>
        public static int GetMaxManaBonus(Player player)
        {
            int mana = 0;
            // Flujo de Mana: small da +20/30/40 maná
            if (HasNode(player, "mana-small-0")) mana += 20;
            if (HasNode(player, "mana-small-1")) mana += 30;
            if (HasNode(player, "mana-small-2")) mana += 40;
            // Reserva Inagotable (Notable): +60 maná
            if (HasNode(player, "mana-notable")) mana += 60;
            // MANA INFINITO (Keystone): +100 maná
            if (HasNode(player, "mana-keystone")) mana += 100;
            // Barrera Arcana: small da +10/15/20 escudo (maná)
            if (HasNode(player, "barrier-small-0")) mana += 10;
            if (HasNode(player, "barrier-small-1")) mana += 15;
            if (HasNode(player, "barrier-small-2")) mana += 20;
            if (HasNode(player, "barrier-notable")) mana += 40;
            if (HasNode(player, "barrier-keystone")) mana += 80;
            return mana;
        }

        /// <summary>Multiplicador de regeneracion de maná.</summary>
        public static float GetManaRegenMult(Player player)
        {
            float mult = 1f;
            // Flujo de Mana: notable da +50% regen
            if (HasNode(player, "mana-notable")) mult *= 1.50f;
            // MANA INFINITO: x2 regen
            if (HasNode(player, "mana-keystone")) mult *= 2.00f;
            // Ascendancy: maná infinito
            if (HasNode(player, "ascend-4")) mult *= 2.00f;
            return mult;
        }

        /// <summary>Reduccion de coste de maná (0..1).</summary>
        public static float GetManaCostReduction(Player player)
        {
            float reduction = 0f;
            // Velocidad Arcana (cast-speed): notable da -20% costo
            if (HasNode(player, "cast-speed-notable")) reduction += 0.20f;
            // (cast-speed-keystone no existe — el cluster cast-speed no tiene keystone en PoETreeCatalog)
            // Reserva Inagotable (Notable): si maná < 20, gratis
            if (HasNode(player, "mana-notable")) reduction += 0.10f;
            // MANA INFINITO (Keystone): -50% costo
            if (HasNode(player, "mana-keystone")) reduction += 0.50f;
            // Ascendancy: maná infinito (ascend-4)
            if (HasNode(player, "ascend-4")) reduction += 0.50f;
            return MathF.Min(reduction, 1f);
        }

        /// <summary>Bonus de slots de minion.</summary>
        public static int GetBonusMinionSlots(Player player)
        {
            int slots = 0;
            // Maestria de Invocacion: small da +0/0/1 slot
            if (HasNode(player, "summon-small-0")) slots += 0;
            if (HasNode(player, "summon-small-1")) slots += 0;
            if (HasNode(player, "summon-small-2")) slots += 1;
            // Enjambre Estelar (Notable): +1 slot
            if (HasNode(player, "summon-notable")) slots += 1;
            // ENJAMBRE ESTELAR (Keystone): +5 slots
            if (HasNode(player, "summon-keystone")) slots += 5;
            return slots;
        }

        /// <summary>Bonus de slots de runa de memoria (capstone Absorption).</summary>
        public static int GetBonusRuneSlots(Player player)
        {
            int slots = 0;
            // Absorption: notable da +1, keystone da +2
            if (HasNode(player, "absorption-notable")) slots += 1;
            if (HasNode(player, "absorption-keystone")) slots += 2;
            return slots;
        }

        // ====================================================================
        // EFECTOS GLOBALES (todas las ramas)
        // ====================================================================

        /// <summary>Aplica modificadores pasivos cada tick (mana, HP, defensa).</summary>
        public static void ApplyPassiveEffects(Player player)
        {
            var sp = player.GetModPlayer<Players.ShardPlayer>();
            if (sp == null || !sp.IsImprinted) return;

            // Mana bonuses (todas las ramas pueden tener nodos de maná, pero solo Magic los usa activamente)
            int manaBonus = GetMaxManaBonus(player);
            player.statManaMax2 += manaBonus;

            // Mana regen pasivo (si tiene nodos relevantes)
            float regenMult = GetManaRegenMult(player);
            if (regenMult > 1f)
            {
                int chance = Math.Max(1, (int)(60f / regenMult));
                if (Main.rand.NextBool(chance))
                    player.statMana = Math.Min(player.statManaMax2, player.statMana + 1);
            }

            // Reserva Inagotable (Notable): restaurar maná cuando quieto
            if (HasNode(player, "mana-notable") && player.velocity.Length() < 0.1f)
            {
                if (Main.rand.NextBool(12))
                    player.statMana = Math.Min(player.statManaMax2, player.statMana + 1);
            }

            // Defensa bonus (Survival aplica a todas las ramas; GetMeleeDefenseBonus ya lo incluye)
            int defBonus = GetMeleeDefenseBonus(player);
            player.statDefense += defBonus;

            // Bonus de vida por Survival (Notable/Keystone)
            int lifeBonus = 0;
            if (HasNode(player, "survival-small-0")) lifeBonus += 10;
            if (HasNode(player, "survival-small-1")) lifeBonus += 15;
            if (HasNode(player, "survival-small-2")) lifeBonus += 20;
            if (HasNode(player, "survival-notable")) lifeBonus += 50;
            if (HasNode(player, "survival-keystone")) lifeBonus += 100;
            if (lifeBonus > 0) player.statLifeMax2 += lifeBonus;
        }

        /// <summary>Maneja efectos al matar un NPC (lifesteal, reset cooldowns, etc.).</summary>
        public static void OnKillNPC(Player player, NPC npc)
        {
            var sp = player.GetModPlayer<Players.ShardPlayer>();
            if (sp == null || !sp.IsImprinted) return;

            // Ciclo Eterno (Notable): restaura 30% maná + 10% HP
            if (HasNode(player, "convert-notable"))
            {
                player.statMana = Math.Min(player.statManaMax2,
                    player.statMana + (int)(player.statManaMax2 * 0.30f));
                int healHp = (int)(player.statLifeMax2 * 0.10f);
                player.HealEffect(healHp);
            }
            // CICLO ETERNO (Keystone): restaura 50% maná + 20% HP
            if (HasNode(player, "convert-keystone"))
            {
                player.statMana = Math.Min(player.statManaMax2,
                    player.statMana + (int)(player.statManaMax2 * 0.50f));
                int healHp = (int)(player.statLifeMax2 * 0.20f);
                player.HealEffect(healHp);
            }

            // MARCA LETAL (Keystone): resetea cooldowns (cura 5 maná)
            if (HasNode(player, "hunter-keystone"))
            {
                player.statMana = Math.Min(player.statManaMax2, player.statMana + 5);
            }

            // EXPLOSION SOLAR (Keystone): enemigos quemados explotan
            if (HasNode(player, "solar-keystone") && npc.onFire)
            {
                foreach (NPC nearby in Main.ActiveNPCs)
                {
                    if (nearby.whoAmI == npc.whoAmI || !nearby.active || nearby.friendly) continue;
                    if (Vector2.Distance(nearby.Center, npc.Center) < 100f)
                    {
                        int dmg = npc.lifeMax / 10;
                        // SimpleStrikeNPC en tModLoader v2026.06 signature:
                        //   SimpleStrikeNPC(int damage, int hitDirection, bool ?, float critChance?,
                        //     DamageClass damageType, bool noEffects, float knockbackScale, bool fromNet)
                        // Args: dmg, player.whoAmI (hitDirection), true (avoid interrupt), 0f (critChance),
                        //       DamageClass.Melee, false (noEffects), 0f (knockbackScale), false (fromNet)
                        _ = nearby.SimpleStrikeNPC(dmg, player.whoAmI, true, 0f, DamageClass.Melee, false, 0f, false);
                    }
                }
                for (int i = 0; i < 20; i++)
                    Dust.NewDust(npc.Center, 20, 20, DustID.Smoke);
            }
        }

        /// <summary>True si el jugador tiene escudo de maná (Barrera Arcana).</summary>
        public static bool HasManaShield(Player player) =>
            HasNode(player, "barrier-notable") || HasNode(player, "barrier-keystone");

        // ====================================================================
        // REGENERACION DE SALUD POR DAÑO (Lifesteal Keystone - Ascendancy Lv 100+)
        // Distancia y Melee: 0.01% del daño causado = regen de salud.
        // Magia: igual pero el daño de INVOCACIONES NO cuenta.
        // ====================================================================

        /// <summary>
        /// Llamado cuando el jugador causa daño a un NPC.
        /// Si tiene el Keystone de Regeneracion Vital, cura al jugador.
        /// </summary>
        public static void OnHitNPC(Player player, NPC target, int damage, bool isFromMinion)
        {
            var sp = player.GetModPlayer<Players.ShardPlayer>();
            if (sp == null || !sp.IsImprinted) return;

            // Verificar si tiene el Keystone de regeneracion vital (Ascendancy).
            if (!HasNode(player, "ascend-regen-keystone")) return;

            // Magia: si el daño viene de una invocacion (minion), NO aplica regeneracion.
            if (sp.ActiveBranch == Players.BranchType.Magic && isFromMinion) return;

            // Regenerar 0.01% de la salud maxima del jugador por cada punto de daño causado.
            float healPct = 0.0001f;
            int healAmount = (int)(damage * player.statLifeMax2 * healPct);

            if (healAmount > 0)
            {
                player.statLife = Math.Min(player.statLifeMax2, player.statLife + healAmount);
                player.HealEffect(healAmount, true);
            }
        }

        /// <summary>
        /// Version para proyectiles: determina si el proyectil es de invocacion.
        /// </summary>
        public static void OnProjectileHitNPC(Player player, NPC target, int damage, Projectile projectile)
        {
            bool isFromMinion = projectile.minion || projectile.sentry || projectile.DamageType == DamageClass.Summon;
            OnHitNPC(player, target, damage, isFromMinion);
        }
    }
}
