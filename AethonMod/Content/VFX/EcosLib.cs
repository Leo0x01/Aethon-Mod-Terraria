using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// EcosLib — v6.41 — LA LIBRERÍA DE LOS FANTASMAS.
    ///
    /// EL AGUJERO que la casa tenía (el análisis v6.41 del arsenal visual):
    /// TODO efecto premium del ecosistema vive de LOS ECOS — el cuerpo que
    /// se repite en su pasado (el dash con estela de clones, la espada que
    /// deja copias de sí misma en el arco, el jefe con espejitos orbitando)
    /// — y cada arma nuestra se construía su bucle a mano. Esta librería
    /// reúne los DOS modos de fantasma en un contrato común:
    ///
    ///   · LA MEMORIA (modo historia): un búfer circular de posiciones y
    ///     ángulos del cuerpo — `Registrar` cada tick, `Pasado` para
    ///     muestrear hace N ticks. Sobre él, `ColaHistoria` compone la
    ///     fila de fantasmas decrecientes (el rastro del dash, la estela
    ///     del dardo) sin aloca nada.
    ///
    ///   · EL ESPEJO (modo orbital): clones que ORBITAN y RESPIRAN alrededor
    ///     del cuerpo — `EcoOrbital` + `PulsoTriangular`: el eco clásico de
    ///     las singularidades vivas (un anillo interior tenue + un anillo
    ///     exterior cálido girando a distinta velocidad, con un pulso
    ///     triangular de 4 segundos que los acerca y aleja).
    ///
    /// CONTRATO DE LOTE (la casa): `ColaHistoria` emite quads al búfer de
    /// VFXCore (mundo → el llamador vuelca con FlushAdditive); los modos
    /// orbitales son MATEMÁTICA PURA (el llamador compone su cuerpo en la
    /// posición devuelta con su propio compositor). CERO Main.rand en el
    /// render: todo por reloj y hash.
    ///
    /// ROBUSTEZ MP: la memoria vive en el ModProjectile (campos de
    /// instancia — cada cliente la reconstruye por su cuenta con el MISMO
    /// reloj: determinista por construcción).
    /// </summary>
    public static class EcosLib
    {
        // ==================================================================
        //  LA MEMORIA — el búfer circular de posiciones del cuerpo
        // ==================================================================

        /// <summary>
        /// El búfer circular: guarda las últimas <see cref="Slots"/> posiciones
        /// y ángulos del cuerpo (un registro por tick de IA). Es un struct con
        /// arrays: los arrays se piden UNA vez en el constructor de la entidad
        /// (cero GC por frame), el struct se guarda como campo del proyectil.
        /// </summary>
        public struct Memoria
        {
            /// <summary>Las posiciones registradas (búfer circular).</summary>
            public Vector2[] Pos;
            /// <summary>El ángulo del cuerpo en cada registro.</summary>
            public float[] Ang;
            /// <summary>Índice del registro MÁS RECIENTE.</summary>
            public int Cabeza;
            /// <summary>Cuántos registros hay (techo <see cref="Slots"/>).</summary>
            public int N;
        }

        /// <summary>Capacidad de la memoria (32 ticks de historia — medio segundo).</summary>
        public const int Slots = 32;

        /// <summary>Crea una memoria vacía (los arrays viven mientras el cuerpo).</summary>
        public static Memoria Crear()
        {
            return new Memoria { Pos = new Vector2[Slots], Ang = new float[Slots], Cabeza = -1, N = 0 };
        }

        /// <summary>
        /// Registra la posición y ángulo ACTUALES del cuerpo (un tick, una
        /// llamada — llamarla desde la IA al FINAL del movimiento).
        /// </summary>
        public static void Registrar(ref Memoria m, Vector2 posicion, float angulo)
        {
            m.Cabeza = (m.Cabeza + 1) % Slots;
            m.Pos[m.Cabeza] = posicion;
            m.Ang[m.Cabeza] = angulo;
            if (m.N < Slots) m.N++;
        }

        /// <summary>
        /// La posición del cuerpo hace <paramref name="ticks"/> (0 = ahora).
        /// Si la memoria no llega tan atrás, clampa al registro más viejo.
        /// </summary>
        public static Vector2 Pasado(in Memoria m, int ticks)
        {
            if (m.N == 0) return Vector2.Zero;
            ticks = Math.Clamp(ticks, 0, m.N - 1);
            int idx = (m.Cabeza - ticks + Slots * 8) % Slots;
            return m.Pos[idx];
        }

        /// <summary>El ángulo del cuerpo hace <paramref name="ticks"/> (0 = ahora).</summary>
        public static float PasadoAng(in Memoria m, int ticks)
        {
            if (m.N == 0) return 0f;
            ticks = Math.Clamp(ticks, 0, m.N - 1);
            int idx = (m.Cabeza - ticks + Slots * 8) % Slots;
            return m.Ang[idx];
        }

        /// <summary>Cuántos ticks de historia hay (diagnóstico / composición).</summary>
        public static int Profundidad(in Memoria m) => m.N;

        // ==================================================================
        //  EL ESPEJO — los clones orbitales que respiran
        // ==================================================================

        /// <summary>
        /// EL PULSO TRIANGULAR (ciclo 4 s): el reloj de los espejos — sube
        /// despacio y baja despacio (0.5 → 1 → 0.5), sin el latido seco del
        /// seno. Es el tempo de las singularidades vivas.
        /// </summary>
        public static float PulsoTriangular(float tiempo)
        {
            float t = tiempo % 4f;
            t /= 2f;
            if (t >= 1f) t = 2f - t;
            return t * 0.5f + 0.5f;
        }

        /// <summary>
        /// La posición del eco orbital con fase <paramref name="fase"/>: un
        /// anillo de clones girando alrededor del cuerpo — la fase avanza
        /// con el reloj de media vuelta + el tiempo propio
        /// (inconmensurables: el anillo NUNCA se cierra en patrón repetido).
        /// El radio lo modula el llamador con
        /// <see cref="PulsoTriangular"/> (los espejos respiran).
        /// </summary>
        /// <param name="centro">El centro del cuerpo (coords de MUNDO).</param>
        /// <param name="tiempo">El reloj (Main.GlobalTimeWrappedHourly + la edad propia).</param>
        /// <param name="fase">La fase del eco en el anillo (0..1 — pasos
        /// de 0.25 para 4 clones, 0.34 para 3: nunca divisor exacto).</param>
        /// <param name="radio">El radio del anillo en px (ya modulado).</param>
        public static Vector2 EcoOrbital(Vector2 centro, float tiempo, float fase, float radio)
        {
            float rad = (fase + tiempo) * MathHelper.TwoPi;
            return centro + new Vector2(0f, radio).RotatedBy(rad);
        }

        /// <summary>
        /// EL TEMPO DE LOS ESPEJOS: la fase que usan los anillos de clones —
        /// media vuelta por segundo + la deriva del propio cuerpo (0.04/tick):
        /// dos anillos a radios distintos giran a velocidades DIFERENTES y
        /// jamás sincronizan.
        /// </summary>
        public static float TempoEspejos(float tiempoGlobal, float edad)
        {
            return tiempoGlobal * 0.5f + edad * 0.04f;
        }

        // ==================================================================
        //  LA COMPOSICIÓN — la fila de fantasmas del pasado
        // ==================================================================

        /// <summary>
        /// LA COLA DE HISTORIA: dibuja <paramref name="numero"/> fantasmas del
        /// cuerpo en sus posiciones pasadas (uno cada <paramref name="cada"/>
        /// ticks), con el tinte ENFRIÁNDOSE y la escala MURIÉNDOSE con la edad
        /// (el fantasma recién nacido casi tan brillante como el cuerpo; el
        /// más viejo, un susurro). Emite quads de VFXCore: el llamador abre
        /// SU lote y vuelca (FlushAdditive) — el contrato de la casa.
        /// </summary>
        /// <param name="m">La memoria del cuerpo.</param>
        /// <param name="cada">Ticks entre fantasma y fantasma (2-4 recomendado).</param>
        /// <param name="numero">Cuántos fantasmas (4-8 recomendado).</param>
        /// <param name="tinte">El color del cuerpo vivo.</param>
        /// <param name="ancho">El tamaño del cuerpo vivo (px).</param>
        /// <param name="alfaVivo">El alfa del fantasma recién nacido (0.25-0.5).</param>
        /// <param name="estirarX">Multiplicador de estirado a lo largo del movimiento.</param>
        public static void ColaHistoria(ref Memoria m, int cada, int numero,
            Color tinte, Vector2 tamano, float alfaVivo, float estirarX = 1f)
        {
            if (m.N == 0) return;

            for (int k = 1; k <= numero; k++)
            {
                int ticks = k * cada;
                if (ticks > m.N - 1) break;

                float f = 1f - k / (float)(numero + 1);   // 1 → 0 con la edad

                Vector2 pos = Pasado(m, ticks);
                float ang = PasadoAng(m, ticks);

                // La distancia al anterior estira el fantasma a lo largo del
                // vuelo (la estela se ALARGA al acelerar — física leída).
                Vector2 prev = Pasado(m, Math.Max(0, ticks - cada));
                float vel = Vector2.Distance(pos, prev);
                float estira = 1f + Math.Min(vel * 0.05f, 1.6f) * estirarX;

                Color c = tinte * (alfaVivo * f * f);      // enfriado cuadrático
                if (c.A == 0) continue;

                VFXCore.Quad(pos, c, new Vector2(tamano.X * estira, tamano.Y) * (0.55f + 0.45f * f), ang);
            }
        }
    }
}
