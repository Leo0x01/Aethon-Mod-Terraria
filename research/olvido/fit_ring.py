#!/usr/bin/env python3
"""fit_ring.py — Ajuste de la elipse del anillo interior del agujero de la referencia."""
import numpy as np
from PIL import Image
import json

im = Image.open('referencia.png').convert('RGB')
a = np.array(im).astype(np.float32)
H, W = a.shape[:2]
r_, g_, b_ = a[..., 0], a[..., 1], a[..., 2]
lum = 0.299*r_ + 0.587*g_ + 0.114*b_
cx, cy = 495.0, 224.0

# solo plasma (canal R dominante sobre G)
plasma_lum = np.where(r_ > g_*1.6, lum, 0)

N = 360
thetas = np.radians(np.arange(0, N, 2))
rs = []
for th in thetas:
    rr = np.arange(20, 100, 0.5)
    px = cx + np.cos(th)*rr
    py = cy - np.sin(th)*rr  # theta visual: 90 = arriba
    xi = np.round(px).astype(int)
    yi = np.round(py).astype(int)
    ok = (xi >= 0) & (xi < W) & (yi >= 0) & (yi < H)
    vals = np.zeros(len(rr))
    vals[ok] = plasma_lum[yi[ok], xi[ok]]
    rs.append(rr[np.argmax(vals)] if vals.max() > 40 else np.nan)
rs = np.array(rs)
valid = ~np.isnan(rs)
print(f"Ángulos con anillo detectable: {valid.sum()}/{len(rs)}")

xs = rs*np.cos(thetas)
ys = -rs*np.sin(thetas)  # coords imagen (y hacia abajo)
P = np.stack([xs[valid]**2, xs[valid]*ys[valid], ys[valid]**2], 1)
coef, *_ = np.linalg.lstsq(P, np.ones(int(valid.sum())), rcond=None)
A_, B_, C_ = coef
M = np.array([[A_, B_/2], [B_/2, C_]])
evals, evecs = np.linalg.eigh(M)
axes = 1/np.sqrt(evals)
big = int(np.argmax(axes))
small = int(np.argmin(axes))
a_mayor, b_menor = float(axes[big]), float(axes[small])
dir_mayor = evecs[:, big]
ang_deg = float(np.degrees(np.arctan2(dir_mayor[1], dir_mayor[0])) % 180)
print(f"ELIPSE DEL ANILLO: a={a_mayor:.1f}px, b={b_menor:.1f}px, ratio={b_menor/a_mayor:.3f}")
print(f"Eje mayor en {ang_deg:.1f}° | Radio deprojected ~{(a_mayor+b_menor)/2:.1f}px = {(a_mayor+b_menor)/2/34:.2f}R")

json.dump({'cx': cx, 'cy': cy, 'R_sphere': 34.0,
           'ring_r_deproj': float((a_mayor+b_menor)/2),
           'squash': float(b_menor/a_mayor), 'tilt_deg': ang_deg},
          open('geom.json', 'w'), indent=1)

print("\nRadio del anillo por ángulo visual:")
for k in range(0, len(rs), 15):
    v = rs[k] if not np.isnan(rs[k]) else -1
    j = (k + len(rs)//2) % len(rs)
    v2 = rs[j] if not np.isnan(rs[j]) else -1
    print(f"  {k*2:3d}°: {v:5.1f}px   {(k*2+180)%360:3d}°: {v2:5.1f}px")
