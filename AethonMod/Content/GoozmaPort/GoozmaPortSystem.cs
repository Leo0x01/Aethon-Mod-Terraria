// ============================================================================
// PUERTO DE INVESTIGACIÓN — Calamity: Hunt of the Old God
// (github.com/TohruKobayashi/CalamityHunt) · Team Hypnos.
// Licencia original: GPL-3.0 (ver LICENSE-GPL3.txt).
// ============================================================================
// EL SISTEMA DEL PUERTO: el Load() esencial de la clase Mod original
// (CalamityHunt.cs) — los TRES enganches del sistema de partículas y los
// TRES filtros de escena + el cielo de la monzón de slimes. Añadido el
// Unload() que el original no necesita (su Mod vive todo el proceso) pero
// un SUBSISTEMA recargable sí: los hooks se desenganchan al descargar.
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.GoozmaPort.Common.Graphics.Skies;

namespace AethonMod.Content.GoozmaPort
{
    public class GoozmaPortSystem : ModSystem
    {
        private On_Main.hook_UpdateParticleSystems _hkUpdate;
        private On_Main.hook_DrawDust _hkDrawDust;
        private On_Main.hook_DoDraw_DrawNPCsOverTiles _hkDrawBehind;

        public override void Load()
        {
            if (Main.dedServ)
                return;

            _hkUpdate = UpdateParticleSystems;
            _hkDrawDust = DrawParticleSystems;
            _hkDrawBehind = DrawParticleSystemBehindEntities;
            On_Main.UpdateParticleSystems += _hkUpdate;
            On_Main.DrawDust += _hkDrawDust;
            On_Main.DoDraw_DrawNPCsOverTiles += _hkDrawBehind;

            Ref<Effect> stellarblackhole = new Ref<Effect>(AssetDirectory.Effects.BlackHole.Value);
            Filters.Scene["HuntOfTheOldGods:StellarBlackHole"] = new Filter(new ScreenShaderData(stellarblackhole, "BlackHolePass"), EffectPriority.VeryHigh);
            Filters.Scene["HuntOfTheOldGods:StellarBlackHole"].Load();

            Ref<Effect> pluripotentSpawnDistort = new Ref<Effect>(AssetDirectory.Effects.PluripotentDistortion.Value);
            Filters.Scene["HuntOfTheOldGods:PluripotentSpawn"] = new Filter(new ScreenShaderData(pluripotentSpawnDistort, "DistortionPass"), EffectPriority.Medium);
            Filters.Scene["HuntOfTheOldGods:PluripotentSpawn"].Load();

            Ref<Effect> slimeMonsoonDistort = new Ref<Effect>(AssetDirectory.Effects.SlimeMonsoonDistortion.Value);
            Filters.Scene["HuntOfTheOldGods:SlimeMonsoon"] = new Filter(new ScreenShaderData(slimeMonsoonDistort, "DistortionPass"), EffectPriority.Medium);
            Filters.Scene["HuntOfTheOldGods:SlimeMonsoon"].Load();
            SkyManager.Instance["HuntOfTheOldGods:SlimeMonsoon"] = new SlimeMonsoonSky();
            SkyManager.Instance["HuntOfTheOldGods:SlimeMonsoon"].Load();
        }

        public override void Unload()
        {
            if (_hkUpdate != null) On_Main.UpdateParticleSystems -= _hkUpdate;
            if (_hkDrawDust != null) On_Main.DrawDust -= _hkDrawDust;
            if (_hkDrawBehind != null) On_Main.DoDraw_DrawNPCsOverTiles -= _hkDrawBehind;
            _hkUpdate = null; _hkDrawDust = null; _hkDrawBehind = null;
        }

        private void UpdateParticleSystems(On_Main.orig_UpdateParticleSystems orig, Main self)
        {
            orig(self);
            GoozmaPortHub.ParticlesBehindEntities.Update();
            GoozmaPortHub.Particles.Update();
        }

        private void DrawParticleSystems(On_Main.orig_DrawDust orig, Main self)
        {
            orig(self);
            GoozmaPortHub.Particles.Draw(Main.spriteBatch);
        }

        private void DrawParticleSystemBehindEntities(On_Main.orig_DoDraw_DrawNPCsOverTiles orig, Main self)
        {
            GoozmaPortHub.ParticlesBehindEntities.Draw(Main.spriteBatch);
            orig(self);
        }
    }
}
