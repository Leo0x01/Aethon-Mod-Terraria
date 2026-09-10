using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.Custom
{
    /// <summary>
    /// LightningBolt — rayo zigzagueante dorado.
    /// Se mueve en zigzag dejando estela eléctrica.
    /// </summary>
    public class LightningBolt : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 150;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.extraUpdates = 2;
        }

        public override void AI()
        {
            // Zigzag: oscilar perpendicular a la velocidad
            float zigzag = (float)System.Math.Sin(Main.GameUpdateCount * 0.3f + Projectile.whoAmI) * 3f;
            Vector2 perp = new Vector2(-Projectile.velocity.Y, Projectile.velocity.X);
            if (perp.Length() > 0.1f)
            {
                perp.Normalize();
                Projectile.position += perp * zigzag * 0.1f;
            }

            // Estela eléctrica dorada
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                    new Vector2(
                        Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-2f, 2f)),
                    180, new Color(255, 255, 100), 0.8f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Chispas eléctricas cian
            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch,
                    new Vector2(
                        Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, 3f)),
                    200, new Color(0, 255, 255), 0.6f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Luz dorada intensa
            Lighting.AddLight(Projectile.Center, new Vector3(1f, 0.9f, 0.3f));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                float t = Main.GameUpdateCount;
                float pulse = 0.8f + 0.2f * (float)System.Math.Sin(t * 0.3f);

                // Beam dorado alargado en dirección de movimiento
                float angle = (float)System.Math.Atan2(Projectile.velocity.Y, Projectile.velocity.X);
                DrawAdditive("AethonMod/Content/Effects/BeamGold", Projectile.Center,
                    1.5f * pulse, new Color(255, 255, 100, 200), angle);

                // GlowCircle dorado
                DrawAdditive("AethonMod/Content/Effects/GlowCircleGold", Projectile.Center,
                    0.8f * pulse, new Color(255, 217, 61, 180), 0f);

                // Núcleo blanco
                DrawAdditive("AethonMod/Content/Effects/GlowOrbWhite", Projectile.Center,
                    0.4f * pulse, new Color(255, 255, 255, 220), 0f);
            }
            catch { }
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Rayos eléctricos dispersos
            for (int i = 0; i < 15; i++)
            {
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(10f, 30f);
                Vector2 spawnPos = target.Center + new Vector2(
                    (float)System.Math.Cos(angle) * dist,
                    (float)System.Math.Sin(angle) * dist);
                Vector2 vel = (target.Center - spawnPos) * 0.2f;
                Dust d = Dust.NewDustPerfect(spawnPos, DustID.GoldFlame,
                    vel, 200, new Color(255, 255, 100), 0.8f);
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
