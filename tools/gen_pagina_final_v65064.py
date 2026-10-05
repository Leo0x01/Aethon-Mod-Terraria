#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_pagina_final_v65064.py — LA PÁGINA FINAL (v6.50.64) · ARMA 4.

El icono del arma 4 (la evolución AAA de La Sombra de la Página): EL
LIBRO — dos hojas negras con el lomo rojo entreabierto (la garganta), el
OJO BLANCO de pupila roja que mira (Pride), tres colmillos blancos en el
borde inferior (la página MUERDE hacia abajo — es la boca que sube
comiéndose al jefe) y la BRUMA NEGRA violeta exhalando por debajo (la
petición del usuario hecha icono). Paleta de la casa (SombrasLib).

Salida: AethonMod/Content/Weapons/Sombras/PaginaFinal.png (30x30 RGBA)
"""
from PIL import Image
import numpy as np
import math

W = H = 30
DST = 'AethonMod/Content/Weapons/Sombras/PaginaFinal.png'

# --- LA PALETA (SombrasLib / gen_bolsa_sombras_v65063) ---
NEGRO_ABS = (8, 6, 12)        # borde: masa que no deja pasar luz
CUERPO_1 = (26, 18, 38)       # hoja, lado claro del dither
CUERPO_2 = (15, 10, 26)       # hoja, lado oscuro del dither
VELO = (84, 60, 122)          # penumbra violeta (la bruma)
VELO_CLARO = (132, 102, 184)  # la bruma más clara (lila vivo)
BLANCO = (244, 242, 248)      # el ojo y los colmillos
ROJO = (196, 28, 40)          # la pupila / el lomo
ROJO_BRILLO = (255, 96, 96)   # el destello
ROJO_GARGANTA = (120, 8, 14)  # la garganta entre los colmillos

img = np.zeros((H, W, 4), dtype=int)


def dentro(y, x):
    return 0 <= y < H and 0 <= x < W


def px(y, x, c, a=255):
    if dentro(y, x):
        img[y, x, 0], img[y, x, 1], img[y, x, 2], img[y, x, 3] = c[0], c[1], c[2], a


# ============ 1. EL LIBRO: dos hojas verticales (x 7..22, y 3..24) ============
# la silueta con las esquinas superiores recortadas (páginas)
HOJA_X0, HOJA_X1, HOJA_Y0, HOJA_Y1 = 7, 22, 3, 24
SPINE = 15  # el lomo (la columna central)


def en_hoja(y, x):
    if not (HOJA_X0 <= x <= HOJA_X1 and HOJA_Y0 <= y <= HOJA_Y1):
        return False
    # esquinas superiores recortadas (el corte de la página)
    if y < HOJA_Y0 + 2:
        d = HOJA_Y0 + 1 - y
        if x < HOJA_X0 + d or x > HOJA_X1 - d:
            return False
    # esquina inferior derecha recortada (la hoja que se abre)
    if y > HOJA_Y1 - 2 and x > HOJA_X1 - (HOJA_Y1 - y):
        return False
    return True


for y in range(HOJA_Y0, HOJA_Y1 + 1):
    for x in range(HOJA_X0, HOJA_X1 + 1):
        if not en_hoja(y, x):
            continue
        # el hueco del lomo (1 px): la garganta del libro
        if x == SPINE:
            continue
        borde = (not en_hoja(y - 1, x) or not en_hoja(y + 1, x) or
                 not en_hoja(y, x - 1) or not en_hoja(y, x + 1))
        if borde:
            px(y, x, NEGRO_ABS)
        else:
            # dither diagonal: la textura de la masa
            d = (x + y * 2) % 5
            c = CUERPO_1 if d < 2 else CUERPO_2
            # lado derecho de cada hoja un pelo más claro (la luz del lomo)
            if abs(x - SPINE) <= 2:
                c = CUERPO_1
            px(y, x, c)

# --- LOS PLIEGUES de las hojas (cada mitad se curva hacia el lomo) ---
PLIEGUE = (11, 8, 18)
for y in range(HOJA_Y0 + 3, HOJA_Y1 - 2):
    for fx in (11, 20):
        if en_hoja(y, fx):
            px(y, fx, PLIEGUE, 170)
    # la luz del lomo: el borde interno de cada hoja un pelo vivo
    for fx in (13, 17):
        if en_hoja(y, fx):
            px(y, fx, VELO, 70)

# --- LAS RUNAS: líneas de texto difuminas en las páginas (es un LIBRO) ---
for (rx, ry, largo) in ((9, 6, 3), (17, 6, 3), (9, 17, 3), (17, 17, 4), (9, 19, 2), (18, 19, 2)):
    for i in range(largo):
        x, y = rx + i, ry
        if en_hoja(y, x) and img[y, x, 3] == 255 and (y, x) != (ey, ex):
            # no pisar los ojos: comprobar distancia a ambos ojos
            if math.hypot(x - ex, y - ey) < 5 or math.hypot(x - ex2, y - ey) < 4:
                continue
            px(y, x, VELO_CLARO, 95)

# ============ 2. EL LOMO ROJO (la garganta entreabierto) ============
for y in range(HOJA_Y0 + 2, HOJA_Y1 - 3):
    px(y, SPINE, ROJO, 255)
# el destello del lomo (latido)
px(HOJA_Y0 + 6, SPINE, ROJO_BRILLO, 255)
px(HOJA_Y0 + 7, SPINE, ROJO_BRILLO, 255)

# ============ 3. EL OJO (blanco, pupila roja, mira ligeramente abajo-derecha) ============
# elipse blanca 8x5 centrada en (14.5, 10.5) — a la IZQUIERDA del lomo para no taparlo
ex, ey, ew, eh = 11, 10, 5, 4   # ojo izquierdo en la hoja izquierda
for y in range(ey - eh, ey + eh + 1):
    for x in range(ex - ew, ex + ew + 1):
        dx = (x - ex) / ew
        dy = (y - ey) / eh
        if dx * dx + dy * dy <= 1.0 and en_hoja(y, x) and x < SPINE - 1:
            px(y, x, BLANCO)
# la pupila (roja, desplazada: mira a su presa)
for y in range(ey - 1, ey + 2):
    for x in range(ex, ex + 3):
        if en_hoja(y, x) and x < SPINE:
            px(y, x, ROJO)
px(ey, ex + 1, ROJO_BRILLO)   # el destello de la pupila

# el SEGUNDO ojo (la hoja derecha también mira — la página es TODA ojos)
ex2 = 19
for y in range(ey - eh + 1, ey + eh):
    for x in range(ex2 - 2, ex2 + 3):
        dx = (x - ex2) / 3.0
        dy = (y - ey) / 2.6
        if dx * dx + dy * dy <= 1.0 and en_hoja(y, x) and x > SPINE + 1:
            px(y, x, BLANCO)
for y in range(ey - 1, ey + 1):
    for x in range(ex2 - 1, ex2 + 2):
        if en_hoja(y, x) and x > SPINE:
            px(y, x, ROJO)

# ============ 4. LA BOCA + LOS COLMILLOS (el borde inferior MUERDE) ============
# primero la GARGANTA: una línea roja ancha en el borde inferior de las
# hojas — la boca abierta de la página (el lomo la parte en dos)
for x in range(HOJA_X0 + 1, HOJA_X1 + 1):
    if en_hoja(HOJA_Y1, x) or en_hoja(HOJA_Y1 - 1, x):
        if abs(x - SPINE) <= 1:
            px(HOJA_Y1, x, ROJO_GARGANTA)
            px(HOJA_Y1 - 1, x, ROJO_GARGANTA)
        else:
            px(HOJA_Y1, x, ROJO)
            px(HOJA_Y1 - 1, x, ROJO_GARGANTA, 200)
px(HOJA_Y1 - 1, SPINE, ROJO_BRILLO)

# los colmillos: agujas blancas (base 3px → punta 1px) con contorno negro
for (cx, largo) in ((10, 6), (15, 7), (20, 5)):
    for i in range(largo):
        y = HOJA_Y1 + i
        semi = 1 if i < largo - 2 else 0     # 3px de base, 1px de punta
        for x in range(cx - semi, cx + semi + 1):
            px(y, x, BLANCO, 255)
        # el contorno (recorta sobre la bruma)
        px(y, cx - semi - 1, NEGRO_ABS, 190)
        px(y, cx + semi + 1, NEGRO_ABS, 190)

# ============ 5. LA BRUMA NEGRA (violeta, exhalada por debajo de la boca) ============
# UNA NUBE COHESIVA bajo la boca (no ruido): el aliento de la página
for y in range(23, 30):
    for x in range(3, 28):
        # elipse ancha centrada en (15.5, 27.0) que ENVUELVE los colmillos
        dx = (x - 15.5) / 12.5
        dy = (y - 27.0) / 3.6
        d2 = dx * dx + dy * dy
        if d2 <= 1.0:
            a = int(195 * (1.0 - d2 * 0.7))
            # textura ondulada (los lóbulos de la exhalación)
            onda = 0.78 + 0.22 * math.sin(x * 0.9 + y * 0.4)
            a = int(a * onda)
            if a > 12:
                c = VELO_CLARO if d2 < 0.34 else VELO
                if img[y, x, 3] < a:
                    px(y, x, c, a)
# dos rizos finos que suben por los lados del libro
for (bx, fase) in ((5, 0.0), (25, 2.1)):
    for i in range(11):
        t = i / 10.0
        y = int(27 - t * 11)
        x = int(bx + math.sin(t * 3.0 + fase) * 1.8)
        a = int(165 * (1.0 - t * 0.55))
        px(y, x, VELO_CLARO, a)
        px(y, x + 1, VELO_CLARO, a)
        px(y, x - 1, VELO, int(a * 0.6))

# ============ 6. EL VELO DE LA SALA (las esquinas del icono se apagan) ============
for y in range(H):
    for x in range(W):
        if img[y, x, 3] == 0:
            d1 = math.hypot(x - 1, y - 1)
            d2 = math.hypot(W - 2 - x, H - 2 - y)
            d = min(d1, d2)
            if d < 6:
                a = int(60 * (1 - d / 6))
                if a > 6:
                    px(y, x, VELO, a)

out = Image.fromarray(img.astype('uint8'), 'RGBA')
out.save(DST)
print(f'OK → {DST} (30x30, {len(out.tobytes())} B raw)')

# --- el mock de verificación (8x) ---
mock = out.resize((240, 240), Image.NEAREST)
mock.save('tools/tools/pagina_final_v65064_mock.png')
print('mock 8x → tools/tools/pagina_final_v65064_mock.png')
