using Terraria;
using Terraria.ModLoader;

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
            // Cargar assets, registrar UI, etc.
            // Los sistemas se cargan automáticamente por reflexión de tModLoader.
        }

        public override void Unload()
        {
            // Limpiar referencias estáticas para permitir hot-reload.
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
