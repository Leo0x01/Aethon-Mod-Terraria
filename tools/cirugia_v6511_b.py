#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# ======================================================================
# cirugia_v6511_b.py — VISUALES + TOOLTIPS + AETHON + RAREZA + BESTIARIO
#  (6) LA BRUMA DE VERDAD (SombrasLib.Bruma — la librería fBm de la casa)
#      en LA LLEGADA y EL ESCUPIDIZO (adiós ovalos planos)
#  (7) LA TERCERA VENTANA: solo ESTADÍSTICAS del escuadrón (la
#      progresión fuera; sin descripciones; +50%/mejora → +200% con 4)
#  (8) AETHON: la barra de vida (AutoloadBossHead + BossHeadSlot), las
#      PÁGINAS CUADRADAS (adiós Pixel-rects), y el loot nuevo
#  (9) LA RAREZA CÓSMICA del Eterno
#  (10) BESTIARIO: centrado (sin overrides de posición), las 10
#       estrellas negras PRIMERO
#  (11) RECETAS: sin items→esencia; el arma corrupta no se re-corrompe
#      y el aviso de sus stats en el menú
#  (12) CorruptoGlobalItem: +5% a TODAS las estadísticas (etiqueta)
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
# (6a) LA LLEGADA: LA BRUMA DE VERDAD
# ======================================================================
R = "Content/Projectiles/LlegadaBrumaProjectile.cs"
s = leer(R)
i0 = s.index("        public override bool PreDraw(ref Color lightColor)")
s = s[:i0] + '''        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;
            try
            {
                float t = Projectile.localAI[0];

                // LA INTENSIDAD DE LA ESCENA: crece hasta el pico (nadie
                // ve NADA de lo que pasa dentro) y luego se disipa (el
                // dormido asoma entre jirones).
                float vida = t < T_CRECE
                    ? MathHelper.Clamp(t / T_CRECE, 0f, 1f)
                    : MathHelper.Clamp(1f - (t - T_CRECE) / T_DISIPA, 0f, 1f);
                // LA CURVA DEL CRECIMIENTO: primero LENTA (la sospecha),
                // luego VORAZ (el mundo se cierra de golpe).
                float crecimiento = t < T_CRECE
                    ? vida * vida * (3f - 2f * vida)
                    : 1f;
                if (vida <= 0.02f) return false;

                // ============================================================
                //  v6.51.1 — LA BRUMA DE VERDAD (la letra: «la bruma se ve
                //  mal, en vez de bruma parecen óvalos casi planos»): la
                //  cura es LA LIBRERÍA DE LA CASA — SombrasLib.Bruma, los
                //  puffs de humo fBm que se DESGARRAN de verdad (los que
                //  visten al Grimorio Dormido y a las fauces). Los óvalos
                //  eran SoftGlow estirados (discos lisos); ahora cada
                //  mechón es una NUBE VIVA con su rampa de enfriamiento y
                //  su aliento violeta — un MAR de niebla de verdad.
                // ============================================================
                VFXCore.CerrarLoteSiAbierto();
                try
                {
                    float tiempo = Main.GlobalTimeWrappedHourly;
                    int semilla = Projectile.identity % 97 + 3;
                    Vector2 centro = Projectile.Center;
                    float alfa = 0.35f + 0.65f * crecimiento;

                    // EL CORAZÓN: la nube madre (gruesa, respirando).
                    SombrasLib.Bruma(centro, RADIO * (0.55f + 0.65f * crecimiento),
                        alfa, tiempo, semilla);

                    // LOS BRAZOS: cinco nubes satélite en corro alrededor
                    // (cada una SU tamaño y SU deriva — el mar no es un
                    // círculo: es una MULTITUD).
                    for (int i = 0; i < 5; i++)
                    {
                        float ang = i / 5f * MathHelper.TwoPi + tiempo * 0.11f
                            + semilla * 0.13f;
                        float lejos = (0.55f + 0.35f * ((i * 37 + semilla * 11) % 10) / 10f)
                            * RADIO * crecimiento;
                        Vector2 pos = centro + new Vector2(
                            MathF.Cos(ang) * lejos,
                            MathF.Sin(ang) * lejos * 0.66f);
                        SombrasLib.Bruma(pos,
                            RADIO * (0.38f + 0.30f * ((i * 53) % 7) / 7f) * crecimiento,
                            alfa * 0.85f, tiempo * 0.9f, semilla + i * 7 + 11);
                    }

                    // LA CÚPULA: dos nubes ALTAS (el techo del evento — la
                    // bruma que SUBE, porque lo que llega no viene del
                    // suelo).
                    for (int i = 0; i < 2; i++)
                    {
                        SombrasLib.Bruma(
                            centro + new Vector2((i == 0 ? -1f : 1f) * RADIO * 0.45f * crecimiento,
                                -RADIO * 0.40f * crecimiento),
                            RADIO * 0.5f * crecimiento, alfa * 0.7f,
                            tiempo * 1.1f, semilla + 31 + i * 13);
                    }

                    // EL VACÍO CENTRAL (lo que no deja ver): un corazón
                    // oscuro ALFA con rim violeta — sutil, la niebla manda.
                    Texture2D glow = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                    if (glow != null)
                    {
                        Vector2 posScr = centro - Main.screenPosition;
                        Main.spriteBatch.Draw(glow, posScr, null,
                            new Color(16, 7, 28) * (0.85f * crecimiento),
                            0f, glow.Size() * 0.5f,
                            new Vector2(RADIO * 1.25f, RADIO * 0.92f) * crecimiento / glow.Size(),
                            SpriteEffects.None, 0f);
                        Main.spriteBatch.Draw(glow, posScr, null,
                            new Color(70, 38, 120) * (0.16f * vida),
                            0f, glow.Size() * 0.5f,
                            new Vector2(RADIO * 1.9f, RADIO * 1.5f) * crecimiento / glow.Size(),
                            SpriteEffects.None, 0f);
                    }
                }
                catch { VFXCore.CerrarLoteSiAbierto(); }
                VFXCore.ReabrirLoteVanilla();

                return false;
            }
            catch { return false; }
        }
    }
}
'''
escribir(R, s)
print("OK — (6a) la llegada: bruma de verdad")

