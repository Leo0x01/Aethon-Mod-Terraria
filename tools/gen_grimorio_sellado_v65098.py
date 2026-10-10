#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.50.98 — EL ARTE DEL GRIMORIO SELLADO (4 texturas nuevas).

LA LETRA DEL USUARIO: «te dare el codigo para un arma nueva de prueba,
recuerda darcela al jugador, este sera el proyectil y su efecto» — el
arma es EL GRIMORIO SELLADO (contiene a Aethon), su disparo es el
FRAGMENTO DE AETHON (una astilla dorada del sello), su muerte es LA
APERTURA DE AETHON (la grieta se abre violentamente → se sella) y el
debuff es OBSERVADO (Aethon te está mirando).

Paleta de la FAMILIA del grimorio (coherencia con el arte de la casa:
tapa negra + marco dorado + iris violeta):
  · oro: brillante (255,218,94) / medio (212,164,62) / oscuro (140,102,36)
        / sombra (92,64,24)
  · violeta: núcleo (188,120,255) / medio (130,70,210) / oscuro (74,32,138)
  · vacío: (16,8,34) · blanco cálido: (255,248,228)

(1) GrimorioSellado.png 28×30 — el ícono del arma: el libro CERRADO y
    encadenado por un SELLO dorado con gema violeta (donde la familia
    tiene el ojo, el sellado tiene el sello: Aethon duerme dentro).
(2) FragmentoAethon.png 24×192 — STRIP de 8 frames 24×24 (vertical: el
    draw rota +PiOver2 para alinearla al vuelo): la astilla dorada con
    corazón de violeta que RESPIRA (brillo por frame) y un destello que
    la recorre.
(3) ExplosionAethon.png 48×384 — STRIP de 8 frames 48×48: LA APERTURA
    violenta (f0 cicatriz → f4 grieta abierta al máximo → f7 sellada en
    una línea de oro) — el mismo desgarro creciendo y sanándose, borde
    dentado determinista (la MISMA forma por fila en todos los frames).
(4) Observado.png 32×32 — el ícono del debuff: el ojo dorado que mira.

