using Terraria;
using Terraria.ModLoader;
using AethonMod.Content.Systems;

namespace AethonMod.Content.Players
{
    /// <summary>
    /// ModPlayer que bloquea TODA interaccion con el juego mientras las UIs del mod estan abiertas.
    ///
    /// ESTRATEGIA: Usar Main.playerInventory = true que pone el juego en modo "menu"
    /// (como el bestiario/inventario). En este modo, el juego NO procesa clicks del
    /// mouse para atacar/colocar bloques, pero las UIs pueden seguir leyendo
    /// Main.mouseLeft/mouseRight via el sistema de captura.
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

            if (!anyUIOpen)
            {
                // Si no hay UI abierta, asegurar que el inventario este cerrado
                // (lo abrimos para bloquear el juego, pero lo cerramos cuando no hay UI)
                return;
            }

            // === 1. CAPTURAR ESTADO DEL MOUSE ===
            UISystem.CaptureAndBlockInput();

            // === 2. BLOQUEAR EL JUEGO CON Main.playerInventory ===
            // Esto pone el juego en modo "menu" donde no procesa clicks del mundo
            // pero las UIs pueden leer el estado del mouse libremente
            Main.playerInventory = true;

            // === 3. BLOQUEAR MOVIMIENTO ===
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
