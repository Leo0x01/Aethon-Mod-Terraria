using Terraria;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Effects.Bruma
{
    /// <summary>
    /// BrumaSystem — v6.25 — EL CICLO DE VIDA DE LAS LIBRERÍAS DE VFX.
    ///
    /// La librería de humo/niebla/bruma procedural hornea sus pinceles
    /// (Texture2D nacidas de código) EN RUNTIME, la primera vez que se
    /// dibujan. Este ModSystem garantiza que, al recargar o descargar el
    /// mod, TODAS esas texturas se disponeN — cero fugas de VRAM entre
    /// sesiones (BrumaBrushes.Unload).
    ///
    /// v6.25: también vacía el estado estático de las librerías nuevas
    /// (los campos de brasas de PyraLib y los tracks de camino de
    /// EstelaLib) — la descarga queda LIMPIA de verdad.
    ///
    /// Las librerías en sí son INERTES: no dibujan nada por su cuenta;
    /// las usa quien las necesite desde su propio renderer con SU lote
    /// y SU contrato de batch.
    /// </summary>
    public class BrumaSystem : ModSystem
    {
        public override void Unload()
        {
            // Disposición de TODAS las texturas horneadas en runtime.
            BrumaBrushes.Unload();

            // Estado estático de las librerías v6.25.
            PyraLib.ClearFields();
            EstelaLib.ClearTracks();
        }
    }
}
