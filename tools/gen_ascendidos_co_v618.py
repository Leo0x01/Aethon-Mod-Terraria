#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_ascendidos_co_v618.py — v6.18 (Task D3)

Genera los assets de los AGUJEROS ASCENDIDOS (copias mejoradas del
CÓSMICO y del OLVIDO, con la librería de rayos LightningCore):

1) Sombras de proyectil (76×76 RGBA, patrón v6.09 exacto):
   · CÓSMICO ASCENDIDO   — rim ROJO-NARANJA BRILLANTE (255,110,45)
                           → rojo oscuro (130,15,5), un punto más de
                           pico que el Cósmico base (la señal ascendida)
   · OLVIDO ASCENDIDO    — rim AZUL-VIOLETA BRILLANTE (110,160,255)
                           → índigo (15,25,110)

2) Iconos de bastón (28×30 RGBA, patrón v6.15 + los RASGOS nuevos):
   · CÓSMICO ASCENDIDO — bastón caoba + anillo rojo-naranja de bandas
                         (+ eco interior = doble anillo) + 2 MINI-RAYOS
                         naranjas escapando + JET POLAR vertical
   · OLVIDO ASCENDIDO  — bastón índigo + anillo morado-azul +
                         MINI-ARCO eléctrico azul alrededor del
                         horizonte + BRAZO ESPIRAL desenroscándose

Salida:
    AethonMod/Content/Projectiles/Cosmic/CosmicAscendidoBlackHoleProjectile.png
    AethonMod/Content/Projectiles/Cosmic/OlvidoAscendidoBlackHoleProjectile.png
    AethonMod/Content/Weapons/Cosmic/CosmicAscendidoBlackHoleStaff.png
    AethonMod/Content/Weapons/Cosmic/OlvidoAscendidoBlackHoleStaff.png
