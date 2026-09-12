using System.IO;

using Terraria.ModLoader;
using AethonMod.Content.Systems;

namespace AethonMod
{
    public class AethonMod : Mod
    {
        public static AethonMod Instance => ModContent.GetInstance<AethonMod>();

        public override void Load()
        {
            // Sistema simplificado: el SkillTree y el Codex fueron eliminados.
            // No hay inicializacion extra necesaria.
        }

        public override void Unload()
        {
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            ShardSyncSystem.HandlePacket(reader);
        }
    }
}
