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
using static Terraria.ModLoader.ModContent;

namespace AethonMod.Content.Systems
{
    // ======================================================================
    //  SubmundoGrimorio — v6.114 — EL MUNDO INTERIOR DEL GRIMORIO ETERNO.
    //
    //  LA LETRA DEL USUARIO: «tambien quiero que investigues como crear
    //  mundos separados… necesito crear un mundo nuevo que sera el mundo
    //  interior del grimorio del eterno, estara lleno de bruma y de
    //  momento se puede entrar usando el altar».
    //
    //  LA INVESTIGACIÓN: se leyó la mecánica de SubworldLibrary (jjohnsnaill
    //  — sin licencia: SOLO la mecánica se aprendió, TODO este código es
    //  de la casa). El ciclo que la hace posible en SP:
    //    ENTRAR — menú 10 → guardar jugador y mundo principal →
    //      SystemLoader.OnWorldUnload → fijar dimensiones nuevas →
    //      WorldGen.setWorldSize + clearWorld → generar a mano →
    //      QueueMainThreadAction(Spawn + gameMenu=false).
    //    DENTRO — un hook en WorldFile.SaveWorld_bool_bool BLOQUEA el
    //      guardado (el interior NUNCA pisa el archivo del mundo real);
    //      el reloj se clava en la medianoche eterna.
    //    SALIR — menú 10 → guardar jugador (el mundo NO se guarda) →
    //      OnWorldUnload → recargar el mundo principal DESDE DISKCO →
    //      Spawn + devolver al jugador al altar donde entró.
    //
    //  EL MUNDO: 800×400 de vacío — un cielo sin sol (la medianoche
    //  eterna), islas flotantes de amatista encendida (AmethystGemspark
    //  — el cristal que BRILLA violeta) con el borde dorado de las
    //  páginas, la PLAZA DEL ALTAR al centro (con el altar de la
    //  consagración para VOLVER), y encima de todo EL VELO DE BRUMA:
    //  la interfaz más baja del juego pintada con la niebla violeta de
    //  la casa, las bandas de neblina corriendo, las motas a la deriva
    //  y EL OJO COLOSAL del grimorio mirando desde arriba — parpadea y
    //  SIGUE AL JUGADOR.
    //
    //  DE MOMENTO (la letra): SP solamente — en multijugador el altar
    //  explica que el interior solo se abre en mundo de un jugador.
    // ======================================================================
    public class SubmundoGrimorio : ModSystem
    {
        // === EL TAMAÑO DE LA CASA: 800×400 — compacto, TODO ≤8400×2400 ===
        private const int Ancho = 800;
        private const int Alto = 400;
        private const int Superficie = 60;      // las islas viven BAJO el cielo abierto
        private const int Roca = 160;
        private const int PlazaX = 400;
        private const int PlazaY = 208;

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
        }

