#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_cosmic_umbral_tex_v616.py — v6.16

Genera los assets OBLIGATORIOS de los DOS agujeros negros nuevos:

1) Sombras por defecto de los ModProjectile (76×76 RGBA):
   tModLoader AUTO-REQUESTA la textura por defecto de todo ModProjectile
   (namespace + clase) durante TransferAllAssets aunque el PreDraw
   devuelva false y jamás se dibuje (LECCIÓN v6.14.1: sin ellas el mod
   entero se desactiva al cargar).

   Patrón: calco EXACTO del CrimsonBlackHoleProjectile.png (v6.09):
   disco negro sólido (r <= 14, alpha 255) · rim de identidad (pico
   r ≈ 18-22, alpha ~70) · desvanecido hacia r = 37, transparente en 38.

   Identidades:
   · CÓSMICO — rim MAGENTA del script Unity (255,51,204) desvaneciendo
               a violeta oscuro (90,0,120).
   · UMBRAL  — rim NARANJA incandescente del lado Doppler (255,120,50)
               desvaneciendo a rojo profundo (120,10,30).

2) Iconos de los bastones (28×30 RGBA, patrón del Olvido v6.15):
   bastón diagonal + mini-agujero con la identidad de cada uno.

Salida:
    Content/Projectiles/Cosmic/CosmicBlackHoleProjectile.png
    Content/Projectiles/Cosmic/UmbralBlackHoleProjectile.png
    Content/Weapons/Cosmic/CosmicBlackHoleStaff.png
    Content/Weapons/Cosmic/UmbralBlackHoleStaff.png
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
    yy, xx = np.mgrid[0:SIZE, 0:SIZE].astype(float)
    r = np.hypot(xx - CENTER, yy - CENTER)

    img = np.zeros((SIZE, SIZE, 4), dtype=float)

    # --- disco negro sólido ---
    disk = r <= core_r
    img[..., 0][disk] = 6
    img[..., 1][disk] = 2
    img[..., 2][disk] = 8
    img[..., 3][disk] = 255

    # --- rim de identidad: pico en peak_r, cae a 0 en fade_r ---
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


def gen_shadows():
    # CÓSMICO: magenta Unity (255,51,204) → violeta oscuro
    cosmic = make_shadow((255, 51, 204), (90, 0, 120))
    Image.fromarray(cosmic).save(os.path.join(
        OUT_DIR_P, 'CosmicBlackHoleProjectile.png'))

    # UMBRAL: naranja Doppler (255,120,50) → rojo profundo
    umbral = make_shadow((255, 120, 50), (120, 10, 30))
    Image.fromarray(umbral).save(os.path.join(
        OUT_DIR_P, 'UmbralBlackHoleProjectile.png'))

    print('OK sombras 76x76:',
          'CosmicBlackHoleProjectile.png', 'UmbralBlackHoleProjectile.png')


# =====================================================================
#  2) ICONOS DE BASTÓN (28×30, patrón Olvido v6.15)
# =====================================================================

W, H = 28, 30
SS = 8
OW, OH = W * SS, H * SS


