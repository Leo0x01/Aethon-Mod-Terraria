using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Effects;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.IO;
using Terraria.Localization;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.Social;
using Terraria.Utilities;
using Terraria.UI;
using Terraria.WorldBuilding;
using AethonMod.Content.VFX;
using AethonMod.Content.Effects.Bruma;
using static Terraria.ModLoader.ModContent;

namespace AethonMod.Content.Systems
{
    // ======================================================================
    //  SubmundoGrimorio — v6.118 — EL MUNDO INTERIOR DEL GRIMORIO ETERNO:
    //  LA SUPERFICIE DEL CUERPO DEL DIOS, EN ECLIPSE PERMANENTE Y BAJO EL
    //  CICLO ETERNO DE LAS OLEADAS DEL HAMBRE.
    //
    //  LA LETRA DEL REWORK (v6.118): «el nuevo mundo no es lo que pedí,
    //  primero tiene que estar en la superficie, luego debe estar en
    //  eclipse permanente y debe ser el de las oleadas de hambre, luego
    //  no debe tener circulos planos… el fondo del mundo debe ser un
    //  mundo de tentaculos por capas y oscuridad por capas, ademas de
    //  brumas y particulas» + el cuestionario: terreno CONTINUO de
    //  superficie, ciclo automático de oleadas, enemigos de casa, la
    //  MEZCLA de materiales (amatista/oro + vetas del tile forjado), el
    //  ojo colosal FUERA (los ojos del cielo lo reemplazan — TODOS te
    //  miran) y TODAS las brumas (paralaje · columnas · volutas ·
    //  reactiva).
    //
    //  LO QUE VIVE AHORA (v6.118):
    //   · LA SUPERFICIE: terreno CONTINUO de borde a borde (colinas de
    //     AmethystGemspark con corteza GoldBrick — el filo de la
    //     página), la PLAZA plana al centro con el prisma, LAS VETAS de
    //     LA PÁGINA DEL ETERNO (el tile forjado — el oro del veredicto
    //     corriéndole por la carne) y EL CORAZÓN de amatista bajo la
    //     plaza (su LATIDO pulsa TODO el mundo: el cielo, la oscuridad
    //     y las brumas).
    //   · EL ECLIPSE PERMANENTE: sol Sun3 clavado en el cenit + tinte
    //     morado + niebla de cementerio — y durante el festín, EL TINTE
    //     DE LA OLEADA manda (el eclipse morado de las oleadas de
    //     hambre, cedido suave).
    //   · EL CICLO ETERNO: cada ~2,5 min de calma el libro SIRVE una
    //     oleada (GrimorioFuriaSistema.Provocar — los enemigos DE LA
    //     CASA escalados por el nivel del portador), en bucle eterno.
    //   · EL FONDO: CieloInteriorEterno (el CustomSky de la casa) — las
    //     TRES CAPAS de tentáculos con la Piel del Eterno de la Forja,
    //     los ojos que te siguen, el polvo estelar y las brumas del
    //     astro. EL VELO de abajo viste la OSCURIDAD POR CAPAS con
    //     BrumaFX real (CERO círculos planos — la letra) y las motas
    //     son partículas de casa (CERO Dust de vanilla).
    //
    //  LO QUE SE CONSERVA (si funciona, no se toca): el motor ENTRAR/
    //  SALIR completo (el ciclo SubworldLibrary probado y blindado), el
    //  cerrojo de guardado, LA LEY DE LAS SECCIONES del minimapa
    //  (800×450), el préstamo idempotente del sol, la red de caída, el
    //  faro de la devoración en OnPostDraw y su contrato de lote, y la
    //  música.
    //
    //  DE MOMENTO (la letra): SP solamente — en multijugador el altar
    //  explica que el interior solo se abre en mundo de un jugador.
    // ======================================================================
    public class SubmundoGrimorio : ModSystem
    {
        // === EL TAMAÑO DE LA CASA: 800×450 — LA LEY DE LAS SECCIONES
        // (v6.117, leída en el IL del tML 2026.9.3.1 real: el minimapa
        // camina el mundo en SECCIONES DE 200×150 TILES y TODOS los
        // mundos de vanilla son múltiplos exactos — 800 = 4×200 EXACTO
        // y 450 = 3×150 EXACTO: el interior nace con secciones
        // PERFECTAS; NI UN TILE menos ni uno más). ===
        private const int Ancho = 800;
        private const int Alto = 450;
        // v6.118 — LAS LÍNEAS DEL TERRENO CONTINUO: las colinas de la
        // superficie viven entre y≈95 y y≈145 (la PLAZA plana con su
        // primer tile sólido en PlazaY−7 = 113); worldSurface = 150
        // queda BAJO el terreno (el portador de pie vive EN LA ZONA DEL
        // CIELO — el CustomSky del cuerpo se ve de verdad; el interior
        // viejo enterraba la plaza 58 tiles bajo la superficie: el cielo
        // NUNCA existió a la vista) y rockLayer = 270 abre la zona de
        // la carne profunda (las paredes de la profundidad mandan).
        private const int Superficie = 150;
        private const int Roca = 270;
        private const int PlazaX = 400;
        private const int PlazaY = 120;   // la plaza: su roca alta es PlazaY−7

        /// <summary>¿Estamos DENTRO del mundo interior?</summary>
        public static bool Activo;

        /// <summary>¿Hay una transición en marcha? (no se re-entra a mitad)</summary>
        public static bool Transicion;

        private static WorldFileData _mundoPrincipal;
        private static Vector2 _posRetorno;

        // === v6.115 — EL FARO DE BRUMA (la inundación de la DEVORACIÓN):
        // mientras sube (0→1) el mundo real se LLENA de bruma — la
        // transición vive escondida detrás (la letra: «en el momento en
        // que es devorado el mundo se llena de bruma solo en la
        // animacion»). Lo pinta EL VELO — también en el mundo real. ===
        public static float BrumaFaro;

        // === v6.115 — LA REVELACIÓN (la llegada): el interior nace
        // LLENO de bruma y esta SE DISIPA revelando cómo es el mundo
        // nuevo (la letra). Revelado cuenta los ticks que le quedan a
        // la niebla de la llegada. ===
        public const int ReveladoMax = 260;
        public static int Revelado;

        // === v6.115 — EL SOL DEL ECLIPSE PRESTADO (la misma técnica de
        // la oleada de hambre: el Sun3 de vanilla es EL SOL ECLIPSE —
        // clavado en el cenit del interior mientras este viva) ===
        private static Asset<Texture2D> _solOriginal;
        private static bool _solPuesto;

        // ================================================================
        //  v6.118 — LA VIDA NUEVA DEL CUERPO DEL DIOS
        // ================================================================

        /// <summary>EL LATIDO DEL CORAZÓN de amatista (0..1 — sube como
        /// una ola cada ~5 s): el cielo de tentáculos, la OSCURIDAD por
        /// capas y las brumas del interior se hinchan a cada golpe — el
        /// cuerpo del dios RESPIRA (lo leen CieloInteriorEterno, el velo
        /// y las columnas de bruma).</summary>
        public static float PulsoCorazon;

        /// <summary>LAS VENTANAS DE VIDA del terreno (posiciones de MUNDO
        /// donde la carne respira: LAS COLUMNAS de bruma ascienden y las
        /// MOTES DE ORO nacen — las genera GenerarInterior, siempre las
        /// mismas: el interior se regenera idéntico).</summary>
        private static Vector2[] _vents;

        /// <summary>El reloj de calma del CICLO ETERNO de oleadas (cuenta
        /// hacia CalmaTicks entre festines).</summary>
        private static int _ticksCalma;

        /// <summary>¿Cuántos festines ha servido el libro dentro? (la
        /// memoria del ciclo — el apetito del dios crece mientras el
        /// portador se queda).</summary>
        private static int _ciclosFestin;

        /// <summary>El ciclo de calma entre oleadas (~2,5 min — el
        /// cuestionario: «2-3 min de calma y luego una oleada, en bucle
        /// eterno»).</summary>
        private const int CalmaTicks = 60 * 60 * 5 / 2;   // 9000 t ≈ 2,5 min

        /// <summary>EL CEDIMIENTO DEL TINTE (0..1): cuando el festín vive
        /// dentro, EL ECLIPSE DE LA OLEADA manda (AmbienteOleadaSistema
        // — «ya sabes el eclipse de las oleadas de hambre»); el tinte
        /// propio del interior se apaga SUAVE (lerp ~2 s, nada de
        /// cortes).</summary>
        private static float _tinteCedido;

        // === EL VELO (la bruma que cubre el interior) ===
        private static Texture2D _softGlow;
        private static bool _veloProbeFallos;

