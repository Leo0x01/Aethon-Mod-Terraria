using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.Players.Investigaciones
{
    /// <summary>
    /// SigiloRatero — v6.124 — EL SIGILO RATERO (la adaptación de la casa
    /// del STEALTH de los ladrones de Calamity para las ARMAS DE
    /// INVESTIGACIÓN de Calamity Entropy: Arbitrator II y El Principio y
    /// el Fin — la letra: «investiga y copia al 100% todo sobre el arma
    /// Arbitrator II en CALAMITY ENTROPY» + «copia el arma the beginning
    /// and the end de CALAMITY ENTROPY»).
    ///
    /// LA LEY DEL ORIGINAL: el stealth de Calamity se CARGA mientras el
    /// ladrón NO ataca y se GASTA en el ATAQUE SIGILOSO (la versión
    /// potenciada del arma). LA ADAPTACIÓN DE LA CASA (el mod no tiene
    /// clase ratera): el sigilo se CARGA estando QUIETO (150 ticks — 2,5
    /// s sin moverse ni usar items) y el primer tiro de un arma de
    /// investigación LO GASTA en su versión sigilosa (documentado en cada
    /// arma). El brillo dorado avisa cuándo está LISTO.
    /// </summary>
    public class SigiloRatero : ModPlayer
    {
        /// <summary>Los ticks de carga acumulados (0..CargaTotal).</summary>
        public int Carga;

        /// <summary>La carga completa del sigilo (2,5 s quieto).</summary>
        public const int CargaTotal = 150;

        /// <summary>¿El sigilo está LISTO para el próximo tiro?</summary>
        public bool Cargado => Carga >= CargaTotal;

        public override void ResetEffects()
        {
            // La carga se congela al morir.
            if (Player.dead) return;

            bool quieto = Player.velocity.LengthSquared() < 0.06f &&
                !Player.controlUseItem && !Player.controlUseTile &&
                Player.jump == 0 && !Player.controlJump;

            if (quieto && Carga < CargaTotal)
                Carga++;
            else if (!quieto && Carga < CargaTotal)
                Carga = System.Math.Max(0, Carga - 3);   // moverse lo desgasta

            // EL AVISO: el destello dorado del sigilo listo (una vez, al
            // llenarse) + el polvo del ladrón esperando.
            if (Carga == CargaTotal)
            {
                if (Main.netMode != Terraria.ID.NetmodeID.Server && !Main.dedServ)
                {
                    if (Main.rand.NextBool(6))
                    {
                        Dust d = Dust.NewDustPerfect(
                            Player.Center + Main.rand.NextVector2Circular(18f, 24f),
                            Terraria.ID.DustID.Enchanted_Gold,
                            new Vector2(0f, -Main.rand.NextFloat(0.4f, 1.1f)),
                            200, new Color(255, 232, 160), Main.rand.NextFloat(0.5f, 0.9f));
                        d.noGravity = true;
                        d.fadeIn = 0f;
                    }
                }
            }
        }

        /// <summary>GASTA el sigilo (lo llama el tiro sigiloso del arma de
        /// investigación — devuelve true si HABÍA sigilo que gastar).</summary>
        public bool Gastar()
        {
            if (!Cargado) return false;
            Carga = 0;
            return true;
        }
    }
}
