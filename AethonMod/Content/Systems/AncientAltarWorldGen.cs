using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using AethonMod.Content.Tiles;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// Generación del mundo — coloca el Altar Antiguo naturalmente bajo tierra.
    /// El altar aparece en la capa de roca (rock layer) en cuevas, donde el
    /// jugador puede encontrarlo y reclamar su Fragmento Génesis.
    ///
    /// Usa PostWorldGen (hook simple de ModSystem) en vez de ModifyWorldGenTasks
    /// para mayor compatibilidad entre versiones de tModLoader.
    /// </summary>
    public class AncientAltarWorldGen : ModSystem
    {
        public override void PostWorldGen()
        {
            // Solo generar en mundos nuevos (no cada carga)
            int altarTileType = ModContent.TileType<AncientAltar>();
            int numAltars = 3; // 3 altares por mundo

            // Evitar duplicados: si ya hay altares, no hacer nada
            int existing = 0;
            for (int i = 0; i < Main.maxTilesX; i++)
            {
                for (int j = (int)Main.worldSurface; j < Main.maxTilesY - 50; j++)
                {
                    Tile t = Main.tile[i, j];
                    if (t != null && t.HasTile && t.TileType == altarTileType)
                    {
                        existing++;
                        if (existing >= numAltars) return;
                    }
                }
            }

            int placed = existing;
            int attempts = 0;
            int maxAttempts = 30000;

            while (placed < numAltars && attempts < maxAttempts)
            {
                attempts++;
                // Elegir posición aleatoria en la capa de roca
                int x = WorldGen.genRand.Next(80, Main.maxTilesX - 80);
                int minY = (int)Main.worldSurface + 40;
                int maxY = Main.maxTilesY - 150;
                if (minY >= maxY) continue;
                int y = WorldGen.genRand.Next(minY, maxY);

                // Buscar un punto con suelo sólido debajo y aire arriba
                Tile tile = Main.tile[x, y];
                if (tile == null) continue;
                Tile below = Main.tile[x, y + 2];
                if (below == null) continue;
                bool belowSolid = below.HasTile && Main.tileSolid[below.TileType] &&
                                  below.TileType != TileID.MagicalIceBlock;
                bool hereAir = !tile.HasTile || !Main.tileSolid[tile.TileType];
                if (!belowSolid || !hereAir) continue;

                // Colocar el altar (3x2 tiles). Origin en (1,1) según TileObjectData.
                bool ok = WorldGen.PlaceObject(x, y, altarTileType, mute: true,
                    style: 0, direction: 1);
                if (ok)
                {
                    placed++;
                }
            }

            // Fallback: si no se colocó ninguno, forzar uno cerca del spawn subterráneo
            if (placed == 0)
            {
                for (int ty = (int)Main.worldSurface + 20; ty < Main.maxTilesY - 100; ty += 3)
                {
                    bool found = false;
                    for (int tx = 50; tx < Main.maxTilesX - 50; tx += 3)
                    {
                        Tile below = Main.tile[tx, ty + 2];
                        Tile here = Main.tile[tx, ty];
                        if (below != null && here != null &&
                            below.HasTile && Main.tileSolid[below.TileType] &&
                            (!here.HasTile || !Main.tileSolid[here.TileType]))
                        {
                            WorldGen.PlaceObject(tx, ty, altarTileType, mute: true);
                            if (Main.tile[tx, ty].HasTile &&
                                Main.tile[tx, ty].TileType == altarTileType)
                            {
                                found = true;
                                break;
                            }
                        }
                    }
                    if (found) break;
                }
            }
        }
    }
}
