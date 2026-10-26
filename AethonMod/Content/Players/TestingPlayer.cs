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
    ///   · LAS VEINTIUNA BOLSAS (una por categoría del arsenal — cada una se
    ///     abre con clic derecho y solo entrega lo que falte):
    ///       1. La Bolsa del Probador          (herramientas de prueba)
    ///       2. La Bolsa de los Fundacionales  (los 4 del alba)
    ///       3. La Bolsa de los Clásicos       (el sol y sus criaturas)
    ///       4. La Bolsa de los Agujeros Negros (los 11)
    ///       5. La Bolsa de los Soles Rúnicos  (los 20)
    ///       6. La Bolsa de las Estrellas Reales (las 19)
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
    ///      17. La Bolsa de las Ideas del Grimorio (v6.50.19 — los 6 futuros del proyectil)
    ///      18. La Bolsa de las Armas de Rayo   (v6.50.18+24 — las armas de rayo)
    ///      19. La Bolsa de las Sombras         (v6.50.63 — LAS FAUCES: las 3 armas
    ///          devoradoras de la v6.50.62 que nacieron sin kit — el feedback
    ///          "te olvidaste darle las armas al jugador", la lección v6.14.2 otra vez)
    ///      20. La Bolsa de las Pruebas de la Emperatriz (v6.50.99)
    ///      21. La Bolsa de la Cría (v6.50.121 — LA MISMA FORMA QUE TODAS: la
    ///          letra del usuario: «la bolsa debe entregarse al jugador de la
    ///          misma forma que todas las demás bolsas del mod» — la pareja de
    ///          mascotas del recuerdo, garantizada en cada entrada al mundo,
    ///          igual que sus veinte hermanas; la entrega tras la pelea de la
    ///          .120 SIGUE viva — la vía canónica; esta es la de pruebas)
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
            // v6.50.10 — ARMADURA: PlayerLoader.OnEnterWorld NO envuelve el
            // hook en try/catch (IL-verificado contra el tML 2026.07.3.0 —
            // a diferencia de SaveData/LoadData) y este cuerpo muta el
            // INVENTARIO en cada entrada: una excepción aquí abortaría la
            // entrada al mundo. La armadura es gratis (mismo patrón que
            // SaveData/NetSend del ShardLevelItem).
            try
            {
            if (Main.netMode != Terraria.ID.NetmodeID.SinglePlayer) return;
            if (Player.whoAmI != Main.myPlayer) return;

            // === v6.54.0 — LA REORGANIZACIÓN DE LAS BOLSAS (la letra del
            //     27/10: «respecto a las bolsas anteriores, guardalas
            //     todas en otra bolsa y de esa forma solo se entregara
            //     esta bolsa con mas bolsas de las viejas, mientras que
            //     la nueva bolsa aparece aparte»): el kit entrega
            //     DOS cosas —
            //       1. EL COFRE DEL ARSENAL (la Bolsa Maestra con las
            //          VEINTITRÉS bolsas de siempre DENTRO), y
            //       2. LA BOLSA DE LA INVESTIGACIÓN aparte (los items
            //          nuevos del estudio de las texturas del dios +
            //          la Bolsa de las Alas con las quince alas). ===
            int bolsas = 0;
            bolsas += Entregar(ModContent.ItemType<BolsaMaestra>());
            bolsas += Entregar(ModContent.ItemType<BolsaInvestigacion>());
            // v6.54.4 — LA BOLSA DE LAS ARMAS DEFINITIVAS aparte (la nueva
            // de esta versión: «El Veredicto de Aethon», las doce sentencias)
            bolsas += Entregar(ModContent.ItemType<BolsaDefinitivas>());

            // v6.51.0 — LA LLAVE DEL DESPERTAR sigue como entrega DIRECTA
            // (no es una bolsa — el probador de la progresión viaja suelto,
            // igual que las dummies: la letra de la reorganización habla de
            // LAS BOLSAS).
            bolsas += Entregar(ModContent.ItemType
                <global::AethonMod.Content.Items.Llamados.EcoDelDespertar>());

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
            catch { } // v6.50.10 — la armadura de arriba
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
