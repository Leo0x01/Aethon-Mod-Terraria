using Microsoft.Xna.Framework;

namespace AethonMod.Content.Particles
{
    /// <summary>
    /// Rectángulo visible de la cámara en coordenadas del mundo.
    /// Se usa para frustum culling: no dibujar partículas fuera de pantalla.
    ///
    /// </summary>
    public readonly struct CameraBounds
    {
        public Vector2 TopLeft { get; }
        public Vector2 Size { get; }
        public Vector2 BottomRight => TopLeft + Size;

        public CameraBounds(Vector2 topLeft, Vector2 size)
        {
            TopLeft = topLeft;
            Size = size;
        }

        /// <summary>
        /// Verifica si una posición en mundo está dentro del área visible de la cámara.
        /// Incluye un margen para partículas grandes que pueden estar parcialmente fuera.
        /// </summary>
        public bool IsVisible(Vector2 worldPos, float margin = 64f)
        {
            return worldPos.X >= TopLeft.X - margin &&
                   worldPos.X <= BottomRight.X + margin &&
                   worldPos.Y >= TopLeft.Y - margin &&
                   worldPos.Y <= BottomRight.Y + margin;
        }
    }
}
