using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Items.Cosmetics
{
    /// <summary>
    /// AnilloRunicoDorsalItem — v6.36 — EL ANILLO RÚNICO DORSAL.
    ///
    /// EL ACCESORIO COSMÉTICO DEL VACÍO: la gran firma mágica colgada
    /// de tu ESPALDA — un anillo rúnico de pie detrás del cuerpo (a
    /// escorzo, como las alas de un círculo mágico) con las OCHO RUNAS
    /// cabalgando su tangente, el ANILLO DE FOTONES interior
    /// contrarrotando (LOS ANILLOS RÚNICOS de los agujeros negros de
    /// la casa — no su disco de acreción), las cuatro perlas cardinales
    /// y el polvo rúnico orbitando. La precesión del plano lo mantiene
    /// vivo: el anillo respira mientras caminas.
    ///
    /// Puro adorno: cualquier hueco de accesorio, cero estadísticas —
    /// la misma regla de las coronas. La detección la hace
    /// CosmeticPlayer (la bandera AnilloDorsal), el dibujado la capa
    /// AnilloRunicoDorsalDrawLayer y la geometría AnilloDorsalRenderer.
    /// </summary>
    public class AnilloRunicoDorsalItem : ModItem
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
            tooltips.Add(new TooltipLine(Mod, "C", "[c/FF5CA0:═══ EL ANILLO RÚNICO DORSAL ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D",
                "[c/FF9ECB:La firma mágica del vacío cuelga de tu espalda: el anillo rúnico a escorzo,\ncon sus ocho runas cabalgando la tangente y el anillo de fotones contrarrotando]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/E6B8FF:Los anillos rúnicos de los agujeros negros — no su disco de acreción:\nescritura que respira, perlas cardinales y polvo rúnico orbitándote]"));
            tooltips.Add(new TooltipLine(Mod, "D3",
                "[c/78788C:Puro cosmético: cero estadísticas. Equípalo en cualquier hueco\nde accesorio, funcional o de vanidad]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
