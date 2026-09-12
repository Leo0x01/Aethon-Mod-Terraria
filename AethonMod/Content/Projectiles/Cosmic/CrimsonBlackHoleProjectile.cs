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
    /// CrimsonBlackHoleProjectile — v6.02 — EL AGUJERO NEGRO CARMESÍ.
    ///
    /// Copia con personalidad propia del BlackHoleProjectile (que queda
    /// INTACTO): misma física probada (aura de daño con ticks que aceleran
    /// cerca del centro, atracción 10× la del sol, devora balas enemigas al
    /// cruzar el horizonte, persecución lenta, anillo de Einstein final) con
    /// un visual de reina cósmica:
    ///   - Disco de acreción MAGENTA (el shader toma el color por parámetro).
    ///   - Anillo de fotones ROSA-INCANDESCENTE fino sobre el horizonte.
    ///   - CORONA DE ARCOs: 5 lazos de neón carmesí→magenta arqueados sobre
    ///     el anillo de fotones, con nudos NARANJA incandescentes en los
    ///     ápices (líneas de campo magnético solidificadas en luz).
    ///   - Halo carmesí profundo y materia absorbida rosa/carmesí.
    /// </summary>
    public class CrimsonBlackHoleProjectile : ModProjectile
    {
        /// <summary>Shader del núcleo (mismo asset que el agujero original:
        /// el color del disco y los ángulos son parámetros).</summary>
        private static Ref<Effect> _shader;
        private static bool _shaderFailed;

        /// <summary>Tiempo visual de vida — pop elástico de aparición.</summary>
        public ref float VisualsTime => ref Projectile.ai[0];

        /// <summary>Multiplicador del radio del campo sobre el horizonte.</summary>
        private const float ShieldRadiusMult = 2.2f;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
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
            // === POP ELÁSTICO DE APARICIÓN ===
            Projectile.scale = ElasticOut(Utils.GetLerpValue(0f, 120f, VisualsTime, true)) *
                               (float)Math.Sqrt(Utils.GetLerpValue(0f, 60f, VisualsTime, true));
            VisualsTime += 1f;

            // === SECUENCIA DE MUERTE ===
            // FASE 1 — CRECIMIENTO (t-90..t-36): el horizonte se hincha.
            float expansion = 0f;
            if (Projectile.timeLeft > 36f && Projectile.timeLeft <= 90f)
            {
                expansion = 1f - (Projectile.timeLeft - 36f) / 54f;
                Projectile.scale *= 1f + expansion * 0.6f;
            }
            else if (Projectile.timeLeft <= 36f)
            {
                // FASE 2 — EVAPORACIÓN: colapso hacia la singularidad. El radio
                // del campo se captura UNA VEZ con la escala pre-colapso.
                if (Projectile.localAI[1] <= 0f)
                    Projectile.localAI[1] = 0.3f * Projectile.width *
                                            Math.Max(Projectile.scale, 0.08f) * ShieldRadiusMult;

                float collapse = Utils.GetLerpValue(36f, 0f, Projectile.timeLeft, true);
                Projectile.scale *= 1f - collapse;
                expansion = 1f;
            }

            // La materia absorbida ACELERA hacia el centro al evaporarse.
            ParticleManager.PullToGlobalBoost = 1f + expansion * 5f;

            // === MOVIMIENTO: deriva lenta y frenado ===
            Projectile.velocity *= 0.97f;

            // === PERSIGUE LIGERAMENTE A LOS ENEMIGOS (deriva amenazante) ===
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

            // === PARTÍCULAS (solo cliente) — paleta carmesí/magenta ===
            if (Main.netMode != NetmodeID.Server)
            {
                SpawnAbsorbedDusts();
                SpawnCapturedEnergySparks();
                AttractNearbyDust();
                SpawnLibraryAbsorbedMatter();
                SpawnLibraryAccretionDisk();
                SpawnCrownEmbers();
            }

            // === ATRACCIÓN GRAVITACIONAL DE ENEMIGOS (10× la del sol) ===
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

                    // ¿Cruzó el horizonte? → ABSORBIDA en chispas rosas.
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

            // === AURA DE DAÑO: TICKS QUE ACELERAN CERCA DEL CENTRO ===
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

            // === ILUMINACIÓN PULSANTE (carmesí-magenta del disco) ===
            float pulse = 0.8f + 0.2f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5f);
            Lighting.AddLight(Projectile.Center, new Vector3(1.0f * pulse, 0.16f * pulse, 0.34f * pulse));
        }

        /// <summary>El enemigo chaseable más cercano dentro de maxDist.</summary>
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
        /// escala pre-colapso durante la evaporación.</summary>
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
        //  PARTÍCULAS — paleta carmesí / magenta / rosa
        // ------------------------------------------------------------------

        /// <summary>Materia absorbida (dusts): polvo carmesí/magenta cayendo
        /// en espiral, blanco-rosa al rozar el horizonte.</summary>
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

                    // Materia carmesí que se vuelve rosa-incandescente al caer.
                    Color color;
                    if (dist < 60f)
                    {
                        color = new Color(255, 205, 225); // blanco-rosa al borde
                    }
                    else
                    {
                        color = Main.rand.Next(3) switch
                        {
                            0 => new Color(255, 30, 70),   // carmesí
                            1 => new Color(255, 60, 150),  // magenta
                            _ => new Color(220, 20, 110),  // granate
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

        /// <summary>
        /// ASCUAS DE LA CORONA: chispas rosas que se alzan sobre los ápices
        /// de los arcos (la corona es energía VIVA, no un adorno estático).
        /// </summary>
        private void SpawnCrownEmbers()
        {
            if (Main.rand.NextBool(18))
            {
                // Horizonte aproximado en px (mismo cálculo del dibujado).
                float horizonPx = 0.3f * Projectile.width * Math.Max(Projectile.scale, 0.08f);
                // Punto sobre la corona (entre el anillo y el ápice exterior).
                Vector2 emberPos = Projectile.Center + new Vector2(
                    Main.rand.NextFloat(-1.3f, 1.3f) * horizonPx,
                    -(horizonPx * (1.1f + Main.rand.NextFloat(0.8f, 2.2f))));
                Dust d = Dust.NewDustPerfect(emberPos, DustID.Enchanted_Pink,
                    new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), -Main.rand.NextFloat(0.7f, 1.5f)),
                    180, new Color(255, 175, 215), 0.8f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        /// <summary>Devora el polvo del ambiente en espiral.</summary>
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

        /// <summary>Materia absorbida: estelas TrailGlow carmesí→rosa que
        /// caen en espiral hacia el horizonte y mueren devoradas.</summary>
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

                Color start = new Color(255, 40, 95, 190);
                Color end = new Color(255, 215, 235, 235);

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

        /// <summary>Disco de acreción: estelas TrailGlow magenta orbitando
        /// con rotación sincronizada (materia carmesí arremolinada).</summary>
        private void SpawnLibraryAccretionDisk()
        {
            if (Main.rand.NextBool(4))
            {
                float radius = Main.rand.NextFloat(26f, 46f) * MathHelper.Max(Projectile.scale, 0.4f);
                float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                float angVel = 0.22f;

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
                    PackedColor = ParticleManager.PackColor(new Color(255, 60, 130, 200)),
                    PackedStartColor = ParticleManager.PackColor(new Color(255, 90, 160, 200)),
                    PackedEndColor = ParticleManager.PackColor(new Color(120, 0, 45, 40)),
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
        //  RENDER
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
        /// Dibuja el núcleo completo del agujero carmesí: halo profundo +
        /// RealBlackHoleShader (disco magenta, cámara más de canto) +
        /// horizonte negro + ANILLO DE FOTONES rosa + CORONA DE ARCOS.
        /// Compartido entre el pase del mundo y el pase posterior a la lente.
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

                // === 1. HALO CARMESÍ PROFUNDO (aura de fondo) ===
                Texture2D glowTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                if (endActiveBatch)
                    Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                float haloPulse = 0.85f + 0.15f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3.5f);
                Main.spriteBatch.Draw(glowTex, drawPos, null,
                    new Color(48, 0, 18, 44) * haloPulse * p.scale, 0f,
                    glowTex.Size() * 0.5f, 3.2f * p.scale, SpriteEffects.None, 0f);
                Main.spriteBatch.End();

                // Radio del horizonte en píxeles (mismo cálculo que el original).
                float targetSize = 256f;
                float zoomBase = p.width / targetSize * 2f;

                if (_shader != null && _shader.Value != null)
                {
                    // === 2. REALBLACKHOLESHADER — disco MAGENTA, cámara más de canto ===
                    Effect shader = _shader.Value;
                    float canvasPx = targetSize * Math.Max(p.scale, 0.08f);

                    shader.Parameters["blackHoleRadius"].SetValue(0.3f);
                    shader.Parameters["blackHoleCenter"].SetValue(Vector3.Zero);
                    shader.Parameters["aspectRatioCorrectionFactor"].SetValue(1f);
                    // v6.02 — LA FIRMA CARMESÍ: disco de acreción magenta eléctrico.
                    shader.Parameters["accretionDiskColor"].SetValue(new Color(255, 0, 85).ToVector3());
                    // Cámara más inclinada: el disco casi de canto (más dramático).
                    shader.Parameters["cameraAngle"].SetValue(0.42f);
                    shader.Parameters["cameraRotationAxis"].SetValue(new Vector3(p.velocity.Y * -0.022f + 1f, 0f, p.rotation));
                    // Toro algo más plano y algo más grueso: disco de reina prominente.
                    shader.Parameters["accretionDiskScale"].SetValue(new Vector3(1f, 0.30f, 1f));
                    shader.Parameters["zoom"].SetValue(Vector2.One * zoomBase);
                    shader.Parameters["accretionDiskRadius"].SetValue(Math.Min(p.scale, 1f) * 0.44f);
                    shader.Parameters["globalTime"].SetValue(Main.GlobalTimeWrappedHourly);

                    Texture2D fireNoise = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Textures/FireNoiseB").Value;
                    Main.graphics.GraphicsDevice.Textures[1] = fireNoise;
                    Main.graphics.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;

                    Texture2D pixel = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Textures/InvisiblePixel").Value;

                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                        SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone,
                        null, Main.GameViewMatrix.TransformationMatrix);
                    shader.CurrentTechnique.Passes[0].Apply();
                    Main.spriteBatch.Draw(pixel, drawPos, null, Color.White, 0f,
                        pixel.Size() * 0.5f, canvasPx, SpriteEffects.None, 0f);
                    Main.spriteBatch.End();

                    // === 3. REFUERZO DEL EVENT HORIZON (negro absoluto) ===
                    float eventHorizonPx = 0.3f * zoomBase * canvasPx * 0.5f;
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

                        // === 4. ANILLO DE FOTONES ROSA-INCANDESCENTE ===
                        // Fino, CALIENTE (blanco-rosa #FFBB90 con halo #FF8A93):
                        // el punto más brillante del agujero, justo fuera del borde.
                        Texture2D ringTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Ring").Value;
                        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                            null, Main.GameViewMatrix.TransformationMatrix);
                        float ringPulse = 0.9f + 0.1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4.5f);
                        float photonR = eventHorizonPx * 1.12f;
                        float photonScale = photonR / (ringTex.Width * 0.5f);
                        // halo ancho rosa
                        Main.spriteBatch.Draw(ringTex, drawPos, null,
                            new Color(255, 105, 140, 110) * ringPulse, 0f, ringTex.Size() * 0.5f,
                            photonScale * 1.06f, SpriteEffects.None, 0f);
                        // núcleo fino blanco-rosa
                        Main.spriteBatch.Draw(ringTex, drawPos, null,
                            new Color(255, 200, 190, 235) * ringPulse, 0f, ringTex.Size() * 0.5f,
                            photonScale * 0.98f, SpriteEffects.None, 0f);
                        Main.spriteBatch.End();

                        // === 5. LA CORONA DE LA REINA — 5 ARCOS DE NEÓN ===
                        DrawCoronaCrown(drawPos, eventHorizonPx);
                    }
                }
                else
                {
                    // === FALLBACK sin shader ===
                    DrawFallback(p, drawPos);
                }
            }
            catch
            {
                // Cierre defensivo SOLO en el path de error.
                try { Main.spriteBatch.End(); } catch { }
            }
        }

        /// <summary>
        /// LA CORONA: lazos de neón carmesí→magenta arqueados sobre el anillo
        /// de fotones, con ASIMETRÍA por lazo (líneas de campo curvadas, no un
        /// arcoíris), un ECO interior más tenue por lazo (filamentos
        /// encajados) y NUDOS NARANJA con destello de 4 puntas en los ápices.
        /// Respiran con el tiempo y se dibujan aditivamente.
        /// </summary>
        private static void DrawCoronaCrown(Vector2 drawPos, float horizonPx)
        {
            if (horizonPx < 3f) return;

            Texture2D glowTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
            float time = Main.GlobalTimeWrappedHourly;

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);

            const int Loops = 5;
            const int Segments = 18;

            for (int l = 0; l < Loops; l++)
            {
                float t01 = l / (float)(Loops - 1);            // 0 exterior → 1 interior
                float breathe = 1f + 0.06f * (float)Math.Sin(time * 2.2f + l * 1.7f);
                // v6.02 — ASIMETRÍA por lazo (semianchos izquierdo/derecho
                // distintos y balanceo determinista por índice): las líneas de
                // campo se curvan, no forman un arcoíris simétrico.
                float sway = 0.18f * (float)Math.Sin(time * 0.9f + l * 2.6f);
                float halfWL = horizonPx * (1.55f - 0.30f * t01) * (1f - sway) * breathe;
                float halfWR = horizonPx * (1.55f - 0.30f * t01) * (1f + sway) * breathe;
                float apexH = horizonPx * (2.30f - 0.95f * t01) * breathe;
                float baseY = drawPos.Y - horizonPx * 1.06f;

                // El lazo completo: media elipse superior ASIMÉTRICA por puntos
                // de glow con grosor variable (fino en las bases, corpulento al
                // subir, afilado en el ápice — materia incandescente).
                for (int s = 0; s <= Segments; s++)
                {
                    float ang = s / (float)Segments * MathHelper.Pi;   // 0..π
                    float edge = (float)Math.Sin(ang);                 // 0 bases, 1 ápice
                    float side = (float)Math.Cos(ang);                 // -1 izq → +1 der
                    float halfW = side < 0f ? halfWL : halfWR;
                    Vector2 pos = new Vector2(
                        drawPos.X - (float)Math.Cos(ang) * halfW,
                        baseY - edge * apexH);

                    Color col = Color.Lerp(new Color(255, 23, 56), new Color(254, 25, 242), edge);
                    // Grosor: crece hacia arriba, afila en el ápice (chispa final).
                    float thickness = 0.16f + 0.13f * edge * (1f - 0.35f * edge);
                    float intensity = 0.30f + 0.70f * edge;
                    float pointPx = horizonPx * thickness;
                    float scale = pointPx / glowTex.Width * 2f;

                    Main.spriteBatch.Draw(glowTex, pos, null,
                        col * intensity, 0f, glowTex.Size() * 0.5f,
                        scale, SpriteEffects.None, 0f);
                }

                // ECO interior: un filamento más tenue y fino encajado dentro
                // del lazo (los "sigilos de fuego" — filamentos encajados).
                for (int s = 1; s < Segments; s++)
                {
                    float ang = s / (float)Segments * MathHelper.Pi;
                    float edge = (float)Math.Sin(ang);
                    float side = (float)Math.Cos(ang);
                    float halfW = (side < 0f ? halfWL : halfWR) * 0.66f;
                    Vector2 pos = new Vector2(
                        drawPos.X - (float)Math.Cos(ang) * halfW,
                        baseY - edge * apexH * 0.72f);
                    Color col = Color.Lerp(new Color(255, 40, 80), new Color(255, 60, 190), edge);
                    float scale = (horizonPx * 0.09f) / glowTex.Width * 2f;
                    Main.spriteBatch.Draw(glowTex, pos, null,
                        col * 0.55f, 0f, glowTex.Size() * 0.5f,
                        scale, SpriteEffects.None, 0f);
                }

                // NUDO NARANJA en el ápice: englobado + DESTELLO DE 4 PUNTAS
                // (dos glows estirados en cruz) + chispa blanca central.
                float knotPulse = 0.85f + 0.30f * (float)Math.Sin(time * 3.1f + l * 2.3f);
                Vector2 apex = new Vector2(drawPos.X, baseY - apexH);
                Main.spriteBatch.Draw(glowTex, apex, null,
                    new Color(255, 138, 60) * knotPulse, 0f, glowTex.Size() * 0.5f,
                    (horizonPx * 0.34f) / glowTex.Width * 2f, SpriteEffects.None, 0f);
                // destello de 4 puntas: dos elipses estiradas en cruz
                float flareLen = horizonPx * 0.85f * knotPulse;
                Main.spriteBatch.Draw(glowTex, apex, null,
                    new Color(255, 170, 90) * knotPulse * 0.85f, 0f, glowTex.Size() * 0.5f,
                    new Vector2(flareLen / glowTex.Width, (horizonPx * 0.10f) / glowTex.Width),
                    SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(glowTex, apex, null,
                    new Color(255, 170, 90) * knotPulse * 0.85f, 0f, glowTex.Size() * 0.5f,
                    new Vector2((horizonPx * 0.10f) / glowTex.Width, flareLen / glowTex.Width),
                    SpriteEffects.None, 0f);
                // chispa blanca central
                Main.spriteBatch.Draw(glowTex, apex, null,
                    new Color(255, 240, 230) * knotPulse * 0.9f, 0f, glowTex.Size() * 0.5f,
                    (horizonPx * 0.14f) / glowTex.Width * 2f, SpriteEffects.None, 0f);
            }

            Main.spriteBatch.End();
        }

        /// <summary>Radio del campo (px), compartido con el aura y el OnKill.</summary>
        internal static float GetShieldRadius(Projectile p)
        {
            if (p.localAI[1] > 4f)
                return p.localAI[1];
            return 0.3f * p.width * Math.Max(p.scale, 0.08f) * ShieldRadiusMult;
        }

        /// <summary>Restaura el SpriteBatch al estado que tML espera tras PreDraw.</summary>
        private static void RestoreSpriteBatch()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise,
                null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>Dibujado manual de respaldo (paleta carmesí).</summary>
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

            // Disco de acreción frontal (magenta)
            Main.spriteBatch.Draw(vortexTex, drawPos, null,
                new Color(255, 60, 140, 200), p.rotation * 2f,
                vortexTex.Size() * 0.5f, new Vector2(1.5f, 0.5f) * s, SpriteEffects.None, 0f);

            // Disco trasero (anillo de Einstein)
            Main.spriteBatch.Draw(vortexTex, drawPos - new Vector2(0f, 4f * s), null,
                new Color(160, 0, 60, 100), -p.rotation * 2f,
                vortexTex.Size() * 0.5f, new Vector2(1.5f, 0.5f) * s, SpriteEffects.FlipVertically, 0f);

            // Beaming relativístico
            Main.spriteBatch.Draw(vortexTex, drawPos - new Vector2(3f * s, 0f), null,
                new Color(255, 190, 220, 130), p.rotation * 2f,
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

            // Anillo de fotones rosa
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
            float horizonPx = 0.3f * p.width * Math.Max(p.scale, 0.08f);
            float photonR = horizonPx * 1.3f * pulse / 0.92f;
            float photonScale = photonR / (ringTex.Width * 0.5f);
            Main.spriteBatch.Draw(ringTex, drawPos, null,
                new Color(255, 60, 130, 200), 0f, ringTex.Size() * 0.5f,
                photonScale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(ringTex, drawPos, null,
                new Color(255, 200, 190, 220), 0f, ringTex.Size() * 0.5f,
                photonScale, SpriteEffects.None, 0f);
            Main.spriteBatch.End();

            // Corona de arcos también en el fallback
            if (horizonPx > 3f)
                DrawCoronaCrown(drawPos, horizonPx);
        }

        // ------------------------------------------------------------------
        //  IMPACTO Y MUERTE
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
            // agujero original.
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
