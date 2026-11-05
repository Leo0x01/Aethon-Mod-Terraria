using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

        // ================================================================
        //  v6.97 — LA ANIMACIÓN DEL ALTAR (la letra: «creo que se veria
        //  bien que el altar tuviera alguna animacion y que en el altar
        //  no se vea ningun libro despues de todo es el grimorio quien
        //  va ahi»): la lámina del sprite trae CUATRO marcos apilados en
        //  vertical (36×216 — cada marco el altar entero 2×3 con su
        //  brasa de runas latiendo distinto y SU chispa subiendo) — LA
        //  PROPIEDAD AnimationFrameHeight de esta era del tML aplica el
        //  marco vivo SOLA (MainFrameY += AnimationFrameHeight ×
        //  Main.tileFrame) y AnimateTile camina los marcos: el altar
        //  VIVE, las runas respiran y una chispa de consagración sube.
        //  Y SIN LIBRO: la cima queda LIMPIA — el único libro que llega
        //  al altar es el GRIMORIO MISMO (LA LLEGADA lo trae flotando
        //  encima).
        // ================================================================

        // ================================================================
        //  v6.100 — EL ALTAR ASENTADO + LAS DOS VELAS VIVAS (la letra
        //  del usuario: «el altar esta unos pixeles por encima del
        //  suelo, asi que al colocarlo es como si estuviera flotando,
        //  me gustaria que mejoraras la animacion ya que en el sprite
        //  parece que hay 2 velas y estas dos no estan animadas»).
        //
        //  LA CURA DOBLE: (1) el sprite fue REGENERADO con
        //  transparencia NATIVA (la regla permanente de la casa: la
        //  transparencia vive en la GENERACIÓN, jamás en el
        //  post-proceso) a 34×200 — CUATRO marcos de 50 px cuyo ARTE
        //  LLEGA HASTA LA ÚLTIMA FILA (la v6.99 dejaba 4 px vacíos
        //  bajo el pedestal: el altar FLOTABA); (2) LAS DOS VELAS del
        //  pedestal ahora ARDEN en la animación — sus llamas laten y
        //  se estiran marco a marco, alternando como velas de verdad,
        //  además del latido de runas y la chispa de consagración.
        //
        //  EL MAPEO DE LA LÁMINA: con CoordinatePadding = 0 y
        //  CoordinateWidth = 17, los 34 px de ancho del marco son DOS
        //  celdas exactas del tile sheet — el recorte vertical del
        //  sprite nuevo queda PERFECTO sin costuras.
        // ================================================================

        /// <summary>La altura de CADA marco de la animación (px — el
        /// altar mide 2×3 tiles: 34×50, arte hasta el suelo).</summary>
        private const int ALTO_MARCO = 50;

        /// <summary>CUÁNTOS marcos tiene la lámina (el latido completo).</summary>
        private const int MARCOS = 4;

        /// <summary>v6.97 — EL MARCO VIVO: la propiedad de la era 2026
        /// (0 = sin animación; 54 = los CUATRO marcos de la lámina — el
        /// tML aplica el solo el offset del marco que AnimateTile
        /// diga) — se VISTE en el SetStaticDefaults (la regla de esta
        /// era: las propiedades del ModTile se ponen, no se sobreponen).
        /// </summary>
        public override void AnimateTile(ref int frame, ref int frameCounter)
        {
            // EL LATIDO: 7 ticks por marco — lento, ritual, de altar.
            if (++frameCounter >= 7)
            {
                frameCounter = 0;
                if (++frame >= MARCOS) frame = 0;
            }
        }

        public override void DrawEffects(int i, int j, SpriteBatch spriteBatch,
            ref Terraria.DataStructures.TileDrawInfo drawData)
        {
            // LA CHISPA DE CONSAGRACIÓN: de vez en cuando, una brasa
            // sube de las runas (el polvo del alma de la casa — la
            // animación misma ya la pone el AnimationFrameHeight).
            if (Main.tileFrame[Type] == 2 && (i + j) % 3 == 0 &&
                Main.rand.NextBool(90) && !Main.gameMenu)
            {
                Dust d = Dust.NewDustPerfect(
                    new Vector2(i * 16f + Main.rand.Next(4, 30), j * 16f - Main.rand.Next(0, 8)),
                    DustID.Enchanted_Gold, new Vector2(0f, -0.45f), 150,
                    new Color(150, 90, 220), 0.5f);
                d.noGravity = true;
                d.fadeIn = 0.6f;
            }
        }

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
            // v6.97 — EL ALTAR ANIMADO: cuatro marcos en la lámina (la
            // propiedad AnimationFrameHeight de la era 2026 — el offset
            // del marco vivo lo aplica el tML SOLO).
            AnimationFrameHeight = ALTO_MARCO;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style2xX);
            TileObjectData.newTile.CoordinateHeights = new[] { 16, 16, 18 };
            // v6.100 — EL MAPEO SIN COSTURAS del sprite 34×200: sin
            // padding y con celda de 17 px, cada marco de 34 px de ancho
            // son DOS celdas exactas del tile sheet (el recorte del
            // sprite asentado queda byte-perfecto en el mundo).
            TileObjectData.newTile.CoordinatePadding = 0;
            TileObjectData.newTile.CoordinateWidth = 17;
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
