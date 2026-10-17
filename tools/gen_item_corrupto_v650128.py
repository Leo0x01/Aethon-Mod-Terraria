#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_item_corrupto_v650128.py — EL SPRITE DE LA ESENCIA CORRUPTA (v6.50.128).

Lo que queda de un item tras pasar por el HAZ DEL ALTAR: una CÁSCARA
oscura violeta-negra, retorcida, con un OJO MUERTO (el iris apagado del
Eterno) y GRIETAS violetas que laten — el resto de lo que fue.

22×22 px, pixel art limpio estilo Terraria.
"""
from PIL import Image
import random

random.seed(128)

W = H = 22
img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
px = img.load()

# === LA PALETA ===
TINTA      = (18, 8, 30, 255)      # el cuerpo (negro-violeta)
TINTA_CLAR = (40, 22, 62, 255)     # el lomo iluminado
TINTA_UMBR = (10, 4, 18, 255)      # la panza hundida
VIOLETA    = (139, 72, 191, 255)   # las grietas que laten
VIOLETA_CL = (186, 122, 236, 255)  # el brillo de las grietas
BLANCO    = (235, 228, 246, 255)   # la esclerótica del ojo muerto
ROJO      = (196, 44, 74, 255)     # la pupila apagada

def poner(x, y, c):
    if 0 <= x < W and 0 <= y < H:
        px[x, y] = c

# === 1. EL CUERPO: una masa retorcida (elipse a la que le crecen bultos) ===
cx, cy = 10.5, 11.0
for y in range(3, 20):
    for x in range(3, 19):
        dx = (x - cx) / 7.6
        dy = (y - cy) / 8.0
        d = dx * dx + dy * dy
        # los bultos: el radio respira con ruido determinista
        bulto = 0.16 * random.Random(x * 31 + y).random()
        if d <= 1.0 + bulto:
            # el sombreado: lomo arriba-izquierda
            luz = dx * -0.6 + dy * -0.7
            if luz > 0.35:
                c = TINTA_CLAR
            elif dx * 0.7 + dy * 0.7 > 0.45:
                c = TINTA_UMBR
            else:
                c = TINTA
            poner(x, y, c)

# === 2. LAS GRIETAS VIOLETAS (lo que late dentro de la cáscara) ===
grietas = [
    # (x, y) pares que cruzan el cuerpo — como venas abiertas
    (6, 6), (7, 7), (8, 8), (9, 9),
    (15, 5), (14, 6), (13, 7), (12, 8),
    (5, 13), (6, 14), (7, 15), (8, 16),
    (16, 12), (15, 13), (15, 14), (14, 15),
    (10, 4), (11, 4), (10, 18), (11, 17),
]
for (x, y) in grietas:
    poner(x, y, VIOLETA)
# los destellos de las grietas (viven en los extremos)
poner(6, 6, VIOLETA_CL)
poner(15, 5, VIOLETA_CL)
poner(8, 16, VIOLETA_CL)
poner(16, 12, VIOLETA_CL)

# === 3. EL OJO MUERTO (lo que queda de lo que fue) ===
# la esclerótica
for y in range(8, 12):
    for x in range(8, 14):
        dx = (x - 10.5) / 3.0
        dy = (y - 9.5) / 2.0
        if dx * dx + dy * dy <= 1.0:
            poner(x, y, BLANCO)
# el iris apagado (rojo oscuro)
poner(10, 9, ROJO)
poner(11, 9, ROJO)
poner(10, 10, ROJO)
poner(11, 10, ROJO)
# el brillo ausente (un pixel blanco en el iris — la chispa que quedó)
poner(10, 9, (255, 240, 240, 255))

# === 4. EL RIM (la cáscara se recorta contra el mundo) ===
# unos cuantos trazos aditivos en el contorno superior (el lomo brilla un poco)
for x, y in [(5, 5), (12, 4), (16, 6), (4, 11), (17, 11), (7, 18), (13, 18)]:
    poner(x, y, VIOLETA_CL)

out = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Content/Items/Altar/ItemCorrupto.png"
import os
os.makedirs(os.path.dirname(out), exist_ok=True)
img.save(out)
print("OK →", out, img.size)
img.resize((22 * 12, 22 * 12), Image.NEAREST).save("/tmp/itemcorrupto_big.png")
