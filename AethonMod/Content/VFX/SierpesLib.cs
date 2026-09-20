using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// SierpesLib — v6.38 — LA LIBRERÍA DE LA CAMADA DE LAS SIERPES.
    ///
    /// ONCE CRIATURAS segmentadas, cada una con SU forma de mover la misma
    /// cadena, nacidas de la investigación de locomoción (informe
    /// research/sierpes_v638/INFORME_LOCOMOCION.md — 14 patrones
    /// investigados en la web, 10 adoptados aquí + la cría). LA SIERPE
    /// ESTELAR ORIGINAL (CodigosLib.SierpeEstelar) NO SE TOCA: esta
    /// librería es su descendencia, no su reemplazo.
    ///
    ///   · OuroborosAstral(...)    — PATRÓN 2 (persecución cíclica): el
    ///     anillo de doce cuentas donde CADA UNA persigue a la siguiente
    ///     (la causalidad circular — nadie manda) y al morir colapsa en
    ///     espiral logarítmica (el mice problem).
    ///   · CaravanaEspectral(...)   — PATRÓN 3 (camino-memoria): el tren
    ///     fantasma — el cuerpo repite la TRAYECTORIA EXACTA de la cabeza
    ///     (nada de recortar esquinas).
    ///   · AnguilaSolar(...)        — PATRÓN 5 (onda viajera): el cuerpo
    ///     ES una fórmula — A(s)·sin(2π(s/λ−ft)) con envolvente creciente;
    ///     las CRESTAS son las que muerden.
    ///   · CienpiesRunico(...)      — PATRÓN 6 (marcha metacronal): las
    ///     patas con fase Δφ=45° plantándose alternas — el empuje repartido
    ///     entre doce apéndices, no un líder.
    ///   · FlageloEstelar(...)      — PATRÓN 7 (látigo): cadena Verlet con
    ///     constraint repartido por masa y afinamiento — la energía del
    ///     mango concentrada en la punta (v×√m ≈ const).
    ///   · ViboraGenesiaca(...)     — PATRÓN 9 (doble hélice): dos hebras
    ///     ±R sobre UNA espina — la ley de cadena vive solo en el esqueleto.
    ///   · BoaEclipse(...)          — PATRÓN 10 (constrictor): el anillo de
    ///     apriete — cada vuelta completa es un stack que aprieta más.
    ///   · FarolGuardian(...)       — PATRÓN 13 (IK FABRIK): la PUNTA manda
    ///     (reach adelante+atrás) — la criatura ALCANZA, no nada.
    ///   · CintaAurora(...)         — PATRÓN 14 (cinta al viento): espina
    ///     anclada + offset de senos inconmensurables — la amplitud es el
    ///     viento, y el viento es la velocidad del corredor.
    ///   · ManadaAstral(...)        — PATRÓN 8 (boids): seis cazadores con
    ///     distancia ELÁSTICA y liderazgo que SALTA al más cercano.
    ///   · CriaEstelar(...)         — LA CRÍA: la sierpe estelar en miniatura
    ///     — el ADN de la original hecho sirviente.
    ///
    /// CONTRATO de la casa (el de CodigosLib/OrbitaLib): coordenadas de
    /// MUNDO al entrar (la resta de screenPosition se hace aquí), lote
    /// cerrado→cerrado (AbrirAdditive … CerrarBatch en try/finally), cero
    /// Main.rand en el render (determinismo por Hash01 + senos).
    /// </summary>
    public static class SierpesLib
    {
        // ==================================================================
        //  EL PINCEL (las texturas procedurales del proyecto, cacheadas)
        // ==================================================================

        private static Asset<Texture2D> _star;

        /// <summary>La estrella de 4 puntas (16×16 — chispas y apófisis).</summary>
        private static Texture2D Star =>
            (_star ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/Star")).Value;

        /// <summary>El orbe de NÚCLEO SÓLIDO de la casa (VFXCore).</summary>
        private static Texture2D GlowOrb => VFXCore.GlowOrb;

        /// <summary>El halo suave de la casa (VFXCore).</summary>
        private static Texture2D SoftGlow => VFXCore.SoftGlow;

        /// <summary>Buffer de conversión mundo→pantalla (cero GC).</summary>
        private static readonly Vector2[] _scr = new Vector2[64];

        /// <summary>Tinte premultiplicado de la casa (RGB·f, alfa 255·f).</summary>
        private static Color Tint(Color c, float f) => OrbitaLib.Tint(c, f);

        // ==================================================================
        //  LAS PALETAS DE LA CAMADA (una identidad por criatura)
        // ==================================================================

        /// <summary>OUROBOROS — la cuenta del anillo: oro cálido.</summary>
        private static readonly Color OuroCuerpo = new(255, 208, 120);

        /// <summary>OUROBOROS — la soga de luz entre cuentas: turquesa.</summary>
        private static readonly Color OuroEnlace = new(120, 220, 255);

        /// <summary>CARAVANA — el cuerpo fantasma: verde espectral.</summary>
        private static readonly Color CaravanaCuerpo = new(140, 255, 220);

        /// <summary>CARAVANA — el farol de la cabeza: ámbar.</summary>
        private static readonly Color CaravanaFarol = new(255, 214, 120);

        /// <summary>ANGUILA — el cuerpo ondulante: amarillo solar.</summary>
        private static readonly Color AnguilaCuerpo = new(255, 236, 120);

        /// <summary>ANGUILA — la cresta caliente: blanco solar.</summary>
        private static readonly Color AnguilaCresta = new(255, 252, 220);

        /// <summary>CIEMPIÉS — placas: ámbar rúnico.</summary>
        private static readonly Color CienCuerpo = new(255, 176, 96);

        /// <summary>CIEMPIÉS — patas: frío pálido.</summary>
        private static readonly Color CienPata = new(180, 220, 255);

        /// <summary>CIEMPIÉS — la runa plantada: dorada.</summary>
        private static readonly Color CienRuna = new(255, 214, 106);

        /// <summary>FLAGELO — la carne del látigo: carmesí fuego.</summary>
        private static readonly Color FlageloCuerpo = new(255, 96, 64);

        /// <summary>FLAGELO — el mango: dorado.</summary>
        private static readonly Color FlageloMango = new(255, 196, 92);

        /// <summary>FLAGELO — la punta al chasquear: blanco caliente.</summary>
        private static readonly Color FlageloCrack = new(255, 240, 220);

        /// <summary>VÍBORA — la hebra A: dorada.</summary>
        private static readonly Color ViboraA = new(255, 214, 120);

        /// <summary>VÍBORA — la hebra B: violeta.</summary>
        private static readonly Color ViboraB = new(190, 120, 255);

        /// <summary>BOA — el lomo córneo: blanco hueso.</summary>
        private static readonly Color BoaCuerpo = new(240, 240, 230);

        /// <summary>BOA — el vientre en sombra: violeta.</summary>
        private static readonly Color BoaSombra = new(150, 90, 200);

        /// <summary>BOA — los anillos de apriete: ámbar.</summary>
        private static readonly Color BoaApriete = new(255, 176, 96);

        /// <summary>FAROL — la lámpara: ámbar cálido.</summary>
        private static readonly Color FarolLuz = new(255, 196, 92);

        /// <summary>FAROL — la cadena: fría.</summary>
        private static readonly Color FarolCadena = new(168, 224, 255);

        /// <summary>CINTA — el naciente de la aurora: verde.</summary>
        private static readonly Color CintaVerde = new(120, 255, 190);

        /// <summary>CINTA — el final de la aurora: violeta.</summary>
        private static readonly Color CintaVioleta = new(200, 130, 255);

        /// <summary>MANADA — el lobo estelar: azul.</summary>
        private static readonly Color ManadaCuerpo = new(140, 200, 255);

        /// <summary>MANADA — el ojo del cazador: dorado.</summary>
        private static readonly Color ManadaOjo = new(255, 214, 106);

        /// <summary>CRÍA — la carne: blanco estelar (el ADN de la sierpe).</summary>
        private static readonly Color CriaCuerpo = new(255, 248, 225);

        /// <summary>CRÍA — el halo: dorado suave.</summary>
        private static readonly Color CriaHalo = new(255, 214, 120);

        /// <summary>CRÍA — las aletas: cian pálido.</summary>
        private static readonly Color CriaAleta = new(168, 224, 255);

        // ==================================================================
        //  COMPOSITOR 1 — EL OUROBOROS ASTRAL (patrón 2: persecución cíclica)
        // ==================================================================

        /// <summary>
        /// EL OUROBOROS ASTRAL — el render del anillo que se persigue a sí
        /// mismo. Doce cuentas (nada de cabeza: la causalidad es CIRCULAR),
        /// cada una unida a la SIGUIENTE por la soga de luz de la
        /// persecución; el PUNTO DE LA MORDIDA (donde la cola alcanza a la
        /// cabeza) destella; y en el COLAPSO final la espiral logarítmica
        /// del mice problem se hace visible: las cuentas se aprietan y una
        /// estrella nace en el centro.
        /// </summary>
        /// <param name="segs">Posiciones de MUNDO de las 12 cuentas.</param>
        /// <param name="n">Número de cuentas vivas.</param>
        /// <param name="time">Tiempo animado.</param>
        /// <param name="seed">Semilla determinista.</param>
        /// <param name="alpha">Intensidad global (0..1).</param>
        /// <param name="colapso">0 = anillo vivo · 1 = espiral cerrándose.</param>
        public static void OuroborosAstral(Vector2[] segs, int n, float time,
            int seed, float alpha, float colapso)
        {
            if (n <= 1 || alpha <= 0.02f || Main.netMode == NetmodeID.Server) return;
            if (n > 24) n = 24;

            Vector2 off = Main.screenPosition;
            for (int i = 0; i < n; i++) _scr[i] = segs[i] - off;
            Vector2 centro = CentroDe(segs, n) - off;

            OrbitaLib.AbrirAdditive();
            try
            {
                float vivo = alpha * (1f + colapso * 1.2f);

                // === 1. LA SOGA DE LA PERSECUCIÓN: cada cuenta atada a la
                //     siguiente (el anillo entero es la cadena). ===
                for (int i = 0; i < n; i++)
                {
                    int j = (i + 1) % n;
                    Vector2 a = _scr[i], b = _scr[j];
                    Vector2 delta = b - a;
                    float len = delta.Length();
                    if (len < 0.5f) continue;
                    float rot = (float)Math.Atan2(delta.Y, delta.X);
                    OrbitaLib.Capsule((a + b) * 0.5f, len, 2.4f, rot,
                        Tint(OuroEnlace, 0.16f * vivo));
                }

                // === 2. LAS CUENTAS: orbe + halo + la chispa que titila
                //     (el destello de la persecución). ===
                for (int i = 0; i < n; i++)
                {
                    OrbitaLib.Quad(SoftGlow, _scr[i], new Vector2(38f, 38f), 0f,
                        Tint(OuroCuerpo, 0.14f * vivo));
                    OrbitaLib.Quad(GlowOrb, _scr[i], new Vector2(12f, 12f), 0f,
                        Tint(OuroCuerpo, 0.85f * vivo));
                    float h = VFXCore.Hash01(seed, 300 + i, 71);
                    float tw = 0.55f + 0.45f * (float)Math.Sin(time * (2.2f + h) + i * 2.4f);
                    OrbitaLib.Quad(Star, _scr[i], new Vector2(9f, 9f), time * (0.8f + h),
                        Tint(OuroEnlace, 0.50f * tw * vivo));
                }

                // === 3. EL PUNTO DE LA MORDIDA: donde la cola alcanza a la
                //     cabeza — la boca del ouroboros (índice n−1 → 0). ===
                {
                    Vector2 a = _scr[n - 1], b = _scr[0];
                    Vector2 d = b - a;
                    float len = MathF.Max(d.Length(), 1f);
                    d /= len;
                    float rot = (float)Math.Atan2(d.Y, d.X);
                    OrbitaLib.Capsule(b + d * 12f, 14f, 4.2f, rot,
                        Tint(OuroCuerpo, 0.80f * vivo));
                    OrbitaLib.Quad(Star, b, new Vector2(15f, 15f), rot,
                        Tint(OuroEnlace, 0.85f * vivo));
                }

                // === 4. EL CENTRO (solo en el colapso): la estrella que
                //     nace cuando el anillo se traga a sí mismo. ===
                if (colapso > 0.03f)
                {
                    float f = MathHelper.Clamp(colapso, 0f, 1f);
                    OrbitaLib.Quad(SoftGlow, centro, new Vector2(60f * f, 60f * f), 0f,
                        Tint(OuroCuerpo, 0.35f * f * alpha));
                    OrbitaLib.Quad(Star, centro, new Vector2(34f * f, 34f * f), time * 2.4f,
                        Tint(OuroEnlace, 0.95f * f * alpha));
                }
            }
            finally
            {
                OrbitaLib.CerrarBatch();
            }
        }

        // ==================================================================
        //  COMPOSITOR 2 — LA CARAVANA ESPECTRAL (patrón 3: camino-memoria)
        // ==================================================================

        /// <summary>
        /// LA CARAVANA ESPECTRAL — el render del tren fantasma. La cabeza
        /// es un FAROL que abre camino; el cuerpo es el RASTRO EXACTO —
        /// cápsulas y cuentas que se desvanecen hacia la cola (el pasado
        /// que aún arde), y chispas de memoria titilando sobre cada
        /// eslabón: la trayectoria hecha visible.
        /// </summary>
        /// <param name="segs">Posiciones de MUNDO (0 = farol, resto = rastro).</param>
        /// <param name="n">Número de segmentos vivos.</param>
        /// <param name="time">Tiempo animado.</param>
        /// <param name="seed">Semilla determinista.</param>
        /// <param name="alpha">Intensidad global (0..1).</param>
        public static void CaravanaEspectral(Vector2[] segs, int n, float time,
            int seed, float alpha)
        {
            if (n <= 0 || alpha <= 0.02f || Main.netMode == NetmodeID.Server) return;
            if (n > 32) n = 32;

            Vector2 off = Main.screenPosition;
            for (int i = 0; i < n; i++) _scr[i] = segs[i] - off;

            OrbitaLib.AbrirAdditive();
            try
            {
                // === 1. EL CUERPO: el rastro que se desvanece hacia la cola
                //     (el pasado más lejano, más fantasma). ===
                for (int i = 1; i < n; i++)
                {
                    float t = i / (float)Math.Max(1, n - 1);
                    float fade = (1f - t) * 0.55f + 0.08f;
                    float rd = MathHelper.Lerp(11f, 4f, t);

                    Vector2 prev = _scr[Math.Max(0, i - 1)];
                    Vector2 delta = _scr[i] - prev;
                    float len = delta.Length();
                    if (len > 0.5f)
                    {
                        float rot = (float)Math.Atan2(delta.Y, delta.X);
                        OrbitaLib.Capsule(_scr[i], rd * 2.2f, rd * 0.8f, rot,
                            Tint(CaravanaCuerpo, fade * alpha));
                    }
                    OrbitaLib.Quad(SoftGlow, _scr[i], new Vector2(rd * 3.4f, rd * 3.4f), 0f,
                        Tint(CaravanaCuerpo, fade * 0.45f * alpha));

                    // LA CHISPA DE MEMORIA: el titileo del eslabón.
                    float tw = 0.4f + 0.6f * (float)Math.Sin(time * 3.1f + i * 1.7f);
                    OrbitaLib.Quad(Star, _scr[i], new Vector2(rd * 1.3f, rd * 1.3f),
                        time * 0.9f + i, Tint(CaravanaCuerpo, 0.45f * tw * fade * alpha));
                }

                // === 2. EL FAROL (la cabeza): la luz que abre camino. ===
                {
                    OrbitaLib.Quad(SoftGlow, _scr[0], new Vector2(52f, 52f), 0f,
                        Tint(CaravanaFarol, 0.30f * alpha));
                    OrbitaLib.Quad(GlowOrb, _scr[0], new Vector2(20f, 20f), 0f,
                        Tint(CaravanaFarol, 0.95f * alpha));
                    // LA JAULA del farol: tres varillas alrededor.
                    for (int v = -1; v <= 1; v++)
                    {
                        float dir = time * 1.1f + v * 0.62f;
                        Vector2 punta = _scr[0] + new Vector2((float)Math.Cos(dir),
                            (float)Math.Sin(dir)) * 15f;
                        OrbitaLib.Capsule((_scr[0] + punta) * 0.5f, 14f, 2.6f, dir,
                            Tint(CaravanaFarol, 0.55f * alpha));
                    }
                }

                // === 3. LAS LUCIÉRNAGAS del farol: chispas que lo orbitan. ===
                for (int k = 0; k < 4; k++)
                {
                    float h = VFXCore.Hash01(seed, 500 + k, 53);
                    float ang = h * MathHelper.TwoPi + time * (1.4f + h * 0.8f);
                    Vector2 p = _scr[0] + new Vector2((float)Math.Cos(ang),
                        (float)Math.Sin(ang)) * (18f + 10f * h);
                    float tw = 0.4f + 0.6f * (float)Math.Sin(time * 4.2f + k * 2.1f);
                    OrbitaLib.Quad(Star, p, new Vector2(8f, 8f), ang,
                        Tint(CaravanaFarol, 0.60f * tw * alpha));
                }
            }
            finally
            {
                OrbitaLib.CerrarBatch();
            }
        }

        // ==================================================================
        //  COMPOSITOR 3 — LA ANGUILA SOLAR (patrón 5: onda viajera)
        // ==================================================================

        /// <summary>
        /// LA ANGUILA SOLAR — el render del cuerpo-fórmula. El cuerpo ES
        /// la onda: cápsulas a lo largo de la curva analítica con aletas
        /// dorsales en las CRESTAS (los únicos segmentos calientes — la
        /// envolvente creciente de la anguila) y una cabeza de dos ojos
        /// fríos. La onda viaja hacia atrás: la luz del cuerpo parece
        /// empujarla hacia delante.
        /// </summary>
        /// <param name="segs">Posiciones de MUNDO de la curva (0 = cabeza).</param>
        /// <param name="crestas">TRUE en los segmentos con |lateral| máximo.</param>
        /// <param name="n">Número de segmentos vivos.</param>
        /// <param name="time">Tiempo animado.</param>
        /// <param name="seed">Semilla determinista.</param>
        /// <param name="alpha">Intensidad global (0..1).</param>
        public static void AnguilaSolar(Vector2[] segs, bool[] crestas, int n,
            float time, int seed, float alpha)
        {
            if (n <= 1 || alpha <= 0.02f || Main.netMode == NetmodeID.Server) return;
            if (n > 32) n = 32;

            Vector2 off = Main.screenPosition;
            for (int i = 0; i < n; i++) _scr[i] = segs[i] - off;

            OrbitaLib.AbrirAdditive();
            try
            {
                float RadioDe(int i) => MathHelper.Lerp(12f, 5f, i / MathF.Max(1, n - 1));

                // === 1. EL VELO ondulante (el cuerpo entero sudando luz). ===
                for (int i = 0; i < n; i++)
                    OrbitaLib.Quad(SoftGlow, _scr[i],
                        new Vector2(RadioDe(i) * 4.0f, RadioDe(i) * 4.0f), 0f,
                        Tint(AnguilaCuerpo, 0.10f * alpha));

                // === 2. LA CARNE: cápsulas a lo largo de la curva — la
                //     cresta arde BLANCA, el valle queda tibio. ===
                for (int i = 1; i < n; i++)
                {
                    float rd = RadioDe(i);
                    Vector2 delta = _scr[i] - _scr[i - 1];
                    float len = delta.Length();
                    if (len < 0.5f) continue;
                    float rot = (float)Math.Atan2(delta.Y, delta.X);
                    bool cresta = crestas != null && i < crestas.Length && crestas[i];
                    Color carne = cresta ? AnguilaCresta : AnguilaCuerpo;
                    float f = cresta ? 0.85f : 0.50f;
                    OrbitaLib.Capsule(_scr[i], rd * 2.4f, rd * 0.95f, rot,
                        Tint(carne, f * alpha));

                    // LA ALETA DORSAL en la cresta: la varilla perpendicular.
                    if (cresta)
                    {
                        float perp = rot - MathHelper.PiOver2;
                        float largo = (10f + 6f * (i / (float)n)) *
                            (0.8f + 0.2f * MathF.Sin(time * 6.2f + i));
                        Vector2 punta = _scr[i] + new Vector2((float)Math.Cos(perp),
                            (float)Math.Sin(perp)) * largo;
                        OrbitaLib.Capsule((_scr[i] + punta) * 0.5f, largo, 2.8f, perp,
                            Tint(AnguilaCresta, 0.65f * alpha));
                        OrbitaLib.Quad(Star, punta, new Vector2(7f, 7f), perp,
                            Tint(AnguilaCresta, 0.70f * alpha));
                    }
                }

                // === 3. LA CABEZA: el hocico y sus dos ojos fríos. ===
                {
                    Vector2 dir = _scr[0] - _scr[Math.Min(1, n - 1)];
                    float len = dir.Length();
                    if (len > 0.5f) dir /= len; else dir = Vector2.UnitX;
                    float rumbo = (float)Math.Atan2(dir.Y, dir.X);

                    OrbitaLib.Quad(GlowOrb, _scr[0], new Vector2(26f, 26f), 0f,
                        Tint(AnguilaCuerpo, 0.95f * alpha));
                    OrbitaLib.Quad(SoftGlow, _scr[0], new Vector2(44f, 44f), 0f,
                        Tint(AnguilaCuerpo, 0.28f * alpha));
                    for (int o = -1; o <= 1; o += 2)
                    {
                        Vector2 ojo = _scr[0] + new Vector2(
                            (float)Math.Cos(rumbo + o * 0.55f),
                            (float)Math.Sin(rumbo + o * 0.55f)) * 8f;
                        OrbitaLib.Quad(GlowOrb, ojo, new Vector2(6f, 6f), 0f,
                            Tint(new Color(140, 240, 255), 0.95f * alpha));
                    }
                    // LAS BRANQUIAS: dos arcos que respiran con la onda.
                    for (int g = 1; g <= 2; g++)
                    {
                        float perp = rumbo + MathHelper.PiOver2;
                        Vector2 baseG = _scr[0] - dir * (6f + g * 7f);
                        float largo = 7f + 3f * (float)Math.Sin(time * 4.4f + g * 1.9f);
                        Vector2 punta = baseG + new Vector2((float)Math.Cos(perp),
                            (float)Math.Sin(perp)) * largo;
                        OrbitaLib.Capsule((baseG + punta) * 0.5f, largo, 2.2f, perp,
                            Tint(AnguilaCuerpo, 0.50f * alpha));
                    }
                }
            }
            finally
            {
                OrbitaLib.CerrarBatch();
            }
        }

        // ==================================================================
        //  COMPOSITOR 4 — EL CIEMPIÉS RÚNICO (patrón 6: marcha metacronal)
        // ==================================================================

        /// <summary>
        /// EL CIEMPIÉS RÚNICO — el render de la marcha repartida. El lomo
        /// son placas ámbar; las PATAS (una por placa, fase Δφ=45°) se
        /// plantan alternas — la runa dorada que deja cada pata plantada
        /// es el paso hecho escritura; y la cabeza lleva antenas frías.
        /// La onda de patas recorre el cuerpo de atrás hacia delante
        /// (marcha retrograda, como el ciempiés real).
        /// </summary>
        /// <param name="segs">Posiciones de MUNDO de las placas (0 = cabeza).</param>
        /// <param name="fasePata">La fase de cada pata (rad — φ_i = φ₀ − i·π/4).</param>
        /// <param name="n">Número de placas vivas.</param>
        /// <param name="time">Tiempo animado.</param>
        /// <param name="seed">Semilla determinista.</param>
        /// <param name="alpha">Intensidad global (0..1).</param>
        public static void CienpiesRunico(Vector2[] segs, float[] fasePata, int n,
            float time, int seed, float alpha)
        {
            if (n <= 1 || alpha <= 0.02f || Main.netMode == NetmodeID.Server) return;
            if (n > 32) n = 32;

            Vector2 off = Main.screenPosition;
            for (int i = 0; i < n; i++) _scr[i] = segs[i] - off;

            OrbitaLib.AbrirAdditive();
            try
            {
                float RadioDe(int i) => MathHelper.Lerp(11f, 5f, i / MathF.Max(1, n - 1));

                // === 1. EL LOMO: placas ámbar (cápsula + orbe por placa). ===
                for (int i = 1; i < n; i++)
                {
                    float rd = RadioDe(i);
                    Vector2 delta = _scr[i] - _scr[i - 1];
                    float len = delta.Length();
                    if (len > 0.5f)
                    {
                        float rot = (float)Math.Atan2(delta.Y, delta.X);
                        OrbitaLib.Capsule(_scr[i], rd * 2.3f, rd * 1.05f, rot,
                            Tint(CienCuerpo, 0.55f * alpha));
                    }
                    OrbitaLib.Quad(SoftGlow, _scr[i], new Vector2(rd * 3.6f, rd * 3.6f), 0f,
                        Tint(CienCuerpo, 0.12f * alpha));
                }

                // === 2. LAS PATAS: la onda metacronal — cada pata con SU
                //     fase; plantada (sin < 0) deja la runa en el suelo;
                //     alzada (sin > 0) se levanta en arco. ===
                for (int i = 0; i < n; i++)
                {
                    if (fasePata == null || i >= fasePata.Length) break;
                    float phi = fasePata[i];
                    float s = MathF.Sin(phi);
                    bool plantada = s < 0f;

                    // La inclinación de la pata: el barrido de la zancada —
                    // las patas SIEMPRE hacia el suelo (π/2 = abajo en
                    // pantalla), alternando el vaivén como el ciempiés.
                    float barrido = MathF.Cos(phi) * 0.55f;
                    float rd = RadioDe(i);
                    float largo = 13f + rd * 0.5f;

                    float dirA = MathHelper.PiOver2 + barrido * ((i % 2 == 0) ? 1f : -1f);
                    Vector2 pie = _scr[i] + new Vector2((float)Math.Cos(dirA),
                        (float)Math.Sin(dirA)) * largo;
                    OrbitaLib.Capsule((_scr[i] + pie) * 0.5f, largo, 2.6f, dirA,
                        Tint(CienPata, (plantada ? 0.60f : 0.30f) * alpha));

                    if (plantada)
                    {
                        // LA RUNA PLANTADA: el paso hecho escritura (titila
                        // mientras la pata apoya).
                        float tw = 0.6f + 0.4f * (float)Math.Sin(time * 5.0f + i * 0.9f);
                        OrbitaLib.Quad(Star, pie, new Vector2(10f, 10f),
                            MathHelper.PiOver4 + (i % 2 == 0 ? 0f : MathHelper.PiOver2),
                            Tint(CienRuna, 0.75f * tw * alpha));
                        OrbitaLib.Quad(SoftGlow, pie, new Vector2(18f, 18f), 0f,
                            Tint(CienRuna, 0.20f * alpha));
                    }
                    else
                    {
                        // La pata alzada: el arco con su puntita fría.
                        OrbitaLib.Quad(GlowOrb, pie, new Vector2(4.5f, 4.5f), 0f,
                            Tint(CienPata, 0.55f * alpha));
                    }
                }

                // === 3. LA CABEZA: las antenas y los ojos ámbar. ===
                {
                    Vector2 dir = _scr[0] - _scr[Math.Min(1, n - 1)];
                    float len = dir.Length();
                    if (len > 0.5f) dir /= len; else dir = Vector2.UnitX;
                    float rumbo = (float)Math.Atan2(dir.Y, dir.X);

                    OrbitaLib.Quad(GlowOrb, _scr[0], new Vector2(24f, 24f), 0f,
                        Tint(CienCuerpo, 0.92f * alpha));
                    OrbitaLib.Quad(SoftGlow, _scr[0], new Vector2(42f, 42f), 0f,
                        Tint(CienCuerpo, 0.25f * alpha));
                    // LAS ANTENAS: dos varillas que exploran al frente.
                    for (int a = -1; a <= 1; a += 2)
                    {
                        float dirA = rumbo + a * 0.5f +
                            0.16f * MathF.Sin(time * 2.6f + a) * a;
                        Vector2 punta = _scr[0] + new Vector2((float)Math.Cos(dirA),
                            (float)Math.Sin(dirA)) * 18f;
                        OrbitaLib.Capsule((_scr[0] + punta) * 0.5f, 17f, 2.4f, dirA,
                            Tint(CienPata, 0.55f * alpha));
                        OrbitaLib.Quad(Star, punta, new Vector2(6f, 6f), dirA,
                            Tint(CienPata, 0.70f * alpha));
                    }
                    // LOS OJOS.
                    for (int o = -1; o <= 1; o += 2)
                    {
                        Vector2 ojo = _scr[0] + new Vector2(
                            (float)Math.Cos(rumbo + o * 0.5f),
                            (float)Math.Sin(rumbo + o * 0.5f)) * 7f;
                        OrbitaLib.Quad(GlowOrb, ojo, new Vector2(5.5f, 5.5f), 0f,
                            Tint(CienRuna, 0.95f * alpha));
                    }
                }
            }
            finally
            {
                OrbitaLib.CerrarBatch();
            }
        }

        // ==================================================================
        //  COMPOSITOR 5 — EL FLAGELO ESTELAR (patrón 7: látigo)
        // ==================================================================

        /// <summary>
        /// EL FLAGELO ESTELAR — el render del látigo vivo. El MANGO es el
        /// aro dorado que orbita al portador; la cuerda se afina del mango
        /// a la punta (taper cuadrático — la masa que concentra la
        /// energía); y cuando la punta VIAJA, su carne se estira y arde:
        /// el CHASQUIDO es la punta vuelta blanco caliente.
        /// </summary>
        /// <param name="nodos">Posiciones de MUNDO (0 = mango, n−1 = punta).</param>
        /// <param name="n">Número de nodos vivos.</param>
        /// <param name="vTip">La velocidad ACTUAL de la punta (px/tick).</param>
        /// <param name="time">Tiempo animado.</param>
        /// <param name="seed">Semilla determinista.</param>
        /// <param name="alpha">Intensidad global (0..1).</param>
        public static void FlageloEstelar(Vector2[] nodos, int n, float vTip,
            float time, int seed, float alpha)
        {
            if (n <= 2 || alpha <= 0.02f || Main.netMode == NetmodeID.Server) return;
            if (n > 32) n = 32;

            Vector2 off = Main.screenPosition;
            for (int i = 0; i < n; i++) _scr[i] = nodos[i] - off;

            OrbitaLib.AbrirAdditive();
            try
            {
                float RadioDe(int i) => MathHelper.Lerp(15f, 4.5f, i / (float)(n - 1));
                float calor = MathHelper.Clamp(vTip / 26f, 0f, 1f);

                // === 1. LA CUERDA: cápsulas afinándose — el brillo de cada
                //     tramo crece con su velocidad propia (la energía se
                //     ve VIAJAR del mango a la punta). ===
                for (int i = 1; i < n; i++)
                {
                    float rd = RadioDe(i);
                    Vector2 delta = _scr[i] - _scr[i - 1];
                    float len = delta.Length();
                    if (len < 0.5f) continue;
                    float rot = (float)Math.Atan2(delta.Y, delta.X);

                    // La cercanía a la punta hereda el calor del chasquido.
                    float cercaPunta = i / (float)(n - 1);
                    float f = 0.45f + 0.35f * calor * cercaPunta;
                    Color carne = cercaPunta > 0.75f && calor > 0.55f
                        ? FlageloCrack : FlageloCuerpo;
                    OrbitaLib.Capsule(_scr[i], len * 1.05f, rd * 1.05f, rot,
                        Tint(carne, f * alpha));
                }

                // === 2. LOS NUDOS: estrellas pequeñas a lo largo (la
                //     articulación de la cuerda). ===
                for (int i = 1; i < n; i++)
                {
                    float rd = RadioDe(i);
                    float tw = 0.5f + 0.5f * (float)Math.Sin(time * 3.6f + i * 2.3f);
                    OrbitaLib.Quad(Star, _scr[i], new Vector2(rd * 1.4f, rd * 1.4f),
                        time * 1.6f + i, Tint(FlageloCuerpo, 0.45f * tw * alpha));
                }

                // === 3. EL MANGO (el nodo 0): el aro dorado del portador. ===
                {
                    OrbitaLib.AnilloFino(_scr[0], 11f, time * 1.8f,
                        Tint(FlageloMango, 0.80f * alpha));
                    OrbitaLib.Quad(GlowOrb, _scr[0], new Vector2(9f, 9f), 0f,
                        Tint(FlageloMango, 0.90f * alpha));
                }

                // === 4. LA PUNTA: la perínea del látigo — arde cuando
                //     viaja; el CHASQUIDO la vuelve estrella blanca. ===
                {
                    Vector2 punta = _scr[n - 1];
                    float f = 0.5f + calor * 0.5f;
                    OrbitaLib.Quad(SoftGlow, punta,
                        new Vector2(30f + 26f * calor, 30f + 26f * calor), 0f,
                        Tint(FlageloCuerpo, 0.35f * f * alpha));
                    OrbitaLib.Quad(GlowOrb, punta,
                        new Vector2(10f + 8f * calor, 10f + 8f * calor), 0f,
                            Tint(calor > 0.6f ? FlageloCrack : FlageloCuerpo,
                                0.95f * f * alpha));
                    if (calor > 0.6f)
                        OrbitaLib.Quad(Star, punta,
                            new Vector2(20f * calor, 20f * calor), time * 3.2f,
                            Tint(FlageloCrack, 0.90f * calor * alpha));
                }
            }
            finally
            {
                OrbitaLib.CerrarBatch();
            }
        }

        // ==================================================================
        //  COMPOSITOR 6 — LA VÍBORA GENESÍACA (patrón 9: doble hélice)
        // ==================================================================

        /// <summary>
        /// LA VÍBORA GENESÍACA — el render de la criatura de dos hebras.
        /// La ESPINA (la ley de cadena de la sierpe, intacta) queda como
        /// esqueleto fantasma; alrededor, DOS HEBRAS — la dorada y la
        /// violeta — se entrelazan girando; y en cada PUNTO DE TEJIDO
        /// (donde las hebras cruzan el eje) un destello blanco: la doble
        /// hélice leyéndose como escritura viva.
        /// </summary>
        /// <param name="espina">La espina central (coords de MUNDO).</param>
        /// <param name="hebraA">La hebra dorada (coords de MUNDO).</param>
        /// <param name="hebraB">La hebra violeta (coords de MUNDO).</param>
        /// <param name="n">Nodos vivos de cada hebra.</param>
        /// <param name="time">Tiempo animado.</param>
        /// <param name="seed">Semilla determinista.</param>
        /// <param name="alpha">Intensidad global (0..1).</param>
        /// <param name="compresion">0 = hélice abierta · 1 = enroscada (daño ×1.6).</param>
        public static void ViboraGenesiaca(Vector2[] espina, Vector2[] hebraA,
            Vector2[] hebraB, int n, float time, int seed, float alpha, float compresion)
        {
            if (n <= 1 || alpha <= 0.02f || Main.netMode == NetmodeID.Server) return;
            if (n > 24) n = 24;

            Vector2 off = Main.screenPosition;
            for (int i = 0; i < n; i++) _scr[i] = espina[i] - off;

            OrbitaLib.AbrirAdditive();
            try
            {
                float vivo = alpha * (1f + compresion * 0.6f);

                // === 1. LA ESPINA FANTASMA: la sierpe original en esqueleto
                //     (el ADN de la casa — la ley de cadena intacta). ===
                for (int i = 1; i < n; i++)
                {
                    Vector2 delta = _scr[i] - _scr[i - 1];
                    float len = delta.Length();
                    if (len < 0.5f) continue;
                    float rot = (float)Math.Atan2(delta.Y, delta.X);
                    OrbitaLib.Capsule(_scr[i], len * 0.9f, 1.8f, rot,
                        Tint(new Color(200, 200, 220), 0.14f * alpha));
                }

                // === 2. LAS DOS HEBRAS: cuentas doradas y violetas con sus
                //     cuerdas — el radio crece hacia la cola (hélice cónica). ===
                for (int h = 0; h < 2; h++)
                {
                    Color color = h == 0 ? ViboraA : ViboraB;
                    Vector2[] hebra = h == 0 ? hebraA : hebraB;
                    for (int i = 0; i < n; i++)
                    {
                        Vector2 p = hebra[i] - off;
                        float t = i / (float)(n - 1);
                        float rd = MathHelper.Lerp(5.5f, 9f, t);

                        OrbitaLib.Quad(GlowOrb, p, new Vector2(rd * 2f, rd * 2f), 0f,
                            Tint(color, 0.80f * vivo));
                        OrbitaLib.Quad(SoftGlow, p, new Vector2(rd * 3.6f, rd * 3.6f), 0f,
                            Tint(color, 0.16f * vivo));

                        if (i > 0)
                        {
                            Vector2 prev = hebra[i - 1] - off;
                            Vector2 delta = p - prev;
                            float len = delta.Length();
                            if (len > 0.5f)
                            {
                                float rot = (float)Math.Atan2(delta.Y, delta.X);
                                OrbitaLib.Capsule((p + prev) * 0.5f, len, 2.2f, rot,
                                    Tint(color, 0.35f * vivo));
                            }
                        }
                    }
                }

                // === 3. LA CABEZA: el hocico de la víbora (la espina manda,
                //     las hebras la visten) con los dos ojos heterócromos. ===
                {
                    Vector2 cabeza = _scr[0];
                    Vector2 dir = _scr[0] - _scr[Math.Min(1, n - 1)];
                    float len = dir.Length();
                    if (len > 0.5f) dir /= len; else dir = Vector2.UnitX;
                    float rumbo = (float)Math.Atan2(dir.Y, dir.X);

                    OrbitaLib.Quad(GlowOrb, cabeza, new Vector2(22f, 22f), 0f,
                        Tint(ViboraA, 0.85f * vivo));
                    // La lengua bífida: dos filamentos que exploran.
                    for (int l = -1; l <= 1; l += 2)
                    {
                        float dirL = rumbo + l * 0.24f +
                            0.10f * MathF.Sin(time * 5.0f + l);
                        Vector2 punta = cabeza + new Vector2((float)Math.Cos(dirL),
                            (float)Math.Sin(dirL)) * 16f;
                        OrbitaLib.Capsule((cabeza + punta) * 0.5f, 15f, 2.0f, dirL,
                            Tint(ViboraB, 0.70f * vivo));
                    }
                    for (int o = -1; o <= 1; o += 2)
                    {
                        Vector2 ojo = cabeza + new Vector2(
                            (float)Math.Cos(rumbo + o * 0.5f),
                            (float)Math.Sin(rumbo + o * 0.5f)) * 7f;
                        Color cojo = o < 0 ? ViboraA : ViboraB;
                        OrbitaLib.Quad(GlowOrb, ojo, new Vector2(5.5f, 5.5f), 0f,
                            Tint(cojo, 0.95f * vivo));
                    }
                }

                // === 4. LOS DESTELLOS DE TEJIDO: chispas blancas que viajan
                //     por las hebras (deterministas — la casa). ===
                for (int k = 0; k < 6; k++)
                {
                    float h = VFXCore.Hash01(seed, 700 + k, 89);
                    float tt = (time * (0.35f + h * 0.5f) + h) % 1f;
                    int i = (int)(tt * (n - 1));
                    Vector2 p = (h < 0.5f ? hebraA[i] : hebraB[i]) - off;
                    float tw = 0.5f + 0.5f * (float)Math.Sin(time * 6.0f + k * 2.0f);
                    OrbitaLib.Quad(Star, p, new Vector2(9f, 9f), time * 2.0f,
                        Tint(new Color(255, 250, 240), 0.55f * tw * vivo));
                }
            }
            finally
            {
                OrbitaLib.CerrarBatch();
            }
        }

        // ==================================================================
        //  COMPOSITOR 7 — LA BOA DEL ECLIPSE (patrón 10: constrictor)
        // ==================================================================

        /// <summary>
        /// LA BOA DEL ECLIPSE — el render de la que abraza. El cuerpo
        /// córneo (blanco por el lomo, sombra violeta en el vientre) se
        /// enrosca; y alrededor de la presa, LOS ANILLOS DE APRIETE: cada
        /// vuelta completa es un aro ámbar más que respira sobre la
        /// víctima — la muerte hecha visible como un eclipse de anillos.
        /// </summary>
        /// <param name="segs">Posiciones de MUNDO del cuerpo (0 = cabeza).</param>
        /// <param name="n">Segmentos vivos.</param>
        /// <param name="vueltas">Vueltas completas acumuladas.</param>
        /// <param name="centroPresa">El centro de la presa (MUNDO).</param>
        /// <param name="radioPresa">El radio del anillo de enrosque (px).</param>
        /// <param name="time">Tiempo animado.</param>
        /// <param name="seed">Semilla determinista.</param>
        /// <param name="alpha">Intensidad global (0..1).</param>
        public static void BoaEclipse(Vector2[] segs, int n, int vueltas,
            Vector2 centroPresa, float radioPresa, float time, int seed, float alpha)
        {
            if (n <= 1 || alpha <= 0.02f || Main.netMode == NetmodeID.Server) return;
            if (n > 32) n = 32;

            Vector2 off = Main.screenPosition;
            for (int i = 0; i < n; i++) _scr[i] = segs[i] - off;
            Vector2 presa = centroPresa - off;

            OrbitaLib.AbrirAdditive();
            try
            {
                float RadioDe(int i) => MathHelper.Lerp(14f, 6f, i / MathF.Max(1, n - 1));

                // === 1. EL CUERPO: lomo córneo + vientre en sombra (dos
                //     tonos — la boa pesa). ===
                for (int i = 1; i < n; i++)
                {
                    float rd = RadioDe(i);
                    Vector2 delta = _scr[i] - _scr[i - 1];
                    float len = delta.Length();
                    if (len < 0.5f) continue;
                    float rot = (float)Math.Atan2(delta.Y, delta.X);
                    OrbitaLib.Capsule(_scr[i], rd * 2.5f, rd * 1.15f, rot,
                        Tint(BoaCuerpo, 0.60f * alpha));
                    // El vientre: la cápsula violeta desplazada 2 px abajo.
                    Vector2 abajo = new Vector2(0f, 2.2f);
                    OrbitaLib.Capsule(_scr[i] + abajo, rd * 2.2f, rd * 0.7f, rot,
                        Tint(BoaSombra, 0.30f * alpha));
                }

                // === 2. LA CABEZA: la mandíbula pesada y el ojo ámbar. ===
                {
                    Vector2 dir = _scr[0] - _scr[Math.Min(1, n - 1)];
                    float len = dir.Length();
                    if (len > 0.5f) dir /= len; else dir = Vector2.UnitX;
                    float rumbo = (float)Math.Atan2(dir.Y, dir.X);

                    OrbitaLib.Quad(GlowOrb, _scr[0], new Vector2(28f, 28f), 0f,
                        Tint(BoaCuerpo, 0.95f * alpha));
                    OrbitaLib.Quad(SoftGlow, _scr[0], new Vector2(48f, 48f), 0f,
                        Tint(BoaSombra, 0.30f * alpha));
                    for (int m = -1; m <= 1; m += 2)
                    {
                        float dirM = rumbo + m * 0.34f;
                        Vector2 boca = _scr[0] + new Vector2((float)Math.Cos(dirM),
                            (float)Math.Sin(dirM)) * 15f;
                        OrbitaLib.Capsule((_scr[0] + boca) * 0.5f + dir * 5f, 15f, 4.0f, dirM,
                            Tint(BoaCuerpo, 0.80f * alpha));
                    }
                    Vector2 ojo = _scr[0] + new Vector2(
                        (float)Math.Cos(rumbo + 0.5f),
                        (float)Math.Sin(rumbo + 0.5f)) * 8f;
                    OrbitaLib.Quad(GlowOrb, ojo, new Vector2(6.5f, 6.5f), 0f,
                        Tint(BoaApriete, 0.95f * alpha));
                }

                // === 3. LOS ANILLOS DE APRIETE: las vueltas hechas aros —
                //     cada uno respirando (aprietan con la exhalación). ===
                {
                    int aros = Math.Min(Math.Max(vueltas, 0), 6);
                    for (int k = 0; k < aros; k++)
                    {
                        float latido = 0.72f + 0.28f *
                            (float)Math.Sin(time * 3.4f + k * MathHelper.PiOver2);
                        float rr = radioPresa + 6f + k * 5.5f;
                        OrbitaLib.AnilloFino(presa, rr, time * (0.4f + k * 0.18f) *
                            (k % 2 == 0 ? 1f : -1f),
                            Tint(BoaApriete, 0.55f * latido * alpha));
                    }
                }

                // === 4. LA COLA: la punta que busca a la cabeza (el
                ///    semi-ouroboros de la boa). ===
                {
                    Vector2 cola = _scr[n - 1];
                    float tw = 0.6f + 0.4f * (float)Math.Sin(time * 4.0f);
                    OrbitaLib.Quad(Star, cola, new Vector2(12f, 12f), time * 2.2f,
                        Tint(BoaApriete, 0.65f * tw * alpha));
                }
            }
            finally
            {
                OrbitaLib.CerrarBatch();
            }
        }

        // ==================================================================
        //  COMPOSITOR 8 — EL FAROL GUARDIÁN (patrón 13: IK FABRIK)
        // ==================================================================

        /// <summary>
        /// EL FAROL GUARDIÁN — el render del que alcanza. El ANCLA es un
        /// farol colgante (ámbar cálido en su jaula); la CADENA de
        /// eslabones fríos se ESTIRA hacia la presa — la FABRIK hace que
        /// el cuerpo parezca TENSARSE como un arco; y la MANO final (tres
        /// dedos de luz) abre sus falanges cuando toca.
        /// </summary>
        /// <param name="segs">Posiciones de MUNDO (0 = farol ancla, n−1 = mano).</param>
        /// <param name="n">Eslabones vivos.</param>
        /// <param name="time">Tiempo animado.</param>
        /// <param name="seed">Semilla determinista.</param>
        /// <param name="alpha">Intensidad global (0..1).</param>
        /// <param name="alcanzando">TRUE cuando la mano está sobre una presa.</param>
        public static void FarolGuardian(Vector2[] segs, int n, float time,
            int seed, float alpha, bool alcanzando)
        {
            if (n <= 2 || alpha <= 0.02f || Main.netMode == NetmodeID.Server) return;
            if (n > 32) n = 32;

            Vector2 off = Main.screenPosition;
            for (int i = 0; i < n; i++) _scr[i] = segs[i] - off;

            OrbitaLib.AbrirAdditive();
            try
            {
                // === 1. LA CADENA: eslabones fríos — el eslabón se estira
                ///    cuando la FABRIK tensa (la tensión se VEE). ===
                for (int i = 1; i < n - 2; i++)
                {
                    Vector2 delta = _scr[i] - _scr[i - 1];
                    float len = delta.Length();
                    if (len < 0.5f) continue;
                    float rot = (float)Math.Atan2(delta.Y, delta.X);
                    float rd = MathHelper.Lerp(8f, 5.5f, i / (float)n);
                    OrbitaLib.Capsule(_scr[i], len * 0.85f, rd, rot,
                        Tint(FarolCadena, 0.42f * alpha));
                    OrbitaLib.Quad(GlowOrb, _scr[i], new Vector2(rd, rd), 0f,
                        Tint(FarolCadena, 0.55f * alpha));
                }

                // === 2. EL FAROL (el ancla): la lámpara en su jaula. ===
                {
                    Vector2 ancla = _scr[0];
                    OrbitaLib.Quad(SoftGlow, ancla, new Vector2(58f, 58f), 0f,
                        Tint(FarolLuz, 0.35f * alpha));
                    OrbitaLib.Quad(GlowOrb, ancla, new Vector2(19f, 19f), 0f,
                        Tint(FarolLuz, 0.95f * alpha));
                    // LA JAULA: cuatro varillas en rombo alrededor.
                    for (int v = 0; v < 4; v++)
                    {
                        float dir = time * 0.9f + v * MathHelper.PiOver2;
                        Vector2 punta = ancla + new Vector2((float)Math.Cos(dir),
                            (float)Math.Sin(dir)) * 16f;
                        OrbitaLib.Capsule((ancla + punta) * 0.5f, 15f, 2.6f, dir,
                            Tint(FarolLuz, 0.55f * alpha));
                    }
                    // EL GANCHO superior.
                    OrbitaLib.AnilloFino(ancla + new Vector2(0f, -22f), 6f, time * 1.4f,
                        Tint(FarolCadena, 0.70f * alpha));
                }

                // === 3. LA MANO (la punta): tres dedos de luz que se ABREN
                //     al alcanzar — el FABRIK se lee como una mano tendida. ===
                {
                    Vector2 mano = _scr[n - 1];
                    Vector2 dir = _scr[n - 1] - _scr[n - 2];
                    float len = dir.Length();
                    if (len > 0.5f) dir /= len; else dir = Vector2.UnitX;
                    float rumbo = (float)Math.Atan2(dir.Y, dir.X);

                    float apertura = alcanzando ? 0.55f : 0.28f;
                    for (int d = -1; d <= 1; d++)
                    {
                        float dirD = rumbo + d * apertura +
                            0.08f * MathF.Sin(time * 4.4f + d * 1.6f);
                        Vector2 punta = mano + new Vector2((float)Math.Cos(dirD),
                            (float)Math.Sin(dirD)) * 13f;
                        OrbitaLib.Capsule((mano + punta) * 0.5f, 12f, 2.8f, dirD,
                            Tint(FarolCadena, 0.65f * alpha));
                        OrbitaLib.Quad(GlowOrb, punta, new Vector2(4.5f, 4.5f), 0f,
                            Tint(FarolLuz, 0.75f * alpha));
                    }
                    OrbitaLib.Quad(GlowOrb, mano, new Vector2(11f, 11f), 0f,
                        Tint(FarolCadena, 0.85f * alpha));
                    if (alcanzando)
                    {
                        float tw = 0.6f + 0.4f * (float)Math.Sin(time * 5.2f);
                        OrbitaLib.Quad(Star, mano, new Vector2(18f, 18f), time * 2.6f,
                            Tint(FarolLuz, 0.80f * tw * alpha));
                    }
                }
            }
            finally
            {
                OrbitaLib.CerrarBatch();
            }
        }

        // ==================================================================
        //  COMPOSITOR 9 — LA CINTA AURORA (patrón 14: cinta al viento)
        // ==================================================================

        /// <summary>
        /// LA CINTA AURORA — el render del estandarte vivo. El CUERPO es
        /// una cinta ancha que ondea (quads suaves orientados a lo largo,
        /// con el GRADIENTE de la aurora: verde en el broche, violeta en
        /// la punta); las CRESTAS llevan su destello; y el BROCHE dorado
        /// la ata a la espalda del corredor — la amplitud de la onda ES
        /// la velocidad de la carrera.
        /// </summary>
        /// <param name="cuerpo">Posiciones de MUNDO de la cinta ondulada.</param>
        /// <param name="n">Nodos vivos de la cinta.</param>
        /// <param name="viento">0..1 — el factor de viento (velocidad del jugador).</param>
        /// <param name="time">Tiempo animado.</param>
        /// <param name="seed">Semilla determinista.</param>
        /// <param name="alpha">Intensidad global (0..1).</param>
        public static void CintaAurora(Vector2[] cuerpo, int n, float viento,
            float time, int seed, float alpha)
        {
            if (n <= 2 || alpha <= 0.02f || Main.netMode == NetmodeID.Server) return;
            if (n > 32) n = 32;

            Vector2 off = Main.screenPosition;
            for (int i = 0; i < n; i++) _scr[i] = cuerpo[i] - off;

            OrbitaLib.AbrirAdditive();
            try
            {
                float vivo = alpha * (0.8f + 0.4f * MathHelper.Clamp(viento, 0f, 1f));

                // === 1. LA CINTA: quads anchos orientados a lo largo con
                //     el gradiente aurora (verde → violeta) — la anchura
                //     crece hacia la punta como una banderola. ===
                for (int i = 1; i < n; i++)
                {
                    float t = i / (float)(n - 1);
                    Vector2 delta = _scr[i] - _scr[i - 1];
                    float len = delta.Length();
                    if (len < 0.5f) continue;
                    float rot = (float)Math.Atan2(delta.Y, delta.X);
                    Color c = Color.Lerp(CintaVerde, CintaVioleta, t);
                    float ancho = MathHelper.Lerp(7f, 13f, t) * (1f + viento * 0.3f);

                    OrbitaLib.Quad(SoftGlow, _scr[i],
                        new Vector2(len * 1.15f, ancho), rot,
                        Tint(c, 0.28f * vivo));
                    OrbitaLib.Capsule(_scr[i], len * 0.9f, ancho * 0.32f, rot,
                        Tint(c, 0.50f * vivo));
                }

                // === 2. LAS CRESTAS: el filo de la aurora — destellos en
                //     los nodos alternos (deterministas). ===
                for (int i = 2; i < n; i += 2)
                {
                    float t = i / (float)(n - 1);
                    Color c = Color.Lerp(CintaVerde, CintaVioleta, t);
                    float tw = 0.5f + 0.5f * (float)Math.Sin(time * 3.8f + i * 1.3f);
                    float rotC = (float)Math.Atan2(
                        _scr[i].Y - _scr[i - 1].Y, _scr[i].X - _scr[i - 1].X);
                    OrbitaLib.Quad(Star, _scr[i], new Vector2(9f + 4f * viento,
                        9f + 4f * viento), rotC, Tint(c, 0.55f * tw * vivo));
                }

                // === 3. EL BROCHE: la estrella dorada que ata la cinta a
                //     la espalda del corredor. ===
                {
                    OrbitaLib.Quad(GlowOrb, _scr[0], new Vector2(13f, 13f), 0f,
                        Tint(new Color(255, 214, 120), 0.90f * alpha));
                    OrbitaLib.Quad(Star, _scr[0], new Vector2(17f, 17f), time * 1.1f,
                        Tint(new Color(255, 236, 180), 0.70f * alpha));
                }
            }
            finally
            {
                OrbitaLib.CerrarBatch();
            }
        }

        // ==================================================================
        //  COMPOSITOR 10 — LA MANADA ASTRAL (patrón 8: boids / cadena blanda)
        // ==================================================================

        /// <summary>
        /// LA MANADA ASTRAL — el render de los seis cazadores. Cada
        /// lobo-estrella: cuerpo azul con DOS OREJAS frías y el ojo dorado
        /// que apunta a la presa; su COLA (tres cuentas desvanecidas) es
        /// la distancia ELÁSTICA hecha visible — se estira cuando
        /// acelera, se comprime en el giro; y al LÍDER lo corona una
        /// estrella dorada: el mando que salta de cuello en cuello.
        /// </summary>
        /// <param name="cazas">Cabezas de los 6 cazadores (coords de MUNDO).</param>
        /// <param name="colas">Las colas aplanadas: 3 nodos por cazador.</param>
        /// <param name="nCaz">Número de cazadores vivos.</param>
        /// <param name="liderIdx">El índice del líder actual (−1 = ninguno).</param>
        /// <param name="rapidez">0..1 por cazador (|vel| normalizada — el calor).</param>
        /// <param name="time">Tiempo animado.</param>
        /// <param name="seed">Semilla determinista.</param>
        /// <param name="alpha">Intensidad global (0..1).</param>
        public static void ManadaAstral(Vector2[] cazas, Vector2[] colas, int nCaz,
            int liderIdx, float[] rapidez, float time, int seed, float alpha)
        {
            if (nCaz <= 0 || alpha <= 0.02f || Main.netMode == NetmodeID.Server) return;

            OrbitaLib.AbrirAdditive();
            try
            {
                Vector2 off = Main.screenPosition;
                for (int c = 0; c < nCaz; c++)
                {
                    Vector2 cuerpo = cazas[c] - off;
                    Vector2 atras = colas != null && c * 3 < colas.Length
                        ? colas[c * 3] - off : cuerpo;
                    Vector2 dir = cuerpo - atras;
                    float len = dir.Length();
                    if (len > 0.5f) dir /= len; else dir = Vector2.UnitX;
                    float rumbo = (float)Math.Atan2(dir.Y, dir.X);
                    float rap = rapidez != null && c < rapidez.Length
                        ? MathHelper.Clamp(rapidez[c], 0f, 1f) : 0f;

                    // === LA COLA (3 cuentas desvanecidas — la distancia
                    //     elástica hecha visible). ===
                    for (int k = 0; k < 3 && colas != null; k++)
                    {
                        int idx = c * 3 + k;
                        if (idx >= colas.Length) break;
                        Vector2 p = colas[idx] - off;
                        float fade = 0.34f - k * 0.09f;
                        float rd = 6f - k * 1.2f;
                        OrbitaLib.Quad(GlowOrb, p, new Vector2(rd, rd), 0f,
                            Tint(ManadaCuerpo, fade * alpha));
                    }

                    // === LAS OREJAS: dos varillas hacia atrás-arriba (el
                    //     lobo escucha a la manada). ===
                    for (int e = -1; e <= 1; e += 2)
                    {
                        float dirE = rumbo + MathHelper.Pi +
                            e * (0.5f + 0.12f * MathF.Sin(time * 6.0f + c * 1.9f) * e);
                        Vector2 punta = cuerpo + new Vector2((float)Math.Cos(dirE),
                            (float)Math.Sin(dirE)) * 13f;
                        OrbitaLib.Capsule((cuerpo + punta) * 0.5f, 12f, 2.6f, dirE,
                            Tint(ManadaCuerpo, 0.60f * alpha));
                    }

                    // === EL CUERPO: el lobo-estrella (arde al correr). ===
                    float f = 0.65f + 0.30f * rap;
                    OrbitaLib.Quad(SoftGlow, cuerpo,
                        new Vector2(34f + 14f * rap, 34f + 14f * rap), 0f,
                        Tint(ManadaCuerpo, 0.30f * alpha));
                    OrbitaLib.Quad(GlowOrb, cuerpo,
                        new Vector2(13f + 4f * rap, 13f + 4f * rap), 0f,
                        Tint(ManadaCuerpo, f * alpha));

                    // === EL OJO dorado (fijo al rumbo de la presa). ===
                    Vector2 ojo = cuerpo + dir * 5f;
                    OrbitaLib.Quad(GlowOrb, ojo, new Vector2(4.5f, 4.5f), 0f,
                        Tint(ManadaOjo, 0.95f * alpha));

                    // === LA CORONA DEL LÍDER: el mando que salta. ===
                    if (c == liderIdx)
                    {
                        float tw = 0.7f + 0.3f * (float)Math.Sin(time * 5.4f);
                        OrbitaLib.Quad(Star, cuerpo - new Vector2(0f, 20f),
                            new Vector2(15f, 15f), time * 2.4f,
                            Tint(ManadaOjo, 0.85f * tw * alpha));
                    }
                }
            }
            finally
            {
                OrbitaLib.CerrarBatch();
            }
        }

        // ==================================================================
        //  COMPOSITOR 11 — LA CRÍA ESTELAR (la sierpe en miniatura)
        // ==================================================================

        /// <summary>
        /// LA CRÍA ESTELAR — el render de la pequeña sierpe: el ADN visual
        /// de la original (blanco estelar + halo dorado + aletas cian)
        /// con proporciones de CACHORRO — la cabeza más redonda y grande,
        /// los OJOS enormes, solo DOS aletas (en los índices 4 y 8 de su
        /// cuerpecito de 10) y una coronita: la sierpe estelar hecha
        /// sirviente, diminuta y despierta.
        /// </summary>
        /// <param name="segs">Posiciones de MUNDO de los 10 segmentos.</param>
        /// <param name="ang">Ángulo de cada segmento (rad — el rumbo del nado).</param>
        /// <param name="n">Segmentos vivos.</param>
        /// <param name="time">Tiempo animado.</param>
        /// <param name="seed">Semilla determinista.</param>
        /// <param name="alpha">Intensidad global (0..1).</param>
        public static void CriaEstelar(Vector2[] segs, float[] ang, int n,
            float time, int seed, float alpha)
        {
            if (n <= 1 || alpha <= 0.02f || Main.netMode == NetmodeID.Server) return;
            if (n > 24) n = 24;
            // v6.49 — EL CLAMP DE LOS PARÁMETROS PARALELOS (hallazgo
            // AUD-B: las hermanas clampean contra crestas/fasePata pero
            // ESTA indexaba ang[i] sin check — un caller con ang más
            // corto que segs se comía un IndexOutOfRangeException).
            if (ang != null && ang.Length < n) n = ang.Length;
            if (segs != null && segs.Length < n) n = segs.Length;

            Vector2 off = Main.screenPosition;
            for (int i = 0; i < n; i++) _scr[i] = segs[i] - off;

            OrbitaLib.AbrirAdditive();
            try
            {
                // El TAPER de cachorro: cabeza redonda, cola fina.
                float RadioDe(int i) => MathHelper.Lerp(12f, 4f, i / MathF.Max(1, n - 1));

                // === 1. EL VELO: el cuerpecito sudando luz tenue. ===
                for (int i = 0; i < n; i++)
                    OrbitaLib.Quad(SoftGlow, _scr[i],
                        new Vector2(RadioDe(i) * 4.2f, RadioDe(i) * 4.2f), 0f,
                        Tint(CriaHalo, 0.10f * alpha));

                // === 2. LA ESPINA: cápsulas + apófisis estelares. ===
                for (int i = 1; i < n; i++)
                {
                    float rd = RadioDe(i);
                    OrbitaLib.Capsule(_scr[i], rd * 2.6f, rd * 0.95f, ang[i],
                        Tint(CriaCuerpo, 0.55f * alpha));
                    float tw = 0.7f + 0.3f * (float)Math.Sin(time * 2.8f + i * 1.8f);
                    OrbitaLib.Quad(Star, _scr[i], new Vector2(rd * 1.4f, rd * 1.4f),
                        ang[i], Tint(CriaHalo, 0.55f * tw * alpha));
                }

                // === 3. LAS ALETAS (solo dos — el cachorro todavía no las
                //     tiene gemelas): abanico de tres varillas. ===
                for (int f = 0; f < 2; f++)
                {
                    int i = f == 0 ? 4 : 8;
                    if (i >= n) continue;
                    float lado = f == 0 ? 1f : -1f;
                    float perp = ang[i] + MathHelper.PiOver2 * lado;
                    float latido = 0.8f + 0.2f * (float)Math.Sin(time * 3.2f + f * 1.2f);
                    for (int v = -1; v <= 1; v++)
                    {
                        float dirA = perp + v * 0.40f;
                        float largo = (13f + 5f * (1 - Math.Abs(v))) * latido;
                        Vector2 punta = _scr[i] + new Vector2((float)Math.Cos(dirA),
                            (float)Math.Sin(dirA)) * largo;
                        OrbitaLib.Capsule((_scr[i] + punta) * 0.5f, largo, 2.6f, dirA,
                            Tint(CriaAleta, 0.50f * alpha));
                        OrbitaLib.Quad(Star, punta, new Vector2(6f, 6f), dirA,
                            Tint(CriaAleta, 0.70f * alpha));
                    }
                }

                // === 4. LA CABECITA: redonda, con OJOS ENORMES y la
                //     coronita de la cría (es la hija de la sierpe). ===
                {
                    int i = 0;
                    float rumbo = ang[i];

                    // El hocico corto y romo.
                    for (int m = -1; m <= 1; m += 2)
                    {
                        float dirM = rumbo + m * 0.34f;
                        Vector2 boca = _scr[i] + new Vector2((float)Math.Cos(dirM),
                            (float)Math.Sin(dirM)) * 12f;
                        OrbitaLib.Capsule((_scr[i] + boca) * 0.5f, 11f, 3.6f, dirM,
                            Tint(CriaCuerpo, 0.85f * alpha));
                    }

                    OrbitaLib.Quad(GlowOrb, _scr[i], new Vector2(25f, 25f), 0f,
                        Tint(CriaCuerpo, 0.95f * alpha));
                    OrbitaLib.Quad(SoftGlow, _scr[i], new Vector2(42f, 42f), 0f,
                        Tint(CriaHalo, 0.25f * alpha));

                    // LOS OJOS ENORMES del cachorro.
                    for (int o = -1; o <= 1; o += 2)
                    {
                        Vector2 ojo = _scr[i] + new Vector2(
                            (float)Math.Cos(rumbo + o * 0.52f),
                            (float)Math.Sin(rumbo + o * 0.52f)) * 8f;
                        OrbitaLib.Quad(GlowOrb, ojo, new Vector2(7.5f, 7.5f), 0f,
                            Tint(new Color(140, 240, 255), 0.95f * alpha));
                    }

                    // LA CORONITA: la marca de la casa estelar.
                    float twC = 0.7f + 0.3f * (float)Math.Sin(time * 4.0f);
                    OrbitaLib.Quad(Star, _scr[i] - new Vector2(0f, 15f),
                        new Vector2(11f, 11f), time * 1.6f,
                        Tint(CriaHalo, 0.75f * twC * alpha));
                }

                // === 5. LAS CHISPITAS: el rastro de la cría. ===
                for (int k = 0; k < 4; k++)
                {
                    float h = VFXCore.Hash01(seed, 900 + k, 97);
                    float angS = h * MathHelper.TwoPi + time * (1.4f + h);
                    Vector2 p = _scr[0] + new Vector2((float)Math.Cos(angS),
                        (float)Math.Sin(angS)) * (16f + 10f * h);
                    float tw = 0.4f + 0.6f * (float)Math.Sin(time * 3.4f + k * 2.0f);
                    OrbitaLib.Quad(Star, p, new Vector2(6f, 6f), angS,
                        Tint(CriaHalo, 0.50f * tw * alpha));
                }
            }
            finally
            {
                OrbitaLib.CerrarBatch();
            }
        }

        // ==================================================================
        //  EL CENTRO (el aliado de los anillos)
        // ==================================================================

        /// <summary>El centroide de los primeros n puntos (MUNDO).</summary>
        private static Vector2 CentroDe(Vector2[] pts, int n)
        {
            Vector2 suma = Vector2.Zero;
            int cuenta = Math.Min(n, pts.Length);
            for (int i = 0; i < cuenta; i++) suma += pts[i];
            return cuenta > 0 ? suma / cuenta : Vector2.Zero;
        }
    }
}
