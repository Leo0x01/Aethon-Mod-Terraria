using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.Globals
{
    /// <summary>
    /// LA CURA DEL originalDamage HUÉRFANO (v6.51.2) — EL HALLAZGO AL IL
    /// DE tModLoader 2026.08.3.0 QUE EXPLICA EL DAÑO MUERTO DEL
    /// ESCUADRÓN:
    ///
    /// · `Projectile.NewProjectile(..., Damage, ...)` asigna
    ///   `Projectile.damage` — pero JAMÁS toca `originalDamage`
    ///   (verificado al IL: el parámetro viaja a `stfld damage` y nada
    ///   más; `SetDefaults` lo dejó en 0).
    /// · Y `Projectile.Update` recalcula CADA TICK, para todo
    ///   proyectil con `minion == true` (o `sentry`):
    ///   `damage = (int)owner.GetTotalDamage(DamageType).ApplyTo(originalDamage)`.
    /// · Con `originalDamage == 0`: `ApplyTo(0) = 0` → EL DAÑO DEL
    ///   MINION COLAPSA A 0 AL PRIMER TICK, y todo lo que nace de él
    ///   (los rayos, los soles, los agujeros: `Math.Max(1, ...)`)
    ///   pegaba MONEDAS de 1. El escuadrón "disparaba"… y el daño no
    ///   respondía NI al grimorio NI a la armadura: la letra exacta del
    ///   playtest («el daño de todos deben ser afectados por las
    ///   estadísticas del grimorio y las armaduras o accesorios»).
    ///
    /// LA CURA: en el instante del nacimiento (OnSpawn corre DENTRO de
    /// NewProjectile, después de las asignaciones), si es un
    /// minion/sentry con `originalDamage == 0` y daño real, se fija
    /// `originalDamage = damage`. Desde ese tick el refresh de vanilla
    /// hace lo que el juego siempre quiso:
    ///
    ///   daño final = FÓRMULA DEL GRIMORIO (la base que pasamos al
    ///   invocar: nivel, despertar, lo devorado) × LAS ARMADURAS Y
    ///   ACCESORIOS DEL PORTADOR (GetTotalDamage(Summon) — vivo, se
    ///   recalcula cada tick: equipar una banda de invocación en plena
    ///   pelea ENGORDA al escuadrón al instante).
    ///
    /// Los caminos de VANILLA no se tocan: ItemCheck_Shoot y
    /// SpawnMinionOnCursor escriben SU originalDamage DESPUÉS del
    /// spawn (nuestro valor queda pisado por el suyo — el juego queda
    /// intacto); el guard `originalDamage == 0` además respeta a
    /// cualquiera que ya lo haya fijado bien. Curados de una vez (los
    /// seis minions de la casa): el Ojo Eterno, el Fragmento de
    /// Supernova, el Living Pulsar, la Nebula Jellyfish, el Stellar
    /// Comet y la sierpe — y cualquiera que nazca en el futuro.
    /// </summary>
    public class MinionOriginalDamageGlobal : GlobalProjectile
    {
        public override void OnSpawn(Projectile projectile, Terraria.DataStructures.IEntitySource source)
        {
            try
            {
                if ((projectile.minion || projectile.sentry) &&
                    projectile.originalDamage <= 0 &&
                    projectile.damage > 0)
                {
                    projectile.originalDamage = projectile.damage;
                }
            }
            catch { }
        }
    }
}
