using Terraria;
using Terraria.ModLoader;
using AethonMod.Content.Items;

namespace AethonMod.Content.Players
{
    /// <summary>
    /// Player de TESTING — da items al entrar al mundo para pruebas.
    /// TEMPORAL: eliminar antes de release oficial.
    ///
    /// Items entregados:
    /// - GenesisShard (arma de luz + material del Grimorio)
    /// - 100 GoldBar (para craftear el Grimorio)
    /// - LevelUpTester (+10 niveles al Grimorio por uso)
    /// - BossSummonBag (999 invocadores de cada jefe)
    /// </summary>
    public class TestingPlayer : ModPlayer
    {
        public override void OnEnterWorld()
        {
            if (Main.netMode != Terraria.ID.NetmodeID.SinglePlayer) return;
            if (Player.whoAmI != Main.myPlayer) return;

            // Verificar si ya tiene el GenesisShard (para no duplicar la entrega).
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

            // Entregar el kit de testing en el primer slot vacío de cada item.
            GiveItem(ModContent.ItemType<GenesisShard>(), 1);
            GiveItem(Terraria.ID.ItemID.GoldBar, 100);
            GiveItem(ModContent.ItemType<LevelUpTester>(), 1);
            GiveItem(ModContent.ItemType<BossSummonBag>(), 1);
            GiveItem(ModContent.ItemType<Items.SeerOrb>(), 1); // v5.6: item de prueba para toggle tooltip
            GiveItem(ModContent.ItemType<Weapons.TestStaff>(), 1); // v5.8: arma de prueba para proyectiles/minions
            GiveItem(ModContent.ItemType<Weapons.TestStaffA>(), 1); // v5.23: ModifyWeaponDamage
            GiveItem(ModContent.ItemType<Weapons.TestStaffB>(), 1); // v5.23: ModifyWeaponKnockback
            GiveItem(ModContent.ItemType<Weapons.TestStaffC>(), 1); // v5.23: ModifyManaCost
            GiveItem(ModContent.ItemType<Weapons.TestStaffD>(), 1); // v5.23: UseTimeMultiplier (sospechoso)
        }

        /// <summary>
        /// Coloca un item en el primer slot vacío del inventario.
        /// </summary>
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
        }
    }
}
