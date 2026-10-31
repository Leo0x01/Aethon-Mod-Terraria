using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    // ====================================================================
    //  v6.56.9 — TIEMPOSLIB — EL HITSTOP DE LA CASA (la librería nueva nº1
    //  de la auditoría §5.1: «congelar o ralentizar EL JUEGO unos ticks en
    //  momentos clave… 0 resultados de hitstop/timeScale en todo el mod»).
    //
    //  EL LENGUAJE DEL IMPACTO MODERNO: cuando el golpe definitivo conecta,
    //  el mundo CONTIENE LA RESPIRACIÓN — unos pocos ticks con los NPC
    //  clavados en el aire venden la fuerza mejor que cualquier número de
    //  daño. Consumidor de nacimiento: LA IGNICIÓN de las copias del RC2
    //  (el instante en que el conducto se enciende).
    //
    //  EL DISEÑO SEGURO (la advertencia de la auditoría: «en MP el tiempo
    //  de juego es compartido — congelar IA de forma determinista, no
    //  Main.time a lo loco»):
    //    · SOLO EN UN JUGADOR (netmode 0): en multijugador la llamada es
    //      un no-op documentado — ningún desync posible.
    //    · SOLO LOS NPC (PreAI → false): los proyectiles del jugador
    //      siguen vivos (el haz sigue barriendo congelado el mundo — el
    //      momento «el mundo observa»), el jugador sigue dueño de su
    //      input, los golpes siguen conectando (la colisión no es AI).
    //    · COLA PRIORIZADA por duración: un Congelar(8) no es pisoteado
    //      por un Congelar(3) posterior — manda el más largo.
    //    · CERO Main.time, cero pausa del motor: la congelación es de la
    //      IA de los NPC, determinista y reversible.
    // ====================================================================
    public static class TiemposLib
    {
        // LA COLA — el tiempo restante de congelación (ticks).
        private static int _congelado;

        /// <summary>¿Está el mundo congelado AHORA? (los GlobalNPC lo
        /// consultan cada tick — jamás lo cachees fuera de un frame).</summary>
        public static bool Congelado => _congelado > 0 && Main.netMode == NetmodeID.SinglePlayer;

        /// <summary>LOS TICKS RESTANTES de la congelación activa (para
        /// HUD/debug — la casa lo muestra en el F8).</summary>
        public static int CongeladoRestante => Congelado ? _congelado : 0;

        /// <summary>
        /// EL MUNDO CONTIENE LA RESPIRACIÓN: congela la IA de TODOS los
        /// NPC durante [ticks] (solo un jugador — en MP es un no-op
        /// documentado). Manda la petición MÁS LARGA de la ventana (la
        /// cola priorizada: un golpe mayor no se acorta por uno menor).
        /// </summary>
        public static void Congelar(int ticks)
        {
            if (Main.netMode != NetmodeID.SinglePlayer) return;
            if (ticks <= 0) return;
            if (ticks > _congelado)
                _congelado = ticks;
        }

        /// <summary>EL RELOJ INTERNO — lo llama TiemposSistema una vez por
        /// tick (NUNCA lo llames a mano).</summary>
        internal static void Tick()
        {
            if (_congelado > 0)
                _congelado--;
        }
    }

    /// <summary>EL SISTEMA DEL TIEMPO — un tick por frame, el orden de la
    /// casa: el reloj baja ANTES de que los NPC decidan.</summary>
    public class TiemposSistema : ModSystem
    {
        public override void PreUpdateWorld()
        {
            TiemposLib.Tick();
        }
    }

    /// <summary>EL CONGELADOR DE NPC — mientras TiemposLib.Congelado, la IA
    /// de cada NPC se salta (PreAI → false): quietos en el aire, la pose
    /// congelada del impacto. Los golpes siguen conectando (la colisión no
    /// es AI) y los proyectiles del jugador siguen volando.</summary>
    public class TiemposGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => false;

        public override bool PreAI(NPC npc)
        {
            return !TiemposLib.Congelado;
        }
    }
}
