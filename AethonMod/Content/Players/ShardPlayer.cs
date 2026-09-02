using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using AethonMod.Content.Systems;
using AethonMod.Content.Globals;

namespace AethonMod.Content.Players
{
    /// <summary>
    /// Estado persistente del jugador para el mod Aethon.
    /// Sistema: fragmento (shard) + selección de arma.
    ///
    /// NOTA: El nivel/XP del ARMA ya no vive aqui — se guarda por-item
    /// en ShardLevelItem (GlobalItem). ShardPlayer solo guarda la rama
    /// elegida y los contadores de kills para el imprinting.
    /// </summary>
    public class ShardPlayer : ModPlayer
    {
        public BranchType ActiveBranch = BranchType.None;
        public WeaponSubForm SubForm = WeaponSubForm.None;
        public int DistanceKills = 0;
        public int MeleeKills = 0;
        public int MagicKills = 0;
        public const int KILLS_TO_IMPRINT = 20;
        public bool IsImprinted => ActiveBranch != BranchType.None;

        // Moneda conservada (otorgada por NPCs).
        public int ResonanceShards = 0;

        /// <summary>
        /// Helper: devuelve el nivel del arma Aethon sostenida, o 0 si no hay ninguna.
        /// El nivel/XP vive por-item (ShardLevelItem), no en el jugador.
        /// </summary>
        public int HeldWeaponLevel
        {
            get
            {
                Item held = Player.HeldItem;
                if (held == null) return 0;
                var sl = held.GetGlobalItem<Globals.ShardLevelItem>();
                if (sl == null) return 0;
                return sl.Level;
            }
        }

        public override void SaveData(TagCompound tag)
        {
            tag["activeBranch"] = (int)ActiveBranch;
            tag["subForm"] = (int)SubForm;
            tag["distanceKills"] = DistanceKills;
            tag["meleeKills"] = MeleeKills;
            tag["magicKills"] = MagicKills;
            tag["resonanceShards"] = ResonanceShards;
        }

        public override void LoadData(TagCompound tag)
        {
            // CRITICAL DEFENSIVE: LoadData must NEVER throw.
            try
            {
                ActiveBranch = (BranchType)tag.GetInt("activeBranch");
                SubForm = (WeaponSubForm)tag.GetInt("subForm");
                DistanceKills = tag.GetInt("distanceKills");
                MeleeKills = tag.GetInt("meleeKills");
                MagicKills = tag.GetInt("magicKills");
                ResonanceShards = tag.GetInt("resonanceShards");
            }
            catch
            {
                // Defensive: legacy / partial saves keep loading with defaults.
            }
        }

        /// <summary>
        /// PostUpdateEquips: aplica bonuses que deben stackear con armadura.
        /// Los slots de minion del Grimorio se aplican aqui, usando el nivel
        /// del item Grimorio sostenido (no el nivel del jugador).
        /// </summary>
        public override void PostUpdateEquips()
        {
            if (IsImprinted && ActiveBranch == BranchType.Magic)
            {
                Item held = Player.HeldItem;
                if (held != null && held.type == ModContent.ItemType<Weapons.GrimoireEternal>())
                {
                    var sl = held.GetGlobalItem<ShardLevelItem>();
                    if (sl != null)
                    {
                        Player.maxMinions += WeaponScaling.BonusMinionSlots(sl.Level);
                    }
                }
            }
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers) { }
        public override void OnHurt(Player.HurtInfo info) { }
    }
}
