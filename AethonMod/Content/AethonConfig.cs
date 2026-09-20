
using Terraria.ModLoader.Config;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Input;
using System.ComponentModel;

namespace AethonMod.Content
{
    /// <summary>
    /// Configuración del mod Aethon.
    /// </summary>
    public class AethonConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        [DefaultValue(1f)]
        [Range(0.1f, 10f)]
        public float XPMultiplier = 1f;

        [DefaultValue(0)]
        [Range(0, 500)]
        public int MaxShardLevel = 0;

        [DefaultValue(true)]
        public bool ShowLevelUpNotifications = true;

        [DefaultValue(true)]
        public bool ShowMilestoneNotifications = true;

        [DefaultValue(false)]
        public bool ShowDebugInfo = false;

        // ------------------------------------------------------------------
        //  v6.44 — EL MODO PRUEBAS DE LOS LUGARES
        // ------------------------------------------------------------------

        /// <summary>
        /// ¿El Sagrario Hueco baja su puerta de 400 PV al máximo? ON por
        /// defecto: todo el mod es de PRUEBAS y el paisaje del Sagrario
        /// (los fondos de CieloLib) debe poder verse descendiendo al
        /// subsuelo con cualquier jugador de pruebas. Apágalo para
        /// restaurar la puerta real de los 400 PV.
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
        //  v6.47 — LA VOZ DEL HAMBRE Y LA FURIA
        // ------------------------------------------------------------------

        /// <summary>
        /// ¿El grimorio hambriento convoca sus OLEADAS cuando pasa
        /// demasiado tiempo sin comer? ON por defecto (el evento ES
        /// contenido del mod de pruebas). Apágalo para dejar SOLO los
        /// susurros y la barra palidecida (puro sabor, cero castigo).
        /// La Carnada del Grimorio (el ítem de prueba) funciona SIEMPRE,
        /// con la bandera apagada o no.
        /// </summary>
        [DefaultValue(true)]
        public bool EventoHambreGrimorio = true;
    }
}
