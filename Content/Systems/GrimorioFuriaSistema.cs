using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using AethonMod.Content.Globals;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// GrimorioFuriaSistema — LA FURIA DEL GRIMORIO: el evento de las
    /// OLEADAS DE HAMBRE.
    ///
    /// EL CUENTO ENTERO:
    /// 1. LA VOZ DEL HAMBRE (ShardPlayer): con el libro a nivel alto
    ///    (25+), cada 75 segundos sin matar es un MOMENTO DE HAMBRE —
    ///    EcoLib susurra (con EL SAJOR DEL BIOMA en la primera línea)
    ///    y la barra dorada palidece un poco más. Pure flavor… hasta
    ///    que no.
    /// 2. LA FURIA: al cuarto momento (~5 minutos sin comer), el libro
    ///    pierde la paciencia: "El grimorio está furioso…" y luego
    ///    "El grimorio llama a su comida…".
    /// 3. EL EVENTO (5 minutos de oleadas): UNA OLEADA POR CADA MOMENTO
    ///    DE HAMBRE acumulado, hasta 10. Cada oleada: LA CHUSMA del
    ///    bioma con stats ×(k+1) (la 1 ×2, la 10 ×11 — la letra del
    ///    usuario), y AL FINAL UN JEFE PRE-HARDMODE del bioma — v6.48
    ///    SIN importar la hora (los guardianes de la furia no duermen:
    ///    cada zona tiene SU guardián fijo y la superficie alterna Rey
    ///    Gelatina/Ojo por paridad de oleada, para que no se repita).
    ///    El pago: k monedas de oro por monstruo, k de platino + SU
    ///    ESENCIA por jefe, y TODO paga XP ×(k+1).
    /// 4. LA OLEADA ESPECIAL — EL JUICIO (v6.48): tras la oleada 10 (o
    ///    directa con la Carnada preparada en 11), TODOS los guardianes
    ///    a la vez: siete jefes ×15 en vida, daño y XP, con el aura del
    ///    JUICIO y sin pausa en los dientes. Empiezan DOS EN PANTALLA y
    ///    el resto se van sumando cada 15 s — el festín final. Cuando
    ///    cae el último: "El grimorio está saciado… por ahora." y el
    ///    perdón de la hambre.
    /// 5. LA MUERTE DEL PORTADOR (v6.48): si el jugador cae durante el
    ///    festín, EL EVENTO TERMINA y el libro TOMA VENGANZA — varias
    ///    líneas (la voz del libro SIEMPRE con prioridad: se cuela al
    ///    frente de la cola de EcoLib y las demás voces esperan detrás).
    ///
    /// SP-FIRST: la máquina corre en el servidor del mundo (en SP es el
    /// mismo proceso — las voces de EcoLib y los chat funcionan); en MP
    /// dedicado las voces son TODO pendiente como el resto del sync de
    /// la casa. La Carnada del Grimorio (ítem de prueba) dispara esto
    /// SIEMPRE, aunque la config tenga el hambre automática apagada (y
    /// cicla 1..10 y LA ESPECIAL).
    /// </summary>
    public class GrimorioFuriaSistema : ModSystem
    {
        // === LAS FASES DEL EVENTO ===
        private enum Fase { Inactivo, Llamada, Monstruos, Jefe, Interludio, Especial, Fin }

        // === EL ESTADO (servidor del mundo; SP = el mismo proceso) ===
        private static Fase _fase = Fase.Inactivo;
        private static int _ticksFase = 0;
        private static int _oleadasTotales = 0;    // N (1..10, 11 = solo especial)
        private static int _oleadaActual = 0;      // k (1..N)
        private static int _ticksOleada = 0;       // reloj de la fase de chusma
        private static int _pulsoSpawn = 0;        // tempo entre escupitajos
        private static int _bossIdx = -1;          // whoAmI del jefe de la oleada
        private static int _jugador = -1;          // whoAmI del hambriento
        // v6.50.2 — FIX (carrera del slot reciclado en FaseJefe): el TYPE del
        // jefe de la oleada, fijado al spawnear. El sello de OleadaNPC
        // valida el SLOT; el tipo valida la ESPECIE: un town NPC que recicle
        // el slot del jefe muerto en el MISMO tick (UpdateTime corre ANTES
        // de PostUpdateWorld) ya no puede colarse en la comparación del
        // sello — el crédito de la oleada y el timeout miran SOLO al jefe.
        private static int _tipoJefeOleada = -1;

        // === EL ESTADO DE LA OLEADA ESPECIAL ===
        private static int _spawneados = 0;         // cuántos guardián ya nacieron (la cuenta la valida el escaneo de sellos)
        private static int _ticksSuma = 0;          // tempo de las sumas

        // === LAS CONSTANTES DE LA CASA ===
        /// <summary>El evento completo dura 5 MINUTOS de oleadas (300 s).</summary>
        public const int TicksEvento = 18000;
        /// <summary>El jefe de la oleada tiene 90 s para caer antes de hundirse.</summary>
        public const int TicksJefeMax = 5400;
        /// <summary>La llamada dramática inicial (las dos voces).</summary>
        public const int TicksLlamada = 200;
        /// <summary>El respiro entre oleadas.</summary>
        private const int TicksInterludio = 90;
        /// <summary>Tope de chusma viva por oleada: 8 + 2k.</summary>
        private const int VivosBase = 8;
        /// <summary>Cada cuánto se suma un guardián más en LA ESPECIAL.</summary>
        private const int TicksSumaEspecial = 900; // 15 s

        /// <summary>¿El evento de las oleadas está corriendo ahora?</summary>
        public static bool Activo => _fase != Fase.Inactivo && _fase != Fase.Fin;
        /// <summary>La oleada actual (0 si no hay evento; 11 = la ESPECIAL).</summary>
        public static int OleadaActual => Activo ? _oleadaActual : 0;
        /// <summary>¿Está corriendo LA OLEADA ESPECIAL (El Juicio)?</summary>
        public static bool EspecialActiva => Activo && _fase == Fase.Especial;

        // ==================================================================
        //  v6.50.2 — EL FESTÍN VISTO DESDE LOS CLIENTES (diagnóstico)
        // ==================================================================
        //
        // La máquina de fases corre SOLO en el server (PostUpdateWorld):
        // _fase/_oleadaActual/_oleadasTotales son CEROS eternos en un cliente
        // remoto — el panel F8 mostraba "silencio" durante todo el festín.
        // EcoRed.MsgHambre lleva (fase, oleada, total) al portador y llena
        // estos estáticos en la recepción (red de seguridad 600t + cada
        // cambio de fase del ciclo).

        /// <summary>La fase del festín vista por el CLIENTE (0 = Inactivo … 6 = Fin).</summary>
        public static int FaseCliente;
        /// <summary>La oleada actual vista por el CLIENTE (0 si no hay festín).</summary>
        public static int OleadaCliente;
        /// <summary>Las oleadas totales vistas por el CLIENTE.</summary>
        public static int TotalesCliente;

        /// <summary>La fase del festín en el SERVER (lo serializa EcoRed.SincronizarHambre).</summary>
        public static int FaseServidor => (int)_fase;
        /// <summary>La oleada actual en el SERVER (lo serializa EcoRed).</summary>
        public static int OleadaServidor => _oleadaActual;
        /// <summary>Las oleadas totales en el SERVER (lo serializa EcoRed).</summary>
        public static int TotalesServidor => _oleadasTotales;

        // ==================================================================
        //  EL DISPARO
        // ==================================================================

        /// <summary>
        /// ¿Está el mundo LIBRE para el festín? (un jefe vivo o una
        /// invasión en marcha BLOQUEAN la furia — y el libro sigue
        /// acumulando hambres hasta 10 mientras espera su turno: así se
        /// alcanzan las 10 oleadas naturales).
        /// </summary>
        public static bool MundoLibre()
        {
            if (Main.invasionType != 0) return false;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n != null && n.active && n.boss) return false;
            }
            return true;
        }

        /// <summary>
        /// LA FURIA: arranca el evento con N oleadas (el número de
        /// momentos de hambre, 1..10; 11 = LA OLEADA ESPECIAL directa —
        /// la vía de prueba de la Carnada). La llama ShardPlayer cuando
        /// el libro cruza el umbral — y la Carnada del Grimorio para las
        /// pruebas (salta la config: el cebo es la herramienta de test).
        /// Corre en el servidor del mundo (SP = el mismo proceso).
        /// </summary>
        public static void Provocar(Player jugador, int oleadas)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return; // el servidor manda
            if (Activo) return;                                      // un festín a la vez
            if (jugador == null || !jugador.active) return;
            // v6.50.10 — FIX: la furia no se provoca sobre un CAÍDO —
            // Paso la cancelaría al primer tick con VenganzaPorMuerte
            // (el festín naciendo-muriendo mientras el portador
            // respawnea). La hambre ya no crece muerto (ShardPlayer);
            // esto cierra la puerta a la Carnada/otros llamadores.
            if (jugador.dead) return;

            _oleadasTotales = (int)MathHelper.Clamp(oleadas, 1, 11);
            _oleadaActual = 0;
            _jugador = jugador.whoAmI;
            _fase = Fase.Llamada;
            _ticksFase = 0;
            // v6.50.2 — EL FESTÍN CAMINA (cada cambio de fase): el estado
            // viaja al portador dentro del MsgHambre para SU diagnóstico.
            EcoRed.SincronizarHambre(jugador); // no-op fuera del servidor

            // LAS DOS VOCES: primero la ira, después la llamada — LA VOZ
            // DEL LIBRO VA PRIMERO (prioridad: se cuela al frente).
            // v6.49: EcoRed las lleva SOLO al portador (en SP habla
            // directo; en MP viajan al cliente del portador).
            EcoRed.HablarAlPortador(jugador, "Mods.AethonMod.Eco.Furia.Ira",
                new Color(168, 96, 60), rugido: true, prioridad: true);
            EcoRed.HablarAlPortador(jugador, "Mods.AethonMod.Eco.Furia.Llamada",
                new Color(226, 64, 64), rugido: true, prioridad: true);
        }

        // ==================================================================
        //  LA MÁQUINA (PostUpdateWorld: el reloj del mundo)
        // ==================================================================

        public override void PostUpdateWorld()
        {
            if (_fase == Fase.Inactivo) return;
            try { Paso(); }
            catch { Terminar(); }
        }

        private static void Paso()
        {
            _ticksFase++;
            Player hambriento = (_jugador >= 0 && _jugador < Main.maxPlayers) ? Main.player[_jugador] : null;
            if (hambriento == null || !hambriento.active)
            {
                // el portador se fue del mundo: el banquete se cancela
                Terminar();
                return;
            }

            // v6.48 — LA MUERTE DEL PORTADOR: el festín se CANCELA y el
            // libro TOMA VENGANZA (varias líneas — la voz del libro
            // siempre con prioridad). Castigo puro de voz: el evento
            // muere, la venganza es la despedida.
            if (hambriento.dead)
            {
                VenganzaPorMuerte(hambriento);
                Terminar();
                return;
            }

            switch (_fase)
            {
                case Fase.Llamada:
                    if (_ticksFase >= TicksLlamada)
                    {
                        if (_oleadasTotales >= 11) ArrancarEspecial(hambriento);
                        else SiguienteOleada(hambriento);
                    }
                    break;

                case Fase.Monstruos:
                    FaseMonstruos(hambriento);
                    break;

                case Fase.Jefe:
                    FaseJefe(hambriento);
                    break;

                case Fase.Interludio:
                    if (_ticksFase >= TicksInterludio) SiguienteOleada(hambriento);
                    break;

                case Fase.Especial:
                    FaseEspecial(hambriento);
                    break;

                case Fase.Fin:
                    if (_ticksFase >= 90) Terminar();
                    break;
            }
        }

        // ==================================================================
        //  LAS OLEADAS
        // ==================================================================

        private static void SiguienteOleada(Player hambriento)
        {
            _oleadaActual++;
            if (_oleadaActual > _oleadasTotales)
            {
                // ¿LA OLEADA ESPECIAL? Solo tras un 10x10 COMPLETO (10
                // oleadas pedidas y las 10 servidas).
                if (_oleadasTotales == 10 && _oleadaActual == 11)
                {
                    ArrancarEspecial(hambriento);
                    return;
                }

                // EL FINAL: saciedad y perdón
                _fase = Fase.Fin;
                _ticksFase = 0;
                EcoRed.HablarAlPortador(hambriento, "Mods.AethonMod.Eco.Furia.Saciado",
                    new Color(245, 196, 81), rugido: false, prioridad: true);
                // la hambre del portador se perdona: el festín contó
                var sp = hambriento.GetModPlayer<Players.ShardPlayer>();
                sp?.PerdonarHambre(); // v6.50.2: PerdonarHambre ya viaja con el festín (MsgHambre)
                return;
            }

            _fase = Fase.Monstruos;
            _ticksFase = 0;
            _ticksOleada = 0;
            _pulsoSpawn = 0;
            _bossIdx = -1;
            _tipoJefeOleada = -1;
            // v6.50.2 — EL FESTÍN CAMINA: fase nueva al portador.
            EcoRed.SincronizarHambre(hambriento); // no-op fuera del servidor

            // EL ANUNCIO: la oleada k de N (la final se anuncia en negro y rojo)
            // v6.49 — EL FESTÍN ES DEL MUNDO: el anuncio viaja a TODOS
            // (ChatHelper en MP; el usuario lo pidió así — "el evento de
            // uno es el evento del mundo", la VOZ sigue siendo del
            // portador, el AVISO es del festín).
            if (_oleadaActual == _oleadasTotales && _oleadasTotales >= 5)
                EcoRed.AnunciarMundo("Mods.AethonMod.Furia.OleadaFinal", new Color(178, 26, 38),
                    _oleadaActual, _oleadasTotales);
            else
                EcoRed.AnunciarMundo("Mods.AethonMod.Furia.Oleada", new Color(198, 200, 206),
                    _oleadaActual, _oleadasTotales, _oleadaActual + 1);
        }

        private static void FaseMonstruos(Player hambriento)
        {
            int duracion = Math.Max(600, TicksEvento / Math.Max(1, _oleadasTotales)); // ≥ 10 s por oleada
            _ticksOleada++;

            // === LOS ESCUPITAJOS DE CHUSMA ===
            _pulsoSpawn++;
            if (_pulsoSpawn >= 90) // cada 1.5 s
            {
                _pulsoSpawn = 0;
                int vivos = ContarChusma();
                int tope = VivosBase + 2 * _oleadaActual;
                if (vivos < tope)
                {
                    int porPulso = 2 + (_oleadaActual + 2) / 3; // 3..6
                    int[] pool = PoolMonstruos(hambriento);
                    for (int s = 0; s < porPulso; s++)
                        SpawnMonstruo(hambriento, pool);
                }
            }

            // === ¿SE ACABÓ LA OLEADA? ===
            // Por reloj (su parte de los 5 minutos) o porque la chusma se
            // limpió habiendo pasado al menos media fase.
            bool porReloj = _ticksOleada >= duracion;
            bool limpia = ContarChusma() == 0 && _ticksOleada >= duracion / 2;
            if (porReloj || limpia)
            {
                _fase = Fase.Jefe;
                _ticksFase = 0;
                _bossIdx = -1;
                SpawnJefeOleada(hambriento);
                // v6.50.2 — EL FESTÍN CAMINA: fase nueva al portador.
                EcoRed.SincronizarHambre(hambriento); // no-op fuera del servidor
            }
        }

        private static void FaseJefe(Player hambriento)
        {
            NPC jefe = (_bossIdx >= 0 && _bossIdx < Main.maxNPCs) ? Main.npc[_bossIdx] : null;

            // v6.50.1 — FIX (EL SLOT HUÉRFANO + LA HUIDA QUE ERA VICTORIA):
            // (a) _bossIdx es un whoAmI reciclable — si el jefe moría y el
            // slot se re-llenaba con otro NPC, la furia miraba al EXTRAÑO
            // hasta el timeout (o lo "mataba" por él): el sello de
            // OleadaNPC valida que el slot siga siendo del festín.
            // (b) un jefe que se DESPAWNEA solo (amanecer, distancia) no
            // debe entregar el DerrotaOleada10: solo la MUERTE (life ≤ 0,
            // confirmada por el sello) paga. La instancia muerta conserva
            // el sello (solo NewNPC lo resetea) y el reciclado lo pierde.
            var sello = jefe != null ? jefe.GetGlobalNPC<Globals.OleadaNPC>() : null;
            // v6.50.2 — FIX (carrera del slot reciclado en FaseJefe): además
            // del sello, la ESPECIE — el type del jefe se fija al spawnear
            // (_tipoJefeOleada). Un town NPC que recicle el slot del jefe
            // muerto en el MISMO tick (UpdateTime corre ANTES de
            // PostUpdateWorld) queda fuera del festín por AMBAS puertas: no
            // roba el crédito de DerrotaOleada10 ni sufre el timeout como
            // si fuera el guardián.
            bool esDelFestin = sello != null && sello.EsJefeDeOleada &&
                _tipoJefeOleada > 0 && jefe.type == _tipoJefeOleada;

            // EL JEFE CALLÓ (vida ≤ 0 con sello) → la oleada se completa
            if (esDelFestin && jefe.life <= 0)
            {
                // v6.48 — LA OLEADA 10 VENCIDA: el portador gana el
                // DERECHO A LAS ESENCIAS del Testigo (la tienda de 10
                // de platino se abre) — y la ESPECIAL viene después.
                if (_oleadaActual >= 10 && _oleadasTotales >= 10)
                {
                    var sp = hambriento.GetModPlayer<Players.ShardPlayer>();
                    if (sp != null && !sp.DerrotaOleada10)
                    {
                        sp.DerrotaOleada10 = true;
                        // v6.50 — el DERECHO CAMINA: la puerta de la tienda
                        // la lee el Testigo en el CLIENTE del portador —
                        // EcoRed.MsgCronica lleva la marca YA (antes vivía
                        // solo en la réplica del server y el botón de las
                        // esencias jamás se encendía en MP).
                        EcoRed.SincronizarCronica(hambriento);
                        // v6.49 — EL AVISO PRIVADO: solo al portador (su
                        // derecho, su pantalla).
                        EcoRed.AnunciarAlPortador(hambriento,
                            "Mods.AethonMod.Furia.DerechoEsencias",
                            new Color(196, 150, 255));
                    }
                }
                _fase = Fase.Interludio;
                _ticksFase = 0;
                // v6.50.2 — EL FESTÍN CAMINA: fase nueva al portador.
                EcoRed.SincronizarHambre(hambriento); // no-op fuera del servidor
                return;
            }

            // EL JEFE YA NO ESTÁ (slot reciclado, nunca nació o se fue
            // solo) → la furia sigue SIN crédito de victoria.
            if (!esDelFestin || !jefe.active)
            {
                _fase = Fase.Interludio;
                _ticksFase = 0;
                // v6.50.2 — EL FESTÍN CAMINA: fase nueva al portador.
                EcoRed.SincronizarHambre(hambriento); // no-op fuera del servidor
                return;
            }

            // EL JEFE SE CANSÓ: se hunde insatisfecho (90 s) y la furia
            // sigue su curso — 10 jefes vivos a la vez no es un evento,
            // es un dilema de render.
            if (_ticksFase >= TicksJefeMax)
            {
                EcoRed.AnunciarMundo("Mods.AethonMod.Furia.JefeHuido",
                    new Color(150, 140, 148), jefe.FullName);
                jefe.active = false; // despawn limpio (patrón HollowTitan)
                // v6.50.1 — FIX (JEFE FANTASMA): este despawn corre en
                // PostUpdateWorld (FUERA de la AI del NPC — vanilla difunde
                // el 23 desde UpdateNetworkCode solo si el apagado pasa
                // DENTRO de su update). Sin el 23 explícito, los clientes
                // conservaban un jefe congelado/invulnerable hasta que el
                // slot se reciclaba.
                if (Main.netMode == NetmodeID.Server)
                    Terraria.NetMessage.SendData(23, -1, -1, null, jefe.whoAmI);
                _fase = Fase.Interludio;
                _ticksFase = 0;
                // v6.50.2 — EL FESTÍN CAMINA: fase nueva al portador.
                EcoRed.SincronizarHambre(hambriento); // no-op fuera del servidor
            }
        }

        // ==================================================================
        //  LA OLEADA ESPECIAL — EL JUICIO (todos los guardianes a ×15)
        // ==================================================================

        /// <summary>
        /// LOS SIETE GUARDIANES de las oleadas (el reparto completo de
        /// zonas) — TODOS juntos en la ESPECIAL, cada uno vestido de
        /// ×15 y con el aura del JUICIO.
        /// </summary>
        private static int[] LosSiete()
        {
            return new int[]
            {
                NPCID.KingSlime,
                NPCID.EyeofCthulhu,
                NPCID.Deerclops,
                NPCID.QueenBee,
                NPCID.EaterofWorldsHead,
                NPCID.BrainofCthulhu,
                NPCID.SkeletronHead,
            };
        }

        /// <summary>
        /// ARRANCA EL JUICIO: la voz del libro anuncia el festín final y
        /// los guardianes empiezan a nacer — DOS EN PANTALLA desde el
        /// primer segundo (la letra del usuario) y el resto se suma cada
        /// 15 s hasta los siete.
        /// </summary>
        private static void ArrancarEspecial(Player hambriento)
        {
            _fase = Fase.Especial;
            _ticksFase = 0;
            _oleadaActual = 11;
            _ticksSuma = 0;
            _spawneados = 0;
            // v6.50.2 — EL FESTÍN CAMINA: el Juicio empieza — fase nueva al
            // portador (la oleada 11 también viaja).
            EcoRed.SincronizarHambre(hambriento); // no-op fuera del servidor

            // LA VOZ DEL JUICIO (prioridad: el libro manda — solo al
            // portador: es SU grimorio quien juzga).
            EcoRed.HablarAlPortador(hambriento, "Mods.AethonMod.Eco.Furia.Juicio",
                new Color(178, 26, 38), rugido: true, prioridad: true);
            EcoRed.AnunciarMundo("Mods.AethonMod.Furia.OleadaEspecial",
                new Color(178, 26, 38));

            // LOS DOS PRIMEROS: en pantalla YA.
            for (int i = 0; i < 2; i++) NacerGuardian(hambriento);
        }

        /// <summary>Nace UN guardián de la ESPECIAL en su anillo alrededor del portador.</summary>
        private static void NacerGuardian(Player hambriento)
        {
            try
            {
                if (_spawneados >= 7) return;
                int tipo = LosSiete()[_spawneados];
                float ang = _spawneados * (MathHelper.TwoPi / 7f) + 0.7f;
                Vector2 pos = hambriento.Center + new Vector2(
                    MathF.Cos(ang), MathF.Sin(ang)) * 520f - new Vector2(0f, 180f);

                int idx = NPC.NewNPC(hambriento.GetSource_FromAI(), (int)pos.X, (int)pos.Y, tipo);
                NPC jefe = (idx >= 0 && idx < Main.maxNPCs) ? Main.npc[idx] : null;
                if (jefe == null || !jefe.active) return;

                // EL SELLO DEL JUICIO: ×15 en TODO, aura del JUICIO.
                jefe.GetGlobalNPC<OleadaNPC>().Marcar(jefe, 11, jefe: true, especial: true);
                jefe.netUpdate = true;

                _spawneados++;

                EcoRed.AnunciarMundo("Mods.AethonMod.Furia.Jefe",
                    new Color(226, 64, 64), jefe.FullName, 15);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, jefe.Center);
            }
            catch { }
        }

        /// <summary>
        /// EL JUICIO EN MARCHA: cada 15 s nace OTRO guardián hasta los
        /// siete (siempre ≥ 2 en pantalla mientras vivan). El festín
        /// termina cuando cae el ÚLTIMO — entonces la saciedad especial
        /// y el perdón de la hambre.
        /// </summary>
        private static void FaseEspecial(Player hambriento)
        {
            // LA SUMA: otro guardián cada 15 s (hasta 7).
            if (_spawneados < 7)
            {
                _ticksSuma++;
                if (_ticksSuma >= TicksSumaEspecial)
                {
                    _ticksSuma = 0;
                    NacerGuardian(hambriento);
                }
            }

            // ¿VIVE ALGUNO? (los índices pueden reciclarse: valida tipo+marca)
            // v6.50.2 — FIX (el Juicio se cerraba con el Devorador vivo):
            // EoW (tipos 13/14/15) NO pone npc.boss=true en vanilla (lo
            // trackean por tipo) → quedaba fuera del conteo y el Juicio
            // terminaba "saciado" con el guardián ×15 aún en el mundo. El
            // sello (EsDeOleada && EsEspecial && EsJefeDeOleada) ya valida
            // de sobra — el filtro boss sobraba y mentía.
            int vivos = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active) continue;
                var sello = n.GetGlobalNPC<OleadaNPC>();
                if (sello != null && sello.EsDeOleada && sello.EsEspecial && sello.EsJefeDeOleada)
                    vivos++;
            }

            if (_spawneados >= 7 && vivos == 0)
            {
                // EL FINAL DEL JUICIO: la saciedad especial (la variante la
                // reparte la autoridad y viaja por EcoRed).
                _fase = Fase.Fin;
                _ticksFase = 0;
                EcoRed.HablarVarianteAlPortador(hambriento,
                    "Mods.AethonMod.Eco.Furia.JuicioFin", 3,
                    new Color(245, 196, 81), rugido: false, prioridad: true);
                var sp = hambriento.GetModPlayer<Players.ShardPlayer>();
                sp?.PerdonarHambre();
            }
        }

        // ==================================================================
        //  LA VENGANZA POR MUERTE (el castigo de voz del libro)
        // ==================================================================

        /// <summary>
        /// v6.48 — EL PORTADOR MURIÓ durante el festín: el evento SE
        /// TERMINA y el libro toma venganza — no de daño: de PALABRA
        /// (4 muestras para no repetir). La voz del libro con PRIORIDAD:
        /// las voces de jefes que esperaban en la cola salen DESPUÉS.
        /// </summary>
        private static void VenganzaPorMuerte(Player hambriento)
        {
            try
            {
                // LA VENGANZA ES DEL LIBRO — la oye SOLO el portador muerto
                // (prioridad: las voces de jefes esperan detrás).
                EcoRed.HablarVarianteAlPortador(hambriento,
                    "Mods.AethonMod.Eco.Furia.Venganza", 4,
                    new Color(178, 26, 38), rugido: true, escala: 0.62f, prioridad: true);
                EcoRed.AnunciarMundo("Mods.AethonMod.Furia.MuertePortador",
                    new Color(150, 140, 148));
            }
            catch { }
        }

        /// <summary>Cuánta chusma de ESTA oleada sigue viva.</summary>
        private static int ContarChusma()
        {
            int vivos = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active) continue;
                var sello = n.GetGlobalNPC<OleadaNPC>();
                if (sello != null && sello.EsDeOleada && !n.boss) vivos++;
            }
            return vivos;
        }

        // ==================================================================
        //  LOS POOLS — el bioma decide la comida (la hora ya no manda)
        // ==================================================================

        /// <summary>
        /// Los monstruos que el bioma del portador ofrece. IDs verificados
        /// contra el Terraria real (sondeo de reflexión).
        /// </summary>
        private static int[] PoolMonstruos(Player p)
        {
            // === INFIERNO ===
            if (p.ZoneUnderworldHeight)
                return new int[] { NPCID.Demon, NPCID.FireImp, NPCID.LavaSlime };

            // === MAZMORRA ===
            if (p.ZoneDungeon)
                return new int[] { NPCID.AngryBones, NPCID.DungeonSlime, NPCID.BlazingWheel };

            // === CAVERNAS DE GRANITO/MÁRMOL ===
            if (p.ZoneGranite)
                return new int[] { NPCID.GraniteGolem, NPCID.GraniteFlyer };
            if (p.ZoneMarble)
                return new int[] { NPCID.GreekSkeleton, NPCID.Medusa, NPCID.GreekSkeleton };

            // === NIEVE ===
            if (p.ZoneSnow)
                return new int[] { NPCID.IceSlime, NPCID.SnowFlinx, NPCID.ZombieEskimo };

            // === JUNGLA ===
            if (p.ZoneJungle)
                return new int[] { NPCID.JungleSlime, NPCID.JungleBat, NPCID.Hornet };

            // === CORRUPCIÓN / CARMESÍ ===
            if (p.ZoneCorrupt)
                return new int[] { NPCID.EaterofSouls, NPCID.EaterofSouls, NPCID.CorruptSlime };
            if (p.ZoneCrimson)
                return new int[] { NPCID.Crimera, NPCID.FaceMonster, NPCID.BloodCrawler };

            // === DESIERTO ===
            if (p.ZoneDesert)
                return new int[] { NPCID.SandSlime, NPCID.Antlion, NPCID.Antlion };

            // === PLAYA ===
            if (p.ZoneBeach)
                return new int[] { NPCID.Crab, NPCID.BlueSlime, NPCID.Crab };

            // === CIELO ===
            if (p.ZoneSkyHeight)
                return new int[] { NPCID.Harpy, NPCID.Harpy, NPCID.BlueSlime };

            // === SUBSUELO (roca o tierra, sin bioma especial) ===
            if (p.ZoneRockLayerHeight || p.ZoneDirtLayerHeight)
                return new int[] { NPCID.CaveBat, NPCID.Skeleton, NPCID.BlueSlime };

            // === SUPERFICIE ===
            return new int[] { NPCID.Zombie, NPCID.DemonEye, NPCID.GreenSlime, NPCID.BlueSlime };
        }

        /// <summary>
        /// El JEFE PRE-HARDMODE que cierra la oleada según el bioma.
        ///
        /// v6.48 — LA REGLA NUEVA DEL USUARIO: los jefes de oleada NO se
        /// ven afectados por la HORA — cada ZONA tiene su guardián fijo
        /// (aparecen en su zona predeterminada), y las zonas que no
        /// tenían guardián YA TIENEN UNO:
        ///   · superficie → alterna Rey Gelatina / Ojo por PARIDAD de
        ///     oleada (variedad sin hora: la 1 Rey, la 2 Ojo, la 3 Rey…)
        ///   · desierto → REY GELATINA (la corona de la arena)
        ///   · playa y cielo → OJO DE CTHULHU (el vigía que vuela)
        ///   · granito/mármol/subsuelo → el MAL DEL MUNDO (Devorador o
        ///     Cerebro según el mundo)
        ///   · nieve → Deerclops · jungla → Abeja Reina · corrupción →
        ///     Devorador · carmesí → Cerebro · mazmorra → Skeletron ·
        ///     infierno → el Ojo los caza.
        /// El Muro de Carne sigue EXCLUIDO a propósito (una furia
        /// involuntaria no abre el hardmode).
        /// </summary>
        private static int JefeDelLugar(Player p)
        {
            if (p.ZoneUnderworldHeight) return NPCID.EyeofCthulhu; // el ojo los caza en el infierno
            if (p.ZoneDungeon) return NPCID.SkeletronHead;          // v6.48: SIN hora — el guardián no duerme
            if (p.ZoneSnow) return NPCID.Deerclops;
            if (p.ZoneJungle) return NPCID.QueenBee;
            if (p.ZoneCorrupt) return NPCID.EaterofWorldsHead;
            if (p.ZoneCrimson) return NPCID.BrainofCthulhu;

            // DESIERTO (v6.48 — zona SIN guardián, ahora con el Rey).
            if (p.ZoneDesert) return NPCID.KingSlime;

            // PLAYA y CIELO (v6.48 — zonas sin guardián, ahora con el Ojo).
            if (p.ZoneBeach) return NPCID.EyeofCthulhu;
            if (p.ZoneSkyHeight) return NPCID.EyeofCthulhu;

            // SUBSUELO sin bioma: el mal del mundo (o el Rey, en mundos limpios)
            if (p.ZoneRockLayerHeight || p.ZoneDirtLayerHeight)
                return WorldGen.crimson ? NPCID.BrainofCthulhu : NPCID.EaterofWorldsHead;

            // SUPERFICIE: por PARIDAD de oleada (sin hora — la 1 Rey, la 2 Ojo…)
            return (_oleadaActual % 2 == 1) ? NPCID.KingSlime : NPCID.EyeofCthulhu;
        }

        // ==================================================================
        //  LOS SPAWNS
        // ==================================================================

        /// <summary>Escupe UN monstruo de la oleada alrededor del portador.</summary>
        private static void SpawnMonstruo(Player hambriento, int[] pool)
        {
            try
            {
                int tipo = pool[(int)(Main.rand.NextFloat() * pool.Length) % pool.Length];

                // posición: anillo alrededor del portador (fuera de pantalla a ser posible)
                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                float dist = 380f + Main.rand.NextFloat(320f);
                Vector2 pos = hambriento.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * dist;

                // v6.50.2 — FIX (spawns voladores FUERA del mundo): los
                // noTileCollide (DemonEye, murciélagos, Harpy…) nacen donde
                // caiga el anillo — junto al borde del mundo (océano, cielo)
                // nacían MÁS ALLÁ de los límites y vanilla los descarta o
                // los deja como zombis sin IA. Clamp ANTES de NewNPC (los
                // que chocan con tiles siguen pasando por BuscarSuelo, que
                // ya respeta bordes por su cuenta).
                pos.X = Math.Clamp(pos.X, Main.leftWorld + 400f, Main.rightWorld - 400f);
                pos.Y = Math.Clamp(pos.Y, Main.topWorld + 400f, Main.bottomWorld - 400f);

                NPC npc = Main.npc[NPC.NewNPC(hambriento.GetSource_FromAI(),
                    (int)pos.X, (int)pos.Y, tipo)];
                if (npc == null || !npc.active) return;

                // los que chocan con tiles necesitan SUELO: buscar hacia abajo
                if (!npc.noTileCollide)
                {
                    Vector2 good = BuscarSuelo(pos);
                    if (good == Vector2.Zero) { npc.active = false; return; } // sin hueco: este no nace
                    npc.position = good - new Vector2(npc.width / 2f, npc.height);
                }
                npc.netUpdate = true;

                // EL SELLO (marca + stats + aura)
                npc.GetGlobalNPC<OleadaNPC>().Marcar(npc, _oleadaActual, jefe: false);
            }
            catch { }
        }

        /// <summary>Convoca al JEFE (versión especial de la oleada) del bioma.</summary>
        private static void SpawnJefeOleada(Player hambriento)
        {
            try
            {
                int tipo = JefeDelLugar(hambriento);
                Vector2 pos = hambriento.Center + new Vector2(
                    hambriento.direction * -560f, -240f); // frente al portador, arriba

                int idx = NPC.NewNPC(hambriento.GetSource_FromAI(), (int)pos.X, (int)pos.Y, tipo);
                NPC jefe = (idx >= 0 && idx < Main.maxNPCs) ? Main.npc[idx] : null;
                if (jefe == null || !jefe.active) return;
                _bossIdx = idx;
                // v6.50.2 — FIX (carrera del slot reciclado): la ESPECIE del
                // jefe queda registrada al nacer — FaseJefe la valida junto
                // al sello (un slot reciclado por un town NPC del mismo tick
                // ya no pasa por jefe del festín).
                _tipoJefeOleada = tipo;

                jefe.GetGlobalNPC<OleadaNPC>().Marcar(jefe, _oleadaActual, jefe: true);
                jefe.netUpdate = true;

                EcoRed.AnunciarMundo("Mods.AethonMod.Furia.Jefe",
                    new Color(226, 64, 64), jefe.FullName, _oleadaActual + 1);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, jefe.Center);
            }
            catch { _bossIdx = -1; _tipoJefeOleada = -1; }
        }

        /// <summary>
        /// Busca el primer tile SÓLIDO bajo la posición (hasta 50 tiles
        /// abajo); devuelve el punto DE ENCIMA o Vector2.Zero si no hay.
        /// Bordes del mundo respetados (índices clampeados).
        /// </summary>
        private static Vector2 BuscarSuelo(Vector2 pos)
        {
            int x = (int)(pos.X / 16f);
            int y = (int)(pos.Y / 16f);
            if (x < 5 || x >= Main.maxTilesX - 5 || y < 5 || y >= Main.maxTilesY - 5)
                return Vector2.Zero; // fuera del mundo: este no nace
            for (int i = 0; i < 50; i++)
            {
                if (y + i >= Main.maxTilesY - 5) break;
                Tile tile = Main.tile[x, y + i];
                if (tile != null && tile.HasTile && Main.tileSolid[tile.TileType])
                    return new Vector2(pos.X, (y + i) * 16f);
            }
            return Vector2.Zero;
        }

        // ==================================================================
        //  EL FINAL
        // ==================================================================

        private static void Terminar()
        {
            // v6.50.2 — EL FESTÍN CAMINA: el estado final (Inactivo) también
            // viaja — sin esto el F8 del portador mostraba el último festín
            // "en marcha" para siempre. ANTES de resetear _jugador (lo
            // necesita la réplica); seguro en unload (try/catch dentro de
            // SincronizarHambre y la réplica ya puede no existir).
            try
            {
                Player ultimo = (_jugador >= 0 && _jugador < Main.maxPlayers)
                    ? Main.player[_jugador] : null;
                if (ultimo != null && ultimo.active)
                    EcoRed.SincronizarHambre(ultimo); // no-op fuera del servidor
            }
            catch { }

            _fase = Fase.Inactivo;
            _oleadasTotales = 0;
            _oleadaActual = 0;
            _bossIdx = -1;
            _tipoJefeOleada = -1;
            _jugador = -1;
            _spawneados = 0;
            _ticksSuma = 0;

            // v6.50.10 — FIX: también las RÉPLICAS de cliente (el F8 de
            // un remoto mostraba el último festín "en marcha" para
            // siempre tras cambiar de mundo — fantasmas estáticos que
            // nadie reseteaba; Terminar corre vía OnWorldUnload).
            FaseCliente = 0;
            OleadaCliente = 0;
            TotalesCliente = 0;
        }

        /// <summary>
        /// v6.49 — EL DIAGNÓSTICO DEL FESTÍN (lo pinta el overlay F8): la
        /// oleada y la fase, ya localizadas — o el silencio.
        /// </summary>
        public static string Diagnostico()
        {
            try
            {
                // v6.50.2 — FIX (diagnóstico de furia siempre "inactivo" en
                // clientes MP): la máquina vive SOLO en el server
                // (PostUpdateWorld) — un remoto leía _fase == Inactivo
                // eterno. En clientes MP se leen los estáticos que
                // EcoRed.MsgHambre llena en la recepción (red de seguridad
                // 600t + cada cambio de fase).
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    if (FaseCliente == 0)
                        return Language.GetTextValue("Mods.AethonMod.Diag.FuriaInactivo");
                    return Language.GetTextValue("Mods.AethonMod.Diag.FuriaActiva",
                        OleadaCliente, TotalesCliente);
                }
                if (_fase == Fase.Inactivo)
                    return Language.GetTextValue("Mods.AethonMod.Diag.FuriaInactivo");
                return Language.GetTextValue("Mods.AethonMod.Diag.FuriaActiva",
                    _oleadaActual, _oleadasTotales);
            }
            catch { return ""; }
        }

        public override void OnWorldUnload()
        {
            Terminar();
            AuraLib.Reiniciar(); // los emisores de partículas mueren con el mundo
            // v6.49 — EL BARRENDERO COMPLETO (auditoría AUD-C): el núcleo
            // de VFX y el anti-coros de audio también mueren con el mundo.
            try { VFXCore.Reiniciar(); } catch { }
            try { AudioLib.Reiniciar(); } catch { }
            // v6.50.1 — FIX: el contador de la carnada era un ESTÁTICO que
            // sobrevivía al mundo (dato de mundo en campo de clase — la
            // deuda nº recurrente de las auditorías). Vuelve al default.
            try { Items.CarnadaDelGrimorio.OleadasPreparadas = 3; } catch { }
        }
        public override void Unload()
        {
            Terminar();
            AuraLib.Reiniciar(); // las texturas de ruido también
        }
    }
}
