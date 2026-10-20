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
    //  LA TILE DE BRUMA — VESTIGIO DE LA VIEJA CAÍDA (v6.50.130 →
    //  v6.51.0 DEPRECADA).
    //
    //  LA LETRA NUEVA: «mejor dejemos eso, que la bruma sea SOLO
    //  ESTÉTICA y desaparezca cuando el grimorio sea tomado» — el ALTAR
    //  y la bruma-colocable MURIERON con LA LLEGADA (la corrupción vive
    //  DESDE EL INVENTARIO: RecetasCorrupcionSistema). ESTA clase
    //  sobrevive SÓLO para que los mundos viejos que la guardaron no
    //  traigan tiles fantasma: la purga al cargar (PurgarLaViejaEra) las
    //  mata sin soltar nada, y si alguna escapó (mods externos, edits
    //  manuales) es un mechón INERTE: ni zona, ni luz de altar, ni
    //  drops — un recuerdo de cómo NO se le da un altar a un dios.
    // ======================================================================
    public class BrumaAltarTile : ModTile
    {
        public override string Texture => "AethonMod/Content/Tiles/BrumaAltarTile";

        public override void SetStaticDefaults()
        {
            // EL CUERPO: un hueco que NO estorba el paso (como siempre).
            Main.tileSolid[Type] = false;
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLighted[Type] = false;
            Main.tileBlockLight[Type] = false;
            TileID.Sets.PreventsSandfall[Type] = true;
            TileID.Sets.DisableSmartCursor[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
            TileObjectData.newTile.AnchorBottom =
                new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, 1, 0);
            TileObjectData.addTile(Type);

            // EL MAPA: el vestigio violeta apagado.
            LocalizedText nombre = CreateMapEntryName();
            AddMapEntry(new Color(120, 78, 170), nombre);
        }

        /// <summary>v6.51.0 — EL VESTIGIO NO SUELTA NADA: la bruma ya no
        /// es un altar que viaja — al picarla se evapora (y la purga al
        /// cargar el mundo la barrió igual).</summary>
        public override bool CanDrop(int i, int j) => false;
    }
}
