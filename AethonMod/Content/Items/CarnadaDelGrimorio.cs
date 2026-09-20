using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using AethonMod.Content.Systems;

namespace AethonMod.Content.Items
{
    /// <summary>
    /// La Carnada del Grimorio (ÍTEM DE PRUEBA) — provoca LAS OLEADAS DEL
    /// HAMBRE sin esperar a que el libro pase hambre.
    ///
    /// · CLICK IZQUIERDO: la furia COMIENZA con el número de oleadas
    ///   preparado (o las que el libro lleve acumuladas de hambre real,
    ///   si son más).
    /// · CLICK DERECHO: prepara el número de oleadas del próximo uso
    ///   (cicla 1→2→…→10→11(LA ESPECIAL: los 7 guardianes ×15)→1) — la
    ///   vía rápida para ver las 10 oleadas, el aura que se pudre, EL
    ///   JUICIO y los jefes especiales.
    ///
    /// Salta la bandera EventoHambreGrimorio de la config a propósito:
    /// es LA herramienta de prueba del evento.
    /// </summary>
    public class CarnadaDelGrimorio : ModItem
    {
        /// <summary>Las oleadas preparadas para el próximo uso (1..11 — 11 = LA ESPECIAL).</summary>
        public static int OleadasPreparadas = 3;

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.maxStack = 1;
            Item.consumable = false;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.UseSound = SoundID.Item4;
            Item.rare = ItemRarityID.Quest;
            Item.value = Item.buyPrice(0, 0, 0, 0);
        }

        public override bool AltFunctionUse(Player player) => true; // clic derecho prepara

        public override bool CanUseItem(Player player)
        {
            return player.whoAmI == Main.myPlayer;
        }

        public override bool? UseItem(Player player)
        {
            if (Main.myPlayer != player.whoAmI) return null;

            // === CLICK DERECHO: PREPARAR (ciclar 1..11 — el 11 es EL JUICIO) ===
            if (player.altFunctionUse == 2)
            {
                OleadasPreparadas = OleadasPreparadas % 11 + 1;
                Main.NewText(Language.GetTextValue("Mods.AethonMod.Carnada.Preparadas", OleadasPreparadas),
                    new Color(198, 200, 206));
                return true;
            }

            // === CLICK IZQUIERDO: LA FURIA ===
            if (Main.netMode == NetmodeID.MultiplayerClient) return null; // el servidor manda

            // las hambres reales del libro, si ya tenía (y eran más)
            var sp = player.GetModPlayer<Players.ShardPlayer>();
            int oleadas = OleadasPreparadas;
            if (sp != null && sp.MomentosHambre > oleadas)
                oleadas = sp.MomentosHambre;

            if (GrimorioFuriaSistema.Activo)
            {
                Main.NewText(Language.GetTextValue("Mods.AethonMod.Carnada.YaActivo"),
                    new Color(178, 26, 38));
                return false;
            }
            if (!GrimorioFuriaSistema.MundoLibre())
            {
                Main.NewText(Language.GetTextValue("Mods.AethonMod.Carnada.Ocupado"),
                    new Color(255, 160, 90));
                return false;
            }

            GrimorioFuriaSistema.Provocar(player, oleadas);
            return true;
        }
    }
}