# ======================================================================
# (6b) EL ESCUPIDIZO: LA BRUMA DE VERDAD
# ======================================================================
R = "Content/Projectiles/Cosmetic/EscupidizoProjectile.cs"
s = leer(R)
s = rep(s,
"""                // === 1. EL MAR DE BRUMAS (la plataforma del evento:
                //     jirones oscuros + rim violeta debajo del libro). ===
                float escalaBruma = 0.7f + 0.5f * MathHelper.Clamp(t / (float)T_SUBE, 0f, 1f)
                    - 0.5f * despedida;
                for (int i = 0; i < 10; i++)
                {
                    float ang = i / 10f * MathHelper.TwoPi + tiempo * (0.5f + 0.1f * (i % 3));
                    float radio = (46f + 34f * ((i * 29) % 5) / 5f) * escalaBruma;
                    Vector2 p = pos + new Vector2(MathF.Cos(ang) * radio,
                        MathF.Sin(ang) * radio * 0.55f + 16f);
                    Color jiron = new Color(24, 10, 42) * (0.8f * alfa *
                        (0.7f + 0.3f * MathF.Sin(tiempo * 1.9f + i * 2.2f)));
                    float tam = (52f + 26f * ((i * 41) % 7) / 7f) * escalaBruma;
                    Main.spriteBatch.Draw(glow, p, null, jiron, ang,
                        glow.Size() * 0.5f,
                        new Vector2(tam, tam * 0.75f) / glow.Size(),
                        SpriteEffects.None, 0f);
                }
                Main.spriteBatch.Draw(glow, pos + new Vector2(0f, 14f), null,
                    new Color(96, 52, 150) * (0.25f * alfa), 0f,
                    glow.Size() * 0.5f,
                    new Vector2(190f, 130f) * escalaBruma / glow.Size(),
                    SpriteEffects.None, 0f);
""",
"""                // === v6.51.1 — EL MAR DE BRUMAS DE VERDAD (la cura de los
                //     óvalos planos: SombrasLib.Bruma, la librería fBm de
                //     la casa — puffs que se desgarran, no discos lisos).
                //     Se dibuja con SU propio lote (el patrón del Dormido):
                //     cerrar → bruma → reabrir. ===
                VFXCore.CerrarLoteSiAbierto();
                try
                {
                    float escalaBruma = 0.7f + 0.5f * MathHelper.Clamp(t / (float)T_SUBE, 0f, 1f)
                        - 0.5f * despedida;
                    if (escalaBruma > 0.1f && alfa > 0.05f)
                    {
                        Vector2 centroMundo = Projectile.Center + new Vector2(0f, 14f);
                        // LA NUBE MADRE (debajo del libro, respirando).
                        SombrasLib.Bruma(centroMundo, 84f * escalaBruma,
                            alfa * (0.6f + 0.4f * apertura), tiempo, Projectile.identity % 89 + 5);
                        // LOS DOS BRAZOS (a los costados — el mar no es un
                        // punto: es una multitud que lo sostiene todo).
                        SombrasLib.Bruma(centroMundo + new Vector2(-70f * escalaBruma, 6f),
                            58f * escalaBruma, alfa * 0.8f, tiempo * 0.93f,
                            Projectile.identity % 89 + 17);
                        SombrasLib.Bruma(centroMundo + new Vector2(70f * escalaBruma, 6f),
                            58f * escalaBruma, alfa * 0.8f, tiempo * 1.07f,
                            Projectile.identity % 89 + 29);
                    }
                }
                catch { VFXCore.CerrarLoteSiAbierto(); }
                VFXCore.ReabrirLoteVanilla();

                // EL RIM VIOLETA (la membrana del evento — aditivo sutil).
                Main.spriteBatch.Draw(glow, pos + new Vector2(0f, 14f), null,
                    new Color(96, 52, 150) * (0.22f * alfa), 0f,
                    glow.Size() * 0.5f,
                    new Vector2(190f, 130f) * escalaBruma / glow.Size(),
                    SpriteEffects.None, 0f);
""", R)
# la variable escalaBruma ahora vive en el bloque de bruma — quitar la
# declaraciรณn duplicada si el código posterior la usa
escribir(R, s)
print("OK — (6b) el escupidizo: bruma de verdad")

