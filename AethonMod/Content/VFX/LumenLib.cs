using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using ReLogic.Content;

namespace AethonMod.Content.VFX
{
    // =====================================================================
    //  EL MOTOR DE COLOR (la lección nº1: el color de la luz VIVE)
    // =====================================================================

    /// <summary>Las paletas cíclicas de la casa (una por personalidad).</summary>
    public static class LumenPalettes
    {
        /// <summary>La firma rosa-violeta de la Emperatriz (cuerpo de BoltPrism).</summary>
        public static readonly Color[] PrismRose = new Color[]
        {
            new(207, 0, 151), new(255, 220, 154), new(0, 255, 255),
            new(35, 175, 255), new(144, 61, 196),
        };

        /// <summary>El arcoíris vivo de 9 tonos (S=1, L=0.5) — el ciclo día.</summary>
        public static readonly Color[] PrismDay = new Color[]
        {
            new(255, 80, 80), new(255, 160, 60), new(255, 240, 90),
            new(140, 255, 120), new(90, 255, 220), new(90, 220, 255),
            new(140, 140, 255), new(220, 110, 255), new(255, 100, 200),
        };

        /// <summary>El oro solar regia (la casa ya vive en esta paleta).</summary>
        public static readonly Color[] SolarGold = new Color[]
        {
            new(255, 250, 235), new(255, 195, 85), new(255, 140, 40),
        };

        /// <summary>El frio del vacío (violeta→azul→cian).</summary>
        public static readonly Color[] VoidCold = new Color[]
        {
            new(150, 80, 255), new(60, 80, 220), new(80, 200, 255),
        };

        /// <summary>La furia del eclipse (dorado→carmesí→violeta).</summary>
        public static readonly Color[] EclipseFire = new Color[]
        {
            new(255, 231, 69), new(255, 90, 40), new(150, 80, 255),
            new(255, 195, 85),
        };
    }

    /// <summary>
    /// LumenLib — v6.22 — LA LIBRERÍA DE LA LUZ.
    ///
    /// Nace de la investigación superprofunda de las técnicas de luz del
    /// ecosistema (el rework de la Emperatriz de los grandes mods de VFX +
    /// la Emperatriz vanilla extraída del binario real) y se re-implementa
    /// 100% con código PROPIO sobre la pila de la casa: SpriteBatch +
    /// texturas procedurales + hash determinista. Cero referencias
    /// externas — todo el código es nuestro.
    ///
    /// StormLib es la librería de los RAYOS (filamentos eléctricos);
    /// BrumaFX la de humo/niebla; VFXCore el núcleo de quads. LumenLib es
    /// la de la LUZ que EMANA — lo que la investigación enseñó:
    ///
    ///   1. EL BLOOM APILADO INVERTIDO — la textura radial universal
    ///      (LumenBloom) dibujada en 2-4 capas: grande+tenue FUERA,
    ///      pequeño+brillante DENTRO (4.1/2.85/1.5/0.8 con alphas
    ///      0.25/0.67/0.7/1.0 — los números medidos del ecosistema).
    ///
    ///   2. LA DOBLE PASADA UNIVERSAL — TODO se dibuja dos veces: la capa
    ///      de COLOR (alpha÷2, escala ×1.1-1.4) SOBRE la capa BLANCA
    ///      (alpha÷2, escala ×1.0). El glow coloreado SIEMPRE es más
    ///      grande que el núcleo blanco.
    ///
    ///   3. EL COLOR VIVE EN HSL — hslToRgb(hue, S, L) con L firmado por
    ///      CAPA: 0.5 cuerpo · 0.85 luz emitida · 1.0 núcleos · 0.3-0.66
    ///      velos exteriores. El hue SIEMPRE deriva de algo (identidad,
    ///      progreso, índice, ángulo) — nunca un arcoíris sincronizado.
    ///
    ///   4. EL DRIFT LENTO — el hue de cada entidad camina 0.2-0.6
    ///      hue/s + semilla por identidad (id×0.23): cada proyectil vive
    ///      su propio ciclo cromático.
    ///
    ///   5. LAS LANZAS DE LUZ — hoja alargada (LumenBlade) con núcleo
    ///      caliente + telegraph previo + ESTELA de N copias fantasma que
    ///      crecen hacia atrás (escala hasta ×1.4, alpha por distancia).
    ///
    ///   6. LOS RAYOS DE SOL — haces estirados de la textura de bloom
    ///      (elipse con centro caliente) con GROSOR ANIMADO (0.25→0.7)
    ///      y 3 capas: velo ×1.6, cuerpo ×1.0, núcleo blanco ×0.3.
    ///
    ///   7. EL DESTELLO DE 4 PUNTAS — LumenFlare (cruz principal + cruz
    ///      diagonal ×0.45 + punto caliente) — el lenguaje de "está
    ///      ARDIENDO" a cualquier distancia.
    ///
    ///   8. EL AURORA — bandas verticales espejadas (π·i) con dos vueltas
    ///      de hue y dos frecuencias — la muerte/renacimiento prismático.
    ///
    ///   9. LA LUZ DEL MUNDO MUESTREADA — AddLight a lo largo del camino
    ///      cada N px (no por frame, no por punto) con el color a L=0.85.
    ///
    /// CONTRATO (idéntico al de StormLib): los métodos de DIBUJO reciben
    /// el batch ABIERTO en modo aditivo y no lo tocan — se pueden anidar
    /// dentro de un renderer mayor. Coordenadas tal cual lleguen.
    /// </summary>
    public static class LumenLib
    {
        // ==================================================================
        //  PINCELES — las texturas de la luz (procedurales, v6.22)
        // ==================================================================

