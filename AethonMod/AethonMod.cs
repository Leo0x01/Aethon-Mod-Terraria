using System.IO;

using Terraria.ModLoader;
using AethonMod.Content.Systems;
using AethonMod.Content.VFX;

namespace AethonMod
{
    public class AethonMod : Mod
    {
        public static AethonMod Instance => ModContent.GetInstance<AethonMod>();

        public override void Load()
        {
            // Sistema simplificado: el SkillTree y el Codex fueron eliminados.
            // No hay inicializacion extra necesaria.

            // v6.43 — CIELOLIB: EL REGISTRO DE LAS TEXTURAS DE FONDO DEL
            // SAGRARIO (los slots de fondo de esta versión se dan de alta
            // aquí — el estilo de bioma las pide por su ruta relativa).
            BackgroundTextureLoader.AddBackgroundTexture(this, SanctumBackgroundStyle.RutaFar);
            BackgroundTextureLoader.AddBackgroundTexture(this, SanctumBackgroundStyle.RutaMiddle);
            BackgroundTextureLoader.AddBackgroundTexture(this, SanctumBackgroundStyle.RutaClose);
        }

        public override void Unload()
        {
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            ShardSyncSystem.HandlePacket(reader, whoAmI);
        }
    }
}
