using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.Buffs
{
    /// <summary>
    /// SobrecalentadoBuff — v6.27 — EL LOCKOUT DEL OCASO.
    ///
    /// El castigo de la trinidad (carga → burst → lockout): mientras
    /// vive, EL BASTÓN DEL OCASO no dispara. El estado real vive en
    /// `OcasoPlayer` (OverheatTime); este buff es el indicador con su
    /// cuenta atrás de 2 s.
    /// </summary>
    public class SobrecalentadoBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = false;   // la cuenta atrás de 2 s ES la información
            Main.buffNoSave[Type] = true;
            Main.debuff[Type] = true;               // es el castigo: pinta rojo
        }
    }
}
