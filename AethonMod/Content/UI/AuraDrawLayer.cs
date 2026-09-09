using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using AethonMod.Content.Weapons;

namespace AethonMod.Content.UI
{
    /// <summary>
    /// AuraDrawLayer — PlayerDrawLayer que dibuja efectos de aura alrededor del
    /// jugador cuando sostiene items de prueba.
    ///
    /// ESTO ES LA SOLUCIÓN CORRECTA: PlayerDrawLayer se ejecuta durante el
    /// RENDERIZADO, por lo que spriteBatch ESTÁ activo y se puede usar
    /// additive blending.
    ///
    /// HoldItem NO es un hook de renderizado → spriteBatch no está activo.
    /// PreDraw de proyectiles SÍ es de renderizado → funciona.
    /// PlayerDrawLayer SÍ es de renderizado → funciona.
    /// </summary>
    public class AuraDrawLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.HeldItem);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            // Siempre visible; el check del item se hace dentro de Draw
            return true;
        }

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

            // === TEST AURA — glow dorado pulsante ===
            if (type == ModContent.ItemType<TestAura>())
            {
                float pulse = 0.5f + 0.1f * (float)System.Math.Sin(t * 0.05f);
                DrawGlow("AethonMod/Content/Effects/GlowCircleGold", center, pulse, new Color(255, 217, 61, 80), 0f);
            }
            // === TEST RAYS — glow cian tenue ===
            else if (type == ModContent.ItemType<TestRays>())
            {
                float pulse = 0.4f + 0.08f * (float)System.Math.Sin(t * 0.08f);
                DrawGlow("AethonMod/Content/Effects/GlowCircleCyan", center, pulse, new Color(0, 255, 255, 50), 0f);
            }
            // === AURA SHIELD — escudo hexagonal cian ===
            else if (type == ModContent.ItemType<AuraShield>())
            {
                float pulse = 0.8f + 0.2f * (float)System.Math.Sin(t * 0.03f);
                DrawGlow("AethonMod/Content/Effects/ShieldCyan", center, 1.2f * pulse, new Color(100, 200, 255, 100), 0f);
                DrawGlow("AethonMod/Content/Effects/HexCyan", center, 0.9f, new Color(0, 255, 255, 80), t * 0.02f);
                DrawGlow("AethonMod/Content/Effects/GlowCircleCyan", center, 0.5f * pulse, new Color(0, 255, 255, 60), 0f);
            }
            // === AURA SPHERE — esfera cian + núcleo blanco + rayo vertical ===
            else if (type == ModContent.ItemType<AuraSphere>())
            {
                float pulse = 0.7f + 0.3f * (float)System.Math.Sin(t * 0.04f);
                DrawGlow("AethonMod/Content/Effects/GlowCircleCyan", center, 1.5f * pulse, new Color(0, 200, 255, 80), 0f);
                DrawGlow("AethonMod/Content/Effects/GlowCircleWhite", center, 0.5f * pulse, new Color(255, 255, 255, 150), 0f);
                DrawGlow("AethonMod/Content/Effects/BeamCyan", center + new Vector2(0, -60), 1f, new Color(100, 200, 255, 100), 0f);
            }
            // === AURA DIVINE — púrpura + anillo girando ===
            else if (type == ModContent.ItemType<AuraDivine>())
            {
                float pulse = 0.6f + 0.4f * (float)System.Math.Sin(t * 0.02f);
                DrawGlow("AethonMod/Content/Effects/GlowCirclePurple", center, 1.3f * pulse, new Color(200, 50, 255, 70), 0f);
                DrawGlow("AethonMod/Content/Effects/MagicRing", center, 1.0f * pulse, new Color(200, 50, 255, 120), t * 0.03f);
            }
            // === AURA BLOOM — 3 anillos expandiéndose ===
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
            // === AURA COSMIC — 2 anillos girando ===
            else if (type == ModContent.ItemType<AuraCosmic>())
            {
                float rot = t * 0.03f;
                DrawGlow("AethonMod/Content/Effects/MagicRingGold", center, 1.0f, new Color(255, 217, 61, 100), rot);
                DrawGlow("AethonMod/Content/Effects/MagicRing", center, 1.2f, new Color(0, 255, 255, 80), -rot * 0.7f);
                DrawGlow("AethonMod/Content/Effects/GlowCircleGold", center, 0.4f, new Color(255, 217, 61, 60), 0f);
            }
        }

        /// <summary>
        /// Dibuja una textura con additive blending.
        /// SEGURO en PlayerDrawLayer.Draw porque spriteBatch está activo.
        /// </summary>
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
