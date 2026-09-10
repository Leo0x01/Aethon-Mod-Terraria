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
    /// VoidOrbStaff — bastón del vacío que dispara orbes púrpura absorbentes.
    /// </summary>
    public class VoidOrbStaff : ModItem
    {
        public override void SetStaticDefaults() { }
        public override void SetDefaults()
        {
            Item.damage = 50;
            Item.DamageType = DamageClass.Generic;
            Item.width = 30; Item.height = 30;
            Item.useTime = 30; Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<VoidOrb>();
            Item.shootSpeed = 10f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item20;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "T", "[c/9600FF:═══ VOID ORB ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Orbe de vacío que atrae partículas hacia él]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }
}
