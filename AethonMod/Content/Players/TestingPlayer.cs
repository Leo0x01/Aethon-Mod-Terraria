using Terraria;
using Terraria.ModLoader;
using AethonMod.Content.Items;

namespace AethonMod.Content.Players
{
    /// <summary>
    /// Player de TESTING — da items al entrar al mundo para pruebas.
    /// TEMPORAL: eliminar antes de release oficial.
    /// </summary>
    public class TestingPlayer : ModPlayer
    {
        public override void OnEnterWorld()
        {
            if (Main.netMode != Terraria.ID.NetmodeID.SinglePlayer) return;
            if (Player.whoAmI != Main.myPlayer) return;

            // Verificar si ya tiene el CosmicPetItem (para no duplicar)
            bool hasPet = false;
            for (int i = 0; i < 58; i++)
            {
                if (Player.inventory[i] != null &&
                    Player.inventory[i].type == ModContent.ItemType<CosmicPetItem>())
                {
                    hasPet = true;
                    break;
                }
            }

            if (!hasPet)
            {
                // CosmicPetItem
                for (int i = 0; i < 58; i++)
                {
                    if (Player.inventory[i] == null || Player.inventory[i].type == Terraria.ID.ItemID.None)
                    {
                        Player.inventory[i].SetDefaults(ModContent.ItemType<CosmicPetItem>());
                        break;
                    }
                }
                // BossSummonBag
                for (int i = 0; i < 58; i++)
                {
                    if (Player.inventory[i] == null || Player.inventory[i].type == Terraria.ID.ItemID.None)
                    {
                        Player.inventory[i].SetDefaults(ModContent.ItemType<BossSummonBag>());
                        break;
                    }
                }
                // TestSlayer
                for (int i = 0; i < 58; i++)
                {
                    if (Player.inventory[i] == null || Player.inventory[i].type == Terraria.ID.ItemID.None)
                    {
                        Player.inventory[i].SetDefaults(ModContent.ItemType<TestSlayer>());
                        break;
                    }
                }
                // GenesisShard
                for (int i = 0; i < 58; i++)
                {
                    if (Player.inventory[i] == null || Player.inventory[i].type == Terraria.ID.ItemID.None)
                    {
                        Player.inventory[i].SetDefaults(ModContent.ItemType<GenesisShard>());
                        break;
                    }
                }
                // 100 GoldBar
                for (int i = 0; i < 58; i++)
                {
                    if (Player.inventory[i] == null || Player.inventory[i].type == Terraria.ID.ItemID.None)
                    {
                        Player.inventory[i].SetDefaults(Terraria.ID.ItemID.GoldBar);
                        Player.inventory[i].stack = 100;
                        break;
                    }
                }
            }
        }
    }
}
