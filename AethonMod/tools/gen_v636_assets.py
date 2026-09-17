#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_v636_assets.py — LOS ASSETS DE LOS CUATRO CÓDIGOS VIVOS + EL ANILLO
RÚNICO DORSAL + LA BOLSA (v6.36).

Pixel-art 30×30 estilo el mod (bastón diagonal + cabeza temática):
  · DanzaOrbesStaff      — cabeza: el sol dorado con planeta y luna encadenados
  · LenteAbismoStaff     — cabeza: el agujero negro con anillo magenta y estrellas dobladas
  · SolVivoStaff         — cabeza: el sol latiendo con prominencias
  · SierpeEstelarStaff   — cabeza: la sierpe estelar (cabeza + aleta + espina)

Y:
  · AnilloRunicoDorsalItem 30×30 — el anillo rúnico a escorzo (accesorio)
  · BolsaCodigosVivos 30×30      — el saco de la casa con runas vivas
  · Los sprites 76×76 de los 4 proyectiles (glow suave del color).
"""
import math
import os
import random

from PIL import Image, ImageDraw

BASE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
W = os.path.join(BASE, "Content", "Weapons", "Cosmic")
P = os.path.join(BASE, "Content", "Projectiles", "Cosmic")
I = os.path.join(BASE, "Content", "Items", "Cosmetics")
B = os.path.join(BASE, "Content", "Items", "Bolsas")


def baston_base(draw, madero=(94, 62, 38)):
    """El bastón diagonal clásico (abajo-izq → arriba-der) + engarce."""
    for i in range(17):
        x, y = 6 + i, 23 - i
        c = madero if i % 5 < 3 else (118, 80, 48)
        draw.point((x, y), fill=c)
        draw.point((x + 1, y), fill=(66, 44, 28))
    # El engarce dorado bajo la cabeza.
    for dx, dy in ((21, 7), (22, 6), (22, 8), (23, 7)):
        draw.point((dx, dy), fill=(255, 214, 106))


def danza(path):
    """EL SOL con su planeta y su luna ENCADENADOS (la órbita en miniatura)."""
    im = Image.new("RGBA", (30, 30), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    baston_base(d)
    # EL SOL (la raíz dorada — 3×3 con núcleo blanco).
    sx, sy = 22, 7
    d.ellipse((sx - 2, sy - 2, sx + 2, sy + 2), fill=(255, 196, 92, 255))
    d.ellipse((sx - 1, sy - 1, sx + 1, sy + 1), fill=(255, 244, 214, 255))
    # EL ENLACE orbital hacia el planeta (la cuerda de luz).
    for st in range(4):
        t = st / 3
        d.point((round(sx - 1 - 3.4 * t), round(sy + 2 + 1.6 * t)),
                fill=(255, 196, 92, 120))
    # EL PLANETA (naranja estelar — 2×2).
    px, py = round(sx - 4.4), round(sy + 3.6)
    d.ellipse((px, py, px + 1, py + 1), fill=(255, 176, 96, 255))
    # LA LUNA (azul pálido, colgada del planeta).
    d.ellipse((px - 3, py + 2, px - 2, py + 3), fill=(196, 220, 255, 220))
    # LAS CHISPAS del sol (solo dos, limpias).
    d.point((sx + 4, sy - 3), fill=(255, 215, 0, 240))
    d.point((sx - 4, sy - 3), fill=(255, 215, 0, 200))
    im.save(path)


def lente(path):
    """EL AGUJERO NEGRO con su anillo magenta y las estrellas dobladas."""
    im = Image.new("RGBA", (30, 30), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    rng = random.Random(63636)
    baston_base(d, madero=(70, 48, 88))
    cx, cy = 22, 7
    # EL VELO violeta profundo detrás.
    for k in range(14):
        a = rng.random() * math.tau
        rr = 5.0 + rng.random() * 1.8
        d.point((round(cx + math.cos(a) * rr), round(cy + math.sin(a) * rr)),
                fill=(80, 12, 110, 120))
    # EL ANILLO MAGENTA con sus bandas (la fórmula del shader).
    for k in range(48):
        a = k * math.tau / 48
        glow = math.sin(a * 20 + 2.0) * 0.5 + 0.5
        r = 4.6
        x, y = cx + math.cos(a) * r, cy + math.sin(a) * r
        if glow > 0.8:
            c = (255, 228, 250, 255)
        elif glow > 0.45:
            c = (255, 64, 208, 255)
        else:
            c = (150, 30, 180, 235)
        d.point((round(x), round(y)), fill=c)
        d.point((round(x + 1), round(y)), fill=c)
    # EL NÚCLEO NEGRO ABSOLUTO (el horizonte se come la luz).
    d.ellipse((cx - 2, cy - 2, cx + 2, cy + 2), fill=(8, 2, 12, 255))
    # LAS ESTRELLAS DOBLADAS cayendo en espiral (con su estela curvada).
    for k, (a0, u) in enumerate(((0.3, 0.25), (2.6, 0.6))):
        r = 5.2 - 3.4 * u
        a = a0 + u * 3.0
        x, y = cx + math.cos(a) * r, cy + math.sin(a) * r * 0.7
        d.point((round(x), round(y)), fill=(255, 228, 250, 255))
        d.point((round(x - 1), round(y - 1)), fill=(255, 150, 240, 180))
        d.point((round(x - 2), round(y - 2)), fill=(200, 90, 220, 140))
    im.save(path)


def sol(path):
    """EL SOL VIVO: el núcleo latiendo con sus prominencias y su anillo."""
    im = Image.new("RGBA", (30, 30), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    baston_base(d)
    cx, cy = 22, 7
    # LA CÁSCARA (el velo que respira).
    for r in range(7, 3, -1):
        f = (1 - (r - 4) / 3) * 0.5
        d.ellipse((cx - r, cy - r, cx + r, cy + r),
                  outline=(int(255 * f + 120), int(150 * f + 60), 40, 110))
    # EL NÚCLEO: blanco al centro, dorado al borde.
    d.ellipse((cx - 3, cy - 3, cx + 3, cy + 3), fill=(255, 236, 180, 255))
    d.ellipse((cx - 2, cy - 2, cx + 2, cy + 2), fill=(255, 252, 240, 255))
    # LOS LÓBULOS del núcleo girando (coreGroup).
    for a in (0.4, 2.5, 4.6):
        x = cx + math.cos(a) * 2.4
        y = cy + math.sin(a) * 2.4
        d.point((round(x), round(y)), fill=(255, 196, 96, 235))
    # LAS PROMINENCIAS (las lenguas arqueándose).
    for a, h in ((-0.5, 4), (0.9, 5), (2.2, 3), (3.8, 4), (5.2, 3)):
        bx = cx + math.cos(a) * 3.4
        by = cy + math.sin(a) * 3.4
        for st in range(h):
            t = st / max(1, h - 1)
            x = bx + math.cos(a) * t * h * 0.8
            y = by + math.sin(a) * t * h * 0.8
            c = (255, 250, 205, 255) if t < 0.4 else (255, 165, 0, 235)
            d.point((round(x), round(y)), fill=c)
    # EL ANILLO DEL PULSO.
    d.ellipse((cx - 6, cy - 6, cx + 6, cy + 6),
              outline=(255, 214, 120, 150))
    # LAS ASCUAS escapando.
    d.point((cx + 7, cy + 2), fill=(255, 200, 90, 220))
    d.point((cx - 6, cy + 4), fill=(255, 140, 40, 200))
    d.point((cx + 5, cy - 6), fill=(255, 220, 140, 220))
    im.save(path)


def sierpe(path):
    """LA SIERPE: la cabeza estelar, las aletas frías y la espina en S
    (el estilo limpio del leviatán v6.35 — vértebras de puntos)."""
    im = Image.new("RGBA", (30, 30), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    baston_base(d, madero=(60, 74, 92))
    # LA SIERPE en diagonal (la cabeza arriba-derecha, la cola abajo-izq).
    dx, dy = 0.90, 0.44
    px, py = -0.44, 0.90
    # LA COLUMNA: vértebras en S (la onda viaja a la cola y crece).
    for v in range(9):
        f = v / 8
        onda = math.sin(f * 3.4) * (1.2 + 2.4 * f)
        x = 20 - dx * (2.1 * v) + px * onda
        y = 11 - dy * (2.1 * v) + py * onda
        c = (255, 248, 225, 255) if v % 2 else (255, 214, 120, 245)
        d.point((round(x), round(y)), fill=c)
        d.point((round(x) + 1, round(y)), fill=c)
        d.point((round(x), round(y) + 1), fill=(255, 208, 110, 215))
    # LAS ALETAS (radios alternando lados, afiladas — las gemelas).
    for v, lado in ((2, 1), (5, -1)):
        f = v / 8
        onda = math.sin(f * 3.4) * (1.2 + 2.4 * f)
        x = 20 - dx * (2.1 * v) + px * onda
        y = 11 - dy * (2.1 * v) + py * onda
        largo = 4.6 - f * 1.4
        ax = x + px * lado * largo - dx * largo * 0.5
        ay = y + py * lado * largo - dy * largo * 0.5
        for st in range(4):
            sx2 = x + (ax - x) * st / 3
            sy2 = y + (ay - y) * st / 3
            d.point((round(sx2), round(sy2)), fill=(140, 210, 255, 235))
    # LA CABEZA: el cráneo con la mandíbula abierta en V y el ojo frío.
    cx2, cy2 = 20, 11
    d.ellipse((cx2 - 1, cy2 - 1, cx2 + 1, cy2 + 1), fill=(255, 248, 225, 255))
    d.point((cx2 + 2, cy2 - 1), fill=(255, 255, 255, 255))   # mandíbula sup.
    d.point((cx2 + 2, cy2 + 1), fill=(210, 232, 250, 240))   # mandíbula inf.
    d.point((cx2 + 3, cy2 - 2), fill=(255, 248, 225, 225))
    d.point((cx2 + 3, cy2 + 2), fill=(190, 225, 250, 210))
    d.point((cx2, cy2 - 1), fill=(30, 200, 255, 255))        # el ojo frío
    d.point((cx2 - 1, cy2 - 2), fill=(255, 214, 120, 240))   # la cresta
    # LAS ESTRELLAS del rastro (dos, limpias).
    d.point((cx2 + 5, cy2 + 4), fill=(255, 214, 120, 220))
    d.point((cx2 - 4, cy2 - 4), fill=(168, 224, 255, 210))
    im.save(path)


def anillo_dorsal(path):
    """EL ANILLO RÚNICO A ESCORZO: la firma del vacío para la espalda."""
    im = Image.new("RGBA", (30, 30), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    cx, cy = 15, 16
    # EL ARO PRINCIPAL: la elipse magenta a escorzo, CONTINUA y gruesa
    #     (elipse de PIL con grosor 2 — silueta limpia, sin dientes).
    d.ellipse((cx - 11, cy - 6, cx + 11, cy + 6),
              outline=(255, 92, 158, 255), width=2)
    # EL FRENTE más brillante (la profundidad de la casa: la mitad de
    # abajo del anillo pasa por delante — una segunda pasada).
    d.arc((cx - 11, cy - 6, cx + 11, cy + 6), 15, 165,
          fill=(255, 150, 200, 255), width=2)
    # EL ANILLO DE FOTONES interior (el eco magenta claro, fino).
    d.ellipse((cx - 8, cy - 4, cx + 8, cy + 4),
              outline=(255, 150, 240, 170), width=1)
    # LAS OCHO RUNAS: trazos en L ROSA PÁLIDO sobre el aro (la misma
    #     familia del anillo — 4 legibles al frente + 4 tenues atrás).
    for g in range(8):
        a = g * math.tau / 8
        x = cx + math.cos(a) * 11
        y = cy + math.sin(a) * 6
        if math.sin(a) < 0:
            # La mitad trasera: perlas tenues.
            d.point((round(x), round(y)), fill=(220, 130, 175, 210))
            continue
        # El frente: la runa en L (dos trazos de 2 px).
        rx, ry = round(x), round(y) - 1
        d.point((rx, ry), fill=(255, 228, 244, 255))
        d.point((rx, ry + 1), fill=(255, 228, 244, 255))
        d.point((rx + 1, ry + 1), fill=(255, 190, 225, 235))
    # LAS DOS PERLAS cardinales (izquierda y derecha, brillantes).
    for a in (0, math.pi):
        x = cx + math.cos(a) * 11
        y = cy + math.sin(a) * 6
        d.ellipse((x - 1, y - 1, x + 1, y + 1), fill=(255, 236, 248, 255))
    im.save(path)


def bolsa_codigos(path):
    """EL SACO DE LA CASA con runas vivas (la bolsa de los códigos)."""
    im = Image.new("RGBA", (30, 30), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    rng = random.Random(63637)
    # EL SACO: el cuerpo redondeado oscuro con el brillo del código.
    d.ellipse((6, 9, 24, 27), fill=(38, 34, 48, 255), outline=(70, 62, 88, 255))
    d.ellipse((9, 12, 21, 24), fill=(26, 23, 34, 255))
    # EL CUELLO del saco atado.
    d.rectangle((12, 5, 18, 10), fill=(52, 46, 66, 255), outline=(84, 74, 104, 255))
    d.point((13, 5), fill=(120, 255, 190, 255))
    d.point((17, 5), fill=(120, 255, 190, 255))
    # LAS RUNAS VIVAS (el código hecho escritura — el verde menta de la bolsa).
    runas = (
        (12, 15, 1), (16, 14, 2), (20, 16, 3),
        (14, 19, 2), (18, 20, 1), (11, 21, 3),
    )
    for rx, ry, forma in runas:
        if forma == 1:      # la lanza
            d.point((rx, ry - 1), fill=(160, 255, 205, 255))
            d.point((rx, ry), fill=(120, 255, 190, 235))
            d.point((rx, ry + 1), fill=(90, 220, 170, 210))
        elif forma == 2:    # la puerta
            d.point((rx - 1, ry), fill=(160, 255, 205, 245))
            d.point((rx, ry), fill=(120, 255, 190, 235))
            d.point((rx + 1, ry), fill=(90, 220, 170, 210))
        else:               # la estrella
            d.point((rx, ry), fill=(200, 255, 225, 255))
            d.point((rx + 1, ry - 1), fill=(120, 255, 190, 200))
    # LAS CHISPAS escapando del saco.
    for k in range(6):
        a = rng.random() * math.tau
        r = 10 + rng.random() * 3
        x = 15 + math.cos(a) * r
        y = 18 + math.sin(a) * r * 0.9
        d.point((round(x), round(y)), fill=(120, 255, 190, 170))
    im.save(path)


def glow_proyectil(path, rgb):
    """Sprite 76×76: glow radial suave (placeholder — la librería dibuja)."""
    im = Image.new("RGBA", (76, 76), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    cx = cy = 38
    for r in range(34, 0, -1):
        f = (1 - r / 34) ** 1.6
        d.ellipse((cx - r, cy - r, cx + r, cy + r),
                  fill=(int(rgb[0] * f), int(rgb[1] * f), int(rgb[2] * f), int(150 * f)))
    im.save(path)


danza(os.path.join(W, "DanzaOrbesStaff.png"))
lente(os.path.join(W, "LenteAbismoStaff.png"))
sol(os.path.join(W, "SolVivoStaff.png"))
sierpe(os.path.join(W, "SierpeEstelarStaff.png"))

glow_proyectil(os.path.join(P, "DanzaOrbesProjectile.png"), (255, 214, 120))
glow_proyectil(os.path.join(P, "LenteAbismoProjectile.png"), (255, 64, 208))
glow_proyectil(os.path.join(P, "SolVivoProjectile.png"), (255, 180, 80))
glow_proyectil(os.path.join(P, "SierpeEstelarProjectile.png"), (200, 240, 255))

anillo_dorsal(os.path.join(I, "AnilloRunicoDorsalItem.png"))
bolsa_codigos(os.path.join(B, "BolsaCodigosVivos.png"))

print("10 PNGs generados")
