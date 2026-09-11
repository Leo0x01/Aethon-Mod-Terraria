using Luminance.Assets;
using Luminance.Common.Easings;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.Items.Pets;
using NoxusBoss.Core.Graphics.RenderTargets;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Projectiles.Pets;

public class BlackHolePet : ModProjectile
{
    /// <summary>
    /// The render target that contains all information for black holes rendered to the main menu screen.
    /// </summary>
    public static InstancedRequestableTarget MainMenuTarget
    {
        get;
        private set;
    }

    public Player Owner => Main.player[Projectile.owner];

    /// <summary>
    /// The timer used for the purposes of making this black hole pop out as it appears.
    /// </summary>
    public ref float VisualsTime => ref Projectile.ai[0];

    public override string Texture => MiscTexturesRegistry.InvisiblePixelPath;

    public override void SetStaticDefaults()
    {
        Main.projPet[Projectile.type] = true;
        ProjectileID.Sets.CharacterPreviewAnimations[Projectile.type] = ProjectileID.Sets.SimpleLoop(0, 1).
            WithOffset(new Vector2(-30f, 10f));

        MainMenuTarget = new InstancedRequestableTarget();
        Main.ContentThatNeedsRenderTargets.Add(MainMenuTarget);
    }

    public override void SetDefaults()
    {
        Projectile.width = 76;
        Projectile.height = 76;
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
        Vector2 hoverDestination = Owner.Center + new Vector2(Owner.direction * 50f, -36f);
        Projectile.velocity = (Projectile.velocity + Projectile.SafeDirectionTo(hoverDestination) * 0.3f).ClampLength(0f, 25f);
        if (Vector2.Dot(Projectile.velocity, Projectile.SafeDirectionTo(hoverDestination)) < 0f)
            Projectile.velocity *= 0.96f;

        // Fly away from the owner if too close, so that it doesn't obscure them with the distortion effects.
        Projectile.velocity -= Projectile.SafeDirectionTo(Owner.Center) * InverseLerp(145f, 70f, Projectile.Distance(Owner.Center)) * 2f;

        // Teleport near the player if they're very far away.
        if (!Projectile.WithinRange(Owner.Center, 2000f))
        {
            VisualsTime = 0f;
            Projectile.Center = Owner.Center - Vector2.UnitY * 150f;
            Projectile.velocity *= 0.05f;
            Projectile.netUpdate = true;
        }

        Projectile.rotation = Projectile.rotation.AngleLerp(Projectile.velocity.X * 0.04f, 0.3f);
        Projectile.scale = EasingCurves.Elastic.Evaluate(EasingType.Out, InverseLerp(0f, 120f, VisualsTime)) * Sqrt(InverseLerp(0f, 60f, VisualsTime));
        VisualsTime++;
    }

    public void CheckActive()
    {
        // Keep the projectile from disappearing as long as the player isn't dead and has the pet buff.
        if (!Owner.dead && Owner.HasBuff(BlackHole.BuffID))
            Projectile.timeLeft = 2;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        // This pet is rendered via screen shader by default.
        // However, this doesn't work for pet/character previews on the player select screen.
        // To account for this, the black hole is drawn manually if on the game menu.
        if (Main.gameMenu)
        {
            int identifier = Main.LocalPlayer.name.GetHashCode();
            Vector2 targetSize = Vector2.One * 384f;
            MainMenuTarget.Request((int)targetSize.X, (int)targetSize.Y, identifier, () =>
            {
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, CullOnlyScreen);
                BlackHole.DrawBlackHole(targetSize * 0.5f, 0.061f);
                Main.spriteBatch.End();
            });
            if (MainMenuTarget.TryGetTarget(identifier, out RenderTarget2D? target) && target is not null)
            {
                Main.EntitySpriteDraw(target, Projectile.Center - Main.screenPosition, null, Color.White, 0f, target.Size() * 0.5f, 1, 0);

                ManagedShader blackShader = ShaderManager.GetShader("NoxusBoss.BlackOnlyShader");
                blackShader.Apply();
                Main.spriteBatch.Draw(target, Projectile.Center - Main.screenPosition, null, Color.White, 0f, target.Size() * 0.5f, 1, 0, 0f);
            }
        }

        return false;
    }
}
