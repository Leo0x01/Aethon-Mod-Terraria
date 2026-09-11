using Luminance.Assets;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Assets;
using NoxusBoss.Content.Items.Pets;
using NoxusBoss.Core.Graphics.Automators;
using NoxusBoss.Core.Graphics.HeatDistortion;
using NoxusBoss.Core.Graphics.RenderTargets;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Projectiles.Pets;

public class StarPet : ModProjectile, IDrawsWithShader
{
    public Player Owner => Main.player[Projectile.owner];

    public ref float Time => ref Projectile.ai[0];

    public static InstancedRequestableTarget StarDrawContents
    {
        get;
        private set;
    }

    public override string Texture => MiscTexturesRegistry.InvisiblePixelPath;

    public override void SetStaticDefaults()
    {
        Main.projPet[Projectile.type] = true;
        ProjectileID.Sets.LightPet[Projectile.type] = true;

        // Register the star target.
        if (Main.netMode != NetmodeID.Server)
        {
            StarDrawContents = new InstancedRequestableTarget();
            Main.ContentThatNeedsRenderTargets.Add(StarDrawContents);
        }
    }

    public override void SetDefaults()
    {
        Projectile.width = 92;
        Projectile.height = 92;
        Projectile.friendly = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 90000;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
    }

    public override void AI()
    {
        CheckActive();

        // Hover near the owner.
        Vector2 hoverDestination = Owner.Center + new Vector2(Owner.direction * -50f, Cos01(Time * 0.056f) * -30f - 36f);
        Projectile.SmoothFlyNear(hoverDestination, 0.199f, 0.81f);

        // Emit a lot of light.
        Lighting.AddLight(Projectile.Center, Vector3.One * 3.2f);

        Time++;
    }

    public void CheckActive()
    {
        // Keep the projectile from disappearing as long as the player isn't dead and has the pet buff.
        if (!Owner.dead && Owner.HasBuff(Starseed.BuffID))
            Projectile.timeLeft = 2;
    }

    public void DrawSelf(Vector2 drawPosition)
    {
        // Prepare the sprite batch for shaders.
        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);

        // Draw a bloom backglow.
        Main.spriteBatch.Draw(BloomCircleSmall, drawPosition, null, (Color.Yellow with { A = 0 }) * Projectile.Opacity * 0.7f, 0f, BloomCircleSmall.Size() * 0.5f, Projectile.scale * 0.95f, 0, 0f);
        Main.spriteBatch.Draw(BloomCircleSmall, drawPosition, null, (Color.Red with { A = 0 }) * Projectile.Opacity * 0.45f, 0f, BloomCircleSmall.Size() * 0.5f, Projectile.scale * 1.61f, 0, 0f);

        // Draw a radial shine.
        ManagedShader shineShader = ShaderManager.GetShader("NoxusBoss.RadialShineShader");
        shineShader.Apply();
        Vector2 shineScale = Vector2.One * Projectile.width * Projectile.scale * 2.72f / WavyBlotchNoise.Size();
        Main.spriteBatch.Draw(WavyBlotchNoise, drawPosition, null, new Color(252, 212, 112) * 0.24f, Projectile.rotation, WavyBlotchNoise.Size() * 0.5f, shineScale, 0, 0f);

        // Draw the sun.
        ManagedShader fireballShader = ShaderManager.GetShader("NoxusBoss.SunShader");
        fireballShader.TrySetParameter("coronaIntensityFactor", 0.05f);
        fireballShader.TrySetParameter("mainColor", new Color(255, 255, 255));
        fireballShader.TrySetParameter("darkerColor", new Color(204, 92, 25));
        fireballShader.TrySetParameter("subtractiveAccentFactor", new Color(181, 0, 0));
        fireballShader.TrySetParameter("sphereSpinTime", Main.GlobalTimeWrappedHourly * 0.9f);
        fireballShader.SetTexture(WavyBlotchNoise, 1, SamplerState.LinearWrap);
        fireballShader.SetTexture(GennedAssets.Textures.Extra.PsychedelicWingTextureOffsetMap, 2, SamplerState.LinearWrap);
        fireballShader.Apply();
        Vector2 scale = Vector2.One * Projectile.width * Projectile.scale * 1.5f / DendriticNoiseZoomedOut.Size();
        Main.spriteBatch.Draw(DendriticNoiseZoomedOut, drawPosition, null, Color.White, Projectile.rotation, DendriticNoiseZoomedOut.Size() * 0.5f, scale, 0, 0f);

        Main.spriteBatch.End();
    }

    public void DrawWithShader(SpriteBatch spriteBatch)
    {
        StarDrawContents.Request(384, 384, Projectile.whoAmI, () =>
        {
            DrawSelf(Vector2.One * 192f);
        });

        // If the star drawer is ready, draw it to the screen.
        // If a dye is in use, apply it.
        if (!StarDrawContents.TryGetTarget(Projectile.whoAmI, out RenderTarget2D? target) || target is null)
            return;

        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        DrawData targetData = new DrawData(target, drawPosition, target.Frame(), Color.White, 0f, target.Size() * 0.5f, 1f, 0, 0f);
        GameShaders.Armor.Apply(Owner.cLight, Projectile, targetData);
        targetData.Draw(Main.spriteBatch);

        Texture2D heatAura = BloomCircleSmall.Value;
        DrawData heatData = new DrawData(heatAura, drawPosition, null, new Color(255, 255, 255, 0), 0f, heatAura.Size() * 0.5f, 2.5f, 0, 0f);
        DrawData heatExclusionData = new DrawData(heatAura, drawPosition, null, new Color(255, 255, 255, 0), 0f, heatAura.Size() * 0.5f, 2f, 0, 0f);
        ScreenDataHeatDistortionSystem.HeatDistortionData.Enqueue(heatData);
        ScreenDataHeatDistortionSystem.ExclusionData.Enqueue(targetData);
        ScreenDataHeatDistortionSystem.ExclusionData.Enqueue(heatExclusionData);

        Main.spriteBatch.PrepareForShaders();
    }
}
