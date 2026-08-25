using System.IO;
using Terraria;
using Terraria.ModLoader;
using AethonMod.Content.Systems;

namespace AethonMod
{
    /// <summary>
    /// Punto de entrada del mod "Aethon, la Luz Primordial".
    /// Carga todos los sistemas, items, NPCs y UI del mod.
    /// </summary>
    public class AethonMod : Mod
    {
        public const string ModName = "AethonMod";
        public static AethonMod Instance => ModContent.GetInstance<AethonMod>();

        public override void Load()
        {
            // Los sistemas se cargan automáticamente por reflexión de tModLoader.
        }

        public override void Unload()
        {
            // Limpiar referencias estáticas para permitir hot-reload.
        }

        /// <summary>
        /// Procesa paquetes de red recibidos (sync multi-jugador).
        /// </summary>
        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            ShardSyncSystem.HandlePacket(reader);
        }

        /// <summary>
        /// Helper para obtener rutas de texturas de forma tipada.
        /// </summary>
        public static string TexturePath(string path)
        {
            return $"{ModName}/Content/{path}";
        }
    }
}
