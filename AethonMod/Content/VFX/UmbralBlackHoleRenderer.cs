using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// UmbralBlackHoleRenderer — v6.16 — EL AGUJERO NEGRO DEL UMBRAL, 100% CÓDIGO.
    ///
    /// NACIDO DE LA REFERENCIA DEL USUARIO (imagen analizada en 8 capas):
    /// "pon la referencia de fondo y comienza a agregarle cosas hasta
    /// llegar al agujero negro de la referencia, todo por código".
    ///
    /// LA REFERENCIA (análisis VLM exhaustivo):
    ///   · FONDO: negro casi absoluto con polvo escaso carmesí/naranja,
    ///     más denso cerca del centro.
    ///   · NÚCLEO: círculo de negro absoluto (~25% del ancho) con
    ///     filamentos violeta-azul tenues cayendo dentro (cuadrante sup-izq).
    ///   · DISCO DE ACRECIÓN OBLICUO en elipse, NO uniforme: el lado
    ///     DERECHO (Doppler, acercándose) más grueso, brillante y BLANCO;
    ///     el IZQUIERDO (alejándose) fino, apagado y ROJO PROFUNDO.
    ///     Gradiente radial: blanco-amarillo → naranja → rojo neón →
    ///     magenta → púrpura. Textura de ESTRÍAS LARGAS Y CURVAS pintadas
    ///     (motion blur, esmerilado) — NO un anillo sólido.
    ///   · PÚA DE ENERGÍA blanco-rosa prominente al lado derecho.
    ///   · BRUMAS: velos amplios magenta/carmesí hacia sup-derecha e
    ///     inf-izquierda (materia vaporizada, 30-50%).
    ///   · RAYO naranja-rojo dentado ramificando hacia abajo (inf-derecha).
    ///   · CÍRCULO DE RUNAS dorado-ámbar (255,140,20) CONGIGENTE con el
    ///     conjunto, CON HUECOS (fallas, sobre todo donde la púa lo cruza),
    ///     trazos finos y elegantes como grabados a láser.
    ///   · BRASAS con estelas: naranja-amarillo, rojas, blancas.
    ///
    /// TODO se compone AQUÍ, CADA FRAME, por código (~400 cuadros de luz)
    /// con solo TRES pinceles genéricos de la biblioteca VFX (SoftGlow,
    /// Ring, BlackDisk). Cero sprites de arte, cero estado, cero red.
    ///
    /// CONTRATO DE BATCH (v6.10): Draw() exige el SpriteBatch CERRADO
    /// y lo deja CERRADO.
    /// </summary>
    public static class UmbralBlackHoleRenderer
    {
        // ==================================================================
        //  PARÁMETROS — calibrados contra la referencia
        // ==================================================================

        /// <summary>Radio de la esfera negra en px a escala 1 — GIGANTE.</summary>
        public const float SpherePx = 46f;

        // ==================================================================
        //  LA GEOMETRÍA MEDIDA — la topología "∞" de la referencia (perfil
        //  vertical/horizontal píxel a píxel): esfera de vacío + DISCO FINO
        //  casi de canto (la mitad delantera cruza POR DEBAJO-delante),
        //  ARCO DE LENTE sobre la esfera (el lado lejano doblado ARRIBA,
        //  con línea de filo), ARCO INFERIOR magenta y ALA ANCHA barrida
        //  abajo a la izquierda (donde el Doppler ARDE).
        // ==================================================================

        /// <summary>EL DISCO FINO — la línea delantera cruza a ~1.4R bajo la esfera (medido: banda 1.3-1.9R).</summary>
        private const float RingA = 2.60f;      // semieje mayor (×R)
        private const float RingB = 1.40f;      // semieje menor (×R)
        private const float RingTilt = -0.10f;  // rad — inclinación leve

        /// <summary>EL ALA BARRIDA — banda circular en el frente (medida a ~2.45R, de 110° a 265°).</summary>
        private const float WingRadius = 2.45f;     // ×R — radio de la banda del ala
        private const float WingSquash = 0.92f;     // achatado vertical leve

        /// <summary>EL ARCO DE LENTE superior (el lado lejano doblado sobre la esfera) — DOBLE.</summary>
        private const float TopArcRadius = 1.45f;   // ×R — banda externa
        private const float TopArcInner = 1.22f;    // ×R — el segundo anillo de fotones

        /// <summary>EL ARCO INFERIOR magenta (la imagen lenseda de abajo).</summary>
        private const float BotArcRadius = 1.28f;   // ×R

        /// <summary>
        /// Ángulo del BEAMING DOPPLER — MEDIDO: el extremo IZQUIERDO del
        /// eje mayor ARDE (lum 185 vs 128 del derecho); los núcleos blancos
        /// se concentran en 120-180° (abajo-izquierda).
        /// </summary>
        private const float DopplerAngle = 3.05f;

        /// <summary>Centro de la CUÑA OSCURA medida (240-270° → t≈4.4).</summary>
        private const float WedgeAngle = 4.45f;

        /// <summary>Semi-ancho de la cuña oscura (rad).</summary>
        private const float WedgeHalf = 0.55f;

        /// <summary>Deriva de las estrías a lo largo del disco (rad/s).</summary>
        private const float StreakFlow = 0.42f;

        /// <summary>Estrías pintadas por mitad del disco.</summary>
        private const int StreaksPerHalf = 26;

        /// <summary>Regeneración de las estrías (Hz) — lenta: es pintura viva.</summary>
        private const float FlickHz = 4f;

        /// <summary>Volúmenes del ALA barrida (la masa gaseosa del frente).</summary>
        private const int PlasmaVolumes = 9;

        // --- la púa de energía (lado superior-derecho, medido) ---
        private const float SpikeAngle = -0.55f;  // t de la base de la púa
        private const float SpikeLen = 1.45f;     // ×R

        // --- el rayo naranja (abajo-derecha) ---
        private const float BoltAngle = 0.55f;    // t de la base del rayo

        // --- el círculo de runas (CON HUECOS) — radio MEDIDO ≈ el del anillo ---
        // (v6.37: los 30 segmentos del aro roto viven AHORA en
        //  OrbitaLib.ErosionSegmentos — el CircleSegments local se retiró
        //  con el refactor porque quedó sin uso.)
        private const int RuneCount = 12;
        private const float RuneRadius = 2.55f;   // ×R — a la altura del anillo
        private const float RuneOrbit = 0.06f;    // rad/s — gira MUY lento

        // --- ondas de distorsión ---
        private const float WaveCycle = 2.8f;
        private const int WaveCount = 2;

        // ==================================================================
        //  PALETA — los RGB MEDIDOS píxel a píxel sobre la referencia:
        //  núcleos blanco-rosado (249,210,220), rosa caliente (243,128,149),
        //  rosa (225,74,127), carmesí-rosa (183,29,83), magenta profundo
        //  (153,14,76), desvaneciendo a vino (110,17,51). Runas ámbar
        //  (240,124,65). LA FAMILIA ES ROSA/MAGENTA — no naranja.
        // ==================================================================

        private static readonly Color HotInner = new(250, 210, 220);   // núcleo blanco-rosado (MEDIDO)
        private static readonly Color HotRose = new(243, 128, 149);     // rosa caliente (MEDIDO)
        private static readonly Color ArcSalmon = new(248, 110, 95);    // salmón del arco superior (MEDIDO (248,103,88))
        private static readonly Color WingMagenta = new(240, 41, 168);   // magenta del ala/arco inferior (MEDIDO)
        private static readonly Color MidRose = new(225, 74, 127);      // rosa (MEDIDO)
        private static readonly Color Rose = new(183, 29, 83);          // carmesí-rosa (MEDIDO)
        private static readonly Color DeepRose = new(153, 14, 76);      // magenta profundo (MEDIDO)
        private static readonly Color WineFade = new(110, 17, 51);      // vino exterior (MEDIDO)
        private static readonly Color MagentaViolet = new(130, 25, 145);// violeta secundario (tonos 300-315°)
        private static readonly Color SpikeWhite = new(250, 200, 225);  // blanco-rosa de la púa
        private static readonly Color BoltOrange = new(255, 100, 0);    // rayo naranja-rojo
        private static readonly Color RuneGold = new(240, 124, 65);     // dorado-ámbar (MEDIDO)
        private static readonly Color RuneTip = new(255, 205, 140);     // punta pálida
        private static readonly Color WispPink = new(200, 20, 120);     // velo magenta
        private static readonly Color WispCrimson = new(160, 10, 50);   // velo carmesí
        private static readonly Color EmberYellow = new(255, 180, 50);  // brasa naranja-amarilla
        private static readonly Color EmberRed = new(200, 40, 40);      // brasa roja
        private static readonly Color EmberWhite = new(255, 255, 240);  // brasa blanca

        // ==================================================================
        //  PINCELES (los tres genéricos de la biblioteca, generados por código)
        // ==================================================================

        private static Asset<Texture2D> _glow;
        private static Asset<Texture2D> _ring;
        private static Asset<Texture2D> _disk;

        private static Texture2D Glow =>
            (_glow ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow")).Value;

        private static Texture2D Ring =>
            (_ring ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Ring")).Value;

        private static Texture2D Disk =>
            (_disk ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/BlackDisk")).Value;

        // ==================================================================
        //  HELPERS DE DIBUJO
        // ==================================================================

        private static void BeginAdditive()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
        }

        private static void BeginAlpha()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>Quad centrado (tamaño total = size px), rotación opcional.</summary>
        private static void Quad(Texture2D tex, Vector2 pos, Vector2 size, float rot, Color tint)
        {
            if (tint.A == 0) return;
            Main.spriteBatch.Draw(tex, pos, null, tint, rot,
                new Vector2(tex.Width, tex.Height) * 0.5f,
                size / new Vector2(tex.Width, tex.Height),
                SpriteEffects.None, 0f);
        }

        /// <summary>Cápsula de luz: SoftGlow estirado (largo × ancho px).</summary>
        private static void Capsule(Vector2 mid, float len, float width, float rot, Color tint)
        {
            if (tint.A == 0) return;
            Quad(Glow, mid, new Vector2(len + width, width * 1.9f), rot, tint);
        }

        /// <summary>Anillo fino: Ring.png calibrado para radio visible.</summary>
        private static void RingQuad(Vector2 pos, float visibleRadius, float rot, Color tint)
        {
            if (tint.A == 0) return;
            float s = visibleRadius * 2.174f;
            Quad(Ring, pos, new Vector2(s, s), rot, tint);
        }

        /// <summary>Hash determinista [0,1).</summary>
        private static float Hash01(int seed, int a, int b)
        {
            int h = unchecked(seed * 374761393 + a * 668265263 + b * 1911520717);
            h = unchecked(h ^ (h >> 13));
            h = unchecked(h * 1274126177);
            h = unchecked(h ^ (h >> 16));
            return (h & 0xFFFFFF) / 16777216f;
        }

        /// <summary>Tinte de INTENSIDAD LINEAL (patrón validado del Cometa).</summary>
        private static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color(c.R, c.G, c.B, (byte)(int)(255f * f));
        }

        /// <summary>Punto sobre la elipse del disco (param t en rad).</summary>
        private static Vector2 Ellipse(Vector2 c, float r, float t)
        {
            return VFXCore.Ellipse(c, RingA * r, RingB * r, RingTilt, t);
        }

        /// <summary>
        /// EL BEAMING DOPPLER de la referencia (medido): máximo en el
        /// extremo inferior-izquierdo del eje mayor, mínimo en el opuesto.
        /// CÚBICO: el contraste medido entre el sector caliente (lum 167)
        /// y los apagados (lum 30-68) es ~4:1.
        /// </summary>
        private static float Doppler(float t)
        {
            float d = 0.5f + 0.5f * (float)Math.Cos(t - DopplerAngle);
            return d * d * d;
        }

        /// <summary>
        /// LA CUÑA OSCURA medida (240-270° casi muerto): el disco tiene un
        /// SECTOR apagado donde la sombra del agujero muerde el anillo.
        /// </summary>
        private static float Wedge(float t)
        {
            float d = Math.Abs(MathHelper.WrapAngle(t - WedgeAngle));
            // 1 fuera de la cuña → 0.15 en su centro.
            float k = MathHelper.Clamp(1f - d / WedgeHalf, 0f, 1f);
            return 1f - 0.85f * k * k;
        }

        // ==================================================================
        //  EL RENDER COMPLETO — contrato: batch CERRADO → CERRADO
        // ==================================================================

        public static void Draw(Vector2 center, float scale, float time, int seed)
        {
            float r = SpherePx * Math.Max(scale, 0.02f);
            if (r < 2f) return;   // el batch queda INTACTO (cerrado)

            try
            {
                int flick = (int)(time * FlickHz);
                float breathe = 1f + 0.012f * (float)Math.Sin(time * 1.1f);
                float rr = r * breathe;

                // ============ 0. AURA OSCURA (alfa) ============
                // El negro casi absoluto de la referencia ABRAZA el mundo.
                BeginAlpha();
                Quad(Glow, center, new Vector2(7.4f * rr, 7.4f * rr), 0f,
                    new Color(16, 2, 6, 175));
                Main.spriteBatch.End();

                // ============ 1..11: TODO LO BRILLANTE (aditivo) ============
                BeginAdditive();

                // --- 1. POLVO DE FONDO carmesí/naranja (denso cerca) ---
                DrawBackgroundDust(center, rr, time, seed);

                // --- 2. BRUMAS/VELOES amplios (materia vaporizada) ---
                DrawVaporWisps(center, rr, time, seed);

                // --- 3. EL LADO LEJANO DOBLADO: arco de lente SUPERIOR ---
                // (detrás de la esfera — la referencia lo muestra como una
                // línea de filo fina y brillante arqueando POR ENCIMA).
                DrawLensingArcTop(center, r, time);

                // --- 3b. DISCO FINO — mitad TRASERA (apenas visible: el lado
                //      lejano real está casi todo DOBLADO en los arcos) ---
                DrawAccretionDisk(center, rr, time, seed, flick, front: false);

                // --- 4. NÚCLEO + filamentos interiores ---
                Main.spriteBatch.End();
                BeginAlpha();
                Quad(Disk, center, new Vector2(2.28f * r, 2.28f * r), 0f, Color.White);
                Main.spriteBatch.End();
                BeginAdditive();
                // Filo púrpura del horizonte (la última luz doblada).
                RingQuad(center, 1.02f * r, time * 0.12f,
                    Tint(MagentaViolet, 0.34f + 0.10f * (float)Math.Sin(time * 1.5f)));
                // (v6.18: el núcleo queda LIMPIO — sin filamentos interiores.)

                // --- 5. DISCO FINO — mitad DELANTERA: la línea CALIENTE que
                //      cruza POR DEBAJO-DELANTERO de la esfera ---
                DrawAccretionDisk(center, rr, time, seed, flick, front: true);

                // --- 5b. EL ALA BARRIDA — la masa ancha barriendo abajo-
                //      izquierda (donde el Doppler ARDE) ---
                DrawWing(center, rr, time, seed, flick);

                // --- 5b'. EL VACÍO VUELVE A DEVORAR: el ala es TAN ancha
                //      que su resplandor invade el centro — repintamos el
                //      disco negro para que el horizonte SIGA siendo negro
                //      absoluto (el agujero se come el derrame). ---
                Main.spriteBatch.End();
                BeginAlpha();
                Quad(Disk, center, new Vector2(2.28f * r, 2.28f * r), 0f, Color.White);
                Main.spriteBatch.End();
                BeginAdditive();
                // El filo del horizonte, de nuevo (la última luz tras el ala).
                RingQuad(center, 1.02f * r, time * 0.12f,
                    Tint(MagentaViolet, 0.30f + 0.10f * (float)Math.Sin(time * 1.5f)));

                // --- 5c. EL ARCO INFERIOR magenta (la imagen lenseda de abajo) ---
                DrawLensingArcBottom(center, r, time);

                // --- 6. LA PÚA DE ENERGÍA (lado derecho) ---
                DrawEnergySpike(center, rr, time, seed);

                // --- 7. EL RAYO NARANJA (abajo, ramificado) ---
                DrawOrangeBolt(center, r, time, seed, flick);

                // --- 8. EL CÍRCULO DE RUNAS (CON HUECOS) ---
                DrawRuneCircle(center, r, time, seed);

                // --- 9. BRASAS CON ESTELAS ---
                DrawEmbers(center, r, time, seed);

                // --- 10. ONDAS DE DISTORSIÓN ---
                DrawDistortionWaves(center, r, time, seed);

                // --- 11. AURA FINAL carmesí pulsante (en ANILLO, no sobre
                //      el vacío: el centro debe quedar NEGRO ABSOLUTO) ---
                float aura = 0.85f + 0.15f * (float)Math.Sin(time * 1.8f);
                RingQuad(center, 2.6f * rr, time * 0.1f,
                    Tint(new Color(200, 30, 90), 0.16f * aura));

                // --- 12. EL VACÍO FINAL: última devoración — cualquier
                //      derrame aditivo sobre el horizonte se borra y el
                //      centro queda del NEGRO MÁS ABSOLUTO de la referencia. ---
                Main.spriteBatch.End();
                BeginAlpha();
                Quad(Disk, center, new Vector2(2.28f * r, 2.28f * r), 0f, Color.White);
                Main.spriteBatch.End();
                BeginAdditive();
                // El filo del horizonte (la última luz doblada) respira.
                RingQuad(center, 1.02f * r, time * 0.12f,
                    Tint(MagentaViolet, 0.26f + 0.08f * (float)Math.Sin(time * 1.5f)));

                // (v6.18: los FILAMENTOS interiores fueron QUITADOS — el
                //      núcleo queda NEGRO ABSOLUTO y LIMPIO, como en el
                //      agujero de la Bruma. Petición del usuario.)

                Main.spriteBatch.End();
                // El batch queda CERRADO (contrato).
            }
            catch
            {
                // Cierre defensivo SOLO en el path de error (v6.10).
                try { Main.spriteBatch.End(); } catch { }
            }
        }

        // ------------------------------------------------------------------
        //  1. POLVO DE FONDO — carmesí/naranja, denso cerca del centro
        // ------------------------------------------------------------------

        private static void DrawBackgroundDust(Vector2 center, float rr, float time, int seed)
        {
            for (int i = 0; i < 16; i++)
            {
                float h = Hash01(seed, 600 + i, 13);
                float ang = h * MathHelper.TwoPi + time * 0.025f * (i % 2 == 0 ? 1f : -1f);
                // Densidad MAYOR cerca del centro: dist = base + base·h²
                // (el cuadrado sesga la distribución hacia adentro).
                float dist = (1.6f + 3.4f * h * h) * rr * 0.62f;
                Vector2 pos = center + new Vector2(
                    (float)Math.Cos(ang) * dist, (float)Math.Sin(ang) * dist);
                float pulse = 0.5f + 0.5f * (float)Math.Sin(time * 1.2f + i * 2.1f);

                // Carmesí profundo y naranja apagado (los RGB de la referencia).
                Color c = h < 0.55f ? new Color(120, 20, 20) : new Color(180, 80, 20);
                float size = (0.09f + 0.09f * h) * rr;
                Quad(Glow, pos, new Vector2(size, size), 0f, Tint(c, 0.50f * pulse));
            }
        }

        // ------------------------------------------------------------------
        //  2. BRUMAS / VELOS — materia vaporizada en los diagonales
        // ------------------------------------------------------------------

        private static void DrawVaporWisps(Vector2 center, float rr, float time, int seed)
        {
            // Dos velos amplios: sup-derecha e inf-izquierda (los diagonales
            // de la referencia), cada uno un CÚMULO de glows suaves con
            // offsets por hash — bordes nubosos fractales, NO bolas.
            for (int w = 0; w < 2; w++)
            {
                // Dirección diagonal del velo (w=0: sup-derecha; w=1: inf-izq).
                float baseAng = w == 0 ? -0.65f : MathHelper.Pi + 0.55f;
                float dist = 2.35f * rr;
                Vector2 anchor = center + new Vector2(
                    (float)Math.Cos(baseAng) * dist,
                    (float)Math.Sin(baseAng) * dist * 0.8f);

                Color cBase = w == 0 ? WispPink : WispCrimson;

                for (int k = 0; k < 6; k++)
                {
                    float h1 = Hash01(seed, 520 + w * 40 + k, 11);
                    float h2 = Hash01(seed, 521 + w * 40 + k, 19);
                    float h3 = Hash01(seed, 522 + w * 40 + k, 23);

                    // El velo GIRA muy lento alrededor de su ancla.
                    float swirl = time * 0.10f * (w == 0 ? 1f : -1f) + h1 * MathHelper.TwoPi;
                    float offR = (0.4f + 0.75f * h2) * rr;
                    Vector2 pos = anchor + new Vector2(
                        (float)Math.Cos(swirl) * offR,
                        (float)Math.Sin(swirl) * offR * 0.7f);

                    float size = (1.1f + 0.9f * h3) * rr;
                    // Opacidad 30-50% de la referencia, en aditivo tenue.
                    float alpha = 0.09f + 0.09f * h2;
                    float breathe = 0.8f + 0.2f * (float)Math.Sin(time * 0.6f + k * 1.8f);

                    Quad(Glow, pos, new Vector2(size, size * (0.55f + 0.35f * h1)),
                        swirl, Tint(cBase, alpha * breathe));
                }
            }
        }

        // ------------------------------------------------------------------
        //  3/5. EL ALA BARRIDA + EL DISCO FINO + LOS ARCOS DE LENTE
        // ------------------------------------------------------------------

        /// <summary>
        /// EL ALA BARRIDA: la MASA del frente de la referencia — una BANDA
        /// CIRCULAR GORDA en 2.45·R barriendo de abajo-izquierda (donde el
        /// Doppler ARDE) por el fondo hasta abajo-derecha (la referencia:
        /// "the bottom is a broad, sweeping wing of light"; medido: banda
        /// magenta (240,41,168) a 2.4-2.9R bajo la esfera, lum 111-126).
        /// </summary>
        private static void DrawWing(Vector2 center, float rr, float time,
            int seed, int flick)
        {
            const int Segments = 18;
            // La banda barre de 34° (abajo-derecha) por el FONDO hasta 189°
            // (extremo izquierdo, donde el Doppler ARDE) — convención y-ABAJO:
            // 90°=abajo, 180°=izquierda.
            float a0 = 0.60f, a1 = MathHelper.Pi + 0.05f;

            for (int s = 0; s < Segments; s++)
            {
                float t = a0 + (s + 0.5f) / Segments * (a1 - a0);
                float dop = Doppler(t);
                float wedge = Wedge(t);

                Vector2 dir = new Vector2((float)Math.Cos(t), (float)Math.Sin(t) * WingSquash);
                Vector2 pos = center + dir * WingRadius * rr;

                // Tangente local de la banda.
                Vector2 tang = new Vector2(-(float)Math.Sin(t), (float)Math.Cos(t) * WingSquash);
                float rot = (float)Math.Atan2(tang.Y, tang.X);
                float len = (a1 - a0) * WingRadius * rr / Segments * 1.35f;

                // LA BANDA: gruesa (~0.8R de espesor), gorda donde arde.
                float w = (0.52f + 0.34f * dop) * rr;

                // Color: MAGENTA (medido (240,41,168)) con rosa caliente
                // donde el Doppler ARDE.
                Color c = dop > 0.55f ? HotRose : WingMagenta;

                float turb = 0.70f + 0.30f * Hash01(seed, 752 + s, flick);
                float a = (0.42f + 0.50f * dop) * turb * wedge;

                Capsule(pos, len, w * 1.9f, rot, Tint(c, a));
                Capsule(pos, len, w * 0.9f, rot, Tint(c, a * 0.8f));

                // Núcleo interior más caliente donde el Doppler ARDE
                // (la estela blanca del ala en la referencia).
                if (dop > 0.60f)
                {
                    Capsule(pos, len, w * 0.45f, rot,
                        Tint(Color.Lerp(HotRose, HotInner, (dop - 0.60f) / 0.40f),
                            0.55f * dop * turb));
                }
            }
        }

        /// <summary>
        /// EL ARCO DE LENTE SUPERIOR: el lado LEJANO del disco doblado
        /// ARRIBA de la esfera (la topología "∞" de la referencia) — un
        /// arco circular de 1.45·R con LÍNEA DE FILO fina y brillante
        /// ("the top edge appears as a bright, thin line arcing over"),
        /// salmón-rosa, ARDIENDO al lado izquierdo (el Doppler).
        /// Se dibuja DETRÁS de la esfera (la esfera lo devora al pasar).
        /// </summary>
        private static void DrawLensingArcTop(Vector2 center, float r, float time)
        {
            const int Segments = 22;
            // El arco cubre de ~200° a ~340° (SOBRE la cúspide — convención
            // y-ABAJO: 270°=arriba).
            float a0 = MathHelper.Pi + 0.35f, a1 = MathHelper.TwoPi - 0.35f;

            for (int s = 0; s < Segments; s++)
            {
                float t = a0 + (s + 0.5f) / Segments * (a1 - a0);
                float dop = Doppler(t);

                Vector2 dir = new Vector2((float)Math.Cos(t), (float)Math.Sin(t));
                Vector2 pos = center + dir * TopArcRadius * r;

                // Tangente local del arco.
                Vector2 tang = new Vector2(-dir.Y, dir.X);
                float rot = (float)Math.Atan2(tang.Y, tang.X);
                float len = (a1 - a0) * TopArcRadius * r / Segments * 1.35f;

                // LA BANDA del arco: GRUESA (medida: banda de ~40px a
                // escala 70px ≈ 0.57R) y BRILLANTE en TODA su extensión
                // (la referencia arde a lum 143 incluso directamente
                // encima), salmón-rosa — el Doppler solo acompaña.
                Color cGlow = Color.Lerp(ArcSalmon, HotRose, dop);
                float pulse = 0.80f + 0.20f * (float)Math.Sin(time * 2.0f + s * 0.7f);
                Capsule(pos, len, (0.30f + 0.20f * dop) * r, rot,
                    Tint(cGlow, (0.42f + 0.26f * dop) * pulse));

                // EL SEGUNDO ANILLO DE FOTONES: más apretado (1.22·R),
                // tenue — el "back-of-the-head loop" de la referencia (dos
                // bucles lensed anidados sobre la cúspide).
                Vector2 pos2 = center + dir * TopArcInner * r;
                Capsule(pos2, len * 0.9f, (0.05f + 0.04f * dop) * r, rot,
                    Tint(Color.Lerp(ArcSalmon, HotRose, dop),
                        (0.30f + 0.30f * dop) * pulse));

                // EL FILO: la línea fina INCANDESCENTE en el borde interior
                // (el rasgo icónico: "a bright, thin line arcing over").
                Color cEdge = Color.Lerp(HotRose, HotInner, dop);
                Capsule(pos, len, (0.055f + 0.05f * dop) * r, rot,
                    Tint(cEdge, (0.80f + 0.20f * dop) * pulse));
            }
        }

        /// <summary>
        /// EL ARCO INFERIOR magenta: la imagen lenseda del disco por DEBAJO
        /// (medido: banda magenta (240,41,168) bajo la esfera) — circular,
        /// 1.28·R, abrazando la panza del vacío. Se dibuja DELANTE de la
        /// esfera (la luz doblada pasa por delante del horizonte inferior).
        /// </summary>
        private static void DrawLensingArcBottom(Vector2 center, float r, float time)
        {
            const int Segments = 12;
            // El arco cubre de ~37° a ~160° (BAJO la panza — convención
            // y-ABAJO: 90°=abajo), asimétrico hacia el lado caliente.
            float a0 = 0.65f, a1 = MathHelper.Pi - 0.35f;

            for (int s = 0; s < Segments; s++)
            {
                float t = a0 + (s + 0.5f) / Segments * (a1 - a0);
                float dop = Doppler(t);

                Vector2 dir = new Vector2((float)Math.Cos(t), (float)Math.Sin(t));
                Vector2 pos = center + dir * BotArcRadius * r;

                Vector2 tang = new Vector2(-dir.Y, dir.X);
                float rot = (float)Math.Atan2(tang.Y, tang.X);
                float len = (a1 - a0) * BotArcRadius * r / Segments * 1.15f;

                // Magenta → rosa donde el Doppler acompaña.
                Color c = Color.Lerp(WingMagenta, MidRose, dop * 0.7f);
                float pulse = 0.80f + 0.20f * (float)Math.Sin(time * 1.7f + s * 0.9f);
                Capsule(pos, len, (0.09f + 0.10f * dop) * r, rot,
                    Tint(c, (0.44f + 0.36f * dop) * pulse));
            }
        }

        private static void DrawAccretionDisk(Vector2 center, float rr, float time, int seed,
            int flick, bool front)
        {
            // La mitad delantera (t ∈ 0..π) es la LÍNEA CALIENTE que cruza
            // por debajo-delante; la trasera apenas asoma (el lado lejano
            // está DOBLADO en los arcos de lente).
            float t0 = front ? 0f : MathHelper.Pi;
            float span = MathHelper.Pi;
            float bright = front ? 1.25f : 0.42f;

            // Las estrías DERIVAN por el disco (la pintura fluye).
            float flow = time * StreakFlow;

            for (int s = 0; s < StreaksPerHalf; s++)
            {
                // Nacimiento por hash + deriva: cada estría nace en un punto
                // del semicírculo y VIAJA con el flujo (regeneración lenta).
                float h0 = Hash01(seed, 700 + s, flick / 2);
                float a0 = t0 + ((h0 + flow / MathHelper.TwoPi) % 1f) * span;

                // JITTER RADIAL por estría: la línea NO es una elipse
                // geométrica perfecta — cada estría respira a su radio
                // (0.96..1.08×R) como el plasma distorsionado de la referencia.
                float radialJit = 0.96f + 0.12f * Hash01(seed, 703 + s, 5);

                // DOPPLER en el PUNTO MEDIO de la estría.
                float midT = a0 + 0.10f;
                float dop = Doppler(midT);

                // LARGO de la estría: LARGA en el lado brillante (las
                // pinceladas blancas de la derecha son extensas).
                float arcLen = (0.22f + 0.50f * Hash01(seed, 701 + s, flick / 2)) *
                               (0.45f + 0.75f * dop);

                // GROSOR: el disco es FINO (medido RingB=0.5R): estrías
                // delgadas, algo más gordas donde el Doppler ARDE.
                float wBase = (0.11f + 0.17f * dop) * rr;

                // TURBULENCIA de brillo (la pintura VIVE, a 4 Hz).
                float turb = 0.70f + 0.30f * Hash01(seed, 702 + s, flick);
                float wedge = Wedge(a0 + arcLen * 0.5f);   // LA CUÑA OSCURA

                // --- BANDA INTERNA CALIENTE (borde del horizonte) ---
                // 3 cápsulas siguiendo la curvatura: halo + núcleo.
                for (int seg = 0; seg < 3; seg++)
                {
                    float ta = a0 + arcLen * seg / 3f;
                    float tb = a0 + arcLen * (seg + 1) / 3f;
                    Vector2 pa = Ellipse(center, rr * radialJit, ta);
                    Vector2 pb = Ellipse(center, rr * radialJit, tb);
                    Vector2 mid = (pa + pb) * 0.5f;
                    Vector2 d = pb - pa;
                    float len = d.Length();
                    if (len < 0.5f) continue;
                    float rot = (float)Math.Atan2(d.Y, d.X);

                    // Color del NÚCLEO por Doppler: lado caliente BLANCO-ROSADO,
                    // medio rosa, lado frío magenta profundo → vino.
                    float inten = (0.25f + 0.75f * dop) * turb * bright * wedge;
                    Color core;
                    if (dop > 0.62f) core = HotInner;
                    else if (dop > 0.30f) core = HotRose;
                    else if (dop > 0.10f) core = Rose;
                    else core = DeepRose;

                    Capsule(mid, len, wBase * 2.1f, rot, Tint(Color.Lerp(core, MidRose, 0.25f), 0.55f * inten));
                    Capsule(mid, len, wBase, rot, Tint(core, 1.0f * inten));

                    // Punto BLANCO-ROSADO: los núcleos medidos aparecen por
                    // TODO el anillo (concentrados al lado caliente) — aquí,
                    // donde la turbulencia ARDE, independiente del Doppler.
                    if (turb > 0.82f && inten > 0.28f)
                    {
                        Quad(Glow, mid, new Vector2(0.36f * rr, 0.36f * rr), rot,
                            Tint(HotInner, 0.95f * inten * (turb - 0.82f) / 0.18f));
                    }
                }

                // --- BANDA MEDIA: magenta (a 1.10× del anillo) ---
                {
                    float tm = a0 + arcLen * 0.5f;
                    Vector2 pm = Ellipse(center, rr * 1.10f, tm);
                    Vector2 pn = Ellipse(center, rr * 1.10f, tm + arcLen * 0.55f);
                    Vector2 mid = (pm + pn) * 0.5f;
                    Vector2 d = pn - pm;
                    float len = d.Length();
                    if (len > 0.5f)
                    {
                        float rot = (float)Math.Atan2(d.Y, d.X);
                        float inten = (0.30f + 0.55f * dop) * turb * bright * wedge;
                        Capsule(mid, len, wBase * 1.5f, rot,
                            Tint(MidRose, 0.48f * inten));
                    }
                }

                // --- BANDA EXTERNA: púrpura desvaneciéndose (a 1.22×) ---
                {
                    float tm = a0 + arcLen * 0.4f;
                    Vector2 pm = Ellipse(center, rr * 1.22f, tm);
                    Vector2 pn = Ellipse(center, rr * 1.22f, tm + arcLen * 0.45f);
                    Vector2 mid = (pm + pn) * 0.5f;
                    Vector2 d = pn - pm;
                    float len = d.Length();
                    if (len > 0.5f)
                    {
                        float rot = (float)Math.Atan2(d.Y, d.X);
                        float inten = (0.22f + 0.40f * dop) * turb * bright * wedge;
                        Capsule(mid, len, wBase * 1.2f, rot,
                            Tint(WineFade, 0.40f * inten));
                    }
                }
            }
        }

        // ------------------------------------------------------------------
        //  (v6.18: los FILAMENTOS INTERIORES fueron QUITADOS — el centro
        //  de la bola negra queda LIMPIO, como en el agujero de la Bruma.)
        // ------------------------------------------------------------------

        // ------------------------------------------------------------------
        //  6. LA PÚA DE ENERGÍA — el chorro blanco-rosa del lado derecho
        // ------------------------------------------------------------------

        private static void DrawEnergySpike(Vector2 center, float rr, float time, int seed)
        {
            // La base vive en el extremo derecho del disco (donde el Doppler
            // es máximo) y la púa APUNTA hacia afuera, ligeramente arriba.
            Vector2 basePos = Ellipse(center, rr, SpikeAngle);
            Vector2 outward = basePos - center;
            if (outward.LengthSquared() < 0.01f) return;
            outward.Normalize();
            // Inclinación hacia arriba (la referencia la muestra alzada).
            Vector2 up = new Vector2(0f, -0.34f);
            Vector2 dir = (outward + up).SafeNormalize(Vector2.UnitX);

            float pulse = 0.80f + 0.20f * (float)Math.Sin(time * 2.6f);
            float len = SpikeLen * rr * pulse;

            // Resplandor de la BASE (donde nace, intensísimo).
            Quad(Glow, basePos, new Vector2(1.5f * rr, 1.5f * rr), 0f,
                Tint(SpikeWhite, 0.45f * pulse));
            Quad(Glow, basePos, new Vector2(0.7f * rr, 0.7f * rr), 0f,
                Tint(new Color(255, 250, 245), 0.85f * pulse));

            // CUATRO tramos ahusados: base ancha → punta fina.
            float rot = (float)Math.Atan2(dir.Y, dir.X);
            for (int k = 0; k < 4; k++)
            {
                float f0 = k / 4f, f1 = (k + 1) / 4f;
                float midF = (f0 + f1) * 0.5f;
                Vector2 a = basePos + dir * (f0 * len);
                Vector2 b = basePos + dir * (f1 * len);
                Vector2 mid = (a + b) * 0.5f;
                float segLen = (b - a).Length();

                // Grosor menguante + alfa menguante (la púa se afila).
                float w = (0.34f - 0.26f * midF) * rr;
                float fade = (1f - midF * 0.65f) * pulse;
                Color c = Color.Lerp(SpikeWhite, MidRose, midF * 0.7f);
                Capsule(mid, segLen, w * 2.0f, rot, Tint(c, 0.22f * fade));
                Capsule(mid, segLen, w, rot, Tint(c, 0.55f * fade));
            }

            // Punta incandescente con CRUCE de destello (4 puntas).
            Vector2 tip = basePos + dir * len;
            Quad(Glow, tip, new Vector2(0.75f * rr, 0.75f * rr), 0f,
                Tint(SpikeWhite, 0.40f * pulse));
            Quad(Glow, tip, new Vector2(2.1f * rr, 0.16f * rr), rot,
                Tint(SpikeWhite, 0.35f * pulse));
            Quad(Glow, tip, new Vector2(0.16f * rr, 1.3f * rr), rot,
                Tint(SpikeWhite, 0.35f * pulse));
        }

        // ------------------------------------------------------------------
        //  7. EL RAYO NARANJA — dentado, ramificando hacia abajo
        // ------------------------------------------------------------------

        private static void DrawOrangeBolt(Vector2 center, float r, float time, int seed, int flick)
        {
            int boltFlick = flick;   // 4 Hz: el rayo dura lo que un latido

            // El rayo nace del disco abajo-derecha y CAE hacia afuera-abajo.
            if (Hash01(seed, 890, boltFlick) < 0.35f) return;

            Vector2 start = Ellipse(center, r, BoltAngle);
            Vector2 outward = start - center;
            if (outward.LengthSquared() < 0.01f) return;
            outward.Normalize();
            Vector2 down = new Vector2(0.18f, 0.55f);
            Vector2 dir = (outward + down).SafeNormalize(Vector2.UnitY);

            float len = (1.45f + 0.65f * Hash01(seed, 891, boltFlick)) * r;
            Vector2 end = start + dir * len;

            DrawBolt(start, end, seed + 300, boltFlick, r * 0.11f,
                Tint(new Color(200, 55, 0), 0.55f), Tint(BoltOrange, 0.95f));

            // Núcleo CASI BLANCO en la base del rayo (el plasma fresco).
            Quad(Glow, start, new Vector2(0.6f * r, 0.6f * r), 0f,
                Tint(new Color(255, 220, 170), 0.55f));
        }

        /// <summary>
        /// Un rayo en zigzag con RAMAS (la matemática de BoltRenderer en
        /// coords de pantalla): funda de halo + núcleo fino por segmento +
        /// ramas laterales donde el hash lo pide.
        /// </summary>
        private static void DrawBolt(Vector2 start, Vector2 end, int seed, int flick,
            float width, Color halo, Color core)
        {
            Vector2 delta = end - start;
            float length = delta.Length();
            if (length < 4f) return;

            Vector2 dir = delta / length;
            Vector2 normal = new Vector2(-dir.Y, dir.X);
            float amp = Math.Min(length * 0.18f, 0.34f * width * 4f);

            const int Segments = 6;
            Vector2[] pts = new Vector2[Segments + 1];
            for (int s = 0; s <= Segments; s++)
            {
                float f = s / (float)Segments;
                float envelope = (float)Math.Sin(f * Math.PI);
                float jitter = (Hash01(seed, flick, s) - 0.5f) * 2f * amp * envelope;
                pts[s] = start + dir * (length * f) + normal * jitter;
            }

            for (int s = 0; s < Segments; s++)
            {
                Vector2 a = pts[s];
                Vector2 b = pts[s + 1];
                Vector2 mid = (a + b) * 0.5f;
                Vector2 seg = b - a;
                float segLen = seg.Length();
                if (segLen < 0.5f) continue;
                float rot = (float)Math.Atan2(seg.Y, seg.X);

                Capsule(mid, segLen, width * 2.0f, rot, halo);
                Capsule(mid, segLen, width * 0.8f, rot, core);

                // RAMAS laterales donde el hash lo pide (la referencia las
                // muestra bifurcando hacia abajo).
                if (s > 0 && s < Segments - 1 && Hash01(seed, flick, s + 91) > 0.55f)
                {
                    float side = Hash01(seed, flick, s + 37) > 0.5f ? 1f : -1f;
                    float branchLen = (0.35f + 0.4f * Hash01(seed, flick, s + 53)) * length * 0.28f;
                    Vector2 branchDir = (dir * 0.45f + normal * side).SafeNormalize(Vector2.UnitY);
                    Vector2 bEnd = b + branchDir * branchLen;
                    Vector2 bMid = (b + bEnd) * 0.5f;
                    float bRot = (float)Math.Atan2(branchDir.Y, branchDir.X);
                    Capsule(bMid, branchLen, width * 1.3f, bRot, halo * 0.6f);
                    Capsule(bMid, branchLen, width * 0.5f, bRot, core * 0.6f);
                }
            }

            // Extremos incandescentes.
            Quad(Glow, start, new Vector2(width * 5f, width * 5f), 0f, halo);
            Quad(Glow, start, new Vector2(width * 2.6f, width * 2.6f), 0f, core);
            Quad(Glow, end, new Vector2(width * 4f, width * 4f), 0f, halo);
        }

        // ------------------------------------------------------------------
        //  8. EL CÍRCULO DE RUNAS — dorado, ENORME y CON HUECOS
        //     v6.37 — delega en OrbitaLib (la librería de los anillos
        //     rúnicos): ni un número cambiado. El alfabeto U vive AHORA
        //     en OrbitaLib.RunasAbismo (el default de la librería).
        // ------------------------------------------------------------------

        /// <summary>
        /// v6.37 — delega en OrbitaLib (la librería de los anillos
        /// rúnicos): ni un número cambiado. EL SIGILO EROSIONADO
        /// completo — el aro ROTO con huecos por hash (940+s, 3), los
        /// glifos PERDIDOS (960+g, 5) y APAGADOS donde la PÚA cruza, la
        /// brasa de cada tramo y las perlas erosionadas — vive AHORA en
        /// la primitiva `OrbitaLib.SigiloErosionado`. El alfabeto U (la
        /// tabla `_runes` de siempre, idéntica byte a byte) es el
        /// default de la librería: OrbitaLib.RunasAbismo.
        /// </summary>
        private static void DrawRuneCircle(Vector2 center, float r, float time, int seed)
        {
            OrbitaLib.SigiloErosionado(center, r, time, seed,
                RuneRadius, RuneCount, RuneOrbit,
                Math.Max(r / 52f, 0.25f) * 1.75f,
                RuneGold, RuneTip,
                offset: 0, hashOff: 0,
                spikeAngle: SpikeAngle + RingTilt, spikeWindow: 0.45f);
        }

        // ------------------------------------------------------------------
        //  9. BRASAS CON ESTELAS — las chispas de la referencia
        // ------------------------------------------------------------------

        private static void DrawEmbers(Vector2 center, float r, float time, int seed)
        {
            for (int i = 0; i < 12; i++)
            {
                float h = Hash01(seed, 980 + i, 23);
                float life = (time * 0.16f + h) % 1f;
                float ang = Hash01(seed, 981 + i, 31) * MathHelper.TwoPi +
                            time * 0.05f * (i % 2 == 0 ? 1f : -1f);

                // La mitad cae HACIA el centro (devorada) y la mitad vuela
                // HACIA afuera: "pulled into the center or flying outward".
                bool inward = i % 2 == 0;
                float distNear = inward ? 2.9f - life * 1.6f : 1.6f + life * 2.2f;
                float dist = distNear * r;
                Vector2 pos = center + new Vector2(
                    (float)Math.Cos(ang) * dist, (float)Math.Sin(ang) * dist);

                // Tamaño y opacidad variables (realismo de brasa).
                float size = (0.10f + 0.12f * h) * r * 2f;
                float alpha = (float)Math.Sin(life * Math.PI) * (0.55f + 0.45f * h);

                // Naranja-amarillo / roja / blanca (la paleta de la referencia).
                Color c = h < 0.45f ? EmberYellow
                        : h < 0.85f ? EmberRed
                        : EmberWhite;

                // LA ESTELA: cápsula apuntando CONTRA el movimiento (la
                // brasa en vuelo deja rastro — las chispas de la referencia).
                Vector2 radial = pos - center;
                if (radial.LengthSquared() > 1f)
                {
                    Vector2 dir = radial; dir.Normalize();
                    Vector2 motion = inward ? -dir : dir;
                    Vector2 tail = pos - motion * (0.45f + 0.55f * h) * r;
                    Vector2 tmid = (pos + tail) * 0.5f;
                    float tlen = (pos - tail).Length();
                    float trot = (float)Math.Atan2(motion.Y, motion.X);
                    Capsule(tmid, tlen, 0.09f * r, trot, Tint(c, 0.40f * alpha));
                }

                Quad(Glow, pos, new Vector2(size, size), 0f, Tint(c, alpha));
            }
        }

        // ------------------------------------------------------------------
        //  10. ONDAS DE DISTORSIÓN — el espacio-tiempo late
        // ------------------------------------------------------------------

        private static void DrawDistortionWaves(Vector2 center, float r, float time, int seed)
        {
            for (int w = 0; w < WaveCount; w++)
            {
                float phase = ((time / WaveCycle) + w / (float)WaveCount) % 1f;
                float radius = (1.15f + phase * 1.95f) * r;
                float fade = (1f - phase) * (1f - phase);
                RingQuad(center, radius, phase * 2.4f + w,
                    Tint(new Color(255, 205, 210), 0.28f * fade));
            }
        }
    }
}
