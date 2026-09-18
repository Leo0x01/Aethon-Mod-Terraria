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
    /// BrumaAscendidoBlackHoleRenderer — v6.23 — LA COPIA MEJORADA DE LA
    /// BRUMA: "LA ASCENDIDA".
    ///
    /// v6.23 — EL DOBLE CÍRCULO DE RUNAS (petición del usuario): 8 runas
    /// TEAL CW @2.55·R + 6 runas BLANCAS DE HIELO CCW @3.15·R, MÁS
    /// AFUERA — los DOS anillos rúnicos de tipo agujero que faltaban,
    /// dibujados con los glifos de la casa en tema ESCARCHA.
    ///
    /// La Bruma original (BrumaBlackHoleRenderer) queda INTACTA; este es
    /// un archivo NUEVO nacido de ella — misma identidad GELIDA (teal /
    /// cian / violeta), misma librería de bruma (BrumaFX), mismo contrato
    /// de batch — con SEIS MEJORAS SUSTANCIALES (cinco de ellas sobre la
    /// LIBRERÍA DE RAYOS StormLib, más el doble círculo rúnico v6.23):
    ///
    ///   1. ⚡ CORONAS DE ESCARCHA ELÉCTRICA — 3 StormLib.Arc cian
    ///      alrededor del horizonte a radios ligeramente distintos,
    ///      parpadeando a ~10 Hz — LA FIRMA VISUAL de la Ascendida.
    ///
    ///   2. ⚡ RAYOS GELIDOS — 2 StormLib.Bolt cian-blancos ESCAPANDO
    ///      del anillo de humo (doble tira + ramas heredadas).
    ///
    ///   3. VOLUTAS REFORZADAS — las Tendril pasan de 3 a 5, con MÁS
    ///      fuerza de curl (BrumaNoise.Curl): la materia devorada
    ///      serpentea remolinando hacia el núcleo.
    ///
    ///   4. CRISTALES DE HIELO — 6 destellos angulares (agujas cruzadas
    ///      blanco-cian con parpadeo lento) flotando alrededor.
    ///
    ///   5. CHIMENEAS POLARES REFORZADAS — Column más ALTAS (2.2·R, era
    ///      1.6·R, 7 pasos, era 5) con motas de escarcha subiendo por el
    ///      eje del vórtice.
    ///
    ///   6. DOBLE CÍRCULO DE RUNAS (v6.23) — 8 runas TEAL CW @2.55·R +
    ///      6 BLANCAS DE HIELO CCW @3.15·R, MÁS AFUERA: la firma rúnica
    ///      de los agujeros negros, en versión hielo-estelar.
    ///
    /// CONTRATO DE BATCH (v6.10, IDÉNTICO al original): Draw() exige el
    /// SpriteBatch CERRADO y lo deja CERRADO. El humo se dibuja en el lote
    /// ADITIVO (bruma LUMINOSA).
    /// </summary>
    public static class BrumaAscendidoBlackHoleRenderer
    {
        // ==================================================================
        //  PARÁMETROS
        // ==================================================================

        /// <summary>Radio de la esfera negra en px a escala 1 — IGUAL al original.</summary>
        public const float SpherePx = 48f;

        /// <summary>El anillo de humo (más redondo que un disco: es bruma).</summary>
        private const float RingA = 1.75f;      // semieje mayor (×R)
        private const float RingB = 1.25f;      // semieje menor (×R)
        private const float RingTilt = -0.33f;  // rad — inclinación

        /// <summary>Flujo de los fumarelitos por el anillo (rad/s).</summary>
        private const float SmokeFlow = 0.38f;

        /// <summary>Fumarelitos por mitad del anillo.</summary>
        private const int SmokeletsPerHalf = 16;

        // --- ASCENDIDO: las volutas REFORZADAS (3 → 5, con curl) ---
        private const int TendrilCount = 5;
        private const float TendrilStart = 2.85f;   // ×R — donde nace la voluta
        private const float TendrilSweep = 2.1f;    // rad de barrido espiral
        private const float TendrilCurl = 0.30f;    // ×R — fuerza del curl noise

        // --- ASCENDIDO: LAS CORONAS DE ESCARCHA ELÉCTRICA ---
        private const int FrostCrownCount = 3;      // coronas concéntricas
        private const float FrostCrownHz = 10f;     // parpadeo nervioso (~10 Hz)
        private const float FrostCrownSpan = 1.35f; // rad que cubre cada corona

        // --- ASCENDIDO: LOS RAYOS GELIDOS ---
        private const int GelidBoltCount = 2;       // rayos escapando del anillo
        private const float GelidBoltHz = 9f;       // regeneración nerviosa
        private const float GelidBoltLen = 1.75f;   // ×R — largo del rayo

        // --- ASCENDIDO: los cristales de hielo flotando ---
        private const int IceCrystalCount = 6;

        // --- v6.23: EL DOBLE CÍRCULO DE RUNAS (hielo-estelar) ---
        private const int RuneCount = 8;         // teal, CW
        private const float RuneRadius = 2.55f;  // ×R — círculo rúnico
        private const float RuneOrbit = 0.10f;   // rad/s
        private const int RuneCount2 = 6;        // blancas, CCW
        private const float RuneRadius2 = 3.15f; // ×R — MÁS AFUERA
        private const float RuneOrbit2 = -0.14f; // rad/s — CONTRARROTO

        // --- ASCENDIDO: chimeneas polares reforzadas ---
        private const int ChimneySteps = 7;         // (el original usaba 5)
        private const float ChimneyHeight = 2.2f;   // ×R (el original: 1.6)
        private const int ChimneyMotes = 4;         // motas de escarcha subiendo

        // --- ondas de distorsión ---
        private const float WaveCycle = 2.7f;
        private const int WaveCount = 2;

        // ==================================================================
        //  PALETA — la estirpe GELIDA (copia): teal / cian / violeta
        // ==================================================================

        private static readonly Color SmokeTeal = new(30, 140, 160);    // masa principal
        private static readonly Color SmokeCyan = new(90, 210, 235);    // bordes iluminados
        private static readonly Color SmokeViolet = new(70, 60, 190);   // bruma arcana
        private static readonly Color DeepBlue = new(25, 45, 140);      // profundidad
        private static readonly Color PhotonWhite = new(225, 250, 255); // cian-blanco caliente
        private static readonly Color RimTeal = new(60, 200, 220);      // horizonte congelado
        private static readonly Color FrostMote = new(185, 235, 250);   // escarcha
        private static readonly Color RuneTeal = new(60, 200, 220);     // cuerpo de runa teal
        private static readonly Color RuneTealTip = new(225, 250, 255); // punta hielo-blanca

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
                BeginAdditive();

                // --- 1. EL HALO: dos Cloud de la librería (teal + violeta) ---
                BrumaFX.Cloud(center, 2.6f * rr, SmokeTeal, seed + 11, time,
                    puffs: 5, alpha: 0.30f);
                BrumaFX.Cloud(center, 3.3f * rr, SmokeViolet, seed + 47, time * 0.8f,
                    puffs: 4, alpha: 0.20f);

                // --- 2. ANILLO DE HUMO — mitad TRASERA ---
                DrawSmokeRing(center, rr, time, seed, front: false);

                // --- 3. VOLUTAS espiralando hacia el núcleo (×5, con CURL) ---
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

                // --- 6b. ⚡ LAS CORONAS DE ESCARCHA ELÉCTRICA — LA FIRMA
                //      de la Ascendida: 3 arcos StormLib cian alrededor
                //      del horizonte, a radios ligeramente distintos. ---
                DrawFrostCrowns(center, r, time, seed);

                // --- 6c. ⚡ LOS RAYOS GELIDOS — 2 StormLib.Bolt
                //      cian-blancos ESCAPANDO del anillo de humo. ---
                DrawGelidBolts(center, rr, time, seed);

                // --- 7. CHIMENEAS POLARES REFORZADAS (más altas + motas de
                //      escarcha subiendo) + agujas frías ---
                DrawPolarChimneys(center, rr, time, seed);
                DrawPolarFlares(center, rr, time);

                // --- 8. ESCARCHA — motas blanco-cian radiales ---
                DrawFrostMotes(center, r, time, seed);

                // --- 8b. LOS CRISTALES DE HIELO — destellos angulares
                //      flotando alrededor (nuevos del Ascendido). ---
                DrawIceCrystals(center, r, time, seed);

                // --- 8c. EL DOBLE CÍRCULO DE RUNAS — 8 teal CW + 6 blancas
                //      de hielo CCW MÁS AFUERA (v6.23: la firma rúnica,
                //      versión hielo-estelar de la Ascendida). ---
                DrawRuneCircle(center, r, time, seed);

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
                try { Main.spriteBatch.End(); } catch { }
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

                // La densidad VIVE: fBm por posición y tiempo.
                float n = BrumaNoise.Fbm(pos.X * 0.012f, pos.Y * 0.012f + time * 0.10f,
                    seed + 5, 3);
                float a = (0.28f + 0.42f * n) * bright;

                // Color: la masa teal; los del filo interno se iluminan cian.
                Color c = n > 0.55f ? SmokeCyan : SmokeTeal;

                // EL FUMARELITO: un Puff de la librería.
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
        //     (ASCENDIDO: 5 volutas con MÁS FUERZA DE CURL)
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
                    Vector2 basePt = center + new Vector2(
                        (float)Math.Cos(ang) * rad * RingA / 1.75f,
                        (float)Math.Sin(ang) * rad * RingB / 1.25f);

                    // ASCENDIDO: MÁS FUERZA DE CURL — el campo de remolinos
                    // (BrumaNoise.Curl) TORSIONA la ruta: la voluta no cae
                    // por una espiral geométrica, SERPENTea remolinando.
                    Vector2 curl = BrumaNoise.Curl(
                        basePt.X * 0.006f, basePt.Y * 0.006f + time * 0.08f,
                        seed + k * 7);
                    path[p] = basePt + curl * (TendrilCurl * r);
                }

                // La voluta se DISUELVE al acercarse al núcleo (fade=1: la
                // masa muere en el final de la ruta — devorada).
                Color c = k % 2 == 0 ? SmokeTeal : SmokeViolet;
                float n = BrumaNoise.Fbm(k * 3.7f, time * 0.15f, seed + k, 3);
                BrumaFX.Tendril(path, 0.55f * r, c, seed + k * 97, time,
                    alpha: 0.48f + 0.22f * n, fade: 1f);
            }
        }

        // ------------------------------------------------------------------
        //  6b. ⚡ LAS CORONAS DE ESCARCHA ELÉCTRICA — StormLib.Arc ×3
        //      alrededor del horizonte, a radios ligeramente distintos,
        //      parpadeando a ~10 Hz — LA FIRMA VISUAL del Ascendido.
        // ------------------------------------------------------------------

        private static void DrawFrostCrowns(Vector2 center, float r, float time, int seed)
        {
            int cflick = StormLib.FlickTick(time, FrostCrownHz);
            float spin = time * 0.55f;

            for (int k = 0; k < FrostCrownCount; k++)
            {
                int cseed = seed + 8100 + k * 131;
                // El parpadeo nervioso: cada corona se APAGA a veces.
                if (!StormLib.IsLit(cseed, cflick, 0.88f)) continue;

                // Radios ligeramente distintos (coronas concéntricas) y
                // contrarrotación alternada: el horizonte CHISPEA.
                float radius = (1.18f + 0.13f * k) * r;
                float a0 = spin * (k % 2 == 0 ? 1f : -0.7f) + k * 2.2f;
                float span = FrostCrownSpan - 0.18f * k;
                // (v2 del calibrado: 0.080·R — calibrado con el mock VLM.)
                float w = Math.Max(r * (0.080f - 0.010f * k), 2.2f);

                StormLib.ArcRing(Main.spriteBatch, center, radius, a0, a0 + span,
                    cseed, cflick, w,
                    Tint(SmokeCyan, 0.50f), Tint(PhotonWhite, 0.95f),
                    alpha: 1f, count: 9);
            }
        }

        // ------------------------------------------------------------------
        //  6c. ⚡ LOS RAYOS GELIDOS — StormLib.Bolt ×2 cian-blancos
        //      ESCAPANDO del anillo de humo (doble tira + ramas + gorros).
        // ------------------------------------------------------------------

        private static void DrawGelidBolts(Vector2 center, float rr, float time, int seed)
        {
            int bflick = StormLib.FlickTick(time, GelidBoltHz);

            for (int i = 0; i < GelidBoltCount; i++)
            {
                int bseed = seed + 9200 + i * 173;
                // El parpadeo nervioso: el rayo se APAGA a veces.
                if (!StormLib.IsLit(bseed, bflick, 0.78f)) continue;

                // El ancla VIVE sobre el anillo de humo (deriva lenta) y el
                // rayo ESCAPA hacia afuera, ligeramente hacia arriba.
                float t = time * (0.16f * (i % 2 == 0 ? 1f : -1f)) +
                          i * MathHelper.Pi + 0.6f;
                Vector2 start = Ellipse(center, rr, t);
                Vector2 outward = start - center;
                if (outward.LengthSquared() < 0.01f) continue;
                outward.Normalize();
                Vector2 bias = new Vector2(0.22f * (i == 0 ? 1f : -1f), -0.30f);
                Vector2 dir = (outward + bias).SafeNormalize(Vector2.UnitY);

                float len = GelidBoltLen * rr * (0.85f + 0.40f * Hash01(seed, 9210 + i, 3));
                Vector2 end = start + dir * len;

                // LA DOBLE TIRA DE StormLib (funda + núcleo + ramas).
                float w = Math.Max(rr * 0.075f, 2.2f);
                StormLib.Bolt(Main.spriteBatch, start, end, bseed, bflick,
                    w, Tint(SmokeCyan, 0.55f), Tint(PhotonWhite, 0.95f),
                    alpha: 1f, segments: 7, amp: rr * 0.15f);

                // Núcleo CASI BLANCO en la base (la escarcha fresca).
                Quad(Glow, start, new Vector2(0.55f * rr, 0.55f * rr), 0f,
                    Tint(FrostMote, 0.55f));
            }
        }

        // ------------------------------------------------------------------
        //  6d. CORREDORES DE FOTONES — chispas cian-blanco orbitando
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
        //  7a. CHIMENEAS POLARES REFORZADAS — más puffs, más ALTOS y con
        //      MOTAS DE ESCARCHA subiendo por el eje del vórtice
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
                Vector2 perp = new Vector2(-dir.Y, dir.X);

                // La chimenea ASCENDIDO: 7 pasos (eran 5) y hasta 2.2·R de
                // altura (eran 1.6·R) — la bruma del elevado ERUPCIONA.
                for (int i = 0; i < ChimneySteps; i++)
                {
                    float phase = (time * 0.45f + i / (float)ChimneySteps) % 1f;
                    float riseR = (0.10f + 0.30f * phase) * rr;
                    float along = baseOff + phase * ChimneyHeight * rr;
                    Vector2 pos = center + dir * along;
                    float n = BrumaNoise.Fbm(i * 2.3f, time * 0.2f + side, seed + side * 31, 3);
                    float a = 0.32f * MathF.Sin(phase * MathHelper.Pi) * (0.5f + 0.5f * n);
                    BrumaFX.Puff(pos, riseR, side == 0 ? SmokeCyan : SmokeTeal,
                        seed + side * 53 + i * 17, time,
                        MathHelper.Clamp(a, 0.04f, 0.5f), quality: 0.4f,
                        velocity: dir * phase * rr * 0.6f);
                }

                // LAS MOTAS DE ESCARCHA: chispas frías SUBIENDO por la
                // chimenea (el viento polar del Ascendido las arrastra).
                for (int m = 0; m < ChimneyMotes; m++)
                {
                    float mphase = (time * 0.55f + m / (float)ChimneyMotes +
                                    side * 0.5f) % 1f;
                    float along = baseOff + mphase * (ChimneyHeight + 0.15f) * rr;
                    float sway = 2.6f * (float)Math.Sin(time * 1.3f + m * 2.1f + side);
                    Vector2 mpos = center + dir * along + perp * sway;
                    float mg = 0.30f + 0.70f * (float)Math.Sin(mphase * Math.PI);
                    Quad(Glow, mpos, new Vector2(0.14f * rr, 0.14f * rr), 0f,
                        Tint(FrostMote, 0.55f * mg));
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
        //  8b. LOS CRISTALES DE HIELO — destellos angulares flotando
        //      alrededor (NUEVOS del Ascendido): agujas cruzadas con
        //      parpadeo LENTO (el hielo respira, no chispea).
        // ------------------------------------------------------------------

        private static void DrawIceCrystals(Vector2 center, float r, float time, int seed)
        {
            for (int i = 0; i < IceCrystalCount; i++)
            {
                float h = Hash01(seed, 9500 + i, 29);
                float ang = h * MathHelper.TwoPi + time * 0.05f * (i % 2 == 0 ? 1f : -1f);
                float dist = (1.9f + 1.3f * Hash01(seed, 9501 + i, 31)) * r;
                Vector2 pos = center + new Vector2(
                    (float)Math.Cos(ang) * dist,
                    (float)Math.Sin(ang) * dist * 0.85f);

                // Flotación lenta (el cristal deriva) + rotación propia.
                pos += new Vector2(
                    3f * (float)Math.Sin(time * 0.7f + i * 1.9f),
                    4f * (float)Math.Sin(time * 0.5f + i * 2.7f));
                float rot = i * 0.8f + time * 0.12f;

                // EL PARPADEO LENTO: brillo ^2 — destello amplio y pausado.
                float glint = 0.35f + 0.65f * (float)Math.Pow(
                    0.5f + 0.5f * (float)Math.Sin(time * (0.5f + 0.3f * h) + i * 2.3f), 2.0f);

                // EL DESTELLO ANGULAR: dos agujas cruzadas + una diagonal
                // + núcleo (glow pequeño blanco-cian).
                float size = (0.16f + 0.14f * h) * r;
                Capsule(pos, size * 2.6f, size * 0.22f, rot,
                    Tint(FrostMote, 0.55f * glint));
                Capsule(pos, size * 2.6f, size * 0.22f, rot + MathHelper.PiOver2,
                    Tint(FrostMote, 0.55f * glint));
                Capsule(pos, size * 1.5f, size * 0.16f, rot + MathHelper.PiOver4,
                    Tint(PhotonWhite, 0.40f * glint));
                Quad(Glow, pos, new Vector2(size * 0.9f, size * 0.9f), 0f,
                    Tint(PhotonWhite, 0.75f * glint));
            }
        }

        // ------------------------------------------------------------------
        //  8c. EL DOBLE CÍRCULO DE RUNAS — 8 TEAL CW + 6 BLANCAS CCW
        //      (v6.23 — NUEVO: la firma rúnica de los agujeros negros,
        //       versión HIELO-ESTELAR de la Ascendida)
        //      v6.37 — delega en OrbitaLib (la librería de los anillos
        //      rúnicos): ni un número cambiado. El alfabeto H (glifos
        //      idénticos byte a byte al S) vive AHORA en
        //      OrbitaLib.RunasVacio (el default de la librería).
        // ------------------------------------------------------------------

        /// <summary>
        /// EL DOBLE CÍRCULO DE RUNAS (v6.23): aro + 8 glifos TEAL CW
        /// adentro + 6 glifos BLANCOS DE HIELO CCW MÁS AFUERA — dos
        /// coronas de conjuro girando en sentidos opuestos (la firma
        /// rúnica de los agujeros negros, versión de la Ascendida).
        /// v6.37 — delega en OrbitaLib (la librería de los anillos
        /// rúnicos): ni un número cambiado. El aro de pauta, las runas
        /// DE PIE y las perlas viven AHORA en la primitiva; el alfabeto
        /// H (glifos idénticos byte a byte al S de la casa) es el
        /// default de la librería: OrbitaLib.RunasVacio.
        /// </summary>
        private static void DrawRuneCircle(Vector2 center, float r, float time, int seed)
        {
            float glyphScale = Math.Max(r / 52f, 0.25f) * 1.35f;

            // ============================================================
            //  EL CÍRCULO TEAL — 8 runas girando CW (el conjuro interior)
            // ============================================================
            OrbitaLib.CirculoRunico(center, r, time, RuneRadius, RuneCount,
                RuneOrbit, RuneTeal, RuneTealTip, glyphScale,
                offset: 0, ringAlpha: 0.22f);

            // ============================================================
            //  EL CÍRCULO BLANCO DE HIELO — 6 runas MÁS AFUERA girando
            //  CCW (el contrarroto de escarcha: la nieve gira al revés;
            //  cuerpo FrostMote → punta PhotonWhite, ambas de la paleta)
            // ============================================================
            OrbitaLib.CirculoRunico(center, r, time, RuneRadius2, RuneCount2,
                RuneOrbit2, FrostMote, PhotonWhite, glyphScale * 0.85f,
                offset: 3, ringAlpha: 0.18f);
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
