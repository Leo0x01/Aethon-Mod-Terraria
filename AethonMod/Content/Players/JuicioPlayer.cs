using Terraria;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles;

namespace AethonMod.Content.Players
{
    /// <summary>
    /// JuicioPlayer — v6.98 — EL PORTADOR DEL ESTANDARTE.
    ///
    /// EL AURA QUE DAÑA — el concepto abandonado de los primeros
    /// commits, RESUCITADO y hecho superior (la letra: «recuerda la
    /// creacion de un aura que hiciera daño, es algo de los primeros
    /// commit que abandonamos porque nunca fuimos capaces de crearlo
    /// correctamente»). Inspirado en la IDEA del estandarte de guerra
    /// de la referencia del usuario (la mecánica: un aura alrededor
    /// del jugador que daña a los enemigos — cuanto más cerca, más
    /// daño) pero implementado 100% por la casa: otro motor, otros
    /// números, otras texturas, otras capas — nada de nadie.
    ///
    /// LA MÁQUINA: el flag se enciende en UpdateAccessory, se apaga
    /// en ResetEffects, y PostUpdate mantiene VIVO el ancla (el
    /// proyectil invisible que viste el aura y ejecuta el juicio).
    /// </summary>
    public class JuicioPlayer : ModPlayer
    {
        /// <summary>¿El estandarte está equipado? (el flag del ciclo).</summary>
        public bool EstandarteJuicio;

        /// <summary>¿El portador ocultó el visual? (la mecánica vive,
        /// el Vestido se apaga — el respeto al hideVisual).</summary>
        public bool EstandarteJuicioOculto;

        public override void ResetEffects()
        {
            EstandarteJuicio = false;
            EstandarteJuicioOculto = false;
        }

        public override void PostUpdate()
        {
            if (!EstandarteJuicio || Player.dead || !Player.active) return;

            // EL ANCLA VIVA: un solo proyectil-aura por portador (el
            // tiempo lo mantiene el propio ancla mientras el flag viva).
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active &&
                    p.type == ModContent.ProjectileType<EstandarteJuicioAuraProjectile>() &&
                    p.owner == Player.whoAmI)
                    return;                     // ya vive — nada que hacer
            }
            int idx = Projectile.NewProjectile(Player.GetSource_FromThis(),
                Player.MountedCenter, default,
                ModContent.ProjectileType<EstandarteJuicioAuraProjectile>(),
                0, 0f, Player.whoAmI);
            if (idx >= 0) Main.projectile[idx].netUpdate = true;
        }
    }
}
