#!/usr/bin/env python3
"""
v6.50.105 — ANÁLISIS DE LOS 3 SPRITES DEL OJO MINION.

El usuario entregó 3 sprites EXACTOS en código (RLE base64 de tuplas
[count,R,G,B,A], 706×967 — el formato de la casa):
  · sprite libro v3 - minion ojo cerrado.txt
  · sprite libro v3 - minion ojo medio cerrado.txt
  · sprite libro v3 - minion ojo abierto.txt

La letra: «el minion que use el grimorio será su propio ojo […] yo te daré
los sprites del ojo usando código». Este script DECODIFICA los 3, mide la
región del ojo (bbox opaco, diferencia entre estados) y guarda previews
para decidir el recorte del minion.
"""
import base64
import math
import os
import re

import numpy as np
from PIL import Image

UP = '/home/z/my-project/upload'
OUT = '/home/z/my-project/download/Aethon-Mod-Terraria/tools/preview_ojos_v650105'
os.makedirs(OUT, exist_ok=True)

SRC_W, SRC_H = 706, 967
FILES = {
    'cerrado': 'sprite libro v3 - minion ojo cerrado.txt',
    'medio': 'sprite libro v3 - minion ojo medio cerrado.txt',
    'abierto': 'sprite libro v3 - minion ojo abierto.txt',
}


def decodificar(path):
    data = open(path, encoding='utf-8').read()
    m = re.search(r'w:(\d+)', data)
    m2 = re.search(r'h:(\d+)', data)
    w = int(m.group(1)) if m else SRC_W
    h = int(m2.group(1)) if m2 else SRC_H
    rle = base64.b64decode(re.search(r'rle:"([^"]+)"', data).group(1))
    assert len(rle) % 5 == 0, f'RLE truncado: {len(rle)}'
    px = bytearray(w * h * 4)
    p = i = 0
    while i + 5 <= len(rle):
        n, r, g, b, a = rle[i:i + 5]
        i += 5
        for _ in range(n):
            px[p] = r; px[p + 1] = g; px[p + 2] = b; px[p + 3] = a
            p += 4
    assert p == w * h * 4, f'pixeles {p // 4} != {w * h}'
    return np.frombuffer(bytes(px), dtype=np.uint8).reshape(h, w, 4), w, h


def bbox(arr, alpha_min=8):
    mask = arr[..., 3] >= alpha_min
    ys, xs = np.where(mask)
    if len(xs) == 0:
        return None
    return (int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max()))


def main():
    estados = {}
    for nombre, fname in FILES.items():
        arr, w, h = decodificar(f'{UP}/{fname}')
        estados[nombre] = arr
        bb = bbox(arr)
        opacos = int((arr[..., 3] > 8).sum())
        print(f'{nombre:8s} {w}×{h} · {opacos} px opacos · bbox opaco {bb}')

    # === DIFF entre estados: la región que cambia = el párpado/iris ===
    for a, b in [('abierto', 'medio'), ('medio', 'cerrado'), ('abierto', 'cerrado')]:
        da = estados[a].astype(int); db = estados[b].astype(int)
        diff = np.abs(da - db).sum(axis=2)
        mask = diff > 30
        ys, xs = np.where(mask)
        if len(xs):
            print(f'diff {a:7s}↔{b:8s}: {int(mask.sum()):6d} px · bbox '
                  f'({xs.min()},{ys.min()})-({xs.max()},{ys.max()})')
        else:
            print(f'diff {a:7s}↔{b:8s}: IDÉNTICOS')

    # === el ojo del libro: centro (374.3, 443.1) disco r=84 — medir el
    #     socket (la esclera) en el estado ABIERTO: el disco opaco grande ===
    CX, CY = 374.3, 443.1
    ab = estados['abierto']
    for r_probe in (84, 100, 120, 140, 160, 180, 200):
        ys, xs = np.mgrid[0:SRC_H, 0:SRC_W]
        d = np.sqrt((xs - CX) ** 2 + (ys - CY) ** 2)
        ring = (d <= r_probe) & (d > r_probe - 20)
        alpha_medio = float(np.median(ab[..., 3][ring])) if ring.sum() else 0
        dentro = d <= r_probe
        op = int((ab[..., 3][ring] > 8).sum())
        print(f'  anillo {r_probe-20:3d}-{r_probe:3d}: {op:6d}/{int(ring.sum()):6d} px opacos '
              f'({100*op/max(1,int(ring.sum())):5.1f}%)')

    # === previews: el recorte 340×340 alrededor del ojo, ×2 y al tamaño juego ===
    for nombre, arr in estados.items():
        half = 170
        x0, x1 = int(CX - half), int(CX + half)
        y0, y1 = int(CY - half), int(CY + half)
        caja = arr[max(0,y0):y1, max(0,x0):x1]
        Image.fromarray(caja, 'RGBA').save(f'{OUT}/ojo_{nombre}_340.png')
    print('previews en', OUT)


if __name__ == '__main__':
    main()