# ======================================================================
# (7) LA TERCERA VENTANA (solo estadísticas)
# ======================================================================
R = "Content/Weapons/GrimoireEternal.cs"
s = leer(R)

# (7a) las stats devoradas/festín: SOLO en la ventana del festín
s = rep(s,
"""            Player portadorTip = Main.LocalPlayer;
            var spDevorado = portadorTip?.GetModPlayer<Players.ShardPlayer>();
            if (spDevorado != null && spDevorado.DevoracionPoder > 0)
            {""",
"""            Player portadorTip = Main.LocalPlayer;
            var spDevorado = portadorTip?.GetModPlayer<Players.ShardPlayer>();
            // v6.51.1 — LO DEVORADO solo en SU ventana (la letra: la
            // tercera ventana es SOLO estadísticas del escuadrón y su
            // festín — nada de progresión ni descripciones).
            if (ventanaFestin && spDevorado != null && spDevorado.DevoracionPoder > 0)
            {""", R)
s = rep(s,
"""            if (spDevorado != null && (spDevorado.DevoracionExp > 0 ||
                spDevorado.DevoracionFuerza > 0 ||""",
"""            if (ventanaFestin && spDevorado != null && (spDevorado.DevoracionExp > 0 ||
                spDevorado.DevoracionFuerza > 0 ||""", R)

# (7b) LA VENTANA 2 REESCRITA: SOLO ESTADÍSTICAS DEL ESCUADRÓN
i0 = s.index("""            if (ventanaFestin)
            {
                int minionCost = WeaponScaling.MinionManaCost(sl.Level);""")
