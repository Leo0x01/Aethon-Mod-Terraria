using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Items
{
    /// <summary>
    /// Polvo Estelar — material cósmico fino.
    /// Se obtiene al desarmar Fragmentos de Resonancia en el altar,
    /// o como drop raro de jefes cósmicos (Aethon, Hollow Titan).
    /// Se usa para craftear el Sello de Aethon y futuros items ceremoniales.
    /// </summary>
    public class StellarDust : ModItem
    {
        public override void SetStaticDefaults()
        {
            // Flota en el aire como otras reliquias cósmicas
            ItemID.Sets.ItemNoGravity[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.maxStack = Item.CommonMaxStack;
            Item.value = Item.buyPrice(0, 0, 50, 0);
            Item.rare = ItemRarityID.Purple; // Tier 7 (lime sería Yellow)
        }

        public override void AddRecipes()
        {
            // 3 Fragmentos de Resonancia -> 1 Polvo Estelar (convertidor)
            Recipe.Create(ModContent.ItemType<StellarDust>(), 1)
                .AddIngredient<ResonanceShard>(3)
                .AddTile(TileID.Anvils)
                .Register();

            // Polvo Estelar -> 1 Fragmento de Resonancia (reverse, en caso de necesitarlo)
            Recipe.Create(ModContent.ItemType<ResonanceShard>(), 1)
                .AddIngredient<StellarDust>(1)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
