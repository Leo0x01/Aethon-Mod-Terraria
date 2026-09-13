#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_ascendidos_ub_v618.py — v6.18 (Task D2)

Genera los assets de los AGUJEROS ASCENDIDOS de UMBRAL y BRUMA
(las copias mejoradas — los originales quedan intactos):

1) Sombras de proyectil (76×76 RGBA, patrón v6.09 exacto):
   · UMBRAL ASCENDIDO  — rim NARANJA intenso (255,140,60) → carmesí (150,20,20)
   · BRUMA ASCENDIDA   — rim CIANO brillante (90,230,255) → teal profundo (10,80,95)

2) Iconos de bastón (28×30 RGBA, patrón v6.15 con supersampling ×8):
   · UMBRAL ASCENDIDO  — bastón rojizo + anillo DOPPLER (lado derecho
                          blanco cálido y GRUESO, izquierdo carmesí fino)
                          + 2 mini-rayos naranjas (zigzags finos ramificados)
   · BRUMA ASCENDIDA   — bastón teal + anillo de HUMO (banda suave con
                          grumos) + mini-ARCO ELÉCTRICO cian alrededor del
                          horizonte + destello-cristal de hielo

Salida:
    AethonMod/Content/Projectiles/Cosmic/UmbralAscendidoBlackHoleProjectile.png
    AethonMod/Content/Projectiles/Cosmic/BrumaAscendidoBlackHoleProjectile.png
    AethonMod/Content/Weapons/Cosmic/UmbralAscendidoBlackHoleStaff.png
    AethonMod/Content/Weapons/Cosmic/BrumaAscendidoBlackHoleStaff.png
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
    # === UMBRAL ASCENDIDO: rim NARANJA intenso → carmesí ===
    umbral = make_shadow(
        rim_inner=(255, 140, 60),    # naranja intenso vivo
        rim_outer=(150, 20, 20),     # carmesí profundo desvanecido
        core_r=14.0, peak_r=19.5, fade_r=37.0, peak_alpha=78.0)
    umbral.save(os.path.join(OUT_DIR_P, 'UmbralAscendidoBlackHoleProjectile.png'))
    print('OK sombra UmbralAscendidoBlackHoleProjectile.png')

    # === BRUMA ASCENDIDA: rim CIANO brillante → teal profundo ===
    bruma = make_shadow(
        rim_inner=(90, 230, 255),    # ciano brillante vivo
        rim_outer=(10, 80, 95),      # teal profundo desvanecido
        core_r=14.0, peak_r=19.5, fade_r=37.0, peak_alpha=78.0)
    bruma.save(os.path.join(OUT_DIR_P, 'BrumaAscendidoBlackHoleProjectile.png'))
    print('OK sombra BrumaAscendidoBlackHoleProjectile.png')


# =====================================================================
#  2) ICONOS DE BASTÓN (28×30, patrón v6.15 con SS×8)
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


def polyline_alpha(xx, yy, pts, width):
    """Alfa suave [0..1] de una polilínea (distancia a los segmentos)."""
    best = np.full(xx.shape, 1e9)
    for (x0, y0), (x1, y1) in zip(pts[:-1], pts[1:]):
        dx, dy = x1 - x0, y1 - y0
        L2 = dx * dx + dy * dy
        if L2 < 1e-9:
            d = np.hypot(xx - x0, yy - y0)
        else:
            t = ((xx - x0) * dx + (yy - y0) * dy) / L2
            t = np.clip(t, 0.0, 1.0)
            d = np.hypot(xx - (x0 + t * dx), yy - (y0 + t * dy))
        best = np.minimum(best, d)
    a = np.clip(1.0 - best / max(width, 1e-3), 0.0, 1.0)
    return a ** 0.85


def paint_stroke(img, alpha, color, gain=1.0):
    """Pinta una capa con alfa [0..1] sobre el icono (composición por máx.)."""
    a = np.clip(alpha * gain, 0.0, 1.0) * 255.0
    m = a > img[..., 3]
    img[..., 0] = np.where(m, color[0], img[..., 0])
    img[..., 1] = np.where(m, color[1], img[..., 1])
    img[..., 2] = np.where(m, color[2], img[..., 2])
    img[..., 3] = np.maximum(img[..., 3], a)


def zigzag(x0, y0, x1, y1, n, amp, phase=0.0):
    """Polilínea zigzag: puntos equidistantes con jitter perpendicular
    alternado (el mini-rayo del icono del Ascendido)."""
    pts = []
    dx, dy = x1 - x0, y1 - y0
    L = np.hypot(dx, dy)
    if L < 1e-6:
        return [(x0, y0), (x1, y1)]
    ux, uy = dx / L, dy / L
    nx, ny = -uy, ux
    for i in range(n + 1):
        f = i / n
        # envolvente: quieto en los anclajes, loco en el centro
        env = np.sin(f * np.pi)
        side = 1.0 if (i + int(phase)) % 2 == 0 else -1.0
        off = side * amp * env
        pts.append((x0 + ux * (L * f) + nx * off,
                    y0 + uy * (L * f) + ny * off))
    return pts


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


