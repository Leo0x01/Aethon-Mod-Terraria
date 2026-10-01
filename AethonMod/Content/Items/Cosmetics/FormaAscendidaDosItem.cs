using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Items.Cosmetics
{
    // ======================================================================
    //  v6.50.48 — LA FORMA ASCENDIDA 2: UN ITEM NUEVO, NO UN CAMBIO.
    //
    //  La letra del usuario: «tenias que crear un nuevo item de la forma
    //  ascendida no cambiar la que ya estaba, aun sigue sin verse divino,
    //  ademas las alas de serafin no llegan a extenderse por completo de
    //  arriba hasta abajo se quedan solo a mitad, necesitan mas plumas,
    //  al menos el doble y crear un nuevo item de la forma ascendida 2
    //  donde lo hagas mas divino y sagrado y al mismo tiempo implementes
    //  los cambios como las alas con mas plumas y todos esos efectos del
    //  anterior. ademas la forma ascendida y la forma ascendida 2 deben
    //  dar vuelo infinito».
    //
    //  LA FORMA 1 QUEDO COMO ESTABA (su apoteosis de la .47 intacta);
    //  esta es SU HERMANA MAYOR: la apoteosis ABSOLUTA — las once capas
    //  de la .47 elevadas (mas grandes, mas blancas) + las cinco del
    //  arte sacro que faltaban (LA MANDORLA que enmarca al dios, LA
    //  CORONA DE DOCE ESTRELLAS, LOS SIETE CANDELEROS, EL RIO DE LUZ y
    //  las plumas dobladas) + LAS ALAS con VEINTIOCHO plumas por lado
    //  (el doble de la .47) y la extension COMPLETA de arriba abajo
    //  (por encima de la cabeza y por debajo de los pies).
    //  Y EL VUELO INFINITO en AMBAS (CosmeticPlayer.VueloDivino: la
    //  Insignia del Alba de vanilla prestada; sin alas puestas, la
    //  fisica de alas se inyecta y las plumas del aura SON las alas).
    // ======================================================================
    /// <summary>
    /// FormaAscendidaDosItem — LA FORMA ASCENDIDA 2 (la apoteosis absoluta).
    ///
    /// El drop de AETHON, LA SEGUNDA LUZ — y fabricable con madera (el
    /// mod es de pruebas: el divino se prueba cuando se quiere). Cosmético
    /// puro + VUELO INFINITO: la detección la hace CosmeticPlayer.
    /// </summary>
    public class FormaAscendidaDosItem : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.accessory = true;
            Item.rare = ItemRarityID.Quest;
            Item.value = Item.buyPrice(gold: 12);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // Puro cosmético + el vuelo: la detección la hace CosmeticPlayer.
        }

        public override void UpdateVanity(Player player) { }

        public override bool CanEquipAccessory(Player player, int slot, bool modded)
        {
            return true;
        }

        public override void AddRecipes()
        {
            // La receta de pruebas de la casa (la Brasa usa 5 maderas; el
            // dios mayor, el doble): el mod es de PRUEBAS.
            CreateRecipe().AddIngredient(ItemID.Wood, 10).Register();
        }
    }
}
