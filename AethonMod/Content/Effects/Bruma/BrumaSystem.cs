using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.Effects.Bruma
{
    /// <summary>
    /// BrumaSystem — v6.17 — EL CICLO DE VIDA DE LA LIBRERÍA DE BRUMA.
    ///
    /// La librería de humo/niebla/bruma procedural hornea sus pinceles
    /// (Texture2D nacidas de código) EN RUNTIME, la primera vez que se
    /// dibujan. Este ModSystem garantiza que, al recargar o descargar el
    /// mod, TODAS esas texturas se disponeN — cero fugas de VRAM entre
    /// sesiones (BrumaBrushes.Unload).
    ///
    /// La librería en sí es INERTE: no dibuja nada por su cuenta; la usa
    /// quien la necesite (p. ej. el Agujero Negro de la Bruma, v6.17)
    /// desde su propio renderer con SU lote y SU contrato de batch.
    /// </summary>
    public class BrumaSystem : ModSystem
    {
        public override void Unload()
        {
            // Disposición de TODAS las texturas horneadas en runtime.
            BrumaBrushes.Unload();
        }
    }
}
