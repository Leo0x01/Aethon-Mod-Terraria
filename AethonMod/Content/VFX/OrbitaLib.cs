using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// OrbitaLib — v6.34 — LA ESCRITURA MÁGICA DEL VACÍO. · v6.37 — LA
    /// CORRECCIÓN: LOS ANILLOS RÚNICOS.
    ///
    /// LA LIBRERÍA DE SIGNOS MÁGICOS DE LOS AGUJEROS NEGROS. La
    /// aclaración del usuario (v6.37): "cuando te pedí una librería para
    /// LOS ANILLOS DE LOS AGUJEROS, me refería a los ANILLOS RÚNICOS" —
    /// los círculos de runas DE PIE con sus perlas que rodean a los
    /// vórtices (la firma de conjuro), NO el lado energético del disco.
    /// Esta librería contiene AHORA las DOS escrituras del vacío:
    ///
    ///   · LOS ANILLOS RÚNICOS (v6.37 — la petición original, completada):
    ///     el CÍRCULO DE CONJURO (el aro fino de pauta + las runas DE PIE
    ///     cabalgándolo — el radio respira por glifo, el glifo se mece,
    ///     cada runa arde con su resplandor y su PERLA, y el latido late
    ///     por glifo) — la técnica exacta del agujero SUPREMO, promovida
    ///     a primitiva 1:1. Dos alfabetos públicos (RunasVacio del
    ///     Supremo + RunasAbismo del Umbral) y DOS humores de vida: el
    ///     sereno (la casa) y el NERVIOSO (los círculos contrarrotantes
    ///     de los Ascendidos). EL SIGILO EROSIONADO: la variante del
    ///     Umbral — el aro ROTO con huecos por hash, glifos perdidos y
    ///     la púa que apaga lo que cruza. Y LA CORONA DE CONJURO: la
    ///     triple corona del Supremo (blanca íntima + dorada + violeta
    ///     contrarrotante) invocable en una llamada.
    ///   · EL LADO ENERGÉTICO (v6.34): el anillo de VEINTE BANDAS de
    ///     emisión (la fórmula fiel del shader: glow = sin(uv.x·20+t·5)
    ///     ·0.5+0.5), los ecos en resonancia, los fotones corriendo el
    ///     vórtice, el temblor de la distorsión y la corona de lazos de
    ///     neón — el disco de acreción y su familia.
    ///
    /// LAS LEYES DE LA FAMILIA (heredadas 1:1 de los agujeros negros —
    /// ni un número cambiado, el look de los vórtices queda INTACTO):
    ///   · EL CÍRCULO DE CONJURO: aro fino de pauta girando con el
    ///     conjunto + runas DE PIE flotando a radio·R (respirando 2.4·gs
    ///     a 1.35 Hz por glifo, meciéndose 2.0·gs a 0.85 Hz), latido de
    ///     brillo 0.75+0.25, resplandor 36·gs al 20%, trazos de cápsula
    ///     3.4·gs al 85% con gradiente cuerpo→punta pálida y PERLA
    ///     (7.0·gs al 62% + 3.2·gs al 90% del latido propio 0.8+0.2 a
    ///     3 Hz) a 11.5·gs sobre el glifo.
    ///   · EL ANILLO ENERGÉTICO: elipse oblicua (vórtice), MITAD TRASERA
    ///     y MITAD DELANTERA por separado (el anillo PASA por delante del
    ///     núcleo — el frente más brillante), veinte bandas de brillo
    ///     recorriéndolo a 5 rad/s, turbulencia viva por hash a 12 Hz,
    ///     gradiente térmico de TRES colores (pico = blanco incandescente,
    ///     media = color del anillo, valle = color profundo) y cápsula
    ///     HALO gruesa + cápsula NÚCLEO fina por segmento.
    ///   · LOS ECOS: dos anillos fantasma a ±1 banda de fase — la emisión
    ///     "rebota" en el espacio curvado.
    ///   · LOS FOTONES: corredores blanco-rosa orbitando con estela sobre
    ///     la tangente.
    ///   · LA DISTORSIÓN: el vaivén sinusoidal global (±3% del radio) que
    ///     atraviesa todo el render.
    ///   · LA CORONA DE ARCOS: cinco lazos de neón con asimetría por lazo
    ///     (líneas de campo curvadas), eco interior y NUDOS con destello
    ///     de 4 puntas en los ápices.
    ///
    /// LO NUEVO (el arsenal de embellecimiento):
    ///   · CirculoRunico(...) / RunaVacia(...) — el círculo de conjuro
    ///     y su runa de pie (v6.37, la petición completada).
    ///   · SigiloErosionado(...) — el sigilo del Umbral: aro roto y
    ///     glifos con huecos (v6.37).
    ///   · CoronaConjuro(...) — la triple corona del Supremo en una
    ///     llamada (v6.37).
    ///   · AnilloFotones(...) — el aro fino del horizonte (el anillo de
    ///     fotones puro).
    ///   · OndasDistorsion(...) — anillos de choque expandiéndose desde el
    ///     vórtice.
    ///   · SelloVacio(...) — EL SIGNO MÁGICO INVOCABLE de la casa oscura:
    ///     el círculo del vacío completo (anillo de energía + ecos +
    ///     fotones + aro del horizonte), el par oscuro del SelloSolar.
    ///
    /// CONTRATO DE DIBUJO (DOS secciones, cada una con SU patrón validado):
    ///   · SECCIÓN A (el patrón del agujero negro): las primitivas del
    ///     anillo dibujan al SPRITEBATCH ACTUAL — el llamador abre el
    ///     batch aditivo con los helpers AbrirAdditive()/CerrarBatch()
    ///     (o el suyo propio con Main.GameViewMatrix). CONTRATO: batch
    ///     ABIERTO en aditivo → batch ABIERTO. Los ANILLOS RÚNICOS
    ///     (CirculoRunico/RunaVacia/SigiloErosionado/CoronaConjuro)
    ///     siguen ESTE contrato: son hijos directos del pase aditivo de
    ///     los vórtices.
    ///   · SECCIÓN B (el patrón de la corona): CoronaArcos emite al BUFFER
    ///     de VFXCore (pase aditivo, coords de mundo) — Begin/Flush del
    ///     llamador, igual que el resto de librerías de la casa.
    /// </summary>
    public static class OrbitaLib
    {
        // ==================================================================
        //  LAS LEYES DE LA FAMILIA — constantes públicas (eran el corazón
        //  privado del agujero negro cósmico; ahora son el CONTRATO)
        // ==================================================================

        /// <summary>Semieje mayor del anillo (×R del vórtice).</summary>
        public const float RingA = 1.90f;

        /// <summary>Semieje menor del anillo (×R del vórtice).</summary>
        public const float RingB = 1.18f;

        /// <summary>Inclinación de la elipse del anillo (rad — vórtice oblicuo).</summary>
        public const float RingTilt = -0.38f;

        /// <summary>Rotación del anillo: 20°/s de la casa.</summary>
        public const float RingSpin = 0.349f;   // rad/s

        /// <summary>LA FÓRMULA DEL SHADER: bandas por vuelta del anillo.</summary>
        public const float BandFreq = 20f;

        /// <summary>Velocidad de las bandas (rad/s en espacio paramétrico).</summary>
        public const float BandSpeed = 5f;

        /// <summary>Segmentos de cada mitad del anillo.</summary>
        public const int RingSegments = 44;

        /// <summary>Regeneración de la turbulencia (Hz).</summary>
        public const int FlickHz = 12;

        /// <summary>
        /// Distorsión sinusoidal global de la casa (0.3): el vaivén que
        /// atraviesa TODO el render del vórtice.
        /// </summary>
        public const float DistorsionFuerza = 0.3f;

        /// <summary>Fotones corriendo el vórtice (la cuenta de la casa).</summary>
        public const int FotonesCount = 3;

        // ==================================================================
        //  LA PALETA DEL VÓRTICE (los tres peldaños del gradiente térmico +
        //  los colores de los satélites — la casa cósmica rojo-naranja;
        //  cada consumidor puede pasar la SUYA)
        // ==================================================================

        /// <summary>Pico de banda: blanco cálido incandescente.</summary>
        public static readonly Color HotWhite = new(255, 240, 220);

        /// <summary>Media banda: el color vivo del anillo.</summary>
        public static readonly Color RingVivo = new(255, 68, 26);

        /// <summary>Valle de banda: el color profundo.</summary>
        public static readonly Color RingProfundo = new(200, 30, 20);

        // ==================================================================
        //  EL PINCEL (las texturas procedurales del proyecto)
        // ==================================================================

        private static Asset<Texture2D> _glow;
        private static Asset<Texture2D> _ring;

        private static Texture2D Glow =>
            (_glow ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow")).Value;

        private static Texture2D Ring =>
            (_ring ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Ring")).Value;

        // ==================================================================
        //  SECCIÓN A · LOS HELPERS DE BATCH (el patrón del agujero negro)
        // ==================================================================

        /// <summary>
        /// v6.50.11 — ¿cerramos un lote AJENO al abrir? (la sonda de
        /// VFXCore.LoteAbierto reemplaza al End defensivo de v6.49, que
        /// disparaba una first-chance por llamada en el flujo NORMAL —
        /// el PreDraw del llamador ya había cerrado el lote — y tML
        /// 2026.07 las registraba como "Excepción silenciosa"). El
        /// rastreo es EXACTO: true solo si de verdad había un Begin vivo
        /// — y CerrarBatch aplica el CONTRATO DE CURACIÓN (lote vanilla
        /// ABIERTO al salir, siempre — la restauración exacta de v6.49
        /// devolvía el veneno cuando encontraba el lote cerrado).
        /// </summary>
        private static bool _loteAjenoAbierto;

        /// <summary>Abre el SpriteBatch en ADITIVO con la matriz del juego.</summary>
        public static void AbrirAdditive()
        {
            // v6.50.11 — SONDA (adiós a la first-chance): el End defensivo
            // solo cuando hay un Begin vivo — la pareja try{End}catch de
            // v6.49 disparaba una first-chance CADA VEZ que el PreDraw del
            // llamador ya había cerrado el lote (el flujo NORMAL de la
            // casa), y tML 2026.07 las registra como "Excepción
            // silenciosa" — 27 stacks únicos en el client.log del
            // usuario, todas capturadas por el propio catch: ruido puro.
            // El rastreo del lote ajeno ahora es EXACTO (la sonda no
            // miente; y el helper lleva el fallback clásico por si un FNA
            // futuro renombra el campo).
            _loteAjenoAbierto = VFXCore.CerrarLoteSiAbierto();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>Abre el SpriteBatch en ALFA (el pase de lo oscuro).</summary>
        public static void AbrirAlpha()
        {
            // v6.50.11 — mismo blindaje por sonda (ver AbrirAdditive).
            _loteAjenoAbierto = VFXCore.CerrarLoteSiAbierto();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>
        /// Cierra el SpriteBatch del efecto y devuelve el lote ABIERTO y
        /// válido (v6.50.11 — EL CONTRATO DE CURACIÓN). v6.49 devolvía
        /// "como estaba" (abierto→abierto, cerrado→cerrado): la rama
        /// cerrado→cerrado era la "restauración exacta" que en realidad
        /// devolvía el veneno — tML 2026.07 mata al proyectil que dibuja
        /// con el lote cerrado (el try/catch de DrawProjectiles hace
        /// projectile.active = false) y el End final del bucle de
        /// proyectiles lanza. Ahora: si el ABRIR cerró un lote ajeno se le
        /// devuelve su lote (con los parámetros EXACTOS del pase de
        /// entidades de vanilla — antes LinearClamp+CullNone, que dejaba
        /// el resto del pase muestreando bilineal); y si el ABRIR lo
        /// encontró CERRADO, aquí SE CURA: un lote vanilla vivo al salir,
        /// siempre. Idempotente por sonda (ReabrirLoteVanilla no pisa un
        /// Begin vivo).
        /// </summary>
        public static void CerrarBatch()
        {
            // v6.50.11 — sonda: cierra NUESTRO lote (el del efecto) sin
            // first-chance.
            VFXCore.CerrarLoteSiAbierto();
            _loteAjenoAbierto = false;
            // La curación incondicional (los parámetros vanilla exactos).
            VFXCore.ReabrirLoteVanilla();
        }

        /// <summary>Quad centrado al batch actual (tamaño total = size px).</summary>
        public static void Quad(Texture2D tex, Vector2 pos, Vector2 size, float rot, Color tint)
        {
            if (tint.A == 0) return;
            Main.spriteBatch.Draw(tex, pos, null, tint, rot,
                new Vector2(tex.Width, tex.Height) * 0.5f,
                size / new Vector2(tex.Width, tex.Height),
                SpriteEffects.None, 0f);
        }

        /// <summary>Cápsula de luz: SoftGlow estirado (largo × ancho px).</summary>
        public static void Capsule(Vector2 mid, float len, float width, float rot, Color tint)
        {
            if (tint.A == 0) return;
            Quad(Glow, mid, new Vector2(len + width, width * 1.9f), rot, tint);
        }

        /// <summary>
        /// Anillo fino: Ring.png calibrado para radio VISIBLE
        /// (la constante 2.174 medida del pincel del proyecto).
        /// </summary>
        public static void AnilloFino(Vector2 pos, float radioVisible, float rot, Color tint)
        {
            if (tint.A == 0) return;
            float s = radioVisible * 2.174f;
            Quad(Ring, pos, new Vector2(s, s), rot, tint);
        }

        /// <summary>Punto sobre la elipse del anillo (param t en rad).</summary>
        public static Vector2 Ellipse(Vector2 c, float r, float t)
        {
            return VFXCore.Ellipse(c, RingA * r, RingB * r, RingTilt, t);
        }

        /// <summary>Tinte de la familia del vacío: RGB intacto, alfa = intensidad.</summary>
        public static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color(c.R, c.G, c.B, (byte)(int)(255f * f));
        }

        // ==================================================================
        //  PRIMITIVA 0 — LA DISTORSIÓN (el temblor global del vórtice)
        // ==================================================================

        /// <summary>
        /// EL VAIVÉN SINUSOIDAL GLOBAL: devuelve el factor de respiración
        /// del radio (≈1 ± 3%) — la distorsión 0.3 de la casa atravesando
        /// todo lo que orbita el vórtice. Multiplícalo por el radio.
        /// </summary>
        public static float Distorsion(float time, float fuerza = DistorsionFuerza)
        {
            return 1f + 0.03f * ((float)Math.Sin(time) * fuerza);
        }

        // ==================================================================
        //  PRIMITIVA 1 — EL ANILLO ENERGÉTICO (la fórmula fiel del shader)
        // ==================================================================

        /// <summary>
        /// EL ANILLO ENERGÉTICO — la fórmula EXACTA del shader de la casa:
        /// glow = sin(uv.x·20 + t·5)·0.5+0.5 → VEINTE BANDAS de emisión
        /// recorriendo la elipse del vórtice, con turbulencia viva por hash
        /// (12 Hz), gradiente térmico de TRES colores, cápsula HALO gruesa
        /// + cápsula NÚCLEO fina y el punto blanco del pico. Se pinta por
        /// MITADES (trasera/delantera) para que el anillo PUEDA pasar por
        /// delante del núcleo del consumidor.
        /// </summary>
        /// <param name="center">Centro en coords de PANTALLA (mundo − Main.screenPosition — el batch abre con GameViewMatrix).</param>
        /// <param name="rr">Radio respirado del vórtice (px — ya con Distorsion aplicado).</param>
        /// <param name="time">Tiempo animado.</param>
        /// <param name="seed">Semilla determinista del disparo.</param>
        /// <param name="flick">Tick de turbulencia ((int)(time·12)).</param>
        /// <param name="front">TRUE = mitad DELANTERA (cruza por delante); FALSE = trasera.</param>
        /// <param name="hot">Color del pico de banda (blanco incandescente).</param>
        /// <param name="mid">Color de media banda (el vivo).</param>
        /// <param name="deep">Color del valle (el profundo).</param>
        /// <param name="semiA">Semieje mayor (×rr) — la casa: 1.90.</param>
        /// <param name="semiB">Semieje menor (×rr) — la casa: 1.18.</param>
        /// <param name="tilt">Inclinación del vórtice — la casa: −0.38.</param>
        /// <param name="bright">Multiplicador de brillo — trasera 0.90 / delantera 1.30.</param>
        public static void AnilloEnergia(Vector2 center, float rr, float time, int seed, int flick,
            bool front, Color? hot = null, Color? mid = null, Color? deep = null,
            float semiA = RingA, float semiB = RingB, float tilt = RingTilt, float bright = -1f)
        {
            // La mitad delantera (t ∈ 0..π) cruza POR DELANTE del núcleo.
            float t0 = front ? 0f : MathHelper.Pi;
            float span = MathHelper.Pi;
            float bMul = bright >= 0f ? bright : (front ? 1.30f : 0.90f);
            Color cHot = hot ?? HotWhite;
            Color cMid = mid ?? RingVivo;
            Color cDeep = deep ?? RingProfundo;

            // El anillo ROTA (20°/s de la casa).
            float spin = time * RingSpin;

            for (int s = 0; s < RingSegments; s++)
            {
                float t = t0 + span * (s + 0.5f) / RingSegments;
                float tSpin = t + spin;   // el patrón de bandas gira con el anillo

                // Posición + tangente de la cápsula (la elipse paramétrica).
                Vector2 a = VFXCore.Ellipse(center, semiA * rr, semiB * rr, tilt,
                    t - span / (RingSegments * 2f));
                Vector2 b = VFXCore.Ellipse(center, semiA * rr, semiB * rr, tilt,
                    t + span / (RingSegments * 2f));
                Vector2 midPos = (a + b) * 0.5f;
                Vector2 seg = b - a;
                float segLen = seg.Length();
                if (segLen < 0.5f) continue;
                float rot = (float)Math.Atan2(seg.Y, seg.X);

                // ============================================================
                //  LA FÓRMULA EXACTA DEL SHADER:
                //  glow = sin(uv.x · 20 + _Time.y · 5) · 0.5 + 0.5
                //  → VEINTE BANDAS de emisión recorriendo el anillo.
                // ============================================================
                float glow = 0.5f + 0.5f * (float)Math.Sin(tSpin * BandFreq - time * BandSpeed);

                // Turbulencia viva por hash (regenerada a 12 Hz).
                float turb = 0.74f + 0.26f * VFXCore.Hash01(seed, 700 + s, flick);
                float inten = glow * turb * bMul;

                // Gradiente térmico: banda al máximo = blanco incandescente;
                // media = color vivo; valle = color profundo.
                Color c;
                if (inten > 0.82f) c = cHot;
                else if (inten > 0.45f) c = cMid;
                else c = cDeep;

                // Cápsula HALO (gruesa, tenue) + cápsula NÚCLEO (fina, viva).
                Capsule(midPos, segLen, 0.36f * rr * (0.7f + inten), rot, Tint(c, 0.40f * inten));
                Capsule(midPos, segLen, 0.12f * rr * inten, rot, Tint(c, 0.85f * inten));

                // En el PICO de cada banda, un punto blanco extra.
                if (inten > 0.86f)
                {
                    Quad(Glow, midPos, new Vector2(0.32f * rr, 0.32f * rr), rot,
                        Tint(cHot, 0.70f * (inten - 0.86f) / 0.14f));
                }
            }
        }

        // ==================================================================
        //  PRIMITIVA 2 — LOS ECOS DEL ANILLO (la resonancia del shader)
        // ==================================================================

        /// <summary>
        /// LOS ECOS: dos anillos fantasma a ±1 banda de fase del shader —
        /// la emisión del anillo "rebota" en el espacio curvado. Respiran
        /// con la distorsión sinusoidal global.
        /// </summary>
        /// <param name="center">Centro en coords de PANTALLA (mundo − Main.screenPosition — el batch abre con GameViewMatrix).</param>
        /// <param name="rr">Radio respirado (px).</param>
        /// <param name="time">Tiempo animado.</param>
        /// <param name="distortion">La distorsión cruda (sin(time)·0.3).</param>
        /// <param name="vivo">Color del anillo vivo.</param>
        /// <param name="profundo">Color del eco profundo.</param>
        public static void EcosAnillo(Vector2 center, float rr, float time, float distortion,
            Color? vivo = null, Color? profundo = null)
        {
            Color cVivo = vivo ?? RingVivo;
            Color cDeep = profundo ?? RingProfundo;

            float phase = time * BandSpeed * 0.20f;

            float echo1 = 0.55f + 0.45f * (float)Math.Sin(phase);
            AnilloFino(center, 1.42f * rr, time * RingSpin, Tint(cVivo, 0.16f * echo1));

            float echo2 = 0.55f + 0.45f * (float)Math.Sin(phase + MathHelper.Pi);
            AnilloFino(center, 0.80f * rr, -time * RingSpin * 0.7f, Tint(cDeep, 0.14f * echo2));

            // Un velo tenue de emisión alrededor del anillo entero.
            Quad(Glow, center, new Vector2(4.4f * rr, 3.6f * rr), RingTilt,
                Tint(cVivo, 0.10f + 0.05f * distortion));
        }

        // ==================================================================
        //  PRIMITIVA 3 — LOS FOTONES (luz corriendo el vórtice)
        // ==================================================================

        /// <summary>
        /// LOS CORREDORES DE FOTONES: destellos blanco-vivo orbitando el
        /// vórtice con estela sobre la tangente — luz "corriendo" en
        /// círculos alrededor de lo que no la deja escapar.
        /// </summary>
        /// <param name="center">Centro en coords de PANTALLA (mundo − Main.screenPosition — el batch abre con GameViewMatrix).</param>
        /// <param name="rr">Radio respirado (px).</param>
        /// <param name="time">Tiempo animado.</param>
        /// <param name="count">Número de fotones (la casa: 3).</param>
        /// <param name="vivo">Color del halo del fotón.</param>
        /// <param name="caliente">Color del núcleo del fotón.</param>
        public static void Fotones(Vector2 center, float rr, float time, int count = FotonesCount,
            Color? vivo = null, Color? caliente = null)
        {
            Color cVivo = vivo ?? RingVivo;
            Color cHot = caliente ?? HotWhite;

            for (int i = 0; i < count; i++)
            {
                // Orbitan con la rotación del anillo más su propia velocidad.
                float t = time * (RingSpin + 0.55f + 0.20f * i) + i * 2.1f;
                Vector2 pos = Ellipse(center, rr, t);
                float twinkle = 0.65f + 0.35f * (float)Math.Sin(time * 8f + i * 2.3f);

                // Estela corta detrás del fotón (cápsula sobre la tangente).
                Vector2 behind = Ellipse(center, rr, t - 0.22f);
                Vector2 seg = pos - behind;
                float len = seg.Length();
                if (len > 0.5f)
                {
                    float rot = (float)Math.Atan2(seg.Y, seg.X);
                    Vector2 mid = (pos + behind) * 0.5f;
                    Capsule(mid, len, 0.14f * rr, rot, Tint(cVivo, 0.40f * twinkle));
                }

                // Halo vivo + núcleo blanco caliente.
                Quad(Glow, pos, new Vector2(1.05f * rr, 1.05f * rr), 0f,
                    Tint(cVivo, 0.35f * twinkle));
                Quad(Glow, pos, new Vector2(0.42f * rr, 0.42f * rr), 0f,
                    Tint(cHot, 0.85f * twinkle));
            }
        }

        // ==================================================================
        //  PRIMITIVA 4 — LAS ONDAS DE DISTORSIÓN (el choque del vórtice)
        // ==================================================================

        /// <summary>
        /// LAS ONDAS DE DISTORSIÓN: anillos de choque expandiéndose desde
        /// el vórtice en ciclo continuo (nace en el horizonte, se dilata,
        /// se disuelve — y vuelve a nacer). El pulso gravitatorio.
        /// </summary>
        /// <param name="center">Centro en coords de PANTALLA (mundo − Main.screenPosition — el batch abre con GameViewMatrix).</param>
        /// <param name="r">Radio base del vórtice (px).</param>
        /// <param name="time">Tiempo animado.</param>
        /// <param name="seed">Semilla determinista.</param>
        /// <param name="count">Ondas simultáneas (2).</param>
        /// <param name="ciclo">Segundos de ciclo de cada onda (2.4).</param>
        /// <param name="color">Color de la onda.</param>
        public static void OndasDistorsion(Vector2 center, float r, float time, int seed,
            int count = 2, float ciclo = 2.4f, Color? color = null)
        {
            Color c = color ?? RingVivo;
            if (ciclo <= 0.01f) ciclo = 2.4f;   // guard: ciclo degenerado → la casa
            for (int i = 0; i < count; i++)
            {
                float fase = (time / ciclo + i / (float)count + VFXCore.Hash01(seed, 810 + i, 37)) % 1f;
                float growth = (float)Math.Sin(fase * MathHelper.Pi);   // nace → crece → muere
                if (growth < 0.06f) continue;
                float radio = (1.0f + 1.6f * fase) * r;
                float alpha = 0.20f * (1f - fase) * growth;
                AnilloFino(center, radio, time * 0.4f * (i % 2 == 0 ? 1f : -1f),
                    Tint(c, alpha));
            }
        }

        // ==================================================================
        //  SECCIÓN B · LA CORONA DE ARCOS (el patrón buffer de VFXCore)
        // ==================================================================

        /// <summary>Número de lazos de la corona (5: el diseño original).</summary>
        public const int LazosCorona = 5;

        /// <summary>Segmentos por lazo (suavidad del arco).</summary>
        private const int CoronaSegments = 18;

        /// <summary>
        /// LA CORONA DE ARCOS DE NEÓN: cinco lazos con asimetría por lazo
        /// (líneas de campo curvadas, no un arcoíris), un ECO interior más
        /// tenue por lazo y NUDOS con destello de 4 puntas en los ápices.
        /// Respira con el tiempo y balancea cada lazo de forma
        /// determinista — energía VIVA, no un adorno estático.
        ///
        /// CONTRATO: emite al BUFFER de VFXCore (coords de mundo) — el
        /// llamador controla Begin/Flush (VFXCore.Begin() → llamada →
        /// AppendToPlayerDraw/FlushAdditive).
        /// </summary>
        /// <param name="center">Centro de la cabeza que corona (mundo).</param>
        /// <param name="horizonPx">Radio del "horizonte" sobre el que se
        /// arquean los lazos (cabeza humana ≈ 0.55× su ancho).</param>
        /// <param name="time">Tiempo animado (GlobalTimeWrappedHourly).</param>
        /// <param name="alpha">Multiplicador global de intensidad.</param>
        public static void CoronaArcos(Vector2 center, float horizonPx, float time, float alpha = 1f)
        {
            if (horizonPx < 1.5f) return;

            for (int l = 0; l < LazosCorona; l++)
            {
                float t01 = l / (float)(LazosCorona - 1);            // 0 exterior → 1 interior
                float breathe = VFXCore.Breathe(time, 2.2f, l * 1.7f);
                // ASIMETRÍA por lazo (semianchos izq/der distintos y balanceo
                // determinista): las líneas de campo se curvan.
                float sway = 0.18f * VFXCore.Sway(time, 0.9f, l * 2.6f);
                float halfWL = horizonPx * (1.55f - 0.30f * t01) * (1f - sway) * breathe;
                float halfWR = horizonPx * (1.55f - 0.30f * t01) * (1f + sway) * breathe;
                float apexH = horizonPx * (2.30f - 0.95f * t01) * breathe;
                float baseY = center.Y - horizonPx * 1.06f;

                // El lazo completo: media elipse superior ASIMÉTRICA por
                // puntos de glow con grosor variable (fino en las bases,
                // corpulento al subir, afilado en el ápice).
                for (int s = 0; s <= CoronaSegments; s++)
                {
                    float ang = s / (float)CoronaSegments * MathHelper.Pi;   // 0..π
                    float edge = (float)Math.Sin(ang);                 // 0 bases, 1 ápice
                    float side = (float)Math.Cos(ang);                 // -1 izq → +1 der
                    float halfW = side < 0f ? halfWL : halfWR;
                    Vector2 pos = new Vector2(
                        center.X - (float)Math.Cos(ang) * halfW,
                        baseY - edge * apexH);

                    Color col = VFXPalettes.CrimsonCourt.Loop(edge);
                    // Grosor: crece hacia arriba, afila en el ápice.
                    float thickness = 0.16f + 0.13f * edge * (1f - 0.35f * edge);
                    float intensity = 0.30f + 0.70f * edge;
                    float pointPx = horizonPx * thickness;

                    Color finalCol = col * (intensity * alpha);
                    VFXCore.Quad(pos, finalCol, new Vector2(pointPx * 2f, pointPx * 2f));
                }

                // ECO interior: un filamento más tenue y fino encajado dentro
                // del lazo (los filamentos encajados de la casa).
                for (int s = 1; s < CoronaSegments; s++)
                {
                    float ang = s / (float)CoronaSegments * MathHelper.Pi;
                    float edge = (float)Math.Sin(ang);
                    float side = (float)Math.Cos(ang);
                    float halfW = (side < 0f ? halfWL : halfWR) * 0.66f;
                    Vector2 pos = new Vector2(
                        center.X - (float)Math.Cos(ang) * halfW,
                        baseY - edge * apexH * 0.72f);
                    Color col = Color.Lerp(VFXPalettes.CrimsonCourt.EchoBase,
                                           VFXPalettes.CrimsonCourt.EchoApex, edge);
                    float echoPx = horizonPx * 0.09f;
                    VFXCore.Quad(pos, col * (0.55f * alpha), new Vector2(echoPx * 2f, echoPx * 2f));
                }

                // NUDO en el ápice: englobado + DESTELLO DE 4 PUNTAS
                // (dos glows estirados en cruz) + chispa blanca central.
                float knotPulse = 0.85f + 0.30f * (float)Math.Sin(time * 3.1f + l * 2.3f);
                Vector2 apex = new Vector2(center.X, baseY - apexH);

                float knotPx = horizonPx * 0.34f;
                VFXCore.Quad(apex, VFXPalettes.CrimsonCourt.Knot * (knotPulse * alpha),
                    new Vector2(knotPx * 2f, knotPx * 2f));

                // Destello de 4 puntas: dos elipses estiradas en cruz.
                float flareLen = horizonPx * 0.85f * knotPulse;
                float flareWide = horizonPx * 0.10f;
                VFXCore.Quad(apex, VFXPalettes.CrimsonCourt.KnotFlare * (knotPulse * 0.85f * alpha),
                    new Vector2(flareLen, flareWide));
                VFXCore.Quad(apex, VFXPalettes.CrimsonCourt.KnotFlare * (knotPulse * 0.85f * alpha),
                    new Vector2(flareWide, flareLen));

                // Chispa blanca central.
                float sparkPx = horizonPx * 0.14f;
                VFXCore.Quad(apex, VFXPalettes.CrimsonCourt.KnotSpark * (knotPulse * 0.9f * alpha),
                    new Vector2(sparkPx * 2f, sparkPx * 2f));
            }
        }

        // ==================================================================
        //  LOS ANILLOS RÚNICOS DEL VACÍO (v6.37 — LA CORRECCIÓN DE LA
        //  LIBRERÍA): los círculos de conjuro de los agujeros negros
        //  promovidos a primitivas 1:1. La aclaración del usuario: los
        //  anillos de los agujeros SON los rúnicos.
        // ==================================================================

        // --- EL ALFABETO DEL SUPREMO (público — la escritura del vacío) ---

        /// <summary>
        /// EL ALFABETO DEL VACÍO — los OCHO glifos DE PIE del Supremo:
        /// cada runa es una lista de TRAZOS (pares de puntos en espacio
        /// local ~11×15). Diseños angulares de CORONA SUPREMA (el sol
        /// roto, el cetro, el trono...) — la escritura que rodea al
        /// vórtice con sus perlas.
        /// </summary>
        public static readonly Vector2[][] RunasVacio = new Vector2[][]
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

        /// <summary>
        /// EL ALFABETO DEL ABISMO — los DOCE glifos del Umbral (la
        /// escritura EROSIONADA: la corona, el colmillo, la cadena, el
        /// ojo cerrado...), la firma del sigilo roto.
        /// </summary>
        public static readonly Vector2[][] RunasAbismo = new Vector2[][]
        {
            // U0 — LA CORONA
            new Vector2[] { new(-4f, 5.5f), new(-4f, -5.5f), new(-4f, -5.5f), new(-2.5f, -1f), new(-2.5f, -1f), new(-1f, -6.5f), new(-1f, -6.5f), new(1f, -1f), new(1f, -1f), new(2.5f, -6.5f), new(2.5f, -6.5f), new(4f, -1f), new(4f, -1f), new(4f, 5.5f), new(-4f, 5.5f), new(4f, 5.5f) },
            // U1 — EL COLMILLO
            new Vector2[] { new(-3f, -6.5f), new(-3f, 4f), new(-3f, 4f), new(0f, 7f), new(0f, 7f), new(3f, 4f), new(3f, 4f), new(3f, -6.5f), new(-3f, -6.5f), new(3f, -6.5f) },
            // U2 — LA CADENA
            new Vector2[] { new(-3.5f, -3.5f), new(3.5f, -3.5f), new(3.5f, -3.5f), new(3.5f, 1.5f), new(3.5f, 1.5f), new(-3.5f, 1.5f), new(-3.5f, 1.5f), new(-3.5f, -3.5f), new(-3.5f, 1.5f), new(3.5f, 6.5f) },
            // U3 — EL OJO CERRADO
            new Vector2[] { new(-4f, 0f), new(0f, -3f), new(0f, -3f), new(4f, 0f), new(4f, 0f), new(0f, 3f), new(0f, 3f), new(-4f, 0f), new(-2f, -1.2f), new(2f, -1.2f) },
            // U4 — LA MEDIA LUNA
            new Vector2[] { new(2f, -6.5f), new(-2f, -6.5f), new(-2f, -6.5f), new(-3.5f, 0f), new(-3.5f, 0f), new(-2f, 6.5f), new(-2f, 6.5f), new(2f, 6.5f), new(2f, 6.5f), new(0.5f, 0f), new(0.5f, 0f), new(2f, -6.5f) },
            // U5 — EL ABISMO
            new Vector2[] { new(-3.5f, -6f), new(3.5f, -6f), new(3.5f, -6f), new(0f, 6.5f), new(-1.5f, 0f), new(1.5f, 0f) },
            // U6 — LA ESTACA
            new Vector2[] { new(0f, -7f), new(0f, 7f), new(-3f, -2f), new(0f, -5f), new(3f, -2f), new(0f, -5f), new(-2f, 4.5f), new(2f, 4.5f) },
            // U7 — EL TRÉBOL ANGULAR
            new Vector2[] { new(0f, -6.5f), new(0f, 0f), new(0f, 0f), new(-3.5f, -2.5f), new(0f, 0f), new(3.5f, -2.5f), new(0f, 0f), new(0f, 6.5f), new(-2.5f, 3.5f), new(2.5f, 3.5f) },
            // U8 — LA GARRA
            new Vector2[] { new(-3.5f, 7f), new(-3.5f, -2f), new(-3.5f, -2f), new(-1.5f, -6.5f), new(-1.5f, -6.5f), new(0f, -1f), new(0f, -1f), new(1.5f, -6.5f), new(1.5f, -6.5f), new(3.5f, -2f), new(3.5f, -2f), new(3.5f, 7f) },
            // U9 — EL SIGILO
            new Vector2[] { new(0f, -6.5f), new(-4f, 3.5f), new(-4f, 3.5f), new(4f, 3.5f), new(4f, 3.5f), new(0f, -6.5f), new(-2.5f, 6.5f), new(2.5f, 6.5f) },
            // U10 — EL PORTÓN
            new Vector2[] { new(-3.5f, 7f), new(-3.5f, -7f), new(-3.5f, -7f), new(3.5f, -7f), new(3.5f, -7f), new(3.5f, 7f), new(-3.5f, 7f), new(3.5f, 7f), new(-3.5f, -3f), new(3.5f, -3f) },
            // U11 — LA VELA
            new Vector2[] { new(-2.5f, 7f), new(-2.5f, -3f), new(-2.5f, -3f), new(0f, -6.5f), new(0f, -6.5f), new(2.5f, -3f), new(2.5f, -3f), new(2.5f, 7f), new(-2.5f, 7f), new(2.5f, 7f), new(-1.2f, 1f), new(1.2f, 1f) },
        };

        // --- LAS CONSTANTES DE LA TRIPLE CORONA (del Supremo, públicas) ---

        /// <summary>Radio del aro ÍNTIMO de la triple corona (×R — la corona blanca).</summary>
        public const float AroIntimo = 2.02f;

        /// <summary>Runas del aro íntimo (giran rápido: el círculo vivo).</summary>
        public const int RunasIntimas = 6;

        /// <summary>Giro del aro íntimo (rad/s — rápido e íntimo, CW).</summary>
        public const float GiroIntimo = 0.16f;

        /// <summary>Radio del aro MEDIO de la triple corona (×R — la corona dorada).</summary>
        public const float AroMedio = 2.62f;

        /// <summary>Runas del aro medio (giran con el conjunto).</summary>
        public const int RunasMedias = 8;

        /// <summary>Giro del aro medio (rad/s — CW lento).</summary>
        public const float GiroMedio = 0.10f;

        /// <summary>Radio del aro EXTERNO de la triple corona (×R — la corona violeta).</summary>
        public const float AroExterno = 3.30f;

        /// <summary>Runas del aro externo (el contrarroto arcano).</summary>
        public const int RunasExternas = 6;

        /// <summary>Giro del aro externo (rad/s — CONTRARROTO, CCW).</summary>
        public const float GiroExterno = -0.075f;

        /// <summary>Calibre del glifo: r de referencia de la escala (la casa: 52).</summary>
        public const float GlifoCalibre = 52f;

        /// <summary>Multiplicador de escala del glifo del Supremo (la casa: 1.40).</summary>
        public const float GlifoMul = 1.40f;

        /// <summary>Segmentos del aro del sigilo erosionado (la casa: 30).</summary>
        public const int ErosionSegmentos = 30;

        // --- LA PALETA RÚNICA DEL VACÍO (los pares cuerpo/punta) ---

        /// <summary>Cuerpo de runa BLANCA íntima (la corona viva del Supremo).</summary>
        public static readonly Color RunaBlanca = new(255, 245, 220);

        /// <summary>Punta de runa blanca (incandescente).</summary>
        public static readonly Color RunaBlancaTip = new(255, 252, 240);

        /// <summary>Cuerpo de runa DORADA (el conjuro del Supremo).</summary>
        public static readonly Color RunaDorada = new(255, 180, 70);

        /// <summary>Punta de runa dorada (pálida cálida).</summary>
        public static readonly Color RunaDoradaTip = new(255, 235, 175);

        /// <summary>Cuerpo de runa VIOLETA (la envoltura contrarrotante).</summary>
        public static readonly Color RunaVioleta = new(110, 130, 255);

        /// <summary>Punta de runa violeta (fría pálida).</summary>
        public static readonly Color RunaVioletaTip = new(205, 220, 255);

        // ==================================================================
        //  PRIMITIVA RÚNICA 1 — LA RUNA DE PIE (la unidad atómica)
        // ==================================================================

        /// <summary>
        /// UNA RUNA DE PIE del círculo de conjuro — la unidad atómica de
        /// los anillos rúnicos de los agujeros negros (la técnica exacta
        /// del Supremo): el resplandor suave DETRÁS del glifo, los TRAZOS
        /// de cápsula con gradiente vertical (abajo cuerpo, arriba punta
        /// pálida — la runa queda DE PIE, sin rotar con la órbita) y la
        /// PERLA de la corona latiendo sobre el glifo.
        ///
        /// CONTRATO (Sección A): batch ABIERTO en aditivo → ABIERTO.
        /// </summary>
        /// <param name="glyphPos">Centro del glifo (coords de PANTALLA).</param>
        /// <param name="time">Tiempo animado.</param>
        /// <param name="g">Índice del glifo (desfasa latidos y perlas).</param>
        /// <param name="strokes">Los trazos de la runa (alfabeto).</param>
        /// <param name="body">Color del cuerpo de la runa.</param>
        /// <param name="tip">Color de la punta pálida (y de la perla).</param>
        /// <param name="glyphScale">Escala final del glifo.</param>
        /// <param name="alphaMul">Multiplicador global (fade de vida).</param>
        /// <param name="nervioso">TRUE = el humor de los círculos contrarrotantes de los Ascendidos (glow 30 al 18%, latido 0.70+0.30 a 2.8 Hz, perlas 6.2/2.9 al 55/90%).</param>
        /// <param name="strokeW">Grosor del trazo en unidades de gs (−1 = el del humor: 3.4 sereno / 3.2 nervioso).</param>
        /// <param name="strokeAlpha">Alfa del trazo (−1 = el del humor: 0.85 / 0.80).</param>
        /// <param name="pearlTip">Color de la punta de la PERLA (null = tip — el modelo de la casa).</param>
        public static void RunaVacia(Vector2 glyphPos, float time, int g,
            Vector2[] strokes, Color body, Color tip, float glyphScale,
            float alphaMul = 1f, bool nervioso = false,
            float strokeW = -1f, float strokeAlpha = -1f, Color? pearlTip = null)
        {
            if (strokes == null || strokes.Length < 2 || glyphScale <= 0.01f) return;

            // Latido de brillo propio por glifo.
            float pulse = nervioso
                ? 0.70f + 0.30f * (float)Math.Sin(time * 2.8f + g * 1.7f)
                : 0.75f + 0.25f * (float)Math.Sin(time * 2.4f + g * 1.3f);

            // Resplandor suave DETRÁS de cada runa (el "glow arcano").
            float glowSize = nervioso ? 30f : 36f;
            float glowAlpha = nervioso ? 0.18f : 0.20f;
            Quad(Glow, glyphPos, new Vector2(glowSize * glyphScale, glowSize * glyphScale), 0f,
                Tint(body, glowAlpha * pulse * alphaMul));

            // Trazos: cápsulas, cuerpo → punta pálida (runa DE PIE).
            float w = strokeW >= 0f ? strokeW : (nervioso ? 3.2f : 3.4f);
            float a = strokeAlpha >= 0f ? strokeAlpha : (nervioso ? 0.80f : 0.85f);
            for (int s = 0; s < strokes.Length; s += 2)
            {
                Vector2 pa = glyphPos + strokes[s] * glyphScale;
                Vector2 pb = glyphPos + strokes[s + 1] * glyphScale;
                Vector2 mid = (pa + pb) * 0.5f;
                Vector2 delta = pb - pa;
                float len = delta.Length();
                if (len < 0.01f) continue;
                float rot = (float)Math.Atan2(delta.Y, delta.X);

                // Gradiente vertical: abajo cuerpo, arriba punta pálida.
                float localY = ((strokes[s].Y + strokes[s + 1].Y) * 0.5f + 7f) / 14f;
                Color col = Color.Lerp(tip, body, 1f - localY * 0.25f);

                Capsule(mid, len, w * glyphScale, rot, Tint(col, a * pulse * alphaMul));
            }

            // PERLA sobre el glifo (la gema de la corona).
            float pearlOff = nervioso ? 10.5f : 11.5f;
            float pearlA = nervioso ? 6.2f : 7.0f;
            float pearlB = nervioso ? 2.9f : 3.2f;
            Vector2 pearlPos = glyphPos - new Vector2(0f, pearlOff * glyphScale);
            // El nervioso funde la perla con el latido del glifo; el
            // sereno le da su PROPIO latido (0.8+0.2 a 3 Hz).
            float pearlPulse = nervioso
                ? pulse
                : 0.8f + 0.2f * (float)Math.Sin(time * 3.0f + g * 2.0f);
            Color cTip = pearlTip ?? tip;
            Quad(Glow, pearlPos, new Vector2(pearlA * glyphScale, pearlA * glyphScale), 0f,
                Tint(body, (nervioso ? 0.55f : 0.62f) * pulse * alphaMul));
            Quad(Glow, pearlPos, new Vector2(pearlB * glyphScale, pearlB * glyphScale), 0f,
                Tint(cTip, 0.9f * pearlPulse * alphaMul));
        }

        // ==================================================================
        //  PRIMITIVA RÚNICA 2 — EL CÍRCULO DE CONJURO (el anillo completo)
        // ==================================================================

        /// <summary>
        /// EL CÍRCULO DE CONJURO — EL ANILLO RÚNICO DEL AGUJERO NEGRO, la
        /// firma de los vórtices (la petición completada v6.37): el ARO
        /// FINO de pauta girando con el conjunto y las runas DE PIE
        /// cabalgándolo — el radio RESPIRA por glifo (2.4·gs a 1.35 Hz),
        /// el glifo se MECE (2.0·gs a 0.85 Hz), cada runa arde con su
        /// resplandor y su PERLA. La técnica exacta del Supremo 1:1.
        ///
        /// CONTRATO (Sección A): batch ABIERTO en aditivo → ABIERTO.
        /// </summary>
        /// <param name="center">Centro (coords de PANTALLA).</param>
        /// <param name="r">Radio del vórtice (px — el aro vive a radius·r).</param>
        /// <param name="time">Tiempo animado.</param>
        /// <param name="radius">Radio del aro (×r — la casa: 2.02/2.62/3.30).</param>
        /// <param name="count">Runas del círculo (la casa: 6/8/12).</param>
        /// <param name="orbit">Velocidad angular del conjunto (rad/s — negativa = contrarroto).</param>
        /// <param name="body">Color del cuerpo de runa (y del aro).</param>
        /// <param name="tip">Color de la punta pálida.</param>
        /// <param name="glyphScale">Escala FINAL de los glifos (la casa: Max(r/52, 0.25)·1.40).</param>
        /// <param name="alphabet">Alfabeto de runas (null = RunasVacio, el del Supremo).</param>
        /// <param name="offset">Desfase del alfabeto (los círculos del conjunto empiezan en runas distintas).</param>
        /// <param name="stride">Paso del alfabeto (1 = seguido; 3 = los glifos desfasados de los nerviosos).</param>
        /// <param name="alphaMul">Multiplicador global de intensidad.</param>
        /// <param name="ringAlpha">Alfa del aro de pauta (la casa: 0.24/0.20/0.18).</param>
        /// <param name="nervioso">El humor contrarrotante de los Ascendidos (respiración y latido propios).</param>
        /// <param name="strokeW">Grosor del trazo ×gs (−1 = el del humor).</param>
        /// <param name="strokeAlpha">Alfa del trazo (−1 = el del humor).</param>
        /// <param name="pearlTip">Color de la perla (null = tip).</param>
        public static void CirculoRunico(Vector2 center, float r, float time,
            float radius, int count, float orbit, Color body, Color tip,
            float glyphScale, Vector2[][] alphabet = null, int offset = 0,
            int stride = 1, float alphaMul = 1f, float ringAlpha = 0.24f,
            bool nervioso = false, float strokeW = -1f, float strokeAlpha = -1f,
            Color? pearlTip = null)
        {
            if (count <= 0 || r < 2f || alphaMul <= 0.02f) return;

            // El aro fino de pauta (gira con el conjunto — Ring.png).
            AnilloFino(center, radius * r, time * orbit, Tint(body, ringAlpha * alphaMul));

            // Flotación viva: los humores respiran distinto.
            float breatheAmp = nervioso ? 2.2f : 2.4f;
            float breatheHz = nervioso ? 1.1f : 1.35f;
            float breathePh = nervioso ? 1.4f : 0.9f;
            float bobAmp = nervioso ? 1.8f : 2.0f;
            float bobHz = nervioso ? 0.95f : 0.85f;
            float bobPh = nervioso ? 2.1f : 1.7f;

            // El alfabeto (defensa de librería pública: nada de tablas vacías).
            Vector2[][] tabla = alphabet ?? RunasVacio;
            if (tabla.Length == 0) return;

            for (int g = 0; g < count; g++)
            {
                float ang = g / (float)count * MathHelper.TwoPi + time * orbit;

                // El radio respira por glifo y el glifo se mece.
                float floatR = radius * r +
                               breatheAmp * glyphScale * (float)Math.Sin(time * breatheHz + g * breathePh);
                float bobY = bobAmp * glyphScale * (float)Math.Sin(time * bobHz + g * bobPh);
                Vector2 glyphPos = center + new Vector2(
                    (float)Math.Cos(ang) * floatR,
                    (float)Math.Sin(ang) * floatR + bobY);

                // La runa de la posición g (stride/offset: los conjuntos
                // empiezan en glifos distintos y los nerviosos los saltan).
                Vector2[] strokes = tabla[(g * stride + offset) % tabla.Length];

                RunaVacia(glyphPos, time, g, strokes, body, tip, glyphScale,
                    alphaMul, nervioso, strokeW, strokeAlpha, pearlTip);
            }
        }

        // ==================================================================
        //  PRIMITIVA RÚNICA 3 — EL SIGILO EROSIONADO (la variante del Umbral)
        // ==================================================================

        /// <summary>
        /// EL SIGILO EROSIONADO — el círculo rúnico ROTO del Umbral: el
        /// aro deshecho en segmentos de cápsula con HUECOS por hash (el
        /// sigilo devorado), los glifos PERDIDOS al azar determinista, la
        /// PÚA que APAGA las runas que cruza y las brasas latiendo por
        /// tramo. La escritura del abismo, 1:1 del Umbral.
        ///
        /// CONTRATO (Sección A): batch ABIERTO en aditivo → ABIERTO.
        /// </summary>
        /// <param name="center">Centro (coords de PANTALLA).</param>
        /// <param name="r">Radio del vórtice (px).</param>
        /// <param name="time">Tiempo animado.</param>
        /// <param name="seed">Semilla determinista (los huecos).</param>
        /// <param name="radius">Radio del aro (×r — la casa: 2.55/1.95).</param>
        /// <param name="count">Runas del círculo (la casa: 12/8).</param>
        /// <param name="orbit">Velocidad angular (rad/s — lento: 0.06).</param>
        /// <param name="glyphScale">Escala FINAL (la casa: Max(r/52, 0.25)·1.75).</param>
        /// <param name="body">Color del cuerpo (la casa: dorado).</param>
        /// <param name="tip">Color de la punta pálida.</param>
        /// <param name="alphabet">Alfabeto (null = RunasAbismo, el del Umbral).</param>
        /// <param name="offset">Desfase del alfabeto (el segundo anillo: 500).</param>
        /// <param name="hashOff">Desfase del hash (el segundo anillo hueca DISTINTO: 500).</param>
        /// <param name="spikeAngle">Ángulo de la púa (rad — lo que cruza, se apaga; pasa SpikeAngle+RingTilt).</param>
        /// <param name="spikeWindow">Ventana de la púa (rad — la casa: 0.45/0.30).</param>
        public static void SigiloErosionado(Vector2 center, float r, float time, int seed,
            float radius, int count, float orbit, float glyphScale,
            Color body, Color tip, Vector2[][] alphabet = null, int offset = 0,
            int hashOff = 0, float spikeAngle = -0.55f, float spikeWindow = 0.45f)
        {
            if (count <= 0 || r < 2f) return;
            float circleR = radius * r;
            float giro = time * orbit;

            // El alfabeto (defensa de librería pública: nada de tablas vacías).
            Vector2[][] tabla = alphabet ?? RunasAbismo;
            if (tabla.Length == 0) return;

            // --- EL ARO ROTO: segmentos de cápsula con HUECOS por hash ---
            for (int s = 0; s < ErosionSegmentos; s++)
            {
                float h = VFXCore.Hash01(seed, 940 + s, 3 + hashOff);
                if (h < 0.30f) continue;   // HUECO: el tramo no existe

                float ta = s / (float)ErosionSegmentos * MathHelper.TwoPi + giro;
                float tb = (s + 1) / (float)ErosionSegmentos * MathHelper.TwoPi + giro;
                Vector2 pa = center + new Vector2(
                    (float)Math.Cos(ta) * circleR, (float)Math.Sin(ta) * circleR);
                Vector2 pb = center + new Vector2(
                    (float)Math.Cos(tb) * circleR, (float)Math.Sin(tb) * circleR);
                Vector2 mid = (pa + pb) * 0.5f;
                Vector2 d = pb - pa;
                float len = d.Length();
                if (len < 0.5f) continue;
                float rot = (float)Math.Atan2(d.Y, d.X);

                // Brasa viva: cada tramo late con su propia fase.
                float pulse = 0.55f + 0.45f * (float)Math.Sin(time * 1.6f + s * 1.1f);
                float fade = 0.35f + 0.65f * h;
                Capsule(mid, len, 1.7f * glyphScale, rot,
                    Tint(body, 0.42f * pulse * fade));
            }

            // --- LOS GLIFOS: dorados, finos, CON HUECOS ---
            for (int g = 0; g < count; g++)
            {
                float ang = g / (float)count * MathHelper.TwoPi + giro;

                // Flotación viva: el radio respira por glifo.
                float floatR = circleR +
                               2.2f * glyphScale * (float)Math.Sin(time * 1.2f + g * 0.9f);
                float bobY = 1.8f * glyphScale * (float)Math.Sin(time * 0.8f + g * 1.7f);
                Vector2 glyphPos = center + new Vector2(
                    (float)Math.Cos(ang) * floatR,
                    (float)Math.Sin(ang) * floatR + bobY);

                // LOS HUECOS DE LA REFERENCIA: glifos PERDIDOS por hash
                // (sigilo erosionado) y glifos APAGADOS donde la PÚA cruza.
                float gapRoll = VFXCore.Hash01(seed, 960 + g, 5 + hashOff);
                bool nearSpike = Math.Abs(MathHelper.WrapAngle(
                    ang - spikeAngle)) < spikeWindow;

                float presence = gapRoll < 0.18f ? 0f            // hueco total
                              : nearSpike ? 0.25f                 // atravesado por la púa
                              : 1f;
                if (presence <= 0f) continue;

                // Latido de brillo propio por glifo.
                float pulse = (0.70f + 0.30f * (float)Math.Sin(time * 2.2f + g * 1.3f))
                              * presence;

                // Resplandor suave DETRÁS (el "grabado a láser" ardiendo).
                Quad(Glow, glyphPos, new Vector2(30f * glyphScale, 30f * glyphScale), 0f,
                    Tint(body, 0.18f * pulse));

                // Trazos FINOS: cápsulas finísimas de grabado.
                Vector2[] strokes = tabla[(g + offset) % tabla.Length];
                for (int s = 0; s < strokes.Length; s += 2)
                {
                    Vector2 pa = glyphPos + strokes[s] * glyphScale;
                    Vector2 pb = glyphPos + strokes[s + 1] * glyphScale;
                    Vector2 mid = (pa + pb) * 0.5f;
                    Vector2 delta = pb - pa;
                    float len = delta.Length();
                    if (len < 0.01f) continue;
                    float rot = (float)Math.Atan2(delta.Y, delta.X);

                    // Gradiente vertical: abajo cuerpo, arriba punta pálida.
                    float localY = ((strokes[s].Y + strokes[s + 1].Y) * 0.5f + 7f) / 14f;
                    Color col = Color.Lerp(tip, body, 1f - localY * 0.25f);

                    Capsule(mid, len, 3.2f * glyphScale, rot, Tint(col, 0.95f * pulse));
                }

                // PERLA dorada sobre el glifo (la gema erosionada — el
                // blanco cálido de la casa del abismo).
                Vector2 pearlPos = glyphPos - new Vector2(0f, 11.0f * glyphScale);
                Quad(Glow, pearlPos, new Vector2(5.6f * glyphScale, 5.6f * glyphScale), 0f,
                    Tint(body, 0.50f * pulse));
                Quad(Glow, pearlPos, new Vector2(2.6f * glyphScale, 2.6f * glyphScale), 0f,
                    Tint(new Color(255, 235, 195), 0.85f * pulse));
            }
        }

        // ==================================================================
        //  EL COMPUESTO RÚNICO — LA CORONA DE CONJURO (la triple corona)
        // ==================================================================

        /// <summary>
        /// LA CORONA DE CONJURO — la TRIPLE CORONA del agujero SUPREMO
        /// invocable en una llamada (la firma rúnica de los vórtices, la
        /// petición completada v6.37): el círculo DORADO de 8 runas
        /// girando CW con el conjunto, el círculo VIOLETA de 6 runas MÁS
        /// AFUERA contrarrotando (el contrarroto arcano) y el círculo
        /// BLANCO de 6 runas íntimas rápido (entre el anillo de fotones y
        /// el dorado — la corona viva). Escala de glifo del Supremo.
        ///
        /// CONTRATO (Sección A): batch ABIERTO en aditivo → ABIERTO.
        /// </summary>
        /// <param name="center">Centro (coords de PANTALLA).</param>
        /// <param name="r">Radio del vórtice (px — el aro externo llega a 3.30×).</param>
        /// <param name="time">Tiempo animado.</param>
        /// <param name="alphaMul">Multiplicador global de intensidad.</param>
        /// <param name="glyphMul">Multiplicador de la escala de glifo (1 = el calibre del Supremo; la espalda del jugador usa ~2.6 para que las runas SE LEAN).</param>
        /// <param name="alphabet">Alfabeto (null = RunasVacio).</param>
        /// <param name="blanco">Par cuerpo del círculo íntimo (null = RunaBlanca).</param>
        /// <param name="dorado">Par cuerpo del círculo medio (null = RunaDorada).</param>
        /// <param name="violeta">Par cuerpo del círculo externo (null = RunaVioleta).</param>
        public static void CoronaConjuro(Vector2 center, float r, float time,
            float alphaMul = 1f, float glyphMul = 1f, Vector2[][] alphabet = null,
            Color? blanco = null, Color? dorado = null, Color? violeta = null)
        {
            if (r < 2f || alphaMul <= 0.02f) return;

            float glyphScale = Math.Max(r / GlifoCalibre, 0.25f) * GlifoMul * glyphMul;
            Color cBlanco = blanco ?? RunaBlanca;
            Color cDorado = dorado ?? RunaDorada;
            Color cVioleta = violeta ?? RunaVioleta;

            // EL CÍRCULO DORADO — 8 runas girando CW (con el conjunto).
            CirculoRunico(center, r, time, AroMedio, RunasMedias, GiroMedio,
                cDorado, RunaDoradaTip, glyphScale, alphabet, offset: 0,
                alphaMul: alphaMul, ringAlpha: 0.24f);

            // EL CÍRCULO VIOLETA — 6 runas MÁS AFUERA girando CCW
            // (el contrarroto arcano de la fusión).
            CirculoRunico(center, r, time, AroExterno, RunasExternas, GiroExterno,
                cVioleta, RunaVioletaTip, glyphScale * 0.85f, alphabet, offset: 3,
                alphaMul: alphaMul, ringAlpha: 0.18f);

            // EL CÍRCULO BLANCO — 6 runas MÁS ADENTRO, rápido e íntimo
            // (entre el anillo de fotones y el dorado).
            CirculoRunico(center, r, time, AroIntimo, RunasIntimas, GiroIntimo,
                cBlanco, RunaBlancaTip, glyphScale * 0.92f, alphabet, offset: 6,
                alphaMul: alphaMul, ringAlpha: 0.20f);
        }

        // ==================================================================
        //  EL COMPUESTO — EL SELLO DEL VACÍO (el signo invocable, NUEVO)
        // ==================================================================

        /// <summary>
        /// EL SELLO DEL VACÍO — EL SIGNO MÁGICO INVOCABLE de la casa
        /// oscura (el par de SelloSolar): el círculo completo del vórtice —
        /// anillo energético con SUS veinte bandas viajando (ambas
        /// mitades), los ecos en resonancia, los fotones corriendo el
        /// aro, el ANILLO DE FOTONES del horizonte y las ondas de
        /// distorsión pulsando. Sin núcleo negro: SOLO el signo — el
        /// consumidor lo pone debajo de lo que quiera (o encima de un
        /// cuerpo, un portal, un altar, un jefe).
        ///
        /// CONTRATO (Sección A): abre y CIERRA su propio batch — el
        /// SpriteBatch del llamador debe estar CERRADO al llamar (y queda
        /// CERRADO al salir), igual que el render completo del vórtice.
        /// </summary>
        /// <param name="center">Centro en coords de PANTALLA (mundo − Main.screenPosition).</param>
        /// <param name="radius">Radio del vórtice (px — el anillo llega a ~1.9×).</param>
        /// <param name="time">Tiempo animado (GlobalTimeWrappedHourly).</param>
        /// <param name="seed">Semilla determinista.</param>
        /// <param name="vivo">Color vivo del anillo (la casa: rojo-naranja).</param>
        /// <param name="profundo">Color profundo (valle de banda).</param>
        /// <param name="caliente">Color del pico incandescente.</param>
        /// <param name="alpha">Multiplicador global de intensidad.</param>
        public static void SelloVacio(Vector2 center, float radius, float time, int seed,
            Color? vivo = null, Color? profundo = null, Color? caliente = null,
            float alpha = 1f)
        {
            if (radius < 2f || alpha <= 0.02f) return;
            Color cVivo = (vivo ?? RingVivo) * alpha;
            Color cDeep = profundo ?? RingProfundo;
            Color cHot = caliente ?? HotWhite;

            try
            {
                int flick = (int)(time * FlickHz);
                float distortion = (float)Math.Sin(time) * DistorsionFuerza;
                float rr = radius * Distorsion(time);

                AbrirAdditive();

                // --- 1. LOS ECOS en resonancia (la resonancia del shader) ---
                EcosAnillo(center, rr, time, distortion, cVivo, cDeep);

                // --- 2. EL ANILLO ENERGÉTICO, mitad TRASERA ---
                AnilloEnergia(center, rr, time, seed, flick, front: false,
                    hot: cHot, mid: cVivo, deep: cDeep);

                // --- 3. EL ANILLO DE FOTONES del horizonte (el aro fino) ---
                AnilloFino(center, 1.02f * radius, time * 0.15f,
                    Tint(cVivo, 0.40f + 0.12f * (float)Math.Sin(time * 1.7f)));

                // --- 4. EL ANILLO ENERGÉTICO, mitad DELANTERA ---
                AnilloEnergia(center, rr, time, seed, flick, front: true,
                    hot: cHot, mid: cVivo, deep: cDeep);

                // --- 5. LOS FOTONES corriendo el vórtice ---
                Fotones(center, rr, time, FotonesCount, cVivo, cHot);

                // --- 6. LAS ONDAS DE DISTORSIÓN pulsando ---
                OndasDistorsion(center, radius, time, seed, 2, 2.4f, cVivo);

                CerrarBatch();
                // v6.50.11 — el lote sale ABIERTO y vanilla (el contrato
                // de curación; era "CERRADO al salir" en v6.49).
            }
            catch
            {
                // Cierre defensivo SOLO en el path de error — por la
                // PUERTA BLINDADA (v6.49): respeta al lote ajeno si lo
                // había (le devuelve su estado en vez de cerrarlo a ciegas).
                try { CerrarBatch(); } catch { }
            }
        }
    }
}
