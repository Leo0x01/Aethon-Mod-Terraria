#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_bruma_tex_v617.py — v6.17

Assets OBLIGATORIOS del Agujero Negro de la Bruma (7mo agujero, el
nacido con la LIBRERÍA de humo/niebla/bruma procedural):

1) Sombra por defecto del ModProjectile (76×76 RGBA, patrón carmesí
   v6.09): disco negro + rim de identidad TEAL de la estirpe gelida
   (60,200,220) desvaneciendo a azul profundo (15,50,90).

2) Icono del bastón (28×30 RGBA, patrón v6.15): bastón azul-teal +
   mini-agujero con anillo de HUMO suave teal/cian + destello de
   escarcha.

Salida:
    Content/Projectiles/Cosmic/BrumaBlackHoleProjectile.png
    Content/Weapons/Cosmic/BrumaBlackHoleStaff.png
"""

import os
import numpy as np
from PIL import Image

OUT_DIR_P = os.path.join(os.path.dirname(__file__), '..',
                         'AethonMod', 'Content', 'Projectiles', 'Cosmic')
OUT_DIR_W = os.path.join(os.path.dirname(__file__), '..',
                         'AethonMod', 'Content', 'Weapons', 'Cosmic')

SIZE = 76
CENTER = SIZE / 2.0


def make_shadow(rim_inner, rim_outer, core_r=14.0, peak_r=19.5,
                fade_r=37.0, peak_alpha=70.0):
    yy, xx = np.mgrid[0:SIZE, 0:SIZE].astype(float)
    r = np.hypot(xx - CENTER, yy - CENTER)

    img = np.zeros((SIZE, SIZE, 4), dtype=float)

    disk = r <= core_r
    img[..., 0][disk] = 3
    img[..., 1][disk] = 8
    img[..., 2][disk] = 10
    img[..., 3][disk] = 255

    rim = np.exp(-((r - peak_r) / (fade_r - peak_r)) ** 2 * 2.2)
    a_rim = rim * peak_alpha

    t_rim = np.clip((r - core_r) / (fade_r - core_r), 0, 1)
    rim_r = rim_inner[0] + (rim_outer[0] - rim_inner[0]) * t_rim
    rim_g = rim_inner[1] + (rim_outer[1] - rim_inner[1]) * t_rim
    rim_b = rim_inner[2] + (rim_outer[2] - rim_inner[2]) * t_rim

    better = a_rim > img[..., 3]
    img[..., 0] = np.where(better, rim_r, img[..., 0])
    img[..., 1] = np.where(better, rim_g, img[..., 1])
    img[..., 2] = np.where(better, rim_b, img[..., 2])
    img[..., 3] = np.maximum(img[..., 3], a_rim)

    return np.clip(img, 0, 255).astype(np.uint8)


W, H = 28, 30
SS = 8
OW, OH = W * SS, H * SS


def staff_base(img, xx, yy, wood_r, wood_g, wood_b):
    ax, ay = 6.0 * SS, 27.0 * SS
    bx, by = 16.5 * SS, 6.5 * SS
    dx, dy = bx - ax, by - ay
    L = np.hypot(dx, dy)
    ux, uy = dx / L, dy / L
    nx_, ny_ = -uy, ux

    px = xx - ax
    py = yy - ay
    along = px * ux + py * uy
    perp = px * nx_ + py * ny_

    t = np.clip(along / L, 0, 1)
    halfw = (2.1 - 0.9 * t) * SS
    staff = np.abs(perp) <= halfw
    inseg = (along >= -0.5 * SS) & (along <= L + 0.5 * SS)

    base_r = wood_r + 26 * (1 - t)
    base_g = wood_g + 14 * (1 - t)
    base_b = wood_b + 34 * (1 - t)
    edge = np.abs(perp) > (halfw - 0.9 * SS)
    staff_r = np.where(edge, base_r * 0.45, base_r)
    staff_g = np.where(edge, base_g * 0.45, base_g)
    staff_b = np.where(edge, base_b * 0.45, base_b)
    vein = 0.5 + 0.5 * np.sin(along / (2.2 * SS) * np.pi)
    staff_r = staff_r * (0.9 + 0.14 * vein)
    staff_g = staff_g * (0.9 + 0.10 * vein)
    staff_b = staff_b * (0.9 + 0.16 * vein)

    m_staff = staff & inseg
    img[..., 0] = np.where(m_staff, staff_r, img[..., 0])
    img[..., 1] = np.where(m_staff, staff_g, img[..., 1])
    img[..., 2] = np.where(m_staff, staff_b, img[..., 2])
    img[..., 3] = np.where(m_staff, 255, img[..., 3])


def gen_bruma_icon():
    """BRUMA: bastón azul-teal + mini-agujero con anillo de HUMO suave
    (varios blobs suaves teal/cian) + destello de escarcha."""
    yy, xx = np.mgrid[0:OH, 0:OW].astype(float)
    img = np.zeros((OH, OW, 4), dtype=float)

    # --- el bastón (madera oscura azul-teal, la estirpe gelida) ---
    staff_base(img, xx, yy, 34, 56, 72)

    # --- EL MINI-AGUJERO ---
    cx, cy = 19.5 * SS, 9.5 * SS
    r = np.hypot(xx - cx, yy - cy)

    R = 4.6 * SS

    # halo de bruma (GRANDE y suave — el halo de la librería)
    halo = np.exp(-((r - 1.55 * R) / (0.95 * R)) ** 2)
    a_halo = halo * 95
    better_halo = a_halo > img[..., 3]
    img[..., 0] = np.where(better_halo, 70, img[..., 0])
    img[..., 1] = np.where(better_halo, 175, img[..., 1])
    img[..., 2] = np.where(better_halo, 195, img[..., 2])
    img[..., 3] = np.maximum(img[..., 3], a_halo)

    # EL ANILLO DE HUMO: banda ancha y DIFUSA (no un aro fino: es bruma),
    # con tres GRUMOS más brillantes (los fumarelitos).
    ring_hi = 1.75 * R
    ex = (xx - cx) / ring_hi
    ey = (yy - cy) / (ring_hi * 0.78)
    re = np.hypot(ex, ey)
    ang = np.arctan2(ey, ex)

    # Banda ancha difusa (sigma grande = humo).
    ring_band = np.exp(-((re - 1.0) / 0.42) ** 2)
    # Grumos: 6 fumarelitos alrededor.
    grumos = 0.5 + 0.5 * np.cos(ang * 6.0)
    grumos = grumos ** 1.3

    ring_r = 40 + 120 * grumos          # teal → cian iluminado
    ring_g = 130 + 105 * grumos
    ring_b = 150 + 100 * grumos
    a_ring = ring_band * 235

    m_ring = a_ring > img[..., 3]
    img[..., 0] = np.where(m_ring, ring_r, img[..., 0])
    img[..., 1] = np.where(m_ring, ring_g, img[..., 1])
    img[..., 2] = np.where(m_ring, ring_b, img[..., 2])
    img[..., 3] = np.maximum(img[..., 3], a_ring)

    # El horizonte negro (más GRANDE: el humo lo abraza).
    disk = r <= R * 1.05
    img[..., 0] = np.where(disk, 3, img[..., 0])
    img[..., 1] = np.where(disk, 8, img[..., 1])
    img[..., 2] = np.where(disk, 10, img[..., 2])
    img[..., 3] = np.where(disk, 255, img[..., 3])

    # VOLUTA cayendo al núcleo (espiral tenue a la izquierda)
    sp_ang = np.arctan2(yy - cy, xx - cx)
    sp_r = np.hypot(xx - cx, yy - cy) / R
    espiral = np.exp(-((sp_r - (2.6 - 1.6 * ((sp_ang + 3.14) % 6.28) / 6.28)) / 0.30) ** 2)
    m_esp = (espiral > 0.35) & (sp_r < 2.6) & (sp_r > 1.0) & (sp_ang < 0.5)
    img[..., 0] = np.where(m_esp, 60, img[..., 0])
    img[..., 1] = np.where(m_esp, 160, img[..., 1])
    img[..., 2] = np.where(m_esp, 185, img[..., 2])
    img[..., 3] = np.where(m_esp, np.maximum(img[..., 3], 150), img[..., 3])

    # Destello de ESCARCHA (4 puntas blanco-cian)
    sx, sy = 23.5 * SS, 5.5 * SS
    dxs = np.abs(xx - sx)
    dys = np.abs(yy - sy)
    star_v = np.exp(-(dxs / (1.6 * SS)) ** 2) * np.exp(-(dys / (0.42 * SS)) ** 2)
    star_h = np.exp(-(dys / (1.6 * SS)) ** 2) * np.exp(-(dxs / (0.42 * SS)) ** 2)
    star = np.maximum(star_v, star_h)
    m_star = star > 0.25
    img[..., 0] = np.where(m_star, 225, img[..., 0])
    img[..., 1] = np.where(m_star, 250, img[..., 1])
    img[..., 2] = np.where(m_star, 255, img[..., 2])
    img[..., 3] = np.where(m_star, 255, img[..., 3])

    # Guardar
    img = np.clip(img, 0, 255).astype(np.uint8)
    out = Image.fromarray(img)
    out = out.resize((W, H), Image.LANCZOS)
    a = np.array(out)
    a[..., 3][a[..., 3] < 28] = 0
    out = Image.fromarray(a)
    out.save(os.path.join(OUT_DIR_W, 'BrumaBlackHoleStaff.png'))
    print('OK icono BrumaBlackHoleStaff.png', out.size, out.mode)


if __name__ == '__main__':
    # Sombra: rim teal gelido (60,200,220) → azul profundo (15,50,90)
    bruma = make_shadow((60, 200, 220), (15, 50, 90))
    Image.fromarray(bruma).save(os.path.join(
        OUT_DIR_P, 'BrumaBlackHoleProjectile.png'))
    print('OK sombra 76x76: BrumaBlackHoleProjectile.png')

    gen_bruma_icon()
