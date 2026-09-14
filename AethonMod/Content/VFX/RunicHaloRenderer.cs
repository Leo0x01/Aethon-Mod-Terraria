using System;
using Microsoft.Xna.Framework;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// RunicHaloRenderer — v6.26 — EL ANILLO RÚNICO ESTELAR:
    /// LOS ANILLOS DE LOS SOLES, LITERALES, EN LA ESPALDA.
    ///
    /// Petición original v6.22: "un anillo rúnico en la espalda que
    /// funcione como alas y halo; cuando el jugador va a volar, este
    /// anillo brilla con intensidad".
    ///
    /// v6.25 dibujaba círculos de los AGUJEROS (copia a mano). v6.26
    /// cumple la orden del usuario: "tienen que ser creados por códigos
    /// y tienen que COPIAR LOS ANILLOS DE LOS SOLES". ESTE renderer YA
    /// NO TIENE geometría propia: ES una llamada al EMISOR COMPARTIDO
    /// de RuneSunRenderer (EmitRingSystem, tier 3) — el MISMO código
    /// que viste al Sol Rúnico III en el mundo: TRES anillos en planos
    /// orbitales propios con GIROS ALTERNOS, runas cabalgando la
    /// tangente, perlas y latidos — los alphas de los soles, tal cual,
    /// a escala de espalda. Si el sol cambia, el ala cambia CON él.
    ///
    /// LA INTENSIDAD VIVE (contenida al lenguaje de los soles): `flight`
    /// (0..1) es la energía de vuelo — al VOLAR los anillos se avivan
    /// (×0.80..1.20), quietos reposan un pelo por debajo del sol. Cero
    /// bloom añadido: se lee AL MISMO brillo que en los soles.
    /// </summary>
    public static class RunicHaloRenderer
    {
        /// <summary>
        /// El estelar ES el Sol III: TRES anillos rúnicos (tier 3 del
        /// emisor compartido — la copia tercera del sol, con chispas y
        /// destellos de 4 puntas latiendo con el vuelo).
        /// </summary>
        public const int Tier = 3;

        /// <summary>
        /// La base del sistema (px): el radio al que el emisor planta el
        /// primer aro (los semiejes quedan a 1.62/2.06/2.50× esto — el
        /// abrazo de la espalda y el gran halo exterior).
        /// </summary>
        private const float BaseR = 17f;

        /// <summary>Semilla fija del estelar (latidos estables).</summary>
        private const int HaloSeed = 9292;

        /// <summary>
        /// Calcula LOS ANILLOS DE LOS SOLES (tier 3, LITERAL) como cuadros
        /// de luz en el buffer de VFXCore (coordenadas de MUNDO).
        /// </summary>
        /// <param name="back">Ancla: la ESPALDA ALTA del jugador (omóplatos).</param>
        /// <param name="scale">Escala (humano ≈ 1).</param>
        /// <param name="time">Tiempo animado.</param>
        /// <param name="flight">LA ENERGÍA DE VUELO (0..1) — el avivo.</param>
        /// <param name="alpha">Multiplicador global (luz del mundo).</param>
        public static void ComputeQuads(Vector2 back, float scale, float time,
            float flight, float alpha = 1f)
        {
            if (scale <= 0.05f || alpha <= 0.02f) return;

            // LA INTENSIDAD CONTENIDA AL LENGUAJE DE LOS SOLES: quieto un
            // pelo por debajo; volando un pelo por encima — NUNCA bloom.
            float glow = MathHelper.Clamp(0.80f + 0.40f * flight, 0f, 1.25f);

            // LOS ANILLOS DE LOS SOLES — sin geometría propia: el emisor
            // compartido de la familia, tal cual, en la espalda.
            RuneSunRenderer.EmitRingSystem(back, BaseR * scale, time, HaloSeed,
                Tier, 0f, 0f, glow * alpha);
        }

        /// <summary>Posición mundial del glifo g del sistema (las chispas).</summary>
        public static Vector2 GetGlyphPosition(Vector2 back, float scale, float time, int g)
        {
            // Mapa acumulativo de los TRES anillos del Sol III (6/8/10).
            int k = 0, idx = g;
            for (int i = 0; i < Tier && idx >= RuneSunRenderer.RunesOfRing(i); i++)
            {
                idx -= RuneSunRenderer.RunesOfRing(i);
                k++;
            }
            k = Math.Min(k, Tier - 1);

            // La MISMA matemática del sol (anillo k del tier 3).
            return RuneSunRenderer.RingGlyphWorld(back, BaseR * scale, time, Tier, k, idx);
        }

        /// <summary>Glifos totales del sistema (las chispas de vuelo).</summary>
        public static int Glyphs
        {
            get
            {
                int total = 0;
                for (int k = 0; k < Tier; k++) total += RuneSunRenderer.RunesOfRing(k);
                return total;
            }
        }
    }
}
