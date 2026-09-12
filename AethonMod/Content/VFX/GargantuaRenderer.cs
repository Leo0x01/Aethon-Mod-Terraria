using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// GargantuaRenderer — v6.04 — EL AGUJERO NEGRO EXACTO A LA REFERENCIA.
    ///
    /// Render por CAPAS con las dos texturas procedurales generadas con la
    /// física de proyección de un Gargantua (medidas píxel-exactas de la
    /// referencia del usuario: sombra 28% del ancho, disco compacto ±3.55 r_sh,
    /// banda gruesa ±0.43 r_sh, arco de lente a 1.34 r_sh, Doppler cálido a
    /// la izquierda, inclinación -12°):
    ///
    ///   1. GARGANTUABACK (aditivo): neblina roja + tendrillas de gas +
    ///      arco de lente superior ARDIENDO + arco inferior + ANILLO DE
    ///      FOTONES naranja-blanco (la parte más brillante) + penumbra.
    ///   2. LA SOMBRA (alpha): círculo negro ABSOLUTO con borde NÍTIDO
    ///      (GargantuaShadow, caída de 6 px) — el vacío que come la luz.
    ///   3. GARGANTUAFRONT (aditivo): la banda del disco de acreción que
    ///      CRUZA por delante de la sombra — filamentos de plasma, rim
    ///      interior blanco-dorado y hotspot cegador en el cruce.
    ///
    /// El conjunto BAMBOLEA (±1.1°) y RESPIRA (0.97..1.03) para estar vivo.
    /// La distorsión del fondo la sigue aportando BlackHoleLensSystem, que
    /// llama a DrawCoreVisuals ENCIMA del pase de lente (mismo contrato de
    /// batch que StylizedVoidRenderer: al terminar el batch queda CERRADO).
    ///
    /// Calibración de textura: la sombra vive a R_SH=150 px de un lienzo
    /// 2048×1024 → el quad completo mide (2048, 1024)·(R/150) y la sombra
    /// cae exactamente en R píxeles de mundo.
    /// </summary>
    public static class GargantuaRenderer
    {
        /// <summary>Semieje de la sombra en píxeles de textura (calibración).</summary>
        private const float TexRSh = 150f;

        /// <summary>Lienzo de las texturas Gargantua (px).</summary>
        private const float TexW = 2048f, TexH = 1024f;

        private static Asset<Texture2D> _back;
        private static Asset<Texture2D> _front;
        private static Asset<Texture2D> _shadow;

        private static Texture2D Back
        {
            get
            {
                if (_back == null)
                    _back = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Procedural/GargantuaBack");
                return _back.Value;
            }
        }

        private static Texture2D Front
        {
            get
            {
                if (_front == null)
                    _front = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Procedural/GargantuaFront");
                return _front.Value;
            }
        }

        private static Texture2D Shadow
        {
            get
            {
                if (_shadow == null)
                    _shadow = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Procedural/GargantuaShadow");
                return _shadow.Value;
            }
        }

        /// <summary>Radio del horizonte en px (misma fórmula del proyectil).</summary>
        public static float GetHorizonPx(Projectile p)
        {
            return 0.3f * p.width * Math.Max(p.scale, 0.08f);
        }

        /// <summary>
        /// Dibuja el Gargantua completo. Contrato de batch idéntico al render
        /// anterior: si <paramref name="endActiveBatch"/> es true se cierra el
        /// batch activo antes; al terminar el batch queda CERRADO (el llamador
        /// lo restaura — PreDraw del mundo o el pase posterior a la lente).
        /// </summary>
        public static void Draw(Projectile p, bool endActiveBatch)
        {
            if (Main.netMode == Terraria.ID.NetmodeID.Server) return;

            try
            {
                Vector2 center = p.Center;
                float R = GetHorizonPx(p);
                if (R < 2f) return;

                float time = Main.GlobalTimeWrappedHourly;

                // BAMBOLEO (±1.1°) + RESPIRACIÓN (0.97..1.03): el fenómeno vivo.
                float sway = (float)Math.Sin(time * 0.33f + p.whoAmI * 0.7f) * 0.019f;
                float breathe = 1f + 0.03f * (float)Math.Sin(time * 1.1f + p.whoAmI);

                float scale = R / TexRSh * breathe;
                Vector2 backSize = new Vector2(TexW * scale, TexH * scale);
                Vector2 shadowSize = new Vector2(2f * R, 2f * R);
                Vector2 screen = Main.screenPosition;
                Vector2 cPos = center - screen;

                // ==============================================================
                //  PASO A — GARGANTUABACK (aditivo): todo lo que vive DETRÁS
                //  de la esfera: lente, anillo de fotones, neblina, wisps.
                // ==============================================================
                if (endActiveBatch)
                    Main.spriteBatch.End();

                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                Main.spriteBatch.Draw(Back, cPos, null, Color.White, sway,
                    Back.Size() * 0.5f, backSize / Back.Size(), SpriteEffects.None, 0f);

                Main.spriteBatch.End();

                // ==============================================================
                //  PASO B — LA SOMBRA (alpha): negro absoluto con borde NÍTIDO.
                //  Come el fondo del mundo y el brillo trasero: el vacío.
                // ==============================================================
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                Main.spriteBatch.Draw(Shadow, cPos, null, Color.White, 0f,
                    Shadow.Size() * 0.5f, shadowSize / Shadow.Size(), SpriteEffects.None, 0f);

                Main.spriteBatch.End();

                // ==============================================================
                //  PASO C — GARGANTUAFRONT (aditivo): la banda del disco que
                //  CRUZA por delante de la sombra (el plano ecuatorial).
                // ==============================================================
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                Main.spriteBatch.Draw(Front, cPos, null, Color.White, sway,
                    Front.Size() * 0.5f, backSize / Front.Size(), SpriteEffects.None, 0f);

                Main.spriteBatch.End();
            }
            catch
            {
                // Cierre defensivo SOLO en el path de error.
                try { Main.spriteBatch.End(); } catch { }
            }
        }
    }
}
