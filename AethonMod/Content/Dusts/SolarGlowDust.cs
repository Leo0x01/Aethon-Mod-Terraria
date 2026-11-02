using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.Dusts
{
    /// <summary>
    /// SolarGlowDust — v6.85 — EL POLVO DEL AURA SOLAR (la investigación
    /// R1: el «GlowFastDecelerate» de Bombus Apis, recreado con los
    /// parámetros exactos de su fuente):
    ///   · decae ×0,95/tick (muere bajo 0,05),
    ///   · frena ×0,95 (la variante FastDecelerate),
    ///   · gira +0,06 rad/tick,
    ///   · ALUMBRA su color (el glow con luz — la firma del original).
    /// Textura: halo radial blanco 64×64 (se tiñe al nacer con newColor).
    /// </summary>
    public class SolarGlowDust : ModDust
    {
        public override void OnSpawn(Dust dust)
        {
            dust.noGravity = true;
            dust.fadeIn = 0f;
        }

        public override bool Update(Dust dust)
        {
            // LA VIDA (la tabla del informe: ×0,95 hasta <0,05).
            dust.scale *= 0.95f;
            if (dust.scale < 0.05f)
            {
                dust.active = false;
                return false;
            }
            // EL FRENO (FastDecelerate) y el GIRO.
            dust.velocity *= 0.95f;
            dust.rotation += 0.06f;

            // LA LUZ (el polvo alumba su color — la firma del original).
            if (Main.netMode != Terraria.ID.NetmodeID.Server && !Main.dedServ)
            {
                Vector3 luz = dust.color.ToVector3();
                Lighting.AddLight(dust.position, luz * dust.scale * 1.6f);
            }
            return true;
        }
    }
}
