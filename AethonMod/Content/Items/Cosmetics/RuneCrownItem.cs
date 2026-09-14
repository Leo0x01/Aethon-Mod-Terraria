using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Items.Cosmetics
{
    /// <summary>
    /// RuneCrownItem — v6.24 — LA CORONA RÚNICA: LA AUREOLA DEL SOL I.
    ///
    /// v6.24 — LA ORDEN DEL USUARIO: "debe estar en la cabeza del
    /// jugador como una AUREOLA, tiene que ser una aureola IGUAL al
    /// anillo del sol rúnico 1" — EL ANILLO DEL SOL RÚNICO I, LITERAL,
    /// ringiendo la cabeza: el aro elíptico de cápsulas del sol (plano
    /// 1.62R × 0.34, inclinación −0.55, giro CW 0.26) con sus 6 glifos
    /// solares cabalgando la órbita, al brillo EXACTO de los soles.
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
            tooltips.Add(new TooltipLine(Mod, "C", "[c/FFC864:═══ CORONA RÚNICA — LA AUREOLA DEL SOL I ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/FFE8B0:El ANILLO DEL SOL RÚNICO I ringiendo tu cabeza como una aureola:\nel mismo aro del sol con sus seis glifos cabalgando la órbita]"));
            tooltips.Add(new TooltipLine(Mod, "D2", "[c/78788C:La geometría, el giro y el brillo LITERALES del sol — al mismo nivel\nde luz que ves en los soles rúnicos, nada más]"));
            tooltips.Add(new TooltipLine(Mod, "D3", "[c/78788C:Puro cosmético: cero estadísticas. Equípala en cualquier hueco\nde accesorio, funcional o de vanidad]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
