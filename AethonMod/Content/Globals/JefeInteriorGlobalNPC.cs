using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
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

        /// <summary>EL AURA VIVA (la dibujan PreDraw/PostDraw — el
        /// contrato de lotes de la casa).</summary>
        public AuraPerfil Aura;

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
            g.AplicarStats(n);
            n.netUpdate = true;
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
            int vidaMaxNueva = Math.Max(1, (int)(_vidaBase * mult));
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

        public override void SetDefaults(NPC npc)
        {
            // LA RE-ADOPCIÓN (la lección del Devorador: si vanilla MUTA
            // una pieza, SetDefaults destruye los globals frescos — el
            // sello se re-aplica en el mismo tick).
            try
            {
                if (EsInterior && npc.active && _vidaBase > 0)
                    AplicarStats(npc);
            }
            catch { }
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
            catch { }
            return true;
        }

        public override void PostAI(NPC npc)
        {
            if (!EsInterior) return;
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
                    catch { }
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

                // === NIVEL 8+ (jefes vanilla): LOS ATAQUES TEMÁTICOS ===
                // la coreografía del festín (cada jefe convoca a LOS
                // SUYOS): prestada de OleadaNPC — nivel 10: SIN PAUSA.
                if (NivelIA >= 8 && EsVanilla(npc.type))
                {
                    _tickAtaque++;
                    // v6.140 — SIN TOPE: la cadencia APRIETA con cada
                    // nivel sobre 10 (36 t base, −1 t cada 5 niveles,
                    // piso 18 t — el combo eterno de la arena infinita).
                    int cadencia = NivelIA >= 10
                        ? Math.Max(18, 36 - (NivelIA - 10) / 5) : 60;
                    if (_tickAtaque >= cadencia)
                    {
                        _tickAtaque = 0;
                        Player presa = null;
                        float mejor = float.MaxValue;
                        for (int i = 0; i < Main.maxPlayers; i++)
                        {
                            Player p = Main.player[i];
                            if (p == null || !p.active || p.dead) continue;
                            float d = p.Center.DistanceSQ(npc.Center);
                            if (d < mejor) { mejor = d; presa = p; }
                        }
                        if (presa != null && mejor < 1400f * 1400f)
                            OleadaNPC.CoreografiaPrestada(npc, presa, NivelIA);
                    }
                }
            }
            catch { }
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Aura == null) return true;
            try
            {
                if (AuraLib.DibujarNPC(npc, Aura, frontal: false))
                    AuraLib.ReabrirLoteVanilla();
            }
            catch { try { AuraLib.ReabrirLoteVanilla(); } catch { } }
            return true;
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Aura == null) return;
            try
            {
                if (AuraLib.DibujarNPC(npc, Aura, frontal: true))
                    AuraLib.ReabrirLoteVanilla();
            }
            catch { try { AuraLib.ReabrirLoteVanilla(); } catch { } }
        }

        public override void OnKill(NPC npc)
        {
            // LA DERROTA SUMA (solo AUTORIDAD): el jugador mató a ESTE
            // jefe DENTRO de la arena — su próxima versión nace +25%.
            if (!EsInterior) return;
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
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
            catch { }
        }
    }
}
