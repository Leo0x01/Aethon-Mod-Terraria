using System.IO;
using Terraria;
using Terraria.ModLoader;
using AethonMod.Content.Systems;

namespace AethonMod
{
    public class AethonMod : Mod
    {
        public static AethonMod Instance => ModContent.GetInstance<AethonMod>();

        public override void Load()
        {
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
