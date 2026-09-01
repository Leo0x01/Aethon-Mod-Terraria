using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using AethonMod.Content.Systems;

namespace AethonMod.Content.Players
{
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

        // --- Nuevo sistema de árbol (sigue AnRPG) ---
        public int[] SkillNodeLevels;

        
        // --- Campos necesarios para el SkillTree de AnRPG ---
        public Content.SkillTree.RPGModule.SkillTree GetskillTree;
        public int GetSkillPoints => AvailableSkillPoints();
        public int GetLevel() => ShardLevel;
        public void ResetSkillTree()
        {
            if (GetskillTree != null)
            {
                GetskillTree = new Content.SkillTree.RPGModule.SkillTree();
                GetskillTree.Init();
            }
        }
        

        // --- Codex ---
        public List<string> MemorizedRunes = new();
        public bool CodexUnlocked = false;

        // --- Monedas ---
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

        public int CumulativeSkillPoints()
        {
            int total = 0;
            for (int lvl = 1; lvl <= ShardLevel; lvl++)
                total += lvl <= 10 ? 1 : lvl <= 20 ? 2 : lvl <= 30 ? 3 : lvl <= 40 ? 4 :
                         lvl <= 50 ? 5 : lvl <= 60 ? 6 : lvl <= 70 ? 7 : lvl <= 80 ? 8 :
                         lvl <= 90 ? 9 : lvl <= 100 ? 10 : 10;
            return total;
        }


        public int SpentSkillPoints() { return 0; }
        public int AvailableSkillPoints() => CumulativeSkillPoints() - SpentSkillPoints();

        public int RuneSlots()
        {
            if (ShardLevel < 50) return 0;
            if (ShardLevel < 75) return 1;
            if (ShardLevel < 100) return 2;
            if (ShardLevel < 125) return 3;
            if (ShardLevel < 150) return 4;
            return 5;
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
            tag["memorizedRunes"] = MemorizedRunes;
            tag["codexUnlocked"] = CodexUnlocked;
            // Guardar niveles de nodos
            {
                var levels = new List<int>();
                tag["skillNodeLevels"] = levels;
            }
        }

        public override void LoadData(TagCompound tag)
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
            MemorizedRunes = new List<string>(tag.GetList<string>("memorizedRunes"));
            CodexUnlocked = tag.GetBool("codexUnlocked");
            // Cargar niveles de nodos
            GetskillTree = new Content.SkillTree.RPGModule.SkillTree();
            GetskillTree.Init();
            
            var levels = tag.GetList<int>("skillNodeLevels");
        }

        public override void PostUpdateEquips()
        {
            // Aplicar efectos del árbol (nuevo sistema)
            // Los efectos del skill tree se aplican via GetskillTree (AnRPG pattern)
        }

        // Mana shield simplificado — sin NodeEffectSystem
        public override void ModifyHurt(ref Player.HurtModifiers modifiers) { }
        public override void OnHurt(Player.HurtInfo info) { }
    }
}
