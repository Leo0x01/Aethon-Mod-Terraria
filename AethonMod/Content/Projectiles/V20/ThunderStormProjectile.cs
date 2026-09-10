using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.V20
{
    /// <summary>
    /// ThunderStormProjectile — rayo que cae del cielo sobre el cursor.
    ///
    /// Visuales:
    ///   - BeamCyan dibujado verticalmente con flickering (scale aleatorio
    ///     cada frame para simular el parpadeo del relámpago).
    ///   - SoftGlow cian en la cabeza del rayo.
    ///   - TrailCacheLength = 12.
    ///
    /// Físicas:
    ///   - Cae hacia abajo a alta velocidad (22f desde el arma).
    ///   - penetrate = -1.
    ///   - timeLeft = 60.
    ///   - tileCollide = true (impacta paredes).
    ///   - Al impactar NPC o tile: SoundID.Thunder + ráfaga de BlueTorch dust
    ///     en círculo.
    /// </summary>
    public class ThunderStormProjectile : ModProjectile
    {
        private bool _impacted = false;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.tileCollide = true;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
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
                Projectile.ai[0] += 1f;
                Projectile.rotation = Projectile.velocity.ToRotation();

                // Dust trail BlueTorch a lo largo del rayo
                if (Main.rand.NextBool(3))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch,
                        new Vector2(Main.rand.NextFloat(-1.5f, 1.5f),
                                     Main.rand.NextFloat(-1.5f, 1.5f)),
                        180, new Color(120, 200, 255), 1.0f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }

                // Iluminación cian brillante (parpadeante)
                float flicker = 0.7f + Main.rand.NextFloat(0.3f);
                Lighting.AddLight(Projectile.Center,
                    new Vector3(0.4f * flicker, 0.85f * flicker, 1f * flicker));
            }
            catch { }
        }

        // ================================================================
        //  OnHitNPC — impacto: sonido thunder + burst de BlueTorch
        // ================================================================
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            TriggerImpact();
        }

        // ================================================================
        //  Kill — al expirar o impactar tile (Kill se llama por tile collide)
        // ================================================================
        public override void Kill(int timeLeft)
        {
            TriggerImpact();
        }

        // ================================================================
        //  TriggerImpact — efecto de impacto del relámpago
        // ================================================================
        private void TriggerImpact()
        {
            if (_impacted) return;
            _impacted = true;
            try
            {
                // Sonido de thunder
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Thunder, Projectile.Center);

                // Burst de BlueTorch dust en círculo
                int count = 24;
                for (int i = 0; i < count; i++)
                {
                    float angle = (MathHelper.TwoPi / count) * i + Main.rand.NextFloat(-0.15f, 0.15f);
                    float speed = Main.rand.NextFloat(4f, 9f);
                    Vector2 vel = new Vector2((float)Math.Cos(angle) * speed,
                                               (float)Math.Sin(angle) * speed);
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch,
                        vel, 220, new Color(150, 220, 255), 1.5f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
                // Destello blanco central
                for (int i = 0; i < 8; i++)
                {
                    float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                    float speed = Main.rand.NextFloat(2f, 5f);
                    Vector2 vel = new Vector2((float)Math.Cos(angle) * speed,
                                               (float)Math.Sin(angle) * speed);
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Pink,
                        vel, 255, new Color(255, 255, 255), 0.9f);
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
                Texture2D beamCyan = ModContent.Request<Texture2D>("AethonMod/Content/Effects/BeamCyan").Value;
                Texture2D softGlow = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;

                if (beamCyan == null || softGlow == null)
                    return false;

                Vector2 drawPos = Projectile.Center - Main.screenPosition;
                float rotation = Projectile.velocity.ToRotation();

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                // === Estela trail (vertical) ===
                int trailLen = ProjectileID.Sets.TrailCacheLength[Type];
                for (int i = 0; i < trailLen - 1; i++)
                {
                    Vector2 p1 = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                    Vector2 p2 = Projectile.oldPos[i + 1] + Projectile.Size / 2f - Main.screenPosition;
                    if (p1 == p2) continue;

                    float segLen = Vector2.Distance(p1, p2);
                    if (segLen < 0.1f) continue;
                    Vector2 segDir = (p2 - p1).SafeNormalize(Vector2.Zero);
                    float segRot = segDir.ToRotation();
                    float alpha = (1f - (float)i / trailLen) * 0.5f;

                    Main.spriteBatch.Draw(beamCyan, p1, null,
                        new Color(120, 220, 255, (byte)(alpha * 255f)),
                        segRot,
                        new Vector2(0f, beamCyan.Height / 2f),
                        new Vector2(segLen / (float)beamCyan.Width, 0.35f),
                        SpriteEffects.None, 0f);
                }

                // === BeamCyan vertical con FLICKERING (scale aleatorio cada frame) ===
                Vector2 beamOrigin = new Vector2(0f, beamCyan.Height / 2f);
                // Scale X = 1.2..3.0 (largo del rayo), Scale Y = 0.2..0.6 (ancho flicker)
                float flickerX = 1.5f + Main.rand.NextFloat(1.5f);
                float flickerY = 0.2f + Main.rand.NextFloat(0.4f);
                // Beam principal
                Main.spriteBatch.Draw(beamCyan, drawPos, null,
                    new Color(180, 240, 255, 240),
                    rotation, beamOrigin,
                    new Vector2(flickerX, flickerY), SpriteEffects.None, 0f);
                // Halo cian más amplio
                Main.spriteBatch.Draw(beamCyan, drawPos, null,
                    new Color(80, 180, 255, 120),
                    rotation, beamOrigin,
                    new Vector2(flickerX * 1.1f, flickerY * 1.8f), SpriteEffects.None, 0f);

                // === Glow central cian-blanco ===
                Vector2 glowOrigin = new Vector2(softGlow.Width / 2f, softGlow.Height / 2f);
                float glowFlicker = 0.8f + Main.rand.NextFloat(0.4f);
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(180, 230, 255, 220),
                    0f, glowOrigin, 0.9f * glowFlicker, SpriteEffects.None, 0f);
                // Núcleo blanco
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(255, 255, 255, 240),
                    0f, glowOrigin, 0.45f * glowFlicker, SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
            return false;
        }
    }
}
