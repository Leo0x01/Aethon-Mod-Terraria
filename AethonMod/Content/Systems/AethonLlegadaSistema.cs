using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.NPCs;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Systems
{
    // ======================================================================
    //  LA LLEGADA DE LA LUZ (v6.50.42) — EL RELOJ, EL CERROJO Y EL TEMBLOR.
    //
    //  LA OSCURIDAD MURIÓ (v6.50.39/.40 → 41): «mejor quita la capa de
    //  oscuridad, no se ve nada bien, se ve horrible». VeloLib (el velo
    //  bajo la interfaz con agujeros de luz), EL SOL NEGRO, las luces
    //  del frame y el aviso del Grimorio fueron BORRADOS de raíz. Este
    //  sistema queda con sus DOS trabajos de siempre:
    //
    //  1. EL ESPEJO + EL RELOJ: reconstruye el estado de LA LLEGADA en
    //     TODAS las máquinas leyendo ai[] del jefe (el servidor manda,
    //     cada cliente padece) y maneja el tiempo — CON LA REGLA DE LA
    //     v6.50.41: «el sol no se haga teletransportación… si está más
    //     allá del centro, un día completo avanza con noche completa,
    //     un nuevo día hasta el amanecer; si está antes del centro, se
    //     adelanta hasta llegar al centro». EL RESTANTE se mide hasta
    //     el PRÓXIMO mediodía (POR LA NOCHE si el sol ya pasó el
    //     centro) y el reloj corre en proporción a esa distancia: lejos
    //     vuela (110× — el sol ATRAVIESA el cielo visiblemente, un día
    //     entero en ~14 s) y al acercarse desacelera en aproximación
    //     (rate = distancia × 0.08, piso 1) hasta ATERRIZAR en 27000
    //     exacto: el sol se POSA en el centro, jamás se teletransporta.
    //     Y EL CERROJO DE LA v6.50.42: con el climax/la pelea vivos, un
    //     reloj que se pasó del mediodía VUELVE activamente a 27000 (el
    //     bug de la .41: pasado 27001 la carrera REVIVÍA por la noche a
    //     110× y el sol del cliente daba la vuelta entera).
    //
    //  2. EL TEMBLOR: los kicks de la casa durante el acto 1 (y suave
    //     mientras el tiempo corre) — cada cliente padece el suyo.
    // ======================================================================
    public class AethonLlegadaSistema : ModSystem
    {
        public override void OnWorldUnload()
        {
            Entierro();
        }

        public override void Unload()
        {
            Entierro();
        }

        private static void Entierro()
        {
            AethonBoss.SubLlegada = 0;
            AethonBoss.TiempoCorriendo = false;
            AethonBoss.TiempoCongelado = false;
        }

        // ==================================================================
        //  EL ESPEJO — PRE UPDATE TIME (corre en TODAS las máquinas,
        //  DESPUÉS de la IA del jefe y ANTES de UpdateTime: el reloj
        //  obedece en el MISMO tick en que la IA decide)
        // ==================================================================
        public override void PreUpdateTime()
        {
            if (Main.gameMenu)
            {
                Entierro();
                return;
            }

            NPC jefe = BuscarJefe();

            if (jefe == null)
            {
                // la luz murió o se fue: el reloj vuelve al mundo.
                Entierro();
                return;
            }

            bool enLlegada = jefe.ai[0] == 0f;                 // EST_NACIENDO
            int sub = enLlegada ? (int)jefe.ai[1] : 0;
            AethonBoss.SubLlegada = sub;
            bool enPelea = jefe.ai[0] >= 1f && jefe.ai[0] <= 7f;
            bool muriendo = jefe.ai[0] == 99f;

            // === EL RELOJ — LA REGLA DE LA v6.50.41 + EL CERROJO DE LA .42
            // «en el centro» es la VENTANA del mediodía [26999, 27001]:
            // la TARDE ya no cuenta como centro (el bug del salto hacia
            // atrás). Cada máquina corre SU propio reloj hacia el
            // PRÓXIMO mediodía — por la noche si hace falta — y el
            // aterrizaje es CONVERGENTE: nadie teletransporta nada.
            bool climaxLucha = (sub >= 12 || enPelea) && !muriendo;

            // EL CERROJO DE LA v6.50.42 — LA CONGELACIÓN ACTIVA (corre
            // ANTES de leer el estado, para que el tick que corrige
            // TAMBIÉN congele): un reloj que se PASÓ del mediodía
            // (cliente adelantado, deriva de red) VUELVE al centro —
            // un snap sub-tick, invisible. El bug de la .41: pasado
            // 27001, «enCentro» moría, la carrera REVIVÍA por la rama
            // «próximo mediodía por la noche» y el sol del cliente daba
            // LA VUELTA COMPLETA a 110×. Y si la deriva fue tan grande
            // que hasta cambió el día: de vuelta al mediodía, sin
            // vueltas.
            if (climaxLucha)
            {
                if (!Main.dayTime)
                {
                    Main.dayTime = true;
                    Main.time = 27000.0;
                }
                else if (Main.time > 27001.0)
                {
                    Main.time = 27000.0;
                }
            }

            bool enCentro = Main.dayTime &&
                Main.time >= 26999.0 && Main.time <= 27001.0;

            // corre mientras: la carrera oficial (sub 11)… o lo que falte
            // para llegar (un cliente que se enteró tarde del climax
            // TERMINA su carrera: el sol llega al centro, no salta).
            AethonBoss.TiempoCorriendo =
                (sub == 11 || (climaxLucha && Main.dayTime && Main.time < 26999.0)) &&
                !muriendo;

            // congela: el climax en adelante — pero SOLO si ya llegó (un
            // reloj a medio camino sigue corriendo hasta clavarse solo).
            AethonBoss.TiempoCongelado = climaxLucha && enCentro;

            // el aterrizaje exacto: si el propio reloj pasó de 26999 (la
            // desaceleración aterriza a ≤ 1 unidad por tick), clavarlo — es
            // MENOS de un tick de sol: nadie lo ve, no es un salto.
            if (AethonBoss.TiempoCorriendo && Main.dayTime &&
                Main.time >= 26999.0 && Main.time < 27000.0)
                Main.time = 27000.0;

            // === EL TEMBLOR DE LA LLEGADA (los kicks de la casa — sub 10
            //     crece hasta 13 px, sub 11 se sostiene suave mientras el
            //     tiempo corre) ==============================================
            if (Main.netMode != NetmodeID.Server && !Main.dedServ &&
                sub >= 10 && sub <= 11 && (Main.GameUpdateCount % 13u) == 0u)
            {
                float f = sub == 10 ? Math.Min(1f, jefe.ai[2] / 150f) : 0.55f;
                OndaLib.Kick(4f + 9f * f, 13);
            }
        }

        // ==================================================================
        //  EL RELOJ — MODIFY TIME RATE (corre en TODAS las máquinas).
        //
        //  LA CARRERA NATURAL (v6.50.41): «si está más allá del centro,
        //  un día completo avanza con noche completa, un nuevo día hasta
        //  el amanecer; si está antes del centro, se adelanta hasta
        //  llegar al centro». EL RESTANTE se mide hasta el PRÓXIMO
        //  mediodía — POR LA NOCHE si el sol ya pasó el centro — y el
        //  rate corre en proporción a esa distancia (×0.08, techo 110×,
        //  piso 1): lejos, el día entero pasa VISIBLEMENTE en ~14 s (el
        //  sol y la luna ATRAVIESAN el cielo); cerca, la aproximación
        //  desacelera y el sol se POSA en 27000 exacto — como un avión,
        //  NUNCA como un teletransporte.
        // ==================================================================
        public override void ModifyTimeRate(ref double timeRate,
            ref double tileUpdateRate, ref double eventUpdateRate)
        {
            if (AethonBoss.TiempoCorriendo)
            {
                double restante;   // ticks de juego hasta el PRÓXIMO mediodía
                if (Main.dayTime)
                {
                    restante = Main.time < 27000.0
                        ? 27000.0 - Main.time          // ANTES del centro: directo al centro
                        : (Main.dayLength - Main.time) + Main.nightLength + 27000.0;
                        // MÁS ALLÁ del centro: el resto del día + la noche
                        // COMPLETA + el amanecer + la mañana del nuevo día
                }
                else
                {
                    restante = (Main.nightLength - Main.time) + 27000.0;
                        // de noche: el resto de la noche + la mañana entera
                }
                timeRate = Math.Min(110.0, Math.Max(1.0, restante * 0.08));
                tileUpdateRate = 1.0;      // el mundo físico sigue normal
                eventUpdateRate = 0.0;     // cero tiradas de eventos en el barrido
            }
            else if (AethonBoss.TiempoCongelado)
            {
                // EL MEDIO DÍA ETERNO: el sol clavado en el centro.
                timeRate = 0.0;
            }
        }

        /// <summary>El jefe de la luz (o null si no está).</summary>
        private static NPC BuscarJefe()
        {
            try
            {
                int tipo = ModContent.NPCType<AethonBoss>();
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n != null && n.active && n.type == tipo) return n;
                }
            }
            catch { }
            return null;
        }
    }
}
