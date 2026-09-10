using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.V20
{
    /// <summary>
    /// FlamethrowerProjectile — corriente de fuego continuo.
    ///
    /// Visuales:
    ///   - SoftGlow en color naranja-rojo
    ///   - Genera DustID.Torch cada frame con spread aleatorio
    ///   - El visual es una "corriente" de fuego, no un solo proyectil
    ///
    /// Físicas:
    ///   - Sin gravedad
    ///   - timeLeft = 45 (corta vida)
    ///   - On hit: aplica buff OnFire
    /// </summary>
    public class FlamethrowerProjectile : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.tileCollide = true;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 45;
            Projectile.light = 0f;
            Projectile.alpha = 0;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.ignoreWater = false;
            Projectile.extraUpdates = 1;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            try
            {
                Projectile.ai[0] += 1f;
                Projectile.rotation += 0.05f;

                // Sin gravedad — la corriente de fuego flota
                // (no aplicamos gravity)

                // Amortiguación leve para que la corriente se disperse
                Projectile.velocity *= 0.99f;

                // === Generar partículas de fuego cada frame ===
                // DustID.Torch con spread aleatorio en torno al proyectil
                int numFire = Main.rand.Next(2, 4);
                for (int i = 0; i < numFire; i++)
                {
                    Vector2 spread = new Vector2(
                        Main.rand.NextFloat(-3f, 3f),
                        Main.rand.NextFloat(-3f, 3f));
                    Vector2 vel = Projectile.velocity * 0.2f + spread;
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch,
                        vel, 150,
                        new Color(255, 120, 30),
                        Main.rand.NextFloat(0.9f, 1.4f));
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }

                // Embers secundarios (más blancos en el centro, más calientes)
                if (Main.rand.NextBool(3))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.YellowTorch,
                        -Projectile.velocity * 0.1f + new Vector2(
                            Main.rand.NextFloat(-1.5f, 1.5f),
                            Main.rand.NextFloat(-1.5f, 1.5f)),
                        150, new Color(255, 220, 140), 1.0f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }

                // Iluminación naranja-roja cálida
                Lighting.AddLight(Projectile.Center, new Vector3(1f, 0.45f, 0.1f));
            }
            catch { }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            try
            {
                // Aplicar buff OnFire (BUFF_ID 24)
                target.AddBuff(BuffID.OnFire, 180); // 3 segundos
            }
            catch { }
        }

        public override void Kill(int timeLeft)
        {
            try
            {
                // Pequeña ráfaga final de brasas
                for (int i = 0; i < 6; i++)
                {
                    float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                    float speed = Main.rand.NextFloat(1f, 4f);
                    Vector2 vel = new Vector2((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed);
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, vel, 200,
                        new Color(255, 140, 40), 1.0f);
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

                if (softGlow == null)
                    return false;

                Vector2 drawPos = Projectile.Center - Main.screenPosition;
                Vector2 origin = new Vector2(softGlow.Width / 2f, softGlow.Height / 2f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                // === Trail con oldPos (corriente de fuego) ===
                int trailLen = ProjectileID.Sets.TrailCacheLength[Type];
                for (int i = 0; i < trailLen; i++)
                {
                    Vector2 oldPos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                    float t = (float)i / trailLen;
                    float alpha = (1f - t) * 0.55f;
                    float scale = (1f - t) * 0.85f;
                    if (alpha <= 0f || scale <= 0f) continue;

                    // Color que va de blanco-amarillo (centro caliente) a rojo oscuro (cola)
                    byte r = (byte)(255 - t * 60);
                    byte g = (byte)(180 - t * 130);
                    byte b = (byte)(60 - t * 50);

                    Main.spriteBatch.Draw(softGlow, oldPos, null,
                        new Color(r, g, b, (byte)(alpha * 255f)),
                        0f, origin, scale, SpriteEffects.None, 0f);
                }

                // === Glow central naranja-rojo ===
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(255, 120, 30, 220),
                    0f, origin, 0.85f, SpriteEffects.None, 0f);
                // Núcleo blanco-amarillo caliente
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(255, 220, 130, 200),
                    0f, origin, 0.45f, SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
            return false;
        }
    }
}
