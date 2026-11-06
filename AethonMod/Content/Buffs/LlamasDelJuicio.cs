using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Dusts;
using AethonMod.Content.Globals;

namespace AethonMod.Content.Buffs
{
    /// <summary>
    /// LlamasDelJuicio — v6.104 — LAS LLAMAS SAGRADAS DEL AURA DE
    /// JUICIO (el debuff del golpe de quemadura del original — las
    /// “Holy Flames” del Warbanner, hechas casa).
    ///
    /// LA LETRA DEL USUARIO: «copia exactamente el codigo de Warbanner
    /// of the Righteous y sus textura, efectos vfx tecnica todo».
    ///
    /// EL PAQUETE (cada golpe de la quemadura lo refresca 30 ticks):
    ///  · EL DOT: lifeRegen −12 (la convención vanilla de OnFire —
    ///    MP-seguro sin paquetes: el servidor lo resuelve solo).
    ///  · LAS LLAMAS DORADAS: DustID.Torch en dorado/naranja subiendo
    ///    desde dentro del cuerpo + la luz cálida del fuego vivo.
    ///  · El MOTOR DEL FUEGO (ritmo, stacks, intensidad, repel y
    ///    chispas) NO vive aquí: vive en AuraJuicioGlobalNPC — este
    ///    buff es la MECHA que prende el paquete cuando el impío YA
    ///    SALIÓ del campo (sus últimos 30 ticks siguen doliendo).
    /// </summary>
    public class LlamasDelJuicio : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;                       // es un debuff (las coronas no lo quitan)
            Main.buffNoTimeDisplay[Type] = false;           // la cuenta atrás ES la información
            Main.buffNoSave[Type] = true;
            BuffID.Sets.LongerExpertDebuff[Type] = true;    // los expertos arden más largo
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            // EL DOT DE LAS LLAMAS — la convención vanilla: cada punto
            // de lifeRegen negativo = 1 HP/medio-segundo (−12 = 24 HP/s
            // brutos antes de la defensa del juego — el “lifeRegen −12”
            // del original).
            npc.lifeRegen -= 12;

            // ==============================================================
            //  EL TEATRO (solo cliente): las llamas doradas subiendo +
            //  la luz cálida del impío ardiendo.
            // ==============================================================
            if (Main.netMode != NetmodeID.Server && !Main.dedServ && !Main.gameMenu)
            {
                if (Main.rand.NextBool(2))
                {
                    // LA LLAMA — Torch en DORADO/NARANJA (la juventud de
                    // la llama es dorada clara; la vieja, naranja rojiza).
                    Color llama = Main.rand.NextBool(3)
                        ? new Color(255, 235, 160)     // la llama joven: blanco-dorado
                        : (Main.rand.NextBool(2)
                            ? new Color(255, 200, 80)   // la llama viva: dorada
                            : new Color(255, 140, 40)); // la llama vieja: naranja
                    Dust d = Dust.NewDustPerfect(
                        npc.Center + new Vector2(
                            Main.rand.NextFloat(-(npc.width * 0.5f), npc.width * 0.5f),
                            Main.rand.NextFloat(-(npc.height * 0.5f), npc.height * 0.5f)),
                        DustID.Torch,
                        new Vector2(Main.rand.NextFloat(-0.4f, 0.4f),
                            -Main.rand.NextFloat(0.9f, 2.0f)),
                        0, llama, Main.rand.NextFloat(0.8f, 1.3f));
                    d.noGravity = true;
                    d.fadeIn = 0.9f;
                }

                // LA LUZ CÁLIDA — el fuego del juicio ilumina (más
                // denso cuando la marca del motor sigue viva).
                AuraJuicioGlobalNPC marca = npc.GetGlobalNPC<global::AethonMod.Content.Globals.AuraJuicioGlobalNPC>();
                float calor = marca != null && marca.Marcada
                    ? 0.6f + 0.2f * marca.IntensidadQuemadura
                    : 0.45f;
                Lighting.AddLight(npc.Center,
                    new Vector3(1f, 0.72f, 0.25f) * calor);
            }
        }
    }
}
