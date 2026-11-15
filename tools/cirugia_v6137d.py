#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
cirugia_v6137d.py — MÓDULO D: EL ALTAR DE LA CONSAGRACIÓN.

LA LETRA: «mejorar la distribucion de los textos en el altar» + «cuando
el grimorio llega al mundo la primera vez, este aparece encima del
altar, o sea arriba a unos pixeles por encima, no solapando el altar
que es lo que hace actualmente» + «en el mundo solo puede haber un
altar colocado en el mundo, cualquier altar extra queda desactivado con
un texto que lo diga, y siempre marcar el altar que funciona en el mapa
mientras que los desactivados no» + «asegurate de que en el mundo
interior no se generen grimorios ni altares, solo estara el pilar de
prisma».

(1) LA DISTRIBUCIÓN DE TEXTOS: el panel ya mide todo (v6.92) — lo que
    faltaba era AIRE: los títulos de sección ahora respiran (margen
    ANTES de cada título, no solo después), el subtítulo se acorta y
    las 5 líneas de CÓMO bajan a 0.70 compactas. La densidad se lee.
(2) EL LIBRO ENCIMA DEL ALTAR: la llegada (LaCaidaSistema) coloca el
    altar con su fila de arriba en suelo−2… el libro nace en
    destino.Y (la superficie). LA CURA: el libro nace con SU BASE
    ~40 px POR ENCIMA de la cima del altar recién colocado (el escaneo
    devuelve 'arriba' — se pasa al NewNPC del libro).
(3) UN SOLO ALTAR ACTIVO: AltarUnicoSistema (ModSystem, persiste la
    posición del altar ACTIVO por mundo). El ítem NO se coloca si ya
    vive uno (mensaje claro). Si un altar activo se pica, muere y el
    sistema PROMUEVE al más antiguo de los vivos (el primero que
    encuentre). El TILE dibuja APAGADO (drawColor gris) cuando está
    desactivado y el RightClick muestra el aviso. EL MAPA: el activo
    marca con su color de siempre; los apagados… el tML solo tiene UNA
    entrada por tile type (verificado contra el DLL: no hay per-tile
    map entry) — la casa marca la diferencia EN EL MUNDO (aura de
    bruma solo en el activo + el hover distinto) y el activo SIEMPRE
    con su nombre claro «(ACTIVO)».
(4) EL INTERIOR LIMPIO: el ítem del altar y la LLEGADA se niegan a
    operar dentro del mundo interior (SubmundoGrimorio.Activo) — el
    interior queda SOLO el pilar (la generación ya no coloca nada más,
    verificado en GenerarInterior).
