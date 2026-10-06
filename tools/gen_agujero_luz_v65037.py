#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
v6.50.37 — EL AGUJERO DE LUZ (AgujeroLuz.png, 256×256).

La textura de LA OSCURIDAD PRIMORDIAL: un gradiente radial de LUMINANCIA
(blanco en el centro → negro en el borde, ALPHA 255 en todo el lienzo).

NO es un glow aditivo (ésos viven en RGB con alpha desvaneciente): esta
textura se compone en la MÁSCARA DE LUZ (un render target a media
resolución) con lote ADITIVO — el blanco suma, el negro no aporta — y
luego la máscara entera se multiplica sobre la escena (BlendState
multiplicativo: escena × máscara). El blanco del centro RESTAURA la
escena (×1), el negro del borde la MATA (×0), y el falloff suave del
medio es la penumbra. Dos agujeros que se tocan SUMAN (aditivo) — la
luz nunca se resta a sí misma.

Perfil: r/R < 0.34 → 255 · smoothstep 0.34→0.50 → 0 · alpha 255 fijo.
La caída suave completa (sin meseta del núcleo, sin escalones) es la
firma de la casa desde v6.50.18 (NovaBurst).
"""

from PIL import Image
import math

LADO = 256
CENTRO = (LADO - 1) / 2.0
R_NUCLEO = 0.34   # meseta blanca (luz plena)
R_BORDE = 0.50    # negro absoluto (la oscuridad)

def smoothstep(a, b, x):
    t = max(0.0, min(1.0, (x - a) / (b - a)))
    return t * t * (3.0 - 2.0 * t)

img = Image.new("RGBA", (LADO, LADO), (0, 0, 0, 255))
px = img.load()

for y in range(LADO):
    for x in range(LADO):
        dx = x - CENTRO
        dy = y - CENTRO
        r = math.hypot(dx, dy) / CENTRO   # 0..~1.41
        v = 255.0 if r <= R_NUCLEO else 255.0 * (1.0 - smoothstep(R_NUCLEO, R_BORDE, r))
        if r > R_BORDE:
            v = 0.0
        c = int(round(v))
        px[x, y] = (c, c, c, 255)

SALIDA = "AethonMod/Content/Effects/Procedural/AgujeroLuz.png"
img.save(SALIDA, optimize=True)

# verificación: perfil en el eje horizontal
c = LADO // 2
print("AgujeroLuz.png", img.size, "->", SALIDA)
print("perfil:", [px[c + d, c][0] for d in (0, 30, 60, 80, 90, 100, 110, 127)])
