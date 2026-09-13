using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// SolarCoronaWings — v6.13 — LAS ALAS DE PROMINENCIAS SOLARES.
    ///
    /// Rediseño con la técnica de las coronas — LITERALMENTE la corona de
    /// arcos (ArcCrownRenderer) promovida a alas: por lado hay TRES LAZOS
    /// de prominencia arqueados desde el hombro (semianchos y alturas
    /// distintos — asimetría deliberada, no un arcoíris), cada lazo es una
    /// CADENA de cápsulas con gradiente de temperatura (base rojo profundo
    /// → naranja → oro pálido en el ápice), con su FILAMENTO eco interior
    /// (más tenue y caliente) y su NUDO en el ápice: perla + DESTELLO DE
    /// 4 PUNTAS + chispa blanca — exactamente los nudos naranjas de la
    /// corona que el usuario ama.
    ///
    /// Las llamaradas RESPIRAN (breathe por lazo, desfasado) y se mecen
    /// (sway asimétrico); al volar los ápices se estiran.
    /// </summary>
    public static class SolarCoronaWings
    {
        /// <summary>Lazos de prominencia por lado.</summary>
        private const int Loops = 3;

        /// <summary>Segmentos por lazo.</summary>
        private const int Segments = 14;

        public static void Render(ref WingDrawContext ctx)
        {
            if (ctx.Alpha <= 0f) return;
            float open = MathHelper.Clamp(ctx.Open, 0f, 1.1f);

            float flap = (float)Math.Sin(ctx.FlapPhase) * ctx.FlapAmp;

            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 root = GetBack(ctx, side);

                // ============ EL NÚCLEO SOLAR EN LA ESPALDA ============
                // un "sol pequeño" del que nacen las prominencias
                float corePulse = 0.85f + 0.15f * (float)Math.Sin(ctx.Time * 2.4f);
                VFXCore.Quad(root, new Color(120, 30, 10) * (0.55f * ctx.Alpha),
                    new Vector2(26f, 26f) * corePulse);
                VFXCore.Quad(root, new Color(255, 130, 40) * (0.65f * ctx.Alpha),
                    new Vector2(14f, 14f) * corePulse);
                WingStrokes.Pearl(root, 9.0f, new Color(255, 170, 70),
                    new Color(255, 250, 235), 0.95f * ctx.Alpha);

                for (int l = 0; l < Loops; l++)
                {
                    float t01 = l / (float)(Loops - 1);       // 0 interior → 1 exterior

                    // ============ ASIMETRÍA por lazo (líneas de campo) ============
                    float breathe = VFXCore.Breathe(ctx.Time, 2.1f + t01 * 0.6f, l * 1.7f, 0.05f);
                    float sway = 0.16f * VFXCore.Sway(ctx.Time, 0.85f, l * 2.6f);
                    // el aleteo inclina los lazos completos
                    float flapTilt = flap * (0.28f - 0.08f * t01);

                    // geometría: semianchos izq/der DISTINTOS + ápice desplazado
                    float reach = MathHelper.Lerp(22f, 36f, t01) * (0.52f + 0.48f * open);
                    float halfWL = reach * (1.06f - sway) * breathe;
                    float halfWR = reach * (0.86f + sway) * breathe;
                    // el ápice apunta arriba-afuera, cada lazo con su ángulo
                    float apexAng = MathHelper.Lerp(-0.95f, -0.35f, t01) * side + flapTilt * 0.4f;
                    float apexH = MathHelper.Lerp(32f, 24f, t01) *
                                  (0.52f + 0.48f * open) * breathe;

                    // base del lazo: en el "sol" de la espalda, desplazada por lazo
                    Vector2 basePos = root + new Vector2(side * (4f + t01 * 6f), 2f * ctx.GravDir);

                    // ============ EL LAZO (cadena con gradiente de temperatura) ============
                    Vector2[] pts = new Vector2[Segments + 1];
                    for (int s = 0; s <= Segments; s++)
                    {
                        float u = s / (float)Segments;         // 0 base A → 1 base B
                        float arc = u * MathHelper.Pi;        // 0..π sobre el lazo
                        float edge = (float)Math.Sin(arc);    // 0 bases → 1 ápice
                        float sideSign = (float)Math.Cos(arc); // -1 → +1

                        // punto del semicírculo ASIMÉTRICO
                        float hx = -(float)Math.Cos(arc) * (sideSign < 0 ? halfWL : halfWR) * side;
                        float hy = -edge * apexH * ctx.GravDir;

                        // el ápice completo se inclina según apexAng
                        float ca = (float)Math.Cos(apexAng * edge * 0.35f);
                        float sa = (float)Math.Sin(apexAng * edge * 0.35f);
                        Vector2 local = new Vector2(hx * ca - hy * sa, hx * sa + hy * ca);

                        // ondulación fina de plasma (llama viva)
                        float wob = 1.6f * (float)Math.Sin(u * 9f + ctx.Time * (3f + t01)) * edge;

                        pts[s] = basePos + local + new Vector2(0f, wob * ctx.GravDir);
                    }

                    // pintar la cadena: base roja → naranja → ápice oro pálido
                    for (int s = 0; s < Segments; s++)
                    {
                        float u = s / (float)Segments;
                        float edge = (float)Math.Sin(u * MathHelper.Pi);
                        Color ca = TempColor(u);
                        Color cb = TempColor((s + 1) / (float)Segments);
                        // grosor variable: fino en bases, corpulento al subir
                        float w = MathHelper.Lerp(2.2f, 4.4f, edge) * (0.7f + 0.3f * open);
                        WingStrokes.Stroke(pts[s], pts[s + 1], w, ca, cb,
                            0.90f * ctx.Alpha * (0.6f + 0.4f * open));
                    }

                    // ============ EL FILAMENTO ECO (más tenue, más caliente) ============
                    for (int s = 1; s < Segments - 1; s += 2)
                    {
                        Vector2 a = Vector2.Lerp(basePos, pts[s], 0.68f);
                        Vector2 b = Vector2.Lerp(basePos, pts[s + 1], 0.68f);
                        WingStrokes.Stroke(a, b, 1.4f, new Color(255, 220, 150),
                            new Color(255, 240, 190), 0.5f * ctx.Alpha);
                    }

                    // ============ EL NUDO DEL ÁPICE (perla + destello 4 puntas) ============
                    Vector2 apex = pts[Segments / 2];
                    float knotPulse = 0.85f + 0.30f * (float)Math.Sin(ctx.Time * 3.1f + l * 2.3f);

                    // halo del nudo
                    VFXCore.Quad(apex, new Color(255, 138, 60) * (knotPulse * 0.55f * ctx.Alpha),
                        new Vector2(16f, 16f));
                    // DESTELLO DE 4 PUNTAS (la firma de la corona de arcos)
                    WingStrokes.Flare4(apex, 15f + 6f * t01, 2.6f,
                        new Color(255, 178, 96), knotPulse * 0.85f * ctx.Alpha);
                    // chispa blanca central
                    WingStrokes.Pearl(apex, 6.5f, new Color(255, 170, 90),
                        new Color(255, 250, 238), knotPulse * 0.9f * ctx.Alpha);
                }

                // ============ BRASAS flotando bajo los lazos ============
                for (int k = 0; k < 4; k++)
                {
                    float h = VFXCore.Hash01(side * 13 + k, k * 7 + 2, 5);
                    float tw = 0.5f + 0.5f * (float)Math.Sin(ctx.Time * (2.5f + h * 3f) + h * 13f);
                    if (tw < 0.66f) continue;
                    Vector2 pos = root + new Vector2(
                        side * (6f + h * 30f) * open,
                        (4f - h * 22f) * open * ctx.GravDir +
                        2.0f * (float)Math.Sin(ctx.Time * 1.9f + h * 8f) * ctx.GravDir);
                    WingStrokes.Pearl(pos, 4.8f, new Color(255, 130, 50),
                        new Color(255, 244, 224), 0.8f * tw * ctx.Alpha);
                }
            }
        }

        /// <summary>Anclaje del ala (compatibilidad de firma).</summary>
        private static Vector2 GetBack(WingDrawContext ctx, int side)
            => ctx.Back + new Vector2(side * 3f, -1f * ctx.GravDir);

        /// <summary>Gradiente de TEMPERATURA base→ápice (rojo→oro pálido).</summary>
        private static Color TempColor(float u)
        {
            // u: 0 base → 1 ápice
            if (u < 0.45f)
                return Color.Lerp(new Color(196, 44, 22), new Color(255, 110, 36), u / 0.45f);
            return Color.Lerp(new Color(255, 110, 36), new Color(255, 232, 150), (u - 0.45f) / 0.55f);
        }
    }
}
