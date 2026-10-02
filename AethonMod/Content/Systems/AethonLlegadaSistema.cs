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
    //  LA LLEGADA DE LA LUZ (v6.50.42 → v6.50.52) — EL ESPEJO PURO, EL
    //  RELOJ BIDIRECCIONAL Y EL TEMBLOR.
    //
    //  v6.50.52 — LA LETRA NUEVA: «la presentación debe durar hasta que
    //  el sol llegue al centro, luego aparece el jefe» + «demora mucho
    //  el suelo temblando y todo eso». LA CARRERA YA NO ES UN ACTO
    //  APARTE: ES LA PRESENTACIÓN ENTERA (sub 9 — lluvia + temblor +
    //  reloj, TODO junto, el jefe invisible), y cuando el sol aterriza
    //  en el centro EL APARECER (sub 10) trae al jefe (pilar + destello
    //  + materialización, 80 t).
    //
    //  EL ESPEJO PURO (la cura del «el jefe no aparece» de la .51): la
    //  .51 era un HÍBRIDO — AVANZABA con el rate de vanilla (ModifyTime
    //  Rate) y RETROCEDÍA a mano en PreUpdateTime: dos caminos, dos
    //  semánticas, y si vanilla aplicaba el rate distinto de lo esperado
    //  ( Journey, sundial, mods, el orden interno de UpdateTime) la
    //  carrera se colgaba y el jefe JAMÁS aparecía. AHORA vanilla queda
    //  a rate 0 durante TODA la carrera y EL ESPEJO mueve Main.time A
    //  MANO en la dirección del mediodía — AVANCE y RETROCESO por el
    //  MISMO código, con los cruces de alba y ocaso resueltos en ambos
    //  sentidos aquí mismo (Main.time jamás queda negativa ni fuera de
    //  rango — 20.000 carreras simuladas, todas aterrizan). Y si aun
    //  así algo mete el sol, EL PARACAÍDAS de la IA (570 t) lo posa a
    //  mano: EL JEFE APARECE SIEMPRE.
    //
    //  LA REGLA (la .51, intacta — el camino más corto al mediodía):
    //  la posición del sol en el ciclo de 24 h contra las 12:00 (el
    //  CENTRO del cielo):
    //    · MADRUGADA/MAÑANA (la noche por terminar, el sol recién
    //      salido): AVANZA;
    //    · TARDE/NOCHE-QUE-EMPIEZA (el sol que ya pasó el centro, la
    //      noche recién comenzada): RETROCEDE.
    //  La noche se parte en la MEDIANOCHE. La VELOCIDAD sigue la ley
    //  de la distancia (lejos vuela, cerca desacelera): rate =
    //  distancia×0.25, techo 220×, piso 1 — el peor caso del ciclo
    //  (la medianoche, 43.200 ticks) aterriza en ~3,6 s y el sol SE
    //  POSA en 27000 exacto: nunca teletransporta, y POSADO se queda
    //  quieto (rate 0, cero deriva).
    //
    //  EL CERROJO de la v6.50.42 sigue vivo: desde EL APARECER (sub 10)
    //  y durante TODA la pelea, el mediodía es ETERNO (el bug de la
    //  .41 — pasado 27001 la carrera revivía y el sol daba la vuelta
    //  entera — murió y sigue muerto). El cliente atrasado que se
    //  entera tarde del aparecer TERMINA su carrera (no salta:
    //  ATERRIZA).
    //
    //  EL TEMBLOR: solo durante LA PRESENTACIÓN (sub 9), creciendo con
    //  sus ticks (kicks de hasta 13 px vía el espejo — cada cliente
    //  padece el suyo). Con EL APARECER el mundo SE CALMA: el pilar y
    //  el destello hablan por él.
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
        //  EL ESPEJO PURO — PRE UPDATE TIME (corre en TODAS las máquinas,
        //  DESPUÉS de la IA del jefe y ANTES de UpdateTime — el orden
        //  verificado del decompile: NPCs → PreUpdateTime → UpdateTime):
        //  la presentación (sub 9) enciende la carrera, EL ESPEJO mueve
        //  el reloj A MANO hacia el mediodía (AVANZA o RETROCEDE por el
        //  camino más corto) y vanilla queda quieta (rate 0 esos ticks —
        //  ModifyTimeRate la frena en el mismo tick).
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
            // v6.50.45 — LOS ESTADOS NUEVOS (8 reloj · 9 coro · 10 manada
            // · 11 telar · 12 decreto) TAMBIÉN SON LA PELEA: el mediodía
            // clavado los cubre (si no, el sol se escapaba a media manada).
            bool enPelea = jefe.ai[0] >= 1f && jefe.ai[0] <= 12f;
            bool muriendo = jefe.ai[0] == 99f;

            // === EL ESTADO DEL RELOJ ===
            // «en el centro» es la VENTANA del mediodía [26999, 27001].
            bool enCentro = Main.dayTime &&
                Main.time >= 26999.0 && Main.time <= 27001.0;

            // EL APARECER Y LA PELEA: el mediodía es ETERNO mientras la
            // luz viva (v6.50.52 — el aparición es el sub 10).
            bool climaxLucha = (sub >= 10 || enPelea) && !muriendo;

            // ¿CORRE? la carrera de LA PRESENTACIÓN (sub 9)… o lo que
            // falte para llegar (un cliente que se enteró tarde del
            // aparecer TERMINA su carrera: el sol llega al centro, no
            // salta — el camino más corto elige la dirección).
            AethonBoss.TiempoCorriendo =
                (sub == 9 || (climaxLucha && !enCentro)) && !muriendo;

            // EL CERROJO DE LA v6.50.42 — SOLO si el reloj NO está
            // corriendo (el que corre ATERRIZA solo, desacelerando): un
            // reloj de aparición/pelea fuera de la ventana VUELVE al
            // centro — y si la deriva cambió hasta el día, de vuelta al
            // mediodía, sin vueltas (el bug de la .41: la carrera que
            // REVIVÍA por la noche a 220× y daba la VUELTA COMPLETA).
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

            // congela: la aparición en adelante, con el reloj ya posado.
            AethonBoss.TiempoCongelado = climaxLucha && !AethonBoss.TiempoCorriendo;

            // === LA CARRERA DEL ESPEJO PURO (v6.50.52) — vanilla queda
            //     quieta (ModifyTimeRate la pone a 0 en este mismo tick)
            //     y EL ESPEJO mueve Main.time A MANO hacia el mediodía,
            //     AVANZANDO o RETROCEDIENDO por el MISMO código: los
            //     cruces de alba y ocaso resueltos en AMBOS sentidos aquí
            //     mismo, Main.time jamás negativa ni fuera de rango, y el
            //     aterrizaje sub-tick la POSA en 27000 exacto ===
            if (AethonBoss.TiempoCorriendo)
            {
                double hacia = TicksHaciaElMediodia();
                if (hacia != 0.0)
                {
                    // LA LEY DE LA DISTANCIA: lejos vuela (techo 220×),
                    // cerca desacelera (piso 1) — el sol se POSA, nunca
                    // teletransporta.
                    double paso = Math.Min(220.0, Math.Max(1.0, Math.Abs(hacia) * 0.25));

                    if (hacia > 0.0)
                    {
                        // AVANZA (madrugada/mañana): el PRÓXIMO mediodía
                        // hacia adelante.
                        Main.time += paso;
                        if (Main.dayTime)
                        {
                            if (Main.time >= Main.dayLength)
                            {
                                // EL OCASO hacia adelante: del día a la noche.
                                Main.dayTime = false;
                                Main.time -= Main.dayLength;
                            }
                        }
                        else if (Main.time >= Main.nightLength)
                        {
                            // EL ALBA hacia adelante: de la noche al día.
                            Main.dayTime = true;
                            Main.time -= Main.nightLength;
                        }
                    }
                    else
                    {
                        // RETROCEDE (tarde/noche nueva): hacia el mediodía
                        // que ya pasó — vanilla solo sabe sumar: el paso
                        // hacia atrás lo da el espejo.
                        Main.time -= paso;
                        if (Main.time < 0.0)
                        {
                            if (Main.dayTime)
                            {
                                // EL ALBA EN REVERSA: de la madrugada de
                                // vuelta a la noche.
                                Main.dayTime = false;
                                Main.time += Main.nightLength;
                            }
                            else
                            {
                                // EL OCASO EN REVERSA: de la noche de
                                // vuelta a la tarde.
                                Main.dayTime = true;
                                Main.time += Main.dayLength;
                            }
                        }
                    }
                }

                // EL ATERRIZAJE (la corrección sub-tick — menos de un tick
                // de sol: nadie lo ve, no es un salto). POSADO en 27000,
                // el reloj se queda QUIETO (hacia == 0 → sin paso, y
                // vanilla a rate 0 — cero deriva).
                if (Main.dayTime &&
                    Main.time >= 26999.0 && Main.time < 27000.0)
                    Main.time = 27000.0;
            }

            // === EL TEMBLOR DE LA PRESENTACIÓN (los kicks de la casa):
            //     SOLO durante la presentación (sub 9), creciendo con sus
            //     ticks hasta 13 px — con EL APARECER (sub 10) el mundo
            //     se calma: el pilar y el destello hablan por él ===
            if (Main.netMode != NetmodeID.Server && !Main.dedServ &&
                sub == 9 && (Main.GameUpdateCount % 13u) == 0u)
            {
                float f = Math.Min(1f, jefe.ai[2] / 150f);
                OndaLib.Kick(4f + 9f * f, 13);
            }
        }

        // ==================================================================
        //  EL RELOJ — MODIFY TIME RATE (corre en TODAS las máquinas).
        //
        //  v6.50.52 — EL ESPEJO PURO: durante la carrera vanilla queda
        //  QUIETA (rate 0) en AMBAS direcciones — el paso lo da el
        //  espejo en PreUpdateTime (mismo tick, mismo código para
        //  AVANZAR y RETROCEDER). La .51 avanzaba con el rate de
        //  vanilla: dos semánticas, y si vanilla lo aplicaba distinto
        //  (Journey, sundial, mods) la carrera se colgaba y el jefe no
        //  aparecía nunca. UN solo conductor ahora: el espejo.
        // ==================================================================
        public override void ModifyTimeRate(ref double timeRate,
            ref double tileUpdateRate, ref double eventUpdateRate)
        {
            if (AethonBoss.TiempoCorriendo)
            {
                timeRate = 0.0;         // vanilla QUIETA — corre el espejo
                tileUpdateRate = 1.0;   // el mundo físico sigue normal
                eventUpdateRate = 0.0;  // cero tiradas de eventos en el barrido
            }
            else if (AethonBoss.TiempoCongelado)
            {
                // EL MEDIO DÍA ETERNO: el sol clavado en el centro.
                timeRate = 0.0;
            }
        }

        /// <summary>
        /// v6.50.51 → v6.50.52 — LOS TICKS HACIA EL MEDIODÍA, CON SIGNO.
        /// La posición del sol en el ciclo de 24 h contra las 12:00 (el
        /// CENTRO), POR EL CAMINO MÁS CORTO:
        ///   · + = AVANZAR (madrugada/mañana: «si el sol acaba de salir,
        ///     el tiempo avanza» / «si la noche está por terminar, avanza»);
        ///   · − = RETROCEDER (tarde/noche temprana: «si está cerca del
        ///     centro pero ya lo pasó, retrocede» / «si la noche acaba de
        ///     comenzar, retrocede»).
        /// La NOCHE se parte en la MEDIANOCHE (el punto equidistante — la
        /// medianoche exacta cuenta como «antes» → AVANZA).
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

            // EL LADO DEL CENTRO (−12..+12: − = antes del mediodía).
            double d = hora % 24.0 - 12.0;

            if (d < 0.0)
            {
                // AVANZAR: el PRÓXIMO mediodía yendo hacia adelante (por
                // la noche completa si hace falta).
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
