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
    /// EclipsePrimordialProjectile — v6.28 — EL ECLIPSE TOTAL (el rediseño).
    ///
    /// v6.28 — LA ORDEN DEL USUARIO: "rediseña el bastón de eclipse
    /// primordial". La versión v6.26 (el Sol de los 20 Anillos + la mezcla
    /// de TODOS los agujeros) se retira. EL NUEVO CONCEPTO es el nombre del
    /// arma: UN ECLIPSE SOLAR TOTAL — el disco de la noche se DESLIZA sobre
    /// el sol primordial (EL CRECIENTE menguante), el día MUERE (el mundo
    /// se apaga con RiftLib.Oscurecer — el sesgo violeta de la casa), la
    /// CORONA blanca streamerea desde detrás, la CROMOSFERA roja arde al
    /// limbo con sus perlas de Baily, LAS PROMINENCIAS lamen desde la cara
    /// oculta y EL ANILLO DE DIAMANTE viaja por el borde — acelerando en
    /// LA ÚLTIMA LUZ. LOS TRES CÍRCULOS RÚNICOS (blanco/dorado/violeta)
    /// contienen la noche: la firma de la casa.
    ///
    /// LA FÍSICA se conserva (la good): atracción gravitacional de 600px
    /// + devorar proyectiles enemigos + el aura de quemadura de la corona
    /// (los enemigos caen al eclipse y arden). Vida ~14 s.
    ///
    /// LA MUERTE es EL RETORNO DE LA LUZ: el disco IMPLODE (la noche se
    /// traga a sí misma), la corona EXPLOTA soplada, el núcleo queda
    /// CEGADOR — y el Anillo de Einstein + la nova ×1.6 estallan mientras
    /// EL DÍA VUELVE (la oscuridad se suelta).
    /// </summary>
    public class EclipsePrimordialProjectile : ModProjectile
    {
        /// <summary>Vida total en ticks (~14 s — el coloso dura).</summary>
        private const int LifeTicks = 840;

        /// <summary>Multiplicador del aura sobre el cuerpo del sol.</summary>
        private const float ShieldRadiusMult = 4.8f;

        /// <summary>Multiplicador del radio de la lente gravitacional de pantalla.</summary>
        internal const float LensRadiusMult = 2.6f;

        /// <summary>Radio de atracción gravitacional (px) — la herencia de los agujeros.</summary>
        private const float GravityRadius = 600f;

        /// <summary>Ticks de LA ÚLTIMA LUZ + EL RETORNO (el colapso final).</summary>
        private const int NovaTicks = 36;

        /// <summary>Ticks del DESLIZAMIENTO del disco (la fase del creciente).</summary>
        private const int FormTicks = 45;

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
            Projectile.timeLeft = LifeTicks;
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

            // === LA SECUENCIA DE MUERTE: SOLO EL COLAPSO (la noche se
            //     traga a sí misma — el renderer pinta el resto) ===
            float novaT = 0f;
            if (Projectile.timeLeft <= NovaTicks)
            {
                if (Projectile.localAI[1] <= 0f)
                    Projectile.localAI[1] = 0.3f * Projectile.width *
                                            Math.Max(Projectile.scale, 0.08f) * ShieldRadiusMult;

                novaT = Utils.GetLerpValue(NovaTicks, 0f, Projectile.timeLeft, true);
                Projectile.scale *= Math.Max(1f - novaT * 0.7f, 0.10f);
            }

            float expansion = novaT;
            ParticleManager.PullToGlobalBoost = 1f + expansion * 5f;

            // === LA NOVA / EL RETORNO DE LA LUZ: se dispara al empezar el colapso ===
            if (Projectile.timeLeft <= NovaTicks)
                TriggerNovaDelEclipse();

            // === EL DÍA MUERE CON EL ECLIPSE (v6.28 — el mundo se apaga
            //     mientras el disco cubre al sol; VUELVE con la nova) ===
            if (Main.netMode != NetmodeID.Server)
            {
                float formT = MathHelper.Clamp(VisualsTime / FormTicks, 0f, 1f);
                if (novaT > 0f)
                {
                    // EL RETORNO: la oscuridad se suelta con la nova.
                    RiftLib.Oscurecer(0.32f * (1f - novaT));
                }
                else
                {
                    // EL TRÁNSITO apaga el mundo gradualmente (el creciente
                    // mengua → el total → la ÚLTIMA LUZ al máximo).
                    float ultima = LifeTicks - Projectile.timeLeft > LifeTicks * 0.88f ? 0.06f : 0f;
                    RiftLib.Oscurecer(0.26f * formT + ultima);
                }
            }

            // === MOVIMIENTO: deriva lenta y frenado (copia exacta) ===
            Projectile.velocity *= 0.97f;

            // === PERSIGUE LIGERAMENTE A LOS ENEMIGOS (copia exacta) ===
            NPC prey = FindNearestEnemy(780f);
            if (prey != null)
            {
                Vector2 toPrey = prey.Center - Projectile.Center;
                if (toPrey.LengthSquared() > 120f)
                {
                    toPrey.Normalize();
                    Projectile.velocity += toPrey * 0.07f;
                }
                float spd = Projectile.velocity.Length();
                if (spd > 2.6f)
                    Projectile.velocity *= 2.6f / spd;
            }

            float targetRotation = Projectile.velocity.X * 0.04f;
            Projectile.rotation += MathHelper.WrapAngle(targetRotation - Projectile.rotation) * 0.3f;

            // === PARTÍCULAS (solo cliente) — el oro del sol + la aurora ===
            if (Main.netMode != NetmodeID.Server)
            {
                SpawnFallingMatter();
                SpawnCapturedEnergySparks();
                AttractNearbyDust();
                SpawnLibraryFallingMatter();
                SpawnLibraryAccretionBand();
            }

            // === ATRACCIÓN GRAVITACIONAL DE ENEMIGOS (600px — hacia el sol) ===
            float gravityRadius = GravityRadius * (1f + expansion * 0.6f);
            const float gravityStrength = 2.6f;
            // v6.50.3 — FIX (la promesa del SunProjectile v6.50.2 estaba rota:
            // "la familia de agujeros gatea la misma succión así" — solo el SOL
            // la gateaba): el empuje de npc.velocity corre SOLO en la autoridad
            // (SP + server); en un cliente remoto el server corrige la posición
            // por netUpdate y no hay doble empuje divergente (el jitter de
            // réplicas que v6.50.2 curó en el sol, ahora en toda la familia).
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

            // === LA GRAVEDAD DOBLA Y DEVORA LOS PROYECTILES ENEMIGOS ===
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
                                200, new Color(185, 150, 255), 0.7f);
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

            // === AURA DE DAÑO DEL SOL: TICKS QUE ACELERAN CERCA DEL CENTRO ===
            // v6.50 — el daño del aura va por GolpeMotor (el cauce del
            // motor); el exprimido y la quemadura siguen en el servidor.
            if (VisualsTime > 0f)
            {
                float lifeProgress = MathHelper.Clamp(1f - Projectile.timeLeft / (float)LifeTicks, 0f, 1f);
                float auraRadius = ShieldRadius * (1.15f + 0.75f * lifeProgress);
                int auraDamage = Math.Max(1, (int)(Projectile.damage * (0.35f + 0.30f * lifeProgress)));
                int t = (int)VisualsTime;
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!VFXCore.EsObjetivo(npc)) continue;
                    float dist = (npc.Center - Projectile.Center).Length();
                    if (dist > auraRadius) continue;

                    float prox = MathHelper.Clamp(dist / auraRadius, 0f, 1f);
                    int interval = 6 + (int)(prox * 18f);
                    if ((t + npc.whoAmI) % interval != 0) continue;

                    // Lógica de servidor: el exprimido hacia el centro + quemadura.
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 toCenter = Projectile.Center - npc.Center;
                        if (toCenter.LengthSquared() > 0.01f)
                        {
                            toCenter.Normalize();
                            npc.velocity += toCenter * 0.8f;
                        }
                        try { npc.AddBuff(ModContent.BuffType<Content.Buffs.QuemaduraCosmica>(), 300); } catch { }
                    }
                    // v6.50 — GolpeMotor (el cauce del motor: crítica real,
                    // varianza, on-hit y sync MP del propio motor).
                    Content.Systems.GolpeMotor.Golpear(Projectile, npc, auraDamage, 0f, true);
                }
            }

            // === ILUMINACIÓN PULSANTE — TRES puntos (oro solar + violeta) ===
            float pulse = 0.8f + 0.2f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5f);
            Vector3 sunLight = new Vector3(1.00f * pulse, 0.82f * pulse, 0.55f * pulse);
            Lighting.AddLight(Projectile.Center, sunLight);
            float le = EclipsePrimordialRenderer.SunPx * Math.Max(Projectile.scale, 0.1f);
            Lighting.AddLight(Projectile.Center + new Vector2(0f, -le * 1.5f), sunLight * 0.55f);
            Lighting.AddLight(Projectile.Center + new Vector2(0f, le * 1.7f),
                new Vector3(0.45f * pulse, 0.25f * pulse, 0.75f * pulse) * 0.75f);
        }

        /// <summary>El enemigo chaseable más cercano dentro de maxDist (copia exacta).</summary>
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

        /// <summary>Radio actual del campo (px) — abraza el cuerpo del sol.</summary>
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
        //  PARTÍCULAS — el ORO DEL SOL + el GRADIENTE AURORA
        // ------------------------------------------------------------------

        /// <summary>Materia cayendo al sol (dusts) desde el borde de los anillos.</summary>
        private void SpawnFallingMatter()
        {
            float deathSpeedBoost = 1f;
            if (Projectile.timeLeft <= NovaTicks)
                deathSpeedBoost = 1f + (NovaTicks - Projectile.timeLeft) / (float)NovaTicks * 2f;

            // El ancla: el CUERPO del sol (×1.30 — el coloso de la mezcla).
            float core = EclipsePrimordialRenderer.SunPx * Math.Max(Projectile.scale, 0.1f);

            for (int i = 0; i < 2; i++)
            {
                float angle = Projectile.rotation * 1.5f + i * (MathHelper.TwoPi / 2f) +
                              Main.rand.NextFloat(-0.25f, 0.25f);
                // Nace en el BORDE del sistema de anillos (~8.6× el cuerpo).
                float dist = (3.8f + 0.7f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2f + i)) * core;
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist,
                    (float)Math.Sin(angle) * dist);

                Vector2 toCenter = Projectile.Center - spawnPos;
                float speed = (6f + 6f * (1f - dist / (16f * core))) * deathSpeedBoost;
                if (toCenter.LengthSquared() > 0.01f)
                {
                    toCenter.Normalize();
                    Vector2 velocity = toCenter * speed;

                    float angleToCenter = (float)Math.Atan2(toCenter.Y, toCenter.X);
                    velocity = velocity.RotateTowards(angleToCenter + MathHelper.PiOver2 * 0.3f, 0.5f);

                    // LA PALETA: MORADO frío al nacer en el borde → AZUL al
                    // caer → DORADO/blanco al arder junto al sol.
                    Color color;
                    if (dist < 2.6f * core)
                    {
                        color = new Color(238, 242, 255); // blanco frío
                    }
                    else
                    {
                        float t = MathHelper.Clamp((dist - 2.6f * core) / (6.0f * core), 0f, 1f);
                        color = t < 0.45f
                            ? Color.Lerp(new Color(185, 105, 255), new Color(92, 150, 255), t / 0.45f)
                            : Color.Lerp(new Color(92, 150, 255), new Color(255, 195, 90), (t - 0.45f) / 0.55f);
                    }

                    Dust d = Dust.NewDustPerfect(spawnPos, DustID.Crimson,
                        velocity, 150, color, 1.2f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                    d.scale = Main.rand.NextFloat(0.8f, 1.4f);
                }
            }
        }

        /// <summary>Chispas aurora capturadas por el campo.</summary>
        private void SpawnCapturedEnergySparks()
        {
            if (Main.rand.NextBool(12))
            {
                float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(380f, 560f) * MathHelper.Max(Projectile.scale, 0.5f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist,
                    (float)Math.Sin(angle) * dist);
                Vector2 vel = (Projectile.Center - spawnPos) * 0.03f;
                Dust d = Dust.NewDustPerfect(spawnPos, DustID.Enchanted_Pink,
                    vel, 255, new Color(185, 150, 255), 0.9f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        /// <summary>Devora el polvo del ambiente en espiral (copia exacta).</summary>
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

        /// <summary>Materia cayendo al sol: estelas TrailGlow espiralando.</summary>
        private void SpawnLibraryFallingMatter()
        {
            for (int i = 0; i < 2; i++)
            {
                float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                // Nace MÁS AFUERA del sistema (la acreción de la mezcla).
                float dist = Main.rand.NextFloat(4.2f, 5.8f) *
                             EclipsePrimordialRenderer.SunPx * MathHelper.Max(Projectile.scale, 0.4f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist,
                    (float)Math.Sin(angle) * dist);

                Vector2 inward = new Vector2(-(float)Math.Cos(angle), -(float)Math.Sin(angle));
                Vector2 tangent = new Vector2(-(float)Math.Sin(angle), (float)Math.Cos(angle));
                Vector2 velocity = inward * Main.rand.NextFloat(1.8f, 2.8f) +
                                   tangent * Main.rand.NextFloat(0.25f, 0.5f);

                Color start = new Color(185, 105, 255, 190);   // morado aurora (frío, lejos)
                Color end = new Color(255, 225, 150, 235);     // oro solar (ardiendo)

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

        /// <summary>Estelas orbitando en la banda del borde (elipse aurora).</summary>
        private void SpawnLibraryAccretionBand()
        {
            if (Main.rand.NextBool(4))
            {
                // La banda vive EN el borde de los anillos del sol.
                float core = EclipsePrimordialRenderer.SunPx *
                               MathHelper.Max(Projectile.scale, 0.4f);
                float radius = Main.rand.NextFloat(4.3f, 6.2f) * core;
                float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                float angVel = 0.16f * (float)Math.Pow(5.8f * core / radius, 1.5f);

                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * radius,
                    (float)Math.Sin(angle) * radius * 1.24f);

                // EL COLOR por DOPPLER: el lado que se ACERCA arde DORADO,
                // el que se ALEJA se enfría al MORADO (la mezcla vive).
                bool hotSide = Math.Cos(angle) < 0f;
                Color c = hotSide ? new Color(255, 195, 90, 200) : new Color(185, 105, 255, 200);

                var p = new ParticleData
                {
                    Position = spawnPos,
                    Velocity = Vector2.Zero,
                    Scale = new Vector2(2.2f, 0.55f),
                    Rotation = angle + MathHelper.PiOver2,
                    RotationSpeed = angVel,
                    PackedColor = ParticleManager.PackColor(c),
                    PackedStartColor = ParticleManager.PackColor(new Color(238, 242, 255, 200)),
                    PackedEndColor = ParticleManager.PackColor(new Color(60, 30, 110, 40)),
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
        //  RENDER — EL SOL + LA MEZCLA (100% código, v6.26)
        // ------------------------------------------------------------------

        public override bool PreDraw(ref Color lightColor)
        {
            // La lente va DETRÁS: el BlackHoleLensSystem pinta el sol y la
            // mezcla ENCIMA de la distorsión llamando a DrawCoreVisuals.
            if (BlackHoleLensSystem.LensActive)
                return false;

            // CONTRATO DE BATCH A PRUEBA DE BALAS (v6.10): durante PreDraw
            // el batch de tML está ABIERTO; hay que CERRARLO antes de que
            // los renderers llamen a Begin() con sus propios estados.
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
        /// Dibuja EL ECLIPSE TOTAL (el sol + el disco de la noche + la
        /// corona + la cromosfera + las prominencias + el anillo de
        /// diamante + los círculos rúnicos). Compartido entre el pase del
        /// mundo (PreDraw) y el pase posterior a la lente.
        /// CONTRATO: el SpriteBatch llega CERRADO y queda CERRADO.
        /// </summary>
        internal static void DrawCoreVisuals(Projectile p)
        {
            Vector2 drawPos = p.Center - Main.screenPosition;
            float time = Main.GlobalTimeWrappedHourly;
            int seed = p.whoAmI * 17 + 5;

            // LA EDAD (para el deslizamiento del disco + la última luz).
            float age = p.ai[0];

            // novaT: el avance de EL RETORNO DE LA LUZ (el colapso final).
            float novaT = 0f;
            if (p.timeLeft <= NovaTicks)
                novaT = MathHelper.Clamp(1f - p.timeLeft / (float)NovaTicks, 0f, 1f);

            // === EL SOL PRIMORDIAL (el coloso de 74 px × escala) ===
            float sunR = EclipsePrimordialRenderer.SunPx * Math.Max(p.scale, 0.02f);

            // === EL ECLIPSE (o SU RETORNO en los ticks finales) ===
            if (novaT > 0f)
                EclipsePrimordialRenderer.DrawRetorno(drawPos, sunR, novaT, time, seed);
            else
                EclipsePrimordialRenderer.Draw(drawPos, sunR, time, seed, age, LifeTicks);
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
        //  IMPACTO Y MUERTE (física exacta, paleta oro solar + aurora)
        // ------------------------------------------------------------------

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode == NetmodeID.Server) return;

            // Micro-colapso sobre el objetivo (aurora).
            ParticlePresets.Implosion(target.Center, 70f, 16, new Color(185, 105, 255), 18);
            ParticlePresets.RingPulse(target.Center, 90f, new Color(200, 170, 255, 170), 22);

            // Implosión: 50 partículas convergiendo en espiral.
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
                        vel, 200, new Color(255, 195, 90), 1.3f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }

            // Explosión: 40 radiales (aurora completa).
            for (int i = 0; i < 40; i++)
            {
                float angle = (MathHelper.TwoPi / 40) * i;
                Vector2 dir = new Vector2(
                    (float)Math.Cos(angle) * Main.rand.NextFloat(5f, 11f),
                    (float)Math.Sin(angle) * Main.rand.NextFloat(5f, 11f));
                Color c = i % 3 == 0 ? new Color(185, 105, 255)
                        : i % 3 == 1 ? new Color(92, 150, 255)
                        : new Color(255, 195, 90);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Crimson,
                    dir, 220, c, 1.5f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Destellos de la corona aurora.
            for (int i = 0; i < 15; i++)
            {
                Color c = Main.rand.NextBool(3)
                    ? new Color(238, 242, 255)   // blanco frío
                    : new Color(200, 160, 255);  // aurora clara
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Pink,
                    new Vector2(Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-4f, 4f)),
                    255, c, 1.0f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
        }

        // ------------------------------------------------------------------
        //  LA NOVA DEL ECLIPSE — la explosión final del arma
        // ------------------------------------------------------------------

        /// <summary>
        /// v6.26 — LA NOVA DEL ECLIPSE (UNA sola vez, guardada en
        /// localAI[2]): el ANILLO DE EINSTEIN (daño completo) + LA NOVA
        /// RÚNICA del sol fundido ×1.6 (daño y radio del Sol XX) — las dos
        /// explosiones en un solo instante — más el sacudón, los sonidos y
        /// las ráfagas de partículas. MP-seguro: los proyectiles de onda
        /// solo los spawnea el dueño; lo visual solo el cliente.
        /// </summary>
        private void TriggerNovaDelEclipse()
        {
            if (Projectile.localAI[2] != 0f) return;
            Projectile.localAI[2] = 1f;

            // === EL DAÑO: Einstein + nova rúnica ×1.6 (una sola vez) ===
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center.X, Projectile.Center.Y, 0f, 0f,
                    ModContent.ProjectileType<CosmicShockwaveProjectile>(),
                    Projectile.damage, 0f, Projectile.owner,
                    0f,
                    CosmicShockwaveProjectile.StyleEinstein,
                    620f);

                // LA NOVA RÚNICA DEL SOL XX: daño ×1.6 y el radio récord
                // de la familia (380 + 14·19 + 120 = 766 px).
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center.X, Projectile.Center.Y, 0f, 0f,
                    ModContent.ProjectileType<CosmicShockwaveProjectile>(),
                    Math.Max(1, (int)(Projectile.damage * 1.6f)), 0f, Projectile.owner,
                    0f,
                    CosmicShockwaveProjectile.StyleNova,
                    766f);
            }

            ParticleManager.PullToGlobalBoost = 1f;

            if (Main.netMode == NetmodeID.Server) return;

            // === EL SACUDÓN Y LOS SONIDOS (la veinte sacude: 12) ===
            try
            {
                Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(
                    Projectile.Center, new Vector2(1f, 0f), 12f, 12, 22, 0.5f,
                    "AethonEclipseNova"));
            }
            catch { }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item88, Projectile.Center);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item45, Projectile.Center);

            // === LOS PRESETS — el colapso dorado y aurora ===
            ParticlePresets.Implosion(Projectile.Center, 210f, 60,
                new Color(185, 105, 255), 32);
            ParticlePresets.Explosion(Projectile.Center, 165f, 36,
                new Color(238, 242, 255), new Color(255, 195, 90), 50);
            ParticlePresets.RingPulse(Projectile.Center, 260f,
                new Color(255, 210, 100, 210), 34);
            ParticlePresets.RingPulse(Projectile.Center, 330f,
                new Color(255, 80, 30, 150), 46);

            // Implosión: 70 partículas convergiendo al punto de la nova.
            for (int i = 0; i < 70; i++)
            {
                float angle = (MathHelper.TwoPi / 70) * i;
                float dist = Main.rand.NextFloat(120f, 210f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist, (float)Math.Sin(angle) * dist);
                Vector2 toCenter = Projectile.Center - spawnPos;
                if (toCenter.LengthSquared() > 0.01f)
                {
                    toCenter.Normalize();
                    Vector2 tangent = new Vector2(-toCenter.Y, toCenter.X) * 0.7f;
                    Dust d = Dust.NewDustPerfect(spawnPos, DustID.Crimson,
                        toCenter * 9f + tangent * 5f, 220, new Color(255, 195, 90), 1.4f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }

            // Explosión: anillo expansivo aurora (morado/azul/dorado).
            for (int i = 0; i < 54; i++)
            {
                float angle = (MathHelper.TwoPi / 54) * i;
                Vector2 dir = new Vector2(
                    (float)Math.Cos(angle) * Main.rand.NextFloat(6f, 14f),
                    (float)Math.Sin(angle) * Main.rand.NextFloat(6f, 14f));
                Color c = i % 3 == 0 ? new Color(185, 105, 255)
                        : i % 3 == 1 ? new Color(92, 150, 255)
                        : new Color(255, 195, 90);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Crimson,
                    dir, 230, c, 1.6f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // LAS RUNAS DE ECO: la nova escupe los glifos del sol fundido.
            int echoRunes = 34;
            for (int i = 0; i < echoRunes; i++)
            {
                float angle = (MathHelper.TwoPi / echoRunes) * i +
                              Main.rand.NextFloat(-0.08f, 0.08f);
                Vector2 outward = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Gold,
                    outward * Main.rand.NextFloat(6f, 13f) * Math.Max(Projectile.scale, 0.4f),
                    255, new Color(255, 235, 170), 1.5f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // EL FUEGO DE LA NOVA (dusts del patrón del sol).
            for (int i = 0; i < 60; i++)
            {
                float angle = (MathHelper.TwoPi / 60) * i;
                Vector2 dir = new Vector2(
                    (float)Math.Cos(angle) * Main.rand.NextFloat(6f, 14f),
                    (float)Math.Sin(angle) * Main.rand.NextFloat(6f, 14f)) *
                    Math.Max(Projectile.scale, 0.4f);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                    dir, 240, new Color(255, 200, 100), 1.7f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        public override void OnKill(int timeLeft)
        {
            // Si la nova NO fue disparada por el reloj (muerte prematura),
            // se dispara AQUÍ — nunca se pierde la explosión final.
            TriggerNovaDelEclipse();
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
