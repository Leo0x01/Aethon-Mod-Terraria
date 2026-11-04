using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Items.Investigaciones
{
    /// <summary>
    /// CodiceHabilidades — v6.86 — EL CÓDICE DE HABILIDADES (la letra:
    /// «recuerdas cuando estabamos intentando crear un arbol de
    /// habilidades y no lo logramos, bueno los mods de referencia
    /// tienen el arbol de habilidades en una ventana
    /// funcional justo como lo necesitabamos, asi que investigalo y
    /// crea un item que la use»).
    ///
    /// EL PATRÓN DEL ITEM (R6 §d.2 — el item-libro de la
    /// referencia): AltFunctionUse permite el CLIC DERECHO y CanUseItem
    /// lo traduce: clic derecho = ABRIR EL ÁRBOL (y NO "usar" el item
    /// — return false). El uso izquierdo también lo abre (el códice
    /// no hace otra cosa).
    /// </summary>
    public class CodiceHabilidades : ModItem
    {
        public override string Texture => "AethonMod/Content/Items/Investigaciones/CodiceHabilidades";

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.useStyle = ItemUseStyleID.HoldUp;   // «sostener en alto» (el patrón de la referencia)
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.mana = 0;               // item de la investigación: 0 maná
            Item.rare = ItemRarityID.Quest;
            Item.value = 0;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            // CLIC DERECHO = ABRIR EL ÁRBOL (el patrón del item-libro de
            // la referencia): no consume el "uso" — solo abre la ventana.
            if (player.altFunctionUse == 2)
            {
                UI.ArbolHabilidadesUI.Alternar();
                return false;
            }
            // El clic izquierdo también lo abre (el códice no hace más).
            UI.ArbolHabilidadesUI.Alternar();
            return false;
        }

        public override void AddRecipes()
        {
            // LA RECETA DE PRUEBAS DE LA CASA (5 maderas — el patrón del
            // kit: el arma de investigación NACE a mano).
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
