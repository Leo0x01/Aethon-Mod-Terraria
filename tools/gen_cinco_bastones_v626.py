#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_cinco_bastones_v626.py — LOS PNGs DE LOS CINCO BASTONES CREATIVOS (v6.26).

CINCO BASTONES NUEVOS con tipos de proyectiles CREATIVOS y originales
(petición del usuario: "luego crea mas bastones con nuevos tipos de
proyectiles creativos, al menos 5"). Cada icono lleva la IDENTIDAD de su
arma; cada sombra de proyectil el disco-76 con su firma.

Genera:
  · 5 ICONOS de arma (30×30):
      - Reloj de Arena Cósmico: el reloj dorado con la arena cayendo.
      - Marea Gravitatoria: la ola azul con cresta y espuma.
      - Enjambre Prismático: tres avispas multicolor con alas.
      - Péndulo del Juicio: ancla + hilo + maza de runas.
      - Coro Espectral: la nota dorada con su anillo sonoro.
  · 5 SOMBRAS de proyectil (76×76): la silueta de cada concepto.

Patrón PIL+numpy de la casa (gen_v624_assets.py / gen_real_stars_v626.py).
"""
import os
import numpy as np
from PIL import Image

PROJ_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                        '..', 'AethonMod')
WPN_OUT = os.path.join(PROJ_DIR, 'Content', 'Weapons', 'Cosmic')
PRJ_OUT = os.path.join(PROJ_DIR, 'Content', 'Projectiles', 'Cosmic')

SS = 8  # supersampling


# =====================================================================
#  LOS HELPERS DE LA CASA (copiados del gen_real_stars_v626.py)
# =====================================================================

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


def save_raw(col, S, path):
    out = downsample(col, S, S)
    out[..., 3] = np.where(out[..., 3] < 6, 0, out[..., 3])
    Image.fromarray(out, 'RGBA').save(path)
    print(f"  -> {os.path.basename(path)} ({S}x{S}) "
          f"{int((out[..., 3] > 8).sum())} px visibles")


# =====================================================================
#  1. EL RELOJ DE ARENA CÓSMICO — icono 30x30
# =====================================================================

def icon_relojarena():
    """EL RELOJ DE ARENA: dos triAngulos dorados (vidrio de luz) con la
    arena cayendo por el cuello y el montIculo creciendo abajo."""
    W, H = 30, 30
    img = new_canvas(W, H)
    cx = W * SS * 0.5
    cy = H * SS * 0.38

    ORO = (255, 214, 120)
    ARENA = (255, 236, 160)
    CALIENTE = (255, 250, 215)
    MASTIL = (150, 120, 70)

    staff_handle(img, cx, cy + 8.5 * SS, H * SS * 0.96, MASTIL, 0.95)

    top, bot = -8.0 * SS, 8.0 * SS      # las tapas
    halfw, neck = 7.0 * SS, 1.6 * SS    # ancho y cuello

    # EL MARCO: rieles del trapezoide (4 cApsulas) + tapas.
    for lado in (-1, 1):
        capsule(img, cx + lado * halfw, cy + top, cx + lado * neck, cy - 1.2 * SS,
                0.55, ORO, 0.75)
        capsule(img, cx + lado * neck, cy + 1.2 * SS, cx + lado * halfw, cy + bot,
                0.55, ORO, 0.75)
    capsule(img, cx - halfw, cy + top, cx + halfw, cy + top, 0.7, ORO, 0.8)
    capsule(img, cx - halfw, cy + bot, cx + halfw, cy + bot, 0.7, ORO, 0.8)
    capsule(img, cx - neck - 0.4 * SS, cy, cx + neck + 0.4 * SS, cy, 0.5, CALIENTE, 0.7)

    # LA ARENA ARRIBA (el cono superior medio vacIo).
    for k in range(7):
        f = k / 7.0
        y = cy - 1.8 * SS - f * 5.4 * SS
        x = cx + (hash01(7, k, 1) - 0.5) * (halfw * (1.0 - f) * 0.9)
        glow_dot(img, x, y, 0.7, ARENA, 0.75)

    # LA CORRIENTE (el hilo del cuello) + el montIculo abajo.
    capsule(img, cx, cy - 1.2 * SS, cx, cy + 3.2 * SS, 0.55, CALIENTE, 0.9)
    for k in range(6):
        f = (k % 3) / 3.0
        fila = k // 3
        y = cy + bot - 1.2 * SS - fila * 1.8 * SS
        x = cx + (f - 0.5) * (halfw * (1.0 - fila * 0.3) * 0.8)
        glow_dot(img, x, y, 0.7, ARENA, 0.85)

    # EL BRILLO del vidrio (el corazOn del reloj).
    glow_dot(img, cx, cy, 5.0, ORO, 0.30)
    glow_dot(img, cx, cy, 2.2, CALIENTE, 0.55)

    save(img, W, H, os.path.join(WPN_OUT, 'RelojArenaStaff.png'))


# =====================================================================
#  2. LA MAREA GRAVITATORIA — icono 30x30
# =====================================================================

def icon_marea():
    """LA MAREA: la ola azul de luz con su cresta brillante y la espuma
    (chispas blancas sobre el lomo) cabalgando a la derecha."""
    W, H = 30, 30
    img = new_canvas(W, H)
    cx = W * SS * 0.5
    base = H * SS * 0.80

    AGUA = (70, 160, 230)
    AGUA_P = (35, 100, 165)
    CRESTA = (170, 230, 255)
    ESPUMA = (240, 252, 255)

    staff_handle(img, cx, base - 6.5 * SS, H * SS * 0.96, (110, 130, 160), 0.95)

    # EL CUERPO DE AGUA (la masa profunda, achatada).
    for k in range(5):
        f = k / 4.0
        x = cx + (f - 0.5) * 13.0 * SS
        y = base - 4.2 * SS
        glow_dot(img, x, y, 4.6 - f, AGUA_P, 0.4)

    # EL LOMO (la curva de la cresta: baja atrAs, se alza al frente).
    pts = []
    for k in range(13):
        f = k / 12.0
        x = cx - 8.5 * SS + f * 17.0 * SS
        y = base - (2.2 + 8.2 * f * f) * SS
        pts.append((x, y))
    for i in range(len(pts) - 1):
        capsule(img, pts[i][0], pts[i][1], pts[i + 1][0], pts[i + 1][1],
                1.5, AGUA, 0.7)
        capsule(img, pts[i][0], pts[i][1], pts[i + 1][0], pts[i + 1][1],
                0.7, CRESTA, 0.85)

    # LA CUMBRE de la cresta (el punto mAas alto delante).
    glow_dot(img, pts[-1][0] - 1.0 * SS, pts[-1][1], 3.2, CRESTA, 0.8)
    glow_dot(img, pts[-1][0] - 1.0 * SS, pts[-1][1], 1.4, ESPUMA, 0.95)

    # LA ESPUMA (5 chispas nerviosas sobre el lomo).
    for k in range(5):
        f = 0.45 + 0.5 * (k / 4.0)
        idx = int(f * 12)
        x = pts[idx][0] + (hash01(9, k, 3) - 0.5) * 2.0 * SS
        y = pts[idx][1] - (0.8 + hash01(9, k, 7) * 1.6) * SS
        glow_dot(img, x, y, 0.55, ESPUMA, 0.85)

    save(img, W, H, os.path.join(WPN_OUT, 'MareaGravitatoriaStaff.png'))


# =====================================================================
#  3. EL ENJAMBRE PRISMÁTICO — icono 30x30
# =====================================================================

def hue_rgb(h, l=0.6):
    """HSL->RGB (el LumenLib.Hue de la casa, en Python)."""
    h = h % 1.0
    import colorsys
    r, g, b = colorsys.hls_to_rgb(h, l, 1.0)
    return (int(r * 255), int(g * 255), int(b * 255))


def icon_enjambre():
    """EL ENJAMBRE: tres avispas prismAticas (cuerpo + ala aleteando)
    de tres colores del arcoIris alrededor del corazOn del nUcleo."""
    W, H = 30, 30
    img = new_canvas(W, H)
    cx, cy = W * SS * 0.5, H * SS * 0.38

    MASTIL = (140, 110, 150)

    staff_handle(img, cx, cy + 7.0 * SS, H * SS * 0.96, MASTIL, 0.95)

    # EL NÚCLEO (el corazOn prismAtico del que nacen).
    glow_dot(img, cx, cy, 5.6, hue_rgb(0.90), 0.45)
    glow_dot(img, cx, cy, 2.4, (255, 250, 240), 0.8)

    # LAS TRES AVISPAS (cuerpo cApsula + ala + cabeza, colores distintos).
    avispas = [(-7.5, -3.5, 0.02), (7.0, -4.5, 0.55), (0.5, -9.0, 0.80)]
    for ax, ay, h in avispas:
        px, py = cx + ax * SS, cy + ay * SS
        c = hue_rgb(h)
        blanca = (255, 250, 240)
        # El ABDOMEN (orientado hacia el nUcleo).
        dx, dy = cx - px, cy - py
        ln = max(np.hypot(dx, dy), 1e-6)
        dx, dy = dx / ln, dy / ln
        capsule(img, px, py, px + dx * 3.2 * SS, py + dy * 3.2 * SS, 1.0, c, 0.85)
        # La CABEZA (blanca, delante).
        glow_dot(img, px - dx * 1.2 * SS, py - dy * 1.2 * SS, 0.85, blanca, 0.9)
        # EL ALA (un trazo diagonal semitransparente).
        capsule(img, px - dy * 1.5 * SS, py + dx * 1.5 * SS,
                px - dy * 3.6 * SS, py + dx * 3.6 * SS, 0.6, blanca, 0.4)
        # LA ESTELA corta (un punto mAas atrAs).
        glow_dot(img, px + dx * 4.6 * SS, py + dy * 4.6 * SS, 0.7, c, 0.35)

    save(img, W, H, os.path.join(WPN_OUT, 'EnjambrePrismaticoStaff.png'))


# =====================================================================
#  4. EL PÉNDULO DEL JUICIO — icono 30x30
# =====================================================================

def icon_pendulo():
    """EL PÉNDULO: el ancla arriba (aro + runa), el hilo dorado tensado
    y la MAZA de runas colgando abajo-ladeada (en plena sentencia)."""
    W, H = 30, 30
    img = new_canvas(W, H)
    ax = W * SS * 0.62          # el ancla arriba a la derecha
    ay = H * SS * 0.16
    mx = W * SS * 0.42          # la maza abajo a la izquierda (el arco)
    my = H * SS * 0.74

    ORO = (255, 205, 105)
    CALIENTE = (255, 240, 190)
    RUNA = (140, 85, 25)
    MASTIL = (150, 120, 70)

    staff_handle(img, W * SS * 0.22, H * SS * 0.30, H * SS * 0.96, MASTIL, 0.95)

    # EL ANCLA (aro + la runa de clavado).
    ring_stroke(img, ax, ay, 2.6 * SS, 2.6 * SS, 0.0, 0.5, ORO, 0.9, 24)
    capsule(img, ax - 1.4 * SS, ay - 1.4 * SS, ax + 1.4 * SS, ay + 1.4 * SS,
            0.4, CALIENTE, 0.7)

    # EL HILO (tensado, con vena blanca).
    capsule(img, ax, ay, mx, my, 0.8, ORO, 0.6)
    capsule(img, ax, ay, mx, my, 0.35, CALIENTE, 0.8)

    # LA MAZA (bola sOlida con runas grabadas + rim).
    glow_dot(img, mx, my, 6.4, ORO, 0.55)
    glow_dot(img, mx, my, 4.6, CALIENTE, 0.75)
    ring_stroke(img, mx, my, 5.4 * SS, 5.4 * SS, 0.0, 0.5, ORO, 0.75, 28)
    for k in range(6):
        t = k / 6.0 * 2 * np.pi
        rx, ry = mx + np.cos(t) * 2.6 * SS, my + np.sin(t) * 2.6 * SS
        glow_dot(img, rx, ry, 0.5, RUNA, 0.9)

    save(img, W, H, os.path.join(WPN_OUT, 'PenduloJuicioStaff.png'))


# =====================================================================
#  5. EL CORO ESPECTRAL — icono 30x30
# =====================================================================

def icon_coro():
    """EL CORO: la NOTA dorada (cabeza + mAstil + bandera) cantando su
    anillo sonoro, con dos notas compaNeras fantasmales detrAs."""
    W, H = 30, 30
    img = new_canvas(W, H)
    nx, ny = W * SS * 0.44, H * SS * 0.62    # la cabeza de la nota

    DORADA = (255, 214, 110)
    AMBAR = (255, 190, 92)
    BLANCO = (255, 250, 235)
    MASTIL = (150, 125, 80)

    staff_handle(img, W * SS * 0.82, H * SS * 0.42, H * SS * 0.96, MASTIL, 0.9)

    # EL ANILLO SONORO (el canto expandiEndose).
    ring_stroke(img, nx, ny, 10.5 * SS, 8.2 * SS, -0.25, 0.55, DORADA, 0.5, 40)
    ring_stroke(img, nx, ny, 6.8 * SS, 5.3 * SS, -0.25, 0.4, AMBAR, 0.65, 32)

    # LAS NOTAS COMPAÑERAS (fantasmales, mAs pequeNas, detrAs).
    for (ox, oy, s, a) in [(-6.5, -4.0, 0.6, 0.4), (5.0, -6.0, 0.55, 0.35)]:
        px, py = nx + ox * SS, ny + oy * SS
        glow_dot(img, px, py, 1.5 * s * 2, AMBAR, a)
        capsule(img, px + 0.5 * SS, py - 1.0 * SS, px + 0.7 * SS,
                py - 6.0 * s * SS, 0.45, AMBAR, a * 0.9)

    # LA NOTA PRINCIPAL: la CABEZA (sOlida) + el MASTIL + la BANDERA.
    glow_dot(img, nx, ny, 4.6, DORADA, 0.6)
    glow_dot(img, nx, ny, 2.6, BLANCO, 0.9)
    capsule(img, nx + 1.4 * SS, ny - 1.6 * SS, nx + 1.8 * SS, ny - 9.5 * SS,
            0.75, BLANCO, 0.85)
    # LA BANDERA (3 tramos ondulando a la derecha).
    fx, fy = nx + 1.8 * SS, ny - 9.5 * SS
    for k in range(3):
        nxp = fx + (2.6 - k * 0.5) * SS
        nyp = fy + (2.0 + np.sin(k * 1.9) * 0.8) * SS
        capsule(img, fx, fy, nxp, nyp, 0.55, DORADA, 0.8)
        fx, fy = nxp, nyp

    save(img, W, H, os.path.join(WPN_OUT, 'CoroEspectralStaff.png'))


# =====================================================================
#  LAS SOMBRAS DE PROYECTIL (76x76) — la silueta de cada concepto
# =====================================================================

def sombra_relojarena():
    """EL RELOJ: la silueta trapezoidal doble dorada con el cuello
    brillante (la corriente) y el montIculo abajo."""
    S = 76
    img = new_canvas(S, S)
    cx, cy = S * SS * 0.5, S * SS * 0.5

    ORO = (255, 214, 120)
    ARENA = (255, 236, 160)
    CALIENTE = (255, 250, 215)

    top, bot = -26.0 * SS, 26.0 * SS
    halfw, neck = 21.0 * SS, 3.0 * SS

    # EL VIDRIO: el interior dorado tenue (dos trapecios).
    for k in range(24):
        f = k / 24.0
        # el trapecio superior.
        y1 = cy + top + f * (bot * 0.5 - top - 2 * SS)
        w1 = halfw * (1 - f) + neck * f
        # el inferior (espejo).
        y2 = cy + bot - f * (bot * 0.5 - top - 2 * SS)
        w2 = w1
        for sgn in (-1, 1):
            glow_dot(img, cx + sgn * w1 * 0.5, y1, 2.4, ORO, 0.10 + 0.05 * (1 - f))
            glow_dot(img, cx + sgn * w2 * 0.5, y2, 2.4, ORO, 0.10 + 0.05 * f)

    # EL MARCO (los rieles + tapas, mAas densos).
    for lado in (-1, 1):
        capsule(img, cx + lado * halfw, cy + top, cx + lado * neck, cy - 2.0 * SS,
                1.3, ORO, 0.8)
        capsule(img, cx + lado * neck, cy + 2.0 * SS, cx + lado * halfw, cy + bot,
                1.3, ORO, 0.8)
    capsule(img, cx - halfw, cy + top, cx + halfw, cy + top, 1.6, ORO, 0.85)
    capsule(img, cx - halfw, cy + bot, cx + halfw, cy + bot, 1.6, ORO, 0.85)

    # EL CUELLO (la corriente brillante).
    capsule(img, cx, cy - 2.0 * SS, cx, cy + 2.0 * SS, 1.1, CALIENTE, 0.95)
    glow_dot(img, cx, cy, 3.0, CALIENTE, 0.8)

    # EL MONTÍCULO abajo (filas de arena) + el cono arriba.
    for k in range(18):
        fila = k // 6
        col = k % 6
        y = cy + bot - 2.5 * SS - fila * 3.2 * SS
        half = halfw * (1.0 - 0.14 * fila)
        x = cx + (col / 5.0 - 0.5) * half * 0.85
        glow_dot(img, x, y, 1.3, ARENA, 0.7)
    for k in range(8):
        f = k / 8.0
        y = cy - 3.5 * SS - f * 16.0 * SS
        x = cx + (hash01(13, k, 2) - 0.5) * halfw * (1.0 - f) * 0.8
        glow_dot(img, x, y, 1.3, ARENA, 0.6)

    save_raw(img, S, os.path.join(PRJ_OUT, 'RelojArenaProjectile.png'))


def sombra_marea():
    """LA MAREA: la media-luna azul del lomo con la cresta brillante y
    la espuma blanca (chispas nerviosas) + el cuerpo de agua abajo."""
    S = 76
    img = new_canvas(S, S)
    cx, cy = S * SS * 0.5, S * SS * 0.62

    AGUA = (70, 160, 230)
    AGUA_P = (40, 105, 170)
    CRESTA = (170, 230, 255)
    ESPUMA = (240, 252, 255)

    # EL CUERPO DE AGUA (la masa achatada abajo).
    for k in range(7):
        f = k / 6.0
        x = cx + (f - 0.5) * 52.0 * SS
        y = cy + 12.0 * SS
        glow_dot(img, x, y, 9.0 - 3.0 * abs(f - 0.5), AGUA_P, 0.35)

    # EL LOMO (la curva de la ola, gruesa).
    pts = []
    for k in range(21):
        f = k / 20.0
        x = cx - 30.0 * SS + f * 60.0 * SS
        y = cy - (6.0 + 26.0 * f * f) * SS
        pts.append((x, y))
    for i in range(len(pts) - 1):
        capsule(img, pts[i][0], pts[i][1], pts[i + 1][0], pts[i + 1][1],
                2.6, AGUA, 0.55)
        capsule(img, pts[i][0], pts[i][1], pts[i + 1][0], pts[i + 1][1],
                1.1, CRESTA, 0.75)

    # LA CUMBRE (el frente de la ola, mAas alto y brillante).
    glow_dot(img, pts[-1][0] - 3.0 * SS, pts[-1][1], 7.0, CRESTA, 0.7)
    glow_dot(img, pts[-1][0] - 3.0 * SS, pts[-1][1], 3.0, ESPUMA, 0.9)

    # LA ESPUMA (12 chispas sobre el lomo, con el hash de la casa).
    for k in range(12):
        f = 0.35 + 0.6 * (k / 11.0)
        idx = int(f * 20)
        x = pts[idx][0] + (hash01(21, k, 5) - 0.5) * 5.0 * SS
        y = pts[idx][1] - (1.5 + hash01(21, k, 9) * 4.0) * SS
        glow_dot(img, x, y, 1.0, ESPUMA, 0.8)

    # EL ROCÍO delante (gotas que saltan al frente).
    for k in range(4):
        f = k / 4.0
        x = cx + (30.0 + 6.0 * f) * SS
        y = cy - (14.0 + 10.0 * f * f) * SS
        glow_dot(img, x, y, 1.0, CRESTA, 0.6 - 0.1 * f)

    save_raw(img, S, os.path.join(PRJ_OUT, 'MareaGravitatoriaProjectile.png'))


def sombra_enjambre():
    """EL ENJAMBRE: doce avispas prismAticas (el arcoIris entero)
    orbitando el corazOn del nUcleo, cada una cuerpo + ala + estela."""
    S = 76
    img = new_canvas(S, S)
    cx, cy = S * SS * 0.5, S * SS * 0.5

    # EL NÚCLEO (el corazOn del que nacieron).
    glow_dot(img, cx, cy, 9.0, hue_rgb(0.92), 0.5)
    glow_dot(img, cx, cy, 4.0, (255, 250, 240), 0.85)

    # LAS 12 AVISPAS en anillo (cada color del arcoIris).
    n = 12
    for k in range(n):
        h = k / n
        c = hue_rgb(h)
        ang = k / n * 2 * np.pi + 0.35
        rr = (22.0 + 6.0 * np.sin(k * 2.4)) * SS
        px, py = cx + np.cos(ang) * rr, cy + np.sin(ang) * rr * 0.8
        # El CUERPO (cApsula radial apuntando al centro).
        dx, dy = cx - px, cy - py
        ln = max(np.hypot(dx, dy), 1e-6)
        dx, dy = dx / ln, dy / ln
        capsule(img, px, py, px + dx * 4.5 * SS, py + dy * 4.5 * SS, 1.4, c, 0.8)
        # La CABEZA blanca.
        glow_dot(img, px - dx * 1.6 * SS, py - dy * 1.6 * SS, 1.2,
                 (255, 250, 240), 0.9)
        # EL ALA (trazo diagonal).
        capsule(img, px - dy * 2.0 * SS, py + dx * 2.0 * SS,
                px - dy * 5.0 * SS, py + dx * 5.0 * SS, 0.8, (255, 250, 240), 0.4)
        # LA ESTELA (un punto mAas atrAs).
        glow_dot(img, px + dx * 6.5 * SS, py + dy * 6.5 * SS, 1.0, c, 0.4)

    save_raw(img, S, os.path.join(PRJ_OUT, 'EnjambrePrismaticoProjectile.png'))


def sombra_pendulo():
    """EL PÉNDULO: la MAZA de runas al frente (la bola grande con sus 8
    marcas grabadas y el rim) + el hilo dorado subiendo al ANCLA."""
    S = 76
    img = new_canvas(S, S)
    ax, ay = S * SS * 0.68, S * SS * 0.12    # el ancla
    mx, my = S * SS * 0.40, S * SS * 0.66    # la maza (el arco abierto)

    ORO = (255, 205, 105)
    CALIENTE = (255, 240, 190)
    RUNA = (135, 82, 22)

    # EL ANCLA (aro + runa de clavado).
    ring_stroke(img, ax, ay, 3.4 * SS, 3.4 * SS, 0.0, 0.6, ORO, 0.9, 28)
    capsule(img, ax - 1.8 * SS, ay - 1.8 * SS, ax + 1.8 * SS, ay + 1.8 * SS,
            0.5, CALIENTE, 0.7)

    # EL HILO (vena blanca sobre cuerpo dorado).
    capsule(img, ax, ay, mx, my, 1.2, ORO, 0.55)
    capsule(img, ax, ay, mx, my, 0.5, CALIENTE, 0.8)

    # EL ARCO FANTASMA (el rastro del barrido — 3 ecos hacia atrAs).
    dx, dy = ax - mx, ay - my
    ln = max(np.hypot(dx, dy), 1e-6)
    tx, ty = -dy / ln, dx / ln          # la tangente del vaivEn
    for g in (1, 2, 3):
        gx, gy = mx - tx * 6.0 * g * SS, my - ty * 6.0 * g * SS
        glow_dot(img, gx, gy, 6.0, ORO, 0.10 * (4 - g))

    # LA MAZA (bola sOlida con rim + runas).
    glow_dot(img, mx, my, 12.5, ORO, 0.6)
    glow_dot(img, mx, my, 9.5, CALIENTE, 0.8)
    ring_stroke(img, mx, my, 10.6 * SS, 10.6 * SS, 0.0, 0.6, ORO, 0.8, 36)
    for k in range(8):
        t = k / 8.0 * 2 * np.pi + 0.4
        rx, ry = mx + np.cos(t) * 5.2 * SS, my + np.sin(t) * 5.2 * SS
        glow_dot(img, rx, ry, 0.8, RUNA, 0.95)

    save_raw(img, S, os.path.join(PRJ_OUT, 'PenduloJuicioProjectile.png'))


def sombra_coro():
    """EL CORO: la NOTA dorada al centro (cabeza + mAstil + bandera)
    cantando DOS anillos concEntricos (el canto y su eco) + las 6 notas
    compaNeras fantasmales en Orbita."""
    S = 76
    img = new_canvas(S, S)
    cx, cy = S * SS * 0.5, S * SS * 0.58
    nx, ny = cx, cy                     # la nota principal (al centro)

    DORADA = (255, 214, 110)
    AMBAR = (255, 190, 92)
    BRONCE = (225, 165, 96)
    BLANCO = (255, 250, 235)

    # LOS ANILLOS DEL CANTO (el principal + su ECO interior).
    ring_stroke(img, nx, ny, 32.0 * SS, 26.0 * SS, -0.18, 0.9, DORADA, 0.6, 48)
    ring_stroke(img, nx, ny, 21.5 * SS, 17.5 * SS, -0.18, 0.7, AMBAR, 0.45, 40)

    # LAS 6 NOTAS COMPAÑERAS (en Orbita, colores de la escala).
    escala = [DORADA, AMBAR, BRONCE, (198, 140, 82), (172, 112, 66), (150, 96, 55)]
    for k in range(6):
        ang = k / 6.0 * 2 * np.pi + 0.5
        rr = 27.0 * SS
        px, py = nx + np.cos(ang) * rr, ny + np.sin(ang) * rr * 0.72
        c = escala[k]
        glow_dot(img, px, py, 2.6, c, 0.7)
        capsule(img, px + 0.8 * SS, py - 1.2 * SS, px + 1.1 * SS,
                py - 6.5 * SS, 0.6, c, 0.6)

    # LA NOTA PRINCIPAL: CABEZA + MASTIL + BANDERA (mAs grande).
    glow_dot(img, nx, ny, 7.0, DORADA, 0.7)
    glow_dot(img, nx, ny, 4.0, BLANCO, 0.95)
    capsule(img, nx + 2.2 * SS, ny - 2.4 * SS, nx + 2.8 * SS, ny - 15.5 * SS,
            1.1, BLANCO, 0.9)
    fx, fy = nx + 2.8 * SS, ny - 15.5 * SS
    for k in range(4):
        nxp = fx + (4.0 - k * 0.7) * SS
        nyp = fy + (3.0 + np.sin(k * 1.8) * 1.1) * SS
        capsule(img, fx, fy, nxp, nyp, 0.8, DORADA, 0.85)
        fx, fy = nxp, nyp

    save_raw(img, S, os.path.join(PRJ_OUT, 'CoroEspectralProjectile.png'))


# =====================================================================
#  EL MAIN
# =====================================================================

if __name__ == '__main__':
    print("gen_cinco_bastones_v626.py — LOS CINCO BASTONES CREATIVOS (v6.26)")
    print("  ICONOS (30x30):")
    icon_relojarena()
    icon_marea()
    icon_enjambre()
    icon_pendulo()
    icon_coro()
    print("  SOMBRAS DE PROYECTIL (76x76):")
    sombra_relojarena()
    sombra_marea()
    sombra_enjambre()
    sombra_pendulo()
    sombra_coro()
    print("LISTO: 10 PNGs.")
