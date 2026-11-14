#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_dummynegro_v6136.py — EL MUÑECO DE PRUEBAS NEGRO DE LA CASA (v6.136).

LA LETRA: «Genera las texturas con SForja» + el veredicto del QA VLM tras
3 rondas pluma/refina (5,5 → 7 → 5,5: la pluma inyecta la paleta del libro
y la cruceta marrón pelea contra ella — la regla de la casa: a la tercera
se cambia de enfoque). ESTE es el enfoque clásico de la casa: el generador
PIL puro (la técnica de las recetas `proyectil`/`sol` de la SForja), donde
cada píxel es de la casa — nacido NEGRO-VIOLETA de verdad.

EL SPRITE (40×56, UN solo marco):
  · LA CABEZA de arpillera GRIS-VIOLETA oscuro (el saco del grimorio) con
    OJOS VIOLETAS que brillan (los ojos-minion de la casa) y la boca COSIDA
    (puntadas en X).
  · LA CRUZETA de brazos: TRAVESAÑO de madera NEGRA-violeta (viga oscura
    con veta), con MANOJOS DE PAJA violeta-gris en las puntas (briznas
    sueltas de 1 px).
  · EL CUERPO: palo vertical DELGADO con el PETATE de arpillera estrecho
    atado en el pecho (2-3 px de cuerda).
  · LA BASE: la punta del palo clavada.
  · EL TINTE DE LA CASA: TODO el sprite vive en la rampa negro-violeta
    (26,14,44 → 150,84,235 de VFXPalettes) — es EL MUÑECO NEGRO DEL
    GRIMORIO, no el dummy vanilla recoloreado.

SALIDAS:
  · AethonMod/Content/NPCs/DummyNegroNPC.png   (el NPC — 40×56)
  · (el ítem lo toma prestado con el override Texture de la casa)
  · montaje_qa.png para EL OJO.
