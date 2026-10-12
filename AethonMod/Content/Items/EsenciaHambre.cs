using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using AethonMod.Content.Systems;

namespace AethonMod.Content.Items
{
    /// <summary>
    /// ESENCIA DE HAMBRE (ÍTEM DE PRUEBA) — v6.50.110.
    ///
    /// La letra del usuario: «crea un item que le de hambre al grimorio
    /// eterno para comprobar si funciona, que suba el hambre de 5 en 5
    /// y que no sea consumible».
    ///
    /// · Cada uso sube el hambre DEL GRIMORIO DEL ETERNO +5% (de 5 en 5:
    /// 0 → 5 → 10 → … → 100%) — el probador del reloj nervioso SIN
    ///   esperar la hora completa de la .103.
    /// · NO CONSUMIBLE (el patrón de la casa: el Verdugo de Niveles y la
    ///   Carnada): pila 1, no se gasta, se usa las veces que haga falta.
    /// · Sin receta: llega con la Bolsa Probador (el kit de pruebas).
    /// </summary>
    public class EsenciaHambre : ModItem
    {
        /// <summary>Lo que sube cada uso: +5% (la letra: «de 5 en 5»).</summary>
        public const float HambrePorUso = 0.05f;

        public override string Texture => "AethonMod/Content/Items/EsenciaHambre";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.maxStack = 1;
            // NO CONSUMIBLE (la letra): cada uso alimenta el apetito y el
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
            // v6.50.1 — la lección de la Carnada: CanUseItem corre en
            // local, server Y clientes remotos — NUNCA devolver false por
            // guards de máquina (el uso sincronizado del remoto lo corre
            // el server con SU copia del jugador).
            return true;
        }

        public override bool? UseItem(Player player)
        {
            // LA AUTORIDAD DEL APETITO (SP / server): el cliente MP no
            // toca el reloj — su barra le llega por EcoRed.SincronizarHambre
            if (Main.netMode == NetmodeID.MultiplayerClient) return null;

            // EL GRIMORIO DEL ETERNO (sólo él: la letra lo pide por su
            // nombre — el Nervioso de prueba corre su propio minuto)
            var et = Weapons.GrimoireEternal.EstadoEterno;
            et.Hambre = System.Math.Min(1f, et.Hambre + HambrePorUso);

            // LA VOZ del apetito nuevo (en la pantalla del portador)
            // v6.50.113 — CON DECIMALES (la letra: «tambien debes permitir
            // que visualizar el medidor de hambre tenga decimales»):
            // el (int) truncaba (47,3% → «47%»); ahora "0.0" — un
            // decimal SIEMPRE visible, a juego con la barra del tooltip.
            string pct = (et.Hambre * 100f).ToString("0.0");
            if (Main.netMode == NetmodeID.Server)
            {
                EcoRed.AnunciarAlPortador(player, "Mods.AethonMod.EsenciaHambre.Sube",
                    new Color(206, 108, 96), pct);
                EcoRed.SincronizarHambre(player);   // la barra, YA
            }
            else
            {
                Main.NewText(Language.GetTextValue("Mods.AethonMod.EsenciaHambre.Sube", pct),
                    new Color(206, 108, 96));
            }
            return true;
        }
    }
}
