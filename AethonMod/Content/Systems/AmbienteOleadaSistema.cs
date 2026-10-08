using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent;
using AethonMod.Content.Effects.Bruma;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// AMBIENTEOLEADASISTEMA — v6.50.74 — LA OLEADA ES SU PROPIO CIELO.
    /// v6.50.76 — LOS ASTROS PRESTADOS DE VERDAD.
    /// v6.50.78 — LA LUNA DE SANGRE + EL CROSSFADE + LA BRUMA DEL GRIMORIO.
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
    /// La letra del usuario (v6.50.78): «con respecto al evento de
    /// oleadas, la luna no es la luna roja, solo dejas la luna normal y
    /// no es asi, debe ser el sprite de la luna de sangre, y luego
    /// teñirla de morado lo mismo con el sol, y algo mas, en el momento
    /// en el que comienza el evento tanto el sol como la luna no deben
    /// simplemente cambiar de un frame a otro eso es muy brusco, para
    /// mejorar eso intenta que el cambio sea con un degradado de un
    /// sprite a otro y para mejorarlo aun mas es buena idea hacer que
    /// durante todo el evento una masa de bruma morada y negra crubra
    /// tanto el sol como la luna, ya que este evento es algo creado por
    /// el propio grimorio».
    ///
    /// EL ESTADO NUEVO (ni eclipse real ni cementerio real ni luna de
    /// sangre real): JAMÁS se toca Main.eclipse, Main.bloodMoon ni las
    /// zonas del cementerio — el festín se viste con CUATRO piezas que
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
    ///   3. LOS ASTROS (v6.50.78 — LA LUNA DE SANGRE + EL CROSSFADE):
    ///      el SOL sigue siendo EL SOL ECLIPSE DE VANILLA (Sun3, prestado
    ///      por referencia a la ranura TextureAssets.Sun); la LUNA pasa
    ///      a ser LA LUNA DE SANGRE DE VANILLA — el sprite del mundo
    ///      (cualquier tipo, su fase exacta) con EL COLOR EXACTO que
    ///      vanilla pinta sus lunas de sangre (la rama BloodMoonActive
    ///      de SetBackColor, replicada fórmula a fórmula: R=205, G/B
    ///      respirando con la noche) empujado hacia el morado — porque
    ///      en Terraria LA LUNA ROJA ES LA LUNA DEL MUNDO TEÑIDA (el
    ///      decompile lo demuestra: bloodMoon NO cambia el sprite, solo
    ///      el color), así que esta ES la luna de sangre de verdad.
    ///      NADA DE CORTES: el cambio vive en _transicionAstros (rampa
    ///      lineal ~1.4 s) — el astro de siempre se DESVANECE (el sol
    ///      viejo dibujado encima con alpha 1−t; la luna vanilla con su
    ///      moonColor.A → 0) MIENTRAS el astro de la oleada NACE con
    ///      alpha t: un DEGRADADO de un sprite a otro. Y al morir el
    ///      festín el mismo crossfade DEVUELVE los astros originales
    ///      sin costura.
    ///   4. LA BRUMA DEL GRIMORIO — durante TODO el evento una MASA de
    ///      bruma MORADA y NEGRA cubre el sol y la luna (el festín es
    ///      algo creado por el propio grimorio): la librería de la casa
    ///      (BrumaFX — puffs procedurales con flipbook de ruido, núcleo
    ///      morado + velo negro + satélites que orbitan y respiran)
    ///      dibujada en el MISMO lote del cielo sobre el astro activo.
    ///
    /// LA DINÁMICA (el reloj manda): el AMANECER y el ATARDECER dentro
    /// de una misma oleada cruzan suave entre los dos ambientes (el
    /// último tramo del día ya cae hacia la noche; la madrugada amanece
    /// hacia el día — el cruce vive en _diaSuave). TODO termina (fade +
    /// crossfade devuelto + hook inerte) en el mismo tick en que el
    /// festín muere.
    ///
    /// LA RED: el festín es del mundo — el SERVIDOR hace BROADCAST del
    /// estado (EcoRed.MsgAmbienteOleada: 1 byte) al cambiar y un latido
    /// cada 5 s (para quien entra a media fiesta); SP y host lo leen
    /// directo de GrimorioFuriaSistema.Activo (mismo proceso). El hook
    /// de tinte corre en el dibujo de CADA cliente con SU _intensidad.
    /// </summary>
    public class AmbienteOleadaSistema : ModSystem
    {
        // === LOS TINTES DE LOS ASTROS (v6.50.78 — la letra del usuario) ===
        // El SOL ECLIPSE morado: el núcleo se dibuja con sunColor y el
        // resplandor con (255, sunColor.G, sunColor.B) — con el canal B
        // alto el halo respira magenta y el núcleo violeta (y el alpha
        // del núcleo ES sunColor.B en vanilla: B=255 lo deja opaco).
        private static readonly Color TinteSolEclipse = new Color(196, 112, 255);
        // LA LUNA DE SANGRE con el toque morado: «y luego teñirla de
        // morado lo mismo con el sol» — EL MISMO morado del sol (la
        // base roja la aporta el color de luna de sangre vanilla).
        private static readonly Color TinteLunaOleada = new Color(196, 112, 255);

        // === EL ESTADO ===
        private static bool _clienteFestin;   // MP remoto: el paquete del server
        private static float _intensidad;     // 0..1 — el fade de entrada/salida del CIELO
        private static float _transicionAstros; // 0..1 — EL CROSSFADE de los sprites (~1.4 s lineal)
        private static float _diaSuave = 1f;  // 1 = ambiente de día, 0 = de noche (cruce suave)
        private static bool _solPuesto;       // ¿el sol eclipse prestado está puesto?
        private static bool _ultimoEnviado;   // server: lo último que se broadcasteó

        // === EL SOL PRESTADO (el original de vanilla se guarda para DEVOLVERLO) ===
        private static Asset<Texture2D> _solOriginal;

        // === LOS COLORES DE LA BRUMA DEL GRIMORIO ===
        private static readonly Color BrumaMorada = new Color(96, 34, 140);
        private static readonly Color BrumaNegra = new Color(20, 9, 34);

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
        //  EL PULSO — el fade del cielo y el crossfade de los astros
        // ================================================================

        public override void PostUpdateEverything()
        {
            if (Main.netMode == NetmodeID.Server) return; // el server no dibuja

            // EL FADE DEL CIELO: ~3 s para vestirse / desvestirse (nada de cortes)
            float objetivo = FestinVisible ? 1f : 0f;
            _intensidad += (objetivo - _intensidad) * 0.025f;
            if (_intensidad < 0.005f) _intensidad = 0f;
            if (_intensidad > 0.995f) _intensidad = 1f;

            // EL CROSSFADE DE LOS ASTROS (~1.4 s lineal — la letra: «no
            // deben simplemente cambiar de un frame a otro eso es muy
            // brusco… que el cambio sea con un degradado de un sprite a
            // otro»). Rampa PROPIA, más viva que el fade del cielo: los
            // sprites se cruzan entre sí mientras el cielo hace su lento
            // giro. El préstamo del sol vive ENGANCHADO a este valor (en
            // el hook del dibujo) para que el swap JAMÁS se vea solo.
            float objT = FestinVisible ? 1f : 0f;
            float paso = 1f / 84f; // 60 fps × 1.4 s
            if (_transicionAstros < objT) _transicionAstros = Math.Min(objT, _transicionAstros + paso);
            else if (_transicionAstros > objT) _transicionAstros = Math.Max(objT, _transicionAstros - paso);

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

            if (_intensidad <= 0f && _transicionAstros <= 0f)
            {
                DevolverSolPrestado(); // la oleada murió Y el crossfade cerró: el sol vuelve a su dueño
                return;
            }

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
        //  LOS ASTROS — el hook del dibujo (v6.50.78)
        // ================================================================

        /// <summary>
        /// EL TINTE + EL CROSSFADE + LA BRUMA: DrawSunAndMoon dibuja el
        /// sol con sunColor y la luna con moonColor — los DOS son locales
        /// de SetBackColor que SOLO alimentan este dibujo (decompile
        /// verificado: el cielo y la iluminación van por OTRO camino).
        ///
        /// EL SOL: la ranura Sun ya apunta a Sun3 (el préstamo vive
        /// ENGANCHADO a t&gt;0 — nunca un frame de sol eclipse sin tinte)
        /// y el SOL DE SIEMPRE se dibuja ENCIMA desvaneciéndose (alpha
        /// 1−t): el eclipse morado EMERGE de debajo del sol de siempre.
        ///
        /// LA LUNA: la vanilla se desvanece (moonColor.A → 0) mientras
        /// la LUNA DE SANGRE (el sprite del mundo con el COLOR EXACTO de
        /// las lunas de sangre de vanilla, empujado al morado) nace con
        /// alpha t en el MISMO sitio, con la MISMA fase, el MISMO tamaño
        /// y la MISMA rotación — la fórmula de posición es la EXACTA de
        /// vanilla (decompile 81905-81944), replicada píxel a píxel.
        /// </summary>
        private static void TintarAstros(Terraria.On_Main.orig_DrawSunAndMoon orig, Terraria.Main self,
            Main.SceneArea sceneArea, Color moonColor, Color sunColor, float tempMushroomInfluence)
        {
            float t = _transicionAstros;
            bool activo = t > 0f && !Main.gameMenu;

            // LOS COLORES ORIGINALES (ANTES del tinte): el sol de siempre
            // del crossfade se dibuja con LO QUE HABÍA — la réplica exacta
            // del sol que vanilla habría pintado sin el festín.
            Color sunOriginal = sunColor;
            Color moonOriginal = moonColor;

            if (activo)
            {
                // EL SOL (la ranura YA es Sun3): el tinte morado completo.
                sunColor = Color.Lerp(sunColor, TinteSolEclipse, t);

                // LA LUNA vanilla: se DESVANECE mientras la de sangre nace
                // (el lerp baja el alpha hacia 0 — Color.Lerp tumba los 4 canales).
                Color lunaHacia = TinteLunaOleada;
                lunaHacia.A = 0;
                moonColor = Color.Lerp(moonColor, lunaHacia, t);

                // EL PRÉSTAMO DEL SOL vive aquí (fase de dibujo): puesto
                // SOLO mientras el crossfade está vivo — jamás un swap
                // huérfano visible en el menú o tras la devolución.
                PonerSolPrestado();
            }
            else
            {
                DevolverSolPrestado(); // crossfade cerrado: el sol de siempre, dueño de su ranura
            }

            orig(self, sceneArea, moonColor, sunColor, tempMushroomInfluence);

            if (activo)
                DibujarAstrosDeLaOleada(sceneArea, sunOriginal, tempMushroomInfluence, t);
        }

        // ================================================================
        //  LOS ASTROS DE LA OLEADA — el crossfade y la luna de sangre
        // ================================================================

        /// <summary>
        /// EL POSTFIX (el lote del cielo sigue ABIERTO): el sol de
        /// siempre DESVANECIÉNDOSE sobre el eclipse (el degradado), LA
        /// LUNA DE SANGRE naciendo con su color vanilla replicado, y LA
        /// BRUMA DEL GRIMORIO cubriendo el astro activo. Toda la
        /// matemática de posiciones es la EXACTA de vanilla (decompile
        /// de Main.DrawSunAndMoon, líneas 81905-81944) — el astro de la
        /// oleada vive PÍXEL SOBRE PÍXEL con el que vanilla dibujó.
        /// </summary>
        private static void DibujarAstrosDeLaOleada(Main.SceneArea sceneArea,
            Color sunColor, float tempMushroomInfluence, float t)
        {
            try
            {
                if (Main.dayTime)
                {
                    // ============== EL SOL (el degradado) ==============
                    // El remix world (drunk) NO dibuja el sol de día
                    // (decompile 81977) — el crossfade tampoco.
                    if (Main.remixWorld || WorldGen.remixWorldGen) return;

                    Texture2D solAhora = TextureAssets.Sun.Value; // con el préstamo YA es Sun3
                    Texture2D solViejo = _solOriginal?.Value;     // el sol de siempre
                    int w = solAhora.Width;

                    // LA POSICIÓN (decompile 81905-81926, EXACTO):
                    int num3 = (int)(Main.time / 54000.0 * (sceneArea.totalWidth + w * 2f)) - w;
                    double parab;
                    if (Main.time < 27000.0)
                        parab = Math.Pow(1.0 - Main.time / 54000.0 * 2.0, 2.0);
                    else
                        parab = Math.Pow((Main.time / 54000.0 - 0.5) * 2.0, 2.0);
                    int num4 = (int)(sceneArea.bgTopY + parab * 250.0 + 180.0);
                    float escala = (float)(1.2 - parab * 0.4) * Main.ForcedMinimumZoom * 1.1f;
                    float rot = (float)(Main.time / 54000.0) * 2f - 7.3f;
                    Vector2 pos = new Vector2(num3, num4 + Main.sunModY) + sceneArea.SceneLocalScreenPositionOffset;

                    // EL SOL DE SIEMPRE ENCIMA, DESVANECIÉNDOSE (alpha 1−t):
                    // las DOS pasadas de vanilla (el resplandor val3 y el
                    // núcleo val4, decompile 81988-81991) con los colores
                    // ORIGINALES (los de antes del tinte) escalados por
                    // (1−t) — en t=0 es EL SOL DE SIEMPRE idéntico; en
                    // t=1 se disolvió del todo y solo queda el eclipse.
                    if (t < 1f && solViejo != null)
                    {
                        float num13 = 1f - tempMushroomInfluence - Main.cloudAlpha * 1.5f * Main.atmo;
                        if (num13 < 0f) num13 = 0f;
                        float a = 1f - t;
                        Color resplandor = new Color(
                            (byte)(255f * num13 * a),
                            (byte)(sunColor.G * num13 * a),
                            (byte)(sunColor.B * num13 * a),
                            (byte)(255f * num13 * a));
                        Color nucleo = new Color(
                            (byte)(sunColor.R * num13 * a),
                            (byte)(sunColor.G * num13 * a),
                            (byte)(sunColor.B * num13 * a),
                            (byte)(sunColor.B * num13 * a));
                        Vector2 origen = solViejo.Size() / 2f;
                        Main.spriteBatch.Draw(solViejo, pos, null, resplandor, rot, origen, escala, SpriteEffects.None, 0f);
                        Main.spriteBatch.Draw(solViejo, pos, null, nucleo, rot, origen, escala, SpriteEffects.None, 0f);
                    }

                    // LA BRUMA DEL GRIMORIO sobre el sol.
                    DibujarBrumaDelAstro(pos, solAhora.Width * 0.5f * escala, t);
                }
                else
                {
                    // ============== LA LUNA DE SANGRE ==============
                    int tipo = Main.moonType; // la MISMA luna del mundo — así
                    if (tipo < 0 || tipo > 8) tipo = 8; // se ven las lunas de sangre de vanilla
                    Texture2D luna = TextureAssets.Moon[tipo].Value;
                    int w = luna.Width;

                    // LA POSICIÓN (decompile 81909-81941, EXACTO):
                    int num7 = (int)(Main.time / 32400.0 * (sceneArea.totalWidth + w * 2f)) - w;
                    double parab;
                    if (Main.time < 16200.0)
                        parab = Math.Pow(1.0 - Main.time / 32400.0 * 2.0, 2.0);
                    else
                        parab = Math.Pow((Main.time / 32400.0 - 0.5) * 2.0, 2.0);
                    int num8 = (int)(sceneArea.bgTopY + parab * 250.0 + 180.0);
                    float escala = (float)(1.2 - parab * 0.4) * Main.ForcedMinimumZoom;
                    float rot = (float)(Main.time / 32400.0) * 2f - 7.3f;
                    Vector2 pos = new Vector2(num7, num8 + Main.moonModY) + sceneArea.SceneLocalScreenPositionOffset;

                    // LA LUNA DE SANGRE: el sprite del mundo (SU tipo, SU
                    // fase — el sub-rectángulo vertical de vanilla) con EL
                    // COLOR EXACTO de las lunas de sangre vanilla (la rama
                    // BloodMoonActive de SetBackColor replicada) empujado
                    // hacia el morado (la letra: «y luego teñirla de morado
                    // lo mismo con el sol»), naciendo con alpha t mientras
                    // la vanilla se desvanece debajo — EL DEGRADADO.
                    float nubes = 1f - Main.cloudAlpha * 1.5f * Main.atmo;
                    if (nubes < 0f) nubes = 0f;
                    Color color = Color.Lerp(ColorLunaDeSangreVanilla(), TinteLunaOleada, 0.55f);
                    color *= t * nubes; // RGB y alpha escalados (premultiplicado — el estilo de la casa)
                    Rectangle frame = new Rectangle(0, w * Main.moonPhase, w, w);
                    Main.spriteBatch.Draw(luna, pos, frame, color, rot,
                        new Vector2(w / 2f, w / 2f), escala, SpriteEffects.None, 0f);

                    // LA BRUMA DEL GRIMORIO sobre la luna.
                    DibujarBrumaDelAstro(pos, w * 0.5f * escala, t);
                }
            }
            catch
            {
                // Programación defensiva: el dibujo del cielo JAMÁS puede
                // tumbar el frame (la casa).
            }
        }

        /// <summary>
        /// EL COLOR DE LA LUNA DE SANGRE DE VANILLA — la rama
        /// BloodMoonActive de SetBackColor (decompile 82756-82780)
        /// replicada fórmula a fórmula: R=205 fijo, G y B RESPIRANDO con
        /// la noche (55→225/255 en el anochecer, el rojo más puro a
        /// medianoche, y de vuelta). En Terraria la luna de sangre ES la
        /// luna del mundo con ESTE color — replicarlo es tener LA LUNA
        /// DE SANGRE de verdad.
        /// </summary>
        private static Color ColorLunaDeSangreVanilla()
        {
            float f = Main.time < 16200.0
                ? (float)(1.0 - Main.time / 16200.0)
                : (float)((Main.time / 32400.0 - 0.5) * 2.0);
            return new Color(
                205,
                (byte)MathHelper.Clamp(f * 170f + 55f, 0f, 255f),
                (byte)MathHelper.Clamp(f * 200f + 55f, 0f, 255f));
        }

        // ================================================================
        //  LA BRUMA DEL GRIMORIO — la masa que cubre los astros
        // ================================================================

        /// <summary>
        /// UNA MASA DE BRUMA MORADA Y NEGRA cubriendo el astro activo
        /// durante TODO el evento (la letra: «ya que este evento es algo
        /// creado por el propio grimorio»). La librería de la casa
        /// (BrumaFX — puffs procedurales de ruido con flipbook): UN
        /// NÚCLEO morado que cubre el astro, EL VELO negro de la tinta
        /// del grimorio algo descentrado, y CINCO SATÉLITES que orbitan
        /// y respiran alrededor — la masa VIVE (se retuerce, se
        /// desgarra y vuelve a juntarse) sobre el sol eclipse y la luna
        /// de sangre.
        /// </summary>
        private static void DibujarBrumaDelAstro(Vector2 centro, float radio, float t)
        {
            float tiempo = Main.GlobalTimeWrappedHourly;

            // EL NÚCLEO — la masa morada que CUBRE el astro.
            BrumaFX.Puff(centro, radio * 1.15f, BrumaMorada, 4711, tiempo,
                alpha: 0.52f * t, quality: 0.8f);

            // EL VELO — la tinta negra del grimorio, algo descentrada (respira aparte).
            BrumaFX.Puff(centro + new Vector2(radio * 0.3f, -radio * 0.18f),
                radio * 0.9f, BrumaNegra, 8231, tiempo + 37f,
                alpha: 0.46f * t, quality: 0.7f);

            // LOS SATÉLITES — la masa que orbita: anillo elíptico de puffs
            // morados y negros que crece y mengua DESFASADO (nunca en coro).
            for (int i = 0; i < 5; i++)
            {
                float ang = tiempo * 0.10f + i * MathHelper.TwoPi / 5f;
                float r = radio * (1.1f + 0.3f * (float)Math.Sin(tiempo * 0.4f + i * 2.1f));
                Vector2 p = centro + new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang) * 0.75f) * r;
                BrumaFX.Puff(p, radio * 0.6f, (i & 1) == 0 ? BrumaMorada : BrumaNegra,
                    900 + i * 53, tiempo + i * 11f, alpha: 0.32f * t, quality: 0.5f);
            }
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
        /// v6.50.78: el préstamo se pone EN EL HOOK del dibujo, atado al
        /// crossfade — jamás un frame de sol eclipse sin su velo.
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
            // El tinte + el crossfade + la bruma: enganchado al dibujo
            // vanilla del sol y la luna (la firma vive en TerrariaHooks —
            // verificado con el decompile: orig_DrawSunAndMoon(Main,
            // SceneArea, Color moonColor, Color sunColor, float)).
            Terraria.On_Main.DrawSunAndMoon += TintarAstros;
        }

        public override void OnWorldUnload()
        {
            _clienteFestin = false;
            _intensidad = 0f;
            _transicionAstros = 0f;
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
            _transicionAstros = 0f;
            _diaSuave = 1f;
            _ultimoEnviado = false;
        }
    }
}
