using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Weapons.Projectiles
{
    /// <summary>
    /// Onda de corte del Alba — proyectil de la espada Solbrand.
    /// Onda frontal de energía solar que perfora 2 enemigos.
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
            Projectile.light = 0.6f;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            // Rotación visual.
            Projectile.rotation += 0.3f;
            // Desaceleración.
            Projectile.velocity *= 0.98f;
            // Estela solar.
            if (Main.rand.NextBool(2))
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height,
                    DustID.SolarFlare, 0f, 0f, 100, default, 1f);
            }
        }
    }
}
