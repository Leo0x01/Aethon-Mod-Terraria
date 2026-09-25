using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// RiftLib (parcial) — v6.33 — LA FAMILIA DE LOS PORTALES.
    ///
    /// Uno de los TRES archivos parciales de la librería de los desgarros
    /// (organización v6.34: el núcleo del desgarro clásico, las paletas y
    /// las primitivas compartidas viven en el archivo maestro). Los
    /// desgarros nuevos nacen de las referencias del usuario analizadas con
    /// VLM (research/v633/refs + vlm_ref1/2.json). Aquí viven los tres de
    /// la familia de portales:
    ///   · PortalAnillos — "Se abre portal dimensional" (el túnel de anillos).
    ///   · OjoEspacial — "Apertura de portal dimensional" (el pliegue ojo).
    ///   · HeridaElectrica — "Desgarro de realidad eléctrica" (la grieta
    ///     con estática, arcos y ramas).
    /// (El cuarto desgarro v6.33 — DesgarroGlitch — vive en su parcial
    /// propio, el de la familia glitch.)
    ///
    /// CONTRATO: reciben el sprite batch CERRADO y lo dejan CERRADO
    /// (contrato v6.10) — gestionan sus lotes alfa y aditivo internamente
    /// (PortalAlpha/PortalAdditive del núcleo compartido). Coordenadas de
    /// PANTALLA (resta screenPosition antes de llamar).
    /// El split v6.34 es PURAMENTE mecánico: mismos métodos, mismos
    /// cuerpos, mismo namespace — los call-sites no cambian.
    /// </summary>
    public static partial class RiftLib
    {
        private static Asset<Texture2D> _disk;

        /// <summary>El disco negro sólido (el núcleo del pliegue).</summary>
        private static Texture2D DiskTex =>
            (_disk ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/BlackDisk")).Value;

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
                VFXCore.CerrarLoteSiAbierto();
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
                VFXCore.CerrarLoteSiAbierto();
            }
        }

        // ------------------------------------------------------------------
        //  3 — LA HERIDA ELÉCTRICA (la referencia: grieta lineal dentada con
        //  borde blanco→cian→violeta, interior de estática con scanlines,
        //  arcos voltaicos internos, strobe 10-30 Hz, chispas zig-zag)
        // ------------------------------------------------------------------

        /// <summary>
        /// LA HERIDA ELÉCTRICA — v6.39: EL DESGARRO RASGADO DE PUNTA A PUNTA.
        /// Antes era UN QUAD RECTO + arcos dentro ("si la realidad se
        /// desgarra no sería una fea línea recta" — el usuario tiene
        /// razón). Ahora:
        ///   · EL BORDE RASGADO — CaminoDesgarro (los dientes congelados de
        ///     la casa, deriva lenta, anclajes exactos).
        ///   · EL VACÍO por camino (RiftTaperVoid recortada a la meseta por
        ///     segmento, solape adaptativo + perlas — la geometría de la
        ///     cadena gemela) con la ESTÁTICA (scanlines) siguiendo la
        ///     tangente local del rasgado.
        ///   · LOS LABIOS de luz por camino (velo/cuerpo/núcleo + LA
        ///     ABERRACIÓN CROMÁTICA desplazada en perpendicular + el strobe).
        ///   · LOS ARCOS VOLTAICOS corriendo POR DENTRO del rasgado
        ///     (StormArc entre PUNTOS DEL CAMINO — la corriente sigue la
        ///     herida; v6.39 los arcos ya no se cortan por secciones: la
        ///     reparación de StormLib).
        ///   · LAS RAMIFICACIONES y LAS CHISPAS como siempre, pero naciendo
        ///     de los puntos del camino.
        /// `progress` 0→1 = la herida abriéndose.
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

            // v6.39 — EL CAMINO RASGADO (la herida con dientes).
            Vector2[] camino = CaminoDesgarro(origin, dir, len, seed, time, Rasgado(maxWidth));
            float h = w * 1.30f;

            // === 1. EL VACÍO DE LA HERIDA (pase alfa — el negro con ESTÁTICA) ===
            PortalAlpha();
            var b = Main.spriteBatch;
            // El cuerpo negro POR EL CAMINO (la banda del vacío — por
            // segmento con el solape adaptativo y las perlas: el negro
            // apilado es idempotente — cobertura garantizada).
            for (int i = 0; i < camino.Length - 1; i++)
            {
                Vector2 a = camino[i];
                Vector2 c = camino[i + 1];
                float segLen = Vector2.Distance(a, c);
                if (segLen < 0.30f) continue;
                float rot = (float)Math.Atan2(c.Y - a.Y, c.X - a.X);
                float giroA = i > 0 ? GiroEn(camino, i) : 0f;
                float giroB = i < camino.Length - 2 ? GiroEn(camino, i + 1) : 0f;
                float largo = segLen + ExtensionSolape(h * 0.5f, giroA) + ExtensionSolape(h * 0.5f, giroB);
                bool extremo = i == 0 || i == camino.Length - 2;

                TaperQuad(b, TaperVoidTex, (a + c) * 0.5f, largo, h, rot,
                    new Color(5, 5, 16, (byte)(int)(240 * alpha * open)), extremo);
                if (i > 0)
                    TaperQuad(b, TaperVoidTex, a, h * 0.75f, h, rot,
                        new Color(5, 5, 16, (byte)(int)(240 * alpha * open)));
            }

            // LAS SCANLINES (la estática interior — el buffer detrás de la
            // realidad), cada una sobre su punto del camino con la TANGENTE
            // local del rasgado (v6.39: antes todas sobre la recta).
            int lineas = Math.Max(3, (int)(len / 22f));
            for (int l = 0; l < lineas; l++)
            {
                float hh = H01(seed, l, 149);
                float hh2 = H01(seed, l, 151);
                // El punto del camino a esa fracción (aproximación por índice).
                float idxF = (0.08f + 0.84f * hh) * (camino.Length - 1);
                int i0 = Math.Clamp((int)idxF, 0, camino.Length - 2);
                float tt = idxF - i0;
                Vector2 p = Vector2.Lerp(camino[i0], camino[i0 + 1], tt);
                Vector2 segl = camino[i0 + 1] - camino[i0];
                float rotl = segl.LengthSquared() > 0.01f
                    ? (float)Math.Atan2(segl.Y, segl.X) : (float)Math.Atan2(dir.Y, dir.X);
                // Cada scanline parpadea a su frecuencia (10-30 Hz — el strobe).
                float st = MathF.Sin(tick * (0.17f + 0.34f * hh2) + hh * 6.28f) * 0.5f + 0.5f;
                if (st < 0.35f) continue;
                Quad(b, GlowTex, p, new Vector2(len * (0.10f + 0.16f * hh2), 1.3f + 1.5f * st),
                    rotl, Tint(blancoHielo, 0.10f * st * alpha * open));
            }
            Main.spriteBatch.End();

            // === 2. LA LUZ (aditivo — batch del llamador reabierto) ===
            PortalAdditive();
            b = Main.spriteBatch;

            // LA ABERRACIÓN CROMÁTICA por camino: el filo dibujado DOS VECES
            // desplazado en perpendicular (rojo a un lado, cian al otro —
            // la fractura óptica del rasgado).
            Vector2 perp = new(-dir.Y, dir.X);
            Vector2 abOff = perp * (1.6f + 1.2f * MathF.Sin(time * 13f));
            for (int i = 0; i < camino.Length - 1; i++)
            {
                Vector2 a = camino[i];
                Vector2 c = camino[i + 1];
                float segLen = Vector2.Distance(a, c);
                if (segLen < 0.30f) continue;
                float rot = (float)Math.Atan2(c.Y - a.Y, c.X - a.X);
                float giroA = i > 0 ? GiroEn(camino, i) : 0f;
                float giroB = i < camino.Length - 2 ? GiroEn(camino, i + 1) : 0f;
                float largo = segLen + ExtensionSolape(h * 0.5f, giroA, 0f) + ExtensionSolape(h * 0.5f, giroB, 0f);
                bool extremo = i == 0 || i == camino.Length - 2;

                // LA ABERRACIÓN (dos velos finos a cada lado).
                TaperQuad(b, TaperVeloTex, (a + c) * 0.5f + abOff, largo, h * 0.88f, rot,
                    Tint(rojoCA, 0.16f * alpha * open), extremo);
                TaperQuad(b, TaperVeloTex, (a + c) * 0.5f - abOff, largo, h * 0.88f, rot,
                    Tint(cian, 0.16f * alpha * open), extremo);

                // EL VELO violeta + EL CUERPO cian + EL NÚCLEO blanco (la herida).
                TaperQuad(b, TaperVeloTex, (a + c) * 0.5f, largo, h * 1.42f, rot,
                    Tint(violeta, 0.55f * alpha * open), extremo);
                TaperQuad(b, TaperVeloTex, (a + c) * 0.5f, largo, h * 1.12f, rot,
                    Tint(cian, 0.30f * alpha * open), extremo);
                TaperQuad(b, TaperCuerpoTex, (a + c) * 0.5f, largo, h * 0.77f, rot,
                    Tint(cian, 0.85f * alpha * open), extremo);
                TaperQuad(b, TaperNucleoTex, (a + c) * 0.5f, largo, h * 0.29f, rot,
                    Tint(blancoHielo, 1f * alpha * open), extremo);
            }

            // EL STROBE (10-30 Hz — el arco que falla): todo el cuerpo pica
            // (los segmentos pares del camino — el pico viaja).
            float strobe = MathF.Sin(tick * 0.38f + seed) * 0.5f + 0.5f;
            if (strobe > 0.62f)
            {
                for (int i = 0; i < camino.Length - 1; i += 2)
                {
                    Vector2 a = camino[i];
                    Vector2 c = camino[i + 1];
                    float segLen = Vector2.Distance(a, c);
                    if (segLen < 0.30f) continue;
                    float rot = (float)Math.Atan2(c.Y - a.Y, c.X - a.X);
                    TaperQuad(b, TaperVeloTex, (a + c) * 0.5f, segLen + 2f, h * 1.77f, rot,
                        Tint(cian, 0.22f * alpha * open * strobe));
                    TaperQuad(b, TaperNucleoTex, (a + c) * 0.5f, segLen + 2f, h * 0.42f, rot,
                        Tint(blancoHielo, 0.55f * alpha * open * strobe));
                }
            }

            // === 3. LOS ARCOS VOLTAICOS INTERNOS — CORRIENDO POR DENTRO del
            //     rasgado (v6.39: entre PUNTOS DEL CAMINO — la corriente
            //     SIGUE la herida; antes colgaban de la recta) ===
            int tramosArco = Math.Min(3, Math.Max(1, camino.Length / 6));
            for (int t = 0; t < tramosArco; t++)
            {
                int i0 = Math.Clamp(2 + t * 5 + (seed % 3), 1, camino.Length - 3);
                int i1 = Math.Min(i0 + 4, camino.Length - 1);
                Vector2 a0 = camino[i0] + perp * (w * 0.10f * (H01(seed, t, 157) - 0.5f) * 2f);
                Vector2 a1 = camino[i1] - perp * (w * 0.10f * (H01(seed, t, 163) - 0.5f) * 2f);
                StormLib.StormArc(b, a0, a1, seed + t * 31, time,
                    3.5f, cian, 0.65f * alpha * open);
            }

            // === 3b. LAS RAMIFICACIONES (la herida con ramas que se
            //     bifurcan como raíces o rayos — naciendo DEL CAMINO) ===
            const int Ramas = 5;
            for (int r = 0; r < Ramas; r++)
            {
                float h1 = H01(seed, r, 173);
                float h2 = H01(seed, r, 179);
                float h3 = H01(seed, r, 181);
                if (h1 < 0.30f) continue;                    // no todas nacen
                // La rama nace de un punto del camino (no de la recta).
                float idxF = (0.12f + 0.76f * h2) * (camino.Length - 1);
                int i0 = Math.Clamp((int)idxF, 1, camino.Length - 2);
                Vector2 nace = camino[i0];
                // La dirección local del rasgado en ese punto.
                Vector2 segl = camino[Math.Min(i0 + 1, camino.Length - 1)] - camino[i0];
                float rotLocal = segl.LengthSquared() > 0.01f
                    ? (float)Math.Atan2(segl.Y, segl.X) : (float)Math.Atan2(dir.Y, dir.X);
                float lado = h3 > 0.5f ? 1f : -1f;
                // La rama: 55°-75° del cuerpo (los rayos que brotan).
                float angRama = rotLocal + lado * (0.96f + 0.35f * h1);
                float largoR = len * (0.10f + 0.13f * h2);
                Vector2 fin = nace + new Vector2(MathF.Cos(angRama), MathF.Sin(angRama)) * largoR;
                // LA GRIETA SECUNDARIA: fractal cian/violeta (StormLib con
                // semilla propia — el zig-zag de las ramas de un rayo).
                StormLib.Bolt(b, nace, fin, seed + 601 + r * 43, (int)(tick * 0.4f),
                    5.5f, violeta, blancoHielo, 0.75f * alpha * open, 5, largoR * 0.18f);
                StormLib.Bolt(b, nace, fin, seed + 601 + r * 43, (int)(tick * 0.4f),
                    2.5f, cian, blancoHielo, 0.85f * alpha * open, 5, largoR * 0.18f);
            }

            // === 3c. CHISPAS A LO LARGO del camino (no de la recta) ===
            int tramos = Math.Max(2, (int)(len / 110f));
            for (int s = 0; s <= tramos; s++)
            {
                float hs = H01(seed, s, 191);
                if (hs < 0.45f) continue;
                float idxF = (s / (float)tramos) * (camino.Length - 1);
                int i0 = Math.Clamp((int)idxF, 0, camino.Length - 2);
                Vector2 sp = Vector2.Lerp(camino[i0], camino[i0 + 1], idxF - i0)
                             + perp * (w * 0.4f * (H01(seed, s, 193) - 0.5f) * 2f);
                float st = ((tick + s * 9f) % 30f) / 30f;
                StormLib.SparkBurst(b, sp, cian, w * 0.75f, 3, seed + 21 + s,
                    st, w * 0.8f);
            }

            // === 4. LAS CHISPAS ZIG-ZAG en los extremos (StormLib.SparkBurst) ===
            float sparkT = (tick % 26f) / 26f;
            StormLib.SparkBurst(b, camino[0], cian, w * 1.3f, 5, seed + 5,
                sparkT, w * 1.2f);
            StormLib.SparkBurst(b, camino[camino.Length - 1], cian, w * 1.3f, 5, seed + 9,
                sparkT, w * 1.2f);
            Main.spriteBatch.End();
        }
    }
}
