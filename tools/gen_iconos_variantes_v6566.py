#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_iconos_variantes_v6566.py — LOS ICONOS DE LAS 9 VARIANTES DEL RC2.

Familia coherente con el RayoConcentradoDos.png original (44×44): fondo
violeta oscuro translúcido, HAZ DIAGONAL con el gradiente de la variante,
núcleo blanco y el DESTELLO DE 4 PUNTAS (la firma de los soles rúnicos)
en la boca. Cada variante se distingue por SUS DOS COLORES + su GLIFO de
técnica (el signo que cuenta qué hace).
"""
import math
from PIL import Image, ImageDraw, ImageFilter

DESTINO = '/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Content/Weapons/Definitivas'

# (clase, colorA, colorB, glifo) — los colores son los de VariantesRC2.Gradiente
VARIANTES = [
    ('RayoConcentradoPrisma',    (255, 90, 60),  (80, 230, 255), 'espectro'),   # 1
    ('RayoConcentradoEclipse',   (120, 60, 180), (216, 170, 255), 'luna'),       # 2
    ('RayoConcentradoTormenta',  (64, 128, 255), (214, 236, 255), 'rayo'),       # 3
    ('RayoConcentradoSolar',     (255, 196, 64), (255, 96, 24),  'sol'),         # 4
    ('RayoConcentradoVortice',   (64, 226, 232), (148, 72, 255), 'espiral'),     # 5
    ('RayoConcentradoEspectro',  (96, 214, 150), (226, 255, 238),'eco'),         # 6
    ('RayoConcentradoSierpe',    (255, 176, 66), (255, 234, 176),'onda'),        # 7
    ('RayoConcentradoCuantico',  (255, 72, 200), (72, 224, 255), 'ramas'),       # 8
    ('RayoConcentradoNova',      (255, 128, 196),(255, 238, 208),'anillo'),      # 9
]

S = 44


def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def icono(col_a, col_b, glifo):
    im = Image.new('RGBA', (S, S), (0, 0, 0, 0))

    # === 1. EL FONDO: orbe violeta oscuro translúcido (la casa del RC2) ===
    fondo = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    df = ImageDraw.Draw(fondo)
    df.ellipse((6, 6, S - 6, S - 6), fill=(53, 24, 80, 190))
    df.ellipse((10, 10, S - 10, S - 10), fill=(60, 30, 90, 110))
    fondo = fondo.filter(ImageFilter.GaussianBlur(1.2))
    im.alpha_composite(fondo)

    # === 2. EL HAZ DIAGONAL: de la boca (abajo-izq) a la punta (arriba-der) ===
    haz = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    dh = ImageDraw.Draw(haz)
    p0 = (10, 34)     # la boca
    p1 = (40, 8)      # la punta
    dx, dy = p1[0] - p0[0], p1[1] - p0[1]
    largo = math.hypot(dx, dy)
    ux, uy = dx / largo, dy / largo
    px, py = -uy, ux  # perpendicular
    pasos = 16
    for i in range(pasos):
        t0, t1 = i / pasos, (i + 1) / pasos
        c = lerp(col_a, col_b, (t0 + t1) / 2)
        w0 = 5.5 * (1.0 - 0.55 * t0)   # se afila hacia la punta
        w1 = 5.5 * (1.0 - 0.55 * t1)
        a0 = (p0[0] + dx * t0 + px * w0, p0[1] + dy * t0 + py * w0)
        b0 = (p0[0] + dx * t0 - px * w0, p0[1] + dy * t0 - py * w0)
        a1 = (p0[0] + dx * t1 + px * w1, p0[1] + dy * t1 + py * w1)
        b1 = (p0[0] + dx * t1 - px * w1, p0[1] + dy * t1 - py * w1)
        dh.polygon([a0, b0, b1, a1], fill=c + (235,))
    # EL NÚCLEO BLANCO (la vena central)
    for i in range(pasos):
        t0, t1 = i / pasos, (i + 1) / pasos
        w0 = 2.0 * (1.0 - 0.5 * t0)
        w1 = 2.0 * (1.0 - 0.5 * t1)
        a0 = (p0[0] + dx * t0 + px * w0, p0[1] + dy * t0 + py * w0)
        b0 = (p0[0] + dx * t0 - px * w0, p0[1] + dy * t0 - py * w0)
        a1 = (p0[0] + dx * t1 + px * w1, p0[1] + dy * t1 + py * w1)
        b1 = (p0[0] + dx * t1 - px * w1, p0[1] + dy * t1 - py * w1)
        dh.polygon([a0, b0, b1, a1], fill=(255, 250, 240, 235))
    im.alpha_composite(haz)

    # === 3. EL GLIFO DE LA TÉCNICA (la marca que cuenta qué hace) ===
    gl = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    dg = ImageDraw.Draw(gl)
    cG = lerp(col_b, (255, 255, 255), 0.35)
    g = cG + (255,)
    if glifo == 'espectro':      # Prisma: las 3 bandas RGB separadas
        for k, c in enumerate([(255, 70, 70), (90, 255, 120), (80, 200, 255)]):
            y = 9 + k * 4
            dg.line((7, y, 15, y - 4), fill=c + (255,), width=2)
    elif glifo == 'luna':        # Eclipse: creciente
        dg.ellipse((5, 8, 17, 20), fill=g)
        dg.ellipse((9, 9, 18, 18), fill=(45, 18, 70, 255))
    elif glifo == 'rayo':        # Tormenta: zigzag
        dg.line((6, 12, 11, 12, 8, 17, 13, 17, 7, 24), fill=g, width=2, joint='curve')
    elif glifo == 'sol':         # Solar: círculo + rayos
        dg.ellipse((7, 9, 15, 17), fill=g)
        for a in range(8):
            r0, r1 = 6.5, 10.5
            cx, cy = 11, 13
            x0 = cx + math.cos(a * math.pi / 4) * r0
            y0 = cy + math.sin(a * math.pi / 4) * r0
            x1 = cx + math.cos(a * math.pi / 4) * r1
            y1 = cy + math.sin(a * math.pi / 4) * r1
            dg.line((x0, y0, x1, y1), fill=g, width=2)
    elif glifo == 'espiral':     # Vórtice: remolino (dos arcos gruesos)
        dg.arc((4, 7, 18, 21), 300, 90, fill=g, width=3)
        dg.arc((8, 11, 14, 17), 120, 270, fill=g, width=2)
    elif glifo == 'eco':         # Espectro: dos arcos fantasma
        for k in range(2):
            dg.arc((5 + k * 3, 8 + k * 3, 17 + k * 3, 20 + k * 3), 200, 340,
                   fill=cG + (200 - k * 70,), width=2)
    elif glifo == 'onda':        # Sierpe: la onda serpenteante (grande y gruesa)
        pts = [(4 + i * 2.0, 15 + math.sin(i * 1.05) * 6.5) for i in range(8)]
        dg.line(pts, fill=g, width=3, joint='curve')
    elif glifo == 'ramas':       # Cuántico: el tronco que se parte
        dg.line((11, 23, 11, 17), fill=g, width=2)
        dg.line((11, 17, 6, 11), fill=g, width=2)
        dg.line((11, 17, 16, 11), fill=g, width=2)
        dg.line((11, 17, 11, 8), fill=g, width=1)
    elif glifo == 'anillo':      # Nova: dos anillos concéntricos (grandes)
        dg.ellipse((3, 6, 19, 22), outline=g, width=3)
        dg.ellipse((8, 11, 14, 17), outline=g, width=2)
    im.alpha_composite(gl)

    # === 4. EL DESTELLO DE 4 PUNTAS en la boca (la firma de los soles) ===
    tw = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    dt = ImageDraw.Draw(tw)
    bx, by = 10, 34
    dt.polygon([(bx - 8, by), (bx, by - 1.4), (bx + 8, by), (bx, by + 1.4)],
               fill=(255, 248, 225, 235))
    dt.polygon([(bx - 1.4, by - 8), (bx, by - 0.5), (bx + 1.4, by - 8), (bx, by + 8)],
               fill=(255, 248, 225, 235))
    dt.ellipse((bx - 3.4, by - 3.4, bx + 3.4, by + 3.4), fill=(255, 214, 120, 235))
    im.alpha_composite(tw)

    # el borde del orbe (el filete de la casa)
    db = ImageDraw.Draw(im)
    db.ellipse((6, 6, S - 6, S - 6), outline=lerp(col_b, (255, 255, 255), 0.25) + (220,), width=1)
    return im


for clase, ca, cb, glifo in VARIANTES:
    icono(ca, cb, glifo).save(f'{DESTINO}/{clase}.png')
    print(f'  {clase}.png  ({glifo})')

print('ICONOS GENERADOS: 9')
