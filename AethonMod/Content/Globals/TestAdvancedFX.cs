using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Globals
{
    /// <summary>
    /// TestAdvancedFX v5.43 — REESCRITO COMPLETO
    ///
    /// Efectos que SÍ funcionaban (mantenidos):
    /// 3003 = TestMagicRing, 3004 = TestSparkle, 4001 = ProjBeam, 4006 = TestMagicRingV2
    ///
    /// Efectos REESCRITOS:
    /// 4002 = ProjElectric (trail con Dust + glow más grande)
    /// 4003 = ProjImpact (explosión en Kill, no en OnHitNPC)
    /// 4004 = ProjRainbow (trail con Dust arcoíris + glow)
    /// 4005 = ProjLightning (relámpagos DrawTex en PreDraw + glow)
    /// </summary>
    public class TestAdvancedFX : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public override bool AppliesToEntity(Projectile projectile, bool lateInstantiation)
        { return projectile.type == 931; }

        public override void AI(Projectile projectile)
        {
            // Sparkle stars (3004) — Dust que crece
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

            // === PROJ ELECTRIC (4002) — sparkles eléctricos cian ===
            if (projectile.ai[1] == 4002 && Main.rand.NextBool(3))
            {
                // Sparkles alrededor del proyectil
                for (int i = 0; i < 3; i++)
                {
                    float angle = Main.rand.NextFloat(0, System.MathF.PI * 2);
                    float dist = Main.rand.NextFloat(5f, 15f);
                    Vector2 offset = new Vector2(
                        (float)System.Math.Cos(angle) * dist,
                        (float)System.Math.Sin(angle) * dist);
                    Dust d = Dust.NewDustPerfect(projectile.Center + offset,
                        DustID.BlueTorch, -projectile.velocity * 0.05f, 200,
                        new Color(0, 255, 255), 0.6f);
                    d.noGravity = true; d.fadeIn = 0f;
                }
            }

            // === PROJ RAINBOW (4004) — Dust arcoíris ===
            if (projectile.ai[1] == 4004 && Main.rand.NextBool(2))
            {
                float hue = (Main.GameUpdateCount * 0.02f) % 1f;
                Color c = Main.hslToRgb(hue, 1f, 0.5f);
                Dust d = Dust.NewDustPerfect(projectile.Center, DustID.RainbowTorch,
                    -projectile.velocity * 0.08f + new Vector2(
                        Main.rand.NextFloat(-1, 1), Main.rand.NextFloat(-1, 1)),
                    200, c, 0.7f);
                d.noGravity = true; d.fadeIn = 0f;
            }

            // === PROJ LIGHTNING (4005) — relámpagos Dust ===
            if (projectile.ai[1] == 4005 && Main.rand.NextBool(4))
            {
                float angle = Main.rand.NextFloat(0, System.MathF.PI * 2);
                float dist = 25f;
                Vector2 start = projectile.Center + new Vector2(
                    (float)System.Math.Cos(angle) * dist, (float)System.Math.Sin(angle) * dist);
                // Relámpago jagged
                Vector2 current = start;
                for (int j = 0; j < 5; j++)
                {
                    Vector2 next = current + new Vector2(
                        Main.rand.NextFloat(-8, 8), Main.rand.NextFloat(3, 10));
                    Dust d = Dust.NewDustPerfect(current, DustID.BlueTorch,
                        Vector2.Zero, 220, new Color(0, 255, 255), 0.5f);
                    d.noGravity = true; d.fadeIn = 0f;
                    current = next;
                }
            }
        }

        public override bool PreDraw(Projectile projectile, ref Color lightColor)
        {
            float t = Main.GameUpdateCount;

            // === TEST MAGIC RING (3003) — MANTIENE ===
            if (projectile.ai[1] == 3003)
            {
                DrawTex("AethonMod/Content/Effects/MagicRing", projectile.Center, 0.6f, new Color(0, 255, 255, 180), t * 0.05f);
                DrawTex("AethonMod/Content/Effects/MagicRingGold", projectile.Center, 0.8f, new Color(255, 217, 61, 100), -t * 0.035f);
                DrawTex("AethonMod/Content/Effects/GlowCircleGold", projectile.Center, 0.5f, new Color(255, 217, 61, 120), 0f);
            }

            // === TEST SPARKLE (3004) — MANTIENE ===
            if (projectile.ai[1] == 3004)
            {
                DrawTex("AethonMod/Content/Effects/GlowCircleGold", projectile.Center, 0.7f, new Color(255, 217, 61, 130), 0f);
            }

            // === PROJ BEAM (4001) — MANTIENE ===
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

            // === PROJ ELECTRIC (4002) — MEJORADO: glow más grande ===
            if (projectile.ai[1] == 4002)
            {
                float pulse = 0.6f + 0.2f * (float)System.Math.Sin(t * 0.2f);
                DrawTex("AethonMod/Content/Effects/GlowCircleCyan", projectile.Center, 1.0f * pulse, new Color(0, 255, 255, 150), 0f);
                DrawTex("AethonMod/Content/Effects/GlowCircleWhite", projectile.Center, 0.4f * pulse, new Color(255, 255, 255, 100), 0f);
            }

            // === PROJ IMPACT (4003) — MEJORADO: glow siempre presente ===
            if (projectile.ai[1] == 4003)
            {
                float pulse = 0.7f + 0.3f * (float)System.Math.Sin(t * 0.15f);
                DrawTex("AethonMod/Content/Effects/GlowCircleWhite", projectile.Center, 0.8f * pulse, new Color(255, 255, 255, 150), 0f);
                DrawTex("AethonMod/Content/Effects/GlowCircleCyan", projectile.Center, 0.5f * pulse, new Color(0, 255, 255, 100), 0f);
            }

            // === PROJ RAINBOW (4004) — MEJORADO: glow arcoíris ===
            if (projectile.ai[1] == 4004)
            {
                float hue = (t * 0.01f) % 1f;
                Color c = Main.hslToRgb(hue, 1f, 0.5f);
                float pulse = 0.7f + 0.3f * (float)System.Math.Sin(t * 0.1f);
                DrawTex("AethonMod/Content/Effects/GlowCircleWhite", projectile.Center, 0.8f * pulse, new Color(255, 255, 255, 150), 0f);
                DrawTex("AethonMod/Content/Effects/GlowCircleCyan", projectile.Center, 0.6f * pulse,
                    new Color(c.R, c.G, c.B, 120), 0f);
            }

            // === PROJ LIGHTNING (4005) — MEJORADO: glow cian grande ===
            if (projectile.ai[1] == 4005)
            {
                float pulse = 0.8f + 0.3f * (float)System.Math.Sin(t * 0.2f);
                DrawTex("AethonMod/Content/Effects/GlowCircleCyan", projectile.Center, 1.0f * pulse, new Color(0, 255, 255, 180), 0f);
                DrawTex("AethonMod/Content/Effects/GlowCircleWhite", projectile.Center, 0.5f * pulse, new Color(255, 255, 255, 120), 0f);
            }

            // === TEST MAGIC RING V2 (4006) — MANTIENE ===
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
            // === PROJ IMPACT (4003) — explosión de Dust multicolor ===
            if (projectile.ai[1] == 4003)
            {
                for (int i = 0; i < 20; i++)
                {
                    float angle = Main.rand.NextFloat(0, System.MathF.PI * 2);
                    float speed = Main.rand.NextFloat(3f, 8f);
                    Dust d = Dust.NewDustPerfect(target.Center, DustID.RainbowTorch,
                        new Vector2((float)System.Math.Cos(angle) * speed, (float)System.Math.Sin(angle) * speed),
                        200, Main.hslToRgb(Main.rand.NextFloat(0, 1), 1f, 0.5f), 1.2f);
                    d.noGravity = true; d.fadeIn = 0f;
                }
                Lighting.AddLight(target.Center, new Vector3(1f, 1f, 1f));
            }
        }

        public override void Kill(Projectile projectile, int timeLeft)
        {
            // === PROJ IMPACT (4003) — explosión extra al morir ===
            if (projectile.ai[1] == 4003)
            {
                for (int i = 0; i < 25; i++)
                {
                    float angle = Main.rand.NextFloat(0, System.MathF.PI * 2);
                    float speed = Main.rand.NextFloat(2f, 10f);
                    Dust d = Dust.NewDustPerfect(projectile.Center, DustID.RainbowTorch,
                        new Vector2((float)System.Math.Cos(angle) * speed, (float)System.Math.Sin(angle) * speed),
                        200, Main.hslToRgb(Main.rand.NextFloat(0, 1), 1f, 0.5f), 1.5f);
                    d.noGravity = true; d.fadeIn = 0f;
                }
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
