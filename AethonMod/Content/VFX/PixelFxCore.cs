using System;
using System.Collections.Generic;

// ======================================================================
//  PixelFxCore.cs — EL PORT EXACTO (1:1, NO IMITACIÓN) DEL MOTOR
//  pixelfx de patch-gg (MIT, 3.7k★) a C#.
//
//  POR QUÉ ESTE ARCHIVO EXISTE: la v6.90 escribió las tres armas del
//  Taller de Efectos «a mano» (imitación) y el usuario lo vio al
//  instante: «todos esos efectos se ven horrible, veo que no usaste lo
//  que te dije que usaras». La directiva es literal: «usando
//  exactamente esto […] es usarlo exactamente no solo intentar
//  imitarlo que eso nunca funciona». Así que aquí está EL MOTOR:
//  cada función traduce una función del JS de /tmp/research/pixelfx
//  (el origen se cita en el comentario de cada una) y la matemática
//  va en DOUBLE de principio a fin — el JS usa doubles IEEE-754, y si
//  el port usara float el hash/ruido divergiría al primer valor
//  distinto y TODO el efecto se desincronizaría con el original.
//
//  QUÉ SE PORTÓ (y de dónde):
//   · src/core/math.js     → PxMath (TAU, clamp/lerp/seg/smooth, el
//                            hash murmur de enteros, mulberry32, el
//                            ruido de valores con fundido cúbico).
//   · src/core/color.js    → PxColor (colores como uint RGBA en el
//                            layout de ImageData: A<<24|B<<16|G<<8|R).
//   · src/core/fx.js       → PxFrame (los DOS buffers del motor:
//                            color opaco + luz aditiva) y
//                            PxFxInstance (createFx: substeps a 120 Hz,
//                            el place() del origen, el fade de stop()).
//   · src/core/painter.js  → PxPainter (put/add/blend/dither Bayer 4×4,
//                            each/shade, line/circle/ellipse/disc,
//                            glow/glowLine — TODO con la semántica
//                            píxel-exacta del JS).
//   · src/core/sim.js      → PxParticles/PxBits/sparkle (las chispas,
//                            ascuas y esquirlas con física real).
//   · src/core/particles.js→ Cycle (los bucles sin costura: cada
//                            partícula nace en su fase y su rng es el
//                            MISMO cada frame — determinista).
//   · src/fx/shadow/_lib.js→ PxShadowLib (VOID, unlight, ringFast,
//                            halo, Puffs: la llama violeta y el hollín).
//   · Los 3 efectos        → PxHexSigil (shadow/hex-sigil.js),
//                            PxShadowFlames (shadow/shadow-flames.js),
//                            PxEclipse (shadow/eclipse.js) con sus
//                            constantes, semillas y tiempos LITERALES.
//
//  LO QUE NO SE PORTÓ (nadie de los 3 efectos lo usa): font.js/text,
//  sprites/masks del painter, actor/rig/poses/act.js (el teatro del
//  personaje — Terraria pone el cuerpo), scene.js (backdrops —
//  Terraria pone el mundo), stream() de particles.js, fbm/tileNoise
//  (los efectos shadow usan noise2), Blazes/Lights/flare/band/crystal
//  de sim.js, y el resto de shadow/_lib.js (shard/Drops/floorPool/
//  blackStar/attract — son de curse/void-orb/black-hole).
//
//  VERIFICACIÓN: este archivo es C# PURO (cero XNA/Terraria) a
//  propósito — el arnés /tmp/pixelfx-harness lo compila solo y lo
//  compara PÍXEL A PÍXEL contra los renders del motor JS original
//  (node + tools/lib/fxlib.mjs). Composite() reproduce el
//  compositor de fx.js (la luz sobre transparencia se premultiplica,
//  se suma y se divide por el alfa nuevo — «premultiplied»).
//
//  COSTE (por qué es viable en el juego): son simulaciones LOCALES
//  de 104×112 a 168×128 píxeles (11k–23k px), no fullscreen. Cada
//  proyectil vivo paga: clear de 2 buffers + N substeps de 1/120 s +
//  el dibujo (cientos de ops de píxel) + un composite de ~23k px +
//  UN Texture2D.SetData del tamaño del efecto. Del orden de decenas
//  de µs a 60 fps; los buffers se liberan al morir el proyectil.
// ======================================================================

namespace AethonMod.Content.VFX
{
    // ==================================================================
    //  src/core/math.js — «Numbers, easing, randomness and noise.
    //  Everything here is pure and deterministic, so an effect drawn
    //  at time t always looks the same.»
    // ==================================================================
    internal static class PxMath
    {
        public const double TAU = Math.PI * 2;

        // math.js: clamp = (x, a = 0, b = 1) => (x < a ? a : x > b ? b : x)
        public static double Clamp(double x, double a = 0, double b = 1) => x < a ? a : x > b ? b : x;
        public static double Clamp01(double x) => Clamp(x, 0, 1);
        // math.js: lerp
        public static double Lerp(double a, double b, double t) => a + (b - a) * t;
        // math.js: seg = (t, t0, t1) => clamp((t - t0) / (t1 - t0))
        public static double Seg(double t, double t0, double t1) => Clamp01((t - t0) / (t1 - t0));
        // math.js: smooth — hermite smoothstep entre e0 y e1
        public static double Smooth(double e0, double e1, double x)
        {
            double t = Seg(x, e0, e1);
            return t * t * (3 - 2 * t);
        }

        // math.js: ease.outCubic (el único ease que usan los 3 efectos)
        public static double OutCubic(double t) => 1 - Math.Pow(1 - t, 3);

        // Math.imul de JS: multiplicación entera de 32 bits truncada.
        // Con uint el resultado bit a bit es idéntico (imul firma int32,
        // pero XOR/AND/>>>& son operaciones de bits: mismo patrón).
        internal static uint Imul(uint a, uint b) => unchecked(a * b);

        /// <summary>math.js: hash(...ns) — el hash murmur- FINALIZER de
        /// enteros de 32 bits que alimenta TODO el azar determinista del
        /// motor (semillas de partícula, celdas del ruido…).</summary>
        public static uint Hash(params int[] ns)
        {
            uint h = 0x9e3779b9u;
            foreach (int n in ns)
            {
                h = Imul(h ^ (uint)n, 0x85ebca6bu);   // h = Math.imul(h ^ (n | 0), 0x85ebca6b)
                h ^= h >> 13;                          // h ^= h >>> 13
                h = Imul(h, 0xc2b2ae35u);              // h = Math.imul(h, 0xc2b2ae35)
                h ^= h >> 16;                          // h ^= h >>> 16
            }
            return h;                                  // return h >>> 0
        }

        /// <summary>math.js: rand(...ns) — determinista en [0,1) desde enteros.</summary>
        public static double Rand(params int[] ns) => Hash(ns) / 4294967296.0;

        /// <summary>math.js: class Rng — mulberry32. El orden EXACTO de
        /// llamadas a Next() decide el aspecto del efecto: el JS y este
        /// port consumen el generador en la misma secuencia.</summary>
        public sealed class Rng
        {
            uint s;

            // JS: constructor(seed = 1) { this.s = seed >>> 0 || 1; }
            public Rng(uint seed = 1) => s = seed == 0 ? 1u : seed;

            public double Next()
            {
                s = unchecked(s + 0x6d2b79f5u);                       // t = (this.s = (this.s + 0x6d2b79f5) >>> 0)
                uint t = s;
                t = Imul(t ^ (t >> 15), t | 1u);                       // t = Math.imul(t ^ (t >>> 15), t | 1)
                t ^= unchecked(t + Imul(t ^ (t >> 7), t | 61u));       // t ^= t + Math.imul(t ^ (t >>> 7), t | 61)
                return (t ^ (t >> 14)) / 4294967296.0;                 // ((t ^ (t >>> 14)) >>> 0) / 4294967296
            }

            public double Range(double a, double b) => a + (b - a) * Next();
            public int Int(int a, int b) => (int)Math.Floor(Range(a, b + 1));
            public bool Chance(double p) => Next() < p;
            public uint Pick(uint[] list) => list[(int)Math.Floor(Next() * list.Length)];
        }

        /// <summary>math.js: rng(...ns) — un generador por partícula:
        /// rng(seed, i) da los mismos números cada frame.</summary>
        public static Rng RngOf(params int[] ns) => new Rng(Hash(ns));

        /// <summary>math.js: noise2(x, y, seed) — ruido de valores 2D suave
        /// en [-1,1] con la interpolación cúbica (el fundido 3-2f). El
        /// retículo se hash-ea con rand(ix, iy, seed): DOBLES siempre.</summary>
        public static double Noise2(double x, double y, int seed = 0)
        {
            int ix = (int)Math.Floor(x), iy = (int)Math.Floor(y);
            double fx = x - ix, fy = y - iy;
            double ux = fx * fx * (3 - 2 * fx), uy = fy * fy * (3 - 2 * fy);
            double a = Rand(ix, iy, seed), b = Rand(ix + 1, iy, seed);
            double c = Rand(ix, iy + 1, seed), d = Rand(ix + 1, iy + 1, seed);
            return Lerp(Lerp(a, b, ux), Lerp(c, d, ux), uy) * 2 - 1;
        }
    }

    // ==================================================================
    //  src/core/color.js — «Colors are 32-bit integers laid out like
    //  ImageData bytes (R, G, B, A in memory)». Aquí: uint con
    //  A<<24 | B<<16 | G<<8 | R (el mismo entero que ve el JS).
    // ==================================================================
    internal static class PxColor
    {
        public static uint Rgb(int r, int g, int b)
            => 0xFF000000u | ((uint)(b & 255) << 16) | ((uint)(g & 255) << 8) | (uint)(r & 255);
        public static uint Red(uint c) => c & 255;
        public static uint Green(uint c) => (c >> 8) & 255;
        public static uint Blue(uint c) => (c >> 16) & 255;

        /// <summary>color.js: hex() — "#rgb"/"#rrggbb" (o un número, que
        /// pasa tal cual) a color del motor.</summary>
        public static uint Hex(string h)
        {
            string s = h.Replace("#", "");
            if (s.Length == 3) s = "" + s[0] + s[0] + s[1] + s[1] + s[2] + s[2];
            int v = Convert.ToInt32(s.Substring(0, 6), 16);
            return Rgb((v >> 16) & 255, (v >> 8) & 255, v & 255);
        }
        public static uint HexN(uint c) => c; // hex() con un número: passthrough

        /// <summary>color.js: mix() — mezcla lineal, t clampeado. La
        /// resta va en ENTEROS CON SIGNO: red() devuelve un Number en el
        /// JS, y con uint la resta (42-70) desbordaría.</summary>
        public static uint Mix(uint a, uint b, double t)
        {
            t = PxMath.Clamp01(t);
            int ra = (int)Red(a), ga = (int)Green(a), ba = (int)Blue(a);
            return Rgb((int)(ra + ((int)Red(b) - ra) * t),
                       (int)(ga + ((int)Green(b) - ga) * t),
                       (int)(ba + ((int)Blue(b) - ba) * t));
        }

        /// <summary>color.js: scale() — multiplicar el brillo por k.</summary>
        public static uint Scale(uint c, double k)
            => Rgb((int)Math.Min(255, Red(c) * k), (int)Math.Min(255, Green(c) * k), (int)Math.Min(255, Blue(c) * k));

        /// <summary>color.js: ramp() — una lista de hex a colores del motor.</summary>
        public static uint[] Ramp(params string[] list)
        {
            var r = new uint[list.Length];
            for (int i = 0; i < list.Length; i++) r[i] = Hex(list[i]);
            return r;
        }

        /// <summary>color.js: fit(r, n) — remuestrear una rampa a n pasos
        /// conservando los extremos, para indexar por número.</summary>
        public static uint[] Fit(uint[] r, int n)
        {
            if (r.Length == n) return r;
            var o = new uint[n];
            for (int i = 0; i < n; i++)
                o[i] = r[(int)Math.Round((i * (r.Length - 1)) / (double)Math.Max(1, n - 1))];
            return o;
        }

        public static readonly uint WHITE = Hex("#ffffff");
        public static readonly uint BLACK = Hex("#000000");
    }

    // ==================================================================
    //  src/core/fx.js: createFrame — «A frame: w x h pixels whose
    //  top-left sits at (ox, oy) in world space. `color` holds opaque
    //  pixels, `light` additive glow; composite() puts them together.»
    //  LOS DOS BUFFERS DEL MOTOR, tal cual.
    // ==================================================================
    internal sealed class PxFrame
    {
        public readonly int w, h, ox, oy;
        public readonly uint[] color, light;
        PxPainter _painter; // fx.js: painterFor(frame) — uno por frame, reutilizado

