using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using ReLogic.Content;

namespace AethonMod.Content.VFX
{
    /// <summary>Curva de anchura a lo largo del rayo (el CONTRATO del taper).</summary>
    public enum StormTaper
    {
        /// <summary>Grueso al centro, fino en los anclajes (senoidal).</summary>
        Center,
        /// <summary>Decrece lineal hacia el destino.</summary>
        Linear,
        /// <summary>Fino al nacer, GRUESO en el impacto (la caída del cielo).</summary>
        Impact,
    }

    /// <summary>Un filamento de la tormenta: puntos + anchura relativa + alpha.</summary>
    public struct StormStrand
    {
        public Vector2[] Points;
        public float WidthScale;
        public float Alpha;

        public StormStrand(Vector2[] pts, float widthScale, float alpha)
        {
            Points = pts;
            WidthScale = widthScale;
            Alpha = alpha;
        }
    }

    /// <summary>
    /// StormLib — v6.21 — LA SEGUNDA GENERACIÓN DE RAYOS.
    ///
    /// Nace de la investigación profunda y metódica del ecosistema (los
    /// sistemas de rayos de los grandes mods de VFX, estudiados a fondo:
    /// generación, flicker, multi-filamento, tapers, perfiles de textura,
    /// impactos) y se re-implementa 100% con código PROPIO sobre la pila de
    /// la casa: SpriteBatch + texturas procedurales + hash determinista.
    ///
    /// LA DIFERENCIA con LightningCore (v6.18, que queda intacta para los
    /// agujeros negros): LightningCore dibuja cápsulas de GLOW suave — es
    /// una tira de energía difusa. StormLib dibuja FILAMENTOS de verdad:
    ///
    ///   1. TEXTURAS DE FILAMENTO — BoltHalo (banda suave) y BoltCore (el
    ///      núcleo blanco que serpentea DENTRO de la textura, con grietas
    ///      de alta frecuencia y nodos brillantes). La nitidez de un rayo
    ///      vive en la TEXTURA, no en la geometría.
    ///
    ///   2. TRIPLE CAPA por sub-segmento — halo ancho de color + cuerpo
    ///      del color + NÚCLEO BLANCO a ~⅓ del ancho (la vena caliente).
    ///
    ///   3. PARPADEO CON APAGADO — el rayo se re-genera a ~15 Hz y además
    ///      tiene probabilidad de apagarse un frame entero (el factor #1
    ///      del look "rayo real"). Determinista por semilla.
    ///
    ///   4. JITTER PERPENDICULAR + DISPERSIÓN 2D con envolvente — los
    ///      EXTREMOS quedan anclados EXACTOS (un rayo flotando se ve falso).
    ///
    ///   5. MULTI-FILAMENTO — 2-3 rayos paralelos de 2 colores con
    ///      semillas independientes: más rico que un solo path ramificado.
    ///
    ///   6. RAMAS CON AUTO-CORRECCIÓN — cada paso rota al azar pero se
    ///      corrige contra la curvatura acumulada (los rayos siguen
    ///      rectos en promedio); las ramas heredan ~½ ancho.
    ///
    ///   7. MUERTE VIOLENTA — al disolverse la amplitud del jitter
    ///      REVIENTA (DeathGrow) mientras el alpha cae con easing suave.
    ///
    ///   8. IMPACTO = CRUZ DE LUZ + DESTELLO — 4 draws del glow en 2
    ///      orientaciones × 2 escalas + el estallido radial girando.
    ///
    /// CONTRATO (idéntico al de LightningCore): los métodos de DIBUJO
    /// reciben el batch ABIERTO en modo aditivo y no lo tocan — se pueden
    /// aninar dentro de un renderer mayor. Coordenadas tal cual lleguen.
    /// </summary>
    public static class StormLib
    {
        // ==================================================================
        //  PINCELES — las texturas de la tormenta (procedurales, v6.21)
        // ==================================================================

        private static Asset<Texture2D> _haloTex;
        private static Asset<Texture2D> _coreTex;
        private static Asset<Texture2D> _chainTex;
        private static Asset<Texture2D> _impactTex;
        private static Asset<Texture2D> _glowTex;

