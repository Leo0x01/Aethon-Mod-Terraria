#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_v637_assets.py — v6.37 — EL ICONO DEL ANILLO RÚNICO DORSAL
REGENERADO: la triple corona de conjuro (el nuevo look literal).

El v6.36 dibujaba la elipse fucsia a escorzo; la corrección de la
librería (los anillos de los agujeros = los RÚNICOS) reconstruyó el
accesorio como EL CÍRCULO DE CONJURO LITERAL: tres aros concéntricos
(blanco íntimo + dorado + violeta externo) con las runas de pie y sus
perlas. Este icono lo pinta a 30×30 con la técnica CRISP de la casa
(elipses PIL continuas, runas de puntos limpios — la que dio 7/10 al
icono v6.36; la primera pasada con blur dio 3/10 en VLM y se descartó).
"""
import math
import os

from PIL import Image, ImageDraw

BASE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(BASE, "Content", "Items", "Cosmetics", "AnilloRunicoDorsalItem.png")

GOLD = (255, 180, 70)
GOLD_TIP = (255, 235, 175)
VIOLET = (140, 118, 255)
VIOLET_TIP = (205, 220, 255)
WHITE = (255, 245, 220)


def anillo_dorsal(path):
    """LA TRIPLE CORONA DE CONJURO: el círculo rúnico literal del
    agujero negro, colgado de la espalda (tres aros + fotones + runas)."""
    im = Image.new("RGBA", (30, 30), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    cx, cy = 15, 15

    # === EL ARO VIOLETA EXTERNO (la envoltura contrarrotante). ===
    d.ellipse((cx - 13, cy - 13, cx + 13, cy + 13),
              outline=VIOLET + (255,), width=2)

    # === EL ARO DORADO MEDIO (el círculo de 8 runas del conjuro). ===
    d.ellipse((cx - 10, cy - 10, cx + 10, cy + 10),
              outline=GOLD + (255,), width=2)

    # === EL ARO BLANCO ÍNTIMO (la corona viva, rápida). ===
    d.ellipse((cx - 7, cy - 7, cx + 7, cy + 7),
              outline=WHITE + (235,), width=1)

    # === EL ANILLO DE FOTONES (el horizonte interior). ===
    d.ellipse((cx - 4, cy - 4, cx + 4, cy + 4),
              outline=(210, 236, 255, 190), width=1)

    # === EL PUNTO DEL VACÍO (la nada que conjura). ===
    d.point((cx, cy), fill=(110, 84, 220, 255))

    # === LAS OCHO RUNAS DEL CÍRCULO DORADO (de pie sobre el aro):
    #     trazos en L pálido-cálido, 4 legibles al frente + 4 tenues. ===
    for g in range(8):
        a = g * math.tau / 8 + 0.19
        x = round(cx + math.cos(a) * 10)
        y = round(cy + math.sin(a) * 10)
        if math.sin(a) < -0.35:
            # La mitad trasera: perlas tenues.
            d.point((x, y), fill=(200, 178, 110, 200))
            continue
        # El frente: la runa en L (dos trazos de 2 px).
        d.point((x, y), fill=GOLD_TIP + (255,))
        d.point((x, y + 1), fill=GOLD_TIP + (255,))
        d.point((x + 1, y + 1), fill=(255, 214, 120, 235))

    # === LAS PERLAS DEL ARO VIOLETA (las gemas de la envoltura). ===
    for a in (math.pi * 0.25, math.pi * 0.75, math.pi * 1.25, math.pi * 1.75):
        x = cx + math.cos(a) * 13
        y = cy + math.sin(a) * 13
        d.ellipse((x - 1, y - 1, x + 1, y + 1), fill=VIOLET_TIP + (255,))

    im.save(path)
    print("icono", path, im.size)


anillo_dorsal(OUT)
