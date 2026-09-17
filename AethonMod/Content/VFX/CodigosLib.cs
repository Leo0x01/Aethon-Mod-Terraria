using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// CodigosLib — v6.36 — LA LIBRERÍA DE LOS CÓDIGOS VIVOS.
    ///
    /// CUATRO CÓDIGOS de demostraciones web (JavaScript / THREE.js), uno
    /// por imagen del usuario, traducidos a las primitivas de la casa:
    /// cada compositor es la REPLICA VISUAL del algoritmo de su imagen —
    /// la FÍSICA (la simulación de nodos) vive en su proyectil, aquí
    /// vive la CARNE (el render).
    ///
    ///   · DanzaOrbes(...)    — IMAGEN 1: follow() — el sistema de orbes
    ///     encadenados: cada hijo conserva la distancia exacta a su
    ///     padre (this.size) y su ángulo absoluto/relativo se recalcula
    ///     cada frame (absAngle / relAngle) — los brazos orbitales con
    ///     su estela tangencial son el "updateRelative(false, true)"
    ///     del original hecho luz.
    ///   · OjoAbismo(...)     — IMAGEN 2: animate() del agujero negro —
    ///     los tres materiales vivos del shader (discoDMaterial → el
    ///     anillo energético de 20 bandas, starMaterial → LAS ESTRELLAS
    ///     DOBLADAS cayendo en espiral hacia el horizonte,
    ///     eventHorizonMat → el núcleo negro absoluto) todos animados
    ///     por el uTime del reloj, y la lente de pantalla curvando el
    ///     fondo alrededor (la lensingPass del original la pone
    ///     GravLens desde el proyectil).
    ///   · SolVivo(...)       — IMAGEN 3: animate() del sol — EL PULSO
    ///     EXACTO del original: pulse = 0.5 + 0.5·sin(time·2.15) y el
    ///     bloom = 0.8 + 0.4·pulse respirando TODA la luz; el núcleo
    ///     girando a 0.05 rad/s (coreGroup.rotation.y += delta·0.05) y
    ///     los seis materiales vivos: núcleo estelar, cáscara, disco
    ///     ecuatorial, anillos de pulso, prominencias y ascuas.
    ///   · SierpeEstelar(...) — IMAGEN 4: elems — la criatura de 16
    ///     segmentos con su jerarquía Cabeza / Aletas / Espina (la del
    ///     original: i==1 la cabeza, i==8 e i==14 las aletas, el resto
    ///     espina) nadando alrededor del puntero.
    ///
    /// CONTRATO (el patrón SigiloLib): TODAS las coordenadas son de
    /// MUNDO (la librería resta Main.screenPosition al entrar) y el lote
    /// entra CERRADO y sale CERRADO — cada compositor abre y cierra SUS
    /// pases (aditivo para la luz, alfa para lo que OCULLE) contra la
    /// GameViewMatrix del juego.
    /// </summary>
    public static class CodigosLib
    {
        // ==================================================================
        //  EL PINCEL (las texturas procedurales del proyecto, cacheadas)
        // ==================================================================

        private static Asset<Texture2D> _star;

        /// <summary>La estrella de 4 puntas (16×16 — chispas y ascuas).</summary>
        private static Texture2D Star =>
            (_star ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/Star")).Value;

        /// <summary>El orbe de NÚCLEO SÓLIDO de la casa (VFXCore).</summary>
        private static Texture2D GlowOrb => VFXCore.GlowOrb;

        /// <summary>Buffer de conversión mundo→pantalla (cero GC).</summary>
        private static readonly Vector2[] _scr = new Vector2[32];

        /// <summary>Tinte premultiplicado de la casa (RGB·f, alfa 255·f).</summary>
        private static Color Tint(Color c, float f) => OrbitaLib.Tint(c, f);

        // ==================================================================
        //  LAS PALETAS DE LOS CUATRO CÓDIGOS
        // ==================================================================

        /// <summary>IMAGEN 1 — el sol de la danza: blanco cálido.</summary>
        private static readonly Color SolCuerpo = new(255, 244, 214);

        /// <summary>IMAGEN 1 — el halo dorado del sol de la danza.</summary>
        private static readonly Color SolHalo = new(255, 196, 92);

        /// <summary>IMAGEN 1 — planeta: naranja estelar.</summary>
        private static readonly Color PlanetaCuerpo = new(255, 176, 96);

        /// <summary>IMAGEN 1 — planeta: brasa profunda.</summary>
        private static readonly Color PlanetaHalo = new(255, 122, 42);

        /// <summary>IMAGEN 1 — luna: azul pálido frío.</summary>
        private static readonly Color LunaCuerpo = new(196, 220, 255);

        /// <summary>IMAGEN 1 — luna: azul estelar.</summary>
        private static readonly Color LunaHalo = new(122, 164, 255);

        /// <summary>IMAGEN 2 — el anillo vivo del abismo (magenta).</summary>
        private static readonly Color AbismoVivo = new(255, 64, 208);

        /// <summary>IMAGEN 2 — el valle profundo del abismo (violeta).</summary>
        private static readonly Color AbismoProfundo = new(150, 30, 180);

        /// <summary>IMAGEN 2 — el pico incandescente (blanco rosado).</summary>
        private static readonly Color AbismoHot = new(255, 228, 250);

        /// <summary>IMAGEN 4 — la carne de la sierpe: blanco estelar.</summary>
        private static readonly Color SierpeCuerpo = new(255, 248, 225);

        /// <summary>IMAGEN 4 — el halo de la sierpe: dorado suave.</summary>
        private static readonly Color SierpeHalo = new(255, 214, 120);

        /// <summary>IMAGEN 4 — las aletas: cian pálido frío.</summary>
        private static readonly Color AletaColor = new(168, 224, 255);

        // ==================================================================
        //  COMPOSITOR 1 — LA DANZA DE LOS ORBES (imagen 1: follow())
        // ==================================================================

        /// <summary>
        /// LA DANZA DE LOS ORBES — el render del sistema encadenado de la
        /// IMAGEN 1. Cada nodo llega con su posición YA resuelta por el
        /// puerto C# del follow() que vive en el proyectil; aquí se viste:
        /// el ENLACE ORBITAL (la cuerda de luz padre→hijo), la ESTELA
        /// TANGENCIAL (el "updateRelative" del original: cada orbe
        /// estirado CONTRA su movimiento, perpendicular al radio que lo
        /// ata — así se lee la órbita), el CUERPO (núcleo sólido + halo)
        /// y las chispas del sol.
        /// </summary>
        /// <param name="pos">Posiciones de MUNDO de los nodos.</param>
        /// <param name="radio">Radio visual de cada nodo (px).</param>
        /// <param name="absAngle">this.absAngle del original (rad — ángulo
        /// padre→nodo, recalculado por Follow cada tick).</param>
        /// <param name="tipo">0 = sol (raíz), 1 = planeta, 2 = luna.</param>
        /// <param name="padre">Índice del padre de cada nodo (la raíz se
        /// apunta a sí misma; su enlace no se dibuja).</param>
        /// <param name="n">Número de nodos vivos del array.</param>
        /// <param name="time">Tiempo animado.</param>
        /// <param name="seed">Semilla determinista.</param>
        /// <param name="alpha">Intensidad global (0..1).</param>
        public static void DanzaOrbes(Vector2[] pos, float[] radio, float[] absAngle,
            int[] tipo, int[] padre, int n, float time, int seed, float alpha)
        {
            if (alpha <= 0.02f || n <= 0 || Main.netMode == NetmodeID.Server) return;
            if (n > _scr.Length) n = _scr.Length;

            // MUNDO → PANTALLA (el contrato SigiloLib de la casa).
            Vector2 off = Main.screenPosition;
            for (int i = 0; i < n; i++) _scr[i] = pos[i] - off;

            OrbitaLib.AbrirAdditive();
            try
            {
                // === 1. LOS ENLACES ORBITALES: la cuerda de luz que ata a
                //     cada hijo con su padre (this.size hecha visible). ===
                for (int i = 1; i < n; i++)
                {
                    int p = padre[i];
                    Vector2 a = _scr[p], b = _scr[i];
                    Vector2 delta = b - a;
                    float len = delta.Length();
                    if (len < 0.5f) continue;
                    float rot = (float)Math.Atan2(delta.Y, delta.X);
                    OrbitaLib.Capsule((a + b) * 0.5f, len, 2.6f, rot,
                        Tint(SolHalo, 0.12f * alpha));
                }

                // === 2. LOS CUERPOS: núcleo sólido + halo + ESTELA
                //     TANGENCIAL (el updateRelative del original — el
                //     estiramiento perpendicular al radio de la órbita). ===
                for (int i = 0; i < n; i++)
                {
                    Color cuerpo, halo;
                    float mult = 1f;
                    switch (tipo[i])
                    {
                        case 1: cuerpo = PlanetaCuerpo; halo = PlanetaHalo; mult = 0.82f; break;
                        case 2: cuerpo = LunaCuerpo; halo = LunaHalo; mult = 0.62f; break;
                        default: cuerpo = SolCuerpo; halo = SolHalo; mult = 1f; break;
                    }

                    float pulse = 0.80f + 0.20f * (float)Math.Sin(time * 2.2f + i * 1.7f);

                    // La estela tangencial: perpendicular al radio (absAngle).
                    OrbitaLib.Capsule(_scr[i], radio[i] * 2.3f, radio[i] * 0.55f,
                        absAngle[i] + MathHelper.PiOver2,
                        Tint(halo, 0.20f * pulse * alpha));

                    // El halo respirando y el núcleo sólido.
                    OrbitaLib.Quad(VFXCore.SoftGlow, _scr[i],
                        new Vector2(radio[i] * 5.4f, radio[i] * 5.4f), 0f,
                        Tint(halo, 0.16f * pulse * alpha * mult));
                    OrbitaLib.Quad(GlowOrb, _scr[i],
                        new Vector2(radio[i] * 2.1f * mult, radio[i] * 2.1f * mult), 0f,
                        Tint(cuerpo, 0.92f * alpha));

                    // El corazón blanco del sol (la raíz arde más).
                    if (tipo[i] == 0)
                        OrbitaLib.Quad(GlowOrb, _scr[i],
                            new Vector2(radio[i] * 1.0f, radio[i] * 1.0f), 0f,
                            Tint(SolCuerpo, 0.95f * alpha));
                }

                // === 3. LAS CHISPAS DEL SOL: la raíz suelta estrellitas. ===
                for (int k = 0; k < 6; k++)
                {
                    float h = VFXCore.Hash01(seed, 500 + k, 61);
                    float ang = h * MathHelper.TwoPi + time * (0.5f + 0.3f * h);
                    float r = radio[0] * (1.7f + 1.6f * VFXCore.Hash01(seed, 510 + k, 67));
                    Vector2 p = _scr[0] + new Vector2((float)Math.Cos(ang) * r,
                        (float)Math.Sin(ang) * r * 0.7f);
                    float tw = 0.4f + 0.6f * (float)Math.Sin(time * 3.1f + k * 2.0f);
                    OrbitaLib.Quad(Star, p, new Vector2(9f, 9f), ang,
                        Tint(SolHalo, 0.55f * tw * alpha));
                }
            }
            finally
            {
                OrbitaLib.CerrarBatch();
            }
        }

        // ==================================================================
        //  COMPOSITOR 2 — EL OJO DEL ABISMO (imagen 2: el animate() del
        //  agujero negro: diskMaterial / starMaterial / eventHorizonMat)
        // ==================================================================

        /// <summary>
        /// EL OJO DEL ABISMO — el render del agujero negro de la IMAGEN 2.
        /// Los tres materiales del animate() original, todos esclavos del
        /// MISMO reloj (uTime → time):
        ///   · diskMaterial → el ANILLO ENERGÉTICO de 20 bandas viajando
        ///     (mitad trasera tenue, mitad delantera incandescente — la
        ///     fórmula fiel del shader de la casa);
        ///   · eventHorizonMat → el NÚCLEO NEGRO ABSOLUTO (el pase de
        ///     ALFA que se come la luz) con su anillo de fotones fino;
        ///   · starMaterial → LAS ESTRELLAS DOBLADAS: doce estrellas
        ///     cayendo en espiral hacia el horizonte, acelerando al
        ///     acercarse (la lente amplifica su brillo) y DESTELLANDO al
        ///     cruzar el anillo de fotones antes de renacer lejos.
        /// La lensingPass del original (la pantalla curvándose alrededor
        /// del agujero proyectado a coordenadas de pantalla) la registra
        /// el proyectil con GravLens cada tick.
        /// </summary>
        /// <param name="center">Centro en coords de MUNDO.</param>
        /// <param name="radius">Radio del vórtice (px — el anillo llega a ~1.9×).</param>
        /// <param name="progress">0..1 de la vida (apertura/cierre).</param>
        /// <param name="time">El reloj (uTime del original).</param>
        /// <param name="seed">Semilla determinista.</param>
        public static void OjoAbismo(Vector2 center, float radius, float progress,
            float time, int seed)
        {
            if (radius < 2f || progress <= 0.02f || Main.netMode == NetmodeID.Server) return;

            center -= Main.screenPosition;   // MUNDO → PANTALLA

            int flick = (int)(time * 12f);
            float rr = radius * (0.94f + 0.06f * (float)Math.Sin(time * 1.1f)) * progress;

            // === PASO 1 (ADITIVO): los ecos y las ondas de distorsión. ===
            OrbitaLib.AbrirAdditive();
            try
            {
                OrbitaLib.EcosAnillo(center, rr, time, (float)Math.Sin(time) * 0.3f,
                    AbismoVivo, AbismoProfundo);
                OrbitaLib.OndasDistorsion(center, rr, time, seed, 2, 2.6f, AbismoProfundo);

                // La mitad TRASERA del disco (diskMaterial, uTime).
                OrbitaLib.AnilloEnergia(center, rr, time, seed, flick, front: false,
                    hot: AbismoHot, mid: AbismoVivo, deep: AbismoProfundo);
            }
            finally
            {
                OrbitaLib.CerrarBatch();
            }

            // === PASO 2 (ALFA): EL HORIZONTE DE SUCESOS — el núcleo negro
            //     absoluto que se COME la luz (eventHorizonMat). ===
            OrbitaLib.AbrirAlpha();
            try
            {
                float nucleo = radius * 0.98f * progress;
                Main.spriteBatch.Draw(GlowOrb, center, null, Color.Black,
                    0f, new Vector2(GlowOrb.Width, GlowOrb.Height) * 0.5f,
                    new Vector2(nucleo, nucleo) / new Vector2(GlowOrb.Width, GlowOrb.Height),
                    SpriteEffects.None, 0f);
            }
            finally
            {
                OrbitaLib.CerrarBatch();
            }

            // === PASO 3 (ADITIVO): el anillo de fotones, la mitad
            //     DELANTERA del disco y LAS ESTRELLAS DOBLADAS. ===
            OrbitaLib.AbrirAdditive();
            try
            {
                OrbitaLib.AnilloFino(center, 1.02f * rr, time * 0.15f,
                    Tint(AbismoVivo, 0.40f + 0.12f * (float)Math.Sin(time * 1.7f)));

                OrbitaLib.AnilloEnergia(center, rr, time, seed, flick, front: true,
                    hot: AbismoHot, mid: AbismoVivo, deep: AbismoProfundo);

                // --- LAS ESTRELLAS DOBLADAS (starMaterial): cada estrella
                //     nace lejos, cae en espiral, ACELERA cerca del
                //     horizonte (el brillo se amplifica — la lente) y
                //     DESTELLA al cruzar el anillo de fotones. ---
                for (int k = 0; k < 12; k++)
                {
                    float h1 = VFXCore.Hash01(seed, 600 + k, 71);
                    float h2 = VFXCore.Hash01(seed, 610 + k, 73);
                    float ciclo = 3.2f + 3.4f * h1;                 // s de caída
                    float u = (time / ciclo + h2) % 1f;             // 0 lejos → 1 capturada

                    // La espiral: el radio se cierra, el ángulo integra
                    // la velocidad creciente (Kepler a lo bestia: ω ×4
                    // en el tramo final).
                    float rNorm = 1f - u;
                    float r = radius * (0.95f + 2.5f * rNorm * rNorm);
                    float w = 0.7f + 2.9f * u * u;                  // acelera al caer
                    float ang = h1 * MathHelper.TwoPi + time * w * 0.9f;

                    Vector2 p = center + new Vector2((float)Math.Cos(ang) * r,
                        (float)Math.Sin(ang) * r * 0.42f).RotatedBy(-0.30f);

                    // La AMPLIFICACIÓN de la lente: más cerca = más
                    // brillante (la luz se dobla y se apila).
                    float amp = 0.30f + 0.70f * u * u;
                    // El DESTELLO de la captura: el último suspiro al
                    // cruzar el anillo de fotones.
                    float flash = u > 0.94f ? (u - 0.94f) / 0.06f : 0f;

                    OrbitaLib.Quad(Star, p, new Vector2(7f + 9f * u, 7f + 9f * u),
                        ang, Tint(AbismoHot, 0.55f * amp * rNorm));
                    if (flash > 0f)
                    {
                        OrbitaLib.Quad(VFXCore.SoftGlow, p,
                            new Vector2(46f * flash, 46f * flash), 0f,
                            Tint(AbismoHot, 0.80f * flash));
                        OrbitaLib.AnilloFino(p, 16f * flash, time * 3f,
                            Tint(AbismoVivo, 0.60f * flash));
                    }
                }
            }
            finally
            {
                OrbitaLib.CerrarBatch();
            }
        }

        // ==================================================================
        //  COMPOSITOR 3 — EL SOL VIVO (imagen 3: el animate() del sol)
        // ==================================================================

        /// <summary>LA FRECUENCIA EXACTA del pulso del animate() original.</summary>
        public const float FrecuenciaPulso = 2.15f;

        /// <summary>LA ROTACIÓN EXACTA del núcleo del original (rad/s:
        /// coreGroup.rotation.y += delta · 0.05).</summary>
        public const float RotacionNucleo = 0.05f;

        /// <summary>
        /// EL SOL VIVO — el render del sol de la IMAGEN 3. EL PULSO
        /// EXACTO del animate() original late en TODO:
        /// <code>
        ///   const pulse = 0.5 + 0.5 * Math.sin(time * 2.15);
        ///   bloomPass.strength = 0.8 + 0.4 * pulse;
        ///   coreGroup.rotation.y += delta * 0.05;
        /// </code>
        /// Los seis materiales del original, uno a uno: el NÚCLEO
        /// estelar girando (starMaterial + coreGroup), la CÁSCARA
        /// respirando (shellMaterial), el DISCO ecuatorial (diskMat),
        /// los ANILLOS que nacen en cada pico del pulso (ringMat), las
        /// PROMINENCIAS arqueándose desde la superficie (prominenceMat
        /// → PyraLib.Tongue, la llama de la casa) y las ASCUAS que
        /// escapan y se apagan (emberMat). El bloom del original
        /// (0.8..1.2) escala cada brillo Y la luz del mundo (la mide el
        /// proyectil con la MISMA fórmula).
        /// </summary>
        /// <param name="center">Centro en coords de MUNDO.</param>
        /// <param name="radius">Radio del núcleo solar (px).</param>
        /// <param name="time">El reloj (elapsedTime del original).</param>
        /// <param name="seed">Semilla determinista.</param>
        /// <param name="alpha">Intensidad global (0..1).</param>
        public static void SolVivo(Vector2 center, float radius, float time,
            int seed, float alpha)
        {
            if (radius < 2f || alpha <= 0.02f || Main.netMode == NetmodeID.Server) return;

            center -= Main.screenPosition;   // MUNDO → PANTALLA

            // === EL PULSO EXACTO DEL ORIGINAL (línea por línea). ===
            float pulse = 0.5f + 0.5f * (float)Math.Sin(time * FrecuenciaPulso);
            float bloom = 0.8f + 0.4f * pulse;

            OrbitaLib.AbrirAdditive();
            try
            {
                // === 1. LA CÁSCARA (shellMaterial): el velo exterior que
                //     respira MÁS DESPACIO que el pulso — el contraste
                //     que hace vivo al sol. ===
                float cascaras = 0.5f + 0.5f * (float)Math.Sin(time * 0.9f + 1.1f);
                OrbitaLib.Quad(VFXCore.SoftGlow, center,
                    new Vector2(radius * 5.6f, radius * 5.6f) * (0.94f + 0.10f * cascaras),
                    0f, Tint(new Color(255, 176, 80), 0.15f * bloom * alpha));

                // === 2. EL DISCO ECUATORIAL (diskMat): tres aros achatados
                //     girando a velocidades distintas (la rotación
                //     diferencial del plasma). ===
                for (int d = 0; d < 3; d++)
                {
                    float a = radius * (1.42f + 0.16f * d);
                    float b = radius * (0.30f + 0.05f * d);
                    float spinD = time * (0.9f + 0.35f * d) * (d % 2 == 0 ? 1f : -1f);
                    const int Pasos = 26;
                    for (int s = 0; s < Pasos; s++)
                    {
                        float t0 = spinD + s / (float)Pasos * MathHelper.TwoPi;
                        float t1 = spinD + (s + 1) / (float)Pasos * MathHelper.TwoPi;
                        Vector2 p0 = VFXCore.Ellipse(center, a, b, -0.22f, t0);
                        Vector2 p1 = VFXCore.Ellipse(center, a, b, -0.22f, t1);
                        Vector2 mid = (p0 + p1) * 0.5f;
                        Vector2 seg = p1 - p0;
                        float len = seg.Length();
                        if (len < 0.5f) continue;
                        float rot = (float)Math.Atan2(seg.Y, seg.X);
                        float glow = 0.5f + 0.5f * (float)Math.Sin(t0 * 9f + time * 4f);
                        Color c = PyraPalettes.Sample(PyraPalettes.SolarFire, 0.35f + 0.6f * glow);
                        OrbitaLib.Capsule(mid, len, 3.2f + 1.6f * glow, rot,
                            Tint(c, (0.28f + 0.30f * glow) * bloom * alpha * 0.8f));
                    }
                }

                // === 3. EL NÚCLEO GIRANDO (starMaterial + coreGroup): el
                //     corazón blanco rodeado de TRES LÓBULOS de plasma que
                //     giran a la ROTACIÓN EXACTA del original. ===
                float rotNucleo = time * RotacionNucleo;
                for (int l = 0; l < 3; l++)
                {
                    float ang = rotNucleo + l * MathHelper.TwoPi / 3f;
                    Vector2 p = center + new Vector2((float)Math.Cos(ang),
                        (float)Math.Sin(ang)) * radius * 0.42f;
                    OrbitaLib.Quad(GlowOrb, p,
                        new Vector2(radius * 1.15f, radius * 1.15f), 0f,
                        Tint(new Color(255, 196, 96), 0.50f * bloom * alpha));
                }
                OrbitaLib.Quad(GlowOrb, center,
                    new Vector2(radius * 1.9f, radius * 1.9f) * (0.93f + 0.14f * pulse),
                    0f, Tint(new Color(255, 236, 180), 0.85f * bloom * alpha));
                OrbitaLib.Quad(GlowOrb, center,
                    new Vector2(radius * 0.85f, radius * 0.85f), 0f,
                    Tint(new Color(255, 252, 240), 0.95f * bloom * alpha));

                // === 4. LOS ANILLOS DEL PULSO (ringMat): un anillo nace en
                //     cada PICO del pulso y se expande apagándose — el
                //     período EXACTO 2π/2.15 s del original. ===
                float periodo = MathHelper.TwoPi / FrecuenciaPulso;
                for (int r = 0; r < 2; r++)
                {
                    float prog = (time / periodo + 0.5f * r) % 1f;
                    OndaLib.Pulse(Main.spriteBatch, center, prog, radius * 3.1f,
                        new Color(255, 214, 120), (0.35f + 0.30f * pulse) * alpha, seed + r);
                }

                // === 5. LAS PROMINENCIAS (prominenceMat): lenguas de fuego
                //     arqueándose desde la superficie — crecen con el pulso
                //     y cada una tiene SU latido (PyraLib.Tongue). ===
                for (int k = 0; k < 5; k++)
                {
                    float ang = k / 5f * MathHelper.TwoPi + time * 0.15f;
                    Vector2 basePos = center + new Vector2((float)Math.Cos(ang),
                        (float)Math.Sin(ang)) * radius * 0.88f;
                    float altura = radius * (0.55f + 0.35f * (float)Math.Sin(time * 1.3f + k * 2.1f))
                        * (0.55f + 0.75f * pulse);
                    PyraLib.Tongue(Main.spriteBatch, basePos, altura, radius * 0.17f,
                        PyraPalettes.SolarFire, 0.72f, seed + 40 + k, time,
                        (0.55f + 0.45f * pulse) * alpha);
                }

                // === 6. LAS ASCUAS (emberMat): chispas deterministas que
                //     escapan de la superficie, se alejan y se APAGAN
                //     (blanco → naranja → rojo → extinta). ===
                for (int e = 0; e < 7; e++)
                {
                    float h1 = VFXCore.Hash01(seed, 700 + e, 83);
                    float h2 = VFXCore.Hash01(seed, 710 + e, 89);
                    float ciclo = 1.1f + 1.3f * h1;
                    float u = (time / ciclo + h2) % 1f;             // 0 nace → 1 extinta
                    float ang = h2 * MathHelper.TwoPi + time * 0.6f;
                    float dist = radius * (0.9f + 2.0f * u);
                    Vector2 p = center + new Vector2((float)Math.Cos(ang) * dist,
                        (float)Math.Sin(ang) * dist - u * u * radius * 0.8f);
                    Color c = PyraPalettes.Sample(PyraPalettes.SolarFire, 1f - u);
                    OrbitaLib.Quad(Star, p, new Vector2(8f * (1f - u) + 3f, 8f * (1f - u) + 3f),
                        ang, Tint(c, 0.65f * (1f - u) * alpha));
                }
            }
            finally
            {
                OrbitaLib.CerrarBatch();
            }
        }

        // ==================================================================
        //  COMPOSITOR 4 — LA SIERPE ESTELAR (imagen 4: los elems)
        // ==================================================================

        /// <summary>EL ÍNDICE DE LA CABEZA en 0-based (la i==1 del original).</summary>
        public const int IdxCabeza = 0;

        /// <summary>LOS ÍNDICES DE LAS ALETAS en 0-based (las i==8 e i==14).</summary>
        public static readonly int[] IdxAletas = { 7, 13 };

        /// <summary>¿Es este segmento un ALETA? (las i==8 e i==14 del original).</summary>
        public static bool EsAleta(int i) => i == IdxAletas[0] || i == IdxAletas[1];

        /// <summary>
        /// LA SIERPE ESTELAR — el render de la criatura segmentada de la
        /// IMAGEN 4. La jerarquía EXACTA del prepend() original (índices
        /// traducidos a 0-based: la i==1 → cabeza, las i==8 e i==14 →
        /// aletas, el resto → espina):
        ///   · LA CABEZA: el orbe brillante con la MANDÍBULA abierta en V,
        ///     sus DOS OJOS y la cresta;
        ///   · LAS ALETAS: abanicos de tres varillas frías perpendicular al
        ///     cuerpo (la aleta de la i==8 mira a un lado, la de la i==14
        ///     al otro — la simetría del pez original);
        ///   · LA ESPINA: las vértebras — cápsula a lo largo del cuerpo +
        ///     la estrella de 4 puntas del pincel de la casa como apófisis.
        /// Las posiciones llegan resueltas por la cadena follow() del
        /// proyectil (la MISMA ley de la imagen 1 — el código compartido
        /// de ambas demostraciones).
        /// </summary>
        /// <param name="segs">Posiciones de MUNDO de los 16 segmentos.</param>
        /// <param name="ang">Ángulo de cada segmento (rad — la dirección del nado).</param>
        /// <param name="n">Número de segmentos vivos.</param>
        /// <param name="time">Tiempo animado.</param>
        /// <param name="seed">Semilla determinista.</param>
        /// <param name="alpha">Intensidad global (0..1).</param>
        public static void SierpeEstelar(Vector2[] segs, float[] ang, int n,
            float time, int seed, float alpha)
        {
            if (n <= 0 || alpha <= 0.02f || Main.netMode == NetmodeID.Server) return;
            if (n > _scr.Length) n = _scr.Length;

            // MUNDO → PANTALLA (el contrato SigiloLib de la casa).
            Vector2 off = Main.screenPosition;
            for (int i = 0; i < n; i++) _scr[i] = segs[i] - off;

            OrbitaLib.AbrirAdditive();
            try
            {
                // El TAPER: la sierpe se afina de la cabeza (15 px) a la cola.
                float RadioDe(int i) => MathHelper.Lerp(15f, 4.5f, i / MathF.Max(1, n - 1));

                // === 1. EL VELO: el cuerpo entero sudando luz tenue. ===
                for (int i = 0; i < n; i++)
                    OrbitaLib.Quad(VFXCore.SoftGlow, _scr[i],
                        new Vector2(RadioDe(i) * 4.4f, RadioDe(i) * 4.4f), 0f,
                        Tint(SierpeHalo, 0.10f * alpha));

                // === 2. LA ESPINA (el prepend("Espina", i) del original):
                //     cápsula a lo largo del cuerpo + la apófisis estelar. ===
                for (int i = 1; i < n; i++)
                {
                    float rd = RadioDe(i);
                    OrbitaLib.Capsule(_scr[i], rd * 2.6f, rd * 0.9f, ang[i],
                        Tint(SierpeCuerpo, 0.55f * alpha));

                    float tw = 0.7f + 0.3f * (float)Math.Sin(time * 2.6f + i * 1.9f);
                    OrbitaLib.Quad(Star, _scr[i], new Vector2(rd * 1.5f, rd * 1.5f),
                        ang[i], Tint(SierpeHalo, 0.60f * tw * alpha));
                }

                // === 3. LAS ALETAS (el prepend("Aletas", i) en las i==8 e
                //     i==14): abanico de TRES varillas frías perpendicular
                //     al cuerpo, alternando lado. ===
                for (int f = 0; f < IdxAletas.Length; f++)
                {
                    int i = IdxAletas[f];
                    if (i >= n) continue;
                    float lado = f == 0 ? 1f : -1f;
                    float perp = ang[i] + MathHelper.PiOver2 * lado;
                    float latido = 0.75f + 0.25f * (float)Math.Sin(time * 3.4f + f * 1.4f);
                    for (int v = -1; v <= 1; v++)
                    {
                        float dirA = perp + v * 0.42f;
                        float largo = (16f + 7f * (1 - Math.Abs(v))) * latido;
                        Vector2 punta = _scr[i] + new Vector2((float)Math.Cos(dirA),
                            (float)Math.Sin(dirA)) * largo;
                        Vector2 mid = (_scr[i] + punta) * 0.5f;
                        OrbitaLib.Capsule(mid, largo, 3.0f, dirA,
                            Tint(AletaColor, 0.50f * alpha));
                        OrbitaLib.Quad(Star, punta, new Vector2(7f, 7f), dirA,
                            Tint(AletaColor, 0.70f * alpha));
                    }
                }

                // === 4. LA CABEZA (el prepend("Cabeza", 1) del original):
                //     el cráneo brillante, la MANDÍBULA abierta en V, los
                //     DOS OJOS fríos y la cresta. ===
                {
                    int i = IdxCabeza;
                    float rumbo = ang[i];

                    // La mandíbula: dos cápsulas abriéndose al frente.
                    for (int m = -1; m <= 1; m += 2)
                    {
                        float dirM = rumbo + m * 0.38f;
                        Vector2 boca = _scr[i] + new Vector2((float)Math.Cos(dirM),
                            (float)Math.Sin(dirM)) * 17f;
                        OrbitaLib.Capsule((_scr[i] + boca) * 0.5f + new Vector2(
                            (float)Math.Cos(rumbo), (float)Math.Sin(rumbo)) * 6f,
                            16f, 4.4f, dirM, Tint(SierpeCuerpo, 0.85f * alpha));
                    }

                    // El cráneo.
                    OrbitaLib.Quad(GlowOrb, _scr[i], new Vector2(30f, 30f), 0f,
                        Tint(SierpeCuerpo, 0.95f * alpha));
                    OrbitaLib.Quad(VFXCore.SoftGlow, _scr[i],
                        new Vector2(52f, 52f), 0f, Tint(SierpeHalo, 0.25f * alpha));

                    // Los DOS OJOS (fríos, fijos al rumbo).
                    for (int o = -1; o <= 1; o += 2)
                    {
                        Vector2 ojo = _scr[i] + new Vector2((float)Math.Cos(rumbo + o * 0.5f),
                            (float)Math.Sin(rumbo + o * 0.5f)) * 9f;
                        OrbitaLib.Quad(GlowOrb, ojo, new Vector2(6.5f, 6.5f), 0f,
                            Tint(new Color(140, 240, 255), 0.95f * alpha));
                    }

                    // La CRESTA: la varilla dorsal de la cabeza.
                    float perpC = rumbo - MathHelper.PiOver2;
                    Vector2 cresta = _scr[i] + new Vector2((float)Math.Cos(perpC),
                        (float)Math.Sin(perpC)) * 14f;
                    OrbitaLib.Capsule((_scr[i] + cresta) * 0.5f, 14f, 3.4f, perpC,
                        Tint(SierpeHalo, 0.60f * alpha));
                }

                // === 5. LAS ESTRELLAS DE LA ESTELA: chispas que la sierpe
                //     deja en su rastro (deterministas, orbitando la cabeza). ===
                for (int k = 0; k < 5; k++)
                {
                    float h = VFXCore.Hash01(seed, 800 + k, 97);
                    float angS = h * MathHelper.TwoPi + time * (1.2f + h);
                    Vector2 p = _scr[IdxCabeza] + new Vector2((float)Math.Cos(angS),
                        (float)Math.Sin(angS)) * (20f + 14f * h);
                    float tw = 0.4f + 0.6f * (float)Math.Sin(time * 3.5f + k * 2.2f);
                    OrbitaLib.Quad(Star, p, new Vector2(7f, 7f), angS,
                        Tint(SierpeHalo, 0.55f * tw * alpha));
                }
            }
            finally
            {
                OrbitaLib.CerrarBatch();
            }
        }
    }
}
