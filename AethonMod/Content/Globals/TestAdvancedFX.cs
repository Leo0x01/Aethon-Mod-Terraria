using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Globals
{
    /// <summary>
    /// TestAdvancedFX v5.44
    ///
    /// Flags que funcionan (mantenidos):
    /// 3003 = TestMagicRing
    /// 3004 = TestSparkle
    /// 4001 = ProjBeam
    /// 4006 = TestMagicRingV2
    ///
    /// Nuevos flags de color (re-tintan el sprite del Nightglow):
    /// 5001 = ColorGold (dorado)
    /// 5002 = ColorCyan (cian)
    /// 5003 = ColorMagenta (magenta)
    /// 5004 = ColorRainbow (arcoíris hue shift)
    ///
    /// Técnica: PreDraw dibuja el sprite original con Color.Lerp
    /// hacia el color del tinte, luego dibuja un glow circle
    /// del mismo color con additive blending.
    /// </summary>
    public class TestAdvancedFX : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public override bool AppliesToEntity(Projectile projectile, bool lateInstantiation)
        { return projectile.type == 931; }

        public override void AI(Projectile projectile)
        {
            // Sparkle stars (3004)
            if (projectile.ai[1] == 3004 && Main.rand.NextBool(4))
            {
                Dust d = Dust.NewDustPerfect(projectile.Center, DustID.Enchanted_Gold,
                    new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), Main.rand.NextFloat(-1.5f, 1.5f)),
                    200, new Color(255, 255, 255), 0.8f);
                d.noGravity = true; d.fadeIn = 1.5f;
            }

            // MagicRingV2 (4006) — sparkles en anillo
            if (projectile.ai[1] == 4006 && Main.rand.NextBool(6))
            {
                float angle = Main.rand.NextFloat(0, System.MathF.PI * 2);
                float dist = 35f;
                Vector2 pos = projectile.Center + new Vector2(
                    (float)System.Math.Cos(angle) * dist, (float)System.Math.Sin(angle) * dist);
                Dust d = Dust.NewDustPerfect(pos, DustID.Enchanted_Gold, Vector2.Zero, 200, new Color(255, 255, 255), 0.5f);
                d.noGravity = true; d.fadeIn = 0f;
            }
        }

        public override bool PreDraw(Projectile projectile, ref Color lightColor)
        {
            float t = Main.GameUpdateCount;

            // === TEST MAGIC RING (3003) ===
            if (projectile.ai[1] == 3003)
            {
                DrawTex("AethonMod/Content/Effects/MagicRing", projectile.Center, 0.6f, new Color(0, 255, 255, 180), t * 0.05f);
                DrawTex("AethonMod/Content/Effects/MagicRingGold", projectile.Center, 0.8f, new Color(255, 217, 61, 100), -t * 0.035f);
                DrawTex("AethonMod/Content/Effects/GlowCircleGold", projectile.Center, 0.5f, new Color(255, 217, 61, 120), 0f);
            }

            // === TEST SPARKLE (3004) ===
            if (projectile.ai[1] == 3004)
            {
                DrawTex("AethonMod/Content/Effects/GlowCircleGold", projectile.Center, 0.7f, new Color(255, 217, 61, 130), 0f);
            }

            // === PROJ BEAM (4001) ===
            if (projectile.ai[1] == 4001)
            {
                float pulse = 0.8f + 0.2f * (float)System.Math.Sin(t * 0.15f);
                DrawTex("AethonMod/Content/Effects/GlowCircleWhite", projectile.Center, 1.2f * pulse, new Color(255, 255, 255, 180), 0f);
                DrawTex("AethonMod/Content/Effects/GlowCircleCyan", projectile.Center, 0.8f * pulse, new Color(0, 255, 255, 100), 0f);
                for (int i = 0; i < 6; i++)
                {
                    float angle = (System.MathF.PI * 2 / 6) * i + t * 0.02f;
                    DrawTex("AethonMod/Content/Effects/BeamCyan", projectile.Center, 0.8f * pulse,
                        new Color(0, 255, 255, 80), angle);
                }
            }

            // === TEST MAGIC RING V2 (4006) ===
            if (projectile.ai[1] == 4006)
            {
                float hue = (t * 0.005f) % 1f;
                Color c1 = Main.hslToRgb(hue, 1f, 0.5f);
                Color c2 = Main.hslToRgb((hue + 0.33f) % 1f, 1f, 0.5f);
                Color c3 = Main.hslToRgb((hue + 0.66f) % 1f, 1f, 0.5f);

                DrawTex("AethonMod/Content/Effects/MagicRing", projectile.Center, 0.6f, new Color(c1.R, c1.G, c1.B, 200), t * 0.05f);
                DrawTex("AethonMod/Content/Effects/MagicRingGold", projectile.Center, 0.85f, new Color(c2.R, c2.G, c2.B, 150), -t * 0.035f);
                DrawTex("AethonMod/Content/Effects/MagicRing", projectile.Center, 1.1f, new Color(c3.R, c3.G, c3.B, 100), t * 0.025f);

                float pulse = 0.7f + 0.3f * (float)System.Math.Sin(t * 0.1f);
                DrawTex("AethonMod/Content/Effects/GlowCircleWhite", projectile.Center, 0.5f * pulse, new Color(255, 255, 255, 150), 0f);
                DrawTex("AethonMod/Content/Effects/GlowCircleGold", projectile.Center, 0.3f * pulse, new Color(255, 217, 61, 80), 0f);
            }

            // ================================================================
            //  COLOR DEL PROYECTIL (5001-5004)
            //  Usan GlowOrb (esfera de energía con núcleo blanco + halo de color)
            //  + GlowRay (rayo de luz radial) para lens flare
            //  + re-tinte del sprite con additive blending
            // ================================================================

            // === COLOR DORADO (5001) ===
            if (projectile.ai[1] == 5001)
            {
                Color tintColor = new Color(255, 217, 61, 255);
                float pulse = 0.8f + 0.2f * (float)System.Math.Sin(t * 0.1f);
                // Esfera de energía dorada (núcleo blanco + halo dorado)
                DrawTex("AethonMod/Content/Effects/GlowOrbGold", projectile.Center, 0.6f * pulse, new Color(255, 255, 255, 200), 0f);
                // Rayo de luz dorado rotando (lens flare)
                DrawTex("AethonMod/Content/Effects/GlowRayGold", projectile.Center, 0.5f * pulse, new Color(255, 217, 61, 100), t * 0.03f);
                // Re-tintar el sprite
                DrawTintedProjectile(projectile, tintColor, 0.6f);
            }

            // === COLOR CIAN (5002) ===
            if (projectile.ai[1] == 5002)
            {
                Color tintColor = new Color(0, 255, 255, 255);
                float pulse = 0.8f + 0.2f * (float)System.Math.Sin(t * 0.1f);
                DrawTex("AethonMod/Content/Effects/GlowOrbCyan", projectile.Center, 0.6f * pulse, new Color(255, 255, 255, 200), 0f);
                DrawTex("AethonMod/Content/Effects/GlowRayCyan", projectile.Center, 0.5f * pulse, new Color(0, 255, 255, 100), t * 0.03f);
                DrawTintedProjectile(projectile, tintColor, 0.6f);
            }

            // === COLOR MAGENTA (5003) ===
            if (projectile.ai[1] == 5003)
            {
                Color tintColor = new Color(255, 0, 255, 255);
                float pulse = 0.8f + 0.2f * (float)System.Math.Sin(t * 0.1f);
                DrawTex("AethonMod/Content/Effects/GlowOrbMagenta", projectile.Center, 0.6f * pulse, new Color(255, 255, 255, 200), 0f);
                DrawTex("AethonMod/Content/Effects/GlowRay", projectile.Center, 0.5f * pulse, new Color(255, 0, 255, 100), t * 0.03f);
                DrawTintedProjectile(projectile, tintColor, 0.6f);
            }

            // === COLOR ARCOÍRIS (5004) — hue shift continuo ===
            if (projectile.ai[1] == 5004)
            {
                float hue = (t * 0.01f) % 1f;
                Color tintColor = Main.hslToRgb(hue, 1f, 0.5f);
                float pulse = 0.8f + 0.2f * (float)System.Math.Sin(t * 0.1f);
                // Esfera blanca con tinte del hue
                DrawTex("AethonMod/Content/Effects/GlowOrbWhite", projectile.Center, 0.6f * pulse,
                    new Color(tintColor.R, tintColor.G, tintColor.B, 200), 0f);
                // Rayo de luz rotando con color del hue
                DrawTex("AethonMod/Content/Effects/GlowRay", projectile.Center, 0.5f * pulse,
                    new Color(tintColor.R, tintColor.G, tintColor.B, 100), t * 0.03f);
                // Re-tintar el sprite con el color del hue
                DrawTintedProjectile(projectile, tintColor, 0.6f);
            }

            return true;
        }

        /// <summary>
        /// Dibuja el sprite del proyectil con un tinte de color.
        /// Usa Color.Lerp para mezclar la luz natural con el color del tinte.
        /// </summary>
        private void DrawTintedProjectile(Projectile projectile, Color tintColor, float tintAmount)
        {
            try
            {
                Texture2D tex = Terraria.GameContent.TextureAssets.Projectile[projectile.type].Value;
                if (tex == null) return;
                Vector2 origin = new Vector2(tex.Width / 2f, tex.Height / 2f);
                Vector2 drawPos = projectile.Center - Main.screenPosition;

                // Dibujar el sprite original con tinte (mezcla aditiva del color)
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
                // Tinte: dibuja el sprite multiplicado por el color del tinte
                Main.spriteBatch.Draw(tex, drawPos, null,
                    new Color(tintColor.R, tintColor.G, tintColor.B, (int)(255 * tintAmount)),
                    projectile.rotation, origin, projectile.scale, SpriteEffects.None, 0f);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
        }

        // === HELPER: dibuja textura con additive blending ===
        private void DrawTex(string path, Vector2 worldPos, float scale, Color color, float rotation)
        {
            try
            {
                Texture2D tex = ModContent.Request<Texture2D>(path).Value;
                if (tex == null) return;
                Vector2 origin = new Vector2(tex.Width / 2f, tex.Height / 2f);
                Vector2 drawPos = worldPos - Main.screenPosition;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
                Main.spriteBatch.Draw(tex, drawPos, null, color, rotation, origin, scale, SpriteEffects.None, 0f);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
        }
    }
}
