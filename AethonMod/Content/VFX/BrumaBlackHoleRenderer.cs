using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;
using AethonMod.Content.Effects.Bruma;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// BrumaBlackHoleRenderer — v6.17 — EL AGUJERO NEGRO DE LA BRUMA, 100% CÓDIGO.
    ///
    /// "crea un 3er agujero con todo lo aprendido y librerías creadas en el
    /// proyecto, todo por código" (petición del usuario) — este agujero es
    /// la DEMOSTRACIÓN de la LIBRERÍA DE BRUMA (Content/Effects/Bruma):
    /// humo, niebla y bruma PROCEDURAL con calidad a CUALQUIER escala.
    ///
    /// IDENTIDAD (única entre los 7 agujeros del mod): un vacío GELIDO
    /// envuelto en HUMO NEBULAR FRIO — teal/cian/violeta/azul profundo,
    /// nada de fuego. La materia no arde al caer: se DISUELVE en bruma.
    ///
    /// QUÉ USA DE LA LIBRERÍA:
    ///   · BrumaFX.Cloud     → el HALO: masas enormes de bruma fría girando
    ///                          lento alrededor del conjunto.
    ///   · BrumaFX.Puff      → los FUMARELITOS del anillo (puffs pequeños
    ///                          orbitando: el "disco" aquí es HUMO).
    ///   · BrumaFX.Tendril   → las VOLUTAS espiralando hacia el núcleo (la
    ///                          materia devorada se disuelve en humo).
    ///   · BrumaFX.Column    → las chimeneas POLARES de bruma que escapan
    ///                          por el eje del vórtice.
    ///   · BrumaNoise        → la densidad VIVE por fBm (el ruido decide).
    ///
    /// Las capas (orden de pintado):
    ///   0. AURA OSCURA fría (alfa) — el vacío absorbe la luz.
    ///   1. HALO de bruma (Cloud ×2: teal profundo + violeta).
    ///   2. ANILLO DE HUMO (mitad TRASERA) — fumarelitos por Puff.
    ///   3. VOLUTAS espiralando al núcleo (Tendril ×3, detrás del disco).
    ///   4. NÚCLEO negro absoluto + rim teal del horizonte congelado.
    ///   5. ANILLO DE HUMO (mitad DELANTERA, más brillante).
    ///   6. ANILLO DE FOTONES + corredores cian-blancos.
    ///   7. CHIMENEAS POLARES de bruma (Column) + agujas frías.
    ///   8. ESCARCHA — motas blanco-cian derivando radialmente.
    ///   9. ONDAS DE DISTORSIÓN.
    ///  10. AURA FINAL fría pulsante.
    ///
    /// CONTRATO DE BATCH (v6.10): Draw() exige el SpriteBatch CERRADO y lo
    /// deja CERRADO. El humo se dibuja en el lote ADITIVO (bruma LUMINOSA).
    /// </summary>
    public static class BrumaBlackHoleRenderer
    {
        // ==================================================================
        //  PARÁMETROS
        // ==================================================================

        /// <summary>Radio de la esfera negra en px a escala 1 — GIGANTE.</summary>
        public const float SpherePx = 48f;

        /// <summary>El anillo de humo (más redondo que un disco: es bruma).</summary>
        private const float RingA = 1.75f;      // semieje mayor (×R)
        private const float RingB = 1.25f;      // semieje menor (×R)
        private const float RingTilt = -0.33f;  // rad — inclinación

        /// <summary>Flujo de los fumarelitos por el anillo (rad/s).</summary>
        private const float SmokeFlow = 0.38f;

        /// <summary>Fumarelitos por mitad del anillo.</summary>
        private const int SmokeletsPerHalf = 16;

        // --- las volutas que caen al núcleo ---
        private const int TendrilCount = 3;
        private const float TendrilStart = 2.85f;   // ×R — donde nace la voluta
        private const float TendrilSweep = 2.1f;    // rad de barrido espiral

        // --- ondas de distorsión ---
        private const float WaveCycle = 2.7f;
        private const int WaveCount = 2;

        // ==================================================================
        //  PALETA — la estirpe GELIDA: teal / cian / violeta / azul profundo
        // ==================================================================

        private static readonly Color SmokeTeal = new(30, 140, 160);    // masa principal
        private static readonly Color SmokeCyan = new(90, 210, 235);    // bordes iluminados
        private static readonly Color SmokeViolet = new(70, 60, 190);   // bruma arcana
        private static readonly Color DeepBlue = new(25, 45, 140);      // profundidad
        private static readonly Color PhotonWhite = new(225, 250, 255); // cian-blanco caliente
        private static readonly Color RimTeal = new(60, 200, 220);      // horizonte congelado
        private static readonly Color FrostMote = new(185, 235, 250);   // escarcha

        // ==================================================================
        //  PINCELES (los genéricos + los de la LIBRERÍA DE BRUMA)
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

        /// <summary>Punto sobre la elipse del anillo de humo (param t en rad).</summary>
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
                float breathe = 1f + 0.012f * (float)Math.Sin(time * 1.2f);
                float rr = r * breathe;

                // ============ 0. AURA OSCURA FRÍA (alfa) ============
                BeginAlpha();
                Quad(Glow, center, new Vector2(7.2f * rr, 7.2f * rr), 0f,
                    new Color(3, 10, 16, 170));
                Main.spriteBatch.End();

                // ============ 1..10: TODO LO BRILLANTE (aditivo) ============
                // El humo LUMINOSO se dibuja en el lote aditivo: la bruma
                // mágica SUMA luz (el llamador eligió el modo; el pincel de
                // la librería es neutro).
                BeginAdditive();

                // --- 1. EL HALO: dos Cloud de la librería (teal + violeta) ---
                // Masas ENORMES y tenues girando lento: la envoltura nebular.
                BrumaFX.Cloud(center, 2.6f * rr, SmokeTeal, seed + 11, time,
                    puffs: 5, alpha: 0.30f);
                BrumaFX.Cloud(center, 3.3f * rr, SmokeViolet, seed + 47, time * 0.8f,
                    puffs: 4, alpha: 0.20f);

                // --- 2. ANILLO DE HUMO — mitad TRASERA ---
                DrawSmokeRing(center, rr, time, seed, front: false);

                // --- 3. VOLUTAS espiralando hacia el núcleo ---
                DrawFallingTendrils(center, r, time, seed);

                // --- 4. NÚCLEO + rim del horizonte congelado ---
                Main.spriteBatch.End();
                BeginAlpha();
                Quad(Disk, center, new Vector2(2.28f * r, 2.28f * r), 0f, Color.White);
                Main.spriteBatch.End();
                BeginAdditive();
                RingQuad(center, 1.02f * r, time * 0.13f,
                    Tint(RimTeal, 0.36f + 0.12f * (float)Math.Sin(time * 1.6f)));

                // --- 5. ANILLO DE HUMO — mitad DELANTERA (más brillante) ---
                DrawSmokeRing(center, rr, time, seed, front: true);

                // --- 6. ANILLO DE FOTONES + corredores ---
                RingQuad(center, 1.10f * r, time * 0.2f,
                    Tint(SmokeCyan, 0.20f + 0.08f * (float)Math.Sin(time * 2.1f)));
                DrawPhotonRunners(center, rr, time, seed);

                // --- 7. CHIMENEAS POLARES + agujas frías ---
                DrawPolarChimneys(center, rr, time, seed);
                DrawPolarFlares(center, rr, time);

                // --- 8. ESCARCHA — motas blanco-cian radiales ---
                DrawFrostMotes(center, r, time, seed);

                // --- 9. ONDAS DE DISTORSIÓN ---
                DrawDistortionWaves(center, r, time, seed);

                // --- 10. AURA FINAL fría pulsante ---
                float aura = 0.85f + 0.15f * (float)Math.Sin(time * 1.9f);
                Quad(Glow, center, new Vector2(5.8f * rr, 5.8f * rr), 0f,
                    Tint(SmokeTeal, 0.20f * aura));

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
        //  2/5. EL ANILLO DE HUMO — fumarelitos de la LIBRERÍA orbitando
        // ------------------------------------------------------------------

        private static void DrawSmokeRing(Vector2 center, float rr, float time, int seed,
            bool front)
        {
            float t0 = front ? 0f : MathHelper.Pi;
            float span = MathHelper.Pi;
            float bright = front ? 1.15f : 0.85f;
            float flow = time * SmokeFlow;

            for (int s = 0; s < SmokeletsPerHalf; s++)
            {
                // Cada fumarelito nace por hash + deriva (orbitan lento).
                float h = Hash01(seed, 700 + s, 3);
                float t = t0 + ((h + flow / MathHelper.TwoPi) % 1f) * span;

                Vector2 pos = Ellipse(center, rr, t);

                // Tamaño variable (el humo se amontona en grumos).
                float sizeR = (0.16f + 0.22f * Hash01(seed, 701 + s, 7)) * rr;

                // La densidad VIVE: fBm por posición y tiempo — el ruido
                // decide qué fumarelito está denso ahora (regla del morph).
                float n = BrumaNoise.Fbm(pos.X * 0.012f, pos.Y * 0.012f + time * 0.10f,
                    seed + 5, 3);
                float a = (0.28f + 0.42f * n) * bright;

                // Color: la masa teal; los que están sobre el filo interno
                // se iluminan cian (la luz del horizonte atraviesa lo fino).
                Color c = n > 0.55f ? SmokeCyan : SmokeTeal;

                // EL FUMARELITO: un Puff de la librería (textura fBm horneada
                // en runtime + sub-blobs que respiran desfasados).
                BrumaFX.Puff(pos, sizeR, c, seed + s * 61, time,
                    MathHelper.Clamp(a, 0.05f, 0.65f), quality: 0.45f);

                // Un núcleo de brillo interno para definición del anillo.
                Vector2 ahead = Ellipse(center, rr, t + 0.08f);
                Vector2 seg = ahead - pos;
                float len = seg.Length();
                if (len > 0.5f)
                {
                    float rot = (float)Math.Atan2(seg.Y, seg.X);
                    Vector2 mid = (pos + ahead) * 0.5f;
                    Capsule(mid, len, sizeR * 0.9f, rot,
                        Tint(c, 0.30f * a));
                }
            }
        }

        // ------------------------------------------------------------------
        //  3. VOLUTAS — Tendril de la librería espiralando al núcleo
        // ------------------------------------------------------------------

        private static void DrawFallingTendrils(Vector2 center, float r, float time, int seed)
        {
            for (int k = 0; k < TendrilCount; k++)
            {
                // Cada voluta gira a su velocidad (la rotación de la caída).
                float baseAng = time * (0.22f + 0.08f * k) + k * 2.1f;

                // LA RUTA ESPIRAL: nace lejos (TendrilStart·R) y CAE al
                // núcleo (0.45·R) barriendo TendrilSweep rad — 8 puntos.
                var path = new Vector2[8];
                for (int p = 0; p < 8; p++)
                {
                    float f = p / 7f;
                    float ang = baseAng + f * TendrilSweep;
                    float rad = (TendrilStart - (TendrilStart - 0.45f) * f) * r;
                    path[p] = center + new Vector2(
                        (float)Math.Cos(ang) * rad * RingA / 1.75f,
                        (float)Math.Sin(ang) * rad * RingB / 1.25f);
                }

                // La voluta se DISUELVE al acercarse al núcleo (fade=1: la
                // masa muere en el final de la ruta — devorada).
                Color c = k % 2 == 0 ? SmokeTeal : SmokeViolet;
                float n = BrumaNoise.Fbm(k * 3.7f, time * 0.15f, seed + k, 3);
                BrumaFX.Tendril(path, 0.55f * r, c, seed + k * 97, time,
                    alpha: 0.42f + 0.20f * n, fade: 1f);
            }
        }

        // ------------------------------------------------------------------
        //  6b. CORREDORES DE FOTONES — chispas cian-blanco orbitando
        // ------------------------------------------------------------------

        private static void DrawPhotonRunners(Vector2 center, float rr, float time, int seed)
        {
            for (int i = 0; i < 3; i++)
            {
                float t = time * (0.9f + 0.25f * i) + i * 2.1f;
                Vector2 pos = Ellipse(center, rr, t);
                float twinkle = 0.65f + 0.35f * (float)Math.Sin(time * 8f + i * 2.3f);

                // Estela corta detrás del fotón.
                Vector2 behind = Ellipse(center, rr, t - 0.22f);
                Vector2 seg = pos - behind;
                float len = seg.Length();
                if (len > 0.5f)
                {
                    float rot = (float)Math.Atan2(seg.Y, seg.X);
                    Vector2 mid = (pos + behind) * 0.5f;
                    Capsule(mid, len, 0.13f * rr, rot, Tint(SmokeCyan, 0.40f * twinkle));
                }

                // halo cian + núcleo blanco-cian
                Quad(Glow, pos, new Vector2(0.95f * rr, 0.95f * rr), 0f,
                    Tint(SmokeCyan, 0.35f * twinkle));
                Quad(Glow, pos, new Vector2(0.40f * rr, 0.40f * rr), 0f,
                    Tint(PhotonWhite, 0.85f * twinkle));
            }
        }

        // ------------------------------------------------------------------
        //  7a. CHIMENEAS POLARES — Column de la librería escapando por el eje
        // ------------------------------------------------------------------

        private static void DrawPolarChimneys(Vector2 center, float rr, float time, int seed)
        {
            // Eje menor de la elipse (los "polos" del vórtice).
            Vector2 pole = new Vector2(
                -(float)Math.Sin(RingTilt + MathHelper.PiOver2),
                (float)Math.Cos(RingTilt + MathHelper.PiOver2));

            for (int side = 0; side < 2; side++)
            {
                Vector2 dir = side == 0 ? pole : -pole;
                float baseOff = RingB * rr * 0.9f;
                Vector2 basePos = center + dir * baseOff;

                // La chimenea: bruma ESCAPANDO por el polo (Column de la
                // librería — nace, crece al ascender, se erosiona al morir).
                // OJO: la columna asciende en −Y de pantalla; la rotamos al
                // eje del polo componiendo la posición a mano.
                int steps = 5;
                for (int i = 0; i < steps; i++)
                {
                    float phase = (time * 0.45f + i / (float)steps) % 1f;
                    float riseR = (0.10f + 0.30f * phase) * rr;
                    float along = baseOff + phase * 1.6f * rr;
                    Vector2 pos = center + dir * along;
                    float n = BrumaNoise.Fbm(i * 2.3f, time * 0.2f + side, seed + side * 31, 3);
                    float a = 0.30f * MathF.Sin(phase * MathHelper.Pi) * (0.5f + 0.5f * n);
                    BrumaFX.Puff(pos, riseR, side == 0 ? SmokeCyan : SmokeTeal,
                        seed + side * 53 + i * 17, time,
                        MathHelper.Clamp(a, 0.04f, 0.5f), quality: 0.4f,
                        velocity: dir * phase * rr * 0.6f);
                }
            }
        }

        // ------------------------------------------------------------------
        //  7b. AGUJAS POLARES frías
        // ------------------------------------------------------------------

        private static void DrawPolarFlares(Vector2 center, float rr, float time)
        {
            Vector2 pole = new Vector2(
                -(float)Math.Sin(RingTilt + MathHelper.PiOver2),
                (float)Math.Cos(RingTilt + MathHelper.PiOver2));
            float pulse = 0.7f + 0.3f * (float)Math.Sin(time * 1.8f);

            for (int side = 0; side < 2; side++)
            {
                Vector2 dir = side == 0 ? pole : -pole;
                float rot = (float)Math.Atan2(dir.Y, dir.X);

                float baseOff = RingB * rr * 0.85f;
                for (int k = 0; k < 3; k++)
                {
                    float f0 = k / 3f, f1 = (k + 1) / 3f;
                    float midF = (f0 + f1) * 0.5f;
                    Vector2 a = center + dir * (baseOff + f0 * 1.7f * rr);
                    Vector2 b = center + dir * (baseOff + f1 * 1.7f * rr);
                    Vector2 mid = (a + b) * 0.5f;
                    float len = (b - a).Length();
                    float w = (0.28f - 0.20f * midF) * rr;
                    Color c = Color.Lerp(PhotonWhite, SmokeCyan, midF);
                    Capsule(mid, len, w, rot, Tint(c, 0.28f * pulse * (1f - midF * 0.5f)));
                }

                Vector2 tip = center + dir * (baseOff + 1.75f * rr);
                Quad(Glow, tip, new Vector2(0.45f * rr, 0.45f * rr), 0f,
                    Tint(PhotonWhite, 0.40f * pulse));
            }
        }

        // ------------------------------------------------------------------
        //  8. ESCARCHA — motas blanco-cian derivando radialmente afuera
        // ------------------------------------------------------------------

        private static void DrawFrostMotes(Vector2 center, float r, float time, int seed)
        {
            for (int i = 0; i < 10; i++)
            {
                float h = Hash01(seed, 900 + i, 23);
                float life = (time * 0.12f + h) % 1f;
                float ang = Hash01(seed, 901 + i, 31) * MathHelper.TwoPi +
                            time * 0.05f * (i % 2 == 0 ? 1f : -1f);
                float dist = (1.7f + life * 2.6f) * r;
                Vector2 pos = center + new Vector2(
                    (float)Math.Cos(ang) * dist, (float)Math.Sin(ang) * dist);

                float size = (0.12f + 0.12f * h) * r * 2f;
                float alpha = (float)Math.Sin(life * Math.PI) * (0.55f + 0.45f * h);

                // Cian / teal / violeta / blanca (la escarcha del vacío).
                Color c = h < 0.40f ? SmokeCyan
                        : h < 0.70f ? SmokeTeal
                        : h < 0.90f ? SmokeViolet
                        : PhotonWhite;

                Quad(Glow, pos, new Vector2(size, size), 0f, Tint(c, alpha));
            }
        }

        // ------------------------------------------------------------------
        //  9. ONDAS DE DISTORSIÓN — el espacio-tiempo late
        // ------------------------------------------------------------------

        private static void DrawDistortionWaves(Vector2 center, float r, float time, int seed)
        {
            for (int w = 0; w < WaveCount; w++)
            {
                float phase = ((time / WaveCycle) + w / (float)WaveCount) % 1f;
                float radius = (1.15f + phase * 1.95f) * r;
                float fade = (1f - phase) * (1f - phase);
                RingQuad(center, radius, phase * 2.4f + w,
                    Tint(new Color(200, 245, 255), 0.28f * fade));
            }
        }
    }
}
