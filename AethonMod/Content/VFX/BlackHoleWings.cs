using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// BlackHoleWings — v6.13 — ALAS DEL HORIZONTE DE SUCESOS + ANILLO DE
    /// FOTONES — rediseñadas con la TÉCNICA DE LAS CORONAS.
    ///
    /// HORIZONTE DE SUCESOS — "plumas dobladas al vacío": SIETE plumas de
    /// luz por lado (la primitiva Feather: volumen oscuro + trazo con
    /// gradiente violeta→magenta→rosa pálido + nervio + PERLA en la punta)
    /// que se abanican desde un MINI-HORIZONTE DE SUCESOS en el hombro:
    /// un disco negro opaco rodeado de un ANILLO DE FOTONES blanco. Las
    /// puntas se doblan hacia atrás como si la luz misma estuviera cayendo
    /// en la espalda del jugador.
    ///
    /// ANILLO DE FOTONES — "anillos orbitales": DOS anillos elípticos por
    /// lado (cápsulas en cadena con gradiente Doppler: el filo delantero
    /// arde más) con FOTONES orbitando — perlas con estela que recorren el
    /// anillo — y perlas fijas en los extremos. En reposo los anillos se
    /// pliegan hacia el cuerpo; al volar respiran y se inclinan.
    /// </summary>
    public static class BlackHoleWings
    {
        // ==================================================================
        //  HORIZONTE DE SUCESOS — plumas dobladas al vacío
        // ==================================================================

        /// <summary>Plumas por lado.</summary>
        private const int Feathers = 7;

        /// <summary>Segmentos del anillo de fotones del hombro.</summary>
        private const int ShoulderRingSegs = 10;

        public static void RenderEventHorizon(ref WingDrawContext ctx)
        {
            if (ctx.Alpha <= 0f) return;
            float open = MathHelper.Clamp(ctx.Open, 0f, 1.1f);

            float stroke = (float)Math.Sin(ctx.FlapPhase);
            float flap = stroke * ctx.FlapAmp;

            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 root = ctx.Back + new Vector2(side * 3f, -2f * ctx.GravDir);

                // ============ EL MINI-HORIZONTE EN EL HOMBRO ============
                // Disco negro opaco + anillo de fotones blanco: el "motor"
                // del que nacen las plumas.
                float voidR = 4.5f + 1.5f * open;
                VFXCore.Quad(root, new Color(16, 8, 26) * (0.92f * ctx.Alpha),
                    new Vector2(voidR * 2.4f, voidR * 2.4f), VFXCore.GlowOrb);

                // anillo de fotones alrededor del vacío (cápsulas en cadena)
                for (int s = 0; s < ShoulderRingSegs; s++)
                {
                    float th = s * MathHelper.TwoPi / ShoulderRingSegs + ctx.Time * 0.9f;
                    Vector2 a = root + new Vector2((float)Math.Cos(th), (float)Math.Sin(th)) * (voidR * 1.25f);
                    float th2 = (s + 1) * MathHelper.TwoPi / ShoulderRingSegs + ctx.Time * 0.9f;
                    Vector2 b = root + new Vector2((float)Math.Cos(th2), (float)Math.Sin(th2)) * (voidR * 1.25f);
                    // Doppler: el lado que "se acerca" arde más
                    float dop = 0.55f + 0.45f * Math.Max(0f, (float)Math.Sin(th));
                    WingStrokes.Stroke(a, b, 1.6f, new Color(235, 200, 255), new Color(255, 250, 255),
                        (0.55f + 0.40f * dop) * ctx.Alpha);
                }
                // chispa del horizonte
                WingStrokes.Pearl(root, 6.5f, new Color(190, 150, 255), new Color(255, 250, 255),
                    0.9f * ctx.Alpha);

                // ============ LAS PLUMAS DOBLADAS AL VACÍO ============
                // Abanico: -0.38 rad (arriba) → +0.62 rad (abajo), la más
                // larga en el centro. Cada pluma RETRASA su aleteo con la
                // distancia a la raíz (onda a lo largo del ala).
                for (int f = 0; f < Feathers; f++)
                {
                    float u = f / (float)(Feathers - 1);              // 0 arriba → 1 abajo

                    // ángulo base del abanico + aleteo con retardo
                    float baseAng = MathHelper.Lerp(-0.38f, 0.62f, u);
                    float lag = f * 0.42f;
                    float flapAng = (float)Math.Sin(ctx.FlapPhase - lag) * ctx.FlapAmp * 0.38f;

                    // en reposo las plumas se comprimen contra la espalda
                    float fold = MathHelper.Lerp(0.44f, 1f, open);
                    float ang = (baseAng + flapAng) * fold + 0.10f * (1f - open) * (1f - u);

                    // longitud: la pluma central domina (silueta de ala)
                    float L = MathHelper.Lerp(32f, 52f, (float)Math.Sin(u * MathHelper.Pi)) *
                              (0.50f + 0.50f * open);

                    // el barrido aerodinámico: la velocidad dobla las puntas
                    float sweep = MathHelper.Clamp(ctx.SpeedX * ctx.Direction * 0.05f, -0.55f, 0.55f);

                    // dirección base de la pluma (con espejado por lado)
                    Vector2 dir = new Vector2((float)Math.Cos(ang) * side, (float)Math.Sin(ang) * ctx.GravDir);
                    Vector2 tip = root + dir * L;

                    // punto de control: la curva DOBLA la punta hacia atrás
                    // (hacia -side·X): la luz cayendo en el vacío de la espalda
                    float bendAmt = (0.42f + sweep * 0.8f) * fold;
                    Vector2 bend = root + dir * (L * 0.52f) +
                        new Vector2(-side * (float)Math.Cos(ang) * L * bendAmt * 0.45f, 0f);

                    // grosor: raíz MUY corpulenta, punta afilada (lección VLM:
                    // los trazos finos se leían como estelas sueltas)
                    float wRoot = 6.8f + 2.6f * (1f - Math.Abs(u - 0.45f));
                    float wTip = 2.1f;

                    // gradiente violeta profundo → magenta → rosa pálido
                    Color cRoot = new Color(96, 38, 148);
                    Color cMid = new Color(198, 62, 176);
                    Color cTip = new Color(255, 168, 218);
                    Color dark = new Color(38, 14, 62);

                    WingStrokes.Feather(root, tip, bend, wRoot, wTip, cRoot, cMid, cTip, dark,
                        ctx.Alpha * (0.78f + 0.22f * open), true, 0.68f, 3.3f);
                }

                // ============ POLVO DE FOTONES escapando ============
                // 3 micro-perlas titilantes flotando sobre el ala (deterministas)
                for (int k = 0; k < 3; k++)
                {
                    float h = VFXCore.Hash01(side * 7 + k, k * 3 + 1, 5);
                    float tw = 0.5f + 0.5f * (float)Math.Sin(ctx.Time * (3f + h * 3f) + h * 9f);
                    if (tw < 0.6f) continue;
                    float px = root.X + side * (14f + h * 34f) * open;
                    float py = root.Y + (h - 0.5f * k) * 22f * open - 6f +
                               2.5f * (float)Math.Sin(ctx.Time * 1.7f + h * 7f);
                    WingStrokes.Pearl(new Vector2(px, py), 5.0f,
                        new Color(220, 170, 255), new Color(255, 255, 252),
                        0.85f * tw * ctx.Alpha);
                }
            }
        }

        // ==================================================================
        //  ANILLO DE FOTONES — anillos orbitales
        // ==================================================================

        /// <summary>Segmentos por anillo.</summary>
        private const int RingSegs = 20;

        /// <summary>Fotones orbitando por anillo.</summary>
        private const int OrbitingPhotons = 4;

        public static void RenderPhotonRing(ref WingDrawContext ctx)
        {
            if (ctx.Alpha <= 0f) return;
            float open = MathHelper.Clamp(ctx.Open, 0f, 1.1f);

            float flutter = (float)Math.Sin(ctx.FlapPhase) * ctx.FlapAmp;
            float breathe = VFXCore.Breathe(ctx.Time, 2.0f, 0f, 0.035f);

            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 root = ctx.Back + new Vector2(side * 3f, -2f * ctx.GravDir);

                // ============ LOS HUESOS DEL ALA + LA MEMBRANA (ronda 3) ============
                // Dos trazos GRUESOS raíz→cresta del aro mayor y raíz→frente
                // del aro menor, con la MEMBRANA translúcida violeta ENTRE
                // ellos — la superficie alar que cerraba la lectura de ALA.
                {
                    float fold0 = MathHelper.Lerp(0.45f, 1f, open);
                    Vector2 boneEnd1 = root + new Vector2(
                        side * 33f * fold0, -27f * fold0 * ctx.GravDir);   // hacia la cresta
                    Vector2 boneEnd2 = root + new Vector2(
                        side * 27f * fold0, 9f * fold0 * ctx.GravDir);      // hacia el frente

                    // LA MEMBRANA: bandas translúcidas entre los dos huesos
                    for (int w = 0; w < 3; w++)
                    {
                        float t0 = (w + 1) / 4f;
                        Vector2 p1 = Vector2.Lerp(root, boneEnd1, t0);
                        Vector2 p2 = Vector2.Lerp(root, boneEnd2, t0);
                        Vector2 mid = (p1 + p2) * 0.5f;
                        float span = Vector2.Distance(p1, p2);
                        float rotW = (float)Math.Atan2(p2.Y - p1.Y, p2.X - p1.X);
                        float lenW = Vector2.Distance(root, boneEnd1) * 0.34f;
                        VFXCore.Quad(mid, new Color(58, 24, 96) * (0.40f * ctx.Alpha * fold0),
                            new Vector2(span, lenW), rotW);
                        VFXCore.Quad(mid, new Color(148, 84, 210) * (0.24f * ctx.Alpha * fold0),
                            new Vector2(span * 0.75f, lenW * 0.75f), rotW);
                    }

                    Color boneA = new Color(150, 84, 205);
                    Color boneB = new Color(236, 180, 250);
                    WingStrokes.Volume(root, boneEnd1, 9f, new Color(44, 18, 76), 0.55f * ctx.Alpha);
                    WingStrokes.Volume(root, boneEnd2, 8f, new Color(44, 18, 76), 0.5f * ctx.Alpha);
                    WingStrokes.Stroke(root, boneEnd1, 5.0f, boneA, boneB, 0.9f * ctx.Alpha);
                    WingStrokes.Stroke(root, boneEnd2, 4.2f, boneA, boneB, 0.9f * ctx.Alpha);
                    // perlas en las puntas de los huesos (nudos de articulación)
                    WingStrokes.Pearl(boneEnd1, 8.5f, new Color(225, 170, 250),
                        new Color(255, 250, 255), 0.9f * ctx.Alpha);
                    WingStrokes.Pearl(boneEnd2, 7.5f, new Color(225, 170, 250),
                        new Color(255, 250, 255), 0.85f * ctx.Alpha);
                }

                // DOS anillos: el superior GRANDE inclinado, el inferior
                // menor caído — ambos con su fase de aleteo propia.
                for (int ring = 0; ring < 2; ring++)
                {
                    bool upper = ring == 0;

                    // geometría del anillo (envergadura de ala vanilla)
                    float rx = (upper ? 31f : 20f) * (0.40f + 0.60f * open) * breathe;
                    float ry = (upper ? 17f : 10.5f) * (0.40f + 0.60f * open) * breathe;
                    float tilt = (upper ? -0.34f : 0.28f) * side;

                    // el aleteo mece el anillo entero (el inferior retrasado)
                    float lag = upper ? 0f : 0.55f;
                    float bob = (float)Math.Sin(ctx.FlapPhase - lag) * ctx.FlapAmp * (upper ? 5.5f : 3.5f);
                    float ringTilt = tilt + flutter * (upper ? 0.16f : 0.10f);

                    Vector2 c = root + new Vector2(
                        side * (upper ? 20f : 15f) * open,
                        (upper ? -9f : 7f) * ctx.GravDir * open) +
                        new Vector2(0f, bob * ctx.GravDir);

                    // ---- VOLUMEN: velo oscuro dentro del anillo ----
                    VFXCore.Quad(c, new Color(52, 20, 84) * (0.42f * ctx.Alpha * open),
                        new Vector2(rx * 1.5f, ry * 1.6f), ringTilt);

                    // ---- EL ANILLO en cadena de cápsulas con DOPPLER ----
                    // El filo delantero (abajo-delante) arde más que la cresta
                    // — y va MÁS GRUESO: el FILO DE ATAQUE del ala.
                    Vector2[] pts = new Vector2[RingSegs + 1];
                    for (int s = 0; s <= RingSegs; s++)
                    {
                        float th = s * MathHelper.TwoPi / RingSegs;
                        float ex = (float)Math.Cos(th) * rx;
                        float ey = (float)Math.Sin(th) * ry;
                        float ct = (float)Math.Cos(ringTilt), st = (float)Math.Sin(ringTilt);
                        pts[s] = c + new Vector2(ex * ct - ey * st, ex * st + ey * ct);
                    }
                    for (int s = 0; s < RingSegs; s++)
                    {
                        // doppler por segmento: el frente brilla, la cresta descansa
                        float th = s * MathHelper.TwoPi / RingSegs;
                        float dop = 0.45f + 0.55f * Math.Max(0f, (float)Math.Sin(th + MathHelper.Pi * 0.15f));
                        Color ca = Color.Lerp(new Color(150, 70, 200), new Color(255, 190, 235), dop);
                        // el FILO DE ATAQUE (frente-bajo) lleva el doble de grosor
                        float lead = Math.Max(0f, (float)Math.Sin(th + MathHelper.Pi * 0.15f));
                        float w = MathHelper.Lerp(2.6f, 4.6f, lead);
                        WingStrokes.Stroke(pts[s], pts[s + 1], w, ca, ca,
                            ctx.Alpha * (0.50f + 0.45f * dop) * (0.55f + 0.45f * open));
                    }

                    // ---- FOTONES orbitando (perlas con estela) ----
                    for (int k = 0; k < OrbitingPhotons; k++)
                    {
                        float h = VFXCore.Hash01(ring * 11 + k, k * 5 + 2, 9);
                        float speed = (upper ? 1.5f : 1.9f) * (0.8f + 0.5f * h) * (0.5f + 0.5f * open);
                        float th = ctx.Time * speed + h * MathHelper.TwoPi;

                        // posición + dos puntos de estela detrás
                        for (int e = 0; e < 3; e++)
                        {
                            float eth = th - e * 0.16f;
                            float ex = (float)Math.Cos(eth) * rx;
                            float ey = (float)Math.Sin(eth) * ry;
                            float ct = (float)Math.Cos(ringTilt), st = (float)Math.Sin(ringTilt);
                            Vector2 p = c + new Vector2(ex * ct - ey * st, ex * st + ey * ct);

                            if (e == 0)
                            {
                                // la perla del fotón (núcleo casi blanco)
                                float tw = 0.8f + 0.2f * (float)Math.Sin(ctx.Time * 7f + k * 2.2f);
                                WingStrokes.Pearl(p, 7.5f, new Color(255, 180, 230),
                                    new Color(255, 253, 255), 0.95f * tw * ctx.Alpha);
                            }
                            else
                            {
                                // la estela (cápsula tangencial desvaneciéndose)
                                float eth2 = eth - 0.09f;
                                Vector2 p2 = c + new Vector2(
                                    (float)Math.Cos(eth2) * rx * (float)Math.Cos(ringTilt) -
                                    (float)Math.Sin(eth2) * ry * (float)Math.Sin(ringTilt),
                                    (float)Math.Cos(eth2) * rx * (float)Math.Sin(ringTilt) +
                                    (float)Math.Sin(eth2) * ry * (float)Math.Cos(ringTilt));
                                WingStrokes.Stroke(p2, p, 1.7f, new Color(200, 120, 190),
                                    new Color(255, 190, 235), 0.55f * ctx.Alpha / e);
                            }
                        }
                    }

                    // ---- PERLA FIJA en la cresta del anillo ----
                    float crestTh = -MathHelper.PiOver2;
                    Vector2 crest = c + new Vector2(
                        (float)Math.Cos(crestTh) * rx * (float)Math.Cos(ringTilt) -
                        (float)Math.Sin(crestTh) * ry * (float)Math.Sin(ringTilt),
                        (float)Math.Cos(crestTh) * rx * (float)Math.Sin(ringTilt) +
                        (float)Math.Sin(crestTh) * ry * (float)Math.Cos(ringTilt));
                    float crestTw = 0.75f + 0.25f * (float)Math.Sin(ctx.Time * 3.2f + ring * 1.4f);
                    WingStrokes.Pearl(crest, 6.0f, new Color(230, 160, 235),
                        new Color(255, 250, 255), 0.85f * crestTw * ctx.Alpha);
                }

                // núcleo del hombro: perla de anclaje
                WingStrokes.Pearl(root, 8.0f, new Color(190, 110, 220),
                    new Color(255, 248, 255), 0.9f * ctx.Alpha);
            }
        }
    }
}
