using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Jefes;

namespace AethonMod.Content.Players
{
    /// <summary>
    /// v6.50.117 — LA TRACCIÓN DEL SOL MORIBUNDO (la letra del usuario:
    /// «cada tick: player.velocity += dirección al sol * 0.06f mientras
    /// esté a <900 px — castiga la calma, no el reflejo»).
    ///
    /// POR QUÉ UN MODPLAYER Y NO LA IA DEL PROYECTIL: mover a un jugador
    /// es cosa de SU máquina (el cauce MP de Terraria: la posición del
    /// jugador la simula su dueño; un velocity += del servidor sobre un
    /// remoto se pisa al siguiente sync). El hook corre EN CADA MÁQUINA
    /// para SU PROPIO jugador — el tirón funciona igual en SP, en host y
    /// en cualquier remoto.
    ///
    /// El aro del radio (880 px) lo dibuja el propio sol: el aviso
    /// legible de que estás siendo arrastrado.
    /// </summary>
    public class TraccionRecuerdoPlayer : ModPlayer
    {
        public override void PreUpdateMovement()
        {
            if (Main.gameMenu) return;

            int tipo = ModContent.ProjectileType<AtaqueFusionProjectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p == null || !p.active || p.type != tipo) continue;
                if ((int)p.ai[0] != AtaqueFusionProjectile.EstiloSolMoribundo) continue;

                float dist = Vector2.Distance(p.Center, Player.Center);
                if (dist < 900f && dist > 1f)
                {
                    Vector2 hacia = Vector2.Normalize(p.Center - Player.Center);
                    Player.velocity += hacia * 0.06f;
                }
            }
        }
    }
}
