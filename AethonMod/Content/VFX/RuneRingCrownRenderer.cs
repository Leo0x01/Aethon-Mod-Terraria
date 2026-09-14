using Microsoft.Xna.Framework;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// RuneRingCrownRenderer — v6.26 — LA CORONA DE ANILLOS RÚNICOS:
    /// EL ANILLO DEL SOL RÚNICO I, LITERAL, SOBRE LA CABEZA.
    ///
    /// v6.25 dibujaba una COPIA aproximada (constantes trasladadas a
    /// mano). v6.26 cumple la orden del usuario de verdad: "tienen que
    /// ser creados por códigos y tienen que COPIAR LOS ANILLOS DE LOS
    /// SOLES". Esta corona YA NO TIENE geometría propia: ES una llamada
    /// al EMISOR COMPARTIDO de RuneSunRenderer (EmitRingSystem, tier 1)
    /// — el MISMO código que dibuja el anillo del Sol Rúnico I en el
    /// mundo. Mismo aro de cápsulas con profundidad, mismas 6 runas
    /// cabalgando la tangente, mismas perlas, mismos latidos y los
    /// MISMOS ALPHAS — a escala de aureola. Si el sol cambia, la
    /// corona cambia CON él.
    ///
    /// Uso (biblioteca): VFXCore.Begin() → ComputeQuads(...) →
    /// VFXCore.AppendToPlayerDraw(...) (el camino oficial de tML).
    /// </summary>
    public static class RuneRingCrownRenderer
    {
        /// <summary>
        /// La corona ES el Sol I: un solo anillo rúnico (tier 1 del
        /// emisor compartido — la copia primera del sol).
        /// </summary>
        public const int Tier = 1;

        /// <summary>
        /// La base del anillo (px): el radio al que el emisor dibuja el
        /// aro (el semieje mayor queda a 1.62× esto — la aureola abraza
        /// la cabeza con aire).
        /// </summary>
        private const float BaseR = 15f;

        /// <summary>Semilla fija de la corona (latidos estables).</summary>
        private const int CrownSeed = 4321;

        /// <summary>
        /// Calcula LA AUREOLA (el anillo del Sol I, LITERAL) como cuadros
        /// de luz en el buffer de VFXCore (coordenadas de MUNDO).
        /// </summary>
        /// <param name="head">Centro de la cabeza que ringea la aureola.</param>
        /// <param name="scale">Escala del conjunto (humano ≈ 1).</param>
        /// <param name="time">Tiempo animado (Main.GlobalTimeWrappedHourly).</param>
        /// <param name="alpha">Multiplicador global de intensidad (0..1).</param>
        public static void ComputeQuads(Vector2 head, float scale, float time, float alpha = 1f)
        {
            if (scale <= 0.05f || alpha <= 0.02f) return;

            // EL ANILLO DEL SOL I — sin geometría propia: el emisor
            // compartido de la familia de los soles, tal cual, en la cabeza.
            RuneSunRenderer.EmitRingSystem(head, BaseR * scale, time, CrownSeed,
                Tier, 0f, 0f, alpha);
        }

        /// <summary>Posición mundial del glifo g de la aureola (las chispas).</summary>
        public static Vector2 GetGlyphPosition(Vector2 head, float scale, float time, int g)
        {
            // La MISMA matemática del sol (anillo 0 del tier 1).
            return RuneSunRenderer.RingGlyphWorld(head, BaseR * scale, time, Tier, 0, g);
        }

        /// <summary>Glifos de la aureola (= los del anillo del Sol I).</summary>
        public static int Glyphs => RuneSunRenderer.RunesOfRing(0);
    }
}
