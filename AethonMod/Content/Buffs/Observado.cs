using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Buffs
{
    /// <summary>
    /// OBSERVADO — v6.50.98 — EL DEBUFF DEL GRIMORIO SELLADO.
    /// La letra del usuario: «Aethon te está mirando...» — el debuff en
    /// sí NO hace daño: los stacks los lleva el sistema
    /// (<see cref="Systems.SistemaObservado"/> — quien cuenta los 5
    /// golpes del «Conocido» y le regala el crítico garantizado al
    /// FragmentoDeAethon). Lo que el debuff pone es la PRESENCIA: las
    /// chispas doradas alrededor del enemigo marcado (el oro de la
    /// casa: GoldFlame teñido — el GoldFlare del código del usuario no
    /// existe en esta tML, lección .96).
    ///
    /// El nombre y la descripción viven en el hjson (la casa); el
    /// DisplayName.SetDefault del borrador murió aquí.
    /// </summary>
    public class Observado : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;              // es un debuff (las coronas no lo quitan)
            Main.buffNoTimeDisplay[Type] = false;  // la cuenta atrás ES la información
            Main.buffNoSave[Type] = true;         // no se guarda en el NPC (los
                                                  // stacks mueren con el mundo —
                                                  // un debuff huérfano sería mentira)
        }

        /// <summary>LA PRESENCIA: el debuff no hace daño — solo las
        /// chispas doradas del que está siendo mirado.</summary>
        public override void Update(NPC npc, ref int buffIndex)
        {
            if (Main.rand.NextBool(5))
            {
                Dust dust = Dust.NewDustDirect(
                    npc.position,
                    npc.width,
                    npc.height,
                    DustID.GoldFlame,             // GoldFlare NO existe — la casa
                    0f, -0.5f,
                    100, new Color(255, 200, 100), 0.6f
                );
                dust.noGravity = true;
                dust.fadeIn = 0f;
            }
        }
    }
}
