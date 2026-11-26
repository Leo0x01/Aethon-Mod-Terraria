using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Buffs
{
    /// <summary>RecuperacionProyectada — v6.177 — EL ENFRIAMIENTO DE LA
    /// PROYECCIÓN (el plano del meme): 20 s de «tus emociones se están
    /// recargando» — el debuff que cuenta el tiempo hasta poder
    /// proyectarte otra vez.</summary>
    public class RecuperacionProyectada : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
            Terraria.ID.BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }
    }
}
