using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Globals;

namespace AethonMod.Content.Globals
{
    /// <summary>
    /// TooltipToggleItem — GlobalItem que detecta clicks derechos en el Grimorio
    /// mientras está en el inventario y alterna el flag ShowExtendedTooltip.
    ///
    /// v5.5: En tModLoader 1.4.4 no hay hook directo para interceptar click
    /// derecho en inventario sin consumir el item. CanRightClick()=true hace
    /// que el item se consuma (como una poción). Por eso usamos el hook
    /// ModifyTooltips (que se llama cada frame mientras el tooltip está visible)
    /// para detectar el flanco de subida del click derecho.
    ///
    /// El problema del commit v5.4 era que ModifyTooltips se llama múltiples
    /// veces (para el item y para el hover item), causando toggles dobles.
    /// Este GlobalItem solo aplica al Grimorio y trackea el estado del click
    /// de forma estática para evitar toggles múltiples.
    /// </summary>
    public class TooltipToggleItem : GlobalItem
    {
        public override bool InstancePerEntity => false; // no necesita per-entity

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

            // Detectar flanco de subida del click derecho
            bool rightMouseNow = Main.mouseRight;
            if (!rightMouseNow || _rightMouseLast)
            {
                _rightMouseLast = rightMouseNow;
                return;
            }
            _rightMouseLast = rightMouseNow;

            // Alternar el flag
            var sl = item.GetGlobalItem<ShardLevelItem>();
            if (sl == null) return;

            sl.ShowExtendedTooltip = !sl.ShowExtendedTooltip;
            string mode = sl.ShowExtendedTooltip ? "completa" : "básica";
            Main.NewText($"Grimorio: vista {mode}",
                new Microsoft.Xna.Framework.Color(245, 196, 81));
            Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuOpen);
        }
    }
}
