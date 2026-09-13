using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using AethonMod.Content.Particles;
using AethonMod.Content.Effects;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// FusionBlackHoleProjectile — v6.14 — EL AGUJERO NEGRO DE FUSIÓN.
    ///
    /// PETICIÓN DEL USUARIO: "crea un tercero que sea la fusión del agujero
    /// negro del vacío con el agujero negro base".
    ///
    /// LA FUSIÓN LITERAL — ambos renderizadores originales componen en el
    /// MISMO centro, cada uno con su identidad intacta:
    ///
    ///   · DETRÁS: el agujero BASE (BlackHoleProjectile.DrawCoreVisuals) —
    ///     el Gargantua de marcha de luz con su RealBlackHoleShader de 75
    ///     pasos, su disco de acreción naranja lensado y su halo ámbar.
    ///   · DELANTE: el agujero DEL VACÍO (CrimsonBlackHoleRenderer, v6.13
    ///     con sus SIETE capas de personalidad) a 0.68× de escala — su
    ///     esfera negra se alinea con el horizonte del Gargantua y sus
    ///     hojas de plasma carmesí barren por encima.
    ///
    /// El resultado: una esfera de negro profundo abrazada por el ANILLO
    /// NARANJA lensado del Gargantua, envuelta en el VÓRTICE OBLIVION del
    /// vacío (ondas de espacio-tiempo, pulsos de fotones, chorros
    /// relativistas, corrientes de materia, llamaradas, arcos de Einstein y
    /// rim violeta) — FUEGO y VACÍO en un solo cuerpo.
    ///
    /// La FÍSICA de juego es la MISMA copia probada del carmesí (que a su
    /// vez es copia del base). Ambos agujeros fuente quedan INTACTOS.
    /// </summary>
    public class FusionBlackHoleProjectile : ModProjectile
    {
        /// <summary>Multiplicador del aura sobre la sombra visual.</summary>
        private const float ShieldRadiusMult = 4.6f;

        /// <summary>Multiplicador del radio de la lente gravitacional de
        /// pantalla — la FUSIÓN distorsiona más lejos que cualquiera de sus
        /// padres (la suma de ambas masas).</summary>
        internal const float LensRadiusMult = 3.2f;

        /// <summary>Escala del vórtice del vacío sobre el Gargantua base.</summary>
        private const float VoidScale = 0.68f;

        /// <summary>Tiempo visual de vida — usada para el pop elástico de aparición.</summary>
        public ref float VisualsTime => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            // width=96 como los padres: el canvas del Gargantua base escala
            // con width (zoomBase = width/256×2) — misma geometría que el
            // original.
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
                Projectile.scale *= Math.Max(1f - collapse, 0.06f);
                expansion = 1f;
            }

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

            // === PARTÍCULAS (solo cliente) — paleta FUSIÓN: carmesí del
            //     vacío + naranja del Gargantua, entremezcladas ===
            if (Main.netMode != NetmodeID.Server)
            {
                SpawnAbsorbedDusts();
                SpawnCapturedEnergySparks();
                AttractNearbyDust();
                SpawnLibraryAbsorbedMatter();
                SpawnLibraryAccretionDisk();
            }

            // === ATRACCIÓN GRAVITACIONAL DE ENEMIGOS (copia exacta) ===
            float gravityRadius = 480f * (1f + expansion * 0.6f);
            const float gravityStrength = 2.8f;
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
                            // la materia devorada destella en AMBAS familias
                            Color c = Main.rand.NextBool() ? new Color(255, 130, 200) : new Color(255, 190, 90);
                            Dust d2 = Dust.NewDustPerfect(pr.Center, DustID.PinkCrystalShard,
                                (pr.Center - Projectile.Center) * -0.02f +
                                new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), Main.rand.NextFloat(-1.5f, 1.5f)),
                                200, c, 0.7f);
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
                    int interval = 6 + (int)(prox * 18f);
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

            // === ILUMINACIÓN PULSANTE — la DOBLE estirpe: carmesí + ámbar ===
            float pulse = 0.8f + 0.2f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5f);
            Vector3 light = new Vector3(1.25f * pulse, 0.40f * pulse, 0.42f * pulse);
            Lighting.AddLight(Projectile.Center, light);
            float le = CrimsonBlackHoleRenderer.SpherePx * VoidScale * Math.Max(Projectile.scale, 0.1f);
            Lighting.AddLight(Projectile.Center + new Vector2(0f, -le * 1.5f), light * 0.55f);
            Lighting.AddLight(Projectile.Center + new Vector2(0f, le * 1.7f), light * 0.75f);
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

        /// <summary>Radio actual del campo (px).</summary>
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
        //  PARTÍCULAS — paleta FUSIÓN (carmesí + naranja entremezcladas)
        // ------------------------------------------------------------------

        /// <summary>Materia absorbida: media del vacío, media del Gargantua.</summary>
        private void SpawnAbsorbedDusts()
        {
            float deathSpeedBoost = 1f;
            if (Projectile.timeLeft <= 90f)
                deathSpeedBoost = 1f + (90f - Projectile.timeLeft) / 90f * 2f;

            float shadow = CrimsonBlackHoleRenderer.SpherePx * VoidScale *
                           Math.Max(Projectile.scale, 0.1f);

            for (int i = 0; i < 2; i++)
            {
                float angle = Projectile.rotation * 1.5f + i * (MathHelper.TwoPi / 2f) +
                              Main.rand.NextFloat(-0.25f, 0.25f);
                float dist = 2.6f * shadow + 1.4f * shadow * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2f + i);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist,
                    (float)Math.Sin(angle) * dist);

                Vector2 toCenter = Projectile.Center - spawnPos;
                float speed = (6f + 6f * (1f - dist / (4f * shadow))) * deathSpeedBoost;
                if (toCenter.LengthSquared() > 0.01f)
                {
                    toCenter.Normalize();
                    Vector2 velocity = toCenter * speed;

                    float angleToCenter = (float)Math.Atan2(toCenter.Y, toCenter.X);
                    velocity = velocity.RotateTowards(angleToCenter + MathHelper.PiOver2 * 0.3f, 0.5f);

                    Color color;
                    if (dist < 1.6f * shadow)
                    {
                        color = new Color(255, 240, 220); // blanco cálido al borde
                    }
                    else
                    {
                        color = Main.rand.Next(4) switch
                        {
                            0 => new Color(238, 40, 90),    // carmesí del vacío
                            1 => new Color(255, 70, 150),    // fucsia del vacío
                            2 => new Color(255, 160, 60),    // naranja del Gargantua
                            _ => new Color(200, 90, 30),     // brasa del Gargantua
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

        /// <summary>Chispas capturadas: rosas y doradas entremezcladas.</summary>
        private void SpawnCapturedEnergySparks()
        {
            if (Main.rand.NextBool(12))
            {
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(260f, 380f) * MathHelper.Max(Projectile.scale, 0.5f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist,
                    (float)Math.Sin(angle) * dist);
                Vector2 vel = (Projectile.Center - spawnPos) * 0.03f;
                Color c = Main.rand.NextBool() ? new Color(255, 130, 190) : new Color(255, 200, 120);
                Dust d = Dust.NewDustPerfect(spawnPos, DustID.Enchanted_Pink,
                    vel, 255, c, 0.9f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        /// <summary>Devora el polvo del ambiente en espiral.</summary>
        private void AttractNearbyDust()
        {
            float radius = 440f;
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

        /// <summary>Materia absorbida: estelas TrailGlow de AMBAS estirpes.</summary>
        private void SpawnLibraryAbsorbedMatter()
        {
            for (int i = 0; i < 2; i++)
            {
                float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(2.4f, 4.2f) *
                             CrimsonBlackHoleRenderer.SpherePx * VoidScale *
                             MathHelper.Max(Projectile.scale, 0.4f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist,
                    (float)Math.Sin(angle) * dist);

                Vector2 inward = new Vector2(-(float)Math.Cos(angle), -(float)Math.Sin(angle));
                Vector2 tangent = new Vector2(-(float)Math.Sin(angle), (float)Math.Cos(angle));
                Vector2 velocity = inward * Main.rand.NextFloat(1.8f, 2.8f) +
                                   tangent * Main.rand.NextFloat(0.25f, 0.5f);

                Color start = Main.rand.NextBool()
                    ? new Color(255, 60, 130, 190)    // vacío
                    : new Color(255, 150, 70, 190);   // Gargantua
                Color end = new Color(255, 244, 225, 235);

                var p = new ParticleData
                {
                    Position = spawnPos,
                    Velocity = velocity,
                    Scale = new Vector2(1.6f, 0.5f),
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

        /// <summary>Estelas orbitando en la banda del vórtice del vacío.</summary>
        private void SpawnLibraryAccretionDisk()
        {
            if (Main.rand.NextBool(4))
            {
                float shadow = CrimsonBlackHoleRenderer.SpherePx * VoidScale *
                               MathHelper.Max(Projectile.scale, 0.4f);
                float radius = Main.rand.NextFloat(2.3f, 5.8f) * shadow;
                float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                float angVel = 0.16f * (float)Math.Pow(4.6f * shadow / radius, 1.5f);

                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * radius,
                    (float)Math.Sin(angle) * radius * 0.345f);

                Color c = Main.rand.NextBool()
                    ? new Color(255, 70, 140, 200)     // vacío
                    : new Color(255, 170, 80, 200);    // Gargantua

                var p = new ParticleData
                {
                    Position = spawnPos,
                    Velocity = Vector2.Zero,
                    Scale = new Vector2(2.2f, 0.55f),
                    Rotation = angle + MathHelper.PiOver2,
                    RotationSpeed = angVel,
                    PackedColor = ParticleManager.PackColor(c),
                    PackedStartColor = ParticleManager.PackColor(c),
                    PackedEndColor = ParticleManager.PackColor(new Color(90, 20, 20, 40)),
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
        //  RENDER — LA FUSIÓN LITERAL DE AMBOS AGUJEROS
        // ------------------------------------------------------------------

        public override bool PreDraw(ref Color lightColor)
        {
            // La lente va DETRÁS: el BlackHoleLensSystem pinta el núcleo
            // ENCIMA de la distorsión llamando a DrawCoreVisuals.
            if (BlackHoleLensSystem.LensActive)
                return false;

            // CONTRATO DE BATCH A PRUEBA DE BALAS (v6.10).
            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try
            {
                DrawCoreVisuals(Projectile);
            }
            catch { try { Main.spriteBatch.End(); } catch { } }

            if (wasActive)
                RestoreSpriteBatch();
            return false;
        }

        /// <summary>
        /// LA FUSIÓN: primero el agujero BASE (Gargantua de marcha de luz con
        /// su RealBlackHoleShader y su disco naranja lensado), y ENCIMA el
        /// agujero DEL VACÍO (vórtice Oblivion + sus 7 capas de personalidad
        /// v6.13) a 0.68× — la esfera del vacío se alinea con el horizonte
        /// del Gargantua y su anillo naranja asoma alrededor.
        /// CONTRATO: el SpriteBatch llega CERRADO y queda CERRADO.
        /// </summary>
        internal static void DrawCoreVisuals(Projectile p)
        {
            // === 1. EL AGUJERO BASE (detrás) ===
            // endActiveBatch=false: el batch llega cerrado y lo deja cerrado.
            BlackHoleProjectile.DrawCoreVisuals(p, false);

            // === 2. EL AGUJERO DEL VACÍO (delante, 0.68×) ===
            Vector2 drawPos = p.Center - Main.screenPosition;
            float time = Main.GlobalTimeWrappedHourly;
            int seed = p.whoAmI * 11 + 5;
            CrimsonBlackHoleRenderer.Draw(drawPos, p.scale * VoidScale, time, seed);
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
        //  IMPACTO Y MUERTE (física exacta, paleta doble)
        // ------------------------------------------------------------------

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode == NetmodeID.Server) return;

            // Micro-colapso Doble sobre el objetivo
            ParticlePresets.Implosion(target.Center, 70f, 16, new Color(255, 40, 100), 18);
            ParticlePresets.Implosion(target.Center, 70f, 12, new Color(255, 170, 70), 14);
            ParticlePresets.RingPulse(target.Center, 90f, new Color(255, 170, 190, 170), 22);

            // Implosión: partículas de ambas estirpes convergiendo en espiral
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
                    Color c = Main.rand.NextBool() ? new Color(255, 70, 130) : new Color(255, 170, 80);
                    Dust d = Dust.NewDustPerfect(spawnPos, DustID.Crimson,
                        vel, 200, c, 1.3f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }

            // Explosión: 40 radiales doble familia
            for (int i = 0; i < 40; i++)
            {
                float angle = (MathHelper.TwoPi / 40) * i;
                Vector2 dir = new Vector2(
                    (float)Math.Cos(angle) * Main.rand.NextFloat(5f, 11f),
                    (float)Math.Sin(angle) * Main.rand.NextFloat(5f, 11f));
                Color c = Main.rand.NextBool() ? new Color(255, 60, 120) : new Color(255, 180, 80);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Crimson,
                    dir, 220, c, 1.5f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Destellos encantados: rosas y dorados
            for (int i = 0; i < 15; i++)
            {
                Color c = Main.rand.NextBool() ? Color.White : new Color(255, 230, 180);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Pink,
                    new Vector2(Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-4f, 4f)),
                    255, c, 1.0f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
        }

        public override void OnKill(int timeLeft)
        {
            // LA EXPLOSIÓN ES EL ANILLO DE EINSTEIN (única, daño COMPLETO).
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center.X, Projectile.Center.Y, 0f, 0f,
                    ModContent.ProjectileType<CosmicShockwaveProjectile>(),
                    Projectile.damage, 0f, Projectile.owner,
                    0f,
                    CosmicShockwaveProjectile.StyleEinstein,
                    540f);
            }

            ParticleManager.PullToGlobalBoost = 1f;

            if (Main.netMode == NetmodeID.Server) return;

            try
            {
                Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(
                    Projectile.Center, new Vector2(1f, 0f), 8f, 10, 22, 0.5f,
                    "AethonFusionBlackHoleFinalBlast"));
            }
            catch { }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item88, Projectile.Center);

            // Presets — colapso gravitatorio DOBLE (carmesí + ámbar)
            ParticlePresets.Implosion(Projectile.Center, 175f, 48,
                new Color(255, 60, 110), 26);
            ParticlePresets.Implosion(Projectile.Center, 175f, 38,
                new Color(255, 170, 70), 22);
            ParticlePresets.Explosion(Projectile.Center, 140f, 32,
                new Color(255, 235, 240), new Color(255, 60, 70), 44);

            // Implosión: partículas convergiendo
            for (int i = 0; i < 60; i++)
            {
                float angle = (MathHelper.TwoPi / 60) * i;
                float dist = Main.rand.NextFloat(105f, 175f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist, (float)Math.Sin(angle) * dist);
                Vector2 toCenter = Projectile.Center - spawnPos;
                if (toCenter.LengthSquared() > 0.01f)
                {
                    toCenter.Normalize();
                    Vector2 tangent = new Vector2(-toCenter.Y, toCenter.X) * 0.7f;
                    Color c = Main.rand.NextBool() ? new Color(255, 80, 140) : new Color(255, 180, 90);
                    Dust d = Dust.NewDustPerfect(spawnPos, DustID.Crimson,
                        toCenter * 9f + tangent * 5f, 220, c, 1.4f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }

            // Explosión: anillo expansivo doble
            for (int i = 0; i < 45; i++)
            {
                float angle = (MathHelper.TwoPi / 45) * i;
                Vector2 dir = new Vector2(
                    (float)Math.Cos(angle) * Main.rand.NextFloat(6f, 13f),
                    (float)Math.Sin(angle) * Main.rand.NextFloat(6f, 13f));
                Color c = Main.rand.NextBool() ? new Color(255, 50, 110) : new Color(255, 190, 90);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Crimson,
                    dir, 230, c, 1.6f);
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
