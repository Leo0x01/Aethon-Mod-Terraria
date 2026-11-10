using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace AethonMod.Content.VFX
{
    // ====================================================================
    //  v6.93 — LA LIBRERÍA DE HAZ (la sesión de LA LIMPIEZA DE COPYRIGHT;
    //  la letra: «deberías crear una librería sobre los hazes, una
    //  librería de Haz, con la que se pueda hacer ataques como el arma de
    //  la mirada y como Rayo concentrado 2 sin problemas y así ya no
    //  tendremos más picos molestos»).
    //
    //  QUÉ ES UN HAZ: una banda de luz aditiva que une DOS EMISORES (un
    //  PAR, el estilo «tag-team») o que nace de una boca y se abre hacia
    //  su objetivo (un CONO, el estilo del Rayo Concentrado 2). Un haz
    //  NO es una textura estirada ni una cadena de rectángulos girados:
    //  es una REJILLA de quads — nodos a lo largo del eje × filas a lo
    //  ancho de la banda — cuyo color se decide VÉRTICE a VÉRTICE en la
    //  CPU. La posición, el color y las coordenadas de ley (la «u» del
    //  eje y la «v» de la fila) se calculan en BUFFERS ESTÁTICOS que se
    //  reutilizan de por vida: dibujar un haz no cuesta ni un byte.
    //
    //  LA TÉCNICA ANTI-PICOS (la cura verificada de la v6.91, ahora ley
    //  de la librería): los «picos molestos» del histórico eran píxeles
    //  blancos que se encendían DE GOLPE en el borde del strip — el
    //  fundido vivía en el dominio equivocado (una textura cuyo borde
    //  jamás es matemáticamente cero tras el filtro bilineal). LA CURA
    //  vive en la FÓRMULA, no en el png: EL FILo DE BORDE. En cada
    //  vértice, el rgb se multiplica por
    //        Filo(y) = smoothstep(0.5 → 0.4, |y − 0.5|)
    //  — en el borde de la banda (|y−0.5| = 0.5) el factor es CERO
    //  EXACTO ⇒ rgb = 0 ⇒ el lote aditivo NO APORTA NADA ahí; y la
    //  transición sucede ANTES del borde, con la curva smoothstep (sin
    //  escalones). Con blending de suma, un borde que aporta 0 no puede
    //  encender un píxel: LOS PICOS DE BRILLO SON MATEMÁTICAMENTE
    //  IMPOSIBLES. La librería aplica este filo a TODOS los vértices de
    //  TODAS las formas — el llamador solo dicta el INTERIOR del haz
    //  (su ruido, su núcleo, su recolor); el borde es NEGOCIO DE LA CASA
    //  y de nadie más.
    //
    //  UN DRAW CALL: todas las capas de todos los haces del frame se
    //  apilan en el POZO DE PRIMITIVAS de RayoStrip (triángulos con
    //  color completo por vértice) y se vacían UNA VEZ al Cerrar — el
    //  lote de pantalla soft-add de la casa (dst += rgb·(1−dst)):
    //  la acumulación es asintótica y dos haces cruzados SE SUMAN sin
    //  quemarse en blanco. El pozo se vacía solo si se llena (16.384
    //  vértices): un haz de la Mirada (velo + cuerpo) cabe de sobra.
    //
    //  CERO-GC: pozos y filas estáticos; sin LINQ; sin asignaciones por
    //  frame. Los delegados de ley se pasan como MÉTODOS ESTÁTICOS
    //  guardados en campos static readonly (se alocan UNA vez al cargar
    //  el tipo — el patrón de toda la casa). Las leyes que necesitan
    //  estado del proyectil lo leen de casillas estáticas que el llamador
    //  refresca al empezar su hook: nada de closures capturadoras.
    //
    //  LA GEOMETRÍA ES PROPIA: el eje es una BEZIER CUADRÁTICA CON
    //  TENSIÓN — el punto de control cae en el medio del segmento
    //  desplazado sobre la perpendicular por la COMBA de la cuerda:
    //    · comba 0 ⇒ el control cae EN el medio y la curva degenera
    //      EXACTO en la recta A(1−t)+B·t (la cuerda tensa: la prueba
    //      algebraica está en PrepararEje);
    //    · comba > 0 ⇒ la cuerda cede hacia la perpendicular positiva
    //      (el haz se arquea); comba < 0 cede al otro lado.
    //  NADA de remuestreos ajenos: los nodos se evalúan directos sobre
    //  la curva, uniformes en el parámetro, y el semiancho es el OFFSET
    //  ±perp·semiancho·(2y−1) sobre la perpendicular DEL SEGMENTO.
    //
    //  LAS DOS FORMAS:
    //    · EL PAR — PrepararPar(A, B, …): el segmento entre DOS
    //      emisores; el semiancho por defecto crece de la boca al final
    //      a lo largo del eje (crece con la distancia recorrida), o lo
    //      dicta la LeyAncho del llamador (la Mirada pasa su ley viva).
    //    · EL CONO — PrepararCono(boca, objetivo, …): nace fino en la
    //      boca del emisor y se abre hacia el objetivo con la curva
    //      p^0.8 — la firma del Rayo Concentrado 2 (abre rápido al
    //      nacer y afloja al llegar).
    //
    //  EL EJEMPLO (un ataque de haz en 5 líneas desde un PreDraw):
    //
    //      public override bool PreDraw(ref Color lightColor)
    //      {
    //          HazLib.Abrir();                                    // 1. el lote
    //          HazLib.PrepararCono(Boca, Punta, 4f, 120f, 0f, 48); // 2. el eje
    //          HazLib.Dibujar(MiTinte, null, HazLib.FilasNucleo);  // 3. los vértices
    //          HazLib.Cerrar();                                   // 4. UN draw call
    //          return false;                                      // 5. vanilla no dibuja nada
    //      }
    //
    //  (Preparar* pide el eje; Dibujar lo pinta; la siguiente Preparar*
    //  puede reutilizar los mismos pozos. El filo de borde lo pone
    //  SIEMPRE la librería — el llamador jamás tiene que acordarse de
    //  apagar el borde: esa fue toda la lección de los picos.)
    // ====================================================================
    public static class HazLib
    {
        // ================================================================
        //  LAS LEYES — los delegados del llamador
        // ================================================================

        /// <summary>El SEMIANCHO del eje en el nodo p∈[0,1] (0 = la
        /// boca, 1 = la punta). Devuélvalo ≤0 para matar la banda: la
        /// emisión se salta los quads muertos.</summary>
        public delegate float LeyAncho(float p);

        /// <summary>El TINTE del nodo p∈[0,1] — el color A LO LARGO del
        /// eje (la ley que dicta cómo nace y muere el haz de punta a
        /// punta). Llega a la LeyVertice como colorNodo.</summary>
        public delegate Color LeyNodo(float p);

        /// <summary>El color del VÉRTICE en (x a lo largo, y a lo ancho)
        /// con el tinte del nodo — EL INTERIOR del haz: su ruido, su
        /// núcleo, su recolor. El FILo DE BORDE lo aplica la librería
        /// ENCIMA de lo que devuelva esta ley: devuelva el color ya
        /// saturado y despreocúpese del borde. Null = el tinte tal cual
        /// (un haz liso con el borde fundido).</summary>
        public delegate Color LeyVertice(float x, float y, Color colorNodo);

        // ================================================================
        //  LAS FILAS — el muestreo a lo ancho (la «v» de la ley)
        //
        //  El filo del borde se curva en |y−0.5| ∈ (0.4, 0.5) y las leyes
        //  de la casa curvan en el primer tercio: las filas se CONCENTRAN
        //  DONDE la fórmula se curva y van RALAS por el centro plano. Los
        //  EXTREMOS (y=0/1) son filas de SIEMPRE: ahí la ley del filo da
        //  rgb=0 EXACTO — el borde del haz es matemáticamente invisible.
        // ================================================================

        /// <summary>Las filas del NÚCLEO: densas en el filo del borde
        /// (y 0..0.09: 4 muestras de la curva letal) y en la zona del
        /// recolor/brillo interior (y 0.15..0.33), ralas por el centro.
        /// Para leyes con estructura fuerte a lo ancho.</summary>
        public static readonly float[] FilasNucleo =
        {
            0f, 0.03f, 0.06f, 0.09f,
            0.15f, 0.21f, 0.27f, 0.33f,
            0.40f, 0.47f, 0.50f,
            0.53f, 0.60f, 0.67f, 0.73f, 0.79f, 0.85f,
            0.91f, 0.94f, 0.97f, 1f
        };

        /// <summary>Las filas del VELO: para leyes de cúpula (bumps en
        /// (4y(1−y))^k) que suben rápido del borde y aplanan en el
        /// centro — el halo que envuelve al núcleo.</summary>
        public static readonly float[] FilasVelo =
        {
            0f, 0.04f, 0.08f, 0.14f, 0.22f, 0.32f, 0.42f,
            0.50f,
            0.58f, 0.68f, 0.78f, 0.86f, 0.92f, 0.96f, 1f
        };

        // ================================================================
        //  LOS POZOS — estáticos de por vida (cero-GC)
        // ================================================================

        private const int MaxNodos = 96;

        private static readonly Vector2[] _pos = new Vector2[MaxNodos];   // la posición del nodo
        private static readonly Vector2[] _perp = new Vector2[MaxNodos];  // la perpendicular del segmento
        private static readonly float[] _semi = new float[MaxNodos];      // el semiancho del nodo
        private static readonly float[] _ratio = new float[MaxNodos];     // la «u» del nodo (0..1)
        private static readonly Color[] _tinte = new Color[MaxNodos];     // el tinte del nodo
        private static int _n;

        /// <summary>El vértice liso (la LeyVertice de cuando el llamador
        /// no dicta interior: el tinte tal cual — cacheada, cero-GC).</summary>
        private static readonly LeyVertice Liso = VerticeLiso;

        private static Color VerticeLiso(float x, float y, Color colorNodo) => colorNodo;

        // ================================================================
        //  EL LOTE — el baile de UN draw call
        // ================================================================

        /// <summary>ABRE el lote de haces (anidable): cierra el
        /// SpriteBatch vivo y deja el pozo de primitivas listo. Todos
        /// los haces del frame se acumulan hasta el <see cref="Cerrar"/>.</summary>
        public static void Abrir()
        {
            if (Main.netMode == NetmodeID.Server || Main.dedServ) return;
            RayoStrip.AbrirLote();
            // v6.122 — LA INTENSIDAD DE VIDA se rearma en cada apertura:
            // el llamador que quiera nacer/morir suave la fija DESPUÉS
            // de Abrir y ANTES de Dibujar (quien no la toca, dibuja pleno).
            IntensidadGlobal = 1f;
        }

        /// <summary>
        /// v6.122 — LA INTENSIDAD DE VIDA del haz: el multiplicador global
        /// que el llamador fija entre <see cref="Abrir"/> y
        /// <see cref="Dibujar"/> para que NINGÚN haz nazca ni muera en un
        /// corte brusco (la letra: «sus hazes siempre terminan y empiezan
        /// en un corte brusco, sin efectos»). Viaja con el filo: la luz
        /// se APAGA y también se ADELGAZA — un desvanecimiento de verdad.
        /// </summary>
        public static float IntensidadGlobal = 1f;

        /// <summary>CIERRA el lote: vacía el pozo a pantalla en UN draw
        /// call (el soft-add de la casa) y devuelve el SpriteBatch a
        /// vanilla. Traga sus propias tormentas: el llamador solo hace
        /// try/finally, sin catch.</summary>
        public static void Cerrar()
        {
            if (Main.netMode == NetmodeID.Server || Main.dedServ) return;
            try { RayoStrip.VolcarTriangulos(); }
            catch { }
            finally { RayoStrip.CerrarLote(); }
        }

        // ================================================================
        //  EL EJE — la bezier cuadrática con tensión (geometría propia)
        // ================================================================

        /// <summary>LLena los pozos con el eje entre A y B: nodos
        /// uniformes en el parámetro t (la «u» de la ley ES el t del
        /// nodo — la geometría y la ley viajan sincronizadas, sin
        /// rezagos de remuestreo) y la perpendicular del segmento que
        /// empieza (el último toma la del que termina — la regla del
        /// oficio para que el ancho no gire en la punta).</summary>
        private static void PrepararEje(Vector2 a, Vector2 b, float comba, int nodos)
        {
            if (nodos < 2) nodos = 2;
            if (nodos > MaxNodos) nodos = MaxNodos;
            _n = nodos;

            Vector2 eje = b - a;
            float largo = eje.Length();
            Vector2 dir = largo > 0.001f ? eje * (1f / largo) : Vector2.UnitX;
            Vector2 perp = new Vector2(-dir.Y, dir.X);

            // EL CONTROL: el medio del segmento desplazado por la comba.
            // Con comba 0 el control CAE en el medio y la cuadrática
            // degenera EXACTO en la recta:
            //   (1−t)²A + 2t(1−t)·(A+B)/2 + t²B = A(1−t) + B·t
            // — la cuerda tensa, la LERP pura. La comba es la TENSIÓN
            // que se le suelta a la cuerda.
            Vector2 control = (a + b) * 0.5f + perp * comba;

            for (int i = 0; i < nodos; i++)
            {
                float t = i / (float)(nodos - 1);
                float u = 1f - t;
                _ratio[i] = t;
                _pos[i] = (u * u) * a + (2f * u * t) * control + (t * t) * b;
            }

            for (int i = 0; i < nodos; i++)
            {
                Vector2 d = i < nodos - 1
                    ? _pos[i + 1] - _pos[i]
                    : _pos[i] - _pos[i - 1];
                float l = d.Length();
                // segmento degenerado (eje de punto): la perpendicular del eje entero
                _perp[i] = l > 0.0001f ? new Vector2(-d.Y / l, d.X / l) : perp;
            }
        }

        /// <summary>Aplica la ley de semiancho por nodo (clampeada a ≥0:
        /// lo que baje de 0 es banda muerta).</summary>
        private static void FijarSemianchos(LeyAncho semiancho)
        {
            if (semiancho == null)
            {
                for (int i = 0; i < _n; i++) _semi[i] = 0f;
                return;
            }
            for (int i = 0; i < _n; i++)
            {
                float s = semiancho(_ratio[i]);
                _semi[i] = s > 0f ? s : 0f;
            }
        }

        // ================================================================
        //  LAS DOS FORMAS — la API pública
        // ================================================================

        /// <summary>EL HAZ PAREADO (tag-team): el segmento entre DOS
        /// EMISORES con el semiancho dictado por la LeyAncho del
        /// llamador — la forma de la Mirada del Eterno (su ley viva:
        /// bulto de arranque, latido, cierre y punta redonda).
        /// [comba] la tensión de la cuerda (0 = recta exacta).</summary>
        public static void PrepararPar(Vector2 emisorA, Vector2 emisorB,
            LeyAncho semiancho, float comba, int nodos)
        {
            PrepararEje(emisorA, emisorB, comba, nodos);
            FijarSemianchos(semiancho);
        }

        /// <summary>EL HAZ PAREADO con la ley por defecto: el semiancho
        /// crece de la boca al final A LO LARGO del eje — lineal en la
        /// distancia recorrida (la boca fina, la punta ancha).</summary>
        public static void PrepararPar(Vector2 emisorA, Vector2 emisorB,
            float semianchoBoca, float semianchoFin, float comba, int nodos)
        {
            PrepararEje(emisorA, emisorB, comba, nodos);
            for (int i = 0; i < _n; i++)
            {
                float s = MathHelper.Lerp(semianchoBoca, semianchoFin, _ratio[i]);
                _semi[i] = s > 0f ? s : 0f;
            }
        }

        /// <summary>EL HAZ DE CONO (el estilo del Rayo Concentrado 2):
        /// nace en la BOCA del emisor y se abre hacia el objetivo con la
        /// curva p^0.8 — abre rápido al nacer y afloja al llegar. La
        /// misma ley de borde que el par: el filo es de la librería.</summary>
        public static void PrepararCono(Vector2 boca, Vector2 objetivo,
            float semianchoBoca, float semianchoFin, float comba, int nodos)
        {
            PrepararEje(boca, objetivo, comba, nodos);
            for (int i = 0; i < _n; i++)
            {
                float s = MathHelper.Lerp(semianchoBoca, semianchoFin,
                    MathF.Pow(_ratio[i], 0.8f));
                _semi[i] = s > 0f ? s : 0f;
            }
        }

        // ================================================================
        //  EL DIBUJO — la rejilla de quads con el filo de borde
        // ================================================================

        /// <summary>PINTA el eje preparado: por cada segmento×fila un
        /// quad (2 triángulos) con el color de la LeyVertice en cada
        /// esquina, PRE-MULTIPLICADO por el FILo DE BORDE de la
        /// librería. Los quads degenerados, las bandas muertas y los
        /// quads enteramente invisible-borde se saltan. Todo cae en el
        /// pozo de RayoStrip: se vacía en el <see cref="Cerrar"/> (UN
        /// draw call). [filas] null = FilasNucleo.</summary>
        public static void Dibujar(LeyNodo tinte, LeyVertice vertice, float[] filas = null)
        {
            if (Main.netMode == NetmodeID.Server || Main.dedServ) return;
            if (_n < 2) return;
            if (vertice == null) vertice = Liso;
            if (filas == null || filas.Length < 2) filas = FilasNucleo;

            // LOS TINTES DE LOS NODOS — el color a lo largo del eje,
            // resuelto UNA vez por nodo (la ley a lo ancho lo recibe
            // como colorNodo, como manda el contrato de la LeyVertice).
            if (tinte != null)
            {
                for (int i = 0; i < _n; i++) _tinte[i] = tinte(_ratio[i]);
            }
            else
            {
                for (int i = 0; i < _n; i++) _tinte[i] = Color.White;
            }

            for (int i = 0; i < _n - 1; i++)
            {
                if ((_pos[i + 1] - _pos[i]).LengthSquared() < 0.0001f)
                    continue;                                // el segmento degenerado
                float s0 = _semi[i], s1 = _semi[i + 1];
                if (s0 <= 0.05f && s1 <= 0.05f) continue;    // la banda muerta
                float x0 = _ratio[i], x1 = _ratio[i + 1];
                Vector2 p0 = _pos[i], p1 = _pos[i + 1];
                Vector2 n0 = _perp[i], n1 = _perp[i + 1];
                Color k0 = _tinte[i], k1 = _tinte[i + 1];

                for (int r = 0; r < filas.Length - 1; r++)
                {
                    float y0 = filas[r], y1 = filas[r + 1];
                    // EL OFFSET: ±perp·semiancho·(2y−1) — la banda
                    // ±semiancho sobre la perpendicular del segmento;
                    // y = la «v» de la ley (0/1 SON el borde invisible).
                    Vector2 a0 = p0 + n0 * (s0 * (y0 * 2f - 1f));
                    Vector2 a1 = p0 + n0 * (s0 * (y1 * 2f - 1f));
                    Vector2 b0 = p1 + n1 * (s1 * (y0 * 2f - 1f));
                    Vector2 b1 = p1 + n1 * (s1 * (y1 * 2f - 1f));
                    // EL COLOR: la ley del llamador (el INTERIOR) y
                    // ENCIMA el filo de la casa (el BORDE) — en ese
                    // orden, que el borde apague lo que sea que haga el
                    // interior. Filo(y) solo depende de la fila: una vez
                    // por esquina de fila. v6.122 — y LA INTENSIDAD DE
                    // VIDA viaja con el filo (nacer/morir suave: la luz
                    // se apaga Y se adelgaza — jamás un corte brusco).
                    float f0 = Filo(y0) * IntensidadGlobal, f1 = Filo(y1) * IntensidadGlobal;
                    Color ca0 = PorFactor(vertice(x0, y0, k0), f0);
                    Color ca1 = PorFactor(vertice(x0, y1, k0), f1);
                    Color cb0 = PorFactor(vertice(x1, y0, k1), f0);
                    Color cb1 = PorFactor(vertice(x1, y1, k1), f1);
                    if ((ca0.R | ca0.G | ca0.B) == 0 && (ca1.R | ca1.G | ca1.B) == 0 &&
                        (cb0.R | cb0.G | cb0.B) == 0 && (cb1.R | cb1.G | cb1.B) == 0)
                        continue;                            // el quad entero es borde: invisible
                    RayoStrip.TrianguloColoresEnLote(a0, a1, b1, ca0, ca1, cb1);
                    RayoStrip.TrianguloColoresEnLote(a0, b1, b0, ca0, cb1, cb0);
                }
            }
        }

        // ================================================================
        //  EL FILo — la ley del borde (el corazón anti-picos)
        // ================================================================

        /// <summary>LA LEY DEL BORDE: el factor del rgb en la fila y.
        /// d = |y−0.5| (d=0.5 ES el borde del strip): smoothstep
        /// (0.5 → 0.4) — CERO EXACTO en el borde (rgb=0: el lote
        /// aditivo no enciende NADA) y la transición suave ANTES del
        /// borde. Ningún píxel puede encenderse de golpe: los picos del
        /// histórico son matemáticamente imposibles.</summary>
        private static float Filo(float y)
        {
            float d = MathF.Abs(y - 0.5f);
            float t = d >= 0.5f ? 0f : (d <= 0.4f ? 1f : (0.5f - d) * 10f);
            return t * t * (3f - 2f * t);
        }

        /// <summary>El rgb del color por el factor del filo (cuantizado
        /// a byte con redondeo — el alfa no pinta: el lote aditivo de la
        /// casa suma el rgb, la lección v6.50.26 de la casa).</summary>
        private static Color PorFactor(Color c, float f)
        {
            if (f >= 0.999f) return c;
            return new Color(
                (byte)(c.R * f + 0.5f),
                (byte)(c.G * f + 0.5f),
                (byte)(c.B * f + 0.5f), 255);
        }
    }
}
