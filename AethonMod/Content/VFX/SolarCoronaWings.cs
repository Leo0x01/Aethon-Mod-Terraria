using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// SolarCoronaWings — v6.08 — LAS ALAS DE CORONA SOLAR.
    ///
    /// Alas hechas de PROMINENCIAS: lazos de plasma que nacen de una
    /// mancha solar en cada omóplato, se arquean hacia fuera y vuelven a
    /// caer — la forma de los lazos coronales de las fotos de la NASA.
    ///
    /// Cada lazo es una CINTA de cuadros rotados por la tangente (nuevo
    /// Quad de VFXCore) con gradiente físico de temperatura: BLANCO
    /// incandescente en la base → dorado → naranja → rojo braza en la cima
    /// del arco, donde el plasma se enfría al estirarse. Las puntas
    /// PARPADEAN con ruido determinista (llamaradas) y al aletear los lazos
    /// se ESTIRAN (las prominencias reales se alargan antes de romperse).
    ///
    /// Detrás: la neblina de la corona (halo naranja amplio) y delante, en
    /// la base, la MANCHA: un núcleo blanco cegador con granulación.
    /// </summary>
    public static class SolarCoronaWings
    {
        /// <summary>Lazos de prominencia por lado.</summary>
        private const int Loops = 4;

        /// <summary>Segmentos por lazo.</summary>
        private const int Segs = 14;

        /// <summary>Pinta las alas de corona solar en el buffer de VFXCore.</summary>
        public static void Render(ref WingDrawContext ctx)
        {
            if (ctx.Alpha <= 0f) return;
            float open = MathHelper.Clamp(ctx.Open, 0f, 1.15f);

            // Golpe asimétrico suave (1.25): el lazo se estira al empujar.
            float stroke = (float)Math.Sin(ctx.FlapPhase);
            float flap = (stroke > 0f ? (float)Math.Pow(stroke, 0.8f) : stroke) * ctx.FlapAmp;
            float o = open * VFXCore.Breathe(ctx.Time, 1.7f, 0f, 0.035f);
            float sweep = MathHelper.Clamp(Math.Abs(ctx.SpeedX) / 9f, 0f, 1f) * 0.45f;

            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 root = ctx.Back + new Vector2(side * 6f, 0f);

                // ============================================================
                //  LA NEBLINA DE LA CORONA (fondo cálido amplio)
                // ============================================================
                Vector2 haze = root + new Vector2(side * 18f * o, -10f * ctx.GravDir * o);
                VFXCore.Quad(haze, new Color(120, 42, 8) * (0.34f * ctx.Alpha * o),
                    new Vector2(72f * (0.6f + 0.4f * o), 54f * (0.6f + 0.4f * o)));
                VFXCore.Quad(haze, new Color(90, 22, 4) * (0.22f * ctx.Alpha * o),
                    new Vector2(48f * (0.6f + 0.4f * o), 36f * (0.6f + 0.4f * o)));

                // ============================================================
                //  LOS LAZOS DE PROMINENCIA (la silueta del ala)
                // ============================================================
                for (int l = 0; l < Loops; l++)
                {
                    float l01 = l / (float)(Loops - 1);   // 0 interior → 1 exterior
                    // El abanico: cada lazo nace un poco más afuera y sube más.
                    float baseOff = (4f + 11f * l01) * (0.45f + 0.55f * o);
                    float height = (26f + 16f * l01) * (0.45f + 0.55f * o)
                                   * (1f + 0.16f * flap);
                    float width = (13f + 7f * l01) * (0.45f + 0.55f * o)
                                  * (1f + sweep * 0.4f + 0.05f * flap);
                    Vector2 loopRoot = root + new Vector2(side * baseOff, 2f * ctx.GravDir);
                    float sway = VFXCore.Sway(ctx.Time, 1.1f + 0.3f * l01, l * 1.7f) * 2.2f;

                    Vector2 prev = loopRoot;
                    for (int s = 0; s <= Segs; s++)
                    {
                        float t = s / (float)Segs;
                        // EL LAZO: nace en la base, sube en arco ELÍPTICO y CAE de
                        // nuevo (una prominencia anclada por los dos pies).
                        float arcX = MathHelper.Lerp(-width * 0.35f, width, t)
                                     * (1f + sweep * 0.3f);
                        float arcY = (float)Math.Sin(t * MathHelper.Pi) * height
                                     * (1f - 0.10f * t);
                        // La cima del arco se barre con el viento solar.
                        arcX += (float)Math.Sin(t * MathHelper.Pi) * sway * (0.4f + l01 * 0.6f);
                        Vector2 pos = loopRoot + new Vector2(
                            side * arcX, -arcY * ctx.GravDir);

                        // Gradiente de temperatura del plasma: la BASE está
                        // pegada a la fotosfera (blanco cegador) y la cima se
                        // enfría (rojo braza).
                        float cooling = (float)Math.Sin(t * MathHelper.Pi); // 1 en la cima
                        Color col;
                        if (cooling < 0.35f)
                            col = Color.Lerp(new Color(255, 250, 235), new Color(255, 210, 110),
                                cooling / 0.35f);
                        else if (cooling < 0.75f)
                            col = Color.Lerp(new Color(255, 210, 110), new Color(255, 130, 40),
                                (cooling - 0.35f) / 0.4f);
                        else
                            col = Color.Lerp(new Color(255, 130, 40), new Color(200, 60, 20),
                                (cooling - 0.75f) / 0.25f);

                        // Grosor: los PIES del lazo son gruesos, la cima es fina.
                        float th = (4.0f - 2.6f * cooling) * (0.6f + 0.4f * o);

                        // CINTA rotada por la tangente.
                        Vector2 delta = pos - prev;
                        float rot = delta.LengthSquared() > 0.0001f
                            ? (float)Math.Atan2(delta.Y, delta.X) : 0f;

                        // LLAMARADAS: las puntas parpadean (ruido agudo).
                        float flick = 1f;
                        if (cooling > 0.55f)
                        {
                            flick = 0.65f + 0.35f * (VFXCore.Hash01(side + l, s, (int)(ctx.Time * 9f)) > 0.45f ? 1f : 0.3f);
                        }
                        VFXCore.Quad(pos, col * (0.55f * flick * ctx.Alpha * (0.5f + 0.5f * o)),
                            new Vector2(th * 3.0f, th * 1.8f), rot, VFXCore.SoftGlow);
                        prev = pos;
                    }

                    // La cima del lazo: chispa blanca al romper la prominencia.
                    Vector2 apex = loopRoot + new Vector2(
                        side * (width * 0.5f + sway), -height * ctx.GravDir * 0.965f);
                    VFXCore.Quad(apex, new Color(255, 235, 180) * (0.55f * ctx.Alpha * (0.5f + 0.5f * o)),
                        new Vector2(4.0f, 4.0f));
                }

                // ============================================================
                //  LA MANCHA SOLAR (la raíz: fotosfera local)
                // ============================================================
                float granule = 0.8f + 0.2f * (float)Math.Sin(ctx.Time * 4.4f + side * 0.9f);
                VFXCore.Quad(root, new Color(255, 246, 225) * (0.85f * granule * ctx.Alpha),
                    new Vector2(9.5f, 9.5f));
                VFXCore.Quad(root, new Color(255, 200, 110) * (0.45f * granule * ctx.Alpha),
                    new Vector2(19f, 19f));
                VFXCore.Quad(root, new Color(255, 130, 40) * (0.28f * granule * ctx.Alpha),
                    new Vector2(32f, 32f));
            }
        }
    }
}
