using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// RayoStrip — v6.50.25 — EL FILAMENTO DE PRIMITIVAS DE VÉRTICES.
    /// v6.50.26 — LA LECCIÓN DE LA INVESTIGACIÓN (R63-a: rayos reales +
    /// las técnicas del género): (1) EL ALFA DEL VÉRTICE NO ESCALA
    /// EL COLOR — el BasicEffect del motor pasa el COLOR sin pre-multiplicar
    /// y el lote aditivo (One,One) SUMA el rgb TAL CUAL: el alfa por
    /// columna era LETRA MUERTA y la funda salía como BANDA SÓLIDA de
    /// color pleno («el brillo es muy intenso… se rompe en los bordes
    /// cuando toca el brillo de otra línea» — el borde duro de la banda
    /// + el clipeo al cruzarse dos bandas). EL FIX DE RAÍZ: el rgb se
    /// PRE-MULTIPLICA por el factor en el vértice (el contrato de
    /// Color·f de TODA la casa, que RayoStrip violaba). (2) EL BLEND
    /// DE PANTALLA (soft-add): SourceBlend=InverseDestinationColor,
    /// DestBlend=One → dst += rgb·(1−dst): la acumulación es
    /// ASINTÓTICA (0.3→0.51→0.66…) y NUNCA clipea — dos rayos que se
    /// cruzan SE SUMAN SIN ROMPERSE, el fix clásico del «disco blanco
    /// plano» del aditivo puro. (3) EL PERFIL DE LOS RAYOS DE VERDAD:
    /// núcleo fino (~1/5 del ancho total, casi blanco) + halo 4× más
    /// ancho con alfa BAJO (10-26%) — «todo lo que pueda solaparse
    /// debe quedar lejos de 1.0; solo el núcleo único llega a blanco».
    /// (4) ESQUINAS AGUDAS: los rayos reales son zigzag fractal con
    /// KINKS BRUSCOS (stepped leaders) — el Chaikin que redondeaba
    /// cada curva («demasiado redondeado cuando la línea se curva»)
    /// pasa a OPT-IN: solo el canal Perlin (el arco eléctrico de la
    /// vida real, curvatura CONTINUA) lo usa.
    ///
    /// EL DIAGNÓSTICO (el reporte del usuario, literal): «todos los rayos
    /// nuevos presentan el mismo problema, el rayo no es una línea lisa…
    /// la línea del raya está llena de pequeños bultos como puntos
    /// difuminados al rededor de toda la línea… el cetro del trueno
    /// rúnico… su rayo son solo lineas pegadas una a otras, son lineas
    /// discontinuas y se nota que son lineas individuales».
    ///
    /// LA CAUSA (forense de la PILA v6.50.22): el pixel del motor NO podía
    /// dibujar una línea lisa POR CONSTRUCCIÓN — cada pasada era un RECTÁNGULO
    /// SÓLIDO centrado en el punto medio del tramo con la NORMAL MEDIA y la
    /// EXTENSIÓN DE GIRO para tapar la cuña de la esquina: en cada vértice
    /// del zigzag, la extensión del tramo A y la del tramo B se SOLAPABAN
    /// en una cuña → en el lote aditivo esa cuña se dibujaba DOS VECES →
    /// UN PUNTO BRILLANTE EN CADA VÉRTICE («los bultos»). Encima las 6
    /// pasadas telescópicas sumaban ESCALONES (0.06→0.09→0.13→…) que el ojo
    /// leía como bandas, y los gorros de glow de CADA extremo de rama
    /// sembraban puntos difuminados alrededor del canal. El SpriteBatch solo
    /// sabe dibujar RECTÁNGULOS: en una esquina, o hay hueco o hay solape —
    /// no existe la línea lisa con quads.
    ///
    /// LA SOLUCIÓN (la que usa el motor de verdad): TIRAS TRIANGULADAS de
    /// PRIMITIVAS con COLOR POR VÉRTICE — la geometría es CONTINUA de punta
    /// a punta (cada trapecio comparte EXACTAMENTE sus dos vértices con el
    /// siguiente: CERO solapes, CERO huecos, CERO juntas), y el perfil
    /// transversal es la INTERPOLACIÓN LINEAL del color de los vértices de
    /// cada columna (degradado REAL, no escalones de rectángulos apilados).
    /// El rayo del clima de Terraria 1.4.5 se dibuja así (VertexStrip con
    /// shader propio — StormLightningDrawer): «así son los rayos de verdad».
    ///
    ///   · CINCO COLUMNAS por nodo para la funda:  [−1, −0.44, 0, +0.44, +1]·ancho
    ///     con alfa [0.08, 0.44, 0.92, 0.44, 0.08] — gaussiana CONTINUA de
    ///     bordes SUAVES que muere en casi-cero (sin el escalón del borde).
    ///   · TRES columnas para la VENA blanca: [−0.36, 0, +0.36]·ancho con
    ///     alfa [0.30, 0.95, 0.30] — el núcleo caliente con hombros suaves.
    ///   · EL ALISADO CHAIKIN (1 iteración, anclas exactas) deshace las
    ///     esquinas del fractal: el zigzag vive pero la LÍNEA es lisa.
    ///   · EL CRACKLE por vértice (hash de baja frecuencia): el brillo
    ///     respira a lo largo del canal — continuo, no por secciones.
    ///   · CERO SPRITES: ni pixel ni banda ni glow — solo vértices y color.
    ///     (Los gorros de los EXTREMOS siguen siendo sprites de la casa,
    ///     dibujados por el llamador con el lote ABIERTO — son PUNTOS de
    ///     descarga, no la línea.)
    ///
    /// EL LOTE (el contrato de la casa, extendido): los filamentos de
    /// primitivas no pueden vivir dentro de un SpriteBatch abierto — hay
    /// que cerrarlo, poner los estados del dispositivo y dibujar con
    /// BasicEffect (vertex color, sin textura, aditivo). TODOS los
    /// consumidores de rayos de la casa abren el lote con LOS MISMOS
    /// parámetros (Deferred · Additive · LinearClamp · CullNone ·
    /// GameViewMatrix — auditado 10/10 contextos), así que el baile
    /// End→primitivas→Begin es invisible: el aditivo es conmutativo y el
    /// Deferred no ordena. El lote es ANIDABLE (profundidad contada):
    /// MultiBolt/PerlinBolt/RayoSistema envuelven TODO su árbol en UN solo
    /// baile. Reabre con la sonda de la casa (idempotente).
    /// </summary>
    public static class RayoStrip
    {
        // ==================================================================
        //  EL DISPOSITIVO (BasicEffect cacheado + pozo de vértices)
        // ==================================================================

        private static BasicEffect _fx;

        /// <summary>El pozo de vértices (una tira por todas — cero GC).</summary>
        private static VertexPositionColor[] _verts = new VertexPositionColor[16384];

        /// <summary>La profundidad del lote anidado (0 = el lote del juego).</summary>
        private static int _profundidad = 0;

        /// <summary>La profundidad pública (para que el anfitrión decida sus gorros).</summary>
        public static int Profundidad => _profundidad;

        // LAS COLUMNAS DE LA FUNDA (fracción del semiancho · alfa relativo).
        // v6.50.27 — EL HALO DE VERDAD, TENUE (el reporte: «el resplandor
        // o brillo de los rayos sigue siendo muy fuerte y en los rayos
        // grandes el brillo se solapa y se ven como cortes»): la funda era
        // una BANDA GRUESA al 0.62 de intensidad DELANTE del núcleo — al
        // solaparse dos fundas (ramas junto al tronco, arcos cruzados) el
        // salto 0.62→0.86 marcaba el borde de la intersección como un
        // CORTE. La fotografía de rayos reales lo dice claro: el halo
        // acompañante vive al 10-24% — el NÚCLEO manda, el aura susurra.
        // Dos fundas solapadas ahora suman ~0.38: suave, sin borde.
        private static readonly float[] ColF = { -1f, -0.42f, 0f, 0.42f, 1f };
        private static readonly float[] ColA = { 0.03f, 0.10f, 0.24f, 0.10f, 0.03f };

        // LAS COLUMNAS DE LA VENA (el núcleo caliente — el protagonista).
        private static readonly float[] VenF = { -0.30f, 0f, 0.30f };
        private static readonly float[] VenA = { 0.14f, 0.85f, 0.14f };

        // v6.50.26 — EL BLEND DE PANTALLA (soft-add): dst += rgb·(1−dst).
        // La acumulación asintótica NUNCA clipea — el fix del «brillo que
        // se rompe al cruzarse» (receta clásica de los foros de gráficos,
        // verificada en
        // la documentación de MonoGame: Blend.InverseDestinationColor).
        private static readonly BlendState _pantalla = new BlendState
        {
            ColorSourceBlend = Blend.InverseDestinationColor,
            ColorDestinationBlend = Blend.One,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.One,
        };

        // ==================================================================
        //  EL LOTE (el baile End → primitivas → Begin, anidable)
        // ==================================================================

        /// <summary>
        /// ABRE el lote de primitivas: cierra el SpriteBatch vivo (si lo
        /// hay — la sonda de la casa, cero first-chance). Anidable: solo el
        /// PRIMERO cierra; los anidados son no-op.
        /// </summary>
        public static void AbrirLote()
        {
            if (_profundidad == 0) VFXCore.CerrarLoteSiAbierto();
            _profundidad++;
        }

        /// <summary>
        /// CIERRA el lote de primitivas: reabre el SpriteBatch con los
        /// parámetros del lote aditivo de la casa (solo si lo habíamos
        /// cerrado nosotros — idempotente por sonda). Anidable.
        /// </summary>
        public static void CerrarLote()
        {
            if (_profundidad <= 0) return;           // par de guardia (excepción perdida)
            _profundidad--;
            if (_profundidad == 0 && !VFXCore.LoteAbierto)
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None,
                    RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>El número de vértices del pozo que se usan ya (diagnóstico).</summary>
        private static int _nVerts = 0;

        // ==================================================================
        //  v6.52.2 — LA FAJA TEXTURIZADA (la tira del haz de verdad).
        //
        //  LA INVESTIGACIÓN DEL CICLO (la letra: «la nueva arma que se
        //  supone que es de hazes como la de los mods populares esta mal
        //  creada, en vez de un haz de luz con todo el conjunto… son
        //  varias lineas con muchos cuadrados discontinuos»): la receta
        //  de BeamLib era CORRECTA (boca 0.38/6% · punta 0.12 · core
        //  0.42 · diamantes sin⁷/92/9 — todo calca el haz gamma de
        //  la investigación) pero el RENDERER la traicionaba:
        //  VFXCore.Ribbon dibujaba el haz como CADENA DE RECTÁNGULOS
        //  rotados independientes con solape aditivo (bultos), la
        //  textura ENTERA comprimida en cada segmento (cuadrados
        //  periódicos), y los segmentos de <1.5 px SALTADOS (líneas
        //  discontinuas literales). LOS MAESTROS (los haces de
        //  referencia): UNA TIRA CONTINUA con
        //  UV.X = largo de arco (el flujo del ruido HORNEADO en las UVs
        //  — el «shader» resuelto en CPU) y la caída gaussiana VIVA EN
        //  LA TEXTURA (eje V de RuidoTinta: 0 en bordes → centro).
        //
        //  ESTE EMISOR: dos columnas (V 0..1 o la banda que el llamador
        //  pida — el CORE muestrea 0.36..0.64, la vena caliente de la
        //  textura), U = arcs/periodo + tiempo·flujo con LinearWrap (el
        //  ruido AVANZA raíz→punta, continuo de punta a punta), el
        //  color del vértice PRE-MULTIPLICADO como toda la casa.
        // ==================================================================

        private static BasicEffect _fxT;
        private static VertexPositionColorTexture[] _vertsT = new VertexPositionColorTexture[8192];
        private static int _nVertsT;

        /// <summary>
        /// LA FAJA TEXTURIZADA (el baile completo): abre lote, emite la
        /// tira, cierra lote. <paramref name="pts"/> en COORDENADAS DE
        /// PANTALLA (mundo − Main.screenPosition).
        /// </summary>
        /// <param name="pts">La espina del haz (pantalla).</param>
        /// <param name="semiancho">Semiancho por nodo (px — la mitad del ancho total).</param>
        /// <param name="color">El tinte PRE-MULTIPLICADO (StormLib.Tint(color, alfa)).</param>
        /// <param name="tex">La textura del cuerpo (gaussiana en V, ruido en U, wrap).</param>
        /// <param name="periodo">Px de haz por TILE de textura (el periodo del ruido).</param>
        /// <param name="flujo">Tiles por SEGUNDO que avanza la carne (raíz→punta).</param>
        /// <param name="tiempo">El reloj determinista (GlobalTimeWrappedHourly).</param>
        /// <param name="vMin">Banda V inferior (0 para el cuerpo; 0.36 para el core).</param>
        /// <param name="vMax">Banda V superior (1 para el cuerpo; 0.64 para el core).</param>
        public static void FajaTexturada(Vector2[] pts, float[] semiancho, Color color,
            Texture2D tex, float periodo, float flujo, float tiempo,
            float vMin = 0f, float vMax = 1f)
        {
            AbrirLote();
            try { FajaTexturadaEnLote(pts, semiancho, color, tex, periodo, flujo, tiempo, vMin, vMax); }
            finally { CerrarLote(); }
        }

        /// <summary>La faja DENTRO de un lote ya abierto (el patrón de
        /// FilamentoEnLote — BeamLib dibuja sus TRES capas en UN solo baile).</summary>
        public static void FajaTexturadaEnLote(Vector2[] pts, float[] semiancho, Color color,
            Texture2D tex, float periodo, float flujo, float tiempo,
            float vMin = 0f, float vMax = 1f)
            => FajaTexturadaEnLote(pts, semiancho, null, color, tex, periodo, flujo, tiempo, vMin, vMax);

        /// <summary>v6.52.2 — LA FAJA DE GRADIENTE (la firma del
        /// EL RAYO DE FUSIÓN del dios: el color VIAJA A LO LARGO del haz —
        /// coloresPorNodo[i] tiñe cada segmento; null = el color fijo).
        /// EL ARCOÍRIS A LO LARGO es lo que hacía el shader de la referencia
        /// con `ValueAt(localAI[0] − progress·100)` — aquí, resuelto por
        /// vértice en CPU, como todo en la casa.</summary>
        public static void FajaTexturadaEnLote(Vector2[] pts, float[] semiancho,
            Color[] coloresPorNodo, Color color, Texture2D tex,
            float periodo, float flujo, float tiempo,
            float vMin = 0f, float vMax = 1f)
        {
            if (Main.netMode == Terraria.ID.NetmodeID.Server || Main.dedServ) return;
            if (pts == null || pts.Length < 2 || semiancho == null || tex == null || tex.IsDisposed) return;
            if (periodo < 16f) periodo = 16f;
            int n = pts.Length;
            if (semiancho.Length < n) n = semiancho.Length;
            if (coloresPorNodo != null && coloresPorNodo.Length < n) coloresPorNodo = null;
            if (n < 2) return;

            // EL LARGO DE ARCO (el U del ruido — continuo de punta a punta).
            float[] arcs = new float[n];
            float total = 0f;
            for (int i = 1; i < n; i++)
            {
                total += Vector2.Distance(pts[i - 1], pts[i]);
                arcs[i] = total;
            }
            if (total < 1f) return;

            // EL FLUJO: u = arco/periodo − tiempo·flujo → los rasgos del
            // ruido AVANZAN raíz→punta (la «carne que camina» — el
            // uTime NEGATIVO del haz de referencia, horneado en las UVs).
            float uBase = -tiempo * flujo;

            // LA EMISIÓN: un quad por segmento (dos triángulos, vértices
            // COMPARTIDOS con los vecinos — la tira continua de la casa).
            _nVertsT = 0;
            for (int i = 0; i < n - 1; i++)
            {
                float s0 = semiancho[i], s1 = semiancho[i + 1];
                if (s0 <= 0.05f && s1 <= 0.05f) continue;

                // LA NORMAL MEDIA por nodo (la tira es continua por construcción).
                Vector2 d0 = i > 0 ? pts[i] - pts[i - 1] : pts[i + 1] - pts[i];
                Vector2 d1 = i < n - 2 ? pts[i + 2] - pts[i + 1] : pts[i + 1] - pts[i];
                if (d0.LengthSquared() < 0.0001f) d0 = d1;
                if (d1.LengthSquared() < 0.0001f) d1 = d0;
                Vector2 m0 = Vector2.Normalize(Vector2.Normalize(d0) + Vector2.Normalize(d1));
                if (!(m0.LengthSquared() >= 0.001f)) m0 = d1;
                m0 = Vector2.Normalize(m0);
                Vector2 n0 = new(-m0.Y, m0.X);
                Vector2 n1 = n0;   // la normal media del segmento (suficiente a 34+ nodos)

                float u0 = arcs[i] / periodo + uBase;
                float u1 = arcs[i + 1] / periodo + uBase;

                Vector2 a0 = pts[i] + n0 * s0, a1 = pts[i] - n0 * s0;
                Vector2 b0 = pts[i + 1] + n1 * s1, b1 = pts[i + 1] - n1 * s1;

                // EL GRADIENTE A LO LARGO: cada segmento SU color (null = fijo)
                Color c0 = coloresPorNodo != null ? coloresPorNodo[i] : color;
                Color c1 = coloresPorNodo != null ? coloresPorNodo[i + 1] : color;
                EmitirQuadTex(a0, a1, b0, b1, c0, c1, u0, u1, vMin, vMax);
            }
            VolcarTextura(tex);
        }

        /// <summary>Apila el quad texturizado (2 triángulos) en el pozo —
        /// con el color por vértice del gradiente (c0 el nodo i, c1 el i+1).</summary>
        private static void EmitirQuadTex(Vector2 a0, Vector2 a1, Vector2 b0, Vector2 b1,
            Color c0, Color c1, float u0, float u1, float vMin, float vMax)
        {
            if (_nVertsT + 6 > _vertsT.Length) return;
            _vertsT[_nVertsT++] = new VertexPositionColorTexture(new Vector3(a0.X, a0.Y, 0f), c0, new Vector2(u0, vMin));
            _vertsT[_nVertsT++] = new VertexPositionColorTexture(new Vector3(a1.X, a1.Y, 0f), c0, new Vector2(u0, vMax));
            _vertsT[_nVertsT++] = new VertexPositionColorTexture(new Vector3(b0.X, b0.Y, 0f), c1, new Vector2(u1, vMin));
            _vertsT[_nVertsT++] = new VertexPositionColorTexture(new Vector3(a0.X, a0.Y, 0f), c0, new Vector2(u0, vMin));
            _vertsT[_nVertsT++] = new VertexPositionColorTexture(new Vector3(b0.X, b0.Y, 0f), c1, new Vector2(u1, vMin));
            _vertsT[_nVertsT++] = new VertexPositionColorTexture(new Vector3(b1.X, b1.Y, 0f), c1, new Vector2(u1, vMax));
        }

        /// <summary>El volcado de la tira texturizada (BasicEffect con
        /// textura + LinearWrap — el flujo vive en las UVs, CERO shaders).</summary>
        private static void VolcarTextura(Texture2D tex)
        {
            if (_nVertsT < 3) { _nVertsT = 0; return; }
            try
            {
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (_fxT == null)
                {
                    _fxT = new BasicEffect(gd)
                    {
                        LightingEnabled = false,
                        VertexColorEnabled = true,
                        TextureEnabled = true,
                        Alpha = 1f
                    };
                }
                _fxT.Texture = tex;
                _fxT.World = Main.GameViewMatrix.TransformationMatrix;
                _fxT.View = Matrix.Identity;
                _fxT.Projection = Matrix.CreateOrthographicOffCenter(
                    0, Main.screenWidth, Main.screenHeight, 0, 0, -1);

                gd.BlendState = _pantalla;
                gd.DepthStencilState = DepthStencilState.None;
                gd.RasterizerState = RasterizerState.CullNone;
                gd.SamplerStates[0] = SamplerState.LinearWrap;

                foreach (EffectPass pass in _fxT.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    int porLote = (_vertsT.Length / 3) * 3;
                    int inicio = 0;
                    while (inicio < _nVertsT)
                    {
                        int cuenta = Math.Min(_nVertsT - inicio, porLote);
                        int prims = cuenta / 3;
                        if (prims > 0)
                            gd.DrawUserPrimitives(PrimitiveType.TriangleList,
                                _vertsT, inicio, prims);
                        inicio += cuenta;
                    }
                }
            }
            catch { }
            _nVertsT = 0;
        }

        // ==================================================================
        //  EL FILAMENTO (la entrada pública con su propio baile)
        // ==================================================================

        /// <summary>
        /// DIBUJA un filamento de rayo LISO (tira de primitivas con perfil
        /// gaussiano por vértice): el baile completo — abre lote, dibuja,
        /// cierra lote. <paramref name="pts"/> en COORDENADAS DE PANTALLA
        /// (mundo − Main.screenPosition, como todos los draws de la casa).
        /// </summary>
        /// <param name="pts">El camino del canal (pantalla).</param>
        /// <param name="width">El ancho base (la funda llega a 5.2×, la vena 0.72×).</param>
        /// <param name="halo">El color de la funda.</param>
        /// <param name="nucleo">El color de la vena caliente.</param>
        /// <param name="alpha">Brillo global 0..1.</param>
        /// <param name="taper">La curva de anchura a lo largo.</param>
        /// <param name="seed">Semilla del crackle.</param>
        /// <param name="flick">El flick actual (regenera el crackle).</param>
        /// <param name="suavizar">Chaikin ×1 (SOLO el canal Perlin: los rayos
        /// de verdad tienen kinks AGUDOS — stepped leaders — y el suavizado
        /// los redondeaba como tubos).</param>
        /// <param name="crackle">El brillo por vértice (la casa lo usa en Strand).</param>
        /// <param name="vena">Dibujar la vena blanca (true salvo capas raras).</param>
        /// <param name="alphaPorNodo">Alfa extra por nodo (la OLA de RayoLib) o null.</param>
        /// <param name="anchoPorNodo">Multiplicador de ancho por nodo (la OLA) o null.</param>
        public static void Filamento(Vector2[] pts, float width, Color halo, Color nucleo,
            float alpha = 1f, StormTaper taper = StormTaper.Center,
            int seed = 0, int flick = 0, bool suavizar = false, bool crackle = true,
            bool vena = true, float[] alphaPorNodo = null, float[] anchoPorNodo = null)
        {
            AbrirLote();
            try { FilamentoEnLote(pts, width, halo, nucleo, alpha, taper, seed, flick,
                suavizar, crackle, vena, alphaPorNodo, anchoPorNodo); }
            finally { CerrarLote(); }
        }

        // ==================================================================
        //  EL FILAMENTO EN LOTE (el motor de la tira)
        // ==================================================================

        /// <summary>
        /// El filamento DENTRO de un lote ya abierto (ver
        /// <see cref="AbrirLote"/>): no toca el SpriteBatch — solo emite
        /// primitivas. Esta es la versión que usan MultiBolt/PerlinBolt/
        /// RayoSistema para dibujar TODO el árbol en UN solo baile.
        /// </summary>
        public static void FilamentoEnLote(Vector2[] pts, float width, Color halo, Color nucleo,
            float alpha = 1f, StormTaper taper = StormTaper.Center,
            int seed = 0, int flick = 0, bool suavizar = false, bool crackle = true,
            bool vena = true, float[] alphaPorNodo = null, float[] anchoPorNodo = null)
        {
            if (Main.netMode == Terraria.ID.NetmodeID.Server || Main.dedServ) return;
            if (pts == null || pts.Length < 2 || alpha <= 0.01f || width <= 0.05f) return;

            // v6.50.26 — EL ALISADO CHAIKIN ES OPT-IN (el reporte:
            // «demasiado redondeado cuando la línea se curva»): los rayos
            // reales son zigzag con KINKS AGUDOS — la tira continua ya ES
            // lisa (cero juntas por construcción); solo el canal Perlin
            // (el ARCO eléctrico, curvatura continua en la vida real)
            // conserva el meandro suavizado.
            if (suavizar) pts = Chaikin1(pts);

            int n = pts.Length;
            if (n < 2) return;

            // === POR NODO: dirección media (la normal suave), semiancho y alfa ===
            float[] semi = new float[n];      // semiancho de la FUNDA
            float[] semiV = new float[n];     // semiancho de la VENA
            float[] alfa = new float[n];      // alfa del halo por nodo
            float[] alfaV = new float[n];     // alfa de la vena por nodo
            Vector2[] norm = new Vector2[n];

            float total = 0f;                 // longitud de arco (para el taper)
            float[] arcs = new float[n];
            for (int i = 1; i < n; i++)
            {
                total += Vector2.Distance(pts[i - 1], pts[i]);
                arcs[i] = total;
            }
            if (total < 1f) return;

            for (int i = 0; i < n; i++)
            {
                float t = arcs[i] / total;

                // LA NORMAL MEDIA (por vértice, no por tramo: la tira es
                // continua por CONSTRUCCIÓN — el trapecio comparte vértices).
                Vector2 prevD = i > 0 ? pts[i] - pts[i - 1] : pts[Math.Min(i + 1, n - 1)] - pts[i];
                Vector2 nextD = i < n - 1 ? pts[i + 1] - pts[i] : pts[i] - pts[Math.Max(i - 1, 0)];
                if (prevD.LengthSquared() < 0.0001f) prevD = nextD;
                if (nextD.LengthSquared() < 0.0001f) nextD = prevD;
                Vector2 avg = Vector2.Normalize(prevD) + Vector2.Normalize(nextD);
                if (!(avg.LengthSquared() >= 0.001f)) avg = nextD;
                avg = Vector2.Normalize(avg);
                norm[i] = new Vector2(-avg.Y, avg.X);

                // EL ANCHO: taper × OLA (si la trae) × suelos de visibilidad.
                // v6.50.27 — LA FUNDA ESTRECHA: 1.55×w de semiancho (±1.55w
                // = 3.1w de halo total — era 2.0×w/4w: la falda GORDA es la
                // mitad del «brillo muy fuerte» y del área de solape que
                // cortaba). La vena engorda un pelo para seguir mandando.
                // (LOS ARRAYS POR NODO del llamador son del camino ORIGINAL
                // — tras el Chaikin el índice ya NO coincide: se muestrean
                // POR FRACCIÓN de camino con interpolación lineal.)
                float w = width * TaperLocal(taper, t);
                if (anchoPorNodo != null)
                    w *= Muestrear(anchoPorNodo, t);
                if (w < 0.05f) w = 0.05f;
                semi[i] = Math.Max(1.55f * w, 1.55f);
                semiV[i] = Math.Max(0.34f * w, 0.75f);

                // EL ALFA: global × crackle (la vena arde SIEMPRE) × OLA.
                float cr = crackle ? 0.66f + 0.34f * VFXCore.Hash01(seed, flick, i * 41 + 17) : 1f;
                float aN = alphaPorNodo != null ? Muestrear(alphaPorNodo, t) : 1f;
                alfa[i] = alpha * cr * aN;
                alfaV[i] = alpha * aN;
                if (alfa[i] < 0.003f && alfaV[i] < 0.003f) { alfa[i] = 0f; alfaV[i] = 0f; }
            }

            // === LA EMISIÓN (trapecios columna a columna — cero solapes) ===
            _nVerts = 0;
            EmitirBanda(pts, norm, semi, alfa, ColF, ColA, halo);
            if (vena) EmitirBanda(pts, norm, semiV, alfaV, VenF, VenA, nucleo);
            Volcar();
        }

        /// <summary>
        /// MUESTREA un array por-nodo POR FRACCIÓN de camino (0..1) con
        /// interpolación lineal — los arrays del llamador son del camino
        /// ORIGINAL y el Chaikin duplica los nodos: el índice directo no
        /// sirve, la FRACCIÓN sí (Chaikin conserva la parametrización
        /// de nodos uniformes).
        /// </summary>
        private static float Muestrear(float[] nodos, float t)
        {
            if (nodos == null || nodos.Length == 0) return 1f;
            if (nodos.Length == 1) return nodos[0];
            float x = MathHelper.Clamp(t, 0f, 1f) * (nodos.Length - 1);
            int i0 = (int)x;
            int i1 = Math.Min(i0 + 1, nodos.Length - 1);
            return MathHelper.Lerp(nodos[i0], nodos[i1], x - i0);
        }

        /// <summary>
        /// EMITE una banda: por cada segmento, un trapecio por cada par de
        /// columnas adyacentes (2 triángulos). Los vértices del nodo i son
        /// COMPARTIDOS por el segmento i−1 y el i → la tira es CONTINUA
        /// (esta es la propiedad que el SpriteBatch no puede dar: aquí NO
        /// existen juntas, huecos ni solapes).
        /// </summary>
        private static void EmitirBanda(Vector2[] pts, Vector2[] norm, float[] semi,
            float[] alfa, float[] frac, float[] colA, Color color)
        {
            int n = pts.Length;
            int cols = frac.Length;

            for (int i = 0; i < n - 1; i++)
            {
                if (alfa[i] <= 0f && alfa[i + 1] <= 0f) continue;
                float len = Vector2.Distance(pts[i], pts[i + 1]);
                if (len < 0.30f) continue;

                for (int c = 0; c < cols - 1; c++)
                {
                    // El trapecio entre columnas c y c+1 (esquinas por nodo).
                    Vector2 a0 = pts[i] + norm[i] * (semi[i] * frac[c]);
                    Vector2 a1 = pts[i] + norm[i] * (semi[i] * frac[c + 1]);
                    Vector2 b0 = pts[i + 1] + norm[i + 1] * (semi[i + 1] * frac[c]);
                    Vector2 b1 = pts[i + 1] + norm[i + 1] * (semi[i + 1] * frac[c + 1]);

                    float fA0 = alfa[i] * colA[c];
                    float fA1 = alfa[i] * colA[c + 1];
                    float fB0 = alfa[i + 1] * colA[c];
                    float fB1 = alfa[i + 1] * colA[c + 1];

                    EmitirTri(a0, a1, b1, fA0, fA1, fB1, color);
                    EmitirTri(a0, b1, b0, fA0, fB1, fB0, color);
                }
            }
        }

        /// <summary>Apila un triángulo en el pozo (vaciando si se llena).</summary>
        private static void EmitirTri(Vector2 a, Vector2 b, Vector2 c,
            float fa, float fb, float fc, Color color)
        {
            if (_nVerts + 3 > _verts.Length) Volcar();
            _verts[_nVerts++] = new VertexPositionColor(
                new Vector3(a.X, a.Y, 0f), VertColor(color, fa));
            _verts[_nVerts++] = new VertexPositionColor(
                new Vector3(b.X, b.Y, 0f), VertColor(color, fb));
            _verts[_nVerts++] = new VertexPositionColor(
                new Vector3(c.X, c.Y, 0f), VertColor(color, fc));
        }

        /// <summary>v6.50.26 — EL COLOR PRE-MULTIPLICADO (EL FIX DE RAÍZ):
        /// el BasicEffect pasa el color del vértice SIN multiplicar por su
        /// alfa y el lote SUMA el rgb TAL CUAL — el alfa del vértice era
        /// LETRA MUERTA (la funda salía como banda SÓLIDA: «el brillo es
        /// muy intenso, se rompe en los bordes»). Ahora el rgb escala por
        /// f AQUÍ, en la CPU — el contrato Color·f de TODA la casa, y el
        /// lote de pantalla (soft-add) lo compone sin clipear.</summary>
        private static Color VertColor(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color((byte)(int)(c.R * f + 0.5f), (byte)(int)(c.G * f + 0.5f),
                (byte)(int)(c.B * f + 0.5f), 255);
        }

        // ==================================================================
        //  EL VOLCADO (DrawUserPrimitives con BasicEffect)
        // ==================================================================

        private static void Volcar()
        {
            if (_nVerts < 3) { _nVerts = 0; return; }
            try
            {
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (_fx == null)
                {
                    _fx = new BasicEffect(gd)
                    {
                        LightingEnabled = false,
                        VertexColorEnabled = true,
                        TextureEnabled = false,
                        Alpha = 1f
                    };
                }
                _fx.World = Main.GameViewMatrix.TransformationMatrix;
                _fx.View = Matrix.Identity;
                _fx.Projection = Matrix.CreateOrthographicOffCenter(
                    0, Main.screenWidth, Main.screenHeight, 0, 0, -1);

                // v6.50.26 — EL LOTE DE PANTALLA (soft-add) en vez del
                // aditivo puro: dst += rgb·(1−dst) — la acumulación es
                // asintótica y NUNCA clipea (el fix del brillo que «se
                // rompe en los bordes cuando toca el brillo de otra
                // línea»: dos fundas cruzadas se SUMAN, no se queman).
                gd.BlendState = _pantalla;
                gd.DepthStencilState = DepthStencilState.None;
                gd.RasterizerState = RasterizerState.CullNone;

                foreach (EffectPass pass in _fx.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    int porLote = (_verts.Length / 3) * 3;
                    int inicio = 0;
                    while (inicio < _nVerts)
                    {
                        int cuenta = Math.Min(_nVerts - inicio, porLote);
                        int prims = cuenta / 3;
                        if (prims > 0)
                            gd.DrawUserPrimitives(PrimitiveType.TriangleList,
                                _verts, inicio, prims);
                        inicio += cuenta;
                    }
                }
            }
            catch { }
            _nVerts = 0;
        }

        // ==================================================================
        //  LAS HERRAMIENTAS (Chaikin + el taper local)
        // ==================================================================

        /// <summary>
        /// EL ALISADO CHAIKIN (una iteración): cada esquina se corta al
        /// 25% y al 75% de su segmento — la curva fluye sin esquinas duras
        /// conservando la SILUETA del zigzag. Anclas primera/última exactas.
        /// </summary>
        private static Vector2[] Chaikin1(Vector2[] pts)
        {
            int n = pts.Length;
            if (n < 3) return pts;
            var r = new Vector2[(n - 1) * 2];
            r[0] = pts[0];
            for (int i = 0; i < n - 1; i++)
            {
                Vector2 a = pts[i], b = pts[i + 1];
                r[i * 2] = a + (b - a) * 0.25f;
                r[i * 2 + 1] = a + (b - a) * 0.75f;
            }
            r[(n - 1) * 2 - 1] = pts[n - 1];
            return r;
        }

        /// <summary>La curva de anchura (el contrato del taper de StormLib).</summary>
        private static float TaperLocal(StormTaper taper, float t)
        {
            switch (taper)
            {
                case StormTaper.Linear:
                    return MathHelper.Lerp(1f, 0.25f, t);
                case StormTaper.Impact:
                    return MathHelper.Lerp(0.30f, 1f, (float)Math.Pow(t, 0.6));
                case StormTaper.Flat:
                    return 1f;
                default: // Center
                    return Math.Max(0.35f,
                        (float)Math.Pow(Math.Sin(t * Math.PI), 0.65));
            }
        }
    }
}
