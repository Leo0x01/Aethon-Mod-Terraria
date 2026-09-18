using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.Players
{
    /// <summary>
    /// ApuestasPlayer — v6.42 — EL ESTADO DE LAS CINCO APUESTAS.
    ///
    /// El estado compartido de las cinco armas de las apuestas que vive
    /// en el JUGADOR (no en los proyectiles): la ventana de la guardia
    /// de la égida (y su parry), el aturdimiento del parry fallido, el
    /// enfriamiento de la guardia, EL HAMBRE de la guadaña (el daño de
    /// los proyectiles enemigos que sus fisuras han tragado) y el
    /// enfriamiento de la resonancia del eco.
    ///
    /// CONVENCIONES DE LA CASA: el parry se decide en FreeDodge (el
    /// único punto de la tubería de daño donde un mod puede cancelar
    /// un golpe ya calculado — patrón del esquiva-ninja de vanilla),
    /// el bloqueo tardío rebaja en ModifyHurt y el aturdimiento del
    /// parry fallido bloquea el uso de ítems en PreItemCheck (el hook
    /// que envuelve TODO el pipeline de ítems de vanilla).
    /// </summary>
    public class ApuestasPlayer : ModPlayer
    {
        // === LA ÉGIDA DE NOVA ===
        /// <summary>Ticks restantes de la guardia alzada (18 al alzarla;
        /// los primeros 8 son LA VENTANA PERFECTA del parry).</summary>
        public int GuardiaTicks;

        /// <summary>Enfriamiento de la guardia (480 ticks = 8 s).</summary>
        public int EnfriamientoGuardia;

        /// <summary>El aturdimiento del parry fallido (30 ticks = 0,5 s).</summary>
        public int Aturdido;

        /// <summary>Marca que la guardia ACTUAL ya paró (para no aturdir
        /// al expirar una guardia que ya cumplió).</summary>
        public bool ParryHecho;

        // === EL ECO CUÁNTICO ===
        /// <summary>Enfriamiento de la resonancia (300 ticks = 5 s).</summary>
        public int ResonanciaCooldown;

        // === LA GUADAÑA DEL DESGARRO ===
        /// <summary>EL HAMBRE: el daño devorado por las fisuras, listo
        /// para SUMARSE al próximo tajo (tope puesto por la fisura).</summary>
        public int HambreGuadana;

        public override void PreUpdate()
        {
            // LOS RELOJES — todos cuentan hacia abajo, todos con suelo.
            if (GuardiaTicks > 0) GuardiaTicks--;
            if (EnfriamientoGuardia > 0) EnfriamientoGuardia--;
            if (Aturdido > 0) Aturdido--;
            if (ResonanciaCooldown > 0) ResonanciaCooldown--;
        }

        /// <summary>
        /// EL PARRY — la ventana perfecta. FreeDodge solo corre para el
        /// jugador local y SOLO para golpes esquivables (contacto y
        /// proyectiles enemigos): si la guardia está en sus primeros 8
        /// ticks, el golpe NO EXISTE — y en su lugar detona la nova.
        /// </summary>
        public override bool FreeDodge(Player.HurtInfo info)
        {
            if (GuardiaTicks >= 11 && info.Dodgeable)
            {
                // La guardia consumió su suerte: ventana cerrada,
                // enfriamiento abierto, invulnerabilidad de regalo.
                GuardiaTicks = 0;
                ParryHecho = true;
                EnfriamientoGuardia = 480;
                Player.SetImmuneTimeForAllTypes(60);

                // LA NOVA DEL PARRY (en la posición del jugador, con el
                // índice de dirección del golpe como empujón).
                if (Main.netMode != Terraria.ID.NetmodeID.Server)
                {
                    int nova = Projectile.NewProjectile(Player.GetSource_FromThis(),
                        Player.Center, Vector2.Zero,
                        ModContent.ProjectileType<Projectiles.Cosmic.NovaParryProjectile>(),
                        Player.HeldItem != null ? Player.HeldItem.damage : 50,
                        9f, Player.whoAmI);
                    if (nova >= 0)
                        Main.projectile[nova].CritChance = 100;
                }
                return true;      // el golpe se deshace — vanilla devuelve 0.0
            }
            return false;
        }

        /// <summary>
        /// EL BLOQUEO TARDÍO — la guardia aún cubre (ticks 11..1) pero la
        /// ventana perfecta pasó: el golpe entra, pero AMORTIGUADO.
        /// </summary>
        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            if (GuardiaTicks > 0)
                modifiers.FinalDamage *= 0.3f;
        }

        /// <summary>
        /// EL ATURDIMIENTO DEL PARRY FALLIDO — mientras dura, el jugador
        /// no puede usar ítems (PreItemCheck envuelve ItemCheck_Inner de
        /// vanilla: nada de atacar, nada de canalizar). Moverse sí: es
        /// la recuperación, no la parálisis.
        /// </summary>
        public override bool PreItemCheck()
        {
            return Aturdido <= 0;
        }
    }
}
