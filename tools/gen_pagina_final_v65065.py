#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_pagina_final_v65065.py — LA PÁGINA FINAL (v6.50.65) · ARMA 4 · ICONO v4.

v4 — PIXEL ART LIMPIO (v2=4/10, v3=3/10 en VLM: composiciones embarradas).
Ahora ZONAS CLARAS sin mezcla alfa: EL OJO grande arriba (almendra blanca
13×6 con pupila roja 3×3 y borde oscuro), EL LIBRO abajo (tapa
negro-violeta sólida con el LOMO ROJO central latiendo y el borde violeta
que se lee en cualquier fondo), TRES GARRAS colgando con su punta de
hueso blanca INEQUÍVOCAs y dos volutas de bruma en las esquinas bajas.
Directo, sin re-tintes de sprite — píxel sólido puro.
"""
from PIL import Image
import math

W = H = 30
DST = 'AethonMod/Content/Weapons/Sombras/PaginaFinal.png'

NEGRO = (12, 8, 20)
NEGRO_MED = (22, 14, 36)
VIOLETA = (108, 66, 178)
VIOLETA_BORDE = (150, 108, 216)
BLANCO = (248, 246, 252)
ROJO = (204, 26, 40)
ROJO_CLARO = (255, 110, 110)
GRIS_PAG = (196, 190, 214)

img = Image.new('RGBA', (W, H), (0, 0, 0, 0))
P = img.load()

def set_px(y, x, c, a=255):
    if 0 <= y < H and 0 <= x < W:
        P[x, y] = (c[0], c[1], c[2], a)

# ================================================================
# ZONA 1 (y 2..8) — EL OJO: almendra blanca GRANDE con pupila roja
# ================================================================
OX = 15
for y in range(2, 9):
    # el perfil de la almendra: ancho máximo en el centro (y=5)
    dy = (y - 5) / 3.0          # -1 .. 1
    half = 6.5 * math.sqrt(max(0.0, 1.0 - dy * dy))
    x0, x1 = OX - half, OX + half
    for x in range(30):
        if x0 <= x <= x1:
            borde = (x < x0 + 1.2) or (x > x1 - 1.2) or y in (2, 8)
            set_px(y, x, NEGRO if borde else BLANCO)
# LA PUPILA roja centrada — redonda, 3 de alto
for y in range(4, 7):
    for x in (13, 14, 15, 16):
        set_px(y, x, ROJO)
set_px(5, 14, ROJO_CLARO, 230)   # el brillo de la córnea
# las pestañas de sombra (dos picos negros arriba — la mirada de Pride)
set_px(1, 9, NEGRO); set_px(2, 8, NEGRO)
set_px(1, 20, NEGRO); set_px(2, 21, NEGRO)

# ================================================================
# ZONA 2 (y 10..21) — EL LIBRO: tapa sólida + lomo rojo + borde violeta
# ================================================================
BX0, BX1, BY0, BY1 = 7, 22, 10, 21
for y in range(BY0, BY1 + 1):
    for x in range(BX0, BX1 + 1):
        # la silueta: esquinas superiores cortadas (las páginas abiertas)
        c = NEGRO_MED
        if y == BY0 and (x <= BX0 + 1 or x >= BX1 - 1):
            continue
        # EL BORDE violeta (el rim que se lee en fondo negro)
        if x in (BX0, BX1) or y == BY1 or (y == BY0 and (x == BX0 + 2 or x == BX1 - 2)):
            c = VIOLETA_BORDE
        # la tapa con vetas verticales sutiles (el cuero del grimorio)
        elif (x + y * 2) % 7 == 0:
            c = NEGRO
        set_px(y, x, c)
# EL LOMO ROJO — la columna central que LATE (la garganta del libro)
for y in range(BY0 + 2, BY1):
    set_px(y, 14, ROJO if y % 3 else ROJO_CLARO)
    set_px(y, 15, ROJO)
# LAS LUCES de la tapa (la galaxia del grimorio — 4 destellos blancos)
set_px(13, 11, BLANCO); set_px(12, 12, GRIS_PAG, 200)
set_px(18, 11, BLANCO); set_px(19, 12, GRIS_PAG, 200)
set_px(13, 18, BLANCO); set_px(18, 18, GRIS_PAG, 200)

# ================================================================
# ZONA 3 (y 22..28) — TRES GARRAS colgando del libro
# ================================================================
garras = [(10, 6), (15, 8), (20, 6)]   # (x, largo)
for gx, largo in garras:
    curva = -1 if gx < 15 else (1 if gx > 15 else 0)
    for s in range(largo):
        y = 22 + s
        if s == 0:
            xs, ancho = (gx - 1, 3)          # nace ancho
        elif s < largo - 2:
            xs, ancho = (gx - 1 + curva, 2)  # se afila
        else:
            xs, ancho = (gx + curva * 2, 1)  # la aguja
        for w2 in range(ancho):
            set_px(y, xs + w2, NEGRO if s else NEGRO_MED)
        set_px(y, xs - 1, VIOLETA, 130)      # rim violeta
        set_px(y, xs + ancho, VIOLETA, 130)
    # LA PUNTA DE HUESO — blanca, clara
    ty, tx = 22 + largo, gx + curva * 2
    set_px(ty, tx, BLANCO)
    set_px(ty, tx + curva, BLANCO, 200)
    set_px(ty - 1, tx, BLANCO, 110)

# ================================================================
# ZONA 4 — LAS DOS VOLUTAS DE BRUMA (esquinas bajas, sutiles)
# ================================================================
for bx, fase in [(3, 0.0), (27, 2.2)]:
    for s in range(5):
        ang = 0.55 * s + fase
        xx = int(round(bx + 1.8 * math.sin(ang)))
        yy = 24 + s // 3
        r = 1.8 - s * 0.22
        for y2 in range(-1, 2):
            for x2 in range(-1, 2):
                if math.hypot(x2, y2) <= r:
                    set_px(yy + y2, xx + x2, VIOLETA_BORDE, 90)

img.save(DST)
print("OK:", DST, img.size)
