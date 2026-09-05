using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using AethonMod.Content.Systems;
using AethonMod.Content.Globals;

namespace AethonMod.Content.Weapons
{
    /// <summary>
    /// Grimorio del Eterno — arma mágica híbrida (magia + invocación).
    ///
    /// SISTEMA DE NIVELES:
    /// - Sube de nivel al matar enemigos (XP guardada en ShardLevelItem).
    /// - Niveles infinitos. Sin dependencias de BranchType ni IsImprinted.
    ///
    /// CLICK IZQUIERDO: dispara ArcaneBolt (homing). Cuesta 3 mana (+3 cada 20 niveles).
    /// CLICK DERECHO: invoca CosmicOrbMinion. Cuesta 15 mana (+1 por nivel).
    ///
    /// ESCALADO POR NIVEL:
    /// - +2.2% daño mágico, +1% daño summon, +0.2% crit, +0.4% armor pen
    /// - -0.3% use time (tope -25%)
    /// - +1 slot de minion cada 5 niveles
    /// - +1 bolt extra cada 5 niveles
    /// - Lifesteal nivel 7+: +0.1% cada 7 niveles
    /// - Bonus por mana faltante: +0.5% daño por 1% mana faltante (tope +50%)
    /// </summary>
    public class GrimoireEternal : ModItem
    {
        public override void OnCraft(Recipe recipe)
        {
            // Disparar evento cinematográfico al craftear (solo sin lore por ahora).
            var sp = Main.LocalPlayer?.GetModPlayer<Players.ShardPlayer>();
            if (sp != null && !sp.FirstLevelUpTriggered && Main.myPlayer == Main.LocalPlayer.whoAmI)
            {
                sp.FirstLevelUpTriggered = true;
                sp.ActiveBranch = Players.BranchType.Magic;
                LevelUpEventSystem.Trigger(false);
            }
        }

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 11;
            Item.DamageType = DamageClass.Magic;
            Item.width = 30;
            Item.height = 38;
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
            Item.mana = 3;
            Item.noMelee = true;
        }

        /// <summary>
        /// Obtiene el ShardLevelItem del arma. Defensivo: try/catch.
        /// </summary>
        private ShardLevelItem? GetShard(Item item)
        {
            try { return item.GetGlobalItem<ShardLevelItem>(); }
            catch { return null; }
        }

        // ================================================================
        //  ESCALADO DE DAÑO
        // ================================================================

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            var sl = GetShard(Item);
            if (sl == null) return;

            // +2.2% daño mágico por nivel
            damage *= WeaponScaling.MagicDamageMult(sl.Level);

            // +1% daño de invocación por nivel
            player.GetDamage(DamageClass.Summon) += WeaponScaling.SummonDamageBonus(sl.Level);

            // +0.2% critico magico por nivel
            player.GetCritChance(DamageClass.Magic) += WeaponScaling.CritBonus(sl.Level);

