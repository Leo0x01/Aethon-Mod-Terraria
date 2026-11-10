using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Dusts;

namespace AethonMod.Content.Buffs
{
    /// <summary>
    /// AuraVacioBuff — v6.122 — EL AURA DEL VACÍO (el daño de la Forma
    /// Eterna, hecho buff visible).
    ///
    /// LA LETRA DEL USUARIO: «el daño que causa la forma eterna debe ser
    /// el mismo que el 0.1% de la suma del daño de invocacion final y el
    /// daño magico final del grimorio del eterno, o sea es el daño que
    /// tiene el grimorio despues de hacer el calculo de todos los buff,
    /// este daño causado por la forma Eterna se debe ver reflejado como
    /// un buff y como el daño viene del aura del vacio, el buff se
    /// llamara Aura del Vacio».
    ///
    /// EL ESPEJO DEL AURA: mientras el campo de la Forma Eterna marque a
    /// un impío, el portador luce ESTE buff — su ventana cuenta EXACTO
    /// el daño que el vacío está cobrando (el 0,1 % de la suma de los
    /// DOS daños finales del Grimorio del Eterno, ya con TODOS los buffs
    /// aplicados). Es un buff de INFORMACIÓN: cero stats, puro espejo —
    /// el cálculo vive en AuraJuicio2Ancla.DanoFormaEterna (la única
    /// fuente de verdad) y el valor llega aquí en
    /// <see cref="UltimoDano"/> cada tick que el aura trabaja.
    /// </summary>
    public class AuraVacioBuff : ModBuff
    {
        /// <summary>
        /// El daño por segundo que el aura está cobrando AHORA (lo escribe
        /// AuraJuicio2Ancla cada tick que marca a un impío — la única
        /// fuente de verdad del cálculo).
        /// </summary>
        public static int UltimoDano;

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;               // es un BUFF (el espejo del aura)
            Main.buffNoTimeDisplay[Type] = true;     // se refresca cada tick: sin cuenta ruidosa
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // LA VIDA DEL BUFF — el teatro mínimo del aura: una mota de
            // vacío invertido (núcleo negro + halo violeta) cada tanto,
            // la misma firma de la quemadura pero SERENA (es el aura del
            // dueño, no el fuego del impío).
            if (Main.netMode != NetmodeID.Server && !Main.dedServ && !Main.gameMenu)
            {
                if (Main.rand.NextBool(5))
                {
                    Dust d = Dust.NewDustPerfect(
                        player.Center + new Vector2(
                            Main.rand.NextFloat(-player.width * 0.55f, player.width * 0.55f),
                            Main.rand.NextFloat(-player.height * 0.55f, player.height * 0.55f)),
                        ModContent.DustType<PolvoVacioInvertido>(),
                        new Vector2(0f, -Main.rand.NextFloat(0.3f, 0.8f)),
                        0, new Color(150, 84, 235), Main.rand.NextFloat(0.5f, 0.8f));
                    d.noGravity = true;
                }
            }
        }

        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            // LA VENTANA CUENTA EL DAÑO EXACTO (la letra: «este daño
            // causado por la forma Eterna se debe ver reflejado como un
            // buff»): el número vive en la única fuente de verdad.
            tip = string.Format(
                Terraria.Localization.Language.GetTextValue(
                    "Mods.AethonMod.Buffs.AuraVacioBuff.Description"),
                UltimoDano);
        }
    }
}
