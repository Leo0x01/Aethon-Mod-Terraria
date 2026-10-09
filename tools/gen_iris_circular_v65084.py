#!/usr/bin/env python3
"""
v6.50.84 — EL IRIS CIRCULAR.

La letra del usuario: «pero veo tu tecnica es muy buena y te lo hare mas facil,
te dare el mismo sprite y te lo dare en codigo para que uses la tecnica y puedas
recortar bien el ojo, ya que lo recortaste en un cuadrado en ves de un circulo».

El usuario entregó el sprite EXACTO en código (upload/codigo 2.txt): RLE de
tuplas de 5 bytes [count, R, G, B, A] en base64 — 706×967, 682.702 px, sin
pérdida. La .82 recortó el iris como un CUADRADO 166×166 (crop) — las esquinas
arrastraban esclera y fragmentos del anillo dorado y el borde se leía cuadrado.

LA CURA: recorte CIRCULAR del disco del ojo desde el sprite exacto:
  · centro (374.3, 443.1) y radio 84 — ajuste de círculo por mínimos cuadrados
    sobre el borde dorado→esclera medido en 24 direcciones (residuo 3,8 px:
    el arte es orgánico, no un compás);
  · máscara circular con pluma de 5 px (α=1 hasta r 81,5 → α=0 en r 86,5);
  · textura de salida 32×32 (4× supermuestreo) — el borde del disco queda
    redondo con cualquier sampler (LinearClamp suave / PointClamp limpio);
  · IRIS_ESC mapea la caja fuente 1:1 a escala de juego (36/706).

Salida (SOLO las 2 capas del iris — la base y los párpados NO se tocan):
  GrimorioHambriento_Iris.png      32×32  disco dorado
  GrimorioHambriento_IrisRojo.png  32×32  disco rojo (hue −45° en los dorados)
"""
import base64
import colorsys
import math
import re

import numpy as np
from PIL import Image

UP = '/home/z/my-project/upload/codigo 2.txt'
DEST = '/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Content/Weapons'
PREV = '/home/z/my-project/download/Aethon-Mod-Terraria/tools/tools/preview_iris_v65084.png'
BASE = f'{DEST}/GrimorioHambriento.png'      # la base NO se toca (solo para el preview)

CX, CY = 374.3, 443.1     # centro del disco (ajuste de círculo, sprite exacto)
R_DISCO = 84.0            # radio del disco dorado (borde cálido→esclera)
PLUMA = 2.5               # semi-ancho de la pluma del borde (px fuente)
TEX = 32                  # textura de salida (supermuestreo 4×)
SRC_W, SRC_H = 706, 967
GAME_W, GAME_H = 36, 49


def decodificar_exacto():
    """RLE [count,R,G,B,A] base64 → array RGBA 706×967 (el sprite EXACTO)."""
    data = open(UP, encoding='utf-8').read()
    rle = base64.b64decode(re.search(r'rle:"([^"]+)"', data).group(1))
    assert len(rle) % 5 == 0, f'RLE truncado: {len(rle)} bytes'
    px = bytearray(SRC_W * SRC_H * 4)
    p = i = 0
    while i + 5 <= len(rle):
        n, r, g, b, a = rle[i:i + 5]
        i += 5
        for _ in range(n):
            px[p] = r; px[p + 1] = g; px[p + 2] = b; px[p + 3] = a
            p += 4
    assert p == SRC_W * SRC_H * 4, f'pixeles {p // 4} != {SRC_W * SRC_H}'
    return np.frombuffer(bytes(px), dtype=np.uint8).reshape(SRC_H, SRC_W, 4)


def hue_rojo(arr):
    """Dorado → rojo (hue −45°) en los píxeles cálidos saturados (patrón .82)."""
    out = arr.copy()
    h, w = arr.shape[:2]
    n = 0
    for y in range(h):
        for x in range(w):
            r, g, b, a = arr[y, x]
            if a == 0:
                continue
            hh, s, v = colorsys.rgb_to_hsv(r / 255.0, g / 255.0, b / 255.0)
            if 0.05 <= hh <= 0.20 and s >= 0.25 and v >= 0.2:
                r2, g2, b2 = colorsys.hsv_to_rgb((hh - 0.125) % 1.0, s, v)
                out[y, x] = (round(r2 * 255), round(g2 * 255), round(b2 * 255), a)
                n += 1
    return out, n


