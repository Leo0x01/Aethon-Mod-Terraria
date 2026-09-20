using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Players;
using AethonMod.Content.Systems;

namespace AethonMod.Content.Globals
{
    /// <summary>
    /// GlobalNPC que:
    /// - v6.45: otorga XP REAL (rareza del bestiario) a TODO Grimorio del
    ///   inventario del jugador que mató — no solo al sostenido, y también
    ///   por las kills de sus propios minions (el minion acredita al dueño).
    /// - Aplica lifesteal si el Grimorio sostenido tiene nivel >= 7.
    /// - Hace que King Slime y Eye of Cthulhu dropeen el Fragmento Génesis.
    /// </summary>
    public class GlobalNPCXP : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            // v6.45: garantiza el crédito de la kill para FindKiller
            // (playerInteraction) aunque el motor no lo hubiera marcado.
            if (player != null && player.whoAmI >= 0 && player.whoAmI < Main.player.Length)
                npc.playerInteraction[player.whoAmI] = true;
            ApplyAethonLifesteal(player, damageDone);
        }

        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            if (projectile.owner < 0 || projectile.owner >= Main.player.Length) return;
            Player player = Main.player[projectile.owner];
            if (player != null && player.active)
            {
                // v6.45: LAS KILLS DE LOS MINIONS ACREDITAN AL DUEÑO. El Orbe
                // Cósmico del Grimorio (y cualquier proyectil del jugador)
                // deja marcado playerInteraction: FindKiller encuentra al
                // dueño aunque la kill la dé el minion, y el Grimorio en su
                // inventario cobra la XP. Idempotente: si el motor ya lo
                // marcó, esto no cambia nada.
                npc.playerInteraction[projectile.owner] = true;
                ApplyAethonLifesteal(player, damageDone);
            }
        }

        private void ApplyAethonLifesteal(Player player, int damageDone)
        {
            Item held = player.HeldItem;
            if (held == null || held.type != ModContent.ItemType<Weapons.GrimoireEternal>()) return;

            var sl = held.GetGlobalItem<ShardLevelItem>();
            if (sl == null) return;
            if (!WeaponScaling.HasLifesteal(sl.Level)) return;

            WeaponScaling.ApplyLifesteal(player, damageDone, sl.Level);
        }

        public override void OnKill(NPC npc)
        {
            if (npc.friendly || npc.townNPC) return;

            // === DROP DEL FRAGMENTO GÉNESIS ===
            if (npc.type == NPCID.KingSlime || npc.type == NPCID.EyeofCthulhu)
            {
                Player killer = FindKiller(npc);
                if (killer != null)
                {
                    bool hasGrimoire = false;
                    for (int i = 0; i < 58; i++)
                    {
                        if (killer.inventory[i] != null &&
                            killer.inventory[i].type == ModContent.ItemType<Weapons.GrimoireEternal>())
                        {
                            hasGrimoire = true;
                            break;
                        }
                    }
                    if (!hasGrimoire)
                    {
                        int drop = Item.NewItem(npc.GetSource_Loot(), npc.Center,
                            ModContent.ItemType<Items.GenesisShard>(), 1);
                        if (drop >= 0 && drop < Main.item.Length)
                            Main.item[drop].noGrabDelay = 0;
                    }
                }
            }

            // === v6.45: OTORGAR XP REAL A TODO GRIMORIO DEL INVENTARIO ===
            // El arma gana XP SIEMPRE QUE ESTÉ EN EL INVENTARIO (no solo al
            // sostenerla: matar con otra arma también la alimenta) y las
            // kills de sus propios minions pagan igual (el minion acredita
            // al dueño vía playerInteraction). TODAS las copias del Grimorio
            // en el inventario cobran — cada una sube su propio nivel.
            Player player = FindKiller(npc);
            if (player == null) return;

            try
            {
                int baseXP = ShardLevelSystem.XPForNPC(npc);
                int xp = ShardLevelSystem.ApplyXPMultiplier(baseXP);
                if (xp > 0)
                {
                    for (int i = 0; i < 58; i++)
                    {
                        Item inv = player.inventory[i];
                        if (inv == null || inv.type != ModContent.ItemType<Weapons.GrimoireEternal>())
                            continue;
                        var sl = inv.GetGlobalItem<ShardLevelItem>();
                        if (sl != null)
                            sl.GrantXP(inv, xp);
                    }
                }
            }
            catch { }
        }

        private Player FindKiller(NPC npc)
        {
            int killerWho = -1;
            if (npc.lastInteraction >= 0 && npc.lastInteraction < Main.player.Length)
            {
                Player last = Main.player[npc.lastInteraction];
                if (last != null && last.active && !last.dead)
                    killerWho = npc.lastInteraction;
            }
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
            if (killerWho == -1) return null;
            return Main.player[killerWho];
        }
    }
}
