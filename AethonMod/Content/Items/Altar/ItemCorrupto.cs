using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace AethonMod.Content.Items.Altar
{
    /// <summary>
    /// LA ESENCIA CORRUPTA (v6.50.128) — lo que queda de un item tras
    /// pasar por EL HAZ DEL ALTAR DE LA CAÍDA.
    ///
    /// v6.51.11 — LA COMIDA DE DOS BOCALES (la letra del usuario):
    /// «sobre las esencia corrupta estas si las consume el jugador
    /// aumentan su ataque siguen aumentando hasta un maximo de 100 % en
    /// esencia consumidas por el jugador independientemente de lo que
    /// consuma el grimorio, las esencias consumidas por el grimorio no
    /// cuentan para el aumento general de daño, solo cuentan para su
    /// despertar, cuando el jugador cumpla con el limite de esencias
    /// consumida y llegue al tope que es 100% de daño o sea que el
    /// jugador consuma 1000 esencias corruptas, entonces las esencias
    /// se convierten en monedas de oro, 1 esencia 1 de oro».
    ///
    /// EL CIRCUITO NUEVO:
    ///  · EL PORTADOR LA COME (clic, como cualquier consumible): +0,1 %
    ///    de daño en TODO lo suyo, PERMANENTE — tope +100 % con 1000
    ///    (ShardPlayer.EsenciasConsumidas). Al llegar al tope, cada
    ///    esencia comida se convierte en 1 MONEDA DE ORO.
    ///  · EL GRIMORIO LA DEVORA DEL SUELO: con el Eterno SELECCIONADO,
    ///    suelta la pila DELANTE del libro — come SOLO las que le faltan
    ///    del peldaño de despertar (contadores parciales: la .51.11) y
    ///    las demás se quedan en el suelo. Lo que come el libro SOLO
    ///    despierta: NO sube el daño de nadie.
    /// </summary>
    public class ItemCorrupto : ModItem
    {
        public override string Texture => "AethonMod/Content/Items/Altar/ItemCorrupto";

        public override void SetStaticDefaults()
        {
            // El DisplayName y el Tooltip viven en la localización
            // (Items.ItemCorrupto) — la regla de la casa.
        }

        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 22;
            Item.maxStack = 999;
            Item.rare = ItemRarityID.LightPurple;
            Item.value = 0;                 // no vale nada: vale lo que el Eterno pague
            // v6.51.11 — SE COME (la letra): un consumible de verdad.
            Item.consumable = true;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 12;
            Item.useAnimation = 12;
            Item.UseSound = SoundID.Item4;
            Item.autoReuse = true;          // mantener clic = comer la pila
        }

        /// <summary>El portador PUEDE comerla siempre (el festín es
        /// suyo — el grimorio come aparte, del suelo).</summary>
        public override bool CanUseItem(Player player) => true;

        /// <summary>EL FESTÍN DEL PORTADOR (la letra): +0,1 % de daño en
        /// TODO por esencia (tope +100 % con 1000 — ShardPlayer).
        /// v6.50 — EL PATRÓN DE LAS ESENCIAS (EsenciasJefes): en MP el
        /// cliente solo avisa (uso sincronizado); el server hace el
        /// trabajo y EcoRed.MsgDevoracion lleva el número a la máquina
        /// del portador (el daño se calcula allí).</summary>
        public override bool? UseItem(Player player)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return true;

            var sp = player.GetModPlayer<global::AethonMod.Content.Players.ShardPlayer>();
            if (sp == null) return false;

            if (sp.EsenciasConsumidas < global::AethonMod.Content.Players.ShardPlayer.EsenciasTope)
            {
                sp.EsenciasConsumidas++;

                // EL AVISO DEL TOPE (una sola vez — desde ahora es oro).
                if (sp.EsenciasConsumidas ==
                    global::AethonMod.Content.Players.ShardPlayer.EsenciasTope)
                {
                    global::AethonMod.Content.Systems.EcoRed.AnunciarAlPortador(player,
                        "Mods.AethonMod.Despertar.TopeEsencias",
                        new Color(255, 215, 120), 100f.ToString("0"));
                }
            }
            else
            {
                // AL TOPE: 1 esencia = 1 MONEDA DE ORO (la letra).
                player.QuickSpawnItem(player.GetSource_ItemUse(Item),
                    ItemID.GoldCoin, 1);
            }

            // LAS CHISPAS DEL TRAGO (la máquina con pantalla).
            if (!Main.dedServ)
            {
                for (int i = 0; i < 6; i++)
                {
                    Dust d = Dust.NewDustPerfect(player.Center, DustID.Shadowflame,
                        new Microsoft.Xna.Framework.Vector2(
                            Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-3f, 0.3f)),
                        140, new Color(150, 70, 220), 0.9f);
                    d.noGravity = true;
                }
            }

            // EL FESTÍN CAMINA (el daño del portador se calcula en SU
            // máquina: el número tiene que llegar).
            sp.SyncAhoraDevoracion();
            return true;
        }

        /// <summary>LA VENTANA NUEVA (la letra: «la informacion que
        /// aparece en la esencia corrupta en su ventana de informacion,
        /// debes cambiarla ya que ahora es diferente»): el festín ya NO
        /// es del jugador a secas — es del PORTADOR CON EL LIBRO: la
        /// LIGADURA (qué tanto del festín está activo AHORA: en la mano
        /// el 100 %, en la bolsa el 25 %, guardado NADA), el PROGRESO
        /// vivo (X/1000 comidas y su porcentaje REAL vestido por la
        /// ligadura) y al tope, EL ORO (cada esencia comida = 1 moneda).</summary>
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            try
            {
                Player portador = Main.LocalPlayer;
                var sp = portador.GetModPlayer
                    <global::AethonMod.Content.Players.ShardPlayer>();
                int n = sp == null ? 0 : sp.EsenciasConsumidas;
                int vistos = System.Math.Min(n,
                    global::AethonMod.Content.Players.ShardPlayer.EsenciasTope);

                // === LA LIGADURA (qué parte del festín vive AHORA) ===
                float factor = global::AethonMod.Content.Globals.AlmaEternoGlobalItem
                    .FactorDelLibro(portador);
                string claveLigadura = factor >= 0.99f
                    ? "Mods.AethonMod.Despertar.EsenciaLigadaMano"
                    : factor > 0.01f
                        ? "Mods.AethonMod.Despertar.EsenciaLigadaBolsa"
                        : "Mods.AethonMod.Despertar.EsenciaLigadaNada";
                tooltips.Add(new TooltipLine(Mod, "EsenciaLigadura",
                    Language.GetTextValue(claveLigadura)));

                // === EL PROGRESO VIVO (o el ORO del tope) ===
                if (n >= global::AethonMod.Content.Players.ShardPlayer.EsenciasTope)
                {
                    tooltips.Add(new TooltipLine(Mod, "EsenciaTope",
                        Language.GetTextValue("Mods.AethonMod.Despertar.EsenciaTopeTooltip")));
                }
                else
                {
                    // el porcentaje REAL: el festín vestido por la ligadura
                    // (con el libro en la mano, el +34,2 % entero; en la
                    // bolsa, su cuarta parte — la ventana cuenta la verdad)
                    string pct = (vistos * 0.1f).ToString("0.#",
                        System.Globalization.CultureInfo.InvariantCulture);
                    string pctReal = (vistos * 0.1f * factor).ToString("0.#",
                        System.Globalization.CultureInfo.InvariantCulture);
                    tooltips.Add(new TooltipLine(Mod, "EsenciaProgreso",
                        Language.GetTextValue("Mods.AethonMod.Despertar.EsenciaProgresoTooltip",
                            vistos,
                            global::AethonMod.Content.Players.ShardPlayer.EsenciasTope,
                            pct, pctReal)));
                }
            }
            catch { }
        }
    }
}
