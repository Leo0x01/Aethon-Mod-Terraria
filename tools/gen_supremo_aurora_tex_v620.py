#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_supremo_aurora_tex_v620.py — v6.20

Genera los assets del AGUJERO NEGRO SUPREMO AURORA (el hermano del
Supremo v6.18 con el GRADIENTE DEL USUARIO: centro negro → morado
cerca del centro → azul al medio → dorado en los bordes; el original
queda INTACTO):

1) Sombra de proyectil (76×76 RGBA, patrón v6.09 exacto):
   · AURORA — rim MORADO (185,105,255) → azul profundo (48,85,205),
     MÁS BRILLANTE que los demás (peak_alpha 78, como el Supremo).

2) Icono de bastón (28×30 RGBA, patrón v6.15/v6.18):
   · AURORA — bastón MORADO + mini-agujero con el GRADIENTE AURORA
     pintado en el anillo (morado cerca del núcleo → azul → dorado en
     el borde) + runa DORADA + doble destello frío.

Salida:
    AethonMod/Content/Projectiles/Cosmic/SupremoAuroraBlackHoleProjectile.png
    AethonMod/Content/Weapons/Cosmic/SupremoAuroraBlackHoleStaff.png
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
#  1) SOMBRA DE PROYECTIL (patrón v6.09 exacto)
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
    # === AURORA: rim MORADO → azul profundo, MÁS BRILLANTE (como el
    #     Supremo) — el gradiente empieza en morado junto al núcleo ===
    aurora = make_shadow(
        rim_inner=(185, 105, 255),    # MORADO vivo (junto al núcleo)
        rim_outer=(48, 85, 205),      # azul profundo desvanecido
        core_r=14.0, peak_r=20.0, fade_r=38.0, peak_alpha=78.0)
    aurora.save(os.path.join(OUT_DIR_P, 'SupremoAuroraBlackHoleProjectile.png'))
    print('OK sombra SupremoAuroraBlackHoleProjectile.png')


# =====================================================================
#  2) ICONO DE BASTÓN (28×30, patrón v6.15/v6.18)
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


