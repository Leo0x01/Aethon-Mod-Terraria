#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_recolor_tex_v618.py — v6.18

Regenera los assets de CÓSMICO y OLVIDO tras el RECOLOR v6.18
(petición del usuario: "has que Cosmic sea de color rojo naranja,
y Olvido sea morado azul"):

1) Sombras de proyectil (76×76 RGBA, patrón v6.09 exacto):
   · CÓSMICO — rim ROJO-NARANJA (255,95,35) → rojo profundo (110,15,5)
               (antes: magenta del script Unity)
   · OLVIDO  — rim MORADO-AZUL (100,145,255) → azul profundo (10,20,95)
               (antes: violeta-rosa)

2) Iconos de bastón (28×30 RGBA, patrón v6.15):
   · CÓSMICO — bandas ROJO-NARANJA + runa dorada + bastón caoba
   · OLVIDO  — anillo MORADO-AZUL + runa dorada + bastón índigo

Salida:
    AethonMod/Content/Projectiles/Cosmic/CosmicBlackHoleProjectile.png
    AethonMod/Content/Projectiles/Cosmic/OlvidoBlackHoleProjectile.png
    AethonMod/Content/Weapons/Cosmic/CosmicBlackHoleStaff.png
    AethonMod/Content/Weapons/Cosmic/OlvidoBlackHoleStaff.png
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
#  1) SOMBRAS DE PROYECTIL (patrón carmesí v6.09 exacto)
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


def gen_shadows():
    # === CÓSMICO: rim ROJO-NARANJA (la nueva identidad) ===
    cosmic = make_shadow(
        rim_inner=(255, 95, 35),     # rojo-naranja vivo
        rim_outer=(110, 15, 5),      # rojo profundo desvanecido
        core_r=14.0, peak_r=19.5, fade_r=37.0, peak_alpha=72.0)
    cosmic.save(os.path.join(OUT_DIR_P, 'CosmicBlackHoleProjectile.png'))
    print('OK sombra CosmicBlackHoleProjectile.png')

    # === OLVIDO: rim MORADO-AZUL (la nueva identidad) ===
    olvido = make_shadow(
        rim_inner=(100, 145, 255),   # azul-violeta vivo
        rim_outer=(10, 20, 95),      # azul profundo desvanecido
        core_r=14.0, peak_r=19.5, fade_r=37.0, peak_alpha=72.0)
    olvido.save(os.path.join(OUT_DIR_P, 'OlvidoBlackHoleProjectile.png'))
    print('OK sombra OlvidoBlackHoleProjectile.png')


# =====================================================================
#  2) ICONOS DE BASTÓN (28×30, patrón v6.15)
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


def gen_cosmic_icon():
    """CÓSMICO (v6.18): bastón caoba + mini-agujero con anillo de BANDAS
    ROJO-NARANJA + runa dorada de circuito + destello cálido."""
    yy, xx = np.mgrid[0:OH, 0:OW].astype(float)
    img = np.zeros((OH, OW, 4), dtype=float)

    # --- el bastón (madera caoba rojiza) ---
    staff_base(img, xx, yy, 74, 34, 22)

    # --- EL MINI-AGUJERO (sobre la punta) ---
    cx, cy = 19.5 * SS, 9.5 * SS
    r = np.hypot(xx - cx, yy - cy)

    R = 4.6 * SS

    # halo exterior rojo-naranja tenue
    halo = np.exp(-((r - 1.45 * R) / (0.85 * R)) ** 2)
    a_halo = halo * 90
    better_halo = a_halo > img[..., 3]
    img[..., 0] = np.where(better_halo, 235, img[..., 0])
    img[..., 1] = np.where(better_halo, 90, img[..., 1])
    img[..., 2] = np.where(better_halo, 35, img[..., 2])
    img[..., 3] = np.maximum(img[..., 3], a_halo)

    # anillo con LAS BANDAS del shader (rojo-naranja → blanco cálido)
    ring_hi = 1.75 * R
    ex = (xx - cx) / ring_hi
    ey = (yy - cy) / (ring_hi * 0.78)
    re = np.hypot(ex, ey)
    ring_band = np.exp(-((re - 1.0) / 0.28) ** 2)

    bands = 0.5 + 0.5 * np.sin(np.arctan2(ey, ex) * 6.0 + 1.2)
    bands = bands ** 1.4

    ring_r = 190 + 65 * bands         # rojo → blanco cálido
    ring_g = 45 + 190 * bands
    ring_b = 15 + 205 * bands
    a_ring = ring_band * 255

    m_ring = a_ring > img[..., 3]
    img[..., 0] = np.where(m_ring, ring_r, img[..., 0])
    img[..., 1] = np.where(m_ring, ring_g, img[..., 1])
    img[..., 2] = np.where(m_ring, ring_b, img[..., 2])
    img[..., 3] = np.maximum(img[..., 3], a_ring)

    # el horizonte negro
    disk = r <= R
    img[..., 0] = np.where(disk, 10, img[..., 0])
    img[..., 1] = np.where(disk, 3, img[..., 1])
    img[..., 2] = np.where(disk, 2, img[..., 2])
    img[..., 3] = np.where(disk, 255, img[..., 3])

    # runa dorada de CIRCUITO (rombo con línea vertical) a la izquierda
    rx, ry = 11.5 * SS, 13.5 * SS
    rcx = xx - rx
    rcy = yy - ry
    rombo = (np.abs(rcx) / (2.4 * SS) + np.abs(rcy) / (2.4 * SS)) <= 1.0
    vline = (np.abs(rcx) <= 0.8 * SS) & (np.abs(rcy) <= 2.4 * SS)
    rune = rombo | vline
    img[..., 0] = np.where(rune, 255, img[..., 0])
    img[..., 1] = np.where(rune, 185, img[..., 1])
    img[..., 2] = np.where(rune, 70, img[..., 2])
    img[..., 3] = np.where(rune, 255, img[..., 3])

    # destello de 4 puntas (la perla del circuito — cálido)
    sx, sy = 23.5 * SS, 5.5 * SS
    dxs = np.abs(xx - sx)
    dys = np.abs(yy - sy)
    star_v = np.exp(-(dxs / (1.6 * SS)) ** 2) * np.exp(-(dys / (0.42 * SS)) ** 2)
    star_h = np.exp(-(dys / (1.6 * SS)) ** 2) * np.exp(-(dxs / (0.42 * SS)) ** 2)
    star = np.maximum(star_v, star_h)
    m_star = star > 0.25
    img[..., 0] = np.where(m_star, 255, img[..., 0])
    img[..., 1] = np.where(m_star, 235, img[..., 1])
    img[..., 2] = np.where(m_star, 200, img[..., 2])
    img[..., 3] = np.where(m_star, 255, img[..., 3])

    save_icon(img, 'CosmicBlackHoleStaff.png')


