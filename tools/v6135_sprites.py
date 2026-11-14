#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
v6.135 — LOS SEIS SPRITES de las alas mitológicas (30×30, el estilo de
la casa: la silueta del ala-lámina con la rampa de la paleta propia).
"""
import math
import os

from PIL import Image, ImageDraw

BASE = '/home/z/my-project/mod/AethonMod/Content/Items/Alas'

# paletas: (vivo, hondo, fondo) — las mismas de las entradas del catálogo
MITOS = {
    'AlasSerafinDelVacio': [(235, 120, 255), (158, 66, 214), (44, 16, 64), (255, 220, 255)],
    'AlasValquiriaRunica': [(218, 232, 255), (130, 150, 190), (22, 28, 48), (240, 248, 255)],
    'AlasQuetzalEmplumado': [(94, 222, 198), (46, 168, 146), (10, 44, 40), (220, 255, 248)],
    'AlasFenixDelRenacer': [(255, 244, 214), (255, 150, 64), (150, 40, 20), (255, 235, 200)],
    'AlasTenguDelCrepusculo': [(92, 110, 158), (38, 46, 76), (8, 10, 20), (170, 190, 240)],
    'AlasDragonDelVacio': [(200, 64, 72), (108, 28, 40), (20, 6, 10), (255, 190, 190)],
}


def lerp(a, b, t):
    return tuple(int(a[c] + (b[c] - a[c]) * t) for c in range(3))


def silueta_alas(vivo, hondo, fondo, brillo, plumas=5, curva=0.18, ancho_r=2.6):
    """La silueta de la casa: dos abanicos de plumas-tiras espejados,
    la rampa vivo→hondo→fondo y el borde con brillo."""
    im = Image.new('RGBA', (30, 30), (0, 0, 0, 0))
    d = ImageDraw.Draw(im, 'RGBA')
    # el cuerpo: la mota central
    d.rounded_rectangle([13, 15, 17, 19], 2, fill=fondo + (255,))
    for lado in (-1, 1):
        cx = 15 + lado * 2.5
        for i in range(plumas):
            n = i / (plumas - 1)
            # el abanico: de arriba (-100°) a abajo (-10°)
            ang = math.radians(-100 + 78 * n)
            largo = 11 + 3 * math.sin(math.radians(70 * n)) - 2 * n
            x0 = cx + math.cos(ang) * 2
            y0 = 16 + math.sin(ang) * 2
            x1 = cx + math.cos(ang) * (largo + 1.5)
            y1 = 16 + math.sin(ang) * (largo + 1.5) + curva * 6 * n * n
            # la rampa: vivo en la raíz → hondo en la punta
            col = lerp(vivo, hondo, 0.55 * n)
            d.line([(x0, y0), (x1, y1)], fill=col + (240,), width=2)
            # la punta con brillo
            d.ellipse([x1 - 1, y1 - 1, x1 + 1, y1 + 1], fill=lerp(brillo, col, 0.4) + (255,))
    # el velo entre plumas (la membrana sutil)
    return im


def silueta_dragon(vivo, hondo, fondo, brillo):
    """El dragón: DOS dedos de membrana por lado (la vela gorda)."""
    im = Image.new('RGBA', (30, 30), (0, 0, 0, 0))
    d = ImageDraw.Draw(im, 'RGBA')
    d.rounded_rectangle([13, 15, 17, 19], 2, fill=fondo + (255,))
    for lado in (-1, 1):
        cx = 15 + lado * 2.5
        # los dos dedos: arriba y medio
        for k, ang0 in enumerate((-95, -45)):
            for j in range(3):
                ang = math.radians(ang0 + 22 * j)
                largo = 12 - 2.5 * k
                x0 = cx + math.cos(ang) * 2
                y0 = 16 + math.sin(ang) * 2
                x1 = cx + math.cos(ang) * largo
                y1 = 16 + math.sin(ang) * largo
                col = lerp(vivo, hondo, 0.3 + 0.25 * j)
                d.line([(x0, y0), (x1, y1)], fill=col + (230,), width=2)
        # la membrana entre los dedos
        p1 = (cx + math.cos(math.radians(-95)) * 12, 16 + math.sin(math.radians(-95)) * 12)
        p2 = (cx + math.cos(math.radians(-73)) * 12, 16 + math.sin(math.radians(-73)) * 12)
        p3 = (cx + math.cos(math.radians(-45)) * 10, 16 + math.sin(math.radians(-45)) * 10)
        d.polygon([p1, p2, p3], fill=lerp(hondo, fondo, 0.35) + (150,))
        d.line([p1, p2, p3], fill=lerp(brillo, vivo, 0.5) + (200,), width=1)
    return im


for nombre, (vivo, hondo, fondo, brillo) in MITOS.items():
    if 'Dragon' in nombre:
        im = silueta_dragon(vivo, hondo, fondo, brillo)
    elif 'Valquiria' in nombre:
        im = silueta_alas(vivo, hondo, fondo, brillo, plumas=7, curva=0.0)
    elif 'Quetzal' in nombre:
        im = silueta_alas(vivo, hondo, fondo, brillo, plumas=7, curva=0.42)
    elif 'Tengu' in nombre:
        im = silueta_alas(vivo, hondo, fondo, brillo, plumas=7, curva=0.2)
    else:
        im = silueta_alas(vivo, hondo, fondo, brillo, plumas=5)
    ruta = os.path.join(BASE, f'{nombre}.png')
    im.save(ruta)
    # verificación de la casa: existe, >0 B, no en blanco
    im2 = Image.open(ruta).convert('L')
    h = im2.histogram()
    ratio = 1 - h[0] / sum(h)   # alfa-0 cuenta como negro en L… se mide distinto
    print(f'{nombre}.png {im.size} px no-transparentes: {sum(1 for p in Image.open(ruta).getdata() if p[3]>0)}')
print("sprites listos")
