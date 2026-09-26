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
    /// StormLib — v6.39 — LA TERCERA GENERACIÓN: LA REPARACIÓN DEL ADITIVO.
    ///
    /// v6.39 — el informe research/v637 encontró DOS raíces medidas en la
    /// v6.21 que juntas producían el artefacto reportado por el usuario
    /// ("líneas intermitentes de un color más oscuro o claro, brillo y
    /// desenfoque CORTADO POR SECCIONES"):
    ///
    ///   RAÍZ 1 — EL PERFIL VIVE EN EL RGB. La teoría v6.39 decía que
    ///   BlendState.Additive de XNA/FNA era (Src=One, Dst=One) y que el
    ///   alfa "no entraba nunca" — v6.50.3 lo SONDEÓ contra el FNA.dll
    ///   real del tML 2026.07.3.0: Additive es (SourceAlpha, One) — el
    ///   alfa GATEA el aporte de verdad. Las texturas Bolt* v6.21 llevaban
    ///   el filamento SOLO en el alfa (RGB=blanco) → contribution =
    ///   blanco·alfa — el filamento SÍ se veía, pero sin color propio y a
    ///   brillo plano; la convención de la casa (perfil horneado en RGB,
    ///   SoftGlow, GlowOrb… desde v5.x) es la correcta y v6.39 la aplicó.
    ///   v6.50.7 — la sonda v6.50.3 estaba INCOMPLETA: también sondeó el
    ///   AlphaBlend=(One, InvSourceAlpha) (compositing PREMULTIPLICADO) y
    ///   el loader premultiplica los PNG (PngReader.PreMultiplyAlpha):
    ///   el tinte lineal (RGB intacto + alfa=f) rompía los lotes de masa
    ///   (color sin escalar) — REVERTIDO al premultiplicado (RGB·f + alfa·f)
    ///   de v6.25/v6.39. PERO la revertida global dejó pasar UNA CUARTA
    ///   raíz, medida y cerrada en v6.50.9 (ver Tint): en el lote ADITIVO el
    ///   aporte real es P.rgb·P.a²·color·f² — el f² mataba el halo (0.30 →
    ///   0.055·color, invisible) y TODO rayo del mod quedaba en una línea
    ///   delgada sin resplandor. El tinte v6.50.9 (RGB·√f, A·√f) da la
    ///   intensidad LINEAL correcta en el aditivo y compone premult bien en
    ///   cualquier lote alfa — auditoría 30/30 rutas aditivas, 0 violaciones.
    ///
    ///   RAÍZ 2 — EL RIBBON SE CORTABA. Cada sub-segmento solapaba al vecino
    ///   `subLen + w*2` → el aditivo APILABA el brillo en cada junta (las
    ///   "cuentas" claras del v6.21). v6.39 lo enterró: quads BORDE A
    ///   BORDE con la normal media y el largo exacto proyectado.
    ///
    ///   v6.50.8 — RAÍZ 3 (el reporte del usuario: "los rayos son solo
    ///   líneas discontinuas, no son rayos de verdad"): ChainBolt dibujaba
    ///   su cuerpo con ChainTex — la textura de ESLABONES, un patrón de
    ///   GUIONES a lo largo — y el tronco era un ZigPath de UNA sola
    ///   escala. Reconstruido con la receta canónica de los relámpagos 2D
    ///   (midpoint displacement multi-escala + ramas + bandas suaves):
    ///   ver ChainBolt/Bolt/MultiBolt. BoltChain.png queda como asset
    ///   retirado (la convención de la casa).
    ///
    ///   v6.50.22 — LA GENERACIÓN 100% CÓDIGO (el reporte del usuario:
    ///   «los rayos se siguen viendo mal, se nota que son líneas que se
    ///   unen a otra línea, creo que usas un sprite para los rayos,
    ///   recuerda que los rayos deben ser creados mediante código, nada
    ///   de sprite»). Tenía razón DOS veces: (1) el "sprite" existía —
    ///   BoltHalo.png/BoltCore.png, bandas horneadas con perfil
    ///   transversal (¡y con SUELO de alfa ~50 en los bordes largos de
    ///   BoltHalo! medido por píxeles: cada capa del rayo dibujaba
    ///   BORDES DUROS de banda — de ahí el look de "líneas"); (2) la
    ///   referencia del usuario (el commit v5.54) era el rayo DEL MOTOR:
    ///   el LightningArc 466 de vanilla — que NO usa sprite de rayo del
    ///   mod, sino EL MOTOR: 3 pasadas apiladas de ancho decreciente
    ///   (0.6/0.4/0.2 · color×0.5, aditivo) sobre el camino jagged — el
    ///   PERFIL transversal suave ES LA SUMA de las pasadas sólidas.
    ///
    ///   LA RECONSTRUCCIÓN: el pincel pasa a ser EL PIXEL 1×1 BLANCO del
    ///   motor (TextureAssets.MagicPixel — la primitiva de rectángulo
    ///   sólido que vanilla expone; cero arte del mod) y el filamento se
    ///   pinta con LA PILA DE 6 PASADAS + VENA (la receta 466 extendida):
    ///   anchos [5.2, 3.6, 2.5, 1.7, 1.15, 0.72]×w con brillos
    ///   telescópicos [0.06 … 0.42] + la vena BLANCA al 34% — la suma de
    ///   las pasadas ES la gaussiana transversal (cada pasada aporta
    ///   color·f EXACTO: el pixel es sólido, sin perfil de textura que
    ///   elevar al cuadrado). Juntas BORDE A BORDE exactas (mismo avg →
    ///   líneas de corte colineales: la regla de relleno del rasterizador
    ///   tesela sin hueco) + extensión de giro por pasada con suelo
    ///   0.35px (la regla top-left mata el hueco de 1-ulp; el suelo
    ///   mataría el riesgo de driver). BoltHalo/BoltCore se RETIRAN del
    ///   consumo (la convención de la casa con BoltChain).
    ///
    ///   EL IMPACTO (el otro reporte: «se ve el cuadrado cuando aparece,
    ///   debería tener un degradado suave y difuminarse al final, hacia
    ///   los bordes quedar transparente»): BoltImpact.png estaba RECORTADO
    ///   por el lienzo — alfa ~29 en todo el perímetro (medido) → el borde
    ///   del quad se leía como un cuadrado. Re-horneado con el contrato
    ///   NovaBurst de la casa: perfil monotónico 236→0 que MUERE a 0
    ///   EXACTO en el borde (verificado por píxeles por el generador
    ///   tools/gen_boltimpact_v65022.py).
    ///
    /// Nace de la investigación profunda y metódica del ecosistema (los
    /// sistemas de rayos de los grandes mods de VFX, estudiados a fondo:
    /// generación, flicker, multi-filamento, tapers, perfiles de textura,
    /// impactos) y se re-implementa 100% con código PROPIO sobre la pila de
    /// la casa: SpriteBatch + texturas procedurales + hash determinista.
    ///
    /// LA DIFERENCIA con la primera generación (v6.18, ya retirada del
    /// proyecto): aquella dibujaba cápsulas de GLOW suave — era
    /// una tira de energía difusa. StormLib dibuja FILAMENTOS de verdad:
    ///
    ///   1. TEXTURAS DE FILAMENTO — v6.39: BoltHalo y BoltCore son BANDAS
    ///      UNIFORMES a lo largo (solo 3 px de fundido antialias en los
    ///      extremos) con el perfil PREMULTIPLICADO en RGB (la convención
    ///      SoftGlow de la casa; el loader vuelve a premultiplicar encima —
    ///      lo absorbe el Tint v6.50.9 de intensidad lineal).
    ///      La nitidez de un rayo vive en la GEOMETRÍA multi-escala y en el
    ///      crackle por punto, NUNCA en ruido horneado a lo largo de la
    ///      textura (eso era el "cortado por secciones").
    ///
    ///   2. TRIPLE CAPA por segmento — halo ancho de color + cuerpo
    ///      del color + NÚCLEO BLANCO a ~¼ del ancho (la vena caliente),
    ///      quads BORDE A BORDE (largo exacto proyectado + extensión
    ///      adaptativa de giro — cero apilamiento aditivo en juntas rectas).
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
    /// CONTRATO (el de siempre, heredado de la primera generación): los
    /// métodos de DIBUJO reciben el batch ABIERTO en modo aditivo y no lo
    /// tocan — se pueden ANIDAR dentro de un renderer mayor. Coordenadas
    /// tal cual lleguen. (Por eso el Tint de INTENSIDAD LINEAL: TODO lo que
    /// dibuja esta librería vive en el lote aditivo — 30/30 rutas auditadas
    /// en v6.50.9, 0 violaciones — donde el aporte real es
    /// P.rgb·P.a²·color·f: ver el Tint.)
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

        /// <summary>v6.50.8 — RETIRADA del consumo: la banda de ESLABONES
        /// (patrón de guiones) era la causa del reporte del usuario ("los
        /// rayos son solo líneas discontinuas"). El asset BoltChain.png y
        /// esta carga se conservan por la convención de la casa con lo
        /// retirado — NADIE la dibuja desde v6.50.8.</summary>
        private static Texture2D ChainTex =>
            (_chainTex ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/BoltChain")).Value;

        private static Texture2D ImpactTex =>
            (_impactTex ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/BoltImpact")).Value;

        private static Texture2D GlowTex =>
            (_glowTex ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow")).Value;

        /// <summary>v6.50.22 — RETIRADA DEL CONSUMO (como BoltChain en
        /// v6.50.8): el rayo ya no usa bandas horneadas — el pincel es EL
        /// PIXEL del motor (VFXCore.Pixel) y el perfil transversal ES LA
        /// SUMA de las pasadas de la pila. El asset se conserva por la
        /// convención de la casa con lo retirado — NADIE la dibuja desde
        /// v6.50.22.</summary>
        public static Texture2D BandaTex => HaloTex;

        /// <summary>v6.50.22 — RETIRADA DEL CONSUMO (la vena es también un
        /// quad del pixel del motor ahora). Conservada por convención.</summary>
        public static Texture2D VenaTex => CoreTex;

        // ==================================================================
        //  v6.50.22 — LA PILA DE PASADAS (el filamento 100% código)
        // ==================================================================

        /// <summary>
        /// LOS ANCHOS DE LA PILA (múltiplos del ancho base w): la receta
        /// del LightningArc 466 de vanilla (0.6/0.4/0.2 del scale)
        /// extendida a 6 pasadas para que la suma lea como una gaussiana
        /// suave — con el pincel SÓLIDO (el pixel del motor) cada paso de
        /// intensidad entre pasadas tiene que ser pequeño para que el
        /// ojo no lea "anillos". PÚBLICA: BoltRenderer (el rayo del buffer
        /// de VFXCore) comparte LA MISMA receta — una sola anatomía de
        /// rayo en todo el mod.
        /// </summary>
        public static readonly float[] PilaW = { 5.2f, 3.6f, 2.5f, 1.7f, 1.15f, 0.72f };

        /// <summary>
        /// LOS BRILLOS DE LA PILA (telescópicos): la suma acumulada al
        /// centro es 1.17·color + la vena blanca — el mismo balance de la
        /// v6.50.15 (bloom 0.066 + halo 0.123 + cuerpo 0.774 ≈ 0.96 tras
        /// el a² de las bandas viejas). Con el pixel sólido el aporte es
        /// color·f EXACTO (sin el a² de la textura) — de ahí los valores
        /// más bajos por pasada.
        /// </summary>
        public static readonly float[] PilaF = { 0.06f, 0.09f, 0.13f, 0.19f, 0.28f, 0.42f };

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
        //  v6.50.17 — EL ÁRBOL DE DESCARGA (la estructura que el usuario
        //  pide con nombre y apellidos: «un rayo debe estar creado como
        //  rama de un árbol, el final de una línea debe conectarse con el
        //  inicio de otra, en zigzag, extendiéndose como ramas de un árbol
        //  tipo fractal»)
        // ==================================================================

        /// <summary>El presupuesto de ramas del árbol (tronco aparte).</summary>
        private const int MaxRamasArbol = 10;

        /// <summary>
        /// EL ÁRBOL DEL RAYO: el tronco + sus ramas RECURSIVAS (ramas de
        /// ramas — cada nivel hereda ×0.62 de ancho y ×0.74 de brillo).
        ///
        /// LA CONEXIÓN (lo que el usuario describe): cada rama NACE en un
        /// VÉRTICE del padre — el FINAL de un segmento del padre es el
        /// PRINCIPIO de la rama — y cada rama es a su vez un ZIGZAG FRACTAL
        /// (FractalPath multi-escala), no un paseo recto. La longitud de
        /// cada rama es proporcional a lo que QUEDA del padre (la regla del
        /// árbol: cerca de la meta, ramitas; en la raíz, ramas grandes).
        /// Determinista por (seed, flick): todas las máquinas ven el MISMO
        /// árbol, y el flick lo RE-GERMINA a 15 Hz.
        ///
        /// Devuelve strands[0] = el tronco (1,1) + las ramas con sus
        /// WidthScale/Alpha ya escalados por nivel.
        /// </summary>
        public static List<StormStrand> BuildTree(Vector2[] trunk, int seed, int flick,
            int profundidad, float lenTronco)
        {
            var strands = new List<StormStrand> { new StormStrand(trunk, 1f, 1f) };
            Ramificar(strands, trunk, seed, flick, 1f, 1f, profundidad, lenTronco);
            return strands;
        }

        /// <summary>
        /// LA RECURSIÓN del árbol: recorre los VÉRTICES del camino padre y
        /// donde el hash lo pide germina una rama fractal que a su vez
        /// llama a ESTE método (el fractal de árbol de verdad — profundidad
        /// 2-3: rama → ramita → retonito).
        /// </summary>
        private static void Ramificar(List<StormStrand> strands, Vector2[] padre, int seed, int flick,
            float wPadre, float aPadre, int prof, float lenPadre)
        {
            if (prof <= 0 || strands.Count >= MaxRamasArbol + 1) return;
            float largoPadre = PathLength(padre);
            if (largoPadre < 36f || padre.Length < 4) return;

            int n = padre.Length;
            for (int i = 1; i < n - 1 && strands.Count < MaxRamasArbol + 1; i++)
            {
                float t = i / (float)(n - 1);

                // La probabilidad por vértice: MÁS ramas cerca del origen
                // (la raíz alimenta), menos hacia la meta — y el nivel 1
                // germina más que el 2.
                float pRama = 0.30f * (1f - 0.55f * t) * (prof >= 2 ? 1.15f : 0.85f);
                if (VFXCore.Hash01(seed, flick * 13 + prof, i * 37 + 11) > pRama) continue;

                // La TANGENTE LOCAL del padre en el vértice (la rama
                // continúa la dirección del tramo, girada a su lado).
                Vector2 tang = padre[Math.Min(i + 1, n - 1)] - padre[Math.Max(i - 1, 0)];
                float tl = tang.Length();
                if (tl < 0.01f) continue;
                tang /= tl;

                float lado = VFXCore.Hash01(seed, flick + i, 613 + prof) > 0.5f ? 1f : -1f;
                float ang = lado * (0.30f + 0.40f * VFXCore.Hash01(seed, i * 7, 617 + prof)); // 17°..40°
                Vector2 dir = tang.RotatedBy(ang);

                // La regla del árbol: la rama mide una fracción de lo que
                // QUEDA del padre (30-60% del resto).
                float restante = lenPadre * MathF.Max(0f, 1f - t);
                float largo = restante * (0.30f + 0.30f * VFXCore.Hash01(seed, i, 619 + prof));
                if (largo < 16f) continue;

                // LA RAMA ES FRACTAL TAMBIÉN: zigzag multi-escala (no un
                // paseo recto) — el material del rayo es el mismo en cada
                // escala del árbol.
                int gens = Math.Clamp((int)MathF.Round(MathF.Log2(MathF.Max(4f, largo / 12f))), 2, 4);
                int seedRama = seed + 313 + i * 17 + prof * 101;
                Vector2[] rama = FractalPath(padre[i], padre[i] + dir * largo,
                    seedRama, flick, gens, 0.14f);

                float w = wPadre * 0.62f;
                float a = aPadre * 0.74f;
                strands.Add(new StormStrand(rama, w, a));

                // LA RECURSIÓN: las ramas tienen ramas (el árbol fractal).
                Ramificar(strands, rama, seedRama, flick, w, a, prof - 1, largo);
            }
        }

        // ==================================================================
        //  v6.50.17 — EL RUIDO 1D (el motor del PerlinBolt: value noise
        //  determinista por semilla sobre el hash de la casa + fBm de
        //  octavas — la MISMA matemática del CampoFbm del aura, en 1D)
        // ==================================================================

        /// <summary>
        /// EL VALUE NOISE 1D: retícula de valores hash interpolados con
        /// smoothstep (el Perlin «de andar por casa» — determinista por
        /// semilla, cero estado, cero GC).
        /// </summary>
        public static float Ruido1D(float x, int semilla)
        {
            int i = (int)MathF.Floor(x);
            float f = x - i;
            float u = f * f * (3f - 2f * f);
            float a = VFXCore.Hash01(semilla, i, 1013);
            float b = VFXCore.Hash01(semilla, i + 1, 1013);
            return a + (b - a) * u;
        }

        /// <summary>
        /// EL fBm 1D (fractal Brownian motion): octavas con lacunaridad 2
        /// y persistencia 0.5 (los pesos canónicos 1/0.5/0.25… — la receta
        /// de la investigación R57-a). Devuelve 0..1.
        /// </summary>
        public static float Fbm1D(float x, int semilla, int octavas = 3)
        {
            if (octavas < 1) octavas = 1;
            float suma = 0f, peso = 1f, norma = 0f, freq = 1f;
            for (int o = 0; o < octavas; o++)
            {
                suma += Ruido1D(x * freq, semilla + o * 131) * peso;
                norma += peso;
                peso *= 0.5f;
                freq *= 2f;
            }
            return suma / MathF.Max(norma, 0.0001f);
        }

        // ==================================================================
        //  EL RENDER — triple capa borde a borde (batch ABIERTO aditivo)
        // ==================================================================

        /// <summary>
        /// Dibuja UN filamento por su lista de puntos — v6.39, EL RIBBON DE
        /// VERDAD: cada segmento es UN quad tocando a sus vecinos BORDE A
        /// BORDE (la NORMAL MEDIA orienta la junta — el quad de ayer y el de
        /// mañana comparten el corte; el LARGO es la distancia PROYECTADA
        /// sobre esa dirección media + la EXTENSIÓN ADAPTATIVA DE GIRO de
        /// RiftLib v6.31: w/2·tan(δ/2) SOLO donde el camino gira de verdad).
        /// Tres capas de ancho (halo ×2 / cuerpo ×1 / vena ×¼), BANDAS
        /// UNIFORMES a lo largo (brillo CONTINUO — el v6.21 solapaba
        /// subLen+w*2 y el aditivo APILABA cada junta: las "cuentas
        /// brillantes") y EL CRACKLE POR PUNTO (el brillo respira entre
        /// VÉRTICES interpolado, nunca por sub-sección dura: el
        /// "cortado por secciones"). Taper a lo largo y gorros en extremos.
        /// </summary>
        /// <param name="batch">Batch ABIERTO en modo aditivo.</param>
        /// <param name="pts">La polilínea (ZigPath/Boil/ramas).</param>
        /// <param name="width">Ancho del CUERPO en px (el halo va ×2, la vena ×¼).</param>
        /// <param name="halo">Color del halo/cuerpo.</param>
        /// <param name="core">Color del núcleo (casi blanco de verdad).</param>
        public static void Strand(SpriteBatch batch, Vector2[] pts, int seed, int flick,
            float width, Color halo, Color core, float alpha = 1f, StormTaper taper = StormTaper.Center)
            => StrandImpl(batch, pts, seed, flick, width, halo, core, alpha, taper);

        /// <summary>
        /// v6.50.8 — LA DESCARGA DE VERDAD ENTRE DOS ANCLAJES (la
        /// reconstrucción del reporte del usuario: "los rayos son solo
        /// líneas discontinuas, no son rayos de verdad").
        ///
        /// Hasta v6.50.7 este método dibujaba su CUERPO con ChainTex (la
        /// textura de ESLABONES, un patrón de guiones premultiplicado a lo
        /// largo de la banda) sobre un ZigPath de UNA sola escala → el
        /// resultado literal era una LÍNEA DISCONTINUA, no un rayo. El
        /// usuario pidió investigar la librería pre-auditoría + cómo se
        /// crean los rayos reales: la receta canónica de los efectos de
        /// relámpago 2D ("How to Generate Shockingly Good 2D Lightning
        /// Effects", la escuela clásica del midpoint displacement) es:
        ///
        ///   1. EL TRONCO por MIDPOINT DISPLACEMENT MULTI-ESCALA
        ///      (FractalPath: cada generación parte el segmento por su
        ///      punto medio desplazado la perpendicular con offset que SE
        ///      DIVIDE A LA MITAD — lazadas grandes + micro-detalle) más
        ///      el refino cúbico de la casa (Refine).
        ///   2. LAS RAMAS (la firma visual de una descarga): ForkTree
        ///      camina 2-3 horquillas con auto-corrección de curvatura,
        ///      heredando ~½ del ancho y ~85% del brillo — y sus puntas
        ///      llevan gorro de descarga (StrandImpl los añade).
        ///   3. EL PINTADO de 3 capas con las BANDAS PREMULTIPLICADAS
        ///      (halo/cuerpo/vena — StrandImpl borde a borde con la
        ///      normal media y el crackle por punto).
        ///
        /// La textura de eslabones (ChainTex/BoltChain.png) queda como
        /// asset sin consumidores (la convención de la casa con lo
        /// retirado). Determinista por (seed, flick) — todas las máquinas
        /// ven el MISMO rayo; el llamador lo REGENERA con flick para que
        /// la descarga VIVA (IsLit/FlickTick siguen siendo la receta).
        /// </summary>
        public static void ChainBolt(SpriteBatch batch, Vector2 start, Vector2 end,
            int seed, int flick, float width, Color halo, Color core,
            float alpha = 1f, int segments = 8, float amp = 12f)
        {
            float len = Vector2.Distance(start, end);
            if (len < 4f || alpha <= 0.01f || width <= 0.05f) return;

            // === 1. EL TRONCO FRACTAL (midpoint displacement) ===
            // segments→generaciones (potencias de 2 de tramos base).
            // v6.50.15 — EL CONTRATO ABSOLUTO DEL AMP (defecto 4 del
            // forense R55-d): la v6.50.8 volvió el amp RELATIVO
            // (chaos = amp·2.2/len) y las cadenas de 80-150px derivaban
            // 20-37px de su cuerda — garabatos, no arcos tensos. La
            // vieja buena (v6.39) prometía amp EN PX capados al 30% del
            // largo (ampLen = min(amp, len·0.30)): ESE contrato vuelve —
            // la gen-0 desplaza como MUCHO amp px, estire o encoga la
            // cadena. Y SIN el Refine extra: el midpoint displacement ya
            // es multi-escala; el refino cúbico encima solo SUMABA
            // wiggle (defecto 6).
            int gens = Math.Clamp((int)MathF.Round(MathF.Log2(Math.Max(4, segments))), 2, 4);
            float ampPx = Math.Min(amp, len * 0.30f);
            float chaos = Math.Clamp(ampPx / Math.Max(len, 1f), 0.02f, 0.30f);
            Vector2[] trunk = FractalPath(start, end, seed, flick, gens, chaos);

            // El tronco a brillo completo (taper lineal: tenso al anclaje
            // de destino, como la vena de una descarga que se disipa).
            StrandImpl(batch, trunk, seed, flick, width, halo, core, alpha,
                StormTaper.Linear);

            // === 2. v6.50.17 — LAS RAMAS DEL ÁRBOL FRACTAL (ramas de ramas,
            //     CONECTADAS por vértices: el final de un segmento es el
            //     inicio de la rama — la estructura de árbol que el
            //     usuario pide). PROGRESIVAS: 1 nivel si len>120, 2 si
            //     len>350 (las cadenas cortas son puro ruido con ramas). ===
            if (len > 120f)
            {
                int prof = len > 350f ? 2 : 1;
                List<StormStrand> arbol = BuildTree(trunk, seed, flick, prof, len);
                for (int f = 1; f < arbol.Count; f++)
                {
                    StormStrand s = arbol[f];
                    StrandImpl(batch, s.Points, seed + 23 + f, flick,
                        width * s.WidthScale, halo, core, alpha * s.Alpha,
                        StormTaper.Linear);
                }
            }
        }

        /// <summary>
        /// Atajo: UN filamento de A a B — v6.50.8: el tronco también es
        /// FRACTAL multi-escala (midpoint displacement + refino cúbico,
        /// la receta ChainBolt) en vez del ZigPath de una sola escala —
        /// los rayos de TODOS los renderers de la casa (lluvia naranja del
        /// Umbral, rayos fugitivos del Supremo, saltos del Sol Rúnico,
        /// bordes de los portales) ganan la rugosidad de las descargas
        /// de verdad.
        /// </summary>
        public static void Bolt(SpriteBatch batch, Vector2 start, Vector2 end,
            int seed, int flick, float width, Color halo, Color core,
            float alpha = 1f, int segments = 12, float amp = 24f)
        {
            float len = Vector2.Distance(start, end);
            if (len < 4f) return;

            int gens = Math.Clamp((int)MathF.Round(MathF.Log2(Math.Max(4, segments))), 3, 5);
            // v6.50.9: el suelo baja a 0.02 — el telegraph del cetro (amp 12
            // sobre ~810 px de caída) pedía chaos 0.03 y el suelo 0.06 lo
            // FORZABA a 49 px de deriva: la "línea fina de aviso" salía
            // 4× más serpenteante que su diseño. Los rayos largos con poca
            // amplitud respetan ahora su intención.
            // v6.50.15 — EL CONTRATO ABSOLUTO DEL AMP (ChainBolt lo
            // documenta completo): la deriva de la gen-0 vuelve a medir
            // amp PX de verdad, y SIN el Refine extra encima.
            float ampPx = Math.Min(amp, len * 0.30f);
            float chaos = Math.Clamp(ampPx / Math.Max(len, 1f), 0.02f, 0.26f);
            Vector2[] pts = FractalPath(start, end, seed, flick, gens, chaos);
            StrandImpl(batch, pts, seed, flick, width, halo, core, alpha,
                StormTaper.Center);
        }

        /// <summary>
        /// EL RAYO DE VERDAD: EL ÁRBOL — v6.50.17 (la re-construcción que el
        /// usuario pide: «un rayo debe estar creado como rama de un árbol,
        /// el final de una línea debe conectarse con el inicio de otra, en
        /// zigzag, extendiéndose como ramas de un árbol tipo fractal, las
        /// líneas no deben ser individuales»).
        ///
        /// Hasta v6.50.16 este método era un tronco + DOS FILAMENTOS
        /// PARALELOS + DOS PELOS — literalmente «líneas una detrás de
        /// otra», el reporte exacto del usuario. LA NUEVA ANATOMÍA es el
        /// ÁRBOL CONECTADO: UN tronco fractal multi-escala del que germinan
        /// ramas en sus VÉRTICES (el final de un segmento = el inicio de
        /// una rama), cada rama es a su vez fractal y germina ramitas —
        /// profundidad 2-3, ancho ×0.62 y brillo ×0.74 por nivel, la
        /// longitud proporcional a lo que queda del padre. UNA SOLA
        /// ESTRUCTURA conectada de punta a punta: cero líneas suéltas.
        /// Las ramas alternan los DOS colores del arsenal (haloA/haloB).
        /// </summary>
        public static void MultiBolt(SpriteBatch batch, Vector2 start, Vector2 end,
            int seed, int flick, float width, Color haloA, Color haloB, Color core,
            float alpha = 1f, float amp = 26f, int segments = 12)
        {
            // === EL TRONCO FRACTAL (midpoint displacement multi-escala):
            // amp ABSOLUTO (min(amp, 30%·len)) — el contrato v6.50.15. ===
            float lenMb = Vector2.Distance(start, end);
            if (lenMb < 4f) return;
            float ampPxMb = Math.Min(amp, lenMb * 0.30f);
            Vector2[] trunk = FractalPath(start, end, seed, flick,
                Math.Clamp((int)MathF.Round(MathF.Log2(MathF.Max(4, segments))), 3, 5),
                Math.Clamp(ampPxMb / Math.Max(lenMb, 1f), 0.06f, 0.26f));

            // === v6.50.17 — EL ÁRBOL COMPLETO: tronco + ramas RECURSIVAS
            // (conectadas por vértices, cada una fractal). Profundidad 2;
            // 3 para los rayos largos (el árbol se lee cuando hay tela). ===
            int prof = lenMb > 300f ? 3 : 2;
            float largoTronco = PathLength(trunk);
            List<StormStrand> arbol = BuildTree(trunk, seed, flick, prof, largoTronco);

            for (int f = 0; f < arbol.Count; f++)
            {
                StormStrand s = arbol[f];
                // El tronco al color A a brillo pleno; las ramas alternan
                // A/B (la riqueza de DOS colores sin líneas paralelas).
                Color cHalo = f == 0 ? haloA : (f % 2 == 1 ? haloB : haloA);
                StrandImpl(batch, s.Points, seed + 17 + f * 13, flick,
                    width * s.WidthScale, cHalo, core, alpha * s.Alpha,
                    f == 0 ? StormTaper.Center : StormTaper.Linear);
            }
        }

        /// <summary>
        /// v6.50.17 — EL RAYO PERLIN: la descarga generada con RUIDO (el
        /// método DISTINTO que pide el usuario para el arma nueva — el
        /// propio Terraria tiene rayos; esta es nuestra idea mejorada).
        ///
        /// LA DIFERENCIA con el fractal (midpoint displacement): aquel
        /// produce zigzag autosimilar con esquinas en TODAS las escalas
        /// («cristal roto»); EL PERLIN produce un CANAL que MEANDRA con
        /// curvatura continua — el arco eléctrico entre dos electrodos.
        /// La receta (investigación R57-a, defaults medidos de
        /// SamyBlue/Lightning-Beams + NVIDIA + drilian):
        ///
        ///   p(t) = lerp(A,B,t) + perp·a·(0.62·meandro + 0.38·jitter + 0.45·arco)·env(t)
        ///
        ///   · a = 12% de la longitud (MaxRadius), capado al 30%.
        ///   · MEANDRO: fBm 3 octavas a 2.8 ciclos/largo — el serpenteo
        ///     suave de baja frecuencia. SU FASE RESBALA 0.15/flick.
        ///   · JITTER: fBm 2 octavas a 10 ciclos (el nerviosismo fino).
        ///     Fase 0.47/flick — el rayo SERPENTEA en vez de
        ///     tele-transportarse (la firma Perlin).
        ///   · ARCO: noise casi CONGELADO (t·0.5) — la combadura lenta del
        ///     canal entre electrodos, la personalidad del arco sostenido.
        ///   · ENVOLVENTE exp(−5000·(t−0.5)^10): clava los anclajes
        ///     EXACTOS y deja el 60% central a plena amplitud.
        ///   · RAMAS: germinan en los MÁXIMOS LOCALES del meandro (los
        ///     puntos de mayor curvatura del canal — ESTABLES mientras la
        ///     fase resbala): ±20-45°, ×0.55 de lo que resta, ancho ×0.6,
        ///     brillo ×0.55, UNA ramita de segundo nivel (el árbol chico
        ///     del arco).
        /// </summary>
        public static void PerlinBolt(SpriteBatch batch, Vector2 start, Vector2 end,
            int seed, int flick, float width, Color halo, Color core,
            float alpha = 1f, float ampFrac = 0f)
        {
            float len = Vector2.Distance(start, end);
            if (len < 4f || alpha <= 0.01f || width <= 0.05f) return;

            Vector2 dir = (end - start) / len;
            Vector2 perp = new Vector2(-dir.Y, dir.X);

            // LA AMPLITUD: 12% del largo (la medida de los defaults reales).
            float a = ampFrac > 0.001f ? ampFrac * len : 0.12f * len;
            a = MathF.Min(a, len * 0.30f);

            // LAS FASES DESLIZANTES (el meandro lento + el jitter rápido).
            float faseM = flick * 0.15f;
            float faseJ = flick * 0.47f;

            // 48 puntos (el meandro necesita resolución: 32 dejaba la
            // escalera de vértices que el VLM del mock reportó) + UNA pasada
            // de Chaikin al final = la curvatura continua del canal.
            const int N = 48;
            var pts = new Vector2[N + 1];
            var mvals = new float[N + 1];       // el meandro, para las ramas
            for (int i = 0; i <= N; i++)
            {
                float t = i / (float)N;

                // EL MEANDRO (fBm 3 octavas, 2.8 ciclos) y el JITTER (fBm 2
                // octavas, 10 ciclos — el nerviosismo del canal).
                float m = (Fbm1D(t * 2.8f + faseM, seed, 3) - 0.5f) * 2f;
                float j = (Fbm1D(t * 10f + faseJ, seed + 77, 2) - 0.5f) * 2f;

                // EL ARCO LENTO (congelado por semilla: la combadura del
                // canal entre electrodos — no se anima, se sostiene).
                float arco = (Ruido1D(t * 0.5f + seed * 0.013f, seed + 55) - 0.5f) * 2f;

                // LA ENVOLVENTE: clava los anclajes, plana en el centro.
                float d = t - 0.5f;
                float d2 = d * d;
                float d5 = d2 * d2 * d;          // (t-0.5)^5
                float env = MathF.Exp(-5000f * d5 * d5);   // ^10

                float off = (m * 0.62f + j * 0.38f) * a + arco * 0.45f * a;
                pts[i] = start + dir * (len * t) + perp * (off * env);
                mvals[i] = m;
            }
            pts[0] = start;
            pts[N] = end;                        // anclajes EXACTOS

            // === EL ALISADO CHAIKIN (el meandro suave — la firma Perlin;
            //     el mock+VLM v6.50.17 lo pidió: sin él el canal lee
            //     «escalera» y no «serpenteo») ===
            Vector2[] canal = Chaikin(pts, 1);

            // === EL TRONCO (el canal que meandra) ===
            StrandImpl(batch, canal, seed, flick, width, halo, core, alpha,
                StormTaper.Center);

            // === LAS RAMAS EN LOS MÁXIMOS LOCALES DEL MEANDRO (la regla
            //     del ruido: la curvatura máxima del canal es donde la
            //     descarga salta) — hasta 3, cada una con SU ramita. Las
            //     ramas parten del CANAL ALISADO (conectadas de verdad). ===
            float maxAbs = 0.001f;
            for (int i = 1; i < N; i++)
                maxAbs = MathF.Max(maxAbs, MathF.Abs(mvals[i]));

            int ramas = 0;
            for (int i = 6; i < N - 5 && ramas < 3; i++)
            {
                bool maximo = mvals[i] > mvals[i - 1] && mvals[i] >= mvals[i + 1]
                           || mvals[i] < mvals[i - 1] && mvals[i] <= mvals[i + 1];
                if (!maximo) continue;
                if (MathF.Abs(mvals[i]) < 0.60f * maxAbs) continue;

                // La dirección de la rama: la tangente local del canal
                // girada 20-45° al lado del meandro.
                Vector2 tang = canal[Math.Min(i * 2 - 1, canal.Length - 1)]
                             - canal[Math.Max(i * 2 - 3, 0)];
                float tl = tang.Length();
                if (tl < 0.01f) continue;
                tang /= tl;
                float lado = mvals[i] >= 0f ? 1f : -1f;
                float ang = lado * (0.35f + 0.44f * VFXCore.Hash01(seed, i, 733));
                Vector2 dirR = tang.RotatedBy(ang);

                // El ORIGEN de la rama: el vértice equivalente del canal
                // alisado (i·2-2, el punto más cercano al vértice i).
                Vector2 origen = canal[Math.Clamp(i * 2 - 2, 0, canal.Length - 1)];
                float t0 = i / (float)N;
                float restante = len * (1f - t0);
                float largo = restante * (0.38f + 0.24f * VFXCore.Hash01(seed, i, 739));
                if (largo < 18f) continue;

                int gens = Math.Clamp((int)MathF.Round(MathF.Log2(MathF.Max(4f, largo / 12f))), 2, 4);
                int seedR = seed + 733 + i * 19;
                Vector2[] rama = FractalPath(origen, origen + dirR * largo,
                    seedR, flick, gens, 0.12f);

                StrandImpl(batch, rama, seedR, flick, width * 0.60f, halo, core,
                    alpha * 0.55f, StormTaper.Linear);

                // LA RAMITA (segundo nivel — el arbolito del canal).
                var hojas = new List<StormStrand>();
                Ramificar(hojas, rama, seedR, flick, 0.60f, 0.55f, 1, largo);
                for (int h = 0; h < hojas.Count; h++)
                {
                    StormStrand s = hojas[h];
                    StrandImpl(batch, s.Points, seedR + 17 + h, flick,
                        width * s.WidthScale, halo, core, alpha * s.Alpha,
                        StormTaper.Linear);
                }
                ramas++;
            }

            // LOS GORROS de descarga (los electrodos arden).
            EndCap(batch, canal[0], width, halo, core, alpha);
            EndCap(batch, canal[canal.Length - 1], width * 0.8f, halo, core, alpha);
        }

        /// <summary>
        /// v6.50.17 — EL ALISADO CHAIKIN (el meandro de verdad): corta cada
        /// esquina de la polilínea a ¼-¾ (Q = ¾·P[i]+¼·P[i+1],
        /// R = ¼·P[i]+¾·P[i+1]) — el canal Perlin gana la CURVATURA CONTINUA
        /// que lo distingue del zigzag fractal (la firma del «arco eléctrico
        /// que serpentea»). Los extremos quedan EXACTOS.
        /// </summary>
        public static Vector2[] Chaikin(Vector2[] pts, int pasadas = 1)
        {
            if (pts == null || pts.Length < 3 || pasadas < 1) return pts;
            Vector2[] actuales = pts;
            for (int p = 0; p < pasadas; p++)
            {
                int n = actuales.Length;
                // [P0, Q0,R0, Q1,R1, …, Qn-2,Rn-2, Pn-1] = 2 + 2·(n−1)
                var suaves = new Vector2[(n - 1) * 2 + 2];
                suaves[0] = actuales[0];
                suaves[suaves.Length - 1] = actuales[n - 1];
                int k = 1;
                for (int i = 0; i < n - 1; i++)
                {
                    Vector2 a = actuales[i], b = actuales[i + 1];
                    suaves[k++] = a * 0.75f + b * 0.25f;
                    suaves[k++] = a * 0.25f + b * 0.75f;
                }
                actuales = suaves;
            }
            return actuales;
        }

        /// <summary>
        /// ARCO ELÉCTRICO alrededor de un centro (las coronas de impacto):
        /// el arco clásico re-implementado con las texturas de
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
                StormTaper.Center);
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
        //  v6.33 — LA SEGUNDA GENERACIÓN MEJORADA (informe v633/INFORME_
        //  RAYOS_ELECTRICOS.md: las técnicas clásicas de generación fractal +
        //  + vanilla decompilada — las 5 técnicas con SUS números)
        // ==================================================================

        /// <summary>
        /// T1 — EL CAMINO FRACTAL DE VERDAD (midpoint displacement, la técnica
        /// canónica de midpoint-displacement): empieza con 1 segmento y
        /// cada generación PARTE cada segmento por su punto medio desplazado
        /// sobre la PERPENDICULAR con offset = len·chaos que SE DIVIDE A LA
        /// MITAD por generación (offset /= 2). 5 generaciones → 32 tramos con
        /// rugosidad MULTI-ESCALA: lazadas grandes + micro-detalle — lo que el
        /// jitter mono-escala de ZigPath no puede dar. Extremos anclados.
        /// Determinista por (seed, flick).
        /// </summary>
        public static Vector2[] FractalPath(Vector2 start, Vector2 end,
            int seed, int flick, int generations = 5, float chaos = 0.15f)
        {
            if (generations < 1) generations = 1;
            if (generations > 8) generations = 8;
            float len = Vector2.Distance(start, end);
            if (len < 2f)
                return new[] { start, end };

            // La lista de puntos (empieza con los anclajes exactos).
            var pts = new List<Vector2> { start, end };
            float offset = len * MathHelper.Clamp(chaos, 0.02f, 0.30f);

            for (int g = 0; g < generations; g++)
            {
                int count = pts.Count;
                for (int i = 0; i < count - 1; i++)
                {
                    Vector2 a = pts[i * 2];
                    Vector2 b = pts[i * 2 + 1];
                    Vector2 seg = b - a;
                    float sl = seg.Length();
                    Vector2 mid = (a + b) * 0.5f;
                    if (sl > 0.01f)
                    {
                        Vector2 normal = new Vector2(-seg.Y / sl, seg.X / sl);
                        float j = (VFXCore.Hash01(seed, flick * 31 + g, i * 61 + 13) - 0.5f) * 2f;
                        mid += normal * (j * offset);
                    }
                    // Inserta el midpoint entre a y b.
                    pts.Insert(i * 2 + 1, mid);
                }
                offset *= 0.5f;      // ← LA REGLA: cada generación, la mitad
            }
            return pts.ToArray();
        }

        /// <summary>
        /// T1+T2 — EL RAYO FRACTAL CON RAMAS DE SUBDIVISIÓN: FractalPath +
        /// forks que nacen AL PARTIR cada segmento (la escuela clásica): prob
        /// 0.25-0.35 por segmento en las generaciones 2+, ángulo 20°-50° del
        /// tronco, longitud ×0.6-0.7, ancho ×0.5 y alpha ×0.4 (SOLO el tronco
        /// va a brillo completo). Máx 4 ramas (presupuesto de quads).
        /// Dibuja todo con la receta de pintado de la casa.
        /// </summary>
        public static void FractalBolt(SpriteBatch batch, Vector2 start, Vector2 end,
            int seed, int flick, float width, Color halo, Color core,
            float alpha = 1f, int generations = 5, float chaos = 0.15f,
            float forkChance = 0.28f)
        {
            Vector2[] trunk = FractalPath(start, end, seed, flick, generations, chaos);
            if (trunk.Length < 2) return;

            // === EL TRONCO a brillo completo ===
            StrandImpl(batch, trunk, seed, flick, width, halo, core, alpha,
                StormTaper.Center);

            // === LAS RAMAS DE SUBDIVISIÓN (nacieron al partir) ===
            int forks = 0;
            for (int g = 1; g < generations && forks < 4; g++)
            {
                int segsAtG = 1 << g;                     // segmentos en esta generación
                // El midpoint del segmento i de la gen g acaba en el índice
                // (2i+1)·2^(G-g-1) del array final (verificado a mano).
                int step = 1 << (generations - g - 1);
                for (int i = 0; i < segsAtG && forks < 4; i++)
                {
                    int idx = (2 * i + 1) * step;
                    if (idx <= 0 || idx >= trunk.Length - 1) continue;
                    if (VFXCore.Hash01(seed, flick * 7 + g, idx * 97 + 41) > forkChance)
                        continue;

                    Vector2 origin = trunk[idx];
                    Vector2 dir = trunk[idx + 1] - trunk[idx - 1];
                    float dl = dir.Length();
                    if (dl < 0.01f) continue;
                    dir /= dl;

                    float side = VFXCore.Hash01(seed, g * 13, idx * 7 + 5) > 0.5f ? 1f : -1f;
                    float ang = side * (0.35f + 0.52f * VFXCore.Hash01(seed, g, idx * 11));
                    Vector2 fdir = dir.RotatedBy(ang);
                    // la regla clásica: el fork mide ~×0.65 del segmento que lo parió.
                    float flen = Math.Min(dl * 0.65f,
                        Vector2.Distance(start, end) * 0.22f);

                    Vector2[] branch = FractalPath(origin, origin + fdir * flen,
                        seed + 313 + g * 17 + idx, flick,
                        Math.Max(2, generations - 2), 0.12f);
                    StrandImpl(batch, branch, seed + 313 + g * 17 + idx, flick,
                        width * 0.5f, halo, core, alpha * 0.4f,
                        StormTaper.Linear);
                    forks++;
                }
            }
        }

        /// <summary>
        /// T3+T4 — EL ARCO DE CORRIENTE CONTINUA (el estándar de los grandes, no el
        /// blink binario): DOS rayos fractales ENTRELAZADOS que se RELEVAN —
        /// cada uno se regenera cada 10 ticks (6 Hz) con vida de 20 ticks y
        /// se desvanece 100%→50% mientras el otro nace a full: SIEMPRE hay
        /// un rayo visible, la corriente no se corta. Pintado con LA RECETA
        /// ELÉCTRICA de 3 pasadas (glow #1E50A8 ×0.30 esc ×1.0 · mid #5EB3FF
        /// ×0.55 esc ×0.5 · core BLANCO ×0.90 esc ×0.22 (la receta de 3 capas
        /// 0.6/0.4/0.2) y BOIL doble en los midpoints (±6 px, "hierve más
        /// donde se partió"). Ideal para el interior de las heridas y los
        /// arcos sostenidos entre máquinas.
        /// </summary>
        public static void StormArc(SpriteBatch batch, Vector2 start, Vector2 end,
            int seed, float time, float width, Color? tint = null,
            float alpha = 1f, float len = 0f)
        {
            Color t = tint ?? Color.White;
            Color glow = Tint(new Color(30, 80, 168).MultiplyRGB(t), 255);    // #1E50A8
            Color mid = new Color(94, 179, 255).MultiplyRGB(t);               // #5EB3FF
            Color coreC = new Color(255, 255, 255);                            // #FFFFFF
            if (len <= 0f) len = Vector2.Distance(start, end);
            if (len < 4f) return;

            // La fase de relevos: 6 Hz, cada bolt vive 20 ticks.
            float phase = time * 6f;
            int slotA = (int)phase;
            int slotB = (int)(phase + 1f);
            float ageA = phase - slotA;                    // 0..1 edad de A
            float ageB = phase + 1f - slotB;               // 0..1 edad de B

            // A nació hace ageA·(1/6s): los primeros 10 ticks a full, luego →50%.
            float alphaA = alpha * (ageA < 0.5f ? 1f : MathHelper.Lerp(1f, 0.5f, (ageA - 0.5f) * 2f));
            float alphaB = alpha * (ageB < 0.5f ? 1f : MathHelper.Lerp(1f, 0.5f, (ageB - 0.5f) * 2f));

            PintaArco(batch, start, end, seed, slotA, width, glow, mid, coreC, alphaA);
            PintaArco(batch, start, end, seed + 977, slotB, width, glow, mid, coreC, alphaB);
        }

        /// <summary>Una pasada del arco — v6.50.22 — LA PILA DE PASADAS
        /// (el filamento 100% código): el pincel es EL PIXEL del motor y
        /// el perfil transversal ES LA SUMA de 6 pasadas + vena (la
        /// receta del LightningArc 466 de vanilla, extendida) — las
        /// pasadas 0-3 con el color GLOW (la funda azul oscura), las 4-5
        /// con el MID (#5EB3FF, el cuerpo) y la vena BLANCA. El v6.39
        /// estiraba BANDAS horneadas (con suelo de alfa en los bordes —
        /// el look "líneas"); el v6.33 antes estiraba SOFTGLOW (RADIAL:
        /// franjas oscuras + apiles claros). Muertos ambos.</summary>
        private static void PintaArco(SpriteBatch batch, Vector2 start, Vector2 end,
            int seed, int slot, float width, Color glow, Color mid, Color coreC, float alpha)
        {
            if (alpha <= 0.02f) return;
            Vector2[] pts = FractalPath(start, end, seed, slot, 5, 0.15f);
            // EL BOIL DOBLE EN LOS MIDPOINTS (±3 px normal, ±6 en las potencias de 2).
            for (int i = 1; i < pts.Length - 1; i++)
            {
                bool esMid = (i & (i - 1)) == 0;           // índice potencia de 2
                float amp = esMid ? 6f : 3f;
                pts[i] += new Vector2(
                    (VFXCore.Hash01(seed, slot, i * 19 + 7) - 0.5f) * 2f * amp,
                    (VFXCore.Hash01(seed, slot + 31, i * 23 + 3) - 0.5f) * 2f * amp);
            }

            float total = PathLength(pts);
            if (total < 1f) return;
            float arc = 0f;
            for (int i = 0; i < pts.Length - 1; i++)
            {
                Vector2 seg = pts[i + 1] - pts[i];
                float sl = seg.Length();
                if (sl < 0.30f) { arc += sl; continue; }
                // NORMAL MEDIA (la lección del ribbon): la perpendicular al promedio de las
                // direcciones adyacentes — mata los puntos brillantes de las juntas.
                Vector2 prev = i > 0 ? pts[i] - pts[i - 1] : seg;
                Vector2 next = i < pts.Length - 2 ? pts[i + 2] - pts[i + 1] : seg;
                Vector2 avg = Vector2.Normalize(prev) + Vector2.Normalize(next);
                if (!(avg.LengthSquared() >= 0.001f)) avg = seg; // v6.50.10 — también atrapa NaN (Normalize de un vector cero: "NaN < 0.001" es false y esquivaba el guard)
                avg = Vector2.Normalize(avg);
                float rot = (float)Math.Atan2(avg.Y, avg.X);
                Vector2 pos = (pts[i] + pts[i + 1]) * 0.5f;
                float tMid = (arc + sl * 0.5f) / total;
                float w = width * MathHelper.Clamp((float)Math.Sin(tMid * Math.PI) + 0.35f, 0.3f, 1f);

                // v6.39 — EL LARGO EXACTO + LA EXTENSIÓN DE GIRO (borde a
                // borde — nada de solapes que el aditivo apila).
                float largo = Vector2.Dot(seg, avg);
                if (largo < 0.30f) { arc += sl; continue; }

                // v6.50.22 — LA PILA DE PASADAS (100% código: la suma de
                // quads SÓLIDOS del pixel del motor ES el degradado).
                PilaFilamento(batch, pos, rot, largo,
                    AnguloEntre(prev, seg), AnguloEntre(seg, next), w,
                    glow, mid, coreC, alpha, 1f);
                arc += sl;
            }
        }

        // ==================================================================
        //  v6.34 — EL ARCO PERSEGUIDOR (SeekArc): el camino se CURVA al objetivo
        // ==================================================================

        /// <summary>
        /// v6.34 — EL ARCO PERSEGUIDOR: un arco eléctrico de
        /// <paramref name="from"/> a <paramref name="to"/> cuyo CAMINO ENTERO
        /// se CURVA hacia <paramref name="target"/> — el rayo que "busca" a su
        /// víctima sin desanclarse de sus extremos. La receta, en tres pasos:
        /// 1) el CAMINO FRACTAL normal de A a B (FractalPath, la misma
        /// generación de StormArc); 2) EL SESGO — cada punto interior se
        /// desplaza hacia el objetivo con peso senoidal
        /// (sin(t01·π)·fuerza: 0 en los anclajes, MÁXIMO en el medio — el
        /// vientre del arco se abomba hacia target, los extremos quedan
        /// EXACTOS); 3) el PINTADO DE 3 CAPAS de la casa (glow ×1.0 · mid
        /// ×0.5 · core ×0.22 — ver StormArc) con NORMAL MEDIA por tramo.
        /// A diferencia del resto de StormLib (que dibuja al batch abierto),
        /// este EMITE AL BUFFER DE VFXCORE (Quad con rotación → el llamador
        /// vuelca con FlushAdditive, que pinta con la MISMA textura SoftGlow
        /// de las 3 capas): encaja con los efectos que ya componen cuadros
        /// de luz y no abre ni cierra NINGÚN batch.
        /// </summary>
        /// <param name="from">Anclaje A (coords de mundo).</param>
        /// <param name="to">Anclaje B (coords de mundo).</param>
        /// <param name="target">El objetivo al que el camino se curva (el arco se abomba hacia él; NUNCA lo alcanza).</param>
        /// <param name="fuerza">Cuánto se abomba el camino (≈0.25 recomendado: en el vientre, cada punto recorre esa fracción de su distancia al objetivo).</param>
        /// <param name="cBase">Color de la capa glow (la receta: #1E50A8).</param>
        /// <param name="cMedia">Color de la capa media (la receta: #5EB3FF).</param>
        /// <param name="cNucleo">Color del núcleo (la receta: #FFFFFF).</param>
        /// <param name="alpha">Multiplicador global de intensidad.</param>
        /// <param name="seed">Semilla determinista del fractal.</param>
        /// <param name="time">Tiempo animado (regenera la forma a 15 Hz con FlickTick).</param>
        /// <param name="width">Ancho del cuerpo del arco en px (3.5 ≈ el de los arcos internos de la casa).</param>
        public static void SeekArc(Vector2 from, Vector2 to, Vector2 target, float fuerza,
            Color cBase, Color cMedia, Color cNucleo, float alpha,
            int seed = 0, float time = 0f, float width = 3.5f)
        {
            float len = Vector2.Distance(from, to);
            if (len < 4f) return;

            // === 1. EL CAMINO FRACTAL NORMAL (la misma receta de StormArc) ===
            int flick = FlickTick(time);
            Vector2[] pts = FractalPath(from, to, seed, flick, 5, 0.15f);

            // === 2. EL SESGO HACIA EL OBJETIVO: peso senoidal — 0 en los
            // anclajes (quedan EXACTOS), máximo al medio (el vientre busca).
            // Lerp(punto→target, peso): con fuerza 0.25, el vientre recorre
            // el 25% de su distancia al objetivo. ===
            for (int i = 1; i < pts.Length - 1; i++)
            {
                float t01 = i / (float)(pts.Length - 1);
                float peso = (float)Math.Sin(t01 * Math.PI) * fuerza;
                pts[i] = Vector2.Lerp(pts[i], target, peso);
            }

            // === 3. EL PINTADO DE 3 CAPAS — el mismo motor de PintaArco
            // (normal media + largo exacto + extensión de giro), pero
            // EMITIDO al buffer de VFXCore. v6.39: los quads llevan la
            // TEXTURA DE BANDA propia (BoltHalo/BoltCore — la banda
            // premultiplicada que el aditivo respeta): antes se volcaban
            // SIN textura y FlushAdditive los pintaba con SoftGlow (RADIAL:
            // funde a lo largo) → el brillo se cortaba por secciones. ===
            float total = PathLength(pts);
            if (total < 1f) return;
            float arc = 0f;
            for (int i = 0; i < pts.Length - 1; i++)
            {
                Vector2 seg = pts[i + 1] - pts[i];
                float sl = seg.Length();
                if (sl < 0.30f) { arc += sl; continue; }
                // NORMAL MEDIA (la lección del ribbon): mata los puntos
                // brillantes de las juntas.
                Vector2 prev = i > 0 ? pts[i] - pts[i - 1] : seg;
                Vector2 next = i < pts.Length - 2 ? pts[i + 2] - pts[i + 1] : seg;
                Vector2 avg = Vector2.Normalize(prev) + Vector2.Normalize(next);
                if (!(avg.LengthSquared() >= 0.001f)) avg = seg; // v6.50.10 — también atrapa NaN (Normalize de un vector cero: "NaN < 0.001" es false y esquivaba el guard)
                avg = Vector2.Normalize(avg);
                float rot = (float)Math.Atan2(avg.Y, avg.X);
                Vector2 pos = (pts[i] + pts[i + 1]) * 0.5f;
                float tMid = (arc + sl * 0.5f) / total;
                float w = width * MathHelper.Clamp((float)Math.Sin(tMid * Math.PI) + 0.35f, 0.3f, 1f);

                // v6.39 — EL LARGO EXACTO + LA EXTENSIÓN DE GIRO.
                float largo = Vector2.Dot(seg, avg);
                if (largo < 0.30f) { arc += sl; continue; }

                // v6.50.22 — LA PILA DE PASADAS emitida al buffer de VFXCore
                // (100% código: los quads llevan EL PIXEL del motor — la
                // suma de las pasadas ES el degradado, sin banda horneada).
                for (int k = 0; k < PilaW.Length; k++)
                {
                    float wk = w * PilaW[k];
                    float ext = ExtensionJunta(wk, AnguloEntre(prev, seg))
                              + ExtensionJunta(wk, AnguloEntre(seg, next));
                    Color c = k >= 4 ? cMedia : cBase;
                    VFXCore.Quad(pos, Tint(c, PilaF[k] * alpha),
                        new Vector2(largo + ext, wk), rot, VFXCore.Pixel);
                }
                float wv = Math.Max(w * 0.34f, 1.5f);
                float extV = ExtensionJunta(wv, AnguloEntre(prev, seg))
                           + ExtensionJunta(wv, AnguloEntre(seg, next));
                VFXCore.Quad(pos, Tint(cNucleo, alpha),
                    new Vector2(largo + extV, wv), rot, VFXCore.Pixel);
                arc += sl;
            }
        }

        /// <summary>
        /// T5 — LA RÁFAGA DE CHISPAS DE IMPACTO (la receta ThunderBoltVFX de
        /// clásica): N chispas con STRETCH (0.5, 1.6), SHAKE que DECAE
        /// lineal a 0 (Vector2.One.RotatedByRandom(2π)·(1−t)·power), SQUISH
        /// que las ADELGAZA hasta un hilo antes de morir, DOBLE PASADA (glow
        /// color×0.6 + core lerp(White→color)), 3 VARIANTES de forma por
        /// hash y flip cada medio segundo. Determinista por (seed, time).
        /// </summary>
        public static void SparkBurst(SpriteBatch batch, Vector2 pos, Color color,
            float size, int count, int seed, float t, float power = 14f)
        {
            if (t < 0f || t > 1f || count < 1) return;
            float fade = t > 0.5f ? 1f - 0.95f * ((t - 0.5f) / 0.5f) : 1f;
            float shake = (1f - t) * power;
            bool flip = (int)(Main.GlobalTimeWrappedHourly * 2f) % 2 == 0;

            for (int i = 0; i < count; i++)
            {
                float h1 = VFXCore.Hash01(seed, i, 31);
                float h2 = VFXCore.Hash01(seed, i, 67);
                float h3 = VFXCore.Hash01(seed, i, 97);
                float ang = h1 * MathHelper.TwoPi;
                float dist = (6f + 34f * h2) * (0.4f + 0.6f * t);
                Vector2 dir = new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang));
                Vector2 p = pos + dir * dist
                    + new Vector2(
                        (VFXCore.Hash01(seed, (int)(t * 60f), i * 13) - 0.5f) * 2f * shake,
                        (VFXCore.Hash01(seed, (int)(t * 60f) + 7, i * 17) - 0.5f) * 2f * shake);

                // Variante de forma (3) + el flip de medio segundo.
                float variant = h3;
                float lenS = 0.5f + 1.1f * variant;
                float widS = 1.6f - 0.9f * variant;
                if (flip) widS *= 0.7f;
                widS *= fade;                              // ← EL SQUISH

                Color glow = Tint(color, 0.60f * fade * (1f - t * 0.3f));
                Color core = Color.Lerp(Color.White, color, t);
                Quad(batch, GlowTex, p, new Vector2(size * lenS, size * widS), ang, glow);
                Quad(batch, GlowTex, p, new Vector2(size * lenS * 0.55f, size * widS * 0.55f),
                    ang, Tint(core, 0.85f * fade));
            }

            // El NÚCLEO del estallido (doble pasada, se apaga con t).
            Quad(batch, GlowTex, pos, new Vector2(size * 2.2f, size * 2.2f) * (1f - t * 0.5f),
                0f, Tint(color, 0.5f * fade));
            Quad(batch, GlowTex, pos, new Vector2(size * 1.0f, size * 1.0f) * (1f - t * 0.5f),
                0f, Tint(Color.Lerp(Color.White, color, t), 0.9f * fade));
        }

        // ==================================================================
        //  PRIMITIVAS INTERNAS
        // ==================================================================

        /// <summary>
        /// EL MOTOR DEL FILAMENTO (v6.50.22 — 100% CÓDIGO: el pincel es EL
        /// PIXEL 1×1 del motor, cero textura de banda): recorre la
        /// polilínea SEGMENTO A SEGMENTO pintando LA PILA de pasadas
        /// sólidas que SUMAN el perfil transversal — la receta del
        /// LightningArc 466 de vanilla extendida a 6+1 (ver PilaW/PilaF).
        ///
        /// Las juntas: cada segmento se corta PERPENDICULAR a la NORMAL
        /// MEDIA del vértice compartido — los dos quads adyacentes usan
        /// la MISMA línea de corte (colineales por construcción) → la
        /// regla de relleno top-left del rasterizador TESLA SIN HUECO NI
        /// SOLAPE en las rectas, y la EXTENSIÓN DE GIRO por pasada cubre
        /// la cuña exterior de las esquinas (solape mínimo calculado,
        /// no un suelo grande). El crackle interpolado por vértices
        /// (v6.39) sigue modulando las pasadas de color; la VENA arde
        /// SIEMPRE.
        /// </summary>
        private static void StrandImpl(SpriteBatch batch, Vector2[] pts, int seed, int flick,
            float width, Color halo, Color core, float alpha, StormTaper taper)
        {
            if (pts == null || pts.Length < 2 || alpha <= 0.01f || width <= 0.05f) return;

            float total = PathLength(pts);
            if (total < 1f) return;

            // EL CRACKLE INTERPOLADO POR CONSTRUCCIÓN (v6.39): el brillo de
            // un segmento es la media de los brillos de SUS DOS VÉRTICES
            // (cada uno 0.66..1.0 por hash de baja frecuencia) — continuo de
            // punta a punta, cero secciones duras, cero búfer.
            // La VENA no lleva crackle (el núcleo caliente arde SIEMPRE).
            float Brillo(int i)
                => 0.66f + 0.34f * VFXCore.Hash01(seed, flick, i * 41 + 17);

            float arc = 0f;
            for (int i = 0; i < pts.Length - 1; i++)
            {
                Vector2 a = pts[i];
                Vector2 b = pts[i + 1];
                Vector2 seg = b - a;
                float len = seg.Length();
                if (len < 0.35f) { arc += len; continue; }

                // === LA NORMAL MEDIA (la lección del ribbon) ===
                Vector2 prev = i > 0 ? a - pts[i - 1] : seg;
                Vector2 next = i < pts.Length - 2 ? pts[i + 2] - b : seg;
                Vector2 avg = Vector2.Normalize(prev) + Vector2.Normalize(next);
                if (!(avg.LengthSquared() >= 0.001f)) avg = seg; // v6.50.10 — también atrapa NaN (Normalize de un vector cero: "NaN < 0.001" es false y esquivaba el guard)
                avg = Vector2.Normalize(avg);

                float rot = (float)Math.Atan2(avg.Y, avg.X);
                Vector2 mid = (a + b) * 0.5f;
                float tMid = (arc + len * 0.5f) / total;

                // El TAPER y el CRACKLE (interpolado entre los vértices).
                float w = width * TaperFactor(taper, tMid);
                float crackle = (Brillo(i) + Brillo(i + 1)) * 0.5f;

                // === v6.50.22 — LA PILA DE PASADAS (el filamento 100%
                //     código: quads SÓLIDOS del pixel del motor cuya SUMA
                //     es el degradado transversal — el mismo truco del
                //     LightningArc 466 de vanilla, con 6+1 pasadas). ===
                float largo = Vector2.Dot(seg, avg);
                if (largo < 0.35f) { arc += len; continue; }
                float dPrev = AnguloEntre(prev, seg);
                float dNext = AnguloEntre(seg, next);

                PilaFilamento(batch, mid, rot, largo, dPrev, dNext, w,
                    halo, halo, core, alpha, crackle);

                arc += len;
            }

            // Los GORROS de descarga en ambos extremos.
            EndCap(batch, pts[0], width, halo, core, alpha);
            EndCap(batch, pts[pts.Length - 1], width, halo, core, alpha);
        }

        /// <summary>
        /// v6.50.22 — LA PILA: 6 pasadas SÓLIDAS de ancho/brillo telescópicos
        /// (la suma ES la gaussiana transversal — así se construye un
        /// degradado SIN textura, apilando rectángulos del pixel del motor)
        /// + LA VENA BLANCA al 34% del ancho (mínimo 1.5 px — con el pincel
        /// sólido NO hay fusión bilineal que la coma: el núcleo es un
        /// rectángulo CRISPO de color·f exacto). Las pasadas 0-3 usan el
        /// color HALO (la funda), las 4-5 el color MEDIA (el cuerpo — para
        /// los arcos de 3 colores de PintaArco/SeekArc; la familia Strand
        /// pasa halo dos veces). La VENA arde SIEMPRE (sin crackle).
        /// Cada pasada lleva SU extensión de giro (geometría por ancho —
        /// la lección v6.50.15).
        /// </summary>
        private static void PilaFilamento(SpriteBatch batch, Vector2 mid, float rot,
            float largo, float dPrev, float dNext, float w,
            Color halo, Color media, Color core, float alpha, float crackle)
        {
            for (int k = 0; k < PilaW.Length; k++)
            {
                float wk = w * PilaW[k];
                float ext = ExtensionJunta(wk, dPrev) + ExtensionJunta(wk, dNext);
                Color c = k >= 4 ? media : halo;
                Quad(batch, VFXCore.Pixel, mid, new Vector2(largo + ext, wk), rot,
                    Tint(c, PilaF[k] * alpha * crackle));
            }

            // LA VENA — la línea BLANCA razor-fina (núcleo del canal).
            float wv = Math.Max(w * 0.34f, 1.5f);
            float extV = ExtensionJunta(wv, dPrev) + ExtensionJunta(wv, dNext);
            Quad(batch, VFXCore.Pixel, mid, new Vector2(largo + extV, wv), rot,
                Tint(core, alpha));
        }

        /// <summary>El ángulo (0..π) entre dos direcciones (el GIRO de la junta).</summary>
        private static float AnguloEntre(Vector2 d0, Vector2 d1)
        {
            if (d0.LengthSquared() < 0.0001f || d1.LengthSquared() < 0.0001f) return 0f;
            float dot = MathHelper.Clamp(Vector2.Dot(Vector2.Normalize(d0), Vector2.Normalize(d1)), -1f, 1f);
            return MathF.Acos(dot);
        }

        /// <summary>
        /// LA EXTENSIÓN DE JUNTA (la geometría del round-join, medida de
        /// RiftLib v6.31): dos bandas de ancho w que giran δ necesitan
        /// e = w/2·tan(δ/2) para que sus esquinas se crucen. v6.50.22 — el
        /// SUELO 1.2 px de las bandas horneadas MUERE: era el margen del
        /// fundido de 3 px de la textura; con el PIXEL SÓLIDO las rectas
        /// teselan EXACTO (líneas de corte colineales + regla top-left) y
        /// el suelo viejo solo APILABA cuentas aditivas en las venas
        /// finas. Suelo 0.35 px (el seguro contra quirkies de redondeo del
        /// driver — invisible al ojo, mata el hueco de 1-ulp), techo w/2
        /// (los giros de 90°+ el solape clásico).
        /// </summary>
        private static float ExtensionJunta(float w, float giro)
            => MathHelper.Clamp(w * 0.5f * MathF.Tan(giro * 0.5f), 0.35f, w * 0.5f);

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

        /// <summary>Tinte de INTENSIDAD LINEAL PARA EL LOTE ADITIVO (v6.50.9 —
        /// LA ECUACIÓN REAL, cerrada de una vez con la sonda de hoy contra el
        /// FNA.dll 23.10.0.0 del tML 2026.07.3.0): el lote aditivo real es
        /// Additive=(SourceAlpha, One) — NO (One, One) como decía la teoría
        /// v6.39 — y el loader PREMULTIPLICA los PNG (ReLogic
        /// PngReader.PreMultiplyAlpha). Con el modulate del SpriteEffect, el
        /// aporte de cada quad en el lote aditivo es:
        ///
        ///   aporte = textura.rgb · tinte.rgb · (textura.a · tinte.a)
        ///          = P.rgb·P.a · color·f · (P.a · f)
        ///          = P.rgb · P.a² · color · f²        ← con el premult (RGB·f, A·f)
        ///
        /// EL f² ERA EL ASESINO DEL HALO: la capa 1 (0.30) moría a
        /// 0.055·color — INVISIBLE sobre el mundo — y el rayo entero quedaba
        /// reducido a la VENA de 3 px con una banda tenue: "una línea
        /// delgada, no son rayos de verdad" (el reporte del usuario, exacto).
        /// El tinte v6.50.3 LINEAL (RGB intacto, A=f) daba la intensidad
        /// correcta AQUÍ pero rompía los lotes de masa; el premult v6.50.7
        /// arreglaba los lotes de masa y mataba el brillo de TODOS los rayos
        /// del mod (la revertida global castigó a esta librería por un crimen
        /// que no cometió). ESTE es el tinte correcto para AMBOS mundos:
        /// (RGB·√f, A·√f) — intensidad f LINEAL en el aditivo
        /// (P.rgb·P.a²·color·f) y compositing premult CORRECTO en cualquier
        /// lote alfa (a intensidad √f — nunca fantasma, nunca caja). La
        /// auditoría v6.50.9 verificó las 30 rutas de dibujo de esta
        /// librería: TODAS aditivas, 0 violaciones.
        ///
        /// PÚBLICA v6.50.22: BoltRenderer y cualquier emisor al buffer
        /// aditivo de VFXCore necesitan el MISMO tinte lineal — el
        /// `Color * f` de XNA escala RGB y A a la vez → aporte c·f²
        /// (el hallo v6.50.9 otra vez).</summary>
        public static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            // v6.50.9 — LA RAÍZ CUADRADA DE LA INTENSIDAD (ver doc del
            // método): el f² del premult clásico mataba al halo (0.30 → 0.055)
            // y dejaba el rayo en una línea delgada; con (RGB·√f, A·√f) la
            // intensidad aditiva sale f LINEAL y el compositing alfa sigue
            // correcto. Verificado con mock 1:1 + VLM: halo/cuerpo/vena
            // visibles de verdad, "real, powerful lightning strike" (9-10/10
            // frente al 4-6/10 del premult).
            float s = (float)Math.Sqrt(f);
            return new Color(
                (byte)(int)(c.R * s), (byte)(int)(c.G * s), (byte)(int)(c.B * s),
                (byte)(int)(255f * s));
        }
    }
}
