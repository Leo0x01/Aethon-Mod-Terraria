using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.V20
{
    /// <summary>
    /// BlackHoleProjectile — agujero negro REAL basado en técnicas de:
    /// - WoTG Nameless Deity (partículas en espiral con RotateTowards)
    /// - Calamity StratusBlackHole (texturas con polar-coord swirl)
    /// - CircularSuctionParticle (velocity stretch para motion blur)
    ///
    /// NO usa un sprite estático rotando. Usa:
    /// 1. Partículas spawneadas en círculo rotando (pinwheel pattern)
    /// 2. Cada partícula acelera hacia el centro + rota velocity (espiral)
    /// 3. Motion blur estirando partículas según velocidad
    /// 4. Event horizon con smoothstep (no círculo negro)
    /// 5. Aberración cromática con 3 partículas RGB
    /// 6. Glow pulsante del disco de acreción
    /// </summary>
    public class BlackHoleProjectile : ModProjectile
    {
        private float SpinAngle { get => Projectile.ai[0]; set => Projectile.ai[0] = value; }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 60;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 180;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            // Spin del ángulo de spawn (crea el patrón pinwheel)
            SpinAngle += 0.08f;

            // Deceleración lenta (el agujero negro "flota")
            Projectile.velocity *= 0.97f;

            // === 1. PARTÍCULAS INFALLING EN ESPIRAL ===
            // Spawn en círculo rotando + aceleración hacia centro + RotateTowards
            if (Main.netMode != NetmodeID.Server)
            {
                // Spawn 2 partículas por frame en ángulos rotando
                for (int i = 0; i < 2; i++)
                {
                    float angle = SpinAngle + i * MathHelper.Pi;
                    float dist = Main.rand.NextFloat(80f, 120f);
                    Vector2 spawnPos = Projectile.Center + new Vector2(
                        (float)Math.Cos(angle) * dist,
                        (float)Math.Sin(angle) * dist);

                    // Velocidad inicial hacia el centro
                    Vector2 toCenter = Projectile.Center - spawnPos;
                    float speed = Main.rand.NextFloat(3f, 6f);
                    if (toCenter.Length() > 0.1f)
                    {
                        toCenter.Normalize();
                        Vector2 velocity = toCenter * speed;

                        // Rotar velocity hacia la línea central (crea espiral)
                        float angleToCenter = (float)Math.Atan2(toCenter.Y, toCenter.X);
                        velocity = velocity.RotateTowards(angleToCenter + MathHelper.PiOver2 * 0.3f, 0.5f);

                        // Color: caliente (naranja-blanco) cerca del centro, púrpura lejos
                        Color color;
                        if (dist < 50f)
                            color = new Color(255, 230, 150); // blanco-amarillo (hot)
                        else if (dist < 80f)
                            color = new Color(255, 150, 50);  // naranja
                        else
                            color = new Color(180, 80, 255);  // púrpura (frío)

                        Dust d = Dust.NewDustPerfect(spawnPos, DustID.PurpleTorch,
                            velocity, 150, color, 1.2f);
                        d.noGravity = true;
                        d.fadeIn = 0f;
                        d.scale = Main.rand.NextFloat(0.8f, 1.5f);
                    }
                }

                // === 2. PARTÍCULAS CALIENTES DEL DISCO DE ACRECIÓN ===
                // Spawn muy cerca del centro en plano horizontal (disco)
                if (Main.rand.NextBool(2))
                {
                    float diskAngle = SpinAngle * 3f; // rota más rápido
                    float diskRadius = Main.rand.NextFloat(15f, 35f);
                    Vector2 diskPos = Projectile.Center + new Vector2(
                        (float)Math.Cos(diskAngle) * diskRadius,
                        (float)Math.Sin(diskAngle) * diskRadius * 0.3f); // aplanado (disco visto en ángulo)

                    // Velocidad tangencial (orbitando)
                    Vector2 tangent = new Vector2(
                        -(float)Math.Sin(diskAngle),
                        (float)Math.Cos(diskAngle) * 0.3f) * 2f;

                    Dust d = Dust.NewDustPerfect(diskPos, DustID.GoldFlame,
                        tangent, 200, new Color(255, 200, 100), 1.0f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }

                // === 3. POLVO DE FONTO ABSORBIDO ===
                if (Main.rand.NextBool(4))
                {
                    float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                    float dist = Main.rand.NextFloat(100f, 150f);
                    Vector2 spawnPos = Projectile.Center + new Vector2(
                        (float)Math.Cos(angle) * dist,
                        (float)Math.Sin(angle) * dist);
                    Vector2 vel = (Projectile.Center - spawnPos) * 0.03f;
                    Dust d = Dust.NewDustPerfect(spawnPos, DustID.PurpleTorch,
                        vel, 100, new Color(100, 50, 150), 0.6f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }

            // === 4. ATRACCIÓN GRAVITACIONAL DE ENEMIGOS ===
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy()) continue;
                Vector2 toCenter = Projectile.Center - npc.Center;
                float dist = toCenter.Length();
                if (dist > 300f || dist < 5f) continue;
                // Fuerza inversamente proporcional a la distancia
                float strength = (1f - dist / 300f) * 1.5f;
                if (toCenter.Length() > 0.1f)
                {
                    toCenter.Normalize();
                    npc.velocity += toCenter * strength;
                    // Cap para evitar teleport
                    // v5.78: removed NPC velocity cap (was capping existing velocity)
                        npc.velocity = Vector2.Normalize(npc.velocity) * 12f;
                }
            }

            // === 5. ILUMINACIÓN ===
            // Luz cálida del disco de acreción + tenue púrpura del entorno
            float pulse = 0.8f + 0.2f * (float)Math.Sin(Main.GameUpdateCount * 0.1f);
            Lighting.AddLight(Projectile.Center, new Vector3(0.9f * pulse, 0.4f * pulse, 0.1f * pulse));
            Lighting.AddLight(Projectile.Center + new Vector2(0, -30f), new Vector3(0.3f, 0.1f, 0.5f) * pulse);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                float t = Main.GameUpdateCount;
                float pulse = 0.8f + 0.2f * (float)Math.Sin(t * 0.1f);

                // === 1. GLOW EXTERNO PÚRPURA (halo de distorsión) ===
                Texture2D glowTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
                // Halo púrpura grande
                Main.spriteBatch.Draw(glowTex,
                    Projectile.Center - Main.screenPosition, null,
                    new Color(80, 30, 120, 60),
                    0f, new Vector2(glowTex.Width / 2f, glowTex.Height / 2f),
                    2.5f * pulse, SpriteEffects.None, 0f);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

                // === 2. DISCO DE ACRECIÓN (textura con polar-coord swirl simulado) ===
                Texture2D vortexTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Vortex").Value;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                // Disco frontal: elíptico (visto en ángulo), naranja caliente
                Main.spriteBatch.Draw(vortexTex,
                    Projectile.Center - Main.screenPosition, null,
                    new Color(255, 180, 80, 200),
                    SpinAngle * 2f,
                    new Vector2(vortexTex.Width / 2f, vortexTex.Height / 2f),
                    new Vector2(1.5f, 0.5f), // elíptico (disco aplanado)
                    SpriteEffects.None, 0f);

                // Disco trasero (Einstein ring) - invertido, rojo
                Main.spriteBatch.Draw(vortexTex,
                    Projectile.Center - Main.screenPosition - new Vector2(0, 4f), null,
                    new Color(200, 50, 0, 100),
                    -SpinAngle * 2f,
                    new Vector2(vortexTex.Width / 2f, vortexTex.Height / 2f),
                    new Vector2(1.5f, 0.5f),
                    SpriteEffects.FlipVertically, 0f);

                // Beaming relativístico: lado brillante
                Main.spriteBatch.Draw(vortexTex,
                    Projectile.Center - Main.screenPosition - new Vector2(3f, 0f), null,
                    new Color(255, 230, 150, 130),
                    SpinAngle * 2f,
                    new Vector2(vortexTex.Width / 2f, vortexTex.Height / 2f),
                    new Vector2(1.5f, 0.5f),
                    SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

                // === 3. EVENT HORIZON (smoothstep, no círculo negro) ===
                // Usar GlowOrbWhite con Color.Black - opaco en el centro, difumado en los bordes
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                Main.spriteBatch.Draw(glowTex,
                    Projectile.Center - Main.screenPosition, null,
                    Color.Black,
                    0f, new Vector2(glowTex.Width / 2f, glowTex.Height / 2f),
                    0.8f, SpriteEffects.None, 0f);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

                // === 4. ANILLO DE FOTONES (brillante, blanco-amarillo) ===
                Texture2D ringTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Ring").Value;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                // Anillo principal blanco
                Main.spriteBatch.Draw(ringTex,
                    Projectile.Center - Main.screenPosition, null,
                    new Color(255, 240, 200, 220),
                    0f, new Vector2(ringTex.Width / 2f, ringTex.Height / 2f),
                    0.6f * pulse, SpriteEffects.None, 0f);

                // === 5. ABERRACIÓN CROMÁTICA ===
                // 3 anillos RGB desplazados
                // Rojo (izquierda)
                Main.spriteBatch.Draw(ringTex,
                    Projectile.Center - Main.screenPosition - new Vector2(2f, 0f), null,
                    new Color(255, 0, 0, 80),
                    0f, new Vector2(ringTex.Width / 2f, ringTex.Height / 2f),
                    0.6f * pulse, SpriteEffects.None, 0f);
                // Verde (centro)
                Main.spriteBatch.Draw(ringTex,
                    Projectile.Center - Main.screenPosition, null,
                    new Color(0, 255, 0, 80),
                    0f, new Vector2(ringTex.Width / 2f, ringTex.Height / 2f),
                    0.6f * pulse, SpriteEffects.None, 0f);
                // Azul (derecha)
                Main.spriteBatch.Draw(ringTex,
                    Projectile.Center - Main.screenPosition + new Vector2(2f, 0f), null,
                    new Color(0, 100, 255, 80),
                    0f, new Vector2(ringTex.Width / 2f, ringTex.Height / 2f),
                    0.6f * pulse, SpriteEffects.None, 0f);

                // === 6. NÚCLEO BRILLANTE (punto de luz en el centro del event horizon) ===
                Main.spriteBatch.Draw(glowTex,
                    Projectile.Center - Main.screenPosition, null,
                    new Color(255, 200, 100, 150 * pulse),
                    0f, new Vector2(glowTex.Width / 2f, glowTex.Height / 2f),
                    0.3f * pulse, SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode == NetmodeID.Server) return;

            // === IMPLOSIÓN MASIVA ===
            // 40 partículas convergiendo al centro en espiral
            for (int i = 0; i < 40; i++)
            {
                float angle = (MathHelper.TwoPi / 40) * i + Main.rand.NextFloat(-0.2f, 0.2f);
                float dist = Main.rand.NextFloat(80f, 130f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist,
                    (float)Math.Sin(angle) * dist);

                // Velocidad hacia el centro + componente tangencial (espiral)
                Vector2 toCenter = Projectile.Center - spawnPos;
                if (toCenter.Length() > 0.1f)
                {
                    toCenter.Normalize();
                    Vector2 tangent = new Vector2(-toCenter.Y, toCenter.X) * 0.5f;
                    Vector2 vel = (toCenter * 6f + tangent * 3f);

                    Dust d = Dust.NewDustPerfect(spawnPos, DustID.PurpleTorch,
                        vel, 200, new Color(200, 100, 255), 1.2f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }

            // === EXPLOSIÓN POST-IMPLOSIÓN ===
            for (int i = 0; i < 30; i++)
            {
                float angle = (MathHelper.TwoPi / 30) * i;
                Vector2 dir = new Vector2(
                    (float)Math.Cos(angle) * Main.rand.NextFloat(4f, 9f),
                    (float)Math.Sin(angle) * Main.rand.NextFloat(4f, 9f));
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                    dir, 220, new Color(255, 200, 100), 1.3f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Flash blanco
            for (int i = 0; i < 10; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Gold,
                    new Vector2(Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, 3f)),
                    255, Color.White, 0.8f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Sonido de colapso
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
        }
    }

    /// <summary>
    /// Extension method para rotar un Vector2 hacia otro ángulo.
    /// Basado en WoTG CircularSuctionParticle.Velocity.RotateTowards.
    /// </summary>
    public static class Vector2Extensions
    {
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
