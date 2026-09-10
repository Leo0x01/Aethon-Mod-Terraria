using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.V20
{
    /// <summary>
    /// GravityWellProjectile — pozo gravitacional que atrae enemigos.
    ///
    /// Visuales:
    ///   - Vortex.png rotando lentamente, color púrpura oscuro
    ///   - Dust espiral hacia el centro
    ///
    /// Físicas:
    ///   - velocity *= 0.95 (el proyectil "deriva" en su sitio)
    ///   - Pull sobre NPCs dentro de 250px
    ///   - timeLeft = 180, penetrate = -1, tileCollide = false
    /// </summary>
    public class GravityWellProjectile : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 48;
            Projectile.height = 48;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 180;
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
                Projectile.ai[0] += 1f;
                Projectile.rotation += 0.03f;

                // Damping — el pozo "deriva" en el sitio
                Projectile.velocity *= 0.95f;
                if (Projectile.velocity.Length() < 0.05f) Projectile.velocity = Vector2.Zero;

                // Dust espiral hacia el centro
                if (Main.rand.NextBool(3))
                {
                    SpawnSpiralDust();
                }

                // Pull sobre NPCs hostiles cercanos
                PullNearbyNPCs(250f);

                // Lighting púrpura oscuro
                Lighting.AddLight(Projectile.Center, new Vector3(0.20f, 0.05f, 0.35f));
            }
            catch { }
        }

        private void SpawnSpiralDust()
        {
            float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
            float dist = Main.rand.NextFloat(70f, 110f);
            Vector2 spawnOffset = new Vector2((float)Math.Cos(angle) * dist, (float)Math.Sin(angle) * dist);
            Vector2 spawnPos = Projectile.Center + spawnOffset;

            Vector2 toCenter = -spawnOffset;
            if (toCenter.Length() < 0.1f) return;
            toCenter = toCenter.SafeNormalize(Vector2.Zero);
            // Tangente para espiral
            Vector2 tangent = new Vector2(-toCenter.Y, toCenter.X) * 0.6f;
            Vector2 vel = (toCenter * Main.rand.NextFloat(2.0f, 4.0f)) + tangent;

            int dustType = DustID.PurpleTorch;
            Color dustColor = new Color(120, 40, 200);
            float dustScale = Main.rand.NextFloat(0.8f, 1.3f);

            Dust d = Dust.NewDustPerfect(spawnPos, dustType, vel, 150, dustColor, dustScale);
            d.noGravity = true;
            d.fadeIn = 0f;
        }

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
                    float strength = (1f - dist / radius) * 1.2f;
                    npc.velocity += toCenter * strength;

                    float maxSpeed = 12f;
                    if (npc.velocity.Length() > maxSpeed)
                        npc.velocity = npc.velocity.SafeNormalize(Vector2.Zero) * maxSpeed;

                    if (Main.rand.NextBool(10))
                    {
                        Dust d = Dust.NewDustPerfect(npc.Center, DustID.PurpleTorch,
                            -toCenter * 1.5f, 150, new Color(120, 40, 200), 0.8f);
                        d.noGravity = true;
                        d.fadeIn = 0f;
                    }
                }
            }
            catch { }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Pequeña explosión púrpura al golpear
            try
            {
                for (int i = 0; i < 15; i++)
                {
                    float a = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                    float s = Main.rand.NextFloat(3f, 7f);
                    Vector2 v = new Vector2((float)Math.Cos(a) * s, (float)Math.Sin(a) * s);
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, v, 200, new Color(120, 40, 200), 1.2f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }
            catch { }
        }

        public override void Kill(int timeLeft)
        {
            try
            {
                for (int i = 0; i < 25; i++)
                {
                    float a = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                    float s = Main.rand.NextFloat(3f, 8f);
                    Vector2 v = new Vector2((float)Math.Cos(a) * s, (float)Math.Sin(a) * s);
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, v, 210, new Color(120, 40, 200), 1.3f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }
            catch { }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                Texture2D vortex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Vortex").Value;
                if (vortex == null) return false;

                Vector2 drawPos = Projectile.Center - Main.screenPosition;
                Vector2 origin = new Vector2(vortex.Width / 2f, vortex.Height / 2f);
                float t = Main.GameUpdateCount;

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                // Capa 1: vortex rotando lento, púrpura oscuro (escala grande)
                float rot1 = t * 0.015f + Projectile.rotation;
                Main.spriteBatch.Draw(vortex, drawPos, null,
                    new Color(90, 30, 160, 180),
                    rot1, origin, 2.2f, SpriteEffects.None, 0f);

                // Capa 2: vortex rotando opuesto, púrpura más oscuro
                float rot2 = -t * 0.010f;
                Main.spriteBatch.Draw(vortex, drawPos, null,
                    new Color(60, 20, 110, 140),
                    rot2, origin, 1.6f, SpriteEffects.None, 0f);

                // Capa 3: vortex compacto al centro, púrpura muy oscuro casi negro
                Main.spriteBatch.Draw(vortex, drawPos, null,
                    new Color(40, 10, 80, 200),
                    Projectile.rotation * 2f, origin, 0.9f, SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
            return false;
        }
    }
}
