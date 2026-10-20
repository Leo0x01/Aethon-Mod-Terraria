using System;
using Microsoft.Xna.Framework;

namespace AethonMod.Content.Projectiles.Cosmic
{
    // ======================================================================
    //  LAS EXTENSIONES VECTORIALES DE LOS AGUJEROS (v6.51.0 — rescatadas
    //  del agujero negro NORMAL cuando este murió, la letra: «quiero que
    //  borres al agujero negro normal del paquete, solo deben estar los
    //  agujeros negros rúnicos»): todas las espirales de los agujeros
    //  RÚNICOS usan RotateTowards para convertir la caída radial de sus
    //  partículas en espiral — la extensión vivía en el archivo del
    //  agujero realista y sobrevive aquí (la matemática es de TODA la
    //  familia).
    // ======================================================================
    /// <summary>Extensiones vectoriales para las partículas en espiral.</summary>
    public static class Vector2Extensions
    {
        /// <summary>Rota el vector hacia el ángulo objetivo como máximo maxStep radianes, conservando la magnitud.</summary>
        public static Vector2 RotateTowards(this Vector2 current, float targetAngle, float maxStep)
        {
            float currentAngle = (float)Math.Atan2(current.Y, current.X);
            float diff = ((targetAngle - currentAngle + MathHelper.Pi * 3) % MathHelper.TwoPi) - MathHelper.Pi;
            if (Math.Abs(diff) <= maxStep)
                return new Vector2((float)Math.Cos(targetAngle), (float)Math.Sin(targetAngle)) * current.Length();
            float newAngle = currentAngle + Math.Sign(diff) * maxStep;
            return new Vector2((float)Math.Cos(newAngle), (float)Math.Sin(newAngle)) * current.Length();
        }
    }
}
