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
    /// CrescentBlade — espada que dispara un slash de energía crescent.
    /// Inspirado en la imagen: slash verde-cian con speed trails.
    /// </summary>
    public class CrescentBlade : ModItem
    {
        public override void SetStaticDefaults() { }
        public override void SetDefaults()
        {
            Item.damage = 45;
            Item.DamageType = DamageClass.Generic;
            Item.width = 30; Item.height = 30;
            Item.useTime = 20; Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<CrescentSlash>();
            Item.shootSpeed = 14f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item60;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "T", "[c/00FFCC:═══ CRESCENT SLASH ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Slash de energía verde-cian con homing]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }
}
