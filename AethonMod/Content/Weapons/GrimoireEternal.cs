using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using AethonMod.Content.Players;
using AethonMod.Content.Systems;
using AethonMod.Content.Globals;

namespace AethonMod.Content.Weapons
{
    /// <summary>
    /// Grimorio del Eterno — arma de la rama de Artes Mágicas.
    ///
    /// SISTEMA DE NIVELES POR ITEM: cada copia tiene su propio nivel/XP.
    /// Solo sube de nivel el item sostenido cuando matas enemigos.
    ///
    /// MANA: base 3, +3 cada 20 niveles, tope 30.
    /// BONUS DE DAÑO POR MANA FALTANTE: +0.5% daño por 1% mana faltante (tope +50%).
    /// LIFESTEAL: desbloqueado nivel 7, +0.1% cada 7 niveles.
    /// ESCALADO: +2.2% daño mágico, +1% summon, +0.2% crit por nivel.
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
            Item.shoot = ModContent.ProjectileType<global::AethonMod.Content.Weapons.Projectiles.ArcaneBolt>();
            Item.shootSpeed = 12f;
            Item.mana = 3;
            Item.noMelee = true;
        }

        private ShardLevelItem GetShard(Item item) => item.GetGlobalItem<ShardLevelItem>();

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            var sl = GetShard(Item);
            if (sl == null) return;
            var sp = player.GetModPlayer<ShardPlayer>();
            if (sp == null || !sp.IsImprinted || sp.ActiveBranch != BranchType.Magic) return;

            damage *= WeaponScaling.DamageMult(BranchType.Magic, sl.Level);
            player.GetDamage(DamageClass.Summon) += sl.Level * WeaponScaling.SummonDamagePerLevel;
            player.GetCritChance(DamageClass.Magic) += WeaponScaling.CritBonus(sl.Level);

            // BONUS POR MANA FALTANTE (aplica a magia Y summon)
            float manaMult = WeaponScaling.LowManaDamageMult(player.statMana, player.statManaMax2);
            damage *= manaMult;
            player.GetDamage(DamageClass.Summon) *= manaMult;
        }

        public override void ModifyManaCost(Player player, ref float reduce, ref float mult)
        {
            var sl = GetShard(Item);
            if (sl == null) return;
            Item.mana = WeaponScaling.ManaCost(sl.Level);
        }

        public override float UseTimeMultiplier(Player player)
        {
            var sl = GetShard(Item);
            if (sl == null) return 1f;
            var sp = player.GetModPlayer<ShardPlayer>();
            if (sp == null || !sp.IsImprinted || sp.ActiveBranch != BranchType.Magic) return 1f;
            return WeaponScaling.UseSpeedMult(sl.Level);
        }

        public override bool CanUseItem(Player player) => player.statMana >= Item.mana;

        public override bool AltFunctionUse(Player player) => true;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            var sl = GetShard(Item);
            if (sl == null) return false;
            var sp = player.GetModPlayer<ShardPlayer>();
            if (sp == null) return false;

            // Click derecho (altUse): invocar minion cosmico
            if (player.altFunctionUse == 2)
            {
                if (sp.ActiveBranch != BranchType.Magic) return false;
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

            if (!sp.IsImprinted || sp.ActiveBranch != BranchType.Magic) return true;

            // Click izquierdo: ArcaneBolt + bolts extra segun hitos
            int extra = WeaponScaling.ExtraProjectiles(BranchType.Magic, sl.Level);
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
            var sl = GetShard(Item);
            if (sl == null) return;
            var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
            if (sp == null) return;

            // === OCULTAR LINEA VANILLA "Level: X" ===
            for (int i = tooltips.Count - 1; i >= 0; i--)
            {
                string t = tooltips[i].Text ?? "";
                bool isLevelLine =
                    tooltips[i].Name == "Level" ||
                    tooltips[i].Name == "ItemLevel" ||
                    t.StartsWith("Level:") ||
                    t.StartsWith("Level：");
                if (isLevelLine) tooltips.RemoveAt(i);
            }

            // Línea de daño de invocación
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
                summonDmg = (int)(Item.damage * (1f + sl.Level * WeaponScaling.SummonDamagePerLevel));
                lowManaBonus = (WeaponScaling.LowManaDamageMult(Main.LocalPlayer.statMana, Main.LocalPlayer.statManaMax2) - 1f) * 100f;
            }
            if (insertIndex >= 0)
            {
                tooltips.Insert(insertIndex, new TooltipLine(Mod, "SummonDamage",
                    $"[c/BE78FD:{summonDmg} daño de invocación]"));
            }

            if (!sp.IsImprinted || sp.ActiveBranch != BranchType.Magic) return;

            int xpNeeded = sl.XPForNextLevel();
            float pct = xpNeeded > 0 ? (float)sl.XP / xpNeeded : 0f;
            pct = System.Math.Clamp(pct, 0f, 1f);
            int barLen = 20;
            int filled = (int)(barLen * pct);
            string bar = "[";
            for (int i = 0; i < barLen; i++)
                bar += i < filled ? "█" : "░";
            bar += "]";

            tooltips.Add(new TooltipLine(Mod, "FragmentLevel", $"[c/FFD700:Nivel {sl.Level}]"));
            tooltips.Add(new TooltipLine(Mod, "FragmentXP", $"[c/B388FF:{bar} {sl.XP}/{xpNeeded} XP]"));

            int manaCost = WeaponScaling.ManaCost(sl.Level);
            int nextManaLevel = ((sl.Level / 20) + 1) * 20;
            tooltips.Add(new TooltipLine(Mod, "ManaInfo",
                $"[c/55AAFF:Mana: {manaCost} por uso (sube +3 cada 20 niveles, próxima en nivel {nextManaLevel})]"));

            int bonusSlots = WeaponScaling.BonusMinionSlots(sl.Level);
            tooltips.Add(new TooltipLine(Mod, "ScalingStats",
                $"[c/FFD700:Escalado por nivel:] " +
                $"[c/FF5555:+{(int)(sl.Level * WeaponScaling.MagicDamagePerLevel * 100)}% daño mágico] " +
                $"[c/BE78FD:+{(int)(sl.Level * WeaponScaling.SummonDamagePerLevel * 100)}% summon] " +
                $"[c/78FF96:+{bonusSlots} slots minion] " +
                $"[c/FFAA55:+{WeaponScaling.CritBonus(sl.Level):F1}% crit]"));

            tooltips.Add(new TooltipLine(Mod, "LowManaBonus",
                $"[c/FF5555:★ Bonus actual por mana faltante: +{lowManaBonus:F1}% daño mágico Y de invocación] " +
                $"[c/78788C:(tope +50% a mana vacío)]"));

            // Lifesteal (desbloqueado a nivel 7)
            if (WeaponScaling.HasLifesteal(sl.Level))
            {
                float lsPct = WeaponScaling.LifestealPercent(sl.Level) * 100f;
                int nextLsLevel = ((sl.Level / 7) + 1) * 7;
                tooltips.Add(new TooltipLine(Mod, "LifestealInfo",
                    $"[c/FF5566:♥ Curación: +{lsPct:F1}% del daño causado (sube +0.1% cada 7 niveles, próximo nivel {nextLsLevel})]"));
            }
            else
            {
                tooltips.Add(new TooltipLine(Mod, "LifestealLocked",
                    $"[c/78788C:♥ Curación por ataque se desbloquea en nivel 7]"));
            }

            if (bonusSlots > 0)
            {
                tooltips.Add(new TooltipLine(Mod, "MinionStackInfo",
                    $"[c/78FF96:★ Los {bonusSlots} slots extra se suman a los de tu armadura de invocador]"));
            }

            int nextMilestone = ((sl.Level / 5) + 1) * 5;
            if (sl.Level >= 5)
            {
                tooltips.Add(new TooltipLine(Mod, "MilestoneHeader", "[c/78FF96:★ Hitos alcanzados:]"));
                var milestones = WeaponScaling.MilestonesReached(BranchType.Magic, sl.Level);
                foreach (var m in milestones)
                    tooltips.Add(new TooltipLine(Mod, "Milestone_" + m, "  " + m));
            }
            tooltips.Add(new TooltipLine(Mod, "NextMilestone",
                $"[c/78788C:Próximo hito nivel {nextMilestone}: {WeaponScaling.MilestoneDescription(BranchType.Magic, nextMilestone / 5)}]"));
        }
    }
}
