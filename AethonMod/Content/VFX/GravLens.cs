using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// GravLens — v6.34 — LA DISTORSIÓN UNIFICADA DE LA CASA.
    ///
    /// LA LUZ SE DOBLA — cualquier cosa que curve el espacio pide su
    /// lente aquí. Hasta v6.33 la ÚNICA puerta al shader de distorsión era
    /// el bucle de proyectiles de BlackHoleLensSystem (agujeros negros,
    /// ondas cromáticas, soles en gigante roja, medusas, cometas y
    /// púlsares): un efecto que NO viviera en uno de esos proyectiles no
    /// podía curvar el fondo. GravLens abre esa puerta al resto del mod:
    ///
    /// <code>
    ///   GravLens.Registrar(centro, radio, fuerza, vida);
    /// </code>
    ///
    /// y el frame siguiente el fondo se dobla alrededor de esa masa —
    /// mismo hook de render, mismo shader, CERO duplicación.
    ///
    /// DECISIÓN DE INTEGRACIÓN (v6.34, tomada leyendo el código real de
    /// BlackHoleLensSystem): los agujeros negros NO se recogen vía
    /// Registrar(...). Su recolección está trenzada con los índices de
    /// dibujado "encima de la lente" (_blackHoleIndices), con los
    /// LensRadiusMult por tipo y con la separación de pases A/B —
    /// reestructurarla para homogeneizarla era un diff grande con riesgo
    /// real de romper la regla de oro ("los agujeros negros DEBEN seguir
    /// distorsionando exactamente igual"). La fusión se hace en el punto
    /// de RENDER: PoblarParaRender(...) vierte las lentes registradas en
    /// LAS MISMAS arrays del pase A del sistema — el shader consume UNA
    /// lista unificada, ni el hook ni el shader se duplican, y el camino
    /// de los agujeros queda INTACTO (pixel-perfect con v6.33).
    ///
    /// SEMÁNTICA (heredada del sistema, no inventada):
    ///   · Centro/Radio: en PÍXELS DE MUNDO (el mismo lenguaje del
    ///     p.width·scale del agujero). La conversión a UV la hace
    ///     PoblarParaRender contra la resolución real del frame.
    ///   · Fuerza: 1 = distorsión de agujero negro estándar. Una onda de
    ///     choque pide 0.2-0.4 con vida corta (0.2-0.5 s); un portal
    ///     estable, 0.25-0.5 re-registrándose cada tick. OJO: el shader
    ///     toma la fuerza GLOBAL = máximo del pase (así conviven hoy las
    ///     ondas cromáticas con los agujeros): una lente de 0.3 junto a
    ///     un agujero de 1.0 no se nota hasta que el agujero se marcha.
    ///   · Vida: SEGUNDOS (decae a 60 Hz enganchada a Main.GameUpdateCount
    ///     — con el juego en pausa las lentes TAMBIÉN se congelan, como
    ///     todo lo demás). Nacimiento rápido (15%) y muerte lenta (último
    ///     30%): la casa jamás hace pops de distorsión.
    ///   · Cap: 8 lentes simultáneas; con la mesa llena se expulsa a la
    ///     MÁS VIEJA (orden de registro, no vida restante — una lente
    ///     joven de vida larga no expulsa a una anciana de vida corta).
    ///
    /// La lente curva el FONDO; el efecto que la pide se dibuja por su
    /// camino normal (queda dentro de la escena curvada — quien deba
    /// quedar INTACTO encima ya tiene el protocolo AboveLens del sistema).
    ///
    /// SIN red y SIN estado entre sesiones: el registro es cosmético de
    /// cliente (no-op en servidor) y todo se decide por frame — un efecto
    /// que se calla deja de curvar el fondo al instante.
    /// </summary>
    public static class GravLens
    {
        /// <summary>Capacidad de la mesa: lentes simultáneas máximo.</summary>
        private const int MaxLentes = 8;

        /// <summary>
        /// Una masa que curva el espacio: Centro y Radio en píxeles de
        /// mundo, Fuerza 0..1 (1 = agujero negro estándar), Vida01 el
        /// reloj 1→0, Orden el ticket de llegada y Decaimiento la vida
        /// que pierde por tick de juego.
        /// </summary>
        public struct Lente
        {
            /// <summary>Centro de la masa en coords de MUNDO.</summary>
            public Vector2 Centro;

            /// <summary>Radio de influencia en px (el "abrazo" de la deformación).</summary>
            public float Radio;

            /// <summary>Fuerza 0..1 — 1 = distorsión de agujero negro estándar.</summary>
            public float Fuerza;

            /// <summary>Reloj de vida: 1 recién registrada → 0 expulsada.</summary>
            public float Vida01;

            /// <summary>Ticket de llegada (el más bajo = el más viejo: él expulsa).</summary>
            public long Orden;

            /// <summary>Vida que pierde por tick de juego (1 / (vida·60)).</summary>
            public float Decaimiento;
        }

        /// <summary>La mesa: 8 lentes fijas, los muertos esperan ser pisados.</summary>
        private static readonly Lente[] _lentes = new Lente[MaxLentes];

        /// <summary>El siguiente ticket de llegada (monótono, jamás se reinicia).</summary>
        private static long _ordenSiguiente;

        /// <summary>Frame del último decay (dedup 60 Hz, patrón VFXCore.Presupuesto).</summary>
        private static uint _frameDecay;

        // ==================================================================
        //  LA PUERTA PÚBLICA — cualquier efecto que curve el espacio
        // ==================================================================

        /// <summary>
        /// REGISTRA una lente gravitacional: el fondo se curvará alrededor
        /// de (centro, radio) mientras la lente viva.
        /// </summary>
        /// <param name="centro">Centro de la masa en coords de MUNDO.</param>
        /// <param name="radio">Radio de influencia en px de mundo (el frente
        /// de una onda, el cuerpo de un portal...).</param>
        /// <param name="fuerza">0..1 — 1 = distorsión de agujero negro
        /// estándar; ondas de choque: 0.2-0.4.</param>
        /// <param name="vida">Segundos de vida (se re-registra cada tick
        /// para un efecto persistente, como un portal).</param>
        public static void Registrar(Vector2 centro, float radio, float fuerza, float vida)
        {
            // Cosmético de cliente: el servidor jamás curva nada (MP-safe).
            if (Main.netMode == NetmodeID.Server) return;

            // v6.27 — LA LECCIÓN NaN DE LA CASA (PyraPalettes): el clamp de
            // MathHelper NO corta NaN y un NaN aquí envenenaría la fuerza
            // GLOBAL del pase (todo el shader). Se rechaza la petición.
            if (!float.IsFinite(centro.X) || !float.IsFinite(centro.Y) ||
                !float.IsFinite(radio) || !float.IsFinite(fuerza) || !float.IsFinite(vida))
                return;

            // Una lente sin vida = un solo frame (la petición mínima).
            vida = MathF.Max(vida, 1f / 60f);

            // El asiento: el primero MUERTO y, con la mesa llena, el
            // ticket MÁS VIEJO — el orden de llegada manda, no la vida
            // restante (decisión documentada en la cabecera).
            int slot = 0;
            long ordenMasViejo = long.MaxValue;
            for (int i = 0; i < _lentes.Length; i++)
            {
                if (_lentes[i].Vida01 <= 0f)
                {
                    slot = i;
                    break;
                }
                if (_lentes[i].Orden < ordenMasViejo)
                {
                    ordenMasViejo = _lentes[i].Orden;
                    slot = i;
                }
            }

            _lentes[slot] = new Lente
            {
                Centro = centro,
                Radio = MathF.Max(radio, 1f),
                Fuerza = MathHelper.Clamp(fuerza, 0f, 1f),
                Vida01 = 1f,
                Orden = _ordenSiguiente++,
                Decaimiento = 1f / (vida * 60f),
            };
        }

        // ==================================================================
        //  EL RELOJ — el decay de las vidas
        // ==================================================================

        /// <summary>
        /// DECAE las vidas de todas las lentes (1 tick de juego = 1/60 s).
        /// La engancha BlackHoleLensSystem en su hook de render — el
        /// sistema NO tiene punto de update propio, así que su tick ES
        /// este método: idempotente por Main.GameUpdateCount (dispare el
        /// hook una o veinte veces por frame, el decay corre a 60 Hz; con
        /// el juego en pausa se congela, como todo lo demás).
        /// </summary>
        public static void Actualizar()
        {
            if (Main.netMode == NetmodeID.Server) return;

            // Dedup por tick de JUEGO (el mismo patrón de VFXCore.Presupuesto
            // y VFXCore.CalidadPermitida: la casa cuenta frames de update,
            // no de dibujo).
            uint frame = Main.GameUpdateCount;
            if (frame == _frameDecay) return;
            _frameDecay = frame;

            for (int i = 0; i < _lentes.Length; i++)
            {
                if (_lentes[i].Vida01 <= 0f) continue;
                _lentes[i].Vida01 -= _lentes[i].Decaimiento;
                // (los muertos se quedan tumbados: Registrar los pisa)
            }
        }

        // ==================================================================
        //  EL PUNTO DE INTEGRACIÓN — la fusión en el render del sistema
        // ==================================================================

        /// <summary>
        /// VIERTE las lentes vivas en las arrays del pase A de
        /// BlackHoleLensSystem (el shader itera 5 fuentes fijas): escribe
        /// desde el índice <paramref name="desde"/> sin pasarse de
        /// <paramref name="capacidad"/> y devuelve cuántas escribió, para
        /// que el sistema avance su contador. Convierte mundo→UV contra la
        /// resolución real del frame y aplica la ventana de
        /// nacimiento/muerte a la fuerza.
        /// </summary>
        /// <param name="posicionesUV">Array de posiciones UV del shader (salida).</param>
        /// <param name="radiosUV">Array de radios en UV normalizados por X (salida).</param>
        /// <param name="fuerzas">Array de fuerzas 0..1 (salida).</param>
        /// <param name="desde">Primer índice libre (cuántas fuentes ya hay).</param>
        /// <param name="capacidad">Total de slots del pase (MaxSources).</param>
        internal static int PoblarParaRender(Vector2[] posicionesUV, float[] radiosUV,
            float[] fuerzas, int desde, int capacidad)
        {
            if (posicionesUV == null || radiosUV == null || fuerzas == null) return 0;
            if (capacidad > posicionesUV.Length || capacidad > radiosUV.Length ||
                capacidad > fuerzas.Length) return 0;
            if (desde < 0 || desde >= capacidad) return 0;
            if (Main.gameMenu) return 0;

            Vector2 screenSize = new Vector2(Main.screenWidth, Main.screenHeight);
            if (screenSize.X <= 0f || screenSize.Y <= 0f) return 0;

            int escritos = 0;
            for (int i = 0; i < _lentes.Length && escritos < capacidad - desde; i++)
            {
                Lente l = _lentes[i];
                if (l.Vida01 <= 0f) continue;

                // Mundo → UV: el mismo lenguaje del shader (screenPos/pantalla).
                Vector2 uv = (l.Centro - Main.screenPosition) / screenSize;

                // Margen de ONDA (±0.35): una lente GRANDE curva pantalla
                // con su centro fuera de ella — el frente de una onda de
                // choque enorme sigue contando aunque su corazón no se vea.
                if (uv.X < -0.35f || uv.X > 1.35f || uv.Y < -0.35f || uv.Y > 1.35f)
                    continue;

                // VENTANA nacimiento/muerte: entra rápido (primer 15% de
                // vida), sale lento (último 30%) — nunca un pop.
                float edad = 1f - l.Vida01;
                float ventana = Suave(edad / 0.15f) * (1f - Suave((edad - 0.70f) / 0.30f));

                posicionesUV[desde + escritos] = uv;
                radiosUV[desde + escritos] = MathF.Max(l.Radio / screenSize.X, 0.0001f);
                fuerzas[desde + escritos] = MathHelper.Clamp(l.Fuerza * ventana, 0f, 1f);
                escritos++;
            }
            return escritos;
        }

        /// <summary>Smoothstep casero 0..1 (t²·(3−2t) con clamp).</summary>
        private static float Suave(float t)
        {
            t = MathHelper.Clamp(t, 0f, 1f);
            return t * t * (3f - 2f * t);
        }
    }
}
