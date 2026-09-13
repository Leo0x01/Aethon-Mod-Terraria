#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_supremo_tex_v618.py — v6.18

Genera los assets del AGUJERO NEGRO SUPREMO (el 5to definitivo, la
combinación de los 4 — Umbral + Cósmico + Olvido + Bruma):

1) Sombra de proyectil (76×76 RGBA, patrón v6.09 exacto):
   · SUPREMO — rim DORADO (255,190,80) → carmesí profundo (140,20,30),
     MÁS BRILLANTE que los demás (peak_alpha 78).

2) Icono de bastón (28×30 RGBA, patrón v6.15):
   · SUPREMO — bastón DORADO + mini-agujero con anillo DORADO de BANDAS
     (la fórmula sin(θ·20) del Cósmico, en oro→blanco cálido) + runa
     VIOLETA + doble destello — "supremo": más brillante que los otros.

Salida:
    AethonMod/Content/Projectiles/Cosmic/SupremoBlackHoleProjectile.png
    AethonMod/Content/Weapons/Cosmic/SupremoBlackHoleStaff.png
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


# =====================================================================
#  1) SOMBRA DE PROYECTIL (patrón carmesí v6.09 exacto)
# =====================================================================

def make_shadow(rim_inner, rim_outer, core_r=14.0, peak_r=19.5,
                fade_r=37.0, peak_alpha=70.0):
    """Sombra visual: disco negro + rim de identidad desvaneciéndose."""
    yy, xx = np.mgrid[0:SIZE, 0:SIZE]
    r = np.sqrt((xx - CENTER + 0.5) ** 2 + (yy - CENTER + 0.5) ** 2)

    img = np.zeros((SIZE, SIZE, 4), dtype=np.uint8)

    # --- 1. DISCO NEGRO SÓLIDO (horizonte) ---
    disk = r <= core_r
    img[disk] = (0, 0, 0, 255)

    # --- 2. RIM DE IDENTIDAD (pico → desvanecido) ---
    ring = ~disk & (r <= fade_r)
    rr = r[ring]
    rise = np.clip((rr - core_r) / max(peak_r - core_r, 1e-3), 0.0, 1.0)
    fall = 1.0 - np.clip((rr - peak_r) / max(fade_r - peak_r, 1e-3), 0.0, 1.0)
    alpha = peak_alpha * (rise ** 1.6) * (fall ** 1.1)
    t = np.clip((rr - peak_r) / max(fade_r - peak_r, 1e-3), 0.0, 1.0) ** 0.8
    ci = np.array(rim_inner, dtype=float)
    co = np.array(rim_outer, dtype=float)
    rgb = ci[None, :] * (1.0 - t)[:, None] + co[None, :] * t[:, None]

    img[ring, 0] = np.clip(rgb[:, 0], 0, 255).astype(np.uint8)
    img[ring, 1] = np.clip(rgb[:, 1], 0, 255).astype(np.uint8)
    img[ring, 2] = np.clip(rgb[:, 2], 0, 255).astype(np.uint8)
    img[ring, 3] = np.clip(alpha, 0, 255).astype(np.uint8)

    img[r > fade_r, 3] = 0
    return Image.fromarray(img, 'RGBA')


def gen_shadow():
    # === SUPREMO: rim DORADO → carmesí profundo, MÁS BRILLANTE ===
    supremo = make_shadow(
        rim_inner=(255, 190, 80),     # ORO vivo (el rim del supremo)
        rim_outer=(140, 20, 30),      # carmesí profundo desvanecido
        core_r=14.0, peak_r=20.0, fade_r=38.0, peak_alpha=78.0)
    supremo.save(os.path.join(OUT_DIR_P, 'SupremoBlackHoleProjectile.png'))
    print('OK sombra SupremoBlackHoleProjectile.png')


# =====================================================================
#  2) ICONO DE BASTÓN (28×30, patrón v6.15)
# =====================================================================

W, H = 28, 30
SS = 8
OW, OH = W * SS, H * SS


def staff_base(img, xx, yy, wood_r, wood_g, wood_b):
    """El bastón diagonal (madera tintada), patrón v6.15."""
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


def save_icon(img, name):
    img = np.clip(img, 0, 255).astype(np.uint8)
    out = Image.fromarray(img)
    out = out.resize((W, H), Image.LANCZOS)
    a = np.array(out)
    a[..., 3][a[..., 3] < 28] = 0
    out = Image.fromarray(a)
    path = os.path.join(OUT_DIR_W, name)
    out.save(path)
    print('OK icono', name, out.size, out.mode)


def four_point_star(xx, yy, sx, sy, arm_len, thin):
    """Destello de 4 puntas gaussiano (máscara 0..1)."""
    dxs = np.abs(xx - sx)
    dys = np.abs(yy - sy)
    star_v = np.exp(-(dxs / arm_len) ** 2) * np.exp(-(dys / thin) ** 2)
    star_h = np.exp(-(dys / arm_len) ** 2) * np.exp(-(dxs / thin) ** 2)
    return np.maximum(star_v, star_h)


