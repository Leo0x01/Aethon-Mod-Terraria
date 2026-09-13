using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// ButterflyWings — v6.08 — LAS ALAS DE MARIPOSA CÓSMICA.
    ///
    /// La silueta de una mariposa de verdad, hecha 100% de luz (técnica de
    /// las coronas): por lado hay un LOBO SUPERIOR grande (redondeado, con
    /// su "ojo" de ala) y un LOBO INFERIOR más pequeño y caído — los dos con
    /// fase de aleteo DIFERENTE (el inferior sigue al superior con retraso,
    /// como las alas de una mariposa real).
    ///
    /// Cada lobo se pinta en TRES capas:
    ///   1. MEMBRANA: retícula de brillos suaves con alpha muy bajo que se
    ///      suman en una película translúcida (celda de triangular a curva).
    ///   2. VENAS: 5 líneas curvas de luz que nacen de la RAÍZ (el torso)
    ///      y se abren hacia el borde — como las venas de una mariposa.
    ///   3. BORDE Y OJOS: el margen del ala arde en dorado con picos
    ///      irregulares y cerca de la punta hay un OJO (anillo pálido con
    ///      núcleo oscuro, como los ojospots de las nymphálidas).
    ///
    /// El golpe de vuelo es el de una mariposa REAL (WingMotionProfile:
    /// StrokeAsymmetry 1.7 — la bajada es rápida y potente, la subida lenta)
    /// y las alas casi SE APLAUDEN sobre la espalda al subir.
    ///
    /// Paleta: membrana magenta profundo → fucsia, venas pálidas, borde
    /// dorado viejo y ojos blanco-rosado (la "mariposa cosmos").
    /// </summary>
    public static class ButterflyWings
    {
        /// <summary>Celdas de membrana por lobo superior.</summary>
        private const int MembraneCells = 26;

        /// <summary>Venas por lobo.</summary>
        private const int Veins = 5;

        /// <summary>Segmentos por vena.</summary>
        private const int VeinSegs = 7;

        /// <summary>Celdas del lobo inferior (menores).</summary>
        private const int LowerCells = 12;

        /// <summary>
        /// La forma del LOBO en coordenadas locales normalizadas
        /// (u = a lo largo del borde del ala 0..1, devuelto en coordenadas
        /// locales del lobo: +x hacia fuera, -y hacia arriba).
        /// </summary>
        private static Vector2 UpperLobeShape(float u)
        {
            // Contorno tipo papilionidae: redondeado con la cima hacia fuera
            // y un pequeño "rabo" en la punta (triángulo suave).
            float theta = MathHelper.Lerp(0.12f, 2.45f, u) * 0.5f; // ángulo polar del contorno
            float r = 1f - 0.18f * (float)Math.Sin(u * MathHelper.Pi);
            // Elipse engordada en la mitad exterior.
            return new Vector2(
                (float)Math.Cos(theta) * r * (0.55f + 0.5f * (float)Math.Sin(theta + 0.4f)),
                (float)Math.Sin(theta) * r * 0.82f);
        }

        private static Vector2 LowerLobeShape(float u)
        {
            // Lobo inferior: gota caída, más pequeño.
            float theta = MathHelper.Lerp(-0.25f, 1.45f, u) * 0.62f;
            float r = 1f - 0.10f * (float)Math.Sin(u * MathHelper.Pi);
            return new Vector2(
                (float)Math.Cos(theta) * r * 0.85f,
                (float)Math.Sin(theta) * r * 0.60f + 0.15f);
        }

        /// <summary>Pinta las alas de la mariposa en el buffer de VFXCore.</summary>
        public static void Render(ref WingDrawContext ctx)
        {
            if (ctx.Alpha <= 0f) return;
            float open = MathHelper.Clamp(ctx.Open, 0f, 1.15f);

            // El golpe asimétrico de mariposa: bajada rápida/potente.
            float stroke = (float)Math.Sin(ctx.FlapPhase);
            float flap = stroke > 0f
                ? (float)Math.Pow(stroke, 0.75f) * ctx.FlapAmp      // bajada: potente
                : stroke * 1.25f * ctx.FlapAmp;                       // subida: larga y suave
            float breathe = VFXCore.Breathe(ctx.Time, 1.4f, 0f, 0.03f);
            float o = open * breathe;

            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 root = ctx.Back + new Vector2(side * 4f, -2f * ctx.GravDir);

                // ============================================================
                //  LOS DOS LOBOS con su FASE DISTINTA (el inferior RETRASADO)
                // ============================================================
                for (int lobe = 0; lobe < 2; lobe++)
                {
                    bool upper = lobe == 0;
                    // El ángulo del plano del ala: abierto = plano casi horizontal
                    // (mariposa planeando); plegado = plano VERTICAL (alas juntas
                    // sobre la espalda, como mariposa en reposo).
                    // La mariposa REAL al volar va de -35° (abajo) a +70° (arriba).
                    float flapAngle = (upper ? flap : flap * 0.72f) * 0.55f;
                    float foldAngle = MathHelper.Lerp(1.25f, 0.12f, o); // reposo: casi vertical
                    float plane = foldAngle + flapAngle;

                    float planeCos = (float)Math.Cos(plane);
                    float planeSin = (float)Math.Sin(plane);

                    // Tamaño del lobo (el superior domina).
                    float W = (upper ? 34f : 20f) * (0.42f + 0.58f * o);
                    float H = (upper ? 30f : 19f) * (0.42f + 0.58f * o);

                    // El lobo inferior nace más abajo/afuera y cae.
                    Vector2 lobeRoot = root + (upper
                        ? new Vector2(side * 2f, -1f * ctx.GravDir)
                        : new Vector2(side * 5f, 4f * ctx.GravDir));

                    // --- 1. MEMBRANA (película translúcida) ---
                    int cells = upper ? MembraneCells : LowerCells;
                    for (int c = 0; c < cells; c++)
                    {
                        // Reticulado jitter determinista dentro del lobo.
                        float cu = (VFXCore.Hash01(side * 7 + lobe, c, 1) + c / (float)cells) % 1f;
                        float cv = VFXCore.Hash01(side * 7 + lobe, c, 2);
                        // Punto interior: mezcla del contorno con el centro.
                        Vector2 edge = upper ? UpperLobeShape(cu) : LowerLobeShape(cu);
                        Vector2 inner = edge * (0.25f + 0.75f * cv);
                        // Coordenadas locales del lobo (antes del giro del plano).
                        Vector2 local = new Vector2(inner.X * W, -Math.Abs(inner.Y) * H * 0.9f
                            - (upper ? 0f : 4f));
                        // Giro del plano del ala alrededor de la raíz.
                        Vector2 spun = new Vector2(
                            local.X * planeCos - local.Y * planeSin,
                            local.X * planeSin + local.Y * planeCos);

                        // La membrana: magenta profundo translúcido.
                        float depth = 1f - cv * 0.7f;   // más denso hacia el borde
                        Color mem = Color.Lerp(
                            VFXPalettes.VoidQueen.InnerDark,
                            VFXPalettes.VoidQueen.Disk, 0.35f + 0.4f * cv);
                        VFXCore.Quad(lobeRoot + new Vector2(spun.X * side, spun.Y * ctx.GravDir),
                            mem * (0.085f * depth * ctx.Alpha * (0.6f + 0.4f * o)),
                            new Vector2(W * 0.34f, H * 0.34f));
                    }

                    // --- 2. VENAS (líneas curvas de la raíz al borde) ---
                    for (int v = 0; v < (upper ? Veins : 3); v++)
                    {
                        float vAng = MathHelper.Lerp(-0.15f, 1.35f, v / (float)(Veins - 1));
                        for (int s = 0; s <= VeinSegs; s++)
                        {
                            float t = s / (float)VeinSegs;
                            // La vena se curva: nace vertical de la raíz y se
                            // abre hacia su ángulo (como el abanico de venas real).
                            float bend = vAng * t + (1f - t) * 0.35f;
                            Vector2 local = new Vector2(
                                (float)Math.Sin(bend) * t * W * 0.92f,
                                -(float)Math.Cos(bend) * t * H * 0.88f);
                            Vector2 spun = new Vector2(
                                local.X * planeCos - local.Y * planeSin,
                                local.X * planeSin + local.Y * planeCos);
                            Color vein = Color.Lerp(VFXPalettes.VoidQueen.Pale,
                                VFXPalettes.VoidQueen.Hot, t * 0.5f);
                            float th = 2.6f * (1f - t * 0.55f);
                            VFXCore.Quad(lobeRoot + new Vector2(spun.X * side, spun.Y * ctx.GravDir),
                                vein * (0.38f * ctx.Alpha * (0.5f + 0.5f * o)),
                                new Vector2(th + 1.6f, th + 1.6f));
                        }
                    }

                    // --- 3. BORDE DORADO + OJO DE ALA ---
                    int rimSteps = upper ? 22 : 13;
                    for (int r = 0; r <= rimSteps; r++)
                    {
                        float u = r / (float)rimSteps;
                        Vector2 edge = upper ? UpperLobeShape(u) : LowerLobeShape(u);
                        Vector2 local = new Vector2(edge.X * W, -Math.Abs(edge.Y) * H * 0.9f
                            - (upper ? 0f : 4f));
                        Vector2 spun = new Vector2(
                            local.X * planeCos - local.Y * planeSin,
                            local.X * planeSin + local.Y * planeCos);
                        // El margen: dorado viejo con PICOS irregulares
                        // (el borde festoneado de las nymphálidas).
                        float spike = 0.75f + 0.25f * (VFXCore.Hash01(side + lobe, r, 5) > 0.4f ? 1f : 0.45f);
                        Color rim = Color.Lerp(new Color(255, 205, 96),
                            new Color(255, 235, 170), u * 0.6f);
                        VFXCore.Quad(lobeRoot + new Vector2(spun.X * side, spun.Y * ctx.GravDir),
                            rim * (0.5f * spike * ctx.Alpha * (0.55f + 0.45f * o)),
                            new Vector2(3.6f, 3.6f));
                    }

                    // El OJO del lobo superior (near de la punta).
                    if (upper && o > 0.3f)
                    {
                        Vector2 eyeLocal = new Vector2(W * 0.62f, -H * 0.48f);
                        Vector2 eyeSpun = new Vector2(
                            eyeLocal.X * planeCos - eyeLocal.Y * planeSin,
                            eyeLocal.X * planeSin + eyeLocal.Y * planeCos);
                        Vector2 eye = lobeRoot + new Vector2(eyeSpun.X * side, eyeSpun.Y * ctx.GravDir);
                        float eyePulse = VFXCore.Breathe(ctx.Time, 1.8f, side * 2f, 0.5f);
                        // Anillo pálido + núcleo oscuro (el ojospot).
                        VFXCore.Quad(eye, VFXPalettes.VoidQueen.Pale * (0.5f * eyePulse * ctx.Alpha),
                            VFXCore.RingQuadSize(4.6f), VFXCore.Ring);
                        VFXCore.Quad(eye, Color.Black * (0.4f * ctx.Alpha),
                            new Vector2(3.4f, 3.4f), VFXCore.GlowOrb);
                        VFXCore.Quad(eye + new Vector2(-1f, -1f), Color.White * (0.4f * ctx.Alpha),
                            new Vector2(1.8f, 1.8f));
                    }
                }

                // El TORAX de la mariposa: un núcleo de luz entre las alas.
                float throb = 0.8f + 0.2f * (float)Math.Sin(ctx.Time * 2.2f);
                VFXCore.Quad(root, new Color(255, 170, 80) * (0.5f * throb * ctx.Alpha),
                    new Vector2(7.5f, 9.5f));
            }
        }
    }
}
