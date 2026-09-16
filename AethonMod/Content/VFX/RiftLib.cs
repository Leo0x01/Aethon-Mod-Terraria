using System;
using System.Collections.Generic;
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
        /// <summary>LA FRACTURA: EL CLÍMAX ÓPTICO (v6.31: la línea PERMANECE RECTA —
        /// flash + ancho ×1.35 + daño ×2.2; la fractura es un evento de LUZ, no de geometría).</summary>
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
    /// RiftLib — v6.31 — LA LIBRERÍA DE LOS DESGARROS DE REALIDAD, TERCERA
    /// GENERACIÓN: LA LÍNEA CONTINUA Y PAREJA.
    ///
    /// LA LECCIÓN RAÍZ de v6.31 (research/v631/INFORME_TAJOS_CORTE_REALIDAD.md,
    /// 54 búsquedas + las fuentes de referencia leídas línea a línea):
    /// NADIE implementa el corte de realidad como ramas — los referentes de
    /// primera línea usan UNA
    /// SOLA LÍNEA CONTINUA (recta o arco único) y la lectura de "realidad
    /// cortada" vive en la ANCHURA, el COLOR y el TIMING, no en la
    /// fragmentación. El huso horneado en las RiftTaper* de v6.28 (100% SOLO
    /// al centro, 0.54 en u=0.10) + la respiración ±15% a 4 ciclos + el
    /// ramillete Lichtenberg de v6.30 eran EXACTAMENTE lo que el ojo lee como
    /// línea discontinua y despareja.
    ///
    /// LA RESPUESTA (v3 — texturas regeneradas, gen_rift_meseta_v631.py):
    ///   · EL DESGARRO RECTO ES UN SOLO QUAD con PERFIL DE MESETA: ancho 100%
    ///     en u∈[0.10, 0.90] (el 80% del largo), TAPAS REDONDAS circulares en
    ///     los extremos (la cápsula de las líneas continuas de verdad — la
    ///     regla CyberRift: cuerpo plano + recogida MÍNIMA). El ancho NUNCA
    ///     respira: la vida la pone la ALPHA (el latido es de brillo, no de
    ///     tamaño — un ancho que late se lee "no parejo").
    ///   · LA ANATOMÍA VERTICAL (quad 1.60·W): banda de VACÍO sólida al 62.5%
    ///     (= maxWidth en pantalla: el "14 constante" del contrato), LOS DOS
    ///     LABIOS en el borde del vacío, los FIL RAZOR sobre los labios y EL
    ///     CENTRO CEGADOR (lección Last Prism: blanco puro ×0.5 del ancho a
    ///     α 0.14 — profundidad dentro de la herida).
    ///   · EL CAMINO VIBRANTE ES LA GEMELA DEL QUAD: los segmentos usan las
    ///     MISMAS texturas Taper RECORTADAS a la meseta (u∈[0.30,0.70]) con
    ///     anchura en los VÉRTICES compartidos + solape len+w + PERLA en cada
    ///     vértice; el latido de la cadena es SUAVE a lo largo del arco (sin
    ///     sin(k·1.7) por segmento: las "perlas-oscuras" alternadas se leían
    ///     como cuentas separadas).
    ///   · SIN ABERRACIÓN R/B, SIN ramillete, SIN shards, SIN ramas: la
    ///     herida es UNA línea de punta a punta durante TODA su vida.
    ///
    /// EL VOCABULARIO (lo que no cambió):
    ///   · EL DESGARRO ES UNA LÍNEA con LABIOS DE LUZ y el INTERIOR ES VACÍO
    ///     PROFUNDO (banda que OCLUYE, lote NO-premultiplicado) con ESTRELLAS
    ///     que fluyen a lo largo del eje (scroll élite) y paralaje.
    ///   · LA APERTURA ES UN GOLPE (3-4 ticks, Kick perpendicular, Flash) y la
    ///     ESTRELLA DE 4 PUNTAS del punto de ruptura (DoG: vertical ×8 +
    ///     horizontal ×5, todo ·3.25·charge).
    ///   · LA FRACTURA ES EL CLÍMAX ÓPTICO: tras la VIBRACIÓN (onda estacionaria
    ///     0→3.5 px a ~10 Hz creciendo 16 ticks), flash 0.30 + ancho ×1.35 +
    ///     el daño ×2.2 — y la línea SIGUE RECTA (más intensa: la herida abierta).
    ///   · EL CIERRE SE COME EL CORTE DESDE LOS EXTREMOS (ErodeT direccional
    ///     CWR): la línea se acorta hacia el centro SIN menguar el ancho —
    ///     "la realidad sana comiéndose el corte".
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

        private static Asset<Texture2D> _star, _glow, _ring, _orb;
        private static Asset<Texture2D> _taperVelo, _taperCuerpo, _taperNucleo, _taperVoid;
        private static Asset<Texture2D> _core;

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

        // --- LAS TEXTURAS v6.31 (la meseta — gen_rift_meseta_v631.py) ---

        /// <summary>EL VELO del desgarro (halo integrador, alto 0.94·quad).</summary>
        private static Texture2D TaperVeloTex =>
            (_taperVelo ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/RiftTaperVelo")).Value;

        /// <summary>EL CUERPO del desgarro: los DOS LABIOS en el borde del vacío.</summary>
        private static Texture2D TaperCuerpoTex =>
            (_taperCuerpo ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/RiftTaperCuerpo")).Value;

        /// <summary>EL NÚCLEO del desgarro: filos razor + el centro cegador.</summary>
        private static Texture2D TaperNucleoTex =>
            (_taperNucleo ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/RiftTaperNucleo")).Value;

        /// <summary>EL VACÍO del desgarro (pase NO-premultiplicado): banda negra sólida 0.625·quad.</summary>
        private static Texture2D TaperVoidTex =>
            (_taperVoid ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/RiftTaperVoid")).Value;

        /// <summary>EL NÚCLEO UNIFORME (banda dura 30% — los rayitos de las chispas).</summary>
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
        /// la banda negra que OCLUYE con el PERFIL DE MESETA horneado (ancho
        /// 100% en el 80% central + tapas redondas — v6.31: el huso era la
        /// raíz de lo "discontinuo"). Se dibuja en el LOTE NO-PREMULTIPLICADO
        /// del llamador (dibujar ANTES de la luz).
        /// CERO juntas: el desgarro entero es una sola pieza; el ancho NO
        /// respira (la vida la pone la alpha del tinte).
        /// </summary>
        /// <param name="batch">Batch ABIERTO (BlendState.NonPremultiplied recomendado).</param>
        /// <param name="progress">0..1 vida del desgarro (0-0.08 apertura, 0.85-1 cierre).</param>
        /// <param name="maxWidth">Ancho MÁXIMO del desgarro abierto (8..24 px recomendado).</param>
        /// <param name="anchoMul">Multiplicador del ancho (1 = normal; 1.35 = el pulso de la fractura).</param>
        public static void TearVacio(SpriteBatch batch, Vector2 origin, Vector2 dir,
            float length, float progress, float maxWidth, int seed, float time,
            float anchoMul = 1f)
        {
            if (batch == null || length < 8f) return;

            float h = QuadAlto * maxWidth * Apertura(progress) * anchoMul;
            if (h < 0.8f) return;

            float breathe = VFXCore.Breathe(time, 2.2f, seed, 0.06f);
            float rot = MathF.Atan2(dir.Y, dir.X);
            Vector2 center = origin + dir * (length * 0.5f);

            // v6.31: EL ANCHO ES CONSTANTE — la respiración vive en la ALPHA.
            batch.Draw(TaperVoidTex, center, null,
                Tint(Color.White, 0.96f * breathe), rot,
                new Vector2(TaperVoidTex.Width, TaperVoidTex.Height) * 0.5f,
                new Vector2(length, h) / new Vector2(TaperVoidTex.Width, TaperVoidTex.Height),
                SpriteEffects.None, 0f);
        }

        /// <summary>
        /// DIBUJA EL DESGARRO RECTO (el pase de LUZ) — v6.31: TRES QUADS, CERO
        /// JUNTAS, PERFIL DE MESETA. La línea de <paramref name="origin"/> a
        /// origin+dir·<paramref name="length"/> con anchura viva
        /// Apertura(progress)·maxWidth·anchoMul·QuadAlto, toda la anatomía
        /// (banda de vacío 62.5% + LOS DOS LABIOS en su borde + filos razor +
        /// EL CENTRO CEGADOR — lección Last Prism) HORNEADA en las RiftTaper*:
        ///   1. EL VELO (RiftTaperVelo, tinte de paleta[0], α 0.30·intensity).
        ///   2. EL CUERPO (RiftTaperCuerpo, tinte de paleta[1], α 0.60·intensity).
        ///   3. EL NÚCLEO (RiftTaperNucleo, tinte BLANCO de paleta[última], α 0.90·intensity).
        ///   4. LAS ESTRELLAS del interior (16-28, scroll + paralaje + parpadeo).
        /// EL ANCHO NO RESPIRA (v6.31 — un ancho que late se lee "no parejo");
        /// la vida la pone el latido de la ALPHA del velo. SIN aberración R/B.
        /// </summary>
        /// <param name="batch">Batch ABIERTO (aditivo recomendado).</param>
        /// <param name="progress">0..1 vida del desgarro (0-0.08 apertura, 0.85-1 cierre).</param>
        /// <param name="maxWidth">Ancho MÁXIMO del desgarro abierto (8..24 px recomendado).</param>
        /// <param name="anchoMul">Multiplicador del ancho (1 = normal; 1.35 = el pulso de la fractura).</param>
        /// <param name="ecoOffset">Offset del eco glitch (re-dibujo desplazado).</param>
        /// <param name="ecoTint">Tinte del eco (null = sin eco).</param>
        public static void Tear(SpriteBatch batch, Vector2 origin, Vector2 dir,
            float length, float progress, float maxWidth, Color[] paleta,
            float intensity, int seed, float time,
            float anchoMul = 1f, Vector2 ecoOffset = default, Color? ecoTint = null)
        {
            if (batch == null || paleta == null || paleta.Length == 0 || length < 8f) return;

            float h = QuadAlto * maxWidth * Apertura(progress) * anchoMul;
            if (h < 0.8f) return;
            intensity = MathHelper.Clamp(intensity, 0f, 1f);
            if (intensity <= 0.02f) return;

            float beat = 0.90f + 0.10f * MathF.Sin(time * 7.3f + seed);
            float rot = MathF.Atan2(dir.Y, dir.X);
            Vector2 center = origin + dir * (length * 0.5f) + ecoOffset;

            Color velo = Eco(Tint(Pal(paleta, 0), 0.30f * intensity * beat), ecoTint);
            Color cuerpo = Eco(Tint(Pal(paleta, 1), 0.60f * intensity), ecoTint);
            Color nucleo = Eco(Tint(Pal(paleta, paleta.Length - 1), 0.90f * intensity), ecoTint);

            var texSize = new Vector2(TaperVeloTex.Width, TaperVeloTex.Height);
            // v6.31: EL ANCHO ES CONSTANTE — cero respiración de escala.
            var scale = new Vector2(length, h) / texSize;
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
        //  EL CAMINO DE LA VIBRACIÓN — la tensión visible antes del golpe
        // ==================================================================

        /// <summary>
        /// EL CAMINO DE LA VIBRACIÓN (v6.28): la línea RECTA con la ONDA
        /// ESTACIONARIA creciendo — la tensión visible antes del golpe
        /// (lección v6.28: amplitud 0→máx, ~10 Hz, 2 nodos). Determinista.
        /// v6.31: es EL ÚNICO camino del desgarro (sin ramas, sin Lichtenberg —
        /// la única curvatura permitida es esta onda de amplitud ≤3.5 px).
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
        /// EL VACÍO DE LA CADENA (pase no-premultiplicado) — v6.31: LA GEMELA
        /// DEL QUAD. Cada segmento se dibuja con la MISMA textura RiftTaperVoid
        /// RECORTADA a la meseta (u∈[0.30,0.70] — sin estadio) y estirada a
        /// (len+e₀+e₁, 1.60·wseg): la banda negra, sus bordes y la altura son
        /// IDÉNTICOS a <see cref="TearVacio"/> → CERO salto visual cuando la
        /// línea empieza a vibrar. EL SOLAPE ES ADAPTATIVO AL GIRO REAL (v6.31:
        /// el solape completo len+w apilaba la LUZ 2-3× en cada junta — las
        /// "cuentas brillantes" medidas por el mock; el vacío es inmune al
        /// apilamiento pero su solape vive alineado con el de la luz): cada
        /// segmento se alarga e = w/2·tan(δ/2)+0.75 por vértice girado δ (para
        /// la onda de 2° son ~1 px; para giros de 90°+ degrada al solape
        /// completo clásico) + PERLA del vacío en TODOS los vértices (cobertura
        /// garantizada pase lo que pase) + PERLA de punta. La respiración vive
        /// en la ALPHA, nunca en el ancho. Dibujar ANTES de <see cref="Grieta"/>.
        /// </summary>
        public static void GrietaVacio(SpriteBatch batch, Vector2[] camino, float progress,
            float maxWidth, int seed, float time, bool plano = false)
        {
            if (batch == null || camino == null || camino.Length < 2) return;

            float vida = MathF.Pow(1f - MathHelper.Clamp(progress, 0f, 1f), 0.8f);
            float respira = 1f + 0.08f * MathF.Sin(time * 2.2f + seed * 0.13f);
            if (LongitudCamino(camino) < 8f) return;
            float[] ws = AnchosCamino(camino, maxWidth, plano);

            for (int i = 0; i < camino.Length - 1; i++)
            {
                Vector2 a = camino[i];
                Vector2 b = camino[i + 1];
                float len = Vector2.Distance(a, b);
                if (len < 0.30f) continue;

                float wa = MathF.Max(ws[i], 0.6f);
                float wb = MathF.Max(ws[i + 1], 0.6f);
                float wseg = (wa + wb) * 0.5f;
                float wmax = MathF.Max(wa, wb);

                float rot = MathF.Atan2(b.Y - a.Y, b.X - a.X);

                // EL SOLAPE ADAPTATIVO: el giro real en cada vértice del segmento.
                float giroA = i > 0 ? GiroEn(camino, i) : 0f;
                float giroB = i < camino.Length - 2 ? GiroEn(camino, i + 1) : 0f;
                float largo = len + ExtensionSolape(wmax, giroA) + ExtensionSolape(wmax, giroB);

                // LOS SEGMENTOS EXTREMOS llevan la textura ESTADIO COMPLETA: su
                // tapa redonda ES el arranque/la punta de la herida (v6.31: la
                // perla de raíz sobresalía 8.75 px FUERA del camino — pelo negro
                // hacia atrás sin luz, medido por el mock).
                bool extremo = i == 0 || i == camino.Length - 2;

                // EL SEGMENTO (alineado con la luz — misma extensión).
                TaperQuad(batch, TaperVoidTex, (a + b) * 0.5f, largo, QuadAlto * wseg, rot,
                    Tint(Color.White, 0.96f * vida * respira), extremo);

                // LA PERLA del vértice (el round-join del vacío — SIEMPRE salvo
                // en la RAÍZ: el estadio del primer segmento ya redondea ahí;
                // el negro apilado sobre negro es idempotente, cobertura gratis).
                if (i > 0)
                    TaperQuad(batch, TaperVoidTex, a, wmax * 1.25f, QuadAlto * wseg, rot,
                        Tint(Color.White, 0.96f * vida * respira));
            }
        }

        /// <summary>
        /// DIBUJA LA HERIDA VIVA (el pase de LUZ) sobre el camino — v6.31: LA
        /// CADENA GEMELA DEL QUAD. Por SEGMENTO: las TRES texturas Taper
        /// RECORTADAS a la meseta (velo/cuerpo/núcleo — la MISMA anatomía y los
        /// MISMOS factores α que <see cref="Tear"/>) estiradas a
        /// (len+e₀+e₁, 1.60·wseg) — EL SOLAPE ADAPTATIVO: la luz aditiva se
        /// APILA en los solapes (2-3× en cada junta del solape completo len+w —
        /// las "cuentas brillantes" del mock), así que cada segmento solo se
        /// alarga lo que el GIRO REAL pide (w/2·tan(δ/2)+0.75 — ~1 px en la
        /// onda de 2°, el solape completo solo si el camino gira de verdad).
        /// LA PERLA DE LUZ solo existe donde hay giro real (δ > 8°); la PERLA
        /// DE PUNTA siempre (es la tapa redonda del extremo). EL LATIDO es
        /// suave a lo largo del arco (fase = fracción de arco·2.0 — el sin(k·1.7)
        /// por índice producía "perlas-oscuras" alternadas). La respiración
        /// vive en la ALPHA. NADA se omite (suelo 0.6px). SIN aberración R/B.
        /// </summary>
        /// <param name="progress">0..1 de la vida de la herida.</param>
        /// <param name="plano">True = ancho PLANO de punta a punta (meseta).</param>
        /// <param name="chispas">True = dibuja las chispas de anomalía.</param>
        /// <param name="ecoOffset">Offset del eco glitch.</param>
        /// <param name="ecoTint">Tinte del eco (null = sin eco).</param>
        /// <param name="estrellas">True = dibuja las estrellas fijas del interior.</param>
        public static void Grieta(SpriteBatch batch, Vector2[] camino, float progress,
            float maxWidth, Color[] paleta, float intensity, int seed, float time,
            bool plano = false, bool chispas = true, Vector2 ecoOffset = default,
            Color? ecoTint = null, bool estrellas = true)
        {
            if (batch == null || camino == null || camino.Length < 2 || paleta == null || paleta.Length == 0) return;
            intensity = MathHelper.Clamp(intensity, 0f, 1f);
            if (intensity <= 0.02f) return;

            float vida = MathF.Pow(1f - MathHelper.Clamp(progress, 0f, 1f), 0.8f);
            float respira = 1f + 0.08f * MathF.Sin(time * 2.2f + seed * 0.13f);

            float total = LongitudCamino(camino);
            if (total < 8f) return;
            float[] ws = AnchosCamino(camino, maxWidth, plano);

            Color velo = Pal(paleta, 0);
            Color cuerpo = Pal(paleta, 1);
            Color nucleo = Pal(paleta, paleta.Length - 1);

            float arc = 0f;
            for (int i = 0; i < camino.Length - 1; i++)
            {
                Vector2 a = camino[i];
                Vector2 b = camino[i + 1];
                float len = Vector2.Distance(a, b);
                if (len < 0.30f) continue;
                arc += len;

                // LA ANCHURA EN LOS VÉRTICES COMPARTIDOS — con SUELO: ningún
                // segmento se omite jamás (v6.30: los `continue` por anchura
                // eran huecos REALES en la cola fina del taper).
                float wa = MathF.Max(ws[i], 0.6f);
                float wb = MathF.Max(ws[i + 1], 0.6f);
                float wseg = (wa + wb) * 0.5f;
                float wmax = MathF.Max(wa, wb);

                Vector2 mid = (a + b) * 0.5f;
                Vector2 delta = b - a;
                float rot = MathF.Atan2(delta.Y, delta.X);
                // EL LATIDO SUAVE (v6.31): la fase es la FRACCIÓN DE ARCO — el
                // MISMO latido del quad (7.3 Hz) desplazado suavemente a lo largo.
                float arcFrac = arc / total;
                float beat = 0.90f + 0.10f * MathF.Sin(time * 7.3f + arcFrac * 2.0f + seed);
                float h = QuadAlto * wseg;

                // EL SOLAPE ADAPTATIVO: el giro real en cada vértice del segmento.
                // La LUZ usa suelo 0 (el aditivo se APILA: su solape es EXACTAMENTE
                // el geométricamente necesario — w/2·tan(δ/2), que cierra la esquina
                // del LABIO; el velo exterior puede quedar 0.03 px corto — sub-píxel
                // invisible; el mock medía +2 px de "cuentas" con suelo 0.35).
                float giroA = i > 0 ? GiroEn(camino, i) : 0f;
                float giroB = i < camino.Length - 2 ? GiroEn(camino, i + 1) : 0f;
                float largoLuz = len + ExtensionSolape(wmax, giroA, 0f) + ExtensionSolape(wmax, giroB, 0f);

                // LOS SEGMENTOS EXTREMOS llevan la textura ESTADIO COMPLETA (la
                // tapa redonda del arranque/la punta — igual que el quad).
                bool extremo = i == 0 || i == camino.Length - 2;

                // === EL VELO (α 0.30 — el halo que integra) ===
                TaperQuad(batch, TaperVeloTex, mid + ecoOffset, largoLuz, h, rot,
                    Eco(Tint(velo, 0.30f * intensity * vida * beat * respira), ecoTint), extremo);

                // === EL CUERPO (los labios en el borde del vacío, α 0.60) ===
                TaperQuad(batch, TaperCuerpoTex, mid + ecoOffset, largoLuz, h, rot,
                    Eco(Tint(cuerpo, 0.60f * intensity * vida), ecoTint), extremo);

                // === EL NÚCLEO RAZOR (los filos + el centro cegador, α 0.90) ===
                TaperQuad(batch, TaperNucleoTex, mid + ecoOffset, largoLuz, h, rot,
                    Eco(Tint(nucleo, 0.90f * intensity * vida), ecoTint), extremo);

                // === LA PERLA DE LUZ solo donde HAY GIRO REAL (δ > 8°): en los
                //     tramos casi rectos la perla aditiva era una cuenta brillante;
                //     en los giros de verdad es el round-join que cierra la esquina.
                if (giroA > 0.14f)
                {
                    TaperQuad(batch, TaperVeloTex, a + ecoOffset, wmax * 1.25f, h, rot,
                        Eco(Tint(velo, 0.30f * intensity * vida * beat * respira), ecoTint));
                    TaperQuad(batch, TaperCuerpoTex, a + ecoOffset, wmax * 1.25f, h, rot,
                        Eco(Tint(cuerpo, 0.60f * intensity * vida), ecoTint));
                    TaperQuad(batch, TaperNucleoTex, a + ecoOffset, wmax * 1.25f, h, rot,
                        Eco(Tint(nucleo, 0.90f * intensity * vida), ecoTint));
                }
            }

            // (LA PUNTA no lleva perla: el ÚLTIMO segmento dibuja la textura
            //  ESTADIO COMPLETA — su tapa redonda ES la punta de la herida.)

            // === LAS ESTRELLAS FIJAS de la herida (sin scroll, con paralaje) ===
            if (estrellas)
                EstrellasCamino(batch, camino, maxWidth, paleta, intensity * vida, seed, time, 14 + seed % 7);

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

        /// <summary>El GIRO (radianes, 0..π) en el vértice k del camino: el ángulo
        /// entre el segmento entrante y el saliente.</summary>
        private static float GiroEn(Vector2[] camino, int k)
        {
            Vector2 d0 = camino[k] - camino[k - 1];
            Vector2 d1 = camino[k + 1] - camino[k];
            if (d0.LengthSquared() < 0.0001f || d1.LengthSquared() < 0.0001f) return 0f;
            float a0 = MathF.Atan2(d0.Y, d0.X);
            float a1 = MathF.Atan2(d1.Y, d1.X);
            float d = MathF.Abs(a1 - a0);
            if (d > MathHelper.Pi) d = MathHelper.TwoPi - d;
            return d;
        }

        /// <summary>
        /// LA EXTENSIÓN DE SOLAPE por vértice (v6.31, la geometría del round-join):
        /// dos bandas de ancho w que giran δ necesitan e = w/2·tan(δ/2) para que
        /// sus esquinas exteriores se crucen (+margen de antialias). Suelo 0.75 px
        /// para el VACÍO (apilamiento idempotente — cobertura gratis) y 0 para la
        /// LUZ (el aditivo se APILA: su solape es exactamente la necesidad
        /// geométrica — cero en los tramos rectos, el completo en giros de 90°+).
        /// </summary>
        private static float ExtensionSolape(float w, float giro, float suelo = 0.75f)
            => MathHelper.Clamp(w * 0.5f * MathF.Tan(giro * 0.5f) + suelo, suelo, w * 0.5f);



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
        /// LOS ANCHOS DEL CAMINO (una pasada): la anchura en cada VÉRTICE.
        /// v6.31 — DOS MODOS: <paramref name="plano"/> = TRUE devuelve el ancho
        /// CONSTANTE de punta a punta (LA MESETA de la cadena — el modo del
        /// desgarro vibrante: una sola línea PAREJA); FALSE conserva el taper
        /// raíz→punta de v6.30 (exponente 0.45 + suelo 25%) para los demás
        /// llamadores. La lección WidthFunction se mantiene: la
        /// anchura vive en los VÉRTICES compartidos, nunca en centros.
        /// </summary>
        public static float[] AnchosCamino(Vector2[] camino, float maxWidth, bool plano = false)
        {
            if (camino == null || camino.Length < 2) return Array.Empty<float>();
            var ws = new float[camino.Length];
            if (plano)
            {
                for (int i = 0; i < ws.Length; i++) ws[i] = maxWidth;
                return ws;
            }
            float suelo = maxWidth * 0.25f;
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
                // TAPER SUAVE: gorda en la raíz, punta viva al 25% (v6.30).
                ws[i] = MathF.Max(maxWidth * MathF.Pow(1f - t, 0.45f), suelo);
            }
            return ws;
        }

        // ==================================================================
        //  LAS PIEZAS SUELTAS (los quiere todo el mundo)
        // ==================================================================

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

        /// <summary>
        /// EL QUAD DE CADENA (v6.31): la textura Taper RECORTADA a la meseta
        /// (u∈[0.30,0.70] — sin estadio) estirada a (len, alto) px. Es LA GEMELA
        /// del quad recto para los segmentos que GIRAN: misma anatomía
        /// vertical, misma banda de vacío, mismos labios — cero salto visual.
        /// Con <paramref name="estadio"/> = true dibuja la textura COMPLETA
        /// (la tapa redonda del estadio vive en sus extremos): es el modo de los
        /// segmentos EXTREMOS de la cadena — el arranque y la punta redondean
        /// igual que el quad, sin perlas que sobresalgan fuera del camino.
        /// </summary>
        private static void TaperQuad(SpriteBatch batch, Texture2D tex, Vector2 pos,
            float len, float alto, float rot, Color tint, bool estadio = false)
        {
            if (tex == null || tint.A == 0 || len < 0.1f || alto < 0.1f) return;
            // El recorte de la MESETA: u∈[0.30,0.70] está en el cuerpo plano
            // del estadio (ancho 100%) — la cadena no hereda las tapas.
            Rectangle src = estadio
                ? new Rectangle(0, 0, tex.Width, tex.Height)
                : new Rectangle(tex.Width * 3 / 10, 0, tex.Width * 2 / 5, tex.Height);
            batch.Draw(tex, pos, src, tint, rot,
                new Vector2(src.Width, src.Height) * 0.5f,
                new Vector2(len, alto) / new Vector2(src.Width, src.Height),
                SpriteEffects.None, 0f);
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

                // LA LATERAL: dentro de la banda del vacío (v6.31 — la herida
                // vibrante es PAREJA: las estrellas viven en el MISMO ancho de
                // punta a punta, igual que Estrellas: 0.31·W con margen 0.85).
                float wLocal = maxWidth * 0.31f;
                float y = (h2 - 0.5f) * 2f * wLocal * 0.85f;
                float tw = 0.45f + 0.55f * MathF.Sin(tick * (0.15f + 0.2f * h3) + h3 * 6.28f + j * 1.9f);
                if (tw <= 0.08f) continue;
                float s = 1f + 1.6f * h3;

                Quad(batch, OrbTex, p + perp * y, new Vector2(s, s), 0f,
                    Tint(baseC, MathHelper.Clamp(tw * intensity, 0f, 1f)));
            }
        }

        // ==================================================================
        //  v6.33 — LA FAMILIA DE LOS PORTALES (los 4 desgarros nuevos nacen
        //  de las referencias del usuario analizadas con VLM: research/v633/
        //  refs + vlm_ref1/2.json). CONTRATO: reciben el sprite batch CERRADO
        //  y lo dejan CERRADO (contrato v6.10) — gestionan sus lotes alfa y
        //  aditivo internamente. Coordenadas de PANTALLA (resta screenPosition
        //  antes de llamar).
        // ==================================================================

        private static Asset<Texture2D> _disk;

        /// <summary>El disco negro sólido (el núcleo del pliegue).</summary>
        private static Texture2D DiskTex =>
            (_disk ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/BlackDisk")).Value;

        /// <summary>Abre/cierra el lote ADITIVO de la casa.</summary>
        private static void PortalAdditive()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>Abre/cierra el lote ALFA de la casa.</summary>
        private static void PortalAlpha()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
        }

        // ------------------------------------------------------------------
        //  1 — EL PORTAL DIMENSIONAL (la referencia: anillos concéntricos
        //  perfectos en túnel, gradiente cian→magenta, glifos, núcleo blanco
        //  que respira, sparkles orbitando, polvo en espiral)
        // ------------------------------------------------------------------

        /// <summary>
        /// EL PORTAL DE ANILLOS — "Se abre portal dimensional": 6 anillos
        /// concéntricos con ROTACIÓN JERÁRQUICA (cada uno gira a su velocidad,
        /// sentidos alternos — el parallax del túnel), gradiente cian→magenta
        /// de fuera a dentro, GLIFOS (marcas runicas) a lo largo de cada
        /// anillo, el NÚCLEO BLANCO que respira a 0.3 Hz, sparkles de 4
        /// puntas orbitando y polvo fluyendo en ESPIRAL hacia dentro.
        /// `progress` 0→1 = la apertura (los anillos nacen del centro).
        /// </summary>
        public static void PortalAnillos(Vector2 center, float radius, float progress,
            float time, int seed, float alpha = 1f)
        {
            if (radius < 4f || alpha <= 0.02f) return;
            progress = MathHelper.Clamp(progress, 0f, 1f);
            try
            {
                Color cian = new(0, 176, 255);        // #00B0FF
                Color magenta = new(213, 0, 249);     // #D500F9
                Color blanco = new(255, 255, 255);

                // El breathing del conjunto (0.3 Hz — el portal "late").
                float breathe = 1f + 0.05f * MathF.Sin(time * 0.3f * MathHelper.TwoPi + seed);
                float open = (float)Math.Pow(progress, 0.7f);   // ease-out de apertura

                PortalAdditive();
                var batch = Main.spriteBatch;

                // === 1. LOS 6 ANILLOS CONCÉNTRICOS (el túnel) ===
                const int Anillos = 6;
                for (int k = 0; k < Anillos; k++)
                {
                    float fk = k / (float)(Anillos - 1);         // 0=interior … 1=exterior
                    // Los anillos exteriores nacen PRIMERO (la apertura
                    // empuja hacia afuera) — cada uno abre con su retraso.
                    float anilloOpen = MathHelper.Clamp(progress * (1.6f + k * 0.35f) - k * 0.35f, 0f, 1f);
                    float r = radius * (0.22f + 0.78f * fk) * anilloOpen * breathe;
                    if (r < 2f) continue;

                    // El gradiente del túnel: cian fuera, magenta dentro
                    // (v6.33 b: el magenta interior MÁS PRESENTE — la lección
                    // del mock: a 0.28 se perdía contra el cielo claro).
                    Color anillo = Color.Lerp(magenta, cian, fk);
                    // Los interiores son MÁS definidos (el fondo del túnel).
                    float grosor = MathHelper.Lerp(3.2f, 6.5f, 1f - fk);
                    float alfa = (0.40f + 0.50f * (1f - fk)) * alpha * anilloOpen;

                    // LA ROTACIÓN JERÁRQUICA: sentidos alternos, el interior
                    // gira más rápido (la entrada gira con el otro lado).
                    float spin = time * (0.35f + 0.55f * (1f - fk)) * (k % 2 == 0 ? 1f : -1f);
                    float rot = spin + seed;

                    // El anillo (dibujado como arco completo con la textura Ring).
                    Quad(batch, RingTex, center, new Vector2(r * 2.15f, r * 2.15f), rot,
                        Tint(anillo, alfa * 0.8f));
                    Quad(batch, RingTex, center, new Vector2(r * 2.0f, r * 2.0f), rot,
                        Tint(anillo, alfa));

                    // === LOS GLIFOS (las marcas del círculo — 10-14 por anillo) ===
                    int glifos = 8 + k * 2;
                    for (int g = 0; g < glifos; g++)
                    {
                        float h = H01(seed, k * 31 + g, 71);
                        if (h < 0.45f) continue;             // no todos los sitios
                        float ang = spin * (1f + 0.15f * fk) + g * MathHelper.TwoPi / glifos;
                        Vector2 gp = center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * r;
                        float gs = grosor * (1.2f + 1.4f * H01(seed, k * 17 + g, 73));
                        // El glifo: una marca alargada RADIAL (como runa del círculo).
                        Quad(batch, GlowTex, gp, new Vector2(gs * 0.5f, gs * 2.4f),
                            ang + MathHelper.PiOver2,
                            Tint(Color.Lerp(anillo, blanco, 0.35f), alfa * 0.85f));
                    }
                }

                // === 2. EL POLVO EN ESPIRAL (fluye hacia dentro) ===
                const int Polvo = 14;
                for (int d = 0; d < Polvo; d++)
                {
                    float h1 = H01(seed, d, 79);
                    float h2 = H01(seed, d, 83);
                    // Cada partícula espiralea hacia el centro (la succión del túnel).
                    float t = (time * (0.25f + 0.3f * h2) + h1) % 1f;      // 1=borde, 0=centro
                    float rr = radius * 1.05f * t * open;
                    float ang = h2 * MathHelper.TwoPi + time * (1.8f + h1 * 1.2f) * (h1 > 0.5f ? 1f : -1f);
                    Vector2 pp = center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * rr;
                    float s = 3f + 5f * h2;
                    float a = 0.55f * alpha * open * (0.3f + 0.7f * MathF.Sin(t * MathF.PI));
                    Quad(batch, OrbTex, pp, new Vector2(s, s), 0f,
                        Tint(Color.Lerp(magenta, cian, h1), a));
                }

                // === 3. LOS SPARKLES ORBITANDO (estrellas de 4 puntas) ===
                const int Sparkles = 7;
                for (int s = 0; s < Sparkles; s++)
                {
                    float h1 = H01(seed, s, 89);
                    float h2 = H01(seed, s, 97);
                    float rr = radius * (0.55f + 0.5f * h1) * open;
                    float ang = time * (0.6f + 0.5f * h2) * (s % 2 == 0 ? 1f : -1f)
                                + h2 * MathHelper.TwoPi;
                    Vector2 sp = center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * rr;
                    float tw = 0.4f + 0.6f * MathF.Abs(MathF.Sin(time * 3f + s * 2.1f));
                    float ss = (5f + 7f * h1) * tw;
                    StarQuad(batch, sp, ang, ss, ss * 0.30f,
                        Tint(blanco, 0.65f * alpha * open * tw));
                }

                // === 4. EL NÚCLEO BLANCO (la otra dimensión — respira) ===
                float heart = 0.5f + 0.5f * MathF.Sin(time * 0.3f * MathHelper.TwoPi + 1.7f);
                float nr = radius * 0.20f * open * (1f + 0.10f * heart);
                // v6.33 b: el VELO MAGENTA del fondo del túnel (la profundidad
                // que el mock mostró faltante — el interior arde en magenta).
                Quad(batch, GlowTex, center, new Vector2(radius * 1.05f, radius * 1.05f), 0f,
                    Tint(magenta, 0.16f * alpha * open));
                Quad(batch, GlowTex, center, new Vector2(nr * 3.4f, nr * 3.4f), 0f,
                    Tint(cian, 0.35f * alpha * open));
                Quad(batch, GlowTex, center, new Vector2(nr * 2.0f, nr * 2.0f), 0f,
                    Tint(magenta, 0.50f * alpha * open));
                Quad(batch, GlowTex, center, new Vector2(nr * 1.15f, nr * 1.15f), 0f,
                    Tint(blanco, (0.75f + 0.25f * heart) * alpha * open));
                Main.spriteBatch.End();
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }
        }

        // ------------------------------------------------------------------
        //  2 — EL PLIEGUE DEL ESPACIO (la referencia: doble elipse diagonal
        //  tipo ojo, rim cian + interior ámbar, NÚCLEO NEGRO, rejilla que
        //  converge, succión de partículas)
        // ------------------------------------------------------------------

        /// <summary>
        /// EL OJO ESPACIAL — "Apertura de portal dimensional": la doble
        /// elipse diagonal (el ojo del pliegue) con el borde RIM cian fino y
        /// el resplandor interior ÁMBAR, el NÚCLEO NEGRO absoluto (pase alfa
        /// sólido — el vacío del otro lado) y LA REJILLA QUE CONVERGE (grid
        //  warping: líneas de fuga que se curvan hacia el centro — la señal
        //  visual #1 de "espacio-tiempo doblado"), con partículas succionadas
        /// hacia dentro. `progress` 0→1 = la apertura del ojo.
        /// </summary>
        public static void OjoEspacial(Vector2 center, float radius, float progress,
            float time, int seed, float alpha = 1f, float tilt = -0.55f)
        {
            if (radius < 4f || alpha <= 0.02f) return;
            progress = MathHelper.Clamp(progress, 0f, 1f);
            try
            {
                Color cian = new(0, 212, 255);        // #00D4FF
                Color ambar = new(255, 184, 0);       // #FFB800
                Color blanco = new(255, 250, 205);

                float open = (float)Math.Pow(progress, 0.6f);
                float breathe = 1f + 0.04f * MathF.Sin(time * 0.8f * MathHelper.TwoPi + seed);
                float R = radius * open * breathe;
                if (R < 2f) return;

                // === PASO 1 — EL NÚCLEO NEGRO (pase ALFA: el vacío sólido) ===
                PortalAlpha();
                var batch = Main.spriteBatch;
                // La doble elipse: dos discos solapados en diagonal (el ojo).
                Vector2 e1 = center + new Vector2(MathF.Cos(tilt), MathF.Sin(tilt)) * R * 0.34f;
                Vector2 e2 = center - new Vector2(MathF.Cos(tilt), MathF.Sin(tilt)) * R * 0.34f;
                float diskR = R * 0.78f;
                Quad(batch, DiskTex, e1, new Vector2(diskR * 2f, diskR * 1.15f), tilt + MathHelper.PiOver2,
                    new Color(8, 8, 14, (byte)(int)(235 * alpha * open)));
                Quad(batch, DiskTex, e2, new Vector2(diskR * 2f, diskR * 1.15f), tilt + MathHelper.PiOver2,
                    new Color(8, 8, 14, (byte)(int)(235 * alpha * open)));
                Main.spriteBatch.End();

                // === PASO 2 — LA REJILLA QUE CONVERGE (aditivo, dentro) ===
                PortalAdditive();
                batch = Main.spriteBatch;
                const int Rayos = 9;
                for (int g = 0; g < Rayos; g++)
                {
                    float ang = g * MathHelper.TwoPi / Rayos + tilt * 0.5f;
                    // La línea de fuga: del borde hacia el centro, curvándose
                    // (el warp: el punto medio se desvía TANGENCIALMENTE).
                    Vector2 borde = center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * R * 0.95f;
                    Vector2 tang = new Vector2(-MathF.Sin(ang), MathF.Cos(ang));
                    float warp = 0.35f * MathF.Sin(time * 0.9f + g * 1.3f) * R;
                    Vector2 mid = Vector2.Lerp(borde, center, 0.5f) + tang * warp;
                    float len1 = Vector2.Distance(borde, mid);
                    if (len1 > 3f)
                        Quad(batch, GlowTex, Vector2.Lerp(borde, mid, 0.5f),
                            new Vector2(len1, 2.2f), (float)Math.Atan2(mid.Y - borde.Y, mid.X - borde.X),
                            Tint(cian, 0.45f * alpha * open));
                    float len2 = Vector2.Distance(mid, center);
                    if (len2 > 3f)
                        Quad(batch, GlowTex, Vector2.Lerp(mid, center, 0.5f),
                            new Vector2(len2, 1.6f), (float)Math.Atan2(center.Y - mid.Y, center.X - mid.X),
                            Tint(ambar, 0.35f * alpha * open));
                }
                // Los anillos de la rejilla (concéntricos, deformados).
                for (int c = 0; c < 3; c++)
                {
                    float rr = R * (0.30f + 0.28f * c);
                    float squish = 0.62f + 0.06f * MathF.Sin(time * 0.7f + c);
                    Quad(batch, RingTex, center, new Vector2(rr * 2.1f, rr * 2.1f * squish),
                        tilt + MathHelper.PiOver2, Tint(cian, 0.20f * alpha * open));
                }

                // === PASO 3 — EL RIM DEL BORDE (cian fino ×2 elipses + ámbar) ===
                for (int e = 0; e < 2; e++)
                {
                    Vector2 ec = e == 0 ? e1 : e2;
                    // El halo ámbar interior (el horizonte caliente).
                    Quad(batch, GlowTex, ec, new Vector2(diskR * 2.30f, diskR * 1.32f),
                        tilt + MathHelper.PiOver2, Tint(ambar, 0.16f * alpha * open));
                    // El rim cian: la línea fina y nítida del borde (Ring estirado).
                    Quad(batch, RingTex, ec, new Vector2(diskR * 2.24f, diskR * 1.26f),
                        tilt + MathHelper.PiOver2, Tint(cian, 0.55f * alpha * open));
                    Quad(batch, RingTex, ec, new Vector2(diskR * 2.10f, diskR * 1.18f),
                        tilt + MathHelper.PiOver2, Tint(cian, 0.35f * alpha * open));
                }
                // El CUELLO del ojo: el punto más brillante (donde se tocan).
                Quad(batch, GlowTex, center, new Vector2(R * 0.55f, R * 0.55f), 0f,
                    Tint(blanco, 0.50f * alpha * open));
                Quad(batch, GlowTex, center, new Vector2(R * 0.22f, R * 0.22f), 0f,
                    Tint(blanco, 0.85f * alpha * open));

                // === PASO 4 — LA SUCCIÓN (partículas cayendo al vacío) ===
                const int Suck = 10;
                for (int d = 0; d < Suck; d++)
                {
                    float h1 = H01(seed, d, 101);
                    float h2 = H01(seed, d, 103);
                    float t = 1f - ((time * (0.30f + 0.35f * h2) + h1) % 1f);   // 1=fuera→0=centro
                    float ang = h2 * MathHelper.TwoPi + time * 0.8f * (h1 - 0.5f) * 2f;
                    Vector2 pp = center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * R * 1.1f * t;
                    float s = 2.5f + 3.5f * h2;
                    Quad(batch, OrbTex, pp, new Vector2(s, s * (1f + t * 0.8f)), ang,
                        Tint(Color.Lerp(ambar, cian, t), 0.50f * alpha * open * t));
                }
                Main.spriteBatch.End();
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }
        }

        // ------------------------------------------------------------------
        //  3 — EL DESGARRO CUÁNTICO (la referencia: círculo fragmentado con
        //  borde GLITCH de bloques, núcleo magenta nebuloso, rayos que
        //  irradian, flicker rápido, partículas de datos)
        // ------------------------------------------------------------------

        /// <summary>
        /// EL DESGARRO GLITCH — "Desgarro de realidad cuántica": el círculo
        /// IRREGULAR fragmentado (24 vértices con radios hash — la silueta
        /// rasgada) con el borde de BLOQUES GLITCH (astillas rectangulares
        /// cian/violeta proyectadas hacia afuera, cada una con su flicker
        /// rápido 0.1-0.3 s), el NÚCLEO MAGENTA nebuloso con vetas fucsia,
        /// RAYOS que irradian desde el borde (StormLib) y partículas de
        /// datos (cuadraditos que se desprenden rotando). `progress` 0→1.
        /// </summary>
        public static void DesgarroGlitch(Vector2 center, float radius, float progress,
            float time, int seed, float alpha = 1f)
        {
            if (radius < 4f || alpha <= 0.02f) return;
            progress = MathHelper.Clamp(progress, 0f, 1f);
            try
            {
                Color cian = new(0, 229, 255);        // #00E5FF
                Color violeta = new(124, 77, 255);    // #7C4DFF
                Color magenta = new(213, 0, 249);     // #D500F9
                Color fucsia = new(255, 64, 129);     // #FF4081
                Color blanco = new(255, 255, 255);

                float open = (float)Math.Pow(progress, 0.55f);
                float R = radius * open;
                const int Verts = 24;
                float tick = time * 60f;

                // === PASO 1 — EL NÚCLEO (pase alfa: la nebulosa SÓLIDA) ===
                PortalAlpha();
                var batch = Main.spriteBatch;
                // La silueta irregular: el polígono rasgado (8 rebanadas de pastel).
                for (int s = 0; s < 8; s++)
                {
                    float a0 = s * MathHelper.PiOver4 + 0.09f * MathF.Sin(time * 1.1f + s);
                    float a1 = (s + 1) * MathHelper.PiOver4 - 0.09f * MathF.Sin(time * 0.9f + s * 2f);
                    float r0 = R * (0.62f + 0.30f * H01(seed, s, 107));
                    float r1 = R * (0.62f + 0.30f * H01(seed, s + 1, 109));
                    // El quad de la rebanada (del centro al arco).
                    Vector2 p0 = center + new Vector2(MathF.Cos(a0), MathF.Sin(a0)) * r0;
                    Vector2 p1 = center + new Vector2(MathF.Cos(a1), MathF.Sin(a1)) * r1;
                    Vector2 pm = center + new Vector2(MathF.Cos((a0 + a1) * 0.5f), MathF.Sin((a0 + a1) * 0.5f)) * Math.Max(r0, r1);
                    float wSeg = Vector2.Distance(p0, p1) + 6f;
                    float hSeg = Math.Max(r0, r1);
                    Quad(batch, GlowTex, (p0 + p1) * 0.5f + (pm - center) * 0.25f,
                        new Vector2(wSeg, hSeg), (a0 + a1) * 0.5f + MathHelper.PiOver2,
                        new Color(magenta.R, magenta.G, magenta.B, (byte)(int)(120 * alpha * open)));
                }
                Main.spriteBatch.End();

                // === PASO 2 — EL BORDE GLITCH + LA LUZ (aditivo) ===
                PortalAdditive();
                batch = Main.spriteBatch;
                for (int v = 0; v < Verts; v++)
                {
                    float h1 = H01(seed, v, 113);
                    float h2 = H01(seed, v, 127);
                    float h3 = H01(seed, v, 131);
                    float ang = v * MathHelper.TwoPi / Verts;
                    float rv = R * (0.70f + 0.34f * h1);             // el borde dentado

                    // El FLICKER rápido (0.1-0.3 s por astilla — inestable).
                    float flickHz = 6f + 14f * h2;
                    float lit = MathF.Sin(tick * flickHz * 0.1047f + h3 * 6.28f) * 0.5f + 0.5f;
                    if (lit < 0.25f) continue;

                    Vector2 dir = new(MathF.Cos(ang), MathF.Sin(ang));
                    Vector2 bord = center + dir * rv;

                    // LA ASTILLA GLITCH: un bloque rectangular proyectado hacia
                    // afuera (los artefactos de compresión de la realidad rota).
                    float bw = 3f + 11f * h2;
                    float bh = 2f + 5f * h3;
                    float bo = rv + (6f + 16f * h3) * lit;
                    Color astC = h1 > 0.5f ? cian : violeta;
                    Quad(batch, GlowTex, center + dir * bo,
                        new Vector2(bw, bh), ang,
                        Tint(astC, (0.45f + 0.55f * lit) * alpha * open));

                    // LA LÍNEA DEL BORDE (el contorno irregular que sangra luz).
                    Quad(batch, GlowTex, bord, new Vector2(R * 0.30f, 2.2f + 2.5f * lit),
                        ang + MathHelper.PiOver2, Tint(astC, 0.60f * alpha * open * lit));

                    // LA VETA FUCSIA (el relámpago interno de la nebulosa).
                    if (v % 3 == 0)
                        Quad(batch, GlowTex, center + dir * (rv * 0.55f),
                            new Vector2(rv * 0.62f, 1.8f), ang,
                            Tint(fucsia, 0.42f * alpha * open * lit));

                    // EL RAYO QUE IRRADIA (el código escapando — StormLib).
                    if (v % 4 == 0 && lit > 0.7f)
                    {
                        Vector2 ext = center + dir * (rv + 26f + 34f * h2);
                        StormLib.Bolt(batch, bord, ext, seed + v * 7, (int)(tick * 0.35f),
                            4.5f, astC, blanco, 0.55f * alpha * open * lit, 4, 9f);
                    }
                }

                // EL NÚCLEO CEGADOR (el corazón de la corrupción) — v6.33 b:
                // el corazón MAGENTA pesa más (la lección del mock: el blanco
                // puro se comía la lectura cuántica).
                float heart = 0.5f + 0.5f * MathF.Sin(time * 9f + seed);
                Quad(batch, GlowTex, center, new Vector2(R * 0.95f, R * 0.95f), 0f,
                    Tint(magenta, 0.34f * alpha * open));
                Quad(batch, GlowTex, center, new Vector2(R * 0.52f, R * 0.52f), 0f,
                    Tint(violeta, 0.42f * alpha * open));
                Quad(batch, GlowTex, center, new Vector2(R * 0.30f, R * 0.30f) * (1f + 0.12f * heart),
                    0f, Tint(blanco, (0.50f + 0.30f * heart) * alpha * open));

                // LAS PARTÍCULAS DE DATOS (cuadraditos que se desprenden).
                for (int d = 0; d < 9; d++)
                {
                    float h1 = H01(seed, d, 137);
                    float h2 = H01(seed, d, 139);
                    float ang = h1 * MathHelper.TwoPi + time * (0.4f + 0.3f * h2);
                    float t = ((time * (0.22f + 0.2f * h2) + h2) % 1f);
                    float rr = R * (0.75f + 0.7f * t);
                    Vector2 pp = center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * rr;
                    float s = 2f + 4f * h2;
                    Quad(batch, GlowTex, pp, new Vector2(s, s * (0.4f + 0.6f * h1)),
                        ang + time * 1.7f * (h1 - 0.5f) * 2f,
                        Tint(h1 > 0.5f ? cian : fucsia, 0.55f * alpha * open * (1f - t)));
                }
                Main.spriteBatch.End();
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }
        }

        // ------------------------------------------------------------------
        //  4 — LA HERIDA ELÉCTRICA (la referencia: grieta lineal dentada con
        //  borde blanco→cian→violeta, interior de estática con scanlines,
        //  arcos voltaicos internos, strobe 10-30 Hz, chispas zig-zag)
        // ------------------------------------------------------------------

        /// <summary>
        /// LA HERIDA ELÉCTRICA — "Desgarro de realidad eléctrica": LA GRIETA
        /// (quad del desgarro de la casa con paleta eléctrica) cuyo interior
        /// lleva ESTÁTICA (scanlines horizontales parpadeando dentro del
        /// vacío) y ARCOS VOLTAICOS (StormLib.StormArc — la corriente que
        /// corre POR DENTRO de la herida), con STROBE nervioso, chispas en
        /// los extremos y ABERRACIÓN CROMÁTICA sutil (el filo rojo/cian
        /// partido). `progress` 0→1 = la herida abriéndose.
        /// </summary>
        public static void HeridaElectrica(Vector2 origin, Vector2 dir,
            float length, float maxWidth, float progress, float time, int seed,
            float alpha = 1f)
        {
            if (length < 8f || maxWidth < 1f || alpha <= 0.02f) return;
            dir = Vector2.Normalize(dir);
            progress = MathHelper.Clamp(progress, 0f, 1f);
            float tick = time * 60f;

            Color cian = new(0, 255, 255);          // #00FFFF
            Color violeta = new(138, 43, 226);      // #8A2BE2
            Color blancoHielo = new(224, 255, 255); // #E0FFFF
            Color rojoCA = new(255, 40, 40);        // aberración

            float open = (float)Math.Pow(progress, 0.6f);
            float len = length * open;
            float w = maxWidth * open;

            // === 1. EL VACÍO DE LA HERIDA (pase alfa — el negro con ESTÁTICA) ===
            PortalAlpha();
            var b = Main.spriteBatch;
            // El cuerpo negro (la banda del vacío — 0.62 del quad como el desgarro).
            Quad(b, TaperVoidTex, origin + dir * (len * 0.5f),
                new Vector2(len, w * 1.30f), (float)Math.Atan2(dir.Y, dir.X),
                new Color(5, 5, 16, (byte)(int)(240 * alpha * open)));
            // LAS SCANLINES (la estática interior — el buffer detrás de la realidad).
            int lineas = Math.Max(3, (int)(len / 22f));
            for (int l = 0; l < lineas; l++)
            {
                float h = H01(seed, l, 149);
                float h2 = H01(seed, l, 151);
                float lx = len * (0.08f + 0.84f * h);
                // Cada scanline parpadea a su frecuencia (10-30 Hz — el strobe).
                float st = MathF.Sin(tick * (0.17f + 0.34f * h2) + h * 6.28f) * 0.5f + 0.5f;
                if (st < 0.35f) continue;
                Quad(b, GlowTex, origin + dir * lx,
                    new Vector2(len * (0.10f + 0.16f * h2), 1.3f + 1.5f * st),
                    (float)Math.Atan2(dir.Y, dir.X) + MathHelper.PiOver2 * 0f,
                    Tint(blancoHielo, 0.10f * st * alpha * open));
            }
            Main.spriteBatch.End();

            // === 2. LA LUZ (aditivo — batch del llamador reabierto) ===
            PortalAdditive();
            b = Main.spriteBatch;
            float rot = (float)Math.Atan2(dir.Y, dir.X);
            Vector2 mid = origin + dir * (len * 0.5f);

            // LA ABERRACIÓN CROMÁTICA: el filo dibujado DOS VECES desplazado
            // en perpendicular (rojo a un lado, cian al otro — la fractura óptica).
            Vector2 perp = new(-dir.Y, dir.X);
            Vector2 abOff = perp * (1.6f + 1.2f * MathF.Sin(time * 13f));
            Quad(b, TaperVeloTex, mid + abOff, new Vector2(len, w * 1.15f), rot,
                Tint(rojoCA, 0.16f * alpha * open));
            Quad(b, TaperVeloTex, mid - abOff, new Vector2(len, w * 1.15f), rot,
                Tint(cian, 0.16f * alpha * open));

            // EL VELO violeta + EL CUERPO cian + EL NÚCLEO blanco (la herida) —
            // v6.33 b: MÁS GRUESA Y MÁS BRILLANTE (la lección del mock: una
            // banda fina oscura era INVISIBLE contra el cielo claro).
            Quad(b, TaperVeloTex, mid, new Vector2(len, w * 1.85f), rot,
                Tint(violeta, 0.55f * alpha * open));
            Quad(b, TaperVeloTex, mid, new Vector2(len, w * 1.45f), rot,
                Tint(cian, 0.30f * alpha * open));
            Quad(b, TaperCuerpoTex, mid, new Vector2(len, w * 1.00f), rot,
                Tint(cian, 0.85f * alpha * open));
            Quad(b, TaperNucleoTex, mid, new Vector2(len, w * 0.38f), rot,
                Tint(blancoHielo, 1f * alpha * open));

            // EL STROBE (10-30 Hz — el arco que falla): todo el cuerpo pica.
            float strobe = MathF.Sin(tick * 0.38f + seed) * 0.5f + 0.5f;
            if (strobe > 0.62f)
            {
                Quad(b, TaperVeloTex, mid, new Vector2(len, w * 2.3f), rot,
                    Tint(cian, 0.22f * alpha * open * strobe));
                Quad(b, TaperNucleoTex, mid, new Vector2(len, w * 0.55f), rot,
                    Tint(blancoHielo, 0.55f * alpha * open * strobe));
            }

            // === 3. LOS ARCOS VOLTAICOS INTERNOS (StormLib.StormArc ×3) ===
            int arcs = 3;
            for (int a = 0; a < arcs; a++)
            {
                float h = H01(seed, a, 157);
                float t0 = 0.12f + 0.24f * h + a * 0.22f;
                Vector2 a0 = origin + dir * (len * t0) + perp * (w * 0.22f * (h - 0.5f) * 2f);
                Vector2 a1 = origin + dir * (len * Math.Min(1f, t0 + 0.30f + 0.2f * h))
                             - perp * (w * 0.22f * (H01(seed, a, 163) - 0.5f) * 2f);
                StormLib.StormArc(b, a0, a1, seed + a * 31, time,
                    3.5f, cian, 0.65f * alpha * open);
            }

            // === 3b. v6.33 b — LAS RAMIFICACIONES (la lección del mock: la
            // herida recta leía "cuchillo"; la referencia manda: "grieta con
            // ramificaciones que se bifurcan como raíces o rayos"). 5 grietas
            // secundarias deterministas saliendo del cuerpo en diagonal. ===
            const int Ramas = 5;
            for (int r = 0; r < Ramas; r++)
            {
                float h1 = H01(seed, r, 173);
                float h2 = H01(seed, r, 179);
                float h3 = H01(seed, r, 181);
                if (h1 < 0.30f) continue;                    // no todas nacen
                float t0 = 0.12f + 0.76f * h2;
                Vector2 nace = origin + dir * (len * t0);
                float lado = h3 > 0.5f ? 1f : -1f;
                // La rama: 55°-75° del cuerpo (los rayos que brotan).
                float angRama = rot + lado * (0.96f + 0.35f * h1);
                float largoR = len * (0.10f + 0.13f * h2);
                Vector2 fin = nace + new Vector2(MathF.Cos(angRama), MathF.Sin(angRama)) * largoR;
                // LA GRIETA SECUNDARIA: fractal cian/violeta (StormLib con
                // semilla propia — el zig-zag de las ramas de un rayo).
                StormLib.Bolt(b, nace, fin, seed + 601 + r * 43, (int)(tick * 0.4f),
                    5.5f, violeta, blancoHielo, 0.75f * alpha * open, 5, largoR * 0.18f);
                StormLib.Bolt(b, nace, fin, seed + 601 + r * 43, (int)(tick * 0.4f),
                    2.5f, cian, blancoHielo, 0.85f * alpha * open, 5, largoR * 0.18f);
            }

            // === 3c. v6.33 b — CHISPAS A LO LARGO (no solo los extremos). ===
            int tramos = Math.Max(2, (int)(len / 110f));
            for (int s = 0; s <= tramos; s++)
            {
                float h = H01(seed, s, 191);
                if (h < 0.45f) continue;
                Vector2 sp = origin + dir * (len * s / (float)tramos)
                             + perp * (w * 0.4f * (H01(seed, s, 193) - 0.5f) * 2f);
                float st = ((tick + s * 9f) % 30f) / 30f;
                StormLib.SparkBurst(b, sp, cian, w * 0.75f, 3, seed + 21 + s,
                    st, w * 0.8f);
            }

            // === 4. LAS CHISPAS ZIG-ZAG en los extremos (StormLib.SparkBurst) ===
            float sparkT = (tick % 26f) / 26f;
            StormLib.SparkBurst(b, origin, cian, w * 1.3f, 5, seed + 5,
                sparkT, w * 1.2f);
            StormLib.SparkBurst(b, origin + dir * len, cian, w * 1.3f, 5, seed + 9,
                sparkT, w * 1.2f);
            Main.spriteBatch.End();
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
