using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Projectiles.Definitivas;

namespace AethonMod.Content.Systems
{
    // ======================================================================
    //  LOS ATAQUES COMBINADOS DE LOS OJOS (v6.51.0) — LOS PODERES DEL
    //  DESPERTAR, LA LETRA COMPLETA:
    //
    //  · EL SOL RÚNICO (nivel 7+): «2 minions con la primera mejora
    //    lanzan su ataque en un punto cercano encima del jugador, los 2
    //    rayos de perlin que lanzan convergen en un punto y de ahí crece
    //    un sol runico de 1 anillo, los minions que están creando el sol
    //    hacen una animación y lanzan con fuerza el sol runico al
    //    enemigo haciendo daño de área, quemando… este ataque se puede
    //    hacer 2 veces por minuto y de forma aleatoria» — nivel 17: DOS
    //    ANILLOS y 3/min; nivel 35: TRES ANILLOS y 4/min.
    //
    //  · LOS AGUJEROS NEGROS (nivel 10+): «al tener 5 o más minion con
    //    la primera mejora, estos pueden combinar su ataque y lanzar un
    //    agujero negro aleatorio de 4 listados: Agujero del Olvido,
    //    Agujero Cósmico, Agujero del Umbral y el Agujero de la Bruma…
    //    solo lanzarán uno de esos 4 aleatoriamente cada minuto» — nivel
    //    25: la versión ASCENDIDA; nivel 45: 2/min Y EL SUPREMO
    //    independiente («Agujero Negro Supremo y Agujero Supremo Aurora…
    //    el ataque combinado de 8 minions o más en su mejora número 4,
    //    siempre se puede lanzar uno de esos cada minuto»).
    //
    //  LA MÁQUINA: cada jugador corre SU coordinador en SU pantalla (el
    //  dueño de los ojos es la autoridad de sus propios proyectiles — el
    //  patrón del Disparar de OjoEternoMinion: owner == Main.myPlayer);
    //  los relojes viven aquí y se limpian con el mundo.
    // ======================================================================
    public class AtaquesCombinadosSistema : ModSystem
    {
        /// <summary>EL RELOJ del sol de cada jugador (GameUpdateCount en
        /// que podrá volver a nacer).</summary>
        private static readonly Dictionary<int, ulong> _solHasta = new();

        /// <summary>EL RELOJ de los agujeros de cada jugador.</summary>
        private static readonly Dictionary<int, ulong> _agujeroHasta = new();

        /// <summary>EL RELOJ del supremo de cada jugador (independiente).</summary>
        private static readonly Dictionary<int, ulong> _supremoHasta = new();

        /// <summary>v6.59 — EL RELOJ de la IRA de cada jugador (el ataque
        /// del despertar 50 — EL FINAL DEL CAMINO). v6.60 — el ritmo lo
        /// pone IRA_MODO_PRUEBA: la PRUEBA 5 haces/min, el FINAL 1 cada
        /// 30 minutos.</summary>
        private static readonly Dictionary<int, ulong> _iraHasta = new();

        /// <summary>v6.62 — LA COLA DEL SILENCIO (la letra: «debes hacer
        /// que en el momento en el que el haz comienza la carga ya no
        /// pueden lanzarse ni agujeros negros runicos ni soles runicos
        /// hasta que el haz se acabe y hayan pasado al menos 5 segundos
        /// de acabado el haz»): mientras la ira VIVE (ceremonia o haz)
        /// el reloj se empuja SIEMPRE ADELANTE; muerto el haz, la llave
        /// expira 300 ticks (5 s) después de su ÚLTIMO tick vivo — el
        /// silencio cubre la creación, el disparo Y la cola.</summary>
        private static readonly Dictionary<int, ulong> _iraColaHasta = new();

        /// <summary>LA COLA en ticks: 5 segundos POST-HAZ (+15 de colchón
        /// de red — «al menos 5 segundos», nunca menos).</summary>
        private const ulong IRA_COLA_TICKS = 300ul + 15ul;

        // ================================================================
        //  v6.60 — LA LETRA NUEVA DE LA IRA, COMPLETA: «la cantidad de
        //  minions necesarios para lanzar el ataque es de 11 y con
        //  todas las mejoras, ese es el requisito, si hay menos de 11
        //  minions o no están mejorados al máximo entonces no pueden
        //  lanzar la ira del ser exterior» — EL ESCUADRÓN ENTERO (el
        //  tope del Eterno ES 11: v6.50.116, «así se ve mejor en la
        //  animación» — anillo interior de 5 + exterior de 6) y CADA
        //  ojo con sus 4 MEJORAS (la última se desbloquea en el
        //  despertar 40): el círculo solo se cierra con el escuadrón
        //  en su máximo absoluto. Y «por ahora y como prueba se debe
        //  lanzar 5 haces por minuto, cuando termine la prueba pasará
        //  a 1 cada 30 minutos» — el MODO PRUEBA marca el compás; al
        //  terminar la prueba SOLO hay que pasar IRA_MODO_PRUEBA a
        //  false (nada más cambia: el tooltip lo lee solo).
        // ================================================================

        /// <summary>EL REQUISITO DE LA IRA: el escuadrón entero — ONCE
        /// ojos (el tope exacto del Eterno) TODOS en su mejora final.
        /// </summary>
        private const int IRA_OJOS_MINIMOS = 11;

