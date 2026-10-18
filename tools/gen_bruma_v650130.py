#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_bruma_v650130.py — LOS DOS SPRITES DE LA BRUMA (v6.50.130).

(1) BrumaDelCrater.png — EL ICONO DEL ITEM (30×30): la bruma EN PERSONA
    — un mechón de niebla violeta con corazón claro y goteo (legible en
    el inventario a cualquier tamaño: la silueta de nube + el fulgor).
(2) BrumaAltarTile.png — LA TILE DE BRUMA (16×16, CASI invisible): un
    suspiro violeta tenue (el espectáculo lo pone LA ZONA del
    AltarDevoradorSistema: columnas, resplandor y vórtice — la tile solo
    marca el dónde).
"""
from PIL import Image, ImageDraw, ImageFilter
import os

BASE = os.path.join(os.path.dirname(__file__), '..', 'AethonMod', 'Content')

# ---------- (1) EL ICONO DEL ITEM: 30×30 ----------

W = H = 30
img = Image.new('RGBA', (W, H), (0, 0, 0, 0))
d = ImageDraw.Draw(img)

def puff(dr, cx, cy, r, color):
    """Un mechón de niebla: círculo difuminado a mano (halo + cuerpo)."""
    # el halo (la orilla que se desvanece)
    for i in range(4, 0, -1):
        rr = r + i * 1.6
        a = int(color[3] * 0.16 * (5 - i) / 4)
        dr.ellipse([cx - rr, cy - rr * 0.82, cx + rr, cy + rr * 0.82],
                   fill=(color[0], color[1], color[2], a))
    # el cuerpo
    dr.ellipse([cx - r, cy - r * 0.82, cx + r, cy + r * 0.82], fill=color)
    # la cima esponjada (dos lóbulos: la nube respira)
    dr.ellipse([cx - r * 0.75, cy - r * 1.05, cx + r * 0.15, cy - r * 0.25],
               fill=color)

# LA NUBE: tres mechones — oscuro atrás, medio al centro, claro delante.
puff(d, 15, 18, 9, (58, 36, 88, 235))        # el fondo oscuro
puff(d, 10, 16, 6, (86, 54, 128, 235))       # el costado izquierdo
puff(d, 20, 15, 6, (104, 66, 152, 240))      # el costado derecho

# EL CORAZÓN CLARO (la bruma alberga algo: el fulgor del altar).
d.ellipse([11, 13, 19, 19], fill=(178, 132, 226, 255))
d.ellipse([13, 14, 17, 18], fill=(232, 208, 255, 255))

# EL GOTEO (dos gotitas que caen de la nube: la bruma escurre).
d.ellipse([8, 22, 10, 25], fill=(96, 60, 140, 220))
d.ellipse([21, 21, 23, 24], fill=(96, 60, 140, 200))

# LA FIRMA OSCURA (el contorno interior que lee el icono en claro):
d.ellipse([13, 15, 18, 20], outline=(44, 24, 70, 255), width=1)

img = img.filter(ImageFilter.SMOOTH_MORE)
img.save(os.path.join(BASE, 'Items', 'Altar', 'BrumaDelCrater.png'))
print('  BrumaDelCrater.png 30x30 OK')

# ---------- (2) LA TILE DE BRUMA: 16×16 (casi invisible) ----------

T = 16
tile = Image.new('RGBA', (T, T), (0, 0, 0, 0))
td = ImageDraw.Draw(tile)

# UN SUSPIRO: un mechón tenue descentrado (la tile no estorba — la zona
# dibuja el minibioma entero alrededor).
td.ellipse([3, 6, 13, 13], fill=(70, 44, 104, 110))
td.ellipse([5, 4, 11, 10], fill=(94, 58, 138, 130))
td.ellipse([6, 7, 10, 11], fill=(150, 100, 205, 120))

# DOS MOTAS (el goteo del suspiro).
td.point((5, 14), fill=(110, 70, 160, 120))
td.point((11, 13), fill=(110, 70, 160, 100))

tile.save(os.path.join(BASE, 'Tiles', 'BrumaAltarTile.png'))
print('  BrumaAltarTile.png 16x16 OK')
print('LISTO')
