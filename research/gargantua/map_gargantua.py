#!/usr/bin/env python3
"""Mapa ASCII de la referencia + detección de la esfera negra rodeada de brillo."""
import numpy as np
from PIL import Image

IMG = "/home/z/my-project/upload/pasted_image_1789228267167.png"
img = Image.open(IMG).convert("RGBA")
W, H = img.size
a = np.asarray(img).astype(np.float32)
rgb = a[..., :3]
lum = rgb.mean(axis=2)

# ASCII art 94x35
cols, rows = 94, 34
cell_w, cell_h = W / cols, H / rows
print("Mapa ASCII (.=oscuro, -=tenue, +=brillante, *=muy brillante, #=blanco):")
for r in range(rows):
    line = ""
    for c in range(cols):
        y0, y1 = int(r * cell_h), int((r + 1) * cell_h)
        x0, x1 = int(c * cell_w), int((c + 1) * cell_w)
        block = lum[y0:y1, x0:x1]
        v = block.mean()
        if v < 8: ch = "."
        elif v < 25: ch = "-"
        elif v < 70: ch = "+"
        elif v < 140: ch = "*"
        else: ch = "#"
        line += ch
    print(f"{int(r*cell_h):3d} {line}")
print("    " + "".join(str(int(c * cell_w) // 100 % 10) if c % 2 == 0 else " " for c in range(cols)))
print("    " + "".join(str(int(c * cell_w) // 10 % 10) if c % 2 == 0 else " " for c in range(cols)))

# --- Detección de la esfera negra: disco oscuro CONTIGUO rodeado de anillo brillante ---
# Máscara de brillo (disco/arcos)
bright = lum > 60
# Región oscura central: flood fill desde el punto más oscuro del centro de masa del brillo
from scipy import ndimage
ys, xs = np.where(bright)
cbx, cby = xs.mean(), ys.mean()
print(f"\nCentro de masa del brillo: ({cbx:.0f}, {cby:.0f})")

# Buscar en un radio de 150px del centro de masa el punto más oscuro
best = None
for y in range(max(0, int(cby) - 140), min(H, int(cby) + 140)):
    for x in range(max(0, int(cbx) - 140), min(W, int(cbx) + 140)):
        if lum[y, x] < lum.min() + 2:
            best = (y, x)
            break
    if best: break
if best is None:
    # fallback: el punto más oscuro
    idx = np.unravel_index(np.argmin(lum), lum.shape)
    best = (idx[0], idx[1])
print(f"Semilla oscura: {best}")

dark = lum < 12
lbl, n = ndimage.label(dark)
seed_lbl = lbl[best[0], best[1]]
sphere = lbl == seed_lbl
sy, sx = np.where(sphere)
if len(sx):
    print(f"Esfera negra (componente de la semilla): {len(sx)} px, bbox x[{sx.min()}..{sx.max()}] y[{sy.min()}..{sy.max()}]")
    cx, cy = (sx.min() + sx.max()) / 2, (sy.min() + sy.max()) / 2
    print(f"  centro=({cx:.0f},{cy:.0f}) diam_x={sx.max()-sx.min()+1} diam_y={sy.max()-sy.min()+1}")

# --- El anillo brillante alrededor de la esfera: perfil radial desde el centro ---
print("\nPerfil radial desde el centro de la esfera (media por anillo):")
for r in range(5, 260, 8):
    yy, xx = np.ogrid[:H, :W]
    mask = (xx - cx) ** 2 + (yy - cy) ** 2 <= r * r
    ring = (xx - cx) ** 2 + (yy - cy) ** 2 <= r * r
    ring2 = (xx - cx) ** 2 + (yy - cy) ** 2 <= max(1, (r - 6)) ** 2
    ann = mask & ~ring2
    if ann.sum() == 0: continue
    vals = rgb[ann]
    print(f"  r={r:3d}: lum={lum[ann].mean():6.1f} rgb=({vals[:,0].mean():5.0f},{vals[:,1].mean():5.0f},{vals[:,2].mean():5.0f})")

# --- Extensión horizontal del disco: fila de máximo brillo integrado ---
rowsum = bright.sum(axis=1)
best_y = int(np.argmax(rowsum))
print(f"\nFila con más píxeles brillantes: y={best_y} ({rowsum[best_y]} px)")
rowb = lum[best_y]
xs_b = np.where(rowb > 60)[0]
if len(xs_b):
    print(f"  disco en esa fila: x[{xs_b.min()}..{xs_b.max()}] ancho={xs_b.max()-xs_b.min()+1}px")
print(f"\nFila central de la esfera y={int(cy)}:")
rowb2 = lum[int(cy)]
xs_b2 = np.where(rowb2 > 60)[0]
if len(xs_b2):
    # tramos
    runs = []
    s = xs_b2[0]
    for i in range(1, len(xs_b2)):
        if xs_b2[i] != xs_b2[i-1] + 1:
            runs.append((s, xs_b2[i-1]))
            s = xs_b2[i]
    runs.append((s, xs_b2[-1]))
    print(f"  tramos brillantes: {runs}")
