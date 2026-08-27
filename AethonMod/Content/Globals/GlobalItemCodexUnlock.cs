using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Players;

namespace AethonMod.Content.Globals
{
    /// <summary>
    /// GlobalItem que detecta cuando el jugador obtiene un arma mágica o de invocación.
    /// Cuando esto ocurre, activa el Codex de Memoria (lo desbloquea para esa rama).
    /// </summary>
    public class GlobalItemCodexUnlock : GlobalItem
    {
        public override bool OnPickup(Item item, Player player)
        {
            // Solo en el jugador local (no en servidor)
            if (Main.netMode == NetmodeID.Server) return true;
            if (item == null || item.IsAir) return true;
            if (player != Main.LocalPlayer) return true;

            var sp = player.GetModPlayer<ShardPlayer>();
            if (sp == null || !sp.IsImprinted) return true;

            // Solo la rama Magic activa el codex al conseguir armas mágicas/invocación
            if (sp.ActiveBranch != BranchType.Magic) return true;

            // Verificar si el item es un arma mágica o de invocación
            if (item.damage > 0 && (item.CountsAsClass(DamageClass.Magic) || item.CountsAsClass(DamageClass.Summon)))
            {
                if (!sp.CodexUnlocked)
                {
                    sp.CodexUnlocked = true;
                    Main.NewText($"✦ El Codex de Memoria ha despertado! Pulsa J para abrirlo.",
                        new Microsoft.Xna.Framework.Color(179, 136, 255));
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4);
                }
            }

            return true; // permitir el pickup
        }
    }
}
