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
        }

        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[projectile.owner];
            if (player != null)
                TrackDamageClass(player, projectile.DamageType);
        }

        /// <summary>
        /// Rastrea la clase de daño para el conteo de kills por rama.
        /// Cuando se alcanza KILLS_TO_IMPRINT, imprime el fragmento.
        /// </summary>
        private void TrackDamageClass(Player player, DamageClass damageClass)
        {
            var sp = player.GetModPlayer<Players.ShardPlayer>();
            if (sp == null || sp.IsImprinted) return;
            // Almacenar temporalmente la última clase para contar al morir.
            if (damageClass == DamageClass.Ranged)
                sp.DistanceKills++;
            else if (damageClass == DamageClass.Melee)
                sp.MeleeKills++;
            else if (damageClass == DamageClass.Magic || damageClass == DamageClass.Summon)
                sp.MagicKills++;
            // Verificar si alguna rama alcanzó el umbral.
            CheckImprint(sp);
        }

        private void CheckImprint(Players.ShardPlayer sp)
        {
            int threshold = Players.ShardPlayer.KILLS_TO_IMPRINT;
            if (sp.DistanceKills >= threshold)
            {
                sp.ActiveBranch = Players.BranchType.Distance;
                sp.SubForm = Players.WeaponSubForm.Bow;
            }
            else if (sp.MeleeKills >= threshold)
            {
                sp.ActiveBranch = Players.BranchType.Melee;
                sp.SubForm = Players.WeaponSubForm.Sword;
            }
            else if (sp.MagicKills >= threshold)
            {
                sp.ActiveBranch = Players.BranchType.Magic;
                sp.SubForm = Players.WeaponSubForm.Spellbook;
            }
            if (sp.IsImprinted)
            {
                // Generar seed del árbol procedural.
                if (sp.SkillTreeSeed == 0)
                    sp.SkillTreeSeed = Main.rand.Next(1, 1_000_000);
                // TODO: reemplazar el item del jugador por el arma correspondiente,
                // o transformar el Fragmento Génesis en el arma.
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
                    }
                }
            }
        }
    }
}
