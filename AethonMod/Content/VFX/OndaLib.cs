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
    /// <summary>Cómo muere el alpha de la onda a lo largo de su vida.</summary>
    public enum OndaFalloff
    {
        /// <summary>Lineal (impactos físicos).</summary>
        Linear,
        /// <summary>Cuadrático (energía que se disipa — el estándar).</summary>
        Quadratic,
        /// <summary>Lento (mágico: la onda se resiste a morir).</summary>
        Slow,
    }

    /// <summary>
    /// OndaLib — v6.25 — LA LIBRERÍA DE LAS ONDAS DE IMPACTO.
    ///
    /// Nació del análisis de huecos de v6.25 (research/humo_v625/
    /// ANALISIS_HUECOS.md): el ecosistema premium pone una onda expansiva
    /// en TODO golpe importante (los grandes mods en cada hit de boss;
    /// su ShockwaveShader de borde roto, SOTS con anillos por estilos) —
    /// nosotros solo teníamos el proyectil pesado de las muertes
    /// cósmicas. Esta librería es "la onda de trabajo" reutilizable.
    ///
    /// EL CONTRATO DE IMPACTO (qué hace que una onda se lea "de verdad"):
    ///   · UNA ONDA SON DOS ANILLOS: el frente fino-brillante + la
    ///     RETAGUARDIA gruesa-tenue (×0.5 alpha, +6% radio) — la "onda
    ///     de presión" detrás del frente de choque.
    ///   · EL BORDE ROTO: el frente se dibuja en 12-16 segmentos de arco
    ///     con radio vivo (±8% por hash) — nunca un círculo perfecto
    ///     (lección del ShockwaveShader del ecosistema).
    ///   · EL GROSOR NACE FINO Y ENGORDA mientras el alpha muere (la
    ///     onda se "gasta" ensanchándose: 4px → 14px en una de 300px).
    ///   · LA EXPANSIÓN FAST-OUT: r(t) = maxR·(1−(1−t)^2.2) — sale como
    ///     una explosión y frena (física leída por el ojo).
    ///   · EL PAQUETE: onda + sacudida de cámara (Kick) + destello
    ///     LOCAL del arma (Flash, v6.50.7: el gradiente nace en el centro
    ///     del proyectil y muere al alejarse — ya no es un velo de
    ///     pantalla completa) — TRES líneas en el call-site.
    ///
    /// CONTRATO DE LOTE (la casa): Shock/Pulse dibujan en el SpriteBatch
    /// ABIERTO que el llamador tenga (aditivo recomendado) y NO lo tocan.
    /// `progress` lo lleva el llamador (0..1 — típicamente 1 − timeLeft/
    /// maxTimeLeft): la librería NO guarda estado de ondas. TODO
    /// determinista por semilla.
    /// </summary>
    public static class OndaLib
    {
        // ==================================================================
        //  TEXTURAS COMPARTIDAS
        // ==================================================================

        private static Asset<Texture2D> _glow, _ring;

        /// <summary>El brillo radial suave (los segmentos del frente).</summary>
        private static Texture2D GlowTex =>
            (_glow ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow")).Value;

        /// <summary>El anillo fino (la retaguardia).</summary>
        private static Texture2D RingTex =>
            (_ring ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/Ring")).Value;

        // ==================================================================
        //  LAS CURVAS (públicas: las quiere todo el mundo)
        // ==================================================================

        /// <summary>
        /// Curva de EXPANSIÓN fast-out: 1−(1−t)^2.2 — sale como una
        /// explosión y frena al final (así se lee "energía").
        /// </summary>
        public static float Expansion(float progress)
        {
            progress = MathHelper.Clamp(progress, 0f, 1f);
            return 1f - MathF.Pow(1f - progress, 2.2f);
        }

        /// <summary>Curva de falloff de alpha por tipo.</summary>
        public static float Falloff(float progress, OndaFalloff type)
        {
            progress = MathHelper.Clamp(progress, 0f, 1f);
            return type switch
            {
                OndaFalloff.Linear => 1f - progress,
                OndaFalloff.Slow => MathF.Pow(1f - progress, 0.8f),
                _ => MathF.Pow(1f - progress, 1.6f),   // Quadratic (el estándar)
            };
        }

        /// <summary>Hash determinista [0,1).</summary>
        private static float H01(int seed, int a, int b)
        {
            int h = unchecked(seed * 374761393 + a * 668265263 + b * 1911520717);
            h = unchecked(h ^ (h >> 13));
            h = unchecked(h * 1274126177);
            h = unchecked(h ^ (h >> 16));
            return (h & 0xFFFFFF) / 16777216f;
        }

        /// <summary>Tinte de intensidad LINEAL de la casa (v6.50.3 — el Additive
        /// de FNA es (SourceAlpha, One): el alfa GATEA; RGB intacto, alfa=f).</summary>
        private static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            // v6.50.7 — REVERSIÓN AL PREMULTIPLICADO (la sonda v6.50.3
            // estaba incompleta): el pipeline REAL premultiplica los PNG al
            // cargar (ReLogic PngReader.PreMultiplyAlpha, verificado en el
            // decompilado del tML 2026.07.3.0) y el AlphaBlend de FNA es
            // (One, InvSourceAlpha) — compositing PREMULTIPLICADO, donde el
            // RGB del tinte ES la intensidad. El tinte lineal dejaba el
            // color SIN escalar en los lotes de masa (bruma fantasma
            // saturada) y sobrealimentaba los aditivos hasta ×10 (destellos
            // que inundaban la pantalla). El (RGB·f, A·f) de v6.25 es el
            // correcto para AMBOS presets de FNA.
            return new Color(
                (byte)(int)(c.R * f), (byte)(int)(c.G * f), (byte)(int)(c.B * f),
                (byte)(int)(255f * f));
        }

        // ==================================================================
        //  LA ONDA — el frente roto + la retaguardia
        // ==================================================================

        /// <summary>
        /// DIBUJA UNA ONDA EXPANSIVA en <paramref name="center"/> con radio
        /// actual = Expansion(progress)·maxRadius: frente fino-brillante de
        /// 12-16 segmentos con radio vivo (borde ROTO) + retaguardia en
        /// anillo ×0.5 alpha a +6% de radio + aberración cromática opcional
        /// (franjas R/B al ±1.8% — el idioma de las ondas cósmicas de la
        /// casa). El GROSOR engorda de 0.4·thickness a 1.4·thickness
        /// mientras el alpha muere.
        /// </summary>
        /// <param name="batch">Batch ABIERTO (aditivo recomendado).</param>
        /// <param name="center">Centro (espacio del lote del llamador).</param>
        /// <param name="progress">0..1 avance de la vida de la onda.</param>
        /// <param name="maxRadius">Radio FINAL en px.</param>
        /// <param name="thickness">Grosor máximo del frente (4..14 recomendado).</param>
        /// <param name="chromatic">True = añade las franjas R/B.</param>
        public static void Shock(SpriteBatch batch, Vector2 center, float progress,
            float maxRadius, Color color, float intensity, int seed,
            float thickness = 10f, OndaFalloff falloff = OndaFalloff.Quadratic,
            bool chromatic = false)
        {
            if (batch == null || maxRadius < 4f) return;
            progress = MathHelper.Clamp(progress, 0f, 1f);
            intensity = MathHelper.Clamp(intensity, 0f, 1f);

            float r = maxRadius * Expansion(progress);
            if (r < 2f) return;
            float a = intensity * Falloff(progress, falloff);
            if (a <= 0.01f) return;

            // === 1. LA RETAGUARDIA: el anillo grueso-tenue a +6% radio ===
            //     (la "onda de presión" detrás del frente de choque).
            float s = r * 1.06f * 2.174f;
            batch.Draw(RingTex, center, null, Tint(color, a * 0.5f), 0f,
                new Vector2(RingTex.Width, RingTex.Height) * 0.5f,
                new Vector2(s, s) / new Vector2(RingTex.Width, RingTex.Height),
                SpriteEffects.None, 0f);

            // === 2. EL FRENTE ROTO: 12-16 segmentos de arco con radio ===
            //     vivo (±8% por hash al cuarto de tick — el borde CRAZCA).
            int segs = Math.Clamp((int)(r / 24f), 12, 16);
            int tick4 = (int)(Main.GameUpdateCount * 0.25f);
            float grosor = thickness * (0.4f + 0.6f * progress) * (1f + 0.35f * a);

            for (int i = 0; i < segs; i++)
            {
                float ang0 = i / (float)segs * MathHelper.TwoPi;
                float ang1 = (i + 1) / (float)segs * MathHelper.TwoPi;
                float w0 = 1f + 0.08f * (H01(seed, i, tick4) - 0.5f);
                float w1 = 1f + 0.08f * (H01(seed, i + 1, tick4) - 0.5f);

                Vector2 p0 = center + new Vector2(MathF.Cos(ang0), MathF.Sin(ang0)) * (r * w0);
                Vector2 p1 = center + new Vector2(MathF.Cos(ang1), MathF.Sin(ang1)) * (r * w1);

                Seg(batch, p0, p1, grosor, Tint(color, a));

                // LA ABERRACIÓN CROMÁTICA: franjas R/B al ±1.8% del radio.
                if (chromatic)
                {
                    Color soloR = new((byte)(int)(color.R * 1.15f), (byte)(int)(color.G * 0.25f), (byte)(int)(color.B * 0.25f));
                    Color soloB = new((byte)(int)(color.R * 0.25f), (byte)(int)(color.G * 0.25f), (byte)(int)(color.B * 1.15f));
                    float rIn = 1f - 0.018f, rOut = 1f + 0.018f;
                    Vector2 q0 = center + new Vector2(MathF.Cos(ang0), MathF.Sin(ang0)) * (r * w0 * rIn);
                    Vector2 q1 = center + new Vector2(MathF.Cos(ang1), MathF.Sin(ang1)) * (r * w1 * rIn);
                    Seg(batch, q0, q1, grosor * 0.55f, Tint(soloR, a * 0.45f));
                    q0 = center + new Vector2(MathF.Cos(ang0), MathF.Sin(ang0)) * (r * w0 * rOut);
                    q1 = center + new Vector2(MathF.Cos(ang1), MathF.Sin(ang1)) * (r * w1 * rOut);
                    Seg(batch, q0, q1, grosor * 0.55f, Tint(soloB, a * 0.45f));
                }
            }
        }

        /// <summary>
        /// PULSO simple: UN anillo que crece y muere — el eco barato (la
        /// "muerte de NPC genérica", el latido de un sello). Frente
        /// continuo (sin romper) + retaguardia tenue: 2 quads.
        /// </summary>
        public static void Pulse(SpriteBatch batch, Vector2 center, float progress,
            float maxRadius, Color color, float intensity, int seed)
        {
            if (batch == null || maxRadius < 4f) return;
            progress = MathHelper.Clamp(progress, 0f, 1f);
            intensity = MathHelper.Clamp(intensity, 0f, 1f);

            float r = maxRadius * Expansion(progress);
            if (r < 2f) return;
            float a = intensity * Falloff(progress, OndaFalloff.Quadratic);
            if (a <= 0.01f) return;

            // El latido sutil del pulso (vive, no crece muerto).
            a *= 0.9f + 0.1f * MathF.Sin(Main.GlobalTimeWrappedHourly * 9f + seed);

            float s = r * 2.174f;
            batch.Draw(RingTex, center, null, Tint(color, a), 0f,
                new Vector2(RingTex.Width, RingTex.Height) * 0.5f,
                new Vector2(s, s) / new Vector2(RingTex.Width, RingTex.Height),
                SpriteEffects.None, 0f);
            s = r * 1.08f * 2.174f;
            batch.Draw(RingTex, center, null, Tint(color, a * 0.4f), 0f,
                new Vector2(RingTex.Width, RingTex.Height) * 0.5f,
                new Vector2(s, s) / new Vector2(RingTex.Width, RingTex.Height),
                SpriteEffects.None, 0f);
        }

        /// <summary>Segmento de arco (cápsula orientada entre dos puntos).</summary>
        private static void Seg(SpriteBatch batch, Vector2 p0, Vector2 p1, float w, Color tint)
        {
            if (tint.A == 0 || w < 0.5f) return;
            Vector2 mid = (p0 + p1) * 0.5f;
            Vector2 d = p1 - p0;
            float len = d.Length();
            if (len < 0.1f) return;
            float rot = MathF.Atan2(d.Y, d.X);
            batch.Draw(GlowTex, mid, null, tint, rot,
                new Vector2(GlowTex.Width, GlowTex.Height) * 0.5f,
                new Vector2(len + w, w * 1.9f) / new Vector2(GlowTex.Width, GlowTex.Height),
                SpriteEffects.None, 0f);
        }

        // ==================================================================
        //  v6.41 — EL TELEGRAPH (el aviso que precede al golpe)
        // ==================================================================

        /// <summary>
        /// v6.41 — EL TELEGRAPH: la LÍNEA DE AVISO que precede a un ataque
        /// alineado (el rayo que va a caer, el beam que va a disparar). LA
        /// regla de legibilidad del ecosistema premium: TODO golpe duro
        /// avisa ~30-60 ticks antes con una línea fina que CRECE en brillo
        /// — el jugador la ve, la entiende, la esquiva.
        ///
        /// El paquete (3 piezas, la gramática del aviso):
        ///   · LA BANDA ANCHA: un corredor tenue (×0.18 alfa, ancho ×4)
        ///     que marca el TERRENO del golpe — está ahí desde el tick 0.
        ///   · EL NÚCLEO FINO: la línea central que GANA brillo con el
        ///     progress (0.15 → 0.85) — el cuchillo que se afila.
        ///   · EL ANILLO DE ORIGEN: un aro en el punto de disparo que se
        ///     CONTRAE al ritmo del aviso (radio ×(1.6−progress) → 0.6) —
        ///     el "cargando" del disparo.
        ///
        /// EL PARPADEO DEL ÚLTIMO SEGUNDO: en el tramo final (progress
        /// &gt; 0.75) el núcleo late a 18 Hz — el aviso "chilla" justo
        /// antes del golpe (determinista: seno del reloj, nada de random).
        /// </summary>
        /// <param name="batch">Batch ABIERTO (aditivo recomendado).</param>
        /// <param name="origen">Punto de disparo (espacio del lote del llamador).</param>
        /// <param name="fin">Punto de llegada del golpe.</param>
        /// <param name="progress">0..1 del aviso (1 = el golpe dispara YA).</param>
        /// <param name="color">El color del ataque que viene.</param>
        /// <param name="anchoCore">Grosor del núcleo en px (2-4 recomendado).</param>
        public static void Telegrafo(SpriteBatch batch, Vector2 origen, Vector2 fin,
            float progress, Color color, float anchoCore = 3f)
        {
            if (batch == null) return;
            progress = MathHelper.Clamp(progress, 0f, 1f);

            float flick = 1f;
            if (progress > 0.75f)
            {
                // EL CHILLIDO: el aviso late rápido justo antes del golpe.
                float t = (progress - 0.75f) / 0.25f;
                flick = 0.6f + 0.4f * MathF.Sin(Main.GlobalTimeWrappedHourly * 18f * MathF.PI) * t + 0.4f * t;
            }

            float aCore = (0.15f + 0.7f * progress) * flick;
            float aBanda = 0.18f * (0.6f + 0.4f * progress);

            // 1. LA BANDA ANCHA: el corredor del golpe.
            Seg(batch, origen, fin, anchoCore * 4f, Tint(color, aBanda));

            // 2. EL NÚCLEO FINO: la línea que se afila.
            Seg(batch, origen, fin, anchoCore, Tint(color, aCore));

            // 3. EL ANILLO DE ORIGEN: se contrae con la carga.
            float r = (1.6f - progress) * anchoCore * 7f + 6f;
            float s = MathF.Max(r, 4f) * 2.174f;
            batch.Draw(RingTex, origen, null, Tint(color, aCore),
                (fin - origen).ToRotation(),
                new Vector2(RingTex.Width, RingTex.Height) * 0.5f,
                new Vector2(s, s) / new Vector2(RingTex.Width, RingTex.Height),
                SpriteEffects.None, 0f);
        }

        /// <summary>
        /// v6.49 — EL TELEGRAPH DE ANILLO (la idea nº1 de la auditoría
        /// AUD-B: el Telegrafo de línea era el ÚNICO del arsenal y los
        /// tres jefes con AoE repetían lógica a mano). El aviso de un
        /// GOLPE DE ÁREA: el anillo EXTERIOR (la frontera del golpe, fija
        /// y seria), el anillo INTERIOR que SE CIERRA al ritmo de la
        /// carga (radio ×(1−progress) → 0 — cuando el interior toca al
        /// exterior, el golpe cae: la señal más legible del repertorio),
        /// el disco tenue del interior y EL CHILLIDO final (parpadeo a
        /// 18 Hz en el último cuarto, igual que el Telegrafo de línea).
        ///
        /// CONTRATO: idéntico al de línea — batch ABIERTO (aditivo
        /// recomendado), progress 0..1 (1 = golpe YA), cero Main.rand,
        /// cero alocaciones (dos RingTex + un disco por llamada).
        /// </summary>
        /// <param name="batch">Batch ABIERTO (aditivo recomendado).</param>
        /// <param name="centro">Centro del área (espacio del lote del llamador).</param>
        /// <param name="radio">Radio EXTERIOR del golpe (la frontera).</param>
        /// <param name="progress">0..1 del aviso (1 = el golpe dispara YA).</param>
        /// <param name="color">El color del ataque que viene.</param>
        public static void TelegrafoAnillo(SpriteBatch batch, Vector2 centro, float radio,
            float progress, Color color)
        {
            if (batch == null || radio < 4f) return;
            progress = MathHelper.Clamp(progress, 0f, 1f);

            // EL CHILLIDO (el mismo del Telegrafo de línea).
            float flick = 1f;
            if (progress > 0.75f)
            {
                float t = (progress - 0.75f) / 0.25f;
                flick = 0.6f + 0.4f * MathF.Sin(Main.GlobalTimeWrappedHourly * 18f * MathF.PI) * t + 0.4f * t;
            }

            float aExt = (0.12f + 0.55f * progress) * flick;
            float aInt = (0.35f + 0.5f * progress) * flick;
            float aDisco = 0.06f + 0.10f * progress;

            // 1. LA FRONTERA: el anillo exterior (donde termina el golpe).
            float sExt = radio * 2.174f;
            batch.Draw(RingTex, centro, null, Tint(color, aExt), 0f,
                new Vector2(RingTex.Width, RingTex.Height) * 0.5f,
                new Vector2(sExt, sExt) / new Vector2(RingTex.Width, RingTex.Height),
                SpriteEffects.None, 0f);

            // 2. EL CUERPO: el disco tenue del área entera.
            float sDisco = radio * 2f;
            batch.Draw(GlowTex, centro, null, Tint(color, aDisco), 0f,
                new Vector2(GlowTex.Width, GlowTex.Height) * 0.5f,
                new Vector2(sDisco, sDisco) / new Vector2(GlowTex.Width, GlowTex.Height),
                SpriteEffects.None, 0f);

            // 3. EL CUENTO: el anillo interior SE CIERRA con la carga —
            //    cuando el interior alcanza la frontera, el golpe cae.
            float rInt = radio * (1f - progress);
            if (rInt > 2f)
            {
                float sInt = rInt * 2.174f;
                batch.Draw(RingTex, centro, null, Tint(color, aInt), 0f,
                    new Vector2(RingTex.Width, RingTex.Height) * 0.5f,
                    new Vector2(sInt, sInt) / new Vector2(RingTex.Width, RingTex.Height),
                    SpriteEffects.None, 0f);
            }
        }

        // ==================================================================
        //  LA ONDA DE SUELO — medio-anillo pegado al piso + polvo
        // ==================================================================

        /// <summary>
        /// v6.49 — LA ONDA DE SUELO VISUAL (solo el medio-anillo, sin el
        /// paquete de polvo): para las MARCAS DE TELEGRAPH pegadas al piso
        /// (la estrella fugaz de la Arquera la usa — la promesa del
        /// comentario de AtaqueJefeProjectile, CUMPLIDA por fin). El
        /// mismo contrato de la casa: batch ABIERTO, cero Main.rand,
        /// cero alocaciones.
        /// </summary>
        public static void GroundVisual(SpriteBatch batch, Vector2 center, float progress,
            float maxRadius, Color color, float intensity, float gravDir = 1f)
        {
            if (batch == null || maxRadius < 4f) return;
            progress = MathHelper.Clamp(progress, 0f, 1f);
            intensity = MathHelper.Clamp(intensity, 0f, 1f);

            float r = maxRadius * Expansion(progress);
            float a = intensity * Falloff(progress, OndaFalloff.Quadratic);
            if (a <= 0.01f) return;

            // === EL MEDIO-ANILLO: solo los segmentos de ARRIBA del piso ===
            int segs = 14;
            float grosor = 8f * (0.5f + 0.5f * progress);
            for (int i = 0; i < segs; i++)
            {
                float ang0 = MathHelper.Pi + i / (float)segs * MathHelper.Pi;
                float ang1 = MathHelper.Pi + (i + 1) / (float)segs * MathHelper.Pi;
                // En gravedad invertida el suelo está ARRIBA: espejar.
                float ang0m = gravDir < 0 ? -ang0 : ang0;
                float ang1m = gravDir < 0 ? -ang1 : ang1;

                Vector2 p0 = center + new Vector2(MathF.Cos(ang0m), MathF.Sin(ang0m)) * r;
                Vector2 p1 = center + new Vector2(MathF.Cos(ang1m), MathF.Sin(ang1m)) * r;
                Seg(batch, p0, p1, grosor, Tint(color, a * 0.8f));
            }
        }

        /// <summary>
        /// ONDA DE SUELO: medio-anillo pegado al piso (los impactos físicos
        /// de las armas cuerpo a cuerpo — el frente SOLO visible encima del
        /// suelo) + el PAQUETE DE POLVO que levanta (determinista: la
        /// librería NO spawnea — devuelve el paquete para el ParticleManager
        /// del llamador).
        /// v6.49 — EL WRAPPER: la parte VISUAL vive en GroundVisual (para
        /// las marcas de telegraph que NO quieren el paquete); este
        /// método es visual + polvo (el original, contrato intacto).
        /// </summary>
        /// <param name="gravDir">1 normal · −1 mundo invertido.</param>
        public static void Ground(SpriteBatch batch, Vector2 center, float progress,
            float maxRadius, Color color, float intensity, int seed,
            out ParticleData[] dustKick, float gravDir = 1f)
        {
            dustKick = null;
            GroundVisual(batch, center, progress, maxRadius, color, intensity, gravDir);

            if (batch == null || Main.netMode == NetmodeID.Server || maxRadius < 4f) return;
            if (progress >= 0.45f) return; // el polvo solo nace al nacer la onda
            progress = MathHelper.Clamp(progress, 0f, 1f);

            float r = maxRadius * Expansion(progress);

            // === EL POLVO: el paquete determinista (chispas que caen con
            //     drag + motas que flotan — listo para ParticleManager). ===
            {
                int count = 6;
                dustKick = new ParticleData[count];
                for (int k = 0; k < count; k++)
                {
                    float h1 = H01(seed, 700 + k, 23);
                    float h2 = H01(seed, 710 + k, 29);
                    float ang = MathHelper.Pi + h1 * MathHelper.Pi;   // abanico superior
                    if (gravDir < 0) ang = -ang;
                    float dist = r * (0.7f + 0.5f * h2);

                    var p = new ParticleData
                    {
                        Position = center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * dist,
                        Velocity = new Vector2(
                            MathF.Cos(ang) * (1.2f + 2.4f * h2),
                            MathF.Sin(ang) * (1.0f + 2.0f * h1) * gravDir),
                        Scale = Vector2.One * (0.5f + 0.9f * h1),
                        PackedColor = ParticleManager.PackColor(color),
                        PackedStartColor = ParticleManager.PackColor(color),
                        PackedEndColor = ParticleManager.PackColor(Color.Transparent),
                        TimeLeft = 26,
                        Duration = 26,
                        TextureId = ParticleTex.SoftGlow,
                        BlendMode = 1,   // aditivo
                        LayerPriority = LayerPriorities.BeforeProjectiles,
                    };
                    p.EnableComponent(ComponentFlag.FadeOut);
                    p.EnableComponent(ComponentFlag.Gravity);
                    dustKick[k] = p;
                }
            }
        }

        // ==================================================================
        //  EL PAQUETE DE IMPACTO — cámara y pantalla
        // (aplicados por OndaSystem en UN punto del frame — centralizados)
        // ==================================================================

        /// <summary>
        /// SACUDIDA de cámara gestionada: registra un impulso (strength px,
        /// decay cuadrático, vida en ticks) en el ACUMULADOR central que
        /// OndaSystem aplica SOLO en ModifyScreenPosition (un único punto
        /// del frame). Máx 2 sacudidas simultáneas (la mayor gana).
        /// Cap de amplitud: 14 px.
        /// </summary>
        /// <param name="directionAngle">Dirección del empujón; −1 = omnidireccional.</param>
        public static void Kick(float strengthPx, int durationTicks = 12, float directionAngle = -1f)
        {
            if (Main.netMode == NetmodeID.Server) return;
            OndaSystem.Kick(strengthPx, durationTicks, directionAngle);
        }

        /// <summary>
        /// DESTELLO LOCAL DEL ARMA (v6.50.7 — rediseñado a pedido): un
        /// GRADIENTE RADIAL que nace en <paramref name="centroMundo"/> (el
        /// CENTRO del proyectil del arma) y se apaga al alejarse — núcleo
        /// brillante + falda ancha, lote aditivo propio con Identity y el
        /// lote de interfaz restaurado (try/catch/finally). Antes era un
        /// velo a PANTALLA COMPLETA que ahogaba el cuadro entero en color.
        /// Máx 1 destello activo, fuerza por defecto 0.18, cooldown 30
        /// ticks. Sin origen (null): cae al centro del jugador local.
        /// </summary>
        /// <param name="centroMundo">El centro del proyectil del arma (coords de mundo; null = jugador local).</param>
        public static void Flash(Color color, float strength = 0.18f, int durationTicks = 8,
            Vector2? centroMundo = null)
        {
            if (Main.netMode == NetmodeID.Server) return;
            OndaSystem.Flash(color, strength, durationTicks, centroMundo);
        }
    }
}
