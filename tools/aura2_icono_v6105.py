#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
aura2_icono_v6105.py — v6.105 — EL ICONO DEL AURA DE JUICIO 2.

LA PIEZA (30×30 RGBA, lote de iconos de la casa — SIN premultiplicar):
el sello del aura silenciosa — LA ESTRELLA DE 5 PUNTAS del juicio
(negra, el vacío) dentro del ANILLO DEL CAMPO (violeta vivo, el aura
morada y negra de la casa), con el halo que respira y las tres ascuas.

Salida: AethonMod/Content/Items/Accessories/AuraDeJuicio2Item.png
"""
import math
import os

from PIL import Image

AQUI = os.path.dirname(os.path.abspath(__file__))
BASE = os.path.join(AQUI, '..', 'AethonMod')
DEST = os.path.join(BASE, 'Content', 'Items', 'Accessories', 'AuraDeJuicio2Item.png')

N = 30
CX = CY = (N - 1) / 2.0

VIOLETA = (150, 84, 235)
VIOLETA_CLARO = (224, 176, 255)
NEGRO_VIOLETA = (26, 12, 44)


def _valor(semilla, x, y):
    h = semilla * 374761393 + x * 668265263 + y * 2246822519
    h = (h ^ (h >> 13)) * 1274126177
    h = h ^ (h >> 16)
    return (h & 0xFFFFFFFF) / 0xFFFFFFFF


def ruido(x, y, semilla=0):
    xi, yi = int(x), int(y)
    fx, fy = x - xi, y - yi
    fx = fx * fx * (3 - 2 * fx)
    fy = fy * fy * (3 - 2 * fy)
    a = _valor(semilla, xi, yi)
    b = _valor(semilla, xi + 1, yi)
    c = _valor(semilla, xi, yi + 1)
    d = _valor(semilla, xi + 1, yi + 1)
    return a + (b - a) * fx + (c - a) * fy + (a - b - c + d) * fx * fy


def estrella5(cx, cy, r_ext, r_int, angulo=-math.pi / 2.0):
    pts = []
    for k in range(10):
        r = r_ext if k % 2 == 0 else r_int
        a = angulo + k * math.pi / 5.0
        pts.append((cx + math.cos(a) * r, cy + math.sin(a) * r))
    return pts


def dentro_poligono(px, py, pts):
    dentro = False
    j = len(pts) - 1
    for i in range(len(pts)):
        xi, yi = pts[i]
        xj, yj = pts[j]
        if (yi > py) != (yj > py):
            xint = (xj - xi) * (py - yi) / (yj - yi) + xi
            if px < xint:
                dentro = not dentro
        j = i
    return dentro


def dist_poligono(px, py, pts):
    d = 1e9
    j = len(pts) - 1
    for i in range(len(pts)):
        x1, y1 = pts[j]
        x2, y2 = pts[i]
        dx, dy = x2 - x1, y2 - y1
        L2 = dx * dx + dy * dy
        if L2 > 0:
            t = max(0.0, min(1.0, ((px - x1) * dx + (py - y1) * dy) / L2))
            qx, qy = x1 + t * dx, y1 + t * dy
            d = min(d, math.hypot(px - qx, py - qy))
        j = i
    return d


def forjar():
    img = Image.new('RGBA', (N, N), (0, 0, 0, 0))
    px = img.load()

    pts = estrella5(CX, CY, N * 0.36, N * 0.145)

    for y in range(N):
        for x in range(N):
            dx, dy = x - CX, y - CY
            d = math.hypot(dx, dy)
            r, g, b, a = 0, 0, 0, 0

            # === EL HALO DEL CAMPO — el aura que respira (más densa en
            #     el anillo 0.28..0.46 — el BORDE del campo de juicio) ===
            if d < N * 0.52:
                n = ruido(x * 0.5, y * 0.5, 7)
                anillo = max(0.0, 1.0 - abs(d - N * 0.37) / (N * 0.15))
                halo_k = (anillo * 0.75 + 0.25 * max(0.0, 1.0 - d / (N * 0.5))) * (0.7 + 0.3 * n)
                halo_k = min(1.0, halo_k)
                col_h = [int(NEGRO_VIOLETA[k] + (VIOLETA[k] - NEGRO_VIOLETA[k]) * halo_k)
                         for k in range(3)]
                r, g, b = col_h
                a = int(255 * min(1.0, halo_k * 1.15))

            # === LA ESTRELLA DEL JUICIO — negra, el sello ===
            if dentro_poligono(x, y, pts):
                d_borde = dist_poligono(x, y, pts)
                if d_borde < 1.0:
                    # EL FILO — el borde violeta claro de la estrella
                    k = 1.0 - d_borde
                    r = int(10 + (VIOLETA_CLARO[0] - 10) * k * 0.8)
                    g = int(5 + (VIOLETA_CLARO[1] - 5) * k * 0.8)
                    b = int(16 + (VIOLETA_CLARO[2] - 16) * k * 0.8)
                else:
                    r, g, b = 6, 3, 10
                a = 255

            # === EL NÚCLEO — el corazón de la estrella (apenas brilla) ===
            dn = math.hypot(dx, dy)
            if dn < 2.0 and dentro_poligono(x, y, pts):
                k = 1.0 - dn / 2.0
                r = int(r + (VIOLETA_CLARO[0] - r) * k * 0.35)
                g = int(g + (VIOLETA_CLARO[1] - g) * k * 0.35)
                b = int(b + (VIOLETA_CLARO[2] - b) * k * 0.35)

            if a > 0:
                px[x, y] = (max(0, min(255, r)), max(0, min(255, g)),
                            max(0, min(255, b)), max(0, min(255, a)))

    img.save(DEST)
    print(f"AuraDeJuicio2Item.png forjado: {DEST} ({N}×{N})")


def verificar():
    img = Image.open(DEST)
    ok = img.size == (30, 30) and img.mode == 'RGBA'
    px = img.load()
    negros = sum(1 for y in range(30) for x in range(30)
                 if px[x, y][3] > 200 and px[x, y][0] < 20 and px[x, y][1] < 12)
    violetas = sum(1 for y in range(30) for x in range(30)
                   if px[x, y][3] > 60 and px[x, y][2] > 90 and px[x, y][0] < 190)
    if negros < 12:
        print(f"✗ LA ESTRELLA: solo {negros} píxeles negros")
        ok = False
    if violetas < 25:
        print(f"✗ EL ANILLO: solo {violetas} píxeles violetas")
        ok = False
    print("✓ ICONO AURA 2 VERIFICADO" if ok else "✗ REVISAR")
    return ok


if __name__ == '__main__':
    forjar()
    verificar()
