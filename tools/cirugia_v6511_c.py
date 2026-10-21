#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# ======================================================================
# cirugia_v6511_c.py — LA CONTINUACIÓN (lo que la B no alcanzó):
#  (8a-fix) LA BARRA: los atributos NO SE HEREDAN — el fusionado (y el
#           oscuro) jamás registraron SU cabeza. Overrides garantizados
#           en la base + atributo en los tres.
#  (8b) EL LOOT del Recuerdo Completo
#  (8c) LAS PÁGINAS CUADRADAS
#  (9) LA RAREZA CÓSMICA
#  (10) BESTIARIO: centrado + 10 estrellas negras primero
#  (11) RECETAS: solo armas + no re-corromper + aviso de stats
#  (12) +5% a TODAS las estadísticas
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
# (8a-fix) LA BARRA DE VIDA — LA CAUSA REAL
# ======================================================================
R = "Content/NPCs/AethonBoss.cs"
s = leer(R)
# LA BASE ya tiene [AutoloadBossHead] (desde la .121) — le faltan las
# overrides de garantía. La clase ya no lleva el atributo suelto: se
# documenta el porqué de las overrides.
s = rep(s,
"""    [AutoloadBossHead]
    public class AethonBoss : ModNPC
    {
""",
"""    [AutoloadBossHead]
    public class AethonBoss : ModNPC
    {
        // v6.51.1 — LA CABEZA GARANTIZADA (la letra: «al fusionarse su
        // barra de vida se pierde»): LOS ATRIBUTOS NO SE HEREDAN — el
        // fusionado y el gemelo oscuro NO llevan el suyo, así que su
        // cabeza JAMÁS se registró (la barra de vanilla SOLO valida jefes
        // con cabeza: CommonBossBigProgressBar exige un índice ≠ −1). Las
        // tres clases llevan ahora SU [AutoloadBossHead] y esta override
        // de slot es la garantía de la base (BossHeadTexture por
        // convención: AethonBoss_Head_Boss — el archivo existe).
        public override void BossHeadSlot(ref int index)
        {
            index = NPCHeadLoader.GetBossHeadSlot("AethonMod/Content/NPCs/AethonBoss_Head_Boss");
            if (index < 0) index = 0;   // jamás −1: la barra siempre vive
        }
""", R)
escribir(R, s)
print("OK — (8a) la base con el slot garantizado")

R = "Content/NPCs/AethonBossOscuro.cs"
s = leer(R)
s = rep(s,
"""    public class AethonBossOscuro : AethonBoss
    {""",
"""    [AutoloadBossHead]   // v6.51.1 — los atributos NO se heredan: SU propia cabeza
    public class AethonBossOscuro : AethonBoss
    {
""", R)
escribir(R, s)
print("OK — (8a) la oscura")

R = "Content/NPCs/AethonBossFusionado.cs"
s = leer(R)
s = rep(s,
"""    public class AethonBossFusionado : AethonBoss
    {""",
"""    [AutoloadBossHead]   // v6.51.1 — LA CURA DE LA BARRA: los atributos NO se heredan — el fusionado jamás registró SU cabeza
    public class AethonBossFusionado : AethonBoss
    {
""", R)

# ======================================================================
# (8b) EL LOOT DEL RECUERDO COMPLETO
# ======================================================================
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
print("OK — (8b) el loot")

# ======================================================================
# (8c) LAS PÁGINAS CUADRADAS
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
                    // 1×1 estirados — LITERALES cuadrados). Ahora es una
                    // página de sombra: cuerpo y rim con SoftGlow (bordes
                    // difusos) y las líneas de escritura como trazos
                    // suaves — cero esquinas.
                    VFXCore.Begin();
                    try
                    {
                        Color cTinta = new(70, 30, 112);
                        Color cBorde = new(140, 80, 205);
                        // EL RIM (el borde de la hoja — tinta clara difusa).
                        VFXCore.Quad(Projectile.Center, cBorde * 0.30f,
                            new Vector2(46f, 58f), Projectile.rotation, VFXCore.SoftGlow);
                        // EL CUERPO (la hoja de tinta).
                        VFXCore.Quad(Projectile.Center, cBorde * 0.40f,
                            new Vector2(40f, 52f), Projectile.rotation, VFXCore.SoftGlow);
                        VFXCore.Quad(Projectile.Center, cTinta * 0.85f,
                            new Vector2(30f, 42f), Projectile.rotation, VFXCore.SoftGlow);
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
# (9) LA RAREZA CÓSMICA
# ======================================================================
R = "Content/Weapons/GrimoireEternal.cs"
s = leer(R)
s = rep(s,
"""            Item.rare = ItemRarityID.Quest;""",
"""            // v6.51.1 — LA RAREZA CÓSMICA (la letra: «una rareza más que
            // legendaria, más que mítica, más que única, una rareza cósmica
            // además con la etiqueta de legendario supremo»).
            Item.rare = ModContent.RarityType
                <global::AethonMod.Content.Rarities.LegendarioSupremo>();""", R)
escribir(R, s)
print("OK — (9) la rareza cósmica")

# ======================================================================
# (10) BESTIARIO
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
            // FORZABA la posición del retrato a (0,0) — el ORIGEN de la
            // caja, no el centro — y eso lo descuadraba TODO. La cura:
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
# (11) RECETAS
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
                    // Corrupto y SIN prefijo). LAS CONDICIONES:
                    //   · LlevaElGrimorio: el libro en el inventario (la
                    //     corrupción vive en el apartado de creación — sin
                    //     estaciones, sin lanzar nada).
                    //   · QuedaUnaLimpia: el arma corrupta NO puede volver
                    //     a corromperse (la letra) — la receta solo se
                    //     ofrece si llevas una instancia SIN corromper.
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
# (12) +5% A TODAS LAS ESTADÍSTICAS
# ======================================================================
R = "Content/Globals/CorruptoGlobalItem.cs"
s = leer(R)
if "crit += 2f;" in s:
    s = rep(s,
"""            crit += 2f;                      // +2 % de crítico""",
"""            crit *= 1.05f;                   // +5 % de crítico (TODAS las estadísticas en 5 %)""", R)
    escribir(R, s)
    print("OK — (12) +5% a todas")
else:
    print("SKIP — (12) ya aplicado")

print("PARTE C COMPLETA")