        public override void Load()
        {
            Activo = false;
            Transicion = false;
            _mundoPrincipal = null;
            _veloProbeFallos = false;
            // v6.114 — EL CERROJO DEL GUARDADO: mientras el interior vive,
            // NINGÚN WorldFile.SaveWorld corre (ni autosave, ni salir al
            // menú, ni nada) — el archivo del mundo real queda INTACTO.
            Terraria.IO.On_WorldFile.SaveWorld_bool_bool += BloquearGuardado;
            // v6.117 — EL FARO DE LA DEVORACIÓN VIVE EN OnPostDraw: es el
            // ÚNICO punto del frame que pinta TAMBIÉN en el menú (el IL
            // de Main.Draw_Inner: DoDraw completa TODO — mundo, interfaz
            // o MENÚ — y recién entonces dispara OnPostDraw). La
            // inundación de la v6.115 vivía en la capa de interfaz, que
            // NO corre en gameMenu: entre el trago y el interior quedaba
            // un VACÍO de pantalla de carga visible. Ahora la bruma tapa
            // el mundo, la interfaz Y la carga — un solo trago continuo.
            Main.OnPostDraw += PintarFaroDevoracion;
        }

        public override void Unload()
        {
            Terraria.IO.On_WorldFile.SaveWorld_bool_bool -= BloquearGuardado;
            Main.OnPostDraw -= PintarFaroDevoracion;
            Activo = false;
            Transicion = false;
            _mundoPrincipal = null;
            _softGlow = null;
            BrumaFaro = 0f;
            Revelado = 0;
            // v6.118 — la vida nueva también muere con la descarga
            PulsoCorazon = 0f;
            _vents = null;
            _ticksCalma = 0;
            _ciclosFestin = 0;
            _tinteCedido = 0f;
            DevolverSolEclipse();
        }

        private static void BloquearGuardado(Terraria.IO.On_WorldFile.orig_SaveWorld_bool_bool orig, bool useCloud, bool resetTime)
        {
            // EL INTERIOR NO SE GUARDA: se REGENERA en cada visita — el
            // libro siempre está igual que la última vez que soñaste.
            if (Activo) return;
            orig(useCloud, resetTime);
        }

        // ==================================================================
        //  LAS PUERTAS — entrar por el altar (con el Grimorio del Eterno
        //  en la mano) y salir por el altar del interior.
        // ==================================================================

        /// <summary>
        /// ¿El jugador puede entrar? (SP solamente, de momento — la letra:
        /// «de momento se puede entrar usando el altar».)
        /// </summary>
        public static bool PuedeEntrar(Player player, out string razon)
        {
            razon = null;
            if (Main.netMode != 0)
            {
                razon = Language.GetTextValue("Mods.AethonMod.Submundo.SoloSP");
                return false;
            }
            if (Activo || Transicion)
            {
                razon = Language.GetTextValue("Mods.AethonMod.Submundo.YaDentro");
                return false;
            }
            return true;
        }

        /// <summary>
        /// ENTRAR AL INTERIOR — llamada desde el altar (con el Grimorio del
        /// Eterno en la mano) o donde la casa lo decida.
        /// </summary>
        public static void Entrar(Player player)
        {
            string razon;
            if (!PuedeEntrar(player, out razon))
            {
                if (player != null && player.whoAmI == Main.myPlayer && razon != null)
                    Main.NewText(razon, new Color(190, 160, 220));
                return;
            }
            // v6.118 — EL FESTÍN NO CRUZA LA PUERTA: si una oleada corría en
            // el mundo real, muere AQUÍ (el interior sirve SU PROPIO ciclo
            // eterno — las oleadas de afuera no persiguen al portador
            // adentro del cuerpo del dios).
            try { GrimorioFuriaSistema.DetenerFestin(); } catch { }
            _ticksCalma = 0;
            _ciclosFestin = 0;
            _tinteCedido = 0f;
            Transicion = true;
            _mundoPrincipal = Main.ActiveWorldFileData;
            // la vuelta cae ENCIMA del altar, dos tiles arriba — no dentro.
            _posRetorno = player.Center - new Vector2(0f, 48f);
            Main.statusText = Language.GetTextValue("Mods.AethonMod.Submundo.Cargando");
            Main.menuMode = 10;
            Main.gameMenu = true;
            Task.Factory.StartNew(CallbackEntrar);
        }

        private static void CallbackEntrar()
        {
            try
            {
                WorldFile.CacheSaveTime();
                Player.SavePlayer(Main.ActivePlayerFileData);
                Player.ClearPlayerTempInfo();
                Rain.ClearRain();
                // EL MUNDO REAL SE GUARDA AHORA (Activo aún es falso — el
                // hook permite este UNO): al volver, el disco tiene la
                // verdad completa.
                WorldFile.SaveWorld();
                SystemLoader.OnWorldUnload();

                // === EL INTERIOR NACE ===
                Activo = true;
                Main.rand = new UnifiedRandom((int)DateTime.Now.Ticks);
                WorldGen.gen = true;
                WorldGen.loadFailed = false;
                WorldGen.loadSuccess = false;

                Main.maxTilesX = Ancho;
                Main.maxTilesY = Alto;
                // v6.115 — LA CURA DEL ENTIERRO: la plaza (Isla 30×7)
                // tiene su superficie en PlazaY−7 — el spawn viejo
                // (PlazaY−4) nacía TRES filas DENTRO de la roca (el
                // SpawnPlayer de vanilla lo rescataba arriba) y el
                // altar viejo (PlazaY−1) quedaba SEIS filas enterrado:
                // PlaceObject fallaba en silencio y el interior se
                // quedaba SIN PUERTA. Ahora el PRISMA vive DE PIE
                // (PlazaY−8: su fila de abajo es el primer aire) y el
                // spawn nace en el aire limpio SOBRE él.
                Main.spawnTileX = PlazaX;
                Main.spawnTileY = PlazaY - 14;
                WorldGen.setWorldSize();
                WorldGen.clearWorld();
                Main.worldSurface = Superficie;
                Main.rockLayer = Roca;
                GenVars.waterLine = Alto;
                Main.weatherCounter = 18000;
                Cloud.resetClouds();

                GenerarInterior();

                // el mapa del interior: LIMPIO (el velo lo cubre — el
                // interior no se cartografía como el mundo real).
                try { Main.Map = new WorldMap((ushort)Ancho, (ushort)Alto); } catch { }
                // v6.117 — EL MINIMAPA NACE LIMPIO: la cola de secciones
                // y las regiones de actualización del MUNDO REAL mueren
                // AQUÍ (el IL: DrawToMap no clampea mapMaxX/mapMaxY —
                // con los valores del mundo viejo el primer frame
                // revienta). La cura de la casa, en ambas direcciones.
                ReiniciarMiniMapa();

                // el FileData falso del interior (el hook ya impide que se
                // escriba; esto es para que el juego tenga ALGO coherente).
                string carpeta = _mundoPrincipal.IsCloudSave ? Main.CloudWorldPath : Main.WorldPath;
                Main.ActiveWorldFileData = new WorldFileData(
                    Path.Combine(carpeta, "InteriorGrimorio_" + _mundoPrincipal.UniqueId + ".wld"), false)
                {
                    Name = Language.GetTextValue("Mods.AethonMod.Submundo.Nombre"),
                    CreationTime = DateTime.Now,
                    Metadata = FileMetadata.FromCurrentSettings(FileType.World),
                    WorldGeneratorVersion = Main.WorldGeneratorVersion,
                    UniqueId = Guid.NewGuid()
                };

                WorldGen.gen = false;
                WorldGen.loadSuccess = true;
                Main.sectionManager.SetAllSectionsLoaded();

                Main.QueueMainThreadAction(delegate
                {
                    Main.LocalPlayer.Spawn(PlayerSpawnContext.SpawningIntoWorld);
                    WorldFile.SetOngoingToTemps();
                    Main.resetClouds = true;
                    Main.gameMenu = false;
                    Transicion = false;
                    // v6.115 — LA LLEGADA: el interior nace LLENO de
                    // bruma (la revelación la disipa) y el SOL DEL
                    // ECLIPSE se viste (el préstamo del Sun3 — el cenit
                    // morado de la oleada, para siempre).
                    Revelado = ReveladoMax;
                    PedirSolEclipse();
                    // v6.117 — LA ENTREGA DEL FARO: el portador llega
                    // SIN el giro del trago y la inundación empieza a
                    // retirarse (la revelación de niebla toma el relevo
                    // — el decaimiento vive en PostUpdateWorld).
                    try
                    {
                        Main.LocalPlayer.fullRotation = 0f;
                        global::AethonMod.Content.Projectiles.Definitivas.DevoracionEterna.Tragado = false;
                    }
                    catch { }
                    if (Main.myPlayer >= 0 && !Main.dedServ)
                    {
                        Main.NewText(Language.GetTextValue("Mods.AethonMod.Submundo.Bienvenida"),
                            new Color(200, 160, 255));
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74, Main.LocalPlayer.Center);
                    }
                });
            }
            catch (Exception e)
            {
                // la red de seguridad de la casa: si el interior no nace, se
                // vuelve al mundo real POR DISCO — jamás en limbo.
                try { LogModo("Entrar", e); } catch { }
                VolverADiscoPorFallo();
            }
        }

