using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.Rarities
{
    // ======================================================================
    //  v6.89 — LA RAREZA CORRUPTA (la letra: «probe el altar y corrompi un
    //  arma, pero el color del nombre es diferente en el inventario que en
    //  la barra de acceso rapido»): EL NOMBRE DEL ARMA CORRUPTA se dibuja
    //  por DOS rutas — el TOOLTIP (donde la v6.88 le forzaba un
    //  OverrideColor (30,30,40) que SOLO vive ahí) y la BARRA DE ACCESO
    //  RÁPIDO (donde vanilla pinta el nombre con EL COLOR DE SU RAREZA —
    //  y el arma corrupta conservaba su rareza ORIGINAL: blanca, azul,
    //  violeta…): DOS colores distintos para el mismo nombre.
    //
    //  LA CURA (el precedente de la v6.51.11 — LegendarioSupremo): el
    //  arma corrupta VISTE SU PROPIA MODRARITY. Al corromperse, su
    //  item.rare pasa a ESTA rareza — y las DOS rutas leen el MISMO
    //  color: el NEGRO TINTA de la casa (30,30,40 — la letra de la
    //  v6.88: «su nombre se vuelve negro»), idéntico en el inventario,
    //  en la barra rápida, en el chat de recogida y en todas partes.
    // ======================================================================
    public class RarityCorrupta : ModRarity
    {
        /// <summary>EL NEGRO TINTA (la letra: «su nombre se vuelve negro»)
        /// — el MISMO color en tooltip y barra rápida: una sola fuente.</summary>
        public override Color RarityColor => new Color(30, 30, 40);
    }
}
