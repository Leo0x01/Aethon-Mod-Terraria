using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// SigiloLib — v6.34 — LA ESCRITURA MÁGICA DEL SOL.
    ///
    /// LA LIBRERÍA DE SIGNOS MÁGICOS DE LOS SOLES RÚNICOS: todo lo que hoy
    /// viste a las estrellas rúnicas — sus anillos orbitales, sus glifos
    /// cabalgando la tangente, sus perlas, la corona de glifos del jugador —
    /// promovido a PRIMITIVAS INVOCABLES para embellecer cualquier cosa:
    /// portales, invocaciones, telegrafías, suelos, jefes.
    ///
    /// LAS LEYES DE LA FAMILIA (heredadas 1:1 del sol — ni un número
    /// cambiado, el look de las 20 copias queda INTACTO):
    ///   · La GEOMETRÍA: N anillos elípticos, cada uno en SU plano (tilt
    ///     escalonado + achatado por anillo) con packing ajustado del 10º
    ///     en arriba, PRECESIÓN viva en tier alto, y GIRO ALTERNO (par
    ///     horario, impar antihorario — "el otro debe rodear el sol en otra
    ///     dirección").
    ///   · El ARO: polilínea de cápsulas con PROFUNDIDAD (el frente de la
    ///     órbita más brillante que la espalda) y latido recorriendo el
    ///     anillo.
    ///   · LA RUNA: glifo de trazo angular rotado a la TANGENTE (la runa
    ///     "de pie" sobre el aro, andando con él), respiración por glifo,
    ///     latido propio, gradiente vertical cuerpo→punta y PERLA encima
    ///     (la gema del sello).
    ///   · LOS DOS ALFABETOS: la ESCRITURA SOLAR (8 glifos: el astro, la
    ///     llama, la rueda, la espiga, la puerta, la corona, el cometa, el
    ///     sigilo maestro) y la CORONA DEL PORTADOR (8 glifos: la lanza, el
    ///     cáliz, la puerta, la estrella, el rayo, el arco, la espiral, el
    ///     trono).
    ///
    /// LO NUEVO (el motivo de la librería — el arsenal de embellecimiento):
    ///   · Runa(...) — UN glifo standalone en cualquier posición/rotación:
    ///     la letra suelta de la escritura solar, para firmar cualquier
    ///     efecto.
    ///   · SelloSolar(...) — EL SIGNO MÁGICO INVOCABLE: el círculo mágico
    ///     completo (aro doble + runas de pie + 4 nodos cardinales con
    ///     destello + el glifo maestro central + polvo orbital).
    ///   · NodoCardinal(...) y PolvoRunico(...) — adornos sueltos.
    ///
    /// CONTRATO DE DIBUJO: TODO se emite al BUFFER de VFXCore (pase
    /// aditivo, coordenadas de MUNDO). El llamador controla Begin/Flush.
    /// Estilo de tinte de la familia solar: ALFA PURO (Tint — RGB intacto,
    /// canal alfa = intensidad), idéntico al sistema del sol.
    /// </summary>
    public static class SigiloLib
    {
        // ==================================================================
        //  LAS LEYES DE LA FAMILIA — constantes públicas (eran el corazón
        //  privado de RuneSunRenderer; ahora son el CONTRATO de la librería)
        // ==================================================================

        /// <summary>Semieje mayor del 1er anillo (×R).</summary>
        public const float RingA0 = 1.62f;

        /// <summary>Separación entre anillos (×R).</summary>
        public const float RingAStep = 0.44f;

        /// <summary>Packing más ajustado del 10º anillo en arriba (×R).</summary>
        public const float RingAStep2 = 0.30f;

        /// <summary>Giro base (rad/s).</summary>
        public const float RingSpin0 = 0.26f;

        /// <summary>Aceleración del giro por anillo (rad/s por anillo).</summary>
        public const float RingSpinStep = 0.045f;

        /// <summary>Glifos del 1er anillo.</summary>
        public const int Runes0 = 6;

        /// <summary>+2 glifos por anillo (hasta el 9º).</summary>
        public const int RuneStep = 2;

        /// <summary>+1 glifo por anillo del 10º en arriba.</summary>
        public const int RuneStep2 = 1;

        /// <summary>Anillos máximos del sistema completo.</summary>
        public const int MaxAnillos = 20;

        /// <summary>Tier a partir del cual los planos PRECESAN (bamboleo vivo).</summary>
        public const int TierPrecesion = 7;

        // ==================================================================
        //  LA PALETA DE RUNAS DEL SOL (los colores calibrados de la familia)
        // ==================================================================

        /// <summary>Cuerpo de runa dorada (el oro del sol).</summary>
        public static readonly Color CuerpoOro = new(255, 190, 80);

        /// <summary>Punta pálida de la runa dorada.</summary>
        public static readonly Color PuntaOro = new(255, 240, 185);

        /// <summary>Cuerpo de runa azul (el sello frío de tier alto).</summary>
        public static readonly Color CuerpoAzul = new(135, 165, 255);

        /// <summary>Punta fría de la runa azul.</summary>
        public static readonly Color PuntaAzul = new(215, 230, 255);

        /// <summary>Carmesí de la gigante final (tiñe el sistema al morir).</summary>
        public static readonly Color CarminGigante = new(255, 90, 40);

        // ==================================================================
        //  EL PINCEL (la textura de glow procedural del proyecto)
        // ==================================================================

        private static Asset<Texture2D> _glow;

        private static Texture2D Glow =>
            (_glow ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow")).Value;

        // ==================================================================
        //  LOS DOS ALFABETOS — la escritura solar y la corona del portador
        // ==================================================================

        /// <summary>
        /// LA ESCRITURA SOLAR (8 glifos del sistema de anillos del sol):
        /// cada runa es una lista de TRAZOS (pares de puntos en espacio
        /// local ~11×15). Ocho diseños angulares de estilo ASTRO RÚNICO:
        /// soles, llamas, ruedas y puertas.
        /// </summary>
        public static readonly Vector2[][] RunasSolares = new Vector2[][]
        {
            // R0 — EL ASTRO (el punto de luz con rayos)
            new Vector2[] { new(0f, -4.5f), new(0f, 4.5f), new(-4.5f, 0f), new(4.5f, 0f), new(-3f, -3f), new(-1.2f, -1.2f), new(3f, -3f), new(1.2f, -1.2f), new(-3f, 3f), new(-1.2f, 1.2f), new(3f, 3f), new(1.2f, 1.2f) },
            // R1 — LA LLAMA VIVA
            new Vector2[] { new(0f, 6.5f), new(0f, 1f), new(0f, 1f), new(-3f, -2f), new(-3f, -2f), new(0f, -5f), new(0f, -5f), new(3f, -2f), new(3f, -2f), new(0f, 1f), new(-1.5f, -6.5f), new(1.5f, -6.5f) },
            // R2 — LA RUEDA SOLAR
            new Vector2[] { new(0f, -5f), new(0f, 5f), new(-5f, 0f), new(5f, 0f), new(-3.5f, -3.5f), new(3.5f, 3.5f), new(3.5f, -3.5f), new(-3.5f, 3.5f), new(-2.2f, 0f), new(2.2f, 0f), new(0f, -2.2f), new(0f, 2.2f) },
            // R3 — LA ESPIGA DE LUZ
            new Vector2[] { new(0f, -7f), new(0f, 7f), new(-3.2f, -3.5f), new(0f, -0.5f), new(3.2f, -3.5f), new(0f, -0.5f), new(-3.2f, 3.5f), new(0f, 0.5f), new(3.2f, 3.5f), new(0f, 0.5f) },
            // R4 — LA PUERTA DEL DÍA
            new Vector2[] { new(-3.5f, 7f), new(-3.5f, -5f), new(-3.5f, -5f), new(0f, -7f), new(0f, -7f), new(3.5f, -5f), new(3.5f, -5f), new(3.5f, 7f), new(-3.5f, 7f), new(3.5f, 7f), new(0f, -4f), new(0f, 7f) },
            // R5 — LA CORONA BAJA
            new Vector2[] { new(-4f, 5f), new(-4f, -2f), new(-4f, -2f), new(-1.5f, -5.5f), new(-1.5f, -5.5f), new(0f, -1.5f), new(0f, -1.5f), new(1.5f, -5.5f), new(1.5f, -5.5f), new(4f, -2f), new(4f, -2f), new(4f, 5f), new(-4f, 5f), new(4f, 5f) },
            // R6 — EL TRAZO DEL COMETA
            new Vector2[] { new(-4f, 6.5f), new(3f, -1f), new(3f, -1f), new(0f, -6.5f), new(0f, -6.5f), new(4f, -3f), new(1.5f, 2f), new(4.5f, 1.5f), new(-1.5f, 1f), new(1.5f, 4f) },
            // R7 — EL SIGILO SOLAR (el sello maestro)
            new Vector2[] { new(0f, -6.5f), new(-4f, 0f), new(-4f, 0f), new(0f, 6.5f), new(0f, 6.5f), new(4f, 0f), new(4f, 0f), new(0f, -6.5f), new(-2.2f, 0f), new(2.2f, 0f), new(0f, -4f), new(0f, 4f) },
        };

        /// <summary>
        /// LA CORONA DEL PORTADOR (8 glifos del arco que flota sobre la
        /// cabeza): la lanza, el cáliz, la puerta, la estrella, el rayo, el
        /// arco, la espiral y el trono — trazos angulares legibles a
        /// distancia, cada uno con SU actitud.
        /// </summary>
        public static readonly Vector2[][] RunasCorona = new Vector2[][]
        {
            // R0 — LA LANZA: columna + dos diagonales hacia la derecha.
            new Vector2[] { new(-2.5f, 7f), new(-2.5f, -7f), new(-2.5f, -2f), new(3f, -6f), new(-2.5f, 3f), new(2.5f, -1.5f) },
            // R1 — EL CÁLIZ: dos diagonales que caen al centro + base.
            new Vector2[] { new(-4f, -6f), new(0f, 3f), new(4f, -6f), new(0f, 3f), new(-2f, 5.5f), new(2f, 5.5f) },
            // R2 — LA PUERTA: dos columnas + dos travesaños.
            new Vector2[] { new(-3f, 7f), new(-3f, -7f), new(3f, 7f), new(3f, -7f), new(-3f, -5f), new(3f, -5f), new(-3f, 2f), new(3f, 2f) },
            // R3 — LA ESTRELLA: cruz + dos diagonales (el asterisco).
            new Vector2[] { new(0f, 7f), new(0f, -7f), new(-4f, 0f), new(4f, 0f), new(-3f, -5f), new(3f, 5f), new(3f, -5f), new(-3f, 5f) },
            // R4 — EL RAYO: zigzag eléctrico de tres quiebros.
            new Vector2[] { new(-2f, 7f), new(0.5f, 1f), new(0.5f, 1f), new(-2f, -3f), new(-2f, -3f), new(2.5f, -7f) },
            // R5 — EL ARCO: dos diagonales simétricas + dintel superior.
            new Vector2[] { new(-3f, 6f), new(-1f, -6f), new(1f, -6f), new(3f, 6f), new(-1.5f, -6f), new(1.5f, -6f) },
            // R6 — LA ESPIRAL: escalera angular que gira.
            new Vector2[] { new(-3f, 6f), new(-3f, -2f), new(-3f, -2f), new(3f, -6f), new(3f, -6f), new(3f, 2f), new(3f, 2f), new(-2f, 6f) },
            // R7 — EL TRONO: columna central + dos brazos en diagonal + faja.
            new Vector2[] { new(0f, 7f), new(0f, -7f), new(-3f, -2f), new(0f, -7f), new(0f, -2f), new(3f, -7f), new(-2f, 4f), new(2f, 4f) },
        };

        // ==================================================================
        //  LA GEOMETRÍA — las leyes orbitales (públicas, deterministas)
        // ==================================================================

        /// <summary>
        /// Semieje mayor del anillo k (×R). Del 10º en arriba el packing se
        /// AJUSTA para que 20 anillos quepan espectaculares sin lo ridículo.
        /// </summary>
        public static float RingA(int k)
        {
            int k1 = Math.Min(k, 9);
            int k2 = Math.Max(0, k - 9);
            return RingA0 + RingAStep * k1 + RingAStep2 * k2;
        }

        /// <summary>Achatado del anillo k (plano distinto por anillo).</summary>
        public static float RingFlat(int k) => 0.34f + 0.07f * (k % 3);

        /// <summary>
        /// Inclinación del plano del anillo k — y con PRECESIÓN (si
        /// <paramref name="precesion"/>): el plano BAMBOLEA vivo alrededor
        /// de su valor base.
        /// </summary>
        public static float RingTilt(int k, float time, bool precesion)
        {
            float baseTilt = -0.55f + 0.20f * k;
            if (precesion)
                baseTilt += 0.10f * (float)Math.Sin(time * (0.35f + 0.06f * k) + k * 1.9f);
            return baseTilt;
        }

        /// <summary>
        /// El GIRO del anillo k: ALTERNO (par horario, impar antihorario),
        /// con velocidad creciente por anillo.
        /// </summary>
        public static float RingSpin(int k) =>
            (k % 2 == 0 ? 1f : -1f) * (RingSpin0 + RingSpinStep * k);

        /// <summary>Glifos del anillo k (la cuenta LITERAL de la familia).</summary>
        public static int RunesOfRing(int k) =>
            Runes0 + RuneStep * Math.Min(k, 9) + RuneStep2 * Math.Max(0, k - 9);

        /// <summary>Punto sobre una elipse girada (param t en rad).</summary>
        public static Vector2 EllipsePoint(Vector2 center, float a, float b, float tilt, float t)
        {
            float ct = (float)Math.Cos(t), st = (float)Math.Sin(t);
            Vector2 local = new Vector2(a * ct, b * st);
            float cR = (float)Math.Cos(tilt), sR = (float)Math.Sin(tilt);
            return center + new Vector2(local.X * cR - local.Y * sR, local.X * sR + local.Y * cR);
        }

        /// <summary>Ángulo de la TANGENTE de la elipse en t (la runa "de pie").</summary>
        public static float TangenteElipse(float a, float b, float tilt, float t)
        {
            Vector2 dLocal = new Vector2(-a * (float)Math.Sin(t), b * (float)Math.Cos(t));
            float cR = (float)Math.Cos(tilt), sR = (float)Math.Sin(tilt);
            Vector2 d = new Vector2(dLocal.X * cR - dLocal.Y * sR, dLocal.X * sR + dLocal.Y * cR);
            return (float)Math.Atan2(d.Y, d.X);
        }

        /// <summary>Tinte de la familia solar: RGB intacto, alfa = intensidad.</summary>
        public static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color(c.R, c.G, c.B, (byte)(int)(255f * f));
        }

        // ==================================================================
        //  PRIMITIVA 0 — LA RUNA SUELTA (la letra de la escritura solar)
        // ==================================================================

        /// <summary>
        /// UN glifo rúnico standalone: los trazos angulares del alfabeto,
        /// rotados a <paramref name="rot"/>, con gradiente vertical
        /// cuerpo→punta, resplandor trasero, PERLA opcional encima y latido
        /// propio. LA letra suelta para firmar cualquier efecto con
        /// escritura solar.
        /// </summary>
        /// <param name="pos">Centro del glifo (mundo).</param>
        /// <param name="rot">Rotación del marco del glifo (rad).</param>
        /// <param name="alfabeto">RunasSolares o RunasCorona.</param>
        /// <param name="indice">Índice del glifo en el alfabeto (mod len).</param>
        /// <param name="escala">Escala del glifo (1 ≈ 11×15 px).</param>
        /// <param name="time">Tiempo animado.</param>
        /// <param name="fase">Desfase del latido (por glifo/índice).</param>
        /// <param name="cuerpo">Color del cuerpo del trazo.</param>
        /// <param name="punta">Color de la punta pálida.</param>
        /// <param name="brillo">Multiplicador global (0..1+).</param>
        /// <param name="perla">Dibujar la perla encima del glifo.</param>
        public static void Runa(Vector2 pos, float rot, Vector2[][] alfabeto, int indice,
            float escala, float time, float fase, Color cuerpo, Color punta,
            float brillo, bool perla = true)
        {
            if (brillo <= 0.02f || escala <= 0.05f) return;
            Vector2[] strokes = alfabeto[indice % alfabeto.Length];

            // Latido de brillo propio.
            float pulse = 0.75f + 0.25f * (float)Math.Sin(time * 2.4f + fase * 1.3f);

            // Resplandor suave DETRÁS (el grabado ardiendo).
            VFXCore.Quad(pos, Tint(cuerpo, 0.20f * pulse * brillo),
                new Vector2(34f * escala, 34f * escala), 0f, Glow);

            // Los TRAZOS del glifo (rotados al marco pedido).
            for (int s = 0; s < strokes.Length; s += 2)
            {
                Vector2 localA = strokes[s] * escala;
                Vector2 localB = strokes[s + 1] * escala;
                Vector2 rotA = localA.RotatedBy(rot) + pos;
                Vector2 rotB = localB.RotatedBy(rot) + pos;
                Vector2 mid = (rotA + rotB) * 0.5f;
                Vector2 delta = rotB - rotA;
                float len = delta.Length();
                if (len < 0.01f) continue;
                float trazoRot = (float)Math.Atan2(delta.Y, delta.X);

                // Gradiente vertical local: abajo cuerpo, arriba punta pálida.
                float localY = ((strokes[s].Y + strokes[s + 1].Y) * 0.5f + 7f) / 14f;
                Color col = Color.Lerp(punta, cuerpo, 1f - localY * 0.25f);

                EmitCapsule(mid, len, 3.3f * escala, trazoRot, Tint(col, 0.85f * pulse * brillo));
            }

            if (perla)
            {
                // PERLA sobre el glifo (la gema del sello).
                Vector2 pearlPos = pos + new Vector2(0f, -11.5f * escala).RotatedBy(rot);
                VFXCore.Quad(pearlPos, Tint(cuerpo, 0.60f * pulse * brillo),
                    new Vector2(7.0f * escala, 7.0f * escala), 0f, Glow);
                VFXCore.Quad(pearlPos, Tint(punta, 0.9f * pulse * brillo),
                    new Vector2(3.2f * escala, 3.2f * escala), 0f, Glow);
            }
        }

        /// <summary>Cápsula al buffer compartido (coords de mundo).</summary>
        private static void EmitCapsule(Vector2 mid, float len, float width, float rot, Color tint)
        {
            if (tint.A == 0) return;
            VFXCore.Quad(mid, tint, new Vector2(len + width, width * 1.9f), rot);
        }

        // ==================================================================
        //  PRIMITIVA 1 — EL ARO ELÍPTICO (profundidad + latido)
        // ==================================================================

        /// <summary>
        /// El ARO de un anillo: polilínea de cápsulas con PROFUNDIDAD (el
        /// frente de la órbita más brillante que la espalda) y latido de
        /// energía recorriéndolo.
        /// </summary>
        /// <param name="center">Centro del sistema (mundo).</param>
        /// <param name="a">Semieje mayor (px).</param>
        /// <param name="b">Semieje menor (px).</param>
        /// <param name="tilt">Inclinación del plano (rad).</param>
        /// <param name="spin">Ángulo de giro actual (rad — ya integrado).</param>
        /// <param name="time">Tiempo animado (para el latido).</param>
        /// <param name="k">Índice del anillo (desfasa el latido).</param>
        /// <param name="body">Color del aro.</param>
        /// <param name="fade">Fundido de vida (0..1).</param>
        public static void AroEliptico(Vector2 center, float a, float b, float tilt, float spin,
            float time, int k, Color body, float fade)
        {
            const int Segments = 30;
            Vector2 prev = EllipsePoint(center, a, b, tilt, spin);
            for (int s = 1; s <= Segments; s++)
            {
                float t = spin + s / (float)Segments * MathHelper.TwoPi;
                Vector2 pt = EllipsePoint(center, a, b, tilt, t);
                Vector2 mid = (prev + pt) * 0.5f;
                Vector2 delta = pt - prev;
                float len = delta.Length();
                if (len > 0.5f)
                {
                    float rot = (float)Math.Atan2(delta.Y, delta.X);
                    // Profundidad: sin(t)·cos(tilt) > 0 → frente de la órbita.
                    float depth = 0.55f + 0.45f *
                        (float)Math.Sin(t + MathHelper.PiOver2) * (float)Math.Cos(tilt);
                    // Latido del aro (la energía recorre el anillo).
                    float pulse = 0.70f + 0.30f * (float)Math.Sin(time * 1.8f + k * 1.3f + s * 0.35f);
                    EmitCapsule(mid, len, Math.Max(2.2f, 0.052f * a) * (1f + 0.35f * depth),
                        rot, Tint(body, (0.30f + 0.30f * depth) * pulse * fade));
                }
                prev = pt;
            }
        }

        // ==================================================================
        //  PRIMITIVA 2 — LA RUNA EN ÓRBITA (cabalgando la tangente)
        // ==================================================================

        /// <summary>
        /// Un glifo rúnico sobre SU órbita elíptica, rotado a la TANGENTE
        /// (la runa "de pie" sobre el aro, andando con él), con respiración
        /// por glifo y perla. La unidad viva del sistema del sol.
        /// </summary>
        public static void RunaOrbitando(Vector2 center, float a, float b, float tilt,
            float ang, float time, int k, int g, Color body, Color tip,
            float glyphScale, float fade)
        {
            // Flotación viva: el radio respira por glifo.
            float breathe = 1f + 0.045f * (float)Math.Sin(time * 1.35f + g * 0.9f + k * 0.5f);
            Vector2 glyphPos = EllipsePoint(center, a * breathe, b * breathe, tilt, ang);

            // La TANGENTE de la órbita en este punto: la runa cabalga de pie.
            float tanAng = TangenteElipse(a * breathe, b * breathe, tilt, ang);
            float glyphRot = tanAng + MathHelper.PiOver2;

            // Latido de brillo propio.
            float pulse = 0.75f + 0.25f * (float)Math.Sin(time * 2.4f + g * 1.3f + k * 0.8f);

            // Resplandor suave DETRÁS (el grabado ardiendo).
            VFXCore.Quad(glyphPos, Tint(body, 0.20f * pulse * fade),
                new Vector2(34f * glyphScale, 34f * glyphScale), 0f, Glow);

            // Los TRAZOS del glifo (rotados con la órbita).
            Vector2[] strokes = RunasSolares[(g + k) % RunasSolares.Length];
            for (int s = 0; s < strokes.Length; s += 2)
            {
                Vector2 localA = strokes[s] * glyphScale;
                Vector2 localB = strokes[s + 1] * glyphScale;
                Vector2 rotA = localA.RotatedBy(glyphRot) + glyphPos;
                Vector2 rotB = localB.RotatedBy(glyphRot) + glyphPos;
                Vector2 mid = (rotA + rotB) * 0.5f;
                Vector2 delta = rotB - rotA;
                float len = delta.Length();
                if (len < 0.01f) continue;
                float rot = (float)Math.Atan2(delta.Y, delta.X);

                // Gradiente vertical local: abajo cuerpo, arriba punta pálida.
                float localY = ((strokes[s].Y + strokes[s + 1].Y) * 0.5f + 7f) / 14f;
                Color col = Color.Lerp(tip, body, 1f - localY * 0.25f);

                EmitCapsule(mid, len, 3.3f * glyphScale, rot, Tint(col, 0.85f * pulse * fade));
            }

            // PERLA sobre el glifo (la gema del sello).
            Vector2 pearlPos = glyphPos + new Vector2(0f, -11.5f * glyphScale).RotatedBy(glyphRot);
            VFXCore.Quad(pearlPos, Tint(body, 0.60f * pulse * fade),
                new Vector2(7.0f * glyphScale, 7.0f * glyphScale), 0f, Glow);
            VFXCore.Quad(pearlPos, Tint(tip, 0.9f * pulse * fade),
                new Vector2(3.2f * glyphScale, 3.2f * glyphScale), 0f, Glow);
        }

        // ==================================================================
        //  PRIMITIVA 3 — EL ANILLO RÚNICO (aro + runas: la unidad completa)
        // ==================================================================

        /// <summary>
        /// UN anillo rúnico completo: el aro elíptico con profundidad +
        /// sus glifos cabalgando la tangente. La unidad atómica del sistema
        /// del sol, invocable sola para una órbita única alrededor de
        /// cualquier cosa.
        /// </summary>
        /// <param name="center">Centro (mundo).</param>
        /// <param name="a">Semieje mayor (px).</param>
        /// <param name="b">Semieje menor (px).</param>
        /// <param name="tilt">Inclinación del plano (rad).</param>
        /// <param name="spin">Giro actual (rad).</param>
        /// <param name="runeCount">Glifos del anillo.</param>
        /// <param name="glyphScale">Escala de los glifos.</param>
        /// <param name="time">Tiempo animado.</param>
        /// <param name="k">Índice del anillo (desfasa latidos).</param>
        /// <param name="body">Color cuerpo de runa.</param>
        /// <param name="tip">Color punta de runa.</param>
        /// <param name="fade">Fundido de vida (0..1).</param>
        public static void AnilloRunico(Vector2 center, float a, float b, float tilt, float spin,
            int runeCount, float glyphScale, float time, int k,
            Color body, Color tip, float fade)
        {
            if (fade <= 0.02f) return;

            AroEliptico(center, a, b, tilt, spin, time, k, body, fade);

            for (int g = 0; g < runeCount; g++)
            {
                float ang = g / (float)runeCount * MathHelper.TwoPi + spin;
                RunaOrbitando(center, a, b, tilt, ang, time, k, g, body, tip, glyphScale, fade);
            }
        }

        // ==================================================================
        //  PRIMITIVA 4 — EL NODO CARDINAL (perla + destello de 4 puntas)
        // ==================================================================

        /// <summary>
        /// Un NODO cardinal del sello: perla grande con halo, destello de
        /// 4 puntas (dos cápsulas cruzadas) y chispa central — el ancla
        /// luminosa en los puntos cardinales de un círculo mágico.
        /// </summary>
        /// <param name="pos">Posición del nodo (mundo).</param>
        /// <param name="px">Tamaño base (px).</param>
        /// <param name="time">Tiempo animado.</param>
        /// <param name="fase">Desfase del latido.</param>
        /// <param name="halo">Color del halo.</param>
        /// <param name="nucleo">Color del núcleo brillante.</param>
        /// <param name="brillo">Multiplicador global.</param>
        public static void NodoCardinal(Vector2 pos, float px, float time, float fase,
            Color halo, Color nucleo, float brillo)
        {
            if (brillo <= 0.02f) return;
            float pulse = 0.80f + 0.20f * (float)Math.Sin(time * 3.0f + fase);

            // Halo de la perla.
            VFXCore.Quad(pos, Tint(halo, 0.55f * pulse * brillo),
                new Vector2(px * 2.4f, px * 2.4f), 0f, Glow);
            // Núcleo brillante.
            VFXCore.Quad(pos, Tint(nucleo, 0.90f * pulse * brillo),
                new Vector2(px * 1.0f, px * 1.0f), 0f, Glow);

            // DESTELLO DE 4 PUNTAS: dos cápsulas cruzadas que respiran.
            float flare = px * 3.2f * (0.85f + 0.15f * (float)Math.Sin(time * 2.2f + fase));
            float w = Math.Max(1.6f, px * 0.16f);
            EmitCapsule(pos, flare, w, 0f, Tint(nucleo, 0.50f * pulse * brillo));
            EmitCapsule(pos, flare, w, MathHelper.PiOver2, Tint(nucleo, 0.50f * pulse * brillo));
        }

        // ==================================================================
        //  PRIMITIVA 5 — EL POLVO RÚNICO (motas orbitando el sello)
        // ==================================================================

        /// <summary>
        /// POLVO rúnico: motas de luz orbitando lentamente un centro, con
        /// parpadeo determinista por mota. El ambiente vivo de todo signo
        /// mágico (determinista por seed — cero estado).
        /// </summary>
        /// <param name="center">Centro (mundo).</param>
        /// <param name="radius">Radio orbital (px).</param>
        /// <param name="count">Número de motas.</param>
        /// <param name="time">Tiempo animado.</param>
        /// <param name="seed">Semilla determinista.</param>
        /// <param name="color">Color de las motas.</param>
        /// <param name="brillo">Multiplicador global.</param>
        public static void PolvoRunico(Vector2 center, float radius, int count, float time,
            int seed, Color color, float brillo)
        {
            if (brillo <= 0.02f) return;
            for (int i = 0; i < count; i++)
            {
                float h = VFXCore.Hash01(seed, 910 + i, 23);
                float dir = i % 2 == 0 ? 1f : -1f;
                float ang = h * MathHelper.TwoPi + time * 0.10f * dir;
                float r = radius * (0.72f + 0.48f * VFXCore.Hash01(seed, 911 + i, 31));
                Vector2 pos = center + new Vector2(
                    (float)Math.Cos(ang) * r, (float)Math.Sin(ang) * r * 0.82f);
                float twinkle = 0.45f + 0.55f * (float)Math.Sin(time * 2.6f + i * 2.0f);
                float size = (0.08f + 0.06f * h) * radius;
                VFXCore.Quad(pos, Tint(color, 0.50f * twinkle * brillo),
                    new Vector2(size, size), 0f, Glow);
            }
        }

        // ==================================================================
        //  COMPUESTO A — EL SISTEMA DE ANILLOS (la corona del sol, 1:1)
        // ==================================================================

        /// <summary>
        /// EL SISTEMA DE ANILLOS COMPLETO del sol rúnico: N anillos
        /// (n = min(tier, 20)), cada uno en su plano, con la jerarquía
        /// completa de la familia — packing, achatados, precesión en tier
        /// alto, giros alternos, runas creciendo por anillo y el color
        /// ORO/BLANCO/AZUL de la casa. Este MISMO trazo viste a los soles
        /// del mundo y a quien herede su escritura. `alpha` multiplica la
        /// intensidad (luz del mundo / energía).
        /// </summary>
        /// <param name="center">Centro del sistema (mundo).</param>
        /// <param name="R">Radio de referencia del cuerpo (px).</param>
        /// <param name="time">Tiempo animado (GlobalTimeWrappedHourly).</param>
        /// <param name="seed">Semilla determinista del disparo.</param>
        /// <param name="tier">Tier 1..20 (anillos = min(tier, 20)).</param>
        /// <param name="rg">0..1 de la gigante final (tiñe a carmesí).</param>
        /// <param name="lifeT">0..1 del ciclo de vida (fade).</param>
        /// <param name="alpha">Multiplicador de intensidad.</param>
        public static void SistemaAnillos(Vector2 center, float R, float time, int seed,
            int tier, float rg, float lifeT, float alpha = 1f)
        {
            if (R < 1f || alpha <= 0.02f) return;
            int n = Math.Min(tier, MaxAnillos);
            float glyphScale = Math.Max(R / 52f, 0.25f) * 1.45f;

            for (int k = 0; k < n; k++)
            {
                float a = RingA(k) * R;
                float b = a * RingFlat(k);
                float tilt = RingTilt(k, time, tier >= TierPrecesion);
                float spin = time * RingSpin(k);
                int runeCount = RunesOfRing(k);

                // El color del anillo: ORO por defecto, y cada 3º AZUL-ESTELAR
                // desde tier 7 (el sello frío).
                bool blue = tier >= TierPrecesion && k % 3 == 2;
                Color body = blue ? CuerpoAzul : CuerpoOro;
                Color tip = blue ? PuntaAzul : PuntaOro;
                // La gigante final tiñe todo hacia el carmesí.
                if (rg > 0f) body = Color.Lerp(body, CarminGigante, rg * 0.45f);

                float fade = (1f - lifeT * 0.55f) * alpha;
                AnilloRunico(center, a, b, tilt, spin, runeCount, glyphScale,
                    time, k, body, tip, fade);
            }
        }

        // ==================================================================
        //  COMPUESTO B — EL ARCO DE GLORIA (la corona del portador, 1:1)
        // ==================================================================

        /// <summary>
        /// EL ARCO DE GLORIA: el semicírculo de OCHO GLIFOS DEL PORTADOR
        /// flotando sobre una cabeza — cada glifo con su oscilación, su
        /// latido, su PERLA rosa pálido y las chispas que emite. Escritura
        /// mágica personal, no líneas de campo.
        /// </summary>
        /// <param name="anchor">Centro de la cabeza sobre la que flota.</param>
        /// <param name="scale">Escala (cabeza humana ≈ 1).</param>
        /// <param name="time">Tiempo animado.</param>
        /// <param name="alpha">Multiplicador de intensidad.</param>
        public static void ArcoGloria(Vector2 anchor, float scale, float time, float alpha = 1f)
        {
            if (scale <= 0.05f) return;

            const int GlyphCount = 8;
            float arcRadius = 26f * scale;
            // El arco flota por ENCIMA de la cabeza — y ALTO.
            Vector2 arcCenter = anchor - new Vector2(0f, 13f * scale);

            // Halo tenue del arco completo (cohesión: es UNA corona).
            for (int h = 0; h < 3; h++)
            {
                float ha = -MathHelper.PiOver2 + (h - 1) * 0.62f;
                Vector2 hpos = arcCenter + new Vector2(
                    (float)Math.Cos(ha) * arcRadius * 0.92f,
                    (float)Math.Sin(ha) * arcRadius * 0.92f);
                float hpulse = 0.7f + 0.3f * (float)Math.Sin(time * 1.6f + h * 2.1f);
                VFXCore.Quad(hpos, VFXPalettes.RuneStars.ArcHalo * (0.10f * hpulse * alpha),
                    new Vector2(30f * scale, 30f * scale));
            }

            for (int g = 0; g < GlyphCount; g++)
            {
                // Ángulo del glifo en el arco: -160° → -20° (semicírculo sup.).
                float angle = -MathHelper.Pi + MathHelper.Pi * 0.11f +
                              g / (float)(GlyphCount - 1) * MathHelper.Pi * 0.78f;

                // Flotación viva: el radio respira por glifo y el glifo se mece.
                float floatR = arcRadius +
                               2.4f * scale * (float)Math.Sin(time * 1.35f + g * 0.9f);
                float bobY = 2.0f * scale * (float)Math.Sin(time * 0.85f + g * 1.7f);

                Vector2 glyphPos = arcCenter + new Vector2(
                    (float)Math.Cos(angle) * floatR,
                    (float)Math.Sin(angle) * floatR + bobY);

                // Latido de brillo propio por glifo.
                float pulse = 0.75f + 0.25f * (float)Math.Sin(time * 2.4f + g * 1.3f);

                // Trazos de la runa: glows ESTIRADOS a lo largo de cada trazo
                // (soft capsule) — gradiente base fucsia → punta rosa pálido.
                Vector2[] strokes = RunasCorona[g % RunasCorona.Length];
                float strokeW = 2.1f * scale;

                for (int s = 0; s < strokes.Length; s += 2)
                {
                    Vector2 a = glyphPos + strokes[s] * scale;
                    Vector2 b = glyphPos + strokes[s + 1] * scale;
                    Vector2 mid = (a + b) * 0.5f;
                    Vector2 delta = b - a;
                    float len = delta.Length();
                    if (len < 0.01f) continue;
                    float rot = (float)Math.Atan2(delta.Y, delta.X);

                    // La altura del punto medio dentro del glifo manda el
                    // gradiente: abajo cuerpo, arriba punta pálida.
                    float localY = ((strokes[s].Y + strokes[s + 1].Y) * 0.5f + 7f) / 14f;
                    Color strokeCol = VFXPalettes.RuneStars.Glyph(1f - localY * 0.75f);

                    VFXCore.Quad(mid, strokeCol * (pulse * alpha),
                        new Vector2(len + strokeW, strokeW), rot);
                }

                // LA PERLA de la punta (la "gema" de la corona).
                float pearlPulse = 0.8f + 0.2f * (float)Math.Sin(time * 3.0f + g * 2.0f);
                Vector2 pearlPos = glyphPos + new Vector2(0f, -11.5f * scale);
                VFXCore.Quad(pearlPos, VFXPalettes.RuneStars.Pearl * (pulse * alpha),
                    new Vector2(7.0f * scale, 7.0f * scale));
                VFXCore.Quad(pearlPos, VFXPalettes.RuneStars.PearlCore * (pearlPulse * alpha),
                    new Vector2(3.2f * scale, 3.2f * scale));
            }
        }

        /// <summary>
        /// Posición mundial de la perla del glifo g del arco (las chispas
        /// ascendentes que emite la corona desde las perlas).
        /// </summary>
        public static Vector2 PerlaArcoWorld(Vector2 anchor, float scale, float time, int g)
        {
            const int GlyphCount = 8;
            float arcRadius = 26f * scale;
            Vector2 arcCenter = anchor - new Vector2(0f, 13f * scale);
            float angle = -MathHelper.Pi + MathHelper.Pi * 0.11f +
                          g / (float)(GlyphCount - 1) * MathHelper.Pi * 0.78f;
            float floatR = arcRadius + 2.4f * scale * (float)Math.Sin(time * 1.35f + g * 0.9f);
            float bobY = 2.0f * scale * (float)Math.Sin(time * 0.85f + g * 1.7f);
            return arcCenter + new Vector2(
                (float)Math.Cos(angle) * floatR,
                (float)Math.Sin(angle) * floatR + bobY) - new Vector2(0f, 11.5f * scale);
        }

        // ==================================================================
        //  COMPUESTO C — EL SELLO SOLAR (el signo mágico invocable, NUEVO)
        // ==================================================================

        /// <summary>
        /// EL SELLO SOLAR — EL SIGNO MÁGICO INVOCABLE de la casa: un círculo
        /// mágico de plano único (visto ~de frente, ligeramente inclinado)
        /// con TODO el vocabulario de la escritura solar:
        ///
        ///   · El ARO DOBLE concéntrico (exterior corpulento + interior
        ///     fino en eco, contrarrotando — la banda de un sello real).
        ///   · LAS OCHO RUNAS de pie sobre el aro exterior (el alfabeto
        ///     completo), cabalgando la tangente y girando con él.
        ///   · CUATRO NODOS CARDINALES con perla y destello de 4 puntas
        ///     (los anclajes del sello, en los ejes cardinales).
        ///   · EL GLIFO MAESTRO central (el sigilo solar R7) grande, con
        ///     su halo y su perla — la firma de la casa en el centro.
        ///   · EL POLVO rúnico orbitando todo el conjunto.
        ///
        /// Para invocaciones, portales, telegrafías de arma, suelos de
        /// ritual, altares, jefes: cualquier cosa que merezca firmarse con
        /// la escritura del sol.
        /// </summary>
        /// <param name="center">Centro del sello (mundo).</param>
        /// <param name="radius">Radio del aro exterior (px).</param>
        /// <param name="time">Tiempo animado (GlobalTimeWrappedHourly).</param>
        /// <param name="seed">Semilla determinista.</param>
        /// <param name="body">Color del cuerpo de runa (oro por defecto).</param>
        /// <param name="tip">Color de punta de runa (pálido por defecto).</param>
        /// <param name="alpha">Multiplicador de intensidad (0..1).</param>
        /// <param name="giro">Velocidad de rotación del sello (rad/s; la casa: 0.22).</param>
        /// <param name="glifoCentral">Índice del glifo central en RunasSolares (7 = sigilo maestro).</param>
        public static void SelloSolar(Vector2 center, float radius, float time, int seed,
            Color? body = null, Color? tip = null, float alpha = 1f,
            float giro = 0.22f, int glifoCentral = 7)
        {
            if (radius < 4f || alpha <= 0.02f) return;

            Color cBody = body ?? CuerpoOro;
            Color cTip = tip ?? PuntaOro;

            // El plano del sello: ligeramente inclinado (casi frontal).
            float tilt = 0.14f;
            float spin = time * giro;
            // La escala del glifo con la PROPORCIÓN DE LA CASA (la de los
            // anillos del sol: gs = R/52·~1): con 8 runas el glow trasero
            // (34·gs) queda TANGENTE a la separación angular (2πr/8) —
            // glifos densos pero legibles, sin empastarse (v6.34b: eran 12
            // runas con escala lineal al radio y los glows se solapaban 2×).
            float glyphScale = Math.Max(radius / 52f, 0.30f);

            // --- 1. EL ARO DOBLE (exterior + interior contrarrotando, BIEN
            //     separado para leerse como banda de sello real — v6.34c:
            //     0.80r fundía el interior con el exterior en pantalla) ---
            AroEliptico(center, radius, radius * 0.94f, tilt, spin, time, 0, cBody, alpha);
            AroEliptico(center, radius * 0.66f, radius * 0.62f, tilt, -spin * 1.35f,
                time, 1, cTip, alpha * 0.85f);

            // --- 2. LAS OCHO RUNAS de pie sobre el aro exterior (el
            //     alfabeto solar COMPLETO — un glifo de cada) ---
            const int RunasSello = 8;
            for (int g = 0; g < RunasSello; g++)
            {
                float ang = g / (float)RunasSello * MathHelper.TwoPi + spin;
                RunaOrbitando(center, radius, radius * 0.94f, tilt, ang,
                    time, 0, g, cBody, cTip, glyphScale, alpha);
            }

            // --- 3. LOS CUATRO NODOS CARDINALES (los anclajes del sello) ---
            for (int n = 0; n < 4; n++)
            {
                float ang = n * MathHelper.PiOver2 + spin * 0.25f;
                Vector2 pos = EllipsePoint(center, radius * 1.02f, radius * 0.96f, tilt, ang);
                NodoCardinal(pos, radius * 0.085f, time, n * 1.57f,
                    cBody, cTip, alpha * 1.05f);
            }

            // --- 4. EL GLIFO MAESTRO central (la firma de la casa) — GRANDE
            //     y con SU destello de 4 puntas (v6.34c: a ×1.55 era una
            //     elipse difusa sin forma legible) ---
            float centralScale = glyphScale * 2.3f;
            float flareC = radius * 0.52f * (0.85f + 0.15f * (float)Math.Sin(time * 2.0f));
            float wC = Math.Max(1.8f, radius * 0.022f);
            EmitCapsule(center, flareC, wC, 0f, Tint(cTip, 0.35f * alpha));
            EmitCapsule(center, flareC, wC, MathHelper.PiOver2, Tint(cTip, 0.35f * alpha));
            Runa(center, -spin * 0.30f, RunasSolares, glifoCentral,
                centralScale, time, 3f, cBody, cTip, alpha * 0.95f);

            // --- 5. EL POLVO rúnico orbitando el conjunto ---
            PolvoRunico(center, radius * 1.18f, 9, time, seed, cTip, alpha * 0.80f);
        }

        /// <summary>
        /// Posición mundial de la runa g del aro exterior del SelloSolar
        /// (para chispas/sonido/daño en la posición exacta del glifo).
        /// </summary>
        public static Vector2 RunaSelloWorld(Vector2 center, float radius, float time,
            int g, float giro = 0.22f, int total = 8)
        {
            float spin = time * giro;
            float ang = g / (float)total * MathHelper.TwoPi + spin;
            return EllipsePoint(center, radius, radius * 0.94f, 0.14f, ang);
        }
    }
}
