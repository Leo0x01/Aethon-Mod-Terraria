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
    /// SupremoAuroraBlackHoleRenderer — v6.23 — EL AGUJERO NEGRO SUPREMO
    /// AURORA, 100% CÓDIGO.
    ///
    /// v6.23 — EL TRIPLE CÍRCULO DE RUNAS (petición del usuario): junto
    /// a los dos anillos rúnicos clásicos nace el TERCERO — el círculo
    /// MORADO íntimo (6 runas @2.02·R, CW rápido) entre el anillo de
    /// fotones y el azul: EL COLOR QUE FALTABA del gradiente. `DrawRune`
    /// ahora recibe SU `count` y SU `orbit` por llamada.
    ///
    /// PETICIÓN DEL USUARIO (v6.20): "en el agujero negro supremo, cambiar
    /// el color: centro NEGRO y que vaya cambiando de color — a MORADO
    /// cerca del centro, AZUL y DORADO en los bordes. Guardar una copia
    /// del original y crear uno nuevo con estos cambios".
    ///
    /// EL ORIGINAL QUEDA INTACTO: SupremoBlackHoleRenderer no se toca —
    /// esta es la copia NUEVA con el GRADIENTE AURORA aplicado a cada
    /// capa por su DISTANCIA RADIAL al núcleo:
    ///
    ///   · EL NÚCLEO        → NEGRO ABSOLUTO (el horizonte siempre gana).
    ///   · JUNTO AL NÚCLEO  → MORADO (185,105,255): el rim del horizonte,
    ///     el anillo de fotones, los arcos eléctricos, el interior del
    ///     disco Doppler, la nube interna de humo y el círculo íntimo de
    ///     runas moradas (v6.23).
    ///   · EL MEDIO         → AZUL (92,150,255): los brazos espirales al
    ///     desenroscarse, el círculo interior de runas, el contrarroto.
    ///   · LOS BORDES       → DORADO (255,195,90): el exterior del anillo
    ///     de bandas, las puntas de los brazos y jets, el círculo exterior
    ///     de runas, la nube externa y el aura final.
    ///
    /// AuroraGrad(t) — el gradiente como FUNCIÓN: morado → azul → dorado
    /// en t = 0..1. Lo beben el anillo de bandas (por distancia radial de
    /// cada segmento), los brazos espirales (a lo largo del brazo), los
    /// jets polares (a lo largo del haz) y las ondas de distorsión (la
    /// onda NACE morada junto al núcleo y MUERE dorada en el borde).
    ///
    /// La ESTRUCTURA es la MISMA del Supremo original (la fusión de los
    /// 4: disco Doppler del Umbral, anillo de bandas del Cósmico, brazos
    /// del Olvido, humo de la Bruma, corona de rayos de StormLib) —
    /// SOLO cambia el COLOR: el negro→morado→azul→dorado del alba polar.
    ///
    /// CONTRATO DE BATCH (v6.10, a prueba de balas): Draw() exige el
    /// SpriteBatch CERRADO y lo deja CERRADO.
    /// </summary>
    public static class SupremoAuroraBlackHoleRenderer
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

        // --- EL DOBLE CÍRCULO DE RUNAS (v6.20: el interior AZUL — el medio
        //     del gradiente — y el exterior DORADO — el borde) ---
        private const int InnerRuneCount = 8;      // azules, CW
        private const float InnerRuneRadius = 2.62f;   // ×R
        private const float InnerRuneOrbit = 0.10f;    // rad/s
        private const int OuterRuneCount = 6;      // doradas, CCW
        private const float OuterRuneRadius = 3.30f; // ×R — MÁS AFUERA (borde)
        private const float OuterRuneOrbit = -0.075f; // rad/s — CONTRARROTO
        // --- v6.23: EL TERCER CÍRCULO — morado, el MÁS ÍNTIMO (el color
        //     que FALTABA del gradiente, junto al núcleo) girando rápido ---
        private const int PurpleRuneCount = 6;      // moradas, CW rápido
        private const float PurpleRuneRadius = 2.02f;  // ×R — MÁS ADENTRO
        private const float PurpleRuneOrbit = 0.16f;   // rad/s — el círculo vivo

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
        //  PALETA AURORA (v6.20) — EL GRADIENTE RADIAL DEL USUARIO:
        //  centro NEGRO → MORADO cerca del centro → AZUL al medio →
        //  DORADO en los bordes.
        // ==================================================================

        private static readonly Color WhiteIncan = new(238, 242, 255); // blanco frío (picos)
        private static readonly Color AurPurple = new(185, 105, 255);  // MORADO — junto al núcleo
        private static readonly Color DeepPurple = new(108, 58, 190);  // morado profundo (aleja)
        private static readonly Color AurBlue = new(92, 150, 255);     // AZUL — el medio
        private static readonly Color AurGold = new(255, 195, 90);     // DORADO — los bordes
        private static readonly Color RuneBlue = new(125, 165, 255);   // runa azul (círculo interior)
        private static readonly Color RuneBlueTip = new(222, 234, 255);
        private static readonly Color RuneGold = new(255, 185, 75);    // runa dorada (círculo del borde)
        private static readonly Color RuneGoldTip = new(255, 240, 190);
        private static readonly Color RunePurple = new(185, 130, 255);  // runa morada (círculo íntimo)
        private static readonly Color RunePurpleTip = new(235, 215, 255); // punta morada pálida
        private static readonly Color NebPurple = new(98, 62, 188);    // nebulosa morada
        private static readonly Color NebGold = new(182, 132, 52);     // nebulosa dorada
        private static readonly Color WarmWhite = new(240, 245, 255);  // blanco aurora

        /// <summary>
        /// EL GRADIENTE AURORA (la petición v6.20 como FUNCIÓN): MORADO
        /// cerca del centro → AZUL al medio → DORADO en los bordes.
        /// `t` = distancia radial normalizada (0 = junto al horizonte,
        /// 1 = borde del sistema). El NEGRO del centro lo pinta el
        /// BlackDisk encima: el gradiente EMPIEZA en morado.
        /// </summary>
        private static Color AuroraGrad(float t)
        {
            t = MathHelper.Clamp(t, 0f, 1f);
            if (t < 0.45f) return Color.Lerp(AurPurple, AurBlue, t / 0.45f);
            return Color.Lerp(AurBlue, AurGold, (t - 0.45f) / 0.55f);
        }

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
                    new Color(6, 6, 16, 178));
                Main.spriteBatch.End();

                // ============ 2..15: TODO LO BRILLANTE (aditivo) ============
                BeginAdditive();

                // --- 2. NEBULOSAS doradas/violetas + polvo ambiental ---
                DrawNebulas(center, rr, tSlow, seed);

                // --- 3. EL HALO DE HUMO — 2 Cloud de la LIBRERÍA DE BRUMA ---
                // (EL GRADIENTE AURORA: la nube INTERNA morada — cerca del
                // centro — y la EXTERNA dorada — el borde del sistema).
                BrumaFX.Cloud(center, 2.7f * rr, AurPurple, seed + 11, tSlow * 0.8f,
                    puffs: 5, alpha: 0.26f);
                BrumaFX.Cloud(center, 3.4f * rr, AurGold, seed + 47, tSlow * 0.6f,
                    puffs: 4, alpha: 0.17f);

                // --- 4. EL DISCO DOPPLER — mitad TRASERA (estrías pintadas) ---
                DrawDopplerDisk(center, rr, tMid, seed, flick, front: false);

                // --- 4b. ECO TENUE del anillo de bandas (mitad trasera) ---
                DrawBandRing(center, rr, tFast, seed, flick, front: false);

                // --- 5. NÚCLEO negro absoluto + rim MORADO pulsante ---
                // (JUNTO al centro: MORADO — el inicio del gradiente aurora.)
                Main.spriteBatch.End();
                BeginAlpha();
                Quad(Disk, center, new Vector2(2.28f * r, 2.28f * r), 0f, Color.White);
                Main.spriteBatch.End();
                BeginAdditive();
                RingQuad(center, 1.02f * r, tMid * 0.13f,
                    Tint(AurPurple, 0.42f + 0.14f * (float)Math.Sin(tMid * 1.6f)));

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
                // El filo MORADO del horizonte, respirando (la última luz
                // antes del negro — el gradiente empieza aquí).
                RingQuad(center, 1.02f * r, tMid * 0.13f,
                    Tint(AurPurple, 0.30f + 0.10f * (float)Math.Sin(tMid * 1.6f)));

                // --- 15. AURA FINAL GRANDE pulsante ---
                float aura = 0.85f + 0.15f * (float)Math.Sin(tFast * 1.4f);
                // DORADA (6.6R — EL BORDE del sistema: el final del gradiente).
                Quad(Glow, center, new Vector2(6.6f * rr, 6.6f * rr), 0f,
                    Tint(AurGold, 0.20f * aura));
                // El CONTRARROTO azul: el medio del gradiente girando al revés.
                RingQuad(center, 3.1f * rr, -tMid * 0.08f,
                    Tint(AurBlue, 0.10f * aura));

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
        //  2. NEBULOSAS + POLVO AMBIENTAL — moradas y doradas
        // ------------------------------------------------------------------

        private static void DrawNebulas(Vector2 center, float rr, float time, int seed)
        {
            // SEIS velos ENORMES y tenues: el morado alterna con el dorado
            // (el cuerpo y el borde del gradiente aurora).
            for (int i = 0; i < 6; i++)
            {
                float h = Hash01(seed, 501 + i, 17);
                float dir = i % 2 == 0 ? 1f : -1f;
                float ang = h * MathHelper.TwoPi + time * 0.05f * dir;
                float dist = (2.8f + 0.8f * Hash01(seed, 502 + i, 29)) * rr;
                Vector2 pos = center + new Vector2(
                    (float)Math.Cos(ang) * dist, (float)Math.Sin(ang) * dist * 0.8f);
                float size = (2.0f + 1.1f * Hash01(seed, 503 + i, 41)) * rr;
                Color c = i % 2 == 0 ? NebPurple : NebGold;
                float pulse = 0.75f + 0.25f * (float)Math.Sin(time * 0.7f + i * 1.9f);
                Quad(Glow, pos, new Vector2(size, size), ang,
                    Tint(c, (i % 2 == 0 ? 0.20f : 0.14f) * pulse));
            }

            // Polvo ambiental: chispas moradas, azules y brasas doradas.
            for (int i = 0; i < 14; i++)
            {
                float h = Hash01(seed, 600 + i, 13);
                float ang = h * MathHelper.TwoPi + time * 0.03f * (i % 2 == 0 ? 1f : -1f);
                float dist = (2.2f + 3.1f * Hash01(seed, 601 + i, 19)) * rr * 0.55f;
                Vector2 pos = center + new Vector2(
                    (float)Math.Cos(ang) * dist, (float)Math.Sin(ang) * dist);
                float pulse = 0.5f + 0.5f * (float)Math.Sin(time * 1.5f + i * 2.4f);
                Color c = h < 0.40f ? new Color(202, 138, 48)   // brasa dorada (borde)
                        : h < 0.75f ? new Color(92, 150, 255)   // chispa azul (medio)
                        : new Color(168, 100, 250);              // chispa morada (centro)
                float size = (0.10f + 0.10f * h) * rr;
                Quad(Glow, pos, new Vector2(size, size), 0f, Tint(c, 0.45f * pulse));
            }
        }

        // ------------------------------------------------------------------
        //  4. EL DISCO DOPPLER — estrías pintadas (herencia UMBRAL),
        //      blanco frío (acerca) → MORADO (interior) → AZUL (1.10-1.24R)
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

                    // Color del núcleo por Doppler: BLANCO FRÍO (acerca) →
                    // MORADO → morado-azul → MORADO PROFUNDO (aleja). El
                    // disco vive JUNTO al centro: la voz morada del gradiente.
                    float inten = (0.25f + 0.75f * dop) * turb * bright;
                    Color core;
                    if (dop > 0.62f) core = WhiteIncan;
                    else if (dop > 0.30f) core = AurPurple;
                    else if (dop > 0.10f) core = Color.Lerp(AurPurple, AurBlue, 0.5f);
                    else core = DeepPurple;

                    Capsule(mid, len, wBase * 2.1f, rot,
                        Tint(Color.Lerp(core, DeepPurple, 0.25f), 0.55f * inten));
                    Capsule(mid, len, wBase, rot, Tint(core, 1.0f * inten));

                    // Punto BLANCO FRÍO donde la turbulencia ARDE.
                    if (turb > 0.82f && inten > 0.28f)
                    {
                        Quad(Glow, mid, new Vector2(0.34f * rr, 0.34f * rr), rot,
                            Tint(WhiteIncan, 0.95f * inten * (turb - 0.82f) / 0.18f));
                    }
                }

                // --- BANDA MEDIA: morado→azul (a 1.10× del anillo) ---
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
                            Tint(Color.Lerp(AurPurple, AurBlue, 0.45f), 0.48f * inten));
                    }
                }

                // --- BANDA EXTERNA: AZUL desvaneciendo (a 1.24×) — el
                //     puente de color hacia los brazos espirales. ---
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
                            Tint(AurBlue, 0.40f * inten));
                    }
                }
            }
        }

        // ------------------------------------------------------------------
        //  4b/6. EL ANILLO DE BANDAS — LA FÓRMULA DEL SHADER CÓSMICO, FIEL:
        //         glow = sin(θ·20 + t·5)·0.5+0.5 — 20 bandas viajando,
        //         coloreadas por el GRADIENTE AURORA RADIAL de cada punto
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

                // ============================================================
                //  EL GRADIENTE AURORA POR SEGMENTO: el punto del anillo MÁS
                //  CERCANO al núcleo arde MORADO; el más LEJANO muere DORADO
                //  (AZUL entre ambos) — la petición hecha geometría.
                // ============================================================
                float radial = (mid - center).Length() / rr;   // 1.24 .. 1.95
                float gt = MathHelper.Clamp((radial - 1.24f) / 0.71f, 0f, 1f);
                Color c = AuroraGrad(gt);
                if (inten > 0.82f)
                    c = Color.Lerp(c, WhiteIncan, 0.75f);      // pico → blanco frío

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
        //  7. BRAZOS ESPIRALES — el vórtice se desenrosca MORADO en la
        //      base y muere DORADO en la punta (el gradiente a lo largo
        //      del brazo), con FLUJO ANIMADO hacia adentro
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
                    // EL GRADIENTE AURORA A LO LARGO DEL BRAZO: nace MORADO
                    // (junto al anillo) → AZUL → DORADO en la punta (borde).
                    Color c = AuroraGrad(f);
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
                            Tint(AurBlue, 0.45f * (float)Math.Sin(life * Math.PI)));
                    }

                    float alpha = (float)Math.Sin(life * Math.PI) * (0.6f + 0.4f * h);
                    Quad(Glow, pos, new Vector2(0.30f * rr, 0.30f * rr), 0f,
                        Tint(Color.Lerp(WhiteIncan, AurBlue, 0.5f), alpha * 0.8f));
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
                    Color trailC = i % 3 == 0 ? AurPurple : AurGold;
                    Capsule(mid, len, 0.14f * rr, rot, Tint(trailC, 0.40f * twinkle));
                }

                // halo + núcleo blanco (alternando morado / dorado / azul).
                Color c = i % 3 == 0 ? AurPurple : i % 3 == 1 ? AurGold : AurBlue;
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
                    Tint(AurPurple, 0.52f),         // funda MORADA (junto al núcleo)
                    Tint(WhiteIncan, 0.95f),        // núcleo blanco frío
                    1f, 9);
            }
        }

        // ------------------------------------------------------------------
        //  9b. ⚡ LOS RAYOS FUGITIVOS — StormLib.Bolt (2 azules + 1 dorado)
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

                // 2 AZULES + 1 DORADO (el reparto del gradiente exterior).
                Color haloC = i < 2 ? AurBlue : AurGold;
                StormLib.Bolt(Main.spriteBatch, start, end,
                    seed + 130 + i * 53, boltFlick,
                    Math.Max(3f, 0.085f * rr),
                    Tint(haloC, 0.58f), Tint(WhiteIncan, 0.95f),
                    1f, 7, Math.Max(10f, 0.17f * rr));
            }
        }

        // ------------------------------------------------------------------
        //  10. EL TRIPLE CÍRCULO DE RUNAS — 8 AZULES CW + 6 DORADAS CCW
        //      + 6 MORADAS íntimas CW rápido (v6.23: el TERCER anillo —
        //      morado→azul→dorado de dentro afuera YA completo)
        //      v6.37 — delega en OrbitaLib (la librería de los anillos
        //      rúnicos): ni un número cambiado. El alfabeto S vive AHORA
        //      en OrbitaLib.RunasVacio (el default de la librería).
        // ------------------------------------------------------------------

        /// <summary>
        /// v6.37 — delega en OrbitaLib (la librería de los anillos
        /// rúnicos): ni un número cambiado. Cada corona del gradiente
        /// aurora es UNA llamada a `OrbitaLib.CirculoRunico` con SUS
        /// constantes de siempre — el aro fino de pauta, las runas DE
        /// PIE (resplandor, trazos de cápsula con gradiente, perla
        /// latiendo) y la flotación viva viven AHORA en la primitiva.
        /// El alfabeto S (la tabla `_runes` de siempre, idéntica byte a
        /// byte) es el default de la librería: OrbitaLib.RunasVacio.
        /// </summary>
        private static void DrawRuneCircles(Vector2 center, float r, float time, int seed)
        {
            float glyphScale = Math.Max(r / 52f, 0.25f) * 1.40f;

            // ============================================================
            //  EL CÍRCULO AZUL — 8 runas girando CW (el MEDIO del gradiente)
            // ============================================================
            OrbitaLib.CirculoRunico(center, r, time, InnerRuneRadius, InnerRuneCount,
                InnerRuneOrbit, RuneBlue, RuneBlueTip, glyphScale,
                offset: 0, ringAlpha: 0.24f);

            // ============================================================
            //  EL CÍRCULO DORADO — 6 runas MÁS AFUERA girando CCW (el BORDE
            //  del gradiente — el contrarroto del alba polar)
            // ============================================================
            OrbitaLib.CirculoRunico(center, r, time, OuterRuneRadius, OuterRuneCount,
                OuterRuneOrbit, RuneGold, RuneGoldTip, glyphScale * 0.85f,
                offset: 3, ringAlpha: 0.18f);

            // ============================================================
            //  EL CÍRCULO MORADO — 6 runas MÁS ADENTRO, rápido e íntimo
            //  (v6.23: EL COLOR QUE FALTABA del gradiente — junto al
            //  núcleo, entre el anillo de fotones y el azul)
            // ============================================================
            OrbitaLib.CirculoRunico(center, r, time, PurpleRuneRadius, PurpleRuneCount,
                PurpleRuneOrbit, RunePurple, RunePurpleTip, glyphScale * 0.92f,
                offset: 6, ringAlpha: 0.20f);
        }

        // ------------------------------------------------------------------
        //  11. JETS POLARES — el gradiente aurora A LO LARGO del haz:
        //      MORADO en la base (junto al núcleo) → AZUL al medio →
        //      DORADO en la punta, con un RAYO StormLib DENTRO
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
                // side 0 → ARRIBA; side 1 → ABAJO. Ambos con el MISMO
                // gradiente aurora: morado en la base → dorado en la punta.
                Vector2 dir = side == 0 ? -pole : pole;
                float rot = (float)Math.Atan2(dir.Y, dir.X);
                Color cHot = WhiteIncan;              // blanco frío (la punta)
                Color cBody = AurBlue;                // el cuerpo azul (el medio)

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
                    // EL GRADIENTE AURORA por el haz: MORADO (base) → AZUL
                    // (medio) → DORADO (punta) — la petición hecha chorro.
                    Color grad = AuroraGrad(0.10f + 0.90f * midF);
                    Color c = Color.Lerp(cHot, grad, 0.30f + 0.70f * midF);
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

                // Una DORADA y una MORADA (el borde y el centro del
                // gradiente); la voluta se DISUELVE al acercarse al núcleo.
                Color c = k % 2 == 0 ? new Color(202, 142, 55) : AurPurple;
                float n = BrumaNoise.Fbm(k * 3.7f, time * 0.15f, seed + k, 3);
                BrumaFX.Tendril(path, 0.55f * r, c, seed + k * 97, time,
                    alpha: 0.40f + 0.20f * n, fade: 1f);
            }
        }

        // ------------------------------------------------------------------
        //  13. ONDAS DE DISTORSIÓN ×3 — el espacio-tiempo late CON EL
        //     GRADIENTE: cada onda NACE morada junto al núcleo y MUERE
        //     dorada en el borde — el alba polar expandiéndose
        // ------------------------------------------------------------------

        private static void DrawDistortionWaves(Vector2 center, float r, float time, int seed)
        {
            for (int w = 0; w < WaveCount; w++)
            {
                float phase = ((time / WaveCycle) + w / (float)WaveCount) % 1f;
                float radius = (1.15f + phase * 2.05f) * r;
                float fade = (1f - phase) * (1f - phase);

                // NACE MORADA (phase 0, cerca del centro) → AZUL → MUERE
                // DORADA (phase 1, el borde) — literalmente el gradiente.
                Color c = AuroraGrad(phase);
                RingQuad(center, radius, phase * 2.4f + w,
                    Tint(c, 0.28f * fade));
            }
        }
    }
}
