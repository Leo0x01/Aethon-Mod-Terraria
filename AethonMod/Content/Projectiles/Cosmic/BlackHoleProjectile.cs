using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Particles;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// BlackHoleProjectile — agujero negro con lensing gravitacional real.
    ///
    /// RENDER: RealBlackHoleShader.fx (lightmarch de 75 pasos con lensing gravitacional
    /// real) sobre un canvas de InvisiblePixel de 256px:
    ///   - zoom dinámico: width / 256 * scale * 2
    ///   - accretionDiskRadius: scale * 0.4
    ///   - cameraRotationAxis: (velocity.Y * -0.022 + 1, 0, rotation)
    ///   - cameraAngle: 0.32 / accretionDiskScale: (1, 0.33, 1)
    ///
    /// v5.85 — LENTE GRAVITACIONAL de pantalla (BlackHoleLensSystem): el fondo
    /// real del juego se distorsiona alrededor del horizonte de sucesos con el
    /// shader BlackHoleDistortionShader (formalismo relativista con decaimiento
    /// exponencial). Fuerza gravitatoria aumentada y radio de atracción de 450px.
    ///
    /// v5.84 — Capa de partículas de la LIBRERÍA propia (data-oriented, additive,
    /// render en PostDrawTiles = capa de fondo con profundidad): espiral de succión
    /// multicolor (violeta/cian/magenta/oro) con ColorShift, disco de acreción de
    /// estelas TrailGlow orbitando (componente Orbit + rotación tangencial
    /// sincronizada), anillo de fotones pulsante con ScaleUp, halo de distorsión
    /// con ruido procedural, implosión/explosión con presets y screenshake.
    ///
    /// Mejoras propias: pop elástico de aparición, colapso final antes de expirar,
    /// succión espiral de partículas de colores, atracción gravitacional de
    /// enemigos Y devoración del polvo cercano, refuerzo del event horizon e
    /// implosión + doble onda expansiva al morir.
    /// </summary>
    public class BlackHoleProjectile : ModProjectile
    {
        private Ref<Effect> _shader;
        private bool _shaderFailed;

        /// <summary>Tiempo visual de vida — usada para el pop elástico de aparición.</summary>
        public ref float VisualsTime => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            // v5.85: área de daño ampliada (76 → 96): el hitbox y el canvas del
            // shader escalan con width, así que el agujero también se ve mayor.
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
            // scale = ElasticOut(0..120) * sqrt(InverseLerp(0..60)) — el agujero "rebota" al nacer.
            Projectile.scale = ElasticOut(Utils.GetLerpValue(0f, 120f, VisualsTime, true)) *
                               (float)Math.Sqrt(Utils.GetLerpValue(0f, 60f, VisualsTime, true));
            VisualsTime += 1f;

            // === COLAPSO FINAL: los últimos 40 ticks se encoge dramáticamente ===
            if (Projectile.timeLeft < 40f)
                Projectile.scale *= 0.93f;

            // === MOVIMIENTO: deriva lenta y frenado (el agujero flota) ===
            Projectile.velocity *= 0.97f;

            // Rotación suave hacia velocity.X * 0.04
            float targetRotation = Projectile.velocity.X * 0.04f;
            Projectile.rotation += MathHelper.WrapAngle(targetRotation - Projectile.rotation) * 0.3f;

            // === PARTÍCULAS (solo cliente) ===
            if (Main.netMode != NetmodeID.Server)
            {
                // Dusts vanilla (capa frontal, se dibujan encima del canvas del shader)
                SpawnSuctionParticles();
                SpawnAccretionDiskParticles();
                SpawnSmokeParticles();
                SpawnCapturedEnergySparks();
                AttractNearbyDust();

                // v5.84: partículas de la librería propia (capa de fondo aditiva —
                // se renderizan en PostDrawTiles, detrás del canvas del agujero,
                // creando profundidad por capas)
                SpawnLibrarySuctionSpiral();
                SpawnLibraryAccretionDisk();
                SpawnLibraryPhotonRing();
                SpawnLibraryDistortionHalo();
            }

            // === ATRACCIÓN GRAVITACIONAL DE ENEMIGOS (radio 450, fuerza aumentada) ===
            const float gravityRadius = 450f;
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

            // === ILUMINACIÓN PULSANTE (naranja del disco + toque púrpura) ===
            float pulse = 0.8f + 0.2f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5f);
            Lighting.AddLight(Projectile.Center, new Vector3(0.95f * pulse, 0.45f * pulse, 0.15f * pulse));
        }

        // ------------------------------------------------------------------
        //  PARTÍCULAS
        // ------------------------------------------------------------------

        /// <summary>Partículas cayendo en espiral hacia el centro, con paleta multicolor.</summary>
        private void SpawnSuctionParticles()
        {
            for (int i = 0; i < 3; i++)
            {
                float angle = Projectile.rotation * 1.5f + i * (MathHelper.TwoPi / 3f);
                float dist = 100f + 55f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2f + i);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist,
                    (float)Math.Sin(angle) * dist);

                Vector2 toCenter = Projectile.Center - spawnPos;
                // Más rápido cuanto más cerca del horizonte
                float speed = 4f + 4f * (1f - dist / 155f);
                if (toCenter.LengthSquared() > 0.01f)
                {
                    toCenter.Normalize();
                    Vector2 velocity = toCenter * speed;

                    // RotateTowards: convierte la caída radial en espiral
                    float angleToCenter = (float)Math.Atan2(toCenter.Y, toCenter.X);
                    velocity = velocity.RotateTowards(angleToCenter + MathHelper.PiOver2 * 0.3f, 0.5f);

                    // v5.85 — SUCCIÓN ESPIRAL MULTICOLOR: la materia devorada
                    // cubre el espectro (violeta, cian, magenta, oro) y se vuelve
                    // incandescente al acercarse al horizonte.
                    Color color;
                    if (dist < 60f)
                    {
                        color = new Color(255, 240, 180); // incandescente al borde
                    }
                    else
                    {
                        switch (Main.rand.Next(4))
                        {
                            case 0: color = new Color(160, 80, 255); break;  // violeta
                            case 1: color = new Color(80, 200, 255); break;  // cian
                            case 2: color = new Color(255, 80, 220); break;  // magenta
                            default: color = new Color(255, 200, 80); break; // oro
                        }
                    }

                    Dust d = Dust.NewDustPerfect(spawnPos, DustID.PurpleTorch,
                        velocity, 150, color, 1.3f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                    d.scale = Main.rand.NextFloat(0.8f, 1.6f);
                }
            }
        }

        /// <summary>Disco de acreción: GoldFlame orbitando en un plano aplanado.</summary>
        private void SpawnAccretionDiskParticles()
        {
            if (Main.rand.NextBool(2))
            {
                float diskAngle = Projectile.rotation * 4f;
                float diskRadius = Main.rand.NextFloat(10f, 40f);
                Vector2 diskPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(diskAngle) * diskRadius,
                    (float)Math.Sin(diskAngle) * diskRadius * 0.25f);
                Vector2 tangent = new Vector2(
                    -(float)Math.Sin(diskAngle),
                    (float)Math.Cos(diskAngle) * 0.25f) * 3f;
                Dust d = Dust.NewDustPerfect(diskPos, DustID.GoldFlame,
                    tangent, 220, new Color(255, 210, 100), 1.2f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        /// <summary>Humo púrpura siendo absorbido desde los alrededores.</summary>
        private void SpawnSmokeParticles()
        {
            if (Main.rand.NextBool(5))
            {
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(120f, 180f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist,
                    (float)Math.Sin(angle) * dist);
                Vector2 vel = (Projectile.Center - spawnPos) * 0.025f;
                Dust d = Dust.NewDustPerfect(spawnPos, DustID.Smoke,
                    vel, 80, new Color(80, 40, 100), 0.8f);
                d.noGravity = false;
                d.fadeIn = 0f;
            }
        }

        /// <summary>Chispas doradas encantadas capturadas por el campo gravitatorio.</summary>
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
                Dust d = Dust.NewDustPerfect(spawnPos, DustID.Enchanted_Gold,
                    vel, 255, new Color(255, 230, 150), 0.9f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        /// <summary>
        /// Efecto gravitacional sobre el polvo del ambiente: los dusts cercanos
        /// son atraídos hacia el horizonte de sucesos, como si el agujero devorase el entorno.
        /// </summary>
        private void AttractNearbyDust()
        {
            // v5.85: radio de devoración ampliado (190 → 260): el agujero devora
            // el polvo del entorno en un área mucho mayor.
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
                // Componente tangencial sutil → espiral
                Vector2 pull = toCenter * strength + new Vector2(-toCenter.Y, toCenter.X) * strength * 0.35f;
                d.velocity += pull;
            }
        }

        // ------------------------------------------------------------------
        //  v5.84 — PARTÍCULAS DE LA LIBRERÍA PROPIA (capa de fondo aditiva)
        //  Sistema data-oriented de Content/Particles (libro de referencia,
        //  secciones 10-16): additive blending + ColorShift + Orbit + ScaleUp.
        // ------------------------------------------------------------------

        /// <summary>
        /// Espiral de succión multicolor con partículas SoftGlow aditivas: nacen
        /// en el borde del campo gravitatorio con velocidad tangencial + radial y
        /// caen en espiral. v5.85: cada partícula nace de un color cósmico
        /// distinto (violeta/cian/magenta/oro) y muere hacia el blanco-violeta
        /// del horizonte.
        /// </summary>
        private void SpawnLibrarySuctionSpiral()
        {
            for (int i = 0; i < 2; i++)
            {
                float angle = Projectile.rotation * 1.5f + VisualsTime * 0.021f + i * MathHelper.Pi;
                float dist = Main.rand.NextFloat(115f, 165f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist,
                    (float)Math.Sin(angle) * dist);

                // Velocidad: tangente (órbita) + componente hacia el centro → espiral natural
                Vector2 tangent = new Vector2(-(float)Math.Sin(angle), (float)Math.Cos(angle));
                Vector2 toCenter = -new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                Vector2 velocity = tangent * 2.1f + toCenter * 1.05f;

                // Color de nacimiento: uno del espectro cósmico.
                Color start = Main.rand.Next(4) switch
                {
                    0 => new Color(190, 120, 255, 190), // violeta
                    1 => new Color(110, 210, 255, 190), // cian
                    2 => new Color(255, 110, 230, 190), // magenta
                    _ => new Color(255, 220, 130, 190), // oro
                };

                var p = new ParticleData
                {
                    Position = spawnPos,
                    Velocity = velocity,
                    Scale = Vector2.One * Main.rand.NextFloat(0.7f, 1.1f),
                    PackedColor = ParticleManager.PackColor(start),
                    PackedStartColor = ParticleManager.PackColor(start),
                    PackedEndColor = ParticleManager.PackColor(new Color(150, 70, 255, 60)),
                    TimeLeft = 45,
                    Duration = 45,
                    TextureId = ParticleTex.SoftGlow,
                    BlendMode = 1, // Additive
                    LayerPriority = LayerPriorities.BeforeProjectiles,
                };
                p.EnableComponent(ComponentFlag.FadeOut);
                p.EnableComponent(ComponentFlag.ColorShift);
                p.EnableComponent(ComponentFlag.ScaleDown);
                ParticleManager.Spawn(p);
            }
        }

        /// <summary>
        /// Disco de acreción con estelas TrailGlow orbitando: usa el componente Orbit
        /// (centro/radio/velocidad angular) con la rotación sincronizada al mismo
        /// ritmo (RotationSpeed = angVel) para que las estelas queden siempre
        /// alineadas tangencialmente, como materia caliente arremolinada.
        /// </summary>
        private void SpawnLibraryAccretionDisk()
        {
            if (Main.rand.NextBool(4))
            {
                float radius = Main.rand.NextFloat(26f, 46f) * MathHelper.Max(Projectile.scale, 0.4f);
                float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                float angVel = 0.22f; // rad/tick — el disco gira rápido

                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * radius,
                    (float)Math.Sin(angle) * radius);

                var p = new ParticleData
                {
                    Position = spawnPos,
                    Velocity = Vector2.Zero,
                    Scale = new Vector2(1.8f, 0.45f), // estirada tangencialmente (TrailGlow 32x8)
                    Rotation = angle + MathHelper.PiOver2, // alineada a la tangente
                    RotationSpeed = angVel, // sincronizada con la órbita → siempre tangencial
                    PackedColor = ParticleManager.PackColor(new Color(255, 190, 90, 200)),
                    PackedStartColor = ParticleManager.PackColor(new Color(255, 210, 120, 200)),
                    PackedEndColor = ParticleManager.PackColor(new Color(255, 80, 20, 40)),
                    TimeLeft = 48,
                    Duration = 48,
                    TextureId = ParticleTex.TrailGlow,
                    BlendMode = 1,
                    LayerPriority = LayerPriorities.BeforeProjectiles,
                };
                // Orbit: UserData0/1 = centro, UserData2 = velocidad angular, UserData3 = radio
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

        /// <summary>
        /// Anillo de fotones pulsante: Ring con ScaleUp + FadeIn + FadeOut — un destello
        /// circular en el horizonte de sucesos que se expande y desvanece.
        /// </summary>
        private void SpawnLibraryPhotonRing()
        {
            if (VisualsTime % 36f == 0f && VisualsTime > 20f && Projectile.scale > 0.3f)
            {
                var p = new ParticleData
                {
                    Position = Projectile.Center,
                    Velocity = Vector2.Zero,
                    Scale = Vector2.One,
                    PackedColor = ParticleManager.PackColor(new Color(200, 220, 255, 170)),
                    PackedStartColor = ParticleManager.PackColor(new Color(200, 220, 255, 170)),
                    TimeLeft = 30,
                    Duration = 30,
                    TextureId = ParticleTex.Ring,
                    BlendMode = 1,
                    LayerPriority = LayerPriorities.BeforeProjectiles,
                };
                p.UserData1 = 1.15f * Projectile.scale; // escala final X
                p.UserData2 = 1.15f * Projectile.scale; // escala final Y
                p.EnableComponent(ComponentFlag.ScaleUp);
                p.EnableComponent(ComponentFlag.FadeOut);
                ParticleManager.Spawn(p);
            }
        }

        /// <summary>
        /// Halo de distorsión: ruido procedural rotando lentamente con alpha muy bajo
        /// y tinte violeta — sugiere la curvatura del espacio alrededor del agujero.
        /// </summary>
        private void SpawnLibraryDistortionHalo()
        {
            if (Main.rand.NextBool(18))
            {
                var p = new ParticleData
                {
                    Position = Projectile.Center,
                    Velocity = Vector2.Zero,
                    Scale = Vector2.One * 2.6f * MathHelper.Max(Projectile.scale, 0.5f),
                    Rotation = Main.rand.NextFloat(0f, MathHelper.TwoPi),
                    RotationSpeed = Main.rand.NextFloat(-0.02f, 0.02f),
                    PackedColor = ParticleManager.PackColor(new Color(140, 90, 200, 26)),
                    PackedStartColor = ParticleManager.PackColor(new Color(140, 90, 200, 26)),
                    TimeLeft = 60,
                    Duration = 60,
                    TextureId = ParticleTex.Noise,
                    BlendMode = 1,
                    LayerPriority = LayerPriorities.AboveTiles,
                };
                p.EnableComponent(ComponentFlag.FadeOut);
                ParticleManager.Spawn(p);
            }
        }

        // ------------------------------------------------------------------
        //  RENDER
        // ------------------------------------------------------------------

        public override bool PreDraw(ref Color lightColor)
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
                Vector2 drawPos = Projectile.Center - Main.screenPosition;

                // === 1. HALO PÚRPURA EXTERIOR (aura cósmica de fondo) ===
                Texture2D glowTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                float haloPulse = 0.85f + 0.15f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3.5f);
                Main.spriteBatch.Draw(glowTex, drawPos, null,
                    new Color(60, 20, 90, 40) * haloPulse * Projectile.scale, 0f,
                    glowTex.Size() * 0.5f, 3.2f * Projectile.scale, SpriteEffects.None, 0f);
                Main.spriteBatch.End();

                if (_shader != null && _shader.Value != null)
                {
                    // === 2. REALBLACKHOLESHADER — lensing gravitacional de 75 pasos ===
                    Effect shader = _shader.Value;

                    // Canvas de 256px (tamaño nativo del lightmarch)
                    float targetSize = 256f;
                    float resizingScale = Projectile.width / targetSize * Projectile.scale * 2f;

                    shader.Parameters["blackHoleRadius"].SetValue(0.3f);
                    shader.Parameters["blackHoleCenter"].SetValue(Vector3.Zero);
                    shader.Parameters["aspectRatioCorrectionFactor"].SetValue(1f);
                    shader.Parameters["accretionDiskColor"].SetValue(new Color(245, 105, 61).ToVector3());
                    shader.Parameters["cameraAngle"].SetValue(0.32f);
                    shader.Parameters["cameraRotationAxis"].SetValue(new Vector3(Projectile.velocity.Y * -0.022f + 1f, 0f, Projectile.rotation));
                    shader.Parameters["accretionDiskScale"].SetValue(new Vector3(1f, 0.33f, 1f));
                    shader.Parameters["zoom"].SetValue(Vector2.One * resizingScale);
                    shader.Parameters["accretionDiskRadius"].SetValue(Projectile.scale * 0.4f);
                    shader.Parameters["globalTime"].SetValue(Main.GlobalTimeWrappedHourly);

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
                        pixel.Size() * 0.5f, targetSize, SpriteEffects.None, 0f);
                    Main.spriteBatch.End();

                    // === 3. REFUERZO DEL EVENT HORIZON ===
                    // radio del horizonte en píxeles = blackHoleRadius * zoom * (canvas / 2)
                    float eventHorizonPx = 0.3f * resizingScale * targetSize * 0.5f;
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
                }
                else
                {
                    // === FALLBACK: dibujado manual si el shader no carga ===
                    DrawFallback(drawPos);
                }
            }
            catch { }

            RestoreSpriteBatch();
            return false;
        }

        /// <summary>Restaura el SpriteBatch al estado que tML espera tras PreDraw.</summary>
        private static void RestoreSpriteBatch()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise,
                null, Main.Transform);
        }

        /// <summary>Dibujado manual de respaldo (vórtice + anillo de fotones + aberración cromática).</summary>
        private void DrawFallback(Vector2 drawPos)
        {
            float pulse = 0.9f + 0.1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4f);
            Texture2D glowTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
            Texture2D vortexTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Vortex").Value;
            Texture2D ringTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Ring").Value;
            float s = Projectile.scale;

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);

            // Disco de acreción frontal (elíptico, naranja)
            Main.spriteBatch.Draw(vortexTex, drawPos, null,
                new Color(255, 180, 80, 200), Projectile.rotation * 2f,
                vortexTex.Size() * 0.5f, new Vector2(1.5f, 0.5f) * s, SpriteEffects.None, 0f);

            // Disco trasero (anillo de Einstein)
            Main.spriteBatch.Draw(vortexTex, drawPos - new Vector2(0f, 4f * s), null,
                new Color(200, 50, 0, 100), -Projectile.rotation * 2f,
                vortexTex.Size() * 0.5f, new Vector2(1.5f, 0.5f) * s, SpriteEffects.FlipVertically, 0f);

            // Beaming relativístico
            Main.spriteBatch.Draw(vortexTex, drawPos - new Vector2(3f * s, 0f), null,
                new Color(255, 230, 150, 130), Projectile.rotation * 2f,
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

            // Anillo de fotones + aberración cromática
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(ringTex, drawPos - new Vector2(2f * s, 0f), null,
                new Color(255, 0, 0, 80), 0f, ringTex.Size() * 0.5f,
                0.6f * pulse * s, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(ringTex, drawPos, null,
                new Color(0, 255, 0, 80), 0f, ringTex.Size() * 0.5f,
                0.6f * pulse * s, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(ringTex, drawPos + new Vector2(2f * s, 0f), null,
                new Color(0, 100, 255, 80), 0f, ringTex.Size() * 0.5f,
                0.6f * pulse * s, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(ringTex, drawPos, null,
                new Color(255, 240, 200, 220), 0f, ringTex.Size() * 0.5f,
                0.6f * pulse * s, SpriteEffects.None, 0f);
            Main.spriteBatch.End();
        }

        // ------------------------------------------------------------------
        //  IMPACTO Y MUERTE
        // ------------------------------------------------------------------

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode == NetmodeID.Server) return;

            // v5.84: micro-colapso de la librería sobre el objetivo
            ParticlePresets.Implosion(target.Center, 70f, 16, new Color(200, 100, 255), 18);
            ParticlePresets.RingPulse(target.Center, 90f, new Color(220, 180, 255, 170), 22);

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
                    Dust d = Dust.NewDustPerfect(spawnPos, DustID.PurpleTorch,
                        vel, 200, new Color(200, 100, 255), 1.3f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }

            // Explosión: 40 GoldFlame radiales
            for (int i = 0; i < 40; i++)
            {
                float angle = (MathHelper.TwoPi / 40) * i;
                Vector2 dir = new Vector2(
                    (float)Math.Cos(angle) * Main.rand.NextFloat(5f, 11f),
                    (float)Math.Sin(angle) * Main.rand.NextFloat(5f, 11f));
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                    dir, 220, new Color(255, 200, 100), 1.5f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Destellos encantados
            for (int i = 0; i < 15; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Gold,
                    new Vector2(Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-4f, 4f)),
                    255, Color.White, 1.0f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.Server) return;

            // === v5.84: PRESETS DE LA LIBRERÍA — colapso gravitatorio completo ===
            // Implosión: la materia visible colapsa hacia el singularity
            ParticlePresets.Implosion(Projectile.Center, 165f, 46,
                new Color(190, 90, 255), 26);
            // Explosión: liberación de energía del colapso
            ParticlePresets.Explosion(Projectile.Center, 130f, 28,
                new Color(255, 240, 200), new Color(255, 120, 40), 40);
            // Onda expansiva: anillo violeta + anillo dorado retardado
            ParticlePresets.RingPulse(Projectile.Center, 250f,
                new Color(180, 100, 255, 200), 32);
            ParticlePresets.RingPulse(Projectile.Center, 320f,
                new Color(255, 200, 100, 140), 44);

            // Screenshake coordinado (sección 8.5 del libro)
            try
            {
                Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(
                    Projectile.Center, new Vector2(1f, 0f), 7f, 10, 20, 0.4f,
                    "AethonBlackHoleCollapse"));
            }
            catch { }

            // === COLAPSO FINAL: implosión + explosión (dusts, capa frontal) ===
            // Implosión: partículas convergiendo
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
                    Dust d = Dust.NewDustPerfect(spawnPos, DustID.PurpleTorch,
                        toCenter * 9f + tangent * 5f, 220, new Color(190, 90, 255), 1.4f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }

            // Explosión: anillo expansivo de GoldFlame + Torch púrpura
            for (int i = 0; i < 45; i++)
            {
                float angle = (MathHelper.TwoPi / 45) * i;
                Vector2 dir = new Vector2(
                    (float)Math.Cos(angle) * Main.rand.NextFloat(6f, 13f),
                    (float)Math.Sin(angle) * Main.rand.NextFloat(6f, 13f));
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                    dir, 230, new Color(255, 200, 100), 1.6f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
            for (int i = 0; i < 20; i++)
            {
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch,
                    new Vector2((float)Math.Cos(angle) * 3f, (float)Math.Sin(angle) * 3f),
                    200, new Color(160, 80, 255), 1.2f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item88, Projectile.Center);
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

    /// <summary>Extensiones vectoriales para las partículas en espiral.</summary>
    public static class Vector2Extensions
    {
        /// <summary>Rota el vector hacia el ángulo objetivo como máximo maxStep radianes, conservando la magnitud.</summary>
        public static Vector2 RotateTowards(this Vector2 current, float targetAngle, float maxStep)
        {
            float currentAngle = (float)Math.Atan2(current.Y, current.X);
            float diff = ((targetAngle - currentAngle + MathHelper.Pi * 3) % MathHelper.TwoPi) - MathHelper.Pi;
            if (Math.Abs(diff) <= maxStep)
                return new Vector2((float)Math.Cos(targetAngle), (float)Math.Sin(targetAngle)) * current.Length();
            float newAngle = currentAngle + Math.Sign(diff) * maxStep;
            return new Vector2((float)Math.Cos(newAngle), (float)Math.Sin(newAngle)) * current.Length();
        }
    }
}
