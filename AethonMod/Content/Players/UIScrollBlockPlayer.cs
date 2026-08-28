using Terraria;
using Terraria.ModLoader;
using AethonMod.Content.Systems;

namespace AethonMod.Content.Players
{
    /// <summary>
    /// ModPlayer que bloquea TODA interaccion con el juego mientras las UIs del mod estan abiertas.
    ///
    /// ORDEN DE EJECUCIÓN:
    /// 1. PreUpdate (este ModPlayer) — guarda estado del mouse + bloquea input
    /// 2. Juego procesa input (input ya bloqueado, no hace nada)
    /// 3. PostUpdateInput (UISystem) — UIs leen el estado guardado del mouse
    /// 4. Draw — solo dibuja
    /// </summary>
    public class UIScrollBlockPlayer : ModPlayer
    {
        public override void PreUpdate()
        {
            var ui = ModContent.GetInstance<UISystem>();
            if (ui == null) return;

            bool treeOpen = ui.SkillTreeUI?.IsVisible ?? false;
            bool codexOpen = ui.CodexUI?.IsVisible ?? false;
            bool branchOpen = ui.BranchChoiceUI?.IsVisible ?? false;
            bool anyUIOpen = treeOpen || codexOpen || branchOpen;

            if (!anyUIOpen) return;

            // === 1. GUARDAR ESTADO DEL MOUSE + BLOQUEAR INPUT ===
            UISystem.CaptureAndBlockInput();

            // === 2. BLOQUEAR MOVIMIENTO Y USO DE ITEMS ===
            Player.itemTime = 0;
            Player.itemAnimation = 0;
            Player.velocity.X = 0;
            Player.controlLeft = false;
            Player.controlRight = false;
            Player.controlUp = false;
            Player.controlDown = false;
            Player.controlJump = false;
            Player.controlUseItem = false;
            Player.controlUseTile = false;
            Player.grappling[0] = -1;
        }

        public override void PreUpdateMovement()
        {
            var ui = ModContent.GetInstance<UISystem>();
            if (ui == null) return;

            bool anyUIOpen = (ui.SkillTreeUI?.IsVisible ?? false) || (ui.CodexUI?.IsVisible ?? false) || (ui.BranchChoiceUI?.IsVisible ?? false);
            if (anyUIOpen)
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
