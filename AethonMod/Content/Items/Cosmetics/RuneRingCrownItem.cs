using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Items.Cosmetics
{
    /// <summary>
    /// RuneRingCrownItem — v6.23 — LA CORONA: EL ANILLO DEL SOL I.
    ///
    /// Petición original v6.22: "crea un cosmético que sea una corona
    /// de anillos rúnicos que RODEE al jugador" (eran tres aros).
    ///
    /// v6.23 — LA ORDEN DEL USUARIO: "que la corona de anillos rúnicos
    /// SEA LA DEL SOL NÚMERO 1, además el anillo es muy brillante —
    /// reduce el brillo a como se ve en los soles".
    ///
    /// EL ANILLO DEL SOL RÚNICO I, LITERAL: el primer aro del sistema
    /// solar orbitando tu CUERPO — el mismo plano (1.62R × 0.34,
    /// inclinación −0.55), el mismo paso CW y los mismos 6 glifos con
    /// perlas y latidos, al BRILLO EXACTO de los soles.
    ///
    /// Puro adorno: cualquier hueco de accesorio, cero estadísticas.
    /// </summary>
    public class RuneRingCrownItem : ModItem
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
            // Puro cosmético: la detección la hace el propio renderer.
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
            tooltips.Add(new TooltipLine(Mod, "C", "[c/FFC86B:═══ CORONA DE ANILLOS RÚNICOS ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D",
                "[c/FFE8A8:EL anillo del Sol Rúnico I orbitando tu cuerpo: el mismo plano,\nel mismo paso y los mismos 6 glifos con perlas — al brillo EXACTO\nde los soles]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:El aro inclinado −0.55 abraza el torso girando en horario, sus\nglifos cabalgando la órbita a la tangente. Puro cosmético:\ncero estadísticas, vale en cualquier hueco de accesorio o vanidad]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
