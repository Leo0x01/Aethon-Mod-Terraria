#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
iconos_v6114.py — LOS DOS ICONOS DE LA v6.114 (la casa: PIL ×4 SS).

  · FormaEternaItem.png (30×30) — EL COSMÉTICO DEFINITIVO: el disco del
    vacío (26,14,44), la MANDORLA DORADA al centro (la forma que no se
    apaga), su núcleo violeta-blanco, LOS OCHO RAYOS alrededor (los
    ocho del panteón) y las cuatro estrellas de las esquinas.

  · BolsaFormaEterna.png (30×30) — LA BOLSA NUEVA: la silueta de la
    casa (donante: BolsaAuras.png) recolorida a la paleta del vacío
    (masa negro-violeta + nudo dorado) con la mandorla chiquita de la
    Forma Eterna en la ventana.
"""
from PIL import Image, ImageDraw, ImageFilter
import math

SS = 4
N = 30


def nuevo():
    return Image.new("RGBA", (N * SS, N * SS), (0, 0, 0, 0))


def mandorla(dr, cx, cy, rx, ry, grosor, color, rot=0.0):
    """La almendra: dos arcos que se cortan — la forma de la gloria."""
    pts = []
    for i in range(72):
        a = math.pi * 2 * i / 72
        # vesica: |x/rx|^0.72 + |y/ry|^0.72 = 1 (hombros suaves)
        ca, sa = math.cos(a + rot), math.sin(a + rot)
        r = 1.0 / (abs(ca) ** 0.72 + abs(sa) ** 0.72) ** (1 / 0.72)
        pts.append((cx + math.cos(a) * rx * r, cy + math.sin(a) * ry * r))
    dr.polygon(pts, outline=color, width=grosor)


# ======================================================================
#  1. LA FORMA ETERNA — el emblema definitivo
# ======================================================================
img = nuevo()
dr = ImageDraw.Draw(img)
S = N * SS
c = S / 2

# EL DISCO DEL VACÍO (el fondo): el 26,14,44 de la casa con el halo hondo
for r in range(int(S * 0.46), 0, -1):
    f = 1 - r / (S * 0.46)
    col = (26 + int(22 * f), 14 + int(18 * f), 44 + int(26 * f), 255)
    dr.ellipse([c - r, c - r, c + r, c + r], fill=col)

# EL BORDE DORADO (el aro de la eternidad)
dr.ellipse([c - S * 0.46, c - S * 0.46, c + S * 0.46, c + S * 0.46],
           outline=(212, 168, 84, 255), width=SS)

# LOS OCHO RAYOS (los ocho del panteón) — chispas doradas radiales
for i in range(8):
    a = math.pi * 2 * i / 8 + math.pi / 8
    r0, r1 = S * 0.30, S * 0.435
    x0, y0 = c + math.cos(a) * r0, c + math.sin(a) * r0
    x1, y1 = c + math.cos(a) * r1, c + math.sin(a) * r1
    dr.line([x0, y0, x1, y1], fill=(232, 196, 120, 255), width=SS)

# EL NÚCLEO (la forma que no se apaga): el resplandor violeta vivo
for r in range(int(S * 0.16), 0, -1):
    f = 1 - r / (S * 0.16)
    col = (150 + int(70 * f), 84 + int(80 * f), 235, int(200 + 55 * f))
    dr.ellipse([c - r, c - r, c + r, c + r], fill=col)

# LA MANDORLA (la gloria) dorada sobre el núcleo
mandorla(dr, c, c, S * 0.26, S * 0.155, SS + 1, (255, 214, 130, 255))
# y su corazón blanco (la llama que no se apaga)
mandorla(dr, c, c, S * 0.12, S * 0.065, SS, (255, 246, 225, 255))

# LAS CUATRO ESTRELLAS (las esquinas del disco)
for dx, dy in ((-1, -1), (1, -1), (-1, 1), (1, 1)):
    sx, sy = c + dx * S * 0.33, c + dy * S * 0.33
    dr.line([sx - SS * 1.6, sy, sx + SS * 1.6, sy], fill=(226, 206, 255, 235), width=SS)
    dr.line([sx, sy - SS * 1.6, sx, sy + SS * 1.6], fill=(226, 206, 255, 235), width=SS)

img = img.resize((N, N), Image.LANCZOS)
img.save("AethonMod/Content/Items/Cosmetics/FormaEternaItem.png")
print("FormaEternaItem.png OK (30×30)")

# ======================================================================
#  2. LA BOLSA DE LA FORMA ETERNA — la silueta de la casa, paleta del vacío
# ======================================================================
DON = "AethonMod/Content/Items/Bolsas/BolsaAuras.png"
DST = "AethonMod/Content/Items/Bolsas/BolsaFormaEterna.png"

base = Image.open(DON).convert("RGBA")
px = base.load()
W, H = base.size

for y in range(H):
    for x in range(W):
        r, g, b, a = px[x, y]
        if a < 12:
            continue
        lum = (r + g + b) / 3
        if r > 190 and g > 140 and b < 140:
            # los dorados del donante → ORO DE LA CASA
            px[x, y] = (240, 198, 110, a)
        elif lum > 150:
            # las luces claras → BLANCO VIOLETA
            px[x, y] = (228, 214, 252, a)
        elif lum > 95:
            # el cuerpo medio → VIOLETA HONDO
            px[x, y] = (122, 78, 188, a)
        else:
            # las sombras → MASA DEL VACÍO
            px[x, y] = (34, 20, 56, a)

# LA MANDORLA CHIQUITA sobre la ventana de la bolsa (la marca del contenido)
d2 = ImageDraw.Draw(base)
cx, cy = W / 2, H * 0.60
mandorla(d2, cx, cy, 5.2, 3.1, 1, (255, 214, 130, 255))
mandorla(d2, cx, cy, 2.4, 1.3, 1, (255, 246, 225, 255))
d2.line([cx - 7, cy, cx - 4.6, cy], fill=(232, 196, 120, 255), width=1)
d2.line([cx + 4.6, cy, cx + 7, cy], fill=(232, 196, 120, 255), width=1)

base.save(DST)
print(f"BolsaFormaEterna.png OK ({W}×{H})")
