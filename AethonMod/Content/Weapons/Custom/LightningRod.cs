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
    /// LightningRod — vara que dispara rayos zigzagueantes dorados.
    /// </summary>
    public class LightningRod : ModItem
    {
        public override void SetStaticDefaults() { }
        public override void SetDefaults()
        {
            Item.damage = 40;
            Item.DamageType = DamageClass.Generic;
            Item.width = 30; Item.height = 30;
            Item.useTime = 15; Item.useAnimation = 15;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<LightningBolt>();
            Item.shootSpeed = 18f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item93;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "T", "[c/FFFF00:═══ LIGHTNING BOLT ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Rayo zigzagueante dorado con chispas cian]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }
}
