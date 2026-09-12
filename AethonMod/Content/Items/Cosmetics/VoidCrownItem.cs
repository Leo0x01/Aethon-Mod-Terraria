using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Items.Cosmetics
{
    /// <summary>
    /// VoidCrownItem — v6.03 — LA CORONA DE LA REINA DEL VACÍO.
    ///
    /// La corona ORIGINAL del agujero negro carmesí, retirada del proyectil
    /// y convertida en COSMÉTICO DEL JUGADOR: cinco lazos de neón
    /// carmesí→magenta con nudos naranja incandescentes que se arquean
    /// JUSTO DETRÁS de la cabeza de quien la lleva. Respira, se balancea y
    /// suelta ascuas rosas — la corona es energía VIVA.
    ///
    /// Puro adorno: se puede llevar en cualquier hueco de accesorio
    /// (funcional o de vanidad) y no da ninguna estadística.
    /// </summary>
    public class VoidCrownItem : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 28;
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
            tooltips.Add(new TooltipLine(Mod, "C", "[c/FF1738:═══ CORONA DE LA REINA DEL VACÍO ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/FF66AA:Cinco lazos de neón carmesí→magenta con nudos naranja incandescentes,\narqueados JUSTO DETRÁS de tu cabeza]"));
            tooltips.Add(new TooltipLine(Mod, "D2", "[c/78788C:La corona original del Agujero Negro Carmesí — retirada del proyectil\ny hecha joya. Respira, se balancea y suelta ascuas rosas]"));
            tooltips.Add(new TooltipLine(Mod, "D3", "[c/78788C:Puro cosmético: cero estadísticas. Equípala en cualquier hueco\nde accesorio, funcional o de vanidad]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
