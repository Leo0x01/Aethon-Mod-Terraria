using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Weapons.Projectiles
{
    /// <summary>
    /// Flecha de luz estelar — proyectil base del arco Lumina.
    /// Ignora 5 de defensa y emite luz dorada.
    /// </summary>
    public class StarlightArrow : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 600;
            Projectile.aiStyle = ProjAIStyleID.Arrow;
            Projectile.tileCollide = true;
            Projectile.light = 0.8f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.defense -= 5; // Ignora 5 de defensa temporalmente.
        }

        public override void AI()
        {
            // Estela de partículas doradas.
            if (Main.rand.NextBool(3))
            {
                Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                    Projectile.velocity * 0.1f, 100, default, 0.8f);
            }
        }
    }
}
