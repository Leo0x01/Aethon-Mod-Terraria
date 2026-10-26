using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// FAJACPU — v6.54.0 — LA MINI-LIB DE LAS TEXTURAS DE LA INVESTIGACIÓN.
    ///
    /// El contrato compartido de los efectos nacidos de la investigación
    /// de las texturas de Goozma (Task 74-a/b/c): cargar UNA textura a
    /// un array float (GetData lazy en el hilo de dibujo, una vez) y
    /// muestrearla BILINEAL con wrap (el sampler del shader original) —
    /// para luego pintar FAJAS DE TRIÁNGULOS con COLOR POR VÉRTICE (el
    /// pipeline probado de RayoStrip/AlasAurora: la fórmula del pixel
    /// shader se resuelve en CPU por nodo).
    ///
    /// Es el mismo espíritu que RayosConcentradosLib, compartido: el
    /// trueno ramificado (RayoRamo), el charco de lodo (LodoOscuro), la
    /// estela espacial (EstelaEspacial), la tinta viva (TintaViva con
    /// su LUT) y las líneas de constelación.
    /// </summary>
    public static class FajaCPU
    {
        private static BasicEffect _fx;
        private static VertexPositionColor[] _verts = new VertexPositionColor[8192];

        private static readonly BlendState _pantalla = new BlendState
        {
            ColorSourceBlend = Blend.InverseDestinationColor,
            ColorDestinationBlend = Blend.One,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.One,
        };

        // ==================================================================
        //  LA CARGA (una vez por textura — cache por ruta)
        // ==================================================================

        private static readonly System.Collections.Generic.Dictionary<string, Textura> _cache = new();

        /// <summary>Una textura viva (array float + dimensiones).</summary>
        public sealed class Textura
        {
            public readonly float[] Datos;
            public readonly int W, H;

            internal Textura(float[] datos, int w, int h)
            {
                Datos = datos; W = w; H = h;
            }

            /// <summary>El muestreo BILINEAL wrap (U y V periódicos).</summary>
            public float M(float u, float v)
            {
                u -= MathF.Floor(u); v -= MathF.Floor(v);
                float x = u * W - 0.5f, y = v * H - 0.5f;
                int x0 = (int)MathF.Floor(x), y0 = (int)MathF.Floor(y);
                float fx = x - x0, fy = y - y0;
                int x1 = (x0 + 1) % W, y1 = (y0 + 1) % H;
                x0 %= W; if (x0 < 0) x0 += W;
                y0 %= H; if (y0 < 0) y0 += H;
                float a = Datos[y0 * W + x0], b = Datos[y0 * W + x1];
                float c = Datos[y1 * W + x0], d = Datos[y1 * W + x1];
                return (a + (b - a) * fx) * (1f - fy) + (c + (d - c) * fx) * fy;
            }

            /// <summary>El canal RGB como Color (para texturas de color — LUTs).</summary>
            public Color MColor(float u, float v)
            {
                u -= MathF.Floor(u); v -= MathF.Floor(v);
                int x = (int)(u * W); if (x >= W) x = W - 1; if (x < 0) x = 0;
                int y = (int)(v * H); if (y >= H) y = H - 1; if (y < 0) y = 0;
                if (_lutCache != null)
                    return _lutCache[y * W + x];
                float g = Datos[y * W + x];
                return new Color((int)(g * 255), (int)(g * 255), (int)(g * 255));
            }

            private Color[] _lutCache;

            /// <summary>Carga el RGB completo (para LUTs de color).</summary>
            internal void CargarColor(Texture2D tex)
            {
                var cols = new Microsoft.Xna.Framework.Color[tex.Width * tex.Height];
                tex.GetData(cols);
                _lutCache = cols;
            }
        }

        /// <summary>Carga (o devuelve del cache) una textura como canal
        /// (R por defecto, o A para las de máscara alpha).</summary>
        public static Textura Cargar(string ruta, bool alpha = false)
        {
            if (_cache.TryGetValue(ruta, out Textura t) && t != null)
                return t;
            Texture2D tex = Terraria.ModLoader.ModContent.Request<Texture2D>(
                ruta, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            var cols = new Microsoft.Xna.Framework.Color[tex.Width * tex.Height];
            tex.GetData(cols);
            var datos = new float[tex.Width * tex.Height];
            for (int i = 0; i < cols.Length; i++)
                datos[i] = (alpha ? cols[i].A : cols[i].R) / 255f;
            t = new Textura(datos, tex.Width, tex.Height);
            _cache[ruta] = t;
            return t;
        }

        /// <summary>Carga (o devuelve) una LUT DE COLOR completa.</summary>
        public static Textura CargarColor(string ruta)
        {
            if (_cache.TryGetValue(ruta, out Textura t) && t != null)
                return t;
            Texture2D tex = Terraria.ModLoader.ModContent.Request<Texture2D>(
                ruta, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            var cols = new Microsoft.Xna.Framework.Color[tex.Width * tex.Height];
            tex.GetData(cols);
            var datos = new float[tex.Width * tex.Height];
            for (int i = 0; i < cols.Length; i++)
                datos[i] = cols[i].R / 255f;
            t = new Textura(datos, tex.Width, tex.Height);
            t.CargarColor(tex);
            _cache[ruta] = t;
            return t;
        }

        // ==================================================================
        //  LA FAJA (la tira de quads con color por vértice)
        // ==================================================================

        /// <summary>
        /// Dibuja UNA FAJA: espina de puntos + semiancho por nodo +
        /// COLOR por nodo (el color de los dos vértices del nodo). El
        /// perfil transversal lo pone QUIEN LLAMA en el color (la
        /// textura muestreada en V ya vive en el color). Contrato de la
        /// casa: cierra el lote, dibuja, reabre. PANTALLA coords.
        /// </summary>
        public static bool Dibujar(Vector2[] espina, float[] semianchos, Color[] colores)
        {
            int nodos = espina.Length;
            if (Main.netMode == NetmodeID.Server || Main.gameMenu || nodos < 2) return false;
            if (colores.Length < nodos || semianchos.Length < nodos) return false;

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
                    int n = 0;
                    for (int i = 0; i < nodos - 1; i++)
                    {
                        Vector2 d = espina[i + 1] - espina[i];
                        if (d == Vector2.Zero) continue;
                        // la perpendicular del tramo (normalizada manual — cero GC)
                        float len = MathF.Sqrt(d.X * d.X + d.Y * d.Y);
                        Vector2 perp = new(-d.Y / len, d.X / len);

                        Vector2 a0 = espina[i] + perp * semianchos[i];
                        Vector2 b0 = espina[i] - perp * semianchos[i];
                        Vector2 a1 = espina[i + 1] + perp * semianchos[i + 1];
                        Vector2 b1 = espina[i + 1] - perp * semianchos[i + 1];

                        if (n + 6 > _verts.Length) break;
                        _verts[n++] = new VertexPositionColor(new Vector3(a0, 0f), colores[i]);
                        _verts[n++] = new VertexPositionColor(new Vector3(a1, 0f), colores[i + 1]);
                        _verts[n++] = new VertexPositionColor(new Vector3(b0, 0f), colores[i]);
                        _verts[n++] = new VertexPositionColor(new Vector3(a1, 0f), colores[i + 1]);
                        _verts[n++] = new VertexPositionColor(new Vector3(b1, 0f), colores[i + 1]);
                        _verts[n++] = new VertexPositionColor(new Vector3(b0, 0f), colores[i]);
                    }

                    if (n >= 3)
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
                            int prims = n / 3;
                            if (prims > 0)
                                gd.DrawUserPrimitives(PrimitiveType.TriangleList,
                                    _verts, 0, prims);
                        }
                    }
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
