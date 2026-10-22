#!/usr/bin/env python3
# v6.51.4 — EL ICONO DEL BRILLO SOLAR (la letra: «el nuevo item
# cosmetico el brillo de los soles runicos, tiene un sprite en el
# inventario que tiene un cuadrado de glow que se corta muy feo…
# el problema esta solo en el inventario»). LA CAUSA: el png de la
# .51.3 llevaba el resplandor HASTA EL BORDE (alfa 23-35 en los
# bordes: el glow se CORTABA en seco — el cuadrado feo). LA CURA:
# el destello de 4 puntas (dos cápsulas cruzadas de blanco
# incandescente + el corazón dorado — DrawTwinkles) dibujado ENTERO
# con margen: la luz muere en alfa 0 ANTES del borde del icono.
import math
from PIL import Image

S = 30
SS = 8
N = S * SS
C = N / 2.0

acum = [[0.0] * (N * N) for _ in range(3)]

def globo(x, y, rad, col, falloff='core'):
    cr, cg, cb = col
    x0 = max(0, int(x - rad - 2)); x1 = min(N, int(x + rad + 3))
    y0 = max(0, int(y - rad - 2)); y1 = min(N, int(y + rad + 3))
    rr2 = rad * rad + 1e-9
    for py in range(y0, y1):
        dy = py + 0.5 - y
        base = py * N
        dy2 = dy * dy
        for px in range(x0, x1):
            dx = px + 0.5 - x
            d2 = (dx * dx + dy2) / rr2
            if d2 > 1.0:
                continue
            d = math.sqrt(d2)
            a = (1.0 - d) ** 1.6 if falloff == 'core' else math.exp(-3.2 * d * d)
            if a <= 0.004:
                continue
            i = base + px
            acum[0][i] += cr * a
            acum[1][i] += cg * a
            acum[2][i] += cb * a

def capsula(x, y, w, h, col, rot=0.0):
    """Cápsula lineal rotada (la forma del rayo del destello)."""
    cr, cg, cb = col
    cos, sin = math.cos(rot), math.sin(rot)
    R = max(w, h)
    x0 = max(0, int(x - R - 2)); x1 = min(N, int(x + R + 3))
    y0 = max(0, int(y - R - 2)); y1 = min(N, int(y + R + 3))
    w2 = w * w + 1e-9; h2 = h * h + 1e-9
    for py in range(y0, y1):
        dy = py + 0.5 - y
        base = py * N
        for px in range(x0, x1):
            dx = px + 0.5 - x
            ux = dx * cos + dy * sin
            uy = -dx * sin + dy * cos
            d2 = ux * ux / w2 + uy * uy / h2
            if d2 > 1.0:
                continue
            a = 1.0 - math.sqrt(d2)
            if a <= 0.004:
                continue
            i = base + px
            acum[0][i] += cr * a
            acum[1][i] += cg * a
            acum[2][i] += cb * a

# === EL DESTELLO DE 4 PUNTAS (DrawTwinkles en persona) ===
# LAS DOS CÁPSULAS CRUZADAS (blanco incandescente) — largas pero
# CONTENIDAS: máx 13.5 px del centro (canvas 15 px de radio).
for ang in (0.0, math.pi / 2):
    capsula(C, C, 13.5 * SS, 1.55 * SS, (1.05, 1.0, 0.88), rot=ang)
    capsula(C, C, 11.0 * SS, 1.1 * SS, (1.25, 1.18, 1.0), rot=ang)
# LAS CÁPSULAS DIAGONALES (más cortas — el destello de 8 puntas sutil)
for ang in (math.pi / 4, 3 * math.pi / 4):
    capsula(C, C, 7.5 * SS, 1.2 * SS, (0.85, 0.8, 0.7), rot=ang)
# EL CORAZÓN DORADO (el núcleo del brillo solar)
globo(C, C, 4.6 * SS, (1.3, 1.02, 0.42), 'core')
globo(C, C, 2.6 * SS, (1.5, 1.22, 0.52), 'core')
globo(C, C, 1.4 * SS, (1.65, 1.42, 0.85), 'core')

# === TONE MAP (la luz del juego: nada se quema a blanco plano) ===
im = Image.new('RGBA', (N, N))
px = im.load()
for y in range(N):
    base = y * N
    for x in range(N):
        i = base + x
        r = 1.0 - math.exp(-acum[0][i] * 0.95)
        g = 1.0 - math.exp(-acum[1][i] * 0.95)
        b = 1.0 - math.exp(-acum[2][i] * 0.95)
        lum = max(r, g, b)
        a = min(1.0, lum * 1.16)
        if a < 0.006:
            px[x, y] = (0, 0, 0, 0)
            continue
        px[x, y] = (int(255 * r ** (1 / 2.2) + .5),
                    int(255 * g ** (1 / 2.2) + .5),
                    int(255 * b ** (1 / 2.2) + .5),
                    int(255 * a + .5))
im = im.resize((S, S), Image.LANCZOS)
im.save('/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Content/Items/Cosmetics/BrilloSolarItem.png')

# VERIFICACIÓN: el glow MUERE antes del borde (cero cortes).
p = im.load()
w, h = im.size
borde = [p[0, k][3] for k in range(h)] + [p[w-1, k][3] for k in range(h)] + \
        [p[k, 0][3] for k in range(w)] + [p[k, h-1][3] for k in range(w)]
print('alfa máxima en el borde:', max(borde), '(debe ser 0)')
print('centro:', p[w//2, h//2])
