using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Weapons.Projectiles
{
    /// <summary>
    /// Genesis Light — proyectil de luz autoguiado del Fragmento Génesis.
    /// Inspirado en el Nightglow de la Emperatriz de la Luz.
    /// </summary>
    public class GenesisLight : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 180;
            Projectile.light = 0.8f;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            Projectile.rotation += 0.15f;

            // Homing hacia el enemigo hostil más cercano
            NPC target = FindClosestNPC(400f);
            if (target != null)
            {
                Vector2 direction = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero);
                if (direction != Vector2.Zero)
                {
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, direction * 14f, 0.1f);
                }
            }

            // Estela de luz dorada (partículas de corta duración)
            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                    new Vector2(Main.rand.NextFloat(-1, 1), Main.rand.NextFloat(-1, 1)),
                    200, new Color(255, 217, 61), 0.4f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        private NPC FindClosestNPC(float maxDist)
        {
            NPC closest = null;
            float sqr = maxDist * maxDist;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.active) continue;
                if (npc.friendly || npc.townNPC) continue;
                if (npc.dontTakeDamage) continue;
                if (npc.immortal) continue;
                float d = Vector2.DistanceSquared(npc.Center, Projectile.Center);
                if (d < sqr) { sqr = d; closest = npc; }
            }
            return closest;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            for (int i = 0; i < 8; i++)
            {
                Dust d = Dust.NewDustPerfect(target.Center, DustID.GoldFlame,
                    new Vector2(Main.rand.NextFloat(-3, 3), Main.rand.NextFloat(-3, 3)),
                    200, new Color(255, 217, 61), 0.4f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 6; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                    new Vector2(Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2)),
                    200, new Color(255, 217, 61), 0.3f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }
    }
}
