using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Particles;
using AethonMod.Content.Projectiles.Cosmic;

namespace AethonMod.Content.Projectiles.V20
{
    /// <summary>
    /// SupernovaProjectile — estrella que colapsa durante 3 segundos y luego
    /// estalla en una supernova masiva (v5.85, reescrito).
    ///
    /// v5.91 — LA EXPLOSIÓN FINAL DEL SOL (SupernovaStaff): el SunProjectile
    /// la invoca en su segundo 7 con el flag ai[2]=1 (SunInvoked) y la mata
    /// EN SU MISMO TICK de muerte → sincronización POR CONSTRUCCIÓN. Cuando
    /// es hija del sol, esta supernova NO genera sus propias ondas/AoE (el
    /// OnKill del SOL es la autoridad: él genera las 3 ondas de fuego con daño
    /// cada 0.1 s + quemadura 10 s y el AoE del núcleo) — la hija aporta EL
    /// ESPECTÁCULO FINAL: flash blanco, estallido, viento estelar y temblor.
    /// Lanzada sola (SupernovaStaff, ai[2]=0) conserva su explosión COMPLETA.
    ///
    /// v5.91 — PARTÍCULAS DEL COLOR DEL SOL (petición del usuario): la carga ya
    /// NO se blanquea hacia el azul — el halo pasa de dorado (255, 200, 90) a
    /// BLANCO DORADO incandescente (255, 235, 115), el núcleo es blanco-dorado
    /// (255, 250, 215) y la luz se mantiene en la familia cálida de la estrella
    /// (amarillo-oro-naranja, como la corona del SunShader).
    ///
    /// CICLO (180 ticks = 3 segundos exactos):
    ///   - CARGA (0..180): contrae acelerando y se vuelve blanco-dorado,
    ///     atrae enemigos con fuerza CRECIENTE (0.5 → 2.2), genera GoldFlame
    ///     en espiral hacia dentro cada vez más rápido, y tiembla con
    ///     sacudidas de cámara que anticipan el estallido.
    ///   - ONKILL (tick 180): EXPLOSIÓN MASIVA (v5.86/v5.91):
    ///       * [solo standalone] 3 ONDAS EXPANSIVAS DE FUEGO (CosmicShockwaveProjectile
    ///         estilo 2, escalonadas): cada una BARRA dañando cada 0.1 s a medida
    ///         que avanza y aplica QUEMADURA (OnFire, 10 s) a los enemigos barridos.
    ///       * [solo standalone] AoE del núcleo de 340 px + quemadura 10 s.
    ///       * Flash blanco gigante + destello de destellos (SparkleStar)
    ///       * 70 lenguas de GoldFlame + 25 chispas blancas + brasas + humo
    ///       * Temblor de cámara fuerte (PunchCameraModifier)
    ///
    /// INTEGRACIÓN CON EL SOL (SunProjectile): el sol la invoca en su segundo 7,
    /// la mantiene centrada y ambos explotan SIMULTÁNEAMENTE en el segundo 10 —
    /// el sol mata a la nova en su propio OnKill (v5.91) y genera las ondas de
    /// fuego él mismo: la explosión final SIEMPRE sale sincronizada.
    /// La fuerza de succión durante la carga se suma a la gravedad creciente
    /// del propio sol → los enemigos son arrastrados al centro de la nova.
    /// </summary>
    public class SupernovaProjectile : ModProjectile
    {
        /// <summary>Duración total de la carga: 3 segundos.</summary>
        private const int ChargeDuration = 180;

        private float Age { get => Projectile.ai[0]; set => Projectile.ai[0] = value; }

