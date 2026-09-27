using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// BoltRenderer — v6.50.25 — EL RELÁMPAGO DETERMINISTA DE PRIMERA
    /// GENERACIÓN, MONTADO SOBRE LA TIRA DE PRIMITIVAS DE RAYOSTRIP.
    ///
    /// Rayos en zigzag nacidos del mismo principio que el látigo eléctrico
    /// de la medusa nebulosa: el zigzag se deriva de (semilla, flick,
    /// segmento) con hash puro → TODAS las máquinas ven el MISMO rayo sin
    /// sincronizar nada, y el rayo se REGENERA cada pocos ticks (flick) —
    /// está VIVO, no es una textura estática.
    ///
    /// v6.50.25 — LA LÍNEA LISA (el reporte del usuario: «el rayo no es
    /// una línea lisa… lleno de pequeños bultos como puntos difuminados»):
    /// el pincel pasa a ser LA TIRA DE VÉRTICES de RayoStrip — geometría
    /// CONTINUA (trapecios que comparten vértices: cero juntas, cero
    /// solapes — el SpriteBatch de rectángulos no podía dibujar una esquina
    /// sin hueco o sin solape, y el solape aditivo era EL BULTO de cada
    /// vértice) con el perfil transversal en el COLOR DE LOS VÉRTICES
    /// (degradado interpolado, no escalones de pasadas apiladas).
    ///
    /// Uso: recibe coordenadas de MUNDO y se dibuja directo (el baile del
    /// lote de primitivas lo gestiona RayoStrip.Filamento).
    /// </summary>
    public static class BoltRenderer
    {
        /// <summary>Segmentos del rayo principal.</summary>
        private const int Segments = 6;

        /// <summary>
        /// Calcula un rayo de <paramref name="start"/> a <paramref name="end"/>
        /// (coords de MUNDO) y lo dibuja como TIRA de primitivas.
        /// </summary>
        /// <param name="start">Origen del rayo (mundo).</param>
        /// <param name="end">Destino del rayo (mundo).</param>
        /// <param name="seed">Semilla determinista (misma = mismo rayo).</param>
        /// <param name="flick">El "parpadeo" actual: cámbialo cada ~4-9 ticks
        /// para que el zigzag se regenere y el rayo VIVA.</param>
        /// <param name="width">Grosor base del halo en píxeles.</param>
        /// <param name="haloColor">Color de la funda exterior.</param>
        /// <param name="coreColor">Color del núcleo caliente.</param>
        /// <param name="alpha">Multiplicador global (0..1).</param>
        /// <param name="ampFactor">Amplitud del zigzag (1 = estándar).</param>
        public static void ComputeQuads(Vector2 start, Vector2 end, int seed, int flick,
            float width, Color haloColor, Color coreColor, float alpha = 1f, float ampFactor = 1f)
        {
            Vector2 delta = end - start;
            float length = delta.Length();
            if (length < 4f || alpha <= 0.01f || width <= 0.05f) return;

            Vector2 dir = delta / length;
            Vector2 normal = new Vector2(-dir.Y, dir.X);
            float amp = Math.Min(length * 0.16f, 14f) * ampFactor;

            // Puntos del zigzag (coords de PANTALLA para la tira).
            Vector2 screen = Main.screenPosition;
            Vector2[] pts = new Vector2[Segments + 1];
            for (int s = 0; s <= Segments; s++)
            {
                float t = s / (float)Segments;
                // El zigzag se AMPLÍA en el medio (un rayo real tiene la
                // tensión en el centro, quieto en los anclajes).
                float envelope = (float)Math.Sin(t * Math.PI);
                float jitter = (VFXCore.Hash01(seed, flick, s) - 0.5f) * 2f * amp * envelope;
                pts[s] = start + dir * (length * t) + normal * jitter - screen;
            }
            pts[0] = start - screen;
            pts[Segments] = end - screen;

            // LA TIRA DEL TRONCO (la línea LISA — un solo volcado).
            // v6.50.26 — KINKS AGUDOS: sin Chaikin (stepped leaders).
            RayoStrip.Filamento(pts, width, haloColor, coreColor, alpha,
                StormTaper.Center, seed, flick, suavizar: false, crackle: true);

            // RAMAS laterales cortas donde el hash lo pide.
            for (int s = 1; s < Segments - 1; s++)
            {
                if (VFXCore.Hash01(seed, flick, s + 91) <= 0.62f) continue;
                float side = VFXCore.Hash01(seed, flick, s + 37) > 0.5f ? 1f : -1f;
                float branchLen = (0.35f + 0.4f * VFXCore.Hash01(seed, flick, s + 53)) * length * 0.25f;
                Vector2 branchDir = (dir * 0.45f + normal * side).SafeNormalize(Vector2.UnitY);
                Vector2 bEnd = pts[s + 1] + branchDir * branchLen;

                // v6.50.25 — la rama también con LA TIRA (×0.6 brillo, sin
                // gorro: las puntas de rama mueren finas — los gorros eran
                // los «puntos difuminados alrededor de la línea»).
                var rama = new Vector2[] { pts[s + 1], bEnd };
                RayoStrip.Filamento(rama, width * 0.6f, haloColor, coreColor,
                    0.6f * alpha, StormTaper.Linear, seed + s * 7, flick,
                    suavizar: false, crackle: true, vena: false);
            }

            // Extremos brillantes (descarga en el origen, frente de impacto)
            // — puntos radiales SOFT GLOW con el lote del juego reabierto
            // (los gorros son PUNTOS, no tiras).
            try
            {
                if (VFXCore.SoftGlow != null)
                {
                    Main.spriteBatch.Draw(VFXCore.SoftGlow, pts[0], null,
                        StormLib.Tint(coreColor, 0.9f * alpha), 0f,
                        VFXCore.SoftGlow.Size() * 0.5f,
                        new Vector2(width * 2.4f, width * 2.4f) / VFXCore.SoftGlow.Size(),
                        SpriteEffects.None, 0f);
                    Main.spriteBatch.Draw(VFXCore.SoftGlow, pts[Segments], null,
                        StormLib.Tint(coreColor, 0.9f * alpha), 0f,
                        VFXCore.SoftGlow.Size() * 0.5f,
                        new Vector2(width * 2.0f, width * 2.0f) / VFXCore.SoftGlow.Size(),
                        SpriteEffects.None, 0f);
                }
            }
            catch { }
        }
    }
}
