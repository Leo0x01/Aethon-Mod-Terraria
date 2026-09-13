using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// BlackHoleWings — v6.08 — LAS DOS ALAS DE AGUJERO NEGRO, REDISEÑADAS.
    ///
    /// Con la técnica de las coronas (TODO es luz procedural sobre un PNG en
    /// blanco) y ahora con la SILUETA del agujero carmesí v6.08: núcleo
    /// compacto, anillo de fotones abrazándolo y RASTROS ALARGADOS.
    ///
    /// <see cref="RenderEventHorizon"/> — ALAS DEL HORIZONTE DE SUCESOS:
    /// dos mini agujeros negros en los omóplatos de los que barre un RASTRO
    /// DE ACRECIÓN fino y largo (la misma forma del disco nuevo: nace lejos
    /// del horizonte y se estira). El rastro es una CINTA de cuadros
    /// orientados por la tangente (nuevo Quad rotado de VFXCore) con Doppler
    /// δ³, cuentas de materia orbitando y un eco de anillo de Einstein en
    /// cada punta.
    ///
    /// <see cref="RenderPhotonRing"/> — ALAS DEL ANILLO DE FOTONES: la luz
    /// que ESCAPA. Tres aros de fotones ELÍPTICOS por lado (abanico de
    /// órbitas) con fotones viajando por cada aro y estelas; al aletear, un
    /// PULSO recorre el aro mayor de dentro afuera.
    /// </summary>
    public static class BlackHoleWings
    {
        // ============ HORIZONTE DE SUCESOS ============

        /// <summary>Segmentos de la cinta de acreción.</summary>
        private const int StreakSegments = 16;

        /// <summary>Cuentas de materia orbitando cada horizonte.</summary>
        private const int MatterBeads = 3;

        /// <summary>
        /// Alas del Horizonte de Sucesos: mini agujeros negros + rastros de
        /// acreción barriendo el aire (cintas orientadas por tangente).
        /// </summary>
        public static void RenderEventHorizon(ref WingDrawContext ctx)
        {
            if (ctx.Alpha <= 0f) return;
            float open = MathHelper.Clamp(ctx.Open, 0f, 1.15f);

            // La onda de aleteo + la respiración del vacío.
            float flap = (float)Math.Sin(ctx.FlapPhase) * ctx.FlapAmp;
            float breathe = VFXCore.Breathe(ctx.Time, 1.9f, 0f, 0.035f);
            float o = open * breathe;

            // El SWEEP aerodinámico: cuanta más prisa, más barridas atrás.
            float sweep = MathHelper.Clamp(Math.Abs(ctx.SpeedX) / 9f, 0f, 1f) * 0.55f;
            // El DIEDRO: alas más planas subiendo, más recogidas cayendo.
            float dihedral = -ctx.Rise * 0.30f;

            for (int side = -1; side <= 1; side += 2)
            {
                bool forwardSide = side == ctx.Direction;   // el lado que avanza ARDE (δ³)
                float doppler = forwardSide ? 1.32f : 0.88f;
                Vector2 root = ctx.Back + new Vector2(side * 7f, 0f);
                root.Y += flap * 2.2f * ctx.GravDir;

                // ==============================================================
                //  LA CINTA DE ACRECIÓN — el rastro ALARGADO (silueta del ala)
                // ==============================================================
                // Curva: nace pegada al horizonte, sube en arco y se estira
                // afuera con el sweep (como el disco visto de canto).
                float span = (46f + 14f * o) * (0.55f + 0.45f * o);
                float lift = (34f + 12f * o) * (0.50f + 0.50f * o) * (1f + 0.10f * flap);
                float stretch = 1f + sweep * 0.5f + 0.06f * flap;

                Vector2 prev = root;
                for (int s = 0; s <= StreakSegments; s++)
                {
                    float t = s / (float)StreakSegments;          // 0 raíz → 1 punta
                    // El arco: x crece con easing-out, y sube y VUELVE a bajar
                    // un poco en la punta (la caída del rastro de la referencia).
                    float arcX = 1f - (float)Math.Pow(1f - t, 1.7f);
                    float arcY = (float)Math.Sin(t * MathHelper.Pi * 0.88f) * lift
                                 - t * t * lift * 0.22f;
                    Vector2 pos = root + new Vector2(
                        side * arcX * span * stretch,
                        -arcY * ctx.GravDir * (1f + dihedral));

                    // Grosor de la cinta: corpulenta en la raíz, FILAMENTO en la
                    // punta (el rastro se disuelve en plasma).
                    float th = (4.2f - 3.3f * t) * (0.65f + 0.35f * o) + 0.9f;

                    // Color del plasma con Doppler: blanco-rosado en la raíz →
                    // fucsia → carmesí oscuro en la punta disolviéndose.
                    Color col = Color.Lerp(VFXPalettes.VoidQueen.Hot,
                        VFXPalettes.VoidQueen.InnerDark, t * 0.85f);
                    float intensity = (0.50f + 0.50f * (1f - t * 0.6f)) * doppler;

                    // CINTA: cuadro rotado por la TANGENTE de la curva.
                    Vector2 delta = pos - prev;
                    float rot = delta.LengthSquared() > 0.0001f
                        ? (float)Math.Atan2(delta.Y, delta.X) : 0f;
                    VFXCore.Quad(pos, col * (intensity * ctx.Alpha * 0.85f),
                        new Vector2(th * 3.2f, th * 1.7f), rot, VFXCore.SoftGlow);

                    // Vetas de plasma: chispas finas saltando a lo largo del rastro.
                    if (s % 3 == 1)
                    {
                        float jitter = (VFXCore.Hash01(side, s, (int)(ctx.Time * 7f)) - 0.5f) * 3.4f;
                        Vector2 spark = pos + new Vector2(jitter * 0.6f, jitter);
                        VFXCore.Quad(spark,
                            VFXPalettes.VoidQueen.Pale * (0.45f * intensity * ctx.Alpha),
                            new Vector2(2.6f, 2.6f));
                    }

                    prev = pos;
                }

                // La PUNTA: chispa pálida + eco del anillo de Einstein.
                Vector2 tip = prev;
                VFXCore.Quad(tip, VFXPalettes.VoidQueen.Pale * (0.80f * ctx.Alpha * doppler),
                    new Vector2(5.2f, 5.2f));
                float echoPulse = VFXCore.Breathe(ctx.Time, 2.1f, side * 1.2f, 0.5f);
                VFXCore.Quad(tip, VFXPalettes.VoidQueen.Pale * (0.20f * ctx.Alpha * echoPulse),
                    VFXCore.RingQuadSize(9f * (0.8f + 0.2f * o)), VFXCore.Ring);

                // ==============================================================
                //  EL MINI HORIZONTE DE SUCESOS (la raíz del ala)
                // ==============================================================
                float pulse = 0.85f + 0.25f * (float)Math.Sin(ctx.Time * 2.6f + side * 1.3f);
                float orbR = 6.5f * (0.8f + 0.2f * o) * breathe;

                // Anillo de fotones ABRAZANDO el núcleo (1.18×, como el v6.08).
                VFXCore.Quad(root, VFXPalettes.VoidQueen.Pale * (0.60f * pulse * ctx.Alpha),
                    VFXCore.RingQuadSize(orbR * 1.55f), VFXCore.Ring);
                // Disco negro absoluto.
                VFXCore.Quad(root, Color.Black * (0.97f * ctx.Alpha),
                    new Vector2(orbR * 2f, orbR * 2f), VFXCore.GlowOrb);
                // Chispa blanca del limbo.
                Vector2 limb = root + new Vector2(-side * orbR * 0.55f, -orbR * 0.72f * ctx.GravDir);
                VFXCore.Quad(limb, Color.White * (0.85f * pulse * ctx.Alpha),
                    new Vector2(3.4f, 3.4f));

                // ==============================================================
                //  CUENTAS DE MATERIA orbitando el horizonte (al volar)
                // ==============================================================
                if (o > 0.45f)
                {
                    float matterAlpha = (o - 0.45f) / 0.55f * ctx.Alpha;
                    for (int i = 0; i < MatterBeads; i++)
                    {
                        float ang = ctx.Time * 3.1f + i * (MathHelper.TwoPi / MatterBeads) + side * 0.6f;
                        Vector2 bead = root + new Vector2(
                            (float)Math.Cos(ang) * orbR * 2.1f,
                            (float)Math.Sin(ang) * orbR * 0.8f * ctx.GravDir);
                        VFXCore.Quad(bead, VFXPalettes.VoidQueen.Hot * (0.85f * matterAlpha),
                            new Vector2(3.8f, 3.8f));
                        // Estela corta detrás de la cuenta.
                        Vector2 trail = bead - new Vector2((float)Math.Sin(ang) * 3.4f * side, 0f);
                        VFXCore.Quad(trail, VFXPalettes.VoidQueen.Disk * (0.35f * matterAlpha),
                            new Vector2(2.6f, 2.6f));
                    }
                }

                // Neblina púrpura compacta de fondo.
                Vector2 hazeCenter = root + new Vector2(side * 20f * o, -12f * ctx.GravDir * o);
                VFXCore.Quad(hazeCenter, VFXPalettes.VoidQueen.HaloDeep * (0.50f * ctx.Alpha * o),
                    new Vector2(64f * (0.6f + 0.4f * o), 46f * (0.6f + 0.4f * o)));
            }
        }

        // ============ ANILLO DE FOTONES ============

        /// <summary>Aros de órbita por lado.</summary>
        private const int Hoops = 3;

        /// <summary>Fotones viajando por aro.</summary>
        private const int PhotonsPerHoop = 2;

        /// <summary>
        /// Alas del Anillo de Fotones: la luz que escapa del horizonte —
        /// aros de órbita elípticos con fotones corriendo por ellos.
        /// </summary>
        public static void RenderPhotonRing(ref WingDrawContext ctx)
        {
            if (ctx.Alpha <= 0f) return;
            float open = MathHelper.Clamp(ctx.Open, 0f, 1.15f);

            float flap = (float)Math.Sin(ctx.FlapPhase) * ctx.FlapAmp;
            float o = open * VFXCore.Breathe(ctx.Time, 1.6f, 0f, 0.03f);
            float sweep = MathHelper.Clamp(Math.Abs(ctx.SpeedX) / 9f, 0f, 1f) * 0.5f;

            for (int side = -1; side <= 1; side += 2)
            {
                bool forwardSide = side == ctx.Direction;
                float doppler = forwardSide ? 1.25f : 0.92f;
                Vector2 root = ctx.Back + new Vector2(side * 6f, 0f);
                root.Y += flap * 2.0f * ctx.GravDir;

                // El NÚCLEO del que escapa la luz (singelidad pequeña y brillante).
                float pulse = 0.8f + 0.2f * (float)Math.Sin(ctx.Time * 3.1f + side);
                VFXCore.Quad(root, VFXPalettes.VoidQueen.Hot * (0.55f * pulse * ctx.Alpha),
                    new Vector2(11f, 11f));
                VFXCore.Quad(root, Color.White * (0.65f * pulse * ctx.Alpha),
                    new Vector2(5.5f, 5.5f));

                // ==============================================================
                //  EL ABANICO DE AROS (la silueta del ala)
                // ==============================================================
                for (int h = 0; h < Hoops; h++)
                {
                    float h01 = h / (float)(Hoops - 1);      // 0 interior → 1 exterior
                    float rx = (16f + 17f * h01) * (0.50f + 0.50f * o);
                    float ry = rx * (0.62f - 0.10f * h01);   // aros cada vez más tendidos
                    // El abanico: cada aro nace un poco más arriba/out.
                    Vector2 center = root + new Vector2(
                        side * (6f + 8f * h01) * (0.5f + 0.5f * o) * (1f + sweep * 0.4f),
                        -(4f + 7f * h01) * ctx.GravDir * (0.5f + 0.5f * o));
                    float tilt = (-0.42f + 0.21f * h01) * side * (1f + 0.05f * flap);

                    // El aro: textura Ring ESCALADA a la elipse (elipse de órbita).
                    Color hoopCol = Color.Lerp(VFXPalettes.VoidQueen.Pale,
                        VFXPalettes.VoidQueen.Disk, 0.25f + 0.45f * h01);
                    float hoopA = (0.34f + 0.30f * (1f - h01 * 0.5f)) * doppler * ctx.Alpha;
                    Vector2 hoopSize = VFXCore.RingQuadSize(1f) * new Vector2(rx, ry);
                    VFXCore.Quad(center, hoopCol * (hoopA * 0.9f), hoopSize, tilt, VFXCore.Ring);

                    // ==========================================================
                    //  FOTONES corriendo por el aro (con estela)
                    // ==========================================================
                    float photonSpeed = 3.0f + 2.2f * h01 + ctx.FlapAmp * 2.0f;
                    for (int i = 0; i < PhotonsPerHoop; i++)
                    {
                        float ang = ctx.Time * photonSpeed
                                    + i * MathHelper.Pi + h * 1.9f + side * 0.8f;
                        // Punto en la elipse GIRADA del aro.
                        Vector2 local = new Vector2(
                            (float)Math.Cos(ang) * rx,
                            (float)Math.Sin(ang) * ry);
                        float ct = (float)Math.Cos(tilt), st = (float)Math.Sin(tilt);
                        Vector2 spun = new Vector2(local.X * ct - local.Y * st,
                            local.X * st + local.Y * ct);
                        Vector2 photon = center + spun;

                        // Brillo del fotón: un destello blanco-rosado.
                        float twinkle = 0.75f + 0.25f * (float)Math.Sin(ang * 3f + h);
                        VFXCore.Quad(photon, Color.White * (0.80f * twinkle * ctx.Alpha),
                            new Vector2(3.6f, 3.6f));
                        VFXCore.Quad(photon, VFXPalettes.VoidQueen.Hot * (0.45f * twinkle * ctx.Alpha * doppler),
                            new Vector2(7.5f, 7.5f));

                        // Estela: 2 ecos detrás en la órbita.
                        for (int k = 1; k <= 2; k++)
                        {
                            float angT = ang - k * 0.22f;
                            Vector2 localT = new Vector2(
                                (float)Math.Cos(angT) * rx, (float)Math.Sin(angT) * ry);
                            Vector2 spunT = new Vector2(localT.X * ct - localT.Y * st,
                                localT.X * st + localT.Y * ct);
                            VFXCore.Quad(center + spunT,
                                VFXPalettes.VoidQueen.Pale * (0.30f * twinkle * ctx.Alpha / k),
                                new Vector2(2.8f / k, 2.8f / k));
                        }
                    }
                }

                // ==============================================================
                //  EL PULSO DE ALETEO: al golpe hacia abajo, una onda de luz
                //  recorre el aro mayor de dentro afuera.
                // ==============================================================
                if (ctx.FlapAmp > 0.15f)
                {
                    // Posición del pulso: 0→1 con la fase de aleteo (bajada).
                    float down = ctx.FlapPhase % MathHelper.TwoPi;
                    float pulseT = down < MathHelper.Pi
                        ? down / MathHelper.Pi : 1f - (down - MathHelper.Pi) / MathHelper.Pi;
                    float rx = (33f) * (0.50f + 0.50f * o);
                    float ry = rx * 0.52f;
                    Vector2 center = root + new Vector2(
                        side * 20f * (0.5f + 0.5f * o), -15f * ctx.GravDir * (0.5f + 0.5f * o));
                    float tilt = -0.10f * side;
                    Vector2 local = new Vector2(
                        (float)MathHelper.Lerp(-rx, rx, pulseT),
                        -(float)Math.Sin(pulseT * MathHelper.Pi) * ry * 0.4f);
                    float ct = (float)Math.Cos(tilt), st = (float)Math.Sin(tilt);
                    Vector2 spun = new Vector2(local.X * ct - local.Y * st,
                        local.X * st + local.Y * ct);
                    VFXCore.Quad(center + spun,
                        VFXPalettes.VoidQueen.Pale * (0.5f * ctx.FlapAmp * ctx.Alpha),
                        new Vector2(6.5f, 6.5f));
                }
            }
        }
    }
}
