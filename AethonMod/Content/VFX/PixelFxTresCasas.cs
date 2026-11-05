using System;

// ======================================================================
//  PixelFxTresCasas.cs — v6.99 — LAS OCHO PUERTAS DEL MOTOR DE PÍXELES.
//
//  LA LETRA DEL USUARIO (la ley de esta sesión): «entre las
//  herramientas había una para crear animaciones de ataque — es la que
//  usaste para crear las armas de la bolsa de taller — ¿qué pasó con
//  eso, por qué no lo usas para crear proyectiles y ataques
//  interesantes? prácticamente solo usaste cosas básicas para los
//  proyectiles».
//
//  LA HERRAMIENTA ES ESTA: el motor de píxel de la casa (PixelFxCore
//  — el mismo que viste a las tres armas del Taller de Efectos: el
//  Sigilo, las Llamas de Sombra y el Eclipse Vivo, con su QA
//  píxel-a-píxel). La v6.98 vistió los proyectiles con capas de textu-
//  ras glow — el vestido — pero el MOTOR no corría en ellos. HOY SÍ:
//  cada uno de los DIEZ proyectiles de las Tres Casas (los ocho platos
//  y sus dos hijos) lleva DENTRO un efecto del motor de píxel vivo,
//  forjado con su propia semilla y TEÑIDO con los dos colores de su
//  arma — y encima siguen las capas glow: el núcleo es ARTE de píxel
//  animado, el vestido es la luz.
//
//  LAS OCHO PUERTAS (una por plato — el lienzo, el origen y el loop
//  de cada una están en su PxMeta):
//   · EL EMISOR DEL HAZ      — el corazón rúnico que el portador
//     enciende en las manos: runas que despiertan una a una, chispas
//     que caen en espiral hacia el núcleo, el núcleo al blanco.
//   · EL CORAZÓN DE LA CORONA — la estrella de cuatro puntas que late
//     en el pecho del portador, con sus dos aros rúnicos girando en
//     contra y sus tres motes orbitando.
//   · EL VELO DE LA LLUVIA   — el círculo del suelo: las promesas que
//     caen del cielo, el impacto con su metralla parabólica y la
//     quemadura que respira.
//   · LA NOVA (el acto)      — el acto entero: la convergencia rúnica,
//     el fogonazo, el anillo de choque y las ascuas que caen — 1,25 s
//     sincronizados con los 70 ticks del proyectil (20 de carga).
//   · LA GARGANTA DEL VÓRTICE — el corazón negro: el horizonte de
//     sucesos con su aro de fotones, los tres brazos de acreción
//     cayendo hacia adentro y las estrellas del fondo doblándose.
//   · EL NIDO DEL ENJAMBRE   — las siete celdas hexagonales: la ola
//     que las enciende, el lucero que eclosiona y se va, las motes
//     que se escapan.
//   · LA HOJA ESTELA         — la hoja creciente de píxel ARTUDo:
//     cuerpo, filo blanco y joya — girando con el proyectil (el
//     pintor rota el arte con options.Angle) y soltando chispas.
//   · EL CHARCO DE LA ZONA   — la piscina que hierve: bandas de ruido
//     en la superficie, burbujas que suben y revientan, el géiser y
//     las runas del borde.
//  Y LOS DOS HIJOS:
//   · LA HOJA QUE CAE  — la mini creciente que gira al caer.
//   · EL LUCERO QUE CAZA — la estrella de cuatro puntas con su anillo
//     de caza y su cruz que titila.
//
//  LA SEMILLA es del ARMA (estilo×37+variante): todos los proyectiles
//  de la misma arma comparten coreografía — el arma TIENE un ataque
//  propio, no una lotería. La RAMPATINTA se teje con los dos colores
//  del arma: 8 tonos del negro teñido al blanco teñido pasando por
//  sus dos colores — cada efecto nace VISTIENDO a su arma.
//
//  TODO local, determinista, sin una gota de código ajeno: la ley de
//  la casa (0 IP).
// ======================================================================

namespace AethonMod.Content.VFX
{
    // ==================================================================
    //  LA RUNA DE LA CASA — la firma glífica del motor (la espina y sus
    //  ramas sobre la rejilla 3×3, como los glifos del Sigilo): nace
    //  determinista de (semilla, índice, sal) UNA sola vez.
    // ==================================================================
    internal readonly struct PxRuna
    {
        public readonly double Ang;    // dónde vive en su anillo
        public readonly double[] Segs; // [x0,y0,x1,y1, …] en la rejilla 3×3

        public PxRuna(double ang, double[] segs) { Ang = ang; Segs = segs; }

        /// <summary>FORJAR una rejilla de runas: n espinas con sus
        /// ramas, repartidas en el anillo, cada una con SU dado.</summary>
        public static PxRuna[] Anillo(int n, int seed, int sal)
        {
            var rs = new PxRuna[n];
            for (int i = 0; i < n; i++)
            {
                PxMath.Rng r = PxMath.RngOf3(seed, i, sal);
                var segs = new System.Collections.Generic.List<double> { 1, 0, 1, 2 };   // la espina
                int ramas = 2 + (r.Next() < 0.5 ? 1 : 0);
                for (int k = 0; k < ramas; k++)
                {
                    int ax = r.Int(0, 2), ay = r.Int(0, 2);
                    int bx = Math.Max(0, Math.Min(2, ax + r.Int(-1, 1)));
                    int by = Math.Max(0, Math.Min(2, ay + r.Int(-1, 1)));
                    if (ax != bx || ay != by) { segs.Add(ax); segs.Add(ay); segs.Add(bx); segs.Add(by); }
                }
                double ang = -Math.PI / 2 + i * PxMath.TAU / n;
                rs[i] = new PxRuna(ang, segs.ToArray());
            }
            return rs;
        }

        /// <summary>PINTAR la runa en (cx,cy) con su color — la rejilla
        /// 3×3 cabe en ~4 px de arte.</summary>
        public void Pintar(PxPainter p, double cx, double cy, uint c, double esc = 1.5)
        {
            double[] s = Segs;
            for (int k = 0; k < s.Length; k += 4)
                p.Line(cx + (s[k] - 1) * esc, cy + (s[k + 1] - 1) * esc,
                       cx + (s[k + 2] - 1) * esc, cy + (s[k + 3] - 1) * esc, c);
        }
    }

