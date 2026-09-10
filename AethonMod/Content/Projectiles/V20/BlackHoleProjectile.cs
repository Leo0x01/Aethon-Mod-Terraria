using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.V20
{
    /// <summary>
    /// BlackHoleProjectile — agujero negro realista.
    ///
    /// Capas visuales (back → front):
    ///   1. Lensing noise (Noise.png, additive, muy sutil, rotación lenta)
    ///   2. Accretion disk BACK (Vortex.png, FlipVertically, rojo, offset up — Einstein ring)
    ///   3. Event horizon (GlowOrbWhite, Color.Black, AlphaBlend — disco negro opaco)
    ///   4. Accretion disk FRONT (Vortex.png, additive, naranja-white-hot, offset down)
    ///      + relativistic beaming highlight (lado izquierdo más brillante)
    ///   5. Photon ring (Ring.png, blanco, thin scale)
    ///   6. Chromatic aberration (3 copias RGB del photon ring, offset horizontal)
    ///
    /// Físicas:
    ///   - Partículas que caen en espiral hacia el centro (PurpleTorch + GoldFlame)
    ///   - Pull gravitacional sobre NPCs en 300px
    ///   - 2 segundos de vida, penetración infinita, sin colisión con tiles
    ///   - On hit / Kill: implosión de 40 partículas + explosión hacia afuera
    /// </summary>
    public class BlackHoleProjectile : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 48;
            Projectile.height = 48;
            Projectile.tileCollide = false;   // no tile collision
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;          // penetrates everything
            Projectile.timeLeft = 120;          // 2 seconds @ 60fps
            Projectile.light = 0f;              // we manage lighting manually
            Projectile.alpha = 0;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
            Projectile.ignoreWater = true;
        }

        public override bool? CanCutTiles() => false;

        // ================================================================
        //  AI — físicas, partículas, pull gravitacional, lighting
        // ================================================================
        public override void AI()
        {
            try
            {
                // Edad y rotación acumulada (la usa el draw si la necesitamos)
                Projectile.ai[0] += 1f;
                Projectile.rotation += 0.04f;

                // Damping — el agujero negro "deriva" en vez de volar como bala
                Projectile.velocity *= 0.98f;
                if (Projectile.velocity.Length() < 0.05f) Projectile.velocity = Vector2.Zero;

                // === Infalling particles ===
                if (Main.rand.NextBool(2))
                {
                    SpawnInfallingDust();
                }

                // === Pull gravitacional sobre NPCs hostiles cercanos ===
                PullNearbyNPCs(300f);

                // === Lighting ===
                // Bright orange desde el disco de acreción.
                // Terraria no soporta luz negativa (clamp a 0), así que el centro
                // se ve oscuro por el disco negro dibujado en PreDraw.
                Lighting.AddLight(Projectile.Center, new Vector3(0.95f, 0.45f, 0.10f));
                // Halo sutil púrpura alrededor (espacio-tiempo distorsionado)
                Lighting.AddLight(Projectile.Center + new Vector2(40, 0), new Vector3(0.20f, 0.05f, 0.30f));
                Lighting.AddLight(Projectile.Center + new Vector2(-40, 0), new Vector3(0.20f, 0.05f, 0.30f));
            }
            catch { }
        }

        // ================================================================
        //  Infalling dust — partículas que caen en espiral al centro
        // ================================================================
        private void SpawnInfallingDust()
        {
            // Spawn en ángulo aleatorio, 60-100px del centro
            float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
            float dist = Main.rand.NextFloat(60f, 100f);
            Vector2 spawnOffset = new Vector2((float)Math.Cos(angle) * dist, (float)Math.Sin(angle) * dist);
            Vector2 spawnPos = Projectile.Center + spawnOffset;

            // Vector hacia el centro + componente tangencial (espiral)
            Vector2 toCenter = -spawnOffset;
            if (toCenter.Length() < 0.1f) return;
            toCenter = toCenter.SafeNormalize(Vector2.Zero);
            // Componente tangencial: perpendicular a toCenter (giro horario)
            Vector2 tangent = new Vector2(-toCenter.Y, toCenter.X) * 0.45f;
            Vector2 vel = (toCenter * Main.rand.NextFloat(2.5f, 4.5f)) + tangent;

            // Alternar entre material púrpura (espacio-tiempo) y material caliente del disco
            bool hot = Main.rand.NextBool(2);
            int dustType = hot ? DustID.GoldFlame : DustID.PurpleTorch;
            Color dustColor = hot ? new Color(255, 200, 100) : new Color(180, 80, 255);
            float dustScale = Main.rand.NextFloat(0.9f, 1.4f);

            Dust d = Dust.NewDustPerfect(spawnPos, dustType, vel, 150, dustColor, dustScale);
            d.noGravity = true;
            d.fadeIn = 0f;
        }

        // ================================================================
        //  Pull gravitacional sobre NPCs
        // ================================================================
        private void PullNearbyNPCs(float radius)
        {
            try
            {
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.active) continue;
                    if (npc.friendly || npc.townNPC) continue;
                    if (npc.dontTakeDamage) continue;
                    if (!npc.CanBeChasedBy()) continue;

                    float dist = Vector2.Distance(npc.Center, Projectile.Center);
                    if (dist >= radius || dist < 4f) continue;

                    Vector2 toCenter = (Projectile.Center - npc.Center).SafeNormalize(Vector2.Zero);
                    // Más fuerte al acercarse al centro
                    float strength = (1f - dist / radius) * 1.4f;
                    npc.velocity += toCenter * strength;

                    // Limitar velocidad para evitar teleport / glitches
                    float maxSpeed = 14f;
                    if (npc.velocity.Length() > maxSpeed)
                        npc.velocity = npc.velocity.SafeNormalize(Vector2.Zero) * maxSpeed;

                    // Pequeño stun visual: dust que se arrastra con el NPC
                    if (Main.rand.NextBool(8))
                    {
                        Dust d = Dust.NewDustPerfect(npc.Center, DustID.PurpleTorch,
                            -toCenter * 1.5f, 150, new Color(180, 80, 255), 0.8f);
                        d.noGravity = true;
                        d.fadeIn = 0f;
                    }
                }
            }
            catch { }
        }

        // ================================================================
        //  OnHitNPC — implosión masiva + explosión hacia afuera
        // ================================================================
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            ImplosionExplosion(Projectile.Center);
        }

        // ================================================================
        //  Kill — mismo efecto si expira sin impacto
        // ================================================================
        public override void Kill(int timeLeft)
        {
            ImplosionExplosion(Projectile.Center);
        }

        private void ImplosionExplosion(Vector2 center)
        {
            try
            {
                // === IMPLOSIÓN: 40 partículas convergiendo al centro ===
                for (int i = 0; i < 40; i++)
                {
                    float angle = (MathHelper.TwoPi / 40f) * i + Main.rand.NextFloat(-0.2f, 0.2f);
                    float dist = Main.rand.NextFloat(70f, 130f);
                    Vector2 spawnPos = center + new Vector2((float)Math.Cos(angle) * dist, (float)Math.Sin(angle) * dist);
                    Vector2 toCenter = (center - spawnPos).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(5f, 9f);
                    bool hot = Main.rand.NextBool(2);
                    int type = hot ? DustID.GoldFlame : DustID.PurpleTorch;
                    Color c = hot ? new Color(255, 200, 100) : new Color(180, 80, 255);
                    Dust d = Dust.NewDustPerfect(spawnPos, type, toCenter, 200, c, 1.4f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }

                // === EXPLOSIÓN hacia afuera ===
                // Dorado-white-hot (material del disco)
                for (int i = 0; i < 30; i++)
                {
                    float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                    float speed = Main.rand.NextFloat(5f, 11f);
                    Vector2 vel = new Vector2((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed);
                    Dust d = Dust.NewDustPerfect(center, DustID.GoldFlame, vel, 220, new Color(255, 180, 50), 1.5f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
                // Púrpura (distorsión del espacio-tiempo)
                for (int i = 0; i < 22; i++)
                {
                    float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                    float speed = Main.rand.NextFloat(4f, 9f);
                    Vector2 vel = new Vector2((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed);
                    Dust d = Dust.NewDustPerfect(center, DustID.PurpleTorch, vel, 210, new Color(180, 80, 255), 1.3f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
                // Destello blanco central (supernova del colapso)
                for (int i = 0; i < 10; i++)
                {
                    Dust d = Dust.NewDustPerfect(center, DustID.Enchanted_Gold,
                        new Vector2(Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, 3f)),
                        255, new Color(255, 240, 200), 0.9f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }
            catch { }
        }

        // ================================================================
        //  PreDraw — todas las capas visuales custom
        //  Retorna FALSE: el motor no dibuja el sprite original.
        // ================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                Texture2D glowOrbWhite = ModContent.Request<Texture2D>("AethonMod/Content/Effects/GlowOrbWhite").Value;
                Texture2D ring = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Ring").Value;
                Texture2D vortex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Vortex").Value;
                Texture2D noise = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Noise").Value;

                if (glowOrbWhite == null || ring == null || vortex == null || noise == null)
                    return false;

                Vector2 drawPos = Projectile.Center - Main.screenPosition;
                float t = Main.GameUpdateCount;

                // ==========================================
                // (1) LENSING NOISE — distorsión del espacio-tiempo
                // ==========================================
                // Noise.png 128x128, escala grande, alpha muy bajo, rotación lenta.
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
                Main.spriteBatch.Draw(noise, drawPos, null,
                    new Color(80, 60, 100, 30),
                    t * 0.005f,
                    new Vector2(noise.Width / 2f, noise.Height / 2f),
                    3.5f, SpriteEffects.None, 0f);

                // Segunda capa de lensing con rotación opuesta para más detalle
                Main.spriteBatch.Draw(noise, drawPos, null,
                    new Color(60, 40, 80, 25),
                    -t * 0.003f,
                    new Vector2(noise.Width / 2f, noise.Height / 2f),
                    4.5f, SpriteEffects.None, 0f);

                // ==========================================
                // (2) ACCRETION DISK BACK — Einstein ring
                // ==========================================
                // Vortex con FlipVertically, color rojo tenue, offset up (4px)
                // → simula el "back" del disco curvado sobre el agujero negro
                float diskRotBack = t * 0.04f;
                Main.spriteBatch.Draw(vortex, drawPos + new Vector2(0, -4), null,
                    new Color(200, 50, 0, 100),
                    diskRotBack,
                    new Vector2(vortex.Width / 2f, vortex.Height / 2f),
                    new Vector2(2.0f, 0.6f), SpriteEffects.FlipVertically, 0f);

                // ==========================================
                // (3) EVENT HORIZON — disco negro opaco
                // ==========================================
                // GlowOrbWhite con Color.Black en AlphaBlend → disco negro sólido.
                // Pasamos a AlphaBlend para que el negro tape lo que está detrás.
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                Main.spriteBatch.Draw(glowOrbWhite, drawPos, null,
                    Color.Black,
                    0f,
                    new Vector2(glowOrbWhite.Width / 2f, glowOrbWhite.Height / 2f),
                    0.45f, SpriteEffects.None, 0f);

                // ==========================================
                // (4) ACCRETION DISK FRONT
                // ==========================================
                // Vortex normal, color naranja-white-hot, offset down (4px)
                // Rotación rápida en sentido opuesto al back.
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                float diskRotFront = -t * 0.06f;
                Vector2 vortexOrigin = new Vector2(vortex.Width / 2f, vortex.Height / 2f);
                Vector2 diskScale = new Vector2(2.0f, 0.6f);

                // Lado "front" completo — bright orange
                Main.spriteBatch.Draw(vortex, drawPos + new Vector2(0, 4), null,
                    new Color(255, 200, 100, 220),
                    diskRotFront, vortexOrigin, diskScale, SpriteEffects.None, 0f);

                // Relativistic beaming: lado izquierdo más brillante (offset -3px, alpha alto)
                Main.spriteBatch.Draw(vortex, drawPos + new Vector2(-3, 4), null,
                    new Color(255, 230, 150, 130),
                    diskRotFront, vortexOrigin, diskScale, SpriteEffects.None, 0f);

                // Lado derecho más tenue (rojo, alpha bajo) — para reforzar el beaming
                Main.spriteBatch.Draw(vortex, drawPos + new Vector2(3, 4), null,
                    new Color(200, 80, 30, 80),
                    diskRotFront, vortexOrigin, diskScale, SpriteEffects.None, 0f);

                // ==========================================
                // (5) PHOTON RING — anillo brillante fino
                // ==========================================
                // Ring.png blanco justo afuera del event horizon.
                Vector2 ringOrigin = new Vector2(ring.Width / 2f, ring.Height / 2f);
                Main.spriteBatch.Draw(ring, drawPos, null,
                    new Color(255, 255, 240, 220),
                    0f, ringOrigin, 0.55f, SpriteEffects.None, 0f);

                // ==========================================
                // (6) CHROMATIC ABERRATION — RGB split del photon ring
                // ==========================================
                // Red offset left, Green centered, Blue offset right.
                // Cada uno con alpha bajo (50-80) y additive blending.
                Main.spriteBatch.Draw(ring, drawPos + new Vector2(-3, 0), null,
                    new Color(255, 0, 0, 60),
                    0f, ringOrigin, 0.6f, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(ring, drawPos, null,
                    new Color(0, 255, 0, 80),
                    0f, ringOrigin, 0.6f, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(ring, drawPos + new Vector2(3, 0), null,
                    new Color(0, 0, 255, 70),
                    0f, ringOrigin, 0.6f, SpriteEffects.None, 0f);

                // Restaurar el spriteBatch al estado estándar (AlphaBlend Deferred)
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
            return false; // no dibujar sprite original
        }
    }
}
