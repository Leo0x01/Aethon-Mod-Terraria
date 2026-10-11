#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_emperatriz_v65066.py — LAS CUATRO ARMAS DE LA EMPERATRIZ (v6.50.66).

Iconos 30×30, píxel sólido puro (lección v6.50.65: ZONAS CLARAS sin
mezcla alfa — composiciones embarradas = 3/10 en VLM):

  1. GRIMORIO SELLADO (opción A) — el libro oscuro con el SELLO DE CERA
     DORADO (estrella cósmica en relieve) y las CUATRO GEMAS cósmicas
     (oro/cian/magenta/índigo) en las esquinas de la tapa.
  2. ECO DE LA EMPERATRIZ (opción B) — el libro abierto exhalando CUATRO
     ORBES ARCOÍRIS que suben en abanico (rojo/amarillo/verde-cian/azul).
  3. VEREDICTO DE LA EMPERATRIZ (opción C) — el TRAZO GORDO: una banda
     diagonal ARCOÍRIS con núcleo blanco cruzando sobre el libro.
  4. EQUINOCCIO (la cuarta, dual) — el libro PARTIDO EN DOS: media tapa
     DORADA con su sol, media tapa NEGRA-VIOLETA con su bruma; arriba,
     el orbe dorado y el orbe del eclipse.
