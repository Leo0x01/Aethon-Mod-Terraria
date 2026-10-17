using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Items.Altar
{
    /// <summary>
    /// LA ESENCIA CORRUPTA (v6.50.128) — lo que queda de un item tras
    /// pasar por EL HAZ DEL ALTAR DE LA CAÍDA.
    ///
    /// LA LETRA DEL USUARIO: «la mecanica es la de comerse los item y
    /// mejorar el grimorio del eterno, para comer un item este debe
    /// corromperse primero y para eso se usara el lugar de caida».
    ///
    /// EL CIRCUITO: un item arrojado sobre el altar se pudre 2,5 s en el
    /// haz → se convierte EN SITIO en esencias corruptas (tantas como
    /// valía lo que era: la basura da 1, un tesoro hasta 99) → si su
    /// dueño del Eterno anda cerca (<40 tiles), el libro ABRE LA BOCA
    /// sobre el haz y se las traga (AltarDevoradorSistema) → el Grimorio
    /// del Eterno crece PARA SIEMPRE (+daño, +ojos — ShardPlayer.
    /// DevoracionPoder).
    ///
    /// No se usa con la mano: es COMIDA del Eterno (si la recoges, la
    /// vuelves a soltar sobre el altar y el festín sigue igual).
    /// </summary>
    public class ItemCorrupto : ModItem
    {
        public override string Texture => "AethonMod/Content/Items/Altar/ItemCorrupto";

        public override void SetStaticDefaults()
        {
            // El DisplayName y el Tooltip viven en la localización
            // (Items.ItemCorrupto) — la regla de la casa.
        }

        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 22;
            Item.maxStack = 999;
            Item.rare = ItemRarityID.LightPurple;
            Item.value = 0;                 // no vale nada: vale lo que el Eterno pague
        }

        /// <summary>No se usa: es comida del libro, no una herramienta.</summary>
        public override bool CanUseItem(Player player) => false;
    }
}
