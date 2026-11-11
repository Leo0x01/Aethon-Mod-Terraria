using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
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
    //  SubmundoGrimorio — v6.120 — EL MUNDO INTERIOR DEL GRIMORIO ETERNO:
    //  EL LLANO DE OBSIDIANA Y SU PILAR, EN ECLIPSE PERMANENTE Y BAJO EL
    //  CICLO ETERNO DE LAS OLEADAS DEL HAMBRE.
    //
    //  LA LETRA DEL REDISEÑO (v6.120): «el mundo interior debe ser plano,
    //  solo un mundo plano de obsidiana, el fondo deben ser capas de
    //  tentaculos en loop y al menos 3 capas» + «necesitas crear un pilar
    //  tipo prisma altar que permita salir… el pilar debe aparecer ya
    //  colocado en el mundo interior» + «crea 3 banda sonoras que sonaran
    //  aleatoriamente». CÓMO SERÍA EL INTERIOR DE UNA ENTIDAD CÓSMICA:
    //  LA PÁGINA NEGRA INFINITA — el cuerpo del dios destilado a UN
    //  LLANO de obsidiana que no acaba nunca (la obsidiana es la carne
    //  CONGELADA de lo devorado: todo lo que el libro tragó, apagado y
    //  comprimido en UNA verdad plana), EL CIELO de tentáculos EN LOOP
    //  (CieloInteriorEterno — el cuerpo VIVO que sigue moviéndose
    //  alrededor en TRES capas que se repiten sin costura: el dios te
    //  envuelve entero mientras caminas su página), EL PILAR PRISMA
    //  (la única torre del llano — el hueso de la puerta, ya colocado
    //  junto al nacimiento) y LAS TRES BANDAS de la casa (el sueño, la
    //  garganta y el corazón — tres caras del mismo dios, al azar).
    //
    //  (El rework v6.118 del terreno continuo, la geoda y las vetas
    //  MURIÓ con esta letra — el llano es el cuerpo entero del dios.)
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
        // === EL TAMAÑO DE LA CASA: 800×600 — LA LEY DE LAS SECCIONES
        // (v6.117, leída en el IL del tML 2026.9.3.1 real: el minimapa
        // camina el mundo en SECCIONES DE 200×150 TILES y TODOS los
        // mundos de vanilla son múltiplos exactos — 800 = 4×200 EXACTO
        // y 600 = 4×150 EXACTO: el interior nace con secciones
        // PERFECTAS; NI UN TILE menos ni uno más). ===
        //
        // v6.124 — EL ALTO NUEVA: 450 → 600. LA AUTOPSIA DEL CUADRO
        // NEGRO (la letra: «el fondo negro permanece en el mundo
        // interior» + «desaparece al volar alto y reaparece al bajar»):
        // NO era el fondo de cueva (la cura v6.123 escondió las piezas
        // tras la obsidiana — verificado), era EL HELL BLACK BOX DE
        // VANILLA: Main.DrawBackground (decompile REAL del tML
        // 2026.09.3.1, líneas del método) dibuja
        // `spriteBatch.Draw(BlackTile, Rectangle(0, 0, screenWidth+100,
        // screenHeight+200))` — UN RECTÁNGULO NEGRO DE PANTALLA COMPLETA
        // — cuando `num2*16 <= screenPosition.Y + screenHeight`, donde
        // num2 = worldSurface + ((maxTilesY−330−worldSurface)/6)*6 − 5
        // es LA LÍNEA DEL CIELO DEL INFIERNO. Con Alto=450 la línea caía
        // en el tile ~117 — ¡ARRIBA DEL LLANO (140)!: parado en el
        // suelo la cámara SIEMPRE la cruzaba → la caja negra tapaba el
        // cielo entero; volando ~30 tiles la cámara dejaba de cruzarla
        // → el cuadro moría al subir y RENACÍA al bajar, EXACTO el
        // síntoma. LA CURA GEOMÉTRICA DEFINITIVA: con Alto=600 la línea
        // del infierno cae en el tile 261 (4176 px — a 121 tiles BAJO el
        // llano): la caja JAMÁS se dibuja en el alcance de la cámara
        // (el fondo del pozo es obsidiana sólida y nadie la talla), y el
        // UnderworldLayer de vanilla (maxTilesY−200 = 400) queda igual
        // fuera de alcance. La LEY DE SECCIONES sigue VIVA: 600 = 4×150
        // EXACTO. El llano, el pilar y el nacimiento NO SE MUEVEN (la
        // obsidiana sólida de 140 a 595: el cuerpo del dios es MÁS
        // HONDO, nada más).
        private const int Ancho = 800;
        private const int Alto = 600;
        // v6.120 — EL LLANO DE OBSIDIANA (la letra: «el mundo interior
        // debe ser plano, solo un mundo plano de obsidiana»): la
        // superficie del cuerpo es UNA LÍNEA RECTA de obsidiana a
        // y = LlanoY — ni colinas ni plaza ni geoda: SOLO el llano y su
        // pilar.
        // v6.123 → v6.124 — LA CURA DEL CUADRO NEGRO, LA HISTORIA
        // COMPLETA: la v6.123 escondió las piezas de cueva tras la
        // obsidiana (worldSurface = LlanoY — correcto y NECESARIO,
        // verificado con el decompile REAL del tML 2026.09.3.1)… pero
        // el cuadro SIGUIÓ porque el culpable era OTRO pintor: EL HELL
        // BLACK BOX (la autopsia completa vive en el comentario del
        // Alto, arriba). La cura v6.124 = EL MUNDO MÁS HONDO (600):
        // la línea del infierno BAJA al tile 261 y la caja JAMÁS entra
        // al alcance de la cámara. worldSurface sigue CLAVADO EN LA
        // PRIMERA FILA SÓLIDA DEL LLANO (las piezas de cueva nacen
        // donde empieza la obsidiana y quedan OCULTAS TRAS LOS TILES)
        // y rockLayer un pelo más abajo — los DOS pintores muertos.
        private const int Superficie = LlanoY;          // la línea del llano MISMA
        private const int Roca = LlanoY + 2;            // la zona honda nace bajo la piel
        private const int LlanoY = 140;     // la PRIMERA FILA SÓLIDA del llano
        private const int PlazaX = 400;    // donde vive EL PILAR PRISMA
        private const int SpawnX = 405;    // el nacimiento: AL LADO del pilar

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

        // ================================================================
        //  v6.119 — LA PUERTA DEL ALTAR (la letra: «al consagrar 1000
        //  esencias el altar se pueda ir al mundo interior»): el
        //  ALTAR DE LA CONSAGRACIÓN acumula TODAS las esencias que el
        //  Eterno le ha cobrado por sellar ofrendas — y cuando la
        //  cuenta llega a LAS MIL (el mismo número sagrado del ítem y
        //  del tope del festín del portador), EL ALTAR MISMO SE VUELVE
        //  PUERTA: su ventana abre EL BOTÓN que desata LA DEVORACIÓN
        //  (la misma película del ítem — el libro baja, se abre y
        //  traga al portador). La cuenta vive con el MUNDO (se
        //  consagra en un altar, se abre en todos: la voluntad del
        //  libro es del mundo, no del mueble) y el ítem
        //  «MundoInteriorEternoItem» SIGUE siendo la puerta portátil
        //  de siempre (las DOS puertas, la de la bolsa y la del
        //  altar).
        // ================================================================

        /// <summary>EL NÚMERO SAGRADO de la puerta del altar: las MIL
        /// esencias consagradas que abren el interior.</summary>
        public const int EsenciasPuerta = 1000;

        /// <summary>Las esencias que EL ETERNO ha cobrado YA en el altar
        /// de este mundo (consagración tras consagración — la cuenta
        /// acumulada, persistida con el mundo).</summary>
        public static int EsenciasConsagradas;

        /// <summary>¿La puerta del altar está YA abierta? (las mil
        /// esencias pagadas: el botón del interior vive en la ventana
        /// del altar).</summary>
        public static bool PuertaAbierta => EsenciasConsagradas >= EsenciasPuerta;

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
            // v6.119 — la cuenta del altar muere con la descarga (vive
            // con el MUNDO: al recargar, LoadWorldData la devuelve).
            EsenciasConsagradas = 0;
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
                // v6.124 — LA LECCIÓN DE SUBWORLDMODUCTION (la letra:
                // «investiga como funciona la creacion de mundos en otros
                // mods y en la libreria de la cual nos inspiramos»): la
                // SubworldLibrary llama Main.ToggleGameplayUpdates(false)
                // ANTES de tocar el mundo (LoadWorld) — sin ella, el hilo
                // principal SIGUE actualizando el juego (NPCs, proyectiles,
                // iluminación) sobre el mundo VIEJO mientras el hilo de
                // fondo lo borra y lo regenera: LA CARRERA que hacía que
                // a veces la puerta no abriera (excepciones aleatorias →
                // VolverADiscoPorFallo → el intento muere en silencio).
                // Ahora el mundo se congela ANTES del primer byte.
                Main.ToggleGameplayUpdates(false);
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
                // v6.120 — LA MONEDA DE LA MÚSICA nace vírgen en cada
                // visita (EscenaInteriorGrimorio: la banda suelta al azar).
                try { EscenaInteriorGrimorio.AlEntrar(); } catch { }
                Main.rand = new UnifiedRandom((int)DateTime.Now.Ticks);
                WorldGen.gen = true;
                WorldGen.loadFailed = false;
                WorldGen.loadSuccess = false;

                Main.maxTilesX = Ancho;
                Main.maxTilesY = Alto;
                // v6.120 — EL NACIMIENTO EN EL LLANO: al LADO del pilar
                // (SpawnX, en el aire limpio SOBRE la línea del llano —
                // cae dos tiles y PISA la obsidiana; el pilar está a la
                // vista, la salida NUNCA se pierde).
                Main.spawnTileX = SpawnX;
                Main.spawnTileY = LlanoY - 4;
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
                // v6.124 — LA ESPERA DEL CERROJO DEL MAPA (SubworldLibrary,
                // LoadWorld): si una carga del minimapa del mundo viejo
                // sigue CORRIENDO en el hilo de fondo, se espera — jamás
                // se le escribe encima a un mapa cargándose a medias
                // (la otra mitad de la entrada intermitente).
                try
                {
                    int paciencia = 0;
                    while (Main.mapEnabled && Main.loadMapLock && paciencia++ < 6000)
                        Thread.Sleep(0);
                }
                catch { }

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
                    // v6.124 — el mundo vuelve a RESPONDER en el mismo
                    // tick en que el portador despierta dentro (la
                    // lámpara de SubworldLibrary se apaga AQUÍ — vanilla
                    // la enciende en Player.EnterWorld, pero el interior
                    // entra por Spawn: se enciende a mano, EXACTO donde
                    // la carrera terminó).
                    Main.ToggleGameplayUpdates(true);
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
                // v6.124 — LA MISMA LÁMPARA AL VOLVER: el gameplay se
                // congela antes de tocar el disco (la lección de
                // SubworldLibrary, aplicada a la salida).
                Main.ToggleGameplayUpdates(false);
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
                        // v6.121 — EL MAPA RENACE EN LAS DIMENSIONES DEL
                        // MUNDO REAL antes de recargar. LA AUTOPSIA del
                        // client.log: el objeto Main.Map seguía siendo el
                        // del interior (WorldMap 800×450) y el .map del
                        // mundo real trae entradas hasta 4200×1200 →
                        // WorldMap.SetTile fuera de rango → la caída al
                        // disco POR UN MAPA. Renace del tamaño correcto
                        // y LUEGO carga; y si aun así el archivo viene
                        // roto: mapa limpio y a jugar (nada de crash).
                        try { Main.Map = new WorldMap((ushort)Main.maxTilesX, (ushort)Main.maxTilesY); } catch { }
                        try { Main.Map.Load(); }
                        catch
                        {
                            try { Main.Map = new WorldMap((ushort)Main.maxTilesX, (ushort)Main.maxTilesY); } catch { }
                        }
                    }
                    // v6.117 — el minimapa también nace limpio AL VOLVER
                    // (las regiones del interior mueren aquí — la cura
                    // simétrica; clearMap además BARRE los render targets
                    // para que ni un píxel del interior fantasmee en el
                    // mapa del mundo real).
                    ReiniciarMiniMapa();
                    Main.sectionManager.SetAllSectionsLoaded();
                    // v6.124 — LA ESPERA DEL CERROJO (SubworldLibrary):
                    // el mapa del mundo real termina de cargar ANTES de
                    // ceder el turno — nada se escribe sobre un mapa a
                    // medias.
                    try
                    {
                        int paciencia = 0;
                        while (Main.mapEnabled && Main.loadMapLock && paciencia++ < 6000)
                            Thread.Sleep(0);
                    }
                    catch { }
                }

                Main.QueueMainThreadAction(delegate
                {
                    // v6.124 — el gameplay revive con el portador.
                    Main.ToggleGameplayUpdates(true);
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
        /// v6.121 — EL BLINDAJE DEL MINIMAPA (la cura del crash del
        /// client.log: Main engine crash en DrawToMap_Section →
        /// IndexOutOfRangeException al volver a cargar «Chaparral
        /// Mustio de Platino» después de visitar el interior). LA
        /// AUTOPSIA: el jugador entró al interior (Main.Map =
        /// WorldMap(800, 450)), ese objeto SOBREVIVIÓ al menú (los
        /// rebuilds del mod no tocan los statics de vanilla) y al
        /// recargar el mundo real (4200×1200) el Map.Load() llenó el
        /// mapa CHICO con entradas GRANDES → SetTile fuera de rango
        /// (la «Excepción silenciosa» del log) → DrawToMap_Section leyó
        /// Map[x, y] más allá del array → crash fatal. LA CURA: cada
        /// frame (en PreUpdateWorld, ANTES del Draw) se SONDEA la
        /// esquina del mapa con el MISMO indexador que crashea — si el
        /// mapa no cubre el mundo actual, RENACE limpio en las
        /// dimensiones correctas. Un mapa corrupto ya no puede tumbar
        /// el juego: se pierde la exploración del minimapa, JAMÁS la
        /// partida.
        /// </summary>
        private static bool _mapaBlindado;

        private static void BlindarMapa()
        {
            // nada que blindar en el menú, en el servidor o sin minimapa
            if (Main.gameMenu || Main.netMode == 2 || !Main.mapEnabled) return;
            try
            {
                // (1) LA SONDA DE LA ESQUINA: el indexador crudo, el
                //     mismo que crashea en DrawToMap_Section — si el
                //     mapa cubre el mundo, aquí no pasa nada (una
                //     lectura); si es más chico, dispara y lo cazamos.
                _ = Main.Map[Main.maxTilesX - 1, Main.maxTilesY - 1];
                // (2) las regiones de actualización jamás mayores que
                //     el mundo (el caminante espiral de DrawToMap no
                //     puede salirse del array)
                if (Main.mapMaxX > Main.maxTilesX || Main.mapMaxY > Main.maxTilesY)
                    ReiniciarMiniMapa();
            }
            catch
            {
                // EL MAPA ESTÁ ROTO: renace limpio en las dimensiones
                // del mundo actual (la exploración se rehace caminando;
                // un crash fatal jamás)
                try { Main.Map = new WorldMap((ushort)Main.maxTilesX, (ushort)Main.maxTilesY); } catch { }
                ReiniciarMiniMapa();
                if (!_mapaBlindado)
                {
                    _mapaBlindado = true;
                    try
                    {
                        ModContent.GetInstance<SubmundoGrimorio>().Mod.Logger.Info(
                            "[SubmundoGrimorio/BlindarMapa] el mapa sobreviviente era más chico " +
                            "que el mundo — renació limpio (la cura del crash del client.log v6.121)");
                    }
                    catch { }
                }
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
            // v6.124 — LA PUERTA NUNCA MUERE EN SILENCIO (la letra: «a
            // veces no entra al mundo interior y tengo que intentarlo
            // varias veces»): el fallo ahora SE VE — el portador sabe
            // que el intento murió y que la puerta puede volver a
            // abrirse (Transicion ya está libre y el juego responde:
            // ToggleGameplayUpdates(true) ANTES de soltar el mundo).
            try { Main.ToggleGameplayUpdates(true); } catch { }
            try
            {
                Main.QueueMainThreadAction(delegate
                {
                    try
                    {
                        Main.NewText(Language.GetTextValue(
                            "Mods.AethonMod.Submundo.FalloPuerta"),
                            new Color(255, 120, 120));
                    }
                    catch { }
                });
            }
            catch { }
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
        //  LA GENERACIÓN — v6.120 — EL LLANO DE OBSIDIANA (la letra: «el
        //  mundo interior debe ser plano, solo un mundo plano de
        //  obsidiana… el pilar debe aparecer ya colocado en el mundo
        //  interior»). EL CUERPO DEL DIOS, DESTILADO: una página negra
        //  infinita — LA LÍNEA RECTA de obsidiana de borde a borde (el
        //  cuerpo entero ES el llano) y EL PILAR PRISMA ya parado en
        //  él, al lado del nacimiento. Nada más: la vastedad ES el
        //  diseño — el cielo de tentáculos en loop (CieloInteriorEterno)
        //  es el mundo entero, y el llano es la única verdad sólida.
        //
        //  LA LECCIÓN QUE CURÓ EL «NO PUEDO SALIR»: el prisma viejo se
        //  «colocaba» ANTES del terreno — TileObject.CanPlace valida
        //  AnchorBottom sobre el mundo VACÍO y fallaba EN SILENCIO:
        //  el interior nacía SIN PUERTA. EL PILAR SE COLOCA ÚLTIMO
        //  (DESPUÉS del llano): el obsidiana YA es sólido bajo sus
        //  pies — CanPlace dice SÍ y la puerta NACE en cada visita.
        // ==================================================================
        private static void GenerarInterior()
        {
            // ==============================================================
            //  (1) EL LLANO — LA LÍNEA RECTA: obsidiana pura de borde a
            //  borde, desde LlanoY hasta el fondo del mundo (la letra:
            //  «solo un mundo plano de obsidiana» — ni colinas, ni
                       //  plaza, ni geoda, ni vetas: el llano ES el cuerpo).
            // ==============================================================
            for (int x = 5; x < Ancho - 5; x++)
            {
                for (int y = LlanoY; y < Alto - 5; y++)
                {
                    Tile t = Main.tile[x, y];
                    t.HasTile = true;
                    t.TileType = TileID.Obsidian;
                    t.TileFrameX = 0;
                    t.TileFrameY = 0;
                }
            }

            // ==============================================================
            //  (2) EL PILAR PRISMA — YA COLOCADO, al lado del nacimiento
            //  (la letra: «el pilar debe aparecer ya colocado en el
            //  mundo interior»). DESPUÉS del llano — la cura del «no
            //  puedo salir»: el ancla de abajo pisa obsidiana SÓLIDA
            //  y TileObject.CanPlace pasa (el prisma viejo corría
            //  sobre el mundo VACÍO y fallaba en silencio). Con Origin
            //  (1,4), PlaceObject(PlazaX, LlanoY−1) lo para con su
            //  fila de abajo en EL PRIMER AIRE sobre el llano (las
            //  filas LlanoY−5..LlanoY−1: un pilar de CINCO tiles de
            //  alto, la salida que se lee desde lejos).
            // ==============================================================
            try
            {
                WorldGen.PlaceObject(PlazaX, LlanoY - 1,
                    ModContent.TileType<Tiles.PilarPrismaTile>(), false, 0);
            }
            catch { }

            // ==============================================================
            //  (3) LOS RESPIRADEROS — las cinco bocas del llano (la
            //  bruma que ASCIENende y las motas de oro que nacen —
            //  SIEMPRE las mismas: el dios respira igual). v6.120: el
            //  llano NO se excava (la letra: SOLO obsidiana) — los
            //  respiraderos son POSICIONES sobre la línea (las
            //  columnas suben DESDE la superficie; las motas nacen a
            //  ras del cuerpo).
            // ==============================================================
            _vents = new Vector2[5];
            int vi = 0;
            for (int k = 0; k < 5; k++)
            {
                int x = 90 + k * 155 + (int)(VFXCore.Hash01(k * 31 + 7, 1, 6130) * 60f) - 30;
                if (x < 8 || x >= Ancho - 8) x = 90 + k * 155;
                if (Math.Abs(x - PlazaX) < 40) x = PlazaX + (x < PlazaX ? -70 : 70);  // lejos del pilar
                if (x < 8 || x >= Ancho - 8) continue;
                _vents[vi++] = new Vector2(x * 16f + 8f, LlanoY * 16f - 4f);
            }
            if (vi < _vents.Length) System.Array.Resize(ref _vents, vi);

            // EL MAR DE NIEBLA: nada de líquidos — la bruma es el velo.
        }

        // (LA ISLA FLOTANTE murió con la v6.118 · EL TERRENO CONTINUO, la
        // geoda y las vetas murieron con la v6.120 — el LLANO de
        // obsidiana de arriba es el cuerpo entero del dios.)

        // ==================================================================
        //  LA VIDA DEL INTERIOR — la medianoche eterna, la bruma viva, la
        //  luz del vacío y la red de caída.
        // ==================================================================
        public override void PreUpdateWorld()
        {
            // v6.121 — EL BLINDAJE DEL MINIMAPA corre SIEMPRE (también
            // en el mundo real: el crash del client.log ocurrió AL
            // VOLVER del interior — el objeto mapa chico sobrevivió al
            // menú y el mundo grande lo pisó). Corre ANTES del Draw del
            // frame: si el mapa está roto, este frame lo cura.
            BlindarMapa();

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
                // vacío… y el vacío LO DEVUELVE al llano (nadie muere en
                // el libro). v6.120 — al LADO DEL PILAR (el aire limpio
                // sobre el llano, en el nacimiento: la salida a la vista).
                if (p.position.Y > (Alto - 12) * 16f)
                {
                    p.Center = new Vector2(SpawnX * 16f + 8f, (LlanoY - 8) * 16f);
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
            // v6.119 — OJO: EsenciasConsagradas NO se toca aquí — la
            // cuenta del altar es del MUNDO REAL y este hook también
            // corre al ENTRAR al interior (SystemLoader.OnWorldUnload
            // de CallbackEntrar): la puerta abierta sigue abierta al
            // volver a casa (la voluntad del libro es del mundo).
        }

        // ================================================================
        //  v6.119 — LA PERSISTENCIA DE LA PUERTA: la cuenta de las
        //  esencias consagradas en el altar vive con el ARCHIVO del
        //  mundo real (SaveWorldData corre en cada guardado del mundo
        //  — y el cerrojo del interior (BloquearGuardado) garantiza
        //  que NUNCA se guarda desde adentro: la cuenta del mundo
        //  real jamás se contamina con el archivo falso del interior).
        // ================================================================
        public override void SaveWorldData(Terraria.ModLoader.IO.TagCompound tag)
        {
            try { tag["esenciasConsagradasAltar"] = EsenciasConsagradas; }
            catch { }
        }

        public override void LoadWorldData(Terraria.ModLoader.IO.TagCompound tag)
        {
            EsenciasConsagradas = 0;
            try
            {
                var n = tag.Get<int>("esenciasConsagradasAltar");
                if (n > 0) EsenciasConsagradas = n;
            }
            catch { }
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
                    // v6.121 — LA REVELACIÓN NACE CASI OPACA (la letra: al
                    // entrar, una bruma a pantalla completa TAPA CUALQUIER
                    // error de carga): la base vieja (0,66 en el pico)
                    // dejaba VER el mundo medio-formado A TRAVÉS de la
                    // niebla — el fondo negro sin cielo, el terreno
                    // pop-in, el grimorio viejo. Ahora el nacimiento es
                    // 0,97 de opacidad (nada se ve) y se abre POCO a
                    // poco con la disipación.
                    Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle(0, 0, (int)w, (int)h),
                        new Color(16, 8, 28) * ((0.35f + 0.62f * k) * 1f));

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

                    // v6.122 — EL IRIS DEL NACIMIENTO MURIÓ: el óvalo
                    // dorado que crecía y desaparecía al entrar al
                    // interior era EL VESTIGIO del viejo ojo colosal
                    // (la letra: «pienso que sea un vestigio de un
                    // supuesto ojo que querías crear y no está bien
                    // borrado» — exactamente). Extirpado: la llegada
                    // queda solo con las masas violetas de la
                    // revelación, SIN ojo fantasma.
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
                        new Color(18, 9, 32) * (0.97f * a));

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
        //
        //  v6.119 — EL CONTRATO DEL LOTE DE PostDrawTiles (LA CURA DEL
        //  CRASH DEL client.log: «Begin has been called before calling
        //  End» en DrawSuperSpecialProjectiles → Main engine crash
        //  justo al entrar al interior). Leído en el IL del tML
        //  2026.09.3.1 real, el pase de Main.DoDraw es EXACTO:
        //      spriteBatch.End();                        ← vanilla CIERRA
        //      SystemLoader.PostDrawTiles();            ← ESTE hook
        //      TimeLogger.DetailedDrawTime(35);
        //      SortDrawCacheWorms();
        //      DrawSuperSpecialProjectiles(DrawCacheFirstFractals)
        //          → spriteBatch.Begin(...)              ← vanilla ABRE
        //  El hook corre con el lote CERRADO y vanilla espera CERRADO
        //  a la salida (DrawSuperSpecialProjectiles abre el suyo). El
        //  finally de la v6.118 llamaba a ReabrirLoteVanilla() — que
        //  RE-ABRE el lote cuando no hay uno vivo — y el frame salía
        //  con un Begin HUÉRFANO: el Begin de vanilla reventaba
        //  (InvalidOperationException) y el motor moría EN EL PRIMER
        //  frame del interior (el guard «!Activo» lo explicaba todo:
        //  en el mundo real este hook jamás corría). LA CURA: salir
        //  SIEMPRE CERRADO — CerrarLoteSiAbierto en el finally (el
        //  patrón del RayoLib.PostDrawTiles de la casa: quien sigue
        //  abre su propio lote). El re-abrir queda para los PreDraw
        //  (el lote ABIERTO del pase de entidades) — jamás aquí.
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
                // v6.119 — LA SALIDA CERRADA (la cura del crash): el
                // lote vuelve CERRADO — DrawSuperSpecialProjectiles lo
                // abre él mismo justo después de este hook. Si algo
                // ajeno dejara un Begin vivo, la sonda lo caza aquí.
                finally { VFXCore.CerrarLoteSiAbierto(); }
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
    //  EscenaInteriorGrimorio — v6.120 — LAS TRES BANDAS EXCLUSIVAS DEL
    //  INTERIOR (la letra: «crea una banda sonora exclusiva para el
    //  mundo interior, crea 3 banda sonoras que sonaran aleatoriamente»).
    //
    //  EL REPERTORIO (las tres caras del mismo dios — 100 % síntesis de
    //  la casa, tools/gen_musica_interior_v6120.py):
    //   · «EL SUEÑO DEL VACÍO» (Music/MundoSuenoVacio.ogg, 58 BPM): el
    //     dron sub, los pads violetas, el coro lejano, la campana suelta
    //     y EL LATIDO lento — el dios dormido entre mundos devorados.
    //   · «LA GARGANTA DE PÁGINAS» (Music/MundoGarganta.ogg, 92 BPM):
    //     los susurros con formantes, el arpa pizzicato y los glissandi
    //     que caen — el tragadero hablando mientras algo baja.
    //   · «EL CORAZÓN COLOSAL» (Music/MundoCorazon.ogg, 120 BPM): los
    //     taikos EN el latido, el corno, el coro solemne y los
    //     timbales — el órgano que todo lo mueve.
    //
    //  LA MONEDA (el patrón de OleadaHambreMusica): al ENTRAR al
    //  interior se tira UNA de las tres — y cada ~2,5 min de estancia la
    //  moneda VUELVE A TIRARSE (sin repetir la inmediata: las tres
    //  rotan de verdad en una sesión larga; el crossfade lo maneja el
    //  motor de vanilla al cambiar el slot). Prioridad Event: durante
    //  el festín, la oleada (BossHigh) manda como siempre.
    // ======================================================================
    public class EscenaInteriorGrimorio : ModSceneEffect
    {
        public override SceneEffectPriority Priority => SceneEffectPriority.Event;

        /// <summary>La pista que suena AHORA (slot de MusicLoader; 0 si
        /// aún no se ha tirado la moneda).</summary>
        private static int _slotActual;

        /// <summary>La ÚLTIMA que sonó (para no repetir la inmediata).</summary>
        private static int _slotPrevio;

        /// <summary>EL RELOJ ABSOLUTO del re-tiro (el tick de juego en el
        /// que la moneda vuelve a tirarse — 0 = tira YA). El reloj vive en
        /// el getter (ModSceneEffect de esta era NO trae hook Update: la
        /// moneda se consulta en cada frame del interior y el re-tiro es
        /// una comparación de tick absoluto).</summary>
        private static long _proximoTiro;

        /// <summary>El intervalo del re-tiro (~2,5 min — comparable a la
        /// duración de las piezas: la estancia larga oye DOS o TRES).</summary>
        private const long TicksMoneda = 60 * 60 * 5 / 2;

        public override int Music
        {
            get
            {
                try
                {
                    int a = MusicLoader.GetMusicSlot(Mod, "Music/MundoSuenoVacio");
                    int b = MusicLoader.GetMusicSlot(Mod, "Music/MundoGarganta");
                    int c = MusicLoader.GetMusicSlot(Mod, "Music/MundoCorazon");
                    // LA MONEDA: solo las que CARGARON de verdad
                    int[] vivas = System.Array.FindAll(new[] { a, b, c }, s => s > 0);
                    if (vivas.Length == 0) return MusicLoader.GetMusicSlot(Mod, "Music/OleadaHambre");
                    // EL RELOJ DEL RE-TIRO: la primera lectura (o la que cruza
                    // el intervalo) TIRA LA MONEDA — UNA al azar que NO sea
                    // la inmediata anterior (con 2 vivas: la otra; con 3:
                    // cualquiera de las otras dos)
                    long ahora = (long)Main.GameUpdateCount;
                    bool hayQueTirar = _slotActual <= 0 || ahora >= _proximoTiro;
                    if (hayQueTirar)
                    {
                        if (_slotActual > 0) _slotPrevio = _slotActual;
                        int[] sinRepetir = System.Array.FindAll(vivas, s => s != _slotPrevio);
                        int[] pozo = sinRepetir.Length > 0 ? sinRepetir : vivas;
                        _slotActual = pozo[Main.rand.Next(pozo.Length)];
                        _proximoTiro = ahora + TicksMoneda;
                    }
                    return _slotActual;
                }
                catch { return MusicLoader.GetMusicSlot(Mod, "Music/OleadaHambre"); }
            }
        }

        public override bool IsSceneEffectActive(Player player)
            => SubmundoGrimorio.Activo;

        // La llegada del portador: la moneda NACE vírgen en cada visita.
        public static void AlEntrar()
        {
            _slotPrevio = _slotActual > 0 ? _slotActual : 0;
            _slotActual = 0;
            _proximoTiro = 0;      // la primera lectura del interior tira YA
        }
    }
}
