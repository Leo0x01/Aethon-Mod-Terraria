using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Weapons;
using AethonMod.Content.Weapons.TestStaffs;

namespace AethonMod.Content.Items
{
    /// <summary>
    /// Cofre de Pruebas Cósmico — item de testing que contiene TODOS los bastones
    /// de prueba + los items ceremoniales creados (StellarDust, AethonSigil, ResonanceShard).
    ///
    /// Al usarlo (click izquierdo), despliega todos los items en el inventario del jugador.
    /// Es la forma más rápida de probar todos los efectos sin tener que craftear nada.
    ///
    /// NOTA v5.53: Los bastones TestMagicRing/TestSparkle/ProjBeam/TestMagicRingV2
    /// vienen del remote (TestAdvanced.cs, namespace Weapons). Los TestNightglow*
    /// son nuevos (mi trabajo local, namespace Weapons.TestStaffs).
    /// </summary>
    public class TestStaffChest : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.maxStack = 1;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.UseSound = SoundID.Item4;
            Item.consumable = false; // reutilizable
        }

        public override bool? UseItem(Player player)
        {
            if (Main.myPlayer != player.whoAmI) return null;

            // === BASTONES DEL REMOTE (TestAdvanced.cs) ===
            GiveItem(player, ModContent.ItemType<TestMagicRing>(), 1);
            GiveItem(player, ModContent.ItemType<TestSparkle>(), 1);
            GiveItem(player, ModContent.ItemType<ProjBeam>(), 1);
            GiveItem(player, ModContent.ItemType<TestMagicRingV2>(), 1);
            // Armas de color del remote
            GiveItem(player, ModContent.ItemType<ColorRainbow>(), 1);
            GiveItem(player, ModContent.ItemType<ColorRed>(), 1);
            GiveItem(player, ModContent.ItemType<ColorYellow>(), 1);
            GiveItem(player, ModContent.ItemType<ColorGreen>(), 1);

            // === 12 BASTONES NUEVOS (mi trabajo local, namespace Weapons.TestStaffs) ===
            GiveItem(player, ModContent.ItemType<TestNightglowBasic>(), 1);
            GiveItem(player, ModContent.ItemType<TestNightglowCosmicTrail>(), 1);
            GiveItem(player, ModContent.ItemType<TestNightglowStarWrath>(), 1); // recrea la imagen
            GiveItem(player, ModContent.ItemType<TestNightglowRingBurst>(), 1);
            GiveItem(player, ModContent.ItemType<TestNightglowSparkleTrail>(), 1);
            GiveItem(player, ModContent.ItemType<TestNightglowLightBeams>(), 1);
            GiveItem(player, ModContent.ItemType<TestNightglowStarfall>(), 1);
            GiveItem(player, ModContent.ItemType<TestNightglowLifesteal>(), 1);
            GiveItem(player, ModContent.ItemType<TestNightglowEmpower>(), 1);
            GiveItem(player, ModContent.ItemType<TestNightglowMultishot>(), 1);
            GiveItem(player, ModContent.ItemType<TestNightglowRainbowTrail>(), 1);
            GiveItem(player, ModContent.ItemType<TestNightglowSupernova>(), 1);

            // === ITEMS CEREMONIALES (los que creé) ===
            GiveItem(player, ModContent.ItemType<StellarDust>(), 50);
            GiveItem(player, ModContent.ItemType<AethonSigil>(), 1);
            GiveItem(player, ModContent.ItemType<ResonanceShard>(), 20);
            GiveItem(player, ModContent.ItemType<GenesisShard>(), 1);
            GiveItem(player, ModContent.ItemType<SeerOrb>(), 1);

            // Mensaje + efectos visuales
            Main.NewText("✦ Cofre de Pruebas Cósmico desplegado: 16 bastones + 6 items ceremoniales.",
                new Color(245, 196, 81));
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4);
            for (int d = 0; d < 40; d++)
            {
                Dust.NewDustPerfect(player.Center, DustID.GoldFlame,
                    new Vector2(
                        Main.rand.NextFloat(-6, 6),
                        Main.rand.NextFloat(-6, 6)),
                    100, new Color(245, 196, 81), 1.5f);
            }
            return true;
        }

        private void GiveItem(Player player, int itemType, int stack)
        {
            // Buscar slot vacío o stack parcial
            for (int i = 0; i < 58; i++)
            {
                Item inv = player.inventory[i];
                if (inv == null || inv.type == ItemID.None)
                {
                    inv.SetDefaults(itemType);
                    inv.stack = stack;
                    return;
                }
                // Si ya tiene el item y es stackable, no duplicar (para stacks > 1)
                if (inv.type == itemType && inv.stack < inv.maxStack && stack > 1)
                {
                    int space = inv.maxStack - inv.stack;
                    int toAdd = System.Math.Min(space, stack);
                    inv.stack += toAdd;
                    stack -= toAdd;
                    if (stack <= 0) return;
                }
            }
            // Si el inventario está lleno, spawn el item en el suelo
            if (stack > 0)
            {
                int drop = Item.NewItem(player.GetSource_FromThis(), player.Center, itemType, stack);
                if (drop >= 0 && drop < Main.item.Length)
                    Main.item[drop].noGrabDelay = 0;
            }
        }
    }
}
