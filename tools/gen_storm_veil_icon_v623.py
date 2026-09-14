#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_storm_veil_icon_v623.py — EL ICONO DE LA ENVOLTURA DE RAYOS.

La tormenta que te viste: un ANILLO ELÉCTRICO de arcos fractales
(oro solar + azul estelar + núcleo blanco) rodeando el hueco del cuerpo
— el mismo lenguaje visual del render de StormVeilRenderer.
Salida: Content/Items/Cosmetics/StormVeilItem.png (30×30, RGBA).
"""
import os
import numpy as np
from PIL import Image

PROJ_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                        '..', 'AethonMod')
COSM_OUT = os.path.join(PROJ_DIR, 'Content', 'Items', 'Cosmetics')

SS = 8                     # supersampling
CW, CH = 30, 30            # tamaño final del icono
OCW, OCH = CW * SS, CH * SS

# la paleta del rayo aprobado (concordancia total)
GOLD = (255, 195, 85)
GOLD_WARM = (255, 225, 140)
STAR_BLUE = (150, 180, 255)
WHITE_INCAN = (255, 250, 235)


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


def polyline_glow(img, pts, width, color, alpha):
    """Una polilínea de puntos con brillo gaussiano (el pincel filamento)."""
    OCHh, OCWw = img.shape[0], img.shape[1]
    yy, xx = np.mgrid[0:OCHh, 0:OCWw]
    for i in range(len(pts) - 1):
        a, b = pts[i], pts[i + 1]
        seg = (b[0] - a[0], b[1] - a[1])
        ln = max(np.hypot(*seg), 1e-6)
        for t in np.linspace(0.25, 0.75, 3):
            px, py = a[0] + seg[0] * t, a[1] + seg[1] * t
            d = np.sqrt((xx - px) ** 2 + (yy - py) ** 2)
            g = np.exp(-((d / (width * SS * 0.62)) ** 2))
            aa = g * alpha * 255
            m = g * alpha > 0.02
            img[..., 0] = np.where(m, np.maximum(img[..., 0], color[0] * g), img[..., 0])
            img[..., 1] = np.where(m, np.maximum(img[..., 1], color[1] * g), img[..., 1])
            img[..., 2] = np.where(m, np.maximum(img[..., 2], color[2] * g), img[..., 2])
            img[..., 3] = np.maximum(img[..., 3], aa)
    return img


def make_storm_veil_icon():
    """El anillo eléctrico: arcos fractales alrededor del hueco del cuerpo."""
    img = np.zeros((OCH, OCW, 4), dtype=float)
    yy, xx = np.mgrid[0:OCH, 0:OCW]
    cx, cy = OCW / 2.0, OCH / 2.0
    R = OCW * 0.36          # el carril (la silueta)

    seed = 61

    # === 1. LOS ARCOS — descargas fractales abrazando el contorno ===
    for arc in range(5):
        a1 = arc / 5.0 * 2 * np.pi + hash01(seed, arc, 3) * 0.6
        a2 = a1 + 0.9 + hash01(seed, arc, 7) * 0.5
        pts = []
        n = 7
        for i in range(n + 1):
            t = i / n
            ang = a1 + (a2 - a1) * t
            # jitter radial (la envolvente respira chispas)
            j = 1.0 + (hash01(seed, arc * 17, i * 31 + 5) - 0.5) * 0.34 \
                * np.sin(t * np.pi)
            pts.append((cx + np.cos(ang) * R * j, cy + np.sin(ang) * R * j))
        col = GOLD if arc % 2 == 0 else STAR_BLUE
        # halo ancho + cuerpo
        img = polyline_glow(img, pts, 2.1, col, 0.55)
        img = polyline_glow(img, pts, 0.9, col, 0.95)

    # === 2. LOS CHISPAZOS — filamentos cruzando el centro ===
    for bolt in range(3):
        ang1 = hash01(seed, bolt, 41) * 2 * np.pi
        ang2 = ang1 + np.pi + (hash01(seed, bolt, 43) - 0.5) * 1.1
        p1 = (cx + np.cos(ang1) * R, cy + np.sin(ang1) * R)
        p2 = (cx + np.cos(ang2) * R, cy + np.sin(ang2) * R)
        # la polilínea con jitter perpendicular (ZigPath casero)
        pts = []
        n = 6
        for i in range(n + 1):
            t = i / n
            bx, by = p1[0] + (p2[0] - p1[0]) * t, p1[1] + (p2[1] - p1[1]) * t
            env = np.sin(t * np.pi) ** 0.8
            nx, ny = -(p2[1] - p1[1]), (p2[0] - p1[0])
            nl = max(np.hypot(nx, ny), 1e-6)
            j = (hash01(seed, bolt * 7, i * 13 + 3) - 0.5) * 2 * 7 * SS * env
            pts.append((bx + nx / nl * j, by + ny / nl * j))
        pts[0], pts[-1] = p1, p2
        img = polyline_glow(img, pts, 1.6, GOLD_WARM, 0.85)

    # === 3. LOS NÚCLEOS BLANCOS — la vena razor de cada arco ===
    for arc in range(5):
        a1 = arc / 5.0 * 2 * np.pi + hash01(seed, arc, 3) * 0.6
        a2 = a1 + 0.9 + hash01(seed, arc, 7) * 0.5
        pts = []
        n = 5
        for i in range(n + 1):
            t = i / n
            ang = a1 + (a2 - a1) * t
            j = 1.0 + (hash01(seed, arc * 17, i * 31 + 5) - 0.5) * 0.34 \
                * np.sin(t * np.pi)
            pts.append((cx + np.cos(ang) * R * j, cy + np.sin(ang) * R * j))
        img = polyline_glow(img, pts, 0.45, WHITE_INCAN, 1.0)

    # === 4. EL CORAZÓN — el aura azul-estelar tenue en el hueco ===
    d = np.hypot(xx - cx, yy - cy)
    heart = np.exp(-((d / (R * 0.62)) ** 2))
    img[..., 0] = np.maximum(img[..., 0], STAR_BLUE[0] * heart * 0.35)
    img[..., 1] = np.maximum(img[..., 1], STAR_BLUE[1] * heart * 0.35)
    img[..., 2] = np.maximum(img[..., 2], STAR_BLUE[2] * heart * 0.40)
    img[..., 3] = np.maximum(img[..., 3], heart * 90)

    # el disco de recorte (icono circular)
    r = np.hypot(xx - cx, yy - cy)
    img[r > 14.4 * SS, 3] = 0
    return img


def finish_cosm(img, name):
    out = img.reshape(CH, SS, CW, SS, 4).mean(axis=(1, 3))
    out = np.clip(out, 0, 255).astype(np.uint8)
    a = out[..., 3].astype(float)
    a = np.where(a < 24, 0, np.where(a > 200, 255, a))
    out[..., 3] = a.astype(np.uint8)
    path = os.path.join(COSM_OUT, name)
    Image.fromarray(out, 'RGBA').save(path)
    print('OK', path, out.shape)


if __name__ == '__main__':
    print('=== v6.23 · ICONO DE LA ENVOLTURA DE RAYOS ===')
    finish_cosm(make_storm_veil_icon(), 'StormVeilItem.png')
    print('=== LISTO v6.23 ===')
