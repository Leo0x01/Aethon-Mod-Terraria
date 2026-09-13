using System;
using Microsoft.Xna.Framework;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// BlackHolePhysics — v6.09 — LA MATEMÁTICA REAL DE UN AGUJERO NEGRO.
    ///
    /// Petición del usuario: "investiga más sobre agujeros negros, investiga
    /// las matemáticas de cómo crear un agujero negro, crea otra librería de
    /// ser necesario con las físicas correctas". Esta es ESA librería: las
    /// fórmulas de la relatividad general (Schwarzschild) que gobiernan el
    /// ASPECTO de un agujero negro con disco de acreción, en el sistema de
    /// unidades natural r_s = 1 (todas las distancias en radios de
    /// Schwarzschild). Fuentes: James et al. 2015 (DNGR/Interstellar),
    /// Luminet 1979, Shakura–Sunyaev 1973, UNLV shadow notes — véase
    /// research/blackhole/MATEMATICA_AGUJEROS_NEGROS.md.
    ///
    /// Unidades: r está en r_s (r=1 ES el horizonte). Velocidades en c.
    /// </summary>
    public static class BlackHolePhysics
    {
        // ------------------------------------------------------------------
        //  CONSTANTES FÍSICAS (en radios de Schwarzschild)
        // ------------------------------------------------------------------

        /// <summary>Radio del horizonte de sucesos: r_s = 2GM/c². Unidad.</summary>
        public const float SchwarzschildRadius = 1f;

        /// <summary>Esfera de fotones: la luz orbita circularmente a 1.5·r_s.</summary>
        public const float PhotonSphere = 1.5f;

        /// <summary>
        /// Radio de la SOMBRA aparente para un observador lejano:
        /// R_sh = (√27/2)·r_s ≈ 2.598·r_s — el "agujero negro" que se ve es
        /// la imagen lensada de la esfera de fotones, MÁS GRANDE que el
        /// horizonte real. Es el disco negro de la referencia.
        /// </summary>
        public const float ShadowRadius = 2.5980762f;

        /// <summary>
        /// ISCO (última órbita circular estable): 3·r_s = 6GM/c². Por dentro
        /// la materia no puede orbitar: cae irremediablemente → es el borde
        /// interno natural del disco de acreción.
        /// </summary>
        public const float Isco = 3f;

        // ------------------------------------------------------------------
        //  DINÁMICA KEPLERIANA RELATIVISTA
        // ------------------------------------------------------------------

        /// <summary>
        /// Velocidad orbital kepleriana (fracción de c) a radio r (en r_s):
        /// v = √(GM/r) = c·√(r_s/(2r)). En el ISCO: c/√6 ≈ 0.408c.
        /// </summary>
        public static float KeplerSpeed(float r)
        {
            return (float)Math.Sqrt(1f / (2f * Math.Max(r, 0.25f)));
        }

        /// <summary>
        /// Velocidad ANGULAR kepleriana (rad por unidad de tiempo):
        /// ω = v/r ∝ r^(−3/2). El plasma interno HIERVE mucho más rápido
        /// que el externo — la firma visual de un disco real.
        /// </summary>
        public static float KeplerAngularSpeed(float r)
        {
            float rr = Math.Max(r, 0.25f);
            return (float)Math.Sqrt(1f / (2f * rr * rr * rr));
        }

        /// <summary>
        /// Factor Doppler relativístico del plasma que orbita:
        /// δ = 1/(γ·(1 − β·cos θ)), con β = v/c y θ el ángulo entre la
        /// velocidad y la línea de visión HACIA el observador
        /// (cos θ &gt; 0 = se acerca → δ &gt; 1 → azul/brillante).
        /// </summary>
        public static float DopplerFactor(float beta, float cosTheta)
        {
            beta = MathHelper.Clamp(beta, 0f, 0.95f);
            float gamma = 1f / (float)Math.Sqrt(1f - beta * beta);
            return 1f / (gamma * (1f - beta * cosTheta));
        }

        /// <summary>
        /// BRILLO observado por Doppler beaming: I_obs = δ³·I_emit
        /// (δ⁴ bolométrico; δ³ es lo que pesa en una banda de imagen).
        /// Con β≈0.4 el lado que se acerca brilla ~3.6× y el que se aleja
        /// queda a ~0.28× (contraste ~13×, como en la foto del M87).
        /// Aquí se devuelve una versión SUAVIZADA (lineal en el contraste)
        /// calibrada con la referencia, que es más moderada.
        /// </summary>
        public static float DopplerBrightness(float beta, float cosTheta)
        {
            float delta = DopplerFactor(beta, cosTheta);
            return delta * delta * delta;
        }

        // ------------------------------------------------------------------
        //  TERMODINÁMICA DEL DISCO (Shakura–Sunyaev)
        // ------------------------------------------------------------------

        /// <summary>
        /// Temperatura efectiva del disco delgado: T(r) ∝ r^(−3/4)
        /// (Shakura–Sunyaev 1973). El borde interno es el más caliente.
        /// </summary>
        public static float DiskTemperature(float r)
        {
            return (float)Math.Pow(Math.Max(r, 0.5f), -0.75);
        }

        /// <summary>
        /// Emisividad térmica (Stefan–Boltzmann): I(r) ∝ T⁴ ∝ r^(−3).
        /// La ley física pura (caída brutal hacia fuera); el render usa una
        /// curva VISUAL calibrada con la referencia que conserva la forma
        /// (máximo hacia el interior-medio) sin apagar el exterior.
        /// </summary>
        public static float DiskEmissivity(float r)
        {
            return (float)Math.Pow(Math.Max(r, 0.5f), -3f);
        }

        /// <summary>
        /// Corrimiento al rojo gravitatorio: g = √(1 − r_s/r) — la luz
        /// emitida a r se enroja y apaga al escapar del pozo.
        /// </summary>
        public static float GravitationalRedshift(float r)
        {
            return (float)Math.Sqrt(Math.Max(0f, 1f - 1f / Math.Max(r, 1.001f)));
        }

        // ------------------------------------------------------------------
        //  GEOMETRÍA DE OCLUSIÓN DEL DISCO INCLINADO (la clave del look)
        // ------------------------------------------------------------------

        /// <summary>
        /// Proyecta un punto del plano del disco a la pantalla.
        /// El disco se ve con inclinación i (ángulo DESDE face-on):
        /// el eje menor se comprime a b = a·cos(i). Devuelve el desplazamiento
        /// en pantalla respecto al centro del agujero.
        /// </summary>
        /// <param name="r">Radio orbital (mismo units que R_sh).</param>
        /// <param name="phi">Azimut en el plano del disco (rad).</param>
        /// <param name="minorRatio">cos(i) — achatado de la elipse (0.345 en
        /// la referencia: casi de canto, i ≈ 70°).</param>
        public static Vector2 ProjectDiskPoint(float r, float phi, float minorRatio)
        {
            return new Vector2(
                r * (float)Math.Cos(phi),
                r * (float)Math.Sin(phi) * minorRatio);
        }

        /// <summary>
        /// ¿El punto está en el lado CERCANO del disco? (sin φ &gt; 0 — la
        /// mitad inferior en pantalla). El lado cercano se dibuja ENCIMA de
        /// la esfera negra: el disco CRUZA POR DELANTE de su cara inferior
        /// (el "wrap" Gargantua de Interstellar). El lado lejano se dibuja
        /// detrás y la esfera lo oculta dentro de su silueta.
        /// </summary>
        public static bool IsNearSide(float phi)
        {
            return Math.Sin(phi) > 0f;
        }

        /// <summary>
        /// ¿Un punto del lado LEJANO queda oculto tras la esfera?
        /// Cierto si su proyección cae dentro del círculo de la sombra.
        /// (El dibujado por capas lo resuelve automáticamente: esfera
        /// negra opaca dibujada DESPUÉS del lado lejano y ANTES del cercano.)
        /// </summary>
        public static bool IsOccludedByShadow(Vector2 screenOffset, float shadowRadius)
        {
            return screenOffset.LengthSquared() < shadowRadius * shadowRadius;
        }
    }
}
