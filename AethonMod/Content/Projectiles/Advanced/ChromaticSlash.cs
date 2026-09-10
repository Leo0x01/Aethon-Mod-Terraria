using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Particles;

namespace AethonMod.Content.Projectiles.Advanced
{
    /// <summary>
    /// ChromaticSlash — slash effect con aberración cromática.
    /// Combina:
    /// - Crescent texturizado con scale asimétrico (técnica Calamity)
    /// - 3 crescents desplazados en rojo/cyan/magenta (aberración cromática simulada sin shader)
    /// - Trail continuo con oldPos[]
    /// - Partículas Star del ParticleManager
    /// - Additive blending
    /// </summary>
    public class ChromaticSlash : ModProjectile
    {
        public override void SetStaticDefaults() { Main.projFrames[Projectile.type] = 1; }

        public override void SetDefaults()
        {
            Projectile.width = 80;
            Projectile.height = 80;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = 5;
            Projectile.timeLeft = 40;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            // Rotación rápida del slash
            Projectile.rotation += 0.5f;

            // === PARTÍCULAS DEL PARTICLE MANAGER ===
            if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(2))
            {
                Vector2 offset = new Vector2(
                    Main.rand.NextFloat(-35f, 35f),
                    Main.rand.NextFloat(-35f, 35f));
                var data = new ParticleData
                {
                    Position = Projectile.Center + offset,
                    Velocity = offset * -0.04f,
                    Scale = new Vector2(0.6f),
                    PackedColor = ParticleManager.PackColor(Color.White),
                    Rotation = 0f,
                    RotationSpeed = 0.4f,
                    TimeLeft = 18,
                    Duration = 18,
                    TextureId = 2, // Star
                    BlendMode = 1,
                    LayerPriority = LayerPriorities.AfterProjectiles,
                };
                data.EnableComponent(ComponentFlag.FadeOut);
                data.EnableComponent(ComponentFlag.ScaleDown);
                ParticleManager.Spawn(data);
            }

            // Luz blanca intensa pulsante
            float pulse = 0.8f + 0.2f * (float)Math.Sin(Projectile.timeLeft * 0.5f);
            Lighting.AddLight(Projectile.Center, new Vector3(0.9f * pulse, 0.95f * pulse, 1f * pulse));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                float t = Main.GameUpdateCount;
                float lifeProgress = Projectile.timeLeft / 40f;
                float pulse = 0.6f + 0.4f * lifeProgress;

                Texture2D crescentTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/Crescent").Value;
                Texture2D glowTex = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                // === 1. GLOW DE FONDO ===
                if (glowTex != null)
                {
                    Main.spriteBatch.Draw(glowTex,
                        Projectile.Center - Main.screenPosition, null,
                        new Color(255, 255, 255, (byte)(100 * pulse)),
                        0f,
                        new Vector2(glowTex.Width / 2f, glowTex.Height / 2f),
                        1.0f * pulse, SpriteEffects.None, 0f);
                }

                // === 2. ABERRACIÓN CROMÁTICA ===
                // 3 crescents desplazados en rojo/cyan/magenta
                if (crescentTex != null)
                {
                    float scaleX = 2.2f * pulse;
                    float scaleY = 0.9f * pulse;

                    // Rojo (desplazado izquierda)
                    Main.spriteBatch.Draw(crescentTex,
                        Projectile.Center - Main.screenPosition - new Vector2(3f, 0f), null,
                        new Color(255, 0, 0, (byte)(120 * pulse)),
                        Projectile.rotation,
                        new Vector2(crescentTex.Width / 2f, crescentTex.Height / 2f),
                        new Vector2(scaleX, scaleY), SpriteEffects.None, 0f);

                    // Cyan (desplazado derecha)
                    Main.spriteBatch.Draw(crescentTex,
                        Projectile.Center - Main.screenPosition + new Vector2(3f, 0f), null,
                        new Color(0, 255, 255, (byte)(120 * pulse)),
                        Projectile.rotation,
                        new Vector2(crescentTex.Width / 2f, crescentTex.Height / 2f),
                        new Vector2(scaleX, scaleY), SpriteEffects.None, 0f);

                    // Magenta (desplazado arriba)
                    Main.spriteBatch.Draw(crescentTex,
                        Projectile.Center - Main.screenPosition - new Vector2(0f, 3f), null,
                        new Color(255, 0, 255, (byte)(120 * pulse)),
                        Projectile.rotation,
                        new Vector2(crescentTex.Width / 2f, crescentTex.Height / 2f),
                        new Vector2(scaleX, scaleY), SpriteEffects.None, 0f);

                    // Crescent principal blanco (centro)
                    Main.spriteBatch.Draw(crescentTex,
                        Projectile.Center - Main.screenPosition, null,
                        new Color(255, 255, 255, (byte)(200 * pulse)),
                        Projectile.rotation,
                        new Vector2(crescentTex.Width / 2f, crescentTex.Height / 2f),
                        new Vector2(scaleX, scaleY), SpriteEffects.None, 0f);

                    // Crescent secundario cyan (rotación inversa, más pequeño)
                    Main.spriteBatch.Draw(crescentTex,
                        Projectile.Center - Main.screenPosition, null,
                        new Color(150, 220, 255, (byte)(150 * pulse)),
                        -Projectile.rotation * 0.6f,
                        new Vector2(crescentTex.Width / 2f, crescentTex.Height / 2f),
                        new Vector2(scaleX * 0.6f, scaleY * 0.6f), SpriteEffects.None, 0f);
                }

                // === 3. NÚCLEO BLANCO BRILLANTE ===
                if (glowTex != null)
                {
                    Main.spriteBatch.Draw(glowTex,
                        Projectile.Center - Main.screenPosition, null,
                        new Color(255, 255, 255, (byte)(220 * pulse)),
                        0f,
                        new Vector2(glowTex.Width / 2f, glowTex.Height / 2f),
                        0.35f * pulse, SpriteEffects.None, 0f);
                }

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode == NetmodeID.Server) return;

            // Explosión con aberración cromática: 3 anillos de colores
            for (int color = 0; color < 3; color++)
            {
                Color c = color switch
                {
                    0 => new Color(255, 0, 0),      // rojo
                    1 => new Color(0, 255, 255),     // cyan
                    _ => new Color(255, 0, 255),      // magenta
                };
                for (int i = 0; i < 8; i++)
                {
                    float angle = (MathHelper.TwoPi / 8) * i + color * 0.3f;
                    Vector2 dir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 5f;
                    var data = new ParticleData
                    {
                        Position = target.Center,
                        Velocity = dir,
                        Scale = new Vector2(0.8f),
                        PackedColor = ParticleManager.PackColor(c),
                        Rotation = angle,
                        RotationSpeed = 0.3f,
                        TimeLeft = 25,
                        Duration = 25,
                        TextureId = 2, // Star
                        BlendMode = 1,
                        LayerPriority = LayerPriorities.AfterProjectiles,
                    };
                    data.EnableComponent(ComponentFlag.FadeOut);
                    data.EnableComponent(ComponentFlag.ScaleDown);
                    ParticleManager.Spawn(data);
                }
            }
        }
    }
}
