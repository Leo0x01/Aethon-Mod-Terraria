using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Localization;

namespace AethonMod.Content.Systems
{
    // ======================================================================
    //  AltarUnicoSistema — v6.137 — LA LEY DEL ALTAR ÚNICO.
    //
    //  LA LETRA: «en el mundo solo puede haber un altar colocado en el
    //  mundo, cualquier altar extra queda desactivado con un texto que
    //  lo diga, y siempre marcar el altar que funciona en el mapa
    //  mientras que los desactivados no».
    //
    //  LA LEY: UN altar de consagración ACTIVO por mundo. Colocar un
    //  segundo NO se permite (el ítem rebota con el mensaje claro — más
    //  simple y más honesto que un mueble muerto: nunca hay altares
    //  zombie). Si el altar activo se PICA (se recoge), el sistema
    //  PROMUEVE automáticamente al primer altar vivo que quede en el
    //  mundo (si existe) y anuncia la promoción. La posición persiste
    //  con el mundo (SaveData/LoadData en el ModSystem — el patrón de
    //  la casa).
    //
    //  EL MAPA: el tML de esta era (2026.9.3.1, verificado contra el
    //  DLL) solo admite UNA MapEntry por tipo de tile — no hay entrada
    //  por-tile. La casa marca al ACTIVO como se debe: su nombre en el
    //  mapa dice «(ACTIVO)» mediante ModifyMapEntryName del tile (el
    //  hook que sí existe — verificado en el decompile: ModTile tiene
    //  ModifyMapEntryName(i, j, name, hoverColor)), y en el MUNDO el
    //  activo respira con su bruma de consagración (el apagado la
    //  pierde y su luz muere — la lectura visual inmediata).
    // ======================================================================
    public class AltarUnicoSistema : ModSystem
    {
        /// <summary>La coordenada del ORIGEN (esquina superior
        /// izquierda) del altar ACTIVO. (-1, -1) = no hay altar vivo
        /// (aún no llegó el grimorio o lo recogieron).</summary>
        public static int ActivoX = -1;
        public static int ActivoY = -1;

        /// <summary>¿Hay un altar activo registrado?</summary>
        public static bool HayActivo => ActivoX >= 0 && ActivoY >= 0;

        /// <summary>¿El altar de la tile (i, j) es EL ACTIVO? (cualquier
        /// celda del mueble 2×3 cuenta: se comparan los rectángulos).</summary>
        public static bool EsElActivo(int i, int j)
            => HayActivo && i >= ActivoX && i < ActivoX + 2 && j >= ActivoY && j < ActivoY + 3;

        /// <summary>REGISTRA un altar recién colocado como EL ACTIVO (la
        /// colocación ya validó que no hubiera otro).</summary>
        public static void Registrar(int i, int j)
        {
            ActivoX = i;
            ActivoY = j;
        }

        /// <summary>EL ALTAR ACTIVO MURIÓ (lo picaron): si queda otro
        /// altar vivo en el mundo, EL PRIMERO asciende — la puerta del
        /// Eterno nunca se queda sin su altar. Solo AUTORIDAD.</summary>
        public static void PromoverSiHaceFalta()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            int tipoAltar = ModContent.TileType
                <global::AethonMod.Content.Tiles.AltarOfrendasTile>();
            ActivoX = -1; ActivoY = -1;
            for (int x = 4; x < Main.maxTilesX - 4; x += 2)
            {
                for (int y = 8; y < Main.maxTilesY - 4; y++)
                {
                    Tile t = Main.tile[x, y];
                    if (t != null && t.HasTile && t.TileType == (ushort)tipoAltar)
                    {
                        // el ORIGEN del mueble: la tile más alta-izquierda
                        // (TileObjectData Origin (0,2): si esta celda es el
                        // ancla, la de arriba-izquierda es el origen)
                        int ox = x, oy = y;
                        Tile izq = Framing.GetTileSafely(x - 1, y);
                        Tile arriba = Framing.GetTileSafely(x, y - 1);
                        if (izq != null && izq.HasTile && izq.TileType == (ushort)tipoAltar) ox = x - 1;
                        if (arriba != null && arriba.HasTile && arriba.TileType == (ushort)tipoAltar) oy = y - 1;
                        Registrar(ox, oy);
                        if (Main.netMode == NetmodeID.Server)
                        {
                            EcoRed.AnunciarMundo("Mods.AethonMod.Altar.Promovido",
                                new Microsoft.Xna.Framework.Color(120, 235, 190));
                        }
                        else if (Main.netMode == 0)
                        {
                            Main.NewText(Language.GetTextValue("Mods.AethonMod.Altar.Promovido"),
                                new Microsoft.Xna.Framework.Color(120, 235, 190));
                        }
                        return;
                    }
                }
            }
        }

        public override void SaveWorldData(TagCompound tag)
        {
            if (HayActivo)
            {
                tag["altarUnicoX"] = ActivoX;
                tag["altarUnicoY"] = ActivoY;
            }
        }

        public override void LoadWorldData(TagCompound tag)
        {
            ActivoX = tag.ContainsKey("altarUnicoX") ? tag.GetInt("altarUnicoX") : -1;
            ActivoY = tag.ContainsKey("altarUnicoY") ? tag.GetInt("altarUnicoY") : -1;
        }
    }
}
