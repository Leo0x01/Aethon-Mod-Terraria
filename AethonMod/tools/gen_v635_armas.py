#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_v635_armas.py — LOS ICONOS DE LOS CUATRO DESGARROS NUEVOS (v6.35).

Pixel-art 30×30 estilo el mod (bastón diagonal + cabeza temática):
  · CorazonColapsoStaff      — cabeza: la estrella de 4 puntas naranja/carmesí
  · GargantaVacioStaff       — cabeza: el vórtice magenta con núcleo negro
  · UmbralRotoStaff          — cabeza: el corte horizontal con el vacío arriba
  · LeviatanEspectralStaff   — cabeza: el cráneo del leviatán (mandíbula en V)

Y los sprites 76×76 de los proyectiles (glow suave del color).
"""
import math
import os
import random

from PIL import Image, ImageDraw

BASE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
W = os.path.join(BASE, "Content", "Weapons", "Cosmic")
P = os.path.join(BASE, "Content", "Projectiles", "Cosmic")


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


def corazon(path):
    im = Image.new("RGBA", (30, 30), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    baston_base(d)
    cx, cy = 22, 6
    # LA ESTRELLA DE 4 PUNTAS: cuatro filamentos curvos (espiral logarítmica).
    for a in range(4):
        base_ang = a * math.tau / 4 + 0.35
        prev = None
        for s in range(13):
            f = s / 12
            r = 1.6 + 5.2 * f
            ang = base_ang + 0.85 * f * f
            x = cx + math.cos(ang) * r
            y = cy + math.sin(ang) * r
            if prev is not None:
                # El gradiente: blanco → naranja → carmesí hacia la punta.
                if f < 0.4:
                    c = (255, 250, 205, 255)
                elif f < 0.75:
                    c = (255, 165, 0, 255)
                else:
                    c = (220, 20, 60, 255)
                steps = 2
                for st in range(steps + 1):
                    px = round(prev[0] + (x - prev[0]) * st / steps)
                    py = round(prev[1] + (y - prev[1]) * st / steps)
                    d.point((px, py), fill=c)
            prev = (x, y)
    # EL NÚCLEO blanco cálido.
    for dx, dy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1)):
        d.point((cx + dx, cy + dy), fill=(255, 250, 205, 255))
    # LAS CHISPAS de oro.
    d.point((cx + 7, cy - 4), fill=(255, 215, 0, 240))
    d.point((cx - 6, cy + 5), fill=(255, 215, 0, 240))
    im.save(path)


def garganta(path):
    im = Image.new("RGBA", (30, 30), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    rng = random.Random(60606)
    baston_base(d)
    cx, cy = 22, 6
    # EL VELO violeta profundo detrás.
    for k in range(16):
        a = rng.random() * math.tau
        rr = 5.5 + rng.random() * 1.5
        d.point((round(cx + math.cos(a) * rr), round(cy + math.sin(a) * rr)),
                fill=(90, 11, 122, 130))
    # EL ANILLO MAGENTA con sus bandas (la fórmula sin(20a − t)).
    for k in range(60):
        a = k * math.tau / 60
        glow = math.sin(a * 20 + 2.0) * 0.5 + 0.5
        r = 5.0
        x, y = cx + math.cos(a) * r, cy + math.sin(a) * r
        if glow > 0.8:
            c = (255, 228, 250, 255)
        elif glow > 0.45:
            c = (255, 64, 208, 255)
        else:
            c = (150, 30, 180, 235)
        d.point((round(x), round(y)), fill=c)
        d.point((round(x + 1), round(y)), fill=c)
    # LOS TRES BRAZOS espirales.
    for a in range(3):
        prev = None
        for s in range(7):
            f = s / 6
            r = 5.0 + 2.8 * f
            ang = a * math.tau / 3 + 1.25 * f - 0.4
            x, y = cx + math.cos(ang) * r, cy + math.sin(ang) * r
            if prev is not None:
                d.point((round(x), round(y)), fill=(200, 40, 190, 200))
            prev = (x, y)
    # EL NÚCLEO NEGRO ABSOLUTO.
    for dx in (-1, 0, 1):
        for dy in (-1, 0, 1):
            if abs(dx) + abs(dy) < 2:
                d.point((cx + dx, cy + dy), fill=(5, 2, 8, 245))
    # UNA chispa blanca cayendo.
    d.point((cx + 6, cy + 2), fill=(224, 255, 255, 240))
    im.save(path)


def umbral(path):
    im = Image.new("RGBA", (30, 30), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    rng = random.Random(10101)
    # EL BASTÓN vertical bajo (la cabeza es ancha y horizontal).
    for i in range(16):
        y = 26 - i
        c = (94, 62, 38) if i % 5 < 3 else (118, 80, 48)
        d.point((15, y), fill=c)
        d.point((16, y), fill=(66, 44, 28))
    for dx, dy in ((14, 11), (15, 10), (16, 11), (15, 12)):
        d.point((dx, dy), fill=(255, 214, 106))

    # EL VACÍO SUPERIOR (la banda oscura de la otra realidad).
    for x in range(3, 27):
        for y in range(2, 10):
            decae = (y - 2) / 8
            if rng.random() < 0.12 + decae * 0.25:
                continue
            alfa = int(215 - decae * 90)
            d.point((x, y), fill=(0, 0, 12, alfa))

    # EL ESQUELETO ESPECTRAL (las vértebras blancas en S dentro del vacío).
    for v in range(6):
        x = 6 + v * 3.4
        y = 6.5 + math.sin(v * 1.05) * 1.8
        c = (224, 224, 224, 235) if v % 2 else (200, 200, 200, 220)
        d.point((round(x), round(y)), fill=c)
        if v < 5:
            d.point((round(x + 1.7), round(y + math.sin((v + 0.5) * 1.05) * 1.8)), fill=(180, 180, 180, 160))
    # LA CABEZA con mandíbula.
    d.point((5, 6), fill=(240, 240, 240, 255))
    d.point((4, 7), fill=(224, 224, 224, 235))
    d.point((4, 5), fill=(224, 224, 224, 235))

    # LA LÍNEA DEL CORTE (blanca con aberración cian arriba / magenta abajo).
    for x in range(2, 28):
        d.point((x, 10), fill=(0, 255, 255, 120))      # eco cian
        d.point((x, 11), fill=(255, 255, 255, 255))    # el filo
        d.point((x, 12), fill=(255, 0, 249, 120))      # eco magenta
    # LOS EXTREMOS brillantes.
    d.point((2, 11), fill=(255, 255, 255, 255))
    d.point((27, 11), fill=(255, 255, 255, 255))
    # LAS PARTÍCULAS DE DATOS cian.
    d.point((8, 13), fill=(0, 255, 255, 230))
    d.point((21, 14), fill=(0, 255, 255, 230))
    d.point((14, 8), fill=(0, 255, 255, 200))
    im.save(path)


def leviatan(path):
    im = Image.new("RGBA", (30, 30), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    baston_base(d, madero=(60, 74, 92))
    # EL LEVIATÁN nadando en diagonal (la cabeza arriba-derecha).
    cx, cy = 21, 7
    dx, dy = 0.94, 0.34   # el rumbo
    px, py = -0.34, 0.94  # el perpendicular
    # LA COLUMNA de vértebras en S (la onda viaja a la cola).
    for v in range(9):
        f = v / 8
        amp = 1.2 + 2.2 * f
        onda = math.sin(f * 3.4) * amp
        x = cx - dx * (2.0 * v) + px * onda
        y = cy - dy * (2.0 * v) + py * onda
        w = 2.6 - 1.4 * f
        c = (232, 244, 255, 255) if v % 2 else (200, 226, 250, 235)
        d.point((round(x), round(y)), fill=c)
        if v < 8:
            d.point((round(x - dx), round(y - dy)), fill=(159, 216, 255, 180))
    # LAS ALETAS (radios alternando lados, afiladas).
    for v, lado in ((2, 1), (4, -1), (6, 1)):
        f = v / 8
        amp = 1.2 + 2.2 * f
        onda = math.sin(f * 3.4) * amp
        x = cx - dx * (2.0 * v) + px * onda
        y = cy - dy * (2.0 * v) + py * onda
        largo = 4.5 - f * 1.5
        ax = x - dx * largo * 0.7 + px * lado * largo
        ay = y - dy * largo * 0.7 + py * lado * largo
        for st in range(4):
            sx = x + (ax - x) * st / 3
            sy = y + (ay - y) * st / 3
            d.point((round(sx), round(sy)), fill=(159, 216, 255, 190))
    # LA CABEZA: el cráneo con la mandíbula abierta en V.
    d.point((cx, cy), fill=(232, 244, 255, 255))
    d.point((cx + 1, cy), fill=(232, 244, 255, 255))
    d.point((cx + 2, cy - 1), fill=(255, 255, 255, 255))   # la mandíbula sup.
    d.point((cx + 2, cy + 1), fill=(232, 244, 255, 235))   # la mandíbula inf.
    d.point((cx + 3, cy - 2), fill=(232, 244, 255, 220))
    d.point((cx + 3, cy + 2), fill=(210, 232, 250, 210))
    # LA CUENCA del ojo.
    d.point((cx, cy - 1), fill=(255, 255, 255, 255))
    # LA CRESTA.
    d.point((cx - 1, cy - 2), fill=(159, 216, 255, 230))
    d.point((cx - 2, cy - 3), fill=(159, 216, 255, 210))
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


corazon(os.path.join(W, "CorazonColapsoStaff.png"))
garganta(os.path.join(W, "GargantaVacioStaff.png"))
umbral(os.path.join(W, "UmbralRotoStaff.png"))
leviatan(os.path.join(W, "LeviatanEspectralStaff.png"))

glow_proyectil(os.path.join(P, "CorazonColapsoProjectile.png"), (255, 140, 40))
glow_proyectil(os.path.join(P, "GargantaVacioProjectile.png"), (255, 60, 200))
glow_proyectil(os.path.join(P, "UmbralRotoProjectile.png"), (0, 255, 255))
glow_proyectil(os.path.join(P, "LeviatanEspectralProjectile.png"), (180, 230, 255))

print("8 PNGs generados")
