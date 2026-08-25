using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Weapons
{
    /// <summary>
    /// Solbrand, Filo del Alba — arma de la rama de Cuerpo a Cuerpo.
    /// Hoja de luz condensada que corta la realidad.
    /// Daño escala con el nivel: daño = nivel × 3.1.
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
            Item.shoot = ModContent.ProjectileType<Projectiles.DawnSlash>();
            Item.shootSpeed = 12f;
            Item.noMelee = true;
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            var sp = player.GetModPlayer<Players.ShardPlayer>();
            if (sp != null && sp.IsImprinted && sp.ActiveBranch == Players.BranchType.Melee)
            {
                damage += sp.ShardLevel * 3.1f;
            }
            // Aplicar efectos de nodos del árbol de Melee.
            float crit = 0;
            Systems.NodeEffectSystem.ApplyMeleeEffects(player, ref damage, ref crit);
            player.GetCritChance(DamageClass.Melee) += crit;
        }

        public override void ModifyWeaponKnockback(Player player, ref StatModifier knockback)
        {
            knockback *= Systems.NodeEffectSystem.GetMeleeKnockbackMult(player);
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-2f, 0f);
        }
    }
}
