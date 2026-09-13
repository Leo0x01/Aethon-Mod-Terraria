using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace AethonMod.Content.Particles
{
    /// <summary>
    /// Tipos de forma que puede generar un ShapeDescriptor.
    /// </summary>
    public enum ShapeType
    {
        Box,
        Circle,
        Cone,
        Sphere,
        Vortex,
        Line
    }

    /// <summary>
    /// Describe una forma geométrica para spawnear partículas.
    /// En lugar de métodos específicos (SpawnBox, SpawnCircle...), la API genérica
    /// SpawnShape(shape, count, template) se encarga de posicionar las partículas
    /// según la forma. Añadir una forma nueva solo requiere un caso en el switch.
    ///
    /// </summary>
    public readonly struct ShapeDescriptor
    {
        public ShapeType Type { get; }
        /// <summary>width/height para Box, radius para Circle, length/angle para Cone y Vortex.</summary>
        public Vector2 Size { get; }
        public bool Hollow { get; }
        /// <summary>Rotación aplicada al punto generado (para formas orientadas).</summary>
        public float Rotation { get; }

        public ShapeDescriptor(ShapeType type, Vector2 size, bool hollow, float rotation)
        {
            Type = type;
            Size = size;
            Hollow = hollow;
            Rotation = rotation;
        }

        /// <summary>
        /// Genera un punto aleatorio dentro de (o sobre el borde de) la forma,
        /// relativo al origen. Aplica la rotación de la forma si no es cero.
        /// </summary>
        public Vector2 GenerateRandomPoint()
        {
            Vector2 point = Type switch
            {
                ShapeType.Box => GenerateBox(Size.X, Size.Y, Hollow),
                ShapeType.Circle => GenerateCircle(Size.X, Hollow),
                ShapeType.Cone => GenerateCone(Size.X, Size.Y),
                ShapeType.Sphere => GenerateSphere(Size.X),
                ShapeType.Vortex => GenerateVortex(Size.X, Size.Y),
                ShapeType.Line => GenerateLine(Size.X),
                _ => Vector2.Zero
            };

            if (Rotation != 0f)
                point = Rotate(point, Rotation);

            return point;
        }

        // ------------------------------------------------------------------
        //  GENERADORES POR FORMA
        // ------------------------------------------------------------------

        /// <summary>Caja sólida o hueca (solo bordes).</summary>
        private static Vector2 GenerateBox(float w, float h, bool hollow)
        {
            if (hollow)
            {
                int edge = Main.rand.Next(4);
                return edge switch
                {
                    0 => new Vector2(Main.rand.NextFloat(-w / 2, w / 2), -h / 2), // top
                    1 => new Vector2(w / 2, Main.rand.NextFloat(-h / 2, h / 2)),  // right
                    2 => new Vector2(Main.rand.NextFloat(-w / 2, w / 2), h / 2),  // bottom
                    _ => new Vector2(-w / 2, Main.rand.NextFloat(-h / 2, h / 2))  // left
                };
            }
            return new Vector2(
                Main.rand.NextFloat(-w / 2, w / 2),
                Main.rand.NextFloat(-h / 2, h / 2));
        }

        /// <summary>Círculo sólido o hueco (circunferencia). Coordenadas polares.</summary>
        private static Vector2 GenerateCircle(float r, bool hollow)
        {
            float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
            float dist = hollow ? r : Main.rand.NextFloat(0f, r);
            return Rotate(new Vector2(dist, 0f), angle);
        }

        /// <summary>Cono: ángulo aleatorio dentro de ±halfAngle y distancia hasta length.</summary>
        private static Vector2 GenerateCone(float length, float halfAngle)
        {
            float angle = Main.rand.NextFloat(-halfAngle, halfAngle);
            float dist = Main.rand.NextFloat(0f, length);
            return Rotate(new Vector2(dist, 0f), angle);
        }

        /// <summary>Esfera 3D proyectada a 2D: sqrt para distribución uniforme en área.</summary>
        private static Vector2 GenerateSphere(float r)
        {
            float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
            float dist = (float)Math.Sqrt(Main.rand.NextFloat(0f, 1f)) * r;
            return Rotate(new Vector2(dist, 0f), angle);
        }

        /// <summary>Vórtice: brazos espirales con `turns` vueltas hasta el radio r.</summary>
        private static Vector2 GenerateVortex(float r, float turns)
        {
            float t = Main.rand.NextFloat(0f, 1f);
            float angle = t * turns * MathHelper.TwoPi;
            float dist = t * r;
            return Rotate(new Vector2(dist, 0f), angle);
        }

        /// <summary>Línea a lo largo del eje X centrada en el origen.</summary>
        private static Vector2 GenerateLine(float length)
        {
            return new Vector2(Main.rand.NextFloat(-length / 2, length / 2), 0f);
        }

        /// <summary>Rotación manual (evita depender de extensiones de tML).</summary>
        private static Vector2 Rotate(Vector2 v, float radians) => new(
            v.X * (float)Math.Cos(radians) - v.Y * (float)Math.Sin(radians),
            v.X * (float)Math.Sin(radians) + v.Y * (float)Math.Cos(radians));

        // ------------------------------------------------------------------
        //  FACTORY METHODS
        // ------------------------------------------------------------------

        public static ShapeDescriptor Box(float w, float h) => new(ShapeType.Box, new Vector2(w, h), false, 0f);
        public static ShapeDescriptor Circle(float r) => new(ShapeType.Circle, new Vector2(r, r), false, 0f);
        public static ShapeDescriptor HollowBox(float w, float h) => new(ShapeType.Box, new Vector2(w, h), true, 0f);
        public static ShapeDescriptor HollowCircle(float r) => new(ShapeType.Circle, new Vector2(r, r), true, 0f);
        public static ShapeDescriptor Cone(float length, float halfAngle) => new(ShapeType.Cone, new Vector2(length, halfAngle), false, 0f);
        public static ShapeDescriptor Sphere(float r) => new(ShapeType.Sphere, new Vector2(r, r), false, 0f);
        public static ShapeDescriptor Vortex(float r, float turns) => new(ShapeType.Vortex, new Vector2(r, turns), false, 0f);
        public static ShapeDescriptor Line(float length) => new(ShapeType.Line, new Vector2(length, 0), false, 0f);
    }
}
