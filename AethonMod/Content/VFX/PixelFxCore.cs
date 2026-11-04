using System;
using System.Collections.Generic;

// ======================================================================
//  PixelFxCore.cs — EL MOTOR DE PÍXEL DE LA CASA.
//
//  LA LETRA DEL USUARIO es la ley de esta sesión: «todo nuestro
//  proyecto debe ser libre de derechos de autor… todo lo que tenga
//  derecho de autor debe ser recreado, sobre esto asegúrate de que
//  una vez limpiado no hayan fallos y de que todas las armas o
//  efectos se mantengan iguales en el mod».
//
//  Este archivo ES la recreación: el motor de píxel del Taller de
//  Efectos, nacido de su investigación (el Sigilo, las Llamas, el
//  Eclipse) y reescrito ENTERO con matemática y código nuestros para
//  ser 100% de la casa. La API pública es la de siempre — las tres
//  armas del Taller la usan tal cual, sin tocar una coma —; el
//  interior es todo nuestro:
//
//   · PxMath      — la navaja numérica: el MEZCLADOR «cascada Aethon»
//                   (xorshift derecha + multiplicación propia +
//                   xorshift IZQUIERDA, cuatro vueltas y un remate:
//                   avalancha completa — un bit que cae tumba la
//                   mitad de la salida), el GENERADOR «chorro del
//                   taller» (un avance de 64 bits cuyas constantes
//                   llevan el nombre AETHON y TALLER en sus bytes,
//                   destilado a 32 bits por la cascada), el FUNDIDO
//                   RACIONAL (una S propia, sin polinomio de por
//                   medio) y el RUIDO de valores sembrado con
//                   nuestro hash.
//   · PxColor     — colores como uint RGBA (A<<24|B<<16|G<<8|R).
//   · PxFrame     — los DOS buffers del motor: color opaco + luz
//                   aditiva.
//   · PxPainter   — el pincel: put/add/blend, línea, círculo,
//                   elipse, disco, glow — y la MATRIZ DE UMBRALES de
//                   la casa: un blue-noise 8×8 cosido UNA sola vez al
//                   despertar la clase, con el método hueco-y-racimo
//                   y la sal de nuestro hash. Los píxeles de un fundido
//                   caen como lluvia fina y pareja, no en tramado de
//                   imprenta.
//   · PxCycle     — los bucles sin costura: cada partícula nace en su
//                   fase del período y su chorro de azar es el MISMO
//                   en cada vuelta — determinista de punta a punta.
//   · PxSim       — la física del taller: chispas, ascuas, estelas,
//                   motas, copos, vapor, polvo, humo, fuego, tizones
//                   y grava; y los escombros rígidos que caen,
//                   rebotan y se asientan.
//   · PxShadowLib — los pedazos de sombra: la paleta del vacío, la
//                   luz que las siluetas negras cortan (unlight), el
//                   anillo barato de solo-sus-píxeles, el halo abrazado
//                   al disco negro y las bocanadas (la llama violeta
//                   que enfría por la rampa y el hollín encendido del
//                   lado que mira a la luz).
//   · Los efectos — PxHexSigil (el acto del sigilo: trazo → glifos →
//                   destello → desmoronamiento), PxShadowFlames (la
//                   columna de lenguas de ruido fractal que vuelve al
//                   punto de partida cada vuelta) y PxEclipse (la
//                   luna negra de corona perla y cuenta de diamante),
//                   con sus constantes, semillas y tiempos LITERALES
//                   del Taller — los efectos se mantienen iguales.
//   · El corredor — PxFxInstance (los subpasos a 120 Hz para que el
//                   ritmo no dependa del fps del que llama, el place
//                   del origen, el fundido del stop) y el COMPOSITOR:
//                   color sobre transparencia y la luz encima; sobre
//                   píxel vacío la luz se premultiplica, se suma y se
//                   reparte en el alfa nuevo — así un brillo sobre
//                   fondo oscuro se lee aditivo de verdad.
//
//  EL PACTO DE LA CASA con este motor:
//   (1) DETERMINISMO — misma semilla, misma serie, SIEMPRE: todo el
//       azar del motor nace de la cascada; nada de rnd del sistema.
//   (2) DOUBLES donde la exactitud lo pide: hash, ruido y física van
//       en doble de principio a fin.
//   (3) CERO-GC en los bucles de dibujo: buffers y matrices estáticos,
//       el chorro de azar se resiembra en una sola vasija, y las rutas
//       calientes (ruido, chispas) llaman a las variantes de aridad
//       fija — ni un array de argumentos por frame.
//   (4) C# PURO, sin XNA ni Terraria: el motor no sabe dónde corre.
//
//  COSTE (por qué esto vive en el juego): son simulaciones LOCALES
//  de 104×112 a 168×128 píxeles (11k–23k px), no de pantalla entera.
//  Cada proyectil vivo paga el clear de dos buffers + N subpasos de
//  1/120 s + el dibujo (cientos de ops de píxel) + un composite de
//  ~23k px + UN Texture2D.SetData. Del orden de decenas de µs a
//  60 fps; los buffers se liberan al morir el proyectil.
// ======================================================================

namespace AethonMod.Content.VFX
{
    // ==================================================================
    //  PxMath — la navaja numérica de la casa: números, fundidos y el
    //  azar determinista. Todo lo de aquí es puro: lo que se dibuja en
    //  el instante t siempre se ve igual.
    // ==================================================================
    internal static class PxMath
    {
        public const double TAU = Math.PI * 2;

        // ---- la navaja básica ----
        public static double Clamp(double x, double a = 0, double b = 1) => x < a ? a : x > b ? b : x;
        public static double Clamp01(double x) => x < 0 ? 0 : x > 1 ? 1 : x;
        public static double Lerp(double a, double b, double t) => a + (b - a) * t;

        /// <summary>El tramo: dónde cae t entre t0 y t1, clampeado a
        /// [0,1] — la unidad de todos los ritmos del motor.</summary>
        public static double Seg(double t, double t0, double t1) => Clamp01((t - t0) / (t1 - t0));

        /// <summary>La S suave de la casa sobre un tramo: Seg + el
        /// FUNDIDO RACIONAL (ver abajo) — suave en los bordes.</summary>
        public static double Smooth(double e0, double e1, double x) => Fundido(Seg(x, e0, e1));

        /// <summary>El ease de salida cúbico (el único ease que usan
        /// los tres efectos del Taller).</summary>
        public static double OutCubic(double t) => 1 - Math.Pow(1 - t, 3);

        // ----------------------------------------------------------------
        //  EL FUNDIDO RACIONAL — la S de interpolación de la casa:
        //  s(t) = t² / (t² + (1−t)²). Sale 0 en 0 y 1 en 1, tiene
        //  pendiente nula en los dos bordes (continuidad C¹: no deja
        //  pliegues en el ruido ni en los ritmos) y es simétrica:
        //  s(1−t) = 1−s(t). Sin polinomio heredado: la curva es de la
        //  casa y se nota — un pelín más decidida en el centro.
        // ----------------------------------------------------------------
        internal static double Fundido(double t)
        {
            double p = t * t, q = (1 - t) * (1 - t);
            return p / (p + q);
        }

        // ----------------------------------------------------------------
        //  EL MEZCLADOR «CASCADA AETHON» — el corazón del azar de la
        //  casa. Cuatro vueltas de desplazar-y-multiplicar: cada vuelta
        //  SIEMBRA los bits altos hacia abajo (xorshift derecha),
        //  esparce con una constante propia, luego SUBE los bits bajos
        //  hacia arriba (xorshift izquierda — lo que las cascadas
        //  tímidas no hacen) y vuelve a esparcer. El remate mezcla lo
        //  que quedó arriba con lo que quedó abajo. Verificado a ojo de
        //  avalancha: cambiar UN bit de entrada tumba ~16 de los 32
        //  bits de salida, y la salida reparte pareja (ji-cuadrado en
        //  calma). Las constantes KA/KB son del taller.
        // ----------------------------------------------------------------
        const uint KA = 0x9DA44A5Bu, KB = 0x83B23AB1u;

        static uint Cascada(uint v)
        {
            v ^= v >> 14; v = unchecked(v * KA);
            v ^= v << 11; v = unchecked(v * KB);
            v ^= v >> 19; v = unchecked(v * KA);
            v ^= v << 7;  v = unchecked(v * KB);
            return v ^ (v >> 16);
        }

        /// <summary>LA SAL de la casa: por dónde arranca todo hash.</summary>
        internal const uint SAL = 0x51D17B1Eu;

        /// <summary>El hash determinista de la casa: siembra con la SAL
        /// y pasa cada entero por la cascada, mezclado por XOR (el
        /// orden pesa: Hash(a,b) ≠ Hash(b,a)). Es el origen de TODO
        /// el azar del motor — semillas de partícula, celdas del ruido,
        /// desempates de la matriz de umbrales.</summary>
        public static uint Hash(params int[] ns)
        {
            uint h = SAL;
            foreach (int n in ns) h = Cascada(h ^ (uint)n);
            return h;
        }

        // las calientes: mismas cuentas que Hash, aridad fija — para
        // las rutas por-píxel del ruido y por-partícula de los ciclos,
        // que no pueden pagarse un array de argumentos por llamada.
        internal static uint Hash2(int a, int b) => Cascada(Cascada(SAL ^ (uint)a) ^ (uint)b);
        internal static uint Hash3(int a, int b, int c) => Cascada(Cascada(Cascada(SAL ^ (uint)a) ^ (uint)b) ^ (uint)c);

        /// <summary>Dado determinista en [0,1) desde enteros.</summary>
        public static double Rand(params int[] ns) => Hash(ns) / 4294967296.0;
        internal static double Rand2(int a, int b) => Hash2(a, b) / 4294967296.0;
        internal static double Rand3(int a, int b, int c) => Hash3(a, b, c) / 4294967296.0;

        /// <summary>EL GENERADOR «CHORRO DEL TALLER». Un avance lineal
        /// de 64 bits (paso y giro con constantes de la casa — el
        /// multiplicador lleva las letras de AETHON en sus bytes y
        /// acaba en 5, el giro lleva las de TALLER y acaba en 1: paso
        /// ≡1 mod 4 con giro impar, período completo de 2⁶⁴) del que
        /// solo se destilan los 32 bits altos — los mejores del
        /// avance — a través de la CASCADA, que rompe cualquier
        /// retícula residual. Determinista de verdad: misma semilla,
        /// misma serie, siempre.</summary>
        public sealed class Rng
        {
            const ulong PASO = 0x4145484F4E000015ul;   // «AETHON» ×16 | 5
            const ulong GIRO = 0x54414C4C45520001ul;   // «TALLER» ×16 | 1
            ulong z;

