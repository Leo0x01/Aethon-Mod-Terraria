using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.Rarities
{
    // ======================================================================
    //  LA RAREZA CÓSMICA DEL GRIMORIO DEL ETERNO (v6.51.1) — LA LETRA:
    //  «mejorar la rareza del grimorio del eterno, es un arma super rara
    //  asi que su nombre debe tener una rareza mas que legendaria, mas
    //  que mitica, mas que unica, una rareza cosmica ademas con la
    //  etiqueta de legendario supremo».
    //
    //  v6.51.11 — LA RAREZA VISTE LA ESCALERA (la letra del usuario:
    //  «cuando el inventario esta abierto el nombre aparece en el color
    //  correcto para el nivel actual del grimorio con su modificador
    //  (ignorante), pero al cerrar el inventario y poner el cursor
    //  encima del grimorio en la barra de acceso rapido su nombre sale
    //  de colores mostrando un nivel diferente, esto no debe ser»):
    //  la CAUSA RAÍZ — con el inventario ABIERTO el nombre lo pinta el
    //  tooltip del arma (ModifyTooltips → ColorEtiqueta: el color de SU
    //  peldaño de LA REFORJA, gris para el Ignorante)… pero con el
    //  inventario CERRADO, el nombre sobre la barra de acceso rápido lo
    //  dibuja VANILLA con EL COLOR DE LA RAREZA (Main.MouseText con el
    //  rare del item — verificado al IL: hoverItemName + rare), y esta
    //  rareza era SIEMPRE el violeta cósmico animado: «un nivel
    //  diferente». LA CURA: la rareza PREGUNTA por el libro que está a
    //  la vista (el del cursor → el señalado → el ACTIVO del jugador
    //  local) y viste EL COLOR DE SU PELDAÑO — el mismo del tooltip:
    //  gris el Ignorante, oro el Mítico, violeta la cima. El violeta
    //  cósmico de siempre queda como el respaldo cuando NO hay libro a
    //  la vista.
    // ======================================================================
    public class LegendarioSupremo : ModRarity
    {
        public override Color RarityColor
        {
            get
            {
                try
                {
                    int eterno = ModContent.ItemType
                        <global::AethonMod.Content.Weapons.GrimoireEternal>();

                    // 1) EL LIBRO EN EL CURSOR (moverlo entre slots, la
                    //    tienda, el cofre: el color lo sigue).
                    if (Main.mouseItem != null && !Main.mouseItem.IsAir &&
                        Main.mouseItem.type == eterno)
                        return ColorDelLibro(Main.mouseItem);

                    // 2) EL LIBRO SEÑALADO (la copia de tML del tooltip).
                    if (Main.HoverItem != null && !Main.HoverItem.IsAir &&
                        Main.HoverItem.type == eterno)
                        return ColorDelLibro(Main.HoverItem);

                    // 3) EL LIBRO ACTIVO DEL JUGADOR LOCAL (el caso de la
                    //    letra: el inventario CERRADO y el cursor sobre
                    //    la barra — vanilla pinta ese nombre con ESTE
                    //    color, que sea el de SU peldaño).
                    Item activo = global::AethonMod.Content.Globals
                        .AlmaEternoGlobalItem.Activo(Main.LocalPlayer);
                    if (activo != null)
                        return ColorDelLibro(activo);
                }
                catch { }

                // SIN LIBRO A LA VISTA: el violeta cósmico de siempre —
                // el ciclo primordial ↔ índigo del vacío ↔ magenta de la
                // bruma (el color jamás se repite exacto).
                float t = Main.GlobalTimeWrappedHourly;
                float r = 0.62f + 0.20f * MathF.Sin(t * 0.9f);
                float g = 0.22f + 0.14f * MathF.Sin(t * 0.53f + 2.1f);
                float b = 0.95f + 0.05f * MathF.Sin(t * 1.7f + 0.6f);
                return new Color(r, g, b);
            }
        }

        /// <summary>EL COLOR DE SU PELDAÑO de LA REFORJA (la MISMA paleta
        /// del tooltip del arma — una sola fuente de verdad).</summary>
        private static Color ColorDelLibro(Item libro)
        {
            var sl = libro.GetGlobalItem<global::AethonMod.Content.Globals.ShardLevelItem>();
            return global::AethonMod.Content.Weapons.GrimoireEternal
                .ColorEtiqueta(global::AethonMod.Content.Weapons.GrimoireEternal
                    .IndiceEtiqueta(sl?.Level ?? 1));
        }
    }
}
