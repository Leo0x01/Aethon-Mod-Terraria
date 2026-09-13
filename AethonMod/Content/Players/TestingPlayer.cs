using Terraria;
using Terraria.ModLoader;
using AethonMod.Content.Items;

namespace AethonMod.Content.Players
{
    /// <summary>
    /// TestingPlayer — el kit de pruebas del arsenal.
    ///
    /// v6.01 — LA GRAN LIMPIEZA: el usuario seleccionó qué se queda.
    /// FUERA: las 4 armas de COLOR (Rainbow/Red/Yellow/Green), 15 de las
    /// 20 V20 (Tornado, PrismBeam, Earthquake, MirrorDimension,
    /// GravityPulse, ShadowClone, CrystalShatter, VortexChain, AbyssalEye,
    /// SpectralMirage, TemporalRift, InfernoTornado, VoidEater, PlasmaOrb,
    /// BlackHoleMini) y las 2 cósmicas nuevas (Quásar y Galaxia Viviente —
    /// "se ven horrible y son muy simples"). SE QUEDAN 14: los 4 tests
    /// clásicos, el Grimorio, 4 V20 (Supernova, PlasmaStorm, PhoenixNova,
    /// QuantumSplit) y las 5 cósmicas (Agujero, Sol, Medusa, Cometa, Púlsar).
    ///
    /// v5.98 — FIX DEL KIT "CONGELADO": el kit base se entrega UNA sola vez
    /// (gate por GenesisShard), pero las ARMAS CÓSMICAS EN DESARROLLO se
    /// garantizan INDIVIDUALMENTE en cada entrada al mundo.
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
                // v5.77: arsenal creativo V20 — v6.01: solo los 4 ELEGIDOS
                GiveItem(ModContent.ItemType<Weapons.V20.SupernovaStaff>(), 1);
                GiveItem(ModContent.ItemType<Weapons.V20.PlasmaStormStaff>(), 1);
                GiveItem(ModContent.ItemType<Weapons.V20.PhoenixNovaStaff>(), 1);
                GiveItem(ModContent.ItemType<Weapons.V20.QuantumSplitStaff>(), 1);
                // v5.80+: armas cósmicas basadas en shaders de lensing
                GiveItem(ModContent.ItemType<Weapons.Cosmic.BlackHoleStaff>(), 1);
                GiveItem(ModContent.ItemType<Weapons.Cosmic.SunStaff>(), 1);
                // v5.97: LA MEDUSA NEBULAR (invocador de minion cósmico)
                GiveItem(ModContent.ItemType<Weapons.Cosmic.MedusaNebularStaff>(), 1);
            }

            // === GARANTÍA INDIVIDUAL (v5.98) — las armas cósmicas en
            // desarrollo SIEMPRE están en el inventario, venga de la
            // versión que venga el guardado del jugador ===
            // (v6.01: QUÁSAR y GALAXIA VIVIENTE ELIMINADOS — "se ven
            // horrible y son muy simples, no vale la pena que continúen";
            // quien aún los tenga guardados los conserva, pero ya no se
            // garantizan. El OJO DEL VACÍO corrió la misma suerte en v6.00.)
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.BlackHoleStaff>());
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.SunStaff>());
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.MedusaNebularStaff>());
            // v5.99: EL COMETA ESTELAR y EL PÚLSAR VIVO (invocadores)
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.LivingCometStaff>());
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.LivingPulsarStaff>());
            // v6.02: EL AGUJERO NEGRO CARMESÍ (hoy con el visual de la referencia)
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.CrimsonBlackHoleStaff>());
            // v6.03: LOS COSMÉTICOS DE LAS DOS CORONAS (la del agujero,
            // detrás de la cabeza, y la rúnica nueva, flotando sobre ella)
            EnsureItem(ModContent.ItemType<Items.Cosmetics.VoidCrownItem>());
            EnsureItem(ModContent.ItemType<Items.Cosmetics.RuneCrownItem>());
            // v6.08: LAS 8 ALAS DE LUZ — TODO el sistema es ahora técnica
            // coronas (las 8 de spritesheet fueron BORRADAS a petición del
            // usuario; quien las tenga guardadas las conserva pero ya no
            // se garantizan). Mariposa y Hada NUEVAS + las 6 restantes.
            EnsureItem(ModContent.ItemType<Items.Wings.EventHorizonWings>());
            EnsureItem(ModContent.ItemType<Items.Wings.PhotonRingWings>());
            EnsureItem(ModContent.ItemType<Items.Wings.CosmicButterflyWings>());
            EnsureItem(ModContent.ItemType<Items.Wings.StardustFairyWings>());
            EnsureItem(ModContent.ItemType<Items.Wings.SolarCoronaWings>());
            EnsureItem(ModContent.ItemType<Items.Wings.LivingNebulaWings>());
            EnsureItem(ModContent.ItemType<Items.Wings.TotalEclipseWings>());
            EnsureItem(ModContent.ItemType<Items.Wings.CrimsonCometWings>());
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
