#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_v634_assets.py — LOS DOS PNGs FALTANTES DE LAS BOLSAS v6.33.

La auditoría v6.34 los cazó: BolsaDosFormas y BolsaDesgarros salieron a
medias (clase + entrega en TestingPlayer, PERO sin sprite → tML fallaría
la carga del mod con missing texture). Este script las genera con el
PATRÓN EXACTO DE LA CASA (gen_v629_assets.py):

  · BolsaDosFormas    — la bolsa de LAS DOS FORMAS: interior DUAL (mitad
    oro solar → mitad violeta del vacío) y el emblema del Yin cósmico:
    media runa solar dorada abrazando un creciente oscuro (las dos
    formas de uso: la luz y su reverso).
  · BolsaDesgarros    — la bolsa de LOS DESGARROS: saco casi negro con
    limbo magenta-glitch y el emblema del DESGARRO diagonal (grieta
    eléctrica con astillas — la firma de la familia).
"""
import os
import numpy as np
from PIL import Image

PROJ_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                        '..', 'AethonMod')
BOLSA_OUT = os.path.join(PROJ_DIR, 'Content', 'Items', 'Bolsas')

SS = 8  # supersampling


# =====================================================================
#  LOS HELPERS DE LA CASA (patrón gen_v629_assets.py — idénticos)
# =====================================================================

def new_canvas(w, h):
    return np.zeros((h * SS, w * SS, 4), dtype=np.float32)


def downsample(img, w, h):
    """SS×SS → 1×1 promediando (anti-alias de la casa)."""
    small = img.reshape(h, SS, w, SS, 4).mean(axis=(1, 3))
    small[:, :, 3] = np.clip(small[:, :, 3], 0, 255)
    return small.astype(np.uint8)


def add_color(img, px, py, radius, color, alpha):
    """Añade luz (aditiva por canal) con caída gaussiana."""
    H, W = img.shape[0], img.shape[1]
    if radius <= 0.1 or alpha <= 0.01:
        return
    x0, x1 = max(0, int(px - radius * 3)), min(W, int(px + radius * 3) + 1)
    y0, y1 = max(0, int(py - radius * 3)), min(H, int(py + radius * 3) + 1)
    if x0 >= x1 or y0 >= y1:
        return
    xs = np.arange(x0, x1) - px
    ys = np.arange(y0, y1) - py
    gx = np.exp(-(xs / radius) ** 2)
    gy = np.exp(-(ys / radius) ** 2)
    g = np.outer(gy, gx) * alpha
    img[y0:y1, x0:x1, 0] += g * color[0]
    img[y0:y1, x0:x1, 1] += g * color[1]
    img[y0:y1, x0:x1, 2] += g * color[2]
    img[y0:y1, x0:x1, 3] = np.maximum(img[y0:y1, x0:x1, 3], g * 255.0)


def set_alpha_floor(img):
    lum = img[:, :, :3].max(axis=2)
    img[:, :, 3] = np.maximum(img[:, :, 3], lum)


def add_line(img, x0, y0, x1, y1, width, color, alpha=1.0):
    L = np.hypot(x1 - x0, y1 - y0)
    if L < 0.5:
        return
    n = max(2, int(L / (0.6 * SS)))
    for t in np.linspace(0, 1, n):
        px = x0 + (x1 - x0) * t
        py = y0 + (y1 - y0) * t
        add_color(img, px, py, width * 0.5, color, alpha)


def add_ring(img, cx, cy, radius, width, color, alpha=1.0):
    H, W = img.shape[0], img.shape[1]
    ys = np.arange(H)
    xs = np.arange(W)
    XX, YY = np.meshgrid(xs, ys)
    rr = np.sqrt((XX - cx) ** 2 + (YY - cy) ** 2)
    aro = np.exp(-np.abs(rr - radius) / width) * alpha
    img[:, :, 0] += aro * color[0]
    img[:, :, 1] += aro * color[1]
    img[:, :, 2] += aro * color[2]
    img[:, :, 3] = np.maximum(img[:, :, 3], aro * 255)


# =====================================================================
#  LA BOLSA DE LA CASA (gen_bolsa del v629, idéntica)
# =====================================================================

def gen_bolsa(nombre, cuerpo, borde, interior, cordel, emblema):
    W, H = 30, 30
    img = new_canvas(W, H)
    cx = W * SS / 2
    cy = H * SS / 2 + 1.5 * SS

    # --- EL CUERPO DEL SACO ---
    ys = np.arange(H * SS)
    xs = np.arange(W * SS)
    XX, YY = np.meshgrid(xs, ys)
    body_r = 11.2 * SS
    body_cy = cy + 2.2 * SS
    ell = ((XX - cx) / body_r) ** 2 + ((YY - body_cy) / (9.6 * SS)) ** 2
    neck_half = 3.4 * SS
    neck = (np.abs(XX - cx) < neck_half) & \
           (YY > cy - 7.5 * SS) & (YY < body_cy - 3.0 * SS)
    shape = (ell < 1.0) | neck

    # --- EL INTERIOR (aquí llega YA teñido: las dos bolsas nuevas usan
    #     gradientes DUALES, así que `interior` es una FUNCIÓN canal→matriz) ---
    for c in range(3):
        channel = interior(c, XX, YY, cx, body_cy, body_r)
        img[:, :, c] += np.where(shape, channel, 0.0).astype(np.float32)
    img[:, :, 3] += np.where(shape, 250.0, 0.0)

    # --- EL LIMBO LUMINOSO ---
    rim = np.exp(-np.abs(ell - 0.92) * 9.0)
    img[:, :, 0] += rim * borde[0]
    img[:, :, 1] += rim * borde[1]
    img[:, :, 2] += rim * borde[2]
    img[:, :, 3] = np.maximum(img[:, :, 3], rim * 210)

    # --- EL CORDEL ---
    for i, dx in enumerate((-1.1, 0.0, 1.1)):
        px = cx + dx * SS
        add_color(img, px, cy - 6.2 * SS, 0.85 * SS, cordel, 0.95)
        add_line(img, px, cy - 6.2 * SS, px + dx * 1.6 * SS,
                 cy - 4.4 * SS, 0.62 * SS, cordel, 0.85)

    # --- EL EMBLEMA FRONTAL ---
    emblema(cx, body_cy, img)

    set_alpha_floor(img)
    out = downsample(img, W, H)
    path = os.path.join(BOLSA_OUT, nombre + ".png")
    Image.fromarray(out, "RGBA").save(path)
    print("OK", path)


# =====================================================================
#  LOS INTERIORES DUALES (la novedad v6.34)
# =====================================================================

def interior_dual(oro, vacio):
    """Mitad izquierda ORO SOLAR → mitad derecha VACÍO: LAS DOS FORMAS."""
    oro_a, oro_b = np.array(oro) / 255.0, np.array(oro) / 5.0 / 255.0
    vac_a, vac_b = np.array(vacio) / 255.0, np.array(vacio) / 5.0 / 255.0

    def interior(c, XX, YY, cx, body_cy, body_r):
        dist = np.clip(np.sqrt(((XX - cx) / body_r) ** 2 +
                               ((YY - body_cy) / (9.6 * SS)) ** 2), 0, 1)
        # el peso dual: -1 izquierda (oro) → +1 derecha (vacío)
        lado = np.tanh((XX - cx) / (body_r * 0.55))
        base = oro_a[c] * (1 - (lado + 1) * 0.5) + vac_a[c] * (lado + 1) * 0.5
        edge = oro_b[c] * (1 - (lado + 1) * 0.5) + vac_b[c] * (lado + 1) * 0.5
        return (base + (edge - base) * dist) * 255.0

    return interior


def interior_plano(color):
    """El interior clásico de gradiente radial de la casa."""
    a = np.array(color) / 255.0
    b = np.array(color) / 5.0 / 255.0

    def interior(c, XX, YY, cx, body_cy, body_r):
        dist = np.clip(np.sqrt(((XX - cx) / body_r) ** 2 +
                               ((YY - body_cy) / (9.6 * SS)) ** 2), 0, 1)
        return (a[c] + (b[c] - a[c]) * dist) * 255.0

    return interior


# =====================================================================
#  LOS EMBLEMAS NUEVOS
# =====================================================================

def emblema_dos_formas(cx, cy, img):
    """EL YIN CÓSMICO: media runa solar dorada (arriba-izq) abrazando un
    creciente de vacío (abajo-der) — las dos formas de uso de un arma."""
    oro = (255, 196, 90)
    oro_tip = (255, 240, 190)
    vacio = (150, 90, 235)
    vacio_tip = (215, 190, 255)

    # La media runa: un arco de 200° en oro con su punta radial
    R = 4.6 * SS
    for a in np.linspace(np.pi * 0.15, np.pi * 1.05, 26):
        px = cx + np.cos(a) * R * 0.95 - 1.2 * SS
        py = cy + np.sin(a) * R * 0.95 - 1.0 * SS
        add_color(img, px, py, 0.75 * SS, oro, 0.95)
    # punta de la runa (el trazo maestro)
    add_line(img, cx - 1.2 * SS, cy - 1.0 * SS,
             cx - 1.2 * SS + 5.4 * SS * np.cos(np.pi * 0.62),
             cy - 1.0 * SS + 5.4 * SS * np.sin(np.pi * 0.62),
             0.9 * SS, oro_tip, 0.9)

    # El creciente de vacío: disco oscuro + mordida
    add_color(img, cx + 1.8 * SS, cy + 1.6 * SS, 2.6 * SS, vacio, 0.9)
    # el borde interior del creciente (la mordida de luz)
    add_ring(img, cx + 1.8 * SS, cy + 1.6 * SS, 2.6 * SS, 0.55 * SS,
             vacio_tip, 0.85)

    # LA UNIÓN: un punto blanco donde las dos formas se tocan
    add_color(img, cx + 0.3 * SS, cy + 0.1 * SS, 1.0 * SS,
              (255, 250, 240), 1.0)


def emblema_desgarro(cx, cy, img):
    """EL DESGARRO: grieta eléctrica diagonal magenta con astillas
    glitch — la firma de la familia de los portales."""
    magenta = (255, 60, 200)
    cian = (90, 230, 255)
    blanco = (255, 240, 255)

    # LA GRIETA: zigzag diagonal de 3 quiebros, gruesa al centro
    pts = [(cx - 5.0 * SS, cy - 5.5 * SS),
           (cx - 1.5 * SS, cy - 2.0 * SS),
           (cx + 1.0 * SS, cy - 3.2 * SS),
           (cx + 4.8 * SS, cy + 5.2 * SS)]
    for i in range(len(pts) - 1):
        x0, y0 = pts[i]
        x1, y1 = pts[i + 1]
        w = (1.05 + 0.35 * i) * SS
        add_line(img, x0, y0, x1, y1, w, magenta, 0.95)

    # EL NÚCLEO BLANCO de la grieta (donde la realidad se abre)
    for i in range(len(pts) - 1):
        x0, y0 = pts[i]
        x1, y1 = pts[i + 1]
        add_line(img, x0 + 0.3 * SS, y0, x1 + 0.3 * SS, y1,
                 0.38 * SS, blanco, 0.9)

    # LAS ASTILLAS GLITCH: rayitas cian desplazadas (el error de datos)
    add_line(img, cx - 3.2 * SS, cy - 1.0 * SS, cx - 1.0 * SS,
             cy - 0.4 * SS, 0.5 * SS, cian, 0.85)
    add_line(img, cx + 0.6 * SS, cy + 1.4 * SS, cx + 3.0 * SS,
             cy + 0.8 * SS, 0.45 * SS, cian, 0.7)
    add_line(img, cx - 0.6 * SS, cy + 3.0 * SS, cx + 2.2 * SS,
             cy + 3.4 * SS, 0.4 * SS, cian, 0.55)

    # El eco del vacío dentro de la grieta (el centro oscuro)
    add_color(img, cx - 0.4 * SS, cy - 2.4 * SS, 1.1 * SS,
              (60, 8, 55), 0.0)  # (nulo: en bolsa pequeña el eco resta)


def main():
    # --- BOLSA DE LAS DOS FORMAS: saco pardo, limbo dorado, interior
    #     DUAL oro→violeta, cordel neutro claro ---
    gen_bolsa("BolsaDosFormas",
              cuerpo=(92, 70, 46),
              borde=(255, 208, 120),
              interior=interior_dual((235, 165, 60), (86, 54, 150)),
              cordel=(255, 232, 185),
              emblema=emblema_dos_formas)

    # --- BOLSA DE LOS DESGARROS: saco casi negro, limbo magenta,
    #     interior del vacío profundo, cordel cian glitch ---
    gen_bolsa("BolsaDesgarros",
              cuerpo=(30, 22, 38),
              borde=(255, 90, 220),
              interior=interior_plano((96, 46, 120)),
              cordel=(140, 235, 255),
              emblema=emblema_desgarro)


if __name__ == "__main__":
    main()
