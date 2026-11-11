using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Dusts;
using AethonMod.Content.Globals;
using AethonMod.Content.Players;

namespace AethonMod.Content.Buffs
{
    /// <summary>
    /// QuemaduraVacioExterior — v6.101 — LA QUEMADURA DEL VACÍO EXTERIOR.
    ///
    /// LA LETRA DEL USUARIO: «un efecto de quemadura cuyo sprite de buff
    /// es el mismo que el de quemadura de terraria pero con llamas negras
    /// y mientras un enemigo este quemandose con el debuff de quemadura
    /// del vacio exterior, el jugador tendran un buff de vacio exterior
    /// donde el mismo jugador se quema con un efecto de quemadura pero
    /// al jugador al ser el dueño del grimorio lo unico que hace es
    /// aumentar su ataque, defensa y disminuir el consumo de mana».
    ///
    /// EL DEBUFF DEL FUTURO GRIMORIO (la investigación del estandarte):
    /// el mismo ícono de la quemadura de Terraria — la silueta envuelta
    /// en fuego — pero con LLAMAS NEGRAS de borde violeta. La quemadura
    /// vive aquí, NO en el arma que la aplica: cualquier fuente que
    /// aplique este buff (hoy el estandarte, mañana el grimorio en sus
    /// niveles altos) hereda TODO el paquete:
    ///
    ///  · EL DOT (la convención vanilla de OnFire: lifeRegen negativo,
    ///    MP-seguro sin paquetes) que escala con la INTENSIDAD que la
    ///    fuente escribe en <see cref="VacioGlobalNPC"/> (×1 lejos →
    ///    ×3 pegado, más la rampa del tiempo ardiendo).
    ///  · LAS LLAMAS NEGRAS: el polvo del vacío invertido de la casa
    ///    (núcleo negro + halo violeta) subiendo desde dentro — MÁS
    ///    denso cuanto más caliente arde el impío.
    ///  ///  · EL PAGO AL DUEÑO: mientras un enemigo arda con esta
    ///    quemadura, SU portador cobra el buff Vacío Exterior (el dueño
    ///    del grimorio también «arde»… y su fuego es puro provecho).
    /// </summary>
    public class QuemaduraVacioExterior : ModBuff
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
            VacioGlobalNPC vacio = npc.GetGlobalNPC<VacioGlobalNPC>();
            float intensidad = MathHelper.Clamp(vacio.IntensidadQuemadura, 1f, 4.5f);

