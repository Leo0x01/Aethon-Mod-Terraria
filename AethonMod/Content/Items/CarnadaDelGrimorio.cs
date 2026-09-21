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
            // v6.50.1 — FIX (EL GATE QUE SOBREVIVIÓ): CanUseItem corre en
            // local, server Y clientes remotos — en el server Main.myPlayer
            // NO es el jugador que usa el ítem: devolvía false para
            // cualquier remoto y el server jamás corría UseItem → la furia
            // de la carnada no existía en MP para jugadores remotos (el fix
            // v6.50 del doble-gate limpió UseItem pero dejó este guard). El
            // clic-derecho (preparar) sigue auto-limitándose al local dentro
            // de UseItem.
            return true;
        }

        public override bool? UseItem(Player player)
        {
            // === CLICK DERECHO: PREPARAR (ciclar 1..11 — el 11 es EL JUICIO) ===
            // (local: el contador de prueba es un estado de esta máquina)
            if (player.altFunctionUse == 2)
            {
                if (Main.myPlayer != player.whoAmI) return null;
                OleadasPreparadas = OleadasPreparadas % 11 + 1;
                Main.NewText(Language.GetTextValue("Mods.AethonMod.Carnada.Preparadas", OleadasPreparadas),
                    new Color(198, 200, 206));
                return true;
            }

            // === CLICK IZQUIERDO: LA FURIA ===
            // v6.50 — EL DOBLE GATE MUERTO (hallazgo auditoría MP nº5): el
            // guard myPlayer bloqueaba al SERVER (que corre este UseItem
            // por el uso sincronizado del jugador remoto) y el guard de
            // netMode bloqueaba al cliente — en MP NADIE desataba la furia.
            // El servidor es quien debe correrla (las oleadas son suyas).
            if (Main.netMode == NetmodeID.MultiplayerClient) return null; // el servidor manda

            // las hambres reales del libro, si ya tenía (y eran más)
            var sp = player.GetModPlayer<Players.ShardPlayer>();
            int oleadas = OleadasPreparadas;
            if (sp != null && sp.MomentosHambre > oleadas)
                oleadas = sp.MomentosHambre;

            if (GrimorioFuriaSistema.Activo)
            {
                // v6.50.1 — FIX: en MP esta rama corre en el SERVER (el
                // uso sincronizado del remoto) — Main.NewText no llega a
                // ninguna pantalla: el aviso viaja al portador que la usó.
                EcoRed.AnunciarAlPortador(player, "Mods.AethonMod.Carnada.YaActivo",
                    new Color(178, 26, 38));
                return false;
            }
            if (!GrimorioFuriaSistema.MundoLibre())
            {
                EcoRed.AnunciarAlPortador(player, "Mods.AethonMod.Carnada.Ocupado",
                    new Color(255, 160, 90));
                return false;
            }

            GrimorioFuriaSistema.Provocar(player, oleadas);
            return true;
        }
    }
}
