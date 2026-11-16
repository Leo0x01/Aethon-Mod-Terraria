using Terraria;
using Terraria.ModLoader;
using Terraria.GameContent;
using Terraria.ID;
using Microsoft.Xna.Framework;
using System;

namespace AethonMod.Content.Buffs
{
    /// <summary>
    /// IraSerExteriorRecargaBuff — v6.145 — EL CONTADOR TIPO BUFF de la
    /// Ira de un Ser Exterior (la letra: «debe estar solo disponible
    /// cada 10 minutos con un contador tipo buff para el haz»): mientras
    /// el reloj de los 10 minutos corre, el portador lleva ESTE buff —
    /// su descripción ES el contador vivo (mm:ss hasta que la Ira vuelva
    /// a estar lista). Cuando el reloj llega a cero, el buff se retira.
    /// </summary>
    public class IraSerExteriorRecargaBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;          // no es castigo: es un reloj
            Main.buffNoTimeDisplay[Type] = true; // el tiempo lo dibuja la desc
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }

        public override void ModifyBuffText(ref string buffName, ref string tip,
            ref int rare)
        {
            try
            {
                Player p = Main.LocalPlayer;
                ulong resta = Systems.AtaquesCombinadosSistema.IraRestante(p);
                if (resta <= 0ul)
                {
                    tip = "La Ira de un Ser Exterior está LISTA — Shift + click izquierdo para dispararla.";
                }
                else
                {
                    int s = (int)(resta / 60ul);
                    tip = $"La Ira de un Ser Exterior recarga: {s / 60:D2}:{s % 60:D2} — Shift + click izquierdo cuando esté lista.";
                }
            }
            catch { }
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // el buff vive mientras el reloj corra (lo administra el
            // propio sistema en su tick)
        }
    }
}
