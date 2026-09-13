using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// WingStrokes — v6.13 — EL VOCABULARIO DE LA TÉCNICA DE LAS CORONAS,
    /// aplicado a alas.
    ///
    /// ¿Qué hace que las coronas (RuneCrown / ArcCrown) se vean BIEN? No es
    /// la matemática polar — son CUATRO primitivas con identidad propia:
    ///
    ///   1. EL TRAZO: cápsula de luz ESTIRADA a lo largo de una línea
    ///      (len ≫ wid) con GRADIENTE base→punta — se lee como un gesto
    ///      de pincel, no como una mancha.
    ///   2. LA PERLA: punto de luz con núcleo casi blanco + halo tenue —
    ///      la "gema" que ancla la mirada.
    ///   3. EL DESTELLO DE 4 PUNTAS: dos glows estirados en cruz — el
    ///      acento especular de los nudos.
    ///   4. EL VOLUMEN OSCURO: bajo la luz, una cápsula oscura translúcida
    ///      que da SILUETA legible contra cualquier fondo.
    ///
    /// Las alas v6.12 usaban blobs radiales apilados sobre curvas polares —
    /// de ahí el look de "manchas difusas". Estos helpers obligan al diseño
    /// con TRAZOS: cada pluma, vena, filamento o borde es un gesto nítido.
    /// </summary>
    public static class WingStrokes
    {
        /// <summary>
        /// EL TRAZO de la corona: cápsula de luz entre dos puntos con
        /// gradiente de color a→b y el grosor dado (px finales).
        /// </summary>
        public static void Stroke(Vector2 a, Vector2 b, float width, Color ca, Color cb, float alpha)
        {
            Vector2 mid = (a + b) * 0.5f;
            Vector2 d = b - a;
            float len = d.Length();
            if (len < 0.01f || alpha <= 0.01f) return;
            float rot = (float)Math.Atan2(d.Y, d.X);
            Color c = Color.Lerp(ca, cb, 0.5f) * alpha;
            VFXCore.Quad(mid, c, new Vector2(len + width, width), rot);
        }

        /// <summary>
        /// Cadena de trazos a lo largo de una polilínea (contornos, venas
        /// largas): el color se interpola del primero al último punto.
        /// </summary>
        public static void Chain(Vector2[] pts, float width, Color ca, Color cb, float alpha)
        {
            int n = pts.Length;
            if (n < 2 || alpha <= 0.01f) return;
            for (int i = 0; i < n - 1; i++)
            {
                float t0 = i / (float)(n - 1);
                float t1 = (i + 1) / (float)(n - 1);
                Stroke(pts[i], pts[i + 1], width,
                    Color.Lerp(ca, cb, t0), Color.Lerp(ca, cb, t1), alpha);
            }
        }

        /// <summary>
        /// LA PERLA de la corona: halo tenue + núcleo casi blanco. <paramref name="size"/>
        /// es el diámetro del HALO; el núcleo es un 45% de ese.
        /// </summary>
        public static void Pearl(Vector2 pos, float size, Color halo, Color core, float alpha)
        {
            if (alpha <= 0.01f) return;
            VFXCore.Quad(pos, halo * (alpha * 0.85f), new Vector2(size, size));
            VFXCore.Quad(pos, core * alpha, new Vector2(size * 0.45f, size * 0.45f));
        }

        /// <summary>
        /// EL DESTELLO DE 4 PUNTAS de la corona: dos glows estirados en cruz
        /// (el acento especular de los nudos de la corona de arcos).
        /// </summary>
        public static void Flare4(Vector2 pos, float len, float wide, Color c, float alpha)
        {
            if (alpha <= 0.01f) return;
            VFXCore.Quad(pos, c * (alpha * 0.9f), new Vector2(len, wide));
            VFXCore.Quad(pos, c * (alpha * 0.9f), new Vector2(wide, len));
            // chispa central
            VFXCore.Quad(pos, c * alpha, new Vector2(wide * 1.5f, wide * 1.5f));
        }

        /// <summary>
        /// EL VOLUMEN OSCURO: cápsula oscura translúcida bajo la luz — da
        /// silueta al ala contra cielos claros (lección v6.12).
        /// </summary>
        public static void Volume(Vector2 a, Vector2 b, float width, Color dark, float alpha)
        {
            Vector2 mid = (a + b) * 0.5f;
            Vector2 d = b - a;
            float len = d.Length();
            if (len < 0.01f || alpha <= 0.01f) return;
            float rot = (float)Math.Atan2(d.Y, d.X);
            VFXCore.Quad(mid, dark * alpha, new Vector2(len + width, width), rot);
        }

        /// <summary>
        /// LA PLUMA completa (la primitiva reina de las alas nuevas):
        /// una pluma curvada = volumen oscuro + trazo brillante con
        /// gradiente raíz→punta + nervio pálido + PERLA en la punta.
        /// La curva es una Bézier cuadrática root→tip con control en
        /// <paramref name="bend"/> (la curvatura "aerodinámica").
        /// </summary>
        /// <param name="root">Anclaje de la pluma (mundo).</param>
        /// <param name="tip">Punta de la pluma (mundo).</param>
        /// <param name="bend">Punto de control de la curva (mundo).</param>
        /// <param name="wRoot">Grosor en la raíz (px).</param>
        /// <param name="wTip">Grosor en la punta (px).</param>
        /// <param name="cRoot">Color del trazo en la raíz.</param>
        /// <param name="cMid">Color del trazo en medio.</param>
        /// <param name="cTip">Color del trazo en la punta.</param>
        /// <param name="dark">Color del volumen oscuro bajo la luz.</param>
        /// <param name="alpha">Multiplicador global.</param>
        /// <param name="pearl">¿Perla en la punta?</param>
        public static void Feather(Vector2 root, Vector2 tip, Vector2 bend,
            float wRoot, float wTip, Color cRoot, Color cMid, Color cTip, Color dark,
            float alpha, bool pearl = true, float volAlpha = 0.55f, float pearlScale = 2.6f)
        {
            if (alpha <= 0.01f) return;

            // Curva Bézier cuadrática muestreada en 7 tramos.
            Vector2 prev = root;
            for (int i = 1; i <= 7; i++)
            {
                float u = i / 7f;
                float mu = 1f - u;
                Vector2 p = mu * mu * root + 2f * mu * u * bend + u * u * tip;

                float w = MathHelper.Lerp(wRoot, wTip, u) * 0.5f;
                // 1) volumen oscuro (más ancho, da la silueta)
                Volume(prev, p, w * 2.6f, dark, volAlpha * alpha);
                // 2) trazo brillante con gradiente raíz→medio→punta
                Color ca = u < 0.5f
                    ? Color.Lerp(cRoot, cMid, u * 2f)
                    : Color.Lerp(cMid, cTip, (u - 0.5f) * 2f);
                Color cprev = (u - 1f / 7f) < 0.5f
                    ? Color.Lerp(cRoot, cMid, (u - 1f / 7f) * 2f)
                    : Color.Lerp(cMid, cTip, (u - 1f / 7f - 0.5f) * 2f);
                Stroke(prev, p, w * 1.6f, cprev, ca, 0.92f * alpha);
                // 3) nervio pálido central (la "quilla" de la pluma)
                Stroke(prev, p, w * 0.55f, Color.Lerp(ca, Color.White, 0.45f),
                    Color.Lerp(cTip, Color.White, 0.55f), 0.75f * alpha);

                prev = p;
            }

            // 4) la PERLA de la punta — el acento de la corona
            if (pearl)
                Pearl(tip, wTip * pearlScale, Color.Lerp(cTip, Color.White, 0.4f),
                    new Color(255, 252, 250), 0.95f * alpha);
        }

        /// <summary>Un punto de una Bézier cuadrática.</summary>
        public static Vector2 Bezier(Vector2 a, Vector2 c, Vector2 b, float u)
        {
            float mu = 1f - u;
            return mu * mu * a + 2f * mu * u * c + u * u * b;
        }
    }
}
