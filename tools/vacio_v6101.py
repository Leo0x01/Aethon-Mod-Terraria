#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
vacio_v6101.py — v6.101 — LOS ÍCONOS DEL VACÍO EXTERIOR + LA SOMBRA V.

LA LETRA DEL USUARIO: «un efecto de quemadura cuyo sprite de buff es
el mismo que el de quemadura de terraria pero con llamas negras» — y
la Sombra de la Página V con su fauce de colmillos.

TODO con transparencia NATIVA (el lienzo nace alfa=0 — la regla
permanente de la casa: la transparencia vive en la GENERACIÓN, jamás
en el post-proceso).

Salidas:
  Content/Buffs/QuemaduraVacioExterior.png (32×32) — LA QUEMADURA: la
      composición del debuff ¡Quemándose! de Terraria (la silueta
      envuelta en fuego) pero con LLAMAS NEGRAS de borde violeta.
  Content/Buffs/VacioExterior.png (32×32) — EL PAGO AL DUEÑO: la
      llama del vacío con el núcleo negro y la estrella de poder.
  Content/Weapons/Sombras/SombraDeLaPagina5.png (30×30) — LA FAUCE:
      el aro de colmillos blancos con la garganta roja y los tres
      ojos rasgados encima.
"""
from PIL import Image
import math
import os

BASE = os.path.join(os.path.dirname(__file__), '..', 'AethonMod')

# LA PALETA DEL VACÍO
NEGRO_LLAMA = (18, 12, 26)      # la llama negra (casi negro, violeta hondeando)
NEGRO_NUCLEO = (10, 6, 16)      # el núcleo del vacío
VIOLETA_HONDO = (74, 36, 128)   # la base de la llama
VIOLETA = (150, 84, 235)        # el borde vivo
VIOLETA_ALTO = (200, 150, 255)  # el brillo del borde
BLANCO = (248, 244, 255)        # el destello
ROJO = (198, 18, 24)            # la garganta
ROJO_HONDO = (120, 8, 12)       # la sangre viva
HUESO = (238, 232, 220)         # los colmillos


def px(im, x, y, c):
    im.putpixel((int(x), int(y)), c + (255,))


def llama_negra(im, cx, base_y, escala, lado=1.0):
    """UNA LENGA DE LLAMA NEGRA — la forma de la llama clásica (base
    ancha, punta curvada) dibujada en NEGRO con el BORDE VIOLETA (el
    negro solo existe en la pantalla si su borde arde violeta)."""
    h = 9 * escala
    w = 2.7 * escala   # QA v5: más estrecha — llama, no pilar
    for i in range(int(h)):
        t = i / h                       # 0 = base, 1 = punta
        # EL ANCHO: huevo → punta (la silueta de la llama)
        ancho = w * (1.0 - t * 0.82) * (0.55 + 0.45 * math.sin(t * math.pi))
        # LA CURVA: la lengua se inclina hacia su lado al subir
        # QA v5: + LA ONDULACIÓN viva de la punta (la llama se retuerce)
        curva = lado * (t * t * 2.2 + 0.35 * math.sin(t * 9.0)) * escala
        y = base_y - i
        cx_i = cx + curva
        for dx in range(-int(ancho) - 1, int(ancho) + 2):
            if abs(dx) > ancho:
                continue
            k = abs(dx) / max(ancho, 0.001)      # 0 = centro, 1 = borde
            if k > 0.72:
                # EL BORDE VIOLETA — lo que hace legible lo negro
                brillo = (VIOLETA if t < 0.6 else VIOLETA_ALTO)
                mix = (k - 0.72) / 0.28
                col = (int(NEGRO_LLAMA[0] * (1 - mix) + brillo[0] * mix),
                       int(NEGRO_LLAMA[1] * (1 - mix) + brillo[1] * mix),
                       int(NEGRO_LLAMA[2] * (1 - mix) + brillo[2] * mix))
                px(im, cx_i + dx, y, col)
            elif k > 0.45:
                # LA CARNE NEGRA
                px(im, cx_i + dx, y, NEGRO_LLAMA)
            else:
                # EL NÚCLEO — el vacío que se come la luz
                px(im, cx_i + dx, y, NEGRO_NUCLEO)


def quemadura():
    """LA QUEMADURA DEL VACÍO EXTERIOR — QA v4: brazos en V CLARA de
    2 px, llamas laterales APARTADAS (el hueco entre figura y fuego),
    cabeza redonda con borde brillante."""
    im = Image.new('RGBA', (32, 32), (0, 0, 0, 0))

    # LAS LLAMAS NEGRAS — TODAS DETRÁS Y APARTADAS (dejan AIRE alrededor
    # de la figura: el hueco es lo que separa figura de fuego)
    llama_negra(im, 6, 28, 2.3, lado=-0.8)
    llama_negra(im, 26, 28, 2.3, lado=0.8)
    llama_negra(im, 16, 31, 1.5, lado=0.15)   # QA v5: estrecha, sin comer las piernas
    llama_negra(im, 3, 24, 1.2, lado=-0.9)     # las lenguas menores del borde
    llama_negra(im, 29, 24, 1.2, lado=0.9)

    # LA FIGURA (ENCIMA) — violeta CLARO luminoso, brazos en V GRUESOS
    fig = [
        # LA CABEZA redonda (filas 8-12)
        (15, 8), (16, 8),
        (14, 9), (15, 9), (16, 9), (17, 9),
        (13, 10), (14, 10), (15, 10), (16, 10), (17, 10), (18, 10),
        (14, 11), (15, 11), (16, 11), (17, 11),
        (15, 12), (16, 12),
        # EL TORSO (filas 14-20)
        (14, 14), (15, 14), (16, 14), (17, 14),
        (13, 15), (14, 15), (15, 15), (16, 15), (17, 15), (18, 15),
        (13, 16), (14, 16), (15, 16), (16, 16), (17, 16), (18, 16),
        (14, 17), (15, 17), (16, 17), (17, 17),
        (14, 18), (15, 18), (16, 18), (17, 18),
        (14, 19), (15, 19), (16, 19), (17, 19),
        # LOS BRAZOS EN V — diagonal clara, 2 px de grueso cada tramo
        (12, 14), (11, 13), (10, 12), (9, 11), (8, 10),
        (12, 15), (11, 14), (10, 13), (9, 12), (8, 11),
        (19, 14), (20, 13), (21, 12), (22, 11), (23, 10),
        (19, 15), (20, 14), (21, 13), (22, 12), (23, 11),
        # LAS PIERNAS (filas 21-24)
        (14, 21), (15, 21), (16, 21), (17, 21),
        (14, 22), (16, 22), (14, 23), (16, 23),
    ]
    CARNE = (206, 168, 250)
    CARNE_NUCLEO = (162, 112, 224)
    CARNE_ALTA = (244, 232, 255)
    for x, y in fig:
        px(im, x, y, CARNE)
    # el volumen del torso
    for x, y in ((15, 15), (16, 15), (15, 16), (16, 16), (15, 17), (16, 17), (15, 18)):
        px(im, x, y, CARNE_NUCLEO)
    # LA CARA — dos puntos de ojos cerrados (el gesto del que arde)
    px(im, 15, 10, CARNE_NUCLEO)
    px(im, 16, 10, CARNE_NUCLEO)
    # el borde incandescente (contorno brillante: figura ES persona)
    for x, y in ((14, 9), (17, 9), (13, 10), (18, 10), (8, 10), (23, 10),
                 (13, 15), (18, 15), (14, 21), (17, 21), (15, 8), (16, 8)):
        px(im, x, y, CARNE_ALTA)

    # LAS CHISPAS DESPRENDIDAS
    for x, y, c in ((5, 10, VIOLETA), (27, 11, VIOLETA), (9, 4, VIOLETA_ALTO),
                    (22, 3, VIOLETA_ALTO), (16, 2, BLANCO), (3, 17, VIOLETA),
                    (29, 18, VIOLETA), (12, 5, VIOLETA), (20, 5, VIOLETA),
                    (7, 22, VIOLETA_ALTO), (25, 23, VIOLETA_ALTO)):
        px(im, x, y, c)

    im.save(os.path.join(BASE, 'Content', 'Buffs', 'QuemaduraVacioExterior.png'))
    return im




def vacio():
    """EL VACÍO EXTERIOR — el pago al dueño: la gran llama del vacío con
    el núcleo negro y la estrella de poder en el centro."""
    im = Image.new('RGBA', (32, 32), (0, 0, 0, 0))

    # LA GRAN LLAMA — el emblema (tres lenguas fundidas, más vertical y
    # serena que la quemadura: ES un poder, no un castigo)
    llama_negra(im, 16, 29, 2.9, lado=0.1)
    llama_negra(im, 10, 27, 2.0, lado=-0.7)
    llama_negra(im, 22, 27, 2.0, lado=0.7)

    # LA AURA — el resplandor violeta alrededor (el dueño arde en poder)
    for x, y, c in ((4, 16, VIOLETA_HONDO), (27, 16, VIOLETA_HONDO),
                    (8, 6, VIOLETA), (24, 6, VIOLETA), (16, 1, BLANCO),
                    (12, 2, VIOLETA_ALTO), (20, 2, VIOLETA_ALTO),
                    (3, 22, VIOLETA), (28, 22, VIOLETA)):
        px(im, x, y, c)

    # LA ESTRELLA DE PODER — el corazón del emblema: cuatro puntas
    # blancas sobre el núcleo negro (lo que el vacío le da al dueño)
    sx, sy = 16, 17
    for i in range(5):
        px(im, sx, sy - i, BLANCO if i < 3 else VIOLETA_ALTO)
        px(im, sx, sy + i, BLANCO if i < 3 else VIOLETA_ALTO)
        px(im, sx - i, sy, BLANCO if i < 3 else VIOLETA_ALTO)
        px(im, sx + i, sy, BLANCO if i < 3 else VIOLETA_ALTO)
    px(im, sx, sy, BLANCO)

    im.save(os.path.join(BASE, 'Content', 'Buffs', 'VacioExterior.png'))
    return im


def sombra5():
    """LA SOMBRA DE LA PÁGINA V — LA FAUCE GRITANDO (QA v4: «anchor the
    maw to the lower half — horizontal fang row hanging from a dark
    upper lip, red throat as a broad crescent, widen pupils»): LA BOCA
    ES UN ÓVALO HORIZONTAL con fila superior de colmillos COLGANDO y
    fila inferior ENTRELAZADA apuntando arriba, la garganta roja como
    media luna ancha, y los dos ojos rasgados con rendijas de 2 px."""
    im = Image.new('RGBA', (30, 30), (0, 0, 0, 0))
    cx = 15
    R = 13.5

    # LA CARA — el disco de masa con su borde VIOLETA SÓLIDO grueso
    for y in range(30):
        for x in range(30):
            d = math.hypot(x - cx, y - 15)
            if d < R:
                if d > R - 2.0:
                    px(im, x, y, VIOLETA_HONDO)
                else:
                    px(im, x, y, NEGRO_NUCLEO)

    # LA BOCA — el óvalo horizontal GRANDE (la boca abre en el vientre
    # de la cara, más ancha que alta: el grito)
    bx, by = 15.0, 19.5
    bw, bh = 10.0, 5.6
    for y in range(30):
        for x in range(30):
            k = math.hypot((x - bx) / bw, (y - by) / bh)
            if k < 1.0:
                if k > 0.86:
                    px(im, x, y, VIOLETA_HONDO)   # QA v5: EL BORDE de la boca
                    continue
                # LA GARGANTA — la media luna roja ANCHA abajo, negra arriba
                if y > by + 1.2 and math.hypot((x - bx) / (bw * 0.72), (y - by - 1.2) / (bh * 0.68)) < 1.0:
                    col = ROJO
                elif k > 0.8:
                    col = (60, 20, 34)
                else:
                    col = (24, 10, 18)
                px(im, x, y, col)
    # EL NÚCLEO MÁS BRILLANTE de la garganta (el fondo del grito)
    for dx, dy in ((-1, 2), (0, 2), (1, 2), (0, 3), (-1, 3), (1, 3), (0, 4)):
        px(im, bx + dx, by + dy, (240, 60, 50))

    # LA FILA SUPERIOR — CUATRO COLMILLOS GRANDES colgando del labio de
    # arriba (apuntando ABAJO, cada uno 3 px de base y 6 de largo)
    for i in range(4):
        fx = 9.5 + i * 3.7
        for s in range(6):
            t = s / 5
            y = 14 + int(t * 5.5)
            ancho = 1.4 * (1 - t * 0.55)
            for w in (-1, 0, 1):
                if abs(w) <= ancho:
                    px(im, fx + w, y, HUESO if t < 0.8 else (255, 250, 242))
        px(im, fx, 20, (255, 255, 250))       # LA PUNTA

    # LA FILA INFERIOR — TRES colmillos ENTRELAZADOS apuntando ARRIBA
    # (nacidos del labio de abajo, entre los de arriba: la boca que TRITURA)
    for i in range(3):
        fx = 11.2 + i * 3.7
        rim = i == 1     # QA v5: el del medio lleva su rim highlight
        for s in range(5):
            t = s / 4
            y = 25 - int(t * 4.5)
            ancho = 1.2 * (1 - t * 0.5)
            for w in (-1, 0, 1):
                if abs(w) <= ancho:
                    c = (255, 255, 250) if (rim and w == 0) else ((228, 220, 206) if t < 0.75 else (255, 250, 242))
                    px(im, fx + w, y, c)
        px(im, fx, 20, (255, 255, 250))

    # LOS DOS OJOS RASGADOS — almendras blancas GRANDES con la RENDIJA
    # DE 2 PX (la pupila del dragón, legible al 100%)
    for ex in (9, 21):
        ey = 6
        for dx in range(-4, 5):
            k = abs(dx) / 4
            if k > 1:
                continue
            alto = 2.3 * (1 - k * k)
            for dy in range(-int(alto) - 1, int(alto) + 2):
                if abs(dy) <= alto:
                    px(im, ex + dx, ey + dy, (255, 255, 255) if abs(dy) < alto * 0.7 else VIOLETA_ALTO)
        # LA RENDIJA VERTICAL DE 2 PX
        for dy in (-2, -1, 0, 1, 2):
            px(im, ex, ey + dy, NEGRO_NUCLEO)
            px(im, ex + 1, ey + dy, NEGRO_NUCLEO)
        px(im, ex - 4, ey, VIOLETA_ALTO)
        px(im, ex + 4, ey, VIOLETA_ALTO)

    im.save(os.path.join(BASE, 'Content', 'Weapons', 'Sombras', 'SombraDeLaPagina5.png'))
    return im




if __name__ == '__main__':
    a = quemadura()
    b = vacio()
    c = sombra5()
    print(f'QuemaduraVacioExterior {a.size} · VacioExterior {b.size} · SombraDeLaPagina5 {c.size}')
    # LA HOJA DE CONTACTO para QA
    hoja = Image.new('RGBA', (32 * 5 * 3 + 40, 32 * 5 + 20), (120, 120, 135, 255))
    for i, (im2, esc) in enumerate([(a, 5), (b, 5), (c, 32 / 30 * 5)]):
        w, h = int(im2.width * esc), int(im2.height * esc)
        fr = im2.resize((w, h), Image.NEAREST)
        hoja.paste(fr, (10 + i * (32 * 5 + 10), 10), fr)
    hoja.save('/tmp/vacio_qa.png')
    print('hoja de contacto /tmp/vacio_qa.png')
