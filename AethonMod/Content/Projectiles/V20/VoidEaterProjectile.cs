using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.V20
{
    /// <summary>
    /// VoidEaterProjectile — proyectil de vacío que CRECE al consumir enemigos.
    ///
    /// Visuales:
    ///   - SoftGlow con Color.Black (vacío) que crece con cada golpe.
    ///   - Polvo púrpura siendo ABSORVIDO hacia el proyectil.
    ///   - Halo púrpura exterior (más visible que el vacío interno).
    ///
    /// Físicas:
    ///   - ai[0] = cantidad de NPCs consumidos (crecimiento).
    ///   - Cada golpe +0.2 a Projectile.scale.
    ///   - Cuando ai[0] > 5, EXPLOTA.
    ///   - penetrate = 20, timeLeft = 300, tileCollide = false.
    /// </summary>
    public class VoidEaterProjectile : ModProjectile
    {
        private float Hits { get => Projectile.ai[0]; set => Projectile.ai[0] = value; }
        private bool Exploded { get => Projectile.ai[1] > 0f; set => Projectile.ai[1] = value ? 1f : 0f; }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 20;
            Projectile.timeLeft = 300;
            Projectile.light = 0f;
            Projectile.alpha = 0;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.ignoreWater = true;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            try
            {
                // Update scale based on hits (each hit +0.2 starting from 1.0)
                float targetScale = 1.0f + Hits * 0.2f;
                Projectile.scale += (targetScale - Projectile.scale) * 0.2f;

                // Mild homing
                NPC target = null;
                float minDist = 400f;
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.CanBeChasedBy()) continue;
                    float d = (npc.Center - Projectile.Center).Length();
                    if (d < minDist) { minDist = d; target = npc; }
                }
                if (target != null)
                {
                    Vector2 toTarget = (target.Center - Projectile.Center);
                    if (toTarget.Length() > 0.1f)
                    {
                        toTarget.Normalize();
                        Projectile.velocity += toTarget * 0.2f;
                        if (Projectile.velocity.Length() > 10f)
                            Projectile.velocity = Vector2.Normalize(Projectile.velocity) * 10f;
                    }
                }
                // Friction
                Projectile.velocity *= 0.96f;

                // === Purple dust being sucked IN ===
                if (Main.netMode != NetmodeID.Server)
                {
                    int dustPerFrame = 1 + (int)Hits; // more dust as it grows
                    for (int i = 0; i < dustPerFrame; i++)
                    {
                        float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                        float dist = (30f + Hits * 8f) + Main.rand.NextFloat(0f, 40f);
                        Vector2 spawnPos = Projectile.Center + new Vector2(
                            (float)Math.Cos(angle) * dist,
                            (float)Math.Sin(angle) * dist);
                        Vector2 toCenter = Projectile.Center - spawnPos;
                        if (toCenter.Length() > 0.1f)
                        {
                            toCenter.Normalize();
                            // Accelerate toward center
                            float speed = 2f + Hits * 0.3f;
                            Vector2 vel = toCenter * speed;
                            Dust d = Dust.NewDustPerfect(spawnPos, DustID.PurpleTorch,
                                vel, 180, new Color(150, 60, 220), 1.0f);
                            d.noGravity = true;
                            d.fadeIn = 0f;
                        }
                    }
                }

                // Light: dim purple that intensifies slightly with growth
                float intensity = Math.Min(0.3f + Hits * 0.05f, 0.8f);
                Lighting.AddLight(Projectile.Center,
                    new Vector3(0.3f * intensity, 0.1f * intensity, 0.5f * intensity));

                // === Explosion when at max size ===
                if (Hits > 5f && !Exploded)
                {
                    Exploded = true;
                    DoExplosion();
                    Projectile.Kill();
                }
            }
            catch { }
        }

        private void DoExplosion()
        {
            try
            {
                if (Main.netMode != NetmodeID.Server)
                {
                    for (int i = 0; i < 40; i++)
                    {
                        float angle = (MathHelper.TwoPi / 40f) * i + Main.rand.NextFloat(-0.1f, 0.1f);
                        float speed = Main.rand.NextFloat(5f, 12f);
                        Vector2 vel = new Vector2(
                            (float)Math.Cos(angle) * speed,
                            (float)Math.Sin(angle) * speed);
                        Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch,
                            vel, 240, new Color(200, 100, 255), 1.5f);
                        d.noGravity = true;
                        d.fadeIn = 0f;
                    }
                    // 10 white sparks
                    for (int i = 0; i < 10; i++)
                    {
                        Vector2 v = new Vector2(
                            Main.rand.NextFloat(-8f, 8f),
                            Main.rand.NextFloat(-8f, 8f));
                        Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Gold,
                            v, 255, Color.White, 0.9f);
                        d.noGravity = true;
                        d.fadeIn = 0f;
                    }
                }

                // AoE damage in radius (scales with hits)
                float radius = 150f + Hits * 15f;
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.CanBeChasedBy()) continue;
                    float dist = (npc.Center - Projectile.Center).Length();
                    if (dist < radius)
                    {
                        npc.SimpleStrikeNPC(Projectile.damage * 2, npc.direction,
                            true, Projectile.knockBack * 2f, DamageClass.Magic);
                    }
                }

                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            }
            catch { }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            try
            {
                Hits += 1f;
                // Visual: dust burst absorbing
                for (int i = 0; i < 10; i++)
                {
                    float a = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                    float dist = 40f + Main.rand.NextFloat(0f, 20f);
                    Vector2 spawnPos = Projectile.Center + new Vector2(
                        (float)Math.Cos(a) * dist, (float)Math.Sin(a) * dist);
                    Vector2 vel = (Projectile.Center - spawnPos) * 0.04f;
                    Dust d = Dust.NewDustPerfect(spawnPos, DustID.PurpleTorch,
                        vel, 200, new Color(150, 50, 220), 1.0f);
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
                Texture2D softGlow = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                if (softGlow == null) return false;

                Vector2 drawPos = Projectile.Center - Main.screenPosition;
                Vector2 origin = new Vector2(softGlow.Width / 2f, softGlow.Height / 2f);
                float scale = Projectile.scale;

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                // === Outer purple halo (visible void corruption) ===
                // Larger as the void grows
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(100, 40, 180, 130) * scale,
                    0f, origin, 2.0f * scale, SpriteEffects.None, 0f);
                // Mid layer
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(140, 60, 220, 180) * scale,
                    0f, origin, 1.2f * scale, SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

                // === BLACK void core (Solid SoftGlow with Color.Black) ===
                // Drawn with AlphaBlend to mask (truly black)
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    Color.Black,
                    0f, origin, 0.9f * scale, SpriteEffects.None, 0f);

                // === Subtle bright rim around the void ===
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(180, 80, 240, 80) * scale,
                    0f, origin, 1.0f * scale, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(220, 120, 255, 60) * scale,
                    0f, origin, 0.4f * scale, SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
            return false;
        }
    }
}
