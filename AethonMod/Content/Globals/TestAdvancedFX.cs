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
    /// de proyectiles con additive blending via PreDraw.
    ///
    /// PreDraw SÍ es un hook de renderizado → spriteBatch está activo.
    ///
    /// Flags ai[1]:
    /// 3003 = TestMagicRing (2 anillos + glow)
    /// 3004 = TestSparkle (glow + estrellas)
    /// 4001 = ProjBeam (glow blanco+cian + 6 rayos DrawTex)
    /// 4002 = ProjElectric (trail DrawTex + glow)
    /// 4003 = ProjImpact (explosión DrawTex al morir)
    /// 4004 = ProjRainbow (trail arcoíris DrawTex + hue shift)
    /// 4005 = ProjLightning (glow cian + relámpagos DrawTex)
    /// 4006 = TestMagicRingV2 (3 anillos + hue shift + multi-glow)
    /// </summary>
    public class TestAdvancedFX : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        private List<Vector2> _trail = new List<Vector2>();
        private bool _hasImpacted = false;
        private int _impactTimer = 0;

        public override bool AppliesToEntity(Projectile projectile, bool lateInstantiation)
        { return projectile.type == 931; }

        public override void AI(Projectile projectile)
        {
            // Guardar posiciones para trails (Electric y Rainbow)
            if (projectile.ai[1] == 4002 || projectile.ai[1] == 4004)
            {
                _trail.Add(projectile.Center);
                if (_trail.Count > 15) _trail.RemoveAt(0);
            }

            // Sparkle stars (3004) — Dust que crece
            if (projectile.ai[1] == 3004 && Main.rand.NextBool(4))
            {
                Dust d = Dust.NewDustPerfect(projectile.Center, DustID.Enchanted_Gold,
                    new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), Main.rand.NextFloat(-1.5f, 1.5f)),
                    200, new Color(255, 255, 255), 0.8f);
                d.noGravity = true; d.fadeIn = 1.5f;
            }

            // ProjLightning (4005) — Dust de relámpagos
            if (projectile.ai[1] == 4005 && Main.rand.NextBool(5))
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

            // ProjImpact (4003) — después del impacto, contar frames
            if (_hasImpacted)
            {
                _impactTimer++;
                if (_impactTimer > 15) _hasImpacted = false;
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

            // === PROJ BEAM (4001) — glow blanco + cian + rayos DrawTex ===
            if (projectile.ai[1] == 4001)
            {
                float pulse = 0.8f + 0.2f * (float)System.Math.Sin(t * 0.15f);
                DrawTex("AethonMod/Content/Effects/GlowCircleWhite", projectile.Center, 1.2f * pulse, new Color(255, 255, 255, 180), 0f);
                DrawTex("AethonMod/Content/Effects/GlowCircleCyan", projectile.Center, 0.8f * pulse, new Color(0, 255, 255, 100), 0f);
                // 6 rayos rotando con DrawTex (no Dust)
                for (int i = 0; i < 6; i++)
                {
                    float angle = (System.MathF.PI * 2 / 6) * i + t * 0.02f;
                    float len = 0.8f * pulse;
                    DrawTex("AethonMod/Content/Effects/BeamCyan", projectile.Center, len,
                        new Color(0, 255, 255, 80), angle);
                }
            }

            // === PROJ ELECTRIC (4002) — trail DrawTex + glow ===
            if (projectile.ai[1] == 4002 && _trail.Count > 2)
            {
                DrawTex("AethonMod/Content/Effects/GlowCircleCyan", projectile.Center, 0.5f, new Color(0, 255, 255, 100), 0f);
                // Trail con DrawTex (no Dust)
                for (int i = 0; i < _trail.Count - 1; i++)
                {
                    float alpha = (float)i / _trail.Count;
                    Vector2 dir = _trail[i + 1] - _trail[i];
                    float rot = dir.ToRotation();
                    DrawTex("AethonMod/Content/Effects/TrailGlow", _trail[i], 0.3f + alpha * 0.3f,
                        new Color(0, 255, 255, (int)(200 * alpha)), rot);
                }
            }

            // === PROJ IMPACT (4003) — explosión DrawTex después del impacto ===
            if (projectile.ai[1] == 4003 && _hasImpacted)
            {
                float expand = (float)_impactTimer / 15f;
                float scale = 0.5f + expand * 3f;
                int alpha = (int)(200 * (1f - expand));
                DrawTex("AethonMod/Content/Effects/GlowCircleWhite", projectile.Center, scale,
                    new Color(255, 255, 255, alpha), 0f);
                DrawTex("AethonMod/Content/Effects/GlowCircleCyan", projectile.Center, scale * 0.7f,
                    new Color(0, 255, 255, (int)(alpha * 0.6f)), 0f);
            }

            // === PROJ RAINBOW (4004) — trail arcoíris DrawTex ===
            if (projectile.ai[1] == 4004 && _trail.Count > 2)
            {
                DrawTex("AethonMod/Content/Effects/GlowCircleWhite", projectile.Center, 0.6f, new Color(255, 255, 255, 120), 0f);
                // Trail con hue rotation usando DrawTex
                for (int i = 0; i < _trail.Count - 1; i++)
                {
                    float hue = ((float)i / _trail.Count + t * 0.01f) % 1f;
                    Color c = Main.hslToRgb(hue, 1f, 0.5f);
                    float alpha = (float)i / _trail.Count * 0.8f;
                    Vector2 dir = _trail[i + 1] - _trail[i];
                    float rot = dir.ToRotation();
                    DrawTex("AethonMod/Content/Effects/TrailGlow", _trail[i], 0.4f,
                        new Color(c.R, c.G, c.B, (int)(255 * alpha)), rot);
                }
            }

            // === PROJ LIGHTNING (4005) — glow cian + blanco ===
            if (projectile.ai[1] == 4005)
            {
                float pulse = 0.7f + 0.3f * (float)System.Math.Sin(t * 0.2f);
                DrawTex("AethonMod/Content/Effects/GlowCircleCyan", projectile.Center, 0.8f * pulse, new Color(0, 255, 255, 150), 0f);
                DrawTex("AethonMod/Content/Effects/GlowCircleWhite", projectile.Center, 0.4f * pulse, new Color(255, 255, 255, 100), 0f);
            }

            // === TEST MAGIC RING V2 (4006) — 3 anillos + hue shift + multi-glow ===
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

            return true;
        }

        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // PROJ IMPACT (4003) — marcar impacto para explosión DrawTex
            if (projectile.ai[1] == 4003)
            {
                _hasImpacted = true;
                _impactTimer = 0;

                // Dust multicolor
                for (int i = 0; i < 15; i++)
                {
                    Dust d = Dust.NewDustPerfect(target.Center, DustID.RainbowTorch,
                        new Vector2(Main.rand.NextFloat(-5, 5), Main.rand.NextFloat(-5, 5)),
                        200, Main.hslToRgb(Main.rand.NextFloat(0, 1), 1f, 0.5f), 1f);
                    d.noGravity = true; d.fadeIn = 0f;
                }
                Lighting.AddLight(target.Center, new Vector3(1f, 1f, 1f));
            }
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
