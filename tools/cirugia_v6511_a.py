#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# ======================================================================
# cirugia_v6511_a.py — LOS BUGS DE JUEGO:
#  (1) AtaquesCombinados: el blanco SKIPEABA a los enemigos (¡invertido!)
#  (2) Hadas menos frecuentes (25 s la primera moneda, cada 45 s, 22%,
#      garantizada a la 5ª)
#  (3) La llegada 2 grimorios: spawn EN el punto + tolerancias anchas +
#      guarda anti-doble
#  (4) El ESCUADRÓN: filas de 5 alrededor del libro (el sobrante en su
#      propia fila, centrado)
#  (5) La esencia devorada (+0,1% c/u) — se puede seguir devorando
#      SIEMPRE (también a despertar 50)
#  (6) El festín de armas ya no infla los ojos (la corona muere)
# ======================================================================
import io, os

RAIZ = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod"
def leer(r): return io.open(os.path.join(RAIZ, r), encoding="utf-8").read()
def escribir(r, s): io.open(os.path.join(RAIZ, r), "w", encoding="utf-8").write(s)
def rep(s, v, n, ruta, veces=1):
    k = s.count(v)
    assert k == veces, f"[{ruta}] {v[:64]!r}: esperado {veces}, hay {k}"
    return s.replace(v, n)

# ======================================================================
# (1) EL BLANCO INVERTIDO (¡el bug de los ataques que no salían!)
# ======================================================================
R = "Content/Systems/AtaquesCombinadosSistema.cs"
s = leer(R)
s = rep(s,
"""                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n == null || !n.active || !n.friendly) continue;
                    if (!VFXCore.EsObjetivo(n)) continue;""",
"""                for (int i = 0; i < Main.maxNPCs; i++)
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
                    if (!VFXCore.EsObjetivo(n)) continue;""", R)
escribir(R, s)
print("OK — (1) el blanco de los ataques combinados")

# ======================================================================
# (2) LAS HADAS MENOS FRECUENTES
# ======================================================================
R = "Content/Projectiles/Cosmetic/AethonMenorPareja.cs"
s = leer(R)
s = rep(s,
"""                        // LA MONEDA DE LA HORDA: 4 s tras arrancar la
                        // curiosidad y cada 20 s de ella — 35% de plaga,
                        // GARANTIZADA a la tercera tirada fallida (la
                        // cura de «no veo que se active»: la .128 pedía un
                        // minuto EXACTO para UNA moneda del 30% que, si
                        // fallaba, la misión de vecinos se comía el reloj).
                        if (e.TicksEnModo == 240 ||
                            (e.TicksEnModo > 240 && (e.TicksEnModo - 240) % 1200 == 0))
                        {
                            if (e.PlagaEnfriamiento > 0)
                                e.PlagaEnfriamiento--;
                            else if (e.PlagaTiradasFallidas >= 2 ||
                                     VFXCore.Hash01(p.whoAmI,
                                         e.TotalGolpes * 13 + 29,
                                         e.Viajes * 19 + 7 + e.TicksQuieto * 131)
                                     < PLAGA_PROBABILIDAD)""",
"""                        // v6.51.1 — LA MONEDA PACIENTE (la letra: «el
                        // evento de las hadas es muy seguido, que demore
                        // más en aparecer»): la PRIMERA tirada espera 25 s
                        // de curiosidad, luego tira cada 45 s — 22% y
                        // GARANTIZADA a la quinta tirada fallida (peor
                        // caso ~3,4 min: las hadas son una visita RARA,
                        // no una inquilina).
                        if (e.TicksEnModo == 1500 ||
                            (e.TicksEnModo > 1500 && (e.TicksEnModo - 1500) % 2700 == 0))
                        {
                            if (e.PlagaEnfriamiento > 0)
                                e.PlagaEnfriamiento--;
                            else if (e.PlagaTiradasFallidas >= 4 ||
                                     VFXCore.Hash01(p.whoAmI,
                                         e.TotalGolpes * 13 + 29,
                                         e.Viajes * 19 + 7 + e.TicksQuieto * 131)
                                     < PLAGA_PROBABILIDAD)""", R)
s = rep(s,
"""        /// <summary>La moneda del easteregg: 35% plaga por tirada (por
        /// episodio de curiosidad — el espanto es RARO, pero la letra
        /// manda: el usuario TIENE QUE VERLO).</summary>
        private const float PLAGA_PROBABILIDAD = 0.35f;""",
"""        /// <summary>v6.51.1 — La moneda del easteregg: 22% plaga por
        /// tirada (la letra nueva: «que demore más en aparecer» — las
        /// hadas visitan, no se mudan a vivir).</summary>
        private const float PLAGA_PROBABILIDAD = 0.22f;""", R)