        /// <summary>SALIR DEL INTERIOR — el altar de la plaza (o el gesto de entrada).</summary>
        public static void Salir(Player player)
        {
            if (!Activo || Transicion) return;
            // v6.118 — EL FESTÍN MUERE CON EL CUERPO: las oleadas del
            // interior NO persiguen al portador al mundo real (la furia
            // del libro se queda en su casa — afuera manda SU reloj).
            try { GrimorioFuriaSistema.DetenerFestin(); } catch { }
            _ticksCalma = 0;
            _ciclosFestin = 0;
            _tinteCedido = 0f;
            PulsoCorazon = 0f;
            Transicion = true;
            Main.statusText = Language.GetTextValue("Mods.AethonMod.Submundo.Cerrando");
            Main.menuMode = 10;
            Main.gameMenu = true;
            Task.Factory.StartNew(CallbackSalir);
        }

        private static void CallbackSalir()
        {
            try
            {
                WorldFile.CacheSaveTime();
                Player.SavePlayer(Main.ActivePlayerFileData);
                Player.ClearPlayerTempInfo();
                Rain.ClearRain();
                // NADA de SaveWorld: el hook (Activo=true) lo bloquea — el
                // interior se despide sin escribir UN byte.
                SystemLoader.OnWorldUnload();
                Activo = false;

                // === EL MUNDO REAL VUELVE DESDE EL DISCO ===
                Main.rand = new UnifiedRandom((int)DateTime.Now.Ticks);
                WorldGen.gen = true;
                WorldGen.loadFailed = false;
                WorldGen.loadSuccess = false;

                Main.ActiveWorldFileData = _mundoPrincipal;
                CargarMundoPrincipal();

                WorldGen.gen = false;
                if (!WorldGen.loadSuccess)
                {
                    VolverADiscoPorFallo();
                    return;
                }

                if (Main.netMode != 2)
                {
                    if (Main.mapEnabled)
                    {
                        Main.Map.Load();
                    }
                    // v6.117 — el minimapa también nace limpio AL VOLVER
                    // (las regiones del interior mueren aquí — la cura
                    // simétrica; clearMap además BARRE los render targets
                    // para que ni un píxel del interior fantasmee en el
                    // mapa del mundo real).
                    ReiniciarMiniMapa();
                    Main.sectionManager.SetAllSectionsLoaded();
                }

                Main.QueueMainThreadAction(delegate
                {
                    Main.LocalPlayer.Spawn(PlayerSpawnContext.SpawningIntoWorld);
                    WorldFile.SetOngoingToTemps();
                    Main.resetClouds = true;
                    Main.gameMenu = false;
                    Transicion = false;
                    // v6.115 — la salida devuelve el sol de siempre y
                    // apaga los faros de bruma (nada viaja afuera).
                    DevolverSolEclipse();
                    BrumaFaro = 0f;
                    Revelado = 0;
                    // v6.117 — el trago tampoco viaja: el portador vuelve
                    // visible y derecho en el mundo real.
                    try
                    {
                        Main.LocalPlayer.fullRotation = 0f;
                        global::AethonMod.Content.Projectiles.Definitivas.DevoracionEterna.Tragado = false;
                    }
                    catch { }
                    // la vuelta cae AL LADO DEL ALTAR por donde entró —
                    // el libro te devuelve donde te abrió.
                    try
                    {
                        Main.LocalPlayer.Center = _posRetorno;
                        Main.LocalPlayer.velocity = Vector2.Zero;
                    }
                    catch { }
                    if (Main.myPlayer >= 0 && !Main.dedServ)
                    {
                        Main.NewText(Language.GetTextValue("Mods.AethonMod.Submundo.Despedida"),
                            new Color(240, 220, 180));
                    }
                });
            }
            catch (Exception e)
            {
                try { LogModo("Salir", e); } catch { }
                VolverADiscoPorFallo();
            }
        }

        /// <summary>
        /// v6.117 — LA CURA DEL MINIMAPA (leída en el IL del tML
        /// 2026.9.3.1): <see cref="Main.DrawToMap_Section"/> camina las
        /// secciones del <see cref="WorldSections"/> y lee
        /// <c>Map[x, y]</c> con el indexador CRUDO (más allá del mundo
        /// = IndexOutOfRangeException = Main engine crash), y
        /// <see cref="Main.DrawToMap"/> NO clampea mapMaxX/mapMaxY.
        /// Al cambiar de mundo EN JUEGO (sin pasar por el menú), la
        /// cola de secciones, la cuenta de tiles por actualizar y las
        /// regiones quedan con los valores del MUNDO ANTERIOR — el
        /// primer frame revienta. Esta cura todo lo estático del
        /// minimapa en cada cambio de mundo, en ambas direcciones.
        /// </summary>
        private static void ReiniciarMiniMapa()
        {
            try
            {
                Terraria.Map.MapHelper.ResetMapData();   // numUpdateTile = 0
                Main.updateMap = false;                    // la región de actualización muere
                Main.mapMinX = 0;
                Main.mapMaxX = 0;                          // los bucles de DrawToMap no corren
                Main.mapMinY = 0;
                Main.mapMaxY = 0;
                Main.refreshMap = true;                    // todas las secciones se repintan
                Main.clearMap = true;                      // los render targets se barren
            }
            catch { }
        }

        /// <summary>
        /// La recarga del mundo principal — literal el ciclo de
        /// SubworldLibrary (leer bytes → LoadWorld_Version2 →
        /// SystemLoader.OnWorldLoad → WorldIO.Load por reflexión).
        /// </summary>
        private static void CargarMundoPrincipal()
        {
            string ruta = _mundoPrincipal.Path;
            bool nube = _mundoPrincipal.IsCloudSave;
            bool flag = nube && SocialAPI.Cloud != null;
            if (!FileUtilities.Exists(ruta, flag))
            {
                WorldGen.loadFailed = true;
                return;
            }
            int status;
            using (BinaryReader reader = new BinaryReader(new MemoryStream(FileUtilities.ReadAllBytes(ruta, flag))))
            {
                status = WorldFile.LoadWorld_Version2(reader);
            }
            Main.ActiveWorldFileData.Name = Main.worldName;
            Main.ActiveWorldFileData.Metadata = Main.WorldFileMetadata;
            SystemLoader.OnWorldLoad();
            try
            {
                MethodInfo carga = typeof(ModLoader).Assembly
                    .GetType("Terraria.ModLoader.IO.WorldIO")
                    .GetMethod("Load", BindingFlags.NonPublic | BindingFlags.Static);
                carga?.Invoke(null, new object[] { ruta, flag });
            }
            catch { }
            if (status != 0)
            {
                WorldGen.loadFailed = true;
                WorldGen.loadSuccess = false;
                return;
            }
            WorldGen.loadSuccess = true;
            WorldGen.loadFailed = false;
        }

        /// <summary>
        /// LA RED DE SEGURIDAD: cualquier fallo en la transición manda al
        /// jugador AL MENÚ con el mundo real intacto en disco (recargarlo
        /// ahí siempre es posible — nunca quedó un limbo).
        /// </summary>
        private static void VolverADiscoPorFallo()
        {
            Activo = false;
            Transicion = false;
            BrumaFaro = 0f;
            Revelado = 0;
            DevolverSolEclipse();
            try
            {
                if (_mundoPrincipal != null)
                    Main.ActiveWorldFileData = _mundoPrincipal;
                Main.menuMode = 0;
                Main.gameMenu = true;
            }
            catch { }
        }

        private static void LogModo(string donde, Exception e)
        {
            try
            {
                ModContent.GetInstance<SubmundoGrimorio>().Mod.Logger
                    .Error($"[SubmundoGrimorio/{donde}] {e.Message}\n{e.StackTrace}");
            }
            catch { }
        }