def gen_aurora_icon():
    """AURORA (v6.20): bastón MORADO + mini-agujero con el GRADIENTE
    AURORA en el anillo (morado cerca del núcleo → azul → dorado en el
    borde, pintado por DISTANCIA RADIAL real) + runa DORADA + doble
    destello frío — el hermano del alba polar."""
    yy, xx = np.mgrid[0:OH, 0:OW].astype(float)
    img = np.zeros((OH, OW, 4), dtype=float)

    # --- el bastón (madera MORADA realzada) ---
    staff_base(img, xx, yy, 84, 58, 132)

    # --- EL MINI-AGUJERO (sobre la punta) ---
    cx, cy = 19.5 * SS, 9.5 * SS
    r = np.hypot(xx - cx, yy - cy)

    R = 4.6 * SS

    # halo exterior DORADO tenue — el borde del gradiente (como el
    # halo del Supremo: 110 de presencia)
    halo = np.exp(-((r - 1.45 * R) / (0.85 * R)) ** 2)
    a_halo = halo * 110
    better_halo = a_halo > img[..., 3]
    img[..., 0] = np.where(better_halo, 255, img[..., 0])
    img[..., 1] = np.where(better_halo, 195, img[..., 1])
    img[..., 2] = np.where(better_halo, 90, img[..., 2])
    img[..., 3] = np.maximum(img[..., 3], a_halo)

    # EL ANILLO CON EL GRADIENTE AURORA — cada píxel del anillo toma su
    # color de su DISTANCIA RADIAL: morado cerca → azul → dorado en el
    # borde (la petición hecha icono), con las bandas del Cósmico encima.
    ring_hi = 1.75 * R
    ex = (xx - cx) / ring_hi
    ey = (yy - cy) / (ring_hi * 0.78)
    re = np.hypot(ex, ey)
    ring_band = np.exp(-((re - 1.0) / 0.28) ** 2)

    # t de gradiente: 0 en el borde interno del anillo → 1 en el externo
    grad_t = np.clip((re - 0.86) / 0.34, 0.0, 1.0)
    # morado (185,105,255) → azul (92,150,255) → dorado (255,195,90)
    PURPLE = np.array([185.0, 105.0, 255.0])
    BLUE = np.array([92.0, 150.0, 255.0])
    GOLD = np.array([255.0, 195.0, 90.0])
    rgb = np.empty(grad_t.shape + (3,), dtype=float)
    m1 = grad_t < 0.45
    m2 = ~m1
    for c in range(3):
        rgb[..., c] = np.where(
            m1,
            PURPLE[c] + (BLUE[c] - PURPLE[c]) * (grad_t / 0.45),
            BLUE[c] + (GOLD[c] - BLUE[c]) * ((grad_t - 0.45) / 0.55))

    bands = 0.5 + 0.5 * np.sin(np.arctan2(ey, ex) * 6.0 + 1.2)
    bands = bands ** 1.4

    # las bandas realzan hacia el blanco frío en el pico
    ring_r = rgb[..., 0] + (238.0 - rgb[..., 0]) * 0.55 * bands
    ring_g = rgb[..., 1] + (242.0 - rgb[..., 1]) * 0.55 * bands
    ring_b = rgb[..., 2] + (255.0 - rgb[..., 2]) * 0.55 * bands
    a_ring = ring_band * 255

    m_ring = a_ring > img[..., 3]
    img[..., 0] = np.where(m_ring, ring_r, img[..., 0])
    img[..., 1] = np.where(m_ring, ring_g, img[..., 1])
    img[..., 2] = np.where(m_ring, ring_b, img[..., 2])
    img[..., 3] = np.maximum(img[..., 3], a_ring)

    # el horizonte negro
    disk = r <= R
    img[..., 0] = np.where(disk, 6, img[..., 0])
    img[..., 1] = np.where(disk, 4, img[..., 1])
    img[..., 2] = np.where(disk, 10, img[..., 2])
    img[..., 3] = np.where(disk, 255, img[..., 3])

    # runa DORADA (la corona estelar: rombo + travesaño) a la izquierda
    # — el dorado del BORDE del gradiente
    rx, ry = 11.5 * SS, 13.5 * SS
    rcx = xx - rx
    rcy = yy - ry
    rombo = (np.abs(rcx) / (2.4 * SS) + np.abs(rcy) / (2.4 * SS)) <= 1.0
    travesano = (np.abs(rcx) <= 2.4 * SS) & (np.abs(rcy) <= 0.75 * SS)
    punta = (np.abs(rcx) <= 0.75 * SS) & (np.abs(rcy) <= 2.6 * SS)
    rune = rombo | travesano | punta
    img[..., 0] = np.where(rune, 255, img[..., 0])
    img[..., 1] = np.where(rune, 185, img[..., 1])
    img[..., 2] = np.where(rune, 75, img[..., 2])
    img[..., 3] = np.where(rune, 255, img[..., 3])

    # DOBLE DESTELLO (aurora = frío): perla mayor blanco-fría +
    # estrella secundaria MORADA menor abajo-izquierda del agujero.
    star1 = four_point_star(xx, yy, 23.5 * SS, 5.5 * SS, 1.85 * SS, 0.42 * SS)
    m_star = star1 > 0.25
    img[..., 0] = np.where(m_star, 240, img[..., 0])
    img[..., 1] = np.where(m_star, 243, img[..., 1])
    img[..., 2] = np.where(m_star, 255, img[..., 2])
    img[..., 3] = np.where(m_star, 255, img[..., 3])

    star2 = four_point_star(xx, yy, 14.5 * SS, 5.5 * SS, 1.1 * SS, 0.30 * SS)
    m_star2 = star2 > 0.30
    img[..., 0] = np.where(m_star2, 185, img[..., 0])
    img[..., 1] = np.where(m_star2, 105, img[..., 1])
    img[..., 2] = np.where(m_star2, 255, img[..., 2])
    img[..., 3] = np.where(m_star2, 255, img[..., 3])

    save_icon(img, 'SupremoAuroraBlackHoleStaff.png')


if __name__ == '__main__':
    gen_shadow()
    gen_aurora_icon()
    print('\nv6.20 supremo aurora assets OK')
