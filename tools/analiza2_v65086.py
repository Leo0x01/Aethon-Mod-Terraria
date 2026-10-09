#!/usr/bin/env python3
"""v6.50.86 — recalibración de máscaras con el feedback del VLM.

ESTRELLAS reales: cruces amarillas + signos + morados SOBRE LA TAPA NEGRA
(fondo local oscuro) — los remaches del anillo (r~210) y los brillos de las
esquinas doradas NO son estrellas (fondo local brillante).

VENAS: relajar umbrales para cubrir la vena vertical izquierda + racimo
inferior (hue 0.56-0.90, sat>0.20, v>0.25).
"""
import math

import numpy as np
from PIL import Image
from scipy import ndimage

con_iris = np.load('/tmp/v65086_coniris.npy')
base = np.load('/tmp/v65086_base.npy')
SRC_H, SRC_W = con_iris.shape[:2]
CX, CY = 374.3, 443.1

yy, xx = np.mgrid[0:SRC_H, 0:SRC_W]
d_ojo = np.sqrt((xx - CX) ** 2 + (yy - CY) ** 2)

rgba = con_iris.astype(float)
r, g, b, a = rgba[..., 0], rgba[..., 1], rgba[..., 2], rgba[..., 3]
lum = 0.299 * r + 0.587 * g + 0.114 * b

# --- tono/saturación/valor vectoriales ---
rr, gg, bb = r / 255.0, g / 255.0, b / 255.0
mxn = np.maximum(np.maximum(rr, gg), bb)
mnn = np.minimum(np.minimum(rr, gg), bb)
v = mxn
s = np.where(mxn > 0, (mxn - mnn) / np.maximum(mxn, 1e-6), 0)
dr = np.where(mxn == rr, (gg - bb) / np.maximum(mxn - mnn, 1e-6), 0)
dg = np.where(mxn == gg, 2.0 + (bb - rr) / np.maximum(mxn - mnn, 1e-6), 0)
db = np.where(mxn == bb, 4.0 + (rr - gg) / np.maximum(mxn - mnn, 1e-6), 0)
h = (np.where(mxn == rr, dr, np.where(mxn == gg, dg, db)) / 6.0) % 1.0

print('=== VENAS recalibradas (hue .56-.90 · s>.20 · v>.25 · r_ojo>160) ===')
venas = (h > 0.56) & (h < 0.90) & (s > 0.20) & (v > 0.25) & (a > 50) & (d_ojo > 160)
print(f'  px: {int(venas.sum())} (antes 9932 con umbrales duros)')
vy, vx = np.nonzero(venas)
dv = np.sqrt((vx - CX) ** 2 + (vy - CY) ** 2)
print(f'  dist ojo: min {dv.min():.0f} · med {np.median(dv):.0f} · max {dv.max():.0f}')
rgbv = rgba[venas].mean(axis=0)[:3]
print(f'  RGB medio: ({rgbv[0]:.0f},{rgbv[1]:.0f},{rgbv[2]:.0f})')

print('=== ESTRELLAS recalibradas (sobre fondo local oscuro) ===')
# brillantes sobre la tapa: el fondo alrededor (anillo de dilatación) es OSCURO
claro = (lum > 130) & (a > 200) & (d_ojo > 235)
etiquetas, n = ndimage.label(claro)
print(f'  brillantes r_ojo>235: {n} componentes')
tams = ndimage.sum(claro, etiquetas, range(1, n + 1))
estrellas = np.zeros_like(claro)
estrellas_lista = []
for i in range(1, n + 1):
    m = etiquetas == i
    t = int(tams[i - 1])
    if not (5 <= t <= 600):
        continue
    # fondo local: media de luminancia del anillo alrededor
    anillo = ndimage.binary_dilation(m, iterations=7) & ~m
    if anillo.sum() == 0:
        continue
    lum_anillo = lum[anillo].mean()
    if lum_anillo > 100:   # fondo brillante = marco dorado, NO estrella
        continue
    ys, xs = np.nonzero(m)
    cx, cy = xs.mean(), ys.mean()
    estrellas[m] = True
    rgbm = rgba[m].mean(axis=0)[:3]
    estrellas_lista.append((cx, cy, t, rgbm, lum_anillo))
print(f'  ESTRELLAS: {len(estrellas_lista)} · px {int(estrellas.sum())}')
for cx, cy, t, rgbm, la in sorted(estrellas_lista, key=lambda e: -e[2])[:30]:
    print(f'    ({cx:4.0f},{cy:4.0f}) {t:3d} px · RGB ({rgbm[0]:.0f},{rgbm[1]:.0f},{rgbm[2]:.0f})'
          f' · fondo {la:.0f} · r_ojo {math.hypot(cx-CX, cy-CY):.0f}')

np.save('/tmp/v65086_venas2.npy', venas)
np.save('/tmp/v65086_estrellas2.npy', estrellas)

# --- visualización para QA ---
vis = con_iris.copy()
vis[venas] = (255, 60, 60, 255)
vis[estrellas] = (60, 255, 60, 255)
comp = Image.new('RGBA', (SRC_W * 2 + 24, SRC_H + 8), (18, 16, 26, 255))
comp.paste(Image.fromarray(con_iris), (8, 4))
comp.paste(Image.fromarray(vis), (SRC_W + 16, 4))
comp = comp.resize((comp.width // 2, comp.height // 2))
comp.save('/tmp/v65086_arte_mascaras2.png')
print('visual → /tmp/v65086_arte_mascaras2.png')
