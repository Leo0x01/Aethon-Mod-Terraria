#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_texturas_v650100.py — LAS TRES TEXTURAS QUE FALTABAN (v6.50.100).

La lección del client.log del usuario (v6.50.99): el mod NO CARGÓ — tML
lo desactivó entero («Disabling Mod: AethonMod») porque TRES clases
nuevas no tenían su .png (MissingResourceException × 3 → el -build NO
valida texturas y el servidor headless TAMPOCO las carga: sólo el
CLIENTE las pide). Las cuatro armas recibieron su icono… pero la BOLSA
que las entrega y los DOS PROYECTILES se quedaron sin sprite:

  1. BOLSA DE LA EMPERATRIZ (30×30) — la silueta de la casa (la misma
     bolsa de BolsaArmasRayo, como la de Sombras) recolorida al tema de
     la Emperatriz de la Luz: cuerpo perla-rosa, lomo dorado, el nudo
     de oro y en la ventana central EL TRAZO ARCOÍRIS (la diagramp
     prismatic del streak).
  2. ESTELA DE AETHON (30×30) — el proyectil de la opción A: cometa
     vertical con núcleo BLANCO CALIENTE, cabeza ORO y la cola que
     recorre la paleta cósmica (oro→cian→magenta→índigo).
  3. TRAZO DE LA EMPERATRIZ (30×30) — el proyectil de la opción C: la
     banda ARCOÍRIS gruesa vertical (rojo→violeta) con núcleo blanco —
     la firma del streak de la jefa.

