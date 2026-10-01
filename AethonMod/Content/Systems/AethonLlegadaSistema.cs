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
    //  LA LLEGADA DE LA LUZ (v6.50.42 → v6.50.51) — EL ESPEJO, EL RELOJ
    //  BIDIRECCIONAL Y EL TEMBLOR.
    //
    //  v6.50.51 — LA CARRERA AL MEDIODÍA VOLVIÓ… Y APRENDE A RETROCEDER
    //  (la letra: «mantén estas características para el jefe (temblor/
    //  reloj/pilar/descenso), pero con un cambio, el tiempo avanza o
    //  retrocede en consecuencia de qué tan lejos o cerca esté el sol
    //  del objetivo que es tenerlo en centro, ejemplo: si el sol está
    //  cerca del centro pero ya lo pasó, entonces el tiempo retrocede;
    //  si el sol está oculto pero la noche acaba de comenzar, entonces
    //  el tiempo retrocede; si la noche está por terminar el tiempo
    //  avanza; si el sol acaba de salir, el tiempo avanza»).
    //
    //  LA REGLA NUEVA — EL CAMINO MÁS CORTO AL MEDIODÍA: la posición del
    //  sol en el ciclo de 24 h contra las 12:00 (el CENTRO del cielo):
    //    · MADRUGADA/MAÑANA (el sol antes del centro, la noche por
    //      terminar, el sol recién salido): AVANZA — vanilla aplica el
    //      rate y cruza alba/ocaso ella sola (el camino PROBADO de la
    //      v6.50.41: un día entero pasa VISIBLEMENTE en ~8 s).
    //    · TARDE/NOCHE TEMPRANA (el sol ya pasó el centro, la noche que
    //      acaba de comenzar): RETROCEDE — vanilla solo sabe SUMAR: el
    //      paso hacia atrás lo da ESTE espejo A MANO (en PreUpdateTime,
    //      con el mismo rate), con los cruces en reversa (atardecer→
    //      tarde, alba→noche) manejados aquí — Main.time NUNCA queda
    //      negativa después del tick.
    //  La NOCHE se parte en la MEDIANOCHE (el punto equidistante): la
    //  noche que empieza retrocede al atardecer→tarde→mediodía; la noche
    //  por terminar avanza al alba→mañana→mediodía. La VELOCIDAD sigue
    //  la ley de la .41 (rate = distancia·0.08, techo 110×, piso 1):
    //  lejos vuela, cerca desacelera, y el sol se POSA en 27000 exacto —
    //  NUNCA teletransporta (el aterrizaje [26999, 27001] es sub-tick).
    //  Todo determinista (cero Main.rand): cada máquina corre SU propio
    //  reloj y todas convergen — y el cliente atrasado que se entera
    //  tarde del climax TERMINA su carrera (no salta: ATERRIZA).
    //
    //  EL CERROJO DE LA v6.50.42 sigue vivo: con el climax/la pelea
    //  vivos, el reloj descansa CLAVADO en el mediodía (el bug de la
    //  .41 — pasado 27001 la carrera REVIVÍA y el sol daba la vuelta
    //  entera — murió y sigue muerto).
    //
    //  EL TEMBLOR (los kicks de la casa) vuelve con su coreografía:
    //  sub 10 crece hasta 13 px, sub 11 se sostiene suave mientras el
    //  tiempo corre — cada cliente padece el suyo.
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
        //  EL ESPEJO + LA CARRERA — PRE UPDATE TIME (corre en TODAS las
        //  máquinas, DESPUÉS de la IA del jefe y ANTES de UpdateTime: el
        //  reloj obedece en el MISMO tick en que la IA decide — el orden
        //  verificado del decompile: NPCs → PreUpdateTime → UpdateTime).
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

            bool enEntrada = jefe.ai[0] == 0f;                 // EST_NACIENDO
            int sub = enEntrada ? (int)jefe.ai[1] : 0;
            AethonBoss.SubLlegada = sub;
            // v6.50.45 — LOS ESTADOS NUEVOS (8 reloj · 9 coro · 10 manada ·
            // 11 telar · 12 decreto) TAMBIÉN SON LA PELEA: el mediodía
            // clavado los cubre (si no, el sol se escapaba a media manada).
            bool enPelea = jefe.ai[0] >= 1f && jefe.ai[0] <= 12f;
            bool muriendo = jefe.ai[0] == 99f;

            // === EL ESTADO DEL RELOJ ===
            // «en el centro» es la VENTANA del mediodía [26999, 27001].
            bool enCentro = Main.dayTime &&
                Main.time >= 26999.0 && Main.time <= 27001.0;

            // EL CLIMAX Y LA PELEA: el mediodía es ETERNO mientras la luz viva.
            bool climaxLucha = (sub >= 12 || enPelea) && !muriendo;

            // ¿CORRE? la carrera oficial (sub 11)… o lo que falte para
            // llegar (un cliente que se enteró tarde del climax TERMINA
            // su carrera: el sol llega al centro, no salta — el camino
            // más corto elige la dirección, AVANCE o RETROCESO).
            AethonBoss.TiempoCorriendo =
                (sub == 11 || (climaxLucha && !enCentro)) && !muriendo;

            // EL CERROJO DE LA v6.50.42 — SOLO si el reloj NO está
            // corriendo (el que corre ATERRIZA solo, desacelerando): un
            // reloj de climax fuera de la ventana VUELVE al centro — y si
            // la deriva cambió hasta el día, de vuelta al mediodía, sin
            // vueltas (el bug de la .41: la carrera que REVIVÍA por la
            // noche a 110× y daba la VUELTA COMPLETA).
            if (climaxLucha && !AethonBoss.TiempoCorriendo)
            {
                if (!Main.dayTime)
                {
                    Main.dayTime = true;
                    Main.time = 27000.0;
                }
                else if (Main.time > 27001.0 || Main.time < 26999.0)
                    Main.time = 27000.0;
            }

            // congela: el climax en adelante, con el reloj ya posado.
            AethonBoss.TiempoCongelado = climaxLucha && !AethonBoss.TiempoCorriendo;

            // === LA CARRERA EN REVERSA (v6.50.51) — el paso hacia atrás
            //     lo da el espejo A MANO (vanilla solo sabe SUMAR): el
            //     MISMO rate que leería ModifyTimeRate (misma fórmula,
            //     misma máquina — determinista), y los cruces de alba y
            //     ocaso EN REVERSA quedan resueltos aquí mismo: Main.time
            //     NUNCA sobrevive negativa a este tick ===
            if (AethonBoss.TiempoCorriendo)
            {
                double hacia = TicksHaciaElMediodia();
                if (hacia < 0.0)
                {
                    double paso = Math.Min(110.0, Math.Max(1.0, -hacia * 0.08));
                    Main.time -= paso;
                    if (Main.time < 0.0)
                    {
                        if (Main.dayTime)
                        {
                            // EL OCASO EN REVERSA: de la tarde de vuelta a la noche.
                            Main.dayTime = false;
                            Main.time += Main.nightLength;
                        }
                        else
                        {
                            // EL ALBA EN REVERSA: de la madrugada de vuelta al día.
                            Main.dayTime = true;
                            Main.time += Main.dayLength;
                        }
                    }
                }

                // EL ATERRIZAJE (la corrección sub-tick — menos de un tick
                // de sol: nadie lo ve, no es un salto).
                if (Main.dayTime &&
                    Main.time >= 26999.0 && Main.time < 27000.0)
                    Main.time = 27000.0;
            }

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
        //  AVANZA: vanilla aplica el rate (y cruza alba/ocaso ella sola —
        //  el camino PROBADO de la .41-.48, un día entero VISIBLEMENTE).
        //  RETROCEDE: rate 0 para vanilla — el paso ya lo dio el espejo
        //  en PreUpdateTime (misma fórmula, mismo tick).
        // ==================================================================
        public override void ModifyTimeRate(ref double timeRate,
            ref double tileUpdateRate, ref double eventUpdateRate)
        {
            if (AethonBoss.TiempoCorriendo)
            {
                double hacia = TicksHaciaElMediodia();
                if (hacia >= 0.0)
                {
                    // AVANZA (madrugada/mañana): la ley de la v6.50.41 —
                    // rate = distancia·0.08, techo 110×, piso 1.
                    timeRate = Math.Min(110.0, Math.Max(1.0, hacia * 0.08));
                }
                else
                {
                    // RETROCEDE (tarde/noche temprana): vanilla QUIETA —
                    // el paso hacia atrás ya lo dio el espejo.
                    timeRate = 0.0;
                }
                tileUpdateRate = 1.0;      // el mundo físico sigue normal
                eventUpdateRate = 0.0;     // cero tiradas de eventos en el barrido
            }
            else if (AethonBoss.TiempoCongelado)
            {
                // EL MEDIO DÍA ETERNO: el sol clavado en el centro.
                timeRate = 0.0;
            }
        }

        /// <summary>
        /// v6.50.51 — LOS TICKS HACIA EL MEDIODÍA, CON SIGNO. La posición
        /// del sol en el ciclo de 24 h contra las 12:00 (el CENTRO), POR
        /// EL CAMINO MÁS CORTO:
        ///   · + = AVANZAR (madrugada/mañana: «si el sol acaba de salir,
        ///     el tiempo avanza» / «si la noche está por terminar, avanza»);
        ///   · − = RETROCEDER (tarde/noche temprana: «si está cerca del
        ///     centro pero ya lo pasó, retrocede» / «si la noche acaba de
        ///     comenzar, retrocede»).
        /// La NOCHE se parte en la MEDIANOCHE (el punto equidistante).
        /// 0 = ya está en la ventana del mediodía.
        /// </summary>
        private static double TicksHaciaElMediodia()
        {
            // LA VENTANA DEL MEDIODÍA (el aterrizaje — no hay carrera que dar).
            if (Main.dayTime && Main.time >= 26999.0 && Main.time <= 27001.0)
                return 0.0;

            // LA POSICIÓN DEL SOL en horas del ciclo (Terraria: el día corre
            // 4:30→19:30 por Main.time 0→dayLength; la noche 19:30→4:30).
            double hora = Main.dayTime
                ? 4.5 + Main.time / Main.dayLength * 15.0
                : 19.5 + Main.time / Main.nightLength * 9.0;

            // EL LADO DEL CENTRO (−12..+12: − = antes del mediodía, la
            // medianoche exacta cuenta como «antes» → AVANZA).
            double d = hora % 24.0 - 12.0;

            if (d < 0.0)
            {
                // AVANZAR: el PRÓXIMO mediodía yendo hacia adelante (la
                // fórmula de la .41 — por la noche completa si hace falta).
                if (Main.dayTime)
                    return Main.time < 27000.0
                        ? 27000.0 - Main.time
                        : (Main.dayLength - Main.time) + Main.nightLength + 27000.0;
                return (Main.nightLength - Main.time) + 27000.0;
            }
            else
            {
                // RETROCEDER: lo que ya pasó desde el mediodía (la tarde
                // directa, o la noche nueva + el atardecer completo).
                if (Main.dayTime)
                    return -(Main.time - 27000.0);
                return -(Main.time + 27000.0);
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
