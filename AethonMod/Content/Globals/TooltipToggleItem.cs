using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Globals
{
    /// <summary>
    /// TooltipToggleItem — GlobalItem que detecta clicks derechos en el Grimorio
    /// mientras está en el inventario y alterna entre vista básica/completa.
    ///
    /// v5.7: Migrado al patrón del SeerOrb (que funciona correctamente).
    /// Usa un flag ESTÁTICO (no por-item) en vez de modificar ShardLevelItem.
    /// Esto evita el error 134124 que ocurría en v5.6 anterior cuando se
    /// modificaba player.inventory[i] dentro de ModifyTooltips.
    /// </summary>
    public class TooltipToggleItem : GlobalItem
    {
        public override bool InstancePerEntity => false;

        // Flag estático — NO se persiste por-item (igual que SeerOrbToggle)
        public static bool ShowExtendedTooltip = false;

        private static bool _rightMouseLast = false;

        public override bool AppliesToEntity(Item item, bool lateInstantiation)
        {
            try
            {
                return item.type == ModContent.ItemType<Weapons.GrimoireEternal>();
            }
            catch
            {
                return false;
            }
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            // Solo en cliente, con inventario abierto
            if (Main.dedServ || !Main.playerInventory) return;

            // Guard: Main.LocalPlayer puede ser null en pantalla de selección
            if (Main.LocalPlayer == null) return;

            // Detectar flanco de subida del click derecho
            bool rightMouseNow = Main.mouseRight;
            if (!rightMouseNow || _rightMouseLast)
            {
                _rightMouseLast = rightMouseNow;
                return;
            }
            _rightMouseLast = rightMouseNow;

            // Alternar el flag ESTÁTICO (NO toca el inventario)
            ShowExtendedTooltip = !ShowExtendedTooltip;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuOpen);
        }
    }
}
