using System.IO;
using Terraria;
using Terraria.ModLoader;
using AethonMod.Content.Systems;

// En tModLoader, la clase Mod debe:
// 1. Heredar de Terraria.ModLoader.Mod
// 2. Estar en un namespace que coincida con el nombre de la carpeta del mod
// 3. tModLoader busca CUALQUIER clase que herede de Mod dentro del namespace raiz
//
// IMPORTANTE: Si la clase se llama igual que el namespace, C# la omite del DLL.
// Solucion: usar un namespace diferente para la clase, pero tModLoader
// requiere que el namespace raiz sea "AethonMod".
//
// La solucion correcta es: la clase debe estar en el namespace AethonMod,
// pero NO llamarse AethonMod. tModLoader busca cualquier subclase de Mod.

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
    }
}
