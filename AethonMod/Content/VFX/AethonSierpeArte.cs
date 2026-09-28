using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// AethonSierpeArte — v6.50.33 — LA LIBRERÍA ADELGAZADA DEL DRAGÓN.
    ///
    /// HISTORIA: la v6.50.27 parió el arte 100% código de la sierpe
    /// estelar; la v6.50.32 lo reescribió para replicar a Slifer con
    /// pinceles generados en runtime — y el veredicto del usuario fue
    /// literal: «no se parece en nada». LA TÉCNICA NUEVA (v6.50.33, la
    /// petición): «mejor rediseña al jefe completo, podrias crear sprite
    /// segmentados basados en slifer, por ejemplo puedes usar Devourer
    /// of Gods de Calamity sprites por segmento y rediseñarlos cambiando
    /// el color y el arte» — el jefe pasa a SPRITES POR SEGMENTO (el
    /// patrón del DoG: cada segmento lleva su propio sprite), pintados
    /// por tools/tools/gen_slifer_sprites_v6533.py (canon cromático de
    /// las referencias del usuario despedazadas con visión artificial:
    /// hocico plateado #8A9299, lomo #A61C1C, vientre pizarra #2C2D35,
    /// colmillos marfil, ojo oro #FFD700, gema #35C5FF, paño #6B1515,
    /// huesos vivos #D42B2B) y ensamblados por el PreDraw de
    /// AethonBoss/AethonSierpeCuerpo.
    ///
    /// AQUÍ QUEDA: la ESTRELLA DEL FONDO (la pide el leviatán del cielo
    /// de ColaSierpeSky) y los ACCESSORES de los sprites del jefe
    /// (cacheados y null-seguros — el patrón de la casa: Request
    /// perezoso, try/catch en el call-site). Cabeza/Vertebra/Alas y los
    /// DOS PINCELES de la v6.50.32 MUEREN (la auditoría de la DLL debe
    /// verlos AUSENTES).
    /// </summary>
    public static class AethonSierpeArte
    {
        private static Asset<Texture2D> _star;
        private static Asset<Texture2D> _cabeza;
        private static Asset<Texture2D> _mandibula;
        private static Asset<Texture2D> _vertebra;
        private static Asset<Texture2D> _cola;
        private static Asset<Texture2D> _ala;

        /// <summary>La estrella de 4 puntas (las brasas de la casa).</summary>
        private static Texture2D Star =>
            (_star ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/Star")).Value;

        /// <summary>
        /// LA ESTRELLA DEL FONDO (el leviatán del cielo la pide para sus
        /// apófisis — la misma textura, cacheada y null-segura).
        /// </summary>
        public static Texture2D EstrellaDelFondo()
        {
            try { return Star; }
            catch { return VFXCore.SoftGlow; }
        }

        // ==================================================================
        //  v6.50.33 — LOS SPRITES DEL DRAGÓN DEL CIELO (el set del
        //  generador: supersampling ×3, cel-shading en 3 bandas duras,
        //  contorno de 2 px — el look Calamity que pidió el usuario).
        //  Cada accessor puede lanzar si el asset aún no vive (menú,
        //  descarga): los call-sites los envuelven en try/catch.
        // ==================================================================

        /// <summary>
        /// EL CRÁNEO — la máscara plateada del hocico, DOS colmillos
        /// sable, la corona de 5 llamas, el ojo de oro furioso y la gema
        /// azul de la frente (240×152, mira a la DERECHA; el cuello se
        /// estrecha hacia atrás para que la primera vértebra lo solape).
        /// </summary>
        public static Texture2D Cabeza() =>
            (_cabeza ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/NPCs/AethonSierpeCabeza")).Value;

        /// <summary>
        /// LA MANDÍBULA — la boca de acero entera con EL TRIÁNGULO
        /// INTERIOR oscuro (cubre la rendija al abrirse) y la SEGUNDA
        /// BOCA plateada (170×134; la bisagra vive en local (12,50) y
        /// casa con la del cráneo (100,94)).
        /// </summary>
        public static Texture2D Mandibula() =>
            (_mandibula ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/NPCs/AethonSierpeMandibula")).Value;

        /// <summary>
        /// LA VÉRTEBRA — el anillo del cuerpo: chevrones de escama en dos
        /// filas, la aleta dorsal en teja, las 3 bandas de pizarra del
        /// vientre y el filado frontal claro (la lectura de anillos
        /// encadenados cuando los eslabones se solapan, 124×130).
        /// </summary>
        public static Texture2D Vertebra() =>
            (_vertebra ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/NPCs/AethonSierpeVertebra")).Value;

        /// <summary>
        /// LA COLA — la pala espatulada del final con muescas y puntas a
        /// brasa (196×122; la base ancha a la derecha, la punta a la
        /// izquierda).
        /// </summary>
        public static Texture2D Cola() =>
            (_cola ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/NPCs/AethonSierpeCola")).Value;

        /// <summary>
        /// EL ALA — el ala-brazo de murciélago: el brazo y los TRES dedos
        /// ganchudos de rojo vivo SOBRE el paño granate festoneado, la
        /// garra de marfil y las brasas de las puntas (266×214; la RAÍZ
        /// vive en local (168,202) — el origen de anclaje al hombro).
        /// </summary>
        public static Texture2D Ala() =>
            (_ala ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/NPCs/AethonSierpeAla")).Value;
    }
}
