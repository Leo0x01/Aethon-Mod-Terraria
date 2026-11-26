#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_faro_v6176.py — v6.176 — EL REDISEÑO DEL FARO DEL ECLIPSE (la
letra: «el arma faro del eclipse se ve horrible rediseñarla»).

EL DISEÑO NUEVO (el estilo de la casa: silueta + rampa de paleta +
brillo de punta + contorno Terraria) — v2 tras el QA del OJO (6.5 →
meta 7+): rayos CONTINUOS con gradiente (no motas sueltas), techo
cónico macizo con remate de orbe, cornisa fina, EL ECLIPSE con
corona de PICOS y luz de rebote ámbar en la torre.

34×34, RGBA, contorno oscuro de la casa.
"""
import math
import pathlib
from PIL import Image

RAIZ = pathlib.Path(__file__).resolve().parent.parent
DESTINO = RAIZ / "AethonMod" / "Content" / "Weapons" / "Investigaciones" / "FaroDelEclipse.png"
PREVIEW = pathlib.Path("/tmp") / "faro_v6176_preview.png"

N = 34

# LA PALETA DEL ECLIPSE (la casa)
CONTORNO = (10, 8, 32, 255)
TORRE_OSC = (24, 18, 60, 255)
TORRE_MED = (38, 30, 88, 255)
TORRE_LUZ = (56, 46, 116, 255)
TORRE_REBOTE = (86, 64, 120, 255)     # la luz de rebote ámbar-violeta
ORO = (216, 144, 48, 255)
ORO_ALTO = (255, 200, 96, 255)
CORONA = (255, 170, 60, 255)
CORONA_ALTA = (255, 224, 150, 255)
DISCO = (6, 3, 12, 255)
RAYO = (255, 196, 96, 255)
RAYO_NUC = (255, 236, 190, 255)

img = Image.new("RGBA", (N, N), (0, 0, 0, 0))


def px(x, y, c):
    if 0 <= x < N and 0 <= y < N:
        img.putpixel((x, y), c)


def rect(x0, y0, x1, y1, c):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            px(x, y, c)


def linea(x0, y0, x1, y1, grosor, color, alfa=255):
    """La línea gruesa con Bresenham (los rayos CONTINUOS del QA)."""
    dx, dy = abs(x1 - x0), abs(y1 - y0)
    sx = 1 if x0 < x1 else -1
    sy = 1 if y0 < y1 else -1
    err = dx - dy
    x, y = x0, y0
    while True:
        for ox in range(grosor):
            for oy in range(grosor):
                px(x + ox, y + oy, (color[0], color[1], color[2], alfa))
        if x == x1 and y == y1:
            break
        e2 = 2 * err
        if e2 > -dy:
            err -= dy
            x += sx
        if e2 < dx:
            err += dx
            y += sy


def relleno(x, y):
    if 0 <= x < N and 0 <= y < N:
        return img.getpixel((x, y))[3] > 0
    return False


def outline_around():
    bordes = []
    for y in range(N):
        for x in range(N):
            if relleno(x, y):
                continue
            for ddx, ddy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                if relleno(x + ddx, y + ddy):
                    bordes.append((x, y))
                    break
    for x, y in bordes:
        px(x, y, CONTORNO)


cx = 17

# ============ EL TECHO CÓNICO (macizo, 4 faldones con su orbe) ============
for y in range(2, 8):
    a = max(1, 7 - y)          # faldón: 1..5 de semiancho
    for x in range(cx - a, cx + a + 1):
        c = TORRE_LUZ if x >= cx else TORRE_MED
        px(x, y, c)
# EL ORBE del remate (la veleta dorada, 2×2 + su brillo)
rect(cx - 1, 0, cx, 1, ORO)
px(cx - 1, 0, ORO_ALTO)

# ============ LA LINTERNA (la habitación del eclipse) ============
rect(12, 8, 22, 12, TORRE_MED)
rect(12, 8, 12, 12, TORRE_OSC)
rect(22, 8, 22, 12, TORRE_OSC)
# LA CORNISA FINA (una línea de oro — el QA pidió menos peso)
rect(11, 13, 23, 13, ORO)
px(11, 13, ORO_ALTO)
px(23, 13, ORO_ALTO)

# ============ EL ECLIPSE (disco negro + corona de PICOS) ============
ec_x, ec_y, r = 17, 10, 3.1
picos = 8
for y in range(4, 16):
    for x in range(10, 25):
        dist = math.hypot(x - ec_x, y - ec_y)
        # el ángulo para los picos de la corona (el fuego desigual)
        ang = math.atan2(y - ec_y, x - ec_x)
        pico = 1.0 + 0.55 * abs(math.cos(picos * 0.5 * ang))
        if dist <= r - 0.3:
            px(x, y, DISCO)
        elif dist <= r + 0.9 * pico:
            px(x, y, CORONA if (int(dist * 2 + x) % 3) else CORONA_ALTA)
# EL BROCHE de la corona (el destello a la derecha del disco)
px(ec_x + 3, ec_y - 1, CORONA_ALTA)
px(ec_x + 4, ec_y, CORONA)

# ============ LA TORRE (affinada, con ventanas y rebote) ============
def ancho_torre(y):
    if y >= 31:
        return 6
    if y >= 27:
        return 5
    if y >= 21:
        return 4
    return 3


for y in range(14, 33):
    a = ancho_torre(y)
    for x in range(cx - a, cx + a + 1):
        c = TORRE_MED
        if x < cx - a + 2:
            c = TORRE_OSC
        if x == cx + a - 1 and y % 7 != 0:
            c = TORRE_LUZ
        # LA LUZ DE REBOTE (el QA: el ámbar de la linterna baña la
        # torre cercana)
        if y <= 18 and x >= cx - a + 2:
            mezcla = (19 - y) / 5.0
            c = tuple(min(255, int(c[i] + (TORRE_REBOTE[i] - c[i]) * mezcla)) for i in range(3)) + (255,)
        # LOS BROCHES dorados (las abrazaderas de la torre)
        if y in (19, 25) and x > cx - a + 2 and x < cx + a - 1:
            c = ORO if (x + y) % 3 else ORO_ALTO
        px(x, y, c)

# LAS VENTANAS del faro (dos ranuras ámbar — la referencia icónica)
rect(16, 22, 17, 23, RAYO)
px(16, 22, RAYO_NUC)
rect(16, 28, 17, 28, RAYO)

# EL PLINTO
rect(cx - 6, 31, cx + 6, 32, TORRE_OSC)
rect(cx - 6, 31, cx + 6, 31, TORRE_MED)
px(cx - 2, 32, CONTORNO)
px(cx + 1, 32, CONTORNO)

# ============ LOS RAYOS (continuos, con gradiente y núcleo) ============
# izquierdo: de la boca de la linterna (12,9) hacia (3,4)
# derecho: de (22,9) hacia (31,4)
for i in range(9):
    t = i / 8.0
    alfa = int(255 * (1.0 - t * 0.75))
    # izquierdo (grosor 2, núcleo más corto que el halo)
    xi = round(12 - 9 * t)
    yi = round(9 - 5 * t)
    if i < 7:
        for g in range(2):
            px(xi + g, yi + g, (RAYO[0], RAYO[1], RAYO[2], alfa))
    if i < 4:
        px(xi, yi, (RAYO_NUC[0], RAYO_NUC[1], RAYO_NUC[2], alfa))
    # derecho
    xd = round(22 + 9 * t)
    yd = round(9 - 5 * t)
    if i < 7:
        for g in range(2):
            px(xd - g, yd + g, (RAYO[0], RAYO[1], RAYO[2], alfa))
    if i < 4:
        px(xd, yd, (RAYO_NUC[0], RAYO_NUC[1], RAYO_NUC[2], alfa))
# LAS BOCAS de los rayos (la linterna encendida a los lados)
px(12, 9, RAYO_NUC)
px(22, 9, RAYO_NUC)

outline_around()

img.save(DESTINO)
img.resize((N * 14, N * 14), Image.NEAREST).save(PREVIEW)
print(f"✔ FaroDelEclipse.png rediseñado v2 ({N}×{N}) — {DESTINO}")
print(f"  preview: {PREVIEW}")
