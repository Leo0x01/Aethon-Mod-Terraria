using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Items.Bolsas;

namespace AethonMod.Content.Players
{
    /// <summary>
    /// TestingPlayer — el kit de pruebas del arsenal.
    ///
    /// v6.29 — LAS BOLSAS POR CATEGORÍA + LAS DUMMIES: petición del usuario
    /// ("crea varias bolsas para todas las armas que me tienes que dar no
    /// solo una y separalas por categorías, una categoría por bolsa · al
    /// jugador también dale 99 Dummy para probar las armas"). OnEnterWorld
    /// entrega:
    ///
    ///   · LAS DIEZ BOLSAS (una por categoría del arsenal — cada una se
    ///     abre con clic derecho y solo entrega lo que falte):
    ///       1. La Bolsa del Probador          (herramientas de prueba)
    ///       2. La Bolsa de los Fundacionales  (los 4 del alba)
    ///       3. La Bolsa de los Clásicos       (el sol y sus criaturas)
    ///       4. La Bolsa de los Agujeros Negros (los 10)
    ///       5. La Bolsa de los Soles Rúnicos  (los 20)
    ///       6. La Bolsa de las Estrellas Reales (las 7)
    ///       7. La Bolsa de las Librerías      (las 6 VFX)
    ///       8. La Bolsa de los Creativos      (los 6 experimentos)
    ///       9. La Bolsa de los Exhumados      (el rencor + la eminencia)
    ///      10. La Bolsa de los Cosméticos    (las coronas)
    ///      11. La Bolsa de las Dos Formas    (v6.33)
    ///      12. La Bolsa de los Desgarros     (v6.33 + v6.35)
    ///      13. La Bolsa de los Códigos Vivos (v6.36 — los 4 de las imágenes)
    ///      14. La Bolsa de las Sierpes     (v6.38 — la familia)
    ///      15. La Bolsa de los Huéspedes   (v6.41 — las réplicas de prueba)
    ///      16. La Bolsa de las Apuestas   (v6.42 — las 5 apuestas + el Verbo)
    ///   · 99 DUMMIES DE PRUEBA (Target Dummy de vanilla — el campo de
    ///     entrenamiento directo en el inventario).
    ///
    /// Histórico: v5.98 kit congelado + 40+ EnsureItem; v6.27 UNA bolsa
    /// (ArsenalBag); v6.29 la bolsa única RETIRADA — una por categoría.
    /// </summary>
    public class TestingPlayer : ModPlayer
    {
        public override void OnEnterWorld()
        {
            if (Main.netMode != Terraria.ID.NetmodeID.SinglePlayer) return;
            if (Player.whoAmI != Main.myPlayer) return;

            // === LAS DIECISÉIS BOLSAS (garantizadas en cada entrada) ===
            int bolsas = 0;
            bolsas += Entregar(ModContent.ItemType<BolsaProbador>());
            bolsas += Entregar(ModContent.ItemType<BolsaFundacionales>());
            bolsas += Entregar(ModContent.ItemType<BolsaClasicosCosmicos>());
            bolsas += Entregar(ModContent.ItemType<BolsaAgujerosNegros>());
            bolsas += Entregar(ModContent.ItemType<BolsaSolesRunicos>());
            bolsas += Entregar(ModContent.ItemType<BolsaEstrellasReales>());
            bolsas += Entregar(ModContent.ItemType<BolsaArmasLibrerias>());
            bolsas += Entregar(ModContent.ItemType<BolsaBastonesCreativos>());
            bolsas += Entregar(ModContent.ItemType<BolsaExhumados>());
            bolsas += Entregar(ModContent.ItemType<BolsaCosmeticos>());
            bolsas += Entregar(ModContent.ItemType<BolsaDosFormas>());      // v6.33: LAS 4 DE LAS DOS FORMAS
            bolsas += Entregar(ModContent.ItemType<BolsaDesgarros>());      // v6.33+v6.35: LOS 9 DESGARROS
            bolsas += Entregar(ModContent.ItemType<BolsaCodigosVivos>());   // v6.36: LOS 4 CÓDIGOS VIVOS
            bolsas += Entregar(ModContent.ItemType<BolsaSierpes>());        // v6.38: LA FAMILIA DE LAS SIERPES
            bolsas += Entregar(ModContent.ItemType<BolsaHuespedes>());      // v6.41: LAS RÉPLICAS DE PRUEBA
            bolsas += Entregar(ModContent.ItemType<BolsaApuestas>());        // v6.42: LAS 5 APUESTAS + EL VERBO

            // === LAS 99 DUMMIES DE PRUEBA (el campo de entrenamiento) ===
            int dummies = 0;
            if (!HasItem(ItemID.TargetDummy))
            {
                GiveItem(ItemID.TargetDummy, 99);
                dummies = 99;
            }

            if (bolsas > 0 && Player.whoAmI == Main.myPlayer)
            {
                // v6.50.2 — FIX (strings hardcodeados → hjson, regla de la
                // casa): los avisos del kit de pruebas viajan por clave.
                Terraria.Main.NewText(
                    Terraria.Localization.Language.GetTextValue("Mods.AethonMod.TestingPlayer.Bolsas"),
                    new Microsoft.Xna.Framework.Color(230, 196, 255));
                if (dummies > 0)
                    Terraria.Main.NewText(
                        Terraria.Localization.Language.GetTextValue("Mods.AethonMod.TestingPlayer.Dummies", dummies),
                        new Microsoft.Xna.Framework.Color(255, 216, 107));
            }
        }

        /// <summary>Entrega el ítem si no se tiene; devuelve 1 si se entregó.</summary>
        private int Entregar(int itemType)
        {
            if (HasItem(itemType)) return 0;
            GiveItem(itemType, 1);
            return 1;
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
