using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using AethonMod.Content.SkillTree.RPGModule;

namespace AethonMod.Content.SkillTree.Entities
{
    public class RPGPlayer : ModPlayer
    {
        public static RPGPlayer Instance;

        private global::AethonMod.Content.SkillTree.RPGModule.SkillTree skilltree;

        public global::AethonMod.Content.SkillTree.RPGModule.SkillTree GetskillTree
        {
            get { return skilltree; }
        }

        public int GetSkillPoints => 0;
        public int GetLevel() => 1;

        public void ResetSkillTree()
        {
            skilltree = new global::AethonMod.Content.SkillTree.RPGModule.SkillTree();
            skilltree.Init();
        }

        public bool HaveBow() { return false; }
        public bool HaveRangedWeapon() { return false; }
        public void SpentSkillPoints(int cost) { }
        public int GetStat(Stat stat) { return 0; }

        public override void LoadData(TagCompound tag)
        {
            skilltree = new global::AethonMod.Content.SkillTree.RPGModule.SkillTree();
            skilltree.Init();
        }
    }
}
