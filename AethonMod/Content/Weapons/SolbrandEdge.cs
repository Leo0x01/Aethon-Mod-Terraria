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
                // Escalado porcentual moderado: +2.5% por nivel
                damage *= 1f + sp.ShardLevel * 0.025f;
            }
            float crit = 0;
            
            player.GetCritChance(DamageClass.Melee) += crit;
        }

        public override void ModifyWeaponKnockback(Player player, ref StatModifier knockback)
        {
            
        }

        public override float UseTimeMultiplier(Player player)
        {
            // Velocidad de ataque de nodos del arbol Melee (combo, berserk, ascend-5)
            float mult = 1f;
            var sp = player.GetModPlayer<Players.ShardPlayer>();
            if (sp != null && sp.IsImprinted && sp.ActiveBranch == Players.BranchType.Melee)
            {
                // combo: +3/4/5% velocidad
                // berserk: +5/7/9% velocidad
                // ascend-5: +100% velocidad
            }
            return mult;
        }

        /// <summary>
        /// Dispara el proyectil DawnSlash solo si el jugador tiene nodos que lo desbloquean.
        /// NO mutamos Item.shoot (eso causaba bugs de estado).
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            var sp = player.GetModPlayer<Players.ShardPlayer>();
            if (sp == null) return false;

            // Nodos que desbloquean proyectiles (Melee: blade y solar, no celestial).
            bool hasBladeNotable = false ||
                                   false;
            bool hasSolarNotable = false ||
                                   false;

            if (!hasBladeNotable && !hasSolarNotable) return false;

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
                tooltips.Add(new TooltipLine(Mod, "FragmentPts", $"Puntos: {sp.AvailableSkillPoints()} disponibles") { OverrideColor = new Color(120, 255, 150) });
            }
        }
    }
}

