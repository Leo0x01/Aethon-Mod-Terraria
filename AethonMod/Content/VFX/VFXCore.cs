using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// VFXCore — v6.03 — EL NÚCLEO DE LA BIBLIOTECA VISUAL AETHON.
    ///
    /// Idea central: todos los efectos de brillo del mod (coronas, discos,
    /// rayos, halos) se describen como LISTAS DE CUADROS DE LUZ (GlowQuad:
    /// posición en MUNDO, color, escala, rotación) y luego se "vuelcan" al
    /// destino que haga falta con UNA sola llamada:
    ///
    ///   - FlushAdditive(...): dibujo directo aditivo en el mundo
    ///     (proyectiles, efectos de pantalla) — el brillo SUMA, look de neón.
    ///   - AppendToPlayerDraw(...): emisión de DrawData para las capas de
    ///     dibujado del jugador (PlayerDrawLayer) — el camino 100% seguro
    ///     con el pipeline de tML, sin tocar el SpriteBatch del renderer.
    ///
    /// Así un mismo renderizador (p. ej. la corona de arcos) sirve igual en
    /// un proyectil que sobre la cabeza de un jugador, sin duplicar la
    /// matemática y sin riesgo de romper estados de dibujo ajenos.
    ///
    /// El buffer de cuadros es estático y reutilizable: la generación de un
    /// efecto típico no aloca nada (cero GC por frame).
    /// </summary>
    public static class VFXCore
    {
        // ------------------------------------------------------------------
        //  EL CUADRO DE LUZ
        // ------------------------------------------------------------------

        /// <summary>
        /// Un "punto de luz" del tamaño y forma que haga falta: posición en
        /// COORDENADAS DE MUNDO, color (con alfa), escala (x≠y = estirado),
        /// rotación y textura propia (null = SoftGlow). En modo Inmediato el
        /// SpriteBatch envía cada Draw al momento: mezclar texturas en un
        /// mismo volcado es gratis.
        /// </summary>
        public struct GlowQuad
        {
            public Vector2 Position;
            public Color Color;
            public Vector2 Scale;
            public float Rotation;
            public Texture2D Texture;
        }

        /// <summary>Buffer reutilizable de cuadros (evita GC por frame).</summary>
        private static readonly List<GlowQuad> _quads = new List<GlowQuad>(512);

        /// <summary>Prepara el buffer para recibir un efecto nuevo.</summary>
        public static void Begin()
        {
            _quads.Clear();
        }

        /// <summary>Añade un cuadro de luz (posición en coords de mundo).</summary>
        public static void Quad(Vector2 position, Color color, Vector2 scale, float rotation = 0f)
        {
            _quads.Add(new GlowQuad { Position = position, Color = color, Scale = scale, Rotation = rotation, Texture = null });
        }

        /// <summary>Añade un cuadro con TEXTURA propia (Ring, GlowOrb...).</summary>
        public static void Quad(Vector2 position, Color color, Vector2 scale, Texture2D texture)
        {
            _quads.Add(new GlowQuad { Position = position, Color = color, Scale = scale, Rotation = 0f, Texture = texture });
        }

        /// <summary>v6.08 — Cuadro con TEXTURA propia Y ROTACIÓN: cintas de
        /// luz orientadas por la tangente (alas de mariposa, colas de
        /// cometa, rastros de acreción...).</summary>
        public static void Quad(Vector2 position, Color color, Vector2 scale, float rotation, Texture2D texture)
        {
            _quads.Add(new GlowQuad { Position = position, Color = color, Scale = scale, Rotation = rotation, Texture = texture });
        }

        /// <summary>Añade un cuadro circular (atajo: escala uniforme).</summary>
        public static void Quad(Vector2 position, Color color, float scale)
        {
            _quads.Add(new GlowQuad { Position = position, Color = color, Scale = new Vector2(scale, scale), Rotation = 0f, Texture = null });
        }

        /// <summary>Cuántos cuadros lleva el buffer (diagnóstico).</summary>
        public static int QuadCount => _quads.Count;

        // ------------------------------------------------------------------
        //  TEXTURAS COMPARTIDAS (resolución diferida: Asset, no .Value)
        // ------------------------------------------------------------------

        private static Asset<Texture2D> _softGlow;
        private static Asset<Texture2D> _ring;
        private static Asset<Texture2D> _glowOrb;

        /// <summary>Textura de brillo radial suave (la workhorse de la librería).</summary>
        public static Texture2D SoftGlow
        {
            get
            {
                if (_softGlow == null)
                    _softGlow = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Procedural/SoftGlow");
                return _softGlow.Value;
            }
        }

        /// <summary>Orbe con NÚCLEO SÓLIDO y borde suave (para vacíos negros
        /// absolutos y cuerpos compactos de luz).</summary>
        public static Texture2D GlowOrb
        {
            get
            {
                if (_glowOrb == null)
                    _glowOrb = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/GlowOrb");
                return _glowOrb.Value;
            }
        }

        /// <summary>Anillo fino (anillo de fotones, ecos, halos anulares).</summary>
        public static Texture2D Ring
        {
            get
            {
                if (_ring == null)
                    _ring = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Procedural/Ring");
                return _ring.Value;
            }
        }

        /// <summary>
        /// Tamaño de cuadro para que el TRAZO VISIBLE de la textura Ring
        /// (1024px, círculo gráfico a ~0.92 del semiancho) caiga en el radio
        /// pedido: el tamaño final del sprite debe ser ~2.17× ese radio.
        /// </summary>
        public static Vector2 RingQuadSize(float visibleRadius)
        {
            float s = visibleRadius * 2.174f;
            return new Vector2(s, s);
        }

        // ------------------------------------------------------------------
        //  VOLCADO 1 — DIBUJO DIRECTO ADITIVO (mundo / proyectiles)
        // ------------------------------------------------------------------

        /// <summary>
        /// Vuelca el buffer actual con blending ADITIVO: el brillo suma sobre
        /// lo que ya hay en pantalla (neón real). Los cuadros están en coords
        /// de mundo; se les resta Main.screenPosition aquí, una sola vez.
        ///
        /// Semántica de escala: el cuadro mide Scale píxeles FINALES en
        /// pantalla (ancho×alto), sea cual sea la resolución de la textura.
        /// </summary>
        /// <param name="texture">Textura de los cuadros (SoftGlow por defecto).</param>
        /// <param name="endActiveBatch">True si puede haber un batch abierto
        /// que haya que cerrar antes (p. ej. venimos de un PreDraw).</param>
        public static void FlushAdditive(Texture2D texture = null, bool endActiveBatch = true)
        {
            if (_quads.Count == 0) return;
            if (Main.netMode == Terraria.ID.NetmodeID.Server) return;

            Texture2D defaultTex = texture ?? SoftGlow;

            if (endActiveBatch)
                Main.spriteBatch.End();

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);

            Vector2 screen = Main.screenPosition;
            for (int i = 0; i < _quads.Count; i++)
            {
                GlowQuad q = _quads[i];
                if (q.Color.A == 0) continue;

                Texture2D tex = q.Texture ?? defaultTex;
                Vector2 invTex = new Vector2(1f / tex.Width, 1f / tex.Height);
                Main.spriteBatch.Draw(tex, q.Position - screen, null,
                    q.Color, q.Rotation, tex.Size() * 0.5f, q.Scale * invTex, SpriteEffects.None, 0f);
            }

            Main.spriteBatch.End();
            _quads.Clear();
        }

        /// <summary>
        /// Vuelca el buffer como DrawData dentro de la capa de dibujado de un
        /// jugador (PlayerDrawLayer): se añaden al DrawDataCache y tML los
        /// compone con el resto del jugador — el camino oficial, sin tocar
        /// el estado del renderer. Los cuadros van en coords de MUNDO.
        /// </summary>
        /// <param name="drawInfo">El PlayerDrawSet de la capa.</param>
        /// <param name="texture">Textura de los cuadros (SoftGlow por defecto).</param>
        /// <param name="effects">Efectos de espejado del jugador (para
        /// respetar su dirección).</param>
        public static void AppendToPlayerDraw(ref PlayerDrawSet drawInfo, Texture2D texture = null,
            SpriteEffects effects = SpriteEffects.None)
        {
            if (_quads.Count == 0) return;

            Texture2D defaultTex = texture ?? SoftGlow;
            Vector2 screen = Main.screenPosition;

            for (int i = 0; i < _quads.Count; i++)
            {
                GlowQuad q = _quads[i];
                if (q.Color.A == 0) continue;

                Texture2D tex = q.Texture ?? defaultTex;
                Vector2 invTex = new Vector2(1f / tex.Width, 1f / tex.Height);

                // La posición de DrawData vive en coords de PANTALLA.
                Vector2 pos = q.Position - screen;

                drawInfo.DrawDataCache.Add(new DrawData(
                    tex, pos, null, q.Color, q.Rotation,
                    tex.Size() * 0.5f, q.Scale * invTex, effects));
            }

            _quads.Clear();
        }

        // ------------------------------------------------------------------
        //  HELPERS DE MOVIMIENTO (los "latidos" de la librería)
        // ------------------------------------------------------------------

        /// <summary>Respiración: 0..1→0.94..1.06 con la fase pedida.</summary>
        public static float Breathe(float time, float speed = 2.2f, float phase = 0f, float amplitude = 0.06f)
        {
            return 1f + amplitude * (float)Math.Sin(time * speed + phase);
        }

        /// <summary>Oscilación suave -1..1 (balanceo, flotación).</summary>
        public static float Sway(float time, float speed = 0.9f, float phase = 0f)
        {
            return (float)Math.Sin(time * speed + phase);
        }

        /// <summary>
        /// Hash determinista [0,1): la MISMA secuencia en todas las máquinas
        /// sin sincronizar nada (rayos, destellos por índice).
        /// </summary>
        public static float Hash01(int seed, int a, int b)
        {
            int h = unchecked(seed * 374761393 + a * 668265263 + b * 1911520717);
            h = unchecked(h ^ (h >> 13));
            h = unchecked(h * 1274126177);
            h = unchecked(h ^ (h >> 16));
            return (h & 0xFFFFFF) / 16777216f;
        }

        /// <summary>Elipse paramétrica: punto en el ángulo t (rad).</summary>
        public static Vector2 Ellipse(Vector2 center, float semiMajor, float semiMinor, float tilt, float t)
        {
            float ct = (float)Math.Cos(t);
            float st = (float)Math.Sin(t);
            // Punto en la elipse sin girar (eje mayor = X).
            Vector2 local = new Vector2(semiMajor * ct, semiMinor * st);
            float cR = (float)Math.Cos(tilt);
            float sR = (float)Math.Sin(tilt);
            return center + new Vector2(local.X * cR - local.Y * sR, local.X * sR + local.Y * cR);
        }
    }
}
