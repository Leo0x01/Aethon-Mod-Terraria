
using Terraria.ModLoader.Config;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Input;
using System.ComponentModel;

namespace AethonMod.Content
{
    /// <summary>
    /// Configuración del mod Aethon.
    /// v6.50.2 — FIX (config ClientSide leída por la AUTORIDAD):
    /// XPMultiplier y EventoHambreGrimorio eran decisions de la AUTORIDAD
    /// (las leen ShardLevelSystem/ShardPlayer en el SERVER) pero vivían
    /// aquí, en un scope ClientSide — en dedicado aplicaban los defaults
    /// del server y la config del cliente era mentira. Ambas se mudaron a
    /// AethonConfigServidor (ConfigScope.ServerSide). Esta clase conserva
    /// las decisions de PANTALLA del jugador local.
    /// </summary>
    public class AethonConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        // v6.50.3 — FIX (config muerta): MaxShardLevel VIVÍA aquí desde
        // siempre y NADIE lo leía (opción fantasma en el menú). Se muda a
        // AethonConfigServidor — es una decisión de la AUTORIDAD (el nivel
        // lo cuenta el server en MP; el mismo diagnóstico del split
        // v6.50.2) y ahora SÍ se aplica en ShardLevelItem.GrantXP.

        [DefaultValue(true)]
        public bool ShowLevelUpNotifications = true;

        [DefaultValue(true)]
        public bool ShowMilestoneNotifications = true;

        // v6.50.3 — FIX (config muerta): ShowDebugInfo tampoco la leía nadie.
        // AHORA: true = el overlay F8 de diagnóstico arranca ABIERTO al entrar
        // al mundo (la herramienta del probador a la vista); F8 lo alterna
        // como siempre. Se lee en DiagnosticoVFXSystem.OnWorldLoad.
        [DefaultValue(false)]
        public bool ShowDebugInfo = false;

        // ------------------------------------------------------------------
        //  v6.44 — EL MODO PRUEBAS DE LOS LUGARES
        // ------------------------------------------------------------------

        /// <summary>
        /// ¿El Sagrario Hueco baja su puerta de 400 PV al máximo? ON por
        /// defecto: todo el mod es de PRUEBAS y el bioma del Sagrario
        /// debe poder activarse descendiendo al subsuelo con cualquier
        /// jugador de pruebas. Apágalo para restaurar la puerta real de
        /// los 400 PV. (v6.50.76 — los FONDOS de bioma de CieloLib ya no
        /// existen: la bandera ya no cambia el paisaje de las cuevas,
        /// solo la música y demás efectos de escena del bioma.)
        /// </summary>
        [DefaultValue(true)]
        public bool SagrarioAccesibleEnPruebas = true;

        // ------------------------------------------------------------------
        //  v6.45 — EL MODO PRUEBAS DEL MANÁ DEL MINIÓN
        // ------------------------------------------------------------------

        /// <summary>
        /// ¿La invocación del Orbe Cósmico del Grimorio es gratis? ON por
        /// defecto: desde v6.42 TODO el arsenal de pruebas es sin maná y el
        /// coste del minion era la última excepción viva. Es una
        /// conveniencia de PRUEBAS, no el diseño final — apágala para
        /// restaurar el coste real escalado por nivel (15 + nivel, tope 100).
        /// </summary>
        [DefaultValue(true)]
        public bool ManaGratisEnPruebas = true;

        // ------------------------------------------------------------------
        //  v6.120 — LA FORMA ETERNA A LA VISTA (la letra: «la forma eterna
        //  debe poder activarse y ocultarse con las opciones del juego»)
        // ------------------------------------------------------------------

        /// <summary>
        /// ¿El panteón visual de LA FORMA ETERNA se viste? (el interruptor
        /// de las opciones del juego — el menú de configuración del mod).
        /// OFF apaga SOLO el teatro (las dos luces, el anillo dorsal, la
        /// brasa, el brillo, las chispas y la luz): los STATS del panteón
        /// y el AURA DEL VACÍO (el campo que quema impíos) siguen siendo
        /// de juego y NO se tocan. Además, con el accesorio en el hueco
        /// funcional, el OJO de vanilla (ocultar accesorio) también lo
        /// esconde — las DOS opciones del juego mandan: la del menú y la
        /// del hueco.
        /// </summary>
        [DefaultValue(true)]
        public bool MostrarFormaEterna = true;

        /// <summary>¿La Forma Eterna se viste AHORA? (la opción del menú
        /// leída por el ítem y el escaneo — el punto único de la
        /// verdad).</summary>
        public static bool FormaEternaVisible()
        {
            try
            {
                var cfg = ModContent.GetInstance<AethonConfig>();
                return cfg == null || cfg.MostrarFormaEterna;
            }
            catch { return true; }
        }
    }
}
