using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using static CalamityMod.CalamityUtils;

namespace CalamityMod.Particles
{
    public class CustomPulse : Particle
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public bool UseAltVisual = true;
        public override bool UseAdditiveBlend => UseAltVisual;
        public override bool SetLifetime => true;
        public override bool UseCustomDraw => true;

        private string NewTexture;
        private float OriginalScale;
        private float FinalScale;
        private float BaseOpacity;
        private float opacity;
        private bool FadeOut;
        private Vector2 Squish;
        private Color BaseColor;
        private float MakeLight;
        SpriteEffects Effects = SpriteEffects.None;

        public CustomPulse(Vector2 position, Vector2 velocity, Color color, string texture, Vector2 squish, float rotation, float originalScale, float finalScale, int lifeTime, bool UseAdditiveBlend = true, float baseOpacity = 1f, bool fade = true, float makeLight = 1, SpriteEffects effects = SpriteEffects.None)
        {
            Position = position;
            Velocity = velocity;
            BaseColor = color;
            NewTexture = texture;
            OriginalScale = originalScale;
            FinalScale = finalScale;
            Scale = originalScale;
            Lifetime = lifeTime;
            BaseOpacity = baseOpacity;
            FadeOut = fade;
            Squish = squish;
            Effects = effects;
            Rotation = rotation;
            UseAltVisual = UseAdditiveBlend;
            MakeLight = makeLight;
        }

        public override void Update()
        {
            float pulseProgress = PiecewiseAnimation(LifetimeCompletion, new CurveSegment[] { new CurveSegment(EasingType.PolyOut, 0f, 0f, 1f, 4) });
            Scale = MathHelper.Lerp(OriginalScale, FinalScale, pulseProgress);

            opacity = (FadeOut ? (float)Math.Sin(MathHelper.PiOver2 + LifetimeCompletion * MathHelper.PiOver2) : 1f) * BaseOpacity;

            Color = BaseColor * opacity;
            if (MakeLight > 0)
                Lighting.AddLight(Position, (Color.R / 255f) * MakeLight, (Color.G / 255f) * MakeLight, (Color.B / 255f) * MakeLight);
            Velocity *= 0.95f;
        }

        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            Texture2D tex = ModContent.Request<Texture2D>(NewTexture).Value;
            float scaleMult = 1;
            if (Main.zenithWorld)
            {
                DateTime day = DateTime.Now;
                if (day.DayOfWeek == DayOfWeek.Tuesday)
                {
                    Texture2D joke = ModContent.Request<Texture2D>("CalamityMod/Particles/MammothParticle").Value;
                    scaleMult = (MathHelper.Lerp(tex.Size().X / joke.Size().X, tex.Size().Y / joke.Size().Y, 0.5f));
                    tex = joke;
                    UseAltVisual = true;
                }
            }
            spriteBatch.Draw(tex, Position - Main.screenPosition, null, Color * opacity, Rotation, tex.Size() / 2f, Scale * Squish * scaleMult, Effects, 0);
        }
    }
}
