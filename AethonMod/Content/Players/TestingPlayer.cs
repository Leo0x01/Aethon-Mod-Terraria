using Terraria;
using Terraria.ModLoader;
using AethonMod.Content.Items;

namespace AethonMod.Content.Players
{
    /// <summary>
    /// TestingPlayer — el kit de pruebas del arsenal.
    ///
    /// v5.98 — FIX DEL KIT "CONGELADO": el kit completo solo se entrega UNA
    /// vez (gate por GenesisShard), así que quien ya había entrado al mundo
    /// con una versión anterior NUNCA recibía las armas añadidas después
    /// (exactamente lo que pasó con la Medusa Nebular). Ahora el kit base
    /// sigue siendo de una sola vez, pero las ARMAS CÓSMICAS EN DESARROLLO
    /// se garantizan INDIVIDUALMENTE en cada entrada al mundo: si falta una
    /// (se eliminó, se guardó con una versión vieja, lo que sea), vuelve al
    /// inventario — la Medusa Nebular SIEMPRE está ahí desde el inicio.
    /// </summary>
    public class TestingPlayer : ModPlayer
    {
        public override void OnEnterWorld()
        {
            if (Main.netMode != Terraria.ID.NetmodeID.SinglePlayer) return;
            if (Player.whoAmI != Main.myPlayer) return;

            // === KIT BASE (una sola vez) ===
            if (!HasItem(ModContent.ItemType<GenesisShard>()))
            {
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
                // v5.80+: armas cósmicas basadas en shaders de lensing
                GiveItem(ModContent.ItemType<Weapons.Cosmic.BlackHoleStaff>(), 1);
                GiveItem(ModContent.ItemType<Weapons.Cosmic.SunStaff>(), 1);
                // v5.96: EL OJO DEL VACÍO (arma de terror cósmico)
                GiveItem(ModContent.ItemType<Weapons.Cosmic.VoidEyeStaff>(), 1);
                // v5.97: LA MEDUSA NEBULAR (invocador de minion cósmico)
                GiveItem(ModContent.ItemType<Weapons.Cosmic.MedusaNebularStaff>(), 1);
            }

            // === GARANTÍA INDIVIDUAL (v5.98) — las armas cósmicas en
            // desarrollo SIEMPRE están en el inventario, venga de la
            // versión que venga el guardado del jugador ===
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.BlackHoleStaff>());
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.SunStaff>());
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.VoidEyeStaff>());
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.MedusaNebularStaff>());
        }

        /// <summary>¿El jugador tiene este ítem en el inventario (58 slots)?</summary>
        private bool HasItem(int itemType)
        {
            for (int i = 0; i < 58; i++)
            {
                if (Player.inventory[i] != null &&
                    Player.inventory[i].type == itemType)
                    return true;
            }
            return false;
        }

        /// <summary>Entrega el ítem SOLO si no lo tiene (garantía por arma).</summary>
        private void EnsureItem(int itemType)
        {
            if (HasItem(itemType)) return;
            GiveItem(itemType, 1);
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
