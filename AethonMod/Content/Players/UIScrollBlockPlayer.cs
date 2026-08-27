using Terraria;
using Terraria.ModLoader;
using AethonMod.Content.Systems;

namespace AethonMod.Content.Players
{
    /// <summary>
    /// ModPlayer que bloquea la interaccion con el juego mientras las UIs del mod estan abiertas.
    /// Se ejecuta ANTES de que el juego procese el input del jugador (PreUpdate).
    /// Esto replica el comportamiento del bestiario: cuando se abre, el juego se pausa
    /// y no se puede interactuar con el mundo.
    /// </summary>
    public class UIScrollBlockPlayer : ModPlayer
    {
        public override void PreUpdate()
        {
            var ui = ModContent.GetInstance<UISystem>();
            if (ui == null) return;

            bool anyUIOpen = (ui.SkillTreeUI?.IsVisible ?? false) || (ui.CodexUI?.IsVisible ?? false) || (ui.BranchChoiceUI?.IsVisible ?? false);
            if (anyUIOpen)
            {
                // === BLOQUEAR SCROLL DEL INVENTARIO ===
                Terraria.GameInput.PlayerInput.ScrollWheelValue = Terraria.GameInput.PlayerInput.ScrollWheelValueOld;

                // === BLOQUEAR INTERACCION CON EL MUNDO ===
                // Consumir los clicks del mouse para que el jugador no ataque/coloque bloques
                // mientras la UI esta abierta (como hace el bestiario).
                // NOTA: Los UIElements del mod procesan el input ANTES de este punto
                // (via UserInterface.Update en PostUpdateInput), asi que podemos
                // consumirlo aqui sin afectar la interaccion con la UI.
                Main.mouseLeft = false;
                Main.mouseRight = false;
                Main.mouseLeftRelease = false;
                Main.mouseRightRelease = false;

                // === BLOQUEAR INVENTARIO ===
                // Cerrar el inventario si esta abierto (como el bestiario lo hace)
                if (Main.playerInventory)
                {
                    Main.playerInventory = false;
                }

                // === BLOQUEAR USO DE ITEMS ===
                // El jugador no puede usar items mientras la UI esta abierta
                Player.itemTime = 0;
                Player.itemAnimation = 0;

                // === BLOQUEAR MOVIMIENTO ===
                // Frenar al jugador (no atacar, no moverse)
                Player.velocity.X *= 0.8f;
                // No bloquear el salto (para que no caiga en lava, etc.)
            }
        }

        public override void PreUpdateMovement()
        {
            var ui = ModContent.GetInstance<UISystem>();
            if (ui == null) return;

            bool anyUIOpen = (ui.SkillTreeUI?.IsVisible ?? false) || (ui.CodexUI?.IsVisible ?? false);
            if (anyUIOpen)
            {
                // Detener al jugador
                Player.velocity.X *= 0.8f;
            }
        }
    }
}
