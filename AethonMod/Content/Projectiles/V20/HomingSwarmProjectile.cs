using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.V20
{
    /// <summary>
    /// HomingSwarmProjectile — proyectil que persigue agresivamente al NPC más cercano.
    ///
    /// Visuales:
    ///   - Star.png rotando
    ///   - GlowCircleGold detrás
    ///   - Trail con oldPos (TrailCacheLength = 15)
    ///   - 1 dust por frame (color dorado del trail)
    ///
    /// Físicas:
    ///   - Homing agresivo hacia NPC más cercano dentro de 600px
    ///   - Penetrate = 5, timeLeft = 200
    /// </summary>
    public class HomingSwarmProjectile : ModProjectile
    {
        private const float HomingRange = 600f;
        private const float HomingStrength = 0.35f;
        private const float MaxSpeed = 14f;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.TrailCacheLength[Type] = 15;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = 5;
            Projectile.timeLeft = 200;
            Projectile.light = 0f;
            Projectile.alpha = 0;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            try
            {
                Projectile.ai[0] += 1f;
                Projectile.rotation += 0.18f;

                // === Homing agresivo hacia NPC más cercano ===
                NPC target = FindNearestNPC(HomingRange);
                if (target != null)
                {
                    Vector2 toTarget = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero);
                    // Aplicar fuerza de homing a la velocidad
                    Projectile.velocity += toTarget * HomingStrength;

                    // Limitar velocidad máxima
                    float speed = Projectile.velocity.Length();
                    if (speed > MaxSpeed)
                        Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.Zero) * MaxSpeed;
                }

                // === 1 dust por frame en color dorado del trail ===
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                    -Projectile.velocity * 0.15f + new Vector2(
                        Main.rand.NextFloat(-0.6f, 0.6f),
                        Main.rand.NextFloat(-0.6f, 0.6f)),
                    150, new Color(255, 220, 100), 1.0f);
                d.noGravity = true;
                d.fadeIn = 0f;

                // Iluminación dorada
                Lighting.AddLight(Projectile.Center, new Vector3(1f, 0.85f, 0.35f));
            }
            catch { }
        }

        private NPC FindNearestNPC(float range)
        {
            NPC best = null;
            float bestDist = range;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.active) continue;
                if (npc.friendly || npc.townNPC) continue;
                if (npc.dontTakeDamage) continue;
                if (!npc.CanBeChasedBy()) continue;

                float dist = Vector2.Distance(npc.Center, Projectile.Center);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = npc;
                }
            }
            return best;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                Texture2D star = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Star").Value;
                Texture2D glowCircleGold = ModContent.Request<Texture2D>("AethonMod/Content/Effects/GlowCircleGold").Value;
                Texture2D softGlow = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;

                if (star == null || glowCircleGold == null || softGlow == null)
                    return false;

                Vector2 drawPos = Projectile.Center - Main.screenPosition;

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                // === Trail con oldPos (color dorado decayendo) ===
                int trailLen = ProjectileID.Sets.TrailCacheLength[Type];
                for (int i = 0; i < trailLen; i++)
                {
                    Vector2 oldPos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                    // Skip uninitialized positions (default Vector2.Zero)
                    Vector2 oldCenter = Projectile.oldPos[i] + Projectile.Size / 2f;
                    if (oldCenter == Vector2.Zero) continue;
                    float t = (float)i / trailLen;
                    float alpha = (1f - t) * 0.6f;
                    float scale = (1f - t) * 0.6f;
                    if (alpha <= 0f || scale <= 0f) continue;

                    Vector2 oldOrigin = new Vector2(softGlow.Width / 2f, softGlow.Height / 2f);
                    Main.spriteBatch.Draw(softGlow, oldPos, null,
                        new Color(255, 210, 80, (byte)(alpha * 255f)),
                        0f, oldOrigin, scale, SpriteEffects.None, 0f);
                }

                // === GlowCircleGold detrás ===
                Vector2 circleOrigin = new Vector2(glowCircleGold.Width / 2f, glowCircleGold.Height / 2f);
                Main.spriteBatch.Draw(glowCircleGold, drawPos, null,
                    new Color(255, 230, 130, 180),
                    Projectile.rotation * 0.5f, circleOrigin, 0.6f, SpriteEffects.None, 0f);

                // === Glow base dorado ===
                Vector2 glowOrigin = new Vector2(softGlow.Width / 2f, softGlow.Height / 2f);
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(255, 220, 100, 200),
                    0f, glowOrigin, 0.7f, SpriteEffects.None, 0f);

                // === Star rotando ===
                Vector2 starOrigin = new Vector2(star.Width / 2f, star.Height / 2f);
                Main.spriteBatch.Draw(star, drawPos, null,
                    new Color(255, 250, 200, 240),
                    Projectile.rotation, starOrigin, 0.85f, SpriteEffects.None, 0f);
                // Capa star más blanca contrarrotando
                Main.spriteBatch.Draw(star, drawPos, null,
                    new Color(255, 255, 240, 150),
                    -Projectile.rotation * 0.6f, starOrigin, 0.55f, SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
            return false;
        }
    }
}
