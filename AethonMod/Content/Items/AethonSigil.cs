using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Buffs;

namespace AethonMod.Content.Items
{
    /// <summary>
    /// Sello de Aethon — accesorio ceremonial de alto nivel.
    /// Otorga:
    ///  - +5% daño mágico
    ///  - +5% daño de invocación
    ///  - +1 slot de minion
    ///  - +5/s regeneración de mana
    ///  - Aplica el buff "Empoderamiento Cósmico" mientras esté equipado
    ///    (+10% daño, +5% crítico, 1% lifesteal)
    /// </summary>
    public class AethonSigil : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.maxStack = 1;
            Item.value = Item.buyPrice(0, 25, 0, 0);
            Item.rare = ItemRarityID.Yellow; // Tier 8
            Item.accessory = true;
            Item.vanity = false;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // Bonus de daño mágico y de invocación
            player.GetDamage(DamageClass.Magic) += 0.05f;
            player.GetDamage(DamageClass.Summon) += 0.05f;

            // +1 slot de minion
            player.maxMinions += 1;

            // +5/s regeneración de mana (lifeRegen/manaRegen están en unidades/seg)
            // manaRegen se suma cada frame al pool de regeneración
            player.manaRegen += 5;
            player.manaRegenBonus += 5;

            // Aplicar el buff "Empoderamiento Cósmico" mientras el sello esté equipado.
            // Usamos AddBuff con duración de 60 frames (1 segundo) - se refresca cada frame.
            player.AddBuff(ModContent.BuffType<CosmicEmpowermentBuff>(), 60);
        }

        public override void AddRecipes()
        {
            // Crafteo: 1 GenesisShard + 5 StellarDust + 3 ResonanceShard + 1 GoldBar/PlatinumBar
            // Requiere un Anvil (no Altar, para ser accesible)
            Recipe.Create(ModContent.ItemType<AethonSigil>(), 1)
                .AddIngredient<GenesisShard>(1)
                .AddIngredient<StellarDust>(5)
                .AddIngredient<ResonanceShard>(3)
                .AddIngredient(ItemID.GoldBar, 3)
                .AddTile(TileID.Anvils)
                .Register();

            Recipe.Create(ModContent.ItemType<AethonSigil>(), 1)
                .AddIngredient<GenesisShard>(1)
                .AddIngredient<StellarDust>(5)
                .AddIngredient<ResonanceShard>(3)
                .AddIngredient(ItemID.PlatinumBar, 3)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
