using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using AethonMod.Content.Projectiles.V20;

namespace AethonMod.Content.Weapons.V20
{
    /// <summary>
    /// MirrorShardStaff — bastón que dispara una esquirla de cristal
    /// que rebota en los tiles.
    ///
    /// Especificaciones:
    ///   - damage = 55 (DamageClass.Magic)
    ///   - useTime = useAnimation = 35
    ///   - shootSpeed = 12f
    ///   - mana = 0
    ///   - useStyle = HoldUp, autoReuse = true, noMelee = true
    ///   - Quest rarity, SoundID.Item8, receta 5 Wood
    /// </summary>
    public class MirrorShardStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 55;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28;
            Item.height = 30;
            Item.useTime = 35;
            Item.useAnimation = 35;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.mana = 0;
            Item.knockBack = 3f;
            Item.value = Item.buyPrice(0, 5, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.shoot = ModContent.ProjectileType<MirrorShardProjectile>();
            Item.shootSpeed = 12f;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 spawnPos = position + new Vector2(0f, -16f);
            Projectile.NewProjectile(source, spawnPos, velocity, type, damage, knockback, player.whoAmI);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "MS_Title",
                "[c/46E0FF:═══ ESQUIRLA DE ESPEJO ═══]"));
            tooltips.Add(new TooltipLine(Mod, "MS_Desc",
                "[c/88E0FF:Fragmento cristalino que rebota en las paredes]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Wood, 5)
                .Register();
        }
    }
}
