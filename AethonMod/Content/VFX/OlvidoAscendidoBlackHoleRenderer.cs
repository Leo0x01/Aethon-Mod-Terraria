using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// OlvidoAscendidoBlackHoleRenderer — v6.23 — EL OLVIDO ASCENDIDO.
    ///
    /// v6.23 — EL SEGUNDO CÍRCULO RÚNICO (petición del usuario): 6 runas
    /// VIOLETAS @3.20·R CONTRARROTANDO alrededor del círculo dorado — el
    /// DOBLE anillo rúnico de tipo agujero que le faltaba.
    ///
    /// LA COPIA MEJORADA del Olvido definitivo (OlvidoBlackHoleRenderer
    /// v6.15/v6.18 queda INTACTO — este archivo es un VÓRTICE NUEVO): el
    /// mismo cuerpo (anillo de plasma con hotspot Doppler, brazos
    /// espirales, doble círculo de runas, ondas de distorsión, nebulosas
    /// y aura mística) elevado con LA LIBRERÍA DE RAYOS StormLib:
    ///
    ///   1. ⚡ ARCOS DEL VACÍO — 3 StormLib.Arc morado-azules
    ///      abrazando el horizonte (radios 0.95× / 1.08× / 1.22×),
    ///      re-generándose a ~9 Hz con chispas satélite por Flicker.
    ///   2. ⚡ RAYOS ESPIRALES — 2 StormLib.Bolt AZUL ELÉCTRICO que
    ///      SIGUEN los brazos espirales: anclas SOBRE la espiral (del
    ///      extremo exterior al anillo) + JitterPath + Catmull-Rom —
    ///      rayos que ESPIRALAN HACIA EL NÚCLEO.
    ///   3. BRAZOS ESPIRALES REFORZADOS — 3 brazos (antes 2) × 18 pasos
    ///      (antes 14), fluyendo MÁS RÁPIDO (0.26 rad/s vs 0.18).
    ///   4. NEBULOSA fBm MÁS RICA — 8 velos (antes 5) pintados DOBLE
    ///      (halo grande tenue + núcleo pequeño intenso) y con PARPADEO
    ///      POR HASH (cada velo respira a su propio ritmo).
    ///   5. CORREDORES DE FOTONES COMO RAYOS FINOS — además de los 3
    ///      destellos clásicos, 2 fotones-rayo dibujados como
    ///      MICRO-BOLTS de StormLib recorriendo el anillo.
    ///
    /// Paleta MORADO-AZUL (la del recolor v6.18 del Olvido). TODO se
    /// compone AQUÍ, CADA FRAME, por código con los pinceles
    /// procedurales de la biblioteca VFX. Cero arte, cero estado, cero
    /// red (determinismo por hash puro).
    ///
    /// CONTRATO DE BATCH (idéntico al original v6.10): Draw() exige el
    /// SpriteBatch CERRADO y lo deja CERRADO.
    /// </summary>
    public static class OlvidoAscendidoBlackHoleRenderer
    {
        // ==================================================================
        //  PARÁMETROS (el cuerpo del Olvido + las medidas ascendidas)
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

        // --- brazos espirales REFORZADOS (v6.18 ascendido: 3 × 18, rápido) ---
        private const int ArmCount = 3;         // eran 2
        private const int ArmSteps = 18;        // eran 14
        private const float ArmFlow = 0.26f;    // era 0.18 — flujo más rápido
        private const float ArmSweep = 1.35f;   // barrido angular del brazo
        private const float ArmGrow = 0.95f;    // radio creciente del brazo

        // --- NUEVO: la electricidad del vacío (arcos/rayos/fotones) ---
        private const float VoidHz = 9f;        // ~9 Hz de re-generación

        // --- runas doradas (círculo interior) ---
        private const int RuneCount = 10;
        private const float RuneRadius = 2.62f;  // ×R — el círculo rúnico
        private const float RuneOrbit = 0.10f;   // rad/s de rotación del círculo

        // --- v6.23: EL SEGUNDO CÍRCULO RÚNICO — violeta pálido,
        //     CONTRARROTANDO MÁS AFUERA que el dorado ---
        private const int VioletCount = 6;         // 6 glifos violetas
        private const float VioletRadius = 3.20f;  // ×R — MÁS AFUERA que el dorado
        private const float VioletOrbit = -0.12f;  // rad/s — CONTRARROTANDO

        // --- ondas de distorsión ---
        private const float WaveCycle = 2.6f;    // s
        private const int WaveCount = 2;

        // ==================================================================
        //  PALETA — MORADO-AZUL (v6.18, la del recolor del Olvido)
        // ==================================================================

        private static readonly Color HotCore = new(235, 240, 255);   // blanco-frío incandescente
        private static readonly Color MidPink = new(130, 80, 255);    // violeta vivo
        private static readonly Color LowViolet = new(80, 105, 255);  // azul-violeta
        private static readonly Color BoltViolet = new(120, 130, 255);// rayo interior morado-azul
        private static readonly Color BoltPink = new(90, 190, 255);   // rayo exterior azul eléctrico
        private static readonly Color RuneGold = new(255, 150, 40);   // cuerpo de runa
        private static readonly Color RuneTip = new(255, 225, 150);   // punta de runa
        private static readonly Color RuneViolet = new(200, 140, 255);   // runa violeta (círculo exterior v6.23)
        private static readonly Color RuneVioletTip = new(240, 225, 255); // punta violeta pálida
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

        /// <summary>Tinte de INTENSIDAD LINEAL (el patrón validado).</summary>
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
                // El vacío ABSORBE la luz: un degradé oscuro violáceo
                // abraza el mundo alrededor del agujero (bolsillo de vacío).
                BeginAlpha();
                Quad(Glow, center, new Vector2(7.4f * rr, 7.4f * rr), 0f,
                    new Color(6, 4, 22, 175));
                Main.spriteBatch.End();

                // ============ 1..13: TODO LO BRILLANTE (aditivo) ============
                BeginAdditive();

                // --- 1. NUEVO: NEBULOSA fBm MÁS RICA (8 velos dobles con
                //        parpadeo por hash) + polvo ambiental denso ---
                DrawNebulas(center, rr, time, seed, flick);

                // --- 2. ANILLO DE PLASMA — mitad TRASERA ---
                DrawPlasmaRing(center, rr, time, seed, flick, front: false);

                // --- 3. BRAZOS ESPIRALES REFORZADOS (3 × 18, flujo rápido) ---
                DrawSpiralArms(center, rr, time, seed);

                // --- 3.5 NUEVO: RAYOS ESPIRALES siguiendo los brazos ---
                DrawSpiralBolts(center, rr, time, seed);

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

                // --- 5.5 NUEVO: ARCOS DEL VACÍO (3 Arc a ~9 Hz) ---
                DrawVoidArcs(center, r, time, seed);

                // --- 6. CORREDORES DE FOTONES + NUEVO fotones-rayo ---
                DrawPhotonRunners(center, rr, time, seed);

                // --- 7. RAYOS EMERGIENDO DEL ANILLO (StormLib.Bolt) ---
                DrawElectricBolts(center, r, time, seed);

                // --- 8. DESTELLOS POLARES ---
                DrawPolarFlares(center, rr, time);

                // --- 9. RUNAS DORADAS orbitando + anillo rúnico ---
                DrawGoldenRunes(center, r, time, seed);

                // --- 9b. EL CÍRCULO VIOLETA CONTRARROTANTE (v6.23): el
                //      doble círculo rúnico del Olvido. ---
                DrawVioletRunes(center, r, time, seed);

                // --- 10. ONDAS DE DISTORSIÓN expandiéndose ---
                DrawDistortionWaves(center, r, time, seed);

                // --- 11. PARTÍCULAS LUMINOSAS radiales ---
                DrawLuminousMotes(center, r, time, seed);

                // --- 12. AURA MÍSTICA final pulsante ---
                float aura = 0.85f + 0.15f * (float)Math.Sin(time * 2.0f);
                Quad(Glow, center, new Vector2(6.0f * rr, 6.0f * rr), 0f,
                    Tint(AuraViolet, 0.24f * aura));

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
        //  1. NUEVO — NEBULOSA fBm MÁS RICA + POLVO AMBIENTAL
        // ------------------------------------------------------------------

        private static void DrawNebulas(Vector2 center, float rr, float time, int seed, int flick)
        {
            // ============================================================
            //  LA NEBULOSA fBm DEL ASCENDIDO: OCHO VELOS púrpura/azul
            //  (antes 5), cada uno pintado DOBLE — halo grande tenue +
            //  núcleo pequeño intenso, como dos octavas de ruido apiladas
            //  — y con PARPADEO POR HASH: cada velo respira a su propio
            //  ritmo (regenerado a 6 Hz), la lejanía VIVA.
            // ============================================================
            for (int i = 0; i < 8; i++)
            {
                float h = Hash01(seed, 501 + i, 17);
                float dir = i % 2 == 0 ? 1f : -1f;
                float ang = h * MathHelper.TwoPi + time * 0.05f * dir;
                float dist = (2.5f + 1.0f * Hash01(seed, 502 + i, 29)) * rr;
                Vector2 pos = center + new Vector2(
                    (float)Math.Cos(ang) * dist, (float)Math.Sin(ang) * dist * 0.8f);
                float size = (1.8f + 1.2f * Hash01(seed, 503 + i, 41)) * rr;
                Color c = i % 2 == 0 ? NebPurple : NebBlue;

                // EL PARPADEO POR HASH (cada velo tiene SU ritmo).
                float fl = 0.70f + 0.30f * Hash01(seed, 505 + i, flick / 2);
                float pulse = (0.75f + 0.25f * (float)Math.Sin(time * 0.7f + i * 1.9f)) * fl;

                // DOBLE CAPA fBm: halo grande tenue + núcleo pequeño intenso.
                Quad(Glow, pos, new Vector2(size, size), ang,
                    Tint(c, (i % 2 == 0 ? 0.20f : 0.14f) * pulse));
                Quad(Glow, pos, new Vector2(size * 0.45f, size * 0.45f), ang,
                    Tint(c, (i % 2 == 0 ? 0.16f : 0.11f) * pulse));
            }

            // Polvo ambiental: 18 motas morado-azul pulsando (antes 14 —
            // la densidad del vacío ascendido).
            for (int i = 0; i < 18; i++)
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
        //  2/5. EL ANILLO DE PLASMA — 44 cápsulas por mitad (intacto)
        // ------------------------------------------------------------------

        private static void DrawPlasmaRing(Vector2 center, float rr, float time, int seed,
            int flick, bool front)
        {
            // La mitad delantera (t ∈ 0..π) cruza POR DELANTE de la
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

                // INTENSIDAD: hotspot Doppler × turbulencia por hash (12 Hz).
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

                // En la zona MÁS caliente, un punto blanco extra.
                if (inten > 0.88f)
                {
                    Quad(Glow, mid, new Vector2(0.30f * rr, 0.30f * rr), rot,
                        Tint(HotCore, 0.65f * (inten - 0.88f) / 0.12f));
                }
            }
        }

        // ------------------------------------------------------------------
        //  3. BRAZOS ESPIRALES REFORZADOS — el vórtice se desenrosca
        // ------------------------------------------------------------------

        private static void DrawSpiralArms(Vector2 center, float rr, float time, int seed)
        {
            // ============================================================
            //  REFORZADOS: 3 brazos (antes 2) × 18 pasos (antes 14) y
            //  fluyendo a 0.26 rad/s (antes 0.18) — el vórtice del
            //  Ascendido desenrosca MÁS MATERIA, MÁS RÁPIDO.
            // ============================================================
            float armPhase = time * ArmFlow;

            for (int arm = 0; arm < ArmCount; arm++)
            {
                float baseT = armPhase + arm * (MathHelper.TwoPi / ArmCount);
                for (int k = 0; k < ArmSteps; k++)
                {
                    float f = k / (float)(ArmSteps - 1);           // 0 en el anillo → 1 fuera
                    float t = baseT + f * ArmSweep;                // barrido angular
                    float grow = 1f + f * ArmGrow;                 // radio creciente
                    Vector2 pos = VFXCore.Ellipse(center,
                        RingA * rr * grow, RingB * rr * grow, RingTilt, t);
                    Vector2 next = VFXCore.Ellipse(center,
                        RingA * rr * grow, RingB * rr * grow, RingTilt,
                        t + ArmSweep / ArmSteps);
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
        //  3.5 NUEVO — LOS RAYOS ESPIRALES (StormLib sobre el brazo)
        // ------------------------------------------------------------------

        private static void DrawSpiralBolts(Vector2 center, float rr, float time, int seed)
        {
            // ============================================================
            //  LOS RAYOS ESPIRALES — LA FIRMA DEL OLVIDO ASCENDIDO: dos
            //  StormLib.Bolt AZUL ELÉCTRICO que SIGUEN los brazos
            //  espirales. Las ANCLAS se construyen con puntos SOBRE la
            //  espiral (del extremo EXTERIOR al anillo — el rayo NACE
            //  lejos y ESPIRALA HACIA EL NÚCLEO), el camino tiembla con
            //  JitterPath (perpendicular de cuerda) y se suaviza con
            //  Catmull-Rom para serpentear fluido sobre el brazo.
            // ============================================================
            int boltFlick = StormLib.FlickTick(time, VoidHz);
            float armPhase = time * ArmFlow;

            for (int arm = 0; arm < 2; arm++)
            {
                if (!StormLib.IsLit(seed + 431 + arm * 61, boltFlick, 0.80f))
                    continue;

                float baseT = armPhase + arm * (MathHelper.TwoPi / ArmCount);

                // Anclas SOBRE la espiral: f=1 (exterior) → f=0 (anillo).
                const int Anchors = 7;
                Vector2[] anchors = new Vector2[Anchors + 1];
                for (int k = 0; k <= Anchors; k++)
                {
                    float f = 1f - k / (float)Anchors;
                    float t = baseT + f * ArmSweep;
                    float grow = 1f + f * ArmGrow;
                    anchors[k] = VFXCore.Ellipse(center,
                        RingA * rr * grow, RingB * rr * grow, RingTilt, t);
                }

                // El rayo GANA la rugosidad MULTI-ESCALA del refino fractal
                // de StormLib (los extremos quedan ANCLADOS: el jitter vive
                // solo en los midpoints interiores).
                Vector2[] pts = StormLib.Refine(anchors,
                    seed + 500 + arm * 37, boltFlick, 0.9f);

                // EL FILAMENTO azul eléctrico con ramas hacia fuera.
                StormLib.Strand(Main.spriteBatch, pts,
                    seed + 600 + arm * 43, boltFlick, rr * 0.085f,
                    Tint(BoltViolet, 0.46f), Tint(BoltPink, 0.88f));
            }
        }

        // ------------------------------------------------------------------
        //  5.5 NUEVO — LOS ARCOS DEL VACÍO (StormLib.Arc)
        // ------------------------------------------------------------------

        private static void DrawVoidArcs(Vector2 center, float r, float time, int seed)
        {
            // ============================================================
            //  LOS ARCOS DEL VACÍO: tres StormLib.Arc morado-azules
            //  abrazando el horizonte — 0.95× (JUSTO dentro del filo),
            //  1.08× y 1.22× del radio, cada uno derivando a su propia
            //  velocidad. Se re-generan a ~9 Hz con parpadeo Flicker y
            //  sueltan CHISPAS SATÉLITE — el vacío CHISPEA en círculos
            //  alrededor de la esfera.
            // ============================================================
            int arcFlick = StormLib.FlickTick(time, VoidHz);

            for (int c = 0; c < 3; c++)
            {
                float radius = (0.95f + 0.13f * c) * r;
                float drift = time * (0.90f - 0.55f * c) + c * 2.1f;

                if (!StormLib.IsLit(seed + 621 + c * 9, arcFlick, 0.85f))
                    continue;

                float span = 1.05f + 0.55f * Hash01(seed, 631 + c, arcFlick);
                StormLib.ArcRing(Main.spriteBatch, center, radius,
                    drift, drift + span, seed + 210 + c * 29, arcFlick,
                    r * 0.070f, Tint(BoltViolet, 0.40f), Tint(BoltPink, 0.82f), 1f, 11);

                // CHISPA SATÉLITE del arco (el 35% de las regeneraciones).
                if (StormLib.IsLit(seed + 643 + c * 5, arcFlick, 0.35f))
                {
                    float satA = drift - span * 0.6f;
                    StormLib.ArcRing(Main.spriteBatch, center, radius * 1.04f,
                        satA, satA + span * 0.30f, seed + 250 + c * 31, arcFlick,
                        r * 0.040f, Tint(BoltViolet, 0.28f), Tint(HotCore, 0.65f), 1f, 6);
                }
            }
        }

        // ------------------------------------------------------------------
        //  6. CORREDORES DE FOTONES + FOTONES-RAYO (micro-bolts)
        // ------------------------------------------------------------------

        private static void DrawPhotonRunners(Vector2 center, float rr, float time, int seed)
        {
            // (los 3 corredores clásicos: halo violeta + núcleo blanco-frío)
            for (int i = 0; i < 3; i++)
            {
                float t = time * (1.25f + 0.22f * i) + i * 2.1f;
                Vector2 pos = Ellipse(center, rr, t);
                float twinkle = 0.65f + 0.35f * (float)Math.Sin(time * 8f + i * 2.3f);
                Quad(Glow, pos, new Vector2(1.05f * rr, 1.05f * rr), 0f,
                    Tint(MidPink, 0.35f * twinkle));
                Quad(Glow, pos, new Vector2(0.42f * rr, 0.42f * rr), 0f,
                    Tint(HotCore, 0.85f * twinkle));
            }

            // ============================================================
            //  NUEVO — FOTONES-RAYO: dos corredores extra dibujados como
            //  MICRO-BOLTS de StormLib (grosor mínimo) recorriendo
            //  un arco corto del anillo — fotones "electrificados" del
            //  vacío ascendido, con parpadeo vivo por Flicker.
            // ============================================================
            int boltFlick = StormLib.FlickTick(time, VoidHz);
            for (int i = 0; i < 2; i++)
            {
                if (!StormLib.IsLit(seed + 521 + i * 11, boltFlick, 0.72f))
                    continue;

                float t = time * (1.55f + 0.30f * i) + i * 3.3f;
                Vector2 p0 = Ellipse(center, rr, t - 0.32f);
                Vector2 p1 = Ellipse(center, rr, t);
                StormLib.Bolt(Main.spriteBatch, p0, p1,
                    seed + 530 + i * 7, boltFlick, rr * 0.045f,
                    Tint(MidPink, 0.55f), Tint(HotCore, 0.92f), 1f, 5, rr * 0.05f);
            }
        }

        // ------------------------------------------------------------------
        //  7. RAYOS EMERGIENDO DEL ANILLO (StormLib.Bolt)
        // ------------------------------------------------------------------

        private static void DrawElectricBolts(Vector2 center, float r, float time, int seed)
        {
            // ============================================================
            //  DOS RAYOS EMERGIENDO DEL ANILLO hacia afuera — ahora con
            //  StormLib.Bolt: doble tira cuerpo + núcleo, RAMAS
            //  HEREDADAS y gorros de descarga (más ricos que el zigzag
            //  simple del original), re-generados a ~9 Hz.
            // ============================================================
            int boltFlick = StormLib.FlickTick(time, VoidHz);

            for (int i = 0; i < 2; i++)
            {
                if (!StormLib.IsLit(seed + 858 + i * 13, boltFlick, 0.70f))
                    continue;

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

                StormLib.Bolt(Main.spriteBatch, start, end,
                    seed + 100 + i * 53, boltFlick, r * 0.095f,
                    Tint(BoltViolet, 0.52f), Tint(BoltPink, 0.92f), 1f, 7, r * 0.15f);
            }
        }

        // ------------------------------------------------------------------
        //  8. DESTELLOS POLARES — agujas ahusadas en el eje menor (intacto)
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
        //  9. RUNAS DORADAS — 10 glifos originales orbitando (intacto)
        //     + 9b: EL CÍRCULO VIOLETA CONTRARROTANTE (v6.23 — doble
        //      círculo rúnico de tipo agujero)
        //     v6.37 — ambos delegan en OrbitaLib (la librería de los
        //     anillos rúnicos): ni un número cambiado. La tabla R (10
        //     runas) es PROPIA → se pasa como `alphabet`.
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
        //  9b. NUEVO (v6.23) — EL CÍRCULO VIOLETA CONTRARROTANTE: el doble
        //      círculo rúnico de tipo agujero (espejo del círculo ámbar
        //      del Cósmico Ascendido)
        // ------------------------------------------------------------------

        /// <summary>
        /// EL SEGUNDO CÍRCULO RÚNICO (v6.23): 6 glifos VIOLETAS a radio
        /// MAYOR (3.20·R) CONTRARROTANDO respecto a las runas doradas —
        /// dos coronas de conjuro girando en sentidos opuestos, la firma
        /// rúnica completa del Olvido Ascendido.
        /// v6.37 — delega en OrbitaLib (la librería de los anillos
        /// rúnicos): ni un número cambiado. El humor NERVIOSO de la
        /// librería ES el de aquí (respiración 2.2·sin(1.1t+1.4g), vaivén
        /// 1.8·sin(0.95t+2.1g), latido 0.70+0.30·sin(2.8t+1.7g), glow 30
        /// @0.18, trazos 3.2 @0.80, perlas 6.2/2.9 @0.55/0.90 a 10.5·gs
        /// fundidas al latido del glifo); los glifos desfasados de la
        /// tabla (`_runes[(g*3+2) % len]`) como `stride: 3, offset: 2`;
        /// la perla usa la punta violeta → `pearlTip: null` (default).
        /// </summary>
        private static void DrawVioletRunes(Vector2 center, float r, float time, int seed)
        {
            // Glifos al 85% de las doradas (0.85 × 1.35 de la casa).
            OrbitaLib.CirculoRunico(center, r, time, VioletRadius, VioletCount, VioletOrbit,
                RuneViolet, RuneVioletTip, Math.Max(r / 52f, 0.25f) * 1.35f * 0.85f,
                alphabet: _runes, stride: 3, offset: 2,
                ringAlpha: 0.18f, nervioso: true);
        }

        // ------------------------------------------------------------------
        //  10. ONDAS DE DISTORSIÓN — el espacio-tiempo late (intacto)
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

                // Violeta / azul / dorada (rara) / blanca.
                Color c = h < 0.40f ? MidPink
                        : h < 0.72f ? BoltViolet
                        : h < 0.90f ? RuneGold
                        : new Color(240, 245, 255);

                Quad(Glow, pos, new Vector2(size, size), 0f, Tint(c, alpha));
            }
        }
    }
}
