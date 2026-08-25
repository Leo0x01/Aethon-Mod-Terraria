using System.IO;
using Terraria;
using Terraria.ModLoader;
using AethonMod.Content.Systems;

namespace AethonMod
{
    // En tModLoader, la clase Mod debe estar en un namespace
    // que coincida con el nombre del mod (carpeta).
    // La clase NO debe tener el mismo nombre que el namespace.
    // tModLoader busca la clase que hereda de Mod dentro del namespace.

    /// <summary>
    /// Punto de entrada del mod "Aethon, la Luz Primordial".
    /// </summary>
    public class AethonModMod : Mod
    {
        public const string ModName = "AethonMod";
        public static AethonModMod Instance => ModContent.GetInstance<AethonModMod>();

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

        public static string TexturePath(string path)
        {
            return $"{ModName}/Content/{path}";
        }
    }
}
