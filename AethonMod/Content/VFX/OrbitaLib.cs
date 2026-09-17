using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// OrbitaLib — v6.34 — LA ESCRITURA MÁGICA DEL VACÍO.
    ///
    /// LA LIBRERÍA DE SIGNOS MÁGICOS DE LOS AGUJEROS NEGROS: todo lo que
    /// hoy viste a los vórtices — el anillo energético con sus VEINTE
    /// BANDAS de emisión viajando (la fórmula fiel del shader:
    /// glow = sin(uv.x·20 + t·5)·0.5+0.5), los ecos en resonancia, los
    /// fotones corriendo el vórtice, el temblor de la distorsión y la
    /// corona de lazos de neón — promovido a PRIMITIVAS INVOCABLES para
    /// embellecer cualquier cosa: portales oscuros, invocaciones de vacío,
    /// maldiciones, jefes, armas.
    ///
    /// LAS LEYES DE LA FAMILIA (heredadas 1:1 del agujero negro cósmico —
    /// ni un número cambiado, el look de los vórtices queda INTACTO):
    ///   · EL ANILLO ENERGÉTICO: elipse oblicua (vórtice), MITAD TRASERA
    ///     y MITAD DELANTERA por separado (el anillo PASA por delante del
    ///     núcleo — el frente más brillante), veinte bandas de brillo
    ///     recorriéndolo a 5 rad/s, turbulencia viva por hash a 12 Hz,
    ///     gradiente térmico de TRES colores (pico = blanco incandescente,
    ///     media = color del anillo, valle = color profundo) y cápsula
    ///     HALO gruesa + cápsula NÚCLEO fina por segmento.
    ///   · LOS ECOS: dos anillos fantasma a ±1 banda de fase — la emisión
    ///     "rebota" en el espacio curvado.
    ///   · LOS FOTONES: corredores blanco-rosa orbitando con estela sobre
    ///     la tangente.
    ///   · LA DISTORSIÓN: el vaivén sinusoidal global (±3% del radio) que
    ///     atraviesa todo el render.
    ///   · LA CORONA DE ARCOS: cinco lazos de neón con asimetría por lazo
    ///     (líneas de campo curvadas), eco interior y NUDOS con destello
    ///     de 4 puntas en los ápices.
    ///
    /// LO NUEVO (el motivo de la librería — el arsenal de embellecimiento):
    ///   · AnilloFotones(...) — el aro fino del horizonte (el anillo de
    ///     fotones puro).
    ///   · OndasDistorsion(...) — anillos de choque expandiéndose desde el
    ///     vórtice.
    ///   · SelloVacio(...) — EL SIGNO MÁGICO INVOCABLE de la casa oscura:
    ///     el círculo del vacío completo (anillo de energía + ecos +
    ///     fotones + aro del horizonte), el par oscuro del SelloSolar.
    ///
    /// CONTRATO DE DIBUJO (DOS secciones, cada una con SU patrón validado):
    ///   · SECCIÓN A (el patrón del agujero negro): las primitivas del
    ///     anillo dibujan al SPRITEBATCH ACTUAL — el llamador abre el
    ///     batch aditivo con los helpers AbrirAdditive()/CerrarBatch()
    ///     (o el suyo propio con Main.GameViewMatrix). CONTRATO: batch
    ///     ABIERTO en aditivo → batch ABIERTO.
    ///   · SECCIÓN B (el patrón de la corona): CoronaArcos emite al BUFFER
    ///     de VFXCore (pase aditivo, coords de mundo) — Begin/Flush del
    ///     llamador, igual que el resto de librerías de la casa.
    /// </summary>
    public static class OrbitaLib
    {
        // ==================================================================
        //  LAS LEYES DE LA FAMILIA — constantes públicas (eran el corazón
        //  privado del agujero negro cósmico; ahora son el CONTRATO)
        // ==================================================================

        /// <summary>Semieje mayor del anillo (×R del vórtice).</summary>
        public const float RingA = 1.90f;

        /// <summary>Semieje menor del anillo (×R del vórtice).</summary>
        public const float RingB = 1.18f;

        /// <summary>Inclinación de la elipse del anillo (rad — vórtice oblicuo).</summary>
        public const float RingTilt = -0.38f;

        /// <summary>Rotación del anillo: 20°/s de la casa.</summary>
        public const float RingSpin = 0.349f;   // rad/s

        /// <summary>LA FÓRMULA DEL SHADER: bandas por vuelta del anillo.</summary>
        public const float BandFreq = 20f;

        /// <summary>Velocidad de las bandas (rad/s en espacio paramétrico).</summary>
        public const float BandSpeed = 5f;

        /// <summary>Segmentos de cada mitad del anillo.</summary>
        public const int RingSegments = 44;

        /// <summary>Regeneración de la turbulencia (Hz).</summary>
        public const int FlickHz = 12;

        /// <summary>
        /// Distorsión sinusoidal global de la casa (0.3): el vaivén que
        /// atraviesa TODO el render del vórtice.
        /// </summary>
        public const float DistorsionFuerza = 0.3f;

        /// <summary>Fotones corriendo el vórtice (la cuenta de la casa).</summary>
        public const int FotonesCount = 3;

        // ==================================================================
        //  LA PALETA DEL VÓRTICE (los tres peldaños del gradiente térmico +
        //  los colores de los satélites — la casa cósmica rojo-naranja;
        //  cada consumidor puede pasar la SUYA)
        // ==================================================================

        /// <summary>Pico de banda: blanco cálido incandescente.</summary>
        public static readonly Color HotWhite = new(255, 240, 220);

        /// <summary>Media banda: el color vivo del anillo.</summary>
        public static readonly Color RingVivo = new(255, 68, 26);

        /// <summary>Valle de banda: el color profundo.</summary>
        public static readonly Color RingProfundo = new(200, 30, 20);

        // ==================================================================
        //  EL PINCEL (las texturas procedurales del proyecto)
        // ==================================================================

        private static Asset<Texture2D> _glow;
        private static Asset<Texture2D> _ring;

        private static Texture2D Glow =>
            (_glow ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow")).Value;

        private static Texture2D Ring =>
            (_ring ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Ring")).Value;

        // ==================================================================
        //  SECCIÓN A · LOS HELPERS DE BATCH (el patrón del agujero negro)
        // ==================================================================

        /// <summary>Abre el SpriteBatch en ADITIVO con la matriz del juego.</summary>
        public static void AbrirAdditive()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>Abre el SpriteBatch en ALFA (el pase de lo oscuro).</summary>
        public static void AbrirAlpha()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>Cierra el SpriteBatch (el contrato: CERRADO al salir).</summary>
        public static void CerrarBatch() => Main.spriteBatch.End();

        /// <summary>Quad centrado al batch actual (tamaño total = size px).</summary>
        public static void Quad(Texture2D tex, Vector2 pos, Vector2 size, float rot, Color tint)
        {
            if (tint.A == 0) return;
            Main.spriteBatch.Draw(tex, pos, null, tint, rot,
                new Vector2(tex.Width, tex.Height) * 0.5f,
                size / new Vector2(tex.Width, tex.Height),
                SpriteEffects.None, 0f);
        }

        /// <summary>Cápsula de luz: SoftGlow estirado (largo × ancho px).</summary>
        public static void Capsule(Vector2 mid, float len, float width, float rot, Color tint)
        {
            if (tint.A == 0) return;
            Quad(Glow, mid, new Vector2(len + width, width * 1.9f), rot, tint);
        }

        /// <summary>
        /// Anillo fino: Ring.png calibrado para radio VISIBLE
        /// (la constante 2.174 medida del pincel del proyecto).
        /// </summary>
        public static void AnilloFino(Vector2 pos, float radioVisible, float rot, Color tint)
        {
            if (tint.A == 0) return;
            float s = radioVisible * 2.174f;
            Quad(Ring, pos, new Vector2(s, s), rot, tint);
        }

        /// <summary>Punto sobre la elipse del anillo (param t en rad).</summary>
        public static Vector2 Ellipse(Vector2 c, float r, float t)
        {
            return VFXCore.Ellipse(c, RingA * r, RingB * r, RingTilt, t);
        }

        /// <summary>Tinte de la familia del vacío: RGB intacto, alfa = intensidad.</summary>
        public static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color(c.R, c.G, c.B, (byte)(int)(255f * f));
        }

        // ==================================================================
        //  PRIMITIVA 0 — LA DISTORSIÓN (el temblor global del vórtice)
        // ==================================================================

        /// <summary>
        /// EL VAIVÉN SINUSOIDAL GLOBAL: devuelve el factor de respiración
        /// del radio (≈1 ± 3%) — la distorsión 0.3 de la casa atravesando
        /// todo lo que orbita el vórtice. Multiplícalo por el radio.
        /// </summary>
        public static float Distorsion(float time, float fuerza = DistorsionFuerza)
        {
            return 1f + 0.03f * ((float)Math.Sin(time) * fuerza);
        }

        // ==================================================================
        //  PRIMITIVA 1 — EL ANILLO ENERGÉTICO (la fórmula fiel del shader)
        // ==================================================================

        /// <summary>
        /// EL ANILLO ENERGÉTICO — la fórmula EXACTA del shader de la casa:
        /// glow = sin(uv.x·20 + t·5)·0.5+0.5 → VEINTE BANDAS de emisión
        /// recorriendo la elipse del vórtice, con turbulencia viva por hash
        /// (12 Hz), gradiente térmico de TRES colores, cápsula HALO gruesa
        /// + cápsula NÚCLEO fina y el punto blanco del pico. Se pinta por
        /// MITADES (trasera/delantera) para que el anillo PUEDA pasar por
        /// delante del núcleo del consumidor.
        /// </summary>
        /// <param name="center">Centro en coords de PANTALLA (mundo − Main.screenPosition — el batch abre con GameViewMatrix).</param>
        /// <param name="rr">Radio respirado del vórtice (px — ya con Distorsion aplicado).</param>
        /// <param name="time">Tiempo animado.</param>
        /// <param name="seed">Semilla determinista del disparo.</param>
        /// <param name="flick">Tick de turbulencia ((int)(time·12)).</param>
        /// <param name="front">TRUE = mitad DELANTERA (cruza por delante); FALSE = trasera.</param>
        /// <param name="hot">Color del pico de banda (blanco incandescente).</param>
        /// <param name="mid">Color de media banda (el vivo).</param>
        /// <param name="deep">Color del valle (el profundo).</param>
        /// <param name="semiA">Semieje mayor (×rr) — la casa: 1.90.</param>
        /// <param name="semiB">Semieje menor (×rr) — la casa: 1.18.</param>
        /// <param name="tilt">Inclinación del vórtice — la casa: −0.38.</param>
        /// <param name="bright">Multiplicador de brillo — trasera 0.90 / delantera 1.30.</param>
        public static void AnilloEnergia(Vector2 center, float rr, float time, int seed, int flick,
            bool front, Color? hot = null, Color? mid = null, Color? deep = null,
            float semiA = RingA, float semiB = RingB, float tilt = RingTilt, float bright = -1f)
        {
            // La mitad delantera (t ∈ 0..π) cruza POR DELANTE del núcleo.
            float t0 = front ? 0f : MathHelper.Pi;
            float span = MathHelper.Pi;
            float bMul = bright >= 0f ? bright : (front ? 1.30f : 0.90f);
            Color cHot = hot ?? HotWhite;
            Color cMid = mid ?? RingVivo;
            Color cDeep = deep ?? RingProfundo;

            // El anillo ROTA (20°/s de la casa).
            float spin = time * RingSpin;

            for (int s = 0; s < RingSegments; s++)
            {
                float t = t0 + span * (s + 0.5f) / RingSegments;
                float tSpin = t + spin;   // el patrón de bandas gira con el anillo

                // Posición + tangente de la cápsula (la elipse paramétrica).
                Vector2 a = VFXCore.Ellipse(center, semiA * rr, semiB * rr, tilt,
                    t - span / (RingSegments * 2f));
                Vector2 b = VFXCore.Ellipse(center, semiA * rr, semiB * rr, tilt,
                    t + span / (RingSegments * 2f));
                Vector2 midPos = (a + b) * 0.5f;
                Vector2 seg = b - a;
                float segLen = seg.Length();
                if (segLen < 0.5f) continue;
                float rot = (float)Math.Atan2(seg.Y, seg.X);

                // ============================================================
                //  LA FÓRMULA EXACTA DEL SHADER:
                //  glow = sin(uv.x · 20 + _Time.y · 5) · 0.5 + 0.5
                //  → VEINTE BANDAS de emisión recorriendo el anillo.
                // ============================================================
                float glow = 0.5f + 0.5f * (float)Math.Sin(tSpin * BandFreq - time * BandSpeed);

                // Turbulencia viva por hash (regenerada a 12 Hz).
                float turb = 0.74f + 0.26f * VFXCore.Hash01(seed, 700 + s, flick);
                float inten = glow * turb * bMul;

                // Gradiente térmico: banda al máximo = blanco incandescente;
                // media = color vivo; valle = color profundo.
                Color c;
                if (inten > 0.82f) c = cHot;
                else if (inten > 0.45f) c = cMid;
                else c = cDeep;

                // Cápsula HALO (gruesa, tenue) + cápsula NÚCLEO (fina, viva).
                Capsule(midPos, segLen, 0.36f * rr * (0.7f + inten), rot, Tint(c, 0.40f * inten));
                Capsule(midPos, segLen, 0.12f * rr * inten, rot, Tint(c, 0.85f * inten));

                // En el PICO de cada banda, un punto blanco extra.
                if (inten > 0.86f)
                {
                    Quad(Glow, midPos, new Vector2(0.32f * rr, 0.32f * rr), rot,
                        Tint(cHot, 0.70f * (inten - 0.86f) / 0.14f));
                }
            }
        }

        // ==================================================================
        //  PRIMITIVA 2 — LOS ECOS DEL ANILLO (la resonancia del shader)
        // ==================================================================

        /// <summary>
        /// LOS ECOS: dos anillos fantasma a ±1 banda de fase del shader —
        /// la emisión del anillo "rebota" en el espacio curvado. Respiran
        /// con la distorsión sinusoidal global.
        /// </summary>
        /// <param name="center">Centro en coords de PANTALLA (mundo − Main.screenPosition — el batch abre con GameViewMatrix).</param>
        /// <param name="rr">Radio respirado (px).</param>
        /// <param name="time">Tiempo animado.</param>
        /// <param name="distortion">La distorsión cruda (sin(time)·0.3).</param>
        /// <param name="vivo">Color del anillo vivo.</param>
        /// <param name="profundo">Color del eco profundo.</param>
        public static void EcosAnillo(Vector2 center, float rr, float time, float distortion,
            Color? vivo = null, Color? profundo = null)
        {
            Color cVivo = vivo ?? RingVivo;
            Color cDeep = profundo ?? RingProfundo;

            float phase = time * BandSpeed * 0.20f;

            float echo1 = 0.55f + 0.45f * (float)Math.Sin(phase);
            AnilloFino(center, 1.42f * rr, time * RingSpin, Tint(cVivo, 0.16f * echo1));

            float echo2 = 0.55f + 0.45f * (float)Math.Sin(phase + MathHelper.Pi);
            AnilloFino(center, 0.80f * rr, -time * RingSpin * 0.7f, Tint(cDeep, 0.14f * echo2));

            // Un velo tenue de emisión alrededor del anillo entero.
            Quad(Glow, center, new Vector2(4.4f * rr, 3.6f * rr), RingTilt,
                Tint(cVivo, 0.10f + 0.05f * distortion));
        }

        // ==================================================================
        //  PRIMITIVA 3 — LOS FOTONES (luz corriendo el vórtice)
        // ==================================================================

        /// <summary>
        /// LOS CORREDORES DE FOTONES: destellos blanco-vivo orbitando el
        /// vórtice con estela sobre la tangente — luz "corriendo" en
        /// círculos alrededor de lo que no la deja escapar.
        /// </summary>
        /// <param name="center">Centro en coords de PANTALLA (mundo − Main.screenPosition — el batch abre con GameViewMatrix).</param>
        /// <param name="rr">Radio respirado (px).</param>
        /// <param name="time">Tiempo animado.</param>
        /// <param name="count">Número de fotones (la casa: 3).</param>
        /// <param name="vivo">Color del halo del fotón.</param>
        /// <param name="caliente">Color del núcleo del fotón.</param>
        public static void Fotones(Vector2 center, float rr, float time, int count = FotonesCount,
            Color? vivo = null, Color? caliente = null)
        {
            Color cVivo = vivo ?? RingVivo;
            Color cHot = caliente ?? HotWhite;

            for (int i = 0; i < count; i++)
            {
                // Orbitan con la rotación del anillo más su propia velocidad.
                float t = time * (RingSpin + 0.55f + 0.20f * i) + i * 2.1f;
                Vector2 pos = Ellipse(center, rr, t);
                float twinkle = 0.65f + 0.35f * (float)Math.Sin(time * 8f + i * 2.3f);

                // Estela corta detrás del fotón (cápsula sobre la tangente).
                Vector2 behind = Ellipse(center, rr, t - 0.22f);
                Vector2 seg = pos - behind;
                float len = seg.Length();
                if (len > 0.5f)
                {
                    float rot = (float)Math.Atan2(seg.Y, seg.X);
                    Vector2 mid = (pos + behind) * 0.5f;
                    Capsule(mid, len, 0.14f * rr, rot, Tint(cVivo, 0.40f * twinkle));
                }

                // Halo vivo + núcleo blanco caliente.
                Quad(Glow, pos, new Vector2(1.05f * rr, 1.05f * rr), 0f,
                    Tint(cVivo, 0.35f * twinkle));
                Quad(Glow, pos, new Vector2(0.42f * rr, 0.42f * rr), 0f,
                    Tint(cHot, 0.85f * twinkle));
            }
        }

        // ==================================================================
        //  PRIMITIVA 4 — LAS ONDAS DE DISTORSIÓN (el choque del vórtice)
        // ==================================================================

        /// <summary>
        /// LAS ONDAS DE DISTORSIÓN: anillos de choque expandiéndose desde
        /// el vórtice en ciclo continuo (nace en el horizonte, se dilata,
        /// se disuelve — y vuelve a nacer). El pulso gravitatorio.
        /// </summary>
        /// <param name="center">Centro en coords de PANTALLA (mundo − Main.screenPosition — el batch abre con GameViewMatrix).</param>
        /// <param name="r">Radio base del vórtice (px).</param>
        /// <param name="time">Tiempo animado.</param>
        /// <param name="seed">Semilla determinista.</param>
        /// <param name="count">Ondas simultáneas (2).</param>
        /// <param name="ciclo">Segundos de ciclo de cada onda (2.4).</param>
        /// <param name="color">Color de la onda.</param>
        public static void OndasDistorsion(Vector2 center, float r, float time, int seed,
            int count = 2, float ciclo = 2.4f, Color? color = null)
        {
            Color c = color ?? RingVivo;
            if (ciclo <= 0.01f) ciclo = 2.4f;   // guard: ciclo degenerado → la casa
            for (int i = 0; i < count; i++)
            {
                float fase = (time / ciclo + i / (float)count + VFXCore.Hash01(seed, 810 + i, 37)) % 1f;
                float growth = (float)Math.Sin(fase * MathHelper.Pi);   // nace → crece → muere
                if (growth < 0.06f) continue;
                float radio = (1.0f + 1.6f * fase) * r;
                float alpha = 0.20f * (1f - fase) * growth;
                AnilloFino(center, radio, time * 0.4f * (i % 2 == 0 ? 1f : -1f),
                    Tint(c, alpha));
            }
        }

        // ==================================================================
        //  SECCIÓN B · LA CORONA DE ARCOS (el patrón buffer de VFXCore)
        // ==================================================================

        /// <summary>Número de lazos de la corona (5: el diseño original).</summary>
        public const int LazosCorona = 5;

        /// <summary>Segmentos por lazo (suavidad del arco).</summary>
        private const int CoronaSegments = 18;

        /// <summary>
        /// LA CORONA DE ARCOS DE NEÓN: cinco lazos con asimetría por lazo
        /// (líneas de campo curvadas, no un arcoíris), un ECO interior más
        /// tenue por lazo y NUDOS con destello de 4 puntas en los ápices.
        /// Respira con el tiempo y balancea cada lazo de forma
        /// determinista — energía VIVA, no un adorno estático.
        ///
        /// CONTRATO: emite al BUFFER de VFXCore (coords de mundo) — el
        /// llamador controla Begin/Flush (VFXCore.Begin() → llamada →
        /// AppendToPlayerDraw/FlushAdditive).
        /// </summary>
        /// <param name="center">Centro de la cabeza que corona (mundo).</param>
        /// <param name="horizonPx">Radio del "horizonte" sobre el que se
        /// arquean los lazos (cabeza humana ≈ 0.55× su ancho).</param>
        /// <param name="time">Tiempo animado (GlobalTimeWrappedHourly).</param>
        /// <param name="alpha">Multiplicador global de intensidad.</param>
        public static void CoronaArcos(Vector2 center, float horizonPx, float time, float alpha = 1f)
        {
            if (horizonPx < 1.5f) return;

            for (int l = 0; l < LazosCorona; l++)
            {
                float t01 = l / (float)(LazosCorona - 1);            // 0 exterior → 1 interior
                float breathe = VFXCore.Breathe(time, 2.2f, l * 1.7f);
                // ASIMETRÍA por lazo (semianchos izq/der distintos y balanceo
                // determinista): las líneas de campo se curvan.
                float sway = 0.18f * VFXCore.Sway(time, 0.9f, l * 2.6f);
                float halfWL = horizonPx * (1.55f - 0.30f * t01) * (1f - sway) * breathe;
                float halfWR = horizonPx * (1.55f - 0.30f * t01) * (1f + sway) * breathe;
                float apexH = horizonPx * (2.30f - 0.95f * t01) * breathe;
                float baseY = center.Y - horizonPx * 1.06f;

                // El lazo completo: media elipse superior ASIMÉTRICA por
                // puntos de glow con grosor variable (fino en las bases,
                // corpulento al subir, afilado en el ápice).
                for (int s = 0; s <= CoronaSegments; s++)
                {
                    float ang = s / (float)CoronaSegments * MathHelper.Pi;   // 0..π
                    float edge = (float)Math.Sin(ang);                 // 0 bases, 1 ápice
                    float side = (float)Math.Cos(ang);                 // -1 izq → +1 der
                    float halfW = side < 0f ? halfWL : halfWR;
                    Vector2 pos = new Vector2(
                        center.X - (float)Math.Cos(ang) * halfW,
                        baseY - edge * apexH);

                    Color col = VFXPalettes.CrimsonCourt.Loop(edge);
                    // Grosor: crece hacia arriba, afila en el ápice.
                    float thickness = 0.16f + 0.13f * edge * (1f - 0.35f * edge);
                    float intensity = 0.30f + 0.70f * edge;
                    float pointPx = horizonPx * thickness;

                    Color finalCol = col * (intensity * alpha);
                    VFXCore.Quad(pos, finalCol, new Vector2(pointPx * 2f, pointPx * 2f));
                }

                // ECO interior: un filamento más tenue y fino encajado dentro
                // del lazo (los filamentos encajados de la casa).
                for (int s = 1; s < CoronaSegments; s++)
                {
                    float ang = s / (float)CoronaSegments * MathHelper.Pi;
                    float edge = (float)Math.Sin(ang);
                    float side = (float)Math.Cos(ang);
                    float halfW = (side < 0f ? halfWL : halfWR) * 0.66f;
                    Vector2 pos = new Vector2(
                        center.X - (float)Math.Cos(ang) * halfW,
                        baseY - edge * apexH * 0.72f);
                    Color col = Color.Lerp(VFXPalettes.CrimsonCourt.EchoBase,
                                           VFXPalettes.CrimsonCourt.EchoApex, edge);
                    float echoPx = horizonPx * 0.09f;
                    VFXCore.Quad(pos, col * (0.55f * alpha), new Vector2(echoPx * 2f, echoPx * 2f));
                }

                // NUDO en el ápice: englobado + DESTELLO DE 4 PUNTAS
                // (dos glows estirados en cruz) + chispa blanca central.
                float knotPulse = 0.85f + 0.30f * (float)Math.Sin(time * 3.1f + l * 2.3f);
                Vector2 apex = new Vector2(center.X, baseY - apexH);

                float knotPx = horizonPx * 0.34f;
                VFXCore.Quad(apex, VFXPalettes.CrimsonCourt.Knot * (knotPulse * alpha),
                    new Vector2(knotPx * 2f, knotPx * 2f));

                // Destello de 4 puntas: dos elipses estiradas en cruz.
                float flareLen = horizonPx * 0.85f * knotPulse;
                float flareWide = horizonPx * 0.10f;
                VFXCore.Quad(apex, VFXPalettes.CrimsonCourt.KnotFlare * (knotPulse * 0.85f * alpha),
                    new Vector2(flareLen, flareWide));
                VFXCore.Quad(apex, VFXPalettes.CrimsonCourt.KnotFlare * (knotPulse * 0.85f * alpha),
                    new Vector2(flareWide, flareLen));

                // Chispa blanca central.
                float sparkPx = horizonPx * 0.14f;
                VFXCore.Quad(apex, VFXPalettes.CrimsonCourt.KnotSpark * (knotPulse * 0.9f * alpha),
                    new Vector2(sparkPx * 2f, sparkPx * 2f));
            }
        }

        // ==================================================================
        //  EL COMPUESTO — EL SELLO DEL VACÍO (el signo invocable, NUEVO)
        // ==================================================================

        /// <summary>
        /// EL SELLO DEL VACÍO — EL SIGNO MÁGICO INVOCABLE de la casa
        /// oscura (el par de SelloSolar): el círculo completo del vórtice —
        /// anillo energético con SUS veinte bandas viajando (ambas
        /// mitades), los ecos en resonancia, los fotones corriendo el
        /// aro, el ANILLO DE FOTONES del horizonte y las ondas de
        /// distorsión pulsando. Sin núcleo negro: SOLO el signo — el
        /// consumidor lo pone debajo de lo que quiera (o encima de un
        /// cuerpo, un portal, un altar, un jefe).
        ///
        /// CONTRATO (Sección A): abre y CIERRA su propio batch — el
        /// SpriteBatch del llamador debe estar CERRADO al llamar (y queda
        /// CERRADO al salir), igual que el render completo del vórtice.
        /// </summary>
        /// <param name="center">Centro en coords de PANTALLA (mundo − Main.screenPosition).</param>
        /// <param name="radius">Radio del vórtice (px — el anillo llega a ~1.9×).</param>
        /// <param name="time">Tiempo animado (GlobalTimeWrappedHourly).</param>
        /// <param name="seed">Semilla determinista.</param>
        /// <param name="vivo">Color vivo del anillo (la casa: rojo-naranja).</param>
        /// <param name="profundo">Color profundo (valle de banda).</param>
        /// <param name="caliente">Color del pico incandescente.</param>
        /// <param name="alpha">Multiplicador global de intensidad.</param>
        public static void SelloVacio(Vector2 center, float radius, float time, int seed,
            Color? vivo = null, Color? profundo = null, Color? caliente = null,
            float alpha = 1f)
        {
            if (radius < 2f || alpha <= 0.02f) return;
            Color cVivo = (vivo ?? RingVivo) * alpha;
            Color cDeep = profundo ?? RingProfundo;
            Color cHot = caliente ?? HotWhite;

            try
            {
                int flick = (int)(time * FlickHz);
                float distortion = (float)Math.Sin(time) * DistorsionFuerza;
                float rr = radius * Distorsion(time);

                AbrirAdditive();

                // --- 1. LOS ECOS en resonancia (la resonancia del shader) ---
                EcosAnillo(center, rr, time, distortion, cVivo, cDeep);

                // --- 2. EL ANILLO ENERGÉTICO, mitad TRASERA ---
                AnilloEnergia(center, rr, time, seed, flick, front: false,
                    hot: cHot, mid: cVivo, deep: cDeep);

                // --- 3. EL ANILLO DE FOTONES del horizonte (el aro fino) ---
                AnilloFino(center, 1.02f * radius, time * 0.15f,
                    Tint(cVivo, 0.40f + 0.12f * (float)Math.Sin(time * 1.7f)));

                // --- 4. EL ANILLO ENERGÉTICO, mitad DELANTERA ---
                AnilloEnergia(center, rr, time, seed, flick, front: true,
                    hot: cHot, mid: cVivo, deep: cDeep);

                // --- 5. LOS FOTONES corriendo el vórtice ---
                Fotones(center, rr, time, FotonesCount, cVivo, cHot);

                // --- 6. LAS ONDAS DE DISTORSIÓN pulsando ---
                OndasDistorsion(center, radius, time, seed, 2, 2.4f, cVivo);

                CerrarBatch();
                // El batch queda CERRADO (contrato).
            }
            catch
            {
                // Cierre defensivo SOLO en el path de error.
                try { Main.spriteBatch.End(); } catch { }
            }
        }
    }
}
