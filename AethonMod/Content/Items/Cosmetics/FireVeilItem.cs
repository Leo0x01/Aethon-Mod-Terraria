using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Items.Cosmetics
{
    /// <summary>
    /// FireVeilItem — v6.22 — LA ENVOLTURA DE FUEGO PRIMORDIAL.
    ///
    /// Petición del usuario: "crea un item cosmético que envuelva al
    /// personaje con fuego creado por código, el fuego debe interactuar
    /// con las acciones del personaje cuando se mueva".
    ///
    /// EL FUEGO DE PROPAGACIÓN DE INTENSIDADES: un CAMPO DE 26×38 celdas
    /// vive sobre el jugador — la base SIEMPRE encendida y el fuego
    /// PROPAGÁNDOSE hacia arriba con decaimiento aleatorio (el algoritmo
    /// clásico de propagación de fuego por intensidades 0..36, tabla de
    /// 37 colores propia: brasa → carmesí → naranja → ámbar → oro →
    /// blanco). TODO por código: cero sprites de fuego.
    ///
    /// EL FUEGO INTERACTÚA CONTIGO:
    ///   · Al CORRER, el viento inclina las llamas en CONTRA de tu
    ///     marcha (dejan estela) y se avivan (el aire las alimenta).
    ///   · Al SALTAR, el fuego se APLASTA contra el cuerpo y retrasa
    ///     por debajo de ti (la inercia de la subida).
    ///   · Al CAER, las llamas SE ESTIRAN hacia arriba (el viento
    ///     relativo las peina) y arden más alto.
    ///   · Al VOLAR con alas, la envoltura se vuelve una COLUMNA de
    ///     fuego estirada por la velocidad.
    ///   · Quieto: la lumbre calma, con chispas que escapan solas.
    ///
    /// Puro adorno: cualquier hueco de accesorio, cero estadísticas.
    /// </summary>
    public class FireVeilItem : ModItem
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
            // Puro cosmético: el campo de fuego lo vive FireVeilPlayer.
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
            tooltips.Add(new TooltipLine(Mod, "C", "[c/FF5A00:═══ ENVOLTURA DE FUEGO PRIMORDIAL ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D",
                "[c/FFB25A:Un campo de fuego procedural te ENVUELVE de los pies a la cabeza:\nllamas de 37 niveles propagándose por código, sin un solo sprite]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/FFD080:Corre y el viento las aviva en contra de tu marcha · salta y se\naplastan con su inercia · cae y se estiran hacia arriba · vuela y\nse vuelven columna]"));
            tooltips.Add(new TooltipLine(Mod, "D3",
                "[c/78788C:Puro cosmético: cero estadísticas. Equípala en cualquier hueco\nde accesorio, funcional o de vanidad]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
