using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace AethonMod.Content.Tiles
{
    // ======================================================================
    //  PaginaDelEternoTile — v6.118 — LA PÁGINA DEL ETERNO (EL TILE DE
    //  VETA DEL CUERPO DEL DIOS).
    //
    //  LA LETRA (el cuestionario del rework): Materiales = MEZCLA — «la
    //  superficie de amatista/oro + VETAS DEL TILE FORJADO como
    //  decoración». La Piel del Eterno de la Forja (violeta-negro con
    //  vetas DORADAS que fluyen — la paleta del vacío de PROMPT-MUNDO-
    //  INTERIOR) se destiló a TILE: las vetas serpentean por el cuerpo
    //  de amatista del interior — LA CARNE VIVA DE LA PÁGINA bajo la
    //  corteza dorada, el oro del veredicto corriéndole por dentro.
    //
    //  · SOLID: es hueso del cuerpo (se camina sobre ella, se pica
    //    AlREDEDOR — la página no se rasga: MinPick 9999 como el prisma,
    //    la composición del dios no se saquea).
    //  · 3 VARIANTES (la hoja 54×18 de la Forja): el worldgen elige el
    //    marco por celda (TileFrameX = variante·18).
    //  · LUZ PROPIA: la veta dorada ARDE suave — el baño del interior
    //    respira desde dentro de la carne.
    // ======================================================================
    public class PaginaDelEternoTile : ModTile
    {
        public override string Texture => "AethonMod/Content/Tiles/PaginaDelEterno_Tile";

        public override void SetStaticDefaults()
        {
            // EL CUERPO: sólido, parte del terreno del dios.
            Main.tileSolid[Type] = true;
            Main.tileBlockLight[Type] = true;
            Main.tileLighted[Type] = true;
            Main.tileMergeDirt[Type] = false;
            TileID.Sets.CanBeClearedDuringGeneration[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
            TileObjectData.newTile.AnchorBottom =
                new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, 1, 0);
            TileObjectData.addTile(Type);

            // LA PÁGINA NO SE RASGA (la regla de esta era: la propiedad se
            // PONE en SetStaticDefaults — como el prisma): la veta es
            // hueso del Eterno, la composición del dios no se saquea.
            MinPick = 9999;

            // EL MAPA: el violeta de la página con el oro vivo.
            LocalizedText nombre = CreateMapEntryName();
            AddMapEntry(new Color(122, 78, 188), nombre);
        }

        /// <summary>LA PÁGINA NO SE RASGA — la veta no suelta nada.</summary>
        public override bool CanDrop(int i, int j) => false;

        /// <summary>LA LUZ DE LA VETA: el oro del veredicto arde suave
        /// desde dentro de la carne — el baño violeta-oro del interior.</summary>
        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            // el latido de la página (0.55..1.0 — el oro respira despacio)
            float late = 0.55f + 0.45f * (0.5f + 0.5f * MathF.Sin(
                Main.GlobalTimeWrappedHourly * 0.9f + i * 0.35f + j * 0.21f));
            r = 0.24f * late;
            g = 0.15f * late;
            b = 0.38f * late;
        }
    }
}
