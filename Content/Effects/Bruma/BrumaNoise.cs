using System;
using Microsoft.Xna.Framework;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Effects.Bruma
{
    /// <summary>
    /// BrumaNoise — v6.17 — LA MATEMÁTICA DE LA LIBRERÍA DE BRUMA.
    ///
    /// El aparato de ruido PROCEDURAL que alimenta toda la librería de
    /// humo/niebla/bruma del mod — síntesis PROPIA de las técnicas de
    /// ruido fractal (fBm), domain-warping, erosión de alfa y las reglas
    /// anti-fase de la bruma de calidad:
    ///
    ///   · Hash determinista [0,1) — mismo contrato que VFXCore.Hash01:
    ///     cero estado, cero red, misma secuencia SIEMPRE por semilla.
    ///   · Value noise 2D con la QUINTIC de Perlin u(t)=6t⁵−15t⁴+10t³
    ///     (C2 continua — sin arrugas en el gradiente).
    ///   · fBm con LACUNARIDAD 2 EXACTA (potencia entera de 2: las
    ///     coordenadas de cada octava no entran nunca en fase con la
    ///     anterior — la regla nº1 contra el "phasing pulsante").
    ///   · DOMAIN WARPING de IQ: f(p) = fbm(p + warp·fbm(p)) — EL look
    ///     "humo vivo" (nubes retorcidas, no mármol).
    ///   · CURL NOISE: v = (∂ψ/∂y, −∂ψ/∂x) — campo de velocidad SIN
    ///     divergencia: remolinos que no se comprimen (el truco estándar
    ///     del Unity VFX Graph, aquí evaluado en C# puro).
    ///   · OCTAVAS POR RADIO (band-limiting de IQ): el detalle más fino
    ///     mide siempre ~3px → el MISMO puff se ve bien a 10px y a 500px
    ///     (al crecer GANA detalle en vez de estirarse).
    /// </summary>
    public static class BrumaNoise
    {
        // ==================================================================
        //  HASH DETERMINISTA
        // ==================================================================

        /// <summary>Hash entero determinista [0,1) — sin estado, sin red.</summary>
        public static float Hash(int x, int y, int seed)
        {
            int h = unchecked(seed * 374761393 + x * 668265263 + y * 1911520717);
            h = unchecked(h ^ (h >> 13));
            h = unchecked(h * 1274126177);
            h = unchecked(h ^ (h >> 16));
            return (h & 0xFFFFFF) / 16777216f;
        }

        /// <summary>La QUINTIC de Perlin: 6t⁵−15t⁴+10t³ (suave hasta 2ª derivada).</summary>
        private static float Quintic(float t)
            => t * t * t * (t * (t * 6f - 15f) + 10f);

        // ==================================================================
        //  VALUE NOISE 2D
        // ==================================================================

        /// <summary>
        /// Value noise 2D: valores en los 4 nodos de la rejilla, interpolados
        /// bilinealmente con la quintic. Resultado [0,1).
        /// </summary>
        public static float Value(float x, float y, int seed)
        {
            int xi = (int)MathF.Floor(x), yi = (int)MathF.Floor(y);
            float tx = Quintic(x - xi), ty = Quintic(y - yi);

            float a = Hash(xi, yi, seed);
            float b = Hash(xi + 1, yi, seed);
            float c = Hash(xi, yi + 1, seed);
            float d = Hash(xi + 1, yi + 1, seed);

            float ab = a + (b - a) * tx;
            float cd = c + (d - c) * tx;
            return ab + (cd - ab) * ty;
        }

        // ==================================================================
        //  fBm — octavas con lacunaridad 2 EXACTA (anti-fase D3)
        // ==================================================================

        /// <summary>
        /// fBm NORMALIZADO [0,1]: suma de octavas de value noise con
        /// frecuencia ×2 y amplitud ×0.5 por octava (auto-similar — ESA es
        /// la invariancia de escala). <paramref name="octaves"/> es el LOD:
        /// usa OctavesForRadius para elegirlo por tamaño en pantalla.
        /// </summary>
        public static float Fbm(float x, float y, int seed, int octaves = 5)
        {
            const float lacunarity = 2f;   // EXACTA: sin phasing (regla D3)
            const float gain = 0.5f;

            float sum = 0f, amp = 0.5f, norm = 0f;
            float fx = 1f, fy = 1f;
            for (int i = 0; i < octaves; i++)
            {
                sum += amp * Value(x * fx, y * fy, seed + i * 101);
                norm += amp;
                amp *= gain;
                fx *= lacunarity;
                fy *= lacunarity;
            }
            return sum / norm;
        }

        /// <summary>
        /// OCTAVAS ÓPTIMAS para un radio en píxeles (band-limiting de IQ):
        /// el detalle más fino del humo mide SIEMPRE ~3px, así que el mismo
        /// puff a 12px usa 2 octavas y a 384px usa 6 — al crecer GANA
        /// detalle en vez de estirarse como plasticina.
        /// </summary>
        public static int OctavesForRadius(float radiusPx)
            => Math.Clamp((int)MathF.Round(MathF.Log2(MathF.Max(radiusPx, 8f) / 6f)), 2, 7);

        // ==================================================================
        //  DOMAIN WARPING — el look "humo vivo"
        // ==================================================================

        /// <summary>
        /// fBm con DOMINIO TORSIONADO (IQ): f(p) = fbm(p + warp·fbm(p)).
        /// Con warp ~2-4 las nubes se RETUERCEN (filamentos, volutas);
        /// con warp &gt; 6 se vuelve psicodélico.
        /// </summary>
        public static float WarpedFbm(float x, float y, int seed, float warp = 3f)
        {
            // Los offsets 5.2/1.3 solo DECORRELACIONAN los dos campos.
            float qx = Fbm(x, y, seed);
            float qy = Fbm(x + 5.2f, y + 1.3f, seed);
            return Fbm(x + warp * qx, y + warp * qy, seed + 7);
        }

        /// <summary>
        /// v6.25 — WarpedFbm con OCTAVAS explícitas: el horneado del
        /// flipbook de BrumaBrushes usa 4 (velocidad de panadería) en
        /// pines pequeños y 5 en los grandes (detalle extra donde el ojo
        /// lo va a ver). Misma matemática, LOD a la carta.
        /// </summary>
        public static float WarpedFbm(float x, float y, int seed, float warp, int octaves)
        {
            float qx = Fbm(x, y, seed, octaves);
            float qy = Fbm(x + 5.2f, y + 1.3f, seed, octaves);
            return Fbm(x + warp * qx, y + warp * qy, seed + 7, octaves);
        }

        // ==================================================================
        //  CURL NOISE — remolinos sin divergencia
        // ==================================================================

        /// <summary>
        /// CURL 2D del potencial ψ = fBm: v = (∂ψ/∂y, −∂ψ/∂x). Campo de
        /// velocidad SIN divergencia → el humo que lo sigue REMOLINA sin
        /// comprimirse ni agruparse (fluido sin simular fluidos).
        /// Magnitud ~[0,1] en unidades de ruido.
        /// </summary>
        public static Vector2 Curl(float x, float y, int seed, float eps = 0.15f)
        {
            float dy = Fbm(x, y + eps, seed) - Fbm(x, y - eps, seed);
            float dx = Fbm(x + eps, y, seed) - Fbm(x - eps, y, seed);
            return new Vector2(dy, -dx) * (0.5f / eps);
        }

        // ==================================================================
        //  UTILIDADES DE FORMA
        // ==================================================================

        /// <summary>Smoothstep clásico (Hermite) — el falloff suave por excelencia.</summary>
        public static float Smoothstep(float e0, float e1, float t)
        {
            t = Math.Clamp((t - e0) / (e1 - e0), 0f, 1f);
            return t * t * (3f - 2f * t);
        }

        /// <summary>
        /// EROSIÓN DE ALFA: el ruido decide QUÉ muere primero —
        /// el humo se disuelve en GRUMOS por donde el ruido manda, no se
        /// desvanece como fantasma. <paramref name="erosion"/> ∈ [0,1]:
        /// 0 = nada muere, 1 = todo muerto.
        /// </summary>
        public static float Erode(float alpha, float noise01, float erosion, float feather = 0.15f)
        {
            float lo = erosion * (1f + feather) - feather;   // rango [-f, 1+f]
            float hi = lo + feather;
            return Smoothstep(lo, hi, alpha * (0.35f + 0.65f * noise01));
        }

        /// <summary>
        /// "SCALE BY MIDS": al multiplicar capas de
        /// ruido, re-centrar en 0.5 antes — "cuanto más multiplicas, más
        /// quieres constreñir el rango: si te queda mucho negro, se come
        /// toda la acción". Aquí: n' = 0.5 + (n−0.5)·k.
        /// </summary>
        public static float ByMids(float n, float k = 1.6f)
            => 0.5f + (n - 0.5f) * k;
    }
}
