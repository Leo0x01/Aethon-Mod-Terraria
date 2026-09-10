using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.Custom
{
    /// <summary>
    /// CosmicOrbBolt — orbe de energía cósmica que deja estela arcoíris.
    /// Usa GlowOrb + GlowCircle rotando + dust arcoíris.
    /// </summary>
    public class CosmicOrbBolt : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 180;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            // Rotación del proyectil
            Projectile.rotation += 0.15f;

            // Estela arcoíris densa
            float hue = (Main.GameUpdateCount * 0.02f + Projectile.whoAmI * 0.1f) % 1f;
            Color c = Main.hslToRgb(hue, 1f, 0.5f);

            Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.RainbowTorch,
                -Projectile.velocity * 0.15f + new Vector2(
                    Main.rand.NextFloat(-1.5f, 1.5f), Main.rand.NextFloat(-1.5f, 1.5f)),
                200, c, 1.0f);
            d.noGravity = true;
            d.fadeIn = 0f;

            // Sparkles secundarios
            if (Main.rand.NextBool(4))
            {
                Dust d2 = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Gold,
                    new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-1f, 1f)),
                    180, new Color(255, 255, 255), 0.6f);
                d2.noGravity = true;
                d2.fadeIn = 0f;
            }

            // Luz pulsante arcoíris
            Lighting.AddLight(Projectile.Center, new Vector3(c.R / 255f, c.G / 255f, c.B / 255f));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                float t = Main.GameUpdateCount;
                float hue = (t * 0.02f + Projectile.whoAmI * 0.1f) % 1f;
                Color c = Main.hslToRgb(hue, 1f, 0.5f);
                float pulse = 0.8f + 0.2f * (float)System.Math.Sin(t * 0.15f);

                // GlowOrb tintado con el color del hue (additive)
                DrawAdditive("AethonMod/Content/Effects/GlowOrb", Projectile.Center,
                    0.8f * pulse, new Color(c.R, c.G, c.B, 200), 0f);

                // GlowCircle rotando
                DrawAdditive("AethonMod/Content/Effects/GlowCircle", Projectile.Center,
                    0.5f * pulse, new Color(255, 255, 255, 100), t * 0.05f);

                // Núcleo blanco
                DrawAdditive("AethonMod/Content/Effects/GlowOrbWhite", Projectile.Center,
                    0.4f * pulse, new Color(255, 255, 255, 220), 0f);
            }
            catch { }
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Explosión arcoíris
            for (int i = 0; i < 15; i++)
            {
                float hue = (float)i / 15f;
                Color c = Main.hslToRgb(hue, 1f, 0.5f);
                Vector2 dir = new Vector2(
                    (float)System.Math.Cos(i * System.Math.PI * 2 / 15) * 4f,
                    (float)System.Math.Sin(i * System.Math.PI * 2 / 15) * 4f);
                Dust d = Dust.NewDustPerfect(target.Center, DustID.RainbowTorch,
                    dir, 200, c, 1.0f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        private void DrawAdditive(string path, Vector2 pos, float scale, Color color, float rotation)
        {
            try
            {
                Texture2D tex = ModContent.Request<Texture2D>(path).Value;
                if (tex == null) return;
                Vector2 origin = new Vector2(tex.Width / 2f, tex.Height / 2f);
                Vector2 drawPos = pos - Main.screenPosition;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
                Main.spriteBatch.Draw(tex, drawPos, null, color, rotation, origin, scale, SpriteEffects.None, 0f);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
        }
    }
}
