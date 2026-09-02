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
    /// Lumina, la Arcoestelar — arma de la rama de Distancia.
    ///
    /// SISTEMA DE NIVELES POR ITEM: cada copia tiene su propio nivel/XP.
    /// Solo sube de nivel el item sostenido cuando matas enemigos.
    ///
    /// ESCALADO POR NIVEL (INFINITO):
    /// - Cada nivel: +2% daño, +0.2% crit, +0.4% armor pen
    /// - Cada 5 niveles: bonus de hito (flecha extra, crit, velocidad, etc.)
    /// </summary>
    public class LuminaStarbow : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName / Tooltip cargados desde Localization.
        }

        public override void SetDefaults()
        {
            Item.damage = 12;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 40;
            Item.height = 60;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 2f;
            Item.value = Item.buyPrice(0, 10, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item5;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<Projectiles.StarlightArrow>();
            Item.shootSpeed = 14f;
            Item.useAmmo = AmmoID.Arrow;
            Item.noMelee = true;
        }

        private ShardLevelItem GetShard(Item item) => item.GetGlobalItem<ShardLevelItem>();

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            var sl = GetShard(Item);
            if (sl == null) return;
            var sp = player.GetModPlayer<ShardPlayer>();
            if (sp == null || !sp.IsImprinted || sp.ActiveBranch != BranchType.Distance) return;

            damage *= WeaponScaling.DamageMult(BranchType.Distance, sl.Level);
            player.GetCritChance(DamageClass.Ranged) += WeaponScaling.CritBonus(sl.Level);
            player.GetArmorPenetration(DamageClass.Ranged) += WeaponScaling.ArmorPenBonus(sl.Level);
        }

        public override float UseTimeMultiplier(Player player)
        {
            var sl = GetShard(Item);
            if (sl == null) return 1f;
            var sp = player.GetModPlayer<ShardPlayer>();
            if (sp == null || !sp.IsImprinted || sp.ActiveBranch != BranchType.Distance) return 1f;
            return WeaponScaling.UseSpeedMult(sl.Level);
        }

        public override bool CanConsumeAmmo(Item ammo, Player player) => false;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            var sl = GetShard(Item);
            if (sl == null) return true;
            var sp = player.GetModPlayer<ShardPlayer>();
            if (sp == null || !sp.IsImprinted || sp.ActiveBranch != BranchType.Distance) return true;

            int extra = WeaponScaling.ExtraProjectiles(BranchType.Distance, sl.Level);
            for (int i = 0; i < extra; i++)
            {
                float angle = (i + 1) * 0.15f * (i % 2 == 0 ? 1f : -1f);
                Vector2 perturbedVel = velocity.RotatedBy(angle);
                Projectile.NewProjectile(source, position, perturbedVel, type, damage, knockback, player.whoAmI);
            }
            return true;
        }

        public override Vector2? HoldoutOffset() => new Vector2(2f, 0f);

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
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

            var sl = GetShard(Item);
            if (sl == null) return;
            var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
            if (sp == null || !sp.IsImprinted || sp.ActiveBranch != BranchType.Distance) return;

            int xpNeeded = sl.XPForNextLevel();
            float pct = xpNeeded > 0 ? (float)sl.XP / xpNeeded : 0f;
            pct = System.Math.Clamp(pct, 0f, 1f);
            int barLen = 20;
            int filled = (int)(barLen * pct);
            string bar = "[";
            for (int i = 0; i < barLen; i++)
                bar += i < filled ? "█" : "░";
            bar += "]";

            tooltips.Add(new TooltipLine(Mod, "FragmentLevel", $"[c/FFD700:Nivel {sl.Level}]") { OverrideColor = new Color(245, 196, 81) });
            tooltips.Add(new TooltipLine(Mod, "FragmentXP", $"{bar} {sl.XP}/{xpNeeded} XP") { OverrideColor = new Color(179, 136, 255) });

            tooltips.Add(new TooltipLine(Mod, "ScalingStats",
                $"[c/FFD700:Escalado por nivel:] " +
                $"[c/FF5555:+{(int)(sl.Level * WeaponScaling.RangedDamagePerLevel * 100)}% daño] " +
                $"[c/FFAA55:+{WeaponScaling.CritBonus(sl.Level):F1}% crit] " +
                $"[c/55AAFF:+{sl.Level * 0.4f:F1}% armor pen]"));

            int nextMilestone = ((sl.Level / 5) + 1) * 5;

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

            if (sl.Level >= 5)
            {
                tooltips.Add(new TooltipLine(Mod, "MilestoneHeader", "[c/78FF96:★ Hitos alcanzados:]"));
                var milestones = WeaponScaling.MilestonesReached(BranchType.Distance, sl.Level);
                foreach (var m in milestones)
                    tooltips.Add(new TooltipLine(Mod, "Milestone_" + m, "  " + m));
            }
            tooltips.Add(new TooltipLine(Mod, "NextMilestone",
                $"[c/78788C:Próximo hito nivel {nextMilestone}: {WeaponScaling.MilestoneDescription(BranchType.Distance, nextMilestone / 5)}]"));
        }
    }
}
