using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.ArbitraryScreenDistortion;
using Terraria;

namespace NoxusBoss.Content.Particles;

public class CircularSuctionParticle : Particle
{
    /// <summary>
    /// The entity this particle should follow.
    /// </summary>
    public Entity FollowEntity
    {
        get;
        set;
    }

    /// <summary>
    /// The color of bloom for this particles.
    /// </summary>
    public Color BloomColor
    {
        get;
        set;
    }

    /// <summary>
    /// The bloom texture.
    /// </summary>
    public static AtlasTexture BloomTexture
    {
        get;
        private set;
    }

    public override BlendState BlendState => BlendState.Additive;

    public override string AtlasTextureName => "NoxusBoss.BasicMetaballCircle.png";

    public CircularSuctionParticle(Vector2 position, Vector2 velocity, Color color, Color bloomColor, float scale, Entity followEntity, int lifeTime)
    {
        Position = position;
        Velocity = velocity;
        DrawColor = color;
        BloomColor = bloomColor;
        Scale = Vector2.One * scale;
        FollowEntity = followEntity;
        Lifetime = lifeTime;
    }

    public override void Update()
    {
        if ((!FollowEntity.active || Position.WithinRange(FollowEntity.Center, 98f)) && Time < Lifetime)
        {
            Time = Lifetime;
            return;
        }

        Vector2 hoverDestination = FollowEntity.Center;

        if (Position.WithinRange(hoverDestination, 180f))
            Scale *= 0.84f;
        Velocity += Position.SafeDirectionTo(hoverDestination) * (Time * 0.026f + 0.67f);
        Velocity = Velocity.RotateTowards(Position.AngleTo(hoverDestination), 0.09f);

        Opacity = InverseLerp(0f, 12f, Time).Squared();
        Rotation = Velocity.ToRotation();
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        BloomTexture ??= AtlasManager.GetTexture("NoxusBoss.StrongBloom.png");

        Vector2 scaleFactor = new Vector2(1f + Velocity.Length() * 0.04f, 0.7f - Velocity.Length() * 0.01f);
        PrimitivePixelationSystem.RenderToPrimsNextFrame(() =>
        {
            spriteBatch.Draw(BloomTexture, (Position - Main.screenPosition) * 0.5f, null, (BloomColor with { A = 0 }) * Opacity, Rotation, null, Scale * scaleFactor * 0.9f, 0);

            for (int i = 0; i < 2; i++)
                spriteBatch.Draw(Texture, (Position - Main.screenPosition) * 0.5f, null, (DrawColor with { A = 0 }) * Opacity, Rotation, null, Scale * scaleFactor, 0);
        }, PixelationPrimitiveLayer.AfterProjectiles);

        ArbitraryScreenDistortionSystem.QueueDistortionExclusionAction(() =>
        {
            spriteBatch.Draw(Texture, Position - Main.screenPosition, null, DrawColor * Opacity, Rotation, null, Scale * scaleFactor * 2f, 0);
        });
    }
}
