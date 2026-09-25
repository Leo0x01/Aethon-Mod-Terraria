using System;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    // ======================================================================
    //  v6.43 — EL PAISAJE DEL SAGRARIO HUECO (los fondos de bioma de
    //  CieloLib). Tres texturas propias del Sagrario (la nebulosa violeta,
    //  las montañas rúnico-violeta y las columnas verticales brillantes)
    //  enchufadas al sistema de fondos del juego, en sus DOS variantes:
    //
    //   · SanctumBackgroundStyle (ModSurfaceBackgroundStyle): el fondo de
    //     SUPERFICIE — capas lejana / media / cercana con parallax propio,
    //     el camino oficial del paisaje de bioma sobre el suelo.
    //   · SanctumUndergroundBackgroundStyle (ModUndergroundBackgroundStyle):
    //     el fondo de SUBSUELO — el Sagrario vive bajo tierra (capa de
    //     tierra y de roca), así que su paisaje también habla el idioma de
    //     los fondos de cueva.
    //
    //  Las texturas se registran como SLOTS DE FONDO en el Load() del mod
    //  (AethonMod.cs → BackgroundTextureLoader.AddBackgroundTexture ×3) y
    //  aquí solo se PIDEN por su ruta relativa.
    // ======================================================================

    /// <summary>
    /// El fondo de superficie del Sagrario Hueco: la nebulosa lejana, las
    /// montañas rúnicas en la media y las columnas de luz en la cercana.
    /// </summary>
    public class SanctumBackgroundStyle : ModSurfaceBackgroundStyle
    {
        /// <summary>La nebulosa del Sagrario (la capa lejana).</summary>
        public const string RutaFar = "Content/Effects/Cielo/SanctumFar";

        /// <summary>Las montañas rúnico-violeta (la capa media).</summary>
        public const string RutaMiddle = "Content/Effects/Cielo/SanctumMiddle";

        /// <summary>Las columnas verticales brillantes (la capa cercana).</summary>
        public const string RutaClose = "Content/Effects/Cielo/SanctumClose";

        public override int ChooseFarTexture()
            => BackgroundTextureLoader.GetBackgroundSlot(Mod, RutaFar);

        public override int ChooseMiddleTexture()
            => BackgroundTextureLoader.GetBackgroundSlot(Mod, RutaMiddle);

        /// <summary>
        /// La capa CERCANA: las columnas del Sagrario, con su escala y su
        /// parallax (0.35 — viaja sensiblemente menos que el terreno, como
        /// corresponde a lo que está junto al horizonte del bioma).
        /// </summary>
        public override int ChooseCloseTexture(ref float scale, ref double parallax, ref float a, ref float b)
        {
            scale = 0.8f;
            parallax = 0.35;
            a = 0f;
            b = 180f;       // la posición cero vertical del fondo cercano
            return BackgroundTextureLoader.GetBackgroundSlot(Mod, RutaClose);
        }

        /// <summary>
        /// Los fundidos de la capa lejana: el estilo del Sagrario entra con
        /// la velocidad de transición del juego y los demás estilos salen
        /// con la misma — el cruce limpio de los fondos de bioma.
        /// </summary>
        public override void ModifyFarFades(float[] fades, float transitionSpeed)
        {
            for (int i = 0; i < fades.Length; i++)
            {
                if (i == Slot)
                    fades[i] = Math.Min(fades[i] + transitionSpeed, 1f);
                else
                    fades[i] = Math.Max(fades[i] - transitionSpeed, 0f);
            }
        }
    }

    /// <summary>
    /// El fondo de subsuelo del Sagrario Hueco. El bioma está activo en la
    /// capa de tierra y en la de roca, así que su paisaje de cueva usa la
    /// nebulosa en la tierra, las montañas en la roca y las columnas en el
    /// nivel profundo del array (donde el bioma no llega, pero el estilo
    /// debe rellenarlo entero).
    /// </summary>
    public class SanctumUndergroundBackgroundStyle : ModUndergroundBackgroundStyle
    {
        public override void FillTextureArray(int[] textureSlots)
        {
            // 0 = la capa de tierra · 1 = la capa de roca · 2 = lo profundo.
            string[] rutas =
            {
                SanctumBackgroundStyle.RutaFar,
                SanctumBackgroundStyle.RutaMiddle,
                SanctumBackgroundStyle.RutaClose,
            };
            for (int i = 0; i < textureSlots.Length && i < rutas.Length; i++)
                textureSlots[i] = BackgroundTextureLoader.GetBackgroundSlot(Mod, rutas[i]);
        }
    }
}
