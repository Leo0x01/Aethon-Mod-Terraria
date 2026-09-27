using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    // ======================================================================
    //  v6.50.18 — RayoLib: LOS RAYOS DE VERDAD (el puerto 1:1 de Terraria)
    // ======================================================================
    //
    //  LA ORDEN DEL USUARIO: "recuerda que no debes usar sprite para los
    //  rayos ya que se ven raro… investiga los rayos originales de terraria
    //  y crea un arma que los use, además investiga el funcionamiento de
    //  Arc Surge, así es como son los rayos de verdad".
    //
    //  LA INVESTIGACIÓN (R58-a, fuentes reales descargadas): el sistema de
    //  rayos de Terraria 1.4.5 es LightningGenerator + StormLightningDrawer
    //  (decompilados de JonataOliveiraa/Terraria1.4.5, branch main) — y
    //  "Arc Surge" NO es de ningún mod: es el ARMA MÁGICA VANILLA 1.4.5
    //  (drop 1/50 del Platillo Marciano) que dispara ARCOS INSTANTÁNEOS
    //  ROJOS mano→cursor con el MISMO sistema. Esta librería ES ese
    //  sistema, portado pieza a pieza:
    //
    //    · EL GENERADOR — raymarch DIRECCIONAL por pasos de 8 px con 4-5
    //      CAPAS DE ÁNGULO apiladas (la capa 0 tiemble fino en cada ~2
    //      pasos; la capa superior es el QUIEBRE raro y gordo que engendra
    //      las horquillas), con TIMÓN hacia el blanco (el término que
    //      dobla el rumbo de vuelta a la cuerda) y DES-RANDOMIZACIÓN al
    //      80% del trayecto (el rayo SE ENDEREZA antes de impactar, como
    //      los de verdad). NO es midpoint displacement: es un canal que
    //      MARCHA con memoria de rumbo — por eso los de vanilla meandran y
    //      los de esquina muerta zigzaguean.
    //    · LAS HORQUILLAS nacen del quiebre superior reflejado (el ángulo
    //      del espejo del kink), largas 0.7-0.8 de lo que resta, con su
    //      rango de progreso SINCRONIZADO al del padre (la horquilla se
    //      enciende con la misma OLA que pasa por su punto de nacimiento).
    //    · LAS ROTACIONES se suavizan a posteriori (promedio de vecinos
    //      ×2 pasadas) — la curvatura continua del canal.
    //    · LA OLA — LA FIRMA de los rayos de vanilla: un FRENTE ENERGIZADO
    //      que nace en el ORIGEN, recorre el canal y se retira dejando el
    //      rayo apagándose desde arriba. WaveTransition con su longitud de
    //      transición (0.5 del canal en clima, 0.3 en arco de arma).
    //    · EL TAPER: 50% del ancho hacia el impacto + 50% en la segunda
    //      mitad de la vida + horquillas muriendo en PUNTA (a 0, el tronco
    //      a 0.5).
    //    · CERO SPRITES DE VERDAD: el pincel es EL PIXEL 1×1 DEL MOTOR
    //      (TextureAssets.MagicPixel — la primitiva del rectángulo) con
    //      LA PILA DE LA CASA (StormLib.PilaW/PilaF, la receta pública del
    //      LightningArc 466 de vanilla: 6 pasadas SÓLIDAS de ancho/brillo
    //      telescópicos cuya SUMA es el degradado transversal + la VENA
    //      blanca) — la misma anatomía de rayo de TODO el mod desde
    //      v6.50.22. El LightningGenerator/Drawer originales dibujan un
    //      VertexStrip con shader propio (PixelShader "StormLightning",
    //      inexistente en tML 2026.07/1.4.4.9 y no compilable aquí): la
    //      geometría, la ola, el taper y las horquillas son el puerto
    //      EXACTO; la sección transversal es la PILA de pasadas sólidas
    //      con el tinte aditivo lineal de la casa.
    //
    //  LOS PRESETS (los tres de vanilla, verbatim):
    //    · RayoParams.Tormenta   — el rayo del clima (cae de 1000 px).
    //    · RayoParams.Arma       — LightningStrikeWeapon (cae de 750 px).
    //    · RayoParams.Arco(a, b) — GetArcSurgeWeaponGenerator (a→b, los
    //      parámetros se remapean por distancia 0..1000 px).
    // ======================================================================

    /// <summary>El rango [min, max] con Lerp/Contains (el FloatRange de vanilla, propio).</summary>
    public struct RangoF
    {
        public float Min;
        public float Max;

        public RangoF(float min, float max) { Min = min; Max = max; }

        public float Lerp(float t) => Min + (Max - Min) * MathHelper.Clamp(t, 0f, 1f);

        public bool Contains(float v) => v >= Min && v <= Max;

        public bool Contiene(float v) => Contains(v);
    }

    /// <summary>
    /// EL LCG32 de vanilla 1.4.5 (Terraria.Utilities.LCG32Random, portado):
    /// state = state·2438992949 + 1 (el −1856014347 con signo). DETERMINISTA
    /// por semilla de 32 bits — la misma semilla engendra el MISMO rayo en
    /// todas las máquinas, y las horquillas heredan state+1.
    /// </summary>
    internal struct Lcg32
    {
        public uint State;

        public Lcg32(uint semilla) { State = semilla; }

        public double Siguiente()
        {
            State = State * 2438992949u + 1u;
            return State / 4294967296.0;
        }

        public float SiguienteF() => (float)Siguiente();
    }

    /// <summary>Los parámetros del generador — el LightningGenerator de vanilla, campo por campo.</summary>
    public class RayoParams
    {
        public bool ColisionaTiles;
        public float LimiteRotacionOrigen = 0.34906587f;   // ±20° del eje al nacer
        public float Longitud = 1000f;                     // de dónde cae (clima)
        public float FuerzaRotacion = 0.9f;                // el quiebre máximo por capa superior
        public int Paso = 8;                               // px por paso del raymarch
        public int Capas = 4;
        public float FactorCapas = 1.5f;                   // cada capa inferior divide su fuerza
        public float FactorDesvio = 5f;                    // el sobre del timón
        public float ReducirAzarTras = 0.8f;               // enderezar tras el 80%
        public float HorquillaUmbral = 0.65f;              // fracción del quiebre máx que horquilla
        public float HorquillaReflejo = 0.4f;              // el ángulo espejo del kink
        public float HorquillaRot = 0.9f;
        public float HorquillaPaso = 0.8f;
        public float HorquillaLargo = 0.8f;                // 0.8 de lo que resta
        public int MaxHorquillas = 2;
        public int MaxProfundidad = 2;
        public RangoF HorquillaRango = new RangoF(0.3f, 0.8f);

        /// <summary>EL RAYO DEL CLIMA (StormLightning de vanilla, verbatim): cae de 1000 px, choca con tiles y líquido.</summary>
        public static RayoParams Tormenta => new RayoParams
        {
            LimiteRotacionOrigen = 0.34906587f,
            Longitud = 1000f,
            FuerzaRotacion = 0.9f,
            Paso = 8,
            Capas = 4,
            FactorCapas = 1.5f,
            FactorDesvio = 5f,
            ReducirAzarTras = 0.8f,
            HorquillaUmbral = 0.65f,
            HorquillaReflejo = 0.4f,
            HorquillaRot = 0.9f,
            HorquillaPaso = 0.8f,
            HorquillaLargo = 0.8f,
            MaxHorquillas = 2,
            MaxProfundidad = 2,
            HorquillaRango = new RangoF(0.3f, 0.8f),
            ColisionaTiles = true,
        };

        /// <summary>EL RAYO DE ARMA (LightningStrikeWeapon de vanilla): cae de 750 px, atraviesa tiles.</summary>
        public static RayoParams Arma => new RayoParams
        {
            LimiteRotacionOrigen = 0.08726647f,
            Longitud = 750f,
            FuerzaRotacion = 0.9f,
            Paso = 8,
            Capas = 4,
            FactorCapas = 1.5f,
            FactorDesvio = 5f,
            ReducirAzarTras = 0.8f,
            HorquillaUmbral = 0.65f,
            HorquillaReflejo = 0.4f,
            HorquillaRot = 0.9f,
            HorquillaPaso = 0.8f,
            HorquillaLargo = 0.8f,
            MaxHorquillas = 2,
            MaxProfundidad = 2,
            HorquillaRango = new RangoF(0.3f, 0.8f),
            ColisionaTiles = false,
        };

        /// <summary>
        /// EL ARCO DEL ARC SURGE (GetArcSurgeWeaponGenerator de vanilla, los
        /// remapeos por distancia EXACTOS): mano→blanco, 5 capas al 1.2,
        /// paso de 6 px, horquilla al 10-50% del trayecto.
        /// </summary>
        public static RayoParams Arco(Vector2 origen, Vector2 destino)
        {
            float d = Vector2.Distance(origen, destino);
            return new RayoParams
            {
                LimiteRotacionOrigen = 0f,
                Longitud = 1f,
                FuerzaRotacion = 0.7f,
                Paso = 6,
                Capas = 5,
                FactorCapas = 1.2f,
                FactorDesvio = RayoLib.Remap(d, 0f, 1000f, 5f, 1f),
                ReducirAzarTras = 0.7f,
                HorquillaUmbral = RayoLib.Remap(d, 0f, 1000f, 0.3f, 0.5f),
                HorquillaReflejo = RayoLib.Remap(d, 0f, 1000f, 0.6f, 0.2f),
                HorquillaRot = 0.9f,
                HorquillaPaso = 0.8f,
                HorquillaLargo = 0.7f,
                MaxHorquillas = 3,
                MaxProfundidad = 3,
                HorquillaRango = new RangoF(RayoLib.Remap(d, 0f, 1000f, 0.1f, 0.5f), 0.8f),
                ColisionaTiles = true,
            };
        }
    }

    /// <summary>UN rayo del árbol (el Bolt de vanilla): puntos + rotaciones + rango + profundidad.</summary>
    public class RayoBolt
    {
        public Vector2[] Puntos;
        public float[] Rotaciones;
        public RangoF Rango = new RangoF(0f, 1f);
        public int Profundidad;
        public bool ChocoTile;

        /// <summary>¿El tronco? (profundidad 0 — las horquillas son lo demás).</summary>
        public bool EsTronco => Profundidad == 0;
    }

    /// <summary>La curva de animación del canal (el AnimParams de StormLightningDrawer, verbatim).</summary>
    public struct AnimRayo
    {
        public float OpacidadInicial;   // al nacer
        public float FundidoInicio;     // dónde empieza a ganar brillo
        public float FundidoMedio;      // dónde está a tope
        public float FundidoFin;        // dónde muere
        public float OpacidadFinal;     // al final (ArcSurge: 0.3 — nunca invisible)
        public float OlaLongitud;       // el ancho relativo del frente
        public float OlaEntrada;        // cuánto de la vida tarda en energizar
        public float OlaSalida;         // cuánto de la vida tarda en retirarse

        /// <summary>LA ANIMACIÓN DEL CLIMA (DefaultAnim de vanilla).</summary>
        public static AnimRayo Clima => new AnimRayo
        {
            OpacidadInicial = 0.5f,
            FundidoInicio = 0.1f,
            FundidoMedio = 0.25f,
            FundidoFin = 0.75f,
            OpacidadFinal = 0f,
            OlaLongitud = 0.5f,
            OlaEntrada = 0.15f,
            OlaSalida = 0.75f,
        };

        /// <summary>LA ANIMACIÓN DEL ARCO DE ARMA (ArcSurge de vanilla): fondo de 0.3, nunca invisible.</summary>
        public static AnimRayo Arco => new AnimRayo
        {
            OpacidadInicial = 0.5f,
            FundidoInicio = 0.1f,
            FundidoMedio = 0.4f,
            FundidoFin = 1f,
            OpacidadFinal = 0.3f,
            OlaLongitud = 0.3f,
            OlaEntrada = 0.3f,
            OlaSalida = 0.5f,
        };

        /// <summary>
        /// v6.50.24 — LA ANIMACIÓN DE LA CAÍDA VENA TRUENO (el
        /// ThunderFalling de Coralite): el frente ENERGIZA durante media
        /// vida (el canal CRECE del cielo hacia el blanco — la caída
        /// telegrafiada del dragón), ARDE A TOPE hasta el 55% (el
        /// impacto: la caída completa) y muere SOLO en el colapso (el
        /// X2Ease 1−f² del original se queda cerca de 1 casi toda la
        /// agonía — la desintegración con el zigzag ABIERTO debe VERSE
        /// antes de apagarse; la lección del mock v1: fundir desde el
        /// 18% mataba la agonía antes de leerla).
        /// </summary>
        public static AnimRayo VenaTrueno => new AnimRayo
        {
            OpacidadInicial = 0.6f,
            FundidoInicio = 0.04f,
            FundidoMedio = 0.55f,
            FundidoFin = 1f,
            OpacidadFinal = 0f,
            OlaLongitud = 0.4f,
            OlaEntrada = 0.5f,
            OlaSalida = 0.35f,
        };
    }

    /// <summary>
    /// RayoLib — EL GENERADOR + EL RENDER. Todo estático, cero estado entre
    /// frames salvo nada — el pincel es el pixel del motor (compartido),
    /// cero texturas propias, NADA de PNG.
    /// </summary>
    public static class RayoLib
    {
        // ------------------------------------------------------------------
        //  EL REMAP DE VANILLA (Utils.Remap: lineal clampeado)
        // ------------------------------------------------------------------
        public static float Remap(float valor, float a, float b, float c, float d)
        {
            if (b - a < 0.0001f) return valor < a ? c : d;
            float t = MathHelper.Clamp((valor - a) / (b - a), 0f, 1f);
            return c + (d - c) * t;
        }

        // ------------------------------------------------------------------
        //  EL GENERADOR (LightningGenerator.GenerateBolt, portado)
        // ------------------------------------------------------------------

        /// <summary>
        /// GENERA el rayo (tronco + horquillas) hacia el blanco. La dirección
        /// decide de DÓNDE nace: clima = Vector2.UnitY (cae de arriba);
        /// arco = (blanco − origen) con Longitud 1 (nace EN la mano).
        /// </summary>
        public static RayoBolt Generar(RayoParams p, List<RayoBolt> bolts, uint semilla,
            Vector2 blanco, Vector2? direccion = null)
        {
            Vector2 dir = direccion ?? Vector2.UnitY;
            // el ±20° (o ±5° de arma) del eje al nacer — por semilla.
            dir = dir.RotatedBy((new Lcg32(semilla).Siguiente() * 2.0 - 1.0) * p.LimiteRotacionOrigen) * p.Longitud;
            RayoBolt tronco = GenerarBolt(p, bolts, semilla, 0, blanco - dir, blanco,
                p.FuerzaRotacion, p.Paso, new RangoF(0f, 1f));
            if (bolts != null)
            {
                foreach (RayoBolt b in bolts)
                    if (b.Rotaciones == null || b.Rotaciones.Length != (b.Puntos?.Length ?? 0))
                        b.Rotaciones = CalcRotaciones(b.Puntos);
            }
            return tronco;
        }

        /// <summary>El raymarch de un canal (la recursión de las horquillas vive dentro).</summary>
        private static RayoBolt GenerarBolt(RayoParams p, List<RayoBolt> bolts, uint semilla,
            int profundidad, Vector2 inicio, Vector2 blanco,
            float fuerzaRot, float pasoF, RangoF rango)
        {
            var rng = new Lcg32(semilla);
            float rot = 0f;                       // el rumbo acumulado
            var capas = new float[p.Capas];       // el ángulo ACTUAL de cada capa
            var tileBlanco = new Point((int)(blanco.X / 16f), (int)(blanco.Y / 16f));
            Vector2 pos = inicio;
            Vector2 cuerda = blanco - inicio;
            float largoCuerda = cuerda.Length();
            if (largoCuerda < 0.001f) largoCuerda = 0.001f;
            Vector2 eje = cuerda / largoCuerda;
            Vector2 perp = new Vector2(eje.Y, -eje.X);
            int capacidad = (int)Math.Max(largoCuerda * 2f / pasoF, 1f);
            var puntos = new Vector2[capacidad];
            int horquillas = 0;

            var bolt = new RayoBolt
            {
                Puntos = puntos,
                Profundidad = profundidad,
                Rango = rango,
            };

            int i;
            for (i = 0; i < capacidad; i++)
            {
                puntos[i] = pos;
                Vector2 alBlanco = blanco - pos;
                float adelante = Vector2.Dot(alBlanco, eje);
                if (adelante < pasoF) break;   // llegó (o casi): fin del canal

                float progreso = MathHelper.Clamp(1f - adelante / largoCuerda, 0f, 1f);

                // LA COLISIÓN (tiles sólidos o la superficie del líquido — vanilla).
                if (p.ColisionaTiles)
                {
                    var tile = new Point((int)(pos.X / 16f), (int)(pos.Y / 16f));
                    if (tile != tileBlanco && ChocaTile(pos))
                    {
                        bolt.Rango = new RangoF(rango.Min, rango.Lerp(progreso));
                        bolt.ChocoTile = true;
                        break;
                    }
                }

                Vector2 rumbo = alBlanco / alBlanco.Length();

                // EL TIMÓN: cuánto se DESCARRILÓ de la cuerda (en la perp),
                // normalizado por el sobre (más estrecho en los extremos:
                // min(p, 1-p)·FactorDesvio·2) — clavado ±1.
                float timon = MathHelper.Clamp(
                    -Vector2.Dot(rumbo, perp) /
                    Math.Max(0.01f, Math.Min(progreso, 1f - progreso) * p.FactorDesvio * 2f),
                    -1f, 1f);

                // LA CAPA QUE RE-ROULA HOY: P(capa k) = 0.5^(k+1) — la fina
                // cada ~2 pasos, la gorda el 6.25%.
                if (ElegirCapa(rng.Siguiente(), 0.5f, p.Capas, out int capa))
                {
                    float fuerza = fuerzaRot;
                    for (int k = p.Capas - 1; k > capa; k--)
                        fuerza /= p.FactorCapas;

                    float azar = (float)(rng.Siguiente() * 2.0 - 1.0);
                    // la MEZCLA de vanilla: el azar MANDA, el timón corrige
                    // (y se AUTO-LIMITA cuando empuja en contra del azar).
                    float objetivo = (azar + (timon - azar * Math.Abs(timon)) / 2f) * fuerza;
                    float delta = objetivo - capas[capa];
                    rot += delta;
                    capas[capa] = objetivo;

                    // LA HORQUILLA: solo en la capa superior (el quiebre
                    // gordo), reflejada, si el kink cruzó el umbral.
                    if (capa == p.Capas - 1 && bolts != null)
                    {
                        float dado = rng.SiguienteF();
                        float permiso = Remap(horquillas, 0f, p.MaxHorquillas, 1f, 0f);
                        float reflejo = rot - delta * (1f + p.HorquillaReflejo);
                        if (Math.Abs(delta) >= fuerzaRot * p.HorquillaUmbral &&
                            p.HorquillaRango.Contiene(progreso) &&
                            profundidad < p.MaxProfundidad &&
                            dado < permiso &&
                            Math.Abs(reflejo) < 1.3962634f)
                        {
                            horquillas++;
                            float largoH = (1f - progreso) * p.HorquillaLargo;
                            Vector2 blancoH = pos + rumbo.RotatedBy(reflejo) * largoCuerda * largoH;
                            GenerarBolt(p, bolts, rng.State + 1u, profundidad + 1, pos, blancoH,
                                fuerzaRot * p.HorquillaRot, pasoF * p.HorquillaPaso,
                                new RangoF(rango.Lerp(progreso), rango.Lerp(progreso + largoH)));
                        }
                    }
                }

                // LA DES-RANDOMIZACIÓN (el rayo se endereza al final / cuando
                // está muy descarrilado): resetea a 0 la capa superior.
                float chance = Remap(progreso, p.ReducirAzarTras, 1f, 0f, 1f) +
                               Remap(Math.Abs(timon), 0.5f, 1f, 0f, 1f);
                if (ElegirCapa(rng.Siguiente(), chance, p.Capas, out int capaAlta))
                {
                    rot -= capas[p.Capas - 1 - capaAlta];
                    capas[p.Capas - 1 - capaAlta] = 0f;
                }

                pos += rumbo.RotatedBy(rot) * pasoF;
            }

            if (i < capacidad)
            {
                Array.Resize(ref puntos, i + 1);
                bolt.Puntos = puntos;
            }

            if (bolts != null && (bolt.EsTronco || i > 2))
                bolts.Add(bolt);
            return bolt;
        }

        /// <summary>La elección de capa de vanilla: la moneda 0.5 por nivel, en cascada.</summary>
        private static bool ElegirCapa(double r, float chance, int capas, out int capa)
        {
            capa = 0;
            while (capa < capas)
            {
                if (r >= 1.0 - chance) return true;
                r /= chance;
                capa++;
            }
            return false;
        }

        /// <summary>El choque de vanilla: tile sólido/inclinado O la superficie de un líquido.</summary>
        private static bool ChocaTile(Vector2 pos)
        {
            int x = (int)(pos.X / 16f);
            int y = (int)(pos.Y / 16f);
            if (x < 5 || x >= Main.maxTilesX - 5 || y < 5 || y >= Main.maxTilesY - 5)
                return false;
            Tile tile = Main.tile[x, y];
            if (tile == null || !tile.HasTile) return false;
            if (WorldGen.SolidOrSlopedTile(x, y)) return true;
            int liquido = tile.LiquidAmount;
            return liquido > 0 && (int)pos.Y % 16 > 16 * (255 - liquido) / 255;
        }

        // ------------------------------------------------------------------
        //  LAS ROTACIONES (CalcRotations + SmoothRotations de vanilla)
        // ------------------------------------------------------------------

        /// <summary>El suavizado doble de vanilla: promedio de vecinos ×2 pasadas.</summary>
        public static float[] CalcRotaciones(Vector2[] puntos)
        {
            if (puntos == null || puntos.Length < 2) return new float[puntos?.Length ?? 0];
            var rots = new float[puntos.Length];
            rots[0] = (puntos[0] - puntos[1]).ToRotation();
            float prev = rots[0];
            for (int i = 1; i < rots.Length - 1; i++)
            {
                float actual = (puntos[i] - puntos[i + 1]).ToRotation();
                rots[i] = prev + MathHelper.WrapAngle(actual - prev) / 2f;
                prev = actual;
            }
            rots[rots.Length - 1] = prev;
            // la segunda pasada (la curvatura de verdad).
            float antes = rots[0];
            for (int i = 1; i < rots.Length - 1; i++)
            {
                float a = rots[i], b = rots[i + 1];
                rots[i] = a + (MathHelper.WrapAngle(antes - a) + MathHelper.WrapAngle(b - a)) / 2f;
                antes = a;
            }
            return rots;
        }

        // ------------------------------------------------------------------
        //  LA OLA (WaveTransition + StripColors/StripWidth de vanilla)
        // ------------------------------------------------------------------

        /// <summary>La transición que viaja: un smoothstep cuyo borde corre con la ola.</summary>
        private static float OlaTransicion(float p, float w, float longitud, float desde, float hasta)
            => Remap(p, MathHelper.Lerp(-longitud, 1f, w), MathHelper.Lerp(0f, 1f + longitud, w), hasta, desde);

        /// <summary>
        /// EL COLOR de un punto del canal (StripColors de vanilla): el frente
        /// de energizado (cIn) multiplicado por el frente de retirada (cOut)
        /// — el rayo NACE del origen, ARDE entero y se APAGA desde el origen.
        /// </summary>
        public static float OlaColor(float p, float progreso, RangoF rango, AnimRayo anim)
        {
            float pLocal = rango.Lerp(p);
            float wIn = Remap(progreso, 0f, anim.OlaEntrada, 0f, 1f);
            float cIn = OlaTransicion(pLocal, wIn, anim.OlaLongitud, 0f, 1f);
            float wOut = Remap(progreso, 1f - anim.OlaSalida, 1f, 0f, 1f);
            float cOut = OlaTransicion(pLocal, wOut, anim.OlaLongitud, 1f, 0f);
            return MathHelper.Clamp(cIn * cOut, 0f, 1f);
        }

        /// <summary>
        /// EL ANCHO de un punto del canal (StripWidth de vanilla): taper del
        /// 50% hacia el impacto × taper del 50% en la segunda mitad de la
        /// vida × la horquilla muriendo en punta.
        /// </summary>
        public static float OlaAncho(float p, float progreso, RangoF rango, float ancho, bool esTronco)
        {
            float pLocal = rango.Lerp(p);
            float w = ancho * Remap(pLocal, 0.5f, 1f, 1f, 0.5f) *
                      Remap(progreso, 0.5f, 1f, 1f, 0.5f);
            if (rango.Max < 1f)
                w *= Remap(rango.Max - pLocal, 0.1f, 0f, 1f, esTronco ? 0.5f : 0f);
            return w;
        }

        // ------------------------------------------------------------------
        //  EL TINTE ADITIVO (el de la casa: (RGB·√f, A·√f) — lineal en el
        //  lote aditivo, compositing premult correcto en cualquier lote alfa)
        // ------------------------------------------------------------------
        internal static Color Tinte(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            float s = (float)Math.Sqrt(f);
            return new Color(
                (byte)(int)(c.R * s), (byte)(int)(c.G * s), (byte)(int)(c.B * s),
                (byte)(int)(255f * s));
        }

        // ------------------------------------------------------------------
        //  EL PINCEL — 100% CÓDIGO DE VERDAD: EL PIXEL 1×1 DEL MOTOR
        // ------------------------------------------------------------------
        //
        //  v6.50.22 de la casa ya había desterrado las bandas PNG del
        //  StormLib con LA PILA (la receta del LightningArc 466 de vanilla:
        //  pasadas SÓLIDAS apiladas cuya SUMA es el degradado transversal).
        //  RayoLib usa EL MISMO pincel — el pixel del motor (VFXCore.Pixel)
        //  con la MISMA receta pública (StormLib.PilaW/PilaF): cero textura,
        //  cero sprite, NI SIQUIERA horneada por código — el rayo de vanilla
        //  ES geometría pura y así se pinta aquí. La VENA blanca (núcleo)
        //  con su suelo de 1.5 px arde SIEMPRE.
        // ------------------------------------------------------------------

        /// <summary>El pincel del rayo: el pixel 1×1 del motor (nulo si la casa aún no lo tiene).</summary>
        private static Texture2D Pixel => VFXCore.Pixel;

        // ------------------------------------------------------------------
        //  EL RENDER — el canal como cinta de quads con la OLA de vanilla
        // ------------------------------------------------------------------

        /// <summary>
        /// DIBUJA un rayo del árbol (batch ABIERTO aditivo). LA PILA DE LA
        /// CASA (la receta pública StormLib.PilaW/PilaF — pasadas SÓLIDAS del
        /// pixel del motor cuya SUMA es el degradado transversal, la receta
        /// del LightningArc 466 de vanilla) pero con el ANCHO y el COLOR
        /// gobernados por la OLA y el TAPER de vanilla — el frente que
        /// energiza, la retirada que apaga, la horquilla que muere en punta.
        /// SIN textura NI horneada: el pincel es el pixel 1×1 del motor.
        /// </summary>
        public static void Dibujar(SpriteBatch batch, RayoBolt bolt, Color color, Color nucleo,
            float ancho, float progreso, float intensidad, AnimRayo anim)
        {
            try
            {
                if (bolt?.Puntos == null || bolt.Puntos.Length < 2) return;

                // EL ALFA GLOBAL del canal (el uOpacity del drawer de
                // vanilla: gana de OpacidadInicial a 1 en [FundidoInicio,
                // FundidoMedio], cae a OpacidadFinal en [FundidoMedio,
                // FundidoFin]).
                float alfaGlobal = Remap(progreso, anim.FundidoInicio, anim.FundidoMedio,
                        anim.OpacidadInicial, 1f) *
                    Remap(progreso, anim.FundidoMedio, anim.FundidoFin, 1f, anim.OpacidadFinal);
                // las horquillas decaen por profundidad (0.8^k): vanilla usa
                // 0.5·0.8^(k−1) de saturación, pero el mock+VLM de la casa
                // midió que a 0.5 la horquilla NO SE LEE (≈+40 RGB sobre el
                // fondo en el lienzo de verificación — "no distinct forks",
                // el propio VLM): 0.72 conserva el decaimiento geométrico
                // y hace la silueta clásica VISIBLE (calibración documentada).
                if (!bolt.EsTronco)
                    alfaGlobal *= 0.72f * (float)Math.Pow(0.8, Math.Max(0, bolt.Profundidad - 1)) *
                                  MathHelper.Clamp(intensidad, 0f, 1.5f);
                if (alfaGlobal <= 0.01f) return;

                Texture2D pixel = Pixel;
                if (pixel == null) return;

                int n = bolt.Puntos.Length;
                float[] rots = bolt.Rotaciones;
                if (rots == null || rots.Length != n)
                { rots = CalcRotaciones(bolt.Puntos); bolt.Rotaciones = rots; }

                Vector2 pantalla = Main.screenPosition;

                for (int i = 0; i < n - 1; i++)
                {
                    Vector2 a = bolt.Puntos[i] - pantalla;
                    Vector2 b = bolt.Puntos[i + 1] - pantalla;
                    float t = i / (float)(n - 1);

                    // LA OLA en este tramo (el color del vértice de vanilla).
                    float ola = OlaColor(t, progreso, bolt.Rango, anim);
                    if (ola <= 0.004f) continue;

                    // EL ANCHO de este tramo (el half-width de vanilla).
                    float w = OlaAncho(t, progreso, bolt.Rango, ancho, bolt.EsTronco);
                    if (w <= 0.05f) continue;

                    // la rotación del tramo y la junta de la casa.
                    float rot = rots[i];
                    Vector2 seg = b - a;
                    float len = seg.Length();
                    if (len < 0.35f) continue;
                    float largo = Math.Abs(Vector2.Dot(seg, rot.ToRotationVector2()));
                    if (largo < 0.35f) continue;
                    float dPrev = i > 0 ? Angulo(rots[i - 1], rot) : 0f;
                    float dNext = i < n - 2 ? Angulo(rot, rots[i + 1]) : 0f;
                    Vector2 mid = (a + b) * 0.5f;

                    float f = alfaGlobal * ola;

                    // === LA PILA (la receta pública de StormLib — pasadas
                    //     SÓLIDAS del pixel del motor cuya SUMA es el
                    //     degradado; la OLA de vanilla gobierna la f) ===
                    for (int k = 0; k < StormLib.PilaW.Length; k++)
                    {
                        float wk = w * StormLib.PilaW[k];
                        float ext = Junta(wk, dPrev) + Junta(wk, dNext);
                        Quad(batch, pixel, mid, new Vector2(largo + ext, wk), rot,
                            Tinte(color, StormLib.PilaF[k] * f));
                    }

                    // LA VENA — la línea BLANCA razor (arde SIEMPRE, como el
                    // núcleo de vanilla; suelo 1.5 px — el pincel sólido
                    // mantiene la línea CRISPA).
                    float wVena = Math.Max(w * 0.34f, 1.5f);
                    float extV = Junta(wVena, dPrev) + Junta(wVena, dNext);
                    Quad(batch, pixel, mid, new Vector2(largo + extV, wVena), rot,
                        Tinte(nucleo, f));
                }
            }
            catch { }
        }

        /// <summary>El ángulo entre dos rumbos (0..π).</summary>
        private static float Angulo(float r0, float r1)
            => Math.Abs(MathHelper.WrapAngle(r1 - r0));

        /// <summary>La extensión de junta de la casa (e = w/2·tan(δ/2), suelo 1.2 px).</summary>
        private static float Junta(float w, float giro)
            => MathHelper.Clamp(w * 0.5f * (float)Math.Tan(giro * 0.5f) + 1.2f, 1.2f, w * 0.5f + 1.2f);

        /// <summary>Quad centrado rotado (tamaño en px de pantalla, tamaño de textura 8×64).</summary>
        private static void Quad(SpriteBatch batch, Texture2D tex, Vector2 pos,
            Vector2 tam, float rot, Color tinte)
        {
            if (tinte.A == 0 || tam.X < 0.05f || tam.Y < 0.05f) return;
            batch.Draw(tex, pos, null, tinte, rot,
                new Vector2(tex.Width, tex.Height) * 0.5f,
                tam / new Vector2(tex.Width, tex.Height),
                SpriteEffects.None, 0f);
        }
    }

    // ======================================================================
    //  RayoSistema — EL POZO DE RAYOS ACTIVOS (el ParticlePool de vanilla,
    //  en la casa: Update con luz, Draw en PostDrawTiles encima de todo)
    // ======================================================================

    /// <summary>UN rayo vivo en el pozo.</summary>
    internal struct RayoActivo
    {
        public bool Vivo;
        public List<RayoBolt> Arbol;      // tronco + horquillas
        public Color Color;
        public Color Nucleo;
        public float Ancho;
        public int VidaTotal;
        public int Edad;
        public AnimRayo Anim;
        public float Intensidad;
        public bool LuzEstable;          // AddLight por el tronco (los de arma)
        public int JugadorAncla;          // −1: sin ancla; si no: enganchado a la mano
        public Vector2 OrigenAncla;       // la mano en el nacimiento (la elasticidad)

        // --- v6.50.24 — LA FIRMA VENA TRUENO (ThunderTrail del Coralite) ---
        public RayoParams Params;         // para la RECADA (re-germinar el zigzag)
        public Vector2 Blanco;            // el blanco de cada re-germinación
        public Vector2 Direccion;         // la dirección (Vector2.Zero = sin)
        public bool TieneDireccion;
        public int Recada;                // re-random cada N ticks (0: canal congelado)
        public bool Parpadeo;             // 50% de frames visibles (el flicker)
        public float AnchoFin;            // 0: ancho fijo; si no: crece Ancho→AnchoFin
        public bool AperturaColapso;      // el zigzag se ABRE al morir (la desintegración)
        public float DesvioBase;          // el FactorDesvio original (para la apertura)
    }

    /// <summary>
    /// El pozo de rayos de la casa: hasta 24 canales vivos. Update avanza la
    /// ola y emite luz; Draw los pinta TODOS en un solo lote aditivo tras
    /// los tiles (encima de NPCs y proyectiles — como el pase
    /// World_OverPlayers de vanilla).
    /// </summary>
    public class RayoSistema : ModSystem
    {
        private const int Capacidad = 24;
        private static readonly RayoActivo[] _pozo = new RayoActivo[Capacidad];

        /// <summary>¿Hay rayos vivos? (para saltar el frame vacío).</summary>
        private static bool HayVivos()
        {
            for (int i = 0; i < Capacidad; i++)
                if (_pozo[i].Vivo) return true;
            return false;
        }

        /// <summary>
        /// LANZA un rayo al pozo (solo cliente — el daño lo hacen las armas
        /// por sus propios proyectiles; esto es el CINE). Devuelve false si
        /// el pozo está a tope (el más viejo no-prioritario cede).
        /// </summary>
        public static bool Lanzar(RayoParams p, uint semilla, Vector2 blanco, Vector2? direccion,
            Color color, Color nucleo, float ancho, int vidaTicks, AnimRayo anim,
            bool luzEstable = true, int jugadorAncla = -1)
            => Lanzar(p, semilla, blanco, direccion, color, nucleo, ancho, vidaTicks, anim,
                luzEstable, jugadorAncla, 0, false, 0f, false);

        /// <summary>
        /// v6.50.24 — EL LANZAMIENTO CON LA FIRMA VENA TRUENO (el ThunderTrail
        /// del Coralite, portado sobre el generador de vanilla): el canal
        /// PARPADEA (50% de frames), se RE-GERMINA cada <paramref
        /// name="recada"/> ticks (el zigzag re-random de Coralite — 4 en
        /// vida, 6 en la agonía), puede CRECER de ancho
        /// (<paramref name="anchoFin"/>) y DESINTEGRARSE abriéndose al morir
        /// (<paramref name="aperturaColapso"/>: el sobre del meandro se
        /// ensancha hasta ×3.5 en el colapso, la muerta del ThunderFalling).
        /// </summary>
        public static bool Lanzar(RayoParams p, uint semilla, Vector2 blanco, Vector2? direccion,
            Color color, Color nucleo, float ancho, int vidaTicks, AnimRayo anim,
            bool luzEstable, int jugadorAncla,
            int recada, bool parpadeo, float anchoFin, bool aperturaColapso)
        {
            if (Main.netMode == NetmodeID.Server || Main.dedServ) return false;
            try
            {
                var arbol = new List<RayoBolt>();
                RayoLib.Generar(p, arbol, semilla, blanco, direccion);

                int slot = -1;
                int masViejo = -1; int edadMax = -1;
                for (int i = 0; i < Capacidad; i++)
                {
                    if (!_pozo[i].Vivo) { slot = i; break; }
                    if (_pozo[i].Edad > edadMax) { edadMax = _pozo[i].Edad; masViejo = i; }
                }
                if (slot < 0) slot = masViejo;      // el más contado cede
                if (slot < 0) return false;

                Vector2 origenAncla = Vector2.Zero;
                if (jugadorAncla >= 0 && jugadorAncla < Main.maxPlayers &&
                    Main.player[jugadorAncla] != null && Main.player[jugadorAncla].active)
                    origenAncla = Main.player[jugadorAncla].Center;   // la "mano" del ancla (el centro del jugador — el puerto del GetPlayerAnchorPos)

                _pozo[slot] = new RayoActivo
                {
                    Vivo = true,
                    Arbol = arbol,
                    Color = color,
                    Nucleo = nucleo,
                    Ancho = MathHelper.Clamp(ancho, 2f, 96f),
                    VidaTotal = Math.Max(4, vidaTicks),
                    Edad = 0,
                    Anim = anim,
                    Intensidad = 1f,
                    LuzEstable = luzEstable,
                    JugadorAncla = jugadorAncla,
                    OrigenAncla = origenAncla,
                    Params = p,
                    Blanco = blanco,
                    Direccion = direccion ?? Vector2.Zero,
                    TieneDireccion = direccion.HasValue,
                    Recada = Math.Max(0, recada),
                    Parpadeo = parpadeo,
                    AnchoFin = MathHelper.Clamp(anchoFin, 0f, 128f),
                    AperturaColapso = aperturaColapso,
                    DesvioBase = p?.FactorDesvio ?? 5f,
                };
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// v6.50.24 — LA RECADA VENA TRUENO: re-germina el árbol del canal
        /// (otra semilla — el MISMO blanco y dirección) y, en el colapso,
        /// ABRE el sobre del meandro (FactorDesvio hasta ×3.5 del base, la
        /// desintegración del ThunderFalling de Coralite: el zigzag se
        /// desparrama mientras muere).
        /// </summary>
        private static void Recadar(ref RayoActivo r)
        {
            try
            {
                if (r.Params == null) return;
                float p = r.Edad / (float)Math.Max(1, r.VidaTotal);
                float spread = 1f;
                if (r.AperturaColapso && p > 0.58f)
                    spread += 2.5f * ((p - 0.58f) / 0.42f);   // ×1→×3.5 en la agonía
                r.Params.FactorDesvio = r.DesvioBase * spread;
                var arbol = new List<RayoBolt>();
                RayoLib.Generar(r.Params, arbol,
                    (uint)Main.rand.Next(1, int.MaxValue),
                    r.Blanco, r.TieneDireccion ? (Vector2?)r.Direccion : null);
                if (arbol.Count > 0) r.Arbol = arbol;
            }
            catch { }
        }

        /// <summary>La posición del tronco final (el punto de impacto, para las armas).</summary>
        public static Vector2 PuntoImpacto(RayoParams p, uint semilla, Vector2 blanco, Vector2? direccion)
        {
            var tronco = RayoLib.Generar(p, null, semilla, blanco, direccion);
            return tronco.Puntos != null && tronco.Puntos.Length > 0
                ? tronco.Puntos[tronco.Puntos.Length - 1] : blanco;
        }

        public override void PostUpdateWorld()
        {
            if (Main.netMode == NetmodeID.Server || Main.dedServ) return;
            for (int i = 0; i < Capacidad; i++)
            {
                if (!_pozo[i].Vivo) continue;
                RayoActivo r = _pozo[i];
                r.Edad++;
                if (r.Edad >= r.VidaTotal) { r.Vivo = false; _pozo[i] = r; continue; }

                // v6.50.24 — LA RECADA VENA TRUENO (el re-random de Coralite):
                // cada N ticks el zigzag GERMINA DE NUEVO (otra semilla, el
                // MISMO blanco). En la agonía (p>0.62) la recada se RELAJA a
                // cada 6 ticks — el flicker de la muerte del ThunderFalling.
                if (r.Recada > 0)
                {
                    float p = r.Edad / (float)Math.Max(1, r.VidaTotal);
                    int cada = p > 0.62f ? Math.Max(6, r.Recada + 2) : r.Recada;
                    if (r.Edad % cada == 0) { Recadar(ref r); _pozo[i] = r; }
                }
                _pozo[i] = r;

                // LA LUZ (el SteadyLight de vanilla): cada 20 posiciones del
                // TRONCO, con la curva de entrada/salida del propio juego
                // (Remap(0→0.3)·Remap(0.7→1) sobre la vida).
                if (r.LuzEstable && r.Arbol != null && r.Arbol.Count > 0)
                {
                    float p = r.Edad / (float)r.VidaTotal;
                    float k = RayoLib.Remap(p, 0f, 0.3f, 0f, 1f) * RayoLib.Remap(p, 0.7f, 1f, 1f, 0f);
                    Vector3 c = r.Color.ToVector3() * k;
                    RayoBolt tronco = null;
                    foreach (var b in r.Arbol) if (b.EsTronco) { tronco = b; break; }
                    if (tronco?.Puntos != null)
                    {
                        for (int k2 = 0; k2 < tronco.Puntos.Length; k2 += 20)
                        {
                            Lighting.AddLight(tronco.Puntos[k2], c.X, c.Y, c.Z);
                        }
                        // el punto de impacto siempre arde mientras vive.
                        var fin = tronco.Puntos[tronco.Puntos.Length - 1];
                        Lighting.AddLight(fin, c.X, c.Y, c.Z);
                    }
                }
            }
        }

        public override void PostDrawTiles()
        {
            if (Main.netMode == NetmodeID.Server || Main.dedServ) return;
            if (!HayVivos()) return;

            // El lote aditivo de la casa (el pase de vanilla pinta los rayos
            // en ParticleSystem_World_OverPlayers — ENCIMA de los jugadores;
            // aquí: PostDrawTiles, encima de NPCs y proyectiles).
            try
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                for (int i = 0; i < Capacidad; i++)
                {
                    if (!_pozo[i].Vivo || _pozo[i].Arbol == null) continue;
                    RayoActivo r = _pozo[i];
                    float progreso = r.Edad / (float)r.VidaTotal;

                    // v6.50.24 — EL PARPADEO VENA TRUENO (el CanDraw =
                    // NextBool() de Coralite): el canal está vivo solo en
                    // el 50% de los frames — el flicker eléctrico del trío
                    // del dragón. (los canales de vanilla NO parpadean:
                    // solo aplica a quien lo pide).
                    if (r.Parpadeo && !Main.rand.NextBool()) continue;

                    // v6.50.24 — EL ANCHO VENA TRUENO (la receta del
                    // ThunderFalling de Coralite): CRECE de Ancho a AnchoFin
                    // en la caída (primer 55% de la vida) y COLAPSA hacia
                    // el 20% en la agonía (el 20+(1−f)·100 del original).
                    float ancho = r.Ancho;
                    if (r.AnchoFin > 0f)
                    {
                        if (progreso < 0.55f)
                            ancho = MathHelper.Lerp(r.Ancho, r.AnchoFin, progreso / 0.55f);
                        else
                            ancho = MathHelper.Lerp(r.AnchoFin, r.AnchoFin * 0.2f,
                                (progreso - 0.55f) / 0.45f);
                    }

                    // LA ANCLA ELÁSTICA DEL ARC SURGE (la mano que se mueve
                    // arrastra el nacimiento del canal con caída cuártica:
                    // (1 − d²/D²)² — el rayo CUELGA de la mano).
                    Vector2 deltaAncla = Vector2.Zero;
                    if (r.JugadorAncla >= 0 && r.JugadorAncla < Main.maxPlayers)
                    {
                        Player j = Main.player[r.JugadorAncla];
                        if (j != null && j.active)
                        {
                            Vector2 mano = j.RotatedRelativePoint(j.Center);
                            deltaAncla = mano - r.OrigenAncla;
                        }
                    }

                    foreach (RayoBolt bolt in r.Arbol)
                    {
                        // ¿la ancla tira de ESTE canal? (solo el tronco nace
                        // en la mano; las horquillas nacen del canal).
                        RayoBolt dibujar = bolt;
                        if (deltaAncla != Vector2.Zero && bolt.EsTronco && bolt.Puntos != null &&
                            bolt.Puntos.Length > 1)
                        {
                            Vector2 n0 = bolt.Puntos[0];
                            float D = Math.Max(32f, Vector2.Distance(n0, bolt.Puntos[bolt.Puntos.Length - 1]));
                            var pts = new Vector2[bolt.Puntos.Length];
                            for (int k = 0; k < pts.Length; k++)
                            {
                                float dd = Vector2.Distance(bolt.Puntos[k], n0) / D;
                                float peso = (1f - dd * dd);
                                peso *= peso;                       // cuártica
                                pts[k] = bolt.Puntos[k] + deltaAncla * peso;
                            }
                            dibujar = new RayoBolt
                            {
                                Puntos = pts,
                                Rotaciones = null,                  // recalcula (curvatura nueva)
                                Rango = bolt.Rango,
                                Profundidad = bolt.Profundidad,
                                ChocoTile = bolt.ChocoTile,
                            };
                        }
                        RayoLib.Dibujar(Main.spriteBatch, dibujar, r.Color, r.Nucleo,
                            ancho, progreso, r.Intensidad, r.Anim);
                    }
                }
            }
            catch { }
            finally
            {
                VFXCore.CerrarLoteSiAbierto();
            }
        }

        public override void Unload()
        {
            for (int i = 0; i < Capacidad; i++) _pozo[i] = default;
        }
    }
}
