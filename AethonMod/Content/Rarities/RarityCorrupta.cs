using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.Rarities
{
    // ======================================================================
    //  v6.89 — LA RAREZA CORRUPTA (la letra: «probe el altar y corrompi un
    //  arma, pero el color del nombre es diferente en el inventario que en
    //  la barra de acceso rapido»): EL NOMBRE DEL ARMA CORRUPTA se dibuja
    //  por DOS rutas — el TOOLTIP y la BARRA DE ACCESO RÁPIDO — y ambas
    //  leen EL COLOR DE SU RAREZA: UN color, UNA fuente.
    //
    //  v6.104 — EL GRIS HUMO DE LA CONSAGRACIÓN (la letra: «el color con
    //  los efectos a veces no coincide el de la barra de acceso rapido
    //  con el que se muestra en el inventario»): el degradado
    //  letra-por-letra de la v6.97 SOLO vivía en el tooltip (los tags
    //  [c/] se verían crudos en la barra) — DOS colores para un nombre.
    //  MUERE el degradado y la rareza viste EL TONO MEDIO del degradado
    //  que era (bordes blancos → MEDIOS GRISES → centro negro): el GRIS
    //  HUMO (145,145,155) — legible sobre cualquier fondo, idéntico en
    //  tooltip, barra rápida, chat de recogida y TODAS partes.
    // ======================================================================
    public class RarityCorrupta : ModRarity
    {
        /// <summary>EL GRIS HUMO DE LA CONSAGRACIÓN — el tono medio del
        /// degradado que fue (v6.97), el MISMO color en tooltip y barra
        /// rápida: una sola fuente.</summary>
        public override Color RarityColor => new Color(145, 145, 155);
    }
}
