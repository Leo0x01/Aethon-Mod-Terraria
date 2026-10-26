// ============================================================================
// PUERTO DE INVESTIGACIÓN — Calamity: Hunt of the Old God
// (github.com/TohruKobayashi/CalamityHunt) · Team Hypnos.
// Licencia original: GPL-3.0 (ver LICENSE-GPL3.txt).
// ============================================================================
// EL HUB DEL PUERTO: el stand-in de la clase Mod de CalamityHunt (sus
// statics Instance/Particles/ParticlesBehindEntities). En el original el
// Mod mismo expone los dos renderizadores de partículas; aquí viven en
// este hub estático y los engancha GoozmaPortSystem.
using Terraria.ModLoader;
using AethonMod.Content.GoozmaPort.Common.Systems.Particles;

namespace AethonMod.Content.GoozmaPort
{
    public static class GoozmaPortHub
    {
        /// <summary>La instancia del mod anfitrión (AethonMod).</summary>
        public static Mod Instance => ModContent.GetInstance<global::AethonMod.AethonMod>();

        public static ParticleRenderer Particles { get; } = ParticleRenderer.MakeDefaultRenderer();

        public static ParticleRenderer ParticlesBehindEntities { get; } = ParticleRenderer.MakeDefaultRenderer();
    }
}
