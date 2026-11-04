using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Items.Altar
{
    /// <summary>
    /// AltarOfrendasItem — v6.86 — EL ITEM DEL ALTAR DE OFRENDAS (la letra:
    /// «investiga el altar del mod de referencia... este es el tipo de
    /// altar que necesitamos para que el grimorio pueda consumir cosas
    /// que no sean las que necesita y no esten corruptas»).
    ///
    /// LA CADENA DE OBTENCIÓN (el calco de la de la referencia §2.2 —
    /// «crafteo barato sin estación»: 20 piedra + 2 estrellas fugaces +
    /// 1 gema): cualquiera puede levantar un altar — lo que cuesta es
    /// alimentarlo.
    /// </summary>
    public class AltarOfrendasItem : ModItem
    {
        public override string Texture => "AethonMod/Content/Items/Altar/AltarOfrendasItem";

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType
                <global::AethonMod.Content.Tiles.AltarOfrendasTile>());
            Item.width = 30;
            Item.height = 44;
            Item.rare = ItemRarityID.Quest;
            Item.value = 0;
            Item.maxStack = 99;
        }

        public override void AddRecipes()
        {
            // LA RECETA DE ENTRADA (§2.2 de R5: barata, sin estación —
            // la misma economía que el Arsenal Altar: 25 piedra + 3
            // estrellas; la nuestra: 20 piedra + 2 estrellas + 1 diamante).
            CreateRecipe()
                .AddIngredient(ItemID.StoneBlock, 20)
                .AddIngredient(ItemID.FallenStar, 2)
                .AddIngredient(ItemID.Diamond, 1)
                .Register();
        }
    }
}
