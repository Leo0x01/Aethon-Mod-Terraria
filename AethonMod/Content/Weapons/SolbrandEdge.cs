using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;

namespace AethonMod.Content.Weapons
{
    /// <summary>
    /// Solbrand, Filo del Alba — arma de la rama de Cuerpo a Cuerpo.
    ///
    /// PROYECTILES: Base NO dispara proyectiles (melee puro).
    /// Los proyectiles se desbloquean con nodos Notable/Keystone del arbol.
    /// Ej: nodo "blade-notable" (Corte de rayo) desbloquea el proyectil DawnSlash.
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
            // Base: NO dispara proyectiles (melee puro).
            Item.shoot = ProjectileID.None;
            Item.shootSpeed = 12f;
            // Permitir daño melee directo (no solo proyectil).
            Item.noMelee = false;
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            var sp = player.GetModPlayer<Players.ShardPlayer>();
            if (sp != null && sp.IsImprinted && sp.ActiveBranch == Players.BranchType.Melee)
            {
                damage += sp.ShardLevel * 3.1f;
            }
            float crit = 0;
            Systems.NodeEffectSystem.ApplyMeleeEffects(player, ref damage, ref crit);
            player.GetCritChance(DamageClass.Melee) += crit;
        }

        public override void ModifyWeaponKnockback(Player player, ref StatModifier knockback)
        {
            knockback *= Systems.NodeEffectSystem.GetMeleeKnockbackMult(player);
        }

        /// <summary>
        /// Dispara el proyectil DawnSlash solo si el jugador tiene nodos que lo desbloquean.
        /// NO mutamos Item.shoot (eso causaba bugs de estado).
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            var sp = player.GetModPlayer<Players.ShardPlayer>();
            if (sp == null) return false;

            // Nodos que desbloquean proyectiles.
            bool hasBladeNotable = sp.AllocatedNodes.Contains("blade-notable") ||
                                   sp.AllocatedNodes.Contains("blade-keystone");
            bool hasCelestialNotable = sp.AllocatedNodes.Contains("celestial-notable") ||
                                        sp.AllocatedNodes.Contains("celestial-keystone");

            if (!hasBladeNotable && !hasCelestialNotable) return false;

            // Disparar manualmente el proyectil DawnSlash.
            int projType = ModContent.ProjectileType<Projectiles.DawnSlash>();
            Projectile.NewProjectile(source, position, velocity, projType, damage, knockback, player.whoAmI);
            return false; // ya disparamos manualmente.
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-2f, 0f);
        }

        public override void ModifyTooltips(System.Collections.Generic.List<TooltipLine> tooltips)
        {
            // Añadir barra de XP del fragmento al tooltip
            var sp = Main.LocalPlayer?.GetModPlayer<Players.ShardPlayer>();
            if (sp != null && sp.IsImprinted && sp.ActiveBranch == Players.BranchType.Melee)
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

                tooltips.Add(new TooltipLine(Mod, "FragmentLevel", $"[c/FFD700:Nivel {sp.ShardLevel}]") { OverrideColor = new Color(245, 196, 81) });
                tooltips.Add(new TooltipLine(Mod, "FragmentXP", $"{bar} {sp.ShardXP}/{xpNeeded} XP") { OverrideColor = new Color(179, 136, 255) });
                tooltips.Add(new TooltipLine(Mod, "FragmentPts", $"Puntos: {sp.CumulativeSkillPoints() - sp.AllocatedNodes.Count} disponibles") { OverrideColor = new Color(120, 255, 150) });
            }
        }
    }
}

