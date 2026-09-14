using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// RunicHaloRenderer — v6.24 — LOS CÍRCULOS RÚNICOS DE LOS AGUJEROS.
    ///
    /// Petición original v6.22: "un anillo rúnico en la espalda que
    /// funcione como alas y halo; cuando el jugador va a volar, este
    /// anillo brilla con intensidad".
    ///
    /// v6.24 — LA ORDEN DEL USUARIO: "el anillo rúnico estelar tiene los
    /// mismos problemas que la corona (brilla mucho, no se parece al aro
    /// de los soles) — además ESTOS AROS NO DEBEN ESTAR EN ESA FORMA:
    /// la forma correcta es LA MISMA FORMA QUE LA DE LOS AGUJEROS NEGROS".
    ///
    /// LOS CÍRCULOS RÚNICOS DE LOS AGUJEROS, LITERALES (la técnica de
    /// SupremoBlackHoleRenderer.DrawRuneCircles), orbitando la ESPALDA:
    ///   · TRES círculos PLANOS (RingQuad — el aro fino de los agujeros):
    ///     el BLANCO íntimo a 2.02R (CW rápido), el DORADO a 2.62R (CW
    ///     lento) y el VIOLETA a 3.30R (CCW — el contrarroto arcano),
    ///   · las runas flotando ALREDEDOR de cada círculo (el radio respira
    ///     por glifo, el glifo se mece — NADA de runas tangenciales de
    ///     soles: la runa del agujero vive DE PIE sobre su círculo),
    ///   · perlas sobre los glifos y latidos propios,
    ///   · LOS ALPHAS EXACTOS de los agujeros: aros 0.24/0.20/0.18,
    ///     trazos 0.85·pulse, perlas 0.62/0.90·pulse.
    ///
    /// LA INTENSIDAD VIVE (acotada al lenguaje de los agujeros): `flight`
    /// (0..1) es la energía de vuelo — al VOLAR los círculos giran más
    /// rápido y las runas arden hacia el blanco, con un multiplicador
    /// 0.85..1.20, cero bloom cegador.
    /// </summary>
    public static class RunicHaloRenderer
    {
        // --- LA GEOMETRÍA DE LOS AGUJEROS (los valores LITERALES) ---
        private const int WhiteRuneCount = 6;      // blancas, CW rápido
        private const float WhiteRuneRadius = 2.02f;   // ×R — MÁS ADENTRO
        private const float WhiteRuneOrbit = 0.16f;    // rad/s — el círculo vivo
        private const int GoldRuneCount = 8;       // doradas, CW
        private const float GoldRuneRadius = 2.62f;    // ×R
        private const float GoldRuneOrbit = 0.10f;     // rad/s
        private const int VioletRuneCount = 6;     // azul-violeta, CCW
        private const float VioletRuneRadius = 3.30f;  // ×R — MÁS AFUERA
        private const float VioletRuneOrbit = -0.075f; // rad/s — CONTRARROTO

        /// <summary>Los TRES círculos de los agujeros negros.</summary>
        public const int RingCount = 3;

        /// <summary>Runas del círculo k (LITERAL de los agujeros: 6/8/6).</summary>
        public static int RunesOf(int k)
            => k == 0 ? WhiteRuneCount : k == 1 ? GoldRuneCount : VioletRuneCount;

        /// <summary>Radio del círculo k (×R — LITERAL de los agujeros).</summary>
        public static float RadiusOf(int k)
            => k == 0 ? WhiteRuneRadius : k == 1 ? GoldRuneRadius : VioletRuneRadius;

        /// <summary>Giro del círculo k (rad/s — LITERAL de los agujeros).</summary>
        public static float OrbitOf(int k)
            => k == 0 ? WhiteRuneOrbit : k == 1 ? GoldRuneOrbit : VioletRuneOrbit;

        /// <summary>
        /// La base del sistema (px) — el círculo blanco abraza la espalda
        /// (26 px) y el violeta (43 px) es el gran halo exterior.
        /// </summary>
        private const float BaseR = 13f;

        // --- LA PALETA (los colores LITERALES de los círculos del supremo) ---
        private static readonly Color RuneWhite = new(255, 245, 220);    // cuerpo de runa blanca
        private static readonly Color RuneWhiteTip = new(255, 252, 240); // punta incandescente
        private static readonly Color RuneGold = new(255, 180, 70);      // cuerpo de runa dorada
        private static readonly Color RuneGoldTip = new(255, 235, 175);  // punta pálida dorada
        private static readonly Color RuneViolet = new(110, 130, 255);   // cuerpo de runa azul-violeta
        private static readonly Color RuneVioletTip = new(205, 220, 255);// punta pálida fría

        // --- LA TABLA DE GLIFOS (la ESCRITURA DEL AGUJERO: S0..S7 del
        //     SupremoBlackHoleRenderer — soles rotos, cetros y tronos) ---
        private static readonly Vector2[][] _runes = new Vector2[][]
        {
            // S0 — EL SOL ROTO
            new Vector2[] { new(0f, -7f), new(0f, 7f), new(-3.5f, -3f), new(0f, -6.5f), new(3.5f, -3f), new(0f, -6.5f), new(-3.5f, 3.5f), new(3.5f, 3.5f), new(-2f, 5.5f), new(2f, 5.5f) },
            // S1 — EL CETRO
            new Vector2[] { new(0f, 7f), new(0f, -4f), new(0f, -4f), new(-3f, -7f), new(0f, -4f), new(3f, -7f), new(-2.5f, 0f), new(2.5f, 0f), new(-2.5f, 3f), new(2.5f, 3f) },
            // S2 — EL TRONO
            new Vector2[] { new(-3.5f, 7f), new(-3.5f, -5f), new(-3.5f, -5f), new(3.5f, -5f), new(3.5f, -5f), new(3.5f, 7f), new(-3.5f, -5f), new(0f, -7f), new(-1.5f, 1.5f), new(1.5f, 1.5f) },
            // S3 — LA ESTRELLA DOBLE
            new Vector2[] { new(0f, 7f), new(0f, -7f), new(-4f, 0f), new(4f, 0f), new(-2.5f, -4.5f), new(2.5f, 4.5f), new(2.5f, -4.5f), new(-2.5f, 4.5f) },
            // S4 — EL CIRCUITO REAL
            new Vector2[] { new(-3.5f, 6f), new(-3.5f, -4f), new(-3.5f, -4f), new(3.5f, -4f), new(3.5f, -4f), new(3.5f, 6f), new(-3.5f, 6f), new(3.5f, 6f), new(-3.5f, -6.5f), new(3.5f, -6.5f), new(0f, -4f), new(0f, -6.5f) },
            // S5 — LA VUELTA SUPREMA
            new Vector2[] { new(-3f, 6f), new(-3f, -2f), new(-3f, -2f), new(3f, -6f), new(3f, -6f), new(3f, 2f), new(3f, 2f), new(-2.5f, 6f), new(-1.5f, -6.5f), new(1.5f, -6.5f) },
            // S6 — EL OJO DEL VACÍO
            new Vector2[] { new(-4f, 0f), new(0f, -4f), new(0f, -4f), new(4f, 0f), new(4f, 0f), new(0f, 4f), new(0f, 4f), new(-4f, 0f), new(-1.5f, 0f), new(1.5f, 0f), new(0f, -7f), new(0f, -4.5f), new(0f, 4.5f), new(0f, 7f) },
            // S7 — LA CORONA ESTELAR
            new Vector2[] { new(-4f, 5.5f), new(-4f, -5.5f), new(-4f, -5.5f), new(-2f, -1f), new(-2f, -1f), new(0f, -6.5f), new(0f, -6.5f), new(2f, -1f), new(2f, -1f), new(4f, -5.5f), new(4f, -5.5f), new(4f, 5.5f), new(-4f, 5.5f), new(4f, 5.5f) },
        };

        // ==================================================================
        //  EL RENDER — los círculos de los agujeros al buffer de VFXCore
        // ==================================================================

        /// <summary>
        /// Calcula LOS TRES CÍRCULOS RÚNICOS (la forma de los agujeros
        /// negros) como cuadros de luz en el buffer de VFXCore
        /// (coordenadas de MUNDO).
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

            // LA INTENSIDAD ACOTADA AL LENGUAJE DE LOS AGUJEROS: quieto un
            // pelo por debajo; volando un pelo por encima — NUNCA bloom.
            float glow = 0.85f + 0.35f * flight;

            float R = BaseR * scale;
            float glyphScale = Math.Max(R / 52f, 0.25f) * 1.40f;

            // === LOS TRES CÍRCULOS (la forma LITERAL de los agujeros) ===
            for (int k = 0; k < RingCount; k++)
            {
                float radius = RadiusOf(k) * R;
                // Al volar TODO el sistema se aviva (×1..1.5 — el motor).
                float orbit = time * OrbitOf(k) * (1f + 0.5f * flight);
                int count = RunesOf(k);
                bool white = k == 0, gold = k == 1;
                Color body = white ? RuneWhite : gold ? RuneGold : RuneViolet;
                Color tip = white ? RuneWhiteTip : gold ? RuneGoldTip : RuneVioletTip;
                // El alpha del ARO: LITERAL de los agujeros (0.24/0.20/0.18).
                float ringA = (white ? 0.20f : gold ? 0.24f : 0.18f) * glow * alpha;

                // --- 1. EL ARO: el círculo fino de los agujeros (RingQuad) ---
                VFXCore.Quad(back, Tint(body, ringA),
                    VFXCore.RingQuadSize(radius), orbit * 0.9f, VFXCore.Ring);

                // --- 2. LAS RUNAS: flotando ALREDEDOR del círculo (el radio
                //     respira por glifo y el glifo se mece — la runa del
                //     agujero vive DE PIE, no tangencial) ---
                for (int g = 0; g < count; g++)
                {
                    float ang = g / (float)count * MathHelper.TwoPi + orbit;

                    // Flotación viva: el radio respira por glifo y el glifo
                    // se mece verticalmente (la técnica del agujero).
                    float floatR = radius +
                                   2.4f * glyphScale * (float)Math.Sin(time * 1.35f + g * 0.9f);
                    float bobY = 2.0f * glyphScale * (float)Math.Sin(time * 0.85f + g * 1.7f);
                    Vector2 glyphPos = back + new Vector2(
                        (float)Math.Cos(ang) * floatR,
                        (float)Math.Sin(ang) * floatR + bobY);

                    // Latido de brillo propio por glifo.
                    float pulse = 0.75f + 0.25f * (float)Math.Sin(time * 2.4f + g * 1.3f);

                    // Resplandor suave DETRÁS (0.20 del agujero).
                    VFXCore.Quad(glyphPos, Tint(body, 0.20f * pulse * glow * alpha),
                        new Vector2(36f * glyphScale, 36f * glyphScale));

                    // Los TRAZOS del glifo (al volar arden al BLANCO — sutil).
                    Vector2[] strokes = _runes[(g + k * 3) % _runes.Length];
                    Color col = Color.Lerp(tip, new Color(255, 250, 240), flight * 0.45f);
                    for (int s = 0; s < strokes.Length; s += 2)
                    {
                        Vector2 a = glyphPos + strokes[s] * glyphScale;
                        Vector2 b = glyphPos + strokes[s + 1] * glyphScale;
                        Vector2 mid = (a + b) * 0.5f;
                        Vector2 delta = b - a;
                        float len = delta.Length();
                        if (len < 0.01f) continue;
                        float rot = (float)Math.Atan2(delta.Y, delta.X);

                        // Gradiente vertical: abajo cuerpo, arriba punta pálida.
                        float localY = ((strokes[s].Y + strokes[s + 1].Y) * 0.5f + 7f) / 14f;
                        Color strokeCol = Color.Lerp(col, body, 1f - localY * 0.25f);

                        Capsule(mid, len, 3.4f * glyphScale, rot,
                            Tint(strokeCol, 0.85f * pulse * glow * alpha));
                    }

                    // PERLA sobre el glifo (la gema de la corona del agujero).
                    Vector2 pearlPos = glyphPos - new Vector2(0f, 11.5f * glyphScale);
                    float pearlPulse = 0.8f + 0.2f * (float)Math.Sin(time * 3.0f + g * 2.0f);
                    VFXCore.Quad(pearlPos, Tint(body, 0.62f * pulse * glow * alpha),
                        new Vector2(7.0f * glyphScale, 7.0f * glyphScale));
                    VFXCore.Quad(pearlPos, Tint(tip, 0.9f * pearlPulse * glow * alpha),
                        new Vector2(3.2f * glyphScale, 3.2f * glyphScale));
                }
            }
        }

        /// <summary>Posición mundial del glifo g del sistema (las chispas).</summary>
        public static Vector2 GetGlyphPosition(Vector2 back, float scale, float time, int g)
        {
            // Mapa acumulativo: círculo 0 (6 blancas) · 1 (8 doradas) · 2 (6 violetas).
            int k = 0, idx = g;
            for (int i = 0; i < RingCount && idx >= RunesOf(i); i++)
            {
                idx -= RunesOf(i);
                k++;
            }
            k %= RingCount;

            float R = BaseR * scale;
            float glyphScale = Math.Max(R / 52f, 0.25f) * 1.40f;
            float radius = RadiusOf(k) * R;
            float orbit = time * OrbitOf(k);
            float ang = idx / (float)RunesOf(k) * MathHelper.TwoPi + orbit;
            float floatR = radius + 2.4f * glyphScale * (float)Math.Sin(time * 1.35f + idx * 0.9f);
            float bobY = 2.0f * glyphScale * (float)Math.Sin(time * 0.85f + idx * 1.7f);
            return back + new Vector2(
                (float)Math.Cos(ang) * floatR,
                (float)Math.Sin(ang) * floatR + bobY);
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

        private static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color(c.R, c.G, c.B, (byte)(int)(255f * f));
        }
    }
}
