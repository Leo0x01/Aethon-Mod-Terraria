#!/usr/bin/env python3
"""
gen_staves_icons.py — Iconos 28×30 para los 2 bastones nuevos.

Mismo espíritu que el icono del carmesí (bastón oscuro + orbe arriba):
  · OlvidoBlackHoleStaff  — orbe NEGRO con anillo carmesí + brazo espiral.
  · FusionBlackHoleStaff  — orbe NEGRO con anillo NARANJA (Gargantua) y
    hojas carmesí (vacío) entremezcladas — la doble estirpe.
Supersampling ×8 y reducción LANCZOS para nitidez tipo pixel-art suave.
"""
import numpy as np
from PIL import Image

SS = 8
W, H = 28, 30


def make_icon(orb_colors, ring_colors, arm_color, filename):
    img = Image.new('RGBA', (W*SS, H*SS), (0, 0, 0, 0))
    from PIL import ImageDraw
    d = ImageDraw.Draw(img)

    # ---- el bastón: vara vertical oscura con nudos ----
    staff_x = W*SS // 2
    top_y = int(9*SS)
    bot_y = int(29*SS)
    for i, y in enumerate(range(top_y, bot_y, SS)):
        t = i / max(1, (bot_y - top_y)//SS)
        # vara de gris-azulado oscuro con brillo lateral
        base = (52, 30, 44)
        lit = (120, 70, 90)
        d.rectangle([staff_x - SS, y, staff_x + SS - 1, y + SS - 1], fill=base + (255,))
        d.rectangle([staff_x - SS, y, staff_x - 1, y + SS - 1], fill=lit + (255,))
        if i % 5 == 2:  # nudo
            d.rectangle([staff_x - 2*SS, y, staff_x + 2*SS - 1, y + SS - 1], fill=(70, 40, 55, 255))

    # garra superior que sostiene el orbe
    d.polygon([(staff_x - 3*SS, top_y), (staff_x + 3*SS, top_y),
               (staff_x + 2*SS, top_y + 2*SS), (staff_x - 2*SS, top_y + 2*SS)],
              fill=(90, 50, 66, 255))

    # ---- EL ORBE: agujero negro en miniatura ----
    cx, cy = staff_x, int(6*SS)
    r_orb = int(4.6*SS)

    # halo exterior tenue
    d.ellipse([cx - r_orb - 2*SS, cy - r_orb - 2*SS, cx + r_orb + 2*SS, cy + r_orb + 2*SS],
              fill=arm_color + (70,))

    # anillo de plasma (2 colores alternos)
    import math
    for k in range(2):
        rr = r_orb + int((k + 0.5)*SS*0.9)
        col = ring_colors[k % len(ring_colors)]
        d.ellipse([cx - rr, cy - rr, cx + rr, cy + rr], outline=col + (255,), width=SS)

    # esfera negra
    d.ellipse([cx - r_orb, cy - r_orb, cx + r_orb, cy + r_orb], fill=(6, 2, 8, 255))
    # borde de color de la esfera (rim)
    d.ellipse([cx - r_orb, cy - r_orb, cx + r_orb, cy + r_orb],
              outline=orb_colors[0] + (255,), width=max(1, SS//2))
    # destello interior (la estrella)
    d.ellipse([cx - SS, cy - SS, cx + SS, cy + SS], fill=orb_colors[1] + (255,))
    # brazo espiral (3 trazos)
    for i in range(3):
        ang0 = math.radians(90 + i*120)
        for s in range(6):
            a0 = ang0 + s*0.22
            a1 = a0 + 0.26
            r0 = r_orb + (s + 1)*SS*0.8
            r1 = r_orb + (s + 2)*SS*0.8
            x0, y0 = cx + math.cos(a0)*r0, cy + math.sin(a0)*r0
            x1, y1 = cx + math.cos(a1)*r1, cy + math.sin(a1)*r1
            fade = 255 - s*38
            d.line([(x0, y0), (x1, y1)], fill=arm_color + (max(0, fade),), width=SS)

    img = img.resize((W, H), Image.LANCZOS)
    img.save(filename)
    print(f'{filename}: {img.size}')


# OLVIDO: anillo carmesí/fucsia, estrella rosa, brazos fucsia
make_icon(
    orb_colors=[(255, 60, 130), (255, 220, 240)],
    ring_colors=[(255, 40, 110), (255, 130, 190)],
    arm_color=(238, 40, 90),
    filename='OlvidoBlackHoleStaff.png')

# FUSIÓN: anillo naranja (Gargantua) + carmesí (vacío), estrella cálida
make_icon(
    orb_colors=[(255, 170, 70), (255, 245, 220)],
    ring_colors=[(255, 160, 60), (255, 60, 120)],
    arm_color=(255, 150, 70),
    filename='FusionBlackHoleStaff.png')
