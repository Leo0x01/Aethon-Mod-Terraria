using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Custom;

namespace AethonMod.Content.Weapons.Custom
{
    /// <summary>
    /// PhoenixFeatherWeapon — arma que dispara plumas ardientes de fénix.
    /// </summary>
    public class PhoenixFeatherWeapon : ModItem
    {
        public override void SetStaticDefaults() { }
        public override void SetDefaults()
        {
            Item.damage = 38;
            Item.DamageType = DamageClass.Generic;
            Item.width = 30; Item.height = 30;
            Item.useTime = 22; Item.useAnimation = 22;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<PhoenixFeather>();
            Item.shootSpeed = 13f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item34;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "T", "[c/FF6400:═══ PHOENIX FEATHER ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Pluma ardiente que aplica OnFire al impactar]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }
}
