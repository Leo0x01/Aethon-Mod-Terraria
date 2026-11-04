using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.ObjectInteractions;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace AethonMod.Content.Tiles
{
    // ======================================================================
    //  AltarOfrendasTile — v6.86 — EL ALTAR DE OFRENDAS (la letra del
    //  usuario: «investiga el altar del mod de referencia, investiga
    //  todas sus mecanicas, este es el tipo de altar que necesitamos
    //  para que el grimorio pueda consumir cosas que no sean las que
    //  necesita y no esten corruptas»).
    //
    //  EL INFORME R5 (r5_heroic_altar.md, fuentes extraídas de los
    //  .tmod oficiales): el Arsenal Altar de la referencia es un TILE
    //  COLOCADO (Style2xX, 2×3) que NO usa TileEntity — clic-derecho
    //  abre una UI propia con slots y botones que validan, aplican y
    //  CONSUMEN (stack--→TurnToAir). ESTE es su calco en el idioma de
    //  la casa: el mismo cuerpo (2×3, sin attach, lava death), el mismo
    //  gesto (RightClick → panel + inventario), la misma economía de
    //  devorar relleno — pero el botón Ofrendar suma PUNTOS al ALMA DEL
    //  LIBRO (ProgresoOfrendas) en vez de perks.
    // ======================================================================
    public class AltarOfrendasTile : ModTile
    {
        public override string Texture => "AethonMod/Content/Tiles/AltarOfrendasTile";

        public override void SetStaticDefaults()
        {
            // EL CUERPO DEL ALTAR (el calco de ArsenalAltarTile §3.1):
            // un mueble de 2×3 que no estorba el paso.
            Main.tileNoAttach[Type] = true;
            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = true;
            Main.tileLighted[Type] = true;
            Main.tileSolid[Type] = false;
            Main.tileBlockLight[Type] = false;
            TileID.Sets.HasOutlines[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style2xX);
            TileObjectData.newTile.CoordinateHeights = new[] { 16, 16, 18 };
            TileObjectData.newTile.Origin = new Point16(0, 2);
            TileObjectData.addTile(Type);

            // EL MAPA: el teal de las investigaciones.
            LocalizedText nombre = CreateMapEntryName();
            AddMapEntry(new Color(90, 220, 180), nombre);

            // EL POLVO AL PICARLO: el brillo de las estrellas fugaces.
            DustType = DustID.Enchanted_Gold;
        }

        /// <summary>EL GESTO DEL ALTAR (el calco de §3.1): clic-derecho
        /// abre el panel de ofrendas Y el inventario (para que las manos
        /// puedan trabajar).</summary>
        public override bool RightClick(int i, int j)
        {
            if (Main.LocalPlayer == null || !Main.LocalPlayer.active) return false;
            UI.AltarOfrendasUI.Alternar(i, j);
            Main.playerInventory = true;
            return true;
        }

        public override void MouseOver(int i, int j)
        {
            Main.LocalPlayer.cursorItemIconEnabled = true;
            Main.LocalPlayer.cursorItemIconID = ModContent.ItemType
                <Items.Altar.AltarOfrendasItem>();
        }

        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings)
            => true;

        /// <summary>AL PICARLO suelta su item (el altar viaja).</summary>
        public override bool CanDrop(int i, int j) => true;
    }
}
