using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.Effects.Bruma
{
    /// <summary>
    /// BrumaFX — v6.17 — LA LIBRERÍA DE HUMO / NIEBLA / BRUMA PROCEDURAL.
    ///
    /// "Crea una librería especializada en humo, niebla, bruma y todo eso
    /// de forma procedural y con calidad, que sea capaz de usarse en
    /// cualquier proporción ya sea grande o pequeño y en todo se vea bien"
    /// (petición del usuario, v6.17).
    ///
    /// QUÉ ES: una caja de herramientas de dibujo de humo 100% PROCEDURAL:
    /// puffs con textura fBm nacida de código (BrumaBrushes), movimiento
    /// orgánico por senos INCONMENSURABLES + curl noise (BrumaNoise) y
    /// composición INVARIANTE DE ESCALA por construcción.
    ///
    /// LA INVARIANCIA DE ESCALA (que se vea bien a 10px y a 500px):
    ///   1. Sub-blobs ∝ PERÍMETRO (n ≈ 0.45·radio), no área — la misma
    ///      "frecuencia de mordiscos" de borde a cualquier tamaño.
    ///   2. Presupuesto de alfa de COBERTURA CONSTANTE: aBlob =
    ///      1−(1−A)^(1/(n+1)) — un puff de 3 blobs y otro de 14 tienen la
    ///      MISMA densidad óptica (el grande no brilla 40× más).
    ///   3. Respiración DESFASADA por blob (nunca en coro) y rotaciones
    ///      independientes del tamaño (rad/s constantes, NO ∝ escala).
    /// REGLA MENTAL: escalar el ESPACIO DE LA SEMILLA, no el sprite.
    ///
    /// EL CONTRATO DE LOTE: todos los métodos de BrumaFX dibujan en el
    /// SpriteBatch ABIERTO que el llamador tenga (y NO lo tocan). El
    /// llamador ELIGE EL MODO:
    ///   · BlendState.Additive   → humo LUMINOSO (bruma mágica, nebulosa).
    ///   · BlendState.AlphaBlend → humo QUE OCLUYE (masa, sombra, tinta).
    /// (El pincel es neutro: RGB blanco + alfa aparte = tinte lineal.)
    ///
    /// TODO determinista por semilla: misma secuencia SIEMPRE, cero
    /// estado, cero red, cero GC por frame.
    ///
    /// LA API:
    ///   · Puff(...)     — LA unidad de humo (núcleo texturizado + borde
    ///                     de sub-blobs que respiran desfasados).
    ///   · Cloud(...)    — racimo de puffs (nube/voluntad de humo).
    ///   · Tendril(...)  — voluta serpenteando por una RUTA de puntos.
    ///   · Column(...)   — columna ascendente (nace, crece, se disipa).
    ///   · MistBand(...) — banda de niebla en capas con paralaje,
    ///                     gradiente vertical y deriva por senos.
    /// </summary>
    public static class BrumaFX
    {
        // ==================================================================
        //  PINCELES
        // ==================================================================

        private static Asset<Texture2D> _softGlow;

        /// <summary>El pincel suave genérico de la biblioteca VFX del mod.</summary>
        private static Texture2D SoftGlow =>
            (_softGlow ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow")).Value;

        // ==================================================================
        //  HELPERS
        // ==================================================================

        /// <summary>Quad centrado (tamaño total = size px) sobre el lote ABIERTO.</summary>
        private static void Quad(Texture2D tex, Vector2 pos, Vector2 size, float rot, Color tint)
        {
            if (tex == null || tint.A == 0) return;
            Main.spriteBatch.Draw(tex, pos, null, tint, rot,
                new Vector2(tex.Width, tex.Height) * 0.5f,
                size / new Vector2(tex.Width, tex.Height),
                SpriteEffects.None, 0f);
        }

        /// <summary>Tinte de INTENSIDAD LINEAL (patrón validado del mod).</summary>
        private static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color(c.R, c.G, c.B, (byte)(int)(255f * f));
        }

        /// <summary>Hash determinista [0,1) (BrumaNoise, sin estado).</summary>
        private static float H01(int seed, int a, int b) => BrumaNoise.Hash(a, b, seed);

        // ==================================================================
        //  PUFF — LA UNIDAD DE HUMO (invariante de escala por construcción)
        // ==================================================================

        /// <summary>
        /// DIBUJA UN PUFF de humo en <paramref name="center"/> con radio
        /// <paramref name="radius"/> (px). Se ve bien a CUALQUIER escala.
        /// </summary>
        /// <param name="center">Centro en el espacio del lote abierto.</param>
        /// <param name="radius">Radio del puff en px (8..500+: todos bien).</param>
        /// <param name="color">Color/masa (alfa del color se ignora).</param>
        /// <param name="seed">Semilla determinista (firma visual propia).</param>
        /// <param name="time">Tiempo animado (p. ej. GlobalTimeWrappedHourly).</param>
        /// <param name="alpha">Densidad óptica total objetivo [0..1].</param>
        /// <param name="quality">0.3..1 — densidad de sub-blobs del borde.</param>
        /// <param name="velocity">Velocidad (estira los blobs traseros: smear).</param>
        public static void Puff(Vector2 center, float radius, Color color, int seed,
            float time, float alpha = 0.55f, float quality = 1f, Vector2 velocity = default)
        {
            Texture2D tex = BrumaBrushes.Puff(seed);
            if (tex == null || radius < 1.5f) return;
            alpha = MathHelper.Clamp(alpha, 0f, 1f);

            // --- 1. EL NÚCLEO: la textura fBm horneada, rotando LENTO por
            //        semilla (rad/s constante — INDEPENDIENTE del tamaño).
            float rot = time * 0.10f * ((seed & 1) == 0 ? 1f : -1f);
            float breathe = 1f + 0.08f * (float)Math.Sin(time * 0.6f + seed);
            Quad(tex, center, new Vector2(radius * 2f * breathe, radius * 2f * breathe),
                rot, Tint(color, alpha * 0.85f));

            // --- 2. EL BORDE: sub-blobs ∝ PERÍMETRO (nunca ∝ área) que
            //        rompen la "imagen repetida". Presupuesto de alfa de
            //        COBERTURA CONSTANTE: aBlob = 1−(1−A·0.5)^(1/(n+1)).
            int n = (int)Math.Clamp(MathF.Round(radius * 0.45f * quality), 2f, 14f);
            float aBlob = 1f - MathF.Pow(1f - alpha * 0.5f, 1f / (n + 1));

            // Estiramiento por velocidad (smear de los dusts 130-134 de
            // vanilla: el humo EN MOVIMIENTO deja masa detrás).
            float trail = MathHelper.Clamp(velocity.Length() * 0.02f, 0f, 1f);

            for (int i = 0; i < n; i++)
            {
                float h1 = H01(seed, i, 1);
                float h2 = H01(seed, i, 2);
                float h3 = H01(seed, i, 3);
                float h4 = H01(seed, i, 4);

                // Deriva lenta alrededor del centro (dirección por blob).
                float ang = h1 * MathHelper.TwoPi + time * 0.05f * (h2 > 0.5f ? 1f : -1f);
                float ring = (0.55f + 0.35f * h2) * radius;
                Vector2 pos = center + new Vector2(
                    (float)Math.Cos(ang) * ring,
                    (float)Math.Sin(ang) * ring * 0.8f);

                // Respiración DESFASADA por blob (nunca en coro).
                float bBreathe = 1f + 0.12f * (float)Math.Sin(time * 0.8f + h4 * MathHelper.TwoPi);
                float blobR = radius * (0.28f + 0.24f * h3) * bBreathe;

                // Rotación individual lenta.
                float brot = time * (0.2f + 0.25f * h1) * (h2 > 0.5f ? 1f : -1f);

                // Estirar CONTRA el movimiento (el rastro).
                Vector2 dir = velocity.LengthSquared() > 0.01f
                    ? velocity / velocity.Length() : Vector2.Zero;
                Vector2 size = new Vector2(
                    blobR * 2f * (1f + trail * 0.8f * Math.Abs(dir.X)),
                    blobR * 2f * (1f + trail * 0.8f * Math.Abs(dir.Y)));

                Quad(SoftGlow, pos, size, brot, Tint(color, aBlob));
            }
        }

        // ==================================================================
        //  CLOUD — un racimo de puffs (nube / bocanada)
        // ==================================================================

        /// <summary>
        /// DIBUJA UNA NUBE: <paramref name="puffs"/> racimos alrededor del
        /// centro, cada uno con su semilla, escala y fase — la masa crece
        /// hacia el interior y los bordes mueren en volutas.
        /// </summary>
        public static void Cloud(Vector2 center, float radius, Color color, int seed,
            float time, int puffs = 5, float alpha = 0.5f)
        {
            for (int i = 0; i < puffs; i++)
            {
                float h1 = H01(seed, 100 + i, 7);
                float h2 = H01(seed, 101 + i, 11);
                float h3 = H01(seed, 102 + i, 13);

                // Deriva ORGÁNICA: dos senos INCONMENSURABLES (nunca un solo
                // seno: péndulo) + curl noise para el remolino del racimo.
                float t = time * 0.30f + h1 * MathHelper.TwoPi;
                Vector2 curl = BrumaNoise.Curl(
                    center.X * 0.004f + h2, center.Y * 0.004f + h3, seed + i);
                Vector2 pos = center + new Vector2(
                    MathF.Cos(t) * radius * 0.42f * h2 +
                    MathF.Sin(time * 0.31f + h1 * 6.28f) * radius * 0.18f + curl.X * radius * 0.10f,
                    MathF.Sin(t * 0.9f) * radius * 0.36f * h3 +
                    MathF.Sin(time * 0.71f + h1 * 12.56f) * radius * 0.12f + curl.Y * radius * 0.10f);

                float puffR = radius * (0.42f + 0.30f * h1);
                // El puff CENTRAL más denso, los de fuera más tenues.
                float a = alpha * (1.25f - 0.55f * (pos - center).Length() / MathF.Max(radius, 1f));

                Puff(pos, puffR, color, seed + i * 37, time + h2 * 10f,
                    MathHelper.Clamp(a, 0.05f, 0.85f), quality: 0.7f);
            }
        }

        // ==================================================================
        //  TENDRIL — una voluta serpenteando por una ruta
        // ==================================================================

        /// <summary>
        /// DIBUJA UNA VOLUTA de humo siguiendo <paramref name="path"/> (2..n
        /// puntos): puffs encadenados con radio variable (perfil fino-grueso
        /// -fino), balanceo por senos inconmensurables y desvanecimiento
        /// hacia el extremo final de la ruta.
        /// </summary>
        /// <param name="path">Puntos de la ruta (en espacio del lote).</param>
        /// <param name="width">Ancho máximo de la voluta en px.</param>
        /// <param name="fade">0 = muere al final de la ruta; 1 = uniforme.</param>
        public static void Tendril(Vector2[] path, float width, Color color, int seed,
            float time, float alpha = 0.5f, float fade = 0.75f)
        {
            if (path == null || path.Length < 2 || width < 1.5f) return;

            // Longitud total de la polilínea.
            float total = 0f;
            for (int i = 1; i < path.Length; i++)
                total += Vector2.Distance(path[i - 1], path[i]);
            if (total < 1f) return;

            // Un puff cada ~medio ancho (solape del 50%: ni bolas ni masa).
            int steps = Math.Clamp((int)(total / (width * 0.55f)), 3, 26);

            for (int s = 0; s <= steps; s++)
            {
                float f = s / (float)steps;   // 0 inicio → 1 final de la ruta

                // Posición sobre la polilínea (por longitud de arco).
                Vector2 pos = PuntoEnRuta(path, f * total);

                // Perfil de ancho: fino → grueso (al 55%) → fino.
                float prof = MathF.Sin(f * MathHelper.Pi * 0.9f + 0.15f);
                float r = width * 0.5f * MathHelper.Clamp(prof, 0.25f, 1f);

                // Balanceo perpendicular: dos senos INCONMENSURABLES.
                float sway = MathF.Sin(time * 0.5f + f * 4.5f + seed) * width * 0.16f
                           + MathF.Sin(time * 1.13f + f * 9.1f + seed * 2) * width * 0.07f;

                // Normal local de la ruta (para el balanceo perpendicular).
                Vector2 ahead = PuntoEnRuta(path, MathF.Min(f * total + width * 0.5f, total));
                Vector2 behind = PuntoEnRuta(path, MathF.Max(f * total - width * 0.5f, 0f));
                Vector2 seg = ahead - behind;
                if (seg.LengthSquared() < 0.01f) seg = Vector2.UnitX;
                Vector2 normal = new Vector2(-seg.Y, seg.X);
                normal.Normalize();

                // Desvanecimiento hacia el final (la voluta SE DISUELVE).
                float endFade = 1f - fade * f;

                // La densidad VIVE: fBm por posición (el ruido decide).
                float n = BrumaNoise.Fbm(pos.X * 0.02f, pos.Y * 0.02f, seed, 3);
                float a = alpha * endFade * (0.45f + 0.55f * n);

                Puff(pos + normal * sway, r, color, seed + s * 53, time,
                    MathHelper.Clamp(a, 0.03f, 0.8f), quality: 0.45f);
            }
        }

        /// <summary>Punto a <paramref name="dist"/> px del inicio de la polilínea.</summary>
        private static Vector2 PuntoEnRuta(Vector2[] path, float dist)
        {
            float acum = 0f;
            for (int i = 1; i < path.Length; i++)
            {
                float seg = Vector2.Distance(path[i - 1], path[i]);
                if (acum + seg >= dist && seg > 0f)
                {
                    float f = (dist - acum) / seg;
                    return Vector2.Lerp(path[i - 1], path[i], f);
                }
                acum += seg;
            }
            return path[path.Length - 1];
        }

        // ==================================================================
        //  COLUMN — columna de humo ascendente
        // ==================================================================

        /// <summary>
        /// DIBUJA UNA COLUMNA de humo que nace en <paramref name="basePos"/>
        /// y asciende <paramref name="height"/> px: cada puff nace pequeño,
        /// crece al subir, se balancea con el viento y SE DISUELVE por
        /// erosión (el ruido decide qué muere primero).
        /// </summary>
        /// <param name="speed">Puffs por segundo que recorren la columna.</param>
        public static void Column(Vector2 basePos, float height, float width, Color color,
            int seed, float time, float alpha = 0.5f, float speed = 0.35f)
        {
            if (height < 4f || width < 1.5f) return;

            // Un puff por cada tramo de ~medio ancho de la columna.
            int count = Math.Clamp((int)(height / (width * 0.9f)), 3, 18);

            for (int i = 0; i < count; i++)
            {
                // Ciclo de vida por fase: nace abajo, muere arriba.
                float phase = (time * speed + i / (float)count) % 1f;

                // Nace a tamaño 0.35·width y CRECE al ascender (nunca
                // aparece a tamaño final — regla de nacimiento del humo).
                float r = width * (0.35f + 0.85f * phase);
                float y = basePos.Y - phase * height;

                // VIENTO: deriva lateral creciendo con la altura + dos senos
                // inconmensurables (0.31/0.71 — jamais en fase).
                float wind = phase * phase * width * 1.6f;
                float x = basePos.X + wind * H01(seed, 3, 7) +
                          MathF.Sin(time * 0.31f + phase * 6.0f + i) * width * 0.22f +
                          MathF.Sin(time * 0.71f + phase * 11.0f + i * 2) * width * 0.09f;

                // Densidad: nace densa, se EROSIONA al morir (VFXDoc — el
                // humo se disuelve en grumos, no se desvanece como fantasma).
                float n = BrumaNoise.Fbm(x * 0.02f, y * 0.02f, seed + i, 3);
                float life = MathF.Sin(phase * MathHelper.Pi);
                float a = alpha * life * BrumaNoise.Erode(1f, n, phase * 0.55f);

                Puff(new Vector2(x, y), r, color, seed + i * 41, time,
                    MathHelper.Clamp(a, 0.03f, 0.8f), quality: 0.55f,
                    velocity: new Vector2(wind * 0.4f, -phase * width * 0.8f));
            }
        }

        // ==================================================================
        //  MISTBAND — banda de niebla con paralaje (capas de bruma)
        // ==================================================================

        /// <summary>
        /// DIBUJA UNA BANDA DE NIEBLA sobre <paramref name="area"/> (rect del
        /// mundo o de pantalla, según el lote que el llamador tenga abierto):
        /// 6 masas ENORMES y tenues por capa, densa ABAJO (gradiente
        /// vertical), deriva por viento + dos senos inconmensurables, fría
        /// por defecto. Usa 2-3 capas con vientos distintos = PROFUNDIDAD.
        /// </summary>
        /// <param name="layer">0 lejos · 1 medio · 2 cerca (tinte/escala).</param>
        /// <param name="wind">Viento en px/s (deriva + paralaje).</param>
        public static void MistBand(Rectangle area, Color color, int seed, float time,
            float wind = 14f, int layer = 0, float alpha = 0.14f)
        {
            float[] alphas = { 0.07f, 0.11f, 0.16f };
            float a = alpha * (alphas[Math.Clamp(layer, 0, 2)] / 0.11f);

            float depth = 1f - layer / 3f;
            float size = area.Height * (0.9f + layer * 0.35f);
            int count = 6;   // ≤6 quads/capa: el overdraw a raya (vfxlabs)

            for (int k = 0; k < count; k++)
            {
                float h = H01(seed + layer * 100, k, 9);
                float t = time * (0.4f + 0.2f * layer);

                // X: rejilla + viento con PARALAJE + 2 senos inconmensurables.
                float x = area.X + (k + 0.5f) * area.Width / count
                        + wind * time * (1f - depth)
                        + 48f * MathF.Sin(t * 0.31f + h * 6.28f)
                        + 20f * MathF.Sin(t * 0.71f + h * 12.56f);
                // Envolver dentro del área (la niebla no se acaba: circula).
                float span = area.Width + size;
                x = area.X - size * 0.5f + ((x - area.X + size * 0.5f) % span + span) % span;

                // GRADIENTE VERTICAL: densa abajo (bruma de valle).
                float yFrac = 0.55f + 0.35f * h;
                float y = area.Y + area.Height * yFrac;
                float vFade = MathF.Pow(yFrac, 1.5f);

                // Tinte: lejos = FRÍO; cerca = neutro (recetas de color).
                Color c = layer == 2
                    ? new Color(210, 220, 230)
                    : new Color((int)(color.R * 0.8f + 40), (int)(color.G * 0.85f + 55), (int)(color.B * 0.9f + 75));

                Puff(new Vector2(x, y), size * 0.5f, c, seed + layer * 100 + k,
                    time + h * 10f, MathHelper.Clamp(a * vFade, 0.02f, 0.5f),
                    quality: 0.35f,
                    velocity: new Vector2(wind * 0.25f, 0f));
            }
        }
    }
}
