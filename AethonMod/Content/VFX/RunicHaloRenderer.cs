using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// RunicHaloRenderer — v6.22 — EL ANILLO RÚNICO DE LA ESPALDA.
    ///
    /// Petición del usuario: "un anillo rúnico en la espalda del jugador
    /// que funcione como ALAS y HALO en la espalda — cuando el jugador
    /// va a volar, este anillo rúnico BRILLA CON INTENSIDAD".
    ///
    /// UN GRAN ANILLO RÚNICO vertical tras la espalda (el halo) con su
    /// contraro interior + glifos cabalgando la órbita + el corazón de
    /// luz. LA INTENSIDAD VIVE: `flight` (0..1) es la energía de vuelo
    /// acumulada — al VOLAR el anillo SE ENCIENDE:
    ///   · el bloom del corazón crece ×2.2,
    ///   · nace la CRUZ DE LUZ (el destello de 4 puntas),
    ///   · los RAYOS radiales del halo se encienden (8 rayos),
    ///   · el aro exterior gana su DOBLE ancho caliente,
    ///   · y las runas arden al blanco.
    /// Quieto es un sello elegante; volando es un SOL en tu espalda.
    /// </summary>
    public static class RunicHaloRenderer
    {
        // --- LA TABLA DE GLIFOS (la firma angular de la casa) ---
        private static readonly Vector2[][] _runes = new Vector2[][]
        {
            new Vector2[] { new(0f, -4.5f), new(0f, 4.5f), new(-4.5f, 0f), new(4.5f, 0f), new(-3f, -3f), new(-1.2f, -1.2f), new(3f, -3f), new(1.2f, -1.2f), new(-3f, 3f), new(-1.2f, 1.2f), new(3f, 3f), new(1.2f, 1.2f) },
            new Vector2[] { new(0f, -5f), new(0f, 5f), new(-5f, 0f), new(5f, 0f), new(-3.5f, -3.5f), new(3.5f, 3.5f), new(3.5f, -3.5f), new(-3.5f, 3.5f), new(-2.2f, 0f), new(2.2f, 0f), new(0f, -2.2f), new(0f, 2.2f) },
            new Vector2[] { new(0f, -6.5f), new(-4f, 0f), new(-4f, 0f), new(0f, 6.5f), new(0f, 6.5f), new(4f, 0f), new(4f, 0f), new(0f, -6.5f), new(-2.2f, 0f), new(2.2f, 0f), new(0f, -4f), new(0f, 4f) },
            new Vector2[] { new(0f, -7f), new(0f, 7f), new(-3.2f, -3.5f), new(0f, -0.5f), new(3.2f, -3.5f), new(0f, -0.5f), new(-3.2f, 3.5f), new(0f, 0.5f), new(3.2f, 3.5f), new(0f, 0.5f) },
            new Vector2[] { new(-4f, 6.5f), new(3f, -1f), new(3f, -1f), new(0f, -6.5f), new(0f, -6.5f), new(4f, -3f), new(1.5f, 2f), new(4.5f, 1.5f), new(-1.5f, 1f), new(1.5f, 4f) },
            new Vector2[] { new(-3f, 7f), new(-3f, -2f), new(-3f, -2f), new(3f, -6f), new(3f, -6f), new(3f, 2f), new(3f, 2f), new(-2f, 6f) },
        };

        /// <summary>Glifos del aro principal.</summary>
        private const int RuneCount = 10;

        // --- LA PALETA (oro regio + blanco) ---
        private static readonly Color GoldBody = new(255, 190, 80);
        private static readonly Color GoldTip = new(255, 240, 185);
        private static readonly Color WhiteIncan = new(255, 250, 235);

        /// <summary>
        /// Calcula el ANILLO-HALO como cuadros de luz en el buffer de
        /// VFXCore (coordenadas de MUNDO).
        /// </summary>
        /// <param name="back">Ancla: la ESPALDA ALTA del jugador (omóplatos).</param>
        /// <param name="scale">Escala (humano ≈ 1).</param>
        /// <param name="time">Tiempo animado.</param>
        /// <param name="flight">LA ENERGÍA DE VUELO (0..1) — el brillo.</param>
        /// <param name="alpha">Multiplicador global (luz del mundo).</param>
        public static void ComputeQuads(Vector2 back, float scale, float time,
            float flight, float alpha = 1f)
        {
            if (scale <= 0.05f || alpha <= 0.02f) return;

            // LA INTENSIDAD: quieta = sello elegante; volando = SOL.
            float glow = 0.55f + 1.45f * flight;
            float breathe = 1f + (0.02f + 0.035f * flight) * (float)Math.Sin(time * (1.6f + 2.2f * flight));

            // --- EL CORAZÓN: el bloom central (el motor del halo) ---
            float coreSize = 30f * scale * breathe * (1f + 1.2f * flight);
            VFXCore.Quad(back, GoldBody * (0.14f * glow * alpha), new Vector2(coreSize * 2.1f, coreSize * 2.1f));
            VFXCore.Quad(back, WhiteIncan * (0.22f * glow * alpha), new Vector2(coreSize, coreSize));

            // --- LA CRUZ DE LUZ (solo volando: el destello de 4 puntas) ---
            if (flight > 0.12f)
            {
                float arm = 66f * scale * (0.7f + 0.5f * flight) * breathe;
                float crossA = 0.16f + 0.30f * flight;
                VFXCore.Quad(back, GoldBody * (crossA * alpha), new Vector2(arm, 3.2f * scale));
                VFXCore.Quad(back, GoldBody * (crossA * alpha), new Vector2(3.2f * scale, arm));
                VFXCore.Quad(back, WhiteIncan * (crossA * 0.8f * alpha), new Vector2(arm * 0.7f, 2.1f * scale));
                VFXCore.Quad(back, WhiteIncan * (crossA * 0.8f * alpha), new Vector2(2.1f * scale, arm * 0.7f));
            }

            // --- LOS RAYOS RADIALES del halo (8, encendiéndose al volar) ---
            if (flight > 0.05f)
            {
                for (int i = 0; i < 8; i++)
                {
                    float ang = i / 8f * MathHelper.TwoPi + time * 0.20f;
                    Vector2 dir = new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang));
                    float len = (20f + 26f * flight) * scale * (0.8f + 0.2f * (float)Math.Sin(time * 2.6f + i * 1.7f));
                    Vector2 mid = back + dir * (34f * scale + len * 0.5f);
                    // púa de luz radial (cápsula).
                    VFXCore.Quad(mid, Color.Lerp(GoldBody, WhiteIncan, 0.4f) *
                        ((0.20f + 0.26f * flight) * alpha),
                        new Vector2(len, Math.Max(2.2f, 3.4f * scale)), ang);
                }
            }

            // --- EL ARO PRINCIPAL: el gran anillo vertical (el halo) ---
            float a = 36f * scale * breathe;
            float b = 36f * scale * breathe;
            float tilt = 0.06f * (float)Math.Sin(time * 0.7f);   // el aro SE MECE vivo
            float spin = time * (0.42f + 0.30f * flight);        // gira más rápido al volar

            const int Segments = 34;
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
                    float depth = 0.55f + 0.45f * (float)Math.Sin(t + MathHelper.PiOver2);
                    float pulse = 0.74f + 0.26f * (float)Math.Sin(time * 2.1f + s * 0.37f);
                    // El DOBLE ancho caliente al volar.
                    float w = Math.Max(2.4f, 5.6f * scale) * (1f + 0.30f * depth + 0.55f * flight);
                    Capsule(mid, len, w, rot,
                        Tint(GoldBody, (0.30f + 0.28f * depth) * pulse * glow * 0.6f * alpha));
                    // la vena caliente interior.
                    Capsule(mid, len, w * 0.38f, rot,
                        Tint(GoldTip, (0.40f + 0.34f * depth) * pulse * (0.5f + 0.5f * flight) * alpha));
                }
                prev = pt;
            }

            // --- EL CONTRARO interior: fino, blanco-azulado, CCW ---
            float a2 = 28.5f * scale * breathe;
            float b2 = 28.5f * scale * breathe;
            float spin2 = -time * (0.30f + 0.22f * flight);
            const int Segs2 = 26;
            Vector2 prev2 = EllipsePoint(back, a2, b2, tilt, spin2);
            for (int s = 1; s <= Segs2; s++)
            {
                float t = spin2 + s / (float)Segs2 * MathHelper.TwoPi;
                Vector2 pt = EllipsePoint(back, a2, b2, tilt, t);
                Vector2 mid = (prev2 + pt) * 0.5f;
                Vector2 delta = pt - prev2;
                float len = delta.Length();
                if (len > 0.5f)
                {
                    float rot = (float)Math.Atan2(delta.Y, delta.X);
                    Capsule(mid, len, Math.Max(1.7f, 3.2f * scale), rot,
                        Tint(new Color(215, 230, 255), (0.26f + 0.22f * flight) * alpha));
                }
                prev2 = pt;
            }

            // --- LOS GLIFOS cabalgando el aro principal ---
            float glyphScale = Math.Max(scale * 0.88f, 0.28f);
            for (int g = 0; g < RuneCount; g++)
            {
                float ang = g / (float)RuneCount * MathHelper.TwoPi + spin;
                float floatR = 1f + 0.04f * (float)Math.Sin(time * 1.35f + g * 0.9f);
                Vector2 glyphPos = EllipsePoint(back, a * floatR, b * floatR, tilt, ang);
                float tanAng = TangentialAngle(a * floatR, b * floatR, tilt, ang);
                float glyphRot = tanAng + MathHelper.PiOver2;
                float pulse = 0.75f + 0.25f * (float)Math.Sin(time * 2.6f + g * 1.3f);

                // Resplandor del glifo (más grande al volar).
                VFXCore.Quad(glyphPos, GoldBody * ((0.16f + 0.20f * flight) * pulse * alpha),
                    new Vector2(24f * glyphScale, 24f * glyphScale));

                // Los TRAZOS (al volar arden al BLANCO).
                Vector2[] strokes = _runes[g % _runes.Length];
                Color col = Color.Lerp(GoldTip, WhiteIncan, flight * 0.6f);
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
                    Capsule(mid, len, 2.5f * glyphScale, rot, Tint(col, 0.85f * pulse * alpha));
                }

                // LA PERLA.
                Vector2 pearlPos = glyphPos + new Vector2(0f, -8.0f * glyphScale).RotatedBy(glyphRot);
                VFXCore.Quad(pearlPos, GoldBody * (0.5f * pulse * alpha),
                    new Vector2(5.2f * glyphScale, 5.2f * glyphScale));
                VFXCore.Quad(pearlPos, col * (0.9f * pulse * alpha),
                    new Vector2(2.4f * glyphScale, 2.4f * glyphScale));
            }
        }

        /// <summary>Posición mundial del glifo g (para las chispas de vuelo).</summary>
        public static Vector2 GetGlyphPosition(Vector2 back, float scale, float time, int g)
        {
            float breathe = 1f + 0.03f * (float)Math.Sin(time * 1.6f);
            float a = 36f * scale * breathe;
            float tilt = 0.06f * (float)Math.Sin(time * 0.7f);
            float spin = time * 0.42f;
            float ang = g / (float)RuneCount * MathHelper.TwoPi + spin;
            float floatR = 1f + 0.04f * (float)Math.Sin(time * 1.35f + g * 0.9f);
            return EllipsePoint(back, a * floatR, a * floatR, tilt, ang);
        }

        /// <summary>Cuenta de glifos (chispas).</summary>
        public static int Glyphs => RuneCount;

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
