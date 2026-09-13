using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// ButterflyWings — v6.13 — LA MARIPOSA CÓSMICA "VITRAL".
    ///
    /// Rediseño con la técnica de las coronas: la mariposa es un VITRAL
    /// ESTELAR — el CONTORNO de cada lóbulo es una cadena de trazos dorados
    /// (el gesto de pincel de las runas), las VENAS son trazos pálidos que
    /// nacen de la raíz como glifos, y las CELDAS entre vena y vena son
    /// cristales translúcidos con volumen oscuro debajo. Dos OJOS de ala
    /// (perlas grandes con núcleo blanco) anclan la mirada y perlas menores
    /// festonean el borde exterior.
    ///
    /// El golpe sigue siendo el de una mariposa REAL (asimétrico 1.7 — la
    /// bajada rápida da el empuje, la subida es lenta) y el lóbulo inferior
    /// RETRASA su fase tras el superior.
    /// </summary>
    public static class ButterflyWings
    {
        /// <summary>Venas por lóbulo superior.</summary>
        private const int Veins = 5;

        public static void Render(ref WingDrawContext ctx)
        {
            if (ctx.Alpha <= 0f) return;
            float open = MathHelper.Clamp(ctx.Open, 0f, 1.15f);

            // golpe asimétrico de mariposa
            float stroke = (float)Math.Sin(ctx.FlapPhase);
            float flap = stroke > 0f
                ? (float)Math.Pow(stroke, 0.75f) * ctx.FlapAmp
                : stroke * 1.25f * ctx.FlapAmp;

            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 root = ctx.Back + new Vector2(side * 4f, -2f * ctx.GravDir);

                for (int lobe = 0; lobe < 2; lobe++)
                {
                    bool upper = lobe == 0;
                    // plano del ala: reposo casi VERTICAL (mariposa cerrada)
                    float flapAngle = (upper ? flap : flap * 0.72f) * 0.52f;
                    float foldAngle = MathHelper.Lerp(1.22f, 0.10f, open);
                    float plane = foldAngle + flapAngle;

                    float pc = (float)Math.Cos(plane), ps = (float)Math.Sin(plane);

                    // tamaño del lóbulo (el superior domina)
                    float W = (upper ? 44f : 26f) * (0.40f + 0.60f * open);
                    float H = (upper ? 40f : 24f) * (0.40f + 0.60f * open);

                    Vector2 lobeRoot = root + (upper
                        ? new Vector2(side * 2f, -1f * ctx.GravDir)
                        : new Vector2(side * 5f, 4f * ctx.GravDir));

                    // local → mundo: el eje +x del lóbulo se abre con el plano
                    Vector2 L2W(Vector2 local, Vector2 lRoot, float cosP, float sinP, int sd, float grav)
                        => lRoot + new Vector2(
                            local.X * cosP - local.Y * sinP * grav,
                            local.X * sinP * grav + local.Y * cosP) * new Vector2(sd, 1f);

                    // ============ EL CONTORNO (cadena de trazos dorados) ============
                    // Silueta papilionidae: redondeado con "rabo" en la punta.
                    Vector2[] outline = new Vector2[11];
                    for (int s = 0; s <= 10; s++)
                    {
                        float u = s / 10f;
                        float theta = MathHelper.Lerp(0.14f, 2.5f, u);
                        float r = 1f - 0.16f * (float)Math.Sin(u * MathHelper.Pi);
                        outline[s] = L2W(new Vector2(
                            (float)Math.Cos(theta) * r * (0.52f + 0.48f * (float)Math.Sin(theta + 0.4f)) * W,
                            (float)Math.Sin(theta) * r * H), lobeRoot, pc, ps, side, ctx.GravDir);
                    }

                    // ============ LAS VENAS (trazos que nacen de la raíz) ============
                    Vector2[] veinTips = new Vector2[Veins];
                    for (int v = 0; v < Veins; v++)
                    {
                        float u = (v + 1) / (float)(Veins + 1);
                        // la vena apunta a un punto del contorno repartido
                        int oi = 1 + (int)(u * 8.5f);
                        oi = Math.Min(oi, 10);
                        veinTips[v] = outline[oi];
                    }

                    // ============ LAS CELDAS (cristales translúcidos) ============
                    // Entre venas contiguas: cristales con volumen oscuro +
                    // tinte brillante (vitral con luz detrás), hacia el borde.
                    for (int cell = 0; cell < Veins; cell++)
                    {
                        Vector2 a = veinTips[cell];
                        Vector2 b = cell + 1 < Veins ? veinTips[cell + 1] : outline[10];

                        for (int k = 0; k < 3; k++)
                        {
                            float t = (k + 0.5f) / 3f;
                            // posición: a medio camino entre la raíz y el arco ab
                            Vector2 cellPos = lobeRoot +
                                ((a - lobeRoot) * (0.28f + 0.34f * t) +
                                 (b - lobeRoot) * (0.28f + 0.34f * t)) * 0.5f;

                            // cristal: volumen violeta profundo + tinte magenta
                            VFXCore.Quad(cellPos, new Color(64, 22, 104) * (0.50f * ctx.Alpha),
                                new Vector2(W * 0.30f, H * 0.26f) * (1f - t * 0.25f),
                                plane * side * ctx.GravDir);
                            VFXCore.Quad(cellPos, new Color(168, 62, 178) * (0.34f * ctx.Alpha),
                                new Vector2(W * 0.22f, H * 0.19f) * (1f - t * 0.25f),
                                plane * side * ctx.GravDir);
                        }
                    }

                    // ============ EL CUERPO del vitral ============
                    // volumen maestro bajo TODO el lóbulo (silueta a distancia)
                    Vector2 lobeMid = L2W(new Vector2(W * 0.5f, 0f), lobeRoot, pc, ps, side, ctx.GravDir);
                    VFXCore.Quad(lobeMid, new Color(46, 16, 76) * (0.42f * ctx.Alpha),
                        new Vector2(W * 0.9f, H * 0.85f), plane * side * ctx.GravDir);
                    VFXCore.Quad(lobeMid, new Color(130, 44, 150) * (0.22f * ctx.Alpha),
                        new Vector2(W * 0.72f, H * 0.68f), plane * side * ctx.GravDir);

                    // ============ VENAS pálidas (los "glifos" del vitral) ============
                    for (int v = 0; v < Veins; v++)
                    {
                        // la vena curva: control a medio camino, desviada alborde
                        Vector2 tip = veinTips[v];
                        Vector2 dir = tip - lobeRoot;
                        Vector2 bend = lobeRoot + dir * 0.55f +
                            new Vector2(0f, -H * 0.14f * ctx.GravDir * (v - 2) * 0.4f);
                        Vector2 mid1 = WingStrokes.Bezier(lobeRoot, bend, tip, 0.34f);
                        Vector2 mid2 = WingStrokes.Bezier(lobeRoot, bend, tip, 0.68f);

                        Color vcol0 = new Color(255, 132, 196);
                        Color vcol1 = new Color(255, 214, 238);
                        WingStrokes.Stroke(lobeRoot, mid1, 2.0f, vcol0, vcol0, 0.80f * ctx.Alpha);
                        WingStrokes.Stroke(mid1, mid2, 1.7f, vcol0, vcol1, 0.80f * ctx.Alpha);
                        WingStrokes.Stroke(mid2, tip, 1.4f, vcol1, vcol1, 0.80f * ctx.Alpha);
                    }

                    // ============ EL CONTORNO dorado (encima de todo) ============
                    WingStrokes.Chain(outline, 2.6f,
                        new Color(255, 178, 92), new Color(255, 232, 168), 0.92f * ctx.Alpha);
                    // filo interior del borde (eco de la corona)
                    Vector2[] echo = new Vector2[9];
                    for (int s = 0; s < 9; s++)
                    {
                        int oi = (int)(s / 8f * 10f);
                        echo[s] = Vector2.Lerp(lobeRoot, outline[oi], 0.86f);
                    }
                    WingStrokes.Chain(echo, 1.2f,
                        new Color(255, 210, 140), new Color(255, 240, 210), 0.45f * ctx.Alpha);

                    // ============ LOS OJOS DEL ALA (perlas grandes) ============
                    if (upper)
                    {
                        Vector2 eyePos = Vector2.Lerp(lobeRoot, outline[6], 0.62f);
                        float eyeTw = 0.8f + 0.2f * (float)Math.Sin(ctx.Time * 2.1f + side * 1.2f);
                        WingStrokes.Pearl(eyePos, 11.5f, new Color(140, 60, 190),
                            new Color(246, 238, 255), 0.95f * eyeTw * ctx.Alpha);
                        // anillo del ojo (trazo corto alrededor)
                        VFXCore.Quad(eyePos, new Color(255, 218, 150) * (0.75f * ctx.Alpha),
                            VFXCore.RingQuadSize(7.5f));
                    }

                    // perlas menores festoneando el borde exterior
                    for (int p = 0; p < 3; p++)
                    {
                        int oi = 3 + p * 3;
                        if (oi > 10) oi = 10;
                        float tw = 0.6f + 0.4f * (float)Math.Sin(ctx.Time * 3.4f + oi * 2.6f);
                        WingStrokes.Pearl(outline[oi], 5.2f,
                            new Color(255, 196, 130), new Color(255, 250, 240),
                            0.8f * tw * ctx.Alpha);
                    }
                }
            }
        }
    }
}
