using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.Biomes
{
    /// <summary>
    /// El Sagrario Hueco (The Hollow Sanctum) — sub-bioma cristalino
    /// que genera bajo tierra despues de que el jugador tenga 400 HP max.
    /// Contiene el Altar Antiguo con el Fragmento Genesis.
    /// </summary>
    public class HollowSanctumBiome : ModBiome
    {
        // Nota: BestiaryIcon y BackgroundPath removidos porque no existen las texturas.
        // Cuando se anadan los assets, descomentar estas lineas:
        // public override string BestiaryIcon => "AethonMod/Content/Biomes/HollowSanctumBiome_Icon";
        // public override string BackgroundPath => "AethonMod/Content/Biomes/HollowSanctumBiome_Background";

        public override int Music =>
            Terraria.ID.MusicID.Underground;

        public override SceneEffectPriority Priority =>
            SceneEffectPriority.BiomeHigh;

        public override bool IsBiomeActive(Player player)
        {
            // Activo cuando el jugador esta bajo tierra y tiene al menos 400 HP max.
            return (player.ZoneDirtLayerHeight || player.ZoneRockLayerHeight) &&
                   player.statLifeMax >= 400;
        }
    }
}

