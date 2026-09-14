#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_v627_assets.py — LOS PNGs DE v6.27.

1. LA BOLSA DEL ARSENAL PRIMORDIAL (30×30): la bolsa cosmológica con el
   sello rúnico dorado — el ÚNICO ítem que se entrega al jugador (todo el
   arsenal vive dentro; clic derecho lo despliega).
2. EL BASTÓN DEL OCASO DE AETHON (28×30): el arma suprema del patrón gauge
   (carga → burst → lockout) — orbe de eclipse mitad dorado/mitad vacío
   con el arco medidor de 20 segmentos.
3. LOS DOS BUFFS (32×32): OcasoActivo (el eclipse encendido) y
   Sobrecalentado (el bastón humeando en rojo).
4. LAS DOS SOMBRAS de proyectil (76×76): el fragmento del ocaso y la
   muerte del ocaso (requisito de autoload de la casa).

Patrón PIL+numpy de la casa (gen_cinco_bastones_v626.py).
"""
import os
import numpy as np
from PIL import Image

PROJ_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                        '..', 'AethonMod')
ITEM_OUT = os.path.join(PROJ_DIR, 'Content', 'Items')
WPN_OUT = os.path.join(PROJ_DIR, 'Content', 'Weapons', 'Cosmic')
PRJ_OUT = os.path.join(PROJ_DIR, 'Content', 'Projectiles', 'Cosmic')
BUF_OUT = os.path.join(PROJ_DIR, 'Content', 'Buffs')

SS = 8  # supersampling


# =====================================================================
#  LOS HELPERS DE LA CASA
# =====================================================================

def new_canvas(w, h):
    return np.zeros((h * SS, w * SS, 4), dtype=np.float32)


def downsample(img, w, h):
    pil = Image.fromarray(np.clip(img, 0, 255).astype(np.uint8), 'RGBA')
    pil = pil.resize((w, h), Image.LANCZOS)
    return np.array(pil, dtype=np.uint8)


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
    """El alfa nunca baja del brillo máximo (sprites de luz)."""
    lum = img[:, :, :3].max(axis=2)
    img[:, :, 3] = np.maximum(img[:, :, 3], lum)


# =====================================================================
#  1. LA BOLSA DEL ARSENAL (30×30)
# =====================================================================

def gen_bolsa():
    W, H = 30, 30
    img = new_canvas(W, H)
    cx, cy = W * SS / 2, H * SS / 2 + 1.5 * SS

    # --- EL CUERPO DE LA BOLSA (silueta de saco cosmológico) ---
    # Un saco: base ancha redondeada que se estrecha hacia el cuello.
    ys = np.arange(H * SS)
    xs = np.arange(W * SS)
    XX, YY = np.meshgrid(xs, ys)

    # El ancho del saco por fila: estrecha arriba (cuello), ancha abajo.
    # hago: cuerpo = elipse de (7.5, 9) centrada en (cx, cy+2), más un
    # cuello cilíndrico de ancho 5 desde cy-6 hasta cy-2.
    body_r = 11.2 * SS
    body_cy = cy + 2.2 * SS
    ell = ((XX - cx) / body_r) ** 2 + ((YY - body_cy) / (9.6 * SS)) ** 2

    neck_half = 3.4 * SS
    neck = (np.abs(XX - cx) < neck_half) & (YY > cy - 7.5 * SS) & (YY < body_cy - 3.0 * SS)

    shape = (ell < 1.0) | neck

    # --- EL VACÍO INTERIOR (la bolsa guarda un cosmos) ---
    # gradiente radial: violeta profundo en el centro → azul noche al borde.
    dist = np.sqrt(((XX - cx) / body_r) ** 2 + ((YY - body_cy) / (9.6 * SS)) ** 2)
    dist = np.clip(dist, 0, 1)
    # colores: centro (58, 22, 110) → borde (16, 10, 42)
    for c in range(3):
        base_c = [58, 22, 110][c]
        edge_c = [16, 10, 42][c]
        channel = base_c + (edge_c - base_c) * dist
        # el cuello también (más oscuro)
        img[:, :, c] += np.where(shape, channel, 0.0).astype(np.float32)
    img[:, :, 3] += np.where(shape, 252.0, 0.0)

    # --- ESTRELLAS DENTRO (el cosmos guardado) ---
    estrellas = [(cx - 5.2 * SS, body_cy - 1.0 * SS, 0.85, (255, 236, 170)),
                 (cx + 4.4 * SS, body_cy + 2.6 * SS, 0.65, (190, 210, 255)),
                 (cx + 1.2 * SS, body_cy - 3.6 * SS, 0.5, (255, 170, 220)),
                 (cx - 2.6 * SS, body_cy + 4.8 * SS, 0.42, (170, 240, 255)),
                 (cx + 5.8 * SS, body_cy - 4.2 * SS, 0.35, (255, 255, 255))]
    for sx, sy, sr, scol in estrellas:
        add_color(img, sx, sy, sr * SS * 0.8, scol, 0.9)

    # --- EL BRILLO DEL LIMBO (el borde del saco deja ver la luz) ---
    rim = np.exp(-np.abs(ell - 0.92) * 9.0)
    img[:, :, 0] += rim * 120
    img[:, :, 1] += rim * 70
    img[:, :, 2] += rim * 190
    img[:, :, 3] = np.maximum(img[:, :, 3], rim * 210)

    # --- EL CORDÓN DORADO (el lazo del cuello) ---
    add_color(img, cx - neck_half - 1.2 * SS, cy - 6.6 * SS, 1.6 * SS, (255, 196, 84), 0.95)
    add_color(img, cx + neck_half + 1.2 * SS, cy - 6.6 * SS, 1.6 * SS, (255, 196, 84), 0.95)
    add_color(img, cx, cy - 7.8 * SS, 2.0 * SS, (255, 214, 110), 1.0)

    # --- EL SELLO RÚNICO (un glifo de anillo en el frente) ---
    # el anillo del sol de la casa: aro dorado + 6 puntas orbitales.
    ring_r = 4.6 * SS
    ang = np.arctan2(YY - (body_cy - 0.6 * SS), XX - cx)
    rr = np.sqrt((XX - cx) ** 2 + (YY - (body_cy - 0.6 * SS)) ** 2)
    aro = np.exp(-np.abs(rr - ring_r) / (0.55 * SS))
    img[:, :, 0] += aro * 235
    img[:, :, 1] += aro * 190
    img[:, :, 2] += aro * 90
    img[:, :, 3] = np.maximum(img[:, :, 3], aro * 255)

    # las 6 puntas del sello (la.runa de la casa: muescas radiales)
    for k in range(6):
        a = k * np.pi / 3 + 0.35
        px = cx + np.cos(a) * ring_r
        py = body_cy - 0.6 * SS + np.sin(a) * ring_r
        add_color(img, px, py, 0.9 * SS, (255, 224, 130), 0.95)

    # el corazón del sello: un destello de sol enano
    add_color(img, cx, body_cy - 0.6 * SS, 1.7 * SS, (255, 244, 200), 1.0)

    set_alpha_floor(img)
    return downsample(img, W, H)


# =====================================================================
#  2. EL BASTÓN DEL OCASO (28×30)
# =====================================================================

def gen_ocaso():
    W, H = 28, 30
    img = new_canvas(W, H)
    cx = W * SS / 2
    orb_cy = 8.2 * SS

    # --- EL ASTA (negra con vetas doradas) ---
    # línea vertical desde el orbe hasta abajo, ligeramente curvada.
    for t in np.linspace(0, 1, 60):
        y = orb_cy + 3.0 * SS + t * (H * SS - orb_cy - 5.5 * SS)
        x = cx + np.sin(t * 2.2) * 1.1 * SS
        # el grosor se estrecha hacia arriba
        wdt = (1.7 - 0.7 * t) * SS
        add_color(img, x, y, wdt * 0.55, (34, 20, 52), 1.0)
        add_color(img, x, y, wdt * 0.28, (255, 196, 100), 0.35)

    # --- EL ORBE DEL ECLIPSE (mitad sol dorado / mitad vacío violeta) ---
    orb_r = 5.6 * SS
    ys = np.arange(H * SS)
    xs = np.arange(W * SS)
    XX, YY = np.meshgrid(xs, ys)
    dist_o = np.sqrt((XX - cx) ** 2 + (YY - orb_cy) ** 2)
    dentro = dist_o < orb_r

    # mitad IZQUIERDA: el sol dorado (rampa de la casa)
    izq = dentro & (XX < cx)
    t_izq = np.clip(1 - dist_o / orb_r, 0, 1)
    img[:, :, 0] += np.where(izq, 120 + 135 * t_izq ** 0.6, 0)
    img[:, :, 1] += np.where(izq, 60 + 130 * t_izq ** 0.9, 0)
    img[:, :, 2] += np.where(izq, 10 + 60 * t_izq, 0)

    # mitad DERECHA: el vacío violeta oscuro con borde vivo
    der = dentro & (XX >= cx)
    t_der = np.clip(1 - dist_o / orb_r, 0, 1)
    img[:, :, 0] += np.where(der, 30 + 60 * t_der, 0)
    img[:, :, 1] += np.where(der, 8 + 30 * t_der, 0)
    img[:, :, 2] += np.where(der, 46 + 110 * t_der, 0)

    img[:, :, 3] += np.where(dentro, 252, 0)

    # el LIMBO del eclipse: anillo fino brillante
    limbo = np.exp(-np.abs(dist_o - orb_r) / (0.5 * SS))
    img[:, :, 0] += limbo * 200
    img[:, :, 1] += limbo * 150
    img[:, :, 2] += limbo * 230
    img[:, :, 3] = np.maximum(img[:, :, 3], limbo * 255)

    # el destello central dorado y el vacío del centro
    add_color(img, cx - 1.6 * SS, orb_cy, 2.2 * SS, (255, 248, 210), 1.0)
    add_color(img, cx + 2.2 * SS, orb_cy, 1.8 * SS, (170, 90, 255), 0.85)

    # --- EL ARCO GAUGE (20 segmentos alrededor del orbe) ---
    # el medidor del arma: segmentos de violeta→dorado, 2/3 llenos.
    gauge_r = orb_r + 2.6 * SS
    lleno = 14  # de 20
    for k in range(20):
        a0 = -np.pi * 0.85 + k * (np.pi * 1.7 / 19)
        a = a0
        lleno_k = k < lleno
        px = cx + np.cos(a) * gauge_r
        py = orb_cy + np.sin(a) * gauge_r
        if lleno_k:
            t = k / 19
            col = (150 + 105 * t, 80 + 130 * t, 255 - 55 * t)
            al = 0.95
        else:
            col = (70, 55, 95)
            al = 0.4
        add_color(img, px, py, 1.05 * SS, col, al)

    # --- LAS GUARDAS (dos cuernos rodeando el orbe) ---
    for sgn in (-1, 1):
        for t in np.linspace(0, 1, 24):
            a = sgn * (0.25 + 1.1 * t)
            rr = gauge_r - 1.2 * SS * t
            px = cx + np.cos(a) * sgn * 0 + np.cos(a) * rr
            py = orb_cy + np.sin(a) * rr
            col = (255, 196, 90) if t < 0.5 else (170, 110, 250)
            add_color(img, px, py, (1.3 - 0.7 * t) * SS * 0.6, col, 0.8)

    set_alpha_floor(img)
    return downsample(img, W, H)


# =====================================================================
#  3. LOS BUFFS (32×32)
# =====================================================================

def gen_buff_ocaso():
    W, H = 32, 32
    img = new_canvas(W, H)
    cx, cy = W * SS / 2, H * SS / 2

    # EL ECLIPSE ENCENDIDO: disco dorado con el arco gauge lleno pulsando.
    r = 8.5 * SS
    ys = np.arange(H * SS)
    xs = np.arange(W * SS)
    XX, YY = np.meshgrid(xs, ys)
    dist = np.sqrt((XX - cx) ** 2 + (YY - cy) ** 2)
    t = np.clip(1 - dist / r, 0, 1)

    img[:, :, 0] += np.where(dist < r, 120 + 135 * t ** 0.6, 0)
    img[:, :, 1] += np.where(dist < r, 55 + 135 * t ** 0.9, 0)
    img[:, :, 2] += np.where(dist < r, 15 + 90 * t, 0)
    img[:, :, 3] += np.where(dist < r, 250, 0)

    # el anillo de 20 segmentos (lleno — el modo activo)
    for k in range(20):
        a = k * np.pi / 10
        px = cx + np.cos(a) * (r + 3.2 * SS)
        py = cy + np.sin(a) * (r + 3.2 * SS)
        tk = k / 19
        add_color(img, px, py, 1.25 * SS,
                  (150 + 105 * tk, 80 + 130 * tk, 255 - 55 * tk), 0.95)

    # el corazón blanco-oro
    add_color(img, cx, cy, 2.6 * SS, (255, 250, 220), 1.0)

    set_alpha_floor(img)
    return downsample(img, W, H)


def gen_buff_sobrecalentado():
    W, H = 32, 32
    img = new_canvas(W, H)
    cx, cy = W * SS / 2, H * SS / 2

    # EL BASTÓN HUMEANDO: asta rota en diagonal + vapor rojo subiendo.
    # el asta (diagonal, roja-ceniza)
    for t in np.linspace(0, 1, 40):
        x = cx + (t - 0.5) * 14 * SS
        y = cy + (0.5 - t) * 8 * SS
        col = (200, 60, 40) if t < 0.6 else (255, 110, 60)
        add_color(img, x, y, 1.5 * SS * 0.7, col, 0.9)

    # la grieta del sobrecalentamiento (rayo rojo en el medio)
    zig = [(cx - 4 * SS, cy + 4 * SS), (cx - 1 * SS, cy + 1.5 * SS),
           (cx - 2.5 * SS, cy - 0.5 * SS), (cx + 1 * SS, cy - 3 * SS)]
    for i in range(len(zig) - 1):
        x0, y0 = zig[i]
        x1, y1 = zig[i + 1]
        for tt in np.linspace(0, 1, 12):
            add_color(img, x0 + (x1 - x0) * tt, y0 + (y1 - y0) * tt,
                      1.1 * SS * 0.7, (255, 90, 60), 1.0)

    # el vapor subiendo (tres volutas tenues)
    for k, (ox, phase) in enumerate([(-4.5 * SS, 0.0), (0.5 * SS, 1.7), (5.0 * SS, 3.1)]):
        for tt in np.linspace(0, 1, 18):
            y = cy - 2 * SS - tt * 10 * SS
            x = cx + ox + np.sin(tt * 5.0 + phase) * 2.4 * SS * tt
            al = 0.5 * (1 - tt)
            add_color(img, x, y, (2.8 - 1.4 * tt) * SS * 0.7, (255, 120, 90), al)

    set_alpha_floor(img)
    return downsample(img, W, H)


# =====================================================================
#  4. LAS SOMBRAS DE PROYECTIL (76×76)
# =====================================================================

def gen_sombraFragmento():
    W, H = 76, 76
    img = new_canvas(W, H)
    cx, cy = W * SS / 2, H * SS / 2

    # EL FRAGMENTO DEL OCASO: estrella de 4 puntas dorada-violeta.
    for k in range(4):
        a = k * np.pi / 2 + 0.3
        ex = cx + np.cos(a) * 20 * SS
        ey = cy + np.sin(a) * 20 * SS
        for tt in np.linspace(0, 1, 30):
            px = cx + (ex - cx) * tt
            py = cy + (ey - cy) * tt
            wdt = (4.5 - 3.8 * tt) * SS * 0.6
            col = (255, 214, 110) if tt < 0.7 else (200, 140, 255)
            add_color(img, px, py, wdt, col, 0.9 * (1 - tt * 0.35))
    add_color(img, cx, cy, 4.2 * SS, (255, 250, 225), 1.0)

    set_alpha_floor(img)
    return downsample(img, W, H)


def gen_sombraMuerte():
    W, H = 76, 76
    img = new_canvas(W, H)
    cx, cy = W * SS / 2, H * SS / 2

    # LA MUERTE DEL OCASO: el mini-eclipse — disco dorado, limbo blanco,
    # y el anillo rúnico de 3 órbitas con puntas.
    r = 17 * SS
    ys = np.arange(H * SS)
    xs = np.arange(W * SS)
    XX, YY = np.meshgrid(xs, ys)
    dist = np.sqrt((XX - cx) ** 2 + (YY - cy) ** 2)
    t = np.clip(1 - dist / r, 0, 1)

    img[:, :, 0] += np.where(dist < r, 130 + 125 * t ** 0.6, 0)
    img[:, :, 1] += np.where(dist < r, 60 + 140 * t ** 0.9, 0)
    img[:, :, 2] += np.where(dist < r, 20 + 80 * t, 0)
    img[:, :, 3] += np.where(dist < r, 252, 0)

    # el limbo blanco-azulado
    limbo = np.exp(-np.abs(dist - r) / (0.9 * SS))
    img[:, :, 0] += limbo * 220
    img[:, :, 1] += limbo * 230
    img[:, :, 2] += limbo * 255
    img[:, :, 3] = np.maximum(img[:, :, 3], limbo * 255)

    # el anillo rúnico (órbitas con muescas)
    for ring in (22 * SS, 27 * SS):
        aro = np.exp(-np.abs(dist - ring) / (0.7 * SS))
        img[:, :, 0] += aro * 190
        img[:, :, 1] += aro * 130
        img[:, :, 2] += aro * 250
        img[:, :, 3] = np.maximum(img[:, :, 3], aro * 235)

    # las puntas orbitales
    for k in range(6):
        a = k * np.pi / 3
        for ring in (22 * SS, 27 * SS):
            add_color(img, cx + np.cos(a) * ring, cy + np.sin(a) * ring,
                      1.4 * SS, (230, 190, 255), 0.95)

    # el corazón
    add_color(img, cx, cy, 5.5 * SS, (255, 252, 235), 1.0)

    set_alpha_floor(img)
    return downsample(img, W, H)


# =====================================================================
#  EL MAIN
# =====================================================================

def save(arr, path, name):
    Image.fromarray(arr, 'RGBA').save(path)
    print(f"  ✓ {name}: {path} ({arr.shape[1]}×{arr.shape[0]})")


if __name__ == '__main__':
    print("gen_v627_assets.py — LOS PNGs DE v6.27")
    save(gen_bolsa(), os.path.join(ITEM_OUT, 'ArsenalBag.png'),
         'LA BOLSA DEL ARSENAL PRIMORDIAL')
    save(gen_ocaso(), os.path.join(WPN_OUT, 'OcasoAethonStaff.png'),
         'EL BASTÓN DEL OCASO DE AETHON')
    save(gen_buff_ocaso(), os.path.join(BUF_OUT, 'OcasoActivoBuff.png'),
         'BUFF OCASO ACTIVO')
    save(gen_buff_sobrecalentado(), os.path.join(BUF_OUT, 'SobrecalentadoBuff.png'),
         'BUFF SOBRECALENTADO')
    save(gen_sombraFragmento(), os.path.join(PRJ_OUT, 'OcasoShardProjectile.png'),
         'SOMBRA FRAGMENTO DEL OCASO')
    save(gen_sombraMuerte(), os.path.join(PRJ_OUT, 'OcasoBurstProjectile.png'),
         'SOMBRA MUERTE DEL OCASO')
    print("v6.27: 6 PNGs generados.")
