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
            // v5.77: 20 armas creativas (BlackHoleStaff movido a Cosmic)
            GiveItem(ModContent.ItemType<Weapons.V20.BlackHoleMiniStaff>(), 1);
            GiveItem(ModContent.ItemType<Weapons.V20.TornadoStaff>(), 1);
            GiveItem(ModContent.ItemType<Weapons.V20.PrismBeamStaff>(), 1);
            GiveItem(ModContent.ItemType<Weapons.V20.EarthquakeStaff>(), 1);
            GiveItem(ModContent.ItemType<Weapons.V20.MirrorDimensionStaff>(), 1);
            GiveItem(ModContent.ItemType<Weapons.V20.GravityPulseStaff>(), 1);
            GiveItem(ModContent.ItemType<Weapons.V20.ShadowCloneStaff>(), 1);
            GiveItem(ModContent.ItemType<Weapons.V20.CrystalShatterStaff>(), 1);
            GiveItem(ModContent.ItemType<Weapons.V20.SupernovaStaff>(), 1);
            GiveItem(ModContent.ItemType<Weapons.V20.VortexChainStaff>(), 1);
            GiveItem(ModContent.ItemType<Weapons.V20.AbyssalEyeStaff>(), 1);
            GiveItem(ModContent.ItemType<Weapons.V20.SpectralMirageStaff>(), 1);
            GiveItem(ModContent.ItemType<Weapons.V20.TemporalRiftStaff>(), 1);
            GiveItem(ModContent.ItemType<Weapons.V20.PlasmaStormStaff>(), 1);
            GiveItem(ModContent.ItemType<Weapons.V20.InfernoTornadoStaff>(), 1);
            GiveItem(ModContent.ItemType<Weapons.V20.VoidEaterStaff>(), 1);
            GiveItem(ModContent.ItemType<Weapons.V20.PhoenixNovaStaff>(), 1);
            GiveItem(ModContent.ItemType<Weapons.V20.QuantumSplitStaff>(), 1);
            GiveItem(ModContent.ItemType<Weapons.V20.PlasmaOrbStaff>(), 1);
            // v5.80: armas cósmicas basadas en shaders de lensing gravitacional
            GiveItem(ModContent.ItemType<Weapons.Cosmic.BlackHoleStaff>(), 1);
            GiveItem(ModContent.ItemType<Weapons.Cosmic.SunStaff>(), 1);
            // v5.96: EL OJO DEL VACÍO (arma de terror cósmico — petición del
            // usuario; v5.97: FIX — faltaba dársela al jugador aquí)
            GiveItem(ModContent.ItemType<Weapons.Cosmic.VoidEyeStaff>(), 1);
            // v5.97: LA MEDUSA NEBULAR (invocador de minion cósmico — petición
            // del usuario: "crea una nueva arma que sea un invocador para un
            // minion… el proyectil será la invocación")
            GiveItem(ModContent.ItemType<Weapons.Cosmic.MedusaNebularStaff>(), 1);
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
