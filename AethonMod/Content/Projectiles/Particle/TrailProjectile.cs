using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Particles;

namespace AethonMod.Content.Projectiles.Particle
{
    /// <summary>
    /// TrailProjectile — proyectil con trail continuo usando oldPos[].
    /// En lugar de spawner Dust disperso (puntos separados), dibuja un trail
    /// degradado continuo en PreDraw usando el array oldPos[] del proyectil.
    ///
    /// Basado en: "Librería de Partículas para Terraria - Referencia para IA"
    /// Sección 8.3: Trails continuos
    /// </summary>
    public class TrailProjectile : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 180;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            // Rotación suave
            Projectile.rotation += 0.1f;

            // Spawn partículas del ParticleManager cada frame (trail delgado)
            if (Main.netMode != Terraria.ID.NetmodeID.Server)
            {
                var data = new ParticleData
                {
                    Position = Projectile.Center,
                    Velocity = -Projectile.velocity * 0.1f,
                    Scale = new Vector2(0.8f),
                    PackedColor = ParticleManager.PackColor(new Color(100, 200, 255)),
                    PackedStartColor = ParticleManager.PackColor(new Color(100, 200, 255)),
                    PackedEndColor = ParticleManager.PackColor(new Color(50, 100, 200)),
                    Rotation = 0f,
                    RotationSpeed = 0f,
                    TimeLeft = 20,
                    Duration = 20,
                    TextureId = 0, // SoftGlow
                    BlendMode = 1, // Additive
                    LayerPriority = LayerPriorities.BeforeProjectiles,
                };
                data.EnableComponent(ComponentFlag.FadeOut);
                data.EnableComponent(ComponentFlag.ScaleDown);
                ParticleManager.Spawn(data);
            }

            // Luz cian
            Lighting.AddLight(Projectile.Center, new Vector3(0.3f, 0.7f, 1f));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // === TRAIL CONTINUO usando oldPos[] ===
            // Dibuja el trail texturizado a lo largo de la trayectoria del proyectil
            try
            {
                Texture2D trailTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Trail").Value;
                if (trailTex == null) return true;

                int trailLength = Math.Min(Projectile.oldPos.Length, 20);
                if (trailLength < 2) return true;

                // Dibujar el trail de atrás hacia adelante
                for (int i = trailLength - 1; i > 0; i--)
                {
                    if (Projectile.oldPos[i] == Vector2.Zero) continue;

                    Vector2 pos = Projectile.oldPos[i] + new Vector2(Projectile.width / 2f, Projectile.height / 2f);
                    Vector2 prevPos = Projectile.oldPos[i - 1] + new Vector2(Projectile.width / 2f, Projectile.height / 2f);
                    if (Projectile.oldPos[i - 1] == Vector2.Zero) prevPos = pos;

                    // Dirección y distancia entre puntos
                    Vector2 dir = prevPos - pos;
                    float dist = dir.Length();
                    if (dist < 0.1f) continue;
                    dir.Normalize();

                    // Angle y scale
                    float angle = (float)Math.Atan2(dir.Y, dir.X);
                    float progress = i / (float)trailLength; // 1.0 = atrás, 0.0 = adelante
                    float alpha = (1f - progress) * 0.8f;
                    float scale = (1f - progress * 0.7f) * 0.5f;

                    // Dibujar el trail segmento con additive blending
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
                    Main.spriteBatch.Draw(trailTex,
                        pos - Main.screenPosition,
                        null,
                        new Color(100, 200, 255, (byte)(255 * alpha)),
                        angle,
                        new Vector2(0, trailTex.Height / 2f),
                        new Vector2(dist / trailTex.Width, scale),
                        SpriteEffects.None, 0f);
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                }
            }
            catch { }

            return true; // dibujar sprite vanilla del proyectil también
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Impact burst con ParticleManager
            for (int i = 0; i < 12; i++)
            {
                float angle = (MathHelper.TwoPi / 12) * i;
                Vector2 dir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 4f;
                var data = new ParticleData
                {
                    Position = target.Center,
                    Velocity = dir,
                    Scale = new Vector2(1f),
                    PackedColor = ParticleManager.PackColor(new Color(150, 220, 255)),
                    PackedStartColor = ParticleManager.PackColor(new Color(255, 255, 255)),
                    PackedEndColor = ParticleManager.PackColor(new Color(50, 100, 200)),
                    Rotation = 0f,
                    RotationSpeed = 0.2f,
                    TimeLeft = 30,
                    Duration = 30,
                    TextureId = 2, // Star
                    BlendMode = 1, // Additive
                    LayerPriority = LayerPriorities.AfterProjectiles,
                };
                data.EnableComponent(ComponentFlag.FadeOut);
                data.EnableComponent(ComponentFlag.ScaleDown);
                data.EnableComponent(ComponentFlag.Rotation);
                ParticleManager.Spawn(data);
            }
        }
    }
}
