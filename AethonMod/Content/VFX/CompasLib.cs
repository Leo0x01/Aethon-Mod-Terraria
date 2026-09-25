using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// CompasLib — v6.43 — LA COREOGRAFÍA DECLARATIVA DE ATAQUES CÍCLICOS.
    ///
    /// Idea central: hasta v6.42, cada arma cíclica armaba su switch A
    /// MANO — "si voy por el tick 28 hago esto, si pasa de 60 aquello" —
    /// con relojes, contadores y fases repartidos por la AI. Esta librería
    /// invierte el orden: el ciclo se DECLARA (un <see cref="Compas"/> =
    /// duración total + movimientos con ventanas [inicio, duración]
    /// medidas en TICKS del ciclo) y luego se CONSULTA sobre cualquier
    /// reloj: ¿qué movimiento está activo? ¿en qué fase del movimiento?
    /// ¿es su primer o último tick?
    ///
    ///   · El compás es un DATO: no tiene reloj, no tiene estado, no se
    ///     actualiza. El reloj lo lleva SIEMPRE el que llama (un ai[0..2]
    ///     de proyectil, GameUpdateCount, un contador local) — el compás
    ///     solo lo INTERPRETA. Por eso la lección de v6.27 (ai tiene 3
    ///     slots y ni uno más) no duele aquí: la coreografía no consume
    ///     ningún slot.
    ///   · STATELESS = multi-instanciable gratis: treinta proyectiles
    ///     comparten el MISMO compás estático, cada uno con su reloj.
    ///   · CERO GC: el <see cref="Instante"/> es un struct devuelto POR
    ///     VALOR; En() no aloca nada; los compases se construyen UNA vez
    ///     (static readonly en el proyectil) y su primera consulta congela
    ///     el orden de evaluación para siempre.
    ///
    /// La prueba visual: <see cref="DepurarDibujar"/> pinta el compás como
    /// una línea de tiempo (un rectángulo por movimiento, el cursor del
    /// reloj, el activo iluminado) — se activa con <see cref="Depuracion"/>
    /// (tecla <see cref="AtenderTecla"/>, F7) y se llama desde el PostDraw
    /// del proyectil que quiera VERLO mientras ajusta su coreografía.
    /// </summary>
    public static class CompasLib
    {
        // ------------------------------------------------------------------
        //  LAS CONSTANTES Y LA LLAVE DE LA DEPURACIÓN
        // ------------------------------------------------------------------

        /// <summary>
        /// Ticks de juego por segundo real: la física del mod va a 60 Hz por
        /// diseño — el puente entre "pienso la coreografía en segundos" y
        /// "la programo en ticks" (ver <see cref="VentanaAbsoluta(string, float, float)"/>).
        /// </summary>
        public const float TicksPorSegundo = 60f;

        /// <summary>
        /// LA LLAVE DE LA DEPURACIÓN: cuando está activa, los proyectiles
        /// que lo consulten dibujan su compás como línea de tiempo compacta
        /// (para VER la coreografía en juego mientras se ajusta). False por
        /// defecto — es herramienta de diseño, no un efecto. Se alterna con
        /// <see cref="AtenderTecla"/> (F7).
        /// </summary>
        public static bool Depuracion;

        /// <summary>
        /// El estado anterior de la tecla de depuración — detección de
        /// FLANCO: el toggle cuenta pulsaciones, no ticks de tecla mantenida.
        /// (Estado de la HERRAMIENTA, no de la coreografía: el compás sigue
        /// siendo un dato puro.)
        /// </summary>
        private static bool _teclaDepuracionAntes;

        /// <summary>
        /// La paleta FIJA de la línea de depuración, por índice de
        /// movimiento: determinista — JAMÁS Main.rand, ni siquiera aquí.
        /// </summary>
        private static readonly Color[] PaletaDepuracion =
        {
            new Color(90, 200, 255), new Color(255, 200, 90), new Color(160, 255, 140),
            new Color(255, 130, 190), new Color(200, 150, 255),
        };

        // ------------------------------------------------------------------
        //  LOS HELPERS SUELTOS — el patrón más común sin instanciar nada
        // ------------------------------------------------------------------

        /// <summary>
        /// ¿Está el reloj dentro de la ventana [inicio, inicio+duración) del
        /// ciclo? El patrón más común (una sola ventana, sin declarar compás
        /// entero): si está dentro, entrega la fase 0..1 y el tick dentro del
        /// movimiento — la MISMA matemática que <see cref="Compas.En"/> usa
        /// para resolver el instante (una sola fuente de verdad, cero
        /// divergencia entre la consulta suelta y la declarada).
        ///
        /// Convención de ventanas (la de toda la librería): el tick de
        /// inicio CUENTA, el de fin NO (pertenece al siguiente tramo). Una
        /// ventana que CRUZA el borde del ciclo (p. ej. [28..4) en un ciclo
        /// de 30) se recorre por el final — cíclica de verdad.
        /// </summary>
        /// <param name="reloj">El reloj del que llama (cualquier valor, también negativo: se envuelve al ciclo).</param>
        /// <param name="ciclo">Ticks totales del ciclo (se protege si llega vacío).</param>
        /// <param name="inicio">Tick del ciclo donde ARRANCA la ventana.</param>
        /// <param name="dur">Duración de la ventana en ticks.</param>
        /// <param name="fase01">La fase 0..1 dentro del movimiento (0 si no está dentro).</param>
        /// <param name="tickEnMov">Ticks desde el inicio del movimiento (−1 si no está dentro — el centinela distingue "fuera" de "en el tick 0").</param>
        public static bool EnVentana(float reloj, float ciclo, float inicio, float dur, out float fase01, out int tickEnMov)
        {
            fase01 = 0f;
            tickEnMov = -1;
            if (ciclo <= 0f || dur <= 0f) return false;

            float tc = reloj % ciclo;
            if (tc < 0f) tc += ciclo;   // relojes que nacen antes de empezar → rango [0, ciclo)

            float fase;
            if (inicio + dur <= ciclo)
            {
                fase = tc - inicio;
                if (fase < 0f || fase >= dur) return false;
            }
            else
            {
                // LA VENTANA CRUZA EL BORDE: [inicio..ciclo) ∪ [0..fin−ciclo)
                fase = tc >= inicio ? tc - inicio : tc + ciclo - inicio;
                if (fase < 0f || fase >= dur) return false;
            }

            fase01 = fase / dur;
            tickEnMov = (int)fase;
            return true;
        }

        /// <summary>
        /// EL METRÓNOMO SUELTO: dispara cuando el reloj cae en el tick 0 de
        /// cada periodo (cada 30, cada 120...). La comparación es por TICK
        /// ENTERO — (int)reloj % (int)periodo == 0 — y no por módulo
        /// flotante, y el porqué es sutil: un reloj fraccionario con
        /// decimales fijos (29.5, 30.5, 31.5...) NUNCA daría módulo cero y
        /// los disparos se PERDERÍAN; con el tick entero, el 30.5 cae en el
        /// tick 30 y dispara. (Contrapartida documentada: si el reloj avanza
        /// a menos de un tick por frame, el mismo tick entero se visita dos
        /// veces — para esos relojes, la herramienta correcta es el compás
        /// con ventanas.)
        /// </summary>
        public static bool Cada(float reloj, float periodo)
        {
            int p = (int)periodo;
            if (p < 1) return false;
            int r = (int)reloj;
            int m = r % p;
            if (m < 0) m += p;          // relojes negativos también ciclan
            return m == 0;
        }

        /// <summary>
        /// LAS VENTANAS DE DISEÑO EN SEGUNDOS: la coreografía se PIENSA en
        /// segundos de jugador ("el golpe tarda 0,25 s") y se PROGRAMA en
        /// ticks — esta conversión evita el 60 mágico desparramado por el
        /// código. Devuelve un <see cref="Movimiento"/> listo para
        /// <see cref="Compas.Con"/> (nombre neutro: usa la sobrecarga con
        /// nombre para leerlo en la línea de depuración).
        /// </summary>
        public static Movimiento VentanaAbsoluta(float inicioSeg, float durSeg)
            => VentanaAbsoluta("ventana", inicioSeg, durSeg);

        /// <summary>VentanaAbsoluta con nombre propio (el que se lee en la depuración).</summary>
        public static Movimiento VentanaAbsoluta(string nombre, float inicioSeg, float durSeg)
            => Movimiento.Nuevo(nombre, inicioSeg * TicksPorSegundo, durSeg * TicksPorSegundo);

        // ------------------------------------------------------------------
        //  LA DEPURACIÓN — ver el compás en juego
        // ------------------------------------------------------------------

        /// <summary>
        /// LA LLAVE (F7 por defecto): alterna <see cref="Depuracion"/> por
        /// PULSACIÓN (detección de flanco). El que quiera la depuración
        /// llama esto UNA vez por AI — el compás sigue sin tener estado:
        /// esto es herramienta, no coreografía. Solo cliente: el servidor
        /// no tiene teclado ni pantalla.
        /// </summary>
        public static void AtenderTecla(Keys tecla = Keys.F7)
        {
            if (Main.netMode == NetmodeID.Server) return;
            bool pulsada = Main.keyState.IsKeyDown(tecla);
            if (pulsada && !_teclaDepuracionAntes)
                Depuracion = !Depuracion;
            _teclaDepuracionAntes = pulsada;
        }

        /// <summary>
        /// LA PRUEBA VISUAL DEL COMPÁS: una línea de tiempo compacta — un
        /// rectángulo por movimiento (posicionado en [inicio..fin] del
        /// ciclo), el cursor del reloj actual y el movimiento ACTIVO
        /// iluminado con su nombre debajo. Se llama (p. ej. desde el
        /// PostDraw de un proyectil) SOLO si <see cref="Depuracion"/> está
        /// activo; false por defecto = coste cero en el juego normal.
        ///
        /// LOTE DE LA CASA, cerrado→cerrado: End defensivo (si no había
        /// lote abierto, no explota), Begin propio Inmediato en coords de
        /// pantalla PURAS (Matrix.Identity — el PORQUÉ: con la matriz del
        /// juego, el ZOOM del mundo desplazaría y desescalaría la línea
        /// del punto de pantalla que el llamador pidió; una HUD de
        /// depuración tiene que quedarse donde se la pide), los draws, y el
        /// End en finally — que una excepción JAMÁS deje el lote abierto
        /// y corrompa el render (la lección del volcado blindado v6.41) —
        /// con reapertura del lote estándar del juego al salir. Cero
        /// Main.rand: la paleta es fija por índice.
        /// </summary>
        /// <param name="c">El compás a inspeccionar (null = no dibuja).</param>
        /// <param name="reloj">El MISMO reloj con el que el llamador consulta el compás.</param>
        /// <param name="posicionPantalla">El centro de la línea de tiempo (coords de pantalla puras).</param>
        public static void DepurarDibujar(Compas c, float reloj, Vector2 posicionPantalla)
        {
            if (!Depuracion || c == null || Main.netMode == NetmodeID.Server) return;

            // (congela el compás si era su primera consulta — el orden de
            //  evaluación queda fijado de aquí en adelante)
            Instante inst = c.En(reloj);

            // v6.50.11 — sonda: cierra el lote del juego SOLO si hay Begin
            // vivo (cero first-chance — el rastreo del lote ajeno por sonda).
            bool habiaLote = VFXCore.LoteAbierto;
            if (habiaLote) Main.spriteBatch.End();

            try
            {
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Matrix.Identity);

                DibujarLineaDeTiempo(c, inst, posicionPantalla);
            }
            finally
            {
                VFXCore.CerrarLoteSiAbierto();
                // v6.50.11 — curación: el lote sale SIEMPRE ABIERTO y vanilla
                // (la rama condicional devolvía el veneno; idempotente por sonda).
                VFXCore.ReabrirLoteVanilla();
            }
        }

        /// <summary>La línea de tiempo en sí (dentro de un lote YA ABIERTO por <see cref="DepurarDibujar"/>).</summary>
        private static void DibujarLineaDeTiempo(Compas c, Instante inst, Vector2 centro)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            // Techo 360 px y suelo 120: ventanas diminutas no rompen la línea.
            float ancho = MathF.Max(120f, MathF.Min(360f, Main.screenWidth - 60f));
            const float alto = 14f;
            Vector2 esq = new Vector2(centro.X - ancho * 0.5f, centro.Y - alto * 0.5f);

            // EL LIENZO: banda oscura con filo (la pantalla del compás).
            DibujarRect(pixel, esq, ancho, alto, new Color(0, 0, 0) * 0.6f);
            DibujarRect(pixel, esq - Vector2.UnitY, ancho, 1f, new Color(255, 255, 255) * 0.35f);
            DibujarRect(pixel, esq + new Vector2(0f, alto), ancho, 1f, new Color(255, 255, 255) * 0.35f);

            // LOS MOVIMIENTOS: se pintan de prioridad BAJA a ALTA — la alta
            // queda ENCIMA (la misma resolución de solapes que En() usa al
            // elegir, hecha visible).
            Movimiento[] movs = c.Movimientos;
            for (int i = movs.Length - 1; i >= 0; i--)
            {
                Movimiento m = movs[i];
                bool activo = inst.En(m.Indice);
                float x = esq.X + m.Inicio / c.Ciclo * ancho;
                float w = MathF.Max(2f, m.Duracion / c.Ciclo * ancho);
                Color col = PaletaDepuracion[m.Indice % PaletaDepuracion.Length];
                DibujarRect(pixel, new Vector2(x, esq.Y), w, alto, col * (activo ? 0.95f : 0.35f));
                // LA VENTANA QUE CRUZA EL BORDE también vive al PRINCIPIO del
                // ciclo: se pinta su cola envuelta para que la línea diga la
                // verdad (la misma ventana cíclica que EnVentana recorre).
                if (m.Fin > c.Ciclo)
                {
                    float wCola = MathF.Max(2f, (m.Fin - c.Ciclo) / c.Ciclo * ancho);
                    DibujarRect(pixel, new Vector2(esq.X, esq.Y), wCola, alto,
                        col * (activo ? 0.95f : 0.35f));
                }
                if (activo)   // la barra brillante DEBAJO del activo
                    DibujarRect(pixel, new Vector2(x, esq.Y + alto + 2f), w, 2f, new Color(255, 255, 255) * 0.85f);
            }

            // EL CURSOR: dónde está el reloj AHORA dentro del ciclo.
            float cursor = esq.X + inst.FraccionCiclo * ancho;
            DibujarRect(pixel, new Vector2(cursor - 1f, esq.Y - 6f), 2f, alto + 12f, new Color(255, 255, 255) * 0.9f);

            // EL NOMBRE del movimiento activo (el string del propio compás:
            // cero alocación — DrawString no reserva memoria).
            if (!string.IsNullOrEmpty(inst.NombreActivo))
            {
                // En esta tML las fuentes de vanilla son DynamicSpriteFont
                // (ReLogic): sus DrawString son EXTENSIONES de ReLogic.Graphics
                // — el mismo camino del rótulo de OcasoSystem.
                DynamicSpriteFont fuente = FontAssets.ItemStack.Value;
                Main.spriteBatch.DrawString(fuente, inst.NombreActivo,
                    new Vector2(esq.X, esq.Y - 24f), new Color(255, 255, 255) * 0.9f);
            }
        }

        /// <summary>
        /// Un rectángulo sólido con el MagicPixel de vanilla: escala por
        /// tamaño REAL de la textura (da igual su resolución) — el primitivo
        /// de las barras y cursores de la línea de depuración.
        /// </summary>
        private static void DibujarRect(Texture2D tex, Vector2 esquina, float ancho, float alto, Color color)
        {
            if (ancho <= 0f || alto <= 0f || color.A == 0) return;
            Vector2 escala = new Vector2(ancho / tex.Width, alto / tex.Height);
            Main.spriteBatch.Draw(tex, esquina, null, color, 0f, Vector2.Zero,
                escala, SpriteEffects.None, 0f);
        }
    }

    // ======================================================================
    //  EL TRAMO — Movimiento
    // ======================================================================

    /// <summary>
    /// MOVIMIENTO — un tramo de la coreografía: una ventana de ticks
    /// [Inicio, Inicio+Duración) dentro del ciclo, con nombre y prioridad.
    /// Es un DATO plano (struct): se construye con
    /// <see cref="Nuevo(string, float, float)"/> (+ <see cref="ConPrioridad"/>
    /// si compite) y se entrega a <see cref="Compas.Con"/> en la declaración
    /// del compás — nunca se guarda ni se muta en el bucle caliente.
    /// </summary>
    public struct Movimiento
    {
        /// <summary>
        /// El nombre legible del tramo ("Tic", "Barrido2"...). Al ser
        /// literales internados de la declaración, compararlos es la vía
        /// rápida por referencia — y la vía rapidísima es el
        /// <see cref="Indice"/>.
        /// </summary>
        public string Nombre;

        /// <summary>Tick del ciclo en el que ARRANCA la ventana (ese tick cuenta: la ventana es [Inicio, Inicio+Duración)).</summary>
        public float Inicio;

        /// <summary>
        /// Duración de la ventana en ticks. Si Inicio+Duración pasa del
        /// ciclo, la ventana CRUZA el borde y se recorre por el final (un
        /// "Tic" que empieza en 29 de un ciclo de 30 y dura 2 vive en los
        /// ticks 29 y 0 — cíclico de verdad).
        /// </summary>
        public float Duracion;

        /// <summary>
        /// Si dos ventanas se SOLAPAN, gana la de prioridad MAYOR — el
        /// solape pasa de accidente a HERRAMIENTA: un "Tic" de un tick
        /// encima de una "Onda" que cubre el ciclo entero, un tramo de
        /// énfasis que pisa a su fondo. Empate de prioridad → orden de
        /// declaración.
        /// </summary>
        public int Prioridad;

        /// <summary>
        /// La posición del movimiento EN SU COMPÁS: 0, 1, 2... la fija el
        /// ORDEN de <see cref="Compas.Con"/> al declararlo. El PORQUÉ de
        /// este campo: en el bucle caliente se compara por INT — inmune a
        /// typos de string y sin tocar un solo carácter
        /// (<see cref="Instante.En(int)"/> es la vía rápida). Sin compás
        /// vale 0 y NO significa nada.
        /// </summary>
        public int Indice;

        /// <summary>El tick del ciclo en el que TERMINA la ventana (exclusivo: el primero del siguiente tramo).</summary>
        public float Fin => Inicio + Duracion;

        /// <summary>Builder: el tramo con nombre, inicio y duración (prioridad 0 — la base).</summary>
        public static Movimiento Nuevo(string nombre, float inicio, float duracion) => new Movimiento
        {
            Nombre = nombre,
            Inicio = inicio,
            Duracion = duracion,
            Prioridad = 0,
            Indice = 0,
        };

        /// <summary>Builder encadenable: eleva la prioridad (los solapes se resuelven a su favor).</summary>
        public Movimiento ConPrioridad(int prioridad)
        {
            Prioridad = prioridad;
            return this;
        }
    }

    // ======================================================================
    //  EL CICLO — Compas
    // ======================================================================

    /// <summary>
    /// COMPÁS — LA COREOGRAFÍA DECLARADA: un ciclo (ticks totales) y sus
    /// movimientos. Es un DATO sin reloj y sin estado: se construye UNA vez
    /// (static readonly en el proyectil que lo usa), la primera consulta lo
    /// congela y a partir de ahí es INMUTABLE — cualquier número de
    /// proyectiles puede compartirlo, cada uno con SU reloj.
    ///
    /// <code>
    ///   private static readonly Compas Salto = Compas.Nuevo(120f)
    ///       .Con(Movimiento.Nuevo("Carga", 0f, 40f).ConPrioridad(2))
    ///       .Con(Movimiento.Nuevo("Salto", 40f, 20f))
    ///       .Con(Movimiento.Nuevo("Aterrizaje", 60f, 60f));
    ///   ...
    ///   Instante inst = Salto.En(Projectile.ai[0]);   // la AI consulta, no cuenta
    /// </code>
    /// </summary>
    public class Compas
    {
        /// <summary>
        /// Los ticks TOTALES del ciclo. El reloj se envuelve aquí (reloj mod
        /// ciclo): un proyectil que lleve 1000 ticks cae en el tick 1000 mod
        /// ciclo del compás — por eso NO hace falta reiniciar contadores.
        /// </summary>
        public float Ciclo { get; private set; }

        /// <summary>
        /// Los movimientos en ORDEN DE EVALUACIÓN (prioridad descendente;
        /// empate → orden de declaración): se llena al CONGELARSE (primera
        /// consulta o <see cref="Congelar"/> explícito) y es inmutable
        /// después. El array es de SOLO LECTURA POR CONTRATO — no reordenar
        /// ni mutar: se comparte como static readonly (leer un elemento
        /// devuelve una COPIA del struct, pero el array es de todos).
        /// </summary>
        public Movimiento[] Movimientos { get; private set; }

        /// <summary>La lista de construcción (viva solo hasta la congelación).</summary>
        private readonly List<Movimiento> _porDeclarar = new List<Movimiento>(8);

        /// <summary>¿Ya está congelado (inmutable, en marcha)?</summary>
        public bool Congelado { get; private set; }

        /// <summary>
        /// Builder: el compás de un ciclo de <paramref name="ciclo"/> ticks
        /// (se protege a 1 si llega vacío: un compás sin duración no mide
        /// nada y no puede tirar división).
        /// </summary>
        public static Compas Nuevo(float ciclo) => new Compas
        {
            Ciclo = ciclo > 0f ? ciclo : 1f,
            Movimientos = Array.Empty<Movimiento>(),
        };

        /// <summary>
        /// Builder encadenable: AÑADE movimientos (el orden de añadido fija
        /// sus <see cref="Movimiento.Indice"/>: 0, 1, 2...). Es API de
        /// DECLARACIÓN (una vez, al construir el static readonly) — el
        /// params aloca su array en cada llamada, y ese es su único sitio
        /// legítimo. Tras la primera consulta el compás está congelado y
        /// añadir más es un error de programación: se LANZA — mejor
        /// explotar en desarrollo que corromper la coreografía en silencio.
        /// </summary>
        public Compas Con(params Movimiento[] movimientos)
        {
            if (Congelado)
                throw new InvalidOperationException(
                    "El compás ya está en marcha (se consultó): inmutable tras construir.");
            if (movimientos == null) return this;
            for (int i = 0; i < movimientos.Length; i++)
            {
                Movimiento m = movimientos[i];
                m.Indice = _porDeclarar.Count;
                _porDeclarar.Add(m);
            }
            return this;
        }

        /// <summary>
        /// Congela el compás (idempotente): copia los movimientos y los
        /// ordena por PRIORIDAD DESCENDENTE con desempate por orden de
        /// declaración — así <see cref="En"/> recorre y el PRIMERO que pilla
        /// ventana gana, sin comparar prioridades tick a tick. El coste se
        /// paga UNA vez (la primera consulta lo hace sola), no por frame.
        /// </summary>
        public void Congelar()
        {
            if (Congelado) return;
            Congelado = true;
            Movimientos = _porDeclarar.ToArray();
            _porDeclarar.Clear();
            Array.Sort(Movimientos, (a, b) =>
            {
                int porPrioridad = b.Prioridad.CompareTo(a.Prioridad);
                return porPrioridad != 0 ? porPrioridad : a.Indice.CompareTo(b.Indice);
            });
        }

        /// <summary>
        /// RESUELVE el estado del compás sobre el reloj dado: qué movimiento
        /// está activo, en qué fase, ticks transcurridos, flancos. El reloj
        /// lo lleva EL QUE LLAMA (ai[], GameUpdateCount, un contador local):
        /// En() solo lo envuelve al ciclo y consulta ventanas — cero
        /// alocaciones (el Instante vuelve POR VALOR).
        ///
        /// Convención de ventanas: [Inicio, Inicio+Duración) — el tick de
        /// inicio cuenta, el de fin NO (pertenece al siguiente tramo): un
        /// "Tic" [0..1) vive EXACTAMENTE en el tick 0. Si varias ventanas
        /// pisan el mismo tick, gana la de mayor prioridad; si NINGUNA lo
        /// cubre, el instante queda en la intro (NombreActivo = "").
        /// </summary>
        public Instante En(float reloj)
        {
            if (!Congelado) Congelar();

            Instante inst = default;
            inst.NombreActivo = "";

            float ciclo = Ciclo;
            float tc = reloj % ciclo;
            if (tc < 0f) tc += ciclo;      // relojes que nacen negativos → rango [0, ciclo)
            inst.TickEnCiclo = (int)tc;
            inst.FraccionCiclo = tc / ciclo;

            Movimiento[] movs = Movimientos;
            for (int i = 0; i < movs.Length; i++)
            {
                Movimiento m = movs[i];    // prioridad DESC: el primero que pilla ventana gana
                float dur = m.Duracion;
                if (dur <= 0f) continue;

                float fase;
                if (m.Inicio + dur <= ciclo)
                {
                    fase = tc - m.Inicio;
                    if (fase < 0f || fase >= dur) continue;
                }
                else
                {
                    // LA VENTANA CRUZA EL BORDE: [inicio..ciclo) ∪ [0..fin−ciclo)
                    fase = tc >= m.Inicio ? tc - m.Inicio : tc + ciclo - m.Inicio;
                    if (fase < 0f || fase >= dur) continue;
                }

                inst.NombreActivo = m.Nombre;
                inst.Movimiento = m;
                inst.Fase01 = fase / dur;
                inst.TickEnMovimiento = (int)fase;
                inst.PrimerTick = fase < 1f;          // aún dentro del tick 0 (los relojes fraccionarios cuentan)
                inst.UltimoTick = fase >= dur - 1f;   // el último tick que la ventana posee
                break;
            }
            return inst;
        }
    }

    // ======================================================================
    //  LA RESPUESTA — Instante
    // ======================================================================

    /// <summary>
    /// INSTANTE — LA RESPUESTA del compás: el estado del reloj consultado,
    /// ya resuelto. Struct puro: vuelve POR VALOR, se copia y se guarda sin
    /// pensar, cero GC. "" en <see cref="NombreActivo"/> = NINGÚN movimiento
    /// (la intro del ciclo — el hueco entre ventanas).
    /// </summary>
    public struct Instante
    {
        /// <summary>El nombre del movimiento activo ("" = ninguno: la intro del ciclo).</summary>
        public string NombreActivo;

        /// <summary>
        /// La fase 0→1 DENTRO del movimiento activo (SU reloj particular; 0
        /// si no hay activo). No confundir con <see cref="FraccionCiclo"/>:
        /// la fase se reinicia en cada tramo; la fracción es del ciclo
        /// entero.
        /// </summary>
        public float Fase01;

        /// <summary>Ticks desde que ARRANCÓ el movimiento activo (0 en su primer tick; 0 si no hay activo).</summary>
        public int TickEnMovimiento;

        /// <summary>El reloj ENVUELTO al ciclo: reloj mod ciclo (0..ciclo−1) — el "qué hora es" del compás.</summary>
        public int TickEnCiclo;

        /// <summary>¿Es este el PRIMER tick del movimiento? (con relojes fraccionarios: mientras no se cumpla un tick entero).</summary>
        public bool PrimerTick;

        /// <summary>
        /// ¿Es este el ÚLTIMO tick del movimiento? Para los disparos de
        /// cierre (sonidos finales, ondas de despedida) — el flanco que más
        /// se escribe a mano en los switches viejos y aquí sale gratis.
        /// </summary>
        public bool UltimoTick;

        /// <summary>El movimiento ACTIVO tal cual (default — Duracion 0, Nombre null — si nadie está activo).</summary>
        public Movimiento Movimiento;

        /// <summary>
        /// La posición 0→1 dentro del CICLO COMPLETO (reloj/ciclo) —
        /// independiente de los movimientos: la curva "de siempre" para los
        /// efectos que abarcan todo el giro (una onda que nace en el tic y
        /// se apaga al siguiente es una función de la FRACCIÓN del ciclo,
        /// no de la fase de un tramo).
        /// </summary>
        public float FraccionCiclo;

        /// <summary>
        /// ¿El activo es el movimiento de este nombre? Comparación ORDINAL:
        /// sin alocar (los literales internados de la declaración caen en la
        /// vía rápida de igualdad por referencia). Para el bucle caliente,
        /// <see cref="En(int)"/> por índice.
        /// </summary>
        public bool En(string nombre) => string.Equals(NombreActivo, nombre, StringComparison.Ordinal);

        /// <summary>
        /// ¿El activo es el movimiento número <paramref name="indiceMovimiento"/>?
        /// (el orden de .Con fija los índices — la vía SIN strings del bucle
        /// caliente). Falso si no hay movimiento activo.
        /// </summary>
        public bool En(int indiceMovimiento)
            => NombreActivo != null && NombreActivo.Length > 0 && Movimiento.Indice == indiceMovimiento;

        /// <summary>
        /// EASE-IN SUAVE desde el arranque del movimiento: 0 en su tick 0, 1
        /// a los <paramref name="ticks"/> ticks, con el smoothstep
        /// (p·p·(3−2·p) — pendiente nula en ambos extremos: el arranque no
        /// "salta" ni "frena" de golpe). Basado en TickEnMovimiento — el
        /// RELOJ DEL JUEGO, no el del render: la lección de la casa (el
        /// render late distinto que el juego).
        /// </summary>
        public float SuavizadoInicio(float ticks)
        {
            float p = TickEnMovimiento / MathF.Max(1f, ticks);
            if (p < 0f) p = 0f;
            if (p > 1f) p = 1f;
            return p * p * (3f - 2f * p);
        }

        /// <summary>
        /// PULSO determinista 0..1: el seno de TickEnMovimiento por la
        /// frecuencia. El PORQUÉ del tick (y no del tiempo de render): dos
        /// instancias del mismo compás laten IGUAL y en todas las máquinas,
        /// siempre — un latido de coreografía tiene que ser reproducible,
        /// no decorativo.
        /// </summary>
        public float Pulso(float frecuencia)
            => 0.5f + 0.5f * MathF.Sin(TickEnMovimiento * frecuencia);
    }
}
