
using AethonMod.Content.SkillTree.RPGModule;
using AethonMod.Content.SkillTree.Items;

namespace AethonMod.Content.SkillTree.Utils
{

    public enum DamageNameTree : byte
    {
        Melee,
        Ranged,
        Throw,
        Magic,
        Summon,
        Bow, //thorium
        Gun //thorium

    }

    public class SkillTextures
    {

        static public string GetItemTexture(ItemNode node)
        {
            // STUB: original AnRPG item-tree textures are not bundled in this mod.
            // Reuse the existing Node_Small asset so requests never throw.
            string path = "AethonMod/Content/UI/Textures/Node_Small";
            return path;
        }

        static public string GetTexture(Node node)
        {
            // STUB: original AnRPG per-node-type textures are not bundled in this mod.
            // Map node types to the Node_* assets that DO ship with AethonMod so
            // ModContent.Request never throws an AssetLoadException.
            string path;
            switch (node.GetNodeType)
            {
                case NodeType.Class:
                case NodeType.LimitBreak:
                    path = "AethonMod/Content/UI/Textures/Node_Ascendancy";
                    break;
                case NodeType.Stats:
                    path = "AethonMod/Content/UI/Textures/Node_Notable";
                    break;
                default:
                    path = "AethonMod/Content/UI/Textures/Node_Small";
                    break;
            }
            return path;
        }
    }

    
}
