using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// AMBIENTEOLEADASISTEMA — v6.50.74 — LA OLEADA ES SU PROPIO CIELO.
    ///
    /// La letra del usuario: «creo que para las oleadas también seria
    /// bueno darles su propio ambiente, que tal si combinas el ambiente
    /// de el eclipse solar mas el ambiente de cementerio original de
    /// terraria, le das un toque morado a la iluminación naranja del
    /// eclipse y además conviertes esto en un nuevo estado por lo tanto
    /// no tiene nada que ver ni con el eclipse solar verdadero ni con
    /// el cementerio real… eso mientras es de día; si el evento oleada
    /// pasa a la noche… se activa el ambiente que es una mezcla de lunar
    /// de sangre mas el cementerio con el mismo toque morado… el sol
    /// debe cambiar su sprite y en el de noche la luna roja también
    /// debe cambiar su sprite… estos ambientes son dinámicos… se
    /// activa al atardecer y cae la noche entonces esa noche es la
    /// noche especial… estos ambientes se acaban en el momento en el
    /// que la oleada termine».
    ///
    /// EL ESTADO NUEVO (ni eclipse real ni cementerio real ni luna de
    /// sangre real): JAMÁS se toca Main.eclipse, Main.bloodMoon ni las
    /// zonas del cementerio — el festín se viste con TRES piezas que
    /// viven SOLO en la pantalla:
    ///   1. LA LUZ — ModSystem.ModifySunLightColor (el hook oficial que
    ///      tinta el cielo (ColorOfTheSkies) Y la luz de los tiles en el
    ///      mismo pase): DE DÍA, eclipse + cementerio + toque morado
    ///      (naranja quemado hacia violeta); DE NOCHE, luna de sangre +
    ///      cementerio + el mismo morado (carmesí hacia violeta).
    ///   2. LA NIEBLA DEL CEMENTERIO — Main.GraveyardVisualIntensity
    ///      (verificado en el decompile: es SOLO visual — el filtro
    ///      "Graveyard" (niebla), el oscurecimiento del cielo, las
    ///      estrellas apagadas y el RELÁMPAGO esporádico (≥0.9) se
    ///      activan TODOS solos desde ese campo; los enemigos y la
    ///      música del cementerio real viven en ZoneGraveyard, que
    ///      NADIE toca). El propio Player.Update lo tira hacia 0 cada
    ///      tick — este sistema lo vuelve a levantar DESPUÉS (el
    ///      tira-y-afloja queda en 0.90–0.92, invisible).
    ///   3. LOS ASTROS — se PRESTAN las ranuras de vanilla (swap de
    ///      TextureAssets.Sun/Sun2/Sun3 y las 9 de TextureAssets.Moon)
    ///      con DOS sprites de la casa: EL SOL ECLIPSADO (disco morado
    ///      oscuro con corona naranja de fuego — el sol de la oleada)
    ///      de día y LA LUNA CARMESÍ (la luna roja con cráteres y halo
    ///      violeta, con sus 8 fases como vanilla) de noche. El swap es
    ///      reversible y se devuelve al terminar (y en Unload — nada de
    ///      anclas muertas).
    ///
    /// LA DINÁMICA (el reloj manda): el AMANECER y el ATARDECER dentro
    /// de una misma oleada cruzan suave entre los dos ambientes (el
    /// último tramo del día ya cae hacia la noche; la madrugada amanece
    /// hacia el día — el cruce vive en _diaSuave) — «se activa al
    /// atardecer y cae la noche entonces esa noche es la noche
    /// especial». TODO termina (fade + astros devueltos) en el mismo
    /// tick en que el festín muere.
    ///
    /// LA RED: el festín es del mundo — el SERVIDOR hace BROADCAST del
    /// estado (EcoRed.MsgAmbienteOleada: 1 byte) al cambiar y un latido
    /// cada 5 s (para quien entra a media fiesta); SP y host lo leen
    /// directo de GrimorioFuriaSistema.Activo (mismo proceso).
    /// </summary>
    public class AmbienteOleadaSistema : ModSystem
    {
        // === LAS DOS PIEZAS DEL CIELO (rutas de las texturas de la casa) ===
        private const string RutaSol = "AethonMod/Content/Ambientes/SolDeLaOleada";
        private const string RutaLuna = "AethonMod/Content/Ambientes/LunaDeLaOleada";

        // === EL ESTADO ===
        private static bool _clienteFestin;   // MP remoto: el paquete del server
        private static float _intensidad;     // 0..1 — el fade de entrada/salida
        private static float _diaSuave = 1f;  // 1 = ambiente de día, 0 = de noche (cruce suave)
        private static bool _texturasPuestas; // ¿los astros prestados están puestos?
        private static bool _ultimoEnviado;   // server: lo último que se broadcasteó

        // === LOS ASTROS (los originales se guardan para DEVOLVERLOS) ===
        private static Asset<Texture2D> _solNuestro, _lunaNuestra;
        private static Asset<Texture2D> _solOriginal, _sol2Original, _sol3Original;
        private static readonly Asset<Texture2D>[] _lunaOriginal = new Asset<Texture2D>[9];

        /// <summary>¿El festín es visible para ESTA máquina? (SP/host: el
        /// propio sistema; cliente remoto: el broadcast).</summary>
        public static bool FestinVisible =>
            Main.netMode == NetmodeID.MultiplayerClient
                ? _clienteFestin
                : GrimorioFuriaSistema.Activo;

        // ================================================================
        //  LA RED — el festín es del mundo
        // ================================================================

        /// <summary>El servidor broadcastea el estado al cambiar (y un
        /// latido periódico — quien entra a media fiesta lo recibe).</summary>
        public override void PostUpdateWorld()
        {
            if (Main.netMode != NetmodeID.Server) return; // SP: mismo proceso
            bool activo = GrimorioFuriaSistema.Activo;
            if (activo != _ultimoEnviado || (activo && Main.GameUpdateCount % 300u == 0u))
            {
                _ultimoEnviado = activo;
                try
                {
                    ModPacket p = AethonMod.Instance.GetPacket();
                    p.Write(EcoRed.MsgAmbienteOleada);
                    p.Write(activo);
                    p.Send(); // broadcast: TODOS los clientes
                }
                catch { }
            }
        }

        /// <summary>La recepción del broadcast (vía EcoRed — solo clientes remotos).</summary>
        public static void Recibir(bool activo) => _clienteFestin = activo;

        // ================================================================
        //  EL PULSO — el fade, la niebla y los astros (solo pantalla)
        // ================================================================

        public override void PostUpdateEverything()
        {
            if (Main.netMode == NetmodeID.Server) return; // el server no dibuja

            // EL FADE: ~3 s para vestirse / desvestirse (nada de cortes)
            float objetivo = FestinVisible ? 1f : 0f;
            _intensidad += (objetivo - _intensidad) * 0.025f;
            if (_intensidad < 0.005f) _intensidad = 0f;
            if (_intensidad > 0.995f) _intensidad = 1f;

            // LA DINÁMICA DEL RELOJ: el último tramo del día (16:30→19:30)
            // ya cae hacia la noche y la madrugada (4:00→4:30) amanece —
            // el cruce entre los DOS ambientes es suave dentro de una
            // MISMA oleada («esa noche es la noche especial»). Sin festín
            // el cruce SIGUE al reloj directo (así una oleada que nazca
            // de NOCHE empieza ya vestida de noche, sin cruzar colores).
            float diaBruto;
            if (Main.dayTime)
                diaBruto = MathHelper.Clamp((float)(1.0 - (Main.time - 48000.0) / 6000.0), 0f, 1f);
            else
                diaBruto = MathHelper.Clamp((float)((Main.time - 30000.0) / 2400.0), 0f, 1f);
            if (_intensidad <= 0f) _diaSuave = diaBruto;   // sin ambiente: pegado al reloj
            else _diaSuave += (diaBruto - _diaSuave) * 0.012f;  // con ambiente: cruce suave

            if (_intensidad <= 0f)
            {
                RestaurarTexturas(); // la oleada murió: los astros vuelven
                return;
            }

            PonerTexturas();

            // LA NIEBLA DEL CEMENTERIO (visual): el filtro, la oscuridad,
            // las estrellas apagadas y el relámpago esporádico — TODO se
            // enciende solo desde ESTE campo (decompile verificado). El
            // Player.Update lo tira a 0 cada tick; aquí se levanta otra
            // vez (el ambiente vive entre el update y el draw).
            float niebla = _intensidad * 0.92f;
            if (Main.GraveyardVisualIntensity < niebla)
                Main.GraveyardVisualIntensity = niebla;
        }

        // ================================================================
        //  LA LUZ — el hook oficial del cielo
        // ================================================================

        public override void ModifySunLightColor(ref Color tileColor, ref Color backgroundColor)
        {
            if (Main.netMode == NetmodeID.Server || _intensidad <= 0f) return;

            float k = _intensidad;
            float d = _diaSuave;

            // DÍA — ECLIPSE + CEMENTERIO + TOQUE MORADO: naranja quemado
            // hacia el violeta (la letra: «un toque morado a la
            // iluminación naranja del eclipse»).
            Color cieloDia = new Color(118, 48, 92);
            Color luzDia = new Color(172, 104, 146);
            // NOCHE — LUNA DE SANGRE + CEMENTERIO + EL MISMO MORADO:
            // carmesí profundo hacia el violeta.
            Color cieloNoche = new Color(54, 12, 48);
            Color luzNoche = new Color(122, 30, 84);

            Color cielo = Color.Lerp(cieloNoche, cieloDia, d);
            Color luz = Color.Lerp(luzNoche, luzDia, d);

            // el cielo (ColorOfTheSkies — llega por ref) y la luz de los
            // tiles en el MISMO pase: el mundo entero se viste a la vez.
            backgroundColor = Color.Lerp(backgroundColor, cielo, 0.80f * k);
            tileColor = Color.Lerp(tileColor, luz, 0.62f * k);
        }

        // ================================================================
        //  LOS ASTROS PRESTADOS (swap reversible)
        // ================================================================

        private static void PonerTexturas()
        {
            if (_texturasPuestas) return;
            try
            {
                _solNuestro ??= ModContent.Request<Texture2D>(RutaSol, AssetRequestMode.ImmediateLoad);
                _lunaNuestra ??= ModContent.Request<Texture2D>(RutaLuna, AssetRequestMode.ImmediateLoad);

                // los originales a la caja (para devolverlos ÍNTEGROS)
                _solOriginal = TextureAssets.Sun;
                _sol2Original = TextureAssets.Sun2;
                _sol3Original = TextureAssets.Sun3;
                for (int i = 0; i < _lunaOriginal.Length && i < TextureAssets.Moon.Length; i++)
                    _lunaOriginal[i] = TextureAssets.Moon[i];

                // el SOL ECLIPSADO en las tres ranuras (Sun2/Sun3 son las
                // variantes de vanilla: el eclipse real y el sol de
                // gafas — durante la oleada TODOS son el nuestro)
                TextureAssets.Sun = _solNuestro;
                TextureAssets.Sun2 = _solNuestro;
                TextureAssets.Sun3 = _solNuestro;
                // LA LUNA CARMESÍ en las 9 variantes de vanilla (el sorteo
                // de moonType no importa: TODAS son la luna de la oleada)
                for (int i = 0; i < TextureAssets.Moon.Length && i < 9; i++)
                    TextureAssets.Moon[i] = _lunaNuestra;

                _texturasPuestas = true;
            }
            catch { }
        }

        private static void RestaurarTexturas()
        {
            if (!_texturasPuestas) return;
            try
            {
                if (_solOriginal != null) TextureAssets.Sun = _solOriginal;
                if (_sol2Original != null) TextureAssets.Sun2 = _sol2Original;
                if (_sol3Original != null) TextureAssets.Sun3 = _sol3Original;
                if (_lunaOriginal[0] != null)
                    for (int i = 0; i < TextureAssets.Moon.Length && i < _lunaOriginal.Length; i++)
                        TextureAssets.Moon[i] = _lunaOriginal[i];
            }
            catch { }
            _texturasPuestas = false;
        }

        // ================================================================
        //  LA LIMPIEZA (nada de anclas muertas — la lección del barrendero)
        // ================================================================

        public override void OnWorldUnload()
        {
            _clienteFestin = false;
            _intensidad = 0f;
            _diaSuave = 1f;
            RestaurarTexturas();
        }

        public override void Unload()
        {
            RestaurarTexturas();
            _solNuestro = null;
            _lunaNuestra = null;
            _solOriginal = null;
            _sol2Original = null;
            _sol3Original = null;
            for (int i = 0; i < _lunaOriginal.Length; i++) _lunaOriginal[i] = null;
            _clienteFestin = false;
            _intensidad = 0f;
            _diaSuave = 1f;
            _ultimoEnviado = false;
        }
    }
}
