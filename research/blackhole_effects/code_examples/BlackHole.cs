using Luminance.Assets;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.Projectiles.Pets;
using NoxusBoss.Core.Autoloaders;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Items.Pets;

public class BlackHole : ModItem
{
    /// <summary>
    /// The buff ID associated with this black hole.
    /// </summary>
    public static int BuffID
    {
        get;
        private set;
    }

    public override string Texture => MiscTexturesRegistry.InvisiblePixelPath;

    public override void Load() => BuffID = PetBuffAutoloader.Create(Mod, "NoxusBoss/BlackHolePet", "BlackHolePetBuff");

    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 1;
        ItemID.Sets.ItemNoGravity[Type] = true;
    }

    public override void SetDefaults()
    {
        Item.CloneDefaults(ItemID.ZephyrFish);
        Item.shoot = ModContent.ProjectileType<BlackHolePet>();
        Item.buffType = BuffID;
        Item.master = true;
    }

    public override void UseStyle(Player player, Rectangle heldItemFrame)
    {
        if (player.whoAmI == Main.myPlayer && player.itemTime == 0)
            player.AddBuff(Item.buffType, 3600);
    }

    internal static void DrawBlackHole(Vector2 drawPosition, float blackRadius)
    {
        ManagedShader blackHoleShader = ShaderManager.GetShader("NoxusBoss.RealBlackHoleShader");
        blackHoleShader.TrySetParameter("blackHoleRadius", 0.3f);
        blackHoleShader.TrySetParameter("blackHoleCenter", Vector3.Zero);
        blackHoleShader.TrySetParameter("aspectRatioCorrectionFactor", 1f);
        blackHoleShader.TrySetParameter("accretionDiskColor", new Color(245, 105, 61).ToVector3()); // Blue: new Color(90, 126, 210).ToVector3()
        blackHoleShader.TrySetParameter("cameraAngle", 0.32f);
        blackHoleShader.TrySetParameter("cameraRotationAxis", new Vector3(1f, 0f, 0f));
        blackHoleShader.TrySetParameter("accretionDiskScale", new Vector3(1f, 0.2f, 1f));
        blackHoleShader.TrySetParameter("zoom", Vector2.One * blackRadius * 2.7f);
        blackHoleShader.TrySetParameter("accretionDiskRadius", 0.33f);
        blackHoleShader.SetTexture(FireNoiseB, 1, SamplerState.LinearWrap);
        blackHoleShader.Apply();

        Main.spriteBatch.Draw(InvisiblePixel, drawPosition, null, Color.Transparent, 0f, InvisiblePixel.Size() * 0.5f, 400f, 0, 0f);
    }

    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Immediate, null, null, null, null, null, Main.UIScaleMatrix);

        DrawBlackHole(position, 0.04f);

        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, Main.UIScaleMatrix);

        return false;
    }

    public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
    {
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Immediate, null, null, null, null, null, Main.GameViewMatrix.TransformationMatrix);

        DrawBlackHole(Item.position - Main.screenPosition, 0.07f);

        Main.spriteBatch.ResetToDefault();

        return false;
    }
}

