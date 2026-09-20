
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
    }
}
