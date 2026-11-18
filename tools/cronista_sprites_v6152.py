#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""cronista_sprites_v6152.py — LOS SPRITES DEL CRONISTA (el NPC
comerciante nuevo). Por la SFORGA de la casa (composición PIL directa,
transparencia nativa):
  · ElCronista.png — el CUERPO: 25 frames del spritesheet de aldeano
    (2000×1000 → escala de la casa 25 frames de 20×42 en tira vertical
    500×1050... tML autoload: tira de frames VERTICAL 20 ancho).
    EL DISEÑO: el escriba de la primer devoración — túnica violeta
    con capucha, el plumín, el libro de cuentas en la mano, la tinta
    corriendo del bolsillo.
  · ElCronista_Head.png — el RETRATO del chat (20×20).
"""
from PIL import Image, ImageDraw
import os

os.chdir("/home/z/my-project/mod/AethonMod/Content/NPCs")

# ============ EL CUERPO (tira vertical de 25 frames 20×42) ============
FW, FH, FRAMES = 20, 42, 25
tira = Image.new("RGBA", (FW, FH * FRAMES), (0, 0, 0, 0))

# LA PALETA DEL CRONISTA
TUNICA = (86, 52, 140)
TUNICA_OSCURA = (58, 34, 98)
TUNICA_CLARA = (122, 84, 182)
CAPUCHA = (44, 26, 74)
ROSTRO = (214, 188, 168)
ROSTRO_SOMBRA = (172, 146, 128)
TINTA = (28, 16, 44)
PLUMIN = (216, 200, 180)
PERGAMINO = (226, 210, 176)
ORO = (255, 208, 120)

def dibujar_cronista(m):
    im = Image.new("RGBA", (FW, FH), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)

    # LA TÚNICA (el cuerpo vestido: ancho en hombros, cae recto)
    for y in range(14, 40):
        w = 7 if y < 20 else 6
        col = TUNICA if (y // 3) % 2 == 0 else TUNICA_CLARA
        for x in range(10 - w, 10 + w + 1):
            im.putpixel((x, y), col + (255,))
    # el borde de la túnica (el faldón con tinta)
    d.line([(3, 39), (16, 39)], fill=TUNICA_OSCURA + (255,))
    d.line([(4, 40), (15, 40)], fill=TINTA + (255,))

    # LA CAPUCHA (la cabeza encapuchada: el escriba discreto)
    d.ellipse([6, 3, 14, 12], fill=CAPUCHA + (255,))
    d.polygon([(6, 8), (14, 8), (13, 13), (7, 13)], fill=CAPUCHA + (255,))
    # el rostro en sombra (la hendidura de la capucha)
    d.ellipse([8, 6, 12, 10], fill=ROSTRO_SOMBRA + (255,))
    im.putpixel((9, 8), TINTA + (255,))    # el ojo (la pluma lo apunta)
    im.putpixel((11, 8), TINTA + (255,))

    # LOS BRAZOS: el derecho sostiene EL LIBRO DE CUENTAS, el izq el plumín
    d.line([(3, 20), (6, 24)], fill=TUNICA_CLARA + (255,))   # hombro izq
    d.line([(16, 20), (14, 25)], fill=TUNICA_CLARA + (255,))  # hombro der
    # EL PLUMÍN (la mano izquierda — siempre escribiendo)
    d.line([(2, 24), (4, 27)], fill=PLUMIN + (255,))
    # EL LIBRO DE CUENTAS (abierto en la mano derecha)
    d.rectangle([12, 24, 16, 29], fill=PERGAMINO + (255,))
    d.line([(14, 24), (14, 29)], fill=ROSTRO_SOMBRA + (255,))
    # la cuenta escrita (un rasgo dorado — el precio)
    im.putpixel((13, 26), ORO + (255,))

    # EL CINTURÓN DE TINTA (los tinteros colgando)
    d.line([(5, 22), (14, 22)], fill=TINTA + (255,))
    im.putpixel((7, 24), TINTA + (255,))
    im.putpixel((12, 24), TINTA + (255,))

    # LA ANIMACIÓN por frame: el VAIVÉN del escriba (los frames de
    # andar/actuar de vanilla usan desplazamientos — aquí el balanceo
    # de la túnica + el plumín que sube y baja (el gesto de escribir)
    fase = m % 25
    if fase < 10:        # escribiendo (el plumín baja)
        im.putpixel((3, 27), PLUMIN + (255,))
    elif fase < 15:      # pensando (el plumín sube)
        im.putpixel((3, 23), PLUMIN + (255,))
    return im

for m in range(FRAMES):
    tira.alpha_composite(dibujar_cronista(m), (0, m * FH))
tira.save("ElCronista.png")
print(f"  ✔ ElCronista.png — {FW}×{FH*FRAMES} (25 frames de aldeano)")

# ============ LA CABEZA (el retrato del chat — 20×20) ============
cabeza = Image.new("RGBA", (20, 20), (0, 0, 0, 0))
d = ImageDraw.Draw(cabeza)
d.ellipse([3, 2, 17, 14], fill=CAPUCHA + (255,))          # la capucha
d.polygon([(4, 10), (16, 10), (14, 17), (6, 17)], fill=CAPUCHA + (255,))
d.ellipse([6, 6, 14, 12], fill=ROSTRO_SOMBRA + (255,))    # el hueco
im2 = cabeza.load()
im2[8, 9] = TINTA + (255,)   # los ojos del escriba
im2[11, 9] = TINTA + (255,)
# el plumín en la oreja (la firma)
cabeza.putpixel((16, 4), PLUMIN + (255,))
cabeza.putpixel((16, 5), PLUMIN + (255,))
cabeza.save("ElCronista_Head.png")
print("  ✔ ElCronista_Head.png — el retrato (20×20)")