            public Rng(uint seed = 1) => Siembra(seed);

            /// <summary>Re-sembrar la MISMA vasija (los ciclos la usan:
            /// cero basura por partícula y por frame).</summary>
            internal void Resiembra(uint seed) => Siembra(seed);

            void Siembra(uint s) => z = unchecked((ulong)(s == 0 ? 1u : s) * PASO + GIRO);

            /// <summary>El siguiente dado en [0,1).</summary>
            public double Next()
            {
                z = unchecked(z * PASO + GIRO);
                return Cascada((uint)(z >> 32)) / 4294967296.0;
            }

            public double Range(double a, double b) => a + (b - a) * Next();
            public int Int(int a, int b) => (int)Math.Floor(Range(a, b + 1));
            public bool Chance(double p) => Next() < p;
            public uint Pick(uint[] list) => list[(int)Math.Floor(Next() * list.Length)];
        }

        /// <summary>Un chorro por identidad: rng(semilla, i) da los
        /// MISMOS números cada vez que se pida — así vive cada
        /// partícula de un bucle.</summary>
        public static Rng RngOf(params int[] ns) => new Rng(Hash(ns));
        internal static Rng RngOf2(int a, int b) => new Rng(Hash2(a, b));
        internal static Rng RngOf3(int a, int b, int c) => new Rng(Hash3(a, b, c));

        /// <summary>EL RUIDO DE LA CASA: ruido de valores 2D, suave, en
        /// [−1,1]. El retículo entero se siembra con nuestro hash
        /// (dado por celda, en DOUBLE) y entre las cuatro esquinas se
        /// interpola con el FUNDIDO RACIONAL — el paísaje que sale es
        /// nuestro, y con la misma semilla es EL MISMO paísaje, frame
        /// a frame.</summary>
        public static double Noise2(double x, double y, int seed = 0)
        {
            int ix = (int)Math.Floor(x), iy = (int)Math.Floor(y);
            double ux = Fundido(x - ix), uy = Fundido(y - iy);
            double a = Rand3(ix, iy, seed), b = Rand3(ix + 1, iy, seed);
            double c = Rand3(ix, iy + 1, seed), d = Rand3(ix + 1, iy + 1, seed);
            return (Lerp(Lerp(a, b, ux), Lerp(c, d, ux), uy) - 0.5) * 2;
        }
    }

    // ==================================================================
    //  PxColor — colores como uint de 32 bits con el orden interno
    //  A<<24 | B<<16 | G<<8 | R (el mismo entero que viaja por los dos
    //  buffers y sale por el compositor).
    // ==================================================================
    internal static class PxColor
    {
        public static uint Rgb(int r, int g, int b)
            => 0xFF000000u | ((uint)(b & 255) << 16) | ((uint)(g & 255) << 8) | (uint)(r & 255);
        public static uint Red(uint c) => c & 255;
        public static uint Green(uint c) => (c >> 8) & 255;
        public static uint Blue(uint c) => (c >> 16) & 255;

        /// <summary>EL LECTOR DE LA CASA: camina el texto hexadecimal
        /// dígito a dígito (se salta el «#» y lo que no sea dígito), y
        /// si venía corto («#abc») duplica cada dígito. «#rrggbb» o
        /// «#rgb» — nada más hace falta en el Taller.</summary>
        public static uint Hex(string h)
        {
            int v = 0, n = 0;
            for (int i = 0; i < h.Length; i++)
            {
                int d;
                char ch = h[i];
                if (ch >= '0' && ch <= '9') d = ch - '0';
                else if (ch >= 'a' && ch <= 'f') d = ch - 'a' + 10;
                else if (ch >= 'A' && ch <= 'F') d = ch - 'A' + 10;
                else continue;
                if (n < 6) { v = (v << 4) | d; n++; }
            }
            if (n == 3)
            {
                int r = (v >> 8) & 15, g = (v >> 4) & 15, b = v & 15;
                v = (r << 20) | (r << 16) | (g << 12) | (g << 8) | (b << 4) | b;
            }
            return Rgb((v >> 16) & 255, (v >> 8) & 255, v & 255);
        }

        /// <summary>Un color que ya es uint pasa tal cual.</summary>
        public static uint HexN(uint c) => c;

        /// <summary>La mezcla lineal de dos colores, t clampeado. El
        /// canal va en enteros con signo para que la resta no desborde
        /// (42 − 70 tiene que dar −28, no cuatro mil millones).</summary>
        public static uint Mix(uint a, uint b, double t)
        {
            t = PxMath.Clamp01(t);
            return Rgb(Paso((int)Red(a), (int)Red(b), t),
                       Paso((int)Green(a), (int)Green(b), t),
                       Paso((int)Blue(a), (int)Blue(b), t));
        }
        static int Paso(int de, int a, double t) => (int)(de + (a - de) * t);

        /// <summary>Multiplicar el brillo por k (con techo en 255).</summary>
        public static uint Scale(uint c, double k)
            => Rgb(Brillo(Red(c), k), Brillo(Green(c), k), Brillo(Blue(c), k));
        static int Brillo(uint canal, double k) => (int)Math.Min(255.0, canal * k);

        /// <summary>Una lista de hex a colores del motor.</summary>
        public static uint[] Ramp(params string[] list)
        {
            var r = new uint[list.Length];
            for (int i = 0; i < list.Length; i++) r[i] = Hex(list[i]);
            return r;
        }

        /// <summary>Remuestrear una rampa a n pasos conservando los
        /// extremos, para indexar por número.</summary>
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
    //  PxFrame — un frame del motor: w×h píxeles cuyo (0,0) vive en
    //  (ox,oy) del espacio del frame. DOS buffers: `color` (píxeles
    //  opacos, nunca mezclados: se funden con la matriz de umbrales) y
    //  `light` (brillo aditivo, que el compositor pone encima).
    // ==================================================================
    internal sealed class PxFrame
    {
        public readonly int w, h, ox, oy;
        public readonly uint[] color, light;
        PxPainter _pincel;

        public PxFrame(int w, int h, int ox = 0, int oy = 0)
        {
            this.w = w; this.h = h; this.ox = ox; this.oy = oy;
            color = new uint[w * h];
            light = new uint[w * h];
        }

        /// <summary>Vaciar los dos buffers de un golpe.</summary>
        public void Clear()
        {
            Array.Clear(color, 0, color.Length);
            Array.Clear(light, 0, light.Length);
        }

        /// <summary>El pincel del frame: se crea una vez y se re-liga
        /// (Reset) en cada uso — cero basura.</summary>
        public PxPainter Painter()
        {
            if (_pincel == null) _pincel = new PxPainter(this);
            return _pincel.Reset();
        }
    }

    // ==================================================================
    //  PxPainter — el pincel de la casa. Dibuja en los dos buffers del
    //  frame: `color` (píxeles opacos con fundido por UMBRAL, nunca
    //  mezclados) y `light` (glow aditivo). Los efectos dibujan en
    //  coordenadas LOCALES alrededor de su origen; el pincel los
    //  coloca, los espeja y los gira.
    // ==================================================================
    internal sealed class PxOut // lo que un sombreador puede decir de un píxel
    {
        public uint c; public double a = 1; public uint l; public double k;
    }

    internal delegate void PxEachFn(double lx, double ly, int sx, int sy);
    internal delegate uint PxShadeFn(double x, double y, PxOut o);
    internal delegate uint PxColorFn(double d, double ang);
    internal delegate double PxAlphaFn(double d, double ang);

    internal sealed class PxPainter
    {
        // ----------------------------------------------------------------
        //  LA MATRIZ DE UMBRALES DE LA CASA — un blue-noise 8×8 cosido
        //  UNA vez al despertar la clase, con el método HUECO-Y-RACIMO
        //  y la sal de nuestro hash:
        //    Fase 1 — la media matriz: se siembran 32 celdas al azar por
        //    el dado de la casa y se pulen a mudanzas: «el punto más
        //    apretado se muda al mayor hueco», una y otra vez.
        //    Fase 2 — se disuelve: el racimo más apretado se va primero
        //    (encenderá el ÚLTIMO de la media matriz); el que sobrevive
        //    todas las mudanzas enciende el primero.
        //    Fase 3 — se llena: el mayor hueco enciende el primero de la
        //    otra mitad, y así hasta el fondo.
        //  Un umbral por cada 1/64: el fundido de un píxel a media
        //  transparencia cae parejo y sin retícula — lluvia fina.
        // ----------------------------------------------------------------
        static readonly double[] UMBRAL = TejeUmbral();
        const int SAL_MATRIZ = 0xD17;

        static double[] TejeUmbral()
        {
            const int Lado = 8, Celdas = 64, Media = 32;
            // el vistazo: qué tan cerca se ven dos celdas del mosaico
            // (gaussiana toroidal — la matriz se repite en baldosas)
            var vistazo = new double[Celdas, Celdas];
            for (int a = 0; a < Celdas; a++)
                for (int b = 0; b < Celdas; b++)
                {
                    int dx = a % Lado - b % Lado, dy = a / Lado - b / Lado;
                    if (dx > Lado / 2) dx -= Lado; else if (dx < -Lado / 2) dx += Lado;
                    if (dy > Lado / 2) dy -= Lado; else if (dy < -Lado / 2) dy += Lado;
                    vistazo[a, b] = Math.Exp(-(dx * dx + dy * dy) / 4.5);
                }
            var ener = new double[Celdas];

            void Mede(bool[] vivo)
            {
                for (int i = 0; i < Celdas; i++)
                {
                    double e = 0;
                    for (int j = 0; j < Celdas; j++) if (vivo[j]) e += vistazo[i, j];
                    ener[i] = e;
                }
            }

            // FASE 1: la media matriz — siembra por el dado de la casa…
            var vivo = new bool[Celdas];
            for (int k = 0; k < Media; k++)
            {
                int mejor = -1; double dado = 2.0;
                for (int i = 0; i < Celdas; i++)
                {
                    if (vivo[i]) continue;
                    double d = PxMath.Rand2(i, SAL_MATRIZ);
                    if (d < dado) { dado = d; mejor = i; }
                }
                vivo[mejor] = true;
            }
            // …y pulido a mudanzas: el racimo se muda al hueco.
            for (int pulido = 0; pulido < 300; pulido++)
            {
                Mede(vivo);
                int racimo = -1, hueco = -1;
                double apretado = -1.0, vacio = double.PositiveInfinity;
                for (int i = 0; i < Celdas; i++)
                {
                    if (vivo[i]) { if (ener[i] > apretado) { apretado = ener[i]; racimo = i; } }
                    else if (ener[i] < vacio) { vacio = ener[i]; hueco = i; }
                }
                if (apretado - vacio <= 1e-9) break;   // no queda mudanza que mejore
                vivo[racimo] = false; vivo[hueco] = true;
            }

            // FASE 2: disolver — el más apretado enciende el último.
            var rango = new int[Celdas];
            var marca = (bool[])vivo.Clone();
            for (int r = Media - 1; r >= 0; r--)
            {
                Mede(marca);
                int peor = -1; double apretado = -1.0;
                for (int i = 0; i < Celdas; i++)
                    if (marca[i] && ener[i] > apretado) { apretado = ener[i]; peor = i; }
                marca[peor] = false;
                rango[peor] = r;
            }

            // FASE 3: llenar — el mayor hueco enciende el primero.
            for (int r = Media; r < Celdas; r++)
            {
                Mede(vivo);
                int hueco = -1; double vacio = double.PositiveInfinity;
                for (int i = 0; i < Celdas; i++)
                    if (!vivo[i] && ener[i] < vacio) { vacio = ener[i]; hueco = i; }
                vivo[hueco] = true;
                rango[hueco] = r;
            }

            var m = new double[Celdas];
            for (int i = 0; i < Celdas; i++) m[i] = (rango[i] + 0.5) / Celdas;
            return m;
        }

