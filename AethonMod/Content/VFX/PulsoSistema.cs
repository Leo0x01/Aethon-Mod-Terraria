using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// PulsoSistema — v6.49 — EL CORAZÓN QUE FALTABA (hallazgo nº1 de la
    /// auditoría AUD-A, el más grave de las primitivas).
    ///
    /// PulsoLib.ActualizarPantalla() NO TENÍA NINGÚN CALL-SITE en todo el
    /// mod: el trauma de PulsoLib.Trauma() (llamado por las sierpes al
    /// golpear) se ACUMULABA para siempre sin latir, y los 6 empujes de
    /// EmpujarPantalla() (Apuestas, Verbo Primordial) escribían un estado
    /// de flash que NADIE leía — el sistema de pantalla/trauma estaba
    /// DESCONECTADO de su propio corazón.
    ///
    /// ESTE sistema lo enchufa: un latido por frame (el reloj del trauma,
    /// el decay del flash) y la higiene de las estáticas entre mundos
    /// (_trauma/_fuerzaPantalla/_tintePantalla morían con el mundo —
    /// ahora también).
    ///
    /// SP y MP-cliente (en servidor no hay pantalla que sacudir: los
    /// guards internos de PulsoLib ya lo respetan — esto es la correa
    /// de transmisión, no el motor).
    /// </summary>
    public class PulsoSistema : ModSystem
    {
        public override void PostUpdateWorld()
        {
            if (Main.netMode == NetmodeID.Server || Main.gameMenu) return;
            try { PulsoLib.ActualizarPantalla(); }
            catch { }
        }

        public override void OnWorldUnload()
        {
            try { PulsoLib.Reset(); } catch { }
        }

        public override void Unload()
        {
            try { PulsoLib.Reset(); } catch { }
        }
    }
}
