using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Items.Cosmetics
{
    /// <summary>
    /// CoronaRunicoAuraItem — v6.48 — LA CORONA RÚNICA DE AURA.
    ///
    /// "El patrón Polígono con ConLados(5) y tintes del mod ya es una
    /// corona rúnica nueva para la Bolsa de Cosméticos — cuesta un
    /// preset, no un arma." HECHO: el pentágono giratorio de AURALIB
    /// vestido por el jugador — un jaula de energía de CINCO lados con
    /// el violeta del Sagrario al centro y el ORO del grimorio en las
    /// aristas (que la librería dibuja con el color de BORDE), chispas
    /// doradas desprendiéndose y humo que respira. La capa trasera
    /// corre por el PORTADOR aditivo (el camino nuevo del
    /// halo-proyectil — el neón de los NPCs); el velo al 5% pisa el
    /// cuerpo por la capa de DrawData.
    ///
    /// Puro adorno: cualquier hueco de accesorio, cero estadísticas.
    /// </summary>
    public class CoronaRunicoAuraItem : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.accessory = true;     // huecos funcionales Y de vanidad
            Item.rare = ItemRarityID.Quest;
            Item.value = Item.buyPrice(gold: 5);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // Puro cosmético: la detección la hace CosmeticPlayer.
        }

        public override void UpdateVanity(Player player) { }

        public override bool CanEquipAccessory(Player player, int slot, bool modded)
        {
            return true; // siempre equipable: es un adorno
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

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
            // Puro cosmético: la detección la hace CosmeticPlayer.
        }

        public override void UpdateVanity(Player player) { }

        public override bool CanEquipAccessory(Player player, int slot, bool modded)
        {
            return true;
        }
    }
}