def main():
    print('=== 1. SPRITE EXACTO (codigo 2.txt) ===')
    exacto = decodificar_exacto()
    print(f'  decodificado: {SRC_W}×{SRC_H}, {int((exacto[..., 3] > 0).sum())} px opacos')

    print('=== 2. RECORTE CIRCULAR ===')
    half = R_DISCO + PLUMA                       # 86,5 — la caja contiene la pluma
    x0, x1 = int(math.floor(CX - half)), int(math.ceil(CX + half))
    y0, y1 = int(math.floor(CY - half)), int(math.ceil(CY + half))
    caja = exacto[y0:y1, x0:x1].astype(np.float64)
    ch, cw = caja.shape[:2]
    ys, xs = np.mgrid[y0:y1, x0:x1]
    d = np.sqrt((xs - CX) ** 2 + (ys - CY) ** 2)
    # máscara: α=1 hasta R−pluma, 0 desde R+pluma, lineal entre
    mascara = np.clip((R_DISCO + PLUMA - d) / (2 * PLUMA), 0.0, 1.0)
    caja[..., 3] = caja[..., 3] * mascara
    disco_opacos = int((caja[..., 3] > 8).sum())
    print(f'  caja fuente {cw}×{ch} (r±{half:.1f} de ({CX},{CY})) · '
          f'{disco_opacos} px del disco · pluma {2 * PLUMA:.0f} px')

    print('=== 3. VARIANTE ROJA (hue −45° en los dorados) ===')
    rojo_src, n_rojo = hue_rojo(caja.astype(np.uint8))
    print(f'  {n_rojo} px dorados→rojos en el disco fuente')

    print('=== 4. TEXTURAS 32×32 (4× supermuestreo) ===')
    iris = Image.fromarray(caja.astype(np.uint8), 'RGBA').resize((TEX, TEX), Image.LANCZOS)
    iris_rojo = Image.fromarray(rojo_src, 'RGBA').resize((TEX, TEX), Image.LANCZOS)
    iris.save(f'{DEST}/GrimorioHambriento_Iris.png', optimize=True)
    iris_rojo.save(f'{DEST}/GrimorioHambriento_IrisRojo.png', optimize=True)

    import os
    for n in ('GrimorioHambriento_Iris.png', 'GrimorioHambriento_IrisRojo.png'):
        print(f'  {n:44s} {os.path.getsize(f"{DEST}/{n}"):5d} B')

    # comprobación circularidad: esquinas TRANSPARENTES, centro OPACO
    ia = np.array(iris)
    esquinas = [ia[0, 0, 3], ia[0, -1, 3], ia[-1, 0, 3], ia[-1, -1, 3]]
    centro = ia[TEX // 2, TEX // 2, 3]
    print(f'  circularidad: α esquinas={esquinas} (deben ser ~0) · α centro={centro} (255)')

    print('=== 5. CONSTANTE C# ===')
    juego_w = cw * GAME_W / SRC_W
    esc = juego_w / TEX
    print(f'  caja {cw}px fuente → {juego_w:.3f} px de juego · '
          f'IRIS_ESC = {esc:.4f}f  (private const float IRIS_ESC)')

    print('=== 6. PREVIEW 6× (base + iris centro/diagonal/rojo + cerrado) ===')
    base = Image.open(BASE).convert('RGBA')          # 36×49 — la base SIN tocar
    cerrado = Image.open(f'{DEST}/GrimorioHambriento_Cerrado.png').convert('RGBA')
    E = 6
    fw, fh = GAME_W * E, GAME_H * E
    ox, oy = 19.27, 22.70                            # OJO del código (36×49)

    def panel(img36):
        return img36.resize((fw, fh), Image.NEAREST)

    def con_iris(dx, dy, rojo=False):
        p = panel(base)
        t = (iris_rojo if rojo else iris).resize(
            (round(juego_w * E), round(juego_w * E)), Image.LANCZOS)
        p.paste(t, (round((ox + dx) * E - t.width / 2),
                     round((oy + dy) * E - t.height / 2)), t)
        return p

    lienzo = Image.new('RGBA', (fw * 5 + 48, fh + 8), (26, 24, 36, 255))
    for i, im in enumerate([con_iris(0, 0), con_iris(3.5, 0), con_iris(-3.5, 0),
                            con_iris(2.4, -2.0), con_iris(3.5, 0, rojo=True),
                            panel(cerrado)]):
        lienzo.paste(im, (i * (fw + 8) + 8, 4))
    lienzo.save(PREV)
    print(f'  {PREV}')
    print('LISTO')


if __name__ == '__main__':
    main()
