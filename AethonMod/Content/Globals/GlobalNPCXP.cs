using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Players;
using AethonMod.Content.Systems;

namespace AethonMod.Content.Globals
{
    /// <summary>
    /// GlobalNPC que otorga XP al fragmento del jugador cuando mata un NPC.
    /// Tambien aplica LIFESTEAL (curacion por daño causado) si el jugador
    /// tiene un arma Aethon con nivel de fragmento >= 7.
    /// </summary>
    public class GlobalNPCXP : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        /// <summary>Tipo de dano del ultimo golpe recibido (para tracking de rama).</summary>
        public int LastDamageClass = 0;

        public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            ApplyAethonLifesteal(player, damageDone);
        }

        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            // Bounds check: projectile.owner puede ser invalido (orphan projectiles).
            if (projectile.owner < 0 || projectile.owner >= Main.player.Length) return;
            Player? player = Main.player[projectile.owner];
            if (player != null && player.active)
            {
                ApplyAethonLifesteal(player, damageDone);
            }
        }

        /// <summary>
        /// Aplica lifesteal al jugador si tiene un arma Aethon con nivel >= 7.
        /// El nivel se lee del item sostenido (ShardLevelItem), no del jugador.
        /// </summary>
        private void ApplyAethonLifesteal(Player player, int damageDone)
        {
            var sp = player.GetModPlayer<ShardPlayer>();
            if (sp == null || !sp.IsImprinted) return;

            // Solo aplicar si el jugador sostiene una de las 3 armas Aethon.
            Item held = player.HeldItem;
            if (held == null) return;
            bool isAethonWeapon =
                held.type == ModContent.ItemType<Weapons.SolbrandEdge>() ||
                held.type == ModContent.ItemType<Weapons.LuminaStarbow>() ||
                held.type == ModContent.ItemType<Weapons.GrimoireEternal>();
            if (!isAethonWeapon) return;

            // Leer el nivel del item sostenido
            var sl = held.GetGlobalItem<ShardLevelItem>();
            if (sl == null) return;
            if (!WeaponScaling.HasLifesteal(sl.Level)) return;

            WeaponScaling.ApplyLifesteal(player, damageDone, sl.Level);
        }

        public override void OnKill(NPC npc)
        {
            // No contar NPCs amistosos (town NPCs, etc.)
            if (npc.friendly || npc.townNPC) return;

            // Buscar al jugador que mato al NPC (usando npc.lastInteraction como indicador primario).
            // En SP, npc.lastInteraction contiene el whoAmI del ultimo jugador que golpeo al NPC.
            // En MP, solo el servidor deberia ejecutar OnKill logic; pero para SP y splitscreen esto funciona.
            int killerWho = -1;
            // Priorizar npc.lastInteraction (jugador que dio el golpe final).
            if (npc.lastInteraction >= 0 && npc.lastInteraction < Main.player.Length)
            {
                Player last = Main.player[npc.lastInteraction];
                if (last != null && last.active && !last.dead)
                    killerWho = npc.lastInteraction;
            }
            // Fallback: buscar el primer jugador que interactuo con el NPC.
            if (killerWho == -1)
            {
                for (int i = 0; i < Main.player.Length; i++)
                {
                    Player p = Main.player[i];
                    if (p != null && p.active && !p.dead && npc.playerInteraction[i])
                    {
                        killerWho = i;
                        break;
                    }
                }
            }

            if (killerWho == -1) return;
            Player player = Main.player[killerWho];
            if (player == null) return;

            // === OTORGAR XP AL ARMA SOSTENIDA (no al jugador) ===
            // El nivel/XP es por-item individual, no compartido entre armas.
            int xp = Systems.ShardLevelSystem.XPForNPC(npc);
            Item heldItem = player.HeldItem;
            if (heldItem != null)
            {
                var slItem = heldItem.GetGlobalItem<ShardLevelItem>();
                if (slItem != null)
                {
                    // Solo otorgar XP si el item es un arma Aethon (AppliesToEntity lo filtra).
                    slItem.GrantXP(xp);
                }
            }

            // Tracking de kills por rama (solo si el jugador no ha elegido rama aun).
            var sp = player.GetModPlayer<Players.ShardPlayer>();
            if (sp != null && !sp.IsImprinted)
            {
                TrackDamageClass(player, npc);
            }
        }

        /// <summary>
        /// Rastrea la clase de dano del golpe que mato al NPC para el conteo de kills por rama.
        /// Cuando se alcanza KILLS_TO_IMPRINT, imprime el fragmento (muestra la UI de eleccion).
        /// </summary>
        private void TrackDamageClass(Player player, NPC npc)
        {
            var sp = player.GetModPlayer<Players.ShardPlayer>();
            if (sp == null || sp.IsImprinted) return;

            // Usar la clase de dano del ultimo golpe recibido (LastDamageClass).
            // Esta se establece en OnHitByItem/OnHitByProjectile via el DamageType.
            // Como no tenemos acceso directo aqui, inferimos del arma equipada del jugador.
            // Solucion simple: usar el DamageType del arma sostenida actualmente.
            Item weapon = player.HeldItem;
            if (weapon == null) return;
            var damageClass = weapon.DamageType;

            // Usar CountsAsClass para detectar hibridos correctamente.
            if (damageClass.CountsAsClass(DamageClass.Ranged))
                sp.DistanceKills++;
            else if (damageClass.CountsAsClass(DamageClass.Melee))
                sp.MeleeKills++;
            else if (damageClass.CountsAsClass(DamageClass.Magic) || damageClass.CountsAsClass(DamageClass.Summon))
                sp.MagicKills++;

            CheckImprintReady(player, sp);
        }

        private void CheckImprintReady(Player player, Players.ShardPlayer sp)
        {
            // Solo procesar para el jugador local (evita abrir UI en otros clientes en MP)
            if (player.whoAmI != Main.myPlayer) return;

            int totalKills = sp.DistanceKills + sp.MeleeKills + sp.MagicKills;
            int threshold = Players.ShardPlayer.KILLS_TO_IMPRINT;
            if (totalKills >= threshold && !sp.IsImprinted)
            {
                var ui = ModContent.GetInstance<Content.Systems.UISystem>();
                if (ui != null && ui.BranchChoiceUI != null && !ui.BranchChoiceUI.IsVisible)
                {
                    ui.BranchChoiceUI.Show();
                    Main.NewText("Tu Fragmento Genesis esta listo. Elige tu rama!", new Microsoft.Xna.Framework.Color(245, 196, 81));
                    Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Item4);
                }
            }
        }
    }
}