escribir(R, s)
print("OK — (2) hadas paciente")

# ======================================================================
# (3) LA LLEGADA: UN SOLO GRIMORIO
# ======================================================================
R = "Content/Systems/LaCaidaSistema.cs"
s = leer(R)
# (3a) tolerancias anchas (la carrera del reconciliador: el libro nace a
#      ±21-61 px del punto según la semántica de Y de NewNPC — con 64
#      justos, el bobbing de −6 lo sacaba del radar y VOLVÍA a nacer)
s = rep(s,
"""        /// <summary>El Grimorio Dormido vivo a menos de 64 px de un punto (null si nadie).</summary>
        private static NPC BuscarLibroCerca(float x, float y)
        {
            int tipo = ModContent.NPCType<NPCs.GrimorioDormido>();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || n.type != tipo) continue;
                if (Math.Abs(n.Center.X - x) <= 64f && Math.Abs(n.Center.Y - y) <= 64f)
                    return n;
            }
            return null;
        }""",
"""        /// <summary>El Grimorio Dormido vivo cerca de un punto (null si
        /// nadie). v6.51.1 — EL RADAR ANCHO (la cura de los DOS GRIMORIOS:
        /// con la tolerancia vieja de ±64 y el libro flotando a −40−21±6
        /// del punto, el bobbing lo sacaba del radar en algunos ticks — el
        /// reconciliador no lo hallaba y ENGENDRABA OTRO justo al lado:
        /// «en la llegada aparecen 2 grimorios en vez de 1». Ahora el
        /// radar lee ±96/±140: TODO el rango del bobbing, siempre).</summary>
        private static NPC BuscarLibroCerca(float x, float y)
        {
            int tipo = ModContent.NPCType<NPCs.GrimorioDormido>();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || n.type != tipo) continue;
                if (Math.Abs(n.Center.X - x) <= 96f && Math.Abs(n.Center.Y - y) <= 140f)
                    return n;
            }
            return null;
        }""", R)
# (3b) el nacimiento: EN el punto de la pendiente (donde lo busca el
#      reconciliador) + guarda anti-doble
s = rep(s,
"""                // EL LIBRO FLOTANDO EN SU BRUMA (ai[1] = el destinatario;
                // flota 40 px sobre el suelo: el puesto de la espera).
                int npcIdx = NPC.NewNPC(new EntitySource_Misc("LaLlegada"),
                    (int)destino.X, (int)destino.Y - 40,
                    ModContent.NPCType<NPCs.GrimorioDormido>(), 0, 0f, idxJugador);
                NPC libro = (npcIdx >= 0 && npcIdx < Main.maxNPCs) ? Main.npc[npcIdx] : null;""",
"""                // EL LIBRO FLOTANDO EN SU BRUMA — EXACTO EN EL PUNTO de la
                // pendiente (v6.51.1: la cura de los DOS GRIMORIOS — el
                // libro nace donde el reconciliador lo BUSCA; flota solo:
                // noGravity + noTileCollide + su bobbing propio)… y JAMÁS
                // dos: si YA vive uno cerca, el cielo no insiste.
                if (BuscarLibroCerca(destino.X, destino.Y) != null) return;
                int npcIdx = NPC.NewNPC(new EntitySource_Misc("LaLlegada"),
                    (int)destino.X, (int)destino.Y,
                    ModContent.NPCType<NPCs.GrimorioDormido>(), 0, 0f, idxJugador);
                NPC libro = (npcIdx >= 0 && npcIdx < Main.maxNPCs) ? Main.npc[npcIdx] : null;""", R)
escribir(R, s)
print("OK — (3) un solo grimorio")

