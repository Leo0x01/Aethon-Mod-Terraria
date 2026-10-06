using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// VFXCore — v6.03 — EL NÚCLEO DE LA BIBLIOTECA VISUAL AETHON.
    ///
    /// Idea central: todos los efectos de brillo del mod (coronas, discos,
    /// rayos, halos) se describen como LISTAS DE CUADROS DE LUZ (GlowQuad:
    /// posición en MUNDO, color, escala, rotación) y luego se "vuelcan" al
    /// destino que haga falta con UNA sola llamada:
    ///
    ///   - FlushAdditive(...): dibujo directo aditivo en el mundo
    ///     (proyectiles, efectos de pantalla) — el brillo SUMA, look de neón.
    ///   - AppendToPlayerDraw(...): emisión de DrawData para las capas de
    ///     dibujado del jugador (PlayerDrawLayer) — el camino 100% seguro
    ///     con el pipeline de tML, sin tocar el SpriteBatch del renderer.
    ///
    /// Así un mismo renderizador (p. ej. la corona de arcos) sirve igual en
    /// un proyectil que sobre la cabeza de un jugador, sin duplicar la
    /// matemática y sin riesgo de romper estados de dibujo ajenos.
    ///
    /// El buffer de cuadros es estático y reutilizable: la generación de un
    /// efecto típico no aloca nada (cero GC por frame).
    /// </summary>
    public static class VFXCore
    {
        // ------------------------------------------------------------------
        //  EL CUADRO DE LUZ
        // ------------------------------------------------------------------

        /// <summary>
        /// Un "punto de luz" del tamaño y forma que haga falta: posición en
        /// COORDENADAS DE MUNDO, color (con alfa), escala (x≠y = estirado),
        /// rotación y textura propia (null = SoftGlow). En modo Inmediato el
        /// SpriteBatch envía cada Draw al momento: mezclar texturas en un
        /// mismo volcado es gratis.
        /// v6.50.67 — Source: el rectángulo de la textura a muestrear (null
        /// = textura entera, el comportamiento clásico). LA LLAVE DE LAS
        /// CINTAS: una misma textura larga (la carne del tentáculo) se
        /// muestrea POR BANDAS para que la textura FLUYA continua a lo
        /// largo del cuerpo.
        /// </summary>
        public struct GlowQuad
        {
            public Vector2 Position;
            public Color Color;
            public Vector2 Scale;
            public float Rotation;
            public Texture2D Texture;
            /// <summary>v6.50.67 — Rectángulo fuente (null = textura entera).</summary>
            public Rectangle? Source;
        }

        /// <summary>Buffer reutilizable de cuadros (evita GC por frame).</summary>
        private static readonly List<GlowQuad> _quads = new List<GlowQuad>(512);

        /// <summary>Prepara el buffer para recibir un efecto nuevo.</summary>
        public static void Begin()
        {
            _quads.Clear();
        }

        /// <summary>Añade un cuadro de luz (posición en coords de mundo).</summary>
        public static void Quad(Vector2 position, Color color, Vector2 scale, float rotation = 0f)
        {
            _quads.Add(new GlowQuad { Position = position, Color = color, Scale = scale, Rotation = rotation, Texture = null, Source = null });
        }

        /// <summary>v6.50.67 — Añade un cuadro con RECTÁNGULO FUENTE (para
        /// muestrear BANDAS de una textura larga — las cintas de carne que
        /// FLUYEN). La escala sigue siendo px FINALES en pantalla; el origen
        /// del muestreo es el centro del rectángulo fuente.</summary>
        public static void QuadSrc(Vector2 position, Color color, Vector2 scale, float rotation, Texture2D texture, Rectangle source)
        {
            if (texture == null || source.Width <= 0 || source.Height <= 0) return;
            _quads.Add(new GlowQuad { Position = position, Color = color, Scale = scale, Rotation = rotation, Texture = texture, Source = source });
        }

        /// <summary>Añade un cuadro con TEXTURA propia (Ring, GlowOrb...).</summary>
        public static void Quad(Vector2 position, Color color, Vector2 scale, Texture2D texture)
        {
            _quads.Add(new GlowQuad { Position = position, Color = color, Scale = scale, Rotation = 0f, Texture = texture });
        }

        /// <summary>v6.08 — Cuadro con TEXTURA propia Y ROTACIÓN: cintas de
        /// luz orientadas por la tangente (alas de mariposa, colas de
        /// cometa, rastros de acreción...).</summary>
        public static void Quad(Vector2 position, Color color, Vector2 scale, float rotation, Texture2D texture)
        {
            _quads.Add(new GlowQuad { Position = position, Color = color, Scale = scale, Rotation = rotation, Texture = texture });
        }

        /// <summary>Añade un cuadro circular (atajo: escala uniforme).</summary>
        public static void Quad(Vector2 position, Color color, float scale)
        {
            _quads.Add(new GlowQuad { Position = position, Color = color, Scale = new Vector2(scale, scale), Rotation = 0f, Texture = null });
        }

        /// <summary>
        /// v6.41 — EL CUADRO ESTIRADO DE A A B (la línea de la casa): un
        /// quad de <paramref name="grosor"/> px de ancho cubriendo TODO el
        /// segmento A→B (posición = punto medio, rotación = ángulo del
        /// segmento, escala X = longitud). EL primitivo de los rayos, las
        /// líneas de telegraph y los beams — antes cada arma lo recomponía
        /// a mano con senos y cosenos.
        /// </summary>
        public static void Line(Vector2 a, Vector2 b, Color color, float grosor)
        {
            Vector2 delta = b - a;
            float len = delta.Length();
            if (len < 0.5f || grosor <= 0f || color.A == 0) return;

            _quads.Add(new GlowQuad
            {
                Position = a + delta * 0.5f,
                Color = color,
                Scale = new Vector2(len, grosor),
                Rotation = delta.ToRotation(),
                Texture = null,
            });
        }

        /// <summary>Cuántos cuadros lleva el buffer (diagnóstico).</summary>
        public static int QuadCount => _quads.Count;

        // ==================================================================
        //  v6.50.67 — LA CINTA (EL RIBBON): el cuerpo orgánico de verdad
        // ==================================================================

        /// <summary>
        /// v6.50.67 — LA CINTA: convierte una polilínea (la espina) + un
        /// perfil de anchos en una SERIE DE QUADS TANGENTES que se
        /// SOLAPAN en las juntas — el cuerpo continuo, curvo y con
        /// MÚSCULO que las líneas rectas de la v6.50.66 jamás dieron.
        /// Cada segmento es un quad rotado a la tangente, estirado medio
        /// ancho por cada extremo (la extensión TAPA el hueco exterior de
        /// las curvas y se funde en el interior), muestreando su BANDA de
        /// la textura larga para que la carne FLUYA continua raíz→punta.
        /// </summary>
        /// <param name="spine">La espina (raíz→punta, coords de MUNDO).</param>
        /// <param name="widths">Ancho px por PUNTO de la espina.</param>
        /// <param name="color">Tinte (con alfa) de TODA la cinta.</param>
        /// <param name="texture">La textura larga (Carne, Colmillo…).</param>
        /// <param name="uvFlow">0..1 — el desplazamiento del flujo (tiempo·velocidad): la carne AVANZA raíz→punta.</param>
        /// <param name="edgeInset">0..0.5 — cuánto se ENCOGE la cinta por lado (para la cinta de BORDE: pasar ~0.42 dibuja una franja fina pegada al borde).</param>
        /// <param name="edgeOutset">0..1 — cuánto se ENSANCHA por lado (el halo que respira: ~0.5 = ×2 de ancho).</param>
        public static void Ribbon(Vector2[] spine, float[] widths, Color color, Texture2D texture,
            float uvFlow = 0f, float edgeInset = 0f, float edgeOutset = 0f)
        {
            if (spine == null || spine.Length < 2 || texture == null || texture.IsDisposed) return;
            if (color.A == 0) return;
            int n = spine.Length;
            int bandas = Math.Min(texture.Height, 64);          // cuántas bandas-V tiene la textura lógica

            for (int i = 1; i < n; i++)
        {
                Vector2 a = spine[i - 1], b = spine[i];
                Vector2 delta = b - a;
                float len = delta.Length();
                if (len < 0.1f) continue;

                float wIzq = i - 1 < widths.Length ? widths[i - 1] : widths[widths.Length - 1];
                float wDer = i < widths.Length ? widths[i] : widths[widths.Length - 1];
                float w = (wIzq + wDer) * 0.5f * (1f + edgeOutset * 2f) * (1f - edgeInset * 2f);
                if (w < 1.5f) continue;

                // LA EXTENSIÓN: medio ancho por extremo a lo largo de la
                // tangente — las curvas quedan TAPADAS (hueco exterior) y
                // fundidas (solape interior). Acotado para no inflar.
                float ext = MathHelper.Clamp(w * 0.55f, 5f, 30f);
                Vector2 tang = delta * (1f / len);
                Vector2 centro = (a + b) * 0.5f;

                // LA BANDA de la textura: el flujo AVANZA raíz→punta con
                // uvFlow; el propio segmento fija su posición V por su
                // índice — la carne fluye CONTINUA por el cuerpo entero.
                float v01 = ((i - 1) / (float)(n - 1) + uvFlow) % 1f;
                if (v01 < 0f) v01 += 1f;
                int y0 = (int)(v01 * (texture.Height - bandas));
                var src = new Rectangle(0, y0, texture.Width, bandas);

                _quads.Add(new GlowQuad
                {
                    Position = centro,
                    Color = color,
                    // escala en px finales: largo (con extensión) × ancho
                    Scale = new Vector2(len + ext * 2f, w),
                    Rotation = tang.ToRotation(),
                    Texture = texture,
                    Source = src,
                });
            }
        }

        /// <summary>
        /// v6.50.67 — LA CINTA EXTRUIDA: la misma cinta pero con anchos y
        /// colores POR SEGMENTO (el borde de energía que se aviva hacia
        /// la punta) — una cinta por cada segmento con su tinte propio.
        /// </summary>
        public static void RibbonTinted(Vector2[] spine, float[] widths, Color[] tints, Texture2D texture,
            float uvFlow = 0f, float edgeInset = 0f, float edgeOutset = 0f)
        {
            if (spine == null || spine.Length < 2 || tints == null || texture == null) return;
            int n = spine.Length;
            for (int i = 1; i < n; i++)
            {
                Color c = i - 1 < tints.Length ? tints[i - 1] : tints[tints.Length - 1];
                if (c.A == 0) continue;

                Vector2 a = spine[i - 1], b = spine[i];
                Vector2 delta = b - a;
                float len = delta.Length();
                if (len < 0.1f) continue;

                float wIzq = i - 1 < widths.Length ? widths[i - 1] : widths[widths.Length - 1];
                float wDer = i < widths.Length ? widths[i] : widths[widths.Length - 1];
                float w = (wIzq + wDer) * 0.5f * (1f + edgeOutset * 2f) * (1f - edgeInset * 2f);
                if (w < 1.5f) continue;

                float ext = MathHelper.Clamp(w * 0.55f, 5f, 30f);
                Vector2 tang = delta * (1f / len);
                Vector2 centro = (a + b) * 0.5f;

                float v01 = ((i - 1) / (float)(n - 1) + uvFlow) % 1f;
                if (v01 < 0f) v01 += 1f;
                int bandas = Math.Min(texture.Height, 64);
                int y0 = (int)(v01 * (texture.Height - bandas));
                var src = new Rectangle(0, y0, texture.Width, bandas);

                _quads.Add(new GlowQuad
                {
                    Position = centro,
                    Color = c,
                    Scale = new Vector2(len + ext * 2f, w),
                    Rotation = tang.ToRotation(),
                    Texture = texture,
                    Source = src,
                });
            }
        }

        // ------------------------------------------------------------------
        //  TEXTURAS COMPARTIDAS (resolución diferida: Asset, no .Value)
        // ------------------------------------------------------------------

        private static Asset<Texture2D> _softGlow;
        private static Asset<Texture2D> _ring;
        private static Asset<Texture2D> _glowOrb;

        /// <summary>Textura de brillo radial suave (la workhorse de la librería).</summary>
        public static Texture2D SoftGlow
        {
            get
            {
                if (_softGlow == null)
                    _softGlow = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Procedural/SoftGlow");
                return _softGlow.Value;
            }
        }

        /// <summary>Orbe con NÚCLEO SÓLIDO y borde suave (para vacíos negros
        /// absolutos y cuerpos compactos de luz).</summary>
        public static Texture2D GlowOrb
        {
            get
            {
                if (_glowOrb == null)
                    _glowOrb = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/GlowOrb");
                return _glowOrb.Value;
            }
        }

        /// <summary>Anillo fino (anillo de fotones, ecos, halos anulares).</summary>
        public static Texture2D Ring
        {
            get
            {
                if (_ring == null)
                    _ring = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Procedural/Ring");
                return _ring.Value;
            }
        }

        // v6.50.18 — el NovaBurst entra al club de las workhouses: el
        // degradado monótono de caída larga (la textura que el usuario
        // pidió por nombre para los destellos finales).
        private static Asset<Texture2D> _novaBurst;

        /// <summary>
        /// v6.50.18 — EL GRADIENTE DEL DESTELLO FINAL (NovaBurst.png, 256²,
        /// caída monótona 255→0 que muere exactamente en su borde): la
        /// textura que sustituye a los discos gaussianos compactos en TODO
        /// destello de explosión — la firma visual de «degradado suave, no
        /// círculo plano».
        /// Null-safe (recargas calientes): null si el asset aún no vive.
        /// </summary>
        public static Texture2D NovaBurst
        {
            get
            {
                try
                {
                    if (_novaBurst == null)
                        _novaBurst = ModContent.Request<Texture2D>(
                            "AethonMod/Content/Effects/Procedural/NovaBurst");
                    return _novaBurst.IsLoaded ? _novaBurst.Value : null;
                }
                catch { return null; }
            }
        }

        // v6.50.27 — la cruz de 8 rayos (el destello con FORMA de destello:
        // un degradado radial siempre se leyó como «círculo grande y liso»).
        private static Asset<Texture2D> _destelloFinal;

        /// <summary>
        /// v6.50.27 — EL DESTELLO DE RAYOS ESTELARES (DestelloFinal.png,
        /// 256²): 4 rayos largos en los ejes + 4 cortos en las diagonales +
        /// núcleo caliente — la forma del lens-flare del cine. Null-safe.
        /// </summary>
        public static Texture2D DestelloFinal
        {
            get
            {
                try
                {
                    if (_destelloFinal == null)
                        _destelloFinal = ModContent.Request<Texture2D>(
                            "AethonMod/Content/Effects/Procedural/DestelloFinal");
                    return _destelloFinal.IsLoaded ? _destelloFinal.Value : null;
                }
                catch { return null; }
            }
        }

        // v6.50.51 — EL ANILLO ARCOÍRIS (la textura que faltaba). El
        // diagnóstico de la .50: «hilos» de Ring BLANCOS + perlas de 10 px
        // = confeti invisible — nadie veía NINGÚN arcoíris. LA CURA: UNA
        // BANDA REAL, HORNEADA (512², cero assets nuevos: el set de
        // entradas del .tmod NO se toca): siete franjas SATURADAS del
        // espectro — rojo FUERA, violeta DENTRO — entre 0.58 y 0.92 del
        // semiancho (la MISMA convención del Ring: sprite 2.174× el radio
        // visible). El RGB viene PREMULTIPLICADO por la máscara (la
        // convención (q,q,q,q) de la casa: en el lote aditivo el aporte
        // cae máscara·color·f LINEAL).
        private static Texture2D _arcoiris;

        /// <summary>
        /// v6.50.51 — LA BANDA ARCOÍRIS HORNEADA: el anillo de siete
        /// franjas del espectro (rojo exterior → violeta interior, bordes
        /// suaves). Null-safe (servidor dedicado / sin dispositivo).
        /// </summary>
        public static Texture2D Arcoiris
        {
            get
            {
                try
                {
                    if (_arcoiris == null || _arcoiris.IsDisposed)
                        _arcoiris = HornearArcoiris();
                    return _arcoiris;
                }
                catch { return null; }
            }
        }

        /// <summary>
        /// v6.50.51 — EL HORNEADO: 512², banda anular [0.58, 0.92] del
        /// semiancho, SIETE franjas del espectro de FUERA hacia DENTRO
        /// (rojo→naranja→amarillo→verde→cian→azul→violeta), bordes de la
        /// banda con smoothstep (±0.012) y franjas CRUJIENTES (el linear
        /// filtering del motor suaviza el píxel de frontera solo).
        /// </summary>
        private static Texture2D HornearArcoiris()
        {
            if (Main.netMode == Terraria.ID.NetmodeID.Server || Main.dedServ) return null;
            var device = Main.graphics?.GraphicsDevice;
            if (device == null) return null;

            const int L = 512;
            const float RInt = 0.58f, RExt = 0.92f;

            // LAS SIETE FRANJAS (índice 0 = VIOLETA interior … 6 = ROJO exterior)
            int[] fr = { 200, 255, 255,  80,  70,  90, 255 };
            int[] fg = { 100, 150, 235, 240, 230, 150,  60 };
            int[] fb = { 255,  30,  80, 110, 240, 255,  50 };

            var data = new Color[L * L];
            float c = (L - 1) * 0.5f;
            for (int y = 0; y < L; y++)
            {
                float fy = (y - c) / c;
                for (int x = 0; x < L; x++)
                {
                    float fx = (x - c) / c;
                    float r = MathF.Sqrt(fx * fx + fy * fy);

                    // LA MÁSCARA (la banda con bordes suaves).
                    float m = Suave(r, RInt - 0.012f, RInt + 0.012f) *
                              (1f - Suave(r, RExt - 0.012f, RExt + 0.012f));
                    if (m <= 0.001f) continue;

                    // LA FRANJA (la posición radial → su color del espectro).
                    float s = MathHelper.Clamp((r - RInt) / (RExt - RInt), 0f, 1f) * 6f;
                    int idx = (int)s;                       // 0 violeta … 6 rojo
                    if (idx > 6) idx = 6;
                    data[y * L + x] = new Color(
                        (byte)(fr[idx] * m), (byte)(fg[idx] * m), (byte)(fb[idx] * m),
                        (byte)(m * 255f));
                }
            }

            var tex = new Texture2D(device, L, L);
            tex.SetData(data);
            return tex;
        }

        /// <summary>El smoothstep de la casa (0→1 entre a y b).</summary>
        private static float Suave(float x, float a, float b)
        {
            float t = MathHelper.Clamp((x - a) / (b - a), 0f, 1f);
            return t * t * (3f - 2f * t);
        }

        // ==================================================================
        //  v6.50.67 — LAS TEXTURAS DE LA CARNE (horreadas, el patrón
        //  Arcoiris: cero assets nuevos, el set del .tmod NO se toca)
        // ==================================================================

        private static Texture2D _carne;

        /// <summary>
        /// v6.50.67 — LA CARNE DEL TENTÁCULO (128×512, horneada): la
        /// textura que convierte la cadena de líneas de la v6.50.66 en
        /// CUERPO ORGÁNICO. U = a lo ancho (bordes suaves que respiran,
        /// fibras musculares verticales, vetas carmesí tenues), V = a lo
        /// largo (PERIÓDICA — la cinta la muestrea por bandas y la carne
        /// FLUYE raíz→punta con uvFlow). Todo PREMULTIPLICADO (la
        /// convención (q,q,q,q) de la casa: el lote alfa es
        /// One/InverseSourceAlpha). Null-safe (servidor / sin dispositivo).
        /// </summary>
        public static Texture2D Carne
        {
            get
            {
                try
                {
                    if (_carne == null || _carne.IsDisposed)
                        _carne = HornearCarne();
                    return _carne;
                }
                catch { return null; }
            }
        }

        /// <summary>El horneado de la carne: 128 de ancho × 512 de largo.</summary>
        private static Texture2D HornearCarne()
        {
            if (Main.netMode == Terraria.ID.NetmodeID.Server || Main.dedServ) return null;
            var device = Main.graphics?.GraphicsDevice;
            if (device == null) return null;

            const int W = 128, H = 512;
            var data = new Color[W * H];

            // LAS FIBRAS (posiciones U fijas, oscuras/claras alternando)
            float[] fibU = { 0.16f, 0.26f, 0.38f, 0.50f, 0.62f, 0.74f, 0.84f };
            float[] fibF = { 1.00f, 0.90f, 1.06f, 0.88f, 1.04f, 0.92f, 1.00f };

            // LAS VETAS CARMESÍ (sinusoides en U según V — periódicas en V)
            float[] vetaU = { 0.30f, 0.58f, 0.78f };
            float[] vetaF = { 3f, 2f, 4f };

            for (int y = 0; y < H; y++)
            {
                float v = y / (float)(H - 1);
                float vv = v * MathHelper.TwoPi;    // período EXACTO del alto
                for (int x = 0; x < W; x++)
                {
                    float u = x / (float)(W - 1);

                    // LA MÁSCARA — bordes suaves: nada en 0/1, casi opaco
                    // en 0.2..0.8 (la silueta respira, no corta a cuchillo)
                    float m = Suave(u, 0.02f, 0.20f) * (1f - Suave(u, 0.80f, 0.98f));
                    if (m <= 0.003f) continue;

                    // EL CUERPO — negro de vacío con un pelo violeta
                    float cuerpo = 1f;

                    // LAS FIBRAS MUSCULARES — estrías verticales vivas
                    for (int f = 0; f < fibU.Length; f++)
                    {
                        float d = MathF.Abs(u - fibU[f]);
                        cuerpo *= 1f + (fibF[f] - 1f) * 0.10f * MathF.Exp(-d * d * 900f)
                            * (0.8f + 0.2f * MathF.Sin(vv * 2f + f));
                    }

                    // LOS ANILLOS — segmentación orgánica cada 64 px (6 anillos)
                    float anillo = 1f - 0.07f * MathF.Pow(MathF.Abs(MathF.Sin(vv * 3f)), 8f);

                    // EL COLOR BASE — carne de vacío (10,6,14) · todo lo de arriba
                    float r = 10f * cuerpo * anillo, g = 6f * cuerpo * anillo, b = 14f * cuerpo * anillo;

                    // LAS VETAS CARMESÍ — la sangre que corre por dentro
                    for (int k = 0; k < vetaU.Length; k++)
                    {
                        float uu = vetaU[k] + 0.045f * MathF.Sin(vv * vetaF[k] + k * 2.1f);
                        float d = MathF.Abs(u - uu);
                        float ven = MathF.Exp(-d * d * 2600f)
                                  * (0.55f + 0.45f * MathF.Sin(vv * 2f + k * 1.7f));
                        r += 118f * ven; g += 10f * ven; b += 16f * ven;
                    }

                    data[y * W + x] = new Color(
                        (byte)(r * m), (byte)(g * m), (byte)(b * m), (byte)(255f * m));
                }
            }

            var tex = new Texture2D(device, W, H);
            tex.SetData(data);
            return tex;
        }

        private static Texture2D _colmillo;

        /// <summary>
        /// v6.50.67 — EL COLMILLO DE HUESO (48×48, horneado): aguja curva
        /// blanca con base oscura — los dientes de las fauces y las puntas
        /// de las garras de la v6.50.66 eran LÍNEAS GORDAS; esto es un
        /// diente de verdad. Apunta ARRIBA (base ancha abajo, punta arriba,
        /// curvado a la derecha). PREMULTIPLICADO. Null-safe.
        /// </summary>
        public static Texture2D Colmillo
        {
            get
            {
                try
                {
                    if (_colmillo == null || _colmillo.IsDisposed)
                        _colmillo = HornearColmillo();
                    return _colmillo;
                }
                catch { return null; }
            }
        }

        private static Texture2D HornearColmillo()
        {
            if (Main.netMode == Terraria.ID.NetmodeID.Server || Main.dedServ) return null;
            var device = Main.graphics?.GraphicsDevice;
            if (device == null) return null;

            const int L = 48;
            var data = new Color[L * L];
            for (int y = 0; y < L; y++)
            {
                float v = y / (float)(L - 1);          // 0 punta … 1 base
                for (int x = 0; x < L; x++)
                {
                    float u = x / (float)(L - 1);

                    // EL EJE del diente (curvado a la derecha al bajar)
                    float eje = 0.5f + 0.16f * (1f - v) * (1f - v);
                    // EL ANCHO — huso: 0 en la punta, máximo en la base
                    float ancho = 0.42f * MathF.Pow(v, 0.65f);
                    float d = MathF.Abs(u - eje);
                    if (d > ancho) continue;

                    float m = 1f - Suave(d / ancho, 0.55f, 1f);
                    if (m <= 0.003f) continue;

                    // HUESO — blanco cálido, la raíz se ensombrece
                    float hueso = 0.55f + 0.45f * v;
                    byte r = (byte)(250 * hueso * m);
                    byte g = (byte)(244 * hueso * m);
                    byte b = (byte)(230 * hueso * m);
                    data[y * L + x] = new Color(r, g, b, (byte)(255f * m));
                }
            }

            var tex = new Texture2D(device, L, L);
            tex.SetData(data);
            return tex;
        }

        private static Texture2D _ventosa;

        /// <summary>
        /// v6.50.67 — LA VENTOSA (32×32, horneada): anillo de hueso con
        /// el agujero oscuro al centro — la fila de ventosas del lomo del
        /// tentáculo (la referencia del spritesheet del usuario: «white
        /// circular suckers along its length»). PREMULTIPLICADO. Null-safe.
        /// </summary>
        public static Texture2D Ventosa
        {
            get
            {
                try
                {
                    if (_ventosa == null || _ventosa.IsDisposed)
                        _ventosa = HornearVentosa();
                    return _ventosa;
                }
                catch { return null; }
            }
        }

        private static Texture2D HornearVentosa()
        {
            if (Main.netMode == Terraria.ID.NetmodeID.Server || Main.dedServ) return null;
            var device = Main.graphics?.GraphicsDevice;
            if (device == null) return null;

            const int L = 32;
            var data = new Color[L * L];
            float c = (L - 1) * 0.5f;
            for (int y = 0; y < L; y++)
            {
                for (int x = 0; x < L; x++)
                {
                    float r = MathF.Sqrt(((x - c) * (x - c) + (y - c) * (y - c))) / c;

                    // EL ANILLO — aro de hueso entre 0.28 y 0.72, agujero dentro
                    float aro = Suave(r, 0.24f, 0.34f) * (1f - Suave(r, 0.66f, 0.80f));
                    if (aro <= 0.003f) continue;

                    // el agujero central — la sombra de la succión
                    float agujero = 1f - Suave(r, 0.30f, 0.48f);
                    float hueso = 0.85f - 0.35f * agujero;

                    data[y * L + x] = new Color(
                        (byte)(246 * hueso * aro), (byte)(240 * hueso * aro),
                        (byte)(228 * hueso * aro), (byte)(255f * aro));
                }
            }

            var tex = new Texture2D(device, L, L);
            tex.SetData(data);
            return tex;
        }

        private static Texture2D _ojoRasgado;

        /// <summary>
        /// v6.50.67 — EL OJO RASGADO (64×64, horneado): esclerótica blanca
        /// cálida + iris carmesí + LA RENDIJA VERTICAL NEGRA del ojo
        /// dracónico de la idea central del usuario. EL TRUCO DEL LOTE
        /// ADITIVO: la rendija se hornea con alpha 0 — en aditivo añade
        /// CERO, así que se lee NEGRA sobre cualquier fondo sin salir
        /// jamás del lote. PREMULTIPLICADO. Null-safe.
        /// </summary>
        public static Texture2D OjoRasgado
        {
            get
            {
                try
                {
                    if (_ojoRasgado == null || _ojoRasgado.IsDisposed)
                        _ojoRasgado = HornearOjoRasgado();
                    return _ojoRasgado;
                }
                catch { return null; }
            }
        }

        private static Texture2D HornearOjoRasgado()
        {
            if (Main.netMode == Terraria.ID.NetmodeID.Server || Main.dedServ) return null;
            var device = Main.graphics?.GraphicsDevice;
            if (device == null) return null;

            const int L = 64;
            var data = new Color[L * L];
            float c = (L - 1) * 0.5f;
            for (int y = 0; y < L; y++)
            {
                float fy = (y - c) / c;
                for (int x = 0; x < L; x++)
                {
                    float fx = (x - c) / c;

                    // LA ELIPSE DEL OJO (ancha, como el ojo de la casa)
                    float e = MathF.Sqrt(fx * fx / (0.78f * 0.78f) + fy * fy / (0.46f * 0.46f));
                    if (e > 1f) continue;
                    float m = 1f - Suave(e, 0.72f, 1f);          // borde suave

                    // LA RENDIJA — vertical, gruesa al centro, se afina arriba/abajo
                    float rendija = MathF.Exp(-fx * fx * 190f) * (1f - 0.35f * fy * fy);
                    // el iris carmesí — anillo alrededor de la rendija
                    float iris = MathF.Exp(-fx * fx * 26f) * (0.35f + 0.65f * (1f - MathF.Abs(fy)));

                    // esclerótica blanca cálida de fondo
                    float r = 244f, g = 238f, b = 226f;
                    // el iris TIÑE de rojo el centro
                    r = MathHelper.Lerp(r, 214f, iris); g = MathHelper.Lerp(g, 26f, iris); b = MathHelper.Lerp(b, 34f, iris);
                    // la rendija NO dibuja (alpha 0 — en aditivo añade cero)
                    m *= 1f - Suave(rendija, 0.55f, 0.85f);
                    if (m <= 0.004f) continue;

                    data[y * L + x] = new Color((byte)(r * m), (byte)(g * m), (byte)(b * m), (byte)(255f * m));
                }
            }

            var tex = new Texture2D(device, L, L);
            tex.SetData(data);
            return tex;
        }

        private static Texture2D _pixel;

        /// <summary>
        /// v6.50.22 — EL PIXEL BLANCO 1×1 DEL MOTOR (TextureAssets.MagicPixel
        /// de vanilla): EL PINCEL DEL DIBUJO 100% CÓDIGO. No es un asset del
        /// mod ni un sprite de rayo — es la primitiva de rectángulo sólido
        /// que el propio motor expone (el mismo que usa el cursor, las
        /// barras y mil detalles de vanilla). Con él, un filamento eléctrico
        /// se construye APILANDO PASADAS SÓLIDAS de ancho decreciente (la
        /// receta del lightning 466 de vanilla): el degradado transversal ES
        /// LA SUMA de las pasadas — cero textura de banda, cero arte.
        /// </summary>
        public static Texture2D Pixel
        {
            get
            {
                if (_pixel == null)
                    _pixel = Terraria.GameContent.TextureAssets.MagicPixel.Value;
                return _pixel;
            }
        }

        /// <summary>
        /// Tamaño de cuadro para que el TRAZO VISIBLE de la textura Ring
        /// (1024px, círculo gráfico a ~0.92 del semiancho) caiga en el radio
        /// pedido: el tamaño final del sprite debe ser ~2.17× ese radio.
        /// </summary>
        public static Vector2 RingQuadSize(float visibleRadius)
        {
            float s = visibleRadius * 2.174f;
            return new Vector2(s, s);
        }

        // ------------------------------------------------------------------
        //  VOLCADO 1 — DIBUJO DIRECTO ADITIVO (mundo / proyectiles)
        // ------------------------------------------------------------------

        /// <summary>
        /// Vuelca el buffer actual con blending ADITIVO: el brillo suma sobre
        /// lo que ya hay en pantalla (neón real). Los cuadros están en coords
        /// de mundo; se les resta Main.screenPosition aquí, una sola vez.
        ///
        /// Semántica de escala: el cuadro mide Scale píxeles FINALES en
        /// pantalla (ancho×alto), sea cual sea la resolución de la textura.
        /// </summary>
        /// <param name="texture">Textura de los cuadros (SoftGlow por defecto).</param>
        /// <param name="endActiveBatch">True si puede haber un batch abierto
        /// que haya que cerrar antes (p. ej. venimos de un PreDraw).</param>
        public static void FlushAdditive(Texture2D texture = null, bool endActiveBatch = true)
        {
            if (_quads.Count == 0) return;
            if (Main.netMode == Terraria.ID.NetmodeID.Server) return;

            Texture2D defaultTex = texture ?? SoftGlow;

            // v6.49 — EL END TAMBIÉN BLINDADO (hallazgo AUD-C): el End del
            // lote del llamador vivía FUERA del try/finally — si lanzaba
            // (lote ya cerrado por un consumidor del patrón viejo), el
            // finally NUNCA corría: _quads quedaba sin limpiar y los
            // cuadros muertos se re-volcaban y re-contaban CADA frame.
            // Ahora TODO el vuelva es atómico: el búfer se limpia pase lo
            // que pase, incluso en el error.
            try
            {
                // v6.50.58 — LA SONDA YA NO CONFÍA EN NADIE: antes, con
                // endActiveBatch=false, el Begin de abajo pisaba un Begin
                // vivo del llamador (el EchoArcher lo hacía CADA frame →
                // InvalidOperationException deduplicada a una línea en el
                // client.log, con medio draw muerto). La sonda pregunta
                // SIEMPRE: si hay un lote vivo, se cierra aquí — el vuelco
                // jamás hace Begin sobre Begin (el llamador descuidado
                // pierde UN frame de adornos, no el draw entero).
                CerrarLoteSiAbierto();

                // v6.41 — EL VOLCADO BLINDADO (try/finally): si UN Draw lanza
                // (dispositivo perdido, textura nula por descarga caliente), el
                // lote ANTERIOR quedaba ABIERTO para siempre → TODO el render
                // del juego se corrompía hasta relogear, y el búfer nunca se
                // limpiaba (los cuadros muertos se re-volcaban cada frame).
                // Ahora: el End y la limpieza se garantizan pase lo que pase.
                // v6.50.66 — EL FIX DEL CRASH DEL CLIENT.LOG (OutOfMemory
                // 0x8007000E al mapear el vertex buffer): Immediate hacía
                // UN Map(Discard) del vertex buffer POR CADA quad — con los
                // cientos de quads de sombra por frame eran ~millones de
                // Maps en minutos y el pool de staging del driver se agotaba
                // (~7,5 min de juego → crash). Deferred acumula TODOS los
                // cuadros del volcado y sube el buffer UNA sola vez (FNA
                // corta el lote solo si cambia la textura — mezclar Pixel,
                // SoftGlow, GlowOrb y Ring sigue siendo gratis).
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                Vector2 screen = Main.screenPosition;
                for (int i = 0; i < _quads.Count; i++)
                {
                    GlowQuad q = _quads[i];
                    if (q.Color.A == 0) continue;

                    Texture2D tex = q.Texture ?? defaultTex;
                    // v6.50.67 — LA BANDA: si hay rectángulo fuente, la
                    // escala y el origen se miden contra ÉL (la textura
                    // larga se muestrea por bandas que fluyen).
                    Vector2 srcSize = q.Source.HasValue
                        ? new Vector2(q.Source.Value.Width, q.Source.Value.Height)
                        : tex.Size();
                    Vector2 escala = q.Scale / srcSize;
                    Main.spriteBatch.Draw(tex, q.Position - screen, q.Source,
                        q.Color, q.Rotation, srcSize * 0.5f, escala, SpriteEffects.None, 0f);
                }
            }
            finally
            {
                // v6.50.11 — sonda: cierra NUESTRO lote aditivo (y solo si
                // sigue vivo — cero first-chance).
                CerrarLoteSiAbierto();
                ContarQuads(_quads.Count);
                _quads.Clear();
            }
        }

        /// <summary>
        /// v6.50.23 — VUELCA EL BUFFER CON BLENDING ALFA: la capa que
        /// OSCURECE. La hermana gemela de <see cref="FlushAdditive"/> para
        /// el patrón Bruma de AuraLib (la Brasa del Eclipse) — el humo
        /// negro de los bordes que NO puede existir en el lote aditivo
        /// (negro = suma 0 = invisible: para oscurecer hay que SALIR del
        /// aditivo). EL MISMO CONTRATO ATÓMICO de la casa (sonda +
        /// try/finally + limpieza SIEMPRE), mismo parámetros, misma
        /// semántica de escala — solo cambia el BlendState.
        ///
        /// LA SONDA DE LA CASA (v6.50.3, verificada contra el FNA real):
        /// BlendState.AlphaBlend = (One, InverseSourceAlpha) — blending
        /// PREMULTIPLICADO: los cuadros del patrón Bruma llevan el TINTE
        /// CLÁSICO (Color·f: RGB y A escalados a la vez) y las láminas
        /// (q,q,q,q) → aporte = q·color + fondo·(1−q·f): el humo TAPA.
        /// </summary>
        /// <param name="texture">Textura de los cuadros (SoftGlow por defecto).</param>
        /// <param name="endActiveBatch">True si puede haber un batch abierto
        /// que haya que cerrar antes (p. ej. venimos de un PreDraw).</param>
        public static void FlushAlpha(Texture2D texture = null, bool endActiveBatch = true)
        {
            if (_quads.Count == 0) return;
            if (Main.netMode == Terraria.ID.NetmodeID.Server) return;

            Texture2D defaultTex = texture ?? SoftGlow;

            try
            {
                // v6.50.58 — LA MISMA SONDA INCONDICIONAL que FlushAdditive:
                // nunca Begin sobre Begin, aunque el llamador jura que no
                // hay lote abierto (ver el fix del EchoArcher — client.log
                // 12:31:33).
                CerrarLoteSiAbierto();

                // v6.50.66 — mismo fix del crash que FlushAdditive:
                // Deferred = 1 Map por volcado (antes: 1 Map POR QUAD).
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                Vector2 screen = Main.screenPosition;
                for (int i = 0; i < _quads.Count; i++)
                {
                    GlowQuad q = _quads[i];
                    if (q.Color.A == 0) continue;

                    Texture2D tex = q.Texture ?? defaultTex;
                    // v6.50.67 — LA BANDA (mismo trato que FlushAdditive).
                    Vector2 srcSize = q.Source.HasValue
                        ? new Vector2(q.Source.Value.Width, q.Source.Value.Height)
                        : tex.Size();
                    Vector2 escala = q.Scale / srcSize;
                    Main.spriteBatch.Draw(tex, q.Position - screen, q.Source,
                        q.Color, q.Rotation, srcSize * 0.5f, escala, SpriteEffects.None, 0f);
                }
            }
            finally
            {
                // el vuelco atómico de la casa: pase lo que pase, NUESTRO
                // lote se cierra (por sonda) y el búfer se limpia.
                CerrarLoteSiAbierto();
                ContarQuads(_quads.Count);
                _quads.Clear();
            }
        }

        /// <summary>
        /// Vuelca el buffer como DrawData dentro de la capa de dibujado de un
        /// jugador (PlayerDrawLayer): se añaden al DrawDataCache y tML los
        /// compone con el resto del jugador — el camino oficial, sin tocar
        /// el estado del renderer. Los cuadros van en coords de MUNDO.
        /// </summary>
        /// <param name="drawInfo">El PlayerDrawSet de la capa.</param>
        /// <param name="texture">Textura de los cuadros (SoftGlow por defecto).</param>
        /// <param name="effects">Efectos de espejado del jugador (para
        /// respetar su dirección).</param>
        public static void AppendToPlayerDraw(ref PlayerDrawSet drawInfo, Texture2D texture = null,
            SpriteEffects effects = SpriteEffects.None)
        {
            if (_quads.Count == 0) return;

            Texture2D defaultTex = texture ?? SoftGlow;
            Vector2 screen = Main.screenPosition;

            for (int i = 0; i < _quads.Count; i++)
            {
                GlowQuad q = _quads[i];
                if (q.Color.A == 0) continue;

                Texture2D tex = q.Texture ?? defaultTex;
                // v6.50.67 — LA BANDA (mismo trato que los volcados).
                Vector2 srcSize = q.Source.HasValue
                    ? new Vector2(q.Source.Value.Width, q.Source.Value.Height)
                    : tex.Size();

                // La posición de DrawData vive en coords de PANTALLA.
                Vector2 pos = q.Position - screen;

                drawInfo.DrawDataCache.Add(new DrawData(
                    tex, pos, q.Source, q.Color, q.Rotation,
                    srcSize * 0.5f, q.Scale / srcSize, effects));
            }

            _quads.Clear();
        }

        // ==================================================================
        //  SECCIÓN v6.50.11 · LA SONDA DE LOTE (el fin de las first-chance)
        // ==================================================================

        /// <summary>
        /// v6.50.11 — ¿Tiene Main.spriteBatch un Begin vivo?
        ///
        /// LA HISTORIA: el End defensivo de la casa ({try { End } catch {}})
        /// nunca dejó escapar nada, pero cada disparo sobre un lote ya
        /// cerrado lanzaba una InvalidOperationException FIRST-CHANCE — y
        /// tML 2026.07 LAS REGISTRA (AppDomain.FirstChanceException → el
        /// WARN "Excepción silenciosa", deduplicado una vez por stack
        /// único: el client.log v6.50.6 del usuario llevaba 27 de NUESTROS
        /// End defensivos, TODAS capturadas por el propio catch — ruido
        /// puro que ensuciaba el diagnóstico de cualquier otra cosa).
        ///
        /// La sonda PREGUNTA antes de tocar: el campo privado "beginCalled"
        /// del SpriteBatch de FNA (verificado en el decompile del tML
        /// 2026.07.3.0 real), cacheado por reflexión una sola vez. Si un
        /// FNA futuro lo renombrara, la sonda devuelve false y los
        /// ayudantes caen al End defensivo clásico (compatible).
        /// </summary>
        private static readonly System.Reflection.FieldInfo _fiBeginCalled =
            typeof(SpriteBatch).GetField("beginCalled",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        /// <summary>True si Main.spriteBatch tiene un Begin vivo (sonda).</summary>
        public static bool LoteAbierto =>
            _fiBeginCalled != null && Main.spriteBatch != null &&
            _fiBeginCalled.GetValue(Main.spriteBatch) is bool abierto && abierto;

        /// <summary>
        /// Cierra el lote del juego SOLO si hay un Begin vivo — cero
        /// excepciones, cero first-chance en el log (el End defensivo de
        /// la casa SIN su costo). DEVUELVE true si cerró un lote vivo (el
        /// rastreo exacto del "lote ajeno" para quien deba reapertura).
        /// Fallback: si la sonda no está disponible (FNA futuro), el End
        /// defensivo clásico.
        /// </summary>
        public static bool CerrarLoteSiAbierto()
        {
            if (_fiBeginCalled == null)
            {
                try { Main.spriteBatch.End(); return true; }
                catch { return false; }
            }
            if (LoteAbierto)
            {
                Main.spriteBatch.End();
                return true;
            }
            return false;
        }

        /// <summary>
        /// v6.50.11 — EL CONTRATO DE CURACIÓN. Reabre el lote con los
        /// parámetros EXACTOS del pase de entidades de vanilla
        /// (Main.DrawProjectiles, medido en el decompile: Deferred ·
        /// AlphaBlend · DefaultSamplerState · None · Main.Rasterizer ·
        /// null · Main.Transform — el patrón v6.50.2).
        ///
        /// LA LECCIÓN DEL client.log: el restore condicional de la casa
        /// ({if (wasActive) Begin}) dejaba el lote CERRADO cuando el
        /// PreDraw lo encontró cerrado — "restauración exacta" que en
        /// realidad devolvía el veneno: tML mata al proyectil que dibuja
        /// con el lote cerrado (try/catch de DrawProjectiles →
        /// projectile.active = false) y el End final del bucle lanza. Un
        /// PreDraw de la casa SIEMPRE sale con el lote ABIERTO y válido —
        /// si llegó roto (mod ajeno), se CURA. Idempotente por sonda: si
        /// ya hay un Begin vivo no lo pisa (Begin sobre Begin lanza en
        /// FNA) — el estado abierto preexistente se respeta y el Begin se
        /// envuelve a prueba de todo.
        /// </summary>
        public static void ReabrirLoteVanilla()
        {
            if (_fiBeginCalled != null && LoteAbierto)
                return; // ya hay un Begin vivo: no lo pisamos
            try
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    Main.DefaultSamplerState, DepthStencilState.None,
                    Main.Rasterizer, null, Main.Transform);
            }
            catch { }
        }

        // ------------------------------------------------------------------
        //  HELPERS DE MOVIMIENTO (los "latidos" de la librería)
        // ------------------------------------------------------------------

        /// <summary>Respiración: 0..1→0.94..1.06 con la fase pedida.</summary>
        public static float Breathe(float time, float speed = 2.2f, float phase = 0f, float amplitude = 0.06f)
        {
            return 1f + amplitude * (float)Math.Sin(time * speed + phase);
        }

        /// <summary>Oscilación suave -1..1 (balanceo, flotación).</summary>
        public static float Sway(float time, float speed = 0.9f, float phase = 0f)
        {
            return (float)Math.Sin(time * speed + phase);
        }

        /// <summary>
        /// Hash determinista [0,1): la MISMA secuencia en todas las máquinas
        /// sin sincronizar nada (rayos, destellos por índice).
        /// </summary>
        public static float Hash01(int seed, int a, int b)
        {
            int h = unchecked(seed * 374761393 + a * 668265263 + b * 1911520717);
            h = unchecked(h ^ (h >> 13));
            h = unchecked(h * 1274126177);
            h = unchecked(h ^ (h >> 16));
            return (h & 0xFFFFFF) / 16777216f;
        }

        /// <summary>Elipse paramétrica: punto en el ángulo t (rad).</summary>
        public static Vector2 Ellipse(Vector2 center, float semiMajor, float semiMinor, float tilt, float t)
        {
            float ct = (float)Math.Cos(t);
            float st = (float)Math.Sin(t);
            // Punto en la elipse sin girar (eje mayor = X).
            Vector2 local = new Vector2(semiMajor * ct, semiMinor * st);
            float cR = (float)Math.Cos(tilt);
            float sR = (float)Math.Sin(tilt);
            return center + new Vector2(local.X * cR - local.Y * sR, local.X * sR + local.Y * cR);
        }

        // ==================================================================
        //  v6.31 — EL FILTRO DE OBJETIVOS: EL MISMO DE TERRARIA BASE
        // ==================================================================

        /// <summary>
        /// ¿Es este NPC un objetivo VÁLIDO para el daño manual de la casa
        /// (escuela A)? v6.31 — LA PETICIÓN LITERAL DEL USUARIO: "que el
        /// filtro sea el mismo que usan las armas de Terraria base y otros
        /// mods". ES EL PREDICADO EXACTO de la puerta de daño de vanilla
        /// (Projectile.cs, la puerta principal proyectil→NPC, medida sobre
        /// el decompile real):
        /// <code>
        ///   npc.active &amp;&amp; !npc.dontTakeDamage &amp;&amp; !npc.friendly
        /// </code>
        /// Consecuencias medibles (todas = comportamiento vanilla):
        ///   · EL TARGET DUMMY CUENTA (muestra números; immortal, su vida
        ///     jamás baja — exactamente como con las armas base).
        ///   · Los NPC AMISTOSOS (pueblos, atados) NO reciben daño — igual
        ///     que una espada base los atraviesa sin herirlos.
        ///   · Los NPC con dontTakeDamage (escenas/inmunes de evento) se
        ///     respetan.
        /// (La vacuna del Guía [type 22 + killGuide] y el gate de i-frames
        /// por jugador viven en el motor de vanilla; nuestro daño manual
        /// lleva SU PROPIA cadencia por diseño — los cooldowns de la casa.)
        /// </summary>
        public static bool EsObjetivo(NPC npc)
            => npc != null && npc.active && !npc.dontTakeDamage && !npc.friendly;

        // ==================================================================
        //  v6.31 — EL MOTOR v2: capas de oclusión + janitor + calidad + presupuesto
        //  (la guía de ingeniería de la super investigación: lo que los mods
        //  top hacen y nuestro motor no tenía)
        // ==================================================================

        /// <summary>
        /// LAS CAPAS DE DIBUJO del motor (v6.31): los efectos que OCULLEN (el
        /// vacío de un desgarro, el horizonte de un agujero negro) necesitan
        /// dibujarse DEBAJO de los NPCs — que el cuerpo del enemigo TAPE el
        /// horizonte de sucesos vende la profundidad que el aditivo no puede.
        /// </summary>
        public enum VFXLayer
        {
            /// <summary>Debajo de todo (detrás de tiles y NPCs).</summary>
            DetrasDeTodo = 0,
            /// <summary>Sobre los tiles, DEBAJO de los NPCs (la capa de la oclusión).</summary>
            DetrasDeNPCs = 1,
            /// <summary>Sobre los NPCs (el brillo final).</summary>
            SobreNPCs = 2,
        }

        // (delegados por capa: el llamador registra su dibujo del frame).
        // v6.50.5 — SIN readonly: el barrendero de Unload lo anula por
        // reflexión y .NET 8 prohíbe escribir campos initonly (la traza
        // del client.log v6.50.4 — FieldAccessException) — el ancla se
        // quedaba viva tras la descarga ("mod class still using memory").
        private static List<Action<SpriteBatch>>[] _capas =
        {
            new List<Action<SpriteBatch>>(32),
            new List<Action<SpriteBatch>>(32),
            new List<Action<SpriteBatch>>(32),
        };

        /// <summary>
        /// REGISTRA un dibujo con CAPA para este frame (v6.31): el delegado se
        /// ejecuta en <see cref="FlushOcclusion"/> en orden de capa. El dibujo
        /// debe abrir/cerrar SUS lotes (el batch que recibe ya está preparado
        /// en NonPremultiplied). Para la luz normal sigue habiendo
        /// <see cref="FlushAdditive"/> — esto es SOLO para lo que OCULLE.
        /// </summary>
        public static void RegisterLayer(Action<SpriteBatch> draw, VFXLayer layer)
        {
            if (draw == null || Main.netMode == NetmodeID.Server) return;
            _capas[(int)layer].Add(draw);
            if (_capas[(int)layer].Count > 64)       // techo de seguridad
                _capas[(int)layer].RemoveAt(0);
        }

        /// <summary>
        /// VUELCA las capas registradas (v6.31): los dibujos oclusivos en su
        /// orden (detrás de todo → detrás de NPCs). El llamador decide DÓNDE
        /// del pipeline lo invoca (p. ej. un ModSystem.PostDrawTiles para que
        /// los NPCs tapen la oclusión). Limpia los registros del frame.
        /// </summary>
        public static void FlushOcclusion()
        {
            if (Main.netMode == NetmodeID.Server) return;
            for (int c = 0; c < _capas.Length; c++)
            {
                if (_capas[c].Count == 0) continue;
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                for (int i = 0; i < _capas[c].Count; i++)
                {
                    try { _capas[c][i](Main.spriteBatch); }
                    catch { }
                }
                Main.spriteBatch.End();
                _capas[c].Clear();
            }
            Janitor();
        }

        /// <summary>
        /// EL CONSERJE DE ESTADO (v6.31 — la lección de higiene de pipeline de
        /// la super investigación): los shaders que registran texturas en los
        /// SLOTS 1..3 del dispositivo los dejan PEGADOS si el frame termina a
        /// mitad de pase (pausa, teleport, excepción) — y el siguiente dibujo
        /// que use esos slots sale CORROMPIDO. Este barrido los anula al
        /// terminar el volcado de capas: un solo lugar, cero víctimas.
        /// </summary>
        private static void Janitor()
        {
            try
            {
                var device = Main.graphics?.GraphicsDevice;
                if (device == null) return;
                device.Textures[1] = null;
                device.Textures[2] = null;
                device.Textures[3] = null;
                // v6.50.3 — FIX (los samplers quedaban PEGADOS): los shaders
                // del sol registran SamplerStates[1..2] = LinearWrap y NADIE
                // los restauraba (el conservaje cubría solo las texturas —
                // un draw futuro del slot 1 sin sampler propio muestreaba en
                // WRAP). LinearClamp: el neutro de los slots de ruido.
                device.SamplerStates[1] = SamplerState.LinearClamp;
                device.SamplerStates[2] = SamplerState.LinearClamp;
                device.SamplerStates[3] = SamplerState.LinearClamp;
            }
            catch { }
        }

        // --- LA PUERTA DE CALIDAD ---

        /// <summary>Las familias de efectos que PUEDEN no dibujarse según la
        /// configuración del jugador (v6.31 — el quality-gate de la casa).</summary>
        public enum CalidadFX
        {
            /// <summary>Post-proceso de pantalla (bloom propio, aberración fuerte).</summary>
            PostProceso,
            /// <summary>Warp/distorsión de pantalla (lentes, calor).</summary>
            WarpPantalla,
            /// <summary>Brillos HDR apilados.</summary>
            Bloom,
            /// <summary>Lluvias de partículas cosméticas densas.</summary>
            ParticulasAltas,
        }

        private static bool _renderEspecial;
        private static uint _frameDeCalidad;

        /// <summary>
        /// ¿Está PERMITIDA esta familia de efectos? (v6.31): la puerta que
        /// hay que consultar ANTES de cualquier post-proceso — en el menú o
        /// sin dispositivo el post-proceso rompe (la lección de seguridad de
        /// render de la investigación; la detección de iluminación Retro/
        /// Trippy no es API pública en tML 2026.07: el gate es conservador
        /// con lo disponible, listo para ensancharse).
        /// </summary>
        public static bool CalidadPermitida(CalidadFX f)
        {
            uint frame = Main.GameUpdateCount;
            if (frame != _frameDeCalidad)
            {
                _frameDeCalidad = frame;
                try { _renderEspecial = Main.gameMenu || Main.graphics?.GraphicsDevice == null; }
                catch { _renderEspecial = true; }
            }
            return !_renderEspecial;
        }

        // --- EL PRESUPUESTO DE CUADROS ---

        private static int _quadsDelFrame;
        private static uint _frameDelPresupuesto;

        /// <summary>
        /// EL PRESUPUESTO (v6.31 — anti-"abrumador"): cuenta los cuadros
        /// vuelcados este frame; los llamadores COSMÉTICOS consultan esto
        /// antes de emitir (si hay 3 jefes con bruma a la vez, las lluvias de
        /// partículas decorativas se saltan solas). Techo ~24000 quads/frame
        /// — que desde v6.34 RESPIRA: se multiplica por <see cref="FactorCalidad"/>
        /// (con factor 1 el techo es el de siempre, idéntico).
        /// </summary>
        public static bool Presupuesto(int quadsQueQuieroEmitir)
        {
            uint frame = Main.GameUpdateCount;
            if (frame != _frameDelPresupuesto)
            {
                _frameDelPresupuesto = frame;
                _quadsDelFrame = 0;
            }
            // v6.34 — EL TECHO RESPIRA: el factor adaptativo multiplica el
            // límite efectivo (factor 1 → 24000 exactos, comportamiento
            // IDÉNTICO al clásico; factor 0.5 → la mitad de techo).
            return _quadsDelFrame + quadsQueQuieroEmitir <= (int)(24000f * _factorCalidad);
        }

        /// <summary>Registra consumo del presupuesto (lo llama FlushAdditive).</summary>
        private static void ContarQuads(int n)
        {
            uint frame = Main.GameUpdateCount;
            if (frame != _frameDelPresupuesto)
            {
                _frameDelPresupuesto = frame;
                _quadsDelFrame = 0;
            }
            _quadsDelFrame += n;
        }

        // --- v6.34 — EL PRESUPUESTO ADAPTATIVO (la calidad que respira) ---

        /// <summary>
        /// La media móvil EXPONENCIAL de los FPS (0.9·prev + 0.1·último):
        /// nace en 60 — un hijack de un frame (carga, pausa) NO tira la
        /// calidad; solo el hundimiento sostenido cuenta.
        /// </summary>
        private static float _fpsSuave = 60f;

        /// <summary>
        /// El factor de calidad ACTUAL (0.5..1): multiplica el techo de
        /// <see cref="Presupuesto"/>. Arranca en 1 (sin recortes).
        /// </summary>
        private static float _factorCalidad = 1f;

        /// <summary>
        /// El factor de calidad ACTUAL (0.5..1) — público para que los
        /// renderizadores que quieran acompañen la respiración (escalar sus
        /// propias lluvias de partículas, etc.). 1 = el comportamiento de
        /// siempre.
        /// </summary>
        public static float FactorCalidad => _factorCalidad;

        /// <summary>
        /// v6.34 — REPORTA los FPS reales del juego. v6.49 (auditoría
        /// AUD-C): la llama CalidadFpsSystem.PostUpdateEverything — el
        /// "nadie la llama" del comentario viejo era doc-rot.
        /// Lógica: media móvil EXPONENCIAL (0.9·prev + 0.1·fps) y el factor
        /// de calidad RESPIRA con ella — si el promedio cae por debajo de
        /// 45 FPS, el factor BAJA 0.05 por reporte (suelo 0.5: ni a la mitad
        /// del techo se recorta más); si supera los 55, SUBE 0.02 por reporte
        /// (techo 1: recuperación LENTA a propósito — se pierde calidad en un
        /// pico y se recupera con calma, sin ver el yo-yó). El factor
        /// multiplica el límite efectivo del presupuesto: LA CALIDAD SE
        /// ADAPTA SOLA — si los FPS caen, el mod adelgaza sus efectos; nadie
        /// tiene que configurar nada.
        /// </summary>
        public static void ReportarFps(float fps)
        {
            // Robustez: un NaN/infinito envenenaría la media PARA SIEMPRE
            // (0.9·NaN = NaN) y dejaría el factor congelado.
            if (float.IsNaN(fps) || float.IsInfinity(fps)) return;

            _fpsSuave = _fpsSuave * 0.9f + fps * 0.1f;
            if (_fpsSuave < 45f)
                _factorCalidad = Math.Max(_factorCalidad - 0.05f, 0.5f);
            else if (_fpsSuave > 55f)
                _factorCalidad = Math.Min(_factorCalidad + 0.02f, 1f);
        }

        // ------------------------------------------------------------------
        //  v6.49 — LA HIGIENE DEL NÚCLEO (hallazgo AUD-C: "todas las
        //  hermanas tienen Reiniciar; el núcleo no"). El búfer, las
        //  texturas perezosas y el presupuesto se sueltan al recargar
        //  el mod / cambiar de mundo — lo llama DiagnosticoVFXSystem
        //  (que también es el overlay F8 de la casa).
        // ------------------------------------------------------------------

        /// <summary>
        /// v6.49 — LA LIMPIEZA DEL NÚCLEO: búfer vacío, presupuesto en
        /// cero, factor y FPS de vuelta al nacimiento. Para Unload y
        /// OnWorldUnload (las texturas pediosas se repiden solas).
        /// v6.50.56 — FIX DEL CRASH DEL client.log: OnWorldUnload corre en
        /// la cadena de guardado (.NET TP Worker) y el Dispose directo de
        /// la textura es una OPERACIÓN GRÁFICA («most FNA3D audio/graphics
        /// functions must be called on the main thread» —
        /// ThreadStateException al salir del mundo). EL FUNERAL ENCOLADO,
        /// el patrón de AuraLib.Reiniciar: capturar, despegar y dejar que
        /// el hilo principal lo entierre (Main.QueueMainThreadAction); si
        /// ya estamos EN el hilo principal, entierro inmediato.
        /// </summary>
        public static void Reiniciar()
        {
            try { _quads.Clear(); } catch { }
            // v6.50.51 — el anillo horneado también se suelta (la textura
            // perezosa se rehornea sola al próximo uso — 1 ms).
            Texture2D arcoirisViejo = _arcoiris;
            _arcoiris = null;
            if (arcoirisViejo != null)
            {
                try
                {
                    if (Main.dedServ ||
                        System.Threading.Thread.CurrentThread.ManagedThreadId == _hiloPrincipal)
                    {
                        arcoirisViejo.Dispose();
                    }
                    else
                    {
                        Main.QueueMainThreadAction(() =>
                        {
                            try { arcoirisViejo.Dispose(); } catch { }
                        });
                    }
                }
                catch { }
            }
            _quadsDelFrame = 0;
            _frameDelPresupuesto = 0;
            _factorCalidad = 1f;
            _fpsSuave = 60f;
        }

        /// <summary>
        /// v6.50.56 — EL HILO PRINCIPAL DE LA CASA: se aprende en el primer
        /// PostDrawTiles (que vanilla SIEMPRE corre en el hilo del juego) —
        /// el Reiniciar lo consulta antes de tocar una textura.
        /// </summary>
        private static int _hiloPrincipal = -1;

        /// <summary>v6.50.56 — APRENDE el hilo principal (lo llama VFXCoreSystem.PostDrawTiles).</summary>
        internal static void AprenderHiloPrincipal()
        {
            _hiloPrincipal = System.Threading.Thread.CurrentThread.ManagedThreadId;
        }

        /// <summary>v6.49 — EL DIAGNÓSTICO del núcleo (lo pinta el overlay F8).</summary>
        public static int QuadsDelFrame => _quadsDelFrame;
    }

    /// <summary>
    /// VFXCoreSystem — v6.50.2 — FIX: EL SUBSISTEMA QUE NADIE LLAMABA.
    /// RegisterLayer/FlushOcclusion/Janitor existían desde v6.31 como
    /// la protección documentada (volcar las capas de oclusión y anular
    /// las texturas que RuneSunRenderer/CicloEstelarRenderer/SunProjectile/
    /// BlackHoleProjectile dejan BIND-EADAS en los slots 1..3 del
    /// dispositivo), pero grep-verificado NADIE invocaba la puerta
    /// pública: la protección jamás corrió (subsistema muerto — el doc
    /// prometía higiene que no existía).
    ///
    /// Este ModSystem la CONECTA en PostDrawTiles — el punto exacto que
    /// la documentación de <see cref="VFXCore.FlushOcclusion"/> propone
    /// («p. ej. un ModSystem.PostDrawTiles para que los NPCs tapen la
    /// oclusión»): vanilla llama SystemLoader.PostDrawTiles() justo
    /// después de cerrar el lote de tiles y ANTES de dibujar los
    /// proyectiles (decompile: spriteBatch.End() → PostDrawTiles() →
    /// DrawProjectiles()), así que el barrido del Janitor despeja los
    /// slots ANTES del pase de entidades de cada frame (las texturas
    /// bind-eadas por los renderizadores del frame anterior ya no
    /// contaminan el dibujado siguiente).
    ///
    /// EL GUARD (netMode == Server) es el MISMO de todo el stack VFX de
    /// la casa (ParticleManager.PostDrawTiles, RuneSun, auras…): devuelve
    /// al servidor dedicado Y AL HOST del listen-server (netMode 2), donde
    /// este mod no renderiza nada — no hay slots que limpiar ahí
    /// (FlushOcclusion se auto-guarda igual, doble puerta). En el menú no
    /// hay mundo ni renderizadores registrando nada.
    /// </summary>
    public class VFXCoreSystem : ModSystem
    {
        /// <summary>
        /// EL PUNTO ÚNICO DEL VOLCADO DE CAPAS + LA HIGIENE: vuelca las
        /// capas de oclusión registradas (nadie hoy: los _capas quedan
        /// vacíos y el volcado es no-op) y corre el Janitor — la limpieza
        /// de los slots de textura que los shaders del mod dejan pegados.
        /// Con las capas vacías el coste es el barrido del Janitor (tres
        /// asignaciones de slot nulas) — cero lotes abiertos.
        /// </summary>
        public override void PostDrawTiles()
        {
            // v6.50.56 — EL HILO PRINCIPAL, APRENDIDO (este hook corre
            // SIEMPRE en el hilo del juego — el Reiniciar de OnWorldUnload
            // [hilo worker] lo consulta antes de disponer una textura).
            VFXCore.AprenderHiloPrincipal();

            // v6.50.2 — FIX (subsistema muerto): la llamada que faltaba.
            // Solo procesos que dibujan; jamás romper el frame por higiene.
            if (Main.netMode == NetmodeID.Server || Main.gameMenu) return;
            try { VFXCore.FlushOcclusion(); }
            catch { }
        }
    }
}
