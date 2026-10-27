using Microsoft.Xna.Framework;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// COLORPRISMA — v6.52.2 — EL MOTOR DE COLOR DEL PRISMA
    /// (v6.54.2 — renombrado en la purga final: mismo motor, nombre de
    /// la casa).
    ///
    /// LA INVESTIGACIÓN (el jefe del video del usuario): la técnica número UNO que hace que TODO el jefe
    /// se vea como se ve es UNA SOLA COSA — un RELOJ DE COLOR COMPARTIDO
    /// que recorre una paleta de 5 colores en ciclo continuo, y TODOS
    /// los efectos del combate leen de él con DISTINTO desfase: el
    /// cuerpo, los proyectiles, los rayos, las partículas — todo vive
    /// en el MISMO arcoíris desplazándose (12 ticks por color, ciclo
    /// completo de 60 t = 1 s).
    ///
    /// LA PALETA «OIL» (el arcoíris de mancha de aceite de la referencia):
    ///   magenta (174,23,189) → naranja (237,128,60) → amarillo
    ///   (247,255,101) → verde (176,234,85) → cian (102,219,249)
    ///
    /// EL GRADIENTE A LO LARGO (la firma del rayo de fusión): ValueAt(reloj −
    /// progress·100) — el haz es UNA REBANADA MÓVIL del arcoíris: 100
    /// ticks de paleta caben a lo largo del rayo, del blanco de la boca
    /// al cian de la punta, y TODO se desplaza cada frame.
    /// </summary>
    public static class ColorPrisma
    {
        /// <summary>LA PALETA OIL (la de serie — el arcoíris de
        /// mancha de aceite: magenta → naranja → amarillo → verde → cian).</summary>
        public static readonly Color[] Oil =
        {
            new(174, 23, 189),    // magenta
            new(237, 128, 60),    // naranja
            new(247, 255, 101),   // amarillo
            new(176, 234, 85),    // verde
            new(102, 219, 249),   // cian
        };

        /// <summary>Los ticks por SEGMENTO de paleta (12 — el ciclo
        /// completo son 60 t = 1 s, el tempo de la referencia).</summary>
        public const float TicksPorSegmento = 12f;

        /// <summary>La longitud total del ciclo (60 t — la vuelta entera).</summary>
        public const float Ciclo = TicksPorSegmento * 5f;

        /// <summary>
        /// EL COLOR DEL RELOJ: la paleta recorrida en ciclo continuo con
        /// crossfade total (cada color vive 12 t y funde con el siguiente
        /// durante TODO su segmento — un ciclo PURO, sin pausas).
        /// </summary>
        /// <param name="reloj">El reloj de color (Main.GameUpdateCounter o
        /// GlobalTimeWrappedHourly — o cualquier desfase propio).</param>
        public static Color ValueAt(float reloj)
        {
            float t = reloj % Ciclo;
            if (t < 0f) t += Ciclo;
            float seg = t / TicksPorSegmento;          // 0..5 continuo
            int i = (int)seg % Oil.Length;
            int j = (i + 1) % Oil.Length;
            return Color.Lerp(Oil[i], Oil[j], seg - (int)seg);
        }

        /// <summary>
        /// EL GRADIENTE A LO LARGO (la firma del rayo de fusión): el color que
        /// vive a `progress` (0..1) de un haz cuyo reloj camina — la
        /// REBANADA MÓVIL: la boca y la punta viven 100 ticks de paleta
        /// de diferencia y TODO se desliza cada frame.
        /// </summary>
        /// <param name="reloj">El reloj compartido del efecto.</param>
        /// <param name="progress">0 (boca) → 1 (punta) a lo largo del haz.</param>
        /// <param name="spanTicks">Los ticks de paleta que abarca el largo.</param>
        public static Color ALoLargo(float reloj, float progress, float spanTicks = 100f)
            => ValueAt(reloj - progress * spanTicks);
    }
}
