using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.Custom
{
    /// <summary>
    /// FrostSpear — lanza de hielo que congela.
    /// Estela azul-blanca con cristales de hielo.
    /// </summary>
    public class FrostSpear : ModProjectile
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
            Projectile.timeLeft = 180;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            // Rotación alineada con la velocidad
            Projectile.rotation = (float)System.Math.Atan2(Projectile.velocity.Y, Projectile.velocity.X);

            // Estela de hielo (cian + blanco)
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch,
                    -Projectile.velocity * 0.2f + new Vector2(
                        Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-1f, 1f)),
                    180, new Color(150, 220, 255), 0.8f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Cristales de hielo (sparkles)
            if (Main.rand.NextBool(4))
            {
                Vector2 offset = new Vector2(
                    Main.rand.NextFloat(-15f, 15f), Main.rand.NextFloat(-15f, 15f));
                Dust d = Dust.NewDustPerfect(Projectile.Center + offset, DustID.IceTorch,
                    Vector2.Zero,
                    200, new Color(200, 240, 255), 0.7f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Luz azul-hielo
            Lighting.AddLight(Projectile.Center, new Vector3(0.5f, 0.8f, 1f));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                float t = Main.GameUpdateCount;
                float pulse = 0.8f + 0.2f * (float)System.Math.Sin(t * 0.15f);
                float angle = Projectile.rotation;

                // Beam cian alargado
                DrawAdditive("AethonMod/Content/Effects/BeamCyan", Projectile.Center,
                    1.5f * pulse, new Color(150, 220, 255, 200), angle);

                // GlowCircle cian
                DrawAdditive("AethonMod/Content/Effects/GlowCircleCyan", Projectile.Center,
                    0.8f * pulse, new Color(0, 200, 255, 180), 0f);

                // GlowOrb blanco
                DrawAdditive("AethonMod/Content/Effects/GlowOrbWhite", Projectile.Center,
                    0.5f * pulse, new Color(220, 240, 255, 220), 0f);
            }
            catch { }
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Explosión de cristales
            for (int i = 0; i < 18; i++)
            {
                float angle = (System.MathF.PI * 2 / 18) * i;
                Vector2 dir = new Vector2(
                    (float)System.Math.Cos(angle) * 4f,
                    (float)System.Math.Sin(angle) * 4f);
                Dust d = Dust.NewDustPerfect(target.Center, DustID.IceTorch,
                    dir, 200, new Color(200, 240, 255), 1.0f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Debuff de frost
            target.AddBuff(BuffID.Frostburn, 180);
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
