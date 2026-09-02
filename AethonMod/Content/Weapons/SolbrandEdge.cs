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
    /// Solbrand, Filo del Alba — arma de la rama de Cuerpo a Cuerpo.
    ///
    /// ESCALADO POR NIVEL DEL FRAGMENTO:
    /// - Cada nivel: +2.5% daño, +0.2% crit, +0.5% knockback, +0.4% armor pen
    /// - Cada 5 niveles: bonus de hito acumulativo (velocidad, crit, proyectil, etc.)
    /// </summary>
    public class SolbrandEdge : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName / Tooltip cargados desde Localization.
        }

        public override void SetDefaults()
        {
            Item.damage = 14;
            Item.DamageType = DamageClass.Melee;
            Item.width = 50;
            Item.height = 50;
            Item.useTime = 18;
            Item.useAnimation = 18;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 6f;
            Item.value = Item.buyPrice(0, 10, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.shoot = ProjectileID.None;
            Item.shootSpeed = 12f;
            Item.noMelee = false;
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            var sp = player.GetModPlayer<ShardPlayer>();
            if (sp == null || !sp.IsImprinted || sp.ActiveBranch != BranchType.Melee) return;

            // Cada nivel: +2.5% daño
            damage *= WeaponScaling.DamageMult(BranchType.Melee, sp.ShardLevel);

            // Cada nivel: +0.2% critico
            player.GetCritChance(DamageClass.Melee) += WeaponScaling.CritBonus(sp.ShardLevel);

            // Cada nivel: +0.4% armor penetration
            player.GetArmorPenetration(DamageClass.Melee) += WeaponScaling.ArmorPenBonus(sp.ShardLevel);
        }

        public override void ModifyWeaponKnockback(Player player, ref StatModifier knockback)
        {
            var sp = player.GetModPlayer<ShardPlayer>();
            if (sp == null || !sp.IsImprinted || sp.ActiveBranch != BranchType.Melee) return;
            // Cada nivel: +0.5% knockback
            knockback *= WeaponScaling.KnockbackMult(sp.ShardLevel);
        }

        public override float UseTimeMultiplier(Player player)
        {
            var sp = player.GetModPlayer<ShardPlayer>();
            if (sp == null || !sp.IsImprinted || sp.ActiveBranch != BranchType.Melee) return 1f;
            // Cada nivel: -0.3% use time (mas rapido)
            return WeaponScaling.UseSpeedMult(sp.ShardLevel);
        }

        /// <summary>
        /// Dispara el proyectil DawnSlash segun los hitos de nivel alcanzados.
        /// Se desbloquea en nivel 15 (3er hito) y sube +1 cada 10 niveles.
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            var sp = player.GetModPlayer<ShardPlayer>();
            if (sp == null || !sp.IsImprinted || sp.ActiveBranch != BranchType.Melee) return false;

            // Proyectil DawnSlash: +1 cada 10 niveles (nivel 10, 20, 30...)
            int extra = WeaponScaling.ExtraProjectiles(BranchType.Melee, sp.ShardLevel);
            if (extra <= 0) return false;

            int projType = ModContent.ProjectileType<Projectiles.DawnSlash>();
            Projectile.NewProjectile(source, position, velocity, projType, damage, knockback, player.whoAmI);
            for (int i = 0; i < extra - 1; i++)
            {
                float angle = (i + 1) * 0.12f * (i % 2 == 0 ? 1f : -1f);
                Vector2 perturbedVel = velocity.RotatedBy(angle);
                Projectile.NewProjectile(source, position, perturbedVel, projType, damage, knockback, player.whoAmI);
            }
            return false; // ya disparamos manualmente
        }

        public override Vector2? HoldoutOffset() => new Vector2(-2f, 0f);

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
            if (sp == null || !sp.IsImprinted || sp.ActiveBranch != BranchType.Melee) return;

            // Barra de XP
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
                $"[c/FF5555:+{(int)(sp.ShardLevel * WeaponScaling.MeleeDamagePerLevel * 100)}% daño] " +
                $"[c/FFAA55:+{WeaponScaling.CritBonus(sp.ShardLevel):F1}% crit] " +
                $"[c/55AAFF:+{sp.ShardLevel * 0.4f:F1}% armor pen]"));

            // Hito alcanzado (cada 5 niveles)
            int nextMilestone = ((sp.ShardLevel / 5) + 1) * 5;
            int prevMilestone = (sp.ShardLevel / 5) * 5;
            if (sp.ShardLevel >= 5)
            {
                tooltips.Add(new TooltipLine(Mod, "MilestoneHeader", "[c/78FF96:★ Hitos alcanzados:]"));
                var milestones = WeaponScaling.MilestonesReached(BranchType.Melee, sp.ShardLevel);
                foreach (var m in milestones)
                    tooltips.Add(new TooltipLine(Mod, "Milestone_" + m, "  " + m));
            }
            // Próximo hito
            tooltips.Add(new TooltipLine(Mod, "NextMilestone",
                $"[c/78788C:Próximo hito nivel {nextMilestone}: {WeaponScaling.MilestoneDescription(BranchType.Melee, nextMilestone / 5)}]"));
        }
    }
}
