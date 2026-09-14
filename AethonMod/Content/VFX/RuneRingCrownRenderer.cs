using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// RuneRingCrownRenderer — v6.25 — LA CORONA DE ANILLOS RÚNICOS:
    /// LA AUREOLA DEL SOL I SOBRE LA CABEZA.
    ///
    /// v6.22: "una corona de anillos rúnicos que rodee al jugador" (tres
    /// aros propios orbitando el CUERPO).
    /// v6.23: "que SEA la del sol número 1" (el anillo LITERAL del Sol I,
    /// todavía alrededor del torso).
    /// v6.25 — LA ORDEN DEL USUARIO (la corrección del destinatario): "la
    /// que tenías que cambiar era la CORONA DE ANILLOS RÚNICOS — debe
    /// estar en la CABEZA del jugador como una AUREOLA, igual al anillo
    /// del sol rúnico 1, sin brillar de más".
    ///
    /// EL ANILLO DEL SOL RÚNICO I, LITERAL, RINGIENDO LA CABEZA: el mismo
    /// sistema de RuneSunRenderer tier 1, anclado a la CABEZA como una
    /// aureola de verdad:
    ///   · el aro elíptico de cápsulas con PROFUNDIDAD (semiejes
    ///     1.62×R / 0.34×R, inclinación −0.55 — el plano del Sol I),
    ///   · giro CW 0.26 rad/s (el paso del sol),
    ///   · 6 glifos solares cabalgando la órbita rotados a la TANGENTE,
    ///     con perlas y latidos — LOS ALPHAS EXACTOS del sol: aro
    ///     (0.30+0.30·depth)·pulse, resplandor 0.20·pulse, trazos
    ///     0.85·pulse, perlas 0.60/0.90·pulse. Cero bloom añadido: se
    ///     lee AL MISMO BRILLO que en los soles.
    ///
    /// Uso (biblioteca): VFXCore.Begin() → ComputeQuads(...) →
    /// VFXCore.AppendToPlayerDraw(...) o VFXCore.FlushAdditive(...).
    /// </summary>
    public static class RuneRingCrownRenderer
    {
        // --- LA GEOMETRÍA DEL SOL I (los valores LITERALES del sol) ---
        private const float RingA0 = 1.62f;      // semieje mayor (×R)
        private const float RingFlat0 = 0.34f;   // achatado (el plano del Sol I)
        private const float RingTilt0 = -0.55f;  // inclinación (el plano del Sol I)
        private const float RingSpin0 = 0.26f;   // giro CW (rad/s — el paso del sol)

        /// <summary>La corona ES un solo anillo: el del Sol I.</summary>
        public const int RingCount = 1;

        /// <summary>Los glifos del anillo (LITERAL del Sol I: 6).</summary>
        public const int GlyphCount = 6;

        /// <summary>Las runas del anillo (LITERAL del Sol I: 6 glifos).</summary>
        public static int RunesOf(int k) => 6;

        /// <summary>
        /// La base del anillo (px): el radio de la CABEZA — la aureola
        /// ringea la cabeza (el semieje mayor queda a 1.62×12 ≈ 19 px).
        /// </summary>
        private const float BaseR = 12f;

        // --- LA PALETA (los oros del sol — concordancia total) ---
        private static readonly Color RuneGold = new(255, 190, 80);    // cuerpo de runa dorada
        private static readonly Color RuneGoldTip = new(255, 240, 185);// punta pálida

        // --- LA TABLA DE GLIFOS (la escritura solar de RuneSunRenderer) ---
        private static readonly Vector2[][] _runes = new Vector2[][]
        {
            // R0 — EL ASTRO (el punto de luz con rayos)
            new Vector2[] { new(0f, -4.5f), new(0f, 4.5f), new(-4.5f, 0f), new(4.5f, 0f), new(-3f, -3f), new(-1.2f, -1.2f), new(3f, -3f), new(1.2f, -1.2f), new(-3f, 3f), new(-1.2f, 1.2f), new(3f, 3f), new(1.2f, 1.2f) },
            // R1 — LA LLAMA VIVA
            new Vector2[] { new(0f, 6.5f), new(0f, 1f), new(0f, 1f), new(-3f, -2f), new(-3f, -2f), new(0f, -5f), new(0f, -5f), new(3f, -2f), new(3f, -2f), new(0f, 1f), new(-1.5f, -6.5f), new(1.5f, -6.5f) },
            // R2 — LA RUEDA SOLAR
            new Vector2[] { new(0f, -5f), new(0f, 5f), new(-5f, 0f), new(5f, 0f), new(-3.5f, -3.5f), new(3.5f, 3.5f), new(3.5f, -3.5f), new(-3.5f, 3.5f), new(-2.2f, 0f), new(2.2f, 0f), new(0f, -2.2f), new(0f, 2.2f) },
            // R3 — LA ESPIGA DE LUZ
            new Vector2[] { new(0f, -7f), new(0f, 7f), new(-3.2f, -3.5f), new(0f, -0.5f), new(3.2f, -3.5f), new(0f, -0.5f), new(-3.2f, 3.5f), new(0f, 0.5f), new(3.2f, 3.5f), new(0f, 0.5f) },
            // R4 — LA PUERTA DEL DÍA
            new Vector2[] { new(-3.5f, 7f), new(-3.5f, -5f), new(-3.5f, -5f), new(0f, -7f), new(0f, -7f), new(3.5f, -5f), new(3.5f, -5f), new(3.5f, 7f), new(-3.5f, 7f), new(3.5f, 7f), new(0f, -4f), new(0f, 7f) },
            // R5 — LA CORONA BAJA
            new Vector2[] { new(-4f, 5f), new(-4f, -2f), new(-4f, -2f), new(-1.5f, -5.5f), new(-1.5f, -5.5f), new(0f, -1.5f), new(0f, -1.5f), new(1.5f, -5.5f), new(1.5f, -5.5f), new(4f, -2f), new(4f, -2f), new(4f, 5f), new(-4f, 5f), new(4f, 5f) },
        };

        // ==================================================================
        //  EL RENDER — el anillo del Sol I ringiendo la CABEZA
        // ==================================================================

        /// <summary>
        /// Calcula LA AUREOLA (el anillo del Sol I) como cuadros de luz en
        /// el buffer de VFXCore (coordenadas de MUNDO).
        /// </summary>
        /// <param name="head">Centro de la cabeza que ringea la aureola.</param>
        /// <param name="scale">Escala del conjunto (humano ≈ 1).</param>
        /// <param name="time">Tiempo animado (Main.GlobalTimeWrappedHourly).</param>
        /// <param name="alpha">Multiplicador global de intensidad (0..1).</param>
        public static void ComputeQuads(Vector2 head, float scale, float time, float alpha = 1f)
        {
            if (scale <= 0.05f || alpha <= 0.02f) return;

            float R = BaseR * scale;
            float glyphScale = Math.Max(R / 52f, 0.25f) * 1.45f;

            // === EL ARO: el plano LITERAL del Sol I (1.62R × 0.34, inclinado
            //     −0.55, girando CW al paso del sol) ringiendo la cabeza ===
            float a = RingA0 * R;
            float b = a * RingFlat0;
            float tilt = RingTilt0;
            float spin = time * RingSpin0;
            int runeCount = GlyphCount;

            // --- 1. EL ARO ELÍPTICO: polilínea de cápsulas con PROFUNDIDAD
            //     (el alpha EXACTO del sol: (0.30+0.30·depth)·pulse) ---
            const int Segments = 30;
            Vector2 prev = EllipsePoint(head, a, b, tilt, spin);
            for (int s = 1; s <= Segments; s++)
            {
                float t = spin + s / (float)Segments * MathHelper.TwoPi;
                Vector2 pt = EllipsePoint(head, a, b, tilt, t);
                Vector2 mid = (prev + pt) * 0.5f;
                Vector2 delta = pt - prev;
                float len = delta.Length();
                if (len > 0.5f)
                {
                    float rot = (float)Math.Atan2(delta.Y, delta.X);
                    // Profundidad: sin(t)·cos(tilt) > 0 → frente de la órbita.
                    float depth = 0.55f + 0.45f *
                        (float)Math.Sin(t + MathHelper.PiOver2) * (float)Math.Cos(tilt);
                    // Latido del aro (la energía recorre el anillo).
                    float pulse = 0.70f + 0.30f * (float)Math.Sin(time * 1.8f + s * 0.35f);
                    Capsule(mid, len, Math.Max(2.2f, 0.052f * R) * (1f + 0.35f * depth),
                        rot, Tint(RuneGold, (0.30f + 0.30f * depth) * pulse * alpha));
                }
                prev = pt;
            }

            // --- 2. LAS RUNAS: glifos cabalgando la órbita, rotados a la
            //     TANGENTE, con perlas y latidos (el alpha solar) ---
            for (int g = 0; g < runeCount; g++)
            {
                float ang = g / (float)runeCount * MathHelper.TwoPi + spin;
                float breathe = 1f + 0.045f * (float)Math.Sin(time * 1.35f + g * 0.9f);
                Vector2 glyphPos = EllipsePoint(head, a * breathe, b * breathe, tilt, ang);

                float tanAng = TangentialAngle(a * breathe, b * breathe, tilt, ang);
                float glyphRot = tanAng + MathHelper.PiOver2; // la runa "de pie" sobre el aro

                float pulse = 0.75f + 0.25f * (float)Math.Sin(time * 2.4f + g * 1.3f);

                // Resplandor suave DETRÁS (el grabado ardiendo — 0.20 solar).
                VFXCore.Quad(glyphPos, Tint(RuneGold, 0.20f * pulse * alpha),
                    new Vector2(34f * glyphScale, 34f * glyphScale));

                // Los TRAZOS del glifo.
                Vector2[] strokes = _runes[g % _runes.Length];
                for (int s = 0; s < strokes.Length; s += 2)
                {
                    Vector2 rotA = strokes[s] * glyphScale;
                    Vector2 rotB = strokes[s + 1] * glyphScale;
                    rotA = rotA.RotatedBy(glyphRot) + glyphPos;
                    rotB = rotB.RotatedBy(glyphRot) + glyphPos;
                    Vector2 mid = (rotA + rotB) * 0.5f;
                    Vector2 delta = rotB - rotA;
                    float len = delta.Length();
                    if (len < 0.01f) continue;
                    float rot = (float)Math.Atan2(delta.Y, delta.X);

                    // Gradiente vertical local: abajo cuerpo, arriba punta pálida.
                    float localY = ((strokes[s].Y + strokes[s + 1].Y) * 0.5f + 7f) / 14f;
                    Color col = Color.Lerp(RuneGoldTip, RuneGold, 1f - localY * 0.25f);

                    Capsule(mid, len, 3.3f * glyphScale, rot, Tint(col, 0.85f * pulse * alpha));
                }

                // PERLA sobre el glifo (la gema del sello — alphas solares).
                Vector2 pearlPos = glyphPos + new Vector2(0f, -11.5f * glyphScale).RotatedBy(glyphRot);
                VFXCore.Quad(pearlPos, Tint(RuneGold, 0.60f * pulse * alpha),
                    new Vector2(7.0f * glyphScale, 7.0f * glyphScale));
                VFXCore.Quad(pearlPos, Tint(RuneGoldTip, 0.9f * pulse * alpha),
                    new Vector2(3.2f * glyphScale, 3.2f * glyphScale));
            }
        }

        /// <summary>Posición mundial del glifo g del anillo (las chispas).</summary>
        public static Vector2 GetGlyphPosition(Vector2 head, float scale, float time, int g)
        {
            float R = BaseR * scale;
            float a = RingA0 * R;
            float b = a * RingFlat0;
            float spin = time * RingSpin0;
            float ang = g / (float)GlyphCount * MathHelper.TwoPi + spin;
            float breathe = 1f + 0.045f * (float)Math.Sin(time * 1.35f + g * 0.9f);
            return EllipsePoint(head, a * breathe, b * breathe, RingTilt0, ang);
        }

        // ------------------------------------------------------------------
        //  HELPERS (patrón validado del proyecto)
        // ------------------------------------------------------------------

        private static void Capsule(Vector2 mid, float len, float width, float rot, Color tint)
        {
            if (tint.A == 0) return;
            VFXCore.Quad(mid, tint, new Vector2(len + width, width * 1.9f), rot);
        }

        private static Vector2 EllipsePoint(Vector2 center, float a, float b, float tilt, float t)
        {
            float ct = (float)Math.Cos(t), st = (float)Math.Sin(t);
            Vector2 local = new Vector2(a * ct, b * st);
            float cR = (float)Math.Cos(tilt), sR = (float)Math.Sin(tilt);
            return center + new Vector2(local.X * cR - local.Y * sR, local.X * sR + local.Y * cR);
        }

        private static float TangentialAngle(float a, float b, float tilt, float t)
        {
            Vector2 dLocal = new Vector2(-a * (float)Math.Sin(t), b * (float)Math.Cos(t));
            float cR = (float)Math.Cos(tilt), sR = (float)Math.Sin(tilt);
            Vector2 d = new Vector2(dLocal.X * cR - dLocal.Y * sR, dLocal.X * sR + dLocal.Y * cR);
            return (float)Math.Atan2(d.Y, d.X);
        }

        private static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color(c.R, c.G, c.B, (byte)(int)(255f * f));
        }
    }
}
