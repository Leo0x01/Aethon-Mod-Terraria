using Microsoft.Xna.Framework;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// SupremoAuroraBlackHoleRenderer — EL SHIM DEL AURORA (v6.80).
    ///
    /// LA HISTORIA: este archivo fue durante años un CLON ENTERO del
    /// SupremoBlackHoleRenderer — ~671 líneas de código de las que el 79%
    /// (~530) eran byte-idénticas; el propio header de la v6.31 confesaba
    /// «SOLO cambia el COLOR». La auditoría v6.78 lo marcó como LA FUSIÓN
    /// nº1 del mod (la más rentable y segura de todas), y la v6.80 la
    /// ejecutó: TODO el motor vive HOY en SupremoBlackHoleRenderer,
    /// parametrizado por <see cref="PerfilSupremo"/> — y la identidad del
    /// Aurora son 39 NÚMEROS: el perfil
    /// <see cref="SupremoBlackHoleRenderer.Aurora"/> (el GRADIENTE AURORA
    /// de 3 paradas — morado (185,105,255) → azul (92,150,255) @0.45 →
    /// dorado (255,195,90) — reinando en brazos, banda radial, jets y
    /// ondas; rim morado; runas azul/oro/morado; blanco frío).
    ///
    /// Este archivo conserva la FACHADA histórica (los 6 callsites del
    /// SupremoAuroraBlackHoleProjectile no se tocaron): SpherePx y
    /// Draw(center, scale, time, seed) delegan 1:1 en el motor con SU
    /// perfil. Nada más vive aquí — la duplicación murió.
    ///
    /// CONTRATO DE BATCH (el del motor): Draw() exige el SpriteBatch
    /// CERRADO y lo deja CERRADO.
    /// </summary>
    public static class SupremoAuroraBlackHoleRenderer
    {
        /// <summary>El radio del motor (la esfera ES la misma física).</summary>
        public const float SpherePx = SupremoBlackHoleRenderer.SpherePx;

        /// <summary>
        /// EL DRAW DEL AURORA — el motor completo con
        /// <see cref="SupremoBlackHoleRenderer.Aurora"/> (firma histórica
        /// intacta).
        /// </summary>
        public static void Draw(Vector2 center, float scale, float time, int seed)
            => SupremoBlackHoleRenderer.Draw(center, scale, time, seed,
                SupremoBlackHoleRenderer.Aurora);
    }
}