        public override void Unload()
        {
            Terraria.IO.On_WorldFile.SaveWorld_bool_bool -= BloquearGuardado;
            Activo = false;
            Transicion = false;
            _mundoPrincipal = null;
            _softGlow = null;
            BrumaFaro = 0f;
            Revelado = 0;
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
        // ==================================================================
        private static void GenerarInterior()
        {
            // LA PLAZA DEL ALTAR — la isla mayor, la puerta de vuelta.
            Isla(PlazaX, PlazaY, 30, 7);

            // v6.115 — EL PRISMA DEL ETERNO en el corazón de la plaza
            // (la letra: «en el momento en que el jugador aparece en el
            // mundo debe aparecer un altar que sera un primsma de
            // colores, este tiene la capacidad de enviar de regreso al
            // jugador y ademas de invocar a aethon»): DE PIE sobre la
            // superficie — LA CURA DEL ENTIERRO (la superficie de la
            // plaza vive en PlazaY−7; el altar de la v6.114 nacía en
            // PlazaY−1, SEIS filas DENTRO de la roca: PlaceObject
            // fallaba en silencio y el interior quedaba SIN PUERTA).
            // El prisma ocupa PlazaY−10..PlazaY−8: su fila de abajo es
            // EL PRIMER AIRE y sus pies tocan el dorado de la página.
            try
            {
                WorldGen.PlaceObject(PlazaX, PlazaY - 8,
                    ModContent.TileType<Tiles.AltarPrismaTile>(), false, 0);
            }
            catch { }

            // EL CORAZÓN: una amatista colosal bajo la plaza (el latido).
            Isla(PlazaX, PlazaY + 26, 10, 5);

            // LAS ISLAS DE LAS PÁGINAS — el archipiélago flotante.
            Isla(250, 150, 18, 5);
            Isla(560, 160, 16, 5);
            Isla(170, 260, 14, 4);
            Isla(630, 250, 15, 4);
            Isla(330, 120, 12, 4);
            Isla(480, 110, 11, 4);
            Isla(210, 190, 10, 3);
            Isla(600, 200, 10, 3);
            Isla(400, 90, 13, 4);
            Isla(300, 300, 16, 5);
            Isla(500, 310, 14, 4);
            Isla(150, 330, 12, 4);
            Isla(650, 330, 12, 4);
            Isla(400, 360, 20, 6);

            // v6.115 — LAS ISLAS DEL CUERPO CÓSMICO (la letra: «ten
            // encuenta que es el mundo interno del grimorio asi que
            // crealo pensando en que el grimorio es una entidad cosmica
            // exterior todo poderosa»): EL ANILLO DE LA CORONA — seis
            // islas mayores en círculo amplio alrededor de la plaza
            // (los pilares del paladar) y EL RÍO DE PÁGINAS — la
            // constelación menuda que desciende hacia el corazón.
            Isla(PlazaX - 160, PlazaY - 70, 20, 6);
            Isla(PlazaX + 160, PlazaY - 70, 20, 6);
            Isla(PlazaX - 210, PlazaY + 60, 16, 5);
            Isla(PlazaX + 210, PlazaY + 60, 16, 5);
            Isla(PlazaX - 90, PlazaY + 120, 13, 4);
            Isla(PlazaX + 90, PlazaY + 120, 13, 4);
            Isla(PlazaX - 40, PlazaY - 120, 10, 3);
            Isla(PlazaX + 40, PlazaY - 120, 10, 3);
            Isla(PlazaX - 130, PlazaY - 20, 9, 3);
            Isla(PlazaX + 130, PlazaY - 20, 9, 3);

            // EL MAR DE NIEBLA: nada de líquidos — la bruma es el velo.
        }

        /// <summary>
        /// UNA ISLA DE LA CASA: la superficie DORADA (el borde de la
        /// página), el cuerpo de AMATISTA ENCENDIDA (AmethystGemspark —
        /// el cristal que ilumina violeta) y el perfil de lente.
        /// </summary>
        private static void Isla(int cx, int cy, int rx, int ry)
        {
            for (int dx = -rx; dx <= rx; dx++)
            {
                float f = 1f - (float)(dx * dx) / (float)(rx * rx);
                if (f <= 0f) continue;
                int alto = Math.Max(1, (int)MathF.Round(ry * MathF.Sqrt(f)));
                for (int dy = -alto; dy <= alto; dy++)
                {
                    int x = cx + dx, y = cy + dy;
                    if (x < 5 || x >= Ancho - 5 || y < 5 || y >= Alto - 5) continue;
                    Tile t = Main.tile[x, y];
                    t.HasTile = true;
                    // la PRIMERA fila de la columna: el ORO de la página.
                    t.TileType = dy == -alto ? TileID.GoldBrick : TileID.AmethystGemspark;
                    t.TileFrameX = 0;
                    t.TileFrameY = 0;
                }
            }
        }

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
        /// v6.115 — EL TINTE DEL ECLIPSE PERMANENTE: el mismo empuje
        /// morado de la oleada de hambre (su ambiente de día), llevado
        /// al fondo del vacío — el cielo Y la luz de los tiles viven
        /// todos hacia el violeta del eclipse (el hook oficial que
        /// tinta ColorOfTheSkies y la luz en el mismo pase).
        /// </summary>
        public override void ModifySunLightColor(ref Color tileColor, ref Color backgroundColor)
        {
            if (Main.netMode == NetmodeID.Server || !Activo) return;
            backgroundColor = Color.Lerp(backgroundColor, new Color(92, 38, 120), 0.92f);
            tileColor = Color.Lerp(tileColor, new Color(172, 112, 196), 0.52f);
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

            Player p = Main.LocalPlayer;
            if (p == null || !p.active) return;

            // LA LUZ DEL VACÍO: el portador nunca está a ciegas — el
            // interior lo abraza con un aura violeta tenue.
            try
            {
                Lighting.AddLight(p.Center, new Vector3(0.34f, 0.20f, 0.52f));

                // LA BRUMA VIVA: motas de niebla derivando por la pantalla
                // — pares grandes y tenues (el mar de niebla) + alguna
                // chispa pequeña más viva (el polvo de las páginas).
                if (Main.rand.NextBool(3))
                {
                    Vector2 pos = p.Center + new Vector2(
                        Main.rand.NextFloat(-0.62f, 0.62f) * Main.screenWidth,
                        Main.rand.NextFloat(-0.55f, 0.55f) * Main.screenHeight);
                    float deriva = Main.rand.NextFloat(0.25f, 0.7f) *
                        (Main.rand.NextBool() ? 1f : -1f);
                    Dust d = Dust.NewDustPerfect(pos, DustID.Smoke,
                        new Vector2(deriva, Main.rand.NextFloat(-0.05f, 0.08f)),
                        90, new Color(120, 84, 178), Main.rand.NextFloat(2.2f, 3.6f));
                    d.noGravity = true;
                    d.fadeIn = 0f;
                    d.alpha = 160;
                }
                if (Main.rand.NextBool(9))
                {
                    Vector2 pos = p.Center + new Vector2(
                        Main.rand.NextFloat(-0.6f, 0.6f) * Main.screenWidth,
                        Main.rand.NextFloat(-0.5f, 0.5f) * Main.screenHeight);
                    Dust d = Dust.NewDustPerfect(pos, DustID.Enchanted_Pink,
                        new Vector2(Main.rand.NextFloat(-0.3f, 0.3f),
                                    Main.rand.NextFloat(0.05f, 0.35f)),
                        130, new Color(190, 150, 240), Main.rand.NextFloat(0.6f, 1.1f));
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }

                // LA RED DE CAÍDA: quien resbala de las islas cae al vacío…
                // y el vacío LO DEVUELVE a la plaza (nadie muere en el
                // libro). v6.115 — al LADO del prisma (PlazaY−10: el
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
            DevolverSolEclipse();
            BrumaFaro = 0f;
            Revelado = 0;
        }

        // ==================================================================
        //  EL VELO DE BRUMA — la capa más baja de la interfaz: el interior
        //  ES la niebla. Sobre el mundo, bajo TODO lo demás.
        // ==================================================================
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            // v6.115 — el velo también vive en el mundo real mientras
            // EL FARO de la devoración inunda la pantalla (la letra:
            // «en el momento en que es devorado el mundo se llena de
            // bruma solo en la animacion»).
            if ((!Activo && BrumaFaro <= 0.01f) || Main.dedServ || Main.gameMenu) return;
            layers.Insert(0, new LegacyGameInterfaceLayer(
                "AethonMod: VeloInterior", DibujarVelo, InterfaceScaleType.None));
        }

        private static bool DibujarVelo()
        {
            try
            {
                if (Main.gameMenu || Main.dedServ) return true;
                if (_veloProbeFallos) return true;
                if (_softGlow == null || _softGlow.IsDisposed)
                {
                    try
                    {
                        _softGlow = ModContent.Request<Texture2D>(
                            "AethonMod/Content/Effects/Procedural/SoftGlow",
                            (AssetRequestMode)1).Value;
                    }
                    catch
                    {
                        _veloProbeFallos = true;
                        return true;
                    }
                }

                SpriteBatch sb = Main.spriteBatch;
                float w = Main.screenWidth;
                float h = Main.screenHeight;
                float t = (float)Main.time + (float)Main.GameUpdateCount * 0.016f;
                Vector2 centro = new Vector2(w * 0.5f, h * 0.5f);

                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);
                try
                {
                    // ============================================================
                    //  v6.115 — LA REVELACIÓN DE LA LLEGADA: el interior nace
                    //  LLENO de bruma y esta SE DISIPA revelando el mundo
                    //  (la letra). Dibujada ANTES del velo propio: la
                    //  niebla del nacimiento es MÁS densa y CORRE más
                    //  rápido, y EL IRIS DORADO de la entidad respira
                    //  abriéndose a mitad de la revelación.
                    // ============================================================
                    float aRev = Activo ? (float)Revelado / ReveladoMax : 0f;
                    if (aRev > 0.003f)
                    {
                        float k = aRev * aRev;   // ease-out: lenta al final
                        sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, (int)w, (int)h),
                            new Color(16, 8, 28) * (0.92f * k));
                        for (int banda = 0; banda < 5; banda++)
                        {
                            float vel = 30f + banda * 16f;
                            float y = h * (0.10f + banda * 0.19f) + MathF.Sin(t * 0.31f + banda * 1.7f) * h * 0.06f;
                            float x = ((t * vel + banda * 540f) % (w + 1400f)) - 700f;
                            float esc = 1.9f + banda * 0.55f;
                            sb.Draw(_softGlow,
                                new Rectangle((int)(x - 320f * esc), (int)(y - 70f * esc),
                                    (int)(640f * esc), (int)(140f * esc)),
                                null, new Color(110, 72, 176) * ((0.16f + 0.05f * banda) * k), 0f,
                                Vector2.Zero, SpriteEffects.None, 0f);
                        }
                        // EL IRIS DEL NACIMIENTO (el aliento dorado de la
                        // entidad a mitad de la revelación).
                        float abre = 1f - aRev;
                        float aIris = 0.85f * abre * (1f - abre) * 4f;
                        sb.Draw(_softGlow, new Rectangle(
                            (int)(w * 0.5f - w * (0.10f + 0.34f * abre)),
                            (int)(h * 0.17f - h * (0.08f + 0.16f * abre)),
                            (int)(w * (0.20f + 0.68f * abre)), (int)(h * (0.16f + 0.32f * abre))),
                            null, new Color(255, 214, 130) * (0.22f * aIris), 0f,
                            Vector2.Zero, SpriteEffects.None, 0f);
                    }

                    // ============================================================
                    //  EL VELO PROPIO DEL INTERIOR (solo dentro del libro)
                    // ============================================================
                    if (Activo)
                    {
                    // === EL FONDO DEL VACÍO (el degradé del velo) ===
                    sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, (int)w, (int)h),
                        new Color(22, 12, 38) * 0.60f);
                    // el horizonte más denso (el velo engorda hacia abajo).
                    sb.Draw(_softGlow, new Rectangle(-((int)w / 2), (int)(h * 0.55f), (int)w * 2, (int)(h * 0.9f)),
                        null, new Color(30, 16, 52) * 0.42f, 0f, Vector2.Zero, SpriteEffects.None, 0f);

