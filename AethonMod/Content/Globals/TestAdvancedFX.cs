using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Globals
{
    /// <summary>
    /// TestAdvancedFX — GlobalProjectile que maneja TODOS los efectos visuales
    /// avanzados de las armas de prueba.
    ///
    /// Flags ai[1]:
    /// 3003 = Anillo mágico (original)
    /// 3004 = Sparkle stars (original)
    /// 4001 = ProjBeam (lens flare + bloom)
    /// 4002 = ProjElectric (trail jagged)
    /// 4003 = ProjImpact (explosión multicolor en OnHitNPC)
    /// 4004 = ProjRainbow (trail arcoíris)
    /// 4005 = ProjLightning (relámpagos alrededor)
    /// 4006 = MagicRingV2 (3 anillos + hue shift + sparkles)
    /// </summary>
    public class TestAdvancedFX : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        private List<Vector2> _trail = new List<Vector2>();

        public override bool AppliesToEntity(Projectile projectile, bool lateInstantiation)
        { return projectile.type == 931; }

        public override void AI(Projectile projectile)
        {
            // Guardar posiciones para trails
            if (projectile.ai[1] == 4002 || projectile.ai[1] == 4004)
            {
                _trail.Add(projectile.Center);
                if (_trail.Count > 20) _trail.RemoveAt(0);
            }

            // Sparkle stars (3004)
            if (projectile.ai[1] == 3004 && Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(projectile.Center, DustID.Enchanted_Gold,
                    new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), Main.rand.NextFloat(-1.5f, 1.5f)),
                    200, new Color(255, 255, 255), 0.8f);
                d.noGravity = true; d.fadeIn = 1.5f;
            }

            // ProjElectric (4002) — sparkles eléctricos
            if (projectile.ai[1] == 4002 && Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(projectile.Center + new Vector2(
                    Main.rand.NextFloat(-8, 8), Main.rand.NextFloat(-8, 8)),
                    DustID.BlueTorch, Vector2.Zero, 200, new Color(0, 255, 255), 0.5f);
                d.noGravity = true; d.fadeIn = 0f;
            }

            // ProjLightning (4005) — relámpagos alrededor del proyectil
            if (projectile.ai[1] == 4005 && Main.rand.NextBool(4))
            {
                float angle = Main.rand.NextFloat(0, System.MathF.PI * 2);
                float dist = 20f;
                Vector2 start = projectile.Center + new Vector2(
                    (float)System.Math.Cos(angle) * dist, (float)System.Math.Sin(angle) * dist);
                Vector2 end = start + new Vector2(
                    Main.rand.NextFloat(-15, 15), Main.rand.NextFloat(5, 20));
                for (int j = 0; j < 4; j++)
                {
                    float t = j / 4f;
                    Vector2 pos = Vector2.Lerp(start, end, t);
                    Dust d = Dust.NewDustPerfect(pos, DustID.BlueTorch, Vector2.Zero, 220, new Color(0, 255, 255), 0.4f);
                    d.noGravity = true; d.fadeIn = 0f;
                }
            }

            // MagicRingV2 (4006) — sparkles a lo largo del anillo
            if (projectile.ai[1] == 4006 && Main.rand.NextBool(5))
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
            // === ANILLO MÁGICO (3003) ===
            if (projectile.ai[1] == 3003)
            {
                DrawTex("AethonMod/Content/Effects/MagicRing", projectile.Center, 0.6f, new Color(0, 255, 255, 180), Main.GameUpdateCount * 0.05f);
                DrawTex("AethonMod/Content/Effects/MagicRingGold", projectile.Center, 0.8f, new Color(255, 217, 61, 100), -Main.GameUpdateCount * 0.035f);
                DrawTex("AethonMod/Content/Effects/GlowCircleGold", projectile.Center, 0.5f, new Color(255, 217, 61, 120), 0f);
            }

            // === SPARKLE STARS (3004) ===
            if (projectile.ai[1] == 3004)
            { DrawTex("AethonMod/Content/Effects/GlowCircleGold", projectile.Center, 0.7f, new Color(255, 217, 61, 130), 0f); }

            // === PROJ BEAM (4001) — lens flare + bloom radial ===
            if (projectile.ai[1] == 4001)
            {
                float pulse = 0.8f + 0.2f * (float)System.Math.Sin(Main.GameUpdateCount * 0.15f);
                DrawTex("AethonMod/Content/Effects/GlowCircleWhite", projectile.Center, 1.2f * pulse, new Color(255, 255, 255, 180), 0f);
                DrawTex("AethonMod/Content/Effects/GlowCircleCyan", projectile.Center, 0.8f * pulse, new Color(0, 255, 255, 100), 0f);
                // Lens flare rays
                for (int i = 0; i < 6; i++)
                {
                    float angle = (System.MathF.PI * 2 / 6) * i + Main.GameUpdateCount * 0.02f;
                    float len = 25f * pulse;
                    Vector2 end = projectile.Center + new Vector2((float)System.Math.Cos(angle) * len, (float)System.Math.Sin(angle) * len);
                    for (int j = 0; j < 4; j++)
                    {
                        float t = j / 4f;
                        Vector2 pos = Vector2.Lerp(projectile.Center, end, t);
                        Dust d = Dust.NewDustPerfect(pos, DustID.BlueTorch, Vector2.Zero, 220, new Color(0, 255, 255), 0.3f);
                        d.noGravity = true; d.fadeIn = 0f;
                    }
                }
            }

            // === PROJ ELECTRIC (4002) — trail jagged ===
            if (projectile.ai[1] == 4002 && _trail.Count > 2)
            {
                DrawTex("AethonMod/Content/Effects/GlowCircleCyan", projectile.Center, 0.5f, new Color(0, 255, 255, 100), 0f);
                // Dibujar trail jagged con dust
                for (int i = 0; i < _trail.Count - 1; i += 2)
                {
                    float alpha = (float)i / _trail.Count * 0.6f;
                    Vector2 jitter = new Vector2(Main.rand.NextFloat(-5, 5), Main.rand.NextFloat(-5, 5));
                    Dust d = Dust.NewDustPerfect(_trail[i] + jitter, DustID.BlueTorch, Vector2.Zero,
                        (int)(200 * alpha), new Color(0, 255, 255, (int)(255 * alpha)), 0.4f);
                    d.noGravity = true; d.fadeIn = 0f;
                }
            }

            // === PROJ RAINBOW (4004) — trail arcoíris ===
            if (projectile.ai[1] == 4004 && _trail.Count > 2)
            {
                DrawTex("AethonMod/Content/Effects/GlowCircleWhite", projectile.Center, 0.6f, new Color(255, 255, 255, 120), 0f);
                // Trail con hue rotation
                for (int i = 0; i < _trail.Count - 1; i++)
                {
                    float hue = (float)i / _trail.Count + (Main.GameUpdateCount * 0.01f) % 1f;
                    Color c = HueToColor(hue);
                    float alpha = (float)i / _trail.Count * 0.7f;
                    Dust d = Dust.NewDustPerfect(_trail[i], DustID.RainbowTorch, Vector2.Zero,
                        (int)(200 * alpha), c, 0.5f);
                    d.noGravity = true; d.fadeIn = 0f;
                }
            }

            // === PROJ LIGHTNING (4005) — glow cian ===
            if (projectile.ai[1] == 4005)
            {
                float pulse = 0.7f + 0.3f * (float)System.Math.Sin(Main.GameUpdateCount * 0.2f);
                DrawTex("AethonMod/Content/Effects/GlowCircleCyan", projectile.Center, 0.8f * pulse, new Color(0, 255, 255, 150), 0f);
                DrawTex("AethonMod/Content/Effects/GlowCircleWhite", projectile.Center, 0.4f * pulse, new Color(255, 255, 255, 100), 0f);
            }

            // === MAGIC RING V2 (4006) — 3 anillos + hue shift + multi-glow ===
            if (projectile.ai[1] == 4006)
            {
                float t = Main.GameUpdateCount;
                float hue = (t * 0.005f) % 1f;
                Color c1 = HueToColor(hue);
                Color c2 = HueToColor((hue + 0.33f) % 1f);
                Color c3 = HueToColor((hue + 0.66f) % 1f);

                // 3 anillos a diferentes velocidades
                DrawTex("AethonMod/Content/Effects/MagicRing", projectile.Center, 0.6f, c1 * 0.8f, t * 0.05f);
                DrawTex("AethonMod/Content/Effects/MagicRingGold", projectile.Center, 0.85f, c2 * 0.6f, -t * 0.035f);
                DrawTex("AethonMod/Content/Effects/MagicRing", projectile.Center, 1.1f, c3 * 0.4f, t * 0.025f);

                // Multi-glow
                float pulse = 0.7f + 0.3f * (float)System.Math.Sin(t * 0.1f);
                DrawTex("AethonMod/Content/Effects/GlowCircleWhite", projectile.Center, 0.5f * pulse, new Color(255, 255, 255, 150), 0f);
                DrawTex("AethonMod/Content/Effects/GlowCircleGold", projectile.Center, 0.3f * pulse, new Color(255, 217, 61, 80), 0f);
            }

            return true;
        }

        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // === PROJ IMPACT (4003) — explosión multicolor ===
            if (projectile.ai[1] == 4003)
            {
                // 3 colores de sparkles
                for (int i = 0; i < 15; i++)
                {
                    Dust d = Dust.NewDustPerfect(target.Center, DustID.RainbowTorch,
                        new Vector2(Main.rand.NextFloat(-5, 5), Main.rand.NextFloat(-5, 5)),
                        200, Main.hslToRgb(Main.rand.NextFloat(0, 1), 1f, 0.5f), 1f);
                    d.noGravity = true; d.fadeIn = 0f;
                }
                // NOTA: No se puede usar spriteBatch en OnHitNPC (no es hook de render).
                // El glow explosion se hace solo con Dust + Lighting.
                Lighting.AddLight(target.Center, new Vector3(1f, 1f, 1f));
            }
        }

        // === HELPERS ===
        private void DrawTex(string path, Vector2 center, float scale, Color color, float rotation)
        {
            try
            {
                Texture2D tex = ModContent.Request<Texture2D>(path).Value;
                if (tex == null) return;
                Vector2 origin = new Vector2(tex.Width / 2f, tex.Height / 2f);
                Vector2 drawPos = center - Main.screenPosition;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
                Main.spriteBatch.Draw(tex, drawPos, null, color, rotation, origin, scale, SpriteEffects.None, 0f);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
        }

        private static Color HueToColor(float hue)
        {
            return Main.hslToRgb(hue % 1f, 1f, 0.5f);
        }
    }
}
