#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_real_stars_v626.py — LOS PNGs DE LAS ESTRELLAS REALES (v6.26).

SEIS BASTONES NUEVOS basados en CLASES de estrellas reales (petición del
usuario: "crea nuevas variantes de soles basado en estrellas reales, como
estrellas de neutrones, pulsares, enanas blancas, estrellas muertas, entre
otras variantes"). NO son copias del sol: cada icono lleva la IDENTIDAD
astronómica de su estrella.

Genera:
  · 6 ICONOS de arma (30×30): Estrella de Neutrones (núcleo minúsculo
    cegador + líneas de campo dipolares), Púlsar (núcleo + dos haces
    opuestos de faro), Enana Blanca (estrella blanco-azul con facetas
    hexagonales), Estrella Muerta (disco negro con brasas frías),
    Supergigante Roja (bola enorme roja con celdas de convección),
    Magnetar (estrella violeta con líneas retorcidas + descarga).
  · 6 SOMBRAS de proyectil (76×76): el disco con rim del estilo de la
    casa (patrón CrimsonBlackHoleProjectile.png) con la identidad de
    cada estrella (tamaño del núcleo y color del rim).
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


def dipole_loop(img, cx, cy, L, rot, width, color, alpha, segs=44):
    """LA LINEA DE CAMPO DIPOLAR de la casa: r = L*sin(t)^2
    (el bucle cerrado de polo a polo — la firma de la estrella
    de neutrones y del magnetar)."""
    pts = []
    for i in range(segs + 1):
        t = i / segs * 2 * np.pi
        r = L * np.sin(t) ** 2
        if r < 0.02:
            # pegado al polo: empuja un mInimo para cerrar el bucle.
            r = 0.02
        x, y = r * np.cos(t), r * np.sin(t)
        c, s = np.cos(rot), np.sin(rot)
        pts.append((cx + x * c - y * s, cy + x * s + y * c))
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
#  1. ICONOS DE ARMA (30x30) — LA IDENTIDAD DE CADA ESTRELLA
# =====================================================================

def icon_neutronstar():
    """EL BASTON DE LA ESTRELLA DE NEUTRONES: nucleo MINUSCULO
    blanco-azul cegador + 3 lineas de campo dipolares curvAndose."""
    W, H = 30, 30
    img = new_canvas(W, H)
    cx, cy = W * SS * 0.5, H * SS * 0.40

    WHITE = (240, 246, 255)
    AZUL = (150, 190, 255)
    CIAN = (110, 230, 235)
    VIOLET = (140, 120, 220)

    # El mastil (denso: acero de estrella muerta).
    staff_handle(img, W * SS * 0.5, cy + 3.5 * SS, H * SS * 0.96, (110, 120, 150), 0.95)

    # LAS LINEAS DE CAMPO DIPOALARES (3 bucles anidados, girados).
    for k, (L, c, a) in enumerate([(7.2, AZUL, 0.5), (5.4, CIAN, 0.6), (3.8, VIOLET, 0.5)]):
        dipole_loop(img, cx, cy, L * SS, 0.0, 0.55, c, a)

    # EL NUCLEO MINUSCULO: bloom blanco-azul apilado (ULTRADENSO).
    glow_dot(img, cx, cy, 4.6, AZUL, 0.55)
    glow_dot(img, cx, cy, 2.6, WHITE, 0.95)
    glow_dot(img, cx, cy, 1.1, (255, 255, 255), 1.0)

    # EL DESTELLO CHISPEANTE de 4 puntas (la superficie a 20 Hz).
    capsule(img, cx - 7.5 * SS, cy, cx + 7.5 * SS, cy, 0.85, CIAN, 0.7)
    capsule(img, cx, cy - 7.5 * SS, cx, cy + 7.5 * SS, 0.85, CIAN, 0.7)
    # ...y la diagonal corta (el parpadeo).
    capsule(img, cx - 3.6 * SS, cy - 3.6 * SS, cx + 3.6 * SS, cy + 3.6 * SS,
            0.55, WHITE, 0.8)

    # LOS POLOS: dos perlas de aurora en el eje.
    glow_dot(img, cx, cy - 4.0 * SS, 1.1, CIAN, 0.85)
    glow_dot(img, cx, cy + 4.0 * SS, 1.1, CIAN, 0.85)

    save(img, W, H, os.path.join(WPN_OUT, 'NeutronStarStaff.png'))


def icon_pulsar():
    """EL BASTON DEL PULSAR: el nucleo girando con sus DOS HACES
    POLARES opuestos — el faro de 400 px en miniatura."""
    W, H = 30, 30
    img = new_canvas(W, H)
    cx, cy = W * SS * 0.5, H * SS * 0.42

    WHITE = (245, 250, 255)
    AZUL = (140, 185, 255)
    CIAN = (100, 220, 240)

    # El mastil.
    staff_handle(img, W * SS * 0.5, cy + 3.5 * SS, H * SS * 0.96, (110, 130, 160), 0.95)

    # LOS DOS HACES OPUESTOS (el faro): diagonal NW-SE con su cono.
    dx, dy = np.cos(0.62), np.sin(0.62)
    for sign in (1, -1):
        ax, ay = cx + sign * dx * 2.2 * SS, cy + sign * dy * 2.2 * SS
        bx, by = cx + sign * dx * 13.2 * SS, cy + sign * dy * 13.2 * SS
        capsule(img, ax, ay, bx, by, 2.6, AZUL, 0.30)   # el velo del haz
        capsule(img, ax, ay, bx, by, 1.4, CIAN, 0.75)   # el cuerpo
        capsule(img, ax + sign * 0.4 * SS, ay + sign * 0.4 * SS,
                bx - sign * 1.2 * SS, by - sign * 1.2 * SS, 0.6, WHITE, 0.9)
        glow_dot(img, bx, by, 2.0, CIAN, 0.55)          # la boca lejana

    # EL NUCLEO: nucleo denso + el disco de emisiOn.
    glow_dot(img, cx, cy, 5.2, AZUL, 0.5)
    glow_dot(img, cx, cy, 3.0, WHITE, 0.95)
    # El anillo de emisiOn perpendicular al haz.
    ring_stroke(img, cx, cy, 4.6 * SS, 4.6 * SS, 0.62 + np.pi / 2, 0.75, CIAN, 0.55, 32)

    # LOS PULSOS sincronizados: perlas a lo largo de los haces.
    for i in (2, 4, 6):
        for sign in (1, -1):
            px = cx + sign * dx * i * SS
            py = cy + sign * dy * i * SS
            glow_dot(img, px, py, 0.8, WHITE, 0.7)

    save(img, W, H, os.path.join(WPN_OUT, 'PulsarStaff.png'))


def icon_whitedwarf():
    """EL BASTON DE LA ENANA BLANCA: estrella pequeria blanco-azul
    CRISTALINA con facetas hexagonales fijas + aro de acreciOn tenue."""
    W, H = 30, 30
    img = new_canvas(W, H)
    cx, cy = W * SS * 0.5, H * SS * 0.40

    WHITE = (250, 252, 255)
    AZUL = (185, 210, 255)
    CIAN = (150, 230, 240)

    # El mastil.
    staff_handle(img, W * SS * 0.5, cy + 3.5 * SS, H * SS * 0.96, (130, 140, 165), 0.95)

    # EL FULGOR FRIO estable (el bloom cristalino).
    glow_dot(img, cx, cy, 6.8, AZUL, 0.35)
    glow_dot(img, cx, cy, 4.2, CIAN, 0.45)

    # EL CUERPO DENSO: disco blanco-azul cristalino.
    glow_dot(img, cx, cy, 2.9, WHITE, 0.95)

    # LAS FACETAS HEXAGONALES fijas (7 mini-hex: 6 trazos cada una).
    facets = [(-2.1, -1.3), (1.9, -1.7), (-0.4, 1.9), (2.4, 0.9),
              (-2.5, 1.1), (0.6, -2.6), (1.0, 2.7)]
    for k, (fx, fy) in enumerate(facets):
        px, py = cx + fx * SS, cy + fy * SS
        r = 0.9 + 0.5 * hash01(7, k, 3)
        for i in range(6):
            a0 = i / 6 * 2 * np.pi + k * 0.4
            a1 = (i + 1) / 6 * 2 * np.pi + k * 0.4
            capsule(img, px + np.cos(a0) * r * SS, py + np.sin(a0) * r * SS,
                    px + np.cos(a1) * r * SS, py + np.sin(a1) * r * SS,
                    0.42, CIAN if k % 2 else WHITE, 0.8)
        glow_dot(img, px, py, 0.5, WHITE, 0.9)

    # EL ANILLO DE ACRECION tenue (elipse inclinada).
    ring_stroke(img, cx, cy, 6.4 * SS, 2.3 * SS, -0.5, 0.5, AZUL, 0.4, 40)

    # EL DESTELLO de 4 puntas corto (el brillo frio).
    capsule(img, cx - 5.2 * SS, cy, cx + 5.2 * SS, cy, 0.7, CIAN, 0.65)
    capsule(img, cx, cy - 5.2 * SS, cx, cy + 5.2 * SS, 0.7, CIAN, 0.65)

    save(img, W, H, os.path.join(WPN_OUT, 'WhiteDwarfStaff.png'))


def icon_deadstar():
    """EL BASTON DE LA ESTRELLA MUERTA: el NUCLEO OSCURO (enana negra)
    con brasas frIas cayendo y un eco rUnico apagado."""
    W, H = 30, 30
    img = new_canvas(W, H)
    cx, cy = W * SS * 0.5, H * SS * 0.40

    GRIS = (95, 90, 100)
    BRASA = (255, 120, 60)
    BRASA_FRIA = (110, 160, 220)
    AMBAR = (180, 120, 70)

    # El mastil (madera petrificada — mas oscura).
    staff_handle(img, W * SS * 0.5, cy + 3.5 * SS, H * SS * 0.96, (70, 60, 62), 0.95)

    # EL HALO DE ENTROPIA (anillo tenue ambar).
    glow_dot(img, cx, cy, 6.2, (60, 40, 30), 0.45)

    # EL NUCLEO OSCURO: el disco negro de la enana negra. En un icono
    # aditivo "negro" = nada: dibujamos el RIM de su horizonte muerto.
    ring_stroke(img, cx, cy, 3.4 * SS, 3.4 * SS, 0, 0.62, GRIS, 0.8, 36)
    ring_stroke(img, cx, cy, 4.4 * SS, 4.4 * SS, 0, 0.4, (55, 50, 58), 0.5, 36)
    # el centro: casi nada — un rescoldo ambar.
    glow_dot(img, cx, cy, 1.5, AMBAR, 0.5)
    glow_dot(img, cx, cy, 0.6, BRASA, 0.75)

    # LAS BRASAS FRIAS cayendo (lenguas invertidas, alfa bajo).
    for k, (ex, ey) in enumerate([(-4.6, -2.8), (4.2, -3.4), (-2.2, -4.4), (3.0, 2.9)]):
        px, py = cx + ex * SS, cy + ey * SS
        c = BRASA_FRIA if k % 2 else BRASA
        capsule(img, px, py, px + 0.5 * SS, py + 1.8 * SS, 0.55, c, 0.6)
        glow_dot(img, px, py, 0.7, c, 0.8)

    # EL ECO RUNICO apagado (3 glifos titilando muy tenues).
    for i in range(3):
        a = i / 3 * 2 * np.pi + 0.8
        px, py = cx + np.cos(a) * 5.6 * SS, cy + np.sin(a) * 5.6 * SS
        capsule(img, px - 1.2 * SS, py - 1.0 * SS, px + 1.2 * SS, py + 1.0 * SS,
                0.4, AMBAR, 0.45)
        glow_dot(img, px, py, 0.6, (200, 140, 90), 0.5)

    save(img, W, H, os.path.join(WPN_OUT, 'DeadStarStaff.png'))


def icon_redsupergiant():
    """EL BASTON DE LA SUPERGIGANTE ROJA: la bola ENORME roja frIa con
    sus celdas de convecciOn (blobs rojos-naranjas subiendo/bajando)."""
    W, H = 30, 30
    img = new_canvas(W, H)
    cx, cy = W * SS * 0.5, H * SS * 0.42

    ROJO = (255, 90, 40)
    NARANJA = (255, 140, 60)
    ORO = (255, 190, 90)
    OSCURO = (140, 30, 20)

    # El mastil (corto: la gigante lo domina).
    staff_handle(img, W * SS * 0.5, cy + 6.0 * SS, H * SS * 0.96, (120, 70, 45), 0.95)

    # LA ATMOSFERA EXTENSA (corona roja 3x: tres velos).
    glow_dot(img, cx, cy, 10.6, OSCURO, 0.35)
    glow_dot(img, cx, cy, 8.2, ROJO, 0.45)

    # EL CUERPO ENORME: disco rojo frIo.
    glow_dot(img, cx, cy, 5.8, ROJO, 0.85)
    glow_dot(img, cx, cy, 4.0, NARANJA, 0.9)

    # LAS CELDAS DE CONVECCION (blobs voraces girando lento).
    cells = [(-2.6, -1.6, 1.5), (2.2, -2.0, 1.3), (0.2, 1.2, 1.7),
             (-2.9, 1.4, 1.1), (3.0, 1.1, 1.2), (-0.6, -3.1, 1.0),
             (1.6, 3.0, 1.0), (0.9, -0.9, 0.9)]
    for k, (bx, by, r) in enumerate(cells):
        c = ORO if k % 3 == 0 else (NARANJA if k % 3 == 1 else ROJO)
        a = 0.5 + 0.25 * hash01(11, k, 5)
        glow_dot(img, cx + bx * SS, cy + by * SS, r, c, a)

    # EL LIMBO CALIENTE (el borde interior mas brillante).
    ring_stroke(img, cx, cy, 4.6 * SS, 4.6 * SS, 0, 0.5, NARANJA, 0.4, 40)

    save(img, W, H, os.path.join(WPN_OUT, 'RedSupergiantStaff.png'))


def icon_magnetar():
    """EL BASTON DEL MAGNETAR: la estrella de neutrones EXTREMA —
    lineas de campo VIOLETAS retorcidas + la descarga violenta."""
    W, H = 30, 30
    img = new_canvas(W, H)
    cx, cy = W * SS * 0.5, H * SS * 0.40

    VIOLET = (170, 110, 240)
    VIOLET_OSC = (120, 70, 200)
    ROSA = (230, 150, 255)
    WHITE = (255, 250, 255)

    # El mastil (violeta oscuro).
    staff_handle(img, W * SS * 0.5, cy + 3.5 * SS, H * SS * 0.96, (100, 70, 140), 0.95)

    # LAS LINEAS DE CAMPO INTENSAS Y RETORCIDAS (5 bucles, cada uno
    # con su torsion: el RotatedBy por punto del magnetar).
    for k, (L, twist, c, a) in enumerate([
            (8.0, 0.55, VIOLET, 0.45), (6.4, -0.7, ROSA, 0.4),
            (5.0, 0.9, VIOLET_OSC, 0.5), (3.8, -0.5, VIOLET, 0.55)]):
        pts = []
        segs = 44
        for i in range(segs + 1):
            t = i / segs * 2 * np.pi
            r = L * np.sin(t) ** 2
            r = max(r, 0.02)
            # LA TORSION: el angulo se deforma por seno doble.
            tt = t + twist * np.sin(2 * t)
            pts.append((cx + r * np.cos(tt) * SS, cy + r * np.sin(tt) * SS))
        for i in range(segs):
            capsule(img, pts[i][0], pts[i][1], pts[i + 1][0], pts[i + 1][1],
                    0.5, c, a)

    # EL NUCLEO violeta-blanco (mas calmado que el de neutrones: toda
    # la energia vive en el CAMPO).
    glow_dot(img, cx, cy, 3.6, VIOLET, 0.6)
    glow_dot(img, cx, cy, 1.8, WHITE, 0.95)

    # LA DESCARGA violenta cruzando el campo (el rayo violeta).
    bolt = [(cx - 6.5 * SS, cy - 4.5 * SS), (cx - 2.0 * SS, cy - 1.6 * SS),
            (cx - 3.4 * SS, cy + 0.8 * SS), (cx + 0.6 * SS, cy + 2.6 * SS)]
    for i in range(len(bolt) - 1):
        capsule(img, bolt[i][0], bolt[i][1], bolt[i + 1][0], bolt[i + 1][1],
                0.8, ROSA, 0.85)
    # ...y las chispas de reconexion (puntos blancos estallando).
    for (sx, sy) in [(-5.2, -3.6), (1.8, 1.4), (-1.2, -0.4)]:
        glow_dot(img, cx + sx * SS, cy + sy * SS, 0.8, WHITE, 0.9)

    save(img, W, H, os.path.join(WPN_OUT, 'MagnetarStaff.png'))


# =====================================================================
#  2. SOMBRAS DE PROYECTIL (76x76) — patron del estilo de la casa
# =====================================================================

def shadow_star(name, rim_a, rim_b, core_r=26.0, rim_width=6.0, dark_core=False):
    """El disco con rim (patron CrimsonBlackHoleProjectile) adaptado:
    `core_r` controla el TAMANO del nucleo (la gigante lo llena, la
    de neutrones lo encoge) y `dark_core` pinta el cuerpo NEGRO (la
    estrella muerta)."""
    S = 76
    img = new_canvas(S, S)
    cx, cy = S * SS * 0.5, S * SS * 0.5
    R = core_r * SS

    yy, xx = np.mgrid[0:S * SS, 0:S * SS]
    d = np.sqrt((xx - cx) ** 2 + (yy - cy) ** 2) / R  # en unidades de radio

    # EL DISCO: solido (claro u OSCURO).
    body_a = np.clip(1.35 - d, 0, 1) * 0.9
    # EL RIM: banda con el color de identidad.
    rim = np.exp(-((d - 0.86) ** 2) / (2 * (rim_width / R * SS) ** 2))

    base_rgb = (14, 10, 20) if dark_core else (rim_a[0] * 0.10 + 8,
                                               rim_a[1] * 0.10 + 6,
                                               rim_a[2] * 0.10 + 10)
    col = np.zeros((S * SS, S * SS, 4), dtype=np.float32)
    col[..., 0] = rim_a[0] * rim + base_rgb[0] * body_a
    col[..., 1] = rim_a[1] * rim + base_rgb[1] * body_a
    col[..., 2] = rim_a[2] * rim + base_rgb[2] * body_a
    a = np.maximum(body_a * (0.30 if dark_core else 0.55), rim * 0.85)
    col[..., 3] = a * 255

    # El rim EXTERIOR desvanecido (el color B mas alla).
    rim2 = np.exp(-((d - 1.06) ** 2) / (2 * (rim_width / R * SS * 1.4) ** 2))
    col[..., 0] += rim_b[0] * rim2 * 0.55
    col[..., 1] += rim_b[1] * rim2 * 0.55
    col[..., 2] += rim_b[2] * rim2 * 0.55
    col[..., 3] = np.maximum(col[..., 3], rim2 * 0.5 * 255)

    out = downsample(col, S, S)
    out[..., 3] = np.where(out[..., 3] < 6, 0, out[..., 3])
    path = os.path.join(PRJ_OUT, name)
    Image.fromarray(out, 'RGBA').save(path)
    print(f"  -> {name} (76x76) {int((out[..., 3] > 8).sum())} px visibles")


def shadow_pulsar(name, rim_a, rim_b):
    """LA SOMBRA DEL PULSAR: nucleo pequeNo + los DOS HACES opuestos
    alargados (la cruz del faro — no un simple disco)."""
    S = 76
    img = new_canvas(S, S)
    cx, cy = S * SS * 0.5, S * SS * 0.5

    WHITE = (245, 250, 255)
    AZUL = (140, 185, 255)
    CIAN = (100, 220, 240)

    # LOS DOS HACES opuestos (diagonal — el faro barriendo).
    dx, dy = np.cos(0.62), np.sin(0.62)
    for sign in (1, -1):
        ax, ay = cx + sign * dx * 6 * SS, cy + sign * dy * 6 * SS
        bx, by = cx + sign * dx * 34 * SS, cy + sign * dy * 34 * SS
        capsule(img, ax, ay, bx, by, 5.5, AZUL, 0.30)
        capsule(img, ax, ay, bx, by, 2.6, CIAN, 0.55)
        capsule(img, ax, ay, bx, by, 1.1, WHITE, 0.8)
        glow_dot(img, bx, by, 4.0, CIAN, 0.5)

    # EL NUCLEO pequeNo + rim (identidad de la casa).
    glow_dot(img, cx, cy, 13.0, AZUL, 0.5)
    glow_dot(img, cx, cy, 7.5, CIAN, 0.75)
    glow_dot(img, cx, cy, 3.2, WHITE, 0.95)

    # EL ANILLO DE EMISION perpendicular al haz.
    ring_stroke(img, cx, cy, 11.5 * SS, 11.5 * SS, 0.62 + np.pi / 2,
                1.1, rim_a if isinstance(rim_a, tuple) else CIAN, 0.5, 36)

    out = downsample(img, S, S)
    out[..., 3] = np.where(out[..., 3] < 6, 0, out[..., 3])
    path = os.path.join(PRJ_OUT, name)
    Image.fromarray(out, 'RGBA').save(path)
    print(f"  -> {name} (76x76) {int((out[..., 3] > 8).sum())} px visibles")


def shadow_magnetar(name):
    """LA SOMBRA DEL MAGNETAR: nucleo violeta + las LINEAS DE CAMPO
    retorcidas dibujadas alrededor (la firma del campo extremo)."""
    S = 76
    img = new_canvas(S, S)
    cx, cy = S * SS * 0.5, S * SS * 0.5

    VIOLET = (170, 110, 240)
    ROSA = (230, 150, 255)
    WHITE = (255, 250, 255)

    # LAS LINEAS DE CAMPO retorcidas (3 bucles dipolares torcidos).
    for L, twist, c, a in [(30.0, 0.5, VIOLET, 0.4), (22.0, -0.65, ROSA, 0.45),
                           (15.5, 0.8, VIOLET, 0.5)]:
        pts = []
        segs = 48
        for i in range(segs + 1):
            t = i / segs * 2 * np.pi
            r = max(L * np.sin(t) ** 2, 0.6)
            tt = t + twist * np.sin(2 * t)
            pts.append((cx + r * np.cos(tt) * SS / 1.0, cy + r * np.sin(tt) * SS))
        for i in range(segs):
            capsule(img, pts[i][0], pts[i][1], pts[i + 1][0], pts[i + 1][1],
                    0.9, c, a)

    # EL NUCLEO violeta-blanco con rim.
    glow_dot(img, cx, cy, 12.0, VIOLET, 0.55)
    glow_dot(img, cx, cy, 6.5, ROSA, 0.75)
    glow_dot(img, cx, cy, 2.8, WHITE, 0.95)

    out = downsample(img, S, S)
    out[..., 3] = np.where(out[..., 3] < 6, 0, out[..., 3])
    path = os.path.join(PRJ_OUT, name)
    Image.fromarray(out, 'RGBA').save(path)
    print(f"  -> {name} (76x76) {int((out[..., 3] > 8).sum())} px visibles")


# =====================================================================

if __name__ == '__main__':
    print("GEN v6.26 — LAS ESTRELLAS REALES: iconos de arma:")
    icon_neutronstar()
    icon_pulsar()
    icon_whitedwarf()
    icon_deadstar()
    icon_redsupergiant()
    icon_magnetar()
    print("GEN v6.26 — sombras de proyectil:")
    # El nucleo de cada sombra respeta la ESCALA real: la de neutrones
    # y el magnetar son MINUSCULOS (12-14 px), la enana blanca pequeNa
    # (20), la muerta contenida (16), la gigante LLENA el lienzo (34).
    shadow_star('NeutronStarProjectile.png', (190, 215, 255), (110, 230, 235),
                core_r=13.0, rim_width=4.5)
    shadow_pulsar('PulsarProjectile.png', (140, 185, 255), (100, 220, 240))
    shadow_star('WhiteDwarfProjectile.png', (210, 230, 255), (150, 230, 240),
                core_r=17.0, rim_width=4.0)
    shadow_star('DeadStarProjectile.png', (150, 100, 70), (70, 60, 80),
                core_r=15.0, rim_width=5.5, dark_core=True)
    shadow_star('RedSupergiantProjectile.png', (255, 110, 55), (180, 40, 25),
                core_r=33.0, rim_width=8.0)
    shadow_magnetar('MagnetarProjectile.png')
    print("OK — 12 PNGs generados.")