        /// <summary>EL MODO PRUEBA de la ira (la letra: «por ahora y
        /// como prueba»): true = el compás de la PRUEBA (5 haces por
        /// minuto); AL TERMINAR LA PRUEBA se pasa a false y sola cae a
        /// 1 cada 30 minutos — NADA MÁS cambia. (static readonly y no
        /// const: con const el compilador hornea la rama muerta del
        /// modo final y escupe el CS0162 de código inalcanzable — el
        /// oráculo de la casa vive en el 0/0.)</summary>
        private static readonly bool IRA_MODO_PRUEBA = true;

        /// <summary>EL COMPÁS DE PRUEBA: 5 haces por minuto (3600/5 =
        /// cada 720 ticks — 12 s).</summary>
        private const ulong IRA_ENFRIAMIENTO_PRUEBA = 3600ul / 5ul;

        /// <summary>EL COMPÁS FINAL (cuando la prueba termine): 1 haz
        /// cada 30 minutos (3600 × 30 = 108.000 ticks).</summary>
        private const ulong IRA_ENFRIAMIENTO_FINAL = 3600ul * 30ul;

        /// <summary>EL RITMO DE LA IRA EN TEXTO — lo lee el tooltip del
        /// grimorio (GrimoireEternal): SIEMPRE dice la verdad de los
        /// const de arriba, el de la prueba y el del final.</summary>
        public static string IraRitmoTexto() => IRA_MODO_PRUEBA
            ? "prueba: 5 haces/min"
            : "1 cada 30 min";

        /// <summary>El ruido de la moneda aleatoria (por tick, cuando el
        /// reloj ya está listo: 1 de cada ~75 t dispara).</summary>
        private const int DADO = 75;

        /// <summary>El radio de caza del enemigo blanco.</summary>
        private const float RADIO_CAZA = 1400f;

        public override void OnWorldUnload()
        {
            _solHasta.Clear();
            _agujeroHasta.Clear();
            _supremoHasta.Clear();
            _iraHasta.Clear();
            _iraColaHasta.Clear();
        }

        /// <summary>UN TICK DEL COORDINADOR (lo llama ShardPlayer.PostUpdate
        /// — SOLO en la pantalla del dueño de los ojos).</summary>
        public static void Tick(Player p)
        {
            try
            {
                if (p == null || !p.active || p.dead) return;
                if (Main.gameMenu) return;
                // LA PANTALLA DEL DUEÑO (SP: myPlayer; MP: cada cliente
                // con SUS ojos; el host con los suyos).
                if (p.whoAmI != Main.myPlayer || Main.dedServ) return;

                int despertar = DespertarGrimorio.NivelDe(p);
                if (despertar < 7) return;   // el primer poder nace en el 7

                // === EL ESCUADRÓN: cuántos ojos, cuántos con mejora, ===
                // === cuántos en su mejora FINAL                    ===
                int tipoOjo = ModContent.ProjectileType
                    <global::AethonMod.Content.Projectiles.OjoEternoMinion>();
                var mejorados = new List<Projectile>();
                int mejoras4 = 0;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile pr = Main.projectile[i];
                    if (pr == null || !pr.active || pr.type != tipoOjo) continue;
                    if (pr.owner != p.whoAmI) continue;
                    int m = global::AethonMod.Content.Projectiles.OjoEternoMinion.MejorasDe(pr);
                    if (m >= 1) mejorados.Add(pr);
                    if (m >= 4) mejoras4++;
                }
                if (mejorados.Count == 0) return;

                // === EL ENEMIGO BLANCO (el más cercano y de verdad) ===
                NPC blanco = null;
                float mejorD = RADIO_CAZA * RADIO_CAZA;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    // v6.51.1 — LA CURA (la letra: «no son lanzados por los
                    // minions cuando deben ser lanzados»): el filtro viejo
                    // tenía el AMISTOSO invertido — SALTABA A LOS ENEMIGOS y
                    // solo dejaba pasar a los vecinos del pueblo (que
                    // EsObjetivo rechaza): el blanco era SIEMPRE null y los
                    // ataques combinados JAMÁS disparaban. Ahora: solo vivos
                    // — EsObjetivo hace el resto (ni amistosos ni intocables).
                    if (n == null || !n.active) continue;
                    if (!VFXCore.EsObjetivo(n)) continue;
                    float d = n.Center.DistanceSQ(p.Center);
                    if (d < mejorD) { mejorD = d; blanco = n; }
                }
                if (blanco == null) return;

                ulong ahora = Main.GameUpdateCount;

                // === v6.61/v6.62 — EL SILENCIO DE LA IRA Y SU COLA (la
                //     letra v6.61: «mientras se esta creando el haz o
                //     mientras el haz se esta disparando no pueden
                //     crearce ni agujeros negros runicos, ni soles
                //     runicos» + la letra v6.62: «hasta que el haz se
                //     acabe y hayan pasado al menos 5 segundos de
                //     acabado el haz»): mientras la ceremonia vive (la
                //     creación) o el haz de la ira dispara, los OTROS
                //     conjuros del escuadrón ESPERAN — y sus relojes NO
                //     se consumen (cada uno retoma SU turno cuando la
                //     ira termina). RitualDe encuentra EXACTAMENTE eso:
                //     la ceremonia viva… o el haz de
                //     la ira disparando (v6.60.2 — el ancla del círculo).
                //     Y mientras vive, LA COLA se empuja siempre adelante:
                //     muerto el haz, la llave guarda 315 ticks más — el
                //     silencio cubre TODO el ciclo y su resaca. ===
                bool iraViva = IraSerExteriorCeremoniaProjectile.RitualDe(p.whoAmI) != null;
                if (iraViva)
                    _iraColaHasta[p.whoAmI] = ahora + IRA_COLA_TICKS;
                bool iraEnCola = _iraColaHasta.TryGetValue(p.whoAmI, out ulong hastaCola)
                    && ahora < hastaCola;

