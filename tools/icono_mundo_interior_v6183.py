#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.183 — EL ICONO ÉPICO DEL MUNDO INTERIOR (la letra: «hacer que el
icono del buff de Mundo Interior se vea mas epico y que tenga un
marco»).

LA AUTOPSIA (VLM sobre el icono de la v6.182 — 3/10 de épico):
legible pero apagado: sin marco, sin contraste, sin símbolo, sin aura.

LA V2 — EL PAISAJE DEL DIOS CON SU MARCO:
  · EL ECLIPSE (el símbolo): el disco negro con SU CORONA dorada
    brillando en el cielo del interior (EL ECLIPSE PERMANENTE morado
    — el lore del propio interior: el dios sellado tapa el sol).
  · EL CIELO: el degradado profundo del eclipse (casi negro arriba →
    violeta en el horizonte).
  · LOS RAYOS DE LA CORONA: los picos dorados que sangran del disco.
  · LA BRUMA: tres bandas suaves que respiran (más presentes que
    antes — el cuerpo del dios ES niebla).
  · LOS TENTÁCULOS: cuatro siluetas con FILO ILUMINADO (la luz del
    eclipse les dibuja el borde) subiendo del llano.
  · EL MARCO (la letra): la orla de la casa — borde ORO de 2 px con
    su contorno oscuro y las CUATRO ESQUINAS rúnicas (los picos de
    diamante de las runas solares).