        /// <summary>
        /// v5.91 — ¿Fue invocada por el sol? (ai[2] = 1). Las hijas del sol NO
        /// generan ondas/AoE al morir — el OnKill del SOL es la autoridad de la
        /// explosión final (las genera él, sincronizadas con su muerte). Las
        /// novas standalone (SupernovaStaff, ai[2] = 0) conservan la explosión
        /// completa con ondas y AoE propios.
        /// </summary>
        private bool SunInvoked => Projectile.ai[2] == 1f;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 80;
            Projectile.height = 80;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = ChargeDuration;
            Projectile.light = 0f;
            Projectile.alpha = 0;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
            Projectile.ignoreWater = true;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            try
            {
                Age += 1f;
                Projectile.velocity *= 0.92f;

                // Progreso de carga: 0 → 1 durante los 3 segundos (con aceleración final).
                float charge = MathHelper.Clamp(Age / ChargeDuration, 0f, 1f);
                // Ease-in cuadrático: los primeros instantes son calma, el final es frenesí.
                float chargeEased = charge * charge;

                // === CARGA: atracción de enemigos con fuerza creciente ===
                float pullRadius = 300f;
                float pullStrength = 0.5f + chargeEased * 1.7f; // 0.5 → 2.2
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.CanBeChasedBy()) continue;
                    Vector2 toCenter = Projectile.Center - npc.Center;
                    float dist = toCenter.Length();
                    if (dist > pullRadius || dist < 5f) continue;
                    float strength = (1f - dist / pullRadius) * pullStrength;
                    if (toCenter.LengthSquared() > 0.01f)
                    {
                        toCenter.Normalize();
                        npc.velocity += toCenter * strength;
                    }
                }

