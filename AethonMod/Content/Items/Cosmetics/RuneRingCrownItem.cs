using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Items.Cosmetics
{
    /// <summary>
    /// RuneRingCrownItem — v6.22 — LA CORONA DE ANILLOS RÚNICOS.
    ///
    /// Petición del usuario: "crea un cosmético que sea una corona de
    /// anillos rúnicos que RODEE al jugador".
    ///
    /// TRES ANILLOS RÚNICOS orbitando tu CUERPO (no la cabeza — el
    /// conjunto te RODEA): un aro casi vertical dorado abrazando el
    /// torso, un aro inclinado blanco-estelar girando en contra, y el
    /// aro ecuatorial azul flotando en tu cintura como un anillo de
    /// Saturno. Cada aro lleva sus glifos cabalgando la órbita, rotados
    /// a la tangente, con perlas y latidos propios — la técnica de los
    /// anillos del Sol, puesta sobre TU cuerpo.
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
                "[c/FFE8A8:Tres anillos rúnicos orbitando tu cuerpo: el aro dorado del pecho,\nel blanco-estelar inclinado en contrarroto y el ecuatorial azul de la cintura]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:Glifos cabalgando cada órbita con perlas y latidos propios —\nla técnica de los anillos del Sol, puesta sobre ti. Puro cosmético:\ncero estadísticas, vale en cualquier hueco de accesorio o vanidad]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
