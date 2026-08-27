using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;

namespace AethonMod.Content.Weapons
{
    /// <summary>
    /// Grimorio del Eterno — arma de la rama de Artes Mágicas.
    ///
    /// FUNCIONES:
    /// - Click izquierdo: dispara ArcaneBolt (magia ofensiva)
    /// - Click derecho: invoca un minion cosmico (invocacion)
    /// - Barra de XP del fragmento visible en el tooltip del arma
    ///
    /// MANA: Base 0 (no consume mana sin nodos).
    /// Cada nodo Notable del árbol aumenta el costo de mana en 5.
    /// Máximo: 20 de mana por uso (4 notables = 4×5 = 20).
    ///
    /// DAÑO: Escala con el nivel del fragmento.
    /// </summary>
    public class GrimoireEternal : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName / Tooltip cargados desde Localization.
        }

        public override void SetDefaults()
        {
            Item.damage = 11;
            Item.DamageType = DamageClass.Magic;
            Item.width = 36;
            Item.height = 44;
            Item.useTime = 22;
            Item.useAnimation = 22;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.knockBack = 3f;
            Item.value = Item.buyPrice(0, 10, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<Projectiles.ArcaneBolt>();
            Item.shootSpeed = 12f;
            Item.mana = 0; // Base: NO consume mana.
            Item.noMelee = true;
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            var sp = player.GetModPlayer<Players.ShardPlayer>();
            if (sp != null && sp.IsImprinted && sp.ActiveBranch == Players.BranchType.Magic)
            {
                damage += sp.ShardLevel * 2.6f;
            }
            float crit = 0;
            Systems.NodeEffectSystem.ApplyMagicEffects(player, ref damage, ref crit);
            player.GetCritChance(DamageClass.Magic) += crit;
        }

        public override void ModifyManaCost(Player player, ref float reduce, ref float mult)
        {
            var sp = player.GetModPlayer<Players.ShardPlayer>();
            if (sp == null) return;

            // Base: 0 mana. Cada Notable asignado suma 5 de mana, hasta max 20.
            int notableCount = 0;
            var tree = Systems.PoETreeCatalog.GetTree(Players.BranchType.Magic);
            foreach (var node in tree.Nodes)
            {
                if (node.Type == Systems.NodeType.Notable && sp.AllocatedNodes.Contains(node.Id))
                    notableCount++;
            }

            int baseManaCost = System.Math.Min(notableCount * 5, 20);

            // Aplicar reducción de mana de nodos específicos.
            float reduction = Systems.NodeEffectSystem.GetManaCostReduction(player);
            int finalCost = (int)(baseManaCost * (1f - reduction));
            finalCost = System.Math.Max(0, finalCost);

            Item.mana = finalCost;
        }

        public override bool CanUseItem(Player player)
        {
            // Reserva Inagotable (Notable): lanzar con <20 maná es gratis.
            if (Systems.NodeEffectSystem.HasNode(player, "mana-notable") && player.statMana < 20)
                return true;
            return player.statMana >= Item.mana;
        }

        /// <summary>
        /// Click izquierdo: dispara ArcaneBolt (magia ofensiva).
        /// Click derecho: invoca un minion cosmico (invocacion).
        /// </summary>
        public override bool AltFunctionUse(Player player)
        {
            // Permitir click derecho para invocar minions
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            var sp = player.GetModPlayer<Players.ShardPlayer>();
            if (sp == null) return false;

            // Click derecho (altUse): invocar minion cosmico
            if (player.altFunctionUse == 2)
            {
                // Invocar un minion cosmico (usamos un proyectil vanilla minion como base)
                // Si el jugador tiene slots de minion disponibles
                int maxMinions = player.maxMinions + Systems.NodeEffectSystem.GetBonusMinionSlots(player);
                if (player.ownedProjectileCounts.Length > 0)
                {
                    // Contar minions actuales
                    int currentMinions = 0;
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        if (Main.projectile[i].active && Main.projectile[i].owner == player.whoAmI &&
                            Main.projectile[i].minion)
                            currentMinions++;
                    }

                    if (currentMinions < maxMinions)
                    {
                        // Invocar un minion cosmico (FlinxMinion como base)
                        int minionType = ProjectileID.FlinxMinion;
                        Projectile.NewProjectile(source, position, Vector2.Zero, minionType, damage, knockback, player.whoAmI);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item113);
                        return false; // no disparar el bolt
                    }
                    else
                    {
                        // No hay slots — mostrar mensaje
                        if (Main.myPlayer == player.whoAmI)
                            Main.NewText($"Slots de minion llenos: {currentMinions}/{maxMinions}. Asigna mas nodos de invocacion.",
                                new Color(255, 120, 120));
                        return false;
                    }
                }
                return false;
            }

            // Click izquierdo: disparar ArcaneBolt (comportamiento normal)
            // Proyectiles extra si tiene nodos
            int extra = Systems.NodeEffectSystem.GetExtraProjectiles(player);
            for (int i = 0; i < extra; i++)
            {
                float angle = (i + 1) * 0.12f * (i % 2 == 0 ? 1f : -1f);
                Vector2 perturbedVel = velocity.RotatedBy(angle);
                Projectile.NewProjectile(source, position, perturbedVel, type, damage, knockback, player.whoAmI);
            }
            return true; // tModLoader dispara el bolt principal
        }

        public override void ModifyTooltips(System.Collections.Generic.List<TooltipLine> tooltips)
        {
            // Añadir barra de XP del fragmento al tooltip
            var sp = Main.LocalPlayer?.GetModPlayer<Players.ShardPlayer>();
            if (sp != null && sp.IsImprinted && sp.ActiveBranch == Players.BranchType.Magic)
            {
                int xpNeeded = sp.XPForNextLevel();
                float pct = xpNeeded > 0 ? (float)sp.ShardXP / xpNeeded : 0f;
                pct = System.Math.Clamp(pct, 0f, 1f);

                // Barra de XP visual con caracteres
                int barLen = 20;
                int filled = (int)(barLen * pct);
                string bar = "[";
                for (int i = 0; i < barLen; i++)
                    bar += i < filled ? "█" : "░";
                bar += "]";

                tooltips.Add(new TooltipLine(Mod, "FragmentLevel", $"[c/FFD700:Nivel {sp.ShardLevel}]") { OverrideColor = new Color(245, 196, 81) });
                tooltips.Add(new TooltipLine(Mod, "FragmentXP", $"{bar} {sp.ShardXP}/{xpNeeded} XP") { OverrideColor = new Color(179, 136, 255) });
                tooltips.Add(new TooltipLine(Mod, "FragmentPts", $"Puntos: {sp.CumulativeSkillPoints() - sp.AllocatedNodes.Count} disponibles") { OverrideColor = new Color(120, 255, 150) });
            }
        }
    }
}
