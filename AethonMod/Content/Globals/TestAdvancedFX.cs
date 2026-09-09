using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Globals
{
    /// <summary>
    /// TestAdvancedFX v5.46
    ///
    /// Flags:
    /// 3003 = TestMagicRing
    /// 3004 = TestSparkle
    /// 4001 = ProjBeam
    /// 4006 = TestMagicRingV2
    /// 5004 = ColorRainbow (REESCRITO: cambia el sprite + estela completos)
    ///
    /// TÉCNICA ColorRainbow:
    /// PreDraw retorna FALSE → tModLoader NO dibuja el sprite original.
    /// Nosotros dibujamos:
    /// 1. El sprite del Nightglow con el color del hue (AlphaBlend)
    /// 2. El GlowOrb con additive blending encima
    /// 3. Dust de estela con el color del hue
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

            // ColorRainbow (5004) — estela arcoíris
            if (projectile.ai[1] == 5004 && Main.rand.NextBool(2))
            {
                float hue = (Main.GameUpdateCount * 0.01f) % 1f;
                Color c = Main.hslToRgb(hue, 1f, 0.5f);
                Dust d = Dust.NewDustPerfect(projectile.Center, DustID.RainbowTorch,
                    -projectile.velocity * 0.1f + new Vector2(
                        Main.rand.NextFloat(-1, 1), Main.rand.NextFloat(-1, 1)),
                    200, c, 0.7f);
                d.noGravity = true; d.fadeIn = 0f;
            }

            // ColorRed (5005) — estela roja
            if (projectile.ai[1] == 5005 && Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(projectile.Center, DustID.RedTorch,
                    -projectile.velocity * 0.1f + new Vector2(
                        Main.rand.NextFloat(-1, 1), Main.rand.NextFloat(-1, 1)),
                    200, new Color(255, 50, 50), 0.7f);
                d.noGravity = true; d.fadeIn = 0f;
            }

            // ColorYellow (5006) — estela amarilla
            if (projectile.ai[1] == 5006 && Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(projectile.Center, DustID.YellowTorch,
                    -projectile.velocity * 0.1f + new Vector2(
                        Main.rand.NextFloat(-1, 1), Main.rand.NextFloat(-1, 1)),
                    200, new Color(255, 255, 50), 0.7f);
                d.noGravity = true; d.fadeIn = 0f;
            }

            // ColorGreen (5007) — estela verde
            if (projectile.ai[1] == 5007 && Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(projectile.Center, DustID.GreenTorch,
                    -projectile.velocity * 0.1f + new Vector2(
                        Main.rand.NextFloat(-1, 1), Main.rand.NextFloat(-1, 1)),
                    200, new Color(50, 255, 50), 0.7f);
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
                return true; // dibujar sprite original
            }

            // === TEST SPARKLE (3004) ===
            if (projectile.ai[1] == 3004)
            {
                DrawTex("AethonMod/Content/Effects/GlowCircleGold", projectile.Center, 0.7f, new Color(255, 217, 61, 130), 0f);
                return true;
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
                return true;
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
                return true;
            }

            // === COLOR RAINBOW (5004) — puramente aditivo ===
            // NO toca el sprite del Nightglow (mantiene TODOS sus efectos nativos:
            // colores arcoíris, partículas, glow, bloom).
            // Solo agrega GlowOrb + GlowRay con additive blending encima como
            // capa adicional de brillo arcoíris.
            if (projectile.ai[1] == 5004)
            {
                float hue = (t * 0.01f) % 1f;
                Color c = Main.hslToRgb(hue, 1f, 0.5f);
                float pulse = 0.8f + 0.2f * (float)System.Math.Sin(t * 0.1f);

                // GlowOrb blanco tintado con el color del hue (additive)
                DrawTex("AethonMod/Content/Effects/GlowOrbWhite", projectile.Center, 0.5f * pulse,
                    new Color(c.R, c.G, c.B, 180), 0f);

                // Rayo de luz rotando con color del hue (additive)
                DrawTex("AethonMod/Content/Effects/GlowRay", projectile.Center, 0.4f * pulse,
                    new Color(c.R, c.G, c.B, 80), t * 0.03f);

                return true; // tModLoader dibuja el sprite original del Nightglow
            }

            // === COLOR ROJO (5005) ===
            if (projectile.ai[1] == 5005)
            {
                float pulse = 0.8f + 0.2f * (float)System.Math.Sin(t * 0.1f);
                DrawTex("AethonMod/Content/Effects/GlowOrbWhite", projectile.Center, 0.5f * pulse,
                    new Color(255, 50, 50, 180), 0f);
                DrawTex("AethonMod/Content/Effects/GlowRay", projectile.Center, 0.4f * pulse,
                    new Color(255, 50, 50, 80), t * 0.03f);
                return true;
            }

            // === COLOR AMARILLO (5006) ===
            if (projectile.ai[1] == 5006)
            {
                float pulse = 0.8f + 0.2f * (float)System.Math.Sin(t * 0.1f);
                DrawTex("AethonMod/Content/Effects/GlowOrbWhite", projectile.Center, 0.5f * pulse,
                    new Color(255, 255, 50, 180), 0f);
                DrawTex("AethonMod/Content/Effects/GlowRay", projectile.Center, 0.4f * pulse,
                    new Color(255, 255, 50, 80), t * 0.03f);
                return true;
            }

            // === COLOR VERDE (5007) ===
            if (projectile.ai[1] == 5007)
            {
                float pulse = 0.8f + 0.2f * (float)System.Math.Sin(t * 0.1f);
                DrawTex("AethonMod/Content/Effects/GlowOrbWhite", projectile.Center, 0.5f * pulse,
                    new Color(50, 255, 50, 180), 0f);
                DrawTex("AethonMod/Content/Effects/GlowRay", projectile.Center, 0.4f * pulse,
                    new Color(50, 255, 50, 80), t * 0.03f);
                return true;
            }

            return true; // default: dibujar sprite original
        }

        // === HELPER ===
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
