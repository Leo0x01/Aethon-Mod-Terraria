using Terraria;
using Terraria.ModLoader;
using AethonMod.Content.Systems;

namespace AethonMod.Content.Players
{
    /// <summary>
    /// ModPlayer que bloquea el movimiento del jugador mientras las UIs están abiertas.
    /// Player.mouseInterface = true se setea dentro de SkillTreeUIState.DrawSelf
    /// (igual que AnRPG), no aquí.
    /// </summary>
    public class UIScrollBlockPlayer : ModPlayer
    {
        public override void PreUpdate()
        {
            var ui = ModContent.GetInstance<UISystem>();
            if (ui == null) return;

            bool any = (ui.SkillTreeUI?.IsVisible ?? false) ||
                       (ui.CodexUI?.IsVisible ?? false) ||
                       (ui.BranchChoiceUI?.IsVisible ?? false);
            if (!any) return;

            // Bloquear movimiento (igual que AnRPG)
            Player.controlLeft = false;
            Player.controlRight = false;
            Player.controlUp = false;
            Player.controlDown = false;
            Player.controlJump = false;
            Player.controlUseItem = false;
            Player.controlUseTile = false;
            Player.grappling[0] = -1;

            // Cerrar inventario
            if (Main.playerInventory) Main.playerInventory = false;
        }

        public override void PreUpdateMovement()
        {
            var ui = ModContent.GetInstance<UISystem>();
            if (ui == null) return;
            bool any = (ui.SkillTreeUI?.IsVisible ?? false) || (ui.CodexUI?.IsVisible ?? false) || (ui.BranchChoiceUI?.IsVisible ?? false);
            if (any)
            {
                Player.velocity.X = 0;
                Player.controlLeft = false;
                Player.controlRight = false;
                Player.controlUp = false;
                Player.controlDown = false;
                Player.controlJump = false;
            }
        }
    }
}
