using Terraria;
using Terraria.ModLoader;
using AethonMod.Content.Systems;

namespace AethonMod.Content.Players
{
    /// <summary>
    /// ModPlayer que bloquea el scroll del inventario mientras las UIs del mod estan abiertas.
    /// Se ejecuta ANTES de que el juego procese el input del jugador (PreUpdate).
    /// </summary>
    public class UIScrollBlockPlayer : ModPlayer
    {
        public override void PreUpdate()
        {
            // Si alguna UI del mod esta abierta, resetear el scroll value ANTES de que
            // el juego vanilla lo procese (esto evita que el hotbar se mueva).
            var ui = ModContent.GetInstance<UISystem>();
            if (ui == null) return;

            bool anyUIOpen = (ui.SkillTreeUI?.IsVisible ?? false) || (ui.CodexUI?.IsVisible ?? false);
            if (anyUIOpen)
            {
                // Resetear el scroll value para que el juego vanilla no lo procese
                Terraria.GameInput.PlayerInput.ScrollWheelValue = Terraria.GameInput.PlayerInput.ScrollWheelValueOld;
            }
        }

        public override void PreUpdateMovement()
        {
            // Bloquear movimiento del jugador mientras la UI esta abierta
            var ui = ModContent.GetInstance<UISystem>();
            if (ui == null) return;

            bool anyUIOpen = (ui.SkillTreeUI?.IsVisible ?? false) || (ui.CodexUI?.IsVisible ?? false);
            if (anyUIOpen)
            {
                // Detener al jugador
                Player.velocity.X *= 0.8f;
                // No bloquear el salto para que el jugador pueda escapar si esta cayendo
            }
        }
    }
}
