using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Buffs.Investigaciones
{
    // ======================================================================
    //  v6.124 — LOS BUFFS DE EL PRINCIPIO Y EL FIN (las copias locales de
    //  los debuffs de The Beginning and The End de Calamity Entropy — la
    //  ficha del wiki oficial):
    //    · LA MARCA DEL PRINCIPIO (冥煌烙印, la daga de ORO, 12 s, hasta 8
    //      capas): la marca de la luz inferior que ARDE en el impío.
    //    · LA MIRADA DEL FIN (虚渊凝视, la daga AZUL, 12 s, hasta 8 capas):
    //      la mirada del abismo vacío que CONGELA la sangre.
    //    · LA SUPRESIÓN VITAL (生命压制, ambas dagas, 10 s, «has visto tu
    //      muerte»): la vida del impío se rinde.
    //    · LA SERVIDUMBRE (使役, el alma de luz inferior al JUGADOR, 3 s):
    //      +daño, +velocidad de uso y +crítico — el alma sirviente.
    //  (El wiki no detalla los números de los DoT — la casa les da una
    //  proporción serena del daño del arma; el PODER de verdad vive en
    //  las capas que las dagas cobran en almas.)
    // ======================================================================

    /// <summary>LA MARCA DEL PRINCIPIO: la marca dorada que arde (la daga
    /// de oro la graba, capa a capa, hasta 8).</summary>
    public class MarcaDelPrincipio : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.LongerExpertDebuff[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            // EL ARDOR DE LA MARCA: quema suave (la marca acumula, no mata).
            npc.lifeRegen -= 18;
            if (Main.rand.NextBool(4))
            {
                Dust d = Dust.NewDustPerfect(
                    npc.Center + new Vector2(
                        Main.rand.NextFloat(-(npc.width * 0.5f), npc.width * 0.5f),
                        Main.rand.NextFloat(-(npc.height * 0.5f), npc.height * 0.5f)),
                    DustID.Enchanted_Gold,
                    new Vector2(0f, -Main.rand.NextFloat(0.4f, 1.2f)),
                    200, new Color(255, 224, 150), Main.rand.NextFloat(0.6f, 1.0f));
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }
    }

    /// <summary>LA MIRADA DEL FIN: la mirada azul del abismo (la daga azul
    /// la clava, capa a capa, hasta 8).</summary>
    public class MiradaDelFin : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.LongerExpertDebuff[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            // EL FRÍO DEL ABISMO: la sangre se ralentiza.
            npc.lifeRegen -= 18;
            if (Main.rand.NextBool(4))
            {
                Dust d = Dust.NewDustPerfect(
                    npc.Center + new Vector2(
                        Main.rand.NextFloat(-(npc.width * 0.5f), npc.width * 0.5f),
                        Main.rand.NextFloat(-(npc.height * 0.5f), npc.height * 0.5f)),
                    DustID.Shadowflame,
                    new Vector2(0f, -Main.rand.NextFloat(0.2f, 0.8f)),
                    160, new Color(120, 170, 255), Main.rand.NextFloat(0.6f, 1.0f));
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }
    }

    /// <summary>LA SUPRESIÓN VITAL: «has visto tu muerte» (la ficha del
    /// original — la aplican AMBAS dagas, 10 s).</summary>
    public class SupresionVital : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.LongerExpertDebuff[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            // LA VIDA SE RINDE: la mirada de la propia muerte.
            npc.lifeRegen -= 25;
            if (Main.rand.NextBool(5))
            {
                Dust d = Dust.NewDustPerfect(
                    npc.Center + new Vector2(
                        Main.rand.NextFloat(-(npc.width * 0.5f), npc.width * 0.5f),
                        Main.rand.NextFloat(-(npc.height * 0.5f), npc.height * 0.5f)),
                    DustID.Venom,
                    new Vector2(0f, -Main.rand.NextFloat(0.3f, 1.0f)),
                    140, new Color(60, 40, 90), Main.rand.NextFloat(0.5f, 0.9f));
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }
    }

    /// <summary>LA SERVIDUMBRE: el regalo del alma de luz inferior al
    /// jugador (3 s: +daño, +velocidad de uso, +crítico — la ficha).</summary>
    public class Servidumbre : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // EL ALMA SIRVIENTE: la ficha — «+daño, +velocidad de ataque,
            // +crítico» mientras viva el regalo (3 s).
            player.GetDamage<GenericDamageClass>() += 0.10f;
            player.GetAttackSpeed<GenericDamageClass>() += 0.10f;
            player.GetCritChance<GenericDamageClass>() += 8;
            if (Main.rand.NextBool(6))
            {
                Dust d = Dust.NewDustPerfect(
                    player.Center + Main.rand.NextVector2Circular(14f, 20f),
                    DustID.Enchanted_Gold,
                    new Vector2(0f, -Main.rand.NextFloat(0.5f, 1.3f)),
                    210, new Color(255, 230, 160), Main.rand.NextFloat(0.5f, 0.9f));
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }
    }
}
