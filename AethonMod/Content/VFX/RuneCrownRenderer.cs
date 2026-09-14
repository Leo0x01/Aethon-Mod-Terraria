using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// RuneCrownRenderer — v6.03 — LA CORONA RÚNICA ESTELAR.
    ///
    /// DISEÑO NUEVO DESDE CERO sobre la referencia del usuario: un arco de
    /// OCHO GLIFOS RÚNICOS flotando sobre la cabeza — cada glifo es un trazo
    /// angular distinto (la lanza, el cáliz, la puerta, la estrella, el rayo,
    /// el arco, la espiral, el trono) con una PERLA rosa pálido en la punta.
    ///
    /// Los glifos:
    ///   - Flotan con oscilación senoidal desfasada por índice (viven).
    ///   - Pulsan su brillo por glifo (2-3 px de balanceo, latido propio).
    ///   - Emiten una chispa ascendente ocasional (la corona "respira" luz).
    ///
    /// No tiene NADA que ver con la corona de arcos del agujero negro: es
    /// escritura mágica, no líneas de campo.
    ///
    /// Uso (biblioteca): VFXCore.Begin() → ComputeQuads(...) →
    /// VFXCore.AppendToPlayerDraw(...) o VFXCore.FlushAdditive(...).
    /// </summary>
    public static class RuneCrownRenderer
    {
        /// <summary>Número de glifos del arco.</summary>
        public const int GlyphCount = 8;

        /// <summary>
        /// Tabla de runas: cada glifo es una lista de TRAZOS (par de puntos
        /// en espacio local del glifo, ~11×15 px). Ocho diseños angulares
        /// distintos, originales, legibles a distancia.
        /// </summary>
        private static readonly Vector2[][] _runes = new Vector2[][]
        {
            // R0 — LA LANZA: columna + dos diagonales hacia la derecha.
            new Vector2[] { new(-2.5f, 7f), new(-2.5f, -7f), new(-2.5f, -2f), new(3f, -6f), new(-2.5f, 3f), new(2.5f, -1.5f) },
            // R1 — EL CÁLIZ: dos diagonales que caen al centro + base.
            new Vector2[] { new(-4f, -6f), new(0f, 3f), new(4f, -6f), new(0f, 3f), new(-2f, 5.5f), new(2f, 5.5f) },
            // R2 — LA PUERTA: dos columnas + dos travesaños.
            new Vector2[] { new(-3f, 7f), new(-3f, -7f), new(3f, 7f), new(3f, -7f), new(-3f, -5f), new(3f, -5f), new(-3f, 2f), new(3f, 2f) },
            // R3 — LA ESTRELLA: cruz + dos diagonales (el asterisco).
            new Vector2[] { new(0f, 7f), new(0f, -7f), new(-4f, 0f), new(4f, 0f), new(-3f, -5f), new(3f, 5f), new(3f, -5f), new(-3f, 5f) },
            // R4 — EL RAYO: zigzag eléctrico de tres quiebros.
            new Vector2[] { new(-2f, 7f), new(0.5f, 1f), new(0.5f, 1f), new(-2f, -3f), new(-2f, -3f), new(2.5f, -7f) },
            // R5 — EL ARCO: dos diagonales simétricas + dintel superior.
            new Vector2[] { new(-3f, 6f), new(-1f, -6f), new(1f, -6f), new(3f, 6f), new(-1.5f, -6f), new(1.5f, -6f) },
            // R6 — LA ESPIRAL: escalera angular que gira.
            new Vector2[] { new(-3f, 6f), new(-3f, -2f), new(-3f, -2f), new(3f, -6f), new(3f, -6f), new(3f, 2f), new(3f, 2f), new(-2f, 6f) },
            // R7 — EL TRONO: columna central + dos brazos en diagonal + faja.
            new Vector2[] { new(0f, 7f), new(0f, -7f), new(-3f, -2f), new(0f, -7f), new(0f, -2f), new(3f, -7f), new(-2f, 4f), new(2f, 4f) },
        };

        /// <summary>
        /// Calcula el arco rúnico completo como cuadros de luz en el buffer
        /// de VFXCore (coordenadas de MUNDO).
        /// </summary>
        /// <param name="anchor">Centro de la cabeza sobre la que flota.</param>
        /// <param name="scale">Escala del conjunto (para una cabeza humana
        /// ≈ 1; derivar de player.height / 42f para crecer con el sprite).</param>
        /// <param name="time">Tiempo animado (Main.GlobalTimeWrappedHourly).</param>
        /// <param name="alpha">Multiplicador global de intensidad (0..1).</param>
        public static void ComputeQuads(Vector2 anchor, float scale, float time, float alpha = 1f)
        {
            if (scale <= 0.05f) return;

            float arcRadius = 26f * scale;
            // El arco flota por ENCIMA de la cabeza — y ALTO: así no compite
            // con la corona de arcos si el jugador lleva AMBAS a la vez.
            Vector2 arcCenter = anchor - new Vector2(0f, 13f * scale);

            // Halo tenue del arco completo (cohesión: es UNA corona, no 8
            // runas sueltas) — tres glows suaves siguiendo el arco.
            for (int h = 0; h < 3; h++)
            {
                float ha = -MathHelper.PiOver2 + (h - 1) * 0.62f;
                Vector2 hpos = arcCenter + new Vector2(
                    (float)Math.Cos(ha) * arcRadius * 0.92f,
                    (float)Math.Sin(ha) * arcRadius * 0.92f);
                float hpulse = 0.7f + 0.3f * (float)Math.Sin(time * 1.6f + h * 2.1f);
                VFXCore.Quad(hpos, VFXPalettes.RuneStars.ArcHalo * (0.10f * hpulse * alpha),
                    new Vector2(30f * scale, 30f * scale));
            }

            for (int g = 0; g < GlyphCount; g++)
            {
                // Ángulo del glifo en el arco: -160° → -20° (semicírculo sup.).
                float angle = -MathHelper.Pi + MathHelper.Pi * 0.11f +
                              g / (float)(GlyphCount - 1) * MathHelper.Pi * 0.78f;

                // Flotación viva: el radio respira por glifo (desfasado) y el
                // glifo entero se mece verticalmente (oscilación 2-3 px).
                float floatR = arcRadius +
                               2.4f * scale * (float)Math.Sin(time * 1.35f + g * 0.9f);
                float bobY = 2.0f * scale * (float)Math.Sin(time * 0.85f + g * 1.7f);

                Vector2 glyphPos = arcCenter + new Vector2(
                    (float)Math.Cos(angle) * floatR,
                    (float)Math.Sin(angle) * floatR + bobY);

                // Latido de brillo propio por glifo.
                float pulse = 0.75f + 0.25f * (float)Math.Sin(time * 2.4f + g * 1.3f);

                // Trazos de la runa: glows ESTIRADOS a lo largo de cada trazo
                // (soft capsule) — gradiente base fucsia → punta rosa pálido.
                Vector2[] strokes = _runes[g % _runes.Length];
                float strokeW = 2.1f * scale;

                for (int s = 0; s < strokes.Length; s += 2)
                {
                    Vector2 a = glyphPos + strokes[s] * scale;
                    Vector2 b = glyphPos + strokes[s + 1] * scale;
                    Vector2 mid = (a + b) * 0.5f;
                    Vector2 delta = b - a;
                    float len = delta.Length();
                    if (len < 0.01f) continue;
                    float rot = (float)Math.Atan2(delta.Y, delta.X);

                    // La altura del punto medio dentro del glifo (y local −7..7)
                    // manda el gradiente: abajo cuerpo, arriba punta pálida.
                    float localY = ((strokes[s].Y + strokes[s + 1].Y) * 0.5f + 7f) / 14f; // 0 abajo → 1 arriba
                    Color strokeCol = VFXPalettes.RuneStars.Glyph(1f - localY * 0.75f);

                    VFXCore.Quad(mid, strokeCol * (pulse * alpha),
                        new Vector2(len + strokeW, strokeW), rot);
                }

                // LA PERLA de la punta: punto de luz rosa pálido encima del
                // glifo, con núcleo casi blanco BRILLANTE (la "gema" de la
                // corona — generosa, para leerse a distancia).
                float pearlPulse = 0.8f + 0.2f * (float)Math.Sin(time * 3.0f + g * 2.0f);
                Vector2 pearlPos = glyphPos + new Vector2(0f, -11.5f * scale);
                VFXCore.Quad(pearlPos, VFXPalettes.RuneStars.Pearl * (pulse * alpha),
                    new Vector2(7.0f * scale, 7.0f * scale));
                VFXCore.Quad(pearlPos, VFXPalettes.RuneStars.PearlCore * (pearlPulse * alpha),
                    new Vector2(3.2f * scale, 3.2f * scale));
            }
        }

        /// <summary>
        /// Posición mundial de la punta del glifo g (para las chispas
        /// ascendentes que emite la corona desde las perlas).
        /// </summary>
        public static Vector2 GetPearlPosition(Vector2 anchor, float scale, float time, int g)
        {
            float arcRadius = 26f * scale;
            Vector2 arcCenter = anchor - new Vector2(0f, 13f * scale);
            float angle = -MathHelper.Pi + MathHelper.Pi * 0.11f +
                          g / (float)(GlyphCount - 1) * MathHelper.Pi * 0.78f;
            float floatR = arcRadius + 2.4f * scale * (float)Math.Sin(time * 1.35f + g * 0.9f);
            float bobY = 2.0f * scale * (float)Math.Sin(time * 0.85f + g * 1.7f);
            return arcCenter + new Vector2(
                (float)Math.Cos(angle) * floatR,
                (float)Math.Sin(angle) * floatR + bobY) - new Vector2(0f, 11.5f * scale);
        }
    }
}
