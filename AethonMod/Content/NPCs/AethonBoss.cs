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
        private const int EST_ECLIPSE = 7;      // la luz se apaga
        private const int EST_RELOJ = 8;        // v6.50.45 — EL RELOJ DE ARENA GIGANTE
        private const int EST_CORO = 9;         // v6.50.45 — EL CORO ESPECTRAL
        private const int EST_MANADA = 10;      // v6.50.45 — LA MANADA ASTRAL (camada)
        private const int EST_TELAR = 11;       // v6.50.45 — EL TELAR (el círculo veloz)
        private const int EST_DECRETO = 12;     // v6.50.45 — EL DECRETO DEL ECLIPSE (cambio de fase)
        private const int EST_MURIENDO = 99;    // la contracción final

        // === LA LLEGADA — LOS SUB-ESTADOS (viajan en ai[1] durante
        //     EST_NACIENDO; 10-13 para NO chocar con el contrato del
        //     rayo 0-3) ===
        public const int SUB_TEMBLOR = 10;      // el mundo tiembla (150 t)
        public const int SUB_CARRERA = 11;      // el tiempo CORRE al mediodía
        public const int SUB_CLIMAX = 12;       // EL DESTELLO nace del sol + Aethon nace BAJO él
        public const int SUB_DESCENSO = 13;     // la luz cae a su órbita de pelea

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
        private int _subLlegada = SUB_TEMBLOR;   // v6.50.37 — la fase de LA LLEGADA

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

        // === EL VOLTEO Y LAS RUNAS (heredados de la sierpe) ===
        private int _tickGravedad = 0;
        private int _tickRunas = 0;

        // === LA FASE (los umbrales de siempre) ===
        private int Phase = 1;

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

            // === LA LLEGADA — EL PRIMER TICK (v6.50.37: la secuencia completa) ===
            if (!_nacio)
            {
                NPC.Center = target.Center + new Vector2(0f, -860f);   // escondido SOBRE el cielo
                NPC.velocity = Vector2.Zero;
                NPC.alpha = 255;          // invisible hasta EL FLASH del climax
                NPC.dontTakeDamage = true;
                _subLlegada = SUB_TEMBLOR;
                _tickEstado = 0;
                _estado = EST_NACIENDO;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    // v6.50.39 — NUNCA MÁS EL CORTE AL ALBA: si era de noche,
                    // la noche entera CORRE en el acto 11 (la luna barre el
                    // cielo y el alba llega SOLA) — el sol no se teletransporta.
                    EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Presentacion", OroLuz);
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74, NPC.Center);
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
            }

            // === EL FADE DE NACIMIENTO (la materialización en la CÚSPIDE
            //     DEL PILAR, envuelta en el pico del destello del cielo —
            //     v6.50.44: la luz cae del CIELO, no del sol) ===
            if (NPC.alpha > 0)
            {
                bool materializar = _estado == EST_NACIENDO &&
                    _subLlegada == SUB_CLIMAX && _tickEstado >= 45;
                if (materializar)
                    NPC.alpha = Math.Max(0, NPC.alpha - 13);
                if (NPC.alpha == 0) NPC.dontTakeDamage = false;
            }

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
                case EST_ECLIPSE: EstadoEclipse(target); break;
                case EST_RELOJ: EstadoReloj(target); break;
                case EST_CORO: EstadoCoro(target); break;
                case EST_MANADA: EstadoManada(target); break;
                case EST_TELAR: EstadoTelar(target); break;
                case EST_DECRETO: EstadoDecreto(target); break;
            }

            // === LOS ATAQUES DE FONDO (las runas recuerdan — heredado) ===
            switch (Phase)
            {
                case 4: Fase4Runas(); break;
                case 5: Fase5FuriaFondo(target); break;
            }

            // === LA LUZ DEL MUNDO: la luz inunda (y en el eclipse MUERE) ===
            if (_rayo == 3f || _estado == EST_ECLIPSE)
                Lighting.AddLight(NPC.Center, new Vector3(0.06f, 0.04f, 0.10f)); // la luz apagada
            else
                Lighting.AddLight(NPC.Center, new Vector3(1.55f, 1.35f, 0.95f)); // el sol vivo (más grande)

            // === EL CONTRATO MP (la casa): estado/subfase/tick/param viajan ===
            NPC.ai[0] = _estado;
            NPC.ai[1] = _estado == EST_NACIENDO ? _subLlegada : _rayo;
            //     llegada: 10 temblor · 11 carrera · 12 climax · 13 descenso
            //     pelea: 0 nada · 1 rayo cargando · 2 rayo ardiendo · 3 ECLIPSE
            //     (v6.50.45: los estados nuevos — 8 reloj · 9 coro · 10 manada
            //      · 11 telar · 12 decreto — viajan en ai[0] y usan ai[1]=0
            //      salvo el decreto, que manda LA FASE por ai[1] al círculo
            //      desde su spawn, no desde aquí)
            NPC.ai[2] = _tickEstado;
            NPC.ai[3] = _angDestello;   // el rumbo del destello (la línea guía del cliente)

            // MP: la luz respira por el cable cada 12 ticks.
            if ((Main.GameUpdateCount % 12u) == 0u) NPC.netUpdate = true;
        }

        /// <summary>
        /// v6.50.45 — EL FIX DEL JEFE QUE SE ESFUMABA (los DOS síntomas del
        /// usuario: «si el jefe es invocado y el jugador se mueve el jefe
        /// desaparece» + «a veces no termina de ser invocado»): vanilla
        /// CheckActive mata a cualquier NPC cuyo timeLeft expire — NewNPC
        /// lo pone en 937 t y SOLO se refresca si un jugador vive dentro del
        /// rectángulo ±(1180, 760) px del NPC. El jefe nace 860 px SOBRE el
        /// jugador (FUERA del rectángulo vertical desde el TICK 1) y durante
        /// la LLEGADA (temblor 150 + carrera hasta ~900 t desde que la
        /// v6.50.43 lo hizo invocable A CUALQUIER HORA) jamás se acerca:
        /// timeLeft NUNCA se refresca y a los 15,6 s la luz MUERE EN SECRETO
        /// a media llegada — invocado cerca del mediodía llegaba vivo, por
        /// la tarde/noche moría en cámara: el «a veces». AHORA la LLEGADA y
        /// la MUERTE son CINE (nadie las interrumpe — la vida del jefe las
        /// maneja su IA) y el jefe escondido SIGUE AL JUGADOR cada tick
        /// (SeguirEscondido). Durante la PELEA el comportamiento vanilla
        /// queda intacto (el jefe persigue: siempre está cerca).
        /// </summary>
        public override bool CheckActive()
        {
            if (_estado == EST_NACIENDO) return false;   // la llegada es cine
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
        /// LA LLEGADA (v6.50.41/.42) — EL MEDIO DÍA DEL DESTELLO:
        /// (10) EL MUNDO TIEMBLA — la tierra sacude la pantalla 2.5 s;
        /// (11) EL TIEMPO CORRE — SIN TELETRANSPORTE (v6.50.41: «el sol
        /// no se haga teletransportación… si está más allá del centro,
        /// un día completo avanza con noche completa, un nuevo día hasta
        /// el amanecer»): si el sol está MÁS ALLÁ del centro, la carrera
        /// recorre el RESTO del día, la NOCHE COMPLETA y el nuevo día
        /// desde el amanecer hasta el mediodía — un día entero pasando
        /// VISIBLEMENTE; si está ANTES del centro, solo corre hasta él.
        /// El reloj aterriza en 27000 exacto, desacelerando: el sol se
        /// POSA, no salta (el bug de la v6.50.40: con el sol en la tarde
        /// la condición «time >= 26999» se cumplía al INSTANTE y el sol
        /// saltaba HACIA ATRÁS al mediodía);
        /// (12) EL DESTELLO NACE DEL SOL — un brillo radial CENTRADO en
        /// el sol que se difumina hacia los bordes hasta ser transparente
        /// (lo pinta ColaSierpeSky). El sol NO SE APAGA: sigue ahí,
        /// ardiendo. Y Aethon NO NACE DEL CENTRO del sol: se materializa
        /// BAJO él, en el borde inferior de su halo;
        /// (13) EL DESCENSO — la luz cae del mediodía a su órbita de
        /// pelea (la oscuridad y el sol negro MURIERON con la v6.50.41:
        /// «mejor quita la capa de oscuridad, no se ve nada bien»).
        /// </summary>
        private void EstadoNaciendo(Player target)
        {
            switch (_subLlegada)
            {
                case SUB_TEMBLOR:
                {
                    // escondido sobre el cielo, el mundo SACUDE la pantalla
                    // (los kicks los padece cada cliente vía el espejo).
                    // v6.50.45 — LA SOMBRA QUE TE SIGUE: el jefe escondido
                    // CAMINA CONTIGO (nace 860 px sobre TI, no sobre el punto
                    // del llamado — el jugador que se muda durante el temblor
                    // no se lo lleva puesto, y vanilla CheckActive agradece
                    // la cercanía que ya no puede matarlo).
                    SeguirEscondido(target);
                    if (_tickEstado == 1)
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122, NPC.Center);
                    if (_tickEstado >= 150) SubFaseLlegada(SUB_CARRERA);
                    break;
                }

                case SUB_CARRERA:
                {
                    // v6.50.45 — LA SOMBRA SIGUE CAMINANDO CONTIGO (la lección
                    // del bug: el jefe clavado en el punto del llamado era un
                    // muerto que caminaba — el reloj corre, la sombra acompaña).
                    SeguirEscondido(target);

                    // EL TIEMPO CORRE, DE FORMA NATURAL (v6.50.41):
                    // AethonLlegadaSistema.ModifyTimeRate lleva el reloj —
                    // el RESTANTE hasta el PRÓXIMO mediodía (por la noche
                    // completa si el sol ya pasó el centro: la carrera
                    // recorre la tarde, la noche, el amanecer y la mañana
                    // del nuevo día — un día entero, VISIBLEMENTE) y el
                    // aterrizaje desacelera hasta POSARSE en 27000.
                    NPC.velocity = Vector2.Zero;
                    if (Main.netMode != NetmodeID.MultiplayerClient &&
                        Main.dayTime && Main.time >= 26999.0 && Main.time <= 27001.0)
                    {
                        // EL MEDIO DÍA EXACTO — y SOLO desde la llegada de la
                        // mañana: la ventana [26999, 27001] es el aterrizaje
                        // (menos de un tick de sol — la corrección sub-tick,
                        // no un salto). La TARDE ya NO dispara esto: el sol
                        // pasó el centro y la carrera da la vuelta entera.
                        Main.time = 27000.0;
                        TiempoCorriendo = false;
                        TiempoCongelado = true;
                        SubFaseLlegada(SUB_CLIMAX);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item117, NPC.Center);
                        EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.LlegadaLuz", OroLuz);
                    }
                    break;
                }

                case SUB_CLIMAX:
                {
                    // v6.50.44 — EL DESCENSO DEL CIELO (la petición: «el
                    // jefe no apareció desde el punto del sol, ya que no
                    // aparece ahí… que aparezca un destello de luz desde
                    // arriba y que el jefe aparezca desde ahí bajando del
                    // cielo, como la emperatriz de la luz»). El nacimiento
                    // «bajo el sol» MUERE — en la práctica el jefe nunca
                    // estaba DONDE el sol (la cámara de cada jugador mira
                    // a otro lado): ahora la luz NO baja del sol, baja DEL
                    // CIELO: UN PILAR DE LUZ cae desde lo alto sobre la
                    // arena y Aethon se materializa EN SU CÚSPIDE,
                    // envuelto en el destello, y DESCIENDE por él.
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
                    if (_tickEstado == 44)
                    {
                        // LA MATERIALIZACIÓN: en la CÚSPIDE del pilar — alto
                        // en el cielo, sobre la arena. Posición
                        // SERVER-SEGURA de la v6.50.42 (relativa al jugador
                        // + clamps, SIN matemática de pantalla — la lección
                        // queda grabada para siempre).
                        NPC.Center = PosicionAparicion(target) - new Vector2(0f, 760f);
                        NPC.netUpdate = true;
                    }
                    if (_tickEstado == 52)
                    {
                        // EL PICO DEL DESTELLO: el cielo CIEGA y la luz RUGE.
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122, NPC.Center);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                    }
                    if (_tickEstado >= 120) SubFaseLlegada(SUB_DESCENSO);
                    break;
                }

                case SUB_DESCENSO:
                {
                    // v6.50.44 — EL DESCENSO DEL CIELO: la luz BAJA desde
                    // la cúspide del pilar hasta su órbita de pelea,
                    // dejando una lluvia de chispas doradas (el rastro de
                    // la caída — «bajando del cielo», como la emperatriz).
                    // El sol sigue clavado en el centro mientras la luz
                    // viva (el congelado del mediodía de la v6.50.42).
                    Vector2 punto = target.Center + new Vector2(0f, -420f);
                    NPC.velocity = Vector2.Lerp(NPC.velocity, (punto - NPC.Center) * 0.02f, 0.10f);

                    // LA LLUVIA DE LUZ: chispas que caen con la luz.
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

                    if (_tickEstado >= 90)
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
        /// v6.50.44 — LA BASE DEL PILAR DE LA APARICIÓN: el punto del cielo
        /// sobre el que cae la columna de luz (la entrada de la
        /// EMPERATRIZ: el destello desde arriba y el jefe bajando del
        /// cielo). La lección de la v6.50.42 queda grabada: NADA de
        /// matemática de pantalla — la posición es relativa al JUGADOR
        /// (la cámara lo sigue) y SIEMPRE clamped dentro del mundo
        /// (jamás otra muerte por fuera-de-límites).
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

        /// <summary>
        /// v6.50.45 — LA SOMBRA QUE TE SIGUE: el jefe escondido (temblor y
        /// carrera) vive 860 px SOBRE LA PRESA — sobre el JUGADOR, no
        /// sobre el punto del llamado — y la sigue cada tick (con los
        /// clamps de la casa: jamás fuera del mundo). Matar el bug con
        /// estilo: vanilla CheckActive ya no puede ejecutarlo (véase
        /// CheckActive), y de paso el jugador que se muda durante la
        /// llegada no le roba el clímax.
        /// </summary>
        private void SeguirEscondido(Player target)
        {
            Vector2 pos = target.Center + new Vector2(0f, -860f);
            pos.X = MathHelper.Clamp(pos.X, 320f, Main.maxTilesX * 16f - 320f);
            pos.Y = MathHelper.Clamp(pos.Y, 160f, Main.maxTilesY * 16f - 320f);
            NPC.Center = pos;
            NPC.velocity = Vector2.Zero;
        }

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
            float radio = Furia ? 260f : (Phase >= 4 ? 380f : 440f);
            radio += MathF.Sin(_angOrbita * 3f) * 30f;
            Vector2 punto = target.Center + new Vector2(
                MathF.Cos(_angOrbita) * radio,
                -260f + MathF.Sin(_angOrbita) * 90f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, (punto - NPC.Center) * 0.08f, 0.14f);

            // EL VOLTEO (P3+, heredado): el suelo deja de ser tuyo.
            if (Phase >= 3)
            {
                _tickGravedad++;
                if (_tickGravedad >= 480)
                {
                    _tickGravedad = 0;
                    FlipGravity(target);
                }
            }

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
            // el menú de la fase
            int[] menu = Phase switch
            {
                1 => new[] { EST_JUICIO, EST_RAYO },
                2 => new[] { EST_JUICIO, EST_RAYO, EST_NOVA, EST_RELOJ, EST_CORO },
                3 => new[] { EST_JUICIO, EST_RAYO, EST_NOVA, EST_CRUZ, EST_DESTELLO, EST_RELOJ, EST_CORO, EST_MANADA },
                4 => new[] { EST_JUICIO, EST_RAYO, EST_NOVA, EST_CRUZ, EST_DESTELLO, EST_ECLIPSE, EST_RELOJ, EST_CORO, EST_MANADA, EST_TELAR },
                _ => new[] { EST_RAYO, EST_NOVA, EST_CRUZ, EST_DESTELLO, EST_ECLIPSE, EST_JUICIO, EST_RELOJ, EST_CORO, EST_MANADA, EST_TELAR },
            };

            // LA LECTURA (v6.50.44): pesos por comportamiento.
            float velH = Math.Abs(target.velocity.X);
            bool alto = target.Center.Y < NPC.Center.Y - 160f;          // el volador
            bool cerca = Vector2.Distance(NPC.Center, target.Center) < 300f; // el pegado

            int[] bolsa = new int[menu.Length * 6];
            int n = 0;
            foreach (int plato in menu)
            {
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
                for (int w = 0; w < peso; w++) bolsa[n++] = plato;
            }
            int elegido = bolsa[Main.rand.Next(n)];

            if (elegido == _ultimoAtaque && menu.Length > 1)
                elegido = menu[(Array.IndexOf(menu, elegido) + 1) % menu.Length];
            _ultimoAtaque = elegido;

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

            if (_tickEstado >= 130) { _estado = EST_FLOTAR; _tickEstado = 0; }
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
                _estado = EST_FLOTAR;
                _tickEstado = 0;
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

            if (_tickEstado >= 95) { _estado = EST_FLOTAR; _tickEstado = 0; }
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

            if (_tickEstado >= 240) { _estado = EST_FLOTAR; _tickEstado = 0; }
        }

        // ==================================================================
        //  EL DESTELLO — LA EMBESTIDA A VELOCIDAD LUZ
        // ==================================================================

        /// <summary>Prepara un destello: rumbo al CORTE DE LA HUIDA.</summary>
        private void PrepararDestello(Player target)
        {
            // v6.50.44 — EL CORTE DE LA HUIDA: el rumbo ya no apunta A tu
            // predicción sino DONDE NO PUEDES estar cuando llegue — alterna
            // el flanco derecho e izquierdo de tu carrera (el ziguezagueo
            // perezoso ya no basta: hay que CAMBIAR el rumbo, no solo la
            // velocidad).
            Vector2 pred = PredPresa(target);
            Vector2 rumboVec = pred - NPC.Center;
            Vector2 perp = new Vector2(-rumboVec.Y, rumboVec.X);
            if (perp.LengthSquared() > 0.001f) perp.Normalize();
            float lado = (_cadenasDestello % 2 == 0) ? 1f : -1f;
            Vector2 corte = pred + perp * (150f * lado);
            _angDestello = (corte - NPC.Center).ToRotation();
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

            // === EL CRUCE: a velocidad luz por la línea ===
            float vel = 44f + Phase * 2.5f + (Furia ? 4f : 0f);
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

            // el cruce terminó: ¿OTRO destello o el descanso?
            bool cruzo = Vector2.Distance(NPC.Center, target.Center) > 1900f;
            if (_tickEstado > 90 || cruzo)
            {
                int maxCadena = Furia ? 3 : (Phase >= 3 ? 2 : 1);
                if (_cadenasDestello < maxCadena)
                {
                    _cadenasDestello++;
                    _tickEstado = 0;
                    PrepararDestello(target);   // rumbo NUEVO a la presa
                    return;
                }
                _estado = EST_FLOTAR;
                _tickEstado = 0;
            }
        }

        // ==================================================================
        //  EL ECLIPSE — LA LUZ SE APAGA (fase 4+)
        // ==================================================================

        /// <summary>
        /// EL ATAQUE FIRMA: la luz SE APAGA (ai[1]=3: el núcleo se
        /// oscurece en el render de cada máquina, la luz del mundo muere
        /// a un violeta tenue) y el campo se LLENA de orbes lentos que
        /// convergen sobre la presa — SOLO LAS BALAS BRILLAN. La
        /// atracción del núcleo muerto tira de ti hacia el cuerpo (la
        /// gravedad del agujero heredada). Al final: EL REGRESO — la
        /// luz VUELVE y estalla una nova GRATIS (el flash del alba).
        /// </summary>
        private void EstadoEclipse(Player target)
        {
            int duracion = Furia ? 200 : 260;
            _rayo = 3; // el contrato del eclipse (viaja en ai[1])

            if (_tickEstado == 1)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item88, NPC.Center); // el apagón
                EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Eclipse", VioletaLuz);
                OndaLib.Kick(9f, 18);
            }

            // LA DERIVA LENTA: el cuerpo muerto se cierne.
            Vector2 punto = target.Center + new Vector2(0f, -340f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, (punto - NPC.Center) * 0.02f, 0.06f);

            // LA ATRACCIÓN del cuerpo apagado (la herencia del agujero).
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player pl = Main.player[i];
                if (pl == null || !pl.active || pl.dead) continue;
                Vector2 aH = NPC.Center - pl.Center;
                float d = aH.Length();
                if (d > 900f || d < 60f) continue;
                float fuerza = 0.16f * (1f - d / 900f);
                pl.velocity += aH.SafeNormalize(Vector2.Zero) * fuerza;
            }

            // LAS BALAS (v6.50.44 — LAS BALAS QUE TE BUSCAN): orbes
            // GUIADOS desde los bordes, convergiendo (ai[1]=1: el perno
            // estelar curva SUAVE hacia ti y ACELERA al acercarse — la
            // oscuridad no dispara a un punto, TE PERSIGUE).
            if ((_tickEstado % 12) == 0 && _tickEstado < duracion - 40)
            {
                for (int o = 0; o < 2; o++)
                {
                    float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 borde = target.Center + new Vector2(
                        MathF.Cos(ang), MathF.Sin(ang) * 0.7f) * 820f;
                    Vector2 v = (target.Center - borde).SafeNormalize(Vector2.UnitY) * 3.4f;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromAI(),
                            borde, v,
                            ModContent.ProjectileType<AtaqueJefeProjectile>(),
                            (int)(NPC.damage * 0.50f), 2f, Main.myPlayer,
                            AtaqueJefeProjectile.EstiloPernoEstelar, 1f,
                            NPC.whoAmI * 89 + _tickEstado + o * 13);
                    }
                }
            }

            // EL REGRESO: la luz VUELVE — y trae una nova.
            if (_tickEstado >= duracion)
            {
                _rayo = 0;
                _tickEstado = 45;        // el estallido YA (EstadoNova salta al clímax)
                _estado = EST_NOVA;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74, NPC.Center);
                NPC.netUpdate = true;
            }
        }

        // ==================================================================
        //  v6.50.45 — LAS ARMAS DEL MOD EN MANOS DE LA LUZ
        //  (la petición: el jefe usa LOS PROYECTILES DE LOS BASTONES)
        // ==================================================================

        /// <summary>
        /// EL RELOJ DE ARENA CÓSMICO GIGANTE (la petición: «el jefe deberia
        /// usar el proyectil de baston del reloj de arena cosmica pero en
        /// gigante»). La luz se alza, gira el cielo sobre tu RITMO y DEJA
        /// CAER el reloj de arena GIGANTE (×2.6 el del bastón): la arena
        /// estelar cae por el cuello y su PESO aplasta — daño constante y
        /// empuje hacia ABAJO a quien viva debajo (el castigo del
        /// volador) — y cada vez que la cámara superior se vacía el reloj
        /// SE INVIERTE y el pulso HUNDE con más fuerza.
        /// </summary>
        private void EstadoReloj(Player target)
        {
            // SE ALZA (el reloj necesita cielo) y convoca sobre la PREDICCIÓN.
            Vector2 punto = target.Center + new Vector2(0f, -560f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, (punto - NPC.Center) * 0.06f, 0.12f);

            if (_tickEstado == 30)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromAI(),
                        PredPresa(target), Vector2.Zero,
                        ModContent.ProjectileType<AtaqueJefeProjectile>(),
                        (int)(NPC.damage * 0.50f), 0f, Main.myPlayer,
                        AtaqueJefeProjectile.EstiloRelojGigante,
                        Furia ? 1f : 0f, NPC.whoAmI * 31);
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item45.WithPitchOffset(-0.4f), NPC.Center);
                EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Reloj", OroLuz);
                OndaLib.Kick(6f, 14);
                NPC.netUpdate = true;
            }

            if (_tickEstado >= 150) { _estado = EST_FLOTAR; _tickEstado = 0; }
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

            if (_tickEstado >= 140) { _estado = EST_FLOTAR; _tickEstado = 0; }
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

            if (_tickEstado >= 140) { _estado = EST_FLOTAR; _tickEstado = 0; }
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
        /// EL TELAR DE CONSTELACIONES — LA FASE ALTA (la petición: «el jefe
        /// cuando llegue a una fase alta, deberia moverse rapido alrededor
        /// del jugador en circulo y usar el proyectil de El telar de
        /// Constelaciones para atrapar al jugador y provocar daño»). La
        /// luz CORRE en círculo alrededor de la presa — un cometa dorado
        /// trazando la órbita — mientras EL TELAR clava las ESTRELLAS una
        /// a una alrededor del jugador: cuando la figura se CIERRA, el
        /// polígono se ENCIENDE y quien quedó DENTRO paga el corte. Corre,
        /// luz, corre — y teje.
        /// </summary>
        private void EstadoTelar(Player target)
        {
            if (_tickEstado == 1)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromAI(),
                        target.Center, Vector2.Zero,
                        ModContent.ProjectileType<AtaqueJefeProjectile>(),
                        (int)(NPC.damage * 0.55f), 0f, Main.myPlayer,
                        AtaqueJefeProjectile.EstiloTelarJefe,
                        Furia ? 1f : 0f, NPC.whoAmI * 41);
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, NPC.Center);
                EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Telar", OroLuz);
                OndaLib.Kick(8f, 16);
                NPC.netUpdate = true;
            }

            // EL CÍRCULO VELOZ: la órbita apretada y RÁPIDA (una vuelta
            // cada ~2 s — el jefe barre la circunferencia mientras las
            // estrellas cierran la figura por dentro).
            float velAng = (Furia ? 0.062f : 0.052f) * _sentidoOrbita;
            _angOrbita += velAng;
            Vector2 punto = target.Center + new Vector2(
                MathF.Cos(_angOrbita) * 470f,
                MathF.Sin(_angOrbita) * 470f * 0.80f - 150f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, (punto - NPC.Center) * 0.24f, 0.38f);

            // LA ESTELA DEL CORREDOR (el cometa que traza la órbita).
            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(
                    NPC.Center + new Vector2(Main.rand.NextFloat(-60f, 60f),
                        Main.rand.NextFloat(-60f, 60f)),
                    DustID.GoldFlame,
                    -NPC.velocity * 0.06f, 170, OroLuz, 1.1f);
                d.noGravity = true;
            }

            if (_tickEstado >= 330) { _estado = EST_FLOTAR; _tickEstado = 0; }
        }

        /// <summary>
        /// EL DECRETO DEL ECLIPSE — EL CAMBIO DE FASE (la petición: «el jefe
        /// debe usar el proyectil el decreto del eclipse en el cambio de
        /// fases y por cada fase que sea mas grande, al usar este
        /// proyectil el jefe queda inmovil asi le da tiempo al jugador a
        /// escapar»). La luz se CLAVA donde está — INMÓVIL, canalizando —
        /// y decreta: EL CÍRCULO DEL ECLIPSE nace pequeño sobre el jugador
        /// y CRECE +2 px/t, MÁS GRANDE con cada fase (P2 660 → P5 930 px
        /// de radio), con la MARCA DEL OJO y las EJECUCIONES del bastón
        /// cada 15 t. El jefe queda INMÓVIL TODO el decreto — ESO es la
        /// ventana de escape: el círculo tarda en cerrarse y la luz no se
        /// mueve; el que corre, sale.
        /// </summary>
        private void EstadoDecreto(Player target)
        {
            // INMÓVIL (la sentencia no se dicta corriendo).
            NPC.velocity = Vector2.Zero;

            if (_tickEstado == 24)
            {
                // EL CÍRCULO: nace sobre el JUGADOR — ai[1] = la FASE (el
                // tamaño del decreto crece con la ira).
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromAI(),
                        target.Center, Vector2.Zero,
                        ModContent.ProjectileType<AtaqueJefeProjectile>(),
                        (int)(NPC.damage * 0.80f), 0f, Main.myPlayer,
                        AtaqueJefeProjectile.EstiloDecretoJefe,
                        Phase, NPC.whoAmI * 43);
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item88, NPC.Center);
                EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Decreto", VioletaLuz);
                NPC.netUpdate = true;
            }

            // EL JEFE ESPERA TODO el decreto (la vida del círculo: nace a
            // 80 px y crece +2 px/t hasta su radio máximo) + un respiro.
            float radioMax = 660f + (Phase - 2) * 90f;
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

        private void FlipGravity(Player player)
        {
            player.AddBuff(BuffID.Gravitation, 180);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, player.Center);
            OndaLib.Kick(6f, 12);
            EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Gravedad", VioletaLuz);
        }

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

        /// <summary>EL CAMBIO DE FASE (v6.50.45: con EL DECRETO — el jefe se clava y decreta el círculo del eclipse; el jugador gana LA ventana de escape).</summary>
        private void OnPhaseChange()
        {
            NPC.life = Math.Min(NPC.lifeMax, NPC.life + NPC.lifeMax / 20);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
            OndaLib.Kick(10f, 20);
            EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Fase",
                OroLuz, Phase, PhaseName());
            NPC.netUpdate = true;

            // v6.50.45 — EL DECRETO DEL ECLIPSE EN CADA CAMBIO DE FASE: el
            // jefe se CLAVA (inmóvil) y el círculo nace sobre el jugador,
            // MÁS GRANDE con cada fase — la ventana de escape es LITERAL
            // (el jefe no se mueve mientras el círculo crece). Solo si no
            // está YA en pleno decreto (los cambios de fase encadenados por
            // DPS no re-decretan: un decreto a la vez).
            if (Phase >= 2 && _estado != EST_DECRETO)
            {
                _estado = EST_DECRETO;
                _tickEstado = 0;
                _rayo = 0;
                NPC.netUpdate = true;
            }
        }

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
            // LA CONTRACCIÓN: todo el brillo se recoge (el sol se hace punto).
            NPC.velocity = Vector2.Lerp(NPC.velocity, Vector2.Zero, 0.10f);

            if (_tickMuerte >= 120)
            {
                OndaLib.Kick(14f, 30);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74, NPC.Center);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath55, NPC.Center);
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
                bool eclipse = NPC.ai[1] == 3f || (_muriendo && _tickMuerte < 120);
                float faseInt = 0.55f + 0.45f * (Phase - 1) / 4f;   // la furia brilla más

                // === LA MUERTE: la contracción (todo hacia el punto) ===
                float colapso = 1f;
                if (_muriendo) colapso = Math.Max(0.05f, 1f - _tickMuerte / 110f);

                // === EL ECLIPSE: el disco oscuro (el cuerpo muerto de la luz) ===
                if (eclipse)
                {
                    spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                        SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                        null, Main.GameViewMatrix.TransformationMatrix);
                    try
                    {
                        float pulsoE = 0.90f + 0.10f * MathF.Sin(t * 0.8f);
                        spriteBatch.Draw(VFXCore.SoftGlow, posC, null,
                            new Color(16, 8, 30) * (0.96f * visibilidad),
                            0f, new Vector2(VFXCore.SoftGlow.Width, VFXCore.SoftGlow.Height) * 0.5f,
                            new Vector2(3.2f * pulsoE, 3.2f * pulsoE), SpriteEffects.None, 0f);
                    }
                    finally { spriteBatch.End(); }
                }

                // === EL SOL (lote aditivo — TODO lo que brilla) ===
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                try
                {
                    float latido = 0.84f + 0.16f * MathF.Sin(t * 1.6f);
                    float brillo = eclipse ? 0.10f : (faseInt * colapso);

                    // === 1. EL VELO (el aura violeta de la profundidad) ===
                    LumenLib.Bloom(spriteBatch, posC, 540f, VioletaLuz,
                        0.10f * brillo * visibilidad, 2);

                    // === 2. EL HALO DORADO (la corona del sol) ===
                    LumenLib.BloomPulse(spriteBatch, posC, 295f * latido, OroLuz,
                        0.50f * brillo * visibilidad, t, 1.6f);

                    // === 3. LOS RAYOS RADIALES (la rueda de luz girando) ===
                    if (!eclipse)
                    {
                        int nRayos = 8 + Phase * 2;                       // 10 en P1 → 18 en P5
                        float giro = t * 0.10f;
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
                                (eclipse ? 0.06f : 0.42f) * (0.5f + 0.5f * tw) *
                                brillo * visibilidad, 2);
                        }
                    }

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
                            (eclipse ? 0.05f : 0.40f) * brillo * visibilidad, 2);
                    }

                    // === 6. EL NÚCLEO (el corazón blanco de Aethon) ===
                    float tamNucleo = 130f * latido * colapso;
                    if (eclipse)
                    {
                        // EL ECLIPSE: solo el RIM dorado del cuerpo muerto.
                        LumenLib.Bloom(spriteBatch, posC, 108f, OroLuz,
                            0.10f * visibilidad, 2);
                        LumenLib.Bloom(spriteBatch, posC, 44f, new Color(120, 80, 190),
                            0.12f * visibilidad, 2);
                    }
                    else
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
                    DibujarTelegrafos(spriteBatch, NPC, posC);

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
                    if (_muriendo && _tickMuerte >= 110 && _tickMuerte <= 135)
                    {
                        float fp = (_tickMuerte - 110) / 25f;   // 0→1: la inundación
                        LumenLib.Bloom(spriteBatch, posC,
                            850f + 3300f * fp, NucleoBlanco,
                            0.9f * (1f - fp * 0.6f), 3);
                        LumenLib.Bloom(spriteBatch, posC,
                            560f + 2500f * fp, OroLuz,
                            0.6f * (1f - fp * 0.5f), 2);
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
        internal static void DibujarTelegrafos(SpriteBatch sb, NPC npc, Vector2 posC)
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
        }
    }
}
