using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// NebulaWings — v6.13 — LA NEBULOSA VIVA "NUBE DE ESTRELLAS".
    ///
    /// Rediseño con la técnica de las coronas (ronda 2 tras la validación
    /// VLM: los blobs solos se leían como "manchas de humo", no como alas):
    /// el ala tiene ahora un ESQUELETO DE PLUMAS MAESTRAS — SEIS plumas
    /// gruesas en abanico (volumen púrpura profundo + filo magenta) que
    /// definen la SILUETA de ala — y la NEBULOSA vive ALREDEDOR: blobs de
    /// gas que respiran (deriva orgánica, púrpura/teal/magenta), TRES
    /// filamentos fucsia serpentean entre las plumas y CINCO ESTRELLAS
    /// firmadas como en las coronas: perla de núcleo blanco + DESTELLO DE
    /// DIFRACCIÓN de 4 puntas (la cruz del Hubble).
    ///
    /// El conjunto RESPIRA (AlwaysFlutter): los blobs se hinchan y las
    /// estrellas titilan con fases propias — nunca está quieta.
    /// </summary>
    public static class NebulaWings
    {
        /// <summary>Plumas maestras (el esqueleto del ala) por lado.</summary>
        private const int Quills = 6;

        /// <summary>Blobs de nube por lado.</summary>
        private const int Blobs = 6;

        /// <summary>Estrellas (perlas + difracción) por lado.</summary>
        private const int Stars = 5;

        /// <summary>Filamentos por lado.</summary>
        private const int Filaments = 3;

        public static void Render(ref WingDrawContext ctx)
        {
            if (ctx.Alpha <= 0f) return;
            float open = MathHelper.Clamp(ctx.Open, 0f, 1.1f);

            // la respiración global de la nube (lenta, orgánica)
            float breathe = VFXCore.Breathe(ctx.Time, 1.3f, 0f, 0.05f);
            float flap = (float)Math.Sin(ctx.FlapPhase) * ctx.FlapAmp * 0.4f;

            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 root = ctx.Back + new Vector2(side * 3f, -2f * ctx.GravDir);

                // ============ EL ESQUELETO: PLUMAS MAESTRAS (ronda 2) ============
                // SEIS plumas gruesas en abanico — la SILUETA de ala que la
                // nube envuelve. Sin esto el ala era "humo".
                for (int q = 0; q < Quills; q++)
                {
                    float u = q / (float)(Quills - 1);               // 0 arriba → 1 abajo
                    float baseAng = MathHelper.Lerp(-0.72f, 0.52f, u);
                    float lag = q * 0.38f;
                    float flapAng = (float)Math.Sin(ctx.FlapPhase - lag) * ctx.FlapAmp * 0.30f;
                    float fold = MathHelper.Lerp(0.46f, 1f, open);
                    float ang = (baseAng + flapAng) * fold;

                    float L = MathHelper.Lerp(26f, 42f, (float)Math.Sin(u * MathHelper.Pi)) *
                              (0.48f + 0.52f * open);

                    Vector2 dir = new Vector2((float)Math.Cos(ang) * side, (float)Math.Sin(ang) * ctx.GravDir);
                    Vector2 tip = root + dir * L;
                    // curva suave hacia atrás (ala plegada en reposo)
                    float bendAmt = 0.30f * fold;
                    Vector2 bend = root + dir * (L * 0.5f) +
                        new Vector2(-side * (float)Math.Cos(ang) * L * bendAmt * 0.4f, 0f);

                    // plumas nebulosas: volumen púrpura profundo + filo magenta
                    float wRoot = 6.0f + 2.0f * (1f - Math.Abs(u - 0.45f));
                    WingStrokes.Feather(root, tip, bend, wRoot, 1.8f,
                        new Color(84, 40, 130), new Color(150, 78, 190), new Color(216, 130, 235),
                        new Color(30, 14, 52), ctx.Alpha * (0.75f + 0.25f * open), true, 0.62f, 3.0f);
                }

                // ============ LA NUBE (blobs con deriva determinista) ============
                for (int b = 0; b < Blobs; b++)
                {
                    float h1 = VFXCore.Hash01(side * 17 + b, b * 3 + 1, 7);
                    float h2 = VFXCore.Hash01(side * 17 + b, b * 5 + 2, 11);
                    float h3 = VFXCore.Hash01(side * 17 + b, b * 7 + 3, 13);

                    // posición de deriva (lenta, orgánica, determinista)
                    float driftX = (h1 - 0.5f) * 8f * (float)Math.Sin(ctx.Time * 0.4f + h2 * 9f);
                    float driftY = (h2 - 0.5f) * 10f * (float)Math.Sin(ctx.Time * 0.33f + h1 * 7f);

                    // reparto en abanico de ala (0 arriba → 1 abajo)
                    float u = b / (float)(Blobs - 1);
                    float reach = MathHelper.Lerp(18f, 40f, (float)Math.Sin(u * MathHelper.Pi)) *
                                  (0.40f + 0.60f * open);
                    float ang = MathHelper.Lerp(-0.75f, 0.55f, u) + flap * 0.15f * (1f - u);

                    Vector2 pos = root + new Vector2(
                        (float)Math.Cos(ang) * reach * side + driftX,
                        (float)Math.Sin(ang) * reach * 0.7f * ctx.GravDir + driftY * ctx.GravDir);

                    // tamaño del blob (los centrales dominan)
                    float size = MathHelper.Lerp(14f, 26f, (float)Math.Sin(u * MathHelper.Pi)) *
                                 (0.5f + 0.5f * open) * breathe;

                    // paleta de nube: púrpura profundo / teal / magenta (mezcla por blob)
                    Color cloudDark = (b % 3) switch
                    {
                        0 => new Color(64, 26, 104),    // púrpura noche
                        1 => new Color(18, 72, 88),     // teal profundo
                        _ => new Color(96, 26, 84),     // magenta oscuro
                    };
                    Color cloudLight = (b % 3) switch
                    {
                        0 => new Color(142, 84, 208),
                        1 => new Color(58, 158, 178),
                        _ => new Color(212, 92, 176),
                    };

                    // volumen + tinte (la nube translúcida — MENOS alfa que las
                    // plumas: el esqueleto manda, el gas acompaña)
                    VFXCore.Quad(pos, cloudDark * (0.36f * ctx.Alpha * (0.55f + 0.45f * open)),
                        new Vector2(size * 2.0f, size * 1.7f), ang);
                    VFXCore.Quad(pos, cloudLight * (0.26f * ctx.Alpha * (0.55f + 0.45f * open)),
                        new Vector2(size * 1.4f, size * 1.2f), ang);
                }

                // ============ LOS FILAMENTOS (trazos que serpentean) ============
                for (int f = 0; f < Filaments; f++)
                {
                    float h1 = VFXCore.Hash01(side * 23 + f, f * 9 + 4, 17);
                    float fAng = MathHelper.Lerp(-0.55f, 0.35f, f / (float)(Filaments - 1)) + flap * 0.1f;
                    float fLen = MathHelper.Lerp(26f, 42f, h1) * (0.40f + 0.60f * open);

                    // el filamento ondula (serpiente de luz)
                    Vector2 prev = root + new Vector2(side * 5f, h1 * 6f * ctx.GravDir);
                    for (int s = 1; s <= 5; s++)
                    {
                        float u = s / 5f;
                        float wob = 4.5f * (float)Math.Sin(u * 6.5f + ctx.Time * 1.6f + h1 * 8f);
                        Vector2 p = root + new Vector2(
                            (float)Math.Cos(fAng) * fLen * u * side,
                            (float)Math.Sin(fAng) * fLen * 0.6f * u * ctx.GravDir) +
                            new Vector2(wob * 0.4f, wob * ctx.GravDir);

                        Color fa = Color.Lerp(new Color(190, 70, 200), new Color(240, 150, 255), u);
                        WingStrokes.Stroke(prev, p, 1.7f, fa, fa, 0.62f * ctx.Alpha);
                        prev = p;
                    }
                }

                // ============ LAS ESTRELLAS (perlas + difracción 4 puntas) ============
                for (int s = 0; s < Stars; s++)
                {
                    float h1 = VFXCore.Hash01(side * 29 + s, s * 11 + 5, 19);
                    float h2 = VFXCore.Hash01(side * 29 + s, s * 13 + 6, 23);

                    // posición estelar (deriva MUY lenta)
                    float u = h1;
                    float reach = MathHelper.Lerp(14f, 44f, u) * (0.42f + 0.58f * open);
                    float ang = MathHelper.Lerp(-0.85f, 0.65f, h2) + flap * 0.12f;
                    Vector2 pos = root + new Vector2(
                        (float)Math.Cos(ang) * reach * side,
                        (float)Math.Sin(ang) * reach * 0.75f * ctx.GravDir) +
                        new Vector2(
                            2.5f * (float)Math.Sin(ctx.Time * 0.5f + h1 * 9f),
                            2.5f * (float)Math.Cos(ctx.Time * 0.45f + h2 * 7f) * ctx.GravDir);

                    // titileo propio (cada estrella con su fase)
                    float tw = 0.55f + 0.45f * (float)Math.Sin(ctx.Time * (1.8f + h2 * 2.6f) + h1 * 12f);
                    if (tw < 0.30f) continue;

                    // la PERLA estelar (núcleo blanco casi puro)
                    WingStrokes.Pearl(pos, 9.0f, new Color(220, 190, 255),
                        new Color(255, 255, 252), 0.95f * tw * ctx.Alpha);

                    // el DESTELLO DE DIFRACCIÓN (la cruz del Hubble)
                    WingStrokes.Flare4(pos, 13f + 6f * h2, 2.1f,
                        new Color(235, 215, 255), 0.75f * tw * ctx.Alpha);
                }

                // núcleo de la nebulosa en la espalda (estrella mayor)
                float coreTw = 0.8f + 0.2f * (float)Math.Sin(ctx.Time * 2.2f);
                WingStrokes.Pearl(root, 10f, new Color(200, 160, 255),
                    new Color(255, 252, 255), 0.95f * coreTw * ctx.Alpha);
                WingStrokes.Flare4(root, 15f, 2.2f, new Color(225, 200, 255),
                    0.6f * coreTw * ctx.Alpha);
            }
        }
    }
}
