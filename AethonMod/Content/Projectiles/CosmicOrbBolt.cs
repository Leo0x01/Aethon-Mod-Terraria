using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles
{
    /// <summary>
    /// Cosmic Orb Bolt — proyectil cósmico disparado por el CosmicOrbMinion.
    ///
    /// Diseñado para combinar con el Grimorio del Eterno:
    /// - Núcleo dorado (#FFD93D) como la galaxia del grimorio
    /// - Destellos cian (#00FFFF) como la estrella guía
    /// - Fragmentos magenta (#FF0066) como las gemas
    /// - Halo índigo (#4B0082) como el fondo del portal
    ///
    /// Homing hacia el enemigo hostil más cercano.
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
            // === HOMING hacia el enemigo HOSTIL más cercano ===
            NPC? target = null;
            float closestDist = 400f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.active) continue;
                if (npc.friendly || npc.townNPC || npc.dontTakeDamage) continue;
                if (npc.aiStyle == 7) continue;
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

            // Rotar el proyectil (como un fragmento de portal girando)
            Projectile.rotation += 0.15f;

            // === ESTELA CÓSMICA (paleta del grimorio) ===
            // Dorado (núcleo de galaxia) — cada frame
            if (Main.rand.NextBool(2))
            {
                Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                    -Projectile.velocity * 0.05f + new Vector2(Main.rand.NextFloat(-1, 1), Main.rand.NextFloat(-1, 1)),
                    100, new Color(255, 217, 61), 0.9f);
            }

            // Cian (estrella guía) — cada 2 frames
            if (Main.rand.NextBool(2))
            {
                Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch,
                    -Projectile.velocity * 0.1f + new Vector2(Main.rand.NextFloat(-1, 1), Main.rand.NextFloat(-1, 1)),
                    150, new Color(0, 255, 255), 0.8f);
            }

            // Magenta (gemas) — cada 3 frames
            if (Main.rand.NextBool(3))
            {
                Dust.NewDustPerfect(Projectile.Center, DustID.RainbowTorch,
                    -Projectile.velocity * 0.08f + new Vector2(Main.rand.NextFloat(-1, 1), Main.rand.NextFloat(-1, 1)),
                    200, new Color(255, 0, 102), 0.8f);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // === EXPLOSIÓN CÓSMICA al impactar ===
            // Dorado (núcleo)
            for (int i = 0; i < 10; i++)
            {
                Dust.NewDustPerfect(target.Center, DustID.GoldFlame,
                    new Vector2(Main.rand.NextFloat(-4, 4), Main.rand.NextFloat(-4, 4)),
                    100, new Color(255, 217, 61), 1.2f);
            }
            // Cian (estrella)
            for (int i = 0; i < 8; i++)
            {
                Dust.NewDustPerfect(target.Center, DustID.BlueTorch,
                    new Vector2(Main.rand.NextFloat(-4, 4), Main.rand.NextFloat(-4, 4)),
                    150, new Color(0, 255, 255), 1.0f);
            }
            // Magenta (gemas)
            for (int i = 0; i < 6; i++)
            {
                Dust.NewDustPerfect(target.Center, DustID.RainbowTorch,
                    new Vector2(Main.rand.NextFloat(-3, 3), Main.rand.NextFloat(-3, 3)),
                    200, new Color(255, 0, 102), 1.0f);
            }
        }

        public override void Kill(int timeLeft)
        {
            // Explosión al morir (sin impacto)
            for (int i = 0; i < 6; i++)
            {
                Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                    new Vector2(Main.rand.NextFloat(-3, 3), Main.rand.NextFloat(-3, 3)),
                    100, new Color(255, 217, 61), 0.9f);
            }
            for (int i = 0; i < 4; i++)
            {
                Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch,
                    new Vector2(Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2)),
                    150, new Color(0, 255, 255), 0.8f);
            }
        }
    }
}