i1 = s.index("""            // === LA PROGRESIÓN: la barra de EXP y la pareja del HAMBRE.""")
s = s[:i0] + '''            if (ventanaFestin)
            {
                int minionCost = WeaponScaling.MinionManaCost(sl.Level);
                float contactDmg = (WeaponScaling.MinionContactDamageMult(sl.Level) - 1f) * 100f;
                float minionSpd = (WeaponScaling.MinionSpeedMult(sl.Level) - 1f) * 100f;
                float detectRange = WeaponScaling.MinionDetectionRange(sl.Level);
                float ritmoOjos = (1f + 0.5f * System.Math.Min(sl.Level - 1, 99) / 99f - 1f) * 100f;
                int cadenaOjos = WeaponScaling.MinionChainCount(sl.Level);

                // v6.51.1 — LOS NÚMEROS REALES DEL PORTADOR (la letra:
                // «solo estadísticas de los minions… además de las
                // estadísticas que ya están»).
                int despertar = Systems.DespertarGrimorio.NivelDe(portadorTip);
                int techoOjos = Players.ShardPlayer.TechoOjosDe(portadorTip, sl.Level);
                int ojosVivos = 0;
                int tipoOjoTip = ModContent.ProjectileType
                    <global::AethonMod.Content.Projectiles.OjoEternoMinion>();
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile pr = Main.projectile[i];
                    if (pr != null && pr.active && pr.type == tipoOjoTip &&
                        pr.owner == portadorTip.whoAmI) ojosVivos++;
                }
                int mejorasDesbloqueadas = Systems.DespertarGrimorio.MejorasDesbloqueadas(despertar);
                float multMejoras = 1f + 0.5f * mejorasDesbloqueadas;

                tooltips.Add(new TooltipLine(Mod, "SectionMinion", "[c/BE78FD:═══ EL ESCUADRÓN ═══]"));
                tooltips.Add(new TooltipLine(Mod, "MinionSlots",
                    $"[c/E8D8FF:Ojos: {ojosVivos}/{techoOjos}]  " +
                    $"[c/78788C:(+1 ojo cada 10 niveles del grimorio)]"));
                tooltips.Add(new TooltipLine(Mod, "MinionMejoras",
                    $"[c/E8D8FF:Mejoras desbloqueadas: {mejorasDesbloqueadas}/4]  " +
                    $"[c/BE78FD:(+50% daño por mejora · ×{multMejoras:0.##} de daño con las mejoras — +200% con las 4)]"));
                tooltips.Add(new TooltipLine(Mod, "MinionContactDmg",
                    $"[c/BE78FD:+{contactDmg:F0}% daño del rayo]"));
                tooltips.Add(new TooltipLine(Mod, "MinionRitmo",
                    $"[c/BE78FD:+{ritmoOjos:F0}% velocidad de ataque (tope +50% en nivel 100)]"));
                tooltips.Add(new TooltipLine(Mod, "MinionCadena",
                    $"[c/BE78FD:Rayo en cadena: {cadenaOjos} enemigos (base 2, +1 cada 20 niveles)]"));
                tooltips.Add(new TooltipLine(Mod, "MinionSpeed",
                    $"[c/BE78FD:+{minionSpd:F0}% velocidad]"));
                tooltips.Add(new TooltipLine(Mod, "MinionRange",
                    $"[c/BE78FD:{detectRange:F0}px rango de detección]"));
                if (!ManaMinionGratis())
                {
                    tooltips.Add(new TooltipLine(Mod, "ManaMinion",
                        $"[c/55AAFF:Costo: {minionCost} mana por invocación]"));
                }

                // === LOS ATAQUES COMBINADOS (la letra: «si son capaces de
                //     lanzar soles runicos y agujeros negros runicos,
                //     cuanto pueden lanzar y eso») ===
                tooltips.Add(new TooltipLine(Mod, "SectionCombinados",
                    "[c/C49CFF:═══ ATAQUES COMBINADOS ═══]"));
                if (Systems.DespertarGrimorio.SolDisponible(despertar))
                {
                    tooltips.Add(new TooltipLine(Mod, "WSol",
                        $"[c/E8D8FF:Sol Rúnico: {Systems.DespertarGrimorio.AnillosDelSol(despertar)} anillo(s) · " +
                        $"{Systems.DespertarGrimorio.SolesPorMinuto(despertar)}/min]  " +
                        "[c/78788C:(2+ ojos mejorados, al azar)]"));
                }
                else
                {
                    tooltips.Add(new TooltipLine(Mod, "WSol",
                        "[c/78788C:Sol Rúnico: bloqueado — Despertar 7]"));
                }
                if (Systems.DespertarGrimorio.AgujerosDisponibles(despertar))
                {
                    string asc = Systems.DespertarGrimorio.AgujerosAscendidos(despertar)
                        ? " · ASCENDIDOS" : "";
                    tooltips.Add(new TooltipLine(Mod, "WAgujeros",
                        $"[c/E8D8FF:Agujeros Negros Rúnicos: 4 al azar{asc} · " +
                        $"{Systems.DespertarGrimorio.AgujerosPorMinuto(despertar)}/min]  " +
                        "[c/78788C:(5+ ojos mejorados)]"));
                }
                else
                {
                    tooltips.Add(new TooltipLine(Mod, "WAgujeros",
                        "[c/78788C:Agujeros Negros Rúnicos: bloqueado — Despertar 10]"));
                }
                if (Systems.DespertarGrimorio.SupremoDisponible(despertar))
                {
                    tooltips.Add(new TooltipLine(Mod, "WSupremo",
                        "[c/E8D8FF:Supremo / Supremo Aurora: 1/min]  " +
                        "[c/78788C:(8+ ojos con la mejora 4)]"));
                }
                else
                {
                    tooltips.Add(new TooltipLine(Mod, "WSupremo",
                        "[c/78788C:Supremo / Supremo Aurora: bloqueado — Despertar 45]"));
                }
                if (despertar >= Systems.DespertarGrimorio.NivelMax)
                {
                    tooltips.Add(new TooltipLine(Mod, "WMaximo",
                        "[c/C49CFF:Despertar 50: TODO el ataque del escuadrón ×1,25 y más rápido]"));
                }
            }

''' + s[i1:]