        private static Asset<Texture2D> _bloomTex;
        private static Asset<Texture2D> _bladeTex;
        private static Asset<Texture2D> _flareTex;
        private static Asset<Texture2D> _glowTex;

        private static Texture2D BloomTex =>
            (_bloomTex ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/LumenBloom")).Value;

        private static Texture2D BladeTex =>
            (_bladeTex ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/LumenBlade")).Value;

        private static Texture2D FlareTex =>
            (_flareTex ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/LumenFlare")).Value;

        private static Texture2D GlowTex =>
            (_glowTex ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow")).Value;

        // ==================================================================
        //  EL MOTOR DE COLOR — HSL con las L firmadas por capa
        // ==================================================================

        /// <summary>HSL → RGB propio (el hue en vueltas 0..1, S y L 0..1).</summary>
        public static Color Hue(float h, float l = 0.5f, float s = 1f)
        {
            h = h - (float)Math.Floor(h);              // wrap 0..1
            l = MathHelper.Clamp(l, 0f, 1f);
            s = MathHelper.Clamp(s, 0f, 1f);

            float r, g, b;
            if (s <= 0.001f) { r = g = b = l; }
            else
            {
                float q = l < 0.5f ? l * (1f + s) : l + s - l * s;
                float p = 2f * l - q;
                r = HueToRgb(p, q, h + 1f / 3f);
                g = HueToRgb(p, q, h);
                b = HueToRgb(p, q, h - 1f / 3f);
            }
            return new Color((int)(r * 255f), (int)(g * 255f), (int)(b * 255f));
        }

        private static float HueToRgb(float p, float q, float t)
        {
            if (t < 0f) t += 1f;
            if (t > 1f) t -= 1f;
            if (t < 1f / 6f) return p + (q - p) * 6f * t;
            if (t < 0.5f) return q;
            if (t < 2f / 3f) return p + (q - p) * (2f / 3f - t) * 6f;
            return p;
        }

        /// <summary>
        /// EL DRIFT: el hue que CAMINA con el tiempo — cada entidad vive su
        /// propio ciclo cromático (semilla por identidad + 0.2-0.6 hue/s:
        /// nunca rápido, nunca sincronizado).
        /// </summary>
        public static float Drift(float time, float idSeed, float huePerSec = 0.33f)
            => idSeed * 0.23f + time * huePerSec;

        /// <summary>
        /// La PALETA CÍCLICA con wrap (último↔primero): t en vueltas 0..1,
        /// interpolación suave entre los colores vecinos de la tabla.
        /// </summary>
        public static Color Cycle(float t, Color[] palette)
        {
            if (palette == null || palette.Length == 0) return Color.White;
            if (palette.Length == 1) return palette[0];
            t = t - (float)Math.Floor(t);
            float f = t * palette.Length;
            int i = (int)Math.Floor(f);
            float k = f - i;
            // suavizado en los extremos del segmento (sin dientes de sierra)
            k = k * k * (3f - 2f * k);
            Color a = palette[i % palette.Length];
            Color b = palette[(i + 1) % palette.Length];
            return Color.Lerp(a, b, k);
        }

        // ==================================================================
        //  1. EL BLOOM APILADO INVERTIDO — el corazón de la librería
        // ==================================================================

        // v6.49 — LAS ESCALAS/ALFAS MEDIDAS COMO CONSTANTES DE CLASE
        // (hallazgo AUD-A: cada Bloom() alocaba DOS arrays frescos — y
        // Bloom/BloomPulse se llaman decenas de veces por frame: decenas
        // de arrays basura por frame, GC churn continuo. El doc de la
        // casa decía "cero GC por frame": ahora es verdad de nuevo).
        private static readonly float[] _bloomScales = { 4.1f, 2.85f, 1.5f, 0.8f };
        private static readonly float[] _bloomAlphas = { 0.25f, 0.67f, 0.70f, 1.0f };

        /// <summary>
        /// EL BLOOM: la textura radial universal apilada en capas INVERTIDAS
        /// (grande+tenue fuera, pequeño+brillante dentro). `size` = diámetro
        /// de la capa MÁS INTERNA (la caliente). `layers` 2-4.
        /// </summary>
        /// <param name="batch">Batch ABIERTO en modo aditivo.</param>
        /// <param name="pos">Centro (coords tal cual lleguen).</param>
        /// <param name="size">Diámetro del núcleo caliente en px.</param>
        /// <param name="color">El color de la LUZ (el núcleo tiende a blanco).</param>
        /// <param name="intensity">Multiplicador global 0..1.</param>
        public static void Bloom(SpriteBatch batch, Vector2 pos, float size,
            Color color, float intensity, int layers = 3)
        {
            if (intensity <= 0.02f) return;
            float[] scales = _bloomScales;
            float[] alphas = _bloomAlphas;

            int n = (int)MathHelper.Clamp(layers, 1, 4);
            for (int i = 4 - n; i < 4; i++)
            {
                float f = size * scales[i];
                // La capa MÁS INTERNA vira a BLANCO (el corazón de la luz).
                Color c = i >= 2
                    ? Color.Lerp(color, new Color(255, 250, 240), i == 3 ? 0.55f : 0.25f)
                    : color;
                Quad(batch, BloomTex, pos, new Vector2(f, f), 0f,
                    Tint(c, alphas[i] * intensity));
            }
        }

        /// <summary>El VUELO del bloom (para halos que respiran).</summary>
        public static void BloomPulse(SpriteBatch batch, Vector2 pos, float size,
            Color color, float intensity, float time, float hz = 1.4f, int layers = 3)
        {
            float pulse = 0.82f + 0.18f * (float)Math.Sin(time * hz);
            Bloom(batch, pos, size * pulse, color, intensity * pulse, layers);
        }

        /// <summary>
        /// v6.34 — EL BLOOM MULTI-ESCALA: tres bandas de frecuencia de glow
        /// (núcleo + medio + amplio) con alfas calibrados — el look "bloom de
        /// verdad" SIN tocar el pipeline global del juego: la textura radial
        /// suave (SoftGlow) apilada a 1.0×, 1.9× y 3.4× del
        /// <paramref name="radio"/>, con alfas 0.55·i, 0.28·i y 0.13·i
        /// (i = <paramref name="intensidad"/>) y el color CALENTÁNDOSE hacia
        /// blanco al alejarse del centro (la media a un 30%, la amplia a un
        /// 55% — como el corazón de un bloom real, que "quema" la periferia).
        /// La suma de las tres bandas lee como UN SOLO resplandor con cuerpo
        /// y caída suave, no como tres aros concéntricos.
        ///
        /// POR QUÉ MULTI-ESCALA Y NO RenderTarget (decisión de la casa): un
        /// RT global — agarrar la escena, difuminarla y re-proyectarla —
        /// puede dejar la PANTALLA NEGRA si algo falla (un begin/end
        /// desbalanceado, un cambio de resolución a mitad de frame, una
        /// excepción dentro del hook de render) y NO se puede probar fuera
        /// del juego: el fallo solo se ve dentro, cuando ya es tarde. El
        /// multi-escala es VERIFICABLE (la suma de alfas por banda se
        /// puede mockear numéricamente igual que el desgarro) y SEGURO: si
        /// una capa falla, las demás siguen pintadas. Mismo contrato que
        /// <see cref="Bloom"/>: el batch ABIERTO en aditivo, sin tocarlo.
        /// </summary>
        /// <param name="batch">Batch ABIERTO en modo aditivo.</param>
        /// <param name="pos">Centro (coords tal cual lleguen).</param>
        /// <param name="radio">Radio del glow NÚCLEO en px (las bandas crecen desde él).</param>
        /// <param name="color">El color de la LUZ (las bandas externas viran a blanco).</param>
        /// <param name="intensidad">Multiplicador global 0..1 (por defecto 1).</param>
        public static void BloomTriple(SpriteBatch batch, Vector2 pos, float radio,
            Color color, float intensidad = 1f)
        {
            if (intensidad <= 0.02f || radio < 2f) return;

            // El blanco cálido de la casa (el mismo corazón de Bloom/Flare/Ray).
            Color blanco = new Color(255, 250, 240);

            // De FUERA hacia DENTRO (el orden del bloom apilado de la casa):
            // 1) la BANDA AMPLIA — 3.4× el radio, casi un susurro (α 0.13·i):
            //    la atmósfera que "se derrama" muy lejos del centro.
            Quad(batch, GlowTex, pos, new Vector2(radio * 6.8f, radio * 6.8f), 0f,
                Tint(Color.Lerp(color, blanco, 0.55f), 0.13f * intensidad));
            // 2) la BANDA MEDIA — 1.9× el radio, el cuerpo del resplandor
            //    (α 0.28·i), ya recalentada un 30% hacia blanco.
            Quad(batch, GlowTex, pos, new Vector2(radio * 3.8f, radio * 3.8f), 0f,
                Tint(Color.Lerp(color, blanco, 0.30f), 0.28f * intensidad));
            // 3) la BANDA NÚCLEO — 1.0× el radio, la que lleva el COLOR puro
            //    (α 0.55·i): la identidad cromática vive en el centro.
            Quad(batch, GlowTex, pos, new Vector2(radio * 2f, radio * 2f), 0f,
                Tint(color, 0.55f * intensidad));
        }

        // ==================================================================
        //  2. LA DOBLE PASADA — el contrato de la luz que EMANA
        // ==================================================================

        /// <summary>
        /// LA DOBLE PASADA universal: capa de COLOR (alpha ÷2, escala ×1.15)
        /// sobre capa BLANCA (alpha ÷2, escala ×1.0) con la MISMA textura —
        /// el truco que hace leer "energía viva" cualquier sprite de luz.
        /// </summary>
        public static void DoublePass(SpriteBatch batch, Texture2D tex, Vector2 pos,
            Vector2 size, float rot, Color color, float intensity)
        {
            if (intensity <= 0.02f) return;
            // 1) la capa de COLOR, MÁS GRANDE.
            Quad(batch, tex, pos, size * 1.15f, rot, Tint(color, 0.50f * intensity));
            // 2) la capa BLANCA (el corazón), a escala justa.
            Color white = Color.Lerp(color, new Color(255, 250, 240), 0.75f);
            Quad(batch, tex, pos, size, rot, Tint(white, 0.50f * intensity));
        }

        // ==================================================================
        //  3. EL DESTELLO DE 4 PUNTAS — "está ARDIENDO"
        // ==================================================================

        /// <summary>
        /// EL DESTELLO: LumenFlare con su cruz principal + cruz diagonal +
        /// punto caliente, apilado sobre un bloom interior. `size` = brazo
        /// del destello mayor en px. `spin` gira el destello (lento, vivo).
        /// </summary>
        public static void Flare(SpriteBatch batch, Vector2 pos, float size,
            Color color, float intensity, float spin = 0f)
        {
            if (intensity <= 0.02f) return;
            Color white = Color.Lerp(color, new Color(255, 250, 240), 0.6f);
            // la cruz principal, girando si toca
            Quad(batch, FlareTex, pos, new Vector2(size, size), spin,
                Tint(color, 0.75f * intensity));
            // la cruz diagonal interna (×0.62, blanca — el corazón)
            Quad(batch, FlareTex, pos, new Vector2(size * 0.62f, size * 0.62f),
                spin + MathHelper.PiOver4, Tint(white, 0.85f * intensity));
            // el punto caliente
            Quad(batch, BloomTex, pos, new Vector2(size * 0.34f, size * 0.34f), 0f,
                Tint(white, 0.95f * intensity));
        }

        // ==================================================================
        //  4. EL RAYO DE SOL — el haz estirado de grosor animado
        // ==================================================================

        /// <summary>
        /// EL RAYO DE SOL: la textura de bloom ESTIRADA a lo largo de la
        /// dirección (elipse con centro caliente — el haz de verdad) en
        /// TRES capas: velo ×1.6, cuerpo ×1.0 y núcleo blanco ×0.3, con el
        /// GROSOR RESPIRANDO (0.25→0.7 del ancho nominal — el pulso del
        /// rayo). El origen EXACTO queda en `origin` y el extremo lejano
        /// se desvanece (punta viva).
        /// </summary>
        /// <param name="origin">El punto de emisión (coords tal cual).</param>
        /// <param name="dir">Dirección UNITARIA del haz.</param>
        /// <param name="length">Longitud del haz en px.</param>
        /// <param name="width">Grosor NOMINAL del cuerpo en px.</param>
        /// <param name="pulse">0..1 — la fase del grosor animado.</param>
        public static void Ray(SpriteBatch batch, Vector2 origin, Vector2 dir,
            float length, float width, Color color, float intensity, float pulse)
        {
            if (intensity <= 0.02f || length < 2f) return;
            if (dir.LengthSquared() < 0.001f) dir = -Vector2.UnitY;
            dir.Normalize();

            // El centro del quad: a media longitud (el origen queda EXACTO).
            Vector2 center = origin + dir * (length * 0.5f);
            float rot = (float)Math.Atan2(dir.Y, dir.X);

            // EL GROSOR ANIMADO: 0.62..1.0 (la respiración del haz).
            float thick = 0.62f + 0.38f * MathHelper.Clamp(pulse, 0f, 1f);

            // 1) el VELO (×1.6 ancho, muy tenue — la atmósfera del haz).
            Quad(batch, BloomTex, center, new Vector2(length, width * 1.6f * thick), rot,
                Tint(color, 0.22f * intensity));
            // 2) el CUERPO (el haz de color).
            Quad(batch, BloomTex, center, new Vector2(length, width * 1.0f * thick), rot,
                Tint(color, 0.60f * intensity));
            // 3) el NÚCLEO BLANCO (×0.3 — la vena caliente).
            Color white = Color.Lerp(color, new Color(255, 250, 240), 0.8f);
            Quad(batch, BloomTex, center, new Vector2(length * 0.96f, width * 0.30f * thick), rot,
                Tint(white, 0.90f * intensity));

            // La BOCA del rayo: bloom pequeño cegador en el origen.
            Bloom(batch, origin, width * 1.6f, color, intensity * 0.8f, 2);
        }

        // ==================================================================
        //  5. LA LANZA DE LUZ — la hoja con telegraph y estela de fantasmas
        // ==================================================================

        /// <summary>
        /// LA LANZA: la hoja de luz (LumenBlade) orientada a `dir` con la
        /// DOBLE PASADA (color ×1.0 + blanca ×0.86 acortada) y el hue
        /// viviendo su drift. `length` = longitud total de la hoja.
        /// </summary>
        public static void Lance(SpriteBatch batch, Vector2 pos, Vector2 dir,
            float length, float width, Color color, float intensity)
        {
            if (intensity <= 0.02f || length < 4f) return;
            if (dir.LengthSquared() < 0.001f) dir = -Vector2.UnitY;
            dir.Normalize();
            float rot = (float)Math.Atan2(dir.Y, dir.X);

            // La textura apunta ARRIBA (−Y): rotar desde −π/2 al dir.
            float texRot = rot + MathHelper.PiOver2;
            Vector2 size = new Vector2(width * 2.0f, length);

            // 1) la capa de COLOR (más grande).
            Quad(batch, BladeTex, pos, size * 1.12f, texRot, Tint(color, 0.55f * intensity));
            // 2) la capa BLANCA (el corazón de la hoja).
            Color white = Color.Lerp(color, new Color(255, 250, 240), 0.72f);
            Quad(batch, BladeTex, pos, size, texRot, Tint(white, 0.60f * intensity));
        }

        /// <summary>
        /// LA ESTELA DE LA LANZA: N copias fantasma que se quedan ATRÁS a
        /// lo largo de la dirección CONTRARIA, con escala CRECIENDO (hasta
        /// ×1.4 — los fantasmas de la Emperatriz crecen hacia atrás) y
        /// alpha decayendo por índice ((1−i/N)^1.6).
        /// </summary>
        public static void LanceTrail(SpriteBatch batch, Vector2 pos, Vector2 dir,
            float length, float width, Color color, float intensity,
            int ghosts = 8, float spread = 26f)
        {
            if (intensity <= 0.02f || ghosts < 1) return;
            if (dir.LengthSquared() < 0.001f) dir = -Vector2.UnitY;
            dir.Normalize();
            float texRot = (float)Math.Atan2(dir.Y, dir.X) + MathHelper.PiOver2;

            for (int i = 0; i < ghosts; i++)
            {
                float t = i / (float)ghosts;
                // el fantasma i: atrás a lo largo del vuelo, escala creciente.
                float scale = 1f + 0.4f * t;
                float alpha = (float)Math.Pow(1f - t, 1.6) * 0.55f * intensity;
                if (alpha <= 0.02f) continue;
                Vector2 gpos = pos - dir * (spread * i);
                Vector2 size = new Vector2(width * 2.0f * scale, length * scale);
                Quad(batch, BladeTex, gpos, size, texRot, Tint(color, alpha));
            }
        }

        /// <summary>
        /// EL TELEGRAPH DE LA LANZA: la raya fina de aviso (núcleo brillante
        /// a media longitud + velo a longitud completa) que anuncia el
        /// golpe antes de llegar — `progress` 0..1 engorda la línea.
        /// </summary>
        public static void Telegraph(SpriteBatch batch, Vector2 origin, Vector2 dir,
            float length, Color color, float progress)
        {
            if (dir.LengthSquared() < 0.001f) dir = -Vector2.UnitY;
            dir.Normalize();
            float rot = (float)Math.Atan2(dir.Y, dir.X);
            Vector2 center = origin + dir * (length * 0.5f);
            float grow = MathHelper.Clamp(progress, 0f, 1f);

            // el VELO a longitud completa.
            Quad(batch, BloomTex, center, new Vector2(length, 10f * grow + 4f), rot,
                Tint(color, 0.22f * grow));
            // el NÚCLEO a media longitud.
            Vector2 halfCenter = origin + dir * (length * 0.25f);
            Color white = Color.Lerp(color, new Color(255, 250, 240), 0.7f);
            Quad(batch, BloomTex, halfCenter, new Vector2(length * 0.5f, 5f * grow + 2f), rot,
                Tint(white, 0.55f * grow));
            // el ANILLO objetivo en el punto final.
            Vector2 end = origin + dir * length;
            float ringR = 26f + 8f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 9f);
            Quad(batch, VFXCore.Ring, end, VFXCore.RingQuadSize(ringR),
                Main.GlobalTimeWrappedHourly * 2.2f, Tint(color, 0.45f * grow));
        }

        // ==================================================================
        //  6. EL AURORA — las bandas prismáticas de muerte/renacimiento
        // ==================================================================

        /// <summary>
        /// EL AURORA: bandas verticales espejadas (rot = π/2 ± vaivén + π·i
        /// — alternando 180°) con DOS vueltas de hue en las bandas y dos
        /// frecuencias de vaivén. La muerte prismática de la Emperatriz.
        /// </summary>
        public static void Aurora(SpriteBatch batch, Vector2 center, float radius,
            float time, float hueBase, float intensity, int bands = 15)
        {
            if (intensity <= 0.02f) return;
            for (int i = 0; i < bands; i++)
            {
                float f = i / (float)bands;
                // DOS frecuencias (la fase y su doble — el vaivén orgánico).
                float sway = 0.14f * (float)Math.Sin(time * 1.7f + f * MathHelper.TwoPi)
                           + 0.07f * (float)Math.Sin(time * 3.4f + f * (MathHelper.TwoPi * 2f));
                float rot = MathHelper.PiOver2 + sway + MathHelper.Pi * (i % 2);
                // DOS vueltas de hue en las bandas + variante senoidal.
                float hue = hueBase + f * 2f + 0.06f * (float)Math.Sin(f * (MathHelper.TwoPi * 2f) + time * 0.9f);
                Color c = Hue(hue, 0.55f, 1f);

                Vector2 dir = new Vector2((float)Math.Cos(rot), (float)Math.Sin(rot));
                Vector2 pos = center + dir * (radius * (0.55f + 0.45f * f));
                Vector2 size = new Vector2(radius * 0.5f, radius * 0.18f);
                // alpha ÷4 (las bandas son VELO, no cuerpo).
                Quad(batch, BloomTex, pos, size, rot, Tint(c, 0.25f * intensity));
            }
        }

        // ==================================================================
        //  7. LA LUZ DEL MUNDO — muestreada a lo largo del camino
        // ==================================================================

        /// <summary>
        /// LA LUZ MUESTREADA: AddLight cada `everyPx` píxeles del segmento
        /// (la lección del muestreo — no por frame, no por punto), con el
        /// color al nivel L=0.85 (la luz que la Emperatriz SÍ emite).
        /// </summary>
        public static void LightAlong(Vector2 a, Vector2 b, Color color,
            float strength = 1f, float everyPx = 80f)
        {
            if (strength <= 0.02f) return;
            Vector2 seg = b - a;
            float len = seg.Length();
            if (len < 1f) { Lighting.AddLight(a, color.ToVector3() * strength); return; }
            int steps = Math.Max(1, (int)(len / everyPx));
            for (int i = 0; i <= steps; i++)
            {
                Vector2 p = a + seg * (i / (float)steps);
                Lighting.AddLight(p, color.ToVector3() * strength);
            }
        }

        // ==================================================================
        //  PRIMITIVAS INTERNAS
        // ==================================================================

        /// <summary>Quad centrado con rotación (tamaño total = size px).</summary>
        private static void Quad(SpriteBatch batch, Texture2D tex, Vector2 pos,
            Vector2 size, float rot, Color tint)
        {
            if (tint.A == 0) return;
            batch.Draw(tex, pos, null, tint, rot,
                new Vector2(tex.Width, tex.Height) * 0.5f,
                size / new Vector2(tex.Width, tex.Height),
                SpriteEffects.None, 0f);
        }

        /// <summary>Tinte de INTENSIDAD LINEAL (patrón validado del proyecto).</summary>
        private static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color(c.R, c.G, c.B, (byte)(int)(255f * f));
        }
    }
}
