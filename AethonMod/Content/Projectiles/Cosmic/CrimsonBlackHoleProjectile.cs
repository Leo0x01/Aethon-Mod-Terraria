using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using AethonMod.Content.Particles;
using AethonMod.Content.Effects;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// CrimsonBlackHoleProjectile — v6.05 — EL AGUJERO NEGRO CARMESÍ DE LA REFERENCIA.
    ///
    /// MÉTODO (petición del usuario): "primero toma una copia exacta del
    /// agujero negro funcional que tenemos, y a partir de ahí modifica sus
    /// parámetros: disco de acreción más grande y de otro color, el agujero
    /// un poco más pequeño, mejorar la animación". ESTO ES EXACTAMENTE ESO:
    /// el MISMO RealBlackHoleShader (marcha de luz de 75 pasos con lensing
    /// gravitacional real — el render del agujero funcional que ya nos
    /// gustaba) con los PARÁMETROS recalibrados según la referencia y las
    /// fórmulas de la física real:
    ///
    ///   · DISCO MÁS GRANDE  — accretionDiskRadius 0.40 → 0.48 (tubo del
    ///     toro): el disco abraza el horizonte y su borde exterior pasa de
    ///     3.8× a ~4.9× el radio de la sombra (la referencia mide ~3.5-4×).
    ///   · OTRO COLOR        — naranja (245,105,61) → carmesí-fucsia
    ///     (255,45,100): la paleta de la referencia (núcleo blanco-rosado →
    ///     magenta neón → carmesí profundo).
    ///   · AGUJERO MÁS PEQUEÑO — blackHoleRadius 0.30 → 0.25 (-17%): sombra
    ///     más contenida, disco relativamente más dominante.
    ///   · ANIMACIÓN MEJORADA — (1) el tiempo del shader corre ×1.35: el
    ///     plasma del disco HIERVE más vivo; (2) la cámara PRECESIONA con dos
    ///     frecuencias incommensurables (bamboleo orgánico del plano del
    ///     disco, ±0.06 rad); (3) el conjunto RESPIRA (±1.8%); (4) el halo
    ///     late a dos frecuencias; (5) DOPPLER BEAMING real: δ = 1/(γ(1−β·cosθ))
    ///     → el lado que se acerca (izquierda, como la referencia) brilla
    ///     ~δ³ ≈ 3-4× más; (6) ANILLO DE FOTONES rosa pálido pulsante
    ///     (r_fotón = 1.7·r_horizonte en el espacio del shader).
    ///
    /// FÍSICA (formulas investigadas — ver research/blackhole/):
    ///   · Radio de Schwarzschild: r_s = 2GM/c²  (el "blackHoleRadius").
    ///   · Esfera de fotones: r_ph = 1.5·r_s  (fotones orbitando).
    ///   · Sombra aparente: R_sh = (√27/2)·r_s ≈ 2.6·r_s.
    ///   · ISCO: r_isco = 3·r_s — el borde INTERNO del disco de acreción
    ///     (con tubo 0.48 y horizonte 0.25 el borde interno visible nace
    ///     pegado al horizonte, como en la referencia).
    ///   · Kepler: v(r) = √(GM/r); en el ISCO v ≈ 0.41c → beaming δ³.
    ///   · Lente: α = 4GM/(c²·b) — la desviación que la marcha de 75 pasos
    ///     integra paso a paso con curvatura ~1/r² (la misma del agujero
    ///     funcional, INTACTA).
    ///
    /// La física de juego (aura con ticks que aceleran cerca del centro,
    /// atracción 10× la del sol, devora balas al cruzar el horizonte,
    /// persecución lenta, anillo de Einstein final) es la MISMA copia exacta
    /// del BlackHoleProjectile — que queda INTACTO. La lente de pantalla
    /// (BlackHoleLensSystem) sigue curvando el fondo alrededor del
    /// horizonte y pinta este render ENCIMA (DrawCoreVisuals).
    /// </summary>
    public class CrimsonBlackHoleProjectile : ModProjectile
    {
        /// <summary>Shader del núcleo — estático: compartido por todas las
        /// instancias (el MISMO RealBlackHoleShader del agujero funcional:
        /// copia exacta, solo cambian los parámetros que se le pasan).</summary>
        private static Ref<Effect> _shader;
        private static bool _shaderFailed;

        /// <summary>Tiempo visual de vida — usada para el pop elástico de aparición.</summary>
        public ref float VisualsTime => ref Projectile.ai[0];

        /// <summary>
        /// Multiplicador del radio del campo de fuerza sobre el horizonte
        /// de sucesos (2.2×: envuelve el disco de acreción).
        /// </summary>
        private const float ShieldRadiusMult = 2.2f;

        // ==================================================================
        //  PARÁMETROS VISUALES DEL AGUJERO CARMESÍ (los que se pidieron)
        // ==================================================================

        /// <summary>Radio del horizonte en unidades shader — el agujero un
        /// poco MÁS PEQUEÑO que el funcional (0.30 → 0.25: -17% en píxeles).</summary>
        private const float HoleRadius = 0.25f;

        /// <summary>Grosor del tubo del toro del disco — el disco MÁS
        /// GRANDE que el funcional (0.40 → 0.48: borde exterior ~4.9× la
        /// sombra, como la banda amplia de la referencia).</summary>
        private const float DiskTubeRadius = 0.48f;

        /// <summary>Color base del disco — la paleta de la referencia:
        /// carmesí-fucsia (núcleo blanco-rosado → magenta → carmesí).</summary>
        private static readonly Color DiskColor = new Color(255, 45, 100);

        /// <summary>Achatado vertical del toro — banda un poco MÁS FINA que
        /// la del funcional (0.33 → 0.28): anillo elíptico casi de canto,
        /// como el de la referencia (inclinación ~17°).</summary>
        private const float DiskFlattenY = 0.28f;

        /// <summary>Inclinación de la cámara (rad) — ~17°: el plano del
        /// disco se ve casi de canto con su arco de lente arriba (Gargantua).</summary>
        private const float CameraTilt = 0.30f;

        /// <summary>Impulso del lienzo: el disco gana tamaño en pantalla
        /// (+10%) sin tocar la cobertura del shader (zoom constante).</summary>
        private const float CanvasBoost = 1.10f;

        /// <summary>Velocidad del tiempo del shader — el disco HIERVE más
        /// rápido (×1.35) y el remolino de la lente gira más vivo.</summary>
        private const float TimeScale = 1.35f;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            // Copia exacta del agujero funcional: mismo hitbox (el canvas del
            // shader y el aura escalan con width).
            Projectile.width = 96;
            Projectile.height = 96;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 600;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
        }

        public override void AI()
        {
            // === POP ELÁSTICO DE APARICIÓN (copia exacta) ===
            Projectile.scale = ElasticOut(Utils.GetLerpValue(0f, 120f, VisualsTime, true)) *
                               (float)Math.Sqrt(Utils.GetLerpValue(0f, 60f, VisualsTime, true));
            VisualsTime += 1f;

            // ================================================================
            //  SECUENCIA DE MUERTE (copia exacta: crecimiento → evaporación)
            // ================================================================
            float expansion = 0f;
            if (Projectile.timeLeft > 36f && Projectile.timeLeft <= 90f)
            {
                expansion = 1f - (Projectile.timeLeft - 36f) / 54f;
                Projectile.scale *= 1f + expansion * 0.6f;
            }
            else if (Projectile.timeLeft <= 36f)
            {
                if (Projectile.localAI[1] <= 0f)
                    Projectile.localAI[1] = 0.3f * Projectile.width *
                                            Math.Max(Projectile.scale, 0.08f) * ShieldRadiusMult;

                float collapse = Utils.GetLerpValue(36f, 0f, Projectile.timeLeft, true);
                Projectile.scale *= 1f - collapse;
                expansion = 1f;
            }

            // La materia absorbida ACELERA hacia el centro al evaporarse.
            ParticleManager.PullToGlobalBoost = 1f + expansion * 5f;

            // === MOVIMIENTO: deriva lenta y frenado (copia exacta) ===
            Projectile.velocity *= 0.97f;

            // === PERSIGUE LIGERAMENTE A LOS ENEMIGOS (copia exacta) ===
            NPC prey = FindNearestEnemy(650f);
            if (prey != null)
            {
                Vector2 toPrey = prey.Center - Projectile.Center;
                if (toPrey.LengthSquared() > 120f)
                {
                    toPrey.Normalize();
                    Projectile.velocity += toPrey * 0.07f;
                }
                float spd = Projectile.velocity.Length();
                if (spd > 2.4f)
                    Projectile.velocity *= 2.4f / spd;
            }

            float targetRotation = Projectile.velocity.X * 0.04f;
            Projectile.rotation += MathHelper.WrapAngle(targetRotation - Projectile.rotation) * 0.3f;

            // === PARTÍCULAS (solo cliente) — paleta carmesí/fucsia de la referencia ===
            if (Main.netMode != NetmodeID.Server)
            {
                SpawnAbsorbedDusts();
                SpawnCapturedEnergySparks();
                AttractNearbyDust();
                SpawnLibraryAbsorbedMatter();
                SpawnLibraryAccretionDisk();
            }

            // === ATRACCIÓN GRAVITACIONAL DE ENEMIGOS (copia exacta: 10× el sol) ===
            float gravityRadius = 450f * (1f + expansion * 0.6f);
            const float gravityStrength = 2.6f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy()) continue;
                Vector2 toCenter = Projectile.Center - npc.Center;
                float dist = toCenter.Length();
                if (dist > gravityRadius || dist < 5f) continue;
                float strength = (1f - dist / gravityRadius) * gravityStrength;
                if (toCenter.LengthSquared() > 0.01f)
                {
                    toCenter.Normalize();
                    npc.velocity += toCenter * strength;
                }
            }

            // === LA GRAVEDAD DOBLA Y DEVORA LOS PROYECTILES ENEMIGOS (copia exacta) ===
            if (Main.netMode != NetmodeID.Server)
            {
                float projGravityRadius = gravityRadius * 0.9f;
                foreach (Projectile pr in Main.ActiveProjectiles)
                {
                    if (pr == null || !pr.active || !pr.hostile || pr.friendly) continue;
                    Vector2 toHorizon = Projectile.Center - pr.Center;
                    float d = toHorizon.Length();
                    if (d > projGravityRadius || d < 4f) continue;

                    if (d < ShieldRadius * 0.6f)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            pr.Kill();
                        for (int i = 0; i < 5; i++)
                        {
                            Dust d2 = Dust.NewDustPerfect(pr.Center, DustID.PinkCrystalShard,
                                (pr.Center - Projectile.Center) * -0.02f +
                                new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), Main.rand.NextFloat(-1.5f, 1.5f)),
                                200, new Color(255, 130, 200), 0.7f);
                            d2.noGravity = true;
                            d2.fadeIn = 0f;
                        }
                        continue;
                    }

                    float ps = (1f - d / projGravityRadius) * gravityStrength * 0.35f;
                    if (toHorizon.LengthSquared() > 0.01f)
                    {
                        toHorizon.Normalize();
                        pr.velocity += toHorizon * ps;
                    }
                }
            }

            // === AURA DE DAÑO: TICKS QUE ACELERAN CERCA DEL CENTRO (copia exacta) ===
            if (Main.netMode != NetmodeID.MultiplayerClient && VisualsTime > 0f)
            {
                float lifeProgress = MathHelper.Clamp(1f - Projectile.timeLeft / 600f, 0f, 1f);
                float auraRadius = ShieldRadius * (1.15f + 0.75f * lifeProgress);
                int auraDamage = Math.Max(1, (int)(Projectile.damage * (0.35f + 0.30f * lifeProgress)));
                int t = (int)VisualsTime;
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.CanBeChasedBy()) continue;
                    float dist = (npc.Center - Projectile.Center).Length();
                    if (dist > auraRadius) continue;

                    float prox = MathHelper.Clamp(dist / auraRadius, 0f, 1f);
                    int interval = 6 + (int)(prox * 18f); // 6 (centro) → 24 (borde)
                    if ((t + npc.whoAmI) % interval != 0) continue;

                    Vector2 toCenter = Projectile.Center - npc.Center;
                    if (toCenter.LengthSquared() > 0.01f)
                    {
                        toCenter.Normalize();
                        npc.velocity += toCenter * 0.8f;
                    }
                    npc.SimpleStrikeNPC(auraDamage, npc.direction, false, 0f, DamageClass.Magic);
                }
            }

            // === ILUMINACIÓN PULSANTE (carmesí-fucsia del disco) ===
            float pulse = 0.8f + 0.2f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5f);
            Lighting.AddLight(Projectile.Center, new Vector3(1.0f * pulse, 0.28f * pulse, 0.42f * pulse));
        }

        /// <summary>El enemigo chaseable más cercano dentro de maxDist (copia exacta).</summary>
        private NPC FindNearestEnemy(float maxDist)
        {
            NPC best = null;
            float bestDist = maxDist * maxDist;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy()) continue;
                float d2 = (npc.Center - Projectile.Center).LengthSquared();
                if (d2 < bestDist)
                {
                    bestDist = d2;
                    best = npc;
                }
            }
            return best;
        }

        /// <summary>Radio actual del campo (px): 2.2× el horizonte, con la
        /// escala pre-colapso durante la evaporación (copia exacta).</summary>
        private float ShieldRadius
        {
            get
            {
                if (Projectile.localAI[1] > 4f)
                    return Projectile.localAI[1];
                return 0.3f * Projectile.width * Math.Max(Projectile.scale, 0.08f) * ShieldRadiusMult;
            }
        }

        // ------------------------------------------------------------------
        //  PARTÍCULAS — paleta carmesí / fucsia / blanco-rosado (referencia)
        // ------------------------------------------------------------------

        /// <summary>Materia absorbida (dusts): polvo carmesí/fucsia cayendo
        /// en espiral, blanco-rosado al rozar el horizonte.</summary>
        private void SpawnAbsorbedDusts()
        {
            float deathSpeedBoost = 1f;
            if (Projectile.timeLeft <= 90f)
                deathSpeedBoost = 1f + (90f - Projectile.timeLeft) / 90f * 2f;

            for (int i = 0; i < 2; i++)
            {
                float angle = Projectile.rotation * 1.5f + i * (MathHelper.TwoPi / 2f) +
                              Main.rand.NextFloat(-0.25f, 0.25f);
                float dist = 100f + 55f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2f + i);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist,
                    (float)Math.Sin(angle) * dist);

                Vector2 toCenter = Projectile.Center - spawnPos;
                float speed = (4f + 4f * (1f - dist / 155f)) * deathSpeedBoost;
                if (toCenter.LengthSquared() > 0.01f)
                {
                    toCenter.Normalize();
                    Vector2 velocity = toCenter * speed;

                    float angleToCenter = (float)Math.Atan2(toCenter.Y, toCenter.X);
                    velocity = velocity.RotateTowards(angleToCenter + MathHelper.PiOver2 * 0.3f, 0.5f);

                    // Materia carmesí: fucsia→carmesí cayendo hacia el
                    // horizonte, blanco-rosado al rozarlo (Doppler).
                    Color color;
                    if (dist < 60f)
                    {
                        color = new Color(255, 240, 250); // blanco-rosado al borde
                    }
                    else
                    {
                        color = Main.rand.Next(3) switch
                        {
                            0 => new Color(238, 40, 90),   // carmesí vivo
                            1 => new Color(255, 70, 150),   // fucsia
                            _ => new Color(190, 20, 80),    // carmesí profundo
                        };
                    }

                    Dust d = Dust.NewDustPerfect(spawnPos, DustID.Crimson,
                        velocity, 150, color, 1.2f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                    d.scale = Main.rand.NextFloat(0.8f, 1.4f);
                }
            }
        }

        /// <summary>Chispas rosas encantadas capturadas por el campo.</summary>
        private void SpawnCapturedEnergySparks()
        {
            if (Main.rand.NextBool(12))
            {
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(140f, 220f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist,
                    (float)Math.Sin(angle) * dist);
                Vector2 vel = (Projectile.Center - spawnPos) * 0.03f;
                Dust d = Dust.NewDustPerfect(spawnPos, DustID.Enchanted_Pink,
                    vel, 255, new Color(255, 130, 190), 0.9f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        /// <summary>Devora el polvo del ambiente en espiral (copia exacta).</summary>
        private void AttractNearbyDust()
        {
            float radius = 260f;
            for (int i = 0; i < Main.maxDust; i++)
            {
                Dust d = Main.dust[i];
                if (!d.active || d.noGravity) continue;
                Vector2 toCenter = Projectile.Center - d.position;
                float dist = toCenter.Length();
                if (dist > radius || dist < 4f) continue;
                float strength = (1f - dist / radius) * 0.35f;
                toCenter.Normalize();
                Vector2 pull = toCenter * strength + new Vector2(-toCenter.Y, toCenter.X) * strength * 0.35f;
                d.velocity += pull;
            }
        }

        // ------------------------------------------------------------------
        //  PARTÍCULAS DE LA LIBRERÍA PROPIA (capa AboveLens)
        // ------------------------------------------------------------------

        /// <summary>Materia absorbida: estelas TrailGlow carmesí→blanco-rosado
        /// que caen en espiral hacia el horizonte y mueren devoradas.</summary>
        private void SpawnLibraryAbsorbedMatter()
        {
            for (int i = 0; i < 2; i++)
            {
                float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(95f, 165f) * MathHelper.Max(Projectile.scale, 0.4f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist,
                    (float)Math.Sin(angle) * dist);

                Vector2 inward = new Vector2(-(float)Math.Cos(angle), -(float)Math.Sin(angle));
                Vector2 tangent = new Vector2(-(float)Math.Sin(angle), (float)Math.Cos(angle));
                Vector2 velocity = inward * Main.rand.NextFloat(1.8f, 2.8f) +
                                   tangent * Main.rand.NextFloat(0.25f, 0.5f);

                Color start = new Color(255, 60, 130, 190);
                Color end = new Color(255, 244, 235, 235);

                var p = new ParticleData
                {
                    Position = spawnPos,
                    Velocity = velocity,
                    Scale = new Vector2(1.35f, 0.42f),
                    Rotation = angle + MathHelper.Pi,
                    PackedColor = ParticleManager.PackColor(start),
                    PackedStartColor = ParticleManager.PackColor(start),
                    PackedEndColor = ParticleManager.PackColor(end),
                    TimeLeft = 75,
                    Duration = 75,
                    TextureId = ParticleTex.TrailGlow,
                    BlendMode = 1,
                    LayerPriority = LayerPriorities.AboveLens,
                };
                p.UserData0 = Projectile.Center.X;
                p.UserData1 = Projectile.Center.Y;
                p.UserData3 = 0.09f;
                p.EnableComponent(ComponentFlag.PullTo);
                p.EnableComponent(ComponentFlag.FadeOut);
                p.EnableComponent(ComponentFlag.ColorShift);
                ParticleManager.Spawn(p);
            }
        }

        /// <summary>Disco de acreción: estelas TrailGlow fucsia orbitando con
        /// rotación sincronizada — plasma carmesí/fucsia EN la banda del
        /// disco visible (1.5..3.4× el horizonte visual, que ahora es más
        /// pequeño y el disco más grande).</summary>
        private void SpawnLibraryAccretionDisk()
        {
            if (Main.rand.NextBool(4))
            {
                // Horizonte VISUAL del carmesí: 0.25 unidades shader
                // (×96 px/unidad ×1.1 de impulso del lienzo).
                float horizon = 0.275f * Projectile.width * MathHelper.Max(Projectile.scale, 0.4f);
                float radius = Main.rand.NextFloat(1.5f, 3.4f) * horizon;
                float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                float angVel = 0.25f; // el disco carmesí gira un poco más vivo

                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * radius,
                    (float)Math.Sin(angle) * radius);

                var p = new ParticleData
                {
                    Position = spawnPos,
                    Velocity = Vector2.Zero,
                    Scale = new Vector2(1.8f, 0.45f),
                    Rotation = angle + MathHelper.PiOver2,
                    RotationSpeed = angVel,
                    PackedColor = ParticleManager.PackColor(new Color(255, 70, 140, 200)),
                    PackedStartColor = ParticleManager.PackColor(new Color(255, 120, 180, 200)),
                    PackedEndColor = ParticleManager.PackColor(new Color(90, 0, 45, 40)),
                    TimeLeft = 48,
                    Duration = 48,
                    TextureId = ParticleTex.TrailGlow,
                    BlendMode = 1,
                    LayerPriority = LayerPriorities.AboveLens,
                };
                p.UserData0 = Projectile.Center.X;
                p.UserData1 = Projectile.Center.Y;
                p.UserData2 = angVel;
                p.UserData3 = radius;
                p.EnableComponent(ComponentFlag.Orbit);
                p.EnableComponent(ComponentFlag.ColorShift);
                p.EnableComponent(ComponentFlag.FadeOut);
                ParticleManager.Spawn(p);
            }
        }

        // ------------------------------------------------------------------
        //  RENDER — COPIA EXACTA DEL AGUJERO FUNCIONAL + PARÁMETROS NUEVOS
        // ------------------------------------------------------------------

        public override bool PreDraw(ref Color lightColor)
        {
            // La lente va DETRÁS: el BlackHoleLensSystem pinta el núcleo
            // ENCIMA de la distorsión llamando a DrawCoreVisuals.
            if (BlackHoleLensSystem.LensActive)
                return false;

            DrawCoreVisuals(Projectile, true);
            RestoreSpriteBatch();
            return false;
        }

        /// <summary>
        /// Dibuja el agujero carmesí completo: halo + RealBlackHoleShader
        /// (¡el MISMO raymarching de 75 pasos del agujero funcional! copia
        /// exacta, solo cambian los parámetros) + refuerzo del horizonte +
        /// DOPPLER BEAMING + anillo de fotones pulsante. Compartido entre el
        /// pase del mundo (PreDraw, endActiveBatch=true) y el pase posterior
        /// a la lente (BlackHoleLensSystem, endActiveBatch=false: el batch
        /// llega cerrado). Contrato de batch idéntico al original: al
        /// terminar queda CERRADO (el llamador lo restaura).
        /// </summary>
        internal static void DrawCoreVisuals(Projectile p, bool endActiveBatch)
        {
            if (!_shaderFailed && _shader == null)
            {
                try
                {
                    _shader = new Ref<Effect>(ModContent.Request<Effect>(
                        "AethonMod/Content/Effects/Shaders/RealBlackHoleShader",
                        AssetRequestMode.ImmediateLoad).Value);
                }
                catch
                {
                    _shaderFailed = true;
                }
            }

            try
            {
                Vector2 drawPos = p.Center - Main.screenPosition;
                float time = Main.GlobalTimeWrappedHourly;

                // === 1. HALO CARMESÍ EXTERIOR (aura de brasa fucsia de fondo) ===
                // Copia del halo del funcional, recolor: carmesí profundo.
                Texture2D glowTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                if (endActiveBatch)
                    Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                // Latido a DOS frecuencias (animación mejorada: batimiento vivo).
                float haloPulse = 0.85f + 0.11f * (float)Math.Sin(time * 3.1f) +
                                  0.04f * (float)Math.Sin(time * 0.9f + 1.7f);
                Main.spriteBatch.Draw(glowTex, drawPos, null,
                    new Color(70, 6, 28, 46) * haloPulse * p.scale, 0f,
                    glowTex.Size() * 0.5f, 3.4f * p.scale, SpriteEffects.None, 0f);
                Main.spriteBatch.End();

                if (_shader != null && _shader.Value != null)
                {
                    // ============================================================
                    //  2. REALBLACKHOLESHADER — LA COPIA EXACTA CON PARÁMETROS
                    //     CARMESÍ (marcha de luz de 75 pasos + lensing real)
                    // ============================================================
                    Effect shader = _shader.Value;

                    // Lienzo que crece con la escala (fix v5.90 del original)
                    // + impulso del 10% (disco más grande en pantalla) +
                    // RESPIRACIÓN sutil (animación mejorada).
                    float targetSize = 256f;
                    float breathe = 1f + 0.018f * (float)Math.Sin(time * 1.27f + p.whoAmI * 0.9f);
                    float canvasPx = targetSize * Math.Max(p.scale, 0.08f) * CanvasBoost * breathe;
                    float zoomBase = p.width / targetSize * 2f;

                    // --- PARÁMETROS MODIFICADOS (el corazón del cambio) ---
                    // Agujero un poco más pequeño: 0.30 → 0.25.
                    shader.Parameters["blackHoleRadius"].SetValue(HoleRadius);
                    shader.Parameters["blackHoleCenter"].SetValue(Vector3.Zero);
                    shader.Parameters["aspectRatioCorrectionFactor"].SetValue(1f);
                    // Otro color: naranja → carmesí-fucsia de la referencia.
                    shader.Parameters["accretionDiskColor"].SetValue(DiskColor.ToVector3());
                    // Inclinación ~17°: banda casi de canto (referencia).
                    shader.Parameters["cameraAngle"].SetValue(CameraTilt);
                    // ANIMACIÓN MEJORADA: la cámara PRECESIONA — el eje de
                    // rotación oscila con dos frecuencias incommensurables
                    // (±0.05/±0.06 rad): el plano del disco bambolea orgánico
                    // sobre la inclinación base que ya tenía el funcional.
                    float swayX = 1f + 0.05f * (float)Math.Sin(time * 0.63f + p.whoAmI * 0.7f);
                    float swayZ = p.rotation + 0.06f * (float)Math.Sin(time * 0.41f);
                    shader.Parameters["cameraRotationAxis"].SetValue(
                        new Vector3(p.velocity.Y * -0.022f + swayX, 0f, swayZ));
                    // Banda un poco más fina: 0.33 → 0.28 (elipse de canto).
                    shader.Parameters["accretionDiskScale"].SetValue(new Vector3(1f, DiskFlattenY, 1f));
                    shader.Parameters["zoom"].SetValue(Vector2.One * zoomBase);
                    // DISCO MÁS GRANDE: tubo 0.40 → 0.48 (borde ~4.9× sombra).
                    shader.Parameters["accretionDiskRadius"].SetValue(Math.Min(p.scale, 1f) * DiskTubeRadius);
                    // ANIMACIÓN MEJORADA: el tiempo corre ×1.35 — el plasma
                    // del disco HIERVE más rápido y la lente remolina más viva.
                    shader.Parameters["globalTime"].SetValue(time * TimeScale);

                    // FireNoiseB como textura de ruido del disco de acreción (s1)
                    Texture2D fireNoise = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Textures/FireNoiseB").Value;
                    Main.graphics.GraphicsDevice.Textures[1] = fireNoise;
                    Main.graphics.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;

                    // InvisiblePixel como canvas (s0)
                    Texture2D pixel = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Textures/InvisiblePixel").Value;

                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                        SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone,
                        null, Main.GameViewMatrix.TransformationMatrix);
                    shader.CurrentTechnique.Passes[0].Apply();
                    Main.spriteBatch.Draw(pixel, drawPos, null, Color.White, 0f,
                        pixel.Size() * 0.5f, canvasPx, SpriteEffects.None, 0f);
                    Main.spriteBatch.End();

                    // === 3. REFUERZO DEL EVENT HORIZON (copia exacta, radio nuevo) ===
                    // radio del horizonte en píxeles = blackHoleRadius * zoom * (canvas / 2)
                    float pxPerUnit = zoomBase * canvasPx * 0.5f;
                    float eventHorizonPx = HoleRadius * pxPerUnit;
                    if (eventHorizonPx > 2f)
                    {
                        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                            null, Main.GameViewMatrix.TransformationMatrix);
                        float horizonScale = (eventHorizonPx * 2.15f) / glowTex.Width;
                        Main.spriteBatch.Draw(glowTex, drawPos, null,
                            new Color(0, 0, 0, 215), 0f, glowTex.Size() * 0.5f,
                            horizonScale, SpriteEffects.None, 0f);
                        Main.spriteBatch.End();
                    }

                    // ============================================================
                    //  4. DOPPLER BEAMING — el lado que se ACERCA brilla más
                    // ============================================================
                    // Física real: δ = 1/(γ(1−β·cosθ)); el brillo observado
                    // escala ~δ³. Con v_kepler ≈ 0.4c en el borde interno el
                    // lado que se acerca (la IZQUIERDA, como en la referencia)
                    // brilla 3-4× más y el que se aleja se apaga hacia el
                    // carmesí profundo. Dos velos aditivos sobre el render:
                    float diskOuterPx = (0.75f + DiskTubeRadius) * pxPerUnit;
                    float doppler = 0.85f + 0.15f * (float)Math.Sin(time * 1.7f);
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                        SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                        null, Main.GameViewMatrix.TransformationMatrix);

                    // Lado que se ACERCA (izquierda): blanco-rosado cegador.
                    Main.spriteBatch.Draw(glowTex,
                        drawPos + new Vector2(-0.42f * diskOuterPx, -0.05f * diskOuterPx),
                        null, new Color(255, 235, 248, 115) * doppler, 0f,
                        glowTex.Size() * 0.5f,
                        (0.85f * diskOuterPx) / glowTex.Width, SpriteEffects.None, 0f);

                    // Lado que se ALEJA (derecha): brasa carmesí tenue.
                    Main.spriteBatch.Draw(glowTex,
                        drawPos + new Vector2(0.45f * diskOuterPx, 0.05f * diskOuterPx),
                        null, new Color(140, 12, 48, 55), 0f,
                        glowTex.Size() * 0.5f,
                        (1.05f * diskOuterPx) / glowTex.Width, SpriteEffects.None, 0f);

                    Main.spriteBatch.End();

                    // ============================================================
                    //  5. ANILLO DE FOTONES ROSA PÁLIDO (pulsante)
                    // ============================================================
                    // En el shader el anillo brilla a 1.7·r_horizonte del
                    // centro (distGlow): aquí lo REFUERZO con la textura Ring
                    // en ese mismo radio, blanco-rosado, latiendo — la firma
                    // "anillo de fotones rosa pálido" de la referencia.
                    float ringPx = HoleRadius * 1.7f * pxPerUnit;
                    if (ringPx > 3f)
                    {
                        Texture2D ringTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Ring").Value;
                        float ringPulse = 0.75f + 0.25f * (float)Math.Sin(time * 2.3f + p.whoAmI);
                        // Radio visible de Ring ≈ 0.92× su mitad (calibración v5.93).
                        float ringScale = ringPx / (ringTex.Width * 0.5f * 0.92f);
                        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                            null, Main.GameViewMatrix.TransformationMatrix);
                        Main.spriteBatch.Draw(ringTex, drawPos, null,
                            new Color(255, 205, 228, 130) * ringPulse, 0f,
                            ringTex.Size() * 0.5f, ringScale, SpriteEffects.None, 0f);
                        Main.spriteBatch.End();
                    }
                }
                else
                {
                    // === FALLBACK: dibujado manual si el shader no carga ===
                    DrawFallback(p, drawPos);
                }
            }
            catch
            {
                // Cierre defensivo SOLO en el path de error.
                try { Main.spriteBatch.End(); } catch { }
            }
        }

        /// <summary>Radio del campo (px), compartido con el aura y el OnKill
        /// (copia exacta de la física del funcional).</summary>
        internal static float GetShieldRadius(Projectile p)
        {
            if (p.localAI[1] > 4f)
                return p.localAI[1];
            return 0.3f * p.width * Math.Max(p.scale, 0.08f) * ShieldRadiusMult;
        }

        /// <summary>Restaura el SpriteBatch al estado que tML espera tras PreDraw
        /// (copia exacta).</summary>
        private static void RestoreSpriteBatch()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise,
                null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>Dibujado manual de respaldo (copia del fallback del
        /// funcional, recolor carmesí: vórtice + anillo de fotones +
        /// aberración cromática sutil).</summary>
        private static void DrawFallback(Projectile p, Vector2 drawPos)
        {
            float pulse = 0.9f + 0.1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4f);
            Texture2D glowTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
            Texture2D vortexTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Vortex").Value;
            Texture2D ringTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Ring").Value;
            float s = p.scale;

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);

            // Disco de acreción frontal (elíptico, carmesí-fucsia)
            Main.spriteBatch.Draw(vortexTex, drawPos, null,
                new Color(255, 80, 145, 200), p.rotation * 2f,
                vortexTex.Size() * 0.5f, new Vector2(1.5f, 0.5f) * s, SpriteEffects.None, 0f);

            // Disco trasero (anillo de Einstein)
            Main.spriteBatch.Draw(vortexTex, drawPos - new Vector2(0f, 4f * s), null,
                new Color(170, 0, 65, 100), -p.rotation * 2f,
                vortexTex.Size() * 0.5f, new Vector2(1.5f, 0.5f) * s, SpriteEffects.FlipVertically, 0f);

            // Beaming relativístico (lado que se acerca, blanco-rosado)
            Main.spriteBatch.Draw(vortexTex, drawPos - new Vector2(3f * s, 0f), null,
                new Color(255, 210, 235, 130), p.rotation * 2f,
                vortexTex.Size() * 0.5f, new Vector2(1.5f, 0.5f) * s, SpriteEffects.None, 0f);

            Main.spriteBatch.End();

            // Event horizon
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(glowTex, drawPos, null,
                Color.Black, 0f, glowTex.Size() * 0.5f,
                0.8f * s, SpriteEffects.None, 0f);
            Main.spriteBatch.End();

            // Anillo de fotones + aberración cromática (recolor carmesí)
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
            // Horizonte visual del carmesí (0.25 unidades shader × el impulso
            // del lienzo — véase pxPerUnit del pase del shader).
            float horizonPx = HoleRadius * CanvasBoost * p.width * Math.Max(p.scale, 0.08f);
            float photonR = horizonPx * 1.3f * pulse / 0.92f;
            float photonScale = photonR / (ringTex.Width * 0.5f);
            Main.spriteBatch.Draw(ringTex, drawPos - new Vector2(2f * s, 0f), null,
                new Color(255, 0, 80, 80), 0f, ringTex.Size() * 0.5f,
                photonScale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(ringTex, drawPos, null,
                new Color(255, 120, 200, 80), 0f, ringTex.Size() * 0.5f,
                photonScale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(ringTex, drawPos + new Vector2(2f * s, 0f), null,
                new Color(120, 0, 60, 80), 0f, ringTex.Size() * 0.5f,
                photonScale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(ringTex, drawPos, null,
                new Color(255, 240, 250, 220), 0f, ringTex.Size() * 0.5f,
                photonScale, SpriteEffects.None, 0f);
            Main.spriteBatch.End();
        }

        // ------------------------------------------------------------------
        //  IMPACTO Y MUERTE (copia exacta de la física, paleta carmesí)
        // ------------------------------------------------------------------

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode == NetmodeID.Server) return;

            // Micro-colapso carmesí sobre el objetivo
            ParticlePresets.Implosion(target.Center, 70f, 16, new Color(255, 40, 100), 18);
            ParticlePresets.RingPulse(target.Center, 90f, new Color(255, 150, 190, 170), 22);

            // Implosión: 50 partículas carmesí convergiendo en espiral
            for (int i = 0; i < 50; i++)
            {
                float angle = (MathHelper.TwoPi / 50) * i + Main.rand.NextFloat(-0.2f, 0.2f);
                float dist = Main.rand.NextFloat(80f, 140f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist, (float)Math.Sin(angle) * dist);
                Vector2 toCenter = Projectile.Center - spawnPos;
                if (toCenter.LengthSquared() > 0.01f)
                {
                    toCenter.Normalize();
                    Vector2 tangent = new Vector2(-toCenter.Y, toCenter.X) * 0.5f;
                    Vector2 vel = (toCenter * 7f + tangent * 4f);
                    Dust d = Dust.NewDustPerfect(spawnPos, DustID.Crimson,
                        vel, 200, new Color(255, 70, 130), 1.3f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }

            // Explosión: 40 Crimson radiales
            for (int i = 0; i < 40; i++)
            {
                float angle = (MathHelper.TwoPi / 40) * i;
                Vector2 dir = new Vector2(
                    (float)Math.Cos(angle) * Main.rand.NextFloat(5f, 11f),
                    (float)Math.Sin(angle) * Main.rand.NextFloat(5f, 11f));
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Crimson,
                    dir, 220, new Color(255, 60, 120), 1.5f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Destellos encantados rosas
            for (int i = 0; i < 15; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Pink,
                    new Vector2(Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-4f, 4f)),
                    255, Color.White, 1.0f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
        }

        public override void OnKill(int timeLeft)
        {
            // LA EXPLOSIÓN ES EL ANILLO DE EINSTEIN (única, con el daño
            // COMPLETO): la misma onda de lente gravitacional probada del
            // agujero original (copia exacta).
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center.X, Projectile.Center.Y, 0f, 0f,
                    ModContent.ProjectileType<CosmicShockwaveProjectile>(),
                    Projectile.damage, 0f, Projectile.owner,
                    0f,
                    CosmicShockwaveProjectile.StyleEinstein,
                    520f);
            }

            // Reset del boost de succión de la librería.
            ParticleManager.PullToGlobalBoost = 1f;

            if (Main.netMode == NetmodeID.Server) return;

            // Estruendo de la liberación final
            try
            {
                Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(
                    Projectile.Center, new Vector2(1f, 0f), 6f, 9, 18, 0.4f,
                    "AethonCrimsonBlackHoleFinalBlast"));
            }
            catch { }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item88, Projectile.Center);

            // Presets de la librería — colapso gravitatorio carmesí
            ParticlePresets.Implosion(Projectile.Center, 165f, 46,
                new Color(255, 60, 110), 26);
            ParticlePresets.Explosion(Projectile.Center, 130f, 28,
                new Color(255, 225, 240), new Color(255, 20, 90), 40);

            // Implosión: partículas carmesí convergiendo
            for (int i = 0; i < 60; i++)
            {
                float angle = (MathHelper.TwoPi / 60) * i;
                float dist = Main.rand.NextFloat(100f, 170f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist, (float)Math.Sin(angle) * dist);
                Vector2 toCenter = Projectile.Center - spawnPos;
                if (toCenter.LengthSquared() > 0.01f)
                {
                    toCenter.Normalize();
                    Vector2 tangent = new Vector2(-toCenter.Y, toCenter.X) * 0.7f;
                    Dust d = Dust.NewDustPerfect(spawnPos, DustID.Crimson,
                        toCenter * 9f + tangent * 5f, 220, new Color(255, 80, 140), 1.4f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }

            // Explosión: anillo expansivo carmesí
            for (int i = 0; i < 45; i++)
            {
                float angle = (MathHelper.TwoPi / 45) * i;
                Vector2 dir = new Vector2(
                    (float)Math.Cos(angle) * Main.rand.NextFloat(6f, 13f),
                    (float)Math.Sin(angle) * Main.rand.NextFloat(6f, 13f));
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Crimson,
                    dir, 230, new Color(255, 50, 110), 1.6f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
        }

        // ------------------------------------------------------------------
        //  HELPERS
        // ------------------------------------------------------------------

        /// <summary>Elastic ease-out (curva elástica de aparición — copia exacta).</summary>
        private static float ElasticOut(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            float c = (2f * (float)Math.PI) / 3f;
            return (float)(Math.Pow(2, -10 * t) * Math.Sin((t * 10 - 0.75) * c) + 1);
        }
    }
}
