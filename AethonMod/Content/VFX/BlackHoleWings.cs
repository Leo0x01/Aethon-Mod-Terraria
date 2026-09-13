using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// BlackHoleWings — v6.12 — LAS DOS ALAS DE AGUJERO NEGRO.
    ///
    /// Con la técnica de las coronas (TODO es luz procedural sobre un PNG en
    /// blanco) y con la SILUETA del agujero carmesí: núcleo compacto, anillo
    /// de fotones abrazándolo y RASTROS ALARGADOS.
    ///
    /// v6.12: alfas de CORONA (0.7–1.0 en las cintas, núcleos a 1.0) y
    /// envergadura ampliada — se leen como ALAS desde cualquier fondo.
    ///
    /// <see cref="RenderEventHorizon"/> — ALAS DEL HORIZONTE DE SUCESOS:
    /// dos mini agujeros negros en los omóplatos de los que barre un RASTRO
    /// DE ACRECIÓN fino y largo. El rastro es una CINTA de cuadros
    /// orientados por la tangente (Quad rotado de VFXCore) con Doppler
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
                float span = (54f + 16f * o) * (0.55f + 0.45f * o);
                float lift = (40f + 14f * o) * (0.50f + 0.50f * o) * (1f + 0.10f * flap);
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
                    float th = (4.6f - 3.5f * t) * (0.65f + 0.35f * o) + 1.1f;

                    // Color del plasma con Doppler: blanco-rosado en la raíz →
                    // fucsia → carmesí oscuro en la punta disolviéndose.
                    Color col = Color.Lerp(VFXPalettes.VoidQueen.Hot,
                        VFXPalettes.VoidQueen.InnerDark, t * 0.85f);
                    float intensity = (0.72f + 0.42f * (1f - t * 0.6f)) * doppler;

                    // CINTA: cuadro rotado por la TANGENTE de la curva.
                    Vector2 delta = pos - prev;
                    float rot = delta.LengthSquared() > 0.0001f
                        ? (float)Math.Atan2(delta.Y, delta.X) : 0f;
                    VFXCore.Quad(pos, col * (intensity * ctx.Alpha),
                        new Vector2(th * 3.2f, th * 1.7f), rot, VFXCore.SoftGlow);

                    // Vetas de plasma: chispas finas saltando a lo largo del rastro.
                    if (s % 3 == 1)
                    {
                        float jitter = (VFXCore.Hash01(side, s, (int)(ctx.Time * 7f)) - 0.5f) * 3.4f;
                        Vector2 spark = pos + new Vector2(jitter * 0.6f, jitter);
                        VFXCore.Quad(spark,
                            VFXPalettes.VoidQueen.Pale * (0.65f * intensity * ctx.Alpha),
                            new Vector2(3.2f, 3.2f));
                    }

                    prev = pos;
                }

                // La PUNTA: chispa pálida + eco del anillo de Einstein.
                Vector2 tip = prev;
                VFXCore.Quad(tip, VFXPalettes.VoidQueen.Pale * (0.95f * ctx.Alpha * doppler),
                    new Vector2(6.0f, 6.0f));
                float echoPulse = VFXCore.Breathe(ctx.Time, 2.1f, side * 1.2f, 0.5f);
                VFXCore.Quad(tip, VFXPalettes.VoidQueen.Pale * (0.28f * ctx.Alpha * echoPulse),
                    VFXCore.RingQuadSize(10f * (0.8f + 0.2f * o)), VFXCore.Ring);

                // ==============================================================
                //  EL MINI HORIZONTE DE SUCESOS (la raíz del ala)
                // ==============================================================
                float pulse = 0.85f + 0.25f * (float)Math.Sin(ctx.Time * 2.6f + side * 1.3f);
                float orbR = 6.5f * (0.8f + 0.2f * o) * breathe;

                // Anillo de fotones ABRAZANDO el núcleo (1.18×, como el v6.08).
                VFXCore.Quad(root, VFXPalettes.VoidQueen.Pale * (0.75f * pulse * ctx.Alpha),
                    VFXCore.RingQuadSize(orbR * 1.55f), VFXCore.Ring);
                // Disco negro absoluto.
                VFXCore.Quad(root, Color.Black * (1.00f * ctx.Alpha),
                    new Vector2(orbR * 2f, orbR * 2f), VFXCore.GlowOrb);
                // Chispa blanca del limbo.
                Vector2 limb = root + new Vector2(-side * orbR * 0.55f, -orbR * 0.72f * ctx.GravDir);
                VFXCore.Quad(limb, Color.White * (0.95f * pulse * ctx.Alpha),
                    new Vector2(4.0f, 4.0f));

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
                        VFXCore.Quad(bead, VFXPalettes.VoidQueen.Hot * (0.95f * matterAlpha),
                            new Vector2(4.2f, 4.2f));
                        // Estela corta detrás de la cuenta.
                        Vector2 trail = bead - new Vector2((float)Math.Sin(ang) * 3.4f * side, 0f);
                        VFXCore.Quad(trail, VFXPalettes.VoidQueen.Disk * (0.50f * matterAlpha),
                            new Vector2(3.0f, 3.0f));
                    }
                }

                // Neblina púrpura compacta de fondo.
                Vector2 hazeCenter = root + new Vector2(side * 24f * o, -14f * ctx.GravDir * o);
                VFXCore.Quad(hazeCenter, VFXPalettes.VoidQueen.HaloDeep * (0.60f * ctx.Alpha * o),
                    new Vector2(74f * (0.6f + 0.4f * o), 54f * (0.6f + 0.4f * o)));
            }
        }

        // ============ ANILLO DE FOTONES ============

        /// <summary>Aros-pluma del ala.</summary>
        private const int Hoops = 3;

        /// <summary>Fotones viajando por aro.</summary>
        private const int PhotonsPerHoop = 2;

        /// <summary>Segmentos del FILO DE ATAQUE (la línea que grita "ala").</summary>
        private const int EdgeSegs = 9;

        /// <summary>
        /// Alas del Anillo de Fotones: la luz que escapa del horizonte.
        ///
        /// v6.12 — REDISEÑO ESTRUCTURAL (VLM: "campo de energía con forma de
        /// corazón, no un ala"): un ala necesita UN FILO DE ATAQUE — una
        /// cinta dorada continua del hombro a la PUNTA — y las órbitas se
        /// vuelven PLUMAS BARRIDAS: 3 elipses alargadas alineadas AL FILO
        /// (cada una más lejos y más grande), con los fotones corriendo por
        /// ellas. El abanico resultante lee como ala de luz de pepitas
        /// orbitando, no como emblema.
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

                // El NÚCLEO del que escapa la luz (singularidad pequeña y brillante).
                float pulse = 0.8f + 0.2f * (float)Math.Sin(ctx.Time * 3.1f + side);
                VFXCore.Quad(root, VFXPalettes.VoidQueen.Hot * (0.70f * pulse * ctx.Alpha),
                    new Vector2(13f, 13f));
                VFXCore.Quad(root, Color.White * (0.85f * pulse * ctx.Alpha),
                    new Vector2(6.5f, 6.5f));

                // ==============================================================
                //  EL EJE DEL ALA: dirección de barrido (hombro → punta)
                // ===============================================================
                // El ala apunta arriba-afuera (como toda ala en vuelo); el
                // sweep la tiende hacia atrás y el aleteo la bate.
                float wingAng = -0.62f + sweep * 0.38f - flap * 0.30f;
                Vector2 wingDir = new Vector2(
                    (float)Math.Cos(wingAng) * side,
                    (float)Math.Sin(wingAng) * ctx.GravDir);
                float span = (52f + 12f * o) * (0.55f + 0.45f * o);

                // ==============================================================
                //  EL FILO DE ATAQUE — cinta dorada continua del root a la punta
                // ===============================================================
                Vector2 prevE = root;
                for (int s = 0; s <= EdgeSegs; s++)
                {
                    float t = s / (float)EdgeSegs;
                    // Curva del filo: ease-out + ligera comba hacia arriba.
                    float arcX = 1f - (float)Math.Pow(1f - t, 1.6f);
                    float arcY = (float)Math.Sin(t * MathHelper.Pi * 0.55f) * 0.30f;
                    Vector2 pos = root + wingDir * (span * (arcX + arcY * 0.45f));
                    // Grosor: raíz robusta, punta afilada.
                    float th = (4.4f - 3.2f * t) * (0.6f + 0.4f * o) + 1.2f;
                    Vector2 delta = pos - prevE;
                    float rot = delta.LengthSquared() > 0.0001f
                        ? (float)Math.Atan2(delta.Y, delta.X) : 0f;
                    Color edge = Color.Lerp(new Color(255, 214, 120),
                        new Color(255, 250, 235), t * 0.7f);
                    VFXCore.Quad(pos, edge * (0.90f * ctx.Alpha * (0.55f + 0.45f * o) * doppler),
                        new Vector2(th * 3.4f, th * 1.9f), rot, VFXCore.SoftGlow);
                    // Destello en la PUNTA del filo.
                    if (s == EdgeSegs)
                    {
                        VFXCore.Quad(pos, Color.White * (0.95f * ctx.Alpha),
                            new Vector2(5.0f, 5.0f));
                    }
                    prevE = pos;
                }

                // ==============================================================
                //  LAS PLUMAS DE ÓRBITA — 3 aros barridos alineados al filo
                // ===============================================================
                for (int h = 0; h < Hoops; h++)
                {
                    float h01 = h / (float)(Hoops - 1);      // 0 interior → 1 exterior
                    // Cada pluma vive MÁS LEJOS a lo largo del eje del ala.
                    float rx = (15f + 13f * h01) * (0.50f + 0.50f * o);
                    float ry = rx * 0.42f;                   // pluma alargada
                    Vector2 center = root + wingDir * ((7f + 24f * h01) * (0.5f + 0.5f * o))
                                     + wingDir * (rx * 0.35f);
                    // La pluma apunta AL FILO (mismo ángulo, ligeramente más
                    // abierta cuanto más exterior).
                    float tilt = wingAng * side < 0f ? wingAng - 0.10f * h01 : wingAng + 0.10f * h01;
                    if (side < 0) tilt = -tilt;   // espejo del lado izquierdo

                    // El aro: textura Ring ESCALADA a la elipse (pluma de órbita).
                    Color hoopCol = Color.Lerp(VFXPalettes.VoidQueen.Pale,
                        VFXPalettes.VoidQueen.Disk, 0.25f + 0.45f * h01);
                    float hoopA = (0.55f + 0.35f * (1f - h01 * 0.5f)) * doppler * ctx.Alpha;
                    Vector2 hoopSize = VFXCore.RingQuadSize(1f) * new Vector2(rx, ry);
                    VFXCore.Quad(center, hoopCol * (hoopA * 0.9f), hoopSize, tilt, VFXCore.Ring);

                    // ==========================================================
                    //  FOTONES corriendo por la pluma (con estela)
                    // ==========================================================
                    float photonSpeed = 3.0f + 2.2f * h01 + ctx.FlapAmp * 2.0f;
                    for (int i = 0; i < PhotonsPerHoop; i++)
                    {
                        float ang = ctx.Time * photonSpeed
                                    + i * MathHelper.Pi + h * 1.9f + side * 0.8f;
                        // Punto en la elipse GIRADA de la pluma.
                        Vector2 local = new Vector2(
                            (float)Math.Cos(ang) * rx,
                            (float)Math.Sin(ang) * ry);
                        float ct = (float)Math.Cos(tilt), st = (float)Math.Sin(tilt);
                        Vector2 spun = new Vector2(local.X * ct - local.Y * st,
                            local.X * st + local.Y * ct);
                        Vector2 photon = center + spun;

                        // Brillo del fotón: un destello blanco-rosado.
                        float twinkle = 0.75f + 0.25f * (float)Math.Sin(ang * 3f + h);
                        VFXCore.Quad(photon, Color.White * (0.90f * twinkle * ctx.Alpha),
                            new Vector2(4.2f, 4.2f));
                        VFXCore.Quad(photon, VFXPalettes.VoidQueen.Hot * (0.60f * twinkle * ctx.Alpha * doppler),
                            new Vector2(8.5f, 8.5f));

                        // Estela: 2 ecos detrás en la órbita.
                        for (int k = 1; k <= 2; k++)
                        {
                            float angT = ang - k * 0.22f;
                            Vector2 localT = new Vector2(
                                (float)Math.Cos(angT) * rx, (float)Math.Sin(angT) * ry);
                            Vector2 spunT = new Vector2(localT.X * ct - localT.Y * st,
                                localT.X * st + localT.Y * ct);
                            VFXCore.Quad(center + spunT,
                                VFXPalettes.VoidQueen.Pale * (0.42f * twinkle * ctx.Alpha / k),
                                new Vector2(3.2f / k, 3.2f / k));
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
                    float rx = (39f) * (0.50f + 0.50f * o);
                    float ry = rx * 0.52f;
                    Vector2 center = root + new Vector2(
                        side * 23f * (0.5f + 0.5f * o), -17f * ctx.GravDir * (0.5f + 0.5f * o));
                    float tilt = -0.10f * side;
                    Vector2 local = new Vector2(
                        (float)MathHelper.Lerp(-rx, rx, pulseT),
                        -(float)Math.Sin(pulseT * MathHelper.Pi) * ry * 0.4f);
                    float ct = (float)Math.Cos(tilt), st = (float)Math.Sin(tilt);
                    Vector2 spun = new Vector2(local.X * ct - local.Y * st,
                        local.X * st + local.Y * ct);
                    VFXCore.Quad(center + spun,
                        VFXPalettes.VoidQueen.Pale * (0.65f * ctx.FlapAmp * ctx.Alpha),
                        new Vector2(7.5f, 7.5f));
                }
            }
        }
    }
}