Los dos proyectiles se dibujan 100% por código (PreDraw → false), pero
tML EXIGE el asset para cargar — éste es su sprite.
"""
from PIL import Image
import numpy as np
import math

BASE = 'AethonMod/'
MOCK = 'tools/tools/'

# ============================================================
#  1 — LA BOLSA DE LA EMPERATRIZ (recolor de la silueta de la casa)
# ============================================================
SRC = BASE + 'Content/Items/Bolsas/BolsaArmasRayo.png'
DST = BASE + 'Content/Items/Bolsas/BolsaEmperatriz.png'

# La paleta de la Emperatriz: perla, rosa y oro
BORDE_ABS = (10, 6, 16)         # el filo: sombra que no deja pasar luz
PERLA_1 = (255, 234, 244)       # cuerpo, lado claro del dither
PERLA_2 = (240, 202, 224)       # cuerpo, lado oscuro del dither
ROSA = (255, 110, 180)          # el rosa de la Emperatriz (ColorFiesta)
ROSA_OSC = (214, 70, 140)
ORO = (255, 217, 61)            # el lomo y el nudo
ORO_OSC = (188, 138, 22)
BLANCO = (252, 250, 255)

base = np.array(Image.open(SRC).convert('RGBA')).astype(int)
H, W = base.shape[:2]
alpha = base[:, :, 3]

es_borde = np.zeros((H, W), dtype=bool)
es_ventana = np.zeros((H, W), dtype=bool)
for y in range(H):
    for x in range(W):
        a = alpha[y, x]
        if a <= 40 or a >= 200:
            continue
        toca_fuera = any(0 <= y + dy < H and 0 <= x + dx < W and alpha[y + dy, x + dx] < 10
                         for dy in (-2, -1, 0, 1, 2) for dx in (-2, -1, 0, 1, 2))
        if toca_fuera:
            es_borde[y, x] = True
        else:
            es_ventana[y, x] = True


def silueta(y, x):
    return 0 <= y < H and 0 <= x < W and alpha[y, x] > 40


img = np.zeros((H, W, 4), dtype=int)

# --- el cuerpo perla con dither + el lomo dorado ---
for y in range(H):
    for x in range(W):
        if not silueta(y, x):
            continue
        if es_borde[y, x]:
            img[y, x] = (*BORDE_ABS, 255)
            continue
        if es_ventana[y, x]:
            img[y, x] = (*BORDE_ABS, 110)     # la boca de la ventana (penumbra)
            continue
        d = ((x + y * 2) % 5) < 2
        c = PERLA_1 if d else PERLA_2
        if y <= 8:                              # EL LOMO DORADO de la Emperatriz
            c = ORO if ((x * 3 + y) % 7) < 4 else ORO_OSC
        img[y, x] = (*c, 255)

# --- el nudo del cordón: ORO CON VOLUMEN (la lección VLM: una línea
# blanca no es un nudo — 6×5 con cuerpo y destello central) ---
for (y, x) in [(1, 13), (1, 14), (1, 15), (1, 16), (1, 17),
               (2, 12), (2, 13), (2, 14), (2, 15), (2, 16), (2, 17), (2, 18),
               (3, 12), (3, 13), (3, 14), (3, 15), (3, 16), (3, 17), (3, 18),
               (4, 13), (4, 14), (4, 15), (4, 16), (4, 17),
               (5, 14), (5, 15), (5, 16)]:
    img[y, x] = (*ORO, 255)
img[2, 15] = (*ORO_OSC, 255)
img[3, 14] = (*ORO_OSC, 255)
img[3, 16] = (*ORO_OSC, 255)
img[2, 14] = (*BLANCO, 255)
img[3, 15] = (*BLANCO, 255)   # el destello del nudo

# --- LA VENTANA PRISMÁTICA COMPLETA (la lección VLM v2: la ventana NO
# es una boca oscura — es TODO el glow del arcoíris): cada píxel de la
# ventana lleva su color del eje d=x+y (violeta arriba-izquierda → rojo
# abajo-derecha), la BANDA diagonal central va OPACA y la VENA BLANCA
# de 2px la recorre — el trazo de la Emperatriz dentro de la bolsa ---
ARCOIRIS = [(232, 44, 74), (255, 140, 30), (255, 228, 60), (60, 224, 120),
            (0, 234, 234), (70, 130, 255), (150, 90, 255)]

# el rango visible del blob, medido sobre SUS píxeles
dtodos = [x + y for y in range(H) for x in range(W)
          if es_ventana[y, x]]
dmin, dmax = min(dtodos), max(dtodos)

for y in range(H):
    for x in range(W):
        if not es_ventana[y, x]:
            continue
        t = 1.0 - (x + y - dmin) / max(1, dmax - dmin)
        idx = int(max(0.0, min(1.0, t)) * (len(ARCOIRIS) - 1))
        diag = (x - y) + 3                     # distancia a la diagonal central
        if abs(diag) <= 1:
            img[y, x] = (*ARCOIRIS[idx], 255)  # la banda opaca
        else:
            img[y, x] = (*ARCOIRIS[idx], 200)  # el glow prismático de todo el cristal

# la VENA BLANCA de 2px sobre la diagonal (el núcleo del trazo)
for (y, x) in [(14, 12), (15, 12), (15, 13), (16, 13), (16, 14), (17, 14),
               (17, 15), (18, 15), (18, 16), (19, 16), (19, 17), (20, 17)]:
    if 0 <= y < H and 0 <= x < W and es_ventana[y, x]:
        img[y, x] = (*BLANCO, 255)

# --- las dos chispas de la Emperatriz (arriba, a los lados del nudo —
# BRILLANTES: cruz 3px blanca + centro rosa) ---
for (cx, cy) in [(8, 9), (21, 9)]:
    for (dy, dx, c) in [(0, 0, BLANCO), (0, -1, BLANCO), (0, 1, BLANCO),
                        (-1, 0, BLANCO), (1, 0, BLANCO)]:
        if 0 <= cy + dy < H and 0 <= cx + dx < W:
            img[cy + dy, cx + dx] = (*c, 255)
    img[cy, cx] = (*ROSA, 255)

out = Image.fromarray(img.astype(np.uint8))
out.save(DST)
print(f"OK -> {DST} {out.size}")

# ============================================================
#  2 — LA ESTELA DE AETHON (el proyectil A: cometa cósmico) — v2
#     (la lección VLM: cabeza LÁGRIMA que apunta arriba — ancha abajo,
#     pico de 3px arriba —, ALITAS visibles y cola más estrecha)
# ============================================================
S = 30
img2 = Image.new('RGBA', (S, S), (0, 0, 0, 0))
P = img2.load()

ORO = (255, 217, 61)
CIAN = (0, 234, 234)
MAGENTA = (240, 60, 150)
INDIGO = (130, 100, 240)
COSMICA = [ORO, CIAN, MAGENTA, INDIGO]
BLANCO = (252, 250, 255)

# LA CABEZA — EL DIAMANTE (la síntesis de las lecciones VLM v4+v6:
# rombo dorado — punta arriba, punta abajo, ancho al medio — con el
# PICO blanco de 1px en la cima)
for y in range(2, 14):
    # el ancho del rombo por fila: 1px en las puntas → 7px al centro (y=7-8)
    half = int(round(7.0 * (1 - abs(y - 7.5) / 5.5)))
    if half <= 0:
        continue
    for x in range(15 - half, 15 + half + 1):
        P[x, y] = (*ORO, 255 if abs(x - 15) < half else 150)
P[15, 1] = (*BLANCO, 255)
P[15, 2] = (*BLANCO, 255)

# el NÚCLEO BLANCO caliente (el rombo blanco interior + borde cálido
# + EL RESPLANDOR — la lección VLM v7: píxeles amarillo-brillante
# alrededor del núcleo)
def lerp_c(c1, c2, t):
    return tuple(int(round(a + (b - a) * t)) for a, b in zip(c1, c2))

for y in range(4, 12):
    half = int(round(2.8 * (1 - abs(y - 7.5) / 3.5)))
    if half <= 0:
        continue
    for x in range(15 - half, 15 + half + 1):
        if abs(x - 15) < half:
            P[x, y] = (*BLANCO, 255)
        else:
            P[x, y] = (255, 240, 180, 255)     # el borde cálido del núcleo
# EL RESPLANDOR del núcleo: 6 píxeles amarillo-brillante alrededor
GLINT = (255, 244, 190)
for (x, y) in [(12, 6), (18, 6), (11, 8), (19, 8), (13, 10), (17, 10)]:
    P[x, y] = (*GLINT, 255)
for (x, y) in [(10, 7), (20, 7)]:
    P[x, y] = (*GLINT, 180)   # los dos mas lejanos, más tenues

# LOS DESTELLOS del diamante (4 chispas blancas en las esquinas)
for (x, y) in [(11, 4), (19, 4), (10, 8), (20, 8)]:
    P[x, y] = (*BLANCO, 230)

# LAS ALITAS (la lección VLM v7: INCLINADAS HACIA ARRIBA — el vuelo
# ascendente del cometa; 3×3 con el pico blanco apuntando arriba)
for (x, y, c) in [(6, 5, ORO), (6, 6, ORO), (7, 5, ORO), (7, 6, ORO), (7, 7, ORO), (8, 6, CIAN), (8, 7, ORO),
                  (24, 5, ORO), (24, 6, ORO), (23, 5, ORO), (23, 6, ORO), (23, 7, ORO), (22, 6, CIAN), (22, 7, ORO)]:
    P[x, y] = (*c, 250)
P[6, 4] = (*BLANCO, 255)
P[24, 4] = (*BLANCO, 255)   # el destello de la punta del ala

# LA COLA — LARGA Y NÍTIDA (la lección VLM v6: bandas DEFINIDAS de
# 4-5px por color con SOLO 1px de mezcla en las costuras — pixel-art,
# no difuminado; el alfa NUNCA se lava: 255 el cuerpo, 180 el filo)
for y in range(12, 30):
    f = (y - 12) / 18.0                        # 0 arriba → 1 abajo
    # la banda: índigo (12-16) → magenta (17-21) → cian (22-26) → oro (27-29)
    if y <= 16:
        c_base, c_sig, t = INDIGO, MAGENTA, max(0.0, (y - 15) / 2.0)
    elif y <= 21:
        c_base, c_sig, t = MAGENTA, CIAN, max(0.0, (y - 20) / 2.0)
    elif y <= 26:
        c_base, c_sig, t = CIAN, ORO, max(0.0, (y - 25) / 2.0)
    else:
        c_base, c_sig, t = ORO, ORO, 0.0
    color = lerp_c(c_base, c_sig, min(1.0, t)) if t > 0 else c_base
    half = max(1, int(round(3.4 - 2.4 * f)))   # 3px junto al diamante → 1px la punta
    for x in range(15 - half, 15 + half + 1):
        borde = abs(x - 15) >= half
        if borde:
            # EL DITHER del filo (la lección VLM v7): los píxeles del
            # borde alternan entre el color de la banda y el SIGUIENTE
            # — gas que se deshace, no un bloque
            if (x + y) % 2 == 0:
                P[x, y] = (*color, 170)
            else:
                P[x, y] = (*c_sig, 150)
        else:
            P[x, y] = (*color, 255)
    # las motas laterales (1 por lado cada 4 filas — el gas suelto)
    if y % 4 == 1 and y < 27:
        P[15 - half - 1, y + 1] = (*color, 130)
        P[15 + half + 1, y] = (*color, 130)
# LAS DOS CHISPAS BLANCAS del núcleo (la lección VLM v6: calor intenso)
P[14, 6] = (*BLANCO, 255)
P[16, 9] = (*BLANCO, 255)

img2.save(BASE + 'Content/Projectiles/Emperatriz/EstelaAethonProjectile.png')
print(f"OK -> {BASE}Content/Projectiles/Emperatriz/EstelaAethonProjectile.png {img2.size}")

# ============================================================
#  3 — EL TRAZO DE LA EMPERATRIZ (el proyectil C) — v2
#     (la lección VLM: PINCIA DE PINTOR — punta triangular blanca
#     arriba, cuerpo grueso al medio que se afila, vena blanca y
#     destellos laterales — un TRAZO, no un bloque)
# ============================================================
img3 = Image.new('RGBA', (S, S), (0, 0, 0, 0))
P = img3.load()

ARCOIRIS = [(232, 44, 74), (255, 140, 30), (255, 228, 60), (60, 224, 120),
            (0, 234, 234), (70, 130, 255), (150, 90, 255)]

# EL TRAZO ASIMÉTRICO (la lección VLM v5: la U murió — el borde
# IZQUIERDO es la línea continua del pincel hasta abajo; el DERECHO
# se DESHACE en píxeles sueltos que derivan hacia afuera)
for y in range(1, 28):
    if y <= 6:
        half = max(1, int(round(1.0 + 4.0 * (y - 1) / 5.0)))
    elif y <= 20:
        half = 4                              # el cuerpo (afinado v6)
    else:
        half = max(2, int(round(5.0 - 3.0 * (y - 20) / 7.0)))
    fr = (27 - y) / 26.0                      # el matiz: violeta arriba → rojo abajo
    idx = int(max(0.0, min(1.0, fr)) * 6.999)
    color = ARCOIRIS[idx]
    # el lado IZQUIERDO: siempre continuo (la espina del trazo)
    for x in range(15 - half, 15):
        P[x, y] = (*color, 255 if x > 15 - half else 140)
    P[15 - half, y] = (*color, 140)           # el filo izquierdo suave
    # el lado DERECHO: sólido arriba, DISUELTO abajo (y>=20: se desmigaja)
    if y <= 20:
        for x in range(15, 15 + half + 1):
            P[x, y] = (*color, 140 if x == 15 + half else 255)
    else:
        # los píxeles disueltos: derivan afuera y se apagan
        f = (y - 20) / 7.0
        if y % 2 == 0:
            P[15, y] = (*color, int(255 - 100 * f))
            P[16, y] = (*color, int(210 - 120 * f))
            if y < 25:
                P[17, y] = (*color, int(150 - 100 * f))
        else:
            P[15, y] = (*color, int(230 - 110 * f))
            if y < 24:
                P[16, y] = (*color, int(170 - 100 * f))
                P[18, y] = (*color, int(110 - 80 * f))   # la mota derivada

# LA VENA BLANCA central (todo el cuerpo sólido)
for y in range(2, 21):
    for x in (13, 14, 15, 16, 17):
        P[x, y] = (*BLANCO, 255 if x == 15 else 235)

# LA PUNTA TRIANGULAR BLANCA (la lección VLM v3: un pico LARGO y
# totalmente blanco — 4 filas, sin violeta encima de la vena)
for (x, y, a) in [(15, 0, 255), (14, 1, 255), (15, 1, 255), (16, 1, 255),
                  (13, 2, 235), (14, 2, 255), (15, 2, 255), (16, 2, 255), (17, 2, 235),
                  (13, 3, 220), (14, 3, 255), (15, 3, 255), (16, 3, 255), (17, 3, 220)]:
    P[x, y] = (*BLANCO, a)

# LOS DESTELLOS LATERALES del cuerpo sólido
for (x, y) in [(9, 8), (21, 8), (9, 15), (21, 15)]:
    P[x, y] = (*BLANCO, 200)

# LAS ULTIMAS MOTAS — LA ESTELA QUE SE APAGA (v6: MÁS VISIBLE — tres
# filas de píxeles rojos/rosas/naranjas/amarillos que se dispersan)
ROSITA = (255, 130, 115)
NARANJA_CLARO = (255, 175, 130)
AMAR_CLARO = (255, 220, 140)
for (x, y, c, a) in [(13, 27, (232, 44, 74), 220), (14, 27, ROSITA, 220), (15, 27, (232, 44, 74), 230),
                     (16, 27, ROSITA, 220), (17, 27, (232, 44, 74), 220),
                     (12, 28, ROSITA, 170), (14, 28, NARANJA_CLARO, 180), (16, 28, AMAR_CLARO, 160),
                     (18, 28, ROSITA, 170), (15, 28, NARANJA_CLARO, 170),
                     (13, 29, AMAR_CLARO, 110), (15, 29, NARANJA_CLARO, 120), (17, 29, AMAR_CLARO, 110)]:
    P[x, y] = (*c, a)

img3.save(BASE + 'Content/Projectiles/Emperatriz/TrazoEmperatrizProjectile.png')
print(f"OK -> {BASE}Content/Projectiles/Emperatriz/TrazoEmperatrizProjectile.png {img3.size}")

# ============================================================
#  VERIFICACIÓN ASCII de los tres
# ============================================================
def ascii_art(im, nombres):
    a = np.array(im)
    h, w = a.shape[:2]
    print(f"\n===== {nombres} =====")
    for y in range(h):
        row = ''
        for x in range(w):
            r, g, b, al = a[y][x]
            if al < 40:
                row += '.'
            elif al < 200:
                row += '~'
            elif r > 235 and g > 235 and b > 235:
                row += 'W'
            elif r > 200 and g > 150 and b < 120:
                row += 'O'      # oro / amarillo
            elif r > 180 and b > 120 and g < 150:
                row += 'R'      # rojo-magenta
            elif g > 180 and b > 120 and r < 150:
                row += 'C'      # cian-verde
            elif b > 150 and g < 160 and r < 160:
                row += 'V'      # azul-violeta
            elif r > 150 and g > 100 and b > 200:
                row += 'p'      # perla-rosa
            elif r > 100 and g < 90 and b < 90:
                row += 'r'
            else:
                row += 'x'
        print(row)

ascii_art(out, 'BOLSA EMPERATRIZ')
ascii_art(img2, 'ESTELA DE AETHON')
ascii_art(img3, 'TRAZO DE LA EMPERATRIZ')

# mocks ×8 para el VLM
for nombre, im in [('bolsa_emperatriz_v650100', out),
                   ('estela_aethon_v650100', img2),
                   ('trazo_emperatriz_v650100', img3)]:
    im.resize((240, 240), Image.NEAREST).save(f'{MOCK}{nombre}_mock.png')
    print(f"mock -> {MOCK}{nombre}_mock.png")
