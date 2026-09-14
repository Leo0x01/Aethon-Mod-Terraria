using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Effects.Bruma;
using AethonMod.Content.Particles;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// PyraPalettes — v6.25 — LAS TABLAS DE FUEGO DE LA CASA (37 niveles).
    ///
    /// El color del fuego es una TEMPERATURA muestreada en una RAMPA 1D
    /// (lección de la investigación interna: una rampa por material; aquí las tablas son
    /// CÓDIGO, no PNG — el equivalente numérico de las LUT de textura
    /// 1×N con filtro LINEAR del ecosistema, con interpolación LINEAL
    /// entre niveles). Tres materiales:
    ///   · SolarFire — el fuego noble: negro→granate→naranja→oro→blanco.
    ///   · ColdFire  — el fuego frío: azul profundo→cian→blanco.
    ///   · VoidFire  — el fuego maldito: verde→lima→blanco.
    /// </summary>
    public static class PyraPalettes
    {
        /// <summary>Niveles de temperatura de las tablas (0..36 — formato fuego de difusión).</summary>
        public const int Niveles = 37;

        /// <summary>Fuego noble: negro→granate→naranja→oro→blanco (37 colores).</summary>
        public static readonly Color[] SolarFire = Build(
            (0, new Color(0, 0, 0)),
            (7, new Color(60, 0, 22)),
            (12, new Color(130, 12, 42)),
            (18, new Color(255, 138, 60)),
            (26, new Color(255, 195, 85)),
            (32, new Color(255, 240, 200)),
            (36, new Color(255, 252, 240)));

        /// <summary>Fuego frío: azul profundo→cian→blanco (para soles/arcos fríos).</summary>
        public static readonly Color[] ColdFire = Build(
            (0, new Color(0, 0, 0)),
            (7, new Color(2, 10, 48)),
            (12, new Color(0, 60, 140)),
            (18, new Color(0, 130, 215)),
            (26, new Color(80, 210, 255)),
            (32, new Color(190, 242, 255)),
            (36, new Color(245, 253, 255)));

        /// <summary>Fuego maldito: verde→lima→blanco (mirra/abyss).</summary>
        public static readonly Color[] VoidFire = Build(
            (0, new Color(0, 0, 0)),
            (7, new Color(0, 38, 20)),
            (12, new Color(8, 110, 55)),
            (18, new Color(20, 170, 80)),
            (26, new Color(120, 230, 120)),
            (32, new Color(215, 255, 215)),
            (36, new Color(245, 255, 245)));

        /// <summary>Muestreo de la tabla por temperatura 0..1 (interpolación LINEAL).</summary>
        public static Color Sample(Color[] ramp, float temperature)
        {
            if (ramp == null || ramp.Length == 0) return Color.White;
            temperature = MathHelper.Clamp(temperature, 0f, 1f);
            float t = temperature * (ramp.Length - 1);
            int i = Math.Min((int)t, ramp.Length - 2);
            return Color.Lerp(ramp[i], ramp[i + 1], t - i);
        }

        /// <summary>Construye la tabla de 37 niveles desde puntos de control.</summary>
        private static Color[] Build(params (int nivel, Color color)[] keys)
        {
            var res = new Color[Niveles];
            for (int k = 0; k < keys.Length - 1; k++)
            {
                (int n0, Color c0) = keys[k];
                (int n1, Color c1) = keys[k + 1];
                for (int n = n0; n <= n1; n++)
                {
                    float f = n1 > n0 ? (n - n0) / (float)(n1 - n0) : 0f;
                    res[n] = Color.Lerp(c0, c1, f);
                }
            }
            res[Niveles - 1] = keys[keys.Length - 1].color;
            return res;
        }
    }

    /// <summary>
    /// PyraLib — v6.25 — LA LIBRERÍA DEL FUEGO.
    ///
    /// Nació del análisis de huecos de v6.25 (research/humo_v625/
    /// ANALISIS_HUECOS.md): la técnica fuego de difusión de la envoltura v6.22 se
    /// PERDIÓ en la purga — el fuego era deuda de librería. Tres motores,
    /// un contrato:
    ///
    ///   · RAMP: el color es una TEMPERATURA muestreada en tabla (las
    ///     PyraPalettes de la casa — 37 niveles, colores propios).
    ///   · LENGUAS: la llama es una columna de quads con temperatura que
    ///     SUBE hacia la base y una PUNTA que vaga (dos senos
    ///     inconmensurables con reversión), parpadeo de altura 0.85..1.15
    ///     (7.1/17.3 rad/s — jamais en fase), erosión de ruido (la llama
    ///     se DESGARRA como el humo — BrumaNoise.Erode) y viento opcional.
    ///   · CAMPO: la rejilla de brasas con PROPAGACIÓN (técnica fuego de difusión
    ///     validada en v6.22: cada celda enfría 1 nivel al subir y vaga
    ///     ±1 columna por hash determinista) — para zonas de fuego
    ///     persistente; simulación a 30 Hz, celdas con temp ≤ 3 mueren,
    ///     cap de 600 celdas activas.
    ///
    /// CONTRATO (idéntico al de BrumaFX): los métodos dibujan en el lote
    /// ABIERTO que el llamador tenga (ADITIVO para fuego LUMINOSO — el
    /// normal; alfa para brasas que OCLUYEN). TODO determinista por
    /// semilla: misma secuencia SIEMPRE, cero red, cero GC por frame.
    /// El humo lo pone BrumaFX (composición entre librerías, como hace
    /// el ecosistema: fuego + humo + chispas = la hoguera completa).
    /// </summary>
    public static class PyraLib
    {
        // ==================================================================
        //  TEXTURAS COMPARTIDAS
        // ==================================================================

        private static Asset<Texture2D> _glow;

        /// <summary>El brillo radial suave (los tramos de la lengua/celda).</summary>
        private static Texture2D GlowTex =>
            (_glow ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow")).Value;

        /// <summary>Tinte premultiplicado de la casa (v6.25).</summary>
        private static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color(
                (byte)(int)(c.R * f), (byte)(int)(c.G * f), (byte)(int)(c.B * f),
                (byte)(int)(255f * f));
        }

        /// <summary>Hash determinista [0,1).</summary>
        private static float H01(int seed, int a, int b)
        {
            int h = unchecked(seed * 374761393 + a * 668265263 + b * 1911520717);
            h = unchecked(h ^ (h >> 13));
            h = unchecked(h * 1274126177);
            h = unchecked(h ^ (h >> 16));
            return (h & 0xFFFFFF) / 16777216f;
        }

        // ==================================================================
        //  LA LENGUA — la unidad de llama
        // ==================================================================

        /// <summary>
        /// DIBUJA UNA LENGUA de fuego de <paramref name="height"/> px anclada
        /// en <paramref name="basePos"/> (crece hacia ARRIBA en espacio
        /// local, girada por <paramref name="rot"/> — la llama de una punta
        /// que vuela apunta CONTRA su marcha): temperatura MÁXIMA en la
        /// base, decreciente hacia la punta con EROSIÓN de ruido (la llama
        /// se desgarra en grumos como el humo), la PUNTA vaga (dos senos
        /// inconmensurables con reversión a la media), el parpadeo de
        /// altura 0.85..1.15, y triple capa: velo ×1.7 alpha 0.35 · cuerpo
        /// alpha 0.70 · NÚCLEO blanco en el 40% inferior (donde el fuego
        /// es más caliente).
        /// </summary>
        /// <param name="batch">Batch ABIERTO (aditivo recomendado).</param>
        /// <param name="temperature">0..1 — nivel base de la tabla.</param>
        /// <param name="wind">Px/s de empuje lateral (0 = sin viento).</param>
        /// <param name="gravDir">1 normal · −1 mundo invertido.</param>
        /// <param name="rot">Rotación de la lengua alrededor de la base
        /// (rad — 0 = crece hacia arriba).</param>
        public static void Tongue(SpriteBatch batch, Vector2 basePos, float height,
            float width, Color[] ramp, float temperature, int seed, float time,
            float intensity = 1f, float wind = 0f, float gravDir = 1f, float rot = 0f)
        {
            if (batch == null || height < 3f || width < 1f || ramp == null) return;
            temperature = MathHelper.Clamp(temperature, 0f, 1f);
            intensity = MathHelper.Clamp(intensity, 0f, 1f);
            if (intensity <= 0.02f) return;

            Texture2D tex = GlowTex;
            if (tex == null) return;

            // === EL PARPADEO de altura (dos senos INCONMENSURABLES). ===
            float flick = 1f + 0.15f * MathF.Sin(time * 7.1f + seed) +
                          0.05f * MathF.Sin(time * 17.3f + seed * 2);
            float hEff = height * flick;

            const int Tramos = 8;
            float paso = hEff / Tramos;

            for (int i = 0; i < Tramos; i++)
            {
                float f = (i + 0.5f) / Tramos;   // 0 base → 1 punta

                // --- LA PUNTA VAGA: dos senos inconmensurables con la
                //     amplitud DECRECIENTE hacia la punta (reversión a la
                //     media — el filamento que se endereza). ---
                float vag1 = MathF.Sin(f * 2.4f + time * 1.7f + seed) * width * 0.22f;
                float vag2 = MathF.Sin(f * 5.1f - time * 2.9f + seed * 2) * width * 0.10f;
                float x = (vag1 + vag2) * (0.4f + 0.6f * f) + wind * f * f * 0.10f;

                // --- La anchura: barriga al 40%, PUNTA fina. ---
                float w = width * MathF.Pow(MathF.Sin(f * MathHelper.Pi + 0.3f), 0.6f);
                w = MathF.Max(w, width * 0.08f);

                // --- LA TEMPERATURA: máxima en la base, muriendo hacia la
                //     punta, DESGARRADA por el ruido (la llama EROSIONA). ---
                float temp = temperature * (1f - f * 0.55f);
                float n = BrumaNoise.Fbm(f * 3.1f + seed * 0.13f, time * 0.9f, seed, 2);
                temp *= 0.55f + 0.45f * (n * 2f);   // el ruido decide qué arde

                Color col = PyraPalettes.Sample(ramp, MathHelper.Clamp(temp, 0f, 1f));

                // --- El latido del tramo (la energía SUBE). ---
                float beat = 0.85f + 0.15f * MathF.Sin(time * 8.3f - f * 5.1f + seed);

                // --- La posición LOCAL (arriba = −Y) girada por rot. ---
                Vector2 pos = basePos + new Vector2(
                    x, -f * hEff * gravDir).RotatedBy(rot);
                Vector2 size = new(w * 2f, paso * 1.6f);

                // CAPA 1 — EL VELO (×1.7 de ancho, alpha 0.35).
                batch.Draw(tex, pos, null, Tint(col, 0.35f * beat * intensity), 0f,
                    new Vector2(tex.Width, tex.Height) * 0.5f,
                    new Vector2(size.X * 1.7f, size.Y) / new Vector2(tex.Width, tex.Height),
                    SpriteEffects.None, 0f);

                // CAPA 2 — EL CUERPO (alpha 0.70).
                batch.Draw(tex, pos, null, Tint(col, 0.70f * beat * intensity), 0f,
                    new Vector2(tex.Width, tex.Height) * 0.5f,
                    size / new Vector2(tex.Width, tex.Height),
                    SpriteEffects.None, 0f);

                // CAPA 3 — EL NÚCLEO BLANCO (solo el 40% inferior: ahí el
                // fuego está MÁS CALIENTE).
                if (f < 0.4f)
                {
                    Color nucleo = PyraPalettes.Sample(ramp, 1f);
                    batch.Draw(tex, pos, null, Tint(nucleo, 0.90f * (1f - f / 0.4f) * intensity), 0f,
                        new Vector2(tex.Width, tex.Height) * 0.5f,
                        new Vector2(size.X * 0.35f, size.Y) / new Vector2(tex.Width, tex.Height),
                        SpriteEffects.None, 0f);
                }
            }
        }

        /// <summary>
        /// RACIMO DE 3-5 LENGUAS (una hoguera, un estallido): cada lengua
        /// con su semilla, altura (0.55..1.0×) y temperatura propia — la
        /// masa al centro, las lenguas laterales INCLINADAS hacia fuera.
        /// </summary>
        public static void Flame(SpriteBatch batch, Vector2 center, float radius,
            Color[] ramp, int seed, float time, float intensity = 1f, float gravDir = 1f)
        {
            if (batch == null || radius < 2f) return;

            int lenguas = 4;
            for (int l = 0; l < lenguas; l++)
            {
                float h1 = H01(seed, 800 + l, 31);
                float h2 = H01(seed, 810 + l, 37);
                float h3 = H01(seed, 820 + l, 41);

                // La lengua central ALTA y caliente; las laterales bajas,
                // inclinadas hacia fuera (el abanico natural de la hoguera).
                float esLateral = l == 0 ? 0f : 1f;
                Vector2 basePos = center + new Vector2(
                    (h1 - 0.5f) * radius * 1.6f * esLateral,
                    radius * 0.25f * esLateral * gravDir);

                float height = radius * (0.55f + 0.45f * h2) * (l == 0 ? 1.15f : 0.9f);
                float width = radius * (0.24f + 0.16f * h3);
                float temp = l == 0 ? 0.95f : 0.55f + 0.35f * h2;

                // El viento local de cada lengua (el abanico respira).
                float wind = (h1 - 0.5f) * radius * 0.8f;

                Tongue(batch, basePos, height, width, ramp, temp,
                    seed + l * 61, time, intensity, wind, gravDir);
            }
        }

        // ==================================================================
        //  EL CAMPO DE BRASAS — la propagación fuego de difusión (determinista)
        // ==================================================================

        // El estado por semilla: [y·w + x] = temperatura 0..36.
        private static readonly Dictionary<int, sbyte[]> _campos = new();
        private static readonly Dictionary<int, int[]> _dims = new();
        private static readonly Dictionary<int, uint> _ultimoToque = new();

        /// <summary>Celdas activas máximas dibujadas por campo (el cap de la casa).</summary>
        private const int CapCeldas = 600;

        /// <summary>El tick de la última simulación (30 Hz — un sí un no).</summary>
        private static uint _simTick;

        /// <summary>
        /// DIBUJA UN CAMPO DE BRASAS de <paramref name="w"/>×<paramref name="h"/>
        /// celdas (celda = <paramref name="cellSize"/> px) con esquina inferior
        /// en <paramref name="basePos"/>: rejilla de temperaturas 0..36 que
        /// PROPAGA hacia arriba (cada celda toma el valor de abajo−1 con
        /// desfase lateral ±1 por hash determinista — la técnica PSX
        /// clásica, SIN textura dinámica: cada celda ES un quad pequeño
        /// con el SoftGlow y el color de la tabla). Simulación a 30 Hz,
        /// celdas con temp ≤ 3 mueren, cap 600 activas.
        /// Los FOCOS los enciende el llamador con SeedCell(...) en su AI.
        /// </summary>
        /// <param name="basePos">Esquina INFERIOR-IZQUIERDA del campo (espacio del lote).</param>
        public static void EmberField(SpriteBatch batch, Vector2 basePos, int w, int h,
            float cellSize, Color[] ramp, int seed, float time, float intensity = 1f,
            float gravDir = 1f)
        {
            if (batch == null || w < 4 || h < 4 || cellSize < 2f) return;
            Texture2D tex = GlowTex;
            if (tex == null) return;

            // --- El campo (crecimiento/realineación por semilla). ---
            if (!_campos.TryGetValue(seed, out sbyte[] campo) ||
                !_dims.TryGetValue(seed, out int[] dims) ||
                dims[0] != w || dims[1] != h)
            {
                campo = new sbyte[w * h];
                _campos[seed] = campo;
                _dims[seed] = new[] { w, h };
                _ultimoToque[seed] = Main.GameUpdateCount;
            }
            _ultimoToque[seed] = Main.GameUpdateCount;

            // --- LA SIMULACIÓN a 30 Hz (un tick sí un no — imperceptible
            //     a 60 fps de dibujado, mitad de coste). ---
            uint now = Main.GameUpdateCount;
            if (now / 2u != _simTick)
            {
                _simTick = now / 2u;
                Simular(campo, w, h, seed);
            }

            // --- EL RENDER: solo celdas VIVAS (temp > 3), hasta el cap. ---
            int dibujadas = 0;
            float pulse = 0.9f + 0.1f * MathF.Sin(time * 6.2f + seed);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int temp = campo[y * w + x];
                    if (temp <= 3) continue;
                    if (dibujadas >= CapCeldas) break;

                    Color col = PyraPalettes.Sample(ramp, temp / 36f);
                    // Las brasas PARPADEAN por celda (fase propia por hash).
                    float celPulse = 0.8f + 0.2f *
                        MathF.Sin(time * (5f + 3f * H01(seed, x, y)) + H01(seed, y, x) * 6.28f);

                    Vector2 pos = new(
                        basePos.X + (x + 0.5f) * cellSize,
                        basePos.Y - (y + 0.5f) * cellSize * gravDir);

                    batch.Draw(tex, pos, null,
                        Tint(col, 0.5f * pulse * celPulse * intensity), 0f,
                        new Vector2(tex.Width, tex.Height) * 0.5f,
                        new Vector2(cellSize * 1.6f, cellSize * 1.6f) / new Vector2(tex.Width, tex.Height),
                        SpriteEffects.None, 0f);
                    dibujadas++;
                }
                if (dibujadas >= CapCeldas) break;
            }

            // --- LA AUTOLIMPIEZA: campos sin tocar 600 ticks (10 s) mueren. ---
            if (_campos.Count > 4)
            {
                List<int> muertos = null;
                foreach (KeyValuePair<int, uint> kv in _ultimoToque)
                {
                    if (now > kv.Value + 600u)
                    {
                        muertos ??= new List<int>();
                        muertos.Add(kv.Key);
                    }
                }
                if (muertos != null)
                    for (int i = 0; i < muertos.Count; i++)
                    {
                        _campos.Remove(muertos[i]);
                        _dims.Remove(muertos[i]);
                        _ultimoToque.Remove(muertos[i]);
                    }
            }
        }

        /// <summary>Enciende la celda (x,y) del campo por semilla (el FOCO).</summary>
        public static void SeedCell(int seed, int x, int y, int temperature)
        {
            if (!_campos.TryGetValue(seed, out sbyte[] campo) ||
                !_dims.TryGetValue(seed, out int[] dims)) return;
            if (x < 0 || y < 0 || x >= dims[0] || y >= dims[1]) return;
            campo[y * dims[0] + x] = (sbyte)Math.Clamp(temperature, 0, 36);
        }

        /// <summary>EL PASO DE PROPAGACIÓN (fuego de difusión determinista por hash).</summary>
        private static void Simular(sbyte[] campo, int w, int h, int seed)
        {
            // De ABAJO hacia ARRIBA: cada celda hereda del vecino inferior
            // −1 nivel de frío, con vagancia lateral ±1 por hash (¡determinista!).
            for (int y = 0; y < h - 1; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int temp = campo[(y + 1) * w + x];
                    if (temp <= 0) continue;

                    // La vagancia: hacia qué columna sube el calor.
                    int off = (int)(H01(seed, x, y) * 3f) - 1;   // {-1, 0, +1}
                    int nx = x + off;
                    if (nx < 0) nx = 0;
                    if (nx >= w) nx = w - 1;

                    int nueva = temp - 1;

                    if (nueva > campo[y * w + nx])
                        campo[y * w + nx] = (sbyte)nueva;
                }
            }
        }

        /// <summary>Vacía TODOS los campos (descarga limpia).</summary>
        public static void ClearFields()
        {
            _campos.Clear();
            _dims.Clear();
            _ultimoToque.Clear();
        }

        // ==================================================================
        //  LAS CHISPAS / ASCUAS — física de verdad (paquete determinista)
        // ==================================================================

        /// <summary>
        /// PAQUETE DE CHISPAS determinista (NO spawnea: DEVUELVE el array
        /// listo para ParticleManager.Spawn): ascuas con GRAVEDAD suave
        /// (0.12 — las ascuas FLOTAN más que las piedras), vida 30-90
        /// ticks, color = la tabla por temperatura decreciente... (la
        /// ascua se APAGA: blanco→naranja→rojo→extinta) vía ColorShift.
        /// </summary>
        /// <param name="burstDir">Dirección del chorro (Vector2.Zero = radial).</param>
        public static void Sparks(Vector2 center, Vector2 burstDir, int count,
            Color[] ramp, int seed, out ParticleData[] outParticles)
        {
            outParticles = null;
            if (count <= 0 || Main.netMode == NetmodeID.Server) return;

            outParticles = new ParticleData[count];
            for (int k = 0; k < count; k++)
            {
                float h1 = H01(seed, 900 + k, 43);
                float h2 = H01(seed, 910 + k, 47);
                float h3 = H01(seed, 920 + k, 53);

                // La dirección: el chorro si lo hay, si no RADIAL completa.
                Vector2 dir = burstDir.LengthSquared() > 0.01f
                    ? Vector2.Normalize(burstDir)
                    : new Vector2(MathF.Cos(h1 * MathHelper.TwoPi), MathF.Sin(h1 * MathHelper.TwoPi));
                // El cono: ±0.9 rad alrededor de la dirección.
                dir = dir.RotatedBy((h2 - 0.5f) * 1.8f);

                // La temperatura NACE alta y muere: ColorShift la cuenta.
                Color nace = PyraPalettes.Sample(ramp, 0.85f + 0.15f * h3);
                Color muere = PyraPalettes.Sample(ramp, 0.30f * h3);
                int vida = (int)(30 + 60 * h1);

                var p = new ParticleData
                {
                    Position = center,
                    Velocity = dir * (1.5f + 4.5f * h2),
                    Scale = Vector2.One * (0.5f + 0.8f * h3),
                    PackedColor = ParticleManager.PackColor(nace),
                    PackedStartColor = ParticleManager.PackColor(nace),
                    PackedEndColor = ParticleManager.PackColor(muere),
                    TimeLeft = vida,
                    Duration = vida,
                    TextureId = ParticleTex.SoftGlow,
                    BlendMode = 1,   // aditivo
                    LayerPriority = LayerPriorities.BeforeProjectiles,
                    UserData0 = 0.12f,   // gravedad suave (las ascuas flotan)
                };
                p.EnableComponent(ComponentFlag.FadeOut);
                p.EnableComponent(ComponentFlag.Gravity);
                p.EnableComponent(ComponentFlag.ColorShift);
                p.EnableComponent(ComponentFlag.EmitLight);
                outParticles[k] = p;
            }
        }

        // ==================================================================
        //  LA LUZ — el fuego ilumina
        // ==================================================================

        /// <summary>
        /// LUZ DE MUNDO del fuego: el color de la tabla a intensidad 0.85,
        /// muestreada en <paramref name="pos"/> (coordenadas de MUNDO).
        /// </summary>
        public static void Light(Vector2 worldPos, float radius, Color[] ramp,
            float temperature, float strength = 1f)
        {
            if (Main.netMode == NetmodeID.Server) return;
            Color c = PyraPalettes.Sample(ramp, temperature);
            float r = radius / 16f * 0.85f * strength;   // px → tiles
            Lighting.AddLight(worldPos,
                new Vector3(c.R / 255f, c.G / 255f, c.B / 255f) * MathHelper.Clamp(r, 0.1f, 3f));
        }
    }
}
