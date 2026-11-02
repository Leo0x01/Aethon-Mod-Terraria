using Microsoft.Xna.Framework;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// AgujeroAbsolutoBlackHoleRenderer — EL SHIM DEL ABSOLUTO (v6.82).
    ///
    /// LA LETRA: «crea un arma nueva de agujero negro runico con lo que
    /// creas que sea bueno, este debe ser mas fuerte que los supremos
    /// asi que tiene que tener 4 anillos runicos de agujero negro».
    ///
    /// EL AGUJERO MÁS ALLÁ DE LOS SUPREMOS: TODO el motor vivo en
    /// <see cref="SupremoBlackHoleRenderer"/> (la fusión v6.80 — el disco
    /// Doppler del Umbral, el anillo de bandas del Cósmico, los brazos
    /// espirales del Olvido, el humo de la Bruma y la corona de rayos de
    /// StormLib) viste el perfil
    /// <see cref="SupremoBlackHoleRenderer.Absoluto"/> — el GRADIENTE DEL
    /// MÁS ALLÁ (violeta del vacío (140,90,255) → cian estelar
    /// (80,220,255) @0.45 → oro blanco (255,240,200)) reinando en TODO lo
    /// que se desenrolla, rim cian, blanco platino — Y LA CUARTA CORONA
    /// RÚNICA: 10 runas blanco-cian a 3,95·R girando lentas en
    /// contrarroto, MÁS AFUERA que cualquier círculo de cualquier
    /// Supremo (la firma inconfundible del Absoluto — el único agujero
    /// del mod con CUATRO anillos rúnicos).
    ///
    /// Este archivo es la FACHADA del proyectil (el patrón del shim
    /// Aurora, v6.80): SpherePx y Draw(center, scale, time, seed)
    /// delegan 1:1 en el motor con SU perfil. Nada más vive aquí.
    ///
    /// CONTRATO DE BATCH (el del motor): Draw() exige el SpriteBatch
    /// CERRADO y lo deja CERRADO.
    /// </summary>
    public static class AgujeroAbsolutoBlackHoleRenderer
    {
        /// <summary>El radio del motor (la esfera ES la misma física).</summary>
        public const float SpherePx = SupremoBlackHoleRenderer.SpherePx;

        /// <summary>
        /// EL DRAW DEL ABSOLUTO — el motor completo con
        /// <see cref="SupremoBlackHoleRenderer.Absoluto"/> (firma del
        /// patrón del shim Aurora).
        /// </summary>
        public static void Draw(Vector2 center, float scale, float time, int seed)
            => SupremoBlackHoleRenderer.Draw(center, scale, time, seed,
                SupremoBlackHoleRenderer.Absoluto);
    }
}