                    // === LAS BANDAS DE NEBLINA (la bruma que CORRE) ===
                    for (int banda = 0; banda < 4; banda++)
                    {
                        float vel = 14f + banda * 9f;
                        float y = h * (0.18f + banda * 0.22f) + MathF.Sin(t * 0.23f + banda * 2.1f) * h * 0.05f;
                        float x = ((t * vel + banda * 700f) % (w + 1200f)) - 600f;
                        float esc = 1.6f + banda * 0.5f;
                        sb.Draw(_softGlow,
                            new Rectangle((int)(x - 320f * esc), (int)(y - 60f * esc),
                                (int)(640f * esc), (int)(120f * esc)),
                            null, new Color(96, 62, 158) * (0.10f + 0.045f * banda), 0f,
                            Vector2.Zero, SpriteEffects.None, 0f);
                    }

                    // === LAS ESTRELLAS DEL VACÍO (la niebla no las tapa: son
                    //     SUAS — brillan dentro del velo) ===
                    for (int s = 0; s < 26; s++)
                    {
                        uint hx = VFXCore.Hash01(s * 7 + 3, 0, 911) > 0.5f ? 1u : 0u;
                        float sx = VFXCore.Hash01(s * 13 + 1, 0, 913) * w;
                        float sy = VFXCore.Hash01(s * 17 + 5, 0, 917) * h * 0.72f;
                        float pal = 0.55f + 0.45f * MathF.Sin(t * (0.5f + VFXCore.Hash01(s, 0, 919) * 1.3f) + s);
                        sb.Draw(TextureAssets.MagicPixel.Value,
                            new Rectangle((int)sx, (int)sy, 2 + (int)hx, 2),
                            new Color(210, 190, 250) * (0.10f + 0.14f * pal));
                    }

