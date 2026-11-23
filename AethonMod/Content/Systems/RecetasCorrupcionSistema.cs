using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace AethonMod.Content.Systems
{
    // ======================================================================
    //  LAS RECETAS DE LA CORRUPCIÓN (v6.50.130 → v6.51.0) — LA LETRA DEL
    //  USUARIO: «además que no sea necesario lanzar nada, que DESDE EL
    //  INVENTARIO funcione en el apartado creación, que se permita poder
    //  corromper las armas y convertir todo en Esencia Corrupta».
    //
    //  v6.51.0 — LA ESTACIÓN MURIÓ (la tile de bruma era la puerta de la
    //  .130): la condición es LLEVAR EL GRIMORIO DEL ETERNO ENCIMA — con
    //  el libro en el inventario, el APARTADO CREACIÓN muestra:
    //
    //   · CORROMPER LAS ARMAS («las armas de cualquier tipo obtendrán la
    //     etiqueta corrupto y ya sabes no se podrán modificar»): cada
    //     arma del juego (vanilla + mods) tiene su receta 1× arma → 1×
    //     la MISMA arma. El resultado nace con la ESTADÍSTICA CORRUPTA
    //     (MarcarCorrupta pone el sello y pela el prefijo: sin Superior,
    //     sin Mítica, sin Rota — el yunque le queda cerrado para siempre
    //     y el arma corrupta gana +5 % de daño y otros stats en %).
    //
    //   · CONVERTIR TODO EN ESENCIA CORRUPTA: cada item NO-arma tiene su
    //     receta 1× item → N× Esencia Corrupta (N por su valor: la
    //     basura 1, un tesoro hasta 30).
    //
    //  EL TRUCO DEL MENÚ: el apartado creación SOLO muestra las recetas
    //  cuyos materiales LLEVAS — con el libro encima, cada item de tu
    //  inventario EN PERSONA muestra su receta de corrupción (el menú se
    //  convierte en la lista de platos del Eterno).
    //
    //  LAS EXCLUSIONES (lo que el grimorio no toca): la Esencia Corrupta
    //  misma (el bucle infinito) y el Grimorio del Eterno (¡el comensal
    //  no es plato!).
    // ======================================================================
    public class RecetasCorrupcionSistema : ModSystem
    {
        /// <summary>LA CONDICIÓN: «el Grimorio del Eterno en el
        /// inventario» (la letra: DESDE EL INVENTARIO, sin lanzar nada y
        /// sin estaciones — el libro ES la estación).</summary>
        private static readonly Condition LlevaElGrimorio = new Condition(
            Language.GetText("Mods.AethonMod.Despertar.CondicionRecetas"),
            () =>
            {
                Player yo = Main.LocalPlayer;
                if (yo == null) return false;
                int eterno = ModContent.ItemType
                    <global::AethonMod.Content.Weapons.GrimoireEternal>();
                for (int k = 0; k < 58; k++)
                {
                    Item it = yo.inventory[k];
                    if (it != null && !it.IsAir && it.type == eterno) return true;
                }
                return false;
            });

        // v6.173 — LAS RECETAS DE CONSAGRAR DEL INVENTARIO MURIERON
        // (la letra: «veo que hay recetas de consagrar que se ven desde
        // el inventario, quitalas, solo se puede consagrar desde la
        // ventana del altar»): el apartado creación YA NO consagra —
        // LA ÚNICA CONSAGRACIÓN es la ventana del altar del libro (su
        // botón, su precio, su teatro). Este sistema queda VACÍO: ni
        // una receta se registra.
        public override void AddRecipes()
        {
        }

        /// <summary>v6.51.1 — ¿El portador lleva una instancia LIMPIA de
        /// esta arma? (la letra: «un arma corrupta no puede volver a
        /// corromperse» — la receta se apaga cuando la única copia que
        /// llevas ya está sellada).</summary>
        private static Condition QuedaUnaLimpia(int tipo)
        {
            return new Condition(Language.GetText("Mods.AethonMod.Despertar.ReqArmaLimpia"),
                () =>
                {
                    Player yo = Main.LocalPlayer;
                    if (yo == null) return false;
                    for (int k = 0; k < 58; k++)
                    {
                        Item it = yo.inventory[k];
                        if (it == null || it.IsAir || it.type != tipo) continue;
                        var sello = it.GetGlobalItem
                            <global::AethonMod.Content.Globals.CorruptoGlobalItem>();
                        if (sello == null || !sello.Corrompido) return true;   // HAY una limpia
                    }
                    return false;
                });
        }

        /// <summary>v6.51.1 — EL AVISO DEL MENÚ (siempre cierto, verde):
        /// las estadísticas del arma corrupta visibles en el apartado de
        /// creación — la etiqueta que la letra pidió.</summary>
        private static readonly Condition NaceCorrupta = new Condition(
            Language.GetText("Mods.AethonMod.Despertar.NaceCorrupta"),
            () => true);

        /// <summary>EL SELLO DE LA RECETA: el arma fabricada con el
        /// grimorio ENCIMA NACE CORRUPTA — la estadística de la letra
        /// («obtendrán la etiqueta corrupto y ya sabes no se podrán
        /// modificar»): el sello CorruptoGlobalItem que viaja con ella
        /// para siempre + el prefijo PELADO (ResetPrefix — sin Superior,
        /// sin Mítica, sin Rota: el yunque le queda cerrado de por
        /// vida).</summary>
        private static void MarcarCorrupta(Recipe recipe, Item item,
            List<Item> consumedItems, Item destinationStack)
        {
            try
            {
                if (item == null || item.IsAir) return;
                // v6.158 — LA CONSAGRACIÓN CENTRAL (la poción
                // consagrada DUPLICA lo que hace).
                global::AethonMod.Content.Globals.CorruptoGlobalItem.Consagrar(item);
                item.ResetPrefix();   // «ya no puede tener etiquetas» — pelada de fábrica
                // v6.89 — Y VISTE SU RAREZA (el nombre negro en TODAS partes).
                global::AethonMod.Content.Globals.CorruptoGlobalItem.VestirLaRareza(item);
            }
            catch { }
        }
    }
}
