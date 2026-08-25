using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Tiles;

namespace AethonMod.Content.Items.Placeables
{
    /// <summary>
    /// Item colocable del Altar Antiguo (para testeo/debug).
    /// </summary>
    public class AncientAltarItem : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName cargado desde Localization.
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 20;
            Item.maxStack = Item.CommonMaxStack;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 15;
            Item.useTime = 15;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
            Item.createTile = ModContent.TileType<AncientAltar>();
            Item.rare = ItemRarityID.Quest;
            Item.value = Item.buyPrice(0, 0, 50, 0);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.StoneBlock, 20)
                .AddIngredient(ItemID.CrystalBlock, 5)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }
}
