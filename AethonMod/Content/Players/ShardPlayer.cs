using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using AethonMod.Content.Systems;
using AethonMod.Content.Globals;

namespace AethonMod.Content.Players
{
    /// <summary>
    /// Estado persistente del jugador para el mod Aethon.
    /// </summary>
    public class ShardPlayer : ModPlayer
    {
        public int ResonanceShards = 0;
        public bool FirstLevelUpTriggered = false;

        /// <summary>
        /// PostUpdateEquips: aplica bonuses que deben stackear con armadura.
        /// Slots de minion del Grimorio.
        /// </summary>
        public override void PostUpdateEquips()
        {
            Item held = Player.HeldItem;
            if (held != null && held.type == ModContent.ItemType<Weapons.GrimoireEternal>())
            {
                try
                {
                    var sl = held.GetGlobalItem<ShardLevelItem>();
                    if (sl != null)
                        Player.maxMinions += WeaponScaling.BonusMinionSlots(sl.Level);
                }
                catch { }
            }
        }

        public override void SaveData(TagCompound tag)
        {
            tag["resonanceShards"] = ResonanceShards;
            tag["firstLevelUpTriggered"] = FirstLevelUpTriggered;
        }

        public override void LoadData(TagCompound tag)
        {
            try
            {
                ResonanceShards = tag.GetInt("resonanceShards");
                FirstLevelUpTriggered = tag.GetBool("firstLevelUpTriggered");
            }
            catch { }
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers) { }
        public override void OnHurt(Player.HurtInfo info) { }
    }
}
