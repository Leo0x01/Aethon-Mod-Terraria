using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Particles;

namespace AethonMod.Content.Projectiles.V20
{
    /// <summary>
    /// SupernovaProjectile — estrella que colapsa durante 3 segundos y luego
    /// estalla en una supernova masiva (v5.85, reescrito).
    ///
    /// CICLO (180 ticks = 3 segundos exactos):
    ///   - CARGA (0..180): contrae acelerando y se vuelve blanco-azulado,
    ///     atrae enemigos con fuerza CRECIENTE (0.5 → 2.2), genera GoldFlame
    ///     en espiral hacia dentro cada vez más rápido, y tiembla con
    ///     sacudidas de cámara que anticipan el estallido.
    ///   - ONKILL (tick 180): EXPLOSIÓN MASIVA mejorada:
    ///       * DOBLE onda expansiva (blanca-dorada veloz + naranja profunda retardada)
    ///       * Flash blanco gigante + destello de destellos (SparkleStar)
    ///       * 70 lenguas de GoldFlame + 25 chispas blancas + brasas + humo
    ///       * Daño AoE real en 340px (SimpleStrikeNPC) + OnFire
    ///       * Temblor de cámara fuerte (PunchCameraModifier)
    ///
    /// INTEGRACIÓN CON EL SOL (SunProjectile): el sol lo invoca en su segundo 7,
    /// lo mantiene centrado y ambos explotan SIMULTÁNEAMENTE en el segundo 10.
    /// La fuerza de succión durante la carga se suma a la gravedad creciente
    /// del propio sol → los enemigos son arrastrados al centro de la nova.
    /// </summary>
    public class SupernovaProjectile : ModProjectile
    {
        /// <summary>Duración total de la carga: 3 segundos.</summary>
        private const int ChargeDuration = 180;

        private float Age { get => Projectile.ai[0]; set => Projectile.ai[0] = value; }

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

                // === CARGA: luz que se blanquea e intensifica ===
                float lightIntensity = 1f + chargeEased * 1.6f;
                Lighting.AddLight(Projectile.Center,
                    new Vector3(1f, 0.9f - 0.15f * chargeEased, 0.6f - 0.35f * chargeEased) * lightIntensity);
            }
            catch { }
        }

        // ================================================================
        //  EXPLOSIÓN MASIVA (OnKill, sincronizada con el sol en el segundo 10)
        // ================================================================
        public override void OnKill(int timeLeft)
        {
            // Daño AoE: solo en la autoridad (servidor / singleplayer).
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.CanBeChasedBy()) continue;
                    float dist = (npc.Center - Projectile.Center).Length();
                    if (dist < 340f)
                    {
                        npc.SimpleStrikeNPC(Projectile.damage, npc.direction,
                            false, Projectile.knockBack, DamageClass.Magic);
                        npc.AddBuff(BuffID.OnFire, 300);
                    }
                }
            }

            if (Main.netMode == NetmodeID.Server) return;

            // === DOBLE ONDA EXPANSIVA DE LA LIBRERÍA ===
            // Onda 1: blanca-dorada, veloz y agresiva.
            ParticlePresets.RingPulse(Projectile.Center, 320f,
                new Color(255, 245, 200, 230), 22);
            // Onda 2: naranja profunda, más ancha y retardada.
            ParticlePresets.RingPulse(Projectile.Center, 460f,
                new Color(255, 120, 40, 160), 44);

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
                Texture2D ring = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Ring").Value;
                if (softGlow == null || ring == null) return false;

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
                Vector2 ringOrigin = new Vector2(ring.Width / 2f, ring.Height / 2f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                // === Halo contraído: de dorado (2.0) a blanco-azulado compacto (0.6) ===
                float scale = 2.0f - chargeEased * 1.4f;
                // El color se desplaza de oro a blanco puro.
                int r = 255;
                int g = (int)(180 + 75 * chargeEased);
                int b = (int)(80 + 165 * chargeEased);
                Color halo = new Color(r, g, b, 220);
                // Pulso creciente cerca del estallido.
                float pulse = 1f + (float)Math.Sin(Age * (0.25f + chargeEased * 0.5f)) * 0.06f * (1f + chargeEased * 2f);
                Main.spriteBatch.Draw(softGlow, drawPos, null, halo, 0f, glowOrigin, scale * pulse, SpriteEffects.None, 0f);

                // Núcleo blanco-caliente que crece en proporción (la masa se condensa).
                float coreScale = 0.4f + chargeEased * 0.28f;
                Color core = new Color(255, 255, 255, 240);
                Main.spriteBatch.Draw(softGlow, drawPos, null, core, 0f, glowOrigin, coreScale * pulse, SpriteEffects.None, 0f);

                // === Anillos de contención pulsantes (la estrella luchando por no colapsar) ===
                if (charge > 0.25f)
                {
                    float ringPhase = (Age % 24f) / 24f;
                    float ringScale = (0.8f + ringPhase * 1.6f) * (0.6f + charge * 0.5f);
                    byte ringAlpha = (byte)(160 * (1f - ringPhase) * charge);
                    Main.spriteBatch.Draw(ring, drawPos, null,
                        new Color(255, 220, 140, ringAlpha), 0f, ringOrigin, ringScale, SpriteEffects.None, 0f);
                }

                Main.spriteBatch.End();
            }
            catch { }

            // Restaurar el SpriteBatch al estado esperado por tML.
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise,
                null, Main.Transform);
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            try
            {
                // La materia estelar inflama al contacto.
                target.AddBuff(BuffID.OnFire, 300);
            }
            catch { }
        }
    }
}
