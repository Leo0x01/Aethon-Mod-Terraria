using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// TajoLib — v6.40 → v6.172 — LA ESCRITURA DEL FILORTE (los tajos
    /// del anime), REFORJADA con la investigación profunda de la v6.172
    /// (la letra: «los tajos reales no son asi, necesito que hagas una
    /// investigacion profunda en todo internet […] hazle tantas
    /// mejoras como puedas»).
    ///
    /// LO QUE LA INVESTIGACIÓN ENCONTRÓ (y la librería ya tenía, medido
    /// del canon sakuga — research/tajos_v640):
    ///   · LA FORMA: un CRECIENTE con el GROSOR MODULADO — máximo al
    ///     centro, fino en las puntas (el perfil de lente G(u)=sin(π·u)^0.55).
    ///   · LAS BANDAS UNIFORMES (quads rotados a la tangente + solape
    ///     de 2 px): arcos CONTINUOS, sin collares de perlas.
    ///   · EL LOTE ADITIVO SourceAlpha/One: la intensidad vive en el
    ///     alfa del tinte — el brillo responde LINEAL.
    ///   · EL PARPADEO SOLO EN EL HALO, jamás en el núcleo.
    ///   · VIDA ULTRACORTA (10-16 t): el corte OCURRE, no se desarrolla.
    ///   · REVELADO DIRECCIONAL con POP elástico (easeOutBack).
    ///   · DETERMINISMO MP: Hash01, jamás Main.rand en el render.
    ///
    /// LO NUEVO DE LA v6.172 (la investigación de los tajos REALES —
    /// el canon de Hollow Knight / Guilty Gear / Katana Zero / los
    /// impact frames del sakuga / los smear frames del anime):
    ///   1. EL DOBLE FILO — DOS crecientes concéntricos (el exterior
    ///      ×1.0 y el interior ×0.82) revelados en SENTIDOS OPUESTOS:
    ///      la firma del slash de Hollow Knight (el filo doble que
    ///      lee «corte» a cualquier velocidad).
    ///   2. EL FRAME DE IMPACTO — 1-2 ticks de flash MONOCROMO (blanco
    ///      puro) al nacer: el «impact frame» del anime (monochrome
    ///      flash hidden in the sequence, la definición del Sakuga
    ///      Blog) — el clac que el ojo lee ANTES que la media luna.
    ///   3. LOS SMEARS (multi-hoja) — hojas fantasma DELGADAS que
    ///      ADELANTAN el arco con desfase angular: el «smear frame»
    ///      del swing animado (la hoja estirada en arco, la técnica
    ///      documentada de la animación clásica — sword swing =
    ///      triangle/arc elongation).
    ///   4. LAS CHISPAS DEL FILO EXTERIOR — partículas deterministas
    ///      que nacen SOLO en el borde de AFUERA del creciente (la
    ///      falda de chispas del corte, no motas sueltas dentro).
    ///   5. EL ARCO ELÍPTICO — el tajo anime es una ELIPSE inclinada
    ///      (la katana entra en diagonal al plano), no un círculo:
    ///      el radio se aplasta ×EscalaY (0.78 por defecto) — el
    ///      «corte en perspectiva» de las 1.4.4 de Terraria.
    ///   6. LA VARIANTE CRUZADA (TajoCruzado) — DOS tajos en X (el
    ///      doble corte cruzado del combo anime, el «double slash»
    ///      de Guilty Gear Strive).
    ///
    /// EL TINTE: RGB intacto, alfa = f (el de las librerías rúnicas):
    /// el lote aditivo REAL de FNA (SourceAlpha/One — verificado contra
    /// el IL del FNA.dll en v6.40) GATEA por el alfa → el brillo responde
    /// LINEAL a la intensidad. Cero Main.rand: todo el azar vive en Hash01.
    ///
    /// CONTRATO (Sección A de la casa): batch ABIERTO en aditivo →
    /// ABIERTO. Coordenadas de PANTALLA (el llamador resta
    /// Main.screenPosition; el batch lleva la GameViewMatrix).
    /// </summary>
    public static class TajoLib
    {
        // ==================================================================
        //  EL PINCEL (v6.50.22 — EL PIXEL DEL MOTOR: el mismo defecto de
        //  SUELO de alfa en los bordes que los rayos; el filo del tajo
        //  pasa a ser 100% código con pasadas apiladas)
        // ==================================================================

        /// <summary>El pincel de banda — v6.50.22: EL PIXEL 1×1 del motor
        /// (la banda horneada BoltHalo tenía SUELO de alfa ~50 en los
        /// bordes largos — bordes duros medidos por píxeles).</summary>
        private static Texture2D Banda => VFXCore.Pixel;

        /// <summary>El pincel del filo — v6.50.22: EL PIXEL del motor (el
        /// filo es ahora un rectángulo CRISPO — como conviene a una
        /// hoja).</summary>
        private static Texture2D Vena => VFXCore.Pixel;

        private static Asset<Texture2D> _glow;    // SoftGlow: los puntos

        private static Texture2D Glow =>
            (_glow ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow")).Value;

        /// <summary>Tinte de intensidad (RGB intacto, alfa = f — el de las
        /// librerías rúnicas: el lote aditivo real GATEA por el alfa).</summary>
        public static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color(c.R, c.G, c.B, (byte)(int)(255f * f));
        }

        // ==================================================================
        //  LA PRIMITIVA — EL Tajo (la media luna completa)
        // ==================================================================

        /// <summary>Segmentos del arco (20: curva suave con deltas de
        /// ángulo pequeños — los quads banda quedan continuos).</summary>
        public const int Segmentos = 20;

        /// <summary>v6.172 — LA COMPRESIÓN VERTICAL de la elipse (0.78:
        /// el corte katana entra EN DIAGONAL al plano — el arco circular
        /// puro lee «bumerán», el achatado lee «corte en perspectiva»).</summary>
        public const float EscalaY = 0.78f;

        /// <summary>
        /// UN Tajo — la media luna del anime en UN instante de su vida.
        /// (LA FIRMA DE SIEMPRE — compatible con todos los llamadores
        /// viejos: TajoAstralProjectile, LineaDelCielo…)
        ///
        /// CONTRATO: batch ABIERTO en aditivo → ABIERTO.
        /// </summary>
        /// <param name="centro">Centro del círculo del arco (coords de PANTALLA).</param>
        /// <param name="radio">Radio del arco (px — crece con la vida si el llamador lo escala).</param>
        /// <param name="ang0">Ángulo inicial (rad — el revelado EMPIEZA aquí).</param>
        /// <param name="ang1">Ángulo final (rad — el revelado TERMINA aquí: invierte la pareja para invertir el sentido).</param>
        /// <param name="prog">Progreso del revelado 0..1 (los segmentos nacen por orden).</param>
        /// <param name="brillo">La campana de vida 0..1 (la lleva el llamador).</param>
        /// <param name="ancho">Grosor VISIBLE del filo (px, máximo en el centro del arco).</param>
        /// <param name="halo">Color del halo (el color del tajo).</param>
        /// <param name="nucleo">Color del núcleo (blanco-caliente de la casa).</param>
        /// <param name="seed">Semilla determinista (puntas y parpadeo).</param>
        /// <param name="time">Tiempo animado (el parpadeo del halo).</param>
        public static void Tajo(Vector2 centro, float radio, float ang0, float ang1,
            float prog, float brillo, float ancho, Color halo, Color nucleo,
            int seed, float time)
        {
            // v6.172 — LA MASA COMPLETA: el tajo viejo (una sola luna)
            // ahora es TODO EL CANON: el frame de impacto lo lleva el
            // LLAMADOR (necesita saber la edad ABSOLUTA); aquí viven el
            // DOBLE FILO, los SMEARS, las CHISPAS exteriores y la
            // ELIPSE — la geometría nueva con la firma de siempre.
            TajoDoble(centro, radio, ang0, ang1, prog, brillo, ancho,
                halo, nucleo, seed, time, 1f, 0f);
        }

        /// <summary>
        /// v6.172 — EL Tajo DE LA INVESTIGACIÓN: el canon completo.
        ///
        /// Lo nuevo sobre el Tajo() de siempre:
        ///  · EL DOBLE FILO — el creciente exterior se revela ang0→ang1
        ///    y el interior (×0.82) CORRE EN SENTIDO OPUESTO: la firma
        ///    del slash de Hollow Knight (dos arcos que se cruzan).
        ///  · LOS SMEARS — hasta 3 hojas fantasma (ancho ×0.3, alfa bajo)
        ///    a radios intermedios con desfase angular decreciente: el
        ///    smear frame del swing (la hoja «estirada» del anime).
        ///  · LAS CHISPAS DEL FILLO EXTERIOR — puntitos deterministas
        ///    que nacen SOLO en el borde de afuera (la falda del corte).
        ///  · LA ELIPSE — EscalaY aplasta el arco: el corte entra en
        ///    diagonal al plano (la perspectiva de la katana).
        ///  · `impacto` = la EDAD RELATIVA del nacimiento (0..1): los
        ///    primeros 2 ticks de vida pintan EL FRAME DE IMPACTO (el
        ///    flash monocromo del sakuga — blanco puro, un instante).
        /// </summary>
        public static void TajoDoble(Vector2 centro, float radio, float ang0, float ang1,
            float prog, float brillo, float ancho, Color halo, Color nucleo,
            int seed, float time, float smeares, float impacto)
        {
            if (brillo <= 0.02f || radio < 6f || ancho < 0.6f) return;
            prog = MathHelper.Clamp(prog, 0f, 1f);
            if (prog <= 0.01f) return;

            float span = ang1 - ang0;

            // --- 0. EL FRAME DE IMPACTO (v6.172 — la investigación: el
            //     «impact frame» del sakuga: un dibujo MONOCROMO escondido
            //     1-2 frames dentro de la secuencia — el golpe LEE antes
            //     que el filo). El LLAMADOR pasa su edad (0..1 del total);
            //     los 2 primeros ticks = flash blanco PURO en el centro,
            //     más grueso que el filo y SIN forma: el instante del
            //     contacto. ---
            if (impacto > 0f && impacto < 0.28f)
            {
                float f = 1f - impacto / 0.28f;          // 1 → 0 en el parpadeo
                // EL FLASH MONOCROMO: blanco puro que ALCANZA el radio
                // del arco (la pantalla entera del corte, un frame)
                Quad(Glow, centro, new Vector2(radio * 0.55f, radio * 0.34f * EscalaY),
                    (ang0 + ang1) * 0.5f + MathHelper.PiOver2,
                    Tint(nucleo, 0.85f * f));
                // LA CRUZ DEL CONTACTO (dos agujas finas en el eje del
                // corte — el sketch del impact frame)
                Quad(Glow, centro, new Vector2(radio * 1.05f, ancho * 0.9f),
                    (ang0 + ang1) * 0.5f, Tint(nucleo, 0.6f * f));
                Quad(Glow, centro, new Vector2(ancho * 0.9f, radio * 1.05f * EscalaY),
                    (ang0 + ang1) * 0.5f, Tint(nucleo, 0.5f * f));
            }

            // --- 0.5 LOS SMEARS (v6.172 — la multi-hoja del swing
            //     animado: «la hoja se estira en arco» — el smear frame
            //     clásico). Hojas FANTASMA finísimas a radios ±6 %,
            //     desfase angular decreciente, reveladas UN PASO ADELANTE
            //     del filo real (la estela del gesto). ---
            if (smeares > 0.05f)
            {
                for (int s = 0; s < 3; s++)
                {
                    float h = VFXCore.Hash01(seed, 29 + s, 5);
                    if (h > 0.62f * smeares + 0.34f) continue;   // 0-3 según smeares
                    float rS = radio * (1f + (s % 2 == 0 ? 0.065f : -0.055f));
                    float desfase = (s + 1) * span * 0.055f;
                    float progS = MathHelper.Clamp(prog * 1.22f, 0f, 1f);  // ADELANTADO
                    for (int i = 0; i < Segmentos; i++)
                    {
                        float u0 = i / (float)Segmentos;
                        if (u0 >= progS) break;
                        float um = (u0 + (i + 1f) / Segmentos) * 0.5f;
                        float gu = PerfilLente(um);
                        SegmentoArco(Banda, centro, rS, ang0 + desfase, span, u0, um,
                            ancho * 1.05f * gu,
                            Tint(nucleo, 0.10f * brillo * (0.4f + 0.6f * gu) *
                                (1f - s * 0.28f)));
                    }
                }
            }

            // --- 1. EL ECO FANTASMA (el rastro interior del anime: un
            //     segundo arco a radio ×0.88, finísimo, apagándose). ---
            for (int i = 0; i < Segmentos; i++)
            {
                float u0 = i / (float)Segmentos;
                if (u0 >= prog) break;
                float um = (u0 + (i + 1f) / Segmentos) * 0.5f;
                float gu = PerfilLente(um);
                SegmentoArco(Banda, centro, radio * 0.88f, ang0, span, u0, um,
                    ancho * 1.9f * gu,
                    Tint(nucleo, 0.14f * brillo * (0.4f + 0.6f * gu)));
            }

            // --- 2. EL HALO (el color: DOS PASADAS APILADAS del pixel del
            //     motor — el degradado transversal ES LA SUMA, la receta
            //     v6.50.22; PARPADEA — el parpadeo vive SOLO aquí, nunca
            //     en el núcleo). ---
            for (int i = 0; i < Segmentos; i++)
            {
                float u0 = i / (float)Segmentos;
                if (u0 >= prog) break;
                float um = (u0 + (i + 1f) / Segmentos) * 0.5f;
                float gu = PerfilLente(um);
                float flick = 0.82f + 0.18f *
                    (float)Math.Sin(time * 38f + i * 2.7f + seed % 7);
                SegmentoArco(Banda, centro, radio, ang0, span, u0, um,
                    ancho * 6.2f * (0.45f + 0.55f * gu),
                    Tint(halo, 0.08f * brillo * flick * (0.35f + 0.65f * gu)));
                SegmentoArco(Banda, centro, radio, ang0, span, u0, um,
                    ancho * 3.4f * (0.45f + 0.55f * gu),
                    Tint(halo, 0.13f * brillo * flick * (0.35f + 0.65f * gu)));
            }

            // --- 3. EL NÚCLEO (el filo blanco-caliente: BoltCore fino,
            //     limpio, sin parpadeo, el grosor de la lente). ---
            for (int i = 0; i < Segmentos; i++)
            {
                float u0 = i / (float)Segmentos;
                if (u0 >= prog) break;
                float um = (u0 + (i + 1f) / Segmentos) * 0.5f;
                float gu = PerfilLente(um);
                SegmentoArco(Vena, centro, radio, ang0, span, u0, um,
                    ancho * 2.8f * (0.40f + 0.60f * gu),
                    Tint(nucleo, 0.95f * brillo * (0.40f + 0.60f * gu)));
            }

            // --- 3.5 EL DOBLE FILO (v6.172 — la firma de Hollow Knight:
            //     un SEGUNDO creciente a radio ×0.82 que corre EN
            //     SENTIDO OPUESTO — los dos filos se cruzan en el
            //     centro del gesto). Nace cuando el filo principal ya
            //     recorrió un tercio: la segunda hoja DEL cierre. ---
            if (prog > 0.30f)
            {
                float prog2 = MathHelper.Clamp((prog - 0.30f) / 0.70f, 0f, 1f);
                for (int i = 0; i < Segmentos; i++)
                {
                    // EL SENTIDO INVERSO: los segmentos nacen del otro
                    // extremo (ang1 → ang0)
                    float u0 = i / (float)Segmentos;
                    if (u0 >= prog2) break;
                    float um = (u0 + (i + 1f) / Segmentos) * 0.5f;
                    float gu = PerfilLente(um);
                    float flick = 0.82f + 0.18f *
                        (float)Math.Sin(time * 33f + i * 2.3f + seed % 5);
                    SegmentoArco(Banda, centro, radio * 0.82f, ang1, -span, u0, um,
                        ancho * 4.6f * (0.45f + 0.55f * gu),
                        Tint(halo, 0.10f * brillo * flick * (0.35f + 0.65f * gu)));
                    SegmentoArco(Vena, centro, radio * 0.82f, ang1, -span, u0, um,
                        ancho * 2.1f * (0.40f + 0.60f * gu),
                        Tint(nucleo, 0.80f * brillo * (0.40f + 0.60f * gu)));
                }
                // EL FRENTE del segundo filo (su punta ardiendo, la
                // del sentido contrario)
                if (prog2 < 1f)
                {
                    float angFront2 = ang1 - span * prog2;
                    Vector2 pF2 = PuntoArco(centro, radio * 0.82f, angFront2);
                    Quad(Glow, pF2, new Vector2(ancho * 2.2f, ancho * 2.2f), 0f,
                        Tint(nucleo, 0.55f * brillo));
                }
            }

            // --- 4. EL FRENTE DEL REVELADO (la punta del crecimiento
            //     ardiendo: el punto + la cruz de luz — SoftGlow, el
            //     pincel CORRECTO para puntos). ---
            if (prog < 1f)
            {
                float angFront = ang0 + span * prog;
                Vector2 pFront = PuntoArco(centro, radio, angFront);
                Quad(Glow, pFront, new Vector2(ancho * 2.6f, ancho * 2.6f), 0f,
                    Tint(nucleo, 0.65f * brillo));
                Quad(Glow, pFront, new Vector2(ancho * 4.8f, ancho * 0.65f), 0f,
                    Tint(nucleo, 0.45f * brillo));
                Quad(Glow, pFront, new Vector2(ancho * 0.65f, ancho * 4.8f), 0f,
                    Tint(nucleo, 0.45f * brillo));

                // v6.172 — LA CHISPA DEL FILO EXTERIOR (la falda del
                // corte): una puntita determinista que ESCAPA hacia
                // afuera del frente (la firma de los tajos «reales» —
                // partículas naciendo del filo, no dentro del arco)
                float hSp = VFXCore.Hash01(seed, 47, (int)(time * 60f) % 97);
                if (hSp > 0.45f)
                {
                    Vector2 dirOut = new Vector2(
                        (float)Math.Cos(angFront), (float)Math.Sin(angFront) * EscalaY);
                    Vector2 pSp = pFront + dirOut * (4f + 6f * hSp);
                    Quad(Glow, pSp, new Vector2(ancho * 1.1f, ancho * 1.1f), 0f,
                        Tint(halo, 0.5f * brillo * hSp));
                }
            }

            // --- 5. LAS PUNTAS (cuando el creciente está completo, las
            //     puntas prenden y sueltan su chispa). ---
            if (prog >= 0.999f)
            {
                PrenderPunta(centro, radio, ang0, ancho, halo, nucleo, brillo, seed);
                PrenderPunta(centro, radio, ang1, ancho, halo, nucleo, brillo, seed + 3);
            }
        }

        /// <summary>
        /// v6.172 — EL Tajo CRUZADO (la variante del combo): DOS medias
        /// lunas EN X — el doble corte cruzado del anime (el «double
        /// slash» de Guilty Gear / el X-slash clásico). El segundo arco
        /// nace DESFASADO (media apertura + 3 ticks de la casa viven en
        /// el prog del llamador) y con SU propia semilla.
        /// </summary>
        public static void TajoCruzado(Vector2 centro, float radio, float ang0, float ang1,
            float prog, float brillo, float ancho, Color halo, Color nucleo,
            int seed, float time, float smeares)
        {
            // EL PRIMER ARCO (el del gesto)
            TajoDoble(centro, radio, ang0, ang1, prog, brillo, ancho,
                halo, nucleo, seed, time, smeares, 0f);
            // EL SEGUNDO (la X: invertido a través del eje medio del
            // primero — su espejo en el gesto del espadachín)
            float medio = (ang0 + ang1) * 0.5f;
            float a0b = 2f * medio - ang1;
            float a1b = 2f * medio - ang0;
            TajoDoble(centro, radio, a1b, a0b, prog, brillo * 0.92f, ancho * 0.92f,
                halo, nucleo, seed + 11, time, smeares * 0.5f, 0f);
        }

        /// <summary>
        /// UN SEGMENTO del arco con la BANDA UNIFORME: el quad cubre el
        /// tramo u0→u1 del arco, girado a la tangente del punto medio, con
        /// el solape de 2 px que empalma los empalmes (la banda no se
        /// desvanece a lo largo — la costura no existe).
        /// v6.172 — LA ELIPSE: PuntoArco aplasta Y ×EscalaY (la katana
        /// entra en diagonal al plano — el corte en perspectiva).
        /// </summary>
        private static void SegmentoArco(Texture2D tex, Vector2 centro, float radio,
            float ang0, float span, float u0, float um, float altoQuad, Color tint)
        {
            if (tint.A == 0) return;
            float aU = ang0 + span * u0;
            float aV = ang0 + span * (u0 + 1f / Segmentos);
            Vector2 pa = PuntoArco(centro, radio, aU);
            Vector2 pb = PuntoArco(centro, radio, aV);
            Vector2 mid = (pa + pb) * 0.5f;
            Vector2 delta = pb - pa;
            float len = delta.Length();
            if (len < 0.05f) return;
            float rot = (float)Math.Atan2(delta.Y, delta.X);

            // La tangente del punto medio (el quad se gira al filo REAL del
            // arco — el solape de 2px cubre la cuña del giro).
            _ = um;
            Main.spriteBatch.Draw(tex, mid, null, tint, rot,
                new Vector2(tex.Width, tex.Height) * 0.5f,
                new Vector2((len + 2f) / tex.Width, altoQuad / tex.Height),
                SpriteEffects.None, 0f);
        }

        /// <summary>
        /// LA PUNTA del creciente completa: el punto de luz + la chispa
        /// que escapa del filo (determinista por Hash01).
        /// </summary>
        private static void PrenderPunta(Vector2 centro, float radio, float ang,
            float ancho, Color halo, Color nucleo, float brillo, int seed)
        {
            Vector2 p = PuntoArco(centro, radio, ang);
            Quad(Glow, p, new Vector2(ancho * 2.2f, ancho * 2.2f), 0f,
                Tint(halo, 0.55f * brillo));
            Quad(Glow, p, new Vector2(ancho * 1.0f, ancho * 1.0f), 0f,
                Tint(nucleo, 0.92f * brillo));

            // La chispa que escapa (hacia afuera del arco).
            float h = VFXCore.Hash01(seed, 17, 3);
            if (h > 0.4f)
            {
                Vector2 dir = new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang));
                Vector2 sp = p + dir * (2f + 3f * h) * (ancho * 0.6f);
                Quad(Glow, sp, new Vector2(ancho * 1.4f, ancho * 1.4f), 0f,
                    Tint(nucleo, 0.5f * brillo * h));
            }
        }

        // ==================================================================
        //  LOS HELPERS (privados, el pincel de la casa)
        // ==================================================================

        /// <summary>El PERFIL DE LENTE del creciente: grosor máximo al
        /// centro, fino en las puntas (la media luna).
        /// v6.49 — POR LUT (hallazgo AUD-B): se llamaba ~60 veces por tajo
        /// por frame (20 segmentos × 3 capas) con Math.Pow cada vez — la
        /// curva completa es UNA tabla de 33 muestras, interpolada.</summary>
        public static float PerfilLente(float u)
        {
            u = MathHelper.Clamp(u, 0f, 1f) * (_lenteLutN - 1);
            int i0 = (int)u;
            int i1 = i0 + 1 < _lenteLutN ? i0 + 1 : i0;
            float k = u - i0;
            return _lenteLut[i0] + (_lenteLut[i1] - _lenteLut[i0]) * k;
        }

        private const int _lenteLutN = 33;
        private static readonly float[] _lenteLut = BuildLenteLut();

        private static float[] BuildLenteLut()
        {
            var lut = new float[_lenteLutN];
            for (int i = 0; i < _lenteLutN; i++)
            {
                float s = (float)Math.Sin(Math.PI * (i / (float)(_lenteLutN - 1)));
                lut[i] = (float)Math.Pow(Math.Max(s, 0.001f), 0.55f);
            }
            return lut;
        }

        /// <summary>Punto del arco (centro + radio + ángulo).
        /// v6.172 — LA ELIPSE: Y comprimida ×EscalaY (el corte anime
        /// vive EN DIAGONAL — el arco circular puro lee bumerán).</summary>
        private static Vector2 PuntoArco(Vector2 centro, float radio, float ang)
            => centro + new Vector2((float)Math.Cos(ang) * radio,
                (float)Math.Sin(ang) * radio * EscalaY);

        /// <summary>Quad centrado al batch actual (tamaño total = size px).</summary>
        private static void Quad(Texture2D tex, Vector2 pos, Vector2 size, float rot, Color tint)
        {
            if (tint.A == 0) return;
            Main.spriteBatch.Draw(tex, pos, null, tint, rot,
                new Vector2(tex.Width, tex.Height) * 0.5f,
                size / new Vector2(tex.Width, tex.Height),
                SpriteEffects.None, 0f);
        }

        /// <summary>
        /// LA CAMPANA DE VIDA del anime (asimétrica: subida rápida al
        /// 55%, caída larga al 45% restante — la medición de la
        /// investigación). t = 0..1 de la vida.
        /// </summary>
        public static float Campana(float t)
        {
            t = MathHelper.Clamp(t, 0f, 1f);
            float subida = MathHelper.Clamp(t / 0.55f, 0f, 1f);
            float caida = MathHelper.Clamp((1f - t) / 0.45f, 0f, 1f);
            return subida * caida;
        }
    }
}
