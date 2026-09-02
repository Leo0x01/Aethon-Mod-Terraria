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
            // Daño HÍBRIDO: Magic + Summon fusionados (rama Artes Mágicas)
            // El item base es Magic, pero aplicamos bonus de Summon en ModifyWeaponDamage
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
            Item.shoot = ModContent.ProjectileType<global::AethonMod.Content.Weapons.Projectiles.ArcaneBolt>();
            Item.shootSpeed = 12f;
            Item.mana = 0; // Base: NO consume mana.
            Item.noMelee = true;
        }

        /// <summary>
        /// El Grimorio es un arma HÍBRIDA: aplica tanto daño mágico como de invocación.
        /// Click izquierdo: daño mágico (ArcaneBolt)
        /// Click derecho: daño de invocación (CosmicOrbMinion)
        /// </summary>
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            var sp = player.GetModPlayer<Players.ShardPlayer>();
            if (sp != null && sp.IsImprinted && sp.ActiveBranch == Players.BranchType.Magic)
            {
                // Escalado porcentual moderado: +2.2% por nivel (daño mágico)
                damage *= 1f + sp.ShardLevel * 0.022f;
            }
            float crit = 0;
            
            player.GetCritChance(DamageClass.Magic) += crit;

            // === DAÑO HÍBRIDO: también aplica bonus de daño de invocación ===
            // El grimorio beneficia tanto hechizos como minions
            if (sp != null && sp.IsImprinted && sp.ActiveBranch == Players.BranchType.Magic)
            {
                // +1% summon damage por nivel del fragmento
                player.GetDamage(DamageClass.Summon) += sp.ShardLevel * 0.01f;
                // Bonus de slots de minion de los nodos del arbol
                int bonusSlots = 0;
                player.maxMinions += bonusSlots;
                // Crit chance de summon (normalmente 0, pero le damos un poco)
                player.GetCritChance(DamageClass.Summon) += crit * 0.5f;
            }
        }

        public override void ModifyManaCost(Player player, ref float reduce, ref float mult)
        {
            var sp = player.GetModPlayer<Players.ShardPlayer>();
            if (sp == null) return;

            // Mana: base 0, escala con nivel del fragmento
            int baseManaCost = System.Math.Min(sp.ShardLevel / 5, 20);
            float reduction = 0f; // Sin NodeEffectSystem
            int finalCost = (int)(baseManaCost * (1f - reduction));
            finalCost = System.Math.Max(0, finalCost);
            Item.mana = finalCost;
        }

        public override float UseTimeMultiplier(Player player)
        {
            // Velocidad de lanzamiento de nodos del arbol Magic (cast-speed, ascend-5)
            float mult = 1f;
            var sp = player.GetModPlayer<Players.ShardPlayer>();
            if (sp != null && sp.IsImprinted && sp.ActiveBranch == Players.BranchType.Magic)
            {
                // cast-speed: +3/4/5% velocidad
                if (false) mult *= 0.97f;
                if (false) mult *= 0.96f;
                if (false) mult *= 0.95f;
                if (false) mult *= 0.85f;
                // ascend-5: +100% velocidad
                if (false) mult *= 0.50f;
            }
            return mult;
        }

        public override bool CanUseItem(Player player)
        {
            // Reserva Inagotable (Notable): lanzar con <20 maná es gratis.
            if (false && player.statMana < 20)
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
                // NOTA: no sumar GetBonusMinionSlots aqui — ya fue sumado en ModifyWeaponDamage
                int maxMinions = player.maxMinions;
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
                        // Invocar el CosmicOrbMinion (esfera de luz cosmica)
                        int minionType = ModContent.ProjectileType<global::AethonMod.Content.Projectiles.CosmicOrbMinion>();
                        int buffType = ModContent.BuffType<global::AethonMod.Content.Buffs.CosmicOrbBuff>();
                        // Añadir el buff al jugador (aparece en zona de buffs)
                        player.AddBuff(buffType, 18000);
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
            int extra = 0;
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
            var sp = Main.LocalPlayer?.GetModPlayer<Players.ShardPlayer>();
            if (sp == null) return;

            // === AÑADIR LÍNEA DE DAÑO DE INVOCACIÓN ===
            // Encontrar la línea de daño mágico y añadir summon damage después
            int insertIndex = -1;
            for (int i = 0; i < tooltips.Count; i++)
            {
                if (tooltips[i].Name == "Damage" || tooltips[i].Name == "Knockback")
                {
                    insertIndex = i + 1;
                    break;
                }
            }

            // Calcular daño de invocación
            int summonDmg = Item.damage;
            if (sp.IsImprinted && sp.ActiveBranch == Players.BranchType.Magic)
            {
                summonDmg = (int)(Item.damage * (1f + sp.ShardLevel * 0.01f));
            }

            if (insertIndex >= 0)
            {
                tooltips.Insert(insertIndex, new TooltipLine(Mod, "SummonDamage",
                    $"[c/BE78FD:{summonDmg} daño de invocación]"));
            }

            // === BARRA DE XP DEL FRAGMENTO ===
            if (sp.IsImprinted && sp.ActiveBranch == Players.BranchType.Magic)
            {
                int xpNeeded = sp.XPForNextLevel();
                float pct = xpNeeded > 0 ? (float)sp.ShardXP / xpNeeded : 0f;
                pct = System.Math.Clamp(pct, 0f, 1f);

                int barLen = 20;
                int filled = (int)(barLen * pct);
                string bar = "[";
                for (int i = 0; i < barLen; i++)
                    bar += i < filled ? "█" : "░";
                bar += "]";

                tooltips.Add(new TooltipLine(Mod, "FragmentLevel", $"[c/FFD700:Nivel {sp.ShardLevel}]"));
                tooltips.Add(new TooltipLine(Mod, "FragmentXP", $"[c/B388FF:{bar} {sp.ShardXP}/{xpNeeded} XP]"));
            }
        }
    }
}
