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
    /// Solbrand, Filo del Alba — arma de la rama de Cuerpo a Cuerpo.
    ///
    /// SISTEMA DE NIVELES POR ITEM:
    /// - Cada copia de SolbrandEdge tiene su propio nivel/XP independiente.
    /// - Solo sube de nivel el item sostenido cuando matas enemigos.
    /// - Las otras armas (Lumina/Grimorio) NO suben hasta que las uses.
    ///
    /// ESCALADO POR NIVEL (INFINITO):
    /// - Cada nivel: +2.5% daño, +0.2% crit, +0.5% knockback, +0.4% armor pen
    /// - Cada 10 niveles: +1 proyectil "Solbrand Blade" autoguiado (copia el arma)
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

        /// <summary>
        /// Obtiene el GlobalItem que guarda el nivel/XP de este item específico.
        /// </summary>
        private ShardLevelItem GetShard(Item item)
            => item.GetGlobalItem<ShardLevelItem>();

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            var sl = GetShard(Item);
            if (sl == null) return;
            // Solo escala si el jugador eligió esta rama (imprinted Melee)
            var sp = player.GetModPlayer<ShardPlayer>();
            if (sp == null || !sp.IsImprinted || sp.ActiveBranch != BranchType.Melee) return;

            damage *= WeaponScaling.DamageMult(BranchType.Melee, sl.Level);
            player.GetCritChance(DamageClass.Melee) += WeaponScaling.CritBonus(sl.Level);
            player.GetArmorPenetration(DamageClass.Melee) += WeaponScaling.ArmorPenBonus(sl.Level);
        }

        public override void ModifyWeaponKnockback(Player player, ref StatModifier knockback)
        {
            var sl = GetShard(Item);
            if (sl == null) return;
            var sp = player.GetModPlayer<ShardPlayer>();
            if (sp == null || !sp.IsImprinted || sp.ActiveBranch != BranchType.Melee) return;
            knockback *= WeaponScaling.KnockbackMult(sl.Level);
        }

        public override float UseTimeMultiplier(Player player)
        {
            var sl = GetShard(Item);
            if (sl == null) return 1f;
            var sp = player.GetModPlayer<ShardPlayer>();
            if (sp == null || !sp.IsImprinted || sp.ActiveBranch != BranchType.Melee) return 1f;
            return WeaponScaling.UseSpeedMult(sl.Level);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            var sl = GetShard(Item);
            if (sl == null) return false;
            var sp = player.GetModPlayer<ShardPlayer>();
            if (sp == null || !sp.IsImprinted || sp.ActiveBranch != BranchType.Melee) return false;

            int bladeCount = sl.Level / 10;
            if (bladeCount <= 0) return false;

            int projType = ModContent.ProjectileType<Projectiles.DawnSlash>();
            for (int i = 0; i < bladeCount; i++)
            {
                float spread = 0.15f;
                float angle = (i - (bladeCount - 1) / 2f) * spread;
                Vector2 vel = velocity.RotatedBy(angle);
                vel *= 0.9f;
                Projectile.NewProjectile(source, position, vel, projType, damage, knockback, player.whoAmI);
            }
            return false;
        }

        public override Vector2? HoldoutOffset() => new Vector2(-2f, 0f);

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
            if (sp == null || !sp.IsImprinted || sp.ActiveBranch != BranchType.Melee) return;

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

            tooltips.Add(new TooltipLine(Mod, "FragmentLevel", $"[c/FFD700:Nivel {sl.Level}]") { OverrideColor = new Color(245, 196, 81) });
            tooltips.Add(new TooltipLine(Mod, "FragmentXP", $"{bar} {sl.XP}/{xpNeeded} XP") { OverrideColor = new Color(179, 136, 255) });

            // Stats actuales por nivel
            int bladeCount = sl.Level / 10;
            tooltips.Add(new TooltipLine(Mod, "ScalingStats",
                $"[c/FFD700:Escalado por nivel:] " +
                $"[c/FF5555:+{(int)(sl.Level * WeaponScaling.MeleeDamagePerLevel * 100)}% daño] " +
                $"[c/FFAA55:+{WeaponScaling.CritBonus(sl.Level):F1}% crit] " +
                $"[c/55AAFF:+{sl.Level * 0.4f:F1}% armor pen] " +
                $"[c/78FF96:+{bladeCount} blade" + (bladeCount == 1 ? "" : "s") + "]"));

            if (bladeCount > 0)
            {
                tooltips.Add(new TooltipLine(Mod, "BladeInfo",
                    $"[c/78FF96:★ Cada ataque lanza {bladeCount} espada" + (bladeCount == 1 ? "" : "s") + " autoguiada" + (bladeCount == 1 ? "" : "s") + " que persigue enemigos]"));
            }

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

            // Próxima espada
            int nextBladeLevel = ((sl.Level / 10) + 1) * 10;
            tooltips.Add(new TooltipLine(Mod, "NextBlade",
                $"[c/78788C:Próxima espada en nivel {nextBladeLevel} ({nextBladeLevel - sl.Level} niveles)]"));
        }
    }
}
