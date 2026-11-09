using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.ObjectInteractions;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace AethonMod.Content.Tiles
{
    // ======================================================================
    //  PilarPrismaTile — v6.120 — EL PILAR PRISMA: LA SALIDA DEL LLANO.
    //
    //  LA LETRA DEL USUARIO: «una vez dentro del mundo interior no puedo
    //  salir, necesitas crear un pilar tipo prisma altar que permita
    //  salir, este se creara usando las herramientas del mod, la forja
    //  y el taller, ademas el pilar debe aparecer ya colocado en el
    //  mundo interior».
    //
    //  LA AUTOPSIA DEL «NO PUEDO SALIR»: EL PRISMA VIEJO
    //  (SubmundoGrimorio.GenerarInterior v6.115-v6.119) NUNCA NACÍA —
    //  WorldGen.PlaceObject corría sobre el mundo VACÍO (el prisma se
    //  «colocaba» ANTES de generar el terreno) y TileObject.CanPlace
    //  VALIDA AnchorBottom (leído en el decompile: Style2xX hereda
    //  `AnchorBottom = new AnchorData(AnchorType.SolidTile |
    //  AnchorType.SolidWithTop | AnchorType.SolidSide, newTile.Width,
    //  0)`): sin NADA sólido debajo, CanPlace devuelve false —
    //  PlaceObject fallaba EN SILENCIO (la lección que la propia v6.115
    //  documentó del altar enterrado) y el interior nacía SIN PUERTA:
    //  quien entraba, NO SALÍA. LA CURA DOBLE: (1) EL PILAR NUEVO nace
    //  forjado por LA CASA (tools/pilar_prisma_v6120.py — el camino PIL
    //  determinista sancionado, QA VLM SHIP 7/9/8/8); (2) se coloca
    //  DESPUÉS del terreno del llano (SubmundoGrimorio v6.120: el
    //  obsidiana YA es sólido bajo sus pies — el ancla pasa y CanPlace
    //  dice sí) — EL PILAR APARECE YA COLOCADO en cada visita, al lado
    //  del punto de aparición.
    //
    //  EL PILAR (la columna santuario del llano de obsidiana): el
    //  pedimento de amatista con la mesa dorada y LAS DOS VELAS de la
    //  casa (la herencia del altar), EL FUSTE esbelto de cristal
    //  facetado con SUS TRES ANILLOS RÚNICOS latiendo y EL CAPITEL —
    //  el prisma cristalino bebiendo EL HAZ dorado del eclipse y
    //  partiéndolo en el abanico de la casa (violeta y oro). Se lee
    //  ALTO desde cualquier punto del llano: la salida nunca vuelve a
    //  estar perdida.
    //
    //  SU PODER (el gesto del prisma padre — «tipo prisma altar»):
    //  clic-derecho abre LA MISMA VENTANA (PrismaInteriorUI):
    //   · VOLVER AL MUNDO REAL — la puerta de regreso (la cura).
    //   · INVOCAR A AETHON — LA LLAMADA nace del pilar (la ceremonia).
    //
    //  EL PILAR NO SE ROMPE (MinPick 9999): es el HUESO DE LA PUERTA
    //  del Eterno — nadie talla su salida del cuerpo del dios.
    // ======================================================================
    public class PilarPrismaTile : ModTile
    {
        /// <summary>La altura de CADA marco de la animación (3×5 tiles:
        /// 16 + 16 + 16 + 16 + 22 = 86 px por marco — 6 marcos en la tira
        /// de 54×516 forjada por la casa).</summary>
        private const int ALTO_MARCO = 86;

        private const int MARCOS = 6;

        public override string Texture => "AethonMod/Content/Tiles/PilarPrismaTile";

        public override void AnimateTile(ref int frame, ref int frameCounter)
        {
            // EL PILAR RESPIRA: 8 ticks por marco — el mismo tempo del
            // prisma (las velas alternan, los anillos suben, el abanico
            // camina y el glint viaja: el ciclo completo late cada ~3 s).
            if (++frameCounter >= 8)
            {
                frameCounter = 0;
                if (++frame >= MARCOS) frame = 0;
            }
        }

        public override void SetStaticDefaults()
        {
            // EL CUERPO DEL PILAR: un mueble de 3×5 que no estorba el paso
            // (la misma familia del prisma — Style2xX ensanchado a cinco
            // filas: LA BASE del ancla es su fila de abajo).
            Main.tileNoAttach[Type] = true;
            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = false;    // el llano no tiene lava
            Main.tileLighted[Type] = true;
            Main.tileSolid[Type] = false;
            Main.tileBlockLight[Type] = false;
            TileID.Sets.HasOutlines[Type] = true;
            AnimationFrameHeight = ALTO_MARCO;
            // NADIE TALLA SU SALIDA (la regla de esta era: la propiedad
            // se PONE en SetStaticDefaults — como AnimationFrameHeight):
            // el pilar es el hueso de la puerta del Eterno.
            MinPick = 9999;

            // LA BASE: Style2xX — LA MISMA del prisma y del altar (el
            // ancla de abajo es lo que GenerarInterior satisface AHORA:
            // el pilar se coloca SOBRE el obsidiana ya sólido). Celdas
            // de 18 px sin padding (el mapeo sin costuras de la casa) y
            // cinco filas: 16 + 16 + 16 + 16 + 22 = 86.
            TileObjectData.newTile.CopyFrom(TileObjectData.Style2xX);
            TileObjectData.newTile.Width = 3;
            TileObjectData.newTile.Height = 5;
            TileObjectData.newTile.CoordinateHeights = new[] { 16, 16, 16, 16, 22 };
            TileObjectData.newTile.CoordinatePadding = 0;
            TileObjectData.newTile.CoordinateWidth = 18;
            TileObjectData.newTile.Origin = new Point16(1, 4);
            TileObjectData.addTile(Type);

            // EL MAPA: el violeta del pilar (la familia del prisma).
            LocalizedText nombre = CreateMapEntryName();
            AddMapEntry(new Color(180, 120, 240), nombre);

            // EL POLVO AL PICARLO (inútil: nadie lo pica — pero la casa).
            DustType = DustID.Enchanted_Pink;
        }

        /// <summary>NADIE TALLA SU SALIDA: el pilar es el hueso de la
        /// puerta (MinPick 9999 — vestido arriba, en SetStaticDefaults,
        /// la regla de esta era).</summary>
        public override bool CanDrop(int i, int j) => false;

        /// <summary>LA LUZ DEL PILAR: el faro del llano — el violeta de la
        /// casa con el latido dorado del haz (el doble de ancho que el
        /// prisma: la salida se ve desde lejos en la oscuridad del
        /// cuerpo).</summary>
        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            float t = Main.GlobalTimeWrappedHourly;
            float latido = 0.5f + 0.5f * MathF.Sin(t * 2.2f);
            r = 0.40f + 0.16f * latido;
            g = 0.24f + 0.10f * latido;
            b = 0.64f + 0.18f * latido;
        }

        public override bool RightClick(int i, int j)
        {
            if (Main.LocalPlayer == null || !Main.LocalPlayer.active) return false;

            // LA VENTANA CON LOS BOTONES (el gesto del prisma — «tipo
            // prisma altar»): VOLVER AL MUNDO REAL o INVOCAR A AETHON.
            UI.PrismaInteriorUI.Alternar(i, j);
            return true;
        }

        public override void MouseOver(int i, int j)
        {
            Main.LocalPlayer.cursorItemIconEnabled = true;
            Main.LocalPlayer.cursorItemIconText = Language.GetTextValue(
                "Mods.AethonMod.PrismaInterior.NombrePilar");
        }

        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings)
            => true;
    }
}