        public PxFrame(int w, int h, int ox = 0, int oy = 0)
        {
            this.w = w; this.h = h; this.ox = ox; this.oy = oy;
            color = new uint[w * h];
            light = new uint[w * h];
        }

        /// <summary>fx.js: clearFrame(f) — f.color.fill(0); f.light.fill(0).</summary>
        public void Clear()
        {
            Array.Clear(color, 0, color.Length);
            Array.Clear(light, 0, light.Length);
        }

        /// <summary>fx.js: painterFor(frame) — el painter del frame (reutilizado, con reset()).</summary>
        public PxPainter Painter()
        {
            if (_painter == null) _painter = new PxPainter(this);
            return _painter.Reset();
        }
    }

    // ==================================================================
    //  src/core/painter.js — «The painter draws into a frame's two
    //  buffers: `color` (opaque pixels, faded with ordered dither,
    //  never blended) and `light` (additive glow, composited on top).
    //  Effects draw in local coordinates around their origin; the
    //  painter places, mirrors and rotates them.»
    // ==================================================================
    internal sealed class PxOut // painter.js: const OUT = { c: 0, a: 1, l: 0, k: 0 }
    {
        public uint c; public double a = 1; public uint l; public double k;
    }

    internal delegate void PxEachFn(double lx, double ly, int sx, int sy);
    internal delegate uint PxShadeFn(double x, double y, PxOut o);
    internal delegate uint PxColorFn(double d, double ang);
    internal delegate double PxAlphaFn(double d, double ang);

    internal sealed class PxPainter
    {
        // painter.js: BAYER — «a pixel at alpha a is drawn when a is above its cell's threshold»
        static readonly double[] BAYER = InitBayer();
        static double[] InitBayer()
        {
            int[] b = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };
            var o = new double[16];
            for (int i = 0; i < 16; i++) o[i] = (b[i] + 0.5) / 16;
            return o;
        }

        readonly PxFrame frame;
        public int w, h, ox, oy;
        public uint[] C, L;               // los dos buffers (L es público: shadow-flames y unlight lo escriben directo)
        public double tx, ty, fx = 1, cs = 1, sn;
        public double fade = 1, lightK = 1;
        public string layer = "front";    // painter.js lleva la capa — los efectos la ignoran; se portó por fidelidad
        readonly PxOut OUT = new PxOut();

        public PxPainter(PxFrame frame) { this.frame = frame; Reset(); }

        /// <summary>painter.js: reset() — releer el frame y limpiar colocación/fade/luz.</summary>
        public PxPainter Reset()
        {
            w = frame.w; h = frame.h; ox = frame.ox; oy = frame.oy;
            C = frame.color; L = frame.light;
            tx = 0; ty = 0; fx = 1; cs = 1; sn = 0;
            fade = 1; lightK = 1; layer = "front";
            return this;
        }

        /// <summary>painter.js: place(x, y, flip, angle) — el origen del
        /// efecto en (x, y) del frame. SIN ESTO el efecto se dibuja en
        /// (0,0) y «no aparece»: es la traslación que el arnés oficial
        /// usa (x = meta.origin[0], y = meta.origin[1]).</summary>
        public PxPainter Place(double x, double y, bool flip = false, double angle = 0)
        {
            tx = x; ty = y; fx = flip ? -1 : 1;
            cs = Math.Cos(angle); sn = Math.Sin(angle);
            return this;
        }

        // ---- transforms: local <-> frame (painter.js)
        public double ToX(double x, double y) => tx + x * fx * cs - y * sn;
        public double ToY(double x, double y) => ty + x * fx * sn + y * cs;
        public double FromX(double sx, double sy) { double dx = sx - tx, dy = sy - ty; return (dx * cs + dy * sn) * fx; }
        public double FromY(double sx, double sy) { double dx = sx - tx, dy = sy - ty; return -dx * sn + dy * cs; }
        // «the screen pixel of the local pixel cell holding (x, y)»
        public int CellX(double x, double y) => (int)Math.Floor(ToX(Math.Floor(x) + 0.5, Math.Floor(y) + 0.5));
        public int CellY(double x, double y) => (int)Math.Floor(ToY(Math.Floor(x) + 0.5, Math.Floor(y) + 0.5));

        /// <summary>painter.js: put(sx, sy, c, a) — escritura cruda, píxel
        /// entero. Color 0 = «nada». El fade es DITHER ordenado Bayer,
        /// nunca una mezcla: así se ven los píxeles de pixelfx.</summary>
        public void Put(int sx, int sy, uint c, double a = 1)
        {
            if (c == 0) return;
            a *= fade;
            if (a < 1 && a <= BAYER[((sy & 3) << 2) | (sx & 3)]) return;
            int x = sx - ox, y = sy - oy;
            if (x < 0 || y < 0 || x >= w || y >= h) return;
            C[y * w + x] = c;
        }

        /// <summary>painter.js: add(sx, sy, c, k) — luz aditiva al buffer L.</summary>
        public void Add(int sx, int sy, uint c, double k = 1)
        {
            k *= fade * lightK;
            if (k <= 0.004) return;
            int x = sx - ox, y = sy - oy;
            if (x < 0 || y < 0 || x >= w || y >= h) return;
            int i = y * w + x; uint d = L[i];
            uint r = (uint)Math.Min(255.0, (d & 255) + (c & 255) * k);
            uint g = (uint)Math.Min(255.0, ((d >> 8) & 255) + ((c >> 8) & 255) * k);
            uint b = (uint)Math.Min(255.0, ((d >> 16) & 255) + ((c >> 16) & 255) * k);
            L[i] = 0xFF000000u | (b << 16) | (g << 8) | r;
        }

        /// <summary>painter.js: blendPx — mezclar un color con alfa sobre lo
        /// que hay, conservando el alfa propio (humo/vapor translúcido).</summary>
        public void BlendPx(int sx, int sy, uint c, double a)
        {
            a *= fade;
            if (c == 0 || a <= 0.004) return;
            int x = sx - ox, y = sy - oy;
            if (x < 0 || y < 0 || x >= w || y >= h) return;
            int i = y * w + x; uint d = C[i];
            double da = (d >> 24) / 255.0, oa = a + da * (1 - a), k = a / oa;
            uint Ch(int s) => (uint)((int)(((c >> s) & 255) * k + ((d >> s) & 255) * (1 - k)) & 255);
            C[i] = ((((uint)(int)(oa * 255)) & 255) << 24) | (Ch(16) << 16) | (Ch(8) << 8) | Ch(0);
        }

        /// <summary>painter.js: blend(x, y, c, a) — píxel translúcido en un punto LOCAL.</summary>
        public void Blend(double x, double y, uint c, double a) => BlendPx(CellX(x, y), CellY(x, y), c, a);

        /// <summary>painter.js: get(x, y) — leer el color bajo un punto local (0 si vacío).</summary>
        public uint Get(double x, double y)
        {
            int sx = CellX(x, y) - ox, sy = CellY(x, y) - oy;
            return sx < 0 || sy < 0 || sx >= w || sy >= h ? 0 : C[sy * w + sx];
        }

        // ---- pixels (painter.js)
        public void Px(double x, double y, uint c) => Put(CellX(x, y), CellY(x, y), c, 1);
        public void Dot(double x, double y, uint c, double a = 1) { if (a > 0) Put(CellX(x, y), CellY(x, y), c, a); }
        public void Light(double x, double y, uint c, double k = 1) => Add(CellX(x, y), CellY(x, y), c, k);

        /// <summary>painter.js: each — «Visit every screen pixel whose center
        /// falls in the local box [x0,x1]x[y0,y1]… The basis of every
        /// filled shape, so shapes mirror and rotate exactly.»</summary>
        public void Each(double x0, double y0, double x1, double y1, PxEachFn fn)
        {
            double ax = double.PositiveInfinity, bx = double.NegativeInfinity;
            double ay = double.PositiveInfinity, by = double.NegativeInfinity;
            for (int k = 0; k < 4; k++)
            {
                double x = (k & 1) != 0 ? x1 : x0, y = (k & 2) != 0 ? y1 : y0;
                double sx = ToX(x, y), sy = ToY(x, y);
                if (sx < ax) ax = sx; if (sx > bx) bx = sx;
                if (sy < ay) ay = sy; if (sy > by) by = sy;
            }
            int sx0 = Math.Max((int)Math.Floor(ax), ox), sx1 = Math.Min((int)Math.Ceiling(bx), ox + w - 1);
            int sy0 = Math.Max((int)Math.Floor(ay), oy), sy1 = Math.Min((int)Math.Ceiling(by), oy + h - 1);
            for (int sy = sy0; sy <= sy1; sy++)
                for (int sx = sx0; sx <= sx1; sx++)
                {
                    double cx = sx + 0.5, cy = sy + 0.5;
                    fn(FromX(cx, cy), FromY(cx, cy), sx, sy);
                }
        }

        /// <summary>painter.js: shade — «A pixel shader over a local box.
        /// fn(x, y, out) returns a color, or sets out.c/out.a/out.l/out.k.
        /// Return nothing to leave the pixel alone.» (Aquí: devolver 0 = nada.)</summary>
        public void Shade(double x0, double y0, double x1, double y1, PxShadeFn fn)
        {
            PxOut o = OUT;
            Each(x0, y0, x1, y1, (lx, ly, sx, sy) =>
            {
                o.c = 0; o.a = 1; o.l = 0; o.k = 0;
                uint c = fn(lx, ly, o);
                if (c == 0) c = o.c;                     // const c = fn(...) || o.c
                if (c != 0) Put(sx, sy, c, o.a);
                if (o.k > 0) Add(sx, sy, o.l != 0 ? o.l : c, o.k);
            });
        }

