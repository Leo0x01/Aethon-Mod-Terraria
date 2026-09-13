using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// EclipseWings — v6.13 — LAS ALAS DE ECLIPSE TOTAL (ronda 2).
    ///
    /// La ronda 1 (discos opacos) NO pasó la validación VLM: "se ven como
    /// globos de jabón, no como alas". REDISEÑO con la técnica de las
    /// coronas y la primitiva que SÍ lee como ala: la PLUMA.
    ///
    /// El look "eclipse total de un ángel caído": SEIS plumas NEGRAS por
    /// lado (volúmenes azul-noche casi opacos — la luna nueva) cuyas
    /// PUNTAS arden en blanco-caliente (el anillo cromosférico de la
    /// totalidad: perlas blancas + filo pálido al final de cada pluma), un
    /// abanico de RAYOS DE CORONA pálidos radiando por DETRÁS (donde estaría
    /// el sol tapado) y un pequeño DISCO DE ECLIPSE en el hombro — negro
    /// con su aro fino blanco — de donde nace todo.
    ///
    /// Majestad lenta: las plumas se mecen con fases desfasadas y en reposo
    /// se pliegan verticalmente tras la espalda.
    /// </summary>
    public static class EclipseWings
    {
        /// <summary>Plumas negras por lado.</summary>
        private const int Feathers = 6;

        /// <summary>Rayos de corona por lado.</summary>
        private const int CoronaRays = 5;

        /// <summary>Segmentos del aro del disco del hombro.</summary>
        private const int ShoulderRimSegs = 12;

        public static void Render(ref WingDrawContext ctx)
        {
            if (ctx.Alpha <= 0f) return;
            float open = MathHelper.Clamp(ctx.Open, 0f, 1.1f);

            float flap = (float)Math.Sin(ctx.FlapPhase) * ctx.FlapAmp;

            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 root = ctx.Back + new Vector2(side * 4f, -2f * ctx.GravDir);

                // ============ LOS RAYOS DE CORONA (DETRÁS de las plumas) ============
                // Trazos pálidos radiando desde el "sol tapado" de la espalda —
                // se dibujan PRIMERO: nacen por detrás del abanico negro.
                for (int ray = 0; ray < CoronaRays; ray++)
                {
                    float h = VFXCore.Hash01(side * 7, ray * 5 + 1, 3);
                    // repartidos en el semiplano superior, sesgados afuera
                    float th = -MathHelper.Pi * 0.88f + ray / (float)(CoronaRays - 1) * MathHelper.Pi * 1.05f +
                               (h - 0.5f) * 0.38f;
                    // ondean (viento de la corona)
                    th += 0.10f * (float)Math.Sin(ctx.Time * 2.1f + ray * 1.9f + h * 6f);

                    float rayLen = MathHelper.Lerp(16f, 34f, h) * (0.45f + 0.55f * open) *
                                   (0.85f + 0.15f * (float)Math.Sin(ctx.Time * 1.6f + ray * 2.4f));

                    Vector2 dir = new Vector2((float)Math.Cos(th) * side, (float)Math.Sin(th) * ctx.GravDir);
                    if (dir.LengthSquared() < 0.001f) continue;
                    dir.Normalize();

                    Vector2 a = root + dir * (9f + 5f * open);
                    Vector2 b = root + dir * (9f + 5f * open + rayLen);
                    Vector2 bend = root + dir * (9f + 5f * open + rayLen * 0.5f) +
                        new Vector2(0f, -2.5f * ctx.GravDir);

                    Vector2 m1 = WingStrokes.Bezier(a, bend, b, 0.35f);
                    Vector2 m2 = WingStrokes.Bezier(a, bend, b, 0.7f);
                    Color rc0 = new Color(255, 250, 235);
                    Color rc1 = new Color(196, 210, 255);
                    WingStrokes.Stroke(a, m1, 1.8f, rc0, rc0, 0.50f * ctx.Alpha);
                    WingStrokes.Stroke(m1, m2, 1.5f, rc0, rc1, 0.45f * ctx.Alpha);
                    WingStrokes.Stroke(m2, b, 1.2f, rc1, rc1, 0.36f * ctx.Alpha);
                }

                // ============ EL DISCO DE ECLIPSE EN EL HOMBRO ============
                // La "luna nueva" de la que nacen las plumas: disco negro
                // opaco + aro fino blanco (la totalidad en miniatura).
                float voidR = 6.0f + 1.5f * open;
                VFXCore.Quad(root, new Color(14, 12, 32) * (0.94f * ctx.Alpha),
                    new Vector2(voidR * 2.3f, voidR * 2.3f), VFXCore.GlowOrb);
                for (int s = 0; s < ShoulderRimSegs; s++)
                {
                    float th = s * MathHelper.TwoPi / ShoulderRimSegs;
                    float th2 = (s + 1) * MathHelper.TwoPi / ShoulderRimSegs;
                    Vector2 a = root + new Vector2((float)Math.Cos(th), (float)Math.Sin(th) * ctx.GravDir) * (voidR * 1.22f);
                    Vector2 b = root + new Vector2((float)Math.Cos(th2), (float)Math.Sin(th2) * ctx.GravDir) * (voidR * 1.22f);
                    float height = 0.5f + 0.5f * (float)Math.Sin(th + MathHelper.PiOver2);
                    Color rc = Color.Lerp(new Color(205, 216, 255), new Color(255, 253, 248), height);
                    WingStrokes.Stroke(a, b, MathHelper.Lerp(1.2f, 1.9f, height), rc, rc,
                        (0.4f + 0.5f * height) * ctx.Alpha);
                }

                // ============ LAS PLUMAS NEGRAS (la silueta del ala) ============
                // Seis plumas azul-noche casi opacas; las PUNTAS arden en
                // blanco (la cromosfera asomando tras la luna negra).
                for (int f = 0; f < Feathers; f++)
                {
                    float u = f / (float)(Feathers - 1);              // 0 arriba → 1 abajo
                    float baseAng = MathHelper.Lerp(-0.60f, 0.55f, u);
                    float lag = f * 0.45f;
                    float flapAng = (float)Math.Sin(ctx.FlapPhase - lag) * ctx.FlapAmp * 0.34f;

                    // reposo: el abanico se pliega casi vertical tras la espalda
                    float fold = MathHelper.Lerp(0.42f, 1f, open);
                    float ang = (baseAng + flapAng) * fold;

                    // longitud: la central domina (silueta de ala de verdad)
                    float L = MathHelper.Lerp(30f, 50f, (float)Math.Sin(u * MathHelper.Pi)) *
                              (0.50f + 0.50f * open);

                    float sweep = MathHelper.Clamp(ctx.SpeedX * ctx.Direction * 0.04f, -0.45f, 0.45f);

                    Vector2 dir = new Vector2((float)Math.Cos(ang) * side, (float)Math.Sin(ang) * ctx.GravDir);
                    Vector2 tip = root + dir * L;
                    float bendAmt = (0.34f + sweep * 0.7f) * fold;
                    Vector2 bend = root + dir * (L * 0.52f) +
                        new Vector2(-side * (float)Math.Cos(ang) * L * bendAmt * 0.45f, 0f);

                    // plumas de la luna nueva: raíz negro-azul → punta gris-perla
                    // (el graduado hace que la pluma se lea aunque sea oscura)
                    Color cRoot = new Color(26, 22, 54);
                    Color cMid = new Color(52, 46, 96);
                    Color cTip = new Color(122, 118, 168);
                    Color dark = new Color(10, 9, 26);

                    float wRoot = 6.4f + 2.4f * (1f - Math.Abs(u - 0.45f));
                    WingStrokes.Feather(root, tip, bend, wRoot, 2.0f, cRoot, cMid, cTip, dark,
                        ctx.Alpha * (0.80f + 0.20f * open), true, 0.80f, 3.0f);

                    // la PUNTA CROMOSFÉRICA: perla blanco-caliente + micro destello
                    // (el anillo de la totalidad vive en las puntas de las plumas)
                    float rimTw = 0.75f + 0.25f * (float)Math.Sin(ctx.Time * 2.8f + f * 1.7f + side);
                    WingStrokes.Pearl(tip, 8.5f, new Color(216, 226, 255),
                        new Color(255, 253, 250), 0.95f * rimTw * ctx.Alpha);
                    if (f % 2 == 0)
                        WingStrokes.Flare4(tip, 9f, 1.8f, new Color(228, 236, 255),
                            0.5f * rimTw * ctx.Alpha);
                }

                // ============ EL DESTELLO DE TOTALIDAD sobre el abanico ============
                // (solo visible al volar: el momento exacto del eclipse)
                if (open > 0.65f)
                {
                    float tPulse = 0.7f + 0.3f * (float)Math.Sin(ctx.Time * 2.4f);
                    float vis = (open - 0.65f) / 0.35f;
                    Vector2 crown = root + new Vector2(0f, -26f * ctx.GravDir * open);
                    WingStrokes.Flare4(crown, 16f, 2.2f, new Color(235, 240, 255),
                        0.55f * tPulse * vis * ctx.Alpha);
                }
            }
        }
    }
}
