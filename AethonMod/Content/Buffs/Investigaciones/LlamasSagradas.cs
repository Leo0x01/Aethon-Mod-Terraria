using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Buffs.Investigaciones
{
    /// <summary>
    /// LlamasSagradas — v6.124 — LAS LLAMAS SAGRADAS (la copia local del
    /// HOLY FLAMES de Calamity Mod que el Dazzling Stabber Staff de la
    /// investigación prende con sus dagas de FUEGO — la letra: «copia el
    /// arma Dazzling Stabber Staff de Calamity Mod y crea una version con
    /// su codigo, sprite y textura al 100% como investigacion»).
    ///
    /// EL EFECTO DEL ORIGINAL (Holy Flames: "Dissolving from holy light",
    /// 3 s al 100 % de probabilidad con las dagas de fuego): un daño en
    /// el tiempo SAGRADO — la vida del impío se DISUELVE bajo la luz.
    /// La casa lo implementa con el patrón vanilla de OnFire (lifeRegen
    /// negativo — MP-seguro) + el polvo dorado-sagrado de la
    /// disolución. El icono (la llama blanca-dorada) es de la superforja
    /// de la casa.
    /// </summary>
    public class LlamasSagradas : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.LongerExpertDebuff[Type] = true;
        }

        /// <summary>EL DOT SAGRADO: la disolución bajo la luz (el patrón
        /// vanilla de OnFire — el doble de fuerte: la luz disuelve
        /// RÁPIDO) + el polvo dorado que se disuelve hacia arriba.</summary>
        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.lifeRegen -= 40;

            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(
                    npc.Center + new Vector2(
                        Main.rand.NextFloat(-(npc.width * 0.5f), npc.width * 0.5f),
                        Main.rand.NextFloat(-(npc.height * 0.5f), npc.height * 0.5f)),
                    DustID.Enchanted_Gold,
                    new Vector2(0f, -Main.rand.NextFloat(0.5f, 1.4f)),
                    220, new Color(255, 240, 190), Main.rand.NextFloat(0.7f, 1.1f));
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }
    }
}
