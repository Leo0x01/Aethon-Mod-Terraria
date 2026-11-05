#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
altar_coro_v698.py — EL ALTAR DEL CORO (v6.98).

LA LETRA: «el sprite creado para el arma Coro de los profundo es
perfecto para un altar, intenta mejorar el sprite y crea una animacion
para el, no borres el sprite original de coro de los profundos».

EL ALTAR NUEVO nace del CORO (el arma-pebetero de la Casa del Horror
Cósmico): su monolito con el VÓRTICE DEL CORO en el centro, sus dos
BRASEROS gemelos y su pedestal — MEJORADO con dos escalones más de
piedra azul (un altar de verdad apoyado en el suelo), runas de teal
que respiran en el frente y chispas que orbitan la corona.

LA ANIMACIÓN (4 marcos de 36×54 — la lámina 36×216 de siempre):
  · EL VÓRTICE late (su núcleo brilla 0.82 → 1.18 → 1.0 → 1.0)
  · LAS LLAMAS alternan (izquierda ↔ derecha, el fuelle del coro)
  · LAS RUNAS del pedestal respiran (encendidas / apagadas)
  · LAS DOS CHISPAS orbitan la corona (una teal, una fucsia)

El sprite ORIGINAL del Coro NO se toca (la regla): se LEE de la copia
de la carpeta SPRITES-FAVORITOS y se compone sobre lienzo nuevo.

