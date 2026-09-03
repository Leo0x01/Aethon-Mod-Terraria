using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Weapons.Projectiles
{
    /// <summary>
    /// Fragmento de Galaxia — bolt arcano del Grimorio del Eterno.
    ///
    /// Diseñado para combinar estéticamente con el sprite del Grimorio (portal cósmico):
    /// - Núcleo dorado (#FFD93D) como el centro de la galaxia del grimorio
    /// - Halo índigo (#4B0082) como el fondo del portal
    /// - Estela de partículas cian (#00FFFF), magenta (#FF0066) y oro (#FFD700)
    ///   que replican los colores del borde arcoíris y las gemas del grimorio
    /// - Rotación lenta que evoca un fragmento del portal girando
    /// - Explosión cósmica al impactar con la misma paleta
    /// </summary>
    public class ArcaneBolt : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            // DisplayName cargado desde Localization.
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 300;
            Projectile.aiStyle = 0;
            Projectile.tileCollide = true;
            // Luz púrpura-dorada mezclada (evoca el portal del grimorio)
            Projectile.light = 1.2f;
            Projectile.extraUpdates = 1;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // === EXPLOSIÓN CÓSMICA ===
            // Paleta del grimorio: dorado, cian, magenta, índigo

            // Núcleo dorado (galaxy core)
            for (int i = 0; i < 12; i++)
            {
                Dust.NewDustPerfect(target.Center, DustID.GoldFlame,
                    new Vector2(Main.rand.NextFloat(-6, 6), Main.rand.NextFloat(-6, 6)),
                    100, new Color(255, 217, 61), 1.5f);
            }

            // Destellos cian (estrella guía del grimorio)
            for (int i = 0; i < 10; i++)
            {
                Dust.NewDustPerfect(target.Center, DustID.CyanTorch,
                    new Vector2(Main.rand.NextFloat(-5, 5), Main.rand.NextFloat(-5, 5)),
                    150, new Color(0, 255, 255), 1.3f);
            }

            // Fragmentos magenta (gemas de las esquinas)
            for (int i = 0; i < 8; i++)
            {
                Dust.NewDustPerfect(target.Center, DustID.RainbowTorch,
                    new Vector2(Main.rand.NextFloat(-4, 4), Main.rand.NextFloat(-4, 4)),
                    200, new Color(255, 0, 102), 1.2f);
            }

            // Halo índigo (fondo del portal)
            for (int i = 0; i < 6; i++)
            {
                Dust.NewDustPerfect(target.Center, DustID.PurpleTorch,
                    new Vector2(Main.rand.NextFloat(-3, 3), Main.rand.NextFloat(-3, 3)),
                    100, new Color(75, 0, 130), 1.0f);
            }

            // Destello blanco central (supernova)
            for (int i = 0; i < 5; i++)
            {
                Dust.NewDustPerfect(target.Center, DustID.Enchanted_Gold,
                    new Vector2(Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2)),
                    255, default, 0.8f);
            }
        }

        public override void AI()
        {
            // === ROTACIÓN LENTA (fragmento de portal girando) ===
            Projectile.rotation += 0.12f;

            // === ESTELA CÓSMICA ===
            // Alternar entre los colores del grimorio: dorado, cian, magenta

            // Dorado (núcleo de galaxia)
            Dust.NewDust(Projectile.position, Projectile.width, Projectile.height,
                DustID.GoldFlame, 0f, 0f, 100, new Color(255, 217, 61), 0.8f);

            // Cian (estrella guía) — cada 2 frames
            if (Main.rand.NextBool(2))
            {
                Dust.NewDustPerfect(Projectile.Center, DustID.CyanTorch,
                    new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), Main.rand.NextFloat(-1.5f, 1.5f)),
                    150, new Color(0, 255, 255), 0.9f);
            }

            // Magenta (gemas) — cada 3 frames
            if (Main.rand.NextBool(3))
            {
                Dust.NewDustPerfect(Projectile.Center, DustID.RainbowTorch,
                    new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), Main.rand.NextFloat(-1.5f, 1.5f)),
                    200, new Color(255, 0, 102), 0.9f);
            }

            // Índigo (fondo del portal) — cada 4 frames, más sutil
            if (Main.rand.NextBool(4))
            {
                Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch,
                    new Vector2(Main.rand.NextFloat(-1, 1), Main.rand.NextFloat(-1, 1)),
                    80, new Color(75, 0, 130), 0.7f);
            }

            // === HOMING LEVE ===
            NPC? target = FindClosestNPC(15 * 16);
            if (target != null)
            {
                Vector2 direction = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, direction * 12f, 0.05f);
            }
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
