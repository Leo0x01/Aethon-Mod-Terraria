using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Items.Cosmetics
{
    // ======================================================================
    //  v6.50.44 — LA CORONA RÚNICA DE AURA MURIÓ: «los item La forma
    //  Ascendida y la corona runica de aura son lo mismo, dejar solo La
    //  Forma Ascendida». Queda LA FORMA ASCENDIDA (mejorada — véase su
    //  clase: ahora es un cosmético digno de un dios) y LA BRASA.
    // ======================================================================
    /// <summary>
    /// FormaAscendidaItem — v6.48 — LA FORMA ASCENDIDA (el drop prometido).
    ///
    /// El TODO que AethonBoss cargaba desde v5 ("drop de Forma Ascendida
    /// (cosmetico)") POR FIN cumplido: cuando la Luz Primordial te
    /// reconoce como un par y se apaga, deja caer su propia forma — el
    /// aura dorada-violeta de la luz primordial respirando alrededor
    /// del portador (AuraPerfil.FormaAscendida, dibujada por el
    /// PORTADOR aditivo). Cosmético puro: la corona del que ya no
    /// necesita invocarla.
    /// </summary>
    public class FormaAscendidaItem : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.accessory = true;
            Item.rare = ItemRarityID.Quest;
            Item.value = Item.buyPrice(gold: 10);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // v6.50.49 — LA BANDERA EN VIVO (el fix del vuelo infinito):
            // encenderla AQUÍ (dentro de Player.UpdateEquips, vía
            // ApplyEquipFunctional) hace que PostUpdateEquips — el hook
            // que inyecta la física de alas — la vea ENCENDIDA en este
            // MISMO tick. El patrón es el de vanilla: empressBrooch se
            // enciende igual. (En la .48 la única detección vivía en
            // PostUpdate: tarde por un hook — el vuelo jamás corrió.)
            player.GetModPlayer<global::AethonMod.Content.Players.CosmeticPlayer>().FormaAscendida = true;
        }

        public override void UpdateVanity(Player player)
        {
            // Lo mismo en el hueco de VANIDAD: un cosmético es un
            // cosmético viva donde lo pongas.
            player.GetModPlayer<global::AethonMod.Content.Players.CosmeticPlayer>().FormaAscendida = true;
        }

        public override bool CanEquipAccessory(Player player, int slot, bool modded)
        {
            return true;
        }
    }

    /// <summary>
    /// BrasaDelEclipseItem — v6.50.23 — LA BRASA DEL ECLIPSE.
    ///
    /// EL CUARTO TIPO DE AURA (la petición con nombre y apellidos: «crea
    /// un nuevo accesorio con algún tipo nuevo de aura usando luz, bruma,
    /// desenfoque, distorsión, bloom, glow, ruido perlin, y que sea un
    /// aura negra en los bordes, dorado en el medio y roja en el centro,
    /// todo por código»).
    ///
    /// LO QUE VISTE: el patrón BRUMA de AURALIB — la única pila de la
    /// casa con una capa NO aditiva. Humo NEGRO-VIOLETA de fBm de doble
    /// warp de dominio (ruido perlin 100% código, con blur box horneado
    /// = el DESENFOQUE) OSCURECIENDO el mundo en los BORDES por
    /// alfa-blend; encima, por el aditivo: el halo de LUZ (glow: derrame
    /// dorado + calor rojo), el NÚCLEO ROJO latiendo (84 bpm), el CUERPO
    /// DORADO con su ESCALERA DE BLOOM y los FANTASMAS de la DISTORSIÓN
    /// (heat-haze). Las brasas (chispas) escapan del humo y la luz de
    /// mundo late cálida. Cosmético puro, como sus dos hermanas.
    /// </summary>
    public class BrasaDelEclipseItem : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.accessory = true;     // huecos funcionales Y de vanidad
            Item.rare = ItemRarityID.Quest;
            Item.value = Item.buyPrice(gold: 8);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // v6.50.49 — LA BANDERA EN VIVO (el fix del vuelo, como la
            // Forma Ascendida: UpdateEquips la ve, el aura la viste).
            player.GetModPlayer<global::AethonMod.Content.Players.CosmeticPlayer>().BrasaDelEclipse = true;
        }

        public override void UpdateVanity(Player player)
        {
            player.GetModPlayer<global::AethonMod.Content.Players.CosmeticPlayer>().BrasaDelEclipse = true;
        }

        public override bool CanEquipAccessory(Player player, int slot, bool modded)
        {
            return true; // siempre equipable: es un adorno
        }

        public override void AddRecipes()
        {
            // La receta de pruebas de la casa (madera, como la Corona Rúnica).
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
