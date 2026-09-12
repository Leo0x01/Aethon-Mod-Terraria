using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// ArcCrownRenderer — v6.03 — LA CORONA DE ARCOS DE NEÓN.
    ///
    /// La corona original del agujero negro carmesí (v6.02), promovida a
    /// MIEMBRO DE LA BIBLIOTECA VISUAL y a COSMÉTICO DEL JUGADOR: cinco
    /// lazos de neón carmesí→magenta arqueados, con asimetría por lazo
    /// (líneas de campo curvadas, no un arcoíris), un ECO interior más
    /// tenue por lazo y NUDOS NARANJA con destello de 4 puntas en los
    /// ápices. Respira con el tiempo y balancea cada lazo de forma
    /// determinista — la corona es energía VIVA, no un adorno estático.
    ///
    /// Uso (biblioteca): VFXCore.Begin() → ComputeQuads(...) →
    /// VFXCore.AppendToPlayerDraw(...) o VFXCore.FlushAdditive(...).
    /// </summary>
    public static class ArcCrownRenderer
    {
        /// <summary>Número de lazos de la corona (5: el diseño original).</summary>
        public const int Loops = 5;

        /// <summary>Segmentos por lazo (suavidad del arco).</summary>
        private const int Segments = 18;

        /// <summary>
        /// Calcula TODA la corona como cuadros de luz en el buffer de
        /// VFXCore (coordenadas de MUNDO, centrada en <paramref name="center"/>).
        /// </summary>
        /// <param name="center">Centro de la cabeza que corona (mundo).</param>
        /// <param name="horizonPx">Radio de referencia: el "horizonte" sobre
        /// el que se arquean los lazos (para una cabeza humana ≈ 0.55× su
        /// ancho; en el proyectil era el radio del horizonte de sucesos).</param>
        /// <param name="time">Tiempo animado (Main.GlobalTimeWrappedHourly).</param>
        /// <param name="alpha">Multiplicador global de intensidad (0..1).</param>
        public static void ComputeQuads(Vector2 center, float horizonPx, float time, float alpha = 1f)
        {
            if (horizonPx < 1.5f) return;

            for (int l = 0; l < Loops; l++)
            {
                float t01 = l / (float)(Loops - 1);            // 0 exterior → 1 interior
                float breathe = VFXCore.Breathe(time, 2.2f, l * 1.7f);
                // ASIMETRÍA por lazo (semianchos izq/der distintos y balanceo
                // determinista): las líneas de campo se curvan, no forman un
                // arcoíris simétrico.
                float sway = 0.18f * VFXCore.Sway(time, 0.9f, l * 2.6f);
                float halfWL = horizonPx * (1.55f - 0.30f * t01) * (1f - sway) * breathe;
                float halfWR = horizonPx * (1.55f - 0.30f * t01) * (1f + sway) * breathe;
                float apexH = horizonPx * (2.30f - 0.95f * t01) * breathe;
                float baseY = center.Y - horizonPx * 1.06f;

                // El lazo completo: media elipse superior ASIMÉTRICA por
                // puntos de glow con grosor variable (fino en las bases,
                // corpulento al subir, afilado en el ápice).
                for (int s = 0; s <= Segments; s++)
                {
                    float ang = s / (float)Segments * MathHelper.Pi;   // 0..π
                    float edge = (float)Math.Sin(ang);                 // 0 bases, 1 ápice
                    float side = (float)Math.Cos(ang);                 // -1 izq → +1 der
                    float halfW = side < 0f ? halfWL : halfWR;
                    Vector2 pos = new Vector2(
                        center.X - (float)Math.Cos(ang) * halfW,
                        baseY - edge * apexH);

                    Color col = VFXPalettes.CrimsonCourt.Loop(edge);
                    // Grosor: crece hacia arriba, afila en el ápice (chispa final).
                    float thickness = 0.16f + 0.13f * edge * (1f - 0.35f * edge);
                    float intensity = 0.30f + 0.70f * edge;
                    float pointPx = horizonPx * thickness;

                    Color finalCol = col * (intensity * alpha);
                    VFXCore.Quad(pos, finalCol, new Vector2(pointPx * 2f, pointPx * 2f));
                }

                // ECO interior: un filamento más tenue y fino encajado dentro
                // del lazo (los "sigilos de fuego" — filamentos encajados).
                for (int s = 1; s < Segments; s++)
                {
                    float ang = s / (float)Segments * MathHelper.Pi;
                    float edge = (float)Math.Sin(ang);
                    float side = (float)Math.Cos(ang);
                    float halfW = (side < 0f ? halfWL : halfWR) * 0.66f;
                    Vector2 pos = new Vector2(
                        center.X - (float)Math.Cos(ang) * halfW,
                        baseY - edge * apexH * 0.72f);
                    Color col = Color.Lerp(VFXPalettes.CrimsonCourt.EchoBase,
                                           VFXPalettes.CrimsonCourt.EchoApex, edge);
                    float echoPx = horizonPx * 0.09f;
                    VFXCore.Quad(pos, col * (0.55f * alpha), new Vector2(echoPx * 2f, echoPx * 2f));
                }

                // NUDO NARANJA en el ápice: englobado + DESTELLO DE 4 PUNTAS
                // (dos glows estirados en cruz) + chispa blanca central.
                float knotPulse = 0.85f + 0.30f * (float)Math.Sin(time * 3.1f + l * 2.3f);
                Vector2 apex = new Vector2(center.X, baseY - apexH);

                float knotPx = horizonPx * 0.34f;
                VFXCore.Quad(apex, VFXPalettes.CrimsonCourt.Knot * (knotPulse * alpha),
                    new Vector2(knotPx * 2f, knotPx * 2f));

                // Destello de 4 puntas: dos elipses estiradas en cruz.
                float flareLen = horizonPx * 0.85f * knotPulse;
                float flareWide = horizonPx * 0.10f;
                VFXCore.Quad(apex, VFXPalettes.CrimsonCourt.KnotFlare * (knotPulse * 0.85f * alpha),
                    new Vector2(flareLen, flareWide));
                VFXCore.Quad(apex, VFXPalettes.CrimsonCourt.KnotFlare * (knotPulse * 0.85f * alpha),
                    new Vector2(flareWide, flareLen));

                // Chispa blanca central.
                float sparkPx = horizonPx * 0.14f;
                VFXCore.Quad(apex, VFXPalettes.CrimsonCourt.KnotSpark * (knotPulse * 0.9f * alpha),
                    new Vector2(sparkPx * 2f, sparkPx * 2f));
            }
        }
    }
}