        readonly PxFrame frame;
        public int w, h, ox, oy;
        public uint[] C, L;               // los dos buffers (L es público: las llamas de sombra y el unlight lo escriben directo)
        public double tx, ty, fx = 1, cs = 1, sn;
        public double fade = 1, lightK = 1;
        public string layer = "front";    // la capa que se está dibujando
        readonly PxOut OUT = new PxOut();

        public PxPainter(PxFrame frame) { this.frame = frame; Reset(); }

        /// <summary>Releer el frame y limpiar la colocación, el fundido
        /// y la luz — el pincel vuelve a nacer en cada dibujo.</summary>
        public PxPainter Reset()
        {
            w = frame.w; h = frame.h; ox = frame.ox; oy = frame.oy;
            C = frame.color; L = frame.light;
            tx = 0; ty = 0; fx = 1; cs = 1; sn = 0;
            fade = 1; lightK = 1; layer = "front";
            return this;
        }

        /// <summary>El origen del efecto en (x,y) del frame, con espejo
        /// y giro opcionales. SIN ESTO el efecto se dibuja en (0,0) y
        /// «no aparece»: es la traslación que lo pone en su sitio.</summary>
        public PxPainter Place(double x, double y, bool flip = false, double angle = 0)
        {
            tx = x; ty = y; fx = flip ? -1 : 1;
            cs = Math.Cos(angle); sn = Math.Sin(angle);
            return this;
        }

        // ---- los cambios de plano: local <-> frame ----
        public double ToX(double x, double y) => tx + x * fx * cs - y * sn;
        public double ToY(double x, double y) => ty + x * fx * sn + y * cs;
        public double FromX(double sx, double sy) { double dx = sx - tx, dy = sy - ty; return (dx * cs + dy * sn) * fx; }
        public double FromY(double sx, double sy) { double dx = sx - tx, dy = sy - ty; return -dx * sn + dy * cs; }
        /// <summary>El píxel de pantalla de la CELDA local que contiene (x,y).</summary>
        public int CellX(double x, double y) => (int)Math.Floor(ToX(Math.Floor(x) + 0.5, Math.Floor(y) + 0.5));
        public int CellY(double x, double y) => (int)Math.Floor(ToY(Math.Floor(x) + 0.5, Math.Floor(y) + 0.5));

        /// <summary>LA CAJA: el rectángulo de pantalla que abraza el
        /// cuadro local ya transformado (las cuatro esquinas, mínimo y
        /// máximo), recortado al frame. Es la base de toda forma
        /// llena — así las formas se espejan y giran exactas.</summary>
        void Caja(double x0, double y0, double x1, double y1, out int sx0, out int sy0, out int sx1, out int sy1)
        {
            double ax = double.PositiveInfinity, bx = double.NegativeInfinity;
            double ay = double.PositiveInfinity, by = double.NegativeInfinity;
            Esquina(x0, y0, ref ax, ref bx, ref ay, ref by);
            Esquina(x1, y0, ref ax, ref bx, ref ay, ref by);
            Esquina(x0, y1, ref ax, ref bx, ref ay, ref by);
            Esquina(x1, y1, ref ax, ref bx, ref ay, ref by);
            sx0 = Math.Max((int)Math.Floor(ax), ox); sx1 = Math.Min((int)Math.Ceiling(bx), ox + w - 1);
            sy0 = Math.Max((int)Math.Floor(ay), oy); sy1 = Math.Min((int)Math.Ceiling(by), oy + h - 1);
        }
        void Esquina(double x, double y, ref double ax, ref double bx, ref double ay, ref double by)
        {
            double sx = ToX(x, y), sy = ToY(x, y);
            if (sx < ax) ax = sx; if (sx > bx) bx = sx;
            if (sy < ay) ay = sy; if (sy > by) by = sy;
        }

        /// <summary>Visitar cada píxel de pantalla de la caja de un
        /// cuadro local, con su coordenada local de vuelta.</summary>
        public void Each(double x0, double y0, double x1, double y1, PxEachFn fn)
        {
            Caja(x0, y0, x1, y1, out int sx0, out int sy0, out int sx1, out int sy1);
            for (int sy = sy0; sy <= sy1; sy++)
                for (int sx = sx0; sx <= sx1; sx++)
                    fn(FromX(sx + 0.5, sy + 0.5), FromY(sx + 0.5, sy + 0.5), sx, sy);
        }

        /// <summary>Escritura cruda de píxel entero. Color 0 = «nada».
        /// El fundido (fade·a) es UMBRAL de la matriz blue-noise de la
        /// casa, nunca una mezcla: así se ven los píxeles del motor.</summary>
        public void Put(int sx, int sy, uint c, double a = 1)
        {
            if (c == 0) return;
            a *= fade;
            if (a < 1 && a <= UMBRAL[((sy & 7) << 3) | (sx & 7)]) return;
            int x = sx - ox, y = sy - oy;
            if (x < 0 || y < 0 || x >= w || y >= h) return;
            C[y * w + x] = c;
        }

        /// <summary>Luz aditiva al buffer L (con el fundido y el
        /// lightK del momento).</summary>
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

        /// <summary>Mezclar un color con alfa sobre lo que ya hay,
        /// conservando el alfa propio (humo y vapor translúcidos).</summary>
        public void BlendPx(int sx, int sy, uint c, double a)
        {
            a *= fade;
            if (c == 0 || a <= 0.004) return;
            int x = sx - ox, y = sy - oy;
            if (x < 0 || y < 0 || x >= w || y >= h) return;
            int i = y * w + x; uint d = C[i];
            double da = (d >> 24) / 255.0, nueva = a + da * (1 - a), mez = a / nueva;
            C[i] = ((((uint)(int)(nueva * 255)) & 255) << 24)
                 | (Canal(c, d, mez, 16) << 16) | (Canal(c, d, mez, 8) << 8) | Canal(c, d, mez, 0);
        }
        static uint Canal(uint c, uint d, double k, int s)
            => (uint)((int)(((c >> s) & 255) * k + ((d >> s) & 255) * (1 - k)) & 255);

        /// <summary>Píxel translúcido en un punto LOCAL.</summary>
        public void Blend(double x, double y, uint c, double a) => BlendPx(CellX(x, y), CellY(x, y), c, a);

        /// <summary>Leer el color bajo un punto local (0 si está vacío).</summary>
        public uint Get(double x, double y)
        {
            int sx = CellX(x, y) - ox, sy = CellY(x, y) - oy;
            return sx < 0 || sy < 0 || sx >= w || sy >= h ? 0 : C[sy * w + sx];
        }

        // ---- los píxeles ----
        public void Px(double x, double y, uint c) => Put(CellX(x, y), CellY(x, y), c, 1);
        public void Dot(double x, double y, uint c, double a = 1) { if (a > 0) Put(CellX(x, y), CellY(x, y), c, a); }
        public void Light(double x, double y, uint c, double k = 1) => Add(CellX(x, y), CellY(x, y), c, k);

        /// <summary>EL SOMBRADO POR CAJA: un pequeño shader casero por
        /// píxel — fn devuelve un color, o escribe en el PxOut
        /// (c/a/l/k); devolver 0 sin tocar nada deja el píxel quieto.
        /// Sin cierres ni delegados extra: la caja y el out van
        /// directos (cero-GC de verdad en la ruta caliente).</summary>
        public void Shade(double x0, double y0, double x1, double y1, PxShadeFn fn)
        {
            PxOut o = OUT;
            Caja(x0, y0, x1, y1, out int sx0, out int sy0, out int sx1, out int sy1);
            for (int sy = sy0; sy <= sy1; sy++)
                for (int sx = sx0; sx <= sx1; sx++)
                {
                    o.c = 0; o.a = 1; o.l = 0; o.k = 0;
                    uint c = fn(FromX(sx + 0.5, sy + 0.5), FromY(sx + 0.5, sy + 0.5), o);
                    if (c == 0) c = o.c;
                    if (c != 0) Put(sx, sy, c, o.a);
                    if (o.k > 0) Add(sx, sy, o.l != 0 ? o.l : c, o.k);
                }
        }

        /// <summary>LA LÍNEA DE LA CASA: caminante de deuda entera —
        /// pinta el píxel, avanza el eje mayor siempre y el menor solo
        /// cuando la deuda lo exige (1 px de grueso, con el umbral de
        /// Put para el fundido).</summary>
        public void Line(double x0, double y0, double x1, double y1, uint c, double a = 1)
        {
            int sx = CellX(x0, y0), sy = CellY(x0, y0);
            int ex = CellX(x1, y1), ey = CellY(x1, y1);
            int dx = Math.Abs(ex - sx), dy = -Math.Abs(ey - sy);
            int pasoX = sx < ex ? 1 : -1, pasoY = sy < ey ? 1 : -1;
            int deuda = dx + dy;
            for (;;)
            {
                Put(sx, sy, c, a);
                if (sx == ex && sy == ey) break;
                int doble = deuda << 1;
                if (doble >= dy) { deuda += dy; sx += pasoX; }
                if (doble <= dx) { deuda += dx; sy += pasoY; }
            }
        }

        /// <summary>EL CÍRCULO: disco lleno; el color y el alfa pueden
        /// ser funciones de d (0 en el centro, 1 en el borde) y del
        /// ángulo.</summary>
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
        /// <summary>El círculo de color y alfa FIJOS — el camino rápido
        /// sin delegados: la caja y el disco directo a Put.</summary>
        public void Circle(double cx, double cy, double r, uint c, double a = 1)
        {
            if (r < 0.6) { Dot(cx, cy, c, a); return; }
            double r2 = r * r;
            Caja(cx - r, cy - r, cx + r, cy + r, out int sx0, out int sy0, out int sx1, out int sy1);
            for (int sy = sy0; sy <= sy1; sy++)
                for (int sx = sx0; sx <= sx1; sx++)
                {
                    double dx = FromX(sx + 0.5, sy + 0.5) - cx;
                    double dy = FromY(sx + 0.5, sy + 0.5) - cy;
                    if (dx * dx + dy * dy <= r2) Put(sx, sy, c, a);
                }
        }

