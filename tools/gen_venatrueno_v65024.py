#!/usr/bin/env python3
# gen_venatrueno_v65024.py — v6.50.24 — EL COLMILLO DE VENA TRUENO (v3).
# El mapa ASCII de la v2 reveló los DOS defectos: (1) la condición del
# filo cubría TODO el cuerpo del colmillo en las filas finas (la mitad
# inferior era una mancha azul clara entera) y (2) los tres zigzags con
# halos gruesos se fundían en UNA masa amarilla. LA v3: el COLMILLO
# DOMINA (cuerpo navy oscuro, filo de SOLO 1 px solo cuando el cuerpo
# es ancho, punta convergente real) y EL TRÍO son TRES VENAS PARALELAS
# SEPARADAS (columnas x=4/9/14 con meandro angular apretado ±2-3 px —
# como las venas del costado del dragón de Coralite): naranja/amarillo/
# amarillo, núcleo 1 px, halo 1 px. Paleta: ThunderveinDragon.cs.

from PIL import Image, ImageDraw

def nueva(w, h):
    return Image.new("RGBA", (w, h), (0, 0, 0, 0))

def hsave(im, path):
    im.save(path)
    print("  ✓", path)

OUT_W = "/home/z/my-project/AethonMod/AethonMod/Content/Weapons/Cosmic"
OUT_P = "/home/z/my-project/AethonMod/AethonMod/Content/Projectiles/Cosmic"

AMARILLO = (255, 202, 101)
NARANJA = (239, 134, 32)      # el 219,114,22 de Coralite aclarado 1 paso SOLO para el icono
NUCLEO = (255, 246, 215)


def colmillo_vena_trueno():
    im = nueva(28, 30)
    d = ImageDraw.Draw(im)

    # ==============================================================
    # EL COLMILLO (dominante, centro-derecha): base ancha con banda
    # dorada, cuerpo navy, PUNTA convergente abajo.
    # ==============================================================
    cx = 21.0          # centro de la base
    y_base = 3
    punta = (17, 29)   # la punta

    for y in range(y_base, 30):
        u = (y - y_base) / (29.0 - y_base)      # 0 base → 1 punta
        w = 4.6 * (1.0 - u) ** 1.1              # el medio-ancho MUERE en la punta
        if w < 0.4: w = 0.4
        xc = cx + (punta[0] - cx) * (u ** 1.4)  # la curva hacia la punta
        x0 = int(round(xc - w))
        x1 = int(round(xc + w))
        ancho_fila = x1 - x0 + 1
        for x in range(max(0, x0), min(28, x1 + 1)):
            if ancho_fila >= 4 and x == x1:
                # el FILO: 1 px frío SOLO en filas anchas
                d.point((x, y), fill=(96, 112, 142, 255))
            elif ancho_fila >= 4 and x == x0:
                # la SOMBRA: 1 px en el lado izquierdo
                d.point((x, y), fill=(12, 16, 28, 255))
            else:
                # el CUERPO: navy que se oscurece hacia la punta
                k = int(30 - 14 * u)
                d.point((x, y), fill=(k, k + 7, k + 20, 255))

    # LA BANDA DORADA (la encía viva del colmillo)
    for y in range(0, 3):
        u = y / 2.0
        w = 4.8 * (1.0 - 0.15 * u)
        x0 = int(round(cx - w)); x1 = int(round(cx + w))
        for x in range(max(0, x0), min(28, x1 + 1)):
            brillo = 255 if abs(x - cx) < 2 else 190
            d.point((x, y), fill=(AMARILLO[0], AMARILLO[1], AMARILLO[2], brillo))

    # ==============================================================
    # EL TRÍO DE VENAS (TRES rayos PARALELOS, separados, en el costado
    # izquierdo — las venas del dragón): meandro angular apretado.
    # ==============================================================
    def vena(x_ini, color, semilla):
        import random
        rng = random.Random(semilla)
        # PASADA 1: los segmentos (se guardan); PASADA 2: halos TODOS;
        # PASADA 3: núcleos TODOS (los núcleos NUNCA quedan bajo un halo
        # — la lección del mapa ASCII: el halo del segmento siguiente
        # lavaba el núcleo del anterior y todo era una mancha Y).
        segmentos = []
        x = float(x_ini)
        y = 1
        while y < 28:
            dy = rng.randint(3, 5)
            dx = rng.randint(-3, 3)
            x2 = max(2.0, min(15.0, x + dx))
            y2 = min(28, y + dy)
            segmentos.append((x, y, x2, y2))
            x, y = x2, y2
        # los halos
        for (xa, ya, xb, yb) in segmentos:
            pasos = int(max(abs(xb - xa), abs(yb - ya)) * 2) + 1
            for s in range(pasos + 1):
                t = s / pasos
                fx = int(round(xa + (xb - xa) * t))
                fy = int(round(ya + (yb - ya) * t))
                for hx in (-1, 0, 1):
                    for hy in (-1, 0, 1):
                        if hx == 0 and hy == 0: continue
                        if 0 <= fx + hx < 28 and 1 <= fy + hy < 30:
                            d.point((fx + hx, fy + hy),
                                    fill=(color[0], color[1], color[2], 70))
        # los núcleos (DESPUÉS de todos los halos)
        for (xa, ya, xb, yb) in segmentos:
            pasos = int(max(abs(xb - xa), abs(yb - ya)) * 2) + 1
            for s in range(pasos + 1):
                t = s / pasos
                fx = int(round(xa + (xb - xa) * t))
                fy = int(round(ya + (yb - ya) * t))
                if 0 <= fx < 28 and 1 <= fy < 30:
                    d.point((fx, fy), fill=NUCLEO + (255,))

    vena(4, NARANJA, 7)     # la vena [0]: NARANJA
    vena(9, AMARILLO, 19)   # la vena [1]: AMARILLA
    vena(14, AMARILLO, 33)  # la vena [2]: AMARILLA (desalineada)

    # LAS CHISPAS: puntos calientes mínimos (r=2) SOLO en las puntas
    for (sx, sy) in [(4, 27), (9, 27), (14, 27), (17, 28)]:
        d.ellipse((sx - 2, sy - 2, sx + 2, sy + 2), fill=(255, 232, 170, 190))
        d.ellipse((sx - 1, sy - 1, sx + 1, sy + 1), fill=(255, 250, 230, 255))

    hsave(im, f"{OUT_W}/VenaTruenoStaff.png")


# EL PROYECTIL (16×16): invisible en juego (PreDraw false) — solo el
# núcleo de chispa para que exista el sprite autoload de tML.
def chispa():
    im = nueva(16, 16)
    d = ImageDraw.Draw(im)
    r, g, b = AMARILLO
    for rr in range(6, -1, -1):
        a = 0 if rr > 3 else (200 if rr > 1 else 255)
        d.ellipse((8 - rr, 8 - rr, 8 + rr, 8 + rr),
                  fill=(min(255, r + 100), min(255, g + 100), min(255, b + 100), a))
    hsave(im, f"{OUT_P}/VenaTruenoProjectile.png")


colmillo_vena_trueno()
chispa()
print("OK v6.50.24 — iconos v3 (colmillo dominante + 3 venas paralelas)")
