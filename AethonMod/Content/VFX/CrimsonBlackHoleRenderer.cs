using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// CrimsonBlackHoleRenderer — v6.10 — EL AGUJERO NEGRO "OBLIVION".
    ///
    /// La referencia ORIGINAL del usuario (Reddit: Ancients Awakened —
    /// Regicide, Oblivion God of the Void) NO es un Gargantua de disco
    /// delgado: es un VÓRTICE DE PLASMA carmesí con:
    ///   · Esfera negra compacta con un GAP oscuro (el brillo NO la toca).
    ///   · Anillo interior 360° a ~1.5R con borde interno blanco-caliente.
    ///   · UNA HOJA GRUESA EN CRESCIENTE que barre POR ARRIBA
    ///     (O→NO→N→NNE) y se estira en una AGUJA LARGA hasta ~5.9R.
    ///   · Un segundo cresiente BAJO (ESE→S→SSW) cuyo borde exterior
    ///     llega a ~6.3R por el sur.
    ///   · TODO el vórtice INCLINADO (SW→NE, ~24°) y girando HORARIO.
    ///   · Hotspot blanco-amarillo en NNE, chispas, mechones lejanos,
    ///     rayos azul-violeta RAMIFICADOS dentro de la esfera.
    ///
    /// Calibrado con el prototipo tools/mock_oblivion_v610.py contra
    /// mediciones numpy de la referencia (perfil radial EMA 27/255 y
    /// extensión angular por cuadrantes emparejada: aguja NNE 6.0R vs
    /// 6.1R, sur 5.7R vs 6.4R, oeste 3.2R vs 2.3R).
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
        private static readonly float[] BladeT = { 0.00f, 1.00f };
        private static readonly float[] BladeTh = { 175f, 352f };      // grados: O→NO→N→NNE
        private static readonly float[] BladeRo = { 2.30f, 3.30f, 3.00f, 3.30f, 3.20f, 4.20f, 5.90f };
        private static readonly float[] BladeTk = { 1.00f, 0.80f, 0.55f, 0.32f, 0.07f };
        private static readonly float[] BladeBri = { 0.60f, 0.80f, 1.00f, 0.78f, 0.52f };
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
        private static readonly float[] LowerRo = { 1.95f, 3.20f, 6.30f, 6.00f, 4.80f, 2.60f };
        private static readonly float[] LowerTk = { 0.65f, 0.50f, 0.35f, 0.18f, 0.08f };
        private static readonly float[] LowerBri = { 0.60f, 0.78f, 0.82f, 0.50f, 0.22f };
        private static readonly Color[] LowerCol =
        {
            new Color(255, 60, 140), new Color(255, 82, 172),
            new Color(250, 45, 125), new Color(222, 36, 100),
            new Color(140, 18, 55)
        };

        // ==================================================================
        //  TEXTURAS
        // ==================================================================

        private static Asset<Texture2D> _softGlow;
        private static Asset<Texture2D> _blackDisk;

        private static Texture2D SoftGlow =>
            (_softGlow ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow")).Value;

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

        /// <summary>Interpolación por segmentos suavizada: t en [0,1] sobre
        /// una tabla de valores equiespaciados (calibradas del prototipo).</summary>
        private static float KeyLerp(float[] keys, float t)
        {
            int n = keys.Length - 1;
            float u = MathHelper.Clamp(t, 0f, 1f) * n;
            int i = (int)u;
            if (i >= n) return keys[n];
            return MathHelper.Lerp(keys[i], keys[i + 1], SmoothStep(0f, 1f, u - i));
        }

        private static Color KeyColor(Color[] keys, float t)
        {
            int n = keys.Length - 1;
            float u = MathHelper.Clamp(t, 0f, 1f) * n;
            int i = (int)u;
            if (i >= n) return keys[n];
            return Color.Lerp(keys[i], keys[i + 1], MathHelper.Clamp(u - i, 0f, 1f));
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

        /// <summary>Cápsula elíptica aditiva.</summary>
        private static void Cap(Vector2 pos, float len, float wid, float rot, Color c, float alpha)
        {
            if (alpha <= 0.004f) return;
            Texture2D tex = SoftGlow;
            Vector2 texSize = new Vector2(tex.Width, tex.Height);
            Main.spriteBatch.Draw(tex, pos, null, c * alpha, rot,
                texSize * 0.5f, new Vector2(len, wid) / texSize,
                SpriteEffects.None, 0f);
        }

        /// <summary>Cuadro suave estirado/rotado.</summary>
        private static void Quad(Vector2 pos, Vector2 size, float rot, Color c, float alpha)
        {
            Texture2D tex = SoftGlow;
            Main.spriteBatch.Draw(tex, pos, null, c * alpha, rot,
                tex.Size() * 0.5f, size / tex.Size(),
                SpriteEffects.None, 0f);
        }

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
                BeginAdditive();
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
                    BladeT, BladeTh, BladeRo, BladeTk, BladeBri, BladeCol, 112, 1.0f, 0.30f);
                DrawBlade(center, r, rot, time, seed,
                    LowerT, LowerTh, LowerRo, LowerTk, LowerBri, LowerCol, 72, 0.65f, 0.22f);

                // ============ 4. HOTSPOT + nudo NNE + aguja + mechones ============
                Vector2 hx = BladePoint(BladeT, BladeTh, BladeRo, BladeTk, 0.72f, rot, r, center, out float hTh, out _, out _);
                Cap(hx, 1.5f * r, 0.85f * r, hTh, new Color(255, 240, 168), 0.85f);
                Cap(hx, 0.65f * r, 0.40f * r, hTh, new Color(255, 253, 232), 1.10f);
                // nudo caliente NNE a 3R (medido: (255,246,137) a 355°, 3R)
                Vector2 knot = Pol(3.0f, MathHelper.ToRadians(355f), r, center);
                Cap(knot, 0.55f * r, 0.34f * r, MathHelper.ToRadians(355f), new Color(255, 246, 150), 0.55f);
                // chispa de la aguja
                Vector2 tip = BladePoint(BladeT, BladeTh, BladeRo, BladeTk, 0.97f, rot, r, center, out float tTh, out _, out _);
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
                    float th = MathHelper.ToRadians(KeyLerp2(BladeT, BladeTh, sparkT[k])) + rot;
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

        /// <summary>Interpola la tabla de ángulos (grados) de una hoja.</summary>
        private static float KeyLerp2(float[] ts, float[] vals, float t)
        {
            // tablas lineales de 2 puntos
            float u = MathHelper.Clamp(t, 0f, 1f);
            return MathHelper.Lerp(vals[0], vals[1], u);
        }

        /// <summary>Punto central de la sección t de una hoja.</summary>
        private static Vector2 BladePoint(float[] ts, float[] ths, float[] ros, float[] tks,
            float t, float rot, float r, Vector2 center, out float theta, out float rm, out float tk)
        {
            float ro = KeyLerp(ros, t);
            tk = KeyLerp(tks, t);
            rm = ro - tk * 0.5f;
            theta = MathHelper.ToRadians(KeyLerp2(ts, ths, t)) + rot;
            return Pol(rm, theta, r, center);
        }

        /// <summary>Una hoja del vórtice: cápsulas + filamentos + colas.</summary>
        private static void DrawBlade(Vector2 center, float r, float rot, float time, int seed,
            float[] ts, float[] ths, float[] ros, float[] tks, float[] bris, Color[] cols,
            int nSeg, float filamentBoost, float jitter)
        {
            for (int i = 0; i < nSeg; i++)
            {
                float t = i / (float)(nSeg - 1);
                float jr = (Hash01(seed, i * 7 + 1, 13) - 0.5f) * jitter;
                Vector2 pos = BladePoint(ts, ths, ros, tks, t, rot, r, center, out float th, out float rm, out float tk);
                rm += jr * 0.5f;
                pos = Pol(rm, th, r, center);
                // tangente numérica
                float eps = 1.5f / nSeg;
                Vector2 pos2 = BladePoint(ts, ths, ros, tks, Math.Min(t + eps, 1f), rot, r, center, out _, out _, out _);
                float rotA = (float)Math.Atan2(pos2.Y - pos.Y, pos2.X - pos.X);
                float segLen = Math.Max(Vector2.Distance(pos, pos2) * 1.55f, 3f);
                Color col = KeyColor(cols, t);
                float bri = KeyLerp(bris, t);
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
                    float ro = KeyLerp(ros, t);
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