# ======================================================================
# (4) EL ESCUADRÓN: FILAS DE 5
# ======================================================================
R = "Content/Projectiles/OjoEternoMinion.cs"
s = leer(R)
s = rep(s,
"""            if (libro is Vector2 centro)
            {
                // === LA ÓRBITA: el anillo k (5, 6, 7, …) GIRANDO LISO ===
                AnilloDe(idx, out int k, out int enAnillo, out int capAnillo);
                // v6.50.109: anillos más cerca del libro (46+34k — el ojo
                // es la MITAD y las órbitas se aprietan en proporción) y el
                // radio RESPIRA despacio (la formación viva del Ruinous)
                float radio = (46f + 34f * k)
                    * (1f + 0.035f * MathF.Sin(time * 0.9f + k * 1.7f));
                float fase = FaseOrbital(enAnillo, capAnillo, k);
                // el anillo casi circular (0,94): la formación giratoria
                // se lee como UNA RUEDA — sin precesión, sin tirones
                // v6.50.114 — LA PRECESIÓN (la letra: «mejora la
                // animación… alrededor del grimorio en su fase de
                // caza»): el PLANO del anillo SE INCLINA despacio
                // (±0,30 rad, ~14 s por vaivén, fase propia por anillo)
                // — la rueda viva se lee en TRES dimensiones (el eje
                // CABECEA como el de Saturno; contrarrota en anillos
                // pares/impares)
                float incl = 0.30f * MathF.Sin(time * 0.45f + k * 1.9f)
                             * (k % 2 == 0 ? 1f : -1f);
                Vector2 punto = new Vector2((float)Math.Cos(fase) * radio,
                                             (float)Math.Sin(fase) * radio * 0.94f)
                                .RotatedBy(incl);
                // la micro-deriva propia: 1,6 px en 2D, DESPACIO (5–10 s
                // por ciclo — el vaivén vivo del Forbidden Ruin)
                Vector2 deriva = new Vector2(
                    MathF.Sin(time * 0.80f + enAnillo * 2.1f),
                    MathF.Sin(time * 0.65f + enAnillo * 1.3f)) * 1.6f;
                return centro + punto + deriva;
            }""",
"""            if (libro is Vector2 centro)
            {
                // === v6.51.1 — LAS FILAS DE CINCO (la letra: «la
                //     colocación de los minions alrededor del grimorio
                //     cuando este sale por hambre no está bien… son 2
                //     filas de 5 y el minion sobrante se queda muy pegado
                //     en la segunda fila»): el escuadrón forma DOS FILAS
                //     DE 5 — una ARRIBA y una ABAJO del libro flotante —
                //     y los sobrantes (el 11º) van a filas PROPIAS más
                //     abajo, CENTRADOS y separados: nadie pegado a nadie.
                //     Las filas respiran despacio y cada ojo lleva su
                //     micro-deriva viva de siempre. ===
                int n = Math.Max(1, dueño.ownedProjectileCounts[Projectile.type]);
                int i = Math.Min(idx, n - 1);
                const int POR_FILA = 5;

                int fila = i / POR_FILA;              // 0 (arriba) · 1 (abajo) · 2+ (sobrantes)
                int enFila = i % POR_FILA;
                int enEstaFila = Math.Min(n - fila * POR_FILA, POR_FILA);
                if (enEstaFila <= 0) enEstaFila = 1;

                // LA POSICIÓN: la fila se CENTRA en el libro (los pocos de
                // una fila corta también quedan al medio).
                float paso = 42f;
                float x = (enFila - (enEstaFila - 1) * 0.5f) * paso;
                // LA ALTURA: fila 0 ENCIMA del libro · fila 1 DEBAJO · las
                // filas de sobrantes, más abajo y a su aire.
                float y = fila == 0 ? -46f : fila == 1 ? 46f : 46f + (fila - 1) * 44f;

                // LA RESPIRACIÓN DE LA FORMACIÓN: todo el escuadrón se
                // mece despacio (±3 px, filas alternadas — la bandada viva).
                float mecer = MathF.Sin(time * 0.9f + fila * 1.7f) * 3f;
                // la micro-deriva propia: 1,6 px en 2D, DESPACIO.
                Vector2 deriva = new Vector2(
                    MathF.Sin(time * 0.80f + i * 2.1f),
                    MathF.Sin(time * 0.65f + i * 1.3f)) * 1.6f;
                return centro + new Vector2(x, y + mecer) + deriva;
            }""", R)
escribir(R, s)
print("OK — (4) filas de 5")

