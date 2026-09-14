#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
mock_eclipse_v624.py — MOCK DEL ECLIPSE v6.24 (sol visible + humo del vacío).

Compone el final del EclipsePrimordialRenderer.Draw() con aproximaciones
de las texturas de la casa (gauss para SoftGlow, disco opaco para
BlackDisk, anillo gauss para Ring, blobs para BrumaFX.Puff) y el ORDEN
EXACTO de capas de v6.24: corona solar (ANTES del repintado) → repintado
negro → limbo + destello → HUMO (alfa) → brasa. Valida la petición del
usuario: "no se le ve el sol" + "que tenga mucho humo o bruma" en el centro.
"""
import numpy as np
from PIL import Image

S = 420  # lienzo
cx, cy = S / 2, S / 2
r = 58.0  # la esfera (px)

WarmWhite = np.array([255, 225, 175], float)
WhiteIncan = np.array([255, 248, 235], float)
SupGold = np.array([255, 190, 80], float)
SmokeViolet = np.array([108, 72, 160], float)
SmokePurple = np.array([140, 96, 186], float)
SmokeEmber = np.array([158, 110, 62], float)
EmberGlow = np.array([150, 95, 205], float)

yy, xx = np.mgrid[0:S, 0:S].astype(float)


def glow(px, py, radius, color, alpha):
    d = np.sqrt((xx - px) ** 2 + (yy - py) ** 2) / radius
    g = np.exp(-(d * d))
    m = g * alpha
    for c in range(3):
        add[c] = np.maximum(add[c], color[c] * g * alpha)
    return


add = np.zeros((3, S, S), float)   # acumulador ADITIVO
alpha_layer = np.zeros((S, S), float)  # acumulador ALFA (el humo)


def hash01(seed, a, b):
    h = (seed * 374761393 + a * 668265263 + b * 1911520717) & 0xFFFFFFFF
    if h & 0x80000000:
        h -= 0x100000000
    h ^= (h >> 13) & 0xFFFFFFFF
    h = (h * 1274126177) & 0xFFFFFFFF
    if h & 0x80000000:
        h -= 0x100000000
    h ^= (h >> 16) & 0xFFFFFFFF
    return (h & 0xFFFFFF) / 16777216.0


def ray_glow(px, py, dx, dy, length, width, color, intensity):
    # haz estirado: distancia al segmento
    ex, ey = px + dx * length, py + dy * length
    vx, vy = ex - px, ey - py
    L2 = vx * vx + vy * vy
    t = np.clip(((xx - px) * vx + (yy - py) * vy) / L2, 0, 1)
    qx = px + vx * t
    qy = py + vy * t
    d = np.sqrt((xx - qx) ** 2 + (yy - qy) ** 2) / (width * 0.5)
    g = np.exp(-(d * d))
    for c in range(3):
        add[c] = np.maximum(add[c], color[c] * g * intensity * 0.6)
    # el nucleo blanco
    g2 = np.exp(-((d / 0.3) ** 2))
    add[0] = np.maximum(add[0], 255 * g2 * intensity * 0.9)
    add[1] = np.maximum(add[1], 250 * g2 * intensity * 0.9)
    add[2] = np.maximum(add[2], 240 * g2 * intensity * 0.9)


time, seed = 0.0, 5

# ==== 15b. aura final (tenue) ====
glow(cx, cy, 3.3 * r, np.array([255, 195, 90]), 0.18)

# ==== 15c. LA CORONA SOLAR (ANTES del repintado) ====
glow(cx, cy, 1.7 * r, WarmWhite, 0.34)     # halo caliente (3.4r tamaño)
glow(cx, cy, 1.25 * r, WhiteIncan, 0.30)   # nucleo interno (2.5r)
# EL BLOOM (2 capas: 1.5x / 0.8x de 2.1r)
glow(cx, cy, 2.1 * r * 0.75, WarmWhite, 0.34)
# LOS 9 STREAMERS radiando del limbo
for i in range(9):
    a = i / 9 * 2 * np.pi + 0.05
    ox, oy = cx + np.cos(a) * r * 1.02, cy + np.sin(a) * r * 1.02
    ln = (0.75 + 0.85 * hash01(seed, 2300 + i, 23)) * r
    ray_glow(ox, oy, np.cos(a), np.sin(a), ln,
             max(6, 0.16 * r), (WarmWhite * 0.6 + SupGold * 0.4), 0.30)

# ==== 16. EL REPINTADO NEGRO (BlackDisk opaco, 2.28r tamaño → 1.14r) ====
d_core = np.sqrt((xx - cx) ** 2 + (yy - cy) ** 2) / (1.14 * r)
black_mask = np.clip(1.25 - d_core, 0, 1)  # opaco al 100% dentro

# ==== 16b. EL LIMBO (aros finos brillantes al borde) ====
# ==== 16c. EL DESTELLO de 4 puntas (anillo de diamante) ====
# (se pintan tras el repintado: al final del additive)

# ==== 16d. EL HUMO DEL VACÍO (blending ALFA — masa sobre el negro) ====
def puff_alpha(px, py, radius, color, alpha):
    d = np.sqrt((xx - px) ** 2 + (yy - py) ** 2) / radius
    g = np.exp(-(d * d * 1.6))
    alpha_layer[:] = np.maximum(alpha_layer, g * alpha)
    for c in range(3):
        smoke_col[c] = np.where(g * alpha > smoke_al[c],
                                color[c], smoke_col[c])
        smoke_al[c] = np.maximum(smoke_al[c], g * alpha)


smoke_col = [np.zeros((S, S)) for _ in range(3)]
smoke_al = [np.zeros((S, S)) for _ in range(3)]

# la masa central
puff_alpha(cx, cy, 0.40 * r, SmokeViolet, 0.34)
# los 6 volátiles orbitando
for i in range(6):
    a = i / 6 * 2 * np.pi
    dist = (0.30 + 0.30 * hash01(seed, 2400 + i, 31)) * r
    px, py = cx + np.cos(a) * dist, cy + np.sin(a) * dist * 0.88
    c = [SmokeEmber, SmokeViolet, SmokePurple][i % 3]
    puff_alpha(px, py, (0.22 + 0.12 * hash01(seed, 2410 + i, 37)) * r, c, 0.30)
# las 2 espirales (volutas simplificadas: 5 puntos)
for s in range(2):
    for k in range(5):
        f = k / 4
        a = s * np.pi + f * 3.6
        dist = (0.92 - 0.74 * f) * r
        puff_alpha(cx + np.cos(a) * dist, cy + np.sin(a) * dist,
                   0.13 * r, SmokePurple if s == 0 else SmokeViolet, 0.22)

# ==== COMPOSICIÓN FINAL ====
bg = np.zeros((S, S, 3), float) + 14  # cielo nocturno tenue
# 1) lo aditivo bajo el negro
img = bg + np.transpose(np.stack(add), (1, 2, 0))
# 2) el repintado negro (opaco)
for c in range(3):
    img[..., c] = img[..., c] * (1 - black_mask) + 6 * black_mask
# 3) EL HUMO sobre el negro (alfa)
sm = np.transpose(np.stack([smoke_col[c] for c in range(3)]), (1, 2, 0))
sm_a = np.maximum.reduce(smoke_al)
for c in range(3):
    img[..., c] = img[..., c] * (1 - sm_a) + sm[..., c] * sm_a
# 4) el limbo + destello + brasa (aditivo ENCIMA)
add2 = np.zeros((3, S, S), float)


def ring_glow(radius, color, alpha, thick=0.05):
    d = np.sqrt((xx - cx) ** 2 + (yy - cy) ** 2) / r
    g = np.exp(-((d - radius) ** 2) / (2 * thick ** 2))
    for c in range(3):
        add2[c] = np.maximum(add2[c], color[c] * g * alpha)


ring_glow(1.035, WarmWhite, 0.44)
ring_glow(1.13, np.array([255, 195, 90]), 0.14)
# el destello de 4 puntas (dos cruces)
for ang in (0.15, np.pi / 2 + 0.15):
    dx, dy = np.cos(ang), np.sin(ang)
    ray_glow(cx, cy, dx, dy, 0.62 * r, 0.30 * r, WarmWhite, 0.20)
    ray_glow(cx, cy, -dx, -dy, 0.62 * r, 0.30 * r, WarmWhite, 0.20)
# la brasa violeta
gd = np.sqrt((xx - cx) ** 2 + (yy - cy) ** 2) / (0.475 * r)
gg = np.exp(-(gd * gd))
for c in range(3):
    add2[c] = np.maximum(add2[c], EmberGlow[c] * gg * 0.10)

img = img + np.transpose(np.stack(add2), (1, 2, 0))

out = np.clip(img, 0, 255).astype(np.uint8)
Image.fromarray(out).save('/tmp/mock_eclipse_v624.png')
print("mock ok -> /tmp/mock_eclipse_v624.png")
