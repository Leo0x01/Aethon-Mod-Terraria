using Microsoft.Xna.Framework;
using Terraria;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// AnilloDorsalRenderer — v6.37 — EL CÍRCULO RÚNICO DEL AGUJERO NEGRO
    /// EN LA ESPALDA (RECONSTRUIDO sobre la librería corregida).
    ///
    /// La aclaración del usuario, cumplida en la LIBRERÍA y en la ESPALDA:
    /// "cuando te pedí una librería para los anillos de los agujeros, me
    /// refería a los ANILLOS RÚNICOS" — este renderer ya NO inventa su
    /// propio anillo estilizado: dibuja EL CÍRCULO DE CONJURO LITERAL de
    /// los vórtices, invocado de OrbitaLib:
    ///   · LA TRIPLE CORONA DEL SUPREMO (OrbitaLib.CoronaConjuro): el
    ///     círculo DORADO de 8 runas girando con el conjunto, el círculo
    ///     VIOLETA de 6 runas contrarrotando más afuera (el contrarroto
    ///     arcano) y el círculo BLANCO íntimo de 6 rápido — las runas DE
    ///     PIE (el radio respira, el glifo se mece) con sus resplandores
    ///     y sus PERLAS latiendo encima, la técnica exacta 1:1.
    ///   · EL ANILLO DE FOTONES interior: el aro fino del horizonte
    ///     contrarrotando entre el cuerpo del portador y el círculo
    ///     blanco (la firma de los agujeros negros).
    /// La escala de glifo ×2.4: a escala humana las runas SE LEAN.
    ///
    /// CONTRATO (el de SelloVacio): abre y CIERRA su propio batch — el
    /// SpriteBatch del llamador debe estar CERRADO al llamar (y queda
    /// CERRADO al salir). Por eso el dibujado vive en el proyectil halo
    /// AnilloRunicoDorsalHalo (el patrón a prueba de balas): vanilla
    /// dibuja los proyectiles ANTES que los jugadores → la corona queda
    /// DETRÁS del cuerpo del portador: LA ESPALDA.
    /// </summary>
    public static class AnilloDorsalRenderer
    {
        /// <summary>
        /// La escala de la espalda: la corona externa (3.30×R) abraza al
        /// jugador — el aro violeta llega a ~1.15·la altura del cuerpo.
        /// </summary>
        public const float Radio = 0.348f;   // ×altura del sprite

        /// <summary>Las runas SE LEEN a escala humana (×2.4 del calibre).</summary>
        public const float GlifoMul = 2.4f;

        /// <summary>
        /// Posición mundial de la runa g del CÍRCULO DORADO (para las
        /// chispas que escapan de la escritura — el patrón de las coronas).
        /// </summary>
        /// <param name="espalda">El centro de la espalda (mundo).</param>
        /// <param name="altura">La altura del sprite del jugador (px).</param>
        /// <param name="time">Tiempo animado.</param>
        /// <param name="g">Índice de la runa (0..RunasMedias−1).</param>
        public static Vector2 RunaWorld(Vector2 espalda, float altura, float time, int g)
        {
            float r = altura * Radio;
            float gs = System.Math.Max(r / OrbitaLib.GlifoCalibre, 0.25f) *
                       OrbitaLib.GlifoMul * GlifoMul;
            float ang = g / (float)OrbitaLib.RunasMedias * MathHelper.TwoPi +
                        time * OrbitaLib.GiroMedio;
            float floatR = OrbitaLib.AroMedio * r +
                           2.4f * gs * (float)System.Math.Sin(time * 1.35f + g * 0.9f);
            float bobY = 2.0f * gs * (float)System.Math.Sin(time * 0.85f + g * 1.7f);
            return espalda + new Vector2(
                (float)System.Math.Cos(ang) * floatR,
                (float)System.Math.Sin(ang) * floatR + bobY);
        }

        /// <summary>
        /// Dibuja LA CORONA DE CONJURO DEL VACÍO colgada de la espalda
        /// (la triple corona rúnica LITERAL + el anillo de fotones).
        /// CONTRATO: batch CERRADO al entrar → CERRADO al salir.
        /// </summary>
        /// <param name="espalda">El centro de la espalda (coords de MUNDO — el renderer resta screenPosition).</param>
        /// <param name="altura">La altura del sprite del jugador (px).</param>
        /// <param name="time">Tiempo animado (Main.GlobalTimeWrappedHourly).</param>
        /// <param name="alpha">Multiplicador global de intensidad (0..1).</param>
        public static void Draw(Vector2 espalda, float altura, float time, float alpha = 1f)
        {
            if (alpha <= 0.02f || altura < 8f) return;

            // La corona vive en coords de PANTALLA (el contrato de OrbitaLib).
            Vector2 centro = espalda - Main.screenPosition;
            float r = altura * Radio;

            try
            {
                OrbitaLib.AbrirAdditive();

                // --- 1. EL ANILLO DE FOTONES interior (la firma del
                //     horizonte: el aro fino contrarrotando entre el
                //     cuerpo del portador y el círculo blanco íntimo). ---
                float fotones = 0.30f + 0.12f * (float)System.Math.Sin(time * 1.7f);
                OrbitaLib.AnilloFino(centro, 1.35f * r, -time * 0.06f,
                    OrbitaLib.Tint(OrbitaLib.RunaBlanca, fotones * alpha));

                // --- 2. LA TRIPLE CORONA DE CONJURO (la LITERAL del
                //     Supremo, de la librería): dorado de 8 + violeta de
                //     6 contrarrotando + blanco íntimo de 6 rápido, las
                //     runas DE PIE con sus perlas. ---
                OrbitaLib.CoronaConjuro(centro, r, time,
                    alphaMul: alpha, glyphMul: GlifoMul);

                OrbitaLib.CerrarBatch();
                // El batch queda CERRADO (el contrato).
            }
            catch
            {
                // Cierre defensivo SOLO en el path de error.
                VFXCore.CerrarLoteSiAbierto();
            }
        }
    }
}
