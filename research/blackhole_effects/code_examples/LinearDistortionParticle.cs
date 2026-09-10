using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Assets;
using NoxusBoss.Core.Graphics.ArbitraryScreenDistortion;
using Terraria;

namespace NoxusBoss.Content.Particles;

public class LinearDistortionParticle : Particle
{
    /// <summary>
    /// The ideal scale of this particle.
    /// </summary>
    public Vector2 IdealScale;

    public override string AtlasTextureName => "NoxusBoss.SmallSmokeParticle.png";

    public LinearDistortionParticle(Vector2 position, Vector2 velocity, Vector2 startingScale, Vector2 idealScale, int lifetime)
    {
        Position = position;
        Velocity = velocity;
        Scale = startingScale;
        IdealScale = idealScale;
        Lifetime = lifetime;
    }

    public override void Update()
    {
        Opacity = InverseLerp(0.95f, 0.6f, LifetimeRatio);
        Scale = Vector2.Lerp(Scale, IdealScale, 0.06f);

        Rotation += 0.9f / (Time + 3f);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        ArbitraryScreenDistortionSystem.QueueDistortionAction(() =>
        {
            Texture2D distortionTexture = GennedAssets.Textures.Extra.RadialDistortion.Value;
            Vector2 drawPosition = Position - Main.screenPosition;
            Color color = Color.White;
            color.G = (byte)(color.G * Opacity);
            Vector2 scale = Scale / distortionTexture.Size();

            Main.spriteBatch.Draw(distortionTexture, drawPosition, null, color, Rotation, distortionTexture.Size() * 0.5f, scale, 0, 0f);
        });
    }
}
