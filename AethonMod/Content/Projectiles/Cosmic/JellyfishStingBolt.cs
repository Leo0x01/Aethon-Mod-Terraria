using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// v5.97 — JellyfishStingBolt — EL NEMATOCISTO de LA MEDUSA NEBULAR.
    ///
    /// Las células urticantes de una medusa cósmica: agujas de luz
    /// (destello de 4 puntas + estela breve) disparadas por la campana
    /// cuando se contrae junto a una víctima. Rápidas y directas; aplican
    /// QUEMADURA DE HIELO (Frostburn) — la quemadura fría del vacío, la
    /// firma del arsenal que ningún otro arma cósmica usa.
    /// </summary>
    public class JellyfishStingBolt : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 54;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = -1;
            Projectile.light = 0.35f;
            Projectile.extraUpdates = 1;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            // La aguja de luz gira hacia su dirección de vuelo.
            Projectile.rotation = Projectile.velocity.ToRotation();

            // Estela de polvo estelar breve (solo cliente).
            if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch,
                    -Projectile.velocity * 0.1f, 150,
                    Main.rand.NextBool(3)
                        ? new Color(110, 235, 255)
                        : new Color(255, 160, 220), 0.45f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // La quemadura fría del vacío.
            try { target.AddBuff(BuffID.Frostburn, 240); } catch { }
            if (Main.netMode == NetmodeID.Server) return;

            // Micro estallido de cristal al clavarse.
            for (int i = 0; i < 5; i++)
            {
                Dust d = Dust.NewDustPerfect(target.Center, DustID.BlueTorch,
                    new Vector2(Main.rand.NextFloat(-2.2f, 2.2f), Main.rand.NextFloat(-2.2f, 2.2f)),
                    200, new Color(150, 240, 255), 0.6f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Dibujado manual: destello de 4 puntas + núcleo (aditivo).
            try
            {
                Texture2D star = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/Star").Value;
                Texture2D glow = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                Vector2 drawPos = Projectile.Center - Main.screenPosition;

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                float lifeT = 1f - Projectile.timeLeft / 54f;
                float a = 1f - lifeT * lifeT * 0.5f;

                Main.spriteBatch.Draw(glow, drawPos, null,
                    new Color(120, 230, 255, (byte)(160 * a)), 0f,
                    new Vector2(glow.Width * 0.5f, glow.Height * 0.5f),
                    0.45f, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(star, drawPos, null,
                    new Color(235, 255, 255, (byte)(255 * a)), Projectile.rotation,
                    new Vector2(star.Width * 0.5f, star.Height * 0.5f),
                    0.85f, SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise,
                    null, Main.GameViewMatrix.TransformationMatrix);
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise,
                    null, Main.GameViewMatrix.TransformationMatrix);
            }
            return false;
        }
    }
}
