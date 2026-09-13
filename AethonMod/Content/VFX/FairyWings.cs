using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// FairyWings — v6.13 — EL HADA DE POLVO ESTELAR "PÉTALOS".
    ///
    /// Rediseño con la técnica de las coronas: cada uno de los CUATRO
    /// pétalos por lado es una HOJA DE LUZ construida como las runas —
    /// contorno de dos trazos simétricos que se encuentran en la punta
    /// (gradiente ámbar), TRES venas interiores pálidas que nacen de la
    /// base, relleno translúcido con volumen oscuro debajo, y una PERLA
    /// de polvo estelar en la punta. Micro-perlas titilantes festonean el
    /// borde exterior — el "polvo" que deja el hada al pasar.
    ///
    /// La vibración de colibrí se conserva (rápida y superficial, siempre
    /// latiendo) con retardo por pétalo: una ONDA continua punta→raíz.
    /// </summary>
    public static class FairyWings
    {
        public static void Render(ref WingDrawContext ctx)
        {
            if (ctx.Alpha <= 0f) return;
            float open = MathHelper.Clamp(ctx.Open, 0f, 1.1f);

            // vibración de colibrí: alta frecuencia, poca amplitud, SIEMPRE
            float flutter = (float)Math.Sin(ctx.FlapPhase) * (ctx.FlapAmp * 0.75f + 0.25f);

            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 root = ctx.Back + new Vector2(side * 4f, -3f * ctx.GravDir);

                for (int lobe = 0; lobe < 4; lobe++)
                {
                    // configuración por pétalo: (ángulo base, longitud, ancho)
                    float baseAng = lobe switch
                    {
                        0 => -1.28f,   // superior alto
                        1 => -0.88f,   // superior exterior
                        2 => -0.30f,   // inferior exterior
                        _ => 0.10f,    // inferior bajo
                    };
                    float len = lobe switch { 0 => 27f, 1 => 32f, 2 => 22f, _ => 17f };
                    float wid = lobe switch { 0 => 10f, 1 => 11.5f, 2 => 9f, _ => 7f };

                    // la ONDA: cada pétalo retrasa su vibración (punta→raíz)
                    float lag = lobe * 0.55f;
                    float ang = baseAng + flutter * (0.30f + 0.06f * lobe)
                                * (float)Math.Sin(ctx.FlapPhase - lag) * 1.6f;

                    // en reposo los pétalos se PLEGAN hacia el cuerpo
                    float fold = MathHelper.Lerp(0.48f, 1f, open);
                    ang = -MathHelper.Lerp(-0.35f, -ang, fold);

                    float dirX = (float)Math.Cos(ang) * side;
                    float dirY = (float)Math.Sin(ang) * ctx.GravDir;

                    Vector2 base0 = root;
                    Vector2 tip = root + new Vector2(dirX, dirY) * (len * fold)
                                  + new Vector2(0f, flutter * 1.4f);

                    // vectores perpendiculares (el "ancho" del pétalo)
                    Vector2 perp = new Vector2(-dirY * ctx.GravDir, dirX * side * ctx.GravDir);
                    Vector2 mid = (base0 + tip) * 0.5f;
                    float wMid = wid * fold;

                    // ---- RELLENO translúcido (3 celdas a lo largo) ----
                    for (int c = 0; c < 3; c++)
                    {
                        float t = (c + 0.5f) / 3f;
                        Vector2 cpos = root + new Vector2(dirX, dirY) * (len * fold * t)
                            + new Vector2(0f, flutter * 1.4f * t);
                        // perfil de hoja: hinchado al inicio, puntiagudo al final
                        float profile = (float)Math.Sin(t * MathHelper.Pi) * (1f - t * 0.30f);
                        Vector2 csize = new Vector2(wMid * (0.8f + profile) * 1.9f, len * fold / 3f * 1.5f);
                        float crot = (float)Math.Atan2(dirY, dirX);

                        // volumen ámbar oscuro + tinte dorado (translúcido)
                        VFXCore.Quad(cpos, new Color(124, 68, 16) * (0.46f * ctx.Alpha * fold), csize, crot);
                        VFXCore.Quad(cpos, new Color(212, 128, 34) * (0.30f * ctx.Alpha * fold), csize * 0.8f, crot);
                    }

                    // ---- EL CONTORNO: dos trazos simétricos base→punta ----
                    Vector2 edgeA0 = base0 + perp * (wMid * 0.30f);
                    Vector2 edgeB0 = base0 - perp * (wMid * 0.30f);
                    Vector2 edgeA = mid + perp * (wMid * 0.85f);
                    Vector2 edgeB = mid - perp * (wMid * 0.85f);

                    // gradiente ámbar: base cobre → punta dorada pálida
                    Color cBase = new Color(214, 118, 30);
                    Color cMid = new Color(255, 178, 70);
                    Color cTip = new Color(255, 232, 168);

                    // lado A (dos segmentos con curva)
                    Vector2 bendA = mid + perp * (wMid * 0.72f) + new Vector2(0f, -1.2f * ctx.GravDir);
                    WingStrokes.Stroke(edgeA0, edgeA, 2.1f, cBase, cMid, 0.88f * ctx.Alpha);
                    WingStrokes.Stroke(edgeA, tip, 1.6f, cMid, cTip, 0.88f * ctx.Alpha);
                    // lado B
                    WingStrokes.Stroke(edgeB0, edgeB, 2.1f, cBase, cMid, 0.88f * ctx.Alpha);
                    WingStrokes.Stroke(edgeB, tip, 1.6f, cMid, cTip, 0.88f * ctx.Alpha);

                    // ---- LAS VENAS (trazos de runa dentro del pétalo) ----
                    for (int v = 0; v < 3; v++)
                    {
                        float spread = (v - 1) * 0.34f;   // abanico de la base a la punta
                        Vector2 vdir = new Vector2(
                            (float)Math.Cos(ang + spread) * side,
                            (float)Math.Sin(ang + spread) * ctx.GravDir);
                        Vector2 vtip = base0 + vdir * (len * fold * 0.82f);
                        Vector2 vmid = base0 + vdir * (len * fold * 0.45f) + perp * (wMid * 0.18f * (v - 1));

                        Color v0 = new Color(255, 196, 110);
                        Color v1 = new Color(255, 244, 214);
                        WingStrokes.Stroke(base0, vmid, 1.4f, v0, v0, 0.72f * ctx.Alpha);
                        WingStrokes.Stroke(vmid, vtip, 1.1f, v0, v1, 0.72f * ctx.Alpha);
                    }

                    // ---- LA PERLA DE POLVO ESTELAR en la punta ----
                    float tw = 0.75f + 0.25f * (float)Math.Sin(ctx.Time * 6.5f + lobe * 1.9f + side);
                    WingStrokes.Pearl(tip, 8.0f,
                        new Color(255, 196, 100), new Color(255, 253, 244), 0.95f * tw * ctx.Alpha);

                    // ---- MICRO-PERLAS titilantes en el borde exterior ----
                    for (int k = 0; k < 2; k++)
                    {
                        float h = VFXCore.Hash01(side * 9 + lobe, k * 4 + 1, 3);
                        float mtw = 0.5f + 0.5f * (float)Math.Sin(ctx.Time * (4f + h * 4f) + h * 11f);
                        if (mtw < 0.62f) continue;
                        float t = 0.35f + h * 0.5f;
                        Vector2 ppos = root + new Vector2(dirX, dirY) * (len * fold * t)
                            + perp * (wMid * (0.55f + 0.3f * k));
                        WingStrokes.Pearl(ppos, 4.6f,
                            new Color(255, 214, 130), new Color(255, 255, 250),
                            0.85f * mtw * ctx.Alpha);
                    }
                }

                // núcleo de anclaje en la espalda
                WingStrokes.Pearl(root, 7.5f, new Color(255, 178, 80),
                    new Color(255, 250, 235), 0.9f * ctx.Alpha);
            }
        }
    }
}
