using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Buffs
{
    /// <summary>
    /// RelicarioCdBuff — v6.85 — EL BUFF DE RECARGA del Relicario del
    /// Avispón (la investigación R1): el debuff de 45 s del original —
    /// visible en la barra, no se guarda, la enfermera no lo cura.
    /// </summary>
    public class RelicarioCdBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }
    }
}