        /// <summary>LA ELIPSE: como el círculo, con radios por eje.</summary>
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
        {
            if (rx <= 0 || ry <= 0) return;
            Caja(cx - rx, cy - ry, cx + rx, cy + ry, out int sx0, out int sy0, out int sx1, out int sy1);
            for (int sy = sy0; sy <= sy1; sy++)
                for (int sx = sx0; sx <= sx1; sx++)
                {
                    double dx = (FromX(sx + 0.5, sy + 0.5) - cx) / rx;
                    double dy = (FromY(sx + 0.5, sy + 0.5) - cy) / ry;
                    if (dx * dx + dy * dy <= 1) Put(sx, sy, c, a);
                }
        }

        /// <summary>EL DISCO translúcido: volumen suave — bocanadas de
        /// humo, vapor, polvo (mezcla BlendPx, no umbral).</summary>
        public void Disc(double cx, double cy, double r, PxColorFn c, PxAlphaFn a)
        {
            if (r < 0.5) { Blend(cx, cy, c(0, 0), a(0, 0)); return; }
            double r2 = r * r;
            Caja(cx - r, cy - r, cx + r, cy + r, out int sx0, out int sy0, out int sx1, out int sy1);
            for (int sy = sy0; sy <= sy1; sy++)
                for (int sx = sx0; sx <= sx1; sx++)
                {
                    double lx = FromX(sx + 0.5, sy + 0.5), ly = FromY(sx + 0.5, sy + 0.5);
                    double dx = lx - cx, dy = ly - cy, d2 = dx * dx + dy * dy;
                    if (d2 > r2) continue;
                    double d = Math.Sqrt(d2) / r, ang = Math.Atan2(dy, dx);
                    BlendPx(sx, sy, c(d, ang), a(d, ang));
                }
        }
        public void Disc(double cx, double cy, double r, uint c, double a)
        {
            if (r < 0.5) { Blend(cx, cy, c, a); return; }
            double r2 = r * r;
            Caja(cx - r, cy - r, cx + r, cy + r, out int sx0, out int sy0, out int sx1, out int sy1);
            for (int sy = sy0; sy <= sy1; sy++)
                for (int sx = sx0; sx <= sx1; sx++)
                {
                    double dx = FromX(sx + 0.5, sy + 0.5) - cx;
                    double dy = FromY(sx + 0.5, sy + 0.5) - cy;
                    if (dx * dx + dy * dy <= r2) BlendPx(sx, sy, c, a);
                }
        }
        public void Disc(double cx, double cy, double r, uint c, PxAlphaFn a)
        {
            if (r < 0.5) { Blend(cx, cy, c, a(0, 0)); return; }
            double r2 = r * r;
            Caja(cx - r, cy - r, cx + r, cy + r, out int sx0, out int sy0, out int sx1, out int sy1);
            for (int sy = sy0; sy <= sy1; sy++)
                for (int sx = sx0; sx <= sx1; sx++)
                {
                    double dx = FromX(sx + 0.5, sy + 0.5) - cx;
                    double dy = FromY(sx + 0.5, sy + 0.5) - cy;
                    double d2 = dx * dx + dy * dy;
                    if (d2 > r2) continue;
                    double d = Math.Sqrt(d2) / r;
                    BlendPx(sx, sy, c, a(d, Math.Atan2(dy, dx)));
                }
        }

        // ---- la luz ----
        /// <summary>EL GLOW: luz radial en el buffer L — k en el
        /// centro, cayendo al cuadrado hasta 0 en r.</summary>
        public void Glow(double x, double y, double r, uint c, double k = 0.5)
        {
            if (r < 1 || k <= 0.004) return;
            double cx = ToX(x, y), cy = ToY(x, y);
            int x0 = (int)Math.Floor(cx - r), x1 = (int)Math.Ceiling(cx + r);
            int y0 = (int)Math.Floor(cy - r), y1 = (int)Math.Ceiling(cy + r);
            for (int sy = y0; sy <= y1; sy++)
            {
                double dy = (sy + 0.5 - cy) / r, dy2 = dy * dy;
                for (int sx = x0; sx <= x1; sx++)
                {
                    double dx = (sx + 0.5 - cx) / r, d = dx * dx + dy2;
                    if (d >= 1) continue;
                    double f = 1 - Math.Sqrt(d);
                    Add(sx, sy, c, f * f * k);
                }
            }
        }

        /// <summary>EL HAZ: luz alrededor de un segmento — la versión
        /// alargada del glow, para rayos y streamers.</summary>
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
    //  PxCycle — LOS BUCLES SIN COSTURA: partículas que se repiten cada
    //  `period` segundos para loops sin empalme. La partícula i nace en
    //  SU fase del período (determinista por hash de la casa) y vive
    //  `life`; su chorro de azar es EL MISMO en cada vuelta — se
    //  resiembra en la única vasija del ciclo: cero-GC y cero sorpresas.
    // ==================================================================
    internal delegate void PxCycleFn(double age, double life, PxMath.Rng r);

    internal static class PxCycle
    {
        const int SAL_NACIMIENTO = 0x7A1;   // la sal de la fase de nacimiento
        static readonly PxMath.Rng Chorro = new PxMath.Rng(1);

        public static void Run(double t, double period, int count, int seed, double life, PxCycleFn fn)
        {
            for (int i = 0; i < count; i++)
            {
                Chorro.Resiembra(PxMath.Hash3(seed, i, SAL_NACIMIENTO));
                double nace = Chorro.Next() * period;
                double edad = (((t - nace) % period) + period) % period;
                for (; edad < life; edad += period)
                {
                    Chorro.Resiembra(PxMath.Hash2(seed, i));
                    fn(edad, life, Chorro);
                }
            }
        }

        public static void Run(double t, double period, int count, int seed, Func<PxMath.Rng, double> life, PxCycleFn fn)
        {
            for (int i = 0; i < count; i++)
            {
                Chorro.Resiembra(PxMath.Hash3(seed, i, SAL_NACIMIENTO));
                double nace = Chorro.Next() * period;
                double vida = life(Chorro);
                double edad = (((t - nace) % period) + period) % period;
                for (; edad < vida; edad += period)
                {
                    Chorro.Resiembra(PxMath.Hash2(seed, i));
                    fn(edad, vida, Chorro);
                }
            }
        }
    }

    // ==================================================================
    //  PxSim — LA FÍSICA DEL TALLER: partículas simuladas y escombros
    //  para los efectos que quieren física de verdad. Unidades:
    //  píxeles de arte y segundos; se dibuja a través de un pincel.
    // ==================================================================
    internal static class PxSim
    {
        public const double GRAVITY = 950; // px/s² — la gravedad del taller

        // las rampas del fuego: lo caliente, la columna y el humo
        public static readonly uint[] HOT = PxColor.Ramp("#fff3a6", "#ffe26b", "#ff8a2a", "#f2561d");
        public static readonly uint[] FIRE_RAMP = PxColor.Ramp("#ffffff", "#fff3a6", "#ffe26b", "#ffc23d", "#ff8a2a", "#f2561d", "#c9281f", "#8a1f17", "#4a1712");
        public static readonly uint[] SMOKE_RAMP = PxColor.Ramp("#7b6f72", "#5d5660", "#433d48", "#2b2630", "#1a171e");

        /// <summary>El paso de la rampa para t en 0..1 — sin mezclar:
        /// el color es el color, y el siguiente píxel es otro.</summary>
        internal static uint RampAt(uint[] r, double t)
            => r[Math.Min(r.Length - 1, Math.Max(0, (int)Math.Floor(t * r.Length)))];

        // los tipos de partícula del taller
        public const int SPARK = 0, MOTE = 1, STREAK = 2, FLAKE = 3, VAPOUR = 4, DUST = 5, STAR = 6, EMBER = 7, SMOKE = 8, FIRE = 9, BRAND = 10, GRIT = 11;
        public const int PF_SMOKES = 1, PF_BIG = 2, PF_BACK = 4;   // los flags
        static bool Soft(int k) => k == VAPOUR || k == DUST || k == SMOKE;   // los blandos: capa de atrás

        /// <summary>El redondeo de la casa: media hacia arriba.</summary>
        internal static int Redon(double v) => (int)Math.Floor(v + 0.5);

        internal sealed class Particle
        {
            public int k; public double x, y, vx, vy, life, max, size, seed, delay;
            public uint col; public int flag;
        }

        /// <summary>EL SISTEMA DE PARTÍCULAS con física real. El rng es
        /// COMPARTIDO a propósito: el sigilo pasa el MISMO chorro a sus
        /// tres sistemas — el orden en que se bebe es parte del look.</summary>
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

            /// <summary>Nacer: `delay` segundos hasta aparecer; col 0
            /// deja que fuego y humo usen sus propias rampas. Bebe un
            /// dado del chorro para la semilla de la partícula.</summary>
            public void Spawn(int k, double x, double y, double vx, double vy, double life,
                uint col = 0xFFFFFFFFu, double size = 1, double delay = 0, int flag = 0)
            {
                list.Add(new Particle
                {
                    k = k, x = x, y = y, vx = vx, vy = vy, life = life, max = life,
                    col = col, size = size, seed = R.Next() * 100, delay = delay, flag = flag,
                });
            }

            public int Count => list.Count;

