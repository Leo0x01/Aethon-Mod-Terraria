using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// CosmicAscendidoBlackHoleRenderer — v6.18 — EL CÓSMICO ASCENDIDO.
    ///
    /// LA COPIA MEJORADA del Cósmico definitivo (CosmicBlackHoleRenderer
    /// v6.16/v6.18 queda INTACTO — este archivo es un VÓRTICE NUEVO): el
    /// mismo cuerpo (núcleo de negro absoluto, anillo de VEINTE BANDAS
    /// sin(uv·20 + t·5), runas doradas, ecos, fotones, motas) elevado con
    /// LA LIBRERÍA DE RAYOS StormLib:
    ///
    ///   1. ⚡ TORMENTA DE RAYOS PRO — 4-6 StormLib.Bolt escapando
    ///      del anillo donde la banda del shader está EN SU PICO: doble
    ///      tira CUERPO (rojo-naranja) + NÚCLEO (ámbar casi blanco) con
    ///      RAMAS HEREDADAS y gorros de descarga. Mucho más rico que los
    ///      2 zigzags simples del original.
    ///   2. ⚡ CORONAS DE DESCARGA — 2 StormLib.Arc naranja
    ///      eléctrico abrazando el horizonte a radios 1.0× y 1.15×,
    ///      re-generándose a ~11 Hz con chispas satélite por Flicker.
    ///   3. DOBLE ANILLO DE BANDAS — el anillo principal + un ANILLO ECO
    ///      INTERIOR a 0.62× del radio del anillo, con la MISMA fórmula
    ///      sin(θ·20 + t·5) pero CONTRARROTANDO (la resonancia interior).
    ///   4. DOBLE CÍRCULO DE RUNAS — las 8 doradas girando + un segundo
    ///      círculo de 5 ÁMBAR contrarrotando a radio mayor.
    ///   5. JETS POLARES — chorros naranjas arriba/abajo (cápsulas
    ///      alargadas ANIMADAS con glow pulsante en la base) y un
    ///      StormLib.Bolt vibrando DENTRO de cada jet.
    ///
    /// Paleta ROJO-NARANJA incandescente (la del recolor v6.18 del
    /// Cósmico). TODO se compone AQUÍ, CADA FRAME, por código con los
    /// pinceles procedurales de la biblioteca VFX. Cero arte, cero
    /// estado, cero red (determinismo por hash puro).
    ///
    /// CONTRATO DE BATCH (idéntico al original v6.10): Draw() exige el
    /// SpriteBatch CERRADO y lo deja CERRADO.
    /// </summary>
    public static class CosmicAscendidoBlackHoleRenderer
    {
        // ==================================================================
        //  PARÁMETROS — el cuerpo del Cósmico + las medidas ascendidas
        // ==================================================================

        /// <summary>Radio de la esfera negra en px a escala 1 — GIGANTE.</summary>
        public const float SpherePx = 50f;

        /// <summary>Elipse del anillo energético (oblicua como un vórtice).</summary>
        private const float RingA = 1.90f;      // semieje mayor (×R)
        private const float RingB = 1.18f;      // semieje menor (×R)
        private const float RingTilt = -0.38f;  // rad — inclinación

        /// <summary>Rotación del anillo: 20°/s del script Unity.</summary>
        private const float RingSpin = 0.349f;  // rad/s (= 20°/s)

        /// <summary>
        /// LA FÓRMULA DEL SHADER: glow = sin(uv.x·20 + t·5)·0.5+0.5.
        /// </summary>
        private const float BandFreq = 20f;
        private const float BandSpeed = 5f;

        /// <summary>Distorsión sinusoidal global del script (0.3).</summary>
        private const float DistortionStrength = 0.3f;

        /// <summary>Segmentos de cada mitad del anillo.</summary>
        private const int RingSegments = 44;

        /// <summary>Regeneración de la turbulencia (Hz).</summary>
        private const float FlickHz = 12f;

        // --- runas doradas (círculo interior, igual que el original) ---
        private const int RuneCount = 8;
        private const float RuneRadius = 2.55f;   // ×R — círculo rúnico
        private const float RuneOrbit = 0.10f;    // rad/s

        // --- NUEVO: segundo círculo rúnico ÁMBAR (v6.18 ascendido) ---
        private const int AmberCount = 5;         // 5 glifos ámbar
        private const float AmberRadius = 3.15f;  // ×R — MÁS AFUERA que el dorado
        private const float AmberOrbit = -0.14f;  // rad/s — CONTRARROTANDO

        // --- NUEVO: anillo eco interior (doble anillo de bandas) ---
        private const float EchoScale = 0.62f;    // × el radio del anillo
        private const int EchoSegments = 26;      // cápsulas por mitad

        // --- NUEVO: tormenta/coronas/jets (la electricidad ascendida) ---
        private const float StormHz = 11f;        // ~11 Hz de re-generación

        // --- ondas de distorsión ---
        private const float WaveCycle = 2.4f;
        private const int WaveCount = 2;

        // ==================================================================
        //  PALETA — ROJO-NARANJA (v6.18, la del recolor del Cósmico) +
        //  los tonos ámbar/corona nuevos del Ascendido
        // ==================================================================

        private static readonly Color RingMagenta = new(255, 68, 26);    // rojo-naranja vivo
        private static readonly Color HotWhite = new(255, 240, 220);     // blanco cálido incandescente
        private static readonly Color DeepViolet = new(200, 30, 20);     // rojo profundo
        private static readonly Color BoltViolet = new(255, 90, 30);     // rayo interior naranja
        private static readonly Color BoltPink = new(255, 150, 40);      // rayo exterior ámbar
        private static readonly Color RuneGold = new(255, 170, 60);      // cuerpo de runa (oro)
        private static readonly Color RuneTip = new(255, 232, 170);     // punta de runa
        private static readonly Color NebViolet = new(190, 40, 30);     // nebulosa roja
        private static readonly Color NebBlue = new(200, 95, 25);       // nebulosa ámbar
        private static readonly Color AuraMagenta = new(220, 40, 25);    // aura de emisión roja

        // --- tonos NUEVOS del Ascendido ---
        private static readonly Color CrownOrange = new(255, 160, 60);   // corona de descarga (naranja eléctrico)
        private static readonly Color RuneAmber = new(255, 200, 90);     // runa ámbar exterior
        private static readonly Color AmberTip = new(255, 240, 205);     // punta de runa ámbar

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

                // LA DISTORSIÓN GLOBAL DEL SCRIPT: sin(t)·0.3 — un pulso
                // que atraviesa TODO el render (radio, ecos, aura).
                float distortion = (float)Math.Sin(time) * DistortionStrength;
                float breathe = 1f + 0.03f * distortion; // ±3%: temblor vivo
                float rr = r * breathe;

                // ============ 0. AURA OSCURA (alfa) ============
                // El núcleo oscuro ABSORBE la luz circundante.
                BeginAlpha();
                Quad(Glow, center, new Vector2(7.4f * rr, 7.4f * rr), 0f,
                    new Color(20, 4, 6, 175));
                Main.spriteBatch.End();

                // ============ 1..14: TODO LO BRILLANTE (aditivo) ============
                BeginAdditive();

                // --- 1. NEBULOSAS difusas + polvo ambiental ---
                DrawNebulas(center, rr, time, seed);

                // --- 2. ECOS DEL ANILLO (la resonancia del shader) ---
                DrawRingEchoes(center, rr, time, distortion);

                // --- 3. ANILLO ENERGÉTICO — mitad TRASERA ---
                DrawEnergyRing(center, rr, time, seed, flick, front: false);

                // --- 3.5 NUEVO: ANILLO ECO INTERIOR (0.62×, contrarrotando)
                //        — mitad TRASERA del eco ---
                DrawEchoBandRing(center, rr, time, seed, flick, front: false);

                // --- 4. NÚCLEO + rim del horizonte ---
                Main.spriteBatch.End();
                BeginAlpha();
                Quad(Disk, center, new Vector2(2.28f * r, 2.28f * r), 0f, Color.White);
                Main.spriteBatch.End();
                BeginAdditive();
                RingQuad(center, 1.02f * r, time * 0.15f,
                    Tint(RingMagenta, 0.40f + 0.12f * (float)Math.Sin(time * 1.7f)));

                // --- 5. ANILLO ENERGÉTICO — mitad DELANTERA (más brillante) ---
                DrawEnergyRing(center, rr, time, seed, flick, front: true);

                // --- 5.5 NUEVO: ANILLO ECO INTERIOR — mitad DELANTERA ---
                DrawEchoBandRing(center, rr, time, seed, flick, front: true);

                // --- 6. CORREDORES DE FOTONES orbitando ---
                DrawPhotonRunners(center, rr, time, seed);

                // --- 7. NUEVO: LA TORMENTA DE RAYOS PRO (4-6 Bolt con ramas) ---
                DrawLightningStorm(center, r, time, seed);

                // --- 7.5 NUEVO: CORONAS DE DESCARGA (2 Arc a ~11 Hz) ---
                DrawDischargeCrowns(center, r, time, seed);

                // --- 8. NUEVO: JETS POLARES (cápsulas animadas + Bolt interior) ---
                DrawPolarJets(center, rr, time, seed);

                // --- 9. RUNAS DORADAS + NUEVO círculo ÁMBAR contrarrotante ---
                DrawGoldenRunes(center, r, time, seed);
                DrawAmberRunes(center, r, time, seed);

                // --- 10. ONDAS DE DISTORSIÓN expandiéndose ---
                DrawDistortionWaves(center, r, time, seed);

                // --- 11. PARTÍCULAS LUMINOSAS radiales ---
                DrawLuminousMotes(center, r, time, seed);

                // --- 12. AURA FINAL pulsante (emisión ×5 del script) ---
                float aura = 0.85f + 0.15f * (float)Math.Sin(time * 2.2f);
                Quad(Glow, center, new Vector2(6.0f * rr, 6.0f * rr), 0f,
                    Tint(AuraMagenta, 0.24f * aura));

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
        //  1. NEBULOSAS + POLVO AMBIENTAL
        // ------------------------------------------------------------------

        private static void DrawNebulas(Vector2 center, float rr, float time, int seed)
        {
            // Cuatro velos rojo/ámbar ENORMES y tenues girando lento.
            for (int i = 0; i < 4; i++)
            {
                float h = Hash01(seed, 501 + i, 17);
                float dir = i % 2 == 0 ? 1f : -1f;
                float ang = h * MathHelper.TwoPi + time * 0.05f * dir;
                float dist = (2.8f + 0.8f * Hash01(seed, 502 + i, 29)) * rr;
                Vector2 pos = center + new Vector2(
                    (float)Math.Cos(ang) * dist, (float)Math.Sin(ang) * dist * 0.8f);
                float size = (2.0f + 1.0f * Hash01(seed, 503 + i, 41)) * rr;
                Color c = i % 2 == 0 ? NebViolet : NebBlue;
                float pulse = 0.75f + 0.25f * (float)Math.Sin(time * 0.7f + i * 1.9f);
                Quad(Glow, pos, new Vector2(size, size), ang,
                    Tint(c, (i % 2 == 0 ? 0.20f : 0.14f) * pulse));
            }

            // Polvo ambiental rojo-naranja pulsando (la energía que escapa).
            for (int i = 0; i < 12; i++)
            {
                float h = Hash01(seed, 600 + i, 13);
                float ang = h * MathHelper.TwoPi + time * 0.03f * (i % 2 == 0 ? 1f : -1f);
                float dist = (2.2f + 3.1f * Hash01(seed, 601 + i, 19)) * rr * 0.55f;
                Vector2 pos = center + new Vector2(
                    (float)Math.Cos(ang) * dist, (float)Math.Sin(ang) * dist);
                float pulse = 0.5f + 0.5f * (float)Math.Sin(time * 1.5f + i * 2.4f);
                Color c = h < 0.5f ? new Color(210, 45, 20) : new Color(200, 90, 25);
                float size = (0.10f + 0.10f * h) * rr;
                Quad(Glow, pos, new Vector2(size, size), 0f, Tint(c, 0.45f * pulse));
            }
        }

        // ------------------------------------------------------------------
        //  2. ECOS DEL ANILLO — la resonancia del shader CosmicRing
        // ------------------------------------------------------------------

        private static void DrawRingEchoes(Vector2 center, float rr, float time, float distortion)
        {
            // Dos anillos fantasma a ±1 banda de fase del shader: la emisión
            // del anillo "rebota" en el espacio curvado.
            float phase = time * BandSpeed * 0.20f;

            float echo1 = 0.55f + 0.45f * (float)Math.Sin(phase);
            RingQuad(center, 1.42f * rr, time * RingSpin,
                Tint(RingMagenta, 0.16f * echo1));

            float echo2 = 0.55f + 0.45f * (float)Math.Sin(phase + MathHelper.Pi);
            RingQuad(center, 1.60f * rr, -time * RingSpin * 0.7f,
                Tint(DeepViolet, 0.12f * echo2));

            // Un velo tenue de emisión alrededor del anillo entero.
            Quad(Glow, center, new Vector2(4.4f * rr, 3.6f * rr), RingTilt,
                Tint(RingMagenta, 0.10f + 0.05f * distortion));
        }

        // ------------------------------------------------------------------
        //  3/5. EL ANILLO ENERGÉTICO — EL SHADER COSMICRING FIEL
        // ------------------------------------------------------------------

        private static void DrawEnergyRing(Vector2 center, float rr, float time, int seed,
            int flick, bool front)
        {
            // La mitad delantera (t ∈ 0..π) cruza POR DELANTE de la esfera.
            float t0 = front ? 0f : MathHelper.Pi;
            float span = MathHelper.Pi;
            float bright = front ? 1.30f : 0.90f;

            // El anillo ROTA (rotationSpeed 20°/s del script Unity).
            float spin = time * RingSpin;

            for (int s = 0; s < RingSegments; s++)
            {
                float t = t0 + span * (s + 0.5f) / RingSegments;
                float tSpin = t + spin;   // el patrón de bandas gira con el anillo

                // Posición + tangente de la cápsula.
                Vector2 a = Ellipse(center, rr, t - span / (RingSegments * 2f));
                Vector2 b = Ellipse(center, rr, t + span / (RingSegments * 2f));
                Vector2 mid = (a + b) * 0.5f;
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
                float turb = 0.74f + 0.26f * Hash01(seed, 700 + s, flick);
                float inten = glow * turb * bright;

                // Gradiente térmico: banda al máximo = blanco-rosa
                // incandescente; media = magenta puro; valle = violeta.
                Color c;
                if (inten > 0.82f) c = HotWhite;
                else if (inten > 0.45f) c = RingMagenta;
                else c = DeepViolet;

                // Cápsula HALO (gruesa, tenue) + cápsula NÚCLEO (fina, viva).
                Capsule(mid, segLen, 0.36f * rr * (0.7f + inten), rot, Tint(c, 0.40f * inten));
                Capsule(mid, segLen, 0.12f * rr * inten, rot, Tint(c, 0.85f * inten));

                // En el PICO de cada banda, un punto blanco extra.
                if (inten > 0.86f)
                {
                    Quad(Glow, mid, new Vector2(0.32f * rr, 0.32f * rr), rot,
                        Tint(HotWhite, 0.70f * (inten - 0.86f) / 0.14f));
                }
            }
        }

        // ------------------------------------------------------------------
        //  3.5/5.5. NUEVO — EL ANILLO ECO INTERIOR (doble anillo de bandas)
        // ------------------------------------------------------------------

        private static void DrawEchoBandRing(Vector2 center, float rr, float time, int seed,
            int flick, bool front)
        {
            // ============================================================
            //  EL ANILLO ECO INTERIOR — LA FIRMA DEL DOBLE ANILLO: la
            //  MISMA fórmula de bandas del shader sin(θ·20 + t·5), pero
            //  a 0.62× del radio del anillo y CONTRARROTANDO (patrón en
            //  sentido opuesto a 1.6× la velocidad del anillo). El eco
            //  pasa POR DETRÁS de la esfera (mitad trasera antes del
            //  disco, delantera después) — la resonancia interior del
            //  vórtice ascendido.
            // ============================================================
            float t0 = front ? 0f : MathHelper.Pi;
            float span = MathHelper.Pi;
            float bright = front ? 1.10f : 0.80f;
            float spin = -time * RingSpin * 1.6f;   // CONTRARROTACIÓN viva

            for (int s = 0; s < EchoSegments; s++)
            {
                float t = t0 + span * (s + 0.5f) / EchoSegments;
                float tSpin = t + spin;

                Vector2 a = Ellipse(center, EchoScale * rr, t - span / (EchoSegments * 2f));
                Vector2 b = Ellipse(center, EchoScale * rr, t + span / (EchoSegments * 2f));
                Vector2 mid = (a + b) * 0.5f;
                Vector2 seg = b - a;
                float segLen = seg.Length();
                if (segLen < 0.5f) continue;
                float rot = (float)Math.Atan2(seg.Y, seg.X);

                // La MISMA fórmula del shader (20 bandas a 5 rad/s).
                float glow = 0.5f + 0.5f * (float)Math.Sin(tSpin * BandFreq - time * BandSpeed);
                float turb = 0.74f + 0.26f * Hash01(seed, 970 + s, flick);
                float inten = glow * turb * bright;

                // Gradiente TÉRMICO INVERSO: el eco está MÁS CERCA del
                // horizonte → más caliente (ámbar → rojo vivo).
                Color c;
                if (inten > 0.80f) c = HotWhite;
                else if (inten > 0.45f) c = BoltPink;
                else c = RingMagenta;

                Capsule(mid, segLen, 0.28f * rr * (0.7f + inten), rot, Tint(c, 0.34f * inten));
                Capsule(mid, segLen, 0.09f * rr * inten, rot, Tint(c, 0.72f * inten));
            }
        }

        // ------------------------------------------------------------------
        //  6. CORREDORES DE FOTONES — luz orbitando el vórtice
        // ------------------------------------------------------------------

        private static void DrawPhotonRunners(Vector2 center, float rr, float time, int seed)
        {
            for (int i = 0; i < 3; i++)
            {
                // Orbitan con la rotación del anillo (20°/s) más su propia
                // velocidad angular — fotones "corriendo" el vórtice.
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
                    Capsule(mid, len, 0.14f * rr, rot, Tint(RingMagenta, 0.40f * twinkle));
                }

                // halo rojo-naranja + núcleo blanco cálido
                Quad(Glow, pos, new Vector2(1.05f * rr, 1.05f * rr), 0f,
                    Tint(RingMagenta, 0.35f * twinkle));
                Quad(Glow, pos, new Vector2(0.42f * rr, 0.42f * rr), 0f,
                    Tint(HotWhite, 0.85f * twinkle));
            }
        }

        // ------------------------------------------------------------------
        //  7. NUEVO — LA TORMENTA DE RAYOS PRO (StormLib.Bolt)
        // ------------------------------------------------------------------

        private static void DrawLightningStorm(Vector2 center, float r, float time, int seed)
        {
            // ============================================================
            //  LA TORMENTA DE RAYOS PRO — el corazón del Ascendido:
            //  4-6 StormLib.Bolt de DOBLE TIRA (cuerpo rojo-naranja +
            //  núcleo ámbar casi blanco) con RAMAS HEREDADAS y gorros de
            //  descarga, ESCAPANDO del anillo donde la banda del shader
            //  está EN SU PICO. Cada rayo se re-genera a ~11 Hz y
            //  parpadea con Flicker propio (todo determinista por hash).
            // ============================================================
            int stormFlick = StormLib.FlickTick(time, StormHz);
            int count = 4 + (Hash01(seed, 404, stormFlick / 3) > 0.5f ? 2 : 0); // 4..6

            for (int i = 0; i < count; i++)
            {
                if (!StormLib.IsLit(seed + 300 + i * 97, stormFlick, 0.82f))
                    continue;

                // Emergen donde la banda del shader está EN SU PICO (como
                // el original), repartidos alrededor del anillo.
                float bandPeak = -time * BandSpeed / BandFreq + i * (MathHelper.TwoPi / count);
                float t = bandPeak + 0.5f * Hash01(seed, 851 + i, stormFlick / 3);
                Vector2 start = Ellipse(center, r, t);
                Vector2 outward = start - center;
                if (outward.LengthSquared() < 0.01f) continue;
                outward.Normalize();

                // Mezcla radial + tangencial: el rayo ESCAPA girando.
                float swirl = 0.25f + 0.25f * Hash01(seed, 858 + i, stormFlick);
                Vector2 tangent = new Vector2(-outward.Y, outward.X) * swirl;
                Vector2 end = start + (outward + tangent) *
                              (1.10f + 0.80f * Hash01(seed, 852 + i, stormFlick)) * r;

                // LA DOBLE TIRA CUERPO + NÚCLEO CON RAMAS de StormLib.
                StormLib.Bolt(Main.spriteBatch, start, end,
                    seed + 100 + i * 53, stormFlick, r * 0.105f,
                    Tint(BoltViolet, 0.52f), Tint(BoltPink, 0.92f), 1f, 7, r * 0.16f);
            }
        }

        // ------------------------------------------------------------------
        //  7.5 NUEVO — LAS CORONAS DE DESCARGA (StormLib.Arc)
        // ------------------------------------------------------------------

        private static void DrawDischargeCrowns(Vector2 center, float r, float time, int seed)
        {
            // ============================================================
            //  LAS CORONAS DE DESCARGA: dos ARCOS ELÉCTRICOS de
            //  StormLib abrazando el horizonte — uno a 1.0× y otro
            //  a 1.15× del radio, girando en SENTIDOS OPUESTOS. Se
            //  re-generan a ~11 Hz (parpadeo nervioso) y cada tanto
            //  sueltan una CHISPA SATÉLITE que persigue al arco
            //  principal. La tensión del vórtice hecha chispas circulares.
            // ============================================================
            int crownFlick = StormLib.FlickTick(time, StormHz);

            for (int c = 0; c < 2; c++)
            {
                float radius = (c == 0 ? 1.00f : 1.15f) * r;
                float drift = time * (c == 0 ? 1.15f : -0.85f) + c * 2.4f;
                float span = 1.30f + 0.50f * Hash01(seed, 942 + c, crownFlick);

                if (!StormLib.IsLit(seed + 941 + c * 7, crownFlick, 0.86f))
                    continue;

                // EL ARCO PRINCIPAL de la corona (~1/4 de vuelta).
                StormLib.ArcRing(Main.spriteBatch, center, radius,
                    drift, drift + span, seed + 500 + c * 13, crownFlick,
                    r * 0.075f, Tint(CrownOrange, 0.42f), Tint(HotWhite, 0.88f), 1f, 12);

                // CHISPA SATÉLITE (el 40% de las regeneraciones): mini-arco
                // que persigue al principal por el radio intermedio.
                if (StormLib.IsLit(seed + 963 + c * 3, crownFlick, 0.40f))
                {
                    float satA = drift - span * 0.55f;
                    StormLib.ArcRing(Main.spriteBatch, center, radius * 1.055f,
                        satA, satA + span * 0.35f, seed + 540 + c * 19, crownFlick,
                        r * 0.045f, Tint(CrownOrange, 0.30f), Tint(BoltPink, 0.70f), 1f, 7);
                }
            }
        }

        // ------------------------------------------------------------------
        //  8. NUEVO — LOS JETS POLARES (cápsulas animadas + Bolt interior)
        // ------------------------------------------------------------------

        private static void DrawPolarJets(Vector2 center, float rr, float time, int seed)
        {
            // ============================================================
            //  LOS JETS POLARES ASCENDIDOS: chorros naranjas arriba y
            //  abajo del vórtice (4 cápsulas alargadas Ahusadas cuya
            //  LONGITUD late con el tiempo), un GLOW PULSANTE en la base
            //  (el "motor" del jet) y un StormLib.Bolt vibrando
            //  DENTRO de cada chorro — el núcleo eléctrico del escape.
            // ============================================================
            int jetFlick = StormLib.FlickTick(time, StormHz);
            Vector2 pole = new Vector2(
                -(float)Math.Sin(RingTilt + MathHelper.PiOver2),
                (float)Math.Cos(RingTilt + MathHelper.PiOver2));

            for (int side = 0; side < 2; side++)
            {
                Vector2 dir = side == 0 ? pole : -pole;
                float rot = (float)Math.Atan2(dir.Y, dir.X);
                float pulse = 0.7f + 0.3f * (float)Math.Sin(time * 2.6f + side * 2.7f);

                // El chorro RESPIRA: su longitud late con el tiempo.
                float jetLen = (1.65f + 0.30f * (float)Math.Sin(time * 2.2f + side * 1.9f)) * rr;
                float baseOff = RingB * rr * 0.85f;

                // 4 tramos alargados ahusados (base ancha → punta fina).
                for (int k = 0; k < 4; k++)
                {
                    float f0 = k / 4f, f1 = (k + 1) / 4f;
                    float midF = (f0 + f1) * 0.5f;
                    Vector2 a = center + dir * (baseOff + f0 * jetLen);
                    Vector2 b = center + dir * (baseOff + f1 * jetLen);
                    Vector2 mid = (a + b) * 0.5f;
                    float len = (b - a).Length();
                    float w = (0.34f - 0.26f * midF) * rr;
                    Color c = Color.Lerp(HotWhite, RingMagenta, midF);
                    Capsule(mid, len, w, rot, Tint(c, 0.34f * pulse * (1f - midF * 0.45f)));
                }

                // EL RAYO INTERIOR del jet (el núcleo eléctrico vibrando).
                Vector2 start = center + dir * (baseOff * 0.9f);
                Vector2 end = center + dir * (baseOff + jetLen * 1.05f);
                if (StormLib.IsLit(seed + 701 + side * 37, jetFlick, 0.90f))
                    StormLib.Bolt(Main.spriteBatch, start, end,
                        seed + 810 + side * 41, jetFlick, rr * 0.085f,
                        Tint(BoltViolet, 0.45f), Tint(BoltPink, 0.88f), 1f, 6, rr * 0.09f);

                // Glow pulsante en la BASE (el "motor" del jet).
                Vector2 bpos = center + dir * baseOff;
                Quad(Glow, bpos, new Vector2(0.85f * rr, 0.85f * rr), 0f,
                    Tint(RingMagenta, 0.30f * pulse));
                Quad(Glow, bpos, new Vector2(0.38f * rr, 0.38f * rr), 0f,
                    Tint(HotWhite, 0.55f * pulse));

                // Punta incandescente del jet.
                Vector2 tip = center + dir * (baseOff + jetLen * 1.05f);
                Quad(Glow, tip, new Vector2(0.55f * rr, 0.55f * rr), 0f,
                    Tint(HotWhite, 0.42f * pulse));
            }
        }

        // ------------------------------------------------------------------
        //  9a. RUNAS DORADAS — el runeParticles del script (intacto)
        //      v6.37 — delega en OrbitaLib (la librería de los anillos
        //      rúnicos): ni un número cambiado. La tabla C (8 runas de
        //      CIRCUITO ENERGÉTICO) es PROPIA → se pasa como `alphabet`.
        // ------------------------------------------------------------------

        /// <summary>
        /// Tabla de glifos: cada runa es una lista de TRAZOS (pares de
        /// puntos en espacio local ~11×15). Ocho diseños angulares
        /// ORIGINALES de estilo CIRCUITO ENERGÉTICO, dibujados como
        /// cápsulas doradas (compartida con el círculo ámbar).
        /// </summary>
        private static readonly Vector2[][] _runes = new Vector2[][]
        {
            // C0 — EL PORTAL
            new Vector2[] { new(0f, 7f), new(4f, 0f), new(4f, 0f), new(0f, -7f), new(0f, -7f), new(-4f, 0f), new(-4f, 0f), new(0f, 7f), new(0f, 5f), new(0f, -5f) },
            // C1 — LA ONDA
            new Vector2[] { new(-3.5f, 6f), new(-3.5f, 2f), new(-3.5f, 2f), new(3.5f, -2f), new(3.5f, -2f), new(3.5f, -6f) },
            // C2 — EL NODO
            new Vector2[] { new(-3.5f, -5f), new(3.5f, 5f), new(3.5f, -5f), new(-3.5f, 5f), new(-3.5f, 0f), new(3.5f, 0f) },
            // C3 — LAS FLECHAS
            new Vector2[] { new(-3f, 6.5f), new(3f, 6.5f), new(3f, 6.5f), new(0f, 3.5f), new(-3f, 1f), new(3f, 1f), new(3f, 1f), new(0f, -2f), new(-3f, -4.5f), new(3f, -4.5f), new(0f, -7f), new(0f, -2f) },
            // C4 — EL CIRCUITO
            new Vector2[] { new(-3.5f, 6f), new(-3.5f, -4f), new(-3.5f, -4f), new(3.5f, -4f), new(3.5f, -4f), new(3.5f, 6f), new(3.5f, 6f), new(-3.5f, 6f), new(-3.5f, 1f), new(3.5f, 1f) },
            // C5 — LA CRUZ ESTELADA
            new Vector2[] { new(0f, 7f), new(0f, -7f), new(-4f, 0f), new(4f, 0f), new(-2.5f, -2.5f), new(2.5f, 2.5f), new(2.5f, -2.5f), new(-2.5f, 2.5f) },
            // C6 — EL ANCLA DE ENERGÍA
            new Vector2[] { new(0f, 7f), new(0f, -3f), new(0f, -3f), new(-3.5f, -6.5f), new(0f, -3f), new(3.5f, -6.5f), new(-2.5f, 4f), new(2.5f, 4f) },
            // C7 — EL ROMBO PARTIDO
            new Vector2[] { new(0f, 6.5f), new(3.5f, 0f), new(3.5f, 0f), new(0f, -6.5f), new(0f, -6.5f), new(-3.5f, 0f), new(-3.5f, 0f), new(0f, 6.5f), new(-1.2f, -1f), new(1.2f, 1f) },
        };

        /// <summary>
        /// v6.37 — delega en OrbitaLib (la librería de los anillos
        /// rúnicos): ni un número cambiado. El aro de pauta, las runas
        /// DE PIE y las perlas viven AHORA en la primitiva
        /// `OrbitaLib.CirculoRunico`. La tabla C (8 runas propias de
        /// CIRCUITO ENERGÉTICO) se pasa como `alphabet`; el trazo GRUESO
        /// de la casa (3.8) como `strokeW` y la PERLA CÁLIDA
        /// (255,240,200) — que aquí era hardcoded — como `pearlTip`.
        /// </summary>
        private static void DrawGoldenRunes(Vector2 center, float r, float time, int seed)
        {
            OrbitaLib.CirculoRunico(center, r, time, RuneRadius, RuneCount, RuneOrbit,
                RuneGold, RuneTip, Math.Max(r / 52f, 0.25f) * 1.35f,
                alphabet: _runes, offset: 0, ringAlpha: 0.22f,
                strokeW: 3.8f, pearlTip: new Color(255, 240, 200));
        }

        // ------------------------------------------------------------------
        //  9b. NUEVO — EL CÍRCULO ÁMBAR CONTRARROTANTE (doble círculo rúnico)
        //      v6.37 — delega en OrbitaLib (la librería de los anillos
        //      rúnicos) con el humor NERVIOSO: ni un número cambiado.
        // ------------------------------------------------------------------

        /// <summary>
        /// v6.37 — delega en OrbitaLib (la librería de los anillos
        /// rúnicos): ni un número cambiado. El humor NERVIOSO de la
        /// librería ES el de aquí (respiración 2.2·sin(1.1t+1.4g), vaivén
        /// 1.8·sin(0.95t+2.1g), latido 0.70+0.30·sin(2.8t+1.7g), glow 30
        /// @0.18, trazos 3.2 @0.80, perlas 6.2/2.9 @0.55/0.90 a 10.5·gs
        /// fundidas al latido del glifo); los glifos desfasados de la
        /// tabla (`_runes[(g*3+2) % len]`) como `stride: 3, offset: 2`;
        /// la PERLA ÁMBAR cálida (255,245,220) — que aquí era hardcoded —
        /// como `pearlTip`.
        /// </summary>
        private static void DrawAmberRunes(Vector2 center, float r, float time, int seed)
        {
            // ============================================================
            //  EL SEGUNDO CÍRCULO RÚNICO: 5 glifos ÁMBAR a radio MAYOR
            //  (3.15×R) CONTRARROTANDO respecto a las runas doradas —
            //  dos coronas de conjuro girando en sentidos opuestos, la
            //  firma rúnica del Cósmico Ascendido.
            // ============================================================
            OrbitaLib.CirculoRunico(center, r, time, AmberRadius, AmberCount, AmberOrbit,
                RuneAmber, AmberTip, Math.Max(r / 52f, 0.25f) * 1.10f,
                alphabet: _runes, stride: 3, offset: 2,
                ringAlpha: 0.18f, nervioso: true,
                pearlTip: new Color(255, 245, 220));
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
                    Tint(new Color(255, 200, 150), 0.30f * fade));
            }
        }

        // ------------------------------------------------------------------
        //  11. PARTÍCULAS LUMINOSAS — deriva radial hacia afuera
        // ------------------------------------------------------------------

        private static void DrawLuminousMotes(Vector2 center, float r, float time, int seed)
        {
            for (int i = 0; i < 12; i++)
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

                // Rojo-naranja / naranja / ámbar (rara) / blanca.
                Color c = h < 0.40f ? RingMagenta
                        : h < 0.72f ? BoltViolet
                        : h < 0.90f ? RuneAmber
                        : new Color(255, 250, 252);

                Quad(Glow, pos, new Vector2(size, size), 0f, Tint(c, alpha));
            }
        }
    }
}
