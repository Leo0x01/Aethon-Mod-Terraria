using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace AethonMod.Content.Effects.Bruma
{
    /// <summary>
    /// BrumaBrushes — v6.17 — LA PANADERÍA DE TEXTURAS DE HUMO EN RUNTIME.
    ///
    /// Los pinceles de la librería de bruma son TEXTURAS NACIDAS DE CÓDIGO
    /// en tiempo de ejecución (Texture2D + SetData, CERO PNGs en el mod):
    /// discos de 128×128 con
    ///
    ///     alfa(u,v) = falloffRadial(d) × ByMids(WarpedFbm(uv·4))
    ///     RGB       = BLANCO PURO (255,255,255)
    ///
    /// ...que es la receta canónica del humo procedural (investigación:
    /// Diablo 3 / Book of Shaders / IQ):
    ///   · La MÁSCARA (falloff radial) es BLANDA y sin detalle — una
    ///     máscara con detalle se ve ESTÁTICA al mover el ruido (regla D3).
    ///   · El RUIDO (fBm con domain warping) lleva TODO el detalle —
    ///     grumos, filamentos, volutas.
    ///   · "Scale by Mids" antes de multiplicar (Julian Love: "si te
    ///     queda mucho negro, se come toda la acción").
    ///   · RGB BLANCO + alfa en canal aparte = tinte LINEAL (lección
    ///     v6.15 del propio mod): sirve IGUAL en lote aditivo (humo
    ///     LUMINOSO) que en lote alfa (humo QUE OCLUYE) — el llamador
    ///     decide el modo, el pincel es neutro.
    ///
    /// 8 VARIANTES por semilla (0..7): cada una con su campo de ruido
    /// retorcido distinto — la rotación + deriva por semilla en BrumaFX
    /// rompe cualquier repetición perceptible.
    ///
    /// Ciclo de vida: creación perezosa (primer uso, SOLO cliente),
    /// disposición en BrumaSystem.Unload() — cero fugas de VRAM entre
    /// recargas del mod.
    /// </summary>
    public static class BrumaBrushes
    {
        /// <summary>Variantes de puff horneadas (semillas 0..7).</summary>
        public const int Variantes = 8;

        private const int Size = 128;

        private static readonly Texture2D[] _puffs = new Texture2D[Variantes];

        /// <summary>
        /// El pincel PUFF por semilla (0..7). Creación perezosa SOLO en
        /// cliente (un servidor dedicado no tiene GraphicsDevice → null).
        /// </summary>
        public static Texture2D Puff(int seed)
        {
            int idx = ((seed % Variantes) + Variantes) % Variantes;
            Texture2D tex = _puffs[idx];
            if (tex != null) return tex;

            // Sin dispositivo gráfico (servidor dedicado): nada que dibujar.
            GraphicsDevice gd = Main.graphics?.GraphicsDevice;
            if (gd == null) return null;

            tex = HornearPuff(gd, idx);
            _puffs[idx] = tex;
            return tex;
        }

        /// <summary>
        /// HORNEA un puff: alfa = falloffRadial × ByMids(WarpedFbm),
        /// RGB = blanco puro. La máscara blandita, el ruido con detalle.
        /// </summary>
        private static Texture2D HornearPuff(GraphicsDevice gd, int seed)
        {
            var data = new Color[Size * Size];
            Vector2 c = new(Size * 0.5f, Size * 0.5f);

            for (int j = 0; j < Size; j++)
            {
                for (int i = 0; i < Size; i++)
                {
                    Vector2 p = new(i + 0.5f, j + 0.5f);
                    float d = Vector2.Distance(p, c) / (Size * 0.5f);   // 0 centro → 1 borde

                    // --- LA MÁSCARA: plató interior + caída suave (smoothstep
                    //     0.55→1.0). BLANDA a propósito (regla Diablo 3).
                    float falloff = 1f - BrumaNoise.Smoothstep(0.55f, 1.0f, d);

                    // --- EL RUIDO: 4 celdas base, fBm TORSIONADO (domain
                    //     warping de IQ) — los filamentos del humo vivo.
                    float u = i * 4f / Size;
                    float v = j * 4f / Size;
                    float n = BrumaNoise.WarpedFbm(u, v, 977 + seed * 131, warp: 3f);

                    // --- SCALE BY MIDS (Julian Love) y multiplicación:
                    //     ×2 SOLO al ruido, JAMÁS a la máscara.
                    n = BrumaNoise.ByMids(n, 1.6f);
                    float a = Math.Clamp(falloff * n * 1.35f, 0f, 1f);

                    // RGB BLANCO + alfa aparte: tinte LINEAL en cualquier lote.
                    data[j * Size + i] = new Color(255, 255, 255, (byte)(int)(a * 255f));
                }
            }

            var tex = new Texture2D(gd, Size, Size);
            tex.SetData(data);
            return tex;
        }

        /// <summary>
        /// Disposición de TODAS las texturas horneadas (llamado desde
        /// BrumaSystem.Unload) — cero fugas de VRAM entre recargas.
        /// </summary>
        public static void Unload()
        {
            for (int i = 0; i < Variantes; i++)
            {
                try { _puffs[i]?.Dispose(); }
                catch { }
                _puffs[i] = null;
            }
        }
    }
}
