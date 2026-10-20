using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Items.Llamados
{
    // ======================================================================
    //  EL ECO DEL DESPERTAR (v6.51.0) — LA LLAVE DE PRUEBAS DE LA NUEVA
    //  PROGRESIÓN (la letra del usuario: «también crea un ítem no
    //  consumible que al usarlo suba el nivel de corrupción en 1 por cada
    //  uso del grimorio, que es la nueva mecánica de progresión» — el
    //  nombre cambió a último minuto: EL DESPERTAR).
    //
    //  NO CONSUMIBLE: cada uso sube EL DESPERTAR DEL GRIMORIO en 1 (con
    //  toda la ceremonia: la voz, el aviso dorado, el hito… y en el 20,
    //  LA PELÍCULA DE LA MASCOTA). Las ofrendas reales (esencias +
    //  dinero + el arma del peldaño) siguen siendo el camino canónico:
    //  este item es el PROBADOR de la mecánica (el patrón de la Tinta
    //  del Cometa — entregado en cada entrada al mundo de pruebas).
    // ======================================================================
    public class EcoDelDespertar : ModItem
    {
        public override string Texture => "AethonMod/Content/Items/Llamados/EcoDelDespertar";

        public override void SetStaticDefaults()
        {
            // El DisplayName y el tooltip viven en la localización.
        }

        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 22;
            Item.maxStack = 1;
            Item.consumable = false;          // NO CONSUMIBLE (la letra)
            Item.rare = ItemRarityID.Quest;   // la llave de pruebas de la casa
            Item.value = 0;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.UseSound = SoundID.Item4;
        }

        public override bool CanUseItem(Player player)
        {
            // En el tope ya no despierta más (la llave gira en vacío).
            return Content.Systems.DespertarGrimorio.NivelDe(player)
                < Content.Systems.DespertarGrimorio.NivelMax;
        }

        public override bool? UseItem(Player player)
        {
            // LA SUBIDA COMPLETA (la ceremonia de las tres ofrendas, sin
            // cobrarlas — es el probador).
            Content.Systems.DespertarGrimorioSistema.SubirNivel(player, null);
            return true;
        }
    }
}