def gen_umbral_ascendido_icon():
    """UMBRAL ASCENDIDO: bastón rojizo + mini-agujero con anillo DOPPLER
    (derecha BLANCA cálida y gruesa, izquierda carmesí fina) + 2 MINI-RAYOS
    naranjas zigzag cayendo + destello dorado."""
    yy, xx = np.mgrid[0:OH, 0:OW].astype(float)
    img = np.zeros((OH, OW, 4), dtype=float)

    # --- el bastón (madera rojiza del sello elevado) ---
    staff_base(img, xx, yy, 88, 26, 20)

    # --- EL MINI-AGUJERO (sobre la punta) ---
    cx, cy = 19.5 * SS, 9.5 * SS
    r = np.hypot(xx - cx, yy - cy)

    R = 4.6 * SS

    # halo exterior naranja tenue (el aura del disco)
    halo = np.exp(-((r - 1.45 * R) / (0.85 * R)) ** 2)
    paint_stroke(img, halo * 0.38, (245, 105, 45))

    # ANILLO DOPPLER: elipse + ancho y color ASIMÉTRICOS por ángulo —
    # el lado DERECHO (que se acerca) GRUESO y blanco cálido; el
    # izquierdo (que se aleja) FINO y carmesí profundo.
    ring_hi = 1.75 * R
    ex = (xx - cx) / ring_hi
    ey = (yy - cy) / (ring_hi * 0.78)
    re = np.hypot(ex, ey)
    th = np.arctan2(ey, ex)

    hot = 0.5 + 0.5 * np.cos(th - 0.30)      # máx a la derecha
    hot2 = hot ** 1.5

    sigma = 0.20 + 0.15 * hot                # GRUESO donde arde
    ring_band = np.exp(-((re - 1.0) / sigma) ** 2)

    ring_r = 150 + 105 * hot2                # carmesí → blanco cálido
    ring_g = 25 + 215 * hot2
    ring_b = 30 + 180 * hot2
    a_ring = ring_band * (0.50 + 0.50 * hot2) * 255.0

    m_ring = a_ring > img[..., 3]
    img[..., 0] = np.where(m_ring, ring_r, img[..., 0])
    img[..., 1] = np.where(m_ring, ring_g, img[..., 1])
    img[..., 2] = np.where(m_ring, ring_b, img[..., 2])
    img[..., 3] = np.maximum(img[..., 3], a_ring)

    # el horizonte negro
    disk = r <= R
    img[..., 0] = np.where(disk, 8, img[..., 0])
    img[..., 1] = np.where(disk, 2, img[..., 1])
    img[..., 2] = np.where(disk, 2, img[..., 2])
    img[..., 3] = np.where(disk, 255, img[..., 3])

    # LOS 2 MINI-RAYOS NARANJAS — zigzags finos cayendo bajo el anillo
    # (la lluvia de rayos del Ascendido, en miniature).
    bolt_a = zigzag(21.5 * SS, 14.8 * SS, 24.6 * SS, 22.6 * SS, 5, 1.35 * SS)
    bolt_b = zigzag(18.2 * SS, 15.6 * SS, 19.9 * SS, 21.4 * SS, 4, 1.05 * SS,
                    phase=1.0)

    for bolt in (bolt_a, bolt_b):
        # funda ancha naranja (el halo del rayo)
        a_halo = polyline_alpha(xx, yy, bolt, 1.35 * SS)
        paint_stroke(img, a_halo, (255, 110, 25), gain=0.62)
        # núcleo fino cálido (la línea caliente)
        a_core = polyline_alpha(xx, yy, bolt, 0.55 * SS)
        paint_stroke(img, a_core, (255, 228, 165), gain=1.0)

    # destello de 4 puntas dorado (la perla del sello elevado)
    sx, sy = 23.5 * SS, 5.5 * SS
    dxs = np.abs(xx - sx)
    dys = np.abs(yy - sy)
    star_v = np.exp(-(dxs / (1.6 * SS)) ** 2) * np.exp(-(dys / (0.42 * SS)) ** 2)
    star_h = np.exp(-(dys / (1.6 * SS)) ** 2) * np.exp(-(dxs / (0.42 * SS)) ** 2)
    star = np.maximum(star_v, star_h)
    m_star = star > 0.25
    img[..., 0] = np.where(m_star, 255, img[..., 0])
    img[..., 1] = np.where(m_star, 225, img[..., 1])
    img[..., 2] = np.where(m_star, 150, img[..., 2])
    img[..., 3] = np.where(m_star, 255, img[..., 3])

    save_icon(img, 'UmbralAscendidoBlackHoleStaff.png')


