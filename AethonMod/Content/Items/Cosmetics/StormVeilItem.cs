using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Items.Cosmetics
{
    /// <summary>
    /// StormVeilItem — v6.23 — LA ENVOLTURA DE RAYOS PRIMORDIAL.
    ///
    /// Petición del usuario: "de la misma forma que haces con el fuego,
    /// crea un item cosmético que sea una envoltura de rayos; en esta
    /// envoltura usa nuestras librerías para darle el toque especial".
    ///
    /// LA TORMENTA QUE TE VISTE: la SILUETA del personaje es el carril
    /// de una tormenta viva construida con STORMLIB (nuestra librería de
    /// rayos — la misma matemática del rayo del cielo aprobado):
    ///   · CHISPAZOS fractales cruzando entre anclas del contorno,
    ///   · ARCOS de descarga abrazando el cuerpo,
    ///   · PELOS eléctricos caóticos lanzados hacia afuera.
    /// TODO por código: cero sprites de rayo.
    ///
    /// LA ENVOLTURA INTERACTÚA CONTIGO (como el fuego):
    ///   · Quieto: dos arcos perezosos y un chispazo suelto — la brisa
    ///     eléctrica de la calma.
    ///   · Al CORRER la energía SE ENCIENDE: chispazos vivos, pelos
    ///     eléctricos, flick nervioso — y la estela nace A CONTRA de tu
    ///     marcha.
    ///   · Al SALTAR los arcos caen hacia tus pies; al CAER suben hacia
    ///     tu cabeza (la inercia de la descarga).
    ///
    /// Puro adorno: cualquier hueco de accesorio, cero estadísticas.
    /// </summary>
    public class StormVeilItem : ModItem
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
            // Puro cosmético: la tormenta la vive StormVeilPlayer.
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
            tooltips.Add(new TooltipLine(Mod, "C", "[c/6FA8FF:═══ ENVOLTURA DE RAYOS PRIMORDIAL ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D",
                "[c/BFD4FF:Una tormenta de StormLib te ENVUELVE: chispazos fractales cruzando\nla silueta, arcos de descarga abrazando el cuerpo y pelos eléctricos\nhacia afuera — todo por código, sin un solo sprite]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/FFF0C0:Quieto es una brisa eléctrica · corre y la energía SE ENCIENDE,\ncon la estela a contra de tu marcha · salta y los arcos caen a tus\npies · cae y suben a tu cabeza]"));
            tooltips.Add(new TooltipLine(Mod, "D3",
                "[c/78788C:Puro cosmético: cero estadísticas. Equípala en cualquier hueco\nde accesorio, funcional o de vanidad]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
