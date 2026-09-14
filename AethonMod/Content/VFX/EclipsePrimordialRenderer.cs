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
    /// EclipsePrimordialRenderer — v6.24 — LA FUSIÓN TOTAL, 100% CÓDIGO.
    ///
    /// v6.24 — LA ORDEN DEL USUARIO: "al agujero negro eclipse no se le
    /// ve el sol" + "no debe ser completamente oscuro en el centro, debe
    /// tener alguna animación o mejor, que tenga mucho humo o bruma":
    ///   · LA CORONA SOLAR DEL ECLIPSE — el SOL DE VERDAD detrás de la
    ///     luna negra: halo caliente + bloom LumenLib + 9 STREAMERS de
    ///     luz radiando del limbo (el repintado negro se come el centro
    ///     y el sol queda como el resplandor TOTAL de un eclipse real).
    ///   · EL LIMBO BLANCO-CÁLIDO al borde del disco (el filo del sol
    ///     asomando) + el destello de 4 puntas (el anillo de diamante).
    ///   · EL HUMO DEL VACÍO — la luna negra NO es un punto muerto:
    ///     MUCHA bruma viva (masa central que respira + 6 volátiles
    ///     orbitando CW/CCW + 2 volutas espiralando al centro) girando
    ///     DENTRO del disco, con una brasa violeta latiendo debajo.
    ///
    /// v6.23 — "negro en su centro, el sol debe verse ligeramente": el
    /// aura final se pinta ANTES del repintado negro y el sol asoma al
    /// borde del disco.
    ///
    /// EL ARMA FINAL DEL PROYECTO (petición del usuario): "un bastón nuevo
    /// que fusione el sol de 20 anillos rúnicos, más todos los agujeros
    /// negros, con efectos de luz, bruma, humo, rayos y otros efectos".
    ///
    /// EL ECLIPSE: un agujero negro supremo con EL SISTEMA SOLAR RÚNICO
    /// COMPLETO (los 20 anillos de la copia XX, el gran sellado y su
    /// cometa) orbitando el horizonte de sucesos. Cada herencia:
    ///
    ///   · del SUPREMO      → el núcleo negro devorador + rim dorado + la
    ///                         doble geometría disco/anillo + jets polares.
    ///   · del UMBRAL       → el DISCO DOPPLER oblicuo (ahora con el
    ///                         GRADIENTE AURORA: morado→azul→dorado).
    ///   · del CÓSMICO      → el ANILLO DE BANDAS del shader: 20 zonas de
    ///                         brillo viajando (glow = sin(θ·20+t·5)).
    ///   · del OLVIDO       → los BRAZOS ESPIRALES con flujo hacia adentro.
    ///   · de la BRUMA      → el HALO DE NUBES (BrumaFX.Cloud) + las
    ///                         VOLUTAS cayendo + puffs de humo vivos.
    ///   · del SUPREMO AURORA → AuroraGrad(t): el gradiente negro→morado→
    ///                         azul→dorado pintando el material fundido.
    ///   · del SOL XX       → los 20 ANILLOS RÚNICOS + GRAN SELLADO + el
    ///                         cometa orbital (RuneSunRenderer, tier 20).
    ///   · de STORMLIB      → LA CORONA DE RAYOS de 2ª generación: arcos
    ///                         crispados + multi-boltos con ramas.
    ///   · de LUMENLIB      → LA LUZ: rayos prismáticos radiando + el
    ///                         destello de 4 puntas del corazón.
    ///
    /// CONTRATO DE BATCH (v6.10): Draw() exige el SpriteBatch CERRADO y lo
    /// deja CERRADO.
    /// </summary>
    public static class EclipsePrimordialRenderer
    {
        // ==================================================================
        //  PARÁMETROS
        // ==================================================================

        /// <summary>Radio de la esfera negra en px a escala 1 (el más grande).</summary>
        public const float SpherePx = 55f;

        // --- EL DISCO DOPPLER (herencia UMBRAL con gradiente AURORA) ---
        private const float DiskA = 2.50f;
        private const float DiskB = 0.92f;
        private const float DiskTilt = -0.22f;
        private const float DopplerAngle = 3.05f;
        private const float StreakFlow = 0.44f;
        private const int StreaksPerHalf = 24;
        private const float FlickHz = 12f;

        // --- EL ANILLO DE BANDAS (herencia CÓSMICO) ---
        private const float BandA = 1.95f;
        private const float BandB = 1.24f;
        private const float BandTilt = -0.38f;
        private const float BandFreq = 20f;
        private const float BandSpeed = 5f;
        private const float BandSpin = 0.349f;
        private const int BandSegments = 44;

        // --- LOS BRAZOS ESPIRALES (herencia OLVIDO) ---
        private const int ArmCount = 3;
        private const int ArmSteps = 13;

        // --- EL SISTEMA SOLAR RÚNICO (herencia SOL XX) ---
        private const float SunOrbitR = 0.55f;     // R de los anillos (×esfera)
        private const int SunTier = 20;

        // --- EL DOBLE CÍRCULO DE RUNAS ---
        private const int GoldRuneCount = 8;
        private const float GoldRuneRadius = 2.62f;
        private const float GoldRuneOrbit = 0.10f;
        private const int VioletRuneCount = 6;
        private const float VioletRuneRadius = 3.30f;
        private const float VioletRuneOrbit = -0.075f;

        // --- LOS RAYOS (StormLib — la 2ª generación) ---
        private const float BoltHz = 10f;

        // --- LAS VOLUTAS Y EL HUMO (herencia BRUMA) ---
        private const int TendrilCount = 2;
        private const float TendrilStart = 2.95f;
        private const float TendrilSweep = 2.2f;

        // --- ONDAS DE DISTORSIÓN ---
        private const float WaveCycle = 2.5f;
        private const int WaveCount = 3;

        // ==================================================================
        //  PALETA DEL ECLIPSE — el gradiente AURORA + el oro regio
        // ==================================================================

        private static readonly Color WhiteIncan = new(255, 248, 235);
        private static readonly Color SupGold = new(255, 190, 80);
        private static readonly Color SupViolet = new(150, 80, 255);
        private static readonly Color RuneGold = new(255, 180, 70);
        private static readonly Color RuneGoldTip = new(255, 235, 175);
        private static readonly Color RuneViolet = new(110, 130, 255);
        private static readonly Color RuneVioletTip = new(205, 220, 255);
        private static readonly Color NebGold = new(190, 130, 40);
        private static readonly Color NebViolet = new(80, 50, 170);
        private static readonly Color WarmWhite = new(255, 225, 175);

        // EL HUMO DEL VACÍO (v6.24): la bruma que vive DENTRO de la luna negra.
        private static readonly Color SmokeViolet = new(108, 72, 160);
        private static readonly Color SmokePurple = new(140, 96, 186);
        private static readonly Color SmokeEmber = new(158, 110, 62);
        private static readonly Color EmberGlow = new(150, 95, 205);

        // EL GRADIENTE AURORA (herencia del Supremo Aurora):
        private static readonly Color AurPurple = new(185, 105, 255);
        private static readonly Color AurBlue = new(92, 150, 255);
        private static readonly Color AurGold = new(255, 195, 90);

        /// <summary>EL GRADIENTE como función: morado → azul → dorado.</summary>
        private static Color AuroraGrad(float t)
        {
            t = MathHelper.Clamp(t, 0f, 1f);
            if (t < 0.45f) return Color.Lerp(AurPurple, AurBlue, t / 0.45f);
            return Color.Lerp(AurBlue, AurGold, (t - 0.45f) / 0.55f);
        }

        // ==================================================================
        //  PINCELES
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
        //  EL RENDER COMPLETO — contrato: batch CERRADO → CERRADO
        // ==================================================================

        public static void Draw(Vector2 center, float scale, float time, int seed)
        {
            float r = SpherePx * Math.Max(scale, 0.02f);
            if (r < 2f) return;

            try
            {
                float tSlow = time * 0.55f;
                float tMid = time;
                float tFast = time * 1.55f;

                int flick = (int)(tMid * FlickHz);
                int boltFlick = StormLib.FlickTick(time, BoltHz);

                float breathe = 1f + 0.012f * (float)Math.Sin(tMid * 1.15f);
                float rr = r * breathe;

                // ============ 1. AURA OSCURA (alfa) — el vacío absorbe ============
                BeginAlpha();
                Quad(Glow, center, new Vector2(7.6f * rr, 7.6f * rr), 0f,
                    new Color(8, 4, 16, 182));
                Main.spriteBatch.End();

                // ============ 2..17: TODO LO BRILLANTE (aditivo) ============
                BeginAdditive();

                // --- 2. NEBULOSAS aurora + polvo ambiental ---
                DrawNebulas(center, rr, tSlow, seed);

                // --- 3. EL HALO DE BRUMA — 2 Cloud + 1 PUFF de humo vivo ---
                BrumaFX.Cloud(center, 2.7f * rr, NebGold, seed + 11, tSlow * 0.8f,
                    puffs: 5, alpha: 0.24f);
                BrumaFX.Cloud(center, 3.4f * rr, SupViolet, seed + 47, tSlow * 0.6f,
                    puffs: 4, alpha: 0.16f);
                // EL HUMO que respira (el aliento del eclipse).
                BrumaFX.Puff(center + new Vector2(0f, -1.9f * rr), 1.35f * rr,
                    new Color(120, 90, 160), seed + 83, tSlow,
                    alpha: 0.16f + 0.05f * (float)Math.Sin(tSlow * 0.9f), quality: 0.7f);

                // --- 4. EL SISTEMA SOLAR RÚNICO XX (LA HERENCIA DEL SOL):
                //     los 20 anillos + gran sellado + cometa, orbitando el
                //     horizonte (SIN cuerpo solar: el agujero lo devora) ---
                RuneSunRenderer.DrawOrbitalSystem(center, rr * SunOrbitR, time,
                    seed + 31, SunTier, 1f);

                // --- 5. EL DISCO DOPPLER — mitad TRASERA (gradiente aurora) ---
                DrawDopplerDisk(center, rr, tMid, seed, flick, front: false);

                // --- 5b. ECO TENUE del anillo de bandas (mitad trasera) ---
                DrawBandRing(center, rr, tFast, seed, flick, front: false);

                // --- 6. NÚCLEO negro absoluto + rim DORADO pulsante ---
                Main.spriteBatch.End();
                BeginAlpha();
                Quad(Disk, center, new Vector2(2.28f * r, 2.28f * r), 0f, Color.White);
                Main.spriteBatch.End();
                BeginAdditive();
                RingQuad(center, 1.02f * r, tMid * 0.13f,
                    Tint(SupGold, 0.42f + 0.14f * (float)Math.Sin(tMid * 1.6f)));

                // --- 7. EL ANILLO DE BANDAS — mitad DELANTERA (aurora) ---
                DrawBandRing(center, rr, tFast, seed, flick, front: true);

                // --- 8. BRAZOS ESPIRALES con flujo hacia adentro ---
                DrawSpiralArms(center, rr, tMid, seed);

                // --- 9. ANILLO DE FOTONES + corredores ---
                RingQuad(center, 1.10f * r, tFast * 0.2f,
                    Tint(WarmWhite, 0.20f + 0.08f * (float)Math.Sin(tFast * 2.1f)));
                DrawPhotonRunners(center, rr, tFast, seed);

                // --- 10. ⚡ STORMLIB — LA CORONA DE DESCARGA (2ª generación) ---
                DrawHorizonArcs(center, r, time, seed, boltFlick);
                DrawEscapingBolts(center, rr, tFast, seed, boltFlick);

                // --- 11. DOBLE CÍRCULO DE RUNAS (CW + CCW) ---
                DrawRuneCircles(center, r, tSlow, seed);

                // --- 12. JETS POLARES (oro arriba, aurora abajo) ---
                DrawPolarJets(center, rr, tMid, seed, boltFlick);

                // --- 13. VOLUTAS cayendo (BrumaFX.Tendril) ---
                DrawFallingTendrils(center, r, tSlow, seed);

                // --- 14. ONDAS DE DISTORSIÓN ×3 ---
                DrawDistortionWaves(center, r, tMid, seed);

                // --- 15. ✨ LUMENLIB — LA LUZ PRISMÁTICA radiando ---
                DrawPrismaticLight(center, r, rr, time, seed);

                // --- 15b. EL AURA FINAL GRANDE pulsante (aurora completa) —
                //     v6.23: AHORA ANTES del repintado: el vacío negro la
                //     DEVORA en el centro y el aura queda como halo ALREDEDOR
                //     del disco negro (antes lavaba de dorado el corazón) ---
                float aura = 0.85f + 0.15f * (float)Math.Sin(tFast * 1.4f);
                Quad(Glow, center, new Vector2(6.6f * rr, 6.6f * rr), 0f,
                    Tint(AurGold, 0.18f * aura));
                RingQuad(center, 3.1f * rr, -tMid * 0.08f,
                    Tint(AurPurple, 0.10f * aura));

                // --- 15c. EL SOL DE VERDAD (v6.24 — petición del usuario:
                //     "no se le ve el sol"): LA CORONA SOLAR DEL ECLIPSE —
                //     el halo caliente del sol vivo + sus STREAMERS
                //     radiando del limbo, pintados ANTES del repintado para
                //     que el vacío se coma el centro y el SOL quede como el
                //     resplandor anular de un eclipse TOTAL de verdad ---
                DrawSolarCorona(center, r, tMid, seed);

                // --- 16. EL VACÍO VUELVE A DEVORAR: repintado del núcleo ---
                Main.spriteBatch.End();
                BeginAlpha();
                Quad(Disk, center, new Vector2(2.28f * r, 2.28f * r), 0f, Color.White);
                Main.spriteBatch.End();
                BeginAdditive();
                RingQuad(center, 1.02f * r, tMid * 0.13f,
                    Tint(SupGold, 0.30f + 0.10f * (float)Math.Sin(tMid * 1.6f)));

                // --- 16b. EL LIMBO DEL ECLIPSE (v6.24): el filo BLANCO-CÁLIDO
                //     del sol asomando al borde de la luna negra (ahora con
                //     la fuerza de un limbo solar de verdad) + el jade dorado ---
                RingQuad(center, 1.035f * r, -tMid * 0.10f,
                    Tint(WarmWhite, 0.55f + 0.18f * (float)Math.Sin(tMid * 2.3f)));
                RingQuad(center, 1.13f * r, tMid * 0.07f,
                    Tint(AurGold, 0.16f + 0.06f * (float)Math.Sin(tMid * 1.7f + 1.1f)));

                // --- 16c. EL ANILLO DE DIAMANTE: el destello de 4 puntas
                //     sobre la luna negra (sutil — el centro sigue siendo
                //     la luna; solo el LATIDO de la gema) ---
                float beat = (float)Math.Pow(0.5f + 0.5f * (float)Math.Sin(tMid * 3.1f), 2.0f);
                LumenLib.Flare(Main.spriteBatch, center, r * (1.05f + 0.16f * beat),
                    WarmWhite, 0.14f + 0.10f * beat, tMid * 0.22f);

                // --- 16d. EL HUMO DEL VACÍO (v6.24 — petición del usuario:
                //     "mucho humo o bruma" en el centro): la luna negra
                //     NO es un punto muerto — MUCHA bruma viva girando
                //     DENTRO del disco, pintada con blending ALFA para que
                //     sea MASA de verdad sobre el negro absoluto ---
                Main.spriteBatch.End();
                BeginAlpha();
                DrawVoidSmoke(center, r, tSlow, seed);
                Main.spriteBatch.End();
                BeginAdditive();

                // LA BRASA VIOLETA: el latido de luz bajo el humo (el
                // centro respira — nunca un punto muerto del todo).
                Quad(Glow, center, new Vector2(0.95f * r, 0.95f * r), 0f,
                    Tint(EmberGlow, 0.09f + 0.05f * (float)Math.Sin(tSlow * 1.7f)));

                Main.spriteBatch.End();
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }
        }

        // ------------------------------------------------------------------
        //  2. NEBULOSAS AURORA + POLVO AMBIENTAL
        // ------------------------------------------------------------------

        private static void DrawNebulas(Vector2 center, float rr, float time, int seed)
        {
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

            for (int i = 0; i < 14; i++)
            {
                float h = Hash01(seed, 600 + i, 13);
                float ang = h * MathHelper.TwoPi + time * 0.03f * (i % 2 == 0 ? 1f : -1f);
                float dist = (2.2f + 3.1f * Hash01(seed, 601 + i, 19)) * rr * 0.55f;
                Vector2 pos = center + new Vector2(
                    (float)Math.Cos(ang) * dist, (float)Math.Sin(ang) * dist);
                float pulse = 0.5f + 0.5f * (float)Math.Sin(time * 1.5f + i * 2.4f);
                Color c = h < 0.35f ? new Color(185, 105, 255)   // morado aurora
                        : h < 0.70f ? new Color(92, 150, 255)    // azul aurora
                        : new Color(255, 195, 90);               // dorado aurora
                float size = (0.10f + 0.10f * h) * rr;
                Quad(Glow, pos, new Vector2(size, size), 0f, Tint(c, 0.45f * pulse));
            }
        }

        // ------------------------------------------------------------------
        //  5. EL DISCO DOPPLER — estrías pintadas con GRADIENTE AURORA
        // ------------------------------------------------------------------

        private static void DrawDopplerDisk(Vector2 center, float rr, float time, int seed,
            int flick, bool front)
        {
            float t0 = MathHelper.Pi;
            float span = MathHelper.Pi;
            float a = DiskA * rr, b = DiskB * rr;

            // EL BEAMING DOPPLER: el lado que se acerca BRILLA (blanco-oro);
            // el que se aleja se apaga (morado profundo).
            float beamAng = DopplerAngle;
            float cosBeam = (float)Math.Cos(beamAng);
            float sinBeam = (float)Math.Sin(beamAng);

            int streaks = StreaksPerHalf;
            for (int i = 0; i < streaks; i++)
            {
                float f = i / (float)(streaks - 1);
                float ang = t0 + span * f - time * StreakFlow * 0.12f;
                float ct = (float)Math.Cos(ang), st = (float)Math.Sin(ang);
                Vector2 local = new Vector2(a * ct, b * st);
                float cR = (float)Math.Cos(DiskTilt), sR = (float)Math.Sin(DiskTilt);
                Vector2 pos = center + new Vector2(
                    local.X * cR - local.Y * sR, local.X * sR + local.Y * cR);

                // La dirección Doppler en este punto (la proyección).
                Vector2 radial = pos - center;
                float doppler = radial.X * cosBeam + radial.Y * sinBeam;
                float dop = MathHelper.Clamp((doppler / (a * 0.7f)) * 0.5f + 0.5f, 0f, 1f);

                // LA ESTRÍA: segmento entre dos radios del disco.
                float r0 = 1.18f, r1 = 2.42f;
                Vector2 dir = radial / Math.Max(radial.Length(), 0.01f);
                Vector2 p0 = center + dir * (r0 * rr);
                Vector2 p1 = center + dir * (r1 * rr);
                Vector2 mid = (p0 + p1) * 0.5f;
                float len = (p1 - p0).Length();
                float rot = (float)Math.Atan2(p1.Y - p0.Y, p1.X - p0.X);

                // EL COLOR: el gradiente aurora por Doppler — el lado que se
                // acerca vira a BLANCO-DORADO, el que se aleja al MORADO.
                Color grad = AuroraGrad(0.15f + 0.75f * dop);
                Color hot = Color.Lerp(grad, WhiteIncan, 0.45f * dop);
                float pulse = 0.75f + 0.25f *
                    (float)Math.Sin(time * 2.2f + i * 1.31f + Hash01(seed, 71 + i, 3));

                Capsule(mid, len, Math.Max(3f, 0.11f * rr) * (0.8f + 0.5f * dop),
                    rot, Tint(hot, (0.24f + 0.30f * dop) * pulse));
            }
        }

        // ------------------------------------------------------------------
        //  7. EL ANILLO DE BANDAS — glow = sin(θ·20 + t·5), AURORA
        // ------------------------------------------------------------------

        private static void DrawBandRing(Vector2 center, float rr, float time, int seed,
            int flick, bool front)
        {
            float a = BandA * rr, b = BandB * rr;
            float cR = (float)Math.Cos(BandTilt), sR = (float)Math.Sin(BandTilt);
            float spin = time * BandSpin;

            int segs = BandSegments;
            Vector2 prev = default;
            for (int s = 0; s <= segs; s++)
            {
                float t = s / (float)segs;
                float ang = t * MathHelper.TwoPi + spin;
                // MITAD: solo delantera o trasera según `front`.
                bool isFront = (float)Math.Sin(ang - BandTilt) > 0f;
                if (isFront != front) { prev = default; continue; }

                float ct = (float)Math.Cos(ang), st = (float)Math.Sin(ang);
                Vector2 local = new Vector2(a * ct, b * st);
                Vector2 pos = center + new Vector2(
                    local.X * cR - local.Y * sR, local.X * sR + local.Y * cR);

                if (prev != default)
                {
                    Vector2 mid = (prev + pos) * 0.5f;
                    Vector2 delta = pos - prev;
                    float len = delta.Length();
                    if (len > 0.5f)
                    {
                        float rot = (float)Math.Atan2(delta.Y, delta.X);
                        // LA FÓRMULA DEL SHADER: glow = sin(θ·20 + t·5)·0.5+0.5
                        float glow = 0.5f + 0.5f * (float)Math.Sin(ang * BandFreq + time * BandSpeed);
                        // EL COLOR AURORA a lo largo del anillo (gradiente angular).
                        Color band = AuroraGrad(0.5f + 0.5f * (float)Math.Sin(ang * 0.7f + time * 0.3f));
                        Color hot = Color.Lerp(band, WhiteIncan, glow * 0.35f);
                        Capsule(mid, len + 1.5f, Math.Max(3.4f, 0.085f * rr) * (0.7f + 0.7f * glow),
                            rot, Tint(hot, (0.16f + 0.34f * glow)));
                    }
                }
                prev = pos;
            }
        }

        // ------------------------------------------------------------------
        //  8. LOS BRAZOS ESPIRALES con flujo hacia adentro (herencia OLVIDO)
        // ------------------------------------------------------------------

        private static void DrawSpiralArms(Vector2 center, float rr, float time, int seed)
        {
            for (int arm = 0; arm < ArmCount; arm++)
            {
                float baseT = time * 0.30f + arm * (MathHelper.TwoPi / ArmCount);
                Vector2 prev = default;
                for (int k = 0; k <= ArmSteps; k++)
                {
                    float f = 1f - k / (float)ArmSteps;          // 1 (fuera) → 0 (anillo)
                    float t = baseT + f * 2.4f;
                    float grow = 1f + f * 1.35f;
                    float aa = 2.0f * rr * grow, bb = 0.78f * rr * grow;
                    Vector2 pos = VFXCore.Ellipse(center, aa, bb, -0.18f, t);

                    if (prev != default)
                    {
                        Vector2 mid = (prev + pos) * 0.5f;
                        Vector2 delta = pos - prev;
                        float len = delta.Length();
                        if (len > 0.5f)
                        {
                            float rot = (float)Math.Atan2(delta.Y, delta.X);
                            Color c = AuroraGrad(0.25f + 0.55f * f);
                            Capsule(mid, len, Math.Max(2.5f, 0.07f * rr) * (0.5f + 0.6f * f),
                                rot, Tint(c, 0.16f + 0.18f * f));
                        }
                        // EL FLUJO: puntos de materia espiralando HACIA ADENTRO.
                        if (k % 3 == 1)
                        {
                            float flow = (time * 0.9f + k * 0.37f + arm * 0.7f) % 1f;
                            float fk = MathHelper.Lerp(k - 2, k + 1, flow);
                            float ff = 1f - fk / (float)ArmSteps;
                            float tt = baseT + ff * 2.4f;
                            float gg = 1f + ff * 1.35f;
                            Vector2 fp = VFXCore.Ellipse(center,
                                2.0f * rr * gg, 0.78f * rr * gg, -0.18f, tt);
                            Quad(Glow, fp, new Vector2(0.14f * rr, 0.14f * rr), 0f,
                                Tint(Color.Lerp(AurBlue, WhiteIncan, 0.4f), 0.50f * (1f - flow)));
                        }
                    }
                    prev = pos;
                }
            }
        }

        // ------------------------------------------------------------------
        //  9. LOS CORREDORES DE FOTONES
        // ------------------------------------------------------------------

        private static void DrawPhotonRunners(Vector2 center, float rr, float time, int seed)
        {
            for (int i = 0; i < 5; i++)
            {
                float speed = 1.5f + 0.6f * Hash01(seed, 811 + i, 5);
                float ang = time * speed + i * (MathHelper.TwoPi / 5f);
                float dist = 1.42f * rr * (1f + 0.12f * (float)Math.Sin(time * 2.2f + i));
                Vector2 pos = center + new Vector2(
                    (float)Math.Cos(ang) * dist, (float)Math.Sin(ang) * dist * 0.86f);
                Quad(Glow, pos, new Vector2(0.20f * rr, 0.20f * rr), 0f,
                    Tint(WarmWhite, 0.55f));
                Quad(Glow, pos, new Vector2(0.08f * rr, 0.08f * rr), 0f,
                    Tint(WhiteIncan, 0.90f));
            }
        }

        // ------------------------------------------------------------------
        //  10. ⚡ LA CORONA DE DESCARGA — StormLib 2ª generación
        // ------------------------------------------------------------------

        private static void DrawHorizonArcs(Vector2 center, float r, float time, int seed,
            int boltFlick)
        {
            for (int c = 0; c < 3; c++)
            {
                if (!StormLib.IsLit(seed + 40 + c * 17, boltFlick, 0.82f)) continue;
                float drift = time * (0.9f + 0.25f * c) + c * 2.1f;
                float span = 1.15f + 0.55f * Hash01(seed, 941 + c, boltFlick);
                float radius = (1.02f + 0.10f * c) * r;

                StormLib.ArcRing(Main.spriteBatch, center, radius,
                    drift, drift + span, seed + 500 + c * 13, boltFlick,
                    Math.Max(2.6f, 0.052f * r),
                    Tint(AurGold, 0.46f), Tint(WhiteIncan, 0.90f), 1f, 11);
            }
        }

        private static void DrawEscapingBolts(Vector2 center, float rr, float time, int seed,
            int boltFlick)
        {
            for (int i = 0; i < 3; i++)
            {
                if (!StormLib.IsLit(seed + 61 + i, boltFlick, 0.85f)) continue;

                float bandPeak = -time * BandSpeed / BandFreq + i * (MathHelper.TwoPi / 3f);
                float t = bandPeak + 0.5f * Hash01(seed, 851 + i, boltFlick / 3);
                Vector2 start = VFXCore.Ellipse(center, BandA * rr, BandB * rr, BandTilt, t);
                Vector2 outward = start - center;
                if (outward.LengthSquared() < 0.01f) continue;
                outward.Normalize();

                float swirl = 0.25f + 0.25f * Hash01(seed, 858 + i, boltFlick);
                Vector2 tangent = new Vector2(-outward.Y, outward.X) * swirl;
                Vector2 end = start + (outward + tangent) *
                              (1.10f + 0.80f * Hash01(seed, 852 + i, boltFlick)) * rr;

                // EL MULTI-BOLTO de 2ª generación (filamentos + ramas + pelos).
                StormLib.MultiBolt(Main.spriteBatch, start, end,
                    seed + 100 + i * 53, boltFlick,
                    Math.Max(2.8f, 0.055f * rr),
                    Tint(AurGold, 0.52f), Tint(AurPurple, 0.52f), Tint(WhiteIncan, 0.92f),
                    0.95f, Math.Max(11f, 0.18f * rr), 8);
            }
        }

        // ------------------------------------------------------------------
        //  11. EL DOBLE CÍRCULO DE RUNAS (CW + CCW)
        // ------------------------------------------------------------------

        private static void DrawRuneCircles(Vector2 center, float r, float time, int seed)
        {
            // Las DORADAS (CW, dentro).
            float gspin = time * GoldRuneOrbit;
            for (int g = 0; g < GoldRuneCount; g++)
                DrawRune(center, GoldRuneRadius * r, g / (float)GoldRuneCount *
                    MathHelper.TwoPi + gspin, time, g, RuneGold, RuneGoldTip, 1.0f * r);
            // Las VIOLETAS (CCW, fuera).
            float vspin = time * VioletRuneOrbit;
            for (int g = 0; g < VioletRuneCount; g++)
                DrawRune(center, VioletRuneRadius * r, g / (float)VioletRuneCount *
                    MathHelper.TwoPi + vspin, time, g + 40, RuneViolet, RuneVioletTip, 1.0f * r);
        }

        /// <summary>LA RUNA FUSIONADA: tres trazos angulares (la firma del eclipse).</summary>
        private static void DrawRune(Vector2 center, float radius, float ang, float time,
            int idx, Color body, Color tip, float r)
        {
            float breathe = 1f + 0.05f * (float)Math.Sin(time * 1.3f + idx * 0.9f);
            Vector2 pos = center + new Vector2(
                (float)Math.Cos(ang) * radius * breathe,
                (float)Math.Sin(ang) * radius * breathe * 0.88f);
            float pulse = 0.75f + 0.25f * (float)Math.Sin(time * 2.2f + idx * 1.3f);
            float s = Math.Max(r / 55f, 0.25f);

            Quad(Glow, pos, new Vector2(26f * s, 26f * s), 0f, Tint(body, 0.22f * pulse));

            // LA RUNA DEL ECLIPSE: anillo superior + cuña + cola (3 trazos).
            Vector2[] strokes =
            {
                new Vector2(-4.5f, -3f) * s, new Vector2(4.5f, -3f) * s,
                new Vector2(4.5f, -3f) * s, new Vector2(0f, 1f) * s,
                new Vector2(0f, 1f) * s, new Vector2(0f, 6.5f) * s,
            };
            for (int i = 0; i < strokes.Length; i += 2)
            {
                Vector2 a = pos + strokes[i];
                Vector2 b = pos + strokes[i + 1];
                Vector2 mid = (a + b) * 0.5f;
                Vector2 d = b - a;
                float len = d.Length();
                if (len < 0.01f) continue;
                Capsule(mid, len, 2.9f * s, (float)Math.Atan2(d.Y, d.X),
                    Tint(Color.Lerp(tip, body, 0.3f), 0.85f * pulse));
            }

            // LA PERLA sobre la runa.
            Vector2 pearl = pos + new Vector2(0f, -6.5f * s);
            Quad(Glow, pearl, new Vector2(6.4f * s, 6.4f * s), 0f, Tint(body, 0.55f * pulse));
            Quad(Glow, pearl, new Vector2(3.0f * s, 3.0f * s), 0f, Tint(tip, 0.90f * pulse));
        }

        // ------------------------------------------------------------------
        //  12. LOS JETS POLARES (oro arriba, aurora abajo)
        // ------------------------------------------------------------------

        private static void DrawPolarJets(Vector2 center, float rr, float time, int seed,
            int boltFlick)
        {
            for (int side = 0; side < 2; side++)
            {
                Vector2 dir = side == 0 ? -Vector2.UnitY : Vector2.UnitY;
                float rot = (float)Math.Atan2(dir.Y, dir.X);
                float pulse = 0.7f + 0.3f * (float)Math.Sin(time * 2.6f + side * 2.7f);
                float jetLen = (1.65f + 0.30f * (float)Math.Sin(time * 2.2f + side * 1.9f)) * rr;
                float baseOff = 0.92f * rr;

                for (int k = 0; k < 4; k++)
                {
                    float f0 = k / 4f, f1 = (k + 1) / 4f;
                    float midF = (f0 + f1) * 0.5f;
                    Vector2 a = center + dir * (baseOff + f0 * jetLen);
                    Vector2 b = center + dir * (baseOff + f1 * jetLen);
                    Vector2 mid = (a + b) * 0.5f;
                    float len = (b - a).Length();
                    float w = (0.34f - 0.26f * midF) * rr;
                    // EL COLOR DEL JET: arriba ORO, abajo el GRADIENTE AURORA.
                    Color c = side == 0
                        ? Color.Lerp(WhiteIncan, SupGold, midF * 0.7f)
                        : AuroraGrad(0.35f + 0.5f * midF);
                    Capsule(mid, len, w, rot, Tint(c, 0.34f * pulse * (1f - midF * 0.45f)));
                }

                // ⚡ EL RAYO INTERIOR del jet (la espina de StormLib).
                Vector2 start = center + dir * (baseOff * 0.9f);
                Vector2 end = center + dir * (baseOff + jetLen * 1.05f);
                if (StormLib.IsLit(seed + 90 + side * 13, boltFlick, 0.80f))
                    StormLib.Bolt(Main.spriteBatch, start, end,
                        seed + 960 + side * 29, boltFlick,
                        Math.Max(2.4f, 0.045f * rr),
                        Tint(side == 0 ? SupGold : AurPurple, 0.50f), Tint(WhiteIncan, 0.90f),
                        1f, 6, Math.Max(8f, 0.12f * rr));

                Vector2 bpos = center + dir * baseOff;
                Quad(Glow, bpos, new Vector2(0.85f * rr, 0.85f * rr), 0f,
                    Tint(side == 0 ? SupGold : AurPurple, 0.30f * pulse));
                Vector2 tip = center + dir * (baseOff + jetLen * 1.05f);
                Quad(Glow, tip, new Vector2(0.55f * rr, 0.55f * rr), 0f,
                    Tint(WhiteIncan, 0.42f * pulse));
            }
        }

        // ------------------------------------------------------------------
        //  13. LAS VOLUTAS CAYENDO (BrumaFX.Tendril — herencia BRUMA)
        // ------------------------------------------------------------------

        private static void DrawFallingTendrils(Vector2 center, float r, float time, int seed)
        {
            for (int i = 0; i < TendrilCount; i++)
            {
                float baseAng = time * 0.22f + i * MathHelper.Pi;
                Vector2[] path = new Vector2[5];
                for (int k = 0; k < 5; k++)
                {
                    float f = k / 4f;
                    float ang = baseAng + f * TendrilSweep;
                    float dist = MathHelper.Lerp(TendrilStart * r, 1.25f * r, f);
                    path[k] = center + new Vector2(
                        (float)Math.Cos(ang) * dist, (float)Math.Sin(ang) * dist * 0.8f);
                }
                BrumaFX.Tendril(path, Math.Max(10f, 0.30f * r),
                    i % 2 == 0 ? new Color(150, 110, 200) : new Color(200, 160, 90),
                    seed + 517 + i * 37, time, alpha: 0.20f, fade: 0.8f);
            }
        }

        // ------------------------------------------------------------------
        //  14. LAS ONDAS DE DISTORSIÓN ×3
        // ------------------------------------------------------------------

        private static void DrawDistortionWaves(Vector2 center, float r, float time, int seed)
        {
            for (int w = 0; w < WaveCount; w++)
            {
                float phase = (time / WaveCycle + w / (float)WaveCount) % 1f;
                float radius = (1.35f + phase * 1.9f) * r;
                float fade = (1f - phase) * (1f - phase);
                RingQuad(center, radius, phase * 3.4f + w * 2.1f,
                    Tint(AuroraGrad(phase), 0.20f * fade));
            }
        }

        // ------------------------------------------------------------------
        //  15. ✨ LA LUZ PRISMÁTICA — LumenLib radiando del horizonte
        // ------------------------------------------------------------------

        private static void DrawPrismaticLight(Vector2 center, float r, float rr,
            float time, int seed)
        {
            // LOS RAYOS PRISMÁTICOS radiando del borde del horizonte.
            const int Rays = 8;
            for (int i = 0; i < Rays; i++)
            {
                float drift = LumenLib.Drift(time, seed + i * 13, 0.24f);
                Color col = LumenLib.Hue(drift, 0.55f, 1f);
                float ang = i / (float)Rays * MathHelper.TwoPi + time * 0.11f;
                Vector2 dir = new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang));
                Vector2 origin = center + dir * (r * 1.12f);

                float len = (1.6f + 1.0f * Hash01(seed, 1700 + i, 19)) * r;
                float pulse = 0.5f + 0.5f * (float)Math.Sin(time * 2.3f + i * 1.87f);
                LumenLib.Ray(Main.spriteBatch, origin, dir, len,
                    Math.Max(5f, 0.10f * rr), col, 0.36f + 0.26f * pulse, pulse);
            }

            // EL CORAZÓN: el destello de 4 puntas del eclipse (la fusión
            // latiendo: dos veces por ciclo, como un corazón doble).
            float beat = (float)Math.Pow(0.5f + 0.5f * (float)Math.Sin(time * 3.1f), 2.0f);
            LumenLib.Flare(Main.spriteBatch, center, r * (1.15f + 0.4f * beat),
                Color.Lerp(AurGold, WhiteIncan, 0.35f), 0.22f + 0.38f * beat, time * 0.30f);

            // LA AURORA DE BANDAS prismáticas girando lejos (el velo).
            LumenLib.Aurora(Main.spriteBatch, center, 2.6f * rr, time,
                LumenLib.Drift(time, seed, 0.10f), 0.30f, 12);
        }

        // ------------------------------------------------------------------
        //  15c. EL SOL DE VERDAD — LA CORONA SOLAR DEL ECLIPSE (v6.24)
        // ------------------------------------------------------------------

        /// <summary>
        /// LA CORONA SOLAR: el sol vivo DETRÁS de la luna negra. Se pinta
        /// ANTES del repintado negro: el vacío se come el centro y el sol
        /// queda como el RESPLANDOR ANULAR de un eclipse total — halo
        /// caliente + bloom LumenLib + 9 STREAMERS radiando del limbo.
        /// </summary>
        private static void DrawSolarCorona(Vector2 center, float r, float time, int seed)
        {
            // === 1. EL HALO CALIENTE: el resplandor del sol llenendo el ===
            // ===     borde del disco (respira como la corona solar)     ===
            float breath = 0.92f + 0.08f * (float)Math.Sin(time * 1.3f);
            Quad(Glow, center, new Vector2(3.4f * r * breath, 3.4f * r * breath), 0f,
                Tint(WarmWhite, 0.44f + 0.12f * (float)Math.Sin(time * 1.9f)));
            Quad(Glow, center, new Vector2(2.5f * r, 2.5f * r), 0f,
                Tint(WhiteIncan, 0.38f + 0.10f * (float)Math.Sin(time * 2.1f)));

            // EL BLOOM DEL SOL (LumenLib: capas apiladas invertidas).
            LumenLib.Bloom(Main.spriteBatch, center, 2.1f * r, WarmWhite,
                0.40f + 0.12f * (float)Math.Sin(time * 1.7f), 2);

            // === 2. LOS STREAMERS: rayos de sol radiando del limbo ===
            // ===     (la firma de LumenLib.Ray — cada uno con su pulso) ===
            const int Streamers = 9;
            for (int i = 0; i < Streamers; i++)
            {
                float ang = i / (float)Streamers * MathHelper.TwoPi + time * 0.05f;
                Vector2 dir = new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang));
                Vector2 origin = center + dir * (r * 1.02f);

                float len = (0.75f + 0.85f * Hash01(seed, 2300 + i, 23)) * r;
                float pulse = 0.45f + 0.55f * (float)Math.Sin(time * 1.7f + i * 1.9f);
                Color col = Color.Lerp(WarmWhite, SupGold, 0.40f);

                LumenLib.Ray(Main.spriteBatch, origin, dir, len,
                    Math.Max(7f, 0.18f * r), col, 0.36f + 0.34f * pulse, pulse);
            }
        }

        // ------------------------------------------------------------------
        //  16d. EL HUMO DEL VACÍO — la luna negra VIVA (v6.24)
        // ------------------------------------------------------------------

        /// <summary>
        /// EL HUMO DEL VACÍO: MUCHA bruma girando DENTRO del disco negro
        /// (blending ALFA — masa de verdad sobre el negro absoluto):
        /// la masa central que respira + 6 volátiles orbitando CW/CCW +
        /// 2 volutas espiralando hacia el centro. El centro del eclipse
        /// NUNCA es un punto muerto.
        /// </summary>
        private static void DrawVoidSmoke(Vector2 center, float r, float time, int seed)
        {
            // === 1. LA MASA CENTRAL: el corazón del vacío respirando ===
            BrumaFX.Puff(center, 0.40f * r, SmokeViolet, seed + 201, time,
                alpha: 0.32f + 0.10f * (float)Math.Sin(time * 0.8f), quality: 0.8f);

            // === 2. LOS VOLÁTILES ORBITANDO: seis puffs girando DENTRO ===
            // ===     del disco (CW/CCW alternos — dirección propia)   ===
            for (int i = 0; i < 6; i++)
            {
                float dir = i % 2 == 0 ? 1f : -1f;
                float ang = i / 6f * MathHelper.TwoPi + time * 0.16f * dir;
                float dist = (0.30f + 0.30f * Hash01(seed, 2400 + i, 31)) * r;
                Vector2 pos = center + new Vector2(
                    (float)Math.Cos(ang) * dist,
                    (float)Math.Sin(ang) * dist * 0.88f);

                float puffR = (0.22f + 0.12f * Hash01(seed, 2410 + i, 37)) * r;
                Color c = i % 3 == 0 ? SmokeEmber
                        : i % 3 == 1 ? SmokeViolet : SmokePurple;
                float pulse = 0.75f + 0.25f * (float)Math.Sin(time * 1.1f + i * 1.7f);

                BrumaFX.Puff(pos, puffR, c, seed + 2500 + i * 37, time + i * 3f,
                    alpha: 0.30f * pulse, quality: 0.7f);
            }

            // === 3. LAS ESPIRALES: dos volutas serpenteando AL CENTRO ===
            // ===     (la materia del vacío caendo espiral adentro)    ===
            for (int s = 0; s < 2; s++)
            {
                Vector2[] path = new Vector2[5];
                for (int k = 0; k < 5; k++)
                {
                    float f = k / 4f;
                    float ang = time * (0.20f + 0.10f * s) + s * MathHelper.Pi + f * 3.6f;
                    float dist = MathHelper.Lerp(0.92f, 0.18f, f) * r;
                    path[k] = center + new Vector2(
                        (float)Math.Cos(ang) * dist,
                        (float)Math.Sin(ang) * dist);
                }
                BrumaFX.Tendril(path, Math.Max(10f, 0.26f * r),
                    s == 0 ? SmokePurple : SmokeViolet, seed + 2700 + s * 53, time,
                    alpha: 0.22f, fade: 0.85f);
            }
        }

        // ==================================================================
        //  HELPERS DE DIBUJO
        // ==================================================================

        private static void BeginAdditive()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
        }

        private static void BeginAlpha()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
        }

        private static void Quad(Texture2D tex, Vector2 pos, Vector2 size, float rot, Color tint)
        {
            if (tint.A == 0) return;
            Main.spriteBatch.Draw(tex, pos, null, tint, rot,
                new Vector2(tex.Width, tex.Height) * 0.5f,
                size / new Vector2(tex.Width, tex.Height),
                SpriteEffects.None, 0f);
        }

        private static void Capsule(Vector2 mid, float len, float width, float rot, Color tint)
        {
            if (tint.A == 0) return;
            Quad(Glow, mid, new Vector2(len + width, width * 1.9f), rot, tint);
        }

        private static void RingQuad(Vector2 pos, float visibleRadius, float rot, Color tint)
        {
            if (tint.A == 0) return;
            Quad(Ring, pos, VFXCore.RingQuadSize(visibleRadius), rot, tint);
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

        /// <summary>Tinte de INTENSIDAD LINEAL (patrón validado del proyecto).</summary>
        private static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color(c.R, c.G, c.B, (byte)(int)(255f * f));
        }
    }
}
