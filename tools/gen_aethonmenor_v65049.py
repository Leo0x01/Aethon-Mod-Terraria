#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_aethonmenor_v65049.py — LAS TEXTURAS DE LA TERCERA LUZ (v6.50.49).

Tres piezas:
  AethonBoss.png         -> FormaAscendidaTresItem.png (30x30, PRISMATICO:
                             remapa HSL radial — el matiz gira con el
                             angulo alrededor del nucleo: EL TRONO, el
                             arcoiris alrededor del sol)
  AethonBoss.png         -> AethonMenorItem.png  (24x24, el jefe EN
                             MINIATURA — literal: «Aethon original pero
                             mas pequeño»)
  AethonBoss.png         -> AethonMenorBuff.png  (32x32, la chispa del
                             buff — el nucleo con su arcoiris sutil)
"""
import colorsys
import math
from PIL import Image
import os

RAIZ = os.path.join(os.path.dirname(__file__), '..', 'AethonMod')
SRC = os.path.join(RAIZ, 'Content/NPCs/AethonBoss.png')


def prisma(px, x, y, w, h):
    """EL REMAPA PRISMATICO: el matiz gira con el ANGULO alrededor del
    centro (el arcoiris alrededor del trono) — la forma y la sombra del
    original INTACTAS (solo cambia de luz)."""
    r, g, b, a = px
    if a == 0:
        return px
    h_, l, s = colorsys.rgb_to_hls(r / 255.0, g / 255.0, b / 255.0)
    if s < 0.06 or l > 0.97:
        return px  # los blancos del nucleo quedan BLANCOS
    # el matiz del arcoiris por el angulo (mas un cuarto de vuelta para
    # que el oro quede arriba) — la saturacion a tope y +10% de luz.
    ang = math.atan2(y - h * 0.5, x - w * 0.5)
    hue = (ang / (2 * math.pi) + 0.25) % 1.0
    r2, g2, b2 = colorsys.hls_to_rgb(hue, min(1.0, l * 1.08 + 0.04), min(1.0, s * 1.25))
    return (min(255, int(r2 * 255)), min(255, int(g2 * 255)), min(255, int(b2 * 255)), a)


def generar_trono():
    im = Image.open(SRC).convert('RGBA')
    w, h = im.size
    out = Image.new('RGBA', (w, h))
    out.putdata([prisma(px, i % w, i // w, w, h) for i, px in enumerate(im.getdata())])
    out = out.resize((30, 30), Image.LANCZOS)
    out.save(os.path.join(RAIZ, 'Content/Items/Cosmetics/FormaAscendidaTresItem.png'))
    print('  FormaAscendidaTresItem.png 30x30 (el prisma del trono)')


def generar_menor():
    # EL AETHON MENOR: el jefe EN MINIATURA, literal (mas un pelo mas
    # brillante — es una chispa viva).
    im = Image.open(SRC).convert('RGBA')
    out = Image.new('RGBA', im.size)
    datos = []
    for r, g, b, a in im.getdata():
        if a == 0:
            datos.append((r, g, b, 0))
            continue
        datos.append((min(255, int(r * 1.06 + 6)), min(255, int(g * 1.06 + 6)),
                      min(255, int(b * 1.06 + 6)), a))
    out.putdata(datos)
    out = out.resize((24, 24), Image.LANCZOS)
    out.save(os.path.join(RAIZ, 'Content/Items/Llamados/AethonMenorItem.png'))
    print('  AethonMenorItem.png 24x24 (el jefe en miniatura)')

    # EL BUFF: 32x32 (el estandar de los buffs de la casa) — el nucleo
    # con el arcoiris sutil alrededor.
    im2 = Image.open(SRC).convert('RGBA')
    w, h = im2.size
    bf = Image.new('RGBA', (w, h))
    bf.putdata([prisma(px, i % w, i // w, w, h) for i, px in enumerate(im2.getdata())])
    bf = bf.resize((32, 32), Image.LANCZOS)
    bf.save(os.path.join(RAIZ, 'Content/Buffs/AethonMenorBuff.png'))
    print('  AethonMenorBuff.png 32x32 (la chispa del arcoiris)')


if __name__ == '__main__':
    print('LA TERCERA LUZ — las texturas del trono y del menor (v6.50.49):')
    generar_trono()
    generar_menor()
    print('OK — las tres texturas viven.')
