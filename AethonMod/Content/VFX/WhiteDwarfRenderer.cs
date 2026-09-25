using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// WhiteDwarfRenderer — v6.26 — LA ENANA BLANCA.
    ///
    /// El rescoldo cristalino de un sol extinto: pequeña (~20 px), densa,
    /// BLANCO-AZUL. La firma visual:
    ///
    ///   · FULGOR FRÍO ESTABLE — un bloom casi sin latido (la enana no
    ///     arde: IRRADIA su calor residual acumulado durante eones).
    ///   · SUPERFICIE CRISTALINA — mini-destellos HEXAGONALES fijos que
    ///     rotan LENTO (la red cristalina de carbono-metal de la estrella:
    ///     posiciones fijas por semilla, la textura HexCyan de la casa).
    ///   · EL ANILLO DE ACRECIÓN — si un enemigo pasa cerca, un aro
    ///     elíptico tenue se enciende alrededor + MOTAS DE LEECH que
    ///     viajan del enemigo a la estrella (le "roba masa": visual
    ///     determinista sin estado — posición lerp enemigo→estrella).
    ///
    /// CONTRATO DE BATCH: Draw() exige el SpriteBatch CERRADO y lo deja
    /// CERRADO (el llamador restaura el batch de tML — contrato v6.10).
    /// </summary>
    public static class WhiteDwarfRenderer
    {
        /// <summary>Radio del cuerpo en px a escala 1 (v6.31: 34→39 — más grande).</summary>
        public const float BodyPx = 39f;

        /// <summary>Radio de detección del anillo de acreción (px).</summary>
        public const float AccretionRange = 150f;

        // --- PALETA blanco-azul cristalino ---
        private static readonly Color CrystalWhite = new(250, 252, 255);
        private static readonly Color CrystalBlue = new(185, 210, 255);
        private static readonly Color CrystalCyan = new(150, 230, 240);

        // --- PINCELES ---
        private static Asset<Texture2D> _glow;
        private static Asset<Texture2D> _hex;
        private static Asset<Texture2D> _ring;

        private static Texture2D Glow =>
            (_glow ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow")).Value;

        // (v6.31: el orbe del núcleo BORRADO — el cuerpo es LA TÉCNICA DEL SOL
        // ORIGINAL vía RuneSunRenderer.DrawSunBody; el pincel Orb ya no se usa)

        /// <summary>La FACETA hexagonal cristalina (la red de la enana).</summary>
        private static Texture2D Hex =>
            (_hex ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/HexCyan")).Value;

        private static Texture2D Ring =>
            (_ring ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Ring")).Value;

        // ==================================================================
        //  EL RENDER PRINCIPAL — batch CERRADO → CERRADO
        // ==================================================================

        /// <summary>
        /// Dibuja la enana blanca: cuerpo cristalino + facetas hexagonales
        /// rotando lento + fulgor frío + (si `prey` ≠ null) el anillo de
        /// acreción con sus motas de leech. `lifeT` = 0..1, `seed` = semilla.
        /// </summary>
        public static void Draw(Projectile p, float lifeT, int seed, Vector2? preyCenter)
        {
            try
            {
                Vector2 drawPos = p.Center - Main.screenPosition;
                float scale = Math.Max(p.scale, 0.05f);
                float R = BodyPx * scale;
                float time = Main.GlobalTimeWrappedHourly;
                float fade = 1f - lifeT * 0.40f;
                bool accreting = preyCenter.HasValue;

                // === 1. EL CUERPO DENSO — LA TÉCNICA DEL SOL ORIGINAL (v6.31:
                //     DrawSunBody gestiona SUS PROPIOS lotes → se dibuja ANTES
                //     de abrir el aditivo; el disco blanco-azulado sólido) ===
                DrawBody(drawPos, R, fade);

                BeginAdditive();

                // === 2. EL FULGOR FRÍO ESTABLE (casi sin latido) ===
                DrawColdGlow(drawPos, R, time, fade);

                // === 3. LAS FACETAS CRISTALINAS (hexágonos fijos, lento) ===
                DrawFacets(drawPos, R, time, seed, fade);

                // === 4. EL ANILLO DE ACRECIÓN + MOTAS DE LEECH ===
                if (accreting)
                {
                    DrawAccretionRing(drawPos, R, time, seed, fade);
                    DrawLeechMotes(drawPos, R, preyCenter.Value, time, seed);
                }

                Main.spriteBatch.End();
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }
        }

        // ==================================================================
        //  CAPA 1 — EL FULGOR FRÍO
        // ==================================================================

        private static void DrawColdGlow(Vector2 pos, float R, float time, float fade)
        {
            // Fulgor estable: un micro-latido (0.4 Hz) — la enana está
            // ENFRIÁNDOSE, no latiendo (contraste con el sol de la casa).
            float settle = 0.94f + 0.06f * MathF.Sin(time * 2.5f);
            Main.spriteBatch.Draw(Glow, pos, null,
                Tint(CrystalBlue, 0.30f * fade), 0f,
                Glow.Size() * 0.5f, ScaleOf(R * 3.4f * settle), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(Glow, pos, null,
                Tint(CrystalCyan, 0.38f * fade), 0f,
                Glow.Size() * 0.5f, ScaleOf(R * 1.9f * settle), SpriteEffects.None, 0f);
        }

        // ==================================================================
        //  CAPA 2 — EL CUERPO DENSO
        // ==================================================================

        private static void DrawBody(Vector2 pos, float R, float fade)
        {
            // v6.31 — LA TÉCNICA DEL SOL ORIGINAL (RuneSunRenderer.DrawSunBody):
            // el disco de plasma SunShader BLANCO-AZULADO cristalino — la enana
            // de verdad, sólida y visible (el orbe+blob de v6.30 BORRADO).
            RuneSunRenderer.DrawSunBody(pos, R, 0f, Main.GlobalTimeWrappedHourly,
                new Color(242, 246, 255),
                new Color(150, 170, 210),
                new Color(70, 90, 170),
                new Color(200, 220, 255),
                new Color(120, 150, 255),
                new Color(215, 230, 255),
                0.55f, fade);
        }

        // ==================================================================
        //  CAPA 3 — LAS FACETAS CRISTALINAS (hexágonos fijos rotando lento)
        // ==================================================================

        private static void DrawFacets(Vector2 pos, float R, float time, int seed, float fade)
        {
            const int Count = 7;
            // LA RED CRISTALINA: posiciones FIJAS sobre el disco (por
            // semilla) que rotan LENTO (0.09 rev/s — la red es rígida).
            float lattice = time * MathHelper.TwoPi * 0.09f;

            for (int k = 0; k < Count; k++)
            {
                float ang = VFXCore.Hash01(seed, 131 + k, 3) * MathHelper.TwoPi + lattice;
                float rr = R * (0.30f + 0.55f * VFXCore.Hash01(seed, 137 + k, 7));
                Vector2 fPos = pos + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * rr;

                // Cada faceta destella con su fase fija (el glint del
                // cristal girando bajo la luz — NO parpadeo nervioso).
                float glint = 0.55f + 0.45f * MathF.Sin(time * 1.3f + k * 2.4f +
                    VFXCore.Hash01(seed, 149 + k, 11) * MathHelper.TwoPi);
                float fSize = R * (0.42f + 0.28f * VFXCore.Hash01(seed, 151 + k, 13));

                Vector2 hexScale = new Vector2(fSize, fSize) / Hex.Size();
                Main.spriteBatch.Draw(Hex, fPos, null,
                    Tint(k % 2 == 0 ? CrystalCyan : CrystalBlue, 0.55f * glint * fade),
                    lattice * 0.6f + k * 0.8f,
                    Hex.Size() * 0.5f, hexScale, SpriteEffects.None, 0f);

                // El punto blanco de la faceta (el reflejo especular).
                Main.spriteBatch.Draw(Glow, fPos, null,
                    Tint(CrystalWhite, 0.50f * glint * fade), 0f,
                    Glow.Size() * 0.5f, ScaleOf(fSize * 0.28f), SpriteEffects.None, 0f);
            }
        }

        // ==================================================================
        //  CAPA 4 — EL ANILLO DE ACRECIÓN + EL LEECH
        // ==================================================================

        private static void DrawAccretionRing(Vector2 pos, float R, float time,
            int seed, float fade)
        {
            // El aro elíptico tenue alrededor (inclinado como los discos
            // reales de acreción) girando LENTO y con alfa respirando.
            float spin = time * 0.22f;
            float breathe = 0.75f + 0.25f * MathF.Sin(time * 1.1f);
            Vector2 size = VFXCore.RingQuadSize(R * 2.3f);
            Main.spriteBatch.Draw(Ring, pos, null,
                Tint(CrystalCyan, 0.30f * breathe * fade), spin - 0.5f,
                Ring.Size() * 0.5f,
                new Vector2(size.X, size.Y * 0.38f) / Ring.Size(),
                SpriteEffects.None, 0f);

            // El RIM interior del disco (más caliente al caer).
            Main.spriteBatch.Draw(Ring, pos, null,
                Tint(CrystalWhite, 0.22f * breathe * fade), spin,
                Ring.Size() * 0.5f,
                new Vector2(size.X * 0.62f, size.X * 0.62f * 0.38f) / Ring.Size(),
                SpriteEffects.None, 0f);
        }

        private static void DrawLeechMotes(Vector2 pos, float R, Vector2 preyWorld,
            float time, int seed)
        {
            // LAS MOTAS DE LEECH: deterministas y SIN estado — cada mota
            // es un lerp enemigo→estrella con fase ((t·v + i/N) mod 1),
            // así la corriente de masa FLUYE sin guardar nada.
            const int Motes = 6;
            Vector2 preyScreen = preyWorld - Main.screenPosition;
            float speed = 0.55f;   // motas por segundo

            for (int i = 0; i < Motes; i++)
            {
                float phase = (time * speed + i / (float)Motes) % 1f;
                // La fase INVERTIDA: nacen en el enemigo, MUEREN en la
                // estrella (la masa fluye HACIA la enana).
                float t = 1f - phase;
                Vector2 mote = Vector2.Lerp(preyScreen, pos, t);
                // El zig-zag fino de la caída espiral (órbita decreciente).
                float spiral = (1f - t) * 2.4f + VFXCore.Hash01(seed, 173 + i, 5) * 6.28f;
                mote += new Vector2(MathF.Cos(spiral), MathF.Sin(spiral)) * (6f * t * t);

                // Alfa: nace tenue, se ENCARGA al acercarse a la estrella
                // (la masa se calienta al caer).
                float a = (0.25f + 0.65f * t) * (1f - phase * 0.2f);
                Main.spriteBatch.Draw(Glow, mote, null,
                    Tint(i % 2 == 0 ? CrystalCyan : CrystalWhite, a),
                    spiral, Glow.Size() * 0.5f, ScaleOf(R * (0.16f + 0.10f * t)),
                    SpriteEffects.None, 0f);
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

        /// <summary>Tinte de intensidad LINEAL de la casa (v6.50.3 — el Additive
        /// de FNA es (SourceAlpha, One): el alfa GATEA; RGB intacto, alfa=f).</summary>
        private static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            // v6.50.7 — REVERSIÓN AL PREMULTIPLICADO (la sonda v6.50.3
            // estaba incompleta): el pipeline REAL premultiplica los PNG al
            // cargar (ReLogic PngReader.PreMultiplyAlpha, verificado en el
            // decompilado del tML 2026.07.3.0) y el AlphaBlend de FNA es
            // (One, InvSourceAlpha) — compositing PREMULTIPLICADO, donde el
            // RGB del tinte ES la intensidad. El tinte lineal dejaba el
            // color SIN escalar en los lotes de masa (bruma fantasma
            // saturada) y sobrealimentaba los aditivos hasta ×10 (destellos
            // que inundaban la pantalla). El (RGB·f, A·f) de v6.25 es el
            // correcto para AMBOS presets de FNA.
            return new Color(
                (byte)(int)(c.R * f), (byte)(int)(c.G * f), (byte)(int)(c.B * f),
                (byte)(int)(255f * f));
        }
    }
}
