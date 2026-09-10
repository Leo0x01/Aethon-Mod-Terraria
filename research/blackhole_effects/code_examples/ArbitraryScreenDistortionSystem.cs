using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics.ArbitraryScreenDistortion;

[Autoload(Side = ModSide.Client)]
public class ArbitraryScreenDistortionSystem : ModSystem
{
    private static readonly Queue<Action> distortionActionQueue = [];

    private static readonly Queue<Action> distortionExclusionActionQueue = [];

    /// <summary>
    /// The render target which specifies distortions on the screen.
    /// </summary>
    public static ManagedRenderTarget DistortionTarget
    {
        get;
        private set;
    }

    /// <summary>
    /// The render target which specifies parts of the screen that should not be distorted, in spite of what's present in the <see cref="DistortionTarget"/>.
    /// </summary>
    public static ManagedRenderTarget DistortionExclusionTarget
    {
        get;
        private set;
    }

    public override void OnModLoad()
    {
        DistortionTarget = new(true, ManagedRenderTarget.CreateScreenSizedTarget);
        DistortionExclusionTarget = new(true, ManagedRenderTarget.CreateScreenSizedTarget);
        RenderTargetManager.RenderTargetUpdateLoopEvent += UpdateTargets;
    }

    private void UpdateTargets()
    {
        bool anyDistortionsInEffect = distortionActionQueue.Count >= 1 || distortionExclusionActionQueue.Count >= 1;
        GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
        if (anyDistortionsInEffect)
        {
            graphicsDevice.SetRenderTarget(DistortionTarget);
            graphicsDevice.Clear(Color.Transparent);

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            while (distortionActionQueue.TryDequeue(out Action? a))
                a?.Invoke();

            Main.spriteBatch.End();
        }

        if (anyDistortionsInEffect)
        {
            graphicsDevice.SetRenderTarget(DistortionExclusionTarget);
            graphicsDevice.Clear(Color.Transparent);

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            while (distortionExclusionActionQueue.TryDequeue(out Action? a))
                a?.Invoke();

            Main.spriteBatch.End();

            graphicsDevice.SetRenderTarget(null);
        }

        if (anyDistortionsInEffect)
        {
            ManagedScreenFilter distortionShader = ShaderManager.GetFilter("NoxusBoss.ScreenDistortionShader");
            distortionShader.SetTexture(DistortionTarget, 1, SamplerState.LinearClamp);
            distortionShader.SetTexture(DistortionExclusionTarget, 2, SamplerState.LinearClamp);
            distortionShader.Activate();
        }
    }

    /// <summary>
    /// Queues a given draw action for specification of distortion.
    /// </summary>
    public static void QueueDistortionAction(Action action) => distortionActionQueue.Enqueue(action);

    /// <summary>
    /// Queues a given draw action for specification of distortion exclusion.
    /// </summary>
    public static void QueueDistortionExclusionAction(Action action) => distortionExclusionActionQueue.Enqueue(action);
}
