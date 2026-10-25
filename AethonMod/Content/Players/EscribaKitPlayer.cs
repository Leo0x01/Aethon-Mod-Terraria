using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Items.Bolsas;

namespace AethonMod.Content.Players
{
    /// <summary>
    /// ESCRIBAKITPLAYER — v6.52.0 — LA ENTREGA DE LA BOLSA NUEVA.
    ///
    /// LA REGLA DEL CICLO (la letra del usuario: «no toques nada del
    /// mod… en su lugar crea algo nuevo»): esta entrega es un archivo
    /// NUEVO — el kit de las veintiún bolsas (TestingPlayer) NO se toca.
    /// El patrón es el de la casa (garantizada en cada ENTRADA AL
    /// MUNDO, solo el jugador local, en cualquier modo de red — el
    /// modelo ShardPlayer.EntregarRegaloDePruebas): LA BOLSA DEL
    /// ESCRIBA llega sola; lo de dentro se abre con clic derecho.
    /// </summary>
    public class EscribaKitPlayer : ModPlayer
    {
        public override void OnEnterWorld()
        {
            // LA ARMADURA DE LA CASA (v6.50.10): PlayerLoader.OnEnterWorld
            // NO envuelve el hook en try/catch y este cuerpo muta el
            // inventario — una excepción aquí abortaría la entrada.
            try
            {
                if (Player.whoAmI != Main.myPlayer) return;

                int bolsa = ModContent.ItemType<BolsaDelEscriba>();
                if (YaLaTiene(Player, bolsa)) return;

                // Al primer hueco (0–49); si no hay, a sus pies.
                int hueco = -1;
                for (int i = 0; i < 50; i++)
                    if (Player.inventory[i] == null || Player.inventory[i].IsAir) { hueco = i; break; }

                if (hueco >= 0)
                {
                    Item nuevo = new();
                    nuevo.SetDefaults(bolsa);
                    Player.inventory[hueco] = nuevo;
                }
                else
                    Player.QuickSpawnItem(Player.GetSource_Misc("BolsaDelEscriba"), bolsa, 1);

                Main.NewText(Terraria.Localization.Language.GetTextValue(
                    "Mods.AethonMod.Escriba.BolsaEntregada"),
                    new Microsoft.Xna.Framework.Color(212, 190, 255));
            }
            catch { }
        }

        /// <summary>¿Ya lleva la bolsa? (inventario, armadura, misc — el
        /// patrón YaLoTiene de la casa con .Length, la lección v6.50.54).</summary>
        private static bool YaLaTiene(Player p, int tipo)
        {
            for (int i = 0; i < p.inventory.Length; i++)
                if (p.inventory[i] != null && !p.inventory[i].IsAir && p.inventory[i].type == tipo)
                    return true;
            for (int i = 0; i < p.armor.Length; i++)
                if (p.armor[i] != null && !p.armor[i].IsAir && p.armor[i].type == tipo)
                    return true;
            for (int i = 0; i < p.miscEquips.Length; i++)
                if (p.miscEquips[i] != null && !p.miscEquips[i].IsAir && p.miscEquips[i].type == tipo)
                    return true;
            return false;
        }
    }
}