Determinista (semilla fija): misma corrida = mismo arte. Sin suavizado:
pixel-art puro.
"""
import math
import random

from PIL import Image

DEST = '/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod'

# ===== LA PALETA DE LA FAMILIA =====
ORO_BR = (255, 218, 94, 255)
ORO_MD = (212, 164, 62, 255)
ORO_OS = (140, 102, 36, 255)
ORO_SH = (92, 64, 24, 255)
VIO_BR = (188, 120, 255, 255)
VIO_MD = (130, 70, 210, 255)
VIO_OS = (74, 32, 138, 255)
TAPA = (24, 17, 38, 255)          # la tapa negra-violeta del grimorio
TAPA_HI = (41, 29, 62, 255)       # la tapa con luz
VACIO = (16, 8, 34, 255)
PAGINA = (232, 220, 180, 255)     # el canto de las páginas
PAGINA_SH = (178, 162, 122, 255)
BLANCO = (255, 248, 228, 255)
TRANSP = (0, 0, 0, 0)

_rng = random.Random(65098)   # misma corrida = mismo arte


def clamp(c):
    return tuple(max(0, min(255, int(round(v)))) for v in c)


def mezcla(a, b, t):
    return clamp((a[0] + (b[0] - a[0]) * t,
                  a[1] + (b[1] - a[1]) * t,
                  a[2] + (b[2] - a[2]) * t,
                  255))


def tela(w, h):
    return [[TRANSP for _ in range(w)] for _ in range(h)]


def px(t, x, y, c):
    if 0 <= x < len(t[0]) and 0 <= y < len(t):
        t[y][x] = c


def guardar(t, ruta):
    img = Image.new('RGBA', (len(t[0]), len(t)), (0, 0, 0, 0))
    img.putdata([c for fila in t for c in fila])
    img.save(ruta)
    print(f'  ✓ {ruta} ({img.size[0]}×{img.size[1]})')


# ==================================================================
# (1) GRIMORIO SELLADO — el ícono 28×30: el libro CERRADO + EL SELLO
# ==================================================================
def grimorio_sellado():
    W, H = 28, 30
    t = tela(W, H)

    # --- EL CUERPO DEL LIBRO (tapa negra) ---
    for y in range(1, 24):
        for x in range(1, 27):
            t[y][x] = TAPA
    # luz de la tapa (esquina sup-izq, la que mira al jugador)
    for y in range(2, 12):
        for x in range(2, 14):
            if (x + y) < 16:
                t[y][x] = TAPA_HI
    # el lomo (izquierda): banda oscura + costura dorada
    for y in range(1, 24):
        t[y][1] = VACIO
        t[y][2] = TAPA
    for y in range(3, 22, 4):                 # remaches del lomo
        t[y][2] = ORO_MD
        t[y][3] = ORO_OS

    # --- EL MARCO DORADO (1px alrededor de la tapa) ---
    for x in range(1, 27):
        t[1][x] = ORO_OS
        t[23][x] = ORO_OS
    for y in range(1, 24):
        t[y][26] = ORO_OS
    t[1][1] = ORO_BR; t[1][26] = ORO_BR          # las 4 esquinas
    t[23][1] = ORO_BR; t[23][26] = ORO_BR

    # --- LAS ESTRELLAS DE LA TAPA (el motivo de la familia) ---
    def estrella(cx, cy):
        px(t, cx, cy, ORO_BR)
        px(t, cx - 1, cy, ORO_MD); px(t, cx + 1, cy, ORO_MD)
        px(t, cx, cy - 1, ORO_MD); px(t, cx, cy + 1, ORO_MD)
    estrella(7, 6)
    estrella(21, 6)
    estrella(6, 19)
    estrella(22, 20)

    # --- EL CANTO DE LAS PÁGINAS (abajo) ---
    for y in range(24, 28):
        for x in range(3, 25):
            t[y][x] = PAGINA if (y - 24) % 2 == 0 else PAGINA_SH
    for x in range(3, 25):                     # las líneas de las hojas
        t[25][x] = PAGINA_SH
        t[27][x] = PAGINA_SH
    t[24][2] = ORO_SH; t[24][25] = ORO_SH      # los bordes del canto
    t[27][3] = PAGINA_SH; t[27][24] = PAGINA_SH

    # --- EL SELLO (el corazón del ícono): disco dorado + gema violeta ---
    cx, cy, r = 14, 12, 5
    for y in range(cy - r - 1, cy + r + 2):
        for x in range(cx - r - 1, cx + r + 2):
            d = math.hypot(x - cx, y - cy)
            if d <= r + 0.5:                       # el disco
                if d > r - 0.5:
                    px(t, x, y, ORO_BR)           # el aro exterior
                elif d > r - 2.0:
                    px(t, x, y, ORO_MD)
                else:
                    px(t, x, y, ORO_OS)           # el cuenco interior
    # la gema violeta en el centro del sello
    for y in range(cy - 2, cy + 3):
        for x in range(cx - 2, cx + 3):
            d = math.hypot(x - cx, y - cy)
            if d <= 2.2:
                px(t, x, y, VIO_MD if d > 1.2 else VIO_BR)
    px(t, cx, cy, BLANCO)                          # el brillo de la gema
    # los 4 remaches del sello
    for dx, dy in ((0, -r), (0, r), (-r, 0), (r, 0)):
        px(t, cx + dx, cy + dy, ORO_BR)

    # --- LAS CORREAS (del borde del libro al sello: cerrado por arriba
    #     y por abajo — el sello las tensa) ---
    for y in range(1, 7):
        px(t, 13, y, ORO_MD); px(t, 14, y, ORO_OS); px(t, 15, y, ORO_MD)
    for y in range(18, 24):
        px(t, 13, y, ORO_MD); px(t, 14, y, ORO_OS); px(t, 15, y, ORO_MD)
    # la correa es interrumpida por el sello (el disco la tapa) — ya está

    guardar(t, f'{DEST}/Content/Weapons/GrimorioSellado.png')


# ==================================================================
# (2) FRAGMENTO DE AETHON — strip 24×192 (8 frames 24×24, eje VERTICAL:
#     el draw rota +PiOver2, la astilla apunta donde vuela)
# ==================================================================
def fragmento_aethon():
    FW, FH = 24, 24
    N = 8
    t = tela(FW, N * FH)

    # la SILUETA constante (la misma en los 8 frames — que no tiemble):
    # una astilla vertical: punta (12,2) → panza (8..16, y9..16) → punta
    # (12,21), con una espora al lado derecho (y12..15 hasta x=18)
    silueta = [[False for _ in range(FW)] for _ in range(FH)]
    for y in range(FH):
        for x in range(FW):
            dentro = False
            if 2 <= y <= 21:
                if y <= 9:        # de la punta a la panza (abririendo)
                    w = 1.2 + (y - 2) * (5.0 / 7.0)
                    dentro = abs(x - 12) <= w
                elif y <= 16:     # la panza
                    dentro = abs(x - 12) <= 5.0
                    if 12 <= y <= 15:               # la espora
                        dentro = dentro or (12 <= x <= 18 and abs((y - 13.5)) <= 1.6 - (x - 12) * 0.18)
                else:            # cerrando hacia la punta
                    w = 5.0 - (y - 16) * (4.0 / 5.0)
                    dentro = abs(x - 12) <= max(0.6, w)
            silueta[y][x] = dentro

    # la respiración por frame (la astilla late): brillo 0.68 → 1.08
    brillo = [0.74, 0.87, 1.00, 1.08, 1.00, 0.88, 0.76, 0.68]
    # el destello que RECORRE la astilla (frame → posición del brillo)
    glint_f = [(10, 7), (11, 9), (12, 11), (13, 13), (12, 15), (11, 17), None, None]

    for f in range(N):
        base = f * FH
        b = brillo[f]
        for y in range(FH):
            for x in range(FW):
                if not silueta[y][x]:
                    continue
                # el borde: sombra de oro
                es_borde = (not (0 <= y - 1 < FH) or not silueta[y - 1][x]) or \
                           (not (0 <= y + 1 < FH) or not silueta[y + 1][x]) or \
                           (not (0 <= x - 1 < FW) or not silueta[y][x - 1]) or \
                           (not (0 <= x + 1 < FW) or not silueta[y][x + 1])
                # el corazón de violeta (columna central)
                nucleo = abs(x - 12) <= 1.3 and 5 <= y <= 19
                if es_borde:
                    c = mezcla(ORO_OS, ORO_MD, 0.5 * b)
                elif nucleo:
                    c = mezcla(VIO_MD, VIO_BR, 0.6 * b)
                    if 10 <= y <= 14:
                        c = mezcla(VIO_BR, BLANCO, 0.35 * b)
                elif x <= 12:      # la faceta izquierda (más clara)
                    c = mezcla(ORO_MD, ORO_BR, 0.55 * b)
                else:              # la faceta derecha
                    c = mezcla(ORO_OS, ORO_MD, 0.75 * b)
                # el facetado diagonal (2 arcos de sombra para que se lea
                # cristal, no mancha)
                if not es_borde and not nucleo:
                    if (x + y) % 7 == 0:
                        c = mezcla(c, VACIO, 0.28)
                px(t, x, base + y, c)
        # el destello viajero (2px blancos sobre la faceta izquierda)
        g = glint_f[f]
        if g is not None:
            gx, gy = g
            px(t, gx, base + gy, BLANCO)
            px(t, gx + 1, base + gy, mezcla(BLANCO, ORO_BR, 0.4))
            px(t, gx, base + gy + 1, mezcla(BLANCO, ORO_BR, 0.6))

    guardar(t, f'{DEST}/Content/Projectiles/Aethon/FragmentoAethon.png')


# ==================================================================
# (3) LA APERTURA DE AETHON — strip 48×384 (8 frames 48×48):
#     el desgarro se ABRE VIOLENTAMENTE → se SELLA
# ==================================================================
def explosion_aethon():
    FW, FH = 48, 48
    N = 8
    t = tela(FW, N * FH)
    cx = 24                                   # el eje del desgarro

    # la anchura media por frame (f0 cicatriz → f4 MÁXIMO → f7 sellada)
    hw = [1.0, 5.0, 11.0, 17.0, 20.0, 13.0, 6.5, 1.6]
    # el borde dentado DETERMINISTA: la MISMA forma por fila en todos
    # los frames (el desgarro de f4 es el de f0 crecido — no otro)
    rng = random.Random(98650)                # semilla fija
    jag = [rng.uniform(-2.2, 2.2) for _ in range(FH)]

    for f in range(N):
        base = f * FH
        w = hw[f]
        for y in range(FH):
            # el desgarro vive en y=4..43; se estrecha en los extremos
            if y < 4 or y > 43:
                continue
            tapa = 1.0
            if y < 10:
                tapa = 0.35 + 0.65 * (y - 4) / 6.0
            elif y > 37:
                tapa = 0.35 + 0.65 * (43 - y) / 6.0
            fila_hw = max(0.8, w * tapa + jag[y])
            for x in range(FW):
                d = abs(x - cx)
                if d > fila_hw + 2.5:
                    continue
                if d > fila_hw + 1.2:                       # el borde oscuro
                    px(t, x, base + y, mezcla(ORO_SH, VACIO, 0.5))
                elif d > fila_hw - 0.8:                     # el aro de oro
                    px(t, x, base + y, ORO_BR if (y // 3) % 2 == 0 else ORO_MD)
                elif d > fila_hw - 2.2:                     # la falda
                    px(t, x, base + y, mezcla(ORO_OS, ORO_MD, 0.5))
                else:
                    # el interior: violeta que se hunde hacia el centro
                    prof = 1.0 - (d / max(1.0, fila_hw - 2.2))
                    if d <= 1.3:                            # la costura central
                        c = mezcla(VIO_BR, BLANCO, 0.55)
                    else:
                        c = mezcla(VIO_BR, VACIO, min(1.0, prof * 1.15))
                    px(t, x, base + y, c)
        # en la APERTURA MÁXIMA (f3, f4): chispas de oro fuera del borde
        if f in (3, 4):
            chispas = [(cx - 26, 8), (cx + 25, 12), (cx - 24, 30), (cx + 27, 34), (cx + 23, 22)]
            for i, (sx, sy) in enumerate(chispas):
                px(t, sx, base + sy, ORO_BR)
                px(t, sx + (1 if i % 2 == 0 else -1), base + sy - 1, ORO_MD)
        # en el SELLO final (f7): la cicatriz es una LÍNEA de oro
        if f == 7:
            for y in range(4, 44):
                d = max(0.8, hw[f] * (0.35 + 0.65 * min(y - 4, 43 - y) / 6.0) + jag[y])
                for x in range(FW):
                    if abs(x - cx) <= d:
                        px(t, x, base + y, ORO_BR if abs(x - cx) < 1.0 else ORO_OS)
            # el último destello del sello: 2 puntos blancos
            px(t, cx, base + 10, BLANCO)
            px(t, cx, base + 36, BLANCO)

    guardar(t, f'{DEST}/Content/Projectiles/Aethon/ExplosionAethon.png')


# ==================================================================
# (4) OBSERVADO — el ícono del debuff 32×32: el ojo dorado que MIRA
# ==================================================================
def observado():
    W, H = 32, 32
    t = tela(W, H)
    cx = 16

    # los párpados: dos arcos de oro (el ojo ABIERTO — Aethon mira)
    for x in range(4, 28):
        u = (x - 4) / 23.0
        up = round(16 - 9.0 * math.sin(math.pi * u))
        lo = round(16 + 7.0 * math.sin(math.pi * u))
        for y in range(up, lo + 1):
            if y == up or y == up + 1 or y == lo or y == lo - 1:
                c = ORO_BR if (x // 4) % 2 == 0 else ORO_MD
                px(t, x, y, c)
            else:
                px(t, x, y, mezcla(VACIO, VIO_OS, 0.35))

    # el iris: aro de oro + violeta + pupila negra + brillo
    for y in range(16 - 6, 16 + 7):
        for x in range(cx - 6, cx + 7):
            d = math.hypot(x - cx, y - 16)
            if d <= 5.6:
                if d > 4.6:
                    px(t, x, y, ORO_MD)
                elif d > 1.6:
                    px(t, x, y, VIO_MD if d > 3.0 else VIO_BR)
                else:
                    px(t, x, y, VACIO)
    px(t, cx - 1, 15, BLANCO)                  # el brillo de la pupila

    # las pestañas-rayo (arriba y abajo): la mirada sale del ojo
    for bx, diry in ((8, -1), (13, -1), (20, -1), (25, -1), (10, 1), (22, 1)):
        u = (bx - 4) / 23.0
        if diry < 0:
            y0 = round(16 - 9.0 * math.sin(math.pi * u))
            for k in (1, 2, 3):
                px(t, bx + (k // 2) * (1 if bx < 16 else -1), y0 - k,
                   ORO_BR if k == 1 else ORO_MD)
        else:
            y0 = round(16 + 7.0 * math.sin(math.pi * u))
            for k in (1, 2):
                px(t, bx + (k // 2) * (1 if bx < 16 else -1), y0 + k,
                   ORO_BR if k == 1 else ORO_MD)

    # las motas de la observación (3 chispas sueltas alrededor)
    for sx, sy in ((5, 6), (27, 7), (26, 25)):
        px(t, sx, sy, ORO_BR)

    guardar(t, f'{DEST}/Content/Buffs/Observado.png')


if __name__ == '__main__':
    print('=== v6.50.98 — EL ARTE DEL GRIMORIO SELLADO ===')
    import os
    os.makedirs(f'{DEST}/Content/Projectiles/Aethon', exist_ok=True)
    grimorio_sellado()
    fragmento_aethon()
    explosion_aethon()
    observado()

    # LA HOJA DE PREVISTA (×6, vecino más cercano) para el QA visual
    from PIL import Image as I
    piezas = [
        ('Weapons/GrimorioSellado.png', 0, 0),
        ('Buffs/Observado.png', 200, 0),
        ('Projectiles/Aethon/FragmentoAethon.png', 0, 200),
        ('Projectiles/Aethon/ExplosionAethon.png', 90, 200),
    ]
    hoja = I.new('RGBA', (560, 420), (24, 20, 34, 255))
    for ruta, x, y in piezas:
        img = I.open(f'{DEST}/Content/{ruta}')
        img = img.resize((img.size[0] * 6, img.size[1] * 6), I.NEAREST)
        hoja.paste(img, (x, y), img)
    hoja.save('/tmp/preview_v65098.png')
    print('  ✓ /tmp/preview_v65098.png (hoja ×6 para el QA)')
