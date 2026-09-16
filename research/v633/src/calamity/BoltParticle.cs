using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityMod.Particles
{
    public class BoltParticle : Particle
    {
        public Color InitialColor;
        public bool AffectedByGravity;
        public bool FadeIn = false;
        public float FadeInScale = 0f;
        public bool GlowCenter = false;
        public Vector2 Stretch = new Vector2(0.5f, 1.6f);
        public float ShrinkSpeed = 0;
        public bool Fliped = false;
        public bool GlowFade = false;

        public override int FrameVariants => 3;
        public override string Texture => "CalamityMod/Particles/Bolt2";
        public override bool UseAdditiveBlend => true;
        public override bool SetLifetime => true;
        public override bool UseCustomDraw => true;

        public BoltParticle(Vector2 relativePosition, Vector2 velocity, bool affectedByGravity, int lifetime, float scale, Color color, Vector2 stretch, bool glowCenter = false, bool glowFade = false, bool fadeIn = false, float shrinkSpeed = 0)
        {
            Position = relativePosition;
            Velocity = velocity;
            AffectedByGravity = affectedByGravity;
            Scale = scale;
            Stretch = stretch;
            FadeInScale = scale;
            Lifetime = lifetime;
            Color = InitialColor = color;
            ShrinkSpeed = shrinkSpeed;
            GlowCenter = glowCenter;
            Variant = Main.rand.Next(3);
            Fliped = Main.rand.NextBool();

            FadeIn = fadeIn;

            if (FadeIn)
                Scale = 0f;

            GlowFade = glowFade;
        }

        public override void Update()
        {
            if (!FadeIn)
            {
                Scale *= 0.95f;
                Color = Color.Lerp(InitialColor, Color.Transparent, (float)Math.Pow(LifetimeCompletion, 3D));
            }
            else
            {
                if ((float)Time / (float)Lifetime < 0.5f)
                {
                    Scale = MathHelper.Lerp(Scale, FadeInScale, 0.2f);
                }
                else
                {
                    Scale = MathHelper.Lerp(Scale, FadeInScale, -0.21f);
                }
            }
            Velocity *= 0.95f;
            if (Velocity.Length() < 12f && AffectedByGravity)
            {
                Velocity.X *= 0.94f;
                Velocity.Y += 0.25f;
            }
            Rotation = Velocity.ToRotation() + MathHelper.PiOver2;

            Stretch.X *= (1 - 0.2f * ShrinkSpeed);
            Stretch.Y *= (1 + 0.2f * ShrinkSpeed);
        }

        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            Vector2 scale = Stretch * Scale;
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Rectangle frame = texture.Frame(1, 3, 0, Variant);
            Vector2 origin = frame.Size() * 0.5f;

            Color col = Color;

            spriteBatch.Draw(texture, Position - Main.screenPosition, frame, col, Rotation, origin, scale, Fliped ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
            if (GlowCenter)
                spriteBatch.Draw(texture, Position - Main.screenPosition, frame, Color.Lerp(col, Color.White, 0.8f) * (GlowFade ? Utils.GetLerpValue(Lifetime, 0, Time, true) : 1), Rotation, origin, scale * 0.8f, Fliped ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
        }
    }
}
