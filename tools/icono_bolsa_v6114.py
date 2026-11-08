#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
icono_bolsa_v6114.py — LA BOLSA DE LA FORMA ETERNA v2 — pixel art CRISP
de 30×30 dibujado desde cero (la casa): la bolsa del botín de Terraria
(cuerpo de saco con el pellizco arriba y las dos orejas) en la paleta
del vacío, el lazo DORADO y LA MANDORLA chiquita al frente.
"""
from PIL import Image, ImageDraw

SS = 4
N = 30
img = Image.new("RGBA", (N * SS, N * SS), (0, 0, 0, 0))
dr = ImageDraw.Draw(img)

# LA PALETA DEL VACÍO DE LA CASA
BORDE = (16, 9, 26, 255)
CUERPO_ALTO = (122, 78, 188, 255)
CUERPO_MED = (86, 55, 148, 255)
CUERPO_BAJO = (52, 33, 92, 255)
LUZ = (168, 122, 224, 255)
ORO = (255, 214, 130, 255)
ORO_HONDO = (196, 148, 66, 255)
BLANCO = (255, 246, 225, 255)


def p(x, y, col):
    dr.rectangle([x * SS, y * SS, (x + 1) * SS - 1, (y + 1) * SS - 1], fill=col)


# === LA SILUETA DEL SACO (el botín clásico: pellizco arriba, barriga abajo) ===
sil = {
    8:  (13, 16),   # el cuello del pellizco
    9:  (12, 17),
    10: (11, 18),
    11: (10, 19),
    12: (9, 20),
    13: (8, 21),
    14: (8, 21),
    15: (7, 22),
    16: (7, 22),
    17: (7, 22),
    18: (7, 22),
    19: (7, 22),
    20: (8, 21),
    21: (8, 21),
    22: (9, 20),
    23: (10, 19),
    24: (11, 18),
    25: (12, 17),
    26: (13, 16),
}
for y, (x0, x1) in sil.items():
    for x in range(x0, x1 + 1):
        f = (y - 8) / 18
        col = CUERPO_ALTO if f < 0.28 else (CUERPO_MED if f < 0.68 else CUERPO_BAJO)
        p(x, y, col)

# EL BORDE (el contorno del saco — la masa del vacío)
for y, (x0, x1) in sil.items():
    p(x0, y, BORDE)
    p(x1, y, BORDE)
for y in (8, 26):
    for x in range(sil[y][0], sil[y][1] + 1):
        p(x, y, BORDE)

# LA LUZ del terciopelo (el bulto superior-izquierdo)
for x, y in ((9, 12), (8, 13), (8, 14), (8, 15), (9, 16), (10, 17)):
    p(x, y, LUZ)

# === LAS OREJAS DEL PELLIZCO ===
for x, y in ((10, 8), (10, 9), (9, 8), (19, 8), (19, 9), (20, 8)):
    p(x, y, BORDE)
p(10, 10, CUERPO_BAJO)
p(19, 10, CUERPO_BAJO)

# === EL LAZO DORADO (el nudo del saco) ===
for x in range(11, 19):
    p(x, 11, ORO)
p(11, 11, ORO_HONDO)
p(18, 11, ORO_HONDO)
for x in (13, 14, 15, 16):
    p(x, 10, ORO)

# === LA MANDORLA AL FRENTE (la marca de la Forma Eterna) ===
p(13, 17, ORO); p(14, 16, ORO); p(15, 16, ORO); p(16, 17, ORO)
p(14, 18, ORO); p(15, 18, ORO)
p(13, 19, ORO); p(16, 19, ORO)
p(14, 17, BLANCO); p(15, 17, BLANCO)
p(11, 18, ORO); p(18, 18, ORO)

# === LAS CHISPAS (el contenido asomando) ===
p(10, 22, LUZ)
p(20, 21, LUZ)
p(12, 14, LUZ)

img = img.resize((N, N), Image.NEAREST)
img.save("AethonMod/Content/Items/Bolsas/BolsaFormaEterna.png")
print("BolsaFormaEterna.png v2 OK (30×30 CRISP)")
