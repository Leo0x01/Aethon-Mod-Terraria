using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Weapons.Projectiles
{
    /// <summary>
    /// Bolt arcano — proyectil mágico base del Grimorio del Eterno.
    /// Emite luz violeta y puede homingear al enemigo más cercano.
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
            Projectile.light = 0.7f;
        }

        public override void AI()
        {
            // Estela violeta.
            if (Main.rand.NextBool(2))
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height,
                    DustID.PurpleTorch, 0f, 0f, 100, default, 0.8f);
            }
            // Homing leve al enemigo más cercano dentro de 15 tiles.
            NPC target = FindClosestNPC(15 * 16);
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
