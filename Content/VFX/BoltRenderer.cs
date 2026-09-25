using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// BoltRenderer — v6.39 — EL RELÁMPAGO DETERMINISTA DE PRIMERA GENERACIÓN,
    /// REPARADO CON EL RIBBON DE LA CASA.
    ///
    /// Rayos en zigzag nacidos del mismo principio que el látigo eléctrico
    /// de la medusa nebulosa: el zigzag se deriva de (semilla, flick,
    /// segmento) con hash puro → TODAS las máquinas ven el MISMO rayo sin
    /// sincronizar nada, y el rayo se REGENERAR cada pocos ticks (flick) —
    /// está VIVO, no es una textura estática.
    ///
    /// v6.39 — LA REPARACIÓN (informe research/v637): la versión v6.03
    /// estiraba SoftGlow (RADIAL: funde a 0 en los dos extremos de cada
    /// quad) con solapes `segLen + width` → el aditivo APILABA cada junta
    /// (cuentas claras) y los fundidos dejaban franjas oscuras — EL brillo
    /// "cortado por secciones" que el usuario reportó en los desgarros.
    /// La nueva receta es la de StormLib v6.39: quads BORDE A BORDE con la
    /// NORMAL MEDIA en las juntas, LARGO EXACTO proyectado + extensión
    /// adaptativa de giro (w/2·tan(δ/2)), y LAS TEXTURAS DE BANDA UNIFORME
    /// (BoltHalo halo + BoltCore núcleo — premultiplicadas: el lote aditivo
    /// respeta su perfil). Las ramas laterales y los gorros de extremo
    /// siguen siendo glows RADIALES (puntos, no tiras: ahí SoftGlow es el
    /// pincel correcto).
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
            pts[0] = start;
            pts[Segments] = end;

            // Trazos: halo de banda + vena fina por segmento — BORDE A BORDE
            // (v6.39: la normal media orienta la junta; el largo es la
            // proyección sobre esa dirección + la extensión de giro; las
            // texturas son las BANDAS UNIFORMES de la casa).
            for (int s = 0; s < Segments; s++)
            {
                Vector2 a = pts[s];
                Vector2 b = pts[s + 1];
                Vector2 seg = b - a;
                float segLen = seg.Length();
                if (segLen < 0.5f) continue;

                // LA NORMAL MEDIA (la lección del ribbon).
                Vector2 prev = s > 0 ? a - pts[s - 1] : seg;
                Vector2 next = s < Segments - 1 ? pts[s + 2] - b : seg;
                Vector2 avg = Vector2.Normalize(prev) + Vector2.Normalize(next);
                if (avg.LengthSquared() < 0.001f) avg = seg;
                avg = Vector2.Normalize(avg);

                Vector2 mid = (a + b) * 0.5f;
                float rot = (float)Math.Atan2(avg.Y, avg.X);

                // EL LARGO EXACTO + la extensión de giro del round-join.
                float largo = Vector2.Dot(seg, avg);
                if (largo < 0.5f) continue;
                largo += ExtJunta(width, Angulo(prev, seg)) + ExtJunta(width, Angulo(seg, next));

                // LAS DOS CAPAS: banda del halo (×2) + vena del núcleo (×0.4)
                // — con textura propia (la sobrecarga v6.08 de Quad).
                VFXCore.Quad(mid, haloColor * alpha, new Vector2(largo, width * 2.0f), rot, StormLib.BandaTex);
                VFXCore.Quad(mid, coreColor * alpha, new Vector2(largo, width * 0.42f), rot, StormLib.VenaTex);

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
                        new Vector2(branchLen + width * 0.6f, width * 1.3f), bRot, StormLib.BandaTex);
                    VFXCore.Quad(bMid, coreColor * (0.6f * alpha),
                        new Vector2(branchLen + width * 0.2f, width * 0.34f), bRot, StormLib.VenaTex);
                }
            }

            // Extremos brillantes (descarga en el origen, frente de impacto)
            // — puntos radiales: aquí SoftGlow (via FlushAdditive) es el
            // pincel CORRECTO (los gorros son puntos, no tiras).
            VFXCore.Quad(start, coreColor * (0.9f * alpha), new Vector2(width * 3.2f, width * 3.2f));
            VFXCore.Quad(start, haloColor * (0.8f * alpha), new Vector2(width * 5.0f, width * 5.0f));
            VFXCore.Quad(end, coreColor * (0.9f * alpha), new Vector2(width * 2.6f, width * 2.6f));
            VFXCore.Quad(end, haloColor * (0.7f * alpha), new Vector2(width * 4.0f, width * 4.0f));
        }

        /// <summary>El ángulo (0..π) entre dos direcciones (el giro de la junta).</summary>
        private static float Angulo(Vector2 d0, Vector2 d1)
        {
            if (d0.LengthSquared() < 0.0001f || d1.LengthSquared() < 0.0001f) return 0f;
            float dot = MathHelper.Clamp(Vector2.Dot(Vector2.Normalize(d0), Vector2.Normalize(d1)), -1f, 1f);
            return (float)Math.Acos(dot);
        }

        /// <summary>La extensión del round-join (w/2·tan(δ/2), suelo 1.2 px).</summary>
        private static float ExtJunta(float w, float giro)
            => MathHelper.Clamp(w * 0.5f * (float)Math.Tan(giro * 0.5f) + 1.2f, 1.2f, w * 0.5f + 1.2f);
    }
}
