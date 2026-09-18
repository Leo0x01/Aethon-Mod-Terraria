#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_tajos_v640.py — LOS ASSETS DEL BASTÓN DE LOS TAJOS ASTRALES (v6.40).

Dos PNGs deterministas (random.Random de semilla fija — cero azar del
sistema, el patrón de la casa):
  · TajosAstralesStaff.png  (30×30) — el icono: el bastón de la casa
    (madero diagonal + engarce dorado) coronado por UNA MEDIA LUNA
    blanca-caliente con halo dorado (la cabeza temática del arma).
  · TajoAstralProjectile.png (76×76) — el glow de identidad: el creciente
    blanco con su eco interior y su halo dorado (la firma del proyectil).

El estilo: el de gen_v636/v638 (pixel-art de la casa a 30×30 — cabezas
GRUESAS Y PLANAS que leen bien a pequeña escala; el glow 76×76 suave).
"""
import random
import math
from PIL import Image, ImageDraw
import os

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT_CONTENT = RAIZ

rng = random.Random(64071)  # semilla fija: regenerable byte a byte

# ==================================================================
#  1. EL ICONO (30×30) — el bastón con cabeza de media luna
# ==================================================================
icono = Image.new("RGBA", (30, 30), (0, 0, 0, 0))
d = ImageDraw.Draw(icono)

# --- EL MADERO (el patrón de la casa: diagonal, 2px, claro→oscuro) ---
# Del engarce (arriba-derecha) al puño (abajo-izquierda).
for t in range(26):
    # posición a lo largo de la diagonal
    x = 22 - t * 0.62
    y = 4 + t * 0.72
    # grosor 2px con dos tonos (lado iluminado + sombra)
    d.point((round(x), round(y)), fill=(146, 104, 62, 255))
    d.point((round(x), round(y) + 1), fill=(96, 66, 38, 255))

# --- EL ENGAZCE dorado (donde la luna se monta) ---
d.ellipse([19, 4, 24, 9], outline=(255, 214, 130, 255), width=1)
d.point((21, 6), fill=(255, 250, 235, 255))
d.point((22, 7), fill=(255, 250, 235, 255))

# --- LA CABEZA: UNA MEDIA LUNA blanca-caliente (la firma del arma) ---
# Un creciente = disco blanco MENOS disco desplazado (la mordida).
# Centro del creciente (a la derecha del engarce, arriba):
cx, cy, r = 24, 8, 5.5
# La mordida: disco desplazado hacia abajo-derecha:
bx, by, br = 26.5, 10.5, 5.0
for py in range(2, 15):
    for px in range(17, 30):
        dist = math.hypot(px - cx, py - cy)
        mord = math.hypot(px - bx, py - by)
        if dist <= r and mord > br:
            # EL NÚCLEO: blanco-caliente (la punta del filo más blanca)
            nucleo = dist > r - 2.2
            col = (255, 251, 240, 255) if nucleo else (255, 236, 190, 255)
            d.point((px, py), fill=col)

# --- EL HALO dorado (3 puntitos alrededor de la luna, el brillo) ---
halo_pts = [(17, 4), (27, 3), (20, 13)]
for hx, hy in halo_pts:
    d.point((hx, hy), fill=(255, 214, 130, 160))

# --- EL PUNTO DE PODER en el puño (la gema de la casa) ---
d.point((7, 23), fill=(255, 226, 150, 255))
d.point((6, 22), fill=(255, 226, 150, 180))

icono.save(os.path.join(OUT_CONTENT, "Content/Weapons/Cosmic/TajosAstralesStaff.png"))
print("icono 30x30 OK")

# ==================================================================
#  2. EL GLOW DEL PROYECTIL (76×76) — el creciente de identidad
# ==================================================================
glow = Image.new("RGBA", (76, 76), (0, 0, 0, 0))
gd = ImageDraw.Draw(glow)

CX, CY = 38, 38
RADIO = 24          # el radio del creciente
ANCHO = 5.5         # el grosor máximo (al centro del arco)
SPAN = math.radians(150)  # la abertura (media luna generosa)
A0 = -math.radians(75)    # el arranque (arriba-izquierda)

# El creciente: N segmentos de cápsula con PERFIL DE LENTE (el mismo
# perfil de TajoLib — grosor máximo al centro, fino en las puntas).
N = 44
for i in range(N):
    u = (i + 0.5) / N
    gu = math.sin(math.pi * u) ** 0.55
    w = max(0.9, ANCHO * gu)
    a0 = A0 + SPAN * (i / N)
    a1 = A0 + SPAN * ((i + 1) / N)
    p0 = (CX + math.cos(a0) * RADIO, CY + math.sin(a0) * RADIO)
    p1 = (CX + math.cos(a1) * RADIO, CY + math.sin(a1) * RADIO)
    # El HALO (dorado, ancho):
    gd.line([p0, p1], fill=(255, 214, 130, 44), width=max(2, int(w * 3.2)))
    # El NÚCLEO (blanco-caliente, limpio):
    gd.line([p0, p1], fill=(255, 251, 240, 235), width=max(1, int(w)))

# El ECO interior (el rastro fantasma a radio ×0.88):
for i in range(N):
    u = (i + 0.5) / N
    gu = math.sin(math.pi * u) ** 0.55
    w = max(0.6, ANCHO * 0.30 * gu)
    a0 = A0 + SPAN * (i / N)
    a1 = A0 + SPAN * ((i + 1) / N)
    p0 = (CX + math.cos(a0) * RADIO * 0.88, CY + math.sin(a0) * RADIO * 0.88)
    p1 = (CX + math.cos(a1) * RADIO * 0.88, CY + math.sin(a1) * RADIO * 0.88)
    gd.line([p0, p1], fill=(255, 244, 210, 60), width=max(1, int(w)))

# LAS PUNTAS prendidas (los puntos de luz en los extremos del arco):
for ang in (A0, A0 + SPAN):
    p = (CX + math.cos(ang) * RADIO, CY + math.sin(ang) * RADIO)
    gd.ellipse([p[0] - 3, p[1] - 3, p[0] + 3, p[1] + 3], fill=(255, 251, 240, 255))

# Suavizado con un desenfoque leve + re-contraste (el glow de la casa
# es suave en el halo, nítido en el filo — 2 pasadas):
from PIL import ImageFilter
suave = glow.filter(ImageFilter.GaussianBlur(1.1))
# compone: el suave (halo) DEBAJO del nítido (filo)
final = Image.new("RGBA", (76, 76), (0, 0, 0, 0))
final.alpha_composite(suave)
final.alpha_composite(glow)
final.save(os.path.join(OUT_CONTENT, "Content/Projectiles/Cosmic/TajoAstralProjectile.png"))
print("glow 76x76 OK")

# ==================================================================
#  3. LA HOJA DE CONTACTO (verificación VLM)
# ==================================================================
hoja = Image.new("RGB", (280, 130), (16, 16, 22))
hd = ImageDraw.Draw(hoja)
# el icono ×3 (vecino — sin interpolar para ver los píxeles)
big = icono.resize((90, 90), Image.NEAREST)
hoja.paste(big, (12, 22), big)
# el glow ×1
hoja.paste(final, (130, 12), final)
hd.text((12, 4), "ICONO x3 (30x30)", fill=(220, 220, 220))
hd.text((130, 90), "GLOW 76x76", fill=(220, 220, 220))
os.makedirs(os.path.join(RAIZ, "research/tajos_v640/assets"), exist_ok=True)
hoja.save(os.path.join(RAIZ, "research/tajos_v640/assets/hoja_contacto.png"))
print("hoja de contacto OK")
