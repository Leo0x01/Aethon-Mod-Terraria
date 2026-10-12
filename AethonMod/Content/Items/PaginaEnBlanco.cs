using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using AethonMod.Content.Systems;

namespace AethonMod.Content.Items
{
    /// <summary>
    /// PÁGINA EN BLANCO (ÍTEM DE PRUEBA) — v6.50.112.
    ///
    /// La letra del usuario: «necesito un item que reinicie el contador
    /// de los jefes para verificar como devora los jefes, este iten no
    /// debe ser consumible».
    ///
    /// · EL CONTADOR: LA PRIMERA CENA de la v6.50.110 marca cada tipo de
    ///   jefe en ShardPlayer.JefesDevorados («que esto suceda la primera
    ///   vez que se mate un jefe y solo la primera vez») — ESTE ítem
    ///   BORRA esa lista: la página donde el grimorio escribió sus
    ///   festines queda EN BLANCO y cada jefe vuelve a estrenar su cena
    ///   completa (el jefe se detiene a 100, los sellos lo llevan al
    ///   centro, LA SOMBRA DE LA PÁGINA lo devora, el loot cae igual).
    /// · NO CONSUMIBLE (la letra): pila 1, no se gasta — el probador de
    ///   la devoración, las veces que haga falta.
    /// · Sin receta: llega con la Bolsa Probador (el kit de pruebas).
    /// </summary>
    public class PaginaEnBlanco : ModItem
    {
        public override string Texture => "AethonMod/Content/Items/PaginaEnBlanco";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.maxStack = 1;
            // NO CONSUMIBLE (la letra): cada uso borra la memoria y el
            // ítem QUEDA en la mano — herramienta de pruebas reutilizable.
            Item.consumable = false;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 15;
            Item.useAnimation = 15;
            Item.UseSound = SoundID.Item122;
            Item.rare = ItemRarityID.Quest;
            Item.value = Item.buyPrice(0, 0, 0, 0);
        }

        public override bool CanUseItem(Player player)
        {
            // la lección de la Carnada: CanUseItem corre en local, server
            // Y clientes remotos — NUNCA devolver false por guards de máquina.
            return true;
        }

        public override bool? UseItem(Player player)
        {
            // LA AUTORIDAD DE LA MEMORIA (SP / server): el cliente MP no
            // toca la lista — el gatillo de LA PRIMERA CENA corre en la
            // autoridad y lee SU copia del portador.
            if (Main.netMode == NetmodeID.MultiplayerClient) return null;

            // EL OLVIDO: la lista de jefes devorados queda VACÍA — cada
            // tipo de jefe vuelve a estrenar su primera cena.
            var sp = player.GetModPlayer<Players.ShardPlayer>();
            int olvidados = sp != null ? sp.JefesDevorados.Count : 0;
            if (sp != null)
                sp.JefesDevorados.Clear();

            // LA VOZ del olvido (en la pantalla del portador): cuántos
            // festines se borraron… o que no había ninguno escrito.
            var color = new Color(196, 138, 255);   // el violeta del filo
            if (Main.netMode == NetmodeID.Server)
            {
                if (olvidados > 0)
                    EcoRed.AnunciarAlPortador(player, "Mods.AethonMod.PaginaEnBlanco.Reset",
                        color, olvidados);
                else
                    EcoRed.AnunciarAlPortador(player, "Mods.AethonMod.PaginaEnBlanco.Nada",
                        color);
            }
            else
            {
                Main.NewText(Language.GetTextValue(
                    olvidados > 0 ? "Mods.AethonMod.PaginaEnBlanco.Reset"
                                  : "Mods.AethonMod.PaginaEnBlanco.Nada",
                    olvidados), color);
            }
            return true;
        }
    }
}
