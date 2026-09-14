using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// RuneSunRenderer — v6.19 — LA FAMILIA DE LOS SOLES RÚNICOS.
    ///
    /// Petición del usuario: "al igual que los agujeros negros tienen
    /// anillos con runas, crea varias copias del sol y ponles anillos con
    /// runas — la primera copia UN solo anillo, la segunda DOS (uno
    /// rodeando el sol y el otro en OTRA dirección), la tercera TRES y así
    /// hasta 10 copias; cada copia mejorada un poquito más, hasta la
    /// copia 10 con muchas mejoras y animaciones. El SOL ORIGINAL NO SE
    /// TOCA (SunProjectile queda intacto)".
    ///
    /// ARQUITECTURA (una sola clase parametrizada por `tier` 1..10):
    ///
    ///   · CUERPO SOLAR — la MISMA TÉCNICA del sol original (backglow
    ///     BloomCircle + aura RadialShineShader + disco de plasma
    ///     SunShader con granulación dendrítica), re-implementada aquí
    ///     con parámetros propios: el Sol de Terraria no se toca, las
    ///     copias heredan su piel por técnica, no por dependencia.
    ///
    ///   · N ANILLOS RÚNICOS (N = tier) — cada anillo vive en SU PROPIO
    ///     PLANO ORBITAL: semiejes, achatado e inclinación distintos por
    ///     anillo, y GIRO ALTERNO (horario / antihorario — "el otro debe
    ///     rodear el sol en otra dirección"). Cada anillo es un aro
    ///     elíptico de cápsulas con profundidad (el frente más brillante)
    ///     + glifos rúnicos cabalgando la órbita, rotados a la TANGENTE,
    ///     con perlas y latidos propios.
    ///
    ///   · MEJORAS PROGRESIVAS — cada tier añade UNA capa nueva:
    ///       2 · CHISPAS orbitales viajando por los anillos.
    ///       3 · DESTELLOS de 4 puntas alrededor del sistema.
    ///       4 · PROMINENCIAS — arcos de plasma saltando del limbo.
    ///       5 · VIENTO SOLAR — partículas de luz subiendo radialmente.
    ///       6 · RAYOS FUGITIVOS entre anillos (LightningCore.Bolt).
    ///       7 · PRECESIÓN — los planos orbitales BAMBOLEAN vivos.
    ///       8 · NÚCLEO PULSANTE + ONDAS DE ECO expandiéndose.
    ///       9 · CORONA DE PÉTALOS de plasma orbitando el cuerpo.
    ///      10 · ERUPCIÓN RÚNICA (runas que se desprenden y vuelan) +
    ///           JETS POLARES con rayo interno — el sistema completo.
    ///
    /// PALETA SOLAR REGIA (diferente de los agujeros): blanco-
    /// incandescente (255,250,235) → oro (255,195,85) → ámbar (255,140,40)
    /// → carmesí solo en la GIGANTE final. Acentos azul-estelar
    /// (150,180,255) cada 3er anillo desde tier 7 — la "magia" fría del
    /// sello entre el fuego.
    ///
    /// CONTRATO DE BATCH: Draw() exige el SpriteBatch CERRADO y lo deja
    /// CERRADO (el llamador restaura el batch de tML).
    /// </summary>
    public static class RuneSunRenderer
    {
        // ==================================================================
        //  PARÁMETROS
        // ==================================================================

        /// <summary>Radio del disco de plasma en px a escala 1.</summary>
        public const float BodyPx = 46f;

        /// <summary>Tier máximo de la familia (10 copias).</summary>
        public const int MaxTier = 10;

        // --- LA GEOMETRÍA ORBITAL (por anillo k = 0..N-1) ---
        private const float RingA0 = 1.62f;      // semieje mayor del 1er anillo (×R)
        private const float RingAStep = 0.44f;   // separación entre anillos (×R)
        private const float RingSpin0 = 0.26f;   // giro base (rad/s)
        private const float RingSpinStep = 0.045f;

        // --- LAS RUNAS ---
        private const int Runes0 = 6;            // glifos del 1er anillo
        private const int RuneStep = 2;          // +2 glifos por anillo

        // --- LA VIDA EXTRA ---
        private const float BoltHz = 14f;        // regeneración de los rayos (~14 Hz)

        // --- LA GIGANTE FINAL (tier alto): últimos 90 ticks ---
        public const int RedGiantTicks = 90;

        // ==================================================================
        //  PALETA SOLAR REGIA
        // ==================================================================

        private static readonly Color WhiteIncan = new(255, 250, 235); // blanco-incandescente
        private static readonly Color SunGold = new(255, 195, 85);     // ORO solar
        private static readonly Color SunAmber = new(255, 140, 40);    // ámbar del limbo
        private static readonly Color SunCrimson = new(255, 90, 40);   // carmesí (gigante final)
        private static readonly Color StarBlue = new(150, 180, 255);   // acento azul-estelar
        private static readonly Color RuneGold = new(255, 190, 80);    // cuerpo de runa dorada
        private static readonly Color RuneGoldTip = new(255, 240, 185);// punta pálida
        private static readonly Color RuneBlue = new(135, 165, 255);   // cuerpo de runa azul
        private static readonly Color RuneBlueTip = new(215, 230, 255);// punta fría

        // ==================================================================
        //  PINCELES (la biblioteca de texturas del proyecto)
        // ==================================================================

        private static Asset<Texture2D> _glow;
        private static Asset<Texture2D> _ring;

        private static Texture2D Glow =>
            (_glow ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow")).Value;

        private static Texture2D Ring =>
            (_ring ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Ring")).Value;

        // Los shaders del sol (técnicas del proyecto — el sol original no se toca)
        private static Ref<Effect> _sunShader;
        private static Ref<Effect> _shineShader;
        private static bool _sunShaderFailed;
        private static bool _shineShaderFailed;

        // ==================================================================
        //  EL RENDER PRINCIPAL — batch CERRADO → CERRADO
        // ==================================================================

        /// <summary>
        /// Dibuja la copia `tier` (1..10) del sol: cuerpo solar + sistema
        /// rúnico completo. `lifeT` = 0..1 del ciclo de vida, `rg` = 0..1
        /// de la gigante final, `seed` = semilla determinista del disparo.
        /// </summary>
        public static void Draw(Projectile p, int tier, float lifeT, float rg, int seed)
        {
            LoadShaders();

            try
            {
                Vector2 drawPos = p.Center - Main.screenPosition;
                float scale = Math.Max(p.scale, 0.05f);
                float R = BodyPx * scale;
                float time = Main.GlobalTimeWrappedHourly;
                int boltFlick = LightningCore.FlickTick(time, BoltHz);

                // === 0. GLOW CORONAL + VIENTO SOLAR (aditivo, DETRÁS) ===
                BeginAdditive();
                DrawCorona(drawPos, R, lifeT, rg);
                if (tier >= 5) DrawSolarWind(drawPos, R, time, seed, tier);
                if (tier >= 8) DrawCorePulse(drawPos, R, time, tier);
                Main.spriteBatch.End();

                // === 1. BACKGLOW (alpha — el resplandor profundo) ===
                BeginAlpha();
                Texture2D bloom = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Textures/BloomCircleSmall").Value;
                Color glowHot = Color.Lerp(new Color(255, 230, 100), new Color(255, 75, 25), rg);
                glowHot.A = 0;
                Main.spriteBatch.Draw(bloom, drawPos, null, glowHot * 0.7f, 0f,
                    bloom.Size() * 0.5f, scale * 0.95f, SpriteEffects.None, 0f);
                Color glowRed = Color.Lerp(new Color(255, 50, 0), new Color(255, 30, 10), rg);
                glowRed.A = 0;
                Main.spriteBatch.Draw(bloom, drawPos, null, glowRed * 0.45f, 0f,
                    bloom.Size() * 0.5f, scale * 1.61f, SpriteEffects.None, 0f);
                Main.spriteBatch.End();

                // === 2. AURA (RadialShineShader — ruido de energía) ===
                Texture2D wavyBlotch = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Textures/WavyBlotchNoise").Value;
                if (_shineShader != null && _shineShader.Value != null)
                {
                    Effect shine = _shineShader.Value;
                    shine.Parameters["globalTime"].SetValue(time);
                    // Misma proporción del sol: 2.72 × el ANCHO (2R) del cuerpo.
                    Vector2 shineScale = Vector2.One * R * 5.44f / wavyBlotch.Size();

                    BeginAdditive();
                    shine.CurrentTechnique.Passes[0].Apply();
                    Color shineColor = Color.Lerp(new Color(252, 212, 112), new Color(255, 95, 45), rg);
                    Main.spriteBatch.Draw(wavyBlotch, drawPos, null,
                        shineColor * 0.24f, p.rotation,
                        wavyBlotch.Size() * 0.5f, shineScale, SpriteEffects.None, 0f);
                    Main.spriteBatch.End();
                }

                // === 3. EL DISCO DE PLASMA (SunShader — la piel del sol) ===
                if (_sunShader != null && _sunShader.Value != null)
                {
                    Effect shader = _sunShader.Value;
                    Texture2D psychedelic = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Textures/PsychedelicWingTextureOffsetMap").Value;
                    Texture2D dendritic = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Textures/DendriticNoiseZoomedOut").Value;

                    shader.Parameters["coronaIntensityFactor"].SetValue(0.05f);
                    shader.Parameters["mainColor"].SetValue(
                        Color.Lerp(new Color(255, 255, 255), new Color(255, 150, 120), rg).ToVector3());
                    shader.Parameters["darkerColor"].SetValue(
                        Color.Lerp(new Color(204, 92, 25), new Color(150, 28, 12), rg).ToVector3());
                    shader.Parameters["subtractiveAccentFactor"].SetValue(new Color(181, 0, 0).ToVector3());
                    shader.Parameters["sphereSpinTime"].SetValue(time * 0.9f);
                    shader.Parameters["globalTime"].SetValue(time);

                    Main.graphics.GraphicsDevice.Textures[1] = wavyBlotch;
                    Main.graphics.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
                    Main.graphics.GraphicsDevice.Textures[2] = psychedelic;
                    Main.graphics.GraphicsDevice.SamplerStates[2] = SamplerState.LinearWrap;

                    Vector2 drawScale = Vector2.One * R * 3.0f / dendritic.Size();

                    BeginAlpha();
                    shader.CurrentTechnique.Passes[0].Apply();
                    Main.spriteBatch.Draw(dendritic, drawPos, null, Color.White, p.rotation,
                        dendritic.Size() * 0.5f, drawScale, SpriteEffects.None, 0f);
                    Main.spriteBatch.End();
                }

                // === 4. EL SISTEMA RÚNICO (aditivo — todo lo mágico) ===
                BeginAdditive();
                DrawRingSystem(drawPos, R, time, seed, tier, rg, boltFlick, lifeT);
                if (tier >= 2) DrawOrbitSparks(drawPos, R, time, seed, tier);
                if (tier >= 3) DrawTwinkles(drawPos, R, time, seed, tier);
                if (tier >= 4) DrawProminences(drawPos, R, time, seed, boltFlick, tier);
                if (tier >= 6) DrawInterRingBolts(drawPos, R, time, seed, boltFlick, tier);
                if (tier >= 8) DrawEchoWaves(drawPos, R, time, seed);
                if (tier >= 9) DrawPlasmaPetals(drawPos, R, time, seed, tier);
                if (tier >= 10) DrawRunicEruption(drawPos, R, time, seed, boltFlick);
                if (tier >= 10) DrawPolarJets(drawPos, R, time, seed, boltFlick);
                Main.spriteBatch.End();
            }
            catch
            {
                // Cierre defensivo SOLO en el path de error (contrato v6.10).
                try { Main.spriteBatch.End(); } catch { }
            }
        }

        // ==================================================================
        //  EL GLOW CORONAL — dos capas que crecen con el ciclo (función
        //  pura de lifeT — cero parpadeo, la lección v5.96 del sol)
        // ==================================================================

        private static void DrawCorona(Vector2 drawPos, float R, float lifeT, float rg)
        {
            // HALO EXTERIOR: 1.30× → 2.35× R.
            float haloR = R * (1.30f + 1.05f * lifeT);
            float haloA = (70f + 125f * lifeT) / 255f;
            Color haloCol = Color.Lerp(new Color(255, 150, 55), new Color(255, 60, 25), rg) * haloA;
            float haloScale = haloR / (Glow.Width * 0.5f);
            Main.spriteBatch.Draw(Glow, drawPos, null, haloCol, 0f,
                Glow.Size() * 0.5f, haloScale, SpriteEffects.None, 0f);

            // CORONA INTERNA: 1.05× → 1.55× R.
            float corR = R * (1.05f + 0.50f * lifeT);
            float corA = (55f + 90f * lifeT) / 255f;
            Color corCol = Color.Lerp(new Color(255, 235, 170), new Color(255, 135, 90), rg) * corA;
            float corScale = corR / (Glow.Width * 0.5f);
            Main.spriteBatch.Draw(Glow, drawPos, null, corCol, 0f,
                Glow.Size() * 0.5f, corScale, SpriteEffects.None, 0f);
        }

        // ==================================================================
        //  EL SISTEMA DE ANILLOS — N anillos, cada uno en SU plano
        // ==================================================================

        /// <summary>
        /// Semieje mayor del anillo k (×R).
        /// </summary>
        private static float RingA(int k) => RingA0 + RingAStep * k;

        /// <summary>Achatado del anillo k (plano distinto por anillo).</summary>
        private static float RingFlat(int k) => 0.34f + 0.07f * (k % 3);

        /// <summary>
        /// Inclinación del plano del anillo k — y con PRECESIÓN (tier ≥ 7):
        /// el plano BAMBOLEA vivo alrededor de su valor base.
        /// </summary>
        private static float RingTilt(int k, float time, int tier)
        {
            float baseTilt = -0.55f + 0.20f * k;
            if (tier >= 7)
                baseTilt += 0.10f * (float)Math.Sin(time * (0.35f + 0.06f * k) + k * 1.9f);
            return baseTilt;
        }

        /// <summary>
        /// El GIRO del anillo k: ALTERNO (par horario, impar antihorario —
        /// "el otro debe rodear el sol en otra dirección"), con velocidad
        /// creciente por anillo.
        /// </summary>
        private static float RingSpin(int k) =>
            (k % 2 == 0 ? 1f : -1f) * (RingSpin0 + RingSpinStep * k);

        private static void DrawRingSystem(Vector2 center, float R, float time, int seed,
            int tier, float rg, int boltFlick, float lifeT)
        {
            int n = Math.Min(tier, MaxTier);
            float glyphScale = Math.Max(R / 52f, 0.25f) * 1.45f;

            for (int k = 0; k < n; k++)
            {
                float a = RingA(k) * R;
                float b = a * RingFlat(k);
                float tilt = RingTilt(k, time, tier);
                float spin = time * RingSpin(k);
                int runeCount = Runes0 + RuneStep * k;

                // El color del anillo: ORO / BLANCO-ESTELAR alternando, y
                // cada 3º AZUL-ESTELAR desde tier 7 (el sello frío).
                bool blue = tier >= 7 && k % 3 == 2;
                Color body = blue ? RuneBlue : RuneGold;
                Color tip = blue ? RuneBlueTip : RuneGoldTip;
                // La gigante final tiñe todo hacia el carmesí.
                if (rg > 0f) body = Color.Lerp(body, SunCrimson, rg * 0.45f);

                // --- 1. EL ARO ELÍPTICO: polilínea de cápsulas con PROFUNDIDAD
                //     (el frente de la órbita más brillante que la espalda) ---
                const int Segments = 30;
                Vector2 prev = EllipsePoint(center, a, b, tilt, spin);
                for (int s = 1; s <= Segments; s++)
                {
                    float t = spin + s / (float)Segments * MathHelper.TwoPi;
                    Vector2 pt = EllipsePoint(center, a, b, tilt, t);
                    Vector2 mid = (prev + pt) * 0.5f;
                    Vector2 delta = pt - prev;
                    float len = delta.Length();
                    if (len > 0.5f)
                    {
                        float rot = (float)Math.Atan2(delta.Y, delta.X);
                        // Profundidad: sin(t)·cos(tilt) > 0 → frente de la órbita.
                        float depth = 0.55f + 0.45f *
                            (float)Math.Sin(t + MathHelper.PiOver2) * (float)Math.Cos(tilt);
                        // Latido del aro (la energía recorre el anillo).
                        float pulse = 0.70f + 0.30f * (float)Math.Sin(time * 1.8f + k * 1.3f + s * 0.35f);
                        float fade = (1f - lifeT * 0.55f);
                        Capsule(mid, len, Math.Max(2.2f, 0.052f * R) * (1f + 0.35f * depth),
                            rot, Tint(body, (0.30f + 0.30f * depth) * pulse * fade));
                    }
                    prev = pt;
                }

                // --- 2. LAS RUNAS: glifos cabalgando la órbita, rotados a
                //     la TANGENTE (los glifos "andan" por el anillo) ---
                for (int g = 0; g < runeCount; g++)
                {
                    float ang = g / (float)runeCount * MathHelper.TwoPi + spin;
                    DrawRuneOnOrbit(center, a, b, tilt, ang, time, seed, k, g,
                        body, tip, glyphScale, rg, lifeT, tier);
                }
            }
        }

        /// <summary>Un glifo rúnico sobre SU órbita elíptica.</summary>
        private static void DrawRuneOnOrbit(Vector2 center, float a, float b, float tilt,
            float ang, float time, int seed, int k, int g,
            Color body, Color tip, float glyphScale, float rg, float lifeT, int tier)
        {
            // Flotación viva: el radio respira por glifo.
            float breathe = 1f + 0.045f * (float)Math.Sin(time * 1.35f + g * 0.9f + k * 0.5f);
            Vector2 glyphPos = EllipsePoint(center, a * breathe, b * breathe, tilt, ang);

            // La TANGENTE de la órbita en este punto: la runa cabalga de pie.
            float tanAng = tangentialAngle(a * breathe, b * breathe, tilt, ang);
            float glyphRot = tanAng + MathHelper.PiOver2; // la runa "de pie" sobre el aro

            // Latido de brillo propio.
            float pulse = 0.75f + 0.25f * (float)Math.Sin(time * 2.4f + g * 1.3f + k * 0.8f);
            float fade = 1f - lifeT * 0.55f;

            // Resplandor suave DETRÁS (el grabado ardiendo).
            Quad(Glow, glyphPos, new Vector2(34f * glyphScale, 34f * glyphScale), 0f,
                Tint(body, 0.20f * pulse * fade));

            // Los TRAZOS del glifo (rotados con la órbita).
            Vector2[] strokes = _runes[(g + k) % _runes.Length];
            for (int s = 0; s < strokes.Length; s += 2)
            {
                Vector2 localA = strokes[s] * glyphScale;
                Vector2 localB = strokes[s + 1] * glyphScale;
                // Rota el trazo al marco de la runa (sobre la tangente).
                Vector2 rotA = localA.RotatedBy(glyphRot) + glyphPos;
                Vector2 rotB = localB.RotatedBy(glyphRot) + glyphPos;
                Vector2 mid = (rotA + rotB) * 0.5f;
                Vector2 delta = rotB - rotA;
                float len = delta.Length();
                if (len < 0.01f) continue;
                float rot = (float)Math.Atan2(delta.Y, delta.X);

                // Gradiente vertical local: abajo cuerpo, arriba punta pálida.
                float localY = ((strokes[s].Y + strokes[s + 1].Y) * 0.5f + 7f) / 14f;
                Color col = Color.Lerp(tip, body, 1f - localY * 0.25f);

                Capsule(mid, len, 3.3f * glyphScale, rot, Tint(col, 0.85f * pulse * fade));
            }

            // PERLA sobre el glifo (la gema del sello).
            Vector2 pearlPos = glyphPos + new Vector2(0f, -11.5f * glyphScale).RotatedBy(glyphRot);
            Quad(Glow, pearlPos, new Vector2(7.0f * glyphScale, 7.0f * glyphScale), 0f,
                Tint(body, 0.60f * pulse * fade));
            Quad(Glow, pearlPos, new Vector2(3.2f * glyphScale, 3.2f * glyphScale), 0f,
                Tint(tip, 0.9f * pulse * fade));
        }

        // ==================================================================
        //  TABLA DE GLIFOS — la ESCRITURA SOLAR (8 diseños originales)
        // ==================================================================

        /// <summary>
        /// Cada runa es una lista de TRAZOS (pares de puntos en espacio
        /// local ~11×15). Ocho diseños angulares de estilo ASTRO RÚNICO:
        /// soles, llamas, ruedas y puertas — la escritura del sello que
        /// viste a las copias del sol.
        /// </summary>
        private static readonly Vector2[][] _runes = new Vector2[][]
        {
            // R0 — EL ASTRO (el punto de luz con rayos)
            new Vector2[] { new(0f, -4.5f), new(0f, 4.5f), new(-4.5f, 0f), new(4.5f, 0f), new(-3f, -3f), new(-1.2f, -1.2f), new(3f, -3f), new(1.2f, -1.2f), new(-3f, 3f), new(-1.2f, 1.2f), new(3f, 3f), new(1.2f, 1.2f) },
            // R1 — LA LLAMA VIVA
            new Vector2[] { new(0f, 6.5f), new(0f, 1f), new(0f, 1f), new(-3f, -2f), new(-3f, -2f), new(0f, -5f), new(0f, -5f), new(3f, -2f), new(3f, -2f), new(0f, 1f), new(-1.5f, -6.5f), new(1.5f, -6.5f) },
            // R2 — LA RUEDA SOLAR
            new Vector2[] { new(0f, -5f), new(0f, 5f), new(-5f, 0f), new(5f, 0f), new(-3.5f, -3.5f), new(3.5f, 3.5f), new(3.5f, -3.5f), new(-3.5f, 3.5f), new(-2.2f, 0f), new(2.2f, 0f), new(0f, -2.2f), new(0f, 2.2f) },
            // R3 — LA ESPIGA DE LUZ
            new Vector2[] { new(0f, -7f), new(0f, 7f), new(-3.2f, -3.5f), new(0f, -0.5f), new(3.2f, -3.5f), new(0f, -0.5f), new(-3.2f, 3.5f), new(0f, 0.5f), new(3.2f, 3.5f), new(0f, 0.5f) },
            // R4 — LA PUERTA DEL DÍA
            new Vector2[] { new(-3.5f, 7f), new(-3.5f, -5f), new(-3.5f, -5f), new(0f, -7f), new(0f, -7f), new(3.5f, -5f), new(3.5f, -5f), new(3.5f, 7f), new(-3.5f, 7f), new(3.5f, 7f), new(0f, -4f), new(0f, 7f) },
            // R5 — LA CORONA BAJA
            new Vector2[] { new(-4f, 5f), new(-4f, -2f), new(-4f, -2f), new(-1.5f, -5.5f), new(-1.5f, -5.5f), new(0f, -1.5f), new(0f, -1.5f), new(1.5f, -5.5f), new(1.5f, -5.5f), new(4f, -2f), new(4f, -2f), new(4f, 5f), new(-4f, 5f), new(4f, 5f) },
            // R6 — EL TRAZO DEL COMETA
            new Vector2[] { new(-4f, 6.5f), new(3f, -1f), new(3f, -1f), new(0f, -6.5f), new(0f, -6.5f), new(4f, -3f), new(1.5f, 2f), new(4.5f, 1.5f), new(-1.5f, 1f), new(1.5f, 4f) },
            // R7 — EL SIGILO SOLAR (el sello maestro)
            new Vector2[] { new(0f, -6.5f), new(-4f, 0f), new(-4f, 0f), new(0f, 6.5f), new(0f, 6.5f), new(4f, 0f), new(4f, 0f), new(0f, -6.5f), new(-2.2f, 0f), new(2.2f, 0f), new(0f, -4f), new(0f, 4f) },
        };

        // ==================================================================
        //  LAS MEJORAS PROGRESIVAS (una capa nueva por tier)
        // ==================================================================

        /// <summary>Tier 2 — CHISPAS orbitales viajando por los anillos.</summary>
        private static void DrawOrbitSparks(Vector2 center, float R, float time, int seed, int tier)
        {
            int sparks = Math.Min(2 + tier / 2, 7);
            for (int i = 0; i < sparks; i++)
            {
                // Cada chispa vive en el anillo (i % anillos) a su velocidad.
                int k = i % Math.Min(tier, MaxTier);
                float a = RingA(k) * R;
                float b = a * RingFlat(k);
                float tilt = RingTilt(k, time, tier);
                // Va CONTRA el giro del anillo (la chispa es rápida y propia).
                float t = -time * (0.9f + 0.14f * i) + i * 2.3f;
                Vector2 pos = EllipsePoint(center, a, b, tilt, t);
                Vector2 ahead = EllipsePoint(center, a, b, tilt, t - 0.16f);

                float twinkle = 0.65f + 0.35f * (float)Math.Sin(time * 7f + i * 2.1f);

                // Estela corta detrás de la chispa.
                Vector2 seg = pos - ahead;
                float len = seg.Length();
                if (len > 0.5f)
                {
                    float rot = (float)Math.Atan2(seg.Y, seg.X);
                    Capsule((pos + ahead) * 0.5f, len, Math.Max(2.0f, 0.045f * R), rot,
                        Tint(SunGold, 0.45f * twinkle));
                }

                // Halo + núcleo blanco.
                Quad(Glow, pos, new Vector2(0.55f * R, 0.55f * R), 0f, Tint(SunGold, 0.35f * twinkle));
                Quad(Glow, pos, new Vector2(0.22f * R, 0.22f * R), 0f, Tint(WhiteIncan, 0.85f * twinkle));
            }
        }

        /// <summary>Tier 3 — DESTELLOS de 4 puntas alrededor del sistema.</summary>
        private static void DrawTwinkles(Vector2 center, float R, float time, int seed, int tier)
        {
            int count = 4 + tier;
            for (int i = 0; i < count; i++)
            {
                float h = Hash01(seed, 300 + i, 7);
                float life = (time * 0.55f + h * 3f) % 1f;
                float bright = (float)Math.Sin(life * Math.PI); // nace, brilla, muere
                if (bright < 0.05f) continue;

                float ang = Hash01(seed, 310 + i, 11) * MathHelper.TwoPi + time * 0.07f;
                float dist = (1.35f + 1.9f * Hash01(seed, 320 + i, 13)) * R;
                Vector2 pos = center + new Vector2(
                    (float)Math.Cos(ang) * dist, (float)Math.Sin(ang) * dist * 0.72f);

                float size = (0.30f + 0.25f * Hash01(seed, 330 + i, 17)) * R * bright;
                // EL DESTELLO DE 4 PUNTAS: dos cápsulas cruzadas + núcleo.
                Capsule(pos, size * 2.2f, Math.Max(1.6f, 0.035f * R) * bright, 0f,
                    Tint(WhiteIncan, 0.55f * bright));
                Capsule(pos, size * 2.2f, Math.Max(1.6f, 0.035f * R) * bright, MathHelper.PiOver2,
                    Tint(WhiteIncan, 0.55f * bright));
                Quad(Glow, pos, new Vector2(size * 1.5f, size * 1.5f), 0f,
                    Tint(SunGold, 0.50f * bright));
            }
        }

        /// <summary>Tier 4 — PROMINENCIAS: arcos de plasma saltando del limbo.</summary>
        private static void DrawProminences(Vector2 center, float R, float time, int seed,
            int boltFlick, int tier)
        {
            int count = Math.Min(2 + tier / 3, 5);
            for (int i = 0; i < count; i++)
            {
                if (!LightningCore.Flicker(seed + 500 + i * 23, boltFlick, 0.72f)) continue;

                // El arco nace del limbo y CAE de vuelta (un bucle solar).
                float baseAng = time * (0.30f + 0.08f * i) + i * 2.7f +
                                Hash01(seed, 510 + i, boltFlick / 3) * 0.9f;
                Vector2 start = center + new Vector2(
                    (float)Math.Cos(baseAng) * R * 1.02f,
                    (float)Math.Sin(baseAng) * R * 1.02f * 0.92f);
                float loopH = (1.45f + 0.55f * Hash01(seed, 520 + i, boltFlick)) * R;
                Vector2 mid = center + new Vector2(
                    (float)Math.Cos(baseAng + 0.30f) * loopH,
                    (float)Math.Sin(baseAng + 0.30f) * loopH * 0.9f);
                Vector2 end = center + new Vector2(
                    (float)Math.Cos(baseAng + 0.65f) * R * 1.02f,
                    (float)Math.Sin(baseAng + 0.65f) * R * 1.02f * 0.92f);

                // El bucle serpenteante: ancla → cima → ancla, suavizado.
                var path = new Vector2[] { start, mid, end };
                Vector2[] smooth = LightningCore.Smooth(path, 5);
                Vector2[] jitter = LightningCore.JitterPath(smooth, seed + 530 + i, boltFlick,
                    Math.Max(5f, 0.10f * R));
                LightningCore.Bolt(Main.spriteBatch, jitter, seed + 540 + i, boltFlick,
                    Math.Max(2.8f, 0.055f * R),
                    Tint(SunAmber, 0.55f), Tint(WhiteIncan, 0.92f), 1f, 0.32f);
            }
        }

        /// <summary>Tier 5 — VIENTO SOLAR: partículas subiendo radialmente.</summary>
        private static void DrawSolarWind(Vector2 center, float R, float time, int seed, int tier)
        {
            int count = 10 + tier;
            for (int i = 0; i < count; i++)
            {
                float h = Hash01(seed, 600 + i, 19);
                float life = (time * 0.22f + h) % 1f;
                float ang = Hash01(seed, 610 + i, 23) * MathHelper.TwoPi + time * 0.04f;
                float dist = (1.10f + life * (1.6f + 1.3f * R / BodyPx)) * R;
                Vector2 pos = center + new Vector2(
                    (float)Math.Cos(ang) * dist, (float)Math.Sin(ang) * dist * 0.8f);

                float bright = (float)Math.Sin(life * Math.PI) * (0.5f + 0.5f * Hash01(seed, 620 + i, 29));
                if (bright < 0.04f) continue;

                // Púa de luz radial (una cápsula apuntando afuera).
                float rot = (float)Math.Atan2(pos.Y - center.Y, pos.X - center.X);
                float len = Math.Max(4f, 0.16f * R) * bright;
                Capsule(pos + new Vector2((float)Math.Cos(rot), (float)Math.Sin(rot)) * len * 0.5f,
                    len, Math.Max(1.4f, 0.028f * R), rot, Tint(SunGold, 0.40f * bright));
                Quad(Glow, pos, new Vector2(0.16f * R, 0.16f * R), 0f,
                    Tint(WhiteIncan, 0.55f * bright));
            }
        }

        /// <summary>Tier 6 — RAYOS FUGITIVOS entre anillos (LightningCore).</summary>
        private static void DrawInterRingBolts(Vector2 center, float R, float time, int seed,
            int boltFlick, int tier)
        {
            int n = Math.Min(tier, MaxTier);
            int count = Math.Min(1 + tier / 3, 4);
            for (int i = 0; i < count; i++)
            {
                if (!LightningCore.Flicker(seed + 700 + i * 31, boltFlick, 0.80f)) continue;

                // Del anillo k al anillo k+2 (saltando uno — el chispazo cruzado).
                int k = (i * 2 + (boltFlick / 2)) % Math.Max(1, n - 2);
                int k2 = Math.Min(k + 2, n - 1);

                float a1 = RingA(k) * R, b1 = a1 * RingFlat(k);
                float t1 = RingTilt(k, time, tier);
                float spin1 = time * RingSpin(k);
                float a2 = RingA(k2) * R, b2 = a2 * RingFlat(k2);
                float t2 = RingTilt(k2, time, tier);
                float spin2 = time * RingSpin(k2);

                float ang1 = Hash01(seed, 710 + i, boltFlick) * MathHelper.TwoPi + spin1;
                float ang2 = Hash01(seed, 720 + i, boltFlick) * MathHelper.TwoPi + spin2;
                Vector2 start = EllipsePoint(center, a1, b1, t1, ang1);
                Vector2 end = EllipsePoint(center, a2, b2, t2, ang2);

                // El color del rayo sigue el anillo DESTINO (oro o azul).
                bool blue = tier >= 7 && k2 % 3 == 2;
                Color halo = blue ? Tint(StarBlue, 0.58f) : Tint(SunGold, 0.58f);

                LightningCore.Bolt(Main.spriteBatch, start, end,
                    seed + 730 + i * 53, boltFlick,
                    Math.Max(2.6f, 0.05f * R),
                    halo, Tint(WhiteIncan, 0.95f),
                    1f, 6, Math.Max(9f, 0.15f * R));
            }
        }

        /// <summary>Tier 8 — NÚCLEO PULSANTE (el corazón late).</summary>
        private static void DrawCorePulse(Vector2 center, float R, float time, int tier)
        {
            float pulse = 0.5f + 0.5f * (float)Math.Sin(time * 2.6f);
            float size = R * (0.9f + 0.35f * pulse);
            Quad(Glow, center, new Vector2(size, size), 0f,
                Tint(new Color(255, 245, 210), 0.22f * pulse));
        }

        /// <summary>Tier 8 — ONDAS DE ECO expandiéndose desde el cuerpo.</summary>
        private static void DrawEchoWaves(Vector2 center, float R, float time, int seed)
        {
            const int Waves = 2;
            const float Cycle = 2.2f;
            for (int w = 0; w < Waves; w++)
            {
                float phase = ((time / Cycle) + w / (float)Waves) % 1f;
                float radius = (1.10f + phase * 1.55f) * R;
                float fade = (1f - phase) * (1f - phase);
                RingQuad(center, radius, phase * 3.1f + w * 1.7f,
                    Tint(w % 2 == 0 ? SunGold : WhiteIncan, 0.26f * fade));
            }
        }

        /// <summary>Tier 9 — CORONA DE PÉTALOS de plasma orbitando el cuerpo.</summary>
        private static void DrawPlasmaPetals(Vector2 center, float R, float time, int seed, int tier)
        {
            int petals = 8 + tier / 2;
            for (int i = 0; i < petals; i++)
            {
                float ang = i / (float)petals * MathHelper.TwoPi + time * 0.42f;
                // El pétalo CABALGA cerca del limbo, inclinado al movimiento.
                Vector2 basePos = center + new Vector2(
                    (float)Math.Cos(ang) * R * 0.96f,
                    (float)Math.Sin(ang) * R * 0.96f * 0.88f);
                float rot = (float)Math.Atan2(basePos.Y - center.Y, basePos.X - center.X);

                // La llama del pétalo se ESTIRA hacia afuera (el filo solar).
                float flame = 0.6f + 0.4f * (float)Math.Sin(time * 2.2f + i * 1.4f);
                float len = (0.55f + 0.35f * flame) * R;
                Vector2 dir = new Vector2((float)Math.Cos(rot), (float)Math.Sin(rot));
                Vector2 mid = basePos + dir * len * 0.5f;

                Capsule(mid, len, Math.Max(3f, 0.085f * R), rot,
                    Tint(Color.Lerp(SunGold, SunAmber, 0.5f), 0.34f * flame));
                Capsule(mid, len * 0.55f, Math.Max(1.8f, 0.045f * R), rot,
                    Tint(WhiteIncan, 0.42f * flame));
            }
        }

        /// <summary>Tier 10 — LA ERUPCIÓN RÚNICA: runas desprendiéndose y volando.</summary>
        private static void DrawRunicEruption(Vector2 center, float R, float time, int seed,
            int boltFlick)
        {
            const float Cycle = 1.6f; // una runa cada 1.6 s
            for (int i = 0; i < 2; i++)
            {
                float phase = ((time / Cycle) + i * 0.5f) % 1f;
                float bright = (float)Math.Sin(phase * Math.PI);
                if (bright < 0.05f) continue;

                // Qué glifo y de qué anillo (determinista por ciclo).
                int cycle = (int)(time / Cycle) + i;
                int glyph = (int)(Hash01(seed, 900 + cycle, 3) * _runes.Length) % _runes.Length;
                int ring = Math.Max(0, Math.Min(MaxTier - 1,
                    (int)(Hash01(seed, 910 + cycle, 5) * 4)));
                int idx = (int)(Hash01(seed, 920 + cycle, 7) * (Runes0 + RuneStep * ring));

                float a = RingA(ring) * R, b = a * RingFlat(ring);
                float tilt = RingTilt(ring, time, MaxTier);
                float ang = idx / (float)(Runes0 + RuneStep * ring) * MathHelper.TwoPi +
                            time * RingSpin(ring);

                // Nace EN el anillo y VUELA radialmente hacia afuera.
                Vector2 origin = EllipsePoint(center, a, b, tilt, ang);
                Vector2 outward = origin - center;
                if (outward.LengthSquared() < 0.01f) outward = Vector2.UnitX;
                outward.Normalize();
                Vector2 pos = origin + outward * phase * (0.9f * R);

                float glyphScale = Math.Max(R / 52f, 0.25f) * 1.35f;
                float glyphRot = phase * 3.5f; // gira mientras vuela

                // Resplandor + trazos del glifo fugitivo.
                Quad(Glow, pos, new Vector2(40f * glyphScale, 40f * glyphScale), 0f,
                    Tint(RuneGold, 0.28f * bright));
                Vector2[] strokes = _runes[glyph];
                for (int s = 0; s < strokes.Length; s += 2)
                {
                    Vector2 rotA = strokes[s] * glyphScale * (0.8f + 0.4f * phase);
                    Vector2 rotB = strokes[s + 1] * glyphScale * (0.8f + 0.4f * phase);
                    rotA = rotA.RotatedBy(glyphRot) + pos;
                    rotB = rotB.RotatedBy(glyphRot) + pos;
                    Vector2 mid = (rotA + rotB) * 0.5f;
                    Vector2 delta = rotB - rotA;
                    float len = delta.Length();
                    if (len < 0.01f) continue;
                    float rot = (float)Math.Atan2(delta.Y, delta.X);
                    Capsule(mid, len, 3.0f * glyphScale, rot, Tint(RuneGoldTip, 0.85f * bright));
                }

                // La ESTELA del vuelo (rastro dorado).
                Vector2 behind = origin + outward * (phase - 0.12f) * (0.9f * R);
                Vector2 seg = pos - behind;
                float segLen = seg.Length();
                if (segLen > 0.5f)
                {
                    Capsule((pos + behind) * 0.5f, segLen, Math.Max(2.5f, 0.05f * R),
                        (float)Math.Atan2(seg.Y, seg.X), Tint(SunGold, 0.40f * bright));
                }
            }
        }

        /// <summary>Tier 10 — JETS POLARES con rayo interno.</summary>
        private static void DrawPolarJets(Vector2 center, float R, float time, int seed,
            int boltFlick)
        {
            float pulse = 0.7f + 0.3f * (float)Math.Sin(time * 1.9f);
            for (int side = 0; side < 2; side++)
            {
                Vector2 dir = side == 0 ? -Vector2.UnitY : Vector2.UnitY;
                float rot = (float)Math.Atan2(dir.Y, dir.X);
                Vector2 basePos = center + dir * (R * 0.85f);

                // El chorro: 3 tramos ahusados con brillo VIAJANDO hacia afuera.
                const int Tramos = 3;
                for (int k = 0; k < Tramos; k++)
                {
                    float f0 = k / (float)Tramos, f1 = (k + 1) / (float)Tramos;
                    float midF = (f0 + f1) * 0.5f;
                    Vector2 a = center + dir * (R * 0.85f + f0 * 1.45f * R);
                    Vector2 b = center + dir * (R * 0.85f + f1 * 1.45f * R);
                    Vector2 mid = (a + b) * 0.5f;
                    float len = (b - a).Length();
                    float w = (0.26f - 0.18f * midF) * R;
                    float wave = 0.5f + 0.5f * (float)Math.Sin(midF * 7f - time * 6f);
                    Color c = Color.Lerp(WhiteIncan, SunGold, midF * 0.7f);
                    Capsule(mid, len, w * 2.0f, rot,
                        Tint(c, (0.18f + 0.16f * wave) * pulse * (1f - midF * 0.4f)));
                    Capsule(mid, len, w, rot,
                        Tint(c, (0.42f + 0.32f * wave) * pulse * (1f - midF * 0.4f)));
                }

                // ⚡ EL RAYO dentro del chorro (la espina eléctrica del jet).
                if (LightningCore.Flicker(seed + 950 + side * 13, boltFlick, 0.80f))
                {
                    Vector2 tip = center + dir * (R * 0.85f + 1.35f * R);
                    LightningCore.Bolt(Main.spriteBatch, basePos, tip,
                        seed + 960 + side * 29, boltFlick,
                        Math.Max(2.4f, 0.045f * R),
                        Tint(SunGold, 0.50f), Tint(WhiteIncan, 0.90f),
                        1f, 6, Math.Max(8f, 0.12f * R));
                }

                // Punta incandescente del polo.
                Vector2 jetTip = center + dir * (R * 0.85f + 1.45f * R);
                Quad(Glow, jetTip, new Vector2(0.50f * R, 0.50f * R), 0f,
                    Tint(WhiteIncan, 0.42f * pulse));
            }
        }

        // ==================================================================
        //  HELPERS DE DIBUJO (patrón validado del proyecto)
        // ==================================================================

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

        private static void Quad(Texture2D tex, Vector2 pos, Vector2 size, float rot, Color tint)
        {
            if (tint.A == 0) return;
            Main.spriteBatch.Draw(tex, pos, null, tint, rot,
                new Vector2(tex.Width, tex.Height) * 0.5f,
                size / new Vector2(tex.Width, tex.Height),
                SpriteEffects.None, 0f);
        }

        private static void Capsule(Vector2 mid, float len, float width, float rot, Color tint)
        {
            if (tint.A == 0) return;
            Quad(Glow, mid, new Vector2(len + width, width * 1.9f), rot, tint);
        }

        private static void RingQuad(Vector2 pos, float visibleRadius, float rot, Color tint)
        {
            if (tint.A == 0) return;
            Quad(Ring, pos, VFXCore.RingQuadSize(visibleRadius), rot, tint);
        }

        /// <summary>Punto de la elipse orbital (incluida por tilt).</summary>
        private static Vector2 EllipsePoint(Vector2 center, float a, float b, float tilt, float t)
        {
            float ct = (float)Math.Cos(t), st = (float)Math.Sin(t);
            Vector2 local = new Vector2(a * ct, b * st);
            float cR = (float)Math.Cos(tilt), sR = (float)Math.Sin(tilt);
            return center + new Vector2(local.X * cR - local.Y * sR, local.X * sR + local.Y * cR);
        }

        /// <summary>
        /// Ángulo de la TANGENTE de la elipse (para rotar las runas que la
        /// cabalgan): derivada del punto respecto de t, proyectada al plano.
        /// </summary>
        private static float tangentialAngle(float a, float b, float tilt, float t)
        {
            Vector2 dLocal = new Vector2(-a * (float)Math.Sin(t), b * (float)Math.Cos(t));
            float cR = (float)Math.Cos(tilt), sR = (float)Math.Sin(tilt);
            Vector2 d = new Vector2(dLocal.X * cR - dLocal.Y * sR, dLocal.X * sR + dLocal.Y * cR);
            return (float)Math.Atan2(d.Y, d.X);
        }

        /// <summary>Hash determinista [0,1).</summary>
        private static float Hash01(int seed, int a, int b)
        {
            int h = unchecked(seed * 374761393 + a * 668265263 + b * 1911520717);
            h = unchecked(h ^ (h >> 13));
            h = unchecked(h * 1274126177);
            h = unchecked(h ^ (h >> 16));
            return (h & 0xFFFFFF) / 16777216f;
        }

        /// <summary>Tinte de INTENSIDAD LINEAL (patrón validado del proyecto).</summary>
        private static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color(c.R, c.G, c.B, (byte)(int)(255f * f));
        }

        // ==================================================================
        //  SHADERS (carga diferida con fallback — patrón del sol)
        // ==================================================================

        private static void LoadShaders()
        {
            if (!_sunShaderFailed && _sunShader == null)
            {
                try
                {
                    _sunShader = new Ref<Effect>(ModContent.Request<Effect>(
                        "AethonMod/Content/Effects/Shaders/SunShader",
                        AssetRequestMode.ImmediateLoad).Value);
                }
                catch { _sunShaderFailed = true; }
            }
            if (!_shineShaderFailed && _shineShader == null)
            {
                try
                {
                    _shineShader = new Ref<Effect>(ModContent.Request<Effect>(
                        "AethonMod/Content/Effects/Shaders/RadialShineShader",
                        AssetRequestMode.ImmediateLoad).Value);
                }
                catch { _shineShaderFailed = true; }
            }
        }
    }
}
