#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
hjson_v686.py — LA LETRA DE LA v6.86 EN LOS TRES IDIOMAS.
Actualiza es-MX, es-ES (mismo español) y en-US (inglés):
  · Despertar 100 (hitos/camino redistribuidos + Aethon + contadores de la Ira)
  · EL LIBRO CORRUPTO (puerta del 20)
  · EL ALTAR DE OFRENDAS (puntos de ofrenda)
  · EL ÁRBOL DE HABILIDADES (nodos + ventana)
  · El escuadrón fortalecido (la mejora masiva)
"""
import sys

# ================================================================
#  LOS TEXTOS POR IDIOMA
# ================================================================
ES = {
  # --- Despertar: hitos redistribuidos (10→100) ---
  'HITOS': """        HitoMejora1: ✧ ¡DESPERTAR 10: la MEJORA 1 de los ojos desbloqueada!
        HitoSol: ✧ ¡DESPERTAR 15: DOS OJOS mejorados crean un SOL RÚNICO de 1 anillo! (2/min)
        HitoMascota: ✧ ¡DESPERTAR 20: EL GRIMORIO TE ESCUPE AL MINIGRIMORIO como mascota!
        HitoAgujeros: ✧ ¡DESPERTAR 30: CINCO OJOS mejorados abren un AGUJERO RÚNICO al azar! (1/min)
        HitoMejora2: ✧ ¡DESPERTAR 40: la MEJORA 2 de los ojos desbloqueada!
        HitoSol2: ✧ ¡DESPERTAR 50: el sol gana su SEGUNDO anillo y nace 3 veces por minuto!
        HitoAscendidos: ✧ ¡DESPERTAR 60: los agujeros ASCIENDEN — aún más daño!
        HitoMejora3: ✧ ¡DESPERTAR 70: la MEJORA 3 de los ojos desbloqueada!
        HitoSol3: ✧ ¡DESPERTAR 75: el sol de TRES anillos, 4 veces por minuto!
        HitoMejora4: ✧ ¡DESPERTAR 80: la MEJORA FINAL de los ojos!
        HitoAgujeros2: ✧ ¡DESPERTAR 85: los agujeros negros nacen DOS veces por minuto!
        HitoSupremo: ✧ ¡DESPERTAR 90: OCHO OJOS con la mejora 4 abren el SUPREMO o el SUPREMO AURORA!
        HitoMaximo: ✧✧ ¡DESPERTAR 100: LA IRA DE UN SER EXTERIOR despierta del todo… y ahora exige su caza: el Señor de la Luna 5 veces y Aethon 1. EL HAZ responde!""",
  'CAMINO': """        CaminoMejora1: la MEJORA 1 de los ojos (+50% de daño al escuadrón entero)
        CaminoSol: el SOL RÚNICO de 1 anillo (2 ojos mejorados, 2/min)
        CaminoMascota: LA MASCOTA — el grimorio escupe al minigrimorio
        CaminoAgujeros: los AGUJEROS NEGROS RÚNICOS (5 ojos mejorados, 1/min)
        CaminoMejora2: la MEJORA 2 de los ojos
        CaminoSol2: el sol de 2 ANILLOS (3/min)
        CaminoAscendidos: los agujeros ASCENDIDOS (más daño)
        CaminoMejora3: la MEJORA 3 de los ojos
        CaminoSol3: el sol de 3 ANILLOS (4/min)
        CaminoMejora4: la MEJORA FINAL de los ojos
        CaminoAgujeros2: los agujeros 2/min
        CaminoSupremo: EL SUPREMO/AURORA (8 ojos con la mejora 4)
        CaminoMaximo: LA IRA DE UN SER EXTERIOR — el círculo del escuadrón invoca EL HAZ (daño 1000 + mágico + 50% invocación)""",
  'MEJORAS': "        MejorasSelladas: ✦ Las mejoras nacen con EL DESPERTAR (10/40/70/80). Actual: {0}.",
  'JEFEAETHON': "        JefeAethon: Aethon, el fragmento de un recuerdo lejano",
  'CIELOACT': "        CieloDespertarActual: Despertar {0}/100",
  'LIBRO': """        // v6.86 — EL LIBRO CORRUPTO (la puerta del 20): la voz del
        // grimorio al recogerlo, al devorarlo y el aviso de la puerta.
        LibroEnInventario: (el libro se estremece) ESO lo conozco. Un LIBRO CORRUPTO… déjalo caer al suelo, portador. QUIERO probarlo.
        LibroDevorado: (páginas que se lamen) El LIBRO CORRUPTO ya es mío: la puerta del despertar 20 NACE PAGA. Para siempre.
        PuertaLibro: ✦ La mesa está completa… pero la puerta del 20 exige su LIBRO CORRUPTO: consíguelo (1% rompiendo libros o abriendo cofres en la mazmorra · 50% la Muralla) y déjalo caer delante de mí.
        ReqLibroDevorado: Libro Corrupto: DEVORADO
        ReqLibroPendiente: Libro Corrupto: consíguelo y déjalo caer delante del grimorio
        CreditoPuntos: ✦ El altar paga {0} de las {1} esencias del peldaño (puntos de ofrenda).""",
  # --- Armas: la mejora masiva ---
  'ESCUCHE': "        EscuadronFortalecido: ✧ ¡EL ESCUADRÓN ENTERO SE FORTALECE! ({0} ojos · mejora {1}: +50% de daño cada uno)",
  # --- Items ---
  'ITEM_LIBRO': """        LibroCorrupto: {
                DisplayName: Libro Corrupto
                Tooltip:
                        '''
                        ✦ Un libro con la misma forma que los de la mazmorra… pero NEGRO.
                        · Se consigue: 1% rompiendo libros en la mazmorra · 1% abriendo sus cofres · 50% la Muralla de Carne
                        · Solo 1 por jugador — y el grimorio lo quiere EN EL SUELO
                        · Devorado por debajo del despertar 20: la puerta del 20 queda paga PARA SIEMPRE
                        '''
        }""",
  'ITEM_ALTAR': """        AltarOfrendasItem: {
                DisplayName: Altar de Ofrendas
                Tooltip:
                        '''
                        ✦ El altar de las cosas que SOBRAN (investigación: Heroic Weapons)
                        · Colócalo y hazle clic derecho: el grimorio devora lo que NO necesita y NO está corrupto
                        · Lo devorado se vuelve PUNTOS DE OFRENDA: pagan hasta el 30% de las esencias del peldaño
                        '''
        }""",
  'ITEM_CODICE': """        CodiceHabilidades: {
                DisplayName: El Códice de Habilidades
                Tooltip:
                        '''
                        ✦ El árbol del portador (investigación: Umbra + Path of Terraria)
                        · Clic derecho (o izquierdo): abre EL ÁRBOL DE HABILIDADES
                        · Los puntos nacen del despertar: 3 + 1 cada 5 niveles
                        · Clic para asignar · clic derecho para devolver (sin romper el árbol)
                        '''
        }""",
  # --- Tiles ---
  'TILE_ALTAR': "        AltarOfrendasTile.MapEntry: Altar de Ofrendas",
  # --- Sección Altar ---
  'ALTAR': """Altar: {
        Titulo: EL ALTAR DE OFRENDAS
        Subtitulo: Lo que sobra alimenta el despertar
        PuntosLibro: Puntos del grimorio: {0}
        SinLibro: (sin el grimorio encima)
        SlotVacio: relleno
        EtiquetaRelleno: EL RELLENO — lo que NO se necesita y NO está corrupto
        FiltroRelleno: Ni monedas, ni esencias, ni requisitos, ni favoritos. Vale ≥ 5 de plata
        PuntosVivos: Vale {0} puntos
        CreditoVivo: {0} esencias del peldaño ya pagas
        Ofrendar: OFRENDAR AL GRIMORIO
        Rechazado: ✦ El grimorio rechaza eso: no es relleno válido (requisito, corrupto, sagrado o sin valor).
        OfrendaAceptada: ✦ El grimorio devora la ofrenda: +{0} puntos (total {1}).
}""",
  # --- Sección Arbol ---
  'ARBOL': """Arbol: {
        Titulo: EL ÁRBOL DE HABILIDADES
        Puntos: {0} libres · {1}/{2} gastados
        Ayuda: Clic: asignar · clic derecho: devolver · arrastra el vacío para mover · rueda para el zoom
        Reset: REINICIAR EL ÁRBOL
        ResetSeguro: ¿SEGURO? (otra vez)
        ResetHecho: ✦ El árbol vuelve a su semilla: todos los puntos devueltos.
        NivelDe: Nivel {0}/{1}
        Nodo0: El Despertar del Portador
        Desc0: El ancla del árbol — siempre viva, siempre gratis. Todo crece desde aquí.
        Nodo1: Filo del Eco
        Desc1: +8% de daño (todo tipo) por nivel.
        Nodo2: Cuerpo de Tinta
        Desc2: +20 de vida máxima por nivel.
        Nodo3: Mente Despierta
        Desc3: +40 de maná máximo por nivel.
        Nodo4: Colmillo Crítico
        Desc4: +4% de probabilidad de crítico por nivel.
        Nodo5: Plumas Ligeras
        Desc5: +8% de velocidad de movimiento por nivel.
        Nodo6: Piel de Tinta
        Desc6: +4 de defensa por nivel.
        Nodo7: Tinta Barata
        Desc7: −10% de coste de maná por nivel.
        Nodo8: Corazón de Tinta
        Desc8: +1 vida/s de regeneración por nivel.
        Nodo9: EL ECO DE AETHON
        Desc9: KEYSTONE: +1 espacio de minion. El árbol entero cabe en el despertar 100.
}""",
}

EN = {
  'HITOS': """        HitoMejora1: ✧ AWAKENING 10: the eyes' UPGRADE 1 unlocked!
        HitoSol: ✧ AWAKENING 15: TWO UPGRADED EYES forge a RUNIC SUN with 1 ring! (2/min)
        HitoMascota: ✧ AWAKENING 20: THE GRIMOIRE SPITS OUT THE MINI-GRIMOIRE as your pet!
        HitoAgujeros: ✧ AWAKENING 30: FIVE UPGRADED EYES open a random RUNIC BLACK HOLE! (1/min)
        HitoMejora2: ✧ AWAKENING 40: the eyes' UPGRADE 2 unlocked!
        HitoSol2: ✧ AWAKENING 50: the sun gains its SECOND ring and rises 3 times per minute!
        HitoAscendidos: ✧ AWAKENING 60: the black holes ASCEND — even more damage!
        HitoMejora3: ✧ AWAKENING 70: the eyes' UPGRADE 3 unlocked!
        HitoSol3: ✧ AWAKENING 75: the THREE-RING sun, 4 times per minute!
        HitoMejora4: ✧ AWAKENING 80: the FINAL upgrade of the eyes!
        HitoAgujeros2: ✧ AWAKENING 85: black holes are born TWICE per minute!
        HitoSupremo: ✧ AWAKENING 90: EIGHT EYES with upgrade 4 open THE SUPREME or the SUPREME AURORA!
        HitoMaximo: ✧✧ AWAKENING 100: THE WRATH OF AN OUTER BEING fully awakens… and now demands its hunt: Moon Lord 5 times and Aethon once. THE BEAM answers!""",
  'CAMINO': """        CaminoMejora1: the eyes' UPGRADE 1 (+50% damage to the whole squad)
        CaminoSol: the RUNIC SUN with 1 ring (2 upgraded eyes, 2/min)
        CaminoMascota: THE PET — the grimoire spits out the mini-grimoire
        CaminoAgujeros: the RUNIC BLACK HOLES (5 upgraded eyes, 1/min)
        CaminoMejora2: the eyes' UPGRADE 2
        CaminoSol2: the sun with 2 RINGS (3/min)
        CaminoAscendidos: the ASCENDED holes (more damage)
        CaminoMejora3: the eyes' UPGRADE 3
        CaminoSol3: the sun with 3 RINGS (4/min)
        CaminoMejora4: the FINAL upgrade of the eyes
        CaminoAgujeros2: holes 2/min
        CaminoSupremo: THE SUPREME/AURORA (8 eyes with upgrade 4)
        CaminoMaximo: THE WRATH OF AN OUTER BEING — the squad's circle summons THE BEAM (damage 1000 + magic + 50% summon)""",
  'MEJORAS': "        MejorasSelladas: ✦ Upgrades are born with THE AWAKENING (10/40/70/80). Current: {0}.",
  'JEFEAETHON': "        JefeAethon: Aethon, the fragment of a distant memory",
  'CIELOACT': "        CieloDespertarActual: Awakening {0}/100",
  'LIBRO': """        // v6.86 — THE CORRUPT BOOK (the gate of 20).
        LibroEnInventario: (the book shudders) I KNOW this one. A CORRUPT BOOK… drop it on the ground, bearer. I WANT a taste.
        LibroDevorado: (pages licking themselves) The CORRUPT BOOK is mine: the gate of awakening 20 IS BORN PAID. Forever.
        PuertaLibro: ✦ The table is complete… but the gate of 20 demands its CORRUPT BOOK: get one (1% breaking books or opening chests in the dungeon · 50% the Wall) and drop it before me.
        ReqLibroDevorado: Corrupt Book: DEVOURED
        ReqLibroPendiente: Corrupt Book: get one and drop it before the grimoire
        CreditoPuntos: ✦ The altar pays {0} of the {1} essences of the rung (offering points).""",
  'ESCUCHE': "        EscuadronFortalecido: ✧ THE WHOLE SQUAD IS FORTIFIED! ({0} eyes · upgrade {1}: +50% damage each)",
  'ITEM_LIBRO': """        LibroCorrupto: {
                DisplayName: Corrupt Book
                Tooltip:
                        '''
                        ✦ A book shaped like the dungeon's… but BLACK.
                        · Found: 1% breaking books in the dungeon · 1% opening its chests · 50% the Wall of Flesh
                        · Only 1 per player — and the grimoire wants it ON THE GROUND
                        · Devoured below awakening 20: that gate is paid FOREVER
                        '''
        }""",
  'ITEM_ALTAR': """        AltarOfrendasItem: {
                DisplayName: Offering Altar
                Tooltip:
                        '''
                        ✦ The altar of things that SPARE (research: Heroic Weapons)
                        · Place it and right-click: the grimoire devours what it does NOT need and is NOT corrupted
                        · The devoured becomes OFFERING POINTS: they pay up to 30% of the rung's essences
                        '''
        }""",
  'ITEM_CODICE': """        CodiceHabilidades: {
                DisplayName: The Skills Codex
                Tooltip:
                        '''
                        ✦ The bearer's tree (research: Umbra + Path of Terraria)
                        · Right-click (or left): opens THE SKILL TREE
                        · Points are born from the awakening: 3 + 1 every 5 levels
                        · Click to allocate · right-click to refund (without breaking the tree)
                        '''
        }""",
  'TILE_ALTAR': "        AltarOfrendasTile.MapEntry: Offering Altar",
  'ALTAR': """Altar: {
        Titulo: THE OFFERING ALTAR
        Subtitulo: What's left over feeds the awakening
        PuntosLibro: Grimoire points: {0}
        SinLibro: (no grimoire on you)
        SlotVacio: filler
        EtiquetaRelleno: THE FILLER — what is NOT needed and NOT corrupted
        FiltroRelleno: No coins, no essences, no requirements, no favorites. Worth ≥ 5 silver
        PuntosVivos: Worth {0} points
        CreditoVivo: {0} essences of the rung already paid
        Ofrendar: OFFER TO THE GRIMORIO
        Rechazado: ✦ The grimoire refuses that: not valid filler (a requirement, corrupted, sacred or worthless).
        OfrendaAceptada: ✦ The grimoire devours the offering: +{0} points (total {1}).
}""",
  'ARBOL': """Arbol: {
        Titulo: THE SKILL TREE
        Puntos: {0} free · {1}/{2} spent
        Ayuda: Click: allocate · right-click: refund · drag the void to pan · wheel to zoom
        Reset: RESET THE TREE
        ResetSeguro: SURE? (again)
        ResetHecho: ✦ The tree returns to its seed: all points refunded.
        NivelDe: Level {0}/{1}
        Nodo0: The Bearer's Awakening
        Desc0: The anchor of the tree — always alive, always free. Everything grows from here.
        Nodo1: Echo's Edge
        Desc1: +8% damage (all types) per level.
        Nodo2: Ink Body
        Desc2: +20 max life per level.
        Nodo3: Awakened Mind
        Desc3: +40 max mana per level.
        Nodo4: Critical Fang
        Desc4: +4% critical chance per level.
        Nodo5: Light Feathers
        Desc5: +8% movement speed per level.
        Nodo6: Ink Skin
        Desc6: +4 defense per level.
        Nodo7: Cheap Ink
        Desc7: −10% mana cost per level.
        Nodo8: Ink Heart
        Desc8: +1 life/s regeneration per level.
        Nodo9: THE ECHO OF AETHON
        Desc9: KEYSTONE: +1 minion slot. The whole tree fits at awakening 100.
}""",
}

