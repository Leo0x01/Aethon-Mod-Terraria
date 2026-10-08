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
    /// v6.50.76 — LOS ASTROS PRESTADOS DE VERDAD.
    ///
    /// La letra del usuario (v6.50.74): «creo que para las oleadas también
    /// seria bueno darles su propio ambiente, que tal si combinas el
    /// ambiente de el eclipse solar mas el ambiente de cementerio original
    /// de terraria, le das un toque morado a la iluminación naranja del
    /// eclipse… eso mientras es de día; si el evento oleada pasa a la
    /// noche… se activa el ambiente que es una mezcla de lunar de sangre
    /// mas el cementerio con el mismo toque morado… el sol debe cambiar su
    /// sprite y en el de noche la luna roja también debe cambiar su
    /// sprite… estos ambientes son dinámicos… se acaban en el momento en
    /// el que la oleada termine».
    ///
    /// La letra del usuario (v6.50.76): «el sprite de sol en eclipse se
    /// ve horrible… debes usar el original del juego con un tinte morado
    /// al igual que en el caso de la luna es lo mismo, el original del
    /// juego con un tinte y siempre respetando el mismo tamaño que los
    /// originales ya que el sol eclipse que creaste es mucho mas grande
    /// que el original».
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
    ///      encienden TODOS solos desde ese campo; los enemigos y la
    ///      música del cementerio real viven en ZoneGraveyard, que
    ///      NADIE toca). El propio Player.Update lo tira hacia 0 cada
    ///      tick — este sistema lo vuelve a levantar DESPUÉS (el
    ///      tira-y-afloja queda en 0.90–0.92, invisible).
    ///   3. LOS ASTROS (v6.50.76 — EL ORIGINAL DEL JUEGO, TEÑIDO):
    ///      cero sprites de la casa. El SOL es EL PROPIO SOL ECLIPSE DE
    ///      VANILLA (TextureAssets.Sun3 — el que el juego dibuja en sus
    ///      eclipses reales, decompile línea 81995) prestado por pura
    ///      REFERENCIA de asset a la ranura TextureAssets.Sun: mismo
    ///      sprite, mismo tamaño, cero texturas nuevas, devolución
    ///      limpia. La LUNA NO SE TOCA — el sprite original (cualquier
    ///      tipo y fase) queda tal cual. El TINTE vive en el hook del
    ///      color de dibujo (Terraria.On_Main.DrawSunAndMoon): sunColor
    ///      y moonColor son LOCALES de SetBackColor que SOLO alimentan
    ///      el dibujo de los astros (verificado en el decompile — el
    ///      cielo y la iluminación NO se enteran), así que lerpearlos
    ///      hacia el morado/carmesí tiñe los astros por la MISMA vía
    ///      que vanilla tiñe el sol del amanecer o la luna de sangre.
    ///
    /// LA DINÁMICA (el reloj manda): el AMANECER y el ATARDECER dentro
    /// de una misma oleada cruzan suave entre los dos ambientes (el
    /// último tramo del día ya cae hacia la noche; la madrugada amanece
    /// hacia el día — el cruce vive en _diaSuave). TODO termina (fade +
    /// sol devuelto + hook inerte) en el mismo tick en que el festín
    /// muere.
    ///
    /// LA RED: el festín es del mundo — el SERVIDOR hace BROADCAST del
    /// estado (EcoRed.MsgAmbienteOleada: 1 byte) al cambiar y un latido
    /// cada 5 s (para quien entra a media fiesta); SP y host lo leen
    /// directo de GrimorioFuriaSistema.Activo (mismo proceso). El hook
    /// de tinte corre en el dibujo de CADA cliente con SU _intensidad.
    /// </summary>
    public class AmbienteOleadaSistema : ModSystem
    {
        // === LOS TINTES DE LOS ASTROS (v6.50.76 — la letra del usuario) ===
        // El SOL ECLIPSE morado: el núcleo se dibuja con sunColor y el
        // resplandor con (255, sunColor.G, sunColor.B) — con el canal B
        // alto el halo respira magenta y el núcleo violeta (y el alpha
        // del núcleo ES sunColor.B en vanilla: B=255 lo deja opaco).
        private static readonly Color TinteSolEclipse = new Color(196, 112, 255);
        // LA LUNA de sangre con el toque morado: carmesí tirando a magenta.
        private static readonly Color TinteLunaOleada = new Color(255, 64, 132);

        // === EL ESTADO ===
        private static bool _clienteFestin;   // MP remoto: el paquete del server
        private static float _intensidad;     // 0..1 — el fade de entrada/salida
        private static float _diaSuave = 1f;  // 1 = ambiente de día, 0 = de noche (cruce suave)
        private static bool _solPuesto;       // ¿el sol eclipse prestado está puesto?
        private static bool _ultimoEnviado;   // server: lo último que se broadcasteó

        // === EL SOL PRESTADO (el original de vanilla se guarda para DEVOLVERLO) ===
        private static Asset<Texture2D> _solOriginal;

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
        //  EL PULSO — el fade, la niebla y el sol prestado (solo pantalla)
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
                DevolverSolPrestado(); // la oleada murió: el sol vuelve a su dueño
                return;
            }

            PonerSolPrestado();

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
        //  EL TINTE DE LOS ASTROS — el hook del dibujo (v6.50.76)
        // ================================================================

        /// <summary>
        /// EL TINTE NATIVO: DrawSunAndMoon dibuja el sol con sunColor y la
        /// luna con moonColor — los DOS son locales de SetBackColor que
        /// SOLO alimentan este dibujo (decompile verificado: el cielo y la
        /// iluminación van por OTRO camino). Lerpearlos aquí tiñe los
        /// astros con la MISMA vía que vanilla tiñe el sol del amanecer o
        /// la luna de sangre — sin tocar texturas, sin tocar tamaños.
        /// </summary>
        private static void TintarAstros(Terraria.On_Main.orig_DrawSunAndMoon orig, Terraria.Main self,
            Main.SceneArea sceneArea, Color moonColor, Color sunColor, float tempMushroomInfluence)
        {
            if (_intensidad > 0f && !Main.gameMenu)
            {
                float k = _intensidad;
                // El sol eclipse de la oleada: el sprite ORIGINAL del juego
                // (Sun3, prestado abajo) teñido de morado; la luna: el
                // sprite ORIGINAL (su tipo y su fase, tal cual el mundo los
                // sorteó) teñido de carmesí-morado.
                sunColor = Color.Lerp(sunColor, TinteSolEclipse, k);
                moonColor = Color.Lerp(moonColor, TinteLunaOleada, k);
            }
            orig(self, sceneArea, moonColor, sunColor, tempMushroomInfluence);
        }

        // ================================================================
        //  EL SOL ECLIPSE PRESTADO (referencia pura entre assets VANILLA)
        // ================================================================

        /// <summary>
        /// EL SOL ECLIPSE ORIGINAL DEL JUEGO: vanilla dibuja Sun3 en sus
        /// eclipses reales (decompile línea 81995); durante la oleada la
        /// ranura Sun pasa a apuntar AL MISMO ASSET — el sprite y el
        /// tamaño son LOS del juego (la letra: «el original del juego…
        /// respetando el mismo tamaño que los originales»). Cero texturas
        /// nuevas, cero clonados: solo una referencia que se devuelve.
        /// </summary>
        private static void PonerSolPrestado()
        {
            if (_solPuesto) return;
            try
            {
                _solOriginal = TextureAssets.Sun;      // el sol de siempre, a la caja
                TextureAssets.Sun = TextureAssets.Sun3; // EL SOL ECLIPSE de vanilla
                _solPuesto = true;
            }
            catch { }
        }

        /// <summary>El sol de siempre vuelve a su ranura (idempotente).</summary>
        private static void DevolverSolPrestado()
        {
            if (!_solPuesto) return;
            try
            {
                if (_solOriginal != null) TextureAssets.Sun = _solOriginal;
            }
            catch { }
            _solPuesto = false;
        }

        // ================================================================
        //  LA CARGA Y LA LIMPIEZA (nada de anclas muertas)
        // ================================================================

        public override void Load()
        {
            // El tinte de los astros: enganchado al dibujo vanilla del
            // sol y la luna (la firma vive en TerrariaHooks — verificado
            // con el decompile: orig_DrawSunAndMoon(Main, SceneArea,
            // Color moonColor, Color sunColor, float)).
            Terraria.On_Main.DrawSunAndMoon += TintarAstros;
        }

        public override void OnWorldUnload()
        {
            _clienteFestin = false;
            _intensidad = 0f;
            _diaSuave = 1f;
            DevolverSolPrestado();
        }

        public override void Unload()
        {
            try
            {
                Terraria.On_Main.DrawSunAndMoon -= TintarAstros;
            }
            catch
            {
                // Programación defensiva: el detach del hook jamás puede
                // impedir que la desactivación del mod continúe (la casa).
            }
            DevolverSolPrestado();
            _solOriginal = null;
            _clienteFestin = false;
            _intensidad = 0f;
            _diaSuave = 1f;
            _ultimoEnviado = false;
        }
    }
}
