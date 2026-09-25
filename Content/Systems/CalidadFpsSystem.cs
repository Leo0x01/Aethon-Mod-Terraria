using AethonMod.Content.VFX;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// CalidadFpsSystem — v6.34 — EL ALIMENTADOR DEL PRESUPUESTO ADAPTATIVO.
    ///
    /// La mejora v6.34 de VFXCore (ReportarFps / FactorCalidad) nació
    /// dormida: nadie le daba de comer. Este sistema es SU corazón —
    /// cada tick de juego le susurra los FPS reales al VFXCore, y la
    /// calidad del mod RESPIRA sola:
    ///
    ///   · FPS &lt; 45 sostenidos → el presupuesto de quads adelgaza
    ///     (−0.05 por reporte, suelo 0.5): menos partículas, misma alma.
    ///   · FPS &gt; 55 → recupera lento (+0.02 por reporte, techo 1):
    ///     la abundancia vuelve cuando el mundo puede pagarla.
    ///
    /// El contador de FPS del propio juego (Main.frameRate, refrescado
    /// ~1 vez por segundo) es la fuente — cero medidores propios, cero
    /// estado nuevo, cero configuración: la casa se adapta a la máquina
    /// de cada jugador sin que nadie toque un slider.
    /// </summary>
    public class CalidadFpsSystem : ModSystem
    {
        public override void PostUpdateEverything()
        {
            // Solo el dueño del mundo reporta (en cliente dedicado no hay
            // render que proteger, y en MP el cliente ya reporta el suyo).
            if (Main.dedServ) return;
            VFXCore.ReportarFps(Main.frameRate);
        }
    }
}
