using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Weapons.TestStaffs
{
    /// <summary>
    /// CosmicTrailStaff — dispara Nightglow (#931) con estela cósmica dorada/cian densa.
    /// El efecto se aplica en TestAdvancedFX.AI() (flag 6001) — no en el item.
    /// </summary>
    public class CosmicTrailStaff : ModItem
    {
        public override void SetStaticDefaults() { }
        public override void SetDefaults()
        {
            Item.damage = 10; Item.DamageType = DamageClass.Generic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 20; Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ProjectileID.FairyQueenMagicItemShot; // Nightglow
            Item.shootSpeed = 12f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int proj = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            if (proj >= 0 && proj < Main.maxProjectiles) Main.projectile[proj].ai[1] = 6001;
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "T", "[c/FFD700:═══ ESTELA CÓSMICA ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Estela dorada/cian densa siguiendo el proyectil]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }
}
