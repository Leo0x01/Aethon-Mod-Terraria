using Microsoft.Xna.Framework;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// RuneCrownRenderer — v6.34 — LA CORONA RÚNICA ESTELAR (facade de
    /// SIGILOLIB).
    ///
    /// El arco de OCHO GLIFOS DEL PORTADOR flotando sobre la cabeza — cada
    /// glifo un trazo angular distinto con su PERLA rosa pálido — nació
    /// aquí (v6.03) y ahora VIVE en la librería de signos mágicos del
    /// sol: SigiloLib.ArcoGloria. Este renderer conserva el nombre
    /// histórico y el contrato (los DrawLayers y CosmeticPlayer lo
    /// consumen) y delega 1:1 — ni un número cambiado.
    ///
    /// Uso (biblioteca): VFXCore.Begin() → ComputeQuads(...) →
    /// VFXCore.AppendToPlayerDraw(...) o VFXCore.FlushAdditive(...).
    /// </summary>
    public static class RuneCrownRenderer
    {
        /// <summary>Número de glifos del arco (la ley de SigiloLib).</summary>
        public const int GlyphCount = 8;

        /// <summary>
        /// Calcula el arco rúnico completo como cuadros de luz en el buffer
        /// de VFXCore (coordenadas de MUNDO) — delega en SigiloLib.ArcoGloria.
        /// </summary>
        /// <param name="anchor">Centro de la cabeza sobre la que flota.</param>
        /// <param name="scale">Escala del conjunto (para una cabeza humana
        /// ≈ 1; derivar de player.height / 42f para crecer con el sprite).</param>
        /// <param name="time">Tiempo animado (Main.GlobalTimeWrappedHourly).</param>
        /// <param name="alpha">Multiplicador global de intensidad (0..1).</param>
        public static void ComputeQuads(Vector2 anchor, float scale, float time, float alpha = 1f)
        {
            SigiloLib.ArcoGloria(anchor, scale, time, alpha);
        }

        /// <summary>
        /// Posición mundial de la punta del glifo g (para las chispas
        /// ascendentes que emite la corona desde las perlas) — SigiloLib.
        /// </summary>
        public static Vector2 GetPearlPosition(Vector2 anchor, float scale, float time, int g)
        {
            return SigiloLib.PerlaArcoWorld(anchor, scale, time, g);
        }
    }
}
