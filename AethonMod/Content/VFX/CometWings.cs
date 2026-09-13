using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// CometWings — v6.13 — LAS ALAS DE COMETA CARMESÍ (ronda 2).
    ///
    /// La ronda 1 (estelas finas) NO pasó la validación VLM: "se leen como
    /// jets de propulsión". REDISEÑO en VELAS DE PLASMA: cada ala es un
    /// abanico de TRES VELAS GORDAS — estelas corpulentas (7.5px en la
    /// base) con MEMBRANA translúcida de sustentación ENTRE ellas (la
    /// superficie del ala), gradiente blanco-candente → carmesí → rosa
    /// tenue, ONDA de brillo viajando hacia fuera (el plasma fluye) y
    /// CABEZAS de cometa en el hombro (perla + DESTELLO DE 4 PUNTAS).
    ///
    /// La cola RESPONDE a la velocidad: cuanta más prisa, más se estira y
    /// se barre hacia atrás (personalidad de cometa real).
    /// </summary>
    public static class CometWings
    {
        /// <summary>Estelas-vela por lado.</summary>
        private const int Streaks = 3;

        /// <summary>Búfer de trabajo para las velas (sin GC por frame).</summary>
        private static readonly Vector2[] _streakHeads = new Vector2[Streaks];
        private static readonly Vector2[] _streakTips = new Vector2[Streaks];
        private static readonly Vector2[] _streakBends = new Vector2[Streaks];

        public static void Render(ref WingDrawContext ctx)
        {
            if (ctx.Alpha <= 0f) return;
            float open = MathHelper.Clamp(ctx.Open, 0f, 1.1f);

            float flap = (float)Math.Sin(ctx.FlapPhase) * ctx.FlapAmp;

            // la respuesta a la velocidad: colas más largas y barridas
            float speedT = MathHelper.Clamp(Math.Abs(ctx.SpeedX) / 9f, 0f, 1f);

            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 root = ctx.Back + new Vector2(side * 3f, -2f * ctx.GravDir);

                // ============ EL NÚCLEO DEL COMETA en la espalda ============
                float corePulse = 0.85f + 0.15f * (float)Math.Sin(ctx.Time * 4.2f);
                VFXCore.Quad(root, new Color(120, 16, 30) * (0.5f * ctx.Alpha),
                    new Vector2(22f, 22f));
                VFXCore.Quad(root, new Color(255, 90, 70) * (0.55f * ctx.Alpha),
                    new Vector2(12f, 12f) * corePulse);

                // ============ LAS TRES VELAS ============
                for (int k = 0; k < Streaks; k++)
                {
                    // cada vela con su ángulo, largo y retardo propios
                    float u = k / (float)(Streaks - 1);
                    float baseAng = MathHelper.Lerp(-0.58f, 0.46f, u);
                    float lag = k * 0.5f;
                    float flapAng = (float)Math.Sin(ctx.FlapPhase - lag) * ctx.FlapAmp * 0.3f;

                    // en reposo las velas se recogen
                    float fold = MathHelper.Lerp(0.46f, 1f, open);
                    float ang = (baseAng + flapAng) * fold;

                    // LONGITUD: base + velocidad (la cola crece con la prisa)
                    float L = MathHelper.Lerp(34f, 50f, (float)Math.Sin(u * MathHelper.Pi)) *
                              (0.50f + 0.50f * open) * (1f + speedT * 0.55f);

                    // el barrido: la velocidad DOBLA las colas hacia atrás
                    float sweep = MathHelper.Clamp(ctx.SpeedX * ctx.Direction * 0.05f, -0.5f, 0.5f);
                    float bendAmt = (0.32f + sweep * 0.7f + speedT * 0.22f) * fold;

                    Vector2 dir = new Vector2((float)Math.Cos(ang) * side, (float)Math.Sin(ang) * ctx.GravDir);
                    Vector2 head = root + dir * (7f + 4f * k) * fold;
                    Vector2 tip = head + dir * L;

                    // punto de control: curva hacia ATRÁS (la vela se barre)
                    Vector2 bend = head + dir * (L * 0.5f) +
                        new Vector2(-side * (float)Math.Cos(ang) * L * bendAmt * 0.5f, 0f);

                    _streakHeads[k] = head;
                    _streakTips[k] = tip;
                    _streakBends[k] = bend;

                    // ---- LA VELA (gradiente + onda viajera) ----
                    Vector2 prev = head;
                    for (int s = 1; s <= 7; s++)
                    {
                        float t = s / 7f;
                        Vector2 p = WingStrokes.Bezier(head, bend, tip, t);

                        // ONDA de brillo VIAJANDO hacia fuera (el plasma fluye)
                        float wave = 0.72f + 0.28f * (float)Math.Sin(t * 7f - ctx.Time * 6.5f + u * 2f);
                        // desvanecimiento hacia la punta
                        float fade = 1f - t * t * 0.55f;

                        // grosor: VELA GORDA en la base (superficie de
                        // sustentación), un hilo en la punta
                        float w = MathHelper.Lerp(7.5f, 1.2f, t) * (0.7f + 0.3f * open);

                        // gradiente: blanco-candente → carmesí → rosa tenue
                        Color ca = StreakColor(t - 1f / 7f);
                        Color cb = StreakColor(t);

                        // volumen oscuro bajo la luz (silueta)
                        WingStrokes.Volume(prev, p, w * 2.2f, new Color(70, 10, 22),
                            0.40f * ctx.Alpha * fade);
                        // el trazo brillante
                        WingStrokes.Stroke(prev, p, w * 1.5f, ca, cb,
                            0.88f * ctx.Alpha * fade * wave);

                        prev = p;
                    }

                    // ---- LA CABEZA DEL COMETA ----
                    // perla incandescente + destello de 4 puntas
                    float hPulse = 0.8f + 0.2f * (float)Math.Sin(ctx.Time * 5.5f + k * 1.8f);
                    VFXCore.Quad(head, new Color(255, 120, 80) * (0.5f * hPulse * ctx.Alpha),
                        new Vector2(16f, 16f));
                    WingStrokes.Pearl(head, 9.5f, new Color(255, 140, 100),
                        new Color(255, 252, 248), 0.95f * hPulse * ctx.Alpha);
                    WingStrokes.Flare4(head, 12f, 2.2f, new Color(255, 200, 170),
                        0.65f * hPulse * ctx.Alpha);
                }

                // ============ LA MEMBRANA DE SUSTENTACIÓN (ronda 2) ============
                // Superficie translúcida ENTRE las velas: lo que faltaba para
                // que se lea como ALA y no como jets de propulsión.
                for (int m = 0; m < Streaks - 1; m++)
                {
                    for (int s = 1; s <= 5; s++)
                    {
                        float t = s / 5f;
                        Vector2 a = WingStrokes.Bezier(_streakHeads[m], _streakBends[m], _streakTips[m], t);
                        Vector2 b = WingStrokes.Bezier(_streakHeads[m + 1], _streakBends[m + 1], _streakTips[m + 1], t);
                        Vector2 mid = (a + b) * 0.5f;
                        float wOut = Vector2.Distance(a, b);
                        if (wOut < 1f) continue;
                        float fade = 1f - t * t * 0.6f;
                        float rotM = (float)Math.Atan2(b.Y - a.Y, b.X - a.X);
                        // volumen carmesí oscuro + tinte rojo translúcido
                        VFXCore.Quad(mid, new Color(96, 14, 26) * (0.38f * ctx.Alpha * fade),
                            new Vector2(wOut, 9f * (1f - t * 0.5f)), rotM);
                        VFXCore.Quad(mid, new Color(232, 60, 60) * (0.26f * ctx.Alpha * fade),
                            new Vector2(wOut * 0.8f, 7f * (1f - t * 0.5f)), rotM);
                    }
                }

                // ============ MICRO-CHISPAS en las velas ============
                for (int sp = 0; sp < 8; sp++)
                {
                    float h1 = VFXCore.Hash01(side * 31 + sp, sp * 7 + 1, 5);
                    float h2 = VFXCore.Hash01(side * 31 + sp, sp * 9 + 2, 9);
                    float tw = 0.5f + 0.5f * (float)Math.Sin(ctx.Time * (3f + h2 * 4f) + h1 * 15f);
                    if (tw < 0.58f) continue;

                    float ang = MathHelper.Lerp(-0.55f, 0.35f, h1);
                    float reach = (8f + h2 * 38f) * (0.45f + 0.55f * open);
                    // las chispas también se barren con la velocidad
                    reach *= 1f + speedT * 0.4f;
                    Vector2 pos = root + new Vector2(
                        (float)Math.Cos(ang) * reach * side -
                        side * speedT * reach * 0.35f,
                        (float)Math.Sin(ang) * reach * 0.6f * ctx.GravDir);

                    WingStrokes.Pearl(pos, 5.5f, new Color(255, 150, 130),
                        new Color(255, 252, 250), 0.85f * tw * ctx.Alpha);
                }
            }
        }

        /// <summary>Gradiente de la vela: candente → carmesí → rosa tenue.</summary>
        private static Color StreakColor(float t)
        {
            if (t < 0.25f)
                return Color.Lerp(new Color(255, 244, 232), new Color(255, 120, 80), t / 0.25f);
            if (t < 0.6f)
                return Color.Lerp(new Color(255, 120, 80), new Color(232, 50, 60), (t - 0.25f) / 0.35f);
            return Color.Lerp(new Color(232, 50, 60), new Color(255, 150, 160), (t - 0.6f) / 0.4f);
        }
    }
}