def gen_bruma_ascendido_icon():
    """BRUMA ASCENDIDA: bastón teal + mini-agujero con ANILLO DE HUMO
    (banda suave con grumos) + MINI-ARCO ELÉCTRICO cian alrededor del
    horizonte + destello-cristal de hielo."""
    yy, xx = np.mgrid[0:OH, 0:OW].astype(float)
    img = np.zeros((OH, OW, 4), dtype=float)

    # --- el bastón (madera teal de la bruma elevada) ---
    staff_base(img, xx, yy, 18, 92, 102)

    # --- EL MINI-AGUJERO (sobre la punta) ---
    cx, cy = 19.5 * SS, 9.5 * SS
    r = np.hypot(xx - cx, yy - cy)

    R = 4.6 * SS

    # halo exterior teal tenue (la envoltura nebular)
    halo = np.exp(-((r - 1.5 * R) / (0.9 * R)) ** 2)
    paint_stroke(img, halo * 0.35, (55, 160, 185))

    # ANILLO DE HUMO: banda ANCHA y suave (sigma generoso) con GRUMOS por
    # ángulo (dos senos inconmensurables — el humo se amontona en masas).
    ring_hi = 1.8 * R
    ex = (xx - cx) / ring_hi
    ey = (yy - cy) / (ring_hi * 0.72)
    re = np.hypot(ex, ey)
    th = np.arctan2(ey, ex)

    clumps = 0.55 + 0.45 * np.sin(th * 5.0 + 1.7) * np.sin(th * 2.3 + 0.4)
    clumps = np.clip(clumps, 0.15, 1.0)

    ring_band = np.exp(-((re - 1.0) / 0.42) ** 2)

    ring_r = 40 + 140 * clumps              # teal → cian pálido
    ring_g = 140 + 95 * clumps
    ring_b = 165 + 80 * clumps
    a_ring = ring_band * clumps * 0.92 * 255.0

    m_ring = a_ring > img[..., 3]
    img[..., 0] = np.where(m_ring, ring_r, img[..., 0])
    img[..., 1] = np.where(m_ring, ring_g, img[..., 1])
    img[..., 2] = np.where(m_ring, ring_b, img[..., 2])
    img[..., 3] = np.maximum(img[..., 3], a_ring)

    # el horizonte negro
    disk = r <= R
    img[..., 0] = np.where(disk, 2, img[..., 0])
    img[..., 1] = np.where(disk, 6, img[..., 1])
    img[..., 2] = np.where(disk, 9, img[..., 2])
    img[..., 3] = np.where(disk, 255, img[..., 3])

    # EL MINI-ARCO ELÉCTRICO CIAN — la corona de escarcha en miniature:
    # ~110° de arco sobre el horizonte con jitter radial (12 puntos).
    arc_pts = []
    a0 = 2.35
    span = 1.95
    for i in range(13):
        f = i / 12.0
        ang = a0 + span * f
        # jitter radial determinista (el arco "tiembla")
        jit = 0.10 * np.sin(f * 9.0 + 1.3) + 0.06 * np.sin(f * 17.0)
        rad = (1.38 + jit) * R
        arc_pts.append((cx + np.cos(ang) * rad * 1.12,
                        cy + np.sin(ang) * rad * 0.85))

    # funda cian + núcleo blanco-cian (la doble tira de LightningCore)
    a_halo = polyline_alpha(xx, yy, arc_pts, 1.15 * SS)
    paint_stroke(img, a_halo, (110, 225, 250), gain=0.60)
    a_core = polyline_alpha(xx, yy, arc_pts, 0.48 * SS)
    paint_stroke(img, a_core, (235, 252, 255), gain=1.0)

    # destello de 4 puntas FRÍO (el cristal de hielo flotando)
    sx, sy = 23.5 * SS, 5.5 * SS
    dxs = np.abs(xx - sx)
    dys = np.abs(yy - sy)
    star_v = np.exp(-(dxs / (1.6 * SS)) ** 2) * np.exp(-(dys / (0.42 * SS)) ** 2)
    star_h = np.exp(-(dys / (1.6 * SS)) ** 2) * np.exp(-(dxs / (0.42 * SS)) ** 2)
    star = np.maximum(star_v, star_h)
    m_star = star > 0.25
    img[..., 0] = np.where(m_star, 205, img[..., 0])
    img[..., 1] = np.where(m_star, 240, img[..., 1])
    img[..., 2] = np.where(m_star, 255, img[..., 2])
    img[..., 3] = np.where(m_star, 255, img[..., 3])

    save_icon(img, 'BrumaAscendidoBlackHoleStaff.png')


if __name__ == '__main__':
    gen_shadows()
    gen_umbral_ascendido_icon()
    gen_bruma_ascendido_icon()
    print('\nv6.18 ascendidos (Umbral + Bruma) assets OK')