def staff_base(img, xx, yy, wood_r, wood_g, wood_b):
    """El bastón diagonal (madera tintada), calcando el patrón v6.15."""
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
    """CÓSMICO: bastón violeta-magenta + mini-agujero con anillo de
    BANDAS magenta (el shader) + runa dorada de circuito + destello."""
    yy, xx = np.mgrid[0:OH, 0:OW].astype(float)
    img = np.zeros((OH, OW, 4), dtype=float)

    # --- el bastón (madera oscura violeta-magenta) ---
    staff_base(img, xx, yy, 58, 24, 62)

    # --- EL MINI-AGUJERO (sobre la punta) ---
    cx, cy = 19.5 * SS, 9.5 * SS
    r = np.hypot(xx - cx, yy - cy)
    th = np.arctan2(yy - cy, xx - cx)

    R = 4.6 * SS

    # halo exterior magenta tenue
    halo = np.exp(-((r - 1.45 * R) / (0.85 * R)) ** 2)
    a_halo = halo * 90
    better_halo = a_halo > img[..., 3]
    img[..., 0] = np.where(better_halo, 200, img[..., 0])
    img[..., 1] = np.where(better_halo, 40, img[..., 1])
    img[..., 2] = np.where(better_halo, 150, img[..., 2])
    img[..., 3] = np.maximum(img[..., 3], a_halo)

    # anillo con LAS BANDAS del shader: brillo = sin(θ·20 + fase)
    ring_hi = 1.75 * R
    ex = (xx - cx) / ring_hi
    ey = (yy - cy) / (ring_hi * 0.78)
    re = np.hypot(ex, ey)
    ring_band = np.exp(-((re - 1.0) / 0.28) ** 2)

    # LAS BANDAS: 6 sectores brillantes (a escala icono, 20 es demasiado)
    bands = 0.5 + 0.5 * np.sin(np.arctan2(ey, ex) * 6.0 + 1.2)
    bands = bands ** 1.4

    ring_r = 150 + 105 * bands        # magenta → blanco-rosa
    ring_g = 20 + 215 * bands
    ring_b = 110 + 140 * bands
    a_ring = ring_band * 255

    m_ring = a_ring > img[..., 3]
    img[..., 0] = np.where(m_ring, ring_r, img[..., 0])
    img[..., 1] = np.where(m_ring, ring_g, img[..., 1])
    img[..., 2] = np.where(m_ring, ring_b, img[..., 2])
    img[..., 3] = np.maximum(img[..., 3], a_ring)

    # el horizonte negro
    disk = r <= R
    img[..., 0] = np.where(disk, 8, img[..., 0])
    img[..., 1] = np.where(disk, 2, img[..., 1])
    img[..., 2] = np.where(disk, 12, img[..., 2])
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

    # destello de 4 puntas (la perla del circuito)
    sx, sy = 23.5 * SS, 5.5 * SS
    dxs = np.abs(xx - sx)
    dys = np.abs(yy - sy)
    star_v = np.exp(-(dxs / (1.6 * SS)) ** 2) * np.exp(-(dys / (0.42 * SS)) ** 2)
    star_h = np.exp(-(dys / (1.6 * SS)) ** 2) * np.exp(-(dxs / (0.42 * SS)) ** 2)
    star = np.maximum(star_v, star_h)
    m_star = star > 0.25
    img[..., 0] = np.where(m_star, 255, img[..., 0])
    img[..., 1] = np.where(m_star, 235, img[..., 1])
    img[..., 2] = np.where(m_star, 250, img[..., 2])
    img[..., 3] = np.where(m_star, 255, img[..., 3])

    save_icon(img, 'CosmicBlackHoleStaff.png')


