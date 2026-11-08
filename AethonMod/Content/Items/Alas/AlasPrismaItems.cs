using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Players;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Items.Alas
{
    /// <summary>
    /// ALASDEPRISMA — v6.54.0 → v6.113 — LA BASE DE LAS ALAS DE CÓDIGO.
    ///
    /// v6.113 — LA PURGA: las QUINCE del prisma, la Aurora de Tinta, las
    /// Agujas del Vacío y las Cuchillas del Eclipse MURIERON (la letra:
    /// «el resto de alas las puedes borrar»). Quedan LAS CINCO de la casa
    /// (viven en AlasNuevasV6560.cs): Solares · Vacío · Eclipse · Juicio
    /// · Corolas del Alba.
    ///
    /// EL SISTEMA ES EL MISMO (AlasAuroraLib — el abanico de plumas
    /// VECTORIALES con la onda de aleteo en cadena, el color por
    /// vértice, el ruido que fluye, el desgarre de punta y la física
    /// inyectada SIN sprite); lo que cambia es EL ALMA: cada ala declara
    /// su índice del catálogo (AlasCatalogo) — su paleta, su forma,
    /// su tempo y SU TEXTURA DE RUIDO de las veinte nacidas de la
    /// investigación de texturas de efecto de la casa.
    ///
    /// Llegan en LA BOLSA DE LAS ALAS (v6.55.0: entrega directa del kit
    /// y dentro del Cofre del Arsenal — antes viajaba dentro de la
    /// bolsa de la investigación, retirada).
    /// </summary>
    public abstract class AlasPrismaBase : ModItem
    {
        /// <summary>EL ALMA DEL ALA — el índice del catálogo (2..16).</summary>
        protected abstract byte Estilo { get; }

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.maxStack = 1;
            Item.value = Item.buyPrice(gold: 30);
            Item.rare = ItemRarityID.Cyan;
            Item.accessory = true;      // la técnica: accesorio que INYECTA alas
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            AlasAuroraPlayer mp = player.GetModPlayer<AlasAuroraPlayer>();
            mp.Equipada = true;
            mp.OcultarVisual = hideVisual;
            mp.AlaEstilo = Estilo;
            // v6.118 — EL CINTURÓN DEL ARNÉS (la letra: «a todas las alas
            // del mod tienes que darle inmunidad a las caidas»): la
            // inmunidad vive en AlasAuroraPlayer.PostUpdateEquips (el punto
            // central que cubre las CINCO por la base común) — y AQUÍ se
            // refuerza en el propio tick del accesorio (el patrón exacto
            // de vanilla con sus huecos): la caída no castiga a quien
            // lleva alas de la casa, viva donde viva el tick.
            player.noFallDmg = true;
        }

        public override void UpdateVanity(Player player)
        {
            AlasAuroraPlayer mp = player.GetModPlayer<AlasAuroraPlayer>();
            mp.Vanidad = true;
            if (mp.AlaEstilo == 0)
                mp.AlaEstilo = Estilo;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // LA LÍNEA VIVA: la conducta de la Aurora — la técnica se
            // anuncia en el tooltip (clave compartida del set).
            tooltips.Add(new TooltipLine(Mod, "EstiloAla",
                Terraria.Localization.Language.GetTextValue(
                    "Mods.AethonMod.Items.AlasPrismaBase.Estilo"))
            {
                OverrideColor = new Microsoft.Xna.Framework.Color(212, 190, 255),
            });
        }
    }
}
