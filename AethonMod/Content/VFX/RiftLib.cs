using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Particles;

namespace AethonMod.Content.VFX
{
    /// <summary>Fase vital del desgarro (la línea de tiempo del corte).</summary>
    public enum RiftFase
    {
        /// <summary>Telegrafía: la estrella de ruptura crece y el anillo implosiona (0 daño).</summary>
        Telegrafo,
        /// <summary>EL GOLPE: el desgarro se abre en 3-4 ticks (élite 0.333/tick) — aquí va el Kick.</summary>
        Apertura,
        /// <summary>Sostenido: el desgarro está abierto y DAÑA (la línea viva).</summary>
        Sostenido,
        /// <summary>Cierre: los labios se cierran a −0.05·vida; el daño cesó 8 ticks antes.</summary>
        Cierre,
    }

    /// <summary>La personalidad cromática del desgarro (una por "qué hay al otro lado").</summary>
    public static class RiftPaletas
    {
        /// <summary>El VACÍO frío (violeta→azul→cian→blanco): la grieta que queda cuando la herida ya no sangra.</summary>
        public static readonly Color[] Vacio =
        {
            new(150, 80, 255), new(60, 80, 220), new(80, 200, 255), new(235, 245, 255),
        };

        /// <summary>El desgarro CARMESÍ (realidad herida: rojo profundo→fucsia→rosado).</summary>
        public static readonly Color[] Carmesi =
        {
            new(200, 10, 0), new(255, 60, 120), new(255, 200, 220),
        };

        /// <summary>La ENTROPÍA (violeta-cian eléctrico de los planos rotos).</summary>
        public static readonly Color[] Entropia =
        {
            new(147, 24, 204), new(0, 221, 250), new(255, 255, 255),
        };
    }

    /// <summary>
    /// RiftLib — v6.26 — LA LIBRERÍA DE LOS DESGARROS DE REALIDAD.
    ///
    /// Nace de la investigación del ecosistema (INFORME_DESGARRO_REALIDAD.md,
    /// Task 42-b: los patrones de desgarro de la investigación interna +
    /// élite RainbowRiftArrow + Wrath of the Gods "Reality Shatter") re-implementada
    /// 100% con código PROPIO sobre la pila de la casa: SpriteBatch + texturas
    /// procedurales + hash determinista. Cero dependencias externas, cero código ajeno.
    ///
    /// EL VOCABULARIO (qué es un desgarro de verdad):
    ///   · EL DESGARRO ES UNA LÍNEA con LABIOS DE LUZ (3 capas por segmento —
    ///     velo/cuerpo/núcleo, la triple pasada de la casa) y ABERRACIÓN R/B en el
    ///     borde (3 draws con tinte de canal puro y offset perpendicular ±2 px —
    ///     la lección DrawChromaticAberration, sin shader).
    ///   · EL INTERIOR ES VACÍO PROFUNDO: banda que OCLUYE (negro de verdad,
    ///     lote NO-premultiplicado del llamador, grosor 0.62·w) con ESTRELLAS que
    ///     fluyen a lo largo del eje (scroll élite, −2 px/tick) y paralaje X≠Y
    ///     (lección DoG: las profundas se mueven a distinta velocidad).
    ///   · LA APERTURA ES UN GOLPE: 3-4 ticks de apertura (élite 0.333/tick), Kick
    ///     de cámara perpendicular, Flash, y la ESTRELLA DE 4 PUNTAS del punto de
    ///     ruptura (DoG: vertical ×8 + horizontal ×5, todo ·3.25·charge).
    ///   · EL CIERRE ES JUSTO: el daño cesa 8 ticks ANTES de que el visual muera
    ///     (lección élite SCCut + élite PrismaticBurst).
    ///   · LA GRIETA PERSISTE: el camino Lichtenberg (curvatura ACUMULADA,
    ///     micro-fallas 1/9, taper→0) queda como HERIDA que respira (±8%) y NO
    ///     scrollea — el desgarro fluye, la cicatriz permanece.
    ///
    /// CONTRATO DE LOTE (idéntico al de StormLib/EstelaLib/OndaLib): los métodos
    /// de DIBUJO pintan en el SpriteBatch que el LLAMADOR les pase y NUNCA lo
    /// abren/cierran. DOS lotes hacen falta para un desgarro completo:
    ///   1. EL VACÍO (TearVacio/GrietaVacio/Interior): lote NO-premultiplicado
    ///      (BlendState.NonPremultiplied) — dibujar PRIMERO (occlude antes de iluminar).
    ///   2. LA LUZ (Tear/Star/Grieta): lote ADITIVO — después.
    /// Todo determinista por semilla (misma secuencia SIEMPRE, cero Main.rand);
    /// cero estado de desgarros (progress/fase los lleva el llamador); cero GC
    /// por frame (buffers estáticos reutilizados).
    /// </summary>
    public static class RiftLib
    {
        // ==================================================================
        //  TEXTURAS COMPARTIDAS (resolución diferida)
        // ==================================================================

        private static Asset<Texture2D> _black, _star, _glow, _ring, _orb, _trail;

