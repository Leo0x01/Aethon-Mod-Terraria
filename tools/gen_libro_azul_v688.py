#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.88 — EL SPRITE DEL LIBRO AZUL (30×30, PIL 4×SS — el estándar de la
casa para las armas de Investigaciones: Codice 28, SolEnMiniatura 30).
Un libro cerrado de TAPA AZUL con estrella plateada de 8 puntas y canto
dorado: el arma del árbol de habilidades."""
from PIL import Image, ImageDraw
import math

SS = 4
W = 30 * SS  # 120

img = Image.new("RGBA", (W, W), (0, 0, 0, 0))
d = ImageDraw.Draw(img)

# LA TAPA: el cuerpo del libro (un rect redondeado con el lomo a la
# izquierda). El libro apunta a la derecha: la tapa ocupa x 8..112.
lx, rx, ty, by = 10, 108, 14, 116
# LA SOMBRA propia (pegada al suelo del sprite).
d.rounded_rectangle([lx + 4, ty + 6, rx + 2, by + 4], 14, fill=(8, 14, 34, 160))
# EL CANTO DORADO (el borde exterior).
d.rounded_rectangle([lx, ty, rx, by], 14, fill=(212, 172, 84, 255))
# LA TAPA AZUL (el cuerpo).
d.rounded_rectangle([lx + 5, ty + 5, rx - 5, by - 5], 11,
                    fill=(28, 74, 190, 255))
# EL DEGRADÉ de la tapa (más claro arriba: la luz de la estrella).
for i in range(30):
    a = int(70 * (1 - i / 30))
    d.rounded_rectangle([lx + 6, ty + 6 + i * 3, rx - 6, ty + 6 + (i + 1) * 3],
                        10, fill=(90, 150, 255, a))
# EL LOMO (las páginas vistas: la franja clara a la derecha del libro).
d.rounded_rectangle([rx - 16, ty + 8, rx - 7, by - 8], 6, fill=(226, 232, 246, 255))
for i in range(5):  # las hojas
    yy = ty + 16 + i * 16
    d.line([rx - 15, yy, rx - 8, yy], fill=(160, 172, 196, 255), width=2)
# EL CINTILLO (la banda azul-clara que cierra la tapa).
d.rectangle([lx + 5, ty + 52, rx - 16, ty + 62], fill=(60, 120, 235, 255))
d.rectangle([lx + 5, ty + 55, rx - 16, ty + 59], fill=(140, 196, 255, 255))

# LA ESTRELLA PLATEADA de 8 puntas (el sello del árbol) — el centro de la tapa.
cx, cy = (lx + rx - 16) / 2, (ty + by) / 2
R, r = 26, 9
pts = []
for k in range(16):
    ang = k * math.pi / 8 - math.pi / 2
    rr = R if k % 2 == 0 else r
    pts.append((cx + rr * math.cos(ang), cy + rr * math.sin(ang)))
d.polygon(pts, fill=(232, 240, 252, 255))
# el núcleo de la estrella (el azul del libro).
pts2 = []
for k in range(16):
    ang = k * math.pi / 8 - math.pi / 2
    rr = (R * 0.55) if k % 2 == 0 else (r * 0.55)
    pts2.append((cx + rr * math.cos(ang), cy + rr * math.sin(ang)))
d.polygon(pts2, fill=(70, 130, 240, 255))

# EL BROCHE dorado (abajo-izquierda: el cierre).
d.ellipse([lx + 12, by - 18, lx + 26, by - 6], fill=(212, 172, 84, 255))
d.ellipse([lx + 16, by - 15, lx + 23, by - 9], fill=(120, 92, 40, 255))

img = img.resize((30, 30), Image.LANCZOS)
img.save("AethonMod/Content/Weapons/Investigaciones/LibroAzul.png")
print("OK — LibroAzul.png 30×30")
