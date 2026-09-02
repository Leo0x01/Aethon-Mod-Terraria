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
    /// ESCALADO POR NIVEL DEL FRAGMENTO (INFINITO):
    /// - Cada nivel: +2.5% daño, +0.2% crit, +0.5% knockback, +0.4% armor pen, -0.3% use time
    /// - Cada 10 niveles: +1 proyectil "Solbrand Blade" por ataque.
    ///   Este proyectil COPIA la textura del arma, gira como espada arrojadiza,
    ///   y persigue (homing) al enemigo hostil mas cercano.
    ///   Nivel 10 = 1 blade, Nivel 20 = 2 blades, Nivel 100 = 10 blades, etc.
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

            // Cada nivel: +2.5% daño (infinito)
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
            // Cada nivel: +0.5% knockback (infinito)
            knockback *= WeaponScaling.KnockbackMult(sp.ShardLevel);
        }

        public override float UseTimeMultiplier(Player player)
        {
            var sp = player.GetModPlayer<ShardPlayer>();
            if (sp == null || !sp.IsImprinted || sp.ActiveBranch != BranchType.Melee) return 1f;
            // Cada nivel: -0.3% use time, tope -25%
            return WeaponScaling.UseSpeedMult(sp.ShardLevel);
        }

        /// <summary>
        /// Cada 10 niveles del fragmento, crea un proyectil "Solbrand Blade" que:
        /// - Copia la textura del arma
        /// - Gira como espada arrojadiza
        /// - Persigue al enemigo mas cercano (homing)
        /// Nivel 10 = 1 blade, Nivel 20 = 2, ..., Nivel N = N/10 blades.
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            var sp = player.GetModPlayer<ShardPlayer>();
            if (sp == null || !sp.IsImprinted || sp.ActiveBranch != BranchType.Melee) return false;

            // Numero de blades = nivel / 10 (nivel 10+ = 1, 20+ = 2, 30+ = 3, ...)
            int bladeCount = sp.ShardLevel / 10;
            if (bladeCount <= 0) return false;

            int projType = ModContent.ProjectileType<Projectiles.DawnSlash>();
            for (int i = 0; i < bladeCount; i++)
            {
                // Cada blade sale en un angulo ligeramente distinto
                float spread = 0.15f;
                float angle = (i - (bladeCount - 1) / 2f) * spread;
                Vector2 vel = velocity.RotatedBy(angle);
                // Ligeramente mas lento que el swing para que las blades se vean
                vel *= 0.9f;
                Projectile.NewProjectile(source, position, vel, projType, damage, knockback, player.whoAmI);
            }
            return false; // ya disparamos los blades manualmente
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

            // Stats actuales por nivel (infinitas)
            int bladeCount = sp.ShardLevel / 10;
            tooltips.Add(new TooltipLine(Mod, "ScalingStats",
                $"[c/FFD700:Escalado por nivel:] " +
                $"[c/FF5555:+{(int)(sp.ShardLevel * WeaponScaling.MeleeDamagePerLevel * 100)}% daño] " +
                $"[c/FFAA55:+{WeaponScaling.CritBonus(sp.ShardLevel):F1}% crit] " +
                $"[c/55AAFF:+{sp.ShardLevel * 0.4f:F1}% armor pen] " +
                $"[c/78FF96:+{bladeCount} blade" + (bladeCount == 1 ? "" : "s") + "]"));

            // Linea especial: blades
            if (bladeCount > 0)
            {
                tooltips.Add(new TooltipLine(Mod, "BladeInfo",
                    $"[c/78FF96:★ Cada ataque lanza {bladeCount} espada" + (bladeCount == 1 ? "" : "s") + " autoguiada" + (bladeCount == 1 ? "" : "s") + " que persigue enemigos]"));
            }

            // Proximo hito de blade (cada 10 niveles)
            int nextBladeLevel = ((sp.ShardLevel / 10) + 1) * 10;
            tooltips.Add(new TooltipLine(Mod, "NextBlade",
                $"[c/78788C:Próxima espada en nivel {nextBladeLevel} ({nextBladeLevel - sp.ShardLevel} niveles)]"));
        }
    }
}
