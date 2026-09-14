using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// RuneRingCrownRenderer — v6.22 — LA CORONA DE ANILLOS RÚNICOS.
    ///
    /// TRES ANILLOS RÚNICOS que RODEAN al jugador (petición expresa):
    /// cada aro es una elipse orbital inclinada en SU PROPIO plano con
    /// GIRO PROPIO (uno casi vertical, uno inclinado en contrarroto, uno
    /// ecuatorial) — la MISMA TÉCNICA de los anillos del Sol (aro de
    /// cápsulas con profundidad + glifos cabalgando la órbita rotados a
    /// la TANGENTE con perlas y latidos), puesta sobre el cuerpo.
    ///
    /// Uso (biblioteca): VFXCore.Begin() → ComputeQuads(...) →
    /// VFXCore.AppendToPlayerDraw(...).
    /// </summary>
    public static class RuneRingCrownRenderer
    {
        /// <summary>Número de anillos de la corona.</summary>
        public const int RingCount = 3;

        // --- LA TABLA DE GLIFOS (la firma angular de la casa) ---
        private static readonly Vector2[][] _runes = new Vector2[][]
        {
            // R0 — EL ASTRO (el punto de luz con rayos)
            new Vector2[] { new(0f, -4.5f), new(0f, 4.5f), new(-4.5f, 0f), new(4.5f, 0f), new(-3f, -3f), new(-1.2f, -1.2f), new(3f, -3f), new(1.2f, -1.2f), new(-3f, 3f), new(-1.2f, 1.2f), new(3f, 3f), new(1.2f, 1.2f) },
            // R1 — LA RUEDA SOLAR
            new Vector2[] { new(0f, -5f), new(0f, 5f), new(-5f, 0f), new(5f, 0f), new(-3.5f, -3.5f), new(3.5f, 3.5f), new(3.5f, -3.5f), new(-3.5f, 3.5f), new(-2.2f, 0f), new(2.2f, 0f), new(0f, -2.2f), new(0f, 2.2f) },
            // R2 — EL SIGILO (el sello maestro)
            new Vector2[] { new(0f, -6.5f), new(-4f, 0f), new(-4f, 0f), new(0f, 6.5f), new(0f, 6.5f), new(4f, 0f), new(4f, 0f), new(0f, -6.5f), new(-2.2f, 0f), new(2.2f, 0f), new(0f, -4f), new(0f, 4f) },
            // R3 — LA ESPIGA DE LUZ
            new Vector2[] { new(0f, -7f), new(0f, 7f), new(-3.2f, -3.5f), new(0f, -0.5f), new(3.2f, -3.5f), new(0f, -0.5f), new(-3.2f, 3.5f), new(0f, 0.5f), new(3.2f, 3.5f), new(0f, 0.5f) },
            // R4 — EL TRAZO DEL COMETA
            new Vector2[] { new(-4f, 6.5f), new(3f, -1f), new(3f, -1f), new(0f, -6.5f), new(0f, -6.5f), new(4f, -3f), new(1.5f, 2f), new(4.5f, 1.5f), new(-1.5f, 1f), new(1.5f, 4f) },
        };

        // --- LA GEOMETRÍA DE CADA ARO (índice k) ---
        //  (semiejes ×escala, inclinación, velocidad de giro, runas, colores)
        private static readonly (float a, float b, float tilt, float spin, int runes,
            Color body, Color tip)[] _rings = new (float, float, float, float, int, Color, Color)[]
        {
            // ARO 0 — el DEL PECHO: casi vertical, dorado, giro CW.
            (0.86f, 0.86f, 0.10f, 0.55f, 8, new Color(255, 190, 80), new Color(255, 240, 185)),
            // ARO 1 — el INCLINADO: contrarroto, blanco-estelar.
            (1.02f, 0.62f, -0.62f, -0.42f, 10, new Color(255, 245, 220), new Color(255, 252, 240)),
            // ARO 2 — el ECUATORIAL: la cintura de Saturno, azul-estelar.
            (1.24f, 0.38f, 0.20f, 0.30f, 12, new Color(135, 165, 255), new Color(215, 230, 255)),
        };

        /// <summary>
        /// Calcula los TRES anillos como cuadros de luz en el buffer de
        /// VFXCore (coordenadas de MUNDO).
        /// </summary>
        /// <param name="center">Centro del cuerpo del jugador (mundo).</param>
        /// <param name="scale">Escala del conjunto (humano ≈ 1).</param>
        /// <param name="time">Tiempo animado (GlobalTimeWrappedHourly).</param>
        /// <param name="alpha">Multiplicador global (0..1).</param>
        public static void ComputeQuads(Vector2 center, float scale, float time, float alpha = 1f)
        {
            if (scale <= 0.05f || alpha <= 0.02f) return;

            // EL LATIDO del conjunto: los tres aros respiran juntos.
            float breathe = 1f + 0.035f * (float)Math.Sin(time * 1.15f);

            for (int k = 0; k < RingCount; k++)
            {
                var R = _rings[k];
                float a = R.a * 30f * scale * breathe;
                float b = R.b * 30f * scale * breathe;
                float tilt = R.tilt + 0.07f * (float)Math.Sin(time * 0.8f + k * 1.9f);
                float spin = time * R.spin + k * 1.3f;

                // --- 1. EL ARO: polilínea de cápsulas con PROFUNDIDAD ---
                const int Segments = 26;
                Vector2 prev = EllipsePoint(center, a, b, tilt, spin);
                for (int s = 1; s <= Segments; s++)
                {
                    float t = spin + s / (float)Segments * MathHelper.TwoPi;
                    Vector2 pt = EllipsePoint(center, a, b, tilt, t);
                    Vector2 mid = (prev + pt) * 0.5f;
                    Vector2 delta = pt - prev;
                    float len = delta.Length();
                    if (len > 0.5f)
                    {
                        float rot = (float)Math.Atan2(delta.Y, delta.X);
                        float depth = 0.55f + 0.45f *
                            (float)Math.Sin(t + MathHelper.PiOver2) * (float)Math.Cos(tilt);
                        float pulse = 0.72f + 0.28f * (float)Math.Sin(time * 1.8f + k * 1.3f + s * 0.40f);
                        Capsule(mid, len, Math.Max(2.0f, 0.075f * 30f * scale) * (1f + 0.30f * depth),
                            rot, Tint(R.body, (0.30f + 0.30f * depth) * pulse * alpha));
                    }
                    prev = pt;
                }

                // --- 2. LOS GLIFOS cabalgando la órbita (a la tangente) ---
                float glyphScale = Math.Max(scale * 0.92f, 0.30f);
                for (int g = 0; g < R.runes; g++)
                {
                    float ang = g / (float)R.runes * MathHelper.TwoPi + spin;
                    float floatR = 1f + 0.05f * (float)Math.Sin(time * 1.35f + g * 0.9f + k * 0.6f);
                    Vector2 glyphPos = EllipsePoint(center, a * floatR, b * floatR, tilt, ang);
                    float tanAng = TangentialAngle(a * floatR, b * floatR, tilt, ang);
                    float glyphRot = tanAng + MathHelper.PiOver2;
                    float pulse = 0.75f + 0.25f * (float)Math.Sin(time * 2.4f + g * 1.3f + k * 0.8f);

                    // Resplandor suave detrás del glifo.
                    VFXCore.Quad(glyphPos, R.body * (0.20f * pulse * alpha),
                        new Vector2(26f * glyphScale, 26f * glyphScale));

                    // Los TRAZOS del glifo.
                    Vector2[] strokes = _runes[(g + k) % _runes.Length];
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
                        Color col = Color.Lerp(R.tip, R.body, 0.35f);
                        Capsule(mid, len, 2.6f * glyphScale, rot, Tint(col, 0.85f * pulse * alpha));
                    }

                    // LA PERLA sobre el glifo.
                    Vector2 pearlPos = glyphPos + new Vector2(0f, -8.5f * glyphScale).RotatedBy(glyphRot);
                    VFXCore.Quad(pearlPos, R.body * (0.55f * pulse * alpha),
                        new Vector2(5.6f * glyphScale, 5.6f * glyphScale));
                    VFXCore.Quad(pearlPos, R.tip * (0.9f * pulse * alpha),
                        new Vector2(2.6f * glyphScale, 2.6f * glyphScale));
                }
            }

            // --- 3. EL CORAZÓN: un brillo tenue en el centro del pecho
            //         (cohesión del conjunto — te rodea UNA corona). ---
            float heart = 0.10f + 0.05f * (float)Math.Sin(time * 1.6f);
            VFXCore.Quad(center, new Color(255, 225, 170) * (heart * alpha),
                new Vector2(34f * scale, 34f * scale));
        }

        /// <summary>Posición mundial del glifo g del aro k (para chispas).</summary>
        public static Vector2 GetGlyphPosition(Vector2 center, float scale, float time, int k, int g)
        {
            var R = _rings[k % RingCount];
            float a = R.a * 30f * scale;
            float b = R.b * 30f * scale;
            float tilt = R.tilt + 0.07f * (float)Math.Sin(time * 0.8f + k * 1.9f);
            float spin = time * R.spin + k * 1.3f;
            float ang = g / (float)R.runes * MathHelper.TwoPi + spin;
            float floatR = 1f + 0.05f * (float)Math.Sin(time * 1.35f + g * 0.9f + k * 0.6f);
            return EllipsePoint(center, a * floatR, b * floatR, tilt, ang);
        }

        /// <summary>Las runas por aro (para las chispas del CosmeticPlayer).</summary>
        public static int RunesOf(int k) => _rings[k % RingCount].runes;

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