        private static Texture2D HaloTex =>
            (_haloTex ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/BoltHalo")).Value;

        private static Texture2D CoreTex =>
            (_coreTex ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/BoltCore")).Value;

        private static Texture2D ChainTex =>
            (_chainTex ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/BoltChain")).Value;

        private static Texture2D ImpactTex =>
            (_impactTex ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/BoltImpact")).Value;

        private static Texture2D GlowTex =>
            (_glowTex ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow")).Value;

        // ==================================================================
        //  EL RELOJ Y EL PARPADEO (deterministas)
        // ==================================================================

        /// <summary>Tick de regeneración: avanza ~hz veces por segundo.</summary>
        public static int FlickTick(float time, float hz = 15f)
            => (int)(time * hz);

        /// <summary>
        /// ¿El rayo está ENCENDIDO esta regeneración? La lección número UNO
        /// del look real: además de re-generar la forma, el rayo debe
        /// APAGARSE del todo un porcentaje de los frames (el parpadeo
        /// nervioso de las descargas de verdad). 0.62 ≈ encendido 6 de cada
        /// 10 regeneraciones.
        /// </summary>
        public static bool IsLit(int seed, int flick, float aliveChance = 0.62f)
            => VFXCore.Hash01(seed, 1234 + flick, 977) < aliveChance;

        /// <summary>
        /// Multiplicador de amplitud de la MUERTE VIOLENTA: mientras el
        /// rayo se disuelve (lifeT &gt; 0.55) el jitter CRECE hasta ~×3.2 —
        /// el rayo "revienta" en lugar de desvanecerse educadamente.
        /// </summary>
        public static float DeathGrow(float lifeT)
        {
            float t = MathHelper.Clamp((lifeT - 0.55f) / 0.45f, 0f, 1f);
            return 1f + 2.2f * t * t;
        }

        // ==================================================================
        //  GENERACIÓN (matemática determinista — la misma forma en todas
        //  las máquinas sin sincronizar nada)
        // ==================================================================

        /// <summary>
        /// LA POLILÍNEA DEL RAYO: N puntos entre start y end con jitter
        /// PERPENDICULAR + dispersión paralela, ambos con ENVOLVENTE
        /// senoidal (quietos en los anclajes, locos al medio). Los
        /// EXTREMOS quedan anclados EXACTOS.
        /// </summary>
        /// <param name="amp">Amplitud total del zigzag (px, ya escalada
        /// por el llamador — multiplícala por DeathGrow para la muerte).</param>
        public static Vector2[] ZigPath(Vector2 start, Vector2 end,
            int seed, int flick, int segments = 12, float amp = 24f)
        {
            if (segments < 1) segments = 1;
            Vector2[] pts = new Vector2[segments + 1];
            Vector2 dir = end - start;
            float len = dir.Length();
            if (len < 0.001f)
            {
                for (int i = 0; i <= segments; i++) pts[i] = start;
                return pts;
            }
            dir /= len;
            Vector2 normal = new Vector2(-dir.Y, dir.X);
            float ampLen = Math.Min(amp, len * 0.30f);

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                // Envolvente: la tensión vive en el CENTRO.
                float env = (float)Math.Pow(Math.Sin(t * Math.PI), 0.8);
                // Jitter perpendicular (el zigzag).
                float j = (VFXCore.Hash01(seed, flick, i * 31 + 7) - 0.5f) * 2f
                          * ampLen * env;
                // Dispersión paralela (el "hervir" a lo largo de la cuerda).
                float d = (VFXCore.Hash01(seed, flick, i * 13 + 3) - 0.5f) * 2f
                          * ampLen * 0.35f * env;
                pts[i] = start + dir * (len * t) + normal * j + dir * d;
            }
            pts[0] = start;
            pts[segments] = end;             // anclajes EXACTOS
            return pts;
        }

        /// <summary>
        /// EL HIMBERVOR: todos los puntos interiores derivan un poco por
        /// tick — el rayo entero HIERVE en su sitio (no solo serpentea la
        /// punta). Devuelve una COPIA hervida (no toca la original).
        /// </summary>
        public static Vector2[] Boil(Vector2[] pts, int seed, int tick, float amp = 3f)
        {
            if (pts == null || pts.Length < 3) return pts;
            Vector2[] copy = new Vector2[pts.Length];
            copy[0] = pts[0];
            copy[pts.Length - 1] = pts[pts.Length - 1];
            for (int i = 1; i < pts.Length - 1; i++)
            {
                float dx = (VFXCore.Hash01(seed, tick, i * 17 + 5) - 0.5f) * 2f * amp;
                float dy = (VFXCore.Hash01(seed, tick + 7919, i * 29 + 11) - 0.5f) * 2f * amp;
                copy[i] = pts[i] + new Vector2(dx, dy);
            }
            return copy;
        }

        /// <summary>
        /// LAS RAMAS del tronco: caminan con AUTO-CORRECCIÓN de curvatura
        /// (cada paso rota al azar pero se corrige contra el giro acumulado
        /// — la rama sigue recta en promedio) y heredan ~½ del ancho.
        /// Devuelve el tronco + sus ramas como strands listos para pintar.
        /// </summary>
        public static List<StormStrand> ForkTree(Vector2[] trunk, int seed, int flick,
            float branchScale = 0.5f, int maxBranches = 3)
        {
            var strands = new List<StormStrand>();
            if (trunk == null || trunk.Length < 4) return strands;
            strands.Add(new StormStrand(trunk, 1f, 1f));

            // La dirección local del tronco en el punto de partida de cada rama.
            for (int b = 0; b < maxBranches; b++)
            {
                int idx = 2 + (int)(VFXCore.Hash01(seed, flick * 7 + b, 313) * (trunk.Length - 4));
                if (idx < 1 || idx > trunk.Length - 2) idx = Math.Clamp(idx, 1, trunk.Length - 2);

                Vector2 origin = trunk[idx];
                Vector2 seg = trunk[idx + 1] - trunk[idx - 1];
                float segLen = seg.Length();
                if (segLen < 0.01f) continue;
                seg /= segLen;

                float side = VFXCore.Hash01(seed, flick * 3 + b, 617) > 0.5f ? 1f : -1f;
                float spread = 0.45f + 0.45f * VFXCore.Hash01(seed, flick + b, 619);
                Vector2 dir = seg.RotatedBy(side * spread);

                // Longitud de la rama: una fracción del tronco.
                float trunkLen = PathLength(trunk);
                float branchLen = trunkLen * (0.14f + 0.16f * VFXCore.Hash01(seed, b, 623));

                // EL PASEO con auto-corrección (la curvatura se autocorrige).
                const int Steps = 7;
                var pts = new Vector2[Steps + 1];
                Vector2 pos = origin;
                float totalRot = 0f;
                for (int s = 0; s <= Steps; s++)
                {
                    pts[s] = pos;
                    if (s == Steps) break;
                    float rot = (VFXCore.Hash01(seed, flick + s * 13, 811 + b) - 0.5f) * 0.6f;
                    rot -= totalRot * 0.3f;            // ← la auto-corrección
                    dir = dir.RotatedBy(rot);
                    totalRot += rot;
                    pos += dir * (branchLen / Steps);
                }

                strands.Add(new StormStrand(pts, branchScale, 0.85f));
            }
            return strands;
        }

        /// <summary>
        /// EL REFINO FRACTAL: subdivisión de PUNTO MEDIO — entre cada par
        /// de puntos inserta un midpoint desplazado sobre la perpendicular
        /// con jitter de escala MENOR (y sesgo cúbico: muchos toques
        /// sutiles, algún salto grande). Es lo que rompe la uniformidad
        /// geométrica del zigzag de un solo nivel: el rayo gana la
        /// rugosidad MULTI-ESCALA de las descargas de verdad.
        /// </summary>
        public static Vector2[] Refine(Vector2[] pts, int seed, int flick, float ampScale = 0.5f)
        {
            if (pts == null || pts.Length < 2) return pts;
            var refined = new List<Vector2>((pts.Length - 1) * 2 + 1);
            for (int i = 0; i < pts.Length - 1; i++)
            {
                Vector2 a = pts[i];
                Vector2 b = pts[i + 1];
                refined.Add(a);
                Vector2 seg = b - a;
                float len = seg.Length();
                if (len < 0.5f) continue;
                Vector2 dir = seg / len;
                Vector2 normal = new Vector2(-dir.Y, dir.X);
                // Sesgo cúbico: (h-0.5)^3·2·k → mayoría de toques finos.
                float h = VFXCore.Hash01(seed, flick * 3 + 1, i * 47 + 29) - 0.5f;
                float j = Math.Sign(h) * (float)Math.Pow(Math.Abs(h) * 2f, 1.5f)
                          * len * 0.22f * ampScale;
                Vector2 mid = (a + b) * 0.5f + normal * j;
                refined.Add(mid);
            }
            refined.Add(pts[pts.Length - 1]);
            return refined.ToArray();
        }

        /// <summary>Longitud total de una polilínea.</summary>
        public static float PathLength(Vector2[] pts)
        {
            if (pts == null || pts.Length < 2) return 0f;
            float total = 0f;
            for (int i = 0; i < pts.Length - 1; i++)
                total += (pts[i + 1] - pts[i]).Length();
            return total;
        }

        // ==================================================================
        //  EL RENDER — triple capa por sub-segmento (batch ABIERTO aditivo)
        // ==================================================================

        /// <summary>Máxima longitud de un sub-segmento dibujable (px).</summary>
        private const float SubMax = 42f;

        /// <summary>
        /// Dibuja UN filamento por su lista de puntos: halo + cuerpo +
        /// NÚCLEO BLANCO por sub-segmento (~42 px), brillo de grieta por
        /// sub-segmento, taper a lo largo y gorros en los extremos.
        /// </summary>
        /// <param name="batch">Batch ABIERTO en modo aditivo.</param>
        /// <param name="pts">La polilínea (ZigPath/Boil/ramas).</param>
        /// <param name="width">Ancho del CUERPO en px (el halo va ×2.5, el núcleo ×⅓).</param>
        /// <param name="halo">Color del halo/cuerpo.</param>
        /// <param name="core">Color del núcleo (casi blanco de verdad).</param>
        public static void Strand(SpriteBatch batch, Vector2[] pts, int seed, int flick,
            float width, Color halo, Color core, float alpha = 1f, StormTaper taper = StormTaper.Center)
            => StrandImpl(batch, pts, seed, flick, width, halo, core, alpha, taper, CoreTex, HaloTex);

        /// <summary>
        /// La CADENA eléctrica: el mismo filamento pero con la textura de
        /// eslabones (los saltos entre enemigos, las cadenas de bolas).
        /// </summary>
        public static void ChainBolt(SpriteBatch batch, Vector2 start, Vector2 end,
            int seed, int flick, float width, Color halo, Color core,
            float alpha = 1f, int segments = 8, float amp = 12f)
        {
            Vector2[] pts = ZigPath(start, end, seed, flick, segments, amp);
            StrandImpl(batch, pts, seed, flick, width, halo, core, alpha,
                StormTaper.Linear, ChainTex, HaloTex);
        }

        /// <summary>Atajo: UN filamento recto de A a B.</summary>
        public static void Bolt(SpriteBatch batch, Vector2 start, Vector2 end,
            int seed, int flick, float width, Color halo, Color core,
            float alpha = 1f, int segments = 12, float amp = 24f)
        {
            Vector2[] pts = ZigPath(start, end, seed, flick, segments, amp);
            StrandImpl(batch, pts, seed, flick, width, halo, core, alpha,
                StormTaper.Center, CoreTex, HaloTex);
        }

        /// <summary>
        /// EL RAYO DE VERDAD: MULTI-FILAMENTO — un tronco principal con
        /// REFINO FRACTAL (jaggedness multi-escala) al ancho completo +
        /// filamentos acompañantes (½ y ⅜) + DOS PELOS caóticos finísimos
        /// (el "hair" que rodea a las descargas reales) + RAMAS con
        /// auto-corrección. Es la superposición la que lee "descarga
        /// real", no un solo path.
        /// </summary>
        public static void MultiBolt(SpriteBatch batch, Vector2 start, Vector2 end,
            int seed, int flick, float width, Color haloA, Color haloB, Color core,
            float alpha = 1f, float amp = 26f, int segments = 12)
        {
            // === EL TRONCO PRINCIPAL (base + refino fractal) ===
            Vector2[] trunk = Refine(
                ZigPath(start, end, seed, flick, segments, amp), seed, flick);
            StrandImpl(batch, trunk, seed, flick, width, haloA, core, alpha,
                StormTaper.Center, CoreTex, HaloTex);

            // === LAS RAMAS (con auto-corrección, finas) ===
            List<StormStrand> forks = ForkTree(trunk, seed, flick, 0.40f, 4);
            for (int f = 1; f < forks.Count; f++)
            {
                StormStrand s = forks[f];
                StrandImpl(batch, s.Points, seed + 17 + f, flick,
                    width * s.WidthScale, haloA, core, alpha * s.Alpha,
                    StormTaper.Linear, CoreTex, HaloTex);
            }

            // === LOS FILAMENTOS ACOMPAÑANTES + LOS PELOS ===
            Vector2 dir = end - start;
            float len = dir.Length();
            if (len > 40f)
            {
                dir /= len;
                Vector2 normal = new Vector2(-dir.Y, dir.X);

                // Filamento B: ½ ancho, color secundario, deriva a un lado.
                if (IsLit(seed + 101, flick, 0.55f))
                {
                    Vector2 off = normal * (len * 0.035f);
                    Vector2[] ptsB = Refine(ZigPath(start + off * 1.2f, end + off * 0.4f,
                        seed + 211, flick, segments, amp * 0.8f), seed + 211, flick);
                    StrandImpl(batch, ptsB, seed + 211, flick, width * 0.52f,
                        haloB, core, alpha * 0.85f, StormTaper.Center, CoreTex, HaloTex);
                }

                // Filamento C: ⅜ ancho, al otro lado, más nervioso.
                if (IsLit(seed + 307, flick, 0.45f))
                {
                    Vector2 off = -normal * (len * 0.045f);
                    Vector2[] ptsC = Refine(ZigPath(start + off * 0.8f, end + off * 0.3f,
                        seed + 419, flick, segments + 3, amp * 0.9f), seed + 419, flick);
                    StrandImpl(batch, ptsC, seed + 419, flick, width * 0.38f,
                        haloA, core, alpha * 0.70f, StormTaper.Center, CoreTex, HaloTex);
                }

                // LOS PELOS: filamentos caóticos finísimos con jitter amplio
                // alrededor del tronco — el "hair" de la descarga real.
                for (int hair = 0; hair < 2; hair++)
                {
                    if (!IsLit(seed + 533 + hair * 97, flick, hair == 0 ? 0.50f : 0.40f))
                        continue;
                    int hseed = seed + 533 + hair * 97;
                    Vector2 off = normal * ((hair == 0 ? 1f : -1f) *
                        len * (0.02f + 0.03f * VFXCore.Hash01(hseed, 3, 997)));
                    Vector2[] ptsH = Refine(ZigPath(start + off, end + off * 0.2f,
                        hseed, flick, segments + 5, amp * 1.35f), hseed, flick, 0.7f);
                    StrandImpl(batch, ptsH, hseed, flick, width * (hair == 0 ? 0.24f : 0.20f),
                        hair == 0 ? haloB : haloA, core, alpha * 0.55f,
                        StormTaper.Center, CoreTex, HaloTex);
                }
            }
        }

        /// <summary>
        /// ARCO ELÉCTRICO alrededor de un centro (las coronas de impacto):
        /// el arco de LightningCore re-implementado con las texturas de
        /// filamento — mucho más crispado.
        /// </summary>
        public static void ArcRing(SpriteBatch batch, Vector2 center, float radius,
            float a1, float a2, int seed, int flick, float width,
            Color halo, Color core, float alpha = 1f, int count = 10)
        {
            Vector2[] pts = new Vector2[count + 1];
            for (int i = 0; i <= count; i++)
            {
                float t = i / (float)count;
                float ang = a1 + (a2 - a1) * t;
                float r = radius * (1f + (VFXCore.Hash01(seed, flick, i * 17 + 3) - 0.5f) * 2f * 0.16f
                                    * (float)Math.Sin(t * Math.PI));
                pts[i] = center + new Vector2((float)Math.Cos(ang) * r, (float)Math.Sin(ang) * r);
            }
            StrandImpl(batch, pts, seed, flick, width, halo, core, alpha,
                StormTaper.Center, CoreTex, HaloTex);
        }

        // ------------------------------------------------------------------
        //  EL IMPACTO
        // ------------------------------------------------------------------

        /// <summary>
        /// EL ESTALLIDO DE IMPACTO: el destello radial girando + la CRUZ DE
        /// LUZ (4 draws del glow: 2 orientaciones × 2 escalas) + el punto
        /// cegador. `intensity` 0..1 (los primeros ticks del golpe: pásalo
        /// decayendo para el flash de descarga múltiple).
        /// </summary>
        public static void ImpactFlash(SpriteBatch batch, Vector2 pos, float size,
            Color color, float intensity, float spin)
        {
            if (intensity <= 0.02f) return;
            Color white = new Color(255, 250, 235);

            // 1. El destello radial (girando lento, dos escalas apiladas).
            Quad(batch, ImpactTex, pos, new Vector2(size * 2.0f, size * 2.0f), spin,
                Tint(color, 0.50f * intensity));
            Quad(batch, ImpactTex, pos, new Vector2(size * 1.25f, size * 1.25f), -spin * 0.7f,
                Tint(color, 0.70f * intensity));

            // 2. LA CRUZ DE LUZ: 2 orientaciones × 2 escalas.
            Quad(batch, GlowTex, pos, new Vector2(size * 0.62f, size * 2.35f), 0f,
                Tint(color, 0.40f * intensity));
            Quad(batch, GlowTex, pos, new Vector2(size * 2.35f, size * 0.62f), 0f,
                Tint(color, 0.40f * intensity));
            Quad(batch, GlowTex, pos, new Vector2(size * 0.40f, size * 1.55f), 0f,
                Tint(white, 0.50f * intensity));
            Quad(batch, GlowTex, pos, new Vector2(size * 1.55f, size * 0.40f), 0f,
                Tint(white, 0.50f * intensity));

            // 3. El punto cegador (el nucleo del impacto).
            Quad(batch, GlowTex, pos, new Vector2(size * 0.85f, size * 0.85f), 0f,
                Tint(color, 0.80f * intensity));
            Quad(batch, GlowTex, pos, new Vector2(size * 0.42f, size * 0.42f), 0f,
                Tint(white, 0.95f * intensity));
        }

        /// <summary>
        /// El GORRO de descarga en un extremo del rayo: glow a DOS escalas
        /// apiladas (el truco del doble-draw para intensidad barata).
        /// </summary>
        public static void EndCap(SpriteBatch batch, Vector2 pos, float width,
            Color halo, Color core, float alpha)
        {
            Quad(batch, GlowTex, pos, new Vector2(width * 3.2f, width * 3.2f), 0f,
                Tint(halo, 0.42f * alpha));
            Quad(batch, GlowTex, pos, new Vector2(width * 1.8f, width * 1.8f), 0f,
                Tint(core, 0.78f * alpha));
        }

        // ------------------------------------------------------------------
        //  LA ESCENA
        // ------------------------------------------------------------------

        /// <summary>
        /// LUZ ESTRANGULADA a lo largo del camino: un AddLight cada
        /// `everyPx` píxeles de arco (no por punto, no por frame) — la luz
        /// dinámica cuesta más que el propio triángulo.
        /// </summary>
        public static void AddLightAlong(Vector2[] pts, float r, float g, float b,
            float strength = 1f, float everyPx = 48f)
        {
            if (pts == null || pts.Length < 2 || strength <= 0.01f) return;
            float acc = everyPx;                       // pinta el primero
            Lighting.AddLight(pts[0], r * strength, g * strength, b * strength);
            for (int i = 1; i < pts.Length; i++)
            {
                acc += (pts[i] - pts[i - 1]).Length();
                if (acc >= everyPx)
                {
                    Lighting.AddLight(pts[i], r * strength, g * strength, b * strength);
                    acc = 0f;
                }
            }
        }

        // ==================================================================
        //  PRIMITIVAS INTERNAS
        // ==================================================================

        /// <summary>
        /// EL MOTOR DEL FILAMENTO: recorre la polilínea en sub-segmentos de
        /// ~42 px y pinta las TRES capas (halo ancho + cuerpo + núcleo
        /// blanco) con brillo de grieta por sub-segmento y taper.
        /// </summary>
        private static void StrandImpl(SpriteBatch batch, Vector2[] pts, int seed, int flick,
            float width, Color halo, Color core, float alpha, StormTaper taper,
            Texture2D bodyTex, Texture2D haloTex)
        {
            if (pts == null || pts.Length < 2 || alpha <= 0.01f || width <= 0.05f) return;

            float total = PathLength(pts);
            if (total < 1f) return;

            float arc = 0f;
            int k = 0;
            for (int i = 0; i < pts.Length - 1; i++)
            {
                Vector2 a = pts[i];
                Vector2 b = pts[i + 1];
                Vector2 seg = b - a;
                float len = seg.Length();
                if (len < 0.35f) { arc += len; continue; }

                // Sub-segmentación: las grietas viven a escala fina.
                int m = Math.Max(1, (int)Math.Ceiling(len / SubMax));
                for (int s = 0; s < m; s++)
                {
                    Vector2 p0 = a + seg * (s / (float)m);
                    Vector2 p1 = a + seg * ((s + 1) / (float)m);
                    Vector2 sub = p1 - p0;
                    float subLen = sub.Length();
                    if (subLen < 0.30f) { arc += subLen; continue; }
                    float rot = (float)Math.Atan2(sub.Y, sub.X);
                    Vector2 mid = (p0 + p1) * 0.5f;
                    float tMid = (arc + subLen * 0.5f) / total;

                    // El TAPER y la GRIETA de este sub-segmento.
                    float w = width * TaperFactor(taper, tMid);
                    float crackle = 0.62f + 0.38f * VFXCore.Hash01(seed, flick, k * 41 + 17);

                    // 1) EL HALO — banda suave, ancho, del color (contenido:
                    //                    el halo NO debe engullir al filamento).
                    Quad(batch, haloTex, mid,
                        new Vector2(subLen + w * 2.0f, w * 2.0f), rot,
                        Tint(halo, 0.30f * alpha * crackle));
                    // 2) EL CUERPO — el filamento de color con sus grietas.
                    Quad(batch, bodyTex, mid,
                        new Vector2(subLen + w * 1.0f, w * 1.0f), rot,
                        Tint(halo, 0.85f * alpha * crackle));
                    // 3) EL NÚCLEO — la vena BLANCA razor-fina a ¼ del ancho.
                    Quad(batch, bodyTex, mid,
                        new Vector2(subLen + w * 0.45f, w * 0.26f), rot,
                        Tint(core, 1f * alpha));

                    arc += subLen;
                    k++;
                }
            }

            // Los GORROS de descarga en ambos extremos.
            EndCap(batch, pts[0], width, halo, core, alpha);
            EndCap(batch, pts[pts.Length - 1], width, halo, core, alpha);
        }

        /// <summary>La curva de anchura (el contrato del taper).</summary>
        private static float TaperFactor(StormTaper taper, float t)
        {
            switch (taper)
            {
                case StormTaper.Linear:
                    return MathHelper.Lerp(1f, 0.38f, t);
                case StormTaper.Impact:
                    return MathHelper.Lerp(0.30f, 1f, (float)Math.Pow(t, 0.6));
                default: // Center
                    return Math.Max(0.35f,
                        (float)Math.Pow(Math.Sin(t * Math.PI), 0.65));
            }
        }

        /// <summary>Quad centrado con rotación (tamaño total = size px).</summary>
        private static void Quad(SpriteBatch batch, Texture2D tex, Vector2 pos,
            Vector2 size, float rot, Color tint)
        {
            if (tint.A == 0) return;
            batch.Draw(tex, pos, null, tint, rot,
                new Vector2(tex.Width, tex.Height) * 0.5f,
                size / new Vector2(tex.Width, tex.Height),
                SpriteEffects.None, 0f);
        }

        /// <summary>Tinte de INTENSIDAD LINEAL (patrón validado del proyecto).</summary>
        private static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color(c.R, c.G, c.B, (byte)(int)(255f * f));
        }
    }
}
