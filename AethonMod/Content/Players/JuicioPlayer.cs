using Terraria;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles;

namespace AethonMod.Content.Players
{
    /// <summary>
    /// JuicioPlayer — v6.98 → v6.101 — EL PORTADOR DEL ESTANDARTE.
    ///
    /// LA MÁQUINA: el flag se enciende en UpdateAccessory, se apaga en
    /// ResetEffects, y PostUpdate mantiene VIVO el ancla (el proyectil
    /// invisible que aplica LA QUEMADURA DEL VACÍO EXTERIOR a los impíos
    /// dentro del radio pequeño — la copia fiel del original).
    ///
    /// v6.101 — LA BENDICIÓN DORADA MURIÓ: la v6.100 multiplicaba el
    /// daño hasta +30% por cercanía («bendición del juicio») — NADA que
    /// ver con el original. LO QUE SÍ HACE EL ORIGINAL (la letra del
    /// usuario): mientras un enemigo ARDE con la Quemadura del Vacío
    /// Exterior, el portador cobra el buff VACÍO EXTERIOR (+10% ataque,
    /// +10 defensa, −15% maná) — lo refresca la propia quemadura en
    /// QuemaduraVacioExterior.Update, y los números viven en el buff
    /// (VacioExterior.Update). Este ModPlayer solo viste el ancla.
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
