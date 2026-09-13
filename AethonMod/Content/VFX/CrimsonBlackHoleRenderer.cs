using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// CrimsonBlackHoleRenderer — v6.09 — EL AGUJERO NEGRO CARMESÍ "SUPER
    /// IGUAL" A LA REFERENCIA.
    ///
    /// Petición del usuario: "el disco de acreción debe ser más denso y debe
    /// rodear por completo a la bola negra, esta debe ser de un negro
    /// profundo con bordes de color… que sea igual que la referencia, super
    /// igual; si tiene que ser gigante que así sea". Este renderizador
    /// sustituye al shader de marcha de rayos para el carmesí: es un render
    /// ANALÍTICO POR CAPAS con la geometría de oclusión REAL de un disco
    /// delgado inclinado (BlackHolePhysics), calibrado píxel a píxel contra
    /// la referencia con el prototipo Python (tools/mock_blackhole_v609.py,
    /// EMA 17/255 en el perfil radial — véase
    /// research/blackhole/MATEMATICA_AGUJEROS_NEGROS.md §v6.09).
    ///
    /// CAPAS (de atrás a delante — el orden ES la física):
    ///  1. Fondo: halo ambiental carmesí + 18 rayos cian radiales.
    ///  2. Lado LEJANO del disco (semiplano superior): elipse atenuada,
    ///     comprimida; nace a 2.7·R_sh → el "foso" oscuro del eje mayor.
    ///  3. Halo del anillo de fotones (la esfera lo recorta al dibujarse
    ///     después: solo brilla FUERA de la silueta).
    ///  4. ESFERA NEGRA: disco opaco #000000 puro (negro profundo pedido).
    ///     Oculta el disco lejano y el halo → la bola "come" la luz.
    ///  5. ANILLO DE FOTONES: banda sólida de anillos finos blancos
    ///     (1.16–1.49·R_sh, con eco lensado 1.68–1.96·R_sh) — el "borde de
    ///     color" de la bola + arco de eco sobre la esfera (Luminet 1979).
    ///  6. Lado CERCANO del disco (semiplano inferior): cruza POR DELANTE de
    ///     la cara inferior de la esfera desde +0.76·R_sh (medido en la
    ///     referencia) con su borde interno BLANCO-CALIENTE (2.2·R_sh).
    ///  7. Bloom: arco cercano + hotspot Doppler izquierdo + velo amplio.
    ///
    /// El disco: 32 líneas de corriente keplerianas (ω ∝ r^(−3/2)) dibujadas
    /// como cápsulas SoftGlow solapadas ×2.2 → banda CONTINUA y DENSA con
    /// grano de plasma vivo. Brillo pico a 4.6·R_sh (magenta), carmesí en
    /// los bordes, Doppler δ suavizado (izquierda que se acerca más
    /// brillante), lado cercano aclarado a rosa-blanco en su núcleo.
    ///
    /// TODO a escala gigante autorizada: R_sh = 38px (esfera de 76px),
    /// disco de 494px de envergadura a escala 1 — domina la pantalla como
    /// la referencia domina su encuadre.
    ///
    /// Contrato de batch idéntico al anterior: si endActiveBatch es true
    /// cierra el batch abierto; al terminar queda CERRADO (el llamador lo
    /// restaura). Se usa con el pase del mundo (PreDraw) y con el pase
    /// posterior a la lente (BlackHoleLensSystem, batch ya cerrado).
    /// </summary>
    public static class CrimsonBlackHoleRenderer
    {
        // ==================================================================
        //  PARÁMETROS CALIBRADOS (×R_sh — idénticos al prototipo validado)
        // ==================================================================

        /// <summary>Radio de la sombra en px a escala 1 — GIGANTE.</summary>
        public const float ShadowPx = 38f;

        /// <summary>Achatado de la elipse del disco (b/a): ~70° desde face-on.</summary>
        private const float MinorRatio = 0.345f;

        /// <summary>Borde interno CALIENTE (solo lado cercano) — cruza la esfera.</summary>
        private const float RimRadius = 2.20f;

        /// <summary>Inicio del cuerpo del disco.</summary>
        private const float BodyInner = 2.55f;

        /// <summary>Borde interno del lado LEJANO (el "foso" del eje mayor).</summary>
        private const float FarInner = 2.70f;

        /// <summary>Fade exterior del disco.</summary>
        private const float OuterRadius = 6.50f;

        /// <summary>Radio del brillo máximo (magenta).</summary>
        private const float PeakRadius = 4.60f;

        /// <summary>Líneas de corriente del disco (densidad).</summary>
        private const int Streamlines = 32;

        /// <summary>Grosor de cápsula (×R_sh).</summary>
        private const float CapsuleWidth = 0.68f;

        /// <summary>Paso angular objetivo (×R_sh de arco).</summary>
        private const float SegmentArc = 0.68f;

        /// <summary>Solape de cápsulas (banda continua y densa).</summary>
        private const float CapsuleOverlap = 2.2f;

        /// <summary>Contraste Doppler (izquierda que se acerca +, der −).</summary>
        private const float Doppler = 0.30f;

        /// <summary>Rayos cian del fondo.</summary>
        private const int CyanRays = 18;

        // ==================================================================
        //  TEXTURAS (resolución diferida)
        // ==================================================================

        private static Asset<Texture2D> _softGlow;
        private static Asset<Texture2D> _ring;
        private static Asset<Texture2D> _ray;
        private static Asset<Texture2D> _blackDisk;

        private static Texture2D SoftGlow =>
            (_softGlow ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow")).Value;

        private static Texture2D Ring =>
            (_ring ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Ring")).Value;

        private static Texture2D Ray =>
            (_ray ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/GlowRay")).Value;

        private static Texture2D BlackDisk =>
            (_blackDisk ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/BlackDisk")).Value;

        // ==================================================================
        //  HELPERS DE BATCH
        // ==================================================================

        private static void BeginAdditive(bool endActiveBatch)
        {
            if (endActiveBatch)
                Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
        }

        private static void BeginAlpha(bool endActiveBatch)
        {
            if (endActiveBatch)
                Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>Cuadro estirado/rotado con blending aditivo.</summary>
        private static void AddQuad(Texture2D tex, Vector2 center, Vector2 size,
            float rotation, Color color)
        {
            Main.spriteBatch.Draw(tex, center, null, color, rotation,
                tex.Size() * 0.5f, size / new Vector2(tex.Width, tex.Height),
                SpriteEffects.None, 0f);
        }

        // ==================================================================
        //  CURVAS CALIBRADAS (los mismos valores del prototipo)
        // ==================================================================

        /// <summary>smoothstep de Hermite clampeado.</summary>
        private static float SmoothStep(float e0, float e1, float x)
        {
            float t = MathHelper.Clamp((x - e0) / (e1 - e0), 0f, 1f);
            return t * t * (3f - 2f * t);
        }

        /// <summary>Hash determinista [0,1) — la MISMA secuencia en todas las máquinas.</summary>
        private static float Hash01(int seed, int a, int b)
        {
            int h = unchecked(seed * 374761393 + a * 668265263 + b * 1911520717);
            h ^= h >> 13;
            h = unchecked(h * 1274126177);
            h ^= h >> 16;
            return (h & 0xFFFFFF) / 16777216f;
        }

        /// <summary>
        /// Perfil radial de brillo del disco (medido en la referencia,
        /// normalizado al pico): carril oscuro → subida → PICO 4.6 → caída.
        /// Tabla piecewise-lineal idéntica a la del prototipo validado.
        /// </summary>
        private static float DiskBrightness(float u)
        {
            if (u < RimRadius || u > OuterRadius)
                return 0f;
            if (u < BodyInner)
                return 0.80f; // borde interno caliente
            if (u <= 3.2f) return Lerp(0.30f, 0.55f, (u - BodyInner) / (3.2f - BodyInner));
            if (u <= 3.8f) return Lerp(0.55f, 0.80f, (u - 3.2f) / (3.8f - 3.2f));
            if (u <= PeakRadius) return Lerp(0.80f, 1.0f, (u - 3.8f) / (PeakRadius - 3.8f));
            if (u <= 5.3f) return Lerp(1.0f, 0.82f, (u - PeakRadius) / (5.3f - PeakRadius));
            if (u <= 5.5f) return Lerp(0.82f, 0.52f, (u - 5.3f) / (5.5f - 5.3f));
            if (u <= 5.9f) return Lerp(0.52f, 0.22f, (u - 5.5f) / (5.9f - 5.5f));
            return Lerp(0.22f, 0.08f, (u - 5.9f) / (OuterRadius - 5.9f));
        }

        private static float Lerp(float a, float b, float t)
        {
            t = MathHelper.Clamp(t, 0f, 1f);
            return a + (b - a) * t;
        }

        /// <summary>
        /// Rampa cromática radial NEÓN (medida): borde blanco-rosado →
        /// carmesí vivo → fucsia → MAGENTA (pico) → fucsia → carmesí apagado.
        /// </summary>
        private static Color DiskColor(float u)
        {
            // (radio, color) — la misma tabla del prototipo validado.
            if (u < 2.55f) return LerpColor(new Color(255, 214, 240), new Color(255, 42, 122),
                (u - RimRadius) / (2.55f - RimRadius));
            if (u < 3.20f) return LerpColor(new Color(255, 42, 122), new Color(255, 66, 168),
                (u - 2.55f) / (3.20f - 2.55f));
            if (u < 3.80f) return LerpColor(new Color(255, 66, 168), new Color(255, 88, 232),
                (u - 3.20f) / (3.80f - 3.20f));
            if (u < PeakRadius) return LerpColor(new Color(255, 88, 232), new Color(255, 105, 255),
                (u - 3.80f) / (PeakRadius - 3.80f));
            if (u < 5.30f) return LerpColor(new Color(255, 105, 255), new Color(255, 78, 212),
                (u - PeakRadius) / (5.30f - PeakRadius));
            if (u < 5.90f) return LerpColor(new Color(255, 78, 212), new Color(224, 32, 84),
                (u - 5.30f) / (5.90f - 5.30f));
            return LerpColor(new Color(224, 32, 84), new Color(142, 22, 54),
                (u - 5.90f) / (OuterRadius - 5.90f));
        }

        private static Color LerpColor(Color a, Color b, float t)
        {
            t = MathHelper.Clamp(t, 0f, 1f);
            return new Color(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        /// <summary>
        /// Atenuación del lado lejano: SOLO su región interna se apaga
        /// (u &lt; 5); el PICO del arco lejano brilla SOBRE la esfera como
        /// en la referencia; extremos horizontales y lado cercano a pleno
        /// brillo.
        /// </summary>
        private static float SideFactor(float sinPhi, float u)
        {
            if (sinPhi >= 0f)
                return 1f;
            float farDim = 0.55f + 0.45f * SmoothStep(FarInner, 5.0f, u);
            float topness = SmoothStep(0.18f, 0.85f, -sinPhi);
            return 1f - (1f - farDim) * topness;
        }

        // ==================================================================
        //  EL RENDER COMPLETO
        // ==================================================================

        /// <summary>
        /// Dibuja el agujero carmesí completo según la referencia.
        /// </summary>
        /// <param name="center">Centro en coords de PANTALLA.</param>
        /// <param name="scale">Escala visual (pop elástico / colapso).</param>
        /// <param name="time">Tiempo global animado.</param>
        /// <param name="seed">Semilla determinista por proyectil.</param>
        /// <param name="endActiveBatch">True si puede haber un batch abierto.</param>
        public static void Draw(Vector2 center, float scale, float time, int seed,
            bool endActiveBatch)
        {
            try
            {
                float rSh = ShadowPx * Math.Max(scale, 0.02f);
                if (rSh < 2f) return;

                // ============ 1. FONDO: halo ambiental + rayos cian ============
                BeginAdditive(endActiveBatch);
                AddQuad(SoftGlow, center, new Vector2(7.1f * rSh, 7.1f * rSh * MinorRatio * 1.6f),
                    0f, new Color(110, 15, 48) * 0.16f);
                AddQuad(SoftGlow, center, new Vector2(3.6f * rSh, 3.6f * rSh),
                    0f, new Color(145, 28, 64) * 0.18f);
                for (int i = 0; i < CyanRays; i++)
                {
                    float ang = Hash01(seed, i, 3) * MathHelper.TwoPi +
                                0.10f * (float)Math.Sin(time * 0.31 + i * 2.1);
                    float r0 = 2.1f * rSh;
                    float r1 = (8.5f + 3.5f * Hash01(seed, i, 9)) * rSh;
                    float mid = (r0 + r1) * 0.5f;
                    AddQuad(Ray,
                        center + new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) * mid,
                        new Vector2((r1 - r0) * 2f, 0.18f * rSh),
                        ang, new Color(58, 170, 255) * 0.50f);
                }
                Main.spriteBatch.End();

                // ============ 2. LADO LEJANO DEL DISCO (detrás de la esfera) ============
                DrawDiskHalf(center, rSh, time, seed, nearSide: false);

                // ============ 3. HALO DEL ANILLO DE FOTONES ============
                // (la esfera que viene después lo RECORTA: solo brilla fuera
                //  de la silueta — el "bloom pegado al borde" de la referencia)
                BeginAdditive(false);
                AddQuad(SoftGlow, center, new Vector2(4.2f * rSh, 4.2f * rSh), 0f,
                    new Color(255, 196, 228) * 0.26f);
                AddQuad(SoftGlow, center, new Vector2(3.3f * rSh, 3.3f * rSh), 0f,
                    new Color(255, 214, 238) * 0.42f);
                AddQuad(SoftGlow, center, new Vector2(2.7f * rSh, 2.7f * rSh), 0f,
                    new Color(255, 224, 244) * 0.62f);
                Main.spriteBatch.End();

                // ============ 4. LA ESFERA NEGRA (negro profundo opaco) ============
                // Con AlphaBlend y color (0,0,0): interior = dst·(1−1) = NEGRO
                // PURO. Oculta el disco lejano y el halo: la bola COME la luz.
                BeginAlpha(false);
                Main.spriteBatch.Draw(BlackDisk, center, null, Color.Black, 0f,
                    BlackDisk.Size() * 0.5f,
                    new Vector2(2f * rSh, 2f * rSh) / new Vector2(BlackDisk.Width, BlackDisk.Height),
                    SpriteEffects.None, 0f);
                Main.spriteBatch.End();

                // ============ 5. ANILLOS DE FOTONES (el borde de color) ============
                // Banda sólida 1.16–1.49·R_sh (rungs solapados) + eco lensado
                // 1.68–1.96 + el arco de eco SOBRE la esfera (Luminet).
                BeginAdditive(false);
                float ringPulse = 0.90f + 0.10f * (float)Math.Sin(time * 2.3f);
                DrawRingRung(center, rSh, 1.16f, 150, new Color(255, 248, 253), ringPulse);
                DrawRingRung(center, rSh, 1.22f, 245, new Color(255, 250, 254), ringPulse);
                DrawRingRung(center, rSh, 1.28f, 255, new Color(255, 253, 255), ringPulse);
                DrawRingRung(center, rSh, 1.34f, 255, new Color(255, 253, 255), ringPulse);
                DrawRingRung(center, rSh, 1.40f, 225, new Color(255, 247, 252), ringPulse);
                DrawRingRung(center, rSh, 1.46f, 175, new Color(255, 240, 250), ringPulse);
                DrawRingRung(center, rSh, 1.56f, 100, new Color(255, 236, 248), 1f);
                DrawRingRung(center, rSh, 1.62f, 70, new Color(255, 234, 247), 1f);
                DrawRingRung(center, rSh, 1.68f, 220, new Color(255, 238, 249), 1f);
                DrawRingRung(center, rSh, 1.75f, 205, new Color(255, 236, 248), 1f);
                DrawRingRung(center, rSh, 1.82f, 165, new Color(255, 232, 247), 1f);
                DrawRingRung(center, rSh, 1.89f, 120, new Color(255, 228, 246), 1f);
                DrawRingRung(center, rSh, 1.96f, 80, new Color(255, 226, 245), 1f);
                // Arco de eco sobre la esfera (la imagen secundaria lensada).
                AddQuad(SoftGlow, center - new Vector2(0f, 1.62f * rSh),
                    new Vector2(3.6f * rSh, 1.5f * rSh), 0f,
                    new Color(255, 235, 248) * 0.16f);
                Main.spriteBatch.End();

                // ============ 6. LADO CERCANO (cruza POR DELANTE de la esfera) ============
                DrawDiskHalf(center, rSh, time, seed, nearSide: true);

                // ============ 7. BLOOM ============
                BeginAdditive(false);
                // Arco cercano (el semiplano que nos sale al encuentro).
                AddQuad(SoftGlow,
                    center + new Vector2(0f, PeakRadius * rSh * MinorRatio * 0.72f),
                    new Vector2(6.8f * rSh, 3.1f * rSh * MinorRatio * 1.9f), 0f,
                    new Color(255, 150, 205) * 0.30f);
                // Hotspot Doppler: el lado que se ACERCA (izquierda).
                AddQuad(SoftGlow, center + new Vector2(-3.5f * rSh, 0.10f * rSh),
                    new Vector2(3.3f * rSh, 1.5f * rSh), -0.05f,
                    new Color(255, 130, 190) * 0.18f);
                // Velo amplio de sangrado del disco al espacio.
                AddQuad(SoftGlow, center,
                    new Vector2(6.9f * rSh, 6.9f * rSh * MinorRatio * 1.55f), 0f,
                    new Color(255, 90, 170) * 0.12f);
                Main.spriteBatch.End();
            }
            catch
            {
                // Cierre defensivo SOLO en el path de error.
                try { Main.spriteBatch.End(); } catch { }
            }
        }

        /// <summary>Un anillo fino de fotones a radio u·R_sh.</summary>
        private static void DrawRingRung(Vector2 center, float rSh, float u, int alpha,
            Color color, float pulse)
        {
            // Ring.png: el trazo gráfico vive a ~0.92 del semiancho → el
            // tamaño final del sprite debe ser ~2.174× el radio visible.
            float size = 2.174f * u * rSh;
            AddQuad(Ring, center, new Vector2(size, size), 0f,
                color * ((alpha / 255f) * pulse));
        }

        /// <summary>
        /// Una mitad del disco de acreción (cápsulas SoftGlow solapadas por
        /// línea de corriente kepleriana). El lado cercano se dibuja sobre
        /// la esfera; el lejano queda detrás (la esfera lo oculta).
        /// REQUIERE que no haya ningún batch abierto (el pase anterior
        /// cerró el suyo).
        /// </summary>
        private static void DrawDiskHalf(Vector2 center, float rSh, float time, int seed,
            bool nearSide)
        {
            float capW = CapsuleWidth * rSh;

            BeginAdditive(false);
            Texture2D glow = SoftGlow;
            Vector2 glowSize = new Vector2(glow.Width, glow.Height);

            for (int ri = 0; ri < Streamlines; ri++)
            {
                float u = RimRadius + (OuterRadius - RimRadius) * ri / (Streamlines - 1);
                float r = u * rSh;
                float b = DiskBrightness(u);
                if (b <= 0.01f)
                    continue;

                // El lado LEJANO nace más lejos: el "foso" oscuro sobre el
                // eje mayor de la referencia (el borde caliente 2.2·R_sh
                // SOLO existe en el semiplano que cruza por delante).
                if (!nearSide && u < FarInner)
                    continue;

                Color baseCol = DiskColor(u);

                // Kepler: ω ∝ r^(−3/2) — el interior hierve más rápido.
                float omega = 1.5f * (float)Math.Pow(PeakRadius / u, 1.5f);

                int nSeg = Math.Max(48, Math.Min(120,
                    (int)(MathHelper.TwoPi * r / (SegmentArc * rSh))));
                float step = MathHelper.TwoPi / nSeg;
                // Desalineación de costuras entre anillos (banda uniforme).
                float gridShift = Hash01(seed, ri, 77) * step;
                float capLen = step * r * CapsuleOverlap;
                float capWSide = capW * (nearSide ? 1f : 0.72f);

                for (int si = 0; si < nSeg; si++)
                {
                    float phi = si * step + gridShift;
                    float sinPhi = (float)Math.Sin(phi);
                    if (nearSide != (sinPhi > 0f))
                        continue;

                    // Proyección de pantalla del punto del disco.
                    Vector2 offset = BlackHolePhysics.ProjectDiskPoint(r, phi, MinorRatio);
                    Vector2 pos = center + offset;

                    // Tangente proyectada (orientación de la cápsula).
                    float rot = (float)Math.Atan2(MinorRatio * Math.Cos(phi), -Math.Sin(phi));

                    // Grano fino del plasma (clumps keplerianos orbitando).
                    float flick = 0.88f + 0.12f * (float)Math.Sin(
                        7.0f * phi - omega * time * 1.8f +
                        2.3f * Hash01(seed, ri, si));

                    // Doppler: el lado que se ACERCA (izquierda) brilla más.
                    float dop = 1f + Doppler * (-(float)Math.Cos(phi));

                    float mult = b * flick * dop * SideFactor(sinPhi, u);
                    if (!nearSide)
                        mult *= 0.92f;

                    // El borde interno caliente ARDE en el lado cercano.
                    if (u < BodyInner)
                        mult *= 0.35f + 0.65f * Math.Max(0f, sinPhi);

                    float alpha = Math.Min(1f, 1.02f * mult);

                    Color c = baseCol;
                    if (nearSide && sinPhi > 0f)
                    {
                        // Núcleo del arco cercano más CLARO (rosa-blanco medido).
                        float w = 0.22f * sinPhi;
                        c = LerpColor(c, new Color(255, 196, 255), w);
                    }
                    else if (!nearSide && u < 3.1f)
                    {
                        // Borde del arco lejano más BLANCO (medido arriba).
                        float w = 0.45f * (3.1f - u) / (3.1f - FarInner);
                        c = LerpColor(c, Color.White, w);
                    }

                    Main.spriteBatch.Draw(glow, pos, null, c * alpha, rot,
                        glowSize * 0.5f,
                        new Vector2(capLen, capWSide) / glowSize,
                        SpriteEffects.None, 0f);
                }
            }
            Main.spriteBatch.End();
        }
    }
}
