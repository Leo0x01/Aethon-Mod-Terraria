using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Items.Llamados
{
    // ======================================================================
    //  v6.67 — EL OJO DEL CIELO (la letra del usuario: «necesitamos crear
    //  alguna interfaz nueva para algun item o algo asi que tenga una
    //  ventana nueva donde aparezca un checklist con todos los requisitos
    //  para el despertar, y que estos sean dados como una mision, por
    //  ejemplo matar todos los jefes necesarios con un checlist que lo
    //  marque […] y tambien con las armas que necesita el grimorio para
    //  despertar o lingotes, asi el jugador puede ir completando los
    //  requisitos de cualquier nivel de despertar aunque no este ahi […]
    //  por cierto tenemos una textura que podemos usar para eso es la del
    //  cielo»).
    //
    //  EL SPRITE ES EL DEL CIELO (la vieja IRA DEL CIELO de las Doce
    //  Sentencias — la purga de la .56.6 la retiró del arsenal; su
    //  retrato de 44×44 vuelve AHORA como el ojo que vigila el camino:
    //  el cielo mirando el despertar).
    //
    //  QUÉ HACE: al USARLO (HoldUp) abre/cierra EL CIELO DEL DESPERTAR
    //  (CieloDespiertoUI): la ventana de misiones con EL CHECKLIST de
    //  los 50 peldaños — los 17 jefes de la caza (marcados SIEMPRE que
    //  caigan, hoy o hace un año: crédito retroactivo), las armas y los
    //  lingotes de cada peldaño (con «¿la llevas en la bolsa?»), las
    //  esencias y el dinero del peldaño vivo, los hitos y los
    //  micropoderes. Nunca se consume (el patrón de la Carnada).
    // ======================================================================
    public class OjoDelCieloItem : ModItem
    {
        public override void SetStaticDefaults()
        {
            // EL NOMBRE Y EL TOOLTIP viven en el hjson (DisplayName/
            // Tooltip) — la casa habla español en cualquier idioma.
        }

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.maxStack = 1;
            Item.value = Item.buyPrice(gold: 10);
            Item.rare = ItemRarityID.Purple;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.autoReuse = false;
            Item.consumable = false;
        }

        public override bool? UseItem(Player player)
        {
            // EL CIELO SE ABRE (y se cierra): la ventana de misiones del
            // despertar — puro cine de cliente, nada por el cable.
            if (player.whoAmI == Main.myPlayer)
                UI.CieloDespiertoUI.Alternar();
            return true;
        }

        public override void AddRecipes()
        {
            // LA RECETA DEL CAMINO (barata a propósito — el cielo debe
            // estar al alcance del portador que recién encuentra su
            // libro): piedra de siempre + el oro del primer peldaño.
            Recipe receta = CreateRecipe();
            receta.AddIngredient(ItemID.StoneBlock, 25);
            receta.AddIngredient(ItemID.GoldBar, 2);
            receta.AddTile(TileID.WorkBenches);
            receta.Register();
        }
    }
}