def gen_supremo_icon():
    """SUPREMO (v6.18): bastón DORADO + mini-agujero con anillo DORADO
    de BANDAS (sin(θ·20) del Cósmico, en oro→blanco cálido) + runa
    VIOLETA + doble destello — el icono MÁS BRILLANTE de los agujeros."""
    yy, xx = np.mgrid[0:OH, 0:OW].astype(float)
    img = np.zeros((OH, OW, 4), dtype=float)

    # --- el bastón (madera DORADA realzada) ---
    staff_base(img, xx, yy, 116, 84, 28)

    # --- EL MINI-AGUJERO (sobre la punta) ---
    cx, cy = 19.5 * SS, 9.5 * SS
    r = np.hypot(xx - cx, yy - cy)

    R = 4.6 * SS

    # halo exterior DORADO tenue — más presente que los demás (110 vs 90)
    halo = np.exp(-((r - 1.45 * R) / (0.85 * R)) ** 2)
    a_halo = halo * 110
    better_halo = a_halo > img[..., 3]
    img[..., 0] = np.where(better_halo, 255, img[..., 0])
    img[..., 1] = np.where(better_halo, 185, img[..., 1])
    img[..., 2] = np.where(better_halo, 90, img[..., 2])
    img[..., 3] = np.maximum(img[..., 3], a_halo)

    # anillo con LAS BANDAS del shader — ORO → BLANCO CÁLIDO
    ring_hi = 1.75 * R
    ex = (xx - cx) / ring_hi
    ey = (yy - cy) / (ring_hi * 0.78)
    re = np.hypot(ex, ey)
    ring_band = np.exp(-((re - 1.0) / 0.28) ** 2)

    bands = 0.5 + 0.5 * np.sin(np.arctan2(ey, ex) * 6.0 + 1.2)
    bands = bands ** 1.4

    ring_r = 205 + 50 * bands          # oro → blanco cálido brillante
    ring_g = 125 + 130 * bands
    ring_b = 25 + 215 * bands
    a_ring = ring_band * 255

    m_ring = a_ring > img[..., 3]
    img[..., 0] = np.where(m_ring, ring_r, img[..., 0])
    img[..., 1] = np.where(m_ring, ring_g, img[..., 1])
    img[..., 2] = np.where(m_ring, ring_b, img[..., 2])
    img[..., 3] = np.maximum(img[..., 3], a_ring)

    # el horizonte negro
    disk = r <= R
    img[..., 0] = np.where(disk, 8, img[..., 0])
    img[..., 1] = np.where(disk, 3, img[..., 1])
    img[..., 2] = np.where(disk, 2, img[..., 2])
    img[..., 3] = np.where(disk, 255, img[..., 3])

    # runa VIOLETA (la corona estelar: rombo + travesaño) a la izquierda
    rx, ry = 11.5 * SS, 13.5 * SS
    rcx = xx - rx
    rcy = yy - ry
    rombo = (np.abs(rcx) / (2.4 * SS) + np.abs(rcy) / (2.4 * SS)) <= 1.0
    travesano = (np.abs(rcx) <= 2.4 * SS) & (np.abs(rcy) <= 0.75 * SS)
    punta = (np.abs(rcx) <= 0.75 * SS) & (np.abs(rcy) <= 2.6 * SS)
    rune = rombo | travesano | punta
    img[..., 0] = np.where(rune, 150, img[..., 0])
    img[..., 1] = np.where(rune, 90, img[..., 1])
    img[..., 2] = np.where(rune, 255, img[..., 2])
    img[..., 3] = np.where(rune, 255, img[..., 3])

    # DOBLE DESTELLO (supremo = más brillante): perla mayor cálida +
    # estrella secundaria violeta menor abajo-izquierda del agujero.
    star1 = four_point_star(xx, yy, 23.5 * SS, 5.5 * SS, 1.85 * SS, 0.42 * SS)
    m_star = star1 > 0.25
    img[..., 0] = np.where(m_star, 255, img[..., 0])
    img[..., 1] = np.where(m_star, 250, img[..., 1])
    img[..., 2] = np.where(m_star, 225, img[..., 2])
    img[..., 3] = np.where(m_star, 255, img[..., 3])

    star2 = four_point_star(xx, yy, 14.5 * SS, 5.5 * SS, 1.1 * SS, 0.30 * SS)
    m_star2 = star2 > 0.30
    img[..., 0] = np.where(m_star2, 185, img[..., 0])
    img[..., 1] = np.where(m_star2, 160, img[..., 1])
    img[..., 2] = np.where(m_star2, 255, img[..., 2])
    img[..., 3] = np.where(m_star2, 255, img[..., 3])

    save_icon(img, 'SupremoBlackHoleStaff.png')


if __name__ == '__main__':
    gen_shadow()
    gen_supremo_icon()
    print('\nv6.18 supremo assets OK')
