using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// OleadaHambreMusica — v6.85 — LA BANDA SONORA DE LAS OLEADAS DE
    /// HAMBRE.
    ///
    /// LA LETRA: «¿serías capaz de crear una banda sonora, o sea una
    /// musica instrumental para el evento de oleadas de hambre?».
    ///
    /// LA COMPOSICIÓN (instrumental, generada por la casa — 2:44, 70
    /// BPM, re menor): el pad de acordes i–VI–III–VII (Dm–Bb–F–C, la
    /// progresión oscura clásica) + EL LATIDO DEL HAMBRE (lub-dub grave
    /// cada dos compases — 29/min, pulso dépribido) + arpegios
    /// punteados que despiertan en el compás 16 + LA CAMPANA lejana
    /// (parciales inarmónicos, cada 16 compases) + LA BRUMA (soplo de
    /// ruido filtrado, el mismo aire del festín) + el dron de D2 (la
    /// inquietud que no cesa).
    ///
    /// EL VEHÍCULO: ModSceneEffect con prioridad BossHigh — el festín
    /// es un jefe a todos los efectos: su música calla la del bioma
    /// mientras dure (Music/OleadaHambre.ogg, el crossfade lo maneja
    /// el motor de vanilla).
    /// </summary>
    public class OleadaHambreMusica : ModSceneEffect
    {
        public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;

        public override int Music => MusicLoader.GetMusicSlot(Mod, "Music/OleadaHambre");

        public override bool IsSceneEffectActive(Player player)
        {
            // EL FESTÍN VISIBLE: el mismo estado del ambiente (SP/host:
            // el propio sistema; cliente: el broadcast de EcoRed).
            try
            {
                return AmbienteOleadaSistema.FestinVisible;
            }
            catch { return false; }
        }
    }
}
