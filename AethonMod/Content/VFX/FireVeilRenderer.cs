using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// FireVeilRenderer — v6.22 — EL FUEGO QUE ENVUELVE (100% CÓDIGO).
    ///
    /// EL ALGORITMO DE PROPAGACIÓN DE INTENSIDADES: un campo de W×H celdas
    /// guarda un nivel de intensidad 0..36 por celda; la BASE está siempre
    /// encendida (36) y cada tick el fuego PROPAGA hacia arriba con
    /// DECAIMIENTO ALEATORIO y DERIVA LATERAL — el fuego "camina" hacia
    /// el cielo consumiéndose. El color de cada celda sale de la TABLA DE
    /// 37 NIVELES propia (brasa oscura → carmesí → naranja → ámbar → oro
    /// → blanco cegador). Cero sprites de fuego: cada celda es un pincel
    /// procedural (FlameBrush) y la COHERENCIA de las llamas nace de la
    /// SOLAPACIÓN de las celdas vecinas.
    ///
    /// LA INTERACCIÓN CON EL MOVIMIENTO (el corazón de la petición):
    ///   · VIENTO — la velocidad horizontal del jugador EMPUJA la deriva
    ///     lateral de la propagación: al correr, las llamas se inclinan
    ///     EN CONTRA de la marcha (dejan estela tras de ti).
    ///   · AVIVO — la velocidad total SUBE la intensidad de la base: el
    ///     aire del movimiento alimenta el fuego (correr = arder más).
    ///   · INERCIA VERTICAL — al saltar, el campo se RETRASA por debajo
    ///     (la subida lo aplasta); al caer, se ESTIRA hacia arriba (el
    ///     viento relativo lo peina) — el offset vertical del render.
    ///   · PASES EXTRA — cayendo/volando la propagación corre MÁS pasos
    ///     por tick: las llamas alcanzan más alto (la columna de fuego).
    ///
    /// El estado (el campo) vive en FireVeilPlayer; esta clase es el
    /// MOTOR puro: simulación + paleta + quads al buffer de VFXCore
    /// (camino oficial AppendToPlayerDraw — el de las coronas).
    /// </summary>
    public static class FireVeilRenderer
    {
        // ==================================================================
        //  EL CAMPO — geometría del fuego
        // ==================================================================

        /// <summary>Ancho del campo en celdas.</summary>
        public const int W = 26;

        /// <summary>Alto del campo en celdas.</summary>
        public const int H = 38;

        /// <summary>Tamaño de la celda en px (el pincel solapa ~×1.4).</summary>
        public const float Cell = 4.4f;

        /// <summary>La PALETA: 37 niveles de brasa a blanco cegador.</summary>
        private static readonly Color[] Pal = BuildPalette();

        /// <summary>
        /// LA TABLA DE 37 COLORES propia: brasa oscura → carmesí → naranja
        /// → ámbar → oro → amarillo claro → blanco cegador (interpolación
        /// por puntos de control — nuestra curva, calibrada para leerse
        /// FUEGO sobre cualquier fondo).
        /// </summary>
        private static Color[] BuildPalette()
        {
            var ctrl = new (int r, int g, int b)[]
            {
                (  7,   7,   7),   // 0 — celda apagada (no se dibuja)
                ( 40,   8,   2),   // brasa profunda
                ( 84,  14,   4),   // carmesí oscuro
                (124,  24,   4),   // carmesí
                (166,  42,   6),   // naranja rojizo
                (198,  62,   6),   // naranja
                (222,  88,   8),   // naranja claro
                (240, 118,  14),   // ámbar
                (250, 148,  26),   // ámbar claro
                (255, 176,  46),   // oro
                (255, 202,  84),   // oro claro
                (255, 224, 130),   // amarillo pálido
                (255, 240, 190),   // casi blanco cálido
                (255, 250, 232),   // blanco cegador
            };
            int n = 37;
            var pal = new Color[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)(n - 1);
                float f = t * (ctrl.Length - 1);
                int a = (int)Math.Floor(f);
                int b = Math.Min(a + 1, ctrl.Length - 1);
                float k = f - a;
                pal[i] = Color.Lerp(
                    new Color(ctrl[a].r, ctrl[a].g, ctrl[a].b),
                    new Color(ctrl[b].r, ctrl[b].g, ctrl[b].b), k);
            }
            return pal;
        }

        // ==================================================================
        //  EL PINCEL — la celda de fuego (procedural)
        // ==================================================================

        private static ReLogic.Content.Asset<Microsoft.Xna.Framework.Graphics.Texture2D> _brush;

        private static Microsoft.Xna.Framework.Graphics.Texture2D Brush =>
            (_brush ??= Terraria.ModLoader.ModContent.Request<Microsoft.Xna.Framework.Graphics.Texture2D>(
                "AethonMod/Content/Effects/Procedural/FlameBrush")).Value;

        // ==================================================================
        //  LA SIMULACIÓN — un tick del fuego (motor de propagación)
        // ==================================================================

        /// <summary>
        /// AVANZA EL FUEGO un tick: re-siembra la base con la intensidad
        /// del movimiento y propaga hacia arriba con decaimiento, deriva
        /// y VIENTO. `grid` = W*H intensidades 0..36 (estado del jugador).
        /// </summary>
        /// <param name="grid">El campo (se modifica IN SITU).</param>
        /// <param name="bodyW">Ancho del cuerpo del jugador (px) — la siembra cubre la silueta.</param>
        /// <param name="vel">Velocidad del jugador (el viento y el avivo).</param>
        /// <param name="flying">¿Está volando con alas? (la columna).</param>
        /// <param name="falling">¿Caída rápida? (llamas estiradas).</param>
        public static void Step(int[] grid, float bodyW, Vector2 vel, bool flying, bool falling)
        {
            if (grid == null || grid.Length < W * H) return;

            float speed = vel.Length();
            float fanning = MathHelper.Clamp(speed / 7f, 0f, 1f);      // el avivo del aire
            int wind = (int)MathHelper.Clamp(-vel.X * 0.55f, -2.5f, 2.5f); // el viento en celdas

            // === 1. LA SIEMBRA — la base SIEMPRE encendida sobre la silueta ===
            // (las 3 filas inferiores; la intensidad baja con la distancia
            // al cuerpo y VIVE con el avivo del movimiento).
            float halfBody = Math.Max(bodyW * 0.55f, 8f) / Cell;   // medio ancho del cuerpo en celdas
            int center = W / 2;
            for (int y = H - 3; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    float d = Math.Abs(x - center) / halfBody;      // 0 en el cuerpo → 1+ fuera
                    if (d > 1.55f) { grid[y * W + x] = 0; continue; }

                    int baseI = d < 0.85f ? 36 : (int)(36 - 14f * (d - 0.85f) / 0.7f);
                    // EL AVIVO: el aire del movimiento sube la intensidad
                    // (y la aleatoriedad de la lumbre: la base VIVE).
                    int flick = Main.rand.Next(0, 4) - 1;           // −1..+2
                    int seedI = Math.Clamp(baseI + flick + (int)(fanning * 2.5f), 0, 36);
                    // Los huecos de la pira (la base respira con agujeros).
                    if (d > 0.55f && Main.rand.NextFloat() < 0.18f) seedI = Math.Max(0, seedI - 16);
                    grid[y * W + x] = seedI;
                }
            }

            // === 2. LA PROPAGACIÓN — el fuego camina hacia arriba ===
            // (los PASES: 1 normal · +1 cayendo · +2 volando — las llamas
            // alcanzan más alto cuando el viento relativo las estira).
            int passes = 1 + (falling ? 1 : 0) + (flying ? 2 : 0);
            for (int p = 0; p < passes; p++)
            {
                for (int y = 1; y < H; y++)
                {
                    int row = y * W;
                    int up = (y - 1) * W;
                    for (int x = 0; x < W; x++)
                    {
                        int v = grid[row + x];
                        if (v <= 0) { grid[up + x] = Math.Max(0, grid[up + x] - 6); continue; }

                        // EL DECAIMIENTO ALEATORIO (el consumo del combustible):
                        // sesgo cúbico — muchos pasos finos, algún salto grande.
                        float h = Main.rand.NextFloat();
                        int decay = (int)Math.Clamp(
                            Math.Sign(h - 0.5f) * Math.Pow(Math.Abs(h - 0.5f) * 2f, 1.6f) * 3.2f + 1.2f, 1, 5);

                        // LA DERIVA LATERAL + EL VIENTO (la interacción).
                        int drift = Main.rand.Next(-1, 2);
                        int nx = x + drift + wind;
                        if (nx < 0) nx = 0;
                        if (nx > W - 1) nx = W - 1;

                        int nv = Math.Max(0, v - decay);
                        // la celda destino SOLO crece si estaba más fría.
                        if (nv > grid[up + nx]) grid[up + nx] = nv;
                    }
                }
            }
        }

        // ==================================================================
        //  EL RENDER — las celdas al buffer de VFXCore (camino de coronas)
        // ==================================================================

        /// <summary>
        /// Calcula las llamas como cuadros de luz en el buffer de VFXCore
        /// (coordenadas de MUNDO). El campo se ancla a los PIES del
        /// jugador y SE ESTIRA/retrasa con la inercia vertical.
        /// </summary>
        /// <param name="feet">Posición mundial de los pies (player.Bottom).</param>
        /// <param name="grid">El campo de intensidades.</param>
        /// <param name="vel">Velocidad del jugador (la inercia del render).</param>
        /// <param name="alpha">Multiplicador global (0..1).</param>
        public static void ComputeQuads(Vector2 feet, int[] grid, Vector2 vel, float alpha)
        {
            if (grid == null || grid.Length < W * H || alpha <= 0.02f) return;

            // LA INERCIA VERTICAL del campo: al subir se retrasa por debajo
            // (aplastado contra el cuerpo); al caer se estira hacia arriba.
            float inertia = MathHelper.Clamp(-vel.Y * 1.15f, -9f, 15f);
            // LA INCLINACIÓN total de las llamas (el viento visible).
            float lean = MathHelper.Clamp(-vel.X * 0.35f, -10f, 10f);

            Vector2 origin = new Vector2(feet.X - W * Cell * 0.5f, feet.Y - H * Cell + inertia);

            for (int y = 0; y < H; y++)
            {
                // el nacimiento alto de la fila: las llamas de arriba nacen
                // MÁS ARRIBA cuando el viento las inclina (el abanico).
                float leanF = y / (float)H;
                for (int x = 0; x < W; x++)
                {
                    int v = grid[y * W + x];
                    if (v < 4) continue;   // las brasas apagadas no se pintan

                    float t = v / 36f;
                    Color c = Pal[v];

                    // LA CELDA: pincel solapado (×1.42) para la coherencia
                    // de la llama — las celdas ALTAS se estiran un poco más
                    // (las puntas de fuego se alargan).
                    float sy = Cell * 1.42f * (1f + 0.45f * (y / (float)H) * t);
                    float sx = Cell * 1.42f * (1f - 0.18f * (y / (float)H));

                    Vector2 pos = origin + new Vector2(
                        (x + 0.5f) * Cell + lean * leanF,
                        (y + 0.5f) * Cell);

                    // EL ALPHA: la masa arde SÓLIDA abajo y se desvanece arriba
                    // (la punta de la llama es humeante).
                    float a = (0.52f + 0.48f * t) * (1f - 0.35f * (y / (float)H)) * alpha;
                    if (a <= 0.03f) continue;

                    VFXCore.Quad(pos, new Color(c.R, c.G, c.B, (byte)(int)(255f * a)),
                        new Vector2(sx, sy), 0f, Brush);
                }
            }
        }

        /// <summary>
        /// LA PALETA expuesta (para embers/luz coherentes con el fuego).
        /// </summary>
        public static Color ColorAt(int intensity)
            => Pal[Math.Clamp(intensity, 0, 36)];
    }
}
