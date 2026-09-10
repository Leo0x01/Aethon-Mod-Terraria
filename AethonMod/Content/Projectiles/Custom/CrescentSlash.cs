using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.Custom
{
    /// <summary>
    /// CrescentSlash — slash de energía tipo Influx Waver/Star Wrath.
    /// Inspirado en la imagen de referencia: arco crescente verde-cian con speed trails.
    /// Se dibuja a sí mismo con additive blending usando GlowRay + GlowCircle.
    /// </summary>
    public class CrescentSlash : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 120;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 2;
        }

        public override void AI()
        {
            // Rotación continua para efecto de "slash"
            Projectile.rotation += 0.3f;

            // Estela de speed trails (partículas estiradas)
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch,
                    -Projectile.velocity * 0.3f + new Vector2(
                        Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-1f, 1f)),
                    150, new Color(0, 255, 200), 1.2f);
                d.noGravity = true;
                d.fadeIn = 0f;
                d.scale = 1.5f;
            }

            // Sparkles tipo "+" alrededor
            if (Main.rand.NextBool(3))
            {
                Vector2 offset = new Vector2(
                    Main.rand.NextFloat(-30f, 30f), Main.rand.NextFloat(-30f, 30f));
                Dust d = Dust.NewDustPerfect(Projectile.Center + offset, DustID.Enchanted_Gold,
                    Vector2.Zero,
                    200, new Color(0, 255, 200), 0.8f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Luz cian intensa
            Lighting.AddLight(Projectile.Center, new Vector3(0f, 0.8f, 0.6f));

            // Homing leve hacia enemigos cercanos
            NPC target = FindClosestNPC(400f);
            if (target != null)
            {
                Vector2 dir = target.Center - Projectile.Center;
                dir.Normalize();
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, dir * 12f, 0.05f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Dibujar el crescent con additive blending usando GlowRay
            try
            {
                float t = Main.GameUpdateCount;
                float pulse = 0.8f + 0.2f * (float)System.Math.Sin(t * 0.2f);

                // GlowCircle verde-cian de fondo
                DrawAdditive("AethonMod/Content/Effects/GlowCircleCyan", Projectile.Center,
                    1.2f * pulse, new Color(0, 255, 200, 150), 0f);

                // 2 GlowRay cruzados formando el crescent
                DrawAdditive("AethonMod/Content/Effects/GlowRay", Projectile.Center,
                    1.5f * pulse, new Color(0, 255, 200, 200), Projectile.rotation);
                DrawAdditive("AethonMod/Content/Effects/GlowRay", Projectile.Center,
                    1.2f * pulse, new Color(255, 255, 255, 150), Projectile.rotation + 0.3f);

                // Núcleo blanco brillante
                DrawAdditive("AethonMod/Content/Effects/GlowOrbWhite", Projectile.Center,
                    0.6f * pulse, new Color(255, 255, 255, 200), 0f);
            }
            catch { }
            return false; // no dibujar sprite vanilla
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Explosión de sparkles al impactar
            for (int i = 0; i < 20; i++)
            {
                Vector2 dir = new Vector2(
                    Main.rand.NextFloat(-5f, 5f), Main.rand.NextFloat(-5f, 5f));
                Dust d = Dust.NewDustPerfect(target.Center, DustID.Enchanted_Gold,
                    dir, 200, new Color(0, 255, 200), 1.2f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        private NPC FindClosestNPC(float maxDist)
        {
            NPC closest = null;
            float minDist = maxDist;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy()) continue;
                float dist = Vector2.Distance(Projectile.Center, npc.Center);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = npc;
                }
            }
            return closest;
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
