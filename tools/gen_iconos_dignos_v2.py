#!/usr/bin/env python3
# v6.51.4 — REDIBUJO de los 4 iconos reprobados por el VLM:
# #2 comas (una sola curva cohesiva), #4 ojo (iris dorado GRANDE),
# #5 línea (cabeza violeta grande + libro legible), #6 marcapáginas
# (cinta diagonal sobre página clara).
from PIL import Image
import os

W = H = 30
K = (18, 10, 28, 255)
W_ = (255, 250, 232, 255)
ORO = (255, 214, 120, 255)
ORO_CLARO = (255, 236, 170, 255)
ORO_OSCURO = (196, 140, 52, 255)
VIO = (178, 108, 240, 255)
VIO_CLARO = (216, 168, 252, 255)
VIO_OSCURO = (108, 56, 176, 255)
M = (150, 96, 52, 255)
M_CLARO = (188, 130, 76, 255)
M_OSCURO = (104, 62, 34, 255)
PAPEL = (232, 218, 186, 255)
PAPEL_SOMBRA = (198, 182, 148, 255)

def nuevo():
    return Image.new('RGBA', (W, H), (0, 0, 0, 0))

def px(img, x, y, c):
    if 0 <= int(x) < W and 0 <= int(y) < H:
        img.putpixel((int(x), int(y)), c)

def disco(img, cx, cy, r, relleno, borde=K, anillo=None):
    rr = r * r; rb = (r + 1) * (r + 1)
    for y in range(int(cy - r - 2), int(cy + r + 3)):
        for x in range(int(cx - r - 2), int(cx + r + 3)):
            d2 = (x - cx) ** 2 + (y - cy) ** 2
            if d2 <= (r - 1) * (r - 1) and anillo:
                px(img, x, y, anillo)
            elif d2 <= rr:
                px(img, x, y, relleno)
            elif d2 <= rb:
                px(img, x, y, borde)

def save(img, ruta):
    img.save(ruta); print('OK', ruta)

BASE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
W_COSMIC = os.path.join(BASE, 'AethonMod', 'Content', 'Weapons', 'Cosmic')

# ======================================================================
#  #2 v2 — LAS COMAS QUE MUERDEN: UNA coma cohesiva tipo puntuación:
#  cabeza gorda arriba-derecha, cola que CURVA abajo-izquierda
#  afinándose, punta violeta. UNA comita de compañía.
# ======================================================================
img = nuevo()
# LA CABEZA (r=6, dorada, con highlight).
disco(img, 19, 10, 6, ORO_CLARO)
px(img, 17, 8, W_); px(img, 18, 8, W_); px(img, 17, 9, W_)
# LA COLA: curva desde abajo de la cabeza hacia abajo-izquierda.
# Trazo con grosor decreciente 3 → 2 → 1, SIEMPRE contorneado.
puntos = []
for i in range(11):
    f = i / 10.0
    # curva bezier cuadrática: (19,16) → (12,18) → (9,25)
    x = (1-f)**2 * 19 + 2*(1-f)*f * 12 + f**2 * 9
    y = (1-f)**2 * 16 + 2*(1-f)*f * 18 + f**2 * 25
    g = 3 - int(f * 2.2)   # 3 → 1
    puntos.append((x, y, g))
for (x, y, g) in puntos:
    for dy in range(g):
        px(img, x, y + dy, ORO)
# EL CONTORNO de la cola (K a ambos lados del trazo).
for (x, y, g) in puntos:
    px(img, x - 1, y + g - 1, K)
    px(img, x + 1 if g > 1 else x, y - 1, K)
# LA PUNTA VIOLETA (la tinta del final de la coma).
px(img, 9, 26, VIO); px(img, 10, 26, VIO); px(img, 9, 27, VIO); px(img, 10, 27, VIO_CLARO)
# LA COMITA DE COMPAÑÍA (suspensa, arriba-izquierda).
disco(img, 6, 6, 3, ORO_CLARO)
px(img, 5, 5, W_)
px(img, 4, 9, ORO); px(img, 4, 10, ORO); px(img, 4, 11, VIO)
save(img, os.path.join(W_COSMIC, 'DignosComasQueMuerden.png'))

# ======================================================================
#  #4 v2 — EL OJO QUE CAZA: almendra GRANDE (28 de ancho), iris dorado
#  RODEADO de anillo violeta, deslizado a la derecha, pupila K.
# ======================================================================
img = nuevo()
# LA ALMENDRA (ocupa TODO el lienzo: 28×16, centrada en y=15).
anchos = {0: 6, 1: 9, 2: 11, 3: 13, 4: 14, 5: 14, 6: 14, 7: 14, 8: 14,
          9: 13, 10: 11, 11: 9, 12: 6, 13: 4}
for fila, a in anchos.items():
    y = 8 + fila
    x0 = 15 - a; x1 = 15 + a
    for x in range(x0, x1 + 1):
        if fila in (0, 13) or x == x0 or x == x1:
            px(img, x, y, K)
        else:
            px(img, x, y, W_)
# contorno superior e inferior reforzado.
for x in range(15 - 6, 15 + 7):
    px(img, x, 7, K)
for x in range(15 - 6, 15 + 7):
    px(img, x, 22, K)
# EL IRIS DORADO (r=5) DESLIZADO a la derecha, con anillo violeta.
disco(img, 19, 15, 5, ORO, anillo=VIO_OSCURO)
px(img, 19, 15, K); px(img, 20, 15, K); px(img, 19, 16, K)  # la pupila
px(img, 17, 13, W_)                                            # el brillo
px(img, 18, 13, W_)
# EL PÁRPADO (la línea gruesa arriba = el ojo entreabierto cazando).
for x in range(7, 24):
    px(img, x, 8, K)
