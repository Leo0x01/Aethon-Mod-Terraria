// ============================================================================
// PUERTO DE INVESTIGACIÓN — Calamity: Hunt of the Old God
// (github.com/TohruKobayashi/CalamityHunt) · Team Hypnos:
// caligulasAquarium, IbanPlay, THEREDSTONEBRO, AmmyanDocCiesh, Blockaroz
// Licencia original: GPL-3.0 (ver LICENSE-GPL3.txt en esta carpeta).
// Copiado verbatim por petición del usuario para investigar al jefe
// GOOZMA; pendiente su decisión de re-licencia/modificación.
// ============================================================================

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using ReLogic.Content;

using Terraria.Graphics.Renderers;
using Terraria.ModLoader;

namespace AethonMod.Content.GoozmaPort.Common.Systems.Particles;

public interface IGoozParticle : IPooledParticle
{
    bool RequiresImmediateMode { get; }
}

public abstract class Particle<T> : ILoadable, IGoozParticle where T : Particle<T>
{
    private static ParticlePool<T> pool;

    protected virtual int MaxParticles => 200;

    public virtual bool ShouldBeRemovedFromRenderer { get; protected set; }

    public virtual bool IsRestingInPool { get; protected set; }

    public virtual string Texture => (GetType().FullName!).Replace('.', '/');

    public virtual bool RequiresImmediateMode => false;

    // ReSharper disable once StaticMemberInGenericType
    protected static Asset<Texture2D> TextureAsset { get; private set; }

    public Vector2 Position;
    public Vector2 Velocity;
    public Vector2 Acceleration;
    public float Rotation;
    public float RotationVelocity;
    public Vector2 Scale = Vector2.One;
    public Vector2 ScaleVelocity;
    public Color Color = Color.White;

    void ILoadable.Load(Mod mod)
    {
        pool = new ParticlePool<T>(MaxParticles, NewInstance);

        // Pre-load it.
        TextureAsset = ModContent.Request<Texture2D>(Texture);
    }

    void ILoadable.Unload() { }

    void IParticle.Update(ref ParticleRendererSettings settings)
    {
        Update();
    }

    void IParticle.Draw(ref ParticleRendererSettings settings, SpriteBatch spritebatch)
    {
        Draw(spritebatch);
    }

    public virtual void OnSpawn() { }

    protected virtual void Update()
    {
        Velocity += Acceleration;
        Position += Velocity;
        RotationVelocity += RotationVelocity;
        Scale += ScaleVelocity;
    }

    protected virtual void Draw(SpriteBatch sb) { }

    public virtual void RestInPool()
    {
        IsRestingInPool = true;
    }

    public virtual void FetchFromPool()
    {
        IsRestingInPool = false;
        ShouldBeRemovedFromRenderer = false;

        Position = Vector2.Zero;
        Velocity = Vector2.Zero;
        Acceleration = Vector2.Zero;
        Rotation = 0f;
        RotationVelocity = 0f;
        Scale = Vector2.One;
        Color = Color.White;
    }

    protected abstract T NewInstance();

    public static T RequestParticle()
    {
        return pool.RequestParticle();
    }
}