        // ==================================================================
        //  LA GENERACIÓN — EL INTERIOR SIEMPRE IGUAL (la semilla fija de
        //  la casa: el grimorio ES su interior, no cambia de humor).
        //  v6.118 — LA SUPERFICIE DEL CUERPO: terreno CONTINUO de borde a
        //  borde (la letra: «primero tiene que estar en la superficie») —
        //  colinas de AMATISTA ENCENDIDA con la corteza dorada de la
        //  página, LAS VETAS de la PÁGINA DEL ETERNO corriendo por la
        //  carne, LAS VENTANAS donde la carne respira y EL CORAZÓN de
        //  amatista bajo la plaza. Las islas flotantes MURIERON.
        // ==================================================================
        private static void GenerarInterior()
        {
            // === LA PLAZA DEL ALTAR: plana, al centro (la puerta de vuelta) ===
            // v6.115 — EL PRISMA DEL ETERNO en el corazón de la plaza
            // (la letra: «en el momento en que el jugador aparece en el
            // mundo debe aparecer un altar que sera un prisma de
            // colores…»): DE PIE sobre la superficie — LA CURA DEL
            // ENTIERRO (v6.117): el prisma ocupa PlazaY−10..PlazaY−8 y
            // su fila de abajo es EL PRIMER AIRE (la roca alta de la
            // plaza es PlazaY−7).
            try
            {
                WorldGen.PlaceObject(PlazaX, PlazaY - 8,
                    ModContent.TileType<Tiles.AltarPrismaTile>(), false, 0);
            }
            catch { }

            // === EL TERRENO CONTINUO (el mapa de alturas de la casa con
            //     BrumaNoise — la MISMA matemática de la niebla de la
            //     casa dibuja las colinas del dios) ===
            int[] altura = new int[Ancho];
            for (int x = 5; x < Ancho - 5; x++)
            {
                // LAS COLINAS: fBm de la casa (lento y ampuloso — el
                // cuerpo de un dios no tiene colinas nerviosas)
                float n = BrumaNoise.WarpedFbm(x * 0.013f, 3.7f, 5150, 2.6f);
                float alto = 113f + (n - 0.5f) * 76f;   // ±38 tiles alrededor de 113
                // LA PLAZA PLANa: fusión suave en ±26 del centro
                float d = Math.Abs(x - PlazaX);
                if (d < 40f)
                {
                    float k = MathHelper.Clamp((40f - d) / 14f, 0f, 1f);   // el borde del valle
                    k = k * k * (3f - 2f * k);
                    alto = MathHelper.Lerp(alto, PlazaY - 7, k);
                }
                altura[x] = Math.Max(88, Math.Min(150, (int)alto));
            }

            // === EL CUERPO: la corteza DORADA (el filo de la página), la
            //     CARNE DE AMATISTA (el cristal que arde violeta) y el
            //     FORRO DEL LOMO abajo (las últimas filas: la contratapa
            //     del libro — el mundo ENTERO descansa sobre ella) ===
            for (int x = 5; x < Ancho - 5; x++)
            {
                int top = altura[x];
                for (int y = top; y < Alto - 5; y++)
                {
                    Tile t = Main.tile[x, y];
                    t.HasTile = true;
                    if (y == top)
                        t.TileType = TileID.GoldBrick;                     // EL FILO DORADO
                    else if (y >= Alto - 10)
                        t.TileType = TileID.GoldBrick;                     // LA CONTRATAPA
                    else
                        t.TileType = TileID.AmethystGemspark;              // LA CARNE VIOLETA
                    t.TileFrameX = 0;
                    t.TileFrameY = 0;
                }
            }

            // === LAS VETAS DE LA PÁGINA DEL ETERNO (el tile forjado —
            //     «vetas del tile forjado como decoración»): tres ríos
            //     del ORO DEL VEREDICTO serpenteando por la carne ===
            int tipoPagina = ModContent.TileType<Tiles.PaginaDelEternoTile>();
            for (int v = 0; v < 3; v++)
            {
                float yBase = 150f + 58f * v + 20f * BrumaNoise.Fbm(v * 13.7f, 0.5f, 5230, 2);
                for (int x = 8; x < Ancho - 8; x++)
                {
                    float ondula = (BrumaNoise.WarpedFbm(x * 0.031f, 9.1f + v * 4.4f, 5290 + v * 17, 2.2f) - 0.5f) * 34f;
                    int yV = (int)(yBase + ondula);
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int y = yV + dy;
                        if (y <= altura[x] + 3 || y >= Alto - 12) continue;  // vive DENTRO de la carne
                        Tile t = Main.tile[x, y];
                        if (t == null || !t.HasTile) continue;
                        t.TileType = (ushort)tipoPagina;
                        t.TileFrameX = (short)(VFXCore.Hash01(x, y, 6118 + v) > 0.62f ? 18 : 0);  // 2 variantes vivas
                        t.TileFrameY = 0;
                    }
                }
            }

            // === EL CORAZÓN DE AMATISTA: la cámara geoda bajo la plaza
            //     (el latido nace aquí — una elipse hueca forrada de oro
            //     con la veta del Eterno convergiendo en su techo) ===
            int cx = PlazaX, cy = PlazaY + 44;
            for (int dx = -12; dx <= 12; dx++)
            {
                float f = 1f - (float)(dx * dx) / 144f;
                if (f <= 0f) continue;
                int alto = (int)MathF.Round(8f * MathF.Sqrt(f));
                for (int dy = -alto; dy <= alto; dy++)
                {
                    int x = cx + dx, y = cy + dy;
                    if (x < 5 || x >= Ancho - 5 || y < 5 || y >= Alto - 12) continue;
                    Tile t = Main.tile[x, y];
                    if (dy == -alto || dy == alto || Math.Abs(dx) == 12)
                    {
                        t.HasTile = true;
                        t.TileType = Math.Abs(dx) == 11 || Math.Abs(dx) == 12
                            ? (ushort)tipoPagina : (ushort)TileID.GoldBrick;   // el forro: la página y el oro
                        t.TileFrameX = (short)(VFXCore.Hash01(x, y, 6120) > 0.5f ? 18 : 0);
                        t.TileFrameY = 0;
                    }
                    else
                    {
                        t.HasTile = false;                                 // LA CÁMARA HUECA
                    }
                }
            }
            // el piso de la cámara sigue siendo carne (nadie cae al vacío)
            for (int dx = -11; dx <= 11; dx++)
            {
                int y = cy + 9;
                if (y < Alto - 10 && cx + dx > 5 && cx + dx < Ancho - 5)
                {
                    Tile t = Main.tile[cx + dx, y];
                    t.HasTile = true;
                    t.TileType = TileID.AmethystGemspark;
                    t.TileFrameX = 0;
                    t.TileFrameY = 0;
                }
            }
            // LA BOQUILLA: un tiro vertical estrecho de la plaza a la
            // cámara (la veta sube y baja por él — la entrada al corazón)
            for (int y = PlazaY - 6; y < cy - 8; y++)
            {
                Tile t = Main.tile[cx + 4, y];
                t.HasTile = false;
                Tile t2 = Main.tile[cx + 5, y];
                t2.HasTile = false;
            }

            // === LAS VENTANAS DE VIDA (donde la carne respira): cinco
            //     respiraderos deterministas repartidos por el mundo —
            //     de ellos suben LAS COLUMNAS DE BRUMA y nacen LAS MOTES
            //     DE ORO (las posiciones viven en _vents para el pase
            //     del mundo; siempre las mismas: el dios respira igual) ===
            _vents = new Vector2[5];
            int vi = 0;
            for (int k = 0; k < 5; k++)
            {
                int x = 90 + k * 155 + (int)(VFXCore.Hash01(k * 31 + 7, 1, 6130) * 60f) - 30;
                if (x < 8 || x >= Ancho - 8) x = 90 + k * 155;
                if (Math.Abs(x - PlazaX) < 40) x = PlazaX + (x < PlazaX ? -70 : 70);  // lejos de la plaza
                if (x < 8 || x >= Ancho - 8) continue;
                int y = altura[x] - 1;
                _vents[vi++] = new Vector2(x * 16f + 8f, y * 16f + 8f);
                // la ventana: una pequeña cavidad en la superficie
                for (int dx2 = -2; dx2 <= 2; dx2++)
                {
                    for (int dy2 = -2; dy2 <= 1; dy2++)
                    {
                        int xx = x + dx2, yy = y + dy2;
                        if (xx > 5 && xx < Ancho - 5 && yy > 5 && yy < Alto - 10 && Math.Abs(dx2) + Math.Abs(dy2) < 4)
                        {
                            // v6.118 — EL PATRÓN DE ESCRITURA DE LA CASA (el
                            // indexador de Tilemap devuelve el Tile POR
                            // VALOR en este tML: se escribe vía LOCAL — el
                            // struct envuelve el almacenamiento real)
                            Tile tv = Main.tile[xx, yy];
                            tv.HasTile = false;
                        }
                    }
                }
                // el marco dorado de la ventana
                for (int dx2 = -3; dx2 <= 3; dx2++)
                {
                    int xx = x + dx2, yy = y + 2;
                    if (xx > 5 && xx < Ancho - 5 && yy < Alto - 10 && !Main.tile[xx, yy - 3].HasTile)
                    {
                        Tile t = Main.tile[xx, yy];
                        t.HasTile = true;
                        t.TileType = TileID.GoldBrick;
                        t.TileFrameX = 0;
                        t.TileFrameY = 0;
                    }
                }
            }
            if (vi < _vents.Length) System.Array.Resize(ref _vents, vi);

