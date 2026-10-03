using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Systems;
using AethonMod.Content.Projectiles.Jefes;

namespace AethonMod.Content.NPCs
{
    /// <summary>
    /// AETHON, LA LUZ PRIMORDIAL — LA ENCARNACIÓN (v6.50.36).
    ///
    /// v6.50.46 — LAS ARMAS QUE NO FUNCIONABAN (el fix que despertó tres
    /// ataques muertos) + EL ECLIPSE MUERE: la ronda de feedback sobre
    /// la v6.50.45 reveló que EL CORO, EL TELAR y EL DECRETO NUNCA
    /// HABÍAN FUNCIONADO — el estilo del proyectil vive en ai[0] y los
    /// tres lo SOBRESCRIBÍAN con sus propios datos (el ancla del coro,
    /// el centro del telar, el radio del decreto) en sus primeros
    /// ticks: el estilo moría, la IA y el render dejaban de matchear y
    /// el ataque quedaba INVISIBLE E INERTE («el jefe lo hace mal»,
    /// «no lo usa al cambio de fase» — el jefe se quedaba CLAVADO
    /// decretando un círculo que no existía). EL FIX: NADIE toca ai[0]
    /// jamás. Y el resto de la ronda: EL ANILLO DEL TIEMPO (cuatro
    /// relojes ×5.2 cayendo alrededor de la presa), EL TELAR TRAZA LA
    /// ESTRELLA (pentagrama/heptagrama de punta en punta — la figura
    /// que dibuja el jefe ES la jaula), EL VÓRTICE PRIMORDIAL (el
    /// relevo del eclipse: la galaxia de pernos que gira y colapsa —
    /// «la luz se apaga y solo brillan las balas, se ve mal, quitala»),
    /// LOS TAJOS MUEREN (las runas disparan abanicos de pernos) y LA
    /// GRAVEDAD DE VERDAD (el volteo es inmediato y real — el anuncio
    /// ya no miente).
    ///
    /// v6.50.45 — EL JEFE QUE SE ESFUMABA A MEDIAS LA INVOCACIÓN + LAS
    /// CINCO ARMAS DEL MOD EN SUS MANOS: «si el jefe es invocado y el
    /// jugador se mueve el jefe desaparece / a veces no termina de ser
    /// invocado y a veces desaparece» — CAUSA RAÍZ (decompilado de
    /// NPC.CheckActive, tML 2026.07.3.0): vanilla mata a CUALQUIER NPC
    /// cuyo timeLeft expire (NewNPC lo pone en 937 t) y SOLO lo refresca
    /// si un jugador vive dentro del rectángulo ±(sWidth/2+ancho,
    /// sHeight/2+alto) ≈ ±(1180, 760) px del NPC. El jefe nace 860 px
    /// SOBRE el jugador (FUERA del rectángulo VERTICAL desde el tick 1)
    /// y durante temblor+carrera (hasta 150+900 t desde la v6.50.43, que
    /// lo hizo invocable A CUALQUIER HORA) NO SE MUEVE NI SE ACERCA:
    /// timeLeft jamás se refresca → si la llegada dura más de 15,6 s el
    /// jefe MUERE EN SECRETO a media llegada (invocado cerca del
    /// mediodía llegaba; por la tarde/noche, moría — el «a veces» del
    /// usuario). EL FIX DOBLE: (1) CheckActive() devuelve false durante
    /// la LLEGADA y la MUERTE (el cine manda — vanilla no lo toca) y
    /// (2) el jefe escondido SIGUE AL JUGADOR cada tick (nace sobre TI,
    /// no sobre el punto del llamado). Y LAS ARMAS PEDIDAS: el jefe usa
    /// LOS PROYECTILES DE LOS BASTONES DEL MOD — EL RELOJ DE ARENA
    /// CÓSMICO GIGANTE (EST_RELOJ), EL CORO ESPECTRAL alrededor de la
    /// presa (EST_CORO), LA MANADA ASTRAL en camada — cazadores NPC de
    /// verdad: MENOS VIDA, MÁS LENTOS, EN MAYOR NÚMERO (EST_MANADA), EL
    /// TELAR DE CONSTELACIONES con el jefe CORRIENDO EN CÍRCULO alrededor
    /// del jugador y las estrellas CLAVÁNDOSE para atraparlo (EST_TELAR,
    /// fase 4+) y EL DECRETO DEL ECLIPSE en CADA CAMBIO DE FASE — el
    /// círculo MÁS GRANDE con cada fase y el jefe INMÓVIL todo el
    /// decreto (la ventana de escape del jugador).
    ///
    /// v6.50.42 — EL JEFE QUE NO APARECÍA MUERE AQUÍ (reproducido y
    /// verificado en servidor headless): la materialización BAJO el sol
    /// de la v6.50.41 usaba la matemática pantalla→mundo EN LA MÁQUINA
    /// QUE CORRE LA IA — en el servidor (host MP/dedicado) no hay
    /// pantalla (screenWidth=0, matrices identidad) y la posición salía
    /// (0, ~5500): FUERA DEL MUNDO — el jefe MORÍA al materializarse
    /// (tick 44 del climax, ANTES del fade), el espejo dejaba de verlo,
    /// soltaba el reloj… y el sol NO se quedaba fijo en el centro (los
    /// DOS síntomas de la .41, una sola causa). AHORA: PosicionBajoElSol
    /// es SEGURA en servidor (sobre el jugador — la cámara lo centra:
    /// «bajo el sol» es el cielo de SU pantalla), exacta en cliente,
    /// NUNCA enterrada (mínimo 300 px sobre el jugador) y NUNCA fuera
    /// del mundo (clamp a los límites).
    ///
    /// v6.50.41 — EL MEDIO DÍA DEL DESTELLO: «mejor quita la capa de
    /// oscuridad, no se ve nada bien, se ve horrible» (LA OSCURIDAD Y
    /// EL SOL NEGRO MUEREN — VeloLib borrada de raíz) + «el sol no se
    /// haga teletransportación… si está más allá del centro, un día
    /// completo avanza con noche completa, un nuevo día hasta el
    /// amanecer» (el bug de la v6.50.40: con el sol en la tarde, el
    /// aterrizaje se disparaba al INSTANTE y el sol saltaba HACIA
    /// ATRÁS; ahora la VENTANA de aterrizaje es [26999, 27001] y la
    /// carrera da la vuelta entera por la noche) + EL DESTELLO nace
    /// DEL SOL («un brillo que viene del mismo sol, centrado en el sol
    /// y difuminándose hacia los bordes hasta ser transparente») y
    /// Aethon NO NACE DEL CENTRO del sol: se materializa BAJO él.
    ///
    /// v6.50.37 — MÁS GRANDE Y LA LLEGADA DEFINITIVA (EL MEDIO DÍA DE
    /// LA OSCURIDAD): «has que sea mas grande el jefe… cuando Aethon
    /// aparece el mundo debe temblar… si es de noche se hace de dia y
    /// si es de dia el tiempo avanza hasta que el sol quede centrado…
    /// destellos de luz aparecen en el cielo… el sol brilla con
    /// intensidad y de ahi aparece Aethon…».
    ///
    /// Petición del usuario: «el jefe se ve feo, intenta mejorar por
    /// código… ese jefe se supone que es Aethon, creo que en vez de
    /// hacerlo una sierpe, mejor hacerlo una luz brillante, el jefe es
    /// una potente luz que ataca al jugador con ataques devastadores».
    ///
    /// LA SIERPE MUERE (v6.50.19→35: seis versiones de hueso). Si el
    /// jefe ES Aethon y Aethon ES la Luz Primordial — ¿por qué esconder
    /// la luz dentro de un esqueleto? EL JEFE ES LA LUZ MISMA: un SOL
    /// VIVO que flota sobre el mundo y lo castiga con ataques de luz.
    ///
    /// · EL CUERPO: UN SOLO NPC (la cadena de 68 vértebras MUERE —
    ///   AethonSierpeCuerpo/AethonSierpeCola borrados del mod). Esfera
    ///   pura: no hay anatomía que pueda verse fea — hay FENÓMENO.
    /// · EL ARTE (100% código, la técnica de la casa): EL NÚCLEO blanco
    ///   pulsante + EL HALO dorado + LOS RAYOS RADIALES rotando lento +
    ///   LAS DOS CORONAS DE PERLAS (órbitas elípticas de blooms — el
    ///   efecto 3D de Saturno) + LAS CHISPAS orbitantes + LA ESTELA del
    ///   destello. Todo aditivo: la luz no tiene silueta, tiene brillo.
    /// · LA IA — LA LUZ ATACA CON TODO (devastador, decretado):
    ///   - EL JUICIO DE LUZ: columnas de luz que CAEN DEL CIELO sobre
    ///     las posiciones PREDICHAS (el proyectil se telegrafea solo:
    ///     cae lento 45 t — la línea de luz — y luego ACELERA a 52).
    ///   - EL RAYO PRIMORDIAL: el arco PerlinBolt GRUESO boca→presa con
    ///     lluvia densa de pernos (heredero del aliento de la sierpe).
    ///   - LA NOVA: contracción telegrafiada y ESTALLIDO en anillos con
    ///     huecos — hay que BUSCAR EL HUECO (el anuncio lo dice).
    ///   - LA CRUZ DE LUZ (fase 3+): cuatro chorros de pernos girando —
    ///     la cruz que barre la arena.
    ///   - EL DESTELLO: la embestida a velocidad luz con línea guía
    ///     (heredera del ram: cadena 2-3 desde ángulos nuevos).
    ///   - EL ECLIPSE (fase 4+): LA LUZ SE APAGA — el núcleo se oscurece,
    ///     la atracción tira de ti hacia el cuerpo muerto y SOLO LAS
    ///     BALAS BRILLAN (bullet hell lento); al final, EL REGRESO: la
    ///     luz VUELVE con una nova gratis.
    ///   - LA ROTACIÓN heredada (v6.50.34): NUNCA el mismo ataque dos
    ///     veces — la luz es impredecible.
    ///   - EL ANTI-CAMPING heredado: presa quieta → el flotar se corta.
    /// · LAS FASES (los umbrales de siempre): P1 POLVO ESTELAR (juicio
    ///   + rayo) · P2 NEBULOSA (+nova) · P3 GRAVEDAD (+cruz, +destello,
    ///   el volteo) · P4 AGUJERO NEGRO (+eclipse, las runas recuerdan)
    ///   P5 RECONOCIMIENTO (la furia: TODO + El Recordar + cadenas).
    /// · LA LLEGADA: ColaSierpeSky (el cielo de la casa) ahora pinta EL
    ///   CIELO SE ENCIENDE — resplandor creciente + columnas lejanas +
    ///   la ventana del núcleo (el mismo contrato de lote del fondo).
    ///
    /// LO QUE VIVE DE LA SIERPE: el drop (La Forma Ascendida + esencia
    /// + 250 shards), la barra de jefe con icono (regenerado: EL SOL),
    /// el cine de muerte (ahora: LA CONTRACCIÓN y el estallido final),
    /// el contrato MP (ai[0]/ai[1]/ai[2]/ai[3]), las runas, el volteo
    /// de gravedad, la predicción adaptativa y la paciencia dinámica.
    /// </summary>
    [AutoloadBossHead]
    public class AethonBoss : ModNPC
    {
        // === LOS ESTADOS DE LA LUZ ===
        private const int EST_NACIENDO = 0;     // el cielo se enciende
        private const int EST_FLOTAR = 1;       // la órbita serena
        private const int EST_JUICIO = 2;       // LAS COLUMNAS DEL CIELO
        private const int EST_RAYO = 3;         // el arco primordial
        private const int EST_NOVA = 4;         // la contracción y el estallido
        private const int EST_CRUZ = 5;         // los cuatro chorros girando
        private const int EST_DESTELLO = 6;     // la embestida a velocidad luz
        private const int EST_VORTICE = 7;      // v6.50.46 — EL VÓRTICE PRIMORDIAL (el eclipse MURIÓ: «la luz se apaga y solo brillan las balas, se ve mal, quitala»)
        private const int EST_RELOJ = 8;        // v6.50.45 — EL RELOJ DE ARENA GIGANTE
        private const int EST_CORO = 9;         // v6.50.45 — EL CORO ESPECTRAL
        private const int EST_MANADA = 10;      // v6.50.45 — LA MANADA ASTRAL (camada)
        private const int EST_TELAR = 11;       // v6.50.45 — EL TELAR (el círculo veloz)
        private const int EST_DECRETO = 12;     // v6.50.45 — EL DECRETO DEL ECLIPSE (cambio de fase)
        private const int EST_ESTALLIDO = 13;   // v6.50.53 — EL ESTALLIDO RADIANTE (la imagen del usuario: el punto de luz que ARDE y lo arrasa todo — 60 t de recogida telegrafiada y la explosión de 360° con rayos, anillo segmentado, cruz y chispas)
        private const int EST_DANZA = 14;      // v6.50.54 — LA DANZA SOLAR (la investigación EoL: SUN DANCE — la rueda de rayos del dios girando sobre la presa, el ataque que un dios-sol DEBÍA tener)
        private const int EST_LANZAS = 15;     // v6.50.54 — LAS LANZAS ETERNAS (ETHEREAL LANCE: la luz sembrada DETRÁS de tu carrera, telegrafiada y translúcida hasta volar — castiga la línea recta)
        private const int EST_CORONA = 16;     // v6.50.54 — LA CORONA ETERNA (EVERLASTING RAINBOW: el anillo de 14 plumas prismáticas espiralando alrededor de la presa)
        private const int EST_SOL = 17;       // v6.50.57 — EL SOL DEL DIOS (el ataque especial: el jefe SE CONVIERTE en sol — la asunción — y LUEGO LO LANZA a la presa: persiga lenta + gravedad + la GIGANTE ROJA que estalla en luz, bruma y formas)
        private const int EST_MURIENDO = 99;    // la contracción final

        // === LA ENTRADA — v6.50.52 — LA LETRA NUEVA: «el jefe no aparece,
//     además demora mucho la animación del suelo temblando y todo eso /
//     la presentación debe durar hasta que el sol llegue al centro, LUEGO
//     APARECE EL JEFE» — los CINCO actos de la .51 (180+150+carrera+120+90)
//     se comían 10-20 s de cine antes de ver un solo jefe. Ahora DOS
//     actos, nada más:
//       9  LA PRESENTACIÓN — la lluvia de luz en TODO el cielo (solo
//          color luz) + el temblor creciendo + LA CARRERA del reloj
//          bidireccional, TODO JUNTO desde el tick 1: la presentación ES
//          la carrera — dura exactamente lo que tarda el sol en llegar
//          al centro (mínimo 150 t para que respire) y la luz permanece
//          INVISIBLE («LUEGO aparece el jefe»). EL PARACAÍDAS a los
//          570 t: si cualquier cosa mete el sol, la IA lo posa a mano —
//          el jefe APARECE SIEMPRE (la cura del «no aparece»);
//       10 EL APARECER (80 t) — el sol YA está en el centro: EL PILAR
//          cae, EL DESTELLO nace del sol y la luz SE MATERIALIZA dentro
//          del pilar (fade 24 t) bajando a su órbita de pelea — y a
//          PELEAR. ===
public const int SUB_PRESENTA = 9;      // la presentación: la lluvia + el temblor + la carrera (el jefe INVISIBLE)
public const int SUB_APARICION = 10;    // el sol en el centro: el pilar + el destello + la luz APARECE

        // === EL ESTADO DEL MUNDO (el espejo de AethonLlegadaSistema lo
        //     sincroniza en TODAS las máquinas leyendo ai[] — el servidor
        //     lo escribe aquí, cada cliente lo reconstruye) ===

        /// <summary>El sub-estado de LA LLEGADA (0 = no está llegando).</summary>
        public static int SubLlegada = 0;

        /// <summary>EL TIEMPO CORRE: ModifyTimeRate acelera el día (la carrera al mediodía).</summary>
        public static bool TiempoCorriendo = false;

        /// <summary>EL TIEMPO CONGELADO: el sol clavado en el centro del cielo.</summary>
        public static bool TiempoCongelado = false;

        /// <summary>
        /// LA POSICIÓN DEL SOL EN EL CIELO (espacio del fondo) — LITERAL
        /// del decompile de Main.DrawSunAndMoon (2026.07): x avanza lineal
        /// con el tiempo (de −sunW a totalWidth+sunW a lo largo del día) e
        /// y cabalga una PARÁBOLA (250 px de caída desde bgTopY+180 en el
        /// alba hasta el cenit del mediodía y de vuelta al ocaso). Con el
        /// tiempo CONGELADO en el mediodía exacto (time=27000) degenera a
        /// x = totalWidth·0.5 (el centro EXACTO de la pantalla) e y =
        /// bgTopY + 180 — y durante LA CARRERA sigue al sol en su barrida
        /// (la ventana del cielo LO PERSIGUE). bgTopY (interno de Main) se
        /// replica: worldSurface·16 − screenPosition.Y + 16. Para espacio
        /// de PANTALLA: transformar con Main.BackgroundViewMatrix.EffectMatrix.
        /// </summary>
        public static Vector2 PosicionSolEnCielo()
        {
            float solW = 80f;
            try { solW = Terraria.GameContent.TextureAssets.Sun.Value.Width; }
            catch { }
            double t = Main.dayTime ? Math.Max(0.0, Math.Min(Main.dayLength, Main.time)) : Main.dayLength * 0.5;
            float x = (float)(t / 54000.0 * (Main.screenWidth + solW * 2f)) - solW;
            double parab = t < 27000.0
                ? Math.Pow(1.0 - t / 54000.0 * 2.0, 2.0)
                : Math.Pow((t / 54000.0 - 0.5) * 2.0, 2.0);
            float bgTopY = (float)((int)Main.worldSurface * 16) - Main.screenPosition.Y + 16f;
            float y = bgTopY + (float)(parab * 250.0) + 180f;
            return new Vector2(x, y);
        }

        private int _estado = EST_NACIENDO;
        private int _tickEstado = 0;
        private bool _nacio = false;
        private int _subLlegada = SUB_PRESENTA;   // v6.50.49 — LA PRESENTACIÓN de la Emperatriz

