using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Cosmic;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// EnjambrePrismaticoRenderer — v6.26 — EL BASTÓN DEL ENJAMBRE PRISMÁTICO.
    ///
    /// EL ENJAMBRE dibujado 100% por código — DOS fases:
    ///
    ///   FASE NÚCLEO: un corazón de luz prismático (LumenLib.Bloom con el
    ///   hue en DRIFT + Flare + Aurora de 6 bandas — el huevo del enjambre)
    ///   con la ESTELA del vuelo (EstelaLib.Ribbon Comet).
    ///
    ///   FASE ENJAMBRE: 12 ABISPAJAS, cada una con:
    ///     · CUERPO: cápsula orientada por su velocidad (cabeza + abdomen
    ///       elíptico — leen "avispa", no "chispa");
    ///     · ALAS: DOS quads aleteando (sin(_flap·3) — 25 Hz de aleteo,
    ///       desfasado por avispa — el zumbido se VE);
    ///     · COLOR PRISMÁTICO: LumenLib.Hue(i/12 + drift lento) — cada
    ///       avispa un color DISTINTO del arcoíris;
    ///     · ESTELA CORTA: los 3 puntos del trail dibujados como quads
    ///       decrecientes (el rastro de 3 frames);
    ///     · AGUIJÓN: el destello de picadura cuando el cooldown acaba de
    ///       resetearse (Flare blanco del color de la avispa).
    ///
    ///   EL PANAL: sin presa, el enjambre ronda en órbita (el renderer
    ///   añade el vaivén de la nube con senos inconmensurables).
    ///
    /// CONTRATO DE BATCH: Draw() exige el SpriteBatch CERRADO y lo deja
    /// CERRADO (contrato v6.10). Brillos en Additive.
    /// </summary>
    public static class EnjambrePrismaticoRenderer
    {
        // ==================================================================
        //  LAS PALETAS (el prisma vivo)
        // ==================================================================

        private static readonly Color BlancoCalido = new(255, 250, 240);

        // ==================================================================
        //  EL PUNTO DE ENTRADA
        // ==================================================================

        /// <summary>Dibuja el núcleo o el enjambre. Batch CERRADO → CERRADO.</summary>
        public static void Draw(EnjambrePrismaticoProjectile p, float age, int seed)
        {
            try
            {
                float time = Main.GlobalTimeWrappedHourly;
                Vector2 center = p.Projectile.Center - Main.screenPosition;

                if (!p.EnjambreVivo)
                {
                    DrawNucleo(p, center, age, time, seed);
                    return;
                }

                DrawEnjambre(p, center, time, seed);
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }
        }

        // ==================================================================
        //  FASE 1 · EL NÚCLEO (el huevo prismático)
        // ==================================================================

        private static void DrawNucleo(EnjambrePrismaticoProjectile p, Vector2 center,
            float age, float time, int seed)
        {
            // === LA ESTELA del vuelo del núcleo (Ribbon Comet) ===
            Vector2[] camino = EstelaLib.Track(p.Projectile.whoAmI, 14).Points();
            if (camino != null && camino.Length >= 3)
            {
                Vector2[] pantalla = new Vector2[camino.Length];
                for (int i = 0; i < camino.Length; i++)
                    pantalla[i] = camino[i] - Main.screenPosition;
                BeginAdditive();
                EstelaLib.Ribbon(Main.spriteBatch, pantalla, 12f, EstelaProfile.Comet,
                    Color.HotPink, 0.40f, seed + 7, time, head: false);
                Main.spriteBatch.End();
            }

            // === EL CORAZÓN prismático (Additive) ===
            float hue = LumenLib.Drift(time, seed, 0.45f);
            Color c = LumenLib.Hue(hue, 0.6f);
            BeginAdditive();
            // El PULSO previo al estallido (late más rápido al final).
            float tension = MathHelper.Clamp(age / EnjambrePrismaticoProjectile.NucleoTicks, 0f, 1f);
            float latido = 0.80f + 0.20f * MathF.Sin(time * (8f + 18f * tension));
            LumenLib.Bloom(Main.spriteBatch, center, 30f * latido, c, 0.65f, 3);
            LumenLib.Flare(Main.spriteBatch, center, 26f * latido, c, 0.55f, time * 2.4f);
            // EL AURORA de 6 bandas (el cascarón del arcoíris).
            LumenLib.Aurora(Main.spriteBatch, center, 44f, time, hue, 0.5f, 6);
            Main.spriteBatch.End();

            // === LA LUZ del núcleo ===
            if (Main.netMode != NetmodeID.Server)
                Lighting.AddLight(p.Projectile.Center,
                    c.R / 255f * 0.6f, c.G / 255f * 0.6f, c.B / 255f * 0.6f);
        }

        // ==================================================================
        //  FASE 2 · EL ENJAMBRE (las doce avispas)
        // ==================================================================

        private static void DrawEnjambre(EnjambrePrismaticoProjectile p, Vector2 center,
            float time, int seed)
        {
            int n = EnjambrePrismaticoProjectile.Avispas;

            // === LA NUBE DEL PANAL (AlphaBlend PRIMERO — muy tenue: el
            //     zumbido tiene polen) — omitida por presupuesto: el
            //     enjambre es TODO aditivo (chispas de luz pura). ===

            BeginAdditive();

            // EL VAIVÉN de la nube: 3 motas errantes entre las avispas.
            for (int k = 0; k < 3; k++)
            {
                float ang = time * (0.5f + 0.17f * k) * ((k & 1) == 0 ? 1f : -1f)
                            + VFXCore.Hash01(seed, 5 + k, 3) * MathHelper.TwoPi;
                Vector2 pos = center + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.6f)
                    * (38f + 10f * MathF.Sin(time * 0.7f + k));
                Color c = LumenLib.Hue(VFXCore.Hash01(seed, 7 + k, 11), 0.6f);
                Quad(Glow, pos, new Vector2(2.2f, 2.2f), 0f, Tint(c, 0.3f));
            }

            // === LAS AVISPAS ===
            for (int i = 0; i < n; i++)
            {
                Vector2 pos = p.PosAvispa(i) - Main.screenPosition;
                Vector2 vel = p.PosAvispa(i) - p.TrailAvispa(i)[0];
                if (vel.LengthSquared() < 0.001f) vel = Vector2.UnitX;
                Vector2 dir = Vector2.Normalize(vel);
                float rot = MathF.Atan2(dir.Y, dir.X);

                // EL COLOR PRISMÁTICO de esta avispa (hue fijo + drift lento).
                Color c = LumenLib.Hue(p.HueAvispa(i) + time * 0.03f, 0.62f);
                Color blanca = Color.Lerp(c, BlancoCalido, 0.55f);

                // 1) LA ESTELA CORTA (3 puntos históricos, decrecientes).
                Vector2[] trail = p.TrailAvispa(i);
                for (int t = 0; t < trail.Length; t++)
                {
                    Vector2 tp = trail[t] - Main.screenPosition;
                    float tt = (t + 1) / (float)(trail.Length + 1);
                    float a = 0.28f * (1f - tt) + 0.08f;
                    Quad(Glow, tp, new Vector2(5.5f * (1f - tt * 0.5f),
                        5.5f * (1f - tt * 0.5f)), 0f, Tint(c, a));
                }

                // 2) EL ABDOMEN (cápsula orientada — el cuerpo gordo atrás).
                Quad(Glow, pos - dir * 3.5f, new Vector2(11f, 7.5f), rot, Tint(c, 0.7f));
                // 3) LA CABEZA (más pequeña y BLANCA delante).
                Quad(Glow, pos + dir * 3.2f, new Vector2(6.5f, 5.5f), rot, Tint(blanca, 0.8f));

                // 4) LAS ALAS (dos quads aleteando sobre el tórax — 25 Hz).
                float flap = MathF.Sin(time * 25f + p.HueAvispa(i) * MathHelper.TwoPi * 3f);
                for (int w = -1; w <= 1; w += 2)
                {
                    Vector2 alaPos = pos + new Vector2(-dir.Y, dir.X) * (5.5f * w) - dir * 1.5f;
                    float ang = rot - MathHelper.PiOver2 * w * (0.45f + 0.35f * flap);
                    Quad(Glow, alaPos, new Vector2(8.5f, 3.4f), ang,
                        Tint(BlancoCalido, 0.30f + 0.12f * MathF.Abs(flap)));
                }

                // 5) EL AGUIJÓN: el destello de picadura (el cooldown en su
                //    MÁXIMO = acaba de clavar el aguijón).
                float cool = p.CoolAvispa(i);
                if (cool > EnjambrePrismaticoProjectile.PicaCooldown - 6f)
                {
                    float flash = (cool - (EnjambrePrismaticoProjectile.PicaCooldown - 6f)) / 6f;
                    LumenLib.Flare(Main.spriteBatch, pos, 14f * flash + 6f, c,
                        0.6f * flash, time * 3f);
                }
            }

            Main.spriteBatch.End();
        }

        // ==================================================================
        //  LOS HELPERS DE PINTADO (los de la casa)
        // ==================================================================

        private static Asset<Texture2D> _glow;

        private static Texture2D Glow =>
            (_glow ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow")).Value;

        /// <summary>Abre el lote ADITIVO de la casa (Immediate).</summary>
        private static void BeginAdditive()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>Un quad de luz centrado (el pincel universal).</summary>
        private static void Quad(Texture2D tex, Vector2 pos, Vector2 size, float rot, Color tint)
        {
            if (tex == null || tint.A == 0) return;
            Main.spriteBatch.Draw(tex, pos, null, tint, rot,
                new Vector2(tex.Width, tex.Height) * 0.5f,
                size / new Vector2(tex.Width, tex.Height),
                SpriteEffects.None, 0f);
        }

        /// <summary>Tinte PREMULTIPLICADO de la casa (v6.25 — RGB×f).</summary>
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
