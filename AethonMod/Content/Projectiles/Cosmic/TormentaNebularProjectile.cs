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

        // ================================================================
        //  v6.185 — LA TORMENTA HEREDADA (la letra del ataque básico del
        //  grimorio: «al nivel 100 el proyectil al explotar se transforma
        //  en el proyectil de Cetro de Tormenta Nebular durando unos 2
        //  segundos» · «al nivel 110 […] sus arcos de rayos se ramifican y
        //  saltan a enemigos, este cambio usa el mismo sistema» · «al nivel
        //  115 existe una pequeña posibilidad que del proyectil de Cetro de
        //  Tormenta Nebular implosione violentamente y se convierta en el
        //  proyectil del magnetar […] tendra una vida de 3 segundos»).
        //  · ai[2] = los SALTOS de ramificación que hereda (0 = la tormenta
        //    de siempre — el Cetro no la pasa).
        //  · DespertarHeredado viaja por SendExtraAI (para la implosión).
        // ================================================================
        /// <summary>El nivel de despertar de la estrella que la parió (-1 =
        /// la tormenta del Cetro de siempre: nada heredado).</summary>
        public int DespertarHeredado = -1;

        /// <summary>LOS SALTOS heredados (ai[2]): la tormenta ramifica sus
        /// descargas a N enemigos nuevos cercanos al punto de impacto.</summary>
        private int Ramas => (int)Projectile.ai[2];

        private bool Heredada => DespertarHeredado >= 0;

        public override void SendExtraAI(System.IO.BinaryWriter writer)
        {
            writer.Write(DespertarHeredado);
        }

        public override void ReceiveExtraAI(System.IO.BinaryReader reader)
        {
            DespertarHeredado = reader.ReadInt32();
        }

        // las cadenas de la ramificación (el dibujo)
        private readonly Vector2[] _ramFrom = new Vector2[6];
        private readonly Vector2[] _ramTo = new Vector2[6];
        private readonly float[] _ramLife = new float[6];

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
            for (int c = 0; c < 6; c++)
                if (_ramLife[c] > 0f) _ramLife[c] -= 1f;

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
                catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("TormentaNebularProjectile", exRegistro); }
            }

            // === EL DAÑO EN ÁREA del punto de impacto — v6.50 — GolpeMotor
            //     (el cauce del motor: crítica real, varianza, on-hit y sync
            //     MP del propio motor, resuelto en el cliente dueño; el
            //     debuff sigue siendo autoridad) ===
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;
                if ((npc.Center - point).Length() > StrikeR) continue;
                Content.Systems.GolpeMotor.Golpear(Projectile, npc, Projectile.damage, 2.5f, true);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    try { npc.AddBuff(BuffID.Electrified, 200); } catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("TormentaNebularProjectile", exRegistro); }
                if (Heredada && global::AethonMod.Content.Systems.DespertarGrimorio.QuemaduraDelVacio(DespertarHeredado))
                    TormentaQuemadura(npc);
            }

            // === v6.185 — LA RAMIFICACIÓN HEREDADA (110+): el mismo sistema
            //     de la estrella — el arco salta a N enemigos NUEVOS cercanos
            //     al punto de impacto, cada uno una vez (35 % del daño). ===
            if (Ramas > 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int golpe = Math.Max(1, (int)(Projectile.damage * 0.35f));
                Vector2 desde = point;
                NPC anterior = null;
                int slot = 0;
                for (int r = 0; r < Ramas && slot < 6; r++)
                {
                    NPC presa = ObjetivoNuevo(desde, 260f, anterior);
                    if (presa == null) break;
                    Content.Systems.GolpeMotor.Golpear(Projectile, presa, golpe, 1.5f, true);
                    try { presa.AddBuff(BuffID.Electrified, 180); } catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("TormentaNebularProjectile", exRegistro); }
                    if (global::AethonMod.Content.Systems.DespertarGrimorio.QuemaduraDelVacio(DespertarHeredado))
                        TormentaQuemadura(presa);
                    _ramFrom[slot] = desde;
                    _ramTo[slot] = presa.Center;
                    _ramLife[slot] = 9f;
                    anterior = presa;
                    desde = presa.Center;
                    slot++;
                }
            }
        }

        private void TormentaQuemadura(NPC npc)
        {
            try
            {
                int q = ModContent.BuffType<global::AethonMod.Content.Buffs.QuemaduraVacioExterior>();
                if (q > 0 && npc != null && npc.active && !npc.HasBuff(q)) npc.AddBuff(q, 600);
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("TormentaNebularProjectile", exRegistro); }
        }

        /// <summary>El enemigo válido más cercano a un punto que NO sea el
        /// anterior de la cadena (la ramificación busca uno nuevo).</summary>
        private static NPC ObjetivoNuevo(Vector2 desde, float radio, NPC excluir)
        {
            NPC mejor = null;
            float mejorD = radio * radio;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;
                if (npc == excluir) continue;
                float d = (npc.Center - desde).LengthSquared();
                if (d < mejorD) { mejorD = d; mejor = npc; }
            }
            return mejor;
        }

        /// <summary>v6.185 — LA IMPLOSIÓN (115+): al morir la tormenta
        /// heredada, con un 15 % colapsa en una ESTRELLA HIJA que persigue
        /// agresivamente, atraviesa paredes y vive 3 s: el Magnetar (115) ·
        /// la Enana Blanca (116) · la Estrella Muerta (117, 10 % menos
        /// seguimiento). Nace en la AUTORIDAD (un proyectil normal).</summary>
        public override void OnKill(int timeLeft)
        {
            try
            {
                if (!Heredada || Main.netMode == NetmodeID.MultiplayerClient) return;
                int cual = global::AethonMod.Content.Systems.DespertarGrimorio.ImplosionDeLaTormenta(DespertarHeredado, Projectile.identity);
                if (cual <= 0) return;

                int tipo = cual == 1
                    ? ModContent.ProjectileType<MagnetarProjectile>()
                    : cual == 2
                        ? ModContent.ProjectileType<WhiteDwarfProjectile>()
                        : ModContent.ProjectileType<DeadStarProjectile>();
                // EL SEGUIMIENTO HEREDADO en ai[2] (la Enana y la Muerta lo
                // leen): 3,0 px/t — la Estrella Muerta el 10 % menos (2,7).
                float velHeredada = cual == 3 ? 2.7f : 3.0f;
                int idx = Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center,
                    Vector2.Zero, tipo, Projectile.damage, 0f, Projectile.owner,
                    0f, (Projectile.identity % 9973 + 1) * 1f, velHeredada);
                if (idx >= 0)
                {
                    Main.projectile[idx].timeLeft = 180;          // 3 segundos
                    Main.projectile[idx].tileCollide = false;    // atraviesa paredes
                    Main.projectile[idx].netUpdate = true;
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item12.WithPitchOffset(0.42f).WithVolumeScale(0.7f), Projectile.Center);
                }
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("TormentaNebular.Implosion", exRegistro); }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // ============================================================
            //  CONTRATO DE BATCH A PRUEBA DE BALAS (v6.10).
            // ============================================================
            // v6.50.11 — sonda: cierra el lote del juego SOLO si hay un Begin
            // vivo (el try{End}catch disparaba una first-chance que tML 2026.07
            // registra como "Excepción silenciosa" — 27 stacks únicos en el
            // client.log del usuario, todas capturadas: ruido de diagnóstico).
            VFXCore.CerrarLoteSiAbierto();

            try
            {
                DrawStorm();
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }

            // v6.50.11 — CURACIÓN: el lote sale SIEMPRE ABIERTO y vanilla (si
            // llegó cerrado por un mod ajeno, se cura — el restore condicional
            // devolvía el veneno y tML mataba al proyectil: active=false).
            VFXCore.ReabrirLoteVanilla();
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

                // La MASA principal de la tormenta (respirando) — v6.25:
                // VIVE por la LUZ DEL MUNDO (la nube física se apaga en
                // las sombras, con piso — nunca invisible del todo).
                float breathe = 1f + 0.05f * (float)Math.Sin(time * 0.7f);
                BrumaFX.Cloud(Projectile.Center - Main.screenPosition,
                    92f * breathe, StormViolet, seed + 11, time,
                    puffs: 6, alpha: 0.34f * lifeFade, worldLit: true);

                // La CAPA ALTA tenue (la profundidad del cielo).
                BrumaFX.Cloud(Projectile.Center - Main.screenPosition - new Vector2(0f, 30f),
                    120f * breathe, StormCyan, seed + 47, time * 0.8f,
                    puffs: 5, alpha: 0.20f * lifeFade, worldLit: true);

                // DOS PUFFS GIANTES girando lento (la deriva de la tormenta).
                for (int i = 0; i < 2; i++)
                {
                    float ang = time * 0.10f * (i == 0 ? 1f : -1f) + i * MathHelper.Pi;
                    Vector2 pos = Projectile.Center - Main.screenPosition + new Vector2(
                        (float)Math.Cos(ang) * 30f, (float)Math.Sin(ang) * 12f - 8f);
                    BrumaFX.Puff(pos, 52f, i == 0 ? StormViolet : StormCyan,
                        seed + 83 + i * 37, time,
                        alpha: 0.22f * lifeFade, quality: 0.6f, worldLit: true);
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

                // === v6.185 — LAS CADENAS DE LA RAMIFICACIÓN (el mismo dibujo
                //     de las cadenas de la estrella) ===
                for (int c = 0; c < 6; c++)
                {
                    if (_ramLife[c] <= 0f) continue;
                    if (!StormLib.IsLit(seed + 61 + c * 37, flick, 0.60f)) continue;
                    float a = MathHelper.Clamp(_ramLife[c] / 9f, 0f, 1f);
                    StormLib.ChainBolt(Main.spriteBatch,
                        _ramFrom[c] - Main.screenPosition, _ramTo[c] - Main.screenPosition,
                        seed + 61 + c * 37, flick, 4.2f,
                        Tint(StarBlue, 0.75f * a), Tint(WhiteIncan, 0.9f * a),
                        1f, 7, 10f);
                }

                Main.spriteBatch.End();
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
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
        private static readonly Vector2[][] RuneGlyphs = new Vector2[][]
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
