using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// RunicHaloRenderer — v6.23 — LOS ANILLOS DE VUELO DEL SOL IV.
    ///
    /// Petición original v6.22: "un anillo rúnico en la espalda que
    /// funcione como alas y halo; cuando el jugador va a volar, este
    /// anillo brilla con intensidad".
    ///
    /// v6.23 — LA ORDEN DEL USUARIO: "que los anillos de vuelos sean LOS
    /// ANILLOS DEL SOL NÚMERO 4, además el anillo es muy brillante —
    /// reduce el brillo a como se ve en los soles".
    ///
    /// EL SISTEMA ORBITAL DEL SOL RÚNICO IV, LITERAL: los CUATRO anillos
    /// del Sol Rúnico IV (la MISMA geometría de RuneSunRenderer, tier 4)
    /// orbitando la ESPALDA del jugador:
    ///   · cada anillo en SU PROPIO PLANO (semiejes 1.62+0.44k ×R,
    ///     achatado 0.34..0.48, inclinación −0.55+0.20k),
    ///   · GIRO ALTERNO (par horario, impar antihorario),
    ///   · 6/8/10/12 glifos cabalgando la órbita rotados a la TANGENTE,
    ///     con perlas y latidos — LA MISMA TÉCNICA, al brillo EXACTO de
    ///     los soles (los alphas del DrawRingSystem del sol, sin el
    ///     bloom gigante de v6.22).
    ///
    /// LA INTENSIDAD VIVE (acotada al nivel solar): `flight` (0..1) es
    /// la energía de vuelo — al VOLAR el sistema SE AVIVA: los anillos
    /// giran más rápido, las runas arden hacia el blanco y el corazón
    /// crece — pero NUNCA por encima del lenguaje de los soles: un
    /// multiplicador 0.85..1.20, cero bloom cegador.
    /// </summary>
    public static class RunicHaloRenderer
    {
        // --- LA GEOMETRÍA DEL SOL IV (los valores LITERALES del sol) ---
        private const float RingA0 = 1.62f;      // semieje mayor del 1er anillo (×R)
        private const float RingAStep = 0.44f;   // separación entre anillos (×R)
        private const float RingSpin0 = 0.26f;   // giro base (rad/s)
        private const float RingSpinStep = 0.045f;
        private const int Runes0 = 6;            // glifos del 1er anillo
        private const int RuneStep = 2;          // +2 glifos por anillo

        /// <summary>Los CUATRO anillos del Sol Rúnico IV.</summary>
        public const int RingCount = 4;

        /// <summary>La base del sistema (px) — el anillo interior abraza
        /// la espalda y el IV (2.94×R) es el gran halo.</summary>
        private const float BaseR = 13f;

        // --- LA PALETA (los oros del sol — concordancia total) ---
        private static readonly Color RuneGold = new(255, 190, 80);    // cuerpo de runa dorada
        private static readonly Color RuneGoldTip = new(255, 240, 185);// punta pálida
        private static readonly Color WhiteIncan = new(255, 250, 235); // blanco-incandescente

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
            // R6 — EL TRAZO DEL COMETA
            new Vector2[] { new(-4f, 6.5f), new(3f, -1f), new(3f, -1f), new(0f, -6.5f), new(0f, -6.5f), new(4f, -3f), new(1.5f, 2f), new(4.5f, 1.5f), new(-1.5f, 1f), new(1.5f, 4f) },
            // R7 — EL SIGILO SOLAR (el sello maestro)
            new Vector2[] { new(0f, -6.5f), new(-4f, 0f), new(-4f, 0f), new(0f, 6.5f), new(0f, 6.5f), new(4f, 0f), new(4f, 0f), new(0f, -6.5f), new(-2.2f, 0f), new(2.2f, 0f), new(0f, -4f), new(0f, 4f) },
        };

        /// <summary>Semieje mayor del anillo k (×R) — LITERAL del sol.</summary>
        private static float RingA(int k) => RingA0 + RingAStep * k;

        /// <summary>Achatado del anillo k (plano distinto por anillo).</summary>
        private static float RingFlat(int k) => 0.34f + 0.07f * (k % 3);

        /// <summary>Inclinación del plano del anillo k (LITERAL del sol).</summary>
        private static float RingTilt(int k) => -0.55f + 0.20f * k;

        /// <summary>
        /// El GIRO del anillo k: ALTERNO (par horario, impar antihorario)
        /// — la firma del sol. Al volar TODO el sistema se aviva (×1..1.5).
        /// </summary>
        private static float RingSpin(int k, float flight)
            => (k % 2 == 0 ? 1f : -1f) * (RingSpin0 + RingSpinStep * k) * (1f + 0.5f * flight);

        /// <summary>Las runas del anillo k — LITERAL del sol (6/8/10/12).</summary>
        public static int RunesOf(int k) => Runes0 + RuneStep * k;

        // ==================================================================
        //  EL RENDER — el sistema orbital al buffer de VFXCore
        // ==================================================================

        /// <summary>
        /// Calcula LOS CUATRO ANILLOS DEL SOL IV como cuadros de luz en el
        /// buffer de VFXCore (coordenadas de MUNDO).
        /// </summary>
        /// <param name="back">Ancla: la ESPALDA ALTA del jugador (omóplatos).</param>
        /// <param name="scale">Escala (humano ≈ 1).</param>
        /// <param name="time">Tiempo animado.</param>
        /// <param name="flight">LA ENERGÍA DE VUELO (0..1) — el avivo.</param>
        /// <param name="alpha">Multiplicador global (luz del mundo).</param>
        public static void ComputeQuads(Vector2 back, float scale, float time,
            float flight, float alpha = 1f)
        {
            if (scale <= 0.05f || alpha <= 0.02f) return;

            // LA INTENSIDAD ACOTADA AL LENGUAJE SOLAR: quieto un pelo por
            // debajo del sol; volando un pelo por encima — NUNCA el bloom
            // cegador de v6.22 (el usuario: "reduce el brillo a como se
            // ve en los soles").
            float glow = 0.85f + 0.35f * flight;

            float R = BaseR * scale;
            float glyphScale = Math.Max(R / 52f, 0.25f) * 1.45f;

            // === EL CORAZÓN: un avivo pequeño y contenido (nada del bloom
            //     ×2.1 de v6.22 — un latido solar discreto en la espalda) ===
            float heart = (0.10f + 0.22f * flight) * alpha * glow;
            VFXCore.Quad(back, Tint(RuneGold, heart), new Vector2(22f * scale, 22f * scale));

            // === LOS CUATRO ANILLOS (la geometría LITERAL del Sol IV) ===
            for (int k = 0; k < RingCount; k++)
            {
                float a = RingA(k) * R;
                float b = a * RingFlat(k);
                float tilt = RingTilt(k);
                float spin = time * RingSpin(k, flight);
                int runeCount = RunesOf(k);

                // --- 1. EL ARO ELÍPTICO: polilínea de cápsulas con
                //     PROFUNDIDAD (el frente más brillante) — el alpha
                //     EXACTO del sol: (0.30+0.30·depth)·pulse ---
                const int Segments = 30;
                Vector2 prev = EllipsePoint(back, a, b, tilt, spin);
                for (int s = 1; s <= Segments; s++)
                {
                    float t = spin + s / (float)Segments * MathHelper.TwoPi;
                    Vector2 pt = EllipsePoint(back, a, b, tilt, t);
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
                        float pulse = 0.70f + 0.30f * (float)Math.Sin(time * 1.8f + k * 1.3f + s * 0.35f);
                        Capsule(mid, len, Math.Max(2.2f, 0.052f * R) * (1f + 0.35f * depth),
                            rot, Tint(RuneGold, (0.30f + 0.30f * depth) * pulse * glow * alpha));
                    }
                    prev = pt;
                }

                // --- 2. LAS RUNAS: glifos cabalgando la órbita, rotados a
                //     la TANGENTE, con perlas y latidos (el alpha solar) ---
                for (int g = 0; g < runeCount; g++)
                {
                    float ang = g / (float)runeCount * MathHelper.TwoPi + spin;
                    float breathe = 1f + 0.045f * (float)Math.Sin(time * 1.35f + g * 0.9f + k * 0.5f);
                    Vector2 glyphPos = EllipsePoint(back, a * breathe, b * breathe, tilt, ang);

                    float tanAng = TangentialAngle(a * breathe, b * breathe, tilt, ang);
                    float glyphRot = tanAng + MathHelper.PiOver2; // la runa "de pie" sobre el aro

                    float pulse = 0.75f + 0.25f * (float)Math.Sin(time * 2.4f + g * 1.3f + k * 0.8f);

                    // Resplandor suave DETRÁS (el grabado ardiendo — 0.20 solar).
                    VFXCore.Quad(glyphPos, Tint(RuneGold, 0.20f * pulse * glow * alpha),
                        new Vector2(34f * glyphScale, 34f * glyphScale));

                    // Los TRAZOS del glifo (al volar arden al BLANCO — sutil).
                    Vector2[] strokes = _runes[(g + k) % _runes.Length];
                    Color col = Color.Lerp(RuneGoldTip, WhiteIncan, flight * 0.45f);
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
                        Color strokeCol = Color.Lerp(col, RuneGold, 1f - localY * 0.25f);

                        Capsule(mid, len, 3.3f * glyphScale, rot,
                            Tint(strokeCol, 0.85f * pulse * glow * alpha));
                    }

                    // PERLA sobre el glifo (la gema del sello — alphas solares).
                    Vector2 pearlPos = glyphPos + new Vector2(0f, -11.5f * glyphScale).RotatedBy(glyphRot);
                    VFXCore.Quad(pearlPos, Tint(RuneGold, 0.60f * pulse * glow * alpha),
                        new Vector2(7.0f * glyphScale, 7.0f * glyphScale));
                    VFXCore.Quad(pearlPos, Tint(RuneGoldTip, 0.9f * pulse * glow * alpha),
                        new Vector2(3.2f * glyphScale, 3.2f * glyphScale));
                }
            }
        }

        /// <summary>Posición mundial del glifo g del sistema (las chispas).</summary>
        public static Vector2 GetGlyphPosition(Vector2 back, float scale, float time, int g)
        {
            // Mapa acumulativo: anillo 0 (6) · 1 (8) · 2 (10) · 3 (12).
            int k = 0, idx = g;
            for (int i = 0; i < RingCount && idx >= RunesOf(i); i++)
            {
                idx -= RunesOf(i);
                k++;
            }
            k %= RingCount;

            float R = BaseR * scale;
            float a = RingA(k) * R;
            float b = a * RingFlat(k);
            float tilt = RingTilt(k);
            float spin = time * RingSpin(k, 0f);
            float ang = idx / (float)RunesOf(k) * MathHelper.TwoPi + spin;
            float breathe = 1f + 0.045f * (float)Math.Sin(time * 1.35f + idx * 0.9f + k * 0.5f);
            return EllipsePoint(back, a * breathe, b * breathe, tilt, ang);
        }

        /// <summary>Glifos totales del sistema (las chispas de vuelo).</summary>
        public static int Glyphs
        {
            get
            {
                int total = 0;
                for (int k = 0; k < RingCount; k++) total += RunesOf(k);
                return total;
            }
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
