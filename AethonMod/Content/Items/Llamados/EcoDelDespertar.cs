using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Items.Llamados
{
    // ======================================================================
    //  EL ECO DEL DESPERTAR (v6.51.0) — LA LLAVE DE PRUEBAS DE LA NUEVA
    //  PROGRESIÓN (la letra del usuario: «también crea un ítem no
    //  consumible que al usarlo suba el nivel de corrupción en 1 por cada
    //  uso del grimorio, que es la nueva mecánica de progresión» — el
    //  nombre cambió a último minuto: EL DESPERTAR).
    //
    //  NO CONSUMIBLE: cada uso sube EL DESPERTAR DEL GRIMORIO en 1 (con
    //  toda la ceremonia: la voz, el aviso dorado, el hito… y en el 20,
    //  LA PELÍCULA DE LA MASCOTA). Las ofrendas reales (esencias +
    //  dinero + el arma del peldaño) siguen siendo el camino canónico:
    //  este item es el PROBADOR de la mecánica (el patrón de la Tinta
    //  del Cometa — entregado en cada entrada al mundo de pruebas).
    // ======================================================================
    public class EcoDelDespertar : ModItem
    {
        public override string Texture => "AethonMod/Content/Items/Llamados/EcoDelDespertar";

        public override void SetStaticDefaults()
        {
            // El DisplayName y el tooltip viven en la localización.
            // v6.146 — el aviso del selector va por clave (la regla).
        }

        // v6.146 — EL SELECTOR DE POTENCIA (la letra: «que funciona
        // dando clicj derecho y con eso cambia la potencia, luego con
        // click iziquierdo se aplica, que funciona igual que el
        // seleccionador de oleadas del hambre»): el ECO recuerda SU
        // paso (1 o 10) por portador — el click derecho lo alterna,
        // el izquierdo lo aplica.
        // v6.147 — EL HOOK CORRECTO (la letra del usuario: «esto no
        // funciona»): CanRightClick/RightClick son el par de INVENTARIO
        // (click derecho sobre el SLOT — el gesto de los baúles), NO
        // dispara con el ítem EN LA MANO. El selector real de la casa
        // es AltFunctionUse: el click derecho con el ítem SOSTENIDO
        // entra por CanUseItem+UseItem con player.altFunctionUse==2 —
        // igual que el selector de oleadas del hambre.
        public static int PasoDe(Player p)
        {
            var sp = p.GetModPlayer<global::AethonMod.Content.Players.ShardPlayer>();
            return sp != null && sp.EcoPasoLargo ? 10 : 1;
        }

        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 22;
            Item.maxStack = 1;
            Item.consumable = false;          // NO CONSUMIBLE (la letra)
            Item.rare = ItemRarityID.Quest;   // la llave de pruebas de la casa
            Item.value = 0;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.UseSound = SoundID.Item4;
        }

        // v6.147 — LA FUNCIÓN ALTERNATIVA (click derecho EN LA MANO):
        // AltFunctionUse marca el ítem como usable con click derecho —
        // el uso entra por UseItem con player.altFunctionUse == 2.
        public override bool AltFunctionUse(Player player)
            => !player.mouseInterface;

        public override bool? UseItem(Player player)
        {
            // v6.147 — ¿CLICK DERECHO? (altFunctionUse 2): ALTERNA EL
            // PASO (1 ↔ 10) y NO aplica nada — el borde del selector.
            if (player.altFunctionUse == 2)
            {
                var sp = player.GetModPlayer
                    <global::AethonMod.Content.Players.ShardPlayer>();
                if (sp == null) return true;
                sp.EcoPasoLargo = !sp.EcoPasoLargo;
                Main.NewText(Terraria.Localization.Language.GetTextValue(
                    "Mods.AethonMod.Items.EcoDelDespertar.Paso",
                    sp.EcoPasoLargo ? 10 : 1), new Microsoft.Xna.Framework.Color(196, 138, 255));
                Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                return true;
            }

            // LA APLICACIÓN (click izquierdo): la ceremonia completa de
            // las tres ofrendas, sin cobrarlas — es el probador.
            // v6.51.6 — sube el despertar DEL GRIMORIO ACTIVO (cada
            // libro es su propia escalera). v6.146 — EL PASO DEL
            // SELECTOR: la potencia elegida (1 o 10 — PasoDe) se aplica.
            Item eterno = global::AethonMod.Content.Globals.AlmaEternoGlobalItem.Activo(player);
            if (eterno == null) return false;
            int pasos = PasoDe(player);
            for (int i = 0; i < pasos; i++)
            {
                var alma = eterno.GetGlobalItem
                    <global::AethonMod.Content.Globals.AlmaEternoGlobalItem>();
                if (alma == null ||
                    alma.NivelDespertar >= Content.Systems.DespertarGrimorio.NivelMax)
                    break;
                Content.Systems.DespertarGrimorioSistema.SubirNivel(player, eterno, null);
            }
            return true;
        }

        public override bool CanUseItem(Player player)
        {
            // En el tope ya no despierta más (la llave gira en vacío).
            // v6.51.6 — el despertar es DEL LIBRO: se lee el Eterno
            // ACTIVO (en la mano primero; si no, el del inventario).
            return Content.Systems.DespertarGrimorio.NivelDe(player)
                < Content.Systems.DespertarGrimorio.NivelMax;
        }
    }
}
