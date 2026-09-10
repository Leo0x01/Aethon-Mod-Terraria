using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.V20
{
    /// <summary>
    /// MirrorShardProjectile — esquirla de cristal que rebota en tiles.
    ///
    /// Visuales:
    ///   - Crescent.png rotando, color cyan-blanco
    ///   - Trail.png usando oldPos (TrailCacheLength = 10)
    ///   - En cada rebote: 5 partículas de shard
    ///
    /// Físicas:
    ///   - tileCollide = true, rebota (OnTileCollide)
    ///   - penetrate = 5, timeLeft = 180
    /// </summary>
    public class MirrorShardProjectile : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            // Longitud del trail cacheado
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.tileCollide = true;   // rebota en tiles
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 5;
            Projectile.timeLeft = 180;
            Projectile.light = 0.4f;
            Projectile.alpha = 0;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
            Projectile.ignoreWater = true;
        }

        public override bool? CanCutTiles() => true;

        // Rebote en tiles: volteamos la componente de velocidad que colisiona.
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            try
            {
                // Determinar componente colisionada: si X cambió mucho, rebota X
                if (Math.Abs(Projectile.velocity.X - oldVelocity.X) > float.Epsilon)
                    Projectile.velocity.X = -oldVelocity.X;
                if (Math.Abs(Projectile.velocity.Y - oldVelocity.Y) > float.Epsilon)
                    Projectile.velocity.Y = -oldVelocity.Y;

                // Spawn 5 dust de shard
                for (int i = 0; i < 5; i++)
                {
                    float a = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                    float s = Main.rand.NextFloat(2f, 5f);
                    Vector2 v = new Vector2((float)Math.Cos(a) * s, (float)Math.Sin(a) * s);
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.CrystalSerpent_Pink,
                        v, 150, new Color(150, 230, 255), 1.1f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }

                // Pequeño impulso tras el rebote para evitar atascarse
                Projectile.position += Projectile.velocity;
                Projectile.netUpdate = true;
            }
            catch { }
            return false; // false = NO matar el proyectil, seguir vivo
        }

        public override void AI()
        {
            try
            {
                Projectile.ai[0] += 1f;
                Projectile.rotation += 0.25f; // gira rápido (esquirla)

                // Gravedad muy leve (casi cristal flotante pero con peso)
                Projectile.velocity.Y += 0.03f;
                // Clamp de velocidad máxima
                float maxSpeed = 14f;
                if (Projectile.velocity.Length() > maxSpeed)
                    Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.Zero) * maxSpeed;

                // Dust cyan periódico
                if (Main.rand.NextBool(6))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.CrystalSerpent_Pink,
                        -Projectile.velocity * 0.1f, 150, new Color(150, 230, 255), 0.9f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }

                // Lighting cyan
                Lighting.AddLight(Projectile.Center, new Vector3(0.30f, 0.70f, 0.85f));
            }
            catch { }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            try
            {
                for (int i = 0; i < 10; i++)
                {
                    float a = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                    float s = Main.rand.NextFloat(3f, 7f);
                    Vector2 v = new Vector2((float)Math.Cos(a) * s, (float)Math.Sin(a) * s);
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.CrystalSerpent_Pink,
                        v, 200, new Color(150, 230, 255), 1.2f);
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
                    float s = Main.rand.NextFloat(3f, 8f);
                    Vector2 v = new Vector2((float)Math.Cos(a) * s, (float)Math.Sin(a) * s);
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.CrystalSerpent_Pink,
                        v, 220, new Color(150, 230, 255), 1.3f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }
            catch { }
        }

        // PreDraw retorna TRUE: además del custom, el motor dibuja el sprite.
        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                Texture2D crescent = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Crescent").Value;
                Texture2D trail = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Trail").Value;
                if (crescent == null || trail == null) return true;

                Vector2 drawPos = Projectile.Center - Main.screenPosition;
                Vector2 crescentOrigin = new Vector2(crescent.Width / 2f, crescent.Height / 2f);
                Vector2 trailOrigin = new Vector2(trail.Width / 2f, trail.Height / 2f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                // === TRAIL con oldPos ===
                // TrailCacheLength = 10, dibujamos cada posición previa con alpha decreciente.
                int trailLen = ProjectileID.Sets.TrailCacheLength[Type];
                for (int i = 0; i < trailLen; i++)
                {
                    // oldPos[i] es la posición top-left del proyectil (no el center)
                    if (Projectile.oldPos[i] == Vector2.Zero) continue;
                    float progress = (float)i / (float)trailLen;
                    float alpha = (1f - progress) * 0.6f;
                    float scale = (1f - progress * 0.5f) * 0.6f;
                    Vector2 trailPos = Projectile.oldPos[i] + new Vector2(Projectile.width / 2f, Projectile.height / 2f) - Main.screenPosition;

                    Main.spriteBatch.Draw(trail, trailPos, null,
                        new Color(150, 230, 255, (int)(alpha * 255f)),
                        Projectile.velocity.ToRotation() + MathHelper.PiOver2,
                        trailOrigin, scale, SpriteEffects.None, 0f);
                }

                // === CRESCENT — cyan-blanco, rotando ===
                // Capa principal
                Main.spriteBatch.Draw(crescent, drawPos, null,
                    new Color(180, 240, 255, 220),
                    Projectile.rotation, crescentOrigin, 0.7f, SpriteEffects.None, 0f);

                // Capa secundaria más blanca, rotación opuesta
                Main.spriteBatch.Draw(crescent, drawPos, null,
                    new Color(255, 255, 255, 120),
                    -Projectile.rotation * 0.8f, crescentOrigin, 0.9f, SpriteEffects.None, 0f);

                // Brillo central cyan
                Main.spriteBatch.Draw(crescent, drawPos, null,
                    new Color(100, 220, 255, 160),
                    Projectile.rotation * 1.5f, crescentOrigin, 0.4f, SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
            return true; // también deja que el motor dibuje el sprite original
        }
    }
}