# (7c) la progresión SOLO en la ventana básica (0)
s = rep(s,
"""            // === LA PROGRESIÓN: la barra de EXP y la pareja del HAMBRE.
            //     v6.50.108 — EL ORDEN DE LA LETRA: la BARRA de hambre
            //     primero y el estado JUSTO DEBAJO. En la ventana EL ARMA
            //     (.110) no está — la progresión es del FESTÍN.
            //     Y de vuelta LAS ESTADÍSTICAS DEL PRÓXIMO NIVEL: el
            //     próximo hito y su escalera de mejoras ===
            if (!ventanaArma)
            {""",
"""            // === LA PROGRESIÓN: la barra de EXP y la pareja del HAMBRE.
            //     v6.51.1 — SOLO EN LA VENTANA BÁSICA (la letra: la
            //     tercera ventana es solo estadísticas — la progresión no
            //     debe estar ahí).
            //     v6.50.108 — EL ORDEN DE LA LETRA: la BARRA de hambre
            //     primero y el estado JUSTO DEBAJO. ===
            if (vista == 0)
            {""", R)
escribir(R, s)
print("OK — (7) la tercera ventana")

# ======================================================================
# (8a) AETHON: LA BARRA DE VIDA (el head slot garantizado)
# ======================================================================
R = "Content/NPCs/AethonBoss.cs"
s = leer(R)
s = rep(s,
"""        public override void SetDefaults()
        {
            // v6.50.119 — EL CUERPO GIGANTE""",
"""        public override void SetDefaults()
        {
            // v6.50.119 — EL CUERPO GIGANTE""", R)
# el head texture + slot en la BASE (la gemela de luz no lo tenía)
s = rep(s,
"""    public class AethonBoss : ModNPC
    {
""",
"""    // v6.51.1 — [AutoloadBossHead] (la letra: «al fusionarse su barra de
    // vida se pierde»): LA CAUSA — la barra de vida de vanilla SOLO valida
    // jefes con CABEZA DE JEFE registrada (CommonBossBigProgressBar exige
    // GetBossHeadTextureIndex() != -1) y NINGÚN Aethon tenía la suya: la
    // gemela oscura definía la TEXTURA pero sin el ATRIBUTO jamás se
    // REGISTRÓ el slot. Ahora los tres la llevan — y el slot se FUERZA en
    // BossHeadSlot (la garantía: la barra SIEMPRE valida).
    [AutoloadBossHead]
    public class AethonBoss : ModNPC
    {
        /// <summary>LA CABEZA DE LA BARRA: el sol del recuerdo.</summary>
        public override string BossHeadTexture => "AethonMod/Content/NPCs/AethonBoss_Head_Boss";

        /// <summary>EL SLOT GARANTIZADO (la barra exige un índice válido:
        /// el head registrado por el atributo, sin fallar jamás).</summary>
        public override void BossHeadSlot(ref int index)
        {
            index = NPCHeadLoader.GetBossHeadSlot("AethonMod/Content/NPCs/AethonBoss_Head_Boss");
            if (index < 0) index = 0;   // jamás -1: la barra siempre vive
        }
""", R)
# _yaDropeo → protected (para el override del fusionado)
s = rep(s,
"""        private bool _yaDropeo""",
"""        protected bool _yaDropeo""", R)
escribir(R, s)
print("OK — (8a) barra: la base")

R = "Content/NPCs/AethonBossOscuro.cs"
s = leer(R)
s = rep(s,
"""    public class AethonBossOscuro : AethonBoss
    {""",
"""    [AutoloadBossHead]   // v6.51.1 — la barra de vida siempre valida
    public class AethonBossOscuro : AethonBoss
    {
        // (la cabeza y su slot los hereda de la base: el mismo sol)
""", R)
escribir(R, s)
print("OK — (8a) barra: la oscura")

R = "Content/NPCs/AethonBossFusionado.cs"
s = leer(R)
s = rep(s,
"""    public class AethonBossFusionado : AethonBoss
    {""",
"""    [AutoloadBossHead]   // v6.51.1 — LA CURA: «al fusionarse su barra de vida se pierde»
    public class AethonBossFusionado : AethonBoss
    {
        // (la cabeza y su slot los hereda de la base: el sol del recuerdo)
""", R)
escribir(R, s)
print("OK — (8a) barra: el fusionado")