"""
from PIL import Image
import math

W = H = 30
BASE = 'AethonMod/Content/Weapons/Emperatriz/'
MOCK = 'tools/tools/'

NEGRO = (12, 8, 20)
NEGRO_MED = (26, 17, 40)
VIOLETA = (108, 66, 178)
VIOLETA_BORDE = (150, 108, 216)
BLANCO = (250, 248, 255)
GRIS_PAG = (200, 194, 216)

ORO = (255, 217, 61)
ORO_OSC = (188, 138, 22)
ORO_CLARO = (255, 241, 170)
CIAN = (0, 234, 234)
MAGENTA = (240, 60, 150)
INDIGO = (130, 100, 240)

ROJO = (232, 44, 74)
NARANJA = (255, 140, 30)
AMARILLO = (255, 228, 60)
VERDE = (60, 224, 120)
AZUL = (70, 130, 255)


def nuevo():
    img = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    return img, img.load()


def set_px(P, y, x, c, a=255):
    if 0 <= y < H and 0 <= x < W:
        P[x, y] = (c[0], c[1], c[2], a)


def gem(P, cx, cy, c, brillo=BLANCO):
    """Una gema cósmica 3×3 con corazón blanco."""
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            set_px(P, cy + dy, cx + dx, c)
    set_px(P, cy, cx, brillo)


# ================================================================
# 1 — EL GRIMORIO SELLADO: el libro + el sello de lacre dorado
# ================================================================
def icono_sellado():
    img, P = nuevo()
    # EL LIBRO (tapa sólida, borde violeta legible)
    for y in range(13, 28):
        for x in range(6, 24):
            c = NEGRO_MED
            if x in (6, 23) or y in (13, 27):
                c = VIOLETA_BORDE
            elif (x + y * 2) % 7 == 0:
                c = NEGRO
            set_px(P, y, x, c)
    # EL SELLO DE LACRE DORADO — círculo con la estrella cósmica
    for y in range(16, 25):
        for x in range(10, 20):
            d = math.hypot(x - 15, y - 20.5)
            if d <= 4.2:
                c = ORO if d > 2.6 else ORO_CLARO
                set_px(P, y, x, c)
    # la estrella de 4 puntas en relieve
    set_px(P, 20, 15, BLANCO); set_px(P, 21, 15, BLANCO)
    set_px(P, 20, 15 + 1, BLANCO)  # centro
    set_px(P, 18, 15, ORO_OSC); set_px(P, 23, 15, ORO_OSC)
    set_px(P, 20, 13, ORO_OSC); set_px(P, 20, 17, ORO_OSC)
    set_px(P, 19, 14, BLANCO, 200); set_px(P, 21, 16, ORO_OSC)
    # LAS CUATRO GEMAS cósmicas en las esquinas de la tapa
    gem(P, 9, 15, ORO); gem(P, 20, 15, CIAN)
    gem(P, 9, 25, MAGENTA); gem(P, 20, 25, INDIGO)
    # DOS FRAGMENTOS que escapan por arriba (la fuente)
    set_px(P, 11, 8, ORO); set_px(P, 10, 9, ORO, 160)
    set_px(P, 19, 6, CIAN); set_px(P, 20, 7, CIAN, 160)
    return img


# ================================================================
# 2 — EL ECO: el libro abierto + los orbes arcoíris subiendo
# ================================================================
def icono_eco():
    img, P = nuevo()
    # EL LIBRO ABIERTO — dos hojas de página con el lomo central
    for y in range(20, 28):
        for x in range(6, 24):
            c = GRIS_PAG
            if x in (6, 23) or y == 27:
                c = NEGRO
            elif x in (14, 15):
                c = NEGRO_MED          # el lomo
            elif y == 20 and (x <= 8 or x >= 21):
                continue               # esquinas de página cortadas
            elif (x + y) % 6 == 0:
                c = (176, 168, 198)    # vetas de la página
            set_px(P, y, x, c)
    # la tapa asomando bajo las páginas
    for x in range(6, 24):
        set_px(P, 26, x, VIOLETA, 200); set_px(P, 27, x, VIOLETA_BORDE)
    # LOS CUATRO ORBES ARCOÍRIS — GRANDES (radio 2, círculo sin
    # esquinas), CORAZÓN BLANCO 2×2 y ESTELA PUNTEADA que llega
    # al lomo del libro
    orbes = [(7, 12, ROJO), (12, 6, AMARILLO), (18, 6, VERDE), (23, 12, AZUL)]
    for ox, oy, c in orbes:
        # el orbe: círculo liso de radio 2 (sin esquinas)
        for dy in range(-2, 3):
            for dx in range(-2, 3):
                if dx * dx + dy * dy <= 5:
                    set_px(P, oy + dy, ox + dx, c)
        # el corazón: UN solo píxel blanco centrado (simétrico)
        set_px(P, oy, ox, BLANCO)
        # LA ESTELA CONTINUA: línea de píxeles SEGUIDOS del borde
        # inferior del orbe al lomo del libro (15, 21) — se apaga
        pasos = 16
        for paso in range(1, pasos + 1):
            t = paso / pasos
            ex = int(round(ox + (15 - ox) * t))
            ey = (oy + 2) + (21 - (oy + 2)) * t
            if ey >= 20:
                break               # llegó al libro
            alfa = 235 - int(t * 115)
            set_px(P, int(round(ey)), ex, c, alfa)
            if t < 0.5:             # más gruesa cerca del orbe
                set_px(P, int(round(ey)) + 1, ex + (1 if ox < 15 else -1), c, alfa - 60)
    # el aliento del libro: destello blanco sobre el lomo
    set_px(P, 18, 15, BLANCO); set_px(P, 19, 14, BLANCO, 180)
    return img


# ================================================================
# 3 — EL VEREDICTO: el libro + EL TRAZO GORDO arcoíris
# ================================================================
def icono_veredicto():
    img, P = nuevo()
    # EL LIBRO pequeño abajo (el que obedece)
    for y in range(21, 29):
        for x in range(9, 21):
            c = NEGRO_MED
            if x in (9, 20) or y in (21, 28):
                c = VIOLETA_BORDE
            set_px(P, y, x, c)
    for x in range(11, 19):
        set_px(P, 24, x, VIOLETA)      # la banda de la tapa
    set_px(P, 26, 15, BLANCO)          # el ojo del libro
    # EL TRAZO — banda diagonal gruesa, segmentada arcoíris,
    # con NÚCLEO BLANCO (el streak de la jefa)
    x0, y0, x1, y1 = 3.0, 4.0, 26.0, 19.0
    arcoiris = [ROJO, NARANJA, AMARILLO, VERDE, CIAN, AZUL, VIOLETA]
    L = math.hypot(x1 - x0, y1 - y0)
    # la banda: desplazamientos perpendiculares −2..+2
    nx, ny = -(y1 - y0) / L, (x1 - x0) / L
    for paso in range(int(L) + 1):
        t = paso / L
        px, py = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
        seg = min(6, int(t * 7))
        c = arcoiris[seg]
        # los extremos se afilan (la punta del trazo)
        ancho = 2 if (t < 0.08 or t > 0.92) else 2.6
        for d in (-3, -2, -1, 0, 1, 2, 3):
            if abs(d) <= ancho:
                set_px(P, int(py + ny * d), int(px + nx * d), c, 255 if abs(d) <= 1 else 170)
        # EL NÚCLEO BLANCO
        set_px(P, int(py), int(px), BLANCO)
    # el destello del origen (donde nace)
    set_px(P, 3, 4, BLANCO); set_px(P, 2, 5, BLANCO, 160); set_px(P, 4, 3, BLANCO, 160)
    return img


# ================================================================
# 4 — EL EQUINOCCIO: el libro partido + el sol y el eclipse
# ================================================================
def icono_equinoccio():
    img, P = nuevo()
    # EL LIBRO PARTIDO — izquierda DORADA (el día), derecha NEGRA (la noche)
    for y in range(15, 28):
        for x in range(6, 24):
            if x <= 14:
                c = ORO if (x + y) % 9 else ORO_OSC      # la tapa del día
                if x in (6, 14) or y in (15, 27):
                    c = ORO_CLARO
            else:
                c = NEGRO_MED if (x + y * 2) % 7 else NEGRO   # la tapa de la noche
                if x in (15, 23) or y in (15, 27):
                    c = VIOLETA_BORDE
            set_px(P, y, x, c)
    # EL SOL de la tapa dorada — círculo claro con corazón blanco
    # (2-3px, inequívoco)
    set_px(P, 19, 10, BLANCO)
    set_px(P, 19, 9, ORO_CLARO); set_px(P, 19, 11, ORO_CLARO)
    set_px(P, 18, 10, ORO_CLARO); set_px(P, 20, 10, ORO_CLARO)
    set_px(P, 18, 9, ORO, 170); set_px(P, 20, 11, ORO, 170)
    # EL CRECIENTE de la tapa negra — media luna CLARA (la C de la noche)
    LUNA = (226, 220, 240)
    set_px(P, 17, 19, LUNA); set_px(P, 18, 19, LUNA); set_px(P, 19, 19, LUNA)
    set_px(P, 19, 20, LUNA, 200); set_px(P, 19, 18, LUNA, 200)
    set_px(P, 18, 20, (120, 112, 150), 160)   # la mordida interior
    # EL LOMO DEL LIBRO — la línea de encuadernación que parte el
    # mundo en dos (blanca arriba, se funde con las tapas abajo)
    for y in range(15, 28):
        if y < 18:
            set_px(P, y, 14, BLANCO, 235); set_px(P, y, 15, BLANCO, 140)
        elif y < 25:
            set_px(P, y, 14, ORO_CLARO, 200); set_px(P, y, 15, VIOLETA_BORDE, 200)
        else:
            set_px(P, y, 14, GRIS_PAG, 170); set_px(P, y, 15, GRIS_PAG, 170)
    # el canto de las páginas en la base — DOS líneas alternadas (lee LIBRO)
    for x in range(8, 22):
        set_px(P, 28, x, BLANCO if x % 2 else GRIS_PAG, 220)
        set_px(P, 27, x, GRIS_PAG, 160)
    set_px(P, 28, 7, NEGRO, 160); set_px(P, 28, 22, NEGRO, 160)
    # EL ORBE DORADO arriba-izquierda — con sus rayos de sol
    gem(P, 9, 7, ORO)
    for dx, dy in [(-2, 0), (2, 0), (0, -2), (0, 2), (-2, -2), (2, 2)]:
        set_px(P, 7 + dy, 9 + dx, ORO_CLARO, 170)
    # EL ORBE NEGRO arriba-derecha — el eclipse REDONDO con rim violeta
    for dy in range(-2, 3):
        for dx in range(-2, 3):
            d2 = dx * dx + dy * dy
            if d2 <= 5:
                # el cuerpo: negro con un corazón tenue
                set_px(P, 8 + dy, 21 + dx, (34, 22, 52) if d2 else (64, 40, 96))
    # el rim violeta en los 4 puntos cardinales (la lectura nocturna)
    set_px(P, 6, 21, VIOLETA_BORDE); set_px(P, 10, 21, VIOLETA_BORDE)
    set_px(P, 8, 19, VIOLETA_BORDE); set_px(P, 8, 23, VIOLETA_BORDE)
    set_px(P, 7, 20, VIOLETA, 140); set_px(P, 9, 22, VIOLETA, 140)
    # LA BRUMA bajo el orbe negro (volutas con presencia)
    set_px(P, 11, 20, VIOLETA, 140); set_px(P, 12, 21, VIOLETA_BORDE, 120)
    set_px(P, 12, 23, VIOLETA, 110); set_px(P, 13, 22, VIOLETA_BORDE, 100)
    set_px(P, 12, 25, VIOLETA, 80)
    return img


# ================================================================
#  GENERAR + MOCKS 8× PARA EL VLM
# ================================================================
iconos = [
    ('GrimorioEstrellado', icono_sellado()),
    ('EcoEmperatriz', icono_eco()),
    ('VeredictoEmperatriz', icono_veredicto()),
    ('Equinoccio', icono_equinoccio()),
]

for nombre, img in iconos:
    img.save(BASE + nombre + '.png')
    img.resize((W * 8, H * 8), Image.NEAREST).save(MOCK + f'emperatriz_{nombre.lower()}_v65066_mock.png')
    print('OK:', nombre, img.size)
