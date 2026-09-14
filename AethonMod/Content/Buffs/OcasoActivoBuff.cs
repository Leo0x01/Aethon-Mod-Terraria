using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.Buffs
{
    /// <summary>
    /// OcasoActivoBuff — v6.27 — EL INDICADOR DEL MODO OCASO.
    ///
    /// El estado real vive en `OcasoPlayer` (OcasoTime); este buff es el
    /// indicador de la interfaz con su cuenta atrás de 8 s. Se refresca
    /// cada tick desde `OcasoPlayer.PreUpdate` y no se guarda entre
    /// sesiones (patrón de los buffs de minion de la casa).
    /// </summary>
    public class OcasoActivoBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = false;   // la cuenta atrás de 8 s ES la información
            Main.buffNoSave[Type] = true;
        }
    }
}
