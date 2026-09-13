using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using ReLogic.Content;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// LightningCore — v6.18 — LA LIBRERÍA DE RAYOS DEL PROYECTO.
    ///
    /// Síntesis de TODA la investigación del ecosistema (estudio de los
    /// grandes mods de VFX), re-implementada 100% con código PROPIO sobre
    /// nuestra pila (SpriteBatch + SoftGlow procedural):
    ///
    ///   1. ZIGZAG POR PERPENDICULAR DE CUERDA — cada punto interior se
    ///      desplaza sobre la perpendicular de la cuerda (i-1 ↔ i+1):
    ///      el rayo "tiembla" de forma orgánica y los EXTREMOS quedan
    ///      anclados donde el llamador los puso.
    ///
    ///   2. DOBLE TIRA CUERPO + NÚCLEO — cada segmento se dibuja DOS
    ///      veces: funda ancha de color (el "halo") y núcleo fino casi
    ///      blanco a ¼ del ancho (la "línea caliente" de los rayos de
    ///      verdad). Es lo que hace que un rayo se lea como ENERGÍA y
    ///      no como una línea dibujada.
    ///
    ///   3. PARPADEO VIVO — el rayo se RE-GENERAR cada pocos ticks
    ///      (reloj flick) y además tiene probabilidad de "apagarse" un
    ///      frame entero (flicker). Ambos deterministas por semilla:
    ///      todas las máquinas ven el MISMO rayo sin sincronizar nada.
    ///
    ///   4. ARCOS CIRCULARES — rayos que recorren un trozo de círculo
    ///      alrededor de un centro (las "coronas eléctricas" alrededor
    ///      del horizonte de sucesos).
    ///
    ///   5. RAMIFICACIÓN CON DECAIMIENTO — un rayo principal con ramas
    ///      laterales que HEREDAN ancho y longitud decrecientes (la
    ///      estructura de los relámpagos ramificados de tormenta).
    ///
    ///   6. SUAVIZADO DE CAMINOS — Catmull-Rom para convertir cualquier
    ///      polilínea (volutas, chorros) en un rayo serpenteante.
    ///
    /// ESPACIO: los métodos de DIBUJO son agnósticos — reciben
    /// coordenadas tal cual (de pantalla si el batch está en coords de
    /// pantalla). El batch debe llegar ABIERTO en modo aditivo; los
    /// métodos NO lo abren ni lo cierran (contrato distinto al de los
    /// renderers de agujero para poder anidar).
    ///
    /// Uso típico dentro de un renderer:
    ///   int flick = LightningCore.FlickTick(time, 12f);           // ~12 Hz
    ///   if (LightningCore.Flicker(seed, flick, 0.85f))
    ///       LightningCore.Bolt(spriteBatch, start, end, seed, flick,
    ///           width, halo, core, alpha);
    /// </summary>
    public static class LightningCore
    {
        // ==================================================================
        //  PINCEL — el SoftGlow procedural de la biblioteca
        // ==================================================================

        private static Asset<Texture2D> _glow;

        private static Texture2D Glow =>
            (_glow ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow")).Value;

        // ==================================================================
        //  RELOJ Y PARPADEO (deterministas)
        // ==================================================================

        /// <summary>
        /// El "tick de regeneración": avanza ~<paramref name="hz"/> veces
        /// por segundo. Pásalo a los métodos de puntos: mismo (seed, flick)
        /// = mismo rayo en todas las máquinas.
        /// </summary>
        public static int FlickTick(float time, float hz = 12f)
            => (int)(time * hz);

        /// <summary>
        /// ¿El rayo está ENCENDIDO este frame? (prob. de parpadeo —
        /// <paramref name="aliveChance"/> = 0.85 lo apaga un 15% de los
        /// regeneraciones, el "flicker" nervioso de los rayos reales).
        /// </summary>
        public static bool Flicker(int seed, int flick, float aliveChance = 0.85f)
            => VFXCore.Hash01(seed, 9871, flick) < aliveChance;

        // ==================================================================
        //  GENERADORES DE PUNTOS (matemática pura — reutilizable)
        // ==================================================================

        /// <summary>
        /// Rayo recto con zigzag: N puntos entre <paramref name="start"/> y
        /// <paramref name="end"/>. La tensión del rayo vive en el CENTRO
        /// (envolvente senoidal: quieto en los anclajes, loco en medio).
        /// </summary>
        public static Vector2[] BoltPoints(Vector2 start, Vector2 end,
            int seed, int flick, int segments = 7, float amp = 14f)
        {
            Vector2[] pts = new Vector2[segments + 1];
            Vector2 dir = end - start;
            float len = dir.Length();
            if (len < 0.001f) { for (int i = 0; i <= segments; i++) pts[i] = start; return pts; }
            dir /= len;
            Vector2 normal = new Vector2(-dir.Y, dir.X);
            float ampLen = Math.Min(amp, len * 0.22f);

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float envelope = (float)Math.Sin(t * Math.PI);          // tensión central
                float jitter = (VFXCore.Hash01(seed, flick, i * 31 + 7) - 0.5f) * 2f
                               * ampLen * envelope;
                pts[i] = start + dir * (len * t) + normal * jitter;
            }
            pts[0] = start;                    // anclajes EXACTOS
            pts[segments] = end;
            return pts;
        }

        /// <summary>
        /// ARCO ELÉCTRICO: recorre el círculo de <paramref name="center"/>
        /// desde <paramref name="a1"/> hasta <paramref name="a2"/> con
        /// jitter radial — la corona de chispas alrededor de un horizonte.
        /// </summary>
        public static Vector2[] ArcPoints(Vector2 center, float radius,
            float a1, float a2, int seed, int flick, int count = 9, float amp = 0.16f)
        {
            Vector2[] pts = new Vector2[count + 1];
            for (int i = 0; i <= count; i++)
            {
                float t = i / (float)count;
                float ang = a1 + (a2 - a1) * t;
                // El radio VIBRA alrededor del nominal (jitter radial).
                float r = radius * (1f + (VFXCore.Hash01(seed, flick, i * 17 + 3) - 0.5f) * 2f * amp
                                    * (float)Math.Sin(t * Math.PI));
                pts[i] = center + new Vector2((float)Math.Cos(ang) * r, (float)Math.Sin(ang) * r);
            }
            return pts;
        }

        /// <summary>
        /// Jitter ESTILO CUERDA sobre un camino EXISTENTE (la técnica
        /// maestra): cada punto interior se desplaza sobre la perpendicular
        /// de la cuerda (i-1 ↔ i+1) — el rayo tiembla SIN mover sus anclas
        /// y sin desviarse del recorrido general. Los extremos quedan FIJOS.
        /// </summary>
        public static Vector2[] JitterPath(Vector2[] anchor, int seed, int flick, float amp)
        {
            int n = anchor.Length;
            Vector2[] pts = new Vector2[n];
            if (n < 3)
            {
                for (int i = 0; i < n; i++) pts[i] = anchor[i];
                return pts;
            }

            pts[0] = anchor[0];
            pts[n - 1] = anchor[n - 1];
            for (int i = 1; i < n - 1; i++)
            {
                // Perpendicular de la cuerda (i-1 ↔ i+1).
                Vector2 chord = anchor[i - 1] - anchor[i + 1];
                Vector2 normal = chord.SafeNormalize(Vector2.One)
                    .RotatedBy(MathHelper.PiOver2);
                float side = VFXCore.Hash01(seed, flick, i * 13 + 5) > 0.5f ? 1f : -1f;
                float len = side * amp * VFXCore.Hash01(seed, flick, i * 29 + 11);
                pts[i] = anchor[i] + normal * len;
            }
            return pts;
        }

        /// <summary>
        /// Suavizado Catmull-Rom de una polilínea (con puntos "fantasma"
        /// espejo en los extremos): convierte cualquier camino tosco en la
        /// curva fluida por la que un rayo serpentea.
        /// </summary>
        public static Vector2[] Smooth(Vector2[] path, int subdivisions = 4)
        {
            if (path == null || path.Length < 3 || subdivisions < 1)
                return path;

            var result = new List<Vector2>(path.Length * subdivisions);
            // Puntos fantasma espejo (la curva llega a los extremos reales).
            Vector2 first = path[0] * 2f - path[1];
            Vector2 last = path[path.Length - 1] * 2f - path[path.Length - 2];

            for (int i = 0; i < path.Length - 1; i++)
            {
                Vector2 p0 = i == 0 ? first : path[i - 1];
                Vector2 p1 = path[i];
                Vector2 p2 = path[i + 1];
                Vector2 p3 = i + 2 >= path.Length ? last : path[i + 2];

                for (int j = 0; j < subdivisions; j++)
                {
                    float t = j / (float)subdivisions;
                    result.Add(CatmullRom(p0, p1, p2, p3, t));
                }
            }
            result.Add(path[path.Length - 1]);
            return result.ToArray();
        }

        private static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * (2f * p1 +
                           (-p0 + p2) * t +
                           (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                           (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        // ==================================================================
        //  DIBUJO — la doble tira cuerpo + núcleo (batch ABIERTO, aditivo)
        // ==================================================================

        /// <summary>
        /// Dibuja un rayo completo por su lista de puntos: funda ancha +
        /// núcleo fino casi blanco + gorros de brillo en los extremos.
        /// </summary>
        /// <param name="batch">Batch ABIERTO en modo aditivo.</param>
        /// <param name="pts">Puntos (BoltPoints/ArcPoints/JitterPath/Smooth).</param>
        /// <param name="seed">Semilla (ramas y gorros).</param>
        /// <param name="flick">Tick de regeneración.</param>
        /// <param name="width">Grosor de la funda en px.</param>
        /// <param name="halo">Color de la funda.</param>
        /// <param name="core">Color del núcleo (casi blanco de verdad).</param>
        /// <param name="alpha">Multiplicador global.</param>
        /// <param name="coreScale">Fracción del ancho del núcleo (¼ por defecto).</param>
        public static void Bolt(SpriteBatch batch, Vector2[] pts, int seed, int flick,
            float width, Color halo, Color core, float alpha = 1f, float coreScale = 0.30f)
        {
            if (pts == null || pts.Length < 2 || alpha <= 0.01f) return;

            for (int i = 0; i < pts.Length - 1; i++)
            {
                Vector2 a = pts[i];
                Vector2 b = pts[i + 1];
                Vector2 seg = b - a;
                float segLen = seg.Length();
                if (segLen < 0.5f) continue;
                float rot = (float)Math.Atan2(seg.Y, seg.X);
                Vector2 mid = (a + b) * 0.5f;

                // Perfil de tensión a lo largo del rayo (0 en anclas, pico al medio).
                float t = (i + 0.5f) / (pts.Length - 1);
                float wobble = 0.78f + 0.42f * (float)Math.Sin(t * Math.PI);

                // 1) FUNDA (cuerpo ancho de color).
                Quad(batch, mid, new Vector2(segLen + width, width * 2.05f * wobble),
                    rot, halo * alpha);
                // 2) NÚCLEO (la línea caliente a ~¼ del ancho).
                Quad(batch, mid, new Vector2(segLen + width * coreScale, width * coreScale * 2.4f),
                    rot, core * alpha);

                // 3) RAMAS con decaimiento (donde el hash lo pide).
                if (i > 0 && i < pts.Length - 2 &&
                    VFXCore.Hash01(seed, flick, i * 41 + 19) > 0.66f)
                {
                    float side = VFXCore.Hash01(seed, flick, i * 7 + 23) > 0.5f ? 1f : -1f;
                    // La rama HEREDA el 55% del ancho y sale en diagonal.
                    float branchLen = segLen * (1.4f + 1.1f *
                        VFXCore.Hash01(seed, flick, i * 53 + 31));
                    Vector2 dir = seg / segLen;
                    Vector2 normal = new Vector2(-dir.Y, dir.X);
                    Vector2 branchDir = (dir * 0.4f + normal * side)
                        .SafeNormalize(Vector2.UnitY);
                    Vector2 bEnd = b + branchDir * branchLen;
                    Vector2 bMid = (b + bEnd) * 0.5f;
                    float bRot = (float)Math.Atan2(branchDir.Y, branchDir.X);

                    Quad(batch, bMid,
                        new Vector2(branchLen + width * 0.5f, width * 1.05f),
                        bRot, halo * (0.55f * alpha));
                    Quad(batch, bMid,
                        new Vector2(branchLen + width * 0.15f, width * 0.42f),
                        bRot, core * (0.55f * alpha));
                }
            }

            // 4) GORROS de brillo en los extremos (la descarga y el impacto).
            Cap(batch, pts[0], width, halo, core, alpha);
            Cap(batch, pts[pts.Length - 1], width, halo, core, alpha);
        }

        /// <summary>Atajo: rayo recto de A a B en una sola llamada.</summary>
        public static void Bolt(SpriteBatch batch, Vector2 start, Vector2 end,
            int seed, int flick, float width, Color halo, Color core,
            float alpha = 1f, int segments = 7, float amp = 14f)
        {
            Vector2[] pts = BoltPoints(start, end, seed, flick, segments, amp);
            Bolt(batch, pts, seed, flick, width, halo, core, alpha);
        }

        /// <summary>Atajo: arco eléctrico alrededor de un centro.</summary>
        public static void Arc(SpriteBatch batch, Vector2 center, float radius,
            float a1, float a2, int seed, int flick, float width,
            Color halo, Color core, float alpha = 1f, int count = 9)
        {
            Vector2[] pts = ArcPoints(center, radius, a1, a2, seed, flick, count);
            Bolt(batch, pts, seed, flick, width, halo, core, alpha);
        }

        // ------------------------------------------------------------------
        //  PRIMITIVAS INTERNAS
        // ------------------------------------------------------------------

        /// <summary>Quad centrado con el SoftGlow (tamaño total = size px).</summary>
        private static void Quad(SpriteBatch batch, Vector2 pos, Vector2 size, float rot, Color tint)
        {
            if (tint.A == 0) return;
            batch.Draw(Glow, pos, null, tint, rot,
                new Vector2(Glow.Width, Glow.Height) * 0.5f,
                size / new Vector2(Glow.Width, Glow.Height),
                SpriteEffects.None, 0f);
        }

        /// <summary>El "gorro" de descarga: glow doble en un extremo.</summary>
        private static void Cap(SpriteBatch batch, Vector2 pos, float width,
            Color halo, Color core, float alpha)
        {
            Quad(batch, pos, new Vector2(width * 3.4f, width * 3.4f), 0f, halo * (0.75f * alpha));
            Quad(batch, pos, new Vector2(width * 1.7f, width * 1.7f), 0f, core * (0.95f * alpha));
        }
    }
}
