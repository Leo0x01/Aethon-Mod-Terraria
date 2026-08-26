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
            // SOLO otorgar XP si el NPC fue matado por el jugador o sus invocaciones.
            // npc.SpawnedFromPlayer indica si el jugador interactuo con el.
            // Verificamos npc.playerInteraction que es true si el jugador o sus proyectiles dañaron al NPC.
            // Pero eso no distingue entre NPCs hostiles y NPCs amistosos.
            // Solucion: usar npc.lastInteraction que es el whoAmI del ultimo jugador que golpeo al NPC.

            // No contar NPCs amistosos (town NPCs, etc.)
            if (npc.friendly || npc.townNPC) return;

            // No contar NPCs que fueron matados por otros NPCs (no por el jugador).
            // npc.lastInteraction contiene el whoAmI del ultimo jugador que interactuo.
            // Si fue un NPC el que mato, lastInteraction sera -1 o un indice de NPC.
            // Aceptamos solo si un jugador real interactuo con el NPC.
            bool playerKilled = false;
            foreach (Player player in Main.ActivePlayers)
            {
                if (player.active && !player.dead)
                {
                    // Verificar si este jugador causo el golpe final o daño al NPC.
                    // Usar npc.playerInteraction que es true si el jugador interactuo con el NPC.
                    if (npc.playerInteraction[player.whoAmI])
                    {
                        playerKilled = true;
                        // Otorgar XP SOLO al jugador que mato al NPC.
                        int xp = Systems.ShardLevelSystem.XPForNPC(npc);
                        Systems.ShardLevelSystem.GrantXPToPlayer(player, xp);
                        Systems.NodeEffectSystem.OnKillNPC(player, npc);

                        // Tambien contar kills para la deteccion de rama.
                        var sp = player.GetModPlayer<Players.ShardPlayer>();
                        if (sp != null && !sp.IsImprinted)
                        {
                            // El tracking de kills ya se hace en OnHitByItem/OnHitByProjectile.
                        }
                        break; // Solo el primer jugador que interactuo.
                    }
                }
            }
        }

        // El reemplazo del item ahora lo hace la BranchCard en la UI de elección.
    }
}
