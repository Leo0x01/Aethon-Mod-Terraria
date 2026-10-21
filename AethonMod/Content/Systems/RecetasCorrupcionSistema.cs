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

        public override void AddRecipes()
        {
            try
            {
                int eterno = ModContent.ItemType<global::AethonMod.Content.Weapons.GrimoireEternal>();

                int total = ItemLoader.ItemCount;
                int armas = 0;

                for (int t = 1; t < total; t++)
                {
                    // LO QUE EL GRIMORIO NO TOCA.
                    if (t == eterno) continue;

                    // EL PROTOTIPO (ContentSamples ya conoce a todos: los
                    // items cargan ANTES que las recetas).
                    Item proto = null;
                    try { proto = ContentSamples.ItemsByType[t]; } catch { }
                    if (proto == null) continue;

                    // v6.51.1 — SOLO ARMAS (la letra: «mejor quitemos que
                    // cualquier cosa se convierta en esencia corrupta,
                    // dejemos que la esencia corrupta sea solo un loot de
                    // monstruo y jefes»): las recetas item→esencia
                    // MURIERON — la Esencia Corrupta solo cae de los
                    // cuerpos.
                    if (!DespertarGrimorioSistema.EsArma(proto)) continue;

                    // ===== LA RECETA DEL SELLO: arma → arma CORRUPTA =====
                    // (EL SELLO lo pone MarcarCorrupta — el callback de
                    // ESTA receta: el resultado nace con la etiqueta
                    // Corrupto y SIN prefijo). LAS CONDICIONES:
                    //   · LlevaElGrimorio: el libro en el inventario (la
                    //     corrupción vive en el apartado de creación — sin
                    //     estaciones, sin lanzar nada).
                    //   · QuedaUnaLimpia: el arma corrupta NO puede volver
                    //     a corromperse (la letra) — la receta solo se
                    //     ofrece si llevas una instancia SIN corromper.
                    //   · NaceCorrupta (siempre cierta): el aviso de las
                    //     estadísticas del arma corrupta EN EL MENÚ (la
                    //     letra: «eso debe mostrarse con una etiqueta»).
                    Recipe.Create(t, 1)
                        .AddIngredient(t, 1)
                        .AddCondition(LlevaElGrimorio)
                        .AddCondition(QuedaUnaLimpia(t))
                        .AddCondition(NaceCorrupta)
                        .AddOnCraftCallback(MarcarCorrupta)
                        .Register();
                    armas++;
                }
            }
            catch { }
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
                var sello = item.GetGlobalItem<global::AethonMod.Content.Globals.CorruptoGlobalItem>();
                if (sello == null) return;
                sello.Corrompido = true;
                item.ResetPrefix();   // «ya no puede tener etiquetas» — pelada de fábrica
            }
            catch { }
        }
    }
}
