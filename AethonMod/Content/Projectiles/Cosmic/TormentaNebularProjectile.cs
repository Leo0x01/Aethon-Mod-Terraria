using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Effects.Bruma;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// TormentaNebularProjectile — v6.24 — LA TORMENTA NEBULAR.
    ///
    /// EL PROYECTIL DEL CETRO — la tormenta persistente (5 s), un
    /// concierto de las tres librerías:
    ///
    ///   · LA NUBE (BrumaFX): una Cloud viva de niebla violeta-cian
    ///     (pase ALFA — masa que OCLUYE) + una segunda cloud tenue más
    ///     arriba (la profundidad) + dos puffs grandes girando.
    ///   · EL AURORA (LumenLib): bandas prismáticas de drift latiendo
    ///     DENTRO de la nube + el bloom del corazón cargado.
    ///   · LAS DESCARGAS (StormLib): hasta TRES rayos simultáneos, cada
    ///     uno con su ciclo propio — telegraph (LumenLib.Telegraph, 10
    ///     ticks de aviso con línea + anillo objetivo) → EL GOLPE
    ///     (MultiBolt de la panza de la nube al punto + daño en área
    ///     85 px + Electrified + trueno) → el afterglow (flash + onda).
    ///   · LAS RUNAS: dos círculos orbitando el borde de la tormenta
    ///     (8 doradas CW + 6 violetas CCW — la forma de los agujeros).
    ///
    /// Daño 100% manual (patrón RunicLightning): determinismo MP gratis.
    /// </summary>
    public class TormentaNebularProjectile : ModProjectile
    {
        /// <summary>Vida total de la tormenta (5 s).</summary>
        private const int StormLife = 300;

        /// <summary>Ticks de telegraph de cada descarga.</summary>
        private const int TelegraphTicks = 10;

        /// <summary>Duración total de cada ciclo de descarga.</summary>
        private const int StrikeCycle = 26;

        /// <summary>Ticks entre activaciones de descarga.</summary>
        private const int StrikeEvery = 14;

        /// <summary>Radio del golpe de cada descarga.</summary>
        private const float StrikeR = 85f;

        /// <summary>Regeneración de los rayos (Hz).</summary>
        private const float FlickHz = 12f;

        // === LOS TRES SLOTS DE DESCARGA (ciclos independientes) ===
        private readonly Vector2[] _slotPoint = new Vector2[3];
        private readonly float[] _slotAge = new float[3] { -1f, -1f, -1f };
        private readonly bool[] _slotStruck = new bool[3];

        private float _age;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            // Daño 100% manual (las descargas).
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = StormLife;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
        }

        /// <summary>Semilla determinista de la tormenta.</summary>
        private int Seed => Math.Max(1, Projectile.identity + 29);

        public override void AI()
        {
            _age += 1f;

            // === EL NACIMIENTO: la tormenta derivation suave (vive) ===
            // (velocidad nula: la tormenta es un LUGAR, pero respira).

            // === LAS DESCARGAS: cada 14 ticks, un slot libre se arma ===
            if (_age % StrikeEvery == 0f && _age > 6f)
            {
                for (int s = 0; s < 3; s++)
                {
                    if (_slotAge[s] >= 0f) continue;
                    _slotAge[s] = 0f;
                    _slotStruck[s] = false;
                    // El punto de impacto: dentro de la zona de la tormenta.
                    _slotPoint[s] = Projectile.Center + new Vector2(
                        Main.rand.NextFloat(-95f, 95f),
                        Main.rand.NextFloat(10f, 60f));
                    break;
                }
            }

            // === EL CICLO de cada slot ===
            for (int s = 0; s < 3; s++)
            {
                if (_slotAge[s] < 0f) continue;
                _slotAge[s] += 1f;

                // EL GOLPE: al terminar el telegraph, TODO sucede YA.
                if (!_slotStruck[s] && _slotAge[s] >= TelegraphTicks)
                {
                    _slotStruck[s] = true;
                    Strike(s);
                }

                // El slot se libera al cerrar su ciclo.
                if (_slotAge[s] >= StrikeCycle) _slotAge[s] = -1f;
            }

            // === LA LUZ de la tormenta (violeta-cian, muestreada) ===
            if (_age % 4f == 0f)
            {
                float fade = MathHelper.Clamp(Projectile.timeLeft / 40f, 0f, 1f);
                Lighting.AddLight(Projectile.Center,
                    0.35f * fade, 0.22f * fade, 0.55f * fade);
            }
        }

        /// <summary>EL GOLPE de la descarga s: daño en área + drama.</summary>
        private void Strike(int s)
        {
            Vector2 point = _slotPoint[s];

            if (Main.netMode != NetmodeID.Server)
            {
                // TRUENO de la descarga (grave, capa media).
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item12.WithPitchOffset(-0.30f).WithVolumeScale(0.85f), point);
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item93.WithPitchOffset(0.35f).WithVolumeScale(0.5f), point);

                // Las chispas del punto de impacto.
                for (int i = 0; i < 16; i++)
                {
                    float ang = Main.rand.NextFloat(0, MathHelper.TwoPi);
                    Dust d = Dust.NewDustPerfect(point, DustID.Electric,
                        new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang) * 0.7f) *
                        Main.rand.NextFloat(2.5f, 8f),
                        220, new Color(190, 170, 255), 1.0f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }

                // El pequeño puñetazo de cámara.
                try
                {
                    Main.instance.CameraModifiers.Add(
                        new Terraria.Graphics.CameraModifiers.PunchCameraModifier(
                            point, new Vector2(0.3f, 1f), 3.5f, 6, 10, 0.25f,
                            "AethonNebularStorm"));
                }
                catch { }
            }

            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            // === EL DAÑO EN ÁREA del punto de impacto ===
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy()) continue;
                if ((npc.Center - point).Length() > StrikeR) continue;
                npc.SimpleStrikeNPC(Projectile.damage, npc.direction, false,
                    2.5f, DamageClass.Magic);
                try { npc.AddBuff(BuffID.Electrified, 200); } catch { }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // ============================================================
            //  CONTRATO DE BATCH A PRUEBA DE BALAS (v6.10).
            // ============================================================
            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try
            {
                DrawStorm();
            }
            catch { try { Main.spriteBatch.End(); } catch { } }

            if (wasActive)
                RestoreSpriteBatch();
            return false;
        }

        /// <summary>Restaura el SpriteBatch con los parámetros EXACTOS del
        /// pase de proyectiles de vanilla (Main.DrawProjectiles).</summary>
        private static void RestoreSpriteBatch()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.Transform);
        }

        // ------------------------------------------------------------------
        //  LA PALETA DE LA TORMENTA
        // ------------------------------------------------------------------

        private static readonly Color GoldHalo = new(255, 195, 85);
        private static readonly Color GoldWarm = new(255, 225, 140);
        private static readonly Color StarBlue = new(150, 180, 255);
        private static readonly Color WhiteIncan = new(255, 250, 235);
        private static readonly Color StormViolet = new(118, 84, 190);
        private static readonly Color StormCyan = new(70, 130, 175);
        private static readonly Color RuneVioletBody = new(110, 130, 255);

        // ------------------------------------------------------------------
        //  EL DIBUJO DE LA TORMENTA
        // ------------------------------------------------------------------

        private void DrawStorm()
        {
            try
            {
                float time = Main.GlobalTimeWrappedHourly;
                int seed = Seed;
                int flick = StormLib.FlickTick(time, FlickHz);
                Vector2 center = Projectile.Center - Main.screenPosition;

                // La vida de la tormenta completa (para el desvanecido final).
                float lifeFade = MathHelper.Clamp(Projectile.timeLeft / 45f, 0f, 1f);

                // ============ 1. LA NUBE (pase ALFA — masa que ocluye) ============
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                // La MASA principal de la tormenta (respirando).
                float breathe = 1f + 0.05f * (float)Math.Sin(time * 0.7f);
                BrumaFX.Cloud(Projectile.Center - Main.screenPosition,
                    92f * breathe, StormViolet, seed + 11, time,
                    puffs: 6, alpha: 0.34f * lifeFade);

                // La CAPA ALTA tenue (la profundidad del cielo).
                BrumaFX.Cloud(Projectile.Center - Main.screenPosition - new Vector2(0f, 30f),
                    120f * breathe, StormCyan, seed + 47, time * 0.8f,
                    puffs: 5, alpha: 0.20f * lifeFade);

                // DOS PUFFS GIANTES girando lento (la deriva de la tormenta).
                for (int i = 0; i < 2; i++)
                {
                    float ang = time * 0.10f * (i == 0 ? 1f : -1f) + i * MathHelper.Pi;
                    Vector2 pos = Projectile.Center - Main.screenPosition + new Vector2(
                        (float)Math.Cos(ang) * 30f, (float)Math.Sin(ang) * 12f - 8f);
                    BrumaFX.Puff(pos, 52f, i == 0 ? StormViolet : StormCyan,
                        seed + 83 + i * 37, time,
                        alpha: 0.22f * lifeFade, quality: 0.6f);
                }

                // ============ 2..6: LO BRILLANTE (pase aditivo) ============
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                // --- 2. EL CORAZÓN CARGADO (LumenLib — bloom tenue) ---
                LumenLib.BloomPulse(Main.spriteBatch, center, 38f,
                    LumenLib.Hue(LumenLib.Drift(time, seed, 0.18f), 0.6f, 1f),
                    0.28f * lifeFade, time, 1.1f, 2);

                // --- 3. EL AURORA DENTRO DE LA NUBE (la firma de LumenLib) ---
                LumenLib.Aurora(Main.spriteBatch, center, 62f, time,
                    LumenLib.Drift(time, seed, 0.12f), 0.30f * lifeFade, 10);

                // --- 4. LAS RUNAS del borde (dos círculos — forma de agujero) ---
                DrawStormRunes(center, time, lifeFade);

                // --- 5. LOS ARCOS AMBIENTALES dentro de la nube ---
                for (int a = 0; a < 2; a++)
                {
                    if (!StormLib.IsLit(seed + 40 + a * 17, flick, 0.40f)) continue;
                    float baseA = time * (0.8f + 0.3f * a) + a * 2.4f;
                    StormLib.ArcRing(Main.spriteBatch, center + new Vector2(0f, -6f),
                        40f + 8f * a, baseA, baseA + 1.3f, seed + 500 + a * 13, flick,
                        3.0f, Tint(StarBlue, 0.45f * lifeFade),
                        Tint(WhiteIncan, 0.8f * lifeFade), 1f, 8);
                }

                // --- 6. LAS DESCARGAS (telegraph → golpe → afterglow) ---
                Vector2 cloudBottom = Projectile.Center - Main.screenPosition +
                    new Vector2(0f, 18f);
                for (int s = 0; s < 3; s++)
                {
                    if (_slotAge[s] < 0f) continue;
                    Vector2 point = _slotPoint[s] - Main.screenPosition;

                    if (!_slotStruck[s])
                    {
                        // EL TELEGRAPH: la raya de aviso + el anillo objetivo.
                        float progress = MathHelper.Clamp(_slotAge[s] / TelegraphTicks, 0f, 1f);
                        Vector2 dir = point - cloudBottom;
                        float len = dir.Length();
                        if (len > 8f)
                        {
                            dir /= len;
                            LumenLib.Telegraph(Main.spriteBatch, cloudBottom, dir, len,
                                StarBlue, progress);
                        }
                    }
                    else
                    {
                        // EL RAYO: multi-filamento de la panza de la nube al punto.
                        float strikeAge = _slotAge[s] - TelegraphTicks;
                        float fade = MathHelper.Clamp(1f - strikeAge / 14f, 0f, 1f) * lifeFade;
                        if (fade > 0.03f)
                        {
                            bool lit = StormLib.IsLit(seed + 61 + s * 37, flick, 0.70f);
                            float alpha = lit ? 1f : 0.18f;
                            StormLib.MultiBolt(Main.spriteBatch, cloudBottom, point,
                                seed + 61 + s * 37, flick,
                                Math.Max(3.4f, 6.5f * fade),
                                Tint(StarBlue, 0.60f * fade), Tint(StormViolet, 0.60f * fade),
                                Tint(WhiteIncan, 0.92f * fade),
                                alpha * fade, 20f, 10);

                            // EL FLASH del punto (los primeros ticks).
                            float flashI = MathHelper.Clamp(1f - strikeAge / 6f, 0f, 1f);
                            if (flashI > 0f)
                                StormLib.ImpactFlash(Main.spriteBatch, point,
                                    44f * (0.6f + 0.4f * flashI), StarBlue,
                                    flashI * fade, time * 0.8f);

                            // LA ONDA de la descarga.
                            float wavePhase = MathHelper.Clamp(strikeAge / 12f, 0f, 1f);
                            if (wavePhase < 1f)
                            {
                                Quad(VFXCore.Ring, point,
                                    VFXCore.RingQuadSize(18f + 80f * wavePhase),
                                    wavePhase * 2.4f + s,
                                    Tint(StarBlue, 0.38f * (1f - wavePhase) * fade));
                            }
                        }
                    }
                }

                Main.spriteBatch.End();
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }
        }

        /// <summary>Las runas del borde de la tormenta (forma de agujero).</summary>
        private void DrawStormRunes(Vector2 center, float time, float fade)
        {
            Vector2[][] glyphs = RuneGlyphs;
            float gScale = 0.72f;

            // EL CÍRCULO DORADO — 8 runas CW.
            for (int g = 0; g < 8; g++)
                DrawStormRune(center, 104f, time * 0.10f, g, 8, glyphs[(g + 1) % glyphs.Length],
                    GoldWarm, gScale, fade);
            // EL CÍRCULO VIOLETA — 6 runas CCW (el contrarroto).
            for (int g = 0; g < 6; g++)
                DrawStormRune(center, 126f, -time * 0.075f, g, 6, glyphs[g % glyphs.Length],
                    RuneVioletBody, gScale * 0.9f, fade);
        }

        private void DrawStormRune(Vector2 center, float radius, float orbit, int g,
            int count, Vector2[] strokes, Color body, float gScale, float fade)
        {
            float ang = g / (float)count * MathHelper.TwoPi + orbit;
            float floatR = radius + 2.4f * gScale * (float)Math.Sin(
                Main.GlobalTimeWrappedHourly * 1.35f + g * 0.9f);
            float bobY = 2.0f * gScale * (float)Math.Sin(
                Main.GlobalTimeWrappedHourly * 0.85f + g * 1.7f);
            Vector2 pos = center + new Vector2(
                (float)Math.Cos(ang) * floatR,
                (float)Math.Sin(ang) * floatR * 0.82f + bobY);
            float pulse = 0.75f + 0.25f * (float)Math.Sin(
                Main.GlobalTimeWrappedHourly * 2.4f + g * 1.3f);

            Quad(VFXCore.SoftGlow, pos, new Vector2(28f * gScale, 28f * gScale), 0f,
                Tint(body, 0.20f * pulse * fade));

            for (int s = 0; s < strokes.Length; s += 2)
            {
                Vector2 a = pos + strokes[s] * gScale;
                Vector2 b = pos + strokes[s + 1] * gScale;
                Vector2 mid = (a + b) * 0.5f;
                Vector2 d = b - a;
                float len = d.Length();
                if (len < 0.01f) continue;
                Capsule(mid, len, 3.2f * gScale, (float)Math.Atan2(d.Y, d.X),
                    Tint(body, 0.85f * pulse * fade));
            }

            Vector2 pearl = pos - new Vector2(0f, 9.5f * gScale);
            Quad(VFXCore.SoftGlow, pearl, new Vector2(6.2f * gScale, 6.2f * gScale), 0f,
                Tint(body, 0.62f * pulse * fade));
            Quad(VFXCore.SoftGlow, pearl, new Vector2(2.9f * gScale, 2.9f * gScale), 0f,
                Tint(WhiteIncan, 0.9f * pulse * fade));
        }

        // ------------------------------------------------------------------
        //  HELPERS
        // ------------------------------------------------------------------

        /// <summary>La tabla de glifos del agujero (3 glifos representativos).</summary>
        private static Vector2[][] RuneGlyphs => new Vector2[][]
        {
            // EL TRONO
            new Vector2[] { new(-3.5f, 7f), new(-3.5f, -5f), new(-3.5f, -5f), new(3.5f, -5f), new(3.5f, -5f), new(3.5f, 7f), new(-3.5f, -5f), new(0f, -7f), new(-1.5f, 1.5f), new(1.5f, 1.5f) },
            // EL OJO DEL VACÍO
            new Vector2[] { new(-4f, 0f), new(0f, -4f), new(0f, -4f), new(4f, 0f), new(4f, 0f), new(0f, 4f), new(0f, 4f), new(-4f, 0f), new(-1.5f, 0f), new(1.5f, 0f), new(0f, -7f), new(0f, -4.5f), new(0f, 4.5f), new(0f, 7f) },
            // LA ESTRELLA DOBLE
            new Vector2[] { new(0f, 7f), new(0f, -7f), new(-4f, 0f), new(4f, 0f), new(-2.5f, -4.5f), new(2.5f, 4.5f), new(2.5f, -4.5f), new(-2.5f, 4.5f) },
        };

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
            Quad(VFXCore.SoftGlow, mid, new Vector2(len + width, width * 1.9f), rot, tint);
        }

        private static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color(c.R, c.G, c.B, (byte)(int)(255f * f));
        }
    }
}
