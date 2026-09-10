using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.Custom
{
    /// <summary>
    /// StarShuriken — estrella giratoria con sparkles.
    /// Usa MagicRing + SparkleStar + dust de estela.
    /// </summary>
    public class StarShuriken : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = 5;
            Projectile.timeLeft = 200;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            // Rotación rápida de shuriken
            Projectile.rotation += 0.4f;

            // Estela dorada
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                    -Projectile.velocity * 0.2f + new Vector2(
                        Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-1f, 1f)),
                    180, new Color(255, 217, 61), 0.8f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Sparkles tipo estrella cada 5 frames
            if (Main.rand.NextBool(5))
            {
                Vector2 offset = new Vector2(
                    Main.rand.NextFloat(-20f, 20f), Main.rand.NextFloat(-20f, 20f));
                Dust d = Dust.NewDustPerfect(Projectile.Center + offset, DustID.Enchanted_Gold,
                    Vector2.Zero,
                    220, new Color(255, 255, 255), 0.7f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Luz dorada
            Lighting.AddLight(Projectile.Center, new Vector3(1f, 0.85f, 0.3f));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                float t = Main.GameUpdateCount;
                float pulse = 0.7f + 0.3f * (float)System.Math.Sin(t * 0.2f);

                // MagicRing dorado rotando
                DrawAdditive("AethonMod/Content/Effects/MagicRingGold", Projectile.Center,
                    0.8f * pulse, new Color(255, 217, 61, 200), Projectile.rotation);

                // GlowCircle dorado de fondo
                DrawAdditive("AethonMod/Content/Effects/GlowCircleGold", Projectile.Center,
                    0.6f * pulse, new Color(255, 217, 61, 150), 0f);

                // SparkleStar central
                DrawAdditive("AethonMod/Content/Effects/SparkleStar", Projectile.Center,
                    0.7f * pulse, new Color(255, 255, 255, 220), -Projectile.rotation * 0.5f);
            }
            catch { }
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Explosión de estrellas
            for (int i = 0; i < 12; i++)
            {
                float angle = (System.MathF.PI * 2 / 12) * i;
                Vector2 dir = new Vector2((float)System.Math.Cos(angle), (float)System.Math.Sin(angle)) * 5f;
                Dust d = Dust.NewDustPerfect(target.Center, DustID.GoldFlame,
                    dir, 200, new Color(255, 217, 61), 1.2f);
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
