using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// RiftLib (parcial) — v6.33 — LA FAMILIA GLITCH.
    ///
    /// Uno de los TRES archivos parciales de la librería de los desgarros
    /// (organización v6.34: el núcleo del desgarro clásico, las paletas y
    /// las primitivas compartidas viven en el archivo maestro). Aquí viven
    /// los dos miembros de la familia glitch:
    ///   · EcoGlitch — los offsets de canal (lección élite, SIN render
    ///     targets) que el llamador aplica re-dibujando su desgarro/grieta.
    ///   · DesgarroGlitch — "Desgarro de realidad cuántica" (v6.33): el
    ///     círculo irregular fragmentado con borde de bloques glitch, núcleo
    ///     magenta nebuloso, rayos que irradian y partículas de datos.
    ///
    /// CONTRATO: DesgarroGlitch recibe el sprite batch CERRADO y lo deja
    /// CERRADO (contrato v6.10) — gestiona sus lotes alfa y aditivo
    /// internamente (PortalAlpha/PortalAdditive del núcleo compartido).
    /// Coordenadas de PANTALLA (resta screenPosition antes de llamar).
    /// El split v6.34 es PURAMENTE mecánico: mismos métodos, mismos
    /// cuerpos, mismo namespace — los call-sites no cambian.
    /// </summary>
    public static partial class RiftLib
    {
        // LOS BUFFERS DEL ECO (cero GC — reutilizados por tick; solo los
        // toca EcoGlitch, por eso viven aquí, con la familia glitch).
        private static readonly Vector2[] _ecoOff = new Vector2[3];
        private static readonly Color[] _ecoTint = new Color[3];

        /// <summary>
        /// EL ECO GLITCH (lección élite glur, SIN render targets): 3 offsets
        /// horizontales alternos ±(2..3) px con tintes de canal y alpha
        /// decreciente. El llamador re-dibuja su desgarro/grieta con estos
        /// offsets. Los arrays son buffers estáticos (cero GC).
        /// (v6.28: el desgarro del arma YA NO lo usa — el usuario lo leyó como
        /// "interrupciones"; queda disponible para otros llamadores.)
        /// </summary>
        public static void EcoGlitch(int seed, float time, out Vector2[] offsets,
            out Color[] tintes)
        {
            int tick = (int)(time * 60f);
            for (int i = 0; i < 3; i++)
            {
                // Offset horizontal alterno ±(2..3) px, por eco y por tick.
                float mag = 2f + H01(seed, tick, 901 + i) * 1f;
                float sign = (i % 2 == 0 ? -1f : 1f) * (H01(seed, tick, 911 + i) > 0.5f ? 1f : -1f);
                _ecoOff[i] = new Vector2(mag * sign, 0f);

                // Tinte de canal (R para un lado, B para el otro) con alpha decreciente.
                float a = 0.30f * (1f - i / 3f);
                _ecoTint[i] = i % 2 == 0
                    ? new Color(255, 0, 0, (byte)(int)(255f * a))
                    : new Color(0, 90, 255, (byte)(int)(255f * a));
            }
            offsets = _ecoOff;
            tintes = _ecoTint;
        }

        // ------------------------------------------------------------------
        //  EL DESGARRO CUÁNTICO (la referencia: círculo fragmentado con
        //  borde GLITCH de bloques, núcleo magenta nebuloso, rayos que
        //  irradian, flicker rápido, partículas de datos)
        // ------------------------------------------------------------------

        /// <summary>
        /// EL DESGARRO GLITCH — "Desgarro de realidad cuántica": el círculo
        /// IRREGULAR fragmentado (24 vértices con radios hash — la silueta
        /// rasgada) con el borde de BLOQUES GLITCH (astillas rectangulares
        /// cian/violeta proyectadas hacia afuera, cada una con su flicker
        /// rápido 0.1-0.3 s), el NÚCLEO MAGENTA nebuloso con vetas fucsia,
        /// RAYOS que irradian desde el borde (StormLib) y partículas de
        /// datos (cuadraditos que se desprenden rotando). `progress` 0→1.
        /// </summary>
        public static void DesgarroGlitch(Vector2 center, float radius, float progress,
            float time, int seed, float alpha = 1f)
        {
            if (radius < 4f || alpha <= 0.02f) return;
            progress = MathHelper.Clamp(progress, 0f, 1f);
            try
            {
                Color cian = new(0, 229, 255);        // #00E5FF
                Color violeta = new(124, 77, 255);    // #7C4DFF
                Color magenta = new(213, 0, 249);     // #D500F9
                Color fucsia = new(255, 64, 129);     // #FF4081
                Color blanco = new(255, 255, 255);

                float open = (float)Math.Pow(progress, 0.55f);
                float R = radius * open;
                const int Verts = 24;
                float tick = time * 60f;

                // === PASO 1 — EL NÚCLEO (pase alfa: la nebulosa SÓLIDA) ===
                PortalAlpha();
                var batch = Main.spriteBatch;
                // La silueta irregular: el polígono rasgado (8 rebanadas de pastel).
                for (int s = 0; s < 8; s++)
                {
                    float a0 = s * MathHelper.PiOver4 + 0.09f * MathF.Sin(time * 1.1f + s);
                    float a1 = (s + 1) * MathHelper.PiOver4 - 0.09f * MathF.Sin(time * 0.9f + s * 2f);
                    float r0 = R * (0.62f + 0.30f * H01(seed, s, 107));
                    float r1 = R * (0.62f + 0.30f * H01(seed, s + 1, 109));
                    // El quad de la rebanada (del centro al arco).
                    Vector2 p0 = center + new Vector2(MathF.Cos(a0), MathF.Sin(a0)) * r0;
                    Vector2 p1 = center + new Vector2(MathF.Cos(a1), MathF.Sin(a1)) * r1;
                    Vector2 pm = center + new Vector2(MathF.Cos((a0 + a1) * 0.5f), MathF.Sin((a0 + a1) * 0.5f)) * Math.Max(r0, r1);
                    float wSeg = Vector2.Distance(p0, p1) + 6f;
                    float hSeg = Math.Max(r0, r1);
                    Quad(batch, GlowTex, (p0 + p1) * 0.5f + (pm - center) * 0.25f,
                        new Vector2(wSeg, hSeg), (a0 + a1) * 0.5f + MathHelper.PiOver2,
                        new Color(magenta.R, magenta.G, magenta.B, (byte)(int)(120 * alpha * open)));
                }
                Main.spriteBatch.End();

                // === PASO 2 — EL BORDE GLITCH + LA LUZ (aditivo) ===
                PortalAdditive();
                batch = Main.spriteBatch;
                for (int v = 0; v < Verts; v++)
                {
                    float h1 = H01(seed, v, 113);
                    float h2 = H01(seed, v, 127);
                    float h3 = H01(seed, v, 131);
                    float ang = v * MathHelper.TwoPi / Verts;
                    float rv = R * (0.70f + 0.34f * h1);             // el borde dentado

                    // El FLICKER rápido (0.1-0.3 s por astilla — inestable).
                    float flickHz = 6f + 14f * h2;
                    float lit = MathF.Sin(tick * flickHz * 0.1047f + h3 * 6.28f) * 0.5f + 0.5f;
                    if (lit < 0.25f) continue;

                    Vector2 dir = new(MathF.Cos(ang), MathF.Sin(ang));
                    Vector2 bord = center + dir * rv;

                    // LA ASTILLA GLITCH: un bloque rectangular proyectado hacia
                    // afuera (los artefactos de compresión de la realidad rota).
                    float bw = 3f + 11f * h2;
                    float bh = 2f + 5f * h3;
                    float bo = rv + (6f + 16f * h3) * lit;
                    Color astC = h1 > 0.5f ? cian : violeta;
                    Quad(batch, GlowTex, center + dir * bo,
                        new Vector2(bw, bh), ang,
                        Tint(astC, (0.45f + 0.55f * lit) * alpha * open));

                    // LA LÍNEA DEL BORDE (el contorno irregular que sangra luz).
                    Quad(batch, GlowTex, bord, new Vector2(R * 0.30f, 2.2f + 2.5f * lit),
                        ang + MathHelper.PiOver2, Tint(astC, 0.60f * alpha * open * lit));

                    // LA VETA FUCSIA (el relámpago interno de la nebulosa).
                    if (v % 3 == 0)
                        Quad(batch, GlowTex, center + dir * (rv * 0.55f),
                            new Vector2(rv * 0.62f, 1.8f), ang,
                            Tint(fucsia, 0.42f * alpha * open * lit));

                    // EL RAYO QUE IRRADIA (el código escapando — StormLib).
                    if (v % 4 == 0 && lit > 0.7f)
                    {
                        Vector2 ext = center + dir * (rv + 26f + 34f * h2);
                        StormLib.Bolt(batch, bord, ext, seed + v * 7, (int)(tick * 0.35f),
                            4.5f, astC, blanco, 0.55f * alpha * open * lit, 4, 9f);
                    }
                }

                // EL NÚCLEO CEGADOR (el corazón de la corrupción) — v6.33 b:
                // el corazón MAGENTA pesa más (la lección del mock: el blanco
                // puro se comía la lectura cuántica).
                float heart = 0.5f + 0.5f * MathF.Sin(time * 9f + seed);
                Quad(batch, GlowTex, center, new Vector2(R * 0.95f, R * 0.95f), 0f,
                    Tint(magenta, 0.34f * alpha * open));
                Quad(batch, GlowTex, center, new Vector2(R * 0.52f, R * 0.52f), 0f,
                    Tint(violeta, 0.42f * alpha * open));
                Quad(batch, GlowTex, center, new Vector2(R * 0.30f, R * 0.30f) * (1f + 0.12f * heart),
                    0f, Tint(blanco, (0.50f + 0.30f * heart) * alpha * open));

                // LAS PARTÍCULAS DE DATOS (cuadraditos que se desprenden).
                for (int d = 0; d < 9; d++)
                {
                    float h1 = H01(seed, d, 137);
                    float h2 = H01(seed, d, 139);
                    float ang = h1 * MathHelper.TwoPi + time * (0.4f + 0.3f * h2);
                    float t = ((time * (0.22f + 0.2f * h2) + h2) % 1f);
                    float rr = R * (0.75f + 0.7f * t);
                    Vector2 pp = center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * rr;
                    float s = 2f + 4f * h2;
                    Quad(batch, GlowTex, pp, new Vector2(s, s * (0.4f + 0.6f * h1)),
                        ang + time * 1.7f * (h1 - 0.5f) * 2f,
                        Tint(h1 > 0.5f ? cian : fucsia, 0.55f * alpha * open * (1f - t)));
                }
                Main.spriteBatch.End();
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }
        }
    }
}