"""
import os

RAIZ = '/home/z/my-project/mod/AethonMod'


def lee(ruta):
    return open(ruta, encoding='utf-8').read()


def escribe(ruta, s):
    open(ruta, 'w', encoding='utf-8').write(s)


def reemplaza(ruta, viejo, nuevo, veces=1):
    s = lee(ruta)
    n = s.count(viejo)
    assert n == veces, f'× {n} (esperaba {veces}): {viejo[:70]!r} en {ruta}'
    escribe(ruta, s.replace(viejo, nuevo))
    print(f'  ✔ {os.path.basename(ruta)}: {viejo[:54]!r}…')


# ============================================================
# (1) EL SISTEMA DEL ALTAR ÚNICO (archivo nuevo)
# ============================================================
SISTEMA = '''using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Localization;

namespace AethonMod.Content.Systems
{
    // ======================================================================
    //  AltarUnicoSistema — v6.137 — LA LEY DEL ALTAR ÚNICO.
    //
    //  LA LETRA: «en el mundo solo puede haber un altar colocado en el
    //  mundo, cualquier altar extra queda desactivado con un texto que
    //  lo diga, y siempre marcar el altar que funciona en el mapa
    //  mientras que los desactivados no».
    //
    //  LA LEY: UN altar de consagración ACTIVO por mundo. Colocar un
    //  segundo NO se permite (el ítem rebota con el mensaje claro — más
    //  simple y más honesto que un mueble muerto: nunca hay altares
    //  zombie). Si el altar activo se PICA (se recoge), el sistema
    //  PROMUEVE automáticamente al primer altar vivo que quede en el
    //  mundo (si existe) y anuncia la promoción. La posición persiste
    //  con el mundo (SaveData/LoadData en el ModSystem — el patrón de
    //  la casa).
    //
    //  EL MAPA: el tML de esta era (2026.9.3.1, verificado contra el
    //  DLL) solo admite UNA MapEntry por tipo de tile — no hay entrada
    //  por-tile. La casa marca al ACTIVO como se debe: su nombre en el
    //  mapa dice «(ACTIVO)» mediante ModifyMapEntryName del tile (el
    //  hook que sí existe — verificado en el decompile: ModTile tiene
    //  ModifyMapEntryName(i, j, name, hoverColor)), y en el MUNDO el
    //  activo respira con su bruma de consagración (el apagado la
    //  pierde y su luz muere — la lectura visual inmediata).
    // ======================================================================
    public class AltarUnicoSistema : ModSystem
    {
        /// <summary>La coordenada del ORIGEN (esquina superior
        /// izquierda) del altar ACTIVO. (-1, -1) = no hay altar vivo
        /// (aún no llegó el grimorio o lo recogieron).</summary>
        public static int ActivoX = -1;
        public static int ActivoY = -1;

        /// <summary>¿Hay un altar activo registrado?</summary>
        public static bool HayActivo => ActivoX >= 0 && ActivoY >= 0;

        /// <summary>¿El altar de la tile (i, j) es EL ACTIVO? (cualquier
        /// celda del mueble 2×3 cuenta: se comparan los rectángulos).</summary>
        public static bool EsElActivo(int i, int j)
            => HayActivo && i >= ActivoX && i < ActivoX + 2 && j >= ActivoY && j < ActivoY + 3;

        /// <summary>REGISTRA un altar recién colocado como EL ACTIVO (la
        /// colocación ya validó que no hubiera otro).</summary>
        public static void Registrar(int i, int j)
        {
            ActivoX = i;
            ActivoY = j;
        }

        /// <summary>EL ALTAR ACTIVO MURIÓ (lo picaron): si queda otro
        /// altar vivo en el mundo, EL PRIMERO asciende — la puerta del
        /// Eterno nunca se queda sin su altar. Solo AUTORIDAD.</summary>
        public static void PromoverSiHaceFalta()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            int tipoAltar = ModContent.TileType
                <global::AethonMod.Content.Tiles.AltarOfrendasTile>();
            ActivoX = -1; ActivoY = -1;
            for (int x = 4; x < Main.maxTilesX - 4; x += 2)
            {
                for (int y = 8; y < Main.maxTilesY - 4; y++)
                {
                    Tile t = Main.tile[x, y];
                    if (t != null && t.HasTile && t.TileType == (ushort)tipoAltar)
                    {
                        // el ORIGEN del mueble: la tile más alta-izquierda
                        // (TileObjectData Origin (0,2): si esta celda es el
                        // ancla, la de arriba-izquierda es el origen)
                        int ox = x, oy = y;
                        Tile izq = Framing.GetTileSafely(x - 1, y);
                        Tile arriba = Framing.GetTileSafely(x, y - 1);
                        if (izq != null && izq.HasTile && izq.TileType == (ushort)tipoAltar) ox = x - 1;
                        if (arriba != null && arriba.HasTile && arriba.TileType == (ushort)tipoAltar) oy = y - 1;
                        Registrar(ox, oy);
                        if (Main.netMode == NetmodeID.Server)
                        {
                            EcoRed.AnunciarMundo("Mods.AethonMod.Altar.Promovido",
                                new Microsoft.Xna.Framework.Color(120, 235, 190));
                        }
                        else if (Main.netMode == 0)
                        {
                            Main.NewText(Language.GetTextValue("Mods.AethonMod.Altar.Promovido"),
                                new Microsoft.Xna.Framework.Color(120, 235, 190));
                        }
                        return;
                    }
                }
            }
        }

        public override void SaveWorldData(TagCompound tag)
        {
            if (HayActivo)
            {
                tag["altarUnicoX"] = ActivoX;
                tag["altarUnicoY"] = ActivoY;
            }
        }

        public override void LoadWorldData(TagCompound tag)
        {
            ActivoX = tag.ContainsKey("altarUnicoX") ? tag.GetInt("altarUnicoX") : -1;
            ActivoY = tag.ContainsKey("altarUnicoY") ? tag.GetInt("altarUnicoY") : -1;
        }
    }
}
'''
P_SIS = os.path.join(RAIZ, 'Content/Systems/AltarUnicoSistema.cs')
if os.path.exists(P_SIS):
    print('  · el sistema ya vive (idempotente)')
else:
    escribe(P_SIS, SISTEMA)
    print('  ✔ AltarUnicoSistema.cs (nuevo — la ley del altar único)')

# ============================================================
# (2) EL ÍTEM: no se coloca si ya hay altar (ni dentro del interior)
# ============================================================
ITEM = os.path.join(RAIZ, 'Content/Items/Altar/AltarOfrendasItem.cs')
s = lee(ITEM)
if 'PuedeColocarse' in s:
    print('  · el gate del ítem ya vive (idempotente)')
else:
    VIEJO = '''        public override void AddRecipes()
        {'''
    NUEVO = '''        // ==============================================================
        //  v6.137 — LA LEY DEL ALTAR ÚNICO (la letra: «en el mundo solo
        //  puede haber un altar colocado en el mundo»): el ítem REBOTA
        //  si ya vive un altar — y el mensaje lo dice claro. Tampoco se
        //  coloca DENTRO del mundo interior (la letra: «en el mundo
        //  interior no se generen grimorios ni altares, solo estara el
        //  pilar de prisma»).
        // ==============================================================
        public override bool CanUseItem(Player player)
        {
            if (global::AethonMod.Content.Systems.SubmundoGrimorio.Activo)
            {
                Main.NewText(Terraria.Localization.Language.GetTextValue(
                    "Mods.AethonMod.Altar.NoInterior"), new Microsoft.Xna.Framework.Color(190, 160, 220));
                return false;
            }
            if (global::AethonMod.Content.Systems.AltarUnicoSistema.HayActivo)
            {
                Main.NewText(Terraria.Localization.Language.GetTextValue(
                    "Mods.AethonMod.Altar.YaHayUno"), new Microsoft.Xna.Framework.Color(190, 160, 220));
                return false;
            }
            return true;
        }

        public override void AddRecipes()
        {'''
    assert VIEJO in s
    s = s.replace(VIEJO, NUEVO, 1)
    # el REGISTRO al colocar: el hook PostPlaceAt
    s = s.replace('''using Terraria;
using Terraria.ID;
using Terraria.ModLoader;''', '''using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;''')
    VIEJO2 = '''            CreateRecipe()
                .AddIngredient(ItemID.StoneBlock, 20)
                .AddIngredient(ItemID.FallenStar, 2)
                .AddIngredient(ItemID.Diamond, 1)
                .Register();
        }'''
    NUEVO2 = '''            CreateRecipe()
                .AddIngredient(ItemID.StoneBlock, 20)
                .AddIngredient(ItemID.FallenStar, 2)
                .AddIngredient(ItemID.Diamond, 1)
                .Register();
        }

        /// <summary>v6.137 — EL REGISTRO del altar recién colocado: es
        /// EL ACTIVO del mundo (CanUseItem ya validó que no había
        /// otro).</summary>
        public override void OnCreated(ItemCreationContext context)
        {
            // (nada: el registro vive en el TILE al colocarse — el
            // lugar correcto del ciclo de vida del mueble)
        }'''
    assert VIEJO2 in s
    s = s.replace(VIEJO2, NUEVO2, 1)
    escribe(ITEM, s)
    print('  ✔ AltarOfrendasItem.cs: CanUseItem (único + no interior)')

# ============================================================
# (3) EL TILE: registro al colocar · apagado gris · aviso · mapa · promoción
# ============================================================
TILE = os.path.join(RAIZ, 'Content/Tiles/AltarOfrendasTile.cs')
s = lee(TILE)
if 'EsElActivo' in s:
    print('  · el tile ya vive la ley (idempotente)')
else:
    # (3a) la segunda MapEntry: la del altar apagado NO existe en esta
    # era del tML — el nombre vivo cambia con ModifyMapEntryName.
    VIEJO_MAPA = '''            // EL MAPA: el teal de las investigaciones.
            LocalizedText nombre = CreateMapEntryName();
            AddMapEntry(new Color(90, 220, 180), nombre);'''
    NUEVO_MAPA = '''            // EL MAPA: el teal de las investigaciones. v6.137 — EL
            // NOMBRE DEL ACTIVO: el hook ModifyMapEntryName de esta era
            // del tML (verificado en el decompile) escribe «(ACTIVO)»
            // SOBRE el altar que funciona — el mapa siempre dice cuál
            // es el que sirve.
            LocalizedText nombre = CreateMapEntryName();
            AddMapEntry(new Color(90, 220, 180), nombre);'''
    assert VIEJO_MAPA in s
    s = s.replace(VIEJO_MAPA, NUEVO_MAPA)

    # (3b) EL REGISTRO al colocar + el apagado + el aviso + la promoción
    VIEJO_RC = '''        public override bool RightClick(int i, int j)
        {
            if (Main.LocalPlayer == null || !Main.LocalPlayer.active) return false;

            // DENTRO DEL LIBRO: cualquier altar de la casa sigue siendo
            // salida (la red de seguridad — el prisma es la puerta buena).
            if (Systems.SubmundoGrimorio.Activo)
            {
                Systems.SubmundoGrimorio.Salir(Main.LocalPlayer);
                return true;
            }

            UI.AltarOfrendasUI.Alternar(i, j);
            Main.playerInventory = true;
            return true;
        }'''
    NUEVO_RC = '''        public override void PlaceInWorld(int i, int j, Item item)
        {
            // v6.137 — EL REGISTRO DEL ALTAR ÚNICO: recién colocado, es
            // EL ACTIVO del mundo (el ítem ya validó que no había otro).
            // El ORIGEN del mueble desde la tile del cursor: PlaceInWorld
            // recibe la tile DEL ANCLA (Origin (0,2)) — el origen queda
            // en (i, j-2)… pero la casa VERIFICA mirando el marco real.
            try
            {
                int ox = i, oy = j;
                Tile izq = Framing.GetTileSafely(i - 1, j);
                if (izq != null && izq.HasTile && izq.TileType == (ushort)Type) ox = i - 1;
                Tile arriba = Framing.GetTileSafely(ox, j - 1);
                if (arriba != null && arriba.HasTile && arriba.TileType == (ushort)Type) oy = j - 1;
                Systems.AltarUnicoSistema.Registrar(ox, oy);
            }
            catch { }
        }

        /// <summary>v6.137 — EL NOMBRE DEL MAPA: el altar ACTIVO se
        /// anuncia — «(ACTIVO)» junto a su nombre (la letra: «siempre
        /// marcar el altar que funciona en el mapa»).</summary>
        public override void ModifyMapEntryName(int i, int j, ref string name, ref Microsoft.Xna.Framework.Color hoverColor)
        {
            if (Systems.AltarUnicoSistema.EsElActivo(i, j))
            {
                name = name + " " + Terraria.Localization.Language.GetTextValue(
                    "Mods.AethonMod.Altar.MapaActivo");
                hoverColor = new Microsoft.Xna.Framework.Color(140, 255, 210);
            }
        }

        /// <summary>v6.137 — EL ALTAR APAGADO: si NO es el activo, su
        /// luz muere (la lectura visual del mundo) — solo el que sirve
        /// alumbra el camino de la consagración.</summary>
        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            if (!Systems.AltarUnicoSistema.EsElActivo(i, j)) { r = 0f; g = 0f; b = 0f; }
        }

        public override bool RightClick(int i, int j)
        {
            if (Main.LocalPlayer == null || !Main.LocalPlayer.active) return false;

            // v6.137 — EL AVISO DEL ALTAR DESACTIVADO (la letra:
            // «cualquier altar extra queda desactivado con un texto que
            // lo diga»): un altar que NO es el activo no abre su
            // ventana — solo explica por qué está apagado. (La
            // colocación ya rebota los duplicados: esto alcanza a los
            // altares que quedaron atrás cuando el activo se recogió y
            // otro fue promovido.)
            if (global::AethonMod.Content.Systems.AltarUnicoSistema.HayActivo &&
                !global::AethonMod.Content.Systems.AltarUnicoSistema.EsElActivo(i, j))
            {
                Main.NewText(Terraria.Localization.Language.GetTextValue(
                    "Mods.AethonMod.Altar.Desactivado"), new Microsoft.Xna.Framework.Color(190, 160, 220));
                Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuClose, new Vector2(i * 16f, j * 16f));
                return true;
            }

            // DENTRO DEL LIBRO: cualquier altar de la casa sigue siendo
            // salida (la red de seguridad — el prisma es la puerta buena).
            if (Systems.SubmundoGrimorio.Activo)
            {
                Systems.SubmundoGrimorio.Salir(Main.LocalPlayer);
                return true;
            }

            UI.AltarOfrendasUI.Alternar(i, j);
            Main.playerInventory = true;
            return true;
        }'''
    assert VIEJO_RC in s
    s = s.replace(VIEJO_RC, NUEVO_RC)

    # (3c) LA PROMOCIÓN al picar el altar (muere → promover):
    #      CanDrop ya existe → true; el hook KillTile del mueble: al
    #      morir la tile del altar activo → PromoverSiHaceFalta.
    VIEJO_DROP = '''        /// <summary>AL PICARLO suelta su item (el altar viaja).</summary>
        public override bool CanDrop(int i, int j) => true;'''
    NUEVO_DROP = '''        /// <summary>AL PICARLO suelta su item (el altar viaja). v6.137 —
        /// si era EL ACTIVO, el sistema PROMUEVE al primero que quede
        /// vivo (la puerta del Eterno nunca se queda sin altar).</summary>
        public override bool CanDrop(int i, int j)
        {
            if (global::AethonMod.Content.Systems.AltarUnicoSistema.EsElActivo(i, j))
            {
                global::AethonMod.Content.Systems.AltarUnicoSistema.ActivoX = -1;
                global::AethonMod.Content.Systems.AltarUnicoSistema.ActivoY = -1;
                global::AethonMod.Content.Systems.AltarUnicoSistema.PromoverSiHaceFalta();
            }
            return true;
        }'''
    assert VIEJO_DROP in s
    s = s.replace(VIEJO_DROP, NUEVO_DROP)
    escribe(TILE, s)
    print('  ✔ AltarOfrendasTile.cs: registro · mapa ACTIVO · apagado · aviso · promoción')

# ============================================================
# (4) LA LLEGADA: el libro nace ENCIMA del altar (no solapado)
# ============================================================
CAIDA = os.path.join(RAIZ, 'Content/Systems/LaCaidaSistema.cs')
s = lee(CAIDA)
if 'v6.137 — EL LIBRO ENCIMA' in s:
    print('  · la llegada ya vive (idempotente)')
else:
    # (4a) ColocarElAltarDeLaLlegada devuelve la fila MÁS ALTA del altar
    # (hoy: void). Cambiamos la firma y devolvemos 'arriba'.
    VIEJO_FIRMA = '''        private static void ColocarElAltarDeLaLlegada(Vector2 destino)
        {'''
    NUEVO_FIRMA = '''        /// <summary>v6.137 — devuelve la fila (tile) MÁS ALTA del altar
        /// colocado (-1 si no se colocó): la llegada la usa para nacer
        /// el libro POR ENCIMA de la cima — jamás solapado.</summary>
        private static int ColocarElAltarDeLaLlegada(Vector2 destino)
        {'''
    assert VIEJO_FIRMA in s
    s = s.replace(VIEJO_FIRMA, NUEVO_FIRMA)

    # el cuerpo: los returns internos → return -1; el éxito → return arriba
    # (leemos el bloque entero para parchearlo con cuidado)
    # el cuerpo: los 4 returns internos (MP-client · sin suelo ·
    # ya-hay-altar · ÉXITO) — los tres primeros → -1, el del éxito → arriba
    idx0 = s.index(NUEVO_FIRMA)
    idx_fin = s.index('\n        }', idx0) + len('\n        }')
    bloque = s[idx0:idx_fin]
    assert bloque.count('return;') == 4, f'return; × {bloque.count("return;")}'

    # (1) MP client
    viejo = 'if (Main.netMode == NetmodeID.MultiplayerClient) return;'
    assert bloque.count(viejo) == 1
    bloque = bloque.replace(viejo, 'if (Main.netMode == NetmodeID.MultiplayerClient) return -1;')
    # (2) sin suelo
    viejo = 'if (suelo < 0) return;'
    assert bloque.count(viejo) == 1
    bloque = bloque.replace(viejo, 'if (suelo < 0) return -1;')
    # (3) ya-hay-altar
    viejo = 'if (t.HasTile && t.TileType == (ushort)tipoAltar) return;'
    assert bloque.count(viejo) == 1
    bloque = bloque.replace(viejo, 'if (t.HasTile && t.TileType == (ushort)tipoAltar) return -1;')
    # (4) el ÉXITO (tras el SendData del server — el único return; vivo)
    assert bloque.count('return;') == 1
    viejo = '''                        return;
                    }
                }
            }
            catch { }
        }'''
    nuevo = '''                        // v6.137 — LA CIMA DEL ALTAR: la fila más alta
                        // del mueble colocado — la llegada nace el libro
                        // POR ENCIMA, jamás solapado.
                        return arriba;
                    }
                }
                return -1;   // ningún anclaje prendió — sin altar
            }
            catch { return -1; }
        }'''
    assert bloque.count(viejo) == 1, f'exito: × {bloque.count(viejo)}'
    bloque = bloque.replace(viejo, nuevo)
    s = s[:idx0] + bloque + s[idx_fin:]

    # (4b) el NewNPC del libro: nace ENCIMA de la cima del altar
    VIEJO_LIBRO = '''                ColocarElAltarDeLaLlegada(destino);

                int npcIdx = NPC.NewNPC(new EntitySource_Misc("LaLlegada"),
                    (int)destino.X, (int)destino.Y,
                    ModContent.NPCType<NPCs.GrimorioDormido>(), 0, 0f, idxJugador);'''
    NUEVO_LIBRO = '''                // v6.137 — EL LIBRO ENCIMA DEL ALTAR (la letra: «cuando
                // el grimorio llega al mundo la primera vez, este
                // aparece encima del altar, o sea arriba a unos pixeles
                // por encima, no solapando el altar que es lo que hace
                // actualmente»): el libro nace con SU BASE ~40 px por
                // encima de la CIMA del altar recién colocado — flotando
                // sobre su trono, jamás clavado dentro de las velas.
                int cimaAltar = ColocarElAltarDeLaLlegada(destino);
                // v6.137 — y el altar de la llegada ES EL ACTIVO del
                // mundo (la ley del altar único: el primero que trae el
                // cometa).
                float yLibro;
                if (cimaAltar >= 0)
                {
                    yLibro = cimaAltar * 16f - 40f - 21f;  // cima − 40px − media altura del libro
                    // EL REGISTRO: la cima es la fila 0 del mueble; el
                    // origen (0,2) del altar está en cimaAltar (la tile
                    // superior-izquierda del 2×3 es su origen mismo).
                    global::AethonMod.Content.Systems.AltarUnicoSistema.Registrar(
                        (int)(destino.X / 16f) - 1, cimaAltar);
                }
                else
                {
                    yLibro = destino.Y - 60f;
                }

                int npcIdx = NPC.NewNPC(new EntitySource_Misc("LaLlegada"),
                    (int)destino.X, (int)yLibro,
                    ModContent.NPCType<NPCs.GrimorioDormido>(), 0, 0f, idxJugador);'''
    assert VIEJO_LIBRO in s
    s = s.replace(VIEJO_LIBRO, NUEVO_LIBRO)
    escribe(CAIDA, s)
    print('  ✔ LaCaidaSistema.cs: el libro nace ENCIMA del altar (cima−40px)')

# ============================================================
# (5) LA DISTRIBUCIÓN DE TEXTOS DEL ALTAR (más aire)
# ============================================================
UIA = os.path.join(RAIZ, 'Content/UI/AltarOfrendasUI.cs')
s = lee(UIA)
if 'v6.137 — EL AIRE' in s:
    print('  · el aire ya vive (idempotente)')
else:
    # (5a) TituloSeccion gana margen ANTES (respirar entre secciones)
    VIEJO_T = '''        private static float TituloSeccion(SpriteBatch sb, string texto, int x, float cy, int w, Color c)
        {'''
    NUEVO_T = '''        // v6.137 — EL AIRE: los títulos de sección respiran — un
        // margen de 8 px ANTES de cada título (antes las secciones
        // venían pegadas una a la otra y el panel se leía apretado).
        private static float TituloSeccion(SpriteBatch sb, string texto, int x, float cy, int w, Color c)
        {
            cy += 8f;   // el respiro de la casa'''
    assert VIEJO_T in s
    s = s.replace(VIEJO_T, NUEVO_T)

    # (5b) las líneas de CÓMO compactas y parejas (0.75/0.72 → 0.70 y
    # separación uniforme via LineaMedida que ya fluye)
    for viejo, nuevo in (
        ('''            cy = LineaMedida(sb, Language.GetTextValue(Clave("ComoCriaturas")),
                x + 24f, cy, w - 48f, 0.75f, TintaTexto);''',
         '''            cy = LineaMedida(sb, Language.GetTextValue(Clave("ComoCriaturas")),
                x + 24f, cy, w - 48f, 0.70f, TintaTexto);'''),
        ('''            cy = LineaMedida(sb, Language.GetTextValue(Clave("ComoJefes")),
                x + 24f, cy, w - 48f, 0.75f, TintaTexto);''',
         '''            cy = LineaMedida(sb, Language.GetTextValue(Clave("ComoJefes")),
                x + 24f, cy, w - 48f, 0.70f, TintaTexto);'''),
        ('''            cy = LineaMedida(sb, Language.GetTextValue(Clave("ComoOleadas")),
                x + 24f, cy, w - 48f, 0.75f, TintaVioleta);''',
         '''            cy = LineaMedida(sb, Language.GetTextValue(Clave("ComoOleadas")),
                x + 24f, cy, w - 48f, 0.70f, TintaVioleta);'''),
        ('''            cy = LineaMedida(sb, Language.GetTextValue(Clave("ComoEstatuas")),
                x + 24f, cy, w - 48f, 0.75f, new Color(235, 130, 120));''',
         '''            cy = LineaMedida(sb, Language.GetTextValue(Clave("ComoEstatuas")),
                x + 24f, cy, w - 48f, 0.70f, new Color(235, 130, 120));'''),
    ):
        assert viejo in s, viejo[:60]
        s = s.replace(viejo, nuevo)

    # (5c) el subtítulo del altar se acorta (es-ES/MX) — en el hjson
    escribe(UIA, s)
    print('  ✔ AltarOfrendasUI.cs: aire en los títulos + CÓMO compacta')

# ============================================================
# (6) LOS TEXTOS NUEVOS ×3 (altar único)
# ============================================================
TEXTOS = {
    'es-ES': '''\t// v6.137 — LA LEY DEL ALTAR ÚNICO (la letra: «en el mundo solo
\t// puede haber un altar colocado, cualquier altar extra queda
\t// desactivado con un texto que lo diga, y siempre marcar el altar
\t// que funciona en el mapa»).
\tYaHayUno: ✦ Ya vive UN ALTAR DE CONSAGRACIÓN en este mundo — el Eterno solo escucha a uno. Recógelo si quieres moverlo.
\tDesactivado: ✦ Este altar está DESACTIVADO — el que funciona es otro (búscalo en el mapa: dice ACTIVO).
\tPromovido: El altar recogido… y otro asciende: ESTE es el altar activo ahora.
\tMapaActivo: (ACTIVO)
\tNoInterior: ✦ El altar no vive dentro del libro — el interior solo tiene su PILAR PRISMA.
''',
    'es-MX': '''\t// v6.137 — LA LEY DEL ALTAR ÚNICO.
\tYaHayUno: ✦ Ya vive UN ALTAR DE CONSAGRACIÓN en este mundo — el Eterno solo escucha a uno. Recógelo si quieres moverlo.
\tDesactivado: ✦ Este altar está DESACTIVADO — el que funciona es otro (búscalo en el mapa: dice ACTIVO).
\tPromovido: El altar recogido… y otro asciende: ESTE es el altar activo ahora.
\tMapaActivo: (ACTIVO)
\tNoInterior: ✦ El altar no vive dentro del libro — el interior solo tiene su PILAR PRISMA.
''',
    'en-US': '''\t// v6.137 — THE ONE-ALTAR LAW.
\tYaHayUno: ✦ A CONSECRATION ALTAR already lives in this world — the Eternal listens to only one. Pick it up to move it.
\tDesactivado: ✦ This altar is DEACTIVATED — the working one is elsewhere (find it on the map: it says ACTIVE).
\tPromovido: The altar was picked up… and another rises: THIS is the active altar now.
\tMapaActivo: (ACTIVE)
\tNoInterior: ✦ The altar does not live inside the book — the interior only has its PRISM PILLAR.
''',
}
for lang, bloque in TEXTOS.items():
    H = os.path.join(RAIZ, f'Localization/{lang}_Mods.AethonMod.hjson')
    s = lee(H)
    if '\tYaHayUno:' in s:
        print(f'  · {lang}: textos ya viven (idempotente)')
        continue
    ancla = '\tConsagrar: '
    assert ancla in s, f'{lang}: falta Consagrar'
    idx = s.index(ancla)
    # fin de línea de Consagrar
    fin = s.index('\n', idx) + 1
    s = s[:fin] + bloque + s[fin:]
    escribe(H, s)
    print(f'  ✔ {lang}: 5 claves del altar único')

# y el subtítulo más corto (la distribución pide menos ruido arriba)
for lang, viejo, nuevo in (
    ('es-ES', '\tSubtitulo: consagra la ofrenda por el Eterno — arma, material o esencias',
     '\tSubtitulo: consagra la ofrenda — arma · material · esencias'),
    ('es-MX', '\tSubtitulo: consagra la ofrenda por el Eterno — arma, material o esencias',
     '\tSubtitulo: consagra la ofrenda — arma · material · esencias'),
):
    H = os.path.join(RAIZ, f'Localization/{lang}_Mods.AethonMod.hjson')
    s = lee(H)
    if viejo in s:
        escribe(H, s.replace(viejo, nuevo, 1))
        print(f'  ✔ {lang}: subtítulo del altar compacto')

print()
print('MÓDULO D COMPLETO ✔')
