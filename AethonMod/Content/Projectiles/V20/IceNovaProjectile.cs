using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.V20
{
    /// <summary>
    /// IceNovaProjectile — anillo de hielo que se expande.
    ///
    /// Visual:
    ///   - Ring.png escalando con el tiempo: scale = 1 + (1 - timeLeft/120) * 5
    ///   - 2 capas: anillo principal cyan + núcleo interior blanco pulsante
    ///   - Dust de IceTorch en círculo cada frame
    ///
    /// Físicas:
    ///   - velocity = 0 (no se mueve)
    ///   - penetrate = -1, timeLeft = 120
    ///   - On hit: aplica Frostburn (3 segundos)
    /// </summary>
    public class IceNovaProjectile : ModProjectile
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
            Projectile.timeLeft = 120;
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
                Projectile.rotation += 0.02f;

                // === IceTorch dust en círculo ===
                SpawnCircleDust();

                // === Lighting ===
                Lighting.AddLight(Projectile.Center, new Vector3(0.4f, 0.7f, 1.0f));
            }
            catch { }
        }

        private void SpawnCircleDust()
        {
            float progress = 1f - (Projectile.timeLeft / 120f);
            float radius = 30f + progress * 180f;
            int count = 8;
            for (int i = 0; i < count; i++)
            {
                float angle = (MathHelper.TwoPi / count) * i + Projectile.ai[0] * 0.05f;
                Vector2 offset = new Vector2((float)Math.Cos(angle) * radius, (float)Math.Sin(angle) * radius);
                Vector2 pos = Projectile.Center + offset;
                Dust d = Dust.NewDustPerfect(pos, DustID.IceTorch, Vector2.Zero, 200,
                    new Color(150, 200, 255), 1.2f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Frostburn, 180);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                Texture2D ring = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Ring").Value;
                if (ring == null) return false;

                Vector2 drawPos = Projectile.Center - Main.screenPosition;
                float scale = 1f + (1f - Projectile.timeLeft / 120f) * 5f;
                Vector2 origin = new Vector2(ring.Width / 2f, ring.Height / 2f);
                float alpha = MathHelper.Clamp(Projectile.timeLeft / 120f, 0f, 1f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                // Anillo principal cyan
                Main.spriteBatch.Draw(ring, drawPos, null,
                    new Color(180, 220, 255, 220) * alpha,
                    Projectile.rotation, origin, scale, SpriteEffects.None, 0f);

                // Núcleo interior blanco pulsante
                Main.spriteBatch.Draw(ring, drawPos, null,
                    new Color(255, 255, 255, 100) * alpha,
                    -Projectile.rotation, origin, scale * 0.5f, SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
            return false;
        }
    }
}
