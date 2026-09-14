#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_rift_v626.py — LOS PNGs DEL DESGARRO EN LA REALIDAD (reproducible).

Genera (Task 43-b — RiftLib + Bastón del Desgarro):
  · DesgarroRealityStaff.png (30×30) — EL ICONO: un desgarro vertical violeta
    con labios brillantes (violeta/carmesí) y estrellitas dentro del vacío,
    con el mástil del bastón debajo (la casa pinta iconos con glow_dot/capsule).
  · RealityTearProjectile.png (76×76) — LA SOMBRA de disco del estilo de la
    casa (patrón CrimsonBlackHoleProjectile.png de gen_v624_assets.py) con la
    identidad del desgarro: rim violeta + rim exterior cian + la CICATRIZ
    vertical brillante en el centro.
"""
import os
import numpy as np
from PIL import Image

PROJ_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                        '..', 'AethonMod')
WPN_OUT = os.path.join(PROJ_DIR, 'Content', 'Weapons', 'Cosmic')
PRJ_OUT = os.path.join(PROJ_DIR, 'Content', 'Projectiles', 'Cosmic')

SS = 8  # supersampling

# LA PALETA DEL ARMA (la del proyectil: velo violeta / cuerpo carmesí / núcleo).
VIOLET = (150, 80, 255)
CRIMSON = (255, 60, 130)
CYAN = (80, 200, 255)
WHITE = (243, 240, 255)
WOOD = (150, 108, 182)


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


def dark_dot(img, px, py, radius, alpha):
    """Un punto OSCURO sOlido (el pincel del VACIO: el BlackDisk de la casa)."""
    H, W = img.shape[0], img.shape[1]
    if radius <= 0.1 or alpha <= 0.01:
        return
    x0, x1 = max(0, int(px - radius * 2.5)), min(W, int(px + radius * 2.5) + 1)
    y0, y1 = max(0, int(py - radius * 2.5)), min(H, int(py + radius * 2.5) + 1)
    if x0 >= x1 or y0 >= y1:
        return
    yy, xx = np.mgrid[y0:y1, x0:x1]
    d = np.sqrt((xx - px) ** 2 + (yy - py) ** 2) / (radius * SS)
    # Núcleo sólido + caída dura (el BlackDisk es un corte, no una bruma).
    a = np.clip(1.35 - d, 0, 1) * alpha
    m = a > 0.02
    for c in range(3):
        img[y0:y1, x0:x1, c] = np.where(m, 8.0, img[y0:y1, x0:x1, c])
    img[y0:y1, x0:x1, 3] = np.maximum(img[y0:y1, x0:x1, 3], a * 255)


def capsule(img, ax, ay, bx, by, width, color, alpha):
    """Una cApsula de luz (segmento con brillo gaussiano)."""
    ln = max(np.hypot(bx - ax, by - ay), 1e-6)
    steps = max(2, int(ln / (width * SS * 0.35)) + 1)
    for t in np.linspace(0, 1, steps):
        glow_dot(img, ax + (bx - ax) * t, ay + (by - ay) * t, width, color, alpha)


def dark_capsule(img, ax, ay, bx, by, width, alpha):
    """Una cApsula OSCURA (un tramo del vacIo del desgarro)."""
    ln = max(np.hypot(bx - ax, by - ay), 1e-6)
    steps = max(2, int(ln / (width * SS * 0.30)) + 1)
    for t in np.linspace(0, 1, steps):
        dark_dot(img, ax + (bx - ax) * t, ay + (by - ay) * t, width, alpha)


def save(img, w, h, path):
    out = downsample(img, w, h)
    # Umbral suave: nada de pIxels fantasma en las esquinas.
    out[..., 3] = np.where(out[..., 3] < 6, 0, out[..., 3])
    Image.fromarray(out, 'RGBA').save(path)
    print(f"  -> {os.path.basename(path)} ({w}x{h}) "
          f"{int((out[..., 3] > 8).sum())} px visibles")


def lens_width(t, wmax):
    """LA CURVA "LENS" del contrato RiftLib: sin(t·pi)^0.6."""
    return wmax * np.power(np.sin(np.clip(t, 0, 1) * np.pi), 0.6)


# =====================================================================
#  1. EL ICONO DEL BASTÓN DEL DESGARRO (30x30)
# =====================================================================

def icon_desgarro():
    """EL BASTÓN DEL DESGARRO EN LA REALIDAD: mástil + la herida vertical.

    La geometría del icono replica el contrato de RiftLib a escala 30 px:
    el VACÍO (banda negra, curva lens sin(t·π)^0.6) con los LABIOS de luz
    (violeta + razor blanco) JUSTO FUERA del borde del vacío, estrellitas
    dentro y la estrella de 4 puntas del punto de ruptura.
    """
    W, H = 30, 30
    img = new_canvas(W, H)
    cx = W * SS * 0.5
    top, bottom = 1.8 * SS, 21.0 * SS      # el largo del desgarro
    cy = (top + bottom) * 0.5

    # La MEDIA-ANCHURA del vacío (en px reales): 0.31·w con w≈11 px → 3.4 px.
    def void_half(t):
        return 3.4 * lens_width(t, 1.0)     # px

    steps = 26

    # === 1. EL RESPLANDOR de la herida (el velo del contrato) + LOS LABIOS
    #         (se pintan ANTES del vacío: el negro CORTA la luz — el orden
    #         importa porque los pinceles componen por máximo de RGB) ===
    glow_dot(img, cx, cy, 9.5, VIOLET, 0.30)
    glow_dot(img, cx, cy, 5.6, CRIMSON, 0.24)

    for i in range(steps):
        t = i / (steps - 1)
        edge = void_half(t) + 1.0           # px fuera del borde del vacío
        y0 = top + (bottom - top) * max(t - 0.5 / steps, 0)
        y1 = top + (bottom - top) * min(t + 0.5 / steps, 1)
        # El labio VIOLETA (el velo exterior)...
        capsule(img, cx - edge * SS, y0, cx - edge * SS, y1, 1.0, VIOLET, 0.9)
        capsule(img, cx + edge * SS, y0, cx + edge * SS, y1, 1.0, VIOLET, 0.9)
        # ...y el NÚCLEO blanco razor pegado al borde (la doble lámina).
        capsule(img, cx - (edge - 0.65) * SS, y0, cx - (edge - 0.65) * SS, y1, 0.4, WHITE, 0.92)
        capsule(img, cx + (edge - 0.65) * SS, y0, cx + (edge - 0.65) * SS, y1, 0.4, WHITE, 0.92)

    # === 2. EL VACÍO: la banda oscura con la curva LENS (gorda al centro,
    #         aguja en las puntas — exactamente la de RiftLib.TearVacio).
    #         DESPUÉS de la luz: el vacío corta el resplandor y queda negro
    #         de verdad (en el runtime el orden lo da el lote no-premult). ===
    for i in range(steps):
        t = i / (steps - 1)
        wv = void_half(t)
        if wv < 0.22:
            continue
        y = top + (bottom - top) * t
        dark_dot(img, cx, y, wv, 0.95)

    # === 4. EL ACENTO CARMESÍ: los picos de la herida (dónde se rompió) —
    capsule(img, cx - 1.6 * SS, top + 1.4 * SS, cx + 1.6 * SS, top + 1.4 * SS, 0.9, CRIMSON, 0.85)
    capsule(img, cx - 1.6 * SS, bottom - 1.4 * SS, cx + 1.6 * SS, bottom - 1.4 * SS, 0.9, CRIMSON, 0.85)

    # === 5. LAS ESTRELLITAS dentro del vacío (el interior de la librería):
    #         son LO que se ve en el negro — que arnan. ===
    star_pos = [(0.2, -0.5), (0.32, 0.35), (0.44, -0.4), (0.62, 0.3), (0.76, -0.35), (0.88, 0.1)]
    for k, (t, off) in enumerate(star_pos):
        y = top + (bottom - top) * t
        x = cx + off * void_half(t) * 0.75 * SS
        glow_dot(img, x, y, 0.8 + 0.3 * (k % 2), WHITE, 0.95)
        glow_dot(img, x, y, 0.4, WHITE, 1.0)

    # === 6. LA ESTRELLA DE 4 PUNTAS del punto de ruptura (RiftLib.Star):
    #         pequeña — el OJO de la herida, sin lavar el vacío que la rodea
    #         (la espiga vertical domina — la lección DoG ×8 vs ×5). ===
    sy = top + (bottom - top) * 0.5
    capsule(img, cx - 3.0 * SS, sy, cx + 3.0 * SS, sy, 0.55, WHITE, 0.85)
    capsule(img, cx, sy - 3.8 * SS, cx, sy + 3.8 * SS, 0.55, WHITE, 0.85)
    glow_dot(img, cx, sy, 1.3, VIOLET, 0.75)
    glow_dot(img, cx, sy, 0.7, WHITE, 0.95)

    # === 7. EL MÁSTIL del bastón (la herida florece del cetro) ===
    capsule(img, cx - 1.0 * SS, bottom - 1.5 * SS, cx - 1.0 * SS, H * SS * 0.97, 1.3, WOOD, 1.0)
    capsule(img, cx + 1.0 * SS, bottom - 1.5 * SS, cx + 1.0 * SS, H * SS * 0.97, 1.3, WOOD, 0.85)
    # El NUDO del mástil (donde se ancla el desgarro).
    glow_dot(img, cx, (bottom + H * SS * 0.97) * 0.5, 2.0, VIOLET, 0.5)
    glow_dot(img, cx, H * SS * 0.955, 1.8, VIOLET, 0.7)
    glow_dot(img, cx, H * SS * 0.955, 0.9, WHITE, 0.8)

    save(img, W, H, os.path.join(WPN_OUT, 'DesgarroRealityStaff.png'))


# =====================================================================
#  2. LA SOMBRA DEL PROYECTIL DESGARRO (76x76) — patron de la casa
# =====================================================================

def shadow_desgarro():
    """EL DISCO CON RIM (patron CrimsonBlackHoleProjectile) + la cicatriz
    vertical brillante: la identidad del desgarro en la sombra de la casa."""
    S = 76
    img = new_canvas(S, S)
    cx, cy = S * SS * 0.5, S * SS * 0.5
    R = 26.0 * SS

    yy, xx = np.mgrid[0:S * SS, 0:S * SS]
    d = np.sqrt((xx - cx) ** 2 + (yy - cy) ** 2) / R  # en unidades de radio

    # EL DISCO: solido oscuro (cuerpo) con alfa fuerte.
    body_a = np.clip(1.35 - d, 0, 1) * 0.9
    # EL RIM VIOLETA (el color de identidad nº1).
    rim = np.exp(-((d - 0.86) ** 2) / (2 * (6.0 / R * SS) ** 2))

    # === LA CICATRIZ se compone SOBRE el disco a resoluciOn SS (un solo
    #     downsample al final — sin mezclar escalas). ===
    img2 = new_canvas(S, S)
    img2[..., 0] = VIOLET[0] * rim + 16 * body_a
    img2[..., 1] = VIOLET[1] * rim + 10 * body_a
    img2[..., 2] = VIOLET[2] * rim + 26 * body_a
    a = np.maximum(body_a * 0.55, rim * 0.85)
    img2[..., 3] = a * 255

    # El rim EXTERIOR cian desvanecido (el color de identidad nº2).
    rim2 = np.exp(-((d - 1.06) ** 2) / (2 * (8.4 / R * SS) ** 2))
    img2[..., 0] += CYAN[0] * rim2 * 0.55
    img2[..., 1] += CYAN[1] * rim2 * 0.55
    img2[..., 2] += CYAN[2] * rim2 * 0.55
    img2[..., 3] = np.maximum(img2[..., 3], rim2 * 0.5 * 255)
    top, bottom = cy - 21.5 * SS, cy + 21.5 * SS
    wmax = 5.2 * SS

    # El velo de la herida (donde toca el disco).
    glow_dot(img2, cx, cy, 12.5, VIOLET, 0.22)

    # Los LABIOS con la curva lens (los del icono, a escala de sombra).
    steps = 26
    for i in range(steps):
        t = i / (steps - 1)
        wl = lens_width(t, wmax)
        edge = 0.31 * wl
        y0 = top + (bottom - top) * max(t - 0.5 / steps, 0)
        y1 = top + (bottom - top) * min(t + 0.5 / steps, 1)
        capsule(img2, cx - edge, y0, cx - edge, y1, max(wl * 0.40 / SS, 0.55), VIOLET, 0.7)
        capsule(img2, cx + edge, y0, cx + edge, y1, max(wl * 0.40 / SS, 0.55), VIOLET, 0.7)
        # El núcleo razor.
        capsule(img2, cx - edge, y0, cx - edge, y1, max(wl * 0.15 / SS, 0.3), WHITE, 0.8)
        capsule(img2, cx + edge, y0, cx + edge, y1, max(wl * 0.15 / SS, 0.3), WHITE, 0.8)

    # EL VACÍO central (la banda oscura — el interior del desgarro).
    for i in range(steps):
        t = i / (steps - 1)
        wv = lens_width(t, wmax * 0.62)
        if wv < 0.3 * SS:
            continue
        y = top + (bottom - top) * t
        dark_dot(img2, cx, y, wv / SS, 0.8)

    # Las estrellitas del interior.
    for k, (t, off) in enumerate([(0.2, -0.4), (0.34, 0.3), (0.5, -0.2),
                                   (0.66, 0.35), (0.8, -0.3)]):
        y = top + (bottom - top) * t
        wv = lens_width(t, wmax * 0.62)
        glow_dot(img2, cx + off * wv, y, 0.9 + 0.3 * (k % 2), WHITE, 0.85)

    # LA ESTRELLA de 4 puntas en el centro (el punto de ruptura).
    capsule(img2, cx - 6.5 * SS, cy, cx + 6.5 * SS, cy, 0.8, WHITE, 0.7)
    capsule(img2, cx, cy - 7.5 * SS, cx, cy + 7.5 * SS, 0.8, WHITE, 0.7)
    glow_dot(img2, cx, cy, 2.0, VIOLET, 0.5)
    glow_dot(img2, cx, cy, 1.0, WHITE, 0.95)

    path = os.path.join(PRJ_OUT, 'RealityTearProjectile.png')
    final = downsample(img2, S, S)
    final[..., 3] = np.where(final[..., 3] < 6, 0, final[..., 3])
    Image.fromarray(final, 'RGBA').save(path)
    print(f"  -> RealityTearProjectile.png (76x76) "
          f"{int((final[..., 3] > 8).sum())} px visibles")


# =====================================================================

if __name__ == '__main__':
    print("GEN v6.26 — el desgarro en la realidad:")
    icon_desgarro()
    shadow_desgarro()
    print("OK — 2 PNGs generados.")
