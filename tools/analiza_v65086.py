#!/usr/bin/env python3
"""v6.50.86 — ANÁLISIS PREVIO: los 4 códigos del usuario.

1. Los PÁRPADOS PUROS: `codigo de sprite libro v3 - ojo medio cerrado sin iris .txt`
   y `codigo de sprite libro v3 - ojo cerrado.txt` — ¿son la MISMA corrida que
   la base (`... sin iris.txt`)? Fuera del ojo |Δ| debe ser ~0 (si lo es, el
   parpadeo ya no necesita el GIF: párpados con la MISMA calidad que la base).
2. LAS ESTRELLAS de la tapa: píxeles brillantes fríos (blancos/celestes) sobre
   la tapa — ¿dónde están, cuántas, qué tamaño?
3. LAS VENAS de luz morada: píxeles púrpura brillantes que recorren el libro —
   máscara por tono; ¿cuántos, cómo se distribuyen en distancia radial al ojo?
"""
import base64
import colorsys
import math
import re

import numpy as np

UP = '/home/z/my-project/upload'
SRC_W, SRC_H = 706, 967
CX, CY = 374.3, 443.1  # el ojo del arte (verificado .84/.85)


def decodificar(ruta):
    data = open(ruta, encoding='utf-8').read()
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


def main():
    print('=== 1. LOS 4 CÓDIGOS ===')
    base = decodificar(f'{UP}/codigo de sprite libro v3 sin iris.txt')
    medio = decodificar(f'{UP}/codigo de sprite libro v3 - ojo medio cerrado sin iris .txt')
    cerrado = decodificar(f'{UP}/codigo de sprite libro v3 - ojo cerrado.txt')
    con_iris = decodificar(f'{UP}/codigo sprite libro v3.txt')
    print(f'  base sin iris      : {base.shape}')
    print(f'  medio cerrado      : {medio.shape}')
    print(f'  ojo cerrado        : {cerrado.shape}')
    print(f'  con iris (v3)      : {con_iris.shape}')

    yy, xx = np.mgrid[0:SRC_H, 0:SRC_W]
    d_ojo = np.sqrt((xx - CX) ** 2 + (yy - CY) ** 2)

    print('=== 2. ¿MISMA CORRIDA? (|Δ| fuera del ojo r>200) ===')
    fuera = d_ojo > 200
    for nombre, arr in (('medio', medio), ('cerrado', cerrado), ('con_iris', con_iris)):
        d = np.abs(arr.astype(int) - base.astype(int))
        d_rgb = d[..., :3][fuera].mean()
        d_a = d[..., 3][fuera].mean()
        difpx = int((d.sum(axis=2) > 12)[fuera].sum())
        print(f'  {nombre:10s}: |Δ RGB|={d_rgb:.4f} · |Δ A|={d_a:.4f} · px distintos={difpx}')

    print('=== 3. LA ZONA QUE CAMBIA (base → párpados) ===')
    for nombre, arr in (('medio', medio), ('cerrado', cerrado)):
        d = np.abs(arr.astype(int) - base.astype(int)).sum(axis=2)
        m = d > 12
        ys, xs = np.nonzero(m)
        if len(ys):
            print(f'  {nombre}: {m.sum()} px · bbox x[{xs.min()},{xs.max()}] y[{ys.min()},{ys.max()}]'
                  f' · dist al ojo r[{np.sqrt((xs-CX)**2+(ys-CY)**2).min():.0f},'
                  f'{np.sqrt((xs-CX)**2+(ys-CY)**2).max():.0f}]')
        # el iris ¿sigue visible en el cerrado? (zonas cálidas dentro de r<90)
        rgba = arr.astype(int)
        calido = (rgba[..., 0] - rgba[..., 2] > 60) & (rgba[..., 3] > 0) & (d_ojo < 90)
        print(f'            px cálidos r<90: {int(calido.sum())} (base: '
              f'{int(((base[...,0].astype(int)-base[...,2].astype(int)>60)&(base[...,3]>0)&(d_ojo<90)).sum())})')

    print('=== 4. LAS ESTRELLAS DE LA TAPA (brillantes fríos) ===')
    rgba = con_iris.astype(float)
    r, g, b, a = rgba[..., 0], rgba[..., 1], rgba[..., 2], rgba[..., 3]
    lum = 0.299 * r + 0.587 * g + 0.114 * b
    mx = np.maximum(np.maximum(r, g), b)
    mn = np.minimum(np.minimum(r, g), b)
    sat = np.where(mx > 0, (mx - mn) / np.maximum(mx, 1), 0)
    # estrella: brillante, poco saturado (blanco/celeste pálido), fuera del ojo
    estrellas = (lum > 175) & (sat < 0.35) & (a > 200) & (d_ojo > 180)
    print(f'  px candidatas: {int(estrellas.sum())}')
    # componentes conexas (BFS ligero por dilatación + etiquetas)
    from scipy import ndimage
    etiquetas, n = ndimage.label(estrellas)
    print(f'  componentes: {n}')
    tams = ndimage.sum(estrellas, etiquetas, range(1, n + 1))
    grandes = [i + 1 for i, t in enumerate(tams) if t >= 6]
    print(f'  con >=6 px: {len(grandes)}')
    for i in grandes[:24]:
        ys, xs = np.nonzero(etiquetas == i)
        cy, cx = ys.mean(), xs.mean()
        t = int(tams[i - 1])
        rgbm = rgba[etiquetas == i].mean(axis=0)[:3]
        print(f'    estrella: centro ({cx:.0f},{cy:.0f}) · {t} px · '
              f'RGB ({rgbm[0]:.0f},{rgbm[1]:.0f},{rgbm[2]:.0f}) · '
              f'r_ojo={math.hypot(cx-CX, cy-CY):.0f}')

    print('=== 5. LAS VENAS DE LUZ MORADA ===')
    # tono púrpura: hue 0.62-0.85 (violeta-magenta), brillante, saturado
    hsv = np.zeros((*rgba.shape[:2], 3))
    nz = a > 0
    rr, gg, bb = r / 255.0, g / 255.0, b / 255.0
    mxn, mnn = np.maximum(np.maximum(rr, gg), bb), np.minimum(np.minimum(rr, gg), bb)
    v = mxn
    s = np.where(mxn > 0, (mxn - mnn) / np.maximum(mxn, 1e-6), 0)
    # hue
    dr = np.where(mxn == rr, (gg - bb) / np.maximum(mxn - mnn, 1e-6), 0)
    dg = np.where(mxn == gg, 2.0 + (bb - rr) / np.maximum(mxn - mnn, 1e-6), 0)
    db = np.where(mxn == bb, 4.0 + (rr - gg) / np.maximum(mxn - mnn, 1e-6), 0)
    h = (np.where(mxn == rr, dr, np.where(mxn == gg, dg, db)) / 6.0) % 1.0
    venas = (h > 0.60) & (h < 0.87) & (s > 0.30) & (v > 0.42) & nz & (d_ojo > 165)
    print(f'  px vena candidatas: {int(venas.sum())}')
    if venas.sum() > 0:
        vy, vx = np.nonzero(venas)
        dv = np.sqrt((vx - CX) ** 2 + (vy - CY) ** 2)
        print(f'  dist al ojo: min {dv.min():.0f} · p25 {np.percentile(dv,25):.0f} · '
              f'med {np.median(dv):.0f} · p75 {np.percentile(dv,75):.0f} · max {dv.max():.0f}')
        print(f'  bbox: x[{vx.min()},{vx.max()}] y[{vy.min()},{vy.max()}]')
        rgbv = rgba[venas].mean(axis=0)[:3]
        print(f'  RGB medio vena: ({rgbv[0]:.0f},{rgbv[1]:.0f},{rgbv[2]:.0f})')
        # brillo de la vena vs su vecindad (para calibrar cuánto iluminar)
        dil = ndimage.binary_dilation(venas, iterations=6) & ~venas & nz
        rgbd = rgba[dil].mean(axis=0)[:3]
        print(f'  RGB medio alrededor: ({rgbd[0]:.0f},{rgbd[1]:.0f},{rgbd[2]:.0f})')
    np.save('/tmp/v65086_venas.npy', venas)
    np.save('/tmp/v65086_estrellas.npy', estrellas)
    np.save('/tmp/v65086_base.npy', base)
    np.save('/tmp/v65086_medio.npy', medio)
    np.save('/tmp/v65086_cerrado.npy', cerrado)
    np.save('/tmp/v65086_coniris.npy', con_iris)
    print('LISTO — arrays en /tmp/v65086_*.npy')


if __name__ == '__main__':
    main()
