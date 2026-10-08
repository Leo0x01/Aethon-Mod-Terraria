#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_reloj_v65075.py — EL RELOJ DE ARENA DEL ESCRIBA (v6.50.75).

El icono del probador del cielo («un clic y es de día, otro clic y es de
noche»). Pixel-art directo a 28×28 (sin downscale: cada píxel a mano,
como manda el pixel-art) con la paleta de la casa:

  · MADERA VIOLETA — la misma madera del grimorio (tapas + columnas),
    violeta-oscuro para que sea de LA familia del libro.
  · TINTA VIVA — la arena es TINTA (la misma #f0a9f6 del Códice Vivo):
    un charco en el bulbo superior, el hilo cayendo por la cintura y un
    montón creciendo abajo (el reloj está A MEDIA VUELTA — girándolo se
    vacía y se llena).
  · CRISTAL — bordes lavanda pálidos con dos brillos (sin relleno: el
    interior es transparente, como el vidrio de Terraria).
  · ORO — el remate del pinza superior: el toque dorado de los
    probadores de la casa (Quest).

Autoverificación: tamaño, paleta cerrada, bbox, simetría izquierda-derecha
de la arquitectura (madera+cristal+tinta base), presencia de tinta en los
TRES sitios (charco / hilo / montón) y alfa binario. Hoja de contacto ×8
para el QA de visión.

Salida: AethonMod/Content/Items/RelojDeArenaDelEscriba.png (28×28 RGBA)
"""
from PIL import Image

W = H = 28
DST = 'AethonMod/Content/Items/RelojDeArenaDelEscriba.png'
HOJA = '/tmp/reloj_v65075_hoja.png'

# === LA PALETA (cerrada — 13 tonos) ===
MADERA_OSC   = (24, 14, 20)    # columnas exterior / tapas borde
MADERA_MED   = (56, 32, 44)    # tapas y columnas
MADERA_CLAR  = (88, 52, 68)    # cara de las tapas
MADERA_BRILLO= (124, 78, 98)   # nudos y aristas
CRISTAL      = (196, 188, 216) # borde del vidrio
CRISTAL_BRILLO = (238, 234, 248) # el reflejo del vidrio
TINTA_CLARA  = (240, 169, 246) # la tinta viva del Códice (#f0a9f6)
TINTA_MEDIA  = (182, 120, 232)
TINTA_OSCURA = (122, 79, 168)
TINTA_PROF   = (74, 45, 104)
ORO          = (245, 196, 81)  # el remate dorado (paleta de probadores)

PALETA = {MADERA_OSC, MADERA_MED, MADERA_CLAR, MADERA_BRILLO,
          CRISTAL, CRISTAL_BRILLO,
          TINTA_CLARA, TINTA_MEDIA, TINTA_OSCURA, TINTA_PROF, ORO}

img = Image.new('RGBA', (W, H), (0, 0, 0, 0))
px = img.load()


def put(x, y, c):
    if 0 <= x < W and 0 <= y < H:
        px[x, y] = (c[0], c[1], c[2], 255)


def fila(y, x0, x1, c, extremos=None):
    """Pinta x0..x1 (inclusive). 'extremos' pinta los 2 px de cada
    punta con otro color (el canto de la tapa)."""
    for x in range(x0, x1 + 1):
        if extremos and (x <= x0 + 1 or x >= x1 - 1):
            put(x, y, extremos)
        else:
            put(x, y, c)


# ======================================================================
#  1. LA ARQUITECTURA — remate, tapas, columnas
# ======================================================================

# EL REMATE DORADO (la pinza superior — el toque del probador)
fila(1, 12, 15, ORO)

# LA TAPA SUPERIOR (dos filas: canto ancho arriba, cara angosta abajo)
fila(2, 8, 19, MADERA_MED, extremos=MADERA_OSC)
fila(3, 6, 21, MADERA_OSC)
for x in (8, 11, 16, 19):      # brillo de la cara (pares espejo)
    put(x, 3, MADERA_CLAR)

# LA TAPA INFERIOR (espejo)
fila(24, 6, 21, MADERA_OSC)
fila(25, 8, 19, MADERA_MED, extremos=MADERA_OSC)
for x in (9, 13, 14, 18):     # brillo inferior (pares espejo)
    put(x, 24, MADERA_CLAR)

# LAS COLUMNAS (violeta-oscuro afuera, medio adentro — con dos nudos)
for y in range(4, 24):
    put(5, y, MADERA_OSC)
    put(6, y, MADERA_MED)
    put(21, y, MADERA_MED)
    put(22, y, MADERA_OSC)
put(6, 9, MADERA_BRILLO);  put(21, 9, MADERA_BRILLO)   # nudos (par espejo)
put(6, 16, MADERA_BRILLO); put(21, 16, MADERA_BRILLO)
put(5, 23, MADERA_BRILLO); put(22, 23, MADERA_BRILLO)  # aristas (par)
put(5, 4, MADERA_BRILLO);  put(22, 4, MADERA_BRILLO)

# ======================================================================
#  2. EL VIDRIO — los dos bulbos y la cintura (solo el borde)
# ======================================================================

# La silueta interior del vidrio por fila (x0..x1 del INTERIOR del bulbo)
BULBO = {
    # superior (ancho arriba, cintura abajo)
    5:  (9, 18), 6: (8, 19), 7: (8, 19), 8: (9, 18), 9: (9, 18),
    10: (10, 17), 11: (11, 16), 12: (12, 15),
    # la cintura
    13: (12, 15), 14: (12, 15),
    # inferior (cintura arriba, ancho abajo)
    15: (12, 15), 16: (11, 16), 17: (11, 16), 18: (10, 17),
    19: (9, 18), 20: (9, 18), 21: (8, 19), 22: (8, 19),
}
for y, (x0, x1) in BULBO.items():
    put(x0, y, CRISTAL)          # borde izquierdo
    put(x1, y, CRISTAL)          # borde derecho
    if y in (5, 22):             # la tapa del bulbo (arriba/abajo)
        for x in range(x0, x1 + 1):
            put(x, y, CRISTAL)

# LOS REFLEJOS del vidrio (2 por bulbo, lado superior-izquierdo)
put(9, 6, CRISTAL_BRILLO); put(9, 7, CRISTAL_BRILLO)
put(9, 21, CRISTAL_BRILLO); put(10, 20, CRISTAL_BRILLO)

# ======================================================================
#  3. LA TINTA — charco arriba, hilo en la cintura, montón abajo
#     (el reloj A MEDIA VUELTA: la página se está reescribiendo)
# ======================================================================

# EL CHARCO del bulbo superior (superficie clara, cuerpo oscuro)
fila(9, 11, 16, TINTA_CLARA)
fila(10, 11, 16, TINTA_MEDIA)
fila(11, 11, 16, TINTA_OSCURA)
fila(12, 12, 15, TINTA_OSCURA)
put(12, 9, TINTA_PROF); put(15, 9, TINTA_PROF)  # las orillas de la superficie
put(13, 11, TINTA_MEDIA); put(14, 11, TINTA_MEDIA)  # una burbuja prisionera
put(12, 12, TINTA_MEDIA); put(15, 12, TINTA_MEDIA)

# EL HILO — la tinta cayendo por la cintura (lo más vivo del reloj)
for y in range(13, 16):
    put(13, y, TINTA_CLARA)
    put(14, y, TINTA_CLARA)
put(13, 16, TINTA_MEDIA)                        # el hilo se estira al caer
put(14, 16, TINTA_MEDIA)

# EL MONTÓN del bulbo inferior (pico claro, base profunda)
fila(17, 12, 15, TINTA_CLARA)     # el pico donde cae el hilo
fila(18, 11, 16, TINTA_MEDIA)
fila(19, 11, 16, TINTA_MEDIA)
fila(20, 10, 17, TINTA_OSCURA)
fila(21, 10, 17, TINTA_OSCURA)
fila(22, 9, 18, TINTA_PROF)       # la base asentada contra el vidrio
put(12, 18, TINTA_CLARA);  put(15, 18, TINTA_CLARA)   # salpicadura (par)
put(11, 20, TINTA_MEDIA);  put(16, 20, TINTA_MEDIA)   # vetas (par)
put(11, 21, TINTA_MEDIA);  put(16, 21, TINTA_MEDIA)

# LAS MOTAS de tinta flotando (el reloj está vivo — como el Códice)
put(7, 9, TINTA_CLARA)
put(20, 15, TINTA_CLARA)
put(8, 15, TINTA_OSCURA)

# ======================================================================
#  4. AUTOVERIFICACIÓN
# ======================================================================

assert img.size == (28, 28), img.size

# alfa binario + paleta cerrada
mal = []
for y in range(H):
    for x in range(W):
        r, g, b, a = px[x, y]
        if a not in (0, 255):
            mal.append(('alfa', x, y, a))
        elif a == 255 and (r, g, b) not in PALETA:
            mal.append(('color', x, y, (r, g, b)))
assert not mal, mal[:8]

# bbox y conteos
xs = [x for y in range(H) for x in range(W) if px[x, y][3] == 255]
ys = [y for y in range(H) for x in range(W) if px[x, y][3] == 255]
bbox = (min(xs), min(ys), max(xs), max(ys))
print(f'bbox: x {bbox[0]}..{bbox[2]}  y {bbox[1]}..{bbox[3]}  '
      f'({bbox[2]-bbox[0]+1}×{bbox[3]-bbox[1]+1} de 28×28)')

tinta = {(TINTA_CLARA), (TINTA_MEDIA), (TINTA_OSCURA), (TINTA_PROF)}
n_tinta_arriba = sum(1 for y in range(5, 13) for x in range(W)
                     if px[x, y][3] == 255 and px[x, y][:3] in tinta)
n_hilo = sum(1 for y in range(13, 17) for x in range(12, 16)
             if px[x, y][:3] in tinta)
n_tinta_abajo = sum(1 for y in range(17, 23) for x in range(W)
                    if px[x, y][3] == 255 and px[x, y][:3] in tinta)
print(f'tinta: charco {n_tinta_arriba} px · hilo {n_hilo} px · montón {n_tinta_abajo} px')
assert n_tinta_arriba >= 20, 'el charco no se ve'
assert n_hilo >= 6, 'el hilo de la cintura no se ve'
assert n_tinta_abajo >= 24, 'el montón no se ve'

# simetría izquierda-derecha de la ARQUITECTURA (madera + vidrio + tinta,
# sin contar reflejos/motas — están puestos a propósito asimétricos)
EXTRA = {(9, 6), (9, 7), (9, 21), (10, 20), (7, 9), (20, 15), (8, 15)}
asim = 0
for y in range(H):
    for x in range(W // 2):
        if (x, y) in EXTRA or (27 - x, y) in EXTRA:
            continue
        a = px[x, y]; b = px[27 - x, y]
        if (a[3] == 255) != (b[3] == 255):
            asim += 1
        elif a[3] == 255 and a != b:
            # tonos espejo: OSC↔OSC, MED↔MED... los pares simétricos
            # exactos salvo el sombreado de columnas (5/6 vs 21/22 son
            # espejo EXACTO: OSC-MED / MED-OSC)
            asim += 1
print(f'asimetrías de arquitectura: {asim} (esperadas: 0)')
assert asim == 0, 'la silueta no es simétrica'

# colores usados
usados = {px[x, y][:3] for y in range(H) for x in range(W) if px[x, y][3] == 255}
print(f'colores usados: {len(usados)}/11 de la paleta')
assert usados <= PALETA and len(usados) >= 10

img.save(DST)
print(f'✓ {DST}')

# HOJA DE CONTACTO ×8 para el QA de visión (fondo cuadriculado claro/oscuro)
S = 8
for fondo, nombre in [((255, 255, 255, 255), 'claro')]:
    hoja = Image.new('RGBA', (28 * S, 28 * S), fondo)
    grande = img.resize((28 * S, 28 * S), Image.NEAREST)
    hoja.alpha_composite(great := grande)
    hoja.convert('RGB').save(HOJA)
    print(f'✓ {HOJA} (fondo {nombre}, ×{S})')