                if (!iraViva && !iraEnCola)
                {
                    // === EL SOL RÚNICO (2+ ojos mejorados, nivel 7+) ===
                    int solesMin = DespertarGrimorio.SolesPorMinuto(despertar);
                    if (solesMin > 0 && mejorados.Count >= 2)
                    {
                        ulong hasta = _solHasta.TryGetValue(p.whoAmI, out ulong v) ? v : 0ul;
                        if (ahora >= hasta && Main.rand.Next(DADO) == 0)
                        {
                            _solHasta[p.whoAmI] = ahora + 3600ul / (ulong)solesMin;
                            DispararElSol(p, blanco, mejorados, despertar);
                        }
                    }

                    // === LOS AGUJEROS NEGROS (5+ ojos mejorados, nivel 10+) ===
                    int agujerosMin = DespertarGrimorio.AgujerosPorMinuto(despertar);
                    if (agujerosMin > 0 && mejorados.Count >= 5)
                    {
                        ulong hasta = _agujeroHasta.TryGetValue(p.whoAmI, out ulong v) ? v : 0ul;
                        if (ahora >= hasta && Main.rand.Next(DADO) == 0)
                        {
                            _agujeroHasta[p.whoAmI] = ahora + 3600ul / (ulong)agujerosMin;
                            LanzarElAgujero(p, blanco, mejorados, despertar);
                        }
                    }

                    // === EL SUPREMO (8+ ojos en su MEJORA 4, nivel 45+) ===
                    if (DespertarGrimorio.SupremoDisponible(despertar) && mejoras4 >= 8)
                    {
                        ulong hasta = _supremoHasta.TryGetValue(p.whoAmI, out ulong v) ? v : 0ul;
                        if (ahora >= hasta && Main.rand.Next(DADO) == 0)
                        {
                            _supremoHasta[p.whoAmI] = ahora + 3600ul;   // 1/min SIEMPRE
                            LanzarElSupremo(p, blanco, mejorados, despertar);
                        }
                    }
                }

