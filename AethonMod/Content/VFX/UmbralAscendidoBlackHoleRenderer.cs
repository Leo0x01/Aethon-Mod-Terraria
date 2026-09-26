using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// UmbralAscendidoBlackHoleRenderer — v6.23 — LA COPIA MEJORADA DEL
    /// UMBRAL: "EL ASCENDIDO".
    ///
    /// v6.23 — EL SEGUNDO ANILLO RÚNICO BIEN VISIBLE (petición del
    /// usuario): radio 1.66→1.95·R y glifos del 62%→80% del tamaño — la
    /// firma contrarrotante del Ascendido ahora se lee CLARA junto al
    /// círculo dorado principal.
    ///
    /// El Umbral original (UmbralBlackHoleRenderer) queda INTACTO; este es
    /// un archivo NUEVO nacido de él — misma geometría medida, misma
    /// paleta carmesí/naranja DOPPLER, mismo contrato de batch — con CINCO
    /// MEJORAS SUSTANCIALES sobre la LIBRERÍA DE RAYOS StormLib:
    ///
    ///   1. ⚡ LLUVIA DE RAYOS NARANJAS — 4 rayos StormLib.Bolt CAYENDO
    ///      alrededor del agujero, naciendo en el círculo de runas y
    ///      cayendo hacia afuera-abajo, con parpadeo vivo a ~8 Hz. La doble
    ///      tira funda+núcleo con RAMAS heredadas sustituye al rayo simple
    ///      del original.
    ///
    ///   2. ⚡ ARCO DORADO — un StormLib.Arc eléctrico parcial (~90°)
    ///      alrededor del horizonte, dorado, GIRANDO con el tiempo, con un
    ///      segundo filo más fino desfasado.
    ///
    ///   3. DOPPLER MÁS VIVO — contraste EXTREMO: el lado que se acerca es
    ///      BLANCO-INCANDESCENTE y GRUESO (curva de beaming elevada a la
    ///      5ª potencia + anchuras amplificadas), el lado lejano es ROJO
    ///      PROFUNDO y fino.
    ///
    ///   4. DOBLE CÍRCULO DE RUNAS — el anillo dorado original + un SEGUNDO
    ///      anillo de runas CONTRARROTANDO (más rápido, al revés): la firma
    ///      del sello elevado. v6.23: el segundo anillo AHORA BIEN VISIBLE
    ///      (radio 1.66→1.95·R, glifos al 80% del tamaño).
    ///
    ///   5. BRASAS AMPLIFICADAS — más partículas (12→20) con ESTELAS MÁS
    ///      LARGAS y brasas doradas rúnicas en la mezcla.
    ///
    /// CONTRATO DE BATCH (v6.10, IDÉNTICO al original): Draw() exige el
    /// SpriteBatch CERRADO y lo deja CERRADO.
    /// </summary>
    public static class UmbralAscendidoBlackHoleRenderer
    {
        // ==================================================================
        //  PARÁMETROS — calibrados contra la referencia (copia del original)
        // ==================================================================

        /// <summary>Radio de la esfera negra en px a escala 1 — IGUAL al original.</summary>
        public const float SpherePx = 46f;

        // ==================================================================
        //  LA GEOMETRÍA MEDIDA — la topología "∞" de la referencia (copia)
        // ==================================================================

        /// <summary>EL DISCO FINO — la línea delantera cruza a ~1.4R bajo la esfera.</summary>
        private const float RingA = 2.60f;      // semieje mayor (×R)
        private const float RingB = 1.40f;      // semieje menor (×R)
        private const float RingTilt = -0.10f;  // rad — inclinación leve

        /// <summary>EL ALA BARRIDA — banda circular en el frente.</summary>
        private const float WingRadius = 2.45f;     // ×R — radio de la banda del ala
        private const float WingSquash = 0.92f;     // achatado vertical leve

        /// <summary>EL ARCO DE LENTE superior (el lado lejano doblado) — DOBLE.</summary>
        private const float TopArcRadius = 1.45f;   // ×R — banda externa
        private const float TopArcInner = 1.22f;    // ×R — el segundo anillo de fotones

        /// <summary>EL ARCO INFERIOR magenta (la imagen lenseda de abajo).</summary>
        private const float BotArcRadius = 1.28f;   // ×R

        /// <summary>Ángulo del BEAMING DOPPLER (medido en el original).</summary>
        private const float DopplerAngle = 3.05f;

        /// <summary>Centro de la CUÑA OSCURA medida.</summary>
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

        // --- el círculo de runas (CON HUECOS) — anillo EXTERIOR original ---
        private const int RuneCount = 12;
        private const float RuneRadius = 2.55f;   // ×R — a la altura del anillo
        private const float RuneOrbit = 0.06f;    // rad/s — gira MUY lento

        // --- ASCENDIDO: el SEGUNDO anillo de runas, CONTRARROTANDO ---
        //     (v6.23: radio 1.66→1.95·R y glifos al 80% — BIEN VISIBLE) ---
        private const int RuneCount2 = 8;         // menos glifos: anillo íntimo
        private const float RuneRadius2 = 1.95f;  // ×R — sube: se lee CLARO (v6.23)
        private const float RuneOrbit2 = -0.21f;  // rad/s — CONTRARROTACIÓN

        // --- ASCENDIDO: LA LLUVIA DE RAYOS (StormLib) ---
        private const int BoltRainCount = 4;      // rayos cayendo alrededor
        private const float BoltRainHz = 8f;      // regeneración nerviosa (~8 Hz)
        private const float BoltRainLen = 1.60f;  // ×R — largo de cada rayo

        // --- ASCENDIDO: EL ARCO DORADO giratorio (StormLib.Arc) ---
        private const float GoldArcHz = 9f;       // regeneración del arco
        private const float GoldArcRadius = 1.38f; // ×R — abraza el horizonte
        private const float GoldArcSpin = 0.85f;  // rad/s — gira con el tiempo

        // --- ondas de distorsión ---
        private const float WaveCycle = 2.8f;
        private const int WaveCount = 2;

        // --- ASCENDIDO: brasas amplificadas ---
        private const int EmberCount = 20;        // (el original pintaba 12)

        // ==================================================================
        //  PALETA — los RGB medidos (copia) + los tonos del ASCENDIDO
        // ==================================================================

        private static readonly Color HotInner = new(250, 210, 220);   // núcleo blanco-rosado (MEDIDO)
        private static readonly Color HotRose = new(243, 128, 149);     // rosa caliente (MEDIDO)
        private static readonly Color ArcSalmon = new(248, 110, 95);    // salmón del arco superior (MEDIDO)
        private static readonly Color WingMagenta = new(240, 41, 168);  // magenta del ala/arco inferior (MEDIDO)
        private static readonly Color MidRose = new(225, 74, 127);      // rosa (MEDIDO)
        private static readonly Color Rose = new(183, 29, 83);          // carmesí-rosa (MEDIDO)
        private static readonly Color DeepRose = new(153, 14, 76);      // magenta profundo (MEDIDO)
        private static readonly Color WineFade = new(110, 17, 51);      // vino exterior (MEDIDO)
        private static readonly Color MagentaViolet = new(130, 25, 145);// violeta secundario
        private static readonly Color SpikeWhite = new(250, 200, 225);  // blanco-rosa de la púa
        private static readonly Color RuneGold = new(240, 124, 65);     // dorado-ámbar (MEDIDO)
        private static readonly Color RuneTip = new(255, 205, 140);     // punta pálida
        private static readonly Color WispPink = new(200, 20, 120);     // velo magenta
        private static readonly Color WispCrimson = new(160, 10, 50);   // velo carmesí
        private static readonly Color EmberYellow = new(255, 180, 50);  // brasa naranja-amarilla
        private static readonly Color EmberRed = new(200, 40, 40);      // brasa roja
        private static readonly Color EmberWhite = new(255, 255, 240);  // brasa blanca

        // --- ASCENDIDO: los tonos nuevos de la elevación ---
        private static readonly Color Incandescent = new(255, 252, 246); // BLANCO-INCANDESCENTE (lado que se acerca)
        private static readonly Color FarDeepRed = new(120, 12, 40);     // rojo PROFUNDO (lado que se aleja)
        private static readonly Color BoltRainHalo = new(205, 60, 0);    // funda de la lluvia de rayos
        private static readonly Color BoltRainCore = new(255, 205, 130); // núcleo cálido del rayo
        private static readonly Color GoldArcHalo = new(255, 185, 70);   // funda del arco dorado
        private static readonly Color GoldArcCore = new(255, 246, 208);  // núcleo dorado pálido

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
        /// EL BEAMING DOPPLER — VERSIÓN ASCENDIDO: la curva sube a la 5ª
        /// potencia. El máximo sigue en 1 pero TODO lo demás cae más rápido:
        /// el sector que se acerca queda BLANCO-INCANDESCENTE y el resto se
        /// hunde en el rojo profundo — el contraste extremo del elevado.
        /// </summary>
        private static float Doppler(float t)
        {
            float d = 0.5f + 0.5f * (float)Math.Cos(t - DopplerAngle);
            return d * d * d * d * d;
        }

        /// <summary>
        /// LA CUÑA OSCURA medida (copia exacta): el sector apagado donde la
        /// sombra del agujero muerde el anillo.
        /// </summary>
        private static float Wedge(float t)
        {
            float d = Math.Abs(MathHelper.WrapAngle(t - WedgeAngle));
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
                DrawLensingArcTop(center, r, time);

                // --- 3b. DISCO FINO — mitad TRASERA ---
                DrawAccretionDisk(center, rr, time, seed, flick, front: false);

                // --- 4. NÚCLEO ---
                Main.spriteBatch.End();
                BeginAlpha();
                Quad(Disk, center, new Vector2(2.28f * r, 2.28f * r), 0f, Color.White);
                Main.spriteBatch.End();
                BeginAdditive();
                // Filo púrpura del horizonte (la última luz doblada).
                RingQuad(center, 1.02f * r, time * 0.12f,
                    Tint(MagentaViolet, 0.34f + 0.10f * (float)Math.Sin(time * 1.5f)));

                // --- 5. DISCO FINO — mitad DELANTERA: la línea CALIENTE ---
                DrawAccretionDisk(center, rr, time, seed, flick, front: true);

                // --- 5b. EL ALA BARRIDA (donde el Doppler ARDE) ---
                DrawWing(center, rr, time, seed, flick);

                // --- 5b'. EL VACÍO VUELVE A DEVORAR el derrame del ala ---
                Main.spriteBatch.End();
                BeginAlpha();
                Quad(Disk, center, new Vector2(2.28f * r, 2.28f * r), 0f, Color.White);
                Main.spriteBatch.End();
                BeginAdditive();
                RingQuad(center, 1.02f * r, time * 0.12f,
                    Tint(MagentaViolet, 0.30f + 0.10f * (float)Math.Sin(time * 1.5f)));

                // --- 5c. EL ARCO INFERIOR magenta (la imagen lenseda de abajo) ---
                DrawLensingArcBottom(center, r, time);

                // --- 6. LA PÚA DE ENERGÍA (lado derecho) ---
                DrawEnergySpike(center, rr, time, seed);

                // --- 7. EL DOBLE CÍRCULO DE RUNAS (original + interior
                //      contrarrotante) — primero las runas, para que la
                //      LLUVIA DE RAYOS nazca ENCIMA de ellas. ---
                DrawRuneCircle(center, r, time, seed);

                // --- 8. ⚡ LA LLUVIA DE RAYOS NARANJAS (StormLib:
                //      doble tira + RAMAS, anclada al círculo de runas) ---
                DrawLightningRain(center, r, time, seed);

                // --- 9. BRASAS CON ESTELAS (amplificadas) ---
                DrawEmbers(center, r, time, seed);

                // --- 10. ONDAS DE DISTORSIÓN ---
                DrawDistortionWaves(center, r, time, seed);

                // --- 11. AURA FINAL carmesí pulsante (en ANILLO, no sobre
                //      el vacío: el centro debe quedar NEGRO ABSOLUTO) ---
                float aura = 0.85f + 0.15f * (float)Math.Sin(time * 1.8f);
                RingQuad(center, 2.6f * rr, time * 0.1f,
                    Tint(new Color(200, 30, 90), 0.16f * aura));

                // --- 12. EL VACÍO FINAL: última devoración ---
                Main.spriteBatch.End();
                BeginAlpha();
                Quad(Disk, center, new Vector2(2.28f * r, 2.28f * r), 0f, Color.White);
                Main.spriteBatch.End();
                BeginAdditive();
                // El filo del horizonte (la última luz doblada) respira.
                RingQuad(center, 1.02f * r, time * 0.12f,
                    Tint(MagentaViolet, 0.26f + 0.08f * (float)Math.Sin(time * 1.5f)));

                // --- 13. ⚡ EL ARCO DORADO GIRATORIO (StormLib.Arc) —
                //      la CAPA FINAL: nada vuelve a devorarlo. Abraza el
                //      horizonte a 1.38·R girando con el tiempo. ---
                DrawGoldenArc(center, r, time, seed);

                Main.spriteBatch.End();
                // El batch queda CERRADO (contrato).
            }
            catch
            {
                // Cierre defensivo SOLO en el path de error (v6.10).
                VFXCore.CerrarLoteSiAbierto();
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
                float dist = (1.6f + 3.4f * h * h) * rr * 0.62f;
                Vector2 pos = center + new Vector2(
                    (float)Math.Cos(ang) * dist, (float)Math.Sin(ang) * dist);
                float pulse = 0.5f + 0.5f * (float)Math.Sin(time * 1.2f + i * 2.1f);

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
            for (int w = 0; w < 2; w++)
            {
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

                    float swirl = time * 0.10f * (w == 0 ? 1f : -1f) + h1 * MathHelper.TwoPi;
                    float offR = (0.4f + 0.75f * h2) * rr;
                    Vector2 pos = anchor + new Vector2(
                        (float)Math.Cos(swirl) * offR,
                        (float)Math.Sin(swirl) * offR * 0.7f);

                    float size = (1.1f + 0.9f * h3) * rr;
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
        /// EL ALA BARRIDA — VERSIÓN ASCENDIDO: el contraste se EXAGERÓ —
        /// la banda se engorda hasta +0.58·R donde el Doppler ARDE y adelgaza
        /// a 0.42·R en el lado que se aleja; la estela interior ya no es rosa:
        /// es BLANCO-INCANDESCENTE puro.
        /// </summary>
        private static void DrawWing(Vector2 center, float rr, float time,
            int seed, int flick)
        {
            const int Segments = 18;
            float a0 = 0.60f, a1 = MathHelper.Pi + 0.05f;

            for (int s = 0; s < Segments; s++)
            {
                float t = a0 + (s + 0.5f) / Segments * (a1 - a0);
                float dop = Doppler(t);
                float wedge = Wedge(t);

                Vector2 dir = new Vector2((float)Math.Cos(t), (float)Math.Sin(t) * WingSquash);
                Vector2 pos = center + dir * WingRadius * rr;

                Vector2 tang = new Vector2(-(float)Math.Sin(t), (float)Math.Cos(t) * WingSquash);
                float rot = (float)Math.Atan2(tang.Y, tang.X);
                float len = (a1 - a0) * WingRadius * rr / Segments * 1.35f;

                // LA BANDA: ASCENDIDO — gruesa donde arde, FINA en el lado frío.
                float w = (0.42f + 0.58f * dop) * rr;

                Color c = dop > 0.40f ? HotRose : WingMagenta;

                float turb = 0.70f + 0.30f * Hash01(seed, 752 + s, flick);
                float a = (0.42f + 0.55f * dop) * turb * wedge;

                Capsule(pos, len, w * 1.9f, rot, Tint(c, a));
                Capsule(pos, len, w * 0.9f, rot, Tint(c, a * 0.8f));

                // Núcleo interior BLANCO-INCANDESCENTE donde el Doppler ARDE
                // (la estela del ala del Ascendido: luz de fusión).
                if (dop > 0.40f)
                {
                    Color hot = Color.Lerp(HotRose, Incandescent,
                        MathHelper.Clamp((dop - 0.40f) / 0.60f, 0f, 1f));
                    Capsule(pos, len, w * 0.48f, rot,
                        Tint(hot, 0.62f * Math.Max(dop, 0.3f) * turb));
                }
            }
        }

        /// <summary>
        /// EL ARCO DE LENTE SUPERIOR (copia con contraste amplificado): la
        /// banda engorda hasta +0.34·R en el sector caliente y el filo corre
        /// de rosa a BLANCO puro.
        /// </summary>
        private static void DrawLensingArcTop(Vector2 center, float r, float time)
        {
            const int Segments = 22;
            float a0 = MathHelper.Pi + 0.35f, a1 = MathHelper.TwoPi - 0.35f;

            for (int s = 0; s < Segments; s++)
            {
                float t = a0 + (s + 0.5f) / Segments * (a1 - a0);
                float dop = Doppler(t);

                Vector2 dir = new Vector2((float)Math.Cos(t), (float)Math.Sin(t));
                Vector2 pos = center + dir * TopArcRadius * r;

                Vector2 tang = new Vector2(-dir.Y, dir.X);
                float rot = (float)Math.Atan2(tang.Y, tang.X);
                float len = (a1 - a0) * TopArcRadius * r / Segments * 1.35f;

                Color cGlow = Color.Lerp(ArcSalmon, HotRose, dop);
                float pulse = 0.80f + 0.20f * (float)Math.Sin(time * 2.0f + s * 0.7f);
                Capsule(pos, len, (0.24f + 0.34f * dop) * r, rot,
                    Tint(cGlow, (0.42f + 0.30f * dop) * pulse));

                // EL SEGUNDO ANILLO DE FOTONES (más apretado, tenue).
                Vector2 pos2 = center + dir * TopArcInner * r;
                Capsule(pos2, len * 0.9f, (0.05f + 0.04f * dop) * r, rot,
                    Tint(Color.Lerp(ArcSalmon, HotRose, dop),
                        (0.30f + 0.30f * dop) * pulse));

                // EL FILO: la línea fina INCANDESCENTE — rosa → BLANCO puro.
                Color cEdge = Color.Lerp(HotRose, Incandescent, dop);
                Capsule(pos, len, (0.05f + 0.06f * dop) * r, rot,
                    Tint(cEdge, (0.80f + 0.20f * dop) * pulse));
            }
        }

        /// <summary>
        /// EL ARCO INFERIOR magenta (copia con asimetría amplificada).
        /// </summary>
        private static void DrawLensingArcBottom(Vector2 center, float r, float time)
        {
            const int Segments = 12;
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

                Color c = Color.Lerp(WingMagenta, MidRose, dop * 0.7f);
                float pulse = 0.80f + 0.20f * (float)Math.Sin(time * 1.7f + s * 0.9f);
                Capsule(pos, len, (0.07f + 0.16f * dop) * r, rot,
                    Tint(c, (0.44f + 0.40f * dop) * pulse));
            }
        }

        private static void DrawAccretionDisk(Vector2 center, float rr, float time, int seed,
            int flick, bool front)
        {
            float t0 = front ? 0f : MathHelper.Pi;
            float span = MathHelper.Pi;
            float bright = front ? 1.25f : 0.42f;

            float flow = time * StreakFlow;

            for (int s = 0; s < StreaksPerHalf; s++)
            {
                float h0 = Hash01(seed, 700 + s, flick / 2);
                float a0 = t0 + ((h0 + flow / MathHelper.TwoPi) % 1f) * span;

                float radialJit = 0.96f + 0.12f * Hash01(seed, 703 + s, 5);

                float midT = a0 + 0.10f;
                float dop = Doppler(midT);

                float arcLen = (0.22f + 0.50f * Hash01(seed, 701 + s, flick / 2)) *
                               (0.45f + 0.75f * dop);

                // GROSOR ASCENDIDO: el disco es FINÍSIMO en el lado lejano
                // (0.09·R) y GORDO donde el Doppler ARDE (hasta 0.39·R).
                float wBase = (0.09f + 0.30f * dop) * rr;

                float turb = 0.70f + 0.30f * Hash01(seed, 702 + s, flick);
                float wedge = Wedge(a0 + arcLen * 0.5f);

                // --- BANDA INTERNA CALIENTE (borde del horizonte) ---
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

                    // Color del NÚCLEO — ASCENDIDO: el lado que se acerca
                    // ARDE a BLANCO-INCANDESCENTE; el lejano se hunde al
                    // rojo profundo.
                    float inten = (0.22f + 0.85f * dop) * turb * bright * wedge;
                    Color core;
                    if (dop > 0.70f) core = Incandescent;
                    else if (dop > 0.40f) core = HotInner;
                    else if (dop > 0.18f) core = HotRose;
                    else if (dop > 0.05f) core = Rose;
                    else core = FarDeepRed;

                    Capsule(mid, len, wBase * 2.1f, rot, Tint(Color.Lerp(core, MidRose, 0.25f), 0.55f * inten));
                    Capsule(mid, len, wBase, rot, Tint(core, 1.0f * inten));

                    // Punto BLANCO puro: donde la turbulencia ARDE.
                    if (turb > 0.82f && inten > 0.28f)
                    {
                        Quad(Glow, mid, new Vector2(0.36f * rr, 0.36f * rr), rot,
                            Tint(Incandescent, 0.95f * inten * (turb - 0.82f) / 0.18f));
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

                // --- BANDA EXTERNA: rojo profundo desvaneciéndose (a 1.22×) ---
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
                            Tint(FarDeepRed, 0.45f * inten));
                    }
                }
            }
        }

        // ------------------------------------------------------------------
        //  6. LA PÚA DE ENERGÍA — el chorro blanco-rosa del lado derecho
        // ------------------------------------------------------------------

        private static void DrawEnergySpike(Vector2 center, float rr, float time, int seed)
        {
            Vector2 basePos = Ellipse(center, rr, SpikeAngle);
            Vector2 outward = basePos - center;
            if (outward.LengthSquared() < 0.01f) return;
            outward.Normalize();
            Vector2 up = new Vector2(0f, -0.34f);
            Vector2 dir = (outward + up).SafeNormalize(Vector2.UnitX);

            float pulse = 0.80f + 0.20f * (float)Math.Sin(time * 2.6f);
            float len = SpikeLen * rr * pulse;

            Quad(Glow, basePos, new Vector2(1.5f * rr, 1.5f * rr), 0f,
                Tint(SpikeWhite, 0.45f * pulse));
            Quad(Glow, basePos, new Vector2(0.7f * rr, 0.7f * rr), 0f,
                Tint(new Color(255, 250, 245), 0.85f * pulse));

            float rot = (float)Math.Atan2(dir.Y, dir.X);
            for (int k = 0; k < 4; k++)
            {
                float f0 = k / 4f, f1 = (k + 1) / 4f;
                float midF = (f0 + f1) * 0.5f;
                Vector2 a = basePos + dir * (f0 * len);
                Vector2 b = basePos + dir * (f1 * len);
                Vector2 mid = (a + b) * 0.5f;
                float segLen = (b - a).Length();

                float w = (0.34f - 0.26f * midF) * rr;
                float fade = (1f - midF * 0.65f) * pulse;
                Color c = Color.Lerp(SpikeWhite, MidRose, midF * 0.7f);
                Capsule(mid, segLen, w * 2.0f, rot, Tint(c, 0.22f * fade));
                Capsule(mid, segLen, w, rot, Tint(c, 0.55f * fade));
            }

            Vector2 tip = basePos + dir * len;
            Quad(Glow, tip, new Vector2(0.75f * rr, 0.75f * rr), 0f,
                Tint(SpikeWhite, 0.40f * pulse));
            Quad(Glow, tip, new Vector2(2.1f * rr, 0.16f * rr), rot,
                Tint(SpikeWhite, 0.35f * pulse));
            Quad(Glow, tip, new Vector2(0.16f * rr, 1.3f * rr), rot,
                Tint(SpikeWhite, 0.35f * pulse));
        }

        // ------------------------------------------------------------------
        //  7. ⚡ LA LLUVIA DE RAYOS NARANJAS — StormLib.Bolt ×4
        //      (doble tira cuerpo+núcleo, RAMAS heredadas, gorros de
        //      descarga) naciendo en el CÍRCULO DE RUNAS y CAYENDO hacia
        //      afuera-abajo. SUSTITUYE al rayo simple del original.
        // ------------------------------------------------------------------

        private static void DrawLightningRain(Vector2 center, float r, float time, int seed)
        {
            int lflick = StormLib.FlickTick(time, BoltRainHz);

            for (int i = 0; i < BoltRainCount; i++)
            {
                int bseed = seed + 310 + i * 97;
                // El parpadeo nervioso: cada rayo se APAGA a veces.
                if (!StormLib.IsLit(bseed, lflick, 0.80f)) continue;

                // El ancla VIVE sobre el círculo de runas (gira despacio,
                // distinto por rayo) y el rayo CAE hacia afuera-abajo.
                float baseAng = i / (float)BoltRainCount * MathHelper.TwoPi +
                                time * 0.10f * (i % 2 == 0 ? 1f : -1f) +
                                0.55f * Hash01(seed, 330 + i, 3);
                Vector2 start = center + new Vector2(
                    (float)Math.Cos(baseAng) * RuneRadius * r,
                    (float)Math.Sin(baseAng) * RuneRadius * r);
                Vector2 outward = start - center;
                if (outward.LengthSquared() < 0.01f) continue;
                outward.Normalize();
                Vector2 down = new Vector2(0.16f, 0.62f);
                Vector2 dir = (outward + down).SafeNormalize(Vector2.UnitY);

                float len = BoltRainLen * r * (0.80f + 0.55f * Hash01(seed, 340 + i, lflick));
                Vector2 end = start + dir * len;

                // LA DOBLE TIRA DE StormLib (funda + núcleo + ramas).
                float w = Math.Max(r * 0.085f, 2.2f);
                StormLib.Bolt(Main.spriteBatch, start, end, bseed, lflick,
                    w, Tint(BoltRainHalo, 0.55f), Tint(BoltRainCore, 0.95f),
                    alpha: 1f, segments: 7, amp: r * 0.16f);

                // Núcleo CASI BLANCO en la base del rayo (el plasma fresco).
                Quad(Glow, start, new Vector2(0.55f * r, 0.55f * r), 0f,
                    Tint(new Color(255, 225, 175), 0.60f));
            }
        }

        // ------------------------------------------------------------------
        //  8. ⚡ EL ARCO DORADO GIRATORIO — StormLib.Arc de ~90°
        //      alrededor del horizonte, con un segundo filo desfasado.
        // ------------------------------------------------------------------

        private static void DrawGoldenArc(Vector2 center, float r, float time, int seed)
        {
            int gflick = StormLib.FlickTick(time, GoldArcHz);
            if (!StormLib.IsLit(seed + 777, gflick, 0.90f)) return;

            // El arco recorre ~90° del horizonte y GIRA con el tiempo.
            // (v2 del calibrado: ancho 0.10·R y halo 0.62 — calibrado con
            // el mock VLM para que la firma dorada SE LEA sobre el disco.)
            float a0 = time * GoldArcSpin;
            float w = Math.Max(r * 0.10f, 2.6f);

            StormLib.ArcRing(Main.spriteBatch, center, GoldArcRadius * r,
                a0, a0 + MathHelper.PiOver2, seed + 777, gflick, w,
                Tint(GoldArcHalo, 0.62f), Tint(GoldArcCore, 0.95f),
                alpha: 1f, count: 9);

            // EL SEGUNDO FILO: más fino, más afuera, desfasado (corona doble).
            if (StormLib.IsLit(seed + 778, gflick, 0.70f))
            {
                StormLib.ArcRing(Main.spriteBatch, center, GoldArcRadius * 1.12f * r,
                    a0 + 0.35f, a0 + 0.35f + MathHelper.PiOver2 * 0.8f,
                    seed + 778, gflick, w * 0.62f,
                    Tint(GoldArcHalo, 0.42f), Tint(GoldArcCore, 0.80f),
                    alpha: 1f, count: 8);
            }
        }

        // ------------------------------------------------------------------
        //  9. EL DOBLE CÍRCULO DE RUNAS — doradas, ENORMES y CON HUECOS
        //     (el original) + el anillo íntimo CONTRARROTANTE (Ascendido)
        //     v6.37 — delega en OrbitaLib (la librería de los anillos
        //     rúnicos): ni un número cambiado. El alfabeto U vive AHORA
        //     en OrbitaLib.RunasAbismo (el default de la librería).
        // ------------------------------------------------------------------

        /// <summary>
        /// v6.37 — delega en OrbitaLib (la librería de los anillos
        /// rúnicos): ni un número cambiado. Cada anillo es UNA llamada a
        /// `OrbitaLib.SigiloErosionado` con SUS constantes de siempre:
        /// el aro ROTO con huecos por hash, los glifos PERDIDOS y
        /// APAGADOS por la PÚA, la brasa por tramo y las perlas
        /// erosionadas viven AHORA en la primitiva. El alfabeto U (la
        /// tabla `_runes` de siempre, idéntica byte a byte) es el
        /// default de la librería: OrbitaLib.RunasAbismo.
        /// </summary>
        private static void DrawRuneCircle(Vector2 center, float r, float time, int seed)
        {
            float glyphScale = Math.Max(r / 52f, 0.25f) * 1.75f;

            // EL ANILLO EXTERIOR — el círculo de runas original, intacto.
            OrbitaLib.SigiloErosionado(center, r, time, seed,
                RuneRadius, RuneCount, RuneOrbit, glyphScale * 1f,
                RuneGold, RuneTip,
                offset: 0, hashOff: 0,
                spikeAngle: SpikeAngle + RingTilt, spikeWindow: 0.45f);

            // EL SEGUNDO ANILLO — CONTRARROTANDO: la firma del Ascendido
            // (gira al revés y 3.5× más rápido que el exterior; seed+5000
            // y hashOff/offset 500 para que hueque y lea DISTINTO). v6.23:
            // glifos al 80% (era 62%) — la firma se lee CLARA, no susurrada.
            OrbitaLib.SigiloErosionado(center, r, time, seed + 5000,
                RuneRadius2, RuneCount2, RuneOrbit2, glyphScale * 0.80f,
                RuneGold, RuneTip,
                offset: 500, hashOff: 500,
                spikeAngle: SpikeAngle + RingTilt, spikeWindow: 0.30f);
        }

        // ------------------------------------------------------------------
        //  10. BRASAS CON ESTELAS — amplificadas (20, estelas largas)
        // ------------------------------------------------------------------

        private static void DrawEmbers(Vector2 center, float r, float time, int seed)
        {
            for (int i = 0; i < EmberCount; i++)
            {
                float h = Hash01(seed, 980 + i, 23);
                float life = (time * 0.16f + h) % 1f;
                float ang = Hash01(seed, 981 + i, 31) * MathHelper.TwoPi +
                            time * 0.05f * (i % 2 == 0 ? 1f : -1f);

                // La mitad cae HACIA el centro (devorada) y la mitad vuela
                // HACIA afuera.
                bool inward = i % 2 == 0;
                float distNear = inward ? 2.9f - life * 1.6f : 1.6f + life * 2.4f;
                float dist = distNear * r;
                Vector2 pos = center + new Vector2(
                    (float)Math.Cos(ang) * dist, (float)Math.Sin(ang) * dist);

                float size = (0.10f + 0.12f * h) * r * 2f;
                float alpha = (float)Math.Sin(life * Math.PI) * (0.55f + 0.45f * h);

                // Naranja-amarillo / roja / blanca / DORADA rúnica (Ascendido).
                Color c = h < 0.40f ? EmberYellow
                        : h < 0.78f ? EmberRed
                        : h < 0.92f ? EmberWhite
                        : RuneGold;

                // LA ESTELA — ASCENDIDO: MÁS LARGA (hasta 1.7·R de rastro):
                // la brasa del elevado corta el vacío como una aguja.
                Vector2 radial = pos - center;
                if (radial.LengthSquared() > 1f)
                {
                    Vector2 dir = radial; dir.Normalize();
                    Vector2 motion = inward ? -dir : dir;
                    Vector2 tail = pos - motion * (0.85f + 0.85f * h) * r;
                    Vector2 tmid = (pos + tail) * 0.5f;
                    float tlen = (pos - tail).Length();
                    float trot = (float)Math.Atan2(motion.Y, motion.X);
                    Capsule(tmid, tlen, 0.11f * r, trot, Tint(c, 0.42f * alpha));
                }

                Quad(Glow, pos, new Vector2(size, size), 0f, Tint(c, alpha));
            }
        }

        // ------------------------------------------------------------------
        //  11. ONDAS DE DISTORSIÓN — el espacio-tiempo late
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
