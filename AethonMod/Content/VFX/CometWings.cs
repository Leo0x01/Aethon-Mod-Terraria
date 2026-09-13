using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// CometWings — v6.08 — LAS ALAS DE COMETA CARMESÍ.
    ///
    /// Dos cometas gemelos anclados en los omóplatos: el NÚCLEO (una bola
    /// de hielo y polvo BLANCO-DORADA incandescente con su coma) y la COLA
    /// IÓNICA arrastrándose atrás — larga, cónica y SIEMPRE ONDEANDO como
    /// una bandera al viento solar.
    ///
    /// La cola es una cinta de cuadros rotados por la tangente con gradiente
    /// físico: blanco cegador junto al núcleo → dorado → carmesí → rojo
    /// oscuro disolviéndose en el vacío, con VETAS curvas (las estrías de
    /// plasma real) y motas de polvo desprendiéndose.
    ///
    /// Firma de este ala: la cola RESPONDE a la velocidad — cuanto más
    /// rápido vuelas, más se BARRRE hacia atrás y más se estira (el sweep
    /// aerodinámico en su máxima expresión). Al aletear, una ONDA viaja por
    /// la cola del núcleo a la punta.
    /// </summary>
    public static class CometWings
    {
        /// <summary>Segmentos de la cola.</summary>
        private const int TailSegs = 18;

        /// <summary>Motas de polvo por cola.</summary>
        private const int DustMotes = 4;

        /// <summary>Pinta las alas de cometa en el buffer de VFXCore.</summary>
        public static void Render(ref WingDrawContext ctx)
        {
            if (ctx.Alpha <= 0f) return;
            float open = MathHelper.Clamp(ctx.Open, 0f, 1.15f);

            float flap = (float)Math.Sin(ctx.FlapPhase) * ctx.FlapAmp;
            float o = open;
            // El SWEEP: la velocidad del jugador barre la cola hacia atrás
            // (0 reposo → 1 a toda prisa). ES la personalidad del cometa.
            float speed01 = MathHelper.Clamp(Math.Abs(ctx.SpeedX) / 9f, 0f, 1f);
            float sweep = speed01 * 0.9f;

            for (int side = -1; side <= 1; side += 2)
            {
                // ============================================================
                //  EL NÚCLEO DEL COMETA (la raíz: hielo incandescente)
                // ============================================================
                Vector2 head = ctx.Back + new Vector2(side * 7f, -3f * ctx.GravDir);
                head.Y += flap * 1.8f * ctx.GravDir;

                float comaPulse = VFXCore.Breathe(ctx.Time, 2.4f, side * 1.5f, 0.18f);
                // La COMA: halo amplio dorado-cálido alrededor del núcleo.
                VFXCore.Quad(head, new Color(255, 150, 70) * (0.30f * ctx.Alpha * comaPulse),
                    new Vector2(30f, 30f));
                VFXCore.Quad(head, new Color(255, 205, 130) * (0.40f * ctx.Alpha * comaPulse),
                    new Vector2(16f, 16f));
                // El NÚCLEO: blanco cegador.
                VFXCore.Quad(head, new Color(255, 250, 240) * (0.95f * ctx.Alpha),
                    new Vector2(7.5f, 7.5f));

                // ============================================================
                //  LA COLA IÓNICA (la silueta del ala: cinta ondeante)
                // ============================================================
                Vector2 prev = head;
                for (int s = 0; s <= TailSegs; s++)
                {
                    float t = s / (float)TailSegs;            // 0 núcleo → 1 punta
                    // Longitud total: crece con la apertura Y con la prisa
                    // (los cometas reales tienen colas de millones de km cuando
                    // se acercan al sol — aquí, cuando TÚ te acercas a toda prisa).
                    float L = (38f + 22f * o) * (0.55f + 0.45f * o) * (1f + sweep * 0.65f + 0.06f * flap);

                    // La ONDA viajera: una perturbación que RECORRE la cola del
                    // núcleo a la punta (la cola de un cometa vivo late).
                    float wavePhase = ctx.Time * 3.2f - t * 4.5f;
                    float wave = (float)Math.Sin(wavePhase) * (2.0f + 7.5f * t) * (0.35f + 0.65f * ctx.FlapAmp);

                    // La CURVA de la cola: sale hacia arriba-fuera y el SWEEP la
                    // dobla hacia atrás (a toda prisa casi horizontal).
                    float curveX = t * L * (side == ctx.Direction ? 0.85f : 1.0f);
                    float curveY = (float)Math.Sin(t * MathHelper.Pi * 0.75f) * (26f * (0.45f + 0.55f * o))
                                   * (1f - sweep * 0.55f)
                                   - t * t * 10f * (1f - sweep * 0.4f);
                    // La cola del lado RETRASADO se cruzaría con el cuerpo: ábrela.
                    curveX = side * MathHelper.Max(Math.Abs(curveX), 6f + 30f * t);

                    Vector2 pos = head + new Vector2(
                        curveX * (1f - sweep * 0.25f) + wave * 0.35f * side,
                        -curveY * ctx.GravDir + wave * ctx.GravDir + flap * 1.8f * (1f - t * 0.5f));

                    // Grosor cónico: gordo junto al núcleo, HEHILO en la punta.
                    float th = (5.2f - 4.4f * t) * (0.55f + 0.45f * o) + 0.8f;

                    // Gradiente de plasma: blanco → dorado → carmesí → braza.
                    Color col;
                    if (t < 0.18f)
                        col = Color.Lerp(new Color(255, 250, 240), new Color(255, 215, 120), t / 0.18f);
                    else if (t < 0.55f)
                        col = Color.Lerp(new Color(255, 215, 120), new Color(255, 120, 60),
                            (t - 0.18f) / 0.37f);
                    else
                        col = Color.Lerp(new Color(255, 120, 60), new Color(150, 30, 25),
                            (t - 0.55f) / 0.45f);

                    // VETAS: estrías curvas dentro de la cola (plasma real).
                    float striation = 0.8f + 0.2f * (float)Math.Sin(t * 22f + ctx.Time * 4f + side * 2f);

                    // CINTA rotada por la tangente.
                    Vector2 delta = pos - prev;
                    float rot = delta.LengthSquared() > 0.0001f
                        ? (float)Math.Atan2(delta.Y, delta.X) : 0f;

                    VFXCore.Quad(pos, col * (0.50f * striation * ctx.Alpha * (0.45f + 0.55f * o) * (1f - t * 0.25f)),
                        new Vector2(th * 2.9f, th * 1.6f), rot, VFXCore.SoftGlow);
                    prev = pos;
                }

                // La PUNTA: la cola se disuelve en motitas.
                Vector2 tip = prev;
                for (int m = 0; m < DustMotes; m++)
                {
                    float u = VFXCore.Hash01(side, m, 41);
                    float v = VFXCore.Hash01(side, m, 42);
                    float tw = (float)Math.Pow(0.5f + 0.5f * (float)Math.Sin(ctx.Time * (2f + 3f * u) + v * 20f), 2f);
                    Vector2 mote = tip + new Vector2(
                        (u - 0.5f) * 14f + side * u * 8f,
                        (v - 0.5f * ctx.GravDir) * 12f - 3f * ctx.GravDir);
                    VFXCore.Quad(mote, new Color(255, 200, 150) * (tw * 0.5f * ctx.Alpha),
                        new Vector2(2.4f, 2.4f));
                }
                VFXCore.Quad(tip, new Color(255, 225, 180) * (0.45f * ctx.Alpha),
                    new Vector2(4.0f, 4.0f));
            }
        }
    }
}
