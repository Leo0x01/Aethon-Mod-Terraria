using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles
{
    /// <summary>
    /// Cosmic Orb Bolt — proyectil mágico disparado por el CosmicOrbMinion.
    /// Homing hacia el enemigo mas cercano, con estela violeta/dorada.
    /// </summary>
    public class CosmicOrbBolt : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;
            Projectile.light = 0.8f;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            // Homing hacia el enemigo HOSTIL mas cercano
            NPC? target = null;
            float closestDist = 400f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.active) continue;
                // Solo NPCs hostiles
                if (npc.friendly || npc.townNPC || npc.dontTakeDamage) continue;
                if (npc.aiStyle == 7) continue; // critters
                if (npc.catchItem > 0) continue;
                if (npc.immortal) continue;
                if (!npc.CanBeChasedBy()) continue;

                float dist = Vector2.Distance(npc.Center, Projectile.Center);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    target = npc;
                }
            }

            if (target != null)
            {
                Vector2 direction = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero);
                if (direction != Vector2.Zero)
                {
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, direction * 14f, 0.1f);
                }
            }

            // Rotar el proyectil
            Projectile.rotation = Projectile.velocity.ToRotation();

            // Estela violeta/dorada
            if (Main.rand.NextBool(2))
            {
                Dust.NewDustPerfect(Projectile.Center, Terraria.ID.DustID.PurpleTorch,
                    -Projectile.velocity * 0.1f + new Vector2(Main.rand.NextFloat(-1, 1), Main.rand.NextFloat(-1, 1)),
                    100, new Color(179, 136, 255), 0.8f);
            }
            if (Main.rand.NextBool(4))
            {
                Dust.NewDustPerfect(Projectile.Center, Terraria.ID.DustID.GoldFlame,
                    -Projectile.velocity * 0.05f + new Vector2(Main.rand.NextFloat(-1, 1), Main.rand.NextFloat(-1, 1)),
                    100, new Color(245, 196, 81), 0.7f);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Explosion de polvo violeta
            for (int i = 0; i < 12; i++)
            {
                Dust.NewDustPerfect(target.Center, Terraria.ID.DustID.PurpleTorch,
                    new Vector2(Main.rand.NextFloat(-4, 4), Main.rand.NextFloat(-4, 4)),
                    100, new Color(179, 136, 255), 1.2f);
            }
            for (int i = 0; i < 6; i++)
            {
                Dust.NewDustPerfect(target.Center, Terraria.ID.DustID.GoldFlame,
                    new Vector2(Main.rand.NextFloat(-3, 3), Main.rand.NextFloat(-3, 3)),
                    100, new Color(245, 196, 81), 1f);
            }
        }

        public override void Kill(int timeLeft)
        {
            // Explosion al morir
            for (int i = 0; i < 8; i++)
            {
                Dust.NewDustPerfect(Projectile.Center, Terraria.ID.DustID.PurpleTorch,
                    new Vector2(Main.rand.NextFloat(-3, 3), Main.rand.NextFloat(-3, 3)),
                    100, new Color(179, 136, 255), 1f);
            }
        }
    }
}
