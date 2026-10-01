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
    //  EL ESPEJO DE LA ENTRADA (v6.50.42 → v6.50.49).
    //
    //  v6.50.49 — LA ENTRADA DE LA EMPERATRIZ (la letra: «que sea
    //  exactamente como la emperatris de la luz»): LA CARRERA AL
    //  MEDIODÍA MURIÓ — el temblor, el reloj que corría, el cerrojo del
    //  mediodía eterno y sus kicks. La Emperatriz no toca el reloj del
    //  mundo: nace, se presenta y pelea. De todo el sistema de la
    //  llegada queda UN SOLO trabajo:
    //
    //  EL ESPEJO: reconstruye el sub-estado de la ENTRADA (la
    //  presentación) en TODAS las máquinas leyendo ai[1] del jefe (el
    //  servidor manda, cada cliente lo reconstruye) — ColaSierpeSky lo
    //  lee para encender EL CIELO (los destellos del sub 10) durante la
    //  presentación de la luz.
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
        //  DESPUÉS de la IA del jefe: el cielo obedece en el MISMO tick
        //  en que la IA decide)
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
                // la luz murió o se fue: el espejo se apaga.
                Entierro();
                return;
            }

            // EL ESPEJO: durante la presentación (ai[0] == 0) el sub
            // viaja en ai[1] (10 = SUB_PRESENTA) — ColaSierpeSky enciende
            // sus destellos con él. El reloj del mundo queda INTACTO
            // (v6.50.49: la Emperatriz no toca el reloj — ni carrera ni
            // congelado; los statics quedan en false para siempre).
            bool enEntrada = jefe.ai[0] == 0f;                 // EST_NACIENDO
            AethonBoss.SubLlegada = enEntrada ? (int)jefe.ai[1] : 0;
            AethonBoss.TiempoCorriendo = false;
            AethonBoss.TiempoCongelado = false;
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