        /// <summary>El disco negro 256² (el VACÍO que ocluye — negro aunque sea mediodía).</summary>
        private static Texture2D BlackTex =>
            (_black ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/BlackDisk")).Value;

        /// <summary>La espiga degradada 16² (la estrella de 4 puntas de la ruptura).</summary>
        private static Texture2D StarTex =>
            (_star ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/Star")).Value;

        /// <summary>El brillo radial suave (velos y blooms).</summary>
        private static Texture2D GlowTex =>
            (_glow ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow")).Value;

        /// <summary>El anillo fino (la implosión del telégrafo).</summary>
        private static Texture2D RingTex =>
            (_ring ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/Ring")).Value;

        /// <summary>El orbe con NÚCLEO SÓLIDO (las estrellitas del interior).</summary>
        private static Texture2D OrbTex =>
            (_orb ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/GlowOrb")).Value;

        /// <summary>La estela degradada 32×8 (el cuerpo de los labios).</summary>
        private static Texture2D TrailTex =>
            (_trail ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/TrailGlow")).Value;

        // ==================================================================
        //  LAS CURVAS (públicas: el contrato numérico del desgarro)
        // ==================================================================

        /// <summary>
        /// Curva de APERTURA del desgarro: sube a 1 en los primeros 8% de la vida
        /// (el equivalente élite de 0.333/tick ≈ 3 ticks) con ease-out cúbico,
        /// sostiene, y cierra linealmente en el último 15% (élite −0.05·vida).
        /// Clampeada 0..1.
        /// </summary>
        public static float Apertura(float progress)
        {
            progress = MathHelper.Clamp(progress, 0f, 1f);
            if (progress < 0.08f)
            {
                float t = progress / 0.08f;
                return 1f - (1f - t) * (1f - t) * (1f - t);   // ease-out cúbico
            }
            if (progress > 0.85f)
                return MathF.Max(0f, 1f - (progress - 0.85f) / 0.15f);
            return 1f;
        }

        /// <summary>¿El desgarro DAÑA en este progress? (false desde el 90% — el daño cesa ~8 ticks antes del final visual).</summary>
        public static bool Daña(float progress)
            => progress >= 0f && progress < 0.90f;

        /// <summary>
        /// COLISIÓN DE LÍNEA del desgarro (la escuela A de daño): cápsula de radio
        /// 0.5·<paramref name="width"/> a lo largo de origin→origin+dir·length.
        /// Paráfrasis propia del patrón Collision.CheckAABBvLineCollision (lección
        /// élite SCCut: el hitbox del corte ES la línea) — lista para Colliding().
        /// </summary>
        public static bool LineaToca(Vector2 origin, Vector2 dir, float length,
            float width, Rectangle hitbox)
        {
            if (length < 1f) return false;
            if (dir.X == 0f && dir.Y == 0f) return false;

            Vector2 end = origin + dir * length;
            float r = MathF.Max(width * 0.5f, 4f);   // franja de gracia mínima

            // AABB inflado por el radio = prueba de cápsula contra la línea.
            var box = new Rectangle(
                hitbox.X - (int)r, hitbox.Y - (int)r,
                hitbox.Width + (int)r * 2, hitbox.Height + (int)r * 2);
            return Collision.CheckAABBvLineCollision(
                new Vector2(box.X, box.Y), new Vector2(box.Width, box.Height), origin, end);
        }

        // ==================================================================
        //  EL DESGARRO — la línea que se abre (escuela A de daño)
        // ==================================================================

        /// <summary>
        /// EL VACÍO DEL DESGARRO: la banda negra que OCLUYE, grosor 0.62·w vivo
        /// (curva "lens" sin(p·π)^0.6 — gorda al centro, aguja en las puntas),
        /// respirando ±6%. Se dibuja en el LOTE NO-PREMULTIPLICADO del llamador
        /// (dibujar ANTES de la luz: el vacío tapa, los labios iluminan).
        /// </summary>
        /// <param name="batch">Batch ABIERTO (BlendState.NonPremultiplied recomendado).</param>
        /// <param name="progress">0..1 vida del desgarro (0-0.08 apertura, 0.85-1 cierre).</param>
        /// <param name="maxWidth">Ancho MÁXIMO del desgarro abierto (8..16 px recomendado).</param>
        public static void TearVacio(SpriteBatch batch, Vector2 origin, Vector2 dir,
            float length, float progress, float maxWidth, int seed, float time)
        {
            if (batch == null || length < 8f) return;

            float w = maxWidth * Apertura(progress);
            if (w < 0.5f) return;

            dir = Vector2.Normalize(dir);
            float breathe = VFXCore.Breathe(time, 2.2f, seed, 0.06f);
            int segs = Math.Clamp((int)(length / 48f), 8, 24);

            for (int i = 0; i < segs; i++)
            {
                float f = (i + 0.5f) / segs;
                float lens = MathF.Pow(MathF.Sin(f * MathHelper.Pi), 0.6f);
                float wseg = w * lens * breathe;
                if (wseg < 0.6f) continue;

                Vector2 a = origin + dir * (length * i / segs);
                Vector2 b = origin + dir * (length * (i + 1) / segs);
                Vector2 mid = (a + b) * 0.5f;
                Vector2 delta = b - a;
                float len = delta.Length();
                if (len < 0.1f) continue;
                float rot = MathF.Atan2(delta.Y, delta.X);

                // EL VACÍO: negro premultiplicado — el interior del desgarro es
                // un agujero en la escena, no una franja oscura encima de ella.
                Quad(batch, BlackTex, mid,
                    new Vector2(len + wseg * 0.35f, wseg * 0.62f), rot,
                    Tint(Color.Black, 0.94f));
            }
        }

        /// <summary>
        /// DIBUJA EL DESGARRO (el pase de LUZ): la línea de <paramref name="origin"/>
        /// a origin+dir·<paramref name="length"/> con anchura viva
        /// Apertura(progress)·maxWidth. ANATOMÍA por segmento (~cada 48 px):
        ///   1. LOS LABIOS ×2 (arriba/abajo, a ±0.5·(0.62·w) del eje — justo en el
        ///      borde del vacío): velo ×1.6 α0.30 · cuerpo α0.60 · NÚCLEO blanco ×0.3 α0.90.
        ///   2. LA ABERRACIÓN: el cuerpo re-dibujado a ±2 px perpendiculares con
        ///      tinte de canal PURO (soloR/soloB), grosor ×0.55 y alpha ×0.45 —
        ///      solo en el sostenido (LOD: nunca en apertura/cierre) y con
        ///      intensity &gt; 0.5.
        ///   3. LAS ESTRELLAS del interior (16-28, scroll + paralaje + parpadeo).
        ///   4. EL VELO DEL BORDE: SoftGlow ×1.6 en cada labio (α 0.18) — el
        ///      "bloom de la herida" que integra el negro con el mundo.
        /// </summary>
        /// <param name="batch">Batch ABIERTO (aditivo recomendado).</param>
        /// <param name="progress">0..1 vida del desgarro (0-0.08 apertura, 0.85-1 cierre).</param>
        /// <param name="maxWidth">Ancho MÁXIMO del desgarro abierto (8..16 px recomendado).</param>
        /// <param name="ecoOffset">Offset del eco glitch (re-dibujo RGB desplazado).</param>
        /// <param name="ecoTint">Tinte de canal puro del eco (null = sin eco).</param>
        public static void Tear(SpriteBatch batch, Vector2 origin, Vector2 dir,
            float length, float progress, float maxWidth, Color[] paleta,
            float intensity, int seed, float time,
            Vector2 ecoOffset = default, Color? ecoTint = null)
        {
            if (batch == null || paleta == null || paleta.Length == 0 || length < 8f) return;

            float w = maxWidth * Apertura(progress);
            if (w < 0.5f) return;
            intensity = MathHelper.Clamp(intensity, 0f, 1f);
            if (intensity <= 0.02f) return;

            dir = Vector2.Normalize(dir);
            Vector2 perp = new(-dir.Y, dir.X);

            Color velo = Pal(paleta, 0);
            Color cuerpo = Pal(paleta, 1);
            Color nucleo = Pal(paleta, paleta.Length - 1);

            // LOS TINTES DE CANAL PURO (la aberración R/B sin shader).
            Color soloR = new((byte)(int)(cuerpo.R * 1.15f), (byte)(int)(cuerpo.G * 0.25f), (byte)(int)(cuerpo.B * 0.25f));
            Color soloB = new((byte)(int)(cuerpo.R * 0.25f), (byte)(int)(cuerpo.G * 0.25f), (byte)(int)(cuerpo.B * 1.15f));

            int segs = Math.Clamp((int)(length / 48f), 8, 24);
            // LOD de capas por anchura (la librería se degrada sola — lección EstelaLib).
            int capas = w >= 6f ? 3 : w >= 3f ? 2 : 1;
            // La aberración SOLO en el sostenido y con intensidad (mitigación del contrato).
            bool aberrar = progress > 0.08f && progress < 0.85f && intensity > 0.5f;
            float beat = 0.90f + 0.10f * MathF.Sin(time * 7.3f + seed);

            for (int i = 0; i < segs; i++)
            {
                float f = (i + 0.5f) / segs;
                float lens = MathF.Pow(MathF.Sin(f * MathHelper.Pi), 0.6f);
                float wseg = w * lens;
                if (wseg < 0.4f) continue;

                Vector2 a = origin + dir * (length * i / segs);
                Vector2 b = origin + dir * (length * (i + 1) / segs);
                Vector2 mid = (a + b) * 0.5f;
                Vector2 delta = b - a;
                float len = delta.Length();
                if (len < 0.1f) continue;
                float rot = MathF.Atan2(delta.Y, delta.X);

                // El borde del vacío: ±0.5·(0.62·w) del eje — ahí viven los labios.
                float edge = 0.31f * wseg;

                for (int lado = -1; lado <= 1; lado += 2)
                {
                    Vector2 lipPos = mid + perp * (edge * lado);

                    // --- CAPA 1: EL VELO (×1.6 de ancho, alpha 0.30) ---
                    if (capas >= 3)
                        LipQuad(batch, lipPos, ecoOffset, len + wseg * 0.9f, wseg * 0.55f, rot,
                            Eco(Tint(velo, 0.30f * intensity * beat), ecoTint));

                    // --- CAPA 2: EL CUERPO (el labio de color) ---
                    if (capas >= 2)
                        LipQuad(batch, lipPos, ecoOffset, len + wseg * 0.5f, wseg * 0.34f, rot,
                            Eco(Tint(cuerpo, 0.60f * intensity), ecoTint));

                    // --- LA ABERRACIÓN CROMÁTICA: ±2 px perpendiculares, canal puro ---
                    if (aberrar)
                    {
                        LipQuad(batch, lipPos + perp * 2f, ecoOffset, len + wseg * 0.5f, wseg * 0.19f, rot,
                            Eco(Tint(soloR, 0.27f * intensity), ecoTint));
                        LipQuad(batch, lipPos - perp * 2f, ecoOffset, len + wseg * 0.5f, wseg * 0.19f, rot,
                            Eco(Tint(soloB, 0.27f * intensity), ecoTint));
                    }

                    // --- CAPA 3: EL NÚCLEO (razor blanco, alpha 0.90) ---
                    LipQuad(batch, lipPos, ecoOffset, len + wseg * 0.3f, MathF.Max(wseg * 0.10f, 0.8f), rot,
                        Eco(Tint(nucleo, 0.90f * intensity), ecoTint));

                    // --- EL VELO DEL BORDE (el bloom que integra el negro) ---
                    if (capas >= 3 && ecoTint == null)
                        Quad(batch, GlowTex, lipPos, new Vector2(len + wseg * 1.1f, wseg * 1.6f), rot,
                            Tint(velo, 0.18f * intensity));
                }
            }

            // LAS ESTRELLAS DEL INTERIOR: el vacío fluye (scroll élite).
            // (En los re-dibujos de ECO no: el glitch es de los LABIOS.)
            if (ecoTint == null)
                Estrellas(batch, origin, dir, length, w, paleta, intensity, seed, time, 2f, 16 + seed % 13);
        }

        /// <summary>
        /// LA ESTRELLA DE 4 PUNTAS del punto de ruptura (lección DoG): la espiga
        /// vertical ×8 y la horizontal ×5 (ambas ·3.25·charge, respirando ±6%) +
        /// copia interior ×0.8 en el color de la paleta + núcleo blanco. Mientras
        /// <paramref name="charge"/> &lt; 1 dibuja además EL ANILLO QUE IMPLOSIONA
        /// (radio Lerp(90, 0, charge) — el telégrafo de la ruptura).
        /// </summary>
        /// <param name="charge">0..1 carga de la ruptura (crece en el telégrafo, decae tras abrir).</param>
        public static void Star(SpriteBatch batch, Vector2 center, float charge,
            Color[] paleta, float intensity, int seed, float time)
        {
            if (batch == null || paleta == null || paleta.Length == 0) return;
            charge = MathHelper.Clamp(charge, 0f, 1f);
            intensity = MathHelper.Clamp(intensity, 0f, 1f);
            if (charge <= 0.03f || intensity <= 0.02f) return;

            // La respiración DoG: la estrella late ±6% en vez de ser un sprite plano.
            float k = 3.25f * charge * (1f + 0.06f * MathF.Sin(time * 7f + seed));
            const float EscalaArma = 2.4f;   // 620 px de desgarro piden una ruptura que se LEA

            // === EL ANILLO QUE IMPLOSIONA (solo mientras carga) ===
            if (charge < 0.98f)
            {
                float r = 90f * (1f - charge);
                if (r > 3f)
                {
                    float s = r * 2.174f;
                    batch.Draw(RingTex, center, null, Tint(Pal(paleta, 1), 0.35f * (1f - charge) * intensity), 0f,
                        new Vector2(RingTex.Width, RingTex.Height) * 0.5f,
                        new Vector2(s, s) / new Vector2(RingTex.Width, RingTex.Height),
                        SpriteEffects.None, 0f);
                }
            }

            Color cuerpo = Pal(paleta, 1);
            Color nucleo = Pal(paleta, paleta.Length - 1);

            // === LA ESPIGA VERTICAL (×8): Star.png rotada π/2 para que el
            //     degradado corra VERTICAL — la dimensión dominante del DoG. ===
            StarQuad(batch, center, MathHelper.PiOver2,
                8f * k * EscalaArma, MathF.Max(1.4f * k * 0.6f, 1.6f), Tint(nucleo, 0.90f * intensity));

            // === LA ESPIGA HORIZONTAL (×5): la cruz de la ruptura. ===
            StarQuad(batch, center, 0f,
                5f * k * EscalaArma, MathF.Max(1.4f * k * 0.6f, 1.6f), Tint(nucleo, 0.90f * intensity));

            // === LA COPIA INTERIOR ×0.8 en el color de la paleta (profundidad) ===
            StarQuad(batch, center, MathHelper.PiOver2,
                8f * k * EscalaArma * 0.8f, MathF.Max(1.2f * k * 0.5f, 1.4f), Tint(cuerpo, 0.55f * intensity));
            StarQuad(batch, center, 0f,
                5f * k * EscalaArma * 0.8f, MathF.Max(1.2f * k * 0.5f, 1.4f), Tint(cuerpo, 0.55f * intensity));

            // === EL NÚCLEO + EL BLOOM de la herida ===
            Quad(batch, GlowTex, center, new Vector2(30f * charge, 30f * charge) * (0.8f + 0.4f * EscalaArma * 0.4f), 0f,
                Tint(cuerpo, 0.30f * intensity));
            Quad(batch, OrbTex, center, new Vector2(5.5f * charge, 5.5f * charge), 0f,
                Tint(Color.White, 0.95f * intensity));
        }

        /// <summary>
        /// EL PAQUETE DE IMPACTO del desgarro (la apertura ES un golpe): Kick de
        /// cámara PERPENDICULAR a la línea (7 px, 12 ticks), Flash de pantalla
        /// (tinte de paleta, 0.22, 8 ticks), el sonido grave de la ruptura (pitch
        /// −0.65 — la lección DoG del pitch −0.75) y la RÁFAGA de chispas de
        /// anomalía (12 motas vía ParticleManager, cliente). Se llama UNA VEZ,
        /// al abrir. Registro puro: NADA de dibujo aquí (el visual lo pinta
        /// PreDraw con la fase Apertura).
        /// </summary>
        public static void TearImpacto(Vector2 origin, Vector2 dir, float length,
            Color[] paleta, int seed)
        {
            if (paleta == null || paleta.Length == 0) return;

            // El KICK perpendicular al corte: la cámara se desplaza AL LADO,
            // como si la realidad hubiera TRONADO (no hacia adelante).
            if (dir.X != 0f || dir.Y != 0f)
            {
                Vector2 p = Vector2.Normalize(new(-dir.Y, dir.X));
                OndaLib.Kick(7f, 12, MathF.Atan2(p.Y, p.X));
            }
            else
                OndaLib.Kick(7f, 12, -1f);

            OndaLib.Flash(Pal(paleta, 1), 0.22f, 8);

            // El sonido de la ruptura: hondo, con el mundo "asentándose" después
            // (pitch −0.65 — la lección DoG del pitch −0.75 al abrir el rift).
            if (Main.netMode != NetmodeID.Server)
            {
                try { Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8.WithPitchOffset(-0.65f), origin); }
                catch { }
            }

            // LA RÁFAGA de chispas de anomalía (12 motas, solo cliente).
            if (Main.netMode != NetmodeID.Server)
            {
                ChispasAnomalia(origin, 12, paleta, seed, out ParticleData[] motas);
                if (motas != null)
                    for (int i = 0; i < motas.Length; i++)
                        ParticleManager.Spawn(motas[i]);
            }
        }

        // ==================================================================
        //  LA GRIETA — la herida persistente (escuela B de daño)
        // ==================================================================

        /// <summary>
        /// CAMINO FRACTAL DE GRIETA (generador Lichtenberg, lección DoGRiftCrack):
        /// paseo con PERSISTENCIA de dirección, curvatura ACUMULADA (giro
        /// ±curvatura·i·0.25° que crece con el paso) con deriva total clampeada,
        /// y MICRO-FALLAS (1/9 de los pasos da un quiebre brusco de ±0.6 rad).
        /// El último paso se acorta a la mitad (la aguja final). Determinista:
        /// la MISMA semilla da la MISMA grieta en todas las máquinas.
        /// </summary>
        /// <param name="points">4..32 puntos (24 recomendado → ~700-900 px).</param>
        /// <param name="pasoMin/pasoMax">25..50 px por punto (0.5× en el último).</param>
        /// <param name="curvatura">5° = vidrio; 9° = caos.</param>
        public static Vector2[] CaminoGrieta(Vector2 origin, Vector2 dir,
            int seed, int points = 24, float pasoMin = 25f, float pasoMax = 50f,
            float curvatura = 5f)
        {
            points = Math.Clamp(points, 4, 32);
            var pts = new Vector2[points];
            pts[0] = origin;

            float bearing = dir.X == 0f && dir.Y == 0f
                ? 0f
                : MathF.Atan2(dir.Y, dir.X);
            float bearing0 = bearing;
            float drift = 0f;

            for (int i = 1; i < points; i++)
            {
                // CURVATURA ACUMULADA: el giro disponible crece con el paso
                // (±curvatura·0.25·i grados) — la grieta se vuelve loca con la longitud.
                float g = (H01(seed, i, 101) - 0.5f) * 2f * curvatura * (0.25f * i) * MathHelper.Pi / 180f;
                drift = MathHelper.Clamp(drift + g, -0.9f, 0.9f);   // nunca media vuelta
                bearing = bearing0 + drift;

                // MICRO-FALLA (1/9): el quiebre brusco — la firma Lichtenberg.
                if (H01(seed, i, 211) < 1f / 9f)
                {
                    float kink = (H01(seed, i, 307) - 0.5f) * 2f * 0.6f;
                    drift = MathHelper.Clamp(drift + kink, -0.9f, 0.9f);
                    bearing = bearing0 + drift;
                }

                float paso = MathHelper.Lerp(pasoMin, pasoMax, H01(seed, i, 401));
                if (i == points - 1) paso *= 0.5f;   // la aguja final

                pts[i] = pts[i - 1] + new Vector2(MathF.Cos(bearing), MathF.Sin(bearing)) * paso;
            }
            return pts;
        }

        /// <summary>
        /// EL VACÍO DE LA GRIETA persistente (pase no-premultiplicado): bandas
        /// negras a lo largo del camino con el taper raíz→punta y la respiración
        /// ±8% de la herida. Dibujar ANTES de <see cref="Grieta"/>.
        /// </summary>
        public static void GrietaVacio(SpriteBatch batch, Vector2[] camino, float progress,
            float maxWidth, int seed, float time)
        {
            if (batch == null || camino == null || camino.Length < 2) return;

            float vida = MathF.Pow(1f - MathHelper.Clamp(progress, 0f, 1f), 0.8f);
            float respira = 1f + 0.08f * MathF.Sin(time * 2.2f + seed * 0.13f);
            float total = 0f;
            for (int i = 1; i < camino.Length; i++)
                total += Vector2.Distance(camino[i - 1], camino[i]);
            if (total < 8f) return;

            float arc = 0f;
            for (int i = 0; i < camino.Length - 1; i++)
            {
                Vector2 a = camino[i];
                Vector2 b = camino[i + 1];
                float len = Vector2.Distance(a, b);
                if (len < 0.35f) { arc += len; continue; }
                float tMid = (arc + len * 0.5f) / total;
                arc += len;

                // TAPER: gorda en la raíz (donde nació el desgarro), aguja en la punta.
                float wseg = maxWidth * MathF.Pow(1f - tMid, 0.9f) * respira;
                if (wseg < 0.5f) continue;

                float rot = MathF.Atan2(b.Y - a.Y, b.X - a.X);
                Quad(batch, BlackTex, (a + b) * 0.5f,
                    new Vector2(len + wseg * 0.35f, wseg * 0.62f * vida + 0.8f), rot,
                    Tint(Color.Black, 0.94f * vida));
            }
        }

        /// <summary>
        /// DIBUJA LA GRIETA PERSISTENTE (el pase de LUZ) sobre el camino: labios
        /// de 3 capas con taper raíz→punta y aberración R/B, ESTRELLAS FIJAS (la
        /// grieta NO scrollea: es una HERIDA, no una boca tragando) y CHISPAS de
        /// anomalía dibujadas (máx 1 cada 3 ticks, determinista). El alpha muere
        /// con falloff Slow (física mágica: se resiste a sanar).
        /// </summary>
        /// <param name="progress">0..1 de la vida de la grieta (480 ticks ≈ 8 s recomendado).</param>
        /// <param name="chispas">True = dibuja las chispas de anomalía (1 cada 3 ticks).</param>
        /// <param name="ecoOffset">Offset del eco glitch (re-dibujo RGB desplazado).</param>
        /// <param name="ecoTint">Tinte de canal puro del eco (null = sin eco).</param>
        public static void Grieta(SpriteBatch batch, Vector2[] camino, float progress,
            float maxWidth, Color[] paleta, float intensity, int seed, float time,
            bool chispas = true, Vector2 ecoOffset = default, Color? ecoTint = null)
        {
            if (batch == null || camino == null || camino.Length < 2 || paleta == null || paleta.Length == 0) return;
            intensity = MathHelper.Clamp(intensity, 0f, 1f);
            if (intensity <= 0.02f) return;

            float vida = MathF.Pow(1f - MathHelper.Clamp(progress, 0f, 1f), 0.8f);
            float respira = 1f + 0.08f * MathF.Sin(time * 2.2f + seed * 0.13f);

            float total = 0f;
            for (int i = 1; i < camino.Length; i++)
                total += Vector2.Distance(camino[i - 1], camino[i]);
            if (total < 8f) return;

            Color velo = Pal(paleta, 0);
            Color cuerpo = Pal(paleta, 1);
            Color nucleo = Pal(paleta, paleta.Length - 1);
            Color soloR = new((byte)(int)(cuerpo.R * 1.15f), (byte)(int)(cuerpo.G * 0.25f), (byte)(int)(cuerpo.B * 0.25f));
            Color soloB = new((byte)(int)(cuerpo.R * 0.25f), (byte)(int)(cuerpo.G * 0.25f), (byte)(int)(cuerpo.B * 1.15f));
            bool aberrar = vida > 0.25f && intensity > 0.5f;

            float arc = 0f;
            int k = 0;
            for (int i = 0; i < camino.Length - 1; i++)
            {
                Vector2 a = camino[i];
                Vector2 b = camino[i + 1];
                float len = Vector2.Distance(a, b);
                if (len < 0.35f) { arc += len; continue; }
                float tMid = (arc + len * 0.5f) / total;
                arc += len;

                float wseg = maxWidth * MathF.Pow(1f - tMid, 0.9f) * respira;
                if (wseg < 0.4f) continue;

                Vector2 mid = (a + b) * 0.5f;
                Vector2 delta = b - a;
                float rot = MathF.Atan2(delta.Y, delta.X);
                Vector2 perp = new(-delta.Y / len, delta.X / len);
                float edge = 0.31f * wseg;
                // El latido de la herida (vive, no es un dibujo muerto).
                float beat = 0.88f + 0.12f * MathF.Sin(time * 6.1f + k * 1.7f + seed);

                for (int lado = -1; lado <= 1; lado += 2)
                {
                    Vector2 lipPos = mid + perp * (edge * lado);

                    // VELO ×1.6 α0.30
                    Quad(batch, GlowTex, lipPos + ecoOffset,
                        new Vector2(len + wseg * 0.9f, wseg * 0.55f), rot,
                        Eco(Tint(velo, 0.30f * intensity * vida * beat), ecoTint));

                    // CUERPO α0.60 + LA ABERRACIÓN R/B (±2 px, canal puro)
                    Quad(batch, TrailTex, lipPos + ecoOffset,
                        new Vector2(len + wseg * 0.5f, wseg * 0.34f), rot,
                        Eco(Tint(cuerpo, 0.60f * intensity * vida), ecoTint));
                    if (aberrar)
                    {
                        Quad(batch, TrailTex, lipPos + perp * 2f + ecoOffset,
                            new Vector2(len + wseg * 0.5f, wseg * 0.19f), rot,
                            Eco(Tint(soloR, 0.27f * intensity * vida), ecoTint));
                        Quad(batch, TrailTex, lipPos - perp * 2f + ecoOffset,
                            new Vector2(len + wseg * 0.5f, wseg * 0.19f), rot,
                            Eco(Tint(soloB, 0.27f * intensity * vida), ecoTint));
                    }

                    // NÚCLEO razor α0.90
                    Quad(batch, TrailTex, lipPos + ecoOffset,
                        new Vector2(len + wseg * 0.3f, MathF.Max(wseg * 0.10f, 0.8f)), rot,
                        Eco(Tint(nucleo, 0.90f * intensity * vida), ecoTint));
                }
                k++;
            }

            // === LAS ESTRELLAS FIJAS de la herida (sin scroll, con paralaje) ===
            EstrellasCamino(batch, camino, maxWidth * respira, paleta, intensity * vida, seed, time, 14 + seed % 7);

            // === LAS CHISPAS DE ANOMALÍA (máx 1 cada 3 ticks, deterministas) ===
            if (chispas && vida > 0.15f)
            {
                int tick = (int)(time * 60f);
                int slot = tick / 3;
                if (H01(seed, slot, 503) < 0.6f)
                {
                    int idx = 1 + (int)(H01(seed, slot, 509) * (camino.Length - 2));
                    Vector2 p = camino[Math.Clamp(idx, 1, camino.Length - 2)];
                    float jx = (H01(seed, slot, 521) - 0.5f) * 10f;
                    float jy = (H01(seed, slot, 523) - 0.5f) * 10f;
                    float fase = (tick % 3) / 3f;
                    float s = (2.2f + 2.6f * H01(seed, slot, 527)) * (1f - fase * 0.5f);

                    Quad(batch, OrbTex, p + new Vector2(jx, jy), new Vector2(s, s), 0f,
                        Tint(Color.Lerp(Pal(paleta, 1), Color.White, 0.65f), 0.9f * vida));
                    // Los dos rayitos de la mota (la anomalía "chasquea").
                    float ang = H01(seed, slot, 531) * MathHelper.TwoPi;
                    Vector2 d = new(MathF.Cos(ang), MathF.Sin(ang));
                    Quad(batch, TrailTex, p + new Vector2(jx, jy) + d * 4f,
                        new Vector2(7f, 1.4f), ang, Tint(nucleo, 0.7f * vida));
                    Quad(batch, TrailTex, p + new Vector2(jx, jy) - d * 4f,
                        new Vector2(7f, 1.4f), ang, Tint(nucleo, 0.7f * vida));
                }
            }
        }

        // ==================================================================
        //  EL INTERIOR — el vacío que se ve dentro (re-utilizable solo)
        // ==================================================================

        /// <summary>
        /// EL VACÍO INTERIOR re-utilizable: la banda que OCLUYE (en el lote del
        /// llamador — NO-premultiplicado) + N estrellas deterministas con PARALAJE
        /// (las profundas se mueven y parpadean distinto) y SCROLL px/tick a lo
        /// largo del eje (el desgarro fluye: 2 px/tick; la grieta no: 0).
        /// El grosor efectivo es width·Apertura(progress).
        /// </summary>
        /// <param name="batch">Batch ABIERTO NO-premultiplicado (EL VACÍO).</param>
        /// <param name="batchEstrellas">Batch ADITIVO del llamador para las estrellas (null = el mismo batch).</param>
        public static void Interior(SpriteBatch batch, Vector2 origin, Vector2 dir, float length,
            float width, float progress, Color[] paleta, int seed, float time,
            float scroll = 2f, int estrellas = 22, SpriteBatch batchEstrellas = null)
        {
            if (batch == null || length < 8f) return;
            float w = width * Apertura(progress);
            if (w < 0.5f) return;

            dir = Vector2.Normalize(dir);
            int segs = Math.Clamp((int)(length / 48f), 8, 24);
            float breathe = VFXCore.Breathe(time, 2.2f, seed, 0.06f);

            for (int i = 0; i < segs; i++)
            {
                float f = (i + 0.5f) / segs;
                float lens = MathF.Pow(MathF.Sin(f * MathHelper.Pi), 0.6f);
                float wseg = w * lens * breathe;
                if (wseg < 0.6f) continue;

                Vector2 a = origin + dir * (length * i / segs);
                Vector2 b = origin + dir * (length * (i + 1) / segs);
                float len = Vector2.Distance(a, b);
                if (len < 0.1f) continue;
                Quad(batch, BlackTex, (a + b) * 0.5f,
                    new Vector2(len + wseg * 0.35f, wseg * 0.62f),
                    MathF.Atan2(b.Y - a.Y, b.X - a.X), Tint(Color.Black, 0.94f));
            }

            Estrellas(batchEstrellas ?? batch, origin, dir, length, w, paleta, 1f, seed, time, scroll, estrellas);
        }

        /// <summary>
        /// EL CAMPO DE ESTRELLAS del interior (16-28): posiciones deterministas
        /// por hash dentro de la banda, tamaño 1-3 px, scroll a lo largo del eje
        /// a velocidad por PROFUNDIDAD (paralaje X≠Y: las profundas van a ~0.4×),
        /// vaivén lateral por profundidad y parpadeo por hash — el vacío "tira".
        /// </summary>
        public static void Estrellas(SpriteBatch batch, Vector2 origin, Vector2 dir,
            float length, float width, Color[] paleta, float intensity, int seed,
            float time, float scroll, int estrellas)
        {
            if (batch == null || paleta == null || paleta.Length == 0 || length < 8f) return;
            estrellas = Math.Clamp(estrellas, 4, 28);
            dir = Vector2.Normalize(dir);
            Vector2 perp = new(-dir.Y, dir.X);
            float tick = time * 60f;

            Color baseC = Color.Lerp(Pal(paleta, paleta.Length - 1), Color.White, 0.5f);

            for (int j = 0; j < estrellas; j++)
            {
                float h1 = H01(seed, j, 3);
                float h2 = H01(seed, j, 7);
                float h3 = H01(seed, j, 11);
                float h4 = H01(seed, j, 17);

                // PROFUNDIDAD: 0.35 (fondo de la caverna) .. 1.0 (superficie).
                float depth = 0.35f + 0.65f * h1;

                // EL SCROLL por profundidad: el interior FLUYE, las profundas rezan.
                float x = h2 * length - tick * scroll * depth;
                x = ((x % length) + length) % length;
                float f = x / length;
                float lens = MathF.Pow(MathF.Sin(f * MathHelper.Pi), 0.6f);
                float halfVoid = 0.31f * width * lens;
                if (halfVoid < 0.8f) continue;

                // La lateral: dentro de la banda + el vaivén de paralaje
                // (las cercanas se mueven MÁS — la lección X≠Y de DoG).
                float y = (h3 - 0.5f) * 2f * halfVoid * 0.85f;
                y += VFXCore.Sway(time * 0.8f + h4 * 6.28f, 1f, j) * halfVoid * 0.22f * (1f - depth);

                // EL PARPADEO por hash (algunas duermen, otras arden).
                float tw = 0.45f + 0.55f * MathF.Sin(tick * (0.18f + 0.22f * h4) + h4 * 6.28f + j * 2.1f);
                if (tw <= 0.08f) continue;

                float s = 1f + 2f * h4;
                // 4-6 estrellas GRANDES de profundidad (la caverna infinita).
                bool grande = h2 > 0.82f && j % 5 == 0;
                if (grande) s += 1.6f;

                Quad(batch, OrbTex, origin + dir * x + perp * y, new Vector2(s, s), 0f,
                    Tint(baseC, MathHelper.Clamp(tw * intensity * (grande ? 0.8f : 1f), 0f, 1f)));
            }
        }

        /// <summary>Las estrellas FIJAS de la grieta (por fracción de arco del camino).</summary>
        private static void EstrellasCamino(SpriteBatch batch, Vector2[] camino,
            float maxWidth, Color[] paleta, float intensity, int seed, float time, int n)
        {
            Color baseC = Color.Lerp(Pal(paleta, paleta.Length - 1), Color.White, 0.5f);
            float tick = time * 60f;

            for (int j = 0; j < n; j++)
            {
                float h1 = H01(seed, j, 60);
                float h2 = H01(seed, j, 61);
                float h3 = H01(seed, j, 62);
                float f = h1;   // fracción de arco (la herida NO scrollea)

                // El punto del camino a esa fracción (aproximación por índice).
                float idxF = f * (camino.Length - 1);
                int i0 = Math.Clamp((int)idxF, 0, camino.Length - 2);
                float t = idxF - i0;
                Vector2 p = Vector2.Lerp(camino[i0], camino[Math.Min(i0 + 1, camino.Length - 1)], t);
                Vector2 seg = camino[Math.Min(i0 + 1, camino.Length - 1)] - camino[i0];
                float segLen = seg.Length();
                Vector2 perp = segLen > 0.01f
                    ? new(-seg.Y / segLen, seg.X / segLen)
                    : new Vector2(0f, -1f);

                float wLocal = maxWidth * MathF.Pow(1f - f, 0.9f);
                float y = (h2 - 0.5f) * 2f * wLocal * 0.30f;
                float tw = 0.45f + 0.55f * MathF.Sin(tick * (0.15f + 0.2f * h3) + h3 * 6.28f + j * 1.9f);
                if (tw <= 0.08f) continue;
                float s = 1f + 1.6f * h3;

                Quad(batch, OrbTex, p + perp * y, new Vector2(s, s), 0f,
                    Tint(baseC, MathHelper.Clamp(tw * intensity, 0f, 1f)));
            }
        }

        // ==================================================================
        //  LAS PIEZAS SUELTAS (los quiere todo el mundo)
        // ==================================================================

        /// <summary>
        /// SHARDS DE CRISTAL: 6-10 esquirlas + 1 GRANDE (números de la investigación interna
        /// RealityRuptureStealth): nacen a lo largo de la línea, salen con
        /// velocidad perpendicular ± jitter, rotan, CAEN (gravedad de vidrio
        /// 0.11) y mueren desvaneciéndose. Pop-in por ScaleUp (la adaptación
        /// casa del PolyOut(4)). Devuelve el paquete para el ParticleManager
        /// (la librería NO spawnea).
        /// </summary>
        /// <param name="length">Largo de la línea donde nacen (0 = solo en el origen).</param>
        public static void Shards(Vector2 origin, Vector2 dir, Color[] paleta,
            int seed, out ParticleData[] outParticles, float length = 0f)
        {
            int count = 6 + seed % 5;                 // 6..10 esquirlas
            outParticles = new ParticleData[count + 1];   // +1: LA GRANDE

            float baseAng = (dir.X == 0f && dir.Y == 0f) ? -MathHelper.PiOver2 : MathF.Atan2(dir.Y, dir.X);

            for (int k = 0; k <= count; k++)
            {
                bool grande = k == count;
                float h1 = H01(seed, k, 701);
                float h2 = H01(seed, k, 709);
                float h3 = H01(seed, k, 719);
                float h4 = H01(seed, k, 727);

                // Nace sobre la línea (la esquirla SALE del desgarro).
                Vector2 pos = origin + dir * (h1 * length);
                // Sale con velocidad perpendicular ± jitter (hacia fuera del corte).
                float ang = baseAng + MathHelper.PiOver2 * (h2 > 0.5f ? 1f : -1f) + (h3 - 0.5f) * 1.2f;
                float speed = (1.5f + 2.5f * h4) * (grande ? 0.7f : 1f);

                // El color del vidrio: paleta con borde blanco (lección DoG).
                Color c = Color.Lerp(Pal(paleta, (int)(h4 * paleta.Length) % paleta.Length), Color.White, 0.5f);

                var p = new ParticleData
                {
                    Position = pos,
                    Velocity = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * speed,
                    Scale = Vector2.One * (grande ? 0.62f : 0.34f + 0.22f * h2),
                    PackedColor = ParticleManager.PackColor(c),
                    PackedStartColor = ParticleManager.PackColor(c),
                    PackedEndColor = ParticleManager.PackColor(Color.Transparent),
                    Rotation = ang + (h1 - 0.5f) * 0.8f,
                    RotationSpeed = (h2 - 0.5f) * 0.22f,
                    TimeLeft = grande ? 80 : 40 + (int)(30f * h3),
                    Duration = grande ? 80 : 40 + (int)(30f * h3),
                    TextureId = ParticleTex.Slash,       // el glifo de vidrio de la casa
                    BlendMode = 1,                        // aditivo: vidrio que BRILLA
                    LayerPriority = LayerPriorities.BeforeProjectiles,
                };
                p.EnableComponent(ComponentFlag.FadeOut);
                p.EnableComponent(ComponentFlag.Rotation);
                p.EnableComponent(ComponentFlag.Gravity);
                p.UserData0 = 0.11f;                     // gravedad de vidrio (cayendo)
                outParticles[k] = p;
            }
        }

        /// <summary>
        /// CHISPAS DE ANOMALÍA: paquete determinista de motas radiales (los
        /// números DoG — vel 8-14 radial con drag — adaptados al ParticleManager
        /// sin drag: velocidad efectiva equivalente), color
        /// Lerp(paleta[hash], Blanco, 0.65), vida 30-45, mota 2-4 px.
        /// La librería NO spawnea: devuelve el paquete.
        /// </summary>
        public static void ChispasAnomalia(Vector2 center, int count, Color[] paleta,
            int seed, out ParticleData[] outParticles)
        {
            count = Math.Clamp(count, 1, 40);
            outParticles = new ParticleData[count];

            for (int k = 0; k < count; k++)
            {
                float h1 = H01(seed, k, 801);
                float h2 = H01(seed, k, 811);
                float h3 = H01(seed, k, 821);
                float h4 = H01(seed, k, 831);

                float ang = h1 * MathHelper.TwoPi;
                float speed = (8f + 6f * h2) / 5.5f;    // 8-14 DoG ÷ drag-equivalente casa
                Color c = Color.Lerp(Pal(paleta, (int)(h3 * paleta.Length) % paleta.Length), Color.White, 0.65f);
                int vida = 30 + (int)(15f * h4);

                var p = new ParticleData
                {
                    Position = center,
                    Velocity = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * speed,
                    Scale = Vector2.One * (0.035f + 0.045f * h3),   // mota 2-4 px
                    PackedColor = ParticleManager.PackColor(c),
                    PackedStartColor = ParticleManager.PackColor(c),
                    PackedEndColor = ParticleManager.PackColor(Color.Transparent),
                    TimeLeft = vida,
                    Duration = vida,
                    TextureId = ParticleTex.SoftGlow,
                    BlendMode = 1,
                    LayerPriority = LayerPriorities.BeforeProjectiles,
                };
                p.EnableComponent(ComponentFlag.FadeOut);
                p.EnableComponent(ComponentFlag.ScaleDown);
                outParticles[k] = p;
            }
        }

        /// <summary>
        /// EL ECO GLITCH (lección élite glur, SIN render targets): 3 offsets
        /// horizontales alternos ±(2..3) px con tintes de CANAL PURO (R/B) y
        /// alpha 0.30·(1−i/3). El llamador re-dibuja su desgarro/grieta con
        /// estos offsets (los parámetros ecoOffset/ecoTint de Tear/Grieta)
        /// — el pulso de brillo glitch SIN tocar la lente.
        /// Los arrays son buffers estáticos (cero GC).
        /// </summary>
        public static void EcoGlitch(int seed, float time, out Vector2[] offsets,
            out Color[] tintes)
        {
            int tick = (int)(time * 60f);
            for (int i = 0; i < 3; i++)
            {
                // Offset horizontal alterno ±(2..3) px, por eco y por tick.
                float mag = 2f + H01(seed, tick, 901 + i) * 1f;
                float sign = (i % 2 == 0 ? -1f : 1f) * (H01(seed, tick, 911 + i) > 0.5f ? 1f : -1f);
                _ecoOff[i] = new Vector2(mag * sign, 0f);

                // Tinte de canal puro (R para un lado, B para el otro) con alpha 0.30·(1−i/3).
                float a = 0.30f * (1f - i / 3f);
                _ecoTint[i] = i % 2 == 0
                    ? new Color(255, 0, 0, (byte)(int)(255f * a))
                    : new Color(0, 90, 255, (byte)(int)(255f * a));
            }
            offsets = _ecoOff;
            tintes = _ecoTint;
        }

        // ==================================================================
        //  EL MUNDO — el oscurecer de la realidad herida (T11 del contrato)
        // ==================================================================

        /// <summary>
        /// PIDE el oscurecimiento del mundo mientras la realidad está desgarrada
        /// (máx 0.35 — la mitad del FillProgress de DoG, arma no jefe). RiftMundoSystem
        /// lo aplica en ModifySunLightColor y lo deja decaer solo (~0.5 s).
        /// Solo afecta al CLIENTE (los servidores no ven pantallas).
        /// </summary>
        public static void Oscurecer(float objetivo)
            => RiftMundoSystem.Pedir(MathHelper.Clamp(objetivo, 0f, 0.35f));

        // ==================================================================
        //  PRIMITIVAS INTERNAS
        // ==================================================================

        private static readonly Vector2[] _ecoOff = new Vector2[3];
        private static readonly Color[] _ecoTint = new Color[3];

        /// <summary>Color de la paleta con índice seguro (las paletas tienen 3 o 4 entradas).</summary>
        private static Color Pal(Color[] paleta, int i)
            => paleta[Math.Clamp(i, 0, paleta.Length - 1)];

        /// <summary>Hash determinista [0,1) (VFXCore, sin estado).</summary>
        private static float H01(int seed, int a, int b) => VFXCore.Hash01(seed, a, b);

        /// <summary>Tinte de INTENSIDAD LINEAL premultiplicado (el de la casa v6.25).</summary>
        private static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color(
                (byte)(int)(c.R * f), (byte)(int)(c.G * f), (byte)(int)(c.B * f),
                (byte)(int)(255f * f));
        }

        /// <summary>Aplica el tinte de canal del eco (null = tal cual).</summary>
        private static Color Eco(Color c, Color? eco)
        {
            if (!eco.HasValue) return c;
            Color e = eco.Value;
            return new Color(
                (byte)(int)(c.R * e.R / 255f),
                (byte)(int)(c.G * e.G / 255f),
                (byte)(int)(c.B * e.B / 255f),
                (byte)(int)(c.A * e.A / 255f));
        }

        /// <summary>Quad centrado con rotación (tamaño total = size px, sobre el lote ABIERTO).</summary>
        private static void Quad(SpriteBatch batch, Texture2D tex, Vector2 pos,
            Vector2 size, float rot, Color tint)
        {
            if (tex == null || tint.A == 0 || size.X < 0.1f || size.Y < 0.1f) return;
            batch.Draw(tex, pos, null, tint, rot,
                new Vector2(tex.Width, tex.Height) * 0.5f,
                size / new Vector2(tex.Width, tex.Height),
                SpriteEffects.None, 0f);
        }

        /// <summary>El quad de un labio (desplazado por el offset del eco glitch).</summary>
        private static void LipQuad(SpriteBatch batch, Vector2 pos, Vector2 ecoOffset,
            float len, float w, float rot, Color tint)
        {
            // El eco desplaza en ESPACIO DE PANTALLA (horizontal ±2..3 px — el
            // glitch VHS de élite), no en el espacio del labio: por eso se suma tal cual.
            Quad(batch, TrailTex, pos + ecoOffset, new Vector2(len, w), rot, tint);
        }

        /// <summary>La espiga de la estrella de 4 puntas (textura degradada orientada).</summary>
        private static void StarQuad(SpriteBatch batch, Vector2 center, float rot,
            float largo, float ancho, Color tint)
        {
            if (tint.A == 0 || largo < 1f) return;
            // rot gira el eje del degradado: π/2 = espiga VERTICAL.
            batch.Draw(StarTex, center, null, tint, rot,
                new Vector2(StarTex.Width, StarTex.Height) * 0.5f,
                new Vector2(largo, ancho) / new Vector2(StarTex.Width, StarTex.Height),
                SpriteEffects.None, 0f);
        }
    }

    /// <summary>
    /// RiftMundoSystem — v6.26 — EL OSCURECER DE LA REALIDAD DESGARRADA.
    ///
    /// El complemento de mundo de RiftLib (lección DoG: cuando el espacio se
    /// abre, EL MUNDO SE OSCURECE — la luz "se cae" dentro de la herida).
    /// Un acumulador estático: los desgarros activos piden su oscuridad
    /// (<see cref="RiftLib.Oscurecer"/>) y el sistema la aplica SOLO en
    /// ModifySunLightColor (el hook oficial de tML), con sesgo violeta-azulado
    /// y decaimiento suave (~0.5 s al dejar de pedir) para no pelear con otros
    /// mods más de lo necesario. Cliente-only.
    /// </summary>
    public class RiftMundoSystem : ModSystem
    {
        private static float _pedido;      // lo que los desgarros piden este tick
        private static float _actual;      // lo aplicado (lerp suave)
        private static uint _lastTick;

        /// <summary>Registra la petición de oscuridad (la mayor gana).</summary>
        internal static void Pedir(float f)
        {
            if (Main.netMode == NetmodeID.Server) return;
            if (f > _pedido) _pedido = f;
        }

        public override void ModifySunLightColor(ref Color tileColor, ref Color backgroundColor)
        {
            if (Main.gameMenu || Main.netMode == NetmodeID.Server) return;

            // El envejecimiento es por TICK de juego (no por frame).
            uint t = Main.GameUpdateCount;
            if (t != _lastTick)
            {
                _lastTick = t;
                // Decaimiento del pedido (DoG): sin desgarros, la luz vuelve en ~0.5 s.
                _pedido = MathF.Max(0f, _pedido - 0.012f);
                _actual = MathHelper.Lerp(_actual, _pedido, 0.12f);
            }

            if (_actual <= 0.004f) return;

            // El sesgo: el mundo se apaga con un resto VIOLETA-AZULADO (la luz
            // que "se filtra" por la herida), el verde muere más rápido.
            float f = 1f - _actual;
            tileColor.R = (byte)(int)MathF.Min(255f, tileColor.R * f + 8f * _actual);
            tileColor.G = (byte)(int)(tileColor.G * f);
            tileColor.B = (byte)(int)MathF.Min(255f, tileColor.B * f + 26f * _actual);
            backgroundColor.R = (byte)(int)MathF.Min(255f, backgroundColor.R * f + 8f * _actual);
            backgroundColor.G = (byte)(int)(backgroundColor.G * f);
            backgroundColor.B = (byte)(int)MathF.Min(255f, backgroundColor.B * f + 26f * _actual);
        }
    }
}
