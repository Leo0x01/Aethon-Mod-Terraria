#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_el plano_arma_v6176.py — v6.176 — EL SPRITE DEL ARMA DEL HAZ
PROYECTADO («Proyección del plano» — la letra: «lo que se tenia que
crear era un arma»).

v5 tras TRES QAs del OJO: el ORDEN DE DIBUJO manda — primero la
cabeza, el ojo y la ceja; el RAYO SE DIBUJA AL FINAL naciendo del
CENTRO del ojo (cruza la frente y SALE — así se lee «el arma dispara
el rayo del ojo» de un vistazo): núcleo blanco-magenta de 2 px,
halo violeta, afinado hacia la punta y con su destello de origen.
"""
import math
import pathlib
from PIL import Image

RAIZ = pathlib.Path(__file__).resolve().parent.parent
DESTINO = RAIZ / "AethonMod" / "Content" / "Weapons" / "Investigaciones" / "ProyeccionDelel plano.png"
PREVIEW = pathlib.Path("/tmp") / "el plano_arma_v6176_preview.png"

N = 34

CONTORNO = (8, 6, 18, 255)
CARNE_BASE = (214, 198, 186, 255)
CARNE_SOMBRA = (172, 150, 134, 255)
CARNE_BRILLO = (244, 234, 224, 255)
ESCLEROTICA = (250, 244, 240, 255)
IRIS_ALTO = (255, 150, 220, 255)
IRIS_MED = (255, 90, 200, 255)
IRIS_PROF = (190, 40, 150, 255)
PUPILA = (30, 10, 30, 255)
ESPECULAR = (255, 240, 252, 255)
VIOL_HALO = (120, 60, 200, 255)
MAG_RAYO = (255, 90, 200, 255)
MAG_NUC = (255, 205, 240, 255)

img = Image.new("RGBA", (N, N), (0, 0, 0, 0))


def px(x, y, c):
    if 0 <= x < N and 0 <= y < N:
        img.putpixel((x, y), c)


# ============ 1) LA CABEZA PÁLIDA (abajo-izquierda, el maniquí) ============
ecx, ecy, rx, ry = 11, 22, 7.6, 9.6
for y in range(12, 33):
    for x in range(3, 20):
        dx = (x - ecx) / rx
        dy = (y - ecy) / ry
        dist = (dx * dx + dy * dy) ** 0.5
        if dist <= 1.0:
            luz = 0.5 + 0.5 * ((dx * 0.8 - dy * 0.6) + 0.5) / 1.5
            if luz > 0.78:
                c = CARNE_BRILLO
            elif luz < 0.34:
                c = CARNE_SOMBRA
            else:
                c = CARNE_BASE
            if dist > 0.93:
                c = CARNE_SOMBRA
            px(x, y, c)

# LA CEJA DEL HAZ (la cuenca GRUESA y OSCURA — se LEE de verdad)
for x in range(5, 20):
    ox = ((x - 12) / 5.2) ** 2
    for y in (12, 13, 14):
        oy = ((y - 21) / 4.0) ** 2
        if ox + oy <= 1.15:
            px(x, y, (58, 42, 40, 255) if y < 14 else (92, 68, 62, 255))

# ============ 2) EL ÚNICO OJO (el MAGENTA DOMINA — el ojo del arma) ============
for y in range(15, 28):
    for x in range(4, 20):
        ox = (x - 12) / 4.8
        oy = (y - 21) / 3.6
        od = (ox * ox + oy * oy) ** 0.5
        if od > 0.84:                                # el aro fino de esclerótica
            continue
        if od <= 0.82:
            px(x, y, ESCLEROTICA if od > 0.66 else IRIS_MED)
for y in range(16, 27):
    for x in range(7, 18):
        ix = (x - 12) / 3.6
        iy = (y - 21) / 3.0
        idd = (ix * ix + iy * iy) ** 0.5
        if idd <= 1.0:
            c = IRIS_MED
            if idd > 0.84:
                c = IRIS_PROF              # el anillo fino del borde
            elif idd < 0.62:
                c = IRIS_ALTO              # EL FUEGO del centro
            px(x, y, c)
# LA PUPILA (4 px — un punto, no un agujero)
for x, y in ((12, 21), (11, 21), (12, 20), (12, 22)):
    px(x, y, PUPILA)
# EL BRILLO ESPECULAR
px(10, 19, ESPECULAR)
px(10, 20, (255, 210, 245, 200))
# LA BOCA (la línea de sombra + la barbilla)
for x in range(8, 15):
    px(x, 29, CARNE_SOMBRA)
for x in range(7, 16):
    px(x, 30, (150, 130, 116, 255))   # la sombra del mentón

# ============ 3) EL RAYO DEL OJO (AL FINAL — nace del CENTRO del ojo,
# cruza la frente y SALE hacia la esquina superior derecha) ============
x0, y0 = 12, 21          # EL CENTRO DEL OJO
x1, y1 = 30, 3           # la punta corta (el rayo es un HAZ, no una hoja)
largo = math.hypot(x1 - x0, y1 - y0)
for paso in range(int(largo * 2)):
    t = paso / (largo * 2)
    x = x0 + (x1 - x0) * t
    y = y0 + (y1 - y0) * t
    alfa = int(255 * (1.0 - t * 0.55))
    # EL GROSOR: 1.5→0.8 px (UN HAZ fino con tres dientes discretos)
    grosor = 1.15 - 0.55 * t + (0.35 if (paso % 11) < 3 else 0.0)
    for dx in (-1, 0, 1):
        for dy in (-1, 0, 1):
            gx, gy = round(x) + dx, round(y) + dy
            dist = math.hypot(dx, dy)
            if dist > grosor:
                continue
            if dist <= 0.75:
                # EL NÚCLEO: BLANCO PURO en el nacimiento (20 px),
                # veteado blanco-rosa después
                c = (255, 255, 255, 255) if paso < 20 else (
                    MAG_NUC if paso % 6 < 2 else MAG_RAYO)
            else:
                c = VIOL_HALO if paso >= 14 else MAG_RAYO
            px(gx, gy, (c[0], c[1], c[2], alfa))
# EL DESTELLO DEL ORIGEN (el ojo ARDE al disparar — la boca del rayo)
for dx in (-1, 0, 1):
    for dy in (-1, 0, 1):
        a = 250 if abs(dx) + abs(dy) < 2 else 190
        px(12 + dx, 21 + dy, (MAG_NUC[0], MAG_NUC[1], MAG_NUC[2], a))
px(12, 21, (255, 255, 255, 255))
# LA PUNTA AGUJA
px(30, 3, (MAG_NUC[0], MAG_NUC[1], MAG_NUC[2], 160))


# ============ 4) EL CONTORNO TERRARIA (la cara — el rayo ya es luz) ============
def cara(x, y):
    if 3 <= x < 20 and 12 <= y < 33:
        dx = (x - ecx) / rx
        dy = (y - ecy) / ry
        return (dx * dx + dy * dy) <= 1.0
    return False


bordes = []
for y in range(N):
    for x in range(N):
        if cara(x, y):
            continue
        ya_luz = 0 <= x < N and 0 <= y < N and img.getpixel((x, y))[3] > 0
        for ddx, ddy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            if cara(x + ddx, y + ddy):
                if not ya_luz:
                    bordes.append((x, y))
                break
for x, y in bordes:
    px(x, y, CONTORNO)

img.save(DESTINO)
img.resize((N * 14, N * 14), Image.NEAREST).save(PREVIEW)
print(f"✔ ProyeccionDelel plano.png forjado v5 ({N}×{N}) — {DESTINO}")
print(f"  preview: {PREVIEW}")
