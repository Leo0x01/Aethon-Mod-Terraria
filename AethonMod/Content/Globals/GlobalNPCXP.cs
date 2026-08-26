using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Globals
{
    /// <summary>
    /// GlobalNPC que otorga XP al fragmento del jugador cuando mata un NPC.
    /// También rastrea el tipo de daño para detectar la rama a imprimir.
    /// </summary>
    public class GlobalNPCXP : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        /// <summary>Tipo de daño del último golpe recibido (para tracking de rama).</summary>
        public int LastDamageClass = 0;

        public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            TrackDamageClass(player, item.DamageType);
            // Lifesteal: regeneración de salud por daño causado (Keystone de Ascendancy).
            Systems.NodeEffectSystem.OnHitNPC(player, npc, damageDone, false);
        }

        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[projectile.owner];
            if (player != null)
            {
                TrackDamageClass(player, projectile.DamageType);
                // Lifesteal para proyectiles (determina si es de invocación).
                Systems.NodeEffectSystem.OnProjectileHitNPC(player, npc, damageDone, projectile);
            }
        }

        /// <summary>
        /// Rastrea la clase de daño para el conteo de kills por rama.
        /// Cuando se alcanza KILLS_TO_IMPRINT, imprime el fragmento.
        /// </summary>
        private void TrackDamageClass(Player player, DamageClass damageClass)
        {
            var sp = player.GetModPlayer<Players.ShardPlayer>();
            if (sp == null || sp.IsImprinted) return;
            // Contar kills por tipo de daño para tracking.
            if (damageClass == DamageClass.Ranged)
                sp.DistanceKills++;
            else if (damageClass == DamageClass.Melee)
                sp.MeleeKills++;
            else if (damageClass == DamageClass.Magic || damageClass == DamageClass.Summon)
                sp.MagicKills++;
            // Verificar si el total de kills alcanza el umbral para mostrar la elección.
            CheckImprintReady(player, sp);
        }

        private void CheckImprintReady(Player player, Players.ShardPlayer sp)
        {
            int totalKills = sp.DistanceKills + sp.MeleeKills + sp.MagicKills;
            int threshold = Players.ShardPlayer.KILLS_TO_IMPRINT;
            // Cuando el total de kills alcanza el umbral, mostrar las tarjetas de elección.
            // NO elegir automaticamente — el jugador decide.
            if (totalKills >= threshold && !sp.IsImprinted)
            {
                // Mostrar la UI de elección de rama.
                var ui = ModContent.GetInstance<Content.Systems.UISystem>();
                if (ui != null && ui.BranchChoiceUI != null && !ui.BranchChoiceUI.IsVisible)
                {
                    ui.BranchChoiceUI.Show();
                    Main.NewText("Tu Fragmento Genesis esta listo. Elige tu rama!", new Microsoft.Xna.Framework.Color(245, 196, 81));
                    Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Item4);
                }
            }
        }

        public override void OnKill(NPC npc)
        {
            // Otorgar XP a cada jugador que haya dañado al NPC.
            // Simplificación: otorga al Main.LocalPlayer (en single-player es suficiente).
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                GrantXPToPlayers(npc);
            }
            else
            {
                // En multi-jugador, cada cliente procesa su propio daño.
                GrantXPToPlayers(npc);
            }
        }

        private void GrantXPToPlayers(NPC npc)
        {
            int xp = Systems.ShardLevelSystem.XPForNPC(npc);
            foreach (Player player in Main.ActivePlayers)
            {
                // Verificar si el jugador dañó a este NPC (npc.playerInteraction).
                // Simplificación: otorga a todos los jugadores activos cercanos.
                if (player.active && !player.dead)
                {
                    float dist = System.Math.Abs(player.Center.X - npc.Center.X) +
                                 System.Math.Abs(player.Center.Y - npc.Center.Y);
                    if (dist < 3000f) // ~150 tiles
                    {
                        Systems.ShardLevelSystem.GrantXPToPlayer(player, xp);
                        // Aplicar efectos de nodos al matar (lifesteal, reset, explosión).
                        Systems.NodeEffectSystem.OnKillNPC(player, npc);
                    }
                }
            }
        }

        // El reemplazo del item ahora lo hace la BranchCard en la UI de elección.
    }
}
