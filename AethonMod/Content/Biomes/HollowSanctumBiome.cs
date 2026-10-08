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

        // v6.50.76 — LOS FONDOS DE BIOMA FUERON RETIRADOS (CieloLib
        // eliminada): el estilo de SUBSUELO del Sagrario era el culpable
        // del fondo de cuevas roto (metía paisajes 1024×256 en slots que
        // exigen miniaturas 160×16/160×96 y dejaba los slots 3-6 en 0,
        // con el bioma activo en TODAS las cuevas por la bandera de
        // pruebas). El fondo de cuevas vuelve a ser 100% vanilla.

        public override bool IsBiomeActive(Player player)
        {
            // Activo cuando el jugador esta bajo tierra y tiene al menos 400 HP max.
            // v6.44 — MODO PRUEBAS (la petición del usuario: "todo es de
            // pruebas"): la puerta de los 400 PV se abre con la bandera de
            // config (SagrarioAccesibleEnPruebas, ON por defecto) para que
            // el paisaje del Sagrario se pueda VER en cualquier jugador de
            // pruebas sin comerse primero 7 corazones de vida. La puerta
            // real (400 PV) se restaura apagando la bandera — la lógica
            // original queda intacta debajo.
            bool puertaDeVida = player.statLifeMax >= 400;
            var config = ModContent.GetInstance<AethonConfig>();
            if (config != null && config.SagrarioAccesibleEnPruebas)
                puertaDeVida = true;
            return (player.ZoneDirtLayerHeight || player.ZoneRockLayerHeight) &&
                   puertaDeVida;
        }
    }
}

