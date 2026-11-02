using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Globals
{
    // ======================================================================
    //  FaucesGlobalProjectile — v6.88 — LA ZONA DEL FESTÍN (el aire).
    //
    //  LA LETRA: «todos los jefes mientras son devorados por el grimorio,
    //  estos siguen atacando… en el momento en el que comienza la
    //  animacion de devorar, los jefes deben dejar de atacar y
    //  detenerse» — la v6.86 congelaba la IA del jefe PERO sus
    //  PROYECTILES hostiles ya disparados (láseres, andanadas, bombas)
    //  seguían volando durante todo el cine. LA CURA: mientras un jefe
    //  devorado vive a ≤700 px, TODO proyectil hostil de la zona se
    //  apaga en vuelo (Kill silencioso, sin dust de explosion): la
    //  comidad del festín es sagrada. El chequeo corre cada 30 ticks por
    //  proyectil (localAI[1] de contador: barato, solo durante el
    //  festín — 3,5-7 s de vida).
    // ======================================================================
    public class FaucesGlobalProjectile : GlobalProjectile
    {
        public override bool CanHitPlayer(Projectile projectile, Player target)
        {
            if (projectile.hostile && !projectile.friendly &&
                global::AethonMod.Content.Globals.FaucesGlobalNPC
                    .HayJefeDevoradoCerca(projectile.Center, 700f))
                return false;
            return base.CanHitPlayer(projectile, target);
        }

        public override void AI(Projectile projectile)
        {
            try
            {
                if (!projectile.hostile || projectile.friendly) return;
                // cada 30 ticks por proyectil — el escaneo es barato y
                // solo vive mientras el festín dura.
                projectile.localAI[1] += 1f;
                if (projectile.localAI[1] < 30f) return;
                projectile.localAI[1] = 0f;
                if (global::AethonMod.Content.Globals.FaucesGlobalNPC
                        .HayJefeDevoradoCerca(projectile.Center, 700f))
                {
                    projectile.active = false;
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.TrySendData(MessageID.SyncProjectile,
                            -1, -1, null, projectile.whoAmI);
                }
            }
            catch { }
        }
    }
}
