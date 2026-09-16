using FargowiltasSouls.Assets.ExtraTextures;
using FargowiltasSouls.Content.Projectiles.Deathrays;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargowiltasSouls.Content.Projectiles.BossWeapons
{
	public class ElectricWhipLightning : BaseDeathray
    {

        public ref float Length => ref Projectile.ai[2];
        public ElectricWhipLightning() : base(30, drawDistance: 3500) { }

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();

            // DisplayName.SetDefault("Love Ray");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            CooldownSlot = -1;
            Projectile.hostile = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.SummonMeleeSpeed;

            Projectile.FargoSouls().CanSplit = false;
            //Projectile.FargoSouls().TimeFreezeImmune = true;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;

            Projectile.hide = true;
        }

        public override void AI()
        {
            Vector2? vector78 = null;
            if (Projectile.velocity.HasNaNs() || Projectile.velocity == Vector2.Zero)
            {
                Projectile.velocity = -Vector2.UnitY;
            }

            if (Projectile.velocity.HasNaNs() || Projectile.velocity == Vector2.Zero)
            {
                Projectile.velocity = -Vector2.UnitY;
            }
            if (Projectile.localAI[0] == 0f)
            {
                SoundEngine.PlaySound(SoundID.Item12, Projectile.Center);
                //SoundEngine.PlaySound(SoundID.Zombie104, Projectile.Center);
            }
            float num801 = 0.5f;
            Projectile.localAI[0] += 1f;
            Projectile.Opacity = 1f;
            float threshold = maxTime - 10;
            if (Projectile.localAI[0] >= threshold)
            {
                Projectile.Opacity = (Projectile.localAI[0] - threshold) / (maxTime - threshold);
            }
            if (Projectile.localAI[0] >= maxTime)
            {
                Projectile.Kill();
                return;
            }
            Projectile.scale = (float)Math.Sin(Projectile.localAI[0] * 3.14159274f / maxTime) * 3f * num801;
            if (Projectile.scale > num801)
                Projectile.scale = num801;
            //if (Projectile.localAI[0] > maxTime / 2)
                //Projectile.Opacity = MathHelper.Clamp(Projectile.scale, 0f, 1f);

            float num804 = Projectile.velocity.ToRotation();
            num804 += Projectile.ai[0];
            Projectile.rotation = num804 - 1.57079637f;
            Projectile.velocity = num804.ToRotationVector2();
            float num805 = 3f;
            float num806 = Projectile.width;
            Vector2 samplingPoint = Projectile.Center;
            if (vector78.HasValue)
            {
                samplingPoint = vector78.Value;
            }
            float[] array3 = new float[(int)num805];
            //Collision.LaserScan(samplingPoint, Projectile.velocity, num806 * Projectile.scale, 3000f, array3);
            for (int i = 0; i < array3.Length; i++)
                array3[i] = Length;
            float num807 = 0f;
            int num3;
            for (int num808 = 0; num808 < array3.Length; num808 = num3 + 1)
            {
                num807 += array3[num808];
                num3 = num808;
            }
            num807 /= num805;
            float amount = 0.5f;
            Projectile.localAI[1] = MathHelper.Lerp(Projectile.localAI[1], num807, amount);
            Vector2 vector79 = Projectile.Center + Projectile.velocity * (Projectile.localAI[1] - 14f);
            //DelegateMethods.v3_1 = new Vector3(0.3f, 0.65f, 0.7f);
            //Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * Projectile.localAI[1], (float)Projectile.width * Projectile.scale, new Utils.PerLinePoint(DelegateMethods.CastLight));

            Projectile.position -= Projectile.velocity;

        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Electrified, 120);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D lightningTexture = TextureAssets.Projectile[Type].Value;
            int lightningFrames = 5;
            Rectangle GetRandomLightningFrame()
            {
                int frameHeight = lightningTexture.Height / lightningFrames;
                int frame = Main.rand.Next(lightningFrames);
                return new(0, frameHeight * frame, lightningTexture.Width, frameHeight);
            }
            void DrawLightning(Vector2 position, Color color, float rotation)
            {
                Rectangle lightningRect = GetRandomLightningFrame();
                Vector2 lightningOrigin = lightningRect.Size() / 2f;
                Main.EntitySpriteDraw(lightningTexture, position - Main.screenPosition, lightningRect, color, rotation, lightningOrigin, 1f, SpriteEffects.None, 0);
            }

            //Texture2D texture = TextureAssets.Chain.Value;
            Vector2 position = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * Length;
            Vector2 mountedCenter = Projectile.Center;

            float texLength = lightningTexture.Height / lightningFrames;
            Vector2 vector24 = mountedCenter - position;
            float rotation = (float)Math.Atan2(vector24.Y, vector24.X) - 1.57f;
            bool flag = true;
            if (float.IsNaN(position.X) && float.IsNaN(position.Y))
                flag = false;
            if (float.IsNaN(vector24.X) && float.IsNaN(vector24.Y))
                flag = false;
            while (flag)
                if (vector24.Length() < texLength + 1.0)
                {
                    flag = false;
                }
                else
                {
                    Vector2 vector21 = vector24;
                    vector21.Normalize();
                    position += vector21 * texLength;
                    vector24 = mountedCenter - position;

                    DrawLightning(position, Color.White * Projectile.Opacity, rotation);

                }
            return false;
        }
    }
}