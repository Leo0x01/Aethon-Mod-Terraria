using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// CosmicBlackHoleRenderer — v6.16 — EL AGUJERO NEGRO CÓSMICO, 100% CÓDIGO.
    ///
    /// NACIDO DEL SCRIPT UNITY DEL USUARIO (CosmicBlackHole.cs + shader
    /// "Custom/CosmicRing"), traducido píxel a píxel al sistema de pinceles
    /// procedurales del mod:
    ///
    ///   · coreSphere      → núcleo de NEGRO ABSOLUTO (BlackDisk).
    ///   · energyRing      → anillo MAGENTA (1.0, 0.2, 0.8) = (255,51,204)
    ///                        con emisión ×5, rotando a 20°/s.
    ///   · lightningParticles → RAYOS ELÉCTRICOS rosa/violeta.
    ///   · runeParticles   → RUNAS DORADAS flotando en círculo.
    ///   · distortionStrength 0.3 → distorsión SINUSOIDAL global
    ///                        (Shader.SetGlobalFloat(sin(t)·0.3)).
    ///   · EL SHADER, FIEL:  glow = sin(uv.x·20 + t·5)·0.5 + 0.5
    ///     → VEINTE BANDAS DE BRILLO recorriendo el anillo, Blend SrcAlpha
    ///       One (aditivo) — aquí son 20 zonas incandescentes que VIAJAN
    ///       por las cápsulas del anillo a 5 rad/s.
    ///
    /// Y LO QUE FALTABA (petición expresa: "agrega lo que falta"): aura
    /// oscura que absorbe la luz, nebulosas de fondo, ecos del anillo,
    /// corredores de fotones, destellos polares, ondas de distorsión,
    /// partículas radiales y aura final.
    ///
    /// TODO se compone AQUÍ, CADA FRAME, por código (~360 cuadros de luz)
    /// con solo TRES pinceles genéricos de la biblioteca VFX (SoftGlow,
    /// Ring, BlackDisk). Cero sprites de arte, cero estado, cero red.
    ///
    /// LAS CAPAS (orden de pintado):
    ///   0. AURA OSCURA (alfa) — el vacío absorbe la luz circundante.
    ///   1. NEBULOSAS violeta/azul + polvo ambiental magenta.
    ///   2. ECOS DEL ANILLO — dos anillos fantasma respirando con la
    ///      misma fase de bandas (la resonancia del shader).
    ///   3. ANILLO ENERGÉTICO (mitad TRASERA) con las 20 BANDAS sin(20t+5τ).
    ///   4. NÚCLEO negro absoluto + rim magenta del horizonte.
    ///   5. ANILLO ENERGÉTICO (mitad DELANTERA, más brillante).
    ///   6. CORREDORES DE FOTONES — 3 destellos blanco-rosa orbitando.
    ///   7. RAYOS ELÉCTRICOS — 2 violetas DANZANDO EN EL NÚCLEO + 2
    ///      magenta EMERGIENDO del anillo (el lightningParticles).
    ///   8. DESTELLOS POLARES — agujas ahusadas en el eje del vórtice.
    ///   9. RUNAS DORADAS — 8 glifos de circuito orbitando + aro tenue.
    ///  10. ONDAS DE DISTORSIÓN — 2 anillos expandiéndose + el vaivén
    ///      sinusoidal del radio (la distorsión 0.3 del script).
    ///  11. PARTÍCULAS LUMINOSAS — 9 motas derivando radialmente afuera.
    ///  12. AURA FINAL magenta pulsante (el resplandor de emisión ×5).
    ///
    /// CONTRATO DE BATCH (v6.10): Draw() exige el SpriteBatch CERRADO
    /// y lo deja CERRADO.
    /// </summary>
    public static class CosmicBlackHoleRenderer
    {
        // ==================================================================
        //  PARÁMETROS — calibrados contra el script Unity del usuario
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
        /// 20 bandas viajando a 5 rad/s en espacio paramétrico.
        /// </summary>
        private const float BandFreq = 20f;
        private const float BandSpeed = 5f;

        /// <summary>
        /// Distorsión sinusoidal global del script (0.3): el radio del
        /// anillo VIBRA con sin(t)·0.3 — aquí, acotado al 3% del radio
        /// para que sea un temblor vivo sin romper la elipse.
        /// </summary>
        private const float DistortionStrength = 0.3f;

        /// <summary>Segmentos de cada mitad del anillo.</summary>
        private const int RingSegments = 44;

        /// <summary>Regeneración de la turbulencia (Hz).</summary>
        private const float FlickHz = 12f;

        // --- runas doradas ---
        private const int RuneCount = 8;
        private const float RuneRadius = 2.55f;   // ×R — círculo rúnico
        private const float RuneOrbit = 0.10f;    // rad/s

        // --- ondas de distorsión ---
        private const float WaveCycle = 2.4f;
        private const int WaveCount = 2;

        // ==================================================================
        //  PALETA — el magenta EXACTO del script (1.0, 0.2, 0.8)
        // ==================================================================

        private static readonly Color RingMagenta = new(255, 51, 204);  // (1.0, 0.2, 0.8)
        private static readonly Color HotWhite = new(255, 238, 252);    // blanco-rosa incandescente
        private static readonly Color DeepViolet = new(150, 40, 255);   // violeta profundo
        private static readonly Color BoltViolet = new(170, 70, 255);   // rayo interior
        private static readonly Color BoltPink = new(255, 80, 200);     // rayo exterior
        private static readonly Color RuneGold = new(255, 170, 60);     // cuerpo de runa
        private static readonly Color RuneTip = new(255, 232, 170);     // punta de runa
        private static readonly Color NebViolet = new(90, 30, 190);     // nebulosa
        private static readonly Color NebBlue = new(40, 70, 200);       // nebulosa fría
        private static readonly Color AuraMagenta = new(190, 30, 150);  // aura de emisión

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
                Quad(Glow, center, new Vector2(7.0f * rr, 7.0f * rr), 0f,
                    new Color(14, 2, 18, 170));
                Main.spriteBatch.End();

                // ============ 1..12: TODO LO BRILLANTE (aditivo) ============
                BeginAdditive();

                // --- 1. NEBULOSAS difusas + polvo ambiental ---
                DrawNebulas(center, rr, time, seed);

                // --- 2. ECOS DEL ANILLO (la resonancia del shader) ---
                DrawRingEchoes(center, rr, time, distortion);

                // --- 3. ANILLO ENERGÉTICO — mitad TRASERA ---
                DrawEnergyRing(center, rr, time, seed, flick, front: false);

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

                // --- 6. CORREDORES DE FOTONES orbitando ---
                DrawPhotonRunners(center, rr, time, seed);

                // --- 7. RAYOS ELÉCTRICOS (en el núcleo + del anillo) ---
                DrawElectricBolts(center, r, time, seed, flick);

                // --- 8. DESTELLOS POLARES ---
                DrawPolarFlares(center, rr, time);

                // --- 9. RUNAS DORADAS orbitando + aro tenue ---
                DrawGoldenRunes(center, r, time, seed);

                // --- 10. ONDAS DE DISTORSIÓN expandiéndose ---
                DrawDistortionWaves(center, r, time, seed);

                // --- 11. PARTÍCULAS LUMINOSAS radiales ---
                DrawLuminousMotes(center, r, time, seed);

                // --- 12. AURA FINAL pulsante (emisión ×5 del script) ---
                float aura = 0.85f + 0.15f * (float)Math.Sin(time * 2.2f);
                Quad(Glow, center, new Vector2(5.8f * rr, 5.8f * rr), 0f,
                    Tint(AuraMagenta, 0.22f * aura));

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
            // Cuatro velos violeta/azul ENORMES y tenues girando lento.
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

            // Polvo ambiental magenta pulsando (la energía que escapa).
            for (int i = 0; i < 12; i++)
            {
                float h = Hash01(seed, 600 + i, 13);
                float ang = h * MathHelper.TwoPi + time * 0.03f * (i % 2 == 0 ? 1f : -1f);
                float dist = (2.2f + 3.1f * Hash01(seed, 601 + i, 19)) * rr * 0.55f;
                Vector2 pos = center + new Vector2(
                    (float)Math.Cos(ang) * dist, (float)Math.Sin(ang) * dist);
                float pulse = 0.5f + 0.5f * (float)Math.Sin(time * 1.5f + i * 2.4f);
                Color c = h < 0.5f ? new Color(190, 25, 130) : new Color(120, 30, 200);
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
            // del anillo "rebota" en el espacio curvado. Respiran con la
            // distorsión sinusoidal global del script.
            float phase = time * BandSpeed * 0.20f;

            float echo1 = 0.55f + 0.45f * (float)Math.Sin(phase);
            RingQuad(center, 1.42f * rr, time * RingSpin,
                Tint(RingMagenta, 0.16f * echo1));

            float echo2 = 0.55f + 0.45f * (float)Math.Sin(phase + MathHelper.Pi);
            RingQuad(center, 0.80f * rr, -time * RingSpin * 0.7f,
                Tint(DeepViolet, 0.14f * echo2));

            // Un velo tenue de emisión alrededor del anillo entero (el
            // resplandor de la emisión ×5 envolviendo el vórtice).
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

                // En el PICO de cada banda, un punto blanco extra (la
                // emisión ×5 del material casi fundiéndose a blanco).
                if (inten > 0.86f)
                {
                    Quad(Glow, mid, new Vector2(0.32f * rr, 0.32f * rr), rot,
                        Tint(HotWhite, 0.70f * (inten - 0.86f) / 0.14f));
                }
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

                // halo magenta + núcleo blanco-rosa
                Quad(Glow, pos, new Vector2(1.05f * rr, 1.05f * rr), 0f,
                    Tint(RingMagenta, 0.35f * twinkle));
                Quad(Glow, pos, new Vector2(0.42f * rr, 0.42f * rr), 0f,
                    Tint(HotWhite, 0.85f * twinkle));
            }
        }

        // ------------------------------------------------------------------
        //  7. RAYOS ELÉCTRICOS — el lightningParticles del script
        // ------------------------------------------------------------------

        private static void DrawElectricBolts(Vector2 center, float r, float time, int seed, int flick)
        {
            int boltFlick = flick / 2;   // ~6 Hz (viven frenéticos)

            // 7.1 — DOS RAYOS VIOLETAS DENTRO DEL NÚCLEO: la tormenta
            //       eléctrica del vacío (el script los enciende con Play()).
            Color boltCoreWhite = new Color(240, 215, 255);
            for (int i = 0; i < 2; i++)
            {
                if (Hash01(seed, 810 + i, boltFlick) < 0.10f) continue;

                float a1 = Hash01(seed, 811 + i, boltFlick) * MathHelper.TwoPi;
                float a2 = a1 + MathHelper.Pi * (0.5f + 0.7f * Hash01(seed, 812 + i, boltFlick));
                float r1 = 0.15f + 0.40f * Hash01(seed, 813 + i, boltFlick);
                float r2 = 0.15f + 0.40f * Hash01(seed, 814 + i, boltFlick);
                Vector2 start = center + new Vector2(
                    (float)Math.Cos(a1) * r1 * r, (float)Math.Sin(a1) * r1 * r);
                Vector2 end = center + new Vector2(
                    (float)Math.Cos(a2) * r2 * r, (float)Math.Sin(a2) * r2 * r);
                DrawBolt(start, end, seed + i * 37, boltFlick, r * 0.13f,
                    Tint(BoltViolet, 0.75f), Tint(boltCoreWhite, 1f));
            }

            // 7.2 — DOS RAYOS MAGENTA EMERGIENDO DEL ANILLO hacia afuera
            //       (el lightningParticles naciendo del energyRing).
            for (int i = 0; i < 2; i++)
            {
                if (Hash01(seed, 850 + i, boltFlick) < 0.30f) continue;

                // Emergen donde la banda del shader está EN SU PICO.
                float bandPeak = -time * BandSpeed / BandFreq + i * MathHelper.Pi;
                float t = bandPeak + 0.5f * Hash01(seed, 851 + i, boltFlick / 3);
                Vector2 start = Ellipse(center, r, t);
                Vector2 outward = start - center;
                if (outward.LengthSquared() < 0.01f) continue;
                outward.Normalize();
                // Mezcla radial + tangencial: el rayo ESCAPA girando.
                Vector2 tangent = new Vector2(-outward.Y, outward.X) * 0.35f;
                Vector2 end = start + (outward + tangent) * (1.35f + 0.60f *
                    Hash01(seed, 852 + i, boltFlick)) * r;
                DrawBolt(start, end, seed + 100 + i * 53, boltFlick, r * 0.10f,
                    Tint(BoltViolet, 0.55f), Tint(BoltPink, 0.95f));
            }
        }

        /// <summary>
        /// Un rayo en zigzag con RAMAS (la matemática de BoltRenderer en
        /// coords de pantalla): funda de halo + núcleo fino por segmento +
        /// ramas laterales cortas donde el hash lo pide.
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
        //  8. DESTELLOS POLARES — agujas en el eje menor
        // ------------------------------------------------------------------

        private static void DrawPolarFlares(Vector2 center, float rr, float time)
        {
            Vector2 pole = new Vector2(
                -(float)Math.Sin(RingTilt + MathHelper.PiOver2),
                (float)Math.Cos(RingTilt + MathHelper.PiOver2));
            float pulse = 0.7f + 0.3f * (float)Math.Sin(time * 1.9f);

            for (int side = 0; side < 2; side++)
            {
                Vector2 dir = side == 0 ? pole : -pole;
                float rot = (float)Math.Atan2(dir.Y, dir.X);

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
                    Color c = Color.Lerp(HotWhite, RingMagenta, midF);
                    Capsule(mid, len, w, rot, Tint(c, 0.30f * pulse * (1f - midF * 0.5f)));
                }

                // Punta incandescente del polo.
                Vector2 tip = center + dir * (baseOff + 1.95f * rr);
                Quad(Glow, tip, new Vector2(0.5f * rr, 0.5f * rr), 0f,
                    Tint(HotWhite, 0.42f * pulse));
            }
        }

        // ------------------------------------------------------------------
        //  9. RUNAS DORADAS — el runeParticles del script
        // ------------------------------------------------------------------

        /// <summary>
        /// Tabla de glifos: cada runa es una lista de TRAZOS (pares de
        /// puntos en espacio local ~11×15). Ocho diseños angulares
        /// ORIGINALES de estilo CIRCUITO ENERGÉTICO (diamantes, ondas y
        /// nodos — la estirpe del anillo cósmico), dibujados como cápsulas
        /// doradas.
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

        private static void DrawGoldenRunes(Vector2 center, float r, float time, int seed)
        {
            float glyphScale = Math.Max(r / 52f, 0.25f) * 1.35f;

            // Aro rúnico tenue que UNE los glifos (el círculo del conjuro).
            RingQuad(center, RuneRadius * r, time * RuneOrbit,
                Tint(RuneGold, 0.22f));

            for (int g = 0; g < RuneCount; g++)
            {
                float ang = g / (float)RuneCount * MathHelper.TwoPi + time * RuneOrbit;

                // Flotación viva: el radio respira por glifo y el glifo se
                // mece verticalmente (el runeParticles flotando).
                float floatR = RuneRadius * r +
                               2.4f * glyphScale * (float)Math.Sin(time * 1.35f + g * 0.9f);
                float bobY = 2.0f * glyphScale * (float)Math.Sin(time * 0.85f + g * 1.7f);
                Vector2 glyphPos = center + new Vector2(
                    (float)Math.Cos(ang) * floatR,
                    (float)Math.Sin(ang) * floatR + bobY);

                // Latido de brillo propio por glifo.
                float pulse = 0.75f + 0.25f * (float)Math.Sin(time * 2.4f + g * 1.3f);

                // Resplandor suave DETRÁS de cada runa.
                Quad(Glow, glyphPos, new Vector2(36f * glyphScale, 36f * glyphScale), 0f,
                    Tint(RuneGold, 0.20f * pulse));

                // Trazos: cápsulas doradas, cuerpo → punta pálida.
                Vector2[] strokes = _runes[g % _runes.Length];
                for (int s = 0; s < strokes.Length; s += 2)
                {
                    Vector2 a = glyphPos + strokes[s] * glyphScale;
                    Vector2 b = glyphPos + strokes[s + 1] * glyphScale;
                    Vector2 mid = (a + b) * 0.5f;
                    Vector2 delta = b - a;
                    float len = delta.Length();
                    if (len < 0.01f) continue;
                    float rot = (float)Math.Atan2(delta.Y, delta.X);

                    // Gradiente vertical: abajo cuerpo dorado, arriba punta pálida.
                    float localY = ((strokes[s].Y + strokes[s + 1].Y) * 0.5f + 7f) / 14f;
                    Color col = Color.Lerp(RuneTip, RuneGold, 1f - localY * 0.25f);

                    Capsule(mid, len, 3.8f * glyphScale, rot, Tint(col, 0.85f * pulse));
                }

                // PERLA dorada sobre el glifo (la gema del circuito).
                Vector2 pearlPos = glyphPos - new Vector2(0f, 11.5f * glyphScale);
                float pearlPulse = 0.8f + 0.2f * (float)Math.Sin(time * 3.0f + g * 2.0f);
                Quad(Glow, pearlPos, new Vector2(7.0f * glyphScale, 7.0f * glyphScale), 0f,
                    Tint(RuneGold, 0.62f * pulse));
                Quad(Glow, pearlPos, new Vector2(3.2f * glyphScale, 3.2f * glyphScale), 0f,
                    Tint(new Color(255, 240, 200), 0.9f * pearlPulse));
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
                    Tint(new Color(255, 205, 245), 0.30f * fade));
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

                // Magenta / violeta / dorada (rara) / blanca.
                Color c = h < 0.40f ? RingMagenta
                        : h < 0.72f ? BoltViolet
                        : h < 0.90f ? RuneGold
                        : new Color(255, 250, 252);

                Quad(Glow, pos, new Vector2(size, size), 0f, Tint(c, alpha));
            }
        }
    }
}
