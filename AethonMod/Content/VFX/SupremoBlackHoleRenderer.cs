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
    /// SupremoBlackHoleRenderer — v6.23 — EL AGUJERO NEGRO SUPREMO, 100% CÓDIGO.
    ///
    /// v6.23 — EL TRIPLE CÍRCULO DE RUNAS (petición del usuario): junto
    /// a los dos anillos rúnicos clásicos nace el TERCERO — el círculo
    /// BLANCO-INCANDESCENTE íntimo (6 runas @2.02·R, CW rápido) entre el
    /// anillo de fotones y el dorado. `DrawRune` ahora recibe SU `count`
    /// y SU `orbit` por llamada: cada corona gira a lo suyo.
    ///
    /// EL 5to AGUJERO DEFINITIVO — LA FUSIÓN DE LOS 4 (petición del
    /// usuario): "el agujero negro supremo, la combinación de los 4
    /// agujeros, mejorado, potenciado y mejor animado. El agujero más
    /// espectacular del mod". Cada uno aporta su HERENCIA:
    ///
    ///   · del UMBRAL    → el DISCO DOPPLER oblicuo con ESTRÍAS PINTADAS
    ///                     que van del blanco-oro (el lado que se acerca)
    ///                     al carmesí profundo (el que se aleja).
    ///   · del CÓSMICO   → el ANILLO DE BANDAS del shader: 20 zonas de
    ///                     brillo viajando a 5 rad/s (glow = sin(θ·20+t·5)).
    ///   · del OLVIDO    → los BRAZOS ESPIRALES del vórtice con flujo
    ///                     animado (puntos que espiralan hacia adentro)
    ///                     y los corredores de fotones.
    ///   · de la BRUMA   → el HALO de nubes (BrumaFX.Cloud) y las VOLUTAS
    ///                     cayendo al núcleo (BrumaFX.Tendril).
    ///   · DE NUEVO (v6.18) → LA CORONA DE RAYOS: StormLib — 3 ARCOS
    ///     eléctricos vibrando alrededor del horizonte (~10 Hz) y 3 RAYOS
    ///     escapando del anillo (2 carmesí + 1 dorado), doble tira
    ///     cuerpo/núcleo con ramas. La librería nueva del proyecto.
    ///
    /// EL NÚCLEO SIGUE LIMPIO (v6.18): el centro de la bola negra queda
    /// NEGRO ABSOLUTO, sin partículas interiores — y se REPINTA al final
    /// para devorar cualquier derrame (el vacío siempre gana).
    ///
    /// PALETA SUPREMA (unifica las 4 identidades — REGIA, no arcoíris):
    /// blanco-incandescente (255,248,235) → oro (255,190,80) → naranja-
    /// carmesí (255,90,40) → violeta (150,80,255) → azul profundo
    /// (60,80,220). EL ORO/CARMESÍ DOMINA, el violeta/azul ACENTÚA. El
    /// rim del horizonte es DORADO.
    ///
    /// LAS CAPAS (orden de pintado):
    ///   1.  AURA OSCURA (alfa) — el vacío absorbe la luz.
    ///   2.  NEBULOSAS doradas/violetas + polvo ambiental.
    ///   3.  HALO DE HUMO — 2 BrumaFX.Cloud (oro cálido + violeta).
    ///   4.  DISCO DOPPLER oblicuo — mitad TRASERA con estrías pintadas.
    ///   4b. ECO TENUE del anillo de bandas (mitad trasera, detrás).
    ///   5.  NÚCLEO negro absoluto + rim DORADO pulsante.
    ///   6.  ANILLO DE BANDAS — mitad DELANTERA (20 bandas, oro→carmesí).
    ///   7.  BRAZOS ESPIRALES (3, violeta-azul) con flujo hacia adentro.
    ///   8.  ANILLO DE FOTONES + corredores.
    ///   9.  ⚡ LIGHTNINGCORE: 3 ARCOS del horizonte + 3 RAYOS fugitivos.
    ///   10. TRIPLE CÍRCULO DE RUNAS: 8 doradas CW + 6 azul-violeta CCW
    ///       + 6 BLANCAS íntimas CW rápido (v6.23 — el tercer anillo).
    ///   11. JETS POLARES (oro arriba, violeta abajo) + rayo interno.
    ///   12. VOLUTAS cayendo (2 BrumaFX.Tendril — la materia se disuelve).
    ///   13. ONDAS DE DISTORSIÓN ×3.
    ///   14. REPINTADO del disco negro (el vacío devora el derrame).
    ///   15. AURA FINAL GRANDE pulsante (oro + contrarroto violeta).
    ///
    /// ANIMACIÓN MEJORADA (v6.18): TRES VELOCIDADES DE TIEMPO — lento
    /// (humo/runas/nebulosas), medio (disco/espirales/ondas) y rápido
    /// (rayos/fotones/bandas). Todo con el `time` del llamador
    /// (Main.GlobalTimeWrappedHourly): determinista, cero estado, cero red.
    ///
    /// CONTRATO DE BATCH (v6.10, a prueba de balas): Draw() exige el
    /// SpriteBatch CERRADO y lo deja CERRADO.
    /// </summary>
    public static class SupremoBlackHoleRenderer
    {
        // ==================================================================
        //  PARÁMETROS
        // ==================================================================

        /// <summary>Radio de la esfera negra en px a escala 1 — EL MÁS GRANDE DEL MOD.</summary>
        public const float SpherePx = 55f;

        // --- EL DISCO DOPPLER (herencia UMBRAL): oblicuo y fino ---
        private const float DiskA = 2.50f;      // semieje mayor (×R)
        private const float DiskB = 0.92f;      // semieje menor (×R) — oblicuo
        private const float DiskTilt = -0.22f;  // rad — inclinación

        /// <summary>Ángulo del BEAMING DOPPLER: aquí el extremo que se
        /// ACERCA (blanco-oro) vive abajo-izquierda; el opuesto se aleja
        /// (carmesí profundo).</summary>
        private const float DopplerAngle = 3.05f;

        /// <summary>Deriva de las estrías por el disco (rad/s).</summary>
        private const float StreakFlow = 0.44f;

        /// <summary>Estrías pintadas por mitad del disco.</summary>
        private const int StreaksPerHalf = 24;

        /// <summary>Regeneración de la turbulencia (Hz).</summary>
        private const float FlickHz = 12f;

        // --- EL ANILLO DE BANDAS (herencia CÓSMICO): la fórmula del shader ---
        private const float BandA = 1.95f;      // semieje mayor (×R)
        private const float BandB = 1.24f;      // semieje menor (×R)
        private const float BandTilt = -0.38f;  // rad — cruza el disco en X

        /// <summary>LA FÓRMULA DEL SHADER: glow = sin(θ·20 + t·5)·0.5+0.5.</summary>
        private const float BandFreq = 20f;
        private const float BandSpeed = 5f;

        /// <summary>Rotación del anillo (20°/s del script Unity original).</summary>
        private const float BandSpin = 0.349f;

        /// <summary>Segmentos de cada mitad del anillo de bandas.</summary>
        private const int BandSegments = 44;

        // --- LOS BRAZOS ESPIRALES (herencia OLVIDO) ---
        private const int ArmCount = 3;
        private const int ArmSteps = 13;

        // --- EL TRIPLE CÍRCULO DE RUNAS (v6.23: el blanco íntimo NUEVO) ---
        private const int GoldRuneCount = 8;     // doradas, CW
        private const float GoldRuneRadius = 2.62f;   // ×R
        private const float GoldRuneOrbit = 0.10f;    // rad/s
        private const int VioletRuneCount = 6;   // azul-violeta, CCW
        private const float VioletRuneRadius = 3.30f; // ×R — MÁS AFUERA
        private const float VioletRuneOrbit = -0.075f; // rad/s — CONTRARROTO
        // --- v6.23: EL TERCER CÍRCULO — blanco-incandescente, el MÁS ÍNTIMO
        //     (entre el anillo de fotones y el dorado) girando rápido ---
        private const int WhiteRuneCount = 6;      // blancas, CW rápido
        private const float WhiteRuneRadius = 2.02f;  // ×R — MÁS ADENTRO
        private const float WhiteRuneOrbit = 0.16f;   // rad/s — el círculo vivo

        // --- LOS RAYOS (LA ESTRELLA — StormLib) ---
        private const float BoltHz = 10f;        // ~10 Hz de parpadeo vivo

        // --- las volutas que caen al núcleo (herencia BRUMA) ---
        private const int TendrilCount = 2;
        private const float TendrilStart = 2.95f;  // ×R — donde nace
        private const float TendrilSweep = 2.2f;   // rad de barrido espiral

        // --- ondas de distorsión ---
        private const float WaveCycle = 2.5f;
        private const int WaveCount = 3;

        // ==================================================================
        //  PALETA SUPREMA — oro/carmesí DOMINA, violeta/azul ACENTÚA
        // ==================================================================

        private static readonly Color WhiteIncan = new(255, 248, 235); // blanco-incandescente
        private static readonly Color SupGold = new(255, 190, 80);     // ORO — el rim y las bandas
        private static readonly Color Crimson = new(255, 90, 40);      // naranja-carmesí
        private static readonly Color DeepCrimson = new(140, 20, 30);  // carmesí profundo
        private static readonly Color SupViolet = new(150, 80, 255);   // violeta
        private static readonly Color DeepBlue = new(60, 80, 220);     // azul profundo
        private static readonly Color RuneGold = new(255, 180, 70);    // cuerpo de runa dorada
        private static readonly Color RuneGoldTip = new(255, 235, 175);// punta pálida dorada
        private static readonly Color RuneViolet = new(110, 130, 255); // cuerpo de runa azul-violeta
        private static readonly Color RuneVioletTip = new(205, 220, 255); // punta pálida fría
        private static readonly Color RuneWhite = new(255, 245, 220);    // cuerpo de runa blanca
        private static readonly Color RuneWhiteTip = new(255, 252, 240); // punta incandescente
        private static readonly Color NebGold = new(190, 130, 40);     // nebulosa dorada
        private static readonly Color NebViolet = new(80, 50, 170);    // nebulosa violeta
        private static readonly Color WarmWhite = new(255, 225, 175);  // blanco cálido

        // ==================================================================
        //  PINCELES (los tres genéricos de la biblioteca VFX)
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

        /// <summary>Punto sobre la elipse del DISCO DOPPLER (param t en rad).</summary>
        private static Vector2 DiskPoint(Vector2 c, float r, float t)
            => VFXCore.Ellipse(c, DiskA * r, DiskB * r, DiskTilt, t);

        /// <summary>Punto sobre la elipse del ANILLO DE BANDAS (param t en rad).</summary>
        private static Vector2 BandPoint(Vector2 c, float r, float t)
            => VFXCore.Ellipse(c, BandA * r, BandB * r, BandTilt, t);

        /// <summary>
        /// EL BEAMING DOPPLER del disco (herencia Umbral): cúbico — el lado
        /// que se ACERCA arde a blanco-oro, el que se ALEJA muere en carmesí.
        /// </summary>
        private static float Doppler(float t)
        {
            float d = 0.5f + 0.5f * (float)Math.Cos(t - DopplerAngle);
            return d * d * d;
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
                // ============================================================
                //  LAS TRES VELOCIDADES DE TIEMPO (v6.18 — animación mejorada)
                //  · tSlow: humo, runas, nebulosas — la caldera REGIA.
                //  · tMid:  disco, espirales, ondas — la materia en caída.
                //  · tFast: rayos, fotones, bandas — la furia eléctrica.
                // ============================================================
                float tSlow = time * 0.55f;
                float tMid = time;
                float tFast = time * 1.55f;

                int flick = (int)(tMid * FlickHz);
                int boltFlick = StormLib.FlickTick(time, BoltHz);   // ~10 Hz

                float breathe = 1f + 0.012f * (float)Math.Sin(tMid * 1.15f);
                float rr = r * breathe;

                // ============ 1. AURA OSCURA (alfa) — absorbe la luz ============
                BeginAlpha();
                Quad(Glow, center, new Vector2(7.6f * rr, 7.6f * rr), 0f,
                    new Color(10, 5, 2, 178));
                Main.spriteBatch.End();

                // ============ 2..15: TODO LO BRILLANTE (aditivo) ============
                BeginAdditive();

                // --- 2. NEBULOSAS doradas/violetas + polvo ambiental ---
                DrawNebulas(center, rr, tSlow, seed);

                // --- 3. EL HALO DE HUMO — 2 Cloud de la LIBRERÍA DE BRUMA ---
                // (giran LENTO: una dorada cálida, una violeta — el aliento
                // del supremo abraza el conjunto entero).
                BrumaFX.Cloud(center, 2.7f * rr, NebGold, seed + 11, tSlow * 0.8f,
                    puffs: 5, alpha: 0.26f);
                BrumaFX.Cloud(center, 3.4f * rr, SupViolet, seed + 47, tSlow * 0.6f,
                    puffs: 4, alpha: 0.17f);

                // --- 4. EL DISCO DOPPLER — mitad TRASERA (estrías pintadas) ---
                DrawDopplerDisk(center, rr, tMid, seed, flick, front: false);

                // --- 4b. ECO TENUE del anillo de bandas (mitad trasera) ---
                DrawBandRing(center, rr, tFast, seed, flick, front: false);

                // --- 5. NÚCLEO negro absoluto + rim DORADO pulsante ---
                Main.spriteBatch.End();
                BeginAlpha();
                Quad(Disk, center, new Vector2(2.28f * r, 2.28f * r), 0f, Color.White);
                Main.spriteBatch.End();
                BeginAdditive();
                RingQuad(center, 1.02f * r, tMid * 0.13f,
                    Tint(SupGold, 0.42f + 0.14f * (float)Math.Sin(tMid * 1.6f)));

                // --- 6. EL ANILLO DE BANDAS — mitad DELANTERA (la fórmula) ---
                DrawBandRing(center, rr, tFast, seed, flick, front: true);

                // --- 7. BRAZOS ESPIRALES con flujo hacia adentro ---
                DrawSpiralArms(center, rr, tMid, seed);

                // --- 8. ANILLO DE FOTONES + corredores ---
                RingQuad(center, 1.10f * r, tFast * 0.2f,
                    Tint(WarmWhite, 0.20f + 0.08f * (float)Math.Sin(tFast * 2.1f)));
                DrawPhotonRunners(center, rr, tFast, seed);

                // --- 9. ⚡ LIGHTNINGCORE — LA CORONA DE DESCARGA ---
                DrawHorizonArcs(center, r, time, seed, boltFlick);
                DrawEscapingBolts(center, rr, tFast, seed, boltFlick);

                // --- 10. TRIPLE CÍRCULO DE RUNAS (CW + CCW + CW íntimo) ---
                DrawRuneCircles(center, r, tSlow, seed);

                // --- 11. JETS POLARES (oro arriba, violeta abajo) ---
                DrawPolarJets(center, rr, tMid, seed, boltFlick);

                // --- 12. VOLUTAS cayendo — la materia se disuelve ---
                DrawFallingTendrils(center, r, tSlow, seed);

                // --- 13. ONDAS DE DISTORSIÓN ×3 ---
                DrawDistortionWaves(center, r, tMid, seed);

                // --- 14. EL VACÍO VUELVE A DEVORAR: repintado del disco ---
                // (cualquier derrame aditivo sobre el horizonte se borra y
                // el centro queda NEGRO ABSOLUTO — el supremo se lo come).
                Main.spriteBatch.End();
                BeginAlpha();
                Quad(Disk, center, new Vector2(2.28f * r, 2.28f * r), 0f, Color.White);
                Main.spriteBatch.End();
                BeginAdditive();
                // El filo DORADO del horizonte, respirando (la última luz).
                RingQuad(center, 1.02f * r, tMid * 0.13f,
                    Tint(SupGold, 0.30f + 0.10f * (float)Math.Sin(tMid * 1.6f)));

                // --- 15. AURA FINAL GRANDE pulsante ---
                float aura = 0.85f + 0.15f * (float)Math.Sin(tFast * 1.4f);
                Quad(Glow, center, new Vector2(6.6f * rr, 6.6f * rr), 0f,
                    Tint(SupGold, 0.20f * aura));
                // El CONTRARROTO violeta: el eco arcano de la fusión.
                RingQuad(center, 3.1f * rr, -tMid * 0.08f,
                    Tint(SupViolet, 0.10f * aura));

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
        //  2. NEBULOSAS + POLVO AMBIENTAL — doradas y violetas
        // ------------------------------------------------------------------

        private static void DrawNebulas(Vector2 center, float rr, float time, int seed)
        {
            // SEIS velos ENORMES y tenues: el oro alterna con el violeta
            // (la unión de las dos estirpes fundidas en el supremo).
            for (int i = 0; i < 6; i++)
            {
                float h = Hash01(seed, 501 + i, 17);
                float dir = i % 2 == 0 ? 1f : -1f;
                float ang = h * MathHelper.TwoPi + time * 0.05f * dir;
                float dist = (2.8f + 0.8f * Hash01(seed, 502 + i, 29)) * rr;
                Vector2 pos = center + new Vector2(
                    (float)Math.Cos(ang) * dist, (float)Math.Sin(ang) * dist * 0.8f);
                float size = (2.0f + 1.1f * Hash01(seed, 503 + i, 41)) * rr;
                Color c = i % 2 == 0 ? NebGold : NebViolet;
                float pulse = 0.75f + 0.25f * (float)Math.Sin(time * 0.7f + i * 1.9f);
                Quad(Glow, pos, new Vector2(size, size), ang,
                    Tint(c, (i % 2 == 0 ? 0.20f : 0.14f) * pulse));
            }

            // Polvo ambiental: brasas doradas y chispas violetas pulsando.
            for (int i = 0; i < 14; i++)
            {
                float h = Hash01(seed, 600 + i, 13);
                float ang = h * MathHelper.TwoPi + time * 0.03f * (i % 2 == 0 ? 1f : -1f);
                float dist = (2.2f + 3.1f * Hash01(seed, 601 + i, 19)) * rr * 0.55f;
                Vector2 pos = center + new Vector2(
                    (float)Math.Cos(ang) * dist, (float)Math.Sin(ang) * dist);
                float pulse = 0.5f + 0.5f * (float)Math.Sin(time * 1.5f + i * 2.4f);
                Color c = h < 0.40f ? new Color(200, 130, 35)   // brasa dorada
                        : h < 0.75f ? new Color(190, 60, 30)    // brasa carmesí
                        : new Color(110, 70, 220);              // chispa violeta
                float size = (0.10f + 0.10f * h) * rr;
                Quad(Glow, pos, new Vector2(size, size), 0f, Tint(c, 0.45f * pulse));
            }
        }

        // ------------------------------------------------------------------
        //  4. EL DISCO DOPPLER — estrías pintadas (herencia UMBRAL),
        //      blanco-oro (acerca) → carmesí profundo (aleja)
        // ------------------------------------------------------------------

        private static void DrawDopplerDisk(Vector2 center, float rr, float time, int seed,
            int flick, bool front)
        {
            // Solo se pinta la MITAD TRASERA: la delantera la cruza el
            // anillo de bandas (la doble geometría del supremo).
            float t0 = MathHelper.Pi;
            float span = MathHelper.Pi;
            float bright = front ? 1.25f : 0.58f;

            // Las estrías DERIVAN por el disco (la pintura fluye).
            float flow = time * StreakFlow;

            for (int s = 0; s < StreaksPerHalf; s++)
            {
                // Nacimiento por hash + deriva: cada estría nace y VIAJA.
                float h0 = Hash01(seed, 700 + s, flick / 2);
                float a0 = t0 + ((h0 + flow / MathHelper.TwoPi) % 1f) * span;

                // JITTER RADIAL: cada estría respira a su radio — el plasma
                // distorsionado, NO una elipse geométrica perfecta.
                float radialJit = 0.94f + 0.14f * Hash01(seed, 703 + s, 5);

                // DOPPLER en el punto medio de la estría.
                float midT = a0 + 0.10f;
                float dop = Doppler(midT);

                // LARGO: pinceladas EXTENSAS en el lado caliente.
                float arcLen = (0.22f + 0.50f * Hash01(seed, 701 + s, flick / 2)) *
                               (0.45f + 0.75f * dop);

                // GROSOR: mayor en el lado CALIENTE (el disco engorda ardiendo).
                float wBase = (0.10f + 0.16f * dop) * rr;

                // TURBULENCIA de brillo (la pintura VIVE).
                float turb = 0.70f + 0.30f * Hash01(seed, 702 + s, flick);

                // --- BANDA INTERNA CALIENTE (3 cápsulas por estría) ---
                for (int seg = 0; seg < 3; seg++)
                {
                    float ta = a0 + arcLen * seg / 3f;
                    float tb = a0 + arcLen * (seg + 1) / 3f;
                    Vector2 pa = DiskPoint(center, rr * radialJit, ta);
                    Vector2 pb = DiskPoint(center, rr * radialJit, tb);
                    Vector2 mid = (pa + pb) * 0.5f;
                    Vector2 d = pb - pa;
                    float len = d.Length();
                    if (len < 0.5f) continue;
                    float rot = (float)Math.Atan2(d.Y, d.X);

                    // Color del núcleo por Doppler: BLANCO-ORO (acerca) →
                    // oro → carmesí → CARMESÍ PROFUNDO (aleja).
                    float inten = (0.25f + 0.75f * dop) * turb * bright;
                    Color core;
                    if (dop > 0.62f) core = WhiteIncan;
                    else if (dop > 0.30f) core = SupGold;
                    else if (dop > 0.10f) core = Crimson;
                    else core = DeepCrimson;

                    Capsule(mid, len, wBase * 2.1f, rot,
                        Tint(Color.Lerp(core, Crimson, 0.25f), 0.55f * inten));
                    Capsule(mid, len, wBase, rot, Tint(core, 1.0f * inten));

                    // Punto BLANCO-ORO donde la turbulencia ARDE.
                    if (turb > 0.82f && inten > 0.28f)
                    {
                        Quad(Glow, mid, new Vector2(0.34f * rr, 0.34f * rr), rot,
                            Tint(WhiteIncan, 0.95f * inten * (turb - 0.82f) / 0.18f));
                    }
                }

                // --- BANDA MEDIA: carmesí (a 1.10× del anillo) ---
                {
                    float tm = a0 + arcLen * 0.5f;
                    Vector2 pm = DiskPoint(center, rr * 1.10f, tm);
                    Vector2 pn = DiskPoint(center, rr * 1.10f, tm + arcLen * 0.55f);
                    Vector2 mid = (pm + pn) * 0.5f;
                    Vector2 d = pn - pm;
                    float len = d.Length();
                    if (len > 0.5f)
                    {
                        float rot = (float)Math.Atan2(d.Y, d.X);
                        float inten = (0.30f + 0.55f * dop) * turb * bright;
                        Capsule(mid, len, wBase * 1.5f, rot,
                            Tint(Crimson, 0.48f * inten));
                    }
                }

                // --- BANDA EXTERNA: carmesí desvaneciendo a VIOLETA (a 1.24×)
                //     — el puente de color hacia los brazos espirales. ---
                {
                    float tm = a0 + arcLen * 0.4f;
                    Vector2 pm = DiskPoint(center, rr * 1.24f, tm);
                    Vector2 pn = DiskPoint(center, rr * 1.24f, tm + arcLen * 0.45f);
                    Vector2 mid = (pm + pn) * 0.5f;
                    Vector2 d = pn - pm;
                    float len = d.Length();
                    if (len > 0.5f)
                    {
                        float rot = (float)Math.Atan2(d.Y, d.X);
                        float inten = (0.22f + 0.40f * dop) * turb * bright;
                        Capsule(mid, len, wBase * 1.2f, rot,
                            Tint(Color.Lerp(DeepCrimson, SupViolet, 0.45f), 0.40f * inten));
                    }
                }
            }
        }

        // ------------------------------------------------------------------
        //  4b/6. EL ANILLO DE BANDAS — LA FÓRMULA DEL SHADER CÓSMICO, FIEL:
        //         glow = sin(θ·20 + t·5)·0.5+0.5 — 20 bandas viajando,
        //         en ORO → CARMESÍ
        // ------------------------------------------------------------------

        private static void DrawBandRing(Vector2 center, float rr, float time, int seed,
            int flick, bool front)
        {
            // La mitad delantera (t ∈ 0..π) cruza POR DELANTE de la esfera.
            float t0 = front ? 0f : MathHelper.Pi;
            float span = MathHelper.Pi;
            float bright = front ? 1.30f : 0.42f;

            // El anillo ROTA (20°/s del script Unity).
            float spin = time * BandSpin / 1.55f;   // tFast ya acelera el patrón

            for (int s = 0; s < BandSegments; s++)
            {
                float t = t0 + span * (s + 0.5f) / BandSegments;
                float tSpin = t + spin;   // el patrón de bandas gira con el anillo

                // Posición + tangente de la cápsula.
                Vector2 a = BandPoint(center, rr, t - span / (BandSegments * 2f));
                Vector2 b = BandPoint(center, rr, t + span / (BandSegments * 2f));
                Vector2 mid = (a + b) * 0.5f;
                Vector2 seg = b - a;
                float segLen = seg.Length();
                if (segLen < 0.5f) continue;
                float rot = (float)Math.Atan2(seg.Y, seg.X);

                // ============================================================
                //  LA FÓRMULA EXACTA: glow = sin(θ·20 + t·5)·0.5 + 0.5
                //  → VEINTE BANDAS de emisión recorriendo el anillo.
                // ============================================================
                float glow = 0.5f + 0.5f * (float)Math.Sin(tSpin * BandFreq - time * BandSpeed);

                // Turbulencia viva por hash (regenerada a 12 Hz).
                float turb = 0.74f + 0.26f * Hash01(seed, 720 + s, flick);
                float inten = glow * turb * bright;

                // Gradiente térmico SUPREMO: banda al máximo = blanco-oro
                // incandescente; media = ORO; valle = CARMESÍ.
                Color c;
                if (inten > 0.82f) c = WhiteIncan;
                else if (inten > 0.45f) c = SupGold;
                else c = Crimson;

                // Cápsula HALO (gruesa, tenue) + cápsula NÚCLEO (fina, viva).
                Capsule(mid, segLen, 0.36f * rr * (0.7f + inten), rot, Tint(c, 0.40f * inten));
                Capsule(mid, segLen, 0.12f * rr * inten, rot, Tint(c, 0.85f * inten));

                // En el PICO de cada banda, un punto blanco extra.
                if (inten > 0.86f)
                {
                    Quad(Glow, mid, new Vector2(0.32f * rr, 0.32f * rr), rot,
                        Tint(WhiteIncan, 0.70f * (inten - 0.86f) / 0.14f));
                }
            }
        }

        // ------------------------------------------------------------------
        //  7. BRAZOS ESPIRALES — el vórtice violeta-azul se desenrosca,
        //      con FLUJO ANIMADO: puntos que espiralan hacia adentro
        // ------------------------------------------------------------------

        private static void DrawSpiralArms(Vector2 center, float rr, float time, int seed)
        {
            float armPhase = time * 0.16f;

            for (int arm = 0; arm < ArmCount; arm++)
            {
                float baseT = armPhase + arm * (MathHelper.TwoPi / ArmCount);

                // --- LA MASA DEL BRAZO: cápsulas desenroscándose ---
                for (int k = 0; k < ArmSteps; k++)
                {
                    float f = k / (float)(ArmSteps - 1);           // 0 en el anillo → 1 fuera
                    float t = baseT + f * 1.5f;                    // barrido angular
                    float grow = 1.10f + f * 1.05f;                // radio creciente
                    Vector2 pos = VFXCore.Ellipse(center,
                        BandA * rr * grow, BandB * rr * grow, BandTilt, t);
                    Vector2 next = VFXCore.Ellipse(center,
                        BandA * rr * grow, BandB * rr * grow, BandTilt,
                        t + 1.5f / ArmSteps);
                    Vector2 mid = (pos + next) * 0.5f;
                    Vector2 seg = next - pos;
                    float len = seg.Length();
                    if (len < 0.5f) continue;
                    float rot = (float)Math.Atan2(seg.Y, seg.X);

                    float fade = (float)Math.Pow(1f - f, 1.5);     // se apaga suave
                    // VIOLETA → AZUL PROFUNDO hacia afuera (el acento).
                    Color c = Color.Lerp(SupViolet, DeepBlue, f);
                    Capsule(mid, len, (0.34f - 0.20f * f) * rr, rot, Tint(c, 0.40f * fade));
                    Capsule(mid, len, (0.11f - 0.07f * f) * rr, rot, Tint(c, 0.60f * fade));
                }

                // --- EL FLUJO HACIA ADENTRO: 3 puntos por brazo que
                //     ESPIRALAN del exterior al anillo (la caída viva). ---
                for (int m = 0; m < 3; m++)
                {
                    float h = Hash01(seed, 810 + arm * 9 + m, 37);
                    float life = (time * (0.30f + 0.10f * m) + h + arm * 0.33f) % 1f;
                    float f = 1f - life;   // nace en la PUNTA y CAE al anillo

                    float t = baseT + f * 1.5f;
                    float grow = 1.10f + f * 1.05f;
                    Vector2 pos = VFXCore.Ellipse(center,
                        BandA * rr * grow, BandB * rr * grow, BandTilt, t);

                    // Estela: el punto de hace un instante (más afuera).
                    Vector2 tail = VFXCore.Ellipse(center,
                        BandA * rr * grow, BandB * rr * grow, BandTilt,
                        t - 0.10f * life + 0.045f);
                    Vector2 mid = (pos + tail) * 0.5f;
                    Vector2 seg = pos - tail;
                    float len = seg.Length();
                    if (len > 0.5f)
                    {
                        float rot = (float)Math.Atan2(seg.Y, seg.X);
                        Capsule(mid, len, 0.10f * rr, rot,
                            Tint(SupViolet, 0.45f * (float)Math.Sin(life * Math.PI)));
                    }

                    float alpha = (float)Math.Sin(life * Math.PI) * (0.6f + 0.4f * h);
                    Quad(Glow, pos, new Vector2(0.30f * rr, 0.30f * rr), 0f,
                        Tint(Color.Lerp(WhiteIncan, SupViolet, 0.5f), alpha * 0.8f));
                    Quad(Glow, pos, new Vector2(0.12f * rr, 0.12f * rr), 0f,
                        Tint(WhiteIncan, alpha));
                }
            }
        }

        // ------------------------------------------------------------------
        //  8. CORREDORES DE FOTONES — luz orbitando el vórtice
        // ------------------------------------------------------------------

        private static void DrawPhotonRunners(Vector2 center, float rr, float time, int seed)
        {
            for (int i = 0; i < 4; i++)
            {
                // Orbitan con la rotación del anillo más su propia velocidad.
                float t = time * (BandSpin + 0.55f + 0.20f * i) + i * 1.6f;
                Vector2 pos = BandPoint(center, rr, t);
                float twinkle = 0.65f + 0.35f * (float)Math.Sin(time * 8f + i * 2.3f);

                // Estela corta detrás del fotón.
                Vector2 behind = BandPoint(center, rr, t - 0.22f);
                Vector2 seg = pos - behind;
                float len = seg.Length();
                if (len > 0.5f)
                {
                    float rot = (float)Math.Atan2(seg.Y, seg.X);
                    Vector2 mid = (pos + behind) * 0.5f;
                    Color trailC = i % 3 == 0 ? SupViolet : SupGold;
                    Capsule(mid, len, 0.14f * rr, rot, Tint(trailC, 0.40f * twinkle));
                }

                // halo + núcleo blanco (alternando oro / carmesí / violeta).
                Color c = i % 3 == 0 ? SupViolet : i % 3 == 1 ? SupGold : Crimson;
                Quad(Glow, pos, new Vector2(1.05f * rr, 1.05f * rr), 0f,
                    Tint(c, 0.35f * twinkle));
                Quad(Glow, pos, new Vector2(0.42f * rr, 0.42f * rr), 0f,
                    Tint(WhiteIncan, 0.85f * twinkle));
            }
        }

        // ------------------------------------------------------------------
        //  9a. ⚡ LOS ARCOS DEL HORIZONTE — StormLib.Arc, ~10 Hz
        // ------------------------------------------------------------------

        private static void DrawHorizonArcs(Vector2 center, float r, float time, int seed,
            int boltFlick)
        {
            // TRES CORONAS DE DESCARGA alrededor del horizonte: cada arco
            // recorre un trozo de círculo a radio ligeramente distinto,
            // girando a velocidades distintas — PARPADEANDO a ~10 Hz.
            for (int i = 0; i < 3; i++)
            {
                if (!StormLib.IsLit(seed + 40 + i * 17, boltFlick, 0.82f))
                    continue;

                float span = 1.15f + 0.55f * Hash01(seed, 920 + i, boltFlick / 4);
                float baseA = time * (0.55f + 0.22f * i) * (i % 2 == 0 ? 1f : -1f)
                              + i * 2.1f;
                float radius = r * (1.05f + 0.08f * i);

                StormLib.ArcRing(Main.spriteBatch, center, radius,
                    baseA, baseA + span,
                    seed + 40 + i * 17, boltFlick,
                    Math.Max(2.6f, 0.052f * r),
                    Tint(SupGold, 0.52f),          // funda DORADA
                    Tint(WhiteIncan, 0.95f),        // núcleo blanco-incandescente
                    1f, 9);
            }
        }

        // ------------------------------------------------------------------
        //  9b. ⚡ LOS RAYOS FUGITIVOS — StormLib.Bolt (2 carmesí + 1 oro)
        // ------------------------------------------------------------------

        private static void DrawEscapingBolts(Vector2 center, float rr, float time, int seed,
            int boltFlick)
        {
            // TRES RAYOS escapando del ANILLO DE BANDAS (dobles tiras
            // cuerpo/núcleo con ramas — la librería nueva del proyecto).
            for (int i = 0; i < 3; i++)
            {
                if (!StormLib.IsLit(seed + 61 + i, boltFlick, 0.85f))
                    continue;

                // Emergen donde la banda del shader está EN SU PICO.
                float bandPeak = -time * BandSpeed / BandFreq + i * 2.1f;
                float t = bandPeak + 0.5f * Hash01(seed, 860 + i, boltFlick / 3);
                Vector2 start = BandPoint(center, rr, t);
                Vector2 outward = start - center;
                if (outward.LengthSquared() < 0.01f) continue;
                outward.Normalize();
                // Mezcla radial + tangencial: el rayo ESCAPA girando.
                Vector2 tangent = new Vector2(-outward.Y, outward.X) * 0.40f;
                Vector2 end = start + (outward + tangent) * (1.35f + 0.60f *
                    Hash01(seed, 861 + i, boltFlick)) * rr;

                // 2 CARMESÍ + 1 DORADO (el reparto regio).
                Color haloC = i < 2 ? Crimson : SupGold;
                StormLib.Bolt(Main.spriteBatch, start, end,
                    seed + 130 + i * 53, boltFlick,
                    Math.Max(3f, 0.085f * rr),
                    Tint(haloC, 0.58f), Tint(WhiteIncan, 0.95f),
                    1f, 7, Math.Max(10f, 0.17f * rr));
            }
        }

        // ------------------------------------------------------------------
        //  10. EL TRIPLE CÍRCULO DE RUNAS — 8 doradas CW + 6 violetas CCW
        //      + 6 BLANCAS íntimas CW rápido (v6.23 — el tercer anillo)
        // ------------------------------------------------------------------

        /// <summary>
        /// Tabla de glifos: cada runa es una lista de TRAZOS (pares de
        /// puntos en espacio local ~11×15). Ocho diseños angulares
        /// ORIGINALES de estilo CORONA SUPREMA (soles, cetros y tronos —
        /// la escritura del agujero que une a los cuatro), dibujados como
        /// cápsulas.
        /// </summary>
        private static readonly Vector2[][] _runes = new Vector2[][]
        {
            // S0 — EL SOL ROTO
            new Vector2[] { new(0f, -7f), new(0f, 7f), new(-3.5f, -3f), new(0f, -6.5f), new(3.5f, -3f), new(0f, -6.5f), new(-3.5f, 3.5f), new(3.5f, 3.5f), new(-2f, 5.5f), new(2f, 5.5f) },
            // S1 — EL CETRO
            new Vector2[] { new(0f, 7f), new(0f, -4f), new(0f, -4f), new(-3f, -7f), new(0f, -4f), new(3f, -7f), new(-2.5f, 0f), new(2.5f, 0f), new(-2.5f, 3f), new(2.5f, 3f) },
            // S2 — EL TRONO
            new Vector2[] { new(-3.5f, 7f), new(-3.5f, -5f), new(-3.5f, -5f), new(3.5f, -5f), new(3.5f, -5f), new(3.5f, 7f), new(-3.5f, -5f), new(0f, -7f), new(-1.5f, 1.5f), new(1.5f, 1.5f) },
            // S3 — LA ESTRELLA DOBLE
            new Vector2[] { new(0f, 7f), new(0f, -7f), new(-4f, 0f), new(4f, 0f), new(-2.5f, -4.5f), new(2.5f, 4.5f), new(2.5f, -4.5f), new(-2.5f, 4.5f) },
            // S4 — EL CIRCUITO REAL
            new Vector2[] { new(-3.5f, 6f), new(-3.5f, -4f), new(-3.5f, -4f), new(3.5f, -4f), new(3.5f, -4f), new(3.5f, 6f), new(-3.5f, 6f), new(3.5f, 6f), new(-3.5f, -6.5f), new(3.5f, -6.5f), new(0f, -4f), new(0f, -6.5f) },
            // S5 — LA VUELTA SUPREMA
            new Vector2[] { new(-3f, 6f), new(-3f, -2f), new(-3f, -2f), new(3f, -6f), new(3f, -6f), new(3f, 2f), new(3f, 2f), new(-2.5f, 6f), new(-1.5f, -6.5f), new(1.5f, -6.5f) },
            // S6 — EL OJO DEL VACÍO
            new Vector2[] { new(-4f, 0f), new(0f, -4f), new(0f, -4f), new(4f, 0f), new(4f, 0f), new(0f, 4f), new(0f, 4f), new(-4f, 0f), new(-1.5f, 0f), new(1.5f, 0f), new(0f, -7f), new(0f, -4.5f), new(0f, 4.5f), new(0f, 7f) },
            // S7 — LA CORONA ESTELAR
            new Vector2[] { new(-4f, 5.5f), new(-4f, -5.5f), new(-4f, -5.5f), new(-2f, -1f), new(-2f, -1f), new(0f, -6.5f), new(0f, -6.5f), new(2f, -1f), new(2f, -1f), new(4f, -5.5f), new(4f, -5.5f), new(4f, 5.5f), new(-4f, 5.5f), new(4f, 5.5f) },
        };

        private static void DrawRuneCircles(Vector2 center, float r, float time, int seed)
        {
            float glyphScale = Math.Max(r / 52f, 0.25f) * 1.40f;

            // ============================================================
            //  EL CÍRCULO DORADO — 8 runas girando CW (con el conjunto)
            // ============================================================
            RingQuad(center, GoldRuneRadius * r, time * GoldRuneOrbit,
                Tint(RuneGold, 0.24f));
            for (int g = 0; g < GoldRuneCount; g++)
            {
                DrawRune(center, r, time, g, GoldRuneRadius,
                    RuneGold, RuneGoldTip, glyphScale,
                    count: GoldRuneCount, orbit: GoldRuneOrbit, offset: 0);
            }

            // ============================================================
            //  EL CÍRCULO VIOLETA — 6 runas MÁS AFUERA girando CCW
            //  (el contrarroto arcano de la fusión)
            // ============================================================
            RingQuad(center, VioletRuneRadius * r, time * VioletRuneOrbit,
                Tint(RuneViolet, 0.18f));
            for (int g = 0; g < VioletRuneCount; g++)
            {
                DrawRune(center, r, time, g, VioletRuneRadius,
                    RuneViolet, RuneVioletTip, glyphScale * 0.85f,
                    count: VioletRuneCount, orbit: VioletRuneOrbit, offset: 3);
            }

            // ============================================================
            //  EL CÍRCULO BLANCO — 6 runas MÁS ADENTRO, rápido e íntimo
            //  (v6.23: entre el anillo de fotones y el dorado — la tercera
            //  corona del conjuro, la más cercana al abismo)
            // ============================================================
            RingQuad(center, WhiteRuneRadius * r, time * WhiteRuneOrbit,
                Tint(RuneWhite, 0.20f));
            for (int g = 0; g < WhiteRuneCount; g++)
            {
                DrawRune(center, r, time, g, WhiteRuneRadius,
                    RuneWhite, RuneWhiteTip, glyphScale * 0.92f,
                    count: WhiteRuneCount, orbit: WhiteRuneOrbit, offset: 6);
            }
        }

        /// <summary>
        /// Una runa del círculo (glifo + resplandor + perla). v6.23: cada
        /// llamada trae SU `count` y SU `orbit` — el círculo ya NO se
        /// deduce del `offset` (hay TRES coronas, no dos).
        /// </summary>
        private static void DrawRune(Vector2 center, float r, float time, int g,
            float radius, Color body, Color tip, float glyphScale,
            int count, float orbit, int offset)
        {
            float ang = g / (float)count * MathHelper.TwoPi + time * orbit;

            // Flotación viva: el radio respira por glifo y el glifo se mece.
            float floatR = radius * r +
                           2.4f * glyphScale * (float)Math.Sin(time * 1.35f + g * 0.9f);
            float bobY = 2.0f * glyphScale * (float)Math.Sin(time * 0.85f + g * 1.7f);
            Vector2 glyphPos = center + new Vector2(
                (float)Math.Cos(ang) * floatR,
                (float)Math.Sin(ang) * floatR + bobY);

            // Latido de brillo propio por glifo.
            float pulse = 0.75f + 0.25f * (float)Math.Sin(time * 2.4f + g * 1.3f);

            // Resplandor suave DETRÁS de cada runa.
            Quad(Glow, glyphPos, new Vector2(36f * glyphScale, 36f * glyphScale), 0f,
                Tint(body, 0.20f * pulse));

            // Trazos: cápsulas, cuerpo → punta pálida.
            Vector2[] strokes = _runes[(g + offset) % _runes.Length];
            for (int s = 0; s < strokes.Length; s += 2)
            {
                Vector2 a = glyphPos + strokes[s] * glyphScale;
                Vector2 b = glyphPos + strokes[s + 1] * glyphScale;
                Vector2 mid = (a + b) * 0.5f;
                Vector2 delta = b - a;
                float len = delta.Length();
                if (len < 0.01f) continue;
                float rot = (float)Math.Atan2(delta.Y, delta.X);

                // Gradiente vertical: abajo cuerpo, arriba punta pálida.
                float localY = ((strokes[s].Y + strokes[s + 1].Y) * 0.5f + 7f) / 14f;
                Color col = Color.Lerp(tip, body, 1f - localY * 0.25f);

                Capsule(mid, len, 3.4f * glyphScale, rot, Tint(col, 0.85f * pulse));
            }

            // PERLA sobre el glifo (la gema de la corona).
            Vector2 pearlPos = glyphPos - new Vector2(0f, 11.5f * glyphScale);
            float pearlPulse = 0.8f + 0.2f * (float)Math.Sin(time * 3.0f + g * 2.0f);
            Quad(Glow, pearlPos, new Vector2(7.0f * glyphScale, 7.0f * glyphScale), 0f,
                Tint(body, 0.62f * pulse));
            Quad(Glow, pearlPos, new Vector2(3.2f * glyphScale, 3.2f * glyphScale), 0f,
                Tint(tip, 0.9f * pearlPulse));
        }

        // ------------------------------------------------------------------
        //  11. JETS POLARES — chorro DORADO arriba + VIOLETA abajo,
        //      cada uno con un RAYO StormLib DENTRO
        // ------------------------------------------------------------------

        private static void DrawPolarJets(Vector2 center, float rr, float time,
            int seed, int boltFlick)
        {
            // Eje menor del anillo de bandas (los "polos" del vórtice).
            Vector2 pole = new Vector2(
                -(float)Math.Sin(BandTilt + MathHelper.PiOver2),
                (float)Math.Cos(BandTilt + MathHelper.PiOver2));
            float pulse = 0.7f + 0.3f * (float)Math.Sin(time * 1.9f);

            for (int side = 0; side < 2; side++)
            {
                // side 0 → ARRIBA (chorro DORADO); side 1 → ABAJO (VIOLETA).
                Vector2 dir = side == 0 ? -pole : pole;
                float rot = (float)Math.Atan2(dir.Y, dir.X);
                Color cHot = side == 0 ? WhiteIncan : new Color(225, 235, 255);
                Color cBody = side == 0 ? SupGold : SupViolet;

                float baseOff = BandB * rr * 0.85f;
                Vector2 basePos = center + dir * baseOff;

                // --- EL CHORRO: 3 tramos ahusados con GLOW VIAJANDO
                //     hacia afuera (la onda de brillo sube por el haz). ---
                const int Tramos = 3;
                for (int k = 0; k < Tramos; k++)
                {
                    float f0 = k / (float)Tramos, f1 = (k + 1) / (float)Tramos;
                    float midF = (f0 + f1) * 0.5f;
                    Vector2 a = center + dir * (baseOff + f0 * 1.95f * rr);
                    Vector2 b = center + dir * (baseOff + f1 * 1.95f * rr);
                    Vector2 mid = (a + b) * 0.5f;
                    float len = (b - a).Length();
                    float w = (0.30f - 0.22f * midF) * rr;

                    // La onda de brillo VIAJA por el haz hacia la punta.
                    float wave = 0.5f + 0.5f * (float)Math.Sin(midF * 7f - time * 6f);
                    Color c = Color.Lerp(cHot, cBody, midF * 0.7f);
                    Capsule(mid, len, w * 2.0f, rot,
                        Tint(c, (0.20f + 0.18f * wave) * pulse * (1f - midF * 0.4f)));
                    Capsule(mid, len, w, rot,
                        Tint(c, (0.45f + 0.35f * wave) * pulse * (1f - midF * 0.4f)));
                }

                // --- LAS BOLAS viajando por el haz (el jet escupe materia). ---
                for (int m = 0; m < 3; m++)
                {
                    float phase = (time * 0.55f + m / 3f + side * 0.5f) % 1f;
                    Vector2 pos = center + dir * (baseOff + (0.15f + 1.75f * phase) * rr);
                    float a = (float)Math.Sin(phase * Math.PI) * 0.55f;
                    Quad(Glow, pos, new Vector2(0.42f * rr, 0.42f * rr), 0f,
                        Tint(cBody, a));
                    Quad(Glow, pos, new Vector2(0.18f * rr, 0.18f * rr), 0f,
                        Tint(cHot, a * 1.6f));
                }

                // --- ⚡ EL RAYO DENTRO DEL JET: un StormLib.Bolt
                //     recorriendo el corazón del chorro. ---
                if (StormLib.IsLit(seed + 90 + side * 13, boltFlick, 0.80f))
                {
                    Vector2 tip = center + dir * (baseOff + 1.85f * rr);
                    StormLib.Bolt(Main.spriteBatch, basePos, tip,
                        seed + 210 + side * 29, boltFlick,
                        Math.Max(2.6f, 0.045f * rr),
                        Tint(cBody, 0.50f), Tint(cHot, 0.90f),
                        1f, 6, Math.Max(8f, 0.12f * rr));
                }

                // --- Punta incandescente del polo. ---
                Vector2 jetTip = center + dir * (baseOff + 1.95f * rr);
                Quad(Glow, jetTip, new Vector2(0.55f * rr, 0.55f * rr), 0f,
                    Tint(cHot, 0.42f * pulse));
            }
        }

        // ------------------------------------------------------------------
        //  12. VOLUTAS CAYENDO — BrumaFX.Tendril espiralando al núcleo
        //      (la materia devorada se DISUELVE en humo — herencia Bruma)
        // ------------------------------------------------------------------

        private static void DrawFallingTendrils(Vector2 center, float r, float time, int seed)
        {
            for (int k = 0; k < TendrilCount; k++)
            {
                // Cada voluta gira a su velocidad (la rotación de la caída).
                float baseAng = time * (0.24f + 0.10f * k) + k * 2.6f;

                // LA RUTA ESPIRAL: nace lejos (2.95·R) y CAE al núcleo
                // (0.45·R) barriendo TendrilSweep rad — 8 puntos.
                var path = new Vector2[8];
                for (int p = 0; p < 8; p++)
                {
                    float f = p / 7f;
                    float ang = baseAng + f * TendrilSweep;
                    float rad = (TendrilStart - (TendrilStart - 0.45f) * f) * r;
                    path[p] = center + new Vector2(
                        (float)Math.Cos(ang) * rad,
                        (float)Math.Sin(ang) * rad * 0.8f);   // leve aplastado
                }

                // Una DORADA y una VIOLETA (la fusión de las estirpes);
                // la voluta se DISUELVE al acercarse al núcleo (fade=1).
                Color c = k % 2 == 0 ? new Color(190, 130, 50) : SupViolet;
                float n = BrumaNoise.Fbm(k * 3.7f, time * 0.15f, seed + k, 3);
                BrumaFX.Tendril(path, 0.55f * r, c, seed + k * 97, time,
                    alpha: 0.40f + 0.20f * n, fade: 1f);
            }
        }

        // ------------------------------------------------------------------
        //  13. ONDAS DE DISTORSIÓN ×3 — el espacio-tiempo late
        // ------------------------------------------------------------------

        private static void DrawDistortionWaves(Vector2 center, float r, float time, int seed)
        {
            for (int w = 0; w < WaveCount; w++)
            {
                float phase = ((time / WaveCycle) + w / (float)WaveCount) % 1f;
                float radius = (1.15f + phase * 2.05f) * r;
                float fade = (1f - phase) * (1f - phase);

                // ORO / VIOLETA / BLANCO CÁLIDO alternando (las tres voces).
                Color c = w % 3 == 0 ? new Color(255, 215, 140)
                        : w % 3 == 1 ? new Color(185, 160, 255)
                        : new Color(255, 240, 210);
                RingQuad(center, radius, phase * 2.4f + w,
                    Tint(c, 0.28f * fade));
            }
        }
    }
}
