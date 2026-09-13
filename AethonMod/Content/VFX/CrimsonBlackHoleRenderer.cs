using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// CrimsonBlackHoleRenderer — v6.11 — EL AGUJERO NEGRO "OBLIVION".
    ///
    /// La referencia ORIGINAL del usuario (Reddit: Ancients Awakened —
    /// Regicide, Oblivion God of the Void): VÓRTICE DE PLASMA carmesí con
    /// esfera negra compacta + GAP, anillo interior 360° a ~1.5R, UNA HOJA
    /// GRUESA EN CRESCIENTE que barre por ARRIBA (O→NO→N→NNE) con aguja
    /// hasta ~5.9R, un cresiente BAJO (ESE→S→SSW) hasta ~6.3R, todo
    /// INCLINADO (SW→NE ~24°) girando HORARIO, hotspot blanco-amarillo,
    /// chispas y rayos azul-violeta ramificados dentro de la esfera.
    ///
    /// v6.11 — FIX DEL VÓRTICE INVISIBLE (el reporte del usuario: "es solo
    /// un agujero, no se parece en nada a la referencia"): el v6.10 dibujaba
    /// SoftGlow con (len, wid) como TAMAÑO TOTAL del quad, pero esos números
    /// son las SIGMAS gaussianas del prototipo (add_blob: visible hasta
    /// ~1.5×sigma) y SoftGlow concentra su brillo en un núcleo diminuto →
    /// cada cápsula brillaba en 2-3px y el vórtice entero era microscópico.
    /// Además el blending aditivo (SourceAlpha, One) multiplicaba el alfa
    /// DOS veces (color premultiplicado × alfa del color) → alfa² → aún más
    /// tenue; y KeyLerp asumía tablas equiespaciadas cuando el prototipo
    /// calibrado usa t-claves explícitas (0.25, 0.45, 0.65…).
    ///
    /// EL MÉTODO NUEVO (calibrado 1:1 contra el prototipo validado con la
    /// referencia, EMA 27/255):
    ///   · Textura OblivionBlob.png: perfil gaussiano EXACTO del prototipo
    ///     horneado en el RGB (g = (exp(-3.6d²)+0.4·exp(-1.2d²))·ventana),
    ///     alpha=255 en toda la textura → el premultiply de tML no la toca
    ///     y el aditivo queda LINEAL en el perfil.
    ///   · Cap/Quad dibujan el quad con TAMAÑO TOTAL = (2·sigma, 2·sigma):
    ///     d = distancia/sigma reproduce add_blob píxel a píxel.
    ///   · Color con alfa 255 y brillo m=alfa·1.4 en el RGB (clampeado a
    ///     255 — el mismo clip del acumulador del prototipo).
    ///   · KeyLerp con T-CLAVES explícitas (idéntico al smooth() del
    ///     prototipo) y KeyColor lineal entre claves (idéntico color_ramp).
    ///
    /// CONTRATO DE BATCH (v6.10, a prueba de balas): Draw() exige el
    /// SpriteBatch CERRADO y lo deja CERRADO. Nunca llama End() sobre un
    /// batch abierto — el llamador (PreDraw) cierra el suyo y lo restaura
    /// con los parámetros EXACTOS de Main.DrawProjectiles.
    /// </summary>
    public static class CrimsonBlackHoleRenderer
    {
        // ==================================================================
        //  PARÁMETROS CALIBRADOS (×R = radio de la esfera negra)
        // ==================================================================

        /// <summary>Radio de la esfera negra en px a escala 1 — GIGANTE.</summary>
        public const float SpherePx = 46f;

        /// <summary>Aplastado vertical del vórtice (perspectiva).</summary>
        private const float Squash = 0.88f;

        /// <summary>Inclinación global SW→NE del vórtice (radianes).</summary>
        private const float Tilt = -0.42f;

        /// <summary>Velocidad de rotación del vórtice (rad/s, horario).</summary>
        private const float SwirlSpeed = 0.16f;

        // ---------------- HOJA SUPERIOR (por ARRIBA) ------------------------
        // Tablas con T-CLAVES explícitas — IGUAL que el prototipo.
        private static readonly float[] BladeT = { 0.00f, 1.00f };
        private static readonly float[] BladeTh = { 175f, 352f };      // grados: O→NO→N→NNE
        private static readonly float[] BladeRoT = { 0.00f, 0.25f, 0.45f, 0.65f, 0.80f, 0.90f, 1.00f };
        private static readonly float[] BladeRo = { 2.30f, 3.30f, 3.00f, 3.30f, 3.20f, 4.20f, 5.90f };
        private static readonly float[] BladeTkT = { 0.00f, 0.30f, 0.55f, 0.80f, 1.00f };
        private static readonly float[] BladeTk = { 1.00f, 0.80f, 0.55f, 0.32f, 0.07f };
        private static readonly float[] BladeBriT = { 0.00f, 0.30f, 0.72f, 0.86f, 1.00f };
        private static readonly float[] BladeBri = { 0.60f, 0.80f, 1.00f, 0.78f, 0.52f };
        private static readonly float[] BladeColT = { 0.00f, 0.30f, 0.55f, 0.72f, 0.84f, 0.93f, 1.00f };
        private static readonly Color[] BladeCol =
        {
            new Color(255, 42, 122), new Color(255, 80, 160),
            new Color(255, 150, 205), new Color(255, 250, 155),
            new Color(255, 110, 200), new Color(222, 38, 98),
            new Color(145, 18, 48)
        };

        // ---------------- CRESCIENTE INFERIOR (por DEBAJO) ------------------
        private static readonly float[] LowerT = { 0.00f, 1.00f };
        private static readonly float[] LowerTh = { 20f, 205f };       // ESE→S→SSW
        private static readonly float[] LowerRoT = { 0.00f, 0.25f, 0.50f, 0.65f, 0.80f, 1.00f };
        private static readonly float[] LowerRo = { 1.95f, 3.20f, 6.30f, 6.00f, 4.80f, 2.60f };
        private static readonly float[] LowerTkT = { 0.00f, 0.35f, 0.60f, 0.85f, 1.00f };
        private static readonly float[] LowerTk = { 0.65f, 0.50f, 0.35f, 0.18f, 0.08f };
        private static readonly float[] LowerBriT = { 0.00f, 0.30f, 0.50f, 0.75f, 1.00f };
        private static readonly float[] LowerBri = { 0.60f, 0.78f, 0.82f, 0.50f, 0.22f };
        private static readonly float[] LowerColT = { 0.00f, 0.30f, 0.50f, 0.75f, 1.00f };
        private static readonly Color[] LowerCol =
        {
            new Color(255, 60, 140), new Color(255, 82, 172),
            new Color(250, 45, 125), new Color(222, 36, 100),
            new Color(140, 18, 55)
        };

        // ==================================================================
        //  TEXTURAS
        // ==================================================================

        private static Asset<Texture2D> _blob;
        private static Asset<Texture2D> _blackDisk;

        /// <summary>El blob gaussiano del prototipo (perfil en RGB, alfa 255).</summary>
        private static Texture2D Blob =>
            (_blob ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/OblivionBlob")).Value;

        private static Texture2D BlackDisk =>
            (_blackDisk ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/BlackDisk")).Value;

        // ==================================================================
        //  HELPERS
        // ==================================================================

        /// <summary>smoothstep clampeado.</summary>
        private static float SmoothStep(float e0, float e1, float x)
        {
            float t = MathHelper.Clamp((x - e0) / (e1 - e0), 0f, 1f);
            return t * t * (3f - 2f * t);
        }

        /// <summary>Interpolación por claveframes con T-CLAVES explícitas y
        /// smoothstep entre tramos — IGUAL al smooth() del prototipo.</summary>
        private static float KeyLerp(float[] ts, float[] vs, float t)
        {
            int n = ts.Length;
            if (t <= ts[0]) return vs[0];
            if (t >= ts[n - 1]) return vs[n - 1];
            for (int i = 0; i < n - 1; i++)
            {
                if (t >= ts[i] && t <= ts[i + 1])
                    return MathHelper.Lerp(vs[i], vs[i + 1],
                        SmoothStep(ts[i], ts[i + 1], t));
            }
            return vs[n - 1];
        }

        /// <summary>Interpolación de COLOR lineal entre claveframes con
        /// t-claves explícitas — IGUAL al color_ramp() del prototipo.</summary>
        private static Color KeyColor(float[] ts, Color[] vs, float t)
        {
            int n = ts.Length;
            if (t <= ts[0]) return vs[0];
            if (t >= ts[n - 1]) return vs[n - 1];
            for (int i = 0; i < n - 1; i++)
            {
                if (t >= ts[i] && t <= ts[i + 1])
                    return Color.Lerp(vs[i], vs[i + 1],
                        MathHelper.Clamp((t - ts[i]) / (ts[i + 1] - ts[i]), 0f, 1f));
            }
            return vs[n - 1];
        }

        /// <summary>Hash determinista [0,1).</summary>
        private static float Hash01(int seed, int a, int b)
        {
            int h = unchecked(seed * 374761393 + a * 668265263 + b * 1911520717);
            h ^= h >> 13;
            h = unchecked(h * 1274126177);
            h ^= h >> 16;
            return (h & 0xFFFFFF) / 16777216f;
        }

        /// <summary>Posición de un punto polar (rr en unidades de R) con
        /// aplastado e inclinación globales — la MISMA del prototipo.</summary>
        private static Vector2 Pol(float rr, float theta, float r, Vector2 center)
        {
            float x = (float)Math.Cos(theta) * rr * r;
            float y = (float)Math.Sin(theta) * rr * r * Squash;
            float ca = (float)Math.Cos(Tilt), sa = (float)Math.Sin(Tilt);
            return center + new Vector2(x * ca - y * sa, x * sa + y * ca);
        }

        /// <summary>
        /// Cápsula elíptica aditiva — v6.11: (len, wid) son las SIGMAS
        /// gaussianas del prototipo → el quad se dibuja con TAMAÑO TOTAL
        /// (2·len, 2·wid) para que el perfil del blob reproduzca add_blob.
        /// El color lleva el brillo m=alfa·1.4 en el RGB (con alfa 255):
        /// blending aditivo LINEAL, sin el alfa² del v6.10.
        /// </summary>
        private static void Cap(Vector2 pos, float len, float wid, float rot, Color c, float alpha)
        {
            if (alpha <= 0.004f) return;
            Texture2D tex = Blob;
            Vector2 texSize = new Vector2(tex.Width, tex.Height);
            float m = alpha * 1.4f;
            var tint = new Color(
                (byte)Math.Min(255f, c.R * m),
                (byte)Math.Min(255f, c.G * m),
                (byte)Math.Min(255f, c.B * m), 255);
            Main.spriteBatch.Draw(tex, pos, null, tint, rot,
                texSize * 0.5f, new Vector2(len * 2f, wid * 2f) / texSize,
                SpriteEffects.None, 0f);
        }

        /// <summary>Cuadro suave estirado/rotado (sigmas como Cap).</summary>
        private static void Quad(Vector2 pos, Vector2 sigma, float rot, Color c, float alpha)
            => Cap(pos, sigma.X, sigma.Y, rot, c, alpha);

        private static void BeginAdditive()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
        }

        private static void BeginAlpha()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
        }

        // ==================================================================
        //  EL RENDER COMPLETO — contrato: batch CERRADO → CERRADO
        // ==================================================================

        public static void Draw(Vector2 center, float scale, float time, int seed)
        {
            float r = SpherePx * Math.Max(scale, 0.02f);
            if (r < 2f) return;   // el batch queda INTACTO (cerrado)

            float rot = time * SwirlSpeed;   // giro HORARIO del vórtice

            try
            {
                // ============ 1. HALO AMBIENTE (cálido, inclinado) ============
                Quad(center, new Vector2(5.6f * r, 3.7f * r * Squash), Tilt,
                    new Color(125, 18, 55), 0.13f);
                Quad(center, new Vector2(3.3f * r, 2.3f * r * Squash), Tilt,
                    new Color(185, 36, 85), 0.12f);

                // ============ 2. ANILLO INTERIOR 360° + rim caliente ============
                const int NRing = 48;
                for (int i = 0; i < NRing; i++)
                {
                    float phi = i * MathHelper.TwoPi / NRing;
                    Vector2 pos = Pol(1.48f, phi, r, center);
                    float rotA = (float)Math.Atan2((float)Math.Cos(phi) * Squash, -(float)Math.Sin(phi)) + Tilt;
                    float ang = phi * (180f / (float)Math.PI);
                    float merge = 0.5f + 0.5f * (float)Math.Cos(MathHelper.ToRadians(ang - 95f + rot * (180f / (float)Math.PI)));
                    float brillo = 0.46f + 0.40f * Math.Max(0f, merge);
                    float flick = 0.86f + 0.14f * (float)Math.Sin(9f * phi + time * 6f + i * 2.3f);
                    Color col = Math.Sin(phi) < 0 ? new Color(255, 66, 142) : new Color(255, 40, 108);
                    Cap(pos, 0.52f * r, 0.26f * r, rotA, col, brillo * flick * 0.72f);
                    // borde interno BLANCO-CALIENTE (abraza el gap)
                    Vector2 hotPos = Pol(1.26f, phi, r, center);
                    float hot = 0.38f + 0.50f * Math.Max(0f, merge);
                    Cap(hotPos, 0.40f * r, 0.16f * r, rotA, new Color(255, 238, 198),
                        hot * flick * 0.45f);
                }

                // ============ 3. LAS DOS HOJAS DEL VÓRTICE ============
                DrawBlade(center, r, rot, time, seed,
                    BladeT, BladeTh, BladeRoT, BladeRo, BladeTkT, BladeTk,
                    BladeBriT, BladeBri, BladeColT, BladeCol, 112, 1.0f, 0.30f);
                DrawBlade(center, r, rot, time, seed,
                    LowerT, LowerTh, LowerRoT, LowerRo, LowerTkT, LowerTk,
                    LowerBriT, LowerBri, LowerColT, LowerCol, 72, 0.65f, 0.22f);

                // ============ 4. HOTSPOT + nudo NNE + aguja + mechones ============
                Vector2 hx = BladePoint(BladeT, BladeTh, BladeRoT, BladeRo, BladeTkT, BladeTk,
                    0.72f, rot, r, center, out float hTh);
                Cap(hx, 1.5f * r, 0.85f * r, hTh, new Color(255, 240, 168), 0.85f);
                Cap(hx, 0.65f * r, 0.40f * r, hTh, new Color(255, 253, 232), 1.10f);
                // nudo caliente NNE a 3R (medido: (255,246,137) a 355°, 3R)
                Vector2 knot = Pol(3.0f, MathHelper.ToRadians(355f), r, center);
                Cap(knot, 0.55f * r, 0.34f * r, MathHelper.ToRadians(355f), new Color(255, 246, 150), 0.55f);
                // chispa de la aguja
                Vector2 tip = BladePoint(BladeT, BladeTh, BladeRoT, BladeRo, BladeTkT, BladeTk,
                    0.97f, rot, r, center, out float tTh);
                Cap(tip, 0.4f * r, 0.14f * r, tTh, new Color(255, 210, 170), 0.5f);
                // mechones lejanos NNE (hasta 6.5R)
                for (int wI = 0; wI < 3; wI++)
                {
                    float wTh = MathHelper.ToRadians(352f + wI * 9f) + rot;
                    for (int s = 0; s < 3; s++)
                    {
                        float wr = 5.9f + s * 0.35f + 0.2f * Hash01(wI, s, 3);
                        Vector2 wp = Pol(wr, wTh, r, center);
                        Color wc = Color.Lerp(new Color(200, 45, 95), new Color(110, 18, 48), s / 2f);
                        Cap(wp, 0.45f * r * (1f - s * 0.2f), 0.10f * r, wTh, wc, 0.50f - s * 0.12f);
                    }
                }
                Main.spriteBatch.End();

                // ============ 5. LA ESFERA NEGRA (come la luz) ============
                // AlphaBlend + Color.Black = NEGRO PURO opaco: garantiza el GAP.
                BeginAlpha();
                Main.spriteBatch.Draw(BlackDisk, center, null, Color.Black, 0f,
                    BlackDisk.Size() * 0.5f,
                    new Vector2(2f * r, 2f * r) / new Vector2(BlackDisk.Width, BlackDisk.Height),
                    SpriteEffects.None, 0f);
                Main.spriteBatch.End();

                // ============ 6. RAYOS AZUL-VIOLETA RAMIFICADOS DENTRO ============
                BeginAdditive();
                int frame = (int)(time * 7f);
                for (int bi = 0; bi < 3; bi++)
                {
                    if ((frame + bi * 3) % 5 >= 2) continue;   // ráfagas
                    float th0 = MathHelper.ToRadians(-60f + bi * 130f + (frame * 47) % 360);
                    Vector2 p0 = center + new Vector2((float)Math.Cos(th0), (float)Math.Sin(th0)) * (r * 0.92f);
                    Vector2 p1 = center + new Vector2((float)Math.Cos(th0 + 1.9f), (float)Math.Sin(th0 + 1.9f)) * (r * 0.45f);
                    Vector2 prev = p0;
                    for (int s = 1; s <= 5; s++)
                    {
                        float u = s / 5f;
                        float jx = (Hash01(frame, bi * 10 + s, 11) - 0.5f) * r * 0.40f * (1f - u);
                        float jy = (Hash01(frame, bi * 10 + s, 17) - 0.5f) * r * 0.40f * (1f - u);
                        Vector2 nxt = Vector2.Lerp(p0, p1, u) + new Vector2(jx, jy);
                        Vector2 mid = (prev + nxt) * 0.5f;
                        float ra = (float)Math.Atan2(nxt.Y - prev.Y, nxt.X - prev.X);
                        float ln = Vector2.Distance(prev, nxt) * 0.8f;
                        Cap(mid, Math.Max(ln, 1.5f), 1.3f, ra, new Color(150, 170, 255), 0.30f);
                        // bifurcación corta
                        if ((s == 2 || s == 4) && Hash01(frame, bi * 7 + s, 23) > 0.4f)
                        {
                            float bra = ra + (Hash01(frame, s, 29) - 0.5f) * 1.6f;
                            Vector2 bEnd = mid + new Vector2((float)Math.Cos(bra), (float)Math.Sin(bra)) * (r * 0.20f);
                            Cap((mid + bEnd) * 0.5f, r * 0.12f, 1.1f, bra, new Color(170, 185, 255), 0.32f);
                        }
                        prev = nxt;
                    }
                }

                // ============ 7. CHISPAS blanco-amarillas ============
                float[] sparkT = { 0.72f, 0.55f, 0.82f, 0.30f, 0.62f, 0.90f, 0.12f };
                float[] sparkR = { 3.6f, 2.7f, 4.6f, 2.3f, 3.4f, 5.2f, 2.0f };
                for (int k = 0; k < sparkT.Length; k++)
                {
                    float tw = 0.5f + 0.5f * (float)Math.Sin(time * (5f + k * 1.7f) + k * 2.9f);
                    if (tw < 0.55f) continue;
                    float th = MathHelper.ToRadians(KeyLerp(BladeT, BladeTh, sparkT[k])) + rot;
                    Vector2 sp = Pol(sparkR[k], th, r, center);
                    Cap(sp, 2.6f, 2.6f, 0f, new Color(255, 252, 222), 1.0f * tw);
                    Cap(sp, 5.5f, 5.5f, 0f, new Color(255, 238, 165), 0.34f * tw);
                }

                // ============ 8. BLOOM ============
                Cap(hx, 2.8f * r, 1.8f * r, hTh, new Color(255, 195, 135), 0.20f);
                Quad(center, new Vector2(3.5f * r, 2.6f * r * Squash), Tilt,
                    new Color(255, 115, 185), 0.09f);
                Main.spriteBatch.End();
            }
            catch
            {
                // Cierre defensivo: dejar el batch CERRADO pase lo que pase.
                try { Main.spriteBatch.End(); } catch { }
            }
        }

        /// <summary>Punto central de la sección t de una hoja.</summary>
        private static Vector2 BladePoint(float[] ts, float[] ths, float[] roT, float[] ros,
            float[] tkT, float[] tks, float t, float rot, float r, Vector2 center, out float theta)
        {
            float ro = KeyLerp(roT, ros, t);
            float tk = KeyLerp(tkT, tks, t);
            float rm = ro - tk * 0.5f;
            theta = MathHelper.ToRadians(KeyLerp(ts, ths, t)) + rot;
            return Pol(rm, theta, r, center);
        }

        /// <summary>Una hoja del vórtice: cápsulas + filamentos + colas.</summary>
        private static void DrawBlade(Vector2 center, float r, float rot, float time, int seed,
            float[] ts, float[] ths, float[] roT, float[] ros, float[] tkT, float[] tks,
            float[] briT, float[] bris, float[] colT, Color[] cols,
            int nSeg, float filamentBoost, float jitter)
        {
            for (int i = 0; i < nSeg; i++)
            {
                float t = i / (float)(nSeg - 1);
                float jr = (Hash01(seed, i * 7 + 1, 13) - 0.5f) * jitter;
                Vector2 pos = BladePoint(ts, ths, roT, ros, tkT, tks, t, rot, r, center, out float th);
                float ro = KeyLerp(roT, ros, t);
                float tk = KeyLerp(tkT, tks, t);
                float rm = ro - tk * 0.5f + jr * 0.5f;
                pos = Pol(rm, th, r, center);
                // tangente numérica
                float eps = 1.5f / nSeg;
                Vector2 pos2 = BladePoint(ts, ths, roT, ros, tkT, tks, Math.Min(t + eps, 1f), rot, r, center, out _);
                float rotA = (float)Math.Atan2(pos2.Y - pos.Y, pos2.X - pos.X);
                float segLen = Math.Max(Vector2.Distance(pos, pos2) * 1.55f, 3f);
                Color col = KeyColor(colT, cols, t);
                float bri = KeyLerp(briT, bris, t);
                // trazos de pincel
                float stroke = 0.70f + 0.30f * (float)Math.Sin(21f * t + time * 3.1f + Math.Sin(7.7f * t) * 2f);
                stroke *= 0.90f + 0.10f * Hash01(seed, i * 31 + 5, 77);
                float al = Math.Min(2.1f, bri * stroke * 1.3f);
                Cap(pos, segLen, tk * r * 0.50f, rotA, col, al);

                // filamentos calientes DENTRO de la hoja (pinceladas)
                for (int f = 0; f < 3; f++)
                {
                    float off = (f - 1) * 0.30f * tk;
                    Vector2 fp = Pol(rm + off, th, r, center);
                    Color fcol = Color.Lerp(col, new Color(255, 255, 235), f == 1 ? 0.60f : 0.35f);
                    float fal = al * filamentBoost * (f == 1 ? 0.55f : 0.34f);
                    Cap(fp, segLen * 0.92f, Math.Max(1.6f, tk * r * 0.14f), rotA, fcol, fal);
                }

                // COLAS DE VELOCIDAD del borde exterior (tramo externo)
                if (t > 0.5f && t < 0.95f && Hash01(seed, i * 13 + 3, 91) > 0.62f)
                {
                    float trailR = ro + 0.18f + 0.5f * Hash01(seed, i, 55);
                    float trailTh = th + 0.10f;
                    for (int s = 0; s < 3; s++)
                    {
                        float ur = trailR + s * 0.55f;
                        float uth = trailTh + s * 0.16f;
                        Vector2 up = Pol(ur, uth, r, center);
                        Color ucol = Color.Lerp(col, new Color(140, 25, 70), 0.5f + 0.4f * s / 2f);
                        Cap(up, 0.5f * r * (1f - s * 0.25f), 0.11f * r, uth + Tilt, ucol,
                            0.45f * (1f - s * 0.3f) * bri);
                    }
                }
            }
        }
    }
}
