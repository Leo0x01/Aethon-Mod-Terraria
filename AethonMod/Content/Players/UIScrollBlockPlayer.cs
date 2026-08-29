using Terraria;
using Terraria.ModLoader;
using AethonMod.Content.Systems;

namespace AethonMod.Content.Players
{
    /// <summary>
    /// ModPlayer que bloquea la interacción con el juego mientras las UIs del mod están abiertas.
    ///
    /// SOLUCIÓN CORRECTA: Usar Player.mouseInterface = true
    /// Esto le dice a Terraria que el mouse está sobre una interfaz de usuario,
    /// por lo que NO debe interpretar los clicks como input del juego (atacar, colocar bloques, etc.).
    /// Esto es EXACTAMENTE lo que hace el bestiario nativo de Terraria.
    /// </summary>
    public class UIScrollBlockPlayer : ModPlayer
    {
        public override void PreUpdate()
        {
            var ui = ModContent.GetInstance<UISystem>();
            if (ui == null) return;

            bool anyUIOpen = (ui.SkillTreeUI?.IsVisible ?? false) ||
                             (ui.CodexUI?.IsVisible ?? false) ||
                             (ui.BranchChoiceUI?.IsVisible ?? false);

            if (!anyUIOpen) return;

            // === CLAVE: Player.mouseInterface = true ===
            // Esto hace que Terraria NO interprete los clicks del mouse como input del juego.
            // El jugador no atacará, no colocará bloques, no usará items con el mouse.
            Player.mouseInterface = true;

            // Bloquear movimiento del jugador
            Player.controlLeft = false;
            Player.controlRight = false;
            Player.controlUp = false;
            Player.controlDown = false;
            Player.controlJump = false;
            Player.controlUseItem = false;
            Player.controlUseTile = false;
            Player.grappling[0] = -1;

            // Cerrar inventario si está abierto (para que no se vea la interfaz vanilla)
            if (Main.playerInventory)
                Main.playerInventory = false;
        }

        public override void PreUpdateMovement()
        {
            var ui = ModContent.GetInstance<UISystem>();
            if (ui == null) return;

            bool anyUIOpen = (ui.SkillTreeUI?.IsVisible ?? false) ||
                             (ui.CodexUI?.IsVisible ?? false) ||
                             (ui.BranchChoiceUI?.IsVisible ?? false);

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
