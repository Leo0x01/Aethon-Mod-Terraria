#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# ======================================================================
#  gen_bolsa_ajuar_v6183.py — EL AJUAR DEL PORTADOR (la letra: «ponlo
#  en una bolsa y esa bolsa ponla dentro de la Bolsa Maestra»).
#
#  LA BOLSA DE LOS TRECE REGALOS: la silueta clásica de la casa (la de
#  BolsaArmasRayo) recolorida en ORO DEL COFRE (el ajuar es tesoro:
#  la rampa cálida del Cofre del Arsenal) con el cordón VIOLETA DEL
#  MOD y, en la ventana central, la firma de sus tesoros: LA PLUMA
#  BLANCA DEL ESCRIBA (pluma diagonal con caña ámbar y dos destellos
#  de 4 puntas — el Brillo de los Soles Rúnicos).
#
#  Convención de la casa: 30×30 RGBA, fondo transparente, contorno
#  (26,15,38), dither en el cuerpo, reporte ASCII + md5 al final.
# ======================================================================
import hashlib
import os

from PIL import Image
import numpy as np

BASE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
CARPETA = os.path.normpath(os.path.join(
    BASE, "AethonMod", "Content", "Items", "Bolsas"))
SRC = os.path.join(CARPETA, "BolsaArmasRayo.png")
OUT = os.path.join(CARPETA, "BolsaAjuarPortador.png")

W = H = 30
OUTLINE = (26, 15, 38, 255)


def dentro(y, x):
    return 0 <= y < H and 0 <= x < W


# ── LA SILUETA CLÁSICA (clasificación) ──────────────────────────────
base = np.array(Image.open(SRC).convert("RGBA")).astype(int)
alpha = base[:, :, 3]

es_borde = np.zeros((H, W), dtype=bool)
es_ventana = np.zeros((H, W), dtype=bool)
for y in range(H):
    for x in range(W):
        a = alpha[y, x]
        if a <= 40 or a >= 200:
            continue
        toca_fuera = any(
            dentro(y + dy, x + dx) and alpha[y + dy, x + dx] < 10
            for dy in (-2, -1, 0, 1, 2) for dx in (-2, -1, 0, 1, 2))
        if toca_fuera:
            es_borde[y, x] = True
        else:
            es_ventana[y, x] = True

es_cordon = np.zeros((H, W), dtype=bool)
for y in range(0, 8):
    for x in range(W):
        if alpha[y, x] >= 200:
            es_cordon[y, x] = True

# ── LA PALETA DEL AJUAR (el oro del cofre) ──────────────────────────
ORO_1 = (196, 138, 62)      # el lado claro del dither — oro viejo
ORO_2 = (150, 99, 40)      # el lado oscuro — cuero dorado
VELO = (222, 170, 92)      # la penumbra del lomo — ámbar claro
NEGRO = (38, 24, 15)       # el cuero oscuro del fondo
VIOLETA = (150, 84, 235)   # el cordón — la firma del mod
VIOLETA_OSC = (104, 52, 170)
NUDO = (238, 210, 255)
PLUMA = (248, 244, 255)    # la pluma blanca
CANA = (255, 218, 140)     # la caña ámbar de la pluma
CHISPA = (255, 240, 190)   # los destellos de 4 puntas

img = np.zeros((H, W, 4), dtype=int)
for y in range(H):
    for x in range(W):
        if alpha[y, x] <= 40:
            continue
        if es_borde[y, x]:
            img[y][x] = OUTLINE
        elif es_ventana[y, x]:
            img[y][x] = (*NEGRO, 120)
        elif es_cordon[y, x]:
            borde_cordon = alpha[y, x - 1 if x > 0 else 0] < 200 or \
                alpha[y, x + 1 if x < W - 1 else W - 1] < 200
            c = VIOLETA_OSC if borde_cordon else VIOLETA
            img[y][x] = (*c, 255)
        else:
            d = ((x + y * 2) % 5) < 2
            c = ORO_1 if d else ORO_2
            if y <= 12 and ((x * 3 + y) % 7) < 2:
                c = VELO
            img[y][x] = (*c, 255)