# ======================================================================
# (8b) EL LOOT DEL RECUERDO COMPLETO (la letra: mascota de luz + 20
#      esencias + 10 platino — «al menos por ahora»)
# ======================================================================
R = "Content/NPCs/AethonBossFusionado.cs"
s = leer(R)
s = rep(s,
"""        protected override void DropBotin()
        {
            base.DropBotin();
            Item.NewItem(NPC.GetSource_Loot(), NPC.Center,
                ModContent.ItemType<Items.Esencias.EsenciaDeAethon>(), 1);
            // LA CRÍA GEMELA — SIEMPRE, y EN SU BOLSA (la letra de la
            // v6.50.119: «crea una bolsa donde esten las mascotas, las
            // 2»): el recuerdo recompuesto suelta LA BOLSA DE LA CRÍA —
            // al abrirla despierta la pareja completa: la de luz y su
            // gemela oscura, LAS DOS MASCOTAS en un solo ítem adentro.
            Item.NewItem(NPC.GetSource_Loot(), NPC.Center,
                ModContent.ItemType<Items.Llamados.BolsaDeLaCria>(), 1);
        }""",
"""        protected override void DropBotin()
        {
            // v6.51.1 — EL BOTÍN DE LA LETRA: «cuando muere Aethon su
            // loot debe ser su mascota de luz, 20 esencia corruptas y 10
            // platino, al menos por ahora» — SOLO eso (el botín viejo —
            // la esencia de Aethon, la Forma Ascendida, los 250
            // fragmentos, la bolsa de la cría — se retira con el override
            // completo: la carta nueva manda).
            if (_yaDropeo) return;
            _yaDropeo = true;
            var src = NPC.GetSource_Loot();
            Item.NewItem(src, NPC.Center,
                ModContent.ItemType<Items.Llamados.AethonMenorItem>(), 1);
            Item.NewItem(src, NPC.Center,
                ModContent.ItemType<global::AethonMod.Content.Items.Altar.ItemCorrupto>(), 20);
            Item.NewItem(src, NPC.Center, ItemID.PlatinumCoin, 10);
        }""", R)
escribir(R, s)
print("OK — (8b) el loot del recuerdo")

# ======================================================================
# (8c) LAS PÁGINAS CUADRADAS (la cura: SoftGlow, no Pixel-rects)
# ======================================================================
R = "Content/Projectiles/Jefes/AtaqueFusionProjectile.cs"
s = leer(R)
s = rep(s,
"""                if (Estilo == EstiloPaginaTinta)
                {
                    VFXCore.Begin();
                    try
                    {
                        Color cTinta = new(70, 30, 112);
                        Color cBorde = new(140, 80, 205);
                        VFXCore.Quad(Projectile.Center, cBorde * 0.55f,
                            new Vector2(34f, 46f), Projectile.rotation, VFXCore.Pixel);
                        VFXCore.Quad(Projectile.Center, cTinta * 0.85f,
                            new Vector2(26f, 38f), Projectile.rotation, VFXCore.Pixel);
                        // LAS TRES LÍNEAS DE ESCRITURA (la letra de la página).
                        for (int l = 0; l < 3; l++)
                        {
                            Vector2 off = new Vector2(0f, (l - 1) * 9f)
                                .RotatedBy(Projectile.rotation);
                            VFXCore.Quad(Projectile.Center + off, cBorde * 0.35f,
                                new Vector2(16f, 2f), Projectile.rotation, VFXCore.Pixel);
                        }
                    }
                    finally { VFXCore.FlushAdditive(); }
                }""",
"""                if (Estilo == EstiloPaginaTinta)
                {
                    // v6.51.1 — LA CURA DE LOS CUADRADOS (la letra: «hay
                    // un ataque que parece que lanza algo pero el sprite
                    // está mal y solo se ven cuadrados»): el cuerpo de la
                    // página era Pixel-rects duros (rectángulos LISOS de
                    // 1×1 estirados). Ahora es una página de VERDAD:
                    // cuerpo de tinta con SoftGlow (borde difuso), rim
                    // violeta y LAS LÍNEAS DE ESCRITURA como trazos
                    // suaves — nada de esquinas.
                    VFXCore.Begin();
                    try
                    {
                        Color cTinta = new(70, 30, 112);
                        Color cBorde = new(140, 80, 205);
                        // EL CUERPO (la hoja: glow difuso — páginas de
                        // sombra, no cartón).
                        VFXCore.Quad(Projectile.Center, cBorde * 0.40f,
                            new Vector2(40f, 52f), Projectile.rotation, VFXCore.SoftGlow);
                        VFXCore.Quad(Projectile.Center, cTinta * 0.85f,
                            new Vector2(30f, 42f), Projectile.rotation, VFXCore.SoftGlow);
                        // EL RIM (el borde de la hoja — tinta más clara).
                        VFXCore.Quad(Projectile.Center, cBorde * 0.30f,
                            new Vector2(46f, 58f), Projectile.rotation, VFXCore.SoftGlow);
                        // LAS TRES LÍNEAS DE ESCRITURA (trazos suaves).
                        for (int l = 0; l < 3; l++)
                        {
                            Vector2 off = new Vector2(0f, (l - 1) * 9f)
                                .RotatedBy(Projectile.rotation);
                            VFXCore.Quad(Projectile.Center + off, cBorde * 0.42f,
                                new Vector2(18f, 4f), Projectile.rotation, VFXCore.SoftGlow);
                        }
                    }
                    finally { VFXCore.FlushAdditive(); }
                }""", R)
