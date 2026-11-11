using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Buffs;
using AethonMod.Content.Items.Accessories;
using AethonMod.Content.Projectiles;

namespace AethonMod.Content.Globals
{
    /// <summary>
    /// VacioGlobalNPC — v6.101 → v6.102 → v6.109 — EL MOTOR DE LA
    /// QUEMADURA DEL VACÍO EXTERIOR (la investigación del estandarte
    /// para el grimorio).
    ///
    /// LA LETRA DEL USUARIO: «cada enemigo que entre al radio de efecto
    /// del estandarte se quema y visualmente se nota como cada vez se
    /// quema mas y como varias particulas crean algo asi como una capa
    /// de resistencia igual a la de los meteoritos entrando a la
    /// atmosfera y todo con energia morada y negra».
    ///
    /// ESTE archivo es EL MOTOR COMPLETO del debuff: cualquier fuente
    /// que aplique <see cref="QuemaduraVacioExterior"/> obtiene TODO
    /// esto (desde la v6.109 la ÚNICA fuente viva es EL AURA DEL VACÍO
    /// — el ancla AuraJuicio2Ancla; el estandarte y el aura de juicio
    /// originales MURIERON por la letra del usuario):
    ///
    ///  · EL ESTADO por enemigo: la INTENSIDAD (×1→×3 por cercanía, la
    ///    técnica Remap del original) y el DUEÑO (quién arde por quién
    ///    — el portador cobra el buff Vacío Exterior).
    ///  · LA LENTITUD (PostAI): cuanto más se ACERCA al portador del
    ///    campo, más LENTO camina — la caída de velocidad es SUAVE por
    ///    cercanía (×1 en el borde → ×0.55 pegado al cuerpo). En PostAI
    ///    la IA ya escribió SU velocidad: el freno pisa DESPUÉS y
    ///    afecta a TODOS los enemigos, caminantes y voladores (y desde
    ///    la v6.109, también a los espíritus — hasta los fantasmas
    ///    sienten la resistencia de la atmósfera del vacío).
    ///  · LA RAMPA VISUAL (0→1 ardiendo): la leen los vestidos del
    ///    ancla (el arco de choque, el plasma y la bruma crecen con
    ///    ella — el impío arde MÁS cada segundo que pasa dentro).
    ///
    /// v6.109 — LA GRAN LIMPIEZA: el vestido del estandarte (el frente
    /// de compresión + el destello + el manto de 16 del PostDraw) murió
    /// CON el estandarte — el AURA DEL VACÍO viste a sus impíos con SU
    /// propio teatro (el arco de choque, las chispas del estancamiento
    /// y la bruma de verdad — todo vive en AuraJuicio2Ancla).
    /// </summary>
    public class VacioGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        /// <summary>LA INTENSIDAD de la quemadura (×1 lejos → ×3 pegado,
        /// más la rampa de tiempo ×1→×1.5). La escribe la fuente (el
        /// ancla del Aura del Vacío). 0 = no arde.</summary>
        public float IntensidadQuemadura;

        /// <summary>EL DAÑO POR SEGUNDO base de la quemadura a intensidad
        /// ×1 (escala con el nivel de despertar del dueño).</summary>
        public int DanoVacio;

        /// <summary>EL DUEÑO de la quemadura (player.whoAmI + 1; 0 = no
        /// tiene dueño). Mientras un enemigo arde por él, el portador
        /// cobra el buff Vacío Exterior.</summary>
        public int DuenoVacio;

        /// <summary>LA RAMPA VISUAL local (0→1 en ~2 s ardiendo): cada
        /// vez se quema MÁS — el arco de choque, el plasma y la bruma
        /// crecen con ella. Se enfría sola cuando el debuff se apaga.</summary>
        public float VisualQuemadura;

        /// <summary>LA CERCANÍA local al portador (0 en el borde del
        /// radio → 1 pegado al cuerpo) — la que frena y la que enciende
        /// el calentamiento aerodinámico.</summary>
        public float CercaniaLocal;

