using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.V20
{
    /// <summary>
    /// VoidRiftProjectile — grieta dimensional vertical.
    ///
    /// Visual:
    ///   - Slash.png estirado verticalmente (scaleX=0.3, scaleY=3)
    ///   - Color púrpura-negro con glow púrpura (3 capas additive)
    ///
    /// Físicas:
    ///   - velocity = 0 (no se mueve)
    ///   - Dust PurpleTorch siendo absorbido hacia el centro
    ///   - Pull ligero sobre NPCs en 200px
    ///   - timeLeft = 90, penetrate = -1, tileCollide = false
    /// </summary>
    public class VoidRiftProjectile : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 120;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 90;
            Projectile.light = 0f;
            Projectile.alpha = 0;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
            Projectile.ignoreWater = true;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            try
            {
                Projectile.velocity = Vector2.Zero;
                Projectile.ai[0] += 1f;
                Projectile.rotation += 0.01f;

                // === Shadow dust siendo absorbido ===
                if (Main.rand.NextBool(2))
                {
                    SpawnInwardDust();
                }

                // === Pull enemies ===
                PullNearbyNPCs(200f);

                // === Lighting ===
                Lighting.AddLight(Projectile.Center, new Vector3(0.3f, 0.05f, 0.5f));
            }
            catch { }
        }

        private void SpawnInwardDust()
        {
            float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
            float dist = Main.rand.NextFloat(60f, 100f);
            Vector2 spawnOffset = new Vector2((float)Math.Cos(angle) * dist,
                                              (float)Math.Sin(angle) * dist);
            Vector2 spawnPos = Projectile.Center + spawnOffset;
            Vector2 toCenter = -spawnOffset;
            if (toCenter.Length() < 0.1f) return;
            toCenter = toCenter.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(2f, 4f);
            Dust d = Dust.NewDustPerfect(spawnPos, DustID.PurpleTorch, toCenter, 150,
                new Color(160, 60, 240), 1.1f);
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
                    float strength = (1f - dist / radius) * 0.8f;
                    npc.velocity += toCenter * strength;

                    float maxSpeed = 8f;
                    if (npc.velocity.Length() > maxSpeed)
                        npc.velocity = npc.velocity.SafeNormalize(Vector2.Zero) * maxSpeed;
                }
            }
            catch { }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                Texture2D slash = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Slash").Value;
                if (slash == null) return false;

                Vector2 drawPos = Projectile.Center - Main.screenPosition;
                Vector2 origin = new Vector2(slash.Width / 2f, slash.Height / 2f);

                // scaleX=0.3, scaleY=3 — grieta vertical
                Vector2 scale = new Vector2(0.3f, 3f);
                float alpha = MathHelper.Clamp(Projectile.timeLeft / 90f, 0f, 1f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                // Capa oscura púrpura-negro
                Main.spriteBatch.Draw(slash, drawPos, null,
                    new Color(80, 0, 130, 220) * alpha,
                    Projectile.rotation * 0.5f, origin, scale, SpriteEffects.None, 0f);

                // Glow púrpura
                Main.spriteBatch.Draw(slash, drawPos, null,
                    new Color(180, 60, 255, 150) * alpha,
                    -Projectile.rotation * 0.3f, origin, scale * 0.8f, SpriteEffects.None, 0f);

                // Línea central brillante
                Main.spriteBatch.Draw(slash, drawPos, null,
                    new Color(220, 100, 255, 180) * alpha,
                    0f, origin, scale * 0.5f, SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
            return false;
        }
    }
}
