using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// Pantalla — v6.43 — LA LIBRERÍA DE EFECTOS DE PANTALLA.
    ///
    /// Idea central: hasta ahora cada explosión grande del mod armaba SU
    /// golpe de pantalla a mano (un PunchCameraModifier aquí, un velo
    /// aditivo allá, nada de viñeta). PantallaLib reúne los CUATRO gestos
    /// de pantalla del cine en una sola API declarativa:
    ///
    ///   · <see cref="Sacudir"/> — la SACUDIDA con modelo trauma (la cámara
    ///     tiembla por <see cref="SacudidaTrauma"/>, un modificador REAL del
    ///     motor: Terraria.Graphics.CameraModifiers.ICameraModifier).
    ///   · <see cref="Flash"/> — el FLASH de pantalla (hasta 4 simultáneos).
    ///   · <see cref="Vineta"/> — la VIÑETA (el túnel que se cierra sobre
    ///     el jugador: 10 plumones de SoftGlow NEGRO en anillo).
    ///   · <see cref="OndaExpansiva"/> — la ONDA (anillo de choque que se
    ///     expande por la capa de interfaz, en coords de pantalla).
    ///
    /// CONTRATO DE TIEMPO (el de la casa): cada efecto guarda SOLO el
    /// <c>frameInicio</c> (double de <see cref="Main.GameUpdateCount"/>) y
    /// sus parámetros; el progreso se DERIVA cada frame
    /// ((GameUpdateCount − inicio)/(60·dur)). CERO timers mutables, CERO
    /// edad acumulada: los efectos no se "actualizan", se CONSULTAN — y el
    /// auto-entierro (cuando el progreso pasa de 1) vive en el dibujado.
    ///
    /// CONTRATO DE MEMORIA: todo el estado vive en ARRAYS ESTÁTICOS
    /// pre-asignados (4 sacudidas, 4 flashes, 1 viñeta, 6 ondas + 4
    /// proxies <see cref="SacudidaTrauma"/> de nacimiento eterno). Un frame
    /// típico no aloca NADA: los senos y las curvas son matemática de pila.
    ///
    /// CONTRATO DE SUERTE: CERO Main.rand en el render — el offset de la
    /// sacudida son DOS SENOS INCONMENSURABLES (17.13 y 29.7 rad/s) y la
    /// deriva de la viñeta es lineal en t: todas las máquinas del mundo
    /// tiemblan EXACTAMENTE igual, sin sincronizar un bit.
    ///
    /// Todo CLIENTE-ONLY: los servidores no tienen pantalla (guarda interna
    /// en cada puerta) y el dibujado vive en
    /// <see cref="PantallaSistema.PostDrawInterface(SpriteBatch)"/> con las
    /// guardas de la casa (menú y servidor fuera).
    /// </summary>
    public static class Pantalla
    {
        // ==================================================================
        //  EL ESTADO PRE-ASIGNADO (cero GC por frame)
        // ==================================================================

        /// <summary>
        /// UNA SACUDIDA VIVA (la fila de la tabla de trauma): solo el
        /// contrato de tiempo (frame de nacimiento + duración) y los
        /// parámetros. El trauma ACTUAL se deriva, jamás se guarda.
        /// </summary>
        internal struct SacudidaSlot
        {
            /// <summary>¿Hay un temblor vivo aquí?</summary>
            public bool Activo;
            /// <summary>Identidad lógica (null = anónima): mismo id → REFRESCAR, no apilar.</summary>
            public string Id;
            /// <summary>Píxeles de amplitud al 100% de trauma (tope 14 = el cap histórico de la casa).</summary>
            public float Intensidad;
            /// <summary>Trauma de NACIMIENTO (0..1). El actual = inicial·(1−progreso) — lineal a 0.</summary>
            public double TraumaInicial;
            /// <summary>Ventana de decaimiento completa, en segundos.</summary>
            public double DuracionSeg;
            /// <summary>GameUpdateCount del nacimiento/último refresco.</summary>
            public double FrameInicio;
        }

        /// <summary>UN FLASH de pantalla (el contrato mínimo: color + curva).</summary>
        private struct FlashSlot
        {
            public bool Activo;
            public Color Color;
            public float Intensidad;
            public double DuracionSeg;
            public double FrameInicio;
        }

        /// <summary>LA VIÑETA (única: la nueva reemplaza a la vieja).</summary>
        private struct VinetaSlot
        {
            public bool Activo;
            public float Intensidad;
            public double DuracionSeg;
            public double FrameInicio;
        }

        /// <summary>UNA ONDA EXPANSIVA de la capa de interfaz.</summary>
        private struct OndaSlot
        {
            public bool Activo;
            public Vector2 CentroMundo;
            public float RadioMax;
            public double DuracionSeg;
            public double FrameInicio;
            public Color Color;
            public float Grosor;
        }

        /// <summary>Máx 4 sacudidas simultáneas (el techo del contrato).</summary>
        internal static readonly SacudidaSlot[] _sacudidas = new SacudidaSlot[4];

        /// <summary>
        /// Los PROXIES de la sacudida: nacen UNA vez (uno por slot) y viven
        /// para siempre — el motor los expulsa solo cuando se declaran
        /// <see cref="SacudidaTrauma.Finished"/> (ClearFinishedModifiers
        /// corre en cada ApplyTo del motor, medido en el binario real), y
        /// <see cref="Sacudir"/> los vuelve a enganchar al revivir su slot.
        /// Cero asignaciones por sacudida.
        /// </summary>
        internal static readonly SacudidaTrauma[] _proxies =
        {
            new SacudidaTrauma(0), new SacudidaTrauma(1), new SacudidaTrauma(2), new SacudidaTrauma(3),
        };

        /// <summary>Máx 4 flashes simultáneos.</summary>
        private static readonly FlashSlot[] _flashes = new FlashSlot[4];

        /// <summary>LA viñeta (máx 1: la nueva REEMPLAZA).</summary>
        private static VinetaSlot _vineta;

        /// <summary>Máx 6 ondas simultáneas.</summary>
        private static readonly OndaSlot[] _ondas = new OndaSlot[6];

        /// <summary>
        /// ¿Se cerró el lote de la interfaz en ESTE frame? La ponen los
        /// métodos de dibujado justo tras su End defensivo, y
        /// <see cref="PantallaSistema"/> la consulta en finally: SOLO se
        /// reabre el lote estándar si algo se cerró de verdad — el frame
        /// vacío no toca UNA sola vez el spriteBatch (la lección v6.90 de
        /// las first-chance silenciosas por frame: jamás un Begin sobre un
        /// lote ya abierto).
        /// </summary>
        internal static bool _loteDeUiCerrado;

        // ==================================================================
        //  API PÚBLICA — SACUDIR (modelo trauma)
        // ==================================================================

        /// <summary>
        /// SACA LA CÁMARA con el modelo TRAUMA: trauma decae LINEAL a 0 en
        /// <paramref name="duracionSeg"/> y la sacudida real es trauma² —
        /// la curva canónica del game-feel: los golpecitos se sienten
        /// contenidos (0.6² = 0.36) y el golpe de trauma 1 golpea entero.
        ///
        /// ESCALA DE LA CASA: 10 de intensidad = trauma 1.0 (la supernova
        /// del mod ES el trauma máximo de la casa). La amplitud de pico es
        /// trauma²·min(intensidad, 14) px — Sacudir(10) da 10 px de pico
        /// (paridad exacta con el PunchCameraModifier de 10 que vivía en la
        /// supernova); por debajo, la curva atenúa por diseño.
        ///
        /// El offset es DETERMINISTA: dos senos inconmensurables
        /// (sin(t·17.13), sin(t·29.7)) — JAMÁS Main.rand: la misma
        /// sacudida en todas las máquinas.
        ///
        /// MÁX 4 VIVAS. Si llega una con el MISMO <paramref name="id"/> →
        /// el trauma se REFRESCA al máximo (nunca se APILA — apilar trauma
        /// es el camino clásico al mareo). Sin id (anónima) ocupa un slot
        /// libre; si las 4 viven, desaloja la de trauma ACTUAL más bajo.
        /// </summary>
        /// <param name="intensidad">Magnitud en píxeles (10 = trauma 1.0, tope 14).</param>
        /// <param name="duracionSeg">Ventana completa del decaído lineal (0.05..8).</param>
        /// <param name="id">Identidad lógica (mismo id → refrescar, no apilar).</param>
        public static void Sacudir(float intensidad, float duracionSeg = 0.35f, string id = null)
        {
            // El servidor no tiene cámara que temblar.
            if (Main.netMode == NetmodeID.Server) return;
            if (intensidad <= 0f) return;
            duracionSeg = MathHelper.Clamp(duracionSeg, 0.05f, 8f);

            // --- 1. ¿QUÉ SLOT? mismo id → refrescar; si no, libre; si no,
            //        la más agonizante (la de trauma ACTUAL más bajo). ---
            int slot = -1;
            if (id != null)
            {
                for (int i = 0; i < _sacudidas.Length; i++)
                    if (_sacudidas[i].Activo && _sacudidas[i].Id == id) { slot = i; break; }
            }
            if (slot < 0)
            {
                for (int i = 0; i < _sacudidas.Length; i++)
                    if (!_sacudidas[i].Activo) { slot = i; break; }
            }
            if (slot < 0)
            {
                double traumaMinimo = double.MaxValue;
                for (int i = 0; i < _sacudidas.Length; i++)
                {
                    double traumaDeEse = TraumaActual(ref _sacudidas[i]);
                    if (traumaDeEse < traumaMinimo) { traumaMinimo = traumaDeEse; slot = i; }
                }
            }

            // --- 2. EL TRAUMA: saturado por la escala de la casa... ---
            double trauma = MathHelper.Clamp(intensidad / 10f, 0f, 1f);

            // --- ...y si el slot ya vivía, REFRESCA al máximo de los dos
            //        (JAMÁS suma: apilar trauma es el camino al mareo). ---
            ref SacudidaSlot s = ref _sacudidas[slot];
            if (s.Activo)
                trauma = Math.Max(trauma, TraumaActual(ref s));

            s.Activo = true;
            s.Id = id;
            s.Intensidad = MathHelper.Clamp(intensidad, 0f, 14f);
            s.TraumaInicial = trauma;
            s.DuracionSeg = duracionSeg;
            s.FrameInicio = Main.GameUpdateCount;

            // --- 3. ENGANCHAR EL PROXY con el motor (una sola vez por
            //        vida: el Add del motor deduplica por identidad —
            //        medido en el binario — y EnPila evita el round-trip). ---
            SacudidaTrauma proxy = _proxies[slot];
            proxy.Reiniciar();
            if (!proxy.EnPila)
            {
                try
                {
                    Main.instance.CameraModifiers.Add(proxy);
                    proxy.EnPila = true;
                }
                catch
                {
                    // El motor no está (carga temprana, menú sin mundo): la
                    // sacudida queda en la tabla — el próximo intento reengancha.
                    proxy.EnPila = false;
                }
            }
        }

        /// <summary>Trauma ACTUAL de un slot (derivado — jamás almacenado).</summary>
        private static double TraumaActual(ref SacudidaSlot s)
        {
            if (!s.Activo || s.DuracionSeg <= 0.0) return 0.0;
            double p = (Main.GameUpdateCount - s.FrameInicio) / (60.0 * s.DuracionSeg);
            if (p >= 1.0) return 0.0;
            return s.TraumaInicial * (1.0 - p);
        }

        // ==================================================================
        //  API PÚBLICA — FLASH
        // ==================================================================

        /// <summary>
        /// EL FLASH: un velo de COLOR a pantalla completa (MagicPixel) que
        /// se desvanece con ease-out cuadrático — alfa = intensidad·(1−p)².
        /// Hasta 4 SIMULTÁNEOS (cada uno con su color y su reloj); si
        /// llegan más, la nueva ocupa el slot del más cercano a morir.
        ///
        /// El canal alfa del color se IGNORA: el dueño del alfa es la
        /// envolvente (el flash es una curva, no un color fijo).
        /// </summary>
        /// <param name="color">El color del velo (RGB; el alfa lo pone la curva).</param>
        /// <param name="duracionSeg">Vida completa (0.05..8).</param>
        /// <param name="intensidad">Alfa de nacimiento (0..1).</param>
        public static void Flash(Color color, float duracionSeg = 0.25f, float intensidad = 0.6f)
        {
            if (Main.netMode == NetmodeID.Server) return;
            if (intensidad <= 0f) return;
            duracionSeg = MathHelper.Clamp(duracionSeg, 0.05f, 8f);
            intensidad = MathHelper.Clamp(intensidad, 0f, 1f);

            int slot = -1;
            for (int i = 0; i < _flashes.Length; i++)
                if (!_flashes[i].Activo) { slot = i; break; }
            if (slot < 0)
            {
                // Los 4 vivos: cede el asiento el más desvanecido.
                double progresoMaximo = -1.0;
                for (int i = 0; i < _flashes.Length; i++)
                {
                    double p = (Main.GameUpdateCount - _flashes[i].FrameInicio) / (60.0 * _flashes[i].DuracionSeg);
                    if (p > progresoMaximo) { progresoMaximo = p; slot = i; }
                }
            }

            _flashes[slot] = new FlashSlot
            {
                Activo = true,
                Color = color,
                Intensidad = intensidad,
                DuracionSeg = duracionSeg,
                FrameInicio = Main.GameUpdateCount,
            };
        }

        // ==================================================================
        //  API PÚBLICA — VIÑETA
        // ==================================================================

        /// <summary>
        /// LA VIÑETA: el túnel que se CIERRA sobre el jugador — 10 plumones
        /// de <see cref="VFXCore.SoftGlow"/> NEGRO, GIGANTES (cada quad
        /// cubre la diagonal de la pantalla: escala ≈ diagonal/tamaño), en
        /// anillo alrededor del centro. Los núcleos de los glows asoman
        /// justo tras las ESQUINAS: el centro queda LIMPIO y los bordes se
        /// oscurecen suave — la viñeta clásica, sin render targets, sin
        /// shaders, con la textura de la casa.
        ///
        /// MÁX 1 VIVA: la nueva REEMPLAZA a la vieja (dos túneles
        /// superpuestos es solo ruido visual). Envolvente: fade-in FIJO de
        /// 0.15 s (la pupila reaccionando), hold, y fade-out en el 30%
        /// final de la vida.
        /// </summary>
        /// <param name="intensidad">Oscuridad de pico en los bordes (0..1; ~0.32 por plumón).</param>
        /// <param name="duracionSeg">Vida completa (0.4..12).</param>
        public static void Vineta(float intensidad, float duracionSeg = 2f)
        {
            if (Main.netMode == NetmodeID.Server) return;
            intensidad = MathHelper.Clamp(intensidad, 0f, 1f);
            duracionSeg = MathHelper.Clamp(duracionSeg, 0.4f, 12f);

            // (máx 1: la nueva REEMPLAZA — la vieja se pisa sin ceremonia)
            _vineta = new VinetaSlot
            {
                Activo = true,
                Intensidad = intensidad,
                DuracionSeg = duracionSeg,
                FrameInicio = Main.GameUpdateCount,
            };
        }

        // ==================================================================
        //  API PÚBLICA — ONDA EXPANSIVA
        // ==================================================================

        /// <summary>
        /// LA ONDA EXPANSIVA: un anillo de choque (textura
        /// <see cref="VFXCore.Ring"/>) que nace en
        /// <paramref name="centroMundo"/> y se expande 0→
        /// <paramref name="radioMaxPx"/> con EASE-OUT CÚBICO (sale
        /// disparada y frena al final: el frente pierde energía con el
        /// radio, la física del cine).
        ///
        /// SE DIBUJA EN LA CAPA DE INTERFAZ: coords de pantalla =
        /// centroMundo − Main.screenPosition, en lote propio ADITIVO con
        /// Matrix.Identity — SIN traslación de cámara (la onda NO vuelve a
        /// temblar con la sacudida que la engendró: el impacto es un hecho
        /// de pantalla, no del mundo). A zoom ≠ 1 el anclaje es aproximado
        /// por contrato (la capa de UI no conoce la matriz del mundo); a
        /// zoom 1 es exacto.
        ///
        /// EL GROSOR DECRECE con el progreso: la textura Ring tiene su
        /// trazo calibrado proporcional al radio (medido: ~4% del radio a
        /// media alfa), así que el grosor pedido se compone con PLUMONES
        /// CONCÉNTRICOS (hasta 8) espaciados un trazo — al adelgazar el
        /// grosor pedido, los plumones se retiran solos: el anillo se
        /// afina de VERDAD, no solo se desvanece.
        ///
        /// MÁX 6 VIVAS; si llegan más, la nueva reemplaza a la más cercana
        /// a morir.
        /// </summary>
        /// <param name="centroMundo">Centro de la explosión (coords de mundo).</param>
        /// <param name="radioMaxPx">Radio final del frente (px de pantalla).</param>
        /// <param name="duracionSeg">Vida completa (0.1..5).</param>
        /// <param name="color">Color del anillo (null = blanco).</param>
        /// <param name="grosor">Grosor de nacimiento en px (2..160).</param>
        public static void OndaExpansiva(Vector2 centroMundo, float radioMaxPx, float duracionSeg = 0.4f,
            Color? color = null, float grosor = 40f)
        {
            if (Main.netMode == NetmodeID.Server) return;
            radioMaxPx = MathHelper.Clamp(radioMaxPx, 8f, 3000f);
            duracionSeg = MathHelper.Clamp(duracionSeg, 0.1f, 5f);
            grosor = MathHelper.Clamp(grosor, 2f, 160f);

            int slot = -1;
            for (int i = 0; i < _ondas.Length; i++)
                if (!_ondas[i].Activo) { slot = i; break; }
            if (slot < 0)
            {
                // Las 6 vivas: cede la más expandida (la que ya contó su historia).
                double progresoMaximo = -1.0;
                for (int i = 0; i < _ondas.Length; i++)
                {
                    double p = (Main.GameUpdateCount - _ondas[i].FrameInicio) / (60.0 * _ondas[i].DuracionSeg);
                    if (p > progresoMaximo) { progresoMaximo = p; slot = i; }
                }
            }

            _ondas[slot] = new OndaSlot
            {
                Activo = true,
                CentroMundo = centroMundo,
                RadioMax = radioMaxPx,
                DuracionSeg = duracionSeg,
                FrameInicio = Main.GameUpdateCount,
                Color = color ?? Color.White,
                Grosor = grosor,
            };
        }

        // ==================================================================
        //  v6.49 — LOS PRESETS DE PANTALLA (la idea nº1 de la auditoría
        //  AUD-A): los CUATRO gestos (sacudir + flash + viñeta + onda)
        //  calibrados juntos en UNA llamada — el golpe que YA se lee como
        //  golpe sin que cada arma escriba sus 4 líneas a mano (el
        //  patrón manual vivía en SupernovaProjectile: aquí, en un botón).
        // ==================================================================

        /// <summary>
        /// EL PRESET DE IMPACTO: la sacudida (6·escala px, 0.30 s), el
        /// flash (blanco cálido 0.18 s), la viñeta (0.6 s) y la onda
        /// expansiva (radio 130·escala px, 0.35 s) — los cuatro gestos
        /// calibrados de la casa para UN impacto.
        /// escala 1 = el impacto estándar; 0.5 = un toque; 2 = el golpe
        /// de un jefe (la sacudida escala LINEAL, el flash/viñeta no
        /// saturan: son gestos, no candados).
        /// </summary>
        /// <param name="centroMundo">Centro del impacto (coords de mundo).</param>
        /// <param name="escala">La fuerza del golpe (0.25..3 recomendado).</param>
        /// <param name="color">El color del flash y la onda (null = blanco cálido).</param>
        public static void PresetImpacto(Vector2 centroMundo, float escala = 1f,
            Color? color = null)
        {
            if (Main.netMode == NetmodeID.Server) return;
            escala = MathHelper.Clamp(escala, 0.1f, 4f);

            Color c = color ?? new Color(255, 236, 200);

            Sacudir(6f * escala, 0.30f);
            Flash(c, 0.18f, MathHelper.Clamp(0.45f * escala, 0.2f, 0.8f));
            Vineta(MathHelper.Clamp(0.35f * escala, 0.15f, 0.6f), 0.6f);
            OndaExpansiva(centroMundo, 130f * escala, 0.35f, c,
                MathHelper.Clamp(40f * escala, 8f, 160f));
        }

        /// <summary>
        /// EL PRESET DE GOLPE SECO (sin onda — para impactos en cadena o
        /// espacios cerrados): sacudida corta + flash corto. El latido,
        /// no la explosión.
        /// </summary>
        /// <param name="escala">La fuerza del golpe (0.25..3).</param>
        /// <param name="color">El color del flash (null = blanco cálido).</param>
        public static void PresetGolpeSeco(float escala = 1f, Color? color = null)
        {
            if (Main.netMode == NetmodeID.Server) return;
            escala = MathHelper.Clamp(escala, 0.1f, 4f);
            Color c = color ?? new Color(255, 236, 200);
            Sacudir(3.5f * escala, 0.16f);
            Flash(c, 0.10f, MathHelper.Clamp(0.3f * escala, 0.15f, 0.6f));
        }

        // ==================================================================
        //  RESET (recargas limpias y cambios de mundo)
        // ==================================================================

        /// <summary>
        /// LIMPIA TODO el estado de pantalla (lo llama
        /// <see cref="PantallaSistema.OnWorldUnload"/> y el Unload del
        /// sistema). Los proxies de la sacudida se marcan terminados: el
        /// ClearFinishedModifiers del motor los expulsa en el próximo
        /// ApplyTo (el binario real lo corre cada frame de cámara) — las
        /// instancias viejas NUNCA quedan colgadas en la pila del motor,
        /// ni siquiera tras una recarga caliente del mod.
        /// </summary>
        public static void Reset()
        {
            for (int i = 0; i < _sacudidas.Length; i++)
            {
                _sacudidas[i] = default;
                _proxies[i].Finalizar();
            }
            for (int i = 0; i < _flashes.Length; i++)
                _flashes[i] = default;
            _vineta = default;
            for (int i = 0; i < _ondas.Length; i++)
                _ondas[i] = default;
        }

        // ==================================================================
        //  EL DIBUJADO (lo orquesta PantallaSistema — mismo archivo)
        // ==================================================================

        /// <summary>
        /// ONDAS — el volcado aditivo propio: coords de pantalla puras
        /// (Matrix.Identity, SIN la matriz del mundo — el contrato de la
        /// capa de interfaz). try/finally con End defensivo antes del Begin
        /// (el lote de la interfaz de tML queda cerrado; la reapertura
        /// estándar al final del sistema devuelve todo).
        /// </summary>
        internal static void DibujarOndas()
        {
            // LA PUERTA DE CALIDAD de la casa: la onda es brillo de
            // pantalla apilado — familia Bloom. (La puerta cachea su
            // chequeo por frame: coste cero en el caso común.)
            if (!VFXCore.CalidadPermitida(VFXCore.CalidadFX.Bloom)) return;

            // Auto-entierro de las caducadas + ¿queda algo vivo?
            bool hay = false;
            for (int i = 0; i < _ondas.Length; i++)
            {
                if (!_ondas[i].Activo) continue;
                if ((Main.GameUpdateCount - _ondas[i].FrameInicio) / (60.0 * _ondas[i].DuracionSeg) >= 1.0)
                    _ondas[i].Activo = false;
                else hay = true;
            }
            if (!hay) return;

            Texture2D ring = VFXCore.Ring;
            if (ring == null || ring.IsDisposed) return;

            Vector2 pantalla = Main.screenPosition;
            Vector2 origen = ring.Size() * 0.5f;
            Vector2 invTex = new Vector2(1f / ring.Width, 1f / ring.Height);

            // End defensivo: cierra el lote de UI abierto para abrir el
            // propio. SOLO en frames CON ondas (el frame vacío de la casa
            // no toca el spriteBatch); y si no había lote, el catch lo
            // traga — la bandera queda a false y nadie reabre de más.
            // v6.50.11 — SONDA: el End solo si hay un Begin vivo (cero
            // first-chance; el rastreo de la bandera es EXACTO).
            _loteDeUiCerrado = VFXCore.LoteAbierto;
            if (_loteDeUiCerrado) Main.spriteBatch.End();

            try
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Matrix.Identity);

                for (int i = 0; i < _ondas.Length; i++)
                {
                    ref OndaSlot o = ref _ondas[i];
                    if (!o.Activo) continue;

                    double t = (Main.GameUpdateCount - o.FrameInicio) / 60.0;
                    float prog = (float)(t / o.DuracionSeg);

                    // EXPANSIÓN ease-out CÚBICA: 0→radioMax disparada, frenando al final.
                    float eased = 1f - (1f - prog) * (1f - prog) * (1f - prog);
                    float radio = o.RadioMax * eased;
                    if (radio < 1f) continue;

                    // GROSOR que DECRECE: plumones concéntricos espaciados
                    // un trazo (el trazo de la textura Ring, medido en su
                    // png: ~4% del radio a media alfa). El número de
                    // plumones CAE con el grosor → el anillo se afina de verdad.
                    float grosorActual = o.Grosor * (1f - prog);
                    float paso = Math.Max(4f, radio * 0.04f);
                    int plumones = (int)MathHelper.Clamp(grosorActual / paso, 1f, 8f);

                    // La ENERGÍA se reparte (2/n por plumón): la banda suma
                    // ~2× el color en los solapes — brillo de choque SIN
                    // clipear a blanco plano cuando la onda es gorda.
                    float alfa = (1f - prog) * (2f / plumones);
                    if (alfa <= 0.01f) continue;

                    // El alfa manda y el RGB queda ÍNTEGRO: en aditivo el
                    // aporte es srcRGB·srcA → color·alfa EXACTO (el píxel
                    // de la casa: RGB·alfa en el color haría alfa²).
                    Color c = o.Color;
                    c.A = (byte)(255f * MathHelper.Clamp(alfa, 0f, 1f));

                    Vector2 centro = o.CentroMundo - pantalla;
                    for (int k = 0; k < plumones; k++)
                    {
                        float radioK = radio + (k - (plumones - 1) * 0.5f) * paso;
                        if (radioK < 1f) radioK = 1f;
                        Vector2 tam = VFXCore.RingQuadSize(radioK);   // la calibración de la casa
                        Main.spriteBatch.Draw(ring, centro, null, c, 0f, origen,
                            tam * invTex, SpriteEffects.None, 0f);
                    }
                }
            }
            finally
            {
                // El End de rescate de la casa (v6.41): un lote abierto
                // corrompe el render PARA SIEMPRE — v6.50.11: por sonda
                // (nunca puede tirar NI disparar first-chance).
                VFXCore.CerrarLoteSiAbierto();
            }
        }

        /// <summary>
        /// FLASH + VIÑETA — un SOLO lote alfa propio (los dos son velos de
        /// pantalla que componen igual en alfa): PRIMERO los flashes
        /// (debajo), DESPUÉS la viñeta (encima: el túnel se cierra sobre
        /// el destello — el orden cinematográfico del impacto). El flash
        /// no pasa por la puerta de calidad (un quad por efecto, coste
        /// marginal); la viñeta sí (familia PostProceso: oscurecer la
        /// pantalla ES post-proceso).
        /// </summary>
        internal static void DibujarFlashYVineta()
        {
            // Auto-entierro de flashes caducados + ¿queda alguno?
            bool hayFlash = false;
            for (int i = 0; i < _flashes.Length; i++)
            {
                if (!_flashes[i].Activo) continue;
                if ((Main.GameUpdateCount - _flashes[i].FrameInicio) / (60.0 * _flashes[i].DuracionSeg) >= 1.0)
                    _flashes[i].Activo = false;
                else hayFlash = true;
            }

            // La viñeta tras la puerta de calidad de la casa.
            bool hayVineta = false;
            if (_vineta.Activo)
            {
                if ((Main.GameUpdateCount - _vineta.FrameInicio) / (60.0 * _vineta.DuracionSeg) >= 1.0)
                    _vineta.Activo = false;
                else hayVineta = VFXCore.CalidadPermitida(VFXCore.CalidadFX.PostProceso);
            }
            if (!hayFlash && !hayVineta) return;

            Texture2D pixel = TextureAssets.MagicPixel?.Value;
            if (pixel == null) return;

            Texture2D glow = hayVineta ? VFXCore.SoftGlow : null;
            if (hayVineta && (glow == null || glow.IsDisposed)) hayVineta = false;
            if (!hayFlash && !hayVineta) return;

            // End defensivo (mismo contrato que las ondas: la bandera
            // ampara la reapertura de PantallaSistema).
            // v6.50.11 — SONDA: el End solo si hay un Begin vivo (cero
            // first-chance; el rastreo de la bandera es EXACTO).
            _loteDeUiCerrado = VFXCore.LoteAbierto;
            if (_loteDeUiCerrado) Main.spriteBatch.End();

            try
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Matrix.Identity);

                // === 1. LOS FLASHES (debajo de la viñeta) ===
                for (int i = 0; i < _flashes.Length; i++)
                {
                    ref FlashSlot f = ref _flashes[i];
                    if (!f.Activo) continue;

                    float prog = (float)((Main.GameUpdateCount - f.FrameInicio) / (60.0 * f.DuracionSeg));

                    // ALFA = intensidad · (1−p)² (el contrato del doc: ease-out
                    // cuadrático de DECAIMIENTO). v6.50.3 — FIX (easing
                    // INVERTIDO): se usaba 1−(1−p)² — la curva ease-out de
                    // CRECIMIENTO — como multiplicador directo: el flash nacía
                    // INVISIBLE, subía hasta la intensidad y moría de un pop.
                    // (La gemela OndaSystem.cs ya decaía bien: a = f·vida².)
                    float alfa = MathHelper.Clamp(f.Intensidad * (1f - prog) * (1f - prog), 0f, 1f);
                    if (alfa <= 0.004f) continue;

                    // RGB íntegro + alfa en el canal alfa: la mezcla clásica
                    // hacia el color del flash (el píxel blanco del juego hace
                    // el resto — un quad, cero texturas propias).
                    Color c = f.Color;
                    c.A = (byte)(255f * alfa);
                    Main.spriteBatch.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), c);
                }

                // === 2. LA VIÑETA (encima: el túnel se cierra) ===
                if (hayVineta)
                {
                    double t = (Main.GameUpdateCount - _vineta.FrameInicio) / 60.0;
                    double dur = _vineta.DuracionSeg;

                    // ENVOLVENTE: fade-in FIJO de 0.15 s (la pupila
                    // reacciona) + hold + fade-out en el 30% final.
                    float fadeIn = (float)Math.Min(1.0, t / 0.15);
                    float tramoFinal = (float)(dur * 0.3);
                    float fadeOut = (float)Math.Max(0.0, Math.Min(1.0, (dur - t) / tramoFinal));
                    float env = MathHelper.Clamp(fadeIn * fadeOut, 0f, 1f);
                    if (env > 0.003f)
                    {
                        // El reparto de los 10 plumones: 0.32 por quad suma
                        // ~1 de oscuridad en las esquinas SIN clipear.
                        float peso = env * 0.32f * _vineta.Intensidad;

                        float ancho = Main.screenWidth;
                        float alto = Main.screenHeight;
                        Vector2 centro = new Vector2(ancho * 0.5f, alto * 0.5f);
                        float diag = (float)Math.Sqrt(ancho * ancho + alto * alto);

                        // ESCALA ≈ diagonal/tamañoTextura (el contrato): cada
                        // quad cubre la diagonal — el plumón GIGANTE.
                        Vector2 escala = new Vector2(diag / glow.Width, diag / glow.Height);
                        Vector2 origenGlow = glow.Size() * 0.5f;

                        // El anillo: núcleos asomando tras las ESQUINAS (a
                        // media diagonal del centro) con una deriva lenta
                        // DETERMINISTA (0.06 rad/s — la viñeta respira sin
                        // Main.rand).
                        float radioAnillo = diag * 0.5f;
                        float deriva = (float)t * 0.06f;
                        Color negro = new Color(0, 0, 0, (byte)(255f * MathHelper.Clamp(peso, 0f, 1f)));

                        for (int i = 0; i < 10; i++)
                        {
                            float ang = deriva + i * (MathF.PI * 2f / 10f);
                            Vector2 pos = centro + new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) * radioAnillo;
                            // NEGRO con alfa en AlphaBlend: dst·(1−a·caída) —
                            // la caída radial del SoftGlow ES la máscara: la
                            // viñeta suave sin shaders y sin render targets.
                            Main.spriteBatch.Draw(glow, pos, null, negro, 0f, origenGlow,
                                escala, SpriteEffects.None, 0f);
                        }
                    }
                }
            }
            finally
            {
                // v6.50.11 — sonda: el End de rescate sin first-chance.
                VFXCore.CerrarLoteSiAbierto();
            }
        }
    }

    // ======================================================================
    //  LA SACUDIDA — UN MODIFICADOR DE CÁMARA REAL DEL MOTOR
    // ======================================================================

    /// <summary>
    /// SacudidaTrauma — la sacudida de <see cref="Pantalla"/> vestida de
    /// modificador de cámara del MOTOR
    /// (Terraria.Graphics.CameraModifiers.ICameraModifier): el motor la
    /// consulta en cada DoDraw_UpdateCameraPosition (ApplyTo), y ella
    /// aplica el offset del modelo trauma sobre
    /// <see cref="CameraInfo.CameraPosition"/>.
    ///
    /// APLICACIÓN (medida en el IL del binario real 2026.07.3.0): el
    /// PunchCameraModifier del propio juego hace
    /// CameraPosition += dirección·cos·fuerza·decaimiento (op_Addition y
    /// stobj, literal) — esta clase calca esa semántica EXACTA para que
    /// sustituir un punch por un trauma no cambie el SIGNO del gesto; y
    /// como el offset es un seno simétrico, la dirección es cosmética:
    /// restar hubiera sido sumar con la fase girada.
    ///
    /// VIDA: cada instancia vive atada a UN slot de la tabla
    /// <see cref="Pantalla._sacudidas"/> (nace una vez, se reutiliza
    /// siempre). Se declara <see cref="Finished"/> cuando su slot muere; el
    /// ClearFinishedModifiers del motor la expulsa de la pila en el ApplyTo
    /// siguiente — y <see cref="Pantalla.Sacudir"/> la reengancha al
    /// revivir el slot. Cero asignaciones por sacudida.
    /// </summary>
    public class SacudidaTrauma : ICameraModifier
    {
        private readonly int _indice;
        private readonly string _identidad;

        /// <summary>
        /// ¿Está ya añadida a Main.instance.CameraModifiers? (El Add del
        /// motor además DEDUPLICa por identidad — medido en el binario —
        /// pero esta bandera evita el round-trip entero.)
        /// </summary>
        internal bool EnPila;

        /// <param name="indice">El slot de la tabla de trauma que esta instancia anima.</param>
        public SacudidaTrauma(int indice)
        {
            _indice = indice;
            // La identidad se teje UNA vez en el constructor: el getter
            // JAMÁS concatena (un get_UniqueIdentity que alocara se
            // ejecutaría en cada consulta del motor).
            _identidad = "AethonPantallaSacudida" + indice;
        }

        /// <summary>Identidad estable ante el motor (deduplicación de su pila).</summary>
        public string UniqueIdentity => _identidad;

        /// <summary>
        /// ¿Terminó? El motor la expulsa de la pila en cuanto esto es true
        /// (ClearFinishedModifiers, corrido en cada ApplyTo del binario real).
        /// </summary>
        public bool Finished { get; internal set; }

        /// <summary>Resucita para un temblor nuevo (lo llama Pantalla.Sacudir).</summary>
        internal void Reiniciar() => Finished = false;

        /// <summary>
        /// Lo marca muerto: el motor lo expulsa solo. (Se usa desde el
        /// propio Update y desde <see cref="Pantalla.Reset"/>.)
        /// </summary>
        internal void Finalizar()
        {
            Finished = true;
            EnPila = false;
        }

        /// <summary>
        /// EL LATIDO DEL MOTOR: aplica el offset del modelo trauma. El
        /// motor llama esto cada frame de cámara con la posición por
        /// referencia — aquí (y SOLO aquí) se lee el reloj del juego y se
        /// deriva todo: sin edad, sin timer, sin Main.rand.
        /// </summary>
        /// <param name="info">La información de cámara del frame (mutar CameraPosition mueve el mundo).</param>
        public void Update(ref CameraInfo info)
        {
            ref Pantalla.SacudidaSlot s = ref Pantalla._sacudidas[_indice];

            // Slot vacío (Reset, u otro dueño): morir sin ruido.
            if (!s.Activo) { Finalizar(); return; }

            double t = (Main.GameUpdateCount - s.FrameInicio) / 60.0;   // segundos vividos
            double prog = t / s.DuracionSeg;

            // La ventana se agotó: trauma a cero → la sacudida MUERE (y el
            // motor la expulsa en el próximo ApplyTo).
            if (prog >= 1.0)
            {
                s.Activo = false;
                Finalizar();
                return;
            }

            // === EL MODELO TRAUMA ===
            // Trauma decae LINEAL a 0 (el contrato) y la sacudida es trauma²
            // — la curva canónica: arranque suave, golpe pleno en el pico.
            double trauma = s.TraumaInicial * (1.0 - prog);
            float shake = (float)(trauma * trauma);
            float amp = shake * s.Intensidad;
            if (amp < 0.01f) return;

            // === EL OFFSET DETERMINISTA ===
            // Dos senos INCONMENSURABLES (17.13 y 29.7 rad/s: su razón no
            // es racional, el patrón NUNCA se repite): la MISMA sacudida
            // en todas las máquinas del mundo sin sincronizar nada. Se
            // SUMA a CameraPosition — la semántica EXACTA del
            // PunchCameraModifier del motor (op_Addition sobre el campo,
            // medido en su IL): paridad de gesto con lo que sustituye.
            info.CameraPosition += new Vector2(
                (float)Math.Sin(t * 17.13) * amp,
                (float)Math.Sin(t * 29.7) * amp);
        }
    }

    // ======================================================================
    //  EL SISTEMA — EL PUNTO ÚNICO DE DIBUJADO DEL FRAME
    // ======================================================================

    /// <summary>
    /// PantallaSistema — el aplicador: TODO el dibujado de la librería
    /// vive en UN punto del frame (PostDrawInterface — sobre la interfaz,
    /// BAJO el cursor) con las guardas de la casa (menú y servidor fuera:
    /// el menú no tiene mundo que golpear y el servidor no tiene
    /// pantalla).
    ///
    /// ORDEN del volcado (de atrás adelante): ONDAS (aditivo, el frente
    /// de choque) → FLASHES (alfa, el destello) → VIÑETA (alfa, el túnel
    /// se cierra ENCIMA de todo: la oscuridad es el último plano).
    ///
    /// Al final, SOLO si algún dibujado cerró el lote de la interfaz
    /// (<see cref="Pantalla._loteDeUiCerrado"/>), la REAPERTURA del lote
    /// DE INTERFAZ — el que vanilla tenía abierto aquí. PostDrawInterface
    /// corre dentro de DrawInterface_33_MouseText (capa "Vanilla: Mouse
    /// Text", InterfaceScaleType.UI): TODO el pipeline de interfaz corre
    /// con PlayerInput.SetZoom_UI() y <see cref="Main.UIScaleMatrix"/> —
    /// v6.50.2 — FIX: el restore viejo reabría con la MATRIZ DEL MUNDO
    /// (GameViewMatrix) y los tooltips/textos de vanilla salían DESPLAZADOS
    /// tras cualquier onda/flash con zoom ≠ 100% o UI scale > 100%. Ahora:
    /// Deferred, AlphaBlend, LinearClamp, sin depth, CullCounterClockwise
    /// (los defaults del lote de UI), Main.UIScaleMatrix. Está en
    /// finally: aunque un efecto tire (dispositivo perdido, textura
    /// descargada en caliente), el juego sigue dibujando normal — y el
    /// frame SIN efectos no toca el spriteBatch ni una vez (la lección
    /// v6.90: un Begin sobre un lote ya abierto dispara una first-chance
    /// silenciosa por frame).
    /// </summary>
    public class PantallaSistema : ModSystem
    {
        /// <summary>
        /// Cambio de mundo: la pantalla del mundo viejo muere con él (los
        /// efectos son de UNA explosión de UN mundo — cruzar la puerta no
        /// puede arrastrar un túnel a cuestas).
        /// </summary>
        public override void OnWorldUnload() => Pantalla.Reset();

        /// <summary>Recarga del mod: mismo funeral, otra razón.</summary>
        public override void Unload() => Pantalla.Reset();

        /// <summary>EL PUNTO ÚNICO DE DIBUJADO (sobre la interfaz, bajo el cursor).</summary>
        public override void PostDrawInterface(SpriteBatch spriteBatch)
        {
            // Las guardas de la casa: el menú no tiene mundo y el servidor
            // no tiene pantalla.
            if (Main.gameMenu || Main.netMode == NetmodeID.Server) return;

            // La bandera del frame: en cero — SOLO si un dibujado cierra el
            // lote de UI habrá reapertura (el frame sin efectos no toca el
            // spriteBatch NI UNA vez: cero churn, cero first-chance).
            Pantalla._loteDeUiCerrado = false;

            try
            {
                Pantalla.DibujarOndas();          // 1. el frente de choque (aditivo)
                Pantalla.DibujarFlashYVineta();   // 2. el destello y el túnel (alfa)
            }
            finally
            {
                // LA REAPERTURA DEL LOTE DE INTERFAZ — v6.50.2 — FIX: el
                // restore reabría con la MATRIZ DEL MUNDO, pero este punto
                // del frame dibuja la INTERFAZ (DrawInterface corre tras
                // PlayerInput.SetZoom_UI() con el lote de UIScaleMatrix):
                // los tooltips de vanilla quedaban desplazados tras
                // cualquier onda/flash. Solo cambia la matriz (y el
                // rasterizer que la casa de UI usa).
                //
                // v6.50.11 — SONDA + CURACIÓN (la lección del client.log):
                // la reapertura ya no depende de la bandera del frame sino
                // del ESTADO REAL del lote — si llega cerrado (mod ajeno,
                // error propio) se CURA con el lote de interfaz correcto;
                // si hay un Begin vivo no se pisa (el frame vacío sigue
                // sin tocar el spriteBatch NI UNA vez).
                if (!VFXCore.LoteAbierto)
                {
                    try
                    {
                        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                            SamplerState.LinearClamp, DepthStencilState.None,
                            RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);
                    }
                    catch { }
                }
            }
        }
    }
}
