using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Players;
using AethonMod.Content.Systems;

namespace AethonMod.Content.Globals
{
    /// <summary>
    /// GlobalNPC que:
    /// - Otorga XP al Grimorio sostenido cuando mata un NPC.
    /// - Aplica lifesteal si el Grimorio tiene nivel >= 7.
    /// - Hace que King Slime y Eye of Cthulhu dropeen el Fragmento Génesis.
    /// </summary>
    public class GlobalNPCXP : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            ApplyAethonLifesteal(player, damageDone);
        }

        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            if (projectile.owner < 0 || projectile.owner >= Main.player.Length) return;
            Player? player = Main.player[projectile.owner];
            if (player != null && player.active)
                ApplyAethonLifesteal(player, damageDone);
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
                Player? killer = FindKiller(npc);
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

            // === OTORGAR XP AL GRIMORIO SOSTENIDO ===
            Player? player = FindKiller(npc);
            if (player == null) return;

            try
            {
                int baseXP = ShardLevelSystem.XPForNPC(npc);
                int xp = ShardLevelSystem.ApplyXPMultiplier(baseXP);
                Item heldItem = player.HeldItem;
                if (heldItem != null && xp > 0 &&
                    heldItem.type == ModContent.ItemType<Weapons.GrimoireEternal>())
                {
                    var sl = heldItem.GetGlobalItem<ShardLevelItem>();
                    if (sl != null)
                        sl.GrantXP(heldItem, xp);
                }
            }
            catch { }
        }

        private Player? FindKiller(NPC npc)
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
