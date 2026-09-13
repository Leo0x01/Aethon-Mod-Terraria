using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// OlvidoBlackHoleRenderer — v6.14 — EL AGUJERO NEGRO DEL OLVIDO.
    ///
    /// PETICIÓN DEL USUARIO: "crea un 4to agujero negro que sea 100% EXACTO
    /// a la referencia… asegúrate de que sea 100% exacto, usa todas las
    /// técnicas que sean necesarias". La referencia:
    /// Ancients Awakened — Regicide, "Oblivion, God of the Void" (Reddit).
    ///
    /// LA TÉCNICA NUEVA (tras 10 rondas de validación VLM + 130 rondas de
    /// optimización automatizada contra la referencia, EMA 16.4/255):
    /// en vez de RECREAR el vórtice con cápsulas gaussians calibradas (los
    /// intentos v6.09-v6.13), el arte se EXTRAE DIRECTO de los píxeles de
    /// la propia referencia y se descompone en capas animables:
    ///
    ///   · OlvidoBackplate.png — el vacío rojizo que envuelve al agujero en
    ///     la referencia (la placa oscura que reproduce su ambientación).
    ///   · OlvidoHalo.png     — el resplandor ambiental + el BLOOM del
    ///     anillo (plasma brillante difuminado y horneado).
    ///   · OlvidoVortex.png   — EL ARTE EXACTO: anillo de fotones + disco +
    ///     brazos espirales + aguja + velos, separado del personaje con
    ///     máscaras estructurales (color + conectividad + zonas ancla) y
    ///     con inpainting angular donde el boss tapaba el plasma.
    ///   · OlvidoSphere.png   — la esfera de NEGRO PROFUNDO con la ESTRELLA
    ///     rosa y el RAYO púrpura del interior, tal cual en la referencia.
    ///   · OlvidoWisps.png    — los velos exteriores tenues (rotan lento).
    ///
    /// Formato de texturas: PREMULTIPLICADO con alfa 255 en las aditivas
    /// (el RGB lleva la cobertura horneada → el blending aditivo de tML
    /// queda LINEAL, sin el alfa² del v6.10 — misma lección del
    /// OblivionBlob v6.11).
    ///
    /// LO VIVO (sin tocar el arte exacto): respiración sutil de escala y
    /// alfa, halo pulsante, velos en rotación lenta, PULSOS DE FOTONES que
    /// recorren el anillo, LLAMARADA del hotspot cada 4.2 s y CHISPAS que
    /// caen en espiral hacia el horizonte.
    ///
    /// CONTRATO DE BATCH (v6.10, a prueba de balas): Draw() exige el
    /// SpriteBatch CERRADO y lo deja CERRADO.
    /// </summary>
    public static class OlvidoBlackHoleRenderer
    {
        // ==================================================================
        //  PARÁMETROS (medidos/optimizados contra la referencia)
        // ==================================================================

        /// <summary>Radio de la esfera negra en px a escala 1 — GIGANTE.</summary>
        public const float SpherePx = 52f;

        /// <summary>Alcance del arte extraído: 7.8·R desde el centro.</summary>
        private const float Coverage = 7.8f;

        /// <summary>Velocidad de rotación de los velos exteriores (rad/s).</summary>
        private const float WispSpeed = 0.03f;

        /// <summary>Elipse del anillo de fotones (medida: a=1.70R b=1.64R, casi circular).</summary>
        private const float RingA = 1.70f;
        private const float RingB = 1.64f;
        private const float RingTilt = 2.2f;      // 126° medidos

        /// <summary>Ángulo del hotspot del anillo (el sector más brillante).</summary>
        private const float HotspotDeg = 125f;

        // ==================================================================
        //  TEXTURAS
        // ==================================================================

        private static Asset<Texture2D> _vortex;
        private static Asset<Texture2D> _halo;
        private static Asset<Texture2D> _sphere;
        private static Asset<Texture2D> _backplate;
        private static Asset<Texture2D> _wisps;
        private static Asset<Texture2D> _blob;

        private static Texture2D Vortex =>
            (_vortex ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/OlvidoVortex")).Value;

        private static Texture2D Halo =>
            (_halo ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/OlvidoHalo")).Value;

        private static Texture2D Sphere =>
            (_sphere ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/OlvidoSphere")).Value;

        private static Texture2D Backplate =>
            (_backplate ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/OlvidoBackplate")).Value;

        private static Texture2D Wisps =>
            (_wisps ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/OlvidoWisps")).Value;

        /// <summary>El blob gaussiano (pulsos de fotones / llamaradas / chispas).</summary>
        private static Texture2D Blob =>
            (_blob ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/OblivionBlob")).Value;

        // ==================================================================
        //  HELPERS
        // ==================================================================

        private static void BeginAdditive()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
        }

        private static void BeginAlpha()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>Quad centrado (total = size), rotación opcional.</summary>
        private static void Quad(Texture2D tex, Vector2 pos, float size, float rot, Color tint)
        {
            Main.spriteBatch.Draw(tex, pos, null, tint, rot,
                new Vector2(tex.Width, tex.Height) * 0.5f,
                new Vector2(size, size) / new Vector2(tex.Width, tex.Height),
                SpriteEffects.None, 0f);
        }

        /// <summary>Posición sobre la elipse del anillo de fotones.</summary>
        private static Vector2 RingPos(Vector2 center, float r, float thetaDeg)
        {
            float th = MathHelper.ToRadians(thetaDeg);
            float x = (float)Math.Cos(th) * RingA * r;
            float y = (float)Math.Sin(th) * RingB * r;
            float ca = (float)Math.Cos(RingTilt), sa = (float)Math.Sin(RingTilt);
            return center + new Vector2(x * ca - y * sa, x * sa + y * ca);
        }

        /// <summary>Hash determinista [0,1).</summary>
        private static float Hash01(int seed, int a)
        {
            int h = unchecked(seed * 374761393 + a * 668265263);
            h ^= h >> 13;
            h = unchecked(h * 1274126177);
            h ^= h >> 16;
            return (h & 0xFFFFFF) / 16777216f;
        }

        // ==================================================================
        //  EL RENDER COMPLETO — contrato: batch CERRADO → CERRADO
        // ==================================================================

        public static void Draw(Vector2 center, float scale, float time, int seed)
        {
            float r = SpherePx * Math.Max(scale, 0.02f);
            if (r < 2f) return;   // el batch queda INTACTO (cerrado)

            try
            {
                // ============ 1. BACKPLATE — el vacío rojizo (alfa) ============
                // La referencia vive sobre un fondo rojo-negro profundo; esta
                // placa reproduce esa ambientación sobre el mundo del juego
                // (de día el plasma no se lava: el vacío lo abraza).
                BeginAlpha();
                float breathe = 1f + 0.006f * (float)Math.Sin(time * 0.9f);
                Quad(Backplate, center, Coverage * 2f * r * breathe, 0f, Color.White);
                Main.spriteBatch.End();

                // ============ 2. CAPAS ADITIVAS: halo + velos + vortex ============
                BeginAdditive();

                // 2.1 Halo ambiental + bloom del anillo (pulsando suavemente)
                float haloPulse = 0.88f + 0.12f * (float)Math.Sin(time * 2.2f);
                Quad(Halo, center, Coverage * 2f * r * 0.92f, 0f,
                    new Color(255, 255, 255) * haloPulse);

                // 2.2 Velos exteriores girando lento (el vórtice "respira" movimiento)
                Quad(Wisps, center, Coverage * 2f * r, time * WispSpeed,
                    new Color(255, 255, 255, 205));

                // 2.3 EL ARTE EXACTO — anillo + disco + brazos + aguja
                //     (respiración imperceptible: ±0.6% escala, ±4% alfa)
                float vAlpha = 0.96f + 0.04f * (float)Math.Sin(time * 1.3f);
                Quad(Vortex, center, Coverage * 2f * r * breathe, 0f,
                    new Color(255, 255, 255) * vAlpha);

                Main.spriteBatch.End();

                // ============ 3. LA ESFERA — negro profundo + estrella + rayo ============
                BeginAlpha();
                Quad(Sphere, center, 2.1f * r, 0f, Color.White);
                Main.spriteBatch.End();

                // ============ 4. OVERLAYS VIVOS (aditivos, sutiles) ============
                // No alteran el arte exacto: lo hacen VIVIR.
                BeginAdditive();

                // 4.1 PULSOS DE FOTONES — dos destellos que corren el anillo
                //     a 2.4× la velocidad de los velos (luz orbitando).
                for (int i = 0; i < 2; i++)
                {
                    float ang = time * 72f + i * 180f;   // deg/s
                    Vector2 pos = RingPos(center, r, ang);
                    float twinkle = 0.65f + 0.35f * (float)Math.Sin(time * 9f + i * 2.1f);
                    BlobQuad(pos, 1.35f * r, 0f,
                        new Color(255, 225, 240) * (0.55f * twinkle));
                }

                // 4.2 LLAMARADA DEL HOTSPOT — el sector más caliente del anillo
                //     estalla cada 4.2 s y se apaga como una promesa.
                const float FlareCycle = 4.2f;
                float phase = (time % FlareCycle) / FlareCycle;
                float decay = (float)Math.Exp(-phase * 6.5f) * Math.Min(phase * 12f, 1f);
                if (decay > 0.02f)
                {
                    Vector2 hpos = RingPos(center, r, HotspotDeg);
                    BlobQuad(hpos, 2.6f * r, 0f, new Color(255, 240, 220) * (0.5f * decay));
                    BlobQuad(hpos, 4.4f * r, 0f, new Color(255, 130, 170) * (0.22f * decay));
                }

                // 4.3 CHISPAS QUE CAEN — materia deslizándose por los brazos
                //     hacia el horizonte (nacen lejos, aceleran, se apagan).
                for (int i = 0; i < 6; i++)
                {
                    float life = (time * 0.22f + Hash01(seed, i * 31)) % 1f;
                    float baseAng = 30f + Hash01(seed, i * 77) * 360f;
                    float ang = baseAng + life * 190f;
                    float rad = (6.9f * (1f - life * life) + 1.15f) * r;
                    Vector2 pos = RingPos(center, 1f, 0f); // placeholder, se recalcula
                    float th = MathHelper.ToRadians(ang);
                    float ca = (float)Math.Cos(RingTilt), sa = (float)Math.Sin(RingTilt);
                    float x = (float)Math.Cos(th) * rad, y = (float)Math.Sin(th) * rad * 0.9f;
                    pos = center + new Vector2(x * ca - y * sa, x * sa + y * ca);
                    float fade = (float)Math.Sin(life * MathHelper.Pi);
                    float warm = Hash01(seed, i * 13 + 5);
                    Color c = warm < 0.5f
                        ? new Color(255, 210, 235)
                        : new Color(255, 120, 160);
                    BlobQuad(pos, 0.55f * r, 0f, c * (0.6f * fade));
                }

                Main.spriteBatch.End();
                // El batch queda CERRADO (contrato).
            }
            catch
            {
                // Cierre defensivo SOLO en el path de error (v6.10).
                try { Main.spriteBatch.End(); } catch { }
            }
        }

        /// <summary>Blob gaussiano aditivo (tamaño total = size, núcleo ≈ size/3).</summary>
        private static void BlobQuad(Vector2 pos, float size, float rot, Color tint)
        {
            if (tint.A <= 2) return;
            Texture2D tex = Blob;
            Main.spriteBatch.Draw(tex, pos, null, tint, rot,
                new Vector2(tex.Width, tex.Height) * 0.5f,
                new Vector2(size, size) / new Vector2(tex.Width, tex.Height),
                SpriteEffects.None, 0f);
        }
    }
}