        // === EL RAYO (heredero del aliento: 1 carga · 2 fuego) ===
        private int _rayo = 0;
        private int _tickRayo = 0;

        // === LA ROTACIÓN (v6.50.34 — la luz nunca repite) ===
        private int _ultimoAtaque = 0;          // 0 ninguno · 1 juicio · 2 rayo · 3 nova · 4 cruz · 5 destello · 6 eclipse
        private int _cadenasDestello = 0;       // cuántos destellos lleva la cadena
        private float _angDestello = 0f;        // el rumbo del destello (viaja en ai[3])

        // === EL ANTI-CAMPING (heredado) ===
        private int _ticksPresaQuieta = 0;

        // === LA MEMORIA DE LA PRESA (v6.50.44 — la predicción con
        //     pasado): un anillo de las últimas 8 posiciones — la luz
        //     no lee tu velocidad de AHORA, lee tu RITMO (y castiga la
        //     línea recta, no el quiebre honesto) ===
        private Vector2[] _memPresa = new Vector2[8];
        private int _idxMem = 0;
        private bool _memLlena = false;

        // === LA ESTELA MINADA DEL DESTELLO (v6.50.44): una mina por
        //     cruzamiento — el punto por el que VOLVISTE ya no es seguro ===
        private bool _minaPuesta = false;

        // === v6.50.54 — EL COMPÁS DEL ESTALLIDO (la frecuencia pedida: «1 vez
        //     en la fase 1, 2 veces en la fase 2 y ser un poco más grande, y
        //     en la fase 3 debe hacerla cada un número de veces aleatorio
        //     entre 1 a 9 ataques de otro tipo»): P1/P2 gastan un PRESUPUESTO
        //     por fase (1 y 2 usos); P3+ lo FUERZA un contador aleatorio 1-9
        //     que se RE-TIRA tras cada estallido) ===
        private int _estallidosUsadosEnFase = 0;
        private int _ataquesDesdeEstallido = 0;
        private int _proximoEstallidoEn = 999;    // en P1/P2 no aplica (presupuesto)
        private float _proximaExplosion = 60f;    // viaja en ai[3]: el tick de la PRÓXIMA explosión (el telegrafo del cliente)

        // === v6.50.57 — LA COREOGRAFÍA DEL DIOS (la mejora de IA pedida:
        //     «investiga jefes de otros mods que sean similares o dioses,
        //     y crea una coreografia con nuestro jefe» — la cadena de la
        //     Emperatriz: los ataques se pasan el turno SIN volver a la
        //     órbita) y LA MEMORIA DOBLE (nunca el mismo plato NI el de
        //     atrás) ===
        private readonly int[] _coreoCola = new int[6];
        private int _coreoLargo = 0;
        private int _ataquesDesdeCoreo = 0;
        private int _proximoCoreoEn = 999;
        private int _anteultimoAtaque = 0;

        // === LAS RUNAS (heredadas de la sierpe). EL VOLTEO DE GRAVEDAD
        //     MURIÓ en la v6.50.57 (la letra: «eso que hace el jefe de
        //     cambiar la gravedad tiene que dejar de hacerlo») — el suelo
        //     vuelve a ser tuyo PARA SIEMPRE ===
        private int _tickRunas = 0;

        // === v6.50.46 — EL TELAR: EL TRAZO DE LA ESTRELLA (qué punta
        //     visita el jefe y cuánto lleva en la pierna del salto) ===
        private int _telarPaso = 0;
        private int _tickPierna = 0;

        // === LA FASE (los umbrales de siempre) ===
        private int Phase = 1;

        /// <summary>
        /// v6.50.59 — LA FASE PÚBLICA: el RELOJ PERMANENTE de fase 2 la
        /// lee (el reloj vive mientras la fase 2 viva — el ataque que dura
        /// TODA la fase, la letra de la decimasexta ronda).
        /// </summary>
        internal int FasePublica => Phase;

        /// <summary>v6.50.59 — el whoAmI del RELOJ PERMANENTE de la fase 2 (−1 = sin reloj).</summary>
        private int _relojPermanente = -1;

        /// <summary>LA FURIA: la fase final es MÁS RÁPIDA en todo.</summary>
        private bool Furia => Phase >= 5;

        // === LA ÓRBITA DE FLOTACIÓN ===
        private float _angOrbita = -MathHelper.PiOver2;
        private int _sentidoOrbita = 1;

        // === EL CINE DE MUERTE ===
        private bool _muriendo = false;
        private int _tickMuerte = 0;
        private bool _yaDropeo = false;

        // === LA PALETA DE LA LUZ PRIMORDIAL ===
        private static readonly Color OroLuz = new(255, 240, 190);
        private static readonly Color VioletaLuz = new(196, 150, 255);
        private static readonly Color NucleoBlanco = new(255, 252, 240);

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
        }

        public override void SetDefaults()
        {
            NPC.width = 220;     // v6.50.37 — MÁS GRANDE (el sol creció ×1.5)
            NPC.height = 220;
            NPC.damage = 120;    // TOCAR LA LUZ quema (devastador, decretado)
            NPC.defense = 50;
            NPC.lifeMax = 2_400_000;
            NPC.HitSound = SoundID.NPCHit52;    // el golpe de energía
            NPC.DeathSound = SoundID.NPCDeath55;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;   // la luz atraviesa el mundo
            NPC.boss = true;
            NPC.npcSlots = 30f;
            NPC.aiStyle = -1;
            NPC.netAlways = true;
            Music = MusicID.Boss5;
            SceneEffectPriority = SceneEffectPriority.BossHigh;
        }

        // ==================================================================
        //  LA IA — LA LUZ PRIMORDIAL
        // ==================================================================
        public override void AI()
        {
            // === EL CINE DE MUERTE (CheckDead manda aquí) ===
            if (_muriendo) { CineMuerte(); return; }

            // === LA PRESA (el despawn limpio de la casa) ===
            Player target = Main.player[NPC.target];
            if (!target.active || target.dead)
            {
                NPC.TargetClosest(false);
                target = Main.player[NPC.target];
                if (!target.active || target.dead)
                {
                    NPC.life = 0;
                    NPC.active = false;
                    return;
                }
            }

            // === EL DETECTOR DE PRESA QUIETA (heredado: pararse es invitar) ===
            if (target.velocity.LengthSquared() < 0.36f) _ticksPresaQuieta++;
            else _ticksPresaQuieta = 0;

            // === LA MEMORIA DE LA PRESA (v6.50.44): guarda el ritmo —
            //     el anillo de las últimas 8 posiciones (la predicción
            //     nueva lo lee cada vez que apunta) ===
            _memPresa[_idxMem] = target.Center;
            _idxMem = (_idxMem + 1) & 7;
            if (_idxMem == 0) _memLlena = true;

            // === LA ENTRADA — EL PRIMER TICK (v6.50.50: LA ENTRADA EXACTA DE
            //     LA EMPERATRIZ, ACTO POR ACTO. El invocador ya la dejó DONDE
            //     nace la Emperatriz (200 px ENCIMA del jugador, el jitter
            //     circular de 50 del case 661) y su SpawnBoss ya dijo SU
            //     anuncio («ha despertado», el de vanilla — como la Emperatriz,
            //     NI UN texto más). Aquí solo el nacimiento: invisible (alpha
            //     255) e intocable — v6.50.52: la presentación ENTERA (la lluvia
            //     + LA CARRERA) corre con el jefe INVISIBLE; el fade
            //     vive ahora en EL APARECER (24 t al final, cuando el
            //     sol ya llegó al centro — «LUEGO aparece el jefe») ===
            if (!_nacio)
            {
                NPC.velocity = Vector2.Zero;
                NPC.alpha = 255;          // invisible hasta que la presentación lo traiga
                NPC.dontTakeDamage = true;
                _subLlegada = SUB_PRESENTA;
                _tickEstado = 0;
                _estado = EST_NACIENDO;
                _nacio = true;
                NPC.netUpdate = true;
            }

            // === LA FASE POR VIDA (los umbrales de siempre, histéresis) ===
            float hpPct = (float)NPC.life / NPC.lifeMax;
            int newPhase = 1;
            if (hpPct < 0.8f) newPhase = 2;
            if (hpPct < 0.6f) newPhase = 3;
            if (hpPct < 0.4f) newPhase = 4;
            if (hpPct < 0.2f) newPhase = 5;
            if (newPhase > Phase)
            {
                Phase = newPhase;
                OnPhaseChange();
                _coreoLargo = 0;   // v6.50.57 — la danza muere con la fase
            }

            // === EL FADE DE NACIMIENTO (v6.50.50): la PRESENTACIÓN tomó el
            //     rampa ENTERA — la curva LITERAL de la Emperatriz (alpha =
            //     255·(1 − t/180), su Opacity clavada) vive ahora en
            //     EstadoNaciendo con dueño ÚNICO (el −17/t de la .49 murió:
            //     la Emperatriz no se materializa en 15 ticks). ===

            // === EL MOTOR DE ESTADOS ===
            _tickEstado++;
            switch (_estado)
            {
                case EST_NACIENDO: EstadoNaciendo(target); break;
                case EST_FLOTAR: EstadoFlotar(target); break;
                case EST_JUICIO: EstadoJuicio(target); break;
                case EST_RAYO: EstadoRayo(target); break;
                case EST_NOVA: EstadoNova(target); break;
                case EST_CRUZ: EstadoCruz(target); break;
                case EST_DESTELLO: EstadoDestello(target); break;
                case EST_VORTICE: EstadoVortice(target); break;
                case EST_RELOJ: EstadoReloj(target); break;
                case EST_CORO: EstadoCoro(target); break;
                case EST_MANADA: EstadoManada(target); break;
                case EST_TELAR: EstadoTelar(target); break;
                case EST_DECRETO: EstadoDecreto(target); break;
                case EST_ESTALLIDO: EstadoEstallido(target); break;
                case EST_DANZA: EstadoDanza(target); break;
                case EST_LANZAS: EstadoLanzas(target); break;
                case EST_CORONA: EstadoCorona(target); break;
                case EST_SOL: EstadoSol(target); break;
            }

            // === LOS ATAQUES DE FONDO (las runas recuerdan — heredado) ===
            switch (Phase)
            {
                case 4: Fase4Runas(); break;
                case 5: Fase5FuriaFondo(target); break;
            }

            // === LA LUZ DEL MUNDO: la luz inunda SIEMPRE (el eclipse y su
            //     apagón MURIERON con la v6.50.46 — el sol no se apaga más) ===
            Lighting.AddLight(NPC.Center, new Vector3(1.55f, 1.35f, 0.95f)); // el sol vivo (más grande)

            // === EL CONTRATO MP (la casa): estado/subfase/tick/param viajan ===
            NPC.ai[0] = _estado;
            NPC.ai[1] = _estado == EST_NACIENDO ? _subLlegada : _rayo;
            //     entrada: 9 presentación (lluvia + temblor + carrera —
            //      el jefe invisible) · 10 aparición (pilar + destello
            //      + materialización — v6.50.52, la llegada en DOS actos)
            //     pelea: 0 nada · 1 rayo cargando · 2 rayo ardiendo
            //     (v6.50.45: los estados nuevos — 8 reloj · 9 coro · 10 manada
            //      · 11 telar · 12 decreto — viajan en ai[0] y usan ai[1]=0
            //      salvo el decreto, que manda LA FASE por ai[1] al círculo
            //      desde su spawn, no desde aquí. v6.50.46: el 3 del eclipse
            //      MURIÓ con él — ai[1] jamás vuelve a 3 en la pelea)
            NPC.ai[2] = _tickEstado;
            // v6.50.54 — ai[3] DUPLA: en EL DESTELLO sigue siendo el rumbo (la
            // línea guía del cliente); en EL ESTALLIDO viaja el tick de la
            // PRÓXIMA explosión (el aro del telegrafo se re-dibuja antes de
            // CADA detonación — el compás del multi-estallido).
            NPC.ai[3] = _estado == EST_ESTALLIDO ? _proximaExplosion : _angDestello;

            // MP: la luz respira por el cable cada 12 ticks.
            if ((Main.GameUpdateCount % 12u) == 0u) NPC.netUpdate = true;
        }

        /// <summary>
        /// v6.50.45 — EL FIX DEL JEFE QUE SE ESFUMABA (los DOS síntomas del
        /// usuario: «si el jefe es invocado y el jugador se mueve el jefe
        /// desaparece» + «a veces no termina de ser invocado»): vanilla
        /// CheckActive mata a cualquier NPC cuyo timeLeft expire. v6.50.52
        /// — la llegada en DOS actos mantiene al jefe cerca de su presa
        /// (la presentación ES la carrera y EL APARECER lo baja del pilar
        /// a la órbita en 80 t): el cine de la llegada NO se interrumpe
        /// (la presentación · la carrera · la aparición), SpawnBoss ya
        /// trajo el timeLife ×20 y la luz SIGUE
        /// al jugador en todo momento (SeguirCielo — jamás clavada en el
        /// punto del llamado, la lección de la .45).
        /// Durante la PELEA el comportamiento vanilla queda intacto (el
        /// jefe persigue: siempre está cerca).
        /// </summary>
        public override bool CheckActive()
        {
            if (_estado == EST_NACIENDO) return false;   // la llegada es cine (2 actos)
            if (_muriendo) return false;                 // la muerte es cine
            return base.CheckActive();
        }

        // ==================================================================
        //  LA PREDICCIÓN ADAPTATIVA (v6.50.44 — LA QUE LEE TU RITMO)
        // ==================================================================
        private Vector2 PredPresa(Player target)
        {
            // v6.50.44 — LA PREDICCIÓN LEE TU RITMO, NO TU INSTANTE: la
            // velocidad MEDIA de las últimas 8 posiciones (el ritmo real
            // de tu esquivo) en vez de la velocidad de ESTE tick (que
            // miente cuando zigzagueas — el jitter del control no es
            // rumbo). Correr en línea recta queda EXPUESTO; el quiebre
            // honesto, premiado.
            Vector2 velMedia = target.velocity;
            if (_memLlena)
            {
                velMedia = Vector2.Zero;
                for (int i = 0; i < 8; i++)
                {
                    int nuevo = (_idxMem - 1 - i) & 7;
                    int viejo = (_idxMem - 2 - i) & 7;
                    velMedia += _memPresa[nuevo] - _memPresa[viejo];
                }
                velMedia /= 8f;   // px/tick promediados
            }

            // LA FINTA: si estás CAMBIANDO de dirección (la media apunta
            // CONTRA tu velocidad actual), la luz NO muerde el anzuelo —
            // recorta el lead a la mitad (respeta el quiebre, castiga la
            // costumbre).
            float dist = Vector2.Distance(NPC.Center, target.Center);
            float lead = MathHelper.Clamp(dist / 35f, 10f, 34f);
            if (Vector2.Dot(velMedia, target.velocity) < 0f) lead *= 0.5f;
            return target.Center + velMedia * lead;
        }

        // ==================================================================
        //  LOS ESTADOS
        // ==================================================================

