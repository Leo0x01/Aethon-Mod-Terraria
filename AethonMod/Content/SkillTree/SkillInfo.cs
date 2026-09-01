using AethonMod.Content.SkillTree.RPGModule;

namespace AethonMod.Content.SkillTree.Utils
{
    public class SkillInfo
    {
        public static string GetDesc(Node node)
        {
            switch (node.GetNodeType)
            {
                case NodeType.Damage:
                    return "Bonus " + (node as DamageNode).GetDamageType + " Damage";
                case NodeType.Speed:
                    return "Bonus " + (node as SpeedNode).GetDamageType + " Speed";
                case NodeType.Leech:
                    return "Bonus " + (node as LeechNode).GetLeechType + " Leech";
                case NodeType.Stats:
                    return "Bonus " + (node as StatNode).GetStatType + " Stats";
                case NodeType.Perk:
                    return "Perk: " + (node as PerkNode).GetPerk;
                case NodeType.Class:
                    return "Class: " + (node as ClassNode).GetClassType;
                case NodeType.LimitBreak:
                    return "LIMIT BREAK";
            }
            return "";
        }
    }
}
