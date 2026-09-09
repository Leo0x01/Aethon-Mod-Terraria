using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using AethonMod.Content.Weapons;

namespace AethonMod.Content.UI
{
    /// <summary>
    /// AuraDrawLayer v5.43 — REESCRITO
    ///
    /// Auras que SÍ funcionaban (mantenidas):
    /// TestAura, TestRays, AuraBloom, AuraCosmic
    ///
    /// Auras REESCRITAS con efectos completamente diferentes:
    /// AuraShield: escudo con ShieldCyan + HexCyan rotando + glow doble
    /// AuraSphere: esfera con 3 glows concéntricos + BeamCyan
    /// AuraDivine: aura con rayos rotando (BeamCyan en 8 direcciones) + glow púrpura
    /// </summary>
    public class AuraDrawLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.HeldItem);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        { return true; }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            if (player == null || !player.active || player.dead) return;
            if (Main.dedServ) return;

            Item held = player.HeldItem;
            if (held == null || held.type == 0) return;

            Vector2 center = player.Center - Main.screenPosition;
            float t = Main.GameUpdateCount;
            int type = held.type;

            // === TEST AURA — MANTIENE: glow dorado pulsante ===
            if (type == ModContent.ItemType<TestAura>())
            {
                float pulse = 0.5f + 0.1f * (float)System.Math.Sin(t * 0.05f);
                DrawGlow("AethonMod/Content/Effects/GlowCircleGold", center, pulse, new Color(255, 217, 61, 80), 0f);
            }
            // === TEST RAYS — MANTIENE: glow cian tenue ===
            else if (type == ModContent.ItemType<TestRays>())
            {
                float pulse = 0.4f + 0.08f * (float)System.Math.Sin(t * 0.08f);
                DrawGlow("AethonMod/Content/Effects/GlowCircleCyan", center, pulse, new Color(0, 255, 255, 50), 0f);
            }
            // === AURA SHIELD — REESCRITO: escudo + hexágono + 2 glows ===
            else if (type == ModContent.ItemType<AuraShield>())
            {
                float pulse = 0.9f + 0.1f * (float)System.Math.Sin(t * 0.03f);
                // Escudo principal
                DrawGlow("AethonMod/Content/Effects/ShieldCyan", center, 1.0f * pulse, new Color(100, 200, 255, 120), 0f);
                // Hexágono rotando
                DrawGlow("AethonMod/Content/Effects/HexCyan", center, 1.1f * pulse, new Color(0, 255, 255, 100), t * 0.02f);
                // Glow interior
                DrawGlow("AethonMod/Content/Effects/GlowCircleCyan", center, 0.6f * pulse, new Color(0, 255, 255, 80), 0f);
                // Glow exterior blanco
                DrawGlow("AethonMod/Content/Effects/GlowCircleWhite", center, 0.3f * pulse, new Color(255, 255, 255, 60), 0f);
            }
            // === AURA SPHERE — REESCRITO: 3 glows concéntricos + beam ===
            else if (type == ModContent.ItemType<AuraSphere>())
            {
                float pulse = 0.8f + 0.2f * (float)System.Math.Sin(t * 0.04f);
                // Esfera exterior cian grande
                DrawGlow("AethonMod/Content/Effects/GlowCircleCyan", center, 1.8f * pulse, new Color(0, 200, 255, 60), 0f);
                // Esfera media cian
                DrawGlow("AethonMod/Content/Effects/GlowCircleCyan", center, 1.0f * pulse, new Color(0, 255, 255, 100), 0f);
                // Núcleo blanco
                DrawGlow("AethonMod/Content/Effects/GlowCircleWhite", center, 0.6f * pulse, new Color(255, 255, 255, 180), 0f);
                // 3 rayos verticales BeamCyan
                DrawGlow("AethonMod/Content/Effects/BeamCyan", center + new Vector2(0, -50), 1.2f, new Color(100, 200, 255, 120), 0f);
                DrawGlow("AethonMod/Content/Effects/BeamCyan", center + new Vector2(-20, -40), 0.8f, new Color(0, 255, 255, 80), 0.1f);
                DrawGlow("AethonMod/Content/Effects/BeamCyan", center + new Vector2(20, -40), 0.8f, new Color(0, 255, 255, 80), -0.1f);
            }
            // === AURA DIVINE — REESCRITO: 8 rayos rotando + glow púrpura ===
            else if (type == ModContent.ItemType<AuraDivine>())
            {
                float pulse = 0.7f + 0.3f * (float)System.Math.Sin(t * 0.02f);
                // Glow púrpura grande
                DrawGlow("AethonMod/Content/Effects/GlowCirclePurple", center, 1.5f * pulse, new Color(200, 50, 255, 60), 0f);
                // 8 rayos BeamCyan rotando (color púrpura)
                for (int i = 0; i < 8; i++)
                {
                    float angle = (System.MathF.PI * 2 / 8) * i + t * 0.015f;
                    DrawGlow("AethonMod/Content/Effects/BeamCyan", center, 1.0f * pulse,
                        new Color(200, 50, 255, 100), angle);
                }
                // Núcleo blanco
                DrawGlow("AethonMod/Content/Effects/GlowCircleWhite", center, 0.4f * pulse, new Color(255, 255, 255, 100), 0f);
            }
            // === AURA BLOOM — MANTIENE: 3 anillos expandiéndose ===
            else if (type == ModContent.ItemType<AuraBloom>())
            {
                for (int i = 0; i < 3; i++)
                {
                    float phase = ((t + i * 20) % 60) / 60f;
                    float scale = 0.3f + phase * 1.8f;
                    int alpha = (int)(150 * (1f - phase));
                    DrawGlow("AethonMod/Content/Effects/MagicRingGold", center, scale, new Color(255, 217, 61, alpha), 0f);
                }
                DrawGlow("AethonMod/Content/Effects/GlowCircleGreen", center, 0.6f, new Color(50, 255, 100, 100), 0f);
            }
            // === AURA COSMIC — MANTIENE: 2 anillos girando ===
            else if (type == ModContent.ItemType<AuraCosmic>())
            {
                float rot = t * 0.03f;
                DrawGlow("AethonMod/Content/Effects/MagicRingGold", center, 1.0f, new Color(255, 217, 61, 100), rot);
                DrawGlow("AethonMod/Content/Effects/MagicRing", center, 1.2f, new Color(0, 255, 255, 80), -rot * 0.7f);
                DrawGlow("AethonMod/Content/Effects/GlowCircleGold", center, 0.4f, new Color(255, 217, 61, 60), 0f);
            }
        }

        private void DrawGlow(string path, Vector2 pos, float scale, Color color, float rotation)
        {
            try
            {
                Texture2D tex = ModContent.Request<Texture2D>(path).Value;
                if (tex == null) return;
                Vector2 origin = new Vector2(tex.Width / 2f, tex.Height / 2f);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
                Main.spriteBatch.Draw(tex, pos, null, color, rotation, origin, scale, SpriteEffects.None, 0f);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
        }
    }
}
