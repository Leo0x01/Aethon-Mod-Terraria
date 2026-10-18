#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
hjson_v650130.py — LAS CLAVES NUEVAS DE LA v6.50.130 (es-ES + es-MX + en-US).

(1) Items.BrumaDelCrater (DisplayName + Tooltip) — el item de bruma.
(2) Tiles.BrumaAltar.MapEntry — la tile de bruma colocada.

La regla de la casa: hjson SOLO por script Python (la herramienta de
edición normaliza tabs y destroza archivos grandes — la lección .65).
"""
import io
import os
import sys

BASE = os.path.join(os.path.dirname(__file__), '..', 'AethonMod', 'Localization')

BLOQUE = {
    'es-ES': (
        "\t// v6.50.130 — LA BRUMA DEL CRÁTER (el item invisible que ES la\n"
        "\t// bruma: el altar principal portátil de la corrupción)\n"
        "\tBrumaDelCrater: {\n"
        "\t\tDisplayName: Bruma del cráter\n"
        "\t\tTooltip:\n"
        "\t\t\t'''\n"
        "\t\t\tLa bruma del minibioma, EN PERSONA: tómala del cráter y COLOCA\n"
        "\t\t\tel altar donde quieras.\n"
        "\t\t\tCualquier cosa que se lance a la bruma colocada se pudre en\n"
        "\t\t\tEsencia corrupta — y las ARMAS ganan la etiqueta Corrupto.\n"
        "\t\t\tJunto a la bruma, el apartado de creación permite corromper\n"
        "\t\t\tarmas y convertirlo TODO en Esencia corrupta, sin lanzar nada.\n"
        "\t\t\t'''\n"
        "\t}\n"
        "\n"),
    'es-MX': (
        "\t// v6.50.130 — LA BRUMA DEL CRÁTER (el item invisible que ES la\n"
        "\t// bruma: el altar principal portátil de la corrupción)\n"
        "\tBrumaDelCrater: {\n"
        "\t\tDisplayName: Bruma del cráter\n"
        "\t\tTooltip:\n"
        "\t\t\t'''\n"
        "\t\t\tLa bruma del minibioma, EN PERSONA: tómala del cráter y COLOCA\n"
        "\t\t\tel altar donde quieras.\n"
        "\t\t\tCualquier cosa que se lance a la bruma colocada se pudre en\n"
        "\t\t\tEsencia corrupta — y las ARMAS ganan la etiqueta Corrupto.\n"
        "\t\t\tJunto a la bruma, el apartado de creación permite corromper\n"
        "\t\t\tarmas y convertirlo TODO en Esencia corrupta, sin lanzar nada.\n"
        "\t\t\t'''\n"
        "\t}\n"
        "\n"),
    'en-US': (
        "\t// v6.50.130 — THE CRATER MIST (the invisible item that IS the\n"
        "\t// mist: the portable corruption altar)\n"
        "\tBrumaDelCrater: {\n"
        "\t\tDisplayName: Crater Mist\n"
        "\t\tTooltip:\n"
        "\t\t\t'''\n"
        "\t\t\tThe minibiome's mist, IN THE FLESH: take it from the crater\n"
        "\t\t\tand PLACE the altar wherever you want.\n"
        "\t\t\tAnything thrown into the placed mist rots into Corrupted\n"
        "\t\t\tEssence — and WEAPONS earn the Corrupted tag.\n"
        "\t\t\tNext to the mist, the crafting menu corrupts weapons and\n"
        "\t\t\tturns EVERYTHING into Corrupted Essence, no throwing needed.\n"
        "\t\t\t'''\n"
        "\t}\n"
        "\n"),
}

TILE = {
    'es-ES': "Tiles.BrumaAltar.MapEntry: Bruma del cráter\n",
    'es-MX': "Tiles.BrumaAltar.MapEntry: Bruma del cráter\n",
    'en-US': "Tiles.BrumaAltar.MapEntry: Crater Mist\n",
}

def tocar(nombre):
    ruta = os.path.join(BASE, f'{nombre}_Mods.AethonMod.hjson')
    with io.open(ruta, 'r', encoding='utf-8') as f:
        texto = f.read()

    fallos = []

    # === (1) EL ITEM: tras el bloque ItemCorrupto ===
    if 'BrumaDelCrater:' in texto:
        print(f'  {nombre}: BrumaDelCrater YA EXISTÍA (skip)')
    else:
        ancla = '\tArmaCorruptaTag:'
        pos = texto.find(ancla)
        if pos < 0:
            fallos.append(f'{nombre}: no hallé el ancla ArmaCorruptaTag')
        else:
            texto = texto[:pos] + BLOQUE[nombre] + texto[pos:]
            print(f'  {nombre}: Items.BrumaDelCrater OK')

    # === (2) LA TILE: tras AltarDeLaCaida.MapEntry ===
    if 'Tiles.BrumaAltar.MapEntry' in texto:
        print(f'  {nombre}: Tiles.BrumaAltar YA EXISTÍA (skip)')
    else:
        ancla = 'Tiles.AltarDeLaCaida.MapEntry'
        pos = texto.find(ancla)
        if pos < 0:
            fallos.append(f'{nombre}: no hallé el ancla AltarDeLaCaida.MapEntry')
        else:
            fin = texto.find('\n', pos) + 1
            texto = texto[:fin] + TILE[nombre] + texto[fin:]
            print(f'  {nombre}: Tiles.BrumaAltar.MapEntry OK')

    if not fallos:
        with io.open(ruta, 'w', encoding='utf-8', newline='\n') as f:
            f.write(texto)
    return fallos

todos = []
for nombre in ('es-ES', 'es-MX', 'en-US'):
    todos += tocar(nombre)

if todos:
    for f in todos:
        print(f'  ✗ {f}')
    sys.exit(1)
print('LISTO — claves nuevas ×3 idiomas')
