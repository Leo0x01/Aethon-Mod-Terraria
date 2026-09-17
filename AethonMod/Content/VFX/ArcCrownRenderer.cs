using Microsoft.Xna.Framework;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// ArcCrownRenderer — v6.34 — LA CORONA DE ARCOS DE NEÓN (facade de
    /// ORBITALIB).
    ///
    /// La corona original del agujero negro carmesí (v6.02), promovida a
    /// MIEMBRO DE LA BIBLIOTECA VISUAL (v6.03) y ahora VIVIENDO en la
    /// librería de signos mágicos del vacío: OrbitaLib.CoronaArcos. Este
    /// renderer conserva el nombre histórico y el contrato (VoidCrownDrawLayer
    /// lo consume) y delega 1:1 — ni un número cambiado: cinco lazos de neón
    /// con asimetría por lazo, eco interior y NUDOS con destello de 4 puntas.
    ///
    /// Uso (biblioteca): VFXCore.Begin() → ComputeQuads(...) →
    /// VFXCore.AppendToPlayerDraw(...) o VFXCore.FlushAdditive(...).
    /// </summary>
    public static class ArcCrownRenderer
    {
        /// <summary>Número de lazos de la corona (la ley de OrbitaLib).</summary>
        public const int Loops = OrbitaLib.LazosCorona;

        /// <summary>
        /// Calcula TODA la corona como cuadros de luz en el buffer de
        /// VFXCore (coordenadas de MUNDO) — delega en OrbitaLib.CoronaArcos.
        /// </summary>
        /// <param name="center">Centro de la cabeza que corona (mundo).</param>
        /// <param name="horizonPx">Radio del "horizonte" sobre el que se
        /// arquean los lazos (cabeza humana ≈ 0.55× su ancho).</param>
        /// <param name="time">Tiempo animado (GlobalTimeWrappedHourly).</param>
        /// <param name="alpha">Multiplicador global de intensidad (0..1).</param>
        public static void ComputeQuads(Vector2 center, float horizonPx, float time, float alpha = 1f)
        {
            OrbitaLib.CoronaArcos(center, horizonPx, time, alpha);
        }
    }
}
