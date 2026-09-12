#!/usr/bin/env python3
"""Recortes de la referencia + zoom ASCII de la región central del agujero."""
import numpy as np
from PIL import Image

IMG = "/home/z/my-project/upload/pasted_image_1789228267167.png"
img = Image.open(IMG).convert("RGBA")
W, H = img.size
a = np.asarray(img).astype(np.float32)
rgb = a[..., :3]
lum = rgb.mean(axis=2)

# Recorte 1: la región central-izquierda (el agujero negro)
img.crop((60, 20, 430, 290)).save("/home/z/my-project/AethonMod/research/gargantua/ref_crop_hole.png")
# Recorte 2: lado derecho
img.crop((380, 0, 627, 294)).save("/home/z/my-project/AethonMod/research/gargantua/ref_crop_right.png")

# Zoom ASCII denso de x[60..430], y[20..290] — celda de 4px
print("ZOOM x[60..430] y[20..290], celda 4x4 (' ' negro, . tenue, - medio, + brillante, * muy brillante, # blanco):")
for y in range(20, 290, 4):
    line = ""
    for x in range(60, 430, 4):
        v = lum[y:y+4, x:x+4].mean()
        if v < 6: ch = " "
        elif v < 20: ch = "."
        elif v < 50: ch = "-"
        elif v < 100: ch = "+"
        elif v < 170: ch = "*"
        else: ch = "#"
        line += ch
    print(f"{y:3d} {line}")
print("     " + "".join(str((60 + i * 4) // 100 % 10) for i in range(92)))
print("     " + "".join(str((60 + i * 4) // 10 % 10) for i in range(92)))
