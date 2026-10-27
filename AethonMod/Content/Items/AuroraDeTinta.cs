using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Players;

namespace AethonMod.Content.Items
{
    /// <summary>
    /// AURORADETINTA — v6.52.1 — LAS ALAS CREADAS POR CÓDIGO.
    ///
    /// La pieza nueva del ciclo de investigación del usuario («analiza
    /// las alas de este mod… hay wings creadas por código ahí… aprende
    /// la técnica y luego crea alas con dicha técnica y ponlas en una
    /// bolsa y dáselas al jugador»): las alas procedurales de la
    /// investigación (la técnica completa documentada en AlasAuroraLib —
    /// el abanico de plumas-quad, la onda de aleteo, la dispersión de
    /// hue por vértice, el ruido que fluye, el desgarre de punta y la
    /// física inyectada SIN sprite) vestidas con la TINTA DEL ESCRIBA:
    ///
    ///   · DE NOCHE son el PRISMA DE TINTA — el espectro completo
    ///     corriendo por diez plumas de luz.
    ///   · DE DÍA la tinta se aclara hacia BLANCO-ORO (la página).
    ///   · VUELO: física de alas de Mothron inyectada (velocidad de
    ///     dios, sin penalidad de hover) + 240 frames de vuelo + sin
    ///     daño de caída + la aceleración aérea de la Emperatriz.
    ///   · SI VISTES ALAS REALES: la aurora se pliega (ceden el visual)
    ///     pero ELEVA su tiempo de vuelo al mínimo.
    ///
    /// Llega en LA BOLSA DEL ESCRIBA (la cuarta pieza del ciclo).
    /// </summary>
    public class AuroraDeTinta : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName/Tooltip: Items.AuroraDeTinta en el hjson del Escriba.
        }

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.maxStack = 1;
            Item.value = Item.buyPrice(gold: 30);
            Item.rare = ItemRarityID.Cyan;
            Item.accessory = true;             // la técnica: accesorio que INYECTA alas
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            AlasAuroraPlayer mp = player.GetModPlayer<AlasAuroraPlayer>();
            mp.Equipada = true;
            mp.OcultarVisual = hideVisual;
            mp.AlaEstilo = global::AethonMod.Content.VFX.AlasCatalogo.Aurora;  // v6.54.0 — la histórica
        }

        public override void UpdateVanity(Player player)
        {
            // LA VANIDAD: la aurora como adorno (SOLO visual — sin física).
            AlasAuroraPlayer mp = player.GetModPlayer<AlasAuroraPlayer>();
            mp.Vanidad = true;
            if (mp.AlaEstilo == 0)
                mp.AlaEstilo = global::AethonMod.Content.VFX.AlasCatalogo.Aurora;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // LA LÍNEA VIVA (la conducta de la referencia — la forma actual
            // del ala se anuncia en el tooltip).
            string forma = Main.dayTime
                ? Terraria.Localization.Language.GetTextValue("Mods.AethonMod.Items.AuroraDeTinta.FormaDia")
                : Terraria.Localization.Language.GetTextValue("Mods.AethonMod.Items.AuroraDeTinta.FormaNoche");
            tooltips.Add(new TooltipLine(Mod, "FormaAurora", forma)
            {
                OverrideColor = new Microsoft.Xna.Framework.Color(212, 190, 255),
            });
        }
    }
}
