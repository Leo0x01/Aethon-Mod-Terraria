#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_aurora_tinta_v6521.py — LA AURORA DE TINTA (icono 30×30 de la casa).

Las alas creadas por código: la pluma de luz prismática — un ala de
cinco plumas estilizadas en abanico, la tinta violeta dispersándose
hacia cian y oro, un núcleo blanco-caliente en el eje y la gota de
tinta debajo. Léxico de la casa: formas grandes legibles, bordes
oscuros cohesivos, brillos limpios.
"""
from PIL import Image, ImageDraw
import math

W = H = 30
img = Image.new('RGBA', (W, H), (0, 0, 0, 0))
d = ImageDraw.Draw(img)

def prisma(h):
    """El color prisma de la casa (hue 0..1 → RGB)."""
    h = h % 1.0
    if h < 1/6:   r, g, b = 1, h*6, 0
    elif h < 2/6: r, g, b = 2-h*6, 1, 0
    elif h < 3/6: r, g, b = 0, 1, h*6-2
    elif h < 4/6: r, g, b = 0, 4-h*6, 1
    elif h < 5/6: r, g, b = h*6-4, 0, 1
    else:         r, g, b = 1, 0, 6-h*6
    return (int(r*255), int(g*255), int(b*255))

# --- EL FONDO DELATOR: la pluma necesita silueta oscura cohesiva ---
# (la lección de la casa: el negro puro alfa es invisible de noche —
# aquí es el icono del inventario: fondo profundo violeta-azulado)
d.ellipse([3, 2, 27, 26], fill=(24, 14, 44, 255))
d.ellipse([4, 3, 26, 25], fill=(34, 20, 60, 255))

# --- EL ABANICO: cinco plumas de luz (la geometría del original) ---
# Cada pluma: una aguja gruesa desde el ancla (abajo-centro) hacia
# arriba/afuera, con su hue del prisma y núcleo blanco.
ancla = (15.5, 22)
for i in range(5):
    t = i / 4.0
    # el abanico abierto: casi horizontal amplio (el off 0.22→1.54)
    ang = -90 + (-72 + 140 * t)  # grados: de -162 a -18
    largo = 17 - 6 * t
    rad = math.radians(ang)
    punta = (ancla[0] + math.cos(rad) * largo, ancla[1] + math.sin(rad) * largo)
    hue = 0.62 + i * 0.085  # violeta → cian → oro del espectro
    c = prisma(hue)
    c2 = prisma(hue + 0.16)  # la dispersión a lo largo
    # el cuerpo de la pluma (gruesa, dos tramos con los dos tonos)
    mid = (ancla[0] + (punta[0]-ancla[0])*0.45, ancla[1] + (punta[1]-ancla[1])*0.45)
    perp = (-(punta[1]-ancla[1]), (punta[0]-ancla[0]))
    pl = math.hypot(*perp) or 1
    perp = (perp[0]/pl, perp[1]/pl)
    wroot, wmid, wtip = 1.2, 2.0, 1.6
    # polígono pluma: raíz±wroot, medio±wmid, punta±wtip
    poly = [
        (ancla[0]+perp[0]*wroot, ancla[1]+perp[1]*wroot),
        (mid[0]+perp[0]*wmid, mid[1]+perp[1]*wmid),
        (punta[0]+perp[0]*wtip, punta[1]+perp[1]*wtip),
        (punta[0]-perp[0]*wtip, punta[1]-perp[1]*wtip),
        (mid[0]-perp[0]*wmid, mid[1]-perp[1]*wmid),
        (ancla[0]-perp[0]*wroot, ancla[1]-perp[1]*wroot),
    ]
    d.polygon(poly, fill=(c[0], c[1], c[2], 235))
    # el núcleo blanco-caliente (el eje)
    d.line([ancla, mid, punta], fill=(255, 250, 240, 255), width=1)
    # la punta dispersada (el tono que corre hacia la punta)
    d.ellipse([punta[0]-1.3, punta[1]-1.3, punta[0]+1.3, punta[1]+1.3], fill=(c2[0], c2[1], c2[2], 255))

# --- EL ANCLA: la gota de tinta que sostiene el ala ---
d.ellipse([13, 21, 18, 26], fill=(18, 8, 34, 255))
d.ellipse([14, 22, 17, 25], fill=(150, 90, 230, 255))
d.ellipse([14.6, 22.4, 15.8, 23.6], fill=(240, 232, 255, 255))

# --- LA TINTA QUE GOTEA: una mota prismática bajo el ala ---
d.ellipse([21, 24, 23.4, 26.4], fill=(*prisma(0.55), 255))
d.ellipse([7, 24, 8.8, 25.8], fill=(*prisma(0.95), 255))

# --- LA VELADURA FINAL: un velo suave de tinta alrededor ---
veil = Image.new('RGBA', (W, H), (0, 0, 0, 0))
dv = ImageDraw.Draw(veil)
dv.ellipse([5, 4, 25, 24], fill=(96, 60, 170, 38))
img = Image.alpha_composite(img, veil)

img.save('/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Content/Items/Escriba/AuroraDeTinta.png')
print('icono AuroraDeTinta.png 30x30 listo')
