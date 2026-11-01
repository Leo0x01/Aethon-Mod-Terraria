using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;
using AethonMod.Content.Effects.Bruma;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// SupremoBlackHoleRenderer — v6.23 → v6.80 — EL AGUJERO NEGRO SUPREMO,
    /// 100% CÓDIGO… Y AHORA TAMBIÉN EL AURORA (LA FUSIÓN nº1 DE LA
    /// AUDITORÍA v6.78, EJECUTADA).
    ///
    /// v6.80 — LA FUSIÓN POR PERFIL DE PALETA: SupremoAuroraBlackHoleRenderer
    /// era un clon del 79% de este archivo (~530 líneas idénticas — el propio
    /// header de aquel confesaba «SOLO cambia el COLOR»). HOY existe UN solo
    /// motor: este archivo, parametrizado por <see cref="PerfilSupremo"/> —
    /// un struct de DATOS puros (39 slots de color + el gradiente de 3
    /// paradas + un bool de estilo). El Aurora quedó reducido a un SHIM
    /// delgado que llama `Draw(..., PerfilAurora)`; el Supremo clásico
    /// conserva su firma histórica `Draw(center, scale, time, seed)` (=
    /// el perfil Clasico). Los 12 callsites externos NO se tocaron.
    ///
    /// Lo que NO es solo color (los 4 puntos de LÓGICA dual, medidos con
    /// diff de cuerpos): el gradiente RADIAL de la banda de anillos, el
    /// gradiente de los JETS polares, el color-por-FASE de las ondas de
    /// distorsión y el gradiente de 3 paradas de los brazos espirales —
    /// todos viven detrás del bool <see cref="PerfilSupremo.GradienteEnTodo"/>
    /// (true = el ESTILO AURORA; false = el TÉRMICO clásico). Un futuro
    /// «Supremo X» es un nuevo perfil de 39 números, CERO líneas nuevas.
    ///
    /// v6.23 — EL TRIPLE CÍRCULO DE RUNAS (petición del usuario): junto
    /// a los dos anillos rúnicos clásicos nace el TERCERO — el círculo
    /// íntimo (6 runas @2.02·R, CW rápido) entre el anillo de fotones y
    /// el interior.
    ///
    /// EL 5to AGUJERO DEFINITIVO — LA FUSIÓN DE LOS 4 (petición del
    /// usuario): "el agujero negro supremo, la combinación de los 4
    /// agujeros, mejorado, potenciado y mejor animado. El agujero más
    /// espectacular del mod". Cada uno aporta su HERENCIA:
    ///
    ///   · del UMBRAL    → el DISCO DOPPLER oblicuo con ESTRÍAS PINTADAS
    ///                     que van del blanco-oro (el lado que se acerca)
    ///                     al carmesí profundo (el que se aleja).
    ///   · del CÓSMICO   → el ANILLO DE BANDAS del shader: 20 zonas de
    ///                     brillo viajando a 5 rad/s (glow = sin(θ·20+t·5)).
    ///   · del OLVIDO    → los BRAZOS ESPIRALES del vórtice con flujo
    ///                     animado (puntos que espiralan hacia adentro)
    ///                     y los corredores de fotones.
    ///   · de la BRUMA   → el HALO de nubes (BrumaFX.Cloud) y las VOLUTAS
    ///                     cayendo al núcleo (BrumaFX.Tendril).
    ///   · DE NUEVO (v6.18) → LA CORONA DE RAYOS: StormLib — 3 ARCOS
    ///     eléctricos vibrando alrededor del horizonte (~10 Hz) y 3 RAYOS
    ///     escapando del anillo (2 carmesí + 1 dorado), doble tira
    ///     cuerpo/núcleo con ramas.
    ///
    /// EL NÚCLEO SIGUE LIMPIO (v6.18): el centro de la bola negra queda
    /// NEGRO ABSOLUTO, sin partículas interiores — y se REPINTA al final
    /// para devorar cualquier derrame (el vacío siempre gana).
    ///
    /// LOS DOS PERFILES DE LA CASA:
    ///   · <see cref="Clasico"/> — ORO/CARMESÍ DOMINA, violeta/azul
    ///     ACENTÚA; rim del horizonte DORADO; jets oro-arriba/violeta-
    ///     abajo; ondas oro/violeta/blanco alternando (el de siempre).
    ///   · <see cref="Aurora"/> — EL AURORA (antes el clon entero): el
    ///     gradiente AURORA de 3 paradas (morado→azul→dorado) reina en
    ///     TODO lo que se desenrolla: brazos, banda radial, jets y ondas
    ///     (nace morada, muere dorada); rim morado; runas azul/oro/morado.
    ///
    /// LAS CAPAS (orden de pintado):
    ///   1.  AURA OSCURA (alfa) — el vacío absorbe la luz.
    ///   2.  NEBULOSAS + polvo ambiental.
    ///   3.  HALO DE HUMO — 2 BrumaFX.Cloud.
    ///   4.  DISCO DOPPLER oblicuo — mitad TRASERA con estrías pintadas.
    ///   4b. ECO TENUE del anillo de bandas (mitad trasera, detrás).
    ///   5.  NÚCLEO negro absoluto + rim pulsante.
    ///   6.  ANILLO DE BANDAS — mitad DELANTERA (la fórmula del shader).
    ///   7.  BRAZOS ESPIRALES (3) con flujo hacia adentro.
    ///   8.  ANILLO DE FOTONES + corredores.
    ///   9.  ⚡ LIGHTNINGCORE: 3 ARCOS del horizonte + 3 RAYOS fugitivos.
    ///   10. TRIPLE CÍRCULO DE RUNAS (interior CW + exterior CCW + íntimo CW).
    ///   11. JETS POLARES + rayo interno.
    ///   12. VOLUTAS cayendo (2 BrumaFX.Tendril — la materia se disuelve).
    ///   13. ONDAS DE DISTORSIÓN ×3.
    ///   14. REPINTADO del disco negro (el vacío devora el derrame).
    ///   15. AURA FINAL GRANDE pulsante + contrarroto.
    ///
    /// ANIMACIÓN (v6.18): TRES VELOCIDADES DE TIEMPO — lento (humo/runas/
    /// nebulosas), medio (disco/espirales/ondas) y rápido (rayos/fotones/
    /// bandas). Todo con el `time` del llamador: determinista, cero
    /// estado propio, cero red.
    ///
    /// CONTRATO DE BATCH (v6.10, a prueba de balas): Draw() exige el
    /// SpriteBatch CERRADO y lo deja CERRADO.
    /// </summary>
    public static class SupremoBlackHoleRenderer
    {
        // ==================================================================
        //  PARÁMETROS
        // ==================================================================

        /// <summary>Radio de la esfera negra en px a escala 1 — EL MÁS GRANDE DEL MOD.</summary>
        public const float SpherePx = 55f;

        // --- EL DISCO DOPPLER (herencia UMBRAL): oblicuo y fino ---
        private const float DiskA = 2.50f;      // semieje mayor (×R)
        private const float DiskB = 0.92f;      // semieje menor (×R) — oblicuo
        private const float DiskTilt = -0.22f;  // rad — inclinación

        /// <summary>Ángulo del BEAMING DOPPLER: aquí el extremo que se
        /// ACERCA (blanco-oro) vive abajo-izquierda; el opuesto se aleja
        /// (carmesí profundo).</summary>
        private const float DopplerAngle = 3.05f;

        /// <summary>Deriva de las estrías por el disco (rad/s).</summary>
        private const float StreakFlow = 0.44f;

        /// <summary>Estrías pintadas por mitad del disco.</summary>
        private const int StreaksPerHalf = 24;

        /// <summary>Regeneración de la turbulencia (Hz).</summary>
        private const float FlickHz = 12f;

        // --- EL ANILLO DE BANDAS (herencia CÓSMICO): la fórmula del shader ---
        private const float BandA = 1.95f;      // semieje mayor (×R)
        private const float BandB = 1.24f;      // semieje menor (×R)
        private const float BandTilt = -0.38f;  // rad — cruza el disco en X

        /// <summary>LA FÓRMULA DEL SHADER: glow = sin(θ·20 + t·5)·0.5+0.5.</summary>
        private const float BandFreq = 20f;
        private const float BandSpeed = 5f;

        /// <summary>Rotación del anillo (20°/s del script Unity original).</summary>
        private const float BandSpin = 0.349f;

        /// <summary>Segmentos de cada mitad del anillo de bandas.</summary>
        private const int BandSegments = 44;

        // --- LOS BRAZOS ESPIRALES (herencia OLVIDO) ---
        private const int ArmCount = 3;
        private const int ArmSteps = 13;

        // --- EL TRIPLE CÍRCULO DE RUNAS (v6.23: el íntimo NUEVO).
        //     v6.80 — nombres NEUTROS (la fusión): la corona INTERIOR ya no
        //     es «dorada» por definición — el color lo pone el PERFIL (el
        //     Aurora la pinta azul). Valores IDÉNTICOS a siempre. ---
        private const int InnerRuneCount = 8;     // interior, CW
        private const float InnerRuneRadius = 2.62f;   // ×R
        private const float InnerRuneOrbit = 0.10f;    // rad/s
        private const int OuterRuneCount = 6;   // exterior, CCW
        private const float OuterRuneRadius = 3.30f; // ×R — MÁS AFUERA
        private const float OuterRuneOrbit = -0.075f; // rad/s — CONTRARROTO
        // --- v6.23: EL TERCER CÍRCULO — el MÁS ÍNTIMO (entre el anillo de
        //     fotones y el interior) girando rápido ---
        private const int IntimateRuneCount = 6;      // íntimas, CW rápido
        private const float IntimateRuneRadius = 2.02f;  // ×R — MÁS ADENTRO
        private const float IntimateRuneOrbit = 0.16f;   // rad/s — el círculo vivo

        // --- LOS RAYOS (LA ESTRELLA — StormLib) ---
        private const float BoltHz = 10f;        // ~10 Hz de parpadeo vivo

        // --- las volutas que caen al núcleo (herencia BRUMA) ---
        private const int TendrilCount = 2;
        private const float TendrilStart = 2.95f;  // ×R — donde nace
        private const float TendrilSweep = 2.2f;   // rad de barrido espiral

        // --- ondas de distorsión ---
        private const float WaveCycle = 2.5f;
        private const int WaveCount = 3;

        // ==================================================================
        //  LOS DOS PERFILES DE LA CASA (v6.80 — LA FUSIÓN)
        // ==================================================================

        /// <summary>
        /// EL SUPREMO CLÁSICO — oro/carmesí DOMINA, violeta/azul ACENTÚA:
        /// el perfil de siempre, número por número (blanco-incandescente
        /// (255,248,235) → oro (255,190,80) → carmesí (255,90,40) → violeta
        /// (150,80,255) → azul (60,80,220)). Gradiente de DOS paradas
        /// (split 1.0 — la tercera jamás se alcanza) y estilo TÉRMICO.
        /// </summary>
        public static readonly PerfilSupremo Clasico = new PerfilSupremo(
            grad0: new Color(150, 80, 255), grad1: new Color(60, 80, 220),
            grad2: new Color(60, 80, 220), gradSplit: 1.00f, gradienteEnTodo: false,
            whiteIncan: new Color(255, 248, 235), hot: new Color(255, 190, 80),
            mid: new Color(255, 90, 40), mid2: new Color(255, 90, 40),
            lerpDest: new Color(255, 90, 40), deep: new Color(140, 20, 30),
            tintFrio: Color.Lerp(new Color(140, 20, 30), new Color(150, 80, 255), 0.45f),
            acento: new Color(150, 80, 255), auraFinal: new Color(255, 190, 80),
            nubeInt: new Color(190, 130, 40), nubeExt: new Color(150, 80, 255),
            nebPar: new Color(190, 130, 40), nebImpar: new Color(80, 50, 170),
            warmWhite: new Color(255, 225, 175), auraOscura: new Color(10, 5, 2, 178),
            polvo1: new Color(200, 130, 35), polvo2: new Color(190, 60, 30),
            polvo3: new Color(110, 70, 220),
            runeInBody: new Color(255, 180, 70), runeInTip: new Color(255, 235, 175),
            runeOutBody: new Color(110, 130, 255), runeOutTip: new Color(205, 220, 255),
            runeIntBody: new Color(255, 245, 220), runeIntTip: new Color(255, 252, 240),
            runnerA: new Color(150, 80, 255), runnerB: new Color(255, 190, 80),
            runnerC: new Color(255, 90, 40),
            boltHot: new Color(255, 90, 40), boltFrio: new Color(255, 190, 80),
            jetDownHot: new Color(225, 235, 255),
            tendrilPar: new Color(190, 130, 50), tendrilImpar: new Color(150, 80, 255),
            onda0: new Color(255, 215, 140), onda1: new Color(185, 160, 255),
            onda2: new Color(255, 240, 210));

        /// <summary>
        /// EL AURORA — el ex-clon entero, hoy 39 números: el GRADIENTE
        /// AURORA de 3 paradas (morado (185,105,255) → azul (92,150,255)
        /// @0.45 → dorado (255,195,90)) reina en TODO lo que se desenrolla
        /// (brazos, banda RADIAL, jets, ondas por FASE); rim morado; runas
        /// azul/oro/morado; blanco frío (238,242,255). Los slots Onda0-2 no
        /// se usan (el estilo gradiente colorea las ondas por fase).
        /// </summary>
        public static readonly PerfilSupremo Aurora = new PerfilSupremo(
            grad0: new Color(185, 105, 255), grad1: new Color(92, 150, 255),
            grad2: new Color(255, 195, 90), gradSplit: 0.45f, gradienteEnTodo: true,
            whiteIncan: new Color(238, 242, 255), hot: new Color(185, 105, 255),
            mid: Color.Lerp(new Color(185, 105, 255), new Color(92, 150, 255), 0.50f),
            mid2: Color.Lerp(new Color(185, 105, 255), new Color(92, 150, 255), 0.45f),
            lerpDest: new Color(108, 58, 190), deep: new Color(108, 58, 190),
            tintFrio: new Color(92, 150, 255),
            acento: new Color(92, 150, 255), auraFinal: new Color(255, 195, 90),
            nubeInt: new Color(185, 105, 255), nubeExt: new Color(255, 195, 90),
            nebPar: new Color(98, 62, 188), nebImpar: new Color(182, 132, 52),
            warmWhite: new Color(240, 245, 255), auraOscura: new Color(6, 6, 16, 178),
            polvo1: new Color(202, 138, 48), polvo2: new Color(92, 150, 255),
            polvo3: new Color(168, 100, 250),
            runeInBody: new Color(125, 165, 255), runeInTip: new Color(222, 234, 255),
            runeOutBody: new Color(255, 185, 75), runeOutTip: new Color(255, 240, 190),
            runeIntBody: new Color(185, 130, 255), runeIntTip: new Color(235, 215, 255),
            runnerA: new Color(185, 105, 255), runnerB: new Color(255, 195, 90),
            runnerC: new Color(92, 150, 255),
            boltHot: new Color(92, 150, 255), boltFrio: new Color(255, 195, 90),
            jetDownHot: new Color(238, 242, 255),
            tendrilPar: new Color(202, 142, 55), tendrilImpar: new Color(185, 105, 255),
            onda0: new Color(240, 245, 255), onda1: new Color(240, 245, 255),
            onda2: new Color(240, 245, 255));

        /// <summary>
        /// EL PERFIL ACTIVO (v6.80): lo fija Draw() al entrar — los 20
        /// métodos privados lo leen como <c>_p.Slot</c>. Seguro: el render
        /// es secuencial por frame y CADA Draw() lo re-fija de entrada.
        /// </summary>
        private static PerfilSupremo _p;

        // ==================================================================
        //  PINCELES (los tres genéricos de la biblioteca VFX)
        // ==================================================================

        private static Asset<Texture2D> _glow;
        private static Asset<Texture2D> _ring;
        private static Asset<Texture2D> _disk;

        private static Texture2D Glow =>
            (_glow ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow")).Value;

        private static Texture2D Ring =>
            (_ring ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Ring")).Value;

        private static Texture2D Disk =>
            (_disk ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/BlackDisk")).Value;

        // ==================================================================
        //  HELPERS DE DIBUJO
        // ==================================================================

        private static void BeginAdditive()
        {
            // v6.56.9 — delegación a la canónica CON SONDA (estándar
            // v6.50.58): idéntico sin lote ajeno abierto (auditoría §4).
            VFXCore.AbrirAdditive();
        }

        private static void BeginAlpha()
        {
            // v6.56.9 — delegación a la canónica CON SONDA (estándar
            // v6.50.58): idéntico sin lote ajeno abierto (auditoría §4).
            VFXCore.AbrirAlpha();
        }

        /// <summary>Quad centrado (tamaño total = size px), rotación opcional.</summary>
        private static void Quad(Texture2D tex, Vector2 pos, Vector2 size, float rot, Color tint)
        {
            if (tint.A == 0) return;
            Main.spriteBatch.Draw(tex, pos, null, tint, rot,
                new Vector2(tex.Width, tex.Height) * 0.5f,
                size / new Vector2(tex.Width, tex.Height),
                SpriteEffects.None, 0f);
        }

        /// <summary>Cápsula de luz: SoftGlow estirado (largo × ancho px).</summary>
        private static void Capsule(Vector2 mid, float len, float width, float rot, Color tint)
        {
            // v6.56.9 — delegación a la canónica (auditoría §4).
            VFXCore.Capsule(mid, len, width, rot, tint);
        }

        /// <summary>Anillo fino: Ring.png calibrado para radio visible.</summary>
        private static void RingQuad(Vector2 pos, float visibleRadius, float rot, Color tint)
        {
            // v6.56.9 — delegación a la canónica (auditoría §4).
            VFXCore.RingQuad(pos, visibleRadius, rot, tint);
        }

        /// <summary>Hash determinista [0,1).</summary>
        private static float Hash01(int seed, int a, int b)
            => VFXCore.Hash01(seed, a, b); // v6.56.9 — delegación a la canónica (auditoría §4)

        /// <summary>Tinte de INTENSIDAD LINEAL (patrón validado del Cometa).</summary>
        private static Color Tint(Color c, float f)
        {
            // v6.56.9 — delegación a la canónica (auditoría §4): familia
            // ALPHA-GATE (RGB intacto, alfa = intensidad).
            return VFXCore.TintAlfa(c, f);
        }

        /// <summary>Punto sobre la elipse del DISCO DOPPLER (param t en rad).</summary>
        private static Vector2 DiskPoint(Vector2 c, float r, float t)
            => VFXCore.Ellipse(c, DiskA * r, DiskB * r, DiskTilt, t);

        /// <summary>Punto sobre la elipse del ANILLO DE BANDAS (param t en rad).</summary>
        private static Vector2 BandPoint(Vector2 c, float r, float t)
            => VFXCore.Ellipse(c, BandA * r, BandB * r, BandTilt, t);

        /// <summary>
        /// EL BEAMING DOPPLER del disco (herencia Umbral): cúbico — el lado
        /// que se ACERCA arde a blanco-oro, el que se ALEJA muere en carmesí.
        /// </summary>
        private static float Doppler(float t)
        {
            float d = 0.5f + 0.5f * (float)Math.Cos(t - DopplerAngle);
            return d * d * d;
        }

        /// <summary>
        /// v6.80 — EL GRADIENTE DE 3 PARADAS del perfil (el «AuroraGrad»
        /// original, unificado): t &lt; split → parada0→parada1; después →
        /// parada1→parada2. Con split 1.0 es un gradiente de DOS paradas
        /// (el Clásico violeta→azul de los brazos espirales).
        /// </summary>
        private static Color Grad(float t)
        {
            t = MathHelper.Clamp(t, 0f, 1f);
            if (t < _p.GradSplit)
                return Color.Lerp(_p.Grad0, _p.Grad1,
                    _p.GradSplit <= 0.0001f ? 1f : t / _p.GradSplit);
            float restante = 1f - _p.GradSplit;
            return Color.Lerp(_p.Grad1, _p.Grad2,
                restante <= 0.0001f ? 1f : (t - _p.GradSplit) / restante);
        }

        // ==================================================================
        //  EL RENDER COMPLETO — contrato: batch CERRADO → CERRADO
        // ==================================================================

        /// <summary>
        /// EL DRAW HISTÓRICO (perfil <see cref="Clasico"/>, el supremo
        /// dorado de siempre) — firma intacta desde siempre: los 6 callsites
        /// del proyectil Supremo no se tocan.
        /// </summary>
        public static void Draw(Vector2 center, float scale, float time, int seed)
            => Draw(center, scale, time, seed, Clasico);

        /// <summary>
        /// v6.80 — EL DRAW PARAMETRIZADO: el mismo motor, el perfil manda
        /// (el shim del Aurora llama aquí con <see cref="Aurora"/>).
        /// </summary>
        public static void Draw(Vector2 center, float scale, float time, int seed,
            PerfilSupremo perfil)
        {
            _p = perfil;   // el perfil manda TODO el render (ver _p)
            float r = SpherePx * Math.Max(scale, 0.02f);
            if (r < 2f) return;   // el batch queda INTACTO (cerrado)

            try
            {
                // ============================================================
                //  LAS TRES VELOCIDADES DE TIEMPO (v6.18 — animación mejorada)
                //  · tSlow: humo, runas, nebulosas — la caldera REGIA.
                //  · tMid:  disco, espirales, ondas — la materia en caída.
                //  · tFast: rayos, fotones, bandas — la furia eléctrica.
                // ============================================================
                float tSlow = time * 0.55f;
                float tMid = time;
                float tFast = time * 1.55f;

                int flick = (int)(tMid * FlickHz);
                int boltFlick = StormLib.FlickTick(time, BoltHz);   // ~10 Hz

                float breathe = 1f + 0.012f * (float)Math.Sin(tMid * 1.15f);
                float rr = r * breathe;

                // ============ 1. AURA OSCURA (alfa) — absorbe la luz ============
                BeginAlpha();
                Quad(Glow, center, new Vector2(7.6f * rr, 7.6f * rr), 0f,
                    _p.AuraOscura);
                Main.spriteBatch.End();

                // ============ 2..15: TODO LO BRILLANTE (aditivo) ============
                BeginAdditive();

                // --- 2. NEBULOSAS + polvo ambiental ---
                DrawNebulas(center, rr, tSlow, seed);

                // --- 3. EL HALO DE HUMO — 2 Cloud de la LIBRERÍA DE BRUMA ---
                // (giran LENTO: la interior cálida, la exterior el acento —
                // el aliento del agujero abraza el conjunto entero).
                BrumaFX.Cloud(center, 2.7f * rr, _p.NubeInt, seed + 11, tSlow * 0.8f,
                    puffs: 5, alpha: 0.26f);
                BrumaFX.Cloud(center, 3.4f * rr, _p.NubeExt, seed + 47, tSlow * 0.6f,
                    puffs: 4, alpha: 0.17f);

                // --- 4. EL DISCO DOPPLER — mitad TRASERA (estrías pintadas) ---
                DrawDopplerDisk(center, rr, tMid, seed, flick, front: false);

                // --- 4b. ECO TENUE del anillo de bandas (mitad trasera) ---
                DrawBandRing(center, rr, tFast, seed, flick, front: false);

                // --- 5. NÚCLEO negro absoluto + rim pulsante ---
                Main.spriteBatch.End();
                BeginAlpha();
                Quad(Disk, center, new Vector2(2.28f * r, 2.28f * r), 0f, Color.White);
                Main.spriteBatch.End();
                BeginAdditive();
                RingQuad(center, 1.02f * r, tMid * 0.13f,
                    Tint(_p.Hot, 0.42f + 0.14f * (float)Math.Sin(tMid * 1.6f)));

                // --- 6. EL ANILLO DE BANDAS — mitad DELANTERA (la fórmula) ---
                DrawBandRing(center, rr, tFast, seed, flick, front: true);

                // --- 7. BRAZOS ESPIRALES con flujo hacia adentro ---
                DrawSpiralArms(center, rr, tMid, seed);

                // --- 8. ANILLO DE FOTONES + corredores ---
                RingQuad(center, 1.10f * r, tFast * 0.2f,
                    Tint(_p.WarmWhite, 0.20f + 0.08f * (float)Math.Sin(tFast * 2.1f)));
                DrawPhotonRunners(center, rr, tFast, seed);

                // --- 9. ⚡ LIGHTNINGCORE — LA CORONA DE DESCARGA ---
                DrawHorizonArcs(center, r, time, seed, boltFlick);
                DrawEscapingBolts(center, rr, tFast, seed, boltFlick);

                // --- 10. TRIPLE CÍRCULO DE RUNAS (CW + CCW + CW íntimo) ---
                DrawRuneCircles(center, r, tSlow, seed);

                // --- 11. JETS POLARES (el perfil decide el reparto) ---
                DrawPolarJets(center, rr, tMid, seed, boltFlick);

                // --- 12. VOLUTAS cayendo — la materia se disuelve ---
                DrawFallingTendrils(center, r, tSlow, seed);

                // --- 13. ONDAS DE DISTORSIÓN ×3 ---
                DrawDistortionWaves(center, r, tMid, seed);

                // --- 14. EL VACÍO VUELVE A DEVORAR: repintado del disco ---
                // (cualquier derrame aditivo sobre el horizonte se borra y
                // el centro queda NEGRO ABSOLUTO — el agujero se lo come).
                Main.spriteBatch.End();
                BeginAlpha();
                Quad(Disk, center, new Vector2(2.28f * r, 2.28f * r), 0f, Color.White);
                Main.spriteBatch.End();
                BeginAdditive();
                // El filo del horizonte, respirando (la última luz).
                RingQuad(center, 1.02f * r, tMid * 0.13f,
                    Tint(_p.Hot, 0.30f + 0.10f * (float)Math.Sin(tMid * 1.6f)));

                // --- 15. AURA FINAL GRANDE pulsante ---
                float aura = 0.85f + 0.15f * (float)Math.Sin(tFast * 1.4f);
                Quad(Glow, center, new Vector2(6.6f * rr, 6.6f * rr), 0f,
                    Tint(_p.AuraFinal, 0.20f * aura));
                // El CONTRARROTO: el eco arcano de la fusión.
                RingQuad(center, 3.1f * rr, -tMid * 0.08f,
                    Tint(_p.Acento, 0.10f * aura));

                Main.spriteBatch.End();
                // El batch queda CERRADO (contrato).
            }
            catch
            {
                // Cierre defensivo SOLO en el path de error (v6.10).
                VFXCore.CerrarLoteSiAbierto();
            }
        }

        // ------------------------------------------------------------------
        //  2. NEBULOSAS + POLVO AMBIENTAL — el par/impar del perfil
        // ------------------------------------------------------------------

        private static void DrawNebulas(Vector2 center, float rr, float time, int seed)
        {
            // SEIS velos ENORMES y tenues: el par/impar del perfil (la
            // unión de las dos estirpes fundidas).
            for (int i = 0; i < 6; i++)
            {
                float h = Hash01(seed, 501 + i, 17);
                float dir = i % 2 == 0 ? 1f : -1f;
                float ang = h * MathHelper.TwoPi + time * 0.05f * dir;
                float dist = (2.8f + 0.8f * Hash01(seed, 502 + i, 29)) * rr;
                Vector2 pos = center + new Vector2(
                    (float)Math.Cos(ang) * dist, (float)Math.Sin(ang) * dist * 0.8f);
                float size = (2.0f + 1.1f * Hash01(seed, 503 + i, 41)) * rr;
                Color c = i % 2 == 0 ? _p.NebPar : _p.NebImpar;
                float pulse = 0.75f + 0.25f * (float)Math.Sin(time * 0.7f + i * 1.9f);
                Quad(Glow, pos, new Vector2(size, size), ang,
                    Tint(c, (i % 2 == 0 ? 0.20f : 0.14f) * pulse));
            }

            // Polvo ambiental: brasas y chispas pulsando (la terna del perfil).
            for (int i = 0; i < 14; i++)
            {
                float h = Hash01(seed, 600 + i, 13);
                float ang = h * MathHelper.TwoPi + time * 0.03f * (i % 2 == 0 ? 1f : -1f);
                float dist = (2.2f + 3.1f * Hash01(seed, 601 + i, 19)) * rr * 0.55f;
                Vector2 pos = center + new Vector2(
                    (float)Math.Cos(ang) * dist, (float)Math.Sin(ang) * dist);
                float pulse = 0.5f + 0.5f * (float)Math.Sin(time * 1.5f + i * 2.4f);
                Color c = h < 0.40f ? _p.Polvo1
                        : h < 0.75f ? _p.Polvo2
                        : _p.Polvo3;
                float size = (0.10f + 0.10f * h) * rr;
                Quad(Glow, pos, new Vector2(size, size), 0f, Tint(c, 0.45f * pulse));
            }
        }

        // ------------------------------------------------------------------
        //  4. EL DISCO DOPPLER — estrías pintadas (herencia UMBRAL),
        //      el lado que ACERCA arde → el que ALEJA muere en el Deep
        // ------------------------------------------------------------------

        private static void DrawDopplerDisk(Vector2 center, float rr, float time, int seed,
            int flick, bool front)
        {
            // v6.80 — EL TECHO DE CALIDAD (la auditoría v6.78 §4.3: los 10
            // agujeros no consultaban NADA): el disco es el método MÁS
            // CARO del renderer (~220 quads: 24 estrías × 3 segs × 2
            // cápsulas + bandas media/externa + picos) — si el frame va
            // apretado (3 jefes con bruma), él es el PRIMERO en saltarse.
            if (!VFXCore.Presupuesto(220)) return;

            // Solo se pinta la MITAD TRASERA: la delantera la cruza el
            // anillo de bandas (la doble geometría del supremo).
            float t0 = MathHelper.Pi;
            float span = MathHelper.Pi;
            float bright = front ? 1.25f : 0.58f;

            // Las estrías DERIVAN por el disco (la pintura fluye).
            float flow = time * StreakFlow;

            for (int s = 0; s < StreaksPerHalf; s++)
            {
                // Nacimiento por hash + deriva: cada estría nace y VIAJA.
                float h0 = Hash01(seed, 700 + s, flick / 2);
                float a0 = t0 + ((h0 + flow / MathHelper.TwoPi) % 1f) * span;

                // JITTER RADIAL: cada estría respira a su radio — el plasma
                // distorsionado, NO una elipse geométrica perfecta.
                float radialJit = 0.94f + 0.14f * Hash01(seed, 703 + s, 5);

                // DOPPLER en el punto medio de la estría.
                float midT = a0 + 0.10f;
                float dop = Doppler(midT);

                // LARGO: pinceladas EXTENSAS en el lado caliente.
                float arcLen = (0.22f + 0.50f * Hash01(seed, 701 + s, flick / 2)) *
                               (0.45f + 0.75f * dop);

                // GROSOR: mayor en el lado CALIENTE (el disco engorda ardiendo).
                float wBase = (0.10f + 0.16f * dop) * rr;

                // TURBULENCIA de brillo (la pintura VIVE).
                float turb = 0.70f + 0.30f * Hash01(seed, 702 + s, flick);

                // --- BANDA INTERNA CALIENTE (3 cápsulas por estría) ---
                for (int seg = 0; seg < 3; seg++)
                {
                    float ta = a0 + arcLen * seg / 3f;
                    float tb = a0 + arcLen * (seg + 1) / 3f;
                    Vector2 pa = DiskPoint(center, rr * radialJit, ta);
                    Vector2 pb = DiskPoint(center, rr * radialJit, tb);
                    Vector2 mid = (pa + pb) * 0.5f;
                    Vector2 d = pb - pa;
                    float len = d.Length();
                    if (len < 0.5f) continue;
                    float rot = (float)Math.Atan2(d.Y, d.X);

                    // Color del núcleo por Doppler: el pico BLANCO →
                    // Hot → Mid → Deep (la escalera térmica del perfil).
                    float inten = (0.25f + 0.75f * dop) * turb * bright;
                    Color core;
                    if (dop > 0.62f) core = _p.WhiteIncan;
                    else if (dop > 0.30f) core = _p.Hot;
                    else if (dop > 0.10f) core = _p.Mid;
                    else core = _p.Deep;

                    Capsule(mid, len, wBase * 2.1f, rot,
                        Tint(Color.Lerp(core, _p.LerpDest, 0.25f), 0.55f * inten));
                    Capsule(mid, len, wBase, rot, Tint(core, 1.0f * inten));

                    // Punto BLANCO donde la turbulencia ARDE.
                    if (turb > 0.82f && inten > 0.28f)
                    {
                        Quad(Glow, mid, new Vector2(0.34f * rr, 0.34f * rr), rot,
                            Tint(_p.WhiteIncan, 0.95f * inten * (turb - 0.82f) / 0.18f));
                    }
                }

                // --- BANDA MEDIA (a 1.10× del anillo) ---
                {
                    float tm = a0 + arcLen * 0.5f;
                    Vector2 pm = DiskPoint(center, rr * 1.10f, tm);
                    Vector2 pn = DiskPoint(center, rr * 1.10f, tm + arcLen * 0.55f);
                    Vector2 mid = (pm + pn) * 0.5f;
                    Vector2 d = pn - pm;
                    float len = d.Length();
                    if (len > 0.5f)
                    {
                        float rot = (float)Math.Atan2(d.Y, d.X);
                        float inten = (0.30f + 0.55f * dop) * turb * bright;
                        Capsule(mid, len, wBase * 1.5f, rot,
                            Tint(_p.Mid2, 0.48f * inten));
                    }
                }

                // --- BANDA EXTERNA desvaneciendo (a 1.24×) — el puente de
                //     color hacia los brazos espirales. ---
                {
                    float tm = a0 + arcLen * 0.4f;
                    Vector2 pm = DiskPoint(center, rr * 1.24f, tm);
                    Vector2 pn = DiskPoint(center, rr * 1.24f, tm + arcLen * 0.45f);
                    Vector2 mid = (pm + pn) * 0.5f;
                    Vector2 d = pn - pm;
                    float len = d.Length();
                    if (len > 0.5f)
                    {
                        float rot = (float)Math.Atan2(d.Y, d.X);
                        float inten = (0.22f + 0.40f * dop) * turb * bright;
                        Capsule(mid, len, wBase * 1.2f, rot,
                            Tint(_p.TintFrio, 0.40f * inten));
                    }
                }
            }
        }

        // ------------------------------------------------------------------
        //  4b/6. EL ANILLO DE BANDAS — LA FÓRMULA DEL SHADER CÓSMICO, FIEL:
        //         glow = sin(θ·20 + t·5)·0.5+0.5 — 20 bandas viajando
        // ------------------------------------------------------------------

        private static void DrawBandRing(Vector2 center, float rr, float time, int seed,
            int flick, bool front)
        {
            // v6.80 — EL TECHO DE CALIDAD (la auditoría v6.78 §4.3): el
            // anillo completo son ~100 quads (44 segmentos × 2 cápsulas +
            // picos) — cosmético puro, salta si el frame va apretado.
            if (!VFXCore.Presupuesto(100)) return;

            // La mitad delantera (t ∈ 0..π) cruza POR DELANTE de la esfera.
            float t0 = front ? 0f : MathHelper.Pi;
            float span = MathHelper.Pi;
            float bright = front ? 1.30f : 0.42f;

            // El anillo ROTA (20°/s del script Unity).
            float spin = time * BandSpin / 1.55f;   // tFast ya acelera el patrón

            for (int s = 0; s < BandSegments; s++)
            {
                float t = t0 + span * (s + 0.5f) / BandSegments;
                float tSpin = t + spin;   // el patrón de bandas gira con el anillo

                // Posición + tangente de la cápsula.
                Vector2 a = BandPoint(center, rr, t - span / (BandSegments * 2f));
                Vector2 b = BandPoint(center, rr, t + span / (BandSegments * 2f));
                Vector2 mid = (a + b) * 0.5f;
                Vector2 seg = b - a;
                float segLen = seg.Length();
                if (segLen < 0.5f) continue;
                float rot = (float)Math.Atan2(seg.Y, seg.X);

                // ============================================================
                //  LA FÓRMULA EXACTA: glow = sin(θ·20 + t·5)·0.5 + 0.5
                //  → VEINTE BANDAS de emisión recorriendo el anillo.
                // ============================================================
                float glow = 0.5f + 0.5f * (float)Math.Sin(tSpin * BandFreq - time * BandSpeed);

                // Turbulencia viva por hash (regenerada a 12 Hz).
                float turb = 0.74f + 0.26f * Hash01(seed, 720 + s, flick);
                float inten = glow * turb * bright;

                // ============================================================
                //  v6.80 — EL COLOR DE LA BANDA (el punto dual nº1):
                //  · ESTILO AURORA: gradiente por DISTANCIA RADIAL del
                //    segmento (nace morado dentro, arde dorado afuera) con
                //    BLANQUEO al 75% en el pico.
                //  · ESTILO CLÁSICO: gradiente TÉRMICO por intensidad
                //    (máximo = blanco incandescente; media = Hot; valle = Mid).
                // ============================================================
                Color c;
                if (_p.GradienteEnTodo)
                {
                    float radial = (mid - center).Length() / rr;   // ~1.24 .. 1.95
                    float gt = MathHelper.Clamp((radial - 1.24f) / 0.71f, 0f, 1f);
                    c = Grad(gt);
                    if (inten > 0.82f)
                        c = Color.Lerp(c, _p.WhiteIncan, 0.75f);
                }
                else
                {
                    if (inten > 0.82f) c = _p.WhiteIncan;
                    else if (inten > 0.45f) c = _p.Hot;
                    else c = _p.Mid;
                }

                // Cápsula HALO (gruesa, tenue) + cápsula NÚCLEO (fina, viva).
                Capsule(mid, segLen, 0.36f * rr * (0.7f + inten), rot, Tint(c, 0.40f * inten));
                Capsule(mid, segLen, 0.12f * rr * inten, rot, Tint(c, 0.85f * inten));

                // En el PICO de cada banda, un punto blanco extra.
                if (inten > 0.86f)
                {
                    Quad(Glow, mid, new Vector2(0.32f * rr, 0.32f * rr), rot,
                        Tint(_p.WhiteIncan, 0.70f * (inten - 0.86f) / 0.14f));
                }
            }
        }

        // ------------------------------------------------------------------
        //  7. BRAZOS ESPIRALES — el vórtice del GRADIENTE se desenrosca,
        //      con FLUJO ANIMADO: puntos que espiralan hacia adentro
        // ------------------------------------------------------------------

        private static void DrawSpiralArms(Vector2 center, float rr, float time, int seed)
        {
            // v6.80 — EL TECHO DE CALIDAD (la auditoría v6.78 §4.3): los 3
            // brazos son ~105 quads (13 pasos × 2 cápsulas + 3 flujos × 3) —
            // cosmético puro, salta si el frame va apretado.
            if (!VFXCore.Presupuesto(105)) return;

            float armPhase = time * 0.16f;

            for (int arm = 0; arm < ArmCount; arm++)
            {
                float baseT = armPhase + arm * (MathHelper.TwoPi / ArmCount);

                // --- LA MASA DEL BRAZO: cápsulas desenroscándose ---
                for (int k = 0; k < ArmSteps; k++)
                {
                    float f = k / (float)(ArmSteps - 1);           // 0 en el anillo → 1 fuera
                    float t = baseT + f * 1.5f;                    // barrido angular
                    float grow = 1.10f + f * 1.05f;                // radio creciente
                    Vector2 pos = VFXCore.Ellipse(center,
                        BandA * rr * grow, BandB * rr * grow, BandTilt, t);
                    Vector2 next = VFXCore.Ellipse(center,
                        BandA * rr * grow, BandB * rr * grow, BandTilt,
                        t + 1.5f / ArmSteps);
                    Vector2 mid = (pos + next) * 0.5f;
                    Vector2 seg = next - pos;
                    float len = seg.Length();
                    if (len < 0.5f) continue;
                    float rot = (float)Math.Atan2(seg.Y, seg.X);

                    float fade = (float)Math.Pow(1f - f, 1.5);     // se apaga suave
                    // v6.80 — EL COLOR DEL BRAZO = EL GRADIENTE DEL PERFIL
                    // (Clásico: violeta→azul de 2 paradas; Aurora: el de 3).
                    Color c = Grad(f);
                    Capsule(mid, len, (0.34f - 0.20f * f) * rr, rot, Tint(c, 0.40f * fade));
                    Capsule(mid, len, (0.11f - 0.07f * f) * rr, rot, Tint(c, 0.60f * fade));
                }

                // --- EL FLUJO HACIA ADENTRO: 3 puntos por brazo que
                //     ESPIRALAN del exterior al anillo (la caída viva). ---
                for (int m = 0; m < 3; m++)
                {
                    float h = Hash01(seed, 810 + arm * 9 + m, 37);
                    float life = (time * (0.30f + 0.10f * m) + h + arm * 0.33f) % 1f;
                    float f = 1f - life;   // nace en la PUNTA y CAE al anillo

                    float t = baseT + f * 1.5f;
                    float grow = 1.10f + f * 1.05f;
                    Vector2 pos = VFXCore.Ellipse(center,
                        BandA * rr * grow, BandB * rr * grow, BandTilt, t);

                    // Estela: el punto de hace un instante (más afuera).
                    Vector2 tail = VFXCore.Ellipse(center,
                        BandA * rr * grow, BandB * rr * grow, BandTilt,
                        t - 0.10f * life + 0.045f);
                    Vector2 mid = (pos + tail) * 0.5f;
                    Vector2 seg = pos - tail;
                    float len = seg.Length();
                    if (len > 0.5f)
                    {
                        float rot = (float)Math.Atan2(seg.Y, seg.X);
                        Capsule(mid, len, 0.10f * rr, rot,
                            Tint(_p.Acento, 0.45f * (float)Math.Sin(life * Math.PI)));
                    }

                    float alpha = (float)Math.Sin(life * Math.PI) * (0.6f + 0.4f * h);
                    Quad(Glow, pos, new Vector2(0.30f * rr, 0.30f * rr), 0f,
                        Tint(Color.Lerp(_p.WhiteIncan, _p.Acento, 0.5f), alpha * 0.8f));
                    Quad(Glow, pos, new Vector2(0.12f * rr, 0.12f * rr), 0f,
                        Tint(_p.WhiteIncan, alpha));
                }
            }
        }

        // ------------------------------------------------------------------
        //  8. CORREDORES DE FOTONES — luz orbitando el vórtice
        // ------------------------------------------------------------------

        private static void DrawPhotonRunners(Vector2 center, float rr, float time, int seed)
        {
            for (int i = 0; i < 4; i++)
            {
                // Orbitan con la rotación del anillo más su propia velocidad.
                float t = time * (BandSpin + 0.55f + 0.20f * i) + i * 1.6f;
                Vector2 pos = BandPoint(center, rr, t);
                float twinkle = 0.65f + 0.35f * (float)Math.Sin(time * 8f + i * 2.3f);

                // Estela corta detrás del fotón.
                Vector2 behind = BandPoint(center, rr, t - 0.22f);
                Vector2 seg = pos - behind;
                float len = seg.Length();
                if (len > 0.5f)
                {
                    float rot = (float)Math.Atan2(seg.Y, seg.X);
                    Vector2 mid = (pos + behind) * 0.5f;
                    Color trailC = i % 3 == 0 ? _p.RunnerA : _p.RunnerB;
                    Capsule(mid, len, 0.14f * rr, rot, Tint(trailC, 0.40f * twinkle));
                }

                // halo + núcleo blanco (la terna cíclica del perfil).
                Color c = i % 3 == 0 ? _p.RunnerA : i % 3 == 1 ? _p.RunnerB : _p.RunnerC;
                Quad(Glow, pos, new Vector2(1.05f * rr, 1.05f * rr), 0f,
                    Tint(c, 0.35f * twinkle));
                Quad(Glow, pos, new Vector2(0.42f * rr, 0.42f * rr), 0f,
                    Tint(_p.WhiteIncan, 0.85f * twinkle));
            }
        }

        // ------------------------------------------------------------------
        //  9a. ⚡ LOS ARCOS DEL HORIZONTE — StormLib.Arc, ~10 Hz
        // ------------------------------------------------------------------

        private static void DrawHorizonArcs(Vector2 center, float r, float time, int seed,
            int boltFlick)
        {
            // TRES CORONAS DE DESCARGA alrededor del horizonte: cada arco
            // recorre un trozo de círculo a radio ligeramente distinto,
            // girando a velocidades distintas — PARPADEANDO a ~10 Hz.
            for (int i = 0; i < 3; i++)
            {
                if (!StormLib.IsLit(seed + 40 + i * 17, boltFlick, 0.82f))
                    continue;

                float span = 1.15f + 0.55f * Hash01(seed, 920 + i, boltFlick / 4);
                float baseA = time * (0.55f + 0.22f * i) * (i % 2 == 0 ? 1f : -1f)
                              + i * 2.1f;
                float radius = r * (1.05f + 0.08f * i);

                StormLib.ArcRing(Main.spriteBatch, center, radius,
                    baseA, baseA + span,
                    seed + 40 + i * 17, boltFlick,
                    Math.Max(2.6f, 0.052f * r),
                    Tint(_p.Hot, 0.52f),           // funda del perfil
                    Tint(_p.WhiteIncan, 0.95f),    // núcleo blanco-incandescente
                    1f, 9);
            }
        }

        // ------------------------------------------------------------------
        //  9b. ⚡ LOS RAYOS FUGITIVOS — StormLib.Bolt (2 Hot + 1 Frío)
        // ------------------------------------------------------------------

        private static void DrawEscapingBolts(Vector2 center, float rr, float time, int seed,
            int boltFlick)
        {
            // TRES RAYOS escapando del ANILLO DE BANDAS (dobles tiras
            // cuerpo/núcleo con ramas — la librería nueva del proyecto).
            for (int i = 0; i < 3; i++)
            {
                if (!StormLib.IsLit(seed + 61 + i, boltFlick, 0.85f))
                    continue;

                // Emergen donde la banda del shader está EN SU PICO.
                float bandPeak = -time * BandSpeed / BandFreq + i * 2.1f;
                float t = bandPeak + 0.5f * Hash01(seed, 860 + i, boltFlick / 3);
                Vector2 start = BandPoint(center, rr, t);
                Vector2 outward = start - center;
                if (outward.LengthSquared() < 0.01f) continue;
                outward.Normalize();
                // Mezcla radial + tangencial: el rayo ESCAPA girando.
                Vector2 tangent = new Vector2(-outward.Y, outward.X) * 0.40f;
                Vector2 end = start + (outward + tangent) * (1.35f + 0.60f *
                    Hash01(seed, 861 + i, boltFlick)) * rr;

                // 2 Hot + 1 Frío (el reparto del perfil).
                Color haloC = i < 2 ? _p.BoltHot : _p.BoltFrio;
                StormLib.Bolt(Main.spriteBatch, start, end,
                    seed + 130 + i * 53, boltFlick,
                    Math.Max(3f, 0.085f * rr),
                    Tint(haloC, 0.58f), Tint(_p.WhiteIncan, 0.95f),
                    1f, 7, Math.Max(10f, 0.17f * rr));
            }
        }

        // ------------------------------------------------------------------
        //  10. EL TRIPLE CÍRCULO DE RUNAS — interior CW + exterior CCW
        //      + íntimas CW rápido (v6.23 — el tercer anillo)
        //      v6.37 — delega en OrbitaLib (la librería de los anillos
        //      rúnicos): ni un número cambiado.
        // ------------------------------------------------------------------

        /// <summary>
        /// v6.37 — delega en OrbitaLib (la librería de los anillos
        /// rúnicos): ni un número cambiado. Cada corona es UNA llamada a
        /// `OrbitaLib.CirculoRunico` con SUS constantes de siempre — el
        /// aro fino de pauta, las runas DE PIE (resplandor, trazos de
        /// cápsula con gradiente, perla latiendo) y la flotación viva
        /// viven AHORA en la primitiva. El alfabeto S (la tabla `_runes`
        /// de siempre, idéntica byte a byte) es el default de la
        /// librería: OrbitaLib.RunasVacio. v6.80: los TRES pares de
        /// color vienen del perfil (los nombres de las coronas son
        /// neutros — la fusión).
        /// </summary>
        private static void DrawRuneCircles(Vector2 center, float r, float time, int seed)
        {
            float glyphScale = Math.Max(r / 52f, 0.25f) * 1.40f;

            // ============================================================
            //  EL CÍRCULO INTERIOR — 8 runas girando CW (con el conjunto)
            // ============================================================
            OrbitaLib.CirculoRunico(center, r, time, InnerRuneRadius, InnerRuneCount,
                InnerRuneOrbit, _p.RuneInBody, _p.RuneInTip, glyphScale,
                offset: 0, ringAlpha: 0.24f);

            // ============================================================
            //  EL CÍRCULO EXTERIOR — 6 runas MÁS AFUERA girando CCW
            //  (el contrarroto arcano de la fusión)
            // ============================================================
            OrbitaLib.CirculoRunico(center, r, time, OuterRuneRadius, OuterRuneCount,
                OuterRuneOrbit, _p.RuneOutBody, _p.RuneOutTip, glyphScale * 0.85f,
                offset: 3, ringAlpha: 0.18f);

            // ============================================================
            //  EL CÍRCULO ÍNTIMO — 6 runas MÁS ADENTRO, rápido e íntimo
            //  (v6.23: entre el anillo de fotones y el interior — la tercera
            //  corona del conjuro, la más cercana al abismo)
            // ============================================================
            OrbitaLib.CirculoRunico(center, r, time, IntimateRuneRadius, IntimateRuneCount,
                IntimateRuneOrbit, _p.RuneIntBody, _p.RuneIntTip, glyphScale * 0.92f,
                offset: 6, ringAlpha: 0.20f);
        }

        // ------------------------------------------------------------------
        //  11. JETS POLARES — el reparto del perfil, cada uno con un
        //      RAYO StormLib DENTRO
        // ------------------------------------------------------------------

        private static void DrawPolarJets(Vector2 center, float rr, float time,
            int seed, int boltFlick)
        {
            // Eje menor del anillo de bandas (los "polos" del vórtice).
            Vector2 pole = new Vector2(
                -(float)Math.Sin(BandTilt + MathHelper.PiOver2),
                (float)Math.Cos(BandTilt + MathHelper.PiOver2));
            float pulse = 0.7f + 0.3f * (float)Math.Sin(time * 1.9f);

            for (int side = 0; side < 2; side++)
            {
                // side 0 → ARRIBA; side 1 → ABAJO.
                Vector2 dir = side == 0 ? -pole : pole;
                float rot = (float)Math.Atan2(dir.Y, dir.X);

                // ============================================================
                //  v6.80 — EL COLOR DEL JET (el punto dual nº2):
                //  · ESTILO AURORA: chorro UNIFORME (WhiteIncan + Acento —
                //    el gradiente vive en los TRAMOS, no en los polos).
                //  · ESTILO CLÁSICO: Hot arriba + Acento abajo (el reparto
                //    regio de siempre, con el blanco frío del polo bajo).
                // ============================================================
                Color cHot, cBody;
                if (_p.GradienteEnTodo)
                {
                    cHot = _p.WhiteIncan;
                    cBody = _p.Acento;
                }
                else
                {
                    cHot = side == 0 ? _p.WhiteIncan : _p.JetDownHot;
                    cBody = side == 0 ? _p.Hot : _p.Acento;
                }

                float baseOff = BandB * rr * 0.85f;
                Vector2 basePos = center + dir * baseOff;

                // --- EL CHORRO: 3 tramos ahusados con GLOW VIAJANDO
                //     hacia afuera (la onda de brillo sube por el haz). ---
                const int Tramos = 3;
                for (int k = 0; k < Tramos; k++)
                {
                    float f0 = k / (float)Tramos, f1 = (k + 1) / (float)Tramos;
                    float midF = (f0 + f1) * 0.5f;
                    Vector2 a = center + dir * (baseOff + f0 * 1.95f * rr);
                    Vector2 b = center + dir * (baseOff + f1 * 1.95f * rr);
                    Vector2 mid = (a + b) * 0.5f;
                    float len = (b - a).Length();
                    float w = (0.30f - 0.22f * midF) * rr;

                    // La onda de brillo VIAJA por el haz hacia la punta.
                    float wave = 0.5f + 0.5f * (float)Math.Sin(midF * 7f - time * 6f);

                    // v6.80 — EL COLOR DEL TRAMO (el punto dual nº3):
                    // · AURORA: el tramo NACE azul y MUERE dorado (Grad
                    //   0.10→1.00) con blend creciente 0.30→1.00.
                    // · CLÁSICO: el blend simple de siempre hacia cBody.
                    Color c;
                    if (_p.GradienteEnTodo)
                    {
                        Color grad = Grad(0.10f + 0.90f * midF);
                        c = Color.Lerp(cHot, grad, 0.30f + 0.70f * midF);
                    }
                    else
                    {
                        c = Color.Lerp(cHot, cBody, midF * 0.7f);
                    }

                    Capsule(mid, len, w * 2.0f, rot,
                        Tint(c, (0.20f + 0.18f * wave) * pulse * (1f - midF * 0.4f)));
                    Capsule(mid, len, w, rot,
                        Tint(c, (0.45f + 0.35f * wave) * pulse * (1f - midF * 0.4f)));
                }

                // --- LAS BOLAS viajando por el haz (el jet escupe materia). ---
                for (int m = 0; m < 3; m++)
                {
                    float phase = (time * 0.55f + m / 3f + side * 0.5f) % 1f;
                    Vector2 pos = center + dir * (baseOff + (0.15f + 1.75f * phase) * rr);
                    float a = (float)Math.Sin(phase * Math.PI) * 0.55f;
                    Quad(Glow, pos, new Vector2(0.42f * rr, 0.42f * rr), 0f,
                        Tint(cBody, a));
                    Quad(Glow, pos, new Vector2(0.18f * rr, 0.18f * rr), 0f,
                        Tint(cHot, a * 1.6f));
                }

                // --- ⚡ EL RAYO DENTRO DEL JET: un StormLib.Bolt
                //     recorriendo el corazón del chorro. ---
                if (StormLib.IsLit(seed + 90 + side * 13, boltFlick, 0.80f))
                {
                    Vector2 tip = center + dir * (baseOff + 1.85f * rr);
                    StormLib.Bolt(Main.spriteBatch, basePos, tip,
                        seed + 210 + side * 29, boltFlick,
                        Math.Max(2.6f, 0.045f * rr),
                        Tint(cBody, 0.50f), Tint(cHot, 0.90f),
                        1f, 6, Math.Max(8f, 0.12f * rr));
                }

                // --- Punta incandescente del polo. ---
                Vector2 jetTip = center + dir * (baseOff + 1.95f * rr);
                Quad(Glow, jetTip, new Vector2(0.55f * rr, 0.55f * rr), 0f,
                    Tint(cHot, 0.42f * pulse));
            }
        }

        // ------------------------------------------------------------------
        //  12. VOLUTAS CAYENDO — BrumaFX.Tendril espiralando al núcleo
        //      (la materia devorada se DISUELVE en humo — herencia Bruma)
        // ------------------------------------------------------------------

        private static void DrawFallingTendrils(Vector2 center, float r, float time, int seed)
        {
            for (int k = 0; k < TendrilCount; k++)
            {
                // Cada voluta gira a su velocidad (la rotación de la caída).
                float baseAng = time * (0.24f + 0.10f * k) + k * 2.6f;

                // LA RUTA ESPIRAL: nace lejos (2.95·R) y CAE al núcleo
                // (0.45·R) barriendo TendrilSweep rad — 8 puntos.
                var path = new Vector2[8];
                for (int p = 0; p < 8; p++)
                {
                    float f = p / 7f;
                    float ang = baseAng + f * TendrilSweep;
                    float rad = (TendrilStart - (TendrilStart - 0.45f) * f) * r;
                    path[p] = center + new Vector2(
                        (float)Math.Cos(ang) * rad,
                        (float)Math.Sin(ang) * rad * 0.8f);   // leve aplastado
                }

                // Una de cada estirpe (el par del perfil); la voluta se
                // DISUELVE al acercarse al núcleo (fade=1).
                Color c = k % 2 == 0 ? _p.TendrilPar : _p.TendrilImpar;
                float n = BrumaNoise.Fbm(k * 3.7f, time * 0.15f, seed + k, 3);
                BrumaFX.Tendril(path, 0.55f * r, c, seed + k * 97, time,
                    alpha: 0.40f + 0.20f * n, fade: 1f);
            }
        }

        // ------------------------------------------------------------------
        //  13. ONDAS DE DISTORSIÓN ×3 — el espacio-tiempo late
        // ------------------------------------------------------------------

        private static void DrawDistortionWaves(Vector2 center, float r, float time, int seed)
        {
            for (int w = 0; w < WaveCount; w++)
            {
                float phase = ((time / WaveCycle) + w / (float)WaveCount) % 1f;
                float radius = (1.15f + phase * 2.05f) * r;
                float fade = (1f - phase) * (1f - phase);

                // ============================================================
                //  v6.80 — EL COLOR DE LA ONDA (el punto dual nº4):
                //  · ESTILO AURORA: la onda NACE morada y MUERE dorada
                //    (el gradiente por FASE de expansión).
                //  · ESTILO CLÁSICO: la terna fija alternando por índice
                //    (las tres voces de siempre).
                // ============================================================
                Color c;
                if (_p.GradienteEnTodo)
                {
                    c = Grad(phase);
                }
                else
                {
                    c = w % 3 == 0 ? _p.Onda0 : w % 3 == 1 ? _p.Onda1 : _p.Onda2;
                }
                RingQuad(center, radius, phase * 2.4f + w,
                    Tint(c, 0.28f * fade));
            }
        }
    }

    // ======================================================================
    //  v6.80 — PERFIL SUPREMO — LA PALETA COMO DATO (la fusión nº1 de la
    //  auditoría v6.78): ~530 líneas de clon reducidas a UN struct de
    //  datos puros. Cada slot es un ROL semántico (no un color): el
    //  Clásico los llena de oro/carmesí/violeta; el Aurora de
    //  morado/azul/dorado. Los roles con Lerp (Mid/Mid2/TintFrio) se
    //  precomputan EN el perfil — el motor unificado queda plano.
    // ======================================================================

    /// <summary>
    /// LA PALETA-PERFIL de un agujero negro supremo: 39 números que
    /// definen SU identidad completa. El gradiente de 3 paradas
    /// (<see cref="Grad0"/>→<see cref="Grad1"/> hasta
    /// <see cref="GradSplit"/>, luego →<see cref="Grad2"/>) tiñe brazos
    /// espirales y, con <see cref="GradienteEnTodo"/>, TAMBIÉN la banda
    /// radial, los jets y las ondas (el estilo Aurora); sin él, esos tres
    /// caen a sus caminos térmicos clásicos (umbrales de intensidad,
    /// colores por lado y terna por índice).
    /// </summary>
    public readonly struct PerfilSupremo
    {
        // --- EL GRADIENTE DE 3 PARADAS ---
        /// <summary>Parada 0 del gradiente (la cuna).</summary>
        public readonly Color Grad0;
        /// <summary>Parada 1 del gradiente (el medio; con split 1.0 es la última).</summary>
        public readonly Color Grad1;
        /// <summary>Parada 2 del gradiente (el final; con split 1.0 jamás se alcanza).</summary>
        public readonly Color Grad2;
        /// <summary>Dónde vive la parada 1 (0..1). 1.0 = gradiente de 2 paradas.</summary>
        public readonly float GradSplit;
        /// <summary>TRUE = el gradiente lo tiñe TODO (estilo Aurora: banda
        /// radial, jets y ondas por fase); FALSE = los caminos térmicos
        /// clásicos.</summary>
        public readonly bool GradienteEnTodo;

        // --- LOS ROLES DEL MOTOR ---
        /// <summary>Blanco pico (picos de doppler/banda/fotones/bolt).</summary>
        public readonly Color WhiteIncan;
        /// <summary>El «rim/hot» del horizonte (rim del núcleo, funda de arcos, doppler medio-alto).</summary>
        public readonly Color Hot;
        /// <summary>El medio del disco doppler (dop &gt; 0.10).</summary>
        public readonly Color Mid;
        /// <summary>La banda media del disco (A: el Crimson plano; B: su Lerp 0.45).</summary>
        public readonly Color Mid2;
        /// <summary>Destino del Lerp del núcleo doppler (A: Mid; B: Deep).</summary>
        public readonly Color LerpDest;
        /// <summary>El color profundo (doppler frío).</summary>
        public readonly Color Deep;
        /// <summary>La banda externa del disco (A: Lerp(Deep,Acento,0.45); B: plano).</summary>
        public readonly Color TintFrio;
        /// <summary>El ACENTO (contrarroto, estela del flujo, nube externa, jets abajo…).</summary>
        public readonly Color Acento;
        /// <summary>El aura final grande.</summary>
        public readonly Color AuraFinal;
        /// <summary>La nube interior del halo.</summary>
        public readonly Color NubeInt;
        /// <summary>La nube exterior del halo.</summary>
        public readonly Color NubeExt;
        /// <summary>Nebulosa PAR.</summary>
        public readonly Color NebPar;
        /// <summary>Nebulosa IMPAR.</summary>
        public readonly Color NebImpar;
        /// <summary>El anillo de fotones.</summary>
        public readonly Color WarmWhite;
        /// <summary>El aura oscura (CON alfa: el velo que absorbe la luz).</summary>
        public readonly Color AuraOscura;
        /// <summary>Polvo ambiental 1 (h &lt; 0.40).</summary>
        public readonly Color Polvo1;
        /// <summary>Polvo ambiental 2 (h &lt; 0.75).</summary>
        public readonly Color Polvo2;
        /// <summary>Polvo ambiental 3 (resto).</summary>
        public readonly Color Polvo3;
        /// <summary>Runa INTERIOR: cuerpo.</summary>
        public readonly Color RuneInBody;
        /// <summary>Runa INTERIOR: punta.</summary>
        public readonly Color RuneInTip;
        /// <summary>Runa EXTERIOR: cuerpo.</summary>
        public readonly Color RuneOutBody;
        /// <summary>Runa EXTERIOR: punta.</summary>
        public readonly Color RuneOutTip;
        /// <summary>Runa ÍNTIMA: cuerpo.</summary>
        public readonly Color RuneIntBody;
        /// <summary>Runa ÍNTIMA: punta.</summary>
        public readonly Color RuneIntTip;
        /// <summary>Fotón/corredor A (i%3==0).</summary>
        public readonly Color RunnerA;
        /// <summary>Fotón/corredor B (i%3==1).</summary>
        public readonly Color RunnerB;
        /// <summary>Fotón/corredor C (i%3==2).</summary>
        public readonly Color RunnerC;
        /// <summary>Rayo fugitivo CALIENTE (i&lt;2).</summary>
        public readonly Color BoltHot;
        /// <summary>Rayo fugitivo FRÍO (el tercero).</summary>
        public readonly Color BoltFrio;
        /// <summary>Jet del polo de ABAJO, punta (solo estilo clásico).</summary>
        public readonly Color JetDownHot;
        /// <summary>Voluta PAR (la que cae primera).</summary>
        public readonly Color TendrilPar;
        /// <summary>Voluta IMPAR.</summary>
        public readonly Color TendrilImpar;
        /// <summary>Onda de distorsión 0 (solo estilo clásico).</summary>
        public readonly Color Onda0;
        /// <summary>Onda de distorsión 1 (solo estilo clásico).</summary>
        public readonly Color Onda1;
        /// <summary>Onda de distorsión 2 (solo estilo clásico).</summary>
        public readonly Color Onda2;

        /// <summary>El perfil COMPLETO (todos los slots, sin defaults: un
        /// perfil nuevo se piensa ENTERO — así ningún color se hereda por
        /// accidente).</summary>
        public PerfilSupremo(Color grad0, Color grad1, Color grad2, float gradSplit,
            bool gradienteEnTodo,
            Color whiteIncan, Color hot, Color mid, Color mid2, Color lerpDest, Color deep,
            Color tintFrio, Color acento, Color auraFinal, Color nubeInt, Color nubeExt,
            Color nebPar, Color nebImpar, Color warmWhite, Color auraOscura,
            Color polvo1, Color polvo2, Color polvo3,
            Color runeInBody, Color runeInTip, Color runeOutBody, Color runeOutTip,
            Color runeIntBody, Color runeIntTip,
            Color runnerA, Color runnerB, Color runnerC,
            Color boltHot, Color boltFrio, Color jetDownHot,
            Color tendrilPar, Color tendrilImpar,
            Color onda0, Color onda1, Color onda2)
        {
            Grad0 = grad0; Grad1 = grad1; Grad2 = grad2; GradSplit = gradSplit;
            GradienteEnTodo = gradienteEnTodo;
            WhiteIncan = whiteIncan; Hot = hot; Mid = mid; Mid2 = mid2;
            LerpDest = lerpDest; Deep = deep; TintFrio = tintFrio; Acento = acento;
            AuraFinal = auraFinal; NubeInt = nubeInt; NubeExt = nubeExt;
            NebPar = nebPar; NebImpar = nebImpar; WarmWhite = warmWhite;
            AuraOscura = auraOscura; Polvo1 = polvo1; Polvo2 = polvo2; Polvo3 = polvo3;
            RuneInBody = runeInBody; RuneInTip = runeInTip;
            RuneOutBody = runeOutBody; RuneOutTip = runeOutTip;
            RuneIntBody = runeIntBody; RuneIntTip = runeIntTip;
            RunnerA = runnerA; RunnerB = runnerB; RunnerC = runnerC;
            BoltHot = boltHot; BoltFrio = boltFrio; JetDownHot = jetDownHot;
            TendrilPar = tendrilPar; TendrilImpar = tendrilImpar;
            Onda0 = onda0; Onda1 = onda1; Onda2 = onda2;
        }
    }
}
