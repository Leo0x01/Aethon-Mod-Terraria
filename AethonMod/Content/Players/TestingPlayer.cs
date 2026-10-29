using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Items.Bolsas;

namespace AethonMod.Content.Players
{
    /// <summary>
    /// TestingPlayer — el kit de pruebas del arsenal.
    ///
    /// v6.56.1 — EL MODELO ACTUAL (la purga v6.55.0 retiró la investigación
    /// y su bolsa; v6.56.0 subió el arsenal): OnEnterWorld (SP + jugador
    /// local) entrega:
    ///
    ///   1. EL COFRE DEL ARSENAL (la Bolsa Maestra: dentro van las
    ///      VEINTICUATRO bolsas de categoría — la de las alas incluida
    ///      desde la v6.55.0; cada una con semántica de garantía: clic
    ///      derecho y solo entrega lo que falte),
    ///   2. LA BOLSA DE LAS ALAS aparte (las veintidós alas procedurales),
    ///   3. LA BOLSA DE LAS ARMAS DEFINITIVAS aparte (la escalera del rayo:
    ///      Alba/Veredicto/Juicio/Prisma/RC2 · las cuatro sentencias · y
    ///      las tres del grimorio v6.56.0: Índice/Pluma/Ojo),
    ///   4. LA LLAVE DEL DESPERTAR (EcoDelDespertar, el probador de la
    ///      progresión — va suelta) y
    ///   5. 99 DUMMIES DE PRUEBA (Target Dummy de vanilla).
    ///
    /// Histórico: v5.98 kit congelado + 40+ EnsureItem; v6.27 UNA bolsa
    /// (ArsenalBag); v6.29 una por categoría; v6.55.0 el cofre maestro.
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

            // === v6.55.0 — LA BOLSA DE LA INVESTIGACIÓN RETIRADA (la
            //     letra: «borra la bolsa de investigación con todas sus
            //     armas que ninguna sirve, deja la bolsa de definitiva y
            //     la bolsa de alas»): el kit entrega ahora —
            //       1. EL COFRE DEL ARSENAL (la Bolsa Maestra con TODAS
            //          las bolsas — incluida la Bolsa de las Alas desde
            //          esta versión),
            //       2. LA BOLSA DE LAS ALAS aparte (las quince alas
            //          procedurales — antes vivía dentro de la
            //          investigación retirada), y
            //       3. LA BOLSA DE LAS ARMAS DEFINITIVAS aparte (las cinco
            //          sentencias + EL RAYO CONCENTRADO II). ===
            int bolsas = 0;
            bolsas += Entregar(ModContent.ItemType<BolsaMaestra>());
            bolsas += Entregar(ModContent.ItemType<BolsaDeAlasPrisma>());
            // v6.54.6 — LA BOLSA DE LAS ARMAS DEFINITIVAS aparte (la
            // reforma: «El Veredicto de Aethon», las cinco sentencias
            // con sus siete capas + el rayo concentrado)
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

        /// <summary>¿El jugador tiene este ítem? (incluido el slot del
        /// cursor — v6.56.1, la misma cura de BolsaCategoria.Tiene).</summary>
        private bool HasItem(int itemType)
        {
            for (int i = 0; i < Player.inventory.Length; i++)
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
