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
    //  QUIÉNES: los CINCO (el Titán Hueco, el Guardián del Rift, la
    //  Arquera Estelar, el Primer Portador y Aethon — cada alma con el
    //  color de su jefe).
    //
    //  NOTA DE LA TIENDA: el Testigo NO vende estas — las de oleada sí
    //  (10 de platino tras la oleada 10). Las almas de los jefes del
    //  mod solo se GANAN matándolos: son derrotas, no monedas.
    // ======================================================================

    /// <summary>EL MAPA jefe del mod → su esencia (lo usa EsenciasModSistema al soltar).</summary>
    public static class EsenciasJefesModMapa
    {
        /// <summary>El ítem de esencia del jefe (0 si no es de los cinco).</summary>
        public static int DeNPC(int npcType)
        {
            if (npcType == ModContent.NPCType<Content.NPCs.HollowTitan>())
                return ModContent.ItemType<EsenciaDelTitanHueco>();
            if (npcType == ModContent.NPCType<Content.NPCs.RiftKeeper>())
                return ModContent.ItemType<EsenciaDelGuardianDelRift>();
            if (npcType == ModContent.NPCType<Content.NPCs.EchoArcher>())
                return ModContent.ItemType<EsenciaDeLaArqueraEstelar>();
            if (npcType == ModContent.NPCType<Content.NPCs.EchoBlade>())
                return ModContent.ItemType<EsenciaDelPrimerPortador>();
            if (npcType == ModContent.NPCType<Content.NPCs.AethonBoss>())
                return ModContent.ItemType<EsenciaDeAethon>();
            return 0;
        }

        /// <summary>El índice de conteo del jefe (para el límite de 10 por mundo).</summary>
        public static int IndiceDe(int npcType)
        {
            if (npcType == ModContent.NPCType<Content.NPCs.HollowTitan>()) return 0;
            if (npcType == ModContent.NPCType<Content.NPCs.RiftKeeper>()) return 1;
            if (npcType == ModContent.NPCType<Content.NPCs.EchoArcher>()) return 2;
            if (npcType == ModContent.NPCType<Content.NPCs.EchoBlade>()) return 3;
            if (npcType == ModContent.NPCType<Content.NPCs.AethonBoss>()) return 4;
            return -1;
        }
    }

    /// <summary>EL TITÁN HUECO: el alma del coloso de cristal del Sagrario.</summary>
    public class EsenciaDelTitanHueco : EsenciaDeJefeItem
    {
        protected override string ClaveJefe => "HollowTitan";
    }

    /// <summary>EL GUARDIÁN DEL RIFT: el alma del centinela del entre-mundos.</summary>
    public class EsenciaDelGuardianDelRift : EsenciaDeJefeItem
    {
        protected override string ClaveJefe => "RiftKeeper";
    }

    /// <summary>LA ARQUERA ESTELAR: el alma del eco que no soltaba el arco.</summary>
    public class EsenciaDeLaArqueraEstelar : EsenciaDeJefeItem
    {
        protected override string ClaveJefe => "EchoArcher";
    }

    /// <summary>EL PRIMER PORTADOR: el alma del eco que no soltaba la hoja.</summary>
    public class EsenciaDelPrimerPortador : EsenciaDeJefeItem
    {
        protected override string ClaveJefe => "EchoBlade";
    }

    /// <summary>AETHON: el alma de la Luz Primordial, la que te reconoció.</summary>
    public class EsenciaDeAethon : EsenciaDeJefeItem
    {
        protected override string ClaveJefe => "AethonBoss";

        // EL PRECIO DE LA LUZ: Aethon ya entrega LA FORMA ASCENDIDA al
        // morir — su esencia es el postre del mismo festín (no la
        // reemplaza).
    }
}
