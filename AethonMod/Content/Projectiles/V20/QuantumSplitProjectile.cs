using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.V20
{
    /// <summary>
    /// QuantumSplitProjectile — proyectil que se divide en 3 tras ~30 frames.
    ///
    /// Visuales:
    ///   - SoftGlow con color arcoíris que cambia (usando Main.GameUpdateCount)
    ///   - Trail con oldPos
    ///
    /// Físicas:
    ///   - En AI: si ai[0]==0 y timeLeft &lt; 90 → set ai[0]=1 y spawn 2 proyectiles
    ///     a ±30° (con ai[0]=1 para que no se dividan a su vez)
    ///   - penetrate = 1, timeLeft = 120
    /// </summary>
    public class QuantumSplitProjectile : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.tileCollide = true;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 120;
            Projectile.light = 0.5f;
            Projectile.alpha = 0;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.ignoreWater = true;
        }

        public override bool? CanCutTiles() => true;

        public override void AI()
        {
            try
            {
                Projectile.ai[0] += 0f; // no usamos ai[0] como contador, solo como flag
                Projectile.rotation += 0.15f;

                // === Split: si ai[0]==0 y timeLeft < 90 → split ===
                // timeLeft=120 inicial → cuando timeLeft=90 han pasado ~30 frames.
                if (Projectile.ai[0] == 0f && Projectile.timeLeft < 90)
                {
                    Projectile.ai[0] = 1f;
                    SplitIntoThree();
                }

                // Gravedad muy leve
                Projectile.velocity.Y += 0.02f;
                // Clamp velocidad
                float maxSpeed = 16f;
                if (Projectile.velocity.Length() > maxSpeed)
                    Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.Zero) * maxSpeed;

                // Dust arcoíris periódico
                if (Main.rand.NextBool(5))
                {
                    Color dustColor = RainbowColor(Main.GameUpdateCount);
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.RainbowRod,
                        -Projectile.velocity * 0.1f, 150, dustColor, 0.9f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }

                // Lighting arcoíris cambiante
                Color lc = RainbowColor(Main.GameUpdateCount);
                Lighting.AddLight(Projectile.Center, new Vector3(lc.R / 255f * 0.6f, lc.G / 255f * 0.6f, lc.B / 255f * 0.6f));
            }
            catch { }
        }

        private void SplitIntoThree()
        {
            try
            {
                Vector2 baseVel = Projectile.velocity;
                if (baseVel.Length() < 0.1f) baseVel = new Vector2(1f, 0f);
                float baseSpeed = baseVel.Length();
                Vector2 baseDir = baseVel.SafeNormalize(Vector2.Zero);

                // ±30° en radianes
                float angleOffset = MathHelper.ToRadians(30f);

                // Spawn dos proyectiles adicionales a ±30° (el original sigue recto)
                Vector2 leftDir = baseDir.RotatedBy(-angleOffset);
                Vector2 rightDir = baseDir.RotatedBy(angleOffset);

                // ai[0]=1 al spawn para que no se dividan a su vez
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    leftDir * baseSpeed,
                    Projectile.type,
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    1f, 0f);

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    rightDir * baseSpeed,
                    Projectile.type,
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    1f, 0f);

                // Pequeña explosión visual al dividir
                for (int i = 0; i < 12; i++)
                {
                    float a = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                    float s = Main.rand.NextFloat(2f, 5f);
                    Vector2 v = new Vector2((float)Math.Cos(a) * s, (float)Math.Sin(a) * s);
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.RainbowRod, v, 200, RainbowColor(Main.GameUpdateCount), 1.0f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }
            catch { }
        }

        // Color arcoíris basado en GameUpdateCount
        private Color RainbowColor(float t)
        {
            float hue = (t * 0.01f) % 1f;
            return Main.hslToRgb(hue, 1f, 0.6f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            try
            {
                for (int i = 0; i < 10; i++)
                {
                    float a = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                    float s = Main.rand.NextFloat(2f, 6f);
                    Vector2 v = new Vector2((float)Math.Cos(a) * s, (float)Math.Sin(a) * s);
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.RainbowRod, v, 200, RainbowColor(Main.GameUpdateCount), 1.0f);
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
                for (int i = 0; i < 15; i++)
                {
                    float a = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                    float s = Main.rand.NextFloat(3f, 7f);
                    Vector2 v = new Vector2((float)Math.Cos(a) * s, (float)Math.Sin(a) * s);
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.RainbowRod, v, 220, RainbowColor(Main.GameUpdateCount), 1.2f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }
            catch { }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // Rebote para que el split no se pierda
            try
            {
                if (Math.Abs(Projectile.velocity.X - oldVelocity.X) > float.Epsilon)
                    Projectile.velocity.X = -oldVelocity.X;
                if (Math.Abs(Projectile.velocity.Y - oldVelocity.Y) > float.Epsilon)
                    Projectile.velocity.Y = -oldVelocity.Y;
                Projectile.position += Projectile.velocity;
                Projectile.netUpdate = true;
            }
            catch { }
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                Texture2D softGlow = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                if (softGlow == null) return false;

                Vector2 drawPos = Projectile.Center - Main.screenPosition;
                Vector2 origin = new Vector2(softGlow.Width / 2f, softGlow.Height / 2f);
                float t = Main.GameUpdateCount;

                Color baseColor = RainbowColor(t);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                // === TRAIL con oldPos ===
                int trailLen = ProjectileID.Sets.TrailCacheLength[Type];
                for (int i = 0; i < trailLen; i++)
                {
                    if (Projectile.oldPos[i] == Vector2.Zero) continue;
                    float progress = (float)i / (float)trailLen;
                    float alpha = (1f - progress) * 0.5f;
                    float scale = (1f - progress * 0.5f) * 0.5f;
                    Vector2 trailPos = Projectile.oldPos[i] + new Vector2(Projectile.width / 2f, Projectile.height / 2f) - Main.screenPosition;

                    Color trailColor = RainbowColor(t - i * 2f);
                    trailColor.A = (byte)(alpha * 255f);
                    Main.spriteBatch.Draw(softGlow, trailPos, null, trailColor, 0f, origin, scale, SpriteEffects.None, 0f);
                }

                // === SOFTGLOW arcoíris ===
                // Halo exterior grande con color arcoíris
                Color outer = baseColor;
                outer.A = 100;
                Main.spriteBatch.Draw(softGlow, drawPos, null, outer, 0f, origin, 1.6f, SpriteEffects.None, 0f);

                // Glow medio
                Color mid = baseColor;
                mid.A = 200;
                Main.spriteBatch.Draw(softGlow, drawPos, null, mid, Projectile.rotation, origin, 1.0f, SpriteEffects.None, 0f);

                // Núcleo blanco brillante
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(255, 255, 255, 220),
                    0f, origin, 0.4f, SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
            return false;
        }
    }
}
