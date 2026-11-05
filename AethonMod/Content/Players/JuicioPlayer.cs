using Terraria;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles;

namespace AethonMod.Content.Players
{
    /// <summary>
    /// JuicioPlayer — v6.98 — EL PORTADOR DEL ESTANDARTE. v6.100 — LA
    /// BENDICIÓN DEL JUICIO.
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
    /// v6.100 — LA BENDICIÓN: la investigación del original
    /// (research/README-ESTANDARTE.md) reveló la SEGUNDA mitad del
    /// concepto que nuestra v6.98 no tenía: el estandarte también
    /// PREMIA AL PORTADOR — la cercanía del impío más próximo
    /// alimenta una BENDICIÓN de daño (hasta +30%) que multiplica
    /// TODO el daño del jugador. La letra del original: «Multiplies
    /// all damage based on how close you are to enemies». El ancla
    /// (EstandarteJuicioAuraProjectile) la escribe cada tick; los
    /// ModifyHit la aplican.
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

        /// <summary>v6.100 — LA BENDICIÓN DEL JUICIO: el multiplicador
        /// de TODO el daño que alimenta la cercanía del impío más
        /// próximo (0 → +0.30). La escribe el ancla del aura cada tick
        /// (remap de la distancia: 720 px → 80 px); ResetEffects la
        /// apaga para que un tick sin ancla no regale nada.</summary>
        public float BendicionJuicio;

        public override void ResetEffects()
        {
            EstandarteJuicio = false;
            EstandarteJuicioOculto = false;
            BendicionJuicio = 0f;
        }

        public override void ModifyHitNPCWithItem(Item item, NPC target,
            ref NPC.HitModifiers modifiers)
        {
            // LA BENDICIÓN multiplica el golpe de cuerpo a cuerpo.
            if (BendicionJuicio > 0f)
                modifiers.FinalDamage *= 1f + BendicionJuicio;
        }

        public override void ModifyHitNPCWithProj(Projectile proj, NPC target,
            ref NPC.HitModifiers modifiers)
        {
            // LA BENDICIÓN multiplica el golpe de todo proyectil.
            if (BendicionJuicio > 0f)
                modifiers.FinalDamage *= 1f + BendicionJuicio;
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
