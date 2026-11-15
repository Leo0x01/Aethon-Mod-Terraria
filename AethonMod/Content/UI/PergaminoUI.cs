using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace AethonMod.Content.UI
{
    /// <summary>
    /// PergaminoUI — v6.141 — EL PERGAMINO MURIÓ (la letra: «de hecho
    /// que no sea un pergamino, que sea un libro creado por la sforza
    /// en fullhd»): TODO su contenido y SU VIDA pasaron a
    /// LibroDelEternoUI — EL LIBRO ABIERTO con el icono del grimorio
    /// junto al botón de logros del inventario. Esta clase queda como
    /// REDIRECTOR para los caminos viejos (la tecla P del altar y
    /// cualquier llamada): abrir el pergamino ES abrir EL LIBRO.
    /// </summary>
    public class PergaminoUI : ModSystem
    {
        internal static bool Abierto => LibroDelEternoUI.Abierto;

        public static void Abrir() => LibroDelEternoUI.Abrir();

        public static void Cerrar() => LibroDelEternoUI.Cerrar();
    }
}
