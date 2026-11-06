using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Dusts;

namespace AethonMod.Content.Buffs
{
    /// <summary>
    /// VacioExterior — v6.101 — EL VACÍO EXTERIOR (el fuego que PAGA).
    ///
    /// LA LETRA DEL USUARIO: «mientras un enemigo este quemandose con el
    /// debuff de quemadura del vacio exterior, el jugador tendran un buff
    /// de vacio exterior donde el mismo jugador se quema con un efecto de
    /// quemadura pero al jugador al ser el dueño del grimorio lo unico
    /// que hace es aumentar su ataque, defensa y disminuir el consumo de
    /// mana».
    ///
    /// EL ESPEJO DEL DUEÑO: el portador TAMBIÉN arde — las mismas llamas
    /// negras de violeta bordean su cuerpo (el vacío reconoce a quien lo
    /// empuña) — pero su fuego no quema: COBRA. Mientras un solo impío
    /// arda con la Quemadura del Vacío Exterior de su propiedad:
    ///
    ///  · +10% a TODO el ataque (el hambre afilada del grimorio).
    ///  · +10 de defensa (la piel del vacío protege a su dueño).
    ///  · −15% del consumo de maná (la tinta fluye más barata).
    ///
    /// El buff lo REFRESCA la propia quemadura cada tick que el enemigo
    /// arde (QuemaduraVacioExterior.Update) — se apaga solo cuando el
    /// último fuego se extingue.
    /// </summary>
    public class VacioExterior : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;              // es un BUFF (el pago al dueño)
            Main.buffNoTimeDisplay[Type] = true;    // se refresca cada tick: sin cuenta ruidosa
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // EL COBRO DEL VACÍO — mientras el fuego del enemigo arde:
            player.statDefense += 10;                                   // la piel del vacío
            player.GetDamage(Terraria.ModLoader.DamageClass.Generic) += 0.10f;  // el hambre afilada
            player.manaCost -= 0.15f;                                   // la tinta barata

            // EL FUEGO DEL DUEÑO — el portador también ARDE: las mismas
            // llamas negras de la quemadura (núcleo negro + halo violeta,
            // el polvo del vacío invertido de la casa) lamiendo su cuerpo.
            if (Main.netMode != NetmodeID.Server && !Main.dedServ && !Main.gameMenu)
            {
                if (Main.rand.NextBool(3))
                {
                    Color halo = Main.rand.NextBool(3)
                        ? new Color(224, 176, 255)
                        : new Color(150, 84, 235);
                    Dust d = Dust.NewDustPerfect(
                        player.Center + new Vector2(
                            Main.rand.NextFloat(-player.width * 0.6f, player.width * 0.6f),
                            Main.rand.NextFloat(-player.height * 0.5f, player.height * 0.5f)),
                        ModContent.DustType<PolvoVacioInvertido>(),
                        new Vector2(Main.rand.NextFloat(-0.3f, 0.3f),
                            -Main.rand.NextFloat(0.9f, 2f)),
                        0, halo, Main.rand.NextFloat(0.65f, 1f));
                    d.noGravity = true;
                }
                // LA LUZ DEL DUEÑO QUE ARDE.
                Lighting.AddLight(player.Center, new Vector3(0.5f, 0.3f, 0.85f) * 0.8f);
            }
        }
    }
}