            // EL MAR DE NIEBLA: nada de líquidos — la bruma es el velo.
        }

        // (LA ISLA FLOTANTE MURIÓ con el rework v6.118 — el terreno
        // continuo de arriba es el cuerpo entero del dios.)

        // ==================================================================
        //  LA VIDA DEL INTERIOR — la medianoche eterna, la bruma viva, la
        //  luz del vacío y la red de caída.
        // ==================================================================
        public override void PreUpdateWorld()
        {
            if (!Activo) return;

            // ==============================================================
            //  v6.115 — EL ECLIPSE PERMANENTE (la letra: «este mundo
            //  estara en eclipse permanente ya sabes el eclipse de las
            //  oleadas de hambre, morado y lleno de bruma»): el sol
            //  CLAVADO EN SU CENIT (mediodía exacto, time 27000 — el
            //  cielo no conoce ni amanecer ni ocaso) y VESTIDO DE
            //  ECLIPSE (el préstamo del Sun3 de vanilla — la misma
            //  técnica de la oleada de hambre). La corona morada de la
            //  entidad es el único mediodía que el interior ha visto;
            //  el OJO COLOSAL del velo lo corona desde arriba.
            // ==============================================================
            Main.dayTime = true;
            Main.time = 27000;
            if (Main.raining)
            {
                Main.raining = false;
                Main.maxRaining = 0f;
            }

            // LA NIEBLA DEL ECLIPSE (el filtro del cementerio — SOLO
            // visual, como en la oleada: la masa de bruma, el cielo
            // oscurecido y el relámpago esporádico de la casa).
            if (Main.GraveyardVisualIntensity < 0.90f)
                Main.GraveyardVisualIntensity = 0.90f;
        }

        /// <summary>
        /// v6.115 — EL TINTE DEL ECLIPSE PERMANENTE · v6.118 — EL
        /// CEDIMIENTO: cuando el festín vive dentro, EL ECLIPSE DE LA
        /// OLEADA manda (AmbienteOleadaSistema — «ya sabes el eclipse de
        /// las oleadas de hambre»); el tinte propio del interior se
        /// apaga SUAVE (lerp ~2 s) para que los DOS eclipses se crucen
        /// sin un solo corte de color.
        /// </summary>
        public override void ModifySunLightColor(ref Color tileColor, ref Color backgroundColor)
        {
            if (Main.netMode == NetmodeID.Server || !Activo) return;
            float fuerza = 1f - _tinteCedido;
            if (fuerza <= 0.02f) return;
            backgroundColor = Color.Lerp(backgroundColor, new Color(92, 38, 120), 0.92f * fuerza);
            tileColor = Color.Lerp(tileColor, new Color(172, 112, 196), 0.52f * fuerza);
        }

        /// <summary>EL SOL DEL ECLIPSE SE VISTE (el préstamo del Sun3 —
        /// idempotente y sin pisar el préstamo de la oleada).</summary>
        private static void PedirSolEclipse()
        {
            if (_solPuesto) return;
            try
            {
                if (TextureAssets.Sun != TextureAssets.Sun3)
                {
                    _solOriginal = TextureAssets.Sun;
                    TextureAssets.Sun = TextureAssets.Sun3;
                    _solPuesto = true;
                }
            }
            catch { }
        }

        /// <summary>El sol de siempre vuelve a su ranura (idempotente).</summary>
        private static void DevolverSolEclipse()
        {
            if (!_solPuesto) return;
            try
            {
                if (_solOriginal != null) TextureAssets.Sun = _solOriginal;
            }
            catch { }
            _solPuesto = false;
            _solOriginal = null;
        }

        public override void PostUpdateWorld()
        {
            if (!Activo || Main.gameMenu || Main.dedServ) return;

            // v6.115 — LA REVELACIÓN CAMINA: la niebla de la llegada se
            // disipa un tick tras otro (los primeros ~4 s del interior
            // son el nacimiento del mundo — la letra: «al aparecer en
            // el nuevo mundo el mundo esta lleno de bruma y esta se
            // disipa revelando como es este mundo nuevo»).
            if (Revelado > 0) Revelado--;

            // v6.117 — LA RETIRADA DEL FARO: la inundación de la
            // devoración se disuelve adentro (~0,7 s) mientras LA
            // REVELACIÓN toma el relevo — el mundo emerge de las dos
            // nieblas a la vez, sin un corte visible.
            if (BrumaFaro > 0f) BrumaFaro = Math.Max(0f, BrumaFaro - 0.025f);

            // ==============================================================
            //  v6.118 — EL LATIDO DEL CORAZÓN de amatista (~5 s): una ola
            //  que sube y muere — TODO el cuerpo del dios respira con
            //  ella (el cielo de tentáculos se hincha, la OSCURIDAD por
            //  capas engorda, las columnas de bruma se yerguen y las
            //  motas de oro se avivan). El golpe se SIENTE: un temblor
            //  suave de la casa en el pico del latido.
            // ==============================================================
            {
                float fase = (Main.GlobalTimeWrappedHourly % 5f) / 5f;
                float onda = MathF.Sin(fase * MathHelper.TwoPi);
                PulsoCorazon = MathF.Pow(MathF.Max(0f, onda), 3f);
                if (PulsoCorazon > 0.85f && fase > 0.1f && fase < 0.4f && Main.netMode != 2)
                    OndaLib.Kick(2);   // el golpe del corazón — suave, como manda la casa
            }

            // ==============================================================
            //  v6.118 — EL CICLO ETERNO DE LAS OLEADAS DEL HAMBRE: cada
            //  ~2,5 min de calma, el libro SIRVE una oleada (la letra:
            //  «debe ser el de las oleadas de hambre» + el cuestionario:
            //  ciclo automático, enemigos DE LA CASA). El festín corre
            //  por GrimorioFuriaSistema (el motor de siempre — su HUD,
            //  su pool, su escala por nivel); el nivel del festín es EL
            //  NIVEL DEL PORTADOR (FuriaNivel: el dios come a la altura
            //  de quien lo carga — y el interior NUNCA sube ese nivel:
            //  probar no es progresar, la lección v6.50.74). Si Aethon
            //  vive dentro (el prisma lo invoca), el ciclo ESPERA.
            // ==============================================================
            bool festinVivo;
            try { festinVivo = GrimorioFuriaSistema.Activo; } catch { festinVivo = false; }
            if (!festinVivo)
            {
                bool mundoLibre;
                try { mundoLibre = GrimorioFuriaSistema.MundoLibre(); } catch { mundoLibre = false; }
                if (mundoLibre)
                {
                    _ticksCalma++;
                    if (_ticksCalma >= CalmaTicks)
                    {
                        Player hambriento = Main.LocalPlayer;
                        if (hambriento != null && hambriento.active && !hambriento.dead)
                        {
                            int nivel = 2;
                            try
                            {
                                var sp = hambriento.GetModPlayer<Players.ShardPlayer>();
                                if (sp != null) nivel = Math.Max(2, Math.Min(10, sp.FuriaNivel));
                            }
                            catch { }
                            GrimorioFuriaSistema.Provocar(hambriento, nivel, natural: false);
                            _ciclosFestin++;
                            _ticksCalma = 0;
                        }
                    }
                }
            }
            else
            {
                _ticksCalma = 0;
            }

            // EL CEDIMIENTO DEL TINTE: el eclipse de la oleada manda
            // mientras el festín viva (cruce suave ~2 s — sin cortes).
            {
                bool festinVisible;
                try { festinVisible = AmbienteOleadaSistema.FestinVisible; }
                catch { festinVisible = false; }
                float objetivo = festinVisible ? 1f : 0f;
                _tinteCedido += (objetivo - _tinteCedido) * 0.012f;
                if (_tinteCedido < 0.005f) _tinteCedido = 0f;
                if (_tinteCedido > 0.995f) _tinteCedido = 1f;
            }

            Player p = Main.LocalPlayer;
            if (p == null || !p.active) return;

            // LA LUZ DEL VACÍO: el portador nunca está a ciegas — el
            // interior lo abraza con un aura violeta tenue (más el
            // AURA DEL LATIDO cuando el corazón golpea).
            try
            {
                float latido = 0.9f + 0.5f * PulsoCorazon;
                Lighting.AddLight(p.Center, new Vector3(0.34f * latido, 0.20f * latido, 0.52f * latido));

                // ==========================================================
                //  v6.118 — LAS MOTES DE ORO (la letra: «particulas» + el
                //  cuestionario: motas doradas de esencia subiendo con luz
                //  propia): LAS ESENCIAS DEVORADAS circulan por el cuerpo
                //  del dios — nacen en LAS VENTANAS de vida y alrededor
                //  del portador, suben en espiral LENTA y se disuelven en
                //  violeta. Partículas DE LA CASA (ParticleManager — los
                //  Dust de vanilla MURIERON con el rework: «no debe tener
                //  circulos planos»).
                // ==========================================================
                if (Main.netMode != NetmodeID.Server)
                {
                    // de las ventanas cercanas (≤1 mota cada 20 t por
                    // ventana — el GATE del tick evita las ráfagas: la
                    // semilla cuantizada dispararía 20 veces por ventana)
                    if (_vents != null && (Main.GameUpdateCount % 20u) == 0u)
                    {
                        foreach (Vector2 vent in _vents)
                        {
                            if (Vector2.DistanceSquared(vent, p.Center) > 1500f * 1500f) continue;
                            if (VFXCore.Hash01((int)(vent.X * 0.13f), (int)(Main.GameUpdateCount / 20), 6140) < 0.55f)
                                continue;
                            SpawnMotaDorada(vent + new Vector2(
                                (VFXCore.Hash01((int)vent.X, (int)Main.GameUpdateCount, 6141) - 0.5f) * 26f, 0f));
                        }
                    }
                    // el aliento del portador (1 cada ~20 t — la esencia
                    // que el libro ya probó se le escapa dorada)
                    if ((Main.GameUpdateCount % 20u) == 7u &&
                        VFXCore.Hash01(p.whoAmI * 3, (int)(Main.GameUpdateCount / 20), 6143) < 0.55f)
                        SpawnMotaDorada(p.Center + new Vector2(
                            (VFXCore.Hash01(p.whoAmI, (int)Main.GameUpdateCount, 6144) - 0.5f) * 44f,
                            10f + VFXCore.Hash01(p.whoAmI + 1, (int)Main.GameUpdateCount, 6145) * 26f));
                }

                // LA RED DE CAÍDA: quien resbala al fondo del cuerpo cae al
                // vacío… y el vacío LO DEVUELVE a la plaza (nadie muere en
                // el libro). v6.115 — al LADO del prisma (PlazaY−10: el
                // aire limpio sobre la superficie, cuatro columnas a la
                // derecha del corazón de la plaza).
                if (p.position.Y > (Alto - 12) * 16f)
                {
                    p.Center = new Vector2((PlazaX + 4) * 16f + 8f, (PlazaY - 10) * 16f);
                    p.velocity = Vector2.Zero;
                    if (p.whoAmI == Main.myPlayer)
                    {
                        Main.NewText(Language.GetTextValue("Mods.AethonMod.Submundo.RedCaida"),
                            new Color(200, 160, 255));
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, p.Center);
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// v6.118 — LA MOTA DE ORO DE LA ESENCIA: una partícula de la casa
        /// (ParticleManager) que nace dorada, sube en espiral LENTA,
        /// ILUMINA lo que toca (EmitLight) y se enfría hacia el violeta
        /// del vacío antes de disolverse — la esencia devorada que
        /// circula por el cuerpo del dios.
        /// </summary>
        private static void SpawnMotaDorada(Vector2 pos)
        {
            try
            {
                int dur = 150 + (int)(VFXCore.Hash01((int)pos.X, (int)pos.Y, 6150) * 110f);
                var m = new global::AethonMod.Content.Particles.ParticleData
                {
                    Position = pos,
                    Velocity = new Vector2(
                        (VFXCore.Hash01((int)Main.GameUpdateCount, (int)pos.X, 6151) - 0.5f) * 0.5f,
                        -0.30f - VFXCore.Hash01((int)pos.Y, (int)Main.GameUpdateCount, 6152) * 0.35f),
                    Scale = Vector2.One * (0.42f + VFXCore.Hash01((int)pos.X, (int)Main.GameUpdateCount, 6153) * 0.45f),
                    PackedColor = global::AethonMod.Content.Particles.ParticleManager.PackColor(new Color(255, 224, 150)),
                    PackedStartColor = global::AethonMod.Content.Particles.ParticleManager.PackColor(new Color(255, 224, 150)),
                    PackedEndColor = global::AethonMod.Content.Particles.ParticleManager.PackColor(new Color(170, 120, 235)),
                    TimeLeft = dur,
                    Duration = dur,
                    TextureId = global::AethonMod.Content.Particles.ParticleTex.SoftGlow,
                    BlendMode = 1,      // aditivo: la esencia ARDE
                    LayerPriority = global::AethonMod.Content.Particles.LayerPriorities.AboveTiles,
                };
                m.EnableComponent(global::AethonMod.Content.Particles.ComponentFlag.FadeIn);
                m.EnableComponent(global::AethonMod.Content.Particles.ComponentFlag.FadeOut);
                m.EnableComponent(global::AethonMod.Content.Particles.ComponentFlag.ColorShift);
                m.EnableComponent(global::AethonMod.Content.Particles.ComponentFlag.EmitLight);
                global::AethonMod.Content.Particles.ParticleManager.Spawn(m);
            }
            catch { }
        }

        public override void OnWorldUnload()
        {
            // si el jugador se va al MENÚ desde dentro (guardar y salir),
            // el mundo NO se guarda (el hook) y el jugador vuelve a su
            // punto de aparición — nunca despierta dentro de la roca del
            // mundo real con coordenadas del interior.
            if (Activo && Main.netMode == 0)
            {
                try
                {
                    Player p = Main.LocalPlayer;
                    if (p != null && p.active)
                    {
                        p.Center = new Vector2(Main.spawnTileX * 16f, Main.spawnTileY * 16f);
                        p.velocity = Vector2.Zero;
                    }
                }
                catch { }
            }
            Activo = false;
            Transicion = false;
            // v6.115 — nada del interior viaja afuera: el sol de
            // siempre vuelve a su ranura y los faros se apagan.
            // v6.118 — la vida nueva también se apaga (el latido muere
            // con el cuerpo y el reloj del ciclo vuelve a cero).
            DevolverSolEclipse();
            BrumaFaro = 0f;
            Revelado = 0;
            PulsoCorazon = 0f;
            _vents = null;
            _ticksCalma = 0;
            _tinteCedido = 0f;
        }

        // ==================================================================
        //  EL VELO DE BRUMA — la capa más baja de la interfaz: el interior
        //  ES la niebla. Sobre el mundo, bajo TODO lo demás.
        // ==================================================================
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            // v6.117 — EL REPARTO DE LAS DOS NIEBLAS: esta capa pinta SOLO
            // el velo del interior (mundo real: nada). EL FARO de la
            // devoración (la inundación que también vive en el mundo
            // real) se mudó a Main.OnPostDraw — el único punto que pinta
            // TAMBIÉN en el menú (la capa de interfaz NO corre en
            // gameMenu y la pantalla de carga quedaba EXPUESTA a mitad
            // del trago).
            if (!Activo || Main.dedServ || Main.gameMenu) return;
            layers.Insert(0, new LegacyGameInterfaceLayer(
                "AethonMod: VeloInterior", DibujarVelo, InterfaceScaleType.None));
        }

        private static bool DibujarVelo()
        {
            // v6.117 — LA CURA DEL LOTE (la excepción silenciosa del
            // client.log: «Begin has been called before calling End» en
            // esta función). EL CONTRATO, leído en el IL de
            // GameInterfaceLayer.Draw de ESTE tML: la capa ABRE ELLA
            // MISMA el lote — Begin(Deferred, AlphaBlend, LinearClamp,
            // DepthStencil.Default, CullCCW, Matrix.Identity) para
            // InterfaceScaleType.None — y lo CIERRA al volver del
            // delegado. LA CURA: dibujar SIN TOCAR Begin/End ni una vez
            // — el lote de la capa ya tiene EXACTO lo que el velo
            // necesita (píxeles de pantalla crudos: la interfaz no
            // corre en gameMenu y la pantalla de carga quedaba EXPUESTA
            // a mitad del trago).
            //
            // v6.118 — EL VELO ES BRUMA DE VERDAD (la letra: «parece que
            // todo lo que llamas bruma son solo circulos planos, eso
            // esta mal, te dije que usaras nuestras librerias»): los
            // RECTS de SoftGlow, las BANDAS planas y EL OJO colosal
            // MURIERON. El velo viste LA OSCURIDAD POR CAPAS con los
            // pinceles fBm de BrumaFX (la librería de la casa — el
            // pincel dibuja EN el lote abierto del llamador: el contrato
            // de la capa queda INTACTO) y la revelación disipa con
            // AnimatedPuffs de verdad + EL IRIS con el NovaBurst
            // sancionado (el degradado monotónico — jamás un círculo
            // plano).
            if (Main.gameMenu || Main.dedServ) return true;
            if (_veloProbeFallos) return true;

            try
            {
                float w = Main.screenWidth;
                float h = Main.screenHeight;
                float t = Main.GlobalTimeWrappedHourly;
                // EL LATIDO DEL CORAZÓN engorda la oscuridad (el cuerpo
                // del dios respira: cada golpe, el velo se hincha).
                float latido = PulsoCorazon;

                // ============================================================
                //  v6.115→v6.118 — LA REVELACIÓN DE LA LLEGADA: el interior
                //  nace LLENO de bruma y esta SE DISIPA revelando el mundo
                //  (la letra). Ahora es NIEBLA DE VERDAD: la base (bajo los
                //  puffs, garantizada) + SIETE MASAS fBm que ENFRÍAN
                //  (endColor violeta) y se disipan al ritmo de la llegada
                //  + EL IRIS DORADO respirando con el NovaBurst.
                // ============================================================
                float aRev = Activo ? (float)Revelado / ReveladoMax : 0f;
                if (aRev > 0.003f)
                {
                    float k = aRev * aRev;   // ease-out: lenta al final
                    Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle(0, 0, (int)w, (int)h),
                        new Color(16, 8, 28) * (0.66f * k));

                    // LAS MASAS DEL NACIMIENTO (7 — se disipan con la llegada)
                    for (int i = 0; i < 7; i++)
                    {
                        float px = w * (0.08f + 0.14f * i) +
                            MathF.Sin(t * 0.4f + i * 1.7f) * w * 0.05f;
                        float py = h * (0.14f + 0.13f * (i % 5)) +
                            MathF.Cos(t * 0.3f + i * 2.1f) * h * 0.06f;
                        float radio = (0.30f + 0.10f * (i % 3)) * h + 120f * k;
                        BrumaFX.AnimatedPuff(new Vector2(px, py), radio,
                            new Color(110, 72, 176), 6211 + i * 13,
                            life01: 1f - aRev, time: t,
                            alpha: (0.52f - 0.10f * (i % 3)) * k,
                            endColor: new Color(30, 16, 52));
                    }

                    // EL IRIS DEL NACIMIENTO (el aliento dorado de la
                    // entidad a mitad de la revelación — el NOVABURST:
                    // el degradado monotónico de la casa, NUNCA un glow
                    // plano apilado).
                    float abre = 1f - aRev;
                    float aIris = 0.85f * abre * (1f - abre) * 4f;
                    if (aIris > 0.01f && VFXCore.NovaBurst != null)
                    {
                        float tamX = w * (0.20f + 0.68f * abre);
                        float tamY = h * (0.16f + 0.32f * abre);
                        Main.spriteBatch.Draw(VFXCore.NovaBurst,
                            new Rectangle((int)(w * 0.5f - tamX * 0.5f),
                                          (int)(h * 0.17f - tamY * 0.5f),
                                          (int)tamX, (int)tamY),
                            null, new Color(255, 214, 130) * (0.34f * aIris), 0f,
                            Vector2.Zero, SpriteEffects.None, 0f);
                    }
                }

                // ============================================================
                //  EL VELO PROPIO — LA OSCURIDAD POR CAPAS (la letra del
                //  cuestionario: «oscuridad por capas» + «ambas»: el
                //  gradiente base + el latido encima). TRES CAPAS de
                //  masa fBm que CUELGA del techo del mundo, ENGORDE en
                //  el horizonte de abajo y CIÑE los costados — la zona
                //  jugable queda legible entre ellas.
                // ============================================================

                // (base tenue — el piso que garantiza el tono del vacío
                // BAJO las masas de verdad; los puffs viven ENCIMA)
                Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle(0, 0, (int)w, (int)h),
                    new Color(22, 12, 38) * 0.30f);

                // CAPA 1 — EL TECHO: la masa que cuelga del cielo del
                // cuerpo (dos puffs enormes respirando con el latido)
                BrumaFX.Puff(new Vector2(w * 0.30f + MathF.Sin(t * 0.12f) * w * 0.04f, -h * 0.14f),
                    h * 0.60f, new Color(16, 9, 28), 6311, t,
                    alpha: 0.36f + 0.12f * latido, quality: 0.8f);
                BrumaFX.Puff(new Vector2(w * 0.74f + MathF.Cos(t * 0.10f) * w * 0.04f, -h * 0.18f),
                    h * 0.54f, new Color(20, 11, 34), 6323, t + 21f,
                    alpha: 0.32f + 0.10f * latido, quality: 0.8f);

                // CAPA 2 — EL HORIZONTE: el velo ENGORDA hacia abajo (el
                // piso de niebla del cuerpo del dios — dos racimos)
                BrumaFX.Cloud(new Vector2(w * 0.32f, h * 1.10f), h * 0.44f,
                    new Color(26, 14, 44), 6331, t, 5,
                    alpha: 0.30f + 0.08f * latido);
                BrumaFX.Cloud(new Vector2(w * 0.78f, h * 1.14f), h * 0.40f,
                    new Color(30, 16, 52), 6343, t + 13f, 4,
                    alpha: 0.26f + 0.08f * latido);

                // CAPA 3 — LOS COSTADOS: la oscuridad CIÑE los bordes
                // laterales (el mundo vive DENTRO del cuerpo)
                BrumaFX.Puff(new Vector2(-w * 0.07f, h * 0.42f), w * 0.26f,
                    new Color(18, 10, 30), 6351, t + 7f, alpha: 0.24f, quality: 0.6f);
                BrumaFX.Puff(new Vector2(w * 1.07f, h * 0.52f), w * 0.28f,
                    new Color(18, 10, 30), 6363, t + 17f, alpha: 0.24f, quality: 0.6f);

                // EL ALIENTO VIOLETA entre las capas (la lectura de
                // noche — la firma de SombrasLib destilada al velo)
                BrumaFX.Cloud(new Vector2(w * 0.5f, h * 0.55f), h * 0.30f,
                    new Color(64, 42, 96), 6371, t * 0.8f, 3, alpha: 0.10f + 0.05f * latido);
            }
            catch
            {
                // v6.117 — la armadura ya NO mata el velo por un frame
                // raro (la sonda solo muere si LA TEXTURA falta): el
                // lote de la capa queda intacto pase lo que pase (nunca
                // lo abrimos) y su End() lo cierra limpio.
            }
            return true;
        }

        /// <summary>
        /// v6.117 — EL FARO DE LA DEVORACIÓN (Main.OnPostDraw): la
        /// bruma que INUNDA la pantalla mientras el libro traga al
        /// portador — y que ahora vive SOBRE TODO EL FRAME TERMINADO:
        /// el mundo, la interfaz Y LA PANTALLA DE CARGA (la letra:
        /// «en el momento en que es devorado el mundo se llena de
        /// bruma solo en la animacion»). El IL de Main.Draw_Inner:
        /// DoDraw completa el frame entero y recién entonces dispara
        /// OnPostDraw — el único punto que pinta también en gameMenu.
        /// </summary>
        private static void PintarFaroDevoracion(GameTime gameTime)
        {
            try
            {
                if (Main.dedServ) return;
                if (BrumaFaro <= 0.01f) return;
                // LA RED DE LA CASA: si el menú NO es nuestra transición
                // (el jugador abortó a mitad del trago), la inundación
                // muere ahí mismo — nadie queda atrapado en la bruma.
                if (Main.gameMenu && !Transicion)
                {
                    BrumaFaro = 0f;
                    return;
                }
                if (_veloProbeFallos) return;
                if (_softGlow == null || _softGlow.IsDisposed)
                {
                    try
                    {
                        _softGlow = ModContent.Request<Texture2D>(
                            "AethonMod/Content/Effects/Procedural/SoftGlow",
                            (AssetRequestMode)1).Value;
                    }
                    catch { return; }
                }

                // EL BAILE DEL LOTE: OnPostDraw recibe el frame con el
                // lote en CUALQUIER estado (el camino del menú termina
                // cerrado; el del juego puede quedar abierto) — se cierra
                // lo que haya (sonda de la casa), se pinta la inundación
                // en un lote propio y se devuelve CERRADO (el estado
                // natural entre frames: DoDraw arranca con su propio
                // Begin).
                VFXCore.CerrarLoteSiAbierto();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null,
                    Matrix.Identity);
                try
                {
                    float a = BrumaFaro;
                    float w = Main.screenWidth;
                    float h = Main.screenHeight;
                    float t = Main.GlobalTimeWrappedHourly;

                    // ========================================================
                    //  v6.118 — LA INUNDACIÓN ES BRUMA DE VERDAD (la letra:
                    //  «todo lo que llamas bruma son solo circulos planos,
                    //  eso esta mal»): la BASE (garantizada bajo la niebla
                    //  — cuando el faro está lleno no hay mundo, solo el
                    //  violeta del trago) + SEIS MASAS fBm que ACELERAN
                    //  con el trago (0.6→1.4×) y ENSANCHAN su deriva —
                    //  BrumaFX en el lote propio del faro (píxeles de
                    //  pantalla crudos: también en la pantalla de carga).
                    // ========================================================
                    Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle(0, 0, (int)w, (int)h),
                        new Color(18, 9, 32) * (0.90f * a));

                    float vel = 0.6f + 0.8f * a;             // la ACELERACIÓN del trago
                    for (int masa = 0; masa < 6; masa++)
                    {
                        float y = h * (0.06f + masa * 0.17f) +
                            MathF.Sin(t * 0.4f + masa * 1.3f) * h * 0.06f;
                        float x = ((t * (46f + masa * 20f) * vel + masa * 520f) % (w + 1400f)) - 700f;
                        float radio = h * (0.16f + 0.05f * (masa % 3)) + 60f * a;
                        BrumaFX.Puff(new Vector2(x, y), radio,
                            masa % 2 == 0 ? new Color(96, 62, 158) : new Color(30, 16, 52),
                            6411 + masa * 17, t + masa * 3.1f,
                            alpha: (0.40f + 0.07f * masa) * a, quality: 0.7f,
                            velocity: new Vector2((14f + masa * 6f) * vel, 0f));
                    }
                }
                finally
                {
                    Main.spriteBatch.End();
                }
            }
            catch { }
        }

        // ==================================================================
        //  v6.118 — LAS BRUMAS DEL MUNDO (el pase del mundo — coords de
        //  MUNDO convertidas a pantalla, en el lote de masa de BrumaFX):
        //  LAS COLUMNAS que ascienden de LAS VENTANAS de vida, LAS
        //  VOLUTAS que serpentean sobre el terreno y LA BRUMA REACTIVA
        //  que SE ABRE a tu paso y se cierra detrás (el cuestionario:
        //  «todas» las brumas — columnas · volutas · reactiva).
        // ==================================================================
        public override void PostDrawTiles()
        {
            if (!Activo || Main.gameMenu || Main.dedServ || Main.netMode == 2) return;
            if (_veloProbeFallos) return;
            try
            {
                Player p = Main.LocalPlayer;
                if (p == null || !p.active || p.dead) return;
                float t = Main.GlobalTimeWrappedHourly;
                float latido = PulsoCorazon;
                Vector2 o = Main.screenPosition;

                VFXCore.CerrarLoteSiAbierto();
                try
                {
                    BrumaFX.BeginMass();
                    try
                    {
                        // === LAS COLUMNAS DE LAS VENTANAS (la carne
                        //     respira: bruma que ASCIENDE de cada ventanal
                        //     cercano — se yerguen con el latido) ===
                        if (_vents != null)
                        {
                            foreach (Vector2 vent in _vents)
                            {
                                if (Vector2.DistanceSquared(vent, p.Center) > 1300f * 1300f) continue;
                                BrumaFX.Column(vent - o,
                                    150f + 60f * latido,              // la altura respira con el corazón
                                    74f + 22f * latido,
                                    new Color(44, 26, 78),
                                    (int)(vent.X * 0.13f), t,
                                    alpha: 0.24f + 0.10f * latido, speed: 0.30f);
                            }
                        }

                        // === LAS VOLUTAS SOBRE EL TERRENO (la niebla
                        //     serpentea rozando la superficie del dios) ===
                        for (int k = 0; k < 2; k++)
                        {
                            float bx = p.Center.X + (k == 0 ? -420f : 430f) +
                                MathF.Sin(t * 0.11f + k * 2.7f) * 130f;
                            float by = p.Center.Y - 130f + MathF.Cos(t * 0.09f + k) * 60f;
                            Vector2[] camino = new Vector2[5];
                            for (int s = 0; s < 5; s++)
                            {
                                camino[s] = new Vector2(
                                    bx - o.X + s * 105f + MathF.Sin(t * 0.3f + s * 1.7f + k * 3f) * 34f,
                                    by - o.Y + MathF.Sin(s * 1.1f + t * 0.2f + k) * 30f);
                            }
                            BrumaFX.Tendril(camino, 52f, new Color(58, 36, 92),
                                6511 + k * 7, t, alpha: 0.18f + 0.06f * latido);
                        }

                        // === LA BRUMA REACTIVA (SE ABRE a tu paso): un
                        //     anillo de puffs orbitando LENTO alrededor del
                        //     portador — cada masa SE APAGA cuando el
                        //     portador se acerca (la niebla lo RESPETA) y
                        //     vuelve a cerrarse detrás (nunca lo toca) ===
                        for (int i = 0; i < 6; i++)
                        {
                            float ang = t * 0.14f + i * MathHelper.TwoPi / 6f;
                            float r = 210f + 70f * MathF.Sin(t * 0.2f + i * 2.3f);
                            Vector2 pos = p.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.7f) * r;
                            float dist = Vector2.Distance(pos, p.Center);
                            float abre = MathHelper.Clamp((dist - 120f) / 130f, 0f, 1f);
                            if (abre <= 0.05f) continue;             // la niebla SE ABRE alrededor
                            BrumaFX.Puff(pos - o, 96f + 30f * (i % 2),
                                new Color(40, 24, 70), 6611 + i * 13, t + i * 2.2f,
                                alpha: 0.20f * abre, quality: 0.6f);
                        }
                    }
                    finally
                    {
                        try { Main.spriteBatch.End(); } catch { }
                    }
                }
                finally { VFXCore.ReabrirLoteVanilla(); }
            }
            catch { }
        }
    }

    // ======================================================================
    //  InteriorSpawnGate — EL SILENCIO DE LAS FIERAS (v6.114 → v6.118 —
    //  LA EXCEPCIÓN DEL FESTÍN): fuera del festín, dentro del libro NO
    //  despierta bestia alguna — el interior es el libro y sus páginas:
    //  la soledad es parte del horror. PERO el mundo ES ahora EL MUNDO
    //  DE LAS OLEADAS DEL HAMBRE (la letra: «debe ser el de las oleadas
    //  de hambre»): cuando el festín corre (GrimorioFuriaSistema), la
    //  puerta se ABRE y el motor de spawn de la oleada manda (OleadaNPC
    //  — el pool de la casa con su escala por nivel). (El hook de la
    //  casa: EditSpawnRate/EditSpawnRange viven en GlobalNPC —
    //  verificado por reflexión contra el tML 2026.09.3.1 real.)
    // ======================================================================
    public class InteriorSpawnGate : GlobalNPC
    {
        public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
        {
            if (SubmundoGrimorio.Activo && !GrimorioFuriaSistema.Activo)
            {
                spawnRate = 0;
                maxSpawns = 0;
            }
        }

        public override void EditSpawnRange(Player player, ref int spawnRangeX, ref int spawnRangeY,
            ref int safeRangeX, ref int safeRangeY)
        {
            if (SubmundoGrimorio.Activo)
            {
                // sin fieras que cazar, el radio da igual — pero quedará
                // corto por si algo se cuela entre la niebla.
                spawnRangeX = 50;
                spawnRangeY = 35;
                safeRangeX = 50;
                safeRangeY = 35;
            }
        }
    }

    // ======================================================================
    //  EscenaInteriorGrimorio — LA MÚSICA DEL INTERIOR: dentro del libro
    //  suena EL HAMBRE (la oleada del grimorio — su tema de casa).
    // ======================================================================
    public class EscenaInteriorGrimorio : ModSceneEffect
    {
        public override SceneEffectPriority Priority => SceneEffectPriority.Event;

        public override int Music
        {
            get
            {
                try
                {
                    return MusicLoader.GetMusicSlot(Mod, "Music/OleadaHambre");
                }
                catch { return -1; }
            }
        }

        public override bool IsSceneEffectActive(Player player)
            => SubmundoGrimorio.Activo;
    }
}
