using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// AlasAuroraLib — v6.52.1 — LAS ALAS CREADAS POR CÓDIGO.
    ///
    /// LA TÉCNICA APRENDIDA (la investigación del usuario: «analiza las
    /// alas de este mod Calamity, hay wings creadas por código ahí,
    /// investiga bien cómo las creas, de ser posible decompila y aprende
    /// la técnica»): el mod CALAMITY OVERHAUL (hocha113/CalamityOverhaul
    /// en GitHub, abierto — se leyó la FUENTE, no hizo falta descompilar)
    /// construye sus ALAS PROCEDURALES así (WingsOfInterference +
    /// InterferenceWingRender.cs + BRelicAuroraWing.fx):
    ///
    ///   1. CINCO PLUMAS POR ALA son QUADS DE PRIMITIVAS GPU
    ///      (DrawUserPrimitives, TriangleStrip) — CERO sprites: cada
    ///      pluma nace de la espalda del jugador como un abanico que se
    ///      PLEGADO (fan vertical estrecho, off 0.50→0.92) se ABRE en
    ///      vuelo (fan casi horizontal, off 0.22→1.54) según
    ///      WingSpread (0..1 con Lerp 0.085 — el ala se despliega suave).
    ///   2. LA ONDA DE ALETEO se PROPAGA de la pluma interior a la
    ///      exterior: off += sin(WingFlap − i·0.45)·amplitud — las plumas
    ///      ondean EN CADENA, no en bloque (la firma visual).
    ///   3. LA PLUMA se AFINA: larga 88→48 px (interior→exterior) ×
    ///      (0.5+0.5·spread), raíz 3.5 px de semiancho → punta
    ///      14+8·spread (la pluma se ENSANCHA al abrirse).
    ///   4. EL COLOR VIAJA POR VÉRTICE (el «contrato de color de
    ///      vértice» del original: R=hue G=blanco-día B=pliegue
    ///      A=intensidad — allí lo lee el pixel shader; AQUÍ lo resuelve
    ///      el CPU): cada pluma lleva su hue (base + i·0.085) y el hue
    ///      SE DISPERSA a lo largo de la pluma (+along·0.16) y con el
    ///      tiempo (+t·0.015) — el espectro CORRE por el ala.
    ///   5. EL RUIDO DE PERLIN es el PLIEGUE VIVO: dos capas de ruido
    ///      desplazándose a frecuencias y velocidades distintas a lo
    ///      largo de la pluma (f1: along·1.7 − t·(0.20−fold·0.08),
    ///      f2: along·3.4 + t·0.12) — la «aurora» es el ruido FLUYENDO
    ///      por la superficie del ala.
    ///   6. EL DESGARRE DE LA PUNTA: la punta se RASGA en mechones — el
    ///      ruido decide dónde se abre el borde.
    ///   7. LA FÍSICA SE INYECTA SIN SPRITE: wingsLogic = 45 (la
    ///      Emperatriz) + wingTimeMax propio — el dibujado de alas de
    ///      vanilla lee Player.wings (el slot), NO wingsLogic: CERO
    ///      sprite vanilla, TODO el visual es el código (la casa ya lo
    ///      había verificado en CosmeticPlayer v6.50.48).
    ///
    /// LA ADAPTACIÓN DE LA CASA (por qué no hay .fx nuevo): el original
    /// lleva un pixel shader propio (BRelicAuroraWing.fx compilado a
    /// .fxc con fxc.exe/fx_2_0 — el pipeline documentado en
    /// COMPILACION.md: los .fx NO se compilan solos). Este entorno no
    /// dispone del compilador fx_2_0, así que EL MISMO lenguaje visual
    /// se resuelve con el pipeline DE PRIMITIVAS DE LA CASA (RayoStrip,
    /// v6.50.26): columnas gaussianas por vértice (núcleo blanco-cálido
    /// + cuerpo + halo tenue — la exp() del shader hecha geometría) +
    /// DOS PASADAS DE RUIDO con UVs que se DESPLAZAN por vértice (el
    /// Perlin fluyendo por la pluma — igual que el shader, pero el
    /// desplazamiento lo computa el CPU) + el desgarre de punta por
    /// hash determinista. El RESULTADO ES LA MISMA TÉCNICA: alas 100%
    /// código, sin un solo sprite.
    ///
    /// EL PAR COMPLETO: el original dibuja UN abanico de 5 plumas que
    /// se espeja con la dirección; AQUÍ son DIEZ plumas — 5 por CADA
    /// ALA (los dos abanicos simétricos del par de verdad).
    ///
    /// CONTRATO DE LOTE (el de la casa, como RayoStrip): el llamador
    /// (el halo-proyectil) trae el lote del pase de entidades ABIERTO;
    /// Dibujar lo cierra (sonda), dibuja las primitivas y lo REABRE
    /// (idempotente). Coordenadas de PANTALLA (mundo − Main.screenPosition
    /// — LA LECCIÓN v6.53: el pipeline de Volcar usa GameViewMatrix como
    /// World, que SOLO aplica el zoom — la cámara la resta EL LLAMADOR,
    /// exactamente como hacen todos los consumidores de RayoStrip; las
    /// alas nacieron en coordenadas de mundo y se dibujaban ~17 pantallas
    /// a la derecha — INVISIBLES por toda la .52, por eso «no se ve
    /// ningún ala en ningún lugar»).
    /// </summary>
    public static class AlasAuroraLib
    {
        // === LAS CONSTANTES DEL ORIGINAL (calibradas por Calamity
        //     Overhaul — se respetan casi byte a byte) ===

        /// <summary>Plumas por ala (el original: FeatherCount = 5).</summary>
        public const int PlumasPorAla = 5;

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

        /// <summary>El pozo de vértices del ala (triángulos planos — cero GC).</summary>
        private static VertexPositionColorTexture[] _verts = new VertexPositionColorTexture[1024];

        // LOS POZOS DE LA GEOMETRÍA (estáticos — la lección cero-GC de la casa:
        // el original cachea TODO, nada nace por frame).
        private static readonly Pluma[] _plumas = new Pluma[PlumasPorAla * 2];
        private static readonly float[] _hues = new float[PlumasPorAla * 2];
        private static readonly float[] _fundidos = new float[PlumasPorAla * 2];

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
        /// PALETA del ala (crossfade continuo — el contrato ColorGoozma)
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
        /// ciclo continuo con crossfade total (12/5 del ColorGoozma).</summary>
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

        /// <summary>Los vértices vivos de una pluma (raíz y punta, con perp).</summary>
        private struct Pluma
        {
            public Vector2 RaizMas, RaizMenos;   // raíz ± perp·semiancho
            public Vector2 PuntaMas, PuntaMenos; // punta ± perp·semiancho
            public float Largo;                  // 0..1 normalizado (para UV)
        }

        /// <summary>
        /// Construye la pluma i del ala `lado` (−1 derecha del sprite, +1
        /// izquierda) — el abanico del original con EL PAR PROPIO DE LA
        /// CASA (v6.52.2): cada ala vive su lado con la inclinación base
        /// que abre el hueco central, y el par entero barre hacia atrás
        /// con la dirección de vista.
        /// </summary>
        private static Pluma ConstruirPluma(Vector2 ancla, int i, float spread, float flap,
            float grav, int direccion, int lado, float time)
            => ConstruirPluma(ancla, i, spread, flap, grav, direccion, lado, time,
                AlasCatalogo.De(AlasCatalogo.Aurora));

        /// <summary>v6.54.0 — LA VERSIÓN CON ESTILO: la forma del abanico
        /// (largos, semianchos, inclinación y barrido) vive en el ala.</summary>
        private static Pluma ConstruirPluma(Vector2 ancla, int i, float spread, float flap,
            float grav, int direccion, int lado, float time, in EstiloAlas estilo)
        {
            float t = i / (float)(PlumasPorAla - 1);

            // EL ABANICO: plegado estrecho ↔ abierto horizontal (la
            // interpolación del original, respetada).
            float offPlegado = MathHelper.Lerp(OffPlegadoMin, OffPlegadoMax, t);
            float offAbierto = MathHelper.Lerp(OffAbiertoMin, OffAbiertoMax, t);
            float off = MathHelper.Lerp(offPlegado, offAbierto, spread);

            // LA ONDA DE ALETEO: de la pluma interior a la exterior (el
            // segundero del ala — la firma).
            float amplitud = 0.05f + 0.11f * spread;
            off += MathF.Sin(flap - i * 0.45f) * amplitud;

            // EL ÁNGULO (v6.52.2 — EL PAR QUE SE LEE): −π/2 (arriba) más
            // la INCLINACIÓN propia del lado (el hueco central) barrido
            // por el abanico — y todo el conjunto rotado hacia ATRÁS
            // según mira el jugador (más abierto = más barrido).
            float angulo = -MathHelper.PiOver2 + lado * (estilo.Inclinacion + off)
                - direccion * estilo.Barrido * (0.35f + 0.65f * spread);
            Vector2 dir = new(MathF.Cos(angulo), MathF.Sin(angulo) * grav);
            Vector2 perp = new(-dir.Y, dir.X);

            // EL LARGO que respira (interior→exterior × apertura + la
            // vibración sutil del aleteo).
            float largo = MathHelper.Lerp(estilo.LargoInterior, estilo.LargoExterior, t)
                * (0.5f + 0.5f * spread);
            largo *= 1f + MathF.Sin(flap * 0.7f + i * 0.6f) * 0.045f;

            float raizMedio = estilo.RaizSemiancho;
            float puntaMedio = estilo.PuntaSemianchoBase + estilo.PuntaSemianchoAbierto * spread;

            Pluma p;
            p.RaizMas = ancla + perp * raizMedio;
            p.RaizMenos = ancla - perp * raizMedio;
            Vector2 punta = ancla + dir * largo;
            p.PuntaMas = punta + perp * puntaMedio;
            p.PuntaMenos = punta - perp * puntaMedio;
            p.Largo = largo;
            return p;
        }

        // ==================================================================
        //  EL VOLCADO DE UNA CAPA (una tira de quads de pluma)
        // ==================================================================

        /// <summary>
        /// Vuelca las plumas como TRIÁNGULOS PLANOS: cada pluma es un quad
        /// con las CUATRO ESQUINAS de color propio (el contrato de color
        /// por vértice del original, resuelto en CPU) partido en dos
        /// triángulos compartiendo vértices — cero solapes, cero juntas
        /// (la lección de RayoStrip). `capa` elige el ancho y la
        /// intensidad: 0 núcleo · 1 cuerpo · 2 halo.
        /// </summary>
        private static int EmitirCapa(int nVerts, Pluma[] plumas, float[] hues,
            float[] fundidos, float mezclaDia, int capa)
            => EmitirCapa(nVerts, plumas, hues, fundidos, mezclaDia, capa,
                AlasCatalogo.De(AlasCatalogo.Aurora));

        /// <summary>v6.54.0 — LA VERSIÓN CON ESTILO (las intensidades de
        /// las capas viven en el ala).</summary>
        private static int EmitirCapa(int nVerts, Pluma[] plumas, float[] hues,
            float[] fundidos, float mezclaDia, int capa, in EstiloAlas estilo)
        {
            // LAS COLUMNAS (la gaussiana de RayoStrip adaptada a la pluma):
            // el núcleo estrecho casi blanco, el cuerpo medio, el halo
            // ancho que susurra — la exp(−cruz²·k) del shader hecha
            // tres anchos de geometría.
            float frac, inten;
            switch (capa)
            {
                case 0: frac = 0.30f; inten = estilo.NucleoInten; break;   // el eje blanco-caliente
                case 1: frac = 0.62f; inten = estilo.CuerpoInten; break;   // el cuerpo del prisma
                default: frac = 1f; inten = estilo.HaloInten; break;       // el halo que muere suave
            }

            for (int i = 0; i < plumas.Length; i++)
            {
                ref Pluma p = ref plumas[i];
                if (nVerts + 6 > _verts.Length)
                    break;
                float haciaBlanco = capa == 0 ? 0.72f : capa == 1 ? 0.30f : 0.12f;

                // LOS CUATRO COLORES (hue que SE DISPERSA a lo largo de
                // la pluma: la raíz y la punta viven hues distintos —
                // el espectro corre del hombro a la punta).
                Color cRaiz = TonoPluma(hues[i], mezclaDia, haciaBlanco, estilo);
                Color cPunta = TonoPluma(hues[i] + estilo.HueLargo, mezclaDia,
                    haciaBlanco * 0.7f, estilo);

                // LOS FUNDIDOS: la raíz entra suave (rootFade), la punta
                // se DESGARRA (el tear del original: el hash decide qué
                // mechones viven — determinista, todas las máquinas
                // iguales).
                float fRaiz = 0.22f;
                float fPunta = fundidos[i];

                Color cr = Pre(cRaiz, inten * fRaiz);
                Color cp = Pre(cPunta, inten * fPunta);

                // El quad de la capa: raíz±perp·frac y punta±perp·frac
                // (los semianchos ya viven en las esquinas — aquí se
                // ACHICAN hacia el centro por frac).
                Vector2 rMas = Lerp(p.RaizMenos, p.RaizMas, (1f + frac) * 0.5f);
                Vector2 rMen = Lerp(p.RaizMenos, p.RaizMas, (1f - frac) * 0.5f);
                Vector2 pMas = Lerp(p.PuntaMenos, p.PuntaMas, (1f + frac) * 0.5f);
                Vector2 pMen = Lerp(p.PuntaMenos, p.PuntaMas, (1f - frac) * 0.5f);

                VertexPositionColorTexture vrA = new(new Vector3(rMas, 0f), cr, new Vector2(0f, 0f));
                VertexPositionColorTexture vrB = new(new Vector3(rMen, 0f), cr, new Vector2(0f, 1f));
                VertexPositionColorTexture vpA = new(new Vector3(pMas, 0f), cp, new Vector2(1f, 0f));
                VertexPositionColorTexture vpB = new(new Vector3(pMen, 0f), cp, new Vector2(1f, 1f));

                // DOS TRIÁNGULOS compartiendo la diagonal (cero juntas).
                _verts[nVerts++] = vrA;
                _verts[nVerts++] = vrB;
                _verts[nVerts++] = vpA;
                _verts[nVerts++] = vrA;
                _verts[nVerts++] = vpA;
                _verts[nVerts++] = vpB;
            }
            return nVerts;
        }

        /// <summary>La versión texturizada: las UV del ruido FLUYEN por la pluma.</summary>
        private static int EmitirCapaRuido(int nVerts, Pluma[] plumas, float[] hues,
            float mezclaDia, float time, int pasada, float fold)
            => EmitirCapaRuido(nVerts, plumas, hues, mezclaDia, time, pasada, fold,
                AlasCatalogo.De(AlasCatalogo.Aurora));

        /// <summary>v6.54.0 — LA VERSIÓN CON ESTILO (la intensidad del
        /// ruido vive en el ala — el «caudal» de su flujo).</summary>
        private static int EmitirCapaRuido(int nVerts, Pluma[] plumas, float[] hues,
            float mezclaDia, float time, int pasada, float fold, in EstiloAlas estilo)
        {
            // LAS DOS FRECUENCIAS DEL ORIGINAL (f1 lenta que fluye HACIA
            // la punta, f2 fina que retrocede — el pliegue doble de la
            // aurora). El fold PLEGADO frena el flujo (la tinta quieta).
            float escalaU, velU, escalaV, velV, inten;
            if (pasada == 0)
            {
                escalaU = 1.7f; velU = 0.20f - fold * 0.08f; escalaV = 0.6f; velV = 0.31f;
                inten = 0.46f;
            }
            else
            {
                escalaU = 3.4f; velU = -0.12f; escalaV = 1.15f; velV = 0f;
                inten = 0.30f;
            }
            inten *= estilo.RuidoInten;

            for (int i = 0; i < plumas.Length; i++)
            {
                ref Pluma p = ref plumas[i];
                if (nVerts + 6 > _verts.Length)
                    break;
                float fase = hues[i] * 7.3f; // el «phase» del original: cada pluma desfasa su ruido

                float u0 = -time * velU + fase;
                float u1 = escalaU - time * velU + fase;
                float v0 = fase * velV * 0.1f;
                float v1 = escalaV + v0;

                Color c = TonoPluma(hues[i], mezclaDia, 0.05f, estilo);
                Color cr = Pre(c, inten * 0.30f);
                Color cp = Pre(c, inten);

                VertexPositionColorTexture vrA = new(new Vector3(p.RaizMas, 0f), cr, new Vector2(u0, v0));
                VertexPositionColorTexture vrB = new(new Vector3(p.RaizMenos, 0f), cr, new Vector2(u0, v1));
                VertexPositionColorTexture vpA = new(new Vector3(p.PuntaMas, 0f), cp, new Vector2(u1, v0));
                VertexPositionColorTexture vpB = new(new Vector3(p.PuntaMenos, 0f), cp, new Vector2(u1, v1));

                _verts[nVerts++] = vrA;
                _verts[nVerts++] = vrB;
                _verts[nVerts++] = vpA;
                _verts[nVerts++] = vrA;
                _verts[nVerts++] = vpA;
                _verts[nVerts++] = vpB;
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

                    int k = 0;
                    for (int lado = -1; lado <= 1; lado += 2)
                    {
                        // v6.52.2 — EL ANCLA PROPIA DE CADA ALA: ±5 px hacia
                        // su lado (la raíz separada del par — junto con la
                        // inclinación base, lo que hace que se LEAN DOS alas).
                        Vector2 ancla = anclaBase + new Vector2(lado * 5f, 0f);
                        for (int i = 0; i < PlumasPorAla; i++)
                        {
                            plumas[k] = ConstruirPluma(ancla, i, spread, flap, grav,
                                direccion, lado, time, estilo);
                            float hue = (hueBase + i * estilo.HueEntre) % 1f;
                            if (hue < 0f) hue += 1f;
                            hues[k] = hue;

                            // EL DESGARRE DE LA PUNTA (el «tear»): el hash
                            // determinista decide qué mechones de la punta
                            // viven en este instante — la punta se RASGA en
                            // filamentos que titilan (más violento al abrir).
                            float desgarre = VFXCore.Hash01(pl.whoAmI * 7 + i * 13 + (lado + 2) * 31,
                                (int)(time * 24f), 5);
                            float vivo = 0.55f + 0.45f * desgarre;
                            fundidos[k] = vivo * (0.72f + 0.28f * spread);
                            k++;
                        }
                    }

                    // === 1. LAS TRES CAPAS DE GEOMETRÍA (núcleo + cuerpo + halo) ===
                    int n = 0;
                    for (int capa = 0; capa < 3; capa++)
                        n = EmitirCapa(n, plumas, hues, fundidos, mezclaDia, capa, estilo);
                    if (n >= 3)
                        Volcar(gd, _fxPlano, n, false);

                    // === 2. LAS DOS PASADAS DE RUIDO (el pliegue de la aurora
                    //     FLUYENDO por cada pluma — el corazón del original) ===
                    n = EmitirCapaRuido(0, plumas, hues, mezclaDia, time, 0, fold, estilo);
                    if (n >= 3)
                        Volcar(gd, _fxTextura, n, true);
                    n = EmitirCapaRuido(0, plumas, hues, mezclaDia, time, 1, fold, estilo);
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
