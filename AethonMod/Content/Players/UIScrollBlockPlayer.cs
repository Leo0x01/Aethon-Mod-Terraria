using Terraria;
using Terraria.ModLoader;
using AethonMod.Content.Systems;

namespace AethonMod.Content.Players
{
    /// <summary>
    /// Bloquea el movimiento del jugador mientras una UI del mod esté abierta.
    /// Sistema simplificado: solo BranchChoiceUI (selección de arma).
    /// </summary>
    public class UIScrollBlockPlayer : ModPlayer
    {
        public override void PreUpdate()
        {
            var ui = ModContent.GetInstance<UISystem>();
            if (ui == null) return;
            if (!(ui.BranchChoiceUI?.IsVisible ?? false)) return;
            Player.controlLeft = false;
            Player.controlRight = false;
            Player.controlUp = false;
            Player.controlDown = false;
            Player.controlJump = false;
            Player.controlUseItem = false;
            Player.controlUseTile = false;
            Player.grappling[0] = -1;
            if (Main.playerInventory) Main.playerInventory = false;
        }

        public override void PreUpdateMovement()
        {
            var ui = ModContent.GetInstance<UISystem>();
            if (ui == null) return;
            if (ui.BranchChoiceUI?.IsVisible ?? false)
            {
                Player.velocity.X = 0;
                Player.controlLeft = false;
                Player.controlRight = false;
                Player.controlJump = false;
            }
        }
    }
}
