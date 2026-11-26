using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// TajoLib — v6.40 → v6.174 — LA ESCRITURA DEL FILORTE (los tajos del anime).
    ///
    /// v6.174 — LA LUNA DE UNA PIEZA (la letra, por tercera vez: «siguen
    /// con líneas seccionadas; un tajo debe ser una línea continua sin
    /// subdivisiones»). LA AUTOPSIA DE VERDAD: desde la v6.40 la luna era
    /// una COLLERA DE QUADS — 32 rectángulos rotados con SOLAPE de 3 px.
    /// El lote aditivo SUMA: en cada uno de los 32 empalmes el solape
    /// contaba DOS veces → 32 cuentas más brillantes, una cada 5 px (con
    /// radio 80 y 110° cada quad mide 4,75 px y 3 de ellos eran solape:
    /// el 63 % de la línea era «unión»). Más segmentos y más solape
    /// (v6.173) = más cuentas, más cerca. LA CURA: LA MALLA CONTINUA —
    /// la luna es UNA TIRA DE TRIÁNGULOS (RayoStrip, el renderer de tiras
    /// de la casa): cada banda nace de UNA sola pasada por el arco, sin
    /// principio ni fin por dentro — no hay empalmes que sumar.
    ///
    /// LO QUE VIVE AQUÍ (el canon de la investigación sakuga v6.40/v6.172):
    ///   · EL PERFIL DE LENTE G(u)=sin(π·u)^0.55 — grueso al centro, fino
    ///     en las puntas — evaluado POR VÉRTICE (la luna se afila sola).
    ///   · DIEZ BANDAS EMBEBIDAS (8·ancho de halo → 1·ancho de núcleo):
    ///     el degradado transversal es la SUMA de las bandas, cada una
    ///     con su peso de núcleo; el halo parpadea con UNA ONDA que
    ///     viaja por el arco — jamás ruido por segmento.
    ///   · EL FRAME DE IMPACTO (el flash monocromo de los 2 primeros
    ///     ticks), EL FRENTE del revelado, LAS PUNTAS prendidas y las
    ///     CHISPAS del filo exterior (tres como mucho, AFUERA de la línea).
    ///   · LA ELIPSE (EscalaY 0,78) con su NORMAL VERDADERA (la banda
    ///     sigue el grosor real de la elipse, no el de un círculo).
    ///
    /// LA TIRA ES DE LA CASA: <see cref="Cinta"/> dibuja cualquier línea
    /// continua por puntos (la librería de líneas del mod — la usan el
    /// Faro del Eclipse y cualquier arma que necesite UN trazo sin uniones).
    ///
    /// CONTRATO (Sección A de la casa): batch ABIERTO en aditivo → ABIERTO
    /// (RayoStrip cierra el lote vivo y lo reabre). Coordenadas de
    /// PANTALLA (el llamador resta Main.screenPosition). Cero Main.rand.
    /// </summary>
    public static class TajoLib
    {
        // ==================================================================
        //  EL PINCEL
        // ==================================================================

        private static Asset<Texture2D> _glow;

        /// <summary>SoftGlow: los PUNTOS (frente, puntas, flash) — lo
        /// circular se dibuja con textura; lo LINEAL con la malla.</summary>
        private static Texture2D Glow =>
            (_glow ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow")).Value;

        /// <summary>Tinte de intensidad para el SpriteBatch (RGB intacto,
        /// alfa = f: el lote aditivo GATEA por el alfa).</summary>
        public static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color(c.R, c.G, c.B, (byte)(int)(255f * f));
        }

        /// <summary>EL COLOR PRE-MULTIPLICADO del pozo de triángulos (el
        /// contrato de RayoStrip.VertColor: el rgb escala en la CPU y el
        /// alfa vale 255 — el lote de pantalla SUMA el rgb tal cual).</summary>
        public static Color Premult(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color((byte)(int)(c.R * f + 0.5f), (byte)(int)(c.G * f + 0.5f),
                (byte)(int)(c.B * f + 0.5f), 255);
        }

        /// <summary>La mezcla de dos colores de la casa (k = 0 → a, 1 → b).</summary>
        public static Color Mezcla(Color a, Color b, float k)
        {
            k = MathHelper.Clamp(k, 0f, 1f);
            return new Color((int)(a.R + (b.R - a.R) * k), (int)(a.G + (b.G - a.G) * k),
                (int)(a.B + (b.B - a.B) * k), 255);
        }

        // ==================================================================
        //  LA GEOMETRÍA
        // ==================================================================

        /// <summary>Las muestras MÍNIMAS de un arco completo (v6.174: la
        /// malla densifica sola con los píxeles de arco — 2,5 px por
        /// muestra, de 12 a 96 — la curva es LISA a cualquier radio).</summary>
        public const int Segmentos = 32;

        /// <summary>v6.172 — LA COMPRESIÓN VERTICAL de la elipse (0,78: el
        /// corte katana entra EN DIAGONAL al plano).</summary>
        public const float EscalaY = 0.78f;

        private const int MaxMuestras = 96;

        // LAS DIEZ BANDAS (de AFUERA hacia ADENTRO): semiancho en múltiplos
        // de `ancho`, peso y porcentaje de núcleo. La suma en el centro es
        // 0,986 (blanco-caliente) y cae sin escalones: 0,955 en el borde
        // de la vena, 0,90 a 1,7, 0,72 a 2,7, 0,38 a 4,6, 0,10 a 8.
        private static readonly float[] BandaF = { 8f, 6f, 4.6f, 3.5f, 2.7f, 2.1f, 1.7f, 1.4f, 1.18f, 1f };
        private static readonly float[] BandaA = { 0.012f, 0.022f, 0.04f, 0.07f, 0.12f, 0.2f, 0.3f, 0.4f, 0.55f, 0.9f };
        private static readonly float[] BandaN = { 0f, 0f, 0f, 0f, 0f, 0f, 0.25f, 0.5f, 0.75f, 1f };

        // el telar de la malla (estático: cero GC por frame)
        private static readonly Vector2[] _A = new Vector2[MaxMuestras + 1];
        private static readonly Vector2[] _B = new Vector2[MaxMuestras + 1];
        private static readonly Color[] _C = new Color[MaxMuestras + 1];

        /// <summary>
        /// UN Tajo — la media luna del anime en UN instante de su vida.
        /// (LA FIRMA DE SIEMPRE — compatible con todos los llamadores.)
        /// CONTRATO: batch ABIERTO en aditivo → ABIERTO.
        /// </summary>
        public static void Tajo(Vector2 centro, float radio, float ang0, float ang1,
            float prog, float brillo, float ancho, Color halo, Color nucleo,
            int seed, float time)
        {
            TajoDoble(centro, radio, ang0, ang1, prog, brillo, ancho,
                halo, nucleo, seed, time, 1f, 0f);
        }

        /// <summary>
        /// v6.174 — EL Tajo DE LA MALLA CONTINUA. `smeares` ya no son hojas
        /// paralelas (eran la segunda línea de la letra): es UNA aureola
        /// ancha y tenue pegada al filo (×12, 1 %). `impacto` = la edad
        /// relativa del nacimiento (0..1): los 2 primeros ticks pintan el
        /// FRAME DE IMPACTO (el flash monocromo del sakuga).
        /// </summary>
        public static void TajoDoble(Vector2 centro, float radio, float ang0, float ang1,
            float prog, float brillo, float ancho, Color halo, Color nucleo,
            int seed, float time, float smeares, float impacto)
        {
            if (brillo <= 0.02f || radio < 6f || ancho < 0.6f) return;
            prog = MathHelper.Clamp(prog, 0f, 1f);
            if (prog <= 0.01f) return;

            float span = ang1 - ang0;
            float medio = (ang0 + ang1) * 0.5f;

            // --- 0. EL FRAME DE IMPACTO (el dibujo monocromo escondido
            //     1-2 frames dentro de la secuencia: el golpe LEE antes
            //     que el filo). Va al lote de sprites del llamador, ANTES
            //     de la malla (en el aditivo el orden no cambia la suma). ---
            if (impacto > 0f && impacto < 0.28f)
            {
                float f = 1f - impacto / 0.28f;
                Quad(Glow, centro, new Vector2(radio * 0.55f, radio * 0.34f * EscalaY),
                    medio + MathHelper.PiOver2, Tint(nucleo, 0.85f * f));
                Quad(Glow, centro, new Vector2(radio * 1.05f, ancho * 0.9f),
                    medio, Tint(nucleo, 0.6f * f));
                Quad(Glow, centro, new Vector2(ancho * 0.9f, radio * 1.05f * EscalaY),
                    medio, Tint(nucleo, 0.5f * f));
            }

            // --- 4. EL FRENTE DEL REVELADO (la punta del crecimiento
            //     ardiendo: el punto + la cruz de luz). ---
            if (prog < 1f)
            {
                float angFront = ang0 + span * prog;
                Vector2 pFront = PuntoArco(centro, radio, angFront);
                Quad(Glow, pFront, new Vector2(ancho * 2.6f, ancho * 2.6f), 0f,
                    Tint(nucleo, 0.65f * brillo));
                Quad(Glow, pFront, new Vector2(ancho * 4.8f, ancho * 0.65f), 0f,
                    Tint(nucleo, 0.45f * brillo));
                Quad(Glow, pFront, new Vector2(ancho * 0.65f, ancho * 4.8f), 0f,
                    Tint(nucleo, 0.45f * brillo));
            }

            // --- 5. LAS PUNTAS (el creciente completo: prenden y sueltan
            //     su chispa). ---
            if (prog >= 0.999f)
            {
                PrenderPunta(centro, radio, ang0, ancho, halo, nucleo, brillo, seed);
                PrenderPunta(centro, radio, ang1, ancho, halo, nucleo, brillo, seed + 3);
            }

            // --- 1·2·3·3.5 — LA LUNA ENTERA: DIEZ BANDAS CONTINUAS en el
            //     pozo de triángulos de la casa (RayoStrip cierra el lote
            //     de sprites vivo, vacia la malla y reabre el lote). ---
            try
            {
                RayoStrip.AbrirLote();
                try
                {
                    Tejer(centro, radio, ang0, span, prog, brillo, ancho, halo, nucleo,
                        seed, time, smeares);
                }
                finally
                {
                    RayoStrip.VolcarTriangulos();
                    RayoStrip.CerrarLote();
                }
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("TajoLib", exRegistro); }
        }

        /// <summary>
        /// v6.172 — EL Tajo CRUZADO (la variante del combo): DOS medias
        /// lunas EN X — el doble corte cruzado del anime. Cada una es una
        /// malla continua; la segunda nace espejada sobre el eje medio.
        /// </summary>
        public static void TajoCruzado(Vector2 centro, float radio, float ang0, float ang1,
            float prog, float brillo, float ancho, Color halo, Color nucleo,
            int seed, float time, float smeares)
        {
            TajoDoble(centro, radio, ang0, ang1, prog, brillo, ancho,
                halo, nucleo, seed, time, smeares, 0f);
            float medio = (ang0 + ang1) * 0.5f;
            float a0b = 2f * medio - ang1;
            float a1b = 2f * medio - ang0;
            TajoDoble(centro, radio, a1b, a0b, prog, brillo * 0.92f, ancho * 0.92f,
                halo, nucleo, seed + 11, time, smeares * 0.5f, 0f);
        }

        // ==================================================================
        //  LOS EMISORES DE LA MALLA
        // ==================================================================

        /// <summary>
        /// TEJE la luna: recorre el arco UNA VEZ por banda y apila la tira
        /// de triángulos (dos por muestra). El semiancho de cada vértice
        /// es ancho·F·(0,06 + 0,94·G(u)): la luna nace gruesa al centro y
        /// se cierra en las puntas SIN recortes. Debe correr DENTRO de un
        /// lote de RayoStrip (TajoDoble lo abre y lo cierra).
        /// </summary>
        public static void Tejer(Vector2 centro, float radio, float ang0, float span, float prog,
            float brillo, float ancho, Color halo, Color nucleo, int seed, float time, float smeares)
        {
            int n = (int)(Math.Abs(span) * prog * radio / 2.5f) + 2;
            n = Math.Max(12, Math.Min(MaxMuestras, n));
            int nb = BandaF.Length + (smeares > 0.05f ? 1 : 0);
            for (int k = 0; k < nb; k++)
            {
                bool aureola = k == BandaF.Length;
                float f = aureola ? 12f : BandaF[k];
                float peso = aureola ? 0.010f * smeares : BandaA[k];
                float nucleoK = aureola ? 0f : BandaN[k];
                Color tinta = Mezcla(halo, nucleo, nucleoK);

                for (int i = 0; i <= n; i++)
                {
                    float u = prog * i / n;
                    float a = ang0 + span * u;
                    Vector2 p = PuntoArco(centro, radio, a);

                    // LA NORMAL DE LA ELIPSE: la tangente de
                    // (cos a·R, sin a·R·EscalaY) girada un cuarto.
                    Vector2 t = new Vector2(-(float)Math.Sin(a), (float)Math.Cos(a) * EscalaY);
                    float lt = t.Length();
                    if (lt < 0.0001f) lt = 1f;
                    Vector2 nor = new Vector2(-t.Y / lt, t.X / lt);

                    float g = PerfilLente(u);
                    float w = ancho * f * (0.06f + 0.94f * g);
                    _A[i] = p - nor * w;
                    _B[i] = p + nor * w;

                    // LA ONDA que viaja por el halo (viva, continua); el
                    // núcleo no parpadea NUNCA.
                    float vivo = nucleoK < 0.5f
                        ? 0.86f + 0.14f * (float)Math.Sin(time * 34f + u * 6.283f + seed % 7)
                        : 1f;
                    _C[i] = Premult(tinta, peso * brillo * (0.35f + 0.65f * g) * vivo);
                }
                EmitirCinta(_A, _B, _C, n);
            }

            ChispasDelFilo(centro, radio, ang0, span, prog, brillo, ancho, halo, seed, time);
        }

        /// <summary>
        /// LA TIRA: n + 1 columnas (borde A, borde B, color) → 2·n
        /// triángulos en el pozo de RayoStrip. Un solo trazo, cero
        /// empalmes: la línea continua de la casa.
        /// </summary>
        public static void EmitirCinta(Vector2[] a, Vector2[] b, Color[] c, int n)
        {
            for (int i = 0; i < n; i++)
            {
                RayoStrip.TrianguloColoresEnLote(a[i], b[i], b[i + 1], c[i], c[i], c[i + 1]);
                RayoStrip.TrianguloColoresEnLote(a[i], b[i + 1], a[i + 1], c[i], c[i + 1], c[i + 1]);
            }
        }

        /// <summary>
        /// v6.174 — LA LÍNEA CONTINUA de la casa: una cinta por PUNTOS
        /// arbitrarios (la curva que sea), con su semiancho y su alfa por
        /// punto. El trazo de los haces, las espirales y las estelas que
        /// NO deben verse hechas de cuadrados. Abre y cierra su lote.
        /// </summary>
        public static void Cinta(Vector2[] pts, float[] semiancho, Color color, float[] alfa = null)
        {
            if (pts == null || semiancho == null || pts.Length < 2 || semiancho.Length < pts.Length) return;
            int n = Math.Min(pts.Length - 1, MaxMuestras);
            for (int i = 0; i <= n; i++)
            {
                int ia = Math.Max(0, i - 1);
                int ib = Math.Min(pts.Length - 1, i + 1);
                Vector2 d = pts[ib] - pts[ia];
                float ld = d.Length();
                Vector2 nor = ld > 0.0001f ? new Vector2(-d.Y / ld, d.X / ld) : new Vector2(0f, 1f);
                _A[i] = pts[i] - nor * semiancho[i];
                _B[i] = pts[i] + nor * semiancho[i];
                _C[i] = Premult(color, alfa == null ? 1f : alfa[i]);
            }
            try
            {
                RayoStrip.AbrirLote();
                try { EmitirCinta(_A, _B, _C, n); }
                finally
                {
                    RayoStrip.VolcarTriangulos();
                    RayoStrip.CerrarLote();
                }
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("TajoLib", exRegistro); }
        }

        /// <summary>
        /// LAS CHISPAS DEL FILO EXTERIOR — la falda del corte: como mucho
        /// TRES puntitos deterministas que ESCAPAN hacia afuera del filo
        /// mientras la luna se revela. Viven AFUERA de la línea (2,6·ancho
        /// de margen): jamás pueden leerse como una unión.
        /// </summary>
        private static void ChispasDelFilo(Vector2 centro, float radio, float ang0, float span,
            float prog, float brillo, float ancho, Color halo, int seed, float time)
        {
            if (prog >= 0.999f) return;
            for (int s = 0; s < 3; s++)
            {
                float h = VFXCore.Hash01(seed, 47 + s, (int)(time * 30f) % 89);
                if (h < 0.5f) continue;
                float a = ang0 + span * prog * (0.4f + 0.6f * h);
                Vector2 dir = new Vector2((float)Math.Cos(a), (float)Math.Sin(a) * EscalaY);
                float ld = dir.Length();
                if (ld < 0.0001f) ld = 1f;
                dir /= ld;
                Vector2 p = PuntoArco(centro, radio, a) + dir * (ancho * (2.6f + 3.4f * h));
                float tam = ancho * (0.4f + 0.45f * h);
                Color c = Premult(halo, 0.3f * brillo * h);
                RayoStrip.TrianguloColoresEnLote(p - new Vector2(tam, 0f), p + new Vector2(0f, tam),
                    p + new Vector2(tam, 0f), c, c, c);
                RayoStrip.TrianguloColoresEnLote(p - new Vector2(tam, 0f), p + new Vector2(tam, 0f),
                    p - new Vector2(0f, tam), c, c, c);
            }
        }

        /// <summary>
        /// LA PUNTA del creciente completo: el punto de luz + la chispa
        /// que escapa del filo (determinista por Hash01).
        /// </summary>
        private static void PrenderPunta(Vector2 centro, float radio, float ang,
            float ancho, Color halo, Color nucleo, float brillo, int seed)
        {
            Vector2 p = PuntoArco(centro, radio, ang);
            Quad(Glow, p, new Vector2(ancho * 2.2f, ancho * 2.2f), 0f,
                Tint(halo, 0.55f * brillo));
            Quad(Glow, p, new Vector2(ancho * 1.0f, ancho * 1.0f), 0f,
                Tint(nucleo, 0.92f * brillo));

            float h = VFXCore.Hash01(seed, 17, 3);
            if (h > 0.4f)
            {
                Vector2 dir = new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang));
                Vector2 sp = p + dir * (2f + 3f * h) * (ancho * 0.6f);
                Quad(Glow, sp, new Vector2(ancho * 1.4f, ancho * 1.4f), 0f,
                    Tint(nucleo, 0.5f * brillo * h));
            }
        }

        // ==================================================================
        //  LOS HELPERS (privados, el pincel de la casa)
        // ==================================================================

        /// <summary>El PERFIL DE LENTE del creciente: grosor máximo al
        /// centro, fino en las puntas (la media luna). POR LUT (hallazgo
        /// AUD-B de la v6.49): una tabla de 33 muestras, interpolada.</summary>
        public static float PerfilLente(float u)
        {
            u = MathHelper.Clamp(u, 0f, 1f) * (_lenteLutN - 1);
            int i0 = (int)u;
            int i1 = i0 + 1 < _lenteLutN ? i0 + 1 : i0;
            float k = u - i0;
            return _lenteLut[i0] + (_lenteLut[i1] - _lenteLut[i0]) * k;
        }

        private const int _lenteLutN = 33;
        private static readonly float[] _lenteLut = BuildLenteLut();

        private static float[] BuildLenteLut()
        {
            var lut = new float[_lenteLutN];
            for (int i = 0; i < _lenteLutN; i++)
            {
                float s = (float)Math.Sin(Math.PI * (i / (float)(_lenteLutN - 1)));
                lut[i] = (float)Math.Pow(Math.Max(s, 0.001f), 0.55f);
            }
            return lut;
        }

        /// <summary>Punto del arco (centro + radio + ángulo) — LA ELIPSE:
        /// Y comprimida ×EscalaY.</summary>
        private static Vector2 PuntoArco(Vector2 centro, float radio, float ang)
            => centro + new Vector2((float)Math.Cos(ang) * radio,
                (float)Math.Sin(ang) * radio * EscalaY);

        /// <summary>Quad centrado al batch actual (tamaño total = size px).</summary>
        private static void Quad(Texture2D tex, Vector2 pos, Vector2 size, float rot, Color tint)
        {
            if (tint.A == 0) return;
            Main.spriteBatch.Draw(tex, pos, null, tint, rot,
                new Vector2(tex.Width, tex.Height) * 0.5f,
                size / new Vector2(tex.Width, tex.Height),
                SpriteEffects.None, 0f);
        }

        /// <summary>
        /// LA CAMPANA DE VIDA del anime (asimétrica: subida rápida al
        /// 55%, caída larga al 45 % restante). t = 0..1 de la vida.
        /// </summary>
        public static float Campana(float t)
        {
            t = MathHelper.Clamp(t, 0f, 1f);
            float subida = MathHelper.Clamp(t / 0.55f, 0f, 1f);
            float caida = MathHelper.Clamp((1f - t) / 0.45f, 0f, 1f);
            return subida * caida;
        }
    }
}
