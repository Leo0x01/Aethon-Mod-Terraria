using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Systems;

namespace AethonMod.Content.Items.Llamados
{
    // ======================================================================
    //  LA TINTA DEL COMETA (v6.50.126) — EL ACTIVADOR DE LA CAÍDA.
    //
    //  LA LETRA DEL USUARIO: «crea un item para activar el evento, este
    //  será un item de prueba, además como activador obligatorio para
    //  evitar que el evento del grimorio se pierda».
    //
    //  EL CUENTO: un frasco con la MISMA tinta negra y morada del cometa
    //  (la que dejó el cráter de la primera caída). Al romperlo contra
    //  el aire, el cielo RECUERDA: la estrella negra cruza otra vez —
    //  de día o de noche, con la caída de este mundo vivida o no (es un
    //  ítem DE PRUEBAS: el camino manual al cielo).
    //
    //  LAS REGLAS (la otra letra: «el evento se activará 1 vez por
    //  jugador siempre que este no lo haya activado antes» — los caminos
    //  AUTOMÁTICOS siguen esa ley):
    //  · La PRIMERA NOCHE dispara (v6.50.119/.125 — una vez por jugador
    //    por mundo; en otro mundo, otra caída).
    //  · EL PRIMER VECINO que no sea el Guía es el failsafe (v6.50.126:
    //    si la noche pasó de largo, el cometa llega con él — la caída
    //    NUNCA se pierde).
    //  · ESTA TINTA es el camino MANUAL: rompe horarios y silenciadores
    //    PERO JAMÁS DUPLICA (si un grimorio ya duerme para ti o un
    //    cometa vuela hacia ti, el cielo no insiste — te lo dice).
    //
    //  LA ENTREGA: vive en la Bolsa de la Cría (el kit de pruebas — el
    //  mod es de pruebas) y SIN receta.
    // ======================================================================
    /// <summary>
    /// TintaDelCometa — LA TINTA DEL COMETA (el activador de LA CAÍDA).
    ///
    /// Rompe el frasco y el cielo recuerda: dispara el cometa del
    /// Grimorio del Eterno para quien lo usa (item de pruebas — el
    /// camino manual; la primera noche y el primer vecino siguen
    /// siendo los caminos automáticos).
    /// </summary>
    public class TintaDelCometa : ModItem
    {
        /// <summary>El violeta del cometa (el tinte del aviso).</summary>
        private static readonly Color TinteCaida = new(150, 60, 220);

        public override string Texture => "AethonMod/Content/Items/Llamados/TintaDelCometa";

        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 26;
            Item.maxStack = 1;
            Item.consumable = false;              // reutilizable: es de pruebas
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 45;                    // lento a propósito: un cielo por uso
            Item.useAnimation = 45;
            Item.UseSound = SoundID.Item107;      // el vidrio que se rompe
            Item.rare = ItemRarityID.Quest;       // la familia del kit de pruebas
            Item.value = Item.buyPrice(gold: 1);
        }

        public override bool? UseItem(Player player)
        {
            // SP / HOST: la AUTORIDAD está aquí mismo.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                AtenderPedido(player);
                return true;
            }

            // MP CLIENTE: el cielo nace del server (la mano por el cable —
            // la misma lección de la toma de la .117: sin paquete, cometa
            // fantasma).
            EcoRed.PedirForzarCaida();
            return true;
        }

        /// <summary>LA DECISIÓN DE LA AUTORIDAD (SP/host directo; MP por
        /// EcoRed.MsgForzarCaida): dispara el cometa — o explica por qué
        /// el cielo no insiste (el aviso privado de la tinta).</summary>
        public static void AtenderPedido(Player player)
        {
            try
            {
                if (Main.netMode == NetmodeID.MultiplayerClient) return;
                if (player == null || !player.active) return;

                int resultado = LaCaidaSistema.ForzarCaida(player);
                switch (resultado)
                {
                    case 0:
                        // EL CIELO RECUERDA: el aviso del cometa lo canta
                        // DispararCometa (privado, al portador).
                        break;
                    case 1:
                        EcoRed.AnunciarAlPortador(player,
                            "Mods.AethonMod.Caida.TintaYaEspera", TinteCaida);
                        break;
                    case 2:
                        EcoRed.AnunciarAlPortador(player,
                            "Mods.AethonMod.Caida.TintaEnVuelo", TinteCaida);
                        break;
                }
            }
            catch { }
        }

        // v6.50.126 — SIN RECETA: la tinta viaja en la Bolsa de la Cría
        // (el kit de pruebas del mod — como el Grimorio Menor).
    }
}