                    // === EL OJO COLOSAL (el que lee desde arriba — corona
                    //     del sol del eclipse, clavado en el cenit) ===
                    DibujarElOjo(sb, w, h, t, centro);
                    } // fin del velo propio

                    // ============================================================
                    //  v6.115 — EL FARO DE LA DEVORACIÓN: la bruma que
                    //  INUNDA la pantalla mientras el libro traga al
                    //  portador (vive TAMBIÉN en el mundo real — la
                    //  transición va escondida detrás). ES lo último que
                    //  se dibuja: cuando el faro está lleno, no hay mundo.
                    // ============================================================
                    if (BrumaFaro > 0.01f)
                    {
                        float a = BrumaFaro;
                        sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, (int)w, (int)h),
                            new Color(18, 9, 32) * (0.97f * a));
                        for (int banda = 0; banda < 6; banda++)
                        {
                            // la bruma ACELERA con el trago (0.6→1.4×).
                            float vel = (46f + banda * 20f) * (0.6f + 0.8f * a);
                            float y = h * (0.06f + banda * 0.16f) + MathF.Sin(t * 0.4f + banda * 1.3f) * h * 0.05f;
                            float x = ((t * vel + banda * 500f) % (w + 1500f)) - 750f;
                            float esc = 2.1f + banda * 0.6f;
                            sb.Draw(_softGlow,
                                new Rectangle((int)(x - 320f * esc), (int)(y - 80f * esc),
                                    (int)(640f * esc), (int)(160f * esc)),
                                null, new Color(96, 62, 158) * ((0.14f + 0.05f * banda) * a), 0f,
                                Vector2.Zero, SpriteEffects.None, 0f);
                        }
                    }
                }
                finally
                {
                    sb.End();
                }
            }
            catch
            {
                _veloProbeFallos = true; // el velo jamás rompe el render
            }
            return true;
        }

        /// <summary>
        /// EL OJO COLOSAL DEL GRIMORIO: la almendra pálida arriba en el
        /// centro del cielo, la pupila vertical que SIGUE al jugador y el
        /// PARPADEO lento — el libro te está leyendo.
        /// </summary>
        private static void DibujarElOjo(SpriteBatch sb, float w, float h, float t, Vector2 centroPantalla)
        {
            float cx = w * 0.5f;
            float cy = h * 0.17f;

            // EL PARPADEO: cada ~8 s el ojo se cierra un instante.
            float ciclo = t % 8.5f;
            float apertura = 1f;
            if (ciclo > 7.9f)
            {
                float f = (ciclo - 7.9f) / 0.6f;              // 0→1→0
                float pliegue = MathF.Sin(MathF.PI * f);       // campana
                apertura = 1f - 0.97f * pliegue;
            }
            if (apertura < 0.03f) return;

            // LA ALMENDRA: el resplandor tenue + el blanco del ojo.
            sb.Draw(_softGlow, new Rectangle((int)(cx - 460f), (int)(cy - 150f), 920, 300),
                null, new Color(84, 52, 140) * (0.30f * apertura), 0f, Vector2.Zero, SpriteEffects.None, 0f);
            sb.Draw(_softGlow, new Rectangle((int)(cx - 300f), (int)(cy - 62f * apertura), 600, (int)(124f * apertura)),
                null, new Color(196, 172, 235) * (0.30f * apertura), 0f, Vector2.Zero, SpriteEffects.None, 0f);
            sb.Draw(_softGlow, new Rectangle((int)(cx - 250f), (int)(cy - 44f * apertura), 500, (int)(88f * apertura)),
                null, new Color(224, 206, 252) * (0.34f * apertura), 0f, Vector2.Zero, SpriteEffects.None, 0f);

            // LA PUPILA: vertical, oscura — MIRA AL JUGADOR (su posición
            // en pantalla inclina la mirada).
            float mirada = MathF.Sin(t * 0.13f) * 26f +
                MathHelper.Clamp((centroPantalla.X - cx) * 0.05f, -80f, 80f);
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle((int)(cx + mirada - 13f), (int)(cy - 54f * apertura), 26, (int)(108f * apertura)),
                new Color(16, 8, 28) * (0.88f * apertura));
            // el brillo de la córnea alrededor de la pupila.
            sb.Draw(_softGlow, new Rectangle((int)(cx + mirada - 60f), (int)(cy - 40f * apertura), 120, (int)(80f * apertura)),
                null, new Color(150, 84, 235) * (0.20f * apertura), 0f, Vector2.Zero, SpriteEffects.None, 0f);
        }
    }

    // ======================================================================
    //  InteriorSpawnGate — EL SILENCIO DE LAS FIERAS (v6.114): dentro del
    //  libro NO despierta bestia alguna — el interior es el libro y sus
    //  páginas: la soledad es parte del horror. (El hook de la casa:
    //  EditSpawnRate/EditSpawnRange viven en GlobalNPC — verificado por
    //  reflexión contra el tML 2026.09.3.1 real.)
    // ======================================================================
    public class InteriorSpawnGate : GlobalNPC
    {
        public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
        {
            if (SubmundoGrimorio.Activo)
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
