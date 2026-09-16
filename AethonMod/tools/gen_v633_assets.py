#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_v633_assets.py — LOS ICONOS DE LOS CUATRO DESGARROS NUEVOS (v6.33).

Pixel-art 30×30 estilo el mod (bastón diagonal + cabeza temática):
  · SuturaCuanticaStaff      — cabeza: círculo glitch cian/violeta fragmentado
  · PortalDimensionalStaff   — cabeza: anillos concéntricos cian→magenta
  · PliegueEspacioStaff      — cabeza: el ojo (doble elipse, negro + rim cian + ámbar)
  · HeridaElectricaStaff     — cabeza: la grieta eléctrica en zig-zag

Y los sprites 76×76 de los proyectiles (glow suave del color, como los
demás desgarros del mod — el dibujo real lo hace la librería).
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


def sutura(path):
    im = Image.new("RGBA", (30, 30), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    baston_base(d)
    rng = random.Random(3332231)
    cx, cy, r = 22, 6, 6
    # El círculo fragmentado (arcos que no cierran — glitch).
    for k in range(26):
        a = k * math.tau / 26
        rr = r * (0.72 + 0.42 * rng.random())
        x, y = cx + math.cos(a) * rr, cy + math.sin(a) * rr
        if rng.random() < 0.28:
            continue  # los huecos del glitch
        c = (0, 229, 255, 255) if k % 3 else (124, 77, 255, 255)
        d.point((round(x), round(y)), fill=c)
    # Las astillas hacia afuera.
    for k in range(7):
        a = rng.random() * math.tau
        rr = r + 2.4
        x, y = cx + math.cos(a) * rr, cy + math.sin(a) * rr
        d.point((round(x), round(y)), fill=(124, 77, 255, 255))
    # El núcleo magenta.
    for dx in (-1, 0, 1):
        for dy in (-1, 0, 1):
            if abs(dx) + abs(dy) < 2:
                d.point((cx + dx, cy + dy), fill=(213, 0, 249, 255))
    d.point((cx, cy), fill=(255, 130, 255, 255))
    im.save(path)


def portal(path):
    im = Image.new("RGBA", (30, 30), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    baston_base(d)
    cx, cy = 22, 6
    # Los anillos concéntricos (cian fuera, magenta dentro).
    paletas = [(0, 176, 255, 255), (90, 160, 255, 255), (170, 80, 255, 255), (213, 0, 249, 255)]
    for i, r in enumerate((6.4, 4.7, 3.2, 1.8)):
        c = paletas[i]
        for k in range(int(r * 9)):
            a = k * math.tau / int(r * 9)
            x, y = cx + math.cos(a) * r, cy + math.sin(a) * r
            if (k + i) % 7 == 3:
                continue  # los glifos ausentes
            d.point((round(x), round(y)), fill=c)
    # El núcleo blanco.
    d.point((cx, cy), fill=(255, 255, 255, 255))
    d.point((cx + 1, cy), fill=(224, 247, 250, 255))
    d.point((cx, cy + 1), fill=(224, 247, 250, 255))
    im.save(path)


def pliegue(path):
    im = Image.new("RGBA", (30, 30), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    baston_base(d)
    # EL OJO: dos elipses diagonales solapadas.
    tilt = -0.5
    cx, cy = 22, 6
    for ex, ey, s in ((-2.6, 1.5, 1.0), (2.6, -1.5, 1.0)):
        for k in range(30):
            a = k * math.tau / 30
            rx, ry = 4.6 * s, 2.9 * s
            x = cx + ex + math.cos(a) * rx * math.cos(tilt) - math.sin(a) * ry * math.sin(tilt)
            y = cy + ey + math.cos(a) * rx * math.sin(tilt) + math.sin(a) * ry * math.cos(tilt)
            d.point((round(x), round(y)), fill=(0, 212, 255, 255))
    # El negro del ojo (pase oscuro).
    for dx in range(-3, 4):
        for dy in range(-2, 3):
            if dx * dx / 5.5 + dy * dy / 2.2 <= 1.0:
                d.point((cx + dx, cy + dy), fill=(10, 10, 18, 235))
    # El rim ámbar del horizonte.
    for k in range(14):
        a = k * math.tau / 14
        d.point((round(cx + math.cos(a) * 2.9), round(cy + math.sin(a) * 2.0)),
                fill=(255, 184, 0, 255))
    # El cuello brillante.
    d.point((cx, cy), fill=(255, 250, 205, 255))
    im.save(path)


def herida(path):
    im = Image.new("RGBA", (30, 30), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    baston_base(d)
    rng = random.Random(2909907)
    # LA GRIETA en zig-zag diagonal (el rayo de la herida).
    x, y = 8, 24
    pts = [(x, y)]
    while x < 27 and y > 3:
        x += rng.randint(2, 4)
        y -= rng.randint(1, 4)
        pts.append((x, y))
    for i, (px, py) in enumerate(pts):
        c = (224, 255, 255, 255) if i % 2 else (0, 255, 255, 255)
        d.point((px, py), fill=c)
        if i + 1 < len(pts):
            nx, ny = pts[i + 1]
            steps = max(abs(nx - px), abs(ny - py))
            for s in range(1, steps):
                d.point((round(px + (nx - px) * s / steps), round(py + (ny - py) * s / steps)),
                        fill=(0, 255, 255, 200))
    # Los bordes violetas.
    for (px, py) in pts[1:-1:2]:
        d.point((px + 1, py), fill=(138, 43, 226, 255))
        d.point((px, py + 1), fill=(138, 43, 226, 255))
    # Las chispas en los extremos.
    for (px, py) in (pts[0], pts[-1]):
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            if rng.random() < 0.8:
                d.point((px + dx, py + dy), fill=(255, 255, 255, 230))
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


sutura(os.path.join(W, "SuturaCuanticaStaff.png"))
portal(os.path.join(W, "PortalDimensionalStaff.png"))
pliegue(os.path.join(W, "PliegueEspacioStaff.png"))
herida(os.path.join(W, "HeridaElectricaStaff.png"))

glow_proyectil(os.path.join(P, "DesgarroCuanticoProjectile.png"), (124, 77, 255))
glow_proyectil(os.path.join(P, "PortalDimensionalProjectile.png"), (0, 176, 255))
glow_proyectil(os.path.join(P, "PliegueEspacioProjectile.png"), (0, 212, 255))
glow_proyectil(os.path.join(P, "HeridaElectricaProjectile.png"), (0, 255, 255))

print("8 PNGs generados")
