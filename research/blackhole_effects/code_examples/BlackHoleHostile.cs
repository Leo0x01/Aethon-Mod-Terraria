using Luminance.Assets;
using Luminance.Common.DataStructures;
using Luminance.Common.Easings;
using Microsoft.Xna.Framework;
using NoxusBoss.Assets;
using NoxusBoss.Core.AdvancedProjectileOwnership;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using NoxusBoss.Core.SoundSystems;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity.Projectiles;

public class BlackHoleHostile : ModProjectile, IProjOwnedByBoss<NamelessDeityBoss>
{
    /// <summary>
    /// The whirr sound instance of this black hole.
    /// </summary>
    public LoopedSoundInstance BrrrrrSound
    {
        get;
        private set;
    }

    /// <summary>
    /// How long this black hole has existed for, in frames.
    /// </summary>
    public ref float Time => ref Projectile.ai[0];

    /// <summary>
    /// How long this black hole should exist before dying.
    /// </summary>
    public ref float Lifetime => ref Projectile.ai[2];

    public override string Texture => MiscTexturesRegistry.InvisiblePixelPath;

    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 750;

    public override void SetDefaults()
    {
        Projectile.width = 480;
        Projectile.height = 480;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.hostile = true;
        Projectile.timeLeft = 999999;
        Projectile.netImportant = true;
    }

    public override void AI()
    {
        // No Nameless Deity? Die.
        if (!Projectile.TryGetGenericOwner(out NPC nameless) || nameless.ModNPC is not NamelessDeityBoss namelessModNPC)
        {
            Projectile.Kill();
            return;
        }

        // Grow over time.
        float expand = EasingCurves.Quartic.Evaluate(EasingType.In, InverseLerp(0f, 12f, Time));
        float contract = Pow(InverseLerp(0f, 25f, Lifetime - Time), 0.75f);
        Projectile.scale = expand * contract * 1.5f;
        if (Time >= Lifetime)
            Projectile.Kill();

        // Accelerate towards the target.
        NPCAimedTarget target = nameless.GetTargetData();

        if (!Projectile.WithinRange(target.Center, 380f) && Time >= 56f)
        {
            Vector2 force = Projectile.SafeDirectionTo(target.Center) * NamelessDeityBoss.CrushStarIntoQuasar_QuasarAcclerationForce * 6.67f;

            // Apply difficulty-specific balancing.
            if (CommonCalamityVariables.RevengeanceModeActive)
                force *= 1.1765f;

            // GFB? Die.
            if (Main.zenithWorld)
                force *= 3.3f;

            Projectile.velocity += force * namelessModNPC.DifficultyFactor;

            // Make the black hole go far faster if it's moving away from the target.
            if (Vector2.Dot(Projectile.SafeDirectionTo(target.Center), Projectile.velocity) < 0f)
                Projectile.velocity += force * namelessModNPC.DifficultyFactor * 1.3f;

            // Zip towards the target if they're not moving much.
            if (target.Velocity.Length() <= 4f)
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(target.Center) * 22f, 0.08f);
        }

        // Enforce a hard limit on the velocity.
        Projectile.velocity = Projectile.velocity.ClampLength(0f, NamelessDeityBoss.CrushStarIntoQuasar_QuasarMaxSpeed * Pow(namelessModNPC.DifficultyFactor, 1.2f));

        // Rotate forward.
        Projectile.rotation = Projectile.rotation.AngleLerp(Projectile.velocity.X * 0.032f, 0.24f);

        // Start the loop sound on the first frame.
        if (BrrrrrSound is null)
        {
            SoundStyle startSound = GennedAssets.Sounds.NamelessDeity.QuasarLoopStart with { Volume = 1.2f };
            SoundStyle loopSound = GennedAssets.Sounds.NamelessDeity.QuasarLoop with { Volume = 1.2f };
            BrrrrrSound = LoopedSoundManager.CreateNew(startSound, loopSound, () =>
            {
                return !Projectile.active;
            });
        }

        // Update the loop sound.
        BrrrrrSound.Update(Projectile.Center, sound =>
        {
            sound.Volume = contract;
        });

        Time++;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        // Create a sucking effect over the black hole.
        float suckPulse = 1f - Main.GlobalTimeWrappedHourly * 4f % 1f;
        float suckRotation = Main.GlobalTimeWrappedHourly * -3f;
        Color suckColor = Color.Wheat * InverseLerpBump(0.05f, 0.25f, 0.67f, 1f, suckPulse) * Projectile.Opacity * 0.2f;
        suckColor.A = 0;

        Main.spriteBatch.Draw(ChromaticBurst, Projectile.Center - Main.screenPosition, null, suckColor, suckRotation, ChromaticBurst.Size() * 0.5f, Vector2.One * suckPulse * 2.6f, 0, 0f);

        return false;
    }

    // Prevent cheap hits if the quasar happens to spawn near a player at first.
    public override bool? CanDamage() => Time >= 48f && Projectile.Opacity >= 0.8f;

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
        CircularHitboxCollision(Projectile.Center, Projectile.width * 0.45f, targetHitbox);

    public override void OnKill(int timeLeft)
    {
        NamelessDeityBoss.CreateTwinkle(Projectile.Center, Vector2.One * 2f);
        RadialScreenShoveSystem.Start(Projectile.Center, 72);
    }
}
