using Luminance.Core;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using NoxusBoss.Core.Configuration;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics.GeneralScreenEffects;

[Autoload(Side = ModSide.Client)]
public class GeneralScreenEffectSystem : ModSystem
{
    private static readonly List<GeneralScreenEffect> registeredEffects = [];

    internal static readonly List<GeneralScreenEffect> activeEffects = [];

    /// <summary>
    /// The screen effect responsible for chromatic aberration post-processing.
    /// </summary>
    public static readonly GeneralScreenEffect ChromaticAberration = Register(new("NoxusBoss.ChromaticAberrationShader", (filter, source, intensity) =>
    {
        if (WoTGConfig.Instance.PhotosensitivityMode || WoTGConfig.Instance.VisualOverlayIntensity <= 0f)
            return;

        filter.TrySetParameter("splitIntensity", intensity * ModContent.GetInstance<Config>().ScreenshakeModifier * 0.0004f);
        filter.TrySetParameter("impactPoint", source / Main.ScreenSize.ToVector2());
        filter.Activate();
    }));

    /// <summary>
    /// The screen effect responsible for high-contrast post-processing.
    /// </summary>
    public static readonly GeneralScreenEffect HighContrast = Register(new("NoxusBoss.HighContrastScreenShader", (filter, source, intensity) =>
    {
        if (WoTGConfig.Instance.PhotosensitivityMode || WoTGConfig.Instance.VisualOverlayIntensity <= 0f)
            return;

        float configIntensityInterpolant = intensity * InverseLerp(0f, 0.45f, WoTGConfig.Instance.VisualOverlayIntensity);
        filter.TrySetParameter("contrastMatrix", CalculateContrastMatrix(configIntensityInterpolant));
        filter.Activate();
    }));

    /// <summary>
    /// The screen effect responsible for radial-blur post-processing.
    /// </summary>
    public static readonly GeneralScreenEffect RadialBlur = Register(new("NoxusBoss.SourcedRadialMotionBlurShader", (filter, source, intensity) =>
    {
        if (WoTGConfig.Instance.PhotosensitivityMode || WoTGConfig.Instance.VisualOverlayIntensity <= 0f)
            return;

        filter.TrySetParameter("blurIntensity", intensity * ModContent.GetInstance<Config>().ScreenshakeModifier * 0.0009f);
        filter.TrySetParameter("sourcePosition", source / Main.ScreenSize.ToVector2());
        filter.Activate();
    }));

    public static Matrix CalculateContrastMatrix(float contrast)
    {
        // The way matrices work is as a means of creating linear transformations, such as squishes, rotations, scaling effects, etc.
        // Strictly speaking, however, they act as a sort of encoding for functions. The exact specifics of how this works is a bit too dense
        // to stuff into a massive code comment, but 3blue1brown's linear algebra series does an excellent job of explaining how they work:
        // https://www.youtube.com/watch?v=fNk_zzaMoSs&list=PLZHQObOWTQDPD3MizzM2xVFitgF8hE_ab

        // For this matrix, the "axes" are the RGBA channels, in that order.
        // Given that the matrix is somewhat sparse, it can be easy to represent the output equations for each color one-by-one.
        // For the purpose of avoiding verbose expressions, I will represent "oneOffsetContrast" as "c", and "inverseForce" as "f":

        // R = c * R + f * A
        // G = c * G + f * A
        // B = c * B + f * A
        // For the purposes of the screen shaders, A is always 1, so it's possible to rewrite things explicitly like so:
        // R = c * R + (1 - c) * 0.5
        // G = c * G + (1 - c) * 0.5
        // B = c * B + (1 - c) * 0.5

        // These are all linear equations with slopes that become increasingly sharp the greater c is. At a certain point (which can be trivially computed from c) the output
        // will be zero, and everything above or below that will race towards a large absolute value. The result of this is that color channels that are already strong are emphasized to their maximum
        // extent while color channels that are weak vanish into nothing, effectively increasing the contrast by a significant margin.
        // The reason the contrast needs to be offset by 1 is because inputs from 0-1 have the inverse effect, making the resulting colors more homogenous by bringing them closer to a neutral grey.
        // This effect could be useful to note for other contexts, but for the intended purposes of this shader it's easier to correct for this.
        float oneOffsetContrast = contrast + 1f;
        float inverseForce = (1f - oneOffsetContrast) * 0.5f;
        return new(
            oneOffsetContrast, 0f, 0f, 0f,
            0f, oneOffsetContrast, 0f, 0f,
            0f, 0f, oneOffsetContrast, 0f,
            inverseForce, inverseForce, inverseForce, 1f);
    }

    /// <summary>
    /// Registers a new effect for use by this system.
    /// </summary>
    public static GeneralScreenEffect Register(GeneralScreenEffect effect)
    {
        registeredEffects.Add(effect);
        return effect;
    }

    public override void PostUpdateEverything()
    {
        foreach (GeneralScreenEffect effect in activeEffects)
        {
            ManagedScreenFilter filter = ShaderManager.GetFilter(effect.ShaderKey);

            effect.Timer++;
            effect.PreparationAction(filter, effect.SourcePosition - Main.screenPosition, InverseLerp(effect.Duration, 1f, effect.Timer) * effect.IntensityFactor);
        }
        activeEffects.RemoveAll(e => e.Timer >= e.Duration);
    }
}
