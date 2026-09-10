using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.V20
{
    /// <summary>
    /// PlasmaOrbProjectile — orbe de plasma que orbita al jugador.
    ///
    /// Visual:
    ///   - SoftGlow con color púrpura-azul pulsante (3 capas)
    ///   - Dust pequeño orbitando
    ///
    /// Físicas:
    ///   - Posición = player.Center + offset rotado por el tiempo
    ///   - velocity = 0 (la posición se setea manualmente)
    ///   - penetrate = -1, timeLeft = 300
    ///   - ExtraUpdates = 0 (un update por frame para órbita suave)
    /// </summary>
    public class PlasmaOrbProjectile : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            Projectile.light = 0f;
            Projectile.alpha = 0;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 0;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            try
            {
                Player player = Main.player[Projectile.owner];
                if (!player.active || player.dead)
                {
                    Projectile.Kill();
                    return;
                }

                Projectile.ai[0] += 1f;
                float t = Projectile.ai[0];

                // === Órbita alrededor del jugador ===
                float orbitRadius = 60f;
                float orbitSpeed = 0.08f;
                float angle = t * orbitSpeed;
                Vector2 offset = new Vector2((float)Math.Cos(angle) * orbitRadius,
                                              (float)Math.Sin(angle) * orbitRadius);
                Projectile.Center = player.Center + offset;
                Projectile.velocity = Vector2.Zero;

                // === Dust pequeño orbitando ===
                if (Main.rand.NextBool(3))
                {
                    float dAngle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                    float dRadius = Main.rand.NextFloat(10f, 30f);
                    Vector2 dOffset = new Vector2((float)Math.Cos(dAngle) * dRadius,
                                                   (float)Math.Sin(dAngle) * dRadius);
                    Vector2 dPos = Projectile.Center + dOffset;
                    Vector2 dVel = -dOffset.SafeNormalize(Vector2.Zero) * 0.5f;
                    int dustType = Main.rand.NextBool(2) ? DustID.PurpleTorch : DustID.Fireworks;
                    Color color = Main.rand.NextBool(2)
                        ? new Color(180, 100, 255)
                        : new Color(100, 150, 255);
                    Dust d = Dust.NewDustPerfect(dPos, dustType, dVel, 150, color, 0.9f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }

                // === Lighting ===
                Lighting.AddLight(Projectile.Center, new Vector3(0.5f, 0.3f, 0.9f));
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
                float pulse = 0.5f + 0.5f * (float)Math.Sin(Projectile.ai[0] * 0.15f);
                float scale = 0.6f + pulse * 0.3f;

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                // Glow púrpura exterior
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(180, 80, 255, 200) * (0.7f + pulse * 0.3f),
                    0f, origin, scale, SpriteEffects.None, 0f);

                // Glow azul medio
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(80, 130, 255, 200) * (0.7f + pulse * 0.3f),
                    0f, origin, scale * 0.6f, SpriteEffects.None, 0f);

                // Núcleo blanco interior
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(255, 240, 255, 220) * (0.5f + pulse * 0.5f),
                    0f, origin, scale * 0.3f, SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
            return false;
        }
    }
}
