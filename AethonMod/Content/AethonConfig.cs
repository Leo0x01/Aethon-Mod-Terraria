using Terraria;
using Terraria.ModLoader.Config;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Input;
using System.ComponentModel;

namespace AethonMod.Content
{
    /// <summary>
    /// Configuracion del mod Aethon.
    /// </summary>
    public class AethonConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        [DefaultValue(Keys.K)]
        public Keys SkillTreeKey = Keys.K;

        [DefaultValue(Keys.J)]
        public Keys CodexKey = Keys.J;

        [DefaultValue(1f)]
        [Range(0.1f, 10f)]
        public float XPMultiplier = 1f;

        [DefaultValue(0)]
        [Range(0, 500)]
        public int MaxShardLevel = 0;

        [DefaultValue(true)]
        public bool EnableCosmicEvents = true;

        [DefaultValue(true)]
        public bool EnableStarlightRain = true;

        [DefaultValue(true)]
        public bool EnableDimensionalRifts = true;

        [DefaultValue(true)]
        public bool ShowLevelUpNotifications = true;

        [DefaultValue(true)]
        public bool ShowMilestoneNotifications = true;

        [DefaultValue(false)]
        public bool ShowDebugInfo = false;
    }
}