    // ==================================================================
    //  1) PxEmisorHaz — EL EMISOR DEL HAZ. El corazón rúnico que el
    //     portador enciende: 8 runas que despiertan una a una y se
    //     apagan, chispas que caen en espiral hacia el núcleo, el
    //     núcleo al blanco que respira. Lienzo 96×96, origen [48,48]
    //     (el pecho del portador), loop 1,6 s.
    // ==================================================================
    internal sealed class PxEmisorHaz : IPxFx, IPxFxBack
    {
        public static readonly PxMeta Meta = new PxMeta(96, 96, 48, 48, 1.6, loop: true, fps: 30, seed: 41);
        const double D = 1.6;
        readonly PxRuna[] Runas = PxRuna.Anillo(8, 41, 0x5A1);

        public void DrawBack(PxPainter p, double t, PxOptions o)
        {
            uint[] C = PxColor.Fit(o.Palette, 8);
            // el halo que sostiene el acto + el aro exterior punteado girando
            p.Glow(0, 0, 42, C[4], 0.30);
            double giro = PxMath.TAU * t / D;
            for (int i = 0; i < 20; i++)
            {
                double a = giro * 0.35 + i * PxMath.TAU / 20;
                p.Dot(Math.Cos(a) * 42, Math.Sin(a) * 42, C[3], 0.55 + 0.45 * PxMath.Rand2(i, 7));
            }
        }

        public void Draw(PxPainter p, double t, PxOptions o)
        {
            uint[] C = PxColor.Fit(o.Palette, 8);
            // LAS RUNAS: cada una despierta en su fase del loop — blanca
            // al despertar, enfriándose a su tono, y un tirón de luz
            // (el QA r2: el ciclo de despertar corre ×2 — se LEE)
            for (int i = 0; i < Runas.Length; i++)
            {
                double fase = (t / D * 4 + i / (double)Runas.Length) % 1.0;
                double despierta = 1 - fase;
                double x = Math.Cos(Runas[i].Ang) * 32, y = Math.Sin(Runas[i].Ang) * 32;
                uint col = despierta > 0.82 ? PxColor.WHITE : despierta > 0.6 ? C[7] : C[5];
                Runas[i].Pintar(p, x, y, col);
                if (despierta > 0.6) p.Glow(x, y, 8, C[5], 0.45 * (despierta - 0.6));
            }
            // LAS CHISPAS QUE CAEN EN ESPIRAL hacia el núcleo (deterministas)
            PxCycle.Run(t, D, 9, 43, r => r.Range(0.55, 0.95), (age, L, r) =>
            {
                double u = age / L, a0 = r.Range(0, PxMath.TAU);
                double a = a0 + u * 5.5, rad = 44 * (1 - u) + 9;
                p.Dot(Math.Cos(a) * rad, Math.Sin(a) * rad, u < 0.25 ? C[7] : u < 0.6 ? C[6] : C[4], 0.9 * (1 - u * 0.4));
                if (u > 0.8) p.Glow(Math.Cos(a) * rad, Math.Sin(a) * rad, 5, C[5], 0.5 * (u - 0.8));
            });
            // EL NÚCLEO: el corazón que respira al blanco (el QA r2:
            // la respiración se DOBLÓ — 9±2,4 y el sparkle con él)
            double late = 1 + 0.26 * Math.Sin(PxMath.TAU * t / D * 2);
            p.Circle(0, 0, 9 * late, (d, _) => d < 0.4 ? PxColor.WHITE : d < 0.75 ? C[7] : C[5], (_, _) => 1);
            p.Glow(0, 0, 26 * late, C[5], 0.55);
            PxSim.Sparkle(p, 0, 0, 5 + 3 * Math.Sin(PxMath.TAU * t / D * 2), C[7], C[5], 0.7);
        }
    }

    // ==================================================================
    //  2) PxCorazonCorona — EL CORAZÓN DE LA CORONA. La estrella de
    //     cuatro puntas que late en el pecho del portador, abrazada
    //     por dos aros rúnicos que giran en contra, con sus tres motes
    //     orbitando y dejando rastro. Lienzo 96×96, origen [48,48],
    //     loop 2,4 s.
    // ==================================================================
    internal sealed class PxCorazonCorona : IPxFx, IPxFxBack
    {
        public static readonly PxMeta Meta = new PxMeta(96, 96, 48, 48, 2.4, loop: true, fps: 30, seed: 52);
        const double D = 2.4;

        public void DrawBack(PxPainter p, double t, PxOptions o)
        {
            uint[] C = PxColor.Fit(o.Palette, 8);
            p.Glow(0, 0, 40, C[3], 0.26);
            // LOS DOS AROS PUNTEADOS que giran en contra
            double g = PxMath.TAU * t / D;
            for (int i = 0; i < 16; i++)
            {
                double a = g + i * PxMath.TAU / 16;
                p.Dot(Math.Cos(a) * 38, Math.Sin(a) * 38, C[4], 0.7);
            }
            for (int i = 0; i < 12; i++)
            {
                double a = -g * 1.6 + i * PxMath.TAU / 12;
                p.Dot(Math.Cos(a) * 30, Math.Sin(a) * 30, C[5], 0.6 + 0.4 * PxMath.Rand2(i, 3));
            }
        }

