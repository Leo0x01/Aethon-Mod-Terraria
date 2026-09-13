#!/usr/bin/env python3
"""Análisis métrico por píxel de la referencia del agujero negro (Gargantua).

Mide: bounding box de la estructura, esfera negra (centro/radio), extensión
del disco de acreción edge-on, arcos de lente vertical, y muestrea la paleta
en franjas radiales/verticales para extraer los colores exactos.
"""
import numpy as np
from PIL import Image

IMG = "/home/z/my-project/upload/pasted_image_1789228267167.png"
img = Image.open(IMG).convert("RGBA")
W, H = img.size
a = np.asarray(img).astype(np.float32)
rgb, alpha = a[..., :3], a[..., 3]

print(f"Imagen: {W}x{H}")

lum = rgb.mean(axis=2)
# Estructura = píxeles visiblemente brillantes o coloreados (lum > 18)
struct = (lum > 18) & (alpha > 10)
ys, xs = np.where(struct)
if len(xs):
    x0, x1, y0, y1 = xs.min(), xs.max(), ys.min(), ys.max()
    print(f"Estructura brillante: bbox x[{x0}..{x1}] ({x1-x0+1}px) y[{y0}..{y1}] ({y1-y0+1}px)")
    print(f"  centro estructura: ({(x0+x1)/2:.0f}, {(y0+y1)/2:.0f})  aspecto W/H = {(x1-x0+1)/(y1-y0+1):.2f}")

# --- La esfera negra: región contigua oscura rodeada de brillo ---
cx = (x0 + x1) // 2
cy = (y0 + y1) // 2
row = lum[cy]
dark_run = []
in_dark = False
start = 0
for x in range(x0, x1 + 1):
    if row[x] < 14:
        if not in_dark:
            in_dark = True; start = x
    else:
        if in_dark:
            in_dark = False; dark_run.append((start, x - 1))
if in_dark: dark_run.append((start, x1))
dark_run = [r for r in dark_run if r[1] - r[0] > 20]
print(f"\nTramos negros en fila central y={cy}: {dark_run}")
for (dx0, dx1) in dark_run:
    print(f"  esfera negra: centro_x={(dx0+dx1)/2:.0f} diametro={dx1-dx0+1}px")

scx = int((dark_run[0][0] + dark_run[0][1]) / 2) if dark_run else cx
col = lum[:, scx]
vdark = []
in_dark = False
vstart = 0
for y in range(y0, y1 + 1):
    if col[y] < 14:
        if not in_dark:
            in_dark = True; vstart = y
    else:
        if in_dark:
            in_dark = False; vdark.append((vstart, y - 1))
if in_dark: vdark.append((vstart, y1))
vdark = [r for r in vdark if r[1] - r[0] > 20]
print(f"Tramos negros en columna x={scx}: {vdark}")
for (dy0, dy1) in vdark:
    print(f"  vertical: centro_y={(dy0+dy1)/2:.0f} altura={dy1-dy0+1}px")

# --- Perfil de brillo en la fila del centro ---
print(f"\nPerfil de brillo fila central (cada ~{(x1-x0)//24}px desde x0):")
for x in range(x0, x1 + 1, max(1, (x1 - x0) // 24)):
    print(f"  x={x:4d} lum={lum[cy, x]:6.1f} rgb=({rgb[cy,x,0]:.0f},{rgb[cy,x,1]:.0f},{rgb[cy,x,2]:.0f})")

# --- Paleta a lo largo del disco ---
print("\nPaleta a lo largo del disco (fila central, transiciones):")
prev = None
for x in range(x0, x1 + 1):
    c = tuple(rgb[cy, x].astype(int))
    if prev is None or (abs(c[0]-prev[0])+abs(c[1]-prev[1])+abs(c[2]-prev[2])) > 60:
        print(f"  x={x:4d} #{c[0]:02X}{c[1]:02X}{c[2]:02X}  rgb={c}")
        prev = c

# --- Arco superior ---
print("\nPerfil vertical (arriba de la esfera) en columnas clave:")
for x in [scx - 60, scx - 30, scx, scx + 30, scx + 60]:
    if 0 <= x < W:
        colv = lum[:, x]
        peaks = [(y, colv[y]) for y in range(y0, min(cy, y1)) if colv[y] > 90]
        if peaks:
            print(f"  x={x}: brillo>90 entre y={peaks[0][0]}..{peaks[-1][0]}, pico={max(p[1] for p in peaks):.0f} en y={max(peaks, key=lambda p: p[1])[0]}")

# --- Paleta del arco superior ---
if vdark:
    print(f"\nPaleta del arco superior (columna x={scx}, y de {y0} a esfera):")
    ytop = vdark[0][0]
    for y in range(y0, ytop, max(1, (ytop - y0) // 12)):
        c = tuple(rgb[y, scx].astype(int))
        print(f"  y={y:4d} lum={lum[y, scx]:6.1f} #{c[0]:02X}{c[1]:02X}{c[2]:02X}")

# --- Grosor del disco lado izquierdo ---
if dark_run:
    print("\nGrosor vertical del disco en x=esfera-0.75*diam (lado izq):")
    dx = int(scx - (dark_run[0][1] - dark_run[0][0]) * 0.75)
    if 0 <= dx < W:
        colv = lum[:, dx]
        band = [y for y in range(y0, y1 + 1) if colv[y] > 60]
        if band:
            print(f"  banda brillante: y[{band[0]}..{band[-1]}] grosor={band[-1]-band[0]+1}px")
            for y in range(band[0], band[-1] + 1, max(1, (band[-1] - band[0]) // 8)):
                c = tuple(rgb[y, dx].astype(int))
                print(f"    y={y}: #{c[0]:02X}{c[1]:02X}{c[2]:02X}")

sel = struct
print(f"\nEstadísticas de la estructura ({sel.sum()} px):")
print(f"  RGB medio: {rgb[sel].mean(axis=0).round(0)}")
sat = (rgb[sel].max(axis=1) - rgb[sel].min(axis=1)) / np.maximum(rgb[sel].max(axis=1), 1)
print(f"  Saturación media: {sat.mean():.2f}")
