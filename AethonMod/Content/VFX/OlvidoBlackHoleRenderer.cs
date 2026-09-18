using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// OlvidoBlackHoleRenderer — v6.15 — EL AGUJERO NEGRO DEL OLVIDO, 100% CÓDIGO.
    ///
    /// DIRECTRIZ DEL USUARIO: "el agujero negro no puede ser creado por
    /// sprite, debe ser creado enteramente por código" — TODO el arte
    /// extraído de la referencia roja anterior fue BORRADO del proyecto
    /// (sprites y pipeline de extracción incluidos).
    ///
    /// NUEVA REFERENCIA (imagen + prompt del usuario, 13/9/2026): agujero
    /// negro cósmico-mágico con núcleo de vacío absoluto que absorbe la
    /// luz, ANILLO ENERGÉTICO púrpura/rosa/magenta con textura de plasma
    /// en movimiento e intensidad variable, RAYOS ELÉCTRICOS rosa/violeta,
    /// PARTÍCULAS LUMINOSAS con movimiento radial, RUNAS DORADAS de estilo
    /// antiguo flotando en círculo, DISTORSIÓN ESPACIAL (ondas que doblan
    /// la luz), fondo oscuro con nebulosas púrpura/azul difusas y aura
    /// mística arcana.
    ///
    /// TODO lo que se ve se COMPONE AQUÍ, CADA FRAME, por código: ~370
    /// cuadros de luz (cápsulas, glows, anillos, zigzags) usando solo TRES
    /// pinceles genéricos GENERADOS POR CÓDIGO por la biblioteca VFX del
    /// mod (SoftGlow = degradé radial, Ring = anillo fino, BlackDisk =
    /// disco negro de borde suave — los mismos pinceles de BoltRenderer,
    /// las coronas y los demás agujeros). CERO sprites de arte. Cero
    /// estado, cero red: todo determinista por hash puro.
    ///
    /// LAS CAPAS (en orden de pintado):
    ///   0. AURA OSCURA MÍSTICA (alfa) — el bolsillo de vacío que oscurece
    ///      el mundo alrededor (el agujero absorbe la luz circundante).
    ///   1. NEBULOSAS púrpura/azul muy difusas girando lento + POLVO
    ///      ambiental carmesí/magenta pulsando (la lejanía viva).
    ///   2. ANILLO DE PLASMA (mitad TRASERA): 44 cápsulas sobre la elipse
    ///      inclinada — hotspot Doppler (zona incandescente) + turbulencia
    ///      por hash regenerada a 12 Hz + gradiente térmico blanco-amarillo
    ///      → rosa → violeta según intensidad ("zonas más brillantes
    ///      intercaladas con sombras").
    ///   3. BRAZOS ESPIRALES del vórtice (2 brazos de cápsulas que se
    ///      desenrocan del anillo hacia afuera, magenta→violeta).
    ///   4. NÚCLEO — disco de NEGRO ABSOLUTO (el vacío) + filo violeta
    ///      tenue en el horizonte (la última luz atrapada).
    ///   5. ANILLO DE PLASMA (mitad DELANTERA, más brillante — el plasma
    ///      CRUZA POR DELANTE de la esfera, como todo agujero que se
    ///      respete).
    ///   6. CORREDORES DE FOTONES — 3 destellos blanco-rosa orbitando.
    ///   7. RAYOS ELÉCTRICOS — 2 VIOLETAS danzando DENTRO del núcleo +
    ///      2 ROSAS emergiendo del anillo hacia afuera (zigzag determinista
    ///      regenerado ~7 Hz: están VIVOS, no es una textura estática).
    ///   8. DESTELLOS POLARES — agujas ahusadas blancas-rosas en los polos
    ///      del vórtice (lens-flare del eje menor).
    ///   9. RUNAS DORADAS — 10 glifos angulares originales orbitando en
    ///      círculo perfecto (cuerpo dorado → punta pálida, latido y
    ///      flotación propios) + anillo rúnico tenue que los une.
    ///  10. ONDAS DE DISTORSIÓN — 2 anillos expandiéndose desde el
    ///      horizonte (el espacio-tiempo LATE; la lente de pantalla la
    ///      curva el BlackHoleLensSystem registrado aparte).
    ///  11. PARTÍCULAS LUMINOSAS — 9 motas derivando RADIALMENTE HACIA
    ///      AFUERA con tamaño/opacidad variables (rosa/violeta/dorada).
    ///  12. AURA MÍSTICA final pulsante (el poder arcano emana).
    ///
    /// CONTRATO DE BATCH (v6.10, a prueba de balas): Draw() exige el
    /// SpriteBatch CERRADO y lo deja CERRADO.
    /// </summary>
    public static class OlvidoBlackHoleRenderer
    {
        // ==================================================================
        //  PARÁMETROS (calibrados contra la NUEVA referencia)
        // ==================================================================

        /// <summary>Radio de la esfera negra en px a escala 1 — GIGANTE.</summary>
        public const float SpherePx = 52f;

        /// <summary>La elipse del anillo de plasma (medida sobre la referencia).</summary>
        private const float RingA = 1.85f;      // semieje mayor (×R)
        private const float RingB = 1.06f;      // semieje menor (×R)
        private const float RingTilt = -0.42f;  // rad — vórtice inclinado

        /// <summary>Deriva del patrón de plasma a lo largo del anillo (rad/s).</summary>
        private const float PlasmaFlow = 0.55f;

        /// <summary>Ángulo base del hotspot (la zona incandescente).</summary>
        private const float HotspotBase = 2.18f; // rad en espacio paramétrico

        /// <summary>Segmentos de cada mitad del anillo.</summary>
        private const int RingSegments = 44;

        /// <summary>Regeneración de la turbulencia (Hz).</summary>
        private const float FlickHz = 12f;

        // --- runas doradas ---
        private const int RuneCount = 10;
        private const float RuneRadius = 2.62f;  // ×R — el círculo rúnico
        private const float RuneOrbit = 0.10f;   // rad/s de rotación del círculo

        // --- ondas de distorsión ---
        private const float WaveCycle = 2.6f;    // s
        private const int WaveCount = 2;

        // ==================================================================
        //  PALETA (VLM sobre la nueva referencia: blanco-amarillo → rosa →
        //  violeta; rayos violeta eléctrico; runas doradas)
        // ==================================================================

        private static readonly Color HotCore = new(235, 240, 255);   // blanco-frío incandescente
        private static readonly Color MidPink = new(130, 80, 255);    // violeta vivo
        private static readonly Color LowViolet = new(80, 105, 255);  // azul-violeta
        private static readonly Color BoltViolet = new(120, 130, 255);// rayo interior morado-azul
        private static readonly Color BoltPink = new(90, 190, 255);   // rayo exterior azul eléctrico
        private static readonly Color RuneGold = new(255, 150, 40);   // cuerpo de runa
        private static readonly Color RuneTip = new(255, 225, 150);   // punta de runa
        private static readonly Color NebPurple = new(70, 40, 200);   // nebulosa morada
        private static readonly Color NebBlue = new(40, 100, 230);    // nebulosa azul
        private static readonly Color AuraViolet = new(70, 80, 220);  // aura mística azulada

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

        /// <summary>
        /// Tinte de INTENSIDAD LINEAL (el patrón validado del Cometa
        /// Estelar): rgb PLENO + alfa = factor. Con Color*f de XNA el
        /// blending aditivo queda cuadrático (f²) y todo se apaga — con
        /// este tinte el brillo es LINEAL en f.
        /// </summary>
        private static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color(c.R, c.G, c.B, (byte)(int)(255f * f));
        }

        /// <summary>Punto sobre la elipse del anillo (param t en rad).</summary>
        private static Vector2 Ellipse(Vector2 c, float r, float t)
        {
            return VFXCore.Ellipse(c, RingA * r, RingB * r, RingTilt, t);
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
                float breathe = 1f + 0.012f * (float)Math.Sin(time * 1.3f);
                float rr = r * breathe;

                // ============ 0. AURA OSCURA MÍSTICA (alfa) ============
                // El vacío ABSORBE la luz: un degradé oscuro violetáceo
                // abraza el mundo alrededor del agujero (bolsillo de vacío).
                BeginAlpha();
                Quad(Glow, center, new Vector2(7.0f * rr, 7.0f * rr), 0f,
                    new Color(6, 4, 22, 170));
                Main.spriteBatch.End();

                // ============ 1..12: TODO LO BRILLANTE (aditivo) ============
                BeginAdditive();

                // --- 1. NEBULOSAS difusas + polvo ambiental ---
                DrawNebulas(center, rr, time, seed);

                // --- 2. ANILLO DE PLASMA — mitad TRASERA ---
                DrawPlasmaRing(center, rr, time, seed, flick, front: false);

                // --- 3. BRAZOS ESPIRALES del vórtice ---
                DrawSpiralArms(center, rr, time, seed);

                // --- 4. NÚCLEO + filo del horizonte ---
                // El disco negro NO es aditivo: se pinta en alfa para
                // DEVORAR lo pintado detrás. Cerramos, pintamos, seguimos.
                Main.spriteBatch.End();
                BeginAlpha();
                // BlackDisk es sólido hasta ~0.88 del semiancho → tamaño
                // 2.28·R da un radio visible de R exacto.
                Quad(Disk, center, new Vector2(2.28f * r, 2.28f * r), 0f, Color.White);
                Main.spriteBatch.End();
                BeginAdditive();

                // Filo violeta del horizonte (la última luz atrapada).
                RingQuad(center, 1.02f * r, time * 0.15f,
                    Tint(BoltViolet, 0.38f + 0.12f * (float)Math.Sin(time * 1.7f)));

                // --- 5. ANILLO DE PLASMA — mitad DELANTERA (más brillante) ---
                DrawPlasmaRing(center, rr, time, seed, flick, front: true);

                // --- 6. CORREDORES DE FOTONES orbitando ---
                DrawPhotonRunners(center, rr, time, seed);

                // --- 7. RAYOS ELÉCTRICOS (dentro del núcleo + del anillo) ---
                DrawElectricBolts(center, r, time, seed, flick);

                // --- 8. DESTELLOS POLARES ---
                DrawPolarFlares(center, rr, time);

                // --- 9. RUNAS DORADAS orbitando + anillo rúnico ---
                DrawGoldenRunes(center, r, time, seed);

                // --- 10. ONDAS DE DISTORSIÓN expandiéndose ---
                DrawDistortionWaves(center, r, time, seed);

                // --- 11. PARTÍCULAS LUMINOSAS radiales ---
                DrawLuminousMotes(center, r, time, seed);

                // --- 12. AURA MÍSTICA final pulsante ---
                float aura = 0.85f + 0.15f * (float)Math.Sin(time * 2.0f);
                Quad(Glow, center, new Vector2(5.6f * rr, 5.6f * rr), 0f,
                    Tint(AuraViolet, 0.22f * aura));

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
        //  1. NEBULOSAS + POLVO AMBIENTAL
        // ------------------------------------------------------------------

        private static void DrawNebulas(Vector2 center, float rr, float time, int seed)
        {
            // Cinco velos púrpura/azul ENORMES y tenues girando lento:
            // "sutiles nebulosas púrpura y azul en la lejanía, muy difusas".
            for (int i = 0; i < 5; i++)
            {
                float h = Hash01(seed, 501 + i, 17);
                float dir = i % 2 == 0 ? 1f : -1f;
                float ang = h * MathHelper.TwoPi + time * 0.05f * dir;
                float dist = (2.7f + 0.9f * Hash01(seed, 502 + i, 29)) * rr;
                Vector2 pos = center + new Vector2(
                    (float)Math.Cos(ang) * dist, (float)Math.Sin(ang) * dist * 0.8f);
                float size = (2.0f + 1.1f * Hash01(seed, 503 + i, 41)) * rr;
                Color c = i % 2 == 0 ? NebPurple : NebBlue;
                float pulse = 0.75f + 0.25f * (float)Math.Sin(time * 0.7f + i * 1.9f);
                Quad(Glow, pos, new Vector2(size, size), ang,
                    Tint(c, (i % 2 == 0 ? 0.22f : 0.15f) * pulse));
            }

            // Polvo ambiental: 14 motas carmesí/magenta pulsando (la
            // referencia tiene polvo rojizo con más densidad cerca del
            // centro, desvaneciéndose hacia afuera).
            for (int i = 0; i < 14; i++)
            {
                float h = Hash01(seed, 600 + i, 13);
                float ang = h * MathHelper.TwoPi + time * 0.03f * (i % 2 == 0 ? 1f : -1f);
                float dist = (2.2f + 3.3f * Hash01(seed, 601 + i, 19)) * rr * 0.55f;
                Vector2 pos = center + new Vector2(
                    (float)Math.Cos(ang) * dist, (float)Math.Sin(ang) * dist);
                float pulse = 0.5f + 0.5f * (float)Math.Sin(time * 1.5f + i * 2.4f);
                Color c = h < 0.5f ? new Color(40, 20, 110) : new Color(60, 35, 160);
                float size = (0.10f + 0.10f * h) * rr;
                Quad(Glow, pos, new Vector2(size, size), 0f, Tint(c, 0.45f * pulse));
            }
        }

        // ------------------------------------------------------------------
        //  2/5. EL ANILLO DE PLASMA — 44 cápsulas por mitad
        // ------------------------------------------------------------------

        private static void DrawPlasmaRing(Vector2 center, float rr, float time, int seed,
            int flick, bool front)
        {
            // La mitad delantera (t ∈ 0..π) cruza POR DEBAJO-delante de la
            // esfera; la trasera (t ∈ π..2π) queda POR DETRÁS.
            float t0 = front ? 0f : MathHelper.Pi;
            float span = MathHelper.Pi;
            float bright = front ? 1.25f : 0.92f;

            // El hotspot DERIVA por el anillo (el plasma fluye).
            float hotspot = HotspotBase + time * PlasmaFlow;

            for (int s = 0; s < RingSegments; s++)
            {
                float t = t0 + span * (s + 0.5f) / RingSegments;

                // Posición + tangente de la cápsula.
                Vector2 a = Ellipse(center, rr, t - span / (RingSegments * 2f));
                Vector2 b = Ellipse(center, rr, t + span / (RingSegments * 2f));
                Vector2 mid = (a + b) * 0.5f;
                Vector2 seg = b - a;
                float segLen = seg.Length();
                if (segLen < 0.5f) continue;
                float rot = (float)Math.Atan2(seg.Y, seg.X);

                // INTENSIDAD: hotspot Doppler (una zona incandescente) ×
                // turbulencia por hash (regenerada a 12 Hz — el plasma SE
                // MUEVE, zonas brillantes intercaladas con sombras).
                float hot = 0.5f + 0.5f * (float)Math.Cos(t - hotspot);
                hot = 0.35f + 0.65f * hot * hot;
                float turb = 0.72f + 0.28f * Hash01(seed, 700 + s, flick);
                float inten = hot * turb * bright;

                // Gradiente térmico por intensidad:
                // incandescente → rosa → violeta-magenta.
                Color c;
                if (inten > 0.85f) c = HotCore;
                else if (inten > 0.55f) c = MidPink;
                else c = LowViolet;

                // Cápsula HALO (gruesa, tenue) + cápsula NÚCLEO (fina, viva).
                Capsule(mid, segLen, 0.34f * rr * (0.7f + inten), rot, Tint(c, 0.40f * inten));
                Capsule(mid, segLen, 0.11f * rr * inten, rot, Tint(c, 0.80f * inten));

                // En la zona MÁS caliente, un punto blanco extra (el plasma
                // casi se funde a blanco — "zonas más brillantes").
                if (inten > 0.88f)
                {
                    Quad(Glow, mid, new Vector2(0.30f * rr, 0.30f * rr), rot,
                        Tint(HotCore, 0.65f * (inten - 0.88f) / 0.12f));
                }
            }
        }

        // ------------------------------------------------------------------
        //  3. BRAZOS ESPIRALES — el vórtice se desenrosca
        // ------------------------------------------------------------------

        private static void DrawSpiralArms(Vector2 center, float rr, float time, int seed)
        {
            const int ArmSteps = 14;
            float armPhase = time * 0.18f;

            for (int arm = 0; arm < 2; arm++)
            {
                float baseT = armPhase + arm * MathHelper.Pi;
                for (int k = 0; k < ArmSteps; k++)
                {
                    float f = k / (float)(ArmSteps - 1);           // 0 en el anillo → 1 fuera
                    float t = baseT + f * 1.35f;                   // barrido angular
                    float grow = 1f + f * 0.95f;                   // radio creciente
                    Vector2 pos = VFXCore.Ellipse(center,
                        RingA * rr * grow, RingB * rr * grow, RingTilt, t);
                    Vector2 next = VFXCore.Ellipse(center,
                        RingA * rr * grow, RingB * rr * grow, RingTilt,
                        t + 1.35f / ArmSteps);
                    Vector2 mid = (pos + next) * 0.5f;
                    Vector2 seg = next - pos;
                    float len = seg.Length();
                    if (len < 0.5f) continue;
                    float rot = (float)Math.Atan2(seg.Y, seg.X);

                    float fade = (float)Math.Pow(1f - f, 1.5);         // se apaga suave
                    Color c = Color.Lerp(MidPink, LowViolet, f);
                    Capsule(mid, len, (0.34f - 0.20f * f) * rr, rot, Tint(c, 0.40f * fade));
                    Capsule(mid, len, (0.11f - 0.07f * f) * rr, rot, Tint(c, 0.60f * fade));
                }
            }
        }

        // ------------------------------------------------------------------
        //  6. CORREDORES DE FOTONES — luz orbitando y acelerando
        // ------------------------------------------------------------------

        private static void DrawPhotonRunners(Vector2 center, float rr, float time, int seed)
        {
            for (int i = 0; i < 3; i++)
            {
                float t = time * (1.25f + 0.22f * i) + i * 2.1f;
                Vector2 pos = Ellipse(center, rr, t);
                float twinkle = 0.65f + 0.35f * (float)Math.Sin(time * 8f + i * 2.3f);
                // halo magenta + núcleo blanco-rosa
                Quad(Glow, pos, new Vector2(1.05f * rr, 1.05f * rr), 0f,
                    Tint(MidPink, 0.35f * twinkle));
                Quad(Glow, pos, new Vector2(0.42f * rr, 0.42f * rr), 0f,
                    Tint(HotCore, 0.85f * twinkle));
            }
        }

        // ------------------------------------------------------------------
        //  7. RAYOS ELÉCTRICOS — zigzag determinista (regenerado ~7 Hz)
        // ------------------------------------------------------------------

        private static void DrawElectricBolts(Vector2 center, float r, float time, int seed, int flick)
        {
            int boltFlick = flick / 2;   // ~6 Hz para los rayos (viven frenéticos)

            // (v6.18: los RAYOS INTERIORES del núcleo fueron QUITADOS — el
            //  vacío queda NEGRO ABSOLUTO y LIMPIO, como en el agujero de
            //  la Bruma. Petición del usuario. Solo quedan los que ESCAPAN.)

            // 7.2 — DOS RAYOS ROSA EMERGIENDO DEL ANILLO hacia afuera
            //       ("rayos eléctricos que emergen del anillo").
            for (int i = 0; i < 2; i++)
            {
                if (Hash01(seed, 850 + i, boltFlick) < 0.30f) continue;

                float t = HotspotBase + time * PlasmaFlow + i * MathHelper.Pi +
                          0.6f * Hash01(seed, 851 + i, boltFlick / 3);
                Vector2 start = Ellipse(center, r, t);
                Vector2 outward = start - center;
                if (outward.LengthSquared() < 0.01f) continue;
                outward.Normalize();
                // Mezcla radial + tangencial: el rayo ESCAPA girando.
                Vector2 tangent = new Vector2(-outward.Y, outward.X) * 0.35f;
                Vector2 end = start + (outward + tangent) * (1.30f + 0.60f *
                    Hash01(seed, 852 + i, boltFlick)) * r;
                DrawBolt(start, end, seed + 100 + i * 53, boltFlick, r * 0.09f,
                    Tint(BoltViolet, 0.55f), Tint(BoltPink, 0.95f));
            }
        }

        /// <summary>
        /// Un rayo en zigzag con RAMAS (la matemática de BoltRenderer
        /// adaptada a coords de pantalla): funda de halo + núcleo fino por
        /// segmento + ramas laterales cortas donde el hash lo pide.
        /// </summary>
        private static void DrawBolt(Vector2 start, Vector2 end, int seed, int flick,
            float width, Color halo, Color core)
        {
            Vector2 delta = end - start;
            float length = delta.Length();
            if (length < 4f) return;

            Vector2 dir = delta / length;
            Vector2 normal = new Vector2(-dir.Y, dir.X);
            float amp = Math.Min(length * 0.16f, 0.30f * width * 4f);

            const int Segments = 6;
            Vector2[] pts = new Vector2[Segments + 1];
            for (int s = 0; s <= Segments; s++)
            {
                float f = s / (float)Segments;
                float envelope = (float)Math.Sin(f * Math.PI);   // tenso en el medio
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

                // RAMA lateral corta donde el hash lo pide (fractura real).
                if (s > 0 && s < Segments - 1 && Hash01(seed, flick, s + 91) > 0.62f)
                {
                    float side = Hash01(seed, flick, s + 37) > 0.5f ? 1f : -1f;
                    float branchLen = (0.35f + 0.4f * Hash01(seed, flick, s + 53)) * length * 0.25f;
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
        //  8. DESTELLOS POLARES — agujas ahusadas en el eje menor
        // ------------------------------------------------------------------

        private static void DrawPolarFlares(Vector2 center, float rr, float time)
        {
            // Eje menor de la elipse (los "polos" del vórtice).
            Vector2 pole = new Vector2(
                -(float)Math.Sin(RingTilt + MathHelper.PiOver2),
                (float)Math.Cos(RingTilt + MathHelper.PiOver2));
            float pulse = 0.7f + 0.3f * (float)Math.Sin(time * 1.7f);

            for (int side = 0; side < 2; side++)
            {
                Vector2 dir = side == 0 ? pole : -pole;
                float rot = (float)Math.Atan2(dir.Y, dir.X);

                // Tres tramos ahusados: base ancha → punta fina.
                float baseOff = RingB * rr * 0.85f;
                for (int k = 0; k < 3; k++)
                {
                    float f0 = k / 3f, f1 = (k + 1) / 3f;
                    float midF = (f0 + f1) * 0.5f;
                    Vector2 a = center + dir * (baseOff + f0 * 1.9f * rr);
                    Vector2 b = center + dir * (baseOff + f1 * 1.9f * rr);
                    Vector2 mid = (a + b) * 0.5f;
                    float len = (b - a).Length();
                    float w = (0.30f - 0.22f * midF) * rr;
                    Color c = Color.Lerp(HotCore, MidPink, midF);
                    Capsule(mid, len, w, rot, Tint(c, 0.30f * pulse * (1f - midF * 0.5f)));
                }

                // Punta incandescente del polo.
                Vector2 tip = center + dir * (baseOff + 1.95f * rr);
                Quad(Glow, tip, new Vector2(0.5f * rr, 0.5f * rr), 0f,
                    Tint(HotCore, 0.42f * pulse));
            }
        }

        // ------------------------------------------------------------------
        //  9. RUNAS DORADAS — 10 glifos originales orbitando en círculo
        //     v6.37 — delega en OrbitaLib (la librería de los anillos
        //     rúnicos): ni un número cambiado. La tabla R (10 runas) es
        //     PROPIA del Olvido → se pasa como `alphabet`.
        // ------------------------------------------------------------------

        /// <summary>
        /// Tabla de glifos: cada runa es una lista de TRAZOS (pares de puntos
        /// en espacio local ~11×15). Diez diseños angulares ORIGINALES del
        /// mod, dibujados como cápsulas doradas — escritura mágica antigua.
        /// </summary>
        private static readonly Vector2[][] _runes = new Vector2[][]
        {
            // R0 — LA LANZA
            new Vector2[] { new(-2.5f, 7f), new(-2.5f, -7f), new(-2.5f, -2f), new(3f, -6f), new(-2.5f, 3f), new(2.5f, -1.5f) },
            // R1 — EL CÁLIZ
            new Vector2[] { new(-4f, -6f), new(0f, 3f), new(4f, -6f), new(0f, 3f), new(-2f, 5.5f), new(2f, 5.5f) },
            // R2 — LA PUERTA
            new Vector2[] { new(-3f, 7f), new(-3f, -7f), new(3f, 7f), new(3f, -7f), new(-3f, -5f), new(3f, -5f), new(-3f, 2f), new(3f, 2f) },
            // R3 — LA ESTRELLA
            new Vector2[] { new(0f, 7f), new(0f, -7f), new(-4f, 0f), new(4f, 0f), new(-3f, -5f), new(3f, 5f), new(3f, -5f), new(-3f, 5f) },
            // R4 — EL RAYO
            new Vector2[] { new(-2f, 7f), new(0.5f, 1f), new(0.5f, 1f), new(-2f, -3f), new(-2f, -3f), new(2.5f, -7f) },
            // R5 — EL ARCO
            new Vector2[] { new(-3f, 6f), new(-1f, -6f), new(1f, -6f), new(3f, 6f), new(-1.5f, -6f), new(1.5f, -6f) },
            // R6 — LA ESPIRAL
            new Vector2[] { new(-3f, 6f), new(-3f, -2f), new(-3f, -2f), new(3f, -6f), new(3f, -6f), new(3f, 2f), new(3f, 2f), new(-2f, 6f) },
            // R7 — EL TRONO
            new Vector2[] { new(0f, 7f), new(0f, -7f), new(-3f, -2f), new(0f, -7f), new(0f, -2f), new(3f, -7f), new(-2f, 4f), new(2f, 4f) },
            // R8 — LA LLAVE
            new Vector2[] { new(0f, 7f), new(0f, -4f), new(0f, -4f), new(-3f, -7f), new(0f, -4f), new(3f, -7f), new(-2f, 1f), new(2f, 1f) },
            // R9 — EL OJO
            new Vector2[] { new(-4f, 0f), new(0f, -4.5f), new(0f, -4.5f), new(4f, 0f), new(4f, 0f), new(0f, 4.5f), new(0f, 4.5f), new(-4f, 0f), new(-1.5f, 0f), new(1.5f, 0f) },
        };

        /// <summary>
        /// v6.37 — delega en OrbitaLib (la librería de los anillos
        /// rúnicos): ni un número cambiado. El aro de pauta, las runas
        /// DE PIE y las perlas viven AHORA en la primitiva
        /// `OrbitaLib.CirculoRunico`. La tabla R (10 runas propias) se
        /// pasa como `alphabet`; el trazo GRUESO de la casa (3.8 vs el
        /// 3.4 sereno) como `strokeW` y la PERLA FRÍA (220,235,255) —
        /// que aquí era hardcoded — como `pearlTip`.
        /// </summary>
        private static void DrawGoldenRunes(Vector2 center, float r, float time, int seed)
        {
            OrbitaLib.CirculoRunico(center, r, time, RuneRadius, RuneCount, RuneOrbit,
                RuneGold, RuneTip, Math.Max(r / 52f, 0.25f) * 1.35f,
                alphabet: _runes, offset: 0, ringAlpha: 0.25f,
                strokeW: 3.8f, pearlTip: new Color(220, 235, 255));
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
                    Tint(new Color(200, 220, 255), 0.30f * fade));
            }
        }

        // ------------------------------------------------------------------
        //  11. PARTÍCULAS LUMINOSAS — deriva radial hacia afuera
        // ------------------------------------------------------------------

        private static void DrawLuminousMotes(Vector2 center, float r, float time, int seed)
        {
            for (int i = 0; i < 9; i++)
            {
                float h = Hash01(seed, 900 + i, 23);
                float life = (time * 0.13f + h) % 1f;
                float ang = Hash01(seed, 901 + i, 31) * MathHelper.TwoPi +
                            time * 0.06f * (i % 2 == 0 ? 1f : -1f);
                // Movimiento RADIAL HACIA AFUERA: el radio crece con la vida.
                float dist = (1.7f + life * 2.7f) * r;
                Vector2 pos = center + new Vector2(
                    (float)Math.Cos(ang) * dist, (float)Math.Sin(ang) * dist);

                // Variación de tamaño y opacidad (realismo).
                float size = (0.14f + 0.13f * h) * r * 2f;
                float alpha = (float)Math.Sin(life * Math.PI) * (0.55f + 0.45f * h);

                // Rosa / violeta / dorada (rara) / blanca.
                Color c = h < 0.40f ? MidPink
                        : h < 0.72f ? BoltViolet
                        : h < 0.90f ? RuneGold
                        : new Color(240, 245, 255);

                Quad(Glow, pos, new Vector2(size, size), 0f, Tint(c, alpha));
            }
        }
    }
}
