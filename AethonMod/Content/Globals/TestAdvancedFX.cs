using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;

namespace AethonMod.Content.Globals
{
    /// <summary>
    /// TestAdvancedFX — GlobalProjectile que maneja los efectos visuales
    /// avanzados de las armas de prueba (TestAdvanced.cs).
    ///
    /// Técnicas usadas (de la investigación profunda):
    /// 1. PreDraw con additive blending para glow circles
    /// 2. Trail personalizado guardando posiciones anteriores
    /// 3. Anillo mágico giratorio con textura custom
    /// 4. Sparkle stars con textura custom
    ///
    /// Flags ai[1]:
    /// 3001 = Glow circle con additive blending
    /// 3002 = Trail personalizado
    /// 3003 = Anillo mágico giratorio
    /// 3004 = Sparkle stars
    /// </summary>
    public class TestAdvancedFX : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        // Lista de posiciones anteriores para el trail
        private List<Vector2> _trailPositions = new List<Vector2>();

        public override bool AppliesToEntity(Projectile projectile, bool lateInstantiation)
        {
            return projectile.type == 931; // Nightglow
        }

        public override void AI(Projectile projectile)
        {
            // === SPARKLE STARS (ai[1] == 3004) ===
            if (projectile.ai[1] == 3004)
            {
                // Generar estrellas con textura custom cada 3 frames
                if (Main.rand.NextBool(3))
                {
                    // Usar DustID.Enchanted_Gold pero con la forma de estrella
                    Dust d = Dust.NewDustPerfect(projectile.Center, DustID.Enchanted_Gold,
                        new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), Main.rand.NextFloat(-1.5f, 1.5f)),
                        200, new Color(255, 255, 255), 0.8f);
                    d.noGravity = true;
                    d.fadeIn = 1.5f; // las estrellas crecen un poco
                }
            }

            // === TRAIL PERSONALIZADO (ai[1] == 3002) ===
            if (projectile.ai[1] == 3002)
            {
                // Guardar posiciones para el trail
                _trailPositions.Add(projectile.Center);
                if (_trailPositions.Count > 15)
                    _trailPositions.RemoveAt(0);
            }
        }

        public override bool PreDraw(Projectile projectile, ref Color lightColor)
        {
            // === GLOW CIRCLE con ADDITIVE BLENDING (ai[1] == 3001) ===
            if (projectile.ai[1] == 3001)
            {
                DrawGlowCircle(projectile, "AethonMod/Content/Effects/GlowCircleCyan", 0.8f, new Color(0, 255, 255, 150));
            }

            // === TRAIL PERSONALIZADO (ai[1] == 3002) ===
            if (projectile.ai[1] == 3002)
            {
                DrawTrail(projectile);
                // También dibujar glow circle
                DrawGlowCircle(projectile, "AethonMod/Content/Effects/GlowCircleCyan", 0.6f, new Color(0, 255, 255, 100));
            }

            // === ANILLO MÁGICO GIRATORIO (ai[1] == 3003) ===
            if (projectile.ai[1] == 3003)
            {
                DrawMagicRing(projectile);
                // También glow circle dorado
                DrawGlowCircle(projectile, "AethonMod/Content/Effects/GlowCircleGold", 0.5f, new Color(255, 217, 61, 120));
            }

            // === SPARKLE STARS (ai[1] == 3004) ===
            if (projectile.ai[1] == 3004)
            {
                // Glow dorado detrás
                DrawGlowCircle(projectile, "AethonMod/Content/Effects/GlowCircleGold", 0.7f, new Color(255, 217, 61, 130));
            }

            return true; // dibujar el sprite original encima
        }

        /// <summary>
        /// Dibuja un glow circle con additive blending detrás del proyectil.
        /// </summary>
        private void DrawGlowCircle(Projectile projectile, string texturePath, float scaleMult, Color color)
        {
            try
            {
                Texture2D glow = ModContent.Request<Texture2D>(texturePath).Value;
                if (glow == null) return;

                Vector2 origin = new Vector2(glow.Width / 2f, glow.Height / 2f);
                Vector2 drawPos = projectile.Center - Main.screenPosition;
                float scale = scaleMult + 0.1f * (float)System.Math.Sin(Main.GameUpdateCount * 0.1f); // pulso

                // Cambiar a additive blending
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                Main.spriteBatch.Draw(glow, drawPos, null, color, 0f, origin, scale, SpriteEffects.None, 0f);

                // Volver a alpha blending
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
        }

        /// <summary>
        /// Dibuja un trail conectando las posiciones anteriores del proyectil.
        /// Usa additive blending para que se vea brillante.
        /// </summary>
        private void DrawTrail(Projectile projectile)
        {
            try
            {
                if (_trailPositions.Count < 2) return;

                Texture2D trailTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/TrailGlow").Value;
                if (trailTex == null) return;

                Vector2 origin = new Vector2(trailTex.Width / 2f, trailTex.Height / 2f);

                // Cambiar a additive blending
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                // Dibujar el trail desde la posición más vieja a la más nueva
                for (int i = 0; i < _trailPositions.Count - 1; i++)
                {
                    float progress = (float)i / _trailPositions.Count;
                    float alpha = progress * 0.8f; // las posiciones viejas son más transparentes
                    float scale = progress * 0.5f + 0.2f;

                    Vector2 drawPos = _trailPositions[i] - Main.screenPosition;

                    // Rotar hacia la siguiente posición
                    Vector2 dir = _trailPositions[i + 1] - _trailPositions[i];
                    float rotation = dir.ToRotation();

                    Main.spriteBatch.Draw(trailTex, drawPos, null,
                        new Color(0, 255, 255, (int)(255 * alpha)),
                        rotation, origin, scale, SpriteEffects.None, 0f);
                }

                // Volver a alpha blending
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
        }

        /// <summary>
        /// Dibuja un anillo mágico girando alrededor del proyectil.
        /// Usa additive blending.
        /// </summary>
        private void DrawMagicRing(Projectile projectile)
        {
            try
            {
                Texture2D ring = ModContent.Request<Texture2D>("AethonMod/Content/Effects/MagicRing").Value;
                if (ring == null) return;

                Vector2 origin = new Vector2(ring.Width / 2f, ring.Height / 2f);
                Vector2 drawPos = projectile.Center - Main.screenPosition;

                // Rotación que cambia con el tiempo
                float rotation = Main.GameUpdateCount * 0.05f;
                float scale = 0.6f + 0.1f * (float)System.Math.Sin(Main.GameUpdateCount * 0.08f);

                // Cambiar a additive blending
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                // Anillo cian
                Main.spriteBatch.Draw(ring, drawPos, null,
                    new Color(0, 255, 255, 180), rotation, origin, scale, SpriteEffects.None, 0f);

                // Segundo anillo más grande girando en sentido contrario
                Main.spriteBatch.Draw(ring, drawPos, null,
                    new Color(255, 217, 61, 100), -rotation * 0.7f, origin, scale * 1.3f, SpriteEffects.None, 0f);

                // Volver a alpha blending
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
        }
    }
}
