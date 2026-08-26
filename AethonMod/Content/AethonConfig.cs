using Terraria;
using Terraria.ModLoader.Config;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Input;
using System.ComponentModel;

namespace AethonMod.Content
{
    /// <summary>
    /// Configuración del mod Aethon.
    /// Permite al jugador personalizar:
    /// - Atajos de teclado (árbol de habilidades, códex)
    /// - Multiplicador de XP
    /// - Activar/desactivar eventos cósmicos
    /// - Nivel máximo del fragmento (0 = infinito)
    /// - Activar/desactivar notificaciones
    /// </summary>
    public class AethonConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        [Header("Atajos de teclado")]
        [DefaultValue(Keys.K)]
        public Keys SkillTreeKey = Keys.K;

        [DefaultValue(Keys.J)]
        public Keys CodexKey = Keys.J;

        [Header("Progresión")]
        [DefaultValue(1f)]
        [Range(0.1f, 10f)]
        public float XPMultiplier = 1f;

        [DefaultValue(0)]
        [Range(0, 500)]
        public int MaxShardLevel = 0;

        [Header("Eventos cósmicos")]
        [DefaultValue(true)]
        public bool EnableCosmicEvents = true;

        [DefaultValue(true)]
        public bool EnableStarlightRain = true;

        [DefaultValue(true)]
        public bool EnableDimensionalRifts = true;

        [Header("Interfaz")]
        [DefaultValue(true)]
        public bool ShowLevelUpNotifications = true;

        [DefaultValue(true)]
        public bool ShowMilestoneNotifications = true;

        [DefaultValue(false)]
        public bool ShowDebugInfo = false;
    }
}
