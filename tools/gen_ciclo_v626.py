#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_ciclo_v626.py — LOS PNGs DEL BASTÓN DEL CICLO ESTELAR (v6.26).

Petición del usuario: "crea un baston que simule el ciclo de vida completo
de una estrella que se convierte en super nova y luego lo que siga en su
ciclo de vida" → EL BASTÓN DEL CICLO ESTELAR (Task 43-d).

Genera:
  · CicloEstelarStaff.png (30×30) — EL ICONO-SECUENCIA: el mástil vertical
    recorre la VIDA COMPLETA de arriba a abajo: la NEBULOSA violeta que se
    contrae → el SOL dorado de la secuencia principal → la GIGANTE ROJA
    hinchada → la NOVA blanca estallando → la ENANA de neutrones azul
    chispeante al pie. Cinco actos, un bastón.
  · CicloEstelarProjectile.png (76×76) — la sombra del estilo de la casa
    (patrón CrimsonBlackHoleProjectile.png): disco con rim cuyo color da
    la VUELTA AL CICLO alrededor de la circunferencia (violeta → dorado →
    rojo → blanco → azul — los cinco actos como gradiente angular).
"""
import os
import numpy as np
from PIL import Image

PROJ_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                        '..', 'AethonMod')
WPN_OUT = os.path.join(PROJ_DIR, 'Content', 'Weapons', 'Cosmic')
PRJ_OUT = os.path.join(PROJ_DIR, 'Content', 'Projectiles', 'Cosmic')

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
#  1. EL ICONO DE ARMA (30x30) — LA SECUENCIA VERTICAL DEL CICLO
# =====================================================================

def icon_cicloestelar():
    """EL BASTÓN DEL CICLO ESTELAR: el mástil ES la línea temporal de la
    estrella — de arriba (el nacimiento) a abajo (el cadáver): nebulosa
    violeta contrayEndose → sol dorado → gigante roja → nova blanca →
    enana azul de neutrones. Cinco actos viviendo en un solo icono."""
    W, H = 30, 30
    img = new_canvas(W, H)
    cx = W * SS * 0.5

    VIOLET = (165, 125, 255)
    VIOLET_OSC = (110, 80, 190)
    AZUL = (120, 160, 255)
    ORO = (255, 195, 85)
    AMBAR = (255, 140, 60)
    ROJO = (255, 90, 45)
    ROJO_OSC = (170, 35, 22)
    BLANCO = (255, 252, 240)
    BLANCO_FRI = (225, 240, 255)
    CIAN = (140, 225, 250)

    # EL MASTIL: madera de nebulosa, del nacimiento al cadáver.
    staff_handle(img, cx, H * SS * 0.06, H * SS * 0.97, (105, 88, 138), 0.95)

    # --- ACTO I · LA NEBULOSA (arriba): racimo violeta contrayEndose
    #     hacia su proto-núcleo + motas de polvo cayendo al centro. ---
    ny = H * SS * 0.16
    neb = [(-3.6, -0.9), (3.4, -1.4), (-2.2, 1.6), (2.6, 1.2), (0.4, -2.4), (-0.6, 2.6)]
    for k, (dx, dy) in enumerate(neb):
        r = 2.0 + 1.1 * hash01(21, k, 3)
        c = VIOLET if k % 2 else VIOLET_OSC
        glow_dot(img, cx + dx * SS, ny + dy * SS, r, c, 0.42 + 0.18 * hash01(21, k, 7))
    # el proto-núcleo tenue latiendo al centro de la nube.
    glow_dot(img, cx, ny, 1.6, BLANCO_FRI, 0.5)
    glow_dot(img, cx, ny, 0.7, BLANCO, 0.7)
    # dos motas cayendo en espiral hacia el proto-núcleo.
    for (sx, sy) in [(-4.8, 0.6), (4.6, 1.8)]:
        capsule(img, cx + sx * SS, ny + sy * SS, cx + sx * 0.35 * SS,
                ny + sy * 0.35 * SS, 0.45, AZUL, 0.55)

    # --- ACTO II · LA SECUENCIA PRINCIPAL: el sol dorado VIVO con su
    #     disco, un anillo rúnico tenue y el destello de la ignición. ---
    sy2 = H * SS * 0.40
    glow_dot(img, cx, sy2, 5.4, ORO, 0.40)          # el resplandor
    glow_dot(img, cx, sy2, 3.1, AMBAR, 0.55)
    glow_dot(img, cx, sy2, 2.0, ORO, 0.95)          # el disco solar
    glow_dot(img, cx, sy2, 0.8, BLANCO, 1.0)        # el corazón
    # el anillo rúnico tenue (elipse inclinada — el tier 3 del render).
    ring_stroke(img, cx, sy2, 4.4 * SS, 1.6 * SS, -0.42, 0.45, ORO, 0.55, 32)
    # el destello de 4 puntas de la IGNICIÓN.
    capsule(img, cx - 6.6 * SS, sy2, cx + 6.6 * SS, sy2, 0.55, BLANCO, 0.7)
    capsule(img, cx, sy2 - 6.6 * SS, cx, sy2 + 6.6 * SS, 0.55, BLANCO, 0.7)

    # --- ACTO III · LA GIGANTE ROJA: la bola hinchada y ENROJECIDA con
    #     sus celdas de convección y una capa desprendiEndose. ---
    gy = H * SS * 0.63
    glow_dot(img, cx, gy, 7.0, ROJO_OSC, 0.40)      # la atmósfera extendida
    glow_dot(img, cx, gy, 5.2, ROJO, 0.55)
    glow_dot(img, cx, gy, 3.6, ROJO, 0.95)          # el cuerpo frío
    # las celdas de convección (blobs voraces).
    cells = [(-2.1, -1.1, 1.1), (1.8, -1.4, 0.9), (0.1, 0.9, 1.2),
             (-2.4, 0.9, 0.8), (2.3, 0.8, 0.9), (-0.4, -2.2, 0.8)]
    for k, (bx, by, r) in enumerate(cells):
        c = ORO if k % 3 == 0 else (AMBAR if k % 3 == 1 else ROJO)
        glow_dot(img, cx + bx * SS, gy + by * SS, r, c, 0.55)
    # la capa desprendida (nebulosa planetaria): un puff alejAndose.
    ring_stroke(img, cx + 1.6 * SS, gy - 0.8 * SS, 6.8 * SS, 5.2 * SS,
                0.0, 0.4, VIOLET, 0.30, 30)

    # --- ACTO IV · LA SUPERNOVA: el estallido blanco con sus rayas
    #     fugaras cromáticas y el par de frentes de choque. ---
    vy = H * SS * 0.82
    for i in range(12):                              # las llamaradas radiales
        ang = i / 12 * 2 * np.pi + 0.13
        ln = 5.2 + 1.8 * hash01(43, i, 5)
        c = BLANCO if i % 3 else (255, 170, 90)
        ax = cx + np.cos(ang) * 1.4 * SS
        ay = vy + np.sin(ang) * 1.4 * SS
        bx = cx + np.cos(ang) * ln * SS
        by = vy + np.sin(ang) * ln * SS
        capsule(img, ax, ay, bx, by, 0.6, c, 0.75)
    glow_dot(img, cx, vy, 2.4, BLANCO, 1.0)          # el corazón de la nova
    glow_dot(img, cx, vy, 4.0, AMBAR, 0.45)
    ring_stroke(img, cx, vy, 4.2 * SS, 4.2 * SS, 0, 0.4, (255, 120, 60), 0.5, 28)
    # el núcleo que queda: la semilla azul de la enana (ya se ve).
    glow_dot(img, cx, vy, 0.55, CIAN, 0.95)

    # --- ACTO V · EL REMANENTE (abajo): la ESTRELLA DE NEUTRONES enana
    #     blanco-azul con sus líneas de campo dipolares y el haz pulsar. ---
    ry = H * SS * 0.95
    # los bucles dipolares r = L·sen²θ (la firma de la casa).
    for (L, c, a) in [(3.6, AZUL, 0.5), (2.6, CIAN, 0.6)]:
        pts = []
        segs = 40
        for i in range(segs + 1):
            t = i / segs * 2 * np.pi
            r = max(L * np.sin(t) ** 2, 0.02)
            pts.append((cx + r * np.cos(t) * SS, ry - r * np.sin(t) * SS))
        for i in range(segs):
            capsule(img, pts[i][0], pts[i][1], pts[i + 1][0], pts[i + 1][1],
                    0.42, c, a)
    # el haz del púlsar barriendo (dos chispas diagonales).
    capsule(img, cx - 2.0 * SS, ry - 1.4 * SS, cx - 4.6 * SS, ry - 3.0 * SS,
            0.5, CIAN, 0.6)
    capsule(img, cx + 2.0 * SS, ry - 1.4 * SS, cx + 4.6 * SS, ry - 3.0 * SS,
            0.5, CIAN, 0.6)
    # el núcleo ultradenso.
    glow_dot(img, cx, ry - 0.8 * SS, 1.5, AZUL, 0.6)
    glow_dot(img, cx, ry - 0.8 * SS, 0.75, BLANCO_FRI, 1.0)

    save(img, W, H, os.path.join(WPN_OUT, 'CicloEstelarStaff.png'))


# =====================================================================
#  2. LA SOMBRA DE PROYECTIL (76x76) — EL RIM QUE DA LA VUELTA AL CICLO
# =====================================================================

def shadow_cicloestelar():
    """La sombra del estilo de la casa (disco + rim) con la FIRMA del
    ciclo: el color del rim recorre los CINCO ACTOS alrededor de la
    circunferencia (violeta → dorado → rojo → blanco → azul) — el
    proyectil ES su propia biografía."""
    S = 76
    img = new_canvas(S, S)
    cx, cy = S * SS * 0.5, S * SS * 0.5
    R = 26.0 * SS          # núcleo estándar de la casa
    rim_width = 7.0

    yy, xx = np.mgrid[0:S * SS, 0:S * SS]
    d = np.sqrt((xx - cx) ** 2 + (yy - cy) ** 2) / R   # en unidades de radio
    ang = np.arctan2(yy - cy, xx - cx) + np.pi         # 0..2π

    # EL CICLO DE COLORES alrededor del rim (5 actos en gradiente angular).
    def act_color(a):
        """a: 0..1 la vuelta al ciclo → el color de ese acto."""
        stops = [
            (0.00, (165, 125, 255)),   # I   · NEBULOSA (violeta frío)
            (0.22, (255, 195, 85)),    # II  · SECUENCIA PRINCIPAL (dorado)
            (0.44, (255, 95, 60)),     # III · GIGANTE ROJA (rojo)
            (0.62, (255, 250, 240)),   # IV  · SUPERNOVA (blanco)
            (0.80, (150, 200, 255)),   # V   · REMANENTE (azul enana)
            (1.00, (165, 125, 255)),   # ...y vuelta a nacer.
        ]
        for k in range(len(stops) - 1):
            if stops[k][0] <= a <= stops[k + 1][0]:
                f = (a - stops[k][0]) / (stops[k + 1][0] - stops[k][0])
                c0, c1 = stops[k][1], stops[k + 1][1]
                return tuple(c0[i] + (c1[i] - c0[i]) * f for i in range(3))
        return stops[0][1]

    # Muestreamos el gradiente angular en 64 sectores (suave al bajar SS).
    n_sec = 64
    sectors = np.clip(((ang / (2 * np.pi)) * n_sec).astype(int), 0, n_sec - 1)
    sec_cols = np.array([act_color(i / n_sec) for i in range(n_sec)],
                        dtype=np.float32)

    # EL DISCO: cuerpo tenue neutro (la masa de la estrella por nacer).
    body_a = np.clip(1.35 - d, 0, 1) * 0.9
    # EL RIM: banda con el color del acto en cada sector angular.
    rim = np.exp(-((d - 0.86) ** 2) / (2 * (rim_width / R * SS) ** 2))
    # El rim PULSA alrededor del ciclo: cada sector respira con su acto.
    pulse = 0.82 + 0.18 * np.sin(ang * 5.0)

    col = np.zeros((S * SS, S * SS, 4), dtype=np.float32)
    rim_rgb = sec_cols[sectors] * (rim * pulse)[..., None]
    base_rgb = sec_cols[sectors] * 0.10 + 6.0
    for c in range(3):
        col[..., c] = rim_rgb[..., c] + base_rgb[..., c] * body_a
    a = np.maximum(body_a * 0.55, rim * 0.85 * pulse)
    col[..., 3] = a * 255

    # El rim EXTERIOR desvanecido (el color del acto siguiente más allá).
    rim2 = np.exp(-((d - 1.06) ** 2) / (2 * (rim_width / R * SS * 1.4) ** 2))
    rim2_rgb = sec_cols[sectors] * (rim2 * 0.55)[..., None]
    for c in range(3):
        col[..., c] += rim2_rgb[..., c]
    col[..., 3] = np.maximum(col[..., 3], rim2 * 0.5 * 255)

    # EL CORAZÓN CICLANTE: un núcleo pequeño blanco-azulado (la semilla
    # de la estrella que va a nacer — el mismo punto que queda al morir).
    heart = np.exp(-(d * d) / (2 * 0.16 ** 2))
    for c, v in enumerate((235, 242, 255)):
        col[..., c] = np.maximum(col[..., c], v * heart)
    col[..., 3] = np.maximum(col[..., 3], heart * 0.9 * 255)

    out = downsample(col, S, S)
    out[..., 3] = np.where(out[..., 3] < 6, 0, out[..., 3])
    path = os.path.join(PRJ_OUT, 'CicloEstelarProjectile.png')
    Image.fromarray(out, 'RGBA').save(path)
    print(f"  -> CicloEstelarProjectile.png (76x76) "
          f"{int((out[..., 3] > 8).sum())} px visibles")


# =====================================================================

if __name__ == '__main__':
    print("GEN v6.26 — EL BASTÓN DEL CICLO ESTELAR:")
    icon_cicloestelar()
    shadow_cicloestelar()
    print("OK — 2 PNGs generados.")