                // === v6.59 — LA IRA DE UN SER EXTERIOR (DESPERTAR 50: el
                //     ataque que consigue el grimorio al final del camino)
                //     — EL ESCUADRÓN ENTERO cierra el círculo, reza sus
                //     rayos de perlin al centro, y de la bruma blanco-morada
                //     y los círculos rúnicos nace EL HAZ (el rayo C — EL
                //     SUEÑO) apuntando AL CURSOR del dueño y siguiéndolo
                //     TODO el lanzamiento (v6.60.2 — la letra nueva).
                //     Daño: base 1000 + la cadena mágica del grimorio +
                //     el 50 % del daño total de invocación.
                //     v6.60 — LA LETRA NUEVA: ONCE ojos y TODOS en su
                //     MEJORA MÁXIMA (el requisito), y el compás lo marca
                //     el MODO PRUEBA (5 haces/min) hasta que la prueba
                //     termine (1 cada 30 min). ===
                if (DespertarGrimorio.IraDisponible(despertar))
                {
                    ulong hasta = _iraHasta.TryGetValue(p.whoAmI, out ulong v) ? v : 0ul;
                    // v6.60 — SIN DADO: la ira es EL ATAQUE AGENDADO del
                    // libro — su reloj ES el compás (el dado era el eco
                    // del patrón aleatorio del sol y los agujeros; con el
                    // compás de 5/min desfasaba el conteo a ~4,6/min).
                    if (ahora >= hasta)
                    {
                        // NADA DE HACES SOLAPADOS (el RC2/C del portador —
                        // SU arma): en EL COMPÁS FINAL la ira ESPERA el
                        // próximo reloj si un haz vive; EN LA PRUEBA la
                        // regla se suspende — el ciclo completo (450 de
                        // ceremonia + 800 de haz = 1.250 ticks) sería un
                        // cuello de botella de ~2,9/min y la letra pide
                        // 5/min: la ceremonia nueva arranca mientras el
                        // haz viejo vive sus últimos 80 ticks (dos haces
                        // 80 ticks, sí — el dibujado los lleva TODOS:
                        // HazSobreElMundoSistema escanea la lista entera).
                        int tipoHaz = ModContent.ProjectileType<RayosPruebaProjectile>();
                        bool hazVivo = false;
                        if (!IRA_MODO_PRUEBA)
                        {
                            for (int i = 0; i < Main.maxProjectiles; i++)
                            {
                                Projectile ph = Main.projectile[i];
                                if (ph != null && ph.active && ph.type == tipoHaz &&
                                    ph.owner == p.whoAmI)
                                {
                                    hazVivo = true;
                                    break;
                                }
                            }
                        }
                        // v6.60 — EL ESCUADRÓN DE LA IRA (la letra nueva,
                        //     exacta): ONCE ojos vivos — el escuadrón
                        //     ENTERO, el tope del Eterno ES 11 — y TODOS
                        //     en su MEJORA MÁXIMA: «si hay menos de 11
                        //     minions o no están mejorados al máximo
                        //     entonces no pueden lanzar la ira del ser
                        //     exterior» — un solo ojo sin su última
                        //     mejora y el círculo NO se cierra.
                        int ojosVivos = 0;
                        int ojosAlMax = 0;
                        if (!hazVivo)
                        {
                            for (int i = 0; i < Main.maxProjectiles; i++)
                            {
                                Projectile pr = Main.projectile[i];
                                if (pr == null || !pr.active || pr.type != tipoOjo) continue;
                                if (pr.owner != p.whoAmI) continue;
                                ojosVivos++;
                                if (global::AethonMod.Content.Projectiles.OjoEternoMinion
                                        .MejorasDe(pr) >= global::AethonMod.Content.Projectiles
                                        .OjoEternoMinion.MejorasMax)
                                    ojosAlMax++;
                            }
                        }
                        if (!hazVivo && ojosVivos >= IRA_OJOS_MINIMOS &&
                            ojosVivos == ojosAlMax)
                        {
                            _iraHasta[p.whoAmI] = ahora + (IRA_MODO_PRUEBA
                                ? IRA_ENFRIAMIENTO_PRUEBA : IRA_ENFRIAMIENTO_FINAL);
                            DispararLaIra(p, mejorados, despertar);
                        }
                    }
                }
            }
            catch { }
        }

        // ================================================================
        //  v6.51.8 — LOS CELEBRANTES DEL NACIMIENTO (la letra: «en el
        //  nacimiento de ambos [el sol y los agujeros] los ojos
        //  correspondientes y de forma aleatoria, deben disparar su rayo
        //  de perlin al punto central del nacimiento siendo de ahi donde
        //  nace el sol runico y los agujeros negros runicos, siempre
        //  teniendo en cuenta el numero de minion necesario para el
        //  nacimiento de cada uno»): EL SOL pide 2 ojos mejorados · EL
        //  AGUJERO 5 · EL SUPREMO 8 (en su mejora 4) — elegidos AL AZAR
        //  entre los elegibles, y cada uno dispara SU RAYO DE PERLIN al
        //  PUNTO CENTRAL del ritual (el rayo sostenido del modo ritual
        //  del TruenoPerlin — de ahí nace todo).
        // ================================================================

        /// <summary>EL MÁS DESPIERTO del escuadrón (quien mide el daño
        /// del conjuro — el azar elige quiénes REZAN, no cuánto pega).</summary>
        private static Projectile ElMasDespierto(List<Projectile> ojos)
        {
            Projectile mejor = null;
            int mejorM = -1;
            foreach (Projectile ojo in ojos)
            {
                if (ojo == null || !ojo.active) continue;
                int m = global::AethonMod.Content.Projectiles.OjoEternoMinion.MejorasDe(ojo);
                if (m > mejorM) { mejorM = m; mejor = ojo; }
            }
            return mejor;
        }

        /// <summary>CUÁNTOS ojos AL AZAR (Fisher-Yates sobre una copia —
        /// la letra: «de forma aleatoria»).</summary>
        private static List<Projectile> ElegirAlAzar(List<Projectile> piscina, int cuantos)
        {
            var copia = new List<Projectile>(piscina);
            for (int i = copia.Count - 1; i > 0; i--)
            {
                int j = Main.rand.Next(i + 1);
                Projectile tmp = copia[i]; copia[i] = copia[j]; copia[j] = tmp;
            }
            int n = Math.Min(cuantos, copia.Count);
            return copia.GetRange(0, n);
        }

        /// <summary>LOS CELEBRANTES REZAN: cada ojo elegido dispara SU
        /// RAYO DE PERLIN al PUNTO CENTRAL del ritual (el MODO RITUAL del
        /// TruenoPerlin: sostenido TODO el parto, de la córnea del ojo al
        /// punto donde el sol o el agujero nacerá — sin golpe: es la
        /// reverencia del nacimiento, no un ataque).</summary>
        private static void LosCelebrantesRezan(List<Projectile> celebrantes, Projectile ritual)
        {
            if (ritual == null || !ritual.active) return;
            foreach (Projectile ojo in celebrantes)
            {
                if (ojo == null || !ojo.active) continue;
                Projectile.NewProjectile(ojo.GetSource_FromAI(),
                    ritual.Center, Vector2.Zero,
                    ModContent.ProjectileType
                        <global::AethonMod.Content.Projectiles.Cosmic.TruenoPerlinProjectile>(),
                    0, 0f, ritual.owner,
                    2f,                  // EL MODO: RITUAL (sostenido, sin golpe)
                    ojo.whoAmI * 1f,     // la boca: ESTE ojo
                    ritual.whoAmI * 1f); // el ancla: EL PUNTO CENTRAL del ritual
            }
        }

        // ================================================================
        //  EL SOL RÚNICO COMBINADO
        // ================================================================

        /// <summary>LA CEREMONIA DEL SOL (v6.51.2 — LA LETRA: «para
        /// los soles rúnicos el rayo convergente tome 2 SEGUNDOS en
        /// crearlo y lanzarlo al enemigo… deben aparecer encima del
        /// jugador»): DOS ojos mejorados — AL AZAR entre el escuadrón
        /// (v6.51.8: la letra de los celebrantes) — disparan SUS RAYOS
        /// DE PERLIN al PUNTO CENTRAL del ritual y ENCIMA DEL JUGADOR
        /// nace EL RAYO CONVERGENTE — 2 segundos de rayos que convergen
        /// y CREAN el sol — y al cumplir, el sol sale DISPARADO al
        /// enemigo (su homing hace el resto: daño de área, quemadura y
        /// la nova final).</summary>
        private static void DispararElSol(Player p, NPC blanco,
            List<Projectile> mejorados, int despertar)
        {
            try
            {
                // v6.51.8 — LOS DOS CELEBRANTES, AL AZAR (la letra: «los
                // ojos correspondientes y de forma aleatoria… siempre
                // teniendo en cuenta el numero de minion necesario para
                // el nacimiento de cada uno»: el sol pide DOS).
                var celebrantes = ElegirAlAzar(mejorados, 2);

                // EL DAÑO LO MIDE EL MÁS DESPIERTO (el azar elige quiénes
                // rezan, no cuánto pega el conjuro — el patrón de siempre).
                Projectile lider = ElMasDespierto(mejorados);
                if (lider == null) return;
                int nivelOjo = (int)MathF.Floor(lider.ai[2]);
                float multMejoras = global::AethonMod.Content.Projectiles.OjoEternoMinion
                    .MultMejoras(lider);
                int danoRayo = Math.Max(1, (int)(lider.damage *
                    WeaponScaling.MinionContactDamageMult(nivelOjo) * multMejoras));

                // EL DAÑO DEL SOL QUE NACERÁ (viaja en ai[2] del ritual):
                // la combinación ×3… y CADA ANILLO EXTRA LO AUMENTA (la
                // letra de la .51.9: «con cada anillo runico extra del sol
                // runico este aumenta su daño tambien» — +50 % por anillo
                // de más: ×3 con 1 · ×4,5 con 2 · ×6 con 3). El DESPERTAR
                // ya viaja DENTRO del daño del ojo líder (se hornea al
                // invocarlo — DanoOjosDelEterno: cuenta UNA vez), y el
                // ×1,25 del 50 se cobra UNA vez vía Bonus50Vivo (la
                // auditoría de la .51.9: antes el despertar se multiplicaba
                // DOS veces aquí).
                int anillos = DespertarGrimorio.AnillosDelSol(despertar);
                int danoSol = Math.Max(1, (int)(danoRayo * 3f *
                    (1f + 0.5f * (anillos - 1)) *
                    global::AethonMod.Content.Projectiles.OjoEternoMinion.Bonus50Vivo(lider)));

                // EL RAYO CONVERGENTE: 2 SEGUNDOS de ceremonia ENCIMA DEL
                // JUGADOR — al cumplir, EL SOL nace EN SU PUNTO CENTRAL
                // (donde los celebrantes clavaron sus rayos) y sale al
                // enemigo.
                Projectile ritual = Projectile.NewProjectileDirect(
                    p.GetSource_Misc("SolCombinado"),
                    p.Center + new Vector2(0f, -150f), Vector2.Zero,
                    ModContent.ProjectileType
                        <global::AethonMod.Content.Projectiles.Cosmic.RayoConvergenteProjectile>(),
                    0, 0f, p.whoAmI,
                    1f,                          // EL MODO: SOL
                    blanco.whoAmI * 1f,          // el blanco elegido
                    danoSol * 1f);               // el daño combinado

                // LOS CELEBRANTES REZAN AL CENTRO (la letra de la .51.8):
                // sus rayos de perlin SOSTENIDOS todo el ritual — de sus
                // córneas al punto donde el sol nacerá.
                LosCelebrantesRezan(celebrantes, ritual);

                // EL BRILLO DE LOS ELEGIDOS (la reverencia los marca).
                foreach (Projectile ojo in celebrantes)
                {
                    if (ojo == null || !ojo.active) continue;
                    for (int k = 0; k < 10; k++)
                    {
                        Dust d = Dust.NewDustPerfect(ojo.Center, DustID.GoldFlame,
                            Main.rand.NextVector2Circular(3.5f, 3.5f), 128, default, 1.2f);
                        d.noGravity = true;
                    }
                }

                // v6.62 — LA VOZ ORIGINAL DEL NACIMIENTO (la letra: «el
                //     sonido que se usa al crear los agujeros negros
                //     runicos y los soles runicos por los minions, ese
                //     sonido original de terraria es horrible comparado
                //     con el sonido del haz, debes crear un sonido
                //     original para cuando se crea un agujero negro
                //     runico y un sol runico por los minions»): el
                //     Item122 de Terraria MUERE en los TRES nacimientos
                //     — el sol enciende con RunaNaceSol (el brillo
                //     ascendente) y las puertas (agujero y supremo)
                //     abren con RunaNaceVacio (el pozo que cae). Sintet
                //     izados por la casa (Content/Sounds): voz propia.
                Terraria.Audio.SoundEngine.PlaySound(
                    new Terraria.Audio.SoundStyle(
                        "AethonMod/Content/Sounds/RunaNaceSol",
                        Terraria.Audio.SoundType.Sound)
                    { MaxInstances = 2, Volume = 0.95f },
                    p.Center);
            }
            catch { }
        }

        // ================================================================
        //  LOS AGUJEROS NEGROS COMBINADOS
        // ================================================================

        /// <summary>EL RITUAL DEL AGUJERO (v6.51.2 — LA LETRA: «para
        /// los agujeros negros rúnicos tome 3 SEGUNDOS»): con 5+ ojos
        /// mejorados, CINCO de ellos — AL AZAR (v6.51.8: la letra de los
        /// celebrantes) — disparan SUS RAYOS DE PERLIN al PUNTO CENTRAL
        /// del ritual y ENCIMA DEL JUGADOR nace EL RAYO CONVERGENTE de 3
        /// segundos — y al cumplir, uno de los CUATRO RÚNICOS al azar
        /// (nivel 25+: su versión ASCENDIDA) nace EN ESE PUNTO CENTRAL.
        /// EL DAÑO nace de los ojos combinados.</summary>
        private static void LanzarElAgujero(Player p, NPC blanco,
            List<Projectile> mejorados, int despertar)
        {
            try
            {
                bool ascendidos = DespertarGrimorio.AgujerosAscendidos(despertar);

                // v6.51.8 — LOS CINCO CELEBRANTES, AL AZAR (la letra: el
                // agujero pide CINCO ojos mejorados — los que el azar
                // elija del escuadrón).
                var celebrantes = ElegirAlAzar(mejorados, 5);

                // EL DAÑO COMBINADO: el rayo del ojo más despierto × los
                // celebrantes que abren la puerta (×1,45 en ascendidos).
                // v6.51.9 — LA AUDITORÍA: el DESPERTAR ya vive dentro del
                // daño del ojo líder (horneado al invocarlo — DanoOjos
                // DelEterno) y NO se vuelve a multiplicar aquí; el ×1,25
                // del 50 se cobra UNA vez vía Bonus50Vivo.
                Projectile lider = ElMasDespierto(mejorados);
                if (lider == null) return;
                int nivelOjo = (int)MathF.Floor(lider.ai[2]);
                float multMejoras = global::AethonMod.Content.Projectiles.OjoEternoMinion
                    .MultMejoras(lider);
                int danoRayo = Math.Max(1, (int)(lider.damage *
                    WeaponScaling.MinionContactDamageMult(nivelOjo) * multMejoras));
                int dano = Math.Max(1, (int)(danoRayo * (ascendidos ? 4.5f : 3.2f) *
                    global::AethonMod.Content.Projectiles.OjoEternoMinion.Bonus50Vivo(lider)));

                // EL RAYO CONVERGENTE: 3 SEGUNDOS de ceremonia — al
                // cumplir, la puerta (el agujero rúnico al azar — los
                // ascendidos si el despertar los trajo) se abre EN EL
                // PUNTO CENTRAL donde los celebrantes clavaron sus rayos
                // (la elección del tipo la hace el ritual al cumplir: es
                // UN AZAR de cada lanzamiento, como manda la letra).
                Projectile ritual = Projectile.NewProjectileDirect(
                    p.GetSource_Misc("AgujeroCombinado"),
                    p.Center + new Vector2(0f, -150f), Vector2.Zero,
                    ModContent.ProjectileType
                        <global::AethonMod.Content.Projectiles.Cosmic.RayoConvergenteProjectile>(),
                    0, 0f, p.whoAmI,
                    2f,                          // EL MODO: AGUJERO
                    blanco.whoAmI * 1f,
                    dano * 1f);

                // LOS CELEBRANTES REZAN AL CENTRO (la letra de la .51.8).
                LosCelebrantesRezan(celebrantes, ritual);

                // EL TEATRO DE LA APERTURA (las chispas de los elegidos).
                foreach (Projectile ojo in celebrantes)
                {
                    if (ojo == null || !ojo.active) continue;
                    Dust d = Dust.NewDustPerfect(ojo.Center, DustID.Shadowflame,
                        Main.rand.NextVector2Circular(2.5f, 2.5f), 140,
                        new Color(140, 60, 210), 1f);
                    d.noGravity = true;
                }
                // v6.62 — LA VOZ ORIGINAL: RunaNaceVacio — el pozo que
                //     cae (ver el comentario de DispararElSol).
                Terraria.Audio.SoundEngine.PlaySound(
                    new Terraria.Audio.SoundStyle(
                        "AethonMod/Content/Sounds/RunaNaceVacio",
                        Terraria.Audio.SoundType.Sound)
                    { MaxInstances = 2, Volume = 0.95f },
                    p.Center);
            }
            catch { }
        }

        /// <summary>EL SUPREMO (nivel 45+): «un nuevo tipo de agujero negro
        /// runico independiente… Agujero Negro Supremo y Agujero Supremo
        /// Aurora… su daño es aún mayor que el de los agujeros negros
        /// ascendidos y se necesitan el ataque combinado de 8 minions o
        /// más en su mejora número 4» — uno por minuto, al azar entre los
        /// DOS. v6.51.2: SU RITUAL TAMBIÉN (el rayo convergente de 3
        /// segundos — la carta fuerte se anuncia). v6.51.8: OCHO ojos en
        /// su mejora 4 — AL AZAR — disparan SUS RAYOS al PUNTO CENTRAL
        /// (la letra de los celebrantes) y el SUPREMO/AURORA nace ahí.</summary>
        private static void LanzarElSupremo(Player p, NPC blanco,
            List<Projectile> mejorados, int despertar)
        {
            try
            {
                // v6.51.8 — LA PISCINA DEL SUPREMO: solo los ojos en SU
                // MEJORA 4 (la letra: «el ataque combinado de 8 minions
                // o más en su mejora número 4») — y OCHO de ellos, AL
                // AZAR, son los celebrantes.
                var enLaCuarta = new List<Projectile>();
                foreach (Projectile ojo in mejorados)
                {
                    if (ojo == null || !ojo.active) continue;
                    if (global::AethonMod.Content.Projectiles.OjoEternoMinion
                            .MejorasDe(ojo) >= 4)
                        enLaCuarta.Add(ojo);
                }
                var celebrantes = ElegirAlAzar(enLaCuarta, 8);

                Projectile lider = ElMasDespierto(enLaCuarta);
                if (lider == null) return;
                int nivelOjo = (int)MathF.Floor(lider.ai[2]);
                float multMejoras = global::AethonMod.Content.Projectiles.OjoEternoMinion
                    .MultMejoras(lider);
                int danoRayo = Math.Max(1, (int)(lider.damage *
                    WeaponScaling.MinionContactDamageMult(nivelOjo) * multMejoras));
                // v6.51.9 — LA AUDITORÍA: igual que el sol y los agujeros —
                // el despertar vive DENTRO del daño del líder (horneado) y
                // el ×1,25 del 50 se cobra UNA vez vía Bonus50Vivo.
                int dano = Math.Max(1, (int)(danoRayo * 6.5f *
                    global::AethonMod.Content.Projectiles.OjoEternoMinion.Bonus50Vivo(lider)));

                // EL RAYO CONVERGENTE DEL SUPREMO: 3 segundos — al cumplir,
                // el SUPREMO o el AURORA (al azar) se abre EN EL PUNTO
                // CENTRAL donde los ocho clavaron sus rayos.
                Projectile ritual = Projectile.NewProjectileDirect(
                    p.GetSource_Misc("SupremoCombinado"),
                    p.Center + new Vector2(0f, -150f), Vector2.Zero,
                    ModContent.ProjectileType
                        <global::AethonMod.Content.Projectiles.Cosmic.RayoConvergenteProjectile>(),
                    0, 0f, p.whoAmI,
                    3f,                          // EL MODO: SUPREMO
                    blanco.whoAmI * 1f,
                    dano * 1f);

                // LOS OCHO REZAN AL CENTRO (la letra de la .51.8).
                LosCelebrantesRezan(celebrantes, ritual);

                // LA SOLEMNIDAD: los elegidos estallan (la carta fuerte
                // del grimorio despierto).
                foreach (Projectile ojo in celebrantes)
                {
                    if (ojo == null || !ojo.active) continue;
                    for (int k = 0; k < 6; k++)
                    {
                        Dust d = Dust.NewDustPerfect(ojo.Center, DustID.PurpleTorch,
                            Main.rand.NextVector2Circular(3.5f, 3.5f), 150,
                            new Color(190, 130, 255), 1.2f);
                        d.noGravity = true;
                    }
                }
                // v6.62 — LA VOZ ORIGINAL: RunaNaceVacio, una octava
                //     más abajo — el supremo es la puerta más honda.
                Terraria.Audio.SoundEngine.PlaySound(
                    new Terraria.Audio.SoundStyle(
                        "AethonMod/Content/Sounds/RunaNaceVacio",
                        Terraria.Audio.SoundType.Sound)
                    { MaxInstances = 2, Volume = 1f, Pitch = -0.25f },
                    p.Center);
                // v6.51.8 — EL RUGIDO MURIÓ (la letra: «al parecer los 2
                //     agujeros negros runicos supremos hace ruido de jefes
                //     al aparecer, bueno no deben hacer ningun ruido, deben
                //     ser como los otros»): el SoundID.Roar de la apertura
                //     del ritual supremo MUERE — silenciosos como los otros.
            }
            catch { }
        }

        // ================================================================
        //  v6.59 — LA IRA DE UN SER EXTERIOR (el despertar 50)
        // ================================================================

        /// <summary>LA CEREMONIA DE LA IRA: nace el ritual (el proyectil
        /// que cierra el círculo del escuadrón, recibe los rayos de perlin
        /// y gesta la bruma blanco-morada + los círculos rúnicos) con el
        /// blanco elegido y EL DAÑO DEL HAZ calculado AQUÍ — al cumplir
        /// sus 450 ticks, EL engendra EL HAZ (el rayo C — EL SUEÑO) ya
        /// disparando. La letra: «el daño base del rayo que es 1000, mas
        /// el calculo correspondiente para el aumento de daño magico, mas
        /// el 50% del total del daño de invocacion».</summary>
        private static void DispararLaIra(Player p, List<Projectile> mejorados, int despertar)
        {
            try
            {
                // v6.60.2 — EL BLANCO ES SOLO EL GATILLO: la ira SE
                // DISPARA cuando el escuadrón tiene a quién cazar (un
                // enemigo de verdad cerca — jefes primero, la ley de
                // BuscarBlancoIra), pero el haz YA NO lo persigue: se
                // APUNTA con el cursor del dueño y lo sigue TODO el
                // lanzamiento (la letra nueva).
                NPC blanco = RayosPruebaProjectile.BuscarBlancoIra(p.Center, 2400f);
                if (blanco == null) return;

                // EL DAÑO (la letra, exacta): base 1000 + cadena mágica +
                // 50 % del daño total de invocación.
                Projectile lider = ElMasDespierto(mejorados);
                if (lider == null) return;
                int nivel = (int)MathF.Floor(lider.ai[2]);
                int dano = DanoDeLaIra(p, nivel, lider, despertar);

                // LA CEREMONIA: el ritual nace sobre el jugador (o el libro
                // si está fuera cazando — el centro lo decide ella) — el
                // haz saldrá de SU centro al cumplir.
                Projectile.NewProjectile(p.GetSource_Misc("IraSerExterior"),
                    p.Center, Vector2.Zero,
                    ModContent.ProjectileType<IraSerExteriorCeremoniaProjectile>(),
                    0, 0f, p.whoAmI,
                    0f,                   // ai[0]: LIBRE (v6.60.2: el rumbo es del cursor — el blanco murió con la mira)
                    dano * 1f,            // ai[1]: EL DAÑO DEL HAZ
                    0f);                  // ai[2]: la edad (la llena su IA)

                // LA SOLEMNIDAD: el escuadrón entero estalla en polvo
                // blanco-morado — el círculo empieza.
                int tipoOjoIra = ModContent.ProjectileType
                    <global::AethonMod.Content.Projectiles.OjoEternoMinion>();
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile pr = Main.projectile[i];
                    if (pr == null || !pr.active || pr.type != tipoOjoIra) continue;
                    if (pr.owner != p.whoAmI) continue;
                    for (int k = 0; k < 5; k++)
                    {
                        bool blanca = Main.rand.NextBool(2);
                        Dust d = Dust.NewDustPerfect(pr.Center,
                            blanca ? DustID.PurpleTorch : DustID.Shadowflame,
                            Main.rand.NextVector2Circular(3.5f, 3.5f), 150,
                            blanca ? new Color(235, 225, 255) : new Color(150, 90, 220), 1.1f);
                        d.noGravity = true;
                    }
                }

                // v6.61 — LA VOZ DEL EXTERIOR MURIÓ (la letra: «la
                //     creacion del haz no necesita sonidos de terraria,
                //     ya tiene su propio sonido»): el Item122 de
                //     Terraria calla — la ira YA tiene su voz (la carga
                //     del RC2 a los 150 ticks de la ceremonia y su bucle
                //     de disparo). El nacimiento del ritual es MUDO:
                //     solo el cine de los ojos y el polvo blanco-morado.
            }
            catch { }
        }

        /// <summary>EL DAÑO DE LA IRA (la letra, EXACTA): «el daño base
        /// del rayo que es 1000, mas el calculo correspondiente para el
        /// aumento de daño magico, mas el 50% del total del daño de
        /// invocacion, ese total es lo que se obtiene al terminar todos
        /// los calculos».
        ///
        /// (1) LA PARTE MÁGICA — la cadena EXACTA del clic izquierdo del
        ///     Eterno (GrimoireEternal.ModifyWeaponDamage, multiplicador
        ///     por multiplicador) aplicada sobre la BASE 1000: el nivel
        ///     (+2,2 %/nivel — MagicDamageMult), el bonus de maná bajo
        ///     (LowManaDamageMult), el festín de armas
        ///     (BonusDanoArmasDevoradas), el despertar (+1 %/nivel —
        ///     BonusDano), la reforja viva (la columna Daño del peldaño)
        ///     y las esencias del portador vestidas por la presencia del
        ///     libro (BonusDanoEsencias × FactorDelLibro — como DanoOjos
        ///     DelEterno).
        /// (2) LA PARTE DE INVOACIÓN — el 50 % del daño TOTAL del
        ///     escuadrón: el rayo del ojo MÁS DESPIERTO (el patrón de la
        ///     casa — lider.damage YA trae SU cadena horneada: la suya
        ///     propia de invocación +1,5 %/nivel, el despertar, lo
        ///     devorado, la reforja, las esencias y el ×1,25 del 50)
        ///     escalado por MinionContactDamageMult y SUS mejoras — el
        ///     daño completo del ataque de invocación, al 50 %.</summary>
        private static int DanoDeLaIra(Player p, int nivel, Projectile lider, int despertar)
        {
            // (1) LA CADENA MÁGICA DEL GRIMORIO sobre la base 1000.
            float magica = 1000f
                * WeaponScaling.MagicDamageMult(Math.Max(0, nivel))
                * WeaponScaling.LowManaDamageMult(p.statMana, p.statManaMax2)
                * (1f + Players.ShardPlayer.BonusDanoArmasDevoradas(p))
                * (1f + DespertarGrimorio.BonusDano(despertar))
                * (1f + Content.Weapons.GrimoireEternal.PeldanoDe(Math.Max(1, nivel)).Dano)
                * (1f + Players.ShardPlayer.BonusDanoEsencias(p)
                    * Content.Globals.AlmaEternoGlobalItem.FactorDelLibro(p));

            // (2) EL 50 % DEL DAÑO TOTAL DE INVOACIÓN (el rayo del ojo más
            //     despierto — el ataque del escuadrón, completo).
            float multMejoras = global::AethonMod.Content.Projectiles.OjoEternoMinion
                .MultMejoras(lider);
            float danoOjo = lider.damage
                * WeaponScaling.MinionContactDamageMult(nivel) * multMejoras;

            return Math.Max(1, (int)(magica + danoOjo * 0.5f));
        }
    }
}