        public void Draw(PxPainter p, double t, PxOptions o)
        {
            uint[] C = PxColor.Fit(o.Palette, 8);
            double late = 14 + 3.4 * Math.Sin(PxMath.TAU * t / D);
            // LA ESTRELLA DE CUATRO PUNTAS (la firma de la casa:
            // máx + un eco del mínimo — puntas afiladas)
            p.Shade(-late, -late, late, late, (x, y, luz) =>
            {
                double m = Math.Max(Math.Abs(x), Math.Abs(y)) + 0.42 * Math.Min(Math.Abs(x), Math.Abs(y));
                if (m > late) return 0;
                double u = m / late;
                if (((int)Math.Floor(x) + (int)Math.Floor(y) & 1) == 0 && u > 0.3) luz.k = 0.16 * (1 - u);
                return u < 0.22 ? PxColor.WHITE : u < 0.55 ? C[7] : C[5];
            });
            p.Glow(0, 0, 24, C[5], 0.5);
            // LAS TRES MOTES ORBITANDO con su rastro y su pulso propio
            for (int i = 0; i < 3; i++)
            {
                double a = PxMath.TAU * t / D * 2 + i * PxMath.TAU / 3;
                double pulso = 0.55 + 0.45 * Math.Sin(PxMath.TAU * t / D * 3 + i * 2.1);
                for (int k = 0; k < 4; k++)
                {
                    double am = a - k * 0.14;
                    p.Dot(Math.Cos(am) * 21, Math.Sin(am) * 21, k == 0 ? PxColor.WHITE : C[6], (1 - k * 0.24) * pulso);
                }
                p.Glow(Math.Cos(a) * 21, Math.Sin(a) * 21, 7, C[5], 0.5 * pulso);
            }
            PxSim.Sparkle(p, 0, 0, 4 + 1.5 * Math.Sin(PxMath.TAU * t / D * 2), C[7], C[5], 0.55);
        }
    }

    // ==================================================================
    //  3) PxVeloLluvia — EL VELO DE LA LLUVIA. El círculo del suelo
    //     donde caen las hojas: las promesas (líneas que caen del
    //     cielo), el IMPACTO con su metralla parabólica y su anillo, y
    //     la quemadura que respira entre golpe y golpe. Lienzo
    //     128×96, origen [64,84] (EL SUELO), loop 2 s.
    // ==================================================================
    internal sealed class PxVeloLluvia : IPxFx, IPxFxBack
    {
        public static readonly PxMeta Meta = new PxMeta(128, 96, 64, 84, 2.0, loop: true, fps: 30, seed: 63);
        const double D = 2.0;

        public void DrawBack(PxPainter p, double t, PxOptions o)
        {
            uint[] C = PxColor.Fit(o.Palette, 8);
            // la quemadura elíptica + el aire cargado (el QA: el
            // suelo NUNCA baja del borde del lienzo — la lección)
            p.Ellipse(0, 0, 44, 8, (d, _) => d < 0.7 ? C[2] : C[3], (_, _) => 0.6);
            p.Glow(0, -8, 48, C[3], 0.28);
        }

        public void Draw(PxPainter p, double t, PxOptions o)
        {
            uint[] C = PxColor.Fit(o.Palette, 8);
            // LAS RUNAS DEL BORDE girando despacio (dentro del lienzo:
            // el suelo baja solo hasta +10)
            double g = PxMath.TAU * t / D;
            for (int i = 0; i < 18; i++)
            {
                double a = g * 0.5 + i * PxMath.TAU / 18;
                p.Dot(Math.Cos(a) * 42, 0 + Math.Sin(a) * 9, C[5], 0.5 + 0.5 * PxMath.Rand2(i, 11));
            }
            // LOS TRES IMPACTOS: cada uno con SU punto fijo y su ciclo
            for (int i = 0; i < 3; i++)
            {
                double x = (PxMath.Rand2(i, 21) * 2 - 1) * 36;
                double fase = ((t / D) + PxMath.Rand2(i, 22)) % 1.0;
                if (fase < 0.62)
                {
                    // LA PROMESA: la línea que cae del cielo apuntando al punto
                    double u = fase / 0.62;
                    double y = -72 + u * 66;
                    p.Line(x, y, x, y + 9, u > 0.75 ? C[7] : C[6], 0.85);
                    p.Glow(x, y + 4, 5, C[5], 0.4);
                }
                else
                {
                    // EL IMPACTO: anillo que se abre y metralla parabólica
                    double u = (fase - 0.62) / 0.38;
                    PxShadowLib.RingFast(p, x, 0, 2 + 9 * PxMath.OutCubic(u),
                        Math.Max(1, 2.5 * (1 - u)), u < 0.25 ? PxColor.WHITE : C[6], 0.9 * (1 - u));
                    for (int k = 0; k < 5; k++)
                    {
                        double vx = (PxMath.Rand2(i * 9 + k, 23) * 2 - 1) * 26;
                        double vy = -(14 + PxMath.Rand2(i * 9 + k, 24) * 22);
                        double tt = u * 0.5;
                        p.Dot(x + vx * tt, vy * tt + 0.5 * 110 * tt * tt, tt < 0.1 ? PxColor.WHITE : C[6], 1 - u);
                    }
                    p.Glow(x, 0, 10 * (1 - u), C[5], 0.55 * (1 - u));
                }
            }
            // LAS MOTES DE POLVO que se levantan entre golpe y golpe
            PxCycle.Run(t, D, 7, 64, r => r.Range(0.8, 1.5), (age, L, r) =>
            {
                double u = age / L;
                double x = r.Range(-40, 40) + Math.Sin(u * 5 + r.Range(0, 6)) * 3 * u;
                p.Dot(x, -2 - u * r.Range(10, 26), C[4], 0.5 * (1 - u));
            });
        }
    }

    // ==================================================================
    //  4) PxNovaActo — LA NOVA (el acto). Sincronizada con los 70
    //     ticks del proyectil: 20 ticks de CARGA (0,333 s — las runas
    //     convergen en espiral mientras el núcleo se arma), el
    //     FOGONAZO, y el resto el anillo de choque con sus ascuas
    //     cayendo. Lienzo 128×128, origen [64,64], acto 1,25 s SIN
    //     loop.
    // ==================================================================
    internal sealed class PxNovaActo : IPxFx, IPxFxBack
    {
        public static readonly PxMeta Meta = new PxMeta(128, 128, 64, 64, 1.25, loop: false, fps: 30, seed: 74);
        const double CARGA = 0.333, D = 1.25;
        readonly PxRuna[] Runas = PxRuna.Anillo(8, 74, 0x5A2);

