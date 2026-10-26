using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// RAYOCONCENTRADOLIB — v6.54.0 — EL RAYO DE FUSIÓN DEL JUGADOR.
    ///
    /// La letra del usuario: «para el rayo de fusion se usan varias
    /// texturas llamadas FusionRay.png, FusionRay_0.png y asi hasta
    /// FusionRay_3.png, analiza eso y creamos nuestros propios
    /// FusionRay.png basandote en esos, los nuestros se llamaran rayos
    /// concentrados».
    ///
    /// LA INVESTIGACIÓN (Task 74-a): el jefe NO anima frames — son
    /// CUATRO CAPAS DEL MISMO HAZ multiplicadas en el pixel shader:
    ///   _0 (uTexture0) el cuerpo ruidoso fBm · _1 (uBits) las islas de
    ///   chispas · _2 (uGlow) el perfil 1D de brillo · _3 (uTexture1)
    ///   el núcleo+falda — con scroll U a 3 velocidades, wobble
    ///   vertical sinusoidal, FADE EN LENTE (la punta muere redonda),
    ///   chispas de DOBLE muestreo (la misma textura a 2 velocidades
    ///   multiplicadas = titileo), NÚCLEO BLANCO por umbral de
    ///   luminancia y el ARCOÍRIS VIAJERO del color por vértice.
    ///
    /// LA VERSIÓN CASA (este archivo): tML 2026.08 solo carga .fxc
    /// compilados (la lección de la .53.1 — no hay pipeline de shaders
    /// HLSL para el mod), así que EL SHADER SE RESUELVE EN CPU POR
    /// VÉRTICE: las 4 texturas propias (RayoConcentrado_0..3.png,
    /// nacidas de tools/gen_investigacion_v6540.py) se cargan UNA VEZ
    /// a arrays float (GetData lazy en el primer frame de dibujo) y
    /// cada vértice de la malla (96 nodos × 5 columnas) muestrea la
    /// fórmula COMPLETA del jefe: triple producto, bits, umbral
    /// blanco, lente y arcoíris — una faja de triángulos planos con
    /// color por vértice, el pipeline probado de RayoStrip/AlasAurora.
    ///
    /// LAS CHISPAS: la letra pide sustituir la FusionRaySpark por «los
    /// brillos de los soles rúnicos que es incluso superior» — la boca
    /// y las descargas usan VFXCore.SoftGlow (el brillo de los soles)
    /// y la aguja telegráfica usa el GlowRay de la casa.
    /// </summary>
    public static class RayosConcentradosLib
    {
        // === LAS 4 CAPAS (arrays float cacheados tras GetData) ===
        private static float[] _t0, _t1, _t2, _t3;
        private static int _w0, _h0, _w1, _h1, _w2, _h2, _w3, _h3;
        private static bool _cargaFallo;

        // === EL DISPOSITIVO (el patrón de la casa) ===
        private static BasicEffect _fx;
        private static VertexPositionColor[] _verts = new VertexPositionColor[96 * 5 * 6 + 64];

        /// <summary>El lote de pantalla suave (soft-add) — el de la casa.</summary>
        private static readonly BlendState _pantalla = new BlendState
        {
            ColorSourceBlend = Blend.InverseDestinationColor,
            ColorDestinationBlend = Blend.One,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.One,
        };

        private const int Nodos = 96;
        private static readonly float[] Col = { -1f, -0.5f, 0f, 0.5f, 1f };

        // ==================================================================
        //  LA CARGA DE LAS TEXTURAS (lazy, hilo de dibujo, una vez)
        // ==================================================================

        private static bool AsegurarCargado()
        {
            if (_t0 != null) return true;
            if (_cargaFallo) return false;
            try
            {
                Cargar("AethonMod/Content/Effects/Procedural/Investigacion/RayoConcentrado_0",
                    out _t0, out _w0, out _h0, alpha: false);
                Cargar("AethonMod/Content/Effects/Procedural/Investigacion/RayoConcentrado_1",
                    out _t1, out _w1, out _h1, alpha: true);
                Cargar("AethonMod/Content/Effects/Procedural/Investigacion/RayoConcentrado_2",
                    out _t2, out _w2, out _h2, alpha: false);
                Cargar("AethonMod/Content/Effects/Procedural/Investigacion/RayoConcentrado_3",
                    out _t3, out _w3, out _h3, alpha: false);
                return true;
            }
            catch
            {
                _cargaFallo = true;
                return false;
            }
        }

        private static void Cargar(string ruta, out float[] arr, out int w, out int h, bool alpha)
        {
            Texture2D tex = Terraria.ModLoader.ModContent.Request<Texture2D>(
                ruta, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            w = tex.Width; h = tex.Height;
            var colores = new Microsoft.Xna.Framework.Color[w * h];
            tex.GetData(colores);
            arr = new float[w * h];
            for (int i = 0; i < colores.Length; i++)
                arr[i] = (alpha ? colores[i].A : colores[i].R) / 255f;
        }

        /// <summary>El muestreo BILINEAL con wrap en ambos ejes (el
        /// sampler wrap+linear del shader original).</summary>
        private static float M(float[] t, int w, int h, float u, float v)
        {
            u -= MathF.Floor(u); v -= MathF.Floor(v);
            float x = u * w - 0.5f, y = v * h - 0.5f;
            int x0 = (int)MathF.Floor(x), y0 = (int)MathF.Floor(y);
            float fx = x - x0, fy = y - y0;
            int x1 = (x0 + 1) % w, y1 = (y0 + 1) % h;
            x0 %= w; if (x0 < 0) x0 += w; y0 %= h; if (y0 < 0) y0 += h;
            float a = t[y0 * w + x0], b = t[y0 * w + x1];
            float c = t[y1 * w + x0], d = t[y1 * w + x1];
            return (a + (b - a) * fx) * (1f - fy) + (c + (d - c) * fx) * fy;
        }

        private static float SmoothstepInv(float x)   // smoothstep(0.5, 0, x)
        {
            float t = MathHelper.Clamp((x - 0.5f) / (0f - 0.5f), 0f, 1f);
            return t * t * (3f - 2f * t);
        }

        // ==================================================================
        //  EL HAZ (la fórmula del jefe, resuelta por vértice)
        // ==================================================================

        /// <summary>
        /// Dibuja EL RAYO CONCENTRADO. Contrato de la casa: cierra el
        /// lote, dibuja, reabre. Todo en COORDENADAS DE PANTALLA.
        /// </summary>
        /// <param name="boca">La boca del haz (pantalla).</param>
        /// <param name="rotacion">Dirección del haz.</param>
        /// <param name="largo">Largo en px.</param>
        /// <param name="anchoMax">Semiancho máx en px (la fórmula del jefe respira alrededor).</param>
        /// <param name="reloj">El reloj del efecto (avanza con el tiempo).</param>
        /// <param name="activacion">0..1 el crecimiento del haz (fase de disparo).</param>
        public static bool DibujarHaz(Vector2 boca, float rotacion, float largo,
            float anchoMax, float reloj, float activacion)
        {
            if (Main.netMode == NetmodeID.Server || Main.gameMenu) return false;
            if (!AsegurarCargado()) return false;
            if (largo < 8f || anchoMax < 1f) return false;

            try
            {
                GraphicsDevice gd = Main.graphics.GraphicsDevice;
                if (_fx == null)
                {
                    _fx = new BasicEffect(gd)
                    {
                        LightingEnabled = false,
                        VertexColorEnabled = true,
                        TextureEnabled = false,
                        Alpha = 1f,
                    };
                }

                bool cerro = VFXCore.CerrarLoteSiAbierto();
                try
                {
                    Vector2 dir = new(MathF.Cos(rotacion), MathF.Sin(rotacion));
                    Vector2 perp = new(-dir.Y, dir.X);

                    // LA TIRA CONTINUA: cada nodo emite sus QUADS con el
                    // nodo siguiente (cero juntas — la lección RayoStrip);
                    // el color de cada vértice resuelve la fórmula del
                    // shader del jefe (ColorNodo).
                    int n = 0;
                    for (int i = 0; i < Nodos - 1; i++)
                        n = EmitirSeguro(n, i, dir, perp, largo, anchoMax,
                            activacion, reloj, boca);

                    if (n >= 3)
                        Volcar(gd, n);
                }
                finally
                {
                    if (cerro) VFXCore.ReabrirLoteVanilla();
                }
                return true;
            }
            catch
            {
                try { VFXCore.ReabrirLoteVanilla(); } catch { }
                return false;
            }
        }

        /// <summary>EL COLOR DE UN VÉRTICE — la fórmula del pixel shader
        /// del jefe (FusionRayEffect.fx), resuelta en CPU.</summary>
        private static Color ColorNodo(float p, float v, float t, Color arcoiris, float ancho)
        {
            // EL FADE EN LENTE: el centro llega al 50 % del largo, los
            // bordes mueren antes → la punta muere REDONDA.
            float fade = SmoothstepInv(p - MathF.Sin(v * MathHelper.Pi) * 0.5f);

            // EL WOBBLE VERTICAL (±0.05 — el canal ondula a lo largo).
            float wobble = MathF.Sin((t * 4f + p) * MathHelper.TwoPi - MathHelper.PiOver2) * 0.05f;

            // LAS TRES CAPAS MULTIPLICADAS (el triple producto del jefe).
            float bas = M(_t0, _w0, _h0, t * 3f + p * 1.5f, v + wobble);
            float bas2 = M(_t3, _w3, _h3, t * 4f + p * 1.2f, v);
            float glow = M(_t2, _w2, _h2, t * 2f + p, v + wobble);

            float prod = glow * bas * bas2 * fade;
            float nucleo = bas * bas2;

            // EL LASER: halo tenue + el cuerpo realzado del producto.
            float lum = prod + nucleo * nucleo * fade;

            // LAS CHISPAS (bits): la MISMA textura a DOS velocidades
            // multiplicadas → titileo — y hacia la PUNTA (donde el fade
            // cae, como el jefe: smoothstep(1, 0.6, p)).
            float bits1 = M(_t1, _w1, _h1, t * 3f + p, v + wobble * 0.2f);
            float bits2 = M(_t1, _w1, _h1, t * 5f + p, v + wobble * 0.4f);
            float bits = bits1 * bits2 * (1.5f - fade);
            float haciaPunta = MathHelper.Clamp((1f - p) / 0.4f, 0f, 1f);

            int r, g, b;
            if (bits > 0.24f)
            {
                // LA CHISPA GANA: blanco-arcoíris titilando hacia la punta.
                float s = MathHelper.Clamp(bits * 2.2f, 0f, 1f) * haciaPunta;
                r = (int)(arcoiris.R * 0.15f + 255f * s * 0.85f);
                g = (int)(arcoiris.G * 0.15f + 255f * s * 0.85f);
                b = (int)(arcoiris.B * 0.15f + 255f * s * 0.85f);
            }
            else if (lum > 0.4f)
            {
                // EL NÚCLEO BLANCO-CALIENTE (el truco del jefe: si brilla
                // de más, SATURA a blanco puro).
                float s = MathHelper.Clamp(lum * 1.6f, 0f, 1f);
                r = (int)(arcoiris.R + (255 - arcoiris.R) * s);
                g = (int)(arcoiris.G + (255 - arcoiris.G) * s);
                b = (int)(arcoiris.B + (255 - arcoiris.B) * s);
            }
            else
            {
                // EL CUERPO: el arcoíris viajero por el producto, con el
                // halo susurrando detrás (glow·color).
                float cuerpo = lum * 1.9f;
                r = (int)Math.Min(255, arcoiris.R * cuerpo + arcoiris.R * glow * fade * 0.35f);
                g = (int)Math.Min(255, arcoiris.G * cuerpo + arcoiris.G * glow * fade * 0.35f);
                b = (int)Math.Min(255, arcoiris.B * cuerpo + arcoiris.B * glow * fade * 0.35f);
            }
            const int tope = 255;
            return new Color(r > tope ? tope : r, g > tope ? tope : g, b > tope ? tope : b, 255);
        }

        // (El emisor real: ver EmitirSeguro — el loop de arriba delega.)
        private static int EmitirSeguro(int n, int i, Vector2 dir, Vector2 perp,
            float largo, float anchoMax, float activacion, float reloj, Vector2 boca)
        {
            // cada nodo emite su anillo de QUADS con el nodo siguiente
            // (la tira continua — cero juntas, la lección de RayoStrip);
            // el nodo final cierra con ancho 0.
            if (i >= Nodos - 1) return n;
            float p0 = i / (float)(Nodos - 1), p1 = (i + 1) / (float)(Nodos - 1);
            float t = reloj;
            float respiracion = 1.1f + MathF.Cos(t * 4f) * 0.08f;
            float w0 = anchoMax * MathF.Pow(p0, 0.8f)
                * MathF.Pow(MathHelper.Lerp(1.01f, 0.7f, p0), 1f / 3f)
                * (respiracion - p0 * 0.06f) * activacion;
            float w1 = anchoMax * MathF.Pow(p1, 0.8f)
                * MathF.Pow(MathHelper.Lerp(1.01f, 0.7f, p1), 1f / 3f)
                * (respiracion - p1 * 0.06f) * activacion;
            Vector2 c0 = boca + dir * (largo * p0 * activacion);
            Vector2 c1 = boca + dir * (largo * p1 * activacion);
            Color arco0 = ColorGoozma.ALoLargo(t * 12f, p0 * activacion, 100f);
            Color arco1 = ColorGoozma.ALoLargo(t * 12f, p1 * activacion, 100f);

            for (int j = 0; j < Col.Length - 1; j++)
            {
                if (n + 6 > _verts.Length) return n;
                float va = (Col[j] + 1f) * 0.5f, vb = (Col[j + 1] + 1f) * 0.5f;
                Color ca0 = ColorNodo(p0 * activacion, va, t, arco0, w0);
                Color cb0 = ColorNodo(p0 * activacion, vb, t, arco0, w0);
                Color ca1 = ColorNodo(p1 * activacion, va, t, arco1, w1);
                Color cb1 = ColorNodo(p1 * activacion, vb, t, arco1, w1);

                Vector2 a0 = c0 + perp * (Col[j] * w0);
                Vector2 b0 = c0 + perp * (Col[j + 1] * w0);
                Vector2 a1 = c1 + perp * (Col[j] * w1);
                Vector2 b1 = c1 + perp * (Col[j + 1] * w1);

                _verts[n++] = new VertexPositionColor(new Vector3(a0, 0f), ca0);
                _verts[n++] = new VertexPositionColor(new Vector3(a1, 0f), ca1);
                _verts[n++] = new VertexPositionColor(new Vector3(b0, 0f), cb0);
                _verts[n++] = new VertexPositionColor(new Vector3(a1, 0f), ca1);
                _verts[n++] = new VertexPositionColor(new Vector3(b1, 0f), cb1);
                _verts[n++] = new VertexPositionColor(new Vector3(b0, 0f), cb0);
            }
            return n;
        }

        private static void Volcar(GraphicsDevice gd, int nVerts)
        {
            _fx.World = Main.GameViewMatrix.TransformationMatrix;
            _fx.View = Matrix.Identity;
            _fx.Projection = Matrix.CreateOrthographicOffCenter(
                0, Main.screenWidth, Main.screenHeight, 0, 0, -1);

            gd.BlendState = _pantalla;
            gd.DepthStencilState = DepthStencilState.None;
            gd.RasterizerState = RasterizerState.CullNone;

            foreach (EffectPass pass in _fx.CurrentTechnique.Passes)
            {
                pass.Apply();
                int porLote = (_verts.Length / 3) * 3;
                int inicio = 0;
                while (inicio < nVerts)
                {
                    int cuenta = Math.Min(nVerts - inicio, porLote);
                    int prims = cuenta / 3;
                    if (prims > 0)
                        gd.DrawUserPrimitives(PrimitiveType.TriangleList,
                            _verts, inicio, prims);
                    inicio += cuenta;
                }
            }
        }

        /// <summary>LA REINICIALIZACIÓN (device-lost — el patrón de la casa).</summary>
        public static void Reiniciar()
        {
            BasicEffect fx = _fx;
            _fx = null;
            if (fx != null)
            {
                Terraria.Main.QueueMainThreadAction(() =>
                {
                    try { fx.Dispose(); } catch { }
                });
            }
        }
    }
}
