using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Particles;
using AethonMod.Content.Projectiles.V20;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// SunProjectile — una estrella de plasma viva (10 segundos de vida).
    ///
    /// RENDER (3 capas de profundidad):
    ///   1. Backglow con BloomCircleSmall: amarillo * 0.7 (escala 0.95) + rojo * 0.45 (escala 1.61).
    ///   2. RadialShineShader sobre WavyBlotchNoise: color (252, 212, 112) * 0.24,
    ///      escala = width * scale * 2.72 / tamaño de la textura.
    ///   3. SunShader sobre DendriticNoiseZoomedOut (canvas):
    ///      coronaIntensityFactor = 0.05, mainColor = blanco, darkerColor = (204, 92, 25),
    ///      subtractiveAccentFactor = (181, 0, 0), sphereSpinTime = GlobalTimeWrappedHourly * 0.9,
    ///      s1 = WavyBlotchNoise, s2 = PsychedelicWingTextureOffsetMap,
    ///      escala = width * scale * 1.5 / tamaño de la textura.
    ///
    /// CICLO DE VIDA (v5.85/v5.86/v5.94) - el sol como cuerpo celeste completo:
    ///   - t=0s    : nace con pop elástico (SIN llamarada: en t=0 aún está
    ///               sobre el jugador y la nova estallaría en su posición).
    ///   - cada 2s : llamarada solar desde el centro (4 en total: 2, 4, 6, 8s) -
    ///               v5.91: la llamarada (PhoenixNova) se dibuja DETRÁS del
    ///               cuerpo del sol (DrawBehind → drawCacheProjsBehindProjectiles).
    ///               v5.94: la llamarada YA NO dibuja anillos (los anillos son
    ///               de la onda expansiva final: "solo deben salir al final").
    ///   - t=7s    : aparece SUPERNOVAPROJECTILE centrado y sincronizado (dura 3s);
    ///               carga energía mientras la gravedad del sol AUMENTA progresivamente
    ///               y su luz se intensifica (materia convergiendo en espiral).
    ///               v5.91: nace con el flag ai[2]=1 ("invocada por el sol") para
    ///               NO duplicar ondas al morir - las ondas de fuego las genera
    ///               EL SOL (autoridad absoluta de la sincronización).
    ///   - t=7-10s : v5.94 - GIGANTE ROJA: la estrella amarilla se HINCHA hasta
    ///               x1.85 y ENROJECE (backglow, aura, SunShader, luz, dusts y
    ///               partículas se tiñen) mientras su DAÑO DE ÁREA crece con
    ///               ella: hitbox de contacto x1.85 y daño x1.75, quemadura
    ///               de 10 s (petición del usuario: "una estrella amarilla que
    ///               se convierte en gigante roja y luego explota, todo esto
    ///               haciendo que su daño en area crezca junto con la estrella").
    ///   - t=10s   : ambos proyectiles explotan SIMULTÁNEAMENTE - nova masiva con
    ///               3 ONDAS EXPANSIVAS DE FUEGO (v5.94: radii 240/300/360 -
    ///               antes 360/450/540, cubrían toda la pantalla) que BARRAN
    ///               dañando cada 0.1 s (v5.91) y aplicando QUEMADURA de 10 s,
    ///               más el estallido de dusts y temblor de pantalla.
    ///
    ///     /// v5.91 — LA EXPLOSIÓN FINAL ES LA SUPERNOVA (SupernovaStaff), SINCRONIZADA
    /// POR CONSTRUCCIÓN: el OnKill del sol es ahora la AUTORIDAD de la explosión
    /// final — (1) mata la Supernova hija EN EL MISMO TICK (su flash + partículas
    /// estallan exactamente con el sol, sin depender de la sincronización por
    /// índice de la v5.88, que era el punto único de fallo de la "onda expansiva
    /// que no se procesaba"), (2) genera las 3 ondas de fuego y (3) el AoE del
    /// núcleo con el daño de la nova (daño del sol × 1.25). La Supernova hija
    /// (ai[2]=1) se salta sus propias ondas/AoE: CERO doble explosión — su
    /// papel es ser EL ESPECTÁCULO FINAL (flash + estallido + viento estelar).
    ///
    /// GRAVEDAD (cuerpo celeste): atrae solo enemigos, con una fuerza ~10 veces
    /// menor que la del agujero negro. Durante la carga de la supernova (últimos
    /// 3 segundos) la fuerza se multiplica progresivamente (x4 en el pico).
    ///
    /// QUEMADURA: bola de plasma ardiente → inflama enemigos al contacto (OnFire).
    /// (La quemadura potenciada por daño mágico se implementará cuando este
    /// proyectil se integre en el Grimorio, el arma definitiva.)
    ///
    /// v5.84 — Capa de partículas de la librería propia (data-oriented, additive,
    /// render en PostDrawTiles): corona de glóbulos SoftGlow orbitando con ColorShift
    /// amarillo→naranja, viento solar de estelas TrailGlow radiales, destellos
    /// SparkleStar con FadeIn+EmitLight, arcos de prominencia con estrellas orbitando.
    /// </summary>
    public class SunProjectile : ModProjectile
    {
        private Ref<Effect> _sunShader;
        private Ref<Effect> _shineShader;
        private bool _sunShaderFailed;
        private bool _shineShaderFailed;

        /// <summary>Tiempo visual de vida — usada para el pop elástico y el ritmo de llamaradas.</summary>
        public ref float VisualsTime => ref Projectile.ai[0];

        /// <summary>Índice del proyectil Supernova hijo (-1 = aún no invocado).</summary>
        public ref float SupernovaIndex => ref Projectile.ai[1];

        /// <summary>Duración total del sol: 10 segundos exactos.</summary>
        private const int SunLifetime = 600;

        /// <summary>Momento (ticks restantes) en el que nace la supernova: segundo 7.</summary>
        private const int SupernovaSpawnAtRemaining = 180;

        /// <summary>Cadencia de las llamaradas solares: cada 2 segundos.</summary>
        private const int FlareInterval = 120;

        /// <summary>
        /// v5.94 — Progreso de la fase GIGANTE ROJA: 0 durante la secuencia
        /// principal (estrella amarilla) y 0→1 en los últimos 3 s (la estrella
        /// se hincha y enrojece antes de la supernova). Petición del usuario:
        /// "es una estrella amarilla que se convierte en gigante roja y luego
        /// explota, todo esto haciendo que su daño en area crezca junto con
        /// la estrella".
        /// </summary>
        private float RedGiantProgress => Projectile.timeLeft <= SupernovaSpawnAtRemaining
            ? 1f - Projectile.timeLeft / (float)SupernovaSpawnAtRemaining
            : 0f;

        /// <summary>v5.94 — Mezcla un color hacia el ROJO de la gigante (fase final).</summary>
        private Color ToRedGiant(Color c)
        {
            float p = RedGiantProgress;
            if (p <= 0f) return c;
            int g = Math.Max((int)(c.G * (1f - 0.7f * p)), 20);
            int b = Math.Max((int)(c.B * (1f - 0.85f * p)), 8);
            return new Color(c.R, g, b, c.A);
        }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 92;
            Projectile.height = 92;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = SunLifetime;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
        }

        public override void AI()
        {
            // === LLAMARADAS SOLARES (PhoenixNova cada 2 s, DESDE t=2s) ===
            // v5.86: la primera llamarada YA NO se lanza en t=0 — en ese instante
            // el sol aún está sobre el jugador (nace en su posición y deriva con
            // el disparo), así que la nova explotaba "en la posición del jugador".
            // Ahora la primera espera al segundo 2, cuando el sol ya se ha alejado:
            // llamaradas en t=2, 4, 6 y 8 s (4 en total).
            if (VisualsTime > 0f && VisualsTime % FlareInterval == 0f && Projectile.owner == Main.myPlayer)
            {
                int flareDamage = (int)(Projectile.damage * 0.5f);
                if (flareDamage < 1) flareDamage = 1;
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<V20.PhoenixNovaProjectile>(),
                    flareDamage, Projectile.knockBack * 0.5f,
                    Projectile.owner);
            }

            // === POP ELÁSTICO DE APARICIÓN ===
            Projectile.scale = ElasticOut(Utils.GetLerpValue(0f, 90f, VisualsTime, true)) *
                               (float)Math.Sqrt(Utils.GetLerpValue(0f, 45f, VisualsTime, true));
            VisualsTime += 1f;

            // === v5.94 — GIGANTE ROJA (últimos 3 s) ===
            // La estrella amarilla de la secuencia principal SE HINCHA y
            // ENROJECE (hasta ×1.85 de tamaño) mientras la supernova carga:
            // compresión anterior (×1.0008) e hinchazón final de 30 ticks
            // (×1.025) eliminadas — el crecimiento es ahora CONTINUO y el
            // DAÑO DE ÁREA crece JUNTO con la estrella (hitbox + daño).
            bool supernovaCharging = Projectile.timeLeft <= SupernovaSpawnAtRemaining;
            float redProgress = RedGiantProgress;
            if (supernovaCharging)
            {
                // Crecimiento suave de la gigante roja (smoothstep → ×1.85).
                float growEase = redProgress * redProgress * (3f - 2f * redProgress);
                Projectile.scale *= 1f + 0.85f * growEase;

                // Daño de contacto creciendo con la estrella: ×1 → ×1.75.
                // ai[2] guarda el daño base (registrado al nacer).
                float baseDmg = Projectile.ai[2] > 0f ? Projectile.ai[2] : Projectile.damage;
                Projectile.ai[2] = baseDmg;
                int newDmg = Math.Max(1, (int)(baseDmg * (1f + 0.75f * redProgress)));
                if (newDmg != Projectile.damage)
                    Projectile.damage = newDmg;

                // El hitbox de área CRECE con la estrella (Resize mantiene el
                // centro: verificado en el código de Terraria).
                int sz = Math.Max(16, (int)(92f * Projectile.scale));
                if (sz != Projectile.width)
                    Projectile.Resize(sz, sz);
            }
            else if (Projectile.ai[2] <= 0f)
            {
                // Daño base registrado al nacer (para el ramp de la gigante).
                Projectile.ai[2] = Projectile.damage;
            }

            // === MOVIMIENTO: deriva lenta y frenado ===
            Projectile.velocity *= 0.97f;
            Projectile.rotation += 0.01f;

            // === SUPERNOVA SINCRONIZADA (aparece en el segundo 7) ===
            if (Projectile.timeLeft == SupernovaSpawnAtRemaining && Projectile.owner == Main.myPlayer)
            {
                int novaDamage = Math.Max(1, (int)(Projectile.damage * 1.25f));
                // v5.91 — ai[2] = 1: flag "invocada por el sol". La Supernova
                // hija NO generará sus propias ondas/AoE al morir (el OnKill del
                // SOL es la autoridad de la explosión final: ondas + AoE salen
                // del sol, la hija aporta el espectáculo visual sincronizado).
                // Así jamás hay doble explosión ni dependencia frágil de índices.
                int idx = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center, Projectile.velocity,
                    ModContent.ProjectileType<V20.SupernovaProjectile>(),
                    novaDamage, Projectile.knockBack,
                    Projectile.owner,
                    0f, 0f, 1f); // ai[0]=edad, ai[1]=libre, ai[2]=SunInvoked
                SupernovaIndex = idx;
            }

            // Mantener la supernova PERFECTAMENTE centrada en el sol (y sincronizada).
            if (SupernovaIndex >= 0f)
            {
                int idx = (int)SupernovaIndex;
                if (idx >= 0 && idx < Main.maxProjectiles &&
                    Main.projectile[idx].active &&
                    Main.projectile[idx].type == ModContent.ProjectileType<V20.SupernovaProjectile>())
                {
                    // El sol arrastra a la supernova con él (deriva compartida).
                    Main.projectile[idx].Center = Projectile.Center;
                    Main.projectile[idx].velocity = Projectile.velocity;

                    // SINCRONIZACIÓN EXACTA: en los últimos ticks, la cuenta
                    // regresiva de la supernova se clava a la del sol → ambos
                    // mueren (y explotan) en el MISMO tick, sin deriva de índices.
                    if (Projectile.timeLeft <= 2)
                        Main.projectile[idx].timeLeft =
                            Math.Min(Main.projectile[idx].timeLeft, Projectile.timeLeft);
                }
                else
                {
                    SupernovaIndex = -1f;
                }
            }

            // === GRAVEDAD DEL SOL — 10 veces menor que el agujero negro, solo enemigos ===
            float gravityRadius = 280f;
            float baseStrength = 0.26f; // agujero negro: 2.6 → sol: 2.6 / 10
            // Durante la carga de la supernova la fuerza crece progresivamente (x4 pico).
            float chargeMult = 1f;
            if (supernovaCharging)
            {
                float chargeProgress = 1f - Projectile.timeLeft / (float)SupernovaSpawnAtRemaining;
                chargeMult = 1f + chargeProgress * 3f;
            }
            float sunGravity = baseStrength * chargeMult;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy()) continue;
                Vector2 toCenter = Projectile.Center - npc.Center;
                float dist = toCenter.Length();
                if (dist > gravityRadius || dist < 5f) continue;
                float strength = (1f - dist / gravityRadius) * sunGravity;
                if (toCenter.LengthSquared() > 0.01f)
                {
                    toCenter.Normalize();
                    npc.velocity += toCenter * strength;
                }
            }

            // === PARTÍCULAS (solo cliente) ===
            if (Main.netMode != NetmodeID.Server)
            {
                // Dusts vanilla (capa frontal, se dibujan encima del canvas del shader)
                SpawnOrbitingSparks();
                SpawnFlames();
                SpawnSmoke();
                SpawnSolarFlare();
                SpawnTwinkles();

                // Partículas de la librería propia (capa de fondo aditiva)
                SpawnLibraryCorona();
                SpawnLibrarySolarWind();
                SpawnLibraryTwinkles();
                SpawnLibraryFlareLoop();

                // v5.85: materia convergiendo durante la carga de la supernova
                if (supernovaCharging && Projectile.scale > 0.3f)
                {
                    SpawnSupernovaChargeIntake();
                }
            }

            // === ILUMINACIÓN INTENSA (con pulso sutil + crecimiento en la carga) ===
            // v5.94 — la luz ENROJECE con la gigante: amarillo cálido → rojo.
            float pulse = 0.92f + 0.08f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4f);
            float chargeLight = supernovaCharging
                ? 1f + (1f - Projectile.timeLeft / (float)SupernovaSpawnAtRemaining) * 0.8f
                : 1f;
            Vector3 lightColor = Vector3.Lerp(
                new Vector3(1f, 0.9f, 0.5f),
                new Vector3(1f, 0.25f, 0.1f),
                redProgress);
            Lighting.AddLight(Projectile.Center,
                lightColor * 3.2f * pulse * chargeLight);
        }

        // ------------------------------------------------------------------
        //  PARTÍCULAS VANILLA (capa frontal)
        // ------------------------------------------------------------------

        /// <summary>Chispas de fuego (Torch) orbitando y cayendo hacia la superficie.</summary>
        private void SpawnOrbitingSparks()
        {
            if (Main.rand.NextBool(2))
            {
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(40f, 60f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist,
                    (float)Math.Sin(angle) * dist);
                Vector2 vel = Projectile.Center - spawnPos;
                if (vel.LengthSquared() > 0.01f)
                {
                    vel.Normalize();
                    vel *= Main.rand.NextFloat(1f, 3f);
                    vel += new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-1f, 1f));
                    Dust d = Dust.NewDustPerfect(spawnPos, DustID.Torch,
                        vel, 150, ToRedGiant(new Color(255, 150, 50)), 1.0f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }
        }

        /// <summary>Llamas de GoldFlame escapando de la fotosfera.</summary>
        private void SpawnFlames()
        {
            if (Main.rand.NextBool(3))
            {
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(30f, 45f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist,
                    (float)Math.Sin(angle) * dist);
                Vector2 vel = new Vector2(
                    (float)Math.Cos(angle) * 2f,
                    (float)Math.Sin(angle) * 2f);
                Dust d = Dust.NewDustPerfect(spawnPos, DustID.GoldFlame,
                    vel, 200, ToRedGiant(new Color(255, 200, 100)), 1.2f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        /// <summary>Humo cálido ascendiendo desde la corona.</summary>
        private void SpawnSmoke()
        {
            if (Main.rand.NextBool(8))
            {
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(50f, 70f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist,
                    (float)Math.Sin(angle) * dist);
                Vector2 vel = new Vector2(
                    (float)Math.Cos(angle) * 0.5f,
                    (float)Math.Sin(angle) * 0.5f - 1f);
                Dust d = Dust.NewDustPerfect(spawnPos, DustID.Smoke,
                    vel, 60, new Color(100, 60, 30), 0.6f);
                d.noGravity = false;
                d.fadeIn = 0f;
            }
        }

        /// <summary>Prominencias periódicas: explosión radial de fuego desde el borde (~0.75 s).</summary>
        private void SpawnSolarFlare()
        {
            if (VisualsTime % 45f == 0f && VisualsTime > 30f)
            {
                float baseAngle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                int count = 12;
                for (int i = 0; i < count; i++)
                {
                    float angle = baseAngle + (MathHelper.TwoPi / count) * i * 0.35f;
                    float dist = Projectile.width * 0.55f * Projectile.scale;
                    Vector2 spawnPos = Projectile.Center + new Vector2(
                        (float)Math.Cos(angle) * dist,
                        (float)Math.Sin(angle) * dist);
                    Vector2 vel = new Vector2(
                        (float)Math.Cos(angle) * Main.rand.NextFloat(3f, 6f),
                        (float)Math.Sin(angle) * Main.rand.NextFloat(3f, 6f));
                    Dust d = Dust.NewDustPerfect(spawnPos, DustID.GoldFlame,
                        vel, 220, ToRedGiant(new Color(255, 180, 80)), 1.4f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }
        }

        /// <summary>Destellos encantados parpadeando alrededor de la estrella.</summary>
        private void SpawnTwinkles()
        {
            if (Main.rand.NextBool(20))
            {
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(60f, 110f) * Projectile.scale;
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist,
                    (float)Math.Sin(angle) * dist);
                Dust d = Dust.NewDustPerfect(spawnPos, DustID.Enchanted_Gold,
                    Vector2.Zero, 255, ToRedGiant(new Color(255, 240, 180)), 0.7f);
                d.noGravity = true;
                d.fadeIn = 0.3f;
            }
        }

        // ------------------------------------------------------------------
        //  PARTÍCULAS DE LA LIBRERÍA PROPIA (capa de fondo aditiva)
        // ------------------------------------------------------------------

        /// <summary>Corona de plasma orbitando: SoftGlow con Orbit y ColorShift amarillo→naranja.</summary>
        private void SpawnLibraryCorona()
        {
            if (Main.rand.NextBool(2))
            {
                float radius = Main.rand.NextFloat(48f, 60f) * MathHelper.Max(Projectile.scale, 0.4f);
                float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                float angVel = Main.rand.NextFloat(0.045f, 0.075f);

                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * radius,
                    (float)Math.Sin(angle) * radius);

                var p = new ParticleData
                {
                    Position = spawnPos,
                    Velocity = Vector2.Zero,
                    Scale = Vector2.One * Main.rand.NextFloat(0.55f, 0.95f),
                    PackedColor = ParticleManager.PackColor(ToRedGiant(new Color(255, 235, 140, 150))),
                    PackedStartColor = ParticleManager.PackColor(ToRedGiant(new Color(255, 235, 140, 150))),
                    PackedEndColor = ParticleManager.PackColor(ToRedGiant(new Color(255, 110, 30, 30))),
                    TimeLeft = 55,
                    Duration = 55,
                    TextureId = ParticleTex.SoftGlow,
                    BlendMode = 1,
                    LayerPriority = LayerPriorities.BeforeProjectiles,
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

        /// <summary>Viento solar radial: estelas TrailGlow fluyendo hacia fuera desde la fotosfera.</summary>
        private void SpawnLibrarySolarWind()
        {
            if (Main.rand.NextBool(3))
            {
                float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(34f, 44f) * MathHelper.Max(Projectile.scale, 0.4f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist,
                    (float)Math.Sin(angle) * dist);
                Vector2 outward = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));

                var p = new ParticleData
                {
                    Position = spawnPos,
                    Velocity = outward * Main.rand.NextFloat(1.2f, 2.2f),
                    Scale = new Vector2(1.5f, 0.4f),
                    Rotation = angle,
                    PackedColor = ParticleManager.PackColor(ToRedGiant(new Color(255, 245, 190, 160))),
                    PackedStartColor = ParticleManager.PackColor(ToRedGiant(new Color(255, 245, 190, 160))),
                    PackedEndColor = ParticleManager.PackColor(ToRedGiant(new Color(255, 120, 40, 20))),
                    TimeLeft = 32,
                    Duration = 32,
                    TextureId = ParticleTex.TrailGlow,
                    BlendMode = 1,
                    LayerPriority = LayerPriorities.BeforeProjectiles,
                };
                p.EnableComponent(ComponentFlag.FadeOut);
                p.EnableComponent(ComponentFlag.ColorShift);
                ParticleManager.Spawn(p);
            }
        }

        /// <summary>Destellos luminosos: SparkleStar con FadeIn + FadeOut + EmitLight.</summary>
        private void SpawnLibraryTwinkles()
        {
            if (Main.rand.NextBool(8))
            {
                float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(55f, 100f) * MathHelper.Max(Projectile.scale, 0.4f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist,
                    (float)Math.Sin(angle) * dist);

                var p = new ParticleData
                {
                    Position = spawnPos,
                    Velocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 0.25f,
                    Scale = Vector2.One * Main.rand.NextFloat(0.5f, 0.9f),
                    Rotation = Main.rand.NextFloat(0f, MathHelper.TwoPi),
                    RotationSpeed = Main.rand.NextFloat(-0.1f, 0.1f),
                    PackedColor = ParticleManager.PackColor(new Color(255, 250, 210, 200)),
                    PackedStartColor = ParticleManager.PackColor(new Color(255, 250, 210, 200)),
                    TimeLeft = 40,
                    Duration = 40,
                    TextureId = ParticleTex.SparkleStar,
                    BlendMode = 1,
                    LayerPriority = LayerPriorities.BeforeProjectiles,
                };
                p.UserData0 = 8f;
                p.EnableComponent(ComponentFlag.FadeIn);
                p.EnableComponent(ComponentFlag.FadeOut);
                p.EnableComponent(ComponentFlag.EmitLight);
                ParticleManager.Spawn(p);
            }
        }

        /// <summary>Arcos de prominencia: estrellas orbitando en el borde de cada llamarada (~0.75 s).</summary>
        private void SpawnLibraryFlareLoop()
        {
            if (VisualsTime % 45f == 0f && VisualsTime > 30f && Projectile.scale > 0.3f)
            {
                float flareRadius = Projectile.width * 0.62f * Projectile.scale;
                for (int i = 0; i < 7; i++)
                {
                    float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                    float radius = flareRadius + Main.rand.NextFloat(-4f, 8f);
                    float angVel = Main.rand.NextBool(2) ? 0.14f : -0.14f;

                    Vector2 spawnPos = Projectile.Center + new Vector2(
                        (float)Math.Cos(angle) * radius,
                        (float)Math.Sin(angle) * radius);

                    var p = new ParticleData
                    {
                        Position = spawnPos,
                        Velocity = Vector2.Zero,
                        Scale = Vector2.One * Main.rand.NextFloat(0.4f, 0.8f),
                        Rotation = angle,
                        RotationSpeed = angVel * 2f,
                        PackedColor = ParticleManager.PackColor(ToRedGiant(new Color(255, 210, 110, 210))),
                        PackedStartColor = ParticleManager.PackColor(ToRedGiant(new Color(255, 230, 160, 210))),
                        PackedEndColor = ParticleManager.PackColor(ToRedGiant(new Color(255, 90, 20, 30))),
                        TimeLeft = 45,
                        Duration = 45,
                        TextureId = ParticleTex.Star,
                        BlendMode = 1,
                        LayerPriority = LayerPriorities.BeforeProjectiles,
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
        }

        /// <summary>
        /// v5.85 — Materia convergiendo durante la carga de la supernova:
        /// estelas doradas cayendo en espiral hacia el sol mientras la fuerza
        /// gravitatoria crece (los 3 segundos previos a la nova final).
        /// </summary>
        private void SpawnSupernovaChargeIntake()
        {
            for (int i = 0; i < 2; i++)
            {
                float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(90f, 150f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist,
                    (float)Math.Sin(angle) * dist);

                Vector2 toCenter = Projectile.Center - spawnPos;
                if (toCenter.LengthSquared() < 0.01f) continue;
                toCenter.Normalize();
                Vector2 tangent = new Vector2(-toCenter.Y, toCenter.X);

                // Cuanto más avanzada la carga, más rápido converge la materia.
                float chargeProgress = 1f - Projectile.timeLeft / (float)SupernovaSpawnAtRemaining;
                float speed = 2.2f + chargeProgress * 3.5f;

                var p = new ParticleData
                {
                    Position = spawnPos,
                    Velocity = toCenter * speed + tangent * speed * 0.55f,
                    Scale = new Vector2(1.6f, 0.4f),
                    Rotation = (float)Math.Atan2(toCenter.Y, toCenter.X),
                    PackedColor = ParticleManager.PackColor(ToRedGiant(new Color(255, 240, 170, 190))),
                    PackedStartColor = ParticleManager.PackColor(ToRedGiant(new Color(255, 240, 170, 190))),
                    PackedEndColor = ParticleManager.PackColor(ToRedGiant(new Color(255, 140, 40, 40))),
                    TimeLeft = 38,
                    Duration = 38,
                    TextureId = ParticleTex.TrailGlow,
                    BlendMode = 1,
                    LayerPriority = LayerPriorities.BeforeProjectiles,
                };
                p.EnableComponent(ComponentFlag.FadeOut);
                p.EnableComponent(ComponentFlag.ColorShift);
                p.EnableComponent(ComponentFlag.ScaleDown);
                ParticleManager.Spawn(p);
            }
        }

        // ------------------------------------------------------------------
        //  RENDER
        // ------------------------------------------------------------------

        public override bool PreDraw(ref Color lightColor)
        {
            if (!_sunShaderFailed && _sunShader == null)
            {
                try
                {
                    _sunShader = new Ref<Effect>(ModContent.Request<Effect>(
                        "AethonMod/Content/Effects/Shaders/SunShader",
                        AssetRequestMode.ImmediateLoad).Value);
                }
                catch
                {
                    _sunShaderFailed = true;
                }
            }
            if (!_shineShaderFailed && _shineShader == null)
            {
                try
                {
                    _shineShader = new Ref<Effect>(ModContent.Request<Effect>(
                        "AethonMod/Content/Effects/Shaders/RadialShineShader",
                        AssetRequestMode.ImmediateLoad).Value);
                }
                catch
                {
                    _shineShaderFailed = true;
                }
            }

            try
            {
                Vector2 drawPos = Projectile.Center - Main.screenPosition;
                float scale = Projectile.scale;
                // v5.94 — GIGANTE ROJA: mezcla de color según la fase (0 = amarilla).
                float rg = RedGiantProgress;

                // === 1. BACKGLOW ===
                Texture2D bloomCircle = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Textures/BloomCircleSmall").Value;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                    SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                // Amarillo → ROJO brillante (pequeño e intenso, crece con la gigante)
                Color glowHot = Color.Lerp(new Color(255, 230, 100), new Color(255, 75, 25), rg);
                glowHot.A = 0; // igual que el original (alpha 0)
                Main.spriteBatch.Draw(bloomCircle, drawPos, null,
                    glowHot * 0.7f, 0f,
                    bloomCircle.Size() * 0.5f, scale * (0.95f + 0.35f * rg), SpriteEffects.None, 0f);
                // Rojo profundo (grande y tenue — envuelve a la gigante)
                Color glowRed = Color.Lerp(new Color(255, 50, 0), new Color(255, 30, 10), rg);
                glowRed.A = 0; // igual que el original (alpha 0)
                Main.spriteBatch.Draw(bloomCircle, drawPos, null,
                    glowRed * 0.45f, 0f,
                    bloomCircle.Size() * 0.5f, scale * (1.61f + 0.5f * rg), SpriteEffects.None, 0f);
                Main.spriteBatch.End();

                // === 2. RADIAL SHINE (aura con ruido animado) ===
                Texture2D wavyBlotch = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Textures/WavyBlotchNoise").Value;
                if (_shineShader != null && _shineShader.Value != null)
                {
                    Effect shineShader = _shineShader.Value;
                    shineShader.Parameters["globalTime"].SetValue(Main.GlobalTimeWrappedHourly);
                    Vector2 shineScale = Vector2.One * Projectile.width * scale * 2.72f / wavyBlotch.Size();

                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                        SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone,
                        null, Main.GameViewMatrix.TransformationMatrix);
                    _shineShader.Value.CurrentTechnique.Passes[0].Apply();
                    // v5.94 — el aura ENROJECE con la gigante.
                    Color shineColor = Color.Lerp(new Color(252, 212, 112), new Color(255, 95, 45), rg);
                    Main.spriteBatch.Draw(wavyBlotch, drawPos, null,
                        shineColor * 0.24f, Projectile.rotation,
                        wavyBlotch.Size() * 0.5f, shineScale, SpriteEffects.None, 0f);
                    Main.spriteBatch.End();
                }

                // === 3. SUNSHADER (la estrella) ===
                if (_sunShader != null && _sunShader.Value != null)
                {
                    Effect shader = _sunShader.Value;
                    Texture2D psychedelicWing = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Textures/PsychedelicWingTextureOffsetMap").Value;
                    Texture2D dendritic = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Textures/DendriticNoiseZoomedOut").Value;

                    shader.Parameters["coronaIntensityFactor"].SetValue(0.05f);
                    // v5.94 — GIGANTE ROJA: el cuerpo de la estrella enrojece.
                    shader.Parameters["mainColor"].SetValue(
                        Color.Lerp(new Color(255, 255, 255), new Color(255, 150, 120), rg).ToVector3());
                    shader.Parameters["darkerColor"].SetValue(
                        Color.Lerp(new Color(204, 92, 25), new Color(150, 28, 12), rg).ToVector3());
                    shader.Parameters["subtractiveAccentFactor"].SetValue(new Color(181, 0, 0).ToVector3());
                    shader.Parameters["sphereSpinTime"].SetValue(Main.GlobalTimeWrappedHourly * 0.9f);
                    shader.Parameters["globalTime"].SetValue(Main.GlobalTimeWrappedHourly);

                    // s1 = accentNoise, s2 = uvOffsetNoise
                    Main.graphics.GraphicsDevice.Textures[1] = wavyBlotch;
                    Main.graphics.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
                    Main.graphics.GraphicsDevice.Textures[2] = psychedelicWing;
                    Main.graphics.GraphicsDevice.SamplerStates[2] = SamplerState.LinearWrap;

                    Vector2 drawScale = Vector2.One * Projectile.width * scale * 1.5f / dendritic.Size();

                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                        SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone,
                        null, Main.GameViewMatrix.TransformationMatrix);
                    shader.CurrentTechnique.Passes[0].Apply();
                    Main.spriteBatch.Draw(dendritic, drawPos, null, Color.White, Projectile.rotation,
                        dendritic.Size() * 0.5f, drawScale, SpriteEffects.None, 0f);
                    Main.spriteBatch.End();
                }
                else
                {
                    // === FALLBACK: dibujado manual si el shader no carga ===
                    DrawFallback(drawPos, scale);
                }
            }
            catch
            {
                // v5.90 — cierre defensivo SOLO en el path de error: si la
                // excepción interrumpió un Begin a medias, lo cerramos aquí.
                // (El try{End} incondicional de v5.88 disparaba una excepción
                // first-chance cada frame — tML la registraba como "Excepción
                // silenciosa" en el client.log.)
                try { Main.spriteBatch.End(); } catch { }
            }

            RestoreSpriteBatch();
            return false;
        }

        /// <summary>Restaura el SpriteBatch al estado que tML espera tras PreDraw.</summary>
        private static void RestoreSpriteBatch()
        {
            // v5.90 — Begin directo: el path normal deja el batch CERRADO (todas
            // las capas están balanceadas Begin→End); el cierre defensivo de
            // emergencia vive en el catch del PreDraw, no aquí cada frame
            // (disparaba una "Excepción silenciosa" por frame en el log).
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise,
                null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>Dibujado manual de respaldo (glow multicapa naranja).</summary>
        private void DrawFallback(Vector2 drawPos, float scale)
        {
            Texture2D glowTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
            float pulse = 0.9f + 0.1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4f);

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(glowTex, drawPos, null,
                new Color(255, 250, 200, 220), 0f,
                glowTex.Size() * 0.5f, 1.5f * scale * pulse, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(glowTex, drawPos, null,
                new Color(255, 180, 60, 180), 0f,
                glowTex.Size() * 0.5f, 2.0f * scale * pulse, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(glowTex, drawPos, null,
                new Color(200, 50, 0, 100), 0f,
                glowTex.Size() * 0.5f, 2.8f * scale * pulse, SpriteEffects.None, 0f);
            Main.spriteBatch.End();
        }

        // ------------------------------------------------------------------
        //  IMPACTO Y MUERTE
        // ------------------------------------------------------------------

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode == NetmodeID.Server) return;

            // Estallido solar de la librería sobre el objetivo
            ParticlePresets.Explosion(target.Center, 60f, 14,
                new Color(255, 240, 170), new Color(255, 110, 30), 26);
            ParticlePresets.RingPulse(target.Center, 85f, new Color(255, 200, 90, 180), 20);

            // Explosión radial de fuego sobre el objetivo
            for (int i = 0; i < 30; i++)
            {
                float angle = (MathHelper.TwoPi / 30) * i;
                Vector2 dir = new Vector2(
                    (float)Math.Cos(angle) * Main.rand.NextFloat(5f, 10f),
                    (float)Math.Sin(angle) * Main.rand.NextFloat(5f, 10f));
                Dust d = Dust.NewDustPerfect(target.Center, DustID.GoldFlame,
                    dir, 220, new Color(255, 200, 100), 1.3f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Ráfaga de chispas Torch
            for (int i = 0; i < 12; i++)
            {
                Vector2 dir = new Vector2(Main.rand.NextFloat(-5f, 5f), Main.rand.NextFloat(-5f, 5f));
                Dust d = Dust.NewDustPerfect(target.Center, DustID.Torch,
                    dir, 180, new Color(255, 150, 50), 1.1f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // QUEMADURA: el sol es una bola de plasma ardiente → inflama al enemigo.
            // v5.94 - GIGANTE ROJA: la estrella hinchada quema el DOBLE (10 s).
            target.AddBuff(BuffID.OnFire, RedGiantProgress > 0f ? 600 : 300);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, target.Center);
        }

        public override void OnKill(int timeLeft)
        {
            // ================================================================
            //  v5.91 — LA EXPLOSIÓN FINAL ES LA SUPERNOVA, SINCRONIZADA POR
            //  CONSTRUCCIÓN: el OnKill del sol es la AUTORIDAD (el sol muere
            //  EXACTAMENTE a los 10 s — timeLeft fijo, nada lo mata antes).
            //  ================================================================
            //  1. Mata la Supernova hija EN ESTE MISMO TICK → su flash + estallido
            //     + viento estelar ocurren EXACTAMENTE con la muerte del sol
            //     (la v5.88 dependía de la sincronización por índice ai[1] + un
            //     clamp de timeLeft — punto único de fallo del "la onda no se
            //     procesó correctamente": si el índice cambiaba o el clamp no
            //     llegaba a aplicar, la nova moría antes/después del sol y la
            //     ola se perdía o descuadraba).
            //  2. Genera LAS 3 ONDAS EXPANSIVAS DE FUEGO (las mismas de la nova
            //     v5.88: radii 360/450/540, retardo escalonado de 8 ticks) con el
            //     daño de la nova (sol × 1.25 × 0.5) — cada una BARRA dañando
            //     cada 0.1 s a medida que avanza y aplicando QUEMADURA 10 s.
            //  3. AoE del núcleo (340 px) con el daño de la nova + quemadura 10 s.
            //  La Supernova hija (ai[2]=1) NO genera ondas/AoE propios → cero
            //  dobles. La SupernovaStaff standalone conserva su explosión completa.
            TryKillSupernova();

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int novaDamage = Math.Max(1, (int)(Projectile.damage * 1.25f));
                int waveDamage = Math.Max(1, (int)(novaDamage * 0.5f));
                // v5.94 — ondas REDIMENSIONADAS (petición del usuario: eran
                // demasiado grandes — cubrían toda la pantalla): 360/450/540 →
                // 240/300/360. Sigue habiendo 3 frentes escalonados con daño
                // cada 0.1 s y quemadura de 10 s — solo que a escala justa.
                float[] radii = { 240f, 300f, 360f };
                for (int i = 0; i < 3; i++)
                {
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center.X, Projectile.Center.Y, 0f, 0f,
                        ModContent.ProjectileType<CosmicShockwaveProjectile>(),
                        waveDamage, 0f, Projectile.owner,
                        -i * 8f,                                      // edad: retardo escalonado
                        CosmicShockwaveProjectile.StyleFire,
                        radii[i]);                                    // radio máximo
                }

                // Daño AoE del núcleo de la nova (el epicentro de la explosión).
                // v5.94: 340 → 260 (proporcional a las ondas nuevas).
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.CanBeChasedBy()) continue;
                    float dist = (npc.Center - Projectile.Center).Length();
                    if (dist < 260f)
                    {
                        npc.SimpleStrikeNPC(novaDamage, npc.direction,
                            false, Projectile.knockBack, DamageClass.Magic);
                        npc.AddBuff(BuffID.OnFire, 600); // quemadura 10 s
                    }
                }
            }

            if (Main.netMode == NetmodeID.Server) return;

            // === NOVA MASIVA (segundo 10, sincronizada con la explosión de la Supernova) ===
            // Ráfaga principal con interpolación blanco→naranja
            ParticlePresets.Explosion(Projectile.Center, 170f, 40,
                new Color(255, 245, 200), new Color(255, 90, 20), 50);
            // DOBLE ONDA EXPANSIVA (dorada rápida + roja retardada)
            // v5.94 — tamaños proporcionales a las ondas nuevas (la librería
            // además ahora mide el radio REAL de la textura HD).
            ParticlePresets.RingPulse(Projectile.Center, 200f,
                new Color(255, 210, 100, 210), 34);
            ParticlePresets.RingPulse(Projectile.Center, 270f,
                new Color(255, 80, 30, 150), 46);
            // Ráfaga de viento solar radial de la librería
            for (int i = 0; i < 22; i++)
            {
                float angle = (MathHelper.TwoPi / 22) * i + Main.rand.NextFloat(-0.1f, 0.1f);
                Vector2 outward = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                var p = new ParticleData
                {
                    Position = Projectile.Center + outward * 30f,
                    Velocity = outward * Main.rand.NextFloat(3.5f, 7f),
                    Scale = new Vector2(2.2f, 0.5f),
                    Rotation = angle,
                    PackedColor = ParticleManager.PackColor(new Color(255, 240, 180, 190)),
                    PackedStartColor = ParticleManager.PackColor(new Color(255, 240, 180, 190)),
                    PackedEndColor = ParticleManager.PackColor(new Color(255, 90, 20, 20)),
                    TimeLeft = 42,
                    Duration = 42,
                    TextureId = ParticleTex.TrailGlow,
                    BlendMode = 1,
                    LayerPriority = LayerPriorities.BeforeProjectiles,
                };
                p.EnableComponent(ComponentFlag.FadeOut);
                p.EnableComponent(ComponentFlag.ColorShift);
                ParticleManager.Spawn(p);
            }

            // Screenshake coordinado
            try
            {
                Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(
                    Projectile.Center, new Vector2(1f, 0f), 8f, 12, 18, 0.45f,
                    "AethonSunNova"));
            }
            catch { }

            // === NOVA FINAL: explosión masiva de fuego (dusts, capa frontal) ===
            for (int i = 0; i < 60; i++)
            {
                float angle = (MathHelper.TwoPi / 60) * i;
                Vector2 dir = new Vector2(
                    (float)Math.Cos(angle) * Main.rand.NextFloat(6f, 14f),
                    (float)Math.Sin(angle) * Main.rand.NextFloat(6f, 14f));
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                    dir, 240, new Color(255, 200, 100), 1.7f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Chispas Torch en todas direcciones
            for (int i = 0; i < 35; i++)
            {
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch,
                    new Vector2((float)Math.Cos(angle) * Main.rand.NextFloat(4f, 9f),
                                (float)Math.Sin(angle) * Main.rand.NextFloat(4f, 9f)),
                    200, new Color(255, 150, 50), 1.4f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Núcleo de la nova: destellos encantados
            for (int i = 0; i < 20; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Gold,
                    new Vector2(Main.rand.NextFloat(-6f, 6f), Main.rand.NextFloat(-6f, 6f)),
                    255, new Color(255, 240, 180), 1.2f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Humo ascendente tras la explosión
            for (int i = 0; i < 15; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Smoke,
                    new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-4f, -1f)),
                    100, new Color(120, 70, 40), 1.0f);
                d.noGravity = false;
                d.fadeIn = 0f;
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item45, Projectile.Center);
        }

        /// <summary>
        /// v5.91 — Mata la Supernova hija EN EL MISMO TICK que el sol (si sigue
        /// viva): su OnKill visual (flash + estallido + viento) estalla
        /// EXACTAMENTE con la muerte del sol — sincronización perfecta POR
        /// CONSTRUCCIÓN, sin depender de índices ni clamps de timeLeft. La
        /// hija detecta el flag ai[2]=1 y NO genera ondas/AoE (el sol ya lo
        /// hizo): cero dobles explosiones.
        /// </summary>
        private void TryKillSupernova()
        {
            if (SupernovaIndex < 0f) return;
            int idx = (int)SupernovaIndex;
            if (idx >= 0 && idx < Main.maxProjectiles &&
                Main.projectile[idx].active &&
                Main.projectile[idx].type == ModContent.ProjectileType<V20.SupernovaProjectile>())
            {
                // Kill() dispara su OnKill AHORA — mismo tick que el sol.
                Main.projectile[idx].Kill();
            }
            SupernovaIndex = -1f;
        }

        /// <summary>Elastic ease-out (curva elástica de aparición).</summary>
        private static float ElasticOut(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            float c = (2f * (float)Math.PI) / 3f;
            return (float)(Math.Pow(2, -10 * t) * Math.Sin((t * 10 - 0.75) * c) + 1);
        }
    }
}
