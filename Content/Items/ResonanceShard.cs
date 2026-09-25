using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Items
{
    /// <summary>
    /// Fragmento de Resonancia — moneda secundaria del mod.
    /// Se obtiene de jefes cósmicos y se usa para memorizar armas en el Códex.
    /// </summary>
    public class ResonanceShard : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemNoGravity[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 14;
            Item.height = 14;
            Item.maxStack = Item.CommonMaxStack;
            Item.value = Item.buyPrice(0, 0, 5, 0);
            Item.rare = ItemRarityID.Blue;
        }
    }
}
