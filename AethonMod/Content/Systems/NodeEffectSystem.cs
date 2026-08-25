using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// Aplica los efectos de los nodos asignados del árbol de habilidades.
    /// Cada nodo tiene un ID único; este sistema los interpreta y aplica
    /// modificadores al jugador, arma y proyectiles.
    ///
    /// Convención de IDs: "<branchId>-<index>" ej: "mana-0", "proj-3".
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
        // DISTANCIA — Lumina
        // ====================================================================

        /// <summary>Modifica el daño del arco Lumina según nodos de Distancia.</summary>
        public static void ApplyDistanceEffects(Player player, ref StatModifier damage, ref float critChance)
        {
            // arrow-1: Virote de vacío +30% daño
            if (HasNode(player, "arrow-1")) damage *= 1.30f;
            // quiver-0: Tiro rápido (no afecta daño directo, solo useTime)
            // mark-0: Etiqueta +10% daño
            if (HasNode(player, "mark-0")) damage *= 1.10f;
            // mark-1: Acumulación de crítico +4% por golpe (simplificado a +15%)
            if (HasNode(player, "mark-1")) critChance += 15f;
            // mark-2: Punto débil +20% crítico
            if (HasNode(player, "mark-2")) critChance += 20f;
            // celestial-1: Destello solar (no afecta daño, solo DoT)
            // celestial-2: Eclipse +50% daño
            if (HasNode(player, "celestial-2")) damage *= 1.50f;
            // phantom-3: Forma etérea +25% daño (perfora todo)
            if (HasNode(player, "phantom-3")) damage *= 1.25f;
        }

        /// <summary>Velocidad de uso del arco según nodos.</summary>
        public static float GetUseSpeedMultiplier(Player player)
        {
            float mult = 1f;
            if (HasNode(player, "quiver-0")) mult *= 0.85f; // -15% use time
            return mult;
        }

        /// <summary>Número de proyectiles extra por disparo (Cuerda doble, etc.).</summary>
        public static int GetExtraProjectiles(Player player)
        {
            int extra = 0;
            if (HasNode(player, "quiver-1")) extra += 1; // Cuerda doble
            if (HasNode(player, "proj-4")) extra += 3;   // Multilanzamiento (magic, pero compartido)
            return extra;
        }

        /// <summary>True si el arco no consume munición (Carcaj infinito).</summary>
        public static bool HasInfiniteQuiver(Player player) => HasNode(player, "quiver-2");

        // ====================================================================
        // CUERPO A CUERPO — Solbrand
        // ====================================================================

        public static void ApplyMeleeEffects(Player player, ref StatModifier damage, ref float critChance)
        {
            // combo-1: Remate +200% daño (simplificado a siempre +50%)
            if (HasNode(player, "combo-1")) damage *= 1.50f;
            // solar-1: Eclipse +50% daño
            if (HasNode(player, "solar-1")) damage *= 1.50f;
            // weight-0: Golpes pesados +50% knockback (manejado en OnHit)
            // aegis-3: Guardia eterna no afecta daño directo
        }

        /// <summary>Knockback bonus del Melee.</summary>
        public static float GetMeleeKnockbackMult(Player player)
        {
            float mult = 1f;
            if (HasNode(player, "weight-0")) mult *= 1.50f;
            return mult;
        }

        /// <summary>Defensa bonus del Melee (Baluarte).</summary>
        public static int GetMeleeDefenseBonus(Player player)
        {
            int def = 0;
            if (HasNode(player, "aegis-3")) def += 20; // Bulwark
            return def;
        }

        // ====================================================================
        // ARTES MÁGICAS — Grimorio
        // ====================================================================

        public static void ApplyMagicEffects(Player player, ref StatModifier damage, ref float critChance)
        {
            // element-3: Virote de vacío +30% daño
            if (HasNode(player, "element-3")) damage *= 1.30f;
            // cosmic-1: Supernova +40% daño
            if (HasNode(player, "cosmic-1")) damage *= 1.40f;
            // convert-3: Desesperación: daño escala con maná faltante
            if (HasNode(player, "convert-3"))
            {
                float missingManaPct = 1f - (float)player.statMana / player.statManaMax2;
                damage += missingManaPct * 0.5f;
            }
        }

        /// <summary>Maná máximo bonus según nodos.</summary>
        public static int GetMaxManaBonus(Player player)
        {
            int mana = 0;
            if (HasNode(player, "mana-0")) mana += 40;   // Reserva de maná
            if (HasNode(player, "mana-3")) mana += 60;   // Reserva inagotable
            return mana;
        }

        /// <summary>Multiplicador de regeneración de maná.</summary>
        public static float GetManaRegenMult(Player player)
        {
            float mult = 1f;
            if (HasNode(player, "mana-1")) mult *= 1.50f; // Maná fluyente
            return mult;
        }

        /// <summary>Reducción de coste de maná (0..1).</summary>
        public static float GetManaCostReduction(Player player)
        {
            float reduction = 0f;
            if (HasNode(player, "mana-2")) reduction += 0.25f; // Lanzamiento eficiente
            if (HasNode(player, "mana-4")) reduction += 0.50f; // Reserva inagotable
            return System.MathF.Min(reduction, 1f);
        }

        /// <summary>Bonus de slots de minion.</summary>
        public static int GetBonusMinionSlots(Player player)
        {
            int slots = 0;
            if (HasNode(player, "summon-0")) slots += 1;
            if (HasNode(player, "summon-4")) slots += 3; // Enjambre estelar
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

            // Mana bonuses
            int manaBonus = 0;
            float regenMult = 1f;
            float costReduction = 0f;
            switch (sp.ActiveBranch)
            {
                case Players.BranchType.Magic:
                    manaBonus = GetMaxManaBonus(player);
                    regenMult = GetManaRegenMult(player);
                    costReduction = GetManaCostReduction(player);
                    break;
            }
            // Aplicar mana bonus (solo una vez por frame; tModLoader maneja statManaMax2).
            player.statManaMax2 += manaBonus;

            // Mana regen (simplificado: restaurar maná pasivamente)
            if (regenMult > 1f && sp.ActiveBranch == Players.BranchType.Magic)
            {
                if (Main.rand.NextBool(60 / (int)regenMult))
                    player.statMana = System.Math.Min(player.statManaMax2, player.statMana + 1);
            }

            // Mana well: restaurar maná cuando quieto
            if (HasNode(player, "mana-3") && player.velocity.Length() < 0.1f)
            {
                if (Main.rand.NextBool(12))
                    player.statMana = System.Math.Min(player.statManaMax2, player.statMana + 1);
            }

            // Melee defense bonus (Baluarte)
            if (sp.ActiveBranch == Players.BranchType.Melee)
            {
                int defBonus = GetMeleeDefenseBonus(player);
                player.statDefense += defBonus;
            }

            // Ciclo eterno: ya manejado en OnKillNPC (healing)
        }

        /// <summary>Maneja efectos al matar un NPC (lifesteal, reset cooldowns, etc.).</summary>
        public static void OnKillNPC(Player player, NPC npc)
        {
            var sp = player.GetModPlayer<Players.ShardPlayer>();
            if (sp == null || !sp.IsImprinted) return;

            // convert-4: Ciclo eterno — restaura 30% maná + 10% HP
            if (HasNode(player, "convert-4"))
            {
                player.statMana = System.Math.Min(player.statManaMax2,
                    player.statMana + (int)(player.statManaMax2 * 0.30f));
                int healHp = (int)(player.statLifeMax2 * 0.10f);
                player.HealEffect(healHp);
            }

            // mark-4: Marca letal — resetea cooldowns (simplificado: cura 5 maná)
            if (HasNode(player, "mark-4"))
            {
                player.statMana = System.Math.Min(player.statManaMax2, player.statMana + 5);
            }

            // solar-3: Ignición solar — enemigos quemados explotan
            if (HasNode(player, "solar-3") && npc.onFire)
            {
                // Explosión visual (simplificado: daño en área)
                foreach (NPC nearby in Main.ActiveNPCs)
                {
                    if (nearby.whoAmI == npc.whoAmI || !nearby.active || nearby.friendly) continue;
                    if (Vector2.Distance(nearby.Center, npc.Center) < 100f)
                    {
                        nearby.SimpleStrikeNPC(npc.lifeMax / 10, 0, false, 0, DamageClass.Generic, true, 0);
                    }
                }
                for (int i = 0; i < 20; i++)
                {
                    Dust.NewDust(npc.Center, 20, 20, Terraria.ID.DustID.Smoke);
                }
            }
        }

        /// <summary>True si el jugador tiene escudo de maná (mana shield).</summary>
        public static bool HasManaShield(Player player) => HasNode(player, "convert-0");
    }
}
