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

        /// <summary>Elipse del anillo energético (oblicua como un vórtice) —
        /// LA LEY VIVE EN ORBITALIB (la librería de signos mágicos del vacío).</summary>
        private const float RingA = OrbitaLib.RingA;      // semieje mayor (×R)
        private const float RingB = OrbitaLib.RingB;      // semieje menor (×R)
        private const float RingTilt = OrbitaLib.RingTilt; // rad — inclinación

        /// <summary>Rotación del anillo: 20°/s de la casa (OrbitaLib).</summary>
        private const float RingSpin = OrbitaLib.RingSpin;  // rad/s (= 20°/s)

        /// <summary>LA FÓRMULA DEL SHADER (OrbitaLib): 20 bandas a 5 rad/s.</summary>
        private const float BandFreq = OrbitaLib.BandFreq;
        private const float BandSpeed = OrbitaLib.BandSpeed;

        /// <summary>Distorsión sinusoidal global (OrbitaLib.DistorsionFuerza).</summary>
        private const float DistortionStrength = OrbitaLib.DistorsionFuerza;

        /// <summary>Segmentos de cada mitad del anillo (OrbitaLib).</summary>
        private const int RingSegments = OrbitaLib.RingSegments;

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
        //  PALETA — ROJO-NARANJA (v6.18: recolor a petición del usuario para
        // diferenciarlo del Olvido — era magenta del script Unity original)
        // ==================================================================

        private static readonly Color RingMagenta = new(255, 68, 26);    // rojo-naranja vivo
        private static readonly Color HotWhite = new(255, 240, 220);     // blanco cálido incandescente
        private static readonly Color DeepViolet = new(200, 30, 20);     // rojo profundo
        private static readonly Color BoltViolet = new(255, 90, 30);     // rayo interior naranja
        private static readonly Color BoltPink = new(255, 150, 40);      // rayo exterior ámbar
        private static readonly Color RuneGold = new(255, 170, 60);      // cuerpo de runa (oro — encaja)
        private static readonly Color RuneTip = new(255, 232, 170);     // punta de runa
        private static readonly Color NebViolet = new(190, 40, 30);     // nebulosa roja
        private static readonly Color NebBlue = new(200, 95, 25);       // nebulosa ámbar
        private static readonly Color AuraMagenta = new(220, 40, 25);    // aura de emisión roja

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
                    new Color(20, 4, 6, 170));
                Main.spriteBatch.End();

                // ============ 1..12: TODO LO BRILLANTE (aditivo) ============
                BeginAdditive();

                // --- 1. NEBULOSAS difusas + polvo ambiental ---
                DrawNebulas(center, rr, time, seed);

                // --- 2. ECOS DEL ANILLO (la resonancia del shader) —
                //     ORBITALIB.EcosAnillo (la técnica promovida a librería)
                OrbitaLib.EcosAnillo(center, rr, time, distortion);

                // --- 3. ANILLO ENERGÉTICO — mitad TRASERA —
                //     ORBITALIB.AnilloEnergia (la fórmula fiel del shader)
                OrbitaLib.AnilloEnergia(center, rr, time, seed, flick, front: false);

                // --- 4. NÚCLEO + rim del horizonte ---
                Main.spriteBatch.End();
                BeginAlpha();
                Quad(Disk, center, new Vector2(2.28f * r, 2.28f * r), 0f, Color.White);
                Main.spriteBatch.End();
                BeginAdditive();
                RingQuad(center, 1.02f * r, time * 0.15f,
                    Tint(RingMagenta, 0.40f + 0.12f * (float)Math.Sin(time * 1.7f)));

                // --- 5. ANILLO ENERGÉTICO — mitad DELANTERA (más brillante) —
                //     ORBITALIB.AnilloEnergia
                OrbitaLib.AnilloEnergia(center, rr, time, seed, flick, front: true);

                // --- 6. CORREDORES DE FOTONES orbitando — ORBITALIB.Fotones
                OrbitaLib.Fotones(center, rr, time);

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
                VFXCore.CerrarLoteSiAbierto();
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
                Color c = h < 0.5f ? new Color(210, 45, 20) : new Color(200, 90, 25);
                float size = (0.10f + 0.10f * h) * rr;
                Quad(Glow, pos, new Vector2(size, size), 0f, Tint(c, 0.45f * pulse));
            }
        }

        // ------------------------------------------------------------------
        //  2/3/5/6. EL ANILLO ENERGÉTICO, LOS ECOS Y LOS FOTONES — viven en
        //  ORBITALIB (AnilloEnergia / EcosAnillo / Fotones): la librería de
        //  signos mágicos del vacío. Este renderer los DELEGA 1:1.
        // ------------------------------------------------------------------

        // ------------------------------------------------------------------
        //  7. RAYOS ELÉCTRICOS — el lightningParticles del script
        // ------------------------------------------------------------------

        private static void DrawElectricBolts(Vector2 center, float r, float time, int seed, int flick)
        {
            int boltFlick = flick / 2;   // ~6 Hz (viven frenéticos)

            // (v6.18: los RAYOS INTERIORES del núcleo fueron QUITADOS — el
            //  centro de la bola negra queda LIMPIO, como en el agujero de
            //  la Bruma. Petición del usuario. Solo quedan los que ESCAPAN.)

            // 7.2 — DOS RAYOS EMERGIENDO DEL ANILLO hacia afuera
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
        //     v6.37 — delega en OrbitaLib (la librería de los anillos
        //     rúnicos): ni un número cambiado. La tabla C (8 runas de
        //     CIRCUITO ENERGÉTICO) es PROPIA → se pasa como `alphabet`.
        //     (Fuera del mapa del refactor, pero el cuerpo era IDÉNTICO
        //     al del Cósmico Ascendido → delegación 1:1 segura.)
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
