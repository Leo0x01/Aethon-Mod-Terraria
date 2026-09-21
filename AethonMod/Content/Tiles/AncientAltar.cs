
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.DataStructures;
using Terraria.Localization;

using Terraria.ModLoader;
using Terraria.ObjectData;

namespace AethonMod.Content.Tiles
{
    /// <summary>
    /// Altar Antiguo — tile del Sagrario Hueco donde se encuentra el Fragmento Génesis.
    /// Al interactuar (clic derecho), otorga el Fragmento Génesis al jugador.
    /// </summary>
    public class AncientAltar : ModTile
    {
        public override void SetStaticDefaults()
        {
            Main.tileSolid[Type] = false;
            Main.tileMergeDirt[Type] = false;
            Main.tileBlockLight[Type] = false;
            Main.tileLighted[Type] = true;
            Main.tileFrameImportant[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
            TileObjectData.newTile.Width = 3;
            TileObjectData.newTile.Height = 2;
            TileObjectData.newTile.Origin = new Point16(1, 1);
            TileObjectData.newTile.CoordinateHeights = new[] { 16, 16 };
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.addTile(Type);

            AddMapEntry(new Color(120, 90, 200), CreateMapEntryName());
            // v6.50.2 — FIX (nombre del tile hardcodeado): CreateMapEntryName
            // resuelve la clave "Tiles.AncientAltar.MapEntry" (misma que usa
            // el cursor del MouseOver de abajo) — antes el mapa mostraba el
            // entry SIN nombre y el cursor un literal.
        }

        public override void MouseOver(int i, int j)
        {
            Player player = Main.LocalPlayer;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = ModContent.ItemType<Items.Placeables.AncientAltarItem>();
            // v6.50.2 — FIX (string hardcodeado → hjson): la misma clave del
            // map entry del tile ("Altar Antiguo").
            player.cursorItemIconText = Language.GetTextValue("Mods.AethonMod.Tiles.AncientAltar.MapEntry");
        }

        public override bool RightClick(int i, int j)
        {
            Player player = Main.LocalPlayer;
            var sp = player.GetModPlayer<Players.ShardPlayer>();
            if (sp == null) return false;

            // Otorgar el Fragmento Génesis si el jugador no lo tiene.
            bool hasShard = false;
            for (int k = 0; k < 58; k++)
            {
                if (player.inventory[k].type == ModContent.ItemType<Items.GenesisShard>())
                {
                    hasShard = true;
                    break;
                }
            }
            if (!hasShard)
            {
                // v6.50 — EL FRAGMENTO ES DE LA AUTORIDAD (hallazgo auditoría
                // MP nº5): el RightClick de tile corre SOLO en el cliente —
                // spawn-ear aquí era un drop FANTASMA en MP. En SP nace
                // local (mismo proceso); en MP el cliente pide el fragmento
                // al server por EcoRed y el server lo spawn- ea + difunde.
                if (Main.netMode == NetmodeID.MultiplayerClient)
                    Systems.EcoRed.PedirFragmentoGenesis();
                else
                {
                    int item = Item.NewItem(
                        player.GetSource_GiftOrReward(),
                        player.Center,
                        ModContent.ItemType<Items.GenesisShard>());
                    if (item >= 0 && item < Main.item.Length) // v5.59: bounds check
                        Main.item[item].noGrabDelay = 0;
                }
                // v6.50.1 — FIX (MENSAJE PREMATURO): el texto no anuncia un
                // hecho consumado — el server revalida (v6.50.1, anti-dupe)
                // y puede rechazar. En SP es cierto; en MP es la SOLICITUD.
                // v6.50.2 — FIX (strings hardcodeados → hjson): las tres
                // líneas del altar viajan por clave (regla de la casa).
                if (Main.netMode == NetmodeID.MultiplayerClient)
                    Main.NewText(Language.GetTextValue("Mods.AethonMod.Altar.Solicitando"),
                        new Color(245, 196, 81));
                else
                    Main.NewText(Language.GetTextValue("Mods.AethonMod.Altar.Reclamado"),
                        new Color(245, 196, 81));
            }
            else
            {
                Main.NewText(Language.GetTextValue("Mods.AethonMod.Altar.YaPosees"),
                    new Color(150, 150, 180));
            }
            return true;
        }

        public override void NearbyEffects(int i, int j, bool closer)
        {
            if (closer)
            {
                Lighting.AddLight(new Vector2(i * 16, j * 16), new Vector3(0.4f, 0.3f, 0.6f));
            }
        }
    }
}