        public void DrawBack(PxPainter p, double t, PxOptions o)
        {
            uint[] C = PxColor.Fit(o.Palette, 8);
            if (t < CARGA)
            {
                double u = t / CARGA;
                p.Glow(0, 0, 30 + 40 * u, C[4], 0.3 + 0.25 * u);
            }
            else
            {
                double u = PxMath.Clamp01((t - CARGA) / (D - CARGA));
                // el humo que queda tras el estallido, abriéndose y muriendo
                PxCycle.Run(t - CARGA, D, 6, 75, r => r.Range(0.7, 1.0), (age, L, r) =>
                {
                    double uu = age / L;
                    double a = r.Range(0, PxMath.TAU);
                    p.Disc(Math.Cos(a) * (10 + uu * 34), Math.Sin(a) * (10 + uu * 34) * 0.8,
                        3 + uu * 6, PxColor.Mix(C[2], C[1], uu), 0.35 * (1 - uu));
                });
            }
        }

        public void Draw(PxPainter p, double t, PxOptions o)
        {
            uint[] C = PxColor.Fit(o.Palette, 8);
            if (t < CARGA)
            {
                // LA CONVERGENCIA: las runas caen en espiral hacia el
                // centro, acelerando, encendéndose al llegar (el QA:
                // nacen a radio 52 — CON su glow de 7 entran JUSTAS en
                // el lienzo de 64: nada se corta en el borde)
                double u = t / CARGA, ease = 1 - Math.Pow(1 - u, 2.6);
                double rad = 40 * (1 - ease) + 12;
                double giro = u * 7;
                for (int i = 0; i < Runas.Length; i++)
                {
                    double a = Runas[i].Ang + giro;
                    double x = Math.Cos(a) * rad, y = Math.Sin(a) * rad;
                    Runas[i].Pintar(p, x, y, rad < 20 ? PxColor.WHITE : C[6]);
                    p.Glow(x, y, 7, C[5], 0.4 + 0.4 * u);
                }
                // chispas SUCCIONADAS hacia el núcleo
                PxCycle.Run(t, CARGA, 10, 76, r => r.Range(0.2, 0.35), (age, L, r) =>
                {
                    double uu = age / L, a = r.Range(0, PxMath.TAU) + uu * 4;
                    double rr = 50 * (1 - uu * uu);
                    p.Dot(Math.Cos(a) * rr, Math.Sin(a) * rr, uu > 0.7 ? PxColor.WHITE : C[6], 0.9);
                });
                // el núcleo que se arma
                p.Circle(0, 0, 3 + 7 * u, (d, _) => d < 0.5 ? PxColor.WHITE : C[6], (_, _) => 1);
                p.Glow(0, 0, 16 + 14 * u, C[5], 0.5 + 0.4 * u);
            }
            else
            {
                double e = t - CARGA;
                // EL FOGONAZO: los primeros 0,12 s lo son TODO
                if (e < 0.12)
                {
                    double fl = 1 - e / 0.12;
                    p.Circle(0, 0, 20 + 14 * fl, (d, _) => d < 0.5 ? PxColor.WHITE : C[7], (_, _) => fl);
                    p.Glow(0, 0, 60 * fl + 20, C[5], 0.9 * fl);
                    PxSim.Sparkle(p, 0, 0, 16 * fl + 4, PxColor.WHITE, C[6], fl);
                }
                // EL ANILLO DE CHOQUE (pixel a pixel, con el dither de la casa)
                double u = PxMath.Clamp01(e / 0.85);
                if (u < 1)
                    PxShadowLib.RingFast(p, 0, 0, 12 + 46 * PxMath.OutCubic(u),
                        Math.Max(1, 3.2 * (1 - u)), u < 0.2 ? PxColor.WHITE : u < 0.55 ? C[6] : C[4], 0.95 * (1 - u * 0.7));
                // LAS ASCUAS que salen y caen (16, deterministas)
                double uu2 = PxMath.Clamp01(e / 0.8);
                for (int k = 0; k < 16; k++)
                {
                    double a = k * PxMath.TAU / 16 + PxMath.Rand2(k, 31) * 0.4;
                    double sp = 30 + PxMath.Rand2(k, 32) * 60;
                    double tt = uu2 * 0.6;
                    double x = Math.Cos(a) * sp * tt, y = Math.Sin(a) * sp * tt + 60 * tt * tt;
                    if (Math.Sqrt(x * x + y * y) > 58) continue;
                    p.Dot(x, y, tt < 0.12 ? PxColor.WHITE : C[6], 1 - uu2);
                }
                // EL CORAZÓN que queda, apagándose
                double apaga = PxMath.Clamp01(1 - e / 0.5);
                if (apaga > 0)
                {
                    p.Circle(0, 0, 8, (d, _) => d < 0.4 ? PxColor.WHITE : C[6], (_, _) => apaga);
                    p.Glow(0, 0, 22, C[5], 0.5 * apaga);
                }
            }
        }
    }

    // ==================================================================
    //  5) PxGargantaVortice — LA GARGANTA DEL VÓRTICE. El corazón
    //     negro: el horizonte de sucesos (el disco negro con su aro de
    //     fotones y su Unlight), los TRES BRAZOS de acreción cayendo
    //     hacia adentro — cada vez más rápidos y más blancos cerca del
    //     borde — y las estrellas del fondo que se doblan. Lienzo
    //     128×128, origen [64,64], loop 2,8 s.
    // ==================================================================
    internal sealed class PxGargantaVortice : IPxFx, IPxFxBack
    {
        public static readonly PxMeta Meta = new PxMeta(128, 128, 64, 64, 2.8, loop: true, fps: 30, seed: 85);
        const double D = 2.8, RH = 16;

        public void DrawBack(PxPainter p, double t, PxOptions o)
        {
            uint[] C = PxColor.Fit(o.Palette, 8);
            p.Glow(0, 0, 56, C[3], 0.30);
            // LAS ESTRELLAS DEL FONDO que se doblan: cada una titila y
            // su luz se curva alrededor de la garganta
            for (int i = 0; i < 16; i++)
            {
                double a = PxMath.Rand2(i, 41) * PxMath.TAU;
                double r = 30 + PxMath.Rand2(i, 42) * 28;
                double titila = 0.4 + 0.6 * (0.5 + 0.5 * Math.Sin(PxMath.TAU * t / D * (1 + PxMath.Rand2(i, 43)) + i));
                double x = Math.Cos(a) * r, y = Math.Sin(a) * r;
                p.Dot(x, y, C[6], titila);
                // el arco de su luz doblada: un toque tangencial hacia atrás
                double tang = a + Math.PI / 2 + 0.5;
                p.Dot(x + Math.Cos(tang) * 2.5, y + Math.Sin(tang) * 2.5, C[4], titila * 0.5);
            }
        }

