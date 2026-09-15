using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// RedSupergiantRenderer — v6.26 — LA SUPERGIGANTE ROJA.
    ///
    /// EL COLOSO: un cuerpo ENORME (~90 px) rojo y frío. La firma visual:
    ///
    ///   · CELDAS DE CONVECCIÓN — los blobs voraces de la granulación
    ///     solar pero LENTA y ENORME: 9 celdas deterministas sobre el
    ///     disco, cada una orbitando a su paso, SUBIENDO y BAJANDO (el
    ///     ciclo de convección en fase por celda) y coloreada por la
    ///     rampa PyraPalettes.SolarFire según su temperatura local.
    ///   · LA ATMÓSFERA EXTENSA — la corona roja 3× (tres velos SoftGlow
    ///     + el anillo FireRing de la casa girando lento).
    ///   · EL PULSO DE VIDA LENTO — todo el cuerpo late a 0.2 Hz
    ///     hinchándose 5% (un gigante RESPIRA en segundos, no en ticks).
    ///   · EL COLAPSO — los últimos 20 ticks: TODO se encoge (1−c)²
    ///     mientras el color huye del rojo al BLANCO-AZUL (el núcleo se
    ///     enciende al comprimirse) y un anillo de DISTORSIÓN contrae —
    ///     la nova saldrá DE AQUÍ.
    ///
    /// CONTRATO DE BATCH: Draw() exige el SpriteBatch CERRADO y lo deja
    /// CERRADO (el llamador restaura el batch de tML — contrato v6.10).
    /// </summary>
    public static class RedSupergiantRenderer
    {
        /// <summary>Radio del cuerpo en px a escala 1 (ENORME).</summary>
        public const float BodyPx = 90f;

        /// <summary>La frecuencia del pulso de vida (0.2 Hz — lento).</summary>
        public const float PulseHz = 0.2f;

        // --- PALETA roja fría (la rampa SolarFire de PyraLib + tintes) ---
        private static readonly Color GiantRed = new(255, 90, 40);
        private static readonly Color GiantOrange = new(255, 140, 60);
        private static readonly Color GiantDeep = new(140, 30, 20);
        private static readonly Color CollapseWhite = new(210, 230, 255);

        // --- PINCELES ---
        private static Asset<Texture2D> _glow;
        private static Asset<Texture2D> _fireRing;

        private static Texture2D Glow =>
            (_glow ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow")).Value;

        /// <summary>EL ANILLO DE FUEGO (la atmósfera girando).</summary>
        private static Texture2D FireRing =>
            (_fireRing ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/FireRing")).Value;

        // ==================================================================
        //  EL RENDER PRINCIPAL — batch CERRADO → CERRADO
        // ==================================================================

        /// <summary>
        /// Dibuja la supergigante: cuerpo enorme + celdas de convección +
        /// atmósfera 3× + el pulso de vida 0.2 Hz — y si `collapse` &gt; 0,
        /// la IMPLOSIÓN (todo encoge, se enciende y distorsiona).
        /// `lifeT` = 0..1, `collapse` = 0..1 de los últimos 20 ticks.
        /// </summary>
        public static void Draw(Projectile p, float lifeT, float collapse, int seed)
        {
            try
            {
                Vector2 drawPos = p.Center - Main.screenPosition;
                float scale = Math.Max(p.scale, 0.05f);
                float time = Main.GlobalTimeWrappedHourly;
                float fade = 1f - lifeT * 0.30f;

                // EL PULSO DE VIDA LENTO: late a 0.2 Hz hinchándose 5% —
                // y MUERE con el colapso (el corazón se PARA: breathe→1).
                float breathe = 1f + 0.05f * MathF.Sin(time * PulseHz * MathHelper.TwoPi + seed)
                                      * (1f - collapse);

                // EL COLAPSO: todo se encoge (1−c)² — la implosión acelera.
                float shrink = (1f - collapse) * (1f - collapse);
                float R = BodyPx * scale * breathe * shrink;

                // El COLOR DEL COLAPSO: el rojo frío huye al blanco-azul
                // (el núcleo comprimido se enciende antes de la nova).
                Color bodyTint = Color.Lerp(GiantOrange, CollapseWhite, collapse);
                Color glowTint = Color.Lerp(GiantRed, CollapseWhite, collapse * 0.8f);

                BeginAdditive();

                // === 1. LA ATMÓSFERA EXTENSA (corona roja 3×) ===
                DrawAtmosphere(drawPos, R, glowTint, time, seed, fade, collapse);

                // === 2. EL CUERPO (el disco enorme y frío) ===
                DrawBody(drawPos, R, bodyTint, glowTint, fade);

                // === 3. LAS CELDAS DE CONVECCIÓN (los blobs voraces) ===
                DrawConvectionCells(drawPos, R, time, seed, fade, collapse);

                // === 4. EL ANILLO DE FUEGO (la atmósfera girando) ===
                DrawFireRingHalo(drawPos, R, glowTint, time, fade);

                // === 5. LA DISTORSIÓN DEL COLAPSO (el anillo que contrae) ===
                if (collapse > 0f)
                    DrawCollapseDistortion(drawPos, R, collapse, time, seed);

                Main.spriteBatch.End();
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }
        }

        // ==================================================================
        //  CAPA 1 — LA ATMÓSFERA EXTENSA (3× el cuerpo)
        // ==================================================================

        private static void DrawAtmosphere(Vector2 pos, float R, Color glowTint,
            float time, int seed, float fade, float collapse)
        {
            // TRES VELOS concéntricos (la atmósfera de una gigante se
            // extiende ENORME más allá del disco — 3× su radio).
            float atm = 1f + collapse * 2.2f;   // durante el colapso el
            // halo se estira (la luz ESCAPA mientras el cuerpo encoge)
            Main.spriteBatch.Draw(Glow, pos, null,
                Tint(glowTint, 0.16f * fade), 0f,
                Glow.Size() * 0.5f, ScaleOf(R * 3.0f * atm), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(Glow, pos, null,
                Tint(glowTint, 0.24f * fade), 0f,
                Glow.Size() * 0.5f, ScaleOf(R * 2.2f * atm), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(Glow, pos, null,
                Tint(Color.Lerp(glowTint, GiantOrange, 0.5f), 0.34f * fade), 0f,
                Glow.Size() * 0.5f, ScaleOf(R * 1.45f), SpriteEffects.None, 0f);
        }

        // ==================================================================
        //  CAPA 2 — EL CUERPO
        // ==================================================================

        private static void DrawBody(Vector2 pos, float R, Color bodyTint,
            Color glowTint, float fade)
        {
            // El disco: la panza profunda de la gigante.
            Main.spriteBatch.Draw(Glow, pos, null,
                Tint(GiantDeep, 0.55f * fade), 0f,
                Glow.Size() * 0.5f, ScaleOf(R * 1.02f), SpriteEffects.None, 0f);
            // La masa media (el cuerpo rojo-naranja).
            Main.spriteBatch.Draw(Glow, pos, null,
                Tint(bodyTint, 0.55f * fade), 0f,
                Glow.Size() * 0.5f, ScaleOf(R * 0.78f), SpriteEffects.None, 0f);
            // El limbo caliente (el borde interno más brillante).
            Main.spriteBatch.Draw(Glow, pos, null,
                Tint(Color.Lerp(bodyTint, new Color(255, 220, 140), 0.4f), 0.45f * fade), 0f,
                Glow.Size() * 0.5f, ScaleOf(R * 0.45f), SpriteEffects.None, 0f);
        }

        // ==================================================================
        //  CAPA 3 — LAS CELDAS DE CONVECCIÓN (la granulación LENTA)
        // ==================================================================

        private static void DrawConvectionCells(Vector2 pos, float R, float time,
            int seed, float fade, float collapse)
        {
            const int Cells = 9;
            for (int k = 0; k < Cells; k++)
            {
                float h1 = VFXCore.Hash01(seed, 401 + k, 3);
                float h2 = VFXCore.Hash01(seed, 409 + k, 7);
                float h3 = VFXCore.Hash01(seed, 419 + k, 11);

                // LA ÓRBITA LENTA de la celda (cada una a su paso — el
                // interior de una gigante convecta a PASOS de semanas).
                float orbitDir = k % 2 == 0 ? 1f : -1f;
                float ang = h1 * MathHelper.TwoPi + time * (0.045f + 0.03f * h2) * orbitDir;

                // EL CICLO DE CONVECCIÓN: la celda SUBE y BAJA (0.1 Hz —
                // lenta y ENORME, la granulación del coloso).
                float convHz = 0.08f + 0.05f * h3;
                float conv = MathF.Sin(time * convHz * MathHelper.TwoPi + h2 * MathHelper.TwoPi);
                float rr = R * (0.20f + 0.48f * h2) * (1f + 0.16f * conv);
                Vector2 cPos = pos + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * rr;

                // La celda SUBE: más caliente (más cerca del amarillo en
                // la rampa SolarFire); BAJA: más fría (rojo profundo).
                // (v6.28: rr/R con guardas — R>0.05 garantizado por Draw, y
                // el cociente clampeado: la división JAMÁS puede dar NaN/±Inf
                // que envenene la temperatura — el crash del log del usuario.)
                float rel = R > 0.05f ? MathHelper.Clamp(rr / R, 0f, 1f) : 0.5f;
                float temp = MathHelper.Clamp(0.42f + 0.20f * conv + 0.12f * (1f - rel) - collapse * 0.25f, 0f, 1f);
                Color cell = PyraPalettes.Sample(PyraPalettes.SolarFire, temp);

                // El TAMAÑO de la celda (voraz: respira con su ciclo).
                float size = R * (0.20f + 0.16f * h3) * (1f + 0.30f * conv);
                Main.spriteBatch.Draw(Glow, cPos, null,
                    Tint(cell, 0.38f * fade * (1f + 0.25f * conv)),
                    ang + time * 0.15f * orbitDir,
                    Glow.Size() * 0.5f, ScaleOf(size), SpriteEffects.None, 0f);

                // EL OJO de la celda (el punto caliente del plasma subiendo).
                if (conv > 0.35f)
                    Main.spriteBatch.Draw(Glow, cPos, null,
                        Tint(PyraPalettes.Sample(PyraPalettes.SolarFire, temp + 0.25f),
                            0.45f * fade * conv), 0f,
                        Glow.Size() * 0.5f, ScaleOf(size * 0.35f), SpriteEffects.None, 0f);
            }
        }

        // ==================================================================
        //  CAPA 4 — EL ANILLO DE FUEGO (la atmósfera girando)
        // ==================================================================

        private static void DrawFireRingHalo(Vector2 pos, float R, Color glowTint,
            float time, float fade)
        {
            // FireRing (1024²): dos pasadas girando LENTO en sentidos
            // opuestos (la atmósfera en rotación diferencial).
            Vector2 s1 = new Vector2(R * 2.6f, R * 2.6f) / FireRing.Size();
            Main.spriteBatch.Draw(FireRing, pos, null,
                Tint(glowTint, 0.14f * fade), time * 0.05f,
                FireRing.Size() * 0.5f, s1, SpriteEffects.None, 0f);
            Vector2 s2 = new Vector2(R * 1.7f, R * 1.7f) / FireRing.Size();
            Main.spriteBatch.Draw(FireRing, pos, null,
                Tint(Color.Lerp(glowTint, GiantOrange, 0.5f), 0.18f * fade), -time * 0.08f,
                FireRing.Size() * 0.5f, s2, SpriteEffects.None, 0f);
        }

        // ==================================================================
        //  CAPA 5 — LA DISTORSIÓN DEL COLAPSO (el anillo que CONTRAE)
        // ==================================================================

        private static void DrawCollapseDistortion(Vector2 pos, float R,
            float collapse, float time, int seed)
        {
            // EL ANILLO CONTRAYÉNDOSE: OndaLib.Shock con el progreso
            // INVERTIDO (1−c) y aberración cromática — la onda de choque
            // corriendo HACIA el centro mientras el cuerpo encoge.
            OndaLib.Shock(Main.spriteBatch, pos, 1f - collapse,
                R * (0.6f + 2.6f * collapse), CollapseWhite,
                0.55f * (0.4f + 0.6f * collapse), seed, 9f,
                OndaFalloff.Quadratic, chromatic: true);

            // EL FRENTE CALIENTE: un pulso interno apretándose.
            OndaLib.Pulse(Main.spriteBatch, pos, 1f - collapse,
                R * (0.9f + 1.2f * collapse), CollapseWhite,
                0.40f * (0.4f + 0.6f * collapse), seed + 7);

            // EL ÚLTIMO LATIDO: el corazón blanco antes de la nova
            // (brilla más fuerte cuanto más comprimido está).
            float heart = 0.5f + 0.5f * MathF.Sin(time * 18f);
            Main.spriteBatch.Draw(Glow, pos, null,
                Tint(CollapseWhite, (0.35f + 0.55f * collapse) * (0.7f + 0.3f * heart)), 0f,
                Glow.Size() * 0.5f, ScaleOf(R * (0.5f + 0.5f * collapse)), SpriteEffects.None, 0f);

            // LA ONDA GRANDE DE LA NOVA (v6.26 — el guión: colapso → nova):
            // en el último 15% del colapso la OndaLib.Shock MAYOR se ABRE
            // (progreso 0→1, aberración cromática) — el anillo que la nova
            // atravesará en el tick siguiente (OnKill).
            if (collapse > 0.85f)
            {
                float novaT = (collapse - 0.85f) / 0.15f;
                OndaLib.Shock(Main.spriteBatch, pos, novaT,
                    R * 3.2f + 260f * novaT, CollapseWhite,
                    0.60f * novaT, seed + 13, 12f,
                    OndaFalloff.Quadratic, chromatic: true);
            }
        }

        // ==================================================================
        //  HELPERS
        // ==================================================================

        /// <summary>Escala uniforme del pincel Glow para un diámetro dado.</summary>
        private static Vector2 ScaleOf(float diameter) =>
            new Vector2(diameter, diameter) / Glow.Size();

        /// <summary>Abre el lote ADITIVO de la casa (brillos).</summary>
        private static void BeginAdditive()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>Tinte premultiplicado de la casa (v6.25).</summary>
        private static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color(
                (byte)(int)(c.R * f), (byte)(int)(c.G * f), (byte)(int)(c.B * f),
                (byte)(int)(255f * f));
        }
    }
}