"""

import math
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
    # === CÓSMICO ASCENDIDO: rim ROJO-NARANJA BRILLANTE ===
    cosmic = make_shadow(
        rim_inner=(255, 110, 45),    # rojo-naranja brillante (ascendido)
        rim_outer=(130, 15, 5),      # rojo oscuro desvanecido
        core_r=14.0, peak_r=19.5, fade_r=37.0, peak_alpha=78.0)
    cosmic.save(os.path.join(OUT_DIR_P, 'CosmicAscendidoBlackHoleProjectile.png'))
    print('OK sombra CosmicAscendidoBlackHoleProjectile.png')

    # === OLVIDO ASCENDIDO: rim AZUL-VIOLETA BRILLANTE ===
    olvido = make_shadow(
        rim_inner=(110, 160, 255),   # azul-violeta brillante (ascendido)
        rim_outer=(15, 25, 110),     # índigo desvanecido
        core_r=14.0, peak_r=19.5, fade_r=37.0, peak_alpha=78.0)
    olvido.save(os.path.join(OUT_DIR_P, 'OlvidoAscendidoBlackHoleProjectile.png'))
    print('OK sombra OlvidoAscendidoBlackHoleProjectile.png')


# =====================================================================
#  2) ICONOS DE BASTÓN (28×30, patrón v6.15 + rasgos ascendidos)
# =====================================================================

W, H = 28, 30
SS = 8
OW, OH = W * SS, H * SS

# malla global (todos los estampados la comparten)
GY, GX = np.mgrid[0:OH, 0:OW].astype(float)


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


# ---------------------------------------------------------------------
#  ESTAMPADORES NUEVOS (los rasgos LightningCore del icono)
# ---------------------------------------------------------------------

def _seg_dist(x0, y0, x1, y1):
    """Distancia de cada píxel al segmento (con clamp a los extremos)."""
    dx, dy = x1 - x0, y1 - y0
    L2 = dx * dx + dy * dy
    if L2 < 1e-9:
        return np.hypot(GX - x0, GY - y0)
    t = np.clip(((GX - x0) * dx + (GY - y0) * dy) / L2, 0.0, 1.0)
    cx = x0 + t * dx
    cy = y0 + t * dy
    return np.hypot(GX - cx, GY - cy)


def stamp_polyline(img, pts, width, rgb, alpha):
    """Estampa una polilínea DURA (el cuerpo/núcleo de un mini-rayo)."""
    d = np.full(GY.shape, np.inf)
    for (x0, y0), (x1, y1) in zip(pts[:-1], pts[1:]):
        d = np.minimum(d, _seg_dist(x0, y0, x1, y1))
    m = d <= width
    img[..., 0] = np.where(m, rgb[0], img[..., 0])
    img[..., 1] = np.where(m, rgb[1], img[..., 1])
    img[..., 2] = np.where(m, rgb[2], img[..., 2])
    img[..., 3] = np.where(m, np.maximum(alpha, img[..., 3]), img[..., 3])


def stamp_glow_capsule(img, x0, y0, x1, y1, width, rgb, peak):
    """Estampa una cápsula de GLOW (jet polar: gaussiana con taper)."""
    dx, dy = x1 - x0, y1 - y0
    L2 = dx * dx + dy * dy
    if L2 < 1e-9:
        t = np.zeros(GY.shape)
    else:
        t = np.clip(((GX - x0) * dx + (GY - y0) * dy) / L2, 0.0, 1.0)
    cx = x0 + t * dx
    cy = y0 + t * dy
    d = np.hypot(GX - cx, GY - cy)
    # Taper longitudinal suave (base viva → punta que se apaga).
    taper = 0.30 + 0.70 * np.sin(t * np.pi)
    a = peak * np.exp(-(d / width) ** 2) * taper
    better = a > img[..., 3]
    img[..., 0] = np.where(better, rgb[0], img[..., 0])
    img[..., 1] = np.where(better, rgb[1], img[..., 1])
    img[..., 2] = np.where(better, rgb[2], img[..., 2])
    img[..., 3] = np.maximum(img[..., 3], a)


def zigzag(rng, x0, y0, x1, y1, segments, amp):
    """Mini-rayo en zigzag (jitter perpendicular, tensión central)."""
    dx, dy = x1 - x0, y1 - y0
    L = math.hypot(dx, dy)
    if L < 1e-6:
        return [(x0, y0), (x1, y1)]
    ux, uy = dx / L, dy / L
    nx, ny = -uy, ux
    pts = []
    for i in range(segments + 1):
        f = i / segments
        env = math.sin(f * math.pi)
        j = rng.uniform(-1.0, 1.0) * amp * env
        pts.append((x0 + dx * f + nx * j, y0 + dy * f + ny * j))
    pts[0] = (x0, y0)
    pts[-1] = (x1, y1)
    return pts


def arc_pts(rng, cx, cy, radius, a1, a2, n, amp):
    """Mini-arco eléctrico (puntos sobre el círculo + jitter radial)."""
    pts = []
    for i in range(n + 1):
        f = i / n
        a = a1 + (a2 - a1) * f
        r = radius * (1.0 + rng.uniform(-1.0, 1.0) * amp * math.sin(f * math.pi))
        pts.append((cx + math.cos(a) * r, cy + math.sin(a) * r))
    return pts


def spiral_pts(cx, cy, rx, ry, t0, sweep, grow, n):
    """Puntos sobre un brazo espiral (anillo → afuera)."""
    pts = []
    for i in range(n + 1):
        f = i / n
        t = t0 + f * sweep
        g = 1.0 + f * grow
        pts.append((cx + math.cos(t) * rx * g, cy + math.sin(t) * ry * g))
    return pts


# ---------------------------------------------------------------------
#  CÓSMICO ASCENDIDO
# ---------------------------------------------------------------------

def gen_cosmic_ascendido_icon():
    """CÓSMICO ASCENDIDO: bastón caoba + mini-agujero con anillo de BANDAS
    ROJO-NARANJA (+ ECO INTERIOR = doble anillo) + 2 MINI-RAYOS naranjas
    escapando + JET POLAR vertical + runa dorada + destello cálido."""
    rng = np.random.default_rng(618)
    yy, xx = np.mgrid[0:OH, 0:OW].astype(float)
    img = np.zeros((OH, OW, 4), dtype=float)

    # --- el bastón (madera caoba rojiza, algo más rica que la base) ---
    staff_base(img, xx, yy, 84, 36, 22)

    # --- EL MINI-AGUJERO (sobre la punta) ---
    cx, cy = 19.5 * SS, 9.5 * SS
    r = np.hypot(xx - cx, yy - cy)

    R = 4.6 * SS

    # halo exterior rojo-naranja tenue
    halo = np.exp(-((r - 1.45 * R) / (0.85 * R)) ** 2)
    a_halo = halo * 95
    better_halo = a_halo > img[..., 3]
    img[..., 0] = np.where(better_halo, 240, img[..., 0])
    img[..., 1] = np.where(better_halo, 92, img[..., 1])
    img[..., 2] = np.where(better_halo, 36, img[..., 2])
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

    # EL ECO INTERIOR (0.62× del anillo — el DOBLE ANILLO de bandas):
    eco_hi = ring_hi * 0.62
    eex = (xx - cx) / eco_hi
    eey = (yy - cy) / (eco_hi * 0.78)
    ere = np.hypot(eex, eey)
    eco_band = np.exp(-((ere - 1.0) / 0.22) ** 2)
    eco_bands = (0.5 + 0.5 * np.sin(np.arctan2(eey, eex) * 6.0 - 1.6)) ** 1.4
    eco_r = 210 + 45 * eco_bands
    eco_g = 90 + 150 * eco_bands
    eco_b = 30 + 180 * eco_bands
    a_eco = eco_band * 225
    m_eco = a_eco > img[..., 3]
    img[..., 0] = np.where(m_eco, eco_r, img[..., 0])
    img[..., 1] = np.where(m_eco, eco_g, img[..., 1])
    img[..., 2] = np.where(m_eco, eco_b, img[..., 2])
    img[..., 3] = np.maximum(img[..., 3], a_eco)

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

    # ============================================================
    #  LOS RASGOS ASCENDIDOS
    # ============================================================

    # 1) DOS MINI-RAYOS NARANJAS escapando del anillo (LightningCore
    #    hecho icono: funda ancha + núcleo cálido + zigzag determinista).
    for ang, ln in ((-0.85, 2.3), (3.75, 2.1)):
        sx0 = cx + math.cos(ang) * ring_hi
        sy0 = cy + math.sin(ang) * ring_hi * 0.78
        sx1 = cx + math.cos(ang) * (ring_hi + ln * SS)
        sy1 = cy + math.sin(ang) * (ring_hi * 0.78 + ln * SS * 0.9)
        pts = zigzag(rng, sx0, sy0, sx1, sy1, 4, 0.5 * SS)
        stamp_polyline(img, pts, 0.55 * SS, (255, 140, 50), 235)   # funda
        stamp_polyline(img, pts, 0.22 * SS, (255, 244, 214), 255)  # núcleo

    # 2) EL JET POLAR: chorro vertical ARRIBA y ABAJO del horizonte
    #    (cápsula de glow pulsante con núcleo brillante).
    for sgn in (-1.0, 1.0):
        jx0, jy0 = cx, cy + sgn * R * 0.55
        jx1, jy1 = cx, cy + sgn * R * 1.95
        stamp_glow_capsule(img, jx0, jy0, jx1, jy1, 0.62 * SS, (255, 120, 40), 200)
        stamp_glow_capsule(img, jx0, jy0, jx1, jy1, 0.28 * SS, (255, 238, 210), 255)

    save_icon(img, 'CosmicAscendidoBlackHoleStaff.png')


# ---------------------------------------------------------------------
#  OLVIDO ASCENDIDO
# ---------------------------------------------------------------------

def gen_olvido_ascendido_icon():
    """OLVIDO ASCENDIDO: bastón índigo + mini-agujero con anillo
    MORADO-AZUL + MINI-ARCO eléctrico azul alrededor del horizonte +
    BRAZO ESPIRAL desenroscándose + runa dorada + destello frío."""
    rng = np.random.default_rng(1818)
    yy, xx = np.mgrid[0:OH, 0:OW].astype(float)
    img = np.zeros((OH, OW, 4), dtype=float)

    # --- el bastón (madera índigo oscura) ---
    staff_base(img, xx, yy, 40, 38, 76)

    # --- EL MINI-AGUJERO ---
    cx, cy = 19.5 * SS, 9.5 * SS
    r = np.hypot(xx - cx, yy - cy)

    R = 4.6 * SS

    # halo exterior morado-azul tenue
    halo = np.exp(-((r - 1.45 * R) / (0.85 * R)) ** 2)
    a_halo = halo * 95
    better_halo = a_halo > img[..., 3]
    img[..., 0] = np.where(better_halo, 82, img[..., 0])
    img[..., 1] = np.where(better_halo, 96, img[..., 1])
    img[..., 2] = np.where(better_halo, 220, img[..., 2])
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

    # ============================================================
    #  LOS RASGOS ASCENDIDOS
    # ============================================================

    # 1) EL BRAZO ESPIRAL: se desenrosca del anillo hacia afuera
    #    (arco creciente morado → azul, estampado como glow taper).
    arm = spiral_pts(cx, cy, ring_hi, ring_hi * 0.72, 2.35, 1.05, 0.34, 10)
    for (x0, y0), (x1, y1) in zip(arm[:-1], arm[1:]):
        stamp_glow_capsule(img, x0, y0, x1, y1, 0.55 * SS, (90, 120, 255), 150)

    # 2) LOS MINI-ARCOS ELÉCTRICOS AZULES alrededor del horizonte
    #    (el arco del vacío hecho icono: dos coronas jitteradas).
    for a0, rad in ((0.7, 1.02), (3.6, 1.22)):
        pts = arc_pts(rng, cx, cy, R * rad, a0, a0 + 1.15, 7, 0.09)
        stamp_polyline(img, pts, 0.42 * SS, (90, 170, 255), 225)   # funda
        stamp_polyline(img, pts, 0.17 * SS, (235, 244, 255), 255)  # núcleo

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

    save_icon(img, 'OlvidoAscendidoBlackHoleStaff.png')


if __name__ == '__main__':
    gen_shadows()
    gen_cosmic_ascendido_icon()
    gen_olvido_ascendido_icon()
    print('\nv6.18 ascendidos (D3) assets OK')
