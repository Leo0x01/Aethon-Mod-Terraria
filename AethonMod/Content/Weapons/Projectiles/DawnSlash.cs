using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Weapons.Projectiles
{
    /// <summary>
    /// Onda de corte del Alba — proyectil de Solbrand.
    /// Efectos: estela solar naranja, partículas de fuego, explosión al impactar.
    /// </summary>
    public class DawnSlash : ModProjectile
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
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 120;
            Projectile.aiStyle = 0;
            Projectile.tileCollide = false;
            Projectile.light = 1.0f;
            Projectile.extraUpdates = 1;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Explosión solar al impactar.
            for (int i = 0; i < 20; i++)
            {
                Dust.NewDustPerfect(target.Center, DustID.SolarFlare,
                    new Vector2(Main.rand.NextFloat(-5, 5), Main.rand.NextFloat(-5, 5)),
                    100, new Color(255, 154, 60), 1.8f);
            }
            // Llama.
            for (int i = 0; i < 10; i++)
            {
                Dust.NewDustPerfect(target.Center, DustID.Torch,
                    new Vector2(Main.rand.NextFloat(-3, 3), Main.rand.NextFloat(-3, 3)),
                    120, default, 1.2f);
            }
        }

        public override void AI()
        {
            // Rotación dramática.
            Projectile.rotation += 0.4f;
            Projectile.velocity *= 0.97f;

            // Estela solar.
            for (int i = 0; i < 4; i++)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height,
                    DustID.SolarFlare, 0f, 0f, 100, new Color(255, 154, 60), 1.3f);
            }

            // Chispas de fuego.
            if (Main.rand.NextBool(3))
            {
                Dust.NewDustPerfect(Projectile.Center, DustID.Torch,
                    new Vector2(Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2)),
                    150, default, 1.0f);
            }
        }
    }
}
