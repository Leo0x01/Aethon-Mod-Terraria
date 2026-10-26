// ============================================================================
// PUERTO DE INVESTIGACIÓN — Calamity: Hunt of the Old God
// (github.com/TohruKobayashi/CalamityHunt) · Team Hypnos:
// caligulasAquarium, IbanPlay, THEREDSTONEBRO, AmmyanDocCiesh, Blockaroz
// Licencia original: GPL-3.0 (ver LICENSE-GPL3.txt en esta carpeta).
// Copiado verbatim por petición del usuario para investigar al jefe
// GOOZMA; pendiente su decisión de re-licencia/modificación.
// ============================================================================

using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace AethonMod.Content.GoozmaPort.Content.Items.Misc
{
    public class OverloadedSludge : ModItem
    {
        public override bool IsLoadingEnabled(Mod mod) => !ModLoader.HasMod(HUtils.CalamityMod);

        public override void SetDefaults()
        {
            Item.width = 40;
            Item.height = 40;
            Item.knockBack = 6;
            Item.value = 10000;
            Item.rare = ItemRarityID.LightRed;
            Item.channel = true;
            Item.maxStack = Item.CommonMaxStack;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            TooltipLine tooltip = new(Mod, "AethonMod.Content.GoozmaPort:SludgeWarning", Language.GetOrRegister($"Mods.{"AethonMod"}.Items.OverloadedSludge.Warning").Value);
            if (ModLoader.HasMod(HUtils.CalamityMod)) {
                int check = tooltips.IndexOf(tooltips.Find(t => t.Text.Equals("Summons the Slime God")));
                if (check == -1) {
                    check = tooltips.IndexOf(tooltips.Find(t => t.Name == "Tooltip0"));
                }
                tooltips.RemoveAt(check);
                tooltips.Insert(check, tooltip);
            }
        }
        public override void AddRecipes()
        {
            // PORT NOTE (desviación documentada): la receta original usa el
            // RecipeGroup "AnyEvilBlock" del sistema de recetas de CalamityHunt
            // (no portado) — aquí versión vainilla equivalente.
            if (!ModLoader.HasMod(HUtils.CalamityMod)) {
                CreateRecipe()
                    .AddIngredient(ItemID.PinkGel, 40)
                    .AddIngredient(ItemID.EbonstoneBlock, 40)
                    .AddTile(TileID.DemonAltar)
                    .Register();
                CreateRecipe()
                    .AddIngredient(ItemID.PinkGel, 40)
                    .AddIngredient(ItemID.CrimstoneBlock, 40)
                    .AddTile(TileID.DemonAltar)
                    .Register();
            }
        }
    }
}
