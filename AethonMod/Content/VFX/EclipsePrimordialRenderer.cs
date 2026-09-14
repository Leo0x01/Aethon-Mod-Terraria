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
    /// EclipsePrimordialRenderer — v6.26 — EL SOL DE LOS 20 ANILLOS + LA
    /// MEZCLA DE TODOS LOS AGUJEROS NEGROS RÚNICOS.
    ///
    /// v6.26 — LA ORDEN DEL USUARIO: "el bastón del eclipse primordial
    /// cámbialo, esta nueva versión será el sol de 20 anillos, y la mezcla
    /// de todos los agujeros negros rúnicos". EL NÚCLEO YA NO ES UN
    /// AGUJERO NEGRO: el cuerpo central es EL SOL DE LOS 20 ANILLOS
    /// (RuneSunRenderer.Draw tier 20 — pintado por el proyectil ANTES de
    /// llamar aquí: SunShader + corona + backglow + 20 anillos + cometa +
    /// lluvia de runas + gran sellado). ESTE renderer ya solo dibuja LA
    /// MEZCLA: las firmas de TODOS los agujeros negros rúnicos orbitando
    /// ALREDEDOR del sol, por FUERA del cuerpo y de sus anillos.
    ///
    /// ELIMINADO (ya no hay luna negra): el repintado del núcleo negro
    /// (BlackDisk ×2), el "aura oscura que absorbe", el disco Doppler
    /// oblicuo y el limbo de eclipse — el sol está ENTERO y visible en
    /// todo su esplendor.
    ///
    /// LA GEOMETRÍA DE ENTRELAZADO: R (radio base de la mezcla) = radio
    /// de los ANILLOS EXTERIORES del sol / 2.02 — así el círculo rúnico
    /// BLANCO íntimo (2.02·R) cabalga EXACTAMENTE el anillo 20 del sol,
    /// y las demás herencias envuelven el sistema por fuera:
    ///   · del SUPREMO   → los TRES círculos rúnicos concéntricos DE PIE
    ///                      con perlas: blanco íntimo 2.02R CW rápido +
    ///                      dorado 2.62R CW lento + violeta 3.30R CCW,
    ///                      con sus aros RingQuad 0.24/0.20/0.18.
    ///   · del CÓSMICO   → el ANILLO DE BANDAS: 20 zonas de brillo
    ///                      viajando (glow = sin(θ·20+t·5)) en elipse que
    ///                      se hunde ENTRE los anillos exteriores.
    ///   · del OLVIDO    → los BRAZOS ESPIRALES con flujo hacia ADENTRO,
    ///                      ahora ALIMENTANDO al sol (la materia cae y se
    ///                      enciende de dorado al llegar al cuerpo).
    ///   · de la BRUMA   → el HALO DE NUBES (BrumaFX.Cloud ×2 + Puff
    ///                      vivo — el aliento) + las VOLUTAS cayendo al
    ///                      cuerpo solar (la acreción invertida).
    ///   · del UMBRAL    → el ANILLO DE FOTONES + corredores de fotones.
    ///   · del AURORA    → el GRADIENTE negro→morado→azul→dorado (aura
    ///                      exterior + nebulosas + polvo cautivo).
    ///   · de STORMLIB   → la CORONA DE DESCARGA: arcos crispados en el
    ///                      horizonte + rayos FUGITIVOS entre los sistemas.
    ///   · de LUMENLIB   → la LUZ PRISMÁTICA radiando del borde.
    ///   · JETS POLARES dobles (oro arriba, aurora abajo) + ONDAS DE
    ///     DISTORSIÓN expandiendo del núcleo.
    ///
    /// CONTRATO DE BATCH (v6.10): Draw()/DrawNova() exigen el SpriteBatch
    /// CERRADO y lo dejan CERRADO.
    /// </summary>
    public static class EclipsePrimordialRenderer
    {
        // ==================================================================
        //  PARÁMETROS
        // ==================================================================

        /// <summary>El Sol XX escala ×1.30 (petición v6.26 — el sol manda).</summary>
        public const float SunScale = 1.30f;

        /// <summary>Radio del CUERPO del sol maestro (px a escala 1).</summary>
        public const float SunBodyPx = RuneSunRenderer.BodyPx * SunScale;

        /// <summary>
        /// Radio del BORDE del sistema de anillos del sol (×radio del
        /// cuerpo): RingA(19) = 1.62 + 0.44·9 + 0.30·10 = 8.58 (ver
        /// RuneSunRenderer — el anillo 20 cierra el sistema).
        /// </summary>
        private const float SunRingEdge = 8.58f;

        /// <summary>
        /// Divisor de la mezcla: R = sunR·SunRingEdge/2.02 → el círculo
        /// BLANCO (2.02R) cabalga el anillo 20 y TODO lo demás queda por
        /// fuera del cuerpo y de los anillos del sol.
        /// </summary>
        private const float MixDiv = 2.02f;

        // --- EL TRIPLE CÍRCULO DE RUNAS (herencia SUPREMO) ---
        private const int WhiteRuneCount = 6;       // blancas íntimas, CW rápido
        private const float WhiteRuneRadius = 2.02f;   // ×R — CABALGA el anillo 20
        private const float WhiteRuneOrbit = 0.16f;    // rad/s — el círculo vivo
        private const int GoldRuneCount = 8;        // doradas, CW lento
        private const float GoldRuneRadius = 2.62f;    // ×R
        private const float GoldRuneOrbit = 0.10f;     // rad/s
        private const int VioletRuneCount = 6;      // azul-violeta, CCW
        private const float VioletRuneRadius = 3.30f;  // ×R — la envoltura exterior
        private const float VioletRuneOrbit = -0.075f; // rad/s — contrarroto

        // --- EL ANILLO DE BANDAS (herencia CÓSMICO — se hunde entre anillos) ---
        private const float BandA = 2.42f;      // semieje mayor (×R)
        private const float BandB = 1.55f;      // semieje menor (×R) — ENTRE los anillos
        private const float BandTilt = -0.38f;
        private const float BandFreq = 20f;
        private const float BandSpeed = 5f;
        private const float BandSpin = 0.349f;
        private const int BandSegments = 44;

        // --- LOS BRAZOS ESPIRALES (herencia OLVIDO — alimentan al sol) ---
        private const int ArmCount = 3;
        private const int ArmSteps = 13;

        // --- EL ANILLO DE FOTONES (herencia UMBRAL) ---
        private const float PhotonRingR = 2.06f;    // ×R — justo el borde

        // --- LOS RAYOS (herencia STORMLIB) ---
        private const float BoltHz = 10f;

        // --- LAS VOLUTAS CAYENDO (herencia BRUMA — acreción invertida) ---
        private const int TendrilCount = 3;
        private const float TendrilStart = 3.45f;   // ×R — donde nacen
        private const float TendrilEnd = 0.55f;     // ×R — mueren EN el cuerpo solar
        private const float TendrilSweep = 2.2f;

        // --- ONDAS DE DISTORSIÓN ---
        private const float WaveCycle = 2.5f;
        private const int WaveCount = 3;

        // ==================================================================
        //  PALETA — el ORO DEL SOL manda, el gradiente AURORA acenta
        // ==================================================================

        private static readonly Color WhiteIncan = new(255, 248, 235);
        private static readonly Color SunGold = new(255, 195, 85);
        private static readonly Color SupGold = new(255, 190, 80);
        private static readonly Color SupViolet = new(150, 80, 255);
        private static readonly Color RuneGold = new(255, 180, 70);
        private static readonly Color RuneGoldTip = new(255, 235, 175);
        private static readonly Color RuneViolet = new(110, 130, 255);
        private static readonly Color RuneVioletTip = new(205, 220, 255);
        private static readonly Color RuneWhite = new(255, 245, 220);
        private static readonly Color RuneWhiteTip = new(255, 252, 240);
        private static readonly Color NebGold = new(190, 130, 40);
        private static readonly Color NebViolet = new(80, 50, 170);
        private static readonly Color WarmWhite = new(255, 225, 175);
        private static readonly Color SmokeViolet = new(108, 72, 160);
        private static readonly Color SmokeEmber = new(158, 110, 62);

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

        private static Texture2D Glow =>
            (_glow ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow")).Value;

        private static Texture2D Ring =>
            (_ring ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Ring")).Value;

        // ==================================================================
        //  EL RENDER DE LA MEZCLA — contrato: batch CERRADO → CERRADO
        // ==================================================================

        /// <summary>
        /// Dibuja LA MEZCLA DE TODOS LOS AGUJEROS NEGROS RÚNICOS alrededor
        /// del sol. `sunR` = radio del CUERPO del sol (ya con el ×1.30);
        /// `alphaMul` permite desvanecerla (la nova se la come al final).
        /// </summary>
        public static void Draw(Vector2 center, float sunR, float time, int seed,
            float alphaMul = 1f)
        {
            if (sunR < 2f || alphaMul <= 0.02f) return;

            // EL R DE LA MEZCLA: el entrelazado con los anillos del sol.
            float R = sunR * SunRingEdge / MixDiv;
            if (R < 4f) return;

            try
            {
                float tSlow = time * 0.55f;
                float tMid = time;
                float tFast = time * 1.55f;

                int boltFlick = StormLib.FlickTick(time, BoltHz);

                float breathe = 1f + 0.012f * (float)Math.Sin(tMid * 1.15f);
                float rr = R * breathe;
                float aMul = alphaMul;

                // EL SOL ESTÁ ENTERO: todo lo de aquí BRILLA (nada devora nada).
                BeginAdditive();

                // ============ 1. EL AURA AURORA (herencia AURORA) ============
                // El gradiente negro→morado→azul→dorado envolviendo el
                // sistema completo: el velo de la fusión.
                float aura = 0.85f + 0.15f * (float)Math.Sin(tFast * 1.4f);
                Quad(Glow, center, new Vector2(3.35f * rr, 3.35f * rr), 0f,
                    Tint(AurGold, 0.15f * aura * aMul));
                RingQuad(center, 3.55f * rr, -tMid * 0.08f,
                    Tint(AurPurple, 0.10f * aura * aMul));
                DrawNebulas(center, rr, tSlow, seed, aMul);

                // ============ 2. EL HALO DE BRUMA (herencia BRUMA) ============
                // Dos Cloud + el PUFF DEL ALIENTO: el resuello del coloso.
                BrumaFX.Cloud(center, 2.75f * rr, NebGold, seed + 11, tSlow * 0.8f,
                    puffs: 5, alpha: 0.22f * aMul);
                BrumaFX.Cloud(center, 3.45f * rr, SupViolet, seed + 47, tSlow * 0.6f,
                    puffs: 4, alpha: 0.15f * aMul);
                BrumaFX.Puff(center + new Vector2(0f, -2.35f * rr), 1.15f * rr,
                    new Color(120, 90, 160), seed + 83, tSlow,
                    alpha: (0.14f + 0.05f * (float)Math.Sin(tSlow * 0.9f)) * aMul,
                    quality: 0.7f);

                // ============ 3. LOS BRAZOS ESPIRALES (herencia OLVIDO) ============
                // El flujo ahora es ALIMENTACIÓN: la materia cae de fuera y
                // se enciende de DORADO al acercarse al cuerpo solar.
                DrawSpiralArms(center, rr, tMid, seed, aMul);

                // ============ 4. EL ANILLO DE BANDAS — mitad TRASERA ============
                // (herencia CÓSMICO: 20 zonas de brillo viajando).
                DrawBandRing(center, rr, tFast, seed, front: false, aMul);

                // ============ 5. EL ANILLO DE FOTONES + CORREDORES ============
                // (herencia UMBRAL — el borde donde la luz se enamora).
                RingQuad(center, PhotonRingR * rr, tFast * 0.2f,
                    Tint(WarmWhite, (0.20f + 0.08f * (float)Math.Sin(tFast * 2.1f)) * aMul));
                DrawPhotonRunners(center, rr, tFast, seed, aMul);

                // ============ 6. LOS TRES CÍRCULOS RÚNICOS (herencia SUPREMO) =
                // Blanco íntimo cabalgando el anillo 20 + dorado + violeta.
                DrawRuneCircles(center, rr, tSlow, seed, aMul);

                // ============ 7. EL ANILLO DE BANDAS — mitad DELANTERA ============
                DrawBandRing(center, rr, tFast, seed, front: true, aMul);

                // ============ 8. ⚡ LA CORONA DE DESCARGA (herencia STORMLIB) =
                // Arcos crispados en el horizonte + RAYOS FUGITIVOS saltando
                // del anillo de bandas hacia afuera (entre los sistemas).
                DrawHorizonArcs(center, R, time, seed, boltFlick, aMul);
                DrawEscapingBolts(center, rr, tFast, seed, boltFlick, aMul);

                // ============ 9. LOS JETS POLARES DOBLES ============
                // ORO arriba, AURORA abajo — el par magnético del coloso.
                DrawPolarJets(center, rr, tMid, seed, boltFlick, aMul);

                // ============ 10. LAS ONDAS DE DISTORSIÓN ×3 ============
                DrawDistortionWaves(center, R, tMid, seed, aMul);

                // ============ 11. LAS VOLUTAS CAYENDO (herencia BRUMA) ============
                // LA ACRECIÓN INVERTIDA: bruma cayendo DESDE las firmas
                // exteriores HACIA el cuerpo solar (el sol se la come).
                DrawFallingTendrils(center, R, tSlow, seed, aMul);

                // ============ 12. ✨ LA LUZ PRISMÁTICA (herencia LUMENLIB) ====
                DrawPrismaticLight(center, R, rr, time, seed, aMul);

                Main.spriteBatch.End();
            }
            catch
            {
                // Cierre defensivo (contrato v6.10 — el path de error).
                try { Main.spriteBatch.End(); } catch { }
            }
        }

        // ==================================================================
        //  LA NOVA DEL ECLIPSE — el final con TODAS las librerías
        // ==================================================================

        /// <summary>
        /// v6.26 — LA NOVA VISUAL DEL ECLIPSE (solo cliente): OndaLib.Shock
        /// doble + el anillo de Einstein + BrumaFX.Cloud expansivo + rayos
        /// MultiBolt radiales de StormLib + LumenLib.Aurora + ImpactFlash
        /// CONCENTRADO en el proyectil. PROHIBIDO OndaLib.Flash de pantalla
        /// completa (lección v6.26): aquí el estallido VIVE en el punto.
        /// `progress` 0..1 de la nova. Contrato: batch CERRADO → CERRADO.
        /// </summary>
        public static void DrawNova(Vector2 center, float progress, float time, int seed)
        {
            progress = MathHelper.Clamp(progress, 0f, 1f);
            if (progress <= 0f || progress >= 1f) return;

            try
            {
                float fade = 1f - progress;
                float ease = OndaLib.Expansion(progress);
                int boltFlick = StormLib.FlickTick(time, 14f);

                BeginAdditive();

                // === 1. ⭕ LAS ONDAS DE CHOQUE (OndaLib.Shock ×2 cromáticas) ===
                OndaLib.Shock(Main.spriteBatch, center, progress, 620f,
                    AurGold, 0.90f, seed + 901, 12f, OndaFalloff.Quadratic, chromatic: true);
                OndaLib.Shock(Main.spriteBatch, center,
                    MathHelper.Clamp(progress * 1.15f, 0f, 1f), 520f,
                    SupViolet, 0.65f, seed + 907, 9f, OndaFalloff.Quadratic, chromatic: true);

                // === 2. EL ANILLO DE EINSTEIN: el aro fino de lente que
                //     corre POR DENTRO del frente de choque ===
                RingQuad(center, 90f + ease * 540f, time * 0.5f,
                    Tint(WhiteIncan, 0.45f * fade * fade));

                // === 3. LA BOLA DE BRUMA EXPANSIVA (BrumaFX.Cloud) — el humo
                //     de la detonación abriéndose como una nebulosa nueva ===
                BrumaFX.Cloud(center, 90f + ease * 480f, SmokeViolet, seed + 913,
                    time * 0.8f, puffs: 6, alpha: 0.30f * fade);
                BrumaFX.Cloud(center, 60f + ease * 330f, SmokeEmber, seed + 929,
                    time * 0.6f, puffs: 4, alpha: 0.24f * fade);

                // === 4. ⚡ LOS RAYOS RADIALES (StormLib.MultiBolt): seis
                //     descargas fugitivas huyendo del centro de la nova ===
                for (int i = 0; i < 6; i++)
                {
                    if (!StormLib.IsLit(seed + 61 + i, boltFlick, 0.75f)) continue;
                    float ang = i / 6f * MathHelper.TwoPi + time * 0.4f;
                    Vector2 dir = new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang));
                    float len = 140f + ease * 470f;
                    Vector2 start = center + dir * 80f;
                    Vector2 end = center + dir * len +
                        new Vector2(-dir.Y, dir.X) * (60f * Hash01(seed, 951 + i, boltFlick) - 30f);
                    StormLib.MultiBolt(Main.spriteBatch, start, end,
                        seed + 960 + i * 53, boltFlick, 5.5f,
                        Tint(AurGold, 0.52f), Tint(AurPurple, 0.52f), Tint(WhiteIncan, 0.92f),
                        0.95f, 26f, 8);
                }

                // === 5. LAS CORTINAS DE AURORA (LumenLib.Aurora) — el velo
                //     prismático del estallido, abriéndose ===
                LumenLib.Aurora(Main.spriteBatch, center, 260f + ease * 380f, time,
                    LumenLib.Drift(time, seed, 0.10f), 0.35f * fade, 14);

                // === 6. EL ESTALLIDO CONCENTRADO (StormLib.ImpactFlash) —
                //     el destallo VIVE en el proyectil, NO en la pantalla ===
                StormLib.ImpactFlash(Main.spriteBatch, center, 260f, WarmWhite,
                    0.95f * fade + 0.05f, time * 0.35f);
                StormLib.ImpactFlash(Main.spriteBatch, center, 140f, SunGold,
                    0.80f * fade, -time * 0.5f);

                Main.spriteBatch.End();
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }
        }

        // ------------------------------------------------------------------
        //  1. LAS NEBULOSAS AURORA + EL POLVO CAUTIVO (herencia AURORA)
        // ------------------------------------------------------------------

        private static void DrawNebulas(Vector2 center, float rr, float time, int seed,
            float aMul)
        {
            for (int i = 0; i < 6; i++)
            {
                float h = Hash01(seed, 501 + i, 17);
                float dir = i % 2 == 0 ? 1f : -1f;
                float ang = h * MathHelper.TwoPi + time * 0.05f * dir;
                float dist = (2.6f + 0.8f * Hash01(seed, 502 + i, 29)) * rr;
                Vector2 pos = center + new Vector2(
                    (float)Math.Cos(ang) * dist, (float)Math.Sin(ang) * dist * 0.8f);
                float size = (1.9f + 1.0f * Hash01(seed, 503 + i, 41)) * rr;
                Color c = i % 2 == 0 ? NebGold : NebViolet;
                float pulse = 0.75f + 0.25f * (float)Math.Sin(time * 0.7f + i * 1.9f);
                Quad(Glow, pos, new Vector2(size, size), ang,
                    Tint(c, (i % 2 == 0 ? 0.18f : 0.13f) * pulse * aMul));
            }

            // EL POLVO CAUTIVO: motas orbitando la mezcla completa.
            for (int i = 0; i < 14; i++)
            {
                float h = Hash01(seed, 600 + i, 13);
                float ang = h * MathHelper.TwoPi + time * 0.03f * (i % 2 == 0 ? 1f : -1f);
                float dist = (2.1f + 2.4f * Hash01(seed, 601 + i, 19)) * rr;
                Vector2 pos = center + new Vector2(
                    (float)Math.Cos(ang) * dist, (float)Math.Sin(ang) * dist);
                float pulse = 0.5f + 0.5f * (float)Math.Sin(time * 1.5f + i * 2.4f);
                Color c = h < 0.35f ? new Color(185, 105, 255)   // morado aurora
                        : h < 0.70f ? new Color(92, 150, 255)    // azul aurora
                        : new Color(255, 195, 90);               // dorado aurora
                float size = (0.09f + 0.09f * h) * rr;
                Quad(Glow, pos, new Vector2(size, size), 0f, Tint(c, 0.42f * pulse * aMul));
            }
        }

        // ------------------------------------------------------------------
        //  3. LOS BRAZOS ESPIRALES — flujo hacia ADENTRO (herencia OLVIDO)
        // ------------------------------------------------------------------

        private static void DrawSpiralArms(Vector2 center, float rr, float time, int seed,
            float aMul)
        {
            for (int arm = 0; arm < ArmCount; arm++)
            {
                float baseT = time * 0.30f + arm * (MathHelper.TwoPi / ArmCount);
                Vector2 prev = default;
                for (int k = 0; k <= ArmSteps; k++)
                {
                    float f = 1f - k / (float)ArmSteps;          // 1 (fuera) → 0 (el sol)
                    float t = baseT + f * 2.4f;
                    // EL RADIO: de 4.05R (entre las firmas exteriores) a
                    // 0.85R (DENTRO del sistema — la materia CAE al cuerpo).
                    float rad = MathHelper.Lerp(0.85f, 4.05f, f);
                    Vector2 pos = VFXCore.Ellipse(center, rad * rr, rad * 0.40f * rr, -0.18f, t);

                    if (prev != default)
                    {
                        Vector2 mid = (prev + pos) * 0.5f;
                        Vector2 delta = pos - prev;
                        float len = delta.Length();
                        if (len > 0.5f)
                        {
                            float rot = (float)Math.Atan2(delta.Y, delta.X);
                            // EL COLOR: frío (morado) en el borde exterior,
                            // DORADO al caer hacia el sol — la ignición.
                            Color c = AuroraGrad(0.80f - 0.55f * f);
                            Capsule(mid, len, Math.Max(2.5f, 0.07f * rr) * (0.5f + 0.6f * f),
                                rot, Tint(c, (0.15f + 0.17f * f) * aMul));
                        }
                        // EL FLUJO: puntos de materia espiralando HACIA
                        // ADENTRO — nacen fuera brillantes y mueren en el sol.
                        if (k % 3 == 1)
                        {
                            float flow = (time * 0.9f + k * 0.37f + arm * 0.7f) % 1f;
                            float fk = MathHelper.Lerp(k - 2, k + 1, flow);
                            float ff = 1f - fk / (float)ArmSteps;
                            float ft = baseT + ff * 2.4f;
                            float frad = MathHelper.Lerp(0.85f, 4.05f, ff);
                            Vector2 fp = VFXCore.Ellipse(center, frad * rr,
                                frad * 0.40f * rr, -0.18f, ft);
                            Quad(Glow, fp, new Vector2(0.13f * rr, 0.13f * rr), 0f,
                                Tint(Color.Lerp(AurBlue, WhiteIncan, 0.4f),
                                    0.48f * (1f - flow) * aMul));
                        }
                    }
                    prev = pos;
                }
            }
        }

        // ------------------------------------------------------------------
        //  4/7. EL ANILLO DE BANDAS — glow = sin(θ·20 + t·5) (herencia CÓSMICO)
        // ------------------------------------------------------------------

        private static void DrawBandRing(Vector2 center, float rr, float time, int seed,
            bool front, float aMul)
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
                            rot, Tint(hot, (0.15f + 0.32f * glow) * aMul));
                    }
                }
                prev = pos;
            }
        }

        // ------------------------------------------------------------------
        //  5. LOS CORREDORES DE FOTONES (herencia UMBRAL)
        // ------------------------------------------------------------------

        private static void DrawPhotonRunners(Vector2 center, float rr, float time, int seed,
            float aMul)
        {
            for (int i = 0; i < 5; i++)
            {
                float speed = 1.5f + 0.6f * Hash01(seed, 811 + i, 5);
                float ang = time * speed + i * (MathHelper.TwoPi / 5f);
                float dist = 2.42f * rr * (1f + 0.12f * (float)Math.Sin(time * 2.2f + i));
                Vector2 pos = center + new Vector2(
                    (float)Math.Cos(ang) * dist, (float)Math.Sin(ang) * dist * 0.86f);
                Quad(Glow, pos, new Vector2(0.18f * rr, 0.18f * rr), 0f,
                    Tint(WarmWhite, 0.52f * aMul));
                Quad(Glow, pos, new Vector2(0.07f * rr, 0.07f * rr), 0f,
                    Tint(WhiteIncan, 0.88f * aMul));
            }
        }

        // ------------------------------------------------------------------
        //  6. EL TRIPLE CÍRCULO DE RUNAS (herencia SUPREMO) — runas DE PIE
        //      sobre el círculo, con PERLA encima (la técnica DrawRune del
        //      Supremo, intacta) + los aros 0.24/0.20/0.18.
        // ------------------------------------------------------------------

        private static void DrawRuneCircles(Vector2 center, float rr, float time, int seed,
            float aMul)
        {
            float gs = Math.Max(rr / 118f, 0.30f) * 1.15f;

            // EL CÍRCULO BLANCO ÍNTIMO — 6 runas CW rápido, CABALGANDO el
            // anillo 20 del sol (2.02R = el borde del sistema — el lazo).
            RingQuad(center, WhiteRuneRadius * rr, time * WhiteRuneOrbit,
                Tint(RuneWhite, 0.24f * aMul));
            for (int g = 0; g < WhiteRuneCount; g++)
                DrawRune(center, rr, time, g, WhiteRuneRadius,
                    RuneWhite, RuneWhiteTip, gs * 0.92f,
                    WhiteRuneCount, WhiteRuneOrbit, 6, aMul);

            // EL CÍRCULO DORADO — 8 runas CW lento (con el conjunto).
            RingQuad(center, GoldRuneRadius * rr, time * GoldRuneOrbit,
                Tint(RuneGold, 0.20f * aMul));
            for (int g = 0; g < GoldRuneCount; g++)
                DrawRune(center, rr, time, g, GoldRuneRadius,
                    RuneGold, RuneGoldTip, gs,
                    GoldRuneCount, GoldRuneOrbit, 0, aMul);

            // EL CÍRCULO VIOLETA — 6 runas MÁS AFUERA girando CCW (el
            // contrarroto arcano de la mezcla — la envoltura exterior).
            RingQuad(center, VioletRuneRadius * rr, time * VioletRuneOrbit,
                Tint(RuneViolet, 0.18f * aMul));
            for (int g = 0; g < VioletRuneCount; g++)
                DrawRune(center, rr, time, g, VioletRuneRadius,
                    RuneViolet, RuneVioletTip, gs * 0.85f,
                    VioletRuneCount, VioletRuneOrbit, 3, aMul);
        }

        /// <summary>
        /// Una runa del círculo (glifo DE PIE + resplandor + PERLA) — la
        /// técnica exacta del Supremo: el radio respira, el glifo se mece
        /// y la perla de la corona late encima.
        /// </summary>
        private static void DrawRune(Vector2 center, float rr, float time, int g,
            float radius, Color body, Color tip, float glyphScale,
            int count, float orbit, int offset, float aMul)
        {
            float ang = g / (float)count * MathHelper.TwoPi + time * orbit;

            // Flotación viva: el radio respira por glifo y el glifo se mece.
            float floatR = radius * rr +
                           2.4f * glyphScale * (float)Math.Sin(time * 1.35f + g * 0.9f);
            float bobY = 2.0f * glyphScale * (float)Math.Sin(time * 0.85f + g * 1.7f);
            Vector2 glyphPos = center + new Vector2(
                (float)Math.Cos(ang) * floatR,
                (float)Math.Sin(ang) * floatR + bobY);

            // Latido de brillo propio por glifo.
            float pulse = 0.75f + 0.25f * (float)Math.Sin(time * 2.4f + g * 1.3f);

            // Resplandor suave DETRÁS de cada runa.
            Quad(Glow, glyphPos, new Vector2(36f * glyphScale, 36f * glyphScale), 0f,
                Tint(body, 0.20f * pulse * aMul));

            // Trazos: cápsulas, cuerpo → punta pálida (runa DE PIE).
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

                Capsule(mid, len, 3.4f * glyphScale, rot, Tint(col, 0.85f * pulse * aMul));
            }

            // PERLA sobre el glifo (la gema de la corona).
            Vector2 pearlPos = glyphPos - new Vector2(0f, 11.5f * glyphScale);
            float pearlPulse = 0.8f + 0.2f * (float)Math.Sin(time * 3.0f + g * 2.0f);
            Quad(Glow, pearlPos, new Vector2(7.0f * glyphScale, 7.0f * glyphScale), 0f,
                Tint(body, 0.62f * pulse * aMul));
            Quad(Glow, pearlPos, new Vector2(3.2f * glyphScale, 3.2f * glyphScale), 0f,
                Tint(tip, 0.9f * pearlPulse * aMul));
        }

        /// <summary>
        /// Tabla de glifos DE PIE del SUPREMO: cada runa es una lista de
        /// TRAZOS (pares de puntos en espacio local ~11×15). Ocho diseños
        /// angulares originales de la corona suprema (soles, cetros y
        /// tronos — la escritura del agujero que une a los cuatro), aquí
        /// heredados por la mezcla. Dibujados como cápsulas.
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

        // ------------------------------------------------------------------
        //  8. ⚡ LA CORONA DE DESCARGA (herencia STORMLIB)
        // ------------------------------------------------------------------

        private static void DrawHorizonArcs(Vector2 center, float R, float time, int seed,
            int boltFlick, float aMul)
        {
            for (int c = 0; c < 3; c++)
            {
                if (!StormLib.IsLit(seed + 40 + c * 17, boltFlick, 0.82f)) continue;
                float drift = time * (0.9f + 0.25f * c) + c * 2.1f;
                float span = 1.15f + 0.55f * Hash01(seed, 941 + c, boltFlick);
                float radius = (2.06f + 0.11f * c) * R;

                StormLib.ArcRing(Main.spriteBatch, center, radius,
                    drift, drift + span, seed + 500 + c * 13, boltFlick,
                    Math.Max(2.6f, 0.052f * R),
                    Tint(AurGold, 0.46f * aMul), Tint(WhiteIncan, 0.90f * aMul),
                    1f, 11);
            }
        }

        private static void DrawEscapingBolts(Vector2 center, float rr, float time, int seed,
            int boltFlick, float aMul)
        {
            for (int i = 0; i < 3; i++)
            {
                if (!StormLib.IsLit(seed + 61 + i, boltFlick, 0.85f)) continue;

                // Nacen donde BRILLA la banda (la fórmula del shader).
                float bandPeak = -time * BandSpeed / BandFreq + i * (MathHelper.TwoPi / 3f);
                float t = bandPeak + 0.5f * Hash01(seed, 851 + i, boltFlick / 3);
                Vector2 start = VFXCore.Ellipse(center, BandA * rr, BandB * rr, BandTilt, t);
                Vector2 outward = start - center;
                if (outward.LengthSquared() < 0.01f) continue;
                outward.Normalize();

                float swirl = 0.25f + 0.25f * Hash01(seed, 858 + i, boltFlick);
                Vector2 tangent = new Vector2(-outward.Y, outward.X) * swirl;
                Vector2 end = start + (outward + tangent) *
                              (0.55f + 0.45f * Hash01(seed, 852 + i, boltFlick)) * rr;

                // EL MULTI-BOLTO de 2ª generación (filamentos + ramas + pelos).
                StormLib.MultiBolt(Main.spriteBatch, start, end,
                    seed + 100 + i * 53, boltFlick,
                    Math.Max(2.8f, 0.055f * rr),
                    Tint(AurGold, 0.52f * aMul), Tint(AurPurple, 0.52f * aMul),
                    Tint(WhiteIncan, 0.92f * aMul),
                    0.95f, Math.Max(11f, 0.18f * rr), 8);
            }
        }

        // ------------------------------------------------------------------
        //  9. LOS JETS POLARES DOBLES — ORO arriba, AURORA abajo
        // ------------------------------------------------------------------

        private static void DrawPolarJets(Vector2 center, float rr, float time, int seed,
            int boltFlick, float aMul)
        {
            for (int side = 0; side < 2; side++)
            {
                Vector2 dir = side == 0 ? -Vector2.UnitY : Vector2.UnitY;
                float rot = (float)Math.Atan2(dir.Y, dir.X);
                float pulse = 0.7f + 0.3f * (float)Math.Sin(time * 2.6f + side * 2.7f);
                float jetLen = (1.30f + 0.30f * (float)Math.Sin(time * 2.2f + side * 1.9f)) * rr;
                float baseOff = 1.95f * rr;

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
                        ? Color.Lerp(WhiteIncan, SunGold, midF * 0.7f)
                        : AuroraGrad(0.35f + 0.5f * midF);
                    Capsule(mid, len, w, rot, Tint(c, 0.32f * pulse * (1f - midF * 0.45f) * aMul));
                }

                // ⚡ EL RAYO INTERIOR del jet (la espina de StormLib).
                Vector2 start = center + dir * (baseOff * 0.9f);
                Vector2 end = center + dir * (baseOff + jetLen * 1.05f);
                if (StormLib.IsLit(seed + 90 + side * 13, boltFlick, 0.80f))
                    StormLib.Bolt(Main.spriteBatch, start, end,
                        seed + 960 + side * 29, boltFlick,
                        Math.Max(2.4f, 0.045f * rr),
                        Tint(side == 0 ? SunGold : AurPurple, 0.50f * aMul),
                        Tint(WhiteIncan, 0.90f * aMul),
                        1f, 6, Math.Max(8f, 0.12f * rr));

                Vector2 bpos = center + dir * baseOff;
                Quad(Glow, bpos, new Vector2(0.85f * rr, 0.85f * rr), 0f,
                    Tint(side == 0 ? SunGold : AurPurple, 0.28f * pulse * aMul));
                Vector2 tip = center + dir * (baseOff + jetLen * 1.05f);
                Quad(Glow, tip, new Vector2(0.55f * rr, 0.55f * rr), 0f,
                    Tint(WhiteIncan, 0.40f * pulse * aMul));
            }
        }

        // ------------------------------------------------------------------
        //  10. LAS ONDAS DE DISTORSIÓN ×3
        // ------------------------------------------------------------------

        private static void DrawDistortionWaves(Vector2 center, float R, float time, int seed,
            float aMul)
        {
            for (int w = 0; w < WaveCount; w++)
            {
                float phase = (time / WaveCycle + w / (float)WaveCount) % 1f;
                float radius = (2.02f + phase * 1.85f) * R;
                float fade = (1f - phase) * (1f - phase);
                RingQuad(center, radius, phase * 3.4f + w * 2.1f,
                    Tint(AuroraGrad(phase), 0.18f * fade * aMul));
            }
        }

        // ------------------------------------------------------------------
        //  11. LAS VOLUTAS CAYENDO (BrumaFX.Tendril — la acreción invertida)
        // ------------------------------------------------------------------

        private static void DrawFallingTendrils(Vector2 center, float R, float time, int seed,
            float aMul)
        {
            for (int i = 0; i < TendrilCount; i++)
            {
                float baseAng = time * 0.22f + i * (MathHelper.TwoPi / TendrilCount);
                Vector2[] path = new Vector2[5];
                for (int k = 0; k < 5; k++)
                {
                    float f = k / 4f;
                    float ang = baseAng + f * TendrilSweep;
                    // Nacen en las firmas exteriores (3.45R) y MUEREN en el
                    // cuerpo solar (0.55R) — el sol se come la bruma.
                    float dist = MathHelper.Lerp(TendrilStart * R, TendrilEnd * R, f);
                    path[k] = center + new Vector2(
                        (float)Math.Cos(ang) * dist, (float)Math.Sin(ang) * dist * 0.8f);
                }
                BrumaFX.Tendril(path, Math.Max(12f, 0.34f * R),
                    i % 3 == 0 ? new Color(150, 110, 200)
                    : i % 3 == 1 ? new Color(200, 160, 90)
                    : new Color(120, 150, 255),
                    seed + 517 + i * 37, time, alpha: 0.20f * aMul, fade: 0.8f);
            }
        }

        // ------------------------------------------------------------------
        //  12. ✨ LA LUZ PRISMÁTICA (herencia LUMENLIB)
        // ------------------------------------------------------------------

        private static void DrawPrismaticLight(Vector2 center, float R, float rr,
            float time, int seed, float aMul)
        {
            // LOS RAYOS PRISMÁTICOS radiando del borde del sistema (la firma
            // de LumenLib — cada uno con su drift de hue y su pulso).
            const int Rays = 8;
            for (int i = 0; i < Rays; i++)
            {
                float drift = LumenLib.Drift(time, seed + i * 13, 0.24f);
                Color col = LumenLib.Hue(drift, 0.55f, 1f);
                float ang = i / (float)Rays * MathHelper.TwoPi + time * 0.11f;
                Vector2 dir = new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang));
                Vector2 origin = center + dir * (R * 2.04f);

                float len = (1.2f + 1.0f * Hash01(seed, 1700 + i, 19)) * R;
                float pulse = 0.5f + 0.5f * (float)Math.Sin(time * 2.3f + i * 1.87f);
                LumenLib.Ray(Main.spriteBatch, origin, dir, len,
                    Math.Max(5f, 0.10f * rr), col, (0.34f + 0.24f * pulse) * aMul, pulse);
            }

            // LA AURORA DE BANDAS prismáticas girando lejos (el velo).
            LumenLib.Aurora(Main.spriteBatch, center, 3.15f * rr, time,
                LumenLib.Drift(time, seed, 0.10f), 0.28f * aMul, 12);
        }

        // ==================================================================
        //  HELPERS DE DIBUJO
        // ==================================================================

        private static void BeginAdditive()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
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

        /// <summary>Tinte de INTENSIDAD LINEAR (patrón validado del proyecto).</summary>
        private static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color(c.R, c.G, c.B, (byte)(int)(255f * f));
        }
    }
}