        public void Draw(PxPainter p, double t, PxOptions o)
        {
            uint[] C = PxColor.Fit(o.Palette, 8);
            // LOS TRES BRAZOS DE ACRECIÓN: píxeles en espiral cayendo —
            // cuanto más cerca del borde, más rápidos y más blancos
            double giro = PxMath.TAU * t / D;
            for (int brazo = 0; brazo < 3; brazo++)
            {
                for (int s = 0; s < 26; s++)
                {
                    double a = giro * (1.6 + 2.2 * (1 - s / 26.0)) + brazo * PxMath.TAU / 3 + s * 0.26;
                    double r = RH + 2 + s * 1.85;
                    if (r > 58) break;
                    double cerca = 1 - s / 26.0;
                    uint col = s < 3 ? PxColor.WHITE : s < 8 ? C[7] : s < 16 ? C[5] : C[3];
                    p.Dot(Math.Cos(a) * r, Math.Sin(a) * r * 0.94, col, 0.35 + 0.65 * cerca);
                    if (s < 18)
                        p.GlowLine(Math.Cos(a) * r, Math.Sin(a) * r * 0.94,
                            Math.Cos(a - 0.26) * (r - 1.85), Math.Sin(a - 0.26) * (r - 1.85) * 0.94,
                            2.2, C[4], 0.10 * cerca);
                }
            }
            // LAS CHISPAS QUE CAEN dentro (deterministas, espiral cerrada)
            PxCycle.Run(t, D, 10, 86, r => r.Range(0.5, 0.9), (age, L, r) =>
            {
                double u = age / L, a0 = r.Range(0, PxMath.TAU);
                double a = a0 + u * 6 + giro * 0.4;
                double r0 = (56 - RH) * (1 - u * u) + RH + 2;
                p.Dot(Math.Cos(a) * r0, Math.Sin(a) * r0 * 0.94, u > 0.75 ? PxColor.WHITE : C[6], 0.9);
            });
            // EL HORIZONTE: el disco NEGRO con su aro de fotones fino
            double fl = 0.85 + 0.15 * PxMath.Noise2(t * 2.4, 0, 87);
            p.Circle(0, 0, RH, C[0], 1);
            PxShadowLib.RingFast(p, 0, 0, RH - 0.5, 1.3, PxColor.WHITE, 0.95 * fl);
            PxShadowLib.Halo(p, 0, 0, RH - 0.5, RH + 7, C[5], 0.5 * fl);
            PxShadowLib.Unlight(p, -RH, -RH, RH, RH, (x, y) => x * x + y * y < (RH - 1.2) * (RH - 1.2));
            p.Glow(0, 0, 34, C[4], 0.4 * fl);
        }
    }

    // ==================================================================
    //  6) PxNidoColmena — EL NIDO DEL ENJAMBRE. Las siete celdas
    //     hexagonales (la del centro más grande): la OLA que las
    //     enciende una por una, el LUCERO que eclosiona de cada celda
    //     encendida y se va, las motes que se escapan por los bordes.
    //     Lienzo 96×96, origen [48,48], loop 2,2 s.
    // ==================================================================
    internal sealed class PxNidoColmena : IPxFx, IPxFxBack
    {
        public static readonly PxMeta Meta = new PxMeta(96, 96, 48, 48, 2.2, loop: true, fps: 30, seed: 96);
        const double D = 2.2;

        static void Hexagono(PxPainter p, double cx, double cy, double r, uint c, double a)
        {
            for (int k = 0; k < 6; k++)
            {
                double a0 = k * PxMath.TAU / 6 - Math.PI / 6, a1 = (k + 1) * PxMath.TAU / 6 - Math.PI / 6;
                p.Line(cx + Math.Cos(a0) * r, cy + Math.Sin(a0) * r,
                       cx + Math.Cos(a1) * r, cy + Math.Sin(a1) * r, c, a);
            }
        }

        public void DrawBack(PxPainter p, double t, PxOptions o)
        {
            uint[] C = PxColor.Fit(o.Palette, 8);
            p.Glow(0, 4, 40, C[3], 0.28);
            p.Ellipse(0, 20, 24, 6, C[1], 0.5);   // la sombra del nido
        }

        public void Draw(PxPainter p, double t, PxOptions o)
        {
            uint[] C = PxColor.Fit(o.Palette, 8);
            // LA OLA: qué celda está encendida ahora (0..6, la del
            // centro es la 0 y late cada vuelta; las 6 de alrededor la
            // siguen)
            double fase = (t / D * 2) % 1.0;
            for (int i = 0; i < 7; i++)
            {
                double cx = i == 0 ? 0 : Math.Cos((i - 1) * PxMath.TAU / 6 - Math.PI / 6) * 21;
                double cy = i == 0 ? 4 : 4 + Math.Sin((i - 1) * PxMath.TAU / 6 - Math.PI / 6) * 21;
                double r = i == 0 ? 13 : 11;
                // la fase de ESTA celda: la ola llega en orden
                double fi = (fase + i / 7.0) % 1.0;
                bool encendida = fi < 0.16;
                double calor = encendida ? 1 - fi / 0.16 : 0;
                Hexagono(p, cx, cy, r, encendida ? (calor > 0.5 ? PxColor.WHITE : C[7]) : C[5], 0.9);
                // el relleno: un tono dentro que respira
                Hexagono(p, cx, cy, r - 2.2, C[2], 0.35 + 0.3 * Math.Sin(PxMath.TAU * t / D + i) + (encendida ? 0.3 * calor : 0));
                if (encendida)
                {
                    p.Glow(cx, cy, 12 * calor + 4, C[5], 0.6 * calor);
                    // EL LUCERO QUE ECOLLISIONA: sube desde la celda y se apaga
                    double u = 1 - calor;
                    double y = cy - u * 13;
                    p.Dot(cx, y, u < 0.3 ? PxColor.WHITE : C[6], calor);
                    p.Dot(cx - 1.5, y, C[5], calor * 0.6);
                    p.Dot(cx + 1.5, y, C[5], calor * 0.6);
                }
            }
            // LAS MOTES que se escapan del nido y se disuelven
            PxCycle.Run(t, D, 8, 97, r => r.Range(0.7, 1.2), (age, L, r) =>
            {
                double u = age / L;
                double a = r.Range(0, PxMath.TAU);
                double x = Math.Cos(a) * 24 + Math.Cos(a) * u * 16 + Math.Sin(u * 5 + a) * 2;
                double y = 4 + Math.Sin(a) * 24 + Math.Sin(a) * u * 16 - u * 6;
                p.Dot(x, y, C[4], 0.6 * (1 - u));
            });
            p.Glow(0, 4, 22, C[4], 0.35);
        }
    }

