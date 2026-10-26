using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Players;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Items.Alas
{
    /// <summary>
    /// ALASDEPRISMA — v6.54.0 — LAS QUINCE ALAS DE LA INVESTIGACIÓN.
    ///
    /// La letra del usuario: «las alas la aurora de tinta están bien
    /// pero pueden ser mejor, deja esas alas, y crea otras 15 alas
    /// usando el mismo sistema luego metelas en una bolsa de alas y
    /// daselas al jugador».
    ///
    /// EL SISTEMA ES EL MISMO (AlasAuroraLib — el abanico de plumas
    /// quad con la onda de aleteo en cadena, el color por vértice, el
    /// ruido que fluye, el desgarre de punta y la física inyectada de
    /// Mothron SIN sprite); lo que cambia es EL ALMA: cada ala declara
    /// su índice del catálogo (AlasCatalogo) — su paleta, su forma,
    /// su tempo y SU TEXTURA DE RUIDO de las veinte nacidas de la
    /// investigación de las texturas de efecto de Goozma.
    ///
    /// Llegan en LA BOLSA DE LAS ALAS (dentro de la Bolsa de la
    /// Investigación — la letra: «todos los items nuevos que crees con
    /// esta investigacion profunda ponlos en una unica bolsa»).
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

    // =====================================================================
    //  LAS QUINCE — una clase diminuta por ala (el alma está en el
    //  catálogo; el ítem solo la señala).
    // =====================================================================

    /// <summary>2 — TINTA SERENA: el lila de la página en calma.</summary>
    public class AlasDeTintaSerena : AlasPrismaBase { protected override byte Estilo => 2; }

    /// <summary>3 — ECLIPSE: el morado profundo del cielo tapado.</summary>
    public class AlasDeEclipse : AlasPrismaBase { protected override byte Estilo => 3; }

    /// <summary>4 — PLASMA: fucsia vibrante, aleteo nervioso.</summary>
    public class AlasDePlasma : AlasPrismaBase { protected override byte Estilo => 4; }

    /// <summary>5 — NEBULOSA: el flujo de la nebulosa violeta.</summary>
    public class AlasDeNebulosa : AlasPrismaBase { protected override byte Estilo => 5; }

    /// <summary>6 — COMETARIO: la estela del cometa.</summary>
    public class AlasDeCometario : AlasPrismaBase { protected override byte Estilo => 6; }

    /// <summary>7 — MEDIANOCHE: el cielo estrellado entero.</summary>
    public class AlasDeMedianoche : AlasPrismaBase { protected override byte Estilo => 7; }

    /// <summary>8 — PRISMA: el arcoíris del dios antiguo.</summary>
    public class AlasDelPrisma : AlasPrismaBase { protected override byte Estilo => 8; }

    /// <summary>9 — FUEGO FRÍO: ondas de hielo lila.</summary>
    public class AlasDeFuegoFrio : AlasPrismaBase { protected override byte Estilo => 9; }

    /// <summary>10 — TORMENTA: violeta eléctrico, aleteo de trueno.</summary>
    public class AlasDeTormenta : AlasPrismaBase { protected override byte Estilo => 10; }

    /// <summary>11 — MARIONETA: el carmesí de la Emperatriz.</summary>
    public class AlasDeMarioneta : AlasPrismaBase { protected override byte Estilo => 11; }

    /// <summary>12 — AURORA ÁRTICA: el telón verde-cian del norte.</summary>
    public class AlasDeAuroraArtica : AlasPrismaBase { protected override byte Estilo => 12; }

    /// <summary>13 — ORQUÍDEA: el abanico rosa-magenta.</summary>
    public class AlasDeOrquidea : AlasPrismaBase { protected override byte Estilo => 13; }

    /// <summary>14 — VACÍO: negro absoluto con borde violeta, planea.</summary>
    public class AlasDelVacio : AlasPrismaBase { protected override byte Estilo => 14; }

    /// <summary>15 — SOLARES RÚNICAS: el oro de los soles del grimorio.</summary>
    public class AlasSolaresRunicas : AlasPrismaBase { protected override byte Estilo => 15; }

    /// <summary>16 — SANGRE DE TINTA: magenta profundo que gotea células.</summary>
    public class AlasDeSangreDeTinta : AlasPrismaBase { protected override byte Estilo => 16; }
}