# LAS PESTAÑAS (dos rayitas).
px(img, 9, 6, K); px(img, 9, 7, K)
px(img, 20, 6, K); px(img, 20, 7, K)
# EL DESTELLO DE LA MIRA (a donde va la mirada).
px(img, 26, 12, W_); px(img, 27, 11, W_)
save(img, os.path.join(W_COSMIC, 'DignosOjoQueCaza.png'))

# ======================================================================
#  #5 v2 — LA LÍNEA DE LECTURA: fila de glifos con CABEZA VIOLETA
#  grande y el LIBRO ABIERTO legible (dos páginas claras con renglones).
# ======================================================================
img = nuevo()
# LA LÍNEA tenue (el renglón del libro).
for x in range(4, 23):
    if x % 2 == 0:
        px(img, x, 11, ORO_OSCURO)
# LOS CUATRO GLIFOS dorados (cuadraditos 2×2 con su sombra).
for gx in (4, 8, 12, 16):
    px(img, gx, 10, ORO_CLARO); px(img, gx + 1, 10, ORO_CLARO)
    px(img, gx, 11, ORO); px(img, gx + 1, 11, ORO)
    px(img, gx + 1, 9, ORO_OSCURO)
# LA CABEZA VIOLETA (el punto final de la frase, GRANDE: r=3).
disco(img, 22, 11, 3, VIO)
px(img, 22, 11, VIO_CLARO); px(img, 21, 10, W_)
# EL LIBRO ABIERTO (dos páginas de PAPEL con tapas M, el lomo K).
for x in range(5, 26):
    if x == 15: continue      # el lomo
    px(img, x, 17, PAPEL)
    px(img, x, 18, PAPEL)
    px(img, x, 19, PAPEL_SOMBRA)
# LAS TAPAS del libro (M con contorno K).
for x in range(5, 26):
    px(img, x, 20, M if x < 15 else M_CLARO)
    px(img, x, 21, M_OSCURO if x < 15 else M)
px(img, 5, 16, K); px(img, 25, 16, K)
for x in range(5, 26):
    px(img, x, 16, K if x in (5, 25) else (PAPEL if x != 15 else K))
    px(img, x, 22, K)
px(img, 5, 17, K); px(img, 5, 18, K); px(img, 5, 19, K)
px(img, 25, 17, K); px(img, 25, 18, K); px(img, 25, 19, K)
px(img, 15, 16, K); px(img, 15, 17, K); px(img, 15, 18, K); px(img, 15, 19, K); px(img, 15, 20, K); px(img, 15, 21, K)
# LOS RENGLONES sobre las páginas.
for y in (17, 18):
    for x in range(7, 14):
        if (x + y) % 3 != 0:
            px(img, x, y, PAPEL_SOMBRA)
    for x in range(17, 24):
        if (x + y) % 3 != 0:
            px(img, x, y, PAPEL_SOMBRA)
# LA MARCA DE LECTURA (el glifo VIOLETA sobre la página izquierda).
px(img, 10, 17, VIO); px(img, 11, 17, VIO)
save(img, os.path.join(W_COSMIC, 'DignosLineaDeLectura.png'))

# ======================================================================
#  #6 v2 — EL MARCAPÁGINAS: la página clara de FONDO (con renglones)
#  y la CINTA diagonal encima (ancho 4, cola de milano, punta violeta).
# ======================================================================
img = nuevo()
# LA PÁGINA (papel crema, inclinada leve, con esquina doblada).
for y in range(3, 28):
    dx = (y - 3) // 6       # el corrimiento de la inclinación
    for x in range(6 + dx, 24 + dx):
        borde = (x == 6 + dx or x == 23 + dx or y == 3 or y == 27)
        if borde:
            px(img, x, y, K)
        else:
            px(img, x, y, PAPEL if x < 20 + dx else PAPEL_SOMBRA)
# LOS RENGLONES (el texto de la página).
for y in range(6, 25, 3):
    dx = (y - 3) // 6
    for x in range(9 + dx, 20 + dx):
        if (x + y) % 4 != 0:
            px(img, x, y, M_OSCURO)
# LA ESQUINA DOBLADA (abajo-derecha).
dx28 = (28 - 3) // 6
for i in range(4):
    for j in range(4 - i):
        px(img, 23 + dx28 - 1 - i, 27 - j, PAPEL_SOMBRA)
        px(img, 23 + dx28 - 1 - i, 24 + j if False else 27 - j, PAPEL_SOMBRA)
# LA CINTA DIAGONAL (de arriba-izquierda a abajo-derecha, ancho 4).
for i in range(26):
    f = i / 25.0
    x = 8 + f * 13
    y = 2 + f * 22
    # el cuerpo de la cinta (4 px de ancho, perpendicular a la diagonal).
    for o in range(4):
        px(img, x + o, y, ORO_CLARO if o == 1 else ORO)
    # contornos.
    px(img, x - 1, y, K)
    px(img, x + 4, y, K)
# EL BRILLO satinado.
for i in range(8):
    f = i / 25.0
    px(img, 9 + f * 13, 4 + f * 22, W_)
# LA COLA DE MILANO VIOLETA (la punta en V de la cinta).
x0, y0 = 20, 24
for i in range(3):
    for j in range(i + 1):
        px(img, x0 + i, y0 - 2 + j, VIO if j < 2 else VIO_CLARO)
px(img, x0 - 1, y0 - 2, K); px(img, x0 - 1, y0 - 1, K)
px(img, x0 + 3, y0 - 2, K); px(img, x0 + 3, y0 - 1, K)
px(img, x0 + 3, y0, VIO_OSCURO)
save(img, os.path.join(W_COSMIC, 'DignosMarcapaginas.png'))
print('REDIBUJO LISTO — 4 iconos v2.')