        /// <summary>v6.124 — ¿está DENTRO del radio del aura AHORA MISMO?
        /// Lo escribe el ancla cada tick (true = el aura lo está
        /// tocando: el DAÑO POR CERCANÍA de la letra; false = salió:
        /// la quemadura SIGUE por sus últimos ticks con el 1% de la
        /// vida máxima por medio segundo — la persistencia de 5 s).</summary>
        public bool DentroDelCampo;

        // ================================================================
        //  LA LENTITUD + LA RAMPA VISUAL — cada tick de vida del NPC.
        // ================================================================
        public override void PostAI(NPC npc)
        {
            if (Main.gameMenu || !npc.active)
                return;

            bool arde = TieneQuemadura(npc);
            if (arde)
            {
                // LA RAMPA: cada vez se quema más (0→1 en 120 t).
                VisualQuemadura = Math.Min(1f, VisualQuemadura + 1f / 120f);

                // LA CERCANÍA al portador del campo MÁS CERCANO (la
                // misma técnica Remap del original: radio→pegado = 0→1).
                CercaniaLocal = CalcularCercania(npc);

                // LA LENTITUD DE VERDAD: en PostAI la IA ya escribió SU
                // velocidad — el freno del vacío pisa DESPUÉS y vale para
                // TODOS (los que caminan a velocidad fija incluidos… y
                // los espíritus que atraviesan paredes también: la
                // resistencia de la atmósfera no entiende de física
                // fantasmal).
                if (CercaniaLocal > 0f && Main.netMode != NetmodeID.MultiplayerClient)
                    npc.velocity *= 1f - 0.45f * CercaniaLocal;
            }
            else
            {
                // EL ENFRIAMIENTO: fuera del fuego el frente se apaga solo.
                if (VisualQuemadura > 0f)
                    VisualQuemadura = Math.Max(0f, VisualQuemadura - 0.05f);
                if (VisualQuemadura <= 0f)
                {
                    CercaniaLocal = 0f;
                    IntensidadQuemadura = 0f;
                }
            }
        }

        /// <summary>¿El NPC arde con la Quemadura del Vacío Exterior?</summary>
        public static bool TieneQuemadura(NPC npc) =>
            npc != null && npc.active && npc.HasBuff(ModContent.BuffType<QuemaduraVacioExterior>());

        /// <summary>¿El jugador PORTA EL CAMPO DEL VACÍO (el Aura del
        /// Vacío — antes «aura de juicio 2»)? La lentitud y la cercanía
        /// son del MOTOR COMÚN: el campo que marca con la quemadura del
        /// vacío exterior frena a los impíos que se le acercan.</summary>
        public static bool PortaCampoDeJuicio(Player p) =>
            AuraDeJuicio2Item.LlevaElAura(p);

        /// <summary>LA CERCANÍA (0..1): la distancia al rectángulo del NPC
        /// desde el portador del campo más cercano, remapeada del RADIO
        /// del campo (300 px — el borde del vacío) a 40 px (pegado).</summary>
        private static float CalcularCercania(NPC npc)
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p == null || !p.active || p.dead)
                    continue;
                if (!PortaCampoDeJuicio(p))
                    continue;
                Vector2 borde = Vector2.Clamp(p.Center, npc.TopLeft, npc.BottomRight);
                float d = Vector2.Distance(p.Center, borde);
                if (d < AuraJuicio2Ancla.RADIO)
                    return Remap(d, AuraJuicio2Ancla.RADIO, 40f, 0f, 1f);
            }
            return 0f;
        }

        /// <summary>El Remap acotado del original.</summary>
        private static float Remap(float v, float in1, float in2, float out1, float out2)
        {
            float k = (v - in1) / (in2 - in1);
            if (k < 0f) k = 0f;
            else if (k > 1f) k = 1f;
            return out1 + (out2 - out1) * k;
        }
    }
}
