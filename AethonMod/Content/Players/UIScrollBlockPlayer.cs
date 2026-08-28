using Terraria;
using Terraria.ModLoader;
using AethonMod.Content.Systems;

namespace AethonMod.Content.Players
{
    /// <summary>
    /// ModPlayer que bloquea TODA interaccion con el juego mientras las UIs del mod estan abiertas.
    /// Replica el comportamiento del bestiario: el juego se pausa completamente.
    /// </summary>
    public class UIScrollBlockPlayer : ModPlayer
    {
        /// <summary>Delta del scroll wheel para esta frame (lo guardamos antes de resetear).</summary>
        public static int ScrollDelta = 0;

        public override void PreUpdate()
        {
            var ui = ModContent.GetInstance<UISystem>();
            if (ui == null) return;

            bool anyUIOpen = (ui.SkillTreeUI?.IsVisible ?? false) || (ui.CodexUI?.IsVisible ?? false) || (ui.BranchChoiceUI?.IsVisible ?? false);
            if (anyUIOpen)
            {
                // === GUARDAR EL DELTA DEL SCROLL ANTES DE RESETEARLO ===
                // Esto permite que las UIs lean cuánto se scrolleó en esta frame.
                ScrollDelta = Terraria.GameInput.PlayerInput.ScrollWheelValue - Terraria.GameInput.PlayerInput.ScrollWheelValueOld;

                // === BLOQUEAR SCROLL DEL INVENTARIO ===
                // Resetear inmediatamente para que el juego vanilla no lo procese (hotbar, etc.)
                Terraria.GameInput.PlayerInput.ScrollWheelValue = Terraria.GameInput.PlayerInput.ScrollWheelValueOld;

                // === BLOQUEAR INTERACCION CON EL MUNDO ===
                Main.mouseLeft = false;
                Main.mouseRight = false;
                Main.mouseLeftRelease = false;
                Main.mouseRightRelease = false;

                // === BLOQUEAR INVENTARIO ===
                if (Main.playerInventory)
                    Main.playerInventory = false;

                // === BLOQUEAR USO DE ITEMS ===
                Player.itemTime = 0;
                Player.itemAnimation = 0;

                // === BLOQUEAR MOVIMIENTO COMPLETAMENTE ===
                // Detener TODO el movimiento del jugador (no solo dampenar)
                Player.velocity.X = 0;
                // No detener Y para que el jugador pueda caer si está en el aire
                // pero sí detener el control de movimiento
                Player.controlLeft = false;
                Player.controlRight = false;
                Player.controlUp = false;
                Player.controlDown = false;
                Player.controlJump = false;
                Player.controlUseItem = false;
                Player.controlUseTile = false;
                Player.grappling[0] = -1; // cancelar gancho
            }
            else
            {
                ScrollDelta = 0;
            }
        }

        public override void PreUpdateMovement()
        {
            var ui = ModContent.GetInstance<UISystem>();
            if (ui == null) return;

            bool anyUIOpen = (ui.SkillTreeUI?.IsVisible ?? false) || (ui.CodexUI?.IsVisible ?? false) || (ui.BranchChoiceUI?.IsVisible ?? false);
            if (anyUIOpen)
            {
                // Detener al jugador completamente
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
