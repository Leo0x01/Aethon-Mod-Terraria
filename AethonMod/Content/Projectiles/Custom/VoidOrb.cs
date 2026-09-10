using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.Custom
{
    /// <summary>
    /// VoidOrb — orbe de vacío que atrae partículas hacia él.
    /// Efecto gravitacional: dusts orbitan el proyectil.
    /// </summary>
    public class VoidOrb : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 36;
            Projectile.height = 36;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 240;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            // Rotación lenta
            Projectile.rotation += 0.05f;

            // Partículas orbitando (efecto gravitacional)
            for (int i = 0; i < 2; i++)
            {
                float angle = Main.GameUpdateCount * 0.1f + i * System.MathF.PI;
                float radius = 25f + (float)System.Math.Sin(Main.GameUpdateCount * 0.05f + i) * 5f;
                Vector2 offset = new Vector2(
                    (float)System.Math.Cos(angle) * radius,
                    (float)System.Math.Sin(angle) * radius);
                Dust d = Dust.NewDustPerfect(Projectile.Center + offset, DustID.PurpleTorch,
                    -offset * 0.05f, // atraer hacia el centro
                    150, new Color(150, 0, 200), 0.8f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Partículas siendo "absorbidas" desde lejos
            if (Main.rand.NextBool(4))
            {
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(40f, 60f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)System.Math.Cos(angle) * dist,
                    (float)System.Math.Sin(angle) * dist);
                Vector2 vel = (Projectile.Center - spawnPos) * 0.05f;
                Dust d = Dust.NewDustPerfect(spawnPos, DustID.PurpleTorch,
                    vel, 150, new Color(200, 100, 255), 0.7f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Luz púrpura intensa
            Lighting.AddLight(Projectile.Center, new Vector3(0.6f, 0.2f, 0.9f));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                float t = Main.GameUpdateCount;
                float pulse = 0.7f + 0.3f * (float)System.Math.Sin(t * 0.1f);

                // GlowCircle púrpura de fondo
                DrawAdditive("AethonMod/Content/Effects/GlowCirclePurple", Projectile.Center,
                    1.0f * pulse, new Color(150, 0, 200, 180), 0f);

                // MagicRing púrpura rotando
                DrawAdditive("AethonMod/Content/Effects/MagicRing", Projectile.Center,
                    0.7f * pulse, new Color(200, 100, 255, 150), t * 0.03f);

                // Hex púrpura rotando inverso
                DrawAdditive("AethonMod/Content/Effects/HexCyan", Projectile.Center,
                    0.8f * pulse, new Color(100, 0, 150, 120), -t * 0.02f);

                // Núcleo oscuro (GlowOrb púrpura)
                DrawAdditive("AethonMod/Content/Effects/GlowOrbPurple", Projectile.Center,
                    0.5f * pulse, new Color(80, 0, 120, 200), 0f);
            }
            catch { }
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Implosión: todas las partículas convergen al centro
            for (int i = 0; i < 25; i++)
            {
                float angle = (System.MathF.PI * 2 / 25) * i;
                float dist = 40f;
                Vector2 spawnPos = target.Center + new Vector2(
                    (float)System.Math.Cos(angle) * dist,
                    (float)System.Math.Sin(angle) * dist);
                Vector2 vel = (target.Center - spawnPos) * 0.1f;
                Dust d = Dust.NewDustPerfect(spawnPos, DustID.PurpleTorch,
                    vel, 200, new Color(200, 100, 255), 1.0f);
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
