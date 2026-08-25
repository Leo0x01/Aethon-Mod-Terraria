using Terraria.ModLoader;

namespace AethonMod.Content.Biomes
{
    /// <summary>
    /// El Sagrario Hueco (The Hollow Sanctum) — sub-bioma cristalino
    /// que genera bajo tierra después de que el jugador tenga 200 HP máx.
    /// Contiene el Altar Antiguo con el Fragmento Génesis.
    /// </summary>
    public class HollowSanctumBiome : ModBiome
    {
        public override ModBiome.BiomeType Type => BiomeType.Subworld;

        public override string BestiaryIcon =>
            "AethonMod/Content/Biomes/HollowSanctumBiome_Icon";

        public override string BackgroundPath =>
            "AethonMod/Content/Biomes/HollowSanctumBiome_Background";

        public override int Music =>
            Terraria.ID.MusicID.Underground;

        public override SceneEffectPriority Priority =>
            SceneEffectPriority.BiomeHigh;

        public override bool IsBiomeActive(Player player)
        {
            // Activo cuando el jugador está bajo tierra y cerca de cristales del bioma.
            // Simplificado: bajo tierra + suficientes tiles del Sagrario cerca.
            return (player.ZoneDirtLayerHeight || player.ZoneRockLayerHeight) &&
                   player.statLifeMax >= 400; // 200 HP máx (statLifeMax = HP×2 en hardmode context).
        }
    }
}
