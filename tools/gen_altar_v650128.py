#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_altar_v650128.py — EL SPRITE DEL ALTAR DE LA CAÍDA (v6.50.128).

La referencia del usuario: el bloque DORADO/AMARILLO brillante (#f1c40f)
con un símbolo negro en forma de espiral, en la base del pico central,
con bordes iluminados en violeta (#9b59b6) y su haz de luz vertical.

16×16 px, pixel art limpio estilo Terraria:
  · el bloque dorado con biselado (claro arriba-izq, sombra abajo-der)
  · la ESPIRAL negra en el centro (el símbolo de la referencia)
  · el rim violeta en los bordes exteriores (la energía del haz)
"""
from PIL import Image

W = H = 16
img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
px = img.load()

# === LA PALETA (de la referencia) ===
ORO_CLARO   = (255, 232, 148, 255)   # el brillo del bisel
ORO         = (241, 196, 15, 255)     # el cuerpo dorado (#f1c40f)
ORO_HONDO   = (192, 143, 8, 255)      # la sombra del bisel
ORO_UMBRA   = (141, 100, 7, 255)      # la esquina más hundida
NEGRO       = (12, 8, 20, 255)        # la espiral (negro con un alma violeta)
VIOLETA     = (155, 89, 182, 255)     # el rim del haz (#9b59b6)
VIOLETA_TEN = (94, 52, 122, 255)      # el rim en la sombra

def poner(x, y, c):
    if 0 <= x < W and 0 <= y < H:
        px[x, y] = c

# === 1. EL BLOQUE DORADO (con biselado de 4 tonos) ===
for y in range(1, 15):
    for x in range(1, 15):
        poner(x, y, ORO)

# el biselado: luz arriba-izquierda, sombra abajo-derecha
for x in range(1, 15):
    poner(x, 1, ORO_CLARO)
    poner(x, 14, ORO_HONDO)
for y in range(1, 15):
    poner(1, y, ORO_CLARO)
    poner(14, y, ORO_HONDO)
# las esquinas hundidas (el toque de volumen)
poner(14, 14, ORO_UMBRA)
poner(13, 14, ORO_UMBRA)
poner(14, 13, ORO_UMBRA)
# la esquina de luz pura
poner(1, 1, (255, 244, 190, 255))

# === 2. LA ESPIRAL NEGRA (el símbolo de la referencia) ===
# una espiral cuadrada de 3 vueltas, dibujada a mano (pixel a pixel):
# el trazo empieza afuera (izquierda) y gira hacia adentro.
espiral = [
    (4, 4), (5, 4), (6, 4), (7, 4), (8, 4), (9, 4), (10, 4), (11, 4),   # arriba →
    (11, 5), (11, 6), (11, 7), (11, 8), (11, 9), (11, 10),              # ↓ derecha
    (10, 10), (9, 10), (8, 10), (7, 10), (6, 10), (5, 10),              # ← abajo
    (5, 9), (5, 8), (5, 7), (5, 6),                                     # ↑ izquierda
    (6, 6), (7, 6), (8, 6), (9, 6),                                     # → adentro
    (9, 7), (9, 8),                                                     # ↓ el corazón
    (8, 8), (7, 8),                                                     # ← el centro
]
for (x, y) in espiral:
    poner(x, y, NEGRO)
# el corazón de la espiral late violeta (la energía que corrompe)
poner(7, 7, (94, 52, 122, 255))
poner(8, 7, (60, 32, 84, 255))

# === 3. EL RIM VIOLETA (los bordes exteriores — la firma del haz) ===
for x in range(W):
    poner(x, 0, VIOLETA)
    poner(x, H - 1, VIOLETA if x < 8 else VIOLETA_TEN)
for y in range(H):
    poner(0, y, VIOLETA)
    poner(W - 1, y, VIOLETA_TEN if y >= 8 else VIOLETA)
# las esquinas del rim se disuelven (menos cuadrado, más energía)
poner(0, 0, VIOLETA)
poner(W - 1, H - 1, VIOLETA_TEN)
poner(0, H - 1, VIOLETA_TEN)
poner(W - 1, 0, VIOLETA)

# === 4. LAS CHISPAS DEL RIM (el altar brilla en los bordes) ===
poner(2, 0, (255, 240, 185, 255))
poner(13, 0, (255, 240, 185, 255))
poner(0, 2, (255, 240, 185, 255))
poner(0, 13, (255, 240, 185, 255))

out = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Content/Tiles/AltarDeLaCaida.png"
img.save(out)
print("OK →", out, img.size)
