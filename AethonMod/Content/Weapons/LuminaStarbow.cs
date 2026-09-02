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
    /// Lumina, la Arcoestelar — arma de la rama de Distancia.
    /// Arco que dispara flechas de luz estelar (no consume municion base).
    ///
    /// ESCALADO POR NIVEL DEL FRAGMENTO:
    /// - Cada nivel: +2% daño, +0.2% crit, +0.4% armor pen
    /// - Cada 5 niveles: bonus de hito acumulativo (flecha extra, crit, velocidad, etc.)
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

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            var sp = player.GetModPlayer<ShardPlayer>();
            if (sp == null || !sp.IsImprinted || sp.ActiveBranch != BranchType.Distance) return;

            // Cada nivel: +2% daño
            damage *= WeaponScaling.DamageMult(BranchType.Distance, sp.ShardLevel);

            // Cada nivel: +0.2% critico
            player.GetCritChance(DamageClass.Ranged) += WeaponScaling.CritBonus(sp.ShardLevel);

            // Cada nivel: +0.4% armor penetration
            player.GetArmorPenetration(DamageClass.Ranged) += WeaponScaling.ArmorPenBonus(sp.ShardLevel);
        }

        public override float UseTimeMultiplier(Player player)
        {
            var sp = player.GetModPlayer<ShardPlayer>();
            if (sp == null || !sp.IsImprinted || sp.ActiveBranch != BranchType.Distance) return 1f;
            // Cada nivel: -0.3% use time (mas rapido)
            return WeaponScaling.UseSpeedMult(sp.ShardLevel);
        }

        public override bool CanConsumeAmmo(Item ammo, Player player) => false;

        /// <summary>
        /// Dispara flechas extra segun hitos: +1 cada 5 niveles.
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            var sp = player.GetModPlayer<ShardPlayer>();
            if (sp == null || !sp.IsImprinted || sp.ActiveBranch != BranchType.Distance) return true;

            int extra = WeaponScaling.ExtraProjectiles(BranchType.Distance, sp.ShardLevel);
            for (int i = 0; i < extra; i++)
            {
                float angle = (i + 1) * 0.15f * (i % 2 == 0 ? 1f : -1f);
                Vector2 perturbedVel = velocity.RotatedBy(angle);
                Projectile.NewProjectile(source, position, perturbedVel, type, damage, knockback, player.whoAmI);
            }
            return true; // tModLoader dispara la flecha principal
        }

        public override Vector2? HoldoutOffset() => new Vector2(2f, 0f);

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
            if (sp == null || !sp.IsImprinted || sp.ActiveBranch != BranchType.Distance) return;

            // === OCULTAR LINEA VANILLA "Level: X" ===
            for (int i = tooltips.Count - 1; i >= 0; i--)
            {
                if (tooltips[i].Name == "Level") tooltips.RemoveAt(i);
            }

            int xpNeeded = sp.XPForNextLevel();
            float pct = xpNeeded > 0 ? (float)sp.ShardXP / xpNeeded : 0f;
            pct = System.Math.Clamp(pct, 0f, 1f);
            int barLen = 20;
            int filled = (int)(barLen * pct);
            string bar = "[";
            for (int i = 0; i < barLen; i++)
                bar += i < filled ? "█" : "░";
            bar += "]";

            tooltips.Add(new TooltipLine(Mod, "FragmentLevel", $"[c/FFD700:Nivel {sp.ShardLevel}]") { OverrideColor = new Color(245, 196, 81) });
            tooltips.Add(new TooltipLine(Mod, "FragmentXP", $"{bar} {sp.ShardXP}/{xpNeeded} XP") { OverrideColor = new Color(179, 136, 255) });

            // Stats actuales por nivel
            tooltips.Add(new TooltipLine(Mod, "ScalingStats",
                $"[c/FFD700:Escalado por nivel:] " +
                $"[c/FF5555:+{(int)(sp.ShardLevel * WeaponScaling.RangedDamagePerLevel * 100)}% daño] " +
                $"[c/FFAA55:+{WeaponScaling.CritBonus(sp.ShardLevel):F1}% crit] " +
                $"[c/55AAFF:+{sp.ShardLevel * 0.4f:F1}% armor pen]"));

            // Hito alcanzado (cada 5 niveles)
            int nextMilestone = ((sp.ShardLevel / 5) + 1) * 5;
            if (sp.ShardLevel >= 5)
            {
                tooltips.Add(new TooltipLine(Mod, "MilestoneHeader", "[c/78FF96:★ Hitos alcanzados:]"));
                var milestones = WeaponScaling.MilestonesReached(BranchType.Distance, sp.ShardLevel);
                foreach (var m in milestones)
                    tooltips.Add(new TooltipLine(Mod, "Milestone_" + m, "  " + m));
            }
            tooltips.Add(new TooltipLine(Mod, "NextMilestone",
                $"[c/78788C:Próximo hito nivel {nextMilestone}: {WeaponScaling.MilestoneDescription(BranchType.Distance, nextMilestone / 5)}]"));
        }
    }
}
