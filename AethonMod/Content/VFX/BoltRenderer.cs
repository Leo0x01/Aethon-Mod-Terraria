using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// BoltRenderer — v6.03 — RELÁMPAGOS DETERMINISTAS DE LA BIBLIOTECA.
    ///
    /// Rayos en zigzag nacidos del mismo principio que el látigo eléctrico
    /// de la medusa nebulosa: el zigzag se deriva de (semilla, flick,
    /// segmento) con hash puro → TODAS las máquinas ven el MISMO rayo sin
    /// sincronizar nada, y el rayo se REGENERAR cada pocos ticks (flick) —
    /// está VIVO, no es una textura estática.
    ///
    /// Cada rayo: funda de halo gruesa + núcleo fino casi blanco, puntos de
    /// luz en los extremos y RAMAS laterales cortas donde el hash lo pide.
    ///
    /// Uso (biblioteca): VFXCore.Begin() → ComputeQuads(...) →
    /// VFXCore.FlushAdditive(...).
    /// </summary>
    public static class BoltRenderer
    {
        /// <summary>Segmentos del rayo principal.</summary>
        private const int Segments = 6;

        /// <summary>
        /// Calcula un rayo de <paramref name="start"/> a <paramref name="end"/>
        /// como cuadros de luz en el buffer de VFXCore (coords de MUNDO).
        /// </summary>
        /// <param name="start">Origen del rayo.</param>
        /// <param name="end">Destino del rayo.</param>
        /// <param name="seed">Semilla determinista (misma = mismo rayo).</param>
        /// <param name="flick">El "parpadeo" actual: cámbialo cada ~4-9 ticks
        /// para que el zigzag se regenere y el rayo VIVA.</param>
        /// <param name="width">Grosor base del halo en píxeles.</param>
        /// <param name="haloColor">Color de la funda exterior.</param>
        /// <param name="coreColor">Color del núcleo caliente.</param>
        /// <param name="alpha">Multiplicador global (0..1).</param>
        /// <param name="ampFactor">Amplitud del zigzag (1 = estándar).</param>
        public static void ComputeQuads(Vector2 start, Vector2 end, int seed, int flick,
            float width, Color haloColor, Color coreColor, float alpha = 1f, float ampFactor = 1f)
        {
            Vector2 delta = end - start;
            float length = delta.Length();
            if (length < 4f) return;

            Vector2 dir = delta / length;
            Vector2 normal = new Vector2(-dir.Y, dir.X);
            float amp = Math.Min(length * 0.16f, 14f) * ampFactor;

            // Puntos del zigzag (en coords de mundo).
            Vector2[] pts = new Vector2[Segments + 1];
            for (int s = 0; s <= Segments; s++)
            {
                float t = s / (float)Segments;
                // El zigzag se AMPLÍA en el medio (un rayo real tiene la
                // tensión en el centro, quieto en los anclajes).
                float envelope = (float)Math.Sin(t * Math.PI);
                float jitter = (VFXCore.Hash01(seed, flick, s) - 0.5f) * 2f * amp * envelope;
                pts[s] = start + dir * (length * t) + normal * jitter;
            }

            // Trazos: funda gruesa + núcleo fino por segmento.
            for (int s = 0; s < Segments; s++)
            {
                Vector2 a = pts[s];
                Vector2 b = pts[s + 1];
                Vector2 mid = (a + b) * 0.5f;
                Vector2 seg = b - a;
                float segLen = seg.Length();
                if (segLen < 0.5f) continue;
                float rot = (float)Math.Atan2(seg.Y, seg.X);

                VFXCore.Quad(mid, haloColor * alpha, new Vector2(segLen + width, width * 2.1f), rot);
                VFXCore.Quad(mid, coreColor * alpha, new Vector2(segLen + width * 0.4f, width * 0.75f), rot);

                // RAMA lateral corta donde el hash lo pide.
                if (s > 0 && s < Segments - 1 && VFXCore.Hash01(seed, flick, s + 91) > 0.62f)
                {
                    float side = VFXCore.Hash01(seed, flick, s + 37) > 0.5f ? 1f : -1f;
                    float branchLen = (0.35f + 0.4f * VFXCore.Hash01(seed, flick, s + 53)) * length * 0.25f;
                    Vector2 branchDir = (dir * 0.45f + normal * side).SafeNormalize(Vector2.UnitY);
                    Vector2 bEnd = b + branchDir * branchLen;
                    Vector2 bMid = (b + bEnd) * 0.5f;
                    float bRot = (float)Math.Atan2(branchDir.Y, branchDir.X);
                    VFXCore.Quad(bMid, haloColor * (0.6f * alpha),
                        new Vector2(branchLen + width * 0.6f, width * 1.3f), bRot);
                    VFXCore.Quad(bMid, coreColor * (0.6f * alpha),
                        new Vector2(branchLen + width * 0.2f, width * 0.5f), bRot);
                }
            }

            // Extremos brillantes (descarga en el origen, frente de impacto).
            VFXCore.Quad(start, coreColor * (0.9f * alpha), new Vector2(width * 3.2f, width * 3.2f));
            VFXCore.Quad(start, haloColor * (0.8f * alpha), new Vector2(width * 5.0f, width * 5.0f));
            VFXCore.Quad(end, coreColor * (0.9f * alpha), new Vector2(width * 2.6f, width * 2.6f));
            VFXCore.Quad(end, haloColor * (0.7f * alpha), new Vector2(width * 4.0f, width * 4.0f));
        }
    }
}
