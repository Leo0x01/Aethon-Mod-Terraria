#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_fauce_v650124.py — LA SIMULACIÓN DE LA FAUCE (el mock de la casa).

Simula EXCAVARCARTER v6.50.124 sobre terreno sintético (misma lógica,
mismas fases A-D, mismo azar sembrado) y renderiza el corte 2D:

  · EL CUENCO excavado (radio 11, borde a medias)
  · LA TINTA QUEMADA (obsidiana esparcida + el aro)
  · LAS DOS ESCALERAS (rampas 45° con tiles en pendiente — caminables)
  · LA GARGANTA (los dos colmillos grandes ±3 con punta /\\ + el lecho ±2)
  · EL LIBRO DORMIDO flotando en el centro (ojo cerrado, Zzz)
  · EL CAMINO (la línea verde: la marcha garantizada borde→fondo→libro)

Render 2 tiles = 1 px… no: 16 px por tile para ver las pendientes.
Salida: tools/tools/v650124_fauce_mock.png
"""
import math
import os
import random

from PIL import Image, ImageDraw

SEMILLA = 20261015
CX, CY = 60, 40          # el impacto (tiles)
RADIO = 11
ANCHO, ALTO = 108, 62    # la ventana del render (tiles)

# el terreno sintético: superficie en y=40 (con 1-2 tiles de ondulación), piedra debajo
TERRENO = {}


def tile(x, y):
    return TERRENO.setdefault((x, y), {'sol': False, 'tipo': None, 'slope': 0})


def es_casa(t):
    return False  # terreno sintético: sin casas


def poner_obsidiana(x, y):
    t = tile(x, y)
    if t['sol'] and es_casa(t):
        return False
    t['sol'] = True
    t['tipo'] = 'obsidiana'
    t['slope'] = 0
    return True


def pender(x, y, slope):
    if not poner_obsidiana(x, y):
        return False
    tile(x, y)['slope'] = slope
    return True


def base_solida(x, y_desde, y_hasta):
    for y in range(y_desde, y_hasta + 1):
        t = TERRENO.get((x, y))
        if t and t['sol'] and not es_casa(t):
            return y - 1
    return -1


def poner_colmillo(bx, y_aire, altura, ancho_base, tope_y=None):
    if y_aire < 0:
        return False
    toco = False
    y_punta = y_aire
    for h in range(altura):
        y = y_aire - h
        if tope_y is not None and y <= tope_y:
            break
        f = (h + 1) / altura
        ancho = max(2, math.ceil(ancho_base * (1 - f)))
        x0 = bx - ancho // 2
        for dx in range(ancho):
            toco |= poner_obsidiana(x0 + dx, y)
        y_punta = y
    for dx in range(2):  # LA PUNTA /\
        x = bx - 1 + dx
        t = TERRENO.get((x, y_punta))
        if t and t['sol']:
            t['slope'] = 1 if dx == 0 else 2   # 1=DownLeft 2=DownRight
            toco = True
    return toco


def main():
    rng = random.Random(SEMILLA)

    # === EL TERRENO SINTÉTICO ===
    for x in range(ANCHO):
        sup = CY + (1 if x % 7 == 3 else 0) - (1 if x % 11 == 5 else 0)
        for y in range(ALTO):
            if y >= sup:
                t = tile(x, y)
                t['sol'] = True
                t['tipo'] = 'roca'

    # === FASE A — EL CUENCO ===
    for x in range(CX - RADIO, CX + RADIO + 1):
        for y in range(CY - RADIO, CY + RADIO + 1):
            d = math.sqrt((x - CX) ** 2 + (y - CY) ** 2)
            if d > RADIO:
                continue
            t = tile(x, y)
            if t['sol'] and (d <= 8 or rng.random() < 0.5):
                t['sol'] = False
                t['tipo'] = None
            # FASE B — LA TINTA (todo obsidiana) + EL ARO
            if not t['sol'] and d >= 2.5 and y > CY + 2 and abs(x - CX) > 1:
                dentro = 1 - d / RADIO
                if rng.random() < 0.26 + 0.34 * dentro:
                    poner_obsidiana(x, y)
            elif t['sol'] and y >= CY and d > 8 and rng.random() < 0.35:
                t['tipo'] = 'obsidiana'

    # === FASE C — LAS DOS ESCALERAS ===
    for lado in (-1, 1):
        for i in range(9):
            x = CX + lado * (12 - i)
            ys = CY + i
            pender(x, ys, 1 if lado < 0 else 2)
            poner_obsidiana(x, ys + 1)
            poner_obsidiana(x, ys + 2)
            for y in range(ys - 3, ys):
                t = TERRENO.get((x, y))
                if t and t['sol']:
                    t['sol'] = False
                    t['tipo'] = None

    # === FASE D — LA GARGANTA ===
    poner_colmillo(CX + 3, base_solida(CX + 3, CY + 3, CY + RADIO), 6, 2)
    poner_colmillo(CX - 3, base_solida(CX - 3, CY + 3, CY + RADIO), 6, 2)
    poner_colmillo(CX + 2, base_solida(CX + 2, CY + 3, CY + RADIO), 3, 2)
    poner_colmillo(CX - 2, base_solida(CX - 2, CY + 3, CY + RADIO), 3, 2)

    # === EL RENDER (16 px/tile) ===
    ESC = 16
    img = Image.new('RGBA', (ANCHO * ESC, ALTO * ESC), (128, 170, 220, 255))
    dr = ImageDraw.Draw(img)
    for x in range(ANCHO):
        for y in range(ALTO):
            t = TERRENO.get((x, y))
            px, py = x * ESC, y * ESC
            if not t or not t['sol']:
                continue
            color = (86, 40, 110, 255) if t['tipo'] == 'obsidiana' else (120, 116, 130, 255)
            if t['slope'] == 1:      # DownLeft: sólido abajo-izquierda (la diagonal baja a la derecha)
                dr.polygon([(px, py), (px, py + ESC), (px + ESC, py + ESC)], fill=color)
            elif t['slope'] == 2:    # DownRight: sólido abajo-derecha
                dr.polygon([(px + ESC, py), (px, py + ESC), (px + ESC, py + ESC)], fill=color)
            else:
                dr.rectangle([px, py, px + ESC - 1, py + ESC - 1], fill=color)
            if t['tipo'] == 'obsidiana':   # el brillo del vidrio volcánico
                dr.point((px + 5, py + 5), fill=(150, 110, 190, 255))
                dr.point((px + 9, py + 8), fill=(60, 30, 80, 255))

    # EL LIBRO DORMIDO (flotando en el centro, ojo cerrado + Zzz)
    librox, libroy = CX * ESC + ESC // 2, CY * ESC - 10
    dr.rectangle([librox - 7, libroy - 11, librox + 7, libroy + 11], fill=(30, 14, 44, 255),
                 outline=(120, 70, 160, 255))
    dr.line([librox - 4, libroy - 2, librox + 4, libroy - 2], fill=(200, 170, 255, 255), width=2)
    for i, (zx, zy) in enumerate([(10, -20), (16, -28), (22, -36)]):
        dr.line([librox + zx, libroy + zy, librox + zx + 5, libroy + zy + 6],
                fill=(200, 170, 255, 255), width=2)

    # EL CAMINO GARANTIZADO (la línea verde: borde→rampa→fondo→balcón del libro)
    camino = []
    for lado in (-1, 1):
        pts = [(CX + lado * 16, CY)]
        for i in range(9):
            pts.append((CX + lado * (12 - i), CY + i))       # la rampa
        pts.append((CX + lado * 4, CY + 8))                  # el balcón
        pts.append((CX + lado * 2, CY + 5))                  # hacia el libro (la vista)
        for i in range(len(pts) - 1):
            x0, y0 = pts[i][0] * ESC + 8, pts[i][1] * ESC + 8
            x1, y1 = pts[i + 1][0] * ESC + 8, pts[i + 1][1] * ESC + 8
            dr.line([x0, y0, x1, y1], fill=(80, 220, 120, 230), width=3)
        for (x0, y0) in pts:
            dr.ellipse([x0 * ESC + 3, y0 * ESC + 3, x0 * ESC + 13, y0 * ESC + 13],
                       outline=(80, 220, 120, 255), width=2)

    os.makedirs(os.path.join('tools', 'tools'), exist_ok=True)
    out = os.path.join('tools', 'tools', 'v650124_fauce_mock.png')
    img.save(out)
    print(f'OK {out} {img.size}')

    # === LA VERIFICACIÓN MECÁNICA DEL PASO (el auditor interno) ===
    # La marcha por la rampa izquierda: cada columna de la rampa tiene
    # superficie sólida en ys y 3 filas de aire encima (el jugador pasa).
    fallos = []
    for lado in (-1, 1):
        for i in range(9):
            x = CX + lado * (12 - i)
            ys = CY + i
            for y in range(ys - 3, ys):
                t = TERRENO.get((x, y))
                if t and t['sol']:
                    fallos.append(f'rampa {lado:+d} col {x} fila {y} BLOQUEADA')
            t = TERRENO.get((x, ys))
            if not (t and t['sol']):
                fallos.append(f'rampa {lado:+d} col {x} SIN piso en {ys}')
    # el balcón (±4): piso en CY+8 libre encima
    for lado in (-1, 1):
        x = CX + lado * 4
        for y in range(CY + 5, CY + 8):
            t = TERRENO.get((x, y))
            if t and t['sol']:
                fallos.append(f'balcón {lado:+d} bloqueado en {y}')
    # el libro: su columna libre de dientes
    for y in range(CY + 3, CY + RADIO):
        t = TERRENO.get((CX, y))
        if t and t['sol']:
            fallos.append(f'la garganta bajo el libro tiene tile en {y}')
    if fallos:
        print('FALLOS DEL PASO:')
        for f in fallos:
            print('  ✗', f)
    else:
        print('PASO GARANTIZADO: rampas pisables y con cabeza, balcones libres, garganta abierta')


if __name__ == '__main__':
    main()