def gen_umbral_icon():
    """UMBRAL: bastón rojo-carmesí + mini-agujero con disco DOPPLER
    (derecha blanca-naranja, izquierda rojo profundo) + arco rúnico
    dorado ROTO + brasa."""
    yy, xx = np.mgrid[0:OH, 0:OW].astype(float)
    img = np.zeros((OH, OW, 4), dtype=float)

    # --- el bastón (madera oscura rojo-carmesí) ---
    staff_base(img, xx, yy, 66, 26, 30)

    # --- EL MINI-AGUJERO ---
    cx, cy = 19.5 * SS, 9.5 * SS
    r = np.hypot(xx - cx, yy - cy)

    R = 4.6 * SS

    # halo exterior naranja-rojo tenue
    halo = np.exp(-((r - 1.45 * R) / (0.85 * R)) ** 2)
    a_halo = halo * 90
    better_halo = a_halo > img[..., 3]
    img[..., 0] = np.where(better_halo, 220, img[..., 0])
    img[..., 1] = np.where(better_halo, 70, img[..., 1])
    img[..., 2] = np.where(better_halo, 30, img[..., 2])
    img[..., 3] = np.maximum(img[..., 3], a_halo)

    # DISCO OBLICUO CON DOPPLER: elipse achatada; el lado DERECHO
    # (θ≈0) BLANCO-naranja incandescente, el izquierdo rojo profundo.
    ring_hi = 1.85 * R
    ex = (xx - cx) / ring_hi
    ey = (yy - cy) / (ring_hi * 0.60)
    re = np.hypot(ex, ey)
    ring_band = np.exp(-((re - 1.0) / 0.30) ** 2)

    ang = np.arctan2(ey, ex)
    dop = 0.5 + 0.5 * np.cos(ang)
    dop = dop ** 2

    ring_r = 150 + 105 * dop         # rojo profundo → blanco-amarillo
    ring_g = 18 + 225 * dop
    ring_b = 40 + 180 * dop
    a_ring = ring_band * 255

    m_ring = a_ring > img[..., 3]
    img[..., 0] = np.where(m_ring, ring_r, img[..., 0])
    img[..., 1] = np.where(m_ring, ring_g, img[..., 1])
    img[..., 2] = np.where(m_ring, ring_b, img[..., 2])
    img[..., 3] = np.maximum(img[..., 3], a_ring)

    # LA PÚA: destello alargado saliendo del lado derecho
    sxp = cx + ring_hi * 0.95
    dxs = xx - sxp
    dys = yy - cy
    spike = np.exp(-(dys / (0.35 * R)) ** 2) * np.exp(-(np.clip(-dxs, 0, None) / (1.1 * R)) ** 2)
    m_spike = spike > 0.30
    img[..., 0] = np.where(m_spike, 255, img[..., 0])
    img[..., 1] = np.where(m_spike, 215, img[..., 1])
    img[..., 2] = np.where(m_spike, 235, img[..., 2])
    img[..., 3] = np.where(m_spike, 255, img[..., 3])

    # el horizonte negro
    disk = r <= R
    img[..., 0] = np.where(disk, 8, img[..., 0])
    img[..., 1] = np.where(disk, 2, img[..., 1])
    img[..., 2] = np.where(disk, 5, img[..., 2])
    img[..., 3] = np.where(disk, 255, img[..., 3])

    # ARCO RÚNICO DORADO ROTO (tres tramos, con hueco)
    rr2 = 3.1 * R
    arc = np.exp(-((r - rr2) / (0.5 * SS)) ** 2)
    arc_ang = np.arctan2(yy - cy, xx - cx)
    # tramos: [-2.3,-1.2], [-0.6,0.2], [1.4,2.4] rad (huecos entre ellos)
    in_arc = (((arc_ang > -2.3) & (arc_ang < -1.2)) |
              ((arc_ang > -0.6) & (arc_ang < 0.2)) |
              ((arc_ang > 1.4) & (arc_ang < 2.4)))
    a_arc = arc * in_arc * 255
    m_arc = a_arc > img[..., 3]
    img[..., 0] = np.where(m_arc, 255, img[..., 0])
    img[..., 1] = np.where(m_arc, 150, img[..., 1])
    img[..., 2] = np.where(m_arc, 25, img[..., 2])
    img[..., 3] = np.maximum(img[..., 3], a_arc)

    # brasa naranja-amarilla (la chispa de la referencia)
    sx, sy = 23.5 * SS, 5.5 * SS
    dxs = np.abs(xx - sx)
    dys = np.abs(yy - sy)
    ember = np.exp(-((dxs / (1.0 * SS)) ** 2 + (dys / (1.0 * SS)) ** 2))
    m_ember = ember > 0.30
    img[..., 0] = np.where(m_ember, 255, img[..., 0])
    img[..., 1] = np.where(m_ember, 195, img[..., 1])
    img[..., 2] = np.where(m_ember, 60, img[..., 2])
    img[..., 3] = np.where(m_ember, 255, img[..., 3])

    save_icon(img, 'UmbralBlackHoleStaff.png')


if __name__ == '__main__':
    gen_shadows()
    gen_cosmic_icon()
    gen_umbral_icon()
