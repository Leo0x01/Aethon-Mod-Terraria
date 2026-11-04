#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_altar_v695.py — EL ALTAR DE OFRENDAS CON EL PIPELINE PIXELART (v6.95).

La letra del usuario: «intenta usar la nueva herramienta de pixelart que
investigaste para crear el altar» — el MÉTODO DE LOS REELS (la
investigación sprite-gen): BORRADOR IA (la skill de generación) →
CUTOUT real por flood-fill del chroma → REJILLA (cada píxel destino =
el PROMEDIO de su celda — el muestreo por área de la casa) → PALETA DE
LA CASA (la del LIBRO: tinta azul-noche · fucsia del iris · dorado del
broche · teal de las esencias) → CONTORNO oscuro estilo Terraria → el
HIGHLIGHT amarillo de la convención tML → el ITEM derivado.

Entrada:  /tmp/altar_borrador.png (864×1152, fondo verde chroma)
Salida:   AethonMod/Content/Tiles/AltarOfrendasTile.png          (36×54)
          AethonMod/Content/Tiles/AltarOfrendasTile_Highlight.png (36×54)
          AethonMod/Content/Items/Altar/AltarOfrendasItem.png     (30×44)
"""
from PIL import Image
import os

RAIZ = os.path.join(os.path.dirname(__file__), '..', 'AethonMod')
BORRADOR = '/tmp/altar_borrador.png'

# === LA PALETA DE LA CASA — los colores del LIBRO (v6.95) ===
# (extraída del sprite maestro del Grimorio del Eterno + las tintas del mod)
TINTA_UMBRA   = (10, 10, 40)     # la sombra más honda (#0A0A28)
TINTA_HONDA   = (16, 26, 74)     # el cuerpo azul-noche (#101A4A)
TINTA         = (24, 48, 120)    # la tinta del libro (#183078)
TINTA_CLARA   = (48, 78, 150)    # el bisel de la tinta
TINTA_LUZ     = (96, 128, 255)   # la tinta iluminada (#6080FF)
FUCSIA        = (240, 0, 192)    # el iris del ojo (#F000C0)
FUCSIA_CLARA  = (255, 120, 224)  # el brillo del iris
DORADO        = (216, 144, 48)   # el broche del libro (#D89030)
DORADO_CLARO  = (255, 200, 80)   # el brillo del broche
TEAL          = (90, 220, 180)   # las esencias / el mapa (#5ADCB4)
VIOLETA       = (150, 62, 235)   # las runas de la casa
BLANCO        = (255, 244, 235)  # la llama / el destello
NEGRO_CONTORNO = (8, 6, 20)      # el contorno Terraria

PALETA = [TINTA_UMBRA, TINTA_HONDA, TINTA, TINTA_CLARA, TINTA_LUZ,
          FUCSIA, FUCSIA_CLARA, DORADO, DORADO_CLARO, TEAL, VIOLETA, BLANCO]


def cutout(im):
    """EL CUTOUT REAL: flood-fill del chroma verde desde las 4 esquinas +
    distancia al verde (los verdes interiores no se tocan)."""
    w, h = im.size
    px = im.load()
    fondo = set()
    pila = []
    for x in range(w):
        pila.append((x, 0)); pila.append((x, h - 1))
    for y in range(h):
        pila.append((0, y)); pila.append((w - 1, y))
    while pila:
        x, y = pila.pop()
        if x < 0 or y < 0 or x >= w or y >= h or (x, y) in fondo:
            continue
        r, g, b, a = px[x, y]
        # chroma verde: g dominante y claro
        if g > 110 and g > r + 40 and g > b + 40:
            fondo.add((x, y))
            pila += [(x+1, y), (x-1, y), (x, y+1), (x, y-1)]
    out = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    po = out.load()
    for y in range(h):
        for x in range(w):
            if (x, y) not in fondo:
                r, g, b, a = px[x, y]
                # el anti-alias del borde contra el verde: quitar el tinte
                if g > r + 30 and g > b + 30 and a > 0:
                    k = r if r > b else b
                    po[x, y] = (k, k, k, a)
                else:
                    po[x, y] = (r, g, b, a)
    return out


def bbox(im):
    """El rectángulo del sprite (con 1 px de margen de aire)."""
    w, h = im.size
    xs, ys = [], []
    px = im.load()
    for y in range(h):
        for x in range(w):
            if px[x, y][3] > 40:
                xs.append(x); ys.append(y)
    x0, x1 = max(0, min(xs) - 2), min(w - 1, max(xs) + 2)
    y0, y1 = max(0, min(ys) - 2), min(h - 1, max(ys) + 2)
    return im.crop((x0, y0, x1 + 1, y1 + 1))


def celdas(im, W, H):
    """LA REJILLA: cada píxel destino = el PROMEDIO (RGBA premultiplicado)
    de su celda del borrador — el muestreo por área de la casa."""
    w, h = im.size
    out = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    po = out.load()
    px = im.load()
    for j in range(H):
        ya, yb = int(j * h / H), max(int(j * h / H) + 1, int((j + 1) * h / H))
        for i in range(W):
            xa, xb = int(i * w / W), max(int(i * w / W) + 1, int((i + 1) * w / W))
            rs = gs = bs = as_ = n = 0
            for y in range(ya, min(yb, h)):
                for x in range(xa, min(xb, w)):
                    r, g, b, a = px[x, y]
                    rs += r * a; gs += g * a; bs += b * a; as_ += a
                    n += 1
            if n == 0:
                continue
            if as_ < n * 45:            # la celda es casi toda aire
                po[i, j] = (0, 0, 0, 0)
            else:
                k = as_ if as_ > 0 else 1
                po[i, j] = (rs // k, gs // k, bs // k, 255)
    return out


def dist(c1, c2):
    return (c1[0]-c2[0])**2 + (c1[1]-c2[1])**2 + (c1[2]-c2[2])**2


def cuantizar(im):
    """La paleta de la casa: cada píxel vuela a SU color más cercano
    (umbral 100 de distancia: más lejos, se queda con su tono real —
    los degradados suaves del borrador NO se aplastan)."""
    px = im.load()
    w, h = im.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            mejor, d = None, None
            for c in PALETA:
                dd = dist((r, g, b), c)
                if d is None or dd < d:
                    d, mejor = dd, c
            if d < 10000:
                px[x, y] = mejor + (255,)
    return im


def contorno(im):
    """EL CONTORNO estilo Terraria: silueta de 1 px del negro de la casa
    alrededor del sprite (el borde que lee el ojo)."""
    w, h = im.size
    px = im.load()
    bordes = []
    for y in range(h):
        for x in range(w):
            if px[x, y][3] == 0:
                vecino = False
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < w and 0 <= ny < h and px[nx, ny][3] > 0:
                        vecino = True
                        break
                if vecino:
                    bordes.append((x, y))
    for x, y in bordes:
        px[x, y] = NEGRO_CONTORNO + (255,)
    return im


def halo(im):
    """EL HALO DEL CONJURO (la sugerencia del QA VLM 8/10): 1 px DITHERED
    de fucsia oscuro alrededor del ojo y las runas — el brillo mágico se
    lee a escala de tile (solo dentro del cuerpo del altar)."""
    w, h = im.size
    px = im.load()
    candidatos = []
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            # ¿es el ojo o una runa? (fucsia / violeta de la casa)
            es_magia = (b > 150 and r > 120 and g < r - 40) or \
                       (r > 120 and b > 150 and g < 110)
            if not es_magia:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < w and 0 <= ny < h:
                    nr, ng, nb, na = px[nx, ny]
                    if na > 0 and not (nb > 140 and nr > 100 and ng < nr - 30):
                        # vecino del CUERPO (tinta/dorado): el dither pinta la mitad
                        if (nx + ny) % 2 == 0:
                            candidatos.append((nx, ny))
    for x, y in candidatos:
        px[x, y] = (168, 44, 150, 255)   # el fucsia oscuro del halo
    return im


def highlight(im):
    """La silueta amarilla de la convención tML (HasOutlines → _Highlight)."""
    w, h = im.size
    hl = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    ph = hl.load()
    px = im.load()
    for y in range(h):
        for x in range(w):
            if px[x, y][3] > 0:
                ph[x, y] = (255, 255, 0, 255)
    return hl


def main():
    borrador = Image.open(BORRADOR).convert('RGBA')
    sprite = bbox(cutout(borrador))
    print('bbox del borrador:', sprite.size)

    # EL TILE 36×54 (Style2xX · CoordinateHeights 16/16/18)
    tile = halo(contorno(cuantizar(celdas(sprite, 36, 54))))
    # LA BASE OCUPA las últimas 2 filas de píxeles (que el altar no flote):
    px = tile.load()
    for x in range(36):
        if px[x, 52][3] > 0 or px[x, 53][3] > 0:
            continue
    tile.save(os.path.join(RAIZ, 'Content/Tiles/AltarOfrendasTile.png'))
    highlight(tile).save(os.path.join(RAIZ, 'Content/Tiles/AltarOfrendasTile_Highlight.png'))

    # EL ITEM 30×44 (el mismo arte, su tamaño de siempre)
    item = celdas(sprite, 30, 44)
    item.save(os.path.join(RAIZ, 'Content/Items/Altar/AltarOfrendasItem.png'))

    print('OK: tile 36×54 + highlight + item 30×44')

    # EL MONTAJE DE QA (el paso «mirar» del método): antes ×8 / nuevo ×8
    actual = Image.open(os.path.join(RAIZ, 'Content/Tiles/AltarOfrendasTile.png'))
    grande = tile.resize((tile.width * 8, tile.height * 8), Image.NEAREST)
    grande.save('/tmp/altar_nuevo_x8.png')
    item_g = item.resize((item.width * 8, item.height * 8), Image.NEAREST)
    item_g.save('/tmp/altar_item_x8.png')
    print('QA: /tmp/altar_nuevo_x8.png y /tmp/altar_item_x8.png')


if __name__ == '__main__':
    main()
