using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// EclipsePrimordialRenderer — v6.28 — EL ECLIPSE TOTAL (el rediseño).
    ///
    /// v6.28 — LA ORDEN DEL USUARIO: "rediseña el bastón de eclipse
    /// primordial". La versión v6.26 (el Sol de los 20 Anillos + la mezcla
    /// de TODOS los agujeros) se retira: era una fusión ocupada. EL NUEVO
    /// CONCEPTO es el nombre mismo del arma — UN ECLIPSE SOLAR TOTAL, la
    /// imagen más poderosa de toda la astronomía, contada con el lenguaje
    /// de la casa:
    ///
    ///   · EL SOL PRIMORDIAL: el disco blanco-oro DESNUDO (sin anillos,
    ///     sin runas — la luz original que da nombre al mod), respirando.
    ///   · EL DISCO DE LA NOCHE: el ocultador NEGRO (pase NO-premultiplicado
    ///     — ocluye de verdad) que se DESLIZA sobre el sol: mientras llega
    ///     se ve EL CRECIENTE (la luna comiéndose al sol); al centrarse,
    ///     el día MUERE (el proyectil apaga el mundo con RiftLib.Oscurecer
    ///     — el sesgo violeta de la casa).
    ///   · LA CORONA DEL ECLIPSE: 12 streamers BLANCO-ORO radiando desde
    ///     detrás del disco — las líneas de campo curvadas, ASIMÉTRICAS
    ///     (ecuatoriales largos, polares cortos — la corona REAL), cada
    ///     uno respirando a su paso y con su estela interior fluyendo.
    ///   · LA CROMOSFERA: el aro rojo profundo JUSTO al limbo (1.02R) con
    ///     sus perlas de Baily parpadeando.
    ///   · EL ANILLO DE DIAMANTE: UN punto brillante (floriturna de 4
    ///     puntas + bloom) que VIAJA por el limbo — lento durante el
    ///     total, ACELERANDO en la ÚLTIMA LUZ (el final se acerca).
    ///   · LAS PROMINENCIAS: 5 lenguas rojo-oro (PyraLib.Tongue — la
    ///     librería de fuego de la casa) lamiendo DESDE detrás del disco.
    ///   · LOS TRES CÍRCULOS RÚNICOS (la firma de la casa): blanco 2.4R
    ///     CW rápido + dorado 3.2R CW lento + violeta 4.0R CCW — la
    ///     escritura que contiene al eclipse.
    ///
    /// LA MUERTE es EL RETORNO DE LA LUZ: el disco IMPLODE (la noche se
    /// traga a sí misma), la corona EXPLOTA hacia afuera soplada por la
    /// nova y el núcleo queda CEGADOR un instante antes del estallido —
    /// el día VUELVE (el proyectil suelta la oscuridad).
    ///
    /// CONTRATO DE BATCH (v6.10): Draw()/DrawRetorno() exigen el
    /// SpriteBatch CERRADO y lo dejan CERRADO (los DOS lotes los abre y
    /// cierra el renderer: primero el ocultador NO-premultiplicado, luego
    /// TODO lo demás aditivo).
    /// </summary>
    public static class EclipsePrimordialRenderer
    {
        // ==================================================================
        //  PARÁMETROS — la geometría del eclipse
        // ==================================================================

        /// <summary>Radio del SOL PRIMORDIAL (px a escala 1 — el coloso).</summary>
        public const float SunPx = 74f;

        /// <summary>El disco de la noche cubre 0.94·R (el creciente vive en el 6%).</summary>
        public const float DiscoK = 0.94f;

        /// <summary>Los círculos rúnicos (blanco / dorado / violeta) — la firma de la casa.</summary>
        private const float RingWhite = 2.40f, RingGold = 3.20f, RingViolet = 4.00f;

        // --- EL DESLIZAMIENTO del disco (la fase de formación) ---
        private const int FormTicks = 45;

        // --- LA CORONA: 12 streamers (asimetría ecuatorial/polar) ---
        private const int Streamers = 12;

        // ==================================================================
        //  PALETA — el ECLIPSE: blanco incandescente + oro viejo + el rojo
        //  de la cromosfera + el violeta de la noche de la casa
        // ==================================================================

        private static readonly Color WhiteIncan = new(255, 250, 240);
        private static readonly Color SunGold = new(255, 205, 110);
        private static readonly Color ChromoRed = new(255, 70, 45);
        private static readonly Color PromRed = new(255, 110, 60);
        private static readonly Color PromGold = new(255, 190, 90);
        private static readonly Color NightViolet = new(140, 90, 235);
        private static readonly Color RuneGold = new(255, 180, 70);
        private static readonly Color RuneGoldTip = new(255, 235, 175);
        private static readonly Color RuneViolet = new(110, 130, 255);
        private static readonly Color RuneVioletTip = new(205, 220, 255);
        private static readonly Color RuneWhite = new(255, 245, 220);
        private static readonly Color RuneWhiteTip = new(255, 252, 240);

        // ==================================================================
        //  PINCELES
        // ==================================================================

        private static Asset<Texture2D> _glow, _ring, _black, _orb, _star;

        private static Texture2D Glow =>
            (_glow ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow")).Value;

        private static Texture2D Ring =>
            (_ring ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Ring")).Value;

        /// <summary>El disco negro 256² — EL OCULTADOR (la noche misma).</summary>
        private static Texture2D Black =>
            (_black ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/BlackDisk")).Value;

        private static Texture2D Orb =>
            (_orb ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/GlowOrb")).Value;

        /// <summary>La espiga degradada (el destello de 4 puntas del anillo de diamante).</summary>
        private static Texture2D StarTex =>
            (_star ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Star")).Value;

        // ==================================================================
        //  EL RENDER DEL ECLIPSE — contrato: batch CERRADO → CERRADO
        // ==================================================================

        /// <summary>
        /// DIBUJA EL ECLIPSE TOTAL. `sunR` = radio del sol primordial
        /// (escala del proyectil aplicada); `time` = GlobalTimeWrappedHourly;
        /// `seed` = determinismo; `age` = ticks de vida del proyectil (para
        /// el deslizamiento del disco); `lifeTicks` = su vida total; `alphaMul`
        /// = desvanecimiento global (la nova se lo come); `dibujarDisco` =
        /// false para EL RETORNO (el disco se pinte MURIÉNDOSE aparte).
        /// </summary>
        public static void Draw(Vector2 center, float sunR, float time, int seed,
            float age, float lifeTicks, float alphaMul = 1f, bool dibujarDisco = true)
        {
            if (alphaMul <= 0.02f || sunR < 1f) return;
            alphaMul = MathHelper.Clamp(alphaMul, 0f, 1f);

            // === LA FASE DE FORMACIÓN: el disco SE DESLIZA sobre el sol ===
            // (0..1 — al terminar, el eclipse es TOTAL y el día muere.)
            float formT = MathHelper.Clamp(age / FormTicks, 0f, 1f);
            // LA ÚLTIMA LUZ: el tramo final (últimos 12% de vida) — el anillo
            // de diamante ACELERA y la corona se aviva (el final se acerca).
            float lifeT = MathHelper.Clamp(age / lifeTicks, 0f, 1f);
            float ultima = lifeT > 0.88f ? (lifeT - 0.88f) / 0.12f : 0f;

            // El corazón del sol respira (±4% — el sol VIVE detrás de la noche).
            float breathe = VFXCore.Breathe(time, 1.6f, seed, 0.04f);
            float R = sunR * breathe;

            // === 1. EL SOL PRIMORDIAL (el disco blanco-oro desnudo) ===
            DrawSol(center, R, time, seed, alphaMul, formT);

            // === 2. EL DISCO DE LA NOCHE (el ocultador — lote NO-premult) ===
            if (dibujarDisco)
                DrawDiscoDeLaNoche(center, R, formT, time, seed, alphaMul);

            // === 3. LA CORONA + CROMOSFERA + PROMINENCIAS + DIAMANTE (aditivo) ===
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);

            // v6.50.3 — BLINDAJE (la lección v6.41): End en finally — una
            // excepción en cualquiera de las 5 capas no deja el lote abierto.
            try
            {
                DrawCorona(center, R, time, seed, alphaMul, formT, ultima);
                DrawCromosfera(center, R, time, seed, alphaMul, formT);
                DrawProminencias(center, R, time, seed, alphaMul, formT);
                DrawAnilloDeDiamante(center, R, time, seed, alphaMul, formT, ultima);
                DrawRayosFugitivos(center, R, time, seed, alphaMul);
            }
            finally
            {
                try { Main.spriteBatch.End(); } catch { }
            }

            // === 4. LOS TRES CÍRCULOS RÚNICOS (la firma — lote propio) ===
            DrawRuneCircles(center, R, time, seed, alphaMul);
        }

        /// <summary>
        /// EL RETORNO DE LA LUZ (la muerte): `progress` 0→1 — el disco IMPLODE
        /// (la noche se traga a sí misma), la corona EXPLODE soplada hacia
        /// afuera (×3 de longitud) y el núcleo queda CEGADOR. El proyectil
        /// suelta la oscuridad del mundo en paralelo (RiftLib) — el día vuelve.
        /// </summary>
        public static void DrawRetorno(Vector2 center, float sunR, float progress,
            float time, int seed)
        {
            progress = MathHelper.Clamp(progress, 0f, 1f);
            if (sunR < 1f) return;

            // EL COLAPSO DEL DISCO: rápido y TRAICIONERO (pow 1.6 — el
            // encogimiento SE LEE desde la mitad de la nova; el mock v6.28
            // con pow 2.2 lo escondía hasta el final).
            float diskDie = MathF.Pow(progress, 1.6f);

            // LA CORONA SOPLODA: se estira ×(1+2·progress) y se APAGA al final
            // (SIN el disco entero — el disco se pinte MURIÉNDOSE aparte).
            Draw(center, sunR, time, seed, FormTicks + 1f, FormTicks + 2f,
                1f - progress * 0.65f, dibujarDisco: false);

            // === EL NÚCLEO CEGADOR (el instante del retorno) ===
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);

            // v6.50.3 — BLINDAJE (lección v6.41): End en finally.
            try
            {
                float flash = MathF.Sin(progress * MathHelper.Pi);   // 0→1→0
                if (flash > 0.02f)
                {
                    // EL ESTALLIDO CEGADOR (×2 capas de bloom + la cruz ×2 pares).
                    Quad(Glow, center, new Vector2(sunR * 5.5f, sunR * 5.5f) * (0.5f + 0.8f * flash), 0f,
                        Tint(WhiteIncan, 0.55f * flash));
                    Quad(Glow, center, new Vector2(sunR * 3.6f, sunR * 3.6f) * (0.6f + 0.8f * flash), 0f,
                        Tint(WhiteIncan, 0.85f * flash));
                    Quad(Glow, center, new Vector2(sunR * 1.8f, sunR * 1.8f), 0f,
                        Tint(SunGold, 0.65f * flash));
                    // LA CRUZ del destello (la espiga ×2 pares — ancha y fina).
                    StarQuad(center, 0f, sunR * 6.4f * flash, MathF.Max(3.0f, sunR * 0.07f),
                        Tint(WhiteIncan, 0.9f * flash));
                    StarQuad(center, MathHelper.PiOver2, sunR * 6.4f * flash, MathF.Max(3.0f, sunR * 0.07f),
                        Tint(WhiteIncan, 0.9f * flash));
                    StarQuad(center, 0f, sunR * 3.8f * flash, MathF.Max(2.0f, sunR * 0.04f),
                        Tint(SunGold, 0.85f * flash));
                    StarQuad(center, MathHelper.PiOver2, sunR * 3.8f * flash, MathF.Max(2.0f, sunR * 0.04f),
                        Tint(SunGold, 0.85f * flash));
                }
            }
            finally
            {
                try { Main.spriteBatch.End(); } catch { }
            }

            // === EL DISCO MURIENDO: el ocultador se arremolina al centro ===
            // (se repinta encima del flash con su radio muriendo — la noche
            // se ve TRAGÁNDOSE a sí misma; pow 2.2: el colapso SE LEE.)
            if (diskDie < 0.99f)
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                try
                {
                    float rDie = sunR * DiscoK * (1f - diskDie);
                    Quad(Black, center, new Vector2(rDie * 2.2f, rDie * 2.2f), 0f,
                        Tint(Color.Black, 0.96f * (1f - diskDie * 0.6f)));
                }
                finally
                {
                    try { Main.spriteBatch.End(); } catch { }
                }
            }
        }

        // ==================================================================
        //  CAPA 1 — EL SOL PRIMORDIAL (el disco blanco-oro desnudo)
        // ==================================================================

        /// <summary>
        /// El sol DETRÁS del ocultador: núcleo blanco incandescente + cuerpo
        /// oro + el limbo caliente. Durante el deslizamiento se ve EL
        /// CRECIENTE (el disco aún no lo tapa); en el total solo su borde
        /// asoma bajo la corona.
        /// </summary>
        private static void DrawSol(Vector2 center, float R, float time, int seed, float aMul, float formT)
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);

            // v6.50.3 — BLINDAJE (lección v6.41): End en finally.
            try
            {

            // EL LIMBO caliente (el borde del sol ARDE más que el centro —
            // el limb darkening al revés, el lenguaje de la casa).
            Quad(Glow, center, new Vector2(R * 2.35f, R * 2.35f), 0f, Tint(SunGold, 0.42f * aMul));
            Quad(Glow, center, new Vector2(R * 1.95f, R * 1.95f), 0f, Tint(WhiteIncan, 0.30f * aMul));

            // EL CUERPO (el disco solar — núcleo sólido con el orbe).
            Quad(Glow, center, new Vector2(R * 1.62f, R * 1.62f), 0f, Tint(WhiteIncan, 0.85f * aMul));
            Quad(Orb, center, new Vector2(R * 1.5f, R * 1.5f), 0f, Tint(Color.White, 0.95f * aMul));

            // EL GRANULADO VIVO (7 celdas de convección doradas rotando
            // LENTO — el sol no es un disco plano, es un horno).
            for (int k = 0; k < 7; k++)
            {
                float h1 = Hash01(seed, 601 + k, 3);
                float h2 = Hash01(seed, 607 + k, 7);
                float ang = h1 * MathHelper.TwoPi + time * (0.05f + 0.03f * h2);
                float rr = R * (0.15f + 0.55f * h2);
                Vector2 p = center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * rr;
                float cell = 0.55f + 0.45f * MathF.Sin(time * (0.5f + 0.4f * h1) + k * 1.9f);
                Quad(Glow, p, new Vector2(R * 0.55f, R * 0.55f) * (0.7f + 0.3f * cell), 0f,
                    Tint(SunGold, 0.16f * cell * aMul));
            }

            }
            finally
            {
                try { Main.spriteBatch.End(); } catch { }
            }
        }

        // ==================================================================
        //  CAPA 2 — EL DISCO DE LA NOCHE (el ocultador NO-premultiplicado)
        // ==================================================================

        /// <summary>
        /// El ocultador: disco NEGRO (0.94·R) en lote NO-premultiplicado —
        /// TAPA al sol de verdad. Durante `formT` SE DESLIZA desde un lado
        /// (el tránsito: el creciente menguante); al centrarse el eclipse
        /// es TOTAL. El RIM: 1.5 px violeta-blanco (la última luz doblándose
        /// por la gravedad de la noche).
        /// </summary>
        private static void DrawDiscoDeLaNoche(Vector2 center, float R, float formT,
            float time, int seed, float aMul)
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);

            // EL DESLIZAMIENTO: entra desde la izquierda-arriba (smoothstep
            // — LENTO de verdad: el creciente se VE menguar; v6.28 mock:
            // el ease-out cuártico enterraba el creciente).
            float slide = formT * formT * (3f - 2f * formT);
            // El temblor del asentamiento (los últimos 15% de la llegada).
            float settle = formT > 0.85f ? MathF.Sin(time * 42f) * (1f - formT) * 2.2f : 0f;
            Vector2 offset = new Vector2(-R * 2.6f, -R * 0.5f) * (1f - slide)
                             + new Vector2(settle, settle * 0.4f);

            // EL DISCO (ligeramente MENOR que el sol: el creciente) — el
            // centro y el radio viven FUERA del primer try: el RIM (su
            // propio lote) los reutiliza.
            float diskR = R * DiscoK;
            Vector2 diskC = center + offset;

            // v6.50.3 — BLINDAJE (lección v6.41): End en finally.
            try
            {
                Quad(Black, diskC, new Vector2(diskR * 2.1f, diskR * 2.1f), 0f,
                    Tint(Color.Black, 0.97f * aMul));
            }
            finally
            {
                try { Main.spriteBatch.End(); } catch { }
            }

            // EL RIM (aditivo, tras el disco): la luz doblándose.
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
            try
            {
                float rimA = 0.34f * aMul * (0.75f + 0.25f * MathF.Sin(time * 3.1f + seed));
                Quad(Ring, diskC, new Vector2(diskR * 2.06f, diskR * 2.06f), 0f,
                    Tint(NightViolet, rimA));
                Quad(Ring, diskC, new Vector2(diskR * 2.0f, diskR * 2.0f), 0f,
                    Tint(WhiteIncan, rimA * 0.85f));
            }
            finally
            {
                try { Main.spriteBatch.End(); } catch { }
            }
        }

        // ==================================================================
        //  CAPA 3 — LA CORONA DEL ECLIPSE (los streamers)
        // ==================================================================

        /// <summary>
        /// LA CORONA: 12 streamers radiando desde detrás del disco —
        /// ASIMÉTRICOS como la corona real (los ecuatoriales LARGOS, los
        /// polares cortos y plumosos), curvados como líneas de campo, cada
        /// uno respirando a su paso con la ESTELA interior fluyendo. Con
        /// `ultima` crecen y se avivan (el final se acerca); `formT` los
        /// despierta cuando el disco llega.
        /// </summary>
        private static void DrawCorona(Vector2 center, float R, float time, int seed,
            float aMul, float formT, float ultima)
        {
            // El batch YA está ABIERTO (aditivo — lo abrió Draw()).
            float wake = MathHelper.Clamp(formT * 1.4f, 0f, 1f);   // despiertan con el total
            float grow = 1f + ultima * 0.35f;

            // LA BASE DE LA CORONA: el velo ANULAR (F-corona) — ANILLOS al
            // limbo, NO glows centrados (lección del mock v6.28: el glow
            // centrado LAVABA el disco negro — el centro de la noche debe
            // quedar NEGRO de verdad).
            RingQuad(center, R * 1.34f, 0f, Tint(WhiteIncan, 0.15f * wake * aMul));
            RingQuad(center, R * 1.10f, 0f, Tint(WhiteIncan, 0.22f * wake * aMul));

            for (int s = 0; s < Streamers; s++)
            {
                float h1 = Hash01(seed, 701 + s, 3);
                float h2 = Hash01(seed, 709 + s, 7);
                float h3 = Hash01(seed, 719 + s, 11);
                float h4 = Hash01(seed, 727 + s, 13);

                // EL ÁNGULO base del streamer.
                float ang = s / (float)Streamers * MathHelper.TwoPi + h1 * 0.35f;

                // LA ASIMETRÍA REAL: |sin(ángulo)| grande = ECUATORIAL (largo);
                // pequeño = POLAR (corto y plumoso) — la forma de la corona
                // en el mínimo solar, como en las fotos del total.
                float lat = MathF.Abs(MathF.Sin(ang + MathHelper.PiOver4));
                float length = R * MathHelper.Lerp(1.15f, 2.55f, lat * lat) *
                               (0.75f + 0.5f * h2) * grow;

                // LA RESPIRACIÓN propia (lenta — cada streamer a su paso).
                length *= 1f + 0.09f * MathF.Sin(time * (0.35f + 0.3f * h3) + s * 2.3f);

                // LA CURVATURA de línea de campo (hacia el ecuador — la
                // estructura dipolar arquea los streamers).
                float curve = MathF.Sin(ang + MathHelper.PiOver4) * (0.28f + 0.22f * h4);
                float baseR = R * (0.92f + 0.05f * h3);
                Vector2 dir = new(MathF.Cos(ang), MathF.Sin(ang));
                Vector2 perp = new(-dir.Y, dir.X);

                // EL ANCHO: gordo en la base, aguja en la punta (el taper).
                float wBase = R * (0.16f + 0.13f * lat) * (0.8f + 0.4f * h1);

                // EL STREAMER: 4 segmentos siguiendo la curva (la cadena de
                // la casa — quads con solape, como la Grieta de RiftLib).
                const int Segs = 4;
                Vector2 prev = center + dir * baseR;
                for (int i = 0; i < Segs; i++)
                {
                    float f0 = i / (float)Segs, f1 = (i + 1) / (float)Segs;
                    // La curva: el streamer se ARQUEA (offset perpendicular
                    // creciente con la distancia).
                    Vector2 p0 = center + dir * (baseR + length * f0)
                                       + perp * (curve * length * f0 * f0);
                    Vector2 p1 = center + dir * (baseR + length * f1)
                                       + perp * (curve * length * f1 * f1);

                    float wseg = wBase * (1f - (f0 + f1) * 0.5f) + R * 0.02f;
                    Vector2 mid = (p0 + p1) * 0.5f;
                    Vector2 delta = p1 - p0;
                    float len = delta.Length();
                    if (len < 0.5f) continue;
                    float rot = MathF.Atan2(delta.Y, delta.X);

                    // EL FLUJO interior (la estela que corre por dentro —
                    // la corona EMITE, no es un dibujo estático).
                    float flow = 0.72f + 0.28f * MathF.Sin(time * (2.2f + 1.5f * h2) - f0 * 9f + s);

                    // VELO ancho tenue + CUERPO blanco + PUNTA dorada.
                    Quad(Glow, mid, new Vector2(len + wseg * 2.2f, wseg * 3.0f), rot,
                        Tint(WhiteIncan, 0.10f * wake * aMul * flow));
                    Quad(Glow, mid, new Vector2(len + wseg * 1.1f, wseg * 1.5f), rot,
                        Tint(WhiteIncan, 0.20f * wake * aMul * flow));
                    Quad(Glow, mid, new Vector2(len + wseg * 0.7f, wseg * 0.7f), rot,
                        Tint(Color.Lerp(WhiteIncan, SunGold, f0 * 0.6f), 0.30f * wake * aMul * flow));

                    // LA PERLA del segmento (la continuidad de la cadena).
                    Quad(Glow, p1, new Vector2(wseg * 2.4f, wseg * 2.4f), rot,
                        Tint(WhiteIncan, 0.14f * wake * aMul * flow));

                    prev = p1;
                }

                // LA PUNTA DEL STREAMER: una mota que se desprende y fluye
                // (la corona "llueve" al espacio — el viento solar).
                if (h4 > 0.45f)
                {
                    float fTip = 1.06f + 0.05f * MathF.Sin(time * 0.9f + s * 1.7f);
                    Vector2 tip = center + dir * (baseR + length * fTip)
                                        + perp * (curve * length * fTip * fTip);
                    Quad(Orb, tip, new Vector2(R * 0.07f, R * 0.07f) * (0.7f + 0.6f * h2), 0f,
                        Tint(WhiteIncan, 0.4f * wake * aMul));
                }
            }
        }

        // ==================================================================
        //  CAPA 4 — LA CROMOSFERA (el aro rojo del limbo)
        // ==================================================================

        /// <summary>
        /// La cromosfera: el aro rojo profundo JUSTO al limbo (1.02-1.05R)
        /// con las PERLAS DE BAILY parpadeando (las últimas gotas de luz
        /// por los valles del limbo) — SOLO visible cuando el eclipse ya
        /// es total (formT completa).
        /// </summary>
        private static void DrawCromosfera(Vector2 center, float R, float time, int seed,
            float aMul, float formT)
        {
            if (formT < 0.92f) return;
            float vis = (formT - 0.92f) / 0.08f;

            Quad(Ring, center, new Vector2(R * 2.10f, R * 2.10f), 0f,
                Tint(ChromoRed, 0.30f * vis * aMul));
            Quad(Ring, center, new Vector2(R * 2.045f, R * 2.045f), 0f,
                Tint(ChromoRed, 0.22f * vis * aMul));

            // LAS PERLAS DE BAILY (7 motitas doradas sobre el aro).
            for (int b = 0; b < 7; b++)
            {
                float h1 = Hash01(seed, 801 + b, 3);
                float ang = h1 * MathHelper.TwoPi + time * 0.03f;
                Vector2 p = center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * R * 1.03f;
                float tw = 0.4f + 0.6f * MathF.Max(0f, MathF.Sin(time * (3f + 2f * h1) + b * 2.6f));
                if (tw < 0.15f) continue;
                Quad(Orb, p, new Vector2(R * 0.055f, R * 0.055f) * (0.8f + 0.5f * tw), 0f,
                    Tint(PromGold, 0.7f * vis * aMul * tw));
            }
        }

        // ==================================================================
        //  CAPA 5 — LAS PROMINENCIAS (las lenguas de PyraLib)
        // ==================================================================

        /// <summary>
        /// LAS PROMINENCIAS: 5 lenguas rojo-oro lamiendo DESDE detrás del
        /// disco (arcos anclados al limbo que se curvan y vuelven) — las
        /// erupciones de la cara oculta asomando por el borde. Parpadeo
        /// inconmensurable de PyraLib (el flicker de la casa).
        /// </summary>
        private static void DrawProminencias(Vector2 center, float R, float time, int seed,
            float aMul, float formT)
        {
            if (formT < 0.88f) return;
            float vis = (formT - 0.88f) / 0.12f;

            for (int p = 0; p < 5; p++)
            {
                float h1 = Hash01(seed, 851 + p, 3);
                float h2 = Hash01(seed, 857 + p, 7);
                float h3 = Hash01(seed, 863 + p, 11);

                float ang = h1 * MathHelper.TwoPi + time * 0.05f * (h2 > 0.5f ? 1f : -1f);
                Vector2 anchor = center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * R * 0.97f;
                Vector2 outDir = new(MathF.Cos(ang), MathF.Sin(ang));

                // LA VIDA de la lengua: crece, ARQUEA y cae (el ciclo ~2 s).
                float life = 0.5f + 0.5f * MathF.Sin(time * (0.45f + 0.35f * h2) + p * 2.1f);
                if (life < 0.25f) continue;
                float len = R * (0.18f + 0.30f * h3) * life * vis;

                // EL ARCO: sale radial y SE CURVA de vuelta (las prominencias
                // real-mente son lazos magnéticos — caen de vuelta al sol).
                Vector2 side = new(-outDir.Y, outDir.X);
                const int Segs = 3;
                Vector2 prev = anchor;
                for (int i = 0; i < Segs; i++)
                {
                    float f0 = i / (float)Segs, f1 = (i + 1) / (float)Segs;
                    Vector2 p0 = anchor + outDir * (len * f0) + side * (len * 0.55f * f0 * f0);
                    Vector2 p1 = anchor + outDir * (len * f1) + side * (len * 0.55f * f1 * f1);
                    Vector2 mid = (p0 + p1) * 0.5f;
                    Vector2 delta = p1 - p0;
                    float segLen = delta.Length();
                    if (segLen < 0.5f) continue;
                    float rot = MathF.Atan2(delta.Y, delta.X);

                    // EL PARPADEO INCONMENSURABLE (PyraLib): la lengua NO
                    // arde pareja — chisporrotea por hash.
                    float flick = 0.65f + 0.35f * MathF.Sin(time * (9f + 6f * h3) + i * 5.1f + p * 3.7f);
                    float wseg = R * (0.070f - 0.012f * i) * (0.8f + 0.4f * h2) * life;

                    Quad(Glow, mid, new Vector2(segLen + wseg * 2f, wseg * 2.4f), rot,
                        Tint(PromRed, 0.24f * vis * aMul * flick));
                    Quad(Glow, mid, new Vector2(segLen + wseg * 1.1f, wseg * 1.3f), rot,
                        Tint(Color.Lerp(PromRed, PromGold, 0.45f), 0.34f * vis * aMul * flick));
                    Quad(Glow, mid, new Vector2(segLen + wseg * 0.6f, wseg * 0.6f), rot,
                        Tint(PromGold, 0.42f * vis * aMul * flick));
                    prev = p1;
                }
            }
        }

        // ==================================================================
        //  CAPA 6 — EL ANILLO DE DIAMANTE (el destello viajero)
        // ==================================================================

        /// <summary>
        /// EL ANILLO DE DIAMANTE: UN punto brillante en el limbo — la
        /// floriturna de 4 puntas + bloom + mota — que VIAJA lento por el
        /// borde (0.13 rad/s) y ACELERA en la ÚLTIMA LUZ (×6 — el final
        /// se acerca y la noche empieza a perder el asimiento).
        /// </summary>
        private static void DrawAnilloDeDiamante(Vector2 center, float R, float time, int seed,
            float aMul, float formT, float ultima)
        {
            if (formT < 0.96f) return;
            float vis = (formT - 0.96f) / 0.04f;

            // EL VIAJE: lento, y ACELERA con la última luz.
            float speed = 0.13f * (1f + ultima * 6f);
            float ang = seed * 0.7f + time * speed;

            Vector2 p = center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * R * 0.985f;
            float breathe = 0.9f + 0.1f * MathF.Sin(time * 6.3f + seed);

            // EL BLOOM del diamante.
            Quad(Glow, p, new Vector2(R * 0.85f, R * 0.85f) * breathe, 0f,
                Tint(WhiteIncan, 0.34f * vis * aMul));
            Quad(Glow, p, new Vector2(R * 0.40f, R * 0.40f) * breathe, 0f,
                Tint(WhiteIncan, 0.6f * vis * aMul));

            // LA FLORITURNA de 4 puntas (la espiga ×2, vertical y tangente).
            Vector2 tangent = new(-MathF.Sin(ang), MathF.Cos(ang));
            float rotT = MathF.Atan2(tangent.Y, tangent.X);
            float k = R * 0.9f * vis * breathe;
            StarQuad(p, rotT, k, MathF.Max(2.2f, R * 0.035f), Tint(WhiteIncan, 0.95f * vis * aMul));
            StarQuad(p, rotT + MathHelper.PiOver2, k * 0.55f, MathF.Max(2.0f, R * 0.03f),
                Tint(WhiteIncan, 0.85f * vis * aMul));

            // LA MOTA (el diamante mismo).
            Quad(Orb, p, new Vector2(R * 0.10f, R * 0.10f), 0f,
                Tint(Color.White, 0.95f * vis * aMul));
        }

        // ==================================================================
        //  CAPA 7 — LOS RAYOS FUGITIVOS (la tensión de la noche)
        // ==================================================================

        /// <summary>
        /// Los RAYOS FUGITIVOS: de cuando en cuando (StormLib.IsLit) un
        /// arco crispado SALTA del limbo al círculo rúnico violeta — la
        /// tensión de la noche contenida por la escritura.
        /// </summary>
        private static void DrawRayosFugitivos(Vector2 center, float R, float time, int seed,
            float aMul)
        {
            int flick = (int)(time * 60f);
            for (int c = 0; c < 3; c++)
            {
                if (!StormLib.IsLit(seed + 71 + c * 19, flick, 0.75f)) continue;
                float ang = Hash01(seed, 941 + c, flick) * MathHelper.TwoPi;
                Vector2 a = center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * R * 0.98f;
                Vector2 b = center + new Vector2(MathF.Cos(ang + 0.5f), MathF.Sin(ang + 0.5f)) * R * RingViolet * 0.92f;
                Vector2 mid = (a + b) * 0.5f;
                float len = Vector2.Distance(a, b);
                if (len < 4f) continue;
                float rot = MathF.Atan2(b.Y - a.Y, b.X - a.X);
                Quad(Glow, mid, new Vector2(len, MathF.Max(2.2f, R * 0.028f)), rot,
                    Tint(RuneVioletTip, 0.5f * aMul));
                Quad(Glow, mid, new Vector2(len * 0.9f, MathF.Max(1.2f, R * 0.014f)), rot,
                    Tint(WhiteIncan, 0.8f * aMul));
            }
        }

        // ==================================================================
        //  CAPA 8 — LOS TRES CÍRCULOS RÚNICOS (la firma de la casa)
        // ==================================================================

        /// <summary>
        /// LOS TRES CÍRCULOS: blanco 2.4R CW rápido + dorado 3.2R CW lento
        /// + violeta 4.0R CCW — la escritura que CONTIENE al eclipse (la
        /// técnica exacta del Supremo: RingQuad + glifos DE PIE + perlas).
        /// v6.37: NO delega en OrbitaLib.CirculoRunico — su geometría de
        /// cápsula es propia (len+0.35·w, w) y el refactor rompería el
        /// look 1:1.
        /// </summary>
        private static void DrawRuneCircles(Vector2 center, float rr, float time, int seed,
            float aMul)
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);

            // v6.50.3 — BLINDAJE (lección v6.41): End en finally.
            try
            {

            float gs = Math.Max(rr / 118f, 0.30f) * 1.05f;

            // EL CÍRCULO BLANCO ÍNTIMO — 6 runas CW rápido (el lazo interior).
            RingQuad(center, RingWhite * rr, time * 0.16f, Tint(RuneWhite, 0.24f * aMul));
            for (int g = 0; g < 6; g++)
                DrawRune(center, rr, time, g, RingWhite, RuneWhite, RuneWhiteTip,
                    gs * 0.92f, 6, 0.16f, 6, aMul);

            // EL CÍRCULO DORADO — 8 runas CW lento (con el conjunto).
            RingQuad(center, RingGold * rr, time * 0.10f, Tint(RuneGold, 0.20f * aMul));
            for (int g = 0; g < 8; g++)
                DrawRune(center, rr, time, g, RingGold, RuneGold, RuneGoldTip,
                    gs, 8, 0.10f, 0, aMul);

            // EL CÍRCULO VIOLETA — 6 runas MÁS AFUERA girando CCW (la envoltura).
            RingQuad(center, RingViolet * rr, time * -0.075f, Tint(RuneViolet, 0.18f * aMul));
            for (int g = 0; g < 6; g++)
                DrawRune(center, rr, time, g, RingViolet, RuneViolet, RuneVioletTip,
                    gs * 0.85f, 6, -0.075f, 3, aMul);

            }
            finally
            {
                try { Main.spriteBatch.End(); } catch { }
            }
        }

        /// <summary>
        /// Una runa del círculo (glifo DE PIE + resplandor + PERLA) — la
        /// técnica exacta del Supremo: el radio respira, el glifo se mece
        /// y la perla de la corona late encima.
        /// </summary>
        private static void DrawRune(Vector2 center, float rr, float time, int g,
            float radius, Color body, Color tip, float glyphScale,
            int count, float orbit, int offset, float aMul)
        {
            float ang = g / (float)count * MathHelper.TwoPi + time * orbit;

            // Flotación viva: el radio respira por glifo y el glifo se mece.
            float floatR = radius * rr + 2.4f * glyphScale * MathF.Sin(time * 1.35f + g * 0.9f);
            float bobY = 2.0f * glyphScale * MathF.Sin(time * 0.85f + g * 1.7f);
            Vector2 glyphPos = center + new Vector2(
                MathF.Cos(ang) * floatR, MathF.Sin(ang) * floatR + bobY);

            // Latido de brillo propio por glifo.
            float pulse = 0.75f + 0.25f * MathF.Sin(time * 2.4f + g * 1.3f);

            // Resplandor suave DETRÁS de cada runa.
            Quad(Glow, glyphPos, new Vector2(36f * glyphScale, 36f * glyphScale), 0f,
                Tint(body, 0.20f * pulse * aMul));

            // Trazos: cápsulas, cuerpo → punta pálida (runa DE PIE).
            Vector2[] strokes = _runes[(g + offset) % _runes.Length];
            for (int s = 0; s < strokes.Length; s += 2)
            {
                Vector2 a = glyphPos + strokes[s] * glyphScale;
                Vector2 b = glyphPos + strokes[s + 1] * glyphScale;
                Vector2 mid = (a + b) * 0.5f;
                Vector2 delta = b - a;
                float len = delta.Length();
                if (len < 0.01f) continue;
                float rot = MathF.Atan2(delta.Y, delta.X);

                // Gradiente vertical: abajo cuerpo, arriba punta pálida.
                float localY = ((strokes[s].Y + strokes[s + 1].Y) * 0.5f + 7f) / 14f;
                Color col = Color.Lerp(tip, body, 1f - localY * 0.25f);

                Capsule(mid, len, 3.4f * glyphScale, rot, Tint(col, 0.85f * pulse * aMul));
            }

            // PERLA sobre el glifo (la gema de la corona).
            Vector2 pearlPos = glyphPos - new Vector2(0f, 11.5f * glyphScale);
            float pearlPulse = 0.8f + 0.2f * MathF.Sin(time * 3.0f + g * 2.0f);
            Quad(Glow, pearlPos, new Vector2(7.0f * glyphScale, 7.0f * glyphScale), 0f,
                Tint(body, 0.62f * pulse * aMul));
            Quad(Glow, pearlPos, new Vector2(3.2f * glyphScale, 3.2f * glyphScale), 0f,
                Tint(tip, 0.9f * pearlPulse * aMul));
        }

        /// <summary>
        /// Tabla de glifos DE PIE del SUPREMO: cada runa es una lista de
        /// TRAZOS (pares de puntos en espacio local ~11×15). Ocho diseños
        /// angulares originales de la corona suprema, heredados por el
        /// eclipse (la escritura que contiene la noche).
        /// </summary>
        private static readonly Vector2[][] _runes = new Vector2[][]
        {
            // S0 — EL SOL ROTO
            new Vector2[] { new(0f, -7f), new(0f, 7f), new(-3.5f, -3f), new(0f, -6.5f), new(3.5f, -3f), new(0f, -6.5f), new(-3.5f, 3.5f), new(3.5f, 3.5f), new(-2f, 5.5f), new(2f, 5.5f) },
            // S1 — EL CETRO
            new Vector2[] { new(0f, 7f), new(0f, -4f), new(0f, -4f), new(-3f, -7f), new(0f, -4f), new(3f, -7f), new(-2.5f, 0f), new(2.5f, 0f), new(-2.5f, 3f), new(2.5f, 3f) },
            // S2 — EL TRONO
            new Vector2[] { new(-3.5f, 7f), new(-3.5f, -5f), new(-3.5f, -5f), new(3.5f, -5f), new(3.5f, -5f), new(3.5f, 7f), new(-3.5f, -5f), new(0f, -7f), new(-1.5f, 1.5f), new(1.5f, 1.5f) },
            // S3 — LA ESTRELLA DOBLE
            new Vector2[] { new(0f, 7f), new(0f, -7f), new(-4f, 0f), new(4f, 0f), new(-2.5f, -4.5f), new(2.5f, 4.5f), new(2.5f, -4.5f), new(-2.5f, 4.5f) },
            // S4 — EL CIRCUITO REAL
            new Vector2[] { new(-3.5f, 6f), new(-3.5f, -4f), new(-3.5f, -4f), new(3.5f, -4f), new(3.5f, -4f), new(3.5f, 6f), new(-3.5f, 6f), new(3.5f, 6f), new(-3.5f, -6.5f), new(3.5f, -6.5f), new(0f, -4f), new(0f, -6.5f) },
            // S5 — LA VUELTA SUPREMA
            new Vector2[] { new(-3f, 6f), new(-3f, -2f), new(-3f, -2f), new(3f, -6f), new(3f, -6f), new(3f, 2f), new(3f, 2f), new(-2.5f, 6f), new(-1.5f, -6.5f), new(1.5f, -6.5f) },
            // S6 — EL OJO DEL VACÍO
            new Vector2[] { new(-4f, 0f), new(0f, -4f), new(0f, -4f), new(4f, 0f), new(4f, 0f), new(0f, 4f), new(0f, 4f), new(-4f, 0f), new(-1.5f, 0f), new(1.5f, 0f), new(0f, -7f), new(0f, -4.5f), new(0f, 4.5f), new(0f, 7f) },
            // S7 — LA CORONA ESTELAR
            new Vector2[] { new(-4f, 5.5f), new(-4f, -5.5f), new(-4f, -5.5f), new(-2f, -1f), new(-2f, -1f), new(0f, -6.5f), new(0f, -6.5f), new(2f, -1f), new(2f, -1f), new(4f, -5.5f), new(4f, -5.5f), new(4f, 5.5f), new(-4f, 5.5f), new(4f, 5.5f) },
        };

        // ==================================================================
        //  PRIMITIVAS INTERNAS (las de la casa)
        // ==================================================================

        private static void Quad(Texture2D tex, Vector2 pos, Vector2 size, float rot, Color tint)
        {
            if (tex == null || tint.A == 0 || size.X < 0.1f || size.Y < 0.1f) return;
            Main.spriteBatch.Draw(tex, pos, null, tint, rot,
                new Vector2(tex.Width, tex.Height) * 0.5f,
                size / new Vector2(tex.Width, tex.Height),
                SpriteEffects.None, 0f);
        }

        private static void Capsule(Vector2 mid, float len, float width, float rot, Color tint)
        {
            len += width * 0.35f;
            Main.spriteBatch.Draw(Glow, mid, null, tint, rot,
                new Vector2(Glow.Width, Glow.Height) * 0.5f,
                new Vector2(len, width) / new Vector2(Glow.Width, Glow.Height),
                SpriteEffects.None, 0f);
        }

        private static void RingQuad(Vector2 pos, float visibleRadius, float rot, Color tint)
        {
            if (tint.A == 0 || visibleRadius < 1f) return;
            Main.spriteBatch.Draw(Ring, pos, null, tint, rot,
                new Vector2(Ring.Width, Ring.Height) * 0.5f,
                new Vector2(visibleRadius * 2.174f, visibleRadius * 2.174f) /
                new Vector2(Ring.Width, Ring.Height),
                SpriteEffects.None, 0f);
        }

        private static void StarQuad(Vector2 center, float rot, float largo, float ancho, Color tint)
        {
            if (tint.A == 0 || largo < 1f) return;
            Main.spriteBatch.Draw(StarTex, center, null, tint, rot,
                new Vector2(StarTex.Width, StarTex.Height) * 0.5f,
                new Vector2(largo, ancho) / new Vector2(StarTex.Width, StarTex.Height),
                SpriteEffects.None, 0f);
        }

        private static float Hash01(int seed, int a, int b) => VFXCore.Hash01(seed, a, b);

        private static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            // v6.50.3 — FIX (sonda IL contra el FNA real): BlendState.Additive
            // de FNA es (SourceAlpha, One) — el alfa GATEA el aporte. El Tint
            // premultiplicado v6.25 atenuaba DOS VECES (intensidad real f²:
            // el halo 0.30 salía a 0.09). RGB intacto, alfa=f: LINEAL.
            return new Color(c.R, c.G, c.B, (byte)(int)(255f * f));
        }
    }
}
