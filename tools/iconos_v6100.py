#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
iconos_v6100.py — LOS TRES ICONOS DÉBILES, REFORJADOS (v6.100).

LA AUTOPSIA VLM del set recuperado de la sesión interrumpida: el ojo
(#2, 8/10) y el libro (#5) LEEN; la corona de plumas (#3) era «un
estallido disperso», el derrame (#4) «una caldera» y la sombra IV
(#6) «violeta sobre azul, contraste casi cero — ilegible a 1×».

LA CURA (la regla de los iconos buenos de la casa — el ojo y el
libro): UNA silueta fuerte de 2 colores + UN detalle de alto
contraste. Transparencia NATIVA (lienzo RGBA, cero post-proceso).

  · LA CORONA DE PLUMAS: la banda dorada de la tiara + CINCO plumas
    BLANCAS con espina y barbas dibujadas (nada de puntitos sueltos)
    y el borde teal del santuario.
  · EL DERRAME DE TINTA: el CHARCO violeta brillante (elipse con
    reflejo blanco y el borde claro) + TRES gotas cayendo del pico
    de pluma de arriba — se lee «tinta derramándose».
  · LA SOMBRA DE LA PÁGINA IV: el tentáculo NEGRO-VIOLETA con FILO
    DE LUZ violeta (el rim que lo recorta del fondo) y EL OJO
    blanco-teal en la punta — el iris que mira.

USO: python3 tools/iconos_v6100.py
"""
import math
from PIL import Image

BASE = 'AethonMod/Content/Weapons'

# La paleta de la casa.
BLANCO = (250, 250, 245, 255)
MARFIL = (235, 250, 245, 255)
TEAL = (90, 220, 180, 255)
TEAL_HONDO = (30, 120, 100, 255)
ORO = (255, 208, 96, 255)
ORO_HONDO = (200, 132, 42, 255)
VIOLETA = (150, 96, 235, 255)
VIOLETA_CLARO = (196, 150, 255, 255)
VIOLETA_HONDO = (70, 40, 120, 255)
TINTA = (24, 14, 40, 255)


def nuevo():
    L = [[(0, 0, 0, 0)] * 30 for _ in range(30)]
    return L


def pon(L, x, y, c):
    if 0 <= x < 30 and 0 <= y < 30:
        L[y][x] = c


def guardar(L, ruta):
    img = Image.new('RGBA', (30, 30))
    img.putdata([c for fila in L for c in fila])
    img.save(ruta)
    print('OK —', ruta)


# ============================================================
#  LA CORONA DE PLUMAS — la tiara dorada con 5 plumas SEPARADAS.
# ============================================================
def corona():
    L = nuevo()
    # LA BANDA — el arco dorado de la tiara (y=21..24) CON HUECOS
    # entre pluma y pluma (la banda se ve ENTRE las plumas).
    for x in range(4, 26):
        yc = 23
        if x in (4, 5, 24, 25):
            yc = 24
        for dy in (-1, 0, 1):
            pon(L, x, yc + dy, ORO if dy == 0 else ORO_HONDO)
    # las puntas de la banda.
    for y in range(22, 27):
        pon(L, 4, y, ORO)
        pon(L, 25, y, ORO)
    # CINCO PLUMAS SEPARADAS — 3 px de calle entre cada una, cada
    # pluma UNA silueta limpia: espina de 1 px + barbas cortas
    # diagonales (nada de masa continua).
    alturas = [7, 12, 15, 12, 7]
    xs = [6, 10, 15, 20, 24]
    for cx, alto in zip(xs, alturas):
        base = 22
        for k in range(alto):
            y = base - k
            # LA ESPINA (1 px — el eje limpio).
            pon(L, cx, y, BLANCO if k < alto - 2 else MARFIL)
            # LAS BARBAS — SOLO 2 pares por pluma, cortas y diagonales
            # (las de un penacho de verdad, no una masa).
            if alto >= 10 and k in (2, alto // 2):
                pon(L, cx - 1, y, MARFIL)
                pon(L, cx + 1, y, MARFIL)
            if alto < 10 and k == 2:
                pon(L, cx - 1, y, MARFIL)
                pon(L, cx + 1, y, MARFIL)
        # LA PUNTA teal de cada pluma (la corona del santuario).
        pon(L, cx, base - alto, TEAL)
    # EL BRILLO de la banda (la gema central).
    pon(L, 15, 23, BLANCO); pon(L, 15, 24, MARFIL)
    return L


# ============================================================
#  EL DERRAME DE TINTA — el charco brillante con sus gotas.
# ============================================================
def derrame():
    L = nuevo()
    # EL CHARCO — la elipse violeta (y=20..26), borde claro arriba
    # (la luz) y hondo abajo (el grosor de la tinta).
    for y in range(19, 27):
        semiancho = {19: 4, 20: 8, 21: 10, 22: 11, 23: 11, 24: 9, 25: 7, 26: 4}[y]
        for x in range(15 - semiancho, 15 + semiancho + 1):
            borde = abs(x - 15) >= semiancho - 1 or y == 19
            c = VIOLETA_CLARO if borde else VIOLETA
            if y >= 24 and abs(x - 15) < semiancho - 2:
                c = VIOLETA_HONDO
            pon(L, x, y, c)
    # EL REFLEJO — la luna blanca del charco (el brillo de la tinta).
    for (x, y) in [(12, 21), (13, 21), (14, 21), (12, 22), (13, 22)]:
        pon(L, x, y, BLANCO)
    # EL PIQUE DE LA PLUMA — el plumín de arriba goteando (la fuente).
    for y in range(4, 11):
        pon(L, 15, y, MARFIL)
        pon(L, 16, y, BLANCO if y > 5 else MARFIL)
        if y < 9:
            pon(L, 14, y, TEAL)
    # LAS DOS GOTAS CLARAS cayendo (con estela — se VEN).
    for (x, y) in [(15, 13), (13, 15)]:
        pon(L, x, y, VIOLETA_CLARO)
        pon(L, x, y + 1, VIOLETA_CLARO)
        pon(L, x, y + 2, VIOLETA)
    # los saltitos del chapoteo (a los lados del charco).
    pon(L, 4, 18, VIOLETA_CLARO); pon(L, 25, 19, VIOLETA_CLARO)
    pon(L, 26, 17, VIOLETA); pon(L, 3, 16, VIOLETA)
    return L


# ============================================================
#  LA SOMBRA DE LA PÁGINA IV — el tentáculo con su ojo (limpio).
# ============================================================
def sombra4():
    L = nuevo()
    # EL TENTÁCULO — diagonal LIMPIA diseñada a mano: gordo (5 px)
    # abajo-derecha afilando (2 px) arriba-izquierda, con UNA curva
    # suave. Cuerpo negro-tinta + FILO violeta en el borde superior.
    # (la fila central del cuerpo por cada y: de y=26 a y=7)
    for y in range(26, 6, -1):
        u = (26 - y) / 20.0              # 0 abajo → 1 arriba
        # el centro x serpentea SUAVE (una sola curva).
        cx = 21 - 13 * u
        # el grosor mengua: 5 → 2.
        gordo = 2.5 - 1.5 * u
        for dx in range(-4, 5):
            if abs(dx) <= gordo:
                c = TINTA if abs(dx) < gordo - 0.7 else VIOLETA_HONDO
                pon(L, int(round(cx)) + dx, y, c)
        # EL FILO DE LUZ — el rim violeta CLARO en el borde superior
        # (la luz de la página que lo recorta del fondo oscuro).
        borde_x = int(round(cx)) - int(gordo)
        pon(L, borde_x, y, VIOLETA)
        pon(L, borde_x + 1, y, VIOLETA_CLARO)
    # LA PUNTA — el enganche de la cabeza: el cuerpo termina en pico.
    for k in range(4):
        pon(L, 8 + k, 7 - k, TINTA)
        pon(L, 7 + k, 7 - k, VIOLETA_CLARO if k % 2 == 0 else VIOLETA)
    # EL OJO — GRANDE y CLARO en la punta: la almendra blanca con la
    # pupila teal mirando al frente (el iris que MIRA — la firma del
    # tomo IV).
    ex, ey = 9, 4
    for dy in range(-2, 3):
        for dx in range(-4, 5):
            d = math.hypot(dx, dy * 1.5)
            if d <= 4.0:
                c = BLANCO
                if d > 3.2:
                    c = VIOLETA_CLARO
                if abs(dx) <= 1 and abs(dy) <= 1:
                    c = TEAL_HONDO
                pon(L, ex + dx, ey + dy, c)
    # el brillo del ojo.
    pon(L, ex - 2, ey - 1, BLANCO)
    # LAS VENTOSAS — tres puntitos violeta claro en la panza curva.
    for (x, y) in [(18, 24), (15, 20), (12, 16)]:
        pon(L, x, y, VIOLETA_CLARO)
        pon(L, x + 1, y, VIOLETA)
    return L


if __name__ == '__main__':
    guardar(corona(), f'{BASE}/Auras/CoronaDePlumas.png')
    guardar(derrame(), f'{BASE}/Auras/DerrameDeTinta.png')
    guardar(sombra4(), f'{BASE}/Sombras/SombraDeLaPagina4.png')
