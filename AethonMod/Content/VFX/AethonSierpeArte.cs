using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// AethonSierpeArte — v6.50.32 — SLIFER, EL DRAGÓN DEL CIELO.
    ///
    /// LA PETICIÓN (literal): «tienes que mejorar el arte del jefe, te
    /// daré unas referencias y mediante código debes replicarlo, usa la
    /// misma técnica de la sierpe, pero mejora la técnica para aumentar
    /// su precisión en cuanto a cómo se ve con la imagen referencia…
    /// debe ser solo código, nada de sprite». LAS REFERENCIAS: el
    /// Dragón Divino de Yu-Gi-Oh! — analizadas a fondo con visión
    /// artificial (arte de carta oficial + arte del anime, 3 pasadas de
    /// despiece anatómico): hocico plateado en cuña (40% del cráneo),
    /// cráneo escarlata en wedge con reborde de ceja pesado, DOS
    /// colmillos sable de marfil que sobresalen de la boca cerrada,
    /// dientes inferiores pequeños, OJOS almendrados rabiosos con
    /// barrido hacia atrás (dorado en el anime · cian en la carta),
    /// EL DIAMANTE AZUL de la frente, LA CORONA de 5 llamas espinosas
    /// barriendo hacia atrás (la central la más alta), cuerpo
    /// serpenteante ESCARLATA con VIENTRE de pizarra oscura, crestas
    /// trapezoidales a lo largo del lomo y DOS ALAS INMENSAS de
    /// murciélago arqueadas al cielo detrás de la cabeza (paño
    /// granate translúcido + huesos rojos vivos + borde de ataque con
    /// brasa).
    ///
    /// LA MEJORA DE LA TÉCNICA (lo que pide: MÁS PRECISIÓN):
    ///   · LOS PINCELES DE CÓDIGO — v6.50.32 genera EN RUNTIME dos
    ///     pinceles geométricos que los pinceles gaussianos no sabían
    ///     dibujar: EL TRIÁNGULO (colmillos, espinas, velas, struts,
    ///     paños — bordes NÍTIDOS con antialias de 1 px) y EL ROMBO
    ///     (la gema de la frente). Se generan desde píxeles puros (un
    ///     array de Color → Texture2D): CERO sprites, y se
    ///     auto-regeneran si el device los pierde (IsDisposed →
    ///     regenerar; el patrón de la casa para device-lost).
    ///   · EL PASE ALFA PASA A SER ANATOMÍA: antes era «vacío oscuro +
    ///     luz»; ahora es CARNE con dos tonos (lomo escarlata + vientre
    ///     de pizarra), sombras de placa por segmento, hocico de acero
    ///     con brillo de cel-shading — la bestia de las referencias es
    ///     MATERIA, no fantasma. El pase aditivo queda para lo que de
    ///     verdad BRILLA: ojos, gema, brasas, fauces.
    ///   · EL ORDEN DEL DIBUJO ES EL ORDEN ANATÓMICO: cuello → fauces
    ///     → mandíbula → cráneo → hocico → dientes → corona → ojos,
    ///     cada pieza tapando la costura de la anterior.
    ///
    /// EL CONTRATO (de siempre): TODAS las posiciones en COORDENADAS
    /// DE PANTALLA (mundo − Main.screenPosition); los bailes de lote
    /// los lleva cada método (pase alfa + pase aditivo, la sonda de la
    /// casa y el lote vanilla devuelto ABIERTO — CerrarBatch).
    /// CONVENCIÓN ANATÓMICA: perp = +90° CCW desde el rumbo — el
    /// VIENTRE es +perp y el LOMO (corona, velas, gema) es −perp; la
    /// criatura es anatómicamente consistente de la cabeza a la cola
    /// (se retuerce como una sierpe de verdad al cabalgar).
    /// </summary>
    public static class AethonSierpeArte
    {
        // ==================================================================
        //  LA PALETA DE SLIFER — verificada contra las referencias (VLM)
        // ==================================================================

        /// <summary>EL LOMO: el escarlata del cuerpo (#B22222).</summary>
        public static readonly Color Escarlata = new(178, 34, 34);

        /// <summary>La SOMBRA escarlata (placas, velas, axilas #5E1410).</summary>
        public static readonly Color EscarlataSombra = new(94, 20, 16);

        /// <summary>El escarlata VIVO (struts del ala, filos #C63E2C).</summary>
        public static readonly Color EscarlataViva = new(198, 62, 44);

        /// <summary>EL VIENTRE: pizarra azul-gris oscura (#2E343A).</summary>
        public static readonly Color Vientre = new(46, 52, 58);

        /// <summary>El filo claro del vientre (la línea ventral).</summary>
        public static readonly Color VientreBorde = new(96, 106, 114);

        /// <summary>EL HOCICO: el acero plateado (#8C949B).</summary>
        public static readonly Color Acero = new(140, 148, 155);

        /// <summary>La sombra del acero (la mandíbula inferior #5E656B).</summary>
        public static readonly Color AceroSombra = new(94, 101, 107);

        /// <summary>El brillo del acero (cel-shading del hocico #BAC1C8).</summary>
        public static readonly Color AceroClaro = new(186, 193, 200);

        /// <summary>LOS COLMILLOS: marfil (#F2ECD8).</summary>
        public static readonly Color Marfil = new(242, 236, 216);

        /// <summary>EL INTERIOR DE LAS FAUCES: negro rojizo.</summary>
        public static readonly Color Fauces = new(26, 14, 12);

        /// <summary>LA BRASA: las puntas de la corona y el fuego interno.</summary>
        public static readonly Color Brasa = new(255, 96, 32);

        /// <summary>LOS OJOS fase 1-2: dorado (el anime).</summary>
        public static readonly Color OjoDorado = new(255, 208, 64);

        /// <summary>LOS OJOS fase 3: cian (el arte de la carta).</summary>
        public static readonly Color OjoCian = new(64, 176, 255);

        /// <summary>LOS OJOS fase 4+: violeta (la Luz Primordial).</summary>
        public static readonly Color OjoVioleta = new(196, 150, 255);

        /// <summary>LA GEMA DE LA FRENTE: el diamante azul (#40C4FF).</summary>
        public static readonly Color Gema = new(64, 196, 255);

        /// <summary>EL PAÑO DEL ALA: granate oscuro translúcido (#601A12).</summary>
        public static readonly Color Membrana = new(96, 26, 18);

        private static Asset<Texture2D> _star;

        /// <summary>La estrella de 4 puntas (las brasas de la casa).</summary>
        private static Texture2D Star =>
            (_star ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/Star")).Value;

        /// <summary>El tinte de intensidad de la casa (delega en OrbitaLib).</summary>
        private static Color T(Color c, float f) => OrbitaLib.Tint(c, f);

        // ==================================================================
        //  LOS PINCELES DE CÓDIGO — v6.50.32 (la mejora de la técnica)
        // ==================================================================

        private static Texture2D _pincelTriangulo;
        private static Texture2D _pincelRombo;

        /// <summary>
        /// LA ESTRELLA DEL FONDO (el leviatán del cielo la pide para sus
        /// apófisis — la misma textura, cacheada y null-segura).
        /// </summary>
        public static Texture2D EstrellaDelFondo()
        {
            try { return Star; }
            catch { return VFXCore.SoftGlow; }
        }

        /// <summary>
        /// EL TRIÁNGULO (64²): blanco, ápice arriba, base abajo, con
        /// antialias de 1 px. Anclado por el CENTRO DE LA BASE — el
        /// <see cref="Tri"/> lo apunta donde se pida. Es el pincel de
        /// TODO lo afilado: colmillos, corona, velas, struts, paños.
        /// Auto-regenerable (device-lost → IsDisposed → regenerar).
        /// </summary>
        private static Texture2D PincelTriangulo
        {
            get
            {
                if (_pincelTriangulo == null || _pincelTriangulo.IsDisposed)
                    _pincelTriangulo = GenerarTriangulo();
                return _pincelTriangulo;
            }
        }

        /// <summary>
        /// EL ROMBO (64²): blanco, |u|+|v| ≤ ½ con antialias de 1 px —
        /// LA GEMA de la frente. Auto-regenerable.
        /// </summary>
        private static Texture2D PincelRombo
        {
            get
            {
                if (_pincelRombo == null || _pincelRombo.IsDisposed)
                    _pincelRombo = GenerarRombo();
                return _pincelRombo;
            }
        }

        private static Texture2D GenerarTriangulo()
        {
            try
            {
                GraphicsDevice g = Main.graphics?.GraphicsDevice;
                if (g == null) return VFXCore.SoftGlow; // menú: el pincel suave
                const int S = 64;
                Color[] px = new Color[S * S];
                for (int y = 0; y < S; y++)
                {
                    float v = (y + 0.5f) / S;          // 0=ápice → 1=base
                    float semiancho = v * 0.5f;          // medio ancho de la fila
                    for (int x = 0; x < S; x++)
                    {
                        float u = MathF.Abs((x + 0.5f) / S - 0.5f);
                        float a = MathHelper.Clamp((semiancho - u) * S + 0.5f, 0f, 1f);
                        px[y * S + x] = a <= 0f ? Color.Transparent : Color.White * a;
                    }
                }
                Texture2D t = new Texture2D(g, S, S);
                t.SetData(px);
                return t;
            }
            catch { return VFXCore.SoftGlow; }
        }

        private static Texture2D GenerarRombo()
        {
            try
            {
                GraphicsDevice g = Main.graphics?.GraphicsDevice;
                if (g == null) return VFXCore.SoftGlow;
                const int S = 64;
                Color[] px = new Color[S * S];
                for (int y = 0; y < S; y++)
                {
                    float v = (y + 0.5f) / S - 0.5f;
                    for (int x = 0; x < S; x++)
                    {
                        float u = (x + 0.5f) / S - 0.5f;
                        float a = MathHelper.Clamp((0.5f - (MathF.Abs(u) + MathF.Abs(v))) * S + 0.5f, 0f, 1f);
                        px[y * S + x] = a <= 0f ? Color.Transparent : Color.White * a;
                    }
                }
                Texture2D t = new Texture2D(g, S, S);
                t.SetData(px);
                return t;
            }
            catch { return VFXCore.SoftGlow; }
        }

        /// <summary>
        /// EL TRIÁNGULO DIRIGIDO: base anclada en <paramref name="basePos"/>,
        /// ápice a <paramref name="dir"/>×largo. EL pincel de la precisión.
        /// </summary>
        private static void Tri(Texture2D tex, Vector2 basePos, float dir,
            float largo, float ancho, Color tint)
        {
            if (tint.A == 0 || largo <= 0f || ancho <= 0f) return;
            Main.spriteBatch.Draw(tex, basePos, null, tint, dir + MathHelper.PiOver2,
                new Vector2(tex.Width * 0.5f, tex.Height),
                new Vector2(ancho / tex.Width, largo / tex.Height),
                SpriteEffects.None, 0f);
        }

        /// <summary>El rombo de la gema (centrado, tamaño total px).</summary>
        private static void Rombo(Vector2 pos, float tam, Color tint)
        {
            OrbitaLib.Quad(PincelRombo, pos, new Vector2(tam, tam), 0f, tint);
        }

        // ==================================================================
        //  LA VÉRTEBRA — la anatomía de dos tonos del Dragón del Cielo
        // ==================================================================

        /// <summary>
        /// UNA VÉRTEBRA v6.50.32 — LA CARNE DE SLIFER (antes: hueso de
        /// vacío + luz estelar):
        ///   · PASE ALFA = ANATOMÍA: el LOMO ESCARLATA (la cápsula
        ///     dorsal), el VIENTRE DE PIZARRA (la banda ventral con su
        ///     filo claro — el contraste de dos tonos de TODAS las
        ///     referencias), la SOMBRA DE PLACA (la articulación hacia
        ///     la cola — el look segmentado de paneles del anime) y LA
        ///     VELA DORSAL (la cresta trapezoidal cada 2 huesos,
        ///     barriendo hacia atrás).
        ///   · PASE ADITIVO = EL CALOR: la vena dorsal sutil (la
        ///     bestia está VIVA, no encendida) y la brasa de la vela.
        ///
        /// EL TAPER lo lleva el llamador (radio por índice — la sierpe
        /// se afina de 31 px junto al cráneo a ~9 en la punta).
        /// </summary>
        /// <param name="pos">Centro del hueso (PANTALLA).</param>
        /// <param name="rumbo">La DIRECCIÓN del hueso (rad — hacia la cabeza).</param>
        /// <param name="radio">El radio del hueso (px, ya escalado).</param>
        /// <param name="t">El tiempo animado.</param>
        /// <param name="idx">El índice en la cadena (fase de latidos/velas).</param>
        /// <param name="alpha">La visibilidad global 0..1.</param>
        public static void Vertebra(Vector2 pos, float rumbo, float radio,
            float t, int idx, float alpha)
        {
            if (alpha <= 0.02f) return;
            Vector2 frente = new(MathF.Cos(rumbo), MathF.Sin(rumbo));
            Vector2 perp = new(-frente.Y, frente.X);   // vientre = +perp

            // === 1) EL PASE ALFA: LA ANATOMÍA (la carne de dos tonos) ===
            OrbitaLib.AbrirAlpha();
            try
            {
                // EL LOMO ESCARLATA: la cápsula dorsal entera (la
                // articulación del SIGUIENTE hueso tapará la cola de
                // este — los PANELES segmentados del anime).
                OrbitaLib.Capsule(pos, radio * 2.95f, radio * 1.66f, rumbo,
                    T(Escarlata, 0.96f * alpha));
                // EL VIENTRE DE PIZARRA: la banda ventral (la mitad de
                // abajo de TODAS las referencias — pizarra azul-gris).
                Vector2 posV = pos + perp * (radio * 0.52f);
                OrbitaLib.Capsule(posV, radio * 2.70f, radio * 1.06f, rumbo,
                    T(Vientre, 0.97f * alpha));
                // EL FILO DEL VIENTRE: la línea clara que separa lomo y
                // panza (el cel-shading de la carta).
                OrbitaLib.Capsule(posV + perp * (radio * 0.30f), radio * 2.50f,
                    radio * 0.28f, rumbo, T(VientreBorde, 0.26f * alpha));
                // LA SOMBRA DE PLACA: la articulación hacia la cola (el
                // panel que se hunde — el cuerpo LEE segmentado).
                OrbitaLib.Capsule(pos - frente * (radio * 1.02f), radio * 2.15f,
                    radio * 1.50f, rumbo, T(EscarlataSombra, 0.40f * alpha));
                // LA VELA DORSAL: la cresta trapezoidal cada 2 huesos
                // (desde el 4º — el cuello va limpio hasta el hombro),
                // barriendo hacia atrás como las referencias.
                if (idx >= 4 && (idx & 1) == 0 && VFXCore.Presupuesto(2))
                {
                    Vector2 baseVela = pos - perp * (radio * 0.74f) - frente * (radio * 0.10f);
                    Vector2 dirV = -frente - perp * 1.05f;
                    Tri(PincelTriangulo, baseVela, MathF.Atan2(dirV.Y, dirV.X),
                        radio * 2.05f, radio * 1.12f, T(EscarlataSombra, 0.90f * alpha));
                }
            }
            finally { OrbitaLib.CerrarBatch(); }

            // === 2) EL PASE ADITIVO: EL CALOR (sutil — es carne, no faro) ===
            OrbitaLib.AbrirAdditive();
            try
            {
                // LA VENA DORSAL: el filo tibio del lomo (la vida).
                OrbitaLib.Capsule(pos - perp * (radio * 0.62f), radio * 2.55f,
                    radio * 0.30f, rumbo, T(EscarlataViva, 0.10f * alpha));
                // LA BRASA DE LA VELA: la punta latiendo (el fuego
                // interior asomando por la cresta).
                if (idx >= 4 && (idx & 1) == 0 && VFXCore.Presupuesto(1))
                {
                    Vector2 baseVela = pos - perp * (radio * 0.74f) - frente * (radio * 0.10f);
                    Vector2 dirV = -frente - perp * 1.05f;
                    float largoV = radio * 2.05f;
                    Vector2 puntaV = baseVela + Vector2.Normalize(dirV) * largoV;
                    float lat = 0.55f + 0.45f * MathF.Sin(t * 3.1f + idx * 0.9f);
                    OrbitaLib.Quad(VFXCore.GlowOrb, puntaV,
                        new Vector2(radio * 0.46f, radio * 0.46f), 0f,
                        T(Brasa, 0.16f * lat * alpha));
                }
            }
            finally { OrbitaLib.CerrarBatch(); }
        }

        // ==================================================================
        //  LAS ALAS — el Dragón del CIELO arquea al cielo
        // ==================================================================

        /// <summary>
        /// LAS DOS ALAS DE MURCIÉLAGO v6.50.32 — LA NOVEDAD de las
        /// referencias (el rasgo que la sierpe estelar NO tenía): dos
        /// alas inmensas arqueadas ALTO y ATRÁS de la cabeza, cada una
        /// con CUATRO struts afilados (el hueso rojo vivo), EL PAÑO
        /// granate translúcido festoneado entre ellos (triángulos con
        /// la raíz en el hombro — el borde de fuga lee festoneado como
        /// el de las referencias), LA GARRA de marfil en el strut
        /// guía (el pulgar del murciélago) y LA BRASA del borde de
        /// ataque.
        ///
        /// EL ALETEO es majesticidad: el abanico respira (±13%) a
        /// 1.35 Hz y el paño REZAGA (cada strut va retrasado 0.11 rad
        /// — la tela sigue al hueso, no lo copia). EL EJE DEL ALA
        /// mira al CIELO (atrás + arriba-mundo: es el Dragón del
        /// Cielo — las alas siempre ARQUEAN hacia lo alto, venga de
        /// donde venga la bestia).
        ///
        /// EL ORDEN: el ala LEJANA primero (más oscura — profundidad),
        /// la cercana después. Dibujar ANTES que la cabeza y que el
        /// cuerpo: nacen del hombro (la 3ª vértebra) por detrás de
        /// todo.
        /// </summary>
        /// <param name="hombro">El punto de anclaje (PANTALLA — la 3ª vértebra).</param>
        /// <param name="rumbo">El rumbo del CUERPO (rad — hacia la cabeza).</param>
        /// <param name="t">El tiempo animado.</param>
        /// <param name="fase">La fase de la pelea (1..5 — el color de la brasa).</param>
        /// <param name="alpha">La visibilidad global 0..1.</param>
        /// <param name="esc">La ESCALA del final (AethonSierpeCuerpo.ESC).</param>
        public static void Alas(Vector2 hombro, float rumbo, float t,
            int fase, float alpha, float esc)
        {
            if (alpha <= 0.02f) return;
            Vector2 frente = new(MathF.Cos(rumbo), MathF.Sin(rumbo));

            // EL EJE DEL CIELO: atrás + arriba-mundo (peso 1.2: el arco
            // SIEMPRE tiende a lo alto — la firma del Dragón del Cielo).
            Vector2 eje = -frente + new Vector2(0f, -1.2f);
            eje.Normalize();
            float angEje = eje.ToRotation();

            float aleteo = MathF.Sin(t * 1.35f);
            float despliegue = 1f + 0.13f * aleteo;    // el abanico respira
            float L = 248f * esc;                       // el strut guía (≈350 px)
            Color cBorde = fase >= 4 ? OjoVioleta : fase >= 3 ? OjoCian : Brasa;

            // LA PAREJA: la lejana (lado −) y la cercana (lado +).
            for (int w = 0; w < 2; w++)
            {
                float lado = w == 0 ? -1f : 1f;
                float prof = w == 0 ? 0.42f : 0.55f;   // el paño lejano, más denso el cercano
                float angBase = angEje + lado * 0.34f * despliegue;

                // === LOS CUATRO STRUTS (huesos afilados, largos decrecientes) ===
                float[] offs = { 0f, 0.40f, 0.74f, 1.02f };
                float[] largos = { 1.00f, 0.93f, 0.80f, 0.64f };
                Vector2[] puntas = new Vector2[4];
                float[] angs = new float[4];
                for (int s = 0; s < 4; s++)
                {
                    float lag = s * 0.11f;             // el paño rezaga al hueso
                    angs[s] = angBase + lado * offs[s] *
                        (0.92f + 0.10f * MathF.Sin(t * 1.35f - lag));
                    float len = L * largos[s] * (1f - 0.045f * MathF.Sin(t * 1.35f - lag));
                    puntas[s] = hombro + new Vector2(MathF.Cos(angs[s]), MathF.Sin(angs[s])) * len;
                }

                // === 1) EL PASE ALFA: EL PAÑO Y LOS HUESOS ===
                OrbitaLib.AbrirAlpha();
                try
                {
                    // EL PAÑO: triángulos translúcidos con la raíz en el
                    // hombro — el borde de fuga festoneado sale solo de
                    // los largos decrecientes (como el de las referencias).
                    for (int s = 0; s < 3; s++)
                    {
                        Vector2 punta = puntas[s + 1];
                        float dir = MathF.Atan2(punta.Y - hombro.Y, punta.X - hombro.X);
                        float anchoPanel = Vector2.Distance(puntas[s], puntas[s + 1]) * 1.30f;
                        float largoPanel = Vector2.Distance(hombro, punta);
                        Tri(PincelTriangulo, hombro, dir, largoPanel, anchoPanel,
                            T(Membrana, prof * alpha));
                    }
                    // LOS STRUTS: los huesos del ala (el lejano algo a la
                    // sombra — profundidad de verdad).
                    for (int s = 0; s < 4; s++)
                    {
                        float len = Vector2.Distance(hombro, puntas[s]);
                        Tri(PincelTriangulo, hombro, angs[s], len, 13.5f * esc,
                            T(w == 0 ? Escarlata : EscarlataViva, 0.94f * alpha));
                    }
                    // LA GARRA: el pulgar de marfil del strut guía.
                    Tri(PincelTriangulo, puntas[0], angs[0] + 0.62f * lado,
                        17f * esc, 5.5f * esc, T(Marfil, 0.92f * alpha));
                }
                finally { OrbitaLib.CerrarBatch(); }

                // === 2) EL PASE ADITIVO: LA BRASA DEL BORDE DE ATAQUE ===
                OrbitaLib.AbrirAdditive();
                try
                {
                    // El filo tibio del strut guía (la vena del ala).
                    Vector2 mid0 = (hombro + puntas[0]) * 0.5f;
                    float len0 = Vector2.Distance(hombro, puntas[0]);
                    OrbitaLib.Capsule(mid0, len0 * 0.92f, 3.6f * esc, angs[0],
                        T(cBorde, 0.26f * alpha));
                    // Las brasas de las puntas (la sangre de fuego en los
                    // extremos de los huesos).
                    for (int s = 0; s < 4; s++)
                    {
                        float lat = 0.55f + 0.45f * MathF.Sin(t * 2.9f + s * 1.3f + w);
                        OrbitaLib.Quad(VFXCore.GlowOrb, puntas[s],
                            new Vector2(7f * esc, 7f * esc), 0f,
                            T(cBorde, 0.34f * lat * alpha));
                    }
                }
                finally { OrbitaLib.CerrarBatch(); }
            }
        }

        // ==================================================================
        //  LA CABEZA — la anatomía de Slifer a plena precisión
        // ==================================================================

        /// <summary>
        /// LA CABEZA DEL DRAGÓN DEL CIELO v6.50.32 — el despiece
        /// anatómico de las referencias (vision-verified):
        ///   · PASE ALFA (LA CARNE): el cuello escarlata → LAS FAUCES
        ///     negras (el hueco de la mordida) → LA MANDÍBULA de acero
        ///     GIRANDO con la abertura (bisagra profunda, mentón romo)
        ///     con sus dientes pequeños → EL CRÁNEO en cuña escarlata
        ///     con su reborde de ceja → EL HOCICO de acero plateado
        ///     (40% del cráneo, raptor-like: cubre la punta del hocico
        ///     superior y TODA la mandíbula) con su brillo de
        ///     cel-shading y la fosa nasal → LOS DOS COLMILLOS SABLE de
        ///     marfil que sobresalen de la boca cerrada (bulldog) → LA
        ///     CORONA de 5 llamas espinosas barriendo hacia atrás (la
        ///     central la más alta) → LAS CUENCAS oscuras.
        ///   · PASE ADITIVO (EL FUEGO): EL FUEGO INTERIOR de las
        ///     fauces (crece con la abertura), LOS OJOS almendrados
        ///     rabiosos (dorado fase 1-2 · cian fase 3 — el arte de la
        ///     carta · violeta fase 4+ — la Luz Primordial) con su
        ///     núcleo blanco, LA GEMA AZUL de la frente (el rombo — el
        ///     diamante de las referencias) con su destello, LAS
        ///     BRASAS de las 5 puntas de la corona y EL FILO tibio del
        ///     cráneo (el cel-shading de la carta).
        /// </summary>
        /// <param name="pos">El centro del cráneo (PANTALLA).</param>
        /// <param name="rot">LA ROTACIÓN DEL NPC (rumbo + π/2 — como tML).</param>
        /// <param name="abertura">La abertura de las fauces 0..0.6.</param>
        /// <param name="t">El tiempo animado.</param>
        /// <param name="fase">La fase de la pelea (1..5 — ojos/gema/brasas).</param>
        /// <param name="alpha">La visibilidad global 0..1.</param>
        /// <param name="esc">La ESCALA del final (AethonSierpeCuerpo.ESC).</param>
        public static void Cabeza(Vector2 pos, float rot, float abertura,
            float t, int fase, float alpha, float esc)
        {
            if (alpha <= 0.02f) return;
            float rumbo = rot - MathHelper.PiOver2;              // el frente
            Vector2 frente = new(MathF.Cos(rumbo), MathF.Sin(rumbo));
            Vector2 perp = new(-frente.Y, frente.X);             // vientre = +perp
            Vector2 lomo = -perp;                                // corona = −perp
            Color cOjo = fase >= 4 ? OjoVioleta : fase >= 3 ? OjoCian : OjoDorado;
            Color cBrasa = fase >= 4 ? OjoVioleta : fase >= 3 ? OjoCian : Brasa;

            // LA ANATOMÍA (medidas ×esc — el cráneo mide ~140·esc del
            // cogote a la punta del hocico: el hocico es el 40%).
            Vector2 bisagra = pos + frente * (14f * esc) * -1f + perp * (12f * esc);
            Vector2 puntaHocico = pos + frente * (80f * esc);
            float jawAng = rumbo + 0.08f + abertura * 1.15f;      // la mandíbula GIRA
            Vector2 jawDir = new(MathF.Cos(jawAng), MathF.Sin(jawAng));
            Vector2 perpJaw = new(-jawDir.Y, jawDir.X);           // su propio vientre
            Vector2 menton = bisagra + jawDir * (78f * esc);
            float abierta = MathHelper.Clamp((abertura - 0.10f) / 0.45f, 0f, 1f);

            // === 1) EL PASE ALFA: LA CARNE (el orden ES la anatomía) ===
            OrbitaLib.AbrirAlpha();
            try
            {
                // EL CUELLO: el engarze escarlata (la 1ª vértebra lo tapa).
                OrbitaLib.Capsule(pos - frente * (44f * esc), 64f * esc, 50f * esc,
                    rumbo, T(Escarlata, 0.94f * alpha));

                // LAS FAUCES: el negro de la mordida (el hueco entre las
                // mandíbulas — SOLO cuando abre; cerrada es la línea).
                if (abierta > 0.02f)
                {
                    Vector2 baseFauces = (puntaHocico + menton) * 0.5f + frente * (4f * esc);
                    Vector2 dirF = baseFauces - bisagra;
                    float largoF = dirF.Length();
                    float anchoF = Vector2.Distance(puntaHocico, menton) * 0.82f;
                    Tri(PincelTriangulo, bisagra, MathF.Atan2(dirF.Y, dirF.X),
                        largoF * 0.98f, anchoF, T(Fauces, 0.92f * abierta * alpha));
                }
                else
                {
                    // La línea de la boca (la comisura que sube al final —
                    // la sonrisa siniestra de las referencias).
                    OrbitaLib.Capsule(pos + frente * (26f * esc) + perp * (10f * esc),
                        62f * esc, 5f * esc, rumbo, T(Fauces, 0.80f * alpha));
                }

                // LA MANDÍBULA DE ACERO (la sombra plateada — gira con la
                // abertura: el rugido de las referencias).
                Vector2 midJaw = bisagra + jawDir * (36f * esc);
                OrbitaLib.Capsule(midJaw, 80f * esc, 26f * esc, jawAng,
                    T(AceroSombra, 0.96f * alpha));
                // El filo superior de la mandíbula (donde muerden los
                // dientes pequeños — acero a media luz).
                OrbitaLib.Capsule(midJaw - perpJaw * (10f * esc), 74f * esc,
                    8.5f * esc, jawAng, T(Acero, 0.82f * alpha));

                // LOS DIENTES PEQUEÑOS de la mandíbula (las clavijas del
                // anime — crecen con la abertura).
                for (int j = 0; j < 3; j++)
                {
                    float tt = 0.42f + j * 0.17f;
                    Vector2 baseP = bisagra + jawDir * (tt * 74f * esc) - perpJaw * (10.5f * esc);
                    float dirP = jawAng - (0.18f + abertura * 0.85f);
                    Tri(PincelTriangulo, baseP, dirP, 15f * esc, 4.6f * esc,
                        T(Marfil, (0.25f + 0.75f * abierta) * 0.94f * alpha));
                }

                // EL CRÁNEO EN CUÑA (escarlata — el wedge de las
                // referencias: 60% cráneo, 40% hocico).
                OrbitaLib.Capsule(pos + frente * (-14f * esc), 96f * esc, 66f * esc,
                    rumbo, T(Escarlata, 0.97f * alpha));
                // EL REBORDE DE CEJA (la sombra pesada sobre el ojo).
                OrbitaLib.Capsule(pos + frente * (-2f * esc) + lomo * (21f * esc),
                    62f * esc, 13f * esc, rumbo, T(EscarlataSombra, 0.55f * alpha));
                // LA SOMBRA AXILAR (el mentón y la mejilla).
                OrbitaLib.Capsule(pos + frente * (12f * esc) + perp * (17f * esc),
                    88f * esc, 16f * esc, rumbo, T(EscarlataSombra, 0.45f * alpha));

                // EL HOCICO DE ACERO (el plateado: la punta del cráneo
                // superior — el gancho raptor de las referencias).
                OrbitaLib.Capsule(pos + frente * (52f * esc), 56f * esc, 31f * esc,
                    rumbo, T(Acero, 0.97f * alpha));
                // EL BRILLO del hocico (el cel-shading: la luz del cielo).
                OrbitaLib.Capsule(pos + frente * (50f * esc) + lomo * (9.5f * esc),
                    50f * esc, 8f * esc, rumbo, T(AceroClaro, 0.60f * alpha));
                // LA SOMBRA del hocico (la barba).
                OrbitaLib.Capsule(pos + frente * (50f * esc) + perp * (10.5f * esc),
                    48f * esc, 10f * esc, rumbo, T(AceroSombra, 0.62f * alpha));
                // LA FOSA NASAL (la ranura oscura de la punta).
                OrbitaLib.Capsule(pos + frente * (60f * esc) + lomo * (10f * esc),
                    8f * esc, 3.4f * esc, rumbo, T(Fauces, 0.85f * alpha));

                // LOS DOS COLMILLOS SABLE (marfil — sobresalen de la boca
                // CERRADA: el bulldog de las referencias, el aviso).
                for (int s = -1; s <= 1; s += 2)
                {
                    Vector2 baseC = pos + frente * (46f * esc) + perp * (s * 10f * esc);
                    Tri(PincelTriangulo, baseC, rumbo + 0.40f + s * 0.06f,
                        36f * esc, 9.5f * esc, T(Marfil, 0.97f * alpha));
                }

                // LA CORONA DE LLAMAS (5 espinas barriendo hacia atrás —
                // la central la más alta: el mohawk de las referencias).
                for (int k = 0; k < 5; k++)
                {
                    float tilt = 0.42f + 0.315f * (2 - MathF.Abs(k - 2)); // 1.05 la central
                    float largoK = (0.60f + 0.25f * (2 - MathF.Abs(k - 2))) * 56f * esc;
                    Vector2 baseK = pos + frente * ((-26f + k * 8f) * esc) + lomo * (26f * esc);
                    Vector2 dirK = -frente + lomo * tilt;
                    Tri(PincelTriangulo, baseK, MathF.Atan2(dirK.Y, dirK.X),
                        largoK, 10f * esc, T(Escarlata, 0.95f * alpha));
                }

                // LAS CUENCAS (el negro bajo el fuego del ojo).
                for (int s = -1; s <= 1; s += 2)
                {
                    Vector2 axis = -frente * 0.87f + perp * (s * 0.50f);
                    Vector2 centroOjo = pos + frente * (3f * esc) + perp * (s * 19f * esc);
                    OrbitaLib.Capsule(centroOjo, 24f * esc, 12.5f * esc,
                        MathF.Atan2(axis.Y, axis.X), T(Fauces, 0.82f * alpha));
                }
            }
            finally { OrbitaLib.CerrarBatch(); }

            // === 2) EL PASE ADITIVO: EL FUEGO (solo lo que BRILLA) ===
            OrbitaLib.AbrirAdditive();
            try
            {
                // EL FUEGO INTERIOR (la garganta ardiendo cuando rugue —
                // la Thunder Force acumulándose en las fauces).
                if (abierta > 0.02f)
                {
                    Vector2 centroF = (bisagra + menton + puntaHocico) / 3f;
                    float crec = 0.5f + 0.5f * abierta;
                    OrbitaLib.Quad(VFXCore.GlowOrb, centroF,
                        new Vector2(38f * esc * crec, 24f * esc * crec), rumbo,
                        T(Brasa, 0.50f * abierta * alpha));
                    OrbitaLib.Quad(VFXCore.GlowOrb, centroF,
                        new Vector2(16f * esc * crec, 10f * esc * crec), rumbo,
                        T(Marfil, 0.55f * abierta * alpha));
                }

                // LOS OJOS (almendrados y rabiosos — el barrido hacia
                // atrás de las referencias; el color cuenta la fase).
                for (int s = -1; s <= 1; s += 2)
                {
                    Vector2 axis = -frente * 0.87f + perp * (s * 0.50f);
                    Vector2 centroOjo = pos + frente * (3f * esc) + perp * (s * 19f * esc);
                    float angOjo = MathF.Atan2(axis.Y, axis.X);
                    float fare = 1f + abertura * 0.75f;
                    OrbitaLib.Quad(VFXCore.GlowOrb, centroOjo,
                        new Vector2(26f * esc * fare, 10f * esc * fare), angOjo,
                        T(cOjo, 0.88f * alpha));
                    // EL NÚCLEO BLANCO (la mirada que perfora).
                    OrbitaLib.Quad(VFXCore.GlowOrb, centroOjo + frente * (5f * esc),
                        new Vector2(9f * esc * fare, 7f * esc * fare), angOjo,
                        T(Marfil, 0.92f * alpha));
                }

                // LA GEMA DE LA FRENTE (el diamante azul — el sello del
                // Dios del Cielo: un rombo NÍTIDO, el pincel nuevo).
                Vector2 posGema = pos + frente * (14f * esc) + lomo * (25f * esc);
                Rombo(posGema, 15f * esc, T(Gema, 0.95f * alpha));
                OrbitaLib.Quad(VFXCore.GlowOrb, posGema,
                    new Vector2(8f * esc, 8f * esc), 0f, T(Marfil, 0.90f * alpha));
                float pulsG = 0.6f + 0.4f * MathF.Sin(t * 2.4f);
                OrbitaLib.Quad(Star, posGema, new Vector2(26f * esc, 26f * esc),
                    t * 0.8f, T(Gema, 0.35f * pulsG * alpha));

                // LAS BRASAS DE LA CORONA (las puntas de las 5 llamas —
                // el fuego asomando por la cresta).
                for (int k = 0; k < 5; k++)
                {
                    float tilt = 0.42f + 0.315f * (2 - MathF.Abs(k - 2));
                    float largoK = (0.60f + 0.25f * (2 - MathF.Abs(k - 2))) * 56f * esc;
                    Vector2 baseK = pos + frente * ((-26f + k * 8f) * esc) + lomo * (26f * esc);
                    Vector2 dirK = -frente + lomo * tilt;
                    Vector2 puntaK = baseK + Vector2.Normalize(dirK) * largoK;
                    float lat = 0.55f + 0.45f * MathF.Sin(t * 3.0f + k * 1.1f);
                    OrbitaLib.Quad(VFXCore.GlowOrb, puntaK,
                        new Vector2(9f * esc, 9f * esc), 0f,
                        T(cBrasa, 0.42f * lat * alpha));
                }

                // EL FILO DEL CRÁNEO (el cel-shading cálido del lomo).
                OrbitaLib.Capsule(pos + frente * (-12f * esc) + lomo * (30f * esc),
                    72f * esc, 6.5f * esc, rumbo, T(EscarlataViva, 0.16f * alpha));
            }
            finally { OrbitaLib.CerrarBatch(); }
        }
    }
}