    // ==================================================================
    //  7) PxHojaEstela — LA HOJA ESTELA. La hoja creciente HECHA píxel
    //     a píxel: el cuerpo del arco, el FILO blanco en el borde
    //     exterior, las puntas encendidas y la joya del centro —
    //     girando con el proyectil (el pintor rota el arte con
    //     options.Angle) y soltando chispas por la punta trasera.
    //     Lienzo 72×72, origen [36,36], loop 0,55 s (el vaivén).
    // ==================================================================
    internal sealed class PxHojaEstela : IPxFx
    {
        public static readonly PxMeta Meta = new PxMeta(72, 72, 36, 36, 0.55, loop: true, fps: 30, seed: 107);
        const double D = 0.55, A0 = -2.62, A1 = -0.52;   // el barrido del arco (150°)

        public void Draw(PxPainter p, double t, PxOptions o)
        {
            uint[] C = PxColor.Fit(o.Palette, 8);
            double late = 1 + 0.05 * Math.Sin(PxMath.TAU * t / D);
            double rExt = 21 * late, rInt = 13 * late;
            // EL DESTELLO DEL FILO: dos veces por loop el filo blanco
            // se ensancha — la hoja «muerde» (el QA: vida visible)
            double muerde = 0.5 + 0.5 * Math.Sin(PxMath.TAU * t / D * 2);
            // LA CRECIENTE: el anillo parcial de píxel — el cuerpo, el
            // filo (borde exterior BLANCO) y las puntas encendidas
            p.Shade(-rExt, -rExt, rExt, rExt, (x, y, luz) =>
            {
                double r = Math.Sqrt(x * x + y * y);
                if (r < rInt || r > rExt) return 0;
                double a = Math.Atan2(y, x);
                if (a < A0 || a > A1) return 0;
                double u = (r - rInt) / (rExt - rInt);
                // las puntas (cerca de los extremos del arco) van al blanco
                double punta = Math.Min(Math.Abs(a - A0), Math.Abs(A1 - a));
                if (punta < 0.3) return PxColor.WHITE;
                if (((int)Math.Floor(x) + (int)Math.Floor(y) & 1) == 0) luz.k = 0.18;
                return u > 0.72 - 0.14 * muerde ? C[7] : u > 0.3 ? C[5] : C[3];
            });
            // LA JOYA del centro + su luz
            p.Circle(0, 0, 2.6, (d, _) => d < 0.45 ? PxColor.WHITE : C[6], (_, _) => 1);
            p.Glow(0, 0, 15, C[5], 0.5);
            // LAS CHISPAS de la punta trasera (deterministas, titilan —
            // con su glow: el rastro que se VE)
            double aT = A0;
            for (int i = 0; i < 3; i++)
            {
                double rr = rExt - 2 - i * 3.5;
                double titi = 0.4 + 0.6 * (0.5 + 0.5 * Math.Sin(PxMath.TAU * t / D * 3 + i * 2.4));
                double sx = Math.Cos(aT - 0.12 * i) * rr, sy = Math.Sin(aT - 0.12 * i) * rr;
                p.Dot(sx, sy, i == 0 ? PxColor.WHITE : C[6], titi * (1 - i * 0.28));
                if (i == 0) p.Glow(sx, sy, 4, C[5], 0.5 * titi);
            }
            // el rastro: dos puntos muertos detrás del giro (el QA GIF:
            // el tercero se leía como residuo suelto — fuera)
            for (int i = 1; i <= 2; i++)
                p.Dot(Math.Cos(A1 + 0.3 * i) * (rInt + 3), Math.Sin(A1 + 0.3 * i) * (rInt + 3), C[4], 0.45 - i * 0.17);
        }
    }

    // ==================================================================
    //  8) PxCharcoZona — EL CHARCO DE LA ZONA. La piscina que hierve
    //     en el suelo: la superficie de BANDAS DE RUIDO que corren, las
    //     BURBUJAS que suben desde el fondo y revientan en la piel, el
    //     GÉISER que dispara y cae, y las runas del borde. Lienzo
    //     128×96, origen [64,84] (EL SUELO), loop 3 s.
    // ==================================================================
    internal sealed class PxCharcoZona : IPxFx, IPxFxBack
    {
        public static readonly PxMeta Meta = new PxMeta(128, 96, 64, 84, 3.0, loop: true, fps: 30, seed: 118);
        const double D = 3.0;

        public void DrawBack(PxPainter p, double t, PxOptions o)
        {
            uint[] C = PxColor.Fit(o.Palette, 8);
            p.Ellipse(0, 1, 50, 8, C[1], 0.55);       // el fondo oscuro del charco (el QA: dentro del lienzo)
            p.Glow(0, -6, 56, C[3], 0.30);
            // EL HUMO que se levanta lento del borde
            PxCycle.Run(t, D, 5, 119, r => r.Range(1.4, 2.2), (age, L, r) =>
            {
                double u = age / L;
                double x = r.Range(-44, 44) + Math.Sin(u * 4 + r.Range(0, 6)) * 4 * u;
                p.Disc(x, -6 - u * r.Range(26, 44), 2.5 + u * 5, PxColor.Mix(C[2], C[1], u), 0.30 * (1 - u));
            });
        }

