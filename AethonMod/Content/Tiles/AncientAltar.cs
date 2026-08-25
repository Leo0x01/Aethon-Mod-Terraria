using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
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

            AddMapEntry(new Color(120, 90, 200));
        }

        public override void MouseOver(int i, int j)
        {
            Player player = Main.LocalPlayer;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = ModContent.ItemType<Items.Placeables.AncientAltarItem>();
            player.cursorItemIconText = "Altar Antiguo";
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
                int item = Item.NewItem(
                    player.GetSource_GiftOrReward(),
                    player.Center,
                    ModContent.ItemType<Items.GenesisShard>());
                Main.item[item].noGrabDelay = 0;
                Main.NewText("Has reclamado el Fragmento Génesis. Combate para imprprimir tu rama.", new Color(245, 196, 81));
            }
            else
            {
                Main.NewText("Ya posees el Fragmento Génesis.", new Color(150, 150, 180));
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
