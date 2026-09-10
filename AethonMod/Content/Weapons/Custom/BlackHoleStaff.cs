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
    /// BlackHoleStaff — bastón que dispara un agujero negro con aberración cromática.
    /// El proyectil tiene: aberración cromática (3 sprites RGB desplazados),
    /// ondas expansivas que salen de él, y apariencia de agujero negro.
    /// </summary>
    public class BlackHoleStaff : ModItem
    {
        public override void SetStaticDefaults() { }
        public override void SetDefaults()
        {
            Item.damage = 60;
            Item.DamageType = DamageClass.Generic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 30; Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<BlackHoleBolt>();
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
            tooltips.Add(new TooltipLine(Mod, "T", "[c/9600FF:═══ AGUJERO NEGRO ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Energía concentrada con aberración cromática y ondas expansivas]"));
            tooltips.Add(new TooltipLine(Mod, "D2", "[c/78788C:Atrapa partículas hacia su centro]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }
}
