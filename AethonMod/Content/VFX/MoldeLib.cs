using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// MoldeLib — v6.43 — LA LIBRERÍA DE CUERPOS COMPUESTOS RE-PARAMETRIZABLES.
    ///
    /// Idea central: un cuerpo visual complejo (el ojo alado del fragmento
    /// de supernova, un jefe con caparazón y alas, una cría con cola) no
    /// debería volver a escribirse con quads a mano cada vez. Un MOLDE es
    /// su DESCRIPCIÓN DECLARATIVA: una lista de piezas (offset/escala/
    /// rotación/color/pase de blend/orden Z) que se ANCLAN a un cuerpo
    /// vivo — el llamador solo pasa cada frame la posición, la rotación,
    /// el banking y la escala, y el molde re-proyecta TODAS las piezas
    /// sin que nadie repita la coreografía a mano.
    ///
    /// EL CONTRATO DE LOS DOS PASES (la lección v6.41 de la pupila): el
    /// brillo de las piezas se emite al BÚFER de VFXCore en coordenadas
    /// de MUNDO (el llamador hace <c>VFXCore.Begin()</c> antes y
    /// <c>VFXCore.FlushAdditive</c> después), pero las piezas marcadas
    /// con PaseAlpha — las que OSCURECEN (la pupila: el vacío del ojo) —
    /// quedan apartadas en una cola interna. El llamador las vuelca AL
    /// FINAL, ya con todo el brillo en pantalla, con
    /// <see cref="Molde.FlushPaseAlpha"/>: si la pupila se dibujara
    /// enterrada bajo el aditivo propio, el vacío no oscurece NADA.
    ///
    /// EL PRIMER MOLDE: <see cref="Molde.OjoAlado"/> — el ojo alado del
    /// fragmento, extraído con LOS NÚMEROS EXACTOS de la réplica v6.41
    /// (cuerpo 110×40, alas ±0.44 rad, puntas ±0.6, pupila 34×12 al 60%).
    /// Crear un jefe con cuerpo nuevo = llamar a la factory con otros
    /// parámetros (talla, paleta, envergadura, aleteo) — horas, no días.
    ///
    /// CONVENCIONES DE LA CASA: cero GC por frame (todas las listas
    /// pre-asignadas, el orden Z se cachea al primer uso), cero Main.rand
    /// en render (el aleteo es reloj puro), lotes blindados try/finally
    /// (una excepción NUNCA deja el lote abierto ni la cola sucia).
    /// </summary>
    public static class MoldeLib
    {
        /// <summary>
        /// UNA PIEZA DEL CUERPO: una lámina de luz descrita en el espacio
        /// LOCAL del molde (el origen = el centro del cuerpo, +X = frente,
        /// +Y = abajo). La proyección la gira, la escala y la tiñe según
        /// el cuerpo vivo que la ancle — la pieza en sí JAMÁS cambia.
        /// </summary>
        public struct Pieza
        {
            /// <summary>
            /// Centro de la pieza en espacio local del cuerpo (px del
            /// molde; +X = frente). La rotación de la pieza pivota sobre
            /// este punto — es EL ancla, no una esquina.
            /// </summary>
            public Vector2 Offset;

            /// <summary>Tamaño final de la pieza en px (x≠y = estirada:
            /// el cuerpo del ojo es 110×40 porque ES un ojo horizontal).</summary>
            public Vector2 Escala;

            /// <summary>
            /// Inclinación propia en rad, EN ESPACIO del cuerpo ya girado:
            /// las alas del ojo viven a ±0.44 del eje del cuerpo.
            /// </summary>
            public float RotacionLocal;

            /// <summary>El color de la pieza (con su alfa horneada — la
            /// intensidad por pieza de la réplica: cuerpo 0.85, ala 0.8).</summary>
            public Color Color;

            /// <summary>Textura propia (null = SoftGlow, la lámpara de la
            /// casa). El vacío y los anillos usan las suyas.</summary>
            public Texture2D Textura;

            /// <summary>
            /// TRUE = la pieza va al PASE ALPHA (oscurece) en vez del
            /// aditivo (brilla). ¿El porqué? Las piezas que hacen de
            /// VACÍO (la pupila) deben RESTAR luz ENCIMA de todo el
            /// brillo del cuerpo — en aditivo solo podrían sumar.
            /// </summary>
            public bool PaseAlpha;

            /// <summary>
            /// Orden de emisión (menor = antes). ¿El porqué de un orden
            /// explícito? Con blending aditivo el orden visual da igual
            /// (conmuta), pero el ORDEN ES SEMÁNTICA: z=0 el cuerpo,
            /// z=10 las alas, z=100 la pupila — quien lea el molde lee
            /// la anatomía.
            /// </summary>
            public float OrdenZ;

            /// <summary>
            /// TRUE (default del constructor fluido) = la pieza gira con
            /// la rotación del cuerpo. FALSE = se mantiene vertical en
            /// pantalla aunque el cuerpo gire (halos, iconos flotantes).
            /// </summary>
            public bool SeguirDireccion;

            /// <summary>
            /// Amplitud del aleteo propio en rad: la pieza oscila
            /// senoidalmente (AmplitudAleteo·sin(tiempo·VelocidadAleteo +
            /// FaseAleteo)) — las alas laten SIN que nadie anime nada.
            /// 0 = quieta (el ojo original no aletea: es una divinidad).
            /// </summary>
            public float AmplitudAleteo;

            /// <summary>
            /// Fase del aleteo en rad: las alas ESPEJO llevan fases
            /// separadas por π para aletear en contramovimiento — así la
            /// silueta se queda simétrica mientras late.
            /// </summary>
            public float FaseAleteo;

            /// <summary>
            /// Velocidad del aleteo (rad/s del reloj del juego — 6 = un
            /// latido majestuoso de ~1 s).
            /// </summary>
            public float VelocidadAleteo;

            /// <summary>
            /// Cuánto bancan ESTA pieza respecto al banking de entrada
            /// (1 = como el cuerpo, 1.6 = las alas rockean más — un
            /// cuerpo vivo bambolea sus extremos). 0 = inmune al banco.
            /// </summary>
            public float FactorBanking;
        }

        /// <summary>
        /// EL MOLDE: la descripción declarativa de un cuerpo compuesto.
        /// Se CONSTRUYE UNA vez (builder fluido, en el arranque o lazy) y
        /// se PROYECTA cada frame sobre un cuerpo vivo — cero GC en
        /// render, el orden Z pre-computado y cacheado al primer uso.
        /// </summary>
        public sealed class Molde
        {
            // === LOS PARÁMETROS GLOBALES DEL CUERPO (re-parametrización) ===

            /// <summary>
            /// La longitud de referencia del cuerpo en px del molde (el
            /// ojo: 110 — su eje +X). ¿El porqué? Es el NÚMERO de la
            /// re-parametrización: <see cref="ReEscalar"/> re-proporciona
            /// TODO el molde (offsets y escalas) a una talla nueva con
            /// este número como denominador — un jefe estira el mismo
            /// cuerpo sin re-declarar pieza por pieza.
            /// </summary>
            public float LongitudCuerpo;

            /// <summary>
            /// Multiplicador del banking de entrada: cuánto rockea TODO
            /// el cuerpo cuando el llamador pasa banking (el ojo usa
            /// 0.03 — el número exacto del original: velocity.X·0.03).
            /// </summary>
            public float AmplitudBanking = 1f;

            /// <summary>
            /// Multiplicador global de tamaño del molde (la factory
            /// CrearOjoAlado lo fija con su escalaBase): 2 = el mismo
            /// cuerpo al doble, anclas incluidas.
            /// </summary>
            public float EscalaGlobal = 1f;

            /// <summary>
            /// LAS PIEZAS del molde (pre-asignada a 32 — cero GC mientras
            /// un cuerpo razonable no pase de ahí; se llena SOLO al
            /// construir, el render jamás la toca).
            /// </summary>
            public readonly List<Pieza> Piezas = new List<Pieza>(32);

            /// <summary>
            /// La cola del pase alpha: piezas ya PROYECTADAS (coords de
            /// mundo, rotación y color finales) esperando su vuelco tras
            /// el FlushAdditive. Pre-asignada a 32 — cero GC por frame.
            /// </summary>
            private readonly List<VFXCore.GlowQuad> _pendientesAlpha = new List<VFXCore.GlowQuad>(32);

            /// <summary>El orden de emisión cacheado (OrdenZ, orden
            /// estable) — se computa al PRIMER uso y se invalida si el
            /// molde muta (construcción o re-escala).</summary>
            private Pieza[] _ordenadas;

            /// <summary>El constructor de piezas reutilizado — ni UNA
            /// alocación al construir un molde largo.</summary>
            private readonly ConstructorPieza _constructor;

            /// <summary>Construye un molde vacío; las piezas entran por
            /// el builder fluido (<see cref="NuevaPieza"/>).</summary>
            public Molde()
            {
                _constructor = new ConstructorPieza(this);
            }

            // ------------------------------------------------------------------
            //  EL BUILDER FLUIDO (declarar un cuerpo = leer su anatomía)
            // ------------------------------------------------------------------

            /// <summary>
            /// Empieza una pieza nueva: <c>m.NuevaPieza().ConOffset(..).
            /// ConEscala(..).ConColor(..).AlFinal()</c> — AlFinal la
            /// COMMITA al molde y devuelve EL MOLDE para encadenar la
            /// siguiente pieza sin soltar la cadena.
            /// </summary>
            public ConstructorPieza NuevaPieza()
            {
                _constructor.Reiniciar();
                return _constructor;
            }

            /// <summary>Los parámetros globales, también fluidos (para
            /// encadenarlos tras el new: <c>new Molde().ConLongitudCuerpo(110f)...</c>).</summary>
            public Molde ConLongitudCuerpo(float px) { LongitudCuerpo = px; return this; }

            /// <summary>Ver <see cref="AmplitudBanking"/>.</summary>
            public Molde ConAmplitudBanking(float amplitud) { AmplitudBanking = amplitud; return this; }

            /// <summary>Ver <see cref="EscalaGlobal"/>.</summary>
            public Molde ConEscalaGlobal(float escala) { EscalaGlobal = escala; return this; }

            /// <summary>
            /// RE-ESCALA el molde entero a una nueva longitud de cuerpo:
            /// offsets Y escalas se re-proporcionan por
            /// nueva/LongitudCuerpo (las proporciones anatómicas quedan
            /// CLAVADAS — la envergadura acompaña al cuerpo). Mutar el
            /// molde COMPARTIDO (OjoAlado) afectaría a todos sus usuarios:
            /// re-escala solo moldes PROPIOS (o usa la factory).
            /// </summary>
            public Molde ReEscalar(float nuevaLongitud)
            {
                if (LongitudCuerpo <= 0f || nuevaLongitud <= 0f) return this;
                float factor = nuevaLongitud / LongitudCuerpo;
                for (int i = 0; i < Piezas.Count; i++)
                {
                    Pieza p = Piezas[i];
                    p.Offset *= factor;
                    p.Escala *= factor;
                    Piezas[i] = p;
                }
                LongitudCuerpo = nuevaLongitud;
                _ordenadas = null;      // el cuerpo mutó: el orden se re-computa
                return this;
            }

            /// <summary>Commit interno del builder (AlFinal).</summary>
            internal void Añadir(Pieza pieza)
            {
                Piezas.Add(pieza);
                _ordenadas = null;      // invalidate: hay anatomía nueva
            }

            /// <summary>
            /// Garantiza el orden por <see cref="Pieza.OrdenZ"/> (ESTABLE:
            /// empates conservan el orden de declaración). Se computa UNA
            /// vez al primer render — inserción pura, ≤32 piezas, cero GC
            /// después.
            /// </summary>
            private Pieza[] AsegurarOrden()
            {
                if (_ordenadas != null) return _ordenadas;

                Pieza[] orden = new Pieza[Piezas.Count];
                for (int i = 0; i < Piezas.Count; i++)
                {
                    Pieza p = Piezas[i];
                    int j = i - 1;
                    while (j >= 0 && orden[j].OrdenZ > p.OrdenZ)
                    {
                        orden[j + 1] = orden[j];
                        j--;
                    }
                    orden[j + 1] = p;
                }
                _ordenadas = orden;
                return orden;
            }

            // ------------------------------------------------------------------
            //  LA PROYECCIÓN — el cuerpo vivo ancla el molde cada frame
            // ------------------------------------------------------------------

            /// <summary>
            /// PROYECTA el molde sobre un cuerpo VIVO: por cada pieza (en
            /// orden Z) computa la rotación final
            /// <c>rotacionCuerpo (si SeguirDireccion) + RotacionLocal +
            /// banking·AmplitudBanking·FactorBanking +
            /// AmplitudAleteo·sin(tiempo·VelocidadAleteo + FaseAleteo)</c>,
            /// la posición mundo <c>centro + Offset.RotatedBy(rotacionCuerpo)·escala</c>
            /// (TODO el cuerpo escala junto — la física de un cuerpo que
            /// crece: alas más grandes, anclas más lejos) y el color
            /// (RGB×tinteMult, alfa×alfaGlobal). Las piezas aditivas se
            /// EMITEN al búfer de VFXCore en coords de MUNDO (el llamador
            /// hace VFXCore.Begin() antes y FlushAdditive después); las de
            /// PaseALPHA quedan en la cola interna para
            /// <see cref="FlushPaseAlpha"/> — el contrato de la pupila.
            /// </summary>
            /// <param name="centro">El centro del cuerpo en coords de MUNDO.</param>
            /// <param name="rotacionCuerpo">La rotación del cuerpo (rad).</param>
            /// <param name="banking">El bamboleo de entrada (crudo: la
            /// velocidad lateral — AmplitudBanking lo dosifica).</param>
            /// <param name="escala">La escala del cuerpo (1 = la talla del molde).</param>
            /// <param name="tiempo">El reloj del juego (el aleteo es reloj, jamás random).</param>
            /// <param name="tinteMult">Tinte multiplicativo del RGB (White = colores propios).</param>
            /// <param name="alfaGlobal">Multiplicador global del alfa (0..1: apariciones, fades).</param>
            public void Proyectar(Vector2 centro, float rotacionCuerpo, float banking,
                float escala, float tiempo, Color tinteMult, float alfaGlobal)
            {
                // EL CUERPO VIVO escala todo junto: anclas y carne crecen a la par.
                ProyectarNucleo(centro, rotacionCuerpo, banking, escala, escala, tiempo,
                    tinteMult, alfaGlobal, null);
            }

            /// <summary>
            /// EL NOMBRE DEL CONTRATO — alias literal de <see cref="Proyectar"/>
            /// (mismo mecanismo): recuerda que UNA llamada son DOS pases —
            /// el aditivo cae YA al búfer de VFXCore y las piezas alpha
            /// esperan al <see cref="FlushPaseAlpha"/> posterior.
            /// </summary>
            public void EmitirPases(Vector2 centro, float rotacionCuerpo, float banking,
                float escala, float tiempo, Color tinteMult, float alfaGlobal)
                => Proyectar(centro, rotacionCuerpo, banking, escala, tiempo, tinteMult, alfaGlobal);

            /// <summary>
            /// EL FANTASMA: el molde entero en UN color sólido (la
            /// silueta del afterimage — así se dibujaban los ecos del
            /// fragmento: cuerpo, alas y puntas del MISMO tinte). Las
            /// piezas de pase alpha se SALTAN: el vacío de la pupila es
            /// un detalle del cuerpo vivo; un fantasma es silueta pura.
            /// ¿El porqué de <paramref name="escalaAnclaje"/> aparte? El
            /// eco de la réplica encoge su CARNE (0.55/0.5) pero conserva
            /// la ENVERGADURA (anclas a 1) — por defecto aquí se clava
            /// ese look; pásale el mismo valor que escalaPiezas para un
            /// eco proporcional.
            /// </summary>
            /// <param name="escalaPiezas">La escala de la carne del eco (las anclas van aparte).</param>
            /// <param name="colorSolido">El color de TODA la silueta (con su alfa — modula el aditivo).</param>
            /// <param name="escalaAnclaje">La escala de las anclas (default 1: envergadura fija).</param>
            public void ProyectarFantasma(Vector2 centro, float rotacionCuerpo, float banking,
                float escalaPiezas, float tiempo, Color colorSolido, float escalaAnclaje = 1f)
            {
                ProyectarNucleo(centro, rotacionCuerpo, banking, escalaPiezas, escalaAnclaje,
                    tiempo, Color.White, 0f, colorSolido);
            }

            /// <summary>El motor común de las dos proyecciones (colorSolido
            /// != null = modo fantasma). Cero alocaciones: todo struct y
            /// listas pre-asignadas.</summary>
            private void ProyectarNucleo(Vector2 centro, float rotacionCuerpo, float banking,
                float escalaPiezas, float escalaAnclaje, float tiempo,
                Color tinteMult, float alfaGlobal, Color? colorSolido)
            {
                Pieza[] orden = AsegurarOrden();
                if (orden.Length == 0) return;

                // EL PRESUPUESTO DE LA CASA: si el frame ya va saturado de
                // brillos, el cuerpo entero se salta (degradación elegante —
                // mejor un minion menos brillante que un frame menos fluido).
                if (!VFXCore.Presupuesto(orden.Length)) return;

                float escCarne = escalaPiezas * EscalaGlobal;
                float escAnclas = escalaAnclaje * EscalaGlobal;
                float banco = banking * AmplitudBanking;

                for (int i = 0; i < orden.Length; i++)
                {
                    Pieza p = orden[i];

                    // La silueta no tiene vacíos internos (la pupila es del vivo).
                    if (colorSolido.HasValue && p.PaseAlpha) continue;

                    // LA ROTACIÓN FINAL — la fórmula del contrato: dirección
                    // del cuerpo + inclinación propia + banco por pieza +
                    // aleteo propio (reloj puro, cero random).
                    float rot = (p.SeguirDireccion ? rotacionCuerpo : 0f)
                        + p.RotacionLocal
                        + banco * p.FactorBanking
                        + p.AmplitudAleteo * MathF.Sin(tiempo * p.VelocidadAleteo + p.FaseAleteo);

                    // LA POSICIÓN MUNDO: el offset local girado por el CUERPO
                    // (las anclas acompañan la dirección, no el aleteo).
                    Vector2 pos = centro + p.Offset.RotatedBy(rotacionCuerpo) * escAnclas;
                    Vector2 esc = p.Escala * escCarne;

                    // EL COLOR: fantasma = el sólido tal cual; vivo = RGB por
                    // el tinte (tinte blanco = byte exacto: R·255/255) y alfa
                    // por el global. La división REDONDEA (R·t+127)/255: con
                    // tinte blanco clava el byte EXACTO (R·255+127 < (R+1)·255)
                    // y con tinte teñido no oscurece por truncación — la MISMA
                    // aritmética de redondeo que el operador Color*float de XNA.
                    Color color;
                    if (colorSolido.HasValue)
                    {
                        color = colorSolido.Value;
                    }
                    else
                    {
                        color = new Color(
                            (p.Color.R * tinteMult.R + 127) / 255,
                            (p.Color.G * tinteMult.G + 127) / 255,
                            (p.Color.B * tinteMult.B + 127) / 255,
                            Math.Min(255, (int)(p.Color.A * alfaGlobal)));
                    }

                    if (p.PaseAlpha)
                    {
                        // LA COLA DEL VACÍO: proyectada y apartada — el
                        // llamador la vuelca AL FINAL (FlushPaseAlpha). La
                        // válvula de 256 evita que un llamador que olvide el
                        // flush haga crecer la lista sin techo (GC).
                        if (_pendientesAlpha.Count < 256)
                            _pendientesAlpha.Add(new VFXCore.GlowQuad
                            {
                                Position = pos,
                                Color = color,
                                Scale = esc,
                                Rotation = rot,
                                Texture = p.Textura,
                            });
                    }
                    else if (p.Textura != null)
                    {
                        VFXCore.Quad(pos, color, esc, rot, p.Textura);
                    }
                    else
                    {
                        VFXCore.Quad(pos, color, esc, rot);
                    }
                }
            }

            /// <summary>
            /// VUELCA EL PASE ALPHA: abre el lote AlphaBlend propio de la
            /// casa (coords de MUNDO con Main.screenPosition, la MISMA
            /// semántica de escala que FlushAdditive), dibuja las piezas
            /// pendientes ENCIMA de todo el brillo aditivo ya volcado y
            /// cierra. Blindado try/finally: una excepción NUNCA deja el
            /// lote abierto ni la cola sucia (la lección v6.41 del volcado).
            /// Llamarlo DESPUÉS del FlushAdditive — ese es el contrato
            /// "pupila al final".
            ///
            /// AUDITORÍA v6.43 (T4) — End defensivo previo + reapertura
            /// condicional (el contrato de lote de la casa): si un
            /// llamador llegara con el lote del juego ABIERTO, el Begin de
            /// abajo tiraría y el End de rescate del finally cerraría UN
            /// LOTE AJENO (corrupción de render). Ahora: el End defensivo
            /// detecta el estado, el propio lote se abre limpio, y el
            /// finally solo reabre el estándar si FUIMOS NOSOTROS quienes
            /// cerramos el lote del llamador (cerrado→cerrado — el flujo
            /// real del fragmento: ya cerrado tras el FlushAdditive, así
            /// que nada se reabre y el PreDraw del llamador sigue mandando).
            /// </summary>
            public void FlushPaseAlpha()
            {
                if (_pendientesAlpha.Count == 0) return;
                if (Main.netMode == NetmodeID.Server) return;

                // End defensivo previo: ¿había lote abierto? (el contrato
                // de la casa: Begin JAMÁS sobre un lote ajeno abierto).
                bool loteAjenoAbierto = false;
                try { Main.spriteBatch.End(); loteAjenoAbierto = true; }
                catch { }

                try
                {
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                        SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                        null, Main.GameViewMatrix.TransformationMatrix);

                    Vector2 screen = Main.screenPosition;
                    for (int i = 0; i < _pendientesAlpha.Count; i++)
                    {
                        VFXCore.GlowQuad q = _pendientesAlpha[i];
                        if (q.Color.A == 0) continue;

                        Texture2D tex = q.Texture ?? VFXCore.SoftGlow;
                        Vector2 invTex = new Vector2(1f / tex.Width, 1f / tex.Height);
                        Main.spriteBatch.Draw(tex, q.Position - screen, null,
                            q.Color, q.Rotation, tex.Size() * 0.5f, q.Scale * invTex,
                            SpriteEffects.None, 0f);
                    }
                }
                finally
                {
                    try { Main.spriteBatch.End(); }
                    catch { /* el End del lote de rescate nunca puede tirar */ }
                    _pendientesAlpha.Clear();

                    // Reapertura estándar SOLO si cerramos un lote ajeno:
                    // el estado del llamador se devuelve como estaba.
                    if (loteAjenoAbierto)
                    {
                        try
                        {
                            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                                null, Main.GameViewMatrix.TransformationMatrix);
                        }
                        catch { }
                    }
                }
            }

            /// <summary>
            /// AUDITORÍA v6.43 (T4) — LA HIGIENE DEL CAMINO DE ERROR: descarta
            /// la cola del pase alpha SIN dibujarla. La contrapartida exacta
            /// del <c>VFXCore.Begin()</c> que el catch del llamador ya hace
            /// con el búfer de quads: una excepción entre Proyectar y
            /// FlushPaseAlpha no debe dejar una pupila CADUCADA esperando al
            /// flush del frame siguiente (se dibujaría un frame tarde, en la
            /// posición vieja del cuerpo).
            /// </summary>
            internal void DescartarPaseAlpha() => _pendientesAlpha.Clear();

            // ------------------------------------------------------------------
            //  EL PRIMER MOLDE — EL OJO ALADO (los números EXACTOS de v6.41)
            // ------------------------------------------------------------------

            private static Molde _ojoAlado;

            /// <summary>
            /// EL PRIMER MOLDE DE LA CASA: el ojo alado del fragmento de
            /// supernova con LOS NÚMEROS EXACTOS de la réplica v6.41 —
            /// cuerpo crema-oro 110×40, alas naranjas ±0.44 con puntas
            /// rojas ±0.6, pupila 34×12 al 60% en pase alpha. Instancia
            /// COMPARTIDA (lazy, se construye al primer uso): la misma
            /// que usan el minion y cualquier futuro huésped. El render
            /// de FNA es single-thread — un check simple basta, sin lock.
            /// </summary>
            public static Molde OjoAlado
            {
                get
                {
                    if (_ojoAlado != null) return _ojoAlado;
                    Molde recien = CrearOjoAlado(1f);
                    _ojoAlado = recien;    // se publica COMPLETO, jamás a medias
                    return recien;
                }
            }

            /// <summary>
            /// LA FACTORY DEL OJO ALADO — el molde entero en parámetros:
            /// un jefe futuro re-usa la anatomía cambiando SOLO esto
            /// (talla, paleta, envergadura, aleteo) — horas, no días.
            /// Los defaults son LA RÉPLICA: CrearOjoAlado(1f) clava el
            /// look del fragmento píxel por píxel.
            /// </summary>
            /// <param name="escalaBase">La talla del cuerpo (1 = la del fragmento).</param>
            /// <param name="colorCuerpo">La cara del ojo (default: crema-oro 255,220,150).</param>
            /// <param name="colorAlas">Las alas (default: naranja 240,120,72).</param>
            /// <param name="colorPuntas">Las puntas de las alas (default: rojo oscuro 168,48,48).</param>
            /// <param name="colorPupila">El vacío (default: negro al 55%).</param>
            /// <param name="amplitudAleteo">El latido de las alas en rad (default 0: la divinidad NO aletea).</param>
            /// <param name="escalaAlas">La envergadura respecto al cuerpo (default 1: la del original).</param>
            public static Molde CrearOjoAlado(float escalaBase = 1f,
                Color? colorCuerpo = null, Color? colorAlas = null, Color? colorPuntas = null,
                Color? colorPupila = null, float amplitudAleteo = 0f, float escalaAlas = 1f)
            {
                // LA PALETA (las medidas EXACTAS del sprite original, con las
                // intensidades por pieza horneadas — cuerpo 0.85, ala 0.8,
                // punta 0.85: las mismas multiplicaciones de la réplica).
                Color cuerpo = (colorCuerpo ?? new Color(255, 220, 150)) * 0.85f;
                Color ala = (colorAlas ?? new Color(240, 120, 72)) * 0.8f;
                Color punta = (colorPuntas ?? new Color(168, 48, 48)) * 0.85f;
                Color pupila = colorPupila ?? Color.Black * 0.55f;

                Molde m = new Molde()
                    .ConLongitudCuerpo(110f)       // la referencia: el eje +X del cuerpo
                    .ConAmplitudBanking(0.03f)     // EL número del original (velocity.X·0.03)
                    .ConEscalaGlobal(escalaBase);

                // EL CUERPO: crema-oro horizontal (110×40) — la cara del ojo.
                m.NuevaPieza()
                    .ConOffset(Vector2.Zero)
                    .ConEscala(new Vector2(110f, 40f))
                    .ConColor(cuerpo)
                    .ConOrdenZ(0f)
                    .AlFinal();

                // LAS ALAS Y SUS PUNTAS: a los costados, inclinadas, espejadas.
                for (int lado = -1; lado <= 1; lado += 2)
                {
                    // El aleteo ESPEJO: fases separadas π — la silueta late
                    // quedándose simétrica (default quieta: amplitud 0).
                    float fase = lado < 0 ? 0f : MathHelper.Pi;
                    Vector2 fAlas = new Vector2(escalaAlas, escalaAlas);

                    // EL ALA: naranja, ±0.44 rad de inclinación.
                    m.NuevaPieza()
                        .ConOffset(new Vector2(38f * lado, -4f) * fAlas)
                        .ConEscala(new Vector2(52f, 16f) * fAlas)
                        .ConRotacion(lado * -0.44f)
                        .ConColor(ala)
                        .ConOrdenZ(10f)
                        .ConAleteo(amplitudAleteo, fase)
                        .ConFactorBanking(1.6f)    // las alas bancan más que el cuerpo: vive
                        .AlFinal();

                    // LA PUNTA: rojo oscuro al final de cada ala, ±0.6 rad.
                    m.NuevaPieza()
                        .ConOffset(new Vector2(62f * lado, -12f) * fAlas)
                        .ConEscala(new Vector2(22f, 10f) * fAlas)
                        .ConRotacion(lado * -0.6f)
                        .ConColor(punta)
                        .ConOrdenZ(20f)
                        .ConAleteo(amplitudAleteo, fase)
                        .ConFactorBanking(1.6f)
                        .AlFinal();
                }

                // LA PUPILA: el vacío horizontal del ojo — 34×12 al 60% —
                // PASE ALPHA: oscurece TODO el brillo AL FINAL (la lección
                // del mock v6.41: enterrada bajo el aditivo no oscurece nada).
                m.NuevaPieza()
                    .ConOffset(Vector2.Zero)
                    .ConEscala(new Vector2(34f, 12f) * 0.6f)
                    .ConColor(pupila)
                    .ConOrdenZ(100f)
                    .ConPaseAlpha()
                    .AlFinal();

                return m;
            }
        }

        /// <summary>
        /// EL CONSTRUCTOR FLUIDO DE PIEZAS: la pieza en construcción que
        /// devuelve <see cref="Molde.NuevaPieza"/>. Cada Con* moldea un
        /// aspecto y devuelve EL CONSTRUCTOR (para encadenar);
        /// <see cref="AlFinal"/> la commita al molde y devuelve EL MOLDE
        /// (para encadenar la pieza siguiente). Una sola instancia
        /// reutilizada por molde — construir también es cero GC.
        /// Defaults de la casa: SeguirDireccion=true (un cuerpo mira a
        /// donde va), VelocidadAleteo=6 (latido majestuoso),
        /// FactorBanking=1 (banca como el cuerpo).
        /// </summary>
        public sealed class ConstructorPieza
        {
            private readonly Molde _molde;
            private Pieza _pieza;

            internal ConstructorPieza(Molde molde)
            {
                _molde = molde;
                _pieza = default;
            }

            /// <summary>Reinicia la pieza en construcción con los
            /// defaults de la casa (llama Molde.NuevaPieza, no esto).</summary>
            internal void Reiniciar()
            {
                _pieza = default;
                _pieza.SeguirDireccion = true;
                _pieza.VelocidadAleteo = 6f;
                _pieza.FactorBanking = 1f;
            }

            /// <summary>El ancla de la pieza en espacio local (+X = frente).</summary>
            public ConstructorPieza ConOffset(Vector2 offset) { _pieza.Offset = offset; return this; }

            /// <summary>El tamaño final en px (x≠y = estirada).</summary>
            public ConstructorPieza ConEscala(Vector2 escala) { _pieza.Escala = escala; return this; }

            /// <summary>El tamaño final en px, por componentes.</summary>
            public ConstructorPieza ConEscala(float x, float y) { _pieza.Escala = new Vector2(x, y); return this; }

            /// <summary>La inclinación propia (rad, sobre el cuerpo ya girado).</summary>
            public ConstructorPieza ConRotacion(float rad) { _pieza.RotacionLocal = rad; return this; }

            /// <summary>El color de la pieza (con su alfa horneada).</summary>
            public ConstructorPieza ConColor(Color color) { _pieza.Color = color; return this; }

            /// <summary>Textura propia (null = SoftGlow de la casa).</summary>
            public ConstructorPieza ConTextura(Texture2D textura) { _pieza.Textura = textura; return this; }

            /// <summary>
            /// Marca la pieza para el PASE ALPHA: oscurece al final (el
            /// vacío, la sombra) en vez de brillar en el aditivo.
            /// </summary>
            public ConstructorPieza ConPaseAlpha() { _pieza.PaseAlpha = true; return this; }

            /// <summary>El orden de emisión (menor = antes: 0 cuerpo, 10 alas, 100 pupila).</summary>
            public ConstructorPieza ConOrdenZ(float z) { _pieza.OrdenZ = z; return this; }

            /// <summary>FALSE = la pieza se queda vertical aunque el cuerpo gire.</summary>
            public ConstructorPieza ConSeguirDireccion(bool seguir) { _pieza.SeguirDireccion = seguir; return this; }

            /// <summary>EL ALETEO propio: amplitud (rad) y fase (rad; las
            /// alas espejo van separadas π). Velocidad default 6 rad/s.</summary>
            public ConstructorPieza ConAleteo(float amplitud, float fase, float velocidad = 6f)
            {
                _pieza.AmplitudAleteo = amplitud;
                _pieza.FaseAleteo = fase;
                _pieza.VelocidadAleteo = velocidad;
                return this;
            }

            /// <summary>Cuánto bancan ESTA pieza (1 = como el cuerpo, 1.6 = alas vivas).</summary>
            public ConstructorPieza ConFactorBanking(float factor) { _pieza.FactorBanking = factor; return this; }

            /// <summary>COMMITA la pieza al molde y devuelve EL MOLDE —
            /// encadena la siguiente sin soltar la cadena.</summary>
            public Molde AlFinal()
            {
                _molde.Añadir(_pieza);
                return _molde;
            }
        }
    }
}
