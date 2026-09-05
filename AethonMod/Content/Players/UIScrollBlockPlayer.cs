using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.Players
{
    /// <summary>
    /// Bloquea el movimiento del jugador mientras una UI del mod esté abierta.
    /// Sistema simplificado: sin UIs que bloquear (BranchChoiceUI fue removido).
    /// </summary>
    public class UIScrollBlockPlayer : ModPlayer
    {
        public override void PreUpdate() { }
        public override void PreUpdateMovement() { }
    }
}
