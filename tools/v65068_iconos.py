#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.50.68 — LOS 3 ICONOS v3 (la lección VLM ronda 2: nada de
checkerboard — SILUETAS SÓLIDAS; la garra del sello en BLANCO para que
PRENDE; marcas en diamante angulares; el filo rojo de 1px continuo)."""
from PIL import Image
import math

W = H = 30
NEGRO = (8, 4, 7, 255)
HUMO = (18, 10, 28, 255)
VIOLETA = (118, 74, 190, 255)
VIOLETA_CLARO = (156, 108, 226, 255)
BLANCO = (250, 248, 244, 255)
CREMA = (214, 206, 190, 255)      # la sombra del hueso
ROJO = (198, 18, 24, 255)
ROJO_OSCURO = (120, 8, 12, 255)

def lienzo():
    return Image.new("RGBA", (W, H), (0, 0, 0, 0))

def px(im, x, y, c):
    x, y = int(round(x)), int(round(y))
    if 0 <= x < W and 0 <= y < H:
        im.putpixel((x, y), c)

def tri(im, x, y, dx, dy, h, c, c2=None):
    """Un triángulo sólido (la barba): vértice en (x,y), base h pasos
    hacia (dx,dy) — el arte de la pluma: LOBOS sólidos, no ruido."""
    for k in range(h + 1):
        for m in range(-1, 2):
            px(im, x + dx * k + m * (dy != 0), y + dy * k + m * (dx != 0),
               c if k < h - 1 or c2 is None else c2)

# ======================================================================
#  LA PLUMA v3 — el RAQUIS BLANCO (la caña de hueso: visible sobre
#  cualquier fondo), la bandera: CINCO LOBOS SÓLIDOS con borde jagged,
#  la punta desnuda y la gota gorda con brillo
# ======================================================================
def gen_pluma():
    im = lienzo()
    # EL RAQUIS BLANCO — la caña de hueso (de (6,24) a (27,4), recta y
    # limpia: la aguja ES la pluma)
    for i in range(23):
        x, y = 6 + i, 24 - i * (20 / 22.0)
        px(im, x, y, BLANCO)
    # sombra de la caña (el volumen)
    for i in range(0, 23, 2):
        x, y = 6 + i, 24 - i * (20 / 22.0)
        px(im, x, y + 1, CREMA)

    # LA BANDERA — cinco lobos sólidos a CADA lado, inclinados hacia la
    # punta (dx=+1 hacia la punta, dy=+1 abajo / -1 arriba), tamaño
    # decreciente hacia la punta: la silueta de pluma de verdad
    lados = [
        # (i del raquis, alto del lobo)
        (2, 4), (5, 5), (8, 5), (11, 4), (14, 3),
    ]
    for (i0, h) in lados:
        x = 6 + i0
        y = 24 - i0 * (20 / 22.0)
        # LODO inferior (abajo del raquis): triángulo violeta sólido
        for k in range(h):
            for m in range(k + 1):
                px(im, x + k, y + 1 + m, VIOLETA)
        # su brillo superior (la luz de la magia)
        px(im, x + 1, y + 1, VIOLETA_CLARO)
        # LODO superior (arriba del raquis)
        y2 = 24 - (i0 + 2) * (20 / 22.0)
        for k in range(h):
            for m in range(k + 1):
                px(im, x + k, y2 - 1 - m, VIOLETA)
        px(im, x + 1, y2 - 1, VIOLETA_CLARO)

    # LA PUNTA DESNUDA — la aguja de hueso fina (los últimos 5 px de la
    # caña, ya sin bandera: la parte que ESCRIBE)
    for i in range(23, 28):
        px(im, i, 24 - i * (20 / 22.0), BLANCO)
    # LA GOTA — gorda, negra, con brillo violeta (glossy)
    gx, gy = 5, 26
    for (dx, dy) in ((0, 0), (1, 0), (2, 0), (0, 1), (1, 1), (2, 1), (0, 2), (1, 2), (2, 2)):
        px(im, gx + dx, gy + dy, NEGRO)
    px(im, gx + 3, gy + 1, NEGRO)
    px(im, gx, gy, VIOLETA_CLARO)      # el brillo
    px(im, gx + 3, gy, VIOLETA)        # la salpicadura
    return im

# ======================================================================
#  LA HOJA v3 — el crescente limpio: filo BLANCO continuo de 1px con
#  textura de hueso (CREMA cada 2px), el ROJO de 1px corrido, puntas
#  unidas y cero píxeles huérfanos
# ======================================================================
def gen_hoja():
    im = lienzo()
    def arco(y):
        t = (y - 2) / 25.0
        return int(round(10 + 14 * (1 - (2 * t - 1) ** 2) * 0.92))

    # LA CARNE + EL FILO (de y=4 a 25: puntas aparte)
    for y in range(4, 26):
        t = (y - 2) / 25.0
        x = arco(y)
        grosor = int(2 + 3 * (1 - abs(2 * t - 1)))
        for g in range(grosor):
            px(im, x - g, y, HUMO if g else NEGRO)
        # EL FILO DE HUESO — 1px CONTINUO blanco + textura crema
        px(im, x + 1, y, CREMA if y % 2 else BLANCO)
        # EL BORDE ROJO — 1px corrido (el filo que corta)
        px(im, x - grosor, y, ROJO)
        if y % 3 == 0:
            px(im, x - grosor - 1, y, ROJO_OSCURO)
    # LAS PUNTAS — agujas unidas a la curva (arriba y abajo)
    for k in range(4):
        px(im, arco(4) - 3 - k, 3 - k + 1, BLANCO)
        px(im, arco(25) - 3 - k, 26 + k - 1, BLANCO)
    px(im, 6, 1, BLANCO)
    px(im, 7, 0, BLANCO)
    px(im, 6, 28, BLANCO)
    px(im, 7, 29, BLANCO)
    # EL OJO DE LA SOMBRA (el vientre mira — blanco + pupila roja)
    ex, ey = arco(13), 13
    px(im, ex - 2, ey, BLANCO)
    px(im, ex - 2, ey + 1, BLANCO)
    px(im, ex - 3, ey, ROJO)
    return im

# ======================================================================
#  EL SELLO v3 — contraste BRUTAL: el anillo exterior CLARO brillante,
#  el interior violeta oscuro, OCHO DIAMANTES rúnicos blancos y LA
#  GARRA BLANCA de hueso (la estrella del centro: PRENDE sí o sí)
# ======================================================================
def gen_sello():
    im = lienzo()
    cx, cy = 15, 15
    # LOS ANILLOS — exterior BRILLANTE (1px, limpio), interior VIOLETA
    for ang in range(0, 360):
        a = math.radians(ang)
        px(im, cx + math.cos(a) * 12.4, cy + math.sin(a) * 12.4, VIOLETA_CLARO)
        px(im, cx + math.cos(a) * 11.6, cy + math.sin(a) * 11.6, VIOLETA)
        px(im, cx + math.cos(a) * 7.6, cy + math.sin(a) * 7.6, VIOLETA)
    # LOS 4 PUNTOS CARDINALES del anillo (más brillo donde se firma)
    for (dx, dy) in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        px(im, cx + dx * 12, cy + dy * 12, BLANCO)
        px(im, cx + dx * 13, cy + dy * 13, VIOLETA_CLARO)
    # LOS OCHO DIAMANTES rúnicos (plus rotado 45°: 5px) entre anillos
    for k in range(8):
        a = math.radians(k * 45 + 22.5)
        mx, my = cx + math.cos(a) * 9.6, cy + math.sin(a) * 9.6
        px(im, mx, my, BLANCO)
        px(im, mx + 1, my, VIOLETA_CLARO)
        px(im, mx - 1, my, VIOLETA_CLARO)
        px(im, mx, my + 1, VIOLETA_CLARO)
        px(im, mx, my - 1, VIOLETA_CLARO)
    # EL PISO — cuatro puntos tenues (nada que tape la garra)
    for ang in range(0, 360, 90):
        a = math.radians(ang + 45)
        px(im, cx + math.cos(a) * 3.5, cy + math.sin(a) * 3.5, HUMO)
    # LA GARRA — DOS ZARPOS GRUESOS de hueso (la ronda 3 del VLM: una
    # línea sola = manecilla de reloj; DOS garras de 3px de grosor con
    # puntas afiladas = UNA GARRA de verdad)
    # EL ZARPO MAYOR: base 3px de ancho que se afila, gancho arriba-izq
    mayor = [
        # (x, y, color) — la columna vertebral del zarpo
        (18, 20), (17, 20), (16, 20),            # la base GORDA (3px)
        (18, 19), (17, 19), (16, 19),
        (17, 18), (16, 18),
        (16, 17), (15, 17),
        (15, 16), (14, 16),
        (14, 15), (13, 15),
        (13, 14), (12, 14),
        (12, 13), (11, 13),
        (11, 12), (10, 12),
        (10, 11),
        (10, 10),
        (9, 10),                                  # la PUNTA afilada del gancho
        (9, 9),
    ]
    for (x, y) in mayor:
        px(im, x, y, BLANCO)
    # el volumen: la cara interna del zarpo en crema (la sombra del hueso)
    for (x, y) in ((16, 19), (16, 18), (15, 16), (14, 15), (13, 14), (12, 13), (11, 12), (10, 11)):
        px(im, x - 1, y, CREMA)
    # EL SEGUNDO ZARPO — más corto, paralelo (el pinzado de la garra)
    menor = [
        (20, 19), (19, 19),
        (20, 18), (19, 18), (18, 18),
        (19, 17), (18, 17),
        (18, 16), (17, 16),
        (17, 15), (16, 15),
        (16, 14), (15, 14),
        (15, 13), (14, 13),
    ]
    for (x, y) in menor:
        px(im, x, y, BLANCO)
    px(im, 14, 12, BLANCO)                        # la punta del segundo
    # EL NÚCLEO — la brasa bajo la garra (2×2 + halo: la firma ARDE)
    px(im, 19, 21, ROJO)
    px(im, 20, 21, ROJO)
    px(im, 19, 22, ROJO)
    px(im, 20, 22, ROJO_OSCURO)
    px(im, 21, 21, VIOLETA_CLARO)                 # el halo de la brasa
    px(im, 20, 23, ROJO_OSCURO)
    # el destello de las puntas
    px(im, 8, 9, VIOLETA_CLARO)
    px(im, 8, 10, VIOLETA_CLARO)
    return im

BASE = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Content/Weapons/Sombras/"
gen_pluma().save(BASE + "PlumaDeLaPagina.png")
gen_hoja().save(BASE + "HojaDeLaPagina.png")
gen_sello().save(BASE + "SelloDeLaPagina.png")
print("3 iconos v3 generados")

m = Image.new("RGBA", (30 * 3 * 8 + 16, 30 * 8 + 8), (40, 36, 44, 255))
for i, n in enumerate(("PlumaDeLaPagina", "HojaDeLaPagina", "SelloDeLaPagina")):
    icono = Image.open(BASE + n + ".png").resize((30 * 8, 30 * 8), Image.NEAREST)
    m.paste(icono, (8 + i * (30 * 8 + 4), 4), icono)
m.save("/tmp/v65068_iconos_montage.png")
print("montage: /tmp/v65068_iconos_montage.png")
