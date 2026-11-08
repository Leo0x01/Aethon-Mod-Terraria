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
    //  AltarPrismaTile — v6.115 — EL PRISMA DEL ETERNO.
    //
    //  LA LETRA DEL USUARIO: «en el momento en que el jugador aparece en
    //  el mundo debe aparecer un altar que sera un primsma de colores,
    //  este tiene la capacidad de enviar de regreso al jugador y ademas
    //  de invocar a aethon usando una ventana con botones para
    //  seleccionar lo que se quiere hacer».
    //
    //  EL PRISMA: el altar del MUNDO INTERIOR — un cristal de amatista
    //  en triángulo facetado FLOTANDO sobre el pedimento de las páginas
    //  (el filo dorado y las dos velas de la casa), partido por EL HAZ
    //  DEL ETERNO: la luz dorada entra por arriba y SE ABRE en el
    //  abanico de SIETE colores (el violeta de la casa mandando, el oro
    //  del veredicto y los cinco acentos vivos) que RESPIRAN marco a
    //  marco — el prisma CAMBIA de colores al vivo.
    //
    //  SUS DOS PODERES (la ventana con botones — PrismaInteriorUI):
    //   · VOLVER AL MUNDO REAL — la puerta de regreso.
    //   · INVOCAR A AETHON — LA LLAMADA nace del prisma: el altar de la
    //     consagración se materializa delante y EL GRIMORIO DEL ETERNO
    //     flota justo encima (la ceremonia completa de la v6.114).
    //
    //  EL PRISMA NO SE ROMPE (MinPick 9999): es parte del CUERPO del
    //  libro — nadie talla su propia salida de la entraña del Eterno.
    // ======================================================================
    public class AltarPrismaTile : ModTile
    {
        /// <summary>La altura de CADA marco de la animación (3×3 tiles:
        /// 18 + 16 + 16 + 22 = 54 px por marco — 6 marcos en la tira).</summary>
        private const int ALTO_MARCO = 54;

        private const int MARCOS = 6;

        public override void AnimateTile(ref int frame, ref int frameCounter)
        {
            // EL ABANICO RESPIRA: 8 ticks por marco — el ciclo completo
            // del espectro desplazándose late cada ~3 s.
            if (++frameCounter >= 8)
            {
                frameCounter = 0;
                if (++frame >= MARCOS) frame = 0;
            }
        }

        public override void SetStaticDefaults()
        {
            // EL CUERPO DEL PRISMA: un mueble de 3×3 que no estorba.
            Main.tileNoAttach[Type] = true;
            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = false;    // el interior no tiene lava
            Main.tileLighted[Type] = true;
            Main.tileSolid[Type] = false;
            Main.tileBlockLight[Type] = false;
            TileID.Sets.HasOutlines[Type] = true;
            AnimationFrameHeight = ALTO_MARCO;
            // NADIE TALLA SU SALIDA (la regla de esta era: la propiedad
            // se PONE en SetStaticDefaults — como AnimationFrameHeight):
            // el prisma es hueso del Eterno.
            MinPick = 9999;

            // LA BASE: Style2xX — LA MISMA del altar de la casa (3 filas
            // de alto, Height=3 ya correcto) ensanchada a TRES tiles (el
            // prisma es 3×3): Width=3 + celdas de 18 px sin padding (el
            // mapeo sin costuras). (La lección del headless: Style3x4
            // trae Height=4 y Calculate() EXIGE Height == las entradas
            // de CoordinateHeights — el mod ENTERO se caía al cargar.)
            TileObjectData.newTile.CopyFrom(TileObjectData.Style2xX);
            TileObjectData.newTile.Width = 3;
            TileObjectData.newTile.Height = 3;
            TileObjectData.newTile.CoordinateHeights = new[] { 16, 16, 22 };
            TileObjectData.newTile.CoordinatePadding = 0;
            TileObjectData.newTile.CoordinateWidth = 18;
            TileObjectData.newTile.Origin = new Point16(1, 2);
            TileObjectData.addTile(Type);

            // EL MAPA: el violeta del prisma.
            LocalizedText nombre = CreateMapEntryName();
            AddMapEntry(new Color(180, 120, 240), nombre);

            // EL POLVO AL PICARLO (inútil: nadie lo pica — pero la casa).
            DustType = DustID.Enchanted_Pink;
        }

        /// <summary>NADIE TALLA SU SALIDA: el prisma es hueso del
        /// Eterno (MinPick 9999 — vestido arriba, en
        /// SetStaticDefaults, la regla de esta era).</summary>
        public override bool CanDrop(int i, int j) => false;

        /// <summary>LA LUZ DEL PRISMA: el abanico respira — el violeta
        /// de la casa con el latido dorado del veredicto.</summary>
        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            float t = Main.GlobalTimeWrappedHourly;
            float latido = 0.5f + 0.5f * MathF.Sin(t * 2.2f);
            r = 0.36f + 0.14f * latido;
            g = 0.20f + 0.08f * latido;
            b = 0.58f + 0.16f * latido;
        }

        public override bool RightClick(int i, int j)
        {
            if (Main.LocalPlayer == null || !Main.LocalPlayer.active) return false;

            // LA VENTANA CON LOS BOTONES (la letra): volver al mundo
            // real o invocar a Aethon — el prisma decide con el jugador.
            UI.PrismaInteriorUI.Alternar(i, j);
            return true;
        }

        public override void MouseOver(int i, int j)
        {
            Main.LocalPlayer.cursorItemIconEnabled = true;
            Main.LocalPlayer.cursorItemIconText = Language.GetTextValue(
                "Mods.AethonMod.PrismaInterior.NombreCorto");
        }

        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings)
            => true;
    }
}
