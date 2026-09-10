using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Weapons.TestStaffs
{
    /// <summary>
    /// LightBeamsStaff — dispara Nightglow (#931) con rayos de luz cian alrededor del proyectil.
    /// El efecto se aplica en TestAdvancedFX.AI() (flag 6004).
    /// </summary>
    public class LightBeamsStaff : ModItem
    {
        public override void SetStaticDefaults() { }
        public override void SetDefaults()
        {
            Item.damage = 10; Item.DamageType = DamageClass.Generic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 20; Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ProjectileID.FairyQueenMagicItemShot;
            Item.shootSpeed = 12f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int proj = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            if (proj >= 0 && proj < Main.maxProjectiles) Main.projectile[proj].ai[1] = 6004;
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "T", "[c/FFD700:═══ RAYOS DE LUZ ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Rayos cian radiantes alrededor del proyectil]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }
}