# ── LA PLUMA DEL ESCRIBA (diagonal, caña abajo-izquierda) ──────────
# La pluma vive en la ventana del saco (y>=14): eje diagonal blanco de
# (21,11) a (13,19), barbas escalonadas a ambos lados, la CAÑA ámbar
# en la punta inferior. `forzar` pisa lo que haga falta — la pluma ES
# el motivo, manda sobre el dither.
def forzar(y, x, c):
    if dentro(y, x):
        img[y][x] = (*c, 255)


for i in range(9):                       # el eje (raquis) blanco
    y, x = 21 - i, 11 + i
    forzar(y, x, PLUMA)
    forzar(y, x + 1, PLUMA)
for i in range(0, 7):                     # las barbas (el estandarte)
    y, x = 21 - i, 11 + i
    forzar(y - 1, x, PLUMA)
    forzar(y - 1, x + 1, (214, 206, 235))  # la sombra de la barba
    forzar(y + 1, x - 1, PLUMA)
    if i < 4:
        forzar(y + 2, x - 1, (214, 206, 235))
forzar(22, 10, CANA)                      # la caña ámbar (el cálamo)
forzar(23, 9, CANA)
forzar(22, 9, (200, 150, 70))
forzar(12, 20, PLUMA)                     # la punta del estandarte
forzar(11, 21, (214, 206, 235))

# ── LOS DOS DESTELLOS DE 4 PUNTAS (el brillo de los soles) ────────
# sobre el oro del cuerpo (fuerzan el dither)
def destello(cy, cx):
    forzar(cy, cx, CHISPA)
    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        forzar(cy + dy, cx + dx, (255, 228, 160))
        forzar(cy + dy * 2, cx + dx * 2, (196, 140, 66))


destello(9, 7)
destello(24, 22)

# ── EL CONTORNO FINAL (todo lo que toca la silueta) ────────────────
solid = {(x, y) for y in range(H) for x in range(W) if img[y][x][3] > 0}
cont = set()
for (x, y) in solid:
    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1),
                   (1, 1), (1, -1), (-1, 1), (-1, -1)):
        nx, ny = x + dx, y + dy
        if 0 <= nx < W and 0 <= ny < H and (nx, ny) not in solid:
            cont.add((nx, ny))
for (x, y) in cont:
    img[y][x] = OUTLINE

out = Image.fromarray(img.astype(np.uint8))
out.save(OUT)
with open(OUT, "rb") as f:
    md5 = hashlib.md5(f.read()).hexdigest()
print(f"BolsaAjuarPortador.png → {OUT}  ({out.size[0]}x{out.size[1]}, md5 {md5})")

# ── EL REPORTE ASCII (la verificación de la casa) ──────────────────
print("  (W=pluma c=caña +=oro1 x=oro2 ~=velo #=cordón O=contorno .=nada)")
a = img
for y in range(H):
    fila = ""
    for x in range(W):
        r, g, b, al = a[y][x]
        if al < 40:
            fila += "."
        elif al == 120:
            fila += "~"
        elif (r, g, b) == (26, 15, 38):
            fila += "#"
        elif (r, g, b) == PLUMA or (r, g, b) == (248, 244, 255):
            fila += "W"
        elif (r, g, b) == CANA:
            fila += "c"
        elif (r, g, b) == CHISPA or (r, g, b) == (255, 228, 160) or (r, g, b) == (196, 140, 66):
            fila += "*"
        elif (r, g, b) in ((214, 206, 235), (200, 150, 70)):
            fila += "w"
        elif (r, g, b) == VIOLETA or (r, g, b) == VIOLETA_OSC:
            fila += "O"
        elif (r, g, b) == ORO_1:
            fila += "+"
        elif (r, g, b) == ORO_2:
            fila += "x"
        elif (r, g, b) == VELO:
            fila += "v"
        else:
            fila += "?"
    print("  " + fila)
