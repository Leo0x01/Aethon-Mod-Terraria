using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;
using AethonMod.Content.Effects.Bruma;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// DeadStarRenderer — v6.26 — LA ESTRELLA MUERTA (LA ENANA NEGRA).
    ///
    /// Un sol que agotó hasta su último fotón. La firma visual (la
    /// INVERSA de toda la familia: MASA OSCURA PRIMERO, luz tenue
    /// después — el contrato de lote de la casa para lo que OCLUYE):
    ///
    ///   · EL CUERPO — LA TÉCNICA DEL SOL ORIGINAL (v6.31) con la paleta
    ///     de ASCUAS APAGADAS: granulación tenue rojiza de un cadáver que
    ///     aún guarda la forma de ser un sol.
    ///   · EL AURA OSCURA — quads negros en AlphaBlend alrededor: la
    ///     estrella APAGA la luz del mundo ( Lighting.AddLight negativo
    ///     no existe: el oscurecimiento visual lo hace esta masa).
    ///   · BRASAS FRÍAS CAYENDO — PyraLib.Tongue con la rampa ColdFire
    ///     y alfas bajos, apuntando HACIA ABAJO (brasas que CAEN de un
    ///     cuerpo sin fuego).
    ///   · EL HUMO DE ENTROPÍA — BrumaFX.Puff oscuro que SUBE del cuerpo
    ///     (puffs deterministas en ciclo vertical, sin estado).
    ///   · LOS ECOS FANTASMALES — las runas de su vida pasada titilando
    ///     a 0.2 Hz: RuneSunRenderer.EmitRingSystem (el emisor compartido
    ///     de la casa) con tier 2 y alfa estrangulado al parpadeo.
    ///   · EL RESCOLDO — un rim naranja MUY tenue (la última vez que fue
    ///     un sol) + luz de mundo naranja casi nada.
    ///
    /// CONTRATO DE BATCH: Draw() exige el SpriteBatch CERRADO y lo deja
    /// CERRADO (el llamador restaura el batch de tML — contrato v6.10).
    /// </summary>
    public static class DeadStarRenderer
    {
        /// <summary>Radio del cuerpo en px a escala 1 (v6.31: 28→33 — más grande).</summary>
        public const float BodyPx = 33f;

        // --- PALETA: oscuridad + rescoldo ambar ---
        private static readonly Color VoidBlack = new(12, 9, 16);
        private static readonly Color EntropySmoke = new(26, 22, 30);
        private static readonly Color EmberAmber = new(200, 110, 50);
        private static readonly Color EmberDim = new(120, 70, 45);

        // --- PINCELES ---
        private static Asset<Texture2D> _glow;
        private static Asset<Texture2D> _ring;

        private static Texture2D Glow =>
            (_glow ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow")).Value;


        private static Texture2D Ring =>
            (_ring ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Ring")).Value;

        // ==================================================================
        //  EL RENDER PRINCIPAL — batch CERRADO → CERRADO
        // ==================================================================

        /// <summary>
        /// Dibuja la estrella muerta: EL CUERPO con LA TÉCNICA DEL SOL ORIGINAL
        /// (v6.31 — DrawSunBody con la paleta de ASCUAS APAGADAS: un cadáver
        /// estelar de granulación tenue rojiza, visible y sólido — el disco
        /// negro plano de v6.30 BORRADO) + el aura que apaga ALREDEDOR + el
        /// humo de entropía + las brasas frías + los ecos rúnicos a 0.2 Hz.
        /// `lifeT` = 0..1, `seed` = semilla del disparo.
        /// </summary>
        public static void Draw(Projectile p, float lifeT, int seed)
        {
            try
            {
                Vector2 drawPos = p.Center - Main.screenPosition;
                float scale = Math.Max(p.scale, 0.05f);
                float R = BodyPx * scale;
                float time = Main.GlobalTimeWrappedHourly;

                // === 1. EL AURA QUE APAGA (AlphaBlend — PRIMERO, DETRÁS del
                //     cuerpo: oscurece el MUNDO alrededor, no al cadáver) ===
                DrawDarkAura(drawPos, R, time);

                // === 2. EL CUERPO — LA TÉCNICA DEL SOL ORIGINAL con la paleta
                //     de ascuas (main 150,72,50 · darker 58,28,22 · acento
                //     28,10,8 · giro casi parado 0.22 — un muerto reciente) ===
                RuneSunRenderer.DrawSunBody(drawPos, R, p.rotation, time,
                    new Color(150, 72, 50),
                    new Color(58, 28, 22),
                    new Color(28, 10, 8),
                    new Color(122, 52, 30),
                    new Color(64, 26, 16),
                    new Color(142, 72, 46),
                    0.22f, 1f - lifeT * 0.25f);

                // === 3. EL HUMO DE ENTROPÍA (BrumaFX — sube del cuerpo) ===
                DrawEntropySmoke(drawPos, R, time, seed);

                // === 4. EL RESCOLDO (lo poco que le queda de ser un sol) ===
                BeginAdditive();
                DrawEmbers(drawPos, R, time, seed, lifeT);
                DrawDimRim(drawPos, R, time, lifeT, seed);
                Main.spriteBatch.End();

                // === 5. LOS ECOS FANTASMALES (las runas de su vida pasada
                //     titilando a 0.2 Hz — el emisor compartido de la casa) ===
                float blink = 0.5f + 0.5f * MathF.Sin(time * 0.2f * MathHelper.TwoPi);
                VFXCore.Begin();
                RuneSunRenderer.EmitRingSystem(p.Center, R, time, seed, 2,
                    0f, lifeT, alpha: 0.16f * blink);
                VFXCore.FlushAdditive(null, false);   // el batch ya está CERRADO
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }
        }

        // ==================================================================
        //  CAPA 1 — EL AURA QUE APAGA (AlphaBlend: oscurece el MUNDO
        //  alrededor; el cuerpo del cadáver es la técnica del sol)
        // ==================================================================

        private static void DrawDarkAura(Vector2 pos, float R, float time)
        {
            BeginAlpha();

            // EL AURA OSCURA: dos quads negros (SoftGlow con color casi
            // negro) — la estrella se lleva la luz del mundo alrededor.
            for (int i = 0; i < 2; i++)
            {
                float mul = i == 0 ? 3.1f : 1.9f;
                // El aura RESPIRA hacia adentro (mundo muriendo: 0.3 Hz).
                float breathe = 0.90f + 0.10f * MathF.Sin(time * 1.9f + i * 2.6f);
                float a = (i == 0 ? 0.32f : 0.46f) * breathe;
                Main.spriteBatch.Draw(Glow, pos, null,
                    Tint(VoidBlack, a), 0f,
                    Glow.Size() * 0.5f, ScaleOf(R * mul * breathe), SpriteEffects.None, 0f);
            }

            Main.spriteBatch.End();
        }

        // ==================================================================
        //  CAPA 2 — EL HUMO DE ENTROPÍA (BrumaFX, SUBE del cuerpo)
        // ==================================================================

        private static void DrawEntropySmoke(Vector2 pos, float R, float time, int seed)
        {
            // BrumaFX dibuja en el lote ABIERTO que tenga Main.spriteBatch:
            // usamos SU lote de masa (AlphaBlend + LinearClamp — la casa).
            BrumaFX.BeginMass();

            const int Puffs = 3;
            for (int i = 0; i < Puffs; i++)
            {
                // Ciclo vertical SIN estado: cada puff sube del cuerpo y
                // renace abajo ((t·0.14 + i/N) mod 1).
                float phase = (time * 0.14f + i / (float)Puffs) % 1f;
                float rise = phase * R * 2.6f;
                // El zig horizontal del ascenso (determinista por fase).
                float sway = MathF.Sin(phase * 5.2f + i * 2.4f) * R * 0.42f;
                Vector2 pPos = pos + new Vector2(sway, -R * 0.4f - rise);
                // El puff CRECE y MUERE al subir (la entropía se dispersa).
                float size = R * (0.55f + 0.85f * phase);
                float a = 0.30f * (1f - phase) * (0.6f + 0.4f * phase);
                BrumaFX.Puff(pPos, size, EntropySmoke, seed + 977 + i * 61,
                    time + i * 3f, alpha: a, quality: 0.5f);
            }

            Main.spriteBatch.End();
        }

        // ==================================================================
        //  CAPA 3 — LAS BRASAS FRÍAS Y EL RESCOLDO (aditivo tenue)
        // ==================================================================

        private static void DrawEmbers(Vector2 pos, float R, float time, int seed, float lifeT)
        {
            // LAS BRASAS FRÍAS CAYENDO: PyraLib.Tongue con la rampa
            // ColdFire, alfas bajos y rot = π (la lengua apunta ABAJO —
            // brasas que CAEN de un cuerpo sin fuego).
            const int Embers = 3;
            for (int k = 0; k < Embers; k++)
            {
                float ang = VFXCore.Hash01(seed, 211 + k, 3) * MathHelper.TwoPi + time * 0.10f;
                float rr = R * (0.55f + 0.55f * VFXCore.Hash01(seed, 223 + k, 7));
                Vector2 basePos = pos + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * rr;
                // Cada brasa late a su ritmo apagado (0.5 Hz — frío).
                float dim = 0.30f + 0.25f * MathF.Sin(time * 3.1f + k * 2.1f);
                PyraLib.Tongue(Main.spriteBatch, basePos,
                    R * (0.8f + 0.5f * VFXCore.Hash01(seed, 227 + k, 11)),   // altura
                    R * 0.22f,                                                // ancho
                    PyraPalettes.ColdFire,
                    0.30f + 0.12f * VFXCore.Hash01(seed, 229 + k, 13),       // temperatura
                    seed + 331 + k * 37, time,
                    intensity: dim, wind: 0f, gravDir: 1f,
                    rot: MathHelper.Pi);   // CAE, no sube
            }
        }

        private static void DrawDimRim(Vector2 pos, float R, float time, float lifeT, int seed)
        {
            // EL RESCOLDO: el rim naranja MUY tenue del limbo (el recuerdo
            // de cuando fue un sol) — casi nada, respirando a 0.2 Hz.
            float ember = 0.5f + 0.5f * MathF.Sin(time * 0.2f * MathHelper.TwoPi);
            Vector2 size = VFXCore.RingQuadSize(R * 1.12f);
            Main.spriteBatch.Draw(Ring, pos, null,
                Tint(EmberAmber, 0.14f + 0.10f * ember), time * 0.05f,
                Ring.Size() * 0.5f, size / Ring.Size(), SpriteEffects.None, 0f);

            // Los PUNTOS DE RESCOLDO: 4 ascuas ambar fijas en el limbo,
            // encendiéndose a 0.2 Hz (la vida que le queda, contada).
            int flick02 = StormLib.FlickTick(time, 0.2f);
            for (int k = 0; k < 4; k++)
            {
                if (!StormLib.IsLit(seed + 233 + k, flick02, 0.45f)) continue;
                float ang = k * MathHelper.PiOver2 + 0.6f;
                Vector2 ashen = pos + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * (R * 0.95f);
                Main.spriteBatch.Draw(Glow, ashen, null,
                    Tint(EmberDim, 0.40f * ember), 0f,
                    Glow.Size() * 0.5f, ScaleOf(R * 0.20f), SpriteEffects.None, 0f);
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

        /// <summary>Abre el lote ALFA de la casa (masa que OCLUYE).</summary>
        private static void BeginAlpha()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
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
