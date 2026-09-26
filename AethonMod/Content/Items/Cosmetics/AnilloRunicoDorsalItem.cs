using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace AethonMod.Content.Items.Cosmetics
{
    /// <summary>
    /// AnilloRunicoDorsalItem — v6.37 — EL ANILLO RÚNICO DORSAL (la
    /// triple corona de conjuro del vacío).
    ///
    /// EL ACCESORIO COSMÉTICO DEL VACÍO: el CÍRCULO DE CONJURO LITERAL
    /// de los agujeros negros colgado de tu ESPALDA — la TRIPLE CORONA
    /// DEL SUPREMO a escala del jugador (de la librería corregida):
    /// el círculo DORADO de ocho runas girando con el conjunto, el
    /// círculo VIOLETA de seis runas contrarrotando más afuera (el
    /// contrarroto arcano), el círculo BLANCO íNTIMO de seis latiendo
    /// rápido junto al ANILLO DE FOTONES del horizonte. Las runas DE
    /// PIE — el radio respira, el glifo se mece — cada una con su
    /// resplandor y su PERLA latiendo encima: la escritura exacta de
    /// los vórtices, respirando detrás de tu cuerpo.
    ///
    /// Puro adorno: cualquier hueco de accesorio, cero estadísticas —
    /// la misma regla de las coronas. La detección la hace
    /// CosmeticPlayer (la bandera AnilloDorsal), que invoca el halo
    /// proyectil AnilloRunicoDorsalHalo (la corona se dibuja con
    /// AnilloDorsalRenderer → OrbitaLib.CoronaConjuro, detrás del
    /// cuerpo: LA ESPALDA).
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
            tooltips.Add(new TooltipLine(Mod, "C", Language.GetTextValue("Mods.AethonMod.Items.AnilloRunicoDorsalItem.Titulo")));
            tooltips.Add(new TooltipLine(Mod, "D",
                Language.GetTextValue("Mods.AethonMod.Items.AnilloRunicoDorsalItem.Linea1")));
            tooltips.Add(new TooltipLine(Mod, "D2",
                Language.GetTextValue("Mods.AethonMod.Items.AnilloRunicoDorsalItem.Linea2")));
            tooltips.Add(new TooltipLine(Mod, "D3",
                Language.GetTextValue("Mods.AethonMod.Items.AnilloRunicoDorsalItem.Linea3")));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