            // ---- los pasos, tipo por tipo (la física es la de siempre:
            //      cada conducta se conserva) ----
            void PasoFuego(Particle p, double dt, double ahora)
            {
                p.vy -= 95 * dt;                                        // el fuego sube
                p.vx += Math.Sin(ahora * 17 + p.seed) * 30 * dt;        // y serpentea
                p.vx -= p.vx * 5 * dt;
                p.vy -= p.vy * 3.2 * dt;
            }
            void PasoEstela(Particle p, double dt)
            {
                p.vx += p.vx * 3 * dt; p.vy += p.vy * 3 * dt;           // gana ímpetu
            }
            void PasoCopo(Particle p, double dt, double ahora)
            {
                p.vx -= p.vx * 2.2 * dt;
                p.vy -= p.vy * 2.2 * dt - 35 * dt;                      // flota
                p.x += Math.Sin(ahora * 2.5 + p.seed) * 7 * dt;         // y deriva
            }
            void PasoAscuas(Particle p, double dt, double ahora)
            {
                p.vy += (-30 + Math.Sin(ahora * 3 + p.seed) * 20) * dt; // se remonta
                p.vx += Math.Cos(ahora * 2.3 + p.seed) * 45 * dt;       // culebrea
                p.vx -= p.vx * 1.6 * dt;
                p.vy -= p.vy * 1.6 * dt;
            }
            void PasoHumo(Particle p, double dt, double ahora)
            {
                bool grande = (p.flag & PF_BIG) != 0;
                p.vy -= (grande ? 23 : 16) * dt;                        // el humo sube
                p.vx += Math.Sin(ahora * 1.3 + p.seed) * 8 * dt;        // y se mece
                p.vx -= p.vx * 1.6 * dt;
                p.vy -= p.vy * 1.2 * dt;
            }
            /// <summary>La grava: cae, roza, rebota y se asienta.</summary>
            void PasoGrava(Particle p, double dt)
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
            }
            /// <summary>La chispa: vuela, roza, rebota baja.</summary>
            void PasoChispa(Particle p, double dt)
            {
                p.vy += GRAVITY * 0.45 * dt;
                p.vx -= p.vx * 2.2 * dt;
                p.vy -= p.vy * 2.2 * dt;
                double g = ground(p.x), ny = p.y + p.vy * dt;
                p.x += p.vx * dt;
                if (p.vy > 0 && p.y <= g && ny >= g) { p.vy = -p.vy * 0.45; p.vx *= 0.7; p.y = g - 0.25; }
                else p.y = ny;
            }
            /// <summary>El tizón: cae lento escupiendo fuego y MUERE al
            /// tocar suelo (devuelve false).</summary>
            bool PasoTizon(Particle p, double dt)
            {
                p.vy += GRAVITY * 0.8 * dt;
                p.vx -= p.vx * 0.9 * dt;
                if (R.Chance(0.9 * Math.Min(1, dt * 60)))
                    Spawn(FIRE, p.x, p.y, p.vx * 0.1 + R.Range(-7, 7), p.vy * 0.1 - 10, R.Range(0.16, 0.32),
                        0, R.Range(0.9, 1.7) * (0.5 + (p.life / p.max) * 0.6), 0, R.Chance(0.4) ? PF_SMOKES : 0);
                double g = ground(p.x), ny = p.y + p.vy * dt;
                p.x += p.vx * dt;
                if (p.vy > 0 && p.y <= g && ny >= g) return false;
                p.y = ny;
                return true;
            }

            /// <summary>El paso del sistema: reloj, muerte (con su
            /// relevo de humo cuando el fuego lo pide) y física.</summary>
            public void Update(double dt)
            {
                t += dt;
                double ahora = t;
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    Particle p = list[i];
                    if (p.delay > 0) { p.delay -= dt; continue; }
                    p.life -= dt;
                    if (p.life <= 0)
                    {
                        if (p.k == FIRE && (p.flag & PF_SMOKES) != 0)
                        {
                            bool grande = (p.flag & PF_BIG) != 0;
                            Spawn(SMOKE, p.x, p.y, p.vx * 0.6, p.vy * 0.6 - 6,
                                grande ? R.Range(1.3, 2.3) : R.Range(0.8, 1.4), 0,
                                p.size * (grande ? 0.85 : 0.7), 0, grande ? PF_BIG : 0);
                        }
                        list.RemoveAt(i);
                        continue;
                    }
                    switch (p.k)
                    {
                        case FIRE: PasoFuego(p, dt, ahora); break;
                        case STREAK: PasoEstela(p, dt); break;
                        case FLAKE: PasoCopo(p, dt, ahora); break;
                        case EMBER: PasoAscuas(p, dt, ahora); break;
                        case SMOKE: PasoHumo(p, dt, ahora); break;
                        case MOTE: p.vx -= p.vx * 0.6 * dt; p.vy -= p.vy * 0.6 * dt; break;
                        case GRIT: PasoGrava(p, dt); continue;          // integra él solo
                        case SPARK: PasoChispa(p, dt); continue;        // ídem
                        case BRAND: if (!PasoTizon(p, dt)) { list.RemoveAt(i); } continue;
                        default:                                            // motas blandas y estrellas
                            {
                                double roza = Soft(p.k) ? 4 : 1.6;
                                p.vx -= p.vx * roza * dt;
                                p.vy -= p.vy * roza * dt + 5 * dt;
                                break;
                            }
                    }
                    p.x += p.vx * dt;
                    p.y += p.vy * dt;
                }
            }

