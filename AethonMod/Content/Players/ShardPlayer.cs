using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using AethonMod.Content.Systems;

namespace AethonMod.Content.Players
{
    /// <summary>
    /// Estado persistente del jugador para el mod Aethon.
    /// Sistema SIMPLIFICADO: solo fragmento (shard) + selección de arma.
    /// El árbol de habilidades y el Codex de Memoria fueron eliminados.
    /// </summary>
    public class ShardPlayer : ModPlayer
    {
        public int ShardLevel = 1;
        public int ShardXP = 0;
        public BranchType ActiveBranch = BranchType.None;
        public WeaponSubForm SubForm = WeaponSubForm.None;
        public int DistanceKills = 0;
        public int MeleeKills = 0;
        public int MagicKills = 0;
        public const int KILLS_TO_IMPRINT = 20;
        public bool IsImprinted => ActiveBranch != BranchType.None;

        // Moneda conservada (otorgada por NPCs). Sin UI de gasto por ahora.
        public int ResonanceShards = 0;

        public int XPForNextLevel() => (int)(80 * System.Math.Pow(ShardLevel, 1.5));

        public void GrantXP(int amount)
        {
            if (!IsImprinted) return;
            ShardXP += amount;
            while (ShardXP >= XPForNextLevel())
            {
                ShardXP -= XPForNextLevel();
                ShardLevel++;
                OnLevelUp();
            }
        }

        private void OnLevelUp()
        {
            Main.NewText($"✦ Fragmento Genesis nivel {ShardLevel}!", new Microsoft.Xna.Framework.Color(245, 196, 81));
            Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Item4);
            for (int i = 0; i < 40; i++)
                Dust.NewDustPerfect(Player.Center, Terraria.ID.DustID.GoldFlame,
                    new Microsoft.Xna.Framework.Vector2(Main.rand.NextFloat(-6, 6), Main.rand.NextFloat(-6, 6)),
                    100, new Microsoft.Xna.Framework.Color(245, 196, 81), 1.5f);
            if (ShardLevel == 100)
            {
                Main.NewText("✦✦ Ascendencia desbloqueada! ✦✦", new Microsoft.Xna.Framework.Color(245, 196, 81));
                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.DD2_EtherianPortalOpen);
            }
        }

        public override void SaveData(TagCompound tag)
        {
            tag["shardLevel"] = ShardLevel;
            tag["shardXP"] = ShardXP;
            tag["activeBranch"] = (int)ActiveBranch;
            tag["subForm"] = (int)SubForm;
            tag["distanceKills"] = DistanceKills;
            tag["meleeKills"] = MeleeKills;
            tag["magicKills"] = MagicKills;
            tag["resonanceShards"] = ResonanceShards;
        }

        public override void LoadData(TagCompound tag)
        {
            // CRITICAL DEFENSIVE: LoadData must NEVER throw, or tModLoader marks the
            // whole player save as failed ("UnknownError") and the user loses their
            // character. Every read is guarded so legacy saves (with SkillTree/Codex
            // data we no longer care about) still load cleanly.
            try
            {
                ShardLevel = tag.GetInt("shardLevel");
                if (ShardLevel < 1) ShardLevel = 1;
                ShardXP = tag.GetInt("shardXP");
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

        public override void PostUpdateEquips() { }
        public override void ModifyHurt(ref Player.HurtModifiers modifiers) { }
        public override void OnHurt(Player.HurtInfo info) { }
    }
}