        /// <summary>
        /// v6.50.52 — LA LLEGADA EN DOS ACTOS (la letra nueva: «la
        /// presentación debe durar hasta que el sol llegue al centro,
        /// LUEGO APARECE EL JEFE» — y «demora mucho el suelo temblando y
        /// todo eso»: los cinco actos de la .51 murieron).
        ///
        ///   ACTO 1 — LA PRESENTACIÓN (= LA CARRERA): la coreografía de
        ///   la Emperatriz (SU caída (0,5) que se frena ×0.95, SU
        ///   Item161) con la lluvia SOLO COLOR LUZ (blanco y oro)
        ///   cayendo en TODO EL CIELO, EL TEMBLOR creciendo (los kicks
        ///   del espejo) y LA CARRERA del reloj bidireccional — TODO
        ///   JUNTO desde el primer tick. La presentación dura EXACTO lo
        ///   que tarda el sol en llegar al centro (mínimo 150 t para que
        ///   respire) y la luz permanece INVISIBLE (alpha 255): el jefe
        ///   APARECE al final, no antes. EL PARACAÍDAS a los 570 t: si
        ///   cualquier cosa mete el sol (la carrera de la .51 podía
        ///   colgarse — «el jefe no aparece»), la IA lo POSA a mano y
        ///   aparece IGUAL — el jefe sale SIEMPRE.
        ///
        ///   ACTO 2 — EL APARECER (80 t): el sol YA está en el centro:
        ///   EL PILAR de luz cae del cielo (EstiloPilarAparicion — el
        ///   manto dorado y el charco en la base, cosmético, daño 0),
        ///   EL DESTELLO nace del sol (lo pinta ColaSierpeSky con SU
        ///   curva comprimida) y la luz SE MATERIALIZA dentro del pilar
        ///   (fade 255→0 en 24 t) bajando a su órbita de pelea con su
        ///   lluvia de chispas doradas — y a PELEAR. El anuncio sigue
        ///   siendo SOLO el de SpawnBoss («ha despertado», el de
        ///   vanilla — NI UN texto más).
        /// </summary>
        private void EstadoNaciendo(Player target)
        {
            // LA CUENTA (la ai[1] de la Emperatriz): _tickEstado llega +1
            // por el ++ del motor — el t de vanilla, sin descuentos.
            int t = _tickEstado;

            switch (_subLlegada)
            {
                // ==============================================================
                //  ACTO 1 — LA PRESENTACIÓN (= LA CARRERA): la lluvia de
                //  luz en TODO EL CIELO + el temblor + el reloj corriendo
                //  al mediodía (AVANZA o RETROCEDE — lo lleva
                //  AethonLlegadaSistema en TODAS las máquinas). El jefe
                //  INVISIBLE hasta el final.
                // ==============================================================
                case SUB_PRESENTA:
                {
                    // t=0/1 — LA CAÍDA INICIAL (SU velocity (0,5)).
                    if (t <= 1)
                        NPC.velocity = new Vector2(0f, 5f);

                    // t=10 — EL SONIDO DE SU NACIMIENTO (en SU tick, palabra por palabra).
                    // v6.50.59 — EL RUIDO DE TEMBLOR DE SIEMPRE (la letra:
                    // «en la presentacion del jefe sale el sonido de la
                    // emperatriz de la luz y esto esta mal, el ruido que
                    // debe salir era el ruido de temblor que habia
                    // anteriormente»): el Item161 de la Emperatriz MURIÓ —
                    // vuelve el TEMBLOR de la casa (el Item122 grave de la
                    // llegada de la .48, el que sacudía el mundo). Y
                    // REFUERZOS: el temblor CRECE — el mismo rugido otra
                    // vez a mitad de la carrera (más grave aún).
                    if (t == 11 && !Main.dedServ)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(
                            SoundID.Item122.WithPitchOffset(-0.25f), NPC.Center);
                        OndaLib.Kick(9f, 22);
                    }
                    if (t == 130 && !Main.dedServ)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(
                            SoundID.Item122.WithPitchOffset(-0.4f), NPC.Center);
                        OndaLib.Kick(11f, 24);
                    }

                    // v6.50.59 — EL CIELO ENTERO SE ENCIENDE (la letra: «el
                    // cielo entero no se ilumina, solo salen particulas,
                    // debe tambien iluminarce con aurora blanca y dorada»):
                    // además de la lluvia de polvos, la LUZ DE MUNDO inunda
                    // los alrededores de la presa mientras el dios desciende
                    // — el SUELO también se baña de blanco-dorado (la aurora
                    // del cielo la pinta ColaSierpeSky en TODAS las
                    // pantallas).
                    if (!Main.dedServ)
                    {
                        float encendido = MathHelper.Clamp(t / 180f, 0f, 1f);
                        Lighting.AddLight(target.Center,
                            new Vector3(1.6f, 1.42f, 1.0f) * (0.35f + 0.65f * encendido));
                    }

                    // LA CAÍDA QUE SE FRENA (cada tick: ×0.95 — como la Emperatriz).
                    NPC.velocity *= 0.95f;

                    // LA LLUVIA DE LUZ (TODO EL CIELO, SOLO COLOR LUZ: el
                    // rectángulo de la cámara entero — relativo al
                    // JUGADOR, no al jefe: la lluvia cae por TODOS lados)
                    // — VIVE HASTA EL FINAL de la presentación (la .51 la
                    // cortaba a los 155 t y la carrera seguía sola).
                    if (t > 11 && !Main.dedServ)
                    {
                        float anchoCielo = Math.Max(920f, Main.screenWidth * 0.62f);
                        float techoCielo = Math.Max(520f, Main.screenHeight * 0.62f);
                        for (int k = 0; k < 4; k++)
                        {
                            float opacidad = MathHelper.Clamp(t / 180f, 0f, 1f);
                            float num52 = MathHelper.Lerp(1.3f, 0.7f, opacidad) *
                                Utils.GetLerpValue(0f, 120f, t, true);
                            // SOLO COLOR LUZ: blanco y oro de la casa,
                            // alternados — NI UN matiz del espectro.
                            Color colLuz = (k & 1) == 0
                                ? new Color(255, 255, 250, 255)
                                : new Color(255, 244, 204, 255);
                            int idx = Dust.NewDust(NPC.position, NPC.width, NPC.height,
                                DustID.RainbowMk2, 0f, 0f, 0, colLuz);
                            Main.dust[idx].position = target.Center +
                                new Vector2(Main.rand.NextFloat(-anchoCielo, anchoCielo),
                                    -Main.rand.NextFloat(60f, techoCielo));
                            Main.dust[idx].velocity *= Main.rand.NextFloat() * 0.8f;
                            Main.dust[idx].noGravity = true;
                            Main.dust[idx].fadeIn = 0.6f + Main.rand.NextFloat() * 0.7f * num52;
                            Main.dust[idx].velocity += Vector2.UnitY * 3f;
                            Main.dust[idx].scale = 0.45f;
                            if (idx != 6000)
                            {
                                Dust clon = Dust.CloneDust(idx);
                                clon.scale /= 2f;
                                clon.fadeIn *= 0.85f;
                                clon.color = new Color(255, 255, 255, 255);
                            }
                        }
                    }

                    // LA LUZ PERMANECE INVISIBLE — «la presentación debe
                    // durar hasta que el sol llegue al centro, LUEGO
                    // aparece el jefe» (el fade vive en EL APARECER).
                    NPC.alpha = 255;

                    // EL CAMBIO DE ACTO (manda el server / singleplayer;
                    // en MP cada cliente remata su propia carrera con el
                    // espejo — nadie salta): el sol EN EL CENTRO (con el
                    // mínimo de 150 t de presentación)… o EL PARACAÍDAS
                    // (570 t: cualquier cosa que meta el sol, la IA lo
                    // posa a mano — EL JEFE APARECE SIEMPRE).
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        bool solCentro = Main.dayTime &&
                            Main.time >= 26999.0 && Main.time <= 27001.0;
                        if ((t >= 150 && solCentro) || t >= 570)
                        {
                            if (!solCentro)
                            {
                                // EL ATERRIZAJE DE EMERGENCIA — solo si el
                                // reloj se atascó: el sol se posa y NADIE
                                // espera más (en juego normal jamás dispara:
                                // la carrera más larga son ~220 t).
                                Main.dayTime = true;
                                Main.time = 27000.0;
                            }
                            SubFaseLlegada(SUB_APARICION);
                        }
                    }
                    break;
                }

                // ==============================================================
                //  ACTO 2 — EL APARECER (80 t): el sol YA está en el
                //  centro → EL PILAR cae, EL DESTELLO nace del sol y la
                //  luz se materializa dentro del pilar bajando a su
                //  órbita — y a PELEAR.
                // ==============================================================
                case SUB_APARICION:
                {
                    NPC.velocity = Vector2.Zero;
                    if (_tickEstado == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        // EL PILAR: la columna de luz que cae del cielo
                        // (cosmética — daño 0; la dibuja
                        // AtaqueJefeProjectile.EstiloPilarAparicion con el
                        // manto dorado y el charco de luz en la base).
                        Projectile.NewProjectile(NPC.GetSource_FromAI(),
                            PosicionAparicion(target), Vector2.Zero,
                            ModContent.ProjectileType<AtaqueJefeProjectile>(),
                            0, 0f, Main.myPlayer,
                            AtaqueJefeProjectile.EstiloPilarAparicion, 0f,
                            NPC.whoAmI);
                    }

                    // LA MATERIALIZACIÓN RÁPIDA (fade 255→0 en 24 t): la
                    // luz YA está aquí — aparece DENTRO del pilar.
                    NPC.alpha = (int)Math.Round(255f *
                        Math.Max(0f, 1f - _tickEstado / 24f));

                    // LA BAJADA: directo A LA ÓRBITA DE PELEA — la luz se
                    // materializa DENTRO del pilar y se posa donde va a
                    // flotar (la cúspide de la .51 a −760 quedaba FUERA de
                    // la vista: ahora NADA de la aparición sale de la
                    // pantalla). Posición SERVER-SEGURA (la v6.50.42:
                    // relativa al jugador, SIN matemática de pantalla).
                    if (_tickEstado <= 36)
                    {
                        Vector2 punto = target.Center + new Vector2(0f, -420f);
                        NPC.Center = Vector2.Lerp(NPC.Center, punto, 0.14f);
                        NPC.netUpdate = true;
                    }
                    else
                    {
                        Vector2 punto = target.Center + new Vector2(0f, -420f);
                        NPC.velocity = Vector2.Lerp(NPC.velocity,
                            (punto - NPC.Center) * 0.04f, 0.12f);
                    }

                    // LA LLUVIA DE LUZ: chispas doradas que caen con la luz.
                    if (!Main.dedServ && Main.rand.NextBool(3))
                    {
                        Dust d = Dust.NewDustPerfect(
                            NPC.Center + new Vector2(Main.rand.NextFloat(-100f, 100f),
                                Main.rand.NextFloat(-50f, 50f)),
                            DustID.GoldFlame,
                            new Vector2(Main.rand.NextFloat(-1.5f, 1.5f),
                                Main.rand.NextFloat(1f, 3f)),
                            180, OroLuz, 1.4f);
                        d.noGravity = true;
                    }

                    // t=26 — EL ESTAMPIDO: el fade completó y la luz RUGE
                    // (el destello del sol lo pinta ColaSierpeSky con SU
                    // curva comprimida 20/40/80).
                    if (_tickEstado == 26 && !Main.dedServ)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122, NPC.Center);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                    }

                    if (_tickEstado >= 80)
                    {
                        _estado = EST_FLOTAR;
                        _tickEstado = 0;
                        _sentidoOrbita = Main.rand.NextBool() ? 1 : -1;
                        NPC.alpha = 0;
                        NPC.dontTakeDamage = false;
                        NPC.netUpdate = true;
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// v6.50.44 → v6.50.51 — LA BASE DEL PILAR DE LA APARICIÓN: el punto
        /// del cielo sobre el que cae la columna de luz. La lección de la
        /// v6.50.42 queda grabada: NADA de matemática de pantalla — la
        /// posición es relativa al JUGADOR (la cámara lo sigue) y SIEMPRE
        /// clamped dentro del mundo (jamás otra muerte por fuera-de-límites).
        /// </summary>
        private Vector2 PosicionAparicion(Player target)
        {
            // La órbita de pelea (target − 420): el pilar termina donde la
            // luz va a quedar flotando — el descenso TERMINA en su puesto.
            Vector2 pos = target.Center + new Vector2(0f, -420f);

            // NUNCA ENTERRADO: la base del pilar vive en el CIELO de la
            // arena (montaña o suelo alto no la hunden en el terreno).
            float cielo = target.Center.Y - 300f;
            if (pos.Y > cielo) pos.Y = cielo;

            // NUNCA FUERA DEL MUNDO: los clamps de la v6.50.42 — el jefe
            // que nace (o materializa) fuera de límites MUERE.
            float margen = 320f;
            pos.X = MathHelper.Clamp(pos.X, margen, Main.maxTilesX * 16f - margen);
            pos.Y = MathHelper.Clamp(pos.Y, margen, Main.maxTilesY * 16f - margen);
            return pos;
        }

        // (v6.50.52 — SeguirCielo MURIÓ con los actos largos de la .51:
        // la presentación deja al jefe cerca de la presa con la caída
        // frenada de la Emperatriz y EL APARECER lo baja del pilar a la
        // órbita en 80 t — ya no hay 150+900 ticks de cielo que seguir.)

        /// <summary>El cambio de sub-fase de la llegada (resetea el tick local).</summary>
        private void SubFaseLlegada(int sub)
        {
            _subLlegada = sub;
            _tickEstado = 0;
            NPC.netUpdate = true;
        }

        /// <summary>
        /// LA ÓRBITA SERENA: la luz pasea en elipse sobre la presa —
        /// la fase de leerla. Al agotarse la paciencia (o la quietud de
        /// la presa), LA LUZ ELIGE su próximo castigo — NUNCA el último.
        /// </summary>
        private void EstadoFlotar(Player target)
        {
            float velAng = (0.014f + Phase * 0.003f) * _sentidoOrbita;
            _angOrbita += velAng;
            // v6.50.44 — LA ÓRBITA QUE RESPIRA: el radio ONDULA (la luz no
            // patrulla un circuito muerto — se acerca y se aleja en olas)
            // y en la FURIA se APRIETA (la distancia de seguridad muere:
            // 320 → 260 con la misma ola encima).
            // v6.50.57 — LA ÓRBITA QUE LEE LA DISTANCIA (la mejora de IA
            // pedida: «mejora su IA y movimiento» — como los god-bosses
            // de los mods grandes): si la presa SE ALEJA la luz SE ACERCA
            // y si se APEGA TOMA ESPACIO — el dios ya no patrulla un
            // circuito muerto: MANTIENE su distancia de duelo.
            float distF = Vector2.Distance(NPC.Center, target.Center);
            float radioBase = Furia ? 260f : (Phase >= 4 ? 380f : 440f);
            float radio = radioBase +
                MathHelper.Clamp(distF - radioBase, -140f, 190f) * 0.35f;
            radio += MathF.Sin(_angOrbita * 3f) * 30f;
            Vector2 punto = target.Center + new Vector2(
                MathF.Cos(_angOrbita) * radio,
                -260f + MathF.Sin(_angOrbita) * 90f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, (punto - NPC.Center) * 0.08f, 0.14f);

            // v6.50.57 — EL QUIEBRO DEL DUELISTA (determinista — sin dados:
            // lee el EMBISTE y cam bia de lado): si la presa viene
            // DISPUESTA a cruzarse, la luz VOLTEA el sentido de giro — el
            // dios que se esquiva vivo, no el patrón que se lee dormido.
            if (_tickEstado > 0 && (_tickEstado % 45) == 0 && distF < 250f &&
                Vector2.Dot(target.velocity, NPC.Center - target.Center) > 3f)
            {
                _sentidoOrbita = -_sentidoOrbita;
            }

            // v6.50.57 — EL VOLTEO DE GRAVEDAD MURIÓ (la letra: «eso que
            // hace el jefe de cambiar la gravedad tiene que dejar de
            // hacerlo»): el suelo vuelve a ser tuyo PARA SIEMPRE.

            // LA PACIENCIA (heredada): cada fase flota MENOS… y la presa
            // quieta la derrite (anti-camping: el castigo llega YA).
            int paciencia = Math.Max(50, 120 - (Phase - 1) * 14);
            if (_ticksPresaQuieta > 90) paciencia = Math.Min(paciencia, 26);

            if (_tickEstado >= paciencia)
            {
                ElegirAtaque(target);
            }
        }

        /// <summary>
        /// LA ROTACIÓN (v6.50.44 — LA LUZ TE LEE): el menú crece con la
        /// fase y NUNCA sale el mismo plato dos veces — pero ahora el
        /// plato sale de una BOLSA PONDERADA por lo que ESTÁS HACIENDO:
        /// correr en línea recta invita al DESTELLO (te va a cruzar el
        /// camino), quedarte quieto invita a la NOVA (el castigo del
        /// anti-camping), volar alto invita al JUICIO (las columnas caen
        /// del cielo) y pegarte invita a la CRUZ (los chorros barren).
        /// La luz no tiene patrón — tiene LECTURA.
        /// </summary>
        private void ElegirAtaque(Player target)
        {
            // v6.50.57 — CADA ELECCIÓN CUENTA para el compás de la danza.
            _ataquesDesdeCoreo++;

            // v6.50.54 — EL COMPÁS DEL ESTALLIDO (fase 3+, la letra del
            // usuario: «en la fase 3 debe hacerla cada un número de veces
            // aleatorio entre 1 a 9 ataques de otro tipo»): cuando el
            // contador de ataques de OTRO tipo llega al número tirado, la
            // luz ARDE sí o sí — el estallido YA NO es «poco común».
            if (Phase >= 3 && _ataquesDesdeEstallido >= _proximoEstallidoEn)
            {
                _ataquesDesdeEstallido = 0;
                _proximoEstallidoEn = Main.rand.Next(1, 10);   // 1-9, re-tirado
                _estallidosUsadosEnFase++;
                _ultimoAtaque = EST_ESTALLIDO;
                _estado = EST_ESTALLIDO;
                _tickEstado = 0;
                NPC.netUpdate = true;
                return;
            }

            // v6.50.57 — EL COMPÁS DE LA COREOGRAFÍA (P3+): cada 6-9
            // ataques la luz NO elige — DANZA (la mejora de IA pedida: la
            // cadena de la Emperatriz de vanilla y los god-bosses de
            // Fargo's/Calamity — los ataques se pasan el turno SIN
            // respiro): EL SOL abre, las armas encadenan, EL ESTALLIDO
            // cierra. La fase escribe la danza (P3 corta · P4 completa ·
            // P5/FURIA entera).
            if (Phase >= 3 && _coreoLargo == 0 && _ataquesDesdeCoreo >= _proximoCoreoEn)
            {
                _ataquesDesdeCoreo = 0;
                _proximoCoreoEn = Main.rand.Next(6, 10);
                EncadenarCoreografia();

                // el PRIMER paso de la danza entra YA (la cola guarda el
                // resto — el patrón de sacar la cabeza).
                int primero = _coreoCola[0];
                for (int i = 1; i < _coreoLargo; i++) _coreoCola[i - 1] = _coreoCola[i];
                _coreoLargo--;

                _anteultimoAtaque = _ultimoAtaque;
                _ultimoAtaque = primero;
                _estado = primero;
                _tickEstado = 0;
                NPC.netUpdate = true;
                return;
            }

            // el menú de la fase (v6.50.54 — EL ESTALLIDO SALE de los menús
            // de 3/4/5: su frecuencia la manda EL COMPÁS de arriba; en P1/P2
            // queda en el menú con su PRESUPUESTO — 1 y 2 usos por fase.
            // Y LAS TRES ARMAS NUEVAS DE LA INVESTIGACIÓN EoL: LA DANZA
            // SOLAR desde P3, LAS LANZAS y LA CORONA desde P4).
            int[] menu = Phase switch
            {
                1 => new[] { EST_JUICIO, EST_RAYO, EST_ESTALLIDO },
                2 => new[] { EST_JUICIO, EST_RAYO, EST_NOVA, EST_RELOJ, EST_CORO, EST_ESTALLIDO },
                3 => new[] { EST_JUICIO, EST_RAYO, EST_NOVA, EST_CRUZ, EST_DESTELLO, EST_RELOJ, EST_CORO, EST_MANADA, EST_DANZA, EST_SOL },
                4 => new[] { EST_JUICIO, EST_RAYO, EST_NOVA, EST_CRUZ, EST_DESTELLO, EST_VORTICE, EST_RELOJ, EST_CORO, EST_MANADA, EST_TELAR, EST_DANZA, EST_LANZAS, EST_CORONA, EST_SOL },
                _ => new[] { EST_RAYO, EST_NOVA, EST_CRUZ, EST_DESTELLO, EST_VORTICE, EST_JUICIO, EST_RELOJ, EST_CORO, EST_MANADA, EST_TELAR, EST_DANZA, EST_LANZAS, EST_CORONA, EST_SOL },
            };

            // LA LECTURA (v6.50.44): pesos por comportamiento.
            float velH = Math.Abs(target.velocity.X);
            bool alto = target.Center.Y < NPC.Center.Y - 160f;          // el volador
            bool cerca = Vector2.Distance(NPC.Center, target.Center) < 300f; // el pegado

            int[] bolsa = new int[menu.Length * 6];
            int n = 0;
            foreach (int plato in menu)
            {
                // v6.50.54 — EL PRESUPUESTO DEL ESTALLIDO (P1/P2): 1 uso en
                // toda la fase 1, 2 en la fase 2 — gastado, el plato SALE
                // del menú hasta la siguiente fase.
                if (plato == EST_ESTALLIDO && Phase <= 2 &&
                    _estallidosUsadosEnFase >= Phase)
                    continue;

                // v6.50.59 — EL RELOJ PERMANENTE YA ESTÁ EN LA MESA: en
                // fase 2, con el reloj de TODA la fase vivo, el plato SALE
                // del menú (la catedral del tiempo cabalga con el jefe —
                // re-invocarla no suma nada; los demás platos rotan).
                if (plato == EST_RELOJ && Phase == 2 && RelojPermanenteVivo())
                    continue;

                int peso = 2;                                              // base
                if (plato == EST_DESTELLO && velH > 7f) peso = 5;         // el corredor
                if (plato == EST_NOVA && _ticksPresaQuieta > 30) peso = 6; // el quieto
                if (plato == EST_JUICIO && alto) peso = 5;                 // el volador
                if (plato == EST_CRUZ && cerca) peso = 5;                  // el pegado
                // v6.50.45 — LAS ARMAS NUEVAS LEEN TU COMPORTAMIENTO TAMBIÉN:
                // el RELOJ castiga al VOLADOR (el peso del tiempo lo baja),
                // el CORO castiga al QUIETO (los anillos lo encuentran
                // quieto) y el TELAR castiga al CORREDOR (las estrellas le
                // cierran el camino por delante).
                if (plato == EST_RELOJ && alto) peso = 5;                  // el volador
                if (plato == EST_CORO && _ticksPresaQuieta > 30) peso = 5; // el quieto
                if (plato == EST_TELAR && velH > 7f) peso = 5;             // el corredor
                // v6.50.46 — EL VÓRTICE es la firma de la FURIA: la galaxia
                // le pertenece a la fase final.
                if (plato == EST_VORTICE && Furia) peso = 4;
                // v6.50.53 — EL ESTALLIDO RADIANTE castiga al PEGADO: el que
                // abraza al sol tiene 60 t de aro encendido para ARREPENTIRSE
                // — el radio 600 telegrafiado es LA LEY del ataque.
                if (plato == EST_ESTALLIDO && cerca) peso = 5;              // el pegado
                // v6.50.54 — LAS TRES ARMAS NUEVAS TAMBIÉN LEEN TU COMPORTAMIENTO
                // (la investigación EoL): LA DANZA caza al VOLADOR (la rueda
                // lo alcanza arriba), LAS LANZAS al CORREDOR (nacen DONDE
                // VAS — la línea recta las encuentra de frente) y LA CORONA
                // al QUIETO (el anillo que espirala lo encuentra parado).
                if (plato == EST_DANZA && alto) peso = 5;                    // el volador
                if (plato == EST_LANZAS && velH > 7f) peso = 5;               // el corredor
                if (plato == EST_CORONA && _ticksPresaQuieta > 30) peso = 5;  // el quieto
                // v6.50.57 — EL SOL DEL DIOS es el plato FIRMA (peso alto
                // en la FURIA: el ataque especial del dios lleno).
                if (plato == EST_SOL) peso = 3;
                if (plato == EST_SOL && Furia) peso = 4;
                for (int w = 0; w < peso; w++) bolsa[n++] = plato;
            }
            int elegido = bolsa[Main.rand.Next(n)];

            // v6.50.57 — LA MEMORIA DOBLE (la mejora de rotación): ni el
            // ÚLTIMO plato NI el de atrás — la lectura del duelista no se
            // repite ni se hace predecible.
            if (menu.Length > 2)
            {
                int intentos = 0;
                while ((elegido == _ultimoAtaque || elegido == _anteultimoAtaque) &&
                    intentos++ < menu.Length)
                    elegido = menu[(Array.IndexOf(menu, elegido) + 1) % menu.Length];
            }
            else if (elegido == _ultimoAtaque && menu.Length > 1)
                elegido = menu[(Array.IndexOf(menu, elegido) + 1) % menu.Length];
            _anteultimoAtaque = _ultimoAtaque;
            _ultimoAtaque = elegido;

            // v6.50.54 — EL COMPÁS: los ataques de OTRO tipo alimentan el
            // contador del estallido (y cada estallido gasta su presupuesto).
            if (elegido == EST_ESTALLIDO)
            {
                _estallidosUsadosEnFase++;
                _ataquesDesdeEstallido = 0;
            }
            else _ataquesDesdeEstallido++;

            _estado = elegido;
            _tickEstado = 0;
            NPC.netUpdate = true;

            if (elegido == EST_DESTELLO)
            {
                _cadenasDestello = 1;
                PrepararDestello(target);
            }
        }

        // ==================================================================
        //  EL JUICIO DE LUZ — LAS COLUMNAS DEL CIELO
        // ==================================================================

        /// <summary>
        /// LA PRIMERA LEY: la luz cae. 30 t de alzarse (el anuncio) y
        /// LA PRIMERA OLEADA de N COLUMNAS nace ARRIBA de las posiciones
        /// PREDICHAS — el proyectil se telegrafea solo: cae LENTO (la
        /// línea de luz descendiendo) y a los 45 t ACELERA a toda
        /// velocidad. Y v6.50.44 — LA SEGUNDA OLEADA (la ley que se
        /// aprende): 25 t después, MÁS APRETADA y sobre tu POSICIÓN
        /// ACTUAL — castiga al que esquivó la primera y se quedó a
        /// mirar. Donde estabas parado, ya no existe; donde ESTÁS,
        /// tampoco.
        /// </summary>
        private void EstadoJuicio(Player target)
        {
            // se ALZA sobre la órbita: el juez mira desde arriba.
            Vector2 punto = target.Center + new Vector2(0f, -480f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, (punto - NPC.Center) * 0.06f, 0.12f);

            bool primera = _tickEstado == 30;
            bool segunda = _tickEstado == 55;
            if (primera || segunda)
            {
                int n = Math.Min(6, 2 + Phase);       // 3 en P1 → 6 en P5
                float sep = 190f - Phase * 10f;       // más apretado con la fase
                if (segunda) sep *= 0.78f;            // v6.50.44: la red SE CIERRA
                // la primera cae sobre tu RITMO (PredPresa); la segunda,
                // sobre TI (la posición cruda).
                float centro = segunda ? target.Center.X : PredPresa(target).X;
                for (int i = 0; i < n; i++)
                {
                    float x = centro + (i - (n - 1) * 0.5f) * sep +
                              Main.rand.NextFloat(-30f, 30f);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromAI(),
                            new Vector2(x, target.Center.Y - 1200f), new Vector2(0f, 7f),
                            ModContent.ProjectileType<AtaqueJefeProjectile>(),
                            (int)(NPC.damage * 0.95f), 3f, Main.myPlayer,
                            AtaqueJefeProjectile.EstiloColumnaJuicio, 0f,
                            NPC.whoAmI * 61 + i + (segunda ? 40 : 0));
                    }
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122, NPC.Center);
                if (primera)
                {
                    EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Juicio", OroLuz);
                    OndaLib.Kick(8f, 16);
                }
            }

            if (_tickEstado >= 130) { CerrarEstado(); }
        }

        // ==================================================================
        //  EL RAYO PRIMORDIAL — EL ARCO QUE TE BUSCA
        // ==================================================================

        /// <summary>
        /// EL HEREDERO DEL ALIENTO: 40 t de CARGA (el núcleo crece, los
        /// anillos aceleran) y 70 de FUEGO: el ARCO PerlinBolt GRUESO
        /// del núcleo al pecho de la presa (CINE en el PreDraw de cada
        /// máquina) + la lluvia DENSA de pernos (el daño real). En P5,
        /// al cargar, EL RECORDAR: las runas escupen tu propio estilo.
        /// </summary>
        private void EstadoRayo(Player target)
        {
            if (_rayo == 0)
            {
                _rayo = 1;
                _tickRayo = 0;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item117, NPC.Center);
                NPC.netUpdate = true;
            }
            RayoTick(target);

            if (_rayo == 0 && _tickEstado >= 120)
            {
                CerrarEstado();
            }
        }

        private void RayoTick(Player target)
        {
            if (_rayo == 0) return;
            _tickRayo++;

            if (_rayo == 1)
            {
                // LA CARGA: el núcleo se llena (el cliente dibuja el crescendo).
                if (_tickRayo >= 40)
                {
                    _rayo = 2;
                    _tickRayo = 0;
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122, NPC.Center);
                    EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Aliento", VioletaLuz);
                    NPC.netUpdate = true;
                }
                return;
            }

            // EL FUEGO (v6.50.44 — LAS LANZAS QUE TE BUSCAN): ya no
            // llueven a ciegas sobre tu cabeza — cada VOLVERA nace en el
            // borde de la luz y VUELA hacia tu RITMO (PredPresa), en
            // abanico de tres: una al centro y dos a los flancos (la
            // cortina que hay que atravesar POR DECISIÓN, no por suerte).
            if ((_tickRayo % (Furia ? 6 : 8)) == 0 && _tickRayo <= 64)
            {
                Vector2 pred = PredPresa(target);
                for (int l = 0; l < 3; l++)
                {
                    Vector2 pos = NPC.Center + new Vector2(
                        Main.rand.NextFloat(-170f, 170f),
                        Main.rand.NextFloat(-30f, 70f));
                    float ang = (pred - pos).ToRotation() + (l - 1) * 0.11f;
                    Vector2 v = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 12.5f;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), pos, v,
                            ModContent.ProjectileType<AtaqueJefeProjectile>(),
                            (int)(NPC.damage * 0.45f), 2f, Main.myPlayer,
                            AtaqueJefeProjectile.EstiloPernoEstelar, 0f,
                            NPC.whoAmI * 97 + _tickRayo * 3 + l);
                    }
                }
            }
            if (_tickRayo >= 70)
            {
                _rayo = 0;
                _tickRayo = 0;
                NPC.netUpdate = true;
            }
        }

        // ==================================================================
        //  LA NOVA PRIMORDIAL — LA CONTRACCIÓN Y EL ESTALLIDO
        // ==================================================================

        /// <summary>
        /// 45 t de contracción (el núcleo CRECE y se APRIETA — el pulso
        /// acelera en el render) y EL ESTALLIDO: 2-3 anillos de orbes
        /// ESPIRALES con HUECOS (v6.50.44: los anillos giran al abrirse
        /// — la nova que BARRE, no la que solo empuja — y los huecos se
        /// CIERRAN con la fase). En la FURIA: LA SEGUNDA NOVA — 20 t
        /// después, dos anillos rápidos con los huecos girados: la
        /// trampa para quien encontró el primer hueco y se quedó en él.
        /// </summary>
        private void EstadoNova(Player target)
        {
            if (_tickEstado < 45)
            {
                // LA CONTRACCIÓN: se detiene y se llena.
                NPC.velocity = Vector2.Lerp(NPC.velocity, Vector2.Zero, 0.15f);
                if (_tickEstado == 1)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item117, NPC.Center);
                    EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Nova", OroLuz);
                    OndaLib.Kick(6f, 12);
                }
                return;
            }

            if (_tickEstado == 45)
            {
                // EL ESTALLIDO: anillos espirales con huecos (4 gaps de ~30°
                // que se cierran con la fase: 0.26 rad en P1 → 0.17 en P5).
                int anillos = Furia ? 3 : (Phase >= 3 ? 3 : 2);
                for (int a = 0; a < anillos; a++)
                {
                    float vel = 6.5f + a * 2.2f;
                    float desfase = a * MathHelper.Pi / 9f;
                    float hueco = Math.Max(0.17f, 0.26f - (Phase - 1) * 0.022f);
                    for (int i = 0; i < 18; i++)
                    {
                        float ang = i * MathHelper.TwoPi / 18f + desfase;
                        // LOS HUECOS: 4 sectores libres (la salida).
                        float grad = Math.Abs(MathHelper.WrapAngle(ang - desfase));
                        if (grad < hueco || Math.Abs(grad - MathHelper.PiOver2) < hueco ||
                            Math.Abs(grad - MathHelper.Pi) < hueco ||
                            Math.Abs(grad - MathHelper.Pi * 1.5f) < hueco) continue;
                        // v6.50.44 — LA ESPIRAL: componente TANGENCIAL (el
                        // anillo GIRA mientras se abre — las balas no
                        // radian, BARREN).
                        Vector2 dir = new Vector2(MathF.Cos(ang), MathF.Sin(ang));
                        Vector2 tan = new Vector2(-dir.Y, dir.X) * (1.1f + a * 0.55f);
                        Vector2 v = dir * vel + tan;
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromAI(),
                                NPC.Center, v,
                                ModContent.ProjectileType<AtaqueJefeProjectile>(),
                                (int)(NPC.damage * 0.60f), 2f, Main.myPlayer,
                                AtaqueJefeProjectile.EstiloPernoEstelar, 0f,
                                NPC.whoAmI * 71 + a * 19 + i);
                        }
                    }
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122, NPC.Center);
                OndaLib.Kick(11f, 22);
                NPC.netUpdate = true;
            }

            // v6.50.44 — LA SEGUNDA NOVA (solo FURIA): 20 t después, dos
            // anillos RÁPIDOS con los huecos GIRADOS — el hueco de la
            // primera ya NO sirve: hay que leer la segunda.
            if (Furia && _tickEstado == 65)
            {
                for (int a = 0; a < 2; a++)
                {
                    float vel = 9.5f + a * 2.5f;
                    float desfase = MathHelper.Pi / 5f + a * 0.4f;   // girados
                    float hueco = 0.17f;
                    for (int i = 0; i < 18; i++)
                    {
                        float ang = i * MathHelper.TwoPi / 18f + desfase;
                        float grad = Math.Abs(MathHelper.WrapAngle(ang - desfase));
                        if (grad < hueco || Math.Abs(grad - MathHelper.PiOver2) < hueco ||
                            Math.Abs(grad - MathHelper.Pi) < hueco ||
                            Math.Abs(grad - MathHelper.Pi * 1.5f) < hueco) continue;
                        Vector2 dir = new Vector2(MathF.Cos(ang), MathF.Sin(ang));
                        Vector2 tan = new Vector2(-dir.Y, dir.X) * (1.4f + a * 0.6f);
                        Vector2 v = dir * vel + tan;
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromAI(),
                                NPC.Center, v,
                                ModContent.ProjectileType<AtaqueJefeProjectile>(),
                                (int)(NPC.damage * 0.55f), 2f, Main.myPlayer,
                                AtaqueJefeProjectile.EstiloPernoEstelar, 0f,
                                NPC.whoAmI * 73 + a * 23 + i);
                        }
                    }
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122, NPC.Center);
                OndaLib.Kick(9f, 18);
            }

            if (_tickEstado >= 95) { CerrarEstado(); }
        }

        // ==================================================================
        //  v6.50.53 — EL ESTALLIDO RADIANTE (LA IMAGEN DEL USUARIO: «te
        //  envié una imagen, crea por código un ataque del jefe que sea
        //  igual que la imagen» — el punto de luz potente: la explosión
        //  radiante con núcleo blanco-oro, anillo segmentado de emisores,
        //  rayos radiales de largo variable en 360°, chispas que vuelan y
        //  el destello que inunda la pantalla)
        // ==================================================================

        /// <summary>
        /// LA RECOGIDA (60 t SIEMPRE — ni en FURIA se acorta: el telegrafo
        /// debe leerse IGUAL todas las veces): el jefe se DETIENE, SE LLENA
        /// y el anillo se CIERRA sobre él (DibujarTelegrafos lo pinta
        /// leyendo ai[2] en cada pantalla)… y al tick 60 NACE EL PUNTO DE
        /// LUZ — el proyectil del estallido: daño en radio 600 (EL ARO que
        /// se telegrafeó) durante los primeros 12 t (la onda) y TODO el
        /// espectáculo de la imagen (los 44 rayos, el anillo de emisores,
        /// la cruz, las chispas, el flash que inunda) derritiéndose durante
        /// 2,5 s. El jefe queda PARADO en el centro de su estallido — el
        /// sol no se esconde de su propia luz.
        /// v6.50.54 — EL MULTI-ESTALLIDO (la letra: «1 vez en la fase 1,
        /// 2 veces en la fase 2… y en la fase 3 cada 1-9 ataques de otro
        /// tipo»): cada activación suelta 1 explosión en P1, 2 en P2 y 3
        /// en P3+ (t=60/105/150) — y CADA una con SU recogida telegrafiada
        /// (ai[3] lleva el tick de la próxima: el aro se re-dibuja SIEMPRE).
        /// La ESCALA («un poco más grande») viaja en ai[2] del proyectil
        /// (fase·8192 + seed) y crece con la ira del dios.
        /// </summary>
        private void EstadoEstallido(Player target)
        {
            // LA RECOGIDA: la luz se detiene y se junta.
            NPC.velocity = Vector2.Lerp(NPC.velocity, Vector2.Zero, 0.15f);

            if (_tickEstado == 1)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item117, NPC.Center);
                EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Estallido", OroLuz);
                OndaLib.Kick(5f, 10);
            }

            // EL MULTI-ESTALLIDO: 1 detonación en P1 · 2 en P2 · 3 en P3+.
            int detonaciones = Phase >= 3 ? 3 : Phase;
            int[] ticksDet = { 60, 105, 150 };
            for (int d = 0; d < detonaciones; d++)
            {
                if (_tickEstado == ticksDet[d] && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int dano = (int)(NPC.damage * 0.95f);
                    // LA ESCALA VIAJA EN EL SEED: ai[2] = fase·8192 + seed —
                    // el proyectil la separa (Seed = ai[2]%9973, fase = ai[2]/8192)
                    // y TODO el espectáculo crece con la ira del dios.
                    Projectile.NewProjectile(NPC.GetSource_FromAI(),
                        NPC.Center, Vector2.Zero,
                        ModContent.ProjectileType<AtaqueJefeProjectile>(),
                        dano, 3f, Main.myPlayer,
                        AtaqueJefeProjectile.EstiloEstallidoRadiante,
                        dano, NPC.whoAmI * 79 + _tickEstado + Phase * 8192);
                    NPC.netUpdate = true;
                }
            }

            // LA PRÓXIMA DETONACIÓN (viaja en ai[3]: el aro del telegrafo
            // se dibuja en los 60 t previos a CADA explosión — la señal
            // nunca falta, ni antes de la segunda ni de la tercera).
            _proximaExplosion = -1f;
            for (int d = 0; d < detonaciones; d++)
                if (_tickEstado < ticksDet[d]) { _proximaExplosion = ticksDet[d]; break; }

            int finEst = ticksDet[detonaciones - 1] + 45;
            if (_tickEstado >= finEst) { CerrarEstado(); }
        }

        // ==================================================================
        //  v6.50.54 — LAS TRES ARMAS NUEVAS DE LA INVESTIGACIÓN EMPERATRIZ
        //  (la petición: «intenta mejorar los ataques del jefe y su IA,
        //  investiga el mod mas popular de la emperatris de la luz, que la
        //  mejora sea tanto visual como de variedad, y que los ataques
        //  tengan sentido para un jefe de tipo DIOS»): LA DANZA SOLAR
        //  (Sun Dance — la rueda de rayos girando), LAS LANZAS ETERNAS
        //  (Ethereal Lance — la luz sembrada DONDE VAS, telegrafiada y
        //  translúcida hasta volar) y LA CORONA ETERNA (Everlasting
        //  Rainbow — el anillo de plumas prismáticas espiralando).
        // ==================================================================

        /// <summary>
        /// LA DANZA SOLAR — EL ATAQUE QUE UN DIOS-SOL DEBÍA TENER: el jefe
        /// se cierne sobre la presa y despliega SU RUEDA — SEIS rayos
        /// largos (860 px) que GIRAN despacio (una vuelta cada ~8,4 s) en
        /// TRES tandas desfasadas, cada una naciendo TRANSLÚCIDA los
        /// primeros 25 t (la regla de legibilidad de Fargo's: lo que aún
        /// no daña, se ve translúcido — el rayo se ENCIENDE de verdad
        /// cuando ya mata). La rueda CABALGA con el jefe (ai[2] = su
        /// whoAmI): hay que caminar ENTRE los rayos, moviéndose con el
        /// giro — la firma de la Emperatriz, el compás de un dios.
        /// </summary>
        private void EstadoDanza(Player target)
        {
            // SE ALZA sobre la presa: la danza barre desde arriba.
            Vector2 punto = target.Center + new Vector2(0f, -420f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, (punto - NPC.Center) * 0.06f, 0.12f);

            if (_tickEstado == 1)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    // LA RUEDA: nace en el jefe y LO SIGUE (ai[2] = whoAmI).
                    Projectile.NewProjectile(NPC.GetSource_FromAI(),
                        NPC.Center, Vector2.Zero,
                        ModContent.ProjectileType<AtaqueJefeProjectile>(),
                        (int)(NPC.damage * 0.55f), 0f, Main.myPlayer,
                        AtaqueJefeProjectile.EstiloDanzaSolar,
                        Furia ? 1f : 0f, NPC.whoAmI);
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item117, NPC.Center);
                EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Danza", OroLuz);
                OndaLib.Kick(7f, 14);
                NPC.netUpdate = true;
            }

            if (_tickEstado >= 580) { CerrarEstado(); }
        }

        /// <summary>
        /// LAS LANZAS ETERNAS — EL TELEGRAFO MÁS ELEGANTE DE VANILLA, en
        /// manos del dios: DOS tandas de lanzas de luz sembradas LEJOS
        /// (620 px, en el arco DETRÁS de tu carrera — donde VAS, no donde
        /// estás: el corredor en línea recta las encuentra de frente) que
        /// primero DIBUJAN SU TRAYECTORIA (la línea fina translúcida —
        /// inofensiva mientras se ve así) y a los 50 t VUELAN a su punto.
        /// La SEGUNDA tanda es LA SENTENCIA: cae sobre tu posición ACTUAL
        /// — el que esquivó la primera mirando, paga la segunda moviéndose.
        /// </summary>
        private void EstadoLanzas(Player target)
        {
            // A FLANCO (el arquero divino no dispara de frente).
            Vector2 lejos = target.Center + new Vector2(
                MathF.Cos(_angOrbita) * 500f, -280f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, (lejos - NPC.Center) * 0.05f, 0.10f);

            bool primera = _tickEstado == 30;
            bool segunda = _tickEstado == 92;
            if (primera || segunda)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    SembrarLanzas(target, segunda);
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item122.WithPitchOffset(segunda ? 0.25f : -0.1f), NPC.Center);
                if (primera)
                {
                    EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Lanzas", OroLuz);
                    OndaLib.Kick(6f, 12);
                }
                NPC.netUpdate = true;
            }

            if (_tickEstado >= 175) { CerrarEstado(); }
        }

        /// <summary>LA SIEMBRA: n lanzas alrededor del ancla — cada una
        /// con SU origen (620 px, el arco trasero de tu carrera) y SU
        /// destino (el punto que van a atravesar) — el proyectil dibuja
        /// la línea y vuela (ai[1]/ai[2] = el destino · ai[3] = el daño).</summary>
        private void SembrarLanzas(Player target, bool sentencia)
        {
            int n = Math.Min(14, 8 + Phase);
            // EL ARCO: la primera tanda nace DETRÁS de tu RITMO (donde VAS);
            // LA SENTENCIA nace alrededor de tu posición ACTUAL.
            Vector2 ancla = sentencia ? target.Center : PredPresa(target);
            Vector2 atras = target.velocity.LengthSquared() > 1f
                ? Vector2.Normalize(target.velocity) : new Vector2(0f, -1f);
            float angBase = atras.ToRotation();
            for (int i = 0; i < n; i++)
            {
                float f = (i - (n - 1) * 0.5f) / Math.Max(1f, n - 1);   // -1..1
                float ang = angBase + f * 2.1f + (sentencia ? MathHelper.Pi : 0f);
                Vector2 origen = ancla + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.82f) * 620f;
                // EL DESTINO: el punto que la lanza ATRAVIESA — el ancla con
                // el abanico (la cortina que hay que cruzar por decisión).
                Vector2 destino = ancla + new Vector2(f * 360f, MathF.Abs(f) * -70f);
                // LA FIRMA (ai0 = el estilo · ai[1]/ai[2] = EL DESTINO · el
                // daño viaja en el PARÁMETRO damage — el daño manual es del
                // cauce de la casa: HerirJugador con Projectile.damage).
                Projectile.NewProjectile(NPC.GetSource_FromAI(),
                    origen.X, origen.Y, 0f, 0f,
                    ModContent.ProjectileType<AtaqueJefeProjectile>(),
                    (int)(NPC.damage * 0.75f), 0f, Main.myPlayer,
                    AtaqueJefeProjectile.EstiloLanzaEterna,
                    destino.X, destino.Y);
            }
        }

        /// <summary>
        /// LA CORONA ETERNA — EL ANILLO ARCOÍRIS DE LA EMPERATRIZ: CATORCE
        /// plumas prismáticas (cada una SU color del espectro) en círculo
        /// alrededor de la presa, espiralando HACIA AFUERA y LUEGO HACIA
        /// ADENTRO (el mismo sentido, girando — la jaula que respira) y
        /// dejando el anillo guía translúcido como única advertencia. El
        /// ancla DERIVA hacia ti (el coro de la casa): la corona TE SIGUE
        /// cantando — hay que cruzarla en el compás justo.
        /// </summary>
        private void EstadoCorona(Player target)
        {
            // EL DIOS OBSERDA desde su flanco (la corona espira alrededor TUYO).
            Vector2 lejos = target.Center + new Vector2(
                MathF.Cos(_angOrbita) * 540f, -320f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, (lejos - NPC.Center) * 0.05f, 0.10f);

            if (_tickEstado == 30)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromAI(),
                        target.Center, Vector2.Zero,
                        ModContent.ProjectileType<AtaqueJefeProjectile>(),
                        (int)(NPC.damage * 0.50f), 0f, Main.myPlayer,
                        AtaqueJefeProjectile.EstiloCoronaEterna,
                        Furia ? 1f : 0f, NPC.whoAmI * 53);
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item70.WithPitchOffset(-0.3f), NPC.Center);
                EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Corona", OroLuz);
                OndaLib.Kick(6f, 13);
                NPC.netUpdate = true;
            }

            if (_tickEstado >= 230) { CerrarEstado(); }
        }

        // ==================================================================
        //  v6.50.57 — EL SOL DEL DIOS — EL ATAQUE ESPECIAL (la letra: «el
        //  jefe debe tener un ataque especial, debe convertirse en solo y
        //  luego lanzar ese sol al jugador… el sol que lanza el jefe debe
        //  perseguir lentamente al jugador, tener gravedad y que cresca al
        //  menos 5 a 10 veces su tamañao al convertirse en gigante roja y
        //  explotar en luz, bruma y formas, la cual hace daño»)
        // ==================================================================

        /// <summary>
        /// LA ASUNCIÓN: el dios SE DETIENE (no camina mientras es sol) y el
        /// sol NACE EN ÉL — el proyectil (EstiloSolJefe) lo VISTE: crece de
        /// chispa a sol entero blanco-dorado MIENTRAS el dios se convierte
        /// en él (80 t); al soltarlo EL LANZAMIENTO lo despide rumbo a la
        /// presa y el dios RETROCEDE por el retroceso (el cañón de luz
        /// empuja hacia atrás al que lo dispara). El resto del acto vive en
        /// el proyectil: la persiga LENTA con GRAVEDAD, LA GIGANTE ROJA ×6
        /// y la explosión de luz, bruma y formas.
        /// </summary>
        private void EstadoSol(Player target)
        {
            if (_tickEstado == 1)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int danoSol = Math.Max(1, (int)(NPC.damage * 1.05f));
                    Projectile.NewProjectile(NPC.GetSource_FromAI(),
                        NPC.Center, Vector2.Zero,
                        ModContent.ProjectileType<AtaqueJefeProjectile>(),
                        danoSol, 3f, Main.myPlayer,
                        AtaqueJefeProjectile.EstiloSolJefe,
                        danoSol, NPC.whoAmI);
                }
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item117.WithPitchOffset(-0.25f), NPC.Center);
                EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Sol", OroLuz);
                OndaLib.Kick(8f, 16);
                NPC.netUpdate = true;
            }

            if (_tickEstado <= 80)
            {
                // SE DETIENE y SE ALZA: el dios no camina mientras se
                // convierte — QUIETO mientras el sol lo viste.
                NPC.velocity = Vector2.Lerp(NPC.velocity,
                    new Vector2(0f, -0.35f), 0.18f);
            }
            else if (_tickEstado <= 106)
            {
                // EL RETROCESO del cañonazo (la luz devuelta lo empuja
                // atrás — el dios siente SU propio disparo).
                Vector2 atras = (NPC.Center - target.Center)
                    .SafeNormalize(Vector2.UnitY) * 2.4f;
                NPC.velocity = Vector2.Lerp(NPC.velocity, atras, 0.10f);
            }

            if (_tickEstado >= 130) CerrarEstado();
        }

        /// <summary>
        /// v6.50.57 — EL CIERRE CON COREOGRAFÍA: si la danza tiene pasos en
        /// la cola, el SIGUIENTE entra YA — sin pasar por la órbita (la
        /// cadena de la Emperatriz: los ataques se pasan el turno); si la
        /// cola está vacía, la órbita de siempre.
        /// </summary>
        private void CerrarEstado()
        {
            if (_coreoLargo > 0)
            {
                int siguiente = _coreoCola[0];
                for (int i = 1; i < _coreoLargo; i++) _coreoCola[i - 1] = _coreoCola[i];
                _coreoLargo--;

                _anteultimoAtaque = _ultimoAtaque;
                _ultimoAtaque = siguiente;

                // LA CONTABILIDAD DE CASA: el estallido encadenado gasta su
                // presupuesto y re-arma su compás; el destello encadenado
                // prepara su rumbo (los mismos deberes de ElegirAtaque).
                if (siguiente == EST_ESTALLIDO)
                {
                    _estallidosUsadosEnFase++;
                    _ataquesDesdeEstallido = 0;
                    _proximoEstallidoEn = Main.rand.Next(1, 10);
                }
                if (siguiente == EST_DESTELLO)
                {
                    _cadenasDestello = 1;
                    Player tD = Main.player[NPC.target];
                    if (tD != null && tD.active && !tD.dead)
                        PrepararDestello(tD);
                }

                _estado = siguiente;
                _tickEstado = 0;
                NPC.netUpdate = true;
                return;
            }
            _estado = EST_FLOTAR;
            _tickEstado = 0;
        }

        /// <summary>
        /// v6.50.57 — LA DANZA ESCRITA (la investigación de los jefes-DIOS:
        /// la Emperatriz de vanilla, el Mutante de Fargo's, la Supreme de
        /// Calamity — TODOS encadenan ataques sin respiro): EL SOL abre la
        /// danza (el ataque especial), las armas del dios se pasan el turno
        /// SIN volver a la órbita y EL ESTALLIDO cierra con su firma. P3 la
        /// corta · P4 la completa · P5 (LA FURIA) la larga entera.
        /// </summary>
        private void EncadenarCoreografia()
        {
            _coreoLargo = 0;
            void Paso(int s)
            {
                if (_coreoLargo < _coreoCola.Length) _coreoCola[_coreoLargo++] = s;
            }
            Paso(EST_SOL);                       // el dios SE VUELVE sol y lo lanza
            if (Phase >= 4) Paso(EST_TELAR);     // la jaula mientras el sol vuela
            Paso(EST_DANZA);                     // la rueda de rayos de la EoL
            if (Phase >= 4) Paso(EST_LANZAS);    // la sentencia sobre tu carrera
            if (Phase >= 5) Paso(EST_CORONA);    // la furia: el prisma completo
            Paso(EST_ESTALLIDO);                 // el cierre radiante SIEMPRE
        }

        // ==================================================================
        //  LA CRUZ DE LUZ — LOS CUATRO CHORROS GIRANDO (fase 3+)
        // ==================================================================

        /// <summary>
        /// LA ASPIRADORA: la luz se queda CASI quieta en su órbita y
        /// escupe CUATRO chorros de pernos en cruz — la cruz GIRA lento
        /// (0.011 rad/t: una vuelta cada ~9.5 s) y barre la arena. Y
        /// v6.50.44 — LA RESPIRACIÓN y LA CONTRACRUZ: los chorros ONDULAN
        /// su velocidad (olas de presión — hay ventanas honestas para
        /// acercarse) y en P4+ TRES chorros más giran AL REVÉS: la doble
        /// cruz que no se puede orbitar tranquila.
        /// </summary>
        private void EstadoCruz(Player target)
        {
            // se aparta de la presa: la cruz necesita espacio.
            Vector2 lejos = target.Center + new Vector2(
                MathF.Cos(_angOrbita) * 520f, -300f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, (lejos - NPC.Center) * 0.05f, 0.10f);

            if (_tickEstado >= 30 && (_tickEstado % (Furia ? 6 : 8)) == 0 && _tickEstado <= 200)
            {
                float giro = _tickEstado * 0.011f * (Furia ? 1.5f : 1f);
                // v6.50.44 — LA RESPIRACIÓN: la velocidad ONDULA (9 ± 2.6
                // — olas de presión, ventanas para acercarse).
                float pulsar = 9f + MathF.Sin(_tickEstado * 0.05f) * 2.6f;
                for (int b = 0; b < 4; b++)
                {
                    float ang = giro + b * MathHelper.PiOver2;
                    Vector2 v = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * pulsar;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromAI(),
                            NPC.Center, v,
                            ModContent.ProjectileType<AtaqueJefeProjectile>(),
                            (int)(NPC.damage * 0.50f), 2f, Main.myPlayer,
                            AtaqueJefeProjectile.EstiloPernoEstelar, 0f,
                            NPC.whoAmI * 83 + _tickEstado + b * 7);
                    }
                }
                // v6.50.44 — LA CONTRACRUZ (P4+): tres chorros girando AL
                // REVÉS (×1.35 más rápido) — la doble cruz.
                if (Phase >= 4)
                {
                    for (int b = 0; b < 3; b++)
                    {
                        float ang = -giro * 1.35f + b * (MathHelper.TwoPi / 3f);
                        Vector2 v = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * (pulsar * 0.85f);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromAI(),
                                NPC.Center, v,
                                ModContent.ProjectileType<AtaqueJefeProjectile>(),
                                (int)(NPC.damage * 0.50f), 2f, Main.myPlayer,
                                AtaqueJefeProjectile.EstiloPernoEstelar, 0f,
                                NPC.whoAmI * 87 + _tickEstado + b * 11);
                        }
                    }
                }
                if ((_tickEstado % 24) == 0)
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item9, NPC.Center);
            }

            if (_tickEstado >= 240) { CerrarEstado(); }
        }

        // ==================================================================
        //  EL DESTELLO — LA EMBESTIDA A VELOCIDAD LUZ
        // ==================================================================

        /// <summary>Prepara un destello: rumbo DIRECTO a la presa.</summary>
        private void PrepararDestello(Player target)
        {
            // v6.50.54 — CENTRADO EN EL JUGADOR (la letra: «ese dash que sea
            // mas corto y que este centrado en el jugador"): el rumbo apunta
            // DIRECTO a la posición predicha — el CORTE LATERAL de la .44
            // (pred + perp·150, el flanco de la huida) mandaba la luz «a
            // ningún lado» cuando la presa cambiaba de rumbo. El dios embiste
            // DONDE ESTÁS, no donde no puedes estar.
            Vector2 pred = PredPresa(target);
            _angDestello = (pred - NPC.Center).ToRotation();
            _minaPuesta = false;   // la mina es una por cruzamiento
            NPC.velocity *= 0.25f;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
            EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Carga", OroLuz);
            OndaLib.Kick(7f, 14);
            NPC.netUpdate = true;
        }

        /// <summary>
        /// EL HEREDERO DEL RAM: 20 t de LÍNEA GUÍA (el cliente dibuja el
        /// rayo de puntería creciendo) y EL CRUCE a 46 px/t A TRAVÉS de
        /// la posición predicha — con ESTELA de fantasmas. La CADENA
        /// (fase 3+): 2-3 destellos seguidos, cada uno con rumbo NUEVO
        /// (la vuelta en U de la v6.50.34, ahora en cualquier ángulo).
        /// </summary>
        private void EstadoDestello(Player target)
        {
            // === EL TELEGRAPH (0..20): la línea de luz que apunta ===
            if (_tickEstado <= 20)
            {
                NPC.velocity = Vector2.Lerp(NPC.velocity, Vector2.Zero, 0.14f);
                if (_tickEstado == 1)
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                return;
            }

            // === EL CRUCE: a velocidad luz por la línea (v6.50.54 — MÁS
            //     CORTO: 44→34 px/t — la embestida legible que CRUZA tu
            //     posición y frena, no la que desaparece del mapa) ===
            float vel = 34f + Phase * 2f + (Furia ? 3f : 0f);
            Vector2 rumbo = new Vector2(MathF.Cos(_angDestello), MathF.Sin(_angDestello));
            NPC.velocity = Vector2.Lerp(NPC.velocity, rumbo * vel, 0.30f);

            // v6.50.44 — LA ESTELA MINADA (P3+): al pasar junto a tu
            // posición, la luz deja UNA MINA ESTELAR — el punto donde
            // estabas deja de ser seguro (castiga el esquivo perezoso:
            // volver corriendo por donde viniste).
            if (Phase >= 3 && !_minaPuesta && _tickEstado > 20 &&
                Vector2.Distance(NPC.Center, target.Center) < 240f)
            {
                _minaPuesta = true;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromAI(),
                        target.Center, Vector2.Zero,
                        ModContent.ProjectileType<AtaqueJefeProjectile>(),
                        (int)(NPC.damage * 0.70f), 2f, Main.myPlayer,
                        AtaqueJefeProjectile.EstiloMinaEstelar, 0f,
                        NPC.whoAmI * 53 + _cadenasDestello);
                }
            }

            // el cruce terminó (v6.50.54 — CORTO Y CENTRADO): pasó AL
            // jugador y se apagó — cuando se ALEJA del objetivo, a los 58 t
            // o a 1200 px, la embestida muere (antes: 1900 px y 90 t = el
            // «dash que no va a ninguna parte»).
            float distD = Vector2.Distance(NPC.Center, target.Center);
            bool alejando = Vector2.Dot(NPC.velocity, target.Center - NPC.Center) < 0f;
            if (_tickEstado > 34 && (alejando || _tickEstado > 58 || distD > 1200f))
            {
                int maxCadena = Furia ? 3 : (Phase >= 3 ? 2 : 1);
                if (_cadenasDestello < maxCadena)
                {
                    _cadenasDestello++;
                    _tickEstado = 0;
                    PrepararDestello(target);   // rumbo NUEVO a la presa
                    return;
                }
                CerrarEstado();
            }
        }

        // ==================================================================
        //  v6.50.46 — EL VÓRTICE PRIMORDIAL (el relevo del eclipse)
        //  (la petición: «la fase final, la luz se apaga y solo brillan
        //  las balas, se ve mal, quitala y crea una nueva fase que sea
        //  mejor») — el apagón MURIÓ: la luz ya no se apaga NUNCA. El
        //  relevo es TODO lo contrario: MÁS luz. El jefe se alza sobre
        //  la presa, sus rayos GIRAN más rápido (el sol que acelera) y
        //  decreta LA GALAXIA: brazos de pernos dorados nacen en
        //  espiral alrededor del jugador y ROTAN mientras COLAPSAN
        //  hacia el centro — un remolino de estrellas que hay que leer
        //  y cruzar POR LOS HUECOS, moviéndose CON el giro. Tres
        //  oleadas (cuatro en furia), cada una girada y más hambrienta.
        // ==================================================================

        /// <summary>
        /// LA FIRMA DE LA FASE FINAL: la luz no se apaga — se ENROSACA.
        /// El jefe canaliza arriba (40 t de crescendo: los rayos del sol
        /// giran a cuádruple velocidad en el render) y suelta LA GALAXIA:
        /// 3 brazos (4 en furia) de 12 pernos cada uno, sembrados a lo
        /// largo de una espiral alrededor de la presa y lanzados con
        /// velocidad TANGENCIAL (el giro) + HUNDIMIENTO hacia adentro (el
        /// colapso) — el remolino que se cierra. Segunda galaxia girada
        /// (y tercera en furia): los huecos se mueven, el giro no se
        /// detiene. La luz INUNDA el campo: nada de oscuridad, SOLO
        /// estrellas doradas girando alrededor tuyo.
        /// </summary>
        private void EstadoVortice(Player target)
        {
            // SE ALZA y casi se clava: el sol que ordena su propia luz.
            Vector2 punto = target.Center + new Vector2(0f, -480f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, (punto - NPC.Center) * 0.05f, 0.10f);

            if (_tickEstado == 1)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item117, NPC.Center);
                EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Vortice", OroLuz);
                OndaLib.Kick(6f, 14);
            }

            // LAS GALAXIAS: dos oleadas (tres en furia), cada una GIRADA
            // y con más hambre de colapso.
            bool segunda = _tickEstado == (Furia ? 110 : 120);
            bool tercera = Furia && _tickEstado == 190;
            if (_tickEstado == 40 || segunda || tercera)
                SoltarGalaxia(target, _tickEstado == 40 ? 0 : (segunda ? 1 : 2));

            if (_tickEstado >= 250) { CerrarEstado(); }
        }

        /// <summary>
        /// LA SIEMBRA DE UNA GALAXIA: brazos en espiral alrededor de la
        /// presa — cada perno nace en su lugar de la espiral (radio de
        /// 150 a 630 px) y sale disparado TANGENCIALMENTE (la rotación
        /// del remolino) con un HUNDIMIENTO hacia adentro (el colapso).
        /// Los HUECOS entre brazos son las puertas — y giran con el
        /// remolino.
        /// </summary>
        private void SoltarGalaxia(Player target, int indice)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            int brazos = Furia ? 4 : 3;
            float giro0 = indice * 0.95f + Main.rand.NextFloat(0.35f);
            float hambre = 0.65f + indice * 0.45f;   // cada oleada colapsa más rápido
            for (int b = 0; b < brazos; b++)
            {
                float faseBrazo = giro0 + b * MathHelper.TwoPi / brazos;
                for (int p = 0; p < 12; p++)
                {
                    float tP = p / 11f;                          // 0→1 a lo largo del brazo
                    float ang = faseBrazo + tP * 1.9f;           // el enrollado
                    float r = 150f + tP * 480f;                  // del borde interno al externo
                    Vector2 pos = target.Center +
                        new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.78f) * r;
                    // LA VELOCIDAD DEL REMOLINO: tangencial (ω·r — gira
                    // más rápido cuanto más lejos, como un disco) + el
                    // hundimiento hacia adentro (la galaxia colapsa).
                    Vector2 radial = new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.78f);
                    Vector2 tangencial = new Vector2(-radial.Y, radial.X);
                    Vector2 v = tangencial * (r * 0.021f) - radial * hambre;
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), pos, v,
                        ModContent.ProjectileType<AtaqueJefeProjectile>(),
                        (int)(NPC.damage * 0.45f), 2f, Main.myPlayer,
                        AtaqueJefeProjectile.EstiloPernoEstelar, 0f,
                        NPC.whoAmI * 61 + indice * 29 + b * 13 + p);
                }
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122.WithPitchOffset(0.2f), NPC.Center);
            OndaLib.Kick(8f, 16);
            NPC.netUpdate = true;
        }

        // ==================================================================
        //  v6.50.45 — LAS ARMAS DEL MOD EN MANOS DE LA LUZ
        //  (la petición: el jefe usa LOS PROYECTILES DE LOS BASTONES)
        // ==================================================================

        /// <summary>
        /// v6.50.54 — EL RELOJ DE LA CORONA (la letra: «son 4 ataques
        /// grandes, pero no debe ser asi, debe ser un solo ataque grande
        /// centrado en el jefe y que debe moverse con el jefe»): YA NO EL
        /// ANILLO DE CUATRO — UN SOLO RELOJ GIGANTE (×7.5 — la catedral del
        /// tiempo) que NACE EN EL JEFE y CABALGA CON ÉL: el proyectil lee
        /// ai[2] = el whoAmI del dios y lo SIGUE (la goma suave de Lerp).
        /// El PESO de la arena aplasta alrededor DEL JEFE (radio 560) y
        /// cada INVERSIÓN es el pulso que HUNDE — el tiempo cae del cielo
        /// DONDE ESTÁ EL DIOS, no donde estabas tú.
        /// </summary>
        private void EstadoReloj(Player target)
        {
            // SE ALZA (el reloj necesita cielo sobre su presa).
            Vector2 punto = target.Center + new Vector2(0f, -460f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, (punto - NPC.Center) * 0.06f, 0.12f);

            if (_tickEstado == 30)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    // v6.50.59 — EL RELOJ DE TODA LA FASE 2 (la letra: «el
                    // ataque reloj de arena del jefe es muy corto, deberia
                    // durar al menos toda la fase 2 completa»): en fase 2 el
                    // reloj nace PERMANENTE (ai[1]=2) — INMORTAL mientras
                    // la fase 2 viva, cabalgando al jefe por TODA la fase
                    // (los demás platos siguen saliendo CON el reloj encima)
                    // y disuelto al primer tick de fase 3. En fases 3+ el
                    // plato es el de siempre (la catedral de 620 t). EL
                    // RELOJ ÚNICO: si el permanente YA está vivo, no nace
                    // otro — solo el anuncio de que sigue allí.
                    bool permanente = Phase == 2 && !Furia;
                    if (permanente && !RelojPermanenteVivo())
                    {
                        int idxR = Projectile.NewProjectile(NPC.GetSource_FromAI(),
                            NPC.Center, Vector2.Zero,
                            ModContent.ProjectileType<AtaqueJefeProjectile>(),
                            (int)(NPC.damage * 0.60f), 0f, Main.myPlayer,
                            AtaqueJefeProjectile.EstiloRelojGigante,
                            2f, NPC.whoAmI);
                        _relojPermanente = idxR;
                    }
                    else if (!permanente)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromAI(),
                            NPC.Center, Vector2.Zero,
                            ModContent.ProjectileType<AtaqueJefeProjectile>(),
                            (int)(NPC.damage * 0.60f), 0f, Main.myPlayer,
                            AtaqueJefeProjectile.EstiloRelojGigante,
                            Furia ? 1f : 0f, NPC.whoAmI);
                    }
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item45.WithPitchOffset(-0.4f), NPC.Center);
                EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Reloj", OroLuz);
                OndaLib.Kick(8f, 18);
                NPC.netUpdate = true;
            }

            // v6.50.59 — LA FASE 2 SUELTA EL ESTADO ANTES (120 t): el reloj
            // PERMANENTE ya vuela solo cabalgando al jefe — el dios vuelve
            // a la pelea con la catedral del tiempo ENCIMA. Las fases 3+
            // mantienen los 210 t de la catedral clásica.
            int cierreReloj = Phase == 2 ? 120 : 210;
            if (_tickEstado >= cierreReloj) { CerrarEstado(); }
        }

        /// <summary>
        /// v6.50.59 — ¿El RELOJ PERMANENTE de la fase 2 sigue vivo? (el
        /// menú lo lee: con el reloj de toda-la-fase en la mesa, el plato
        /// EST_RELOJ SALE de la rotación — el jefe pelea CON él, no lo
        /// re-invoca).
        /// </summary>
        private bool RelojPermanenteVivo()
        {
            if (_relojPermanente < 0 || _relojPermanente >= Main.maxProjectiles) return false;
            Projectile r = Main.projectile[_relojPermanente];
            return r != null && r.active &&
                r.type == ModContent.ProjectileType<AtaqueJefeProjectile>() &&
                r.ai[0] == AtaqueJefeProjectile.EstiloRelojGigante &&
                r.ai[1] >= 2f;
        }

        /// <summary>
        /// EL CORO ESPECTRAL (la petición: «el jefe deberia usar el
        /// proyectil de baston del coro espectral»). Seis notas de luz
        /// fantasmales orbitan a la PRESA — cada ciclo canta la nota
        /// siguiente y su anillo de onda se expande: el frente del anillo
        /// corta a quien atraviesa. Daño bajo pero FRECUENTE: el canto
        /// castiga al que se queda quieto escuchándolo.
        /// </summary>
        private void EstadoCoro(Player target)
        {
            // LA CANTORA: se aparta a flanco (el coro canta alrededor TUYO,
            // no de ella).
            Vector2 lejos = target.Center + new Vector2(
                MathF.Cos(_angOrbita) * 560f, -320f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, (lejos - NPC.Center) * 0.05f, 0.10f);

            if (_tickEstado == 40)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromAI(),
                        target.Center, Vector2.Zero,
                        ModContent.ProjectileType<AtaqueJefeProjectile>(),
                        (int)(NPC.damage * 0.42f), 0f, Main.myPlayer,
                        AtaqueJefeProjectile.EstiloCoroJefe,
                        Furia ? 1f : 0f, NPC.whoAmI * 37);
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item70.WithPitchOffset(-0.2f), NPC.Center);
                EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Coro", OroLuz);
                NPC.netUpdate = true;
            }

            if (_tickEstado >= 140) { CerrarEstado(); }
        }

        /// <summary>
        /// LA MANADA ASTRAL (la petición: «el jefe deberia usar el
        /// proyectil de la manada astral, pero con menos vida y mas
        /// lentos, pero en mayor numero»). Los cazadores del bastón, al
        /// servicio de la luz — pero de VERDAD: son NPCs con vida propia
        /// (MENOS VIDA: se matan), persiguen DESPACIO (MÁS LENTOS) y
        /// llegan en CAMADA (MÁS NÚMERO: dos oleadas de cinco). La luz
        /// suelta a los perros del cielo.
        /// </summary>
        private void EstadoManada(Player target)
        {
            // LA CAMADA: el jefe se alza y abre las jaulas.
            Vector2 punto = target.Center + new Vector2(0f, -560f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, (punto - NPC.Center) * 0.06f, 0.12f);

            if (_tickEstado == 30 || _tickEstado == 80)
            {
                SoltarManada(target);
                if (_tickEstado == 30)
                {
                    EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Manada", VioletaLuz);
                    OndaLib.Kick(7f, 15);
                }
            }

            if (_tickEstado >= 140) { CerrarEstado(); }
        }

        /// <summary>La jaula se abre: hasta 10 cazadores astrales vivos (la casa: NUNCA más).</summary>
        private void SoltarManada(Player target)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            int vivos = ContarCazadores();
            int n = Math.Min(5, 10 - vivos);
            for (int i = 0; i < n; i++)
            {
                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 pos = NPC.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.6f) * 90f;
                int idx = NPC.NewNPC(NPC.GetSource_FromAI(),
                    (int)pos.X, (int)pos.Y, ModContent.NPCType<CazadorAstral>(),
                    0, target.whoAmI);
                if (idx >= 0 && idx < Main.maxNPCs)
                {
                    Main.npc[idx].netUpdate = true;
                }
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122.WithPitchOffset(0.5f), NPC.Center);
            NPC.netUpdate = true;
        }

        /// <summary>Los cazadores astrales vivos en el campo.</summary>
        internal static int ContarCazadores()
        {
            int tipo = ModContent.NPCType<CazadorAstral>();
            int n = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC c = Main.npc[i];
                if (c != null && c.active && c.type == tipo) n++;
            }
            return n;
        }

        /// <summary>
        /// EL TELAR DE CONSTELACIONES — LA FIGURA DE ESTRELLA (v6.50.46 —
        /// la corrección LITERAL del pedido: «el jefe debe moverse
        /// formando una figura de tantas puntas como proyectiles se
        /// puedan lanzar al rededor del jugador y rapidamente»). El
        /// jefe YA NO corre el círculo: TRAZA LA ESTRELLA — vuela de
        /// PUNTA EN PUNTA por el SALTO DE LA ESTRELLA (pentagrama +2 en
        /// fase 4 · heptagrama +3 en furia) y cada vez que PASA por una
        /// punta, SU ESTRELLA se clava ahí: la figura que dibuja el
        /// jefe ES la jaula que te atrapa. Con la figura cerrada, el
        /// polígono se ENCIENDE cada 30 t y TODO jugador DENTRO paga —
        /// y el jefe sigue el círculo veloz vigilando su trampa.
        /// </summary>
        private void EstadoTelar(Player target)
        {
            // LAS PUNTAS: 5 en fase 4 (pentagrama) · 7 en furia (heptagrama).
            int puntas = Furia ? 7 : 5;

            if (_tickEstado == 1)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    // EL TELAR: ai[2] = el whoAmI DEL JEFE — el proyectil
                    // vigila su paso por las puntas y clava cada estrella.
                    Projectile.NewProjectile(NPC.GetSource_FromAI(),
                        target.Center, Vector2.Zero,
                        ModContent.ProjectileType<AtaqueJefeProjectile>(),
                        (int)(NPC.damage * 0.55f), 0f, Main.myPlayer,
                        AtaqueJefeProjectile.EstiloTelarJefe,
                        Furia ? 1f : 0f, NPC.whoAmI);
                }
                _telarPaso = 0;
                _tickPierna = 0;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, NPC.Center);
                EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Telar", OroLuz);
                OndaLib.Kick(8f, 16);
                NPC.netUpdate = true;
            }

            // EL CENTRO de la figura: el telar sembrado (SU posición — la
            // misma que leen TODAS las máquinas; si aún no llegó, la presa).
            Vector2 centro = target.Center;
            Projectile telar = BuscarTelar();
            if (telar != null) centro = telar.Center;

            if (_telarPaso <= puntas)
            {
                // === EL TRAZO DE LA ESTRELLA: de punta en punta, por el
                //     SALTO (pentagrama +2 · heptagrama +3) — RÁPIDO ===
                int paso = (_telarPaso * (puntas == 7 ? 3 : 2)) % puntas;
                Vector2 punta = AtaqueJefeProjectile.PuntaTelar(
                    centro, paso, puntas, NPC.whoAmI);

                // VUELO RÁPIDO a la punta (el trazo no se detiene).
                NPC.velocity = Vector2.Lerp(NPC.velocity,
                    (punta - NPC.Center) * 0.20f, 0.32f);

                // ¿LLEGÓ? (rozó la punta o el compás de la pierna murió —
                // el trazo jamás se cuelga aunque lo bloqueen).
                _tickPierna++;
                if (Vector2.Distance(NPC.Center, punta) < 80f || _tickPierna > 40)
                {
                    _telarPaso++;
                    _tickPierna = 0;
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item4.WithPitchOffset(0.25f), NPC.Center);
                }
            }
            else
            {
                // === LA FIGURA CERRADA: el CÍRCULO VELOZ alrededor de la
                //     jaula (la luz vigila su trampa — el cometa de siempre) ===
                _angOrbita += 0.058f * _sentidoOrbita;
                Vector2 punto = centro + new Vector2(
                    MathF.Cos(_angOrbita) * 470f,
                    MathF.Sin(_angOrbita) * 470f * 0.80f - 120f);
                NPC.velocity = Vector2.Lerp(NPC.velocity, (punto - NPC.Center) * 0.24f, 0.38f);
            }

            // LA ESTELA DEL CORREDOR (el cometa que traza la figura).
            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(
                    NPC.Center + new Vector2(Main.rand.NextFloat(-60f, 60f),
                        Main.rand.NextFloat(-60f, 60f)),
                    DustID.GoldFlame,
                    -NPC.velocity * 0.06f, 170, OroLuz, 1.1f);
                d.noGravity = true;
            }

            if (_tickEstado >= 340) { CerrarEstado(); }
        }

        /// <summary>
        /// v6.50.46 — EL TELAR DEL JEFE: su proyectil sembrado (ai[2] =
        /// el whoAmI de ESTE jefe — el contrato para que ambos dibujen
        /// la MISMA figura).
        /// </summary>
        private Projectile BuscarTelar()
        {
            int tipo = ModContent.ProjectileType<AtaqueJefeProjectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p == null || !p.active || p.type != tipo) continue;
                if ((int)p.ai[0] == AtaqueJefeProjectile.EstiloTelarJefe &&
                    (int)p.ai[2] == NPC.whoAmI)
                    return p;
            }
            return null;
        }

        /// <summary>
        /// EL DECRETO DEL ECLIPSE — EL CAMBIO DE FASE (v6.50.46 — la
        /// corrección del pedido: «el jefe no lo usa al cambio de fase» —
        /// NO lo usaba porque su proyectil SE ROMPÍA en el segundo tick:
        /// el estilo vive en ai[0] y el círculo SOBRESCRIBÍA ai[0] con su
        /// radio → la IA moría, el render moría, el decreto era un
        /// círculo invisible congelado. EL FIX está en el proyectil — y
        /// este estado AHORA SÍ es lo pedido: el jefe se CLAVA donde
        /// está — INMÓVIL, canalizando — y decreta: EL CÍRCULO DEL
        /// ECLIPSE nace pequeño sobre el jugador y CRECE, MÁS GRANDE
        /// con cada fase (P2 600 → P5 960 px de radio), con LA MARCA DEL
        /// OJO y las EJECUCIONES del bastón cada 15 t. El jefe queda
        /// INMÓVIL TODO el decreto — ESO es la ventana de escape.
        /// </summary>
        private void EstadoDecreto(Player target)
        {
            // v6.50.54 — YA NO INMÓVIL (la letra: «ese ataque debe estar
            // centrado en el jefe, moverse con el jefe»): el decreto
            // CABALGA con el dios — órbita LENTA alrededor de la presa
            // mientras el círculo lo sigue (el proyectil lee ai[2] = su
            // whoAmI). La ventana de escape sigue existiendo: el círculo
            // crece DESPACIO y el aro se ve venir — pero hay que leerlo
            // EN MOVIMIENTO, como se lee a un dios de verdad.
            _angOrbita += 0.010f * _sentidoOrbita;
            Vector2 punto = target.Center + new Vector2(
                MathF.Cos(_angOrbita) * 380f,
                -240f + MathF.Sin(_angOrbita) * 70f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, (punto - NPC.Center) * 0.05f, 0.10f);

            if (_tickEstado == 24)
            {
                // v6.50.54 — EL CÍRCULO: nace EN EL JEFE y LO SIGUE (la
                // petición: centrado en el jefe, moviéndose con el jefe) —
                // ai[1] = la FASE (el tamaño crece con la ira) · ai[2] = el
                // whoAmI DEL JEFE (el caballo del círculo).
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromAI(),
                        NPC.Center, Vector2.Zero,
                        ModContent.ProjectileType<AtaqueJefeProjectile>(),
                        (int)(NPC.damage * 0.80f), 0f, Main.myPlayer,
                        AtaqueJefeProjectile.EstiloDecretoJefe,
                        Phase, NPC.whoAmI);
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item88, NPC.Center);
                EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Decreto", VioletaLuz);
                NPC.netUpdate = true;
            }

            // EL JEFE ESPERA TODO el decreto (la vida del círculo: nace a
            // 80 px y crece +2 px/t hasta su radio máximo) + un respiro.
            // v6.50.46 — MÁS GRANDE POR FASE: P2 600 → P3 720 → P4 840
            // → P5 960 (la fórmula gemela de la del proyectil).
            float radioMax = 600f + (Phase - 2) * 120f;
            // EL DECRETO NO ENCADENA (la coreografía muere aquí: es el
            // cambio de FASE — después del decreto viene el aire).
            int espera = 24 + (int)((radioMax - 80f) / 2f) + 25;
            if (_tickEstado >= espera) { _estado = EST_FLOTAR; _tickEstado = 0; }
        }

        // ==================================================================
        //  LOS FONDOS DE FASE (heredados de la sierpe)
        // ==================================================================

        private void Fase4Runas()
        {
            _tickRunas++;
            if (_tickRunas >= 240 && ContarRunas() < 2)
            {
                _tickRunas = 0;
                NacerRuna();
            }
        }

        private void Fase5FuriaFondo(Player target)
        {
            _tickRunas++;
            if (_tickRunas >= 200 && ContarRunas() < 4)
            {
                _tickRunas = 0;
                NacerRuna();
            }
        }

        // v6.50.57 — FLIPGRAVITY MURIÓ (la letra: «eso que hace el jefe de
        // cambiar la gravedad tiene que dejar de hacerlo») — el método
        // entero ELIMINADO: el suelo vuelve a ser tuyo PARA SIEMPRE.

        // ==================================================================
        //  LAS RUNAS MEMORIZADAS (heredadas: la luz LEE tu estilo)
        // ==================================================================

        private static int ContarRunas()
        {
            int tipo = ModContent.ProjectileType<AtaqueJefeProjectile>();
            int n = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.type == tipo &&
                    (int)p.ai[0] == AtaqueJefeProjectile.EstiloRunaMemorizada)
                    n++;
            }
            return n;
        }

        private void NacerRuna()
        {
            float fase = Main.rand.NextFloat(MathHelper.TwoPi);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(NPC.GetSource_FromAI(),
                    NPC.Center, Vector2.Zero,
                    ModContent.ProjectileType<AtaqueJefeProjectile>(),
                    (int)(NPC.damage * 0.65f), 2f, Main.myPlayer,
                    AtaqueJefeProjectile.EstiloRunaMemorizada, fase, NPC.whoAmI * 97);
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, NPC.Center);
        }

        /// <summary>EL CAMBIO DE FASE (v6.50.45: con EL DECRETO — el círculo nace en el JEFE y CABALGA con él; el jugador gana LA ventana de escape leyendo el aro en movimiento).</summary>
        private void OnPhaseChange()
        {
            NPC.life = Math.Min(NPC.lifeMax, NPC.life + NPC.lifeMax / 20);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
            OndaLib.Kick(10f, 20);
            EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Fase",
                OroLuz, Phase, PhaseName());
            NPC.netUpdate = true;

            // v6.50.54 — EL COMPÁS DEL ESTALLIDO SE RE-ARMA EN CADA FASE: el
            // presupuesto de P1/P2 vuelve a CERO (1 uso en P1, 2 en P2) y en
            // P3+ se TIRA el primer número 1-9 (el estallido vuelve a ser
            // una CERTEZA que acecha, no una casualidad del menú).
            _estallidosUsadosEnFase = 0;
            _ataquesDesdeEstallido = 0;
            if (Phase >= 3) _proximoEstallidoEn = Main.rand.Next(1, 10);

            // v6.50.45 — EL DECRETO DEL ECLIPSE EN CADA CAMBIO DE FASE: el
            // círculo nace EN EL JEFE y LO SIGUE (v6.50.54 — centrado en el
            // dios y moviéndose con él), MÁS GRANDE con cada fase — la
            // ventana de escape se lee EN MOVIMIENTO. Solo si no está YA en
            // pleno decreto (los cambios encadenados por DPS no re-decretan:
            // un decreto a la vez).
            if (Phase >= 2 && _estado != EST_DECRETO)
            {
                _estado = EST_DECRETO;
                _tickEstado = 0;
                _rayo = 0;
                NPC.netUpdate = true;
            }
        }

        /// <summary>LA ESCALA DEL ESTALLIDO POR FASE (v6.50.54 — «ser un
        /// poco más grande»: 1.15 en P1 → 1.39 en P5 — LA MISMA fórmula que
        /// lee el proyectil desde la fase que viaja en su ai[2]).</summary>
        internal static float EscalaEstallido(int fase) =>
            1.15f + 0.06f * Math.Max(0, fase - 1);

        private string PhaseName() => Phase switch
        {
            1 => Language.GetTextValue("Mods.AethonMod.Jefe.Aethon.Nombre1"),
            2 => Language.GetTextValue("Mods.AethonMod.Jefe.Aethon.Nombre2"),
            3 => Language.GetTextValue("Mods.AethonMod.Jefe.Aethon.Nombre3"),
            4 => Language.GetTextValue("Mods.AethonMod.Jefe.Aethon.Nombre4"),
            5 => Language.GetTextValue("Mods.AethonMod.Jefe.Aethon.Nombre5"),
            _ => "?",
        };

        // ==================================================================
        //  LA MUERTE — LA CONTRACCIÓN Y EL ESTALLIDO FINAL
        // ==================================================================

        /// <summary>
        /// CheckDead → false: la luz NO muere por el camino normal —
        /// COLAPSA: 120 t de contracción total (TODO el brillo se
        /// recoge hacia el punto — el render lo dibuja) y EL ESTALLIDO:
        /// el flash que inunda la pantalla y la lluvia dorada.
        /// </summary>
        public override bool CheckDead()
        {
            if (_muriendo) return false;
            _muriendo = true;
            _tickMuerte = 0;
            NPC.life = 1;
            NPC.dontTakeDamage = true;
            _estado = EST_MURIENDO;
            _rayo = 0;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
            EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Muerte", OroLuz);
            NPC.netUpdate = true;
            return false;
        }

        private void CineMuerte()
        {
            _tickMuerte++;
            // v6.50.57 — DETENIDO DEL TODO (la letra: «el jefe debe
            // quedarse detenido»): 20 t de aquietarse y queda CLAVADO —
            // el dios arde QUIETO en su sitio hasta el estallido final.
            if (_tickMuerte >= 20) NPC.velocity = Vector2.Zero;
            else NPC.velocity = Vector2.Lerp(NPC.velocity, Vector2.Zero, 0.10f);

            // v6.50.56 — LA AGONÍA YA NO SE OSCURECE (la letra: «al final
            // de la muerte del jefe, este se oscurece, no, lo que debe
            // hacer es lanzar un ataque de brillo aun mayor»): el dios
            // muerto SE ENCIENDE — la luz del mundo CRECE hasta el
            // disparo final (×3 al límite) y las motas de fuego CAEN a su
            // cuerpo desde todas partes (la carga del último aliento).
            float carga = Math.Min(1f, _tickMuerte / 110f);
            Lighting.AddLight(NPC.Center,
                1.6f + 2.2f * carga, 1.45f + 2.0f * carga, 1.05f + 1.5f * carga);

            if (!Main.dedServ && _tickMuerte < 110 && Main.rand.NextBool(2))
            {
                float angM = Main.rand.NextFloat(MathHelper.TwoPi);
                float rM = 380f + Main.rand.NextFloat(220f);
                Vector2 posM = NPC.Center + new Vector2(MathF.Cos(angM), MathF.Sin(angM)) * rM;
                Dust dm = Dust.NewDustPerfect(posM, DustID.GoldFlame,
                    -posM.DirectionTo(NPC.Center) * (rM / 22f), 180,
                    new Color(255, 248, 220), 0.9f);
                dm.noGravity = true;
            }

            if (_tickMuerte >= 120)
            {
                OndaLib.Kick(14f, 30);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74, NPC.Center);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath55, NPC.Center);

                // v6.50.56 — EL ÚLTIMO ALIENTO: LA BOLA FINAL (la letra:
                // «creando una bola de energia similar al proyectil sol
                // pero de color blanco dorado y con mucho brillo») — nace
                // del corazón del dios y va rumbo a su asesino: 7 s de
                // fuego blanco-dorado que ALUMBRA el cielo entero.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int danoBola = Math.Max(1, (int)(NPC.damage * 1.3f));
                    Vector2 rumbo = -Vector2.UnitY * 6.5f;
                    Player presa = Main.player[NPC.target];
                    if (presa != null && presa.active && !presa.dead)
                        rumbo = (presa.Center - NPC.Center).SafeNormalize(Vector2.UnitY) * 6.5f;
                    Projectile.NewProjectile(NPC.GetSource_FromAI(),
                        NPC.Center, rumbo,
                        ModContent.ProjectileType<AtaqueJefeProjectile>(),
                        danoBola, 3f, Main.myPlayer,
                        AtaqueJefeProjectile.EstiloBolaFinal,
                        danoBola, 0f);
                }

                if (!Main.dedServ)
                {
                    for (int d = 0; d < 90; d++)
                    {
                        int idx = Dust.NewDust(NPC.Center, 160, 160, DustID.GoldFlame,
                            Main.rand.NextFloat(-13f, 13f), Main.rand.NextFloat(-15f, 4f));
                        Main.dust[idx].noGravity = true;
                    }
                }
                DropBotin();
                DisolverManada();   // v6.50.45 — sin luz que los mande, los cazadores se apagan
                NPC.active = false;
                NPC.netUpdate = true;
            }
        }

        /// <summary>
        /// v6.50.45 — LA MANADA SE APAGA CON SU LUZ: al morir el jefe, los
        /// cazadores astrales vivos se DESHACEN en polvo estelar (nada de
        /// huérfanos persiguiendo al jugador toda la sesión).
        /// </summary>
        private void DisolverManada()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            int tipo = ModContent.NPCType<CazadorAstral>();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC c = Main.npc[i];
                if (c == null || !c.active || c.type != tipo) continue;
                if (!Main.dedServ)
                {
                    for (int d = 0; d < 8; d++)
                    {
                        Dust dd = Dust.NewDustPerfect(c.Center, DustID.PurpleTorch,
                            new Vector2(Main.rand.NextFloat(-3f, 3f),
                                Main.rand.NextFloat(-3f, 1f)), 180,
                            new Color(150, 200, 255), 1.0f);
                        dd.noGravity = true;
                    }
                }
                c.active = false;
                c.netUpdate = true;
            }
        }

        /// <summary>EL BOTÍN (el método de siempre, llamado por el cine).</summary>
        private void DropBotin()
        {
            if (_yaDropeo) return;
            _yaDropeo = true;
            EsenciasModSistema.SoltarEsencia(NPC);
            Item.NewItem(NPC.GetSource_Loot(), NPC.Center,
                ModContent.ItemType<Items.Cosmetics.FormaAscendidaItem>(), 1);
            // v6.50.49 — EL AETHON MENOR (la mascota de luz: «una pequeña
            //     mascota de luz que sea Aethon original pero mas pequeño»)
            //     cae de su amo un 20% de las veces — su recuerdo vivo.
            if (Main.rand.NextFloat() < 0.20f)
                Item.NewItem(NPC.GetSource_Loot(), NPC.Center,
                    ModContent.ItemType<Items.Llamados.AethonMenorItem>(), 1);

            int killerWho = NPC.lastInteraction;
            if (killerWho < 0 || killerWho >= Main.player.Length)
            {
                for (int i = 0; i < Main.player.Length; i++)
                {
                    if (Main.player[i] != null && Main.player[i].active && NPC.playerInteraction[i])
                    {
                        killerWho = i;
                        break;
                    }
                }
            }
            if (killerWho >= 0 && killerWho < Main.player.Length)
            {
                Player player = Main.player[killerWho];
                if (player != null && player.active)
                {
                    var sp = player.GetModPlayer<Players.ShardPlayer>();
                    if (sp != null)
                    {
                        sp.ResonanceShards += 250;
                        EcoRed.AnunciarAlPortador(player, "Mods.AethonMod.Jefe.Resonancia",
                            new Color(245, 196, 81), NPC.FullName, 250);
                        EcoRed.SincronizarCronica(player);
                    }
                }
            }
            EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Reconocimiento", OroLuz);
        }

        /// <summary>Fallback exótico (si algo mata por fuera del cine).</summary>
        public override void OnKill()
        {
            if (_yaDropeo) return;
            DropBotin();
        }

        // ==================================================================
        //  EL ARTE — EL SOL DE CÓDIGO (100% píxeles de luz, cero sprites)
        //
        //  LA LECCIÓN DE LAS 3 MUERTES (dragón de código «no se parece
        //  en nada», dragón de sprites «se ve horrible», sierpe «se ve
        //  feo»): NO MÁS ANATOMÍA. La luz no tiene forma que salir mal
        //  — tiene BRILLO: núcleo, halo, rayos, coronas y chispas. Todo
        //  aditivo (la luz no proyecta silueta: IRRADIA), todo
        //  sincronizado (ai[0]/ai[1]/ai[2]/ai[3] — el contrato de la
        //  casa), todo el contrato de lote cerrado→cerrado→vanilla.
        // ==================================================================
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Main.dedServ) return false;

            VFXCore.CerrarLoteSiAbierto();
            try
            {
                float t = Main.GlobalTimeWrappedHourly;
                Vector2 posC = NPC.Center - Main.screenPosition;
                float visibilidad = 1f - (NPC.alpha / 255f);
                // v6.50.56 — EL ECLIPSE MURIÓ DEL TODO (la letra: «al final
                // de la muerte del jefe, este se oscurece, NO»): el disco
                // oscuro y el apagón del cine de muerte FUERA — el dios
                // muerto JAMÁS se apaga; su luz CRECE (brilloMuerte) hasta
                // el disparo de LA BOLA FINAL, su ataque más brillante.
                float faseInt = 0.55f + 0.45f * (Phase - 1) / 4f;   // la furia brilla más

                // === LA MUERTE: LA CARGA (todo se recoge al punto que
                //     ARDE — el sol se hace punta de lanza, no ceniza) ===
                float colapso = 1f;
                float brilloMuerte = 1f;
                if (_muriendo)
                {
                    float cargaM = Math.Min(1f, _tickMuerte / 110f);
                    colapso = MathHelper.Lerp(1f, 0.55f, cargaM);   // se recoge…
                    brilloMuerte = 1f + 1.4f * cargaM;               // …ARDIENDO: el brillo SUBE (×2.4 al final)
                }

                // === EL SOL (lote aditivo — TODO lo que brilla) ===
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                try
                {
                    float latido = 0.84f + 0.16f * MathF.Sin(t * 1.6f);
                    // v6.50.56 — LA AGONÍA ARDE: brilloMuerte SUBE mientras
                    // muere (antes: eclipse → 0.10 — el apagón de la letra).
                    float brillo = faseInt * brilloMuerte;

                    // === 1. EL VELO (el aura violeta de la profundidad) ===
                    LumenLib.Bloom(spriteBatch, posC, 540f, VioletaLuz,
                        0.10f * brillo * visibilidad, 2);

                    // === 2. EL HALO DORADO (la corona del sol) ===
                    LumenLib.BloomPulse(spriteBatch, posC, 295f * latido, OroLuz,
                        0.50f * brillo * visibilidad, t, 1.6f);

                    // === 3. LOS RAYOS RADIALES (la rueda de luz girando —
                    //     y en EL VÓRTICE la rueda ACELERA ×4: el sol que
                    //     enrosca su propia luz antes de soltar la galaxia.
                    //     v6.50.56 — también ACELERA al morir: la carga del
                    //     último aliento gira la rueda más y más rápido) ===
                    {
                        int nRayos = 8 + Phase * 2;                       // 10 en P1 → 18 en P5
                        float giro = t * ((_muriendo || NPC.ai[0] == EST_VORTICE || NPC.ai[0] == EST_DANZA) ? 0.42f : 0.10f);
                        Vector2 origen = new Vector2(VFXCore.SoftGlow.Width, VFXCore.SoftGlow.Height) * 0.5f;
                        for (int i = 0; i < nRayos; i++)
                        {
                            float ang = giro + i * MathHelper.TwoPi / nRayos;
                            float largo = (255f + 165f * faseInt) *
                                (0.78f + 0.22f * MathF.Sin(t * 2.1f + i * 1.9f)) * colapso;
                            float ancho = 34f - 12f * faseInt;
                            Color cR = (Phase >= 4) ?
                                Color.Lerp(OroLuz, VioletaLuz, 0.45f) : OroLuz;
                            spriteBatch.Draw(VFXCore.SoftGlow, posC, null,
                                cR * (0.30f * brillo * visibilidad), ang, origen,
                                new Vector2(largo / VFXCore.SoftGlow.Width * 2f,
                                            ancho / VFXCore.SoftGlow.Height * 2f),
                                SpriteEffects.None, 0f);
                        }
                    }

                    // === 4. LAS DOS CORONAS DE PERLAS (los anillos de
                    //     Saturno: órbitas elípticas de blooms girando
                    //     en sentidos opuestos — el efecto 3D) ===
                    for (int anillo = 0; anillo < 2; anillo++)
                    {
                        float rx = (anillo == 0 ? 218f : 300f) * colapso;
                        float ry = (anillo == 0 ? 134f : 90f) * colapso;
                        float w = anillo == 0 ? 0.55f : -0.38f;          // sentidos opuestos
                        Color cP = anillo == 0 ? OroLuz : VioletaLuz;
                        int nPerlas = 13;
                        for (int i = 0; i < nPerlas; i++)
                        {
                            float ang = t * w + i * MathHelper.TwoPi / nPerlas;
                            Vector2 perla = posC + new Vector2(MathF.Cos(ang) * rx,
                                MathF.Sin(ang) * ry);
                            float tw = 0.5f + 0.5f * MathF.Sin(t * 3f + i * 2.1f + anillo);
                            LumenLib.Bloom(spriteBatch, perla, 12f * colapso, cP,
                                0.42f * (0.5f + 0.5f * tw) *
                                brillo * visibilidad, 2);
                        }
                    }

                    // === 4.5 EL ANILLO ARCOÍRIS MURIÓ (v6.50.52:
                    //     «el jefe no necesita tener un arcoiris» — la banda
                    //     vive en la Forma 3 y en la mascota; el dios de la
                    //     luz es ORO Y NÚCLEO BLANCO, nada de espectro) ===

                    // === 5. LAS CHISPAS ORBITANTES (el enjambre cercano) ===
                    int nChispas = 5 + Phase;
                    Color cMota = Phase >= 4 ? VioletaLuz : OroLuz;
                    for (int m = 0; m < nChispas; m++)
                    {
                        float ang = t * (1.1f + m * 0.17f) + m * 2.1f;
                        float r = 176f * colapso + 16f * MathF.Sin(t * 2.4f + m);
                        Vector2 mota = posC + new Vector2(MathF.Cos(ang) * r,
                            MathF.Sin(ang) * r * 0.72f);
                        LumenLib.Bloom(spriteBatch, mota, 20f * colapso, cMota,
                            0.40f * brillo * visibilidad, 2);
                    }

                    // === 6. EL NÚCLEO (el corazón blanco de Aethon —
                    //     v6.50.56: NUNCA más el rim del eclipse: el corazón
                    //     ARDE entero hasta el último tick) ===
                    float tamNucleo = 130f * latido * colapso;
                    {
                        LumenLib.Bloom(spriteBatch, posC, tamNucleo, NucleoBlanco,
                            1.0f * brillo * visibilidad, 3);
                        LumenLib.Bloom(spriteBatch, posC, tamNucleo * 0.42f, OroLuz,
                            0.65f * brillo * visibilidad, 2);
                    }

                    // === 7. LA CARGA DEL RAYO (el crescendo del núcleo) ===
                    if (NPC.ai[1] == 1f && !_muriendo)
                    {
                        float prog = Math.Min(1f, _tickRayoLocal / 40f);
                        LumenLib.Bloom(spriteBatch, posC, 88f + 132f * prog,
                            NucleoBlanco, 0.45f * prog * visibilidad, 3);
                    }

                    // === 8. LOS TELEGRAPHS (la casa: todo ataque se
                    //     anuncia — y con la oscuridad MUERTA (v6.50.41)
                    //     SIEMPRE se ven, en el propio pase del mundo) ===
                    DibujarTelegrafos(spriteBatch, NPC, posC, Phase);

                    // === 9. LA ESTELA DEL DESTELLO (los fantasmas del cruce) ===
                    if (NPC.ai[0] == EST_DESTELLO && NPC.ai[2] > 20f && !_muriendo &&
                        NPC.velocity.LengthSquared() > 400f)
                    {
                        Vector2 atras = -Vector2.Normalize(NPC.velocity);
                        for (int g = 1; g <= 4; g++)
                        {
                            Vector2 fantasma = posC + atras * (g * 74f);
                            LumenLib.Bloom(spriteBatch, fantasma, 62f - g * 9f,
                                OroLuz, 0.26f / g * visibilidad, 2);
                        }
                    }

                    // === 10. LA MUERTE: EL ESTALLIDO FINAL (el flash) ===
                    // v6.50.57 — LA INUNDACIÓN MÁS LARGA Y MÁS GRANDE (la
                    // letra: «explotar en brillo intenso»): 50 t de destello
                    // que ENGORDA — el estallido final del dios detenido,
                    // ARDIENDO en su sitio.
                    if (_muriendo && _tickMuerte >= 110 && _tickMuerte <= 160)
                    {
                        float fp = (_tickMuerte - 110) / 50f;   // 0→1: la inundación
                        float engorda = 1f + 0.35f * MathF.Sin(fp * MathHelper.Pi);
                        LumenLib.Bloom(spriteBatch, posC,
                            (1000f + 3800f * fp) * engorda, NucleoBlanco,
                            0.95f * (1f - fp * 0.55f), 4);
                        LumenLib.Bloom(spriteBatch, posC,
                            (680f + 3000f * fp) * engorda, OroLuz,
                            0.65f * (1f - fp * 0.45f), 3);
                    }
                }
                finally { spriteBatch.End(); }

                // === EL RAYO PRIMORDIAL (PerlinBolt — el arco de la casa;
                //     corre en TODAS las máquinas leyendo ai[1]) ===
                if (NPC.ai[1] != _rayoPrevio)
                {
                    _rayoPrevio = NPC.ai[1];
                    _tickRayoLocal = 0f;
                }
                else if (NPC.ai[1] != 0f) _tickRayoLocal++;

                if (!_muriendo && (NPC.ai[1] == 1f || NPC.ai[1] == 2f))
                {
                    Player presa = Main.player[NPC.target];
                    if (presa != null && presa.active && !presa.dead)
                    {
                        bool cargando = NPC.ai[1] == 1f;
                        float prog = cargando ? Math.Min(1f, _tickRayoLocal / 40f) : 1f;
                        Vector2 nucleo = NPC.Center - Main.screenPosition;
                        Vector2 pecho = presa.Center - Main.screenPosition;
                        float ancho = (1.2f + 3.4f * prog) * (cargando ? 0.6f : 1f);
                        int flick = (int)(Main.GameUpdateCount / 3u);
                        StormLib.PerlinBolt(spriteBatch, nucleo, pecho,
                            NPC.whoAmI * 71 + 13, flick, ancho, OroLuz, NucleoBlanco,
                            (cargando ? 0.35f : 0.95f) * visibilidad);
                    }
                }
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }
            finally
            {
                // v6.50.53 — EL FANTASMA DE LOS NPCS, MUERTO: el PerlinBolt
                // del RAYO (la carga y el fuego, ai[1] 1-2) deja el lote
                // ABIERTO EN ADITIVO — RayoStrip.CerrarLote reabre con
                // BlendState.Additive para los gorros del rayo, y el
                // ReabrirLoteVanilla de abajo era un NO-OP («ya hay Begin
                // vivo: no lo pisamos») → TODO NPC dibujado después del
                // jefe salía ADITIVO: el FANTASMA TRANSPARENTE del usuario
                // («hay algun ataque del jefe que al hacerlo los npc se
                // vuelven transparente, creo que es cuando lanza rayo»).
                // LA CURA: si quedó un lote abierto que NO es el de
                // vanilla, se CIERRA y se reabre el de vanilla — el estado
                // de salida de PreDraw es SIEMPRE el lote NPC de vanilla
                // (AlphaBlend + Main.Transform), con rayo o sin él.
                VFXCore.CerrarLoteSiAbierto();
                VFXCore.ReabrirLoteVanilla();
            }
            return false; // el sol de código se dibuja a sí mismo
        }

        // EL TEMPO LOCAL del rayo (el cliente cuenta su propio tick para
        // el crescendo — ai[1] viaja por paquetes cada 12 ticks).
        private float _tickRayoLocal = 0f;
        private float _rayoPrevio = 0f;

        /// <summary>
        /// LOS TELEGRAPHS PÚBLICOS (v6.50.37): la línea guía del destello
        /// y el pulso de la nova — dibujables desde CUALQUIER lote
        /// aditivo (el PreDraw del mundo los pide en su propio pase).
        /// </summary>
        internal static void DibujarTelegrafos(SpriteBatch sb, NPC npc, Vector2 posC, int faseTelegrafo)
        {
            // EL DESTELLO: la LÍNEA GUÍA — el rayo de puntería creciendo
            // en el rumbo del cruce (ai[3] = el ángulo).
            if (npc.ai[0] == EST_DESTELLO && npc.ai[2] <= 20f)
            {
                float prog = npc.ai[2] / 20f;
                float angD = npc.ai[3];
                Vector2 dirD = new Vector2(MathF.Cos(angD), MathF.Sin(angD));
                Vector2 origenL = new Vector2(VFXCore.SoftGlow.Width, VFXCore.SoftGlow.Height) * 0.5f;
                float largoL = 1900f * prog;
                sb.Draw(VFXCore.SoftGlow, posC + dirD * (largoL * 0.5f), null,
                    OroLuz * (0.30f + 0.25f * prog), angD, origenL,
                    new Vector2(largoL / VFXCore.SoftGlow.Width,
                                (7f + 9f * prog) / VFXCore.SoftGlow.Height),
                    SpriteEffects.None, 0f);
                // EL ANILLO que se cierra (el compás del cruce).
                OndaLib.Pulse(sb, posC, prog, 220f, OroLuz, 0.55f, npc.whoAmI);
            }

            // LA NOVA: la contracción (el pulso que se APRIETA).
            if (npc.ai[0] == EST_NOVA && npc.ai[2] <= 45f)
            {
                float prog = npc.ai[2] / 45f;
                OndaLib.Pulse(sb, posC, 1f - prog,
                    420f - 300f * prog, OroLuz, 0.60f, npc.whoAmI + 3);
            }

            // v6.50.53/54 — EL ESTALLIDO RADIANTE: LA RECOGIDA — 60 t de
            //     telegrafo antes de CADA detonación (ai[3] lleva el tick de
            //     la PRÓXIMA — el multi-estallido re-dibuja su aro SIEMPRE)
            //     con CUATRO señales: EL LÍMITE (el aro del radio de peligro
            //     PULSANDO y creciendo con la fase — 600·ESCALA: la MISMA
            //     cifra de la hitbox), LAS CATORCE BRASAS (el anillo
            //     segmentado CAYÉNDOSE en espiral), EL NÚCLEO que SE LLENA
            //     y LAS AGUJAS (la luz CORRIENDO hacia el punto).
            if (npc.ai[0] == EST_ESTALLIDO && npc.ai[3] > 0f &&
                npc.ai[2] >= npc.ai[3] - 60f && npc.ai[2] < npc.ai[3])
            {
                float proxima = npc.ai[3];
                float prog = (npc.ai[2] - (proxima - 60f)) / 60f;
                float escalaR = EscalaEstallido(faseTelegrafo);   // el aro telegrafiado = la hitbox
                float tg = Main.GlobalTimeWrappedHourly;

                // (a) EL LÍMITE: el aro fino del radio de peligro (600·escala
                //     px — LA MISMA cifra de la hitbox de la onda) pulsando y
                //     ENCENDIÉNDOSE a medida que el estallido llega.
                float alfaL = 0.22f + 0.30f * prog + 0.10f * MathF.Sin(tg * 9f);
                Vector2 ringTam = new Vector2(VFXCore.Ring.Width, VFXCore.Ring.Height);
                sb.Draw(VFXCore.Ring, posC, null, OroLuz * alfaL, 0f,
                    ringTam * 0.5f, VFXCore.RingQuadSize(600f * escalaR) / ringTam,
                    SpriteEffects.None, 0f);

                // (b) LAS CATORCE BRASAS: el anillo segmentado CAYENDO al
                //     centro (760·escala → 150 — las brasas de la imagen,
                //     pero en reversa: se RECOGEN antes de explotar).
                float rB = MathHelper.Lerp(760f * escalaR, 150f, prog);
                for (int i = 0; i < 14; i++)
                {
                    float angB = i * MathHelper.TwoPi / 14f + prog * 2.4f + tg * 0.25f;
                    Vector2 e = posC + new Vector2(MathF.Cos(angB), MathF.Sin(angB)) * rB;
                    LumenLib.Bloom(sb, e, 22f + 8f * prog, OroLuz,
                        0.5f + 0.3f * prog, 2);
                }

                // (c) EL NÚCLEO que se llena (el punto de luz creciendo —
                //     el crescendo del rayo, pero GRANDE: este es EL estallido).
                LumenLib.Bloom(sb, posC, 90f + 140f * prog, NucleoBlanco,
                    0.35f + 0.50f * prog, 3);

                // (d) LAS AGUJAS que convergen (la luz CORRIENDO hacia el
                //     punto — deterministas: Hash01 con el flick del rayo).
                Vector2 origenS = new Vector2(VFXCore.SoftGlow.Width, VFXCore.SoftGlow.Height) * 0.5f;
                int flick = (int)(Main.GameUpdateCount / 3u);
                for (int i = 0; i < 10; i++)
                {
                    float angA = VFXCore.Hash01(npc.whoAmI, i, 50) * MathHelper.TwoPi + tg * 0.5f;
                    float rA = 620f * escalaR * (0.30f + 0.70f * VFXCore.Hash01(npc.whoAmI, i, 51 + (flick % 3)));
                    float len = 120f + 80f * prog;
                    Vector2 dirA = new Vector2(MathF.Cos(angA), MathF.Sin(angA));
                    Vector2 centroA = posC + dirA * (rA - len * 0.5f);
                    sb.Draw(VFXCore.SoftGlow, centroA, null,
                        OroLuz * (0.30f + 0.20f * prog), angA, origenS,
                        new Vector2(len / VFXCore.SoftGlow.Width, 4f / VFXCore.SoftGlow.Height),
                        SpriteEffects.None, 0f);
                }
            }
        }
    }
}