        public void Draw(PxPainter p, double t, PxOptions o)
        {
            uint[] C = PxColor.Fit(o.Palette, 8);
            // LA SUPERFICIE: bandas de ruido que corren sobre la elipse
            // (el QA: el charco aplastado — ry 8 — para no cortarse)
            p.Shade(-52, -12, 52, 10, (x, y, luz) =>
            {
                double e = Math.Sqrt((x * x) / (52.0 * 52.0) + (y * y) / (8.5 * 8.5));
                if (e > 1) return 0;
                double n = PxMath.Noise2(x * 0.16 + PxMath.TAU * t / D, y * 0.28, 120);
                if (e > 0.88) return C[6];                       // el borde vivo
                if (e < 0.10) return n > 0.5 ? C[6] : C[5];      // el corazón caliente
                if (((int)Math.Floor(x) + (int)Math.Floor(y) & 1) == 0) luz.k = 0.10;
                return n > 0.62 ? C[4] : n > 0.40 ? C[3] : C[2];
            });
            // LAS BURBUJAS: nacen al fondo, suben, REVIENTAN en la piel
            PxCycle.Run(t, D, 9, 121, r => r.Range(0.6, 1.1), (age, L, r) =>
            {
                double u = age / L;
                double bx = r.Range(-38, 38);
                if (Math.Abs(bx) > 40) return;
                double by = -2 - u * 9;
                if (u > 0.9)
                {
                    // EL POP: un mini anillo y nada
                    double w = (u - 0.9) / 0.1;
                    PxShadowLib.RingFast(p, bx, by, 1 + 2.4 * w, 1, C[6], 1 - w);
                    return;
                }
                p.Circle(bx, by, 1.4, u > 0.75 ? PxColor.WHITE : C[6], 0.8 * (1 - u * 0.3));
                p.Dot(bx, by + 0.6, C[7], 0.5);
            });
            // EL GÉISER: dispara su columna y las gotas caen de vuelta
            PxCycle.Run(t, D, 2, 122, r => r.Range(0.55, 0.8), (age, L, r) =>
            {
                double u = age / L;
                double gx = r.Range(-24, 24);
                if (u < 0.35)
                {
                    double v = PxMath.OutCubic(u / 0.35);
                    for (int k = 0; k < 5; k++)
                        p.Dot(gx + Math.Sin(k * 1.7) * 1.5, -2 - v * (30 - k * 4), k < 2 ? PxColor.WHITE : C[6], 1 - k * 0.14);
                }
                else
                {
                    double v = (u - 0.35) / 0.65;
                    for (int k = 0; k < 4; k++)
                    {
                        double tt = v * 0.5;
                        p.Dot(gx + (k - 1.5) * 6 * v, -26 * v + 40 * tt * tt + 2, C[6], (1 - v) * (1 - k * 0.18));
                    }
                }
            });
            // LAS RUNAS DEL BORDE girando despacio
            double g = PxMath.TAU * t / D;
            for (int i = 0; i < 16; i++)
            {
                double a = g * 0.4 + i * PxMath.TAU / 16;
                p.Dot(Math.Cos(a) * 46, Math.Sin(a) * 7.5, C[5], 0.45 + 0.55 * PxMath.Rand2(i, 51));
            }
            p.Glow(0, -4, 30, C[5], 0.4);
        }
    }

    // ==================================================================
    //  9) PxHojaCae — LA HOJA QUE CAE (hija de la lluvia). La mini
    //     creciente: dos tonos, el filo blanco y su chispa — girando
    //     al caer. Lienzo 56×56, origen [28,28], loop 0,42 s.
    // ==================================================================
    internal sealed class PxHojaCae : IPxFx
    {
        public static readonly PxMeta Meta = new PxMeta(56, 56, 28, 28, 0.42, loop: true, fps: 30, seed: 129);
        const double D = 0.42, A0 = -2.7, A1 = -0.44;

        public void Draw(PxPainter p, double t, PxOptions o)
        {
            uint[] C = PxColor.Fit(o.Palette, 8);
            double late = 1 + 0.07 * Math.Sin(PxMath.TAU * t / D);
            double rExt = 15 * late, rInt = 9 * late;
            // EL DESTELLO DEL FILO (el QA: vida visible en la caída)
            double muerde = 0.5 + 0.5 * Math.Sin(PxMath.TAU * t / D * 2);
            p.Shade(-rExt, -rExt, rExt, rExt, (x, y, luz) =>
            {
                double r = Math.Sqrt(x * x + y * y);
                if (r < rInt || r > rExt) return 0;
                double a = Math.Atan2(y, x);
                if (a < A0 || a > A1) return 0;
                double u = (r - rInt) / (rExt - rInt);
                double punta = Math.Min(Math.Abs(a - A0), Math.Abs(A1 - a));
                if (punta < 0.35) return PxColor.WHITE;
                return u > 0.68 - 0.16 * muerde ? C[6] : C[4];
            });
            p.Circle(0, 0, 1.8, (d, _) => d < 0.5 ? PxColor.WHITE : C[6], (_, _) => 1);
            p.Glow(0, 0, 11, C[5], 0.45);
            // SU CHISPA titilante en la punta trasera (con glow)
            double titi = 0.4 + 0.6 * (0.5 + 0.5 * Math.Sin(PxMath.TAU * t / D * 4));
            double sx = Math.Cos(A0) * (rExt - 1.5), sy = Math.Sin(A0) * (rExt - 1.5);
            p.Dot(sx, sy, C[7], titi);
            p.Glow(sx, sy, 3.5, C[5], 0.5 * titi);
            p.Dot(Math.Cos(A0 - 0.22) * (rExt - 4), Math.Sin(A0 - 0.22) * (rExt - 4), C[5], titi * 0.6);
        }
    }

    // ==================================================================
    //  10) PxLuceroCaza — EL LUCERO QUE CAZA (hijo del enjambre). La
    //      estrella de cuatro puntas con su corazón blanco, su cruz
    //      que titila y su ANILLO DE CAZA: tres motes que orbitan
    //      estrechando el cerco. Lienzo 56×56, origen [28,28],
    //      loop 0,6 s.
    // ==================================================================
    internal sealed class PxLuceroCaza : IPxFx
    {
        public static readonly PxMeta Meta = new PxMeta(56, 56, 28, 28, 0.6, loop: true, fps: 30, seed: 130);
        const double D = 0.6;