TODO local, sin nube — la regla de la casa.
"""
import os
import sys

from PIL import Image

AQUI = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, AQUI)
from gen_altar_v695 import highlight

RAIZ = os.path.join(AQUI, '..', 'AethonMod')
CORO = os.path.join(RAIZ, '..', 'SPRITES-FAVORITOS', 'CoroDeLosProfundos.png')
TILE = os.path.join(RAIZ, 'Content', 'Tiles', 'AltarOfrendasTile.png')
TILE_HL = os.path.join(RAIZ, 'Content', 'Tiles', 'AltarOfrendasTile_Highlight.png')
ITEM = os.path.join(RAIZ, 'Content', 'Items', 'Altar', 'AltarOfrendasItem.png')

ANCHO, ALTO, MARCOS = 36, 54, 4
SUELO = 49          # la última fila dibujada (CoordinateHeights 16+16+18)

# LA PALETA DEL CORO (leída del sprite — la casa de verdad)
PIEDRA = (48, 78, 150)      # #304E96 el cuerpo azul del monolito
PIEDRA_CLARA = (96, 120, 240)  # #6078F0 la luz de la piedra
SOMBRA = (10, 12, 50)       # #0A0C32 el borde oscuro
VIOLETA = (144, 48, 216)    # #9030D8
FUCSIA = (240, 120, 216)    # #F078D8
TEAL = (72, 216, 168)       # #48D8A8
ORO = (216, 144, 48)        # #D89030


def _tono(px, k):
    """El píxel con el brillo multiplicado (el latido)."""
    r, g, b, a = px
    return (min(255, int(r * k)), min(255, int(g * k)),
            min(255, int(b * k)), a)


def _brillo_region(im, caja, k):
    """El latido de UNA región (la caja sube o baja su brillo)."""
    px = im.load()
    x0, y0, x1, y1 = caja
    for y in range(y0, y1):
        for x in range(x0, x1):
            if px[x, y][3] > 0:
                px[x, y] = _tono(px[x, y], k)


def _pedestal(im):
    """LOS DOS ESCALONES (la mejora del altar): el Coro ya trae su base
    azul (las filas 40-45); se le suman DOS escalones de piedra que
    apoyan en el suelo — con su contorno oscuro, su filo iluminado y
    TRES runas de teal en el frente."""
    px = im.load()
    # el escalón medio (fila 46-47): tan ancho como la base del Coro
    for y in (46, 47):
        for x in range(1, 35):
            px[x, y] = PIEDRA if y == 46 else (34, 60, 120, 255)
    # el escalón del suelo (48-49): TODO el ancho del mueble
    for y in (48, SUELO):
        for x in range(0, 36):
            px[x, y] = PIEDRA if y == 48 else (24, 44, 96, 255)
    # los contornos oscuros de los escalones (el borde Terraria)
    for x in range(1, 35):
        if px[x, 46][3] == 0:
            px[x, 46] = SOMBRA + (255,)
    for x in range(0, 36):
        if px[x, 48][3] == 0:
            px[x, 48] = SOMBRA + (255,)
    px[0, 46] = SOMBRA + (255,); px[34, 46] = SOMBRA + (255,)
    px[0, 47] = SOMBRA + (255,); px[34, 47] = SOMBRA + (255,)
    px[0, 48] = SOMBRA + (255,); px[35, 48] = SOMBRA + (255,)
    px[0, 49] = SOMBRA + (255,); px[35, 49] = SOMBRA + (255,)
    # el filo iluminado del escalón del suelo (la luz del coro cae)
    for x in (2, 5, 8, 33):
        px[x, 48] = PIEDRA_CLARA + (255,)
    # los cierres de ORO del pedestal (los ganchos del altar)
    for x in (4, 31):
        px[x, 47] = ORO + (255,)
        px[x, 47 - 1] = (240, 190, 110, 255)


def _runas(im, encendidas):
    """LAS TRES RUNAS del frente del pedestal (la salmodia del coro)."""
    px = im.load()
    posiciones = [(14, 46), (18, 46), (22, 46)]
    for i, (x, y) in enumerate(posiciones):
        viva = encendidas[i]
        color = TEAL if viva else (28, 90, 80)
        px[x, y] = color + (255,)
        px[x + 1, y] = color + (255,)
        if viva and i == 1:
            px[x, y - 1] = (170, 255, 225, 255)


CHISPAS = [
    # (x, y) por marco — la teal orbita la corona, la fucsia el vórtice
    [(6, 4), (29, 6), (30, 22), (5, 20)],
    [(17, 8), (17, 8), (17, 8), (17, 8)],      # la fucsia: el ojo fijo
]


def _chispas(im, marco):
    px = im.load()
    x, y = CHISPAS[0][marco]
    px[x, y] = TEAL + (255,)
    px[x + 1, y] = (150, 255, 220, 255)
    if marco % 2 == 0:
        px[x, y - 1] = (150, 255, 220, 255)
    # la fucsia: un destello que mira (el ojo del coro parpadea)
    if marco in (0, 2):
        cx, cy = CHISPAS[1][0]
        px[cx, cy] = FUCSIA + (255,)
        px[cx + 1, cy] = (255, 200, 245, 255)


def componer():
    """LA LÁMINA COMPLETA: 4 marcos × 36×54 apilados (36×216)."""
    coro = Image.open(CORO).convert('RGBA')
    assert coro.size == (34, 46), f'el Coro cambió: {coro.size}'
    lamina = Image.new('RGBA', (ANCHO, ALTO * MARCOS), (0, 0, 0, 0))
    # los latidos del vórtice por marco
    latidos = [1.0, 1.18, 1.0, 0.82]
    # el fuelle de las llamas: (izq, der) por marco
    fuegos = [(1.22, 0.86), (1.0, 1.0), (0.86, 1.22), (1.12, 1.12)]
    # las runas: encendidas en 0 y 2 (la respiración)
    runas_on = [[True, True, True], [False, False, False],
                [True, False, True], [False, True, False]]
    for marco in range(MARCOS):
        fr = Image.new('RGBA', (ANCHO, ALTO), (0, 0, 0, 0))
        fr.paste(coro, (1, 0), coro)
        # EL VÓRTICE late (la espiral central: x 10..24, y 10..29)
        _brillo_region(fr, (11, 10, 24, 30), latidos[marco])
        # LAS LLAMAS alternan (los dos braseros)
        _brillo_region(fr, (2, 30, 12, 40), fuegos[marco][0])
        _brillo_region(fr, (23, 30, 33, 40), fuegos[marco][1])
        # EL PEDESTAL (la mejora) + sus runas + las chispas
        _pedestal(fr)
        _runas(fr, runas_on[marco])
        _chispas(fr, marco)
        lamina.paste(fr, (0, marco * ALTO))
    return lamina


def main():
    lamina = componer()
    lamina.save(TILE)
    # EL HIGHLIGHT: la silueta amarilla del marco 0 (el ratón lo elige)
    fr0 = lamina.crop((0, 0, ANCHO, ALTO))
    hl = highlight(fr0)
    hl.save(TILE_HL)
    # EL ITEM: el marco 0 del altar SIN reescalado (fidelidad 1:1)
    # v2 — EL ITEM ES EL ALTAR MISMO (el marco 0 recortado al área
    # viva 36×50, SIN reescalado: fidelidad de píxel 1:1 — el icono del
    # inventario es el altar de verdad, no una reducción)
    item = fr0.crop((0, 0, ANCHO, 50))
    item.save(ITEM)
    print(f'ALTAR DEL CORO — {MARCOS} marcos {ANCHO}x{ALTO} → {TILE}')
    print(f'  highlight {hl.size} → {TILE_HL}')
    print(f'  item {item.size} → {ITEM}')
    # el ciclo GIF para VERLO (el preview de la casa)
    frames = []
    for m in range(MARCOS):
        fr = lamina.crop((0, m * ALTO, ANCHO, (m + 1) * ALTO))
        fr = fr.resize((ANCHO * 6, ALTO * 6), Image.NEAREST)
        base = Image.new('RGBA', fr.size, (24, 24, 34, 255))
        base.paste(fr, (0, 0), fr)
        frames.append(base.convert('P', palette=Image.ADAPTIVE))
    gif = os.path.join('/tmp', 'altar_coro_ciclo.gif')
    frames[0].save(gif, save_all=True, append_images=frames[1:],
                   duration=140, loop=0)
    print(f'  el ciclo → {gif}')


if __name__ == '__main__':
    main()
