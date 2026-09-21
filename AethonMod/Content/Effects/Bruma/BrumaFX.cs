using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Effects.Bruma
{
    /// <summary>
    /// BrumaFX — v6.25 — LA LIBRERÍA DE HUMO / NIEBLA / BRUMA PROCEDURAL
    /// (segunda generación).
    ///
    /// v6.17: la caja de herramientas 100% PROCEDURAL (puffs con textura
    /// fBm nacida de código, movimiento orgánico por senos inconmensurables
    /// + curl noise, composición INVARIANTE DE ESCALA).
    ///
    /// v6.25 — LA RECONSTRUCCIÓN tras la investigación de 23 fuentes
    /// (research/humo_v625/INFORME_MODS_HUMO.md — vanilla decompilada,
    /// los mejores renderizadores del ecosistema):
    ///
    ///   1. TINTE PREMULTIPLICADO (RGB×f): el fix del bug de los
    ///      rectángulos — el pincel horneado es premultiplicado y el
    ///      tinte también, así la INTENSIDAD manda en el lote aditivo
    ///      (donde el alfa se ignora) y los bordes son suaves en ambos.
    ///   2. FLIPBOOK de ruido evolucionado: los puffs ciclan frames en
    ///      ping-pong lento (ambiente) o por VIDA (AnimatedPuff/Vapor) —
    ///      el humo SE DESGARRA, no solo rota (anti-patrón nº3).
    ///   3. ESCALERA de texturas 64/128/160 por radio (lección de la investigación interna).
    ///   4. LUZ DEL MUNDO con piso: WorldTint(pos) — el humo VIVE en
    ///      cuevas (factor 0.25..1 por canal, tintado por la luz local).
    ///   5. VIENTO del mundo: Main.windSpeedCurrent empuja Column/Tendril/
    ///      MistBand/Vapor — humo de un mundo con clima (lección nº8).
    ///   6. SMEAR vanilla: N copias del núcleo por velocidad (dusts 130).
    ///   7. Rampa de ENFRIAMIENTO por vida (familia Spirit: color→gris)
    ///      + brasa que EMITE luz mientras arde (lección nº12).
    ///   8. PRESUPUESTO de quads con LOD automático (lección nº22).
    ///
    /// LA INVARIANCIA DE ESCALA (que se vea bien a 10px y a 500px):
    ///   1. Sub-blobs ∝ PERÍMETRO (n ≈ 0.45·radio), no área.
    ///   2. Presupuesto de alfa de COBERTURA CONSTANTE: aBlob =
    ///      1−(1−A)^(1/(n+1)) — misma densidad óptica a cualquier n.
    ///   3. Respiración DESFASADA por blob + rotaciones independientes
    ///      del tamaño + flipbook ping-pong con semilla por puff.
    ///
    /// EL CONTRATO DE LOTE (v6.10, sin cambios): todos los métodos
    /// dibujan en el SpriteBatch ABIERTO que el llamador tenga (y NO lo
    /// tocan). El llamador ELIGE EL MODO ( BeginMass/BeginGlow ayudan):
    ///   · BeginGlow()/Additive   → humo LUMINOSO (bruma mágica, nebulosa).
    ///   · BeginMass()/AlphaBlend → humo QUE OCLUYE (masa, sombra, tinta).
    /// (Pincel neutro premultiplicado: funciona correcto en AMBOS.)
    ///
    /// TODO determinista por semilla: misma secuencia SIEMPRE, cero
    /// estado de ondas, cero red, cero GC por frame.
    ///
    /// LA API:
    ///   · Puff(...)          — la unidad de humo AMBIENTE (cicla el
    ///                          flipbook en ping-pong lento).
    ///   · AnimatedPuff(...)  — la unidad con VIDA (flipbook por vida +
    ///                          rampa de enfriamiento + brasa opcional).
    ///   · Vapor(...)         — el humo ALFA de 1 capa (LUT dura, luz
    ///                          del mundo por defecto — el look moderno).
    ///   · Cloud(...)         — racimo de puffs (nube/voluntad de humo).
    ///   · Tendril(...)       — voluta serpenteando por una RUTA.
    ///   · Wisps(...)         — voluta + motas de brasa (lección nº24).
    ///   · Column(...)        — columna ascendente (nace, crece, disipa).
    ///   · MistBand(...)      — banda de niebla con paralaje.
    /// </summary>
    public static class BrumaFX
    {
        // ==================================================================
        //  PRESUPUESTO — el LOD de vanilla aplicado a la librería
        // ==================================================================

        /// <summary>
        /// Techo de quads de bruma por frame: superado esto, los puffs
        /// NUEVOS degradan su calidad (menos sub-blobs) automáticamente.
        /// </summary>
        private const int PresupuestoQuads = 500;

        private static int _frameQuads;
        private static uint _frameId;

        /// <summary>Cuenta un puff en el presupuesto del frame.</summary>
        private static void Cotizar(int quads)
        {
            uint f = Main.GameUpdateCount;
            if (f != _frameId) { _frameId = f; _frameQuads = 0; }
            _frameQuads += quads;
        }

        /// <summary>¿El presupuesto ya está agotado? (LOD: bajar calidad).</summary>
        private static bool Agotado
        {
            get
            {
                uint f = Main.GameUpdateCount;
                if (f != _frameId) { _frameId = f; _frameQuads = 0; }
                return _frameQuads > PresupuestoQuads;
            }
        }

        // ==================================================================
        //  EL PINCEL SUAVE (sub-blobs) Y LOS LOTES DE LA CASA
        // ==================================================================

        private static Asset<Texture2D> _softGlow;

        /// <summary>El pincel suave genérico de la biblioteca VFX del mod.</summary>
        private static Texture2D SoftGlow =>
            (_softGlow ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow")).Value;

        /// <summary>
        /// ABRE el lote de MASA (AlphaBlend + LinearClamp — el humo que
        /// OCLUYE). El llamador lo CIERRA con Main.spriteBatch.End().
        /// </summary>
        public static void BeginMass()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>
        /// ABRE el lote de BRILLO (Additive — las venas/brasa luminosas).
        /// El llamador lo CIERRA con Main.spriteBatch.End().
        /// </summary>
        public static void BeginGlow()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
        }

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

        /// <summary>
        /// Quad de UN FRAME del flipbook (tira vertical): sourceRect de la
        /// fila <paramref name="frame"/>, centrado, tamaño final = size px.
        /// </summary>
        private static void QuadFrame(Texture2D tex, int frame, int frameSize,
            Vector2 pos, Vector2 size, float rot, Color tint)
        {
            if (tex == null || tint.A == 0) return;
            var src = new Rectangle(0, frame * frameSize, frameSize, frameSize);
            Main.spriteBatch.Draw(tex, pos, src, tint, rot,
                new Vector2(frameSize * 0.5f, frameSize * 0.5f),
                size / new Vector2(frameSize, frameSize),
                SpriteEffects.None, 0f);
        }

        /// <summary>
        /// Tinte de INTENSIDAD LINEAL PREMULTIPLICADO (el fix v6.25): RGB
        /// Y alfa escalados por f — la intensidad manda también en lotes
        /// aditivos (donde el canal alfa se ignora) y los bordes de las
        /// texturas premultiplicadas quedan suaves en cualquier lote.
        /// </summary>
        private static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            // v6.50.3 — FIX (sonda IL contra el FNA real): BlendState.Additive
            // de FNA es (SourceAlpha, One) — el alfa GATEA el aporte. El Tint
            // premultiplicado v6.25 atenuaba DOS VECES (intensidad real f²:
            // el halo 0.30 salía a 0.09). RGB intacto, alfa=f: LINEAL.
            return new Color(c.R, c.G, c.B, (byte)(int)(255f * f));
        }

        /// <summary>Hash determinista [0,1) (BrumaNoise, sin estado).</summary>
        private static float H01(int seed, int a, int b) => BrumaNoise.Hash(a, b, seed);

        // ==================================================================
        //  LA LUZ DEL MUNDO — el humo vive en cuevas (lección de la investigación interna)
        // ==================================================================

        /// <summary>
        /// TINTE POR LA LUZ DEL MUNDO con piso (nunca invisible): factor
        /// por canal = 0.25 + 0.75·(luz/255) — en pleno día factor 1 (sin
        /// cambio), en cueva 0.25 (tenue pero visible), bajo una antorcha
        /// el humo se TINTA cálido. Asume coordenadas de PANTALLA del lote
        /// estándar (GameViewMatrix): mundo = pos + Main.screenPosition.
        /// </summary>
        public static Color WorldTint(Vector2 screenPos, Color color)
        {
            if (Main.netMode == NetmodeID.Server) return color;
            Color L = Lighting.GetColor(
                (int)((screenPos.X + Main.screenPosition.X) / 16f),
                (int)((screenPos.Y + Main.screenPosition.Y) / 16f));
            float fr = 0.25f + 0.75f * (L.R / 255f);
            float fg = 0.25f + 0.75f * (L.G / 255f);
            float fb = 0.25f + 0.75f * (L.B / 255f);
            return new Color(
                (byte)(int)(color.R * fr), (byte)(int)(color.G * fg),
                (byte)(int)(color.B * fb), color.A);
        }

        /// <summary>El VIENTO del mundo (px de empuje por unidad de viento).</summary>
        private static float VientoMundo => Main.windSpeedCurrent;

        // ==================================================================
        //  PUFF — LA UNIDAD DE HUMO AMBIENTE (flipbook en ping-pong)
        // ==================================================================

        /// <summary>
        /// DIBUJA UN PUFF de humo en <paramref name="center"/> con radio
        /// <paramref name="radius"/> (px): núcleo texturizado (frame del
        /// flipbook ciclando en PING-PONGO lento — el ruido morfa, no
        /// salta) + borde de sub-blobs que respiran desfasados + smear
        /// vanilla por velocidad. Se ve bien a CUALQUIER escala.
        /// </summary>
        /// <param name="center">Centro en el espacio del lote abierto.</param>
        /// <param name="radius">Radio del puff en px (8..500+: todos bien).</param>
        /// <param name="color">Color/masa (el alfa del color se ignora).</param>
        /// <param name="seed">Semilla determinista (firma visual propia).</param>
        /// <param name="time">Tiempo animado (p. ej. GlobalTimeWrappedHourly).</param>
        /// <param name="alpha">Densidad óptica total objetivo [0..1].</param>
        /// <param name="quality">0.3..1 — densidad de sub-blobs del borde.</param>
        /// <param name="velocity">Velocidad (smear vanilla del núcleo).</param>
        /// <param name="worldLit">Tintar por la luz del mundo con piso.</param>
        public static void Puff(Vector2 center, float radius, Color color, int seed,
            float time, float alpha = 0.55f, float quality = 1f, Vector2 velocity = default,
            bool worldLit = false)
        {
            if (worldLit) color = WorldTint(center, color);
            PuffBody(center, radius, color, seed, time, alpha, quality, velocity, -1);
        }

        /// <summary>
        /// EL CUERPO compartido del puff: núcleo (frame ping-pong si
        /// frameOverride &lt; 0, si no EL frame dado) + sub-blobs + smear.
        /// </summary>
        private static void PuffBody(Vector2 center, float radius, Color color, int seed,
            float time, float alpha, float quality, Vector2 velocity, int frameOverride)
        {
            if (radius < 1.5f) return;
            alpha = MathHelper.Clamp(alpha, 0f, 1f);

            // --- EL ESCALÓN de la escalera (64/128/160 por radio) ---
            int tier = BrumaBrushes.TierForRadius(radius);
            Texture2D tex = BrumaBrushes.Puff(seed, tier);
            if (tex == null) return;
            int frameSize = BrumaBrushes.SizeOf(tier);
            int frames = BrumaBrushes.FramesOf(tier);

            // --- EL FRAME: ping-pong lento (0.55 ciclos/s — morfa suave,
            //     la vuelta atrás disimula el salto del bucle), o EL
            //     frame de la VIDA si el llamador lo trae (AnimatedPuff). ---
            int frame;
            if (frameOverride >= 0)
            {
                frame = Math.Min(frameOverride, frames - 1);
            }
            else
            {
                float cyc = (time * 0.55f + seed * 0.37f) % 2f;
                if (cyc < 0f) cyc += 2f;
                float ff = cyc < 1f ? cyc : 2f - cyc;
                frame = Math.Min((int)(ff * frames), frames - 1);
            }

            // --- LOD: con el presupuesto agotado, menos sub-blobs. ---
            if (Agotado) quality *= 0.5f;

            // --- 1. EL NÚCLEO: el frame del flipbook, rotando LENTO por
            //        semilla (rad/s constante — INDEPENDIENTE del tamaño). ---
            float rot = time * 0.10f * ((seed & 1) == 0 ? 1f : -1f);
            float breathe = 1f + 0.08f * (float)Math.Sin(time * 0.6f + seed);
            QuadFrame(tex, frame, frameSize, center,
                new Vector2(radius * 2f * breathe, radius * 2f * breathe),
                rot, Tint(color, alpha * 0.85f));

            // --- 1b. EL SMEAR vanilla (dusts 130-134): N copias del
            //        NÚCLEO cayendo atrás por la velocidad. ---
            float vlen = velocity.Length();
            if (vlen > 2.5f)
            {
                int copias = Math.Clamp((int)(vlen * 0.8f), 1, 6);
                for (int j = 1; j <= copias; j++)
                {
                    Vector2 sp = center - velocity * (j * 0.6f);
                    float ss = 1f - j / (copias + 2f);
                    QuadFrame(tex, frame, frameSize, sp,
                        new Vector2(radius * 2f * breathe * ss, radius * 2f * breathe * ss),
                        rot, Tint(color, alpha * 0.5f * ss));
                }
            }

            // --- 2. EL BORDE: sub-blobs ∝ PERÍMETRO con presupuesto de
            //        alfa de COBERTURA CONSTANTE. ---
            int n = (int)Math.Clamp(MathF.Round(radius * 0.45f * quality), 2f, 14f);
            float aBlob = 1f - MathF.Pow(1f - alpha * 0.5f, 1f / (n + 1));

            // Estiramiento por velocidad (smear de los dusts de vanilla).
            float trail = MathHelper.Clamp(vlen * 0.02f, 0f, 1f);

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
                Vector2 dir = vlen > 0.01f ? velocity / vlen : Vector2.Zero;
                Vector2 size = new Vector2(
                    blobR * 2f * (1f + trail * 0.8f * Math.Abs(dir.X)),
                    blobR * 2f * (1f + trail * 0.8f * Math.Abs(dir.Y)));

                Quad(SoftGlow, pos, size, brot, Tint(color, aBlob));
            }

            Cotizar(2 + n);
        }

        // ==================================================================
        //  ANIMATEDPUFF — la unidad con VIDA (flipbook por vida)
        // ==================================================================

        /// <summary>
        /// DIBUJA UN PUFF con su CICLO DE VIDA completo (lecciones la investigación interna/
        /// élite/Spirit): flipbook avanzando POR VIDA (el humo se
        /// DESGARRA de verdad), envolvente nacimiento-rápido/muerte-lenta,
        /// crecimiento ×1.5, Rampa de enfriamiento opcional (color →
        /// endColor — la historia térmica de la bocanada) y BRASA que
        /// emite luz de su color mientras arde (primer 40% de vida).
        /// </summary>
        /// <param name="life01">Vida: 0 = recién nacido → 1 = disolviéndose.</param>
        /// <param name="endColor">Color final de la rampa de enfriamiento
        /// (p. ej. gris (25,25,25) para humo de arma). Null = sin rampa.</param>
        /// <param name="ember">True = emite luz del color mientras arde.</param>
        public static void AnimatedPuff(Vector2 center, float radius, Color color, int seed,
            float life01, float time, float alpha = 0.5f, Color? endColor = null,
            bool worldLit = false, Vector2 velocity = default, bool ember = false)
        {
            if (radius < 1.5f) return;
            life01 = MathHelper.Clamp(life01, 0f, 1f);
            if (worldLit) color = WorldTint(center, color);

            // --- La Rampa de ENFRIAMIENTO (familia Spirit/élite): el
            //     color cuenta la historia térmica al morir. ---
            if (endColor.HasValue)
                color = Color.Lerp(color, endColor.Value,
                    BrumaNoise.Smoothstep(0.55f, 1.0f, life01));

            // --- LA ENVOLVENTE: nace rápido (18% de vida), muere LENTO
            //     (45% final de disolución — nunca fade lineal puro). ---
            float fadeIn = BrumaNoise.Smoothstep(0f, 0.18f, life01);
            float fadeOut = 1f - BrumaNoise.Smoothstep(0.55f, 1f, life01);
            float a = MathHelper.Clamp(alpha, 0f, 1f) * fadeIn * fadeOut;

            // --- LA BRASA EMITE LUZ mientras arde (lección nº12). ---
            if (ember && life01 < 0.4f && a > 0.05f && Main.netMode != NetmodeID.Server)
            {
                Vector3 luz = new Vector3(color.R / 255f, color.G / 255f, color.B / 255f) * (0.1f * a);
                Lighting.AddLight(center + Main.screenPosition, luz);
            }

            // --- EL CRECIMIENTO: nace a ×0.8 y crece a ×1.5 (pow 0.8 —
            //     nunca aparece a tamaño final: regla de nacimiento). ---
            float grow = 0.8f + 0.7f * MathF.Pow(life01, 0.8f);

            // --- EL FRAME POR VIDA: el desgarro del flipbook ACELERA
            //     hacia la muerte (frame = vida·frames). ---
            int tier = BrumaBrushes.TierForRadius(radius * grow);
            int frames = BrumaBrushes.FramesOf(tier);
            int frame = Math.Min((int)(life01 * frames), frames - 1);

            PuffBody(center, radius * grow, color, seed, time, a,
                quality: 1f, velocity, frame);
        }

        // ==================================================================
        //  VAPOR — el humo ALFA de una capa (LUT dura — el look moderno)
        // ==================================================================

        /// <summary>
        /// DIBUJA UN VAPOR (estilo el vapor de élite): pincel de LUT DURA
        /// (núcleo denso, borde que muere en bruma gruesa), LUZ DEL MUNDO
        /// por defecto (el vapor físico se apaga en las sombras), muerte
        /// rápida (fade-out desde el 45%) y viento del mundo en el drift
        /// (la velocidad dada se inclina con el clima real).
        /// </summary>
        /// <param name="life01">Vida: 0 = recién nacido → 1 = disolviéndose.</param>
        public static void Vapor(Vector2 center, float radius, Color color, int seed,
            float life01, float time, float alpha = 0.42f, Vector2 velocity = default)
        {
            if (radius < 1.5f) return;
            life01 = MathHelper.Clamp(life01, 0f, 1f);
            alpha = MathHelper.Clamp(alpha, 0f, 1f);
            color = WorldTint(center, color);

            // El VIENTO del mundo inclina la deriva (mundo con clima).
            // La FLOTABILIDAD la decide el llamador con velocity
            // (vapor de agua: velocity.Y negativa; gas del vacío: positiva).
            Vector2 vel = velocity + new Vector2(VientoMundo * 0.35f, 0f);

            int tier = Math.Min(BrumaBrushes.TierForRadius(radius), 1); // solo 64/128
            Texture2D tex = BrumaBrushes.Vapor(seed, tier);
            if (tex == null) return;
            int frameSize = BrumaBrushes.SizeOf(tier);
            int frames = 6;
            int frame = Math.Min((int)(life01 * frames), frames - 1);

            // Envolvente: nace al 12%, muere desde el 45% (vapor efímero).
            float fadeIn = BrumaNoise.Smoothstep(0f, 0.12f, life01);
            float fadeOut = 1f - BrumaNoise.Smoothstep(0.45f, 1f, life01);
            float a = alpha * fadeIn * fadeOut;

            // Crecimiento +0.1/frame hasta ×1.6 (hoguera: escala +0.1/tick).
            float grow = 0.85f + 0.75f * life01;

            // El drift de la vida: el puff se MUEVE con su velocidad.
            Vector2 pos = center + vel * life01 * 8f;

            float rot = time * (0.05f + 0.03f * (seed & 1)) * ((seed & 1) == 0 ? 1f : -1f);
            QuadFrame(tex, frame, frameSize, pos,
                new Vector2(radius * 2f * grow, radius * 2f * grow), rot,
                Tint(color, a));

            // Núcleo interior doble (densidad ×1.5 gratis — doble draw).
            QuadFrame(tex, frame, frameSize, pos,
                new Vector2(radius * 1.3f * grow, radius * 1.3f * grow), rot,
                Tint(color, a * 0.5f));

            Cotizar(2);
        }

        // ==================================================================
        //  CLOUD — un racimo de puffs (nube / bocanada)
        // ==================================================================

        /// <summary>
        /// DIBUJA UNA NUBE: <paramref name="puffs"/> racimos alrededor del
        /// centro, cada uno con su semilla, escala y fase — la masa crece
        /// hacia el interior y los bordes mueren en volutas. Con
        /// <paramref name="worldLit"/> la nube física VIVE en cuevas.
        /// </summary>
        public static void Cloud(Vector2 center, float radius, Color color, int seed,
            float time, int puffs = 5, float alpha = 0.5f, bool worldLit = false)
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
                    MathHelper.Clamp(a, 0.05f, 0.85f), quality: 0.7f, worldLit: worldLit);
            }
        }

        // ==================================================================
        //  TENDRIL — una voluta serpenteando por una ruta
        // ==================================================================

        /// <summary>
        /// DIBUJA UNA VOLUTA de humo siguiendo <paramref name="path"/> (2..n
        /// puntos): puffs encadenados con radio variable (perfil fino-grueso
        /// -fino), balanceo por senos inconmensurables, desvanecimiento
        /// hacia el extremo final y VIENTO del mundo en el balanceo.
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

            // El VIENTO del mundo inclina la voluta (mundo con clima).
            float windLean = VientoMundo * width * 0.08f;

            for (int s = 0; s <= steps; s++)
            {
                float f = s / (float)steps;   // 0 inicio → 1 final de la ruta

                // Posición sobre la polilínea (por longitud de arco).
                Vector2 pos = PuntoEnRuta(path, f * total);

                // Perfil de ancho: fino → grueso (al 55%) → fino.
                float prof = MathF.Sin(f * MathHelper.Pi * 0.9f + 0.15f);
                float r = width * 0.5f * MathHelper.Clamp(prof, 0.25f, 1f);

                // Balanceo perpendicular: dos senos INCONMENSURABLES + viento.
                float sway = MathF.Sin(time * 0.5f + f * 4.5f + seed) * width * 0.16f
                           + MathF.Sin(time * 1.13f + f * 9.1f + seed * 2) * width * 0.07f
                           + windLean;

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
        //  WISPS — voluta + motas de brasa (lección nº24: el detalle
        //  más barato que más vende — el humo acompañado VIVE)
        // ==================================================================

        /// <summary>
        /// DIBUJA UNA VOLUTA CON BRASAS: la voluta de siempre + 2-4 motas
        /// pequeñas y brillantes flotando a lo largo de la ruta (la brasa
        /// que acompaña al humo — "humo solo nunca parece vivo; humo + 3
        /// chispas + 1 luz sí").
        /// </summary>
        public static void Wisps(Vector2[] path, float width, Color color, Color moteColor,
            int seed, float time, float alpha = 0.5f, float fade = 0.75f, int motes = 3)
        {
            Tendril(path, width, color, seed, time, alpha, fade);

            if (path == null || path.Length < 2 || motes <= 0) return;

            float total = 0f;
            for (int i = 1; i < path.Length; i++)
                total += Vector2.Distance(path[i - 1], path[i]);
            if (total < 1f) return;

            for (int m = 0; m < motes; m++)
            {
                float h1 = H01(seed, 500 + m, 17);
                float f = 0.2f + 0.6f * h1;
                Vector2 basePos = PuntoEnRuta(path, f * total);

                // Flotación propia: dos senos inconmensurables.
                Vector2 pos = basePos + new Vector2(
                    MathF.Sin(time * 0.71f + h1 * 6.28f) * width * 0.30f,
                    MathF.Sin(time * 1.13f + h1 * 9.42f) * width * 0.22f - width * 0.25f);

                // La mota muere hacia el final de la ruta.
                float a = alpha * (1f - fade * f) * (0.4f + 0.6f * H01(seed, 510 + m, 19));
                float sz = width * (0.10f + 0.08f * h1);

                Quad(SoftGlow, pos, new Vector2(sz * 2f, sz * 2f), 0f, Tint(moteColor, a));
                Quad(SoftGlow, pos, new Vector2(sz, sz), 0f, Tint(moteColor, a * 0.9f));
            }
        }

        // ==================================================================
        //  COLUMN — columna de humo ascendente
        // ==================================================================

        /// <summary>
        /// DIBUJA UNA COLUMNA de humo que nace en <paramref name="basePos"/>
        /// y asciende <paramref name="height"/> px: cada puff nace pequeño,
        /// crece al subir, se balancea con el viento (el del MUNDO incluido)
        /// y SE DISUELVE por erosión (el ruido decide qué muere primero).
        /// </summary>
        /// <param name="speed">Puffs por segundo que recorren la columna.</param>
        public static void Column(Vector2 basePos, float height, float width, Color color,
            int seed, float time, float alpha = 0.5f, float speed = 0.35f)
        {
            if (height < 4f || width < 1.5f) return;

            // Un puff por cada tramo de ~medio ancho de la columna.
            int count = Math.Clamp((int)(height / (width * 0.9f)), 3, 18);

            // El VIENTO del mundo empuja la columna al subir (clima real).
            float windWorld = VientoMundo * 2.2f;

            for (int i = 0; i < count; i++)
            {
                // Ciclo de vida por fase: nace abajo, muere arriba.
                float phase = (time * speed + i / (float)count) % 1f;

                // Nace a tamaño 0.35·width y CRECE al ascender (nunca
                // aparece a tamaño final — regla de nacimiento del humo).
                float r = width * (0.35f + 0.85f * phase);
                float y = basePos.Y - phase * height;

                // VIENTO: deriva lateral creciendo con la altura + el
                // viento del MUNDO + dos senos inconmensurables.
                float wind = phase * phase * (width * 1.6f + windWorld);
                float x = basePos.X + wind * H01(seed, 3, 7) +
                          MathF.Sin(time * 0.31f + phase * 6.0f + i) * width * 0.22f +
                          MathF.Sin(time * 0.71f + phase * 11.0f + i * 2) * width * 0.09f;

                // Densidad: nace densa, se EROSIONA al morir (la regla de
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
        /// vertical), deriva por viento (+ el del MUNDO) + dos senos
        /// inconmensurables, fría por defecto. Usa 2-3 capas con vientos
        /// distintos = PROFUNDIDAD.
        /// </summary>
        /// <param name="layer">0 lejos · 1 medio · 2 cerca (tinte/escala).</param>
        /// <param name="wind">Viento en px/s (deriva + paralaje).</param>
        /// <param name="worldLit">Tintar por la luz del mundo (bruma física).</param>
        public static void MistBand(Rectangle area, Color color, int seed, float time,
            float wind = 14f, int layer = 0, float alpha = 0.14f, bool worldLit = false)
        {
            float[] alphas = { 0.07f, 0.11f, 0.16f };
            float a = alpha * (alphas[Math.Clamp(layer, 0, 2)] / 0.11f);

            float depth = 1f - layer / 3f;
            float size = area.Height * (0.9f + layer * 0.35f);
            int count = 6;   // ≤6 quads/capa: el overdraw a raya (vfxlabs)

            // El viento del MUNDO se suma al parámetro (niebla con clima).
            float windTotal = wind + VientoMundo * 5f;

            for (int k = 0; k < count; k++)
            {
                float h = H01(seed + layer * 100, k, 9);
                float t = time * (0.4f + 0.2f * layer);

                // X: rejilla + viento con PARALAJE + 2 senos inconmensurables.
                float x = area.X + (k + 0.5f) * area.Width / count
                        + windTotal * time * (1f - depth)
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
                    velocity: new Vector2(windTotal * 0.25f, 0f),
                    worldLit: worldLit);
            }
        }
    }
}
