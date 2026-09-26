using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// RiftLib (parcial) — v6.35 — LOS CUATRO DESGARROS NUEVOS.
    ///
    /// El cuarto archivo parcial de la librería de los desgarros: las
    /// CUATRO primitivas nacidas de las nuevas referencias del usuario
    /// (imágenes analizadas con VLM, v6.35):
    ///   · VorticeColapso — "El Corazón del Colapso": la estrella de 4
    ///     puntas de FUEGO ESTELAR rotando (los filamentos de plasma que
    ///     se curvan tangencialmente, blanco amarillento → naranja →
    ///     carmesí → rojo oscuro).
    ///   · GargantaVacio — "La Garganta del Vacío": el vórtice circular
    ///     con NÚCLEO NEGRO ABSOLUTO, anillo magenta (la fórmula fiel
    ///     del shader recoloreada), brazos espirales y el polvo estelar
    ///     cayendo en espiral hacia dentro.
    ///   · UmbralRoto — "El Umbral Roto": el corte GEOMÉTRICO horizontal
    ///     perfecto — arriba EL VACÍO con el esqueleto espectral de la
    ///     otra realidad, abajo el mundo; la línea con aberración
    ///     cromática y partículas de datos cian.
    ///   · LeviatanEspectral — "El Leviatán": la columna de vértebras
    ///     etéreas nadando (la criatura marina ósea con sus aletas y su
    ///     cabeza con mandíbula) — se dibuja desde el proyectil vivo.
    ///
    /// CONTRATO (idéntico al resto de la familia): reciben el SpriteBatch
    /// CERRADO y lo dejan CERRADO — gestionan sus pases alfa/aditivo con
    /// PortalAlpha/PortalAdditive del núcleo. Coordenadas de PANTALLA
    /// (resta screenPosition antes de llamar).
    /// </summary>
    public static partial class RiftLib
    {
        // ------------------------------------------------------------------
        //  1 — EL CORAZÓN DEL COLAPSO (la referencia: estrella de 4 puntas
        //  rotante — vórtice de fuego estelar con filamentos curvos)
        // ------------------------------------------------------------------

        /// <summary>
        /// EL CORAZÓN DEL COLAPSO — la estrella de CUATRO PUNTAS de fuego
        /// estelar en rotación: cuatro filamentos de plasma que se curvan
        /// TANGENCIALMENTE (espirales logarítmicas — los brazos de una
        /// galaxia naciendo), gruesos en la base y afilados en las puntas,
        /// con el gradiente térmico de la referencia: núcleo blanco
        /// amarillento #FFFACD → naranja #FFA500 → carmesí #DC143C → el
        /// halo rojo oscuro #8B0000, y chispas de ORO #FFD700 orbitando
        /// hacia fuera. `progress` 0→1 = el corazón encendiéndose.
        /// </summary>
        public static void VorticeColapso(Vector2 center, float radius, float progress,
            float time, int seed, float alpha = 1f)
        {
            if (radius < 4f || alpha <= 0.02f) return;
            progress = MathHelper.Clamp(progress, 0f, 1f);
            try
            {
                Color blancoCalido = new(255, 250, 205);   // #FFFACD
                Color naranja = new(255, 165, 0);          // #FFA500
                Color carmesi = new(220, 20, 60);          // #DC143C
                Color rojo = new(255, 69, 0);              // #FF4500
                Color rojoOscuro = new(139, 0, 0);         // #8B0000
                Color oro = new(255, 215, 0);              // #FFD700

                float open = (float)Math.Pow(progress, 0.65f);
                float breathe = 1f + 0.05f * MathF.Sin(time * 1.9f + seed);
                float R = radius * open * breathe;
                if (R < 2f) return;

                // La ROTACIÓN del corazón (~0.55 rad/s — vivo pero legible).
                float spin = time * 0.55f + seed * 0.1f;

                PortalAdditive();
                var batch = Main.spriteBatch;

                // === 1. EL HALO ROJO OSCURO (el fondo del colapso) ===
                Quad(batch, GlowTex, center, new Vector2(R * 2.35f, R * 2.35f), 0f,
                    Tint(rojoOscuro, 0.20f * alpha * open));
                Quad(batch, GlowTex, center, new Vector2(R * 1.55f, R * 1.55f), 0f,
                    Tint(carmesi, 0.16f * alpha * open));

                // === 2. LOS CUATRO FILAMENTOS (las puntas de la estrella) ===
                const int Brazos = 4;
                const int Segmentos = 16;
                for (int a = 0; a < Brazos; a++)
                {
                    float baseAng = a * MathHelper.PiOver2 + spin;
                    Vector2 prev = center;
                    for (int s = 0; s <= Segmentos; s++)
                    {
                        float f = s / (float)Segmentos;              // 0=centro → 1=punta
                        // LA ESPIRAL LOGARÍTMICA: el ángulo se curva
                        // tangencialmente al crecer el radio (el filamento
                        // se enrosca — la referencia manda).
                        float r = R * (0.14f + 0.86f * f);
                        float ang = baseAng + 0.85f * f * f;         // la curva acumula
                        Vector2 p = center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * r;

                        // El grosor AFILA hacia la punta (grueso→fino).
                        float w = MathHelper.Lerp(R * 0.115f, R * 0.018f, f);
                        // El gradiente térmico: blanco en la base →
                        // naranja → carmesí en la punta.
                        Color c = Color.Lerp(blancoCalido, naranja, f * 1.4f);
                        if (f > 0.55f) c = Color.Lerp(naranja, carmesi, (f - 0.55f) / 0.45f);
                        // El latido viajando por el filamento.
                        float pulse = 0.75f + 0.25f * MathF.Sin(time * 3.2f - f * 5.5f + a * 1.6f);

                        if (s > 0)
                        {
                            Vector2 mid = (prev + p) * 0.5f;
                            float len = Vector2.Distance(prev, p);
                            if (len > 0.5f)
                            {
                                float rot = (float)Math.Atan2(p.Y - prev.Y, p.X - prev.X);
                                // Cápsula HALO + cápsula NÚCLEO (la receta de la casa).
                                Quad(batch, GlowTex, mid, new Vector2(len + w * 2.2f, w * 2.4f), rot,
                                    Tint(rojo, 0.34f * pulse * alpha * open));
                                Quad(batch, GlowTex, mid, new Vector2(len + w * 0.8f, w * 0.95f), rot,
                                    Tint(c, 0.85f * pulse * alpha * open));
                            }
                        }
                        prev = p;
                    }
                }

                // === 3. EL NÚCLEO (el corazón ardiendo) ===
                float heart = 0.5f + 0.5f * MathF.Sin(time * 2.4f + seed);
                // El destello de 4 puntas rotando con el conjunto.
                StarQuad(batch, center, spin, R * 0.85f * (0.85f + 0.15f * heart),
                    R * 0.10f, Tint(blancoCalido, 0.55f * alpha * open));
                StarQuad(batch, center, spin + MathHelper.PiOver2, R * 0.85f * (0.85f + 0.15f * heart),
                    R * 0.10f, Tint(blancoCalido, 0.55f * alpha * open));
                Quad(batch, GlowTex, center, new Vector2(R * 0.62f, R * 0.62f), 0f,
                    Tint(naranja, 0.50f * alpha * open));
                Quad(batch, GlowTex, center, new Vector2(R * 0.30f, R * 0.30f), 0f,
                    Tint(blancoCalido, (0.80f + 0.20f * heart) * alpha * open));

                // === 4. LAS CHISPAS DE ORO orbitando hacia fuera ===
                const int Chispas = 10;
                for (int d = 0; d < Chispas; d++)
                {
                    float h1 = H01(seed, d, 211);
                    float h2 = H01(seed, d, 223);
                    // Cada chispa espiralea HACIA FUERA (la semilla del colapso).
                    float t = (time * (0.22f + 0.26f * h2) + h1) % 1f;     // 0=centro → 1=fuera
                    float rr = R * (0.30f + 0.85f * t);
                    float ang = h2 * MathHelper.TwoPi + spin * 1.6f + t * 1.9f;
                    Vector2 pp = center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * rr;
                    float s = 2.5f + 4.5f * h2;
                    float vida = MathF.Sin(t * MathF.PI);                  // nace y muere suave
                    Quad(batch, OrbTex, pp, new Vector2(s, s), 0f,
                        Tint(Color.Lerp(oro, blancoCalido, h1), 0.65f * vida * alpha * open));
                }
                Main.spriteBatch.End();
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }
        }

        // ------------------------------------------------------------------
        //  2 — LA GARGANTA DEL VACÍO (la referencia: el vórtice circular
        //  con núcleo negro absoluto, anillo magenta y polvo succionado)
        // ------------------------------------------------------------------

        /// <summary>
        /// LA GARGANTA DEL VACÍO — el vórtice devorador: el NÚCLEO NEGRO
        /// ABSOLUTO #000000 (pase alfa sólido — la nada del otro lado), el
        /// ANILLO ENERGÉTICO MAGENTA (la fórmula fiel del shader
        /// sin(uv.x·20−t·5) recoloreada: picos blanco-rosado #FFE4FA, media
        /// #FF40D0, valles #5A0B7A — OrbitaLib.AnilloEnergia), el aro del
        /// horizonte, TRES BRAZOS ESPIRALES de galaxia y el POLVO ESTELAR
        /// blanco/cian cayendo en ESPIRAL hacia la nada. `progress` 0→1 =
        /// la garganta abriéndose.
        /// </summary>
        public static void GargantaVacio(Vector2 center, float radius, float progress,
            float time, int seed, float alpha = 1f)
        {
            if (radius < 4f || alpha <= 0.02f) return;
            progress = MathHelper.Clamp(progress, 0f, 1f);
            try
            {
                Color negroVacio = new(5, 2, 8);          // el negro con alma
                Color blancoRosado = new(255, 228, 250);  // #FFE4FA
                Color magenta = new(255, 64, 208);        // #FF40D0
                Color violetaHondo = new(90, 11, 122);    // #5A0B7A
                Color cianEstelar = new(224, 255, 255);   // #E0FFFF

                float open = (float)Math.Pow(progress, 0.7f);
                float R = radius * open;
                if (R < 2f) return;

                // === PASO 1 — EL NÚCLEO NEGRO ABSOLUTO (pase alfa) ===
                PortalAlpha();
                var batch = Main.spriteBatch;
                Quad(batch, DiskTex, center, new Vector2(R * 0.92f, R * 0.92f), 0f,
                    new Color(negroVacio.R, negroVacio.G, negroVacio.B,
                        (byte)(int)(242 * alpha * open)));
                Main.spriteBatch.End();

                // === PASO 2 — LA GARGANTA (aditivo) ===
                PortalAdditive();
                batch = Main.spriteBatch;

                // El resplandor blanco del borde interior (el bloom de la nada).
                Quad(batch, GlowTex, center, new Vector2(R * 1.30f, R * 1.30f), 0f,
                    Tint(blancoRosado, 0.14f * alpha * open));
                // El velo violeta profundo del fondo.
                Quad(batch, GlowTex, center, new Vector2(R * 1.90f, R * 1.90f), 0f,
                    Tint(violetaHondo, 0.22f * alpha * open));

                // EL ANILLO ENERGÉTICO — mitad TRASERA (la fórmula fiel del
                // shader con la paleta magenta de la referencia).
                int flick = (int)(time * 12f);
                OrbitaLib.AnilloEnergia(center, R * 0.62f, time, seed, flick, front: false,
                    hot: blancoRosado, mid: magenta, deep: violetaHondo);

                // TRES BRAZOS ESPIRALES (la galaxia de la referencia).
                const int Brazos = 3;
                const int Segmentos = 13;
                for (int a = 0; a < Brazos; a++)
                {
                    float baseAng = a * MathHelper.TwoPi / Brazos - time * 0.42f;
                    Vector2 prev = center;
                    for (int s = 0; s <= Segmentos; s++)
                    {
                        float f = s / (float)Segmentos;              // 0=dentro → 1=fuera
                        float r = R * (0.55f + 0.95f * f);
                        float ang = baseAng + 1.25f * f;
                        Vector2 p = center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * r;
                        if (s > 0)
                        {
                            Vector2 mid = (prev + p) * 0.5f;
                            float len = Vector2.Distance(prev, p);
                            if (len > 0.5f)
                            {
                                float rot = (float)Math.Atan2(p.Y - prev.Y, p.X - prev.X);
                                float w = MathHelper.Lerp(R * 0.085f, R * 0.016f, f);
                                // El brazo se APAGA hacia fuera (la galaxia se ralo).
                                Color c = Color.Lerp(magenta, violetaHondo, f * 0.8f);
                                Quad(batch, GlowTex, mid, new Vector2(len + w * 2f, w * 2.6f), rot,
                                    Tint(c, (0.34f - 0.16f * f) * alpha * open));
                            }
                        }
                        prev = p;
                    }
                }

                // EL ARO DEL HORIZONTE (el filo fino de la boca).
                Quad(batch, RingTex, center, new Vector2(R * 1.28f, R * 1.28f), time * 0.12f,
                    Tint(blancoRosado, 0.34f * alpha * open));

                // EL ANILLO ENERGÉTICO — mitad DELANTERA (pasa por delante
                // del núcleo: la profundidad del vórtice).
                OrbitaLib.AnilloEnergia(center, R * 0.62f, time, seed, flick, front: true,
                    hot: blancoRosado, mid: magenta, deep: violetaHondo);

                // === 3. EL POLVO ESTELAR cayendo en espiral (la succión) ===
                const int Polvo = 12;
                for (int d = 0; d < Polvo; d++)
                {
                    float h1 = H01(seed, d, 227);
                    float h2 = H01(seed, d, 229);
                    float t = 1f - ((time * (0.24f + 0.30f * h2) + h1) % 1f); // 1=fuera → 0=nada
                    float rr = R * 1.65f * t;
                    float ang = h2 * MathHelper.TwoPi - time * (1.4f + h1 * 1.1f) + t * 2.2f;
                    Vector2 pp = center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * rr;
                    float s = 2.5f + 4f * h2;
                    // El ESTIRÓN de la caída: la partícula se alarga TANGENCIALMENTE
                    // (el motion blur de la referencia).
                    Quad(batch, OrbTex, pp, new Vector2(s, s * (1f + t * 0.7f)), ang,
                        Tint(Color.Lerp(blancoRosado, cianEstelar, h1),
                            0.55f * t * alpha * open));
                }
                Main.spriteBatch.End();
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }
        }

        // ------------------------------------------------------------------
        //  3 — EL UMBRAL ROTO (la referencia: el corte horizontal perfecto
        //  — el vacío con el esqueleto arriba, la línea con aberración)
        // ------------------------------------------------------------------

        /// <summary>
        /// EL UMBRAL ROTO — el corte GEOMÉTRICO horizontal que parte la
        /// realidad: ARRIBA de la línea EL VACÍO (la banda oscura de la
        /// otra realidad) donde flota EL ESQUELETO ESPECTRAL — una criatura
        /// ósea de vértebras y mandíbula dibujada en líneas blancas
        /// grisesáceas #E0E0E0 meciéndose lenta — y ABAJO el mundo tal
        /// cual. La LÍNEA del corte: blanca nítida con ABERRACIÓN
        /// CROMÁTICA (eco cian arriba, eco magenta abajo) y partículas de
        /// DATOS cian #00FFFF parpadeando cerca. `progress` 0→1 = el
        /// umbral rajándose.
        /// </summary>
        public static void UmbralRoto(Vector2 center, float ancho, float progress,
            float time, int seed, float alpha = 1f)
        {
            if (ancho < 16f || alpha <= 0.02f) return;
            progress = MathHelper.Clamp(progress, 0f, 1f);
            float tick = time * 60f;

            Color vacio = new(0, 0, 10);
            Color hueso = new(224, 224, 224);         // #E0E0E0
            Color blanco = new(255, 255, 255);
            Color cian = new(0, 255, 255);            // #00FFFF
            Color magenta = new(255, 0, 249);         // #FF00F9

            float open = (float)Math.Pow(progress, 0.55f);
            float len = ancho * open;
            float alto = Math.Min(120f, ancho * 0.18f) * open;

            // === PASO 1 — EL VACÍO SUPERIOR (pase alfa: la otra realidad) ===
            PortalAlpha();
            var batch = Main.spriteBatch;
            // La banda oscura ENCIMA de la línea (el disco negro estirado —
            // la realidad pelada hacia arriba).
            Vector2 bandaC = center - new Vector2(0f, alto * 0.5f);
            Quad(batch, DiskTex, bandaC, new Vector2(len * 1.06f, alto * 2.15f), 0f,
                new Color(vacio.R, vacio.G, vacio.B, (byte)(int)(205 * alpha * open)));
            Quad(batch, DiskTex, bandaC, new Vector2(len * 0.94f, alto * 1.55f), 0f,
                new Color(vacio.R, vacio.G, vacio.B, (byte)(int)(235 * alpha * open)));
            Main.spriteBatch.End();

            // === PASO 2 — LA LUZ DEL UMBRAL (aditivo) ===
            PortalAdditive();
            batch = Main.spriteBatch;

            // LA LÍNEA DEL CORTE con ABERRACIÓN CROMÁTICA: el filo blanco
            // y sus dos ecos partidos (cian arriba, magenta abajo).
            float flick = 0.80f + 0.20f * MathF.Sin(tick * 0.31f + seed);
            Vector2 abOff = new Vector2(0f, 1.5f + 1.1f * MathF.Sin(time * 9f));
            Quad(batch, GlowTex, center + abOff, new Vector2(len, 3.2f), 0f,
                Tint(cian, 0.40f * flick * alpha * open));
            Quad(batch, GlowTex, center - abOff, new Vector2(len, 3.2f), 0f,
                Tint(magenta, 0.40f * flick * alpha * open));
            Quad(batch, GlowTex, center, new Vector2(len, 2.6f), 0f,
                Tint(blanco, 0.85f * flick * alpha * open));
            Quad(batch, GlowTex, center, new Vector2(len, 7.5f), 0f,
                Tint(cian, 0.10f * alpha * open));

            // LOS EXTREMOS del corte (donde la realidad sigue rasgándose).
            StarQuad(batch, center - new Vector2(len * 0.5f, 0f), 0f, 14f, 3.2f,
                Tint(blanco, 0.55f * alpha * open));
            StarQuad(batch, center + new Vector2(len * 0.5f, 0f), 0f, 14f, 3.2f,
                Tint(blanco, 0.55f * alpha * open));

            // === 3. EL ESQUELETO ESPECTRAL (la criatura de la otra realidad,
            //     flotando EN el vacío — meciéndose lento). v6.35b: MÁS
            //     GRANDE Y MÁS BRILLANTE (la lección del mock: pequeño y
            //     tenue se perdía contra el vacío). ===
            float mecer = MathF.Sin(time * 0.8f + seed) * 0.12f;
            Vector2 corazon = center - new Vector2(0f, alto * 0.52f) +
                new Vector2(MathF.Sin(time * 0.5f) * len * 0.05f, 0f);
            float dirX = MathF.Cos(mecer);
            float dirY = MathF.Sin(mecer);
            Vector2 dirEsp = new(dirX, dirY);
            Vector2 perpEsp = new(-dirY, dirX);
            const int Vert = 9;
            for (int v = 0; v < Vert; v++)
            {
                float f = v / (float)(Vert - 1);                  // 0=cabeza → 1=cola
                // La columna en S suave (la criatura suspendida).
                float curva = MathF.Sin(f * 2.6f + time * 0.9f) * alto * 0.20f;
                Vector2 p = corazon - dirEsp * (f * len * 0.185f) + perpEsp * curva;
                float w = MathHelper.Lerp(5.6f, 2.6f, f);
                float vv = 0.72f + 0.18f * MathF.Sin(time * 1.6f - v * 0.7f);
                // LA VÉRTEBRA (nudo óseo).
                Quad(batch, GlowTex, p, new Vector2(w * 3.0f, w * 2.4f), mecer,
                    Tint(hueso, vv * alpha * open));
                // LA ESPINA dorsal (el tracito que une las vértebras).
                if (v > 0)
                {
                    float pv = 0.52f + 0.14f * MathF.Sin(time * 1.6f - (v - 1) * 0.7f);
                    Vector2 prevP = corazon - dirEsp * ((v - 1) / (float)(Vert - 1) * len * 0.185f) +
                        perpEsp * MathF.Sin((v - 1) / (float)(Vert - 1) * 2.6f + time * 0.9f) * alto * 0.20f;
                    Vector2 mid = (prevP + p) * 0.5f;
                    float l = Vector2.Distance(prevP, p);
                    Quad(batch, GlowTex, mid, new Vector2(l + w, w * 1.0f),
                        (float)Math.Atan2(p.Y - prevP.Y, p.X - prevP.X),
                        Tint(hueso, pv * alpha * open));
                }
            }
            // LA CABEZA con la mandíbula abierta (dos trazos en V).
            Vector2 cab = corazon;
            Vector2 mand1 = cab + dirEsp * (13f) + perpEsp * (8f);
            Vector2 mand2 = cab + dirEsp * (13f) - perpEsp * (8f);
            foreach (Vector2 m in new[] { mand1, mand2 })
            {
                Vector2 mid = (cab + m) * 0.5f;
                float l = Vector2.Distance(cab, m);
                Quad(batch, GlowTex, mid, new Vector2(l + 3f, 3.4f),
                    (float)Math.Atan2(m.Y - cab.Y, m.X - cab.X),
                    Tint(hueso, 0.78f * alpha * open));
            }
            // LA CUENCA (el punto oscuro del ojo — la única sombra viva).
            Quad(batch, OrbTex, cab + perpEsp * 3.4f, new Vector2(3.4f, 3.4f), 0f,
                Tint(blanco, 0.45f * alpha * open));

            // === 4. LAS PARTÍCULAS DE DATOS (cian parpadeando cerca de la
            //     línea — los píxeles sueltos de la realidad rota). ===
            const int Datos = 14;
            for (int d = 0; d < Datos; d++)
            {
                float h1 = H01(seed, d, 233);
                float h2 = H01(seed, d, 239);
                float h3 = H01(seed, d, 241);
                float x = center.X + (h1 - 0.5f) * len * 1.02f;
                // Flotan cerca de la línea, ARRIBA y abajo (datos escapando).
                float y = center.Y + (h2 - 0.5f) * alto * 1.6f * (h3 > 0.5f ? 1f : -0.5f);
                float st = MathF.Sin(tick * (0.19f + 0.24f * h3) + h1 * 6.28f) * 0.5f + 0.5f;
                if (st < 0.42f) continue;               // el parpadeo del dato
                float s = 1.6f + 2.6f * h2;
                Quad(batch, OrbTex, new Vector2(x, y), new Vector2(s, s), 0f,
                    Tint(h3 > 0.5f ? cian : blanco, 0.55f * st * alpha * open));
            }
            Main.spriteBatch.End();
        }

        // ------------------------------------------------------------------
        //  4 — EL LEVIATÁN ESPECTRAL (la referencia: la criatura marina
        //  ósea — vértebras en S, aletas afiladas, cabeza con mandíbula)
        // ------------------------------------------------------------------

        /// <summary>
        /// EL LEVIATÁN ESPECTRAL — la criatura marina de HUESO ETÉREO: la
        /// COLUMNA de catorce vértebras nadando en S (la onda viaja del
        /// cuerpo a la cola), las ALETAS curvas afiladas proyectándose de
        /// la columna en ángulo (alternando lados, desvaneciéndose en las
        /// puntas), la CABEZA con la mandíbula abierta y la cresta, y el
        /// rastro frío detrás. Líneas blancas frías #E8F4FF con bordes
        /// #9FD8FF — la criatura se dibuja desde la posición viva del
        /// proyectil (la MISMA fórmula de onda del AI: la coherencia
        /// cuerpo-nado).
        /// </summary>
        /// <param name="cabeza">Posición de la cabeza (pantalla).</param>
        /// <param name="dir">Dirección de nado (normalizada).</param>
        /// <param name="edad01">0→1 de la vida (nace y se desvanece).</param>
        /// <param name="time">Tiempo animado (la fase de la onda).</param>
        /// <param name="seed">Semilla determinista.</param>
        /// <param name="alpha">Multiplicador de intensidad.</param>
        public static void LeviatanEspectral(Vector2 cabeza, Vector2 dir, float edad01,
            float time, int seed, float alpha = 1f)
        {
            if (alpha <= 0.02f) return;
            dir = Vector2.Normalize(dir);
            if (float.IsNaN(dir.X) || float.IsNaN(dir.Y)) dir = Vector2.UnitX;

            // La ventana de vida: nace rápido (12%), muere lento (18%).
            float ventana = MathHelper.Clamp(edad01 / 0.12f, 0f, 1f) *
                            MathHelper.Clamp((1f - edad01) / 0.18f, 0f, 1f);
            if (ventana <= 0.02f) return;

            Color blancoFrio = new(232, 244, 255);      // #E8F4FF
            Color borde = new(159, 216, 255);            // #9FD8FF

            Vector2 perp = new(-dir.Y, dir.X);

            PortalAdditive();
            var batch = Main.spriteBatch;

            // === 1. LA COLUMNA VERTEBRAL (14 vértebras, la onda en S) ===
            const int Vert = 14;
            const float Sep = 13f;
            const float Frec = 2.5f;                     // ciclos/s del nado
            Vector2[] posiciones = new Vector2[Vert];
            for (int v = 0; v < Vert; v++)
            {
                float f = v / (float)(Vert - 1);                  // 0=cabeza → 1=cola
                // LA ONDA: viaja del cuerpo a la cola (los peces empujan
                // con la cola) y CRECE hacia atrás (la C de la referencia).
                float amp = 10f + 16f * f;
                float fase = time * Frec * MathHelper.TwoPi - v * 0.55f;
                posiciones[v] = cabeza - dir * (Sep * v) + perp * (MathF.Sin(fase) * amp);
            }

            // El rastro frío detrás de la cola (el fantasma del paso).
            Vector2 cola = posiciones[Vert - 1];
            Quad(batch, GlowTex, cola - dir * 26f, new Vector2(58f, 26f),
                (float)Math.Atan2(dir.Y, dir.X), Tint(borde, 0.10f * alpha * ventana));

            for (int v = 0; v < Vert; v++)
            {
                float f = v / (float)(Vert - 1);
                Vector2 p = posiciones[v];
                // La vértebra decrece hacia la cola (cabeza ×1.0 → cola ×0.35).
                float w = MathHelper.Lerp(7.5f, 3.2f, f);
                // La tangente local (la vértebra se orienta al nado).
                Vector2 tang = v == 0 ? dir : p - posiciones[v - 1];
                float rot = (float)Math.Atan2(tang.Y, tang.X);
                // El pulso espectral recorriendo el cuerpo.
                float pulse = 0.72f + 0.28f * MathF.Sin(time * 3.4f - v * 0.8f);

                // LA VÉRTEBRA (nudo óseo blanco frío + halo de borde).
                Quad(batch, GlowTex, p, new Vector2(w * 2.4f, w * 1.8f), rot,
                    Tint(borde, 0.30f * pulse * alpha * ventana));
                Quad(batch, OrbTex, p, new Vector2(w * 1.15f, w * 1.15f), 0f,
                    Tint(blancoFrio, 0.85f * pulse * alpha * ventana));

                // === 2. LAS ALETAS (los radios curvos de la referencia) ===
                if (v is 2 or 4 or 6 or 8 or 10 or 12)
                {
                    int lado = (v / 2) % 2 == 0 ? 1 : -1;         // alternan
                    float fAleta = 1f - f * 0.55f;                 // grandes delante
                    float largo = (26f + 14f * H01(seed, v, 251)) * fAleta;
                    // La aleta se abre hacia ATRÁS-FUERA (los radios del pez).
                    float angFin = rot + MathHelper.Pi - lado * (0.85f + 0.2f * H01(seed, v, 257));
                    Vector2 fin = p + new Vector2(MathF.Cos(angFin), MathF.Sin(angFin)) * largo;
                    Vector2 mid = (p + fin) * 0.5f;
                    // El TRAZO afilado (grueso en la base, la punta se funde).
                    Quad(batch, GlowTex, mid, new Vector2(largo + 3f, 3.2f * fAleta),
                        (float)Math.Atan2(fin.Y - p.Y, fin.X - p.X),
                        Tint(borde, (0.38f - 0.16f * f) * alpha * ventana));
                }
            }

            // === 3. LA CABEZA (el cráneo con la mandíbula abierta) ===
            Vector2 cab = posiciones[0];
            // El hocico (alargado hacia delante).
            Vector2 hocico = cab + dir * 12f;
            Quad(batch, GlowTex, (cab + hocico) * 0.5f, new Vector2(16f, 7f),
                (float)Math.Atan2(dir.Y, dir.X), Tint(blancoFrio, 0.80f * alpha * ventana));
            // Las mandíbulas en V abierta.
            float abre = 0.42f + 0.10f * MathF.Sin(time * 1.7f);   // respira
            Vector2 mand1 = hocico + perp * 10f * abre + dir * 7f;
            Vector2 mand2 = hocico - perp * 10f * abre + dir * 7f;
            foreach (Vector2 m in new[] { mand1, mand2 })
            {
                Vector2 mid = (hocico + m) * 0.5f;
                float l = Vector2.Distance(hocico, m);
                Quad(batch, GlowTex, mid, new Vector2(l + 2.5f, 3.0f),
                    (float)Math.Atan2(m.Y - hocico.Y, m.X - hocico.X),
                    Tint(blancoFrio, 0.70f * alpha * ventana));
            }
            // LA CUENCA del ojo (la chispa que lo hace VIVO).
            Quad(batch, OrbTex, cab + dir * 4.5f + perp * 3.5f, new Vector2(3.0f, 3.0f), 0f,
                Tint(Color.White, 0.90f * alpha * ventana));
            // LA CRESTA (la corona del dragón — la aleta mayor).
            float angCresta = (float)Math.Atan2(dir.Y, dir.X) - MathHelper.PiOver4;
            Vector2 cresta = cab + new Vector2(MathF.Cos(angCresta), MathF.Sin(angCresta)) * 20f;
            Quad(batch, GlowTex, (cab + cresta) * 0.5f, new Vector2(24f, 4.5f),
                angCresta, Tint(borde, 0.42f * alpha * ventana));

            Main.spriteBatch.End();
        }
    }
}
