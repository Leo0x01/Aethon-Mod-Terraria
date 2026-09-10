using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.V20
{
    /// <summary>
    /// SpectralBladeProjectile — cuchilla espectral fantasmal.
    ///
    /// Visuales:
    ///   - Crescent.png con alpha bajo (fantasmal), tinte blanco-cyan
    ///   - Rota y avanza
    ///   - Deja una estela fantasma: dibuja posiciones previas con alpha decreciente
    ///   - NO genera dust (es un fantasma)
    ///
    /// Físicas:
    ///   - penetrate = 3, timeLeft = 60, tileCollide = false
    /// </summary>
    public class SpectralBladeProjectile : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            // TrailCacheLength para guardar posiciones previas (estela fantasma)
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 60;
            Projectile.light = 0.4f;
            Projectile.alpha = 80; // empieza un poco transparente
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
            Projectile.ignoreWater = true;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            try
            {
                Projectile.ai[0] += 1f;
                // Rotación rápida en sentido de movimiento
                Projectile.rotation += 0.20f;

                // Velocidad constante (no damping, se mueve rápido)
                float maxSpeed = 18f;
                if (Projectile.velocity.Length() > maxSpeed)
                    Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.Zero) * maxSpeed;

                // Sin dust — es un fantasma. Pero lighting blanco-cyan.
                Lighting.AddLight(Projectile.Center, new Vector3(0.55f, 0.80f, 0.85f));
            }
            catch { }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Sin dust — solo un breve flash visual mediante un solo dust fantasmal ligero
            // (mantenemos sin dust según spec, solo lighting pulse)
            try
            {
                // Sin dust, pero marcamos el golpe con un breve flash de lighting
                Lighting.AddLight(Projectile.Center, new Vector3(0.9f, 1.0f, 1.0f));
            }
            catch { }
        }

        public override void Kill(int timeLeft)
        {
            // Sin dust — el fantasma simplemente se disuelve
        }

        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                Texture2D crescent = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Crescent").Value;
                if (crescent == null) return false;

                Vector2 drawPos = Projectile.Center - Main.screenPosition;
                Vector2 origin = new Vector2(crescent.Width / 2f, crescent.Height / 2f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                // === ESTELA FANTASMA ===
                // Dibujamos posiciones previas con alpha decreciente.
                int trailLen = ProjectileID.Sets.TrailCacheLength[Type];
                for (int i = trailLen - 1; i >= 0; i--)
                {
                    if (Projectile.oldPos[i] == Vector2.Zero) continue;
                    float progress = (float)i / (float)trailLen;
                    // Alpha decrece desde el más nuevo (i=0, alpha alto) al más viejo
                    float alpha = (1f - progress) * 0.35f;
                    float scale = (1f - progress * 0.6f) * 0.7f;
                    Vector2 ghostPos = Projectile.oldPos[i] + new Vector2(Projectile.width / 2f, Projectile.height / 2f) - Main.screenPosition;

                    // Rotación de cada fantasma con offset
                    float ghostRot = Projectile.rotation - i * 0.18f;

                    // Color blanco-cyan con alpha decreciente (fantasmal)
                    Color ghostColor = new Color(220, 250, 255, (int)(alpha * 255f));
                    Main.spriteBatch.Draw(crescent, ghostPos, null, ghostColor, ghostRot, origin, scale, SpriteEffects.None, 0f);
                }

                // === CRESCENT principal — alpha bajo (fantasmal), blanco-cyan ===
                // Capa exterior (halo)
                Main.spriteBatch.Draw(crescent, drawPos, null,
                    new Color(180, 230, 240, 80),
                    Projectile.rotation, origin, 1.1f, SpriteEffects.None, 0f);

                // Capa media
                Main.spriteBatch.Draw(crescent, drawPos, null,
                    new Color(220, 250, 255, 130),
                    Projectile.rotation * 1.3f, origin, 0.8f, SpriteEffects.None, 0f);

                // Núcleo (más brillante pero aún fantasmal)
                Main.spriteBatch.Draw(crescent, drawPos, null,
                    new Color(255, 255, 255, 160),
                    -Projectile.rotation * 0.7f, origin, 0.45f, SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
            return false;
        }
    }
}