            // ==============================================================
            //  v6.124 — LOS DOS FUEGOS (la letra nueva + la v6.123,
            //  ambas vivas):
            //
            //  (A) DENTRO DEL AURA — EL DAÑO POR CERCANÍA (la letra:
            //  «dejemos que el daño del aura del vacio sea 1 de daño en
            //  los bordes exteriores del efecto del aura, y a mas cerca
            //  del jugador este el enemigo mas daño hace y mas tick de
            //  daño hace por segundo, el daño va de 1 a 10 por cada tick
            //  de daño» + el bono del 10 % del maná sobre 200): el
            //  ancla (AuraJuicio2Ancla) marca DentroDelCampo = true y la
            //  cercanía vive en CercaniaLocal (0 borde → 1 pegado). EL
            //  GOLPE: D = 1 + (máx−1)·c cada I = 30−20·c ticks (2/s en
            //  el borde → 6/s pegado) — cobrado con la convención
            //  vanilla VERIFICADA (NPC.UpdateNPC, decompile REAL del
            //  tML 2026.09.3.1): el golpe de D sale cada
            //  120·D/|lifeRegen| ticks → lifeRegen = −120·D/I y
            //  lifeRegenExpectedLossPerSecond = D (el número que el
            //  combate enseña).
            //
            //  (B) FUERA DEL AURA — LA QUEMADURA QUE ARDE SOLA (la letra
            //  v6.123: «debe hacer 1% de daño de la vida maxima del
            //  enemigo por cada medio segundo» + «cuando aura del vacio
            //  deja de afectar al enemigo, la quemadura del vacio sobre
            //  el enemigo continua por 5 segundos mas haciendo daño»):
            //  N = 1% de la vida MÁXIMA por MEDIO SEGUNDO — lifeRegen =
            //  −4·N (un golpe de N cada 30 ticks) — la persistencia de
            //  ARDE_FUERA = 300 ticks (5 segundos exactos de fuego tras
            //  dejar el campo).
            // ==============================================================
            if (vacio.DentroDelCampo && vacio.DuenoVacio > 0 &&
                vacio.DuenoVacio <= Main.maxPlayers)
            {
                Player dueno = Main.player[vacio.DuenoVacio - 1];
                float c = MathHelper.Clamp(vacio.CercaniaLocal, 0f, 1f);
                int danoTick = global::AethonMod.Content.Projectiles.AuraJuicio2Ancla.DanoAuraPorCercania(dueno, c);
                int intervalo = (int)MathHelper.Clamp(
                    global::AethonMod.Content.Projectiles.AuraJuicio2Ancla.IntervaloAura(c), 5, 30);
                npc.lifeRegen -= 120 * danoTick / intervalo;
                if (npc.lifeRegenExpectedLossPerSecond < danoTick)
                    npc.lifeRegenExpectedLossPerSecond = danoTick;
            }
            else
            {
                int porMedioSegundo = Math.Max(1, (int)Math.Ceiling(npc.lifeMax * 0.01f));
                npc.lifeRegen -= porMedioSegundo * 4;
                if (npc.lifeRegenExpectedLossPerSecond < porMedioSegundo)
                    npc.lifeRegenExpectedLossPerSecond = porMedioSegundo;
            }

            // EL PAGO AL DUEÑO: mientras ESTE enemigo arda, el portador
            // de la quemadura cobra el Vacío Exterior (su propio «fuego»
            // solo le da poder — la letra del grimorio).
            if (vacio.DuenoVacio > 0 && vacio.DuenoVacio <= Main.maxPlayers)
            {
                Player dueno = Main.player[vacio.DuenoVacio - 1];
                if (dueno != null && dueno.active && !dueno.dead)
                    dueno.AddBuff(ModContent.BuffType<VacioExterior>(), 30, true);
            }

            // LAS LLAMAS NEGRAS — el polvo del vacío invertido de la casa:
            // NÚCLEO NEGRO (el vacío que se come la luz) con HALO VIOLETA,
            // subiendo desde dentro del cuerpo. La densidad crece con la
            // intensidad: cuanto más arde, MÁS llamas se ven.
            if (Main.netMode != NetmodeID.Server && !Main.dedServ && !Main.gameMenu)
            {
                int prob = (int)MathHelper.Clamp(7f - intensidad, 2f, 6f);
                if (Main.rand.NextBool(prob))
                {
                    Color halo = Main.rand.NextBool(3)
                        ? new Color(224, 176, 255)    // la llama joven: violeta claro
                        : new Color(150, 84, 235);    // la llama viva: violeta pleno
                    Dust d = Dust.NewDustPerfect(
                        npc.Center + new Vector2(
                            Main.rand.NextFloat(-(npc.width * 0.5f), npc.width * 0.5f),
                            Main.rand.NextFloat(-(npc.height * 0.5f), npc.height * 0.5f)),
                        ModContent.DustType<PolvoVacioInvertido>(),
                        new Vector2(Main.rand.NextFloat(-0.3f, 0.3f),
                            -Main.rand.NextFloat(0.8f, 1.8f)),
                        0, halo, Main.rand.NextFloat(0.7f, 1.1f));
                    d.noGravity = true;

                    // LA LUZ DE LA QUEMADURA: el violeta del vacío exterior.
                    Lighting.AddLight(npc.Center, new Vector3(0.58f, 0.34f, 0.95f)
                        * (0.5f + 0.3f * intensidad * 0.33f));
                }
            }
        }
    }
}