"""
import os
from PIL import Image

W, H = 40, 56
DEST = '/home/z/my-project/mod/AethonMod/Content/NPCs/DummyNegroNPC.png'
CARPETA = '/tmp/superforja_dummynegro'

# ===== LA RAMPA NEGRO-VIOLETA DE LA CASA (VFXPalettes) =====
SACO_1 = (44, 34, 62)      # arpillera oscura (sombra del saco)
SACO_2 = (62, 48, 86)      # arpillera media
SACO_3 = (84, 66, 114)     # arpillera clara (la luz del saco)
PAJA_1 = (96, 74, 138)     # paja violeta oscura
PAJA_2 = (128, 98, 180)    # paja violeta media
PAJA_3 = (168, 132, 220)   # paja violeta clara
PAJA_4 = (205, 175, 245)   # las puntas brillantes (v3 del QA)
MAD_1 = (36, 26, 52)       # madera negra-violeta (sombra de la viga)
MAD_2 = (52, 40, 76)       # madera media (la viga)
MAD_3 = (74, 58, 104)      # madera clara (el borde iluminado)
OJO = (196, 150, 255)      # el violeta brillante de los ojos-minion
OJO_N = (26, 14, 44)       # la noche del vacío (el fondo del ojo)
HILO = (150, 84, 235)      # el hilo violeta del grimorio (costuras)
CUERDA = (110, 80, 160)    # la cuerda del petate

im = Image.new('RGBA', (W, H), (0, 0, 0, 0))
px = im.load()


def rect(x0, y0, x1, y1, c):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            px[x, y] = c + (255,)


def hline(x0, x1, y, c):
    rect(x0, y, x1, y, c)


def vline(x, y0, y1, c):
    rect(x, y0, x, y1, c)


def paja(x, y, dx, largo, arriba=True):
    """Un manojo de paja: bloque + briznas sueltas de 1 px."""
    for i in range(largo):
        # briznas que se abren en abanico
        for off in (-1, 0, 1):
            yy = y - i if arriba else y + i
            xx = x + off * (i // 3)
            if 0 <= xx < W and 0 <= yy < H:
                c = PAJA_3 if i == largo - 1 or off == 0 and i % 2 == 0 else PAJA_2
                px[xx, yy] = c + (255,)


# ============ LA CABEZA (arpillera cosida) — v3: 10 px de ancho ============
# (el QA pedía -30-40 %: de 14 px a 10 px — la cabeza ya no aplasta)
rect(15, 3, 24, 16, SACO_2)
rect(16, 2, 23, 2, SACO_2)
vline(15, 4, 15, SACO_1)           # sombra izq
vline(24, 4, 15, SACO_1)           # sombra der
hline(16, 23, 17, SACO_1)          # la base del saco
rect(16, 3, 23, 3, SACO_3)         # la luz superior
# la coronilla con puntadas (la costura superior del saco)
hline(17, 22, 5, SACO_1)
for x in (17, 20, 22):
    px[x, 4] = HILO + (255,)       # las puntadas
# el parche cosido (esquina inferior derecha)
rect(20, 12, 23, 15, SACO_3)
vline(20, 12, 15, HILO); vline(23, 12, 15, HILO)
hline(20, 12, 23, HILO); hline(20, 15, 23, HILO)
# LOS OJOS VIOLETAS QUE BRILLAN (los ojos-minion de la casa)
rect(17, 8, 18, 10, OJO_N)         # la noche
rect(21, 8, 22, 10, OJO_N)
px[17, 9] = OJO + (255,)           # el brillo
px[21, 9] = OJO + (255,)
# la boca COSIDA (las X)
px[19, 13] = HILO + (255,); px[20, 13] = HILO + (255,)
px[19, 14] = HILO + (255,); px[20, 14] = HILO + (255,)
px[18, 13] = SACO_1 + (255,); px[21, 14] = SACO_1 + (255,)
# la paja del cuello (briznas pegadas al saco — nada flotante)
for x in (16, 20, 23):
    px[x, 18] = PAJA_2 + (255,)
    px[x, 19] = PAJA_1 + (255,)

# ============ LA CRUZETA (el travesaño, y 21-26) ============
rect(6, 22, 33, 25, MAD_2)         # la viga
hline(6, 33, 22, MAD_3)            # el canto superior iluminado
hline(6, 33, 25, MAD_1)            # la sombra inferior
for x in range(8, 33, 5):          # la veta de la viga
    px[x, 23] = MAD_1 + (255,)
    px[x, 24] = MAD_3 + (255,)
# (v3 del QA: la sombra que asienta la cruzeta sobre el palo)
hline(18, 21, 26, MAD_1)

# LAS MANOS: manojos de paja GRANDES (v3 del QA: 1,5-2×, contorno
# irregular con dientes de 1 px y las PUNTAS BRILLANTES PAJA_4 —
# dentro de la rampa violeta: el muñeco es NEGRO por diseño de la casa)
def manojo(cx, cy):
    """El mechón grande: rombo radio 4 con dientes + briznas largas."""
    for dy in range(-4, 5):
        for dx in range(-4, 5):
            d = abs(dx) + abs(dy)
            xx, yy = cx + dx, cy + dy
            if not (0 <= xx < W and 0 <= yy < H):
                continue
            if d <= 2:
                px[xx, yy] = PAJA_3 + (255,)       # el corazón claro
            elif d <= 3:
                px[xx, yy] = PAJA_2 + (255,)
            elif d == 4 and (dx + dy) % 2 == 0:    # los dientes irregulares
                px[xx, yy] = PAJA_1 + (255,)
    # las briznas largas que se abren (con la punta BRILLANTE)
    for dx, dy, c in ((-5, -2, PAJA_4), (-5, 1, PAJA_3), (-4, -3, PAJA_3),
                      (-6, 0, PAJA_3), (-4, 3, PAJA_2), (5, -2, PAJA_4),
                      (5, 1, PAJA_3), (4, -3, PAJA_3), (6, 0, PAJA_3),
                      (4, 3, PAJA_2)):
        xx, yy = cx + dx, cy + dy
        if 0 <= xx < W and 0 <= yy < H:
            px[xx, yy] = c + (255,)


manojo(5, 23)                       # la mano izquierda
manojo(34, 23)                      # la mano derecha

# ============ EL CUERPO (el palo + el petate, y 20-53) ============
# EL PALO vertical (delgado — 4 px)
rect(18, 20, 21, 53, MAD_2)
vline(18, 20, 53, MAD_3)           # el canto iluminado
vline(21, 20, 53, MAD_1)           # la sombra
# EL PETATE (la arpillera del pecho — v3: 1 px más estrecho por lado)
rect(16, 27, 23, 38, SACO_2)
vline(16, 27, 38, SACO_1)
vline(23, 27, 38, SACO_1)
hline(17, 22, 27, SACO_3)          # la luz del petate
hline(16, 23, 38, SACO_1)
# la cuerda del petate (2 vueltas)
hline(16, 23, 31, CUERDA)
hline(16, 23, 32, CUERDA)
px[15, 31] = CUERDA + (255,)       # el nudo
# la paja que asoma del petate
px[17, 39] = PAJA_3 + (255,); px[22, 39] = PAJA_2 + (255,)
px[19, 39] = PAJA_3 + (255,)

# ============ LA BASE (la punta clavada, y 54-55) ============
rect(17, 54, 22, 55, MAD_1)
px[19, 55] = MAD_1 + (255,); px[20, 55] = MAD_1 + (255,)

os.makedirs(CARPETA, exist_ok=True)
im.save(DEST)
im.save(os.path.join(CARPETA, 'sprite.png'))
print(f'✔ {DEST} ({W}x{H})')

# ===== EL MONTAJE PARA EL OJO =====
esc = 8
big = im.resize((W * esc, H * esc), Image.NEAREST)
tab = Image.new('RGB', (big.width + 40, big.height + 40), (128, 128, 128))
tab.paste(big, (20, 20), big)
tab.save(os.path.join(CARPETA, 'montaje_qa.png'))
print(f'✔ montaje: {CARPETA}/montaje_qa.png')

# ===== EL HIGHLIGHT (la silueta para el contorno Terraria) =====
hl = Image.new('RGBA', (W, H), (0, 0, 0, 0))
hpx = hl.load()
for y in range(H):
    for x in range(W):
        if px[x, y][3] > 0:
            borde = any(0 <= x + dx < W and 0 <= y + dy < H and px[x + dx, y + dy][3] == 0
                        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
            if borde:
                hpx[x, y] = (255, 255, 255, 255)
hl.save(os.path.join(CARPETA, 'highlight.png'))
print('✔ highlight.png')

# estadística de paleta
from collections import Counter
c = Counter(p[:3] for p in im.getdata() if p[3] > 0)
print('opacos:', sum(c.values()), '· colores:', len(c))
print('rampa casa:', c.most_common(5))
