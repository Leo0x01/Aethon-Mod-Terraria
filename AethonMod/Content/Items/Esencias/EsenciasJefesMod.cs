using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Globals;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Items.Esencias
{
    // ======================================================================
    //  v6.49 — LAS ESENCIAS DE LOS JEFES DEL MOD (la letra del usuario:
    //  "cada jefe de mod debe tener su esencia que de 1 nivel completo
    //  al grimorio, pero para evitar el farmeo de niveles, solo podrán
    //  dar hasta 10 esencias por mundo de juego cada jefe; los jefes de
    //  oleadas no tienen límite").
    //
    //  QUÉ HACEN: exactamente lo mismo que las almas de los guardianes
    //  de oleada — UN NIVEL COMPLETO al Grimorio del Eterno (heredan la
    //  maquinaria de EsenciaDeJefeItem). La diferencia es el LÍMITE: el
    //  guardián de oleada paga infinito (la dificultad escala con cada
    //  oleada), el JEFE DEL MOD paga 10 por MUNDO y ni una más
    //  (EsenciasModSistema cuenta, persiste en el TagCompound del
    //  mundo y calla el cofre cuando se seca).
    //
    //  v6.50.61 — LA PURGA DE NPC: los cuatro guardianes murieron con
    //  sus jefes — SOLO queda AETHON, el grimorio mismo (la letra: «el
    //  lore cambia: Aethon ES el propio grimorio y el jefe final, la
    //  prueba del propio grimorio»). Su alma es la última página del
    //  cuento: la prueba superada.
    //
    //  NOTA: el índice de Aethon NO se mueve (sigue siendo el 4º slot
    //  del contador del mundo) — los mundos viejos conservan su cuenta
    //  exacta de almas de Aethon ya pagadas.
    // ======================================================================

    /// <summary>EL MAPA jefe del mod → su esencia (lo usa EsenciasModSistema al soltar).</summary>
    public static class EsenciasJefesModMapa
    {
        /// <summary>El ítem de esencia del jefe (0 si no es Aethon).</summary>
        public static int DeNPC(int npcType)
        {
            if (npcType == ModContent.NPCType<Content.NPCs.AethonBoss>())
                return ModContent.ItemType<EsenciaDeAethon>();
            return 0;
        }

        /// <summary>El índice de conteo del jefe (para el límite de 10 por mundo).</summary>
        public static int IndiceDe(int npcType)
        {
            if (npcType == ModContent.NPCType<Content.NPCs.AethonBoss>()) return 4;
            return -1;
        }
    }

    /// <summary>
    /// AETHON: el alma del Grimorio Eterno — la prueba superada, el
    /// libro reconociendo a quien lo cargó hasta el final.
    /// </summary>
    public class EsenciaDeAethon : EsenciaDeJefeItem
    {
        protected override string ClaveJefe => "AethonBoss";

        // EL PRECIO DE LA LUZ: Aethon ya entrega LA FORMA ASCENDIDA al
        // morir — su esencia es el postre del mismo festín (no la
        // reemplaza).
    }
}
