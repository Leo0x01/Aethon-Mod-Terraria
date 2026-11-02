using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// OleadaHambreMusica — v6.85 — LA BANDA SONORA DE LAS OLEADAS DE
    /// HAMBRE. v6.88 — LAS DOS BANDAS SONORAS.
    ///
    /// LA LETRA: «la banda sonora de la que hiciste y crea otra aun mas
    /// epica, esta nueva banda sonora mas la que ya esta creada, las dos
    /// deben sonar aleatoriamente cuando se activa una oleada».
    ///
    /// EL REPERTORIO (las dos piezas de la casa, 100% síntesis propia):
    ///   · «EL FESTÍN DESATADO» (Music/OleadaHambre.ogg, v6.87): 152
    ///     BPM — el ASALTO: percusión completa, bajo ostinato, cuerno,
    ///     coro de 4, campana inarmónica y el latido del hambre.
    ///   · «LA OLEADA FINAL» (Music/OleadaHambreEpica.ogg, v6.88): 164
    ///     BPM — LA BATALLA CAMPAL: doble clímax con modulación (la
    ///     tercera de picardía de Mi bemol), el coro a OCHO voces, los
    ///     timbales de guerra, la sala épica (reverb de tráiler) y la
    ///     campana tubular del grimorio.
    ///
    /// LA ELECCIÓN: al ACTIVARSE una oleada (el flanco de FestinVisible
    /// falso→verdadero) la moneda elige UNA de las dos — y esa es la
    /// de TODA la oleada (la pieza no cambia a media fiesta: una
    /// oleada, una identidad). La próxima oleada vuelve a tirar la
    /// moneda. EL VEHÍCULO: ModSceneEffect con prioridad BossHigh — el
    /// festín es un jefe a todos los efectos (su música calla la del
    /// bioma mientras dure; el crossfade lo maneja el motor de vanilla).
    /// </summary>
    public class OleadaHambreMusica : ModSceneEffect
    {
        public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;

        /// <summary>¿La última vez que se miró, había festín? (el flanco
        /// de la activación: false→true = la moneda).</summary>
        private static bool _festinVisto;

        /// <summary>La pista elegida para la oleada VIVA (slot de
        /// MusicLoader; 0 antes de la primera oleada).</summary>
        private static int _slotElegido;

        public override int Music
        {
            get
            {
                try
                {
                    bool festin = AmbienteOleadaSistema.FestinVisible;
                    if (festin && !_festinVisto)
                    {
                        // LA MONEDA DE LA OLEADA: una de las dos, al azar
                        // (Main.rand — viaja bien en MP: cada cliente elige
                        // la suya sobre el MISMO festín; el crossfade de
                        // vanilla une la transición).
                        int festin_v1 = MusicLoader.GetMusicSlot(Mod, "Music/OleadaHambre");
                        int epica = MusicLoader.GetMusicSlot(Mod, "Music/OleadaHambreEpica");
                        bool primera = Main.rand.NextBool();
                        _slotElegido = primera ? festin_v1 : epica;
                        if (_slotElegido <= 0)   // (por si un ogg no cargó: la otra)
                            _slotElegido = primera ? epica : festin_v1;
                    }
                    if (!festin && _festinVisto)
                        _slotElegido = 0;        // murió la oleada: la moneda vuelve
                    _festinVisto = festin;
                    if (_slotElegido > 0) return _slotElegido;
                    return MusicLoader.GetMusicSlot(Mod, "Music/OleadaHambre");
                }
                catch { return MusicLoader.GetMusicSlot(Mod, "Music/OleadaHambre"); }
            }
        }

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
