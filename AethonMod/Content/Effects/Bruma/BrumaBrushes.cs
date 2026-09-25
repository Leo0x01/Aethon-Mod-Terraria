using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Effects.Bruma
{
    /// <summary>
    /// BrumaBrushes — v6.25 — LA PANADERÍA DE TEXTURAS DE HUMO EN RUNTIME.
    ///
    /// v6.17: discos de 128×128 horneados en runtime (Texture2D + SetData,
    /// CERO PNGs en el mod) con alfa = falloffRadial × ByMids(WarpedFbm).
    ///
    /// v6.25 — EL FIX CRÍTICO + LA SEGUNDA GENERACIÓN (investigación de 23
    /// fuentes: research/humo_v625/INFORME_MODS_HUMO.md):
    ///
    ///   1. PREMULTIPLICADO (el bug de los rectángulos): antes se horneaba
    ///      RGB = 255 CONSTANTE con alfa variable — en el pipeline FNA/tML
    ///      (que espera alfa premultiplicada) un lote ADITIVO ignora el
    ///      canal alfa y pinta TODO el quad como rectángulo sólido, y en
    ///      lote alfa los bordes quedan duros. AHORA: RGB = blanco × alfa
    ///      (premultiplicado de verdad) — bordes suaves en AMBOS lotes.
    ///      "Premultiplicar o morir" (lección 1 de la investigación).
    ///
    ///   2. ESCALERA DE TAMAÑOS por TEXTURA (lección de la investigación interna — 22/24/32/
    ///      80/256 px): 64 / 128 / 160 px por radio del puff — el detalle
    ///      fino del ruido mide siempre ~3 px de MUNDO, no de textura.
    ///
    ///   3. FLIPBOOK de ruido EVOLUCIONADO (lección de la investigación interna): cada
    ///      variante es una TIRA VERTICAL de 4-6 frames del MISMO campo
    ///      fBm con el dominio desplazándose y el contraste creciendo —
    ///      el humo SE DESGARRA de verdad (la rotación sola delata el
    ///      truco en puffs grandes — anti-patrón nº3 de la investigación).
    ///
    ///   4. VAPOR: la variante de LUT DURA (lección de la investigación interna): núcleo
    ///      denso con caída más brusca y contraste mayor — para el humo
    ///      ALFA de una capa que "ocupa" (el look de juego moderno).
    ///
    /// La receta por píxel sigue siendo la canónica:
    ///     alfa(u,v) = falloffRadial(d) × ByMids(WarpedFbm(uv·4))
    ///     RGB       = BLANCO × alfa   (¡PREMULTIPLICADO!)
    ///
    /// La FRECUENCIA del ruido es 4 celdas/textura en TODOS los escalones
    /// → la celda de ruido mide (2·radio)/4 = radio/2 px de mundo en
    /// cualquier escala (invariancia de escala por construcción).
    ///
    /// Ciclo de vida: creación perezosa (primer uso, SOLO cliente; cada
    /// hornada ≤ ~100 ms — sin tirones), disposición en BrumaSystem.Unload
    /// — cero fugas de VRAM entre recargas del mod.
    /// </summary>
    public static class BrumaBrushes
    {
        /// <summary>Variantes de puff horneadas (semillas 0..3).</summary>
        public const int Variantes = 4;

        /// <summary>Variantes de VAPOR (la LUT dura) horneadas.</summary>
        public const int VaporVariantes = 2;

        /// <summary>Los escalones de la escalera de tamaños (px).</summary>
        private static readonly int[] TierSizes = { 64, 128, 160 };

        /// <summary>Frames del flipbook por escalón (tira vertical).</summary>
        private static readonly int[] TierFrames = { 6, 6, 4 };

        /// <summary>El VAPOR vive en los dos escalones bajos (64/128).</summary>
        private const int VaporTiers = 2;

        // [variante][tier] — textura en TIRA VERTICAL (width = size,
        // height = size × frames; cada frame es una fila).
        // v6.50.5 — SIN readonly: el barrendero de Unload anula los
        // ELEMENTOS por reflexión y .NET 8 prohíbe escribir initonly —
        // ver VFXCore._capas (la traza del client.log v6.50.4).
        private static Texture2D[][] _puffs = new Texture2D[Variantes][];
        private static Texture2D[][] _vapors = new Texture2D[VaporVariantes][];

        // ==================================================================
        //  LA ESCALERA — qué escalón le toca a un radio
        // ==================================================================

        /// <summary>
        /// El escalón de textura para un radio de puff en px: pequeño
        /// (&lt;26) → 64 · mediano (&lt;95) → 128 · grande → 160.
        /// </summary>
        public static int TierForRadius(float radiusPx)
        {
            if (radiusPx < 26f) return 0;
            if (radiusPx < 95f) return 1;
            return 2;
        }

        /// <summary>El tamaño en px del escalón (ancho de la textura).</summary>
        public static int SizeOf(int tier)
            => TierSizes[Math.Clamp(tier, 0, TierSizes.Length - 1)];

        /// <summary>Los frames del flipbook del escalón.</summary>
        public static int FramesOf(int tier)
            => TierFrames[Math.Clamp(tier, 0, TierFrames.Length - 1)];

        // ==================================================================
        //  LOS PINCELES
        // ==================================================================

        /// <summary>
        /// El pincel PUFF por semilla y escalón: una TIRA VERTICAL de
        /// frames (el flipbook del ruido evolucionado). Creación perezosa
        /// SOLO en cliente (un servidor dedicado no tiene GraphicsDevice).
        /// </summary>
        public static Texture2D Puff(int seed, int tier)
        {
            int idx = ((seed % Variantes) + Variantes) % Variantes;
            tier = Math.Clamp(tier, 0, TierSizes.Length - 1);

            Texture2D[] column = _puffs[idx] ??= new Texture2D[TierSizes.Length];
            Texture2D tex = column[tier];
            if (tex != null) return tex;

            // Sin dispositivo gráfico (servidor dedicado): nada que dibujar.
            GraphicsDevice gd = Main.graphics?.GraphicsDevice;
            if (gd == null) return null;

            tex = Hornear(gd, idx, tier, hard: false);
            column[tier] = tex;
            return tex;
        }

        /// <summary>
        /// El pincel VAPOR por semilla y escalón (LUT dura): el humo ALFA
        /// de una capa — núcleo denso, borde que se disuelve en BRUMA
        /// GRUESA, contraste alto. Solo escalones 0/1 (64/128 px).
        /// </summary>
        public static Texture2D Vapor(int seed, int tier)
        {
            int idx = ((seed % VaporVariantes) + VaporVariantes) % VaporVariantes;
            tier = Math.Clamp(tier, 0, VaporTiers - 1);

            Texture2D[] column = _vapors[idx] ??= new Texture2D[VaporTiers];
            Texture2D tex = column[tier];
            if (tex != null) return tex;

            GraphicsDevice gd = Main.graphics?.GraphicsDevice;
            if (gd == null) return null;

            tex = Hornear(gd, 100 + idx, tier, hard: true);
            column[tier] = tex;
            return tex;
        }

        // ==================================================================
        //  LA PANADERÍA
        // ==================================================================

        /// <summary>
        /// HORNEA una tira de flipbook: alfa = falloffRadial ×
        /// ByMids(WarpedFbm evolucionado), RGB = BLANCO × alfa
        /// (PREMULTIPLICADO — el fix v6.25). Cada frame desplaza el
        /// dominio del ruido (+0.3 celdas) y endurece el contraste
        /// (+0.25 por frame): el humo se RETUERCE y luego SE DESGARRA.
        /// </summary>
        private static Texture2D Hornear(GraphicsDevice gd, int seed, int tier, bool hard)
        {
            int size = TierSizes[Math.Clamp(tier, 0, TierSizes.Length - 1)];
            int frames = hard ? 6 : TierFrames[Math.Clamp(tier, 0, TierFrames.Length - 1)];
            var data = new Color[size * size * frames];

            // Los pines pequeños hornean a 4 octavas (velocidad); los
            // grandes a 5 (el ojo les pilla los bultos de lejos).
            int octavas = tier >= 2 ? 5 : 4;

            Vector2 c = new(size * 0.5f, size * 0.5f);

            for (int f = 0; f < frames; f++)
            {
                // La EVOLUCIÓN del frame: el dominio se desplaza lento
                // (0.3 celdas/frame — morfa, no salta) y el contraste
                // crece (el humo se agrieta al envejecer).
                float drift = f * 0.30f;
                float contraste = (hard ? 1.9f : 1.6f) + f * 0.25f;
                // El VAPOR además ENCOGE su plató interior al envejecer
                // (el núcleo denso se come el borde — LUT que se endurece).
                float mesa = (hard ? 0.62f : 0.55f) - f * (hard ? 0.03f : 0.02f);

                int baseIdx = f * size * size;
                for (int j = 0; j < size; j++)
                {
                    for (int i = 0; i < size; i++)
                    {
                        Vector2 p = new(i + 0.5f, j + 0.5f);
                        float d = Vector2.Distance(p, c) / (size * 0.5f);   // 0 centro → 1 borde

                        // --- LA MÁSCARA: plató interior + caída suave.
                        //     BLANDA a propósito (regla anti-fase); el
                        //     VAPOR usa la caída DURA (0.62→0.86: LUT
                        //     255→0 en ~74% de densidad — investigación interna). ---
                        float falloff = hard
                            ? 1f - BrumaNoise.Smoothstep(mesa, 0.86f, d)
                            : 1f - BrumaNoise.Smoothstep(mesa, 1.0f, d);

                        // --- EL RUIDO: 4 celdas base, fBm TORSIONADO
                        //     (domain warping de IQ) + drift del frame. ---
                        float u = i * 4f / size + drift;
                        float v = j * 4f / size + drift * 0.7f;
                        float n = BrumaNoise.WarpedFbm(u, v, 977 + seed * 131, 3f, octavas);

                        // --- SCALE BY MIDS ×2 SOLO al ruido, JAMÁS a la
                        //     máscara (y el contraste crece por frame). ---
                        n = BrumaNoise.ByMids(n, contraste);
                        float a = Math.Clamp(falloff * n * (hard ? 1.55f : 1.35f), 0f, 1f);

                        // --- *** EL FIX v6.25: PREMULTIPLICADO *** ---
                        // --- RGB = blanco × alfa: bordes suaves en el ---
                        // --- lote aditivo (donde el alfa se ignora)  ---
                        // --- y en el lote alfa. Transparente de ver- ---
                        // --- dad (RGB→0 cuando alfa→0).               ---
                        byte prem = (byte)(int)(a * 255f);
                        data[baseIdx + j * size + i] = new Color(prem, prem, prem, prem);
                    }
                }
            }

            var tex = new Texture2D(gd, size, size * frames);
            tex.SetData(data);
            return tex;
        }

        /// <summary>
        /// Disposición de TODAS las texturas horneadas (llamado desde
        /// BrumaSystem.Unload) — cero fugas de VRAM entre recargas.
        /// v6.27 FIX: ModContent.UnloadModContent corre en un hilo del POOL
        /// y FNA exige que Texture.Dispose ocurra en el hilo PRINCIPAL
        /// (ThreadStateException en el log del usuario). Las referencias se
        /// cortan AHORA y la disposición real se ENCOLA al hilo principal.
        /// </summary>
        public static void Unload()
        {
            // Recolectar primero (el cierre solo captura locales — las
            // estáticas ya quedan en null para la próxima carga).
            List<Texture2D> moribundas = new();
            // v6.50.6 — FIX (el segundo dominó del client.log v6.50.5):
            // _puffs/_vapors son mutables desde v6.50.5 y el barrendero de
            // AethonMod.cs (Mod.Unload) corre ANTES que BrumaSystem.Unload
            // — los arrays pueden llegar YA anulados (el elemento null se
            // guardaba, pero el ARRAY en sí no: _puffs[v] sobre _puffs ==
            // null reventaba con NullReferenceException). Guard por array.
            if (_puffs != null)
                for (int v = 0; v < Variantes; v++)
                {
                    if (_puffs[v] == null) continue;
                    for (int t = 0; t < _puffs[v].Length; t++)
                    {
                        if (_puffs[v][t] != null) moribundas.Add(_puffs[v][t]);
                        _puffs[v][t] = null;
                    }
                    _puffs[v] = null;
                }
            if (_vapors != null)
                for (int v = 0; v < VaporVariantes; v++)
                {
                    if (_vapors[v] == null) continue;
                    for (int t = 0; t < _vapors[v].Length; t++)
                    {
                        if (_vapors[v][t] != null) moribundas.Add(_vapors[v][t]);
                        _vapors[v][t] = null;
                    }
                    _vapors[v] = null;
                }

            if (moribundas.Count == 0) return;

            // FNA: las funciones de audio/gráficos deben correr en el hilo
            // principal — Main.QueueMainThreadAction es el canal oficial.
            Main.QueueMainThreadAction(() =>
            {
                foreach (Texture2D tex in moribundas)
                {
                    try { tex.Dispose(); }
                    catch { }
                }
            });
        }
    }
}
