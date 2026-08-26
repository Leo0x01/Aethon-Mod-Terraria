using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Weapons.Projectiles
{
    /// <summary>
    /// Flecha de luz estelar — proyectil del arco Lumina.
    /// Efectos visuales: estela dorada, partículas de estrellas, brillo.
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
            Projectile.light = 1.2f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Aplicar Ichor (reduccion temporal de defensa) en vez de mutar permanentemente.
            // Antes: target.defense -= 5; // BUG: se acumulaba infinitamente.
            // Ahora: debuff de 5 segundos (300 ticks) que reduce defensa.
            target.AddBuff(BuffID.Ichor, 300);

            // Explosion de estrellas doradas al impactar.
            for (int i = 0; i < 15; i++)
            {
                Dust.NewDustPerfect(target.Center, DustID.GoldFlame,
                    new Vector2(Main.rand.NextFloat(-4, 4), Main.rand.NextFloat(-4, 4)),
                    100, new Color(245, 196, 81), 1.5f);
            }
            // Polvo brillante blanco.
            for (int i = 0; i < 8; i++)
            {
                Dust.NewDustPerfect(target.Center, DustID.Enchanted_Pink,
                    new Vector2(Main.rand.NextFloat(-3, 3), Main.rand.NextFloat(-3, 3)),
                    150, default, 1.2f);
            }
        }

        public override void AI()
        {
            // Estela dorada con partículas.
            for (int i = 0; i < 3; i++)
            {
                Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                    Projectile.velocity * 0.05f + new Vector2(Main.rand.NextFloat(-1, 1), Main.rand.NextFloat(-1, 1)),
                    80, new Color(245, 196, 81), 0.9f);
            }

            // Estrella brillante ocasional.
            if (Main.rand.NextBool(5))
            {
                Dust.NewDustPerfect(Projectile.Center, DustID.YellowStarDust,
                    Vector2.Zero, 200, new Color(255, 240, 200), 1.3f);
            }
        }
    }
}
