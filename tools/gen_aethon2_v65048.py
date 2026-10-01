#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_aethon2_v65048.py — LAS TEXTURAS DE LA SEGUNDA LUZ (v6.50.48).

La segunda luz es TAMBIEN una luz — pero la que aprendio de la
Emperatriz: el violeta de la primera se vuelve ROSA y su oro se vuelve
CIAN (el prisma blanco-rosa-cian de AethonSegundo). Cuatro piezas, por
REMAPA HSL (se conserva toda la sombra y la forma de las originales —
la casa estilo supermuestreo no hace falta: aqui solo se CAMBIA DE
LUZ):

  AethonBoss.png            -> AethonSegundo.png            (48x48)
  AethonBoss_Head_Boss.png   -> AethonSegundo_Head_Boss.png  (32x32)
  FormaAscendidaItem.png     -> FormaAscendidaDosItem.png    (30x30)
  NombreDeAethon.png         -> NombreDeAethonSegundo.png    (24x24)

REGLA: hue violeta (250..300) -> rosa (318); hue oro (35..70) -> cian
(197); el blanco se vuelve un pelo mas frio. Luminancia y sombra
INTACTAS (solo cambia de luz).
"""
import colorsys
from PIL import Image
import os

RAIZ = os.path.join(os.path.dirname(__file__), '..', 'AethonMod')


def remap(px):
    r, g, b, a = px
    if a == 0:
        return px
    h, l, s = colorsys.rgb_to_hls(r / 255.0, g / 255.0, b / 255.0)
    hue = h * 360.0
    if 250.0 <= hue <= 300.0:          # el violeta de la primera luz -> ROSA
        h = 318.0 / 360.0
    elif 35.0 <= hue <= 70.0:          # el oro de la primera luz -> CIAN
        h = 197.0 / 360.0
    elif l > 0.86 and s < 0.25:        # los blancos calientes -> blancos frios
        r2, g2, b2 = colorsys.hls_to_rgb(h, min(1.0, l + 0.03), s)
        return (min(255, int(r2 * 255)), min(255, int(g2 * 255)), min(255, int(b2 * 255)), a)
    else:
        return px
    r2, g2, b2 = colorsys.hls_to_rgb(h, l, s)
    return (int(r2 * 255), int(g2 * 255), int(b2 * 255), a)


def convertir(src, dst):
    im = Image.open(src).convert('RGBA')
    out = Image.new('RGBA', im.size)
    out.putdata([remap(px) for px in im.getdata()])
    out.save(dst)
    print(f'  {os.path.basename(src):28s} -> {os.path.basename(dst):30s} {im.size}')


if __name__ == '__main__':
    print('LA SEGUNDA LUZ — el rempa HSL (violeta->rosa, oro->cian):')
    convertir(os.path.join(RAIZ, 'Content/NPCs/AethonBoss.png'),
              os.path.join(RAIZ, 'Content/NPCs/AethonSegundo.png'))
    convertir(os.path.join(RAIZ, 'Content/NPCs/AethonBoss_Head_Boss.png'),
              os.path.join(RAIZ, 'Content/NPCs/AethonSegundo_Head_Boss.png'))
    convertir(os.path.join(RAIZ, 'Content/Items/Cosmetics/FormaAscendidaItem.png'),
              os.path.join(RAIZ, 'Content/Items/Cosmetics/FormaAscendidaDosItem.png'))
    convertir(os.path.join(RAIZ, 'Content/Items/Llamados/NombreDeAethon.png'),
              os.path.join(RAIZ, 'Content/Items/Llamados/NombreDeAethonSegundo.png'))
    print('OK — las cuatro texturas de la segunda luz viven.')