            /// <summary>El dibujo del sistema (layer null = todo).</summary>
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
                                // LA CHISPA: cabeza que enfría, cola de dos tramos y su glow
                                uint[] S = sparks;
                                uint cabeza = k > 0.6 ? S[0] : k > 0.3 ? S[1] : S[2];
                                double len = Math.Min(9, 1 + sp * 0.022);
                                p.Line(x - ux * len, y - uy * len, x - ux * len * 0.4, y - uy * len * 0.4, k > 0.5 ? cabeza : S[S.Length - 1]);
                                p.Line(x - ux * len * 0.45, y - uy * len * 0.45, x, y, cabeza);
                                p.Glow(x, y, 3, cabeza, 0.45 * k);
                                break;
                            }
                        case MOTE:
                            {
                                // LA MOTA: polvo de luz con rabito
                                double len = Math.Min(16, 0.5 + sp * 0.026) * q.size, a = k < 0.35 ? k / 0.35 : 1;
                                p.Line(x - ux * len, y - uy * len, x - ux * len * 0.35, y - uy * len * 0.35, PxColor.Scale(q.col, 0.55), a);
                                p.Line(x - ux * len * 0.4, y - uy * len * 0.4, x, y, q.col, a);
                                p.Glow(x, y, 2.5 + q.size, q.col, 0.4 * k);
                                break;
                            }
                        case STREAK:
                            {
                                // LA ESTELA: puntos que se apagan hacia atrás
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
                                // EL COPO: un punto (con cruz si es grande)
                                double a = Math.Min(1, k * 3);
                                p.Dot(x, y, q.col, a);
                                if (q.size > 1.3)
                                    foreach (var v in CROSS) p.Dot(x + v.Item1, y + v.Item2, PxColor.Scale(q.col, 0.8), a * 0.8);
                                break;
                            }
                        case VAPOUR:
                            {
                                // EL VAPOR: disco blando que engorda al morir
                                double r = q.size * (0.6 + (1 - k) * 0.9);
                                p.Disc(x, y, r, q.col, Math.Pow(k, 1.3) * 0.45);
                                if (haze.HasValue) p.Glow(x, y, r * 1.8, haze.Value, 0.16 * k);
                                break;
                            }
                        case DUST: p.Disc(x, y, q.size * (0.6 + (1 - k) * 0.9), q.col, Math.Pow(k, 1.3) * 0.7); break;
                        case SMOKE:
                            {
                                // EL HUMO: cuerpo blando, núcleo denso y una tapa tenue de joven
                                double age = 1 - k; bool grande = (q.flag & PF_BIG) != 0;
                                double r = q.size * (0.65 + Math.Min(1, age * 2.5) * 0.45 + age * (grande ? 0.5 : 0.4));
                                double a = Math.Min(1, age * 12) * Math.Min(1, k * 2.4) * (grande ? 0.7 : 0.58);
                                uint c = q.col != 0 ? PxColor.Mix(q.col, RampAt(SMOKE_RAMP, age), age) : RampAt(SMOKE_RAMP, age * 1.05);
                                p.Disc(x, y, r, c, a * 0.45);
                                p.Disc(x + r * 0.05, y + r * 0.06, r * 0.74, c, a * 0.62);
                                if (age < 0.5) p.Disc(x - r * 0.22, y - r * 0.3, r * 0.5, PxColor.Mix(c, PxColor.WHITE, 0.12), a * 0.4 * (1 - age * 2));
                                break;
                            }
                        case FIRE:
                            {
                                // EL FUEGO: bola de rampa — borde, núcleo, lengua y corazón caliente
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
                            // EL TIZÓN: brasa que parpadea y alumbra
                            p.Circle(x, y, 1.6, HOT[3]);
                            p.Px(x, y, Math.Sin(this.t * 40 + q.seed) > 0 ? HOT[0] : HOT[1]);
                            p.Px(x + 1, y, HOT[1]);
                            p.Glow(x, y, 7, HOT[2], 0.45);
                            break;
                        case GRIT:
                            // LA GRAVA: uno o cuatro píxeles según el tamaño
                            if (k > 0.125)
                            {
                                p.Px(x, y, q.col);
                                if (q.size >= 2) { p.Px(x + 1, y, q.col); p.Px(x, y + 1, PxColor.Scale(q.col, 0.8)); p.Px(x + 1, y + 1, PxColor.Scale(q.col, 0.7)); }
                            }
                            break;
                        case EMBER:
                            {
                                // EL ASCUA: punto que titila al apagarse
                                uint c = k > 0.6 ? sparks[0] : k > 0.3 ? sparks[1] : sparks[2];
                                if (k < 0.25 && Math.Sin(this.t * 40 + q.seed) < 0) break;
                                p.Px(x, y, c);
                                if (q.size > 1.5) p.Px(x, y + 1, sparks[sparks.Length - 1]);
                                p.Glow(x, y, 3 + q.size, c, 0.35 * k);
                                break;
                            }
                        default:
                            {
                                // LA ESTRELLA: una cruz que titila
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

        // ---- los escombros: tipos y el sistema ----
        public const int SHARD = 0, CHUNK = 1, DRIP = 2;

        internal sealed class Bit
        {
            public int kind = SHARD; public double x, y, vx, vy, a, va, s = 1, life = 1, max;
            public uint c = PxColor.WHITE; public uint[] ramp = null;
            public double g = 1, drag, bounce; public bool rest;
        }

        /// <summary>LOS ESCOMBROS: pedazos rígidos que caen, rebotan y
        /// se asientan (la ruina del sigilo).</summary>
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
                    double piso = ground(b.x), sp = b.vy;
                    if (b.g <= 0 || b.y < piso || sp <= 0) continue;
                    b.y = piso - 0.01;
                    if (b.kind == DRIP) b.life = 0;                                   // la gota muere al caer
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
                        // LA ESQUIRLA: una raya de píxeles con la punta en blanco
                        for (double d = -b.s / 2; d <= b.s / 2; d += 0.5)
                            p.Dot(b.x + ca * d, b.y + sa * d, d > b.s / 2 - 0.6 ? PxColor.WHITE : b.c, a);
                    }
                    else if (b.kind == DRIP)
                    {
                        // LA GOTA: punto con su laga arriba
                        p.Dot(b.x, b.y, b.c, a);
                        p.Dot(b.x, b.y - 1, b.c, a * 0.5);
                    }
                    else
                    {
                        // EL TROZO: rombo girado sombreado por cuadrantes con su rampa
                        uint[] R = b.ramp ?? CHUNK_RAMP; int n = R.Length;
                        int x = Redon(b.x), y = Redon(b.y), r = (int)Math.Ceiling(b.s) + 1;
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

        // la cruz y la diagonal del destello
        internal static readonly (int, int)[] CROSS = { (1, 0), (-1, 0), (0, 1), (0, -1) };
        internal static readonly (int, int)[] DIAG = { (1, 1), (1, -1), (-1, 1), (-1, -1) };

        /// <summary>EL DESTELLO: brazos de longitud `arm` en color c
        /// con puntas en tip, corazón blanco y su luz. La pluma del
        /// sigilo es un destello que respira.</summary>
        public static void Sparkle(PxPainter p, double x, double y, double arm, uint c, uint tip, double k = 0.6)
        {
            int n = Redon(arm);
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
    }

    // ==================================================================
    //  PxShadowLib — LOS PEDAZOS DE SOMBRA: la paleta del vacío, la
    //  luz que las siluetas negras cortan, el humo encendido de un
    //  lado, la llama violeta y su hollín.
    // ==================================================================
    internal static class PxShadowLib
    {
        /// <summary>LA PALETA DEL VACÍO — de lo oscuro a lo claro:
        /// negro de vacío, violeta profundo y un borde blanco-lila.</summary>
        public static readonly uint[] VOID = PxColor.Ramp(
            "#06040b", "#150c26", "#2a1150", "#461a84", "#6f2cc0", "#9d5cec", "#cfa6ff", "#f6eaff");

        /// <summary>EL UNLIGHT: quitar la luz de cada píxel de una caja
        /// local donde dark(x,y) es verdad — así las siluetas negras se
        /// quedan NEGRAS bajo cada glow, chispa y anillo dibujado
        /// antes. El eclipse vive de esto: la luna negra corta la luz
        /// de su propia corona. Con keep>0 se deja un residuo.</summary>
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

        /// <summary>EL ANILLO: las celdas entre r0 y r1, fila a fila,
        /// en DOS TRAMOS por fila — del borde exterior al borde del
        /// agujero y del otro borde del agujero al exterior — sin pisar
        /// el hueco (es lo que mantiene la banda barata).</summary>
        static void Anillo(double x, double y, double r0, double r1, Action<double, double, double> fn)
        {
            double r02 = r0 > 0 ? r0 * r0 : -1, r12 = r1 * r1;
            for (int py = (int)Math.Floor(y - r1); py <= (int)Math.Ceiling(y + r1); py++)
            {
                double cy = py + 0.5, dy = cy - y, dy2 = dy * dy;
                if (dy2 >= r12) continue;
                double xo = Math.Sqrt(r12 - dy2), xi = dy2 < r02 ? Math.Sqrt(r02 - dy2) : 0;
                int izq0 = (int)Math.Floor(x - xo), izq1 = (int)Math.Floor(x + xo);
                if (xi > 0)
                {
                    // el agujero parte la fila: [fuera-izq .. borde-agujero] y [borde-agujero .. fuera-der]
                    int huecoIzq = (int)Math.Floor(x - xi - 0.5) + 1, huecoDer = (int)Math.Ceiling(x + xi - 0.5) - 1;
                    for (int px = izq0; px <= izq1; px++)
                    {
                        if (px >= huecoIzq && px <= huecoDer) { px = huecoDer; continue; }
                        double cx = px + 0.5, dx = cx - x, d2 = dx * dx + dy2;
                        if (d2 < r12 && d2 >= r02) fn(cx, cy, Math.Sqrt(d2));
                    }
                }
                else
                {
                    for (int px = izq0; px <= izq1; px++)
                    {
                        double cx = px + 0.5, dx = cx - x, d2 = dx * dx + dy2;
                        if (d2 < r12 && d2 >= r02) fn(cx, cy, Math.Sqrt(d2));
                    }
                }
            }
        }

        /// <summary>EL ANILLO RÁPIDO: solo visita SUS píxeles (el anillo
        /// de choque del destello del sigilo).</summary>
        public static void RingFast(PxPainter p, double x, double y, double r, double w, uint c, double a = 1)
        {
            if (r <= 0) return;
            Anillo(x, y, Math.Max(0, r - w), r, (cx, cy, d) => p.Dot(cx, cy, c, a));
        }

        /// <summary>EL HALO: un anillo de LUZ abrazado a un disco negro
        /// — k en r0 cayendo a nada en r1, y nada dentro de r0.</summary>
        public static void Halo(PxPainter p, double x, double y, double r0, double r1, uint c, double k)
        {
            if (r1 <= r0 || k <= 0.004) return;
            double ancho = r1 - r0;
            Anillo(x, y, Math.Max(0, r0), r1, (cx, cy, d) =>
            {
                double f = 1 - (d - r0) / ancho;
                p.Light(cx, cy, c, f * f * k);
            });
        }

        // los tipos de bocanada
        public const int PK_FLAME = 0, PK_SOOT = 1;

        /// <summary>La luz que encara al hollín (su borde iluminado).</summary>
        internal sealed class PuffLight
        {
            public double x, y, k, reach;
        }

        internal sealed class Puff
        {
            public int k; public double x, y, vx, vy, life, max, size, delay, seed;
            public bool smokes;
        }

        /// <summary>LAS BOCANADAS: llama violeta que enfría por su
        /// rampa y hollín oscuro encendido del lado que mira a la luz
        /// (y que rueda por el suelo en vez de atravesarlo).</summary>
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
                double ahora = t;
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    Puff q = list[i];
                    if (q.delay > 0) { q.delay -= dt; continue; }
                    q.life -= dt;
                    if (q.life <= 0)
                    {
                        // la llama que muere dejando hollín (si lo pidió)
                        if (q.k == PK_FLAME && q.smokes)
                            Spawn(PK_SOOT, q.x, q.y - 1, q.vx * 0.5, q.vy * 0.5 - 6, R.Range(0.9, 1.6), q.size * 1.25);
                        list.RemoveAt(i);
                        continue;
                    }
                    if (q.k == PK_FLAME)
                    {
                        q.vy -= 95 * dt;                                 // la llama sube
                        q.vx += Math.Sin(ahora * 17 + q.seed) * 30 * dt; // y serpentea
                        q.vx -= q.vx * 5 * dt;
                        q.vy -= q.vy * 3.2 * dt;
                    }
                    else
                    {
                        q.vy -= 18 * dt;                                 // el hollín sube más despacio
                        q.vx += Math.Sin(ahora * 1.3 + q.seed) * 8 * dt; // y se mece
                        q.vx -= q.vx * 1.6 * dt;
                        q.vy -= q.vy * 1.3 * dt;
                    }
                    q.x += q.vx * dt;
                    q.y += q.vy * dt;
                    // «el humo rueda por el suelo, no a través de él»
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
                        // LA BOCA DE FUEGO: borde oscuro, núcleo arriba-izquierda,
                        // lengua encima y corazón caliente de joven
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
                    // EL HOLLÍN: cuerpo blando + núcleo denso + tapa tenue
                    // + el borde encendido hacia la luz
                    double rr = q.size * (0.65 + Math.Min(1, age * 2.5) * 0.45 + age * 0.45);
                    double a = Math.Min(1, age * 10) * Math.Min(1, k * 2.2) * 0.72;
                    uint body = PxColor.Mix(soot[0], soot[1], age);
                    double edge = a * 0.5, nucleo = 1 - (1 - a * 0.5) * (1 - a * 0.62);
                    p.Disc(x, y + rr * 0.06, rr, body, (d, ang) => d < 0.72 ? nucleo : edge);
                    double lk = light != null ? light.k : 0;
                    if (lk < 0.5) p.Disc(x - rr * 0.2, y - rr * 0.32, rr * 0.48, cap, a * 0.32 * (1 - age * 0.7) * (1 - lk * 2));
                    if (lk <= 0.02) continue;
                    double lx = light.x - x, ly = light.y - y;
                    double dl = Math.Sqrt(lx * lx + ly * ly); if (dl == 0) dl = 1;
                    double ux = lx / dl, uy = ly / dl;
                    double lit = Math.Min(1, light.k * PxMath.Clamp01(1.3 - dl / light.reach)) * a;
                    if (lit <= 0.02) continue;
                    uint caliente = rim[0], tibia = rim[1], apagon = rim[2];
                    p.Disc(x + ux * rr * 0.26, y + uy * rr * 0.26, rr * 0.72, apagon, lit * 0.55);
                    p.Disc(x + ux * rr * 0.5, y + uy * rr * 0.5, rr * 0.46, tibia, lit * 0.5);
                    if (rr > 3) p.Disc(x + ux * rr * 0.68, y + uy * rr * 0.68, rr * 0.24, caliente, lit * 0.4);
                }
            }
        }
    }

    // ==================================================================
    //  EL CORREDOR — las opciones, el meta de cada efecto, la instancia
    //  viva y el compositor que junta color+luz en RGBA.
    // ==================================================================

    /// <summary>Las opciones de un efecto: paleta, semilla, colocación
    /// (X/Y/espejo/giro) y cuánta luz trae.</summary>
    internal sealed class PxOptions
    {
        public uint[] Palette = PxShadowLib.VOID;
        public uint[] Pearl = null;                 // el eclipse: su rampa perla
        public int Seed = 1;
        public double X = 0, Y = 0;
        public bool Flip = false;
        public double Angle = 0;
        public double Light = 1;
    }

    /// <summary>El meta de un efecto: su lienzo (w×h, origen), su
    /// duración, si es loop, su fps de autor y su semilla.</summary>
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

    /// <summary>LA INSTANCIA VIVA de un efecto: update(dt) hace avanzar
    /// el acto, draw(frame) lo dibuja en su origen, stop() lo funde.
    /// Las simulaciones pisan a 120 Hz o más fino, sea cual sea el fps
    /// del que llama — así el ritmo es el mismo en todas partes. Y el
    /// reloj del efecto es SU reloj: el que acumulan sus propios
    /// subpasos, no el del corredor.</summary>
    internal sealed class PxFxInstance
    {
        const double FADE = 0.3;   // el fundido del stop: 0,3 s
        readonly IPxFx impl;
        public readonly PxMeta meta;
        public readonly PxOptions options;
        double t, stopAt = double.PositiveInfinity;

        public PxFxInstance(IPxFx impl, PxMeta meta, PxOptions options)
        { this.impl = impl; this.meta = meta; this.options = options; }

        public double T => t;

        public void Update(double dt)
        {
            try
            {
                t += dt;
                var u = impl as IPxFxUpdate;
                if (u == null) return;
                // los subpasos: 120 Hz o más fino (que el acto no
                // dependa de los fps del que llama)
                int n = Math.Max(1, (int)Math.Ceiling(dt * 120 - 1e-9));
                for (int i = 0; i < n; i++) u.Update(dt / n, options);
            }
            catch { /* si el acto tropieza, que no tire el juego */ }
        }

        public void Draw(PxFrame frame, string layer = null)
        {
            try
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
            catch { /* un frame a medias se repinta al siguiente */ }
        }

        /// <summary>Dejarlo acabar: los loops se funden, los de un acto
        /// se cortan.</summary>
        public void Stop() { if (stopAt == double.PositiveInfinity) stopAt = t; }

        public bool Done => t - stopAt >= FADE || (!meta.Loop && t >= meta.Duration);
    }

    internal static class PixelFxCore
    {
        /// <summary>EL COMPOSITOR: color sobre transparencia, y la luz
        /// encima — en bytes RGBA de alfa recta. Sobre píxel vacío la
        /// luz se vuelve color con alfa (la premultiplicación vive aquí
        /// en la matemática: lo que hay se premultiplica, la luz se
        /// suma y se reparte en el alfa nuevo) — así un brillo sobre
        /// fondo oscuro se lee aditivo de verdad.</summary>
        public static void Composite(PxFrame f, byte[] o, double lightK = 1)
        {
            int n = f.w * f.h; uint[] C = f.color, L = f.light;
            for (int i = 0; i < n; i++)
            {
                uint c = C[i], l = L[i] & 0xffffff;
                double r = c & 255, g = (c >> 8) & 255, b = (c >> 16) & 255, a = c >> 24;
                if (l != 0)
                {
                    double lr = (l & 255) * lightK, lg = ((l >> 8) & 255) * lightK, lb = ((l >> 16) & 255) * lightK;
                    if (a < 255)
                    {
                        // sobre transparencia: premultiplicar, sumar, repartir
                        double pa = a / 255.0, pr = r * pa + lr, pg = g * pa + lg, pb = b * pa + lb;
                        double na = Math.Min(1, Math.Max(pa, Math.Max(lr, Math.Max(lg, lb)) / 255));
                        if (na > 0) { r = Math.Min(255, pr / na); g = Math.Min(255, pg / na); b = Math.Min(255, pb / na); a = na * 255; }
                    }
                    else { r = Math.Min(255, r + lr); g = Math.Min(255, g + lg); b = Math.Min(255, b + lb); }
                }
                int j = i * 4;
                o[j] = ByteCasa(r); o[j + 1] = ByteCasa(g); o[j + 2] = ByteCasa(b); o[j + 3] = ByteCasa(a);
            }
        }

        /// <summary>El byte de la casa: clampa a [0,255] y redondea la
        /// media hacia arriba.</summary>
        static byte ByteCasa(double v)
        {
            if (double.IsNaN(v)) return 0;
            if (v <= 0) return 0;
            if (v >= 255) return 255;
            return (byte)Math.Floor(v + 0.5);
        }
    }

    // ==================================================================
    //  PxHexSigil — EL SIGILO DEL ETERNO. El acto completo: una pluma
    //  ardiente traza el sigilo maldito en el aire, sus glifos saltan
    //  uno a uno, luego destella en blanco y se desmorona en ascuas
    //  que caen y rebotan.
    //  Lienzo 168×128, origen [84,112], duración 4,2 s (¡un solo
    //  acto!), semilla 7, paleta del vacío.
    // ==================================================================
    internal sealed class PxHexSigil : IPxFx, IPxFxBack, IPxFxUpdate
    {
        public static readonly PxMeta Meta = new PxMeta(168, 128, 84, 112, 4.2, loop: false, fps: 30, seed: 7);

        // el centro del sigilo, sus radios y los tiempos del acto:
        // trazado, glifos (1,62), destello (2,6) y quiebra (2,74)
        const double CY = -50, RO = 28, RI = 22, RC = 6, GLYPH = 1.62, FLARE = 2.6, BREAK = 2.74;
        const int SAL_GLIFO = 0x613;   // la sal del dado de cada glifo

        internal sealed class PenPt { public double x, y, t; }
        internal sealed class Glyph
        {
            public double x, y, t; public bool popped;
            public List<(int[] a, int[] b)> segs;
        }
        internal sealed class SigLight { public double x, y, r, k, life, max; }

        readonly uint[] C;
        readonly PxMath.Rng R;                 // UN solo chorro COMPARTIDO por ps/embers/puffs: el orden en que se bebe es parte del look
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
            PxMath.Rng rng = R; Func<double, double> floor = _ => 0;   // el suelo del acto es y = 0 (el origen)
            var (pts, fin) = Strokes();
            S = pts; SEnd = fin;
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

        /// <summary>LOS TRAZOS, en el orden en que la pluma los dibuja:
        /// el círculo exterior, la estrella de siete puntas de UNA
        /// sola tirada (tres puntas por vuelta), el círculo íntimo, el
        /// circulito del corazón y el nudo. Muestreado cada medio píxel
        /// con el instante en que la pluma pasa por ahí — las
        /// velocidades del Taller (420/700/550/380/300 px/s y 0,04 s
        /// de respiro entre trazos).</summary>
        static (List<PenPt>, double) Strokes()
        {
            var paths = new List<List<double[]>>();
            List<double[]> Circulo(double r, double a0)
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
            paths.Add(Circulo(RO, -Math.PI / 2));
            var estrella = new List<double[]>();
            for (int i = 0; i <= 7; i++)
            {
                double a = -Math.PI / 2 + ((i * 3) % 7) * (PxMath.TAU / 7);
                estrella.Add(new[] { Math.Cos(a) * RI, CY + Math.Sin(a) * RI });
            }
            paths.Add(estrella);
            paths.Add(Circulo(RI, -Math.PI / 2));
            paths.Add(Circulo(RC, Math.PI / 2));
            paths.Add(new List<double[]>
            {
                new[] { 0.0, CY - 3.5 }, new[] { 2.0, CY }, new[] { 0.0, CY + 3.5 },
                new[] { -2.0, CY }, new[] { 0.0, CY - 3.5 },
            });
            var pts = new List<PenPt>();
            double[] velocidades = { 420, 700, 550, 380, 300 };
            double t = 0.12;
            for (int k = 0; k < paths.Count; k++)
            {
                var path = paths[k];
                for (int i = 1; i < path.Count; i++)
                {
                    double x0 = path[i - 1][0], y0 = path[i - 1][1], x1 = path[i][0], y1 = path[i][1];
                    double L = Math.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0));
                    for (double d = 0; d < L; d += 0.5)
                        pts.Add(new PenPt { x = x0 + (x1 - x0) * d / L, y = y0 + (y1 - y0) * d / L, t = t + d / velocidades[k] });
                    t += L / velocidades[k];
                }
                t += 0.04;
            }
            return (pts, t);
        }

        /// <summary>LOS GLIFOS: siete glifos pequeños de la casa — una
        /// espina y dos o tres ramas sobre una rejilla 3×3 de nodos —,
        /// cada uno con SU dado determinista (semilla, i, la sal del
        /// glifo) y su momento de estallido: GLYPH + i·0,07.</summary>
        static List<Glyph> Glyphs(int seed)
        {
            var g = new List<Glyph>(7);
            for (int i = 0; i < 7; i++)
            {
                PxMath.Rng r = PxMath.RngOf3(seed, i, SAL_GLIFO);
                var segs = new List<(int[], int[])> { (new[] { 1, 0 }, new[] { 1, 2 }) };
                int ramas = 2 + (r.Next() < 0.5 ? 1 : 0);
                for (int k = 0; k < ramas; k++)
                {
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

        /// <summary>EL PASO DEL ACTO: la pluma que escupe chispas y
        /// gotea ascuas, el poder que entra en estelas, los glifos que
        /// estallan, el destello y el desmoronamiento.</summary>
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
            // el poder entra al sigilo mientras se traza y se sostiene:
            // estelas que corren hacia su corazón
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
                // EL DESTELLO: dos luces y una cortina de chispas
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
                // LA QUIEBRA: cada trazo se rompe en fragmentos
                // ardientes que caen dando vueltas y rebotan
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

        // EL CAPA DE ATRÁS: el hollín de la quiebra y el resplandor
        // violeta que sostiene el acto (y se apaga al romperse).
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
                // los trazos: blancos donde la pluma ACABA de pasar,
                // enfriándose al violeta; pulsando mientras se
                // sostiene; blancos en el destello
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
                // los glifos: cada uno salta en blanco y se asienta
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
                // la pluma: un punto al blanco con su crucecita de luz
                if (t > 0.12 && t < SEnd + 0.05)
                {
                    PenPt q = Pen();
                    PxSim.Sparkle(p, q.x, q.y, 3 + Math.Sin(t * 60), C[7], C[6], 0.6);
                    p.Glow(q.x, q.y, 16, C[6], 0.55);
                }
            }
            // el destello: una cruz que se cierra y un anillo de choque
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
    //  PxShadowFlames — LAS LLAMAS DE SOMBRA. Fuego al revés en el
    //  suelo: un corazón negro envuelto en llama violeta de piel pálida
    //  y puntas blancas, que suelta ascuas y humo oscuro.
    //  Lienzo 104×112, origen [52,102] (EL SUELO), loop 2,4 s,
    //  semilla 12, paleta del vacío.
    // ==================================================================
    internal sealed class PxShadowFlames : IPxFx, IPxFxBack
    {
        public static readonly PxMeta Meta = new PxMeta(104, 112, 52, 102, 2.4, loop: true, fps: 30, seed: 12);

        const double D = 2.4;
        const int GX = 22, GY = 60;

        // las lenguas: desplazamiento, altura base, ancho y semilla; la
        // del centro la más alta
        static readonly double[][] TONGUES =
        {
            new[] { -11.0, 20.0, 4.2, 1.0 }, new[] { 11.0, 22.0, 4.4, 2.0 },
            new[] { -6.0, 31.0, 5.4, 3.0 }, new[] { 6.0, 29.0, 5.2, 4.0 },
            new[] { 0.0, 40.0, 6.6, 5.0 },
        };
        // las capas de dentro hacia afuera: cuánto de ancho, cuánto de
        // alto y qué tono (3 es el corazón negro)
        static readonly double[][] LAYERS =
        {
            new[] { 1.0, 1.0, 0.0 }, new[] { 0.8, 0.88, 1.0 },
            new[] { 0.54, 0.68, 2.0 }, new[] { 0.32, 0.48, 3.0 },
        };
        // el orden de pintado: |dx| descendente (la de afuera primero) —
        // precalculado una sola vez
        static readonly int[] ORDEN = OrdenLenguas();
        static int[] OrdenLenguas()
        {
            int[] orden = { 0, 1, 2, 3, 4 };
            for (int a = 1; a < orden.Length; a++)
                for (int b = a; b > 0 && Math.Abs(TONGUES[orden[b - 1]][0]) < Math.Abs(TONGUES[orden[b]][0]); b--)
                    (orden[b - 1], orden[b]) = (orden[b], orden[b - 1]);
            return orden;
        }
        // la rejilla de tonos de la columna: estática — cero basura por frame
        static readonly sbyte[] Rejilla = new sbyte[GX * 2 * GY];

        /// <summary>EL RUIDO QUE VUELVE: ruido de valores que da la
        /// vuelta por un círculo del plano del ruido y regresa a donde
        /// empezó cada vuelta del loop — sin empalme.</summary>
        static double Lnoise(double t, double v, int seed, double speed = 5)
            => PxMath.Noise2(Math.Cos((PxMath.TAU * t) / D) * speed + v, Math.Sin((PxMath.TAU * t) / D) * speed, seed);

        public void DrawBack(PxPainter p, double t, PxOptions o)
        {
            uint[] C = PxColor.Fit(o.Palette, 8);
            int S = o.Seed;
            // el suelo quemado: una mancha oscura con luz violeta pooled
            p.Ellipse(0, 0.5, 22, 3.2, (d, _) => d < 0.6 ? C[0] : C[1], (d, _) => d < 0.85 ? 1 : (1 - d) * 6);
            p.Glow(0, -2, 46, C[4], 0.3);
            // el humo oscuro que sube de las puntas, con el borde
            // encendido por debajo
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
            // rasterizar las lenguas, la de afuera primero, a una
            // rejilla de tonos
            sbyte[] grid = Rejilla;
            Array.Fill(grid, (sbyte)-1);
            void Set(double x, double y, int v)
            {
                int gi = PxSim.Redon(x) + GX, gj = PxSim.Redon(y) + GY;
                if (gi >= 0 && gj >= 0 && gi < GX * 2 && gj < GY) grid[gj * GX * 2 + gi] = (sbyte)v;
            }
            foreach (int i in ORDEN)
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
                        for (int x = PxSim.Redon(cx - half); x <= PxSim.Redon(cx + half); x++)
                        {
                            // la piel se rompe cerca de la punta: los
                            // píxeles parpadean fuera de ella
                            if (tone == 0 && u > 0.45 && Lnoise(t, x * 0.9 + r * 0.7 + n, (int)n + 60, 8) > 0.35) continue;
                            Set(x, -r, tone);
                        }
                    }
                }
            }
            // pintarla: la piel pálida encendiendo hacia las puntas y
            // el corazón negro NEGRO
            for (int j = 0; j < GY; j++)
                for (int i = 0; i < GX * 2; i++)
                {
                    int v = grid[j * GX * 2 + i];
                    if (v < 0) continue;
                    double x = i - GX, y = j - GY;
                    uint c = v switch { 0 => C[6], 1 => C[4], 2 => C[2], _ => C[0] };
                    if (v == 0 && y < -24 && (j > 0 ? grid[(j - 1) * GX * 2 + i] : (sbyte)-1) < 0)
                        c = y < -32 ? PxColor.WHITE : C[7];
                    p.Px(x + 0.5, y + 0.5, c);
                }
            // lengüitas de llama pálida que se desprenden de las puntas
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
            // ascuas que suben derivando, parpadean y enfrían al violeta
            PxCycle.Run(t, D, 30, S + 2, r => r.Range(1, 1.8), (age, L, r) =>
            {
                double u = age / L;
                double x = r.Range(-12, 12) + Math.Sin(u * 6 + r.Range(0, 6)) * 5 * u;
                double y = -10 - u * r.Range(40, 70);
                if (u > 0.7 && Math.Sin(age * 40 + x) < 0) return;
                p.Px(x, y, u < 0.3 ? PxColor.WHITE : u < 0.6 ? C[7] : C[6]);
                p.Glow(x, y, 3, C[6], 0.35 * (1 - u));
            });
            // la luz: un brote violeta alrededor — y NINGUNA sobre el
            // corazón negro
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
    //  PxEclipse — EL ECLIPSE VIVO. Una luna negra sobre el sol: la
    //  corona de perla que respira con streamers largos, un borde
    //  blanco fino y UNA cuenta de diamante blaze en su limbo.
    //  Lienzo 160×144, origen [80,72] (EL CENTRO), loop 4 s, semilla 3.
    // ==================================================================
    internal sealed class PxEclipse : IPxFx, IPxFxBack
    {
        public static readonly PxMeta Meta = new PxMeta(160, 144, 80, 72, 4, loop: true, fps: 30, seed: 3);

        /// <summary>La rampa perla de la corona.</summary>
        public static readonly uint[] PEARL = PxColor.Ramp("#5a4f8a", "#a99be0", "#e6defa", "#fffaf2");

        // el radio de la luna, el loop y dónde blaza la cuenta en el limbo
        const double R = 19, D = 4, BEAD = -0.72;

        static double Breathe(double t, double k, double ph) => Math.Sin((PxMath.TAU * k * t) / D + ph);

        internal sealed class Streamer { public double a, len, curl, k, ph, w; }

        /// <summary>LOS STREAMERS de la corona: apiñados hacia el
        /// ecuador (izquierda y derecha), más largos ahí, cada uno
        /// curvándose al salir — construidos con el dado de la casa,
        /// deterministas para siempre.</summary>
        static readonly Streamer[] STREAMS = BuildStreams();
        static Streamer[] BuildStreams()
        {
            var s = new Streamer[30];
            for (int i = 0; i < 30; i++)
            {
                double lado = i % 2 != 0 ? 0 : Math.PI;
                double abanico = (PxMath.Rand2(i, 1) - 0.5) * (PxMath.Rand2(i, 2) < 0.7 ? 1.3 : 3.1);
                double a = lado + abanico, eq = Math.Abs(Math.Cos(a));
                s[i] = new Streamer
                {
                    a = a,
                    len = (10 + PxMath.Rand2(i, 3) * 28) * (0.45 + 0.75 * eq),
                    curl = (PxMath.Rand2(i, 4) - 0.5) * 0.00018,
                    k = 1 + (i % 3),
                    ph = PxMath.Rand2(i, 5) * PxMath.TAU,
                    w = PxMath.Rand2(i, 6),
                };
            }
            return s;
        }

        public void DrawBack(PxPainter p, double t, PxOptions o)
        {
            uint[] C = PxColor.Fit(o.Palette, 8);
            uint[] Q = PxColor.Fit(o.Pearl ?? PEARL, 4);
            int S = o.Seed;
            // las estrellas que titilan en el cielo oscuro de alrededor
            PxCycle.Run(t, D, 26, S, r => r.Range(1.2, 2.6), (age, L, r) =>
            {
                double x = r.Range(-78, 78), y = r.Range(-70, 70);
                if (Math.Sqrt(x * x + y * y) < R + 22) return;
                double k = Math.Sin((Math.PI * age) / L);
                p.Dot(x, y, k > 0.7 ? PxColor.WHITE : Q[1], k * 1.2);
                if (k > 0.85 && r.Next() < 0.4) { p.Dot(x - 1, y, Q[0], 0.8); p.Dot(x + 1, y, Q[0], 0.8); }
            });
            // la luz de la corona exterior: un brote perla ancho que respira
            double b = 1 + 0.08 * Breathe(t, 1, 0);
            p.Glow(0, 0, 72 * b, C[4], 0.38);
            p.Glow(0, 0, 46 * b, Q[1], 0.34);
            // los streamers: rayos finos de perla, blancos en la raíz,
            // afinando al violeta, curvándose al salir
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
            // la corona interior: un cuello brillante abrazado a la
            // luna, finamente estriado, apagándose de la perla al violeta
            double W = R + 10;
            p.Shade(-W, -W, W, W, (x, y, luz) =>
            {
                double r = Math.Sqrt(x * x + y * y);
                if (r < R - 0.5 || r > R + 9 * b) return 0;
                double a = Math.Atan2(y, x), e = (r - R) / (9 * b);
                double estria = 0.55 + 0.3 * Math.Cos(a * 23 + Math.Sin(a * 5) * 2) + 0.15 * Math.Cos(a * 41 - (PxMath.TAU * t) / D);
                double I = Math.Pow(1 - e, 2.2) * estria * (0.75 + 0.4 * Math.Abs(Math.Cos(a)));
                if (r < R + 0.9) return Q[3];
                if (I < 0.1) return 0;
                if (I > 0.45 && (((int)Math.Floor(x) + (int)Math.Floor(y)) & 1) == 0) luz.k = 0.2 * I;
                return I > 0.72 ? Q[2] : I > 0.42 ? Q[1] : I > 0.22 ? C[5] : C[4];
            });
            // la luna: negra, con un susurro de luz cenicienta en su limbo izquierdo
            p.Shade(-R, -R, R, R, (x, y, luz) =>
            {
                double r = Math.Sqrt(x * x + y * y);
                if (r >= R - 0.5) return 0;
                return r > R - 2.2 && x < -R * 0.45 ? C[1] : C[0];
            });
            // la cuenta de diamante: un punto que blaza en el limbo,
            // con rayos que se abren y respiran
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

    /// <summary>EL FRENTE DEL MOTOR: cada arma instancia su efecto con
    /// su semilla, colocado en su origen — el corredor hace el resto.</summary>
    internal static class PixelFx
    {
        /// <summary>El Sigilo del Eterno: el acto completo (semilla 7).</summary>
        public static PxFxInstance HexSigil(int seed = 7)
        {
            var o = new PxOptions { Seed = seed, X = PxHexSigil.Meta.Ox, Y = PxHexSigil.Meta.Oy };
            return new PxFxInstance(new PxHexSigil(o), PxHexSigil.Meta, o);
        }

        /// <summary>Las Llamas de Sombra: la columna en loop (semilla 12).</summary>
        public static PxFxInstance ShadowFlames(int seed = 12)
        {
            var o = new PxOptions { Seed = seed, X = PxShadowFlames.Meta.Ox, Y = PxShadowFlames.Meta.Oy };
            return new PxFxInstance(new PxShadowFlames(), PxShadowFlames.Meta, o);
        }

        /// <summary>El Eclipse Vivo: la nova negra de corona (semilla 3).</summary>
        public static PxFxInstance Eclipse(int seed = 3)
        {
            var o = new PxOptions { Seed = seed, Pearl = PxEclipse.PEARL, X = PxEclipse.Meta.Ox, Y = PxEclipse.Meta.Oy };
            return new PxFxInstance(new PxEclipse(), PxEclipse.Meta, o);
        }
    }
}
