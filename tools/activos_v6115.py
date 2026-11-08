#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
activos_v6115.py — LOS ACTIVOS DE LA v6.115 (la casa: PIL ×4 SS, el Taller).

  · MundoInteriorEternoItem.png (30×30) — EL ÍTEM DE LA PUERTA: el
    grimorio ABIERTO (las páginas doradas) devorando un remolino de
    bruma violeta que nace de su boca — el mundo interior naciendo del
    trago: la espiral del vacío, la mandorla del umbral y las tres
    motas de esencia subiendo al libro.

  · AltarPrismaTile.png (54×324 = 6 marcos de 54×54) — EL PRISMA DEL
    ETERNO (el altar de colores del mundo interior): el pedestal de
    amatista con el filo dorado de las páginas, EL PRISMA CRISTALINO
    flotando en el corazón (triángulo facetado, núcleo blanco-violeta)
    y EL ABANICO ESPECTRAL — el haz dorado que entra por arriba y se
    PARTE en siete bandas de colores (la casa: violeta dominante, el
    oro del veredicto y los cinco acentos del prisma) que respiran y
    se desplazan marco a marco — el prisma CAMBIA de colores al vivo.
"""
from PIL import Image, ImageDraw, ImageFilter
import math

SS = 4

# ======================================================================
#  1. MUNDO INTERIOR DEL ETERNO — el ítem (30×30)
# ======================================================================
N = 30
img = Image.new("RGBA", (N * SS, N * SS), (0, 0, 0, 0))
dr = ImageDraw.Draw(img)
S = N * SS

# EL HALO DEL VACÍO detrás de todo (el resplandor hondo del libro)
for r in range(int(S * 0.48), 0, -1):
    f = 1 - r / (S * 0.48)
    col = (26 + int(20 * f), 14 + int(16 * f), 44 + int(24 * f), 70 + int(60 * f))
    dr.ellipse([S / 2 - r, S / 2 - r, S / 2 + r, S / 2 + r], fill=col)

# EL GRIMORIO ABIERTO (visto de frente, en la mitad baja): dos páginas
# en V con el lomo dorado — la boca del libro mira ARRIBA.
libx, liby = S / 2, S * 0.66
libw, libh = S * 0.36, S * 0.20
# las páginas: blanco-cálido con el borde dorado, inclinadas en V
for lado in (-1, 1):
    pts = [
        (libx, liby),                                    # el lomo (la boca)
        (libx + lado * libw * 0.92, liby - libh * 0.55),  # la esquina alta
        (libx + lado * libw, liby + libh * 0.55),        # la esquina baja externa
        (libx, liby + libh * 0.72),                      # el pie del lomo
    ]
    dr.polygon(pts, fill=(255, 240, 214, 255), outline=(212, 168, 84, 255))
    # una línea de texto (el renglón rúnico)
    dr.line([(libx + lado * S * 0.05, liby - S * 0.01),
             (libx + lado * S * 0.22, liby - S * 0.055)],
            fill=(178, 138, 96, 255), width=SS - 2)
# el lomo dorado
dr.line([(libx, liby - libh * 0.30), (libx, liby + libh * 0.72)],
        fill=(232, 190, 110, 255), width=SS - 1)
# la tapa del libro asomando bajo las páginas
dr.polygon([(libx - libw, liby + libh * 0.62),
            (libx + libw, liby + libh * 0.62),
            (libx + libw * 0.86, liby + libh * 1.05),
            (libx - libw * 0.86, liby + libh * 1.05)],
           fill=(64, 34, 92, 255), outline=(212, 168, 84, 255))

# EL REMOLINO (la devoración): la espiral de bruma violeta que sube del
# centro del ítem a la BOCA del libro — el mundo entero entrando.
cx, cy = S / 2, S * 0.385
for i in range(3):
    r0 = S * (0.265 - i * 0.075)
    paso = S * 0.020
    puntos = []
    for k in range(34):
        a = math.pi * 1.5 + k * 0.24 - i * 0.7
        r = r0 * (1 - k / 40.0)
        x = cx + math.cos(a) * r
        y = cy + math.sin(a) * r * 0.8 + k * paso * 0.34
        puntos.append((x, y))
    col = [(150, 84, 235, 235), (196, 150, 255, 215), (122, 78, 188, 235)][i]
    dr.line(puntos, fill=col, width=SS + (1 if i == 0 else 0), joint="curve")

# LA MANDORLA DEL UMBRAL (el mundo que nace en la boca): la VESICA DE
# VERDAD — DOS ARCOS CIRCULARES que se cortan (la construcción clásica
# de la gloria: puntos AGUDOS a los lados, el ojo de la v6.115 lo pidió
# dos veces) con el núcleo del vacío dentro.
mx, my = S / 2, S * 0.552
mW, mH = S * 0.185, S * 0.085
for r in range(int(S * 0.105), 0, -1):
    f = 1 - r / (S * 0.105)
    col = (26 + int(120 * f), 14 + int(66 * f), 44 + int(180 * f), 200)
    dr.ellipse([mx - r * 1.6, my - r, mx + r * 1.6, my + r], fill=col)


def vesica_puntos(cx, cy, W, H, n=48):
    """La almendra CLÁSICA: dos arcos de círculo que se cortan en las
    puntas (cx±W, cy) — puntos AGUDOS garantizados por geometría (los
    arcos llegan tangenciales al eje: k=(W²−H²)/2H, r=k+H)."""
    k = (W * W - H * H) / (2.0 * H)
    r = k + H
    pts = []
    # el arco de ARRIBA: círculo centrado (cx, cy+k), la parte superior.
    for i in range(n + 1):
        x = -W + 2.0 * W * i / n
        y = (cy + k) - math.sqrt(max(r * r - x * x, 0.0))
        pts.append((cx + x, y))
    # el arco de ABAJO: círculo centrado (cx, cy−k), la parte inferior.
    for i in range(n + 1):
        x = W - 2.0 * W * i / n
        y = (cy - k) + math.sqrt(max(r * r - x * x, 0.0))
        pts.append((cx + x, y))
    return pts


# el contorno dorado grueso (la gloria que no se pierde) + su corazón
# blanco — con la VESICA de puntas agudas.
# (El ojo lo pidió tres veces: a 30 px el CONTORNO fino se vuelve ruido
# sobre el remolino — LA CURA: la almendra RELLENA de oro con su ANILLO
# OSCURO de contraste y los picos SÓLIDOS — la forma lee a cualquier
# escala.)
poliGrande = vesica_puntos(mx, my, mW * 1.16, mH * 1.30)
poli = vesica_puntos(mx, my, mW, mH)
poliChico = vesica_puntos(mx, my, mW * 0.46, mH * 0.44)
# 1 · el anillo oscuro (el contraste que separa la gloria del remolino).
dr.polygon(poliGrande, fill=(24, 11, 42, 255))
# 2 · la almendra DORADA sólida (con sus picos — parte de la forma).
dr.polygon(poli, fill=(255, 214, 130, 255))
# 3 · el corazón blanco (la llama del umbral).
dr.polygon(poliChico, fill=(255, 248, 232, 255))
# 4 · LOS PICOS: triángulos dorados sólidos fundidos a las puntas.
for lado in (-1, 1):
    dr.polygon([
        (mx + lado * mW * 0.70, my - mH * 0.78),
        (mx + lado * mW * 1.42, my),
        (mx + lado * mW * 0.70, my + mH * 0.78),
    ], fill=(255, 214, 130, 255))
    dr.polygon([
        (mx + lado * mW * 1.06, my - mH * 0.22),
        (mx + lado * mW * 1.42, my),
        (mx + lado * mW * 1.06, my + mH * 0.22),
    ], fill=(255, 248, 232, 255))

# LAS TRES ESENCIAS (las motas que alimentan el trago — con su halo
# oscuro para que BRILLEN sobre el remolino).
for i, (ex, ey) in enumerate([(-0.31, 0.28), (0.34, 0.35), (0.13, -0.075)]):
    px, py = S / 2 + ex * S, S / 2 + ey * S
    # el halo oscuro (el contraste que pedía el ojo).
    dr.ellipse([px - SS * 1.7, py - SS * 1.7, px + SS * 1.7, py + SS * 1.7],
               fill=(30, 14, 50, 235))
    dr.ellipse([px - SS * 1.1, py - SS * 1.1, px + SS * 1.1, py + SS * 1.1],
               fill=(255, 236, 170, 255))

# LA ESTRELLA DEL UMBRAL (la chispa de cuatro puntas sobre la espiral)
sx, sy = S / 2, S * 0.16
dr.line([sx - SS * 2.2, sy, sx + SS * 2.2, sy], fill=(226, 206, 255, 245), width=SS - 1)
dr.line([sx, sy - SS * 2.2, sx, sy + SS * 2.2], fill=(226, 206, 255, 245), width=SS - 1)

img = img.resize((N, N), Image.LANCZOS)
img.save("AethonMod/Content/Items/Llamados/MundoInteriorEternoItem.png")
print("✓ MundoInteriorEternoItem.png (30×30)")

# ======================================================================
#  2. EL PRISMA DEL ETERNO — el tile animado (6 marcos de 54×54)
# ======================================================================
F = 54            # el marco: 3×3 tiles (18 + 16 + 16 + 22 = 54 de alto)
MARCOS = 6
W, H = F * SS, F * SS

# LA PALETA DEL PRISMA (la casa + los colores): el abanico — violeta
# dominante, el oro del veredicto y los cinco acentos vivos.
ESPECTRO = [
    (150, 84, 235),   # violeta del vacío (el señor del abanico)
    (196, 112, 255),  # lila vivo
    (110, 170, 235),  # azul del cielo
    (110, 225, 205),  # cian de página
    (150, 230, 120),  # verde de tinta
    (250, 210, 100),  # oro del veredicto
    (250, 140, 110),  # brasa del eclipse
]

tira = Image.new("RGBA", (W, H * MARCOS), (0, 0, 0, 0))

for frame in range(MARCOS):
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    dr = ImageDraw.Draw(img)
    fase = frame / MARCOS

    # --- EL PEDIMENTO DE AMATISTA (las dos filas de abajo) ---
    py0 = H - 10 * SS
    dr.polygon([(W * 0.16, H), (W * 0.84, H),
                (W * 0.78, py0 + 2 * SS), (W * 0.22, py0 + 2 * SS)],
               fill=(64, 34, 96, 255), outline=(212, 168, 84, 255))
    # la mesa dorada (el filo de la página)
    dr.polygon([(W * 0.12, py0 + 2 * SS), (W * 0.88, py0 + 2 * SS),
                (W * 0.84, py0), (W * 0.16, py0)],
               fill=(212, 168, 84, 255))
    dr.polygon([(W * 0.16, py0), (W * 0.84, py0),
                (W * 0.80, py0 - 2 * SS), (W * 0.20, py0 - 2 * SS)],
               fill=(46, 24, 72, 255), outline=(150, 110, 200, 255))
    # LAS DOS VELAS de la casa en el pedestal (la herencia del altar)
    for lado in (-1, 1):
        vx = W / 2 + lado * W * 0.28
        dr.rectangle([vx - SS, py0 - 8 * SS, vx + SS, py0 - 2 * SS],
                     fill=(240, 226, 198, 255), outline=(150, 110, 200, 255))
        # la llama viva (late con la fase)
        llam = 3.2 + 1.1 * math.sin(fase * math.pi * 2 + (1 if lado > 0 else 0) * math.pi)
        dr.polygon([(vx, py0 - 8 * SS - llam * SS),
                    (vx - SS * 0.8, py0 - 8 * SS),
                    (vx + SS * 0.8, py0 - 8 * SS)],
                   fill=(255, 214, 130, 255))

    # --- EL PRISMA CRISTALINO (flotando sobre el pedestal) ---
    bob = math.sin(fase * math.pi * 2) * 1.2 * SS
    px, py = W / 2, H * 0.40 + bob
    pw, ph = W * 0.20, H * 0.26
    # el resplandor del corazón
    for r in range(int(pw * 1.7), 0, -2):
        f = 1 - r / (pw * 1.7)
        col = (150 + int(70 * f), 84 + int(90 * f), 235, int(38 + 42 * f))
        dr.ellipse([px - r, py - r * 1.25, px + r, py + r * 1.25], fill=col)
    # el triángulo facetado (el prisma de verdad)
    A = (px, py - ph)                       # la cima
    B = (px - pw, py + ph * 0.62)           # la base izquierda
    C = (px + pw, py + ph * 0.62)           # la base derecha
    dr.polygon([A, B, C], fill=(84, 48, 138, 255), outline=(226, 206, 255, 255))
    # LAS FACETAS (la cara izquierda clara, la derecha honda, la central viva)
    Mab = ((A[0] + B[0]) / 2, (A[1] + B[1]) / 2)
    Mac = ((A[0] + C[0]) / 2, (A[1] + C[1]) / 2)
    Mbc = ((B[0] + C[0]) / 2, (B[1] + C[1]) / 2)
    dr.polygon([A, Mab, Mbc, Mac], fill=(150, 84, 235, 235))
    dr.polygon([Mab, B, Mbc], fill=(104, 62, 168, 235))
    dr.polygon([Mac, Mbc, C], fill=(66, 38, 112, 235))
    # EL GLINT (la chispa que viaja por las facetas — el marco vivo)
    glint_t = fase
    gp = [(A, Mab), (Mab, B), (B, Mbc), (Mbc, C), (C, Mac), (Mac, A)][int(glint_t * 6) % 6]
    gx = gp[0][0] + (gp[1][0] - gp[0][0]) * 0.5
    gy = gp[0][1] + (gp[1][1] - gp[0][1]) * 0.5
    dr.line([gx - SS * 1.4, gy - SS * 1.4, gx + SS * 1.4, gy + SS * 1.4],
            fill=(255, 246, 225, 255), width=SS - 1)

    # --- EL HAZ QUE ENTRA (la luz dorada del Eterno, desde arriba) ---
    haz_alfa = 150 + int(70 * math.sin(fase * math.pi * 2))
    dr.polygon([(px - SS * 1.6, 0), (px + SS * 1.6, 0),
                (px + SS * 0.9, A[1] + 2 * SS), (px - SS * 0.9, A[1] + 2 * SS)],
               fill=(255, 226, 150, haz_alfa))

    # --- EL ABANICO ESPECTRAL (la luz PARTIDA: siete bandas que
    #     respiran — cada marco las recorre) ---
    for b in range(7):
        ang = math.pi * (0.18 + 0.64 * b / 6.0)          # el abanico 32°→148°
        largo = H * (0.30 + 0.06 * math.sin(fase * math.pi * 2 + b * 0.9))
        x1 = px + math.cos(ang) * pw * 0.9
        y1 = py + math.sin(ang) * ph * 0.5
        x2 = px + math.cos(ang) * (pw * 0.9 + largo)
        y2 = py + math.sin(ang) * (ph * 0.5 + largo * 0.62)
        brillo = 0.55 + 0.45 * math.sin(fase * math.pi * 2 + b * 1.25)
        base = ESPECTRO[(b + frame) % 7]
        col = (base[0], base[1], base[2], int(120 + 110 * brillo))
        dr.line([(x1, y1), (x2, y2)], fill=col, width=SS + 1)
        # la mota al final de la banda (la chispa del color)
        dr.ellipse([x2 - SS, y2 - SS, x2 + SS, y2 + SS],
                   fill=(base[0], base[1], base[2], 255))

    # --- LAS MOTAS DEL UMBRAL (el polvo de colores alrededor) ---
    for i in range(7):
        a = fase * math.pi * 2 + i * math.pi * 2 / 7
        mx = px + math.cos(a) * W * 0.36
        my = py + math.sin(a) * H * 0.27
        base = ESPECTRO[i]
        dr.ellipse([mx - SS * 0.7, my - SS * 0.7, mx + SS * 0.7, my + SS * 0.7],
                   fill=(base[0], base[1], base[2], 200))

    tira.paste(img, (0, frame * H))

tira = tira.resize((F, F * MARCOS), Image.LANCZOS)
tira.save("AethonMod/Content/Tiles/AltarPrismaTile.png")
print("✓ AltarPrismaTile.png (54×%d — %d marcos)" % (F * MARCOS, MARCOS))
