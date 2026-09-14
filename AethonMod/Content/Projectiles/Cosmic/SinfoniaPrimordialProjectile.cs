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
    /// SinfoniaPrimordialProjectile — v6.24 — LA CHISPA DE LA CREACIÓN.
    ///
    /// EL PROYECTIL DEL BASTÓN DE LA SINFONÍA PRIMORDIAL — el arma que
    /// usa TODAS las librerías del proyecto, cada una con su papel:
    ///
    ///   · AL VUELO: un corazón de luz LUMENLIB (bloom prismático de
    ///     drift + destello + aurora + rayos de sol) envuelto en una
    ///     estela de BRUMA viva, con los arcos de corona de STORMLIB
    ///     chasqueando alrededor y CADENAS que saltan a los enemigos
    ///     cercanos mientras viaja. Seis RUNAS orbitan la chispa (la
    ///     forma de los agujeros negros).
    ///   · AL MORIR — LA SINFONÍA: daño en área, el estallido de luz
    ///     (ImpactFlash + aurora completa), SEIS RAYOS RADIALES de
    ///     StormLib reventando hacia afuera, la NUBE de bruma de la
    ///     detonación creciendo y las runas VOLANDO hacia afuera.
    ///
    /// Daño 100% manual (patrón RunicLightning): columna nula, área al
    /// morir, cadenas al vuelo — determinismo MP gratis.
    /// </summary>
    public class SinfoniaPrimordialProjectile : ModProjectile
    {
        /// <summary>Ticks de vuelo antes de detonar sola.</summary>
        private const int FlyLife = 120;

        /// <summary>Duración de la fase de detonación (LA SINFONÍA).</summary>
        private const int FinaleTicks = 26;

        /// <summary>Regeneración de los rayos (Hz).</summary>
        private const float FlickHz = 12f;

        // === LAS CADENAS del vuelo (hasta 3 saltos simultáneos) ===
        private readonly Vector2[] _chainFrom = new Vector2[3];
        private readonly Vector2[] _chainTo = new Vector2[3];
        private readonly float[] _chainLife = new float[3];

        private float _age;
        private bool _dying;
        private float _dyingAge;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            // Daño 100% manual: sin contacto de vanilla.
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = FlyLife + FinaleTicks;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
        }

        /// <summary>Semilla determinista de la chispa.</summary>
        private int Seed => Math.Max(1, Projectile.identity + 13);

        public override void AI()
        {
            _age += 1f;

            // === LA FASE DE DETONACIÓN: la sinfonía suena y se apaga ===
            if (_dying)
            {
                _dyingAge += 1f;
                Projectile.velocity *= 0.80f;

                // Las brasas de la detonación.
                if (Main.netMode != NetmodeID.Server && _dyingAge < 14f && _dyingAge % 2f == 0f)
                {
                    float ang = Main.rand.NextFloat(0, MathHelper.TwoPi);
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.RainbowRod,
                        new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) *
                        Main.rand.NextFloat(1.5f, 4.5f),
                        200, new Color(255, 235, 180), 0.9f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }

                // La luz de la sinfonía apagándose.
                if (_age % 3f == 0f)
                {
                    float f = 1f - _dyingAge / FinaleTicks;
                    Lighting.AddLight(Projectile.Center, 0.75f * f, 0.55f * f, 0.30f * f);
                }
                return;
            }

            // === EL VUELO: la chispa se abre paso, decelerando suave ===
            Projectile.velocity *= 0.988f;

            // === v6.25 — LA ESTELA DE LA SINFONÍA: el track del camino
            //     (EstelaLib — el ring-buffer por identidad) ===
            EstelaLib.Track(Projectile.whoAmI, 24).Push(Projectile.Center);
            if (_age % 120f == 0f) EstelaLib.PurgeTracks();

            // === LAS CADENAS: cada 9 ticks, un rayo al enemigo cercano ===
            if (_age % 9f == 0f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC best = null;
                float bestDist = 260f;
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.CanBeChasedBy()) continue;
                    float dist = (npc.Center - Projectile.Center).Length();
                    if (dist < bestDist) { bestDist = dist; best = npc; }
                }
                if (best != null)
                {
                    int slot = (int)(_age / 9f) % 3;
                    _chainFrom[slot] = Projectile.Center;
                    _chainTo[slot] = best.Center;
                    _chainLife[slot] = 9f;

                    best.SimpleStrikeNPC(
                        Math.Max(1, (int)(Projectile.damage * 0.55f)), best.direction,
                        false, 1.5f, DamageClass.Magic);
                    try { best.AddBuff(BuffID.Electrified, 180); } catch { }
                }
            }

            // La vida de las cadenas decae (para el dibujo).
            for (int c = 0; c < 3; c++)
                if (_chainLife[c] > 0f) _chainLife[c] -= 1f;

            // === LA MUERTE NATURAL: al borde del tiempo, LA SINFONÍA (la
            //     detonación EXTIENDE la vida para la fase final — nunca
            //     se reviva dentro de OnKill, que ya es la sentencia) ===
            if (Projectile.timeLeft <= FinaleTicks + 2)
            {
                Detonate();
                return;
            }

            // === EL CONTACTO: enemigo tocado → LA SINFONÍA, YA ===
            if (Main.netMode != NetmodeID.MultiplayerClient && _age > 4f)
            {
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.CanBeChasedBy()) continue;
                    if ((npc.Center - Projectile.Center).Length() >
                        26f + Math.Max(npc.width, npc.height) * 0.35f) continue;
                    Detonate();
                    break;
                }
            }

            // === LA LUZ prismática de la chispa (muestreada) ===
            if (_age % 3f == 0f)
            {
                float drift = LumenLib.Drift(Main.GlobalTimeWrappedHourly, Seed, 0.22f);
                Color c = LumenLib.Hue(drift, 0.85f, 1f);
                Lighting.AddLight(Projectile.Center, c.R / 255f * 0.9f,
                    c.G / 255f * 0.9f, c.B / 255f * 0.9f);
            }
        }

        /// <summary>LA SINFONÍA: el estallido completo (una sola vez).</summary>
        private void Detonate()
        {
            if (_dying) return;
            _dying = true;
            _dyingAge = 0f;
            Projectile.timeLeft = (int)(FinaleTicks + 2);
            Projectile.velocity = Vector2.Zero;

            if (Main.netMode != NetmodeID.Server)
            {
                // EL ACORDE: trueno grave + campana prismática.
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item12.WithPitchOffset(-0.35f).WithVolumeScale(1.1f), Projectile.Center);
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item122.WithPitchOffset(0.30f).WithVolumeScale(0.7f), Projectile.Center);

                // LAS CHISPAS del estallido (prismáticas + doradas).
                for (int i = 0; i < 30; i++)
                {
                    float ang = Main.rand.NextFloat(0, MathHelper.TwoPi);
                    float spd = Main.rand.NextFloat(3f, 12f);
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.RainbowRod,
                        new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang) * 0.8f) * spd,
                        220, new Color(255, 240, 190), 1.2f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }

                // v6.25 — EL PAQUETE DE IMPACTO CENTRALIZADO (OndaLib):
                // la sacudida de cámara del acumulador ÚNICO del frame +
                // el destello de pantalla del velo radial.
                OndaLib.Kick(9f, 16);
                OndaLib.Flash(new Color(255, 240, 210), 0.16f, 8);
            }

            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            // === EL DAÑO EN ÁREA (radio 140) ===
            const float BurstR = 140f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy()) continue;
                if ((npc.Center - Projectile.Center).Length() > BurstR) continue;
                npc.SimpleStrikeNPC(Projectile.damage, npc.direction, false,
                    3.5f, DamageClass.Magic);
                try { npc.AddBuff(BuffID.Electrified, 240); } catch { }
            }
        }

        public override void OnKill(int timeLeft)
        {
            // Si muere sin haber sonado (expiración), la sinfonía igual suena.
            if (!_dying) Detonate();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // ============================================================
            //  CONTRATO DE BATCH A PRUEBA DE BALAS (v6.10): durante PreDraw
            //  el batch de tML está ABIERTO; cerrarlo antes del pase propio.
            // ============================================================
            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try
            {
                if (_dying) DrawFinale();
                else DrawSpark();
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
        //  LA PALETA DE LA SINFONÍA
        // ------------------------------------------------------------------

        private static readonly Color GoldHalo = new(255, 195, 85);
        private static readonly Color GoldWarm = new(255, 225, 140);
        private static readonly Color StarBlue = new(150, 180, 255);
        private static readonly Color WhiteIncan = new(255, 250, 235);
        private static readonly Color BrumaVioleta = new(120, 88, 168);
        private static readonly Color BrumaDorada = new(170, 128, 62);

        // ------------------------------------------------------------------
        //  AL VUELO — LA CHISPA DE LA CREACIÓN
        // ------------------------------------------------------------------

        private void DrawSpark()
        {
            try
            {
                float time = Main.GlobalTimeWrappedHourly;
                int seed = Seed;
                int flick = StormLib.FlickTick(time, FlickHz);
                Vector2 center = Projectile.Center - Main.screenPosition;
                float drift = LumenLib.Drift(time, seed, 0.22f);
                Color prism = LumenLib.Hue(drift, 0.6f, 1f);

                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                // === 1. LA ESTELA DE BRUMA (BrumaFX — el aliento de la chispa) ===
                Vector2 vel = Projectile.velocity;
                for (int i = 1; i <= 3; i++)
                {
                    Vector2 world = Projectile.Center - vel * (0.55f * i);
                    BrumaFX.Puff(world - Main.screenPosition,
                        14f + 5f * i, i % 2 == 0 ? BrumaDorada : BrumaVioleta,
                        seed + 300 + i * 37, time - 0.12f * i,
                        alpha: 0.22f - 0.05f * i, quality: 0.5f);
                }

                // === 1b. LA ESTELA DE RIBBON (v6.25 — EstelaLib: el CAMINO
                //     REAL de la chispa, ribbon de grosor variable Comet con
                //     triple capa velo/cuerpo/núcleo + cabeza integrada) ===
                Vector2[] camino = EstelaLib.Track(Projectile.whoAmI, 24).Points();
                if (camino.Length > 2)
                {
                    for (int i = 0; i < camino.Length; i++)
                        camino[i] -= Main.screenPosition;
                    EstelaLib.Ribbon(Main.spriteBatch, camino, 15f,
                        EstelaProfile.Comet, prism, 0.55f, seed + 7, time);
                }

                // === 2. EL CORAZÓN DE LUZ (LumenLib — bloom prismático + destello) ===
                LumenLib.BloomPulse(Main.spriteBatch, center, 30f, prism, 0.55f, time, 1.6f, 3);
                LumenLib.Flare(Main.spriteBatch, center, 26f, prism, 0.35f, time * 0.5f);

                // === 3. EL AURORA de bandas (la sinfonía cromática girando) ===
                LumenLib.Aurora(Main.spriteBatch, center, 34f, time, drift, 0.26f, 9);

                // === 4. LOS RAYOS DE SOL radiando (5 radios de drift) ===
                for (int i = 0; i < 5; i++)
                {
                    float ang = i / 5f * MathHelper.TwoPi + time * 0.8f;
                    Vector2 dir = new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang));
                    Color c = LumenLib.Hue(drift + i * 0.20f, 0.6f, 1f);
                    float pulse = 0.5f + 0.5f * (float)Math.Sin(time * 3.2f + i * 1.26f);
                    LumenLib.Ray(Main.spriteBatch, center + dir * 6f, dir,
                        20f + 14f * pulse, 7f, c, 0.30f + 0.25f * pulse, pulse);
                }

                // === 5. LAS RUNAS ORBITANDO (la forma de los agujeros — 6 glifos) ===
                DrawOrbitingRunes(center, 24f, time, seed, 1f);

                // === 6. LA CORONA DE DESCARGA (StormLib — arcos chasqueando) ===
                for (int a = 0; a < 2; a++)
                {
                    if (!StormLib.IsLit(seed + 40 + a * 17, flick, 0.65f)) continue;
                    float span = 1.0f + 0.6f * VFXCore.Hash01(seed, 941 + a, flick);
                    float baseA = time * (1.1f + 0.4f * a) + a * 2.6f;
                    StormLib.ArcRing(Main.spriteBatch, center, 20f + 4f * a,
                        baseA, baseA + span, seed + 500 + a * 13, flick,
                        3.2f, Tint(GoldWarm, 0.55f), Tint(WhiteIncan, 0.9f), 1f, 9);
                }
                StormLib.EndCap(Main.spriteBatch, center, 9f,
                    Tint(prism, 0.55f), Tint(WhiteIncan, 0.85f), 1f);

                // === 7. LAS CADENAS a los enemigos (los saltos del vuelo) ===
                for (int c = 0; c < 3; c++)
                {
                    if (_chainLife[c] <= 0f) continue;
                    if (!StormLib.IsLit(seed + 61 + c * 37, flick, 0.60f)) continue;
                    float a = MathHelper.Clamp(_chainLife[c] / 9f, 0f, 1f);
                    StormLib.ChainBolt(Main.spriteBatch,
                        _chainFrom[c] - Main.screenPosition, _chainTo[c] - Main.screenPosition,
                        seed + 61 + c * 37, flick, 4.2f,
                        Tint(StarBlue, 0.75f * a), Tint(new Color(230, 240, 255), 0.9f * a),
                        1f, 7, 10f);
                }

                Main.spriteBatch.End();
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }
        }

        /// <summary>Las runas orbitando (la forma de los agujeros negros).</summary>
        private void DrawOrbitingRunes(Vector2 center, float radius, float time,
            int seed, float fade)
        {
            // Tres glifos de la tabla del agujero (el sol roto, el cetro, la estrella).
            Vector2[][] glyphs = RuneGlyphs;
            const int Count = 6;
            float gScale = 0.62f;
            for (int g = 0; g < Count; g++)
            {
                float ang = g / (float)Count * MathHelper.TwoPi + time * 0.30f;
                float floatR = radius + 2.4f * gScale * (float)Math.Sin(time * 1.35f + g * 0.9f);
                float bobY = 2.0f * gScale * (float)Math.Sin(time * 0.85f + g * 1.7f);
                Vector2 pos = center + new Vector2(
                    (float)Math.Cos(ang) * floatR,
                    (float)Math.Sin(ang) * floatR + bobY);
                float pulse = 0.75f + 0.25f * (float)Math.Sin(time * 2.4f + g * 1.3f);

                Quad(VFXCore.SoftGlow, pos, new Vector2(26f * gScale, 26f * gScale), 0f,
                    Tint(GoldHalo, 0.20f * pulse * fade));

                Vector2[] strokes = glyphs[(g + 1) % glyphs.Length];
                for (int s = 0; s < strokes.Length; s += 2)
                {
                    Vector2 a = pos + strokes[s] * gScale;
                    Vector2 b = pos + strokes[s + 1] * gScale;
                    Vector2 mid = (a + b) * 0.5f;
                    Vector2 d = b - a;
                    float len = d.Length();
                    if (len < 0.01f) continue;
                    Capsule(mid, len, 3.2f * gScale, (float)Math.Atan2(d.Y, d.X),
                        Tint(GoldWarm, 0.85f * pulse * fade));
                }

                Vector2 pearl = pos - new Vector2(0f, 9.5f * gScale);
                Quad(VFXCore.SoftGlow, pearl, new Vector2(6.4f * gScale, 6.4f * gScale), 0f,
                    Tint(GoldHalo, 0.62f * pulse * fade));
                Quad(VFXCore.SoftGlow, pearl, new Vector2(3.0f * gScale, 3.0f * gScale), 0f,
                    Tint(WhiteIncan, 0.9f * pulse * fade));
            }
        }

        // ------------------------------------------------------------------
        //  AL MORIR — LA SINFONÍA (el estallido)
        // ------------------------------------------------------------------

        private void DrawFinale()
        {
            try
            {
                float time = Main.GlobalTimeWrappedHourly;
                int seed = Seed;
                int flick = StormLib.FlickTick(time, FlickHz);
                Vector2 center = Projectile.Center - Main.screenPosition;
                float phase = MathHelper.Clamp(_dyingAge / FinaleTicks, 0f, 1f);
                float fade = 1f - phase;
                float burst = 1f - phase;                    // la expansión
                float drift = LumenLib.Drift(time, seed, 0.30f);
                Color prism = LumenLib.Hue(drift, 0.6f, 1f);

                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                // === 1. LA NUBE DE BRUMA de la detonación (BrumaFX — creciendo) ===
                // (en pase ALFA para que sea masa; abrimos un micro-pase)
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                float cloudR = 34f + 64f * phase;
                BrumaFX.Cloud(Projectile.Center - Main.screenPosition, cloudR,
                    BrumaVioleta, seed + 71, time, puffs: 6,
                    alpha: 0.36f * fade + 0.06f);
                BrumaFX.Cloud(Projectile.Center - Main.screenPosition, cloudR * 0.62f,
                    BrumaDorada, seed + 137, time * 0.8f, puffs: 4,
                    alpha: 0.28f * fade + 0.04f);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                // === 2. EL ESTALLIDO DE LUZ (LumenLib — flash + aurora completa) ===
                // === 2b. LA ONDA EXPANSIVA (v6.25 — OndaLib: el frente ROTO ===
                // ===     con aberración cromática + retaguardia) ===
                OndaLib.Shock(Main.spriteBatch, center, phase, 215f, prism,
                    MathHelper.Clamp(0.85f * fade + 0.15f, 0f, 1f), seed, 10f,
                    OndaFalloff.Quadratic, chromatic: true);

                float flashI = MathHelper.Clamp(1f - _dyingAge / 8f, 0f, 1f);
                if (flashI > 0f)
                {
                    StormLib.ImpactFlash(Main.spriteBatch, center,
                        70f * (0.6f + 0.4f * flashI), prism, flashI, time * 0.9f);
                    if (_dyingAge < 3f)
                        StormLib.ImpactFlash(Main.spriteBatch, center, 48f,
                            GoldWarm, flashI * 0.7f, -time * 1.2f);
                }
                LumenLib.Flare(Main.spriteBatch, center,
                    (60f + 80f * phase) * (0.5f + 0.5f * fade), prism,
                    0.55f * fade + 0.25f * flashI, time * 0.4f);

                // EL AURORA COMPLETA (la sinfonía cromática de despedida).
                LumenLib.Aurora(Main.spriteBatch, center,
                    50f + 70f * phase, time, drift, 0.45f * fade, 15);

                // === 3. LOS SEIS RAYOS RADIALES (StormLib — reventando afuera) ===
                for (int i = 0; i < 6; i++)
                {
                    if (!StormLib.IsLit(seed + 130 + i * 29, flick, 0.55f)) continue;
                    float ang = i / 6f * MathHelper.TwoPi + seed % 7 * 0.13f;
                    Vector2 dir = new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang));
                    float inner = 26f + 85f * phase;
                    float outer = inner + 42f * burst;
                    StormLib.MultiBolt(Main.spriteBatch,
                        center + dir * inner, center + dir * outer,
                        seed + 130 + i * 29, flick,
                        Math.Max(3f, 7f * burst + 2f),
                        Tint(GoldWarm, 0.60f * fade), Tint(prism, 0.60f * fade),
                        Tint(WhiteIncan, 0.92f * fade),
                        0.9f * fade + 0.1f, 16f, 9);
                }

                // === 4. LAS RUNAS VOLANDO (la firma que revienta) ===
                DrawOrbitingRunes(center, 26f + 78f * phase, time, seed, fade);

                // === 5. LA ONDA DE CHOQUE expandiéndose ===
                float waveR = 30f + 150f * phase;
                Quad(VFXCore.Ring, center, VFXCore.RingQuadSize(waveR),
                    phase * 2.6f, Tint(GoldWarm, 0.40f * fade * fade));
                Quad(VFXCore.Ring, center, VFXCore.RingQuadSize(waveR * 0.66f),
                    -phase * 3.1f, Tint(prism, 0.30f * fade * fade));

                Main.spriteBatch.End();
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }
        }

        // ------------------------------------------------------------------
        //  HELPERS
        // ------------------------------------------------------------------

        /// <summary>La tabla de glifos del agujero (3 glifos representativos).</summary>
        private static Vector2[][] RuneGlyphs => new Vector2[][]
        {
            // EL SOL ROTO
            new Vector2[] { new(0f, -7f), new(0f, 7f), new(-3.5f, -3f), new(0f, -6.5f), new(3.5f, -3f), new(0f, -6.5f), new(-3.5f, 3.5f), new(3.5f, 3.5f), new(-2f, 5.5f), new(2f, 5.5f) },
            // EL CETRO
            new Vector2[] { new(0f, 7f), new(0f, -4f), new(0f, -4f), new(-3f, -7f), new(0f, -4f), new(3f, -7f), new(-2.5f, 0f), new(2.5f, 0f), new(-2.5f, 3f), new(2.5f, 3f) },
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