        public void Draw(PxPainter p, double t, PxOptions o)
        {
            uint[] C = PxColor.Fit(o.Palette, 8);
            double late = 10 + 1.4 * Math.Sin(PxMath.TAU * t / D);
            // LA ESTRELLA de cuatro puntas (el QA: AFILADA — el eco del
            // mínimo bajó a 0,30 para que los brazos sean finos y se
            // lea ESTRELLA, no círculo dentado)
            p.Shade(-late, -late, late, late, (x, y, luz) =>
            {
                double m = Math.Max(Math.Abs(x), Math.Abs(y)) + 0.30 * Math.Min(Math.Abs(x), Math.Abs(y));
                if (m > late) return 0;
                double u = m / late;
                if (((int)Math.Floor(x) + (int)Math.Floor(y) & 1) == 0 && u > 0.3) luz.k = 0.18;
                return u < 0.18 ? PxColor.WHITE : u < 0.45 ? C[6] : C[4];
            });
            p.Glow(0, 0, 15, C[5], 0.55);
            // LA CRUZ que titila (más larga: los brazos de la caza)
            double k = 0.55 + 0.45 * Math.Sin(PxMath.TAU * t / D * 2);
            PxSim.Sparkle(p, 0, 0, 6.5 + 2.5 * k, C[7], C[5], 0.4 + 0.5 * k);
            // EL ANILLO DE CAZA: tres motes orbitando, cerrando el cerco
            for (int i = 0; i < 3; i++)
            {
                double a = PxMath.TAU * t / D * -2 + i * PxMath.TAU / 3;
                double r = 13 + 1.5 * Math.Sin(PxMath.TAU * t / D * 3 + i * 2);
                p.Dot(Math.Cos(a) * r, Math.Sin(a) * r, C[6], 0.85);
                p.Dot(Math.Cos(a - 0.2) * r, Math.Sin(a - 0.2) * r, C[4], 0.45);
            }
        }
    }

    /// <summary>EL FRENTE DE LAS TRES CASAS: cada plato instancia su
    /// efecto del motor con la semilla de su arma y su RAMPATINTA (los
    /// dos colores del arma tejidos en 8 tonos).</summary>
    internal static class PixelFxCasas
    {
        /// <summary>LA RAMPATINTA: del negro teñido al blanco teñido,
        /// pasando por los DOS colores del arma. EL ORDEN LO DECIDE LA
        /// LUZ (la lección del QA de la casa: la casa sagrada trae el
        /// oro y la CREMA — y la crema es la CLARA; si el orden fuera
        /// fijo, los tonos medios salían GRISES lavados y la fila
        /// sagrada se leía apagada: el oscuro es SIEMPRE el menos
        /// luminoso de los dos).</summary>
        public static uint[] Rampatinta(uint uno, uint otro)
        {
            double l1 = Luma(uno), l2 = Luma(otro);
            uint oscuro = l1 < l2 ? uno : otro;
            uint claro = l1 < l2 ? otro : uno;
            return new[]
            {
                PxColor.Mix(PxColor.BLACK, oscuro, 0.30),
                PxColor.Mix(PxColor.BLACK, oscuro, 0.60),
                PxColor.Scale(oscuro, 0.80),
                oscuro,
                PxColor.Mix(oscuro, claro, 0.55),
                claro,
                PxColor.Mix(claro, PxColor.WHITE, 0.45),
                PxColor.Mix(claro, PxColor.WHITE, 0.78),
            };
        }

        /// <summary>La luminancia percibida del color (la navaja de la
        /// casa para ordenar la rampa).</summary>
        static double Luma(uint c)
            => 0.299 * PxColor.Red(c) + 0.587 * PxColor.Green(c) + 0.114 * PxColor.Blue(c);

        static PxFxInstance Nacer(IPxFx fx, PxMeta meta, int seed, uint claro, uint oscuro)
        {
            var o = new PxOptions { Seed = seed, Palette = Rampatinta(claro, oscuro), X = meta.Ox, Y = meta.Oy };
            return new PxFxInstance(fx, meta, o);
        }

        public static PxFxInstance EmisorHaz(int seed, uint claro, uint oscuro)
            => Nacer(new PxEmisorHaz(), PxEmisorHaz.Meta, seed, claro, oscuro);

        public static PxFxInstance CorazonCorona(int seed, uint claro, uint oscuro)
            => Nacer(new PxCorazonCorona(), PxCorazonCorona.Meta, seed, claro, oscuro);

        public static PxFxInstance VeloLluvia(int seed, uint claro, uint oscuro)
            => Nacer(new PxVeloLluvia(), PxVeloLluvia.Meta, seed, claro, oscuro);

        public static PxFxInstance NovaActo(int seed, uint claro, uint oscuro)
            => Nacer(new PxNovaActo(), PxNovaActo.Meta, seed, claro, oscuro);

        public static PxFxInstance GargantaVortice(int seed, uint claro, uint oscuro)
            => Nacer(new PxGargantaVortice(), PxGargantaVortice.Meta, seed, claro, oscuro);

        public static PxFxInstance NidoColmena(int seed, uint claro, uint oscuro)
            => Nacer(new PxNidoColmena(), PxNidoColmena.Meta, seed, claro, oscuro);

        public static PxFxInstance HojaEstela(int seed, uint claro, uint oscuro)
            => Nacer(new PxHojaEstela(), PxHojaEstela.Meta, seed, claro, oscuro);

        public static PxFxInstance CharcoZona(int seed, uint claro, uint oscuro)
            => Nacer(new PxCharcoZona(), PxCharcoZona.Meta, seed, claro, oscuro);

        public static PxFxInstance HojaCae(int seed, uint claro, uint oscuro)
            => Nacer(new PxHojaCae(), PxHojaCae.Meta, seed, claro, oscuro);

        public static PxFxInstance LuceroCaza(int seed, uint claro, uint oscuro)
            => Nacer(new PxLuceroCaza(), PxLuceroCaza.Meta, seed, claro, oscuro);
    }
}