def gen_olvido_icon():
    """OLVIDO (v6.18): bastón índigo + mini-agujero con anillo MORADO-AZUL
    (gradiente violeta→azul eléctrico) + runa dorada + destello frío."""
    yy, xx = np.mgrid[0:OH, 0:OW].astype(float)
    img = np.zeros((OH, OW, 4), dtype=float)

    # --- el bastón (madera índigo oscura) ---
    staff_base(img, xx, yy, 38, 36, 72)

    # --- EL MINI-AGUJERO ---
    cx, cy = 19.5 * SS, 9.5 * SS
    r = np.hypot(xx - cx, yy - cy)

    R = 4.6 * SS

    # halo exterior morado-azul tenue
    halo = np.exp(-((r - 1.45 * R) / (0.85 * R)) ** 2)
    a_halo = halo * 90
    better_halo = a_halo > img[..., 3]
    img[..., 0] = np.where(better_halo, 80, img[..., 0])
    img[..., 1] = np.where(better_halo, 95, img[..., 1])
    img[..., 2] = np.where(better_halo, 215, img[..., 2])
    img[..., 3] = np.maximum(img[..., 3], a_halo)

    # anillo de plasma (gradiente morado → azul eléctrico)
    ring_hi = 1.75 * R
    ex = (xx - cx) / ring_hi
    ey = (yy - cy) / (ring_hi * 0.72)
    re = np.hypot(ex, ey)
    ring_band = np.exp(-((re - 1.0) / 0.28) ** 2)

    th = np.arctan2(ey, ex)
    hot = 0.5 + 0.5 * np.cos(th - 0.6)
    hot2 = hot * hot

    ring_r = 95 + 100 * hot2           # morado → blanco-frío
    ring_g = 105 + 140 * hot2           # → azul caliente
    ring_b = 235 + 20 * hot2            # azul eléctrico → blanco
    a_ring = ring_band * 255

    m_ring = a_ring > img[..., 3]
    img[..., 0] = np.where(m_ring, ring_r, img[..., 0])
    img[..., 1] = np.where(m_ring, ring_g, img[..., 1])
    img[..., 2] = np.where(m_ring, ring_b, img[..., 2])
    img[..., 3] = np.maximum(img[..., 3], a_ring)

    # el horizonte negro (disco sólido)
    disk = r <= R
    img[..., 0] = np.where(disk, 4, img[..., 0])
    img[..., 1] = np.where(disk, 3, img[..., 1])
    img[..., 2] = np.where(disk, 10, img[..., 2])
    img[..., 3] = np.where(disk, 255, img[..., 3])

    # runa dorada (cruz angular a la izquierda del agujero)
    rx, ry = 11.5 * SS, 13.5 * SS
    rcx = xx - rx
    rcy = yy - ry
    rune = ((np.abs(rcx) <= 0.85 * SS) & (np.abs(rcy) <= 2.4 * SS)) | \
           ((np.abs(rcy) <= 0.85 * SS) & (np.abs(rcx) <= 2.4 * SS))
    img[..., 0] = np.where(rune, 255, img[..., 0])
    img[..., 1] = np.where(rune, 205, img[..., 1])
    img[..., 2] = np.where(rune, 95, img[..., 2])
    img[..., 3] = np.where(rune, 255, img[..., 3])

    # destello de 4 puntas (la perla arcana — fría)
    sx, sy = 23.5 * SS, 5.5 * SS
    dxs = np.abs(xx - sx)
    dys = np.abs(yy - sy)
    star_v = np.exp(-(dxs / (1.6 * SS)) ** 2) * np.exp(-(dys / (0.42 * SS)) ** 2)
    star_h = np.exp(-(dys / (1.6 * SS)) ** 2) * np.exp(-(dxs / (0.42 * SS)) ** 2)
    star = np.maximum(star_v, star_h)
    m_star = star > 0.25
    img[..., 0] = np.where(m_star, 210, img[..., 0])
    img[..., 1] = np.where(m_star, 230, img[..., 1])
    img[..., 2] = np.where(m_star, 255, img[..., 2])
    img[..., 3] = np.where(m_star, 255, img[..., 3])

    save_icon(img, 'OlvidoBlackHoleStaff.png')


if __name__ == '__main__':
    gen_shadows()
    gen_cosmic_icon()
    gen_olvido_icon()
    print('\nv6.18 recolor assets OK')
