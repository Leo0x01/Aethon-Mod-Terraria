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
    /// CalamitySlash — slash effect estilo Calamity Mod.
    /// Dibuja un crescent texturizado con rotación + scale asimétrico para
    /// formar el característico arco curvo de los slashes de melee.
    ///
    /// Basado en: "Librería de Partículas para Terraria - Referencia para IA"
    /// Sección 8.1: Slash effects estilo Calamity
    /// </summary>
    public class CalamitySlash : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 80;
            Projectile.height = 80;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 30;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            // Rotación rápida del slash
            Projectile.rotation += 0.4f;

            // Spawn partículas de chispa cada frame
            if (Main.netMode != Terraria.ID.NetmodeID.Server && Main.rand.NextBool(2))
            {
                Vector2 offset = new Vector2(
                    Main.rand.NextFloat(-30f, 30f),
                    Main.rand.NextFloat(-30f, 30f));
                var data = new ParticleData
                {
                    Position = Projectile.Center + offset,
                    Velocity = offset * -0.05f,
                    Scale = new Vector2(0.5f),
                    PackedColor = ParticleManager.PackColor(new Color(255, 255, 255)),
                    PackedStartColor = ParticleManager.PackColor(new Color(255, 255, 255)),
                    PackedEndColor = ParticleManager.PackColor(new Color(150, 200, 255)),
                    Rotation = 0f,
                    RotationSpeed = 0.3f,
                    TimeLeft = 15,
                    Duration = 15,
                    TextureId = 2, // Star
                    BlendMode = 1, // Additive
                    LayerPriority = LayerPriorities.AfterProjectiles,
                };
                data.EnableComponent(ComponentFlag.FadeOut);
                data.EnableComponent(ComponentFlag.ScaleDown);
                ParticleManager.Spawn(data);
            }

            // Luz blanca intensa
            float pulse = 0.8f + 0.2f * (float)Math.Sin(Projectile.timeLeft * 0.5f);
            Lighting.AddLight(Projectile.Center, new Vector3(0.8f * pulse, 0.9f * pulse, 1f * pulse));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // === SLASH EFFECT con crescent texturizado ===
            // Dibuja el sprite Crescent con:
            // - Rotación para orientarse según la dirección del swing
            // - Scale asimétrico (X != Y) para formar el arco curvo
            // - Additive blending para brillar
            try
            {
                Texture2D crescentTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Crescent").Value;
                if (crescentTex == null) return false;

                Texture2D glowTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;

                // Progreso de vida (1.0 = recién nacido, 0.0 = muriendo)
                float lifeProgress = Projectile.timeLeft / 30f;
                float pulse = 0.7f + 0.3f * lifeProgress;

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                // 1. Glow de fondo (radial, scale uniforme)
                if (glowTex != null)
                {
                    Color glowColor = new Color(100, 200, 255, (byte)(120 * pulse));
                    Main.spriteBatch.Draw(glowTex,
                        Projectile.Center - Main.screenPosition,
                        null,
                        glowColor,
                        0f,
                        new Vector2(glowTex.Width / 2f, glowTex.Height / 2f),
                        1.2f * pulse,
                        SpriteEffects.None, 0f);
                }

                // 2. Crescent principal con scale asimétrico (X > Y para alargar)
                float scaleX = 2.0f * pulse;
                float scaleY = 0.8f * pulse;
                Color slashColor = new Color(255, 255, 255, (byte)(255 * pulse));
                Main.spriteBatch.Draw(crescentTex,
                    Projectile.Center - Main.screenPosition,
                    null,
                    slashColor,
                    Projectile.rotation,
                    new Vector2(crescentTex.Width / 2f, crescentTex.Height / 2f),
                    new Vector2(scaleX, scaleY),
                    SpriteEffects.None, 0f);

                // 3. Crescent secundario (más pequeño, cyan, rotación inversa)
                Color slash2Color = new Color(100, 220, 255, (byte)(180 * pulse));
                Main.spriteBatch.Draw(crescentTex,
                    Projectile.Center - Main.screenPosition,
                    null,
                    slash2Color,
                    -Projectile.rotation * 0.7f,
                    new Vector2(crescentTex.Width / 2f, crescentTex.Height / 2f),
                    new Vector2(scaleX * 0.7f, scaleY * 0.7f),
                    SpriteEffects.None, 0f);

                // 4. Núcleo blanco brillante
                if (glowTex != null)
                {
                    Main.spriteBatch.Draw(glowTex,
                        Projectile.Center - Main.screenPosition,
                        null,
                        new Color(255, 255, 255, (byte)(200 * pulse)),
                        0f,
                        new Vector2(glowTex.Width / 2f, glowTex.Height / 2f),
                        0.4f * pulse,
                        SpriteEffects.None, 0f);
                }

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
            return false; // no dibujar sprite vanilla
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Explosión de sparks estilo Calamity
            for (int i = 0; i < 20; i++)
            {
                float angle = (MathHelper.TwoPi / 20) * i;
                Vector2 dir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 6f;
                var data = new ParticleData
                {
                    Position = target.Center,
                    Velocity = dir,
                    Scale = new Vector2(1f),
                    PackedColor = ParticleManager.PackColor(new Color(255, 255, 255)),
                    PackedStartColor = ParticleManager.PackColor(new Color(255, 255, 255)),
                    PackedEndColor = ParticleManager.PackColor(new Color(100, 200, 255)),
                    Rotation = angle,
                    RotationSpeed = 0.4f,
                    TimeLeft = 25,
                    Duration = 25,
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
