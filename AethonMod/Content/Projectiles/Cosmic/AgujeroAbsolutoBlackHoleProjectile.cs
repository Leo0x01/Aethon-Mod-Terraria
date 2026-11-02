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
using AethonMod.Content.Buffs;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// AgujeroAbsolutoBlackHoleProjectile — v6.82 — EL AGUJERO ABSOLUTO.
    ///
    /// LA LETRA: «crea un arma nueva de agujero negro runico con lo que
    /// creas que sea bueno, este debe ser mas fuerte que los supremos
    /// asi que tiene que tener 4 anillos runicos de agujero negro».
    ///
    /// EL MÁS ALLÁ DE LOS SUPREMOS: TODO el motor visual del Supremo (la
    /// fusión v6.80 — disco Doppler + anillo de bandas + brazos
    /// espirales + humo + corona de rayos) viste el PERFIL ABSOLUTO —
    /// el gradiente del más allá (violeta del vacío → cian estelar →
    /// oro blanco), rim cian, blanco platino — con LA CUARTA CORONA
    /// RÚNICA: 10 runas blanco-cian a 3,95·R en contrarroto lento, más
    /// afuera que cualquier círculo de cualquier Supremo. El ÚNICO
    /// agujero del mod con CUATRO anillos rúnicos.
    ///
    /// LA FÍSICA (la copia probada de la familia, ESCALA ABSOLUTA):
    /// pop elástico (×1,14 — la esfera más grande vista), atracción en
    /// 680px (la más ancha del mod), ticks del aura MÁS RÁPIDOS que
    /// devoran al 42-76% del daño, devora balas, persecución lenta,
    /// evaporación y un Anillo de Einstein final de 640px.
    ///
    /// PALETA ABSOLUTA de partículas: CIAN ESTELAR (80,220,255) /
    /// VIOLETA DEL VACÍO (140,90,255) / ORO BLANCO (255,240,200) — el
    /// cian domina, el violeta profundiza y el oro blanco corona.
    /// </summary>
    public class AgujeroAbsolutoBlackHoleProjectile : ModProjectile
    {
        /// <summary>Multiplicador del aura sobre la esfera visual (absoluto).</summary>
        private const float ShieldRadiusMult = 5.0f;

        /// <summary>Multiplicador del radio de la lente gravitacional de pantalla.</summary>
        internal const float LensRadiusMult = 4.4f;

        /// <summary>Radio de atracción gravitacional (px) — EL MÁS ANCHO DEL MOD.</summary>
        private const float GravityRadius = 680f;

        /// <summary>v6.82 — LA ESCALA ABSOLUTA: la esfera vive un 14% MÁS
        /// GRANDE que la de los Supremos (la misma física del motor, la
        /// presencia del más allá).</summary>
        private const float EscalaAbsoluta = 1.14f;

        /// <summary>Tiempo visual de vida — usada para el pop elástico de aparición.</summary>
        public ref float VisualsTime => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            // Mismo hitbox que los agujeros hermanos.
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
            // === POP ELÁSTICO DE APARICIÓN (copia de la familia, escala
            //     ABSOLUTA: la esfera más grande del mod en pantalla) ===
            Projectile.scale = EscalaAbsoluta *
                ElasticOut(Utils.GetLerpValue(0f, 120f, VisualsTime, true)) *
                (float)Math.Sqrt(Utils.GetLerpValue(0f, 60f, VisualsTime, true));
            VisualsTime += 1f;

            // ================================================================
            //  SECUENCIA DE MUERTE (copia de la familia: crecimiento → evaporación)
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
                // Piso de escala (lección v6.10).
                Projectile.scale *= Math.Max(1f - collapse, 0.06f);
                expansion = 1f;
            }

            // La materia absorbida ACELERA hacia el centro al evaporarse.
            ParticleManager.PullToGlobalBoost = 1f + expansion * 5f;

            // === MOVIMIENTO: deriva lenta y frenado (copia de la familia) ===
            Projectile.velocity *= 0.97f;

            // === PERSIGUE LIGERAMENTE A LOS ENEMIGOS (copia de la familia) ===
            NPC prey = FindNearestEnemy(900f);
            if (prey != null)
            {
                Vector2 toPrey = prey.Center - Projectile.Center;
                if (toPrey.LengthSquared() > 120f)
                {
                    toPrey.Normalize();
                    Projectile.velocity += toPrey * 0.07f *
                        Content.Systems.WeaponScaling.HomingAcel(
                            Content.Systems.WeaponScaling.NivelDelDuenio(Projectile));
                }
                float spd = Projectile.velocity.Length();
                float topeH = 2.6f * Content.Systems.WeaponScaling.HomingTope(
                    Content.Systems.WeaponScaling.NivelDelDuenio(Projectile));
                if (spd > topeH)
                    Projectile.velocity *= topeH / spd;
            }

            float targetRotation = Projectile.velocity.X * 0.04f;
            Projectile.rotation += MathHelper.WrapAngle(targetRotation - Projectile.rotation) * 0.3f;

            // === PARTÍCULAS (solo cliente) — PALETA ABSOLUTA: cian estelar /
            //     violeta del vacío / oro blanco (el cian DOMINA) ===
            if (Main.netMode != NetmodeID.Server)
            {
                SpawnAbsorbedDusts();
                SpawnCapturedEnergySparks();
                AttractNearbyDust();
                SpawnLibraryAbsorbedMatter();
                SpawnLibraryAccretionDisk();
            }

            // === ATRACCIÓN GRAVITACIONAL DE ENEMIGOS (680px — EL MÁS ANCHO) ===
            float gravityRadius = GravityRadius * (1f + expansion * 0.6f);
            const float gravityStrength = 3.0f;
            // v6.50.3 — la ley de la familia: el empuje de npc.velocity corre
            // SOLO en la autoridad (SP + server) — sin doble empuje en clientes.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!VFXCore.EsObjetivo(npc)) continue;
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
            }

            // === LA GRAVEDAD DOBLA Y DEVORA LOS PROYECTILES ENEMIGOS (copia) ===
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
                                200, new Color(160, 230, 255), 0.7f);
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

            // === AURA DE DAÑO: TICKS ABSOLUTOS — MÁS RÁPIDOS Y MÁS FUERTES
            //     que los Supremos (el vacío devora con hambre propia) ===
            if (VisualsTime > 0f)
            {
                float lifeProgress = MathHelper.Clamp(1f - Projectile.timeLeft / 600f, 0f, 1f);
                float auraRadius = ShieldRadius * (1.15f + 0.75f * lifeProgress);
                int auraDamage = Math.Max(1, (int)(Projectile.damage * (0.42f + 0.34f * lifeProgress)));
                int t = (int)VisualsTime;
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!VFXCore.EsObjetivo(npc)) continue;
                    float dist = (npc.Center - Projectile.Center).Length();
                    if (dist > auraRadius) continue;

                    float prox = MathHelper.Clamp(dist / auraRadius, 0f, 1f);
                    int interval = 4 + (int)(prox * 14f); // 4 (centro) → 18 (borde) — MÁS RÁPIDO
                    if ((t + npc.whoAmI) % interval != 0) continue;

                    Content.Systems.GolpeMotor.Golpear(Projectile, npc, auraDamage, 0f, true);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 toCenter = Projectile.Center - npc.Center;
                        if (toCenter.LengthSquared() > 0.01f)
                        {
                            toCenter.Normalize();
                            npc.velocity += toCenter * 0.8f;
                        }
                        try { npc.AddBuff(ModContent.BuffType<Content.Buffs.QuemaduraCosmica>(), 480); } catch { }
                    }
                }
            }

            // === ILUMINACIÓN PULSANTE — TRES puntos para el vórtice ===
            // CIAN ESTELAR con pulso vivo (la estirpe absoluta: la banda
            // arde cian, los rayos centellean violeta, la corona dora).
            float pulse = 0.8f + 0.2f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5f);
            Vector3 light = new Vector3(0.55f * pulse, 0.85f * pulse, 1.0f * pulse);
            Lighting.AddLight(Projectile.Center, light);
            float le = AgujeroAbsolutoBlackHoleRenderer.SpherePx * Math.Max(Projectile.scale, 0.1f);
            Lighting.AddLight(Projectile.Center + new Vector2(0f, -le * 1.5f), light * 0.55f);
            Lighting.AddLight(Projectile.Center + new Vector2(0f, le * 1.7f),
                new Vector3(0.60f * pulse, 0.40f * pulse, 1.0f * pulse) * 0.75f);
        }

        /// <summary>El enemigo chaseable más cercano dentro de maxDist (copia de la familia).</summary>
        private NPC FindNearestEnemy(float maxDist)
        {
            NPC best = null;
            float bestDist = maxDist * maxDist;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;
                float d2 = (npc.Center - Projectile.Center).LengthSquared();
                if (d2 < bestDist)
                {
                    bestDist = d2;
                    best = npc;
                }
            }
            return best;
        }

        /// <summary>Radio actual del campo (px) — abraza la mitad interior del anillo.</summary>
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
        //  PARTÍCULAS — PALETA ABSOLUTA: cian estelar / violeta del vacío / oro blanco
        // ------------------------------------------------------------------

        /// <summary>Materia devorada (dusts) cayendo en espiral desde el anillo.</summary>
        private void SpawnAbsorbedDusts()
        {
            float deathSpeedBoost = 1f;
            if (Projectile.timeLeft <= 90f)
                deathSpeedBoost = 1f + (90f - Projectile.timeLeft) / 90f * 2f;

            float shadow = AgujeroAbsolutoBlackHoleRenderer.SpherePx * Math.Max(Projectile.scale, 0.1f);

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
                        color = new Color(235, 250, 255); // blanco-cian al borde
                    }
                    else
                    {
                        color = Main.rand.Next(3) switch
                        {
                            0 => new Color(80, 220, 255),   // CIAN ESTELAR
                            1 => new Color(140, 90, 255),   // VIOLETA DEL VACÍO
                            _ => new Color(255, 240, 200),  // ORO BLANCO
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

        /// <summary>Chispas cian/violetas capturadas por el campo.</summary>
        private void SpawnCapturedEnergySparks()
        {
            if (Main.rand.NextBool(12))
            {
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(300f, 440f) * MathHelper.Max(Projectile.scale, 0.5f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist,
                    (float)Math.Sin(angle) * dist);
                Vector2 vel = (Projectile.Center - spawnPos) * 0.03f;
                Dust d = Dust.NewDustPerfect(spawnPos, DustID.Enchanted_Pink,
                    vel, 255, new Color(150, 230, 255), 0.9f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        /// <summary>Devora el polvo del ambiente en espiral.</summary>
        private void AttractNearbyDust()
        {
            float radius = 500f;
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

        /// <summary>Materia devorada: estelas TrailGlow cayendo en espiral.</summary>
        private void SpawnLibraryAbsorbedMatter()
        {
            for (int i = 0; i < 2; i++)
            {
                float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(2.4f, 4.2f) *
                             AgujeroAbsolutoBlackHoleRenderer.SpherePx * MathHelper.Max(Projectile.scale, 0.4f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist,
                    (float)Math.Sin(angle) * dist);

                Vector2 inward = new Vector2(-(float)Math.Cos(angle), -(float)Math.Sin(angle));
                Vector2 tangent = new Vector2(-(float)Math.Sin(angle), (float)Math.Cos(angle));
                Vector2 velocity = inward * Main.rand.NextFloat(1.8f, 2.8f) +
                                   tangent * Main.rand.NextFloat(0.25f, 0.5f);

                Color start = new Color(80, 220, 255, 190);    // cian estelar
                Color end = new Color(248, 250, 255, 235);     // blanco platino

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

        /// <summary>Estelas orbitando en la banda del anillo de bandas
        /// (2.3..5.8× la esfera, elipse 1.24 del anillo del motor).</summary>
        private void SpawnLibraryAccretionDisk()
        {
            if (Main.rand.NextBool(4))
            {
                float shadow = AgujeroAbsolutoBlackHoleRenderer.SpherePx *
                               MathHelper.Max(Projectile.scale, 0.4f);
                float radius = Main.rand.NextFloat(2.3f, 5.8f) * shadow;
                float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                // Kepler: ω ∝ r^(−3/2) — el interior gira más rápido.
                float angVel = 0.16f * (float)Math.Pow(4.8f * shadow / radius, 1.5f);

                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * radius,
                    (float)Math.Sin(angle) * radius * 1.24f); // elipse del anillo

                // El color nace del lado DOPPLER: mitad CIAN (acerca),
                // mitad VIOLETA (aleja) — el más allá del Umbral.
                bool hotSide = Math.Cos(angle) < 0f;
                Color c = hotSide ? new Color(80, 220, 255, 200) : new Color(140, 90, 255, 200);

                var p = new ParticleData
                {
                    Position = spawnPos,
                    Velocity = Vector2.Zero,
                    Scale = new Vector2(2.2f, 0.55f),
                    Rotation = angle + MathHelper.PiOver2,
                    RotationSpeed = angVel,
                    PackedColor = ParticleManager.PackColor(c),
                    PackedStartColor = ParticleManager.PackColor(new Color(235, 250, 255, 200)),
                    PackedEndColor = ParticleManager.PackColor(new Color(50, 30, 110, 40)),
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
        //  RENDER — 100% CÓDIGO (el motor del Supremo, PERFIL ABSOLUTO)
        // ------------------------------------------------------------------

        public override bool PreDraw(ref Color lightColor)
        {
            // La lente va DETRÁS: el BlackHoleLensSystem pinta el núcleo
            // ENCIMA de la distorsión llamando a DrawCoreVisuals.
            if (BlackHoleLensSystem.LensActive)
                return false;

            // CONTRATO DE BATCH A PRUEBA DE BALAS (v6.10) + la sonda de la
            // v6.50.11 (sin first-chance) y la curación del lote vivo.
            VFXCore.CerrarLoteSiAbierto();

            try
            {
                DrawCoreVisuals(Projectile);
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }

            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        /// <summary>
        /// Dibuja el Agujero Absoluto completo (motor del Supremo, perfil
        /// Absoluto — con la CUARTA CORONA). Compartido entre el pase del
        /// mundo (PreDraw) y el pase posterior a la lente.
        /// CONTRATO: el SpriteBatch llega CERRADO y queda CERRADO.
        /// </summary>
        internal static void DrawCoreVisuals(Projectile p)
        {
            Vector2 drawPos = p.Center - Main.screenPosition;
            float time = Main.GlobalTimeWrappedHourly;
            int seed = p.whoAmI * 17 + 5;

            AgujeroAbsolutoBlackHoleRenderer.Draw(drawPos, p.scale, time, seed);
        }

        // ------------------------------------------------------------------
        //  IMPACTO Y MUERTE (física de la familia, PALETA ABSOLUTA)
        // ------------------------------------------------------------------

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode == NetmodeID.Server) return;

            // Micro-colapso sobre el objetivo
            ParticlePresets.Implosion(target.Center, 70f, 16, new Color(80, 220, 255), 18);
            ParticlePresets.RingPulse(target.Center, 90f, new Color(190, 240, 255, 170), 22);

            // Implosión: 50 partículas convergiendo en espiral
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
                        vel, 200, new Color(80, 220, 255), 1.3f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }

            // Explosión: 40 radiales
            for (int i = 0; i < 40; i++)
            {
                float angle = (MathHelper.TwoPi / 40) * i;
                Vector2 dir = new Vector2(
                    (float)Math.Cos(angle) * Main.rand.NextFloat(5f, 11f),
                    (float)Math.Sin(angle) * Main.rand.NextFloat(5f, 11f));
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Crimson,
                    dir, 220, new Color(140, 90, 255), 1.5f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Destellos de la corona: cian y blanco platino
            for (int i = 0; i < 15; i++)
            {
                Color c = Main.rand.NextBool(3)
                    ? new Color(248, 250, 255)   // blanco platino
                    : new Color(150, 235, 255);  // cian claro
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
            // LA EXPLOSIÓN ES EL ANILLO DE EINSTEIN (única, daño COMPLETO) —
            // el más ancho de la familia: 640px.
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center.X, Projectile.Center.Y, 0f, 0f,
                    ModContent.ProjectileType<CosmicShockwaveProjectile>(),
                    Projectile.damage, 0f, Projectile.owner,
                    0f,
                    CosmicShockwaveProjectile.StyleEinstein,
                    640f);
            }

            ParticleManager.PullToGlobalBoost = 1f;

            if (Main.netMode == NetmodeID.Server) return;

            try
            {
                Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(
                    Projectile.Center, new Vector2(1f, 0f), 8f, 10, 22, 0.45f,
                    "AethonAbsolutoBlackHoleFinalBlast"));
            }
            catch { }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item88, Projectile.Center);

            // Presets — el COLAPSO ABSOLUTO (cian + violeta + blanco platino)
            ParticlePresets.Implosion(Projectile.Center, 200f, 56,
                new Color(80, 220, 255), 30);
            ParticlePresets.Explosion(Projectile.Center, 160f, 34,
                new Color(248, 250, 255), new Color(140, 90, 255), 48);

            // Implosión: partículas convergiendo
            for (int i = 0; i < 60; i++)
            {
                float angle = (MathHelper.TwoPi / 60) * i;
                float dist = Main.rand.NextFloat(110f, 200f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist, (float)Math.Sin(angle) * dist);
                Vector2 toCenter = Projectile.Center - spawnPos;
                if (toCenter.LengthSquared() > 0.01f)
                {
                    toCenter.Normalize();
                    Vector2 tangent = new Vector2(-toCenter.Y, toCenter.X) * 0.7f;
                    Dust d = Dust.NewDustPerfect(spawnPos, DustID.Crimson,
                        toCenter * 9f + tangent * 5f, 220, new Color(80, 220, 255), 1.4f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }

            // Explosión: anillo expansivo (cian + violeta + oro blanco)
            for (int i = 0; i < 45; i++)
            {
                float angle = (MathHelper.TwoPi / 45) * i;
                Vector2 dir = new Vector2(
                    (float)Math.Cos(angle) * Main.rand.NextFloat(6f, 13f),
                    (float)Math.Sin(angle) * Main.rand.NextFloat(6f, 13f));
                Color c = i % 3 == 0 ? new Color(80, 220, 255)
                        : i % 3 == 1 ? new Color(140, 90, 255)
                        : new Color(255, 240, 200);
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

        /// <summary>Elastic ease-out (curva elástica de aparición — copia de la familia).</summary>
        private static float ElasticOut(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            float c = (2f * (float)Math.PI) / 3f;
            return (float)(Math.Pow(2, -10 * t) * Math.Sin((t * 10 - 0.75) * c) + 1);
        }
    }
}
