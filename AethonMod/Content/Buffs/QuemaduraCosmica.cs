using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Buffs
{
    /// <summary>
    /// QuemaduraCosmica — v6.30 — EL DEBUFF DE LOS AGUJEROS NEGROS.
    ///
    /// La petición del usuario: "con los agujeros negros debes crear un nuevo
    /// debuff, para eso toma la forma del debuff de quemadura y luego tiñe de
    /// negro y el debuff nuevo se llama, quemadura cósmica".
    ///
    /// ASÍ ES: la FORMA del fuego (la silueta de la llama de OnFire — ícono
    /// procedural de llama, 100% casa) pero TIÑIDA DE NEGRO con el corazón
    /// violeta-cósmico y motas de estrellas: el fuego que arde HACIA DENTRO,
    /// el horizonte de eventos lamiente. El DoT vive en
    /// <see cref="Update(NPC, ref int)"/> con `lifeRegen` negativo (el patrón
    /// vanilla de OnFire — MP-seguro sin paquetes) + el polvo de brasas
    /// NEGRAS-violetas.
    ///
    /// Lo aplican TODOS los agujeros negros de la casa (9 + los ascendidos +
    /// el aurora + el eclipse devorador).
    /// </summary>
    public class QuemaduraCosmica : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;                       // es un debuff (las coronas no lo quitan)
            Main.buffNoTimeDisplay[Type] = false;           // la cuenta atrás ES la información
            Main.buffNoSave[Type] = true;
            BuffID.Sets.LongerExpertDebuff[Type] = true;    // los expertos queman más largo
            // (El nombre y la descripción viven en el hjson de localización — la casa.)
        }

        /// <summary>EL DOT: regeneración negativa por tick (el patrón vanilla
        /// de los debuff de daño — MP-seguro: el server lo corre solo) + las
        /// brasas negras-violetas del fuego cósmico (más fuerte que OnFire:
        /// el fuego que arde hacia dentro duele DOBLE).</summary>
        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.lifeRegen -= 32;

            // LAS BRASAS NEGRAS: polvo violeta-negro que ASPIRA hacia dentro
            // (el fuego cósmico arde HACIA el centro, no hacia fuera).
            if (Main.rand.NextBool(4))
            {
                Dust d = Dust.NewDustPerfect(
                    npc.Center + new Vector2(
                        Main.rand.NextFloat(-(npc.width * 0.5f), npc.width * 0.5f),
                        Main.rand.NextFloat(-(npc.height * 0.5f), npc.height * 0.5f)),
                    DustID.PurpleTorch,
                    new Vector2(0f, -Main.rand.NextFloat(0.3f, 0.9f)),
                    120, new Color(60, 20, 110), 0.7f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }
    }
}
