using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Globals
{
    /// <summary>
    /// CosmicProjectileFX — GlobalProjectile que añade partículas cósmicas
    /// al proyectil Nightglow (ID 931) del Grimorio del Eterno.
    ///
    /// El Nightglow vanilla es solo azul. Este GlobalProjectile lo transforma
    /// en un proyectil cósmico añadiendo:
    /// - Estela dorada (cada frame)
    /// - Destellos cian (cada 2 frames)
    /// - Fragmentos magenta (cada 3 frames)
    /// - Halo índigo sutil (cada 4 frames)
    /// - Explosión cósmica al impactar
    ///
    /// Solo afecta al proyectil 931 (Nightglow) para no modificar otros proyectiles.
    /// </summary>
    public class CosmicProjectileFX : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        // Solo aplica al Nightglow (931)
        public override bool AppliesToEntity(Projectile projectile, bool lateInstantiation)
        {
            return projectile.type == 931; // Nightglow
        }

        public override void AI(Projectile projectile)
        {
            // === ESTELA CÓSMICA (v5.2: duración CORTA) ===
            // El usuario reportó que la estela magenta duraba demasiado.
            // Fix: configurar cada dust con noGravity=true, fadeIn=0, y alpha alto
            // para que desaparezcan en ~10-15 frames (0.25 seg) en vez de 60+.

            // Dorado (núcleo de galaxia) — cada frame
            Dust d1 = Dust.NewDustPerfect(projectile.Center, DustID.GoldFlame,
                -projectile.velocity * 0.05f + new Vector2(
                    Main.rand.NextFloat(-1f, 1f),
                    Main.rand.NextFloat(-1f, 1f)),
                150, new Color(255, 217, 61), 0.7f);
            d1.noGravity = true;
            d1.fadeIn = 0f;

            // Cian (estrella guía) — cada 2 frames
            if (Main.rand.NextBool(2))
            {
                Dust d2 = Dust.NewDustPerfect(projectile.Center, DustID.BlueTorch,
                    -projectile.velocity * 0.1f + new Vector2(
                        Main.rand.NextFloat(-1.5f, 1.5f),
                        Main.rand.NextFloat(-1.5f, 1.5f)),
                    180, new Color(0, 255, 255), 0.6f);
                d2.noGravity = true;
                d2.fadeIn = 0f;
            }

            // Magenta (gemas) — cada 3 frames — alpha ALTO para que dure poco
            if (Main.rand.NextBool(3))
            {
                Dust d3 = Dust.NewDustPerfect(projectile.Center, DustID.RainbowTorch,
                    -projectile.velocity * 0.08f + new Vector2(
                        Main.rand.NextFloat(-1.5f, 1.5f),
                        Main.rand.NextFloat(-1.5f, 1.5f)),
                    200, new Color(255, 0, 102), 0.5f);
                d3.noGravity = true;
                d3.fadeIn = 0f;
            }

            // Índigo (fondo del portal) — cada 4 frames, muy sutil
            if (Main.rand.NextBool(4))
            {
                Dust d4 = Dust.NewDustPerfect(projectile.Center, DustID.PurpleTorch,
                    new Vector2(
                        Main.rand.NextFloat(-1f, 1f),
                        Main.rand.NextFloat(-1f, 1f)),
                    120, new Color(75, 0, 130), 0.5f);
                d4.noGravity = true;
                d4.fadeIn = 0f;
            }

            // Luz cósmica (más intensa que la default del Nightglow)
            Lighting.AddLight(projectile.Center, new Vector3(0.8f, 0.6f, 1.0f));
        }

        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // === EXPLOSIÓN CÓSMICA al impactar (v5.3: duración CORTA) ===
            // Mismo fix que la estela: noGravity + fadeIn=0 + alpha alto + scale pequeño

            // Núcleo dorado (supernova)
            for (int i = 0; i < 12; i++)
            {
                Dust d = Dust.NewDustPerfect(target.Center, DustID.GoldFlame,
                    new Vector2(
                        Main.rand.NextFloat(-5f, 5f),
                        Main.rand.NextFloat(-5f, 5f)),
                    180, new Color(255, 217, 61), 0.9f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Destellos cian (estrella guía)
            for (int i = 0; i < 10; i++)
            {
                Dust d = Dust.NewDustPerfect(target.Center, DustID.BlueTorch,
                    new Vector2(
                        Main.rand.NextFloat(-4f, 4f),
                        Main.rand.NextFloat(-4f, 4f)),
                    200, new Color(0, 255, 255), 0.8f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Fragmentos magenta (gemas) — alpha alto + scale pequeño = duración corta
            for (int i = 0; i < 8; i++)
            {
                Dust d = Dust.NewDustPerfect(target.Center, DustID.RainbowTorch,
                    new Vector2(
                        Main.rand.NextFloat(-4f, 4f),
                        Main.rand.NextFloat(-4f, 4f)),
                    220, new Color(255, 0, 102), 0.6f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Halo índigo (fondo del portal)
            for (int i = 0; i < 6; i++)
            {
                Dust d = Dust.NewDustPerfect(target.Center, DustID.PurpleTorch,
                    new Vector2(
                        Main.rand.NextFloat(-3f, 3f),
                        Main.rand.NextFloat(-3f, 3f)),
                    150, new Color(75, 0, 130), 0.6f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Destello blanco central (supernova)
            for (int i = 0; i < 5; i++)
            {
                Dust d = Dust.NewDustPerfect(target.Center, DustID.Enchanted_Gold,
                    new Vector2(
                        Main.rand.NextFloat(-2f, 2f),
                        Main.rand.NextFloat(-2f, 2f)),
                    255, default, 0.6f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        public override void Kill(Projectile projectile, int timeLeft)
        {
            // Explosión cósmica al morir (sin impacto con enemigo) — v5.3: duración corta
            for (int i = 0; i < 8; i++)
            {
                Dust d = Dust.NewDustPerfect(projectile.Center, DustID.GoldFlame,
                    new Vector2(
                        Main.rand.NextFloat(-3f, 3f),
                        Main.rand.NextFloat(-3f, 3f)),
                    180, new Color(255, 217, 61), 0.7f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
            for (int i = 0; i < 6; i++)
            {
                Dust d = Dust.NewDustPerfect(projectile.Center, DustID.BlueTorch,
                    new Vector2(
                        Main.rand.NextFloat(-2f, 2f),
                        Main.rand.NextFloat(-2f, 2f)),
                    200, new Color(0, 255, 255), 0.6f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
            for (int i = 0; i < 4; i++)
            {
                Dust d = Dust.NewDustPerfect(projectile.Center, DustID.RainbowTorch,
                    new Vector2(
                        Main.rand.NextFloat(-2f, 2f),
                        Main.rand.NextFloat(-2f, 2f)),
                    220, new Color(255, 0, 102), 0.5f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }
    }
}
