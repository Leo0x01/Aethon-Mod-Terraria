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
        /// <summary>La vibración previa a la fractura: onda estacionaria creciendo.</summary>
        Vibracion,
        /// <summary>LA FRACTURA: la línea recta se QUIEBRA en el camino Lichtenberg (daño ×2+).</summary>
        Fractura,
        /// <summary>Cierre: los labios se cierran; el daño cesó 8 ticks antes.</summary>
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
            new(120, 40, 200), new(190, 60, 255), new(140, 240, 255), new(255, 255, 255),
        };
    }

    /// <summary>
    /// RiftLib — v6.28 — LA LIBRERÍA DE LOS DESGARROS DE REALIDAD, SEGUNDA
    /// GENERACIÓN: EL DESGARRO CONTINUO.
    ///
    /// LA LECCIÓN RAÍZ de v6.28 (INFORME_RIFTLIB_V2.md — medido con PIL sobre
    /// las texturas de la casa): la TrailGlow.png de v1 TENÍA EL DEFECTO — su
    /// alfa rampa 3→204 A LO LARGO del eje de longitud y su color era CIAN
    /// PURO (0,255,255). Cada junta entre los 8-24 segmentos del desgarro era
    /// una franja casi transparente y CIAN — las "interrupciones azules" que
    /// el usuario vio. El contrato del ecosistema (verificado sobre las
    /// texturas de línea de Calamity: BloomLineThick/LineThick): UNIFORME a lo
    /// largo del eje, gradiente SOLO a lo ancho, SIN color horneado.
    ///
    /// LA RESPUESTA (v2 — 6 texturas nuevas 100% procedurales, gen_rift_v628.py):
    ///   · EL DESGARRO RECTO ES UN SOLO QUAD: las RiftTaper* (512×64) llevan
    ///     el PERFIL DE LONGITUD horneado (lens sin^0.6 · respiración nebulosa
    ///     ±15% a 4 ciclos) y la SECCIÓN COMPLETA (par de labios a ±0.31·W
    ///     cabalgando el borde del vacío, núcleo razor sobre cada labio).
    ///     CERO juntas porque CERO segmentos — la lección HyperdeathRiftScepterBeam
    ///     de Calamity: su rayo de 3000 px es UN solo quad estirado.
    ///   · EL CAMINO FRACTURADO ES UNA CADENA SIN HUECOS: RiftLip/RiftCore
    ///     (64×16, columnas IDÉNTICAS — desviación medida 0.0000) + anchura
    ///     evaluada en los VÉRTICES compartidos (la lección WidthFunction de
    ///     Calamity) + solape len+w·0.9 + PERLA en cada vértice (el round-join
    ///     estándar) → la herida Lichtenberg continua aunque gire.
    ///   · SIN ABERRACIÓN R/B en los labios: los re-dibujos de canal puro
    ///     (soloB = azul) eran el segundo culpable de las interrupciones
    ///     azules — el vocabulario queda LIMPIO: labios de color + núcleo
    ///     blanco + vacío negro. El eco glitch (EcoGlitch) sigue disponible
    ///     para quien lo quiera, pero el desgarro del arma YA NO LO USA.
    ///
    /// EL VOCABULARIO (lo que no cambió de v1):
    ///   · EL DESGARRO ES UNA LÍNEA con LABIOS DE LUZ y el INTERIOR ES VACÍO
    ///     PROFUNDO (banda que OCLUYE, lote NO-premultiplicado) con ESTRELLAS
    ///     que fluyen a lo largo del eje (scroll élite, −2 px/tick) y paralaje.
    ///   · LA APERTURA ES UN GOLPE (3-4 ticks, Kick perpendicular, Flash) y la
    ///     ESTRELLA DE 4 PUNTAS del punto de ruptura (DoG: vertical ×8 +
    ///     horizontal ×5, todo ·3.25·charge).
    ///   · LA FRACTURA ES EL CLÍMAX (lección v6.28: el vidrio se propaga a
    ///     1458-1500 m/s — en juego la fractura es un evento de 1-2 frames):
    ///     tras la VIBRACIÓN (onda estacionaria 0→3.5 px a ~10 Hz creciendo
    ///     16 ticks — la tensión visible), la línea se QUIEBRA al camino
    ///     Lichtenberg y EL DAÑO PEGA ×2+ en ese instante exacto.
    ///   · EL CIERRE ES JUSTO: el daño cesa 8 ticks ANTES de que el visual muera.
    ///
    /// CONTRATO DE LOTE (idéntico al de StormLib/EstelaLib/OndaLib): los métodos
    /// de DIBUJO pintan en el SpriteBatch que el LLAMADOR les pase y NUNCA lo
    /// abren/cierran. DOS lotes hacen falta para un desgarro completo:
    ///   1. EL VACÍO (TearVacio/GrietaVacio): lote NO-premultiplicado
    ///      (BlendState.NonPremultiplied) — dibujar PRIMERO (occlude antes de iluminar).
    ///   2. LA LUZ (Tear/Grieta/Star): lote ADITIVO — después.
    /// Todo determinista por semilla (misma secuencia SIEMPRE, cero Main.rand);
    /// cero estado de desgarros (progress/fase los lleva el llamador); cero GC
    /// por frame (buffers estáticos reutilizados).
    /// </summary>
    public static class RiftLib
    {
        // ==================================================================
        //  TEXTURAS COMPARTIDAS (resolución diferida)
        // ==================================================================

        private static Asset<Texture2D> _black, _star, _glow, _ring, _orb;
        private static Asset<Texture2D> _taperVelo, _taperCuerpo, _taperNucleo, _taperVoid;
        private static Asset<Texture2D> _lip, _core;

        /// <summary>El disco negro 256² (el VACÍO del camino fracturado — negro aunque sea mediodía).</summary>
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

        // --- LAS TEXTURAS v6.28 (el desgarro continuo — gen_rift_v628.py) ---

        /// <summary>EL VELO del desgarro recto: UN quad con el perfil de longitud horneado.</summary>
        private static Texture2D TaperVeloTex =>
            (_taperVelo ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/RiftTaperVelo")).Value;

        /// <summary>EL CUERPO del desgarro recto: el par de labios a ±0.31·W.</summary>
        private static Texture2D TaperCuerpoTex =>
            (_taperCuerpo ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/RiftTaperCuerpo")).Value;

        /// <summary>EL NÚCLEO RAZOR del desgarro recto (sobre cada labio).</summary>
        private static Texture2D TaperNucleoTex =>
            (_taperNucleo ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/RiftTaperNucleo")).Value;

        /// <summary>EL VACÍO del desgarro recto (pase NO-premultiplicado): negro oclusivo.</summary>
        private static Texture2D TaperVoidTex =>
            (_taperVoid ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/RiftTaperVoid")).Value;

        /// <summary>EL LABIO UNIFORME del camino fracturado (columnas idénticas — sin juntas).</summary>
        private static Texture2D LipTex =>
            (_lip ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/RiftLip")).Value;

        /// <summary>EL NÚCLEO UNIFORME del camino fracturado (banda dura 30%).</summary>
        private static Texture2D CoreTex =>
            (_core ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/RiftCore")).Value;

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
        //  EL DESGARRO RECTO — LA LÍNEA EN UN SOLO QUAD (v6.28)
        // ==================================================================

        /// <summary>La altura del quad del desgarro recto (el contrato de las RiftTaper*): 1.60·maxWidth.</summary>
        public const float QuadAlto = 1.60f;

        /// <summary>
        /// EL VACÍO DEL DESGARRO RECTO: UN SOLO QUAD RiftTaperVoid (512×64) —
        /// la banda negra que OCLUYE con el perfil de longitud horneado (gorda
        /// al centro, aguja en las puntas, ±6% de respiración). Se dibuja en el
        /// LOTE NO-PREMULTIPLICADO del llamador (dibujar ANTES de la luz).
        /// CERO juntas: el desgarro entero es una sola pieza.
        /// </summary>
        /// <param name="batch">Batch ABIERTO (BlendState.NonPremultiplied recomendado).</param>
        /// <param name="progress">0..1 vida del desgarro (0-0.08 apertura, 0.85-1 cierre).</param>
        /// <param name="maxWidth">Ancho MÁXIMO del desgarro abierto (8..24 px recomendado).</param>
        public static void TearVacio(SpriteBatch batch, Vector2 origin, Vector2 dir,
            float length, float progress, float maxWidth, int seed, float time)
        {
            if (batch == null || length < 8f) return;

            float h = QuadAlto * maxWidth * Apertura(progress);
            if (h < 0.8f) return;

            float breathe = VFXCore.Breathe(time, 2.2f, seed, 0.06f);
            float rot = MathF.Atan2(dir.Y, dir.X);
            Vector2 center = origin + dir * (length * 0.5f);

            batch.Draw(TaperVoidTex, center, null,
                Tint(Color.White, 0.96f * breathe), rot,
                new Vector2(TaperVoidTex.Width, TaperVoidTex.Height) * 0.5f,
                new Vector2(length, h * breathe) / new Vector2(TaperVoidTex.Width, TaperVoidTex.Height),
                SpriteEffects.None, 0f);
        }

        /// <summary>
        /// DIBUJA EL DESGARRO RECTO (el pase de LUZ) — v6.28: TRES QUADS, CERO
        /// JUNTAS. La línea de <paramref name="origin"/> a
        /// origin+dir·<paramref name="length"/> con anchura viva
        /// Apertura(progress)·maxWidth·QuadAlto, toda la anatomía (par de labios
        /// a ±0.31·W + núcleo razor sobre cada labio + perfil de longitud con
        /// respiración nebulosa ±15% a 4 ciclos) HORNEADA en las RiftTaper*:
        ///   1. EL VELO (RiftTaperVelo, tinte de paleta[0], α 0.30·intensity).
        ///   2. EL CUERPO (RiftTaperCuerpo, tinte de paleta[1], α 0.60·intensity).
        ///   3. EL NÚCLEO (RiftTaperNucleo, tinte BLANCO de paleta[última], α 0.90·intensity).
        ///   4. LAS ESTRELLAS del interior (16-28, scroll + paralaje + parpadeo).
        /// SIN aberración R/B (v6.28: los re-dibujos de canal azul eran las
        /// "interrupciones azules" — el vocabulario queda limpio).
        /// </summary>
        /// <param name="batch">Batch ABIERTO (aditivo recomendado).</param>
        /// <param name="progress">0..1 vida del desgarro (0-0.08 apertura, 0.85-1 cierre).</param>
        /// <param name="maxWidth">Ancho MÁXIMO del desgarro abierto (8..24 px recomendado).</param>
        /// <param name="ecoOffset">Offset del eco glitch (re-dibujo desplazado).</param>
        /// <param name="ecoTint">Tinte del eco (null = sin eco).</param>
        public static void Tear(SpriteBatch batch, Vector2 origin, Vector2 dir,
            float length, float progress, float maxWidth, Color[] paleta,
            float intensity, int seed, float time,
            Vector2 ecoOffset = default, Color? ecoTint = null)
        {
            if (batch == null || paleta == null || paleta.Length == 0 || length < 8f) return;

            float h = QuadAlto * maxWidth * Apertura(progress);
            if (h < 0.8f) return;
            intensity = MathHelper.Clamp(intensity, 0f, 1f);
            if (intensity <= 0.02f) return;

            float breathe = VFXCore.Breathe(time, 2.2f, seed, 0.06f);
            float beat = 0.90f + 0.10f * MathF.Sin(time * 7.3f + seed);
            float rot = MathF.Atan2(dir.Y, dir.X);
            Vector2 center = origin + dir * (length * 0.5f) + ecoOffset;

            Color velo = Eco(Tint(Pal(paleta, 0), 0.30f * intensity * beat), ecoTint);
            Color cuerpo = Eco(Tint(Pal(paleta, 1), 0.60f * intensity), ecoTint);
            Color nucleo = Eco(Tint(Pal(paleta, paleta.Length - 1), 0.90f * intensity), ecoTint);

            var texSize = new Vector2(TaperVeloTex.Width, TaperVeloTex.Height);
            var scale = new Vector2(length, h * breathe) / texSize;
            var originPx = texSize * 0.5f;

            // === 1+2+3: LOS TRES QUADS (velo → cuerpo → núcleo) ===
            batch.Draw(TaperVeloTex, center, null, velo, rot, originPx, scale, SpriteEffects.None, 0f);
            batch.Draw(TaperCuerpoTex, center, null, cuerpo, rot, originPx, scale, SpriteEffects.None, 0f);
            batch.Draw(TaperNucleoTex, center, null, nucleo, rot, originPx, scale, SpriteEffects.None, 0f);

            // === 4: LAS ESTRELLAS del interior (el vacío fluye — scroll élite) ===
            // (En los re-dibujos de ECO no: el glitch es de los LABIOS.)
            if (ecoTint == null)
                Estrellas(batch, origin, dir, length, maxWidth * Apertura(progress),
                    paleta, intensity, seed, time, 2f, 16 + seed % 13);
        }

        // ==================================================================
        //  LA ESTRELLA DE RUPTURA + EL PAQUETE DE IMPACTO
        // ==================================================================

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
        /// EL PAQUETE DE IMPACTO del desgarro (la apertura/fractura ES un golpe):
        /// Kick de cámara PERPENDICULAR a la línea, Flash de pantalla (tinte de
        /// paleta), el sonido grave de la ruptura (pitch grave) y la RÁFAGA de
        /// chispas de anomalía vía ParticleManager (cliente). Se llama UNA VEZ.
        /// Registro puro: NADA de dibujo aquí.
        /// </summary>
        /// <param name="fuerza">Escala del golpe (1 = apertura; 1.3 = fractura).</param>
        public static void TearImpacto(Vector2 origin, Vector2 dir, float length,
            Color[] paleta, int seed, float fuerza = 1f)
        {
            if (paleta == null || paleta.Length == 0) return;
            fuerza = MathHelper.Clamp(fuerza, 0.5f, 2f);

            // El KICK perpendicular al corte: la cámara se desplaza AL LADO,
            // como si la realidad hubiera TRONADO (no hacia adelante).
            if (dir.X != 0f || dir.Y != 0f)
            {
                Vector2 p = Vector2.Normalize(new(-dir.Y, dir.X));
                OndaLib.Kick(7f * fuerza, 12, MathF.Atan2(p.Y, p.X));
            }
            else
                OndaLib.Kick(7f * fuerza, 12, -1f);

            OndaLib.Flash(Pal(paleta, 1), MathHelper.Clamp(0.22f * fuerza, 0.15f, 0.34f), 8);

            // El sonido de la ruptura: hondo, con el mundo "asentándose" después
            // (pitch −0.65 apertura / −0.75 fractura — la lección DoG).
            if (Main.netMode != NetmodeID.Server)
            {
                try
                {
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item8.WithPitchOffset(fuerza > 1.1f ? -0.75f : -0.65f), origin);
                }
                catch { }
            }

            // LA RÁFAGA de chispas de anomalía (solo cliente).
            if (Main.netMode != NetmodeID.Server)
            {
                ChispasAnomalia(origin, (int)(12 * fuerza), paleta, seed, out ParticleData[] motas);
                if (motas != null)
                    for (int i = 0; i < motas.Length; i++)
                        ParticleManager.Spawn(motas[i]);
            }
        }

        // ==================================================================
        //  EL CAMINO — la herida FRACTURADA (la cadena sin huecos)
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
        /// <param name="fallas">Probabilidad de micro-falla por paso (1/9 normal, 1/4 = furia).</param>
        public static Vector2[] CaminoGrieta(Vector2 origin, Vector2 dir,
            int seed, int points = 24, float pasoMin = 25f, float pasoMax = 50f,
            float curvatura = 5f, float fallas = 9f)
        {
            points = Math.Clamp(points, 4, 40);
            fallas = MathHelper.Clamp(fallas, 3f, 12f);
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

                // MICRO-FALLA: el quiebre brusco — la firma Lichtenberg.
                if (H01(seed, i, 211) < 1f / fallas)
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
        /// EL CAMINO DE LA VIBRACIÓN (v6.28): la línea RECTA con la ONDA
        /// ESTACIONARIA creciendo — la tensión visible antes de la fractura
        /// (lección v6.28: amplitud 0→máx, ~10 Hz, 2 nodos). Determinista.
        /// </summary>
        /// <param name="amplitud">0..máx px del vaivén lateral.</param>
        public static Vector2[] CaminoVibracion(Vector2 origin, Vector2 dir, float length,
            float amplitud, float time, int nodos = 2)
        {
            int points = Math.Clamp((int)(length / 40f), 10, 20);
            var pts = new Vector2[points];
            dir = dir.LengthSquared() > 0.0001f ? Vector2.Normalize(dir) : Vector2.One;
            Vector2 perp = new(-dir.Y, dir.X);

            for (int i = 0; i < points; i++)
            {
                float f = i / (float)(points - 1);
                // LA ONDA ESTACIONARIA: nodos en los extremos (la herida está
                // CLAVADA en ambos extremos mientras vibra) — sin(f·π·nodos).
                float onda = MathF.Sin(f * MathHelper.Pi * nodos) *
                             MathF.Sin(time * MathHelper.TwoPi * 10f);   // ~10 Hz
                pts[i] = origin + dir * (length * f) + perp * (onda * amplitud);
            }
            return pts;
        }

        /// <summary>
        /// EL VACÍO DE LA HERIDA FRACTURADA (pase no-premultiplicado): bandas
        /// negras BlackDisk a lo largo del camino con la anchura evaluada EN LOS
        /// VÉRTICES (lección Calamity WidthFunction — sin escalones), solape
        /// len+w y el taper raíz→punta con la respiración ±8%. Dibujar ANTES de
        /// <see cref="Grieta"/>. Los extremos REDONDEADOS del BlackDisk hacen de
        /// junta natural (el round-join del estándar).
        /// </summary>
        public static void GrietaVacio(SpriteBatch batch, Vector2[] camino, float progress,
            float maxWidth, int seed, float time)
        {
            if (batch == null || camino == null || camino.Length < 2) return;

            float vida = MathF.Pow(1f - MathHelper.Clamp(progress, 0f, 1f), 0.8f);
            float respira = 1f + 0.08f * MathF.Sin(time * 2.2f + seed * 0.13f);
            if (LongitudCamino(camino) < 8f) return;
            float[] ws = AnchosCamino(camino, maxWidth);

            for (int i = 0; i < camino.Length - 1; i++)
            {
                Vector2 a = camino[i];
                Vector2 b = camino[i + 1];
                float len = Vector2.Distance(a, b);
                if (len < 0.35f) continue;

                float wseg = (ws[i] + ws[i + 1]) * 0.5f * respira;
                if (wseg < 0.5f) continue;

                float rot = MathF.Atan2(b.Y - a.Y, b.X - a.X);
                Quad(batch, BlackTex, (a + b) * 0.5f,
                    new Vector2(len + wseg * 0.35f, wseg * 0.62f * vida + 0.8f), rot,
                    Tint(Color.Black, 0.94f * vida));
            }
        }

        /// <summary>
        /// DIBUJA LA HERIDA FRACTURADA (el pase de LUZ) sobre el camino — v6.28:
        /// LA CADENA SIN HUECOS. Por SEGMENTO: RiftLip (textura UNIFORME a lo
        /// largo — la desviación medida 0.0000) en DOS capas (velo ×1.6 alto +
        /// cuerpo) con la anchura evaluada EN LOS VÉRTICES compartidos, solape
        /// len+w·0.9 (cubre giros ≤~53°) y RiftCore como núcleo razor; por
        /// VÉRTICE interior: LA PERLA (un RiftLip cuadrado de diámetro w — el
        /// round-join estándar que mata los huecos en las esquinas). ESTRELLAS
        /// FIJAS (la grieta NO scrollea: es una herida) y CHISPAS de anomalía
        /// deterministas. SIN aberración R/B (v6.28).
        /// </summary>
        /// <param name="progress">0..1 de la vida de la herida.</param>
        /// <param name="chispas">True = dibuja las chispas de anomalía (1 cada 3 ticks).</param>
        /// <param name="ecoOffset">Offset del eco glitch.</param>
        /// <param name="ecoTint">Tinte del eco (null = sin eco).</param>
        public static void Grieta(SpriteBatch batch, Vector2[] camino, float progress,
            float maxWidth, Color[] paleta, float intensity, int seed, float time,
            bool chispas = true, Vector2 ecoOffset = default, Color? ecoTint = null)
        {
            if (batch == null || camino == null || camino.Length < 2 || paleta == null || paleta.Length == 0) return;
            intensity = MathHelper.Clamp(intensity, 0f, 1f);
            if (intensity <= 0.02f) return;

            float vida = MathF.Pow(1f - MathHelper.Clamp(progress, 0f, 1f), 0.8f);
            float respira = 1f + 0.08f * MathF.Sin(time * 2.2f + seed * 0.13f);

            if (LongitudCamino(camino) < 8f) return;
            float[] ws = AnchosCamino(camino, maxWidth);

            Color velo = Pal(paleta, 0);
            Color cuerpo = Pal(paleta, 1);
            Color nucleo = Pal(paleta, paleta.Length - 1);

            int k = 0;
            for (int i = 0; i < camino.Length - 1; i++)
            {
                Vector2 a = camino[i];
                Vector2 b = camino[i + 1];
                float len = Vector2.Distance(a, b);
                if (len < 0.35f) continue;

                // LA ANCHURA EN EL VÉRTICE COMPARTIDO (la lección WidthFunction):
                // promedio de los dos vértices del segmento — sin escalones.
                float wa = ws[i] * respira;
                float wb = ws[i + 1] * respira;
                float wseg = (wa + wb) * 0.5f;
                if (wseg < 0.4f) continue;

                Vector2 mid = (a + b) * 0.5f;
                Vector2 delta = b - a;
                float rot = MathF.Atan2(delta.Y, delta.X);
                // El latido de la herida (vive, no es un dibujo muerto).
                float beat = 0.88f + 0.12f * MathF.Sin(time * 6.1f + k * 1.7f + seed);

                // === EL VELO (×1.6 de alto, α 0.30 — el halo que integra) ===
                LipQuad(batch, mid + ecoOffset, len + wseg * 0.9f, wseg * 1.6f, rot,
                    Eco(Tint(velo, 0.30f * intensity * vida * beat), ecoTint));

                // === EL CUERPO (el labio de color, α 0.60) ===
                LipQuad(batch, mid + ecoOffset, len + wseg * 0.9f, wseg, rot,
                    Eco(Tint(cuerpo, 0.60f * intensity * vida), ecoTint));

                // === EL NÚCLEO RAZOR (banda dura 30% de RiftCore, α 0.90) ===
                LipQuadCore(batch, mid + ecoOffset, len + wseg * 0.9f, wseg * 0.8f, rot,
                    Eco(Tint(nucleo, 0.90f * intensity * vida), ecoTint));

                // === LA PERLA del vértice compartido (el round-join: CERO
                //     huecos en las esquinas — el estándar regl-gpu-lines) ===
                if (i > 0)
                {
                    LipQuad(batch, a + ecoOffset, wseg * 1.15f, wseg * 1.6f, rot,
                        Eco(Tint(velo, 0.30f * intensity * vida * beat), ecoTint));
                    LipQuad(batch, a + ecoOffset, wseg * 1.15f, wseg, rot,
                        Eco(Tint(cuerpo, 0.60f * intensity * vida), ecoTint));
                    LipQuadCore(batch, a + ecoOffset, wseg * 1.15f, wseg * 0.8f, rot,
                        Eco(Tint(nucleo, 0.90f * intensity * vida), ecoTint));
                }
                k++;
            }

            // === LAS ESTRELLAS FIJAS de la herida (sin scroll, con paralaje) ===
            EstrellasCamino(batch, camino, maxWidth * respira, paleta, intensity * vida, seed, time, 14 + seed % 7);

            // === LAS CHISPAS DE ANOMALÍA (máx 1 cada 3 ticks, deterministas) ===
            if (chispas && ecoTint == null && vida > 0.15f)
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
                    Quad(batch, CoreTex, p + new Vector2(jx, jy) + d * 4f,
                        new Vector2(7f, 1.4f), ang, Tint(nucleo, 0.7f * vida));
                    Quad(batch, CoreTex, p + new Vector2(jx, jy) - d * 4f,
                        new Vector2(7f, 1.4f), ang, Tint(nucleo, 0.7f * vida));
                }
            }
        }

        /// <summary>La longitud total de un camino (px).</summary>
        public static float LongitudCamino(Vector2[] camino)
        {
            if (camino == null || camino.Length < 2) return 0f;
            float total = 0f;
            for (int i = 1; i < camino.Length; i++)
                total += Vector2.Distance(camino[i - 1], camino[i]);
            return total;
        }

        /// <summary>
        /// LOS ANCHOS DEL CAMINO (una pasada): la anchura en cada VÉRTICE con el
        /// taper raíz→punta (1.0 en la raíz, aguja en la punta) — la lección
        /// Calamity WidthFunction: la anchura vive en los VÉRTICES compartidos,
        /// nunca en los centros de segmento (sin escalones en las juntas).
        /// </summary>
        public static float[] AnchosCamino(Vector2[] camino, float maxWidth)
        {
            if (camino == null || camino.Length < 2) return Array.Empty<float>();
            var ws = new float[camino.Length];
            float total = LongitudCamino(camino);
            if (total < 1f)
            {
                for (int i = 0; i < ws.Length; i++) ws[i] = maxWidth;
                return ws;
            }
            float arc = 0f;
            ws[0] = maxWidth;
            for (int i = 1; i < camino.Length; i++)
            {
                arc += Vector2.Distance(camino[i - 1], camino[i]);
                float t = arc / total;
                // TAPER: gorda en la raíz (donde nació el desgarro), aguja en la punta.
                ws[i] = maxWidth * MathF.Pow(1f - t, 0.9f);
            }
            return ws;
        }

        /// <summary>
        /// ¿El hitbox toca la CÁPSULA del camino? (taper local + franja de gracia) —
        /// la colisión de la herida fracturada, lista para el daño de la FRACTURA.
        /// </summary>
        public static bool CaminoToca(Vector2[] camino, float maxWidth, Rectangle hitbox, float gracia = 8f)
        {
            if (camino == null || camino.Length < 2) return false;
            float[] ws = AnchosCamino(camino, maxWidth);
            for (int i = 0; i < camino.Length - 1; i++)
            {
                Vector2 a = camino[i];
                Vector2 b = camino[i + 1];
                Vector2 d = b - a;
                float len = d.Length();
                if (len < 0.35f) continue;
                if (LineaToca(a, d / len, len, MathF.Max(ws[i], ws[i + 1]) + gracia, hitbox))
                    return true;
            }
            return false;
        }

        // ==================================================================
        //  LAS PIEZAS SUELTAS (los quiere todo el mundo)
        // ==================================================================

        /// <summary>
        /// SHARDS DE CRISTAL: 6-10 esquirlas + 1 GRANDE: nacen a lo largo de la
        /// línea, salen con velocidad perpendicular ± jitter, rotan, CAEN
        /// (gravedad de vidrio 0.11) y mueren desvaneciéndose. Devuelve el
        /// paquete para el ParticleManager (la librería NO spawnea).
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
        /// números DoG — vel 8-14 radial adaptados al ParticleManager), color
        /// Lerp(paleta, Blanco, 0.65), vida 30-45, mota 2-4 px.
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
        /// horizontales alternos ±(2..3) px con tintes de canal y alpha
        /// decreciente. El llamador re-dibuja su desgarro/grieta con estos
        /// offsets. Los arrays son buffers estáticos (cero GC).
        /// (v6.28: el desgarro del arma YA NO lo usa — el usuario lo leyó como
        /// "interrupciones"; queda disponible para otros llamadores.)
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

                // Tinte de canal (R para un lado, B para el otro) con alpha decreciente.
                float a = 0.30f * (1f - i / 3f);
                _ecoTint[i] = i % 2 == 0
                    ? new Color(255, 0, 0, (byte)(int)(255f * a))
                    : new Color(0, 90, 255, (byte)(int)(255f * a));
            }
            offsets = _ecoOff;
            tintes = _ecoTint;
        }

        // ==================================================================
        //  EL MUNDO — el oscurecer de la realidad herida
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

        /// <summary>El quad del LABIO del camino (RiftLip — textura uniforme, sin juntas).</summary>
        private static void LipQuad(SpriteBatch batch, Vector2 pos,
            float len, float w, float rot, Color tint)
            => Quad(batch, LipTex, pos, new Vector2(len, w), rot, tint);

        /// <summary>El quad del NÚCLEO del camino (RiftCore — banda dura uniforme).</summary>
        private static void LipQuadCore(SpriteBatch batch, Vector2 pos,
            float len, float w, float rot, Color tint)
            => Quad(batch, CoreTex, pos, new Vector2(len, w), rot, tint);

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

        // ==================================================================
        //  EL INTERIOR — el campo de estrellas del vacío
        // ==================================================================

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
    }

    /// <summary>
    /// RiftMundoSystem — v6.26 — EL OSCURECER DE LA REALIDAD DESGARRADA.
    ///
    /// El complemento de mundo de RiftLib (lección DoG: cuando el espacio se
    /// abre, EL MUNDO SE OSCURECE — la luz "se cae" dentro de la herida).
    /// Un acumulador estático: los desgarros activos piden su oscuridad
    /// (<see cref="RiftLib.Oscurecer"/>) y el sistema la aplica SOLO en
    /// ModifySunLightColor (el hook oficial de tML), con sesgo violeta-azulado
    /// y decaimiento suave (~0.5 s al dejar de pedir). Cliente-only.
    ///
    /// v6.28: también lo usa EL ECLIPSE PRIMORDIAL rediseñado (el día se
    /// apaga durante el eclipse y VUELVE en la nova) — el sistema es la
    /// "oscuridad prestada" de la casa.
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
