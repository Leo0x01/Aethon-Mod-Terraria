using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
using Terraria.IO;
using Microsoft.Xna.Framework;
using AethonMod.Content.Tiles;
using System.Collections.Generic;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// Generación del mundo — coloca el Altar Antiguo naturalmente bajo tierra.
    /// El altar aparece en la capa de roca (rock layer) en cuevas, donde el
    /// jugador puede encontrarlo y reclamar su Fragmento Génesis.
    /// </summary>
    public class AncientAltarWorldGen : ModSystem
    {
        public override void ModifyWorldGenTasks(List<GenPass> passes, GenerationProgress progress)
        {
            // Insertar DESPUÉS de la generación de cuevas/estructura principal.
            int shiniesIndex = passes.FindIndex(pass => pass.Name.Equals("Shinies"));
            if (shiniesIndex != -1)
            {
                passes.Insert(shiniesIndex + 1, new AncientAltarPass("Aethon: Ancient Altars",
                    progress));
            }
            else
            {
                // Fallback: insertar al final
                passes.Add(new AncientAltarPass("Aethon: Ancient Altars", progress));
            }
        }
    }

    /// <summary>
    /// GenPass que coloca altares en la capa de roca.
    /// </summary>
    public class AncientAltarPass : GenPass
    {
        public AncientAltarPass(string name, GenerationProgress progress = null) : base(name, 1f) { }

        protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
        {
            progress.Message = "Sembrando altares antiguos...";

            int altarTileType = ModContent.TileType<AncientAltar>();
            int numAltars = 3; // 3 altares por mundo (uno por rama posible)

            int placed = 0;
            int attempts = 0;
            int maxAttempts = 20000;

            while (placed < numAltars && attempts < maxAttempts)
            {
                attempts++;
                // Elegir posición aleatoria en la capa de roca (bajo tierra)
                int x = WorldGen.genRand.Next(100, Main.maxTilesX - 100);
                // capa de roca: entre (int)(Main.worldSurface) y Main.maxTilesY - 200
                int minY = (int)Main.worldSurface + 30;
                int maxY = Main.maxTilesY - 200;
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

                // Verificar que no haya otro altar cerca
                bool tooClose = false;
                // (No guardamos posiciones; simplemente probamos suerte con la dispersión
                //  del random. En mundos grandes 3 altares no chocan.)
                if (tooClose) continue;

                // Colocar el altar (3x2 tiles). Origin en (1,1) según TileObjectData.
                int originX = x;
                int originY = y;
                bool ok = WorldGen.PlaceObject(originX, originY, altarTileType, mute: true,
                    style: 0, direction: 1);
                if (ok)
                {
                    placed++;
                    // Marcar el área con polvo dorado para avisar al jugador cercano (opcional)
                    for (int d = 0; d < 15; d++)
                    {
                        Dust.NewDustPerfect(new Vector2(originX * 16, originY * 16),
                            DustID.GoldFlame,
                            new Vector2(WorldGen.genRand.NextFloat(-2, 2), WorldGen.genRand.NextFloat(-3, 0)),
                            100, new Color(245, 196, 81), 1.5f);
                    }
                }
            }

            // Si por algún motivo no se colocó ninguno, forzar uno cerca del spawn subterráneo.
            if (placed == 0)
            {
                for (int ty = (int)Main.worldSurface + 20; ty < Main.maxTilesY - 100; ty += 5)
                {
                    for (int tx = 50; tx < Main.maxTilesX - 50; tx += 5)
                    {
                        Tile below = Main.tile[tx, ty + 2];
                        Tile here = Main.tile[tx, ty];
                        if (below != null && here != null &&
                            below.HasTile && Main.tileSolid[below.TileType] &&
                            (!here.HasTile || !Main.tileSolid[here.TileType]))
                        {
                            WorldGen.PlaceObject(tx, ty, altarTileType, mute: true);
                            if (Main.tile[tx, ty].HasTile && Main.tile[tx, ty].TileType == altarTileType)
                            {
                                placed = 1;
                                ty = Main.maxTilesY; // salir
                                break;
                            }
                        }
                    }
                }
            }

            progress.Value = 1f;
        }
    }
}
