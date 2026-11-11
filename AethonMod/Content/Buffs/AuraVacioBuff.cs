using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Dusts;

namespace AethonMod.Content.Buffs
{
    /// <summary>
    /// AuraVacioBuff — v6.122 → v6.124 — EL AURA DEL VACÍO (el efecto
    /// ACTIVO de la Forma Eterna, hecho buff visible).
    ///
    /// LA LETRA DEL USUARIO (v6.124): «el buff del Aura del Vacio
    /// siempre debe estar aplicado mientras se use La Forma Eterna ya
    /// que es un efecto activo de La Forma Eterna y deberia decir algo
    /// como, el poder de vacio de un ser Exterior te rodea dañando a
    /// todo el que se acerque» + «para hacerlo simple con el calculo
    /// de daño, dejemos que el daño del aura del vacio sea 1 de daño en
    /// los bordes exteriores del efecto del aura, y a mas cerca del
    /// jugador este el enemigo mas daño hace y mas tick de daño hace
    /// por segundo, el daño va de 1 a 10 por cada tick de daño» + el
    /// bono del 10 % del maná sobre 200 (SIEMPRE sumado al máximo).
    ///
    /// EL ESPEJO DEL AURA: mientras el ancla viva (la Forma Eterna
    /// puesta), ESTE buff vive — su ventana cuenta EL RANGO EXACTO que
    /// el vacío cobra: 1 en el borde exterior → {DanoMaximoVivo} pegado
    /// al portador (10 + el bono de maná). Es un buff de INFORMACIÓN:
    /// cero stats, puro espejo — el cálculo vive en AuraJuicio2Ancla
    /// (BonusMana / DanoAuraMax — la única fuente de verdad) y el valor
    /// llega aquí en <see cref="DanoMaximoVivo"/> cada tick que el
    /// ancla vive.
    /// </summary>
    public class AuraVacioBuff : ModBuff
    {
        /// <summary>
        /// v6.124 — EL MÁXIMO que el aura está cobrando AHORA (lo escribe
        /// AuraJuicio2Ancla cada tick que vive — la única fuente de
        /// verdad del cálculo). Es el daño por tick del enemigo PEGADO
        /// al portador; el borde exterior siempre cobra 1.
        /// </summary>
        public static int DanoMaximoVivo;

        /// <summary>
        /// v6.124 — ¿el campo tiene a UNA presa viva dentro? (solo el
        /// texto corto del trabajo — el buff SIEMPRE vive con la Forma
        /// Eterna, presa o no).
        /// </summary>
        public static bool Trabajando;

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
            // v6.125 — SOLO LA LETRA DEL USUARIO (la letra de la sesión 7:
            // «en el buff de aura del vacio solo debe estar mi texto no
            // decir si hace daño o no, y eso de decir que el vacio esta
            // cobrando una presa esta bien, solo quita la explicacion de
            // daño y manten el resto»): LA VENTANA YA NO EXPLICA EL DAÑO —
            // el rango «1 → {0} por tick» MURIÓ del texto (el cálculo
            // sigue vivo en AuraJuicio2Ancla, la única fuente de verdad);
            // queda SOLO el texto del usuario + EL SELLO DEL TRABAJO (que
            // el vacío está cobrando una presa — «eso está bien», la letra).
            try
            {
                string texto = Terraria.Localization.Language.GetTextValue(
                    "Mods.AethonMod.Buffs.AuraVacioBuff.Description");
                if (Trabajando)
                {
                    string trabajando = Terraria.Localization.Language.GetTextValue(
                        "Mods.AethonMod.Buffs.AuraVacioBuff.Trabajando");
                    if (!string.IsNullOrEmpty(trabajando) && !trabajando.StartsWith("Mods."))
                        texto = texto + "\n" + trabajando;
                }
                tip = texto;
            }
            catch { }
        }
    }
}
