using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Cosmic;

namespace AethonMod.Content.Weapons.Cosmic
{
    public class BlackHoleStaff : ModItem
    {
        public override void SetStaticDefaults() { }
        public override void SetDefaults()
        {
            Item.damage = 100;
            Item.DamageType = DamageClass.Generic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 60; Item.useAnimation = 60;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<BlackHoleProjectile>();
            Item.shootSpeed = 6f;
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
            tooltips.Add(new TooltipLine(Mod, "T", "[c/9600FF:═══ AGUJERO NEGRO ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Lightmarch de 75 pasos con lensing gravitacional real (shader de WoTG)]"));
            tooltips.Add(new TooltipLine(Mod, "D2", "[c/78788C:Partículas en espiral + disco de acreción + atracción de enemigos]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }

    public class SunStaff : ModItem
    {
        public override void SetStaticDefaults() { }
        public override void SetDefaults()
        {
            Item.damage = 80;
            Item.DamageType = DamageClass.Generic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 50; Item.useAnimation = 50;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<SunProjectile>();
            Item.shootSpeed = 6f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "T", "[c/FFD700:═══ SOL ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Estrella con shader SunShader.fx (esfericidad + corona + lava) de WoTG]"));
            tooltips.Add(new TooltipLine(Mod, "D2", "[c/78788C:Partículas de fuego + iluminación intensa + OnFire]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }
}
