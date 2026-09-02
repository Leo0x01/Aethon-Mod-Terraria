using System.IO;
using Terraria;
using Terraria.ModLoader;
using AethonMod.Content.Systems;
using AethonMod.Content.SkillTree;

namespace AethonMod
{
    public class AethonMod : Mod
    {
        public static AethonMod Instance => ModContent.GetInstance<AethonMod>();

        public override void Load()
        {
            // The AnRPG skill tree code was ported over and relies on two JSON-backed
            // registries (nodes + classes). They MUST be initialized before any
            // ModPlayer constructs a SkillTree, otherwise GetJsonNodeList returns
            // null and the SkillTree ctor NPEs on player load.
            // Init() is safe: it falls back to the hardcoded default lists (the
            // deserialization branch is gated by `if (false)` in the ported code)
            // and is fully wrapped in try/catch internally.
            JsonSkillTree.Init();
            JsonCharacterClass.Init();
        }

        public override void Unload()
        {
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            ShardSyncSystem.HandlePacket(reader);
        }

        // Dibujo y update de UI se manejan en UISystem (ModSystem)
        // que tiene los hooks correctos: PostUpdateInput y ModifyInterfaceLayers
    }
}