        /// <summary>painter.js: line — 1 px, Bresenham tal cual (con el dither de put).</summary>
        public void Line(double x0, double y0, double x1, double y1, uint c, double a = 1)
        {
            int sx = CellX(x0, y0), sy = CellY(x0, y0);
            int ex = CellX(x1, y1), ey = CellY(x1, y1);
            int dx = Math.Abs(ex - sx), dy = -Math.Abs(ey - sy);
            int stepx = sx < ex ? 1 : -1, stepy = sy < ey ? 1 : -1;
            double n = Math.Max(dx, -dy); if (n == 0) n = 1;   // Math.max(dx, -dy) || 1
            int err = dx + dy, i = 0;
            for (;;)
            {
                double u = i++ / n;
                Put(sx, sy, c, a);
                if (sx == ex && sy == ey) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; sx += stepx; }
                if (e2 <= dx) { err += dx; sy += stepy; }
            }
        }

        /// <summary>painter.js: circle — disco lleno (c/a funciones de d: 0 centro..1 borde, y ángulo).</summary>
        public void Circle(double cx, double cy, double r, PxColorFn c, PxAlphaFn a)
        {
            if (r < 0.6) { Dot(cx, cy, c(0, 0), a(0, 0)); return; }
            Shade(cx - r, cy - r, cx + r, cy + r, (x, y, o) =>
            {
                double dx = x - cx, dy = y - cy, d2 = dx * dx + dy * dy;
                if (d2 > r * r) return 0;
                double d = Math.Sqrt(d2) / r, ang = Math.Atan2(dy, dx);
                o.c = c(d, ang); o.a = a(d, ang);
                return 0;
            });
        }
        public void Circle(double cx, double cy, double r, uint c, double a = 1)
            => Circle(cx, cy, r, (_, _) => c, (_, _) => a);

        /// <summary>painter.js: ellipse (c/a funciones de d normalizado y ángulo).</summary>
        public void Ellipse(double cx, double cy, double rx, double ry, PxColorFn c, PxAlphaFn a)
        {
            if (rx <= 0 || ry <= 0) return;
            Shade(cx - rx, cy - ry, cx + rx, cy + ry, (x, y, o) =>
            {
                double dx = (x - cx) / rx, dy = (y - cy) / ry, d2 = dx * dx + dy * dy;
                if (d2 > 1) return 0;
                double d = Math.Sqrt(d2), ang = Math.Atan2(dy, dx);
                o.c = c(d, ang); o.a = a(d, ang);
                return 0;
            });
        }
        public void Ellipse(double cx, double cy, double rx, double ry, uint c, double a = 1)
            => Ellipse(cx, cy, rx, ry, (_, _) => c, (_, _) => a);

        /// <summary>painter.js: disc — «A translucent disc: puffs of smoke,
        /// vapour, dust» (blendPx, no dither: volúmenes translúcidos).</summary>
        public void Disc(double cx, double cy, double r, PxColorFn c, PxAlphaFn a)
        {
            if (r < 0.5) { Blend(cx, cy, c(0, 0), a(0, 0)); return; }
            double r2 = r * r;
            Each(cx - r, cy - r, cx + r, cy + r, (x, y, sx, sy) =>
            {
                double dx = x - cx, dy = y - cy, d2 = dx * dx + dy * dy;
                if (d2 > r2) return;
                double d = Math.Sqrt(d2) / r, ang = Math.Atan2(dy, dx);
                BlendPx(sx, sy, c(d, ang), a(d, ang));
            });
        }
        public void Disc(double cx, double cy, double r, uint c, double a)
            => Disc(cx, cy, r, (_, _) => c, (_, _) => a);
        public void Disc(double cx, double cy, double r, uint c, PxAlphaFn a)
            => Disc(cx, cy, r, (_, _) => c, a);

        // ---- light (painter.js)
        /// <summary>glow — luz radial en el buffer L: k en el centro, 0 en r.</summary>
        public void Glow(double x, double y, double r, uint c, double k = 0.5)
        {
            if (r < 1 || k <= 0.004) return;
            double cx = ToX(x, y), cy = ToY(x, y);
            int x0 = (int)Math.Floor(cx - r), x1 = (int)Math.Ceiling(cx + r);
            int y0 = (int)Math.Floor(cy - r), y1 = (int)Math.Ceiling(cy + r);
            for (int sy = y0; sy <= y1; sy++)
            {
                double dy = (sy + 0.5 - cy) / r;
                for (int sx = x0; sx <= x1; sx++)
                {
                    double dx = (sx + 0.5 - cx) / r, d = dx * dx + dy * dy;
                    if (d >= 1) continue;
                    double f = 1 - Math.Sqrt(d);
                    Add(sx, sy, c, f * f * k);
                }
            }
        }

        /// <summary>glowLine — luz alrededor de un segmento (un haz suave).</summary>
        public void GlowLine(double x0, double y0, double x1, double y1, double r, uint c, double k = 0.5)
        {
            if (r < 1 || k <= 0.004) return;
            double ax = ToX(x0, y0), ay = ToY(x0, y0), bx = ToX(x1, y1), by = ToY(x1, y1);
            double dx = bx - ax, dy = by - ay, L2 = dx * dx + dy * dy; if (L2 == 0) L2 = 1e-9;
            int X0 = (int)Math.Floor(Math.Min(ax, bx) - r), X1 = (int)Math.Ceiling(Math.Max(ax, bx) + r);
            int Y0 = (int)Math.Floor(Math.Min(ay, by) - r), Y1 = (int)Math.Ceiling(Math.Max(ay, by) + r);
            for (int sy = Y0; sy <= Y1; sy++)
                for (int sx = X0; sx <= X1; sx++)
                {
                    double px = sx + 0.5, py = sy + 0.5;
                    double u = PxMath.Clamp01(((px - ax) * dx + (py - ay) * dy) / L2);
                    double ex = ax + dx * u - px, ey = ay + dy * u - py, d = Math.Sqrt(ex * ex + ey * ey) / r;
                    if (d >= 1) continue;
                    double f = 1 - d;
                    Add(sx, sy, c, f * f * k);
                }
        }
    }

    // ==================================================================
    //  src/core/particles.js: cycle — «Particles that repeat every
    //  `period` seconds, for loops with no seam. Particle i is born at
    //  its own phase in the period and lives `life` seconds… r is a
    //  fresh rng for particle i, the same each period.»
    //  LA DETERMINISMO DEL MOTOR DE BUCLES: cada partícula re-crea su
    //  rng desde hash(seed, i) en CADA frame — mismos números siempre.
    // ==================================================================
    internal delegate void PxCycleFn(double age, double life, PxMath.Rng r);

    internal static class PxCycle
    {
        public static void Run(double t, double period, int count, int seed, double life, PxCycleFn fn)
        {
            for (int i = 0; i < count; i++)
            {
                PxMath.Rng g = PxMath.RngOf(seed, i, 911);
                double born = g.Next() * period;
                double L = life;
                double age = (((t - born) % period) + period) % period;
                for (; age < L; age += period) fn(age, L, PxMath.RngOf(seed, i));
            }
        }

        public static void Run(double t, double period, int count, int seed, Func<PxMath.Rng, double> life, PxCycleFn fn)
        {
            for (int i = 0; i < count; i++)
            {
                PxMath.Rng g = PxMath.RngOf(seed, i, 911);
                double born = g.Next() * period;
                double L = life(g);
                double age = (((t - born) % period) + period) % period;
                for (; age < L; age += period) fn(age, L, PxMath.RngOf(seed, i));
            }
        }
    }

    // ==================================================================
    //  src/core/sim.js — «Simulated particles, debris and lights, for
    //  effects that want real physics» (Adaptado del VFX open-source
    //  de Sprite Fusion / Hugo Duprez, MIT). Unidades: píxeles de arte
    //  y segundos; se dibuja a través de un Painter.
    // ==================================================================
    internal static class PxSim
    {
        public const double GRAVITY = 950; // px/s² (sim.js)

        // sim.js: HOT y las rampas FIRE/SMOKE
        public static readonly uint[] HOT = PxColor.Ramp("#fff3a6", "#ffe26b", "#ff8a2a", "#f2561d");
        public static readonly uint[] FIRE_RAMP = PxColor.Ramp("#ffffff", "#fff3a6", "#ffe26b", "#ffc23d", "#ff8a2a", "#f2561d", "#c9281f", "#8a1f17", "#4a1712");
        public static readonly uint[] SMOKE_RAMP = PxColor.Ramp("#7b6f72", "#5d5660", "#433d48", "#2b2630", "#1a171e");

        // sim.js: rampAt — el paso de la rampa para t en 0..1, sin mezclar
        internal static uint RampAt(uint[] r, double t)
            => r[Math.Min(r.Length - 1, Math.Max(0, (int)Math.Floor(t * r.Length)))];

        // sim.js: P — tipos de partícula (los 3 efectos usan SPARK/EMBER/STREAK;
        // el resto se portó entero porque es el bucle de simulación del motor)
        public const int SPARK = 0, MOTE = 1, STREAK = 2, FLAKE = 3, VAPOUR = 4, DUST = 5, STAR = 6, EMBER = 7, SMOKE = 8, FIRE = 9, BRAND = 10, GRIT = 11;
        public const int PF_SMOKES = 1, PF_BIG = 2, PF_BACK = 4; // sim.js: PF
        static bool Soft(int k) => k == VAPOUR || k == DUST || k == SMOKE; // sim.js: SOFT

        internal sealed class Particle
        {
            public int k; public double x, y, vx, vy, life, max, size, seed, delay;
            public uint col; public int flag;
        }

        /// <summary>sim.js: class Particles — el sistema de partículas con
        /// física real. opts: rng COMPARTIDO (hex-sigil pasa el mismo Rng a
        /// sus tres sistemas: el orden de Next() es parte del look).</summary>
        internal sealed class Particles
        {
            public PxMath.Rng R;
            public readonly List<Particle> list = new List<Particle>();
            public double t;
            public Func<double, double> ground;
            public uint[] sparks;
            public uint? haze;

            public Particles(PxMath.Rng rng, Func<double, double> ground = null, uint[] sparks = null, uint? haze = null)
            {
                R = rng ?? new PxMath.Rng(1);
                this.ground = ground ?? (_ => double.PositiveInfinity);
                this.sparks = sparks ?? HOT;
                this.haze = haze;
            }

            /// <summary>sim.js: spawn — delay: segundos hasta aparecer; col 0
            /// deja que FIRE/SMOKE usen sus propias rampas. NOTA: consume
            /// R.next() para la semilla de la partícula.</summary>
            public void Spawn(int k, double x, double y, double vx, double vy, double life,
                uint col = 0xFFFFFFFFu, double size = 1, double delay = 0, int flag = 0) // col default: WHITE (sim.js)
            {
                list.Add(new Particle
                {
                    k = k, x = x, y = y, vx = vx, vy = vy, life = life, max = life,
                    col = col != 0 ? col : 0, size = size, seed = R.Next() * 100, delay = delay, flag = flag,
                });
            }

            public int Count => list.Count;

            /// <summary>sim.js: update(dt) — el bucle de simulación, caso por caso.</summary>
            public void Update(double dt)
            {
                t += dt;
                PxMath.Rng R = this.R; double now = t; // JS: const t = this.t (ya incrementado)
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    Particle p = list[i];
                    if (p.delay > 0) { p.delay -= dt; continue; }
                    p.life -= dt;
                    if (p.life <= 0)
                    {
                        if (p.k == FIRE && (p.flag & PF_SMOKES) != 0)
                        {
                            bool big = (p.flag & PF_BIG) != 0;
                            Spawn(SMOKE, p.x, p.y, p.vx * 0.6, p.vy * 0.6 - 6,
                                big ? R.Range(1.3, 2.3) : R.Range(0.8, 1.4), 0,
                                p.size * (big ? 0.85 : 0.7), 0, big ? PF_BIG : 0);
                        }
                        list.RemoveAt(i);
                        continue;
                    }
                    switch (p.k)
                    {
                        case FIRE:
                            p.vy -= 95 * dt;
                            p.vx += Math.Sin(now * 17 + p.seed) * 30 * dt;
                            p.vx -= p.vx * 5 * dt;
                            p.vy -= p.vy * 3.2 * dt;
                            break;
                        case GRIT:
                        {
                            p.vy += GRAVITY * dt;
                            p.vx -= p.vx * 0.8 * dt;
                            double g = ground(p.x), ny = p.y + p.vy * dt;
                            p.x += p.vx * dt;
                            if (p.vy > 0 && p.y <= g && ny >= g)
                            {
                                p.y = g - 0.25;
                                if (p.vy > 130 && R.Chance(0.4)) { p.vy = -p.vy * 0.25; p.vx = p.vx * 0.5 + R.Range(-10, 10); }
                                else { p.vx = 0; p.vy = 0; p.life = Math.Min(p.life, 0.5); }
                            }
                            else p.y = ny;
                            continue;
                        }
                        case BRAND:
                        {
                            p.vy += GRAVITY * 0.8 * dt;
                            p.vx -= p.vx * 0.9 * dt;
                            if (R.Chance(0.9 * Math.Min(1, dt * 60)))
                                Spawn(FIRE, p.x, p.y, p.vx * 0.1 + R.Range(-7, 7), p.vy * 0.1 - 10, R.Range(0.16, 0.32),
                                    0, R.Range(0.9, 1.7) * (0.5 + (p.life / p.max) * 0.6), 0, R.Chance(0.4) ? PF_SMOKES : 0);
                            double g = ground(p.x), ny = p.y + p.vy * dt;
                            p.x += p.vx * dt;
                            if (p.vy > 0 && p.y <= g && ny >= g) { list.RemoveAt(i); continue; }
                            p.y = ny;
                            continue;
                        }
                        case SPARK:
                        {
                            p.vy += GRAVITY * 0.45 * dt;
                            p.vx -= p.vx * 2.2 * dt;
                            p.vy -= p.vy * 2.2 * dt;
                            double g = ground(p.x), ny = p.y + p.vy * dt;
                            p.x += p.vx * dt;
                            if (p.vy > 0 && p.y <= g && ny >= g) { p.vy = -p.vy * 0.45; p.vx *= 0.7; p.y = g - 0.25; }
                            else p.y = ny;
                            continue;
                        }
                        case STREAK: p.vx += p.vx * 3 * dt; p.vy += p.vy * 3 * dt; break;
                        case FLAKE:
                            p.vx -= p.vx * 2.2 * dt;
                            p.vy -= p.vy * 2.2 * dt - 35 * dt;
                            p.x += Math.Sin(t * 2.5 + p.seed) * 7 * dt; // JS usa this.t: mismo valor
                            break;
                        case EMBER:
                            p.vy += (-30 + Math.Sin(now * 3 + p.seed) * 20) * dt;
                            p.vx += Math.Cos(now * 2.3 + p.seed) * 45 * dt;
                            p.vx -= p.vx * 1.6 * dt;
                            p.vy -= p.vy * 1.6 * dt;
                            break;
                        case SMOKE:
                        {
                            bool big = (p.flag & PF_BIG) != 0;
                            p.vy -= (big ? 23 : 16) * dt;
                            p.vx += Math.Sin(now * 1.3 + p.seed) * 8 * dt;
                            p.vx -= p.vx * 1.6 * dt;
                            p.vy -= p.vy * 1.2 * dt;
                            break;
                        }
                        case MOTE: p.vx -= p.vx * 0.6 * dt; p.vy -= p.vy * 0.6 * dt; break;
                        default:
                        {
                            double drag = Soft(p.k) ? 4 : 1.6;
                            p.vx -= p.vx * drag * dt;
                            p.vy -= p.vy * drag * dt + 5 * dt;
                            break;
                        }
                    }
                    p.x += p.vx * dt;
                    p.y += p.vy * dt;
                }
            }

            /// <summary>sim.js: draw(p, layer) — null dibuja todo.</summary>
            public void Draw(PxPainter p, string layer = null)
            {
                foreach (Particle q in list)
                {
                    if (q.delay > 0) continue;
                    if (layer != null && (Soft(q.k) || (q.flag & PF_BACK) != 0 ? "back" : "front") != layer) continue;
                    double k = q.life / q.max, x = q.x, y = q.y;
                    double sp = Math.Sqrt(q.vx * q.vx + q.vy * q.vy); if (sp == 0) sp = 1;
                    double ux = q.vx / sp, uy = q.vy / sp;
                    switch (q.k)
                    {
                        case SPARK:
                        {
                            uint[] S = sparks;
                            uint head = k > 0.6 ? S[0] : k > 0.3 ? S[1] : S[2];
                            double len = Math.Min(9, 1 + sp * 0.022);
                            p.Line(x - ux * len, y - uy * len, x - ux * len * 0.4, y - uy * len * 0.4, k > 0.5 ? head : S[S.Length - 1]);
                            p.Line(x - ux * len * 0.45, y - uy * len * 0.45, x, y, head);
                            p.Glow(x, y, 3, head, 0.45 * k);
                            break;
                        }
                        case MOTE:
                        {
                            double len = Math.Min(16, 0.5 + sp * 0.026) * q.size, a = k < 0.35 ? k / 0.35 : 1;
                            p.Line(x - ux * len, y - uy * len, x - ux * len * 0.35, y - uy * len * 0.35, PxColor.Scale(q.col, 0.55), a);
                            p.Line(x - ux * len * 0.4, y - uy * len * 0.4, x, y, q.col, a);
                            p.Glow(x, y, 2.5 + q.size, q.col, 0.4 * k);
                            break;
                        }
                        case STREAK:
                        {
                            double L = PxMath.Clamp(sp / 30, 1, 7);
                            for (double d = L; d >= 0; d -= 0.5)
                                p.Dot(x - ux * d, y - uy * d,
                                    d < 1 ? PxColor.WHITE : d < L * 0.5 ? q.col : PxColor.Scale(q.col, 0.6),
                                    d < L * 0.5 ? 1 : 1.2 - d / L);
                            p.Glow(x, y, 4, q.col, 0.4);
                            break;
                        }
                        case FLAKE:
                        {
                            double a = Math.Min(1, k * 3);
                            p.Dot(x, y, q.col, a);
                            if (q.size > 1.3)
                                foreach (var v in CROSS) p.Dot(x + v.Item1, y + v.Item2, PxColor.Scale(q.col, 0.8), a * 0.8);
                            break;
                        }
                        case VAPOUR:
                        {
                            double r = q.size * (0.6 + (1 - k) * 0.9);
                            p.Disc(x, y, r, q.col, Math.Pow(k, 1.3) * 0.45);
                            if (haze.HasValue) p.Glow(x, y, r * 1.8, haze.Value, 0.16 * k);
                            break;
                        }
                        case DUST: p.Disc(x, y, q.size * (0.6 + (1 - k) * 0.9), q.col, Math.Pow(k, 1.3) * 0.7); break;
                        case SMOKE:
                        {
                            double age = 1 - k; bool big = (q.flag & PF_BIG) != 0;
                            double r = q.size * (0.65 + Math.Min(1, age * 2.5) * 0.45 + age * (big ? 0.5 : 0.4));
                            double a = Math.Min(1, age * 12) * Math.Min(1, k * 2.4) * (big ? 0.7 : 0.58);
                            uint c = q.col != 0 ? PxColor.Mix(q.col, RampAt(SMOKE_RAMP, age), age) : RampAt(SMOKE_RAMP, age * 1.05);
                            p.Disc(x, y, r, c, a * 0.45);
                            p.Disc(x + r * 0.05, y + r * 0.06, r * 0.74, c, a * 0.62);
                            if (age < 0.5) p.Disc(x - r * 0.22, y - r * 0.3, r * 0.5, PxColor.Mix(c, PxColor.WHITE, 0.12), a * 0.4 * (1 - age * 2));
                            break;
                        }
                        case FIRE:
                        {
                            double age = 1 - k;
                            double heat = Math.Min(0.999, Math.Pow(age, 0.62) + (q.seed % 1) * 0.1);
                            uint[] F = FIRE_RAMP; int n = F.Length;
                            int idx = Math.Min(n - 2, (int)Math.Floor(heat * (n - 1)));
                            double r = q.size * (0.55 + 0.45 * Math.Min(1, age * 7)) * (1 - age * 0.55);
                            uint c = F[idx];
                            if (idx >= n - 2) p.Disc(x, y, r, c, Math.Min(1, k * 3) * 0.85);
                            else
                            {
                                p.Circle(x, y, r, F[Math.Min(n - 1, idx + 2)]);
                                p.Circle(x - r * 0.16, y - r * 0.22, r * 0.78, c);
                                if (r > 1.6) p.Circle(x, y - r * 0.9, r * 0.42, c);
                                if (idx <= 3 && r > 1.8) p.Circle(x - r * 0.28, y - r * 0.34, r * 0.36, F[Math.Max(0, idx - 1)]);
                                if (idx < 6) p.Glow(x, y, r * 2.4 + 2, c, 0.18 * k);
                            }
                            break;
                        }
                        case BRAND:
                            p.Circle(x, y, 1.6, HOT[3]);
                            p.Px(x, y, Math.Sin(this.t * 40 + q.seed) > 0 ? HOT[0] : HOT[1]);
                            p.Px(x + 1, y, HOT[1]);
                            p.Glow(x, y, 7, HOT[2], 0.45);
                            break;
                        case GRIT:
                            if (k > 0.25 || k * 4 > 0.5)
                            {
                                p.Px(x, y, q.col);
                                if (q.size >= 2) { p.Px(x + 1, y, q.col); p.Px(x, y + 1, PxColor.Scale(q.col, 0.8)); p.Px(x + 1, y + 1, PxColor.Scale(q.col, 0.7)); }
                            }
                            break;
                        case EMBER:
                        {
                            uint c = k > 0.6 ? sparks[0] : k > 0.3 ? sparks[1] : sparks[2];
                            if (k < 0.25 && Math.Sin(this.t * 40 + q.seed) < 0) break;
                            p.Px(x, y, c);
                            if (q.size > 1.5) p.Px(x, y + 1, sparks[sparks.Length - 1]);
                            p.Glow(x, y, 3 + q.size, c, 0.35 * k);
                            break;
                        }
                        default: // STAR: una cruz que titila
                        {
                            double tw = 0.6 + 0.4 * Math.Sin(this.t * (8 + (q.seed % 5)) + q.seed);
                            if (q.size < 1)
                            {
                                if (tw * k > 0.25) p.Px(x, y, tw > 0.8 ? PxColor.WHITE : q.col);
                                p.Glow(x, y, 3, q.col, 0.4 * k * tw);
                                break;
                            }
                            Sparkle(p, x, y, q.size * tw * Math.Min(1, k * 3), q.col, PxColor.Scale(q.col, 0.75), 0.5 * k * tw);
                            break;
                        }
                    }
                }
            }
        }

        // sim.js: B — tipos de debris
        public const int SHARD = 0, CHUNK = 1, DRIP = 2;

        internal sealed class Bit
        {
            public int kind = SHARD; public double x, y, vx, vy, a, va, s = 1, life = 1, max;
            public uint c = PxColor.WHITE; public uint[] ramp = null;
            public double g = 1, drag, bounce; public bool rest;
        }

        /// <summary>sim.js: class Bits — debris rígido que cae, rebota y se asienta.</summary>
        internal sealed class Bits
        {
            public readonly List<Bit> list = new List<Bit>();
            public Func<double, double> ground;

            public Bits(Func<double, double> ground = null) => this.ground = ground ?? (_ => double.PositiveInfinity);

            public void Add(Bit b) { b.max = b.life; list.Add(b); }
            public int Count => list.Count;

            public void Update(double dt)
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    Bit b = list[i];
                    b.life -= dt;
                    if (b.life <= 0) { list.RemoveAt(i); continue; }
                    if (b.rest) continue;
                    b.vy += GRAVITY * b.g * dt;
                    b.vx -= b.vx * b.drag * dt;
                    b.vy -= b.vy * b.drag * dt;
                    b.x += b.vx * dt;
                    b.y += b.vy * dt;
                    b.a += b.va * dt;
                    double floor = ground(b.x), sp = b.vy;
                    if (b.g <= 0 || b.y < floor || sp <= 0) continue;
                    b.y = floor - 0.01;
                    if (b.kind == DRIP) b.life = 0;
                    else if (b.bounce > 0 && sp > 70) { b.vy = -sp * b.bounce; b.vx *= 0.7; b.va *= 0.6; }
                    else { b.rest = true; b.vx = 0; b.vy = 0; }
                }
            }

            static readonly uint[] CHUNK_RAMP = PxColor.Ramp("#1c2030", "#3a4a66", "#6e86a8", "#a8c0dc", "#e8f2ff");

            public void Draw(PxPainter p)
            {
                foreach (Bit b in list)
                {
                    double a = Math.Min(1, b.life / 0.4), ca = Math.Cos(b.a), sa = Math.Sin(b.a);
                    if (b.kind == SHARD)
                    {
                        for (double d = -b.s / 2; d <= b.s / 2; d += 0.5)
                            p.Dot(b.x + ca * d, b.y + sa * d, d > b.s / 2 - 0.6 ? PxColor.WHITE : b.c, a);
                    }
                    else if (b.kind == DRIP)
                    {
                        p.Dot(b.x, b.y, b.c, a);
                        p.Dot(b.x, b.y - 1, b.c, a * 0.5);
                    }
                    else
                    {
                        uint[] R = b.ramp ?? CHUNK_RAMP; int n = R.Length;
                        int x = JsRound(b.x), y = JsRound(b.y), r = (int)Math.Ceiling(b.s) + 1;
                        for (int py = -r; py <= r; py++)
                            for (int px = -r; px <= r; px++)
                            {
                                double u = (px + 0.5) * ca + (py + 0.5) * sa, v = -(px + 0.5) * sa + (py + 0.5) * ca;
                                double d = Math.Abs(u) / b.s + Math.Abs(v) / (b.s * 0.62);
                                if (d > 1.3) continue;
                                p.Dot(x + px, y + py,
                                    d > 1 ? R[0] : v < 0 ? (u < 0 ? R[n - 1] : R[n - 2]) : u < 0 ? R[n - 3] : R[Math.Max(1, n - 4)], a);
                            }
                    }
                }
            }
        }

        // sim.js: CROSS/DIAG de sparkle
        internal static readonly (int, int)[] CROSS = { (1, 0), (-1, 0), (0, 1), (0, -1) };
        internal static readonly (int, int)[] DIAG = { (1, 1), (1, -1), (-1, 1), (-1, -1) };

        /// <summary>sim.js: sparkle — «A star glint: arms of length arm in
        /// color c with tips in tip, a white heart, light.» La PLUMA del
        /// hex-sigil es un sparkle.</summary>
        public static void Sparkle(PxPainter p, double x, double y, double arm, uint c, uint tip, double k = 0.6)
        {
            int n = JsRound(arm);
            if (n < 1) return;
            int X = (int)Math.Floor(x), Y = (int)Math.Floor(y);
            for (int d = 1; d <= n; d++)
            {
                uint cc = d > n * 0.6 ? tip : c;
                foreach (var (dx, dy) in CROSS) p.Px(X + dx * d, Y + dy * d, cc);
            }
            if (n >= 3) foreach (var (dx, dy) in DIAG) p.Px(X + dx, Y + dy, c);
            if (n >= 6) foreach (var (dx, dy) in DIAG) p.Px(X + dx * 2, Y + dy * 2, tip);
            p.Px(X, Y, PxColor.WHITE);
            p.Glow(x, y, 4 + n * 2, c, k);
        }

        /// <summary>Math.round de JS (mitad hacia +∞), NO el banker's de C#.</summary>
        internal static int JsRound(double v) => (int)Math.Floor(v + 0.5);
    }

    // ==================================================================
    //  src/fx/shadow/_lib.js — «Shared bits for the shadow effects: the
    //  void palette, light that black shapes cut, smoke lit from one
    //  side, violet flame, obsidian slivers, ink drops.»
    //  (La llama Puffs y el pull attract vienen del black-hole
    //  open-source de Sprite Fusion / Hugo Duprez, MIT.)
    // ==================================================================
    internal static class PxShadowLib
    {
        /// <summary>_lib.js: VOID — «Dark magic, dark to bright: void black,
        /// deep violet, a white-lilac edge.» La paleta de los 3 efectos.</summary>
        public static readonly uint[] VOID = PxColor.Ramp(
            "#06040b", "#150c26", "#2a1150", "#461a84", "#6f2cc0", "#9d5cec", "#cfa6ff", "#f6eaff");

        /// <summary>_lib.js: unlight — «Take the light back off every pixel in
        /// a local box where dark(x, y) is true… so black shapes stay black
        /// under every glow, spark and ring drawn before.» EL ECLIPSE VIVE
        /// DE ESTO: la luna negra corta la luz de su propia corona.</summary>
        public static void Unlight(PxPainter p, double x0, double y0, double x1, double y1,
            Func<double, double, bool> dark, double keep = 0)
        {
            uint[] L = p.L; int w = p.w, ox = p.ox, oy = p.oy;
            p.Each(x0, y0, x1, y1, (x, y, sx, sy) =>
            {
                if (!dark(x, y)) return;
                int i = (sy - oy) * w + (sx - ox);
                uint d = L[i];
                if (d == 0) return;
                if (keep <= 0) L[i] = 0;
                else L[i] = 0xFF000000u
                    | ((uint)(int)(((d >> 16) & 255) * keep) << 16)
                    | ((uint)(int)(((d >> 8) & 255) * keep) << 8)
                    | (uint)(int)((d & 255) * keep);
            });
        }

        /// <summary>_lib.js: annulus — las celdas del anillo r0..r1, fila a
        /// fila, saltando el agujero (el truco del px mutado se portó tal
        /// cual: es lo que mantiene la banda barata).</summary>
        static void Annulus(double x, double y, double r0, double r1, Action<double, double, double> fn)
        {
            double r02 = r0 > 0 ? r0 * r0 : -1, r12 = r1 * r1;
            for (int py = (int)Math.Floor(y - r1); py <= (int)Math.Ceiling(y + r1); py++)
            {
                double cy = py + 0.5, dy = cy - y, dy2 = dy * dy;
                if (dy2 >= r12) continue;
                double xo = Math.Sqrt(r12 - dy2), xi = dy2 < r02 ? Math.Sqrt(r02 - dy2) : 0;
                for (int px = (int)Math.Floor(x - xo), x1 = (int)Math.Ceiling(x + xo); px <= x1; px++)
                {
                    double cx = px + 0.5, dx = cx - x;
                    if (xi > 0 && dx > -xi && dx < xi)
                    {
                        px = Math.Max(px, (int)Math.Floor(x + xi) - 1);
                        continue;
                    }
                    double d2 = dx * dx + dy2;
                    if (d2 < r12 && d2 >= r02) fn(cx, cy, Math.Sqrt(d2));
                }
            }
        }

        /// <summary>_lib.js: ringFast — un anillo que solo visita SUS píxeles
        /// (el anillo de choque del destello del hex-sigil).</summary>
        public static void RingFast(PxPainter p, double x, double y, double r, double w, uint c, double a = 1)
        {
            if (r <= 0) return;
            double r0 = Math.Max(0, r - w);
            Annulus(x, y, r0, r, (cx, cy, d) => p.Dot(cx, cy, c, a));
        }

        /// <summary>_lib.js: halo — «A ring of light hugging a black disc: k
        /// at radius r0, falling to nothing at r1, and none inside r0.»</summary>
        public static void Halo(PxPainter p, double x, double y, double r0, double r1, uint c, double k)
        {
            if (r1 <= r0 || k <= 0.004) return;
            double w = r1 - r0;
            Annulus(x, y, Math.Max(0, r0), r1, (cx, cy, d) =>
            {
                double f = 1 - (d - r0) / w;
                p.Light(cx, cy, c, f * f * k);
            });
        }

        // _lib.js: PK — tipos de puff
        public const int PK_FLAME = 0, PK_SOOT = 1;

        /// <summary>_lib.js: la «luz» que encara el hollín (su rim iluminado).</summary>
        internal sealed class PuffLight
        {
            public double x, y, k, reach;
        }

        internal sealed class Puff
        {
            public int k; public double x, y, vx, vy, life, max, size, delay, seed;
            public bool smokes;
        }

        /// <summary>_lib.js: class Puffs — «Violet flame and dark smoke.» La
        /// llama que enfría por la rampa y el hollín encendido del lado que
        /// mira a la luz (adaptados del black-hole de Sprite Fusion).</summary>
        internal sealed class Puffs
        {
            public PxMath.Rng R;
            public readonly List<Puff> list = new List<Puff>();
            public double t;
            public readonly uint[] F; public readonly uint[] soot; public readonly uint cap; public readonly uint[] rim;
            public Func<double, double> floor;

            public Puffs(PxMath.Rng rng, uint[] flame, uint[] soot, uint cap, uint[] rim, Func<double, double> floor = null)
            {
                R = rng; F = flame; this.soot = soot; this.cap = cap; this.rim = rim;
                this.floor = floor;
            }

            public void Spawn(int k, double x, double y, double vx, double vy, double life, double size, double delay = 0, bool smokes = false)
            {
                list.Add(new Puff { k = k, x = x, y = y, vx = vx, vy = vy, life = life, max = life, size = size, delay = delay, smokes = smokes, seed = R.Next() * 100 });
            }

            public int Count => list.Count;

            public void Update(double dt)
            {
                t += dt;
                double now = t; // JS: const t = this.t
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    Puff q = list[i];
                    if (q.delay > 0) { q.delay -= dt; continue; }
                    q.life -= dt;
                    if (q.life <= 0)
                    {
                        if (q.k == PK_FLAME && q.smokes)
                            Spawn(PK_SOOT, q.x, q.y - 1, q.vx * 0.5, q.vy * 0.5 - 6, R.Range(0.9, 1.6), q.size * 1.25);
                        list.RemoveAt(i);
                        continue;
                    }
                    if (q.k == PK_FLAME)
                    {
                        q.vy -= 95 * dt;
                        q.vx += Math.Sin(now * 17 + q.seed) * 30 * dt;
                        q.vx -= q.vx * 5 * dt;
                        q.vy -= q.vy * 3.2 * dt;
                    }
                    else
                    {
                        q.vy -= 18 * dt;
                        q.vx += Math.Sin(now * 1.3 + q.seed) * 8 * dt;
                        q.vx -= q.vx * 1.6 * dt;
                        q.vy -= q.vy * 1.3 * dt;
                    }
                    q.x += q.vx * dt;
                    q.y += q.vy * dt;
                    // «smoke rolls along the floor rather than through it»
                    if (floor != null && q.k == PK_SOOT)
                    {
                        double f = floor(q.x) - q.size * 0.6;
                        if (q.y > f) { q.y = f; q.vy = Math.Min(0, q.vy); q.vx *= 1 - 2 * dt; }
                    }
                }
            }

            public void Draw(PxPainter p, int kind, PuffLight light = null)
            {
                uint[] F = this.F; int n = F.Length;
                foreach (Puff q in list)
                {
                    if (q.delay > 0 || q.k != kind) continue;
                    double k = q.life / q.max, age = 1 - k, x = q.x, y = q.y;
                    if (q.k == PK_FLAME)
                    {
                        // «a ball of flame: a darker rim, a lit core up and left, a tongue on top, a hot heart while young»
                        double heat = Math.Min(0.999, Math.Pow(age, 0.62) + (q.seed % 1) * 0.1);
                        int idx = Math.Min(n - 2, (int)Math.Floor(heat * (n - 1)));
                        double r = q.size * (0.55 + 0.45 * Math.Min(1, age * 7)) * (1 - age * 0.55);
                        uint c = F[idx];
                        if (idx >= n - 3) p.Disc(x, y, r, c, Math.Min(1, k * 3) * 0.8);
                        else
                        {
                            p.Circle(x, y, r, F[Math.Min(n - 1, idx + 2)]);
                            p.Circle(x - r * 0.16, y - r * 0.22, r * 0.78, c);
                            if (r > 1.6) p.Circle(x, y - r * 0.9, r * 0.42, c);
                            if (idx <= 3 && r > 1.8) p.Circle(x - r * 0.28, y - r * 0.34, r * 0.36, F[Math.Max(0, idx - 1)]);
                            if (idx < 6) p.Glow(x, y, r * 2.4 + 2, c, 0.2 * k);
                        }
                        continue;
                    }
                    // SOOT: cuerpo blando + núcleo denso + tapa tenue + encendido hacia la luz
                    double rr = q.size * (0.65 + Math.Min(1, age * 2.5) * 0.45 + age * 0.45);
                    double a = Math.Min(1, age * 10) * Math.Min(1, k * 2.2) * 0.72;
                    uint body = PxColor.Mix(soot[0], soot[1], age);
                    double edge = a * 0.5, core = 1 - (1 - a * 0.5) * (1 - a * 0.62);
                    p.Disc(x, y + rr * 0.06, rr, body, (d, ang) => d < 0.72 ? core : edge);
                    double lk = light != null ? light.k : 0;
                    if (lk < 0.5) p.Disc(x - rr * 0.2, y - rr * 0.32, rr * 0.48, cap, a * 0.32 * (1 - age * 0.7) * (1 - lk * 2));
                    if (lk <= 0.02) continue;
                    double lx = light.x - x, ly = light.y - y;
                    double dl = Math.Sqrt(lx * lx + ly * ly); if (dl == 0) dl = 1;
                    double ux = lx / dl, uy = ly / dl;
                    double lit = Math.Min(1, light.k * PxMath.Clamp01(1.3 - dl / light.reach)) * a;
                    if (lit <= 0.02) continue;
                    uint hot = rim[0], warm = rim[1], dim = rim[2];
                    p.Disc(x + ux * rr * 0.26, y + uy * rr * 0.26, rr * 0.72, dim, lit * 0.55);
                    p.Disc(x + ux * rr * 0.5, y + uy * rr * 0.5, rr * 0.46, warm, lit * 0.5);
                    if (rr > 3) p.Disc(x + ux * rr * 0.68, y + uy * rr * 0.68, rr * 0.24, hot, lit * 0.4);
                }
            }
        }
    }

    // ==================================================================
    //  src/core/fx.js: createFx + composite — la INSTANCIA viva de un
    //  efecto y el compositor que junta color+luz en RGBA.
    // ==================================================================

    /// <summary>Las opciones de un efecto (fx.js: resolveOptions — los
    /// defaults del meta con las del llamador encima).</summary>
    internal sealed class PxOptions
    {
        public uint[] Palette = PxShadowLib.VOID; // meta.options.palette de los 3 efectos
        public uint[] Pearl = null;                 // eclipse: meta.options.pearl
        public int Seed = 1;
        public double X = 0, Y = 0;                 // la colocación (el place del arnés: meta.origin)
        public bool Flip = false;
        public double Angle = 0;
        public double Light = 1;                    // opts.light (glow strength)
    }

    /// <summary>El meta de un efecto (su bloque `export const meta` del JS).</summary>
    internal sealed class PxMeta
    {
        public readonly int W, H, Ox, Oy;
        public readonly double Duration;
        public readonly bool Loop;
        public readonly int Fps;
        public readonly int Seed;
        public PxMeta(int w, int h, int ox, int oy, double duration, bool loop, int fps, int seed)
        { W = w; H = h; Ox = ox; Oy = oy; Duration = duration; Loop = loop; Fps = fps; Seed = seed; }
    }

    internal interface IPxFx { void Draw(PxPainter p, double t, PxOptions o); }
    internal interface IPxFxBack { void DrawBack(PxPainter p, double t, PxOptions o); }
    internal interface IPxFxUpdate { void Update(double dt, PxOptions o); }

    /// <summary>fx.js: createFx — «A live instance of an effect module:
    /// { update(dt), draw(frame, layer), stop(), done() }.» Las
    /// simulaciones pisan a 120 Hz o más fino cualquiera que sea el fps
    /// del llamador, «so they look the same everywhere»; draw() coloca
    /// el efecto en su origen y el stop() funde con el FADE del motor.</summary>
    internal sealed class PxFxInstance
    {
        const double FADE = 0.3; // fx.js: const FADE = 0.3
        readonly IPxFx impl;
        public readonly PxMeta meta;
        public readonly PxOptions options;
        double t, stopAt = double.PositiveInfinity;

        public PxFxInstance(IPxFx impl, PxMeta meta, PxOptions options)
        { this.impl = impl; this.meta = meta; this.options = options; }

        public double T => t;

        public void Update(double dt)
        {
            t += dt;
            var u = impl as IPxFxUpdate;
            if (u == null) return;
            // fx.js: «simulations step at 120 Hz or finer whatever the caller's frame rate»
            int n = Math.Max(1, (int)Math.Ceiling(dt * 120 - 1e-9));
            for (int i = 0; i < n; i++) u.Update(dt / n, options);
        }

        public void Draw(PxFrame frame, string layer = null)
        {
            PxPainter p = frame.Painter();
            p.Place(options.X, options.Y, options.Flip, options.Angle);
            p.fade = stopAt == double.PositiveInfinity ? 1 : PxMath.Clamp01(1 - (t - stopAt) / FADE);
            p.lightK = options.Light;
            double D = meta.Duration;
            double tt = meta.Loop ? t % D : Math.Min(t, D);
            if (layer != "front" && impl is IPxFxBack b) { p.layer = "back"; b.DrawBack(p, tt, options); }
            if (layer != "back") { p.layer = "front"; impl.Draw(p, tt, options); }
        }

        /// <summary>fx.js: stop() — «Let it end: loops fade out, one-shots cut short.»</summary>
        public void Stop() { if (stopAt == double.PositiveInfinity) stopAt = t; }

        public bool Done => t - stopAt >= FADE || (!meta.Loop && t >= meta.Duration);
    }

    internal static class PixelFxCore
    {
        /// <summary>fx.js: composite(f, out, bg = null) — «Color over a
        /// background, plus light, into RGBA bytes… Over transparency,
        /// light becomes color with alpha (its brightest channel), which
        /// reads as additive over dark backgrounds.» Es la salida que el
        /// arnés compara píxel a píxel: RGBA directo (no premultiplicado
        /// — la premultiplicación vive en la matemática de la luz).</summary>
        public static void Composite(PxFrame f, byte[] o, double lightK = 1)
        {
            int n = f.w * f.h; uint[] C = f.color, L = f.light;
            for (int i = 0; i < n; i++)
            {
                uint c = C[i], l = L[i] & 0xffffff;
                double r, g, b, a;
                int ca = (int)(c >> 24);
                if (ca == 255) { r = c & 255; g = (c >> 8) & 255; b = (c >> 16) & 255; a = 255; }
                else { r = c & 255; g = (c >> 8) & 255; b = (c >> 16) & 255; a = ca; }
                if (l != 0)
                {
                    double lr = (l & 255) * lightK, lg = ((l >> 8) & 255) * lightK, lb = ((l >> 16) & 255) * lightK;
                    if (a < 255)
                    {
                        // «over transparency: premultiply what's there, add the light, divide by the new alpha»
                        double pa = a / 255.0, pr = r * pa + lr, pg = g * pa + lg, pb = b * pa + lb;
                        double na = Math.Min(1, Math.Max(pa, Math.Max(lr, Math.Max(lg, lb)) / 255));
                        if (na > 0) { r = Math.Min(255, pr / na); g = Math.Min(255, pg / na); b = Math.Min(255, pb / na); a = na * 255; }
                    }
                    else { r = Math.Min(255, r + lr); g = Math.Min(255, g + lg); b = Math.Min(255, b + lb); }
                }
                int j = i * 4;
                o[j] = ClampByte(r); o[j + 1] = ClampByte(g); o[j + 2] = ClampByte(b); o[j + 3] = ClampByte(a);
            }
        }

        // Uint8ClampedArray de JS: clampea a [0,255] y redondea mitad-a-par.
        static byte ClampByte(double v)
        {
            if (double.IsNaN(v)) return 0;
            if (v <= 0) return 0;
            if (v >= 255) return 255;
            return (byte)Math.Round(v); // MidpointRounding.ToEven = ToUint8Clamp
        }
    }

    // ==================================================================
    //  src/fx/shadow/hex-sigil.js — «A burning pen traces a cursed sigil
    //  in the air, its glyphs pop in one by one, then it flares white
    //  and crumbles into falling embers.»
    //  EL ACTO COMPLETO: trazo → glifos → destello → ascuas.
    //  meta: size 168×128, origin [84,112], duration 4.2 (¡un solo
    //  acto!), seed 7, paleta VOID.
    // ==================================================================
    internal sealed class PxHexSigil : IPxFx, IPxFxBack, IPxFxUpdate
    {
        public static readonly PxMeta Meta = new PxMeta(168, 128, 84, 112, 4.2, loop: false, fps: 30, seed: 7);

        // hex-sigil.js: «the sigil's centre and radii; the beats: traced,
        // glyphs pop, holds, flares, crumbles»
        const double CY = -50, RO = 28, RI = 22, RC = 6, GLYPH = 1.62, FLARE = 2.6, BREAK = 2.74;

        internal sealed class PenPt { public double x, y, t; }
        internal sealed class Glyph
        {
            public double x, y, t; public bool popped;
            public List<(int[] a, int[] b)> segs;
        }
        internal sealed class SigLight { public double x, y, r, k, life, max; }

        readonly uint[] C;
        readonly PxMath.Rng R;                 // UN solo Rng COMPARTIDO por ps/embers/puffs (como el JS)
        readonly List<PenPt> S; readonly double SEnd;
        readonly List<Glyph> G;
        readonly PxSim.Particles ps, embers;
        readonly PxShadowLib.Puffs puffs;
        readonly PxSim.Bits bits;
        readonly List<SigLight> lights = new List<SigLight>();
        double t; int pi; bool flared, broke;

        public PxHexSigil(PxOptions o)
        {
            C = PxColor.Fit(o.Palette, 8);
            R = new PxMath.Rng((uint)o.Seed);
            // hex-sigil.js: const floor = () => 0 — el suelo del acto es y = 0 (el origen)
            PxMath.Rng rng = R; Func<double, double> floor = _ => 0;
            var (pts, end) = Strokes();
            S = pts; SEnd = end;
            G = Glyphs(o.Seed);
            ps = new PxSim.Particles(rng, floor, new[] { PxColor.WHITE, C[7], C[6], C[5] });
            embers = new PxSim.Particles(rng, floor, new[] { C[7], C[6], C[5], C[4] });
            puffs = new PxShadowLib.Puffs(rng,
                new[] { PxColor.WHITE, C[7], C[6], C[5], C[4], C[3], C[2], C[1], C[0] },
                new[] { PxColor.Mix(C[3], C[2], 0.5), PxColor.Mix(C[2], C[1], 0.5) },
                PxColor.Mix(C[4], C[2], 0.5),
                new[] { C[6], C[5], C[4] }, floor);
            bits = new PxSim.Bits(floor);
        }

        /// <summary>hex-sigil.js: strokes() — «the strokes, in the order the
        /// pen draws them»: el círculo exterior, la estrella de 7 de UNA
        /// stroke ({7/3}: tres puntas por vuelta), el círculo íntimo, el
        /// circulito del corazón y el nudo; muestreado cada medio píxel
        /// con el instante en que la pluma pasa (las velocidades del JS:
        /// 420/700/550/380/300 px/s y 0.04 s de pausa entre trazos).</summary>
        static (List<PenPt>, double) Strokes()
        {
            var paths = new List<List<double[]>>();
            List<double[]> Circle(double r, double a0)
            {
                int n = (int)Math.Ceiling(r * PxMath.TAU);
                var l = new List<double[]>(n + 1);
                for (int i = 0; i <= n; i++)
                {
                    double a = a0 + (i / (double)n) * PxMath.TAU;
                    l.Add(new[] { Math.Cos(a) * r, CY + Math.Sin(a) * r });
                }
                return l;
            }
            paths.Add(Circle(RO, -Math.PI / 2));
            var star = new List<double[]>();
            for (int i = 0; i <= 7; i++)
            {
                double a = -Math.PI / 2 + ((i * 3) % 7) * (PxMath.TAU / 7);
                star.Add(new[] { Math.Cos(a) * RI, CY + Math.Sin(a) * RI });
            }
            paths.Add(star);
            paths.Add(Circle(RI, -Math.PI / 2));
            paths.Add(Circle(RC, Math.PI / 2));
            paths.Add(new List<double[]>
            {
                new[] { 0.0, CY - 3.5 }, new[] { 2.0, CY }, new[] { 0.0, CY + 3.5 },
                new[] { -2.0, CY }, new[] { 0.0, CY - 3.5 },
            });
            var pts = new List<PenPt>();
            double[] speeds = { 420, 700, 550, 380, 300 };
            double t = 0.12;
            for (int k = 0; k < paths.Count; k++)
            {
                var path = paths[k];
                for (int i = 1; i < path.Count; i++)
                {
                    double x0 = path[i - 1][0], y0 = path[i - 1][1], x1 = path[i][0], y1 = path[i][1];
                    double L = Math.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0));
                    for (double d = 0; d < L; d += 0.5)
                        pts.Add(new PenPt { x = x0 + (x1 - x0) * d / L, y = y0 + (y1 - y0) * d / L, t = t + d / speeds[k] });
                    t += L / speeds[k];
                }
                t += 0.04;
            }
            return (pts, t);
        }

        /// <summary>hex-sigil.js: glyphs() — «seven small glyphs of our own:
        /// a spine and two or three branches on a 3x3 grid of nodes»,
        /// cada uno con su rng(seed, i, 31) (determinista) y su momento
        /// de estallido GLYPH + i·0.07.</summary>
        static List<Glyph> Glyphs(int seed)
        {
            var g = new List<Glyph>(7);
            for (int i = 0; i < 7; i++)
            {
                PxMath.Rng r = PxMath.RngOf(seed, i, 31);
                var segs = new List<(int[], int[])> { (new[] { 1, 0 }, new[] { 1, 2 }) };
                // OJO: en el JS el r.next() del LÍMITE se evalúa en CADA
                // comprobación del bucle — portarlo «optimizado» a una sola
                // llamada desincronizaría el rng compartido de todo el acto.
                for (int k = 0; k < 2 + (r.Next() < 0.5 ? 1 : 0); k++)
                {
                    // EL ORDEN de r.int es parte del look: a[0], a[1], b[0], b[1]
                    int ax = r.Int(0, 2), ay = r.Int(0, 2);
                    int bx = Math.Max(0, Math.Min(2, ax + r.Int(-1, 1)));
                    int by = Math.Max(0, Math.Min(2, ay + r.Int(-1, 1)));
                    if (ax != bx || ay != by) segs.Add((new[] { ax, ay }, new[] { bx, by }));
                }
                double ang = -Math.PI / 2 + (i * PxMath.TAU) / 7 + Math.PI / 7, rr = (RO + RI) / 2;
                g.Add(new Glyph { x = Math.Cos(ang) * rr, y = CY + Math.Sin(ang) * rr, segs = segs, t = GLYPH + i * 0.07 });
            }
            return g;
        }

        PenPt Pen() => S[Math.Min(S.Count - 1, pi)];

        /// <summary>hex-sigil.js: update(dt) — la pluma que escupe chispas y
        /// gotea ascuas, el poder que entra en estelas, los glifos que
        /// estallan, el destello y el desmoronamiento. LITERAL.</summary>
        public void Update(double dt, PxOptions o)
        {
            t += dt;
            while (pi < S.Count - 1 && S[pi + 1].t <= t) pi++;
            if (t > 0.12 && t < SEnd)
            {
                PenPt q = Pen();
                if (R.Chance(dt * 70)) ps.Spawn(PxSim.SPARK, q.x, q.y, R.Range(-70, 70), R.Range(-100, 20), R.Range(0.12, 0.35));
                if (R.Chance(dt * 10)) embers.Spawn(PxSim.EMBER, q.x, q.y, R.Range(-12, 12), R.Range(5, 25), R.Range(0.5, 0.9));
            }
            // «power drawn into the sigil while it's written and held: streaks racing in to its heart»
            if (t > 0.2 && t < FLARE - 0.05 && R.Chance(dt * (8 + 14 * PxMath.Smooth(0.2, FLARE, t))))
            {
                double a = R.Range(0, PxMath.TAU), D0 = R.Range(40, 60), T = R.Range(0.22, 0.32);
                double v = (D0 * 3) / (Math.Exp(3 * T) - 1);
                ps.Spawn(PxSim.STREAK, Math.Cos(a) * D0, CY + Math.Sin(a) * D0, -Math.Cos(a) * v, -Math.Sin(a) * v, T, R.Chance(0.5) ? C[7] : C[6]);
            }
            foreach (Glyph g in G)
                if (!g.popped && t >= g.t)
                {
                    g.popped = true;
                    lights.Add(new SigLight { x = g.x, y = g.y, r = 14, k = 0.7, life = 0.2, max = 0.2 });
                    for (int i = 0; i < 4; i++)
                        ps.Spawn(PxSim.SPARK, g.x, g.y, R.Range(-60, 60), R.Range(-70, 20), R.Range(0.1, 0.25));
                }
            if (t >= FLARE && !flared)
            {
                flared = true;
                lights.Add(new SigLight { x = 0, y = CY, r = 96, k = 1.4, life = 0.45, max = 0.45 });
                lights.Add(new SigLight { x = 0, y = CY, r = 44, k = 1, life = 0.14, max = 0.14 });
                for (int i = 0; i < 24; i++)
                {
                    double a = R.Range(0, PxMath.TAU), sp = R.Range(60, 130);
                    ps.Spawn(PxSim.SPARK, Math.Cos(a) * 13, CY + Math.Sin(a) * 13, Math.Cos(a) * sp, Math.Sin(a) * sp - 30, R.Range(0.2, 0.45));
                }
            }
            if (t >= BREAK && !broke)
            {
                // «it crumbles: every stroke breaks into burning fragments that tumble down and bounce»
                broke = true;
                for (int i = 0; i < S.Count - 1; i += 7)
                {
                    PenPt q = S[i], n = S[i + 1];
                    double an = Math.Atan2(n.y - q.y, n.x - q.x);
                    bits.Add(new PxSim.Bit
                    {
                        kind = PxSim.SHARD, x = q.x, y = q.y,
                        vx = R.Range(-8, 8), vy = R.Range(-12, 6), g = R.Range(0.22, 0.45),
                        drag = 0.6, bounce = 0.25, life = R.Range(0.8, 1.2),
                        a = an, va = R.Range(-3, 3), s = R.Range(2.5, 3.5),
                        c = R.Pick(new[] { C[7], C[6], C[6], C[5] }),
                    });
                    if (i % 27 == 0) embers.Spawn(PxSim.EMBER, q.x, q.y, R.Range(-10, 10), -R.Range(10, 40), R.Range(0.6, 1.1));
                }
                foreach (Glyph g in G)
                    puffs.Spawn(PxShadowLib.PK_SOOT, g.x, g.y, R.Range(-6, 6), -R.Range(8, 18), R.Range(0.7, 1.1), R.Range(3, 5));
                for (int i = 0; i < 8; i++)
                    puffs.Spawn(PxShadowLib.PK_SOOT, R.Range(-20, 20), CY + R.Range(-20, 20), R.Range(-8, 8), -R.Range(6, 16), R.Range(0.8, 1.2), R.Range(3, 6), R.Range(0, 0.15));
            }
            for (int i = lights.Count - 1; i >= 0; i--) { lights[i].life -= dt; if (lights[i].life <= 0) lights.RemoveAt(i); }
            bits.Update(dt);
            ps.Update(dt);
            embers.Update(dt);
            puffs.Update(dt);
        }

        // OJO — ULPS QUE DECIDEN EL ACTO: en el JS el hex-sigil exporta
        // draw(p) y drawBack(p) SIN t: usa el t de la CLAUSURA (el interno,
        // acumulado subpaso a subpaso: 4 sumas de (1/30)/4 por frame), que
        // difiere del t del runner (una sola suma de 1/30) en el último
        // ulp — y con eso el anillo del glifo cruza «age &lt; 0.18» un frame
        // antes y el destello «t &gt;= FLARE» otro distinto. El port usa el
        // t interno (el parámetro tt se ignora a propósito).
        public void DrawBack(PxPainter p, double tt, PxOptions o)
        {
            puffs.Draw(p, PxShadowLib.PK_SOOT, new PxShadowLib.PuffLight { x = 0, y = CY, k = broke ? 0.3 : 0, reach = 50 });
            double k = t < BREAK ? PxMath.Smooth(0.1, 0.6, t) : 1 - PxMath.Smooth(BREAK, BREAK + 0.6, t);
            if (k > 0) p.Glow(0, CY, 52, C[4], 0.3 * k);
        }

        public void Draw(PxPainter p, double tt, PxOptions o)
        {
            if (!broke)
            {
                // «the traced lines: white where the pen just passed, cooling to violet; pulsing as it holds; white in the flare»
                double hold = t > SEnd ? 0.5 + 0.5 * Math.Sin((t - SEnd) * 9) : 0;
                bool white = t >= FLARE;
                foreach (PenPt q in S)
                {
                    if (q.t > t) break;
                    double age = t - q.t;
                    p.Px(q.x, q.y, white ? PxColor.WHITE : age < 0.05 ? PxColor.WHITE : age < 0.15 ? C[7] : age < 0.35 ? C[6] : hold > 0.75 ? C[6] : C[5]);
                }
                if (white)
                    foreach (PenPt q in S)
                        if ((int)(q.x * 2) % 3 == 0) p.Light(q.x, q.y, C[6], 0.5);
                // «glyphs: each pops in white and settles»
                foreach (Glyph g in G)
                {
                    if (t < g.t) continue;
                    double age = t - g.t;
                    uint col = white || age < 0.06 ? PxColor.WHITE : age < 0.2 ? C[7] : hold > 0.75 ? C[7] : C[6];
                    foreach (var (a, b) in g.segs)
                        p.Line(g.x - 2 + a[0] * 2, g.y - 2 + a[1] * 2, g.x - 2 + b[0] * 2, g.y - 2 + b[1] * 2, col);
                    if (age < 0.18) PxShadowLib.RingFast(p, g.x, g.y, 2 + 4 * (age / 0.18), 1, age < 0.09 ? C[7] : C[5]);
                    p.Glow(g.x, g.y, 6, C[5], 0.25);
                }
                // «the pen: a white-hot point with a little cross of light»
                if (t > 0.12 && t < SEnd + 0.05)
                {
                    PenPt q = Pen();
                    PxSim.Sparkle(p, q.x, q.y, 3 + Math.Sin(t * 60), C[7], C[6], 0.6);
                    p.Glow(q.x, q.y, 16, C[6], 0.55);
                }
            }
            // «the flare: a burst ring as it goes white»
            double fu = (t - FLARE) / 0.35, bu = (t - FLARE) / 0.22;
            if (bu > 0 && bu < 1) PxSim.Sparkle(p, 0, CY, 18 * (1 - bu), C[7], C[5], 0.9 * (1 - bu));
            if (fu > 0 && fu < 1)
                PxShadowLib.RingFast(p, 0, CY, RO + 3 + 12 * PxMath.OutCubic(fu), Math.Max(1, 3 * (1 - fu)),
                    fu < 0.4 ? PxColor.WHITE : fu < 0.7 ? C[6] : C[4]);
            bits.Draw(p);
            puffs.Draw(p, PxShadowLib.PK_FLAME);
            ps.Draw(p);
            embers.Draw(p);
            foreach (SigLight l in lights)
            {
                double q = l.life / l.max;
                p.Glow(l.x, l.y, l.r * (0.6 + 0.4 * q), l.k > 1 ? C[5] : C[6], l.k * q * q);
            }
        }
    }

    // ==================================================================
    //  src/fx/shadow/shadow-flames.js — «Inside-out fire on the floor: a
    //  black heart wrapped in violet flame with pale skin and tips,
    //  shedding embers and dark smoke.» (Las lenguas de capas anidadas
    //  vienen del fire de Sprite Fusion / Hugo Duprez, MIT.)
    //  meta: size 104×112, origin [52,102] (el SUELO), duration 2.4
    //  LOOP, seed 12, VOID.
    // ==================================================================
    internal sealed class PxShadowFlames : IPxFx, IPxFxBack
    {
        public static readonly PxMeta Meta = new PxMeta(104, 112, 52, 102, 2.4, loop: true, fps: 30, seed: 12);

        const double D = 2.4;
        const int GX = 22, GY = 60;
        // «tongues: offset, base height, width, seed; the middle tallest»
        static readonly double[][] TONGUES =
        {
            new[] { -11.0, 20.0, 4.2, 1.0 }, new[] { 11.0, 22.0, 4.4, 2.0 },
            new[] { -6.0, 31.0, 5.4, 3.0 }, new[] { 6.0, 29.0, 5.2, 4.0 },
            new[] { 0.0, 40.0, 6.6, 5.0 },
        };
        // «layers from the outside in: width share, height share, which tone (3 is the black heart)»
        static readonly double[][] LAYERS =
        {
            new[] { 1.0, 1.0, 0.0 }, new[] { 0.8, 0.88, 1.0 },
            new[] { 0.54, 0.68, 2.0 }, new[] { 0.32, 0.48, 3.0 },
        };

        /// <summary>shadow-flames.js: lnoise — «noise that comes back to where
        /// it started each loop: a circle through 2D noise, offset by v».
        /// El RUIDO FRACTAL REAL de la columna (noise2 del motor, en double).</summary>
        static double Lnoise(double t, double v, int seed, double speed = 5)
            => PxMath.Noise2(Math.Cos((PxMath.TAU * t) / D) * speed + v, Math.Sin((PxMath.TAU * t) / D) * speed, seed);

        public void DrawBack(PxPainter p, double t, PxOptions o)
        {
            uint[] C = PxColor.Fit(o.Palette, 8);
            int S = o.Seed;
            // «the scorched floor: a dark burn, violet light pooled on it»
            p.Ellipse(0, 0.5, 22, 3.2, (d, _) => d < 0.6 ? C[0] : C[1], (d, _) => d < 0.85 ? 1 : (1 - d) * 6);
            p.Glow(0, -2, 46, C[4], 0.3);
            // «dark smoke climbing off the tips, a lit rim on its underside»
            PxCycle.Run(t, D, 14, S + 1, r => r.Range(1.2, 1.9), (age, L, r) =>
            {
                double u = age / L;
                double x = r.Range(-6, 6) + Math.Sin(u * 4 + r.Range(0, 6)) * 5 * u;
                double y = -30 - u * r.Range(36, 52), rad = 3 + u * 6;
                double a = Math.Min(1, u * 6) * (1 - u) * 0.7;
                p.Disc(x, y, rad, PxColor.Mix(PxColor.Mix(C[3], C[2], 0.5), C[1], u), a);
                p.Disc(x - rad * 0.2, y - rad * 0.3, rad * 0.5, C[3], a * 0.4);
                p.Disc(x, y + rad * 0.45, rad * 0.55, C[5], a * 0.45 * (1 - u));
            });
        }

        public void Draw(PxPainter p, double t, PxOptions o)
        {
            uint[] C = PxColor.Fit(o.Palette, 8);
            int S = o.Seed;
            uint[] TONE = { C[6], C[4], C[2], C[0] };
            // «rasterize the tongues, outermost first, into a grid of tones»
            var grid = new sbyte[GX * 2 * GY];
            for (int i = 0; i < grid.Length; i++) grid[i] = -1;
            void Set(double x, double y, int v)
            {
                int gi = PxSim.JsRound(x) + GX, gj = PxSim.JsRound(y) + GY;
                if (gi >= 0 && gj >= 0 && gi < GX * 2 && gj < GY) grid[gj * GX * 2 + gi] = (sbyte)v;
            }
            // order: |dx| descendente, estable (el sort de JS es estable)
            int[] order = { 0, 1, 2, 3, 4 };
            for (int a = 1; a < order.Length; a++)
                for (int b = a; b > 0 && Math.Abs(TONGUES[order[b - 1]][0]) < Math.Abs(TONGUES[order[b]][0]); b--)
                    (order[b - 1], order[b]) = (order[b], order[b - 1]);
            foreach (int i in order)
            {
                double dx = TONGUES[i][0], H0 = TONGUES[i][1], w = TONGUES[i][2], n = TONGUES[i][3];
                double h = H0 * (0.78 + 0.22 * Lnoise(t, n * 3, (int)n, 4));
                double sway = Lnoise(t, n * 7, (int)n + 9, 3) * 3;
                foreach (double[] L in LAYERS)
                {
                    double lw = L[0], lh = L[1]; int tone = (int)L[2];
                    double Hl = h * lh;
                    for (int r = 0; r <= Hl; r++)
                    {
                        double u = r / Math.Max(1, Hl);
                        double half = w * lw * Math.Pow(1 - u, 0.85) * Math.Min(1, (r + 1.5) / 3.5)
                            + Lnoise(t, r * 0.45 + n, (int)n + 20, 6) * 1.1
                            + Lnoise(t, r * 1.3 + n * 5, (int)n + 50, 9) * 0.6;
                        double cx = dx + sway * u * u * lh + Lnoise(t, r * 0.2 + n * 2, (int)n + 30, 5) * u * 1.4;
                        if (half < 0.3) continue;
                        for (int x = PxSim.JsRound(cx - half); x <= PxSim.JsRound(cx + half); x++)
                        {
                            // «the skin breaks up near the top: pixels flicker out of it»
                            if (tone == 0 && u > 0.45 && Lnoise(t, x * 0.9 + r * 0.7 + n, (int)n + 60, 8) > 0.35) continue;
                            Set(x, -r, tone);
                        }
                    }
                }
            }
            // «draw it: the pale skin whitening to its tips, the black heart pure black»
            for (int j = 0; j < GY; j++)
                for (int i = 0; i < GX * 2; i++)
                {
                    int v = grid[j * GX * 2 + i];
                    if (v < 0) continue;
                    double x = i - GX, y = j - GY;
                    uint c = TONE[v];
                    if (v == 0 && y < -24 && (j > 0 ? grid[(j - 1) * GX * 2 + i] : (sbyte)-1) < 0)
                        c = y < -32 ? PxColor.WHITE : C[7];
                    p.Px(x + 0.5, y + 0.5, c);
                }
            // «licks of pale flame breaking off the tips»
            PxCycle.Run(t, D, 34, S, r => r.Range(0.25, 0.5), (age, L, r) =>
            {
                double u = age / L;
                int k = r.Int(0, 4);
                double dx = TONGUES[k][0], H0 = TONGUES[k][1];
                double x = dx + r.Range(-4, 4) + u * r.Range(-5, 5);
                double y = -H0 * r.Range(0.45, 0.85) - u * r.Range(10, 20);
                double rad = r.Range(1, 2.2) * (1 - u) + 0.3;
                p.Circle(x, y, rad + 0.6, u < 0.5 ? C[6] : C[4]);
                if (rad > 1) p.Circle(x, y + 0.4, rad * 0.5, u < 0.3 ? C[2] : C[4]);
            });
            // «embers drifting up, flickering, cooling to violet»
            PxCycle.Run(t, D, 30, S + 2, r => r.Range(1, 1.8), (age, L, r) =>
            {
                double u = age / L;
                double x = r.Range(-12, 12) + Math.Sin(u * 6 + r.Range(0, 6)) * 5 * u;
                double y = -10 - u * r.Range(40, 70);
                if (u > 0.7 && Math.Sin(age * 40 + x) < 0) return;
                p.Px(x, y, u < 0.3 ? PxColor.WHITE : u < 0.6 ? C[7] : C[6]);
                p.Glow(x, y, 3, C[6], 0.35 * (1 - u));
            });
            // «light: a violet bloom round it, but none on the black heart»
            double fl = 0.85 + 0.15 * Lnoise(t, 0, 77, 6);
            p.Glow(0, -16, 52, C[4], 0.5 * fl);
            p.Glow(0, -12, 26, C[6], 0.34 * fl);
            for (int j = 0; j < GY; j++)
                for (int i = 0; i < GX * 2; i++)
                    if (grid[j * GX * 2 + i] == 3)
                    {
                        int sx = p.CellX(i - GX + 0.5, j - GY + 0.5) - p.ox, sy = p.CellY(i - GX + 0.5, j - GY + 0.5) - p.oy;
                        if (sx >= 0 && sy >= 0 && sx < p.w && sy < p.h) p.L[sy * p.w + sx] = 0;
                    }
        }
    }

    // ==================================================================
    //  src/fx/shadow/eclipse.js — «A black moon over the sun: a breathing
    //  pearl corona with long streamers, a thin white rim, and one
    //  diamond bead blazing on its edge.»
    //  meta: size 160×144, origin [80,72] (EL CENTRO), duration 4 LOOP,
    //  seed 3, VOID + pearl propio.
    // ==================================================================
    internal sealed class PxEclipse : IPxFx, IPxFxBack
    {
        public static readonly PxMeta Meta = new PxMeta(160, 144, 80, 72, 4, loop: true, fps: 30, seed: 3);

        /// <summary>eclipse.js: meta.options.pearl — la rampa perla de la corona.</summary>
        public static readonly uint[] PEARL = PxColor.Ramp("#5a4f8a", "#a99be0", "#e6defa", "#fffaf2");

        // «the moon's radius, the loop, where the bead blazes on the rim»
        const double R = 19, D = 4, BEAD = -0.72;

        static double Breathe(double t, double k, double ph) => Math.Sin((PxMath.TAU * k * t) / D + ph);

        internal sealed class Streamer { public double a, len, curl, k, ph, w; }

        /// <summary>eclipse.js: STREAMS — «streamers: crowded toward the
        /// equator (left and right), longer there, each curving off as it
        /// goes out», construidos con rand(i, 1..6): LA CORONA.</summary>
        static readonly Streamer[] STREAMS = BuildStreams();
        static Streamer[] BuildStreams()
        {
            var s = new Streamer[30];
            for (int i = 0; i < 30; i++)
            {
                double side = i % 2 != 0 ? 0 : Math.PI;
                double spread = (PxMath.Rand(i, 1) - 0.5) * (PxMath.Rand(i, 2) < 0.7 ? 1.3 : 3.1);
                double a = side + spread, eq = Math.Abs(Math.Cos(a));
                s[i] = new Streamer
                {
                    a = a,
                    len = (10 + PxMath.Rand(i, 3) * 28) * (0.45 + 0.75 * eq),
                    curl = (PxMath.Rand(i, 4) - 0.5) * 0.00018,
                    k = 1 + (i % 3),
                    ph = PxMath.Rand(i, 5) * PxMath.TAU,
                    w = PxMath.Rand(i, 6),
                };
            }
            return s;
        }

        public void DrawBack(PxPainter p, double t, PxOptions o)
        {
            uint[] C = PxColor.Fit(o.Palette, 8);
            uint[] Q = PxColor.Fit(o.Pearl ?? PEARL, 4);
            int S = o.Seed;
            // «stars twinkling in the dark sky round it»
            PxCycle.Run(t, D, 26, S, r => r.Range(1.2, 2.6), (age, L, r) =>
            {
                double x = r.Range(-78, 78), y = r.Range(-70, 70);
                if (Math.Sqrt(x * x + y * y) < R + 22) return;
                double k = Math.Sin((Math.PI * age) / L);
                p.Dot(x, y, k > 0.7 ? PxColor.WHITE : Q[1], k * 1.2);
                if (k > 0.85 && r.Next() < 0.4) { p.Dot(x - 1, y, Q[0], 0.8); p.Dot(x + 1, y, Q[0], 0.8); }
            });
            // «the outer corona's light: a wide pearly bloom, breathing»
            double b = 1 + 0.08 * Breathe(t, 1, 0);
            p.Glow(0, 0, 72 * b, C[4], 0.38);
            p.Glow(0, 0, 46 * b, Q[1], 0.34);
            // «streamers: fine rays of pearl, white at the root, thinning to violet, curling as they go out»
            foreach (Streamer s in STREAMS)
            {
                double L = s.len * (1 + 0.18 * Breathe(t, s.k, s.ph));
                for (double d = 4; d <= L; d += 0.5)
                {
                    double u = d / L, a = s.a + s.curl * d * d, r = R + 1 + d;
                    double x = Math.Cos(a) * r, y = Math.Sin(a) * r;
                    p.Dot(x, y, u < 0.3 ? Q[2] : u < 0.65 ? Q[1] : C[5], u < 0.75 ? 1 : (1 - u) * 4);
                }
                p.GlowLine(Math.Cos(s.a) * (R + 4), Math.Sin(s.a) * (R + 4),
                    Math.Cos(s.a) * (R + L * 0.6), Math.Sin(s.a) * (R + L * 0.6), 3, Q[1], 0.12);
            }
        }

        public void Draw(PxPainter p, double t, PxOptions o)
        {
            uint[] C = PxColor.Fit(o.Palette, 8);
            uint[] Q = PxColor.Fit(o.Pearl ?? PEARL, 4);
            double b = 1 + 0.08 * Breathe(t, 1, 0);
            // «the inner corona: a bright collar hugging the moon, finely
            // streaked, fading out through pearl to violet»
            double W = R + 10;
            p.Shade(-W, -W, W, W, (x, y, out_) =>
            {
                double r = Math.Sqrt(x * x + y * y);
                if (r < R - 0.5 || r > R + 9 * b) return 0;
                double a = Math.Atan2(y, x), e = (r - R) / (9 * b);
                double streak = 0.55 + 0.3 * Math.Cos(a * 23 + Math.Sin(a * 5) * 2) + 0.15 * Math.Cos(a * 41 - (PxMath.TAU * t) / D);
                double I = Math.Pow(1 - e, 2.2) * streak * (0.75 + 0.4 * Math.Abs(Math.Cos(a)));
                if (r < R + 0.9) return Q[3];
                if (I < 0.1) return 0;
                if (I > 0.45 && (((int)Math.Floor(x) + (int)Math.Floor(y)) & 1) == 0) out_.k = 0.2 * I;
                return I > 0.72 ? Q[2] : I > 0.42 ? Q[1] : I > 0.22 ? C[5] : C[4];
            });
            // «the moon: black, a whisper of earthshine on its left limb»
            p.Shade(-R, -R, R, R, (x, y, out_) =>
            {
                double r = Math.Sqrt(x * x + y * y);
                if (r >= R - 0.5) return 0;
                return r > R - 2.2 && x < -R * 0.45 ? C[1] : C[0];
            });
            // «the diamond bead: a blazing point on the rim, rays flaring out and breathing»
            double bx = Math.Cos(BEAD) * (R + 0.4), by = Math.Sin(BEAD) * (R + 0.4);
            double k = 0.75 + 0.25 * Breathe(t, 2, 1);
            for (int i = 0; i < 8; i++)
            {
                double a = (i * Math.PI) / 4 + 0.12;
                double len = (i % 2 != 0 ? 7 : 22) * k * (i == 1 || i == 5 ? 1.6 : 1);
                for (double d = 1; d <= len; d += 0.5)
                {
                    double u = d / len, x = bx + Math.Cos(a) * d, y = by + Math.Sin(a) * d;
                    if (x * x + y * y < (R - 0.5) * (R - 0.5)) break;
                    p.Dot(x, y, u < 0.35 ? PxColor.WHITE : u < 0.7 ? Q[2] : Q[1], u < 0.5 ? 1 : (1 - u) * 2.2);
                }
            }
            p.Circle(bx, by, 3 * k, Q[2]);
            p.Circle(bx, by, 2.2 * k, Q[3]);
            p.Circle(bx, by, 1.4, PxColor.WHITE);
            p.Glow(bx, by, 46 * k, Q[2], 0.8 * k);
            p.Glow(bx, by, 14, PxColor.WHITE, 0.8);
            PxShadowLib.Halo(p, 0, 0, R - 0.5, R + 16, Q[2], 0.35 * b);
            PxShadowLib.Unlight(p, -R, -R, R, R, (x, y) => x * x + y * y < (R - 0.6) * (R - 0.6));
        }
    }

    /// <summary>EL PUNTO DE ENTRADA del motor portado: cada arma instancia
    /// su efecto pixelfx con su semilla string→int igual que el JS (aquí
    /// la semilla es el int del meta: seed 7 / 12 / 3, las MISMAS del
    /// motor), colocado en su origen (place(OX, OY) — como el arnés).</summary>
    internal static class PixelFx
    {
        /// <summary>El Sigilo del Eterno = shadow/hex-sigil (el acto completo).</summary>
        public static PxFxInstance HexSigil(int seed = 7)
        {
            var o = new PxOptions { Seed = seed, X = PxHexSigil.Meta.Ox, Y = PxHexSigil.Meta.Oy };
            return new PxFxInstance(new PxHexSigil(o), PxHexSigil.Meta, o);
        }

        /// <summary>Las Llamas de Sombra = shadow/shadow-flames (la columna, loop).</summary>
        public static PxFxInstance ShadowFlames(int seed = 12)
        {
            var o = new PxOptions { Seed = seed, X = PxShadowFlames.Meta.Ox, Y = PxShadowFlames.Meta.Oy };
            return new PxFxInstance(new PxShadowFlames(), PxShadowFlames.Meta, o);
        }

        /// <summary>El Eclipse Vivo = shadow/eclipse (la nova negra de corona, loop).</summary>
        public static PxFxInstance Eclipse(int seed = 3)
        {
            var o = new PxOptions { Seed = seed, Pearl = PxEclipse.PEARL, X = PxEclipse.Meta.Ox, Y = PxEclipse.Meta.Oy };
            return new PxFxInstance(new PxEclipse(), PxEclipse.Meta, o);
        }
    }
}
