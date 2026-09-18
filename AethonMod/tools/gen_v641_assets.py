#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_v641_assets.py — LOS ASSETS DE LOS HUÉSPEDES (v6.41).

Pixel-art 30×30 estilo la casa:
  1. TomoApatiaNula.png          — EL TOMO: libro verde-negro con el glifo
                                   del vacío (espiral invertida light-green)
                                   y el broche apático.
  2. FragmentoSupernovaStaff.png — EL FRAGMENTO: el bastón de la casa con
                                   el OJO ALADO encima (cuerpo crema-oro,
                                   alas naranjas con puntas rojas, pupila
                                   oscura — la firma de la singularidad).
  3. BolsaHuespedes.png          — EL SACO de la casa, firma VERDE HUÉSPED
                                   + el ojo alado bordado al frente.
  4. PolvoVacioInvertido.png     — 4×4 transparente (el polvo dibuja SUS
                                   3 capas a mano en PreDraw — el sprite
                                   declarado jamás se pinta).

Determinista: cero azar del sistema.
"""
import math
import os

from PIL import Image, ImageDraw

BASE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
W = os.path.join(BASE, "Content", "Weapons", "Cosmic")
I = os.path.join(BASE, "Content", "Items", "Bolsas")
D = os.path.join(BASE, "Content", "Dusts")


def lerp(c1, c2, t):
    return tuple(int(c1[i] + (c2[i] - c1[i]) * t) for i in range(3)) + (255,)


def baston_base(draw, madero=(94, 62, 38)):
    """El bastón diagonal clásico (abajo-izq → arriba-der) + engarce."""
    for i in range(17):
        x, y = 6 + i, 23 - i
        c = madero if i % 5 < 3 else (118, 80, 48)
        draw.point((x, y), fill=c)
        draw.point((x + 1, y), fill=(66, 44, 28))
    for dx, dy in ((21, 7), (22, 6), (22, 8), (23, 7)):
        draw.point((dx, dy), fill=(255, 214, 106))


# ----------------------------------------------------------------------
# 1. EL TOMO DE LA APATÍA NULA — el libro del vacío verde.
# ----------------------------------------------------------------------
def tomo(path):
    im = Image.new("RGBA", (30, 30), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)

    # EL CUERPO DEL LIBRO (tapa verde-oscuro casi negro, lomo a la izq).
    d.rectangle((6, 7, 24, 26), fill=(16, 34, 22, 255), outline=(52, 96, 64, 255))
    d.rectangle((8, 9, 23, 24), fill=(22, 44, 28, 255))
    # EL LOMO con las costuras.
    d.rectangle((6, 7, 7, 26), fill=(10, 22, 14, 255))
    for y in (10, 14, 18, 22):
        d.point((6, y), fill=(84, 150, 96, 255))
    # LAS PÁGINAS asomando al filo derecho.
    d.rectangle((24, 8, 25, 25), fill=(214, 226, 198, 255))
    d.point((24, 8), fill=(180, 196, 166, 255))

    # EL GLIFO DEL VACÍO: espiral invertida light-green (el sello del tomo).
    cx, cy = 15, 16
    for k in range(26):
        a = 2.6 + k * 0.34
        r = 1.0 + k * 0.20
        x, y = cx + math.cos(a) * r, cy + math.sin(a) * r
        t = k / 25
        col = lerp((128, 255, 128), (200, 255, 200), t) if k % 7 < 4 else (60, 140, 70)
        rx, ry = int(round(x)), int(round(y))
        if 9 <= rx <= 22 and 10 <= ry <= 23:
            d.point((rx, ry), fill=col)

    # EL OJO INTERIOR: la nada negra al centro de la espiral.
    d.ellipse((13, 14, 17, 18), fill=(4, 8, 5, 255))
    d.point((15, 16), fill=(0, 0, 0, 255))

    # EL BROCHE APÁTICO (la hebilla fría que cierra la nada).
    d.point((9, 16), fill=(255, 214, 106))
    d.point((21, 16), fill=(255, 214, 106))
    d.rectangle((12, 26, 18, 27), fill=(90, 160, 100, 255))

    im.save(path)


# ----------------------------------------------------------------------
# 2. EL FRAGMENTO DE SUPERNOVA — el bastón con el ojo alado.
#     (lección v6.38: cabezas GRUESAS y planas leen mejor a 30×30)
# ----------------------------------------------------------------------
def fragmento(path):
    im = Image.new("RGBA", (30, 30), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)

    # EL BASTÓN GRUESO (abajo-izq), cabeza GRANDE arriba.
    for i in range(10):
        x, y = 5 + i, 26 - i
        c = (118, 80, 48) if i % 4 < 3 else (142, 100, 60)
        d.point((x, y), fill=c)
        d.point((x + 1, y), fill=(94, 62, 38))
        d.point((x, y + 1), fill=(94, 62, 38))
        d.point((x + 1, y + 1), fill=(66, 44, 28))
    for dx, dy in ((14, 17), (15, 16), (15, 18), (16, 17)):
        d.point((dx, dy), fill=(255, 224, 130))

    # EL OJO ALADO GRANDE — el cuerpo crema-oro horizontal 12×4.
    cx, cy = 20, 8
    for k in range(12):
        x = cx - 6 + k
        t = abs(k - 5.5) / 5.5
        col = lerp((255, 244, 210), (238, 196, 112), t)
        d.point((x, cy - 2), fill=col)
        d.point((x, cy - 1), fill=col)
        d.point((x, cy), fill=lerp((255, 220, 150), (206, 152, 70), t))
        if 3 <= k <= 8:
            d.point((x, cy + 1), fill=(214, 170, 90))

    # LA PUPILA oscura — el vacío del ojo (3 px GRUESOS).
    for dx in (-1, 0, 1):
        d.point((cx + dx, cy - 1), fill=(12, 12, 48, 255))
        d.point((cx + dx, cy), fill=(22, 22, 80, 255))

    # LAS ALAS naranjas GRUESAS inclinadas con puntas rojas + contorno.
    for lado in (-1, 1):
        for k in range(5):
            t = k / 4
            x = cx + lado * (6 + 3 * t)
            y = cy - 1 + 3 * t
            col = lerp((240, 120, 72), (168, 48, 48), t * 0.85)
            rx = int(round(x))
            ry = int(round(y))
            d.point((rx, ry), fill=col)
            d.point((rx, ry + 1), fill=lerp((208, 96, 58), (140, 38, 38), t))
            d.point((rx, ry - 1), fill=lerp((255, 160, 100), (200, 70, 70), t))

    # EL HALO dorado — los dos picos de la singularidad.
    d.point((cx - 3, cy - 4), fill=(255, 214, 106, 200))
    d.point((cx + 3, cy - 4), fill=(255, 214, 106, 200))
    d.point((cx, cy - 5), fill=(255, 214, 106, 200))

    im.save(path)


# ----------------------------------------------------------------------
# 3. LA BOLSA DE LOS HUÉSPEDES — el saco con el ojo bordado.
# ----------------------------------------------------------------------
def bolsa(path):
    im = Image.new("RGBA", (30, 30), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    VERDE = (150, 255, 170)

    # EL SACO: el cuerpo redondeado — MÁS CLARO con doble contorno.
    d.ellipse((6, 9, 24, 27), fill=(44, 62, 50, 255), outline=(120, 200, 140, 255))
    d.ellipse((9, 12, 21, 24), fill=(30, 44, 36, 255))
    # EL CUELLO del saco atado.
    d.rectangle((12, 5, 18, 10), fill=(52, 72, 60, 255), outline=(140, 220, 160, 255))
    d.point((13, 5), fill=VERDE)
    d.point((17, 5), fill=VERDE)

    # EL OJO ALADO bordado al frente — GRUESO y con CONTRASTE.
    cx, cy = 15, 17
    # el cuerpo crema: barra horizontal 7×2 con contorno dorado
    for k in range(7):
        d.point((cx - 3 + k, cy), fill=lerp((255, 244, 210), (240, 200, 120), abs(k - 3) / 3))
        d.point((cx - 3 + k, cy - 1), fill=(255, 250, 230))
        d.point((cx - 3 + k, cy + 1), fill=(200, 160, 80))
    # la pupila
    d.point((cx, cy - 1), fill=(8, 8, 30, 255))
    d.point((cx, cy), fill=(8, 8, 30, 255))
    # las alas: trazos naranjas gruesos a los lados
    for lado in (-1, 1):
        for k in range(3):
            t = k / 2
            x = cx + lado * (4 + 2 * t)
            y = cy - 1 + 1 + k
            d.point((int(round(x)), y), fill=lerp((240, 120, 72), (168, 48, 48), t))
            d.point((int(round(x)) - lado, y), fill=(255, 170, 110))
            d.point((int(round(x)), y + 1), fill=(150, 50, 30))

    im.save(path)


# ----------------------------------------------------------------------
# 4. EL POLVO DEL VACÍO — 4×4 transparente (nunca se dibuja).
# ----------------------------------------------------------------------
def polvo(path):
    im = Image.new("RGBA", (4, 4), (0, 0, 0, 0))
    im.save(path)


# ----------------------------------------------------------------------
# 5. EL ICONO DEL BUFF — el ojo alado en 32×32.
# ----------------------------------------------------------------------
def buff_icono(path):
    im = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)

    # EL GLOW de fondo (la singularidad respirando).
    d.ellipse((6, 10, 26, 22), fill=(255, 170, 70, 60))
    d.ellipse((10, 12, 22, 20), fill=(255, 190, 90, 90))

    # EL OJO ALADO — cuerpo crema-oro horizontal 12×4.
    cx, cy = 16, 16
    for k in range(12):
        x = cx - 6 + k
        t = abs(k - 5.5) / 5.5
        col = lerp((255, 244, 210), (238, 196, 112), t)
        d.point((x, cy - 2), fill=col)
        d.point((x, cy - 1), fill=col)
        d.point((x, cy), fill=lerp((255, 220, 150), (206, 152, 70), t))
        if 3 <= k <= 8:
            d.point((x, cy + 1), fill=(214, 170, 90))

    # LA PUPILA oscura.
    for dx in (-1, 0, 1):
        d.point((cx + dx, cy - 1), fill=(12, 12, 48, 255))
        d.point((cx + dx, cy), fill=(22, 22, 80, 255))

    # LAS ALAS naranjas con puntas rojas.
    for lado in (-1, 1):
        for k in range(5):
            t = k / 4
            x = cx + lado * (6 + 4 * t)
            y = cy - 1 + 3 * t
            col = lerp((240, 120, 72), (168, 48, 48), t * 0.85)
            rx, ry = int(round(x)), int(round(y))
            d.point((rx, ry), fill=col)
            d.point((rx, ry + 1), fill=lerp((208, 96, 58), (140, 38, 38), t))
            d.point((rx, ry - 1), fill=lerp((255, 160, 100), (200, 70, 70), t))

    # EL HALO: los picos dorados.
    d.point((cx - 3, cy - 4), fill=(255, 224, 130, 220))
    d.point((cx + 3, cy - 4), fill=(255, 224, 130, 220))
    d.point((cx, cy - 5), fill=(255, 224, 130, 220))

    im.save(path)


# ----------------------------------------------------------------------
if __name__ == "__main__":
    os.makedirs(D, exist_ok=True)
    tomo(os.path.join(W, "TomoApatiaNula.png"))
    fragmento(os.path.join(W, "FragmentoSupernovaStaff.png"))
    bolsa(os.path.join(I, "BolsaHuespedes.png"))
    polvo(os.path.join(D, "PolvoVacioInvertido.png"))
    buff_icono(os.path.join(BASE, "Content", "Buffs", "FragmentoSupernovaBuff.png"))
    print("v6.41: 5 assets generados")
