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
            ApplyCosmicEmpowermentLifesteal(player, damageDone);
        }

        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            if (projectile.owner < 0 || projectile.owner >= Main.player.Length) return;
            Player? player = Main.player[projectile.owner];
            if (player != null && player.active)
            {
                ApplyAethonLifesteal(player, damageDone);
                ApplyCosmicEmpowermentLifesteal(player, damageDone);
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

        /// <summary>
        /// Aplica 1% de lifesteal si el jugador tiene el buff "Empoderamiento Cósmico"
        /// (conferido por el Sello de Aethon equipado), más 4% extra si tiene
        /// lifesteal mejorado (bastones de prueba).
        /// </summary>
        private void ApplyCosmicEmpowermentLifesteal(Player player, int damageDone)
        {
            if (damageDone <= 0) return;

            var sp = player.GetModPlayer<Players.ShardPlayer>();
            if (sp == null) return;

            float lifestealPercent = 0f;
            if (sp.HasCosmicEmpowerment) lifestealPercent += 0.01f; // 1%
            if (sp.HasEnhancedLifesteal) lifestealPercent += 0.04f; // +4% = 5% total

            if (lifestealPercent <= 0f) return;

            int healAmount = (int)(damageDone * lifestealPercent);
            if (healAmount > 0 && player.statLife < player.statLifeMax2)
            {
                player.HealEffect(healAmount, true);
                player.statLife += healAmount;
                if (player.statLife > player.statLifeMax2)
                    player.statLife = player.statLifeMax2;
            }
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

            // === DROP DE POLVO ESTELAR (StellarDust) ===
            // Jefes cósmicos del mod dropean StellarDust al morir.
            int stellarDropAmt = 0;
            if (npc.type == ModContent.NPCType<NPCs.AethonBoss>())
                stellarDropAmt = Main.rand.Next(10, 16); // 10-15 StellarDust
            else if (npc.type == ModContent.NPCType<NPCs.HollowTitan>())
                stellarDropAmt = Main.rand.Next(5, 9); // 5-8 StellarDust
            else if (npc.type == ModContent.NPCType<NPCs.RiftKeeper>())
                stellarDropAmt = Main.rand.Next(3, 6); // 3-5 StellarDust
            else if (npc.type == ModContent.NPCType<NPCs.EchoArcher>() ||
                     npc.type == ModContent.NPCType<NPCs.EchoBlade>())
            {
                // 25% de drop de 1-2 StellarDust en enemigos comunes cósmicos
                if (Main.rand.NextFloat() < 0.25f)
                    stellarDropAmt = Main.rand.Next(1, 3);
            }

            if (stellarDropAmt > 0)
            {
                int stellarDrop = Item.NewItem(npc.GetSource_Loot(), npc.Center,
                    ModContent.ItemType<Items.StellarDust>(), stellarDropAmt);
                if (stellarDrop >= 0 && stellarDrop < Main.item.Length)
                    Main.item[stellarDrop].noGrabDelay = 0;
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
