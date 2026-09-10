using Terraria;
using Terraria.ModLoader;
using AethonMod.Content.Items;

namespace AethonMod.Content.Players
{
    public class TestingPlayer : ModPlayer
    {
        public override void OnEnterWorld()
        {
            if (Main.netMode != Terraria.ID.NetmodeID.SinglePlayer) return;
            if (Player.whoAmI != Main.myPlayer) return;

            bool alreadyHasKit = false;
            for (int i = 0; i < 58; i++)
            {
                if (Player.inventory[i] != null &&
                    Player.inventory[i].type == ModContent.ItemType<GenesisShard>())
                {
                    alreadyHasKit = true;
                    break;
                }
            }

            if (alreadyHasKit) return;

            GiveItem(ModContent.ItemType<GenesisShard>(), 1);
            GiveItem(Terraria.ID.ItemID.GoldBar, 100);
            GiveItem(ModContent.ItemType<LevelUpTester>(), 1);
            GiveItem(ModContent.ItemType<BossSummonBag>(), 1);
            GiveItem(ModContent.ItemType<Items.SeerOrb>(), 1);
            GiveItem(ModContent.ItemType<Weapons.TestMagicRing>(), 1);
            GiveItem(ModContent.ItemType<Weapons.TestSparkle>(), 1);
            GiveItem(ModContent.ItemType<Weapons.ProjBeam>(), 1);
            GiveItem(ModContent.ItemType<Weapons.TestMagicRingV2>(), 1);
            GiveItem(ModContent.ItemType<Weapons.ColorRainbow>(), 1);
            GiveItem(ModContent.ItemType<Weapons.ColorRed>(), 1);
            GiveItem(ModContent.ItemType<Weapons.ColorYellow>(), 1);
            GiveItem(ModContent.ItemType<Weapons.ColorGreen>(), 1);
            // v5.76: armas mantenidas
            GiveItem(ModContent.ItemType<Weapons.V20.BlackHoleStaff>(), 1);
            GiveItem(ModContent.ItemType<Weapons.V20.PhoenixNovaStaff>(), 1);
            GiveItem(ModContent.ItemType<Weapons.V20.QuantumSplitStaff>(), 1);
            GiveItem(ModContent.ItemType<Weapons.V20.PlasmaOrbStaff>(), 1);
        }

        private void GiveItem(int itemType, int stack)
        {
            for (int i = 0; i < 58; i++)
            {
                if (Player.inventory[i] == null ||
                    Player.inventory[i].type == Terraria.ID.ItemID.None)
                {
                    Player.inventory[i].SetDefaults(itemType);
                    Player.inventory[i].stack = stack;
                    return;
                }
            }
            int drop = Item.NewItem(Player.GetSource_GiftOrReward(), Player.Center, itemType, stack);
            if (drop >= 0 && drop < Main.item.Length)
                Main.item[drop].noGrabDelay = 0;
        }
    }
}