            // +0.4% armor penetration por nivel
            player.GetArmorPenetration(DamageClass.Magic) += WeaponScaling.ArmorPenBonus(sl.Level);

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
            return WeaponScaling.UseSpeedMult(sl.Level);
        }

        // ================================================================
        //  USO DEL ARMA
        // ================================================================

        public override bool CanUseItem(Player player)
        {
            var sl = GetShard(Item);
            int level = sl?.Level ?? 1;

            // Click derecho (minion): requiere mana del minion
            if (player.altFunctionUse == 2)
            {
                int minionCost = WeaponScaling.MinionManaCost(level);
                return player.statMana >= minionCost;
            }
            // Click izquierdo (bolt): requiere mana del grimorio
            return player.statMana >= Item.mana;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            var sl = GetShard(Item);
            int level = sl?.Level ?? 1;

            // === CLICK DERECHO: invocar minion ===
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

                if (currentMinions >= maxMinions)
                {
                    if (Main.myPlayer == player.whoAmI)
                        Main.NewText($"Slots de minion llenos: {currentMinions}/{maxMinions}.",
                            new Color(255, 120, 120));
                    return false;
                }

                // Cobrar mana del minion
                int minionCost = WeaponScaling.MinionManaCost(level);
                if (player.statMana < minionCost) return false;
                player.statMana -= minionCost;
                if (player.statMana < 0) player.statMana = 0;

                // Invocar minion
                player.AddBuff(ModContent.BuffType<global::AethonMod.Content.Buffs.CosmicOrbBuff>(), 18000);
                Projectile.NewProjectile(source, position, Vector2.Zero,
                    ModContent.ProjectileType<global::AethonMod.Content.Projectiles.CosmicOrbMinion>(),
                    damage, knockback, player.whoAmI);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item113);
                return false;
            }

            // === CLICK IZQUIERDO: ArcaneBolt + bolts extra ===
            int extra = WeaponScaling.ExtraProjectiles(level);
            for (int i = 0; i < extra; i++)
            {
                float angle = (i + 1) * 0.12f * (i % 2 == 0 ? 1f : -1f);
                Vector2 perturbedVel = velocity.RotatedBy(angle);
                Projectile.NewProjectile(source, position, perturbedVel, type, damage, knockback, player.whoAmI);
            }
            return true; // tModLoader dispara el bolt principal
        }

        // ================================================================
        //  TOOLTIP
        // ================================================================

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // Ocultar línea vanilla "Level: X"
            for (int i = tooltips.Count - 1; i >= 0; i--)
            {
                string t = tooltips[i].Text ?? "";
                if (tooltips[i].Name == "Level" || tooltips[i].Name == "ItemLevel" ||
                    t.StartsWith("Level:") || t.StartsWith("Level："))
                    tooltips.RemoveAt(i);
            }

            var sl = GetShard(Item);
            if (sl == null) return;

            // Línea de daño de invocación (híbrido)
            int insertIndex = -1;
            for (int i = 0; i < tooltips.Count; i++)
            {
                if (tooltips[i].Name == "Damage" || tooltips[i].Name == "Knockback")
                {
                    insertIndex = i + 1;
                    break;
                }
            }
            int summonDmg = (int)(Item.damage * (1f + sl.Level * WeaponScaling.SummonDamagePerLevel));
            if (insertIndex >= 0)
            {
                tooltips.Insert(insertIndex, new TooltipLine(Mod, "SummonDamage",
                    $"[c/BE78FD:{summonDmg} daño de invocación]"));
            }

            // Barra de XP
            int xpNeeded = sl.XPForNextLevel();
            float pct = xpNeeded > 0 ? (float)sl.XP / xpNeeded : 0f;
            pct = System.Math.Clamp(pct, 0f, 1f);
            int barLen = 20;
            int filled = (int)(barLen * pct);
            string bar = "[";
            for (int i = 0; i < barLen; i++)
                bar += i < filled ? "█" : "░";
            bar += "]";

            tooltips.Add(new TooltipLine(Mod, "Level", $"[c/FFD700:Nivel {sl.Level}]"));
            tooltips.Add(new TooltipLine(Mod, "XP", $"[c/B388FF:{bar} {sl.XP}/{xpNeeded} XP]"));

            // Stats de escalado
            int manaCost = WeaponScaling.ManaCost(sl.Level);
            int minionCost = WeaponScaling.MinionManaCost(sl.Level);
            int bonusSlots = WeaponScaling.BonusMinionSlots(sl.Level);
            float lowManaBonus = (WeaponScaling.LowManaDamageMult(Main.LocalPlayer.statMana, Main.LocalPlayer.statManaMax2) - 1f) * 100f;

            tooltips.Add(new TooltipLine(Mod, "Scaling",
                $"[c/FFD700:Escalado:] " +
                $"[c/FF5555:+{(int)(sl.Level * WeaponScaling.MagicDamagePerLevel * 100)}% mágico] " +
                $"[c/BE78FD:+{(int)(sl.Level * WeaponScaling.SummonDamagePerLevel * 100)}% summon] " +
                $"[c/FFAA55:+{WeaponScaling.CritBonus(sl.Level):F1}% crit] " +
                $"[c/78FF96:+{bonusSlots} slots minion]"));

            tooltips.Add(new TooltipLine(Mod, "Mana",
                $"[c/55AAFF:Bolt: {manaCost} mana | Minion: {minionCost} mana]"));

            tooltips.Add(new TooltipLine(Mod, "LowMana",
                $"[c/FF5555:★ Bonus mana faltante: +{lowManaBonus:F1}% daño (tope +50%)]"));

            // Lifesteal
            if (WeaponScaling.HasLifesteal(sl.Level))
            {
                float lsPct = WeaponScaling.LifestealPercent(sl.Level) * 100f;
                int nextLs = ((sl.Level / 7) + 1) * 7;
                tooltips.Add(new TooltipLine(Mod, "Lifesteal",
                    $"[c/FF5566:♥ Lifesteal: +{lsPct:F1}% (próximo +0.1% en nivel {nextLs})]"));
            }
            else
            {
                tooltips.Add(new TooltipLine(Mod, "LifestealLocked",
                    $"[c/78788C:♥ Lifesteal se desbloquea en nivel 7]"));
            }

            // Hitos
            int nextMilestone = ((sl.Level / 5) + 1) * 5;
            if (sl.Level >= 5)
            {
                tooltips.Add(new TooltipLine(Mod, "Milestones", "[c/78FF96:★ Hitos:]"));
                foreach (var m in WeaponScaling.MilestonesReached(sl.Level))
                    tooltips.Add(new TooltipLine(Mod, "MS_" + m, $"  [c/78FF96:{m}]"));
            }
            tooltips.Add(new TooltipLine(Mod, "NextMilestone",
                $"[c/78788C:Próximo hito nivel {nextMilestone}: {WeaponScaling.MilestoneDescription(nextMilestone / 5)}]"));
        }
    }
}
