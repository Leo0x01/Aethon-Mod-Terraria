#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
mock_desgarros_v635.py — EL MOCK 1:1 DE LOS CUATRO DESGARROS NUEVOS (v6.35).

Reimplementa en numpy las CUATRO primitivas de RiftLib.DesgarrosNuevos.cs
con SUS MISMAS fórmulas (constantes, hash, alfas, colores hex) sobre el
cielo claro de Terraria — 4 paneles:
  1. VorticeColapso      (El Corazón del Colapso)
  2. GargantaVacio       (La Garganta del Vacío)
  3. UmbralRoto          (El Umbral Roto)
  4. LeviatanEspectral   (El Leviatán Espectral)
"""
import math
import os

import numpy as np
from PIL import Image

BASE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(os.path.dirname(BASE), "research", "v635")
os.makedirs(OUT, exist_ok=True)

W, H = 480, 300
CX, CY = W // 2, H // 2
TIME = 1.35          # un instante vivo
SEED = 4177


# ---- el hash determinista de VFXCore ----
def H01(seed, a, b):
    x = math.sin(seed * 127.1 + a * 311.7 + b * 74.7) * 43758.5453
    return x - math.floor(x)


def sky(w, h):
    yy = np.linspace(0, 1, h)[:, None, None]
    top = np.array([108, 152, 205], np.float32) / 255
    bot = np.array([168, 205, 235], np.float32) / 255
    img = top[None, None, :] * (1 - yy) + bot[None, None, :] * yy
    return np.broadcast_to(img, (h, w, 3)).copy()


def capsule(img, mx, my, largo, ancho, rot, rgb, alpha):
    """SoftGlow estirado (largo × ancho) centrado en (mx,my) — la cápsula."""
    add_glow(img, mx, my, (largo + ancho) * 0.5, ancho * 0.95, rot, rgb, alpha)


def add_glow(img, cx, cy, rw, rh, rot, rgb, alpha):
    h, w = img.shape[:2]
    x0, x1 = max(0, int(cx - rw)), min(w, int(cx + rw))
    y0, y1 = max(0, int(cy - rh)), min(h, int(cy + rh))
    if x1 <= x0 or y1 <= y0:
        return
    xs = np.arange(x0, x1) + 0.5 - cx
    ys = np.arange(y0, y1) + 0.5 - cy
    X, Y = np.meshgrid(xs, ys)
    cr, sr = math.cos(rot), math.sin(rot)
    U = (X * cr + Y * sr) / max(rw, 0.5)
    V = (-X * sr + Y * cr) / max(rh, 0.5)
    d = np.sqrt(U * U + V * V)
    fall = np.clip(1 - d, 0, 1) ** 1.6
    a = fall * alpha
    img[y0:y1, x0:x1] = np.clip(
        img[y0:y1, x0:x1] + np.array(rgb, np.float32)[None, None, :] * a[..., None], 0, 1)


def alpha_ellipse(img, cx, cy, rw, rh, rot, rgb, alpha):
    h, w = img.shape[:2]
    x0, x1 = max(0, int(cx - rw - 2)), min(w, int(cx + rw + 2))
    y0, y1 = max(0, int(cy - rh - 2)), min(h, int(cy + rh + 2))
    if x1 <= x0 or y1 <= y0:
        return
    xs = np.arange(x0, x1) + 0.5 - cx
    ys = np.arange(y0, y1) + 0.5 - cy
    X, Y = np.meshgrid(xs, ys)
    cr, sr = math.cos(rot), math.sin(rot)
    U = (X * cr + Y * sr) / max(rw, 0.5)
    V = (-X * sr + Y * cr) / max(rh, 0.5)
    inside = (U * U + V * V) <= 1.0
    src = np.array(rgb, np.float32)
    reg = img[y0:y1, x0:x1]
    a = inside * alpha
    img[y0:y1, x0:x1] = src[None, None, :] * a[..., None] + reg * (1 - a[..., None])


def ring_line(img, cx, cy, r, grosor, rgb, alpha):
    h, w = img.shape[:2]
    x0, x1 = max(0, int(cx - r - grosor)), min(w, int(cx + r + grosor))
    y0, y1 = max(0, int(cy - r - grosor)), min(h, int(cy + r + grosor))
    if x1 <= x0 or y1 <= y0:
        return
    xs = np.arange(x0, x1) + 0.5 - cx
    ys = np.arange(y0, y1) + 0.5 - cy
    X, Y = np.meshgrid(xs, ys)
    d = np.sqrt(X * X + Y * Y)
    band = np.clip(1 - np.abs(d - r) / max(grosor, 0.5), 0, 1) ** 1.3
    img[y0:y1, x0:x1] = np.clip(
        img[y0:y1, x0:x1] + np.array(rgb, np.float32)[None, None, :] * (band * alpha)[..., None], 0, 1)


def star4(img, cx, cy, rot, largo, ancho, rgb, alpha):
    capsule(img, cx, cy, largo, ancho, rot, rgb, alpha)
    capsule(img, cx, cy, largo, ancho, rot + math.pi / 2, rgb, alpha)


def lerp_c(c1, c2, t):
    return tuple(a + (b - a) * t for a, b in zip(c1, c2))


# ====================================================================
#  1 — VorticeColapso (fórmulas 1:1 de RiftLib.DesgarrosNuevos.cs)
# ====================================================================
def vortice_colapso():
    img = sky(W, H)
    R = 105.0
    open_ = 1.0
    spin = TIME * 0.55 + SEED * 0.1

    blanco = (255/255, 250/255, 205/255)
    naranja = (255/255, 165/255, 0.0)
    carmesi = (220/255, 20/255, 60/255)
    rojo = (255/255, 69/255, 0.0)
    rojo_osc = (139/255, 0.0, 0.0)
    oro = (255/255, 215/255, 0.0)

    # === 1. EL HALO ===
    add_glow(img, CX, CY, R * 2.35, R * 2.35, 0, rojo_osc, 0.20)
    add_glow(img, CX, CY, R * 1.55, R * 1.55, 0, carmesi, 0.16)

    # === 2. LOS CUATRO FILAMENTOS ===
    for a in range(4):
        base_ang = a * math.pi / 2 + spin
        prev = (CX, CY)
        for s in range(17):
            f = s / 16
            r = R * (0.14 + 0.86 * f)
            ang = base_ang + 0.85 * f * f
            p = (CX + math.cos(ang) * r, CY + math.sin(ang) * r)
            w_ = max(0.1, (R * 0.115) * (1 - f) + (R * 0.018) * f)
            c = lerp_c(blanco, naranja, min(1.0, f * 1.4))
            if f > 0.55:
                c = lerp_c(naranja, carmesi, (f - 0.55) / 0.45)
            pulse = 0.75 + 0.25 * math.sin(TIME * 3.2 - f * 5.5 + a * 1.6)
            if s > 0:
                mx, my = (prev[0] + p[0]) / 2, (prev[1] + p[1]) / 2
                largo = math.hypot(p[0] - prev[0], p[1] - prev[1])
                rot = math.atan2(p[1] - prev[1], p[0] - prev[0])
                capsule(img, mx, my, largo + w_ * 2.2, w_ * 2.4, rot, rojo, 0.34 * pulse)
                capsule(img, mx, my, largo + w_ * 0.8, w_ * 0.95, rot, c, 0.85 * pulse)
            prev = p

    # === 3. EL NÚCLEO ===
    heart = 0.5 + 0.5 * math.sin(TIME * 2.4 + SEED)
    star4(img, CX, CY, spin, R * 0.85 * (0.85 + 0.15 * heart), R * 0.10, blanco, 0.55)
    add_glow(img, CX, CY, R * 0.31, R * 0.31, 0, naranja, 0.50)
    add_glow(img, CX, CY, R * 0.15, R * 0.15, 0, blanco, 0.80 + 0.20 * heart)

    # === 4. LAS CHISPAS DE ORO ===
    for d in range(10):
        h1 = H01(SEED, d, 211); h2 = H01(SEED, d, 223)
        t = (TIME * (0.22 + 0.26 * h2) + h1) % 1.0
        rr = R * (0.30 + 0.85 * t)
        ang = h2 * math.tau + spin * 1.6 + t * 1.9
        pp = (CX + math.cos(ang) * rr, CY + math.sin(ang) * rr)
        s_ = 2.5 + 4.5 * h2
        vida = math.sin(t * math.pi)
        add_glow(img, pp[0], pp[1], s_, s_, 0, lerp_c(oro, blanco, h1), 0.65 * vida)
    return img


# ====================================================================
#  2 — GargantaVacio (fórmulas 1:1)
# ====================================================================
def garganta_vacio():
    img = sky(W, H)
    R = 100.0

    blanco_rosado = (255/255, 228/255, 250/255)
    magenta = (255/255, 64/255, 208/255)
    violeta = (90/255, 11/255, 122/255)
    cian = (224/255, 255/255, 255/255)

    # === PASO 1 — EL NÚCLEO NEGRO (pase alfa) ===
    alpha_ellipse(img, CX, CY, R * 0.46, R * 0.46, 0, (5/255, 2/255, 8/255), 242/255)

    # === PASO 2 — resplandores ===
    add_glow(img, CX, CY, R * 0.65, R * 0.65, 0, blanco_rosado, 0.14)
    add_glow(img, CX, CY, R * 0.95, R * 0.95, 0, violeta, 0.22)

    # === EL ANILLO ENERGÉTICO (la fórmula fiel sin(t·20 − time·5) por
    #     segmentos, semiejes 1.90/1.18, tilt −0.38, 36 segmentos) ===
    def anillo_energia(front):
        rr = R * 0.62
        semi_a, semi_b, tilt = 1.90, 1.18, -0.38
        spin = TIME * 0.30
        t0 = 0.0 if front else math.pi
        span = math.pi
        segs = 36
        bright = 1.30 if front else 0.90
        prev = None
        for s in range(segs + 1):
            t = t0 + span * s / segs
            t_spin = t + spin
            # posición en la elipse rotada
            ex = math.cos(t) * semi_a * rr
            ey = math.sin(t) * semi_b * rr
            x = CX + ex * math.cos(tilt) - ey * math.sin(tilt)
            y = CY + ex * math.sin(tilt) + ey * math.cos(tilt)
            glow = 0.5 + 0.5 * math.sin(t_spin * 20 - TIME * 5)
            turb = 0.74 + 0.26 * H01(SEED, 700 + s, int(TIME * 12))
            inten = glow * turb * bright
            if inten > 0.82:
                c = blanco_rosado
            elif inten > 0.45:
                c = magenta
            else:
                c = violeta
            if prev is not None:
                mx, my = (prev[0] + x) / 2, (prev[1] + y) / 2
                largo = math.hypot(x - prev[0], y - prev[1])
                rot = math.atan2(y - prev[1], x - prev[0])
                capsule(img, mx, my, largo, 0.36 * rr * (0.7 + inten), rot, c, 0.40 * inten)
                capsule(img, mx, my, largo, 0.12 * rr * inten, rot, c, 0.85 * inten)
            prev = (x, y)

    anillo_energia(front=False)

    # === LOS TRES BRAZOS ESPIRALES ===
    for a in range(3):
        base_ang = a * math.tau / 3 - TIME * 0.42
        prev = (CX, CY)
        for s in range(14):
            f = s / 13
            r = R * (0.55 + 0.95 * f)
            ang = base_ang + 1.25 * f
            p = (CX + math.cos(ang) * r, CY + math.sin(ang) * r)
            if s > 0:
                mx, my = (prev[0] + p[0]) / 2, (prev[1] + p[1]) / 2
                largo = math.hypot(p[0] - prev[0], p[1] - prev[1])
                rot = math.atan2(p[1] - prev[1], p[0] - prev[0])
                w_ = (R * 0.085) * (1 - f) + (R * 0.016) * f
                c = lerp_c(magenta, violeta, f * 0.8)
                capsule(img, mx, my, largo + w_ * 2, w_ * 2.6, rot, c, 0.34 - 0.16 * f)
            prev = p

    # === EL ARO DEL HORIZONTE ===
    ring_line(img, CX, CY, R * 0.64, 3.0, blanco_rosado, 0.34)

    anillo_energia(front=True)

    # === EL POLVO ESTELAR cayendo ===
    for d in range(12):
        h1 = H01(SEED, d, 227); h2 = H01(SEED, d, 229)
        t = 1.0 - ((TIME * (0.24 + 0.30 * h2) + h1) % 1.0)
        rr = R * 1.65 * t
        ang = h2 * math.tau - TIME * (1.4 + h1 * 1.1) + t * 2.2
        pp = (CX + math.cos(ang) * rr, CY + math.sin(ang) * rr)
        s_ = 2.5 + 4.0 * h2
        capsule(img, pp[0], pp[1], s_ * 0.6, s_ * 0.5 * (1 + t * 0.7), ang,
                lerp_c(blanco_rosado, cian, h1), 0.55 * t)
    return img


# ====================================================================
#  3 — UmbralRoto (fórmulas 1:1)
# ====================================================================
def umbral_roto():
    img = sky(W, H)
    ancho = 420.0
    len_ = ancho
    alto = min(120.0, ancho * 0.18)
    cx, cy = CX, CY

    hueso = (224/255, 224/255, 224/255)
    blanco = (1.0, 1.0, 1.0)
    cian = (0.0, 1.0, 1.0)
    magenta = (255/255, 0.0, 249/255)

    # === PASO 1 — EL VACÍO SUPERIOR (pase alfa) ===
    banda_c = cy - alto * 0.5
    alpha_ellipse(img, cx, banda_c, len_ * 0.53, alto * 1.075, 0, (0.0, 0.0, 10/255), 205/255)
    alpha_ellipse(img, cx, banda_c, len_ * 0.47, alto * 0.775, 0, (0.0, 0.0, 10/255), 235/255)

    # === PASO 2 — LA LÍNEA con aberración cromática ===
    flick = 0.80 + 0.20 * math.sin(TIME * 60 * 0.31 + SEED)
    ab = 1.5 + 1.1 * math.sin(TIME * 9)
    capsule(img, cx, cy + ab, len_, 3.2, 0, cian, 0.40 * flick)
    capsule(img, cx, cy - ab, len_, 3.2, 0, magenta, 0.40 * flick)
    capsule(img, cx, cy, len_, 2.6, 0, blanco, 0.85 * flick)
    capsule(img, cx, cy, len_, 7.5, 0, cian, 0.10)
    star4(img, cx - len_ * 0.5, cy, 0, 14, 3.2, blanco, 0.55)
    star4(img, cx + len_ * 0.5, cy, 0, 14, 3.2, blanco, 0.55)

    # === 3. EL ESQUELETO ESPECTRAL (v6.35b: MÁS GRANDE Y BRILLANTE) ===
    mecer = math.sin(TIME * 0.8 + SEED) * 0.12
    corazon = (cx - 0 + math.sin(TIME * 0.5) * len_ * 0.05, cy - alto * 0.52)
    dir_e = (math.cos(mecer), math.sin(mecer))
    perp_e = (-dir_e[1], dir_e[0])
    verts = []
    for v in range(9):
        f = v / 8
        curva = math.sin(f * 2.6 + TIME * 0.9) * alto * 0.20
        p = (corazon[0] - dir_e[0] * (f * len_ * 0.185) + perp_e[0] * curva,
             corazon[1] - dir_e[1] * (f * len_ * 0.185) + perp_e[1] * curva)
        verts.append(p)
        w_ = 5.6 * (1 - f) + 2.6 * f
        vv = 0.72 + 0.18 * math.sin(TIME * 1.6 - v * 0.7)
        capsule(img, p[0], p[1], w_ * 3.0, w_ * 2.4, mecer, hueso, vv)
        if v > 0:
            pv = 0.52 + 0.14 * math.sin(TIME * 1.6 - (v - 1) * 0.7)
            q = verts[v - 1]
            mx, my = (q[0] + p[0]) / 2, (q[1] + p[1]) / 2
            l = math.hypot(p[0] - q[0], p[1] - q[1])
            capsule(img, mx, my, l + w_, w_ * 1.0, math.atan2(p[1] - q[1], p[0] - q[0]), hueso, pv)
    # LA CABEZA con mandíbula en V
    cab = verts[0]
    for lado in (1, -1):
        m = (cab[0] + dir_e[0] * 13 + perp_e[0] * 8 * lado,
             cab[1] + dir_e[1] * 13 + perp_e[1] * 8 * lado)
        mx, my = (cab[0] + m[0]) / 2, (cab[1] + m[1]) / 2
        l = math.hypot(m[0] - cab[0], m[1] - cab[1])
        capsule(img, mx, my, l + 3, 3.4, math.atan2(m[1] - cab[1], m[0] - cab[0]), hueso, 0.78)
    add_glow(img, cab[0] + perp_e[0] * 3.4, cab[1] + perp_e[1] * 3.4, 3.4, 3.4, 0, blanco, 0.45)

    # === 4. LAS PARTÍCULAS DE DATOS ===
    for d in range(14):
        h1 = H01(SEED, d, 233); h2 = H01(SEED, d, 239); h3 = H01(SEED, d, 241)
        x = cx + (h1 - 0.5) * len_ * 1.02
        y = cy + (h2 - 0.5) * alto * 1.6 * (1.0 if h3 > 0.5 else -0.5)
        st = math.sin(TIME * 60 * (0.19 + 0.24 * h3) + h1 * 6.28) * 0.5 + 0.5
        if st < 0.42:
            continue
        s_ = 1.6 + 2.6 * h2
        add_glow(img, x, y, s_, s_, 0, cian if h3 > 0.5 else blanco, 0.55 * st)
    return img


# ====================================================================
#  4 — LeviatanEspectral (fórmulas 1:1)
# ====================================================================
def leviatan_espectral():
    img = sky(W, H)
    cabeza = (CX - 130, CY + 40)
    dir_ = (0.94, -0.34)
    dir_ = (dir_[0] / math.hypot(*dir_), dir_[1] / math.hypot(*dir_))
    perp = (-dir_[1], dir_[0])

    blanco_frio = (232/255, 244/255, 255/255)
    borde = (159/255, 216/255, 255/255)

    Sep, Frec = 13.0, 2.5

    # LA COLUMNA (14 vértebras con la onda en S)
    Vert = 14
    posiciones = []
    for v in range(Vert):
        f = v / (Vert - 1)
        amp = 10 + 16 * f
        fase = TIME * Frec * math.tau - v * 0.55
        p = (cabeza[0] - dir_[0] * (Sep * v) + perp[0] * (math.sin(fase) * amp),
             cabeza[1] - dir_[1] * (Sep * v) + perp[1] * (math.sin(fase) * amp))
        posiciones.append(p)

    # El rastro frío
    cola = posiciones[-1]
    capsule(img, cola[0] - dir_[0] * 26, cola[1] - dir_[1] * 26, 58, 26,
            math.atan2(dir_[1], dir_[0]), borde, 0.10)

    for v, p in enumerate(posiciones):
        f = v / (Vert - 1)
        w_ = 7.5 * (1 - f) + 3.2 * f
        tang = dir_ if v == 0 else (p[0] - posiciones[v - 1][0], p[1] - posiciones[v - 1][1])
        rot = math.atan2(tang[1], tang[0])
        pulse = 0.72 + 0.28 * math.sin(TIME * 3.4 - v * 0.8)
        capsule(img, p[0], p[1], w_ * 2.4, w_ * 1.8, rot, borde, 0.30 * pulse)
        add_glow(img, p[0], p[1], w_ * 0.9, w_ * 0.9, 0, blanco_frio, 0.85 * pulse)

        # LAS ALETAS
        if v in (2, 4, 6, 8, 10, 12):
            lado = 1 if (v // 2) % 2 == 0 else -1
            f_aleta = 1 - f * 0.55
            largo = (26 + 14 * H01(SEED, v, 251)) * f_aleta
            ang_fin = rot + math.pi - lado * (0.85 + 0.2 * H01(SEED, v, 257))
            fin = (p[0] + math.cos(ang_fin) * largo, p[1] + math.sin(ang_fin) * largo)
            mx, my = (p[0] + fin[0]) / 2, (p[1] + fin[1]) / 2
            capsule(img, mx, my, largo + 3, 3.2 * f_aleta,
                    math.atan2(fin[1] - p[1], fin[0] - p[0]), borde, 0.38 - 0.16 * f)

    # LA CABEZA
    cab = posiciones[0]
    hocico = (cab[0] + dir_[0] * 12, cab[1] + dir_[1] * 12)
    capsule(img, (cab[0] + hocico[0]) / 2, (cab[1] + hocico[1]) / 2, 16, 7,
            math.atan2(dir_[1], dir_[0]), blanco_frio, 0.80)
    abre = 0.42 + 0.10 * math.sin(TIME * 1.7)
    for lado in (1, -1):
        m = (hocico[0] + perp[0] * 10 * abre + dir_[0] * 7,
             hocico[1] + perp[1] * 10 * abre + dir_[1] * 7)
        mx, my = (hocico[0] + m[0]) / 2, (hocico[1] + m[1]) / 2
        l = math.hypot(m[0] - hocico[0], m[1] - hocico[1])
        capsule(img, mx, my, l + 2.5, 3.0,
                math.atan2(m[1] - hocico[1], m[0] - hocico[0]), blanco_frio, 0.70)
    # LA CUENCA + LA CRESTA
    add_glow(img, cab[0] + dir_[0] * 4.5 + perp[0] * 3.5,
             cab[1] + dir_[1] * 4.5 + perp[1] * 3.5, 3.0, 3.0, 0, (1, 1, 1), 0.90)
    ang_cresta = math.atan2(dir_[1], dir_[0]) - math.pi / 4
    cresta = (cab[0] + math.cos(ang_cresta) * 20, cab[1] + math.sin(ang_cresta) * 20)
    capsule(img, (cab[0] + cresta[0]) / 2, (cab[1] + cresta[1]) / 2, 24, 4.5,
            ang_cresta, borde, 0.42)
    return img


# ---- LA TIRA DE 4 PANELES ----
paneles = [
    ("MOCK_CORAZON_COLAPSO_v635.png", vortice_colapso()),
    ("MOCK_GARGANTA_VACIO_v635.png", garganta_vacio()),
    ("MOCK_UMBRAL_ROTO_v635.png", umbral_roto()),
    ("MOCK_LEVIATAN_ESPECTRAL_v635.png", leviatan_espectral()),
]

for nombre, panel in paneles:
    Image.fromarray((np.clip(panel, 0, 1) * 255).astype(np.uint8)).save(os.path.join(OUT, nombre))

# La cuadrícula 2×2 para la vista conjunta
grid = np.zeros((H * 2 + 8, W * 2 + 8, 3), np.float32)
for i, (_, panel) in enumerate(paneles):
    y, x = divmod(i, 2)
    grid[y * (H + 8):y * (H + 8) + H, x * (W + 8):x * (W + 8) + W] = panel
Image.fromarray((np.clip(grid, 0, 1) * 255).astype(np.uint8)).save(
    os.path.join(OUT, "MOCK_DESGARROS_v635.png"))

print("5 mocks generados en", OUT)
