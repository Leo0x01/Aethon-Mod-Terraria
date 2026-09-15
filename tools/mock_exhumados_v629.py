#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
mock_exhumados_v629.py — EL MOCK 1:1 DE LOS DOS EXHUMADOS.

Simula con PIL/numpy el dibujo EXACTO de:
  1. EL RENCOR PRIMORDIAL — el círculo de transmutación cargando (runas
     encendidas progresivamente + pentagrama + espiral de bruma) y EL HAZ
     CONTINUO con los brazos espectrales brotando de la pared.
  2. LA EMINENCIA ATROZ — la congregación (masa pálida + ojos + LA CARA
     Giygas) y la ABOMINACIÓN completa (ojo mayor + estela + corona).

Para verificación VLM de la geometría/las capas ANTES del juego real.
"""
import numpy as np
from PIL import Image, ImageDraw

W, H = 960, 520
BG = (14, 10, 20)


def canvas():
    return np.zeros((H, W, 3), dtype=np.float32) + np.array(BG, dtype=np.float32)


def alpha_blend_disc(img, px, py, radius, color, alpha):
    """El disco alfa (COMO EL JUEGO): mezcla el color SOBRE el fondo —
    los NEGROS oscurecen de verdad (el pase alfa del DiscoNegro)."""
    x0, x1 = max(0, int(px - radius * 1.6)), min(W, int(px + radius * 1.6) + 1)
    y0, y1 = max(0, int(py - radius * 1.6)), min(H, int(py + radius * 1.6) + 1)
    if x0 >= x1 or y0 >= y1:
        return
    xs = np.arange(x0, x1) - px
    ys = np.arange(y0, y1) - py
    gx = np.exp(-(xs / max(radius, 0.1)) ** 4)  # disco plano con borde suave
    gy = np.exp(-(ys / max(radius, 0.1)) ** 4)
    g = np.outer(gy, gx) * alpha
    for c in range(3):
        img[y0:y1, x0:x1, c] = img[y0:y1, x0:x1, c] * (1 - g) + color[c] * g


def add_color(img, px, py, radius, color, alpha):
    x0, x1 = max(0, int(px - radius * 3)), min(W, int(px + radius * 3) + 1)
    y0, y1 = max(0, int(py - radius * 3)), min(H, int(py + radius * 3) + 1)
    if x0 >= x1 or y0 >= y1:
        return
    xs = np.arange(x0, x1) - px
    ys = np.arange(y0, y1) - py
    gx = np.exp(-(xs / max(radius, 0.1)) ** 2)
    gy = np.exp(-(ys / max(radius, 0.1)) ** 2)
    g = np.outer(gy, gx) * alpha
    img[y0:y1, x0:x1, 0] += g * color[0]
    img[y0:y1, x0:x1, 1] += g * color[1]
    img[y0:y1, x0:x1, 2] += g * color[2]


def capsule(img, x0, y0, x1, y1, width, color, alpha):
    L = np.hypot(x1 - x0, y1 - y0)
    n = max(2, int(L / 3))
    for t in np.linspace(0, 1, n):
        px = x0 + (x1 - x0) * t
        py = y0 + (y1 - y0) * t
        add_color(img, px, py, width * 0.5, color, alpha)


def quad_beam(img, origin, dirx, diry, length, height, color, alpha):
    """El haz en UN SOLO CUAD con perfil de longitud (lens sin^0.6)."""
    ys = np.arange(H)
    xs = np.arange(W)
    XX, YY = np.meshgrid(xs, ys)
    # Coordenadas a lo largo del haz.
    along = (XX - origin[0]) * dirx + (YY - origin[1]) * diry
    perp = -(XX - origin[0]) * diry + (YY - origin[1]) * dirx
    t = np.clip(along / length, 0, 1)
    perfil = np.sin(np.clip(t, 0.001, 0.999) * np.pi) ** 0.6
    half = height * 0.5 * perfil
    d = np.abs(perp)
    dentro = (along > 0) & (along < length) & (d < half + 2)
    fall = np.exp(-(d / np.maximum(half, 0.5)) ** 2)
    g = np.where(dentro, fall * alpha, 0)
    img[:, :, 0] += g * color[0]
    img[:, :, 1] += g * color[1]
    img[:, :, 2] += g * color[2]


# =====================================================================
#  1. EL RENCOR — círculo cargando (izq) + HAZ CON BRAZOS (der)
# =====================================================================

def mock_rencor():
    img = canvas()
    ORO = (255, 210, 110)
    VIOLETA = (150, 110, 255)
    CARMESI = (255, 60, 110)
    AMBAR = (255, 140, 60)
    BLANCO = (255, 244, 214)
    HUESO = (255, 243, 228)

    # ---- PANEL A: EL CÍRCULO CARGANDO (charge = 0.55) ----
    cx, cy = 240, 260
    charge = 0.55

    # La espiral de bruma (se reúne hacia dentro).
    for k in range(14):
        t = k / 13
        ang = 1.8 + t * 4.2
        radio = (92 - 74 * t) * (0.6 + 0.4 * charge)
        px = cx + np.cos(ang) * radio
        py = cy + np.sin(ang) * radio * 0.8
        add_color(img, px, py, 7, (96, 60, 110), 0.30 * charge)

    # Los dos anillos.
    for rr, col, a in [(95, ORO, 0.4), (64, VIOLETA, 0.35)]:
        for adeg in np.linspace(0, 2 * np.pi, 200):
            add_color(img, cx + np.cos(adeg) * rr, cy + np.sin(adeg) * rr * 0.82,
                      2.2, col, a)

    # Las runas (10 oro CW, 6 violeta CCW) — encendidas progresivamente.
    lit_oro = int(np.ceil(10 * charge))
    for g in range(10):
        ang = g / 10 * 2 * np.pi + 0.6
        rx, ry = cx + np.cos(ang) * 96, cy + np.sin(ang) * 96 * 0.82
        lit = 1.0 if g < lit_oro else 0.15
        # El glifo (3 trazos).
        capsule(img, rx - 4, ry + 5, rx, ry - 6, 2.6, ORO, 0.85 * lit)
        capsule(img, rx, ry - 6, rx + 4, ry + 5, 2.6, ORO, 0.85 * lit)
        capsule(img, rx - 2, ry + 2, rx + 2, ry + 2, 2.0, ORO, 0.6 * lit)
        add_color(img, rx, ry, 10, ORO, 0.18 * lit)
    lit_v = int(np.ceil(6 * charge))
    for g in range(6):
        ang = g / 6 * 2 * np.pi - 0.8
        rx, ry = cx + np.cos(ang) * 64, cy + np.sin(ang) * 64 * 0.82
        lit = 1.0 if g < lit_v else 0.15
        capsule(img, rx - 4, ry, rx + 4, ry, 2.4, VIOLETA, 0.85 * lit)
        capsule(img, rx, ry - 5, rx, ry + 5, 2.4, VIOLETA, 0.85 * lit)
        add_color(img, rx, ry, 9, VIOLETA, 0.18 * lit)

    # EL PENTAGRAMA (visible desde charge 0.35).
    sa = (charge - 0.35) / 0.4
    sa = np.clip(sa, 0, 1)
    if sa > 0:
        R = 56
        pts = [(cx + np.cos(0.3 + p / 5 * 2 * np.pi - np.pi / 2) * R,
                cy + np.sin(0.3 + p / 5 * 2 * np.pi - np.pi / 2) * R) for p in range(5)]
        orden = [0, 2, 4, 1, 3, 0]
        for i in range(len(orden) - 1):
            a, b = pts[orden[i]], pts[orden[i + 1]]
            capsule(img, a[0], a[1], b[0], b[1], 7, CARMESI, 0.20 * sa)
            capsule(img, a[0], a[1], b[0], b[1], 3.4, HUESO, 0.55 * sa)

    # El corazón del ritual + el telegraph.
    add_color(img, cx, cy, 16 + 30 * charge, CARMESI, 0.30 + 0.30 * charge)
    add_color(img, cx, cy, 10, BLANCO, 0.5)
    # Telegraph: línea tenue hacia el haz futuro.
    capsule(img, cx, cy, cx + 260, cy + 60, 2.5, CARMESI, 0.35 * charge)

    # ---- PANEL B: EL HAZ + LOS BRAZOS (el disparo) ----
    ox, oy = 620, 300
    dirx, diry = 0.993, -0.118
    L = 320
    # La pared al final del haz.
    wall_x = ox + dirx * L
    wall_y = oy + diry * L

    # EL HAZ CONTINUO — 3 capas (velo → cuerpo → núcleo).
    quad_beam(img, (ox, oy), dirx, diry, L, 44 * 2.2, (180, 30, 90), 0.30)
    quad_beam(img, (ox, oy), dirx, diry, L, 44 * 1.35, CARMESI, 0.55)
    quad_beam(img, (ox, oy), dirx, diry, L, 44 * 0.62, AMBAR, 0.80)
    quad_beam(img, (ox, oy), dirx, diry, L * 0.96, 44 * 0.26, BLANCO, 0.95)

    # La boca del haz.
    add_color(img, ox, oy, 30, CARMESI, 0.8)
    add_color(img, ox, oy, 14, BLANCO, 0.9)

    # LA PARED (tile sólido estilizado).
    for wy in range(int(wall_y - 90), int(wall_y + 90)):
        add_color(img, wall_x + 14, wy, 8, (52, 44, 58), 0.9)

    # LA FOG del impacto.
    add_color(img, wall_x - 8, wall_y - 20, 26, (96, 60, 110), 0.30)
    add_color(img, wall_x + 4, wall_y + 8, 20, (96, 60, 110), 0.22)

    # LA LAVA (la llama lamiendo el tile).
    for k in range(4):
        h1 = 0.3 + 0.2 * k
        add_color(img, wall_x - 6 + (k - 1.5) * 10, wall_y + 18, 8 + 4 * h1,
                  (255, 140, 40), 0.45)

    # LOS BRAZOS ESPECTRALES (4, creciendo hacia -dir con abanico).
    import math
    baseAng = math.atan2(diry, dirx) + math.pi  # contra el haz
    for b in range(4):
        grow = [1.0, 0.85, 0.55, 0.25][b]
        ang = baseAng + (b - 1.5) * 0.55
        sway = math.sin(0.13 * 40 + b * 2.1) * 0.22
        largo = 48 * grow
        a1 = ang + sway * 0.5
        a2 = ang + sway
        bx, by = wall_x - dirx * 6, wall_y - diry * 6
        codo = (bx + math.cos(a1) * largo * 0.45, by + math.sin(a1) * largo * 0.45)
        mano = (codo[0] + math.cos(a2) * largo * 0.55,
                codo[1] + math.sin(a2) * largo * 0.55)
        # El halo carmesí + el hueso (2 capas por segmento).
        capsule(img, bx, by, codo[0], codo[1], 9 * 2.1, CARMESI, 0.28)
        capsule(img, bx, by, codo[0], codo[1], 9, HUESO, 0.78)
        capsule(img, codo[0], codo[1], mano[0], mano[1], 7 * 2.1, CARMESI, 0.28)
        capsule(img, codo[0], codo[1], mano[0], mano[1], 7, HUESO, 0.78)
        # La mano + los dedos.
        add_color(img, mano[0], mano[1], 6, HUESO, 0.65)
        angMano = math.atan2(mano[1] - codo[1], mano[0] - codo[0])
        for f in range(4):
            fa = angMano + (f - 1.5) * 0.38
            fl = 13 + 4 * (f % 2)
            punta = (mano[0] + math.cos(fa) * fl, mano[1] + math.sin(fa) * fl)
            capsule(img, mano[0], mano[1], punta[0], punta[1], 3.2, HUESO, 0.62)
        # El aura del punto de brote.
        add_color(img, bx, by, 18, CARMESI, 0.22)

    # LAS ASCUAS (sparks dorados alrededor del impacto).
    for k in range(9):
        a = k * 0.7 + 0.4
        r = 20 + (k % 4) * 14
        add_color(img, wall_x - 8 + math.cos(a) * r, wall_y + math.sin(a) * r * 0.8,
                  2.5, (255, 170, 60), 0.85)

    return img


# =====================================================================
#  2. LA EMINENCIA — la congregación (izq) + LA ABOMINACIÓN (der)
# =====================================================================

def mock_eminencia():
    img = canvas()
    PALIDO = (225, 240, 238)
    OJO = (245, 250, 250)
    CARMIN = (255, 70, 90)
    INTERNO = (84, 64, 104)

    # ---- PANEL A: LA CONGREGACIÓN (growth = 0.45, LA CARA abierta) ----
    cx, cy = 240, 270
    growth = 0.45
    radio = (44 + 26 * growth) * 1.0

    # La masa (pase ALFA — masa que OCLUYE, NO aditiva: mezcla oscura).
    rng = np.random.RandomState(7)
    ys_, xs_ = np.mgrid[0:H, 0:W]
    masa_d = np.sqrt((xs_ - cx) ** 2 + (ys_ - cy) ** 2) / radio
    dentro = np.clip(1.0 - masa_d, 0, 1)
    # El cuerpo de la nube: pálido TRANSPARENTE (alpha máx 0.55).
    for c, v in [(0, 170), (1, 185), (2, 190)]:
        img[:, :, c] = img[:, :, c] * (1 - dentro * 0.55) + v * (dentro * 0.55)
    # El interior MÁS OSCURO (la profundidad).
    for c, v in [(0, 70), (1, 54), (2, 88)]:
        core = np.clip(1.0 - masa_d / 0.62, 0, 1)
        img[:, :, c] = img[:, :, c] * (1 - core * 0.45) + v * (core * 0.45)
    # Los puffs del borde (el ruido de la silueta).
    for p in range(6):
        px = cx + (rng.random() - 0.5) * radio * 1.15
        py = cy + (rng.random() - 0.5) * radio * 0.85
        add_color(img, px, py, radio * 0.32, (150, 168, 172), 0.10)

    # El corazón tenue (additive SUAVE — 0.10, no 0.20).
    add_color(img, cx, cy, radio * 0.55, PALIDO, 0.10)
    for b in range(8):
        a = b / 8 * 2 * np.pi + 0.5
        col = [(120, 90, 200), (90, 130, 190), (200, 120, 160)][b % 3]
        add_color(img, cx + np.cos(a) * radio * 0.5,
                  cy + np.sin(a) * radio * 0.4, 7, col, 0.12)

    # LOS ESPÍRITUS MENORES (fuera = 0.7 del ciclo).
    for i in range(6):
        h1 = (i * 0.37) % 1
        ang = i * 1.13 + 0.8
        fuera = 0.7
        r_esp = radio * (0.55 + 0.5 * h1) + 62 * fuera
        px = cx + np.cos(ang) * r_esp
        py = cy + np.sin(ang) * r_esp * 0.82 - 8
        add_color(img, px, py, 6, PALIDO, 0.22)
        add_color(img, px, py, 2.2, OJO, 0.55)
        # La estelita (ribbon corto hacia atrás).
        for k in range(4):
            a2 = ang - k * 0.12
            r2 = radio * (0.55 + 0.5 * h1) + 62 * max(0.1, fuera - k * 0.06)
            add_color(img, cx + np.cos(a2) * r2, cy + np.sin(a2) * r2 * 0.82 - 8,
                      2.6, PALIDO, 0.18 * (1 - k / 4))

    # LOS OJOS (2 + 6·0.45 ≈ 4) — con NEGROS DE VERDAD (pase alfa):
    # zócalo negro → esclerótica blanca → pupila negra → destello carmesí al lado.
    ojos = [(cx - 22, cy - 8, 5, (-0.3, 0.2)), (cx + 16, cy + 12, 4, (0.25, -0.15)),
            (cx + 6, cy - 22, 3.5, (0.1, 0.3)), (cx - 8, cy + 24, 3, (-0.2, -0.25))]
    for (ox, oy, ot, (mx, my)) in ojos:
        alpha_blend_disc(img, ox, oy, ot * 2.5, (10, 5, 16), 0.55)      # zócalo
        alpha_blend_disc(img, ox, oy, ot * 1.7, (245, 250, 250), 0.80)  # esclerótica
        alpha_blend_disc(img, ox + mx * ot, oy + my * ot, ot * 0.72,
                         (10, 5, 16), 0.92)                              # pupila
        add_color(img, ox - mx * ot * 1.7, oy - my * ot * 1.7,
                  ot * 0.55, CARMIN, 0.75)                               # destello

    # LA CARA (la ventana Giygas) — alfa: zócalo→esclerótica→pupila + boca NEGRA.
    posOjo = (cx + radio * 0.12, cy - radio * 0.10)
    tam = radio * 0.55
    alpha_blend_disc(img, posOjo[0], posOjo[1], tam * 2.4, (10, 5, 16), 0.55)
    alpha_blend_disc(img, posOjo[0], posOjo[1], tam * 1.6, (245, 250, 250), 0.75)
    alpha_blend_disc(img, posOjo[0] + 3, posOjo[1] + 2, tam * 0.85, (10, 5, 16), 0.88)
    # La boca (la voluta negra de verdad).
    posBoca = (cx - radio * 0.06, cy + radio * 0.42)
    for k in range(5):
        t = k / 4
        bx = posBoca[0] + (t - 0.5) * radio * 0.55
        by = posBoca[1] + 4 * np.sin(t * np.pi)
        alpha_blend_disc(img, bx, by, 7, (10, 5, 16), 0.80)
        add_color(img, bx, by + 2, 2.5, CARMIN, 0.30)

    # ---- PANEL B: LA ABOMINACIÓN (growth = 1.0) ----
    ax, ay = 700, 250
    radio_ab = (44 + 26) * 0.85  # APRETADA

    # La estela Comet (el rastro).
    for k in range(10):
        t = k / 9
        ex = ax + t * 170
        ey = ay + np.sin(t * 3.1) * 26
        wdt = 26 * (1 - t) ** 2 + 3
        add_color(img, ex, ey, wdt * 0.8, PALIDO, 0.16 * (1 - t))

    # La masa apretada (pase ALFA como el panel A).
    ys2, xs2 = np.mgrid[0:H, 0:W]
    masa_ab = np.sqrt((xs2 - ax) ** 2 + (ys2 - ay) ** 2) / radio_ab
    dentro_ab = np.clip(1.0 - masa_ab, 0, 1)
    for c, v in [(0, 178), (1, 194), (2, 198)]:
        img[:, :, c] = img[:, :, c] * (1 - dentro_ab * 0.60) + v * (dentro_ab * 0.60)
    core_ab = np.clip(1.0 - masa_ab / 0.6, 0, 1)
    for c, v in [(0, 74), (1, 56), (2, 92)]:
        img[:, :, c] = img[:, :, c] * (1 - core_ab * 0.50) + v * (core_ab * 0.50)
    add_color(img, ax, ay, radio_ab * 0.5, PALIDO, 0.12)

    # EL OJO MAYOR — alfa: zócalo→esclerótica→pupila (mirando al vuelo).
    dirV = (0.94, -0.34)
    alpha_blend_disc(img, ax, ay - 6, 30 * 1.3, (10, 5, 16), 0.60)
    alpha_blend_disc(img, ax, ay - 6, 30 * 0.95, (245, 250, 250), 0.85)
    pupila = (ax + dirV[0] * 12.6, ay - 6 + dirV[1] * 12.6)
    alpha_blend_disc(img, pupila[0], pupila[1], 30 * 0.475, (10, 5, 16), 0.95)
    add_color(img, ax + dirV[0] * 28, ay - 6 + dirV[1] * 28, 8, CARMIN, 0.85)
    add_color(img, ax - 19, ay - 25, 5, OJO, 0.85)

    # LA CORONA de ojos menores (6) — con pupilas negras.
    for g in range(6):
        a = g / 6 * 2 * np.pi + 0.3
        ox = ax + np.cos(a) * radio_ab * 0.62
        oy = ay + np.sin(a) * radio_ab * 0.5 - 6
        alpha_blend_disc(img, ox, oy, 5 * 2.5, (10, 5, 16), 0.55)
        alpha_blend_disc(img, ox, oy, 5 * 1.7, (245, 250, 250), 0.80)
        alpha_blend_disc(img, ox + 1.5, oy + 1.0, 5 * 0.72, (10, 5, 16), 0.92)
        add_color(img, ox - 2.5, oy - 2, 2.2, CARMIN, 0.72)

    # LOS ESPÍRITUS (12 ahora, más apretados).
    for i in range(12):
        ang = i * 0.78 + 1.2
        fuera = 0.5 + 0.4 * np.sin(i * 1.7)
        r_esp = radio_ab * (0.55 + 0.4 * ((i * 0.37) % 1)) + 62 * fuera
        px = ax + np.cos(ang) * r_esp
        py = ay + np.sin(ang) * r_esp * 0.82 - 8
        add_color(img, px, py, 5, PALIDO, 0.20)
        add_color(img, px, py, 2.0, OJO, 0.5)

    return img


# =====================================================================
if __name__ == '__main__':
    for nombre, fn in [('mock_rencor_v629.png', mock_rencor),
                       ('mock_eminencia_v629.png', mock_eminencia)]:
        img = np.clip(fn(), 0, 255).astype(np.uint8)
        Image.fromarray(img).save('/tmp/' + nombre)
        # Anotaciones.
        pil = Image.open('/tmp/' + nombre).convert('RGB')
        d = ImageDraw.Draw(pil)
        d.text((10, 8), nombre.replace('_v629.png', ''), fill=(180, 180, 200))
        pil.save('/tmp/' + nombre)
        print('✓', nombre)
