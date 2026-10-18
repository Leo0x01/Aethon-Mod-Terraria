using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Systems
{
    // ======================================================================
    //  LAS RECETAS DE LA CORRUPCIÓN (v6.50.130) — LA LETRA DEL USUARIO:
    //  «ademas que no sea necesario lanzar nada, que desde el inventario
    //  funcione en el apartado creacion, que se permita poder corromper
    //  las armas y convertir todo en Esencia Corrupta».
    //
    //  LA ESTACIÓN: LA TILE DE BRUMA (BrumaAltarTile — el altar principal
    //  portátil: el item invisible del cráter, colocado donde el jugador
    //  quiera). Al lado de la bruma, el APARTADO CREACIÓN del inventario
    //  ofrece DOS familias de recetas:
    //
    //   · CORROMPER LAS ARMAS («las armas de cualquier tipo obtendran la
    //     etiqueta corrupto y ya sabes no se podran modificar»): cada
    //     arma del juego (vanilla + mods) tiene su receta 1× arma → 1×
    //     la MISMA arma, fabricada JUNTO A LA BRUMA. El resultado nace
    //     con la ESTADÍSTICA CORRUPTA (CorruptoGlobalItem.OnCraft pone
    //     el sello y pela el prefijo: sin Superior, sin Mítica, sin
    //     Rota — el yunque le queda cerrado para siempre).
    //
    //   · CONVERTIR TODO EN ESENCIA CORRUPTA: cada item NO-arma tiene su
    //     receta 1× item → N× Esencia Corrupta (N por su valor: la
    //     basura 1, un tesoro hasta 30 — el mismo espíritu del
    //     pudrimiento de la bruma, ahora SIN LANZAR NADA).
    //
    //  EL TRUCO DEL MENÚ: el apartado creación SOLO muestra las recetas
    //  cuyos materiales LLEVAS — junto a la bruma, cada item de tu
    //  inventario EN PERSONA muestra su receta de corrupción (el menú se
    //  convierte en la lista de platos del Eterno).
    //
    //  LAS EXCLUSIONES (lo que la bruma no toca): la Esencia Corrupta
    //  misma (el bucle infinito), la Bruma del Cráter (el altar no es
    //  plato) y el Grimorio del Eterno (¡el comensal no es plato!).
    // ======================================================================
    public class RecetasCorrupcionSistema : ModSystem
    {
        public override void AddRecipes()
        {
            try
            {
                int tileBruma = ModContent.TileType<global::AethonMod.Content.Tiles.BrumaAltarTile>();
                int tipoHusos = ModContent.ItemType<global::AethonMod.Content.Items.Altar.ItemCorrupto>();
                int tipoBruma = ModContent.ItemType<global::AethonMod.Content.Items.Altar.BrumaDelCrater>();
                int eterno = ModContent.ItemType<global::AethonMod.Content.Weapons.GrimoireEternal>();

                int total = ItemLoader.ItemCount;
                int armas = 0, esencias = 0;

                for (int t = 1; t < total; t++)
                {
                    // LO QUE LA BRUMA NO TOCA.
                    if (t == tipoHusos || t == tipoBruma || t == eterno) continue;

                    // EL PROTOTIPO (ContentSamples ya conoce a todos: los
                    // items cargan ANTES que las recetas).
                    Item proto = null;
                    try { proto = ContentSamples.ItemsByType[t]; } catch { }
                    if (proto == null) continue;

                    if (AltarDevoradorSistema.EsArma(proto))
                    {
                        // ===== LA RECETA DEL SELLO: arma → arma CORRUPTA =====
                        // (EL SELLO lo pone MarcarCorrupta — el callback de
                        // ESTA receta: el resultado nace con la etiqueta
                        // Corrupto y SIN prefijo. La firma del delegado
                        // verificada al IL del tML 2026.08:
                        // OnCraftCallback(Recipe, Item, List<Item>, Item)).
                        Recipe.Create(t, 1)
                            .AddIngredient(t, 1)
                            .AddTile(tileBruma)
                            .AddOnCraftCallback(MarcarCorrupta)
                            .Register();
                        armas++;
                    }
                    else
                    {
                        // ===== LA RECETA DE LA ESENCIA: item → N esencias =====
                        // (N por lo que VALÍA — el mismo espíritu del
                        // pudrimiento de la bruma: 1 + valor/2500, 1..30).
                        long valor = Math.Max(0L, (long)proto.value);
                        int n = (int)MathHelper.Clamp(1 + valor / 2500L, 1, 30);
                        Recipe.Create(tipoHusos, n)
                            .AddIngredient(t, 1)
                            .AddTile(tileBruma)
                            .Register();
                        esencias++;
                    }
                }
            }
            catch { }
        }

        /// <summary>EL SELLO DE LA RECETA: el arma fabricada junto a la
        /// bruma NACE CORRUPTA — la estadística de la letra («obtendran
        /// la etiqueta corrupto y ya sabes no se podran modificar»):
        /// el sello CorruptoGlobalItem que viaja con ella para siempre +
        /// el prefijo PELADO (ResetPrefix — sin Superior, sin Mítica,
        /// sin Rota: el yunque le queda cerrado de por vida).</summary>
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
