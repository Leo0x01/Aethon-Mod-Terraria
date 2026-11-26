using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.GameContent.UI.BigProgressBar;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Globals
{
    /// <summary>
    /// JefeInteriorGlobalNPC — v6.137 — LA ARENA DEL CUERPO DEL DIOS.
    ///
    /// LA LETRA: «el pilar de prisma tambien permitira invocar a todos
    /// los jefes que el jugador haya vencido y devorado, cada jefe
    /// devorado se podra invocar en una version aun mas poderosa dentro
    /// del mundo interior, estos tendran un aura a su alrededor como
    /// los de la oleada de hambre, seran potenciado por cada muerte que
    /// el jugador haga dentro del mundo interior, si el jugador mata al
    /// rey slime 4 veces su poder crece, por cada muerte el poder de un
    /// jefe crece 25%, crece su vida, su defensa, aumenta el daño de
    /// sus ataques, y su IA mejora en los primeros 5 niveles es la IA
    /// normal, a partir del nivel 6 es la IA del modo experto, cuando
    /// llegue a nivel 8 la IA se vuelve mas agresivas con nuevos
    /// ataques relacionados al jefe, para esto puedes usar los mismos
    /// recuros de los que ya dispone terraria, ya sabes proyectiles y
    /// eso, para el nivel 10 la IA es agresiva con nuevos movimientos
    /// de ataques y combos, esto sera igual para cada jefe y minijefe
    /// original de terraria» + «ahora para jefes que no pertenezcan a
    /// terraria, solo tendran el incremento de poder y la IA del modo
    /// experto correspondiente para dicho jefe o minijefe».
    ///
    /// EL DISEÑO DE LA CASA (todo prestado de máquinas blindadas):
    ///  · EL NIVEL DE PODER: 1 + EL NÚMERO DE VECES que el jugador mató
    ///    a ESTE jefe DENTRO del mundo interior (ShardPlayer.
    ///    KillsInterior — la arena es la que entrena; los kills de
    ///    afuera no cuentan). Persiste con el jugador.
    ///  · +25% POR MUERTE: vida · daño ×(1 + 0,25·k) y defensa
    ///    +2,5·k, SIEMPRE desde las bases capturadas (la idempotencia
    ///    aprendida del festín v6.50.10).
    ///  · EL AURA DE LA OLEADA: AuraLib.OleadaGrimorio(3) — la familia
    ///    visual del festín («un aura a su alrededor como los de la
    ///    oleada de hambre»), con el lote cerrado/reabierto de la casa.
    ///  · LA IA POR NIVELES (SOLO jefes VANILLA — los del mod van
    ///    directos al suplemento experto, la letra):
    ///      1-5  · IA NORMAL (jefe vanilla tal cual, solo los stats).
    ///      6-7  · IA EXPERTA: EL PRÉSTAMO DE MODO de OleadaNPC (la
    ///             reflexión _overrideForExpertMode/_overrideForMasterMode
    ///             de la v6.50.73 — el mecanismo blindado del festín):
    ///             la IA de vanilla corre vestida de MODO MAESTRO (que
    ///             en vanilla INCLUYE los patrones expertos) + el
    ///             suplemento de stats experto.
    ///      8-9  · AGRESIVA: lo anterior + LOS ATAQUES TEMÁTICOS de la
    ///             casa (OleadaNPC.CoreografiaPrestada — la coreografía
    ///             v6.50.48: cada jefe convoca a LOS SUYOS) cada 60 t.
    ///      10   · COMBOS: lo anterior + cadencia 36 t (la de El
    ///             Juicio — sin pausa).
    ///  · LA DERROTA SUMA: OnKill → el kill interior del jugador sube
    ///    y la próxima versión nace +25% (anunciado al mundo).
    /// </summary>
    public class JefeInteriorGlobalNPC : GlobalNPC
    {
        // v6.152 — LA NOCHE PRESTADA UNIVERSAL (ver el PreAI de abajo):
        // mientras el interior vive, TODO SkeletronHead recibe la noche
        // prestada en el PreAI del festín (el clamp del mediodía NUNCA
        // deja que su IA vea el día — el guardián de mazmorra es
        // IMPOSIBLE dentro, llegue como llegue la cabeza).
        private bool _prestaNocheInterior;
        public override bool InstancePerEntity => true;

        /// <summary>¿Es el jefe interior de la arena? (lo marca el pilar
        /// al invocarlo: Sellar()).</summary>
        public bool EsInterior;

        /// <summary>El tipo ORIGINAL del jefe (para piezas que mutan,
        /// como la cabeza del Devorador).</summary>
        public int TipoOriginal;

        /// <summary>EL NIVEL DE IA (1..10): 1 + los kills interiores
        /// del jugador contra ESTE jefe — clavado al sellarse.</summary>
        public int NivelIA;

        // === EL PRÉSTAMO DEL MODO (los campos de la máquina de
        //     OleadaNPC, replicados aquí — el mismo contrato) ===
        private static System.Reflection.FieldInfo _campoExperto = null;
        private static System.Reflection.FieldInfo _campoMaestro = null;
        private static bool _reflexionLista = false;
        private bool _prestaModo = false;
        private bool? _expertoOriginal = null;
        private bool? _maestroOriginal = null;

        // === EL RESTO DEL ESTADO ===
        private int _vidaBase = -1;
        private int _danoBase = -1;
        private int _defBase = -1;
        private int _tickAtaque = 0;
        private int _tickRefrescoStats = 0;

        // ================================================================
        //  v6.175 — EL MOTOR DE COMBATE DE LA ARENA (la letra: jefes
        //  interiores con «nuevos ataques y combos de movimientos, con
        //  dash, nuevos proyectiles basados en el tipo de jefe,
        //  invocaciones de minions, mas vida y una mejor IA de combate
        //  e incluso con nuevos ataques mejorados con muchos efectos
        //  visuales»). TODO corre del lado de la AUTORIDAD (SP/servidor)
        //  y viaja por la red de siempre (netUpdate + proyectiles
        //  netImportant).
        // ================================================================
        private int _dashCd;              // enfriamiento del dash
        private int _dashTele;            // ticks de TELEGRAFO antes del dash
        private int _dashVentana;         // ticks de dash ACTIVO
        private Vector2 _dashRumbo;       // hacia dónde embiste
        private int _voleaCd;             // enfriamiento de la volea
        private int _comboCd;             // enfriamiento entre combos
        private int _comboPaso;           // 0 suelto · 1 dash · 2 anillo · 3 andanada
        private int _comboT;              // reloj interno del paso
        private bool _motorExterno;       // lo usan también los jefes de OLEADA

        // === v6.177 — LA ESCALERA SIN TECHO (la letra: «que este nivel
        //     sea infitino y cuanto mas nivels mas complicado, mas
        //     inteligente y tiene nuevos ataques, combos y nuevos
        //     movimientos, asi como una IA mas avanzada») ===
        private bool _dashDoble;          // nivel 8+: la segunda embestida
        private int _volea2Cd;            // nivel 10+: la volea doble
        private int _saltoCd;             // nivel 12+: el salto de sombra
        private int _saltoTele;           // el telégrafo del salto
        private int _saltoHacia;          // el jugador del salto

        /// <summary>¿El telegraph del dash está VIVO? (lo dibuja PostDraw).</summary>
        public bool TelegraphActivo => _dashTele > 0;

        /// <summary>¿El dash está CORRIENDO? (lo dibuja PostDraw).</summary>
        public bool DashActivo => _dashVentana > 0;

        /// <summary>El RUMBO vivo del dash (para dibujar la estela).</summary>
        public Vector2 RumboDash => _dashRumbo;

        /// <summary>EL AURA VIVA (la dibujan PreDraw/PostDraw — el
        /// contrato de lotes de la casa).</summary>
        public AuraPerfil Aura;

        /// <summary>v6.176 — EL SEGMENTO DE GUSANO: pieza sellada por
        /// adopción (la letra: «la barra de vida se desborda» al invocar
        /// al Devorador): el gusano se sella ENTERO — cada cuerpo/cola
        /// que la IA engendra hereda el sello de SU cabeza. El segmento
        /// lleva los stats del arena y SU barra, pero NO aura, NO motor
        /// de combate, NO regen y NO cuenta kills: el jefe es UNO, no
        /// sesenta y siete.</summary>
        public bool EsSegmento;

        /// <summary>EL SELLO: el pilar acaba de invocar a ESTE jefe —
        /// lo vestimos de arena (aura + stats + nivel de IA).</summary>
        public static void Sellar(NPC n, int nivelIA)
        {
            if (n == null || !n.active) return;
            if (!n.TryGetGlobalNPC<JefeInteriorGlobalNPC>(out var g)) return;
            g.EsInterior = true;
            g.TipoOriginal = n.type;
            // v6.140 — EL NIVEL SIN TOPE (la letra: «que ademas no
            // tengan limites de nivel»): el poder de la arena crece por
            // SIEMPRE — cada muerte dentro del interior hace a la
            // siguiente versión más poderosa, sin techo.
            g.NivelIA = Math.Max(1, nivelIA);
            // v6.175 — EL AURA DE LA OLEADA (la letra: los jefes del
            // interior «con su aura rodeándolos, como los de la oleada
            // de hambre» — la v6.137 la prometió y el sello NUNCA la
            // ponía: solo la llevaban los de la oleada):
            // AuraPerfil.OleadaGrimorio, intensidad = nivel del sello.
            g.Aura = AuraPerfil.OleadaGrimorio(Math.Max(1, Math.Min(10, nivelIA)));
            // v6.175 — LA BARRA PROPIA (la letra: «al aumentar la vida
            // su barra de salud se desborda y como que el numero de la
            // cantidad de vida total que tienen no cambia»): LA CAUSA —
            // la barra de vanilla congela el lifeMax al PRIMER dibujo
            // (el cacheo de BigProgressBarSystem); cuando AplicarStats
            // eleva la vida a mitad de pelea, la barra REBOSA y el
            // número total queda viejo. LA CURA: una ModBossBar propia
            // para TODO jefe interior que pinta VIDA y lifeMax VIVOS.
            n.BossBar = ModContent.GetInstance<BarraJefeInterior>();
            g.AplicarStats(n);
            n.netUpdate = true;
        }

        // ==================================================================
        //  v6.175 — LA BARRA DEL JEFE INTERIOR (la letra: «al aumentar
        //  la vida su barra de salud se desborda y como que el numero de
        //  la cantidad de vida total que tienen no cambia, debes arreglar
        //  eso»). ModifyInfo pisa la vida con los valores REALES de HOY:
        //  la barra escala con el lifeMax nuevo (nada rebosa) y el número
        //  de la derecha — el total — SIEMPRE es el vivo. Solo dibuja
        //  jefes con el sello interior (el resto de jefes usan la de
        //  vanilla, intacta).
        //
        //  v6.177 — LA BARRA QUE NUNCA LLEGÓ (la letra: «las barras de
        //  vida de los jefes dentro del mundo interior siempre empieza
        //  con 1/0 y siempre aparece justo asi, eso esta mal, deberia el
        //  numero reflejar el aumento de vida sin que la barra se
        //  desborde»). LA AUTOPSIA (contra el decompile del tML real):
        //  ModifyInfo devuelve TRUE/FALSE/null — y al devolver NULL la
        //  base PISA nuestros life/lifeMax con los de LA PIEZA seguida
        //  («life = Clamp(nPC.life,…); lifeMax = nPC.lifeMax») — la suma
        //  del gusano entero NUNCA llegaba a la pantalla. Y PEOR: los dos
        //  últimos parámetros NO son «scale» y «dps» — son SHIELD y
        //  SHIELD_MAX: nuestra «escala de nivel» ponía un ESCUDO fantasma
        //  (1.015 con tope 0). LA CURA: devolver TRUE — la base YA NO
        //  pisa nada — y jamás tocar el escudo.
        // ==================================================================
        public class BarraJefeInterior : ModBossBar
        {
            public override bool? ModifyInfo(ref BigProgressBarInfo info,
                ref float life, ref float lifeMax, ref float shield, ref float shieldMax)
            {
                if (info.npcIndexToAimAt < 0 || info.npcIndexToAimAt >= Main.maxNPCs)
                    return false;
                NPC n = Main.npc[info.npcIndexToAimAt];
                if (n == null || !n.active) return false;
                if (!n.TryGetGlobalNPC<JefeInteriorGlobalNPC>(out var g) || !g.EsInterior)
                    return false;   // sin sello: la barra de vanilla
                // LA VIDA VIVA (el cacheo de vanilla muere aquí)
                // v6.176 — EL GUSANO ENTERO (la letra: «la barra de vida
                // se desborda»): para los jefes SECCIONADOS (EoW y el
                // Destructor) la barra es del CUERPO ENTERO — la suma de
                // la vida de TODAS sus piezas vivas (como la barra de
                // vanilla del EoW), con el número total VIVO de verdad.
                if (EsGusanoSeccionadoONo(n.type))
                {
                    int tipoC = n.type == NPCID.TheDestroyer || n.type == NPCID.TheDestroyerBody
                        || n.type == NPCID.TheDestroyerTail
                        ? NPCID.TheDestroyerBody : NPCID.EaterofWorldsBody;
                    int tipoT = tipoC == NPCID.TheDestroyerBody
                        ? NPCID.TheDestroyerTail : NPCID.EaterofWorldsTail;
                    int tipoH = tipoC == NPCID.TheDestroyerBody
                        ? NPCID.TheDestroyer : NPCID.EaterofWorldsHead;
                    double suma = 0.0, sumaMax = 0.0;
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC s = Main.npc[i];
                        if (s == null || !s.active) continue;
                        if (s.type != tipoH && s.type != tipoC && s.type != tipoT) continue;
                        // v6.177 — SOLO LAS PIEZAS DE ESTA ARENA: otro
                        // gusano sin sello (una oleada, el mundo real)
                        // NO se suma a la barra del sellado
                        if (!s.TryGetGlobalNPC<JefeInteriorGlobalNPC>(out var gs) || !gs.EsInterior)
                            continue;
                        suma += s.life;
                        sumaMax += s.lifeMax;
                    }
                    life = (float)suma;
                    lifeMax = (float)Math.Max(1.0, sumaMax);
                }
                else
                {
                    life = n.life;
                    lifeMax = n.lifeMax;
                }
                // v6.176 — JAMÁS REBOSA: la fracción jamás pasa de 1 (el
                // dibujo del rebose es matemáticamente imposible)
                if (lifeMax <= 0f) lifeMax = 1f;
                if (life > lifeMax) life = lifeMax;
                if (life < 0f) life = 0f;
                // v6.177 — TRUE (la letra: «deberia el numero reflejar
                // el aumento de vida»): al devolver NULL la base PISABA
                // nuestra suma con la vida de UNA pieza — ahora la vida
                // y el total que venimos calculando SON los que se dibujan.
                return true;
            }
        }

        /// <summary>LA FÓRMULA DEL PODER: ×(1 + 0,25·k) — k = kills
        /// interiores del jugador contra este jefe.</summary>
        public static float Multiplicador(int kills) => 1f + 0.25f * Math.Max(0, kills);

        /// <summary>¿Es un jefe o minijefe de TERRARIA (no del mod)?
        /// (los del mod no llevan IA por niveles — solo poder+experto,
        /// la letra).</summary>
        public static bool EsVanilla(int tipo) => tipo > 0 && tipo < NPCID.Count;

        /// <summary>LOS STATS DEL ARENA: vida · daño ×(1+0,25k) y
        /// defensa +2,5k — SIEMPRE desde las bases (idempotente).</summary>
        private void AplicarStats(NPC n)
        {
            if (_vidaBase < 0)
            {
                _vidaBase = n.lifeMax;
                _danoBase = n.damage;
                _defBase = n.defense;
            }
            float mult = Multiplicador(NivelIA - 1);
            // v6.175 — LA BASE DEL INTERIOR (la letra: los jefes interiores
            // con MÁS VIDA — «recuerda que en el mundo interior los jefes
            // deben hacerse mas fuerte… mas vida»): la vida nace ya
            // ×1.5 sobre la base capturada; el daño y la defensa siguen
            // la fórmula de siempre (la arena es un MURO, no un cañón).
            int vidaMaxNueva = Math.Max(1, (int)(_vidaBase * mult * 1.5f));
            // v6.140 — LA VIDA YA NO SE RESTAURA (la letra: «en el
            // mundo interior con los jefes… estos se regeneran
            // constantemente eso no debe pasar»): el refresco periódico
            // de stats clavaba life = lifeMax CADA 30 TICKS — el jefe
            // era inmortal. AHORA: el lifeMax puede CRECER (un nivel
            // nuevo), pero la vida ACTUAL solo se AJUSTA
            // proporcionalmente si el máximo subió a mitad de pelea
            // (nunca se cura: el daño hecho queda hecho).
            if (vidaMaxNueva > n.lifeMax)
            {
                float fraccion = n.lifeMax > 0 ? (float)n.life / n.lifeMax : 1f;
                n.life = Math.Max(1, (int)(vidaMaxNueva * fraccion));
            }
            n.lifeMax = vidaMaxNueva;
            if (_danoBase > 0) n.damage = (int)(_danoBase * mult);
            n.defense = _defBase + (int)(Math.Max(0, NivelIA - 1) * 2.5f);
            // LA TALLA EN LOS DEFAULTS DE LA IA (la lección v6.50.73:
            // las IA vanilla re-visten defDamage/defDefense cada tick).
            n.defDamage = _danoBase > 0 ? (int)(_danoBase * mult) : n.damage;
            n.defDefense = n.defense;
        }

        /// <summary>v6.176 — ¿Es una pieza (cuerpo/cola) del EoW o del
        /// Destructor? (la cabeza NO: ella nace sellada por el pilar).</summary>
        private static bool EsPiezaDeGusano(int tipo) => tipo == NPCID.EaterofWorldsBody
            || tipo == NPCID.EaterofWorldsTail
            || tipo == NPCID.TheDestroyerBody || tipo == NPCID.TheDestroyerTail;

        /// <summary>v6.176 — ¿El gusano seccionado? (cualquiera de sus
        /// seis piezas — para la barra del CUERPO entero).</summary>
        private static bool EsGusanoSeccionadoONo(int tipo) => EsPiezaDeGusano(tipo)
            || tipo == NPCID.EaterofWorldsHead || tipo == NPCID.TheDestroyer;

        public override void SetDefaults(NPC npc)
        {
            // v6.176 — LA ADOPCIÓN DE LOS SEGMENTOS (la letra: «algo
            // falló en el mundo interior al invocar al Devoramundos»):
            // el gusano se sella ENTERO — cada cuerpo/cola que la IA de
            // vanilla engendra sobre la marcha hereda el sello de SU
            // cabeza (stats del arena + la barra propia). Como
            // SEGMENTO: sin aura, sin motor, sin regen, sin kills.
            try
            {
                if (!EsInterior && EsPiezaDeGusano(npc.type))
                {
                    int tipoH = npc.type == NPCID.TheDestroyerBody || npc.type == NPCID.TheDestroyerTail
                        ? NPCID.TheDestroyer : NPCID.EaterofWorldsHead;
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC h = Main.npc[i];
                        if (h == null || !h.active || h.type != tipoH) continue;
                        if (!h.TryGetGlobalNPC<JefeInteriorGlobalNPC>(out var gh) || !gh.EsInterior)
                            continue;
                        EsInterior = true;
                        EsSegmento = true;
                        TipoOriginal = npc.type;
                        NivelIA = gh.NivelIA;
                        AplicarStats(npc);
                        npc.BossBar = ModContent.GetInstance<BarraJefeInterior>();
                        break;
                    }
                }
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("JefeInteriorGlobalNPC", exRegistro); }
            // LA RE-ADOPCIÓN (la lección del Devorador: si vanilla MUTA
            // una pieza, SetDefaults destruye los globals frescos — el
            // sello se re-aplica en el mismo tick).
            try
            {
                if (EsInterior && npc.active && _vidaBase > 0)
                {
                    AplicarStats(npc);
                    // v6.175 — LA BARRA PROPIA sobrevive a la mutación
                    // (si vanilla re-encarna la pieza, el sello re-viste)
                    npc.BossBar = ModContent.GetInstance<BarraJefeInterior>();
                }
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("JefeInteriorGlobalNPC", exRegistro); }
        }

        /// <summary>LA REFLEXIÓN DEL MODO (la MISMA de OleadaNPC
        /// v6.50.73 — copiada palabra por palabra: si fallara, el
        /// préstamo no ocurre y todo lo demás sigue).</summary>
        private static void PrepararReflexion()
        {
            if (_reflexionLista) return;
            _reflexionLista = true;
            try
            {
                _campoExperto = typeof(Main).GetField("_overrideForExpertMode",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                _campoMaestro = typeof(Main).GetField("_overrideForMasterMode",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            }
            catch { _campoExperto = null; _campoMaestro = null; }
        }

        public override bool PreAI(NPC npc)
        {
            // === v6.152 — LA NOCHE PRESTADA UNIVERSAL (la letra: «al
            //     invocar a esqueletro me sale la version de la
            //     mazmorra… eso no debe pasar»): el fix v6.141 (la
            //     ai[1] devuelta a 1) solo vestía a los jefes de LA
            //     ARENA (EsInterior) y reñía CADA TICK con la IA
            //     vanilla (que veía día y la volvía a 2 — el tira y
            //     afloja que el usuario vio como cabeza girando). LA
            //     CURA DE RAÍZ: mientras el MUNDO INTERIOR viva y
            //     sea de día, TODO SkeletronHead recibe LA NOCHE
            //     PRESTADA antes de su IA (dayTime=false) y se la
            //     devolvemos en PostAI — la IA vanilla JAMÁS ve el
            //     día: no hay modo guardián que la agarre, ni en la
            //     arena, ni en la oleada, ni en la carnada.
            _prestaNocheInterior = false;
            if (npc.type == NPCID.SkeletronHead &&
                Systems.SubmundoGrimorio.Activo && Main.dayTime)
            {
                Main.dayTime = false;
                _prestaNocheInterior = true;
            }

            // === v6.141 — SKELETRON JAMÁS GUARDIÁN (la letra: «el
            //     esqueletron aparece siendo la version de la mazmorra
            //     con alta defensa, eso esta mal»): el interior vive en
            //     MEDIODÍA ETERNO (time 27000) y el AI_011 de vanilla
            //     (verificado en el decompile: `type == 68 ||
            //     Main.IsItDay()` → ai[1] = 2f) mete a la cabeza en el
            //     MODO SPIN DEL DÍA — la versión furiosa de defensa
            //     brutal. LA CURA: mientras viva en la arena, su ai[1]
            //     JAMÁS pasa de 1 (el modo noche de toda la vida). ===
            if (EsInterior && npc.type == NPCID.SkeletronHead && npc.ai[1] == 2f)
                npc.ai[1] = 1f;
            if (!EsInterior) return true;
            try
            {
                // === NIVEL 6+ (jefes vanilla): EL PRÉSTAMO DE MODO —
                // la IA de vanilla corre vestida de MODO MAESTRO (que
                // en vanilla incluye los patrones expertos — el
                // mecanismo blindado del festín, v6.50.73).
                if (NivelIA >= 6 && EsVanilla(npc.type) && !_prestaModo)
                {
                    PrepararReflexion();
                    if (_campoExperto != null && _campoMaestro != null)
                    {
                        _expertoOriginal = (bool?)_campoExperto.GetValue(null);
                        _maestroOriginal = (bool?)_campoMaestro.GetValue(null);
                        _campoExperto.SetValue(null, (bool?)true);
                        _campoMaestro.SetValue(null, (bool?)true);
                        _prestaModo = true;
                    }
                }
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("JefeInteriorGlobalNPC", exRegistro); }
            return true;
        }

        public override void PostAI(NPC npc)
        {
            // v6.152 — LA NOCHE DEVUELTA (el préstamo dura UN AI — el
            // contrato de la casa: el mediodía del decorado vuelve)
            if (_prestaNocheInterior)
            {
                try { Main.dayTime = true; } catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("JefeInteriorGlobalNPC", exRegistro); }
                _prestaNocheInterior = false;
            }
            if (!EsInterior) return;
            // v6.176 — EL SEGMENTO SOLO LLEVA EL CUERPO: los refrescos de
            // stats corren para todos (la vida del arena es del gusano
            // ENTERO) — pero la REGEN, la coreografía de minions y el
            // MOTOR de combate viven SOLO en la cabeza: 67 piezas
            // dashando y disparando voleas sería el caos (y la mitad de
            // la vida del jefe regenerándose por segundo, el apagón).
            if (EsSegmento)
            {
                try
                {
                    if (++_tickRefrescoStats >= 30)
                    {
                        _tickRefrescoStats = 0;
                        if (_vidaBase > 0) AplicarStats(npc);
                    }
                }
                catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("JefeInteriorGlobalNPC", exRegistro); }
                return;
            }
            try
            {
                // LA DEVOLUCIÓN DEL MODO (el préstamo dura exactamente
                // UN AI — el contrato de la casa).
                if (_prestaModo)
                {
                    _prestaModo = false;
                    try
                    {
                        if (_campoExperto != null) _campoExperto.SetValue(null, _expertoOriginal);
                        if (_campoMaestro != null) _campoMaestro.SetValue(null, _maestroOriginal);
                    }
                    catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("JefeInteriorGlobalNPC", exRegistro); }
                }

                // EL REFRESCO PERIÓDICO DE STATS (la red de seguridad de
                // las IA que se re-visten solas — cada 30 t, desde las
                // bases, idempotente).
                if (++_tickRefrescoStats >= 30)
                {
                    _tickRefrescoStats = 0;
                    if (_vidaBase > 0) AplicarStats(npc);
                }

                // === v6.140 — LA REGENERACIÓN DE LA ARENA (la letra:
                //     «la regeneracion me gusto, hagamos que a partir
                //     del nivel 10, estos puedan regenerarse a 1% de su
                //     vida maxima cada segundo y que con cada nivel esa
                //     regeneracion aumente en 0.1% de su vida maxima»):
                //     1% por segundo en el nivel 10, +0,1% por nivel
                //     adicional — SIN TOPE. Es regen LICITA (vía
                //     lifeRegen, la convención vanilla: 60 ticks =
                //     1 s — nunca toca life directo, jamás interrumpe
                //     el daño del jugador: solo suma lo que la vida
                //     recupera sola). ===
                if (NivelIA >= 10 && npc.life < npc.lifeMax && npc.life > 0)
                {
                    float porSegundo = 0.01f + 0.001f * (NivelIA - 10);
                    int regenTicks = Math.Max(1, (int)(npc.lifeMax * porSegundo / 60f));
                    if (npc.lifeRegen < regenTicks)
                        npc.lifeRegen = regenTicks;
                }

                // === v6.175 — LOS MINIONS DESDE EL NIVEL 4 (la letra:
                //     «invocaciones de minions» desde antes — no solo al
                //     8): la coreografía del festín (cada jefe convoca a
                //     LOS SUYOS) prestada de OleadaNPC; nivel 4-7: cada
                //     120 t; nivel 8+: la cadencia APRIETA de la v6.140.
                if (NivelIA >= 4 && EsVanilla(npc.type))
                {
                    _tickAtaque++;
                    int cadencia = NivelIA >= 10
                        ? Math.Max(18, 36 - (NivelIA - 10) / 5)
                        : NivelIA >= 8 ? 60 : 120;
                    if (_tickAtaque >= cadencia)
                    {
                        _tickAtaque = 0;
                        Player presa = PresaMasCercana(npc, 1400f);
                        if (presa != null)
                            OleadaNPC.CoreografiaPrestada(npc, presa, NivelIA);
                    }
                }

                // === v6.175 — EL MOTOR DE COMBATE DE LA ARENA: dash con
                //     telégrafo · voleas temáticas · combos por pasos —
                //     TODO con sus efectos visuales (el PostDraw los pinta)
                if (EsVanilla(npc.type) || _motorExterno)
                    MotorDeCombate(npc, NivelIA);
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("JefeInteriorGlobalNPC", exRegistro); }
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Aura == null) return true;
            try
            {
                if (AuraLib.DibujarNPC(npc, Aura, frontal: false))
                    AuraLib.ReabrirLoteVanilla();
            }
            catch { try { AuraLib.ReabrirLoteVanilla(); } catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("JefeInteriorGlobalNPC", exRegistro); } }
            return true;
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Aura == null && !TelegraphActivo && !DashActivo) return;
            try
            {
                if (Aura != null && AuraLib.DibujarNPC(npc, Aura, frontal: true))
                    AuraLib.ReabrirLoteVanilla();

                // ============================================================
                //  v6.175 — LA VISTA DEL MOTOR DE COMBATE: el telégrafo del
                //  dash (la línea de aviso jefe→presa que late) y la
                //  ESTELA de la embestida (rayos a la espalda). La casa
                //  dibuja el peligro ANTES de que muerda.
                // ============================================================
                if (TelegraphActivo || DashActivo)
                {
                    Player presa = PresaMasCercana(npc, 2400f);
                    if (presa != null)
                    {
                        Vector2 desde = npc.Center - Main.screenPosition;
                        Vector2 hasta = presa.Center - Main.screenPosition;
                        if (TelegraphActivo)
                        {
                            // LA LÍNEA DE AVISO: un rayo fino rojo-violeta que
                            // LATE (más rápido cuanto más cerca del mordisco)
                            float urgencia = 1f - _dashTele / 16f;
                            float alfa = 0.35f + 0.30f * MathF.Sin(
                                Main.GlobalTimeWrappedHourly * (6f + 10f * urgencia));
                            StormLib.Bolt(spriteBatch, desde, hasta,
                                71 + npc.whoAmI * 3, 3, 4.5f,
                                new Color(120, 30, 160), new Color(255, 70, 110),
                                Math.Clamp(alfa, 0.15f, 0.75f), 7, 34f);
                            // EL MARCA en la presa: el aro que avisa dónde cae
                            spriteBatch.Draw(VFXCore.SoftGlow, hasta, null,
                                new Color(255, 60, 100, 200) * (0.35f + 0.25f * urgencia), 0f,
                                VFXCore.SoftGlow.Size() * 0.5f,
                                new Vector2(0.9f, 0.9f), SpriteEffects.None, 0f);
                        }
                        else if (DashActivo)
                        {
                            // LA ESTELA: dos rayos a la espalda del embiste
                            Vector2 cola = desde - _dashRumbo * 90f;
                            StormLib.Bolt(spriteBatch, cola, desde,
                                53 + npc.whoAmI * 5, 4, 7f,
                                new Color(90, 20, 140), new Color(220, 120, 255),
                                0.85f, 6, 46f);
                            StormLib.Bolt(spriteBatch, cola + _dashRumbo * 18f, desde,
                                97 + npc.whoAmI * 5, 5, 4f,
                                new Color(140, 40, 180), new Color(255, 200, 255),
                                0.55f, 5, 30f);
                        }
                    }
                }
            }
            catch (System.Exception exRegistro)
            {
                global::AethonMod.Content.Systems.RegistroSistema.Fallo("JefeInteriorGlobalNPC", exRegistro);
                try { AuraLib.ReabrirLoteVanilla(); } catch (System.Exception exLote) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("LoteDeSombra", exLote); }
            }
        }

        // ================================================================
        //  v6.175 — EL MOTOR DE COMBATE DE LA ARENA: LOS MÉTODOS
        //  (dash con telégrafo · volea temática · combo de tres pasos)
        // ================================================================

        /// <summary>LA PRESA: el jugador vivo más cercano en rango (o null).</summary>
        private static Player PresaMasCercana(NPC npc, float radio)
        {
            Player mejor = null;
            float mejorD = radio * radio;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p == null || !p.active || p.dead) continue;
                float d = p.Center.DistanceSQ(npc.Center);
                if (d < mejorD) { mejorD = d; mejor = p; }
            }
            return mejor;
        }

        /// <summary>EL MOTOR para jefes de OLEADA (sin sello interior):
        /// el mismo combate mejorado de la arena, prestado al festín.</summary>
        public static void MotorDeCombateExterno(NPC npc, int nivel)
        {
            try
            {
                if (npc == null || !npc.active) return;
                if (Main.netMode == NetmodeID.MultiplayerClient) return;
                if (!npc.TryGetGlobalNPC<JefeInteriorGlobalNPC>(out var g)) return;
                if (!g.EsInterior) g._motorExterno = true;
                g.MotorDeCombate(npc, Math.Max(1, nivel));
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("JefeInteriorGlobalNPC", exRegistro); }
        }

        private void MotorDeCombate(NPC npc, int nivel)
        {
            try
            {
                if (Main.netMode == NetmodeID.MultiplayerClient) return;
                if (DevorarEstadoEnFauces(npc)) return;   // si lo están devorando, no combate
                Player presa = PresaMasCercana(npc, 2200f);
                if (presa == null) return;

                // LOS GUSANOS Y LA MURALLA NO DASHAN (su movimiento es
                // estructural: segmentos en cadena y muro clavado — un
                // empujón los rompería), pero SÍ disparan voleas
                bool puedeDashar = npc.type != NPCID.WallofFlesh && npc.realLife < 0 &&
                    npc.type != NPCID.EaterofWorldsHead && npc.type != NPCID.TheDestroyer;

                // ============================================================
                //  v6.177 — LA ESCALERA SIN TECHO (la letra: nivel infinito
                //  con nuevos ataques, combos y movimientos):
                //   · NIVEL 8+: EL DASH DOBLE — cerrada la embestida, si la
                //     presa sigue en rango, UNA SEGUNDA inmediata con
                //     telégrafo corto.
                //   · NIVEL 10+: LA VOLEA DOBLE — dos voleas seguidas de
                //     patrones DISTINTOS (abanico→anillo).
                //   · NIVEL 12+: EL SALTO DE SOMBRA — el jefe se desvanece
                //     y reaparece DETRÁS de la presa (el telégrafo del
                //     salto avisa la línea).
                //   · NIVEL 15+: EL COMBO DE CUATRO PASOS (ver
                //     EjecutarPasoDeCombo).
                //   · NIVEL 20+: LA FURIA — todo se acelera 20%.
                // ============================================================
                bool furia = nivel >= 20;
                float acelera = furia ? 0.8f : 1f;

                // === EL COMBO (nivel 6+): tres pasos encadenados (4 con 15+) ===
                if (nivel >= 6)
                {
                    if (_comboPaso > 0) { EjecutarPasoDeCombo(npc, presa, nivel); return; }
                    if (_comboCd > 0) _comboCd--;
                    else
                    {
                        _comboPaso = 1; _comboT = 0;
                        _comboCd = (int)(Math.Max(210, 480 - 18 * nivel) * acelera);
                        SonarEn(npc, SoundID.Roar, -0.25f, 0.55f);    // el rugido del combo
                        npc.netUpdate = true;
                    }
                }

                // === EL SALTO DE SOMBRA (nivel 12+): un movimiento NUEVO ===
                if (nivel >= 12 && puedeDashar)
                {
                    if (_saltoTele > 0)
                    {
                        // EL TELÉGRAFO: la línea del salto avisa
                        _saltoTele--;
                        npc.velocity *= 0.70f;
                        if (_saltoTele == 0)
                        {
                            Player victima = _saltoHacia >= 0 && _saltoHacia < Main.maxPlayers
                                ? Main.player[_saltoHacia] : null;
                            if (victima != null && victima.active && !victima.dead)
                            {
                                // EL SALTO: reaparece DETRÁS de la presa
                                Vector2 detras = victima.Center -
                                    victima.DirectionTo(npc.Center) * 190f;
                                npc.Center = detras;
                                npc.velocity = -victima.DirectionTo(npc.Center) * 6f;
                                npc.netUpdate = true;
                                SonarEn(npc, SoundID.Item8, 0.35f, 0.6f);
                                if (furia) { _saltoCd = (int)(190 * 0.8f); }
                            }
                        }
                    }
                    else if (_saltoCd > 0) _saltoCd--;
                    else if (Vector2.Distance(npc.Center, presa.Center) > 380f)
                    {
                        _saltoCd = Math.Max(340, 700 - 16 * nivel);
                        _saltoTele = 18;
                        _saltoHacia = presa.whoAmI;
                        SonarEn(npc, SoundID.Item117, -0.5f, 0.55f);
                        npc.netUpdate = true;
                    }
                }

                // === EL DASH (nivel 3+): telégrafo → embestida ===
                if (nivel >= 3 && puedeDashar)
                {
                    if (_dashVentana > 0)
                    {
                        // LA EMBESTIDA: la velocidad la manda el MOTOR (el
                        // PostAI corre DESPUÉS de la IA: su palabra es la
                        // última — el dash SIEMPRE gana este tick)
                        _dashVentana--;
                        float velocidad = Math.Min(15.5f, 8.5f + 0.5f * (nivel - 2));
                        npc.velocity = _dashRumbo * velocidad;
                        if (_dashVentana % 4 == 0) npc.netUpdate = true;
                        // v6.177 — EL DASH DOBLE (nivel 8+): cerrada la
                        // embestida, si la presa sigue en rango, la SEGUNDA
                        // embestida NACE INMEDIATA con telégrafo corto
                        if (_dashVentana == 0 && nivel >= 8 && !_dashDoble &&
                            Vector2.Distance(npc.Center, presa.Center) > 120f)
                        {
                            _dashDoble = true;
                            _dashTele = 8;
                            npc.netUpdate = true;
                        }
                        else if (_dashVentana == 0) _dashDoble = false;
                    }
                    else if (_dashTele > 0)
                    {
                        // EL TELÉGRAFO: se detiene, la línea de aviso vive
                        // (el PostDraw la pinta) — el jugador VE lo que viene
                        _dashTele--;
                        npc.velocity *= 0.70f;
                        if (_dashTele == 0)
                        {
                            _dashVentana = 14;
                            _dashRumbo = (presa.Center - npc.Center).SafeNormalize(Vector2.UnitX);
                            SonarEn(npc, SoundID.Item122, 0.30f, 0.6f);
                            npc.netUpdate = true;
                        }
                    }
                    else if (_dashCd > 0) _dashCd--;
                    else if (Vector2.Distance(npc.Center, presa.Center) > 110f)
                    {
                        // EL AVISO: 16 ticks de línea, luego el mordisco
                        _dashCd = Math.Max(50, 150 - 6 * (nivel - 2));
                        _dashTele = 16;
                        SonarEn(npc, SoundID.Item74, -0.40f, 0.5f);
                    }
                }

                // === LA VOLEA TEMÁTICA (nivel 4+): el patrón de SU jefe ===
                if (nivel >= 4)
                {
                    if (_voleaCd > 0) _voleaCd--;
                    else
                    {
                        _voleaCd = (int)(Math.Max(70, 210 - 10 * (nivel - 3)) * acelera);
                        DispararVolea(npc, presa, nivel, patronForzado: -1, extra: 0);
                        // v6.177 — LA VOLEA DOBLE (nivel 10+): la segunda
                        // volea vuela con el patrón CONTRARIO (abanico→anillo)
                        if (nivel >= 10) _volea2Cd = 26;
                    }
                    // LA SEGUNDA VOLEA del par (cuando está agendada)
                    if (_volea2Cd > 0 && --_volea2Cd == 0)
                    {
                        DispararVolea(npc, presa, nivel, patronForzado: 1, extra: 2);
                        SonarEn(npc, SoundID.Item122, 0.2f, 0.45f);
                    }
                }
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("JefeInteriorGlobalNPC", exRegistro); }
        }

        /// <summary>¿Este NPC está siendo devorado ahora? (el festín calla
        /// al jefe: no combate mientras lo comen — la ley v6.88).</summary>
        private static bool DevorarEstadoEnFauces(NPC npc)
        {
            try
            {
                if (npc.TryGetGlobalNPC<FaucesGlobalNPC>(out var g) &&
                    (g.DevorarEstado != 0 || g.CongeladoPorAspiradora))
                    return true;
                return false;
            }
            catch { return false; }
        }

        /// <summary>LOS TRES PASOS DEL COMBO: 1 el dash teledirigido ·
        /// 2 el ANILLO doble (la jaula) · 3 la ANDANADA cerrada.</summary>
        private void EjecutarPasoDeCombo(NPC npc, Player presa, int nivel)
        {
            _comboT++;
            switch (_comboPaso)
            {
                case 1: // EL DASH DEL COMBO: telégrafo corto y certero
                    if (_comboT == 1) { _dashTele = 10; SonarEn(npc, SoundID.Item74, -0.4f, 0.5f); }
                    if (_dashTele > 0)
                    {
                        _dashTele--;
                        npc.velocity *= 0.70f;
                        if (_dashTele == 0)
                        {
                            _dashVentana = 16;
                            _dashRumbo = (presa.Center - npc.Center).SafeNormalize(Vector2.UnitX);
                            SonarEn(npc, SoundID.Item122, 0.30f, 0.65f);
                        }
                    }
                    else if (_dashVentana > 0)
                    {
                        _dashVentana--;
                        npc.velocity = _dashRumbo * Math.Min(16.5f, 9.5f + 0.5f * (nivel - 2));
                    }
                    else if (_comboT > 40) { _comboPaso = 2; _comboT = 0; }
                    break;
                case 2: // EL ANILLO DOBLE: la jaula alrededor de la presa
                    if (_comboT == 6)
                    {
                        DispararVolea(npc, presa, nivel, patronForzado: 1, extra: 3);
                        SonarEn(npc, SoundID.Item122, -0.10f, 0.5f);
                    }
                    if (_comboT == 20)
                        DispararVolea(npc, presa, nivel, patronForzado: 1, extra: 5);
                    if (_comboT > 34) { _comboPaso = 3; _comboT = 0; }
                    break;
                case 3: // LA ANDANADA: abanico cerrado + lluvia encima
                    if (_comboT == 4)
                    {
                        DispararVolea(npc, presa, nivel, patronForzado: 0, extra: 3);
                        SonarEn(npc, SoundID.Item122, 0.15f, 0.55f);
                    }
                    if (_comboT == 16)
                        DispararVolea(npc, presa, nivel, patronForzado: 2, extra: 2);
                    // v6.177 — EL CUARTO PASO (nivel 15+): EL DASH FINAL
                    // CERZERO — la embestida que cierra el combo largo
                    if (_comboT > 30)
                    {
                        if (nivel >= 15) { _comboPaso = 4; _comboT = 0; }
                        else { _comboPaso = 0; _comboT = 0; }
                    }
                    break;
                case 4: // v6.177 — EL DASH FINAL: corto y certero
                    if (_comboT == 1) { _dashTele = 8; SonarEn(npc, SoundID.Item74, -0.3f, 0.5f); }
                    if (_dashTele > 0)
                    {
                        _dashTele--;
                        npc.velocity *= 0.70f;
                        if (_dashTele == 0)
                        {
                            _dashVentana = 14;
                            _dashRumbo = (presa.Center - npc.Center).SafeNormalize(Vector2.UnitX);
                            SonarEn(npc, SoundID.Item122, 0.45f, 0.7f);
                        }
                    }
                    else if (_dashVentana > 0)
                    {
                        _dashVentana--;
                        npc.velocity = _dashRumbo * Math.Min(17f, 10f + 0.5f * (nivel - 2));
                    }
                    else if (_comboT > 30) { _comboPaso = 0; _comboT = 0; }
                    break;
            }
            npc.netUpdate = true;
        }

        /// <summary>LA VOLEA: proyectiles TEMÁTICOS (patrón + tema del tipo
        /// de jefe — JefeInteriorVoleaProjectile lleva el dibujo).</summary>
        private static void DispararVolea(NPC npc, Player presa, int nivel, int patronForzado, int extra)
        {
            try
            {
                // v6.177 — EL PROYECTIL TEMÁTICO (la letra: cada jefe con SU
                // proyectil de Terraria — el mismo ataque luminoso para
                // todos MUERE): el mapa devuelve el vanilla del jefe; 0 =
                // el proyectil de la casa (los jefes del mod y el default)
                int tipoCasa = ModContent.ProjectileType
                    <global::AethonMod.Content.Projectiles.Jefes.JefeInteriorVoleaProjectile>();
                int tipo = global::AethonMod.Content.Projectiles.Jefes.JefeInteriorVoleaProjectile
                    .ProyectilTematico(npc.type, patronForzado >= 0 ? patronForzado : 2);
                if (tipo == 0) tipo = tipoCasa;
                int tema = global::AethonMod.Content.Projectiles.Jefes.JefeInteriorVoleaProjectile.TemaDe(npc.type);
                int patron = patronForzado >= 0 ? patronForzado
                    : global::AethonMod.Content.Projectiles.Jefes.JefeInteriorVoleaProjectile.PatronDe(npc.type);
                int dano = Math.Max(8, (int)(npc.damage * 0.55f));
                var fuente = npc.GetSource_FromAI();
                // LA SEMILLA de la salva (determinista por jefe y por racha)
                int semilla = npc.whoAmI * 17 + (int)Main.GameUpdateCount;
                var rng = new System.Random(semilla);

                switch (patron)
                {
                    case 0: // EL ABANICO APUNTADO
                    {
                        int n = 3 + nivel / 2 + extra;
                        Vector2 rumbo = (presa.Center - npc.Center).SafeNormalize(Vector2.UnitX);
                        for (int i = 0; i < n; i++)
                        {
                            float ang = (i - (n - 1) * 0.5f) * 0.16f;
                            Vector2 vel = rumbo.RotatedBy(ang) * 11f;
                            Projectile.NewProjectile(fuente, npc.Center, vel, tipo, dano, 2f,
                                Main.myPlayer, 0f, tema, 0f);
                        }
                        break;
                    }
                    case 1: // EL ANILLO RADIAL (la rueda de fuego)
                    {
                        int n = 8 + nivel + extra * 4;
                        float giro = (float)(rng.NextDouble() * Math.PI * 2);
                        for (int i = 0; i < n; i++)
                        {
                            float ang = giro + i * (MathF.PI * 2f / n);
                            Vector2 vel = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 8.5f;
                            Projectile.NewProjectile(fuente, npc.Center, vel, tipo, dano, 2f,
                                Main.myPlayer, 1f, tema, 0f);
                        }
                        break;
                    }
                    case 2: // LA LLUVIA (cae del cielo sobre la presa)
                    {
                        int n = 4 + nivel / 2 + extra;
                        for (int i = 0; i < n; i++)
                        {
                            float dx = (float)(rng.NextDouble() * 2 - 1) * 240f;
                            Vector2 pos = presa.Center + new Vector2(dx, -430f);
                            Vector2 vel = new Vector2((float)(rng.NextDouble() * 2 - 1) * 1.2f, 6.5f);
                            Projectile.NewProjectile(fuente, pos, vel, tipo, dano, 2f,
                                Main.myPlayer, 2f, tema, 0f);
                        }
                        break;
                    }
                    case 3: // LA ESPIRAL (los brazos giratorios)
                    {
                        int brazos = 3 + nivel / 4;
                        float baseAng = (float)(rng.NextDouble() * Math.PI * 2);
                        for (int b = 0; b < brazos; b++)
                            for (int k = 0; k < 2; k++)
                            {
                                float ang = baseAng + b * (MathF.PI * 2f / brazos) + k * 0.22f;
                                Vector2 vel = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * (7.5f + k * 1.8f);
                                Projectile.NewProjectile(fuente, npc.Center, vel, tipo, dano, 2f,
                                    Main.myPlayer, 3f, tema, 0f);
                            }
                        break;
                    }
                }
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("JefeInteriorGlobalNPC", exRegistro); }
        }

        /// <summary>EL SONIDO cerca del jefe (solo con oídos vivos).</summary>
        private static void SonarEn(NPC npc, Terraria.Audio.SoundStyle sonido, float tono, float volumen)
        {
            try
            {
                Terraria.Audio.SoundEngine.PlaySound(
                    sonido.WithPitchOffset(tono).WithVolumeScale(volumen), npc.Center);
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("JefeInteriorGlobalNPC", exRegistro); }
        }

        public override void OnKill(NPC npc)
        {
            // LA DERROTA SUMA (solo AUTORIDAD): el jugador mató a ESTE
            // jefe DENTRO de la arena — su próxima versión nace +25%.
            if (!EsInterior) return;
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            // v6.176 — EL KILL DEL GUSANO ES DE LA CABEZA: las piezas no
            // cuentan (un EoW no son 67 kills — la arena no se infla)
            if (EsSegmento) return;
            try
            {
                int tipo = TipoOriginal > 0 ? TipoOriginal : npc.type;
                Player cobrador = null;
                float mejor = float.MaxValue;
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player p = Main.player[i];
                    if (p == null || !p.active || p.dead) continue;
                    float d = p.Center.DistanceSQ(npc.Center);
                    if (d < mejor) { mejor = d; cobrador = p; }
                }
                if (cobrador == null) return;
                var sp = cobrador.GetModPlayer<Players.ShardPlayer>();
                if (sp == null) return;
                int nuevo = sp.MarcarKillInterior(tipo);
                if (nuevo > 0)
                {
                    Systems.EcoRed.AnunciarMundo("Mods.AethonMod.PrismaInterior.PoderCrece",
                        new Color(200, 160, 255), npc.FullName, nuevo,
                        (int)(Multiplicador(nuevo) * 100f));
                }
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("JefeInteriorGlobalNPC", exRegistro); }
        }
    }
}
