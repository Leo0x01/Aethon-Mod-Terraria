using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// EventHorizonWingRenderer — v6.06 — LAS ALAS DEL HORIZONTE DE SUCESOS.
    ///
    /// Ala de luz 100% procedural (técnica de las coronas, tema del agujero
    /// negro carmesí) compuesta por:
    ///
    ///   1. Un MINI HORIZONTE DE SUCESOS por lado: disco negro absoluto
    ///      (GlowOrb) con ANILLO DE FOTONES rosa pálido (Ring) y chispa
    ///      blanca en el limbo — la "raíz" del ala en la espalda.
    ///   2. TRES ARCOS ANIDADOS de luz de acreción por lado (la silueta del
    ///      ala): medias elipses que nacen pegadas al cuerpo, suben en arco
    ///      y se barrren hacia fuera — como los lazos de la Corona de la
    ///      Reina, pero en horizontal y con la paleta del disco.
    ///   3. DOPPLER BEAMING: el lado que MIRA hacia donde camina el jugador
    ///      arde más (δ³ — el mismo truco del agujero carmesí).
    ///   4. CUENTAS DE MATERIA orbitando cada horizonte cuando el ala está
    ///      desplegada (el disco devorando materia).
    ///   5. Neblina púrpura compacta de fondo (el halo del vacío).
    ///
    /// La animación (apertura + aleteo) la aporta WingAnimPlayer: el ala
    /// se PLEGAN contra la espalda en reposo y despliegan con muelle al
    /// volar; el aleteo es una onda continua (sin frames).
    ///
    /// Uso (biblioteca): VFXCore.Begin() → ComputeQuads(...) →
    /// VFXCore.AppendToPlayerDraw(ref drawInfo).
    /// </summary>
    public static class EventHorizonWingRenderer
    {
        /// <summary>Arcos de acreción por lado (anidado interior→exterior).</summary>
        private const int Arcs = 3;

        /// <summary>Segmentos por arco (suavidad).</summary>
        private const int Segments = 15;

        /// <summary>Cuentas de materia orbitando cada horizonte.</summary>
        private const int MatterBeads = 3;

        /// <summary>
        /// Calcula TODAS las alas como cuadros de luz en el buffer de VFXCore
        /// (coordenadas de MUNDO, ancladas a la espalda del jugador).
        /// </summary>
        /// <param name="back">Punto de anclaje en la espalda (mundo).</param>
        /// <param name="open">Apertura 0..1 (WingAnimPlayer).</param>
        /// <param name="flapAmp">Amplitud del aleteo 0..1.</param>
        /// <param name="flapPhase">Fase del aleteo (rad).</param>
        /// <param name="time">Main.GlobalTimeWrappedHourly.</param>
        /// <param name="direction">Dirección del jugador (-1/1) — Doppler.</param>
        /// <param name="gravDir">Gravedad (1/-1) — invierte el plano del ala.</param>
        /// <param name="alpha">Multiplicador global (luz del mundo).</param>
        public static void ComputeQuads(Vector2 back, float open, float flapAmp,
            float flapPhase, float time, int direction, float gravDir, float alpha)
        {
            if (alpha <= 0f) return;
            open = MathHelper.Clamp(open, 0f, 1.15f);

            // La onda continua de aleteo (sube/baja ápices y ensancha envergadura).
            float flap = (float)Math.Sin(flapPhase) * flapAmp;
            // Apertura eficaz con la respiración del vacío.
            float breathe = VFXCore.Breathe(time, 1.9f, 0f, 0.035f);
            float o = open * breathe;

            for (int side = -1; side <= 1; side += 2)
            {
                bool forwardSide = side == direction;   // el lado que "avanza" arde más
                float doppler = forwardSide ? 1.30f : 0.88f;
                Vector2 root = back + new Vector2(side * 8f, 0f);
                float rootBob = flap * 2.4f * gravDir;
                root.Y += rootBob;

                // ==========================================================
                //  5. NEBLINA PÚRPURA de fondo (el halo del vacío)
                // ==========================================================
                Vector2 hazeCenter = root + new Vector2(side * 16f * o, -14f * gravDir * o);
                VFXCore.Quad(hazeCenter, VFXPalettes.VoidQueen.HaloDeep * (0.55f * alpha * o),
                    new Vector2(70f * (0.6f + 0.4f * o), 52f * (0.6f + 0.4f * o)));
                VFXCore.Quad(hazeCenter, VFXPalettes.VoidQueen.InnerDark * (0.30f * alpha * o),
                    new Vector2(52f * (0.6f + 0.4f * o), 40f * (0.6f + 0.4f * o)));

                // ==========================================================
                //  2+3. LOS ARCOS DE ACRECIÓN (la silueta del ala) + DOPPLER
                // ==========================================================
                for (int l = 0; l < Arcs; l++)
                {
                    float t01 = l / (float)(Arcs - 1);   // 0 interior → 1 exterior
                    float spanX = (24f + 16f * l) * (0.45f + 0.55f * o) * (1f + 0.05f * flap);
                    float apexH = (30f + 13f * l) * (0.5f + 0.5f * o) * (1f + 0.10f * flap);
                    Vector2 arcRoot = root + new Vector2(side * 3f, 5f * gravDir);
                    float arcBreathe = VFXCore.Breathe(time, 2.3f, l * 1.9f);

                    for (int s = 0; s <= Segments; s++)
                    {
                        // a: 0 (punta exterior, baja) → ~0.62π (ápice sobre el hombro)
                        float a = (0.02f + 0.60f * s / Segments) * MathHelper.Pi;
                        float edge = (float)Math.Sin(a);        // 0 puntas → 1 ápice
                        Vector2 pos = arcRoot + new Vector2(
                            side * (float)Math.Cos(a) * spanX * arcBreathe,
                            -edge * apexH * arcBreathe * gravDir);

                        // Color del disco: caliente en la raíz → oscuro hacia la
                        // punta, con el boost de Doppler del lado que avanza.
                        float along = s / (float)Segments;
                        Color col = VFXPalettes.VoidQueen.DiskDoppler(along * 0.85f);
                        float intensity = (0.40f + 0.60f * edge) * doppler;
                        // Grosor: fino en las puntas, corpulento a media altura.
                        float th = (2.0f + 2.4f * edge * (1f - 0.45f * edge)) * (0.7f + 0.3f * o);

                        VFXCore.Quad(pos, col * (intensity * alpha),
                            new Vector2(th * 2f, th * 2f));
                    }

                    // CHISPA PÁLIDA en la punta exterior de cada arco.
                    Vector2 tip = arcRoot + new Vector2(
                        side * spanX, -0.02f * apexH * gravDir);
                    VFXCore.Quad(tip, VFXPalettes.VoidQueen.Pale * (0.75f * alpha * doppler),
                        new Vector2(4.5f, 4.5f));
                }

                // ==========================================================
                //  1. EL MINI HORIZONTE DE SUCESOS (la raíz del ala)
                // ==========================================================
                float pulse = 0.85f + 0.25f * (float)Math.Sin(time * 2.6f + side * 1.3f);
                float orbR = 7.5f * (0.8f + 0.2f * o) * breathe;

                // Anillo de fotones (Ring) alrededor del horizonte.
                VFXCore.Quad(root, VFXPalettes.VoidQueen.Pale * (0.55f * pulse * alpha),
                    VFXCore.RingQuadSize(orbR * 1.55f), VFXCore.Ring);
                // Disco negro absoluto (GlowOrb, alpha blend → negro real).
                VFXCore.Quad(root, Color.Black * (0.97f * alpha),
                    new Vector2(orbR * 2f, orbR * 2f), VFXCore.GlowOrb);
                // Chispa blanca del limbo (el punto más brillante del borde).
                Vector2 spark = root + new Vector2(
                    -side * orbR * 0.55f, -orbR * 0.72f * gravDir);
                VFXCore.Quad(spark, Color.White * (0.85f * pulse * alpha),
                    new Vector2(3.2f, 3.2f));

                // ==========================================================
                //  4. CUENTAS DE MATERIA orbitando el horizonte (al volar)
                // ==========================================================
                if (o > 0.45f)
                {
                    float matterAlpha = (o - 0.45f) / 0.55f * alpha;
                    for (int i = 0; i < MatterBeads; i++)
                    {
                        float ang = time * 2.8f + i * (MathHelper.TwoPi / MatterBeads) + side * 0.6f;
                        Vector2 bead = root + new Vector2(
                            (float)Math.Cos(ang) * orbR * 1.9f,
                            (float)Math.Sin(ang) * orbR * 0.75f * gravDir);
                        VFXCore.Quad(bead, VFXPalettes.VoidQueen.Hot * (0.8f * matterAlpha),
                            new Vector2(3.6f, 3.6f));
                        // Estela corta detrás de la cuenta.
                        Vector2 trail = bead - new Vector2(
                            (float)Math.Sin(ang) * 3f * side, 0f);
                        VFXCore.Quad(trail, VFXPalettes.VoidQueen.Disk * (0.35f * matterAlpha),
                            new Vector2(2.4f, 2.4f));
                    }
                }
            }
        }
    }
}
