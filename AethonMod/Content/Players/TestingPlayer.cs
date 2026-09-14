using Terraria;
using Terraria.ModLoader;
using AethonMod.Content.Items;

namespace AethonMod.Content.Players
{
    /// <summary>
    /// TestingPlayer — el kit de pruebas del arsenal.
    ///
    /// v6.27 — LA BOLSA: petición del usuario ("todo lo que le vas a dar
    /// al jugador ponlo en una bolsa o cofre y dale solo la bolsa con
    /// todos los objetos dentro"). OnEnterWorld ya NO inunda el
    /// inventario con 40+ ítems: entrega SOLO LA BOLSA DEL ARSENAL
    /// PRIMORDIAL (1 ranura) y el jugador la abre con clic derecho
    /// cuando quiera — el contenido completo (kit base + arsenal) vive
    /// ahora en ArsenalBag.Contenido(), el punto único de la verdad.
    ///
    /// Histórico: v5.98 kit congelado + garantía individual por arma
    /// (40+ EnsureItem); v6.01/v6.18/v6.26 altas y bajas de la gran
    /// limpieza — todo eso ahora es UNA línea.
    /// </summary>
    public class TestingPlayer : ModPlayer
    {
        public override void OnEnterWorld()
        {
            if (Main.netMode != Terraria.ID.NetmodeID.SinglePlayer) return;
            if (Player.whoAmI != Main.myPlayer) return;

            // === EL KIT COMPLETO EN UNA RANULA: LA BOLSA (v6.27) ===
            // Garantizada en cada entrada (si la borraste, vuelve); su
            // contenido se despliega con clic derecho y semántica de
            // "solo lo que falte" — reabrirla repone armas perdidas.
            if (!HasItem(ModContent.ItemType<ArsenalBag>()))
            {
                GiveItem(ModContent.ItemType<ArsenalBag>(), 1);
                if (Player.whoAmI == Main.myPlayer)
                {
                    Terraria.Main.NewText(
                        "La Bolsa del Arsenal Primordial llega contigo: clic derecho para desplegar todo el arsenal.",
                        new Microsoft.Xna.Framework.Color(230, 196, 255));
                }
            }
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
