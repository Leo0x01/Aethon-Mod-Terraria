using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Weapons.Projectiles
{
    /// <summary>
    /// Bolt arcano — proyectil del Grimorio del Eterno.
    /// Efectos: estela violeta, partículas mágicas, explosión arcana.
    /// </summary>
    public class ArcaneBolt : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 300;
            Projectile.aiStyle = 0;
            Projectile.tileCollide = true;
            Projectile.light = 1.0f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Explosión arcana violeta.
            for (int i = 0; i < 18; i++)
            {
                Dust.NewDustPerfect(target.Center, DustID.PurpleTorch,
                    new Vector2(Main.rand.NextFloat(-5, 5), Main.rand.NextFloat(-5, 5)),
                    100, new Color(179, 136, 255), 1.5f);
            }
            // Destello blanco.
            for (int i = 0; i < 8; i++)
            {
                Dust.NewDustPerfect(target.Center, DustID.Enchanted_Gold,
                    new Vector2(Main.rand.NextFloat(-3, 3), Main.rand.NextFloat(-3, 3)),
                    150, default, 1.0f);
            }
        }

        public override void AI()
        {
            // Estela violeta.
            for (int i = 0; i < 3; i++)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height,
                    DustID.PurpleTorch, 0f, 0f, 100, default, 0.9f);
            }

            // Partículas mágicas brillantes.
            if (Main.rand.NextBool(4))
            {
                Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Pink,
                    new Vector2(Main.rand.NextFloat(-1, 1), Main.rand.NextFloat(-1, 1)),
                    200, default, 1.0f);
            }

            // Homing leve.
            NPC? target = FindClosestNPC(15 * 16);
            if (target != null)
            {
                Vector2 direction = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, direction * 12f, 0.05f);
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
        }

        private NPC? FindClosestNPC(float maxDetectDistance)
        {
            NPC? closest = null;
            float sqrMaxDetect = maxDetectDistance * maxDetectDistance;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.friendly || npc.lifeMax <= 5) continue;
                float sqrDist = Vector2.DistanceSquared(npc.Center, Projectile.Center);
                if (sqrDist < sqrMaxDetect)
                {
                    sqrMaxDetect = sqrDist;
                    closest = npc;
                }
            }
            return closest;
        }
    }
}
