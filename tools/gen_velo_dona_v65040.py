#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
v6.50.40 — LA DONA DEL VELO (VeloDona.png, 256×256).

La textura del MOSAICO DE LA OSCURIDAD: un gradiente radial de ALPHA
(transparente en el centro → opaco en el borde, RGB blanco fijo).

NO es un glow aditivo ni la vieja máscara de luminancia: esta textura
se dibuja con ALFA sobre la escena como si fuera EL VELO MISMO — el
núcleo transparente DEJA VER el mundo (el agujero de luz: Aethon, el
círculo del Grimorio ≥50, las balas), la penumbra suave es el falloff
de Don't Starve, y el borde opaco ES la oscuridad plena que las bandas
del mosaico costuran con el resto del velo. Como el mosaico es disjunto
(cada píxel del velo lo pinta UNA sola pieza), nunca hay doble
oscurecimiento ni costuras.

Perfil (r = distancia al centro / semilado, 0..~1.41):
  r <= 0.76          → alpha 0    (el núcleo LIMPIO: la luz quita la oscuridad)
  0.76 < r < 0.90    → smoothstep (la penumbra: la luz se apaga suave)
  r >= 0.90          → alpha 255  (oscuridad PLENA — hasta las esquinas del
                                   cuadrado: el empalme con las bandas es exacto)

El borde de la textura y sus esquinas son OPACOS por diseño: la plaza de
cada luz es un cuadrado y las bandas del velo pleno costuran contra sus
cuatro lados — el borde de la dona tiene que ser velo pleno para que el
empalme sea invisible.

La caída suave (smoothstep, sin meseta del núcleo, sin escalones) es la
firma de la casa desde v6.50.18 (NovaBurst).
"""

from PIL import Image
import math

LADO = 256
CENTRO = (LADO - 1) / 2.0
R_NUCLEO = 0.76   # meseta transparente (la luz plena — el agujero)
R_LLENO = 0.90    # oscuridad plena (el velo que la luz no alcanza)

def smoothstep(a, b, x):
    t = max(0.0, min(1.0, (x - a) / (b - a)))
    return t * t * (3.0 - 2.0 * t)

img = Image.new("RGBA", (LADO, LADO), (255, 255, 255, 255))
px = img.load()

for y in range(LADO):
    for x in range(LADO):
        dx = x - CENTRO
        dy = y - CENTRO
        r = math.hypot(dx, dy) / CENTRO   # 0..~1.41
        if r <= R_NUCLEO:
            a = 0.0
        elif r >= R_LLENO:
            a = 255.0
        else:
            a = 255.0 * smoothstep(R_NUCLEO, R_LLENO, r)
        px[x, y] = (255, 255, 255, int(round(a)))

SALIDA = "AethonMod/Content/Effects/Procedural/VeloDona.png"
img.save(SALIDA, optimize=True)

# verificación: perfil en el eje horizontal (el núcleo limpio y la penumbra)
c = LADO // 2
print("VeloDona.png", img.size, "->", SALIDA)
print("perfil alpha:", [px[c + d, c][3] for d in (0, 60, 90, 95, 100, 105, 110, 115, 127)])
# las esquinas del cuadrado TIENEN que ser opacas (el empalme con las bandas)
print("esquina (0,0):", px[0, 0][3], "· borde (0,c):", px[0, c][3], "· borde (c,0):", px[c, 0][3])
