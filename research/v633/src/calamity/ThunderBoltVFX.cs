using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityMod.Particles
{
    public class ThunderBoltVFX : Particle //Also check out Split mod!
    {
        public override string Texture => "CalamityMod/Particles/ThunderBolt";
        public override bool UseAdditiveBlend => true;
        public override bool UseCustomDraw => true;
        public override bool SetLifetime => true;

        /// <summary>
        /// The function that decides to follow a specific <see cref="Vector2"/>.
        /// </summary>
        public Func<Vector2> FollowFunction;

        /// <summary>
        /// The opacity of this particle, can only go from 0 to 1.<br/>
        /// Defaults to 1.
        /// </summary>
        public float Opacity
        {
            get => opacity;
            set => opacity = Math.Clamp(value, 0f, 1f);
        }
        private float opacity = 1f;

        /// <summary>
        /// The amount of shaking this particle will have.<br/>
        /// Will always be positive.<br/>
        /// Defaults to 0.
        /// </summary>
        public float ShakePower
        {
            get => shakePower;
            set => shakePower = Math.Abs(value);
        }
        private float shakePower;

        /// <summary>
        /// The squish of this particle, how much is streched, widened, distorted, etc.<br/>
        /// For standard purposes, it defaults to <see cref="Vector2.One"/>.
        /// </summary>
        public Vector2 Squish = Vector2.One;

        /// <summary>
        /// A constructor that spawns a particle that does not move from where it spawns.
        /// </summary>
        public ThunderBoltVFX(Vector2 position, float rotation, float scale, Color color, int lifeTime, float shakePower, float opacity = 1f, Vector2? squish = null)
        {
            Position = position;
            Rotation = rotation;
            Scale = scale;
            Color = color;
            Opacity = opacity;
            Lifetime = lifeTime;
            ShakePower = shakePower;
            if (squish != null)
                Squish = (Vector2)squish;
        }

        /// <summary>
        /// A constructor that spawns a particle that follows a specific <see cref="Vector2"/>.<br/>
        /// For simple one-line positions, you can just write in the argument, for example:<br/>
        /// <br/>
        /// <code>() => Projectile.Center</code>
        /// For more complicated logic to decide which <see cref="Vector2"/> it should follow,<br/>
        /// you'd just make your own function which returns a <see cref="Vector2"/> and give it as an argument.
        /// </summary>
        public ThunderBoltVFX(Func<Vector2> followFunction, float rotation, float scale, Color color, int lifeTime, float shakePower, float opacity = 1f, Vector2? squish = null)
        {
            FollowFunction = followFunction;
            Rotation = rotation;
            Scale = scale;
            Color = color;
            Opacity = opacity;
            Lifetime = lifeTime;
            ShakePower = shakePower;
            if (squish != null)
                Squish = (Vector2)squish;
        }

        public override void Update()
        {
            Lighting.AddLight(Position, Color.ToVector3() * 3f);
            float fadeFactor = 1f - 0.05f * MathHelper.Clamp((Time - 10) / 10f, 0f, 1f);
            Opacity *= fadeFactor;
            Squish.X *= fadeFactor;
            if (FollowFunction is not null) Position = FollowFunction.Invoke();
        }

        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 Shake = Vector2.One.RotatedByRandom(MathHelper.TwoPi) * (1 - (Time / (float)Lifetime)) * ShakePower;

            //The draw happens at the base of the texture
            Vector2 Origin = new Vector2(tex.Width / 2f, tex.Height);

            Color drawColor = Color.Lerp(Color.White, Color, (Time / (float)Lifetime));

            SpriteEffects flip = (Main.GlobalTimeWrappedHourly % 30 < 15) ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            spriteBatch.Draw(tex, Position + Shake - Main.screenPosition, null, Color * Opacity * 0.6f, Rotation, Origin, Squish * Scale, flip, 0);
            spriteBatch.Draw(tex, Position - Main.screenPosition, null, drawColor * Opacity, Rotation, Origin, Squish * Scale, flip, 0);
        }
    }
}
