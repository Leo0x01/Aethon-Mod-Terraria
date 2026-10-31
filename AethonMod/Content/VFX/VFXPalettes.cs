using Microsoft.Xna.Framework;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// VFXPalettes — v6.03 — LAS PALETAS DE LA BIBLIOTECA VISUAL AETHON.
    ///
    /// Paletas nombradas (colores planos + gradientes paramétricos) para que
    /// TODOS los efectos del mod compartan un mismo lenguaje de color en
    /// vez de esparcir números mágicos por cada arma.
    ///
    /// La paleta estrella es <see cref="VoidQueen"/>: la del agujero negro
    /// de la referencia del usuario — núcleo negro absoluto, disco de
    /// acreción fucsia neón con borde interior granate, destellos rosa
    /// pálido, rayos lila y halo púrpura profundo.
    /// </summary>
    public static class VFXPalettes
    {
        /// <summary>
        /// LA CORTE CARMESÍ — la paleta de la corona de arcos de neón (la
        /// corona original del agujero negro, hoy cosmético del jugador):
        /// lazos carmesí→magenta con nudos naranja incandescentes.
        /// </summary>
        public static class CrimsonCourt
        {
            /// <summary>Base de los lazos: carmesí vivo.</summary>
            public static readonly Color LoopBase = new Color(255, 23, 56);

            /// <summary>Ápice de los lazos: magenta eléctrico.</summary>
            public static readonly Color LoopApex = new Color(254, 25, 242);

            /// <summary>Eco interior: carmín.</summary>
            public static readonly Color EchoBase = new Color(255, 40, 80);

            /// <summary>Eco interior, ápice: magenta suave.</summary>
            public static readonly Color EchoApex = new Color(255, 60, 190);

            /// <summary>Nudos de los ápices: naranja incandescente.</summary>
            public static readonly Color Knot = new Color(255, 138, 60);

            /// <summary>Destello del nudo: naranja cálido claro.</summary>
            public static readonly Color KnotFlare = new Color(255, 170, 90);

            /// <summary>Chispa blanca central del nudo.</summary>
            public static readonly Color KnotSpark = new Color(255, 240, 230);

            /// <summary>
            /// Color de un lazo según su "altura" edge (0 bases → 1 ápice).
            /// </summary>
            public static Color Loop(float edge)
            {
                return Color.Lerp(LoopBase, LoopApex, edge);
            }
        }

        /// <summary>
        /// LAS RUNAS ESTELARES — la paleta de la corona rúnica (diseño nuevo
        /// desde cero sobre la referencia): glifos fucsia con puntas de perla
        /// rosa pálida y chispas ascendentes.
        /// </summary>
        public static class RuneStars
        {
            /// <summary>Base de los glifos: fucsia de la referencia.</summary>
            public static readonly Color GlyphBase = new Color(255, 0, 85);     // #FF0055

            /// <summary>Cuerpo del glifo: rosa caliente.</summary>
            public static readonly Color GlyphBody = new Color(255, 51, 119);   // #FF3377

            /// <summary>Perla de la punta: rosa pálido.</summary>
            public static readonly Color Pearl = new Color(255, 153, 187);      // #FF99BB

            /// <summary>Núcleo de la perla: blanco rosado.</summary>
            public static readonly Color PearlCore = new Color(255, 240, 245);  // #FFF0F5

            /// <summary>Halo del arco completo: fucsia tenue.</summary>
            public static readonly Color ArcHalo = new Color(255, 20, 147);     // #FF1493

            /// <summary>Gradiente base→punta de un glifo.</summary>
            public static Color Glyph(float t)
            {
                return Color.Lerp(GlyphBase, Pearl, t);
            }
        }
    }
}