SALIDA: AethonMod/Content/Buffs/MundoInteriorBuff.png (32×32) + el
reporte ASCII.
"""
import hashlib

from PIL import Image

W = H = 32
SALIDA = '/home/z/my-project/mod/AethonMod/Content/Buffs/MundoInteriorBuff.png'

# ── LA PALETA DEL ECLIPSE ────────────────────────────────────────────
CIELO_ALTO = (16, 8, 30)
CIELO_MEDIO = (34, 16, 56)
CIELO_BAJO = (58, 28, 88)
BRUMA_CLARA = (108, 66, 148)
BRUMA_TENUE = (74, 44, 108)
LLANO = (30, 16, 46)
LLANO_FILO = (86, 52, 112)
TENTACULO = (20, 11, 34)
TENT_FILO = (196, 138, 235)
DISCO = (8, 4, 16)
CORONA = (255, 208, 96)
CORONA_VIVO = (255, 238, 170)
ORO_MARCO = (232, 176, 78)
ORO_OSC = (170, 120, 44)
OUT = (18, 10, 28)


def px(im, x, y, c):
    if 0 <= x < W and 0 <= y < H:
        im.putpixel((x, y), (*c, 255))


def dentro(x, y):
    return 0 <= x < W and 0 <= y < H


im = Image.new('RGBA', (W, H), (0, 0, 0, 0))

# ══ 1. EL CIELO DEL ECLIPSE (el degradado profundo) ═══════════════
for y in range(H):
    v = y / (H - 1)
    if v < 0.55:
        k = v / 0.55
        c = tuple(int(a + (b - a) * k) for a, b in
                  zip(CIELO_ALTO, CIELO_MEDIO))
    else:
        k = (v - 0.55) / 0.45
        c = tuple(int(a + (b - a) * k) for a, b in
                  zip(CIELO_MEDIO, CIELO_BAJO))
    for x in range(W):
        px(im, x, y, c)

# ══ 2. EL ECLIPSE: el disco negro con SU CORONA ══════════════════
cx, cy, r = 16, 12, 7
# LA CORONA (los rayos dorados que sangran del disco)
for ang_i in range(24):
    import math
    ang = ang_i * (2 * math.pi / 24)
    largo = r + 3 + (2.6 if ang_i % 2 == 0 else 0)
    for d in range(int(r * 0.8), int(largo)):
        x = cx + math.cos(ang) * d
        y = cy + math.sin(ang) * d
        if not dentro(round(x), round(y)):
            continue
        # el pico: se afila hacia afuera
        fino = d / largo
        col = CORONA if fino < 0.72 else CORONA_VIVO
        if fino > 0.85 and ang_i % 2 == 1:
            continue   # los picos impares mueren antes (la corona picada)
        px(im, round(x), round(y), col)
# EL DISCO (el sol muerto — negro absoluto con su filo vivo)
for y in range(cy - r - 1, cy + r + 2):
    for x in range(cx - r - 1, cx + r + 2):
        d2 = (x - cx) ** 2 + (y - cy) ** 2
        if d2 <= r * r:
            px(im, x, y, DISCO)
        elif d2 <= (r + 1) ** 2:
            px(im, x, y, CORONA)

# ══ 4. LA BRUMA (tres bandas que respiran sobre el llano) ════════
for banda, (by, color, den) in enumerate(((21, BRUMA_TENUE, 0.55),
                                          (24, BRUMA_CLARA, 0.42),
                                          (28, BRUMA_TENUE, 0.5))):
    for y in range(by, by + 3):
        for x in range(W):
            import math
            onda = 0.5 + 0.5 * math.sin(x * 0.55 + banda * 2.1)
            if (x * 7 + y * 3 + banda) % 10 < 10 * den * onda:
                c0 = im.getpixel((x, y))
                mezcla = tuple(int((a + b) // 2) for a, b in
                              zip(c0[:3], color))
                px(im, x, y, mezcla)

# ══ 3. EL LLANO Y LOS TENTÁCULOS CON FILO ════════════════════════
for x in range(W):
    h = 25 + (1 if x % 7 < 3 else 0)      # el llano respira
    for y in range(h, H):
        px(im, x, y, LLANO)
    px(im, x, h, LLANO_FILO)               # el filo del horizonte

# los CUATRO tentáculos (silueta + filo iluminado del lado del eclipse)
for (tx, base_y, alto, ancho) in ((5, 26, 10, 3), (12, 27, 7, 2),
                                  (20, 26, 9, 3), (27, 27, 6, 2)):
    for k in range(alto):
        y = base_y + k
        # la curva del tentáculo (se encorva hacia el eclipse)
        doblar = (k * k) * 0.028 * (1 if tx < 16 else -1)
        x0 = tx + doblar
        for j in range(-ancho, ancho + 1):
            x = round(x0 + j)
            if not dentro(x, y):
                continue
            if abs(j) == ancho and k > 1:
                px(im, x, y, TENT_FILO)   # EL FILO ILUMINADO
            else:
                px(im, x, y, TENTACULO)
    # la punta (el remate encorvado)
    px(im, round(tx + (alto * alto) * 0.028 * (1 if tx < 16 else -1)),
       base_y + alto, TENT_FILO)

# ══ 5. EL MARCO (la orla rúnica — la letra) ══════════════════════
for i in range(W):
    for borde, y in ((0, 0), (1, 1), (0, H - 1), (1, H - 2)):
        px(im, i, y, ORO_MARCO if borde == 0 else ORO_OSC)
    px(im, i, 2, OUT) if False else None
for j in range(H):
    for borde, x in ((0, 0), (1, 1), (0, W - 1), (1, W - 2)):
        px(im, x, j, ORO_MARCO if borde == 0 else ORO_OSC)
# EL CONTORNO ÍNTIMO DEL MARCO (la separación oscura)
for i in range(W):
    px(im, i, 2, OUT)
    px(im, i, H - 3, OUT)
for j in range(H):
    px(im, 2, j, OUT)
    px(im, W - 3, j, OUT)
# LAS CUATRO ESQUINAS RÚNICAS (los picos de diamante)
for esq in ((0, 0), (W - 1, 0), (0, H - 1), (W - 1, H - 1)):
    ex, ey = esq
    for d in range(5):
        for dx, dy in ((d, 0), (-d, 0), (0, d), (0, -d)):
            x, y = ex + dx * (1 if ex == 0 else -1), ey + dy * (1 if ey == 0 else -1)
            # el diamante de la runa en cada esquina
        pass
for (ex, ey) in ((0, 0), (W - 1, 0), (0, H - 1), (W - 1, H - 1)):
    # el diamante: un pico de 3×3 brillante clavado en la esquina
    sx = ex + (2 if ex == 0 else -2)
    sy = ey + (2 if ey == 0 else -2)
    for dx in (-2, -1, 0, 1, 2):
        for dy in (-2, -1, 0, 1, 2):
            k = abs(dx) + abs(dy)
            if k <= 2:
                px(im, sx + dx, sy + dy,
                   CORONA_VIVO if k <= 1 else CORONA)

im.save(SALIDA)
with open(SALIDA, 'rb') as f:
    print('MundoInteriorBuff.png 32×32 · md5', hashlib.md5(f.read()).hexdigest())

# ══ EL REPORTE ASCII ══════════════════════════════════════════════
print('  (O=oro marco · o=oro oscuro · #=contorno · C=corona · c=corona viva')
print('   D=disco · b=bruma · T=tentáculo · t=filo tentáculo · .=cielo)')
for y in range(H):
    fila = ''
    for x in range(W):
        r, g, b, a = im.getpixel((x, y))
        rgb = (r, g, b)
        if rgb in (ORO_MARCO,):
            fila += 'O'
        elif rgb in (ORO_OSC,):
            fila += 'o'
        elif rgb == OUT:
            fila += '#'
        elif rgb in (CORONA,):
            fila += 'C'
        elif rgb in (CORONA_VIVO,):
            fila += 'c'
        elif rgb == DISCO:
            fila += 'D'
        elif rgb == TENTACULO:
            fila += 'T'
        elif rgb == TENT_FILO:
            fila += 't'
        elif rgb in (LLANO, LLANO_FILO):
            fila += 'L' if rgb == LLANO else 'l'
        elif rgb in (CIELO_ALTO, CIELO_MEDIO, CIELO_BAJO):
            fila += '.'
        else:
            fila += 'b'
    print('  ' + fila)
