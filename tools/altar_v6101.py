#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
altar_v6101.py — v6.101 — EL NUEVO ALTAR DE OFRENDAS (la letra del
usuario: «sobre el altar, intenta recrear un nuevo sprite para el altar
con una buena animacion decente, puedes tomar el sprite actual del
altar como base»).

TOMA EL SPRITE ACTUAL COMO BASE (la MISMA composición del v6.100 — el
arco gótico, las dos velas, la cámara del vórtice, el pedestal con sus
runas — y la MISMA paleta exacta muestreada pixel a pixel) y lo recrea
con SEIS marcos de animación de verdad:

  · EL VÓRTICE QUE GIRA — tres brazos espirales morados rotando 60°
    por marco (el ciclo completo a 6 marcos: el corazón del altar ya no
    «late», REMOLINA), con la magenta incandescente en el centro.
  · LAS DOS VELAS EN CONTRAFASE — tres alturas de llama alternándose
    entre velas, puntas cruzadas, núcleo incandescente (255,238,170) y
    el halo cálido que respira alrededor.
  · LAS RUNAS EN PERSECUCIÓN — la banda inferior prende sus glifos uno
    tras otro (la luz corre por el borde del pedestal marco a marco).
  · LA CHISPA DE CONSAGRACIÓN — un destello que SUBE por la cámara del
    vórtice (nace en la base del pedestal y asciende un marco tras
    otro hasta el arco).
  · EL DOMO QUE RESPIRA — la claraboya teal de la cumbre pulsa.
  · LOS FAROLES ALTERNOS — las esquinas del pedestal prenden por turnos.

REGLA PERMANENTE DE LA CASA: la transparencia es NATIVA (el lienzo
nace alfa=0 y solo el ARTE pinta) — jamás post-proceso.

Salidas:
  Content/Tiles/AltarOfrendasTile.png   (34×300 — 6 marcos de 50 px)
  Content/Items/Altar/AltarOfrendasItem.png  (34×50 — el marco 0)
