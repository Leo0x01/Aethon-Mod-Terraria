using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using AethonMod.Content.Players;
using AethonMod.Content.Systems;

namespace AethonMod.Content.Weapons
{
    /// <summary>
    /// Grimorio del Eterno — arma de la rama de Artes Mágicas.
    ///
    /// FUNCIONES:
    /// - Click izquierdo: dispara ArcaneBolt (magia ofensiva)
    /// - Click derecho: invoca un minion cosmico (invocacion)
    ///
    /// MANA:
    /// - Costo FIJO: base 3, +3 cada 20 niveles, tope 30.
    /// - Nivel 1-19: 3 mana | Nivel 20-39: 6 | Nivel 40-59: 9 | ... | Nivel 200+: 30
    ///
    /// BONUS DE DAÑO POR MANA FALTANTE:
    /// - +0.5% daño por cada 1% de mana faltante (tope +50%).
    /// - A mana full: sin bonus. A 50% mana: +25% daño. A 10% mana: +45% daño.
    ///
    /// ESCALADO POR NIVEL DEL FRAGMENTO (INFINITO):
    /// - Cada nivel: +2.2% daño magico, +1% daño summon, +0.2% crit
    /// - Cada 5 niveles: bonus de hito (slots de minion, bolts extra, velocidad, crit)
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
            Item.mana = 3; // mana base fijo (se ajusta en ModifyManaCost)
            Item.noMelee = true;
        }

        /// <summary>
        /// Daño híbrido: magia + invocacion. Escala con nivel del fragmento.
        /// Bonus por % mana faltante (a menos mana, mas daño).
        /// </summary>
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            var sp = player.GetModPlayer<ShardPlayer>();
            if (sp == null || !sp.IsImprinted || sp.ActiveBranch != BranchType.Magic) return;

            // Cada nivel: +2.2% daño mágico (infinito)
            damage *= WeaponScaling.DamageMult(BranchType.Magic, sp.ShardLevel);

            // Cada nivel: +1% daño de invocacion
            player.GetDamage(DamageClass.Summon) += sp.ShardLevel * WeaponScaling.SummonDamagePerLevel;

            // Cada nivel: +0.2% critico magico
            player.GetCritChance(DamageClass.Magic) += WeaponScaling.CritBonus(sp.ShardLevel);

            // === BONUS POR MANA FALTANTE (aplica a magia Y summon) ===
            // A menos mana tengas, mas daño haces (tope +50%).
            float manaMult = WeaponScaling.LowManaDamageMult(player.statMana, player.statManaMax2);
            damage *= manaMult;
            player.GetDamage(DamageClass.Summon) *= manaMult; // TAMBIEN aplica a summon
        }

        /// <summary>
        /// Mana: base 3, +3 cada 20 niveles, tope 30.
        /// </summary>
        public override void ModifyManaCost(Player player, ref float reduce, ref float mult)
        {
            var sp = player.GetModPlayer<ShardPlayer>();
            if (sp == null) return;
            Item.mana = WeaponScaling.ManaCost(sp.ShardLevel);
        }

        public override float UseTimeMultiplier(Player player)
        {
            var sp = player.GetModPlayer<ShardPlayer>();
            if (sp == null || !sp.IsImprinted || sp.ActiveBranch != BranchType.Magic) return 1f;
            // Cada nivel: -0.3% use time (mas rapido), tope -25%
            return WeaponScaling.UseSpeedMult(sp.ShardLevel);
        }

        public override bool CanUseItem(Player player) => player.statMana >= Item.mana;

        public override bool AltFunctionUse(Player player) => true;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            var sp = player.GetModPlayer<ShardPlayer>();
            if (sp == null) return false;

            // Click derecho (altUse): invocar minion cosmico
            if (player.altFunctionUse == 2)
            {
                int maxMinions = player.maxMinions;
                int currentMinions = 0;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i].active && Main.projectile[i].owner == player.whoAmI &&
                        Main.projectile[i].minion)
                        currentMinions++;
                }

                if (currentMinions < maxMinions)
                {
                    int minionType = ModContent.ProjectileType<global::AethonMod.Content.Projectiles.CosmicOrbMinion>();
                    int buffType = ModContent.BuffType<global::AethonMod.Content.Buffs.CosmicOrbBuff>();
                    player.AddBuff(buffType, 18000);
                    Projectile.NewProjectile(source, position, Vector2.Zero, minionType, damage, knockback, player.whoAmI);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item113);
                    return false;
                }
                else
                {
                    if (Main.myPlayer == player.whoAmI)
                        Main.NewText($"Slots de minion llenos: {currentMinions}/{maxMinions}. Sube de nivel (cada 5 niveles da +1 slot) o usa armadura de invocador.",
                            new Color(255, 120, 120));
                    return false;
                }
            }

            // Click izquierdo: ArcaneBolt + bolts extra segun hitos
            int extra = WeaponScaling.ExtraProjectiles(BranchType.Magic, sp.ShardLevel);
            for (int i = 0; i < extra; i++)
            {
                float angle = (i + 1) * 0.12f * (i % 2 == 0 ? 1f : -1f);
                Vector2 perturbedVel = velocity.RotatedBy(angle);
                Projectile.NewProjectile(source, position, perturbedVel, type, damage, knockback, player.whoAmI);
            }
            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
            if (sp == null) return;

            // === OCULTAR LINEA VANILLA "Level: X" (por contenido de texto) ===
            // tModLoader agrega una linea "Level: N" azul que confunde con nuestro nivel.
            // El Name del TooltipLine puede variar segun la version, asi que filtramos
            // tanto por Name como por contenido de texto.
            for (int i = tooltips.Count - 1; i >= 0; i--)
            {
                string t = tooltips[i].Text ?? "";
                bool isLevelLine =
                    tooltips[i].Name == "Level" ||
                    tooltips[i].Name == "ItemLevel" ||
                    t.StartsWith("Level:") ||
                    t.StartsWith("Level："); // variante unicode
                if (isLevelLine) tooltips.RemoveAt(i);
            }

            // Línea de daño de invocacion (híbrido)
            int insertIndex = -1;
            for (int i = 0; i < tooltips.Count; i++)
            {
                if (tooltips[i].Name == "Damage" || tooltips[i].Name == "Knockback")
                {
                    insertIndex = i + 1;
                    break;
                }
            }
            int summonDmg = Item.damage;
            float lowManaBonus = 0f;
            if (sp.IsImprinted && sp.ActiveBranch == BranchType.Magic)
            {
                summonDmg = (int)(Item.damage * (1f + sp.ShardLevel * WeaponScaling.SummonDamagePerLevel));
                // Calcular bonus actual por mana faltante
                lowManaBonus = (WeaponScaling.LowManaDamageMult(Main.LocalPlayer.statMana, Main.LocalPlayer.statManaMax2) - 1f) * 100f;
            }
            if (insertIndex >= 0)
            {
                tooltips.Insert(insertIndex, new TooltipLine(Mod, "SummonDamage",
                    $"[c/BE78FD:{summonDmg} daño de invocación]"));
            }

            // Stats del fragmento
            if (!sp.IsImprinted || sp.ActiveBranch != BranchType.Magic) return;

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

            // Info de mana
            int manaCost = WeaponScaling.ManaCost(sp.ShardLevel);
            int nextManaLevel = ((sp.ShardLevel / 20) + 1) * 20;
            tooltips.Add(new TooltipLine(Mod, "ManaInfo",
                $"[c/55AAFF:Mana: {manaCost} por uso (sube +3 cada 20 niveles, próxima en nivel {nextManaLevel})]"));

            // Stats actuales por nivel
            int bonusSlots = WeaponScaling.BonusMinionSlots(sp.ShardLevel);
            tooltips.Add(new TooltipLine(Mod, "ScalingStats",
                $"[c/FFD700:Escalado por nivel:] " +
                $"[c/FF5555:+{(int)(sp.ShardLevel * WeaponScaling.MagicDamagePerLevel * 100)}% daño mágico] " +
                $"[c/BE78FD:+{(int)(sp.ShardLevel * WeaponScaling.SummonDamagePerLevel * 100)}% summon] " +
                $"[c/78FF96:+{bonusSlots} slots minion] " +
                $"[c/FFAA55:+{WeaponScaling.CritBonus(sp.ShardLevel):F1}% crit]"));

            // Bonus por mana faltante (mecánica activa ahora)
            tooltips.Add(new TooltipLine(Mod, "LowManaBonus",
                $"[c/FF5555:★ Bonus actual por mana faltante: +{lowManaBonus:F1}% daño mágico Y de invocación] " +
                $"[c/78788C:(tope +50% a mana vacío)]"));

            // Lifesteal (desbloqueado a nivel 7)
            if (WeaponScaling.HasLifesteal(sp.ShardLevel))
            {
                float lsPct = WeaponScaling.LifestealPercent(sp.ShardLevel) * 100f;
                int nextLsLevel = ((sp.ShardLevel / 7) + 1) * 7;
                tooltips.Add(new TooltipLine(Mod, "LifestealInfo",
                    $"[c/FF5566:♥ Curación: +{lsPct:F1}% del daño causado (sube +0.1% cada 7 niveles, próximo nivel {nextLsLevel})]"));
            }
            else
            {
                int nextLsLevel = 7;
                tooltips.Add(new TooltipLine(Mod, "LifestealLocked",
                    $"[c/78788C:♥ Curación por ataque se desbloquea en nivel 7]"));
            }

            // Nota sobre stack con armadura
            if (bonusSlots > 0)
            {
                tooltips.Add(new TooltipLine(Mod, "MinionStackInfo",
                    $"[c/78FF96:★ Los {bonusSlots} slots extra se suman a los de tu armadura de invocador]"));
            }

            // Hito alcanzado (cada 5 niveles)
            int nextMilestone = ((sp.ShardLevel / 5) + 1) * 5;
            if (sp.ShardLevel >= 5)
            {
                tooltips.Add(new TooltipLine(Mod, "MilestoneHeader", "[c/78FF96:★ Hitos alcanzados:]"));
                var milestones = WeaponScaling.MilestonesReached(BranchType.Magic, sp.ShardLevel);
                foreach (var m in milestones)
                    tooltips.Add(new TooltipLine(Mod, "Milestone_" + m, "  " + m));
            }
            tooltips.Add(new TooltipLine(Mod, "NextMilestone",
                $"[c/78788C:Próximo hito nivel {nextMilestone}: {WeaponScaling.MilestoneDescription(BranchType.Magic, nextMilestone / 5)}]"));
        }
    }
}