FILES = {
    'es-MX': ES, 'es-ES': ES, 'en-US': EN,
}


def reemplazar_bloque(lineas, inicio_marcador, fin_marcador, nuevo):
    """Reemplaza el bloque [inicio_marcador..fin_marcador) por nuevo."""
    ini = fin = -1
    for i, l in enumerate(lineas):
        if inicio_marcador in l:
            ini = i
            break
    if ini < 0:
        return None
    for j in range(ini + 1, len(lineas)):
        if fin_marcador in lineas[j]:
            fin = j
            break
    if fin < 0:
        fin = ini + 1
    return lineas[:ini] + [nuevo + "\n"] + lineas[fin:]


def main():
    ok = True
    for lang, T in FILES.items():
        ruta = f'Localization/{lang}_Mods.AethonMod.hjson'
        with open(ruta, encoding='utf-8') as f:
            lineas = f.readlines()

        # 1) HITOS: desde 'HitoMejora1:' hasta 'CaminoMaximo:' (incluye Caminos viejos:
        #    el bloque viejo va de HitoMejora1 a CaminoMaximo — pero entre Hito y Camino
        #    hay 'Escupidizo' y los comentarios de la caza. Reemplazo en DOS tandas:
        #    a) HitoMejora1..HitoMaximo ; b) CaminoMejora1..CaminoMaximo.
        lineas = reemplazar_bloque(lineas, 'HitoMejora1:', 'CaminoMejora1:', T['HITOS'])
        if lineas is None: print(f'{lang}: FALLO hitos'); ok = False; continue
        # (el bloque de hitos ya no contiene CaminoMejora1: la tanda a comió hasta ahí)
        lineas = reemplazar_bloque(lineas, 'CaminoMejora1:', 'Escupidizo:', T['CAMINO'])
        if lineas is None: print(f'{lang}: FALLO camino'); ok = False; continue

        # 2) MejorasSelladas (una línea)
        for i, l in enumerate(lineas):
            if 'MejorasSelladas:' in l:
                lineas[i] = T['MEJORAS'] + "\n"
                break

        # 3) JefeAethon tras JefeSenorDeLaLuna
        for i, l in enumerate(lineas):
            if 'JefeSenorDeLaLuna:' in l:
                lineas.insert(i + 1, T['JEFEAETHON'] + "\n")
                break

        # 4) CieloDespertarActual
        for i, l in enumerate(lineas):
            if 'CieloDespertarActual:' in l:
                lineas[i] = T['CIELOACT'] + "\n"
                break

        # 5) LIBRO: insertar tras 'ReqJefeViva:' línea
        idx = -1
        for i, l in enumerate(lineas):
            if 'ReqJefeViva:' in l:
                idx = i
                break
        if idx < 0:
            print(f'{lang}: FALLO ReqJefeViva'); ok = False; continue
        lineas.insert(idx + 1, T['LIBRO'] + "\n")

        # 6) EscuadronFortalecido tras OjoFortalecido
        for i, l in enumerate(lineas):
            if 'OjoFortalecido:' in l:
                lineas.insert(i + 1, T['ESCUCHE'] + "\n")
                break

        # 7) ITEMS: tras 'Items: {' — los tres items
        for i, l in enumerate(lineas):
            if l.startswith('Items: {'):
                lineas.insert(i + 1, "\n" + T['ITEM_LIBRO'] + "\n" + T['ITEM_ALTAR'] + "\n" + T['ITEM_CODICE'] + "\n")
                break

        # 8) TILE: tras BrumaAltarTile.MapEntry
        for i, l in enumerate(lineas):
            if 'BrumaAltarTile.MapEntry' in l:
                lineas.insert(i + 1, T['TILE_ALTAR'] + "\n")
                break

        # 9) Secciones Altar + Arbol: al final del archivo
        lineas.append(T['ALTAR'] + "\n")
        lineas.append(T['ARBOL'] + "\n")

        with open(ruta, 'w', encoding='utf-8') as f:
            f.writelines(lineas)
        print(f'{lang}: OK')
    sys.exit(0 if ok else 1)


if __name__ == '__main__':
    main()