"""
from PIL import Image
import math
import os

W, H = 34, 50
MARCOS = 6

# --- LA PALETA EXACTA (muestreada del sprite v6.100) ---
NEGRO_FONDO = (8, 6, 20)       # la piedra más honda
PIEDRA      = (10, 10, 40)     # el cuerpo
PIEDRA_ALTA = (16, 26, 74)     # la cara iluminada
TEAL_TRABAJO = (24, 48, 120)   # la sombra del metal
TEAL        = (48, 78, 150)    # el trim del altar
TEAL_CLARO  = (96, 128, 255)   # el brillo del trim
TEAL_LINTERNA = (90, 220, 180) # los faroles del pedestal
CIAN_CHISPA = (9, 201, 242)    # la chispa viva
TEAL_RUNA   = (72, 216, 168)   # las runas

MORADO      = (150, 62, 235)   # el vórtice
MORADO_HONDO= (168, 44, 150)   # los brazos profundos
MAGENTA     = (255, 120, 224)  # el corazón incandescente
MORADO_OSCURO = (60, 24, 110)  # el valle entre brazos

LLAMA_PUNTA = (255, 238, 170)  # la punta incandescente
LLAMA       = (255, 200, 80)   # la llama viva
LLAMA_MEDIA = (216, 144, 48)   # la base de la llama
PORTALLAMAS = (160, 88, 24)    # el recipiente
VELA_CUERPO = (226, 220, 200)  # la cera de hueso
VELA_SOMBRA = (150, 140, 125)

CX = 16.5   # el eje del altar


def px(im, x, y, c):
    if 0 <= x < W and 0 <= y < H:
        im.putpixel((x, y), c)


def dentro(x, y):
    """LA SILUETA del altar (la composición del sprite v6.100): el arco
    gótico arriba, el cuerpo con pilares, el pedestal ancho abajo."""
    if y >= 41:                       # EL PEDESTAL (ancho completo)
        return 0 <= x < W
    if y >= 32:                       # LA REPISA DE LAS VELAS
        return 1 <= x < W - 1
    if y >= 10:                       # LA CÁMARA (pilares a los lados)
        return 5 <= x <= 29
    if y >= 5:                        # LOS HOMBROS DEL ARCO
        return 7 + (10 - y) // 2 <= x <= 26 - (10 - y) // 2
    # LA CÚPULA (filas 0-4)
    half = 5 + y          # la cúpula se abre hacia abajo
    return CX - half <= x <= CX + half


def cuerpo_piedra(im):
    """EL CUERPO ESTÁTICO — la piedra con su sombreado y su trim teal."""
    for y in range(H):
        for x in range(W):
            if not dentro(x, y):
                continue
            c = PIEDRA
            # LA CARA ILUMINADA: el borde superior-izquierdo de cada masa
            if not dentro(x, y - 1) or not dentro(x - 1, y):
                c = PIEDRA_ALTA
            elif not dentro(x + 1, y) or not dentro(x, y + 1):
                c = TEAL_TRABAJO if y >= 32 else NEGRO_FONDO
            # EL TRIM TEAL: las líneas maestras del altar
            if y in (9, 10, 31, 32, 41, 45) and dentro(x, y + 1) and dentro(x, y - 1):
                c = TEAL if x % 2 == 0 else TEAL_TRABAJO
            px(im, x, y, c)


def domo(im, f):
    """LA CÚPULA — la claraboya teal respirando (el ciclo 0→1→0 en 6
    marcos) y su cruz de piedra."""
    fase = f / MARCOS
    brillo = 0.65 + 0.35 * math.sin(fase * 2 * math.pi)
    for y in range(1, 4):
        for x in range(W):
            if not dentro(x, y):
                continue
            dx = (x - CX) / 6.5
            dy = (y - 1.6) / 2.2
            d = dx * dx + dy * dy
            if d < 0.75:
                r, g, b = TEAL_CLARO
                k = 1 - d / 0.75
                col = (int(r * (0.6 + 0.4 * brillo) * k + 20 * (1 - k)),
                       int(g * (0.6 + 0.4 * brillo) * k + 40 * (1 - k)),
                       int(b * (0.6 + 0.4 * brillo) * k + 60 * (1 - k)))
                px(im, x, y, col)
            elif d < 1.05:
                px(im, x, y, TEAL if brillo > 0.8 else PIEDRA_ALTA)
    # LA CRUZ de piedra que divide la claraboya
    px(im, int(CX), 1, PIEDRA)
    px(im, int(CX), 2, PIEDRA)


def vortex(im, f):
    """EL VÓRTICE QUE GIRA — tres brazos espirales morados rotando 60°
    por marco (el ciclo completo), el corazón magenta latiendo y los
    valles morados hondeando entre brazos. QA VLM v1: «the arm
    positions shift subtly — strengthen the rotation»: LOS BRAZOS SON
    GORDOS (bandas anchas) y el paso de 60° por marco SE VE."""
    cx, cy = CX, 20.5
    rx, ry = 7.6, 10.4
    giro = (f / MARCOS) * 2 * math.pi
    latido = 0.5 + 0.5 * math.sin(f / MARCOS * 2 * math.pi)
    for y in range(10, 31):
        for x in range(8, 26):
            if not dentro(x, y):
                continue
            # LA PIEDRA DE LA CÁMARA: el anillo exterior queda piedra
            dx = (x - cx) / rx
            dy = (y - cy) / ry
            d = math.sqrt(dx * dx + dy * dy)
            if d > 1.18:
                continue                     # el marco de piedra vive
            ang = math.atan2(dy, dx)
            # EL BRAZO: la espiral de tres brazos (distorsión logarítmica)
            espiral = (ang * 3 + d * 7.5 - giro * 3) % (2 * math.pi)
            brazo = math.cos(espiral)
            if d < 0.26:
                # EL CORAZÓN — magenta incandescente, PEQUEÑO y brillante
                k = 1 - d / 0.26
                col = (int(200 + 55 * k),
                       int(60 + 60 * latido * k),
                       int(170 + 54 * k))
                px(im, x, y, col)
            elif brazo > 0.30:
                # EL BRAZO GORDO — banda ANCHA de morado pleno
                k = min(1, (brazo - 0.30) / 0.70)
                if d > 0.88:
                    col = MORADO_HONDO
                else:
                    col = (int(MORADO[0] - 24 * (1 - k)),
                           int(MORADO[1] - 16 * (1 - k)),
                           int(MORADO[2] - 8 * (1 - k)))
                px(im, x, y, col)
            elif brazo > 0.05 and d < 1.0:
                # EL FILO DEL BRAZO — el borde vivo
                px(im, x, y, MORADO if d > 0.45 else MORADO_HONDO)
            elif d > 0.38:
                # EL VALLE — la oscuridad entre brazos
                k = (d - 0.38) / 0.8
                col = (int(MORADO_OSCURO[0] * (1 - k * 0.4)),
                       int(MORADO_OSCURO[1] * (1 - k * 0.3)),
                       int(MORADO_OSCURO[2] * (1 - k * 0.2)))
                px(im, x, y, col)
            else:
                px(im, x, y, MORADO_OSCURO)


def chispa(im, f):
    """LA CHISPA DE CONSAGRACIÓN — el destello que SUBE por la cámara
    (CONTENIDA en la cámara, QA VLM v1: «the sparkle escapes the
    chamber»): nace al fondo y asciende hasta la cumbre de la cámara,
    con su estela tenue que NO tapa los brazos del vórtice."""
    t = f / (MARCOS - 1)
    y = int(29 - t * 16)              # DENTRO de la cámara (13..29)
    x = int(CX + math.sin(t * 6.28) * 3)
    x = max(10, min(23, x))           # nunca fuera de la cámara
    for dy in range(-1, 2):
        for dx in range(-1, 2):
            if abs(dx) + abs(dy) <= 1 and dentro(x + dx, y + dy):
                if dx == 0 and dy == 0:
                    # QA v2: «1px smaller in later frames — a mote, not
                    # a gem»: arriba viaja SOLO (un destello diminuto)
                    px(im, x, y, (255, 255, 242) if t < 0.6 else (210, 240, 238))
                elif t < 0.6:                       # la cruz solo al nacer
                    px(im, x + dx, y + dy, CIAN_CHISPA)
    # LA ESTELA — el rastro tenue que NO destruye los brazos
    if f > 0:
        y_prev = int(29 - (f - 1) / (MARCOS - 1) * 16)
        for yy in range(y + 1, min(y_prev + 1, 30)):
            ex = int(CX + math.sin((29 - yy) / 16 * 6.28) * 3 * 0.7)
            ex = max(10, min(23, ex))
            r, g, b, a = im.getpixel((ex, yy))
            # SOLO sobre los valles oscuros (los brazos siguen vivos)
            if r < 90 and b < 200:
                px(im, ex, yy, (44, 130, 108))


def velas(im, f):
    """LAS DOS VELAS — en CONTRAFASE: tres alturas de llama alternando
    entre velas, puntas cruzadas, núcleo incandescente, halo cálido y
    el portallamas constante. LA LETRA: «el portallamas marrón
    CONSTANTE (la vela no baila: la LLAMA sí)». Llamas GORDAS de
    verdad (2-3 px de cuerpo, como el sprite base)."""
    # LA CERA (estática): columnas de hueso LIMPIAS (QA VLM v1: «noisy
    # wax clusters» — cera sólida con UNA sombra lateral)
    for vx in (6, 27):
        for y in range(37, 41):
            px(im, vx, y, VELA_CUERPO)
            px(im, vx + 1, y, VELA_SOMBRA)
        px(im, vx, 36, VELA_SOMBRA)
        px(im, vx + 1, 36, VELA_SOMBRA)
        # EL PORTALLAMAS (constante): el aro marrón que sostiene la llama
        px(im, vx - 1, 36, PORTALLAMAS)
        px(im, vx + 2, 36, PORTALLAMAS)
        px(im, vx - 1, 37, PORTALLAMAS)
        px(im, vx + 2, 37, PORTALLAMAS)

    # LAS LLAMAS: la izquierda en la fase f, la derecha en la CONTRAFASE
    fases = (f, (f + 3) % MARCOS)
    for vx, fase in zip((6, 27), fases):
        # LA ALTURA (QA v2: «one cleaner flicker loop» — el ciclo parejo:
        # baja↔alta con UN punto álgido por vela en el ciclo de 6)
        alta = (3, 4, 5, 4, 3, 4)[fase]
        # LA PUNTA CRUZA: hacia el centro en los marcos calientes
        lado = -1 if vx == 6 else 1
        cruza = (0, 1, 0, 1, 1, 2)[fase]
        # EL CUERPO DE LA LLAMA — gorda: 3 px en la base, 2 arriba
        for i in range(alta):
            y = 35 - i
            if i == 0:
                col, col2 = LLAMA_MEDIA, LLAMA_MEDIA
            elif i < alta - 1:
                col, col2 = LLAMA, LLAMA_MEDIA if i == 1 else LLAMA
            else:
                col, col2 = LLAMA_PUNTA, LLAMA
            px(im, vx, y, col)
            if i < alta - 1:
                px(im, vx + 1, y, col2)
                if i == 0:
                    px(im, vx - 1, y, LLAMA_MEDIA)   # la base ancha
        # EL CRUCE de la punta (la llama se inclina hacia dentro)
        if cruza > 0:
            tipY = 35 - alta + 1
            for c in range(cruza):
                px(im, vx + 1 + lado * (c + 1), tipY + c,
                   LLAMA_PUNTA if c == cruza - 1 else LLAMA)
        # EL NÚCLEO INCANDESCENTE en los marcos calientes
        if fase in (3, 5):
            px(im, vx, 33, LLAMA_PUNTA)
            px(im, vx, 34, (255, 226, 120))
            px(im, vx + 1, 34, LLAMA_PUNTA)
        # EL HALO CÁLIDO que respira alrededor
        if fase >= 3:
            for dx, dy in ((-2, 0), (3, 0), (-1, -2), (2, -2), (0, -4), (1, -3)):
                hx, hy = vx + dx, 34 + dy
                if dentro(hx, hy):
                    r, g, b, a = im.getpixel((hx, hy))
                    if a < 10 or r < 60:     # solo sobre la piedra
                        px(im, hx, hy, (52, 34, 16))


def pedestal(im, f):
    """EL PEDESTAL — las runas en PERSECUCIÓN (la luz corre por la
    banda), los faroles alternos, LAS BANDAS TEAL del sprite base y el
    borde inferior hondeando."""
    # LAS BANDAS TEAL del pedestal — patrón FIJO que PULSA de brillo
    # (QA VLM v1: «positional jitter reads as noise — runes should
    # PULSE brightness»), con los faroles de las esquinas por turnos.
    izq_on = f % 2 == 0
    pulso = 0.5 + 0.5 * math.sin(f / MARCOS * 2 * math.pi)
    for x in range(W):
        brillo = 0.55 + 0.45 * pulso
        # EL PATRÓN DE LA BANDA: glifos fijos cada 3 px
        if x % 3 == 0:
            col = (int(90 * brillo + 40), int(220 * brillo + 20), int(180 * brillo + 40))
        else:
            col = (int(TEAL[0] * brillo), int(TEAL[1] * brillo), int(TEAL[2] * brillo))
        px(im, x, 43, col)
        px(im, x, 44, PIEDRA_ALTA if x % 3 else TEAL_TRABAJO)
    # LOS FAROLES ALTERNOS (las esquinas BRILLAN por turnos)
    for x in range(1, 6):
        px(im, x, 43, TEAL_LINTERNA if izq_on else TEAL_TRABAJO)
        px(im, x, 44, TEAL_LINTERNA if izq_on else PIEDRA_ALTA)
    for x in range(W - 6, W - 1):
        px(im, x, 43, TEAL_LINTERNA if not izq_on else TEAL_TRABAJO)
        px(im, x, 44, TEAL_LINTERNA if not izq_on else PIEDRA_ALTA)
    # LA BANDA DE RUNAS — PULSA (cada glifo sube y baja de brillo con
    # SU fase fija; los DOS brillantes viajan LENTO, uno por marco)
    for x in range(W):
        fase = (x / W) * 2 * math.pi
        k = 0.35 + 0.65 * (0.5 + 0.5 * math.sin(fase + f / MARCOS * 2 * math.pi))
        if (x - f * 2) % 17 == 0:
            px(im, x, 47, CIAN_CHISPA)          # EL GLIFO QUE CORRE lento
        else:
            px(im, x, 47, (int(TEAL_RUNA[0] * k),
                           int(TEAL_RUNA[1] * k),
                           int(TEAL_RUNA[2] * k)))
    # EL BORDE INFERIOR hondeando (la fila 49 entera viva)
    for x in range(W):
        k = (x + f) % 4
        if k == 0:
            px(im, x, 49, (28, 58, 140))
        elif k == 2:
            px(im, x, 49, TEAL)


def generar():
    base = os.path.join(os.path.dirname(__file__), '..', 'AethonMod')
    hoja = Image.new('RGBA', (W, H * MARCOS), (0, 0, 0, 0))
    for f in range(MARCOS):
        marco = Image.new('RGBA', (W, H), (0, 0, 0, 0))
        cuerpo_piedra(marco)
        domo(marco, f)
        vortex(marco, f)
        chispa(marco, f)
        velas(marco, f)
        pedestal(marco, f)
        hoja.paste(marco, (0, f * H))
    hoja.save(os.path.join(base, 'Content', 'Tiles', 'AltarOfrendasTile.png'))
    # EL ÍTEM: el marco 0
    marco0 = hoja.crop((0, 0, W, H))
    marco0.save(os.path.join(base, 'Content', 'Items', 'Altar', 'AltarOfrendasItem.png'))

    # --- LA VERIFICACIÓN DE LA CASA ---
    im = Image.open(os.path.join(base, 'Content', 'Tiles', 'AltarOfrendasTile.png')).convert('RGBA')
    assert im.size == (W, H * MARCOS), f'tamaño roto: {im.size}'
    pxs = im.load()
    for f in range(MARCOS):
        ultima = -1
        for y in range(H):
            for x in range(W):
                if pxs[x, y + f * H][3] > 10:
                    ultima = y                     # sigue: queremos la ÚLTIMA
        print(f'  marco {f}: última fila con arte = {ultima}')
        assert ultima == H - 1, f'¡el marco {f} flota (fila {ultima})!'
    print(f'ALTAR NUEVO: {im.size[0]}x{im.size[1]} · 6 marcos · arte hasta la ÚLTIMA fila · transparencia nativa')


if __name__ == '__main__':
    generar()
