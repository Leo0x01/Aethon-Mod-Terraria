using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Globals
{
    /// <summary>
    /// CalaveraPotenciadaFX — v6.50.57 — LA ESTELA DE LAS CALAVERAS
    /// POTENCIADAS (la letra del usuario: «el jefe esqueleto en la oleada
    /// debe lanzar los proyectiles del libro de las calavera, pero esos
    /// proyectiles deben estar potenciados de alguna forma»).
    ///
    /// La POTENCIACIÓN visible: todo BookOfSkullsSkull (837) HOSTIL — el
    /// del Skeletron de las oleadas del grimorio; las del jugador quedan
    /// como siempre — arde con la estela de la casa:
    /// - FUEGO DORADO (GoldFlame, cada 2 t — el rastro del festín)
    /// - CHISPAS VIOLETAS (PurpleTorch, cada 3 t — la firma del grimorio)
    /// - UNA LUZ más cálida que la vanilla (la potencia se VE)
    ///
    /// El patrón de CosmicProjectileFX (el GlobalProjectile de referencia
    /// del mod): AppliesToEntity + InstancePerEntity, dusts de vida corta
    /// (noGravity + fadeIn 0) — nada de estelas eternas.
    /// </summary>
    public class CalaveraPotenciadaFX : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        // Solo el BookOfSkullsSkull HOSTIL (el del Skeletron de la oleada —
        // las friendly del jugador NO se tocan).
        public override bool AppliesToEntity(Projectile projectile, bool lateInstantiation)
        {
            return projectile.type == ProjectileID.BookOfSkullsSkull &&
                   !projectile.friendly && projectile.hostile;
        }

        public override void AI(Projectile projectile)
        {
            if (Main.dedServ) return;

            // EL FUEGO DORADO (cada 2 t — el rastro del festín).
            if (Main.rand.NextBool(2))
            {
                Dust d1 = Dust.NewDustPerfect(projectile.Center, DustID.GoldFlame,
                    -projectile.velocity * 0.06f + new Vector2(
                        Main.rand.NextFloat(-1f, 1f),
                        Main.rand.NextFloat(-1f, 1f)),
                    160, new Color(255, 217, 61), 0.8f);
                d1.noGravity = true;
                d1.fadeIn = 0f;
            }

            // LAS CHISPAS VIOLETAS (cada 3 t — la firma del grimorio).
            if (Main.rand.NextBool(3))
            {
                Dust d2 = Dust.NewDustPerfect(projectile.Center, DustID.PurpleTorch,
                    -projectile.velocity * 0.04f + new Vector2(
                        Main.rand.NextFloat(-1.2f, 1.2f),
                        Main.rand.NextFloat(-1.2f, 1.2f)),
                    170, new Color(196, 150, 255), 0.6f);
                d2.noGravity = true;
                d2.fadeIn = 0f;
            }

            // LA LUZ CÁLIDA (la potencia se VE — sobre la 0.9 del spawn).
            Lighting.AddLight(projectile.Center, 0.75f, 0.62f, 0.38f);
        }
    }
}