escribir(R, s)
print("OK — (8c) páginas de sombra")

# ======================================================================
# (9) LA RAREZA CÓSMICA DEL ETERNO
# ======================================================================
R = "Content/Weapons/GrimoireEternal.cs"
s = leer(R)
# el rare se setea en GrimoireHambriento (la familia) o aquí — buscar
if "Item.rare = ItemRarityID.Quest" in s:
    s = rep(s, "Item.rare = ItemRarityID.Quest;",
"""// v6.51.1 — LA RAREZA CÓSMICA (la letra: «una rareza más que legendaria,
            // más que mítica, más que única, una rareza cósmica además con
            // la etiqueta de legendario supremo»).
            Item.rare = ModContent.RarityType
                <global::AethonMod.Content.Rarities.LegendarioSupremo>();""", R)
    escribir(R, s)
    print("OK — (9) rareza en GrimoireEternal")
else:
    # ¿dónde está el rare del Eterno? — buscar en la cadena de herencia
    print("[AVISO] el rare no vive en GrimoireEternal — buscar en la familia")

# ======================================================================
# (10) BESTIARIO: centrado + las 10 estrellas PRIMERO
# ======================================================================
R = "Content/NPCs/GrimorioDormido.cs"
s = leer(R)
s = rep(s,
"""            // v6.51.0 — EL RETRATO EN SU CASILLA (la letra: «está mal
            // colocado y fuera de su casilla»): el cuerpo del Eterno
            // (30×42) se salía de la caja del bestiario — el retrato se
            // ENCOGE a 0,72 y se CENTRA en su marco (la vía oficial de
            // vanilla para acomodar retratos: Velocity 0 = posado
            // quieto, sin la deriva de las mascotas voladoras).
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type,
                new NPCID.Sets.NPCBestiaryDrawModifiers()
                {
                    Velocity = 0f,
                    PortraitScale = 0.72f,
                    PortraitPositionXOverride = 0f,
                    PortraitPositionYOverride = 0f,
                });
""",
"""            // v6.51.1 — EL CENTRADO DE VERDAD (la letra: «no está
            // centrado en su casilla en ninguna de las dos»): la .51.0
            // FORZABA las posiciones del retrato a (0,0) — el ORIGEN de
            // la caja, no el centro — y eso lo descuadraba TODO. La cura:
            // SIN overrides de posición ni escala — el centrado de
            // vanilla es el correcto; solo se apaga la DERIVA flotante
            // (Velocity 0: posado quieto en ambas casillas).
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type,
                new NPCID.Sets.NPCBestiaryDrawModifiers()
                {
                    Velocity = 0f,
                });
""", R)
s = rep(s,
"""            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                new FlavorTextBestiaryInfoElement("Mods.AethonMod.NPCs.GrimorioDormido.Numero"),
                new FlavorTextBestiaryInfoElement("Mods.AethonMod.NPCs.GrimorioDormido.Bestiario"),
            });""",
"""            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                // v6.51.1 — LAS DIEZ ESTRELLAS NEGRAS, PRIMERAS (la letra:
                // «10 estrellas negras brillantes y titilantes» — la fila
                // titilante ANTES que todo: lo primero que lee el catálogo
                // es lo que el grimorio ES).
                new global::AethonMod.Content.Bestiary.EstrellasNegrasInfoElement(),
                new FlavorTextBestiaryInfoElement("Mods.AethonMod.NPCs.GrimorioDormido.Numero"),
                new FlavorTextBestiaryInfoElement("Mods.AethonMod.NPCs.GrimorioDormido.Bestiario"),
            });""", R)
escribir(R, s)
print("OK — (10) bestiario")

