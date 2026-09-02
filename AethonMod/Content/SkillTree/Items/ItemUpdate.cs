using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.SkillTree.Items
{
    public class ItemUpdate : GlobalItem
    {
        public override bool InstancePerEntity => true;
        public static bool NeedSavingStatic(Item item) { return false; }
        public static bool HaveTree(Item item) { return false; }
        public int Get_ItemType { get { return 0; } }
        public int GetWeaponType { get { return 0; } }
    }
}
