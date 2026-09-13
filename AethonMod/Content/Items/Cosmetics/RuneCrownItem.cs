using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Items.Cosmetics
{
    /// <summary>
    /// RuneCrownItem — v6.03 — LA CORONA RÚNICA ESTELAR.
    ///
    /// Diseño NUEVO DESDE CERO sobre la referencia del usuario: un arco de
    /// OCHO GLIFOS RÚNICOS que flota sobre la cabeza — la lanza, el cáliz,
    /// la puerta, la estrella, el rayo, el arco, la espiral y el trono —
    /// cada uno con su perla rosa pálido en la punta. Los glifos flotan,
    /// pulsan su brillo con fase propia y emiten chispas ascendentes.
    ///
    /// No tiene nada que ver con la corona de arcos del agujero negro:
    /// esta es ESCRITURA MÁGICA, no líneas de campo.
    ///
    /// Puro adorno: cualquier hueco de accesorio, cero estadísticas.
    /// </summary>
    public class RuneCrownItem : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.accessory = true;     // funciona en huecos funcionales Y de vanidad
            Item.rare = ItemRarityID.Quest;
            Item.value = Item.buyPrice(gold: 5);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // Puro cosmético: la detección la hace CosmeticPlayer escaneando
            // los huecos de accesorio — nada que aplicar aquí.
        }

        public override void UpdateVanity(Player player)
        {
            // Vanidad pura: nada que aplicar.
        }

        public override bool CanEquipAccessory(Player player, int slot, bool modded)
        {
            return true; // siempre equipable: es un adorno
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "C", "[c/FF0055:═══ CORONA RÚNICA ESTELAR ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/FF66AA:Ocho glifos rúnicos de luz fucsia flotando en arco sobre tu cabeza,\ncada uno con su perla rosa pálido en la punta]"));
            tooltips.Add(new TooltipLine(Mod, "D2", "[c/78788C:La lanza, el cáliz, la puerta, la estrella, el rayo, el arco, la espiral\ny el trono — escritura mágica que respira y suelta chispas ascendentes]"));
            tooltips.Add(new TooltipLine(Mod, "D3", "[c/78788C:Puro cosmético: cero estadísticas. Equípala en cualquier hueco\nde accesorio, funcional o de vanidad]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
