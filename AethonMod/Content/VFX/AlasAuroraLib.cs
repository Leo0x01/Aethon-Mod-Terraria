using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// AlasAuroraLib — v6.55.0 — LAS ALAS CREADAS POR CÓDIGO, DE VERDAD.
    ///
    /// LA TÉCNICA (la investigación de la casa, aprendida y rehecha):
    /// las alas procedurales nacen de PRIMITIVAS GPU — cero sprites.
    ///
    ///   1. UN ABANICO DE PLUMAS POR ALA que se PLEGA (fan vertical
    ///      estrecho) y se ABRE en vuelo (fan casi horizontal) según
    ///      el spread (0..1 con Lerp suave — el ala se despliega).
    ///   2. LA ONDA DE ALETEO se PROPAGA de la pluma interior a la
    ///      exterior: off += sin(flap − i·0.45)·amplitud — las plumas
    ///      ondean EN CADENA, no en bloque (la firma visual).
    ///   3. EL COLOR VIAJA POR VÉRTICE: cada pluma lleva su hue que SE
    ///      DISPERSA a lo largo y con el tiempo — el espectro CORRE.
    ///   4. EL RUIDO es el PLIEGUE VIVO: dos capas fluyendo a
    ///      frecuencias y velocidades distintas por la pluma.
    ///   5. EL DESGARRE DE LA PUNTA: la punta se RASGA — el hash
    ///      determinista decide qué mechones viven.
    ///   6. LA FÍSICA SE INYECTA SIN SPRITE: wingsLogic + wingTimeMax
    ///      propio; TODO el visual es el código.
    ///
    /// v6.55.0 — LA REFORMA VECTORIAL (la letra: «porque todas son
    /// rectángulos, deberías crear formas vectoriales o por código para
    /// las alas, como líneas, plumas, cuchillas o algo así, pero no solo
    /// rectángulos planos»). LA AUTOPSIA: cada pluma era UN SOLO QUAD de
    /// 4 esquinas — un trapecio plano entre raíz y punta (la «curva»
    /// solo desplazaba la punta lateralmente; el borde seguía RECTO).
    /// LA CURA — LA PLUMA ES UNA TIRA DE 9 NODOS:
    ///   · EL EJE SE CURVA de verdad: la desviación perpendicular crece
    ///     CUADRÁTICAMENTE a lo largo (la curva continua de la pluma
    ///     viva, no un empujón en la punta).
    ///   · EL PERFIL DE ANCHO es un perfil de PLUMA: raíz estrecha →
    ///     el VIENTRE (ancho máximo en Vientre·largo — cada ala declara
    ///     dónde) → la PUNTA AFILADA que muere en 0 (el exponente
    ///     Afilada: 0.5 redondeada, 1 cuchilla, 2 aguja cóncava).
    ///   · Vértices COMPARTIDOS entre segmentos (la lección de RayoStrip:
    ///     cero juntas, cero solapes) y las UV del ruido fluyen POR
    ///     NODO (continuas raíz→punta).
    ///   · EL DESGARRE ahora talla la punta: los últimos nodos
    ///     engordan/afilan con el hash — la punta respira.
    ///
    /// CONTRATO DE LOTE (el de la casa, como RayoStrip): el llamador
    /// trae el lote del pase de entidades ABIERTO; Dibujar lo cierra
    /// (sonda), dibuja las primitivas y lo REABRE (idempotente).
    /// Coordenadas de PANTALLA (mundo − Main.screenPosition — la cámara
    /// la resta EL LLAMADOR: el pipeline de Volcar solo aplica el zoom).
    /// </summary>
    public static class AlasAuroraLib
    {
        // === LAS CONSTANTES (calibradas por la casa) ===

        /// <summary>Plumas por ala — el VALOR HISTÓRICO; v6.54.3: cada
        /// estilo declara SU conteo
        /// (EstiloAlas.Plumas, 3..7) y este queda como el tope de los
        /// POZOS (7×2 alas = 14 plumas vivas).</summary>
        public const int PlumasPorAla = 5;

        /// <summary>v6.54.3 — EL TOPE DE PLUMAS de cualquier estilo
        /// (los pozos se dimensionan con esto; los estilos declaran
        /// 3..7 — la Aurora mantiene sus 5 históricas).</summary>
        private const int PlumasMax = 7;

        /// <summary>v6.55.0 — LOS NODOS DE LA TIRA VECTORIAL: cada pluma
        /// es una polilínea de EJE con NodosPluma+1 puntos (raíz→punta)
        /// — el perfil de pluma vive en los semianchos por nodo.</summary>
        public const int NodosPluma = 8;

        /// <summary>Pluma interior→exterior: ángulo PLEGADO (fan vertical).</summary>
        public const float OffPlegadoMin = 0.50f, OffPlegadoMax = 0.92f;

        /// <summary>Pluma interior→exterior: ángulo ABIERTO (fan horizontal).</summary>
        public const float OffAbiertoMin = 0.22f, OffAbiertoMax = 1.54f;

        /// <summary>Largo de la pluma interior (px).</summary>
        public const float LargoInterior = 88f;

        /// <summary>Largo de la pluma exterior (px).</summary>
        public const float LargoExterior = 48f;

        /// <summary>Semiancho de la raíz (px).</summary>
        public const float RaizSemiancho = 3.5f;

        /// <summary>Semiancho de la punta cerrada → abierta (px).</summary>
        public const float PuntaSemianchoBase = 14f, PuntaSemianchoAbierto = 8f;

        /// <summary>Salto de hue entre pluma y pluma (el arcoíris del abanico).</summary>
        public const float HueEntrePlumas = 0.085f;

        /// <summary>v6.52.2 — LA INCLINACIÓN BASE DE CADA ALA: el hueco
        /// central que hace que el par se LEA como DOS alas (la letra:
        /// «solo hay 1 ala»). La pluma interior de cada lado arranca ya
        /// inclinada — sin este hueco ambos abanicos comparten la pluma
        /// vertical y se funden en UN solo abanico de capa.</summary>
        public const float InclinacionPar = 0.12f;

        /// <summary>v6.52.2 — EL BARRIDO HACIA ATRÁS con la dirección de
        /// vista: todo el par rota sutilmente hacia la espalda del
        /// jugador (más abierto = más barrido — el par RESPONDE al
        /// girarse, la firma del original).</summary>
        public const float BarridoAtras = 0.14f;

        /// <summary>Dispersión del hue A LO LARGO de la pluma (el espectro corre).</summary>
        public const float HueALoLargo = 0.16f;

        /// <summary>Velocidad del hue con el tiempo.</summary>
        public const float HueTiempo = 0.015f;

        /// <summary>El blanco-oro de la forma de día (el «whiteGold» del original).</summary>
        private static readonly Color BlancoOro = new(255, 243, 214);

        // === v6.54.0 — EL CACHÉ DE RUIDOS POR ESTILO (cada ala fluye con
        //     SU textura de las 20 de la investigación) ===
        private static readonly System.Collections.Generic.Dictionary<string, Texture2D> _ruidos =
            new();

        // === EL DISPOSITIVO (BasicEffect cacheado ×2 + pozo de vértices) ===

        private static BasicEffect _fxPlano;
        private static BasicEffect _fxTextura;
        private static Texture2D _ruido;
        private static bool _texturaProboFallar;

        /// <summary>El lote de pantalla suave (soft-add: dst += rgb·(1−dst)) — el de RayoStrip.</summary>
        private static readonly BlendState _pantalla = new BlendState
        {
            ColorSourceBlend = Blend.InverseDestinationColor,
            ColorDestinationBlend = Blend.One,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.One,
        };

        /// <summary>El pozo de vértices del ala (triángulos planos — cero GC).
        /// v6.55.0 — 4096: las TRES capas acumulan hasta 3×(7·2·8·6)=2016
        /// vértices antes del Volcar (con 1024 el ala derecha perdía el
        /// cuerpo en los estilos de 6-7 plumas — el truncado silencioso
        /// del agente revisor).</summary>
        private static VertexPositionColorTexture[] _verts = new VertexPositionColorTexture[4096];

        // LOS POZOS DE LA GEOMETRÍA (estáticos — la lección cero-GC de la casa:
        // nada nace por frame). v6.54.3: el tope es PlumasMax×2 (la Aurora
        // usa 5×2 como siempre). v6.55.0: cada pluma trae SU POLILÍNEA
        // (eje + semianchos por nodo) — también del pozo.
        private static readonly Pluma[] _plumas = new Pluma[PlumasMax * 2];
        private static readonly float[] _hues = new float[PlumasMax * 2];
        private static readonly float[] _fundidos = new float[PlumasMax * 2];

        private static readonly Vector2[][] _pozosEje = CrearPozosEje();
        private static readonly float[][] _pozosSemi = CrearPozosSemi();
        private static readonly float[][] _pozosSemi2 = CrearPozosSemi();

        private static Vector2[][] CrearPozosEje()
        {
            var r = new Vector2[PlumasMax * 2][];
            for (int i = 0; i < r.Length; i++) r[i] = new Vector2[NodosPluma + 1];
            return r;
        }

        private static float[][] CrearPozosSemi()
        {
            var r = new float[PlumasMax * 2][];
            for (int i = 0; i < r.Length; i++) r[i] = new float[NodosPluma + 1];
            return r;
        }

        // ==================================================================
        //  EL COLOR PRISMA (el de la casa — el mismo algoritmo del
        //  hueRGB del shader original, resuelto en CPU)
        // ==================================================================

        private static Color ColorPrisma(float h)
        {
            h -= MathF.Floor(h);
            float r, g, b;
            if (h < 1f / 6f) { r = 1f; g = h * 6f; b = 0f; }
            else if (h < 2f / 6f) { r = 2f - h * 6f; g = 1f; b = 0f; }
            else if (h < 3f / 6f) { r = 0f; g = 1f; b = h * 6f - 2f; }
            else if (h < 4f / 6f) { r = 0f; g = 4f - h * 6f; b = 1f; }
            else if (h < 5f / 6f) { r = h * 6f - 4f; g = 0f; b = 1f; }
            else { r = 1f; g = 0f; b = 6f - h * 6f; }
            return new Color((int)(r * 255f), (int)(g * 255f), (int)(b * 255f));
        }

        /// <summary>El tono de la pluma: el prisma dispersado hacia el blanco-oro de día.</summary>
        private static Color TonoPluma(float hue, float mezclaDia, float haciaBlanco)
            => TonoPluma(hue, mezclaDia, haciaBlanco, AlasCatalogo.De(AlasCatalogo.Aurora));

        /// <summary>v6.54.0 — LA VERSIÓN CON ESTILO: el hue vive en la
        /// PALETA del ala (crossfade continuo — el contrato ColorPrisma)
        /// y la forma diurna funde hacia SU luz.</summary>
        private static Color TonoPluma(float hue, float mezclaDia, float haciaBlanco,
            in EstiloAlas estilo)
        {
            Color prisma = DePaleta(estilo, hue);
            Color dia = estilo.Paleta == null ? BlancoOro : estilo.ColorDia;
            float fuerza = estilo.Paleta == null ? 0.62f : estilo.FuerzaDia;
            Color tinte = new(
                (int)(prisma.R + (dia.R - prisma.R) * (fuerza * mezclaDia)),
                (int)(prisma.G + (dia.G - prisma.G) * (fuerza * mezclaDia)),
                (int)(prisma.B + (dia.B - prisma.B) * (fuerza * mezclaDia)));
            // HACIA BLANCO: el eje central de la pluma arde más claro
            // (el «axis» blanco-caliente del shader).
            return new(
                (int)(tinte.R + (255 - tinte.R) * haciaBlanco),
                (int)(tinte.G + (255 - tinte.G) * haciaBlanco),
                (int)(tinte.B + (255 - tinte.B) * haciaBlanco));
        }

        /// <summary>v6.54.0 — EL HUE DE LA PALETA: null = el prisma
        /// completo histórico; si no, la paleta del ala recorrida en
        /// ciclo continuo con crossfade total (12/5 del ColorPrisma).</summary>
        private static Color DePaleta(in EstiloAlas estilo, float h)
        {
            if (estilo.Paleta == null || estilo.Paleta.Length == 0)
                return ColorPrisma(h);
            Color[] pal = estilo.Paleta;
            h -= MathF.Floor(h);
            float seg = h * pal.Length;
            int i = (int)seg % pal.Length;
            int j = (i + 1) % pal.Length;
            return Color.Lerp(pal[i], pal[j], seg - (int)seg);
        }

        // ==================================================================
        //  LA GEOMETRÍA DE UNA PLUMA (el corazón de la técnica)
        // ==================================================================

        /// <summary>v6.55.0 — LA PLUMA VECTORIAL: una POLILÍNEA de
        /// NodosPluma+1 nodos con su PERFIL de semiancho por nodo — el
        /// eje curvado y la forma de pluma (vientre + punta afilada)
        /// viven aquí. Los arrays son prestados de los POZOS estáticos
        /// (cero GC por frame — la lección de la casa).</summary>
        private struct Pluma
        {
            public Vector2[] Eje;       // NodosPluma+1 puntos raíz→punta (el eje CURVADO)
            public float[] SemiMas;     // el semiancho del borde de ATAQUE por nodo (convexo)
            public float[] SemiMenos;   // el semiancho del borde de FUGA por nodo (cóncavo — la cuchilla)
            public float Largo;         // 0..1 normalizado (para UV)
        }

        /// <summary>
        /// v6.55.0 — Construye la pluma i del ala `lado` como UNA TIRA
        /// VECTORIAL: el EJE se curva cuadráticamente (la garra vive en
        /// TODA la pluma, no solo en la punta) y el SEMIANCHO por nodo
        /// sigue EL PERFIL DE PLUMA (raíz estrecha → el VIENTRE en
        /// estilo.Vientre·largo → la PUNTA AFILADA que muere en 0).
        /// El abanico, la onda de aleteo en cadena y el largo que
        /// respiran quedan como siempre.
        /// </summary>
        private static Pluma ConstruirPluma(Vector2 ancla, int i, float spread, float flap,
            float grav, int direccion, int lado, float time, in EstiloAlas estilo,
            Vector2[] pozoEje, float[] pozoSemi, float[] pozoSemi2, float fundido)
        {
            int n = estilo.Plumas < 2 ? 2 : (estilo.Plumas > PlumasMax ? PlumasMax : estilo.Plumas);
            float t = i / (float)(n - 1);

            // EL ABANICO: plegado estrecho ↔ abierto horizontal.
            float offPlegado = MathHelper.Lerp(estilo.PlegadoMin, estilo.PlegadoMax, t);
            float offAbierto = MathHelper.Lerp(estilo.AbiertoMin, estilo.AbiertoMax, t);
            float off = MathHelper.Lerp(offPlegado, offAbierto, spread);

            // LA ONDA DE ALETEO: de la pluma interior a la exterior (el
            // segundero del ala — la firma).
            float amplitud = 0.05f + 0.11f * spread;
            off += MathF.Sin(flap - i * 0.45f) * amplitud;

            // EL ÁNGULO: −π/2 (arriba) más la inclinación del lado (el
            // hueco central) barrido por el abanico — y todo rotado
            // hacia ATRÁS según mira el jugador.
            float angulo = -MathHelper.PiOver2 + lado * (estilo.Inclinacion + off)
                - direccion * estilo.Barrido * (0.35f + 0.65f * spread);
            Vector2 dir = new(MathF.Cos(angulo), MathF.Sin(angulo) * grav);
            Vector2 perp = new(-dir.Y, dir.X);

            // EL LARGO que respira (interior→exterior × apertura + la
            // vibración sutil del aleteo).
            float largo = MathHelper.Lerp(estilo.LargoInterior, estilo.LargoExterior, t)
                * (0.5f + 0.5f * spread);
            largo *= 1f + MathF.Sin(flap * 0.7f + i * 0.6f) * 0.045f;

            // === LA ANATOMÍA VECTORIAL (v6.55.0) ===
            float vientre = MathHelper.Clamp(estilo.Vientre, 0.15f, 0.70f);
            float afilada = MathHelper.Clamp(estilo.Afilada, 0.35f, 2.5f);
            float filosa = MathHelper.Clamp(estilo.Filosa, 0f, 1f);
            float anchoMax = estilo.PuntaSemianchoBase + estilo.PuntaSemianchoAbierto * spread;
            float raiz = estilo.RaizSemiancho;

            // v6.55.0-b — LA ASIMETRÍA DE CUCHILLA (la revisión del mock):
            // el borde de ATAQUE (+) vive su vientre en Vientre·largo; el
            // de FUGA (−) lo corre ANTES (Vientre·(1−0.45·Filosa)) y algo
            // más bajo — el filo convexo y el lomo cóncavo de la media
            // luna (Filosa 0 = pluma simétrica).
            float vientreMas = vientre;
            float vientreMenos = vientre * (1f - 0.45f * filosa);
            float anchoMas = anchoMax * (1f + 0.18f * filosa);
            float anchoMenos = anchoMax * (1f - 0.30f * filosa);

            Pluma p;
            p.Eje = pozoEje;
            p.SemiMas = pozoSemi;
            p.SemiMenos = pozoSemi2;
            p.Largo = largo;

            int nodos = NodosPluma + 1;
            for (int j = 0; j < nodos; j++)
            {
                float q = j / (float)NodosPluma;

                // EL EJE CURVADO: la desviación perpendicular crece
                // CUADRÁTICAMENTE — la pluma se dobla CONTINUA (la garra
                // del murciélago vive en todo el largo).
                Vector2 eje = ancla + dir * (largo * q)
                    + perp * (estilo.Curva * largo * q * q);

                // EL PERFIL por nodo y por borde:
                //   q < vientreX: raíz → anchoX (sin(π/2·w)^0.8)
                //   q ≥ vientreX: anchoX → 0 ((1−w)^afilada — la punta)
                static float Perfil(float q, float raiz, float vientre, float ancho, float afilada)
                {
                    if (q < vientre)
                    {
                        float w = q / vientre;
                        return MathHelper.Lerp(raiz, ancho, MathF.Pow(MathF.Sin(w * MathHelper.PiOver2), 0.8f));
                    }
                    float wm = (q - vientre) / MathF.Max(1f - vientre, 0.001f);
                    return ancho * MathF.Pow(1f - wm, afilada);
                }
                float semiMas = Perfil(q, raiz, vientreMas, anchoMas, afilada);
                float semiMenos = Perfil(q, raiz, vientreMenos, anchoMenos, afilada);

                // EL DESGARRE TALLA LA PUNTA: los últimos nodos engordan
                // o afilan con el fundido del hash — la punta RESPIRA.
                if (q > 0.70f)
                {
                    float g = (q - 0.70f) / 0.30f;
                    float talla = MathHelper.Lerp(1f, 0.25f + 0.75f * fundido, g * g);
                    semiMas *= talla;
                    semiMenos *= talla;
                }

                pozoEje[j] = eje;
                pozoSemi[j] = MathF.Max(semiMas, 0.30f);
                pozoSemi2[j] = MathF.Max(semiMenos, 0.30f);
            }
            return p;
        }

        // ==================================================================
        //  EL VOLCADO DE UNA CAPA (una tira de quads de pluma)
        // ==================================================================

        /// <summary>
        /// v6.55.0 — Vuelca las plumas como TIRAS VECTORIALES: cada pluma
        /// es una CADENA de quads (uno por segmento del eje) con vértices
        /// COMPARTIDOS — la silueta de pluma (vientre + punta afilada) se
        /// lee en el borde, y el color viaja por NODO (la raíz entra suave
        /// y la punta se desgarra). `capa` elige la fracción del
        /// semiancho y la intensidad: 0 núcleo · 1 cuerpo · 2 halo.
        /// </summary>
        private static int EmitirCapa(int nVerts, Pluma[] plumas, float[] hues,
            float[] fundidos, float mezclaDia, int capa, in EstiloAlas estilo, int n)
        {
            float frac, inten;
            switch (capa)
            {
                case 0: frac = estilo.NucleoFrac; inten = estilo.NucleoInten; break;   // el eje blanco-caliente
                case 1: frac = estilo.CuerpoFrac; inten = estilo.CuerpoInten; break;   // el cuerpo del prisma
                default: frac = 1f; inten = estilo.HaloInten; break;                   // el halo que muere suave
            }

            for (int i = 0; i < n; i++)
            {
                ref Pluma p = ref plumas[i];
                if (nVerts + 6 * NodosPluma > _verts.Length)
                    break;
                float haciaBlanco = capa == 0 ? 0.72f : capa == 1 ? 0.30f : 0.12f;

                // EL COLOR POR NODO: el hue se dispersa a lo largo (el
                // espectro corre del hombro a la punta) y el fundido
                // vive en la PUNTA (el desgarre — el hash decide).
                Color cRaiz = TonoPluma(hues[i], mezclaDia, haciaBlanco, estilo);

                for (int j = 0; j < NodosPluma; j++)
                {
                    float q0 = j / (float)NodosPluma;
                    float q1 = (j + 1) / (float)NodosPluma;

                    // LA NORMAL MEDIA por nodo (la tira es continua por
                    // construcción — la lección de RayoStrip).
                    Vector2 d0 = j > 0 ? p.Eje[j] - p.Eje[j - 1] : p.Eje[j + 1] - p.Eje[j];
                    Vector2 d1 = j < NodosPluma - 1 ? p.Eje[j + 2] - p.Eje[j + 1] : p.Eje[j + 1] - p.Eje[j];
                    if (d0.LengthSquared() < 0.0001f) d0 = d1;
                    if (d1.LengthSquared() < 0.0001f) d1 = d0;
                    Vector2 m = Vector2.Normalize(Vector2.Normalize(d0) + Vector2.Normalize(d1));
                    if (!(m.LengthSquared() >= 0.001f)) m = d1;
                    Vector2 nrm = new(-m.Y, m.X);

                    float s0 = p.SemiMas[j] * frac;
                    float s1 = p.SemiMas[j + 1] * frac;
                    float t0 = p.SemiMenos[j] * frac;
                    float t1 = p.SemiMenos[j + 1] * frac;

                    // EL ALFA por nodo: la raíz entra suave (0.22) y la
                    // punta se DESGARRA (el fundido del hash).
                    float f0 = MathHelper.Lerp(0.22f, fundidos[i], q0 * q0);
                    float f1 = MathHelper.Lerp(0.22f, fundidos[i], q1 * q1);

                    Color c0 = Pre(TonoPluma(hues[i] + estilo.HueLargo * q0, mezclaDia,
                        haciaBlanco * (1f - 0.3f * q0), estilo), inten * f0);
                    Color c1 = Pre(TonoPluma(hues[i] + estilo.HueLargo * q1, mezclaDia,
                        haciaBlanco * (1f - 0.3f * q1), estilo), inten * f1);
                    _ = cRaiz;

                    VertexPositionColorTexture a0 = new(new Vector3(p.Eje[j] + nrm * s0, 0f), c0, new Vector2(q0, 0f));
                    VertexPositionColorTexture a1 = new(new Vector3(p.Eje[j] - nrm * t0, 0f), c0, new Vector2(q0, 1f));
                    VertexPositionColorTexture b0 = new(new Vector3(p.Eje[j + 1] + nrm * s1, 0f), c1, new Vector2(q1, 0f));
                    VertexPositionColorTexture b1 = new(new Vector3(p.Eje[j + 1] - nrm * t1, 0f), c1, new Vector2(q1, 1f));

                    // DOS TRIÁNGULOS compartiendo la diagonal (cero juntas).
                    _verts[nVerts++] = a0;
                    _verts[nVerts++] = a1;
                    _verts[nVerts++] = b0;
                    _verts[nVerts++] = a0;
                    _verts[nVerts++] = b0;
                    _verts[nVerts++] = b1;
                }
            }
            return nVerts;
        }

        /// <summary>v6.55.0 — La versión texturizada: la TIRA de la pluma
        /// con las UV del ruido FLUYENDO POR NODO (continuas raíz→punta —
        /// antes un solo quad estiraba la textura de un tirón).</summary>
        private static int EmitirCapaRuido(int nVerts, Pluma[] plumas, float[] hues,
            float mezclaDia, float time, int pasada, float fold, in EstiloAlas estilo, int n)
        {
            // LAS DOS FRECUENCIAS (f1 lenta que fluye HACIA la punta, f2
            // fina que retrocede — el pliegue doble de la aurora). El
            // fold PLEGADO frena el flujo (la tinta quieta).
            float escalaU, velU, escalaV, velV, inten;
            if (pasada == 0)
            {
                escalaU = 1.7f * estilo.RuidoEscala;
                velU = (0.20f - fold * 0.08f) * estilo.RuidoFlujo;
                escalaV = 0.6f * estilo.RuidoEscala;
                velV = 0.31f * estilo.RuidoFlujo;
                inten = 0.46f;
            }
            else
            {
                escalaU = 3.4f * estilo.RuidoEscala;
                velU = -0.12f * estilo.RuidoFlujo;
                escalaV = 1.15f * estilo.RuidoEscala;
                velV = 0f;
                inten = 0.30f;
            }
            inten *= estilo.RuidoInten;

            for (int i = 0; i < n; i++)
            {
                ref Pluma p = ref plumas[i];
                if (nVerts + 6 * NodosPluma > _verts.Length)
                    break;
                float fase = hues[i] * 7.3f; // cada pluma desfasa su ruido

                float uBase = -time * velU + fase;
                float v0 = fase * velV * 0.1f;
                float v1 = escalaV + v0;

                Color cR = Pre(TonoPluma(hues[i], mezclaDia, 0.05f, estilo), inten * 0.30f);
                Color cP = Pre(TonoPluma(hues[i] + estilo.HueLargo, mezclaDia, 0.05f, estilo), inten);

                for (int j = 0; j < NodosPluma; j++)
                {
                    float q0 = j / (float)NodosPluma;
                    float q1 = (j + 1) / (float)NodosPluma;

                    // LA NORMAL MEDIA por nodo.
                    Vector2 d0 = j > 0 ? p.Eje[j] - p.Eje[j - 1] : p.Eje[j + 1] - p.Eje[j];
                    Vector2 d1 = j < NodosPluma - 1 ? p.Eje[j + 2] - p.Eje[j + 1] : p.Eje[j + 1] - p.Eje[j];
                    if (d0.LengthSquared() < 0.0001f) d0 = d1;
                    if (d1.LengthSquared() < 0.0001f) d1 = d0;
                    Vector2 m = Vector2.Normalize(Vector2.Normalize(d0) + Vector2.Normalize(d1));
                    if (!(m.LengthSquared() >= 0.001f)) m = d1;
                    Vector2 nrm = new(-m.Y, m.X);

                    // LAS UV FLUYEN POR NODO: u avanza con el arco de la
                    // pluma y el tiempo — el ruido CAMINA raíz→punta.
                    float u0 = uBase + escalaU * q0;
                    float u1 = uBase + escalaU * q1;

                    Color c0 = Color.Lerp(cR, cP, q0);
                    Color c1 = Color.Lerp(cR, cP, q1);

                    VertexPositionColorTexture a0 = new(new Vector3(p.Eje[j] + nrm * p.SemiMas[j], 0f), c0, new Vector2(u0, v0));
                    VertexPositionColorTexture a1 = new(new Vector3(p.Eje[j] - nrm * p.SemiMenos[j], 0f), c0, new Vector2(u0, v1));
                    VertexPositionColorTexture b0 = new(new Vector3(p.Eje[j + 1] + nrm * p.SemiMas[j + 1], 0f), c1, new Vector2(u1, v0));
                    VertexPositionColorTexture b1 = new(new Vector3(p.Eje[j + 1] - nrm * p.SemiMenos[j + 1], 0f), c1, new Vector2(u1, v1));

                    _verts[nVerts++] = a0;
                    _verts[nVerts++] = a1;
                    _verts[nVerts++] = b0;
                    _verts[nVerts++] = a0;
                    _verts[nVerts++] = b0;
                    _verts[nVerts++] = b1;
                }
            }
            return nVerts;
        }

        private static Vector2 Lerp(Vector2 a, Vector2 b, float t) => a + (b - a) * t;

        /// <summary>El color PRE-MULTIPLICADO (la lección v6.50.26 de RayoStrip: el rgb escala por f AQUÍ).</summary>
        private static Color Pre(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color((byte)(int)(c.R * f + 0.5f), (byte)(int)(c.G * f + 0.5f),
                (byte)(int)(c.B * f + 0.5f), 255);
        }

        // ==================================================================
        //  EL DIBUJO PRINCIPAL (contrato: lote abierto → dibuja → lote abierto)
        // ==================================================================

        /// <summary>
        /// Dibuja EL PAR DE ALAS AURORA del jugador. Contrato de la casa:
        /// cierra el lote del llamador (sonda), dibuja las primitivas y
        /// LO REABRE (ReabrirLoteVanilla — idempotente). DEVUELVE true
        /// si dibujó algo.
        /// </summary>
        /// <param name="pl">El jugador portador.</param>
        /// <param name="spread">0..1 apertura del ala (el halo lleva la suavidad).</param>
        /// <param name="flap">Fase acumulada del aleteo.</param>
        /// <param name="mezclaDia">0..1 la forma nocturna (prisma) ↔ diurna (blanco-oro).</param>
        public static bool Dibujar(Player pl, float spread, float flap, float mezclaDia)
            => Dibujar(pl, AlasCatalogo.De(AlasCatalogo.Aurora), spread, flap, mezclaDia);

        /// <summary>
        /// v6.54.0 — EL DIBUJO CON ESTILO (la letra: «crea otras 15 alas
        /// usando el mismo sistema»): MISMA técnica, MISMO contrato de
        /// lote — solo cambian la paleta, la forma, el tempo, las
        /// intensidades y la TEXTURA DE RUIDO que fluye por las plumas
        /// (cada ala trae la suya de las 20 de la investigación).
        /// </summary>
        public static bool Dibujar(Player pl, in EstiloAlas estilo, float spread,
            float flap, float mezclaDia)
        {
            if (Main.netMode == NetmodeID.Server || pl == null || pl.dead || spread <= 0.02f)
                return false;
            if (Main.gameMenu)
                return false;

            try
            {
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (!AsegurarEfectos(gd, estilo.Ruido))
                    return false;

                float time = Main.GlobalTimeWrappedHourly;
                float grav = pl.gravDir;
                int direccion = pl.direction;
                float fold = 1f - spread;

                // EL BAILE ÚNICO DEL LOTE (el contrato de RayoStrip): se
                // cierra UNA VEZ, van las TRES volcadas y se reabre UNA
                // VEZ — el aditivo es conmutativo y el Deferred no ordena.
                bool cerroLote = VFXCore.CerrarLoteSiAbierto();
                try
                {
                    // EL ANCLA: la espalda, ligeramente hacia atrás (el
                    // original: MountedCenter + (−dir·7, −4·grav)) — EN
                    // PANTALLA (− Main.screenPosition: Volcar aplica SOLO
                    // el zoom — la cámara la restamos aquí, la lección de
                    // las alas invisibles de la .52).
                    Vector2 anclaBase = pl.MountedCenter - Main.screenPosition
                        + new Vector2(-direccion * 7f, -4f * grav);

                    Pluma[] plumas = _plumas;
                    float[] hues = _hues;
                    float[] fundidos = _fundidos;
                    float hueBase = flap * estilo.HueTiempo;

                    // v6.54.3 — EL CONTEO VIVO: las plumas del estilo
                    // (3..7; la Aurora mantiene sus 5 históricas).
                    int nAla = estilo.Plumas < 2 ? 2
                        : (estilo.Plumas > PlumasMax ? PlumasMax : estilo.Plumas);

                    int k = 0;
                    for (int lado = -1; lado <= 1; lado += 2)
                    {
                        // v6.52.2 — EL ANCLA PROPIA DE CADA ALA: la
                        // separación del par vive en el estilo (v6.54.3
                        // — la raíz separada es lo que hace que se LEAN
                        // DOS alas).
                        Vector2 ancla = anclaBase + new Vector2(lado * estilo.AnclajeSep, 0f);
                        for (int i = 0; i < nAla; i++)
                        {
                            // EL DESGARRE DE LA PUNTA (el «tear»): el hash
                            // determinista decide qué mechones de la punta
                            // viven en este instante — la punta se RASGA en
                            // filamentos que titilan (más violento al abrir).
                            // v6.55.0: el desgarre TAMBIÉN talla el ancho
                            // de los últimos nodos (va a ConstruirPluma).
                            float desgarre = VFXCore.Hash01(pl.whoAmI * 7 + i * 13 + (lado + 2) * 31,
                                (int)(time * 24f), 5);
                            float vivo = 0.55f + 0.45f * desgarre;
                            fundidos[k] = vivo * (0.72f + 0.28f * spread);

                            plumas[k] = ConstruirPluma(ancla, i, spread, flap, grav,
                                direccion, lado, time, estilo,
                                _pozosEje[k], _pozosSemi[k], _pozosSemi2[k], fundidos[k]);
                            float hue = (hueBase + i * estilo.HueEntre) % 1f;
                            if (hue < 0f) hue += 1f;
                            hues[k] = hue;
                            k++;
                        }
                    }

                    // === 1. LAS TRES CAPAS DE GEOMETRÍA (núcleo + cuerpo + halo) ===
                    int n = 0;
                    for (int capa = 0; capa < 3; capa++)
                        n = EmitirCapa(n, plumas, hues, fundidos, mezclaDia, capa, estilo,
                            nAla * 2);
                    if (n >= 3)
                        Volcar(gd, _fxPlano, n, false);

                    // === 2. LAS DOS PASADAS DE RUIDO (el pliegue de la aurora
                    //     FLUYENDO por cada pluma — el corazón del original) ===
                    n = EmitirCapaRuido(0, plumas, hues, mezclaDia, time, 0, fold, estilo,
                        nAla * 2);
                    if (n >= 3)
                        Volcar(gd, _fxTextura, n, true);
                    n = EmitirCapaRuido(0, plumas, hues, mezclaDia, time, 1, fold, estilo,
                        nAla * 2);
                    if (n >= 3)
                        Volcar(gd, _fxTextura, n, true);
                }
                finally
                {
                    if (cerroLote)
                        VFXCore.ReabrirLoteVanilla();
                }

                return true;
            }
            catch
            {
                try { VFXCore.ReabrirLoteVanilla(); } catch { }
                return false;
            }
        }

        private static bool AsegurarEfectos(GraphicsDevice gd, string ruido)
        {
            if (_fxPlano == null)
            {
                _fxPlano = new BasicEffect(gd)
                {
                    LightingEnabled = false,
                    VertexColorEnabled = true,
                    TextureEnabled = false,
                    Alpha = 1f,
                };
            }
            if (_fxTextura == null)
            {
                _fxTextura = new BasicEffect(gd)
                {
                    LightingEnabled = false,
                    VertexColorEnabled = true,
                    TextureEnabled = true,
                    Alpha = 1f,
                };
            }
            if (_ruido == null || _ruido.IsDisposed || _ruido.Tag as string != ruido)
            {
                // v6.54.0 — EL CACHÉ POR ESTILO: cada ala fluye con SU
                // textura de ruido (las 20 de la investigación).
                if (!_ruidos.TryGetValue(ruido, out Texture2D tex) || tex == null || tex.IsDisposed)
                {
                    if (_texturaProboFallar) return false;
                    try
                    {
                        tex = Terraria.ModLoader.ModContent.Request<Texture2D>(
                            ruido, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                        _ruidos[ruido] = tex;
                    }
                    catch
                    {
                        _texturaProboFallar = true;
                        return false;
                    }
                }
                _ruido = tex;
                _ruido.Tag = ruido;
            }
            _fxTextura.Texture = _ruido;
            return true;
        }

        /// <summary>El volcado de una tira (el de RayoStrip: mundo + soft-add + CullNone — SIN gestionar el lote: eso lo hace Dibujar).</summary>
        private static void Volcar(GraphicsDevice gd, BasicEffect fx, int nVerts, bool textura)
        {
            fx.World = Main.GameViewMatrix.TransformationMatrix;
            fx.View = Matrix.Identity;
            fx.Projection = Matrix.CreateOrthographicOffCenter(
                0, Main.screenWidth, Main.screenHeight, 0, 0, -1);

            gd.BlendState = _pantalla;
            gd.DepthStencilState = DepthStencilState.None;
            gd.RasterizerState = RasterizerState.CullNone;
            if (textura)
                gd.SamplerStates[0] = SamplerState.LinearWrap;

            foreach (EffectPass pass in fx.CurrentTechnique.Passes)
            {
                pass.Apply();
                int porLote = (_verts.Length / 3) * 3;
                int inicio = 0;
                while (inicio < nVerts)
                {
                    int cuenta = Math.Min(nVerts - inicio, porLote);
                    int prims = cuenta / 3;
                    if (prims > 0)
                        gd.DrawUserPrimitives(PrimitiveType.TriangleList,
                            _verts, inicio, prims);
                    inicio += cuenta;
                }
            }
        }

        /// <summary>
        /// LA REINICIALIZACIÓN (el patrón de la casa: device-lost y salidas
        /// de mundo — hilo principal via QueueMainThreadAction).
        /// </summary>
        public static void Reiniciar()
        {
            BasicEffect plano = _fxPlano, textura = _fxTextura;
            _fxPlano = null;
            _fxTextura = null;
            _ruido = null;
            _ruidos.Clear();      // v6.54.0 — el caché de ruidos también muere
            if (plano != null || textura != null)
            {
                Terraria.Main.QueueMainThreadAction(() =>
                {
                    try { plano?.Dispose(); } catch { }
                    try { textura?.Dispose(); } catch { }
                });
            }
        }
    }
}
