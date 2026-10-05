#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_bolsa_sombras_v65063.py — LA BOLSA DE LAS SOMBRAS (v6.50.63) · v2.

El icono de la bolsa 19 (LAS FAUCES DEL GRIMORIO). La silueta de la casa
(la misma bolsa que BolsaArmasRayo) recolorida a la paleta de las sombras
devoradoras de la v6.50.62 (SombrasLib): masa negro-violeta con dither,
el OJO BLANCO de pupila roja que mira (Tsurime, Pride tras comerse a
Gluttony), y en la ventana semitransparente del centro — que en la bolsa
del rayo era el glow — LA GARGANTA: boca negra con brillo rojo al fondo y
colmillos de aguja blancos colgando de los bordes.

Salida: AethonMod/Content/Items/Bolsas/BolsaSombras.png (30x30 RGBA)
"""
from PIL import Image
import numpy as np

SRC = 'AethonMod/Content/Items/Bolsas/BolsaArmasRayo.png'
DST = 'AethonMod/Content/Items/Bolsas/BolsaSombras.png'

base = np.array(Image.open(SRC).convert('RGBA')).astype(int)
H, W = base.shape[:2]
alpha = base[:, :, 3]

# --- LA PALETA (SombrasLib) ---
NEGRO_ABS = (8, 6, 12)        # borde: masa que no deja pasar luz
CUERPO_1 = (24, 17, 36)       # masa, lado claro del dither
CUERPO_2 = (14, 10, 24)       # masa, lado oscuro del dither
VELO = (48, 32, 70)           # penumbra violeta
NUDO = (126, 100, 156)        # el cordón
NUDO_ROJO = (210, 46, 56)     # el brillo del nudo
BLANCO = (244, 242, 248)      # el ojo y los colmillos
ROJO = (196, 28, 40)          # la pupila
ROJO_BRILLO = (255, 96, 96)   # el destello de la pupila / la garganta

img = np.zeros((H, W, 4), dtype=int)


def dentro(y, x):
    return 0 <= y < H and 0 <= x < W


def silueta(y, x):
    return dentro(y, x) and alpha[y, x] > 40


# --- 0. CLASIFICAR los píxeles semitransparentes: ¿borde exterior o ventana interior? ---
# El borde exterior de la bolsa toca píxeles TOTALMENTE transparentes (alpha<10);
# la ventana central (el glow del rayo, alpha 80) no los toca.
es_borde = np.zeros((H, W), dtype=bool)
es_ventana = np.zeros((H, W), dtype=bool)
for y in range(H):
    for x in range(W):
        a = alpha[y, x]
        if a <= 40:
            continue
        if a >= 200:
            continue
        toca_fuera = any(dentro(y + dy, x + dx) and alpha[y + dy, x + dx] < 10
                         for dy in (-2, -1, 0, 1, 2) for dx in (-2, -1, 0, 1, 2))
        if toca_fuera:
            es_borde[y, x] = True
        else:
            es_ventana[y, x] = True

# --- 1. EL CUERPO: masa negro-violeta con dither diagonal (fluye como líquido) ---
for y in range(H):
    for x in range(W):
        if not silueta(y, x):
            continue
        a = alpha[y, x]
        if es_borde[y, x]:
            img[y, x] = (*NEGRO_ABS, 255)
            continue
        if es_ventana[y, x]:
            # LA GARGANTA: boca negra semitransparente (la masa que se abre)
            img[y, x] = (*NEGRO_ABS, 120)
            continue
        d = ((x + y * 2) % 5) < 2
        c = CUERPO_1 if d else CUERPO_2
        if y <= 8 and ((x * 3 + y) % 7) < 2:
            c = VELO  # la penumbra violeta del lomo
        img[y, x] = (*c, 255)

# --- 2. EL NUDO DEL CORDÓN: violeta pálido con el latigillo rojo ---
for (y, x) in [(1, 14), (1, 15), (1, 16), (2, 13), (2, 14), (2, 15), (2, 16), (2, 17),
               (3, 14), (3, 15), (3, 16), (4, 13), (4, 14), (4, 15), (4, 16), (4, 17)]:
    img[y, x] = (*NUDO, 255)
img[2, 15] = (*NUDO_ROJO, 255)
img[3, 15] = (*NUDO_ROJO, 255)

# --- 3. EL OJO GRANDE (el que mira): almendra blanca y10-14, comisuras que suben ---
for y in range(9, 15):
    for x in range(7, 22):
        if not silueta(y, x):
            continue
        cy, cx = 12.0, 14.5
        dy = (y - cy) / 2.4
        dx = (x - cx) / 6.6
        v = dx * dx + dy * dy
        if v <= 1.0:
            img[y, x] = (*BLANCO, 255)
        elif v <= 1.3:
            img[y, x] = (*VELO, 255)
# el Tsurime: las comisuras SUBEN
img[9, 8] = (*BLANCO, 255); img[9, 9] = (*BLANCO, 255)
img[9, 20] = (*BLANCO, 255); img[9, 21] = (*BLANCO, 255)
# LA PUPILA ROJA que mira (un píxel abajo-derecha: te mira A TI)
for y in range(11, 14):
    for x in range(15, 18):
        img[y, x] = (*ROJO, 255)
img[11, 16] = (*ROJO_BRILLO, 255)

# --- 4. LA GARGANTA: brillo rojo al fondo de la boca + los COLMILLOS de aguja ---
# El fondo rojo de la boca (visible a través de la penumbra): centro-bajo de la ventana
for (y, x) in [(21, 13), (21, 14), (22, 13), (22, 14), (20, 13), (20, 14)]:
    if dentro(y, x) and es_ventana[y, x]:
        img[y, x] = (*ROJO, 170)
for (y, x) in [(22, 12), (22, 15), (23, 13), (23, 14)]:
    if dentro(y, x) and es_ventana[y, x]:
        img[y, x] = (*ROJO, 120)

# Colmillos SUPERIORES: cuelgan del borde de arriba de la boca (bajo el ojo)
sup = [(15, 10), (15, 11), (16, 10),          # colmillo 1
       (15, 13), (16, 13),                     # colmillo 2
       (15, 15), (16, 15), (17, 15),           # colmillo 3 (el largo)
       (15, 17), (16, 17)]                     # colmillo 4
for (y, x) in sup:
    if dentro(y, x) and (es_ventana[y, x] or silueta(y, x)):
        img[y, x] = (*BLANCO, 255)

# Colmillos INFERIORES: apuntan arriba desde el borde de abajo de la boca
inf = [(23, 10), (22, 10), (23, 11),           # colmillo 1
       (22, 12), (21, 12),                     # colmillo 2
       (22, 15), (21, 15), (23, 15), (23, 16), # colmillo 3 (el doble)
       (22, 17), (23, 17)]                     # colmillo 4
for (y, x) in inf:
    if dentro(y, x) and (es_ventana[y, x] or silueta(y, x)):
        img[y, x] = (*BLANCO, 255)

# --- 5. LOS DOS OJOS PEQUEÑOS a los lados (las sombras tienen MUCHOS ojos) ---
img[19, 6] = (*BLANCO, 255); img[20, 6] = (*BLANCO, 255); img[19, 7] = (*BLANCO, 255)
img[19, 22] = (*BLANCO, 255); img[20, 23] = (*BLANCO, 255)
img[20, 5] = (*VELO, 255)

# --- 6. El dither final: píxeles de velo sueltos (la masa viva) ---
for (y, x) in [(7, 10), (17, 24), (19, 12), (24, 19), (8, 21)]:
    if silueta(y, x) and not es_ventana[y, x]:
        img[y, x] = (*VELO, 255)

out = Image.fromarray(img.astype(np.uint8))
out.save(DST)
print(f"OK -> {DST} {out.size}")

# --- verificación ASCII ---
a = np.array(out)
for y in range(H):
    row = ''
    for x in range(W):
        r, g, b, al = a[y][x]
        if al < 40:
            row += '.'
        elif (r, g, b) == BLANCO:
            row += 'W'
        elif (r, g, b) in (ROJO, ROJO_BRILLO, NUDO_ROJO):
            row += 'R'
        elif (r, g, b) == NUDO:
            row += 'n'
        elif (r, g, b) == VELO:
            row += 'v'
        elif al < 200:
            row += '~'      # la garganta semitransparente
        elif (r, g, b) == NEGRO_ABS:
            row += '#'
        else:
            row += 'x'      # la masa
    print(row)