# ======================================================================
# (11) RECETAS: solo armas; no re-corromper; el aviso de stats
# ======================================================================
R = "Content/Systems/RecetasCorrupcionSistema.cs"
s = leer(R)
i0 = s.index("        public override void AddRecipes()")
i1 = s.index("        /// <summary>EL SELLO DE LA RECETA")
s = s[:i0] + '''        public override void AddRecipes()
        {
            try
            {
                int eterno = ModContent.ItemType<global::AethonMod.Content.Weapons.GrimoireEternal>();

                int total = ItemLoader.ItemCount;
                int armas = 0;

                for (int t = 1; t < total; t++)
                {
                    // LO QUE EL GRIMORIO NO TOCA.
                    if (t == eterno) continue;

                    // EL PROTOTIPO (ContentSamples ya conoce a todos: los
                    // items cargan ANTES que las recetas).
                    Item proto = null;
                    try { proto = ContentSamples.ItemsByType[t]; } catch { }
                    if (proto == null) continue;

                    // v6.51.1 — SOLO ARMAS (la letra: «mejor quitemos que
                    // cualquier cosa se convierta en esencia corrupta,
                    // dejemos que la esencia corrupta sea solo un loot de
                    // monstruo y jefes»): las recetas item→esencia
                    // MURIERON — la Esencia Corrupta solo cae de los
                    // cuerpos.
                    if (!DespertarGrimorioSistema.EsArma(proto)) continue;

                    // ===== LA RECETA DEL SELLO: arma → arma CORRUPTA =====
                    // (EL SELLO lo pone MarcarCorrupta — el callback de
                    // ESTA receta: el resultado nace con la etiqueta
                    // Corrupto y SIN prefijo).
                    // LAS DOS CONDICIONES:
                    //   · LlevaElGrimorio: el libro en el inventario (la
                    //     corrupción vive en el apartado de creación — sin
                    //     estaciones, sin lanzar nada).
                    //   · QUEDA UNA LIMPIA: el arma corrupta NO puede
                    //     volver a corromperse (la letra) — la receta solo
                    //     se ofrece si llevas una instancia SIN corromper.
                    //   · NaceCorrupta (siempre cierta): el aviso de las
                    //     estadísticas del arma corrupta EN EL MENÚ (la
                    //     letra: «eso debe mostrarse con una etiqueta»).
                    Recipe.Create(t, 1)
                        .AddIngredient(t, 1)
                        .AddCondition(LlevaElGrimorio)
                        .AddCondition(QuedaUnaLimpia(t))
                        .AddCondition(NaceCorrupta)
                        .AddOnCraftCallback(MarcarCorrupta)
                        .Register();
                    armas++;
                }
            }
            catch { }
        }

        /// <summary>v6.51.1 — ¿El portador lleva una instancia LIMPIA de
        /// esta arma? (la letra: «un arma corrupta no puede volver a
        /// corromperse» — la receta se apaga cuando la única copia que
        /// llevas ya está sellada).</summary>
        private static Condition QuedaUnaLimpia(int tipo)
        {
            return new Condition(Language.GetText("Mods.AethonMod.Despertar.ReqArmaLimpia"),
                () =>
                {
                    Player yo = Main.LocalPlayer;
                    if (yo == null) return false;
                    for (int k = 0; k < 58; k++)
                    {
                        Item it = yo.inventory[k];
                        if (it == null || it.IsAir || it.type != tipo) continue;
                        var sello = it.GetGlobalItem
                            <global::AethonMod.Content.Globals.CorruptoGlobalItem>();
                        if (sello == null || !sello.Corrompido) return true;   // HAY una limpia
                    }
                    return false;
                });
        }

        /// <summary>v6.51.1 — EL AVISO DEL MENÚ (siempre cierto, verde):
        /// las estadísticas del arma corrupta visibles en el apartado de
        /// creación — la etiqueta que la letra pidió.</summary>
        private static readonly Condition NaceCorrupta = new Condition(
            Language.GetText("Mods.AethonMod.Despertar.NaceCorrupta"),
            () => true);

''' + s[i1:]
escribir(R, s)
print("OK — (11) recetas")

# ======================================================================
# (12) CORRUPTO GLOBAL: +5% a TODAS las estadísticas
# ======================================================================
R = "Content/Globals/CorruptoGlobalItem.cs"
s = leer(R)
s = rep(s,
"""        public override void ModifyWeaponCrit(Item item, Player player, ref float crit)
        {
            if (!Corrompido) return;
            crit += 2f;                      // +2 % de crítico
        }""",
"""        public override void ModifyWeaponCrit(Item item, Player player, ref float crit)
        {
            if (!Corrompido) return;
            crit *= 1.05f;                   // +5 % de crítico (relativo — TODAS las estadísticas en 5 %)
        }""", R)
escribir(R, s)
print("OK — (12) +5% a todas")

print("PARTE B COMPLETA")
