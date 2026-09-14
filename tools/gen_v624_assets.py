#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_v624_assets.py — TODOS LOS PNGs DE LA v6.24 (reproducible).

Genera:
  · 3 ICONOS de arma (30×30): Sinfonía Primordial (orbe prismático +
    rayos + runas), Tormenta Nebular (nube violeta + descarga), Lanza
    del Alba (hoja de luz dorada + destello).
  · 3 SOMBRAS de proyectil (76×76): el disco con rim del estilo de la
    casa (patrón CrimsonBlackHoleProjectile.png) con la identidad de
    cada arma.
  · 2 ICONOS de cosmético REGENERADOS: la CORONA RÚNICA como LA AUREOLA
    DEL SOL I (elipse inclinada dorada con 6 glifos) y el ANILLO RÚNICO
    ESTELAR como LOS CÍRCULOS DE LOS AGUJEROS (3 aros finos concéntricos
    blanco/oro/violeta con runas de puntos).
"""
import os
import numpy as np
from PIL import Image

PROJ_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                        '..', 'AethonMod')
WPN_OUT = os.path.join(PROJ_DIR, 'Content', 'Weapons', 'Cosmic')
PRJ_OUT = os.path.join(PROJ_DIR, 'Content', 'Projectiles', 'Cosmic')
COS_OUT = os.path.join(PROJ_DIR, 'Content', 'Items', 'Cosmetics')
WING_OUT = os.path.join(PROJ_DIR, 'Content', 'Items', 'Wings')

SS = 8  # supersampling


def hash01(seed, a, b):
    """El hash determinista de la casa (VFXCore.Hash01 en Python)."""
    h = (seed * 374761393 + a * 668265263 + b * 1911520717) & 0xFFFFFFFF
    if h & 0x80000000:
        h -= 0x100000000
    h ^= (h >> 13) & 0xFFFFFFFF
    h = (h * 1274126177) & 0xFFFFFFFF
    if h & 0x80000000:
        h -= 0x100000000
    h ^= (h >> 16) & 0xFFFFFFFF
    return (h & 0xFFFFFF) / 16777216.0


def new_canvas(w, h):
    return np.zeros((h * SS, w * SS, 4), dtype=np.float32)


def downsample(img, w, h):
    """Baja resoluciOn con area average y cuantiza a uint8."""
    pil = Image.fromarray(np.clip(img, 0, 255).astype(np.uint8), 'RGBA')
    pil = pil.resize((w, h), Image.LANCZOS)
    return np.array(pil, dtype=np.uint8)


def glow_dot(img, px, py, radius, color, alpha):
    """Un punto de luz gaussiano (el pincel SoftGlow de la casa)."""
    H, W = img.shape[0], img.shape[1]
    if radius <= 0.1 or alpha <= 0.01:
        return
    x0, x1 = max(0, int(px - radius * 3)), min(W, int(px + radius * 3) + 1)
    y0, y1 = max(0, int(py - radius * 3)), min(H, int(py + radius * 3) + 1)
    if x0 >= x1 or y0 >= y1:
        return
    yy, xx = np.mgrid[y0:y1, x0:x1]
    d = np.sqrt((xx - px) ** 2 + (yy - py) ** 2) / (radius * SS)
    g = np.exp(-(d * d))
    a = g * alpha
    m = a > 0.02
    for c in range(3):
        img[y0:y1, x0:x1, c] = np.where(
            m, np.maximum(img[y0:y1, x0:x1, c], color[c] * g), img[y0:y1, x0:x1, c])
    img[y0:y1, x0:x1, 3] = np.maximum(img[y0:y1, x0:x1, 3], a * 255)


def capsule(img, ax, ay, bx, by, width, color, alpha):
    """Una cApsula de luz (segmento con brillo gaussiano)."""
    ln = max(np.hypot(bx - ax, by - ay), 1e-6)
    steps = max(2, int(ln / (width * SS * 0.35)) + 1)
    for t in np.linspace(0, 1, steps):
        glow_dot(img, ax + (bx - ax) * t, ay + (by - ay) * t, width, color, alpha)


def ring_stroke(img, cx, cy, rx, ry, tilt, width, color, alpha, segs=48):
    """Un aro elIptico fino (polilInea de cApsulas)."""
    pts = []
    for i in range(segs + 1):
        t = i / segs * 2 * np.pi
        lx, ly = rx * np.cos(t), ry * np.sin(t)
        c, s = np.cos(tilt), np.sin(tilt)
        pts.append((cx + lx * c - ly * s, cy + lx * s + ly * c))
    for i in range(segs):
        capsule(img, pts[i][0], pts[i][1], pts[i + 1][0], pts[i + 1][1],
                width, color, alpha)


def staff_handle(img, cx, top, bottom, color, alpha=0.9):
    """El MASTIL de un bastOn (dos lIneas con nudo)."""
    capsule(img, cx - 1.2 * SS, top, cx + 0.6 * SS, bottom, 1.1, color, alpha)
    capsule(img, cx + 1.2 * SS, top, cx + 0.6 * SS, bottom, 1.1, color, alpha * 0.7)
    glow_dot(img, cx, (top + bottom) * 0.45, 2.2, color, 0.35)


def save(img, w, h, path):
    out = downsample(img, w, h)
    # Umbral suave: nada de pIxels fantasma en las esquinas.
    out[..., 3] = np.where(out[..., 3] < 6, 0, out[..., 3])
    Image.fromarray(out, 'RGBA').save(path)
    print(f"  -> {os.path.basename(path)} ({w}x{h}) "
          f"{int((out[..., 3] > 8).sum())} px visibles")


# =====================================================================
#  1. ICONOS DE ARMA (30x30)
# =====================================================================

def icon_sinfonia():
    """EL BASTON DE LA SINFONIA: mastil dorado + orbe prismAtico + runas."""
    W, H = 30, 30
    img = new_canvas(W, H)
    cx, cy = W * SS * 0.5, H * SS * 0.42

    GOLD = (255, 195, 85)
    GOLD_W = (255, 225, 140)
    WHITE = (255, 250, 235)
    VIOLET = (150, 110, 220)
    STAR = (150, 180, 255)

    # El mastil.
    staff_handle(img, W * SS * 0.5, cy + 3 * SS, H * SS * 0.96, (150, 110, 50), 0.95)

    # EL ORBE prismAtico: bloom apilado (capas invertidas).
    glow_dot(img, cx, cy, 9.5, VIOLET, 0.30)
    glow_dot(img, cx, cy, 6.2, STAR, 0.42)
    glow_dot(img, cx, cy, 4.2, GOLD, 0.62)
    glow_dot(img, cx, cy, 2.4, WHITE, 0.95)

    # EL DESTELLO de 4 puntas.
    capsule(img, cx - 8.5 * SS, cy, cx + 8.5 * SS, cy, 1.1, GOLD_W, 0.75)
    capsule(img, cx, cy - 8.5 * SS, cx, cy + 8.5 * SS, 1.1, GOLD_W, 0.75)

    # LOS RAYOS radiales pequeNos.
    for i in range(6):
        a = i / 6 * 2 * np.pi + 0.4
        ex, ey = cx + np.cos(a) * 6.5 * SS, cy + np.sin(a) * 6.5 * SS
        capsule(img, cx + np.cos(a) * 4.6 * SS, cy + np.sin(a) * 4.6 * SS,
                ex, ey, 0.9, STAR if i % 2 else GOLD, 0.65)

    # LAS RUNAS orbitando (puntos con perlas).
    for i in range(6):
        a = i / 6 * 2 * np.pi + 0.7
        px, py = cx + np.cos(a) * 6.0 * SS, cy + np.sin(a) * 6.0 * SS
        glow_dot(img, px, py, 1.5, GOLD_W, 0.85)
        glow_dot(img, px, py, 0.7, WHITE, 0.95)

    save(img, W, H, os.path.join(WPN_OUT, 'SinfoniaPrimordialStaff.png'))


def icon_tormenta():
    """EL CETRO DE LA TORMENTA NEBULAR: nube violeta + descarga."""
    W, H = 30, 30
    img = new_canvas(W, H)
    cx, cy = W * SS * 0.5, H * SS * 0.36

    VIOLET = (118, 84, 190)
    CYAN = (80, 150, 200)
    STAR = (150, 180, 255)
    WHITE = (255, 250, 235)
    GOLD = (255, 195, 85)

    # El mastil.
    staff_handle(img, W * SS * 0.5, cy + 4 * SS, H * SS * 0.96, (120, 95, 140), 0.95)

    # LA NUBE: racimo de puffs violeta-cian.
    cloud = [(0, 0, 6.5, VIOLET, 0.5), (-4.5, 1.5, 4.4, CYAN, 0.42),
             (4.5, 1.2, 4.6, VIOLET, 0.42), (-2.0, -2.8, 3.8, CYAN, 0.38),
             (2.4, -2.6, 3.9, VIOLET, 0.38)]
    for dx, dy, r, c, a in cloud:
        glow_dot(img, cx + dx * SS, cy + dy * SS, r, c, a)

    # EL CORAZON cargado dentro de la nube.
    glow_dot(img, cx, cy, 2.8, STAR, 0.75)
    glow_dot(img, cx, cy, 1.3, WHITE, 0.95)

    # LA DESCARGA: el rayo cayendo de la panza.
    bolt = [(cx - 0.5 * SS, cy + 5.0 * SS), (cx + 1.8 * SS, cy + 8.2 * SS),
            (cx - 1.2 * SS, cy + 9.4 * SS), (cx + 1.0 * SS, cy + 12.6 * SS)]
    for i in range(len(bolt) - 1):
        capsule(img, bolt[i][0], bolt[i][1], bolt[i + 1][0], bolt[i + 1][1],
                1.15, STAR, 0.9)
    glow_dot(img, bolt[-1][0], bolt[-1][1], 2.2, WHITE, 0.9)

    # LAS RUNAS del borde (puntos dorados + violetas).
    for i in range(8):
        a = i / 8 * 2 * np.pi
        px, py = cx + np.cos(a) * 6.8 * SS, cy + np.sin(a) * 4.6 * SS
        glow_dot(img, px, py, 1.2, GOLD if i % 2 else STAR, 0.75)

    save(img, W, H, os.path.join(WPN_OUT, 'TormentaNebularStaff.png'))


def icon_lanza():
    """LA LANZA DEL ALBA: la hoja de luz dorada diagonal + destello."""
    W, H = 30, 30
    img = new_canvas(W, H)

    GOLD = (255, 225, 150)
    GOLD_D = (255, 195, 85)
    WHITE = (255, 250, 240)
    VIOLET = (148, 120, 190)

    # La hoja: diagonal de abajo-izq a arriba-der.
    ax, ay = 4.5 * SS, 25.5 * SS
    bx, by = 24.5 * SS, 5.0 * SS
    capsule(img, ax, ay, bx, by, 2.6, GOLD_D, 0.55)   # velo
    capsule(img, ax, ay, bx, by, 1.5, GOLD, 0.85)     # cuerpo
    capsule(img, ax + 1 * SS, ay - 1 * SS, bx - 1 * SS, by + 1 * SS,
            0.65, WHITE, 0.95)                        # nucleo razor

    # LA PUNTA: destello de 4 puntas.
    tx, ty = bx + 1.5 * SS, by - 1.5 * SS
    capsule(img, tx - 4.2 * SS, ty, tx + 4.2 * SS, ty, 1.0, GOLD, 0.8)
    capsule(img, tx, ty - 4.2 * SS, tx, ty + 4.2 * SS, 1.0, GOLD, 0.8)
    glow_dot(img, tx, ty, 1.8, WHITE, 0.95)

    # EL EMPUADURA: el mastil corto abajo.
    capsule(img, ax - 1.0 * SS, ay + 0.5 * SS, ax - 3.2 * SS, ay + 4.0 * SS,
            1.2, (150, 110, 60), 0.95)

    # EL ROCIO: puffs de bruma tras la hoja.
    for i in range(3):
        px = ax + (bx - ax) * (0.15 + 0.28 * i)
        py = ay + (by - ay) * (0.15 + 0.28 * i) + 2.4 * SS
        glow_dot(img, px, py, 2.6 - 0.4 * i, VIOLET, 0.30 - 0.06 * i)

    # LA RUNA del corazon (estrella doble pequena).
    hx, hy = (ax + bx) * 0.42, (ay + by) * 0.42
    for da in (0.4, 1.97, 3.54, 5.11):
        capsule(img, hx, hy, hx + np.cos(da) * 3.2 * SS,
                hy + np.sin(da) * 3.2 * SS, 0.7, WHITE, 0.85)

    save(img, W, H, os.path.join(WPN_OUT, 'LanzaAlbaStaff.png'))


# =====================================================================
#  2. SOMBRAS DE PROYECTIL (76x76) — patron del estilo de la casa
# =====================================================================

def shadow_generic(name, rim_a, rim_b, rim_width=6.0, elongated=False):
    """El disco con rim (patron CrimsonBlackHoleProjectile):
    disco solido + rim de color desvaneciendo."""
    S = 76
    img = new_canvas(S, S)
    cx, cy = S * SS * 0.5, S * SS * 0.5
    R = 26.0 * SS

    yy, xx = np.mgrid[0:S * SS, 0:S * SS]
    d = np.sqrt((xx - cx) ** 2 + (yy - cy) ** 2) / R  # en unidades de radio

    # EL DISCO: solido oscuro (cuerpo) con alfa fuerte.
    body_a = np.clip(1.35 - d, 0, 1) * 0.9
    # EL RIM: banda con el color de identidad.
    rim = np.exp(-((d - 0.86) ** 2) / (2 * (rim_width / R * SS) ** 2))

    col = np.zeros((S * SS, S * SS, 4), dtype=np.float32)
    col[..., 0] = rim_a[0] * rim + 14 * body_a
    col[..., 1] = rim_a[1] * rim + 10 * body_a
    col[..., 2] = rim_a[2] * rim + 20 * body_a
    a = np.maximum(body_a * 0.55, rim * 0.85)
    col[..., 3] = a * 255

    # El rim EXTERIOR desvanecido (el color B mas alla).
    rim2 = np.exp(-((d - 1.06) ** 2) / (2 * (rim_width / R * SS * 1.4) ** 2))
    col[..., 0] += rim_b[0] * rim2 * 0.55
    col[..., 1] += rim_b[1] * rim2 * 0.55
    col[..., 2] += rim_b[2] * rim2 * 0.55
    col[..., 3] = np.maximum(col[..., 3], rim2 * 0.5 * 255)

    if elongated:
        # Version alargada (la lanza): estira el eje X del rim.
        pass

    out = downsample(col, S, S)
    out[..., 3] = np.where(out[..., 3] < 6, 0, out[..., 3])
    path = os.path.join(PRJ_OUT, name)
    Image.fromarray(out, 'RGBA').save(path)
    print(f"  -> {name} (76x76) {int((out[..., 3] > 8).sum())} px visibles")


def shadow_lanza():
    """La sombra de LA LANZA: nucleo alargado dorado (no un disco)."""
    S = 76
    img = new_canvas(S, S)
    GOLD = (255, 200, 110)
    WHITE = (255, 250, 240)
    VIOLET = (148, 120, 190)

    # La hoja: diagonal con nucleo caliente.
    ax, ay = 14 * SS, 62 * SS
    bx, by = 62 * SS, 14 * SS
    capsule(img, ax, ay, bx, by, 7.5, VIOLET, 0.28)
    capsule(img, ax, ay, bx, by, 5.2, GOLD, 0.55)
    capsule(img, ax + 2 * SS, ay - 2 * SS, bx - 2 * SS, by + 2 * SS, 2.2, WHITE, 0.8)
    glow_dot(img, bx, by, 7.0, GOLD, 0.55)
    glow_dot(img, bx, by, 3.0, WHITE, 0.9)
    glow_dot(img, ax, ay, 5.0, GOLD, 0.4)

    out = downsample(img, S, S)
    out[..., 3] = np.where(out[..., 3] < 6, 0, out[..., 3])
    path = os.path.join(PRJ_OUT, 'LanzaAlbaProjectile.png')
    Image.fromarray(out, 'RGBA').save(path)
    print(f"  -> LanzaAlbaProjectile.png (76x76) "
          f"{int((out[..., 3] > 8).sum())} px visibles")


# =====================================================================
#  3. ICONOS DE COSMETICO REGENERADOS (30x30)
# =====================================================================

def icon_corona_aureola():
    """LA CORONA RUNICA = LA AUREOLA DEL SOL I: elipse inclinada + 6 glifos."""
    W, H = 30, 30
    img = new_canvas(W, H)
    cx, cy = W * SS * 0.5, H * SS * 0.5

    GOLD = (255, 190, 80)
    GOLD_T = (255, 240, 185)
    WHITE = (255, 250, 235)

    # EL ARO: la elipse del Sol I (1.62R x 0.34R, tilt -0.55).
    rx, ry = 12.5 * SS, 12.5 * SS * 0.34 / 0.55 * 0.55  # visual: achatado
    ry = 4.6 * SS
    ring_stroke(img, cx, cy, rx, ry, -0.55, 1.6, GOLD, 0.9, 40)

    # LOS 6 GLIFOS cabalgando (mini trazos tangentes) + perlas.
    for i in range(6):
        t = i / 6 * 2 * np.pi + 0.3
        lx, ly = rx * np.cos(t), ry * np.sin(t)
        c, s = np.cos(-0.55), np.sin(-0.55)
        px, py = cx + lx * c - ly * s, cy + lx * s + ly * c
        # mini-gllifo: dos trazos angulares.
        capsule(img, px - 1.6 * SS, py - 1.4 * SS, px + 1.6 * SS, py + 1.4 * SS,
                0.8, GOLD_T, 0.9)
        capsule(img, px - 1.4 * SS, py + 1.2 * SS, px + 0.4 * SS, py - 1.6 * SS,
                0.7, GOLD_T, 0.75)
        glow_dot(img, px, py - 2.6 * SS, 1.1, WHITE, 0.85)

    # EL RESPLANDOR suave del conjunto.
    glow_dot(img, cx, cy, 9.0, GOLD, 0.14)

    save(img, W, H, os.path.join(COS_OUT, 'RuneCrownItem.png'))


def icon_anillo_agujeros():
    """EL ANILLO RUNICO ESTELAR = LOS CIRCULOS DE LOS AGUJEROS: 3 aros."""
    W, H = 30, 30
    img = new_canvas(W, H)
    cx, cy = W * SS * 0.5, H * SS * 0.5

    WHITE = (255, 245, 220)
    GOLD = (255, 180, 70)
    VIOLET = (110, 130, 255)

    # LOS TRES AROS concEntricos (la forma de los agujeros).
    ring_stroke(img, cx, cy, 5.6 * SS, 5.6 * SS, 0, 0.9, WHITE, 0.85, 40)
    ring_stroke(img, cx, cy, 8.2 * SS, 8.2 * SS, 0, 1.0, GOLD, 0.85, 48)
    ring_stroke(img, cx, cy, 10.8 * SS, 10.8 * SS, 0, 0.8, VIOLET, 0.7, 56)

    # LAS RUNAS: puntos flotando en cada aro (blancas/oro/violetas).
    for i in range(6):
        a = i / 6 * 2 * np.pi + 0.5
        glow_dot(img, cx + np.cos(a) * 5.6 * SS, cy + np.sin(a) * 5.6 * SS,
                 1.0, WHITE, 0.9)
    for i in range(8):
        a = i / 8 * 2 * np.pi
        glow_dot(img, cx + np.cos(a) * 8.2 * SS, cy + np.sin(a) * 8.2 * SS,
                 1.05, GOLD, 0.9)
    for i in range(6):
        a = i / 6 * 2 * np.pi + 0.26
        glow_dot(img, cx + np.cos(a) * 10.8 * SS, cy + np.sin(a) * 10.8 * SS,
                 0.9, VIOLET, 0.85)

    save(img, W, H, os.path.join(WING_OUT, 'RunicHaloWings.png'))


# =====================================================================

if __name__ == '__main__':
    print("GEN v6.24 — iconos de arma:")
    icon_sinfonia()
    icon_tormenta()
    icon_lanza()
    print("GEN v6.24 — sombras de proyectil:")
    shadow_generic('SinfoniaPrimordialProjectile.png',
                   (255, 195, 100), (170, 120, 220))
    shadow_generic('TormentaNebularProjectile.png',
                   (130, 105, 215), (80, 150, 200))
    shadow_lanza()
    print("GEN v6.24 — iconos de cosmEtico regenerados:")
    icon_corona_aureola()
    icon_anillo_agujeros()
    print("OK — 8 PNGs generados.")