# ======================================================================
# (5) LA ESENCIA DEVORADA: +0,1% c/u — SIEMPRE (también a despertar 50)
# ======================================================================
R = "Content/Players/ShardPlayer.cs"
s = leer(R)
s = rep(s,
"""        public static float BonusDanoDevorado(Player p)
        {
            if (p == null) return 0f;
            var sp = p.GetModPlayer<ShardPlayer>();
            if (sp == null) return 0f;
            return Math.Min(sp.DevoracionPoder, DevoracionTope) * 0.0025f;
        }""",
"""        public static float BonusDanoDevorado(Player p)
        {
            if (p == null) return 0f;
            var sp = p.GetModPlayer<ShardPlayer>();
            if (sp == null) return 0f;
            // v6.51.1 — LA LETRA: «incluso con el despertar al máximo, se
            // debe poder seguir devorando esencia corrupta y esta debe
            // aumentar el daño del grimorio del eterno en 0.1% por cada
            // esencia corrupta» — 0,1% por esencia, SIN techo práctico
            // (el tope del .128 de 300 murió con el altar).
            return Math.Min(sp.DevoracionPoder, 5000) * 0.001f;
        }""", R)
escribir(R, s)
print("OK — (5a) 0,1% por esencia")

# (5b) EL SISTEMA: la devoración de esencias SOLTADAS (con el Eterno en
#      la mano) — vive en el mismo pulso del comensal
R = "Content/Systems/DespertarGrimorioSistema.cs"
s = leer(R)
s = rep(s,
"""                    // Los platos caídos alrededor del portador.
                    for (int d = 0; d < Main.maxItems; d++)
                    {
                        Item drop = Main.item[d];
                        if (drop == null || drop.IsAir) continue;   // SOLO items caídos vivos
                        if (drop.Center.DistanceSQ(p.Center) > RADIO_DEVORAR * RADIO_DEVORAR)
                            continue;
""",
"""                    // v6.51.1 — LAS ESENCIAS SOLTADAS (la letra: «se debe
                    //     poder seguir devorando esencia corrupta»): con el
                    //     Eterno EN LA MANO, cualquier pila de Esencia
                    //     Corrupta caída a ≤200 px es un bocado INMEDIATO —
                    //     +0,1% de daño PERMANENTE por esencia (sigue
                    //     funcionando con el despertar AL MÁXIMO: el hambre
                    //     del libro no termina con los niveles).
                    int tipoEsencia = ModContent.ItemType
                        <global::AethonMod.Content.Items.Altar.ItemCorrupto>();
                    for (int d = 0; d < Main.maxItems; d++)
                    {
                        Item drop = Main.item[d];
                        if (drop == null || drop.IsAir) continue;
                        if (drop.type != tipoEsencia) continue;
                        if (drop.Center.DistanceSQ(p.Center) > RADIO_DEVORAR * RADIO_DEVORAR)
                            continue;

                        int comidas = drop.stack;
                        var spBocado = p.GetModPlayer<Players.ShardPlayer>();
                        if (spBocado != null && comidas > 0)
                        {
                            spBocado.DevoracionPoder += comidas;
                            string nombre = drop.Name;
                            drop.TurnToAir();
                            int idd = Array.IndexOf(Main.item, drop);
                            if (Main.netMode == NetmodeID.Server && idd >= 0)
                                NetMessage.SendData(MessageID.SyncItem, -1, -1, null, idd);

                            if (!Main.dedServ)
                            {
                                for (int k = 0; k < Math.Min(20, 4 + comidas); k++)
                                {
                                    Dust dd = Dust.NewDustPerfect(p.Center, DustID.Shadowflame,
                                        new Vector2(Main.rand.NextFloat(-2.4f, 2.4f),
                                            Main.rand.NextFloat(-3.2f, 0.4f)), 150,
                                        new Color(120, 40, 180), 0.95f);
                                    dd.noGravity = true;
                                }
                                Terraria.Audio.SoundEngine.PlaySound(
                                    SoundID.Item113.WithPitchOffset(-0.45f), p.Center);
                            }

                            EcoRed.AnunciarAlPortador(p, "Mods.AethonMod.Despertar.EsenciaDevorada",
                                new Color(196, 138, 255), nombre, comidas,
                                (Players.ShardPlayer.BonusDanoDevorado(p) * 100f).ToString("0.##"));
                            EcoRed.SincronizarDevoracion(p);
                        }
                    }

                    // Los platos de ARMAS caídos alrededor del portador.
                    for (int d = 0; d < Main.maxItems; d++)
                    {
                        Item drop = Main.item[d];
                        if (drop == null || drop.IsAir) continue;   // SOLO items caídos vivos
                        if (drop.type == tipoEsencia) continue;      // las esencias: su propia mesa
                        if (drop.Center.DistanceSQ(p.Center) > RADIO_DEVORAR * RADIO_DEVORAR)
                            continue;
""", R)
escribir(R, s)
print("OK — (5b) la mesa de las esencias")

print("PARTE A COMPLETA")