                // === CARGA: materia dorada en espiral hacia el núcleo ===
                if (Main.netMode != NetmodeID.Server)
                {
                    int spiralCount = 2 + (int)(chargeEased * 2f); // 2 → 4 por frame
                    for (int i = 0; i < spiralCount; i++)
                    {
                        float angle = Age * (0.18f + chargeEased * 0.14f) + i * MathHelper.Pi;
                        float maxDist = 110f - chargeEased * 30f;
                        float dist = maxDist * (1f - charge * 0.55f) + Main.rand.NextFloat(-8f, 8f);
                        Vector2 spawnPos = Projectile.Center + new Vector2(
                            (float)Math.Cos(angle) * dist,
                            (float)Math.Sin(angle) * dist);
                        Vector2 toCenter = Projectile.Center - spawnPos;
                        if (toCenter.LengthSquared() > 0.01f)
                        {
                            toCenter.Normalize();
                            Vector2 tangent = new Vector2(-toCenter.Y, toCenter.X);
                            float speed = 3.5f + chargeEased * 4.5f;
                            Vector2 vel = toCenter * speed + tangent * speed * 0.45f;
                            Dust d = Dust.NewDustPerfect(spawnPos, DustID.GoldFlame,
                                vel, 200, new Color(255, 220, 150), 1.1f);
                            d.noGravity = true;
                            d.fadeIn = 0f;
                        }
                    }

                    // Sacudidas anticipatorias: cada 40 ticks, cada vez más fuertes.
                    if (Age % 40f == 0f && Age > 20f)
                    {
                        try
                        {
                            float rumble = 1.5f + chargeEased * 4.5f;
                            Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(
                                Projectile.Center, new Vector2(1f, 0f), rumble, 6, 8, 0.3f,
                                "AethonSupernovaCharge"));
                        }
                        catch { }
                    }
                }

                // === CARGA: luz que se INTENSIFICA EN LA FAMILIA CÁLIDA DEL SOL ===
                // v5.91 — sin azul: la luz blanquea dentro de la paleta dorada
                // de la estrella (partículas del color del sol).
                float lightIntensity = 1f + chargeEased * 1.6f;
                Lighting.AddLight(Projectile.Center,
                    new Vector3(1f, 0.85f - 0.05f * chargeEased, 0.55f - 0.1f * chargeEased) * lightIntensity);
            }
            catch { }
        }

        // ================================================================
        //  EXPLOSIÓN MASIVA (OnKill, sincronizada con el sol en el segundo 10)
        // ================================================================
        public override void OnKill(int timeLeft)
        {
            // === v5.91 — HIJA DEL SOL: SOLO ESPECTÁCULO ===
            // Cuando el sol la mata (TryKillSupernova en el OnKill del sol),
            // ESTA nova aporta el flash + estallido + viento estelar — las ONDAS
            // DE FUEGO y el AoE del núcleo los genera EL SOL (autoridad de la
            // sincronización): cero dobles explosiones. Las novas standalone
            // (SupernovaStaff) conservan su explosión completa.
            if (!SunInvoked && Main.netMode != NetmodeID.MultiplayerClient)
            {
                // === v5.86/v5.91 — 3 ONDAS EXPANSIVAS DE FUEGO (solo standalone) ===
                // La onda final de la explosión: tres frentes ardientes
                // escalonados (retardo de 8 ticks) con triple anillo rojo/naranja/
                // amarillo y llamas a lo largo del frente. CADA UNA barre daño
                // de área cada 0.1 s a medida que avanza y aplica QUEMADURA
                // (OnFire, 10 s — v5.91: era 5 s).
                int waveDamage = Math.Max(1, (int)(Projectile.damage * 0.5f));
                float[] radii = { 360f, 450f, 540f };
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

                // Daño AoE del núcleo de la nova: solo en la autoridad
                // (standalone — la hija del sol deja este golpe al OnKill del sol).
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.CanBeChasedBy()) continue;
                    float dist = (npc.Center - Projectile.Center).Length();
                    if (dist < 340f)
                    {
                        npc.SimpleStrikeNPC(Projectile.damage, npc.direction,
                            false, Projectile.knockBack, DamageClass.Magic);
                        npc.AddBuff(BuffID.OnFire, 600); // quemadura 10 s (v5.91)
                    }
                }
            }

            if (Main.netMode == NetmodeID.Server) return;

            // === FLASH BLANCO GIGANTE (SoftGlow aditivo de corta vida) ===
            var flash = new ParticleData
            {
                Position = Projectile.Center,
                Velocity = Vector2.Zero,
                Scale = Vector2.One * 6.5f,
                PackedColor = ParticleManager.PackColor(new Color(255, 255, 245, 255)),
                PackedStartColor = ParticleManager.PackColor(new Color(255, 255, 245, 255)),
                TimeLeft = 14,
                Duration = 14,
                TextureId = ParticleTex.SoftGlow,
                BlendMode = 1,
                LayerPriority = LayerPriorities.AboveTiles,
            };
            flash.EnableComponent(ComponentFlag.FadeOut);
            flash.EnableComponent(ComponentFlag.ScaleDown);
            ParticleManager.Spawn(flash);

            // Ráfaga de núcleo: interpolación blanco → naranja profundo.
            ParticlePresets.Explosion(Projectile.Center, 200f, 46,
                new Color(255, 250, 220), new Color(255, 100, 30), 42);

            // === VIENTO ESTELAR: estelas radiales largas ===
            for (int i = 0; i < 26; i++)
            {
                float angle = (MathHelper.TwoPi / 26) * i + Main.rand.NextFloat(-0.08f, 0.08f);
                Vector2 outward = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                var p = new ParticleData
                {
                    Position = Projectile.Center + outward * 24f,
                    Velocity = outward * Main.rand.NextFloat(4.5f, 8.5f),
                    Scale = new Vector2(2.6f, 0.55f),
                    Rotation = angle,
                    PackedColor = ParticleManager.PackColor(new Color(255, 245, 200, 220)),
                    PackedStartColor = ParticleManager.PackColor(new Color(255, 245, 200, 220)),
                    PackedEndColor = ParticleManager.PackColor(new Color(255, 90, 20, 20)),
                    TimeLeft = 44,
                    Duration = 44,
                    TextureId = ParticleTex.TrailGlow,
                    BlendMode = 1,
                    LayerPriority = LayerPriorities.BeforeProjectiles,
                };
                p.EnableComponent(ComponentFlag.FadeOut);
                p.EnableComponent(ComponentFlag.ColorShift);
                ParticleManager.Spawn(p);
            }

            // === TEMBLOR DE CÁMARA FUERTE ===
            try
            {
                Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(
                    Projectile.Center, new Vector2(1f, 0f), 10f, 14, 22, 0.5f,
                    "AethonSupernovaBlast"));
            }
            catch { }

            // === DUSTS FRONTALES: 70 lenguas de fuego ===
            for (int i = 0; i < 70; i++)
            {
                float angle = (MathHelper.TwoPi / 70) * i + Main.rand.NextFloat(-0.1f, 0.1f);
                float speed = Main.rand.NextFloat(7f, 14f);
                Vector2 vel = new Vector2(
                    (float)Math.Cos(angle) * speed,
                    (float)Math.Sin(angle) * speed);
                Color color = Main.rand.NextBool(3)
                    ? new Color(255, 245, 190)
                    : new Color(255, 230, 150);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                    vel, 240, color, 1.8f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // 25 chispas blancas encantadas
            for (int i = 0; i < 25; i++)
            {
                Vector2 v = new Vector2(
                    Main.rand.NextFloat(-10f, 10f),
                    Main.rand.NextFloat(-10f, 10f));
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Gold,
                    v, 255, Color.White, 1.2f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // 18 brasas de fuego (con gravedad: llueven tras la nova)
            for (int i = 0; i < 18; i++)
            {
                float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch,
                    new Vector2((float)Math.Cos(angle) * Main.rand.NextFloat(4f, 8f),
                                (float)Math.Sin(angle) * Main.rand.NextFloat(4f, 8f)),
                    210, new Color(255, 160, 60), 1.5f);
                d.noGravity = false;
                d.fadeIn = 0f;
            }

            // 14 volutas de humo ascendentes
            for (int i = 0; i < 14; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Smoke,
                    new Vector2(Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-5f, -1f)),
                    110, new Color(130, 80, 50), 1.2f);
                d.noGravity = false;
                d.fadeIn = 0f;
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item45, Projectile.Center);
        }

        // ================================================================
        //  RENDER DE LA CARGA (contracción + blanco caliente + temblor final)
        // ================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                Texture2D softGlow = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                if (softGlow == null) return false;

                float charge = MathHelper.Clamp(Age / ChargeDuration, 0f, 1f);
                float chargeEased = charge * charge;

                // Temblor de anticipación en el último tramo de la carga.
                Vector2 jitter = Vector2.Zero;
                if (charge > 0.6f)
                {
                    float shake = (charge - 0.6f) / 0.4f;
                    jitter = new Vector2(
                        Main.rand.NextFloat(-1f, 1f) * shake * 2.2f,
                        Main.rand.NextFloat(-1f, 1f) * shake * 2.2f);
                }

                Vector2 drawPos = Projectile.Center - Main.screenPosition + jitter;
                Vector2 glowOrigin = new Vector2(softGlow.Width / 2f, softGlow.Height / 2f);
                // v5.94 — los ANILLOS DE CONTENCIÓN se eliminaron: los anillos
                // son parte de la ONDA EXPANSIVA final ("solo deben salir al
                // final", petición del usuario) y además la escala fija sobre
                // la textura HD dibujaba anillos de hasta 2458px. La carga se
                // expresa con el halo dorado condensándose + temblor creciente.

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                // === Halo contraído: de dorado (2.0) a BLANCO DORADO compacto (0.6) ===
                // v5.91 — partículas del color del sol: el halo ya NO se
                // desplaza al blanco-AZUL (255,255,245 de la v5.88) — se
                // condensa en BLANCO DORADO (255, 235, 115), la paleta cálida
                // de la corona del SunShader.
                float scale = 2.0f - chargeEased * 1.4f;
                int r = 255;
                int g = (int)(200 + 35 * chargeEased);  // 200 → 235 (dorado → dorado claro)
                int b = (int)(90 + 25 * chargeEased);   // 90 → 115 (sin azul)
                Color halo = new Color(r, g, b, 220);
                // Pulso creciente cerca del estallido.
                float pulse = 1f + (float)Math.Sin(Age * (0.25f + chargeEased * 0.5f)) * 0.06f * (1f + chargeEased * 2f);
                Main.spriteBatch.Draw(softGlow, drawPos, null, halo, 0f, glowOrigin, scale * pulse, SpriteEffects.None, 0f);

                // Núcleo BLANCO-DORADO que crece en proporción (la masa se condensa).
                float coreScale = 0.4f + chargeEased * 0.28f;
                Color core = new Color(255, 250, 215, 240);
                Main.spriteBatch.Draw(softGlow, drawPos, null, core, 0f, glowOrigin, coreScale * pulse, SpriteEffects.None, 0f);

                Main.spriteBatch.End();
            }
            catch
            {
                // v5.90 — cierre defensivo solo si una excepción cortó el Begin
                // (el try{End} incondicional de v5.88 disparaba una first-chance
                // cada frame que tML registraba como "Excepción silenciosa").
                try { Main.spriteBatch.End(); } catch { }
            }

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise,
                null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            try
            {
                // La materia estellar inflama al contacto (10 s: v5.91 — la
                // quemadura de la nova final acompaña a la de sus ondas).
                target.AddBuff(BuffID.OnFire, 600);
            }
            catch { }
        }
    }
}
