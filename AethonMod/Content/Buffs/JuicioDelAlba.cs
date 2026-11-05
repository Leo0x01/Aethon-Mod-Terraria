using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Buffs
{
    /// <summary>
    /// JuicioDelAlba — v6.98 — LA LLAMA DEL ALBA.
    ///
    /// EL DEBUFF DEL ESTANDARTE DEL JUICIO (el accesorio-aura de la
    /// Casa Sagrada): la luz dorada arde contra los impíos que se
    /// acercan al portador. La llama dorada con el núcleo blanco
    /// (ícono forjado con LA SUPERFORJA — la cadena completa con QA).
    ///
    /// El DoT vive en <see cref="Update(NPC, ref int)"/> con
    /// `lifeRegen` negativo (el patrón vanilla de OnFire — MP-seguro
    /// sin paquetes) + las brasas doradas que suben (el polvo de la
    /// casa). 40 HP/s: el juicio pica, pero el daño GORDO lo pone el
    /// aura misma (la proximidad manda).
    /// </summary>
    public class JuicioDelAlba : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;                       // es un debuff (las coronas no lo quitan)
            Main.buffNoTimeDisplay[Type] = false;           // la cuenta atrás ES la información
            Main.buffNoSave[Type] = true;
            BuffID.Sets.LongerExpertDebuff[Type] = true;    // los expertos queman más largo
        }

        /// <summary>EL DOT: 40 HP/s (la convención vanilla: -2 por
        /// punto de vida) + las brasas doradas que ascienden.</summary>
        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.lifeRegen -= 80;

            // LAS BRASAS DORADAS: la luz del alba sube desde dentro.
            if (Main.rand.NextBool(4))
            {
                Dust d = Dust.NewDustPerfect(
                    npc.Center + new Vector2(
                        Main.rand.NextFloat(-(npc.width * 0.5f), npc.width * 0.5f),
                        Main.rand.NextFloat(-(npc.height * 0.5f), npc.height * 0.5f)),
                    DustID.Enchanted_Gold,
                    new Vector2(0f, -Main.rand.NextFloat(0.3f, 0.9f)),
                    130, new Color(255, 208, 96), 0.7f);
                d.noGravity = true;
                d.fadeIn = 0.4f;
            }
        }
    }
}
