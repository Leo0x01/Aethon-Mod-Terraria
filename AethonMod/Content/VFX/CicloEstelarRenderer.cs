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
    /// CicloEstelarRenderer — v6.26 — EL BASTÓN DEL CICLO ESTELAR.
    ///
    /// Petición del usuario: "crea un baston que simule el ciclo de vida
    /// completo de una estrella que se convierte en super nova y luego lo
    /// que siga en su ciclo de vida".
    ///
    /// EL PROYECTIL ES UNA ESTRELLA QUE VIVE UNA VIDA COMPLETA (~18 s)
    /// EN CINCO ACTOS — cada uno con su RENDER y su FÍSICA propios:
    ///
    ///   ACTO I · NEBULOSA (~4 s) — la nube molecular PROTOSTELAR:
    ///     BrumaFX.Cloud grande girando lento (paleta fría violeta/azul),
    ///     motas de polvo cayendo al centro (BrumaFX.Wisps espiralando),
    ///     el centro CONTRAYÉNDOSE (0.5→0.8) con destellos tenues del
    ///     proto-núcleo. Casi sin luz.
    ///
    ///   ACTO II · SECUENCIA PRINCIPAL (~6 s) — LA IGNICIÓN: flash de
    ///     encendido (ImpactFlash + Kick 6 en el proyectil) y la estrella
    ///     VIVE con la MISMA TÉCNICA del sol de la casa (backglow
    ///     BloomCircleSmall + aura RadialShineShader + disco SunShader de
    ///     granulación dendrítica — patrón copiado de RuneSunRenderer
    ///     secciones 0-3 con parámetros PROPIOS, sin llamar a su Draw)
    ///     + 3 anillos rúnicos tenues (EmitRingSystem tier 3, alpha 0.5).
    ///
    ///   ACTO III · GIGANTE ROJA (~3.5 s) — la estrella ENVEJECE: se
    ///     hincha ×2.2 y ENROJECE (rampa SolarFire lerp hacia rojos),
    ///     celdas de convección (la lengua de PyraLib rotada al limbo)
    ///     latiendo lento a 0.2 Hz, y PIERDE CAPAS: BrumaFX.Puff
    ///     desprendiéndose hacia afuera (la nebulosa planetaria). La luz
    ///     del mundo se tiñe de rojo (el proyectil la enciende).
    ///
    ///   ACTO IV · COLAPSO + SUPERNOVA (~1 s) — EL COLAPSO: 15 ticks de
    ///     implosión (todo se encoge ×0.3 con anillo OndaLib.Pulse
    ///     contráctil + silencio de luz) → LA SUPERNOVA: OndaLib.Shock
    ///     cromático ×2 (400/650 px), llamaradas radiales SolarFire ×12,
    ///     StormLib.MultiBolt fugitivos ×8, BrumaFX.Cloud expansivo e
    ///     ImpactFlash 130 px CONCENTRADO (PROHIBIDO el Flash de pantalla
    ///     completa — la lección v6.26 de la casa).
    ///
    ///   ACTO V · EL REMANENTE (~3.5 s) — "lo que siga en su ciclo de
    ///     vida": del corazón de la nova queda UNA ESTRELLA DE NEUTRONES
    ///     enana (núcleo 10 px blanco-azulado chispeante a 15 Hz con
    ///     líneas de campo dipolares r = L·sen²θ — técnica propia
    ///     inspirada en NeutronStarRenderer, código propio) que PULSA
    ///     (mini-haz girando rápido) 2.5 s y luego se APAGA (fade).
    ///
    /// LA ESTELA de todo el ciclo (EstelaLib.Track + Ribbon perfil
    /// Comet) cuenta la historia del camino: color por acto
    /// violeta → dorado → rojo → blanco → azul.
    ///
    /// CONTRATO DE BATCH: Draw() exige el SpriteBatch CERRADO y lo deja
    /// CERRADO (el llamador restaura el batch de tML — contrato v6.10).
    /// La MASA (bruma) va en AlphaBlend PRIMERO; los brillos en Additive.
    /// </summary>
    public static class CicloEstelarRenderer
    {
        // ==================================================================
        //  LA LÍNEA DE TIEMPO DEL CICLO (ticks — compartida con el proyectil)
        // ==================================================================

        /// <summary>Fin del ACTO I (nebulosa): 240 ticks = 4 s.</summary>
        public const int Act1End = 240;

        /// <summary>Fin del ACTO II (secuencia principal): 600 = 6 s.</summary>
        public const int Act2End = 600;

        /// <summary>Fin del ACTO III (gigante roja): 810 = 3.5 s.</summary>
        public const int Act3End = 810;

        /// <summary>Fin del COLAPSO (acto IV-a): 825 = 15 ticks de implosión.</summary>
        public const int CollapseEnd = 825;

        /// <summary>Fin de la SUPERNOVA (acto IV-b): 870 = 45 ticks de estallido.</summary>
        public const int NovaEnd = 870;

        /// <summary>Vida total del ciclo: 1080 ticks = 18 s.</summary>
        public const int TotalTicks = 1080;

        /// <summary>Duración del flash de IGNICIÓN (inicio del acto II).</summary>
        public const int IgniteFlashTicks = 24;

        // ==================================================================
        //  EL CUERPO DE LA ESTRELLA
        // ==================================================================

        /// <summary>Radio del disco solar en la secuencia principal (px).</summary>
        public const float BodyPx = 40f;

        /// <summary>Radio del REMANENTE — la estrella de neutrones enana (px).</summary>
        public const float RemnantPx = 10f;

        // ==================================================================
        //  LAS PALETAS DEL CICLO (una identidad por acto)
        // ==================================================================

        // --- I · NEBULOSA: el frío violeta de la nube molecular ---
        private static readonly Color NebVioleta = new(122, 88, 178);      // masa exterior
        private static readonly Color NebAzul = new(70, 95, 175);          // masa profunda
        private static readonly Color NebCorazon = new(150, 120, 210);     // el corazón denso
        private static readonly Color NebCorazonCaliente = new(215, 190, 255); // proto-núcleo
        private static readonly Color PolvoEstelar = new(190, 210, 255);   // las motas

        // --- II · SECUENCIA PRINCIPAL: la paleta regia del sol ---
        private static readonly Color SolHalo = new(255, 160, 60);
        private static readonly Color SolCorona = new(255, 235, 175);
        private static readonly Color SolShine = new(252, 214, 112);

        // --- III · GIGANTE ROJA: los rojos del coloso ---
        private static readonly Color GiganteHalo = new(255, 90, 40);
        private static readonly Color GiganteCorona = new(255, 150, 90);
        private static readonly Color GiganteShine = new(255, 95, 45);
        private static readonly Color CapaRosa = new(205, 120, 165);       // nebulosa planetaria
        private static readonly Color CapaTeal = new(95, 165, 170);        // el oxígenio ionizado

        // --- IV · SUPERNOVA: el blanco del estallido ---
        private static readonly Color NovaBlanco = new(255, 252, 245);
        private static readonly Color NovaHumo = new(150, 95, 120);        // la masa eyectada
        private static readonly Color NovaBrasa = new(255, 110, 55);

        // --- V · REMANENTE: el azul ultradenso del cadáver ---
        private static readonly Color NucleoAzul = new(150, 195, 255);
        private static readonly Color NucleoBlanco = new(240, 246, 255);
        private static readonly Color CianCampo = new(110, 230, 235);
        private static readonly Color VioletaCampo = new(145, 125, 245);

        // --- LA ESTELA (la historia del camino, un color por acto) ---
        private static readonly Color[] EstelaColores =
        {
            new Color(168, 128, 255),   // I   · nebulosa violeta
            new Color(255, 196, 88),    // II  · secuencia dorada
            new Color(255, 92, 56),     // III · gigante roja
            new Color(248, 250, 255),   // IV  · nova blanca
            new Color(150, 202, 255),   // V   · remanente azul
        };

        // ==================================================================
        //  PINCELES (la biblioteca de texturas del proyecto)
        // ==================================================================

        private static Asset<Texture2D> _glow;
        private static Asset<Texture2D> _orb;
        private static Asset<Texture2D> _ring;
        // v6.50.3 — FIX (4 lookups de asset por estrella por frame): el mismo
        // cache de la casa (barrido en Unload por reflexión — v6.49).
        private static Asset<Texture2D> _bloom;
        private static Asset<Texture2D> _wavy;
        private static Asset<Texture2D> _psy;
        private static Asset<Texture2D> _dend;

        private static Texture2D Glow =>
            (_glow ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow")).Value;

        private static Texture2D Orb =>
            (_orb ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/GlowOrb")).Value;

        private static Texture2D Ring =>
            (_ring ??= ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Ring")).Value;

        // Los shaders del sol (las TÉCNICAS del proyecto — el sol original
        // no se toca: se copia el patrón de carga con fallback).
        private static Ref<Effect> _sunShader;
        private static Ref<Effect> _shineShader;
        private static bool _sunShaderFailed;
        private static bool _shineShaderFailed;

        // ==================================================================
        //  EL RENDER PRINCIPAL — batch CERRADO → CERRADO
        // ==================================================================

        /// <summary>
        /// Dibuja EL CICLO COMPLETO: `age` = edad del proyectil en ticks
        /// (0..TotalTicks), `seed` = semilla determinista del disparo.
        /// El acto activo se deriva de la línea de tiempo propia.
        /// </summary>
        public static void Draw(Projectile p, float age, int seed)
        {
            LoadShaders();

            try
            {
                Vector2 drawPos = p.Center - Main.screenPosition;
                float time = Main.GlobalTimeWrappedHourly;
                float lifeT = MathHelper.Clamp(age / TotalTicks, 0f, 1f);
                int act = ActOf(age);

                // === LOS PROGRESOS DE CADA ACTO (0..1 dentro de su tramo) ===
                float a1 = Utils.GetLerpValue(0f, Act1End, age, true);
                float a3 = Utils.GetLerpValue(Act2End, Act3End, age, true);
                float col = Utils.GetLerpValue(Act3End, CollapseEnd, age, true);
                float nova = Utils.GetLerpValue(CollapseEnd, NovaEnd, age, true);

                // === 0. LA ESTELA (la historia del camino — TODOS los actos) ===
                DrawEstela(p, act, age, time, seed);

                switch (act)
                {
                    case 1:
                        DrawNebulosa(drawPos, a1, time, seed);
                        break;

                    case 2:
                        {
                            // LA IGNICIÓN: los primeros ticks del acto II
                            // son el FLASH DE ENCENDIDO de la estrella.
                            float ignite = Utils.GetLerpValue(
                                Act1End, Act1End + IgniteFlashTicks, age, true);
                            float R = BodyPx * Math.Max(p.scale, 0.05f);
                            DrawSunBody(p, drawPos, R, 0f, ignite, lifeT, time, seed);
                            DrawVidaSuperficie(drawPos, R, time, seed, ignite);
                        }
                        break;

                    case 3:
                        {
                            // LA GIGANTE: el mismo cuerpo ENROJECIDO e
                            // hinchado (la escala la trae p.scale ×2.2) +
                            // las capas que pierde + la convección.
                            float R = BodyPx * Math.Max(p.scale, 0.05f);
                            DrawCapasPerdidas(drawPos, R, a3, time, seed);
                            DrawSunBody(p, drawPos, R, a3, 1f, lifeT, time, seed);
                            DrawConveccion(drawPos, R, a3, time, seed);
                        }
                        break;

                    case 4:
                        if (col < 1f)
                        {
                            // IV-a · EL COLAPSO: 15 ticks cayendo al centro.
                            float Rg = BodyPx * Math.Max(p.scale, 0.05f);
                            DrawColapso(drawPos, Rg, col, time, seed);
                        }
                        else
                        {
                            // IV-b · LA SUPERNOVA: el estallido total.
                            DrawNova(drawPos, nova, time, seed);
                        }
                        break;

                    case 5:
                        DrawRemanente(drawPos, age, time, seed);
                        break;
                }
            }
            catch
            {
                // Cierre defensivo SOLO en el path de error (contrato v6.10).
                try { Main.spriteBatch.End(); } catch { }
            }
        }

        /// <summary>El acto activo (1..5) para una edad dada.</summary>
        public static int ActOf(float age)
        {
            if (age < Act1End) return 1;
            if (age < Act2End) return 2;
            if (age < Act3End) return 3;
            if (age < NovaEnd) return 4;     // colapso (aún) o nova
            return 5;
        }

        // ==================================================================
        //  LA ESTELA — la historia del camino (color por acto)
        // ==================================================================

        private static void DrawEstela(Projectile p, int act, float age, float time, int seed)
        {
            Vector2[] camino = EstelaLib.Track(p.whoAmI, 24).Points();
            if (camino == null || camino.Length < 3) return;
            for (int i = 0; i < camino.Length; i++)
                camino[i] -= Main.screenPosition;

            // El color del acto, MEZCLADO en la frontera (la historia fluye:
            // violeta → dorado → rojo → blanco → azul sin costuras).
            Color c = EstelaColores[act - 1];
            float actStart = act switch
            {
                1 => 0f,
                2 => Act1End,
                3 => Act2End,
                4 => Act3End,
                _ => NovaEnd,
            };
            float blendT = MathHelper.Clamp((age - actStart) / 26f, 0f, 1f);
            if (act > 1 && blendT < 1f)
                c = Color.Lerp(EstelaColores[act - 2], EstelaColores[act - 1], blendT);

            // El GROSOR también cuenta la vida: fina de joven, gruesa de
            // gigante, hilacha de cadáver.
            float width = act switch
            {
                1 => 12f,
                2 => 16f,
                3 => 21f,
                4 => 18f,
                _ => 9f,
            };

            BeginAdditive();
            EstelaLib.Ribbon(Main.spriteBatch, camino, width, EstelaProfile.Comet,
                c, 0.50f, seed + 7, time, head: false);
            Main.spriteBatch.End();
        }

        // ==================================================================
        //  ACTO I — LA NEBULOSA PROTOSTELAR
        // ==================================================================

        private static void DrawNebulosa(Vector2 pos, float a1, float time, int seed)
        {
            // El centro CONTRAYÉNDOSE (0.5 → 0.8) y la nube CERRÁNDOSE.
            float contract = 0.50f + 0.30f * a1;
            float cloudR = 165f * (1.12f - 0.30f * a1);

            // === 1. LA MASA MOLECULAR (AlphaBlend PRIMERO — la nube OCLUYE) ===
            BrumaFX.BeginMass();
            BrumaFX.Cloud(pos, cloudR, NebVioleta, seed + 11, time * 0.26f,
                puffs: 8, alpha: 0.40f);
            BrumaFX.Cloud(pos, cloudR * 0.62f, NebAzul, seed + 47, time * 0.38f,
                puffs: 5, alpha: 0.28f);
            // El CORAZÓN de la nube: más denso donde nacerá la estrella —
            // se APRIETA conforme el acto avanza (la contracción).
            BrumaFX.Cloud(pos, cloudR * 0.34f * contract, NebCorazon, seed + 83,
                time * 0.50f, puffs: 4, alpha: 0.28f + 0.20f * a1);
            Main.spriteBatch.End();

            // === 2. LAS MOTAS DE POLVO CAYENDO AL CENTRO (Wisps espiralando
            //     hacia adentro — la acreción protostelar) ===
            BeginAdditive();
            for (int k = 0; k < 6; k++)
            {
                float dirSign = (k & 1) == 0 ? 1f : -1f;
                float baseAng = VFXCore.Hash01(seed, 21 + k, 3) * MathHelper.TwoPi
                                + time * 0.11f * dirSign;
                // LA CAÍDA: fase modular determinista (0 = nace lejos, 1 = llega).
                float fall = (time * 0.14f + VFXCore.Hash01(seed, 33 + k, 7)) % 1f;
                float rOut = cloudR * (1.02f - 0.86f * fall);

                Vector2[] path = new Vector2[3];
                for (int j = 0; j < 3; j++)
                {
                    float f = j / 2f;
                    float rr = MathF.Max(rOut * (1f - 0.52f * f), 6f);
                    float ang = baseAng + f * 1.35f * dirSign;
                    path[j] = pos + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * rr;
                }
                BrumaFX.Wisps(path, MathF.Max(9f, cloudR * 0.07f), NebVioleta,
                    PolvoEstelar, seed + 500 + k * 37, time,
                    alpha: 0.26f * (1f - 0.45f * fall), fade: 0.85f, motes: 2);
            }

            // === 3. EL PROTO-NÚCLEO (destellos tenues del corazón por nacer) ===
            float coreR = 10f * contract;
            LumenLib.Bloom(Main.spriteBatch, pos, coreR * 2.2f, NebCorazonCaliente,
                0.26f + 0.24f * a1, 3);
            // El titileo débil (2 Hz — el corazón ainda no sabe arder).
            int flick2 = StormLib.FlickTick(time, 2f);
            for (int k = 0; k < 3; k++)
            {
                if (!StormLib.IsLit(seed + 311 + k * 53, flick2, 0.40f)) continue;
                float ang = VFXCore.Hash01(seed, 71 + k, 5) * MathHelper.TwoPi + time * 0.35f;
                Vector2 sPos = pos + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * coreR * 1.4f;
                LumenLib.Flare(Main.spriteBatch, sPos, coreR * 2.6f, NebCorazonCaliente,
                    0.28f + 0.26f * a1, time * 0.6f + k);
            }

            // === 4. EL HALO FRÍO (sin luz apenas — la casa está vacía) ===
            Quad(Glow, pos, new Vector2(cloudR * 0.55f, cloudR * 0.55f), 0f,
                Tint(NebAzul, 0.14f * (1f - 0.5f * a1)));
            Main.spriteBatch.End();
        }

        // ==================================================================
        //  ACTOS II + III — EL CUERPO SOLAR (la TÉCNICA del sol de la casa,
        //  re-implementada con parámetros propios — secciones 0-3 del
        //  RuneSunRenderer: coronal → backglow → RadialShine → SunShader)
        // ==================================================================

        /// <summary>
        /// El cuerpo de la estrella VIVA. `rg` = 0..1 el ENROJECIMIENTO de
        /// la gigante (0 = secuencia principal dorada), `ignite` = 0..1 el
        /// flash de encendido (1 = YA encendida; solo < 1 en el arranque
        /// del acto II), `lifeT` = 0..1 del ciclo completo.
        /// </summary>
        private static void DrawSunBody(Projectile p, Vector2 pos, float R, float rg,
            float ignite, float lifeT, float time, int seed)
        {
            // === 0. GLOW CORONAL (aditivo — DETRÁS, función PURA de lifeT:
            //     cero parpadeo, la lección v5.96 del sol) ===
            BeginAdditive();
            // HALO EXTERIOR: 1.28× → 1.70× R.
            float haloR = R * (1.28f + 0.42f * lifeT);
            Color haloCol = Color.Lerp(SolHalo, GiganteHalo, rg);
            Quad(Glow, pos, new Vector2(haloR * 2f, haloR * 2f), 0f,
                Tint(haloCol, 0.26f + 0.16f * lifeT));
            // CORONA INTERNA: 1.04× → 1.32× R.
            float corR = R * (1.04f + 0.28f * lifeT);
            Color corCol = Color.Lerp(SolCorona, GiganteCorona, rg);
            Quad(Glow, pos, new Vector2(corR * 2f, corR * 2f), 0f,
                Tint(corCol, 0.20f + 0.14f * lifeT));

            // EL FLASH DE ENCENDIDO (solo el arranque del acto II): el
            // estallido CONCENTRADO del nacimiento — el disco se enciende.
            if (ignite < 1f)
                StormLib.ImpactFlash(Main.spriteBatch, pos, R * 3.4f * (1f - ignite * 0.55f),
                    NovaBlanco, (1f - ignite) * 0.95f, time * 3.1f);
            Main.spriteBatch.End();

            // === 1. BACKGLOW (alpha — el resplandor profundo: BloomCircleSmall ×2) ===
            BeginAlpha();
            Texture2D bloom = (_bloom ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Textures/BloomCircleSmall")).Value;
            Color glowHot = Color.Lerp(new Color(255, 230, 100), new Color(255, 90, 30), rg);
            glowHot.A = 0;
            Main.spriteBatch.Draw(bloom, pos, null, glowHot * 0.62f, 0f,
                bloom.Size() * 0.5f, p.scale * 0.92f, SpriteEffects.None, 0f);
            Color glowRed = Color.Lerp(new Color(255, 75, 25), new Color(255, 35, 12), rg);
            glowRed.A = 0;
            Main.spriteBatch.Draw(bloom, pos, null, glowRed * 0.36f, 0f,
                bloom.Size() * 0.5f, p.scale * 1.58f, SpriteEffects.None, 0f);
            Main.spriteBatch.End();

            // === 2. EL AURA (RadialShineShader — el ruido de energía) ===
            Texture2D wavyBlotch = (_wavy ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Textures/WavyBlotchNoise")).Value;
            if (_shineShader != null && _shineShader.Value != null)
            {
                Effect shine = _shineShader.Value;
                shine.Parameters["globalTime"].SetValue(time);
                // La proporción del sol: 2.72 × el ANCHO (2R) del cuerpo.
                Vector2 shineScale = Vector2.One * R * 5.44f / wavyBlotch.Size();

                BeginAdditive();
                shine.CurrentTechnique.Passes[0].Apply();
                Color shineColor = Color.Lerp(SolShine, GiganteShine, rg);
                Main.spriteBatch.Draw(wavyBlotch, pos, null,
                    shineColor * 0.20f, p.rotation,
                    wavyBlotch.Size() * 0.5f, shineScale, SpriteEffects.None, 0f);
                Main.spriteBatch.End();
            }

            // === 3. EL DISCO DE PLASMA (SunShader — la granulación dendrítica VIVA) ===
            if (_sunShader != null && _sunShader.Value != null)
            {
                Effect shader = _sunShader.Value;
                Texture2D psychedelic = (_psy ??= ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Textures/PsychedelicWingTextureOffsetMap")).Value;
                Texture2D dendritic = (_dend ??= ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Textures/DendriticNoiseZoomedOut")).Value;

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
                Main.spriteBatch.Draw(dendritic, pos, null, Color.White, p.rotation,
                    dendritic.Size() * 0.5f, drawScale, SpriteEffects.None, 0f);
                Main.spriteBatch.End();
            }

            // === 4. LOS ANILLOS RÚNICOS TENUES (el emisor compartido de la
            //     casa — 3 anillos, alpha 0.5; la gigante los VA PERDIENDO) ===
            VFXCore.Begin();
            RuneSunRenderer.EmitRingSystem(p.Center, R, time, seed, 3, rg, lifeT,
                0.5f * (1f - 0.45f * rg));
            VFXCore.FlushAdditive(null, false);   // el batch ya está CERRADO
        }

        /// <summary>La VIDA de la superficie del acto II: chispas orbitales
        /// sobre los anillos + destellos de granulación en el limbo.</summary>
        private static void DrawVidaSuperficie(Vector2 pos, float R, float time, int seed, float ignite)
        {
            BeginAdditive();

            // LAS CHISPAS ORBITALES (3 viajeras sobre el anillo interior).
            for (int k = 0; k < 3; k++)
            {
                float h = VFXCore.Hash01(seed, 41 + k, 19);
                float ang = time * (0.55f + 0.30f * h) * ((k & 1) == 0 ? 1f : -1f)
                            + h * MathHelper.TwoPi;
                Vector2 sPos = pos + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) *
                    R * (1.62f + 0.44f * (k / 2f));
                Quad(Glow, sPos, new Vector2(7f, 7f), 0f,
                    Tint(Color.Lerp(SolCorona, NovaBlanco, 0.4f), 0.55f * ignite));
            }

            // LOS DESTELLOS DE GRANULACIÓN (6 Hz — el disco hierves).
            int flick6 = StormLib.FlickTick(time, 6f);
            for (int k = 0; k < 4; k++)
            {
                if (!StormLib.IsLit(seed + 711 + k * 53, flick6, 0.42f)) continue;
                float ang = VFXCore.Hash01(seed, 51 + k, 23) * MathHelper.TwoPi
                            + time * 0.22f * ((k & 1) == 0 ? 1f : -1f);
                float rr = R * (0.45f + 0.45f * VFXCore.Hash01(seed, 57 + k, 29));
                Vector2 sPos = pos + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * rr;
                LumenLib.Flare(Main.spriteBatch, sPos, R * (0.22f + 0.14f * VFXCore.Hash01(seed, 61 + k, 31)),
                    SolCorona, 0.45f * ignite, time * 0.9f + k * 0.8f);
            }
            Main.spriteBatch.End();
        }

        // ==================================================================
        //  ACTO III — LAS CAPAS PERDIDAS (la nebulosa planetaria) Y LA
        //  CONVECCIÓN DEL COLOSO
        // ==================================================================

        /// <summary>Las CAPAS que la gigante DESPRENDE: puffs de nebulosa
        /// planetaria alejándose al ritmo del acto (AlphaBlend PRIMERO).</summary>
        private static void DrawCapasPerdidas(Vector2 pos, float R, float a3, float time, int seed)
        {
            BrumaFX.BeginMass();
            for (int k = 0; k < 7; k++)
            {
                // Dirección FIJA por semilla (cada capa sale por su propio
                // agujero de la atmósfera) + la fase de vida de la burbuja.
                float ang = VFXCore.Hash01(seed, 61 + k, 5) * MathHelper.TwoPi + k * 2.399f;
                float phase = (time * 0.13f + VFXCore.Hash01(seed, 73 + k, 9)) % 1f;
                float rr = R * (1.05f + 1.75f * phase);
                Vector2 puffPos = pos + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * rr;
                // La burbuja NACE densa al limbo y MUERE lejos.
                float a = 0.26f * (1f - phase) * (0.30f + 0.70f * a3);
                BrumaFX.Puff(puffPos, 20f + 26f * phase,
                    (k & 1) == 0 ? CapaRosa : CapaTeal,
                    seed + 700 + k * 37, time, alpha: a, quality: 0.55f);
            }
            Main.spriteBatch.End();
        }

        /// <summary>Las CELDAS DE CONVECCIÓN de la gigante: lenguas de
        /// PyraLib (la unidad de Flame) rotadas AL LIMBO, latiendo lento a
        /// 0.2 Hz sobre la rampa SolarFire ENROJECIDA.</summary>
        private static void DrawConveccion(Vector2 pos, float R, float a3, float time, int seed)
        {
            // v6.27 FIX: venimos de DrawSunBody con el batch CERRADO (la
            // sección 4 cierra tras FlushAdditive) — Tongue espera el lote
            // ABIERTO del llamador (contrato de PyraLib/StormLib). Sin este
            // Begin el primer batch.Draw lanza InvalidOperationException
            // ("Draw was called, but Begin has not yet been called").
            BeginAdditive();

            Color[] ramp = RampaEnrojecida(a3);

            // EL LATIDO LENTO: 0.2 Hz — el corazón cansado del coloso.
            float beat = 0.75f + 0.25f * MathF.Sin(time * MathHelper.TwoPi * 0.2f);

            const int Cells = 9;
            for (int k = 0; k < Cells; k++)
            {
                float h = VFXCore.Hash01(seed, 90 + k, 13);
                float ang = VFXCore.Hash01(seed, 92 + k, 17) * MathHelper.TwoPi
                            + time * (0.05f + 0.03f * h);          // deriva lenta
                Vector2 basePos = pos + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) *
                    (R * (0.55f + 0.35f * h));
                float height = R * (0.28f + 0.22f * h) * beat;
                float width = R * (0.10f + 0.06f * h);
                float temp = 0.62f + 0.30f * h * beat;
                // rot: la lengua CRECE HACIA AFUERA (arriba local → radial).
                PyraLib.Tongue(Main.spriteBatch, basePos, height, width, ramp, temp,
                    seed + 900 + k * 61, time, intensity: 0.60f * (0.35f + 0.65f * a3),
                    wind: 0f, gravDir: 1f, rot: ang + MathHelper.PiOver2);
            }

            Main.spriteBatch.End();
        }

        /// <summary>La RAMPA SolarFire lerp hacia rojos (cacheada por
        /// cuantos de rg — cero GC por frame).</summary>
        private static Color[] _rampaRoja;
        private static float _rampaRojaRg = -1f;

        private static Color[] RampaEnrojecida(float rg)
        {
            float bucket = MathF.Round(rg * 16f) / 16f;
            if (_rampaRoja == null || bucket != _rampaRojaRg)
            {
                _rampaRoja ??= new Color[PyraPalettes.Niveles];
                for (int i = 0; i < PyraPalettes.Niveles; i++)
                {
                    float t = i / (float)(PyraPalettes.Niveles - 1);
                    _rampaRoja[i] = Color.Lerp(PyraPalettes.Sample(PyraPalettes.SolarFire, t),
                        new Color(255, 45, 25), 0.55f * bucket);
                }
                _rampaRojaRg = bucket;
            }
            return _rampaRoja;
        }

        // ==================================================================
        //  ACTO IV-a — EL COLAPSO (15 ticks: todo cae al centro)
        // ==================================================================

        private static void DrawColapso(Vector2 pos, float Rg, float col, float time, int seed)
        {
            float shrink = 1f - 0.70f * col;        // ×0.3 al final del colapso
            float R = Rg * shrink;
            float heat = col;                        // el núcleo se enciende blanco-azul

            BeginAdditive();

            // === 1. EL ANILLO CONTRÁCTIL (OndaLib.Pulse con el progreso
            //     INVERTIDO: el radio visible se CIERRRA sobre la estrella) ===
            OndaLib.Pulse(Main.spriteBatch, pos, 1f - col, Rg * 2.6f,
                new Color(210, 230, 255), 0.55f * (0.30f + 0.70f * col), seed + 31);
            // Un segundo anillo más rápido, desfasado (la atmósfera cayendo).
            OndaLib.Pulse(Main.spriteBatch, pos, MathF.Max(0.02f, 1f - col * 1.6f),
                Rg * 1.8f, new Color(255, 120, 80), 0.40f * (1f - col * 0.5f), seed + 47);

            // === 2. LA MASA CAYENDO (10 trazos cayendo al centro — la
            //     estrella se COME su propia atmósfera) ===
            for (int k = 0; k < 10; k++)
            {
                float ang = k / 10f * MathHelper.TwoPi
                            + VFXCore.Hash01(seed, 95 + k, 23) * 0.6f;
                float rIn = R * 1.05f;
                float rOut = Rg * (0.55f + 0.75f * (1f - col));
                Vector2 a = pos + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * rOut;
                Vector2 b = pos + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * rIn;
                Vector2 mid = (a + b) * 0.5f;
                float len = Vector2.Distance(a, b);
                if (len > 1f)
                    Capsule(mid, len, 3f, ang,
                        Tint(Color.Lerp(new Color(255, 140, 70), new Color(235, 240, 255), heat),
                            0.45f + 0.20f * heat));
            }

            // === 3. EL CUERPO APRETÁNDOSE (el bloom que se ENFRÍA al azul) ===
            Color cuerpo = Color.Lerp(new Color(255, 110, 55), new Color(225, 238, 255), heat);
            LumenLib.Bloom(Main.spriteBatch, pos, MathF.Max(R * 1.9f, 8f), cuerpo,
                0.50f + 0.40f * heat, 3);
            // EL PUNTO: el corazón ultradenso encendiéndose (la semilla del
            // remanente — ya se ve nacer la estrella de neutrones).
            LumenLib.Bloom(Main.spriteBatch, pos, MathF.Max(R * 0.55f, 4f),
                NucleoBlanco, 0.90f, 2);
            Main.spriteBatch.End();
        }

        // ==================================================================
        //  ACTO IV-b — LA SUPERNOVA (el estallido total, 45 ticks)
        // ==================================================================

        private static void DrawNova(Vector2 pos, float nova, float time, int seed)
        {
            float burst = 1f - nova;                       // 1 al estallar → 0 al final
            float front = OndaLib.Expansion(nova);        // el frente expansivo

            // === 1. LA NUBE EXPANSIVA (AlphaBlend PRIMERO — la masa eyectada) ===
            BrumaFX.BeginMass();
            BrumaFX.Cloud(pos, 140f + 480f * front, NovaHumo, seed + 913,
                time * 0.50f, puffs: 7, alpha: 0.34f * burst);
            BrumaFX.Cloud(pos, 90f + 330f * front, NovaBrasa, seed + 929,
                time * 0.65f, puffs: 5, alpha: 0.26f * burst);
            Main.spriteBatch.End();

            BeginAdditive();

            // === 2. EL DESTELLO CONCENTRADO (ImpactFlash 130 px — el
            //     estallido VIVE en su lugar del mundo: PROHIBIDO el Flash
            //     de pantalla completa, la lección v6.26 de la casa) ===
            if (burst > 0.05f)
                StormLib.ImpactFlash(Main.spriteBatch, pos, 130f * (0.75f + 0.25f * burst),
                    NovaBlanco, 0.95f * burst, time * 2.6f);

            // === 3. LAS ONDAS DE CHOQUE CROMÁTICAS ×2 (400 / 650 px — la
            //     pareja rápida + lenta del estallido) ===
            OndaLib.Shock(Main.spriteBatch, pos, nova, 400f,
                new Color(255, 240, 210), 0.85f * burst + 0.15f, seed + 11,
                thickness: 12f, falloff: OndaFalloff.Quadratic, chromatic: true);
            OndaLib.Shock(Main.spriteBatch, pos, MathF.Max(0.02f, nova * 0.92f), 650f,
                new Color(255, 120, 70), 0.70f * burst, seed + 29,
                thickness: 16f, falloff: OndaFalloff.Quadratic, chromatic: true);

            // === 4. LAS LLAMARADAS RADIALES SolarFire ×12 (el fuego de la
            //     nova cabalgando el frente de choque) ===
            for (int k = 0; k < 12; k++)
            {
                float ang = k / 12f * MathHelper.TwoPi
                            + VFXCore.Hash01(seed, 130 + k, 29) * 0.35f;
                float rr = 40f + 230f * front;
                Vector2 basePos = pos + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * rr;
                float height = (55f + 45f * VFXCore.Hash01(seed, 140 + k, 31)) *
                               (0.35f + 0.65f * burst);
                float width = 16f + 10f * VFXCore.Hash01(seed, 150 + k, 37);
                float temp = 0.35f + 0.60f * burst;      // se enfría al morir
                // rot RADIAL: la llamarada cabalga el frente hacia AFUERA.
                PyraLib.Tongue(Main.spriteBatch, basePos, height, width,
                    PyraPalettes.SolarFire, temp, seed + 1700 + k * 61, time,
                    intensity: 0.85f * (0.30f + 0.70f * burst),
                    wind: 0f, gravDir: 1f, rot: ang + MathHelper.PiOver2);
            }

            // === 5. LOS RAYOS FUGITIVOS ×8 (MultiBolt — la descarga que
            //     escapa del corazón de la nova) ===
            int flick14 = StormLib.FlickTick(time, 14f);
            for (int k = 0; k < 8; k++)
            {
                if (!StormLib.IsLit(seed + 533 + k * 97, flick14, 0.55f)) continue;
                float ang = VFXCore.Hash01(seed, 160 + k, 41) * MathHelper.TwoPi
                            + time * 0.40f * ((k & 1) == 0 ? 1f : -1f);
                float rr = 60f + 380f * front;
                Vector2 end = pos + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * rr;
                StormLib.MultiBolt(Main.spriteBatch, pos, end, seed + 1900 + k * 53, flick14,
                    5f, new Color(255, 210, 120), new Color(170, 130, 255), NovaBlanco,
                    0.75f * burst + 0.10f, 30f, 10);
            }

            // === 6. EL AURORA CROMÁTICO (la firma prismática de la muerte) ===
            if (burst > 0.10f)
                LumenLib.Aurora(Main.spriteBatch, pos, 170f + 320f * front, time,
                    0.02f, 0.30f * burst, 12);

            // === 7. EL REMANENTE YA ENCIENDE (la semilla azul del acto V
            //     naciendo en pleno estallido) ===
            float rem0 = Utils.GetLerpValue(0.25f, 0.85f, nova, true);
            if (rem0 > 0.01f)
                Main.spriteBatch.Draw(Orb, pos, null, Tint(NucleoBlanco, 0.85f * rem0), 0f,
                    Orb.Size() * 0.5f, ScaleOf(Orb, RemnantPx * 2.05f), SpriteEffects.None, 0f);

            Main.spriteBatch.End();
        }

        // ==================================================================
        //  ACTO V — EL REMANENTE (la estrella de neutrones PULSAR)
        // ==================================================================

        private static void DrawRemanente(Vector2 pos, float age, float time, int seed)
        {
            // EL GUION del final: 2.5 s de PÚLSAR vivo + el APAGADO.
            const float PulsarTicks = 150f;              // 2.5 s de pulso
            float remAge = age - NovaEnd;                // edad dentro del acto
            float fade = remAge < PulsarTicks
                ? 1f
                : MathHelper.Clamp(1f - (remAge - PulsarTicks) / 60f, 0f, 1f);  // 1 s de apagado
            if (fade <= 0.01f) return;

            float R = RemnantPx;                          // 10 px — la enana ultradenso
            float spin = time * MathHelper.TwoPi * 1.6f;  // ~1.6 rev/s (¡GIRA!)
            int flick15 = StormLib.FlickTick(time, 15f);  // la superficie a 15 Hz
            float pulse = 0.5f + 0.5f * MathF.Sin(spin * 2f);   // EL LATIDO del faro

            BeginAdditive();

            // === 1. EL RESPLANDOR FRÍO (el "peso" del cadáver en el mundo) ===
            Quad(Glow, pos, new Vector2(R * 8f, R * 8f), 0f, Tint(NucleoAzul, 0.20f * fade));
            Quad(Glow, pos, new Vector2(R * 3.6f, R * 3.6f), 0f,
                Tint(NucleoBlanco, 0.34f * fade));

            // === 2. LAS LÍNEAS DE CAMPO DIPOALRES TENUES (r = L·sen²θ —
            //     la ecuación real del dipolo; técnica propia inspirada en
            //     NeutronStarRenderer, escrita con mi propio pulso) ===
            for (int k = 0; k < 3; k++)
            {
                float L = R * (2.0f + 0.9f * k);
                Color lc = Color.Lerp(CianCampo, VioletaCampo, k / 2f);
                const int Segs = 30;
                Vector2 prev = DipolePoint(pos, L, spin, 0f);
                for (int s = 1; s <= Segs; s++)
                {
                    float t = s / (float)Segs * MathHelper.TwoPi;
                    Vector2 pt = DipolePoint(pos, L, spin, t);
                    Vector2 mid = (prev + pt) * 0.5f;
                    Vector2 delta = pt - prev;
                    float len = delta.Length();
                    if (len > 0.5f)
                        Capsule(mid, len, MathF.Max(1.1f, R * 0.09f),
                            MathF.Atan2(delta.Y, delta.X),
                            Tint(lc, (0.24f - 0.06f * k) * fade * (0.8f + 0.2f * pulse)));
                    prev = pt;
                }
            }

            // === 3. EL MINI-HAZ GIRANDO (EL PÚLSAR: dos haces opuestos
            //     barriendo el mundo como un faro) ===
            float beamLen = R * 7.5f * fade;
            for (int side = 0; side < 2; side++)
            {
                float phi = spin + side * MathF.PI;
                Vector2 dir = new Vector2(MathF.Cos(phi), MathF.Sin(phi));
                LumenLib.Ray(Main.spriteBatch, pos, dir, beamLen, 4.5f,
                    NucleoAzul, (0.35f + 0.30f * pulse) * fade, pulse);
            }

            // === 4. EL NÚCLEO ENANO (10 px — sólido, cegador) ===
            Main.spriteBatch.Draw(Orb, pos, null, Tint(NucleoAzul, 0.85f * fade), 0f,
                Orb.Size() * 0.5f, ScaleOf(Orb, R * 2.05f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(Glow, pos, null, Tint(NucleoBlanco, 0.95f * fade), 0f,
                Glow.Size() * 0.5f, ScaleOf(Glow, R * 1.1f), SpriteEffects.None, 0f);

            // === 5. LA SUPERFICIE CHISPEANTE (destellos de 4 puntas a 15 Hz) ===
            for (int k = 0; k < 5; k++)
            {
                if (!StormLib.IsLit(seed + 733 + k * 53, flick15, 0.5f)) continue;
                float ang = VFXCore.Hash01(seed, 81 + k, 3) * MathHelper.TwoPi + spin;
                float rr = R * (0.40f + 0.55f * VFXCore.Hash01(seed, 93 + k, 7));
                Vector2 sPos = pos + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * rr;
                LumenLib.Flare(Main.spriteBatch, sPos,
                    R * (0.9f + 0.5f * VFXCore.Hash01(seed, 99 + k, 11)),
                    NucleoAzul, 0.80f * fade, spin * 0.30f + k * 0.9f);
            }

            // === 6. EL ANILLO DE EMISIÓN (el ecuador del faro, tenue) ===
            Quad(Ring, pos, VFXCore.RingQuadSize(R * 2.6f), spin * 0.2f,
                Tint(NucleoAzul, 0.20f * fade * (0.7f + 0.3f * pulse)));

            Main.spriteBatch.End();
        }

        /// <summary>Punto del bucle dipolar r = L·sen²θ girado por `rot`.</summary>
        private static Vector2 DipolePoint(Vector2 center, float L, float rot, float t)
        {
            float r = L * MathF.Sin(t) * MathF.Sin(t);
            if (r < 0.02f) r = 0.02f;    // pegado al polo: cierra el bucle
            Vector2 local = new(MathF.Cos(t) * r, MathF.Sin(t) * r);
            return center + local.RotatedBy(rot);
        }

        // ==================================================================
        //  HELPERS DE DIBUJO (patrón validado del proyecto)
        // ==================================================================

        private static void BeginAdditive()
        {
            // v6.21 — LA LECCIÓN DEL CUADRO DE RUIDO: los pases de SHADER
            // exigen SpriteSortMode.Immediate (igual que el sol de la casa).
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
        }

        private static void BeginAlpha()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>Quad centrado (tamaño total = size px) sobre el lote ABIERTO.</summary>
        private static void Quad(Texture2D tex, Vector2 pos, Vector2 size, float rot, Color tint)
        {
            if (tex == null || tint.A == 0) return;
            Main.spriteBatch.Draw(tex, pos, null, tint, rot,
                new Vector2(tex.Width, tex.Height) * 0.5f,
                size / new Vector2(tex.Width, tex.Height),
                SpriteEffects.None, 0f);
        }

        /// <summary>Cápsula de luz orientada (el trazo de las líneas de campo).</summary>
        private static void Capsule(Vector2 mid, float len, float width, float rot, Color tint)
        {
            if (tint.A == 0 || width < 0.5f) return;
            Quad(Glow, mid, new Vector2(len + width, width * 1.9f), rot, tint);
        }

        /// <summary>Escala uniforme del pincel dado para un DIÁMETRO dado.</summary>
        private static Vector2 ScaleOf(Texture2D tex, float diameter) =>
            new Vector2(diameter, diameter) / tex.Size();

        /// <summary>Tinte PREMULTIPLICADO de la casa (v6.25 — RGB×f: la
        /// INTENSIDAD manda en el lote aditivo).</summary>
        private static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            // v6.50.3 — FIX (sonda IL contra el FNA real): BlendState.Additive
            // de FNA es (SourceAlpha, One) — el alfa GATEA el aporte. El Tint
            // premultiplicado v6.25 atenuaba DOS VECES (intensidad real f²:
            // el halo 0.30 salía a 0.09). RGB intacto, alfa=f: LINEAL.
            return new Color(c.R, c.G, c.B, (byte)(int)(255f * f));
        }

        // ==================================================================
        //  SHADERS (carga diferida con fallback — el patrón del sol)
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
